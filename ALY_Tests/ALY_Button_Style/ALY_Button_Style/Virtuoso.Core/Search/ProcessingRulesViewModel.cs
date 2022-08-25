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
    [ExportMetadata("FieldName", "ProcessingRulesSearch")]
    [Export(typeof(ISearch))]
    public class ProcessingRulesViewModel : GenericBase, ISearch
    {
        public IProcessingRulesService Model { get; set; }

        public void Search(bool isSystemSearch, List<SearchParameter> parameters)
        {
            Model.SearchParameters.Clear();
            parameters.ForEach(p => Model.SearchParameters.Add(p));
            Model.GetSearchAsync(isSystemSearch);
        }

        [ImportingConstructor]
        public ProcessingRulesViewModel(IProcessingRulesService _model)
        {
            SelectCommand = new RelayCommand<RuleHeader>(p =>
            {
                SelectAction?.Invoke();
            });

            Model = _model;
            Model.OnLoaded += OnLoaded;
            Rules.SortDescriptions.Add(new SortDescription("RuleDefinition.RuleDescription",
                ListSortDirection.Ascending)); //initial sort ordering...must match a bound property in the XAML
            Rules.SortDescriptions.Add(new SortDescription("ServiceLine.Name",
                ListSortDirection.Ascending)); //initial sort ordering...must match a bound property in the XAML
        }

        private CollectionViewSource rules = new CollectionViewSource();

        public CollectionViewSource Rules
        {
            get { return rules; }
            set
            {
                rules = value;
                RaisePropertyChanged("rules");
            }
        }

        public RelayCommand<RuleHeader> SelectCommand { get; protected set; }

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

        void OnLoaded(object sender, EntityEventArgs<RuleHeader> e)
        {
            if (e.Error == null)
            {
                rules.Source = e.Results.ToList();

                this.RaisePropertyChangedLambda(p => p.Rules);
                this.RaisePropertyChangedLambda(p => p.TotalRecords);
            }
            else
            {
                ErrorDetailLogger.LogDetails(e, true, "Virtuoso.Core.ProcessingRulesViewModel.OnLoaded");
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

        public int TotalRecords => Model.RuleHeaders.Count;

        public string SelectedField => "RuleHeaderKey";

        public string SelectedValue
        {
            get
            {
                if (SelectedItem == null)
                {
                    return String.Empty;
                }

                return ((RuleHeader)SelectedItem).RuleHeaderKey.ToString();
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