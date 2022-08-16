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
    [Export(typeof(IProcessingRulesService))]
    public class ProcessingRulesService : PagedModelBase, IProcessingRulesService
    {
        public ProcessingRulesService()
        {
            Context = new VirtuosoDomainContext();
            Context.PropertyChanged += Context_PropertyChanged;
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

        #region IModelDataService<RuleHeader> Members

        public void Add(RuleHeader entity)
        {
            Context.RuleHeaders.Add(entity);
        }

        public void Remove(RuleHeader entity)
        {
            Context.RuleHeaders.Remove(entity);
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
                Context.RuleHeaders.Clear();

                SearchParameter item = null;
                int serviceLineKey = 0;
                item = SearchParameters.Where(s => s.Field == "ServiceLineKey").FirstOrDefault();

                if (item != null)
                {
                    if (!int.TryParse(item.Value, out serviceLineKey))
                    {
                        serviceLineKey = 0;
                    }
                }

                int ruleDefinitionKey = 0;
                item = SearchParameters.Where(s => s.Field == "RuleDefinitionKey").FirstOrDefault();

                if (item != null)
                {
                    if (!int.TryParse(item.Value, out ruleDefinitionKey))
                    {
                        serviceLineKey = 0;
                    }
                }

                var query = Context.GetRulesForSearchQuery(serviceLineKey, ruleDefinitionKey);

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public void GetAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            //Dispatcher.BeginInvoke(() =>
            //{
            //    Context.RejectChanges();
            //    Context.OrderTrackingGroups.Clear();

            //    int orderTrackingGroupKey = 0;
            //    SearchParameter s = SearchParameters.Where(sp => sp.Field == "OrderTrackingGroupKey").FirstOrDefault();
            //    if (s != null)
            //    {
            //        int.TryParse(s.Value, out orderTrackingGroupKey);
            //    }
            //    var query = Context.GetOrderTrackingGroupByKeyQuery(orderTrackingGroupKey);

            //    query.IncludeTotalCount = true;

            //    IsLoading = true;

            //    Context.Load<OrderTrackingGroup>(
            //        query,
            //        LoadBehavior.RefreshCurrent,
            //        g => HandleEntityResults<OrderTrackingGroup>(g, this.OnLoaded),
            //        null);
            //});
        }

        public IEnumerable<RuleHeader> Items => Context.RuleHeaders;

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

        public event EventHandler<EntityEventArgs<RuleHeader>> OnLoaded;

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
            RuleHeaders.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}