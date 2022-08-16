#region Usings

using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Occasional;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IDynamicFormService : IMultiModelDataService, ICleanup
    {
        DomainContext GetContext();

        void GetAsyncByKeys(int patientkey, int admissionkey, int formkey, int taskkey, bool takeOffline);
        Form CurrentForm { get; }
        int CurrentFormKey { get; set; }
        Patient CurrentPatient { get; }
        Admission CurrentAdmission { get; }
        List<AuthOrderTherapyPOCO_CView> CurrentAuthMappings { get; }
        List<ChangeHistory> OrdersTrackingChangeHistory { get; }

        Task CurrentTask { get; }

        event EventHandler<SHPAlertsRequestArgs> OnSHPAlertsRequestLoaded;

        void getSHPAlertsRequest(string userid, int? OASISHeaderKey, string B1, string casemanager, string clinician,
            string referralsource, string team, string reference, string primarypayername,
            string priorinpatientfacility, object userstate);

        void RemoveTasksAfterDischarge(DateTime dischargedate, int? disciplinekey = null,
            DateTime? mostRecentResumptionDate = null);

        void EndDateAllFCDOrdersForDiscipline(int? disciplineKey, DateTime DischargeDate, bool endDateAll);
        void AddChangeHistory(ChangeHistory entity);
        void RemoveOrderEntryCoSignature(OrderEntryCoSignature entity);
        void RemoveSignature(EncounterSignature entity);
        void RemoveEncounterReview(EncounterReview entity);
        void RemoveEncounterOasis(EncounterOasis entity);
        void RemoveEncounterAddendum(EncounterAddendum entity);
        void RemoveEncounterNarrative(EncounterNarrative entity);
        void RemoveAdmissionConsent(AdmissionConsent entity);
        void RemoveEncounterHospiceElectionAddendumMedication(EncounterHospiceElectionAddendumMedication entity);
        void RemoveEncounterHospiceElectionAddendumService(EncounterHospiceElectionAddendumService entity);
        void RemoveAdmissionDisciplineFrequency(AdmissionDisciplineFrequency entity);
        void RemoveEncounterSupervision(EncounterSupervision entity);
        void RemoveEncounterNPWT(EncounterNPWT entity);
        void RemovePatientDiagnosisComment(PatientDiagnosisComment entity);
        void RemovePatientTranslator(PatientTranslator entity);
        void DischargeAllDisciplines(DateTime dischargeDate, int? dischargeReason, string SummaryOfCareNarrative);

        void Remove(EncounterAllergy entity);
        void Remove(EncounterDiagnosis entity);
        void Remove(EncounterDiagnosisComment entity);
        void Remove(EncounterLevelOfCare entity);
        void Remove(EncounterPainLocation entity);
        void Remove(EncounterIVSite entity);
        void Remove(EncounterMedication entity);
        void Remove(EncounterWoundSite entity);
        void Remove(EncounterLab entity);
        void Remove(EncounterGoalElement entity);
        void Remove(EncounterGoal entity);
        void Remove(EncounterDisciplineFrequency entity);
        void Remove(EncounterStartDisciplineFrequency entity);
        void Remove(EncounterConsent entity);
        void Remove(EncounterEquipment entity);
        void Remove(EncounterData entity);
        void Remove(EncounterSupply entity);
        void Remove(EncounterVisitFrequency entity);

        void RefreshAdmissionCoverage(int AdmissionKey);
        void RefreshAdmissionPhysician(int AdmissionKey);
        void RefreshPatientFacilityStay(int PatientKey);
        void RefreshPatientAddress(int PatientKey);

        Tuple<string, EntityChangeSet>[] CheckChanges();
        void HavenValidateB1RecordAsync(string B1Record, string PPSModelVersion, int OasisVersionKey);

        void CommitAllOpenEdits();
        bool OpenOrInvalidObjects(string tag = "", bool log = false);
        void ConvertOASISB1ToC1Fixed(string B1Record);
        byte[] ConvertOASISB1ToC1PPS(string B1Record, string PPSPlusVendorKey);

        byte[] GetSSRSPDFDynamicForm(int formKey, int patientKey, int encounterKey, int admissionKey,
            bool HideOasisQuestions);

        event Action<InvokeOperation<byte[]>> GetSSRSPDFDynamicFormReturned;

        byte[] GetSSRSPDFAdmissionCommunication(int admissioncommunicationKey, int patientKey, int admissionKey);
        event Action<InvokeOperation<byte[]>> GetSSRSPDFAdmissionCommunicationReturned;

        byte[] GetSSRSPDFAdmissionDocumentation(int admissiondocumentationKey, int patientKey, int admissionKey);
        event Action<InvokeOperation<byte[]>> GetSSRSPDFAdmissionDocumentationReturned;

        event Action<InvokeOperation<string>> ConvertOASISB1ToC1FixedReturned;
        event Action<InvokeOperation<byte[]>> ConvertOASISB1ToC1PPSReturned;
        event Action<InvokeOperation<HavenReturnWrapper>> CallHavenReturned;

        System.Threading.Tasks.Task Save(int taskKey, OfflineStoreType location);
        System.Threading.Tasks.Task Load(int taskKey, OfflineStoreType location);
        System.Threading.Tasks.Task LoadFromDashboard(int admissionKey, int taskKey, OfflineStoreType location);

        void Add<T>(T entity) where T : Entity;
    }
}