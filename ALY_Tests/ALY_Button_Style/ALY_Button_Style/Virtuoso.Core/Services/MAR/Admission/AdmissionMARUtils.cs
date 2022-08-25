#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Model;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services.MAR
{
    public class AdmissionMARUtils
    {
        public static void Refresh_STD_PriorAdministrations(Admission admission, MARMed MARMed)
        {
            if ((MARMed == null) || MARMed.PRN || (MARMed.MARPatientMedication == null) ||
                (MARMed.MARPatientMedication.AdmissionMedicationMAR == null))
            {
                // NOTE: Only refreshing standard medications in this method - not PRNs
                return;
            }
            Func<AdmissionMedicationMAR, bool> isValidForPriorMedication = (amm) =>
            {
                return (amm.PRN == false && amm.DocumentedBy.Equals(Guid.Empty) == false);
            };
            var priorAdministrations = MARUtils.MARGetAdmissionMedicationMARsForMed(admission, MARMed.MARPatientMedication, isValidForPriorMedication)
                .OrderByDescending(p => p.AdministeredAndNotAdministeredDateTimeSort)
                .Take(AdmissionMARDataSet.MAX_PRIOR_ADMINISTRATIONS)
                .ToList();
            MARMed.PriorAdministrationList = priorAdministrations;
        }

        // Populate the MARMed.AdmissionMedicationMARList for the PRN PatientMedication
        public static void Refresh_PRN(Admission admission, MARMed MARMed)
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

            foreach (AdmissionMedicationMAR amm in MARMed.MARPatientMedication.AdmissionMedicationMAR 
                         .OrderBy(p => p.AdministeredAndNotAdministeredDateTimeSort)
                         .ToList())
                // Only add the existing AdmissionMedicationMAR to this PRN MARMed if the MAR was marked as PRN.
                // E.G. this MAR may be currently displaying for this med under the STD list if they documented 
                //      a MAR when the med was STD - prior to switching it to As Needed.
                if (amm.PRN && amm.DocumentedState != MARDocumentState.UnTouched)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"\t [Refresh_PRN]: ADD MAR - Key: {amm.AdmissionMedicationMARKey} \t PatMedKey: {amm.PatientMedicationKey} \t PRN: {amm.PRN} \t Name: {amm.MedicationDescription} \t AdminDateTime: {amm.AdministrationDateTime} \t Comment: {amm.Comment}");
                    ammList.Add(amm);
                }

            // MARs/Administrations for this PRN Med
            var administrations = ammList
                .OrderByDescending(p => p.AdministeredAndNotAdministeredDateTimeSort)
                .ToList();
            MARMed.AdmissionMedicationMARList = administrations;
        }
    }
}
