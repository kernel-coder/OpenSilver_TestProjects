#region Usings

using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class NPWT : QuestionBase
    {
        public NPWT(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private EncounterNPWT _EncounterNPWT;

        public EncounterNPWT EncounterNPWT
        {
            get { return _EncounterNPWT; }
            set
            {
                _EncounterNPWT = value;
                this.RaisePropertyChangedLambda(p => p.EncounterNPWT);
            }
        }

        public void SetupNPWT()
        {
            if ((Encounter != null) && (Encounter.EncounterNPWT != null))
            {
                EncounterNPWT = Encounter.EncounterNPWT.FirstOrDefault();
                if ((EncounterNPWT == null) && CanPerformNPWT &&
                    (Encounter.EncounterStatus == (int)EncounterStatusType.Edit))
                {
                    EncounterNPWT = new EncounterNPWT
                        { EncounterKey = Encounter.EncounterKey, ProcedurePerformedFlag = false };
                    Encounter.EncounterNPWT.Add(EncounterNPWT);
                }
            }

            HiddenOverride = SetupNPWTHiddenOverride();
        }

        private bool SetupNPWTHiddenOverride()
        {
            return (EncounterNPWT == null) ? true : false;
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            if ((Encounter == null) || (EncounterNPWT == null))
            {
                return true;
            }

            if (Encounter.FullValidation == false)
            {
                return true;
            }

            int visitTotalMinutes = (Encounter.EncounterActualTime == null) ? 0 : (int)Encounter.EncounterActualTime;
            bool AllValid = EncounterNPWT.ValidateNPWTFields(visitTotalMinutes);
            return AllValid;
        }
    }

    public class NPWTFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            NPWT n = new NPWT(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline,
                OasisManager = vm.CurrentOasisManager,
            };
            n.SetupNPWT();
            return n;
        }
    }
}