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
    public class ROM : QuestionUI
    {
        public ROM(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private EncounterROM _EncounterROM;

        public EncounterROM EncounterROM
        {
            get { return _EncounterROM; }
            set
            {
                _EncounterROM = value;
                this.RaisePropertyChangedLambda(p => p.EncounterROM);
            }
        }

        public EncounterROM BackupEncounterROM { get; set; }
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

            if (message[0] == ParentGroupKey)
            {
                if (message[1] == 0)
                {
                    if (value == 0 || value >= Sequence - 1)
                    {
                        RightHidden = false;
                    }
                    else
                    {
                        RightHidden = true;
                        EncounterROM.ActiveRightHigh = null;
                        EncounterROM.ActiveRightLow = null;
                        EncounterROM.PassiveRightHigh = null;
                        EncounterROM.PassiveRightLow = null;
                        EncounterROM.StrengthRight = null;
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
                        EncounterROM.ActiveLeftHigh = null;
                        EncounterROM.ActiveLeftLow = null;
                        EncounterROM.PassiveLeftHigh = null;
                        EncounterROM.PassiveLeftLow = null;
                        EncounterROM.StrengthLeft = null;
                    }
                }
            }
        }

        void CopyProperties(EncounterROM source)
        {
            EncounterROM.ActiveLeftHigh = source.ActiveLeftHigh;
            EncounterROM.ActiveLeftLow = source.ActiveLeftLow;
            EncounterROM.ActiveRightHigh = source.ActiveRightHigh;
            EncounterROM.ActiveRightLow = source.ActiveRightLow;
            EncounterROM.PassiveLeftHigh = source.PassiveLeftHigh;
            EncounterROM.PassiveLeftLow = source.PassiveLeftLow;
            EncounterROM.PassiveRightHigh = source.PassiveRightHigh;
            EncounterROM.PassiveRightLow = source.PassiveRightLow;
            EncounterROM.StrengthLeft = source.StrengthLeft;
            EncounterROM.StrengthRight = source.StrengthRight;
        }

        public override bool CopyForwardLastInstance()
        {
            foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                EncounterROM previous = item.EncounterROM.FirstOrDefault(d => d.QuestionKey == Question.QuestionKey);
                if (previous != null)
                {
                    CopyProperties(previous);
                    return true;
                }
            }

            return false;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
            EncounterROM previous = e.EncounterROM.FirstOrDefault(p => p.QuestionKey == Question.QuestionKey 
                                                                       && p.QuestionGroupKey == QuestionGroupKey 
                                                                       && p.Section.Label == Section.Label);
            if (previous != null)
            {
                CopyProperties(previous);
            }
        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                var previous = (EncounterROM)Clone(BackupEncounterROM);
                //need to copy so raise property changes gets called - can't just copy the entire object
                CopyProperties(previous);
            }
            else
            {
                BackupEncounterROM = (EncounterROM)Clone(EncounterROM);
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            EncounterROM.ValidationErrors.Clear();

            if (EncounterROM.ActiveLeftHigh.HasValue || EncounterROM.ActiveLeftLow.HasValue ||
                EncounterROM.ActiveRightHigh.HasValue ||
                EncounterROM.ActiveRightLow.HasValue || EncounterROM.PassiveLeftHigh.HasValue ||
                EncounterROM.PassiveLeftLow.HasValue ||
                EncounterROM.PassiveRightHigh.HasValue || EncounterROM.PassiveRightLow.HasValue ||
                EncounterROM.StrengthLeft.HasValue || EncounterROM.StrengthRight.HasValue ||
                (Required && Encounter.FullValidation))
            {
                if (EncounterROM.Validate())
                {
                    if (EncounterROM.IsNew)
                    {
                        Encounter.EncounterROM.Add(EncounterROM);
                    }

                    return true;
                }

                return false;
            }

            if (EncounterROM.EntityState == EntityState.Modified)
            {
                Encounter.EncounterROM.Remove(EncounterROM);
                EncounterROM = new EncounterROM
                {
                    SectionKey = EncounterROM.SectionKey, QuestionGroupKey = EncounterROM.QuestionGroupKey,
                    QuestionKey = EncounterROM.QuestionKey
                };
            }

            return true;
        }
    }

    public class ROMFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterROM er = vm.CurrentEncounter.EncounterROM.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey 
                && x.SectionKey == formsection.Section.SectionKey 
                && x.QuestionGroupKey == qgkey 
                && x.QuestionKey == q.QuestionKey);

            if (er == null)
            {
                er = new EncounterROM
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            return new ROM(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Sequence = sequence,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterROM = er,
                OasisManager = vm.CurrentOasisManager,
            };
        }
    }
}