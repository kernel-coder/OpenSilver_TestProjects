#region Usings

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class SingleDiagnosis : QuestionBase
    {
        public SingleDiagnosis(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public bool ShowICD9
        {
            get
            {
                if ((EncounterData != null) && (string.IsNullOrWhiteSpace(EncounterData.TextData) == false))
                {
                    return true;
                }

                DateTime date = (Encounter == null) || (Encounter.EncounterOrTaskStartDateAndTime == null)
                    ? DateTime.Today.Date
                    : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;
                if (date > TenantSettingsCache.Current.TenantSettingICD9CessationDate.Date)
                {
                    return false;
                }

                return true;
            }
        }

        public bool ShowICD10
        {
            get
            {
                if ((EncounterData != null) && (string.IsNullOrWhiteSpace(EncounterData.Text2Data) == false))
                {
                    return true;
                }

                DateTime date = (Encounter == null) || (Encounter.EncounterOrTaskStartDateAndTime == null)
                    ? DateTime.Today.Date
                    : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;
                if (date < TenantSettingsCache.Current.TenantSettingICD10PresentDate.Date)
                {
                    return false;
                }

                return true;
            }
        }

        public bool RequireICD9
        {
            get
            {
                if (ShowICD9 == false)
                {
                    return false;
                }

                if (EncounterData == null)
                {
                    return false;
                }

                DateTime date = (Encounter == null) || (Encounter.EncounterOrTaskStartDateAndTime == null)
                    ? DateTime.Today.Date
                    : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;
                if (date < TenantSettingsCache.Current.TenantSettingICD10PreviousMedicalSettingRequiredDate.Date)
                {
                    return true;
                }

                return false;
            }
        }

        public bool RequireICD10
        {
            get
            {
                if (ShowICD10 == false)
                {
                    return false;
                }

                if (EncounterData == null)
                {
                    return false;
                }

                DateTime date = (Encounter == null) || (Encounter.EncounterOrTaskStartDateAndTime == null)
                    ? DateTime.Today.Date
                    : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;
                if (date >= TenantSettingsCache.Current.TenantSettingICD10PreviousMedicalSettingRequiredDate.Date)
                {
                    return true;
                }

                return false;
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            if (EncounterData == null)
            {
                return true;
            }

            EncounterData.ValidationErrors.Clear();
            bool valid = true;

            if (((Required && Encounter.FullValidation) || ConditionalRequired) && (Hidden == false) &&
                (Protected == false) && RequireICD9 && string.IsNullOrWhiteSpace(EncounterData.TextData))
            {
                EncounterData.ValidationErrors.Add(
                    new ValidationResult((string.Format("{0} (ICD9) is required", Label)), new[] { "TextData" }));
                valid = false;
            }

            if (((Required && Encounter.FullValidation) || ConditionalRequired) && (Hidden == false) &&
                (Protected == false) && RequireICD10 && string.IsNullOrWhiteSpace(EncounterData.Text2Data))
            {
                EncounterData.ValidationErrors.Add(
                    new ValidationResult((string.Format("{0} (ICD10) is required", Label)), new[] { "Text2Data" }));
                valid = false;
            }

            if ((string.IsNullOrWhiteSpace(EncounterData.TextData) == false) ||
                (string.IsNullOrWhiteSpace(EncounterData.Text2Data) == false))
            {
                if (EncounterData.IsNew)
                {
                    Encounter.EncounterData.Add(EncounterData);
                }
            }
            else
            {
                if (EncounterData.EntityState == EntityState.Modified)
                {
                    Encounter.EncounterData.Remove(EncounterData);
                    EncounterData = new EncounterData
                    {
                        SectionKey = EncounterData.SectionKey, QuestionGroupKey = EncounterData.QuestionGroupKey,
                        QuestionKey = EncounterData.QuestionKey
                    };
                }
            }

            return valid;
        }
    }

    public class SingleDiagnosisFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterData ed = vm.CurrentEncounter.EncounterData.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey 
                && x.SectionKey == formsection.Section.SectionKey 
                && x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            SingleDiagnosis sd = new SingleDiagnosis(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = ed,
                OasisManager = vm.CurrentOasisManager,
            };
            if ((ed.IsNew) && (copyforward))
            {
                sd.CopyForwardLastInstance();
            }

            ed.PropertyChanged += sd.EncounterData_PropertyChanged;
            // Override default protection on ICD fields - allowing users with the ICDCoder role to edit them if the encounter is in CodeReview state
            if ((vm.CurrentEncounter != null) && (vm.CurrentEncounter.Inactive == false))
            {
                if ((vm.CurrentEncounter.EncounterStatus == (int)EncounterStatusType.CoderReview) &&
                    RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false))
                {
                    sd.ProtectedOverrideRunTime = false;
                }

                if (vm.CurrentEncounter.UserIsPOCOrderReviewerAndInPOCOrderReview)
                {
                    sd.ProtectedOverrideRunTime = false;
                }
            }

            return sd;
        }
    }
}