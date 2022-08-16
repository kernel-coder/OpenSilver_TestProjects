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
    [ExportMetadata("CacheName", ReferenceTableName.BereavementPlan)]
    [Export(typeof(ICache))]
    public class BereavementPlanCache : ReferenceCacheBase<BereavementPlan>
    {
        public static BereavementPlanCache Current { get; private set; }

        [ImportingConstructor]
        public BereavementPlanCache(ILogger logManager)
            : base(logManager, ReferenceTableName.BereavementPlan, "001")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("BereavementPlanCache already initialized.");
            }

            Current = this;
        }

        protected override EntitySet EntitySet => Context.BereavementPlans;

        protected override EntityQuery<BereavementPlan> GetEntityQuery()
        {
            return Context.GetBereavementPlanQuery();
        }

        public static List<BereavementPlan> GetBereavementPlans(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.BereavementPlans == null))
            {
                return null;
            }

            var ret = Current.Context.BereavementPlans.OrderBy(p => p.BereavementPlanDescription).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new BereavementPlan { BereavementPlanKey = 0, BereavementPlanDescription = " " });
            }

            return ret;
        }

        public static BereavementPlan GetBereavementPlanByKey(int? bereavementPlanKey)
        {
            Current?.EnsureCacheReady();
            if (!bereavementPlanKey.HasValue || Current == null || Current.Context == null ||
                Current.Context.BereavementPlans == null)
            {
                return null;
            }

            BereavementPlan bp = Current.Context.BereavementPlans.Where(p => p.BereavementPlanKey == bereavementPlanKey)
                .FirstOrDefault();
            return bp;
        }

        public static string GetBereavementPlanDescriptionByKey(int? bereavementPlanKey)
        {
            BereavementPlan bp = GetBereavementPlanByKey(bereavementPlanKey);
            return (bp == null) ? null : bp.BereavementPlanDescription;
        }

        public static BereavementPlan GetBereavementPlanBySourceLocationRange(int? bereavementSourceKey,
            int? bereavementLocationKey, int? riskRangeKey)
        {
            Current?.EnsureCacheReady();
            if (!bereavementSourceKey.HasValue || !bereavementLocationKey.HasValue || Current == null ||
                Current.Context == null || Current.Context.BereavementPlans == null)
            {
                return null;
            }

            BereavementPlan bp = Current.Context.BereavementPlans.Where(p =>
                ((p.BereavementSourceKey == bereavementSourceKey) &&
                 (p.BereavementLocationKey == bereavementLocationKey) && (p.RiskRangeKey == riskRangeKey) &&
                 (p.Inactive == false) && (p.HistoryKey == null))).FirstOrDefault();
            return bp;
        }

        public static List<BereavementActivity> GetBereavementActivities(int? bereavementPlanKey)
        {
            Current?.EnsureCacheReady();
            if (!bereavementPlanKey.HasValue || Current == null || Current.Context == null ||
                Current.Context.BereavementPlans == null)
            {
                return null;
            }

            List<BereavementPlanActivity> bpaList = Current.Context.BereavementPlanActivities
                .Where(p => ((p.BereavementPlanKey == bereavementPlanKey) && (p.Deleted == false))).ToList();
            if ((bpaList == null) || (bpaList.Any() == false))
            {
                return null;
            }

            List<BereavementActivity> baList = new List<BereavementActivity>();
            foreach (BereavementPlanActivity bpa in bpaList)
            {
                BereavementActivity ba =
                    BereavementActivityCache.GetBereavementActivityByKey(bpa.BereavementActivityKey);
                if ((ba != null) && (ba.Inactive == false))
                {
                    baList.Add(ba);
                }
            }

            if ((baList == null) || (baList.Any() == false))
            {
                return null;
            }

            return baList;
        }
    }
}