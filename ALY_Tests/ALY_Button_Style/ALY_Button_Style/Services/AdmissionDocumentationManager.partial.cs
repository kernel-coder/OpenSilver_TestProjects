#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public partial class AdmissionDocumentationManager
    {
        private static void __ClearSubItemValidationErrors(Entity SubItem, string _docTypeLower)
        {
            //TODO: Need second pass refactoring to get rid of this switch

            switch (_docTypeLower)
            {
                case "encounter":
                    __ClearErrors(SubItem.As<Encounter>());
                    break;
                case "ff":
                case "ndf2f":
                    __ClearErrors(SubItem.As<AdmissionFaceToFace>());
                    break;
                case "signedorder":
                    __ClearErrors(SubItem.As<AdmissionSignedInterimOrder>());
                    break;
                case "signedpoc":
                    __ClearErrors(SubItem.As<AdmissionSignedPOC>());
                    break;
                case "signedbatchedorder":
                    __ClearErrors(SubItem.As<AdmissionBatchedInterimOrder>());
                    break;
                case "cti":
                case "hhf2f":
                case "hndf2f":
                    __ClearErrors(SubItem.As<AdmissionCOTI>());
                    break;
                case "abn":
                    __ClearErrors(SubItem.As<AdmissionABN>());
                    break;
                case "mhes":
                    __ClearErrors(SubItem.As<AdmissionHospiceElectionStatement>());
                    break;
            }
        }

        private static void __ClearErrors(AdmissionHospiceElectionStatement ahes)
        {
            if (ahes != null && ahes.ValidationErrors != null)
            {
                (ahes).ValidationErrors.Clear();
            }
        }

        private static void __ClearErrors(AdmissionABN CurrentABN)
        {
            if (CurrentABN != null && CurrentABN.ValidationErrors != null)
            {
                (CurrentABN).ValidationErrors.Clear();
            }
        }

        private static void __ClearErrors(AdmissionCOTI CurrentFaceToFace)
        {
            if (CurrentFaceToFace != null && CurrentFaceToFace.ValidationErrors != null)
            {
                (CurrentFaceToFace).ValidationErrors.Clear();
            }
        }

        private static void __ClearErrors(AdmissionBatchedInterimOrder CurrentSignedInterimOrder)
        {
            if ((CurrentSignedInterimOrder != null)
                && (CurrentSignedInterimOrder.ValidationErrors != null)
               )
            {
                (CurrentSignedInterimOrder).ValidationErrors.Clear();
            }
        }

        private static void __ClearErrors(AdmissionSignedPOC CurrentSignedPOC)
        {
            if ((CurrentSignedPOC != null)
                && (CurrentSignedPOC.ValidationErrors != null)
               )
            {
                (CurrentSignedPOC).ValidationErrors.Clear();
            }
        }

        private static void __ClearErrors(AdmissionSignedInterimOrder CurrentSignedInterimOrder)
        {
            if (CurrentSignedInterimOrder != null && CurrentSignedInterimOrder.ValidationErrors != null)
            {
                (CurrentSignedInterimOrder).ValidationErrors.Clear();
            }
        }

        private static void __ClearErrors(AdmissionFaceToFace CurrentFaceToFace)
        {
            if (CurrentFaceToFace != null && CurrentFaceToFace.ValidationErrors != null)
            {
                (CurrentFaceToFace).ValidationErrors.Clear();
            }
        }

        private static void __ClearErrors(Encounter CurrentEncounter)
        {
            if (CurrentEncounter != null && CurrentEncounter.ValidationErrors != null)
            {
                CurrentEncounter.ValidationErrors.Clear();
            }
        }

        private bool __ValidateSubItem(Entity SubItem, AdmissionDocumentation SelectedItem, Admission CurrentAdmission,
            int? ServiceTypeKey, string _docTypeLower)
        {
            bool success = true;

            //TODO: Need second pass refactoring to get rid of this switch - 

            switch (_docTypeLower)
            {
                case "encounter":
                    success = __Validate_Success(SubItem.As<Encounter>(), CurrentAdmission, ServiceTypeKey);
                    break;
                case "ff":
                case "ndf2f":
                    success = __Validate_Success(SubItem.As<AdmissionFaceToFace>(), SelectedItem, CurrentAdmission);
                    break;
                case "signedorder":
                    success = __Validate_Success(SubItem.As<AdmissionSignedInterimOrder>());
                    break;
                case "signedpoc":
                    success = __Validate_Success(SubItem.As<AdmissionSignedPOC>());
                    break;
                case "signedbatchedorder":
                    success = __Validate_Success(SubItem.As<AdmissionBatchedInterimOrder>());
                    break;
                case "cti":
                case "hospf2f":
                case "hndf2f":
                    success = __Validate_Success(SubItem.As<AdmissionCOTI>(), CurrentAdmission, SelectedItem,
                        _docTypeLower);
                    break;
                case "phi access":
                    success = __Validate_Success(SubItem.As<AdmissionDocumentationConsent>());
                    break;
                case "abn":
                    success = __Validate_Success(SubItem.As<AdmissionABN>());
                    break;
                case "mhes":
                    AdmissionHospiceElectionStatement ahes = SubItem as AdmissionHospiceElectionStatement;
                    if (ahes != null)
                    {
                        success = __Validate_Success(SubItem.As<AdmissionHospiceElectionStatement>(), SelectedItem,
                            CurrentAdmission);
                        if ((CurrentAdmission != null) && (ahes.HospiceEOBDate != null) && (success) &&
                            (SelectedItem.Inactive == false))
                        {
                            CurrentAdmission.HospiceEOBDate = ahes.HospiceEOBDate;
                        }
                    }

                    break;
            }

            return success;
        }

        private static bool __Validate_Success(AdmissionHospiceElectionStatement ahes,
            AdmissionDocumentation SelectedItem, Admission CurrentAdmission)
        {
            bool success = true;

            if (ahes != null)
            {
                if (ahes.InsuranceKey <= 0)
                {
                    ahes.InsuranceKey = null;
                }

                if (ahes.AttendingAdmissionPhysicianKey <= 0)
                {
                    ahes.AttendingAdmissionPhysicianKey = null;
                }

                if (ahes.HospiceEOBDate == DateTime.MinValue)
                {
                    ahes.HospiceEOBDate = null;
                }

                if (ahes.HospiceEOBDate != null)
                {
                    ahes.HospiceEOBDate = ((DateTime)ahes.HospiceEOBDate).Date;
                }

                if ((SelectedItem.Inactive == false) && (ahes.DesignationOfAttending == null))
                {
                    ValidationResult error = new ValidationResult("The Designation of Attending field is required",
                        new[] { "DesignationOfAttending" });
                    ahes.ValidationErrors.Add(error);
                }

                if ((SelectedItem.Inactive == false) && (ahes.AttendingAdmissionPhysicianKey == null) &&
                    ahes.ShowAttendingAdmissionPhysician)
                {
                    ValidationResult error = new ValidationResult("The Attending Physician field is required",
                        new[] { "AttendingAdmissionPhysicianKey" });
                    ahes.ValidationErrors.Add(error);
                }

                if ((SelectedItem.Inactive == false) && (ahes.HospiceEOBDate == null))
                {
                    ValidationResult error = new ValidationResult("The Election of Benefits Date field is required",
                        new[] { "HospiceEOBDate" });
                    ahes.ValidationErrors.Add(error);
                }

                if ((SelectedItem.Inactive == false) && (ahes.DatedSignaturePresent == null))
                {
                    ValidationResult error = new ValidationResult("The Dated Signature Present field is required",
                        new[] { "DatedSignaturePresent" });
                    ahes.ValidationErrors.Add(error);
                }

                success = !((ahes != null) && ahes.ValidationErrors.Any());
                if (success && (ahes.ShowAttendingAdmissionPhysician == false))
                {
                    ahes.AttendingAdmissionPhysicianKey = null;
                }
            }

            return success;
        }

        private static bool __Validate_Success(AdmissionABN CurrentABN)
        {
            bool success = true;

            if (CurrentABN != null)
            {
                List<string> typesRequirePayer = new List<string> { "150", "151", "152" };
                var abntype = CodeLookupCache.GetCodeLookupFromKey(CurrentABN.ABNType);
                if (((CurrentABN.InsuranceKey == null)
                     || (CurrentABN.InsuranceKey <= 0)) &&
                    (abntype != null && typesRequirePayer.Contains(abntype.Code)))
                {
                    ValidationResult error = new ValidationResult("Applicable Payer is Required for Type '"
                                                                  + abntype.CodeDescription + "' (" + abntype.Code +
                                                                  ").",
                        new[] { "InsuranceKey" });
                    CurrentABN.ValidationErrors.Add(error);
                }

                if (CurrentABN.ABNType <= 0)
                {
                    ValidationResult error = new ValidationResult("Type is required.",
                        new[] { "ABNType" });
                    CurrentABN.ValidationErrors.Add(error);
                }

                if ((CurrentABN.DateOfIssue == null)
                    || (CurrentABN.DateOfIssue == DateTime.MinValue)
                   )
                {
                    ValidationResult error = new ValidationResult("Date of issue/signature is required.",
                        new[] { "DateOfIssue" });
                    CurrentABN.ValidationErrors.Add(error);
                }
                else if (CurrentABN.DateOfIssue.Date > DateTime.Today.Date)
                {
                    ValidationResult error = new ValidationResult("Date of issue/signature cannot be in the future.",
                        new[] { "DateOfIssue" });
                    CurrentABN.ValidationErrors.Add(error);
                }

                if (CurrentABN.DatedSignaturePresent == null)
                {
                    ValidationResult error = new ValidationResult("Dated Signature Present is required.",
                        new[] { "DatedSignaturePresent" });
                    CurrentABN.ValidationErrors.Add(error);
                }

                success = !((CurrentABN != null) && CurrentABN.ValidationErrors.Any());
            }

            return success;
        }

        private static bool __Validate_Success(AdmissionDocumentationConsent CurrentConsent)
        {
            bool success = true;

            if (CurrentConsent != null)
            {
                if (CurrentConsent.ValidationErrors != null)
                {
                    CurrentConsent.ValidationErrors.Clear();
                }

                if ((CurrentConsent.RequestDate == null)
                    || (CurrentConsent.RequestDate == DateTime.MinValue)
                   )
                {
                    ValidationResult error = new ValidationResult("Request Date is required.", new[] { "RequestDate" });
                    CurrentConsent.ValidationErrors.Add(error);
                }
                else if (CurrentConsent.RequestDate > DateTime.Today)
                {
                    ValidationResult error = new ValidationResult("Request Date cannot be in the future.",
                        new[] { "RequestDate" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if ((CurrentConsent.DecisionDate == null)
                    || (CurrentConsent.DecisionDate == DateTime.MinValue)
                   )
                {
                    ValidationResult error =
                        new ValidationResult("Decision Date is required.", new[] { "DecisionDate" });
                    CurrentConsent.ValidationErrors.Add(error);
                }
                else if (CurrentConsent.DecisionDate > DateTime.Today)
                {
                    ValidationResult error = new ValidationResult("Decision Date cannot be in the future.",
                        new[] { "DecisionDate" });
                    CurrentConsent.ValidationErrors.Add(error);
                }
                else if ((CurrentConsent.RequestDate != null)
                         && (CurrentConsent.DecisionDate < CurrentConsent.RequestDate)
                        )
                {
                    ValidationResult error = new ValidationResult("Decision Date cannot be before request date.",
                        new[] { "DecisionDate" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if (CurrentConsent.DecisionKey < 1)
                {
                    ValidationResult error = new ValidationResult("Decision is required.", new[] { "DecisionKey" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if (!string.IsNullOrEmpty(CurrentConsent.GranteeFirstName))
                {
                    if (CurrentConsent.EffectiveDate == null)
                    {
                        ValidationResult error =
                            new ValidationResult("Effective Date is required.", new[] { "EffectiveDate" });
                        CurrentConsent.ValidationErrors.Add(error);
                    }
                }

                if ((CurrentConsent.EffectiveDate != null)
                    && (CurrentConsent.ExpirationDate == null)
                   )
                {
                    ValidationResult error = new ValidationResult(
                        "Expiration Date is required when Effective Date has been populated.",
                        new[] { "ExpirationDate" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if ((CurrentConsent.EffectiveDate == null)
                    && (CurrentConsent.ExpirationDate != null)
                   )
                {
                    ValidationResult error = new ValidationResult(
                        "Effective Date is required when Expiration Date has been populated.",
                        new[] { "EffectiveDate" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if ((CurrentConsent.EffectiveDate != null)
                    && (CurrentConsent.EffectiveDate > DateTime.Today)
                   )
                {
                    ValidationResult error = new ValidationResult("Effective Date cannot be in the future.",
                        new[] { "EffectiveDate" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if ((CurrentConsent.EffectiveDate != null)
                    && (CurrentConsent.ExpirationDate != null)
                    && (CurrentConsent.EffectiveDate > CurrentConsent.ExpirationDate)
                   )
                {
                    ValidationResult error =
                        new ValidationResult("Expiration Date cannot be earlier than Effective Date.",
                            new[] { "ExpirationDate" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if (((CurrentConsent.ExpirationDate != null)
                     && (CurrentConsent.RequestDate != null)
                     && (CurrentConsent.RequestDate > CurrentConsent.ExpirationDate)
                    )
                   )
                {
                    ValidationResult error =
                        new ValidationResult("Expiration Date cannot be earlier than Request Date.",
                            new[] { "ExpirationDate" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if (!string.IsNullOrEmpty(CurrentConsent.RequestorLastName)
                    && string.IsNullOrEmpty(CurrentConsent.RequestorFirstName)
                   )
                {
                    ValidationResult error = new ValidationResult(
                        "Requestor First Name is required when Requestor Last Name has been populated.",
                        new[] { "RequestorLastName", "RequestorFirstName" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if (!string.IsNullOrEmpty(CurrentConsent.RequestorFirstName)
                    && string.IsNullOrEmpty(CurrentConsent.RequestorLastName)
                   )
                {
                    ValidationResult error = new ValidationResult(
                        "Requestor Last Name is required when Requestor First Name has been populated.",
                        new[] { "RequestorFirstName", "RequestorLastName" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                int addrCheck = ((string.IsNullOrEmpty(CurrentConsent.RequestorAddress1)) ? 0 : 1)
                                + ((string.IsNullOrEmpty(CurrentConsent.RequestorCity)) ? 0 : 1)
                                + ((CurrentConsent.RequestorState == null) ? 0 : 1)
                                + ((string.IsNullOrEmpty(CurrentConsent.RequestorZipCode)) ? 0 : 1);

                if ((addrCheck != 0)
                    && (addrCheck != 4)
                   )
                {
                    ValidationResult error = new ValidationResult(
                        "When Requestor Address, City, State or ZIP code is entered, entire Requestor Address must be entered.",
                        new[] { "RequestorAddress1", "RequestorCity", "RequestorState", "RequestorZipCode" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if ((addrCheck == 0)
                    && (!string.IsNullOrEmpty(CurrentConsent.RequestorAddress2))
                   )
                {
                    ValidationResult error = new ValidationResult(
                        "Requestor Address line 2 cannot be entered without entering the entire Address",
                        new[] { "RequestorAddress2" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if (!string.IsNullOrEmpty(CurrentConsent.GranteeLastName)
                    && string.IsNullOrEmpty(CurrentConsent.GranteeFirstName)
                   )
                {
                    ValidationResult error = new ValidationResult(
                        "Grantee First Name is required when Grantee Last Name has been populated.",
                        new[] { "GranteeLastName", "GranteeFirstName" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if (!string.IsNullOrEmpty(CurrentConsent.GranteeFirstName)
                    && string.IsNullOrEmpty(CurrentConsent.GranteeLastName)
                   )
                {
                    ValidationResult error = new ValidationResult(
                        "Grantee Last Name is required when Grantee First Name has been populated.",
                        new[] { "GranteeLastName", "GranteeFirstName" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                addrCheck = ((string.IsNullOrEmpty(CurrentConsent.GranteeAddress1)) ? 0 : 1)
                            + ((string.IsNullOrEmpty(CurrentConsent.GranteeCity)) ? 0 : 1)
                            + ((CurrentConsent.GranteeState == null) ? 0 : 1)
                            + ((string.IsNullOrEmpty(CurrentConsent.GranteeZipCode)) ? 0 : 1);

                if ((addrCheck != 0)
                    && (addrCheck != 4)
                   )
                {
                    ValidationResult error = new ValidationResult(
                        "When Grantee Address, City, State or ZIP code is entered, entire Grantee Address must be entered.",
                        new[] { "GranteeAddress1", "GranteeCity", "GranteeState", "GranteeZipCode" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if ((addrCheck == 0)
                    && (!string.IsNullOrEmpty(CurrentConsent.GranteeAddress2))
                   )
                {
                    ValidationResult error = new ValidationResult(
                        "Grantee Address line 2 cannot be entered without entering the entire Address",
                        new[] { "GranteeAddress2" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if (string.IsNullOrEmpty(CurrentConsent.DecisionMakerFName) &&
                    string.IsNullOrEmpty(CurrentConsent.DecisionMakerLName))
                {
                    ValidationResult error = new ValidationResult("Decision Maker is required.",
                        new[] { "DecisionMakerLName" });
                    CurrentConsent.ValidationErrors.Add(error);
                }
                else if (string.IsNullOrEmpty(CurrentConsent.DecisionMakerFName) &&
                         ((string.IsNullOrEmpty(CurrentConsent.DecisionMakerLName) == false) &&
                          (CurrentConsent.DecisionMakerLName != "Self")))
                {
                    ValidationResult error = new ValidationResult("Decision Maker First Name is required.",
                        new[] { "DecisionMakerFName" });
                    CurrentConsent.ValidationErrors.Add(error);
                }
                else if (string.IsNullOrEmpty(CurrentConsent.DecisionMakerLName))
                {
                    ValidationResult error = new ValidationResult("Decision Maker Last Name is required.",
                        new[] { "DecisionMakerLName" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if ((!string.IsNullOrEmpty(CurrentConsent.RequestorZipCode))
                    && (CurrentConsent.RequestorZipCode.Length != 5)
                    && (CurrentConsent.RequestorZipCode.Length != 10)
                   )
                {
                    ValidationResult error = new ValidationResult("Requestor ZIP code is not formatted correctly.",
                        new[] { "RequestorZipCode" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                if ((!string.IsNullOrEmpty(CurrentConsent.GranteeZipCode))
                    && (CurrentConsent.GranteeZipCode.Length != 5)
                    && (CurrentConsent.GranteeZipCode.Length != 10)
                   )
                {
                    ValidationResult error = new ValidationResult("Grantee ZIP code is not formatted correctly.",
                        new[] { "GranteeZipCode" });
                    CurrentConsent.ValidationErrors.Add(error);
                }

                success = !((CurrentConsent != null) && CurrentConsent.ValidationErrors.Any());
            }

            return success;
        }

        private static bool __Validate_Success(AdmissionCOTI CurrentAdmissionCOTI, Admission CurrentAdmission,
            AdmissionDocumentation SelectedItem, string _docTypeLower)
        {
            bool success = true;

            if (CurrentAdmissionCOTI != null)
            {
                var IsCOTI = (CurrentAdmissionCOTI.IsF2F == false);
                var IsF2F = CurrentAdmissionCOTI.IsF2F;

                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //BEGIN defaulting AdmissionCOTI for saving to database - populate data that is not bound in the UI

                CurrentAdmissionCOTI.ServiceStartDate = CurrentAdmission.HospiceEOBDate;

                var admissionCertification = CurrentAdmission.AdmissionCertification
                    .Where(ac => ac.PeriodNumber == CurrentAdmissionCOTI.PeriodNumber.GetValueOrDefault())
                    .FirstOrDefault();
                if (admissionCertification != null)
                {
                    CurrentAdmissionCOTI.CertificationFromDate = admissionCertification.PeriodStartDate;
                    CurrentAdmissionCOTI.CertificationThruDate = admissionCertification.PeriodEndDate;
                }

                //END defaulting AdmissionCOTI for saving to database
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                if (SelectedItem.Inactive == false &&
                    CurrentAdmissionCOTI.AttachingClinicianId.GetValueOrDefault().Equals(Guid.Empty))
                {
                    ValidationResult error =
                        new ValidationResult("The Employee is required.", new[] { "AttachingClinicianId" });
                    CurrentAdmissionCOTI.ValidationErrors.Add(error);
                }

                //Note: "Certification of Terminal Illness requires one or both of IsMedDirect and IsAttending
                if (SelectedItem.Inactive == false
                    && IsCOTI
                    && !CurrentAdmissionCOTI.IsAttending && !CurrentAdmissionCOTI.IsMedDirect)
                {
                    ValidationResult error = new ValidationResult("The Physician Role is required.",
                        new[] { "IsAttending", "IsMedDirect" });
                    CurrentAdmissionCOTI.ValidationErrors.Add(error);
                }

                //Note: "Hospice Face To Face Attestation requires one and only one of the following: 
                //      IsMedDirect, IsHospicePhysician, or IsHospiceNursePractitioner
                bool roleChecked = (CurrentAdmissionCOTI.IsMedDirect || CurrentAdmissionCOTI.IsHospicePhysician ||
                                    CurrentAdmissionCOTI.IsHospiceNursePractitioner);
                if (SelectedItem.Inactive == false
                    && IsF2F
                    && roleChecked == false)
                {
                    ValidationResult error = new ValidationResult("A Physician Role is required.",
                        new[] { "IsMedDirect", "IsHospicePhysician", "IsHospiceNursePractitioner" });
                    CurrentAdmissionCOTI.ValidationErrors.Add(error);
                }

                if (SelectedItem.Inactive == false && CurrentAdmissionCOTI.PeriodNumber.HasValue == false)
                {
                    ValidationResult error =
                        new ValidationResult("The Period Number is required.", new[] { "PeriodNumber" });
                    CurrentAdmissionCOTI.ValidationErrors.Add(error);
                }

                if (SelectedItem.Inactive == false && CurrentAdmissionCOTI.SigningPhysicianKey.HasValue == false)
                {
                    var errorStr = IsF2F ? "Performed By is required" : "The Signing Physician is required.";
                    ValidationResult error = new ValidationResult(errorStr, new[] { "SigningPhysicianKey" });
                    CurrentAdmissionCOTI.ValidationErrors.Add(error);
                }

                if (SelectedItem.Inactive == false &&
                    CurrentAdmissionCOTI.SignatureDate.GetValueOrDefault().Equals(DateTime.MinValue))
                {
                    ValidationResult error =
                        new ValidationResult("The Signature Date is required.", new[] { "SignatureDate" });
                    CurrentAdmissionCOTI.ValidationErrors.Add(error);
                }

                //For COTI - prohibit a signature date > 15 days prior to first day of benefit period
                if (SelectedItem.Inactive == false &&
                    IsCOTI) //_docTypeLower.Equals("cti")) - IsF2F == false means this is a COTI
                {
                    int __COTI_MAX_DAYS_PRIOR_TO_BENEFIT_PERIOD__ = -15;
                    DateTime? __PeriodStartDate = CurrentAdmission.AdmissionCertification
                        .Where(ac => ac.PeriodNumber == CurrentAdmissionCOTI.PeriodNumber.GetValueOrDefault())
                        .Select(ac => ac.PeriodStartDate)
                        .FirstOrDefault();

                    if (__PeriodStartDate.HasValue
                        && __PeriodStartDate.Equals(DateTime.MinValue) == false
                        && CurrentAdmissionCOTI.SignatureDate.GetValueOrDefault().Date < __PeriodStartDate
                            .GetValueOrDefault().Date.AddDays(__COTI_MAX_DAYS_PRIOR_TO_BENEFIT_PERIOD__))
                    {
                        ValidationResult error = new ValidationResult(
                            string.Format(
                                "The Signature Date cannot be more than {0} days prior to the first day of the benefit period.",
                                Math.Abs(__COTI_MAX_DAYS_PRIOR_TO_BENEFIT_PERIOD__)),
                            new[] { "SignatureDate" });
                        CurrentAdmissionCOTI.ValidationErrors.Add(error);
                    }
                }

                //For Face-To-Face - prohibit a signature date > 30 days prior to first day of benefit period
                if (SelectedItem.Inactive == false && IsF2F)
                {
                    int __F2F_MAX_DAYS_PRIOR_TO_BENEFIT_PERIOD__ = -30;
                    DateTime? __PeriodStartDate = CurrentAdmission.AdmissionCertification
                        .Where(ac => ac.PeriodNumber == CurrentAdmissionCOTI.PeriodNumber.GetValueOrDefault())
                        .Select(ac => ac.PeriodStartDate)
                        .FirstOrDefault();

                    if (__PeriodStartDate.HasValue
                        && __PeriodStartDate.Equals(DateTime.MinValue) == false
                        && CurrentAdmissionCOTI.SignatureDate.GetValueOrDefault().Date < __PeriodStartDate
                            .GetValueOrDefault().Date.AddDays(__F2F_MAX_DAYS_PRIOR_TO_BENEFIT_PERIOD__))
                    {
                        ValidationResult error = new ValidationResult(
                            string.Format(
                                "The Signature Date cannot be more than {0} days prior to the first day of the benefit period.",
                                Math.Abs(__F2F_MAX_DAYS_PRIOR_TO_BENEFIT_PERIOD__)),
                            new[] { "SignatureDate" });
                        CurrentAdmissionCOTI.ValidationErrors.Add(error);
                    }
                }

                success = (CurrentAdmissionCOTI.ValidationErrors.Any() == false);
            }

            return success;
        }

        private static bool __Validate_Success(AdmissionBatchedInterimOrder CurrentBatchedInterimOrder)
        {
            bool success;

            if (CurrentBatchedInterimOrder != null)
            {
                if (CurrentBatchedInterimOrder.SigningPhysicianKey <= 0)
                {
                    ValidationResult error = new ValidationResult("Signing Physician is required.",
                        new[] { "SigningPhysicianKey" });
                    CurrentBatchedInterimOrder.ValidationErrors.Add(error);
                }
            }

            if (!CurrentBatchedInterimOrder.OrderDateNullable.HasValue)
            {
                ValidationResult error = new ValidationResult("Order Date is required.", new[] { "OrderDateNullable" });
                CurrentBatchedInterimOrder.ValidationErrors.Add(error);
            }

            success = !((CurrentBatchedInterimOrder != null) && CurrentBatchedInterimOrder.ValidationErrors.Any());
            return success;
        }

        private static bool __Validate_Success(AdmissionSignedPOC CurrentSignedPOC)
        {
            bool success = true;

            if (CurrentSignedPOC != null)
            {
                if (CurrentSignedPOC.SigningPhysicianKey <= 0)
                {
                    ValidationResult error = new ValidationResult("Signing Physician is required.",
                        new[] { "SigningPhysicianKey" });
                    CurrentSignedPOC.ValidationErrors.Add(error);
                }

                if (!CurrentSignedPOC.CertFromDateNullable.HasValue)
                {
                    ValidationResult error = new ValidationResult("Cert From Date is required.",
                        new[] { "CertFromDateNullable" });
                    CurrentSignedPOC.ValidationErrors.Add(error);
                }

                if (!CurrentSignedPOC.CertThruDateNullable.HasValue)
                {
                    ValidationResult error = new ValidationResult("Cert Thru Date is required.",
                        new[] { "CertThruDateNullable" });
                    CurrentSignedPOC.ValidationErrors.Add(error);
                }

                if (!CurrentSignedPOC.SignatureDateNullable.HasValue)
                {
                    ValidationResult error = new ValidationResult("Signature Date is required.",
                        new[] { "SignatureDateNullable" });
                    CurrentSignedPOC.ValidationErrors.Add(error);
                }

                success = !((CurrentSignedPOC != null) && CurrentSignedPOC.ValidationErrors.Any());
            }

            return success;
        }

        private static bool __Validate_Success(AdmissionSignedInterimOrder CurrentSignedInterimOrder)
        {
            bool success;

            if (CurrentSignedInterimOrder != null)
            {
                if (CurrentSignedInterimOrder.SigningPhysicianKey <= 0)
                {
                    ValidationResult error = new ValidationResult("Signing Physician is required.",
                        new[] { "SigningPhysicianKey" });
                    CurrentSignedInterimOrder.ValidationErrors.Add(error);
                }

                if (!CurrentSignedInterimOrder.OrderDateNullable.HasValue)
                {
                    ValidationResult error =
                        new ValidationResult("Order Date is required.", new[] { "OrderDateNullable" });
                    CurrentSignedInterimOrder.ValidationErrors.Add(error);
                }

                if (!CurrentSignedInterimOrder.OrderTimeNullable.HasValue)
                {
                    ValidationResult error =
                        new ValidationResult("Order Time is required.", new[] { "OrderTimeNullable" });
                    CurrentSignedInterimOrder.ValidationErrors.Add(error);
                }
            }

            success = !((CurrentSignedInterimOrder != null) && CurrentSignedInterimOrder.ValidationErrors.Any());
            return success;
        }

        private static bool __Validate_Success(AdmissionFaceToFace CurrentFaceToFace,
            AdmissionDocumentation SelectedItem, Admission CurrentAdmission)
        {
            bool success;

            if (CurrentFaceToFace != null)
            {
                if (CurrentFaceToFace.SigningPhysicianKey <= 0)
                {
                    ValidationResult error = new ValidationResult("Signing Physician is required.",
                        new[] { "SigningPhysicianKey" });
                    CurrentFaceToFace.ValidationErrors.Add(error);
                }

                if ((CurrentFaceToFace.PhysianEncounterDate == null) ||
                    (CurrentFaceToFace.PhysianEncounterDate == DateTime.MinValue))
                {
                    ValidationResult error = new ValidationResult("Physician Encounter Date is required.",
                        new[] { "PhysianEncounterDate" });
                    CurrentFaceToFace.ValidationErrors.Add(error);
                }
                else
                {
                    if (CurrentFaceToFace.PhysianEncounterDate.Date > DateTime.Now.Date)
                    {
                        ValidationResult error = new ValidationResult(
                            "Physician Encounter Date cannot be in the future.", new[] { "PhysianEncounterDate" });
                        CurrentFaceToFace.ValidationErrors.Add(error);
                    }
                }

                int OtherActiveCount = CurrentAdmission.AdmissionFaceToFace.Where(f2f =>
                    (SelectedItem.AdmissionDocumentationKey != f2f.AdmissionDocumentationKey)
                    && !f2f.AdmissionDocumentation.Inactive).Count();
                if ((!SelectedItem.Inactive) && (OtherActiveCount > 0))
                {
                    ValidationResult error = new ValidationResult(
                        "Can only have one active face to face document per admission.",
                        new[] { "SigningPhysicianKey" });
                    CurrentFaceToFace.ValidationErrors.Add(error);
                }
            }

            bool result = !((CurrentFaceToFace != null) && CurrentFaceToFace.ValidationErrors.Any());
            success = result;
            return success;
        }

        private bool __Validate_Success(Encounter CurrentEncounter, Admission CurrentAdmission, int? ServiceTypeKey)
        {
            bool success = true;

            if (CurrentEncounter != null)
            {
                ServiceType st = null;
                if (ServiceTypeKey.HasValue == false)
                {
                    ValidationResult error =
                        new ValidationResult("Service type is required.", new[] { "ServiceTypeKey" });
                    CurrentEncounter.ValidationErrors.Add(error);
                }
                else
                {
                    st = ServiceTypeCache.GetServiceTypeFromKey(ServiceTypeKey.Value);
                }

                if ((CurrentAdmission.SOCDate.HasValue == false)
                    && (st != null)
                    && (!st.AllowBeforeAdmit)
                   )
                {
                    ValidationResult error = new ValidationResult(
                        "For Service Types that are not allowed before admit, the " +
                        CurrentAdmission.StartOfCareDateLabel + " is required.", new[] { "ServiceTypeKey" });
                    CurrentEncounter.ValidationErrors.Add(error);
                }

                if (CurrentEncounter != null && CurrentEncounter.EncounterStartDate.HasValue == false ||
                    CurrentEncounter.EncounterStartTime.HasValue == false)
                {
                    if (CurrentEncounter.EncounterStartDate.HasValue == false)
                    {
                        ValidationResult error =
                            new ValidationResult("Start Date is required.", new[] { "EncounterStartDate" });
                        CurrentEncounter.ValidationErrors.Add(error);
                    }

                    if (CurrentEncounter.EncounterStartTime.HasValue == false)
                    {
                        ValidationResult error =
                            new ValidationResult("Start Time is required.", new[] { "EncounterStartTime" });
                        CurrentEncounter.ValidationErrors.Add(error);
                    }
                }
                else
                {
                    __ValidateAdmissionDiscipline(CurrentAdmission, CurrentEncounter, ServiceTypeKey);
                }

                if (CurrentEncounter != null)
                {
                    if (CurrentEncounter.EncounterEndDate.HasValue == false ||
                        CurrentEncounter.EncounterEndTime.HasValue == false)
                    {
                        if (CurrentEncounter.EncounterEndDate.HasValue == false)
                        {
                            ValidationResult error =
                                new ValidationResult("End Date is required.", new[] { "EncounterEndDate" });
                            CurrentEncounter.ValidationErrors.Add(error);
                        }

                        if (CurrentEncounter.EncounterEndTime.HasValue == false)
                        {
                            ValidationResult error =
                                new ValidationResult("End Time is required.", new[] { "EncounterEndTime" });
                            CurrentEncounter.ValidationErrors.Add(error);
                        }
                    }
                    else
                    {
                        if (CurrentEncounter.EncounterStartDate.HasValue && CurrentEncounter.EncounterEndDate.HasValue)
                        {
                            if (CurrentEncounter.EncounterEndDate.Value.Date <
                                CurrentEncounter.EncounterStartDate.Value.Date)
                            {
                                ValidationResult error = new ValidationResult("End Date cannot be before Start Date.",
                                    new[] { "EncounterEndDate" });
                                CurrentEncounter.ValidationErrors.Add(error);
                            }

                            if (CurrentEncounter.EncounterActualTime > 1440)
                            {
                                ValidationResult error = new ValidationResult("Total Minutes cannot exceed 24 hours.",
                                    new[] { "EncounterActualTime" });
                                CurrentEncounter.ValidationErrors.Add(error);
                            }
                        }
                    }

                    if (CurrentEncounter.PatientAddressKey.HasValue == false ||
                        CurrentEncounter.PatientAddressKey.GetValueOrDefault() <= 0)
                    {
                        ValidationResult error = new ValidationResult("Place of Service is required.",
                            new[] { "PatientAddressKey" });
                        CurrentEncounter.ValidationErrors.Add(error);
                    }

                    if (CurrentEncounter.Distance.HasValue == false)
                    {
                        ValidationResult error =
                            new ValidationResult("Distance traveled is required.", new[] { "Distance" });
                        CurrentEncounter.ValidationErrors.Add(error);
                    }

                    success = !((CurrentEncounter != null) && CurrentEncounter.ValidationErrors.Any());
                }
            }

            return success;
        }

        private void __ValidateAdmissionDiscipline(Admission CurrentAdmission, Encounter CurrentEncounter,
            int? ServiceTypeKey)
        {
            CurrentEncounter.AdmissionDisciplineKey = 0; //initialize
            var lastError = string.Empty;
            var serviceType = ServiceTypeCache.GetServiceTypeFromKey(ServiceTypeKey.GetValueOrDefault());
            var dispKey = ServiceTypeCache.GetDisciplineKey(ServiceTypeKey.GetValueOrDefault());
            if (CurrentEncounter.EncounterStartDate.HasValue && dispKey != null && serviceType != null)
            {
                var CurrentAdmissionDiscipline = CurrentAdmission.AdmissionDiscipline
                    .Where(ad => ad.DisciplineKey == dispKey.GetValueOrDefault())
                    .Where(ad =>
                        ad.ReferDateTime.HasValue && CurrentEncounter.EncounterStartDate.Value.Date >=
                        ad.ReferDateTime.Value.Date)
                    .OrderByDescending(ad => ad.ReferDateTime)
                    .ToList();
                foreach (var ad in CurrentAdmissionDiscipline)
                    if (ad.DisciplineAdmitDateTime.HasValue &&
                        CurrentEncounter.EncounterStartDate.Value.Date < ad.DisciplineAdmitDateTime.Value.Date &&
                        serviceType.AllowBeforeAdmit ==
                        false) //if service date is < admit date of dscp - is service type allowed prior to SOC?
                    {
                        lastError = "Service Date Prior to Discipline Admit Date";
                    }
                    else if (ad.DischargeDateTime.HasValue &&
                             CurrentEncounter.EncounterStartDate.Value.Date > ad.DischargeDateTime.Value.Date &&
                             serviceType.AllowAfterDischarge ==
                             false) //if service date is > dschg date of dscp - is service type allowed after discharge?
                    {
                        lastError = "Service Date After Discipline Discharge Date";
                    }
                    else
                    {
                        CurrentEncounter.AdmissionDisciplineKey = ad.AdmissionDisciplineKey;
                        break;
                    }

                if (CurrentEncounter.AdmissionDisciplineKey == 0)
                {
                    if (string.IsNullOrEmpty(lastError))
                    {
                        lastError =
                            "Service Date Not Valid for Admission"; //Could be no AdmissionDisciplines for the service type selected?
                    }

                    ValidationResult error = new ValidationResult(lastError, new[] { "EncounterStartDate" });
                    CurrentEncounter.ValidationErrors.Add(error);
                }
            }
        }
    }
}