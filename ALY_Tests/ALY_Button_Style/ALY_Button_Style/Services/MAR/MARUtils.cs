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
        #region Factories

        // Maybe move factories into their respective class since they just wrap...but I like the client API.

        public static MAREntityValidator CreateMARValidator(Encounter encounter,
            AdmissionMedicationMAR admissionMedicationMAR)
        {
            return new MAREntityValidator(encounter, admissionMedicationMAR);
        }

        public static MARMedicationView CreateCurrentMedicationsFilter(Patient patient, Encounter encounter)
        {
            return new MARMedicationView(patient, encounter);
        }

        public static DataSetValidator CreateDataSetValidator(Patient patient, Encounter encounter)
        {
            return new DataSetValidator(patient, encounter);
        }

        public static MARDataSet CreateMarDataSet(IPatientService model, Patient patient, Encounter encounter)
        {
            return new MARDataSet(model, patient, encounter);
        }

        #endregion Factories

        public static void Refresh_PRN(Encounter encounter, MARMed MARMed)
        {
            if ((MARMed == null) || (MARMed.PRN == false) || (MARMed.MARPatientMedication == null) ||
                (MARMed.MARPatientMedication.AdmissionMedicationMAR == null))
            {
                // NOTE: Only refreshing PRNs in this method - not standards, as PRNs are dynamic.
                return;
            }

            // NOTE:
            //       1.) PRNs are dynamic in the sense that the AdmissionMedicationMAR is not created for the PRN 
            //           medication until returned from the PRN Medication Administration popup.
            //
            //       2.) There could be an AdmissionMedicationMAR for the PatientMedication, where the AdmissionMedicationMAR
            //           was created when the PatientMedication was not PRN.  E.G. a STD medication that had a MAR created and
            //           not yet saved to the database, but then the user switched the med to As Needed.

            // Init the AdmissionMedicationMAR List for this MARMed
            List<AdmissionMedicationMAR> ammList = new List<AdmissionMedicationMAR>();

            // Add most recently administered AdmissionMedicationMAR for this PatientMedication that is also marked PRN bfm - just this encounter?
            AdmissionMedicationMAR ammPrior = MARMed.MARPatientMedication
                .AdmissionMedicationMAR
                .Where(amm => amm.PatientMedication.MedicationStatus != 2) // MedicationStatus == 2 is Discontinued
                .Where(amm => amm.AddedFromEncounterKey != encounter.EncounterKey) //bfm - just this encounter?
                .Where(amm => amm.PRN)
                .OrderByDescending(amm => amm.AdministrationDateTimeSort)
                .FirstOrDefault();
            if (ammPrior != null)
            {
                ammPrior.OtherEncounter = true;
                System.Diagnostics.Debug.WriteLine(
                    $"\t [Refresh_PRN]: ADD PRIOR MAR - Key: {ammPrior.AdmissionMedicationMARKey} \t PatMedKey: {ammPrior.PatientMedicationKey} \t PRN: {ammPrior.PRN} \t Name: {ammPrior.MedicationDescription} \t AdminDateTime: {ammPrior.AdministrationDateTime} \t Comment: {ammPrior.Comment}");
                ammList.Add(ammPrior);
            }

            foreach (AdmissionMedicationMAR amm in MARMed.MARPatientMedication.AdmissionMedicationMAR //bfm - just this encounter?
                         .Where(amm => amm.AddedFromEncounterKey == encounter.EncounterKey)
                         .OrderBy(p => p.AdministrationDateTimeSort).ToList())
                // Only add the existing AdmissionMedicationMAR to this PRN MARMed if the MAR was marked as PRN.
                // E.G. this MAR may be currently displaying for this med under the STD list if they documented 
                //      a MAR when the med was STD - prior to switching it to As Needed.
                if (amm.PRN && amm.DocumentedState != MARDocumentState.UnTouched)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"\t [Refresh_PRN]: ADD MAR - Key: {amm.AdmissionMedicationMARKey} \t PatMedKey: {amm.PatientMedicationKey} \t PRN: {amm.PRN} \t Name: {amm.MedicationDescription} \t AdminDateTime: {amm.AdministrationDateTime} \t Comment: {amm.Comment}");
                    ammList.Add(amm);
                }

            MARMed.AdmissionMedicationMARList = ammList.OrderBy(p => p.AdministrationDateTimeSort).ToList();
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