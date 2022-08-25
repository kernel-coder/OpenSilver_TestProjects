#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Events;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Helpers;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Search
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("FieldName", "HighRiskMedicationSearch")]
    [Export(typeof(ISearch))]
    public class HighRiskMedicationViewModel : GenericBase, ISearch
    {
        public IHighRiskMedicationService Model { get; set; }

        public void Search(bool isSystemSearch, List<SearchParameter> parameters)
        {
            Model.SearchParameters.Clear();
            parameters.ForEach(p => Model.SearchParameters.Add(p));
            Model.GetSearchAsync(isSystemSearch);
        }

        private List<ServiceLine> slList;

        [ImportingConstructor]
        public HighRiskMedicationViewModel(IHighRiskMedicationService _model)
        {
            SelectCommand = new RelayCommand<HighRiskMedication>(p =>
            {
                SelectAction?.Invoke();
            });

            Model = _model;
            Model.OnLoaded += OnLoaded;
            slList = ServiceLineCache.GetActiveUserServiceLinePlusMe(null);
            HighRiskMedications.SortDescriptions.Add(new SortDescription("MedicationName",
                ListSortDirection.Ascending));
            HighRiskMedications.SortDescriptions.Add(
                new SortDescription("ServiceLineName", ListSortDirection.Ascending));
            HighRiskMedications.Filter += HighRiskMedicationsFilter;
        }

        private void HighRiskMedicationsFilter(object sender, FilterEventArgs e)
        {
            var m = e.Item as HighRiskMedication;
            if ((m == null) || (slList == null))
            {
                e.Accepted = false;
                return;
            }

            e.Accepted = slList.Any(s => s.ServiceLineKey == m.ServiceLineKey);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            HighRiskMedications.Filter -= HighRiskMedicationsFilter;
            if (Model != null)
            {
                Model.OnLoaded -= OnLoaded;
            }
        }

        private CollectionViewSource _HighRiskMedications = new CollectionViewSource();

        public CollectionViewSource HighRiskMedications
        {
            get { return _HighRiskMedications; }
            set
            {
                _HighRiskMedications = value;
                RaisePropertyChanged("HighRiskMedications");
            }
        }

        public RelayCommand<HighRiskMedication> SelectCommand { get; protected set; }

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

        void OnLoaded(object sender, EntityEventArgs<HighRiskMedication> e)
        {
            if (e.Error == null)
            {
                HighRiskMedications.Source = e.Results.ToList();
                if (HighRiskMedications.Source != null)
                {
                    HighRiskMedications.View.Refresh();
                }

                this.RaisePropertyChangedLambda(p => p.HighRiskMedications);
                this.RaisePropertyChangedLambda(p => p.TotalRecords);
            }
            else
            {
                ErrorDetailLogger.LogDetails(e, true, "Virtuoso.Core.Search.HighRiskMedicationViewModel.OnLoaded");
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

        public int TotalRecords => Model.HighRiskMedications.Count;

        public string SelectedField => "HighRiskMedicationKey";

        public string SelectedValue
        {
            get
            {
                if (SelectedItem == null)
                {
                    return String.Empty;
                }

                return ((HighRiskMedication)SelectedItem).HighRiskMedicationKey.ToString();
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