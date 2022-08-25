#region Usings

using System;

#endregion

namespace Virtuoso.Server.Data
{
    public interface IOrderEntry
    {
        int? AddedFromEncounterKey { get; set; }
        int AdmissionKey { get; set; }
        DateTimeOffset? CompletedDate { get; set; }
        Guid? CompletedBy { get; set; }
        bool CoSign { get; set; }
        int? HistoryFor { get; set; }
        int? HistoryKey { get; set; }
        string OasisLookbackReferenceCodes { get; set; }
        string OasisLookbackReferences { get; set; }
        int OrderStatus { get; set; }
        string GeneratedOrderText { get; set; }
        string GeneratedReferral { get; set; }
        string GeneratedVisitFrequency { get; set; }
        string GeneratedGoals { get; set; }
        string GeneratedLabs { get; set; }
        string GeneratedMedications { get; set; }
        string GeneratedEquipment { get; set; }
        string GeneratedSupply { get; set; }
        string GeneratedSupplyEquipment { get; set; }
        string GeneratedOther { get; set; }
        string GeneratedInitialServiceOrder { get; set; }
        string GeneratedRecertificationOrder { get; set; }
        string OverrideReferral { get; set; }
        string OverrideVisitFrequency { get; set; }
        string OverrideGoals { get; set; }
        string OverrideLabs { get; set; }
        string OverrideInitialServiceOrder { get; set; }
        string OverrideMedications { get; set; }
        string OverrideEquipment { get; set; }
        string OverrideSupply { get; set; }
        string OverrideOther { get; set; }
        string OverrideRecertificationOrder { get; set; }
        int? OrderEntryVersion { get; set; }
        bool IsGeneratedReferral { get; set; }
        bool IsGeneratedVisitFrequency { get; set; }
        bool IsGeneratedGoals { get; set; }
        bool IsGeneratedLabs { get; set; }
        bool IsGeneratedEquipment { get; set; }
        bool IsGeneratedSupply { get; set; }
        bool IsGeneratedSupplyEquipment { get; set; }
        bool IsGeneratedMedications { get; set; }
        bool IsGeneratedOther { get; set; }
        bool IsGeneratedInitialServiceOrder { get; set; }
        bool IsGeneratedRecertificationOrder { get; set; }
        string OrderText { get; set; }
        int PatientKey { get; set; }
        bool ReadBack { get; set; }
        string ReadTo { get; set; }
        DateTimeOffset? ReviewDate { get; set; }
        Guid? ReviewBy { get; set; }
        string ReviewComment { get; set; }
        DateTimeOffset? SigningPhysicianVerifiedDate { get; set; }
        Guid? SigningPhysicianVerifiedBy { get; set; }
        int? SigningPhysicianKey { get; set; }
        int? SigningPhysicianAddressKey { get; set; }
        int TenantID { get; set; }
        Guid UpdatedBy { get; set; }
        DateTime UpdatedDate { get; set; }
        DateTimeOffset? VoidDate { get; set; }
        Guid? VoidBy { get; set; }
        string VoidReason { get; set; }

        string AgencyAddress1 { get; }
        string AgencyAddress2 { get; }
        string AgencyCityStateZip { get; }
        string AgencyName { get; }
        string AgencyPhone { get; }
        bool CanEditOrder { get; }
        bool CanEditOrderData { get; }
        bool CanEditOrderReviewed { get; }
        bool CanEditOrderSignature { get; }
        bool CanEditOrderSigningPhysicianVerified { get; }
        bool CanEditOrdersVoided { get; }
        bool CanEditPhysician { get; }
        bool CanPrint { get; }
        bool CanRefreshOrderText { get; }
        bool CanRefreshOverrideEquipment { get; }
        bool CanRefreshOverrideGoals { get; }
        bool CanRefreshOverrideLabs { get; }
        bool CanRefreshOverrideInitialServiceOrder { get; }
        bool CanRefreshOverrideMedications { get; }
        bool CanRefreshOverrideOther { get; }
        bool CanRefreshOverrideRecertificationOrder { get; }
        bool CanRefreshOverrideReferral { get; }
        bool CanRefreshOverrideSupply { get; }
        bool CanRefreshOverrideVisitFrequency { get; }
        string CompletedOnText { get; }
        bool IsGeneratedEquipmentEqualToOverride { get; }
        bool IsGeneratedGoalsEqualToOverride { get; }
        bool IsGeneratedLabsEqualToOverride { get; }
        bool IsGeneratedMedicationsEqualToOverride { get; }
        bool IsGeneratedOrderTextEqualToOrderText { get; }
        bool IsGeneratedOtherEqualToOverride { get; }
        bool IsGeneratedInitialServiceOrderEqualToOverride { get; }
        bool IsGeneratedRecertificationOrderEqualToOverride { get; }
        bool IsGeneratedReferralEqualToOverride { get; }
        bool IsGeneratedSupplyEqualToOverride { get; }
        bool IsGeneratedVisitFrequencyEqualToOverride { get; }
        bool IsOrderInactive { get; }
        bool IsOrderStatusCompleted { get; }
        bool IsOrderStatusInProcess { get; }
        bool IsOrderStatusOrderEntryReview { get; }
        bool IsOrderStatusSigningPhysicianVerified { get; }
        bool IsOrderStatusVoided { get; }
        bool IsPreviousGeneratedOtherEqualToOverride { get; }
        bool IsPreviousGeneratedInitialServiceOrderEqualToOverride { get; }
        bool IsPreviousGeneratedRecertificationOrderEqualToOverride { get; }
        bool IsReviewed { get; set; }
        bool IsReviewedCommentsVisible { get; }
        bool IsReviewedVisible { get; }
        bool IsSigned { get; }
        bool IsSigningPhysicianVerified { get; set; }
        bool IsSigningPhysicianVerifiedVisible { get; }
        bool IsVoided { get; set; }
        bool IsVoidedVisible { get; }
        string PreviousGeneratedOther { get; set; }
        string PreviousGeneratedInitialServiceOrder { get; set; }
        string PreviousGeneratedRecertificationOrder { get; set; }
        int PreviousOrderStatus { get; set; }
        void RaiseChanged();
        string ReviewText { get; }
        bool ShowOverrideEquipment { get; }
        bool ShowOverrideGoals { get; }
        bool ShowOverrideLabs { get; }
        bool ShowOverrideInitialServiceOrder { get; }
        bool ShowOverrideMedications { get; }
        bool ShowOverrideOther { get; }
        bool ShowOverrideRecertificationOrder { get; }
        bool ShowOverrideReferral { get; }
        bool ShowOverrideSupply { get; }
        bool ShowOverrideVisitFrequency { get; }
        Physician SigningPhysician { get; }
        string SigningPhysicianName { get; }
        string SigningPhysicianVerifiedText { get; }
        string VoidText { get; }
    }
}