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
    [ExportMetadata("CacheName", ReferenceTableName.ICDCM10)]
    [Export(typeof(ICache))]
    public class ICDCM10Cache : FlatFileCacheBase<CachedICDCode>
    {
        public static ICDCM10Cache Current { get; private set; }

        [ImportingConstructor]
        public ICDCM10Cache(ILogger logManager)
            : base(logManager, true)
        {
            if (Current == this)
            {
                throw new InvalidOperationException("ICDCM10Cache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.ICDCM10;
            DatabaseWrapper = DatabaseService.Current.DatabaseFor(VirtuosoDatabase.ICDCM10);
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

            var _icdCodeCache = (from CachedICDCode i in Data
                where i.ICDCodeKey == icdCode.ICDCodeKey
                select i).FirstOrDefault();
            if (_icdCodeCache != null)
            {
                DynamicCopy.CopyProperties(icdCode, _icdCodeCache);
                _icdCodeCache.FullText = Portable.Extensions.ICDCodeExtensions.CalculateFullText(_icdCodeCache);
            }
            // NOTE: We're not updating the anchor, so this AND any other changes made by other users will 
            //       be re-sync'd upon next restart.
        }
    }
}