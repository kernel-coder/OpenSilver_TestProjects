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
    [ExportMetadata("CacheName", ReferenceTableName.TrackingGroup)]
    [Export(typeof(ICache))]
    public class TrackingGroupCache : ReferenceCacheBase<OrderTrackingGroup>
    {
        public static TrackingGroupCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.OrderTrackingGroups;

        [ImportingConstructor]
        public TrackingGroupCache(ILogger logManager)
            : base(logManager, ReferenceTableName.TrackingGroup, "003")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("TrackingGroupCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.TrackingGroup;
        }

        protected override EntityQuery<OrderTrackingGroup> GetEntityQuery()
        {
            return Context.GetOrderTrackingGroupQuery();
        }

        public static List<OrderTrackingGroup> GetActiveTrackingGroupsPlusMe(int? orderTrackingGroupKey,
            bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.OrderTrackingGroups == null))
            {
                return null;
            }

            int key = orderTrackingGroupKey ?? 0;
            var ret = Current.Context.OrderTrackingGroups
                .Where(p => ((p.Inactive == false) || (p.OrderTrackingGroupKey == key))).OrderBy(p => p.GroupID)
                .ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new OrderTrackingGroup { OrderTrackingGroupKey = 0, GroupID = " " });
            }

            return ret;
        }

        public static List<OrderTrackingGroup> GetActiveTrackingGroupsForFaciltyBranch(int? facilityBranchKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.OrderTrackingGroups == null)
                || (!facilityBranchKey.HasValue)
               )
            {
                return null;
            }

            var ret = Current.Context.OrderTrackingGroups
                .Where(p => (p.Inactive == false) && (p.FacilityBranchKey == facilityBranchKey)).ToList();

            return ret;
        }

        public static List<OrderTrackingGroup> GetTrackingGroupForKey(int? orderTrackingGroupKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.OrderTrackingGroups == null)
                || (!orderTrackingGroupKey.HasValue) || (orderTrackingGroupKey <= 0)
               )
            {
                return null;
            }

            var ret = Current.Context.OrderTrackingGroups.Where(p => (p.OrderTrackingGroupKey == orderTrackingGroupKey))
                .ToList();

            return ret;
        }
    }
}