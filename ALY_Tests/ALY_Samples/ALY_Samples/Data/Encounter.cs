
using System;

namespace Virtuoso.Server.Data
{
    public class Encounter
    {
        public bool ValidateState_IsEval { get; set; }
        public bool ValidateState_IsEvalFullValidation { get; set; }
        public bool ValidateState_IsPreEval { get; set; }
        public bool IsContractProvider { get; set; }
        public string AdmissionID { get; set; }
        public int AdmissionKey { get; set; }
        public int AdmissionStatus { get; set; }
        public DateTime AdmitDateTime { get; set; }
        public string CareCoordinator { get; set; }
        public int CertificationPeriodDuration { get; set; }
        public object Confidentiality { get; set; }
        public object DeathDate { get; set; }
        public object DeathTime { get; set; }
        public object DischargeDateTime { get; set; }
        public bool DischargedFromTransfer { get; set; }
        public object DischargedFromTransferUser { get; set; }
        public object DischargeReasonKey { get; set; }
        public object EmploymentRelatedEmployer { get; set; }
        public int FaceToFaceEncounter { get; set; }
        public object FaceToFaceEncounterDate { get; set; }
        public object FaceToFaceExceptReason { get; set; }
        public object FaceToFacePhysicianKey { get; set; }
        public object FacilityKey { get; set; }
        public bool HasTrauma { get; set; }
        public object HistoryFor { get; set; }
        public object HistoryKey { get; set; }
        public bool HospiceAdmission { get; set; }
        public bool HospiceBenefitReelection { get; set; }
        public object HospiceEOBDate { get; set; }
        public object HospiceHETNotVerifiedReason { get; set; }
        public object HospiceHETOnFile { get; set; }
        public object HospiceHETVerified { get; set; }
        public object HospiceHETVerifiedByFirstName { get; set; }
        public object HospiceHETVerifiedByLastName { get; set; }
        public object HospiceHETVerifiedDate { get; set; }
        public bool IgnoreSOCMismatch { get; set; }
        public DateTime InitialReferralDate { get; set; }
        public bool IsDependentOnElectricity { get; set; }
        public bool IsEmploymentRelated { get; set; }
        public object NotTakenDateTime { get; set; }
        public object NotTakenReason { get; set; }
        public int PatientInsuranceKey { get; set; }
        public int PatientKey { get; set; }
        public bool PerformOasis { get; set; }
        public DateTime PhysicianOrderedSOCDate { get; set; }
        public object PreEvalFollowUpComments { get; set; }
        public object PreEvalFollowUpDate { get; set; }
        public object PreEvalOnHoldDateTime { get; set; }
        public object PreEvalOnHoldReason { get; set; }
        public bool PreEvalRequired { get; set; }
        public bool PreEvalResumptionRequired { get; set; }
        public object PreEvalStatus { get; set; }
        public DateTime ReferDateTime { get; set; }
        public bool ReleaseOfInformation { get; set; }
        public int ServiceLineKey { get; set; }
        public int ServicePriorityOne { get; set; }
        public string ServicePriorityOneComment { get; set; }
        public int ServicePriorityThree { get; set; }
        public int ServicePriorityTwo { get; set; }
        public DateTime SOCDate { get; set; }
        public int SourceOfAdmission { get; set; }
        public object StartPeriodNumber { get; set; }
        public int TenantID { get; set; }
        public bool TransferHospice { get; set; }
        public object TransferHospiceAgency { get; set; }
        public object TraumaDate { get; set; }
        public object TraumaStateCode { get; set; }
        public object TraumaType { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public bool UseHospiceBilling { get; set; }
        public DateTime VerbalSOCDate { get; set; }
        public bool RemoveFromView { get; set; }
        public bool IsEditting { get; set; }
        public bool IsInCancel { get; set; }
        public bool IsOKed { get; set; }
    }
}
