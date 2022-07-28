using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.ServiceModel.DomainServices.Client;
using System.Windows.Threading;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;
using System.ComponentModel.DataAnnotations;

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IPhysicianAddressService))]
    public class PhysicianAddressService : PagedModelBase, IPhysicianAddressService
    {
        public VirtuosoDomainContext Context { get; set; }

        public PhysicianAddressService()
        {
            Context = new VirtuosoDomainContext();
            PhysicianAddresses = new PagedEntityCollectionView<PhysicianAddress>(Context.PhysicianAddresses, this);
            Context.PropertyChanged += Context_PropertyChanged;
        }

        public PhysicianAddressService(VirtuosoDomainContext ctx)
        {
            this.Context = ctx;
            PhysicianAddresses = new PagedEntityCollectionView<PhysicianAddress>(this.Context.PhysicianAddresses, this);
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

            this.GetAsync();
        }

        #endregion

        #region IModelDataService<PhysicianAddresses> Members

        public void Add(PhysicianAddress entity)
        {
            Context.PhysicianAddresses.Add(entity);
        }

        public void Remove(PhysicianAddress entity)
        {
            Context.PhysicianAddresses.Remove(entity);
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
        }


        public void GetSearchAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.PhysicianAddresses.Clear();


                var facilityTypeKey =  SearchValue("FacilityTypeKey");
                var facilityKey = SearchValue("FacilityKey");
                var facilityBranchKey = SearchValue("FacilityBranchKey");
                var includeInactive = SearchValueBool("Inactive");

                var query = Context.GetPhysicianBranchesForSearchQuery(facilityTypeKey, facilityKey, facilityBranchKey, includeInactive);

                if (SearchParameters.Count > 0)
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "PhysicianKey":
                                query = query.Where(p => p.PhysicianKey == Convert.ToInt32(searchvalue));
                                break;
                            case "LastName":
                                query = query.Where(p => p.Physician.LastName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "FirstName":
                                query = query.Where(p => p.Physician.FirstName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Inactive":
                                bool inactive = Convert.ToBoolean(searchvalue);
                                if (!inactive)
                                    query = query.Where(p => p.Inactive == inactive);
                                break;
                        }
                    }
                }
                else
                    query = query.Where(p => p.Inactive == false);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load<PhysicianAddress>(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults<PhysicianAddress>(g, this.OnLoaded),
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
                Context.Physicians.Clear();

                var facilityKey = 0; //DS 05062014 
                var facilityTypeKey = 0; //DS 04252014 
                var facilityBranchKey = 0; //DS 04252014 
                var includeInactive = true;
                var query = Context.GetPhysicianBranchesForSearchQuery(facilityTypeKey, facilityKey, facilityBranchKey, includeInactive);
                if (SearchParameters.Count > 0)
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "PhysicianKey":
                                query = query.Where(p => p.PhysicianKey == Convert.ToInt32(searchvalue));
                                break;
                            case "LastName":
                                query = query.Where(p => p.Physician.LastName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "FirstName":
                                query = query.Where(p => p.Physician.FirstName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Inactive":
                                bool inactive = Convert.ToBoolean(searchvalue);
                                if (!inactive)
                                    query = query.Where(p => p.Inactive == inactive);
                                break;

                        }
                    }
                }
                else
                    query = query.Where(p => p.Inactive == false);

                query.IncludeTotalCount = true;

                if (PageSize > 0)
                {
                    query = query.Skip(PageSize * PageIndex);
                    query = query.Take(PageSize);
                }

                IsLoading = true;

                Context.Load<PhysicianAddress>(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults<PhysicianAddress>(g, this.OnLoaded), //DS0506
                    null);
            });

        }

        public IEnumerable<PhysicianAddress> Items { get { return this.Context.PhysicianAddresses; } }

        PagedEntityCollectionView<PhysicianAddress> _PhysicianAddresses;
        public PagedEntityCollectionView<PhysicianAddress> PhysicianAddresses
        {
            get { return _PhysicianAddresses; }
            set
            {
                if (_PhysicianAddresses != value)
                {
                    _PhysicianAddresses = value;
                    this.RaisePropertyChanged(p => p.PhysicianAddresses);
                }
            }
        }
        public event EventHandler<EntityEventArgs<PhysicianAddress>> OnLoaded; //DS 0506

        public event EventHandler<ErrorEventArgs> OnSaved;

        public bool SaveAllAsync()
        {
            //J.E.???  TODO: who put this code in here and why is it needed?
            foreach (var phone in Context.PhysicianPhones)
            {
                if (phone.Validate() == false)
                {
                    EntityState entity_state = phone.EntityState;
                    //var PROBLEM = "how got here?";
                    if (phone.EntityState == EntityState.Detached)
                        Context.PhysicianPhones.Remove(phone);                    
                }
            }

            var open_or_invalid = OpenOrInvalidObjects(Context);
            if (open_or_invalid)  //TODO: should we raise/return an error or something???
            {
                PendingSubmit = true;
                return false;
            }
            else
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


        public int SearchValue(string searchParameterName)
        {
            var searchValue = 0;

            var parm = SearchParameters.FirstOrDefault(i => i.Field.Equals(searchParameterName));

            if (parm != null)
            {
                searchValue = Convert.ToInt32(parm.Value + "");
            }
            return searchValue;

        }

        public bool SearchValueBool(string searchParameterName)
        {
            var searchValue = false;

            var parm = SearchParameters.FirstOrDefault(i => i.Field.Equals(searchParameterName));

            if (parm != null)
            {
                searchValue = Convert.ToBoolean(parm.Value + "");
            }
            return searchValue;

        }

        #endregion

        #region IPhysicianService Members

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Return TRUE if no server-side validation errors
        //
        // NOTE: async-server side functions coded to return true if the error condition exists
        ///////////////////////////////////////////////////////////////////////////////////////////////
        public System.Threading.Tasks.Task<bool> ValidatePhysicianAsync(PhysicianAddress physicianAddress)  //DS0506
        {
            return null;
            /*
            List<ValidationResult> asyncValidationResultList = new List<ValidationResult>();
            //NOTE: async-server side functions coded to return true if the error condition exists
            return this.Context.NPIExists(
                Virtuoso.Services.Authentication.WebContext.Current.User.TenantID,
                physicianAddress.NPI,
                physician.PhysicianKey)
                .AsTask()
                .ContinueWith((t) =>
                {
                    var operation = t.Result;
                    if (!operation.HasError && operation.Value)
                        asyncValidationResultList.Add(new ValidationResult("Duplicate NPIs are not permitted.", new string[] { "NPI" }));
                    return t.Result.Value;
                })
                .ContinueWith<bool>(
                task =>
                {
                    if (task.IsFaulted)
                        return false;
                    else
                    {
                        //Add cached errors to entity on the UI thread
                        asyncValidationResultList.ForEach((error) =>
                        {
                            ((Entity)physicianAddress).ValidationErrors.Add(error);
                        });
                        return !(task.Result);
                    }
                },
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.None,
                System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
             * */
        }
       
        public void Remove(PhysicianEmail entity)
        {
            Context.PhysicianEmails.Remove(entity);
        }
        public void Remove(PhysicianLicense entity)
        {
            Context.PhysicianLicenses.Remove(entity);
        }
        public void Remove(PhysicianPhone entity)
        {
            Context.PhysicianPhones.Remove(entity);
        }

        #endregion

        public bool ContextHasChanges { get { return Context.HasChanges; } }

        void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges")) { this.RaisePropertyChanged("ContextHasChanges"); }
        }

        public void Cleanup()
        {
            Context.PropertyChanged -= Context_PropertyChanged;
            //context = null; //this may cause errors with DetailControlBase/ChildControlBase
        }
    }
}
