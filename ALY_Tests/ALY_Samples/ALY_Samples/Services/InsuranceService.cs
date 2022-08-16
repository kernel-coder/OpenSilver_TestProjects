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
    [Export(typeof(IInsuranceService))]
    public class InsuranceService : PagedModelBase, IInsuranceService
    {
        public VirtuosoDomainContext Context { get; set; }

        public InsuranceService()
        {
            Context = new VirtuosoDomainContext();
            Insurances = new PagedEntityCollectionView<Insurance>(Context.Insurances, this);
            Context.PropertyChanged += Context_PropertyChanged;
        }

        #region Validation

        private object asyncValidationsLock = new object();
        private List<ValidationResult> asyncValidationResultList = new List<ValidationResult>();

        #endregion

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

        #region IModelDataService<Insurance> Members

        public void Add(Insurance entity)
        {
            Context.Insurances.Add(entity);
        }

        public void Add(InsuranceGroup entity)
        {
            Context.InsuranceGroups.Add(entity);
        }

        public void Add(InsuranceGroupDetail entity)
        {
            Context.InsuranceGroupDetails.Add(entity);
        }

        public void Remove(Insurance entity)
        {
            Context.Insurances.Remove(entity);
        }

        public void Remove(InsuranceAddress entity)
        {
            Context.InsuranceAddresses.Remove(entity);
        }

        public void Remove(InsuranceGroup entity)
        {
            Context.InsuranceGroups.Remove(entity);
        }

        public void Remove(InsuranceGroupDetail entity)
        {
            Context.InsuranceGroupDetails.Remove(entity);
        }

        public void Remove(InsuranceContact entity)
        {
            Context.InsuranceContacts.Remove(entity);
        }

        public void Remove(InsuranceCertDefinition entity)
        {
            Context.InsuranceCertDefinitions.Remove(entity);
        }

        public void Remove(InsuranceCertStatement entity)
        {
            Context.InsuranceCertStatements.Remove(entity);
        }

        public void Remove(InsuranceRecertStatement entity)
        {
            Context.InsuranceRecertStatements.Remove(entity);
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
        }

        public void GetInsuranceGroupByKeyAsync(int InsuranceGroupKey)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.InsuranceParameterJoins.Clear();

                var query = Context.GetInsuranceGroupByKeyQuery(InsuranceGroupKey);

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnInsuranceGroupsLoaded),
                    null);
            });
        }

        public void GetInsuranceGroupByNameAsync(string InsuranceGroupName, bool isInactive)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.InsuranceParameterJoins.Clear();
                Context.EntityContainer.Clear();

                var query = Context.GetInsuranceGroupByNameQuery(InsuranceGroupName, isInactive);

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnInsuranceGroupsLoaded),
                    null);
            });
        }

        public void GetInsuranceParametersForMaintAsync(int InsuranceKey)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.InsuranceParameterJoins.Clear();

                var query = Context.GetInsuranceParametersForMaintQuery(InsuranceKey);

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnParmDefsLoaded),
                    null);
            });
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
                Context.Insurances.Clear();

                var query = Context.GetInsuranceForSearchQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "InsuranceKey":
                                query = query.Where(p => p.InsuranceKey == Convert.ToInt32(searchvalue));
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

        public void GetAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.Insurances.Clear();

                var query = Context.GetInsuranceQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "InsuranceKey":
                                query = query.Where(p => p.InsuranceKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Name":
                                if (item.Condition.Equals("0"))
                                {
                                    query = query.Where(p => p.Name.ToLower().StartsWith(searchvalue.ToLower()));
                                }
                                else
                                {
                                    query = query.Where(p => p.Name.ToLower().Contains(searchvalue.ToLower()));
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

        public EntitySet<InsuranceGroup> InsuranceGroups => Context.InsuranceGroups;

        public EntitySet<InsuranceParameterJoin> InsuranceParameters => Context.InsuranceParameterJoins;
        public IEnumerable<Insurance> Items => Context.Insurances;

        PagedEntityCollectionView<Insurance> _Insurances;

        public PagedEntityCollectionView<Insurance> Insurances
        {
            get { return _Insurances; }
            set
            {
                if (_Insurances != value)
                {
                    _Insurances = value;
                    this.RaisePropertyChanged(p => p.Insurances);
                }
            }
        }

        public event EventHandler<EntityEventArgs<InsuranceParameterJoin>> OnParmDefsLoaded;

        public event EventHandler<EntityEventArgs<Insurance>> OnLoaded;
        public event EventHandler<EntityEventArgs<InsuranceGroup>> OnInsuranceGroupsLoaded;

        public event EventHandler OnCertRefreshed;
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


        public Task<int> RefreshCertPeriodsForInsuranceAsync(int insuranceKey)
        {
            //NOTE: async-server side functions coded to return true if the error condition exists
            return Context.RefreshCertCycles(insuranceKey, null, null)
                .AsTask()
                .ContinueWith(t => t.Result.Value)
                .ContinueWith(
                    task =>
                    {
                        if (task.IsFaulted)
                        {
                            return 0;
                        }

                        if (OnCertRefreshed != null)
                        {
                            OnCertRefreshed(this, EventArgs.Empty);
                        }

                        return task.Result;
                    },
                    System.Threading.CancellationToken.None,
                    TaskContinuationOptions.None,
                    Client.Utils.AsyncUtility.TaskScheduler);
        }

        public void GetEVVImplementationAsync()
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.EVVImplementationPOCOs.Clear();

                var query = Context.GetEVVImplementationQuery();

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    EVVImplementationLoaded,
                    null);
            });
        }

        private void EVVImplementationLoaded(LoadOperation<EVVImplementationPOCO> results)
        {
            HandleEntityResults(results, OnEVVImplementationLoaded);
            IsLoading = false;
        }

        public event EventHandler<EntityEventArgs<EVVImplementationPOCO>> OnEVVImplementationLoaded;

        public void RejectChanges()
        {
            Context.RejectChanges();
        }

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
            Insurances.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }

        #endregion
    }
}