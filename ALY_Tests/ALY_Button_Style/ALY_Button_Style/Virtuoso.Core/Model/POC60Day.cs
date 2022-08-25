#region Usings

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class POC60DaySummary : QuestionBase
    {
        public POC60DaySummary(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private AdmissionCertification AdmissionCertification;

        public void POC60DaySummarySetup()
        {
            if (DynamicFormViewModel != null)
            {
                AdmissionCertification = DynamicFormViewModel.GetAdmissionCertificationFor60DaySummary();
            }

            // Check to see if the current encounter is a recert POC
            // If not, hide the POC60DaySummary section
            if (AdmissionCertification != null)
            {
                // Default value for DateTime as we cannot have them be nullable
                DateTime periodStartDate = DateTime.MinValue;
                DateTime socDate = DateTime.MinValue;

                if (AdmissionCertification.PeriodStartDate != null)
                {
                    periodStartDate = (DateTime)AdmissionCertification.PeriodStartDate;
                }

                if (Admission != null &&
                    Admission.SOCDate != null)
                {
                    socDate = (DateTime)Admission.SOCDate;
                }

                if (socDate != DateTime.MinValue && periodStartDate != DateTime.MinValue &&
                    socDate.Date == periodStartDate.Date)
                {
                    // this is a recert POC
                    Hidden = true;
                    return;
                }
            }

            POC60DaySummarySetupVisitSummaries();
        }

        private void POC60DaySummarySetupVisitSummaries()
        {
            AdmissionCertification PreviousCert = null;
            if (Admission != null && AdmissionCertification != null)
            {
                PreviousCert =
                    Admission.GetAdmissionCertificationByPeriodNumber(AdmissionCertification.PeriodNumber - 1);
            }

            if ((Admission == null) || (PreviousCert == null) || (EncounterData == null))
            {
                EncounterData.Text2Data = null;
                return;
            }

            if (EncounterData.IsNew == false)
            {
                return; // Default only once - at start of POC
            }

            EncounterData.Text2Data = Admission.GetPOC60DayVisitSummaryHistory(PreviousCert);
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            EncounterData.ValidationErrors.Clear();

            if (EncounterData.IsNew)
            {
                Encounter.EncounterData.Add(EncounterData);
            }

            if (string.IsNullOrWhiteSpace(EncounterData.TextData) && (Encounter.FullValidation) && (Hidden == false))
            {
                EncounterData.ValidationErrors.Add(new ValidationResult("The 60-Day Summary field is required.",
                    new[] { "TextData" }));
                return false;
            }

            return true;
        }
    }

    public class POC60DaySummaryFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterData ed = vm.CurrentEncounter.EncounterData.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey 
                && x.SectionKey == formsection.Section.SectionKey 
                && x.QuestionGroupKey == qgkey 
                && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            POC60DaySummary p60 = new POC60DaySummary(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = ed,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm
            };
            p60.POC60DaySummarySetup();
            return p60;
        }
    }

    public class POC60DaySummarySignature : QuestionBase
    {
        public POC60DaySummarySignature(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private AdmissionCertification AdmissionCertification;

        public void POC60DaySummarySignatureSetup()
        {
            PageBreakAfter = true;
            if (DynamicFormViewModel != null)
            {
                AdmissionCertification = DynamicFormViewModel.GetAdmissionCertificationFor60DaySummary();
            }

            // Check to see if the current encounter is a recert POC
            // If not, hide the POC60DaySummary section
            if (AdmissionCertification != null)
            {
                // Default value for DateTime as we cannot have them be nullable
                DateTime periodStartDate = DateTime.MinValue;
                DateTime socDate = DateTime.MinValue;

                if (AdmissionCertification.PeriodStartDate != null)
                {
                    periodStartDate = (DateTime)AdmissionCertification.PeriodStartDate;
                }

                if (DynamicFormViewModel != null &&
                    DynamicFormViewModel.CurrentAdmission != null &&
                    DynamicFormViewModel.CurrentAdmission.SOCDate != null)
                {
                    socDate = (DateTime)DynamicFormViewModel.CurrentAdmission.SOCDate;
                }

                if (socDate != DateTime.MinValue && periodStartDate != DateTime.MinValue &&
                    socDate.Date == periodStartDate.Date)
                {
                    // this is a recert POC
                    Hidden = true;
                }
            }
        }
    }

    public class POC60DaySummarySignatureFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterData ed = vm.CurrentEncounter.EncounterData
                .FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey 
                                     && x.SectionKey == formsection.Section.SectionKey 
                                     && x.QuestionGroupKey == qgkey 
                                     && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            POC60DaySummarySignature p60s = new POC60DaySummarySignature(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = ed,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm
            };
            p60s.POC60DaySummarySignatureSetup();
            return p60s;
        }
    }
}