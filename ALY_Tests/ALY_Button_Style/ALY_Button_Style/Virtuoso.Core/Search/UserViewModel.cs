#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Events;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Helpers;
using Virtuoso.Metrics;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Search
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("FieldName", "UserSearch")]
    [Export(typeof(ISearch))]
    public class UserViewModel : GenericBase, ISearch
    {
        public IUserService Model { get; set; }

        public void Search(bool isSystemSearch, List<SearchParameter> parameters)
        {
            Model.SearchParameters.Clear();
            parameters.ForEach(p => Model.SearchParameters.Add(p));
            Model.GetSearchAsync(isSystemSearch);
        }

        [ImportingConstructor]
        public UserViewModel(IUserService _model)
        {
            SelectCommand = new RelayCommand<UserProfile>(p =>
            {
                SelectAction?.Invoke();
            });

            Model = _model;
            Model.SetLocationForMonitoring(Logging.LocationOverride.Search);
            Model.OnLoaded += OnLoaded;
            UserProfiles.SortDescriptions.Add(new SortDescription("UserName",
                ListSortDirection.Ascending)); //initial sort ordering...must match a bound property in the XAML
            UserProfiles.SortDescriptions.Add(new SortDescription("FullName",
                ListSortDirection.Ascending)); //initial sort ordering...must match a bound property in the XAML
        }

        private CollectionViewSource _UserProfiles = new CollectionViewSource();

        public CollectionViewSource UserProfiles
        {
            get { return _UserProfiles; }
            set
            {
                _UserProfiles = value;
                RaisePropertyChanged("UserProfiles");
            }
        }

        public RelayCommand<UserProfile> SelectCommand { get; protected set; }

        private Object _SelectedItem;

        public Object SelectedItem
        {
            get { return _SelectedItem; }
            set
            {
                if (_SelectedItem != value)
                {
                    _SelectedItem = value;

                    this.RaisePropertyChangedLambda(p => p.SelectedItem);
                    this.RaisePropertyChangedLambda(p => p.SelectedValue);

                    if (OnItemSelected != null)
                    {
                        OnItemSelected();
                    }
                }
            }
        }

        public bool IsBusy
        {
            get { return Model.IsLoading; }
            set
            {
                if (Model.IsLoading != value)
                {
                    Model.IsLoading = value;
                    this.RaisePropertyChangedLambda(p => p.IsBusy);
                }
            }
        }

        void OnLoaded(object sender, EntityEventArgs<UserProfile> e)
        {
            if (e.Error == null)
            {
                UserProfiles.Source = e.Results.ToList();

                this.RaisePropertyChangedLambda(p => p.UserProfiles);
                this.RaisePropertyChangedLambda(p => p.TotalRecords);
            }
            else
            {
                ErrorDetailLogger.LogDetails(e, true, "Virtuoso.Core.UserProfileViewModel.OnLoaded");
            }

            IsBusy = false;
        }

        #region ISearch Members

        private Action _OnItemSelected;

        public Action OnItemSelected
        {
            get { return _OnItemSelected; }
            set { _OnItemSelected = value; }
        }

        private Action _SearchAction;

        public Action SelectAction
        {
            get { return _SearchAction; }
            set { _SearchAction = value; }
        }

        public int TotalRecords => Model.UserProfiles.Count;

        public string SelectedField => "UserId";

        public string SelectedValue
        {
            get
            {
                if (SelectedItem == null)
                {
                    return String.Empty;
                }

                return ((UserProfile)SelectedItem).UserId.ToString();
            }
        }

        public void ClearResults()
        {
            SelectedItem = null;

            Model.Clear();

            this.RaisePropertyChangedLambda(p => p.TotalRecords);
            this.RaisePropertyChangedLambda(p => p.SelectedValue);
        }

        #endregion
    }
}