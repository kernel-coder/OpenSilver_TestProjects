using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Server.Data;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Virtuoso.Validation
{
    public static class AdmissionValidations
    {
        public static ValidationResult ValidateAdmissionStatusOnPreEvaluation(Admission CurrentAdmission, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (CurrentAdmission.ValidateState_IsPreEval == false) return ValidationResult.Success;
            if (string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalStatus))
            {
                string[] memberNames = new string[] { "PreEvalStatus" };
                return new ValidationResult("Patient Status is required", memberNames);
            }
            return ValidationResult.Success;
#else
            return ValidationResult.Success;
#endif
        }
        public static ValidationResult ValidateOnHold(Admission CurrentAdmission, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (CurrentAdmission.ValidateState_IsPreEval == false) return ValidationResult.Success;
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(CurrentAdmission.AdmissionStatus);
                if (_admissionStatusCode == "H")
                {
                    if (((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalOnHoldReason)) == true) && (CurrentAdmission.PreEvalFollowUpDate != null) && ((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalFollowUpComments) == false)))
                    {
                        string[] memberNames = new string[] { "PreEvalOnHoldReason" };
                        return new ValidationResult("On Hold Reason is required", memberNames);
                    }
                    else if (((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalOnHoldReason)) == false) && (CurrentAdmission.PreEvalFollowUpDate == null) && ((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalFollowUpComments) == false)))
                    {
                        string[] memberNames = new string[] { "PreEvalFollowUpDate" };
                        return new ValidationResult("Date for Follow-up is required", memberNames);
                    }
                    else if (((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalOnHoldReason)) == false) && (CurrentAdmission.PreEvalFollowUpDate != null) && ((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalFollowUpComments) == true)))
                    {
                        string[] memberNames = new string[] { "PreEvalFollowUpComments" };
                        return new ValidationResult("Follow-up Coments are required", memberNames);
                    }
                    else if (((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalOnHoldReason)) == true) && (CurrentAdmission.PreEvalFollowUpDate == null) && ((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalFollowUpComments) == false)))
                    {
                        string[] memberNames = new string[] { "PreEvalOnHoldReason", "PreEvalFollowUpDate" };
                        return new ValidationResult("On Hold Reason and Date for Follow-up are required", memberNames);
                    }
                    else if (((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalOnHoldReason)) == true) && (CurrentAdmission.PreEvalFollowUpDate != null) && ((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalFollowUpComments) == true)))
                    {
                        string[] memberNames = new string[] { "PreEvalOnHoldReason", "PreEvalFollowUpComments" };
                        return new ValidationResult("On Hold Reason and Follow-up Coments are required", memberNames);
                    }
                    else if (((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalOnHoldReason)) == false) && (CurrentAdmission.PreEvalFollowUpDate == null) && ((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalFollowUpComments) == true)))
                    {
                        string[] memberNames = new string[] { "PreEvalFollowUpDate", "PreEvalFollowUpComments" };
                        return new ValidationResult("Date for Follow-up and Follow-up Coments are required", memberNames);
                    }
                    else if (((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalOnHoldReason)) == true) && (CurrentAdmission.PreEvalFollowUpDate == null) && ((string.IsNullOrWhiteSpace(CurrentAdmission.PreEvalFollowUpComments) == true)))
                    {
                        string[] memberNames = new string[] { "PreEvalOnHoldReason", "PreEvalFollowUpDate", "PreEvalFollowUpComments" };
                        return new ValidationResult("On Hold Reason and Date for Follow-up and Follow-up Coments are required", memberNames);
                    }
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "PreEvalOnHoldReason" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
#else
            return ValidationResult.Success;
