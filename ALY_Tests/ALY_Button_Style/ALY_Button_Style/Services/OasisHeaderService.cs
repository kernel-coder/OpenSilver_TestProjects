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
    [Export(typeof(IOasisHeaderService))]
    public class OasisHeaderService : PagedModelBase, IOasisHeaderService
    {
        public VirtuosoDomainContext Context { get; set; }

        public OasisHeaderService()
        {
            Context = new VirtuosoDomainContext();
            OasisHeaders = new PagedEntityCollectionView<OasisHeader>(Context.OasisHeaders, this);
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

        #region IModelDataService<OasisHeader> Members

        public void Add(OasisHeader entity)
        {
            Context.OasisHeaders.Add(entity);
        }

        public void Remove(OasisHeader entity)
        {
            Context.OasisHeaders.Remove(entity);
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
                Context.OasisHeaders.Clear();

                var query = Context.GetOasisHeaderQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "OasisHeaderKey":
                                query = query.Where(p => p.OasisHeaderKey == Convert.ToInt32(searchvalue));
                                break;
                            case "OasisHeaderName":
                                query = query.Where(p => p.OasisHeaderName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "NPI":
                                query = query.Where(p => p.NPI.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "BranchIDNumber":
                                query = query.Where(p => p.BranchIDNumber.ToLower().Contains(searchvalue.ToLower()));
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
                Context.OasisHeaders.Clear();

                var query = Context.GetOasisHeaderQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "OasisHeaderKey":
                                query = query.Where(p => p.OasisHeaderKey == Convert.ToInt32(searchvalue));
                                break;
                            case "OasisHeaderName":
                                query = query.Where(p => p.OasisHeaderName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "NPI":
                                query = query.Where(p => p.NPI.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "BranchIDNumber":
                                query = query.Where(p => p.BranchIDNumber.ToLower().Contains(searchvalue.ToLower()));
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

        public IEnumerable<OasisHeader> Items => Context.OasisHeaders;

        PagedEntityCollectionView<OasisHeader> _OasisHeaders;

        public PagedEntityCollectionView<OasisHeader> OasisHeaders
        {
            get { return _OasisHeaders; }
            set
            {
                if (_OasisHeaders != value)
                {
                    _OasisHeaders = value;
                    this.RaisePropertyChanged(p => p.OasisHeaders);
                }
            }
        }

        public event EventHandler<EntityEventArgs<OasisHeader>> OnLoaded;

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
            OasisHeaders.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}