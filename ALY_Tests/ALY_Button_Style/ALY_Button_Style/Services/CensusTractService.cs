#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ICensusTractService))]
    public class CensusTractService : PagedModelBase, ICensusTractService
    {
        public VirtuosoDomainContext Context { get; set; }

        public CensusTractService()
        {
            Context = new VirtuosoDomainContext();
            CensusTracts = new PagedEntityCollectionView<CensusTract>(Context.CensusTracts, this);
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

        #region IModelDataService<CensusTract> Members

        public void Add(CensusTract entity)
        {
            Context.CensusTracts.Add(entity);
        }

        public void Remove(CensusTract entity)
        {
            Context.CensusTracts.Remove(entity);
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
                bool _Inactive = true;
                Context.RejectChanges();
                Context.CensusTracts.Clear();

                var query = Context.GetCensusTractQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "CensusTractKey":
                                query = query.Where(p => p.CensusTractKey == Convert.ToInt32(searchvalue));
                                break;

                            case "CensusTractID":
                                query = query.Where(p => p.CensusTractID.ToLower().Contains(searchvalue.ToLower()));
                                break;

                            case "CensusTractText":
                                query = query.Where(p => p.CensusTractText.ToLower().Contains(searchvalue.ToLower()));
                                break;

                            case "State":
                                var statecode = CodeLookupCache.GetKeyFromCode("STATE", searchvalue);
                                query = query.Where(p => p.StateCode == statecode);
                                break;

                            case "County":
                                query = query.Where(p => p.County.ToLower().Contains(searchvalue.ToLower()));
                                break;

                            case "ZipCode":
                                query = query.Where(p => p.ZipCode.ToLower().Contains(searchvalue.ToLower()));
                                break;

                            case "Inactive":
                                _Inactive = Convert.ToBoolean(item.Value);
                                break;
                        }
                    }

                    if (isSystemSearch == false)
                    {
                        //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
                        query = query.Where(p => p.Inactive == false);
                    }
                }

                if (_Inactive == false)
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
                Context.CensusTracts.Clear();

                var query = Context.GetCensusTractQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "CensusTractKey":
                                query = query.Where(p => p.CensusTractKey == Convert.ToInt32(searchvalue));
                                break;

                            case "CensusTractID":
                                query = query.Where(p => p.CensusTractID.ToLower().Contains(searchvalue.ToLower()));
                                break;

                            case "CensusTractText":
                                query = query.Where(p => p.CensusTractText.ToLower().Contains(searchvalue.ToLower()));
                                break;

                            case "State":
                                query = query.Where(p => p.ZipCode.ToLower().Contains(searchvalue.ToLower()));
                                break;

                            case "County":
                                query = query.Where(p => p.County.ToLower().Contains(searchvalue.ToLower()));
                                break;

                            case "ZipCode":
                                query = query.Where(p => p.ZipCode.ToLower().Contains(searchvalue.ToLower()));
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

        public IEnumerable<CensusTract> Items => Context.CensusTracts;

        PagedEntityCollectionView<CensusTract> _CensusTracts;

        public PagedEntityCollectionView<CensusTract> CensusTracts
        {
            get { return _CensusTracts; }
            set
            {
                if (_CensusTracts != value)
                {
                    _CensusTracts = value;
                    this.RaisePropertyChanged(p => p.CensusTracts);
                }
            }
        }

        public event EventHandler<EntityEventArgs<CensusTract>> OnLoaded;

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
            CensusTracts.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }

        public System.Threading.Tasks.Task<bool> ValidateCensusTractAsync(CensusTract censusTract)
        {
            List<ValidationResult> asyncValidationResultList = new List<ValidationResult>();
            //NOTE: async-server side functions coded to return true if the error condition exists
            return Context.CensusTractIDExists(
                    Virtuoso.Services.Authentication.WebContext.Current.User.TenantID,
                    censusTract.CensusTractID)
                .AsTask()
                .ContinueWith(t =>
                {
                    var operation = t.Result;
                    if (!operation.HasError && operation.Value)
                    {
                        asyncValidationResultList.Add(
                            new ValidationResult("Duplicate CensusTractID's are not permitted.",
                                new[] { "CensusTractID" }));
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
                        asyncValidationResultList.ForEach(error => { censusTract.ValidationErrors.Add(error); });
                        return !(task.Result);
                    },
                    System.Threading.CancellationToken.None,
                    System.Threading.Tasks.TaskContinuationOptions.None,
                    Client.Utils.AsyncUtility.TaskScheduler);
        }
    }
}