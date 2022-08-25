#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface ISupplyService : IModelDataService<Supply>, ICleanup
    {
        PagedEntityCollectionView<Supply> Supplies { get; }
        void Remove(BillCodes entity);
    }
}