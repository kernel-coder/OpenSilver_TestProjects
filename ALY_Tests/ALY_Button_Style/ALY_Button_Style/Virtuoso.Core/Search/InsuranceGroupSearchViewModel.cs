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
    [ExportMetadata("FieldName", "InsuranceGroupSearch")]
    [Export(typeof(ISearch))]
    public class InsuranceGroupSearchViewModel : GenericBase, ISearch
    {
        IInsuranceService Model { get; set; }

        [ImportingConstructor]
        public InsuranceGroupSearchViewModel(IInsuranceService _model)
        {
            Model = _model;
            Model.OnInsuranceGroupsLoaded += OnLoaded;
            InsuranceGroups.SortDescriptions.Add(new SortDescription("Name",
                ListSortDirection.Ascending)); // Initial sort ordering, must match property in XAML
            SelectCommand = new RelayCommand<InsuranceGroup>(p =>
            {
                SelectAction?.Invoke();
            });
        }

        #region Members

        private CollectionViewSource _InsuranceGroups = new CollectionViewSource();

        public CollectionViewSource InsuranceGroups
        {
            get { return _InsuranceGroups; }
            set
            {
                _InsuranceGroups = value;
                RaisePropertyChanged("InsuranceGroups");
            }
        }

        #endregion Members

        #region ISearch Implementation

        public void Search(bool isSystemSearch, List<SearchParameter> parameters)
        {
            Model.SearchParameters.Clear();
            parameters.ForEach(p => Model.SearchParameters.Add(p));

            var nameRow = parameters.FirstOrDefault(a => a.Field == "InsuranceGroupName");
            var inactiveRow = parameters.FirstOrDefault(a => a.Field == "Inactive");

            string name = "";
            string inactiveString = "";

            if (nameRow != null && nameRow.Value != null)
            {
                name = nameRow.Value;
            }

            if (inactiveRow != null && inactiveRow.Value != null)
            {
                inactiveString = inactiveRow.Value;
            }

            bool includeInactive = inactiveString.ToLower().Contains("true");

            Model.GetInsuranceGroupByNameAsync(name, includeInactive);
        }

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

        public int TotalRecords => Model.InsuranceGroups.Count;

        public string SelectedField => "InsuranceGroupKey";

        public string SelectedValue
        {
            get
            {
                if (SelectedItem == null)
                {
                    return String.Empty;
                }

                return ((InsuranceGroup)SelectedItem).InsuranceGroupKey.ToString();
            }
        }

        public void ClearResults()
        {
            SelectedItem = null;

            Model.Clear();

            this.RaisePropertyChangedLambda(p => p.TotalRecords);
            this.RaisePropertyChangedLambda(p => p.SelectedValue);
        }

        #endregion ISearch Implementation

        #region Event Handlers

        void OnLoaded(object sender, EntityEventArgs<InsuranceGroup> e)
        {
            if (e.Error == null)
            {
                InsuranceGroups.Source = e.Results.ToList();

                this.RaisePropertyChangedLambda(p => p.InsuranceGroups);
                this.RaisePropertyChangedLambda(p => p.TotalRecords);
            }
            else
            {
                ErrorDetailLogger.LogDetails(e, true, "Virtuoso.Core.Search.InsuranceGroupSearchViewModel.OnLoaded");
            }

            IsBusy = false;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (Model != null)
            {
                Model.OnInsuranceGroupsLoaded -= OnLoaded;
            }
        }

        public RelayCommand<InsuranceGroup> SelectCommand { get; protected set; }

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

        #endregion
    }
}