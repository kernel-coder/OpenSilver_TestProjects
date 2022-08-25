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
    [ExportMetadata("FieldName", "FacilitySearch")]
    [Export(typeof(ISearch))]
    public class FacilityViewModel : GenericBase, ISearch
    {
        public IFacilityService Model { get; set; }

        public void Search(bool isSystemSearch, List<SearchParameter> parameters)
        {
            Model.SearchParameters.Clear();
            parameters.ForEach(p => Model.SearchParameters.Add(p));
            Model.GetSearchAsync(isSystemSearch);
        }

        [ImportingConstructor]
        public FacilityViewModel(IFacilityService _model)
        {
            SelectCommand = new RelayCommand<Facility>(p =>
            {
                SelectAction?.Invoke();
            });

            Model = _model;
            Model.OnLoaded += OnLoaded;
            Facilities.SortDescriptions.Add(new SortDescription("Name",
                ListSortDirection.Ascending)); //initial sort ordering...must match a bound property in the XAML
        }

        private CollectionViewSource _Facilities = new CollectionViewSource();

        public CollectionViewSource Facilities
        {
            get { return _Facilities; }
            set
            {
                _Facilities = value;
                RaisePropertyChanged("Facilities");
            }
        }

        public RelayCommand<Facility> SelectCommand { get; protected set; }

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

        void OnLoaded(object sender, EntityEventArgs<Facility> e)
        {
            if (e.Error == null)
            {
                Facilities.Source = e.Results.ToList();

                this.RaisePropertyChangedLambda(p => p.Facilities);
                this.RaisePropertyChangedLambda(p => p.TotalRecords);
            }
            else
            {
                ErrorDetailLogger.LogDetails(e, true, "Virtuoso.Core.Search.FacilityViewModel.OnLoaded");
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

        public int TotalRecords => Model.Facilities.Count;

        public string SelectedField => "FacilityKey";

        public string SelectedValue
        {
            get
            {
                if (SelectedItem == null)
                {
                    return String.Empty;
                }

                return ((Facility)SelectedItem).FacilityKey.ToString();
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