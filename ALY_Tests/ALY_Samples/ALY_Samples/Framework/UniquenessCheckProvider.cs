#region Usings

using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Framework
{
    public class UniquenessCheckProvider : IUniquenessCheckProvider
    {
        private bool _IsSupplyItemCodeUnique(int? SupplyKey, string itemCode)
        {
            List<Supply> supplies = SupplyCache.GetSupplies();
            if (supplies == null)
            {
                return true;
            }

            int testKey = SupplyKey.HasValue ? SupplyKey.Value : 0;
            return !supplies.Where(w => (w.ItemCode == itemCode) && (w.SupplyKey != testKey)).Any();
        }

        bool IUniquenessCheckProvider.IsSupplyItemCodeUnique(int? SupplyKey, string ItemCode)
        {
            return _IsSupplyItemCodeUnique(SupplyKey, ItemCode);
        }
    }
}