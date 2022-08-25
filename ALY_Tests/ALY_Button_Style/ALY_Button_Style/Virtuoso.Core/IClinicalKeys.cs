namespace Virtuoso.Server.Data
{
    public interface IClinicalKeys
    {
        int PatientKey { get; }
        int AdmissionKey { get; }
    }
}