#region Usings

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class HealthCareFacility : QuestionBase, INotifyDataErrorInfo
    {
        public override void Cleanup()
        {
            Messenger.Default.Unregister<int>(this, "PatientFacilityStayRefreshed");
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }

        public HealthCareFacility(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public EncounterTransfer EncounterTransfer =>
            (DynamicFormViewModel == null) ? null : DynamicFormViewModel.CurrentEncounterTransfer;

        public List<PatientFacilityStay> PatientFacilityStayList
        {
            get
            {
                if ((Patient == null) || (Patient.PatientFacilityStay == null) ||
                    (Patient.PatientFacilityStay.Any() == false))
                {
                    return null;
                }

                int myKey = (EncounterTransfer == null)
                    ? 0
                    : ((EncounterTransfer.PatientFacilityStayKey == null)
                        ? 0
                        : (int)EncounterTransfer.PatientFacilityStayKey);
                return Patient.PatientFacilityStay
                    .Where(pfs =>
                        (((pfs.HistoryKey == null) && (pfs.EndDate == null)) || (pfs.PatientFacilityStayKey == myKey)))
                    .OrderByDescending(pfs => pfs.StartDate).ToList();
            }
        }

        public PatientFacilityStay PatientFacilityStay
        {
            get
            {
                if ((Patient == null) || (Patient.PatientFacilityStay == null) ||
                    (Patient.PatientFacilityStay.Any() == false))
                {
                    return null;
                }

                if ((EncounterTransfer == null) || (EncounterTransfer.PatientFacilityStayKey <= 0))
                {
                    return null;
                }

                return Patient.PatientFacilityStay
                    .Where(pfs => pfs.PatientFacilityStayKey == EncounterTransfer.PatientFacilityStayKey)
                    .FirstOrDefault();
            }
        }

        public void SetupHealthCareFacility()
        {
            Messenger.Default.Register<int>(this, "PatientFacilityStayRefreshed",
                PatientKey => { OnPatientFacilityStayChanged(PatientKey); });
        }

        private void OnPatientFacilityStayChanged(int PatientKey)
        {
            if ((Patient == null) || (Patient.PatientKey != PatientKey))
            {
                return;
            }

            if (Encounter == null)
            {
                return;
            }

            PatientFacilityStay pfs = null;
            if (Encounter.EncounterIsInEdit == false)
            {
                // Refresh on possible data changes within the facility stay
                int key = ((EncounterTransfer == null) || (EncounterTransfer.PatientFacilityStayKey == null))
                    ? 0
                    : (int)EncounterTransfer.PatientFacilityStayKey;
                pfs = (PatientFacilityStayList == null)
                    ? null
                    : PatientFacilityStayList.Where(p => p.PatientFacilityStayKey == key).FirstOrDefault();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    RaisePropertyChanged("PatientFacilityStay");
                    RaisePropertyChanged("PatientFacilityStayList");
                    if (EncounterTransfer != null)
                    {
                        EncounterTransfer.RaisePropertyChangedPatientFacilityStayKey();
                    }

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (pfs != null)
                        {
                            pfs.RaisePropertyChangedPatientFacilityStay();
                        }
                    });
                });
                return;
            }

            int? saveKey = EncounterTransfer?.PatientFacilityStayKey;
            pfs = (PatientFacilityStayList == null)
                ? null
                : PatientFacilityStayList.FirstOrDefault(p => p.PatientFacilityStayKey == saveKey);
            if (pfs == null)
            {
                saveKey = null;
            }

            if ((pfs != null) && (pfs.EndDate != null))
            {
                saveKey = null;
            }

            if (EncounterTransfer != null)
            {
                EncounterTransfer.PatientFacilityStayKey = saveKey;
            }

            pfs = (PatientFacilityStayList == null)
                ? null
                : PatientFacilityStayList.Where(p => p.PatientFacilityStayKey == saveKey).FirstOrDefault();
            if ((pfs == null) && (EncounterTransfer != null))
            {
                EncounterTransfer.PatientFacilityStayKey = null;
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                RaisePropertyChanged("PatientFacilityStay");
                RaisePropertyChanged("PatientFacilityStayList");
                if (EncounterTransfer != null)
                {
                    EncounterTransfer.RaisePropertyChangedPatientFacilityStayKey();
                }

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (pfs != null)
                    {
                        pfs.RaisePropertyChangedPatientFacilityStay();
                    }
                });
            });
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;

            if ((Encounter != null && Encounter.FullValidation && EncounterTransfer != null))
            {
                if ((EncounterTransfer.PatientFacilityStayKey != null) &&
                    (EncounterTransfer.PatientFacilityStayKey <= 0))
                {
                    EncounterTransfer.PatientFacilityStayKey = null;
                }

                if ((EncounterTransfer.PatientFacilityStayKey == null) && (Required))
                {
                    DynamicFormViewModel.CurrentEncounterTransfer.ValidationErrors.Add(
                        new ValidationResult("Health Care Facility field is required",
                            new[] { "PatientFacilityStayKey" }));
                    AllValid = false;
                }
            }

            return AllValid;
        }
    }

    public class HealthCareFacilityFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HealthCareFacility hcf = new HealthCareFacility(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
            };
            hcf.SetupHealthCareFacility();
            return hcf;
        }
    }
}