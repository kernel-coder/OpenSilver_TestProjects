#region Usings

using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class SimpleROM : QuestionBase
    {
        public SimpleROM(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private bool _QuestionHidden;

        public bool QuestionHidden
        {
            get { return RightHidden && LeftHidden; }
            set
            {
                if (_QuestionHidden != value)
                {
                    _QuestionHidden = value;

                    this.RaisePropertyChangedLambda(p => p.QuestionHidden);
                }
            }
        }

        private bool _RightHidden;

        public bool RightHidden
        {
            get { return _RightHidden; }
            set
            {
                if (_RightHidden != value)
                {
                    _RightHidden = value;

                    this.RaisePropertyChangedLambda(p => p.RightHidden);
                    this.RaisePropertyChangedLambda(p => p.QuestionHidden);
                }
            }
        }

        private bool _LeftHidden;

        public bool LeftHidden
        {
            get { return _LeftHidden; }
            set
            {
                if (_LeftHidden != value)
                {
                    _LeftHidden = value;

                    this.RaisePropertyChangedLambda(p => p.LeftHidden);
                    this.RaisePropertyChangedLambda(p => p.QuestionHidden);
                }
            }
        }

        public override void ProcessAmputationMessage(int[] message)
        {
            int value = message[2];

            if (value > 0)
            {
                value--;
            }

            if (value == 4)
            {
                value = 3;
            }

            if (message[1] == 0)
            {
                if (value == 0 || value >= Sequence - 1)
                {
                    RightHidden = false;
                }
                else
                {
                    RightHidden = true;
                    EncounterData.TextData = null;
                }
            }
            else
            {
                if (value == 0 || value >= Sequence - 1)
                {
                    LeftHidden = false;
                }
                else
                {
                    LeftHidden = true;
                    EncounterData.Text2Data = null;
                }
            }
        }
    }

    public class SimpleROMFactory
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

            SimpleROM sr = new SimpleROM(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = ed,
                OasisManager = vm.CurrentOasisManager,
            };

            var qg = formsection.FormSectionQuestion.Where(p => p.QuestionGroupKey == qgkey)
                .Select(p => p.QuestionGroup).FirstOrDefault();
            if (qg != null)
            {
                var seq = qg.QuestionGroupQuestion.Where(p => p.QuestionKey == q.QuestionKey).FirstOrDefault().Sequence;
                sr.Sequence = seq;
            }

            return sr;
        }
    }
}