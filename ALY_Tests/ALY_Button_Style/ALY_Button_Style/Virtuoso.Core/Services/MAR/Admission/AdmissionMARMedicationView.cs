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
    public class AdmissionMARMedicationView : IMARMedicationView
    {
        Patient Patient;
        Admission Admission;
        DateTimeOffset? MARProxyStartDate;

        internal AdmissionMARMedicationView(Patient patient, Admission admission, DateTimeOffset? startDate)
        {
            Patient = patient;
            Admission = admission;
            MARProxyStartDate = startDate;
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
            if (pm.Superceded == true)
            {
                return false;
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

            // If viewing MAR in Admission Maintenance, then match on start date
            if ((pm.MedicationStartDate == null || (pm.MedicationStartDate != null && pm.MedicationStartDate <= MARProxyStartDate.Value.Date)) &&
                (pm.MedicationEndDate == null   || (pm.MedicationEndDate   != null && pm.MedicationEndDate >= MARProxyStartDate.Value.Date)))
            {
                return true;
            }

            return false;
        }
    }
}
