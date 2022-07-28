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
    [Export(typeof(INonServiceTypeService))]
    public class NonServiceTypeService : PagedModelBase, INonServiceTypeService
    {
        public VirtuosoDomainContext Context { get; set; }

        public NonServiceTypeService()
        {
            Context = new VirtuosoDomainContext();
            NonServiceTypes = new PagedEntityCollectionView<NonServiceType>(Context.NonServiceTypes, this);
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

        #region IModelDataService<NonServiceType> Members

        public void Add(NonServiceType entity)
        {
            Context.NonServiceTypes.Add(entity);
        }

        public void Remove(NonServiceType entity)
        {
            Context.NonServiceTypes.Remove(entity);
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
                Context.NonServiceTypes.Clear();

                string NonServiceTypeID = null;
                string ShortDescription = null;
                string Description = null;

                var parm = SearchParameters.Where(i => i.Field.Equals("NonServiceTypeID")).FirstOrDefault();

                if (parm != null)
                {
                    NonServiceTypeID = parm.Value;
                }

                parm = SearchParameters.Where(i => i.Field.Equals("ShortDescription")).FirstOrDefault();

                if (parm != null)
                {
                    ShortDescription = parm.Value;
                }

                parm = SearchParameters.Where(i => i.Field.Equals("Description")).FirstOrDefault();

                if (parm != null)
                {
                    Description = parm.Value;
                }

                var query = Context.GetNonServiceTypeForSearchQuery(NonServiceTypeID, ShortDescription, Description);

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
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

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public bool ContextHasChanges => Context.HasChanges;

        public void GetAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.NonServiceTypes.Clear();

                var query = Context.GetNonServiceTypeQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "NonServiceTypeKey":
                                query = query.Where(p => p.NonServiceTypeKey == Convert.ToInt32(searchvalue));
                                break;
                            case "NonServiceTypeID":
                                query = query.Where(p => p.NonServiceTypeID.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "ShortDescription":
                                query = query.Where(p => p.ShortDescription.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Description":
                                query = query.Where(p => p.Description.ToLower().Contains(searchvalue.ToLower()));
                                break;
                        }
                    }
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

        public IEnumerable<NonServiceType> Items => Context.NonServiceTypes;

        PagedEntityCollectionView<NonServiceType> _NonServiceTypes;

        public PagedEntityCollectionView<NonServiceType> NonServiceTypes
        {
            get { return _NonServiceTypes; }
            set
            {
                if (_NonServiceTypes != value)
                {
                    _NonServiceTypes = value;
                    this.RaisePropertyChanged(p => p.NonServiceTypes);
                }
            }
        }

        public event EventHandler<EntityEventArgs<NonServiceType>> OnLoaded;

        public event EventHandler<ErrorEventArgs> OnSaved;

        public bool SaveAllAsync()
        {
            var open_edits =
                Context.NonServiceTypes.Any(e => e.IsEditting);

            var invalid_objects = Context.NonServiceTypes.Any(e => e.IsModified && e.Validate() == false);

            if (open_edits || invalid_objects)
            {
                return false; //TODO: should we raise/return an error or something???
            }

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

        public void Cleanup()
        {
            NonServiceTypes.Cleanup();
        }
    }
}