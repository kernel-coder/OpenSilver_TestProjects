#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
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
    [Export(typeof(IServiceLineService))]
    public class ServiceLineService : PagedModelBase, IServiceLineService
    {
        public VirtuosoDomainContext Context { get; set; }

        public ServiceLineService()
        {
            Context = new VirtuosoDomainContext();
            ServiceLines = new PagedEntityCollectionView<ServiceLine>(Context.ServiceLines, this);
            Context.PropertyChanged += Context_PropertyChanged;
            ApplicationCoreContext.ServiceLineService = this;
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

        #region IModelDataService<ServiceLine> Members

        public void Remove(PhysicianGrouping entity)
        {
            Context.PhysicianGroupings.Remove(entity);
        }

        public void Add(PhysicianGrouping entity)
        {
            Context.PhysicianGroupings.Add(entity);
        }
        
        public void Remove(CensusTractMapping entity)
        {
            Context.CensusTractMappings.Remove(entity);
        }

        public void Add(CensusTractMapping entity)
        {
            Context.CensusTractMappings.Add(entity);
        }

        public void Add(ServiceLine entity)
        {
            Context.ServiceLines.Add(entity);
        }

        public void Remove(ServiceLine entity)
        {
            if (entity.ServiceLineGrouping != null)
            {
                foreach (ServiceLineGrouping e in entity.ServiceLineGrouping.ToList()) Remove(e);
            }

            Context.ServiceLines.Remove(entity);
        }

        public void Remove(ServiceLineGrouping entity)
        {
            Context.ServiceLineGroupings.Remove(entity);
        }

        public void Remove(ServiceLineGroupingParent entity)
        {
            Context.ServiceLineGroupingParents.Remove(entity);
        }

        public void Remove(ServiceTypeGrouping entity)
        {
            Context.ServiceTypeGroupings.Remove(entity);
        }

        public void Remove(ServiceLineGroupHeader entity)
        {
            Context.ServiceLineGroupHeaders.Remove(entity);
        }

        public void Remove(TeamMeetingSchedule entity)
        {
            if (Context.TeamMeetingSchedules.Any(ts => ts.TeamScheduleKey == entity.TeamScheduleKey))
            {
                Context.TeamMeetingSchedules.Remove(entity);
            }
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
                Context.ServiceLines.Clear();

                var query = Context.GetServiceLineQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "ServiceLineKey":
                                query = query.Where(p => p.ServiceLineKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Name":
                                query = query.Where(p => p.Name.ToLower().Contains(searchvalue.ToLower()));
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

        public void GetCensusTractAsync(int? CensusTractKey)
        {
            Dispatcher.BeginInvoke(() =>
            {
                var query = Context.GetCensusTractQuery();

                query = query.Where(x => x.CensusTractKey == CensusTractKey);

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, CensusTract_OnLoaded),
                    null);
            });
        }

        public void GetCensusMappingAsync(int? serviceLineGroupingKey)
        {
            Dispatcher.BeginInvoke(() =>
            {
                var query = Context.GetCensusTractMappingQuery();

                query = query.Where(x => x.ServiceLineGroupingKey == serviceLineGroupingKey);

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, CensusTractMapping_OnLoaded),
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
                Context.ServiceLines.Clear();

                var query = Context.GetServiceLineQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "ServiceLineKey":
                                query = query.Where(p => p.ServiceLineKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Name":
                                query = query.Where(p => p.Name.ToLower().Contains(searchvalue.ToLower()));
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

        public IEnumerable<ServiceLine> Items => Context.ServiceLines;

        PagedEntityCollectionView<ServiceLine> _ServiceLines;

        public PagedEntityCollectionView<ServiceLine> ServiceLines
        {
            get { return _ServiceLines; }
            set
            {
                if (_ServiceLines != value)
                {
                    _ServiceLines = value;
                    this.RaisePropertyChanged(p => p.ServiceLines);
                }
            }
        }

        public event EventHandler<EntityEventArgs<ServiceLine>> OnLoaded;

        public event EventHandler<EntityEventArgs<CensusTractMapping>> CensusTractMapping_OnLoaded;

        public event EventHandler<EntityEventArgs<CensusTract>> CensusTract_OnLoaded;

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
            ServiceLines.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }

        public System.Threading.Tasks.Task<bool> ValidateServiceLineAsync(ServiceLine serviceLine)
        {
            var asyncValidationResultList = new Utility.ValidationResults(); // new List<ValidationResult>();

            //NOTE: async-server side functions coded to return true if the error condition exists
            return Context.ServiceLineExists(
                    Virtuoso.Services.Authentication.WebContext.Current.User.TenantID,
                    serviceLine.FinancialSystemUnitID,
                    serviceLine.ServiceLineKey)
                .AsTask()
                .ContinueWith(t =>
                {
                    var operation = t.Result;
                    if (!operation.HasError && operation.Value)
                    {
                        asyncValidationResultList.AddSingle(new ValidationResult("Duplicate ID's are not permitted.",
                            new[] { "FinancialSystemUnitID" }));
                    }

                    return t.Result.Value;
                })
                .ContinueWith(
                    task =>
                    {
                        if (task.IsFaulted)
                        {
                            return false;
                        }

                        //Add cached errors to entity on the UI thread
                        asyncValidationResultList.ForEach(error => { serviceLine.ValidationErrors.Add(error); });
                        return !(task.Result);
                    },
                    System.Threading.CancellationToken.None,
                    System.Threading.Tasks.TaskContinuationOptions.None,
                    Client.Utils.AsyncUtility.TaskScheduler);
        }
    }
}