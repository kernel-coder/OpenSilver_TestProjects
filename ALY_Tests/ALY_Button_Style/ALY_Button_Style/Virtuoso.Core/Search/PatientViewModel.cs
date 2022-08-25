using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Events;
using Virtuoso.Core.Services;
using Virtuoso.Core.View;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Core.Utility;

namespace Virtuoso.Core.Search
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("FieldName", "PatientSearch")]
    [Export(typeof(ISearch))]
    public class PatientViewModel : GenericBase, ISearch
    {
        private List<SearchParameter> lastSearchParameters = null;
        public IPatientService Model { get; set; }

        public void Search(List<SearchParameter> parameters)
        {
            lastSearchParameters = parameters;
            Model.SearchParameters.Clear();
            parameters.ForEach(p => Model.SearchParameters.Add(p));
            Model.GetSearchAsync();
        }

        [ImportingConstructor]
        public PatientViewModel(IPatientService _model)
        {
            SetupCommands();

            Model = _model;
            Model.OnSearchLoaded += new EventHandler<EntityEventArgs<PatientSearch>>(OnSearchLoaded);
            Model.OnPatientRefreshLoaded += new EventHandler<EntityEventArgs<Patient>>(PatientModel_OnPatientRefreshLoaded);
            Model.OnPatientAdmissionRefreshLoaded += new EventHandler<EntityEventArgs<Admission>>(PatientModel_OnAdmissionRefreshLoaded);

        }

        //private CollectionViewSource _Patients = new CollectionViewSource();
        //public CollectionViewSource Patients
        //{
        //    get { return _Patients; }
        //    set { _Patients = value; this.RaisePropertyChanged("Patients"); }
        //}



        void PatientModel_OnAdmissionRefreshLoaded(object sender, EntityEventArgs<Admission> e)
        {
            if (e.Error != null)
            {
                if (e.EntityErrors.Count == 0)
                    MessageBox.Show(e.Error.Message); //show general error
                else
                {
                    //show detailed errors
                    foreach (var err in e.EntityErrors)
                    {
                        MessageBox.Show(err);
                    }
                }
                this.IsBusy = false;
            }
            else
            {
                var admissions = (IEnumerable<Admission>)e.Results;
                if (admissions.Any())
                {
                    var currentAdmission = admissions.FirstOrDefault(a => a.AdmissionKey == CurrentSearchResult.AdmissionKey);
                    if (currentAdmission != null)
                    {
                        var patientAdmissionInfoWindow = new PatientAdmissionInfoWindow(currentAdmission.Patient, currentAdmission);
                        patientInfoWindow.Show();
                    }
                }
                else
                {
                    Model.RefreshPatientAsync(CurrentSearchResult.PatientKey);
                }

            }
        }

        private void PatientModel_OnPatientRefreshLoaded(object sender, EntityEventArgs<Patient> e)
        {

            if (e.Error != null)
            {
                if (e.EntityErrors.Count == 0)
                    MessageBox.Show(e.Error.Message); //show general error
                else
                {
                    //show detailed errors
                    foreach (var err in e.EntityErrors)
                    {
                        MessageBox.Show(err);
                    }
                }
                this.IsBusy = false;
            }
            else
            {
                //Speed this up by loading patient when row is selected
                var patients = (IEnumerable<Patient>)e.Results;
                var patient = patients.Where(p => p.PatientKey == CurrentSearchResult.PatientKey).FirstOrDefault();

                if (patient != null)
                {
                    Admission admission = null;

                    var admissionKey = CurrentSearchResult.AdmissionKey;
                    if (admissionKey != null)
                    {
                        admission = patient.Admission.Where(a => a.AdmissionKey == admissionKey).FirstOrDefault();

                    }
                    //var patientInfoWindow = new PatientAdmissionDetailsDialog(patient, admission);
                    var patientInfoWindow = new PatientAdmissionInfoWindow(patient, admission);
                   // var patientInfoWindow = new PatientInfoWindow();
                    patientInfoWindow.Show();
                }
            }







        }



        #region "Patient Search Commands"

        //  Command Definitions

        public RelayCommand<PatientSearch> ViewPatientProfileCommand { get; protected set; }
        public RelayCommand<PatientSearch> OpenPatientAdmissionCommand { get; protected set; }
        public RelayCommand<PatientSearch> OpenPatientProfileCommand { get; protected set; }


        //  Command Actions       

        private void SetupCommands()
        {

            OpenPatientAdmissionCommand = new RelayCommand<PatientSearch>(OpenPatientAdmission, item => RoleAccessHelper.CheckPermission(RoleAccess.Patient, false));
            OpenPatientProfileCommand = new RelayCommand<PatientSearch>(OpenPatientProfile, item => RoleAccessHelper.CheckPermission(RoleAccess.Patient, false));
            ViewPatientProfileCommand = new RelayCommand<PatientSearch>(ViewPatientProfile, item => RoleAccessHelper.CheckPermission(RoleAccess.Patient, false));

            SelectCommand = new RelayCommand<PatientSearch>((p) =>
            {

                if (SelectAction != null)
                {
                    SelectAction();
                }
            }); //, () =>
            //{
            //    return ((SelectedSearch != null) && String.IsNullOrEmpty(SelectedSearch.SearchResultsViewModel.SelectedValue) == false);
            //});

        }

        private PatientSearch CurrentSearchResult
        {
            get
            {
                var currentItem = ((PatientSearch)(this.PatientsForSearch.View).CurrentItem);
                return currentItem;                
            }
        }

        private void NavigateToDetails(string detailType)
        {
            string uri = "";

            bool hasPermission = false;
            switch (detailType)
            {
                case "Admission":
                    if (CurrentSearchResult.PatientKey > 0)
                    {
                        hasPermission = RoleAccessHelper.CheckPermission(RoleAccess.Patient, false);

                        if (CurrentSearchResult.AdmissionKey == null || CurrentSearchResult.AdmissionKey <= 0)
                        {
                            uri = string.Format("/MaintenanceAdmission/1/{0}/{1}", CurrentSearchResult.PatientKey, "0");
                        }
                        else
                        {
                            uri = string.Format("/MaintenanceAdmission/1/{0}/{1}", CurrentSearchResult.PatientKey, CurrentSearchResult.AdmissionKey);
                        }

                    }
                    break;

                case "Patient":

                    if (CurrentSearchResult.PatientKey > 0)
                    {
                        hasPermission = RoleAccessHelper.CheckPermission(RoleAccess.Patient, false);

                        //Uri="/MaintenancePatient/{tab}/{patient}/{admission}"
                        uri = string.Format("/Virtuoso.Maintenance;component/Views/PatientList.xaml?tab=0&patient={0}", CurrentSearchResult.PatientKey);
                    }
                    break;
            }

            if (hasPermission)
            {
                if (SelectAction != null)
                    SelectAction();
                Messenger.Default.Send<Uri>(new Uri(uri, UriKind.Relative), "NavigationRequest");
            }



        }

        public void ViewPatientProfile(PatientSearch ps)
        {
            Model.RefreshPatientAdmissionsAsync(CurrentSearchResult.PatientKey);
        }

        public void OpenPatientAdmission(PatientSearch patientSearch)
        {
            NavigateToDetails("Admission");
        }

        public void OpenPatientProfile(PatientSearch patientSearch)
        {
            NavigateToDetails("Patient");
        }

        #endregion


        private CollectionViewSource _PatientsForSearch = new CollectionViewSource();
        public CollectionViewSource PatientsForSearch
        {
            get { return _PatientsForSearch; }
            set { _PatientsForSearch = value; this.RaisePropertyChanged("PatientsForSearch"); }
        }

        public RelayCommand<PatientSearch> SelectCommand
        {
            get;
            protected set;
        }

        private Object _SelectedItem = null;
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
                        OnItemSelected();
                }
            }
        }

        public bool IsBusy
        {
            get
            {
                return this.Model.IsLoading;
            }
            set
            {
                if (Model.IsLoading != value)
                {
                    Model.IsLoading = value;
                    this.RaisePropertyChangedLambda(p => p.IsBusy);
                }
            }
        }

        void OnSearchLoaded(object sender, EntityEventArgs<PatientSearch> e)
        {
            if (e.Error == null)
            {
                PatientsForSearch.Source = null;
                totalReccords = e.TotalEntityCount; // patSearchList.Count;

                if (totalReccords == 0)
                {
                    NoResults = true;
                    TooManyResults = false;
                }
                else if (totalReccords > 1000)
                {
                    TooManyResults = true;
                    NoResults = false;
                    totalReccords = 0;  //reset to no results...
                }
                else
                    PatientsForSearch.Source = e.Results.ToList(); //patSearchList;

                //this.RaisePropertyChangedLambda(p => p.Patients);
                this.RaisePropertyChangedLambda(p => p.PatientsForSearch);
                this.RaisePropertyChangedLambda(p => p.TotalRecords);
                this.RaisePropertyChangedLambda(p => p.NoResults);
                this.RaisePropertyChangedLambda(p => p.TooManyResults);
            }
            else
            {
                if (e.EntityErrors.Count == 0)
                    MessageBox.Show(e.Error.Message);
                else
                {
                    foreach (var err in e.EntityErrors)
                    {
                        MessageBox.Show(err);
                    }
                }
            }

            this.IsBusy = false;
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean own resources if needed

        ////    base.Cleanup();
        ////}

        #region ISearch Members

        private Action _OnItemSelected;
        public Action OnItemSelected
        {
            get { return this._OnItemSelected; }
            set { _OnItemSelected = value; }
        }

        private Action _SearchAction;
        public Action SelectAction
        {
            get { return this._SearchAction; }
            set { _SearchAction = value; }
        }

        public bool NoResults { get; set; }
        public bool TooManyResults { get; set; }

        int totalReccords = 0;
        public int TotalRecords
        {
            //get { return Model.Patients.Count; }
            get { return totalReccords; }
        }

        public string SelectedField
        {
            get { return "PatientKey"; }
        }

        public string SelectedValue
        {
            get
            {
                if (SelectedItem == null)
                    return String.Empty;
                else
                    return ((PatientSearch)SelectedItem).PatientKey.ToString();
            }
        }

        public void ClearResults()
        {
            this.SelectedItem = null;
            PatientsForSearch.Source = null;

            Model.Clear();

            totalReccords = 0;
            NoResults = false;
            TooManyResults = false;
            this.RaisePropertyChangedLambda(p => p.TotalRecords);
            this.RaisePropertyChangedLambda(p => p.NoResults);
            this.RaisePropertyChangedLambda(p => p.TooManyResults);
            this.RaisePropertyChangedLambda(p => p.SelectedValue);
        }

        #endregion
    }

}