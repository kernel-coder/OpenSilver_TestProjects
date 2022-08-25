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
    [ExportMetadata("CacheName", ReferenceTableName.ICDGEMS9)]
    [Export(typeof(ICache))]
    public class ICDGEMS9Cache : FlatFileCacheBase<Virtuoso.Portable.Model.CachedICDGEMS>
    {
        public static ICDGEMS9Cache Current { get; private set; }

        [ImportingConstructor]
        public ICDGEMS9Cache(ILogger logManager)
            : base(logManager, true)
        {
            if (Current == this)
            {
                throw new InvalidOperationException("ICDGEMS9Cache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.ICDGEMS9;
            DatabaseWrapper = DatabaseService.Current.DatabaseFor(VirtuosoDatabase.ICDGEMS9);
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

        public List<Virtuoso.Portable.Model.CachedICDGEMS> Search(string code9)
        {
            return null;
        }
    }
}