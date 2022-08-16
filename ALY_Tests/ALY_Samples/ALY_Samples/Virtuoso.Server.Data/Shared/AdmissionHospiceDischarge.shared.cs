using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Virtuoso.Server.Data
{
    public partial class AdmissionHospiceDischarge
    {
        public void TidyUpData()
        {
            if (DischargeReason == 0) DischargeReason = null;
            if (DischargeDate == DateTime.MinValue) DischargeDate = null;
            if (DischargeDate != null) DischargeDate = ((DateTime)DischargeDate).Date;
            if (string.IsNullOrWhiteSpace(HowInformedOfDeath)) HowInformedOfDeath = null;
            if (string.IsNullOrWhiteSpace(DeathPronouncement)) DeathPronouncement = null;
            if (string.IsNullOrWhiteSpace(DeathPronouncementComment)) DeathPronouncementComment = null;
            if (string.IsNullOrWhiteSpace(PersonsPresentAtDeath)) PersonsPresentAtDeath = null;
            if (DeathDateTime == DateTime.MinValue) DeathDateTime = null;
            if (LocationOfDeath == 0) LocationOfDeath = null;
            if (string.IsNullOrWhiteSpace(NotificationOfHospiceDischarge)) NotificationOfHospiceDischarge = null;
            if (string.IsNullOrWhiteSpace(AdditionalInformation)) AdditionalInformation = null;
            if (RevocationEffectiveDate == DateTime.MinValue) RevocationEffectiveDate = null;
            if (RevocationEffectiveDate != null) RevocationEffectiveDate = ((DateTime)RevocationEffectiveDate).Date;
            if (string.IsNullOrWhiteSpace(SummaryOfIDTDecision)) SummaryOfIDTDecision = null;
            if (string.IsNullOrWhiteSpace(HospiceDCPlan)) HospiceDCPlan = null;
            if (string.IsNullOrWhiteSpace(HospiceDCPlanComment)) HospiceDCPlanComment = null;
            if (PhysicianOrderForDischargeDate == DateTime.MinValue) PhysicianOrderForDischargeDate = null;
            if (PhysicianOrderForDischargeDate != null) PhysicianOrderForDischargeDate = ((DateTime)PhysicianOrderForDischargeDate).Date;
            if (NOMNCDate == DateTime.MinValue) NOMNCDate = null;
            if (NOMNCDate != null) NOMNCDate = ((DateTime)NOMNCDate).Date;
            if (string.IsNullOrWhiteSpace(ExplanationOfRelocation)) ExplanationOfRelocation = null;
            if (TransferRequestReceiptDate == DateTime.MinValue) TransferRequestReceiptDate = null;
            if (TransferRequestReceiptDate != null) TransferRequestReceiptDate = ((DateTime)TransferRequestReceiptDate).Date;
            if (string.IsNullOrWhiteSpace(ReceivingHospiceContact)) ReceivingHospiceContact = null;
            if (HospiceTransferType == 0) HospiceTransferType = null;
            if (DischargeForCauseDate == DateTime.MinValue) DischargeForCauseDate = null;
            if (DischargeForCauseDate != null) DischargeForCauseDate = ((DateTime)DischargeForCauseDate).Date;
            if (NotificationOfMedicareDate == DateTime.MinValue) NotificationOfMedicareDate = null;
            if (NotificationOfMedicareDate != null) NotificationOfMedicareDate = ((DateTime)NotificationOfMedicareDate).Date;
            if (NotificationOfStateDate == DateTime.MinValue) NotificationOfStateDate = null;
            if (NotificationOfStateDate != null) NotificationOfStateDate = ((DateTime)NotificationOfStateDate).Date;
            if (Version <= 0) Version = 2;
            if ((RevokeReason == 0) || (Version == 1)) RevokeReason = null;
            if ((TransferReason == 0) || (Version == 1))  TransferReason = null;
            if (InactiveDate == DateTime.MinValue) InactiveDate = null;
        }
    }
}