#endif
        }

        public static ValidationResult ValidateNotTaken(Admission CurrentAdmission, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(CurrentAdmission.AdmissionStatus);
                if (_admissionStatusCode == "N")
                {
                    if ((CurrentAdmission.NotTakenReason == null) && (CurrentAdmission.ValidateState_IsEval == false))
                    {
                        string[] memberNames = new string[] { "NotTakenReason" };
                        return new ValidationResult("You must specify a not admitted reason", memberNames);
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(CurrentAdmission.NotTakenReason) == false) CurrentAdmission.NotTakenReason = null;
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "NotTakenReason" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
#else
            return ValidationResult.Success;
#endif
        }

        public static ValidationResult ValidateAdmissionStatusOnEvaluation(Admission CurrentAdmission, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (!CurrentAdmission.ValidateState_IsEvalFullValidation) return ValidationResult.Success;
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(CurrentAdmission.AdmissionStatus);
                if ((_admissionStatusCode != "A") && (_admissionStatusCode != "N") && (_admissionStatusCode != "M")
                    && (_admissionStatusCode !="D") && (_admissionStatusCode != "T"))
                {
                    string[] memberNames = new string[] { "Admitted" };
                    return new ValidationResult("You must specify a status of admitted or not admitted", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "Admitted" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
#else
            return ValidationResult.Success;
#endif
        }
        public static ValidationResult ValidateAdmissionDischargeStatusFromTransfer(Admission CurrentAdmission, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(CurrentAdmission.AdmissionStatus);
                if ((_admissionStatusCode == "D") && CurrentAdmission.DischargedFromTransfer)
                {
                    if (!CurrentAdmission.DischargeDateTime.HasValue)
                    {
                        string[] memberNames = new string[] { "DischargeDateTime" };
                        return new ValidationResult("You must specify a Discharge Date and Reason in order to Discharge this Admission.", memberNames);
                    }
                    if (!CurrentAdmission.DischargeReasonKey.HasValue)
                    {
                        string[] memberNames = new string[] { "DischargeReasonKey" };
                        return new ValidationResult("You must specify a Discharge Date and Reason in order to Discharge this Admission.", memberNames);
                    }
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "Admitted" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
        }
        public static ValidationResult ValidateFacilityKeyOnEvaluation(Admission CurrentAdmission, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (!CurrentAdmission.ValidateState_IsEvalFullValidation || !CurrentAdmission.IsContractProvider) return ValidationResult.Success;
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(CurrentAdmission.AdmissionStatus);
                if (_admissionStatusCode == "N") return ValidationResult.Success;
                if (CurrentAdmission.FacilityKey <= 0) CurrentAdmission.FacilityKey = null;
                if (CurrentAdmission.FacilityKey == null)
                {
                    string[] memberNames = new string[] { "FacilityKey" };
                    return new ValidationResult("Owning Facility is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "FacilityKey" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
#else
            return ValidationResult.Success;
#endif
        }

        //public static ValidationResult ValidateAdmittingPhysicianKeyOnEvaluation(Admission CurrentAdmission, ValidationContext validationContext)
        //{
        //    var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
        //    if (codeLookupDataProvider != null)
        //    {
        //        var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(CurrentAdmission.AdmissionStatus);
        //        if (!CurrentAdmission.ValidateState_IsEvalFullValidation || _admissionStatusCode == "N") return ValidationResult.Success;
        //        //TODO-J.E. - FIX THIS
        //        //if (CurrentAdmission.AdmittingPhysicianKey == 0) CurrentAdmission.AdmittingPhysicianKey = null;
        //        //if (CurrentAdmission.AdmittingPhysicianKey == null)
        //        //{
        //        //    string[] memberNames = new string[] { "AdmittingPhysicianKey" };
        //        //    return new ValidationResult("Admitting Physician is required", memberNames);
        //        //}
        //        return ValidationResult.Success;
        //    }
        //    else
        //    {
        //        string[] memberNames = new string[] { "AdmittingPhysicianKey" };
        //        return new ValidationResult("CodeLookup data provider is NULL", memberNames);
        //    }
        //}

        //public static ValidationResult ValidateSigningPhysicianKeyOnEvaluation(Admission CurrentAdmission, ValidationContext validationContext)
        //{
        //    var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
        //    if (codeLookupDataProvider != null)
        //    {
        //        var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(CurrentAdmission.AdmissionStatus);
        //        if (!CurrentAdmission.ValidateState_IsEvalFullValidation || _admissionStatusCode == "N") return ValidationResult.Success;
        //        //TODO-J.E. - FIX THIS
        //        //if (CurrentAdmission.SigningPhysicianKey == 0) CurrentAdmission.SigningPhysicianKey = null;
        //        //if (CurrentAdmission.SigningPhysicianKey == null)
        //        //{
        //        //    string[] memberNames = new string[] { "SigningPhysicianKey" };
        //        //    return new ValidationResult("Signing Physician is required", memberNames);
        //        //}
        //        return ValidationResult.Success;
        //    }
        //    else
        //    {
        //        string[] memberNames = new string[] { "SigningPhysicianKey" };
        //        return new ValidationResult("CodeLookup data provider is NULL", memberNames);
        //    }
        //}

        public static ValidationResult ValidateCareCoordinatorOnEvaluation(Admission CurrentAdmission, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (!CurrentAdmission.ValidateState_IsEvalFullValidation) return ValidationResult.Success;
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(CurrentAdmission.AdmissionStatus);
                if (_admissionStatusCode == "N") return ValidationResult.Success;
                if (CurrentAdmission.CareCoordinator.ToString().Equals("00000000-0000-0000-0000-000000000000")) CurrentAdmission.CareCoordinator = null;
                if (CurrentAdmission.CareCoordinator == null)
                {
                    string[] memberNames = new string[] { "CareCoordinator" };
                    return new ValidationResult("Care Coordinator is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "CareCoordinator" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
#else
            return ValidationResult.Success;
#endif
        }

        public static ValidationResult ValidatePhysicianOrderedSOCDateOnEvaluation(Admission CurrentAdmission, ValidationContext validationContext)
        {
            return ValidationResult.Success; // ?? maybe must have InitialReferralDate or PhysicianOrderedSOCDate, but not both
            //    if (!CurrentAdmission.ValidateState_IsEvalFullValidation || CurrentAdmission.AdmissionStatusCode == "N") return ValidationResult.Success;
            //    if (CurrentAdmission.PhysicianOrderedSOCDate == DateTime.MinValue) CurrentAdmission.PhysicianOrderedSOCDate = null;
            //    if (CurrentAdmission.PhysicianOrderedSOCDate == null)
            //    {
            //        string[] memberNames = new string[] { "PhysicianOrderedSOCDate" };
            //        return new ValidationResult("The Physician Ordered Start of Care Date field is required", memberNames);
            //    }
            //    return ValidationResult.Success;
        }

        public static ValidationResult ValidateSOCDateOnEvaluation(Admission CurrentAdmission, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (!CurrentAdmission.ValidateState_IsEvalFullValidation) return ValidationResult.Success;

            var serviceLineTypeProvider = validationContext.GetService(typeof(IServiceLineTypeProvider)) as IServiceLineTypeProvider;
            if (serviceLineTypeProvider == null)
            {
                string[] memberNames = new string[] { "CareCoordinator" };
                return new ValidationResult("Service Line Type data provider is NULL", memberNames);
            }

            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider == null)
            {
                string[] memberNames = new string[] { "CareCoordinator" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }

            var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(CurrentAdmission.AdmissionStatus);
            if (_admissionStatusCode == "N") return ValidationResult.Success;
            if (CurrentAdmission.SOCDate == DateTime.MinValue) CurrentAdmission.SOCDate = null;
            if (CurrentAdmission.SOCDate == null)
            {
                string[] memberNames = new string[] { "SOCDate" };

                var _serviceLineType = serviceLineTypeProvider.GetServiceLineTypeFromAdmission(CurrentAdmission);
                var _isHospice = (_serviceLineType == 4);
                return new ValidationResult((_isHospice ? "Admitted to Service Date is required" : "Start of Care Date is required"), memberNames);
            }
            return ValidationResult.Success;
#else
            return ValidationResult.Success;
#endif
        }

        public static ValidationResult ValidateResumptionReferralDate(DateTime resumptionReferralDate, ValidationContext validationContext)
        {
            return ValidationResult.Success; // ?? maybe must have ResumptionReferralDate or VerbalResumptionDate, but not both
        }

        public static ValidationResult ValidateVerbalResumptionDate(DateTime verbalResumptionDate, ValidationContext validationContext)
        {
            return ValidationResult.Success; // ?? maybe must have ResumptionReferralDate or VerbalResumptionDate, but not both
        }

        public static ValidationResult ValidateCertificationPeriodDuration(int? CertificationPeriodDuration, ValidationContext validationContext)
        {
            if ((CertificationPeriodDuration == null) || (CertificationPeriodDuration == 0))
            {
                string[] memberNames = new string[] { "CertificationPeriodDuration" };
                return new ValidationResult("Period Duration is required", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult ValidateFaceToFaceEncounterOnEvaluation(Admission CurrentAdmission, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (!CurrentAdmission.ValidateState_IsEvalFullValidation) return ValidationResult.Success;
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            var admissionDataProvider = validationContext.GetService(typeof(IAdmissionDataProvider)) as IAdmissionDataProvider;
            if (codeLookupDataProvider != null && admissionDataProvider != null)
            {
                var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(CurrentAdmission.AdmissionStatus);
                if (_admissionStatusCode == "M") return ValidationResult.Success;

                string _faceToFaceEncounterCode = String.Empty;
                if (CurrentAdmission.FaceToFaceEncounter == null)
                    _faceToFaceEncounterCode = String.Empty;
                else
                {
                    int codeKey = System.Convert.ToInt32(CurrentAdmission.FaceToFaceEncounter);
                    if (codeKey <= 0)
                        _faceToFaceEncounterCode = String.Empty;
                    else
                        _faceToFaceEncounterCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(codeKey);
                }

                // Allow nulls if the Insurance doesn't require a face to face.
                var PatientInsurances = admissionDataProvider.GetPatientInsurances(CurrentAdmission, CurrentAdmission.PatientKey);
                PatientInsurance ins = PatientInsurances.Where(pi => pi.PatientInsuranceKey == CurrentAdmission.PatientInsuranceKey).FirstOrDefault();
                if (ins == null) return ValidationResult.Success;
                if (ins.Insurance == null) return ValidationResult.Success;
                if (!ins.Insurance.FaceToFaceOnAdmit) return ValidationResult.Success;

                if (CurrentAdmission.FaceToFaceEncounter.GetValueOrDefault() <= 0) CurrentAdmission.FaceToFaceEncounter = null;
                if (CurrentAdmission.FaceToFaceEncounterDate == DateTime.MinValue) CurrentAdmission.FaceToFaceEncounterDate = null;
                if (!(_faceToFaceEncounterCode.ToUpper() == "ONFILE" || _faceToFaceEncounterCode.ToUpper() == "EXCEPT")) CurrentAdmission.FaceToFaceEncounterDate = null;
                 if ((!CurrentAdmission.ValidateState_IsEvalFullValidation && _faceToFaceEncounterCode != "OnFile") || _admissionStatusCode == "N") return ValidationResult.Success;
                if (CurrentAdmission.FaceToFaceEncounter == null)
                {
                    string[] memberNames = new string[] { "FaceToFaceEncounter" };
                    return new ValidationResult("Face To Face Encounter is required", memberNames);
                }
                if ((_faceToFaceEncounterCode == "OnFile") && (CurrentAdmission.FaceToFaceEncounterDate == null))
                {
                    string[] memberNames = new string[] { "FaceToFaceEncounterDate" };
                    return new ValidationResult("The Face To Face Encounter Date field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "FaceToFaceEncounter" };
                if (codeLookupDataProvider == null) return new ValidationResult("CodeLookup data provider is NULL", memberNames);
                else return new ValidationResult("Admission data provider is NULL", memberNames);
            }
#else
            return ValidationResult.Success;
#endif
        }

        public static ValidationResult ValidatePatientInsuranceKeyOnEvaluation(Admission CurrentAdmission, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (!CurrentAdmission.ValidateState_IsEvalFullValidation) return ValidationResult.Success;
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(CurrentAdmission.AdmissionStatus);
                if (!CurrentAdmission.ValidateState_IsEvalFullValidation || _admissionStatusCode == "N") return ValidationResult.Success;
                if (CurrentAdmission.PatientInsuranceKey <= 0) CurrentAdmission.PatientInsuranceKey = null;
                if (CurrentAdmission.PatientInsuranceKey == null)
                {
                    string[] memberNames = new string[] { "PatientInsuranceKey" };
                    return new ValidationResult("Health Insurance Number is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "PatientInsuranceKey" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
#else
            return ValidationResult.Success;
#endif
        }

        public static ValidationResult ValidatePatientInsuranceKeyIfHospiceAdmission(Admission CurrentAdmission, ValidationContext validationContext)
        {
            var serviceLineTypeProvider = validationContext.GetService(typeof(IServiceLineTypeProvider)) as IServiceLineTypeProvider;
            if (serviceLineTypeProvider == null)
            {
                string[] memberNames = new string[] { "PatientInsuranceKey" };
                return new ValidationResult("Service Line Type data provider is NULL", memberNames);
            }

            var isHospice = (4 ==serviceLineTypeProvider.GetServiceLineTypeFromAdmission(CurrentAdmission)); // Hospice Value
            if (!isHospice) return ValidationResult.Success;
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(CurrentAdmission.AdmissionStatus);
                if (_admissionStatusCode == "N") return ValidationResult.Success;
                if (CurrentAdmission.PatientInsuranceKey <= 0) CurrentAdmission.PatientInsuranceKey = null;
                if (CurrentAdmission.PatientInsuranceKey == null)
                {
                    string[] memberNames = new string[] { "PatientInsuranceKey" };
                    return new ValidationResult("Health Insurance Number is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "PatientInsuranceKey" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
        }

        public static ValidationResult CommunicationTypeValid(int communicationType, ValidationContext validationContext)
        {
            if (communicationType == 0)
            {
                string[] memberNames = new string[] { "CommunicationType" };
                return new ValidationResult("The Communication Type field is required", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult DocumentationTypeValid(int documentationType, ValidationContext validationContext)
        {
            if (documentationType == 0)
            {
                string[] memberNames = new string[] { "DocumentationType" };
                return new ValidationResult("The Documentation Type field is required", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult AdmissionDisciplineFrequencyValidate(AdmissionDisciplineFrequency CurrentVisitFrequency, ValidationContext validationContext)
        {

            // Moved to FCD Backing Class on the form.  Will need to be added elsewhere if FCD's are ever added somewhere else.
            return ValidationResult.Success;
        }

        public static bool CanEditCertCycle(AdmissionDisciplineFrequency CurrentAdmissionDisciplineFrequency)
        {
            bool canEdit = false;

            if (CurrentAdmissionDisciplineFrequency.Admission.AdmissionCertification.Any() == false)
            {
                canEdit = true;
            }
            else if ((CurrentAdmissionDisciplineFrequency.Admission.AdmissionCertification.Count == 1)
                        && !CurrentAdmissionDisciplineFrequency.Admission.Encounter.Any(e => e.Signed)
                // not sure how to check if IsBillable
                    )
            {
                canEdit = true;
            }

            return canEdit;
        }

        public static ValidationResult AdmissionValidateAuthType(Int32 Data, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "AuthorizationType" };
            if (Data == 0)
                return new ValidationResult("Authorization Type is required.", memberNames);
            return ValidationResult.Success;
            /*
            string[] memberNames = new string[] { validationContext.MemberName };
            string displayName = (validationContext.DisplayName == null) ? validationContext.MemberName : validationContext.DisplayName;
            if (AuthType == 0)
            {
                return new ValidationResult(string.Format("{0} is required.", displayName), memberNames);
            }
            
            return ValidationResult.Success;
            */
        }

        //public static ValidationResult ValidateHasTraumaRequired(Nullable<bool> Data, ValidationContext validationContext)
        //{
        //    var admissionDataProvider = validationContext.GetService(typeof(IAdmissionDataProvider)) as IAdmissionDataProvider;
        //    if (admissionDataProvider != null)
        //    {
        //        var _isUsedInDynamicForm = admissionDataProvider.IsForm();
        //        if ((Data.HasValue == false) && (_isUsedInDynamicForm))
        //        {
        //            string[] memberNames = new string[] { "HasTrauma" };
        //            return new ValidationResult("Trauma Y/N is required", memberNames);
        //        }
        //        return ValidationResult.Success;
        //    }
        //    else
        //    {
        //        string[] memberNames = new string[] { "HasTrauma" };
        //        return new ValidationResult("Admission data provider is NULL", memberNames);
        //    }
        //}

        public static ValidationResult ValidateHasTrauma_Date(Nullable<DateTime> Date, ValidationContext validationContext)
        {
            // Now done on the client to allow partial save in Dynamic Form (10/02/2014 MLL)
//            Admission admission = (Admission)validationContext.ObjectInstance;

//            if ((admission.HasTrauma.GetValueOrDefault() == true) && (Date.HasValue == false))
//            {
//                //http://stackoverflow.com/questions/4488969/split-a-string-by-capital-letters
//                var r = new Regex(@" 
//                (?<=[A-Z])(?=[A-Z][a-z]) | 
//                 (?<=[^A-Z])(?=[A-Z]) | 
//                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
//                return new ValidationResult(String.Format("{0} required when have trauma.", r.Replace(validationContext.MemberName, " ")), new[] { validationContext.MemberName });
//            }

            return ValidationResult.Success;
        }
        public static ValidationResult AdmissionValidateRadiatesToLocation(int? radiatesToLocation, ValidationContext validationContext)
        {
            AdmissionPainLocation admissionPainLocation = (AdmissionPainLocation)validationContext.ObjectInstance;
            if (admissionPainLocation == null) return ValidationResult.Success;

            if ((admissionPainLocation.PainRadiates == true) && (radiatesToLocation == null))
            {
                string[] memberNames = new string[] { "RadiatesToLocation" };
                return new ValidationResult("The To Location field is required.", memberNames);
            }

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateHasTrauma_Code(Nullable<int> Code, ValidationContext validationContext)
        {
            // Now done on the client to allow partial save in Dynamic Form (10/02/2014 MLL)
//            Admission admission = (Admission)validationContext.ObjectInstance;
//            bool haveValidValue = (Code.HasValue && Code.Value > 0);
//            if ((admission.HasTrauma.GetValueOrDefault() == true) && (haveValidValue == false))
//            {
//                //http://stackoverflow.com/questions/4488969/split-a-string-by-capital-letters
//                var r = new Regex(@" 
//                (?<=[A-Z])(?=[A-Z][a-z]) | 
//                 (?<=[^A-Z])(?=[A-Z]) | 
//                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
//                return new ValidationResult(String.Format("{0} required when have trauma.", r.Replace(validationContext.MemberName, " ")), new[] { validationContext.MemberName });
//            }

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateSigningEffective_Date(AdmissionPhysician admissionPhysician, ValidationContext validationContext)
        {
            //AdmissionPhysician admissionPhysician = (AdmissionPhysician)validationContext.ObjectInstance;

            if ((admissionPhysician.Signing == true) && (admissionPhysician.SigningEffectiveFromDate.HasValue == false))
            {
                //http://stackoverflow.com/questions/4488969/split-a-string-by-capital-letters
                var r = new Regex(@" 
                (?<=[A-Z])(?=[A-Z][a-z]) | 
                 (?<=[^A-Z])(?=[A-Z]) | 
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
                return new ValidationResult(String.Format("{0} required when signing.",
                    r.Replace((validationContext.MemberName == null ? "SigningEffectiveFromDate" : validationContext.MemberName), " ")),
                    new[] { validationContext.MemberName == null ? "SigningEffectiveFromDate" : validationContext.MemberName });
            }

            return ValidationResult.Success;
        }

        public static ValidationResult AdmissionPhysicianValidate(AdmissionPhysician currentAdmissionPhysician, ValidationContext validationContext)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //NOTE: this function is essentially a copy of the date range logic from AdmissionDisciplineFrequencyValidate(...)
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            var admissionDataProvider = validationContext.GetService(typeof(IAdmissionDataProvider)) as IAdmissionDataProvider;

            // Crossed Dates
            if (currentAdmissionPhysician.PhysicianEffectiveFromDate > currentAdmissionPhysician.PhysicianEffectiveThruDate)
            {
                string[] memberNames = new string[] { "PhysicianEffectiveFromDate" };
                return new ValidationResult("Physician effective from date cannot be later than the effective thru date.", memberNames);
            }

            //DS 10/15/14 Task 14041
            if (currentAdmissionPhysician.SigningEffectiveFromDate > currentAdmissionPhysician.SigningEffectiveThruDate)
            {
                string[] memberNames = new string[] { "SigningEffectiveFromDate" };
                return new ValidationResult("Signed effective from date cannot be later than the effective thru date.", memberNames);
            }

            List<AdmissionPhysician> adf = null;
            if (admissionDataProvider != null)
            {
                adf = admissionDataProvider.GetAdmissionPhysicians(currentAdmissionPhysician.Admission, currentAdmissionPhysician.AdmissionKey);
            }

            if (adf != null)
            {
                //Only 1 signing physician per Admission, ignore PhysicianType
                var _ret = adf.Any(df => df.AdmissionPhysicianKey != currentAdmissionPhysician.AdmissionPhysicianKey
                    && currentAdmissionPhysician.Signing == true
                    && currentAdmissionPhysician.Inactive == false
                    && df.Signing == true
                    && df.Inactive == false
                    // All non null dates
                    && ((currentAdmissionPhysician.SigningEffectiveFromDate <= df.SigningEffectiveThruDate && currentAdmissionPhysician.SigningEffectiveThruDate >= df.SigningEffectiveFromDate)
                    // row passed in has null thru date
                    || (currentAdmissionPhysician.SigningEffectiveThruDate == null && df.SigningEffectiveThruDate >= currentAdmissionPhysician.SigningEffectiveFromDate)
                    // row passed in has non null
                    || (df.SigningEffectiveThruDate == null && currentAdmissionPhysician.SigningEffectiveThruDate >= df.SigningEffectiveFromDate)
                    // both have non null thru dates
                    || (df.SigningEffectiveThruDate == null && currentAdmissionPhysician.SigningEffectiveThruDate == null)
                    ));
                if (_ret)
                {
                    string[] memberNames = new string[] { "Signing" };
                    return new ValidationResult("Only one physician can sign orders at any time for the patient admission", memberNames);
                }
            }
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            IEnumerable<int> codes_that_cannot_overlap = null;
            if (codeLookupDataProvider != null)
            {
                var physicianTypeCodes = codeLookupDataProvider.GetCodeLookupsFromType("PHTP");
                if (physicianTypeCodes == null)
                {
                    string[] memberNames = new string[] { "PhysicianEffectiveFromDate" };
                    return new ValidationResult("Physician effective from date cannot be later than the effective thru date.", memberNames);
                }
                else
                {
                    string[] codes = { "Admit", "PCP", "Refer" };
                    codes_that_cannot_overlap = physicianTypeCodes.Where(t => codes.Contains(t.Code)).Select(c => c.CodeLookupKey);
                }

                //DS 10/15/14 Task 14041
                if (currentAdmissionPhysician.SigningEffectiveFromDate > currentAdmissionPhysician.SigningEffectiveThruDate)
                {
                    string[] memberNames = new string[] { "SigningEffectiveFromDate" };
                    return new ValidationResult("Signed effective from date cannot be later than the effective thru date.", memberNames);
                }


                if (codes_that_cannot_overlap != null)
                {
                    //Overlapping dates by PhysicianType - then the type = "Admitting' or 'Attending/Primary Care' or 'Referring'
                    if (adf.Any(df => df.AdmissionPhysicianKey != currentAdmissionPhysician.AdmissionPhysicianKey
                            && df.PhysicianType == currentAdmissionPhysician.PhysicianType
                            && codes_that_cannot_overlap.Contains(currentAdmissionPhysician.PhysicianType)
                            && df.Inactive == false
                            && currentAdmissionPhysician.Inactive == false
                        // All non null dates
                        && ((currentAdmissionPhysician.PhysicianEffectiveFromDate <= df.PhysicianEffectiveThruDate && currentAdmissionPhysician.PhysicianEffectiveThruDate >= df.PhysicianEffectiveFromDate)
                        // row passed in has null thru date
                        || (currentAdmissionPhysician.PhysicianEffectiveThruDate == null && df.PhysicianEffectiveThruDate >= currentAdmissionPhysician.PhysicianEffectiveFromDate)
                        // row passed in has non null
                        || (df.PhysicianEffectiveThruDate == null && currentAdmissionPhysician.PhysicianEffectiveThruDate >= df.PhysicianEffectiveFromDate)
                        // both have non null thru dates
                        || (df.PhysicianEffectiveThruDate == null && currentAdmissionPhysician.PhysicianEffectiveThruDate == null)
                        )))
                    {
                        string[] memberNames = new string[] { "PhysicianEffectiveFromDate" };
                        return new ValidationResult("Admission physician effective dates must not overlap for the same type", memberNames);
                    }
                }
            }

            return ValidationResult.Success;
        }

        public static ValidationResult AdmissionCoverageValidate(AdmissionCoverage currentAdmissionCoverage, ValidationContext validationContext)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //NOTE: this function is essentially a copy of the date range logic from AdmissionDisciplineFrequencyValidate(...)
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            var admissionDataProvider = validationContext.GetService(typeof(IAdmissionDataProvider)) as IAdmissionDataProvider;

            if (currentAdmissionCoverage.StartDate == DateTime.MinValue)
            {
                string[] memberNames = new string[] { "StartDate" };
                return new ValidationResult("From Date is required.", memberNames);
            }

            // Crossed Dates
            if (currentAdmissionCoverage.StartDate > currentAdmissionCoverage.EndDate)
            {
                string[] memberNames = new string[] { "StartDate", "EndDate" };
                return new ValidationResult("From date cannot be later than the end date.", memberNames);
            }

            IQueryable<AdmissionCoverage> coverage = null;
            if (admissionDataProvider != null)
            {
                coverage = admissionDataProvider.GetAdmissionCoverage(currentAdmissionCoverage.Admission, currentAdmissionCoverage.AdmissionKey);
            }

            var _ret = coverage.Any(cov => (cov.AdmissionCoverageKey != currentAdmissionCoverage.AdmissionCoverageKey)
                && (currentAdmissionCoverage.CoverageTypeKey == cov.CoverageTypeKey)
                && (currentAdmissionCoverage.StartDate <= cov.StartDate)
                && (!currentAdmissionCoverage.EndDate.HasValue
                    || (currentAdmissionCoverage.EndDate >= cov.StartDate)
                   )
                );
            if (_ret)
            {
                string[] memberNames = new string[] { "StartDate", "EndDate" };
                return new ValidationResult("From and thru dates must not overlap for the same coverage type", memberNames);
            }
            // restrict the AdmissionCoverageInsurance validations to the client - 
            // currentAdmissionCoverage.AdmissionCoverageInsurance is only populated on the client, the count is zero on the server - go figure?
            // this is really only applicable to the "The total billing percentage with each coverage plan must be 100" validation as coded below
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS

            try
            {
                string insurances = null;
                string comma = null;

                if (currentAdmissionCoverage.AdmissionCoverageInsurance != null)
                {
                    foreach (var covIns in currentAdmissionCoverage.AdmissionCoverageInsurance
                                            .Where(ins => !ins.Inactive
                                                          && ((ins.PatientInsurance.EffectiveFromDate > currentAdmissionCoverage.StartDate)
                                                              || ((ins.PatientInsurance.EffectiveThruDate != null)
                                                                   && ((currentAdmissionCoverage.EndDate == null)
                                                                        || (ins.PatientInsurance.EffectiveThruDate < currentAdmissionCoverage.EndDate)
                                                                      )
                                                                 )
                                                              )
                                                 )
                           )
                    {
                        insurances += comma + covIns.PatientInsurance.Insurance.Name + " From " + covIns.PatientInsurance.EffectiveFromDate.Value.ToShortDateString()
                                            + " Thru " + (covIns.PatientInsurance.EffectiveThruDate.HasValue
                                                             ? covIns.PatientInsurance.EffectiveThruDate.Value.ToShortDateString()
                                                             : "None"
                                                         );
                        comma = ", ";
                    }
                }
                if (!string.IsNullOrEmpty(insurances))
                {
                    string[] memberNames = new string[] { "StartDate", "EndDate" };
                    return new ValidationResult("The following Insurances do not cover the entire range of the coverage plan: " + insurances, memberNames);
                }

            }
            catch
            {
            }

            foreach (AdmissionCoverageInsurance ins in currentAdmissionCoverage.AdmissionCoverageInsurance)
            {
                if ((ins.BillingPercent < 0)
                    || (ins.BillingPercent > 100)
                  )
                {
                    string[] memberNames = new string[] { "BillingPercent", "CoverageTypeKey" };
                    return new ValidationResult("The billing percentage must be between  0 and 100", memberNames);
                }
            }

            decimal? totalPercent = currentAdmissionCoverage.AdmissionCoverageInsurance.Where(aci => !aci.Inactive).Sum(p => p.BillingPercent);

            if (totalPercent != 100)
            {
                string[] memberNames = new string[] { "BillingPercent", "CoverageTypeKey" };
                return new ValidationResult("The total billing percentage with each coverage plan must be 100", memberNames);
            }
#endif
            return ValidationResult.Success;
        }
        public static ValidationResult ValidateAdmissionDisciplineDischarge(AdmissionDiscipline admissionDiscipline, ValidationContext validationContext)
        {
            if (admissionDiscipline.DischargeDateTime.HasValue && admissionDiscipline.DisciplineAdmitDateTime.HasValue 
                && admissionDiscipline.DischargeDateTime.Value.Date < admissionDiscipline.DisciplineAdmitDateTime.Value.Date)
            {

                string[] memberNames = new string[] { "DischargeDateTime" };
                return new ValidationResult("Discharge Date cannot be before the Admit Date.", memberNames);
            }
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                var _admissionStatusCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(admissionDiscipline.AdmissionStatus);
                if (_admissionStatusCode == "D" && !admissionDiscipline.DischargeDateTime.HasValue)
                {
                    string[] memberNames = new string[] { "DischargeDateTime" };
                    return new ValidationResult("Discipline is marked as discharged.  Discharge date cannot be null.", memberNames);
                }
                if ((_admissionStatusCode == "D" || _admissionStatusCode == "A") && !admissionDiscipline.DisciplineAdmitDateTime.HasValue)
                {
                    string[] memberNames = new string[] { "DisciplineAdmitDateTime" };
                    var statusString = codeLookupDataProvider.GetCodeLookupCodeDescriptionFromKey(admissionDiscipline.AdmissionStatus);
                    String msg = String.Format("Discipline is marked as {0}.  Admit date cannot be null.", statusString);
                    return new ValidationResult(msg, memberNames);
                }
            }
            else
            {
                string[] memberNames = new string[] { "DischargeDateTime" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult ValidateAdmissionDocumentation(AdmissionDocumentation ad, ValidationContext validationContext)
        {
            if (ad == null || ad.AttachedDocument == null || ad.OverridePDFError)
            {
                return ValidationResult.Success;
            }
            string[] memberNames = new string[] { "DocumentationFileName" };
            var admissionDataProvider = validationContext.GetService(typeof(IAdmissionDataProvider)) as IAdmissionDataProvider;
            if (admissionDataProvider != null)
            {
                var validDoc = admissionDataProvider.IsPDFValid(ad.AttachedDocument);
                if (!validDoc)
                {
                    return new ValidationResult("The selected PDF is either invalid or damaged.  This document cannot be imported.", memberNames);
                }
            }
            else
            {
                return new ValidationResult("Admission data provider is NULL", memberNames);
            }
            //#endif
            return ValidationResult.Success;
        }
    }
}