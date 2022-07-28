#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IOasisHeaderService : IModelDataService<OasisHeader>, ICleanup
    {
        PagedEntityCollectionView<OasisHeader> OasisHeaders { get; }
    }
}