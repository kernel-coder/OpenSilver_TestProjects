#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IFacilityService))]
    public class FacilityService : PagedModelBase, IFacilityService
    {
        public VirtuosoDomainContext Context { get; set; }

        public FacilityService()
        {
            Context = new VirtuosoDomainContext();
            Facilities = new PagedEntityCollectionView<Facility>(Context.Facilities, this);
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

        #region IModelDataService<Facility> Members

        public void Add(Facility entity)
        {
            Context.Facilities.Add(entity);
        }

        public void Remove(Facility entity)
        {
            Context.Facilities.Remove(entity);
        }

        public void Remove(FacilityBranch entity)
        {
            Context.FacilityBranches.Remove(entity);
        }

        public void Remove(FacilityMarketer entity)
        {
            Context.FacilityMarketers.Remove(entity);
        }

        private object asyncValidationsLock = new object();
        private List<ValidationResult> asyncValidationResultList = new List<ValidationResult>();

        public Task<bool> ValidateFacilityAddressAsync(Facility facility)
        {
            lock (asyncValidationsLock)
            {
                asyncValidationResultList.Clear();
            }
            //NOTE: async-server side functions coded to return true if the error condition exists

            var validateCountyTask = Context.FacilityCountySelected(facility)
                .AsTask()
                .ContinueWith(t =>
                {
                    var operation = t.Result;
                    if (operation.HasError || !operation.Value)
                    {
                        lock (asyncValidationsLock)
                        {
                            asyncValidationResultList.Add(new ValidationResult("County is required.",
                                new[] { "County" }));
                        }
                    }

                    return t.Result.Value;
                });

            //wait for all async server calls to complete
            return System.Threading.Tasks.Task.Factory.ContinueWhenAll(
                new System.Threading.Tasks.Task[] { validateCountyTask },
                tasks =>
                {
                    if (tasks.Any(_t => _t.IsFaulted))
                    {
                        return false;
                    }

                    //Add cached errors to entity on the UI thread
                    asyncValidationResultList.ForEach(error => { facility.ValidationErrors.Add(error); });
                    return validateCountyTask.Result;
                },
                System.Threading.CancellationToken.None,
                TaskContinuationOptions.None,
                Client.Utils.AsyncUtility.TaskScheduler);
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
                Context.Facilities.Clear();

                var query = Context.GetFacilityQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "FacilityKey":
                                query = query.Where(p => p.FacilityKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Name":
                                query = query.Where(p => p.Name.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Type":
                                query = query.Where(p => p.Type == Convert.ToInt32(searchvalue));
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
                        //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
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
                Context.Facilities.Clear();

                var query = Context.GetFacilityQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "FacilityKey":
                                query = query.Where(p => p.FacilityKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Name":
                                query = query.Where(p => p.Name.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Type":
                                query = query.Where(p => p.Type == Convert.ToInt32(searchvalue));
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

                if (PageSize > 0)
                {
                    query = query.Skip(PageSize * PageIndex);
                    query = query.Take(PageSize);
                }

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public IEnumerable<Facility> Items => Context.Facilities;

        PagedEntityCollectionView<Facility> _Facilities;

        public PagedEntityCollectionView<Facility> Facilities
        {
            get { return _Facilities; }
            set
            {
                if (_Facilities != value)
                {
                    _Facilities = value;
                    this.RaisePropertyChanged(p => p.Facilities);
                }
            }
        }

        public event EventHandler<EntityEventArgs<Facility>> OnLoaded;

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

        void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public void Cleanup()
        {
            Facilities.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}