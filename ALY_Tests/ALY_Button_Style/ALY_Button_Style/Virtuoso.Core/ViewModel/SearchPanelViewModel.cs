#region Usings
#if OPENSILVER
using Autofac.Features.Metadata;
#endif
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Client.Infrastructure.Storage;
using Virtuoso.Core.Model;
using Virtuoso.Core.Navigation;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;

#endregion

namespace Virtuoso.Core.ViewModel
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export]
    public class SearchPanelViewModel : GenericBase
    {
        public event EventHandler<MenuEventArgs> ItemSelected;

        public SearchPanelViewModel Model => this;
        public bool? AllowAddNewOverride { get; set; }
        public int? ServiceLineTypeFilter { get; set; }

        public string CurrentSearchOverride
        {
            get { return currentSearchOverride; }
            set
            {
                currentSearchOverride = value;
                this.RaisePropertyChangedLambda(p => p.CurrentSearchOverride);
                if (SelectedSearch != null)
                {
                    if (AllowAddNewOverride != null) SelectedSearch.AllowAddNew = (bool)AllowAddNewOverride;
                    if (ServiceLineTypeFilter != null) SelectedSearch.ServiceLineTypeFilter = (int)ServiceLineTypeFilter;

                }
            }
        }

        private string currentSearchOverride;

        protected bool IsOnline { get; set; }
        protected CommandManager CommandManager { get; set; }

        public Action OnSearchClicked { get; set; }

#if OPENSILVER
        List<Meta<Lazy<ISearch>>> SearchViewModels;
#else
        List<Lazy<ISearch, ISearchMetadata>> SearchViewModels;
#endif

        public SearchPanelViewModel()
        {
            SetupCommands();
            IsOnline = EntityManager.IsOnline;
            CommandManager = new CommandManager(this);

            Messenger.Default.Register<bool>(this, Constants.Messaging.NetworkAvailability,
                _IsOnline =>
                {
                    IsOnline = _IsOnline;
                    CommandManager.RaiseCanExecuteChanged();
                });

#if OPENSILVER
            SearchViewModels = Virtuoso.Client.Core.VirtuosoContainer.Current.GetExports<ISearch>().ToList();
#else
            SearchViewModels = Virtuoso.Client.Core.VirtuosoContainer.Current.GetExports<ISearch, ISearchMetadata>().ToList();
#endif

            AllowAddNewOverride = null;
            ServiceLineTypeFilter = null;
        }

        //NOTE: this method is only called when we're using Search for a specific item - E.G. NOT via the main menu's system search.
        private void BuildSearchItemForScreen(string screenName, bool performRoleAccess = true)
        {
            var screenFound = false;
#if OPENSILVER
            var stream = GetType().Assembly.GetManifestResourceStream(@"..\Virtuoso\Controls\MenuComponents.xml");
            XDocument document = XDocument.Load(stream);
#else
            XDocument document = XDocument.Load("/Virtuoso;component/Controls/MenuComponents.xml");
#endif

            foreach(XElement toplevel in document.Element("MenuTree").Elements("TopLevelMenu"))
            {
                if (screenFound)
                {
                    break;
                }

                foreach (XElement sublevel in toplevel.Elements("SubMenuItem"))
                {
                    if (screenFound)
                    {
                        break;
                    }

                    bool addresource = true;
                    string resource = (string)sublevel.Attribute("role");
                    if (!string.IsNullOrEmpty(resource))
                    {
                        addresource = performRoleAccess == false || RoleAccessHelper.CheckPermission(resource);
                        if (addresource == false)
                        {
                            resource = (string)sublevel.Attribute("alternaterole");
                            if (!string.IsNullOrEmpty(resource))
                            {
                                addresource = performRoleAccess == false || RoleAccessHelper.CheckPermission(resource);
                            }
                        }
                    }

                    foreach (XElement search in sublevel.Elements("SearchItem"))
                    {
                        var _screen = (string)search.Attribute("displayURL");
                        if (screenName.ToLower().Equals(_screen.ToLower()))
                        {
                            bool allowadd = true;
                            var _allowAdd = search.Attribute("allowAdd");
                            if (_allowAdd != null)
                            {
                                allowadd = (Boolean)search.Attribute("allowAdd");
                            }

                            var searchRecord = new SearchRecord
                            {
                                Type = (string)search.Attribute("field"),
                                Screen = (string)search.Attribute("displayURL"),
                                Label = (string)search.Attribute("label"),
                                URI = (string)search.Attribute("destinationURL"),
                                //AllowAddNew = false,
                                AllowAddNew = addresource && allowadd,
                                SearchFields = new ObservableCollection<SearchField>()
                            };

                            foreach (XElement searchfield in search.Elements("SearchField"))
                            {
                                var sf = SearchFieldFactory(searchfield);
                                if (sf.Label.Trim().ToLower().Equals("include inactive") == false)
                                {
                                    searchRecord.SearchFields.Add(sf);
                                }
                            }
                            
                            SearchItems.Add(searchRecord);

                            screenFound = true;
                        }
                    }
                }
            }

            foreach (var _searchItem in SearchItems)
            {
#if OPENSILVER
                var _vm = SearchViewModels.FirstOrDefault(m => m.Metadata["FieldName"].ToString() == _searchItem.Type);
#else
                var _vm = SearchViewModels.FirstOrDefault(m => m.Metadata.FieldName == _searchItem.Type);
#endif

                if (_vm != null)
                {
#if OPENSILVER
                    _searchItem.SearchResultsViewModel = _vm.Value.Value;
#else
                    _searchItem.SearchResultsViewModel = _vm.Value;
#endif
                    ISearchWithServiceLineTypeFilter _isvm = _searchItem.SearchResultsViewModel as ISearchWithServiceLineTypeFilter;
                    if (_isvm != null) _isvm.ServiceLineTypeFilter = ServiceLineTypeFilter;
                    _searchItem.SearchResultsViewModel.SelectAction = SelectAction;
                    _searchItem.SearchResultsViewModel.OnItemSelected = UpdateUI;
                    _searchItem.SearchResultsView = String.Format("{0}ResultsView", _searchItem.Type);
                }
                else
                {
                    MessageBox.Show(String.Format("Could not find VM for {0}", _searchItem.Type));
                }
            }
        }

        private void BuildSearchItems(bool performRoleAccess = true)
        {
            if (SearchItems.Any())
            {
                return;
            }

#if OPENSILVER
            var stream = GetType().Assembly.GetManifestResourceStream(@"..\Virtuoso\Controls\MenuComponents.xml");
            XDocument document = XDocument.Load(stream);
#else
            XDocument document = XDocument.Load("/Virtuoso;component/Controls/MenuComponents.xml");
#endif

            foreach (XElement toplevel in document.Element("MenuTree").Elements("TopLevelMenu"))
            {
                foreach (XElement sublevel in toplevel.Elements("SubMenuItem"))
                {
                    bool addresource = true;
                    string resource = (string)sublevel.Attribute("role");
                    if (!string.IsNullOrEmpty(resource))
                    {
                        addresource = performRoleAccess == false || RoleAccessHelper.CheckPermission(resource);
                        if (addresource == false)
                        {
                            resource = (string)sublevel.Attribute("alternaterole");
                            if (!string.IsNullOrEmpty(resource))
                            {
                                addresource = performRoleAccess == false || RoleAccessHelper.CheckPermission(resource);
                            }
                        }
                    }

                    foreach (XElement search in sublevel.Elements("SearchItem"))
                        if (performRoleAccess == false ||
                            (RoleAccessHelper.CheckPermission(((string)sublevel.Attribute("role")))
                             || (!string.IsNullOrEmpty((string)sublevel.Attribute("alternaterole"))
                                 && RoleAccessHelper.CheckPermission(((string)sublevel.Attribute("alternaterole")))))
                           )
                        {
                            bool allowadd = true;
                            var _allowAdd = search.Attribute("allowAdd");
                            if (_allowAdd != null)
                            {
                                allowadd = (Boolean)search.Attribute("allowAdd");
                            }

                            var searchRecord = new SearchRecord
                            {
                                Type = (string)search.Attribute("field"),
                                Screen = (string)search.Attribute("displayURL"),
                                Label = (string)search.Attribute("label"),
                                URI = (string)search.Attribute("destinationURL"),
                                AllowAddNew = addresource && allowadd && (string.IsNullOrEmpty(CurrentSearchOverride)),
                                SearchFields = new ObservableCollection<SearchField>()
                            };

                            foreach (XElement searchfield in search.Elements("SearchField"))
                            {
                                var sf = SearchFieldFactory(searchfield);
                                searchRecord.SearchFields.Add(sf);
                            }

                            SearchItems.Add(searchRecord);
                        }
                }
            }

            foreach (var _searchItem in SearchItems)
            {
#if OPENSILVER
                var _vm = SearchViewModels.FirstOrDefault(m => m.Metadata["FieldName"].ToString() == _searchItem.Type);
#else
                var _vm = SearchViewModels.FirstOrDefault(m => m.Metadata.FieldName == _searchItem.Type);
#endif

                if(_vm != null)
                {
#if OPENSILVER
                    _searchItem.SearchResultsViewModel = _vm.Value.Value;
#else
                    _searchItem.SearchResultsViewModel = _vm.Value;
#endif
                    _searchItem.SearchResultsViewModel.SelectAction = SelectAction;
                    _searchItem.SearchResultsViewModel.OnItemSelected = UpdateUI;
                    _searchItem.SearchResultsView = String.Format("{0}ResultsView", _searchItem.Type);
                }
                else
                {
                    MessageBox.Show(String.Format("Could not find VM for {0}", _searchItem.Type));
                }
            }
        }

        private void SetupCommands()
        {
            AddCommand = new RelayCommand(AddAction, () => IsOnline);

            SearchCommand = new RelayCommand(() =>
            {
                if (SelectedSearch.Valid())
                {
                    SearchAction();
                }
                else
                {
                    MessageBox.Show("Cannot search - have errors!");
                }
            }, () => IsOnline && SelectedSearch != null);

            ClearCommand = new RelayCommand(() =>
            {
                if (SelectedSearch != null)
                {
                    foreach (var item in SelectedSearch.SearchFields)
                    {
                        item.Value = string.Empty;
                        item.Clear();
                    }
                }

                _SavedSelectedSearch = SelectedSearch;
                SelectedSearch = null;
                SelectedSearch = _SavedSelectedSearch;

                ClearResultsAction();
            }, () => { return SelectedSearch != null; });

            SelectCommand = new RelayCommand(SelectAction,
                () => ((IsOnline) && (SelectedSearch != null) &&
                       String.IsNullOrEmpty(SelectedSearch.SearchResultsViewModel.SelectedValue) == false));

            ClearResultsCommand = new RelayCommand(ClearResultsAction,
                () => ((SelectedSearch != null) && (SelectedSearch.SearchResultsViewModel.TotalRecords > 0)));
        }

        public Dictionary<string, List<SearchFieldValue>> SaveStateDictionaryIsoStorage
        {
            get
            {
                return VirtuosoStorageContext.LocalSettings.Get<Dictionary<string, List<SearchFieldValue>>>(
                    "SystemSearchState");
            }
            set
            {
                VirtuosoStorageContext.LocalSettings.Put("SystemSearchState", value);
                RaisePropertyChanged("SystemSearchState");
            }
        }

        public void SaveState()
        {
            var SaveStateDictionary = new Dictionary<string, List<SearchFieldValue>>();
            foreach (var searchRecord in SearchItems)
            {
                foreach (var searchField in searchRecord.SearchFields)
                {
                    var persist = searchField.SaveState;
                    if (persist)
                    {
                        var _key = searchRecord.Label + "." + searchField.Label; //E.G. Patient.Status
                        var _value = searchField.FieldValues().ToList();
                        SaveStateDictionary.Add(_key, _value);
                    }
                }
            }

            SaveStateDictionaryIsoStorage = SaveStateDictionary; //save SaveStateDictionary to isolated storage
        }

        private void RestoreState()
        {
            try
            {
#if OPENSILVER
            if (SaveStateDictionaryIsoStorage == null)
            {
               SaveState();
            }
#endif
            var SaveStateDictionary = SaveStateDictionaryIsoStorage;

                foreach (var _key in SaveStateDictionary.Keys)
                {
                    //E.G. _key = Patient.Status = SubMenuItem.SearchField
                    var _entity_key = _key.Split('.')[0];
                    var _field_key = _key.Split('.')[1];
                    var _value = SaveStateDictionary[_key];

                    var _record = SearchItems.FirstOrDefault(i => i.Label == _entity_key);
                    if (_record != null)
                    {
                        //Find which searchfield
                        var _field = _record.SearchFields.FirstOrDefault(f => f.Label == _field_key);
                        if (_field != null)
                        {
                            _field.Restore(_value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                //Something wrong - clear the state from isolated storage
                SaveStateDictionaryIsoStorage = new Dictionary<string, List<SearchFieldValue>>();
            }
        }

        private int? __ServiceLineKey;

        private int? ServiceLineKey
        {
            get { return __ServiceLineKey; }
            set { __ServiceLineKey = value; }
        }

        public void InitSearch(bool restoreSearchState, int? serviceLineKey)
        {
            ServiceLineKey = serviceLineKey;

            string currentpage = NonLinearNavigationContentLoader.CurrentPage;
            if (!string.IsNullOrEmpty(CurrentSearchOverride))
            {
                currentpage = CurrentSearchOverride;
                var screen = currentpage.Substring(currentpage.LastIndexOf(".") + 1).Replace("List", "");
                BuildSearchItemForScreen(
                    screen); //Launching dialog for a specific entity - e.g. not using via main menu search
            }
            else
            {
                BuildSearchItems();
            }

            if (restoreSearchState)
            {
                //TODO XXX RestoreState to isolated storage
                RestoreState();
            }

            if (currentpage != null)
            {
                currentpage = currentpage.Substring(currentpage.LastIndexOf(".") + 1).Replace("List", "");

                //find matching search based on current screen
                SelectedSearch = SearchItems.FirstOrDefault(p => p.Screen.Equals(currentpage));
            }

            //if on a screen that doesn't have a search default to the first item which should be patient
            if (SelectedSearch == null)
            {
                SelectedSearch = SearchItems.FirstOrDefault();
            }
        }

        public void UpdateUI()
        {
            CommandManager.RaiseCanExecuteChanged();
        }

        public void AddAction()
        {
            //Developer's NOTE: a GUID is generated and added to the URI to make it unique.
            //                  this is so that is you are already on an 'ADD' screen, and
            //                  launch the search ChildWindow to initiate another 'ADD', that
            //                  the navigation request is actually processed - otherwise, you
            //                  are left on the current 'ADD" screen.
            Uri u = new Uri(
                String.Format("{0}/{1}/add",
                    SelectedSearch.URI,
                    Guid.NewGuid().ToString()), //randomize the 'add' URI
                UriKind.Relative);

            SearchUri = u;

            OnSearchClicked?.Invoke(); //just a callback - initiates a navigation request
        }

        public void ClearResultsAction()
        {
            SelectedSearch.SearchResultsViewModel.ClearResults();
        }

        public void SelectAction()
        {
            if (ItemSelected != null)
            {
                MenuEventArgs me = new MenuEventArgs();
                me.ID = SelectedSearch.SearchResultsViewModel.SelectedValue;
                me.Object = SelectedSearch.SearchResultsViewModel.SelectedItem;
                me.ViewModel = this;
                ItemSelected(this, me);
            }

            if (string.IsNullOrEmpty(CurrentSearchOverride))
            {
                if (CreateQueryString())
                {
                    if (OnSearchClicked != null)
                    {
                        OnSearchClicked();
                    }
                }
            }
        }

        public void SearchAction()
        {
            List<SearchParameter> parameters = new List<SearchParameter>();

            if (SelectedSearch != null)
            {
                foreach (var item in SelectedSearch.SearchFields)
                {
                    foreach (var param in item.FieldValues())
                        if (String.IsNullOrWhiteSpace(param.Value) == false)
                        {
                            var _value = param.Value.Trim(); //end users are accidentally entering trailing spaces...
                            if (String.IsNullOrWhiteSpace(_value) == false)
                            {
                                parameters.Add(new SearchParameter
                                    { Condition = "", Field = param.Name, Value = _value });
                            }
                        }
                }

                if (ServiceLineKey.HasValue)
                {
                    parameters.Add(new SearchParameter
                        { Condition = "", Field = "ServiceLineKey", Value = ServiceLineKey.Value.ToString() });
                }

                SelectedSearch.SearchResultsViewModel.Search(string.IsNullOrEmpty(CurrentSearchOverride), parameters);
            }
        }

        private SearchField SearchFieldFactory(XElement searchfield)
        {
            String search_field_type = (string)searchfield.Attribute("type");
            String AssemblyQualifiedNameFormat =
                "Virtuoso.Core.Model.{0}Factory, Virtuoso.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            Type factoryClass = Type.GetType(String.Format(AssemblyQualifiedNameFormat, search_field_type));
            if (factoryClass != null)
            {
                MethodInfo m = factoryClass.GetMethod("Create");
                if (m != null)
                {
                    return (SearchField)m.Invoke(null, new Object[] { searchfield, (Action)SearchAction });
                }

                throw new Exception(String.Format("Invalid factory for class {0}", search_field_type));
            }

            throw new Exception(String.Format("No factory for class {0}", search_field_type));
        }

        public RelayCommand AddCommand { get; protected set; }

        public RelayCommand SelectCommand { get; protected set; }

        public RelayCommand ClearResultsCommand { get; protected set; }

        public RelayCommand SearchCommand { get; protected set; }

        public RelayCommand ClearCommand { get; protected set; }

        public bool CreateQueryString()
        {
            if (SelectedSearch != null)
            {
                Uri u = new Uri(
                    //String.Format("{0}/{1}", SelectedSearch.URI, SelectedSearch.SearchResultsViewModel.SelectedValue),
                    //String.Format("{0}/{1}={2}",
                    String.Format("{0}/{1}",
                        SelectedSearch.URI,
                        SelectedSearch.SearchResultsViewModel.SelectedValue),
                    UriKind.Relative);

                SearchUri = u;
                return true;
            }

            return false;
        }

        ObservableCollection<SearchRecord> _SearchItems = new ObservableCollection<SearchRecord>();

        public ObservableCollection<SearchRecord> SearchItems
        {
            get { return _SearchItems; }
            set
            {
                _SearchItems = value;
                this.RaisePropertyChangedLambda(p => p.SearchItems);
            }
        }

        SearchRecord _SavedSelectedSearch;
        SearchRecord _SelectedSearch;

        public SearchRecord SelectedSearch
        {
            get { return _SelectedSearch; }
            set
            {
                _SelectedSearch = value;
                this.RaisePropertyChangedLambda(p => p.SelectedSearch);
                this.RaisePropertyChangedLambda(p => p.CurrentSearchOverride);
                CurrentSearchOverride = CurrentSearchOverride;
                SearchCommand.RaiseCanExecuteChanged();
                ClearCommand.RaiseCanExecuteChanged();
            }
        }

        private object _SearchUri;

        public object SearchUri
        {
            get { return _SearchUri; }
            set
            {
                _SearchUri = value;
                this.RaisePropertyChangedLambda(p => p.SearchUri);
            }
        }

        //Possibly will be used for returning selected 'entity'....
        private object _SearchResult;

        public object SearchResult
        {
            get { return _SearchResult; }
            set
            {
                _SearchResult = value;
                this.RaisePropertyChangedLambda(p => p.SearchResult);
            }
        }

        public override void Cleanup()
        {
            CommandManager.CleanUp();

            Messenger.Default.Unregister<bool>(this, Constants.Messaging.NetworkAvailability);
            Messenger.Default.Unregister(this);

            base.Cleanup();
        }
    }
}