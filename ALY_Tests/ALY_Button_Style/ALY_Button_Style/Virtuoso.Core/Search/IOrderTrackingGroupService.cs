#region Usings

using System;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IOrderTrackingGroupService : IModelDataService<OrderTrackingGroup>, ICleanup
    {
        event EventHandler<EntityEventArgs> OnOrdersTrackingRowRefreshed;
        PagedEntityCollectionView<OrderTrackingGroup> OrderTrackingGroups { get; }
        PagedEntityCollectionView<RuleHeader> RuleHeaders { get; }
        event EventHandler<EntityEventArgs<RuleHeader>> OnRulesLoaded;

        void GetRuleAsync();
        
        void Add(RuleHeader entity);
        void Remove(RuleHeader entity);
        void Add(OrderTrackingGroupDetail entity);
        void Remove(OrderTrackingGroupDetail entity);
        void Add(RuleOrderTrackFollowUp entity);
        void Remove(RuleOrderTrackFollowUp entity);
    }
}