#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.Supply)]
    [Export(typeof(ICache))]
    public class SupplyCache : ReferenceCacheBase<Supply>
    {
        public static SupplyCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.Supplies;

        [ImportingConstructor]
        public SupplyCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Supply, "005")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("SupplyCache already initialized.");
            }

            Current = this;
        }

        protected override EntityQuery<Supply> GetEntityQuery()
        {
            return Context.GetSupplyQuery();
        }

        public static List<Supply> GetSupplies()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Supplies == null))
            {
                return null;
            }

            return Current.Context.Supplies.OrderBy(p => p.Description1).ToList();
        }

        public static Supply GetSupplyFromKey(int? supplyKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (supplyKey == null) || (Current.Context == null) ||
                (Current.Context.Disciplines == null))
            {
                return null;
            }

            Supply supply = Current.Context.Supplies.FirstOrDefault(p => p.SupplyKey == supplyKey);
            return supply;
        }

        public static String GetSupplyDescription1FromKey(int? key)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Supplies == null) || !key.HasValue)
            {
                return null;
            }

            Supply sup = Current.Context.Supplies.FirstOrDefault(p => p.SupplyKey == key);
            return sup?.Description1;
        }
    }
}