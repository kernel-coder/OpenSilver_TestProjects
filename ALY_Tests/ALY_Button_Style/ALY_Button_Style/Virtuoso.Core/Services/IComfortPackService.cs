#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IComfortPackService : IModelDataService<ComfortPack>, ICleanup
    {
        PagedEntityCollectionView<ComfortPack> ComfortPacks { get; }

        void Add(ComfortPackDiscipline entity);
        void Remove(ComfortPackDiscipline entity);
        void Add(ComfortPackMedication entity);
        void Remove(ComfortPackMedication entity);
        void Add(ComfortPackSupply entity);
        void Remove(ComfortPackSupply entity);
    }
}