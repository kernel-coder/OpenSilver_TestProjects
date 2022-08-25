#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services.MAR
{
    /*
     * This helper class is used to validate the entire set of MAR data when signing a document.
     * 
     * Called from PatientCollectionBase.Validate() for the Medication data template
     * 
     * Contains two public methods:
     *     1.) MARValidationResult AnyCurrentMedicationsRequireMARMedicationTimes(bool initValidationError = false)
     *     2.) MARValidationResult AnyCurrentMedicationsRequireMARDocumentation(bool initValidationError = false)
     * 
     * */
    public class DataSetValidator
    {
        public class MARValidationResult
        {
            public bool Result { get; private set; }
            public string ValidationError { get; private set; }

            internal MARValidationResult(bool result, string validationError)
            {
                Result = result;
                ValidationError = validationError;
            }
        }

        Patient Patient;
        Admission Admission;
        Virtuoso.Server.Data.Encounter Encounter;
        bool IsAdmissionMode;
        DateTimeOffset? StartDate;

        internal DataSetValidator(Patient patient, Admission admission, Virtuoso.Server.Data.Encounter encounter, bool isAdmissionMode, DateTimeOffset? startDate)
        {
            Patient = patient;
            Admission = admission;
            Encounter = encounter;
            IsAdmissionMode = isAdmissionMode;
            StartDate = startDate;
        }

        public MARValidationResult AnyCurrentMedicationsRequireMARMedicationTimes(bool initValidationError = false)
        {
            string ValidationError = string.Empty;
            if (initValidationError)
            {
                ValidationError = string.Empty;
            }

            if (CheckAnyCurrentMedicationsRequireMARMedicationTimes()) // check PatientMedication records for MAR info
            {
                ValidationError = "Indicates the checked medication(s) require MAR Administration Times to be defined.";
                return new MARValidationResult(true, ValidationError);
            }

            return new MARValidationResult(false, ValidationError);
        }

        public MARValidationResult AnyCurrentMedicationsRequireMARDocumentation(bool initValidationError = false)
        {
            string ValidationError = string.Empty;
            if (initValidationError)
            {
                ValidationError = string.Empty;
            }

            if (CheckAnyCurrentMedicationsRequireMARDocumentation()) // check AdmissionMedicationMAR records
            {
                ValidationError = "Medications were not documented for your shift.";
                return new MARValidationResult(true, ValidationError);
            }

            return new MARValidationResult(false, ValidationError);
        }

        private bool MARValidationPrerequisites()
        {
            if ((Patient == null) || (Patient.PatientMedication == null) || (Encounter == null) ||
                (Encounter.Admission == null))
            {
                return false;
            }

            if ((Encounter.EncounterIsEval || Encounter.EncounterIsResumption || Encounter.EncounterIsVisit) == false)
            {
                return false;
            }

            if (Encounter.Admission.AdmissionSiteOfServiceIsApplicableToMAR(Encounter
                    .EncounterOrTaskStartDateAndTime) == false)
            {
                return false; // If site of service is NOT 'In Patient Hospice' - do not validate
            }

            // NOTE: MARCurrentPatientMedications are current meds - excluding future and discontinued
            IMARMedicationView utils = MARUtils.CreateCurrentMedicationsFilter(Patient, Admission, Encounter, IsAdmissionMode, StartDate);
            if (!utils.GetMARMedicationList().Any())
            {
                return false; // We have no Medications to validate
            }

            return true; // passes MAR pre-reqs - continue to validate MAR rules
        }

        private bool CheckAnyCurrentMedicationsRequireMARMedicationTimes()
        {
            if (MARValidationPrerequisites() == false)
            {
                return false;
            }

            bool MARAdministrationTimesRequired = false;
            foreach (PatientMedication pm in Patient.PatientMedication) pm.MARAdministrationTimesRequired = false;

            IMARMedicationView marMedicationView = MARUtils.CreateCurrentMedicationsFilter(Patient, Admission, Encounter, IsAdmissionMode, StartDate);

            // NOTE: MARCurrentPatientMedications are current meds - excluding future and discontinued
            List<PatientMedication> pmViewSource = marMedicationView.GetMARMedicationList();

            foreach (PatientMedication pm in pmViewSource)
            {
                var have_no_MARAdministrationTimes = string.IsNullOrWhiteSpace(pm.MARAdministrationTimes);
                var MARAdministrationTimesCount_greater_than_zero = (pm.MARAdministrationTimesCount > 0);

                // If have no PatientMedication.MARAdministrationTimes, but a positive MARAdministrationTimesCount value tells us we should have MAR times defined = then error
                if (have_no_MARAdministrationTimes && MARAdministrationTimesCount_greater_than_zero)
                {
                    pm.MARAdministrationTimesRequired = true;
                    MARAdministrationTimesRequired = true;
                }
            }

            return MARAdministrationTimesRequired;
        }

        private bool CheckAnyCurrentMedicationsRequireMARDocumentation()
        {
            // bfm return false if IsAdmissionMode
            if (MARValidationPrerequisites() == false)
            {
                return false;
            }

            bool MARAdministrationTimesRequired = false;
            foreach (PatientMedication pm in Patient.PatientMedication) pm.MARAdministrationTimesRequired = false;

            IMARMedicationView marMedicationView = MARUtils.CreateCurrentMedicationsFilter(Patient, Admission, Encounter, IsAdmissionMode, StartDate);

            // NOTE: MARCurrentPatientMedications are current meds - excluding future and discontinued
            List<PatientMedication> pmViewSource =marMedicationView.GetMARMedicationList();

            // Two use cases:
            // 1.) The end user never selected 'View MAR'.  In this case - the system never created AdmissionMedicationMAR records
            //      a.) Encounter.EncounterShift should be NULL
            //      b.) Cannot just look at PatientMedication's MARAdministrationTimesCount - need to account for shift selection
            // 2.) The end user select 'View MAR' and locked in a shift, which created AdmissionMedicationMAR records, but didn't document against any of them

            foreach (PatientMedication pm in pmViewSource)
            {
                var have_MARAdministrationTimes = (string.IsNullOrWhiteSpace(pm.MARAdministrationTimes) == false);
                var MARAdministrationTimesCount_greater_than_zero = (pm.MARAdministrationTimesCount > 0);

                // NOTE: Shift is required - when we have any Medications with MAR times defined and the Medication frequency dictates we must have MAR times documented
                //        Even if after selecting the Shift there are no AdmissionMedicationMAR rows created.
                //
                // Additionally:
                //        1.) If there are no medications - do not require shift
                //        2.) If the only medications are PRN - E.G. have no MAR Times - then do not require shift.  
                // NOTE - a PRN Medication should not have any MAR times defined.  We could have checked pm.AsNeeded instead of MARAdministrationTimes and MARAdministrationTimesCount.
                if (have_MARAdministrationTimes && MARAdministrationTimesCount_greater_than_zero &&
                    Encounter.EncounterShift.HasValue == false)
                {
                    MARAdministrationTimesRequired = true;
                    continue; // do not need to continue further - user must select a Shift
                }

                // NOTE: AdmissionMedicationMAR should only be attached to PatientMedication if the end user selected a shift as that is when the system creats those records bfm? what about admissionmain validate
                foreach (AdmissionMedicationMAR mar in pm.AdmissionMedicationMAR.Where(em =>
                             em.AddedFromEncounterKey ==
                             Encounter
                                 .EncounterKey)) // Validate the MARs on this Encounter - not all attached to every PatientMedication
                {
                    var validator = MARUtils.CreateMARValidator(IsAdmissionMode, Encounter, mar);
                    bool isValid = validator.MARClientValidate();

                    // Second use case
                    // 2.) The end user select 'View MAR' and locked in a shift, which created AdmissionMedicationMAR records, but didn't document against any of them
                    if (isValid == false)
                    {
                        MARAdministrationTimesRequired = true;
                    }
                }
            }

            return MARAdministrationTimesRequired;
        }
    }
}