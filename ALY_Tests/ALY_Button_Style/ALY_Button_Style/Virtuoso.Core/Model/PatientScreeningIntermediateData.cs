namespace Virtuoso.Core.Model
{
    public class PatientScreeningIntermediateData
    {
        public string[] medications;
        public string[] icd9s;
        public string[] icd10s;
        public string[] allergies;
        public float? RenalFunction;
        public float? WeightKG;
        public double BodySurfaceArea;
    }
}