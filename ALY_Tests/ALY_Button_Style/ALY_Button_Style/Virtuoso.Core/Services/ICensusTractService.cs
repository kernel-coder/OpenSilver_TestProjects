#region Usings

using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface ICensusTractService : IModelDataService<CensusTract>, ICleanup
    {
        PagedEntityCollectionView<CensusTract> CensusTracts { get; }
        Task<bool> ValidateCensusTractAsync(CensusTract censusTract);
    }
}