#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Events;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.View;
using Virtuoso.Helpers;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.ViewModel
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export]
    public class PatientAdmissionInfoViewModel : GalaSoft.MvvmLight.ViewModelBase
    {
        #region Constructors

        [ImportingConstructor]
        public PatientAdmissionInfoViewModel(int patientId, int? admissionId, IPatientService model)
        {
            BusyMessage = DefaultBusyMessage;
            SetupCommands();
            CommandManager = new CommandManager(this);

            SetupCommands();

            SearchPatientID = patientId;
            SearchAdmissionID = admissionId;
            Model = model;

            Model.OnPatientAdmissionFullDetailsLoaded += PatientModel_OnPatientAdmissionFullDetailsLoaded;
        }

        #endregion

        #region Properties

        public IPatientService Model { get; set; }
        public int SearchPatientID { get; set; }
        public int? SearchAdmissionID { get; set; }
        
        protected CommandManager CommandManager { get; set; }
        public PatientInfo PatientInfo { get; set; }

        private readonly string DefaultBusyMessage = "Retrieving patient information...";

        private string _busyMessage;

        public string BusyMessage
        {
            get { return _busyMessage; }
            set
            {
                _busyMessage = value;
                RaisePropertyChanged("BusyMessage");
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                RaisePropertyChanged("IsBusy");
            }
        }

        bool? _dialogResult;

        public bool? DialogResult
        {
            get { return _dialogResult; }
            set
            {
                _dialogResult = value;
                RaisePropertyChanged("DialogResult");
            }
        }

        #endregion

        #region "Commands"

        public RelayCommand CloseCommand { get; protected set; }
        public RelayCommand LoadedCommand { get; set; }
        public RelayCommand PhysicianDetailsCommand { get; set; }

        #endregion

        #region "Bindable Data Objects"

        public string WindowTitle
        {
            get
            {
                string windowTitle = "";
                if (CurrentPatient != null)
                {
                    windowTitle = CurrentPatient.FullNameInformal + " - " + CurrentPatient.MRNDescription;
                }

                return windowTitle;
            }
        }

        private Patient _currentPatient;

        public Patient CurrentPatient
        {
            get { return _currentPatient; }
            set
            {
                _currentPatient = value;
                RaisePropertyChanged("ActivePatientAddresses");
                RaisePropertyChanged("CurrentPatient");
            }
        }

        private Admission _currentAdmission;

        public Admission CurrentAdmission
        {
            get { return _currentAdmission; }
            set
            {
                _currentAdmission = value;
                RaisePropertyChanged("CurrentAdmission");
            }
        }

        private List<PatientAddress> _ActivePatientAddresses;

        public List<PatientAddress> ActivePatientAddresses
        {
            get { return _ActivePatientAddresses; }
            set
            {
                _ActivePatientAddresses = value;
                RaisePropertyChanged("ActivePatientAddresses");
            }
        }

        private List<PatientAddress> GetActivePatientAddresses()
        {
            if (CurrentPatient == null || CurrentPatient.PatientAddress == null)
            {
                return null;
            }

            return CurrentPatient.GetActivePatientAddresses(DateTime.Now.Date);
        }

        public string AdmissionPhysicianNameAndPhone(AdmissionPhysician admissionPhysician)
        {
            if ((admissionPhysician == null) || (string.IsNullOrWhiteSpace(admissionPhysician.FullName)))
            {
                return "None";
            }

            string nameAndPhone = admissionPhysician.FullName;
            if (string.IsNullOrEmpty(admissionPhysician.AddressPhoneNumber) == false)
            {
                nameAndPhone += "    " + admissionPhysician.AddressPhoneNumber;
            }

            return nameAndPhone;
        }


        public string CurrentAdmissionPhysicianDetails => AdmissionPhysicianNameAndPhone(CurrentAdmissionPhysician);
        private string _admissionPhysicianLabel;

        public string AdmissionPhysicianLabel
        {
            get { return _admissionPhysicianLabel; }
            set
            {
                _admissionPhysicianLabel = value;
                RaisePropertyChanged("AdmissionPhysicianLabel");
            }
        }

        public AdmissionPhysician CurrentAdmissionPhysician
        {
            get
            {
                AdmissionPhysician currentAdmissionPhysician = null;
                if (CurrentAdmission != null)
                {
                    var physicianFacade = new AdmissionPhysicianFacade(false);

                    physicianFacade.Admission = CurrentAdmission;

                    currentAdmissionPhysician = AdmissionPhysicianByHierarchy(physicianFacade);
                }

                return currentAdmissionPhysician;
            }
        }

        public AdmissionPhysician AdmissionPhysicianByHierarchy(AdmissionPhysicianFacade physicianFacade)
        {
            {
                AdmissionPhysician selectedAdmissioPhysician = null;

                if (physicianFacade.CurrentCertifyingAdmissionPhysician != null)
                {
                    selectedAdmissioPhysician = physicianFacade.CurrentCertifyingAdmissionPhysician;
                    AdmissionPhysicianLabel = "Signing Physician:";
                }
                else if (physicianFacade.SigningAdmissionPhysician != null)
                {
                    selectedAdmissioPhysician = physicianFacade.SigningAdmissionPhysician;
                    AdmissionPhysicianLabel = "Signing Physician:";
                }
                else if (physicianFacade.AttendingAdmissionPhysician != null)
                {
                    selectedAdmissioPhysician = physicianFacade.AttendingAdmissionPhysician;
                    AdmissionPhysicianLabel = "Attending Physician:";
                }
                else
                {
                    AdmissionPhysicianLabel = "Physician:";
                }

                return selectedAdmissioPhysician;
            }
        }

        public UserProfile Coordinator
        {
            get
            {
                var coordinator = new UserProfile();
                if (CurrentAdmission != null)
                {
                    coordinator = UserCache.Current.GetUserProfileFromUserId(CurrentAdmission.CareCoordinator);
                }

                return coordinator;
            }
        }

        public UserProfilePhone CoordinatorPhone
        {
            get
            {
                var coordinatorPhone = new UserProfilePhone();

                if (Coordinator != null)
                {
                    coordinatorPhone = Coordinator.UserProfilePhone.FirstOrDefault();
                }

                return coordinatorPhone;
            }
        }

        #endregion

        #region Methods

        public void LoadPatientAdmissionData()
        {
            DialogResult = null;
            Model.GetPatientAdmissionFullDetailsAsync(SearchPatientID, SearchAdmissionID);
        }

        private void PatientModel_OnPatientAdmissionFullDetailsLoaded(object sender, EntityEventArgs<Patient> e)
        {
            if (e.Error != null)
            {
                ErrorDetailLogger.LogDetails(e, true, "Virtuoso.Core.PatientAdmissionInfoViewModel.OnLoaded");
            }
            else
            {
                var patients = e.Results;
                var patient = patients.FirstOrDefault(p => p.PatientKey == SearchPatientID);

                if (patient != null)
                {
                    CurrentPatient = patient;
                    CurrentAdmission = patient.Admission.FirstOrDefault(a => a.AdmissionKey == SearchAdmissionID);
                }

                ActivePatientAddresses = GetActivePatientAddresses();
            }

            RaisePropertyChanged("WindowTitle");
            RaisePropertyChanged("PatientAdmissions");
            RaisePropertyChanged("CurrentAdmissionPhysicianDetails");
            RaisePropertyChanged("CurrentAdmissionPhysician");
            RaisePropertyChanged("Coordinator");
            RaisePropertyChanged("CoordinatorPhone");

            IsBusy = false;
        }

        private void SetupCommands()
        {
            LoadedCommand = new RelayCommand(() =>
            {
                //NOTE: application creates one instance of this window - it is hidden and then shown subsequently, so you can use
                //      the loaded event to know that the end user is viewing the dialog...
                if (IsBusy)
                {
                    return;
                }

                IsBusy = true;
                LoadPatientAdmissionData();
            }, () => true);

            CloseCommand = new RelayCommand(CloseDialog, () => true);
            PhysicianDetailsCommand = new RelayCommand(() =>
            {
                if (CurrentAdmissionPhysician == null)
                {
                    return;
                }

                PhysicianDisplay pd = new PhysicianDisplay
                {
                    Physician = CurrentAdmissionPhysician.PhysicianProxy, AdmissionPhysician = CurrentAdmissionPhysician
                };
                PhysicianDetailsDialog d = new PhysicianDetailsDialog(pd);
                d.Show();
            });
        }

        private void CloseDialog()
        {
            if (IsBusy)
            {
                return;
            }

            DialogResult = true;
        }

        public override void Cleanup()
        {
            CommandManager.CleanUp();
            base.Cleanup();
        }

        #endregion
    }
}