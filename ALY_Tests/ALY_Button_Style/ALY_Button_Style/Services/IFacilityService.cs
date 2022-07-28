#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IFacilityService : IModelDataService<Facility>, ICleanup
    {
        PagedEntityCollectionView<Facility> Facilities { get; }
        void Remove(FacilityBranch entity);
        void Remove(FacilityMarketer entity);
        System.Threading.Tasks.Task<bool> ValidateFacilityAddressAsync(Facility facility);
    }
}