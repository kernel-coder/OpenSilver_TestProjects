namespace Virtuoso.Core.Model
{
    public struct EncounterFaxParameters
    {
        public string FaxNumber { get; set; }
        public int PhysicianKey { get; set; }
        public int FormKey { get; set; }
        public int PatientKey { get; set; }
        public int EncounterKey { get; set; }
        public int AdmissionKey { get; set; }
        public bool HideOasisQuestions { get; set; }
    }
}