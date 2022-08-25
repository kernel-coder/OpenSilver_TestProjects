#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services.MAR
{
    /*
     * Helper class to get the list of 'current' medications.  Current Medications excluding future and discontinued.
     * 
     * Contains one public method:
     *     1.) List<PatientMedication> GetMARMedicationList()
     * 
     * */
    public class EncounterMARMedicationView : IMARMedicationView
    {
        Patient Patient;
        Virtuoso.Server.Data.Encounter Encounter;

        internal EncounterMARMedicationView(Patient patient, Virtuoso.Server.Data.Encounter encounter)
        {
            Patient = patient;
            Encounter = encounter;
        }

        /*
         * NOTE: MARCurrentPatientMedications are current meds - excluding future and discontinued
         * */
        public List<PatientMedication> GetMARMedicationList(bool returnDiscontinuedMedications = false)
        {
            var pmView = Patient.PatientMedication
                .Where(med => FilterMARMedicationItems(med, returnDiscontinuedMedications))
                .OrderBy(med => med.MedicationStatus)
                .ThenBy(med => med.MedicationName)
                .Select(med => med)
                .ToList();
            return pmView;
        }

        private bool FilterMARMedicationItems(PatientMedication pm, bool returnDiscontinuedMedications = false)
        {
            // If we have an Encounter and the item is not new, only try to include the item if it is in this encounter
            if ((Encounter != null) && (pm.IsNew == false))
            {
                EncounterMedication em = Encounter.EncounterMedication
                    .Where(p => p.PatientMedication.PatientMedicationKey == pm.PatientMedicationKey).FirstOrDefault();
                if (em == null)
                {
                    return false;
                }
            }

            // MedicationStatus 
            //     0 = current
            //     1 = future
            //     2 = discontinued

            if (pm.MedicationStatus == 2 && returnDiscontinuedMedications)
            {
                return true; // client code wants discontinued medications
            }

            if (pm.MedicationStatusIsCurrent == false)
            {
                return false;
            }

            return true;
        }
    }
}
