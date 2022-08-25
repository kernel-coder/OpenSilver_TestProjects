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
    [ExportMetadata("FieldName", "DisciplineSearch")]
    [Export(typeof(ISearch))]
    public class DisciplineViewModel : GenericBase, ISearch
    {
        public IDisciplineService Model { get; set; }

        public void Search(bool isSystemSearch, List<SearchParameter> parameters)
        {
            Model.SearchParameters.Clear();
            parameters.ForEach(p => Model.SearchParameters.Add(p));
            Model.GetSearchAsync(isSystemSearch);
        }

        [ImportingConstructor]
        public DisciplineViewModel(IDisciplineService _model)
        {
            SelectCommand = new RelayCommand<Discipline>(p =>
            {
                SelectAction?.Invoke();
            });

            Model = _model;
            Model.OnLoaded += OnLoaded;
            Disciplines.SortDescriptions.Add(new SortDescription("Description", ListSortDirection.Ascending)); //initial sort ordering...must match a bound property in the XAML          
        }

        private CollectionViewSource _Disciplines = new CollectionViewSource();

        public CollectionViewSource Disciplines
        {
            get { return _Disciplines; }
            set
            {
                _Disciplines = value;
                RaisePropertyChanged("Disciplines");
            }
        }

        public RelayCommand<Discipline> SelectCommand { get; protected set; }

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

        void OnLoaded(object sender, EntityEventArgs<Discipline> e)
        {
            if (e.Error == null)
            {
                var mask = TenantSettingsCache.Current.ServiceLineTypeUseBits;

                Disciplines.Source = e.Results.Where(d => (d.ServiceLineTypeUseBits & mask) > 0).ToList();

                this.RaisePropertyChangedLambda(p => p.Disciplines);
                this.RaisePropertyChangedLambda(p => p.TotalRecords);
            }
            else
            {
                ErrorDetailLogger.LogDetails(e, true, "Virtuoso.Core.Search.DisciplineViewModel.OnLoaded");
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

        public int TotalRecords
        {
            get
            {
                if (Disciplines.View != null)
                {
                    return Disciplines.View.Cast<Discipline>().Count();
                }

                return 0;
            }
        }

        public string SelectedField => "DisciplineKey";

        public string SelectedValue
        {
            get
            {
                if (SelectedItem == null)
                {
                    return String.Empty;
                }

                return ((Discipline)SelectedItem).DisciplineKey.ToString();
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