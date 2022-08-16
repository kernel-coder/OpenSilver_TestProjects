#region Usings

using System;
using System.Collections.Generic;
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
    [ExportMetadata("CacheName", ReferenceTableName.InsuranceGroup)]
    [Export(typeof(ICache))]
    public class InsuranceGroupCache : ReferenceCacheBase<InsuranceGroup>
    {
        public static InsuranceGroupCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.InsuranceGroups;

        [ImportingConstructor]
        public InsuranceGroupCache(ILogger logManager)
            : base(logManager, ReferenceTableName.InsuranceGroup, "002")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("InsuranceGroupCache already initialized.");
            }

            Current = this;
        }

        protected override EntityQuery<InsuranceGroup> GetEntityQuery()
        {
            return Context.GetInsuranceGroupQuery();
        }

        public static List<InsuranceGroup> GetInsuranceGroups()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.InsuranceGroups == null))
            {
                return null;
            }

            return Current.Context.InsuranceGroups.OrderBy(p => p.Name).ToList();
        }

        public static List<InsuranceGroup> GetActiveInsuranceGroups()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.InsuranceGroups == null))
            {
                return null;
            }

            return Current.Context.InsuranceGroups.Where(p => p.Inactive == false).OrderBy(p => p.Name).ToList();
        }

        public static List<InsuranceGroup> GetActiveInsuranceGroupsAndMe(int? currentUserInsuranceGroupKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.InsuranceGroups == null))
            {
                return null;
            }

            var ret = Current.Context.InsuranceGroups
                .Where(a => ((a.Inactive == false) || (a.InsuranceGroupKey == currentUserInsuranceGroupKey)))
                .OrderBy(a => a.Name).ToList();
            return ret;
        }

        public static InsuranceGroup GetInsuranceGroupFromKey(int? insuranceGroupKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.InsuranceGroups == null))
            {
                return null;
            }

            if (insuranceGroupKey == null)
            {
                return null;
            }

            InsuranceGroup i =
                (from c in Current.Context.InsuranceGroups.AsQueryable()
                    where (c.InsuranceGroupKey == insuranceGroupKey)
                    select c).FirstOrDefault();
            if ((i == null) && (insuranceGroupKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error InsuranceGroupCache.GetInsuranceGroupFromKey: InsuranceGroupKey {0} is not defined.  Contact your system administrator.",
                    insuranceGroupKey.ToString()));
            }

            return i;
        }

        public static string GetInsuranceGroupNameFromKey(int? insuranceGroupKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.InsuranceGroups == null))
            {
                return null;
            }

            if (insuranceGroupKey == null)
            {
                return null;
            }

            InsuranceGroup i = GetInsuranceGroupFromKey(insuranceGroupKey);
            return i?.Name;
        }

        public static InsuranceGroupDetail GetInsuranceGroupDetailFromKey(int? insuranceGroupDetailKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.InsuranceGroups == null) ||
                (Current.Context.InsuranceGroupDetails == null))
            {
                return null;
            }

            if (insuranceGroupDetailKey == null)
            {
                return null;
            }

            InsuranceGroupDetail igd =
                (from c in Current.Context.InsuranceGroupDetails.AsQueryable()
                    where (c.InsuranceGroupDetailKey == insuranceGroupDetailKey)
                    select c).FirstOrDefault();
            if ((igd == null) && (insuranceGroupDetailKey != 0))
            {
                return null;
            }

            return igd;
        }

        public static List<InsuranceGroupDetail> GetInsuranceGroupDetailsForGroupKey(int? insuranceGroupKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.InsuranceGroups == null) ||
                (Current.Context.InsuranceGroupDetails == null))
            {
                return null;
            }

            return Current.Context.InsuranceGroupDetails.Where(p => p.InsuranceGroupKey == insuranceGroupKey).ToList();
        }
    }
}