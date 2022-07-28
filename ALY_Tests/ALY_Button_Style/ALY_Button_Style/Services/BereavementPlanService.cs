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
using static System.Diagnostics.Debug;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IBereavementPlanService))]
    public class BereavementPlanService : PagedModelBase, IBereavementPlanService
    {
        public VirtuosoDomainContext Context { get; set; }

        public BereavementPlanService()
        {
            Context = new VirtuosoDomainContext();
            BereavementPlans = new PagedEntityCollectionView<BereavementPlan>(Context.BereavementPlans, this);
            Context.PropertyChanged += Context_PropertyChanged;
        }

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

        #region BereavementPlan Members

        public void Add(BereavementPlan entity)
        {
            Context.BereavementPlans.Add(entity);
        }

        public void Remove(BereavementPlan entity)
        {
            Context.BereavementPlans.Remove(entity);
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
                Context.BereavementPlans.Clear();

                var query = Context.GetBereavementPlanQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "BereavementPlanKey":
                                query = query.Where(p => p.BereavementPlanKey == Convert.ToInt32(searchvalue));
                                break;
                            case "BereavementPlanCode":
                                query = query.Where(
                                    p => p.BereavementPlanCode.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "BereavementPlanDescription":
                                query = query.Where(p =>
                                    p.BereavementPlanDescription.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "BereavementSourceKey":
                                if (Convert.ToInt32(searchvalue) > 0)
                                {
                                    query = query.Where(p => p.BereavementSourceKey == Convert.ToInt32(searchvalue));
                                }

                                break;
                            case "BereavementLocationKey":
                                if (Convert.ToInt32(searchvalue) > 0)
                                {
                                    query = query.Where(p => p.BereavementLocationKey == Convert.ToInt32(searchvalue));
                                }

                                break;
                            case "Inactive":
                                bool inactive = Convert.ToBoolean(searchvalue);
                                if (!inactive)
                                {
                                    query = query.Where(p => p.Inactive == inactive);
                                }

                                break;
                        }
                    }

                    if (isSystemSearch == false)
                    {
                        query = query.Where(p => p.Inactive == false);
                    }
                }
                else
                {
                    query = query.Where(p => p.Inactive == false);
                }

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
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.BereavementPlans.Clear();

                var query = Context.GetBereavementPlanQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "BereavementPlanKey":
                                query = query.Where(p => p.BereavementPlanKey == Convert.ToInt32(searchvalue));
                                break;
                            case "BereavementPlanCode":
                                query = query.Where(
                                    p => p.BereavementPlanCode.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "BereavementPlanDescription":
                                query = query.Where(p =>
                                    p.BereavementPlanDescription.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "BereavementSourceKey":
                                if (Convert.ToInt32(searchvalue) > 0)
                                {
                                    query = query.Where(p => p.BereavementSourceKey == Convert.ToInt32(searchvalue));
                                }

                                break;
                            case "BereavementLocationKey":
                                if (Convert.ToInt32(searchvalue) > 0)
                                {
                                    query = query.Where(p => p.BereavementLocationKey == Convert.ToInt32(searchvalue));
                                }

                                break;
                            case "Inactive":
                                bool inactive = Convert.ToBoolean(searchvalue);
                                if (!inactive)
                                {
                                    query = query.Where(p => p.Inactive == inactive);
                                }

                                break;
                        }
                    }
                }
                else
                {
                    query = query.Where(p => p.Inactive == false);
                }

                query.IncludeTotalCount = true;

                //if (PageSize > 0)
                //{
                //    query = query.Skip(PageSize * PageIndex);
                //    query = query.Take(PageSize);
                //}

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public IEnumerable<BereavementPlan> Items => Context.BereavementPlans;

        PagedEntityCollectionView<BereavementPlan> _BereavementPlans;

        public PagedEntityCollectionView<BereavementPlan> BereavementPlans
        {
            get { return _BereavementPlans; }
            set
            {
                if (_BereavementPlans != value)
                {
                    _BereavementPlans = value;
                    this.RaisePropertyChanged(p => p.BereavementPlans);
                }
            }
        }

        public event EventHandler<EntityEventArgs<BereavementPlan>> OnLoaded;

        public event EventHandler<ErrorEventArgs> OnSaved;

        public bool SaveAllAsync()
        {
            WriteLine($"[4000] {nameof(BereavementPlanService)}: {nameof(SaveAllAsync)}");

            var open_or_invalid = OpenOrInvalidObjects(Context, tag: $"{nameof(BereavementPlanService)}", log: true);
            if (open_or_invalid) //TODO: should we raise/return an error or something???
            {
                PendingSubmit = true;
                WriteLine(
                    $"[4000] {nameof(BereavementPlanService)}: {nameof(SaveAllAsync)}.  Early return because of pending submit.  Not submitting changes.");
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

        void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public void Cleanup()
        {
            BereavementPlans.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}