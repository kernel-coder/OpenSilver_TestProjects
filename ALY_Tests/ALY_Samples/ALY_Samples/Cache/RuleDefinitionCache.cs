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
    [ExportMetadata("CacheName", ReferenceTableName.RuleDefinition)]
    [Export(typeof(ICache))]
    public class RuleDefinitionCache : ReferenceCacheBase<RuleDefinition>
    {
        public static RuleDefinitionCache Current { get; private set; }

        [ImportingConstructor]
        public RuleDefinitionCache(ILogger logManager)
            : base(logManager, ReferenceTableName.RuleDefinition, "003")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("RuleDefinitionCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.RuleDefinition;
        }

        protected override EntitySet EntitySet => Context.RuleDefinitions;

        protected override EntityQuery<RuleDefinition> GetEntityQuery()
        {
            return Context.GetRulesQuery();
        }

        public static List<RuleDefinition> GetRules(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.OrderTrackingGroups == null))
            {
                return null;
            }

            var ret = Current.Context.RuleDefinitions.ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new RuleDefinition { RuleDefinitionKey = 0, RuleDescription = " " });
            }

            return ret;
        }

        public static RuleDefinition GetRuleForKey(int? ruleDefinitionKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.OrderTrackingGroups == null))
            {
                return null;
            }

            return Current.Context.RuleDefinitions
                .FirstOrDefault(r => r.RuleDefinitionKey == ruleDefinitionKey);
        }

        public static List<RuleHeader> GetRuleHeaders(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.OrderTrackingGroups == null))
            {
                return null;
            }

            var ret = Current.Context.RuleHeaders.ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new RuleHeader { RuleHeaderKey = 0, ServiceLineKey = 0 });
            }

            return ret;
        }
        private static RuleDefinition rd = null;
        public static DateTime CalculateDueDate(DateTime baseDate, int ServiceLineKey, string InitialOrSubsequent,
            int? OrderTypeCodeLookupKey)
        {
            Current?.EnsureCacheReady();
            DateTime dueDate = baseDate.AddDays(7).Date;
            if ((OrderTypeCodeLookupKey == null) || (OrderTypeCodeLookupKey) == 0)
            {
                return dueDate;
            }

            if ((Current == null) || (Current.Context == null) || (Current.Context.RuleHeaders == null) ||
                (Current.Context.OrderTrackingGroups == null) || (Current.Context.OrderTrackingGroups == null))
            {
                return dueDate;
            }

            if (rd == null) rd = Current.Context.RuleDefinitions.FirstOrDefault(r => r.RuleDescription == "Order Tracking Follow-Up Frequency");
            if (rd == null)
            {
                return dueDate;
            }

            RuleHeader rh = Current.Context.RuleHeaders
                .FirstOrDefault(r => ((r.RuleDefinitionKey == rd.RuleDefinitionKey) && (r.ServiceLineKey == ServiceLineKey)));
            if (rh == null)
            {
                return dueDate;
            }

            RuleOrderTrackFollowUp rot = Current.Context.RuleOrderTrackFollowUps
                .FirstOrDefault(r => ((r.RuleHeaderKey == rh.RuleHeaderKey) && (r.InitialOrSubsequent == InitialOrSubsequent) && (r.OrderType == OrderTypeCodeLookupKey)));
            if (rot == null)
            {
                return dueDate;
            }

            int duration = (rot.Duration == 0) ? 7 : rot.Duration;
            if (rot.Units == CodeLookupCache.GetRuleCycleDays())
            {
                return baseDate.AddDays(duration).Date;
            }

            if (rot.Units == CodeLookupCache.GetRuleCycleWeeks())
            {
                return baseDate.AddDays(duration * 7).Date;
            }

            if (rot.Units == CodeLookupCache.GetRuleCycleMonths())
            {
                return baseDate.AddMonths(duration).Date;
            }

            return dueDate;
        }

        public static List<RuleHeader> GetRuleHeadersForServiceLine(int? serviceLineKey, bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.OrderTrackingGroups == null))
            {
                return null;
            }

            var ret = Current.Context.RuleHeaders.Where(rh =>
                ((!serviceLineKey.HasValue) && (!rh.ServiceLineKey.HasValue))
                || (rh.ServiceLineKey == serviceLineKey)).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new RuleHeader { RuleHeaderKey = 0, ServiceLineKey = 0 });
            }

            return ret;
        }
    }
}