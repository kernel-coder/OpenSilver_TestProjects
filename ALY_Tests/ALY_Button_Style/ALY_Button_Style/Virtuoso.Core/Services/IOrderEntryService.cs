#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IOrderEntryService : IModelDataService<Admission>, ICleanup
    {
        PagedEntityCollectionView<OrderEntry> Orders { get; }
        PagedEntityCollectionView<Patient> Patients { get; }

        void Add(OrderEntry entity);
        void Remove(OrderEntry entity);

        void Add(OrderEntryVO entity);
        void Remove(OrderEntryVO entity);

        void Add(OrderEntrySignature entity);
        void Remove(OrderEntrySignature entity);

        void Add(OrderEntryCoSignature entity);
        void Remove(OrderEntryCoSignature entity);
    }
}