#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IOrderTrackingGroupService))]
    public class OrderTrackingGroupService : PagedModelBase, IOrderTrackingGroupService
    {
        public OrderTrackingGroupService()
        {
            Context = new VirtuosoDomainContext();
            Context.PropertyChanged += Context_PropertyChanged;
            OrderTrackingGroups = new PagedEntityCollectionView<OrderTrackingGroup>(Context.OrderTrackingGroups, this);
            RuleHeaders = new PagedEntityCollectionView<RuleHeader>(Context.RuleHeaders, this);
        }

        void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public VirtuosoDomainContext Context { get; set; }

        #region PagedModelBase Members

        public override void LoadData()
        {
            if (IsLoading || Context == null)
            {
                return;
            }

            IsLoading = true;

            GetAsync();
        }

        #endregion

        #region IModelDataService<OrderTrackingGroup> Members

        public void Add(OrderTrackingGroup entity)
        {
            Context.OrderTrackingGroups.Add(entity);
        }

        public void Add(OrderTrackingGroupDetail entity)
        {
            Context.OrderTrackingGroupDetails.Add(entity);
        }

        public void Add(RuleHeader entity)
        {
            Context.RuleHeaders.Add(entity);
        }

        public void Add(RuleOrderTrackFollowUp entity)
        {
            Context.RuleOrderTrackFollowUps.Add(entity);
        }

        public void Remove(OrderTrackingGroup entity)
        {
            Context.OrderTrackingGroups.Remove(entity);
        }

        public void Remove(OrderTrackingGroupDetail entity)
        {
            Context.OrderTrackingGroupDetails.Remove(entity);
        }

        public void Remove(RuleHeader entity)
        {
            Context.RuleHeaders.Remove(entity);
        }

        public void Remove(RuleOrderTrackFollowUp entity)
        {
            Context.RuleOrderTrackFollowUps.Remove(entity);
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
        }

        public void GetSearchAsync(bool isSystemSearch)
        {
            //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
            //          when isSystemSearch == false, then Inactive checkbox removed from search criteria; however 
            //          we want to always assume that it is checked - e.g. add Inactive==false to query.

            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.OrderTrackingGroups.Clear();

                string groupID = null;
                var item = SearchParameters.Where(s => s.Field == "GroupID").FirstOrDefault();

                if (item != null)
                {
                    groupID = item.Value;
                }

                int facilityKey = 0;
                item = SearchParameters.Where(s => s.Field == "FacilityKey").FirstOrDefault();

                if (item != null)
                {
                    if (!int.TryParse(item.Value, out facilityKey))
                    {
                        facilityKey = 0;
                    }
                }

                int facilityBranchKey = 0;
                item = SearchParameters.Where(s => s.Field == "FacilityBranchKey").FirstOrDefault();

                if (item != null)
                {
                    if (!int.TryParse(item.Value, out facilityBranchKey))
                    {
                        facilityBranchKey = 0;
                    }
                }

                int state = 0;
                item = SearchParameters.Where(s => s.Field == "State").FirstOrDefault();

                if (item != null)
                {
                    if (!int.TryParse(item.Value, out state))
                    {
                        state = 0;
                    }
                }

                bool includeInactive = false;
                item = SearchParameters.Where(s => s.Field == "Inactive").FirstOrDefault();

                if (item != null)
                {
                    if (!bool.TryParse(item.Value, out includeInactive))
                    {
                        includeInactive = false;
                    }
                }

                string zipCode = null;
                item = SearchParameters.Where(s => s.Field == "ZipCode").FirstOrDefault();

                if (item != null)
                {
                    zipCode = item.Value;
                }

                string county = null;
                item = SearchParameters.Where(s => s.Field == "County").FirstOrDefault();

                if (item != null)
                {
                    county = item.Value;
                }

                if (isSystemSearch == false)
                {
                    //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
                    includeInactive = false;
                }

                var query = Context.GetTrackingGroupsForSearchQuery(groupID, facilityKey, facilityBranchKey, state,
                    zipCode, county, includeInactive);

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public void GetRuleAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.RuleHeaders.Clear();

                int ruleHeaderKey = 0;
                SearchParameter s = SearchParameters.Where(sp => sp.Field == "RuleHeaderKey").FirstOrDefault();
                if (s != null)
                {
                    int.TryParse(s.Value, out ruleHeaderKey);
                }

                var query = Context.GetRuleHeaderByKeyQuery(ruleHeaderKey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnRulesLoaded),
                    null);
            });
        }

        public void GetAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.OrderTrackingGroups.Clear();

                int orderTrackingGroupKey = 0;
                SearchParameter s = SearchParameters.Where(sp => sp.Field == "OrderTrackingGroupKey").FirstOrDefault();
                if (s != null)
                {
                    int.TryParse(s.Value, out orderTrackingGroupKey);
                }

                var query = Context.GetOrderTrackingGroupByKeyQuery(orderTrackingGroupKey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public IEnumerable<OrderTrackingGroup> Items => Context.OrderTrackingGroups;

        PagedEntityCollectionView<RuleHeader> _RuleHeaders;

        public PagedEntityCollectionView<RuleHeader> RuleHeaders
        {
            get { return _RuleHeaders; }
            set
            {
                if (_RuleHeaders != value)
                {
                    _RuleHeaders = value;
                    this.RaisePropertyChanged(p => p.RuleHeaders);
                }
            }
        }

        PagedEntityCollectionView<OrderTrackingGroup> _OrderTrackingGroups;

        public PagedEntityCollectionView<OrderTrackingGroup> OrderTrackingGroups
        {
            get { return _OrderTrackingGroups; }
            set
            {
                if (_OrderTrackingGroups != value)
                {
                    _OrderTrackingGroups = value;
                    this.RaisePropertyChanged(p => p.OrderTrackingGroups);
                }
            }
        }

        public event EventHandler<EntityEventArgs<OrderTrackingGroup>> OnLoaded;
        public event EventHandler<EntityEventArgs<RuleHeader>> OnRulesLoaded;

#pragma warning disable 67
        // warning CS0067: The event 'OrderTrackingGroupService.OnOrdersTrackingRowRefreshed' is never used
        // NOTE: This event is not raised in this service, only subscribed/unsubscribed by DynamicFormViewModel and AdmissionViewModel, so
        //       only disabling the warning for now instead of removing it from the interface entirely.
        // TODO: investigate if this event can be raised or remove the event from interface IOrderTrackingGroupService
        public event EventHandler<EntityEventArgs> OnOrdersTrackingRowRefreshed;
#pragma warning restore 67

        public event EventHandler<ErrorEventArgs> OnSaved;

        public bool SaveAllAsync()
        {
            var open_or_invalid = OpenOrInvalidObjects(Context);
            if (open_or_invalid) //TODO: should we raise/return an error or something???
            {
                PendingSubmit = true;
                return false;
            }

            PendingSubmit = false;

            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                IsLoading = true;
                Context.SubmitChanges(g => HandleErrorResults(g, OnSaved), null);
            });

            return true;
        }

        public void RejectChanges()
        {
            Context.RejectChanges();
        }

        #endregion

        public bool ContextHasChanges => Context.HasChanges;

        public void Cleanup()
        {
            OrderTrackingGroups.Cleanup();
            RuleHeaders.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
            OrderTrackingGroups.Cleanup();
        }
    }
}