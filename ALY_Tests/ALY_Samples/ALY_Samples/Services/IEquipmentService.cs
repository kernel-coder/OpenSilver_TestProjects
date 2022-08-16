#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IEquipmentService : IModelDataService<Equipment>, ICleanup
    {
        PagedEntityCollectionView<Equipment> Equipments { get; }
        void Remove(BillCodes entity);
    }
}