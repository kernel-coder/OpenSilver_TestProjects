#region Usings

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class AdvanceCarePlan : QuestionUI, INotifyPropertyChanged
    {
        public AdvanceCarePlan(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private Virtuoso.Server.Data.AdvanceCarePlan _AdvanceCarePlan;

        public Virtuoso.Server.Data.AdvanceCarePlan AdvanceCarePlanData
        {
            get { return _AdvanceCarePlan; }
            set
            {
                _AdvanceCarePlan = value;
                RaisePropertyChanged("AdvanceCarePlanData");
            }
        }

        private bool isDischargeForm
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsDischarge == false))
                {
                    return false;
                }

                return true;
            }
        }

        private bool isTransferForm
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsTransfer == false))
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsDischargeOrTransfer => (isDischargeForm || isTransferForm);

        public bool? SetupAdvanceCarePlanHiddenOverride()
        {
            // Hide in hospice discharge and transfer forms 
            if ((Admission != null) && Admission.HospiceAdmission && IsDischargeOrTransfer)
            {
                return true;
            }

            return null;
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;
            if (AdvanceCarePlanData != null)
            {
                AdvanceCarePlanData.ValidationErrors.Clear();
            }

            if (Hidden)
            {
                return AllValid;
            }

            // Always add the AdvanceCarePlan to the context if it isn't already attached. This will allow partial saves without having to pass validation
            if (AdvanceCarePlanData.IsNew)
            {
                AdvanceCarePlanData.Patient = Patient;
                AdvanceCarePlanData.Encounter = Encounter; // insure EncounterKey is set, prevent FK exception
                Encounter.AdvanceCarePlan.Add(AdvanceCarePlanData);
            }

            ResetHiddenFields();

            if (!Encounter.FullValidation)
            {
                return AllValid;
            }

            // These fields are always required
            if (AdvanceCarePlanData.HasAdvanceDirective.HasValue == false)
            {
                AllValid = false;
                AdvanceCarePlanData.ValidationErrors.Add(new ValidationResult(
                    "Does the patient have a living will or advance directive is required.",
                    new[] { "HasAdvanceDirective" }));
            }

            if (AdvanceCarePlanData.HasAdvanceDirective.HasValue &&
                !AdvanceCarePlanData.HasAdvanceDirective.Value &&
                AdvanceCarePlanData.DNROrders.HasValue &&
                AdvanceCarePlanData.DNROrders.Value)
            {
                AllValid = false;
                AdvanceCarePlanData.ValidationErrors.Add(new ValidationResult(
                    "The patient must have an advance directive when DNR Orders is 'Yes'.",
                    new[] { "HasAdvanceDirective" }));
            }

            if (AdvanceCarePlanData.DNROrders.HasValue == false)
            {
                AllValid = false;
                AdvanceCarePlanData.ValidationErrors.Add(new ValidationResult("DNR Orders is required.",
                    new[] { "DNROrders" }));
            }

            if (AdvanceCarePlanData.HasPowerOfAttorney.HasValue == false)
            {
                AllValid = false;
                AdvanceCarePlanData.ValidationErrors.Add(new ValidationResult(
                    "Does the Patient have a health care proxy or medical durable power of attorney is required.",
                    new[] { "HasPowerOfAttorney" }));
            }

            if (AdvanceCarePlanData.HasAdvanceDirective.HasValue && AdvanceCarePlanData.HasAdvanceDirective.Value)
            {
                // These fields must have a value when HasAdvanceDirective is checked
                if (AdvanceCarePlanData.MedicalTreatmentPreference.HasValue == false)
                {
                    AllValid = false;
                    AdvanceCarePlanData.ValidationErrors.Add(new ValidationResult(
                        "Medical Treatment Preferences is required.", new[] { "MedicalTreatmentPreference" }));
                }

                if (AdvanceCarePlanData.MentalHealthTreatmentPreference.HasValue == false)
                {
                    AllValid = false;
                    AdvanceCarePlanData.ValidationErrors.Add(new ValidationResult(
                        "Mental Health / Behavioral Treatment Preferences is required.",
                        new[] { "MentalHealthTreatmentPreference" }));
                }

                if (AdvanceCarePlanData.CulturalPreference.HasValue == false)
                {
                    AllValid = false;
                    AdvanceCarePlanData.ValidationErrors.Add(
                        new ValidationResult("Cultural / Social Preferences is required.",
                            new[] { "CulturalPreference" }));
                }

                if (AdvanceCarePlanData.SpiritualPreference.HasValue == false)
                {
                    AllValid = false;
                    AdvanceCarePlanData.ValidationErrors.Add(new ValidationResult(
                        "Spiritual / Religious Preferences is required.", new[] { "SpiritualPreference" }));
                }
            }
            else if (AdvanceCarePlanData.HasAdvanceDirective.HasValue &&
                     AdvanceCarePlanData.HasAdvanceDirective.Value == false)
            {
                // These fields must have a value when HasAdvanceDirective is not checked
                if (AdvanceCarePlanData.ReasonForNoAdvanceDirective.HasValue == false)
                {
                    AllValid = false;
                    AdvanceCarePlanData.ValidationErrors.Add(new ValidationResult(
                        "Reason for No Advance Directives is required.", new[] { "ReasonForNoAdvanceDirective" }));
                }
            }

            if (AdvanceCarePlanData.DateReceived.HasValue == false)
            {
                AllValid = false;
                AdvanceCarePlanData.ValidationErrors.Add(new ValidationResult("Date Recorded is required.",
                    new[] { "DateReceived" }));
            }
            else if (AdvanceCarePlanData.DateReceived.HasValue)
            {
                AdvanceCarePlanData.DateReceived = ((DateTime)AdvanceCarePlanData.DateReceived).Date;
                if (AdvanceCarePlanData.DateReceived > DateTime.Today.Date)
                {
                    AllValid = false;
                    AdvanceCarePlanData.ValidationErrors.Add(
                        new ValidationResult("Date Recorded cannot be in the future.", new[] { "DateReceived" }));
                }
            }

            if (AdvanceCarePlanData.HasPowerOfAttorney.HasValue && !AdvanceCarePlanData.HasPowerOfAttorney.Value)
            {
                if (AdvanceCarePlanData.ReasonForNoSurrogate.HasValue == false)
                {
                    AllValid = false;
                    AdvanceCarePlanData.ValidationErrors.Add(new ValidationResult(
                        "Reason for No Surrogate Decision Makers is required.", new[] { "ReasonForNoSurrogate" }));
                }
            }

            if (!AllValid)
            {
                DynamicFormViewModel.ValidEnoughToSave = false;
            }

            return AllValid;
        }

        private void ResetHiddenFields()
        {
            // If the user answers true for the AdvanceDirective question has a value, assure the reasons for not having are unanswered
            if (AdvanceCarePlanData.HasAdvanceDirective.HasValue && AdvanceCarePlanData.HasAdvanceDirective.Value)
            {
                AdvanceCarePlanData.ReasonForNoAdvanceDirective = null;
            }
            else if (AdvanceCarePlanData.HasAdvanceDirective.HasValue && !AdvanceCarePlanData.HasAdvanceDirective.Value)
            {
                AdvanceCarePlanData.MedicalTreatmentPreference = null;
                AdvanceCarePlanData.MentalHealthTreatmentPreference = null;
                AdvanceCarePlanData.CulturalPreference = null;
                AdvanceCarePlanData.SpiritualPreference = null;
            }

            if (AdvanceCarePlanData.HasPowerOfAttorney.HasValue && AdvanceCarePlanData.HasPowerOfAttorney.Value)
            {
                AdvanceCarePlanData.ReasonForNoSurrogate = null;
            }
        }
    }

    public class AdvanceCarePlanFactory
    {
        public static AdvanceCarePlan Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            AdvanceCarePlan qb = new AdvanceCarePlan(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission
            };
            qb.AdvanceCarePlanData = vm.CurrentPatient.AdvanceCarePlan.Where(a => a.HistoryKey == null)
                .OrderByDescending(a => a.AdvanceCarePlanKey).FirstOrDefault();
            if (qb.AdvanceCarePlanData == null)
            {
                qb.AdvanceCarePlanData = new Server.Data.AdvanceCarePlan
                {
                    AddedFromEncounterKey = qb.Encounter.EncounterKey, PatientKey = qb.Patient.PatientKey,
                    TenantID = qb.Patient.PatientKey
                };
            }

            if (qb.HiddenOverride == null)
            {
                qb.HiddenOverride = qb.SetupAdvanceCarePlanHiddenOverride();
            }

            return qb;
        }
    }
}