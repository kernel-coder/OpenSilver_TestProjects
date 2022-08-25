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
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Search
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("FieldName", "PhysicianSearch")]
    [Export(typeof(ISearch))]
    public class PhysicianViewModel : GenericBase, ISearchWithQueryLimit, ISearchWithServiceLineTypeFilter
    {
        public IPhysicianService Model { get; set; }

        // NOTE: this will match ResultLimit on server method - [Query(ResultLimit = ???)] public IQueryable<Physician> GetPhysicianForSearch(...)        
        public int MAX_RECORDS => QueryLimits.PhysicianSearch;

        public int? ServiceLineTypeFilter { get; set; }

        public string MaxRecordsDisplayText =>
            $"The search yields results in excess of {MAX_RECORDS} records.  Please search using additional criteria to decrease the result set.";

        public void Search(bool isSystemSearch, List<SearchParameter> parameters)
        {
            Model.SearchParameters.Clear();
            parameters.ForEach(p => Model.SearchParameters.Add(p));

            int inactiveCount = parameters.Count(a => a.Field == "Inactive" && a.Value == "True");

            InactiveChecked = inactiveCount > 0;

            Model.GetSearchAsync(isSystemSearch);
        }

        [ImportingConstructor]
        public PhysicianViewModel(IPhysicianService _model) 
        {
            SelectCommand = new RelayCommand<Physician>(p =>
            {
                SelectAction?.Invoke();
            });

            Model = _model;
            Model.OnLoaded += OnLoaded;
            Physicians.SortDescriptions.Add(new SortDescription("FullName",
                ListSortDirection.Ascending)); //initial sort ordering...must match a bound property in the XAML
        }

        private bool _InactiveChecked;

        public bool InactiveChecked
        {
            get { return _InactiveChecked; }
            set
            {
                _InactiveChecked = value;
                RaisePropertyChanged("InactiveChecked");
            }
        }

        private CollectionViewSource _Physicians = new CollectionViewSource();

        public CollectionViewSource Physicians
        {
            get { return _Physicians; }
            set
            {
                _Physicians = value;
                RaisePropertyChanged("Physicians");
            }
        }

        public RelayCommand<Physician> SelectCommand { get; protected set; }

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

        void OnLoaded(object sender, EntityEventArgs<Physician> e)
        {
            if (e.Error == null)
            {
                Physicians.Source = null;
                IEnumerable<Physician> pList = ((e.Results != null) && (ServiceLineTypeFilter != null)) ?
                    e.Results.Where(s => ((ServiceLineTypeFilter & s.ServiceLineTypeUseBits) != 0)).ToList() :
                    e.Results;
                totalRecords = (pList == null) ? 0 : pList.Count();

                if (totalRecords == 0)
                {
                    NoResults = true;
                    TooManyResults = false;
                }
                else if (totalRecords > MAX_RECORDS)
                {
                    TooManyResults = true;
                    NoResults = false;
                    totalRecords = 0; //reset to no results...
                }
                else
                {
                    Physicians.Source = pList;
                }

                this.RaisePropertyChangedLambda(p => p.Physicians);
                this.RaisePropertyChangedLambda(p => p.MAX_RECORDS);
                this.RaisePropertyChangedLambda(p => p.MaxRecordsDisplayText);
                this.RaisePropertyChangedLambda(p => p.TotalRecords);
                this.RaisePropertyChangedLambda(p => p.NoResults);
                this.RaisePropertyChangedLambda(p => p.TooManyResults);
            }
            else
            {
                ErrorDetailLogger.LogDetails(e, true, "Virtuoso.Core.Search.PhysicianViewModel.OnLoaded");
            }

            IsBusy = false;
        }

        public bool NoResults { get; set; }
        public bool TooManyResults { get; set; }

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

        int totalRecords;
        public int TotalRecords => totalRecords;

        public string SelectedField => "PhysicianKey";

        public string SelectedValue
        {
            get
            {
                if (SelectedItem == null)
                {
                    return String.Empty;
                }

                return ((Physician)SelectedItem).PhysicianKey.ToString();
            }
        }

        public void ClearResults()
        {
            SelectedItem = null;
            Physicians.Source = null;

            Model.Clear();

            totalRecords = 0;
            NoResults = true;
            TooManyResults = false;
            this.RaisePropertyChangedLambda(p => p.TotalRecords);
            this.RaisePropertyChangedLambda(p => p.NoResults);
            this.RaisePropertyChangedLambda(p => p.TooManyResults);
            this.RaisePropertyChangedLambda(p => p.SelectedValue);
        }

        #endregion
    }
}