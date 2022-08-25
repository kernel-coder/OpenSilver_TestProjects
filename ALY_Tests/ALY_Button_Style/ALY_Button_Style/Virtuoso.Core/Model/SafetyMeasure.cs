#region Usings

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class SafetyMeasure
    {
        public string Description { get; set; }
    }

    public class POCSafetyMeasures : QuestionUI
    {
        public POCSafetyMeasures(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public ObservableCollection<SafetyMeasure> SafetyMeasures { get; set; }

        public bool CopyForwardLastInstance(EncounterPlanOfCare epcParm)
        {
            // already done in dynmaic form init
            return false;
        }
    }

    public class POCSafetyMeasuresFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            POCSafetyMeasures s = new POCSafetyMeasures(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
            };

            s.SafetyMeasures = new ObservableCollection<SafetyMeasure>();
            EncounterPlanOfCare epc = vm.CurrentEncounter.EncounterPlanOfCare.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey);

            if ((epc != null) && (epc.POCSafetyMeasures != null))
            {
                string[] delimit = { "|" };
                string[] smSplit = epc.POCSafetyMeasures.Split(delimit, StringSplitOptions.RemoveEmptyEntries);
                foreach (string sm in smSplit)
                {
                    SafetyMeasure smo = new SafetyMeasure { Description = sm };
                    s.SafetyMeasures.Add(smo);
                    ;
                }
            }

            return s;
        }
    }
}