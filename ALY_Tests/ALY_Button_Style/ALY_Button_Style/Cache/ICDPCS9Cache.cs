#region Usings

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Virtuoso.Client.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Portable.Database;
using Virtuoso.Portable.Model;
using Virtuoso.Portable.Utility;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.ICDPCS9)]
    [Export(typeof(ICache))]
    public class ICDPCS9Cache : FlatFileCacheBase<CachedICDCode>
    {
        public static ICDPCS9Cache Current { get; private set; }

        [ImportingConstructor]
        public ICDPCS9Cache(ILogger logManager)
            : base(logManager, true)
        {
            if (Current == this)
            {
                throw new InvalidOperationException("ICDPCS9Cache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.ICDPCS9;
            DatabaseWrapper = DatabaseService.Current.DatabaseFor(VirtuosoDatabase.ICDPCS9);
        }
        protected override void OnEntityDeserialized(CachedICDCode entity)
        {
            entity.FullText = Portable.Extensions.ICDCodeExtensions.CalculateFullText(entity);
        }

#if !OPENSILVER
        protected override void DeserializeData(RecordSet recordSet)
        {
            var entity = NewObject();
            Portable.Extensions.ICDCodeExtensions.RecordSetToCachedICDCode(recordSet, entity);
            _DataList.Add(entity);
        }
#endif

        // Called by maintenance screen after code saved to server database
        public async System.Threading.Tasks.Task UpdateICDCodeCache(ICDCode icdCode)
        {
            await EnsureDataLoadedFromDisk();

            var _icdCodeCache = (from i in Data
                where i.ICDCodeKey == icdCode.ICDCodeKey
                select i).FirstOrDefault();
            if (_icdCodeCache != null)
            {
                DynamicCopy.CopyProperties(icdCode, _icdCodeCache);
            }
            // NOTE: We're not updating the anchor, so this AND any other changes made by other users will 
            //       be re-sync'd upon next restart.
        }
    }
}