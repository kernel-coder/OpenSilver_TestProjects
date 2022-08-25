#region Usings

using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    // FYI Have no PatientCodeStatus entity, but do have a Question in the Question table where BackingFactory = 'PatientCodeStatus'
    public class PatientCodeStatusFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            QuestionBase pcs = new QuestionBase(__FormSectionQuestionKey)
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

            EncounterData ed = vm.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
                pcs.EncounterData = ed;
                pcs.ApplyDefaults();

                if (pcs.Encounter.IsNew && copyforward)
                {
                    pcs.CopyForwardLastInstance();
                }

                if (vm != null && vm.CurrentAdmission != null)
                {
                    pcs.EncounterData.TextData = vm.CurrentAdmission.CodeStatusString;
                }
            }
            else
            {
                pcs.EncounterData = ed;
            }

            return pcs;
        }
    }
}