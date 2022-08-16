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
    [Export(typeof(ISurvivorService))]
    public class SurvivorService : PagedModelBase, ISurvivorService
    {
        public VirtuosoDomainContext Context { get; set; }

        public SurvivorService()
        {
            Context = new VirtuosoDomainContext();
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

        #region BereavementActivity Members

        public void Add(Survivor entity)
        {
            Context.Survivors.Add(entity);
        }

        public void Remove(Survivor entity)
        {
            Context.Survivors.Remove(entity);
        }

        public void Remove(SurvivorEncounter entity)
        {
            Context.SurvivorEncounters.Remove(entity);
        }

        public void Remove(SurvivorEncounterRisk entity)
        {
            Context.SurvivorEncounterRisks.Remove(entity);
        }

        public void Remove(SurvivorPlanActivity entity)
        {
            Context.SurvivorPlanActivities.Remove(entity);
        }

        public void Remove(SurvivorPlanActivityDocument entity)
        {
            Context.SurvivorPlanActivityDocuments.Remove(entity);
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
        }

        public void GetSearchAsync(bool isSystemSearch)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.Survivors.Clear();

                var query = Context.GetSurvivorQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "SurvivorKey":
                                query = query.Where(p => p.SurvivorKey == Convert.ToInt32(searchvalue));
                                break;
                        }
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

        public void GetSurvivorSearchAsync(int? BereavementSourceKey, int? BereavementLocationKey, string LastName,
            string FirstName, string MiddleInitial, int? GenderKey, bool IncludeDisenrolled)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.Survivors.Clear();


                var query = Context.GetSurvivorForSearchQuery(BereavementSourceKey, BereavementLocationKey, LastName,
                    FirstName, MiddleInitial, GenderKey, IncludeDisenrolled);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public void GetDeceasedSearchAsync(int? BereavementSourceKey, int? BereavementLocationKey, string LastName,
            string FirstName, string MiddleInitial, int? GenderKey, DateTime? DeathDate, string MRN,
            bool IncludeDeceasedContacts, DateTime? DeceasedAfterDate)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.DeceasedSearches.Clear();


                var query = Context.GetDeceasedForSearchQuery(BereavementSourceKey, BereavementLocationKey, LastName,
                    FirstName, MiddleInitial, GenderKey, DeathDate, MRN, IncludeDeceasedContacts, DeceasedAfterDate);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoadedDeceasedSearch),
                    null);
            });
        }

        public void GetSurvivorForMaintAsync(bool isNewSurvivor, int? survivorKey, int? patientKey,
            int? patientContactKey)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.Survivors.Clear();

                var query = Context.GetSurvivorForMaintQuery(isNewSurvivor, survivorKey, patientKey, patientContactKey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoadedSurvivorForMaint),
                    null);
            });
        }

        public void GetDocumentsForPlanActivityAsync(int survivorPlanActivityKey)
        {
            Dispatcher.BeginInvoke(() =>
            {
                var query = Context.GetDocumentsForPlanActivityQuery(survivorPlanActivityKey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.MergeIntoCurrent,
                    g => HandleEntityResults(g, OnLoadedDocumentsForPlanActivity),
                    null);
            });
        }

        public void GetSurvivorPlanActivityForWorklistAsync(int? BereavementSourceKey, int? BereavementLocationKey,
            int? EventTypeKey, string ActivityDescription, DateTime? StartDate, DateTime? EndDate)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.Survivors.Clear();
                Context.SurvivorPlanActivities.Clear();


                var query = Context.GetSurvivorPlanActivityForWorklistQuery(BereavementSourceKey,
                    BereavementLocationKey, EventTypeKey, ActivityDescription, StartDate, EndDate);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoadedGetSurvivorPlanActivityForWorklist),
                    null);
            });
        }

        public event Action<InvokeOperation<byte[]>> GetCSVBereavementSurvivorLabelsReturned;

        public byte[] GetCSVBereavementSurvivorLabels(int? BereavementSourceKey, int? BereavementLocationKey,
            int? EventTypeKey, string ActivityDescription, DateTime? StartDate, DateTime? EndDate)
        {
            Context.GetCSVBereavementSurvivorLabels(BereavementSourceKey, BereavementLocationKey, EventTypeKey,
                ActivityDescription, StartDate, EndDate, GetCSVBereavementSurvivorLabelsReturned, null);
            return null;
        }

        public void GetAsync()
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.Survivors.Clear();

                var query = Context.GetSurvivorQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "SurvivorKey":
                                query = query.Where(p => p.SurvivorKey == Convert.ToInt32(searchvalue));
                                break;
                        }
                    }
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

        public IEnumerable<Survivor> Items => Context.Survivors;
        public IEnumerable<Survivor> Survivors => Context.Survivors;

        //PagedEntityCollectionView<Survivor> _Survivors;
        //public PagedEntityCollectionView<Survivor> Survivors
        //{
        //    get { return _Survivors; }
        //    set
        //    {
        //        if (_Survivors != value)
        //        {
        //            _Survivors = value;
        //            this.RaisePropertyChanged(p => p.Survivors);
        //        }
        //    }
        //}

        public event EventHandler<EntityEventArgs<Survivor>> OnLoaded;
        public event EventHandler<EntityEventArgs<DeceasedSearch>> OnLoadedDeceasedSearch;
        public event EventHandler<EntityEventArgs<Survivor>> OnLoadedSurvivorForMaint;
        public event EventHandler<EntityEventArgs<SurvivorPlanActivityDocument>> OnLoadedDocumentsForPlanActivity;
        public event EventHandler<EntityEventArgs<SurvivorPlanActivity>> OnLoadedGetSurvivorPlanActivityForWorklist;

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
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}