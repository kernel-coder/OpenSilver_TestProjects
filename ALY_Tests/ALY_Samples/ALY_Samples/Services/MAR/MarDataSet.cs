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
    public class MARDataSet
    {
        public class MARMedResult
        {
            public List<MARMed> PRN { get; private set; }
            public List<MARMed> STD { get; private set; }

            internal MARMedResult(List<MARMed> prn, List<MARMed> std)
            {
                PRN = prn;
                STD = std;
            }
        }

        IPatientService Model;
        Patient Patient;
        Encounter Encounter;

        internal MARDataSet(IPatientService model, Patient patient, Encounter encounter)
        {
            Model = model;
            Patient = patient;
            Encounter = encounter;
        }

        public MARMedResult Refresh()
        {
            /*
             * Once the EncounterShift is selected and confirmed, it cannot change.
             * Both lists refresh - even after shift is selected, to pick up new meds, dropped meds and meds that move from STD to PRN and vice versa.
             *
             * Given: 
             *      Shift is selected
             *      MED = PatientMedication entity
             *      AMM = AdmissionMedicationMAR entity
             *      PRN = PatientMedication.AsNeeded == TRUE
             *
             * UC 1: ADD - MED added
             *      Need to check all meds each time to add them if they weren't there the first time.
             *
             * UC 2: DELETE - MED discontinued.  
             *      If MED has any untouched AMMs - delete those, keep any others.  Need to show the MED even though it is currently discontinued (end dated), 
             *      because the user created documentation against it
             *
             * UC 3: MOVE - MED changed to PRN
             *      MED may have had MAR administration times prior to being changed to PRN
             *      AMM may have been created for MED when it was an STD MED
             *      If MED has any untouched AMMs - delete those, keep any others.  
             *      If the MED had STD AMM - need to show the MED in STD list even though it is currently a PRN, because the user created documentation against it
             *      while it was an STD MED.
             *
             * UC 4: MOVE - MED changed to STD
             *      MED may have MAR administration times now that it is a STD MED
             *      AMM may have been created for MED was it was a PRN MED
             *      If MED has any untouched AMMs - delete those, keep any others.  
             *      If the MED had PRN AMM - need to show the MED in PRN list even though it is currently a STD, because the user created documentation against it
             *      while it was an PRN MED.
             *
            */

            // Remove untouched, MARs for this Encounter where AsNeeded of MED does not equal PRN of MAR - happens when adding a new Medication in the Encounter 
            // and you've not saved to database yet AND you've documented prior to making an edit to flip the MAR from one list to the other.
            Encounter
                .AdmissionMedicationMAR
                .Where(amm => amm.PatientMedication.MedicationStatus == 2) // discontinued medication
                .Where(amm => amm.AddedFromEncounterKey == Encounter.EncounterKey)  //bfm?
                //.Where(amm => amm.PRN != amm.PatientMedication.AsNeeded)         // NOTE: should only happen when adding a new med, then creating a MAR, then changing the med to/from PRN
                .Where(amm =>
                    amm.DocumentedState ==
                    MARDocumentState
                        .UnTouched) // TODO: should we also add a check to only delete MAR data when the data has not been saved to the database?
                .ForEach(amm => { RemoveAdmissionMedicationMAR(amm.Encounter, amm.AdmissionMedicationMARKey); });

            var prn = Refresh_PRN_List();
            var std = Refresh_STD_List();

            return new MARMedResult(prn, std);
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

        private bool MARProxyAllValid =>
            ((MARProxyEncounterShift == null) || (MARProxyEncounterStartDate == null)) ? false : true;

        private int MARProxyEncounterShiftStartHour => MARUtils.MARProxyEncounterShiftStartHour(MARProxyEncounterShift);

        private int MARProxyEncounterShiftEndHour => MARUtils.MARProxyEncounterShiftEndHour(MARProxyEncounterShift);

        private DateTime MARProxyEncounterShiftStartDateTime
        {
            get
            {
                if (MARProxyEncounterStartDate == null)
                {
                    return DateTime.MinValue;
                }

                return new DateTime(MARProxyEncounterStartDate.Value.Year, MARProxyEncounterStartDate.Value.Month,
                    MARProxyEncounterStartDate.Value.Day, MARProxyEncounterShiftStartHour, 0, 0);
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

                int endHour = MARProxyEncounterShiftEndHour;
                DateTime dateTime = new DateTime(MARProxyEncounterStartDate.Value.Year,
                    MARProxyEncounterStartDate.Value.Month, MARProxyEncounterStartDate.Value.Day, endHour, 0, 0);
                if (MARProxyEncounterShiftStartHour > endHour)
                {
                    dateTime = dateTime.AddDays(1);
                }

                return dateTime;
            }
        }

        // TODO refactor this.  Roll PRN filter into GetMARMedicationList - no need for this yield abstraction, now that GetMARMedicationList has changed to LINQ with predicate for filter
        private IEnumerable<PatientMedication> Get_Medication_For_MAR(bool isPRN = false,
            bool returnDiscontinuedMedications = false)
        {
            var utils = MARUtils.CreateCurrentMedicationsFilter(Patient, Encounter);

            List<PatientMedication>
                pmViewSource =
                    utils.GetMARMedicationList(
                        returnDiscontinuedMedications); // NOTE: MARCurrentPatientMedications are current meds - excluding future and discontinued

            foreach (PatientMedication pm in pmViewSource)
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

        private List<MARMed> Refresh_PRN_List()
        {
            List<MARMed> prnList = new List<MARMed>();

            if (MARProxyAllValid) // don't attempt to populate the MAR until we have date/time ranges to do so
            {
                var meds_for_encounter =
                    Get_Medication_For_MAR(isPRN: true, returnDiscontinuedMedications: true).ToList();

                System.Diagnostics.Debug.WriteLine("");
                System.Diagnostics.Debug.WriteLine(
                    "[MARRefresh_PRN_List]-----------------------All PRN Medication(s) For Current Encounter-----------------------");
                meds_for_encounter.ForEach(m =>
                    System.Diagnostics.Debug.WriteLine(
                        $"{m.PatientMedicationKey,10} {m.MedicationStatus} - {m.MedicationName}"));
                System.Diagnostics.Debug.WriteLine(
                    "---------------------------------------------------------------------------------------------------------------");
                System.Diagnostics.Debug.WriteLine("");

                foreach (PatientMedication pm in
                         meds_for_encounter) // NOTE: Some of these meds may be discontinued!  Skip those when building the MAR list unless that med has a MAR for this encounter!
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
                            pm.AdmissionMedicationMAR.Any(amm => amm.AddedFromEncounterKey == Encounter.EncounterKey))  //bfm?
                        {
                            MARUtils.Refresh_PRN(Encounter, newMARMed);
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
                var meds_for_encounter =
                    Get_Medication_For_MAR(isPRN: false, returnDiscontinuedMedications: true).ToList();

                System.Diagnostics.Debug.WriteLine("");
                System.Diagnostics.Debug.WriteLine(
                    "[MARRefresh_STD_List]-----------------------All Standard Medication(s) For Current Encounter-----------------------");
                meds_for_encounter.ForEach(m =>
                    System.Diagnostics.Debug.WriteLine(
                        $"{m.PatientMedicationKey,10} {m.MedicationStatus} - {m.MedicationName}"));
                System.Diagnostics.Debug.WriteLine(
                    "--------------------------------------------------------------------------------------------------------------------");
                System.Diagnostics.Debug.WriteLine("");

                // NOTE: Some of these meds may be discontinued!  Skip those when building the MAR list unless that med has a MAR for this encounter!
                foreach (PatientMedication pm in meds_for_encounter)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("[MARRefresh_STD_List]: ADD STD MED {0} - {1}",
                        pm.PatientMedicationKey, pm.MedicationDescription));

                    // NOTE: PRN now set from AsNeeded and not MARAdministrationTimesCount
                    MARMed newMARMed = new MARMed { PRN = pm.AsNeeded, MARPatientMedication = pm };

                    standardList.Add(newMARMed);

                    if ((newMARMed.PRN == false) && (newMARMed.MARPatientMedication != null) &&
                        (newMARMed.MARPatientMedication.AdmissionMedicationMAR != null))
                    {
                        if (pm.MedicationStatus != 2 ||
                            pm.AdmissionMedicationMAR.Any(amm => amm.AddedFromEncounterKey == Encounter.EncounterKey))  //bfm?
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

            // Check if MAR were already created for this med and encounter and that are not PRN
            var admissionMedicationMAR_For_PatientMedication = MARMed.MARPatientMedication
                .AdmissionMedicationMAR
                .Where(amm => amm.AddedFromEncounterKey == Encounter.EncounterKey) //bfm?
                .Where(amm => amm.PRN == false)
                .OrderBy(p => p.AdministrationDateTimeSort)
                .ToList();

            if (admissionMedicationMAR_For_PatientMedication.Count > 0)
            {
                // Refresh AdmissionMedicationMAR for STD med
                foreach (AdmissionMedicationMAR amm in admissionMedicationMAR_For_PatientMedication)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"\t [Refresh_STD]: ADD BACK MAR - Key: {amm.AdmissionMedicationMARKey} \t PatMedKey: {amm.PatientMedicationKey} \t PRN: {amm.PRN} \t Name: {amm.MedicationDescription} \t AdminDateTime: {amm.AdministrationDateTime} \t Comment: {amm.Comment}");

                    ammList.Add(amm);
                }
            }
            else
            {
                // We have no MARs for this medication - create them for the selected Shift
                AddAdmissionMedicationMARorShift(MARMed.MARPatientMedication, ammList, MARMed);
            }

            MARMed.AdmissionMedicationMARList = ammList.OrderBy(p => p.AdministrationDateTimeSort).ToList();
        }

        // NOTE: only called by Refresh_STD(...)
        private void AddAdmissionMedicationMARorShift(PatientMedication pm, List<AdmissionMedicationMAR> ammList,
            MARMed newMARMed)
        {
            int startHour = MARProxyEncounterShiftStartHour; // selected shift start hour
            int endHour = MARProxyEncounterShiftEndHour; // selected shift end hour

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
                CodeLookup cl =
                    CodeLookupCache.GetCodeLookupFromKey(
                        CodeLookupCache.GetKeyFromCode("MARAdministrationTimes", marTimeCode));
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

                    // Find existing AdmissionMedicationMAR or create a new AdmissionMedicationMAR
                    var amm = CreateStandardAdmissionMedicationMAR(newMARMed.MARPatientMedication, marTimeCode, hour,
                        hourIsPastMidnight);

                    System.Diagnostics.Debug.WriteLine(
                        $"\t [Refresh_STD]: ADD MAR Key: {amm.AdmissionMedicationMARKey} \t PatMedKey: {amm.PatientMedicationKey} \t PRN: {amm.PRN} \t Name: {amm.MedicationDescription} \t AdminDateTime: {amm.AdministrationDateTime} \t Comment: {amm.Comment}");

                    ammList.Add(amm);
                }
            }
        }

        private AdmissionMedicationMAR CreateStandardAdmissionMedicationMAR(PatientMedication pm,
            string MARAdministrationTime, int Hour, bool HourIsPastMidnight)
        {
            AdmissionMedicationMAR instance = Activator.CreateInstance<AdmissionMedicationMAR>(); //bfm

            instance.MedicationDescription = pm.MedicationDescription;
            instance.PRN = false;
            instance.DocumentedInEncounter = true; //bfm
            instance.MARAdministrationTime = MARAdministrationTime;
            DateTime sDate = new DateTime(MARProxyEncounterStartDate.Value.Year, MARProxyEncounterStartDate.Value.Month,
                MARProxyEncounterStartDate.Value.Day, Hour, 0, 0);
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

            if ((Encounter != null) && (Encounter.Admission != null) &&
                (Encounter.Admission.AdmissionMedicationMAR != null))
            {
                instance.AdmissionKey = Encounter.Admission.AdmissionKey;
                Encounter.Admission.AdmissionMedicationMAR.Add(instance);
            }

            if ((Encounter != null) && (Encounter.AdmissionMedicationMAR != null))
            {
                instance.AddedFromEncounterKey = Encounter.EncounterKey;
                Encounter.AdmissionMedicationMAR.Add(instance);
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

                if ((Encounter != null) && (Encounter.Admission != null) &&
                    (Encounter.Admission.AdmissionMedicationMAR != null))
                {
                    Encounter.Admission.AdmissionMedicationMAR.Remove(instance);
                }

                if ((Encounter != null) && (Encounter.AdmissionMedicationMAR != null))
                {
                    Encounter.AdmissionMedicationMAR.Remove(instance);
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