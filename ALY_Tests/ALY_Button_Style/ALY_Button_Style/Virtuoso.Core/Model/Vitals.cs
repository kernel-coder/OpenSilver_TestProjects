#region Usings

using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class Vitals : QuestionUI
    {
        public Vitals(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public EncounterTemp EncounterTemp { get; set; }
        public EncounterPulse EncounterPulse { get; set; }
        public EncounterResp EncounterResp { get; set; }
        public EncounterBP EncounterBP { get; set; }
    }

    public class VitalsFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            Vitals v = new Vitals(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
            };

            EncounterTemp et = vm.CurrentEncounter.EncounterTemp
                .Where(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey).FirstOrDefault();
            if (et == null)
            {
                foreach (var e in v.Admission.Encounter.OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                {
                    et = e.EncounterTemp.OrderByDescending(p => p.Sequence).FirstOrDefault();
                    if (et != null)
                    {
                        v.EncounterTemp = et;
                        break;
                    }
                }
            }
            else
            {
                v.EncounterTemp = et;
            }

            EncounterPulse ep = vm.CurrentEncounter.EncounterPulse.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey);
            if (ep == null)
            {
                foreach (var e in v.Admission.Encounter.OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                {
                    ep = e.EncounterPulse.OrderByDescending(p => p.Sequence).FirstOrDefault();
                    if (ep != null)
                    {
                        v.EncounterPulse = ep;
                        break;
                    }
                }
            }
            else
            {
                v.EncounterPulse = ep;
            }

            EncounterResp er = vm.CurrentEncounter.EncounterResp.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey);
            if (er == null)
            {
                foreach (var e in v.Admission.Encounter.OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                {
                    er = e.EncounterResp.OrderByDescending(p => p.Sequence).FirstOrDefault();
                    if (er != null)
                    {
                        v.EncounterResp = er;
                        break;
                    }
                }
            }
            else
            {
                v.EncounterResp = er;
            }

            EncounterBP eb = vm.CurrentEncounter.EncounterBP.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey);
            if (eb == null)
            {
                foreach (var e in v.Admission.Encounter.OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                {
                    eb = e.EncounterBP.OrderByDescending(p => p.Sequence).FirstOrDefault();
                    if (eb != null)
                    {
                        v.EncounterBP = eb;
                        break;
                    }
                }
            }
            else
            {
                v.EncounterBP = eb;
            }

            return v;
        }
    }
}