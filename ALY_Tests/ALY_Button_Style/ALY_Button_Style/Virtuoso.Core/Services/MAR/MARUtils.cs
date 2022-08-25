#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Model;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services.MAR
{
    public class MARUtils
    {
        // Maybe move factories into their respective class since they just wrap...but I like the client API.

        public static List<AdmissionMedicationMAR> MARGetAdmissionMedicationMARsForMed(Admission admission, PatientMedication patientMedication, Func<AdmissionMedicationMAR, bool> optionalFilter)
        {
            if (optionalFilter == null)
            {
                optionalFilter = (amm) => true;
            }
            // Now all the MARs for shift may not hang off a single PatientMedication
            // This still remains so for Encounters (due to dynamic form save logic that moves them to the new version of the med
            // But in AdmissionMode, the lists spans encounter shifts (using a 0-24 hour shift (the entire day)
            // Each shift may have its own version of any given med
            if ((admission == null) || (admission.AdmissionMedicationMAR == null) || (patientMedication == null)) return null;
            if (patientMedication.HistoryKey == null)
            {
                return admission.AdmissionMedicationMAR
                    .Where(amm => amm.PatientMedicationKey == patientMedication.PatientMedicationKey)
                    .Where(optionalFilter)
                    .ToList();
            }
            Func<AdmissionMedicationMAR, bool> isValidForMedication = (amm) =>
            {
                return (amm.PatientMedicationKey == patientMedication.PatientMedicationKey
                || amm.PatientMedicationHistoryKey == patientMedication.HistoryKey
                || amm.PatientMedicationKey == patientMedication.HistoryKey);
            };
            return admission.AdmissionMedicationMAR
                .Where(isValidForMedication)
                .Where(optionalFilter)
                .ToList();
        }

        #region Factories

        // TODO Maybe move factories into their respective class since they just wrap...but I like the client API.

        public static MAREntityValidator CreateMARValidator(bool isAdmissionMode, Encounter encounter, AdmissionMedicationMAR admissionMedicationMAR)
        {
            return new MAREntityValidator(isAdmissionMode, encounter, admissionMedicationMAR);
        }

        public static IMARMedicationView CreateCurrentMedicationsFilter(Patient patient, Admission admission, Encounter encounter, bool isAdmissionMode, DateTimeOffset? startDate)
        {
            //return new MARMedicationView(patient, admission, encounter, isAdmissionMode, startDate);
            if (isAdmissionMode)
            {
                return new AdmissionMARMedicationView(patient, admission, startDate);
            }
            else
            {
                return new EncounterMARMedicationView(patient, encounter);
            }
        }

        public static DataSetValidator CreateDataSetValidator(Patient patient, Admission admission, Encounter encounter, bool isAdmissionMode, DateTimeOffset? startDate)
        {
            return new DataSetValidator(patient, admission, encounter, isAdmissionMode, startDate);
        }

        public static IMARDataSet CreateMarDataSet(IPatientService model, Patient patient, Admission admission, Encounter encounter, bool isAdmissionMode, DateTimeOffset? startDate)
        {
            if (isAdmissionMode)
            {
                return new AdmissionMARDataSet(model, patient, admission, startDate);
            }
            else
            {
                return new EncounterMARDataSet(model, patient, encounter);
            }
        }

        #endregion Factories

        public static void Refresh_STD_PriorAdministrations(Admission admission, MARMed MARMed)
        {
            AdmissionMARUtils.Refresh_STD_PriorAdministrations(admission, MARMed);
        }

        public static void Refresh_PRN(MARMed MARMed, bool isAdmissionMode, Admission admission, Encounter encounter)
        {
            if (isAdmissionMode)
            {
                if (admission == null)
                {
                    throw new ArgumentNullException("Admission cannot be NULL when AdmissionMode is TRUE");
                }
                AdmissionMARUtils.Refresh_PRN(admission, MARMed);
            }
            else
            {
                if (encounter == null)
                {
                    throw new ArgumentNullException("Encounter cannot be NULL when AdmissionMode is FALSE");
                }
                EncounterMARUtils.Refresh_PRN(encounter, MARMed);
            }
        }

        public static int MARProxyEncounterShiftStartHour(int? MARProxyEncounterShift)
        {
            CodeLookup cl = CodeLookupCache.GetCodeLookupFromKey(MARProxyEncounterShift);
            if ((cl == null) || (string.IsNullOrWhiteSpace(cl.ApplicationData)))
            {
                return 0;
            }

            string ad = cl.ApplicationData;
            string[] DELIMITER = { "|" };
            string[] hours = ad.Split(DELIMITER, StringSplitOptions.None);
            if (hours.Length == 0)
            {
                return 0;
            }

            int hour = 0;
            int.TryParse(hours[0], out hour);
            return hour;
        }

        public static int MARProxyEncounterShiftEndHour(int? MARProxyEncounterShift)
        {
            CodeLookup cl = CodeLookupCache.GetCodeLookupFromKey(MARProxyEncounterShift);
            if ((cl == null) || (string.IsNullOrWhiteSpace(cl.ApplicationData)))
            {
                return 0;
            }

            string ad = cl.ApplicationData;
            string[] DELIMITER = { "|" };
            string[] hours = ad.Split(DELIMITER, StringSplitOptions.None);
            if (hours.Length < 2)
            {
                return 0;
            }

            int hour = 0;
            int.TryParse(hours[1], out hour);
            return hour;
        }
    }
}