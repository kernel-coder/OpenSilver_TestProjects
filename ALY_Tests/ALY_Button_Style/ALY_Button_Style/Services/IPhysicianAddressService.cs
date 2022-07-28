using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Services
{
    public interface IPhysicianAddressService : IModelDataService<PhysicianAddress>, ICleanup
    {
        PagedEntityCollectionView<PhysicianAddress> PhysicianAddresses { get; }
        System.Threading.Tasks.Task<bool> ValidatePhysicianAsync(PhysicianAddress physicianAddress);
       // void Remove(PhysicianAddress entity);
       // void Remove(PhysicianEmail entity);
       // void Remove(PhysicianLicense entity);
       // void Remove(PhysicianPhone entity);
    }
}
