#region Usings

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IMedicationService
    {
        Task<PatientTeachingSheet> GetPatientTeachingSheetAsync(Patient patient, int MediSpanMedicationKey,
            string medicationName, int Language);

        Task<PatientScreening> GetPatientScreeningAsync(
            Patient patient,
            Encounter Encounter,
            List<PatientMedication> currentMedispanMedications,
            bool IncludeFutureMeds);
    }

    public interface IPatientService : IModelDataService<Patient>, IMedicationService, ICleanup
    {
        void
            SetLocationForMonitoring(string locationOverride);

        void HandleError<T>(Task<T> _task, string tag);

        void AddChangeHistory(ChangeHistory entity);
        event EventHandler<EntityEventArgs<InsuranceVerificationRequest>> OnInsuranceVerificationRequestLoaded;

        event EventHandler<EntityEventArgs<Admission>> OnPatientAdmissionRefreshLoaded;

        event EventHandler<EntityEventArgs<AdmissionCareCoordinatorHistoryPOCO>> OnGetAdmissionCareCoordinatorHistoryLoaded;

        event EventHandler<EntityEventArgs<Patient>> OnPatientAdmissionFullDetailsLoaded;

        event EventHandler<EntityEventArgs<PatientInsurance>> OnPatientInsuranceRefreshLoaded;

        event EventHandler<EntityEventArgs<AddressReturn>> OnAddressReturnLoaded;
        event EventHandler<ADFResponseEventArgs> OnADFVendorResponseLoaded;

        event EventHandler<ADFResponseEventArgs> OnADFVendorCountLoaded;

        event EventHandler<EntityEventArgs<PatientSearch>> OnSearchLoaded;
        event EventHandler<EntityEventArgs<Patient>> OnPatientRefreshLoaded;
        event EventHandler<EntityEventArgs<Admission>> OnAdmissionRefreshLoaded;

        byte[] GetPatientChartPrint(List<VirtuosoPrintRequestDetail> PRList);
        event Action<InvokeOperation<byte[]>> GetPatientChartPrintReturned;

        byte[] GetSSRSPDFDynamicForm(int formKey, int patientKey, int encounterKey, int admissionKey, bool HideOasisQuestions);

        event Action<InvokeOperation<byte[]>> GetSSRSPDFDynamicFormReturned;

        byte[] GetSSRSPDFAdmissionCommunication(int admissioncommunicationKey, int patientKey, int admissionKey);
        event Action<InvokeOperation<byte[]>> GetSSRSPDFAdmissionCommunicationReturned;

        byte[] GetSSRSPDFAdmissionDocumentation(int admissiondocumentationKey, int patientKey, int admissionKey);
        event Action<InvokeOperation<byte[]>> GetSSRSPDFAdmissionDocumentationReturned;

        byte[] GetSSRSPDFReportWithParameters(string ReportName, string Parameters);
        event Action<InvokeOperation<byte[]>> GetSSRSPDFReportWithParametersReturned;

        void GetPatientWithPatientMessagesAsync(int PatientKey);
        event EventHandler<EntityEventArgs<Patient>> OnPatientWithPatientMessagesLoaded;

        event EventHandler<EntityEventArgs<ReportArchive>> OnGetReportArchiveLoaded;
        event EventHandler<EntityEventArgs<Patient>> OnGetPatientsPharmacyRefillLoaded;
        event EventHandler<EntityEventArgs<Patient>> OnGetPatientsHospicePumpLoaded;
        event EventHandler<EntityEventArgs<AdmissionHospicePump>> OnGetAdmissionHospicePumpForAdmissionLoaded;
        event EventHandler<EntityEventArgs<AdmissionPharmacyRefill>> OnGetAdmissionPharmacyRefillForAdmissionLoaded;
        event EventHandler<EntityEventArgs<Tenant>> OnGetHospiceRefillImportForImportLoaded;
        event EventHandler<EntityEventArgs<TeamMeetingPOCO>> OnGetTeamMeetingWorkListLoaded;

        void GetTeamMeetingRosterWorkListAsync();
        event EventHandler<EntityEventArgs<TeamMeetingRosterPOCO>> OnGetTeamMeetingRosterWorkListLoaded;

        string SetTeamMeetingRosterSignatureData(int physicianKey, byte[] signatureData);
        event Action<InvokeOperation<string>> SetTeamMeetingRosterSignatureDataReturned;

        event EventHandler<EntityEventArgs<Encounter>> OnAdmissionEncounterRefreshLoaded;
        event EventHandler<BatchEventArgs> OnAdmissionEncounterAndServicesRefreshLoaded;
        event EventHandler<EntityEventArgs<AdmissionDocumentation>> OnAdmissionDocumentationRefreshLoaded;
        event EventHandler<MultiErrorEventArgs> OnMultiSaved;
        event EventHandler<EntityEventArgs<AdmissionDiagnosisGroup>> OnGetAdmissionDiagnosisGroupsForAdmissionLoaded;

        event EventHandler<EntityEventArgs<OrdersTrackingData>> OnTrackingDataLoaded;
        void GetOrdersTrackingDataAsync(bool GetVO, bool GetPOC, bool GetFaceToFace, bool GetCOTI, bool GetHospF2F);
        event EventHandler<MultiErrorEventArgs> OnRefreshCertCyclesAndPhysiciansLoaded;
        void GetOrdersTrackingByTrackingGroupAsync(int? orderTrackingGroupKey, string serviceLineKeys);
        event EventHandler<EntityEventArgs<OrdersTrackingDisplay>> OnOrdersTrackingLoaded;
        void GetOrdersTrackingChangeHistoryByKeyAsync(int ordersTrackingKey);
        event EventHandler<EntityEventArgs<ChangeHistory>> OnOrdersTrackingChangeHistoryLoaded;
        event EventHandler<EntityEventArgs<OrderTrackingGroup>> OnOrderTrackingGroupsLoaded;
        void GetOrderTrackingGroupForOrdersTrackingAsync();

        void GetDischargeTransferWorkListAsync();
        event EventHandler<EntityEventArgs<DischargeTransferTask>> OnDischargeTransferWorkListLoaded;

        void GetPDGMWorkListAsync(string insuranceKeys);
        event EventHandler<EntityEventArgs<PDGMWorkListPOCO>> OnPDGMWorkListLoaded;

        void GenerateBatchDocumentAsync(int InterimOrderBatchKey, bool ReturnDocument);
        event Action<InvokeOperation<byte[]>> OnGenerateBatchDocumentAsyncReturned;
        void UpdateBatchStatusToSentAsync(string InterimOrderBatchKeys);
        event Action<InvokeOperation<bool>> OnUpdateBatchStatusToSentAsyncReturned;

        void RefreshCertCyclesAndPhysicians(int admissionKey, bool refresh_cert_cycles,
            bool refresh_admission_physician);
        
        Task<string> GetMRNAsync();
        Task<bool> ValidatePatientAsync(Patient patient);

        Task<bool> ValidatePatientAddressAsync(PatientAddress patientAddress);
        
        void GetMelissaVerification(string Mode, string ServiceName, string FirstName, string LastName,
            string AddressLine1, string AddressLine2, string City, string State, string PostalCode);

        Task<IEnumerable<InsuranceVerificationRequest>> GenerateImmediateEligibilityCheckAsync(int TenantID, int PatientKey, int InsuranceKey, int PatientInsuranceKey, Guid CreatedBy);

        Task<IEnumerable<InsuranceVerificationRequest>> GetInsuranceVerificationRequestsAsync(int TenantID);

        Task<IEnumerable<PatientInsurance>> ProcessInsuranceVerificationRequest(int TenantID, bool UpdatePatientInsurance, int InsuranceVerificationRequestKey, bool WasVerified, Guid UpdatedBy);

        Task<IEnumerable<PatientInsurance>> RefreshPatientInsuranceAsync(int patientkey);
        Task<IEnumerable<InsuranceVerificationRequest>> RefreshPatientInsuranceVerificationRequestAsync(int patientkey);

        void getADFVendorResponse(int adfkey, object me);

        void getADFVendorCount(int adkey);

        void GetReportArchiveAsync();

        void GetPatientsPharmacyRefillAsync(int? PatientKey, int? AdmissionKey, int? PatientMedicationKey, DateTime? DateFilled);

        void GetPatientsHospicePumpAsync(int? PatientKey, int? AdmissionKey, int? PatientMedicationKey, DateTime? DateFilled);

        void GetAdmissionPharmacyRefillForAdmissionAsync(int AdmissionKey);
        void GetAdmissionDiagnosisGroupsForAdmissionAsync(int AdmissionKey);
        void GetAdmissionHospicePumpForAdmissionAsync(int AdmissionKey);
        void AdmissionPharmacyRefillClear();
        void AdmissionHospicePumpClear();

        void GetHospiceRefillImportForImportAsync();

        void GetPatientAdmissionAsync(int patientkey, int admissionKey);

        void GetPatientAdmissionFullDetailsAsync(int patientkey, int? admissionKey);
        void GetTeamMeetingWorkListAsync(int? ServiceLineGroupingKey);
        void RefreshPatientAdmissionsAsync(int patientkey);
        void RefreshPatientAdmissionEncountersAsync(int patientkey, int admissionKey);
        void RefreshPatientAdmissionEncountersAndServicesAsync(int patientkey, int admissionKey, bool refreshServices);
        void RefreshPatientAdmissionDocumentationsAsync(int patientkey, int admissionKey);
        void RefreshPatientAsync(int patientkey);

        void GetAdmissionCareCoordinatorHistoryAsync(int admissionKey);

        void GeneratePatientPortalInvite(int patientkey, Guid CreatedBy);

        event Action<InvokeOperation<string[]>> OnHospiceImportReadFirstRecordReturned;
        void HospiceImportReadFirstRecord(byte[] importFile);
        event Action<InvokeOperation<int>> OnHospiceImportImportReturned;
        void HospiceImportImport(byte[] importFile);

        PagedEntityCollectionView<Patient> Patients { get; }
        EntitySet<AdmissionPharmacyRefill> AdmissionPharmacyRefills { get; }
        EntitySet<AdmissionHospicePump> AdmissionHospicePumps { get; }
        EntitySet<HospiceRefillImportColumnList> HospiceRefillImportColumnLists { get; }
        EntitySet<HospiceRefillImport> HospiceRefillImports { get; }

        bool HasInsuranceRelatedChanges { get; }

        void Add(AdmissionBillingReview entity);
        void Add(HospiceRefillImport entity);
        void Add(PatientImmunization entity);
        void Add(HospiceRefillImportColumnList entity);
        void Remove(OrderEntry entity);
        void Remove(OrderEntryVO entity);
        void Remove(OrderEntrySignature entity);
        void Remove(OrderEntryCoSignature entity);
        void Remove(Admission entity);
        void Remove(AdmissionCommunication entity);
        void Remove(AdmissionDocumentation entity);
        void Remove(AdmissionAuthorization entity);
        void Remove(AdmissionAuthorizationInstance entity);
        void Remove(AdmissionAuthorizationDetail entity);
        void Remove(AdmissionCommunicationAllergy entity);
        void Remove(AdmissionCommunicationLab entity);
        void Remove(AdmissionCommunicationMedication entity);
        void Remove(AdmissionConsent entity);
        void Remove(AdmissionCertification entity);
        void Remove(AdmissionCoverage entity);
        void Remove(AdmissionCoverageInsurance entity);
        void Remove(AdmissionFaceToFaceDiagnosis entity);
        void Remove(AdmissionGroup entity);
        void Remove(AdmissionInfection entity);
        void Remove(PatientInfection entity);
        void Remove(PatientAdverseEvent entity);
        void Remove(AdmissionPhysician entity);
        void Remove(AdmissionProductCode entity);
        void Detach(Admission entity);
        void Remove(InsuranceEligibility entity);
        void Remove(PatientAdvancedDirective entity);
        void Remove(PatientAddress entity);
        void Remove(PatientAllergy entity);
        void Remove(PatientAlternateID entity);
        void Remove(PatientContact entity);
        void Remove(AdmissionDiagnosis entity);
        void Remove(PatientDiagnosisComment entity);
        void Remove(PatientFacilityStay entity);
        void Remove(PatientInsurance entity);
        void Remove(PatientPharmacy entity);
        void Remove(PatientMedication entity);
        void Remove(PatientMedicationSlidingScale entity);
        void Remove(PatientMedicationAdministration entity);
        void Remove(AdmissionMedicationMAR entity);
        void Remove(PatientMedicationAdministrationMed entity);
        void Remove(PatientMedicationReconcile entity);
        void Remove(PatientMedicationReconcileMed entity);
        void Remove(PatientMedicationTeaching entity);
        void Remove(PatientMedicationTeachingMed entity);
        void Remove(PatientMedicationManagement entity);
        void Remove(PatientMedicationManagementMed entity);
        void Remove(PatientMessage entity);
        void Remove(PatientPhone entity);
        void Remove(PatientLab entity);
        void Remove(PatientTranslator entity);
        void Remove(PatientGenderExpression entity);
        void Remove(PatientPhoto entity);

        void Remove(AdmissionLevelOfCare entity);
        void Remove(AdmissionSiteOfService entity);
        void Remove(AdmissionPainLocation entity);
        void Remove(AdmissionHospicePump entity);
        void Remove(AdmissionPharmacyRefill entity);
        void Remove(AdmissionIVSite entity);
        void Remove(AdmissionWoundSite entity);
        void Remove(WoundPhoto entity);
        void Remove(AdmissionGoal entity);
        void Remove(AdmissionGoalElement entity);
        void Remove(AdmissionDiscipline entity);
        void Remove(AdmissionDisciplineFrequency entity);
        void Remove(AdmissionReferral entity);
        void Remove(EncounterAllergy entity);
        void Remove(EncounterAttachedForm entity);
        void Remove(EncounterCMSForm entity);
        void Remove(EncounterCMSFormField entity);
        void Remove(EncounterConsent entity);
        void Remove(AdmissionEquipment entity);
        void Remove(EncounterEquipment entity);
        void Remove(EncounterCoSignature entity);
        void Remove(EncounterData entity);
        void Remove(EncounterDiagnosis entity);
        void Remove(EncounterDiagnosisComment entity);
        void Remove(EncounterMedication entity);
        void Remove(EncounterStartMedication entity);
        void Remove(EncounterLevelOfCare entity);
        void Remove(EncounterPainLocation entity);
        void Remove(EncounterIVSite entity);
        void Remove(EncounterWoundSite entity);
        void Remove(EncounterGoal entity);
        void Remove(EncounterGoalElement entity);
        void Remove(EncounterDisciplineFrequency entity);
        void Remove(EncounterStartDisciplineFrequency entity);
        void Remove(EncounterInfection entity);
        void Remove(EncounterPatientInfection entity);
        void Remove(EncounterPatientAdverseEvent entity);
        void Remove(EncounterLab entity);
        void Remove(EncounterOasis entity);
        void Remove(EncounterSupervision entity);
        void Remove(EncounterOasisAlert entity);
        void Remove(EncounterSupply entity);
        void Remove(EncounterTeamMeeting entity);
        void Remove(EncounterVisitFrequency entity);
        void Remove(EncounterVendor entity);
        void Remove(EncounterNonServiceTime entity);
        void Remove(HospiceRefillImport entity);
        void Remove(HospiceRefillImportColumnList entity);
        void Remove(InsuranceVerifyHistory entity);
        void Remove(InsuranceVerifyHistoryDetail entity);
        void Add(InsuranceVerifyHistory entity);
        void Add(InsuranceVerifyHistoryDetail entity);

        Tuple<string, EntityChangeSet>[] CheckChanges();
        bool OpenOrInvalidObjects(string tag = "", bool log = false);
    }

    public static class PatientServiceOptionalExtensions
    {
        public static void SetLocationForMonitoring(this IPatientService svc)
        {
            svc.SetLocationForMonitoring(string.Empty);
        }
    }
}