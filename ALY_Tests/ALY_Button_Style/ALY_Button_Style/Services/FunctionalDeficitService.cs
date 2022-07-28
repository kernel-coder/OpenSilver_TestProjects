#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using GalaSoft.MvvmLight;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IFunctionalDeficitService : IModelDataService<FunctionalDeficit>, ICleanup
    {
        PagedEntityCollectionView<FunctionalDeficit> FunctionalDeficits { get; }
        void Remove(QuestionFunctionalDeficit entity);
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IFunctionalDeficitService))]
    public class FunctionalDeficitService : PagedModelBase, IFunctionalDeficitService
    {
        public VirtuosoDomainContext Context { get; set; }

        public FunctionalDeficitService()
        {
            Context = new VirtuosoDomainContext();
            FunctionalDeficits = new PagedEntityCollectionView<FunctionalDeficit>(Context.FunctionalDeficits, this);
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

        #region IModelDataService<FunctionalDeficit> Members

        public void Add(FunctionalDeficit entity)
        {
            Context.FunctionalDeficits.Add(entity);
        }

        public void Remove(FunctionalDeficit entity)
        {
            Context.FunctionalDeficits.Remove(entity);
        }

        public void Remove(QuestionFunctionalDeficit entity)
        {
            Context.QuestionFunctionalDeficits.Remove(entity);
        }

        public void GetAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.FunctionalDeficits.Clear();

                var query = Context.GetFunctionalDeficitQuery();

                if (SearchParameters.Count > 0)
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "FunctionalDeficitKey":
                                query = query.Where(p => p.FunctionalDeficitKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Code":
                                query = query.Where(p => p.Code.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Description":
                                query = query.Where(p => p.Description.ToLower().Contains(searchvalue.ToLower()));
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

                query.IncludeTotalCount = false;

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

        public IEnumerable<FunctionalDeficit> Items => Context.FunctionalDeficits;

        PagedEntityCollectionView<FunctionalDeficit> _FunctionalDeficits;

        public PagedEntityCollectionView<FunctionalDeficit> FunctionalDeficits
        {
            get { return _FunctionalDeficits; }
            set
            {
                if (_FunctionalDeficits != value)
                {
                    _FunctionalDeficits = value;
                    this.RaisePropertyChanged(p => p.FunctionalDeficits);
                }
            }
        }

        public event EventHandler<EntityEventArgs<FunctionalDeficit>> OnLoaded;

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

        #region IModelDataService<FunctionalDeficitElement> Members

        //Used by global search dialog
        public void GetSearchAsync(bool isSystemSearch)
        {
            //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
            //          when isSystemSearch == false, then Inactive checkbox removed from search criteria; however 
            //          we want to always assume that it is checked - e.g. add Inactive==false to query.

            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Clear();

                var query = Context.GetFunctionalDeficitQuery();

                if (SearchParameters.Count > 0)
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "FunctionalDeficitKey":
                                query = query.Where(p => p.FunctionalDeficitKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Code":
                                query = query.Where(p => p.Code.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Description":
                                query = query.Where(p => p.Description.ToLower().Contains(searchvalue.ToLower()));
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
                    g => HandleEntityResults(g, OnLoaded, false),
                    null
                );
            });
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
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
            FunctionalDeficits.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}