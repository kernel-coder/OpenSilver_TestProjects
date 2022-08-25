#region Usings

using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class PTINR : QuestionUI
    {
        public PTINR(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private EncounterPTINR _EncounterPTINR;

        public EncounterPTINR EncounterPTINR
        {
            get { return _EncounterPTINR; }
            set
            {
                _EncounterPTINR = value;
                this.RaisePropertyChangedLambda(p => p.EncounterPTINR);
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            EncounterPTINR.ValidationErrors.Clear();

            if (EncounterPTINR.INRRatio > 0 || (Required && Encounter.FullValidation))
            {
                if (EncounterPTINR.Validate())
                {
                    if (EncounterPTINR.IsNew)
                    {
                        Encounter.EncounterPTINR.Add(EncounterPTINR);
                    }

                    return true;
                }

                return false;
            }

            if (EncounterPTINR.EntityState == EntityState.Modified)
            {
                Encounter.EncounterPTINR.Remove(EncounterPTINR);
                EncounterPTINR = new EncounterPTINR();
            }

            return true;
        }
    }

    public class PTINRFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterPTINR ptinr = vm.CurrentEncounter.EncounterPTINR.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey);
            if (ptinr == null)
            {
                ptinr = new EncounterPTINR();
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            return new PTINR(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterPTINR = ptinr,
                OasisManager = vm.CurrentOasisManager,
            };
        }
    }
}