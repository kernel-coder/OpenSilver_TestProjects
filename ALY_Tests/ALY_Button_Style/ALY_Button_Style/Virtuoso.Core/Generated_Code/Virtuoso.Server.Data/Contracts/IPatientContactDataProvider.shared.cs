
namespace Virtuoso.Validation
{
    public interface IPatientContactDataProvider
    {
        string PatientContact1KeyLabel(int? key);
        string PatientContact2KeyLabel(int? key);
        string PatientContact3KeyLabel(int? key);
    }
}
