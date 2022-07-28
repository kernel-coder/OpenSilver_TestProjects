#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IPhysicianService : IModelDataService<Physician>, ICleanup
    {
        PagedEntityCollectionView<Physician> Physicians { get; }
        System.Threading.Tasks.Task<bool> ValidatePhysicianAsync(Physician physician);
        void Remove(PhysicianAddress entity);
        void Remove(PhysicianAlternateID entity);
        void Remove(PhysicianEmail entity);
        void Remove(PhysicianLicense entity);
        void Remove(PhysicianPhone entity);
    }
}