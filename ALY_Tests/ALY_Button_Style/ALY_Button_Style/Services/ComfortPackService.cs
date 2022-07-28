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
using static System.Diagnostics.Debug;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IComfortPackService))]
    public class ComfortPackService : PagedModelBase, IComfortPackService
    {
        public VirtuosoDomainContext Context { get; set; }

        public ComfortPackService()
        {
            Context = new VirtuosoDomainContext();
            ComfortPacks = new PagedEntityCollectionView<ComfortPack>(Context.ComfortPacks, this);
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

        #region ComfortPack Members

        public void Add(ComfortPack entity)
        {
            Context.ComfortPacks.Add(entity);
        }

        public void Remove(ComfortPack entity)
        {
            Context.ComfortPacks.Remove(entity);
        }

        public void Add(ComfortPackDiscipline entity)
        {
            Context.ComfortPackDisciplines.Add(entity);
        }

        public void Remove(ComfortPackDiscipline entity)
        {
            Context.ComfortPackDisciplines.Remove(entity);
        }

        public void Add(ComfortPackMedication entity)
        {
            Context.ComfortPackMedications.Add(entity);
        }

        public void Remove(ComfortPackMedication entity)
        {
            Context.ComfortPackMedications.Remove(entity);
        }

        public void Add(ComfortPackSupply entity)
        {
            Context.ComfortPackSupplies.Add(entity);
        }

        public void Remove(ComfortPackSupply entity)
        {
            Context.ComfortPackSupplies.Remove(entity);
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
                Context.ComfortPacks.Clear();

                var query = Context.GetComfortPackForSearchQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "ComfortPackKey":
                                query = query.Where(p => p.ComfortPackKey == Convert.ToInt32(searchvalue));
                                break;
                            case "ComfortPackCode":
                                query = query.Where(p => p.ComfortPackCode.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "ComfortPackDescription":
                                query = query.Where(p =>
                                    p.ComfortPackDescription.ToLower().Contains(searchvalue.ToLower()));
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
                Context.ComfortPacks.Clear();
                Context.ComfortPackDisciplines.Clear();
                Context.ComfortPackMedications.Clear();
                Context.ComfortPackSupplies.Clear();

                var query = Context.GetComfortPackQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "ComfortPackKey":
                                query = query.Where(p => p.ComfortPackKey == Convert.ToInt32(searchvalue));
                                break;
                            case "ComfortPackCode":
                                query = query.Where(p => p.ComfortPackCode.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "ComfortPackDescription":
                                query = query.Where(p =>
                                    p.ComfortPackDescription.ToLower().Contains(searchvalue.ToLower()));
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

        public IEnumerable<ComfortPack> Items => Context.ComfortPacks;

        PagedEntityCollectionView<ComfortPack> _ComfortPacks;

        public PagedEntityCollectionView<ComfortPack> ComfortPacks
        {
            get { return _ComfortPacks; }
            set
            {
                if (_ComfortPacks != value)
                {
                    _ComfortPacks = value;
                    this.RaisePropertyChanged(p => p.ComfortPacks);
                }
            }
        }

        public event EventHandler<EntityEventArgs<ComfortPack>> OnLoaded;

        public event EventHandler<ErrorEventArgs> OnSaved;

        public bool SaveAllAsync()
        {
            WriteLine($"[4000] {nameof(ComfortPackService)}: {nameof(SaveAllAsync)}");

            var open_or_invalid = OpenOrInvalidObjects(Context, tag: $"{nameof(ComfortPackService)}", log: true);
            if (open_or_invalid) //TODO: should we raise/return an error or something???
            {
                PendingSubmit = true;
                WriteLine(
                    $"[4000] {nameof(ComfortPackService)}: {nameof(SaveAllAsync)}.  Early return because of pending submit.  Not submitting changes.");
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
            ComfortPacks.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}