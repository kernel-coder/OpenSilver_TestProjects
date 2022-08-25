#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
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
    [ExportMetadata("FieldName", "NonServiceTypeSearch")]
    [Export(typeof(ISearch))]
    public class NonServiceTypeViewModel : GenericBase, ISearch
    {
        public INonServiceTypeService Model { get; set; }

        public void Search(bool isSystemSearch, List<SearchParameter> parameters)
        {
            Model.SearchParameters.Clear();
            parameters.ForEach(p => Model.SearchParameters.Add(p));
            Model.GetSearchAsync(isSystemSearch);
        }

        [ImportingConstructor]
        public NonServiceTypeViewModel(INonServiceTypeService _model)
        {
            SelectCommand = new RelayCommand<NonServiceType>(p =>
            {
                SelectAction?.Invoke();
            });

            Model = _model;
            Model.OnLoaded += OnLoaded;
            NonServiceTypes.SortDescriptions.Add(new SortDescription("Description",
                ListSortDirection.Ascending)); //initial sort ordering...must match a bound property in the XAML
        }

        private CollectionViewSource _NonServiceTypes = new CollectionViewSource();

        public CollectionViewSource NonServiceTypes
        {
            get { return _NonServiceTypes; }
            set
            {
                _NonServiceTypes = value;
                RaisePropertyChanged("NonServiceTypes");
            }
        }

        public RelayCommand<NonServiceType> SelectCommand { get; protected set; }

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

        void OnLoaded(object sender, EntityEventArgs<NonServiceType> e)
        {
            if (e.Error == null)
            {
                NonServiceTypes.Source = e.Results.ToObservableCollection();

                this.RaisePropertyChangedLambda(p => p.NonServiceTypes);
                this.RaisePropertyChangedLambda(p => p.TotalRecords);
            }
            else
            {
                ErrorDetailLogger.LogDetails(e, true, "Virtuoso.Core.Search.NonServiceTypeViewModel.OnLoaded");
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

        public int TotalRecords => Model.NonServiceTypes.Count;

        public string SelectedField => "NonServiceTypeKey";

        public string SelectedValue
        {
            get
            {
                if (SelectedItem == null)
                {
                    return String.Empty;
                }

                return ((NonServiceType)SelectedItem).NonServiceTypeKey.ToString();
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