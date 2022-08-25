#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Model;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services.MAR
{
    /*
     * This helper class is used so that this logic can be used from:
     *     1.) PatientCollectionBase to refresh/cleanup MAR data prior to running MAR data-set validation when signing a document
     *     2.) PatientMedicationUserControl to refresh the PRN and STD Medication list for 'View MAR'
     * 
     * Contains one public method:
     *     MARMedResult Refresh()
     * 
     * */
    public class AdmissionMARDataSet : IMARDataSet
    {
#if DEBUG
        public static readonly int MAX_PRIOR_ADMINISTRATIONS = 25;  // I want to see more when debugging locally
#else
        public static readonly int MAX_PRIOR_ADMINISTRATIONS = 3;
#endif
        IPatientService Model;
        Patient Patient;
        Admission Admission;
        DateTimeOffset? MARProxyStartDate;

        internal AdmissionMARDataSet(IPatientService model, Patient patient, Admission admission, DateTimeOffset? startDate)
        {
            Model = model;
            Patient = patient;
            Admission = admission;
            MARProxyStartDate = startDate;
        }

        // This will create and 'cleanup' MAR entities.  E.G. delete untouched MAR on discontinued meds.
        public MARMedResult Refresh()
        {
            // Remove untouched, MARs for this Encounter where AsNeeded of MED does not equal PRN of MAR - happens when adding a new Medication in the Encounter 
            // and you've not saved to database yet AND you've documented prior to making an edit to flip the MAR from one list to the other.
            Admission.AdmissionMedicationMAR
                .Where(amm => amm.PatientMedication.MedicationStatus == 2) // discontinued medication
                .Where(amm => amm.DocumentedState == MARDocumentState.UnTouched)
                .ForEach(amm => { RemoveAdmissionMedicationMAR(amm.Encounter, amm.AdmissionMedicationMARKey); });

            var std = Refresh_STD_List();
            var prn = Refresh_PRN_List();

            return new MARMedResult(prn, std);
        }

        private bool MARProxyAllValid => (MARProxyStartDate != null);

        private IEnumerable<PatientMedication> Get_Medication_For_MAR(bool isPRN = false, bool returnDiscontinuedMedications = false)
        {
            IMARMedicationView marMedView = MARUtils.CreateCurrentMedicationsFilter(Patient, Admission, null, true, MARProxyStartDate);

            // NOTE: MARCurrentPatientMedications are current meds - excluding future and discontinued
            List<PatientMedication> pmViewSource = marMedView.GetMARMedicationList(returnDiscontinuedMedications);

            foreach (PatientMedication pm in pmViewSource)
            {
                if (isPRN)
                {
                    if (pm.AsNeeded)
                    {
                        yield return pm;
                    }
                }
                else
                {
                    if (!pm.AsNeeded)
                    {
                        yield return pm;
                    }
                }
            }
        }

        private List<MARMed> Refresh_PRN_List()
        {
            List<MARMed> prnList = new List<MARMed>();

            if (MARProxyAllValid) // don't attempt to populate the MAR until we have date/time ranges to do so
            {
                var prn_meds = Get_Medication_For_MAR(isPRN: true, returnDiscontinuedMedications: true).ToList();

                System.Diagnostics.Debug.WriteLine("");
                System.Diagnostics.Debug.WriteLine(
                    "[MARRefresh_PRN_List]-----------------------All PRN Medication(s) For Current Encounter-----------------------");
                prn_meds.ForEach(m =>
                    System.Diagnostics.Debug.WriteLine(
                        $"{m.PatientMedicationKey,10} {m.MedicationStatus} - {m.MedicationName}"));
                System.Diagnostics.Debug.WriteLine(
                    "---------------------------------------------------------------------------------------------------------------");
                System.Diagnostics.Debug.WriteLine("");

                // NOTE: Some of these meds may be discontinued!  Skip those when building the MAR list unless that med has a MAR for this encounter!
                foreach (PatientMedication pm in prn_meds)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("[MARRefresh_PRN_List]: ADD PRN MED {0} - {1}",
                        pm.PatientMedicationKey, pm.MedicationDescription));

                    // NOTE: PRN now set from AsNeeded and not MARAdministrationTimesCount
                    MARMed newMARMed = new MARMed { PRN = pm.AsNeeded, MARPatientMedication = pm };

                    prnList.Add(newMARMed);

                    if (newMARMed.PRN && (newMARMed.MARPatientMedication != null) &&
                        (newMARMed.MARPatientMedication.AdmissionMedicationMAR != null))
                    {
                        // Only meds that are not discontinued
                        // Or meds that have MARs for this encounter (NOTE: the MAR could be linked to a discontinued med)
                        if (pm.MedicationStatus != 2 ||
                            pm.AdmissionMedicationMAR.Any(amm => amm.ScheduledAdministrationDateTime.GetValueOrDefault().Date.Equals(MARProxyStartDate.Value.Date)))
                        {
                            MARUtils.Refresh_PRN(newMARMed, true, Admission, null);
                        }
                    }
                }
            }

            return prnList;
        }

        private List<MARMed> Refresh_STD_List()
        {
            List<MARMed> standardList = new List<MARMed>();

            if (MARProxyAllValid) // don't attempt to populate the MAR until we have date/time ranges to do so
            {
                List<PatientMedication> _meds = Get_Medication_For_MAR(isPRN: false, returnDiscontinuedMedications: true).ToList();

                System.Diagnostics.Debug.WriteLine("");
                System.Diagnostics.Debug.WriteLine(
                    "[MARRefresh_STD_List]-----------------------All Standard Medication(s) For {Admission or Current Encounter}-----------------------");
                _meds.ForEach(m =>
                    System.Diagnostics.Debug.WriteLine(
                        $"{m.PatientMedicationKey,10} {m.MedicationStatus} - {m.MedicationName}"));
                System.Diagnostics.Debug.WriteLine(
                    "----------------------------------------------------------------------------------------------------------------------------------");
                System.Diagnostics.Debug.WriteLine("");                

                // NOTE: Some of these meds may be discontinued!  Skip those when building the MAR list unless that med has a MAR for this encounter!
                foreach (PatientMedication pm in _meds)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("[MARRefresh_STD_List]: ADD STD MED {0} - {1}", pm.PatientMedicationKey, pm.MedicationDescription));

                    // NOTE: PRN now set from AsNeeded and not MARAdministrationTimesCount
                    MARMed newMARMed = new MARMed { PRN = pm.AsNeeded, MARPatientMedication = pm };

                    standardList.Add(newMARMed);

                    if ((newMARMed.PRN == false) && (newMARMed.MARPatientMedication != null) &&
                        (newMARMed.MARPatientMedication.AdmissionMedicationMAR != null))
                    {
                        // NOTE: MedicationStatus used for sorting and UI: 0=current, 1=future, 2=discontinued
                        if (pm.MedicationStatus != 2 ||
                            pm.AdmissionMedicationMAR.Any(amm => amm.ScheduledAdministrationDateTime.GetValueOrDefault().Date.Equals(MARProxyStartDate.Value.Date)))
                        {
                            // Populate AdmissionMedicationMAR List for this med
                            Refresh_STD(newMARMed);
                        }
                    }
                }
            }

            return standardList;
        }

        private void Refresh_STD(MARMed MARMed)
        {
            if ((MARMed == null) || MARMed.PRN || (MARMed.MARPatientMedication == null) ||
                (MARMed.MARPatientMedication.AdmissionMedicationMAR == null))
            {
                // NOTE: Only refreshing standard medications in this method - not PRNs
                return;
            }

            // Init the AdmissionMedicationMAR List for this MARMed
            List<AdmissionMedicationMAR> ammList = new List<AdmissionMedicationMAR>();

            List<AdmissionMedicationMAR> admissionMedicationMAR_For_PatientMedication = null;

            // Check if MAR were already created for this med and date and that are not PRN
            admissionMedicationMAR_For_PatientMedication = MARUtils.MARGetAdmissionMedicationMARsForMed(
                Admission, MARMed.MARPatientMedication, (amm) =>
            {
                return (amm.PRN == false
                && amm.DocumentedBy.Equals(Guid.Empty) == false
                && amm.ScheduledAdministrationDateTime.Value.Date == MARProxyStartDate.Value.Date);
            })
            .OrderBy(p => p.AdministeredAndNotAdministeredDateTimeSort)
            .ToList();

            // Refresh AdmissionMedicationMAR for STD med
            foreach (AdmissionMedicationMAR amm in admissionMedicationMAR_For_PatientMedication)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"\t [Refresh_STD]: ADD BACK MAR - Key: {amm.AdmissionMedicationMARKey} \t PatMedKey: {amm.PatientMedicationKey} \t PRN: {amm.PRN} \t Name: {amm.MedicationDescription} \t AdminDateTime: {amm.AdministrationDateTime} \t Comment: {amm.Comment}");
                int hour = 0;
                CodeLookup cl = CodeLookupCache.GetCodeLookupFromKey(CodeLookupCache.GetKeyFromCode("MARAdministrationTimes", amm.MARAdministrationTime));
                if ((cl != null) && (string.IsNullOrWhiteSpace(cl.ApplicationData) == false))
                {
                    int.TryParse(cl.ApplicationData, out hour); // NOTE: cl.ApplicationData = [0..23]
                }
                amm.MARAdministrationTimeHour = hour;
                ammList.Add(amm);
            }

            // We have no MARs for this medication - create them for the selected (NOT Shift in Admission Mode) - for the selected date
            AddAdmissionMedicationMARorShift(MARMed.MARPatientMedication, ammList, MARMed);

            MARMed.AdmissionMedicationMARList = ammList.OrderBy(p => p.MARAdministrationTimeHour).ToList(); // Order by hour of day Midnight till 11PM

            AdmissionMARUtils.Refresh_STD_PriorAdministrations(Admission, MARMed);
        }

        // NOTE: only called by Refresh_STD(...)
        // checkIfExists TRUE for Admission, FALSE for Encounter
        private void AddAdmissionMedicationMARorShift(PatientMedication pm, List<AdmissionMedicationMAR> ammList, MARMed newMARMed, bool checkIfExists = true)
        {
            // NOTE: Don't need start and end hour - selected date covers entire day when in Admission maintenance
            int startHour = 0; // MARProxyEncounterShiftStartHour; // selected shift start hour
            int endHour = 24;  // MARProxyEncounterShiftEndHour;   // selected shift end hour

            string[] DELIMITER = { ", " };

            // NOTE: MARAdministrationTimes will be NULL for Medications that require no MAR times and where the end user never checked AsNeeded(PRN)
            // MARAdministrationTimes = MAR time(s) for Medication - E.G. "8AM, 8PM"
            string[] marArray = (newMARMed.MARPatientMedication.MARAdministrationTimes != null)
                ? newMARMed.MARPatientMedication.MARAdministrationTimes.Split(DELIMITER, StringSplitOptions.None)
                : new string[] { };

            // marTimeCode = MAR time for Medication - E.G. "8AM" or "8PM", etc...
            foreach (string marTimeCode in marArray)
            {
                int hour = 0;
                CodeLookup cl = CodeLookupCache.GetCodeLookupFromKey(CodeLookupCache.GetKeyFromCode("MARAdministrationTimes", marTimeCode));
                if ((cl != null) && (string.IsNullOrWhiteSpace(cl.ApplicationData) == false))
                {
                    int.TryParse(cl.ApplicationData, out hour); // NOTE: cl.ApplicationData = [0..23]
                }

                if (
                    (startHour > endHour && (hour >= startHour || hour < endHour)) || // case where shift spans midnight
                    (startHour <= endHour && (hour >= startHour && hour < endHour))
                )
                {
                    bool hourIsPastMidnight = ((startHour > endHour) && (hour < endHour));
                    DateTime sDate = new DateTime(MARProxyStartDate.Value.Year, MARProxyStartDate.Value.Month, MARProxyStartDate.Value.Day, hour, 0, 0);
                    if (hourIsPastMidnight)
                    {
                        sDate = sDate.AddDays(1);
                    }
                    if (checkIfExists)
                    {
                        // Find existing AdmissionMedicationMAR or create a new AdmissionMedicationMAR
                        if (ammList
                            .Where(am => am.PatientMedicationKey == newMARMed.MARPatientMedication.PatientMedicationKey)
                            .Where(am => am.PRN == false)
                            .Where(am => am.MARAdministrationTime == marTimeCode)
                            .Where(am => am.ScheduledAdministrationDateTime.GetValueOrDefault().Date == sDate.Date).Count() == 0)
                        {
                            var amm = CreateStandardAdmissionMedicationMAR(newMARMed.MARPatientMedication, marTimeCode, hour, hourIsPastMidnight);
                            System.Diagnostics.Debug.WriteLine(
                                $"\t [Refresh_STD]: ADD MAR Key: {amm.AdmissionMedicationMARKey} \t PatMedKey: {amm.PatientMedicationKey} \t PRN: {amm.PRN} \t Name: {amm.MedicationDescription} \t AdminDateTime: {amm.AdministrationDateTime} \t Comment: {amm.Comment}");
                            amm.MARAdministrationTimeHour = hour;
                            ammList.Add(amm);
                        }
                    }
                    else
                    {
                        var amm = CreateStandardAdmissionMedicationMAR(newMARMed.MARPatientMedication, marTimeCode, hour, hourIsPastMidnight);
                        System.Diagnostics.Debug.WriteLine(
                            $"\t [Refresh_STD]: ADD MAR Key: {amm.AdmissionMedicationMARKey} \t PatMedKey: {amm.PatientMedicationKey} \t PRN: {amm.PRN} \t Name: {amm.MedicationDescription} \t AdminDateTime: {amm.AdministrationDateTime} \t Comment: {amm.Comment}");
                        amm.MARAdministrationTimeHour = hour;
                        ammList.Add(amm);
                    }
                }
            }
        }

        private AdmissionMedicationMAR CreateStandardAdmissionMedicationMAR(
            PatientMedication pm,
            string MARAdministrationTime,
            int Hour,
            bool HourIsPastMidnight)
        {
            AdmissionMedicationMAR instance = Activator.CreateInstance<AdmissionMedicationMAR>();

            instance.MedicationDescription = pm.MedicationDescription;
            instance.PRN = false;
            instance.DocumentedInEncounter = false;
            instance.MARAdministrationTime = MARAdministrationTime;
            DateTime sDate = new DateTime(MARProxyStartDate.Value.Year, MARProxyStartDate.Value.Month, MARProxyStartDate.Value.Day, Hour, 0, 0);
            if (HourIsPastMidnight)
            {
                sDate = sDate.AddDays(1);
            }

            instance.ScheduledAdministrationDateTime = sDate;
            instance.AdministrationDateTimeOffSet = null;
            instance.DocumentedBy = Guid.Empty;
            instance.OtherEncounter = false;

            if ((Patient != null) && (Patient.AdmissionMedicationMAR != null))
            {
                instance.PatientKey = Patient.PatientKey;
                Patient.AdmissionMedicationMAR.Add(instance);
            }

            if ((Admission != null) && (Admission.AdmissionMedicationMAR != null))
            {
                Admission.AdmissionMedicationMAR.Add(instance);
            }

            if (pm.AdmissionMedicationMAR != null)
            {
                instance.PatientMedicationKey = pm.PatientMedicationKey;
                pm.AdmissionMedicationMAR.Add(instance);
            }

            return instance;
        }

        private void RemoveAdmissionMedicationMAR(Encounter encounter, int admissionMedicationMARKey)
        {
            if (encounter == null || encounter.AdmissionMedicationMAR == null)
            {
                return;
            }

            AdmissionMedicationMAR instance = encounter.AdmissionMedicationMAR.FirstOrDefault(amm => amm.AdmissionMedicationMARKey == admissionMedicationMARKey);

            if (instance != null)
            {
                PatientMedication pm = instance.PatientMedication;

                if ((Patient != null) && (Patient.AdmissionMedicationMAR != null))
                {
                    Patient.AdmissionMedicationMAR.Remove(instance);
                }

                if ((Admission != null) && (Admission.AdmissionMedicationMAR != null))
                {
                    Admission.AdmissionMedicationMAR.Remove(instance);
                }

                if ((pm != null) && (pm.AdmissionMedicationMAR != null))
                {
                    pm.AdmissionMedicationMAR.Remove(instance);
                }

                MARRemoveFromModel(instance);
            }
        }

        private void MARRemoveFromModel(AdmissionMedicationMAR entity)
        {
            if (Model == null)
            {
                throw new ArgumentNullException("Model", "Model is NULL");
            }

            Model.Remove(entity);
        }
    }
}
