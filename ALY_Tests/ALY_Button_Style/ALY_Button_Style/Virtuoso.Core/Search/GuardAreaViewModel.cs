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
    [ExportMetadata("FieldName", "GuardAreaSearch")]
    [Export(typeof(ISearch))]
    public class GuardAreaViewModel : GenericBase, ISearch
    {
        public IGuardAreaService Model { get; set; }

        public void Search(bool isSystemSearch, List<SearchParameter> parameters)
        {
            Model.SearchParameters.Clear();
            parameters.ForEach(p => Model.SearchParameters.Add(p));
            Model.GetSearchAsync(isSystemSearch);
        }

        [ImportingConstructor]
        public GuardAreaViewModel(IGuardAreaService _model)
        {
            SelectCommand = new RelayCommand<GuardArea>(p =>
            {
                SelectAction?.Invoke();
            });

            Model = _model;
            Model.OnLoaded += OnLoaded;
            GuardAreas.SortDescriptions.Add(new SortDescription("GuardAreaID",
                ListSortDirection.Ascending)); //initial sort ordering...must match a bound property in the XAML
        }

        private CollectionViewSource _GuardAreas = new CollectionViewSource();

        public CollectionViewSource GuardAreas
        {
            get { return _GuardAreas; }
            set
            {
                _GuardAreas = value;
                RaisePropertyChanged("GuardAreas");
            }
        }

        public RelayCommand<GuardArea> SelectCommand { get; protected set; }

        private Object _SelectedItem;

        public Object SelectedItem
        {
            get
            {
                return _SelectedItem;
            }
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

        void OnLoaded(object sender, EntityEventArgs<GuardArea> e)
        {
            if (e.Error == null)
            {
                GuardAreas.Source = e.Results.ToList();

                this.RaisePropertyChangedLambda(p => p.GuardAreas);
                this.RaisePropertyChangedLambda(p => p.TotalRecords);
            }
            else
            {
                ErrorDetailLogger.LogDetails(e, true, "Virtuoso.Core.Search.GuardAreaViewModel.OnLoaded");
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

        public int TotalRecords => Model.GuardAreas.Count;

        public string SelectedField => "GuardAreaKey";

        public string SelectedValue
        {
            get
            {
                if (SelectedItem == null)
                {
                    return String.Empty;
                }

                return ((GuardArea)SelectedItem).GuardAreaKey.ToString();
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