#region Usings

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.Delta)]
    [Export(typeof(ICache))]
    public class DeltaCache : ReferenceCacheBase<Delta>
    {
        public static DeltaCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.Deltas;

        [ImportingConstructor]
        public DeltaCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Delta, "004")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("DeltaCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.Delta;
        }

        protected override EntityQuery<Delta> GetEntityQuery()
        {
            return Context.GetDeltaQuery();
        }

        public static Delta GetDelta()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Deltas == null))
            {
                return null;
            }

            Delta d = Current.Context.Deltas.FirstOrDefault();
            if (d == null)
            {
                MessageBox.Show("Error DeltaCache.GetDelta: Delta table is not defined.  Contact your system administrator.");
            }

            return d;
        }
    }
}