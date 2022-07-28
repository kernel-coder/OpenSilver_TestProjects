namespace Virtuoso.Validation
{
    interface IPhysicianDataProvider
    {
        Virtuoso.Server.Data.Physician GetPhysicianFromKey(int physicianKey);
    }
}
