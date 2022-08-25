#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Events;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.View;
using Virtuoso.Core.ViewModel;
using Virtuoso.Helpers;
using Virtuoso.Metrics;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Search
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("FieldName", "PatientSearch")]
    [Export(typeof(ISearch))]
    public class PatientSearchViewModel : GenericBase, ISearchWithQueryLimit
    {
        #region Constructors

        [ImportingConstructor]
        public PatientSearchViewModel(IPatientService _model)
        {
            SetupCommands();

            Model = _model;
            Model.SetLocationForMonitoring(Logging.LocationOverride.Search);
            Model.OnSearchLoaded += OnSearchLoaded;
        }

        #endregion

        #region "Properties

        public IPatientService Model { get; set; }

        public int MAX_RECORDS =>
            QueryLimits.PatientSearch; // NOTE: this will match ResultLimit on server method - [Query(ResultLimit = ???)] public IEnumerable<PatientSearch> GetPatientForSearch(...)        

        public string MaxRecordsDisplayText =>
            $"The search yields results in excess of {MAX_RECORDS} records.  Please search using additional criteria to decrease the result set.";

        private bool _IsSystemSearch = true;

        public bool IsSystemSearch
        {
            get { return _IsSystemSearch; }
            set
            {
                _IsSystemSearch = value;
                RaisePropertyChanged("IsSystemSearch");
            }
        }

        private List<SearchParameter> lastSearchParameters;

        public void Search(bool isSystemSearch, List<SearchParameter> parameters)
        {
            IsSystemSearch = isSystemSearch;
            lastSearchParameters = parameters;
            Model.SearchParameters.Clear();
            parameters.ForEach(p => Model.SearchParameters.Add(p));

            Model.GetSearchAsync(isSystemSearch);
        }

        private CollectionViewSource _PatientsForSearch = new CollectionViewSource();

        public CollectionViewSource PatientsForSearch
        {
            get { return _PatientsForSearch; }
            set
            {
                _PatientsForSearch = value;
                RaisePropertyChanged("PatientsForSearch");
            }
        }

        public RelayCommand<PatientSearch> SelectCommand { get; protected set; }

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

        #region "Patient Search Commands"

        //  Command Definitions

        public RelayCommand<PatientSearch> OpenPatientAdmissionCommand { get; protected set; }
        public RelayCommand<PatientSearch> OpenPatientDashboardCommand { get; protected set; }
        public RelayCommand<PatientSearch> OpenPatientMessageCommand { get; protected set; }
        public RelayCommand<PatientSearch> OpenPatientProfileCommand { get; protected set; }

        //  Command Actions       

        private void SetupCommands()
        {
            OpenPatientAdmissionCommand = new RelayCommand<PatientSearch>(OpenPatientAdmission,
                item => RoleAccessHelper.CheckPermission(RoleAccess.Patient, false));
            OpenPatientMessageCommand = new RelayCommand<PatientSearch>(OpenPatientMessage,
                item => RoleAccessHelper.CheckPermission(RoleAccess.Patient, false));
            OpenPatientProfileCommand = new RelayCommand<PatientSearch>(OpenPatientProfile,
                item => RoleAccessHelper.CheckPermission(RoleAccess.Patient, false));
            OpenPatientDashboardCommand = new RelayCommand<PatientSearch>(OpenPatientDashboard,
                item => RoleAccessHelper.CheckPermission(RoleAccess.Patient, false));

            SelectCommand = new RelayCommand<PatientSearch>(SelectCommandImpl);
        }

        private void SelectCommandImpl(PatientSearch p)
        {
            if (SelectAction != null)
            {
                SelectAction();
            }
        }

        private PatientSearch CurrentSearchResult
        {
            get
            {
                var currentItem = ((PatientSearch)(PatientsForSearch.View).CurrentItem);
                return currentItem;
            }
        }

        private void NavigateToDetails(int patientId, int? admissionId, string detailType)
        {
            string uri = "";

            bool hasPermission = false;
            switch (detailType)
            {
                case "Admission":
                    if (patientId > 0)
                    {
                        hasPermission = RoleAccessHelper.CheckPermission(RoleAccess.Patient, false);

                        if (admissionId == null || admissionId <= 0)
                        {
                            uri = NavigationUriBuilder.Instance.GetAdmissionMaintenanceURI(1, patientId, 0);
                        }
                        else
                        {
                            uri = NavigationUriBuilder.Instance.GetAdmissionMaintenanceURI(1, patientId,
                                admissionId.GetValueOrDefault());
                        }
                    }

                    break;

                case "Patient":

                    if (patientId > 0)
                    {
                        hasPermission = RoleAccessHelper.CheckPermission(RoleAccess.Patient, false);

                        uri = NavigationUriBuilder.Instance.GetPatientMaintenanceURI(0, patientId);
                    }

                    break;
            }

            if (hasPermission)
            {
                if (SelectAction != null)
                {
                    //Send the navigation request to SearchDialog, which controls closing the host dialog after a navigation request
                    //This is to prevent a "double" navigation scenario where some end users where clicking the house icon and then 
                    //immediately clicking the Select button.
                    //Clicking the house icon - launches admission maintenance
                    //Clicking the Select button - launches patient maintenance
                    Messenger.Default.Send(new Uri(uri, UriKind.Relative),
                        Constants.Application.SearchDialogNavigationRequest);
                }
            }
        }

        public void OpenPatientDashboard(PatientSearch ps)
        {
            string uri = string.Format("/Dashboard/{0}/{1}/{2}", ps.PatientKey, ps.AdmissionKey, 0);
            Messenger.Default.Send(new Uri(uri, UriKind.Relative), Constants.Application.SearchDialogNavigationRequest);
        }

        public void OpenPatientAdmission(PatientSearch ps)
        {
            NavigateToDetails(ps.PatientKey, ps.AdmissionKey, "Admission");
        }

        public void OpenPatientMessage(PatientSearch ps)
        {
            if ((ps == null) || (Model == null))
            {
                return;
            }

            IsBusy = true;
            Model.Context.Patients.Clear();
            Model.Context.PatientMessages.Clear();
            Model.OnPatientWithPatientMessagesLoaded += model_OnPatientWithPatientMessagesLoaded;
            Model.GetPatientWithPatientMessagesAsync(ps.PatientKey);
        }

        private void model_OnPatientWithPatientMessagesLoaded(object sender, EntityEventArgs<Patient> e)
        {
            IsBusy = false;
            if (e.Error != null)
            {
                ErrorDetailLogger.LogDetails(e, true,
                    "Virtuoso.Core.Search.PatientSearchViewModel.model_OnEVVImplementationLoaded");
                return;
            }

            if (Model != null)
            {
                Model.OnPatientWithPatientMessagesLoaded -= model_OnPatientWithPatientMessagesLoaded;
            }

            Patient p = ((Model == null) || (Model.Context == null) || (Model.Context.Patients == null))
                ? null
                : Model.Context.Patients.FirstOrDefault();
            if (p == null)
            {
                return;
            }

            PatientMessageDialog pmd = new PatientMessageDialog(p.FullNameInformal, p.PatientMessages);
            pmd.Show();
        }

        public void OpenPatientProfile(PatientSearch ps)
        {
            NavigateToDetails(ps.PatientKey, ps.AdmissionKey, "Patient");
        }

        #endregion

        void OnSearchLoaded(object sender, EntityEventArgs<PatientSearch> e)
        {
            if (e.Error == null)
            {
                PatientsForSearch.Source = null;
                totalRecords = e.TotalEntityCount;

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
                    PatientsForSearch.Source = e.Results;
                }
                
                this.RaisePropertyChangedLambda(p => p.PatientsForSearch);

                this.RaisePropertyChangedLambda(p => p.MAX_RECORDS);
                this.RaisePropertyChangedLambda(p => p.MaxRecordsDisplayText);
                this.RaisePropertyChangedLambda(p => p.TotalRecords);
                this.RaisePropertyChangedLambda(p => p.NoResults);
                this.RaisePropertyChangedLambda(p => p.TooManyResults);
            }
            else
            {
                ErrorDetailLogger.LogDetails(e, true, "Virtuoso.Core.Search.PatientSearchViewModel.OnSearchLoaded");
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

        public bool NoResults { get; set; }
        public bool TooManyResults { get; set; }

        int totalRecords;
        public int TotalRecords => totalRecords;

        public string SelectedField => "PatientKey";

        public string SelectedValue
        {
            get
            {
                if (SelectedItem == null)
                {
                    return String.Empty;
                }

                return ((PatientSearch)SelectedItem).PatientKey.ToString();
            }
        }

        public void ClearResults()
        {
            SelectedItem = null;
            PatientsForSearch.Source = null;

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