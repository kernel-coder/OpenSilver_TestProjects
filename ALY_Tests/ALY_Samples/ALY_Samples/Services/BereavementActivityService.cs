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
    [Export(typeof(IBereavementActivityService))]
    public class BereavementActivityService : PagedModelBase, IBereavementActivityService
    {
        public VirtuosoDomainContext Context { get; set; }

        public BereavementActivityService()
        {
            Context = new VirtuosoDomainContext();
            BereavementActivities =
                new PagedEntityCollectionView<BereavementActivity>(Context.BereavementActivities, this);
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

        #region BereavementActivity Members

        public void Add(BereavementActivity entity)
        {
            Context.BereavementActivities.Add(entity);
        }

        public void Remove(BereavementActivity entity)
        {
            Context.BereavementActivities.Remove(entity);
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
                Context.BereavementActivities.Clear();

                var query = Context.GetBereavementActivityQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "BereavementActivityKey":
                                query = query.Where(p => p.BereavementActivityKey == Convert.ToInt32(searchvalue));
                                break;
                            case "ActivityDescription":
                                query = query.Where(
                                    p => p.ActivityDescription.ToLower().Contains(searchvalue.ToLower()));
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
                Context.BereavementActivities.Clear();

                var query = Context.GetBereavementActivityQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "BereavementActivityKey":
                                query = query.Where(p => p.BereavementActivityKey == Convert.ToInt32(searchvalue));
                                break;
                            case "ActivityDescription":
                                query = query.Where(
                                    p => p.ActivityDescription.ToLower().Contains(searchvalue.ToLower()));
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
                //else
                //    query = query.Where(p => p.Inactive == false); Include inactive

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

        public IEnumerable<BereavementActivity> Items => Context.BereavementActivities;

        PagedEntityCollectionView<BereavementActivity> _BereavementActivities;

        public PagedEntityCollectionView<BereavementActivity> BereavementActivities
        {
            get { return _BereavementActivities; }
            set
            {
                if (_BereavementActivities != value)
                {
                    _BereavementActivities = value;
                    this.RaisePropertyChanged(p => p.BereavementActivities);
                }
            }
        }

        public event EventHandler<EntityEventArgs<BereavementActivity>> OnLoaded;

        public event EventHandler<ErrorEventArgs> OnSaved;

        public bool SaveAllAsync()
        {
            WriteLine($"[3000] {nameof(BereavementActivityService)}: {nameof(SaveAllAsync)}");

            var open_or_invalid =
                OpenOrInvalidObjects(Context, tag: $"{nameof(BereavementActivityService)}", log: true);
            if (open_or_invalid) //TODO: should we raise/return an error or something???
            {
                PendingSubmit = true;
                WriteLine(
                    $"[3000] {nameof(BereavementActivityService)}: {nameof(SaveAllAsync)}.  Early return because of pending submit.  Not submitting changes.");
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
            BereavementActivities.Cleanup();

            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}