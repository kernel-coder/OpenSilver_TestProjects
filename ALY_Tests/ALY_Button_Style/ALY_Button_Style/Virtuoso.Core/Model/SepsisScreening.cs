#region Usings

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class SepsisScreening : QuestionBase, INotifyDataErrorInfo
    {
        public SepsisScreening(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        #region Properties

        public EncounterSepsis EncounterSepsis { get; protected set; }

        #endregion Properties

        #region Methods

        public void SetupSepsisScreening()
        {
            Hidden = IsSepsisScreeningHidden;
            if (Hidden)
            {
                return;
            }

            // Setup EncounterSepsis - creating ine for this encounter if need be
            if ((Encounter == null) || (Encounter.EncounterSepsis == null))
            {
                return;
            }

            EncounterSepsis = Encounter.EncounterSepsis.FirstOrDefault();
            if (EncounterSepsis != null)
            {
                return;
            }

            EncounterSepsis = new EncounterSepsis
                { TenantID = Encounter.TenantID, EncounterKey = Encounter.EncounterKey };
            Encounter.EncounterSepsis.Add(EncounterSepsis);
        }

        private bool IsSepsisScreeningHidden
        {
            get
            {
                // If Sepsis is already part of this encounter show SepsisScreening section
                if ((Encounter == null) || (Encounter.EncounterSepsis == null))
                {
                    return true;
                }

                if (Encounter.EncounterSepsis.Any())
                {
                    return false;
                }

                // For encounters in Review status, Completed... (beyond edit), the SepsisScreening section will not backfill
                if ((Encounter.EncounterStatus != (int)EncounterStatusType.Edit) &&
                    (Encounter.EncounterStatus != (int)EncounterStatusType.None))
                {
                    return true;
                }

                // The SepsisScreening section only displays for patients 18 years or older
                if ((Patient != null) && (Patient.IsUnder18))
                {
                    return true;
                }

                // Fell thru, let the TenantSetting decide.
                return !TenantSettingsCache.Current.UsingSepsis;
            }
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;
            if ((Encounter?.EncounterSepsis == null) || Hidden)
            {
                return AllValid;
            }

            if (string.IsNullOrWhiteSpace(EncounterSepsis.PotentialInfection))
            {
                EncounterSepsis.PotentialInfection = null;
            }

            if (string.IsNullOrWhiteSpace(EncounterSepsis.PotentialInfectionComment))
            {
                EncounterSepsis.PotentialInfectionComment = null;
            }

            if (string.IsNullOrWhiteSpace(EncounterSepsis.SystemicCriteria))
            {
                EncounterSepsis.SystemicCriteria = null;
            }

            if (string.IsNullOrWhiteSpace(EncounterSepsis.OrganDysfunctionCriteria))
            {
                EncounterSepsis.OrganDysfunctionCriteria = null;
            }

            if (string.IsNullOrWhiteSpace(EncounterSepsis.OrganDysfunctionCriteriaComment))
            {
                EncounterSepsis.OrganDysfunctionCriteriaComment = null;
            }

            if (string.IsNullOrWhiteSpace(EncounterSepsis.Followup))
            {
                EncounterSepsis.Followup = null;
            }

            if (string.IsNullOrWhiteSpace(EncounterSepsis.FollowupComment))
            {
                EncounterSepsis.FollowupComment = null;
            }

            if (string.IsNullOrWhiteSpace(EncounterSepsis.InterventionComment))
            {
                EncounterSepsis.InterventionComment = null;
            }

            if (EncounterSepsis.PhysicianNotifiedDateTime == DateTime.MinValue)
            {
                EncounterSepsis.PhysicianNotifiedDateTime = null;
            }

            if (EncounterSepsis.PhysicianNotifiedKey == 0)
            {
                EncounterSepsis.PhysicianNotifiedKey = null;
            }

            EncounterSepsis.ValidationErrors.Clear();
            if (Encounter.FullValidation == false)
            {
                return AllValid;
            }

            // PotentialInfection is required
            if (EncounterSepsis.PotentialInfection == null)
            {
                EncounterSepsis.ValidationErrors.Add(new ValidationResult("Question 1. is required",
                    new[] { "PotentialInfection" }));
                AllValid = false;
            }

            // If the answer to PotentialInfection is “Yes”, PotentialInfectionComment is required
            if ((EncounterSepsis.PotentialInfectionIsYes) && (EncounterSepsis.PotentialInfectionComment == null))
            {
                EncounterSepsis.ValidationErrors.Add(new ValidationResult("Question 1. comments are required",
                    new[] { "PotentialInfectionComment" }));
                AllValid = false;
            }

            // SystemicCriteria is required
            if (EncounterSepsis.SystemicCriteria == null)
            {
                EncounterSepsis.ValidationErrors.Add(new ValidationResult("Question 2. is required",
                    new[] { "SystemicCriteria" }));
                AllValid = false;
            }

            // If the answer to SystemicCriteria is “Yes”, at least 2 options (from SystemicCriteriaFevor,SystemicCriteriaTachycardia,SystemicCriteriaTachypnea) must be checked
            int checkCount = 0;
            if (EncounterSepsis.SystemicCriteriaFevor)
            {
                checkCount++;
            }

            if (EncounterSepsis.SystemicCriteriaTachycardia)
            {
                checkCount++;
            }

            if (EncounterSepsis.SystemicCriteriaTachypnea)
            {
                checkCount++;
            }

            if ((EncounterSepsis.SystemicCriteriaIsYes) && (checkCount < 2))
            {
                EncounterSepsis.ValidationErrors.Add(new ValidationResult(
                    "At leaset 2 of the Systemic Criteria must be checked",
                    new[] { "SystemicCriteriaFevor", "SystemicCriteriaTachycardia", "SystemicCriteriaTachypnea" }));
                AllValid = false;
            }

            // OrganDysfunctionCriteria is required
            if (EncounterSepsis.OrganDysfunctionCriteria == null)
            {
                EncounterSepsis.ValidationErrors.Add(new ValidationResult("Question 3. is required",
                    new[] { "OrganDysfunctionCriteria" }));
                AllValid = false;
            }

            // If the answer to OrganDysfunctionCriteria is “Yes”, OrganDysfunctionCriteriaComment is required
            if ((EncounterSepsis.OrganDysfunctionCriteriaIsYes) &&
                (EncounterSepsis.OrganDysfunctionCriteriaComment == null))
            {
                EncounterSepsis.ValidationErrors.Add(new ValidationResult("Question 3. comments are required",
                    new[] { "OrganDysfunctionCriteriaComment" }));
                AllValid = false;
            }

            // PhysicianNotifiedDateTime cannot be a future date
            DateTimeOffset today = DateTimeOffset.Now;
            if ((EncounterSepsis.PhysicianNotifiedDateTime != null) &&
                (EncounterSepsis.PhysicianNotifiedDateTime > today))
            {
                EncounterSepsis.ValidationErrors.Add(new ValidationResult(
                    "The Date/Time Sepsis criteria met and physician notified cannot be in the future.",
                    new[] { "PhysicianNotifiedDateTime" }));
                AllValid = false;
            }

            // If PhysicianNotifiedDateTime or PhysicianNotifiedKey then they must both be entered
            if (((EncounterSepsis.PhysicianNotifiedDateTime == null) &&
                 (EncounterSepsis.PhysicianNotifiedKey != null)) ||
                ((EncounterSepsis.PhysicianNotifiedDateTime != null) && (EncounterSepsis.PhysicianNotifiedKey == null)))
            {
                EncounterSepsis.ValidationErrors.Add(new ValidationResult(
                    "Both the Date/Time Sepsis criteria met and physician notified and the Physician Notified must be answered or left blank.",
                    new[] { "PhysicianNotifiedDateTime", "PhysicianNotifiedKey" }));
                AllValid = false;
            }

            return AllValid;
        }

        #endregion Methods

        #region ICleanup

        public override void Cleanup()
        {
            EncounterSepsis = null;
            base.Cleanup();
        }

        #endregion ICleanup
    }

    public class SepsisScreeningFactory
    {
        public static SepsisScreening Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            SepsisScreening ss = new SepsisScreening(__FormSectionQuestionKey)
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
            ss.SetupSepsisScreening();
            return ss;
        }
    }
}