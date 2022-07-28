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
    [Export(typeof(IGuardAreaService))]
    public class GuardAreaService : PagedModelBase, IGuardAreaService
    {
        public VirtuosoDomainContext Context { get; set; }

        PagedEntityCollectionView<GuardArea> _guardAreas;

        public PagedEntityCollectionView<GuardArea> GuardAreas
        {
            get { return _guardAreas; }
            set
            {
                if (_guardAreas != value)
                {
                    _guardAreas = value;
                    this.RaisePropertyChanged(p => p.GuardAreas);
                }
            }
        }
        
        public static List<SearchParameter> StaticSearchParameters;

        public GuardAreaService()
        {
            Context = new VirtuosoDomainContext();
            GuardAreas = new PagedEntityCollectionView<GuardArea>(Context.GuardAreas, this);
            Context.PropertyChanged += Context_PropertyChanged;
        }

        #region IModelDataService<GuardArea> Members

        public void Add(GuardArea entity)
        {
            Context.GuardAreas.Add(entity);
        }

        public void Remove(GuardArea entity)
        {
            if (Context.GuardAreas.Contains(entity))
            {
                Context.GuardAreas.Remove(entity);
            }
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
        }

        public bool ContextHasChanges => Context.HasChanges;

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
                Context.GuardAreas.Clear();

                var query = Context.GetGuardAreaQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "DisciplineKey":
                                query = query.Where(p => p.GuardAreaKey == Convert.ToInt32(searchvalue));
                                break;
                            case "GuardAreaID":
                                query = query.Where(p => p.GuardAreaID.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "StateCode":
                                query = query.Where(p => p.StateCode == Convert.ToInt32(searchvalue));
                                break;
                            case "ZipCode":
                                query = query.Where(p => p.ZipCode.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Plus4":
                                query = query.Where(p => p.Plus4.ToLower().Contains(searchvalue.ToLower()));
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
                Context.GuardAreas.Clear();

                var query = Context.GetGuardAreaQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "GuardAreaKey":
                                query = query.Where(p => p.GuardAreaKey == Convert.ToInt32(searchvalue));
                                break;
                            case "GuardAreaID":
                                query = query.Where(p => p.GuardAreaID.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "StateCode":
                                query = query.Where(p => p.StateCode == Convert.ToInt32(searchvalue));
                                break;
                            case "ZipCode":
                                query = query.Where(p => p.ZipCode.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Plus4":
                                query = query.Where(p => p.Plus4.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Inactive":
                                bool inactive = Convert.ToBoolean(searchvalue);
                                query = query.Where(p => p.Inactive == inactive);
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

        public IEnumerable<GuardArea> Items => Context.GuardAreas;

        public event EventHandler<EntityEventArgs<GuardArea>> OnLoaded;

        public event EventHandler<ErrorEventArgs> OnSaved;

        public bool SaveAllAsync()
        {
            var open_or_invalid = OpenOrInvalidObjects(Context);
            if (open_or_invalid)
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

        void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public void Cleanup()
        {
            GuardAreas.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}