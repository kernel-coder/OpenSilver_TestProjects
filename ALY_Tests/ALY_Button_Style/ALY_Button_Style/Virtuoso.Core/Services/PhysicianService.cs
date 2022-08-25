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
    [Export(typeof(IPhysicianService))]
    public class PhysicianService : PagedModelBase, IPhysicianService
    {
        public VirtuosoDomainContext Context { get; set; }

        public PhysicianService()
        {
            Context = new VirtuosoDomainContext();
            Physicians = new PagedEntityCollectionView<Physician>(Context.Physicians, this);
            Context.PropertyChanged += Context_PropertyChanged;
        }

        public PhysicianService(VirtuosoDomainContext ctx)
        {
            Context = ctx;
            Physicians = new PagedEntityCollectionView<Physician>(Context.Physicians, this);
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

        #region IModelDataService<Physician> Members

        public void Add(Physician entity)
        {
            Context.Physicians.Add(entity);
        }

        public void Remove(Physician entity)
        {
            Context.Physicians.Remove(entity);
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
                Context.Physicians.Clear();

                var facilityTypeKey = SearchValue("FacilityTypeKey");
                var facilityKey = SearchValue("FacilityKey");
                var facilityBranchKey = SearchValue("FacilityBranchKey");
                var includeInactive = SearchValueBool("Inactive");

                var query = Context.GetPhysicianForSearchQuery(facilityTypeKey, facilityKey, facilityBranchKey,
                    includeInactive);

                if (SearchParameters.Any())
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
                                query = query.Where(p => p.LastName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "FirstName":
                                query = query.Where(p => p.FirstName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "FacilityKey":
                                if (Convert.ToInt32(searchvalue) > 0)
                                {
                                    query = query.Where(p => p.FacilityKey == Convert.ToInt32(searchvalue));
                                }

                                break;
                            case "NPI":
                                query = query.Where(p => p.NPI.ToLower().Contains(searchvalue.ToLower()));
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

                query.IncludeTotalCount = true;

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
                Context.Physicians.Clear();

                var facilityKey = 0; //DS 05062014 
                var facilityTypeKey = 0; //DS 04252014 
                var facilityBranchKey = 0; //DS 04252014 
                var includeInactive = true;

                var query = Context.GetPhysicianForMaintQuery(facilityTypeKey, facilityKey, facilityBranchKey,
                    includeInactive);
                //DS 04252014 var query = Context.GetPhysicianQuery();

                if (SearchParameters.Any())
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
                                query = query.Where(p => p.LastName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "FirstName":
                                query = query.Where(p => p.FirstName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Facility":
                                query = query.Where(p => p.FacilityKey == Convert.ToInt32(searchvalue));
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
                    //DS 0507      query = query.Skip(PageSize * PageIndex);
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

        public IEnumerable<Physician> Items => Context.Physicians;

        PagedEntityCollectionView<Physician> _Physicians;

        public PagedEntityCollectionView<Physician> Physicians
        {
            get { return _Physicians; }
            set
            {
                if (_Physicians != value)
                {
                    _Physicians = value;
                    this.RaisePropertyChanged(p => p.Physicians);
                }
            }
        }

        public event EventHandler<EntityEventArgs<Physician>> OnLoaded;

        public event EventHandler<ErrorEventArgs> OnSaved;

        public bool SaveAllAsync()
        {
            //J.E.???  TODO: who put this code in here and why is it needed?
            foreach (var phone in Context.PhysicianPhones)
                if (phone.Validate() == false)
                {
                    //var PROBLEM = "how got here?";
                    if (phone.EntityState == EntityState.Detached)
                    {
                        Context.PhysicianPhones.Remove(phone);
                    }
                }

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
                Context.SubmitChanges(g =>
                {
                    //HandleErrorResults(g, OnSaved);
                    HandleSubmitOperationResults(g, OnSaved);
                }, null);
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
        public System.Threading.Tasks.Task<bool> ValidatePhysicianAsync(Physician physician)
        {
            List<ValidationResult> asyncValidationResultList = new List<ValidationResult>();
            //NOTE: async-server side functions coded to return true if the error condition exists
            return Context.NPIExists(
                    Virtuoso.Services.Authentication.WebContext.Current.User.TenantID,
                    physician.NPI,
                    physician.PhysicianKey)
                .AsTask()
                .ContinueWith(t =>
                {
                    var operation = t.Result;
                    if (!operation.HasError && operation.Value)
                    {
                        asyncValidationResultList.Add(new ValidationResult("Duplicate NPIs are not permitted.",
                            new[] { "NPI" }));
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
                        asyncValidationResultList.ForEach(error => { physician.ValidationErrors.Add(error); });
                        return !(task.Result);
                    },
                    System.Threading.CancellationToken.None,
                    System.Threading.Tasks.TaskContinuationOptions.None,
                    Client.Utils.AsyncUtility.TaskScheduler);
        }

        public void Remove(PhysicianAddress entity)
        {
            Context.PhysicianAddresses.Remove(entity);
        }

        public void Remove(PhysicianAlternateID entity)
        {
            Context.PhysicianAlternateIDs.Remove(entity);
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
            Physicians.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}