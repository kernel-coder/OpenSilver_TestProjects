#region Usings

using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface ISurvivorService : IModelDataService<Survivor>, ICleanup
    {
        void GetDeceasedSearchAsync(int? BereavementSourceKey, int? BereavementLocationKey, string LastName,
            string FirstName, string MiddleInitial, int? GenderKey, DateTime? DeathDate, string MRN,
            bool IncludeDeceasedContacts, DateTime? DeceasedAfterDate);

        event EventHandler<EntityEventArgs<DeceasedSearch>> OnLoadedDeceasedSearch;
        void GetSurvivorForMaintAsync(bool isNewSurvivor, int? survivorKey, int? patientKey, int? patientContactKey);
        event EventHandler<EntityEventArgs<Survivor>> OnLoadedSurvivorForMaint;
        void GetDocumentsForPlanActivityAsync(int survivorPlanActivityKey);
        event EventHandler<EntityEventArgs<SurvivorPlanActivityDocument>> OnLoadedDocumentsForPlanActivity;

        void GetSurvivorPlanActivityForWorklistAsync(int? BereavementSourceKey, int? BereavementLocationKey,
            int? EventTypeKey, string ActivityDescription, DateTime? StartDate, DateTime? EndDate);

        event EventHandler<EntityEventArgs<SurvivorPlanActivity>> OnLoadedGetSurvivorPlanActivityForWorklist;

        byte[] GetCSVBereavementSurvivorLabels(int? BereavementSourceKey, int? BereavementLocationKey,
            int? EventTypeKey, string ActivityDescription, DateTime? StartDate, DateTime? EndDate);

        event Action<InvokeOperation<byte[]>> GetCSVBereavementSurvivorLabelsReturned;

        void GetSurvivorSearchAsync(int? BereavementSourceKey, int? BereavementLocationKey, string LastName,
            string FirstName, string MiddleInitial, int? GenderKey, bool IncludeDisenrolled);

        IEnumerable<Survivor> Survivors { get; }
        
        void Remove(SurvivorEncounter entity);
        void Remove(SurvivorEncounterRisk entity);
        void Remove(SurvivorPlanActivity entity);
        void Remove(SurvivorPlanActivityDocument entity);
    }
}