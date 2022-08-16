#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Virtuoso.Client.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Portable.Database;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.ICDGEMS10)]
    [Export(typeof(ICache))]
    public class ICDGEMS10Cache : FlatFileCacheBase<Virtuoso.Portable.Model.CachedICDGEMS>
    {
        public static ICDGEMS10Cache Current { get; private set; }

        [ImportingConstructor]
        public ICDGEMS10Cache(ILogger logManager)
            : base(logManager, true)
        {
            if (Current == this)
            {
                throw new InvalidOperationException("ICDCM10Cache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.ICDGEMS10;
            DatabaseWrapper = DatabaseService.Current.DatabaseFor(VirtuosoDatabase.ICDGEMS10);
        }

#if !OPENSILVER
        protected override void DeserializeData(RecordSet recordSet)
        {
        }
#endif

        // Called by maintenance screen after code saved to server database
        public void UpdateICDGEMSCache(ICDCode icdgems)
        {
        }

        public List<Virtuoso.Portable.Model.CachedICDGEMS> Search(string code10)
        {
            return null;
        }
    }
}