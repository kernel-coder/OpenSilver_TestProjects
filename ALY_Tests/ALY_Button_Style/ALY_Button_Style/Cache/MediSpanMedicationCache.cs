#region Usings

using System;
using System.ComponentModel.Composition;
using Virtuoso.Client.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Portable.Database;
using Virtuoso.Portable.Model;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.MediSpanMedication)]
    [Export(typeof(ICache))]
    public class MediSpanMedicationCache : FlatFileCacheBase<CachedMediSpanMedication>
    {
        public static MediSpanMedicationCache Current { get; private set; }

        [ImportingConstructor]
        public MediSpanMedicationCache(ILogger logManager)
            : base(logManager)
        {
            if (Current == this)
            {
                throw new InvalidOperationException("MediSpanMedicationCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.MediSpanMedication;
            DatabaseWrapper = DatabaseService.Current.DatabaseFor(VirtuosoDatabase.Medication);
        }

#if !OPENSILVER
        protected override void DeserializeData(RecordSet recordSet)
        {
            var entity = NewObject();
            Portable.Extensions.MedispanMedicationCacheExtensions
                .RecordSetToCachedMediSpanMedication(recordSet, entity);
            _DataList.Add(entity);
        }
#endif
    }
}