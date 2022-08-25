#region Usings

using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    // FYI Have no VitalsList entity, but do have a Question in the Question table where BackingFactory = 'VitalsList'
    public class VitalsListFactory
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

            EncounterData ed = vm.CurrentEncounter.EncounterData.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey
                && x.SectionKey == formsection.Section.SectionKey
                && x.QuestionGroupKey == qgkey
                && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value,
                    QuestionGroupKey = qgkey,
                    QuestionKey = q.QuestionKey
                };
                pcs.EncounterData = ed;
                pcs.ApplyDefaults();

                if (pcs.Encounter.IsNew && copyforward)
                {
                    pcs.CopyForwardLastInstance();
                }

                if (vm.CurrentAdmission != null
                    && vm.CurrentAdmission.Encounter.Where(e => e.EncounterStatus != (int)EncounterStatusType.None) != null //Exclude Encounters that are place holders for Tasks that haven't been started yet
                    && vm.CurrentEncounter != null)
                {
                    // Right now, this will pick up future dated visits (those newer than ours).  Should it?
                    Encounter mostRecentEncounter = vm.CurrentAdmission.Encounter
                        .Where(e => e.EncounterStatus != (int)EncounterStatusType.None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                        .Where(enc => enc.EncounterKey != vm.CurrentEncounter.EncounterKey)
                        .OrderByDescending(e => e.EncounterOrTaskStartDateAndTime).FirstOrDefault();
                    var text = vm.CurrentAdmission.GetVitalsTextForEncounter(mostRecentEncounter);

                    pcs.EncounterData.TextData = text;
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