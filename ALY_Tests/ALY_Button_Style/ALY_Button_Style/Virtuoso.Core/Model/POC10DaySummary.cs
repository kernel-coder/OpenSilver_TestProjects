#region Usings

using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    // FYI Have no POC10DaySummary entity, but do have a Question in the Question table where BackingFactory = 'POC10DaySummary'
    public class POC10DaySummaryFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            QuestionBase pf = new QuestionBase(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                EncounterData = q.EncounterData.FirstOrDefault(),
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
            };

            AdmissionCertification ac = vm.GetAdmissionCertForEncounter();

            pf.Hidden = true;

            if (vm.CurrentEncounter.EncounterIsPlanOfCare)
            {
                if (ac != null && ac.PeriodNumber <= 1)
                {
                    if (vm.CurrentAdmission != null && vm.CurrentAdmission.PatientInsurance != null)
                    {
                        var ins = InsuranceCache.GetInsuranceFromKey(vm.CurrentAdmission.PatientInsurance.InsuranceKey);
                        if (ins != null && ins.CertPOCSummaryRequired)
                        {
                            pf.Hidden = false;
                        }
                    }
                }
            }

            EncounterData ed = vm.CurrentEncounter.EncounterData
                .FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey 
                                     && x.SectionKey == formsection.Section.SectionKey 
                                     && x.QuestionGroupKey == qgkey 
                                     && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
                pf.EncounterData = ed;
                pf.ApplyDefaults();

                if (pf.Encounter.IsNew && copyforward)
                {
                    pf.CopyForwardLastInstance();
                }
            }
            else
            {
                pf.EncounterData = ed;
            }

            ed.PropertyChanged += pf.EncounterData_PropertyChanged;
            pf.Setup();

            return (pf);
        }
    }
}