using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Server.Data;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Virtuoso.Validation
{
    public static class AdmissionCommunicationValidations
    {
        public static ValidationResult AdmissionCommunicationValidateAssessmentText(string assessmentText, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                AdmissionCommunication ac = validationContext.ObjectInstance as AdmissionCommunication;
                if (ac == null) return ValidationResult.Success;

                var _communicationTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(ac.CommunicationType);
                if (_communicationTypeCode == null)
                    _communicationTypeCode = String.Empty;

                if ((_communicationTypeCode.ToUpper() == "SBAR") && (string.IsNullOrWhiteSpace(assessmentText)))
                {
                    string[] memberNames = new string[] { "AssessmentText" };
                    return new ValidationResult("The Assessment field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "AssessmentText" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionCommunicationValidateBackgroundText(string backgroundText, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                AdmissionCommunication ac = validationContext.ObjectInstance as AdmissionCommunication;
                if (ac == null) return ValidationResult.Success;

                var _communicationTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(ac.CommunicationType);
                if (_communicationTypeCode == null)
                    _communicationTypeCode = String.Empty;

                if (ac == null) return ValidationResult.Success;
                if ((_communicationTypeCode.ToUpper() == "SBAR") && (string.IsNullOrWhiteSpace(backgroundText)))
                {
                    string[] memberNames = new string[] { "BackgroundText" };
                    return new ValidationResult("The Background field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "BackgroundText" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionCommunicationValidateRecommendationText(string recommendationText, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                AdmissionCommunication ac = validationContext.ObjectInstance as AdmissionCommunication;
                if (ac == null) return ValidationResult.Success;

                var _communicationTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(ac.CommunicationType);
                if (_communicationTypeCode == null)
                    _communicationTypeCode = String.Empty;

                if ((_communicationTypeCode.ToUpper() == "SBAR") && (string.IsNullOrWhiteSpace(recommendationText)))
                {
                    string[] memberNames = new string[] { "RecommendationText" };
                    return new ValidationResult("The Recommendation field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "RecommendationText" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionCommunicationValidateSituationText(string situationText, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                AdmissionCommunication ac = validationContext.ObjectInstance as AdmissionCommunication;
                if (ac == null) return ValidationResult.Success;

                var _communicationTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(ac.CommunicationType);
                if (_communicationTypeCode == null)
                    _communicationTypeCode = String.Empty;

                if ((_communicationTypeCode.ToUpper() == "SBAR") && (string.IsNullOrWhiteSpace(situationText)))
                {
                    string[] memberNames = new string[] { "SituationText" };
                    return new ValidationResult("The Situation field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "SituationText" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionCommunicationValidateCoordinationOfCare(string coordinationOfCareText, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                AdmissionCommunication ac = validationContext.ObjectInstance as AdmissionCommunication;
                if (ac == null) return ValidationResult.Success;

                var _communicationTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(ac.CommunicationType);
                if (_communicationTypeCode == null)
                    _communicationTypeCode = String.Empty;

                if ((_communicationTypeCode.ToUpper() == "CareCoordinationNote".ToUpper()) && (string.IsNullOrWhiteSpace(coordinationOfCareText)))
                {
                    string[] memberNames = new string[] { "CoordinationOfCare" };
                    return new ValidationResult("The Coordination of Care with field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "CoordinationOfCare" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionCommunicationValidateNarrative(string narrativeText, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                AdmissionCommunication ac = validationContext.ObjectInstance as AdmissionCommunication;
                if (ac == null) return ValidationResult.Success;

                var _communicationTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(ac.CommunicationType);
                if (_communicationTypeCode == null)
                    _communicationTypeCode = String.Empty;

                if ((_communicationTypeCode.ToUpper() == "CareCoordinationNote".ToUpper()) && (string.IsNullOrWhiteSpace(narrativeText)))
                {
                    string[] memberNames = new string[] { "Narrative" };
                    return new ValidationResult("The Narrative field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "Narrative" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
        }
     
        public static ValidationResult AdmissionCommunicationValidateNoteText(string noteText, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                AdmissionCommunication ac = validationContext.ObjectInstance as AdmissionCommunication;
                if (ac == null) return ValidationResult.Success;

                var _communicationTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(ac.CommunicationType);
                if (_communicationTypeCode == null)
                    _communicationTypeCode = String.Empty;

                if (((_communicationTypeCode.ToLower() == "teamcasenote") || (_communicationTypeCode.ToLower() == "generalnote")) && (string.IsNullOrWhiteSpace(noteText)))
                {
                    string[] memberNames = new string[] { "NoteText" };
                    return new ValidationResult("The Note field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "NoteText" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionCommunicationValidateCompletedDatePart(DateTime completedDatePart, ValidationContext validationContext)
        {
            AdmissionCommunication ac = validationContext.ObjectInstance as AdmissionCommunication;
            if (ac == null) return ValidationResult.Success;
            //#if SILVERLIGHT
            //            if ((ac.CommunicationTypeCode.ToLower() == "teamcasenote") || (ac.CommunicationTypeCode.ToLower() == "generalnote"))
            //            {
            //                if (((completedDatePart.Date == DateTime.Today) || (completedDatePart.Date == DateTime.Today.AddDays(-1))) == false)
            //                {
            //                    string[] memberNames = new string[] { "CompletedDatePart" };
            //                    return new ValidationResult("Completed Date must be yesterday or today", memberNames);
            //                }
            //            }
            //#endif
            return ValidationResult.Success;
        }

        public static ValidationResult AdmissionCommunicationValidateContactedPhysicianKey(AdmissionCommunication communication, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                AdmissionCommunication ac = validationContext.ObjectInstance as AdmissionCommunication;
                if (ac == null) return ValidationResult.Success;

                var _communicationTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(ac.CommunicationType);
                if (_communicationTypeCode == null)
                    _communicationTypeCode = String.Empty;

                int key = (communication.ContactedPhysicianKey == null) ? 0 : (int)communication.ContactedPhysicianKey;
                if ((_communicationTypeCode.ToUpper() == "SBAR") && (key <= 0))
                {
                    string[] memberNames = new string[] { "ContactedPhysicianKey" };
                    return new ValidationResult("The Physician Contacted field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "ContactedPhysicianKey" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionCommunicationValidateCommunicationMode(AdmissionCommunication communication, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                AdmissionCommunication ac = validationContext.ObjectInstance as AdmissionCommunication;
                if (ac == null) return ValidationResult.Success;

                var _communicationTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(ac.CommunicationType);
                if (_communicationTypeCode == null)
                    _communicationTypeCode = String.Empty;

                if ((_communicationTypeCode.ToUpper() == "CareCoordinationNote".ToUpper()) && (string.IsNullOrWhiteSpace(communication.CommunicationMode)))
                {
                    string[] memberNames = new string[] { "CommunicationMode" };
                    return new ValidationResult("The Communication Mode field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "CommunicationMode" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
        }

        //public static ValidationResult AdmissionCommunicationValidateContactedPhysicianKey(int? contactedPhysicianKey, ValidationContext validationContext)
        //{
        //    var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
        //    if (codeLookupDataProvider != null)
        //    {
        //        AdmissionCommunication ac = validationContext.ObjectInstance as AdmissionCommunication;
        //        if (ac == null) return ValidationResult.Success;

        //        var _communicationTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(ac.CommunicationType);
        //        if (_communicationTypeCode == null)
        //            _communicationTypeCode = String.Empty;

        //        int key = (contactedPhysicianKey == null) ? 0 : (int)contactedPhysicianKey;
        //        if ((_communicationTypeCode.ToUpper() == "SBAR") && (key <= 0))
        //        {
        //            string[] memberNames = new string[] { "ContactedPhysicianKey" };
        //            return new ValidationResult("The Physician Contacted field is required", memberNames);
        //        }
        //        return ValidationResult.Success;
        //    }
        //    else
        //    {
        //        string[] memberNames = new string[] { "ContactedPhysicianKey" };
        //        return new ValidationResult("CodeLookup data provider is NULL", memberNames);
        //    }
        //}
    }
}
