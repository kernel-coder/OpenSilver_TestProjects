using System;
using System.Collections.Generic;
using System.Linq;

namespace Virtuoso.Server.Data
{

    public class Admission
    {
        public object Addendum { get; set; }
        public int AdmissionDisciplineKey { get; set; }
        public int AdmissionKey { get; set; }
        public string CompletedBy { get; set; }
        public DateTime CompletedDateTime { get; set; }
        public bool CoSign { get; set; }
        public object DeathNote { get; set; }
        public int DisciplineServiceNumber { get; set; }
        public float Distance { get; set; }
        public string DistanceScale { get; set; }
        public int EncounterActualTime { get; set; }
        public string EncounterBy { get; set; }
        public string EncounterCollectedBy { get; set; }
        public DateTime EncounterDateTime { get; set; }
        public DateTime EncounterEndDate { get; set; }
        public DateTime EncounterEndTime { get; set; }
        public int EncounterKey { get; set; }
        public object EncounterShift { get; set; }
        public DateTime EncounterStartDate { get; set; }
        public DateTime EncounterStartTime { get; set; }
        public int EncounterStatus { get; set; }
        public object EncounterType { get; set; }
        public object ExportDateTime { get; set; }
        public int FormKey { get; set; }
        public object HistoryKey { get; set; }
        public bool Inactive { get; set; }
        public string LastChangedInSystem { get; set; }
        public bool NewDiagnosisVersion { get; set; }
        public object OverrideInsuranceKey { get; set; }
        public int PatientAddressKey { get; set; }
        public int PatientKey { get; set; }
        public object ReCertDiscipline { get; set; }
        public object ReportedBy { get; set; }
        public object ReviewBy { get; set; }
        public object ReviewComment { get; set; }
        public object ReviewDate { get; set; }
        public int ServiceTypeKey { get; set; }
        public bool Signed { get; set; }
        public string SYS_CD { get; set; }
        public int TaskKey { get; set; }
        public object TeamMeetingNote { get; set; }
        public int TenantID { get; set; }
        public object TherapyServiceNumber { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public object UserInitials { get; set; }
        public object VitalsVersion { get; set; }
        public int OriginalEncounterStatus { get; set; }
        public bool RemoveFromView { get; set; }
        public bool IsEditting { get; set; }
        public bool IsInCancel { get; set; }
        public bool IsOKed { get; set; }

        public List<Encounter> Encounter { get; set; }

    }

    public partial class EncounterTemp
    {

    }

}