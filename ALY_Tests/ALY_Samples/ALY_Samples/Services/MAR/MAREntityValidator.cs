#region Usings

using System;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services.MAR
{
    /*
     * This helper class is used to validate a single AdmissionMedicationMAR entity.  Entity is modified via one of the MAR popups.
     * 
     * Called from 
     *     1.) DataSetValidator.CheckAnyCurrentMedicationsRequireMARDocumentation() when the visit is signed and validated.
     *     2.) PatientMedicationUserControl.MARClientValidate() when a MAR is OK'd from a MAR popup.
     * 
     * Contains one public method:
     *     1.) bool MARClientValidate()
     * 
     * */
    public class MAREntityValidator
    {
        Encounter Encounter;
        AdmissionMedicationMAR CurrentAdmissionMedicationMAR;

        internal MAREntityValidator(Encounter encounter, AdmissionMedicationMAR admissionMedicationMAR)
        {
            Encounter = encounter;
            CurrentAdmissionMedicationMAR = admissionMedicationMAR;
        }

        public bool MARClientValidate()
        {
            bool success = true;

            if (string.IsNullOrWhiteSpace(CurrentAdmissionMedicationMAR.NotAdministeredReason))
            {
                CurrentAdmissionMedicationMAR.NotAdministeredReason = null;
            }

            if (string.IsNullOrWhiteSpace(CurrentAdmissionMedicationMAR.Comment))
            {
                CurrentAdmissionMedicationMAR.Comment = null;
            }

            if (CurrentAdmissionMedicationMAR == null)
            {
                return false;
            }

            if (CurrentAdmissionMedicationMAR.PRN)
            {
                if (MARClientValidateAdministrationDateTime(CurrentAdmissionMedicationMAR) == false)
                {
                    success = false;
                }
            }

            if (CurrentAdmissionMedicationMAR.PRN == false)
            {
                if ((CurrentAdmissionMedicationMAR.AdministrationDateTimeSort == DateTime.MinValue) &&
                    (CurrentAdmissionMedicationMAR.NotAdministered == false))
                {
                    CurrentAdmissionMedicationMAR.ValidationErrors.Add(new ValidationResult(
                        "Either a complete Administration Date/Time must be entered or the Medication Not Administered must be checked.",
                        new[] { "AdministrationDatePart", "AdministrationTimePart", "NotAdministered" }));
                    success = false;
                }
                else if (CurrentAdmissionMedicationMAR.NotAdministered == false)
                {
                    if (MARClientValidateAdministrationDateTime(CurrentAdmissionMedicationMAR) == false)
                    {
                        success = false;
                    }
                }
                else if (CurrentAdmissionMedicationMAR.NotAdministeredReason == null)
                {
                    CurrentAdmissionMedicationMAR.ValidationErrors.Add(new ValidationResult(
                        "A Medication Not Administered reason is required.", new[] { "NotAdministeredReason" }));
                    success = false;
                }
            }

            return success;
        }

        private bool MARClientValidateAdministrationDateTime(AdmissionMedicationMAR admissionMedicationMAR)
        {
            bool success = true;
            DateTimeOffset now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            if ((admissionMedicationMAR.AdministrationDatePart == null) ||
                (admissionMedicationMAR.AdministrationDatePart == DateTime.MinValue))
            {
                admissionMedicationMAR.ValidationErrors.Add(new ValidationResult("Administration Date is required.",
                    new[] { "AdministrationDatePart" }));
                success = false;
            }

            if ((admissionMedicationMAR.AdministrationTimePart == null) ||
                (admissionMedicationMAR.AdministrationTimePart == DateTime.MinValue))
            {
                admissionMedicationMAR.ValidationErrors.Add(new ValidationResult("Administration Time is required.",
                    new[] { "AdministrationTimePart" }));
                success = false;
            }

            if ((admissionMedicationMAR.AdministrationDateTimeSort != DateTime.MinValue) &&
                (admissionMedicationMAR.AdministrationDateTimeSort < MARProxyEncounterShiftStartDateTime))
            {
                admissionMedicationMAR.ValidationErrors.Add(new ValidationResult(
                    "Administration Date/Time cannot be before the shift start.",
                    new[] { "AdministrationDatePart", "AdministrationTimePart" }));
                success = false;
            }

            if ((admissionMedicationMAR.AdministrationDateTimeSort != DateTime.MinValue) &&
                (admissionMedicationMAR.AdministrationDateTimeSort > now))
            {
                admissionMedicationMAR.ValidationErrors.Add(new ValidationResult(
                    "Administration Date/Time cannot be in the future.",
                    new[] { "AdministrationDatePart", "AdministrationTimePart" }));
                success = false;
            }
            else if ((admissionMedicationMAR.AdministrationDateTimeSort != DateTime.MinValue) &&
                     (admissionMedicationMAR.AdministrationDateTimeSort > MARProxyEncounterShiftEndDateTime))
            {
                admissionMedicationMAR.ValidationErrors.Add(new ValidationResult(
                    "Administration Date/Time cannot be after the shift end.",
                    new[] { "AdministrationDatePart", "AdministrationTimePart" }));
                success = false;
            }

            return success;
        }

        private DateTime MARProxyEncounterShiftStartDateTime
        {
            get
            {
                if (MARProxyEncounterStartDate == null)
                {
                    return DateTime.MinValue;
                }

                return new DateTime(
                    MARProxyEncounterStartDate.Value.Year,
                    MARProxyEncounterStartDate.Value.Month,
                    MARProxyEncounterStartDate.Value.Day,
                    MARUtils.MARProxyEncounterShiftStartHour(MARProxyEncounterShift),
                    0, 0);
            }
        }

        private DateTimeOffset? MARProxyEncounterStartDate
        {
            get
            {
                if (Encounter == null)
                {
                    return null;
                }

                return Encounter.EncounterStartDate;
            }
        }

        private DateTime MARProxyEncounterShiftEndDateTime
        {
            get
            {
                if (MARProxyEncounterStartDate == null)
                {
                    return DateTime.MinValue;
                }

                int endHour = MARUtils.MARProxyEncounterShiftEndHour(MARProxyEncounterShift);
                DateTime dateTime = new DateTime(MARProxyEncounterStartDate.Value.Year,
                    MARProxyEncounterStartDate.Value.Month, MARProxyEncounterStartDate.Value.Day, endHour, 0, 0);
                if (MARUtils.MARProxyEncounterShiftStartHour(MARProxyEncounterShift) > endHour)
                {
                    dateTime = dateTime.AddDays(1);
                }

                return dateTime;
            }
        }

        private int? MARProxyEncounterShift
        {
            get
            {
                if (Encounter == null)
                {
                    return null;
                }

                return Encounter.EncounterShift;
            }
        }
    }
}