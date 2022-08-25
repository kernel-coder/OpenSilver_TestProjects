#region Usings

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class VisitSupervision : QuestionBase
    {
        public VisitSupervision(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void PreProcessing()
        {
            SetupVisitSupervision();
        }

        public bool? SetHiddenOverride()
        {
            // check to make sure the question should appear.
            if ((DynamicFormViewModel == null) || (Admission == null) || (Encounter == null) ||
                (Encounter.ServiceTypeKey == null))
            {
                // should never happen - exit if invalid setup allowing supervision
                return null;
            }

            if ((EncounterData != null) && (EncounterData.TextData == "1"))
            {
                // form re-edit and already setup for supervision - override everything and allow supervision
                return false;
            }

            if ((DynamicFormViewModel.IsAssistantEncounter) || (DynamicFormViewModel.CurrentEncounter.IsAssistant))
            {
                // an assistant is doing the encounter - never show it
                return true;
            }

            UserProfile up = UserCache.Current.GetCurrentUserProfile();
            if (up == null)
            {
                // should never happen - exit if invalid setup allowing supervision
                return null;
            }

            if (up.UserIsASupervisor == false)
            {
                // user is never a supervisor - never show it
                return true;
            }

            ServiceType st = ServiceTypeCache.GetServiceTypeFromKey((int)Encounter.ServiceTypeKey);
            if (st == null)
            {
                // should never happen - exit if invalid setup allowing supervision
                return null;
            }

            Discipline disc = DisciplineCache.GetDisciplineFromKey(st.DisciplineKey);
            if (disc == null)
            {
                // should never happen - exit if invalid setup allowing supervision
                return null;
            }

            if (disc.SupportsAssistants && up.UserCanSuperviseDiscipline(disc.DisciplineKey))
            {
                // Current discipline supports assistants and we can supervise this discipline - allow supervision
                return null; 
            }

            // Special override - allow supervision of any admitted Aide discipline that support supervision
            // provided this user can supervise that aide discipline.
            // Without could argue that a PT can supervise SN - but we are limiting the override to Aides (HCFACode = F)
            ICollection<AdmissionDiscipline> aList = Admission.ActiveAideAdmissionDisciplinesAdmitted; 
            
            if (aList != null)
            {
                foreach (AdmissionDiscipline ad in aList)
                {
                    Discipline aDisc = DisciplineCache.GetDisciplineFromKey(ad.DisciplineKey);
                    if ((aDisc != null) && aDisc.SupportsAssistants && up.UserCanSuperviseDiscipline(ad.DisciplineKey))
                    {
                        return null; // user can supervise this admitted aide discipline 
                    }
                }
            }

            // fell thru - no Aide supervision override
            return true; // Current discipline does not support assistants and we are a supervisor - never show it
        }

        public void EncounterData_VSPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // hide or show/add the supervision section and questions.
            if (e.PropertyName != "TextData")
            {
                return;
            }

            ProcessUIChanges();
        }

        private string PrevTextData;

        private void SetupVisitSupervision()
        {
            PrevTextData = EncounterData?.TextData;
        }

        private void ProcessUIChanges()
        {
            this.RaisePropertyChangedLambda(p => p.Hidden);
            if ((PrevTextData != EncounterData?.TextData) && (PrevTextData == "1") && (EncounterData?.TextData == "0"))
            {
                // Response going to No after being a Yes, Prompt: 'The supervision section will be cleared.  Proceed?'
                // and if I proceed the supervisory section is cleared
                NavigateCloseDialog d = CreateDialogue("Discipline supervision within this visit",
                    "The supervision section will be cleared.  Proceed?");
                if (d != null)
                {
                    d.Closed += (s, err) =>
                    {
                        if ((s != null) && (((ChildWindow)s).DialogResult == true))
                        {
                            foreach (EncounterSupervision es in Encounter.EncounterSupervision.Reverse())
                            {
                                Encounter.EncounterSupervision.Remove(es);
                                if ((DynamicFormViewModel != null) && (DynamicFormViewModel.FormModel != null))
                                {
                                    ((IPatientService)DynamicFormViewModel.FormModel).Remove(es);
                                }
                            }

                            ProcessUIChanges2();
                        }
                        else
                        {
                            EncounterData.TextData = "1";
                        }
                    };
                    d.Show();
                }
            }
            else
            {
                ProcessUIChanges2();
            }
        }

        private void ProcessUIChanges2()
        {
            PrevTextData = EncounterData?.TextData;
            Messenger.Default.Send((EncounterData.TextData == null ? false : (EncounterData.TextData == "1")),
                string.Format("DoSupervisionWithVisitChanged{0}", Encounter.EncounterID.ToString().Trim()));
            if (EncounterData.TextData != "1")
            {
                Messenger.Default.Send((EncounterData.TextData == null ? false : (EncounterData.TextData == "1")),
                    string.Format("RemoveSupervisedQuestions{0}", Encounter.EncounterKey.ToString().Trim()));
            }
        }

        public override void Cleanup()
        {
            if (EncounterData != null)
            {
                EncounterData.PropertyChanged -= EncounterData_VSPropertyChanged;
            }

            base.Cleanup();
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;
            ValidationError = null;

            if (Hidden)
            {
                return AllValid;
            }

            if ((EncounterData != null) && (EncounterData.IsNew))
            {
                Encounter.EncounterData.Add(EncounterData);
            }

            if (Encounter?.EncounterSupervision == null)
            {
                return AllValid;
            }

            if (EncounterData == null)
            {
                return AllValid;
            }

            if (string.IsNullOrWhiteSpace(EncounterData.TextData))
            {
                EncounterData.TextData = null;
            }

            if ((EncounterData.TextData == null) && (Encounter.FullValidation) && ((Required || ConditionalRequired)))
            {
                EncounterData.BoolData = null;
                ValidationError = "Documenting a discipline supervision with this visit (Y/N) field is required.";
                EncounterData.ValidationErrors.Add(new ValidationResult(ValidationError, new[] { "TextData" }));
                AllValid = false;
                return AllValid;
            }

            EncounterData.BoolData = (EncounterData.TextData == "1") ? true : false;
            if ((EncounterData.BoolData == true) && (Encounter.EncounterSupervision.Any() == false) &&
                (Encounter.FullValidation))
            {
                ValidationError = "At least one Supervision is required.  Refer to the Supervision section.";
                EncounterData.ValidationErrors.Add(new ValidationResult(ValidationError, new[] { "TextData" }));
                AllValid = false;
            }

            return AllValid;
        }
    }

    public class VisitSupervisionFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            VisitSupervision qb = new VisitSupervision(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
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
                    QuestionKey = q.QuestionKey,
                    TextData = null,
                    BoolData = null
                };
                qb.EncounterData = ed;

                if (qb.Encounter.IsNew && copyforward)
                {
                    qb.CopyForwardLastInstance();
                }
            }
            else
            {
                qb.EncounterData = ed;
                // set initial status of the form
                Messenger.Default.Send((ed.TextData != null && (ed.TextData == "1")),
                    string.Format("DoSupervisionWithVisitChanged{0}", qb.Encounter.EncounterID.ToString().Trim()));
            }

            ed.PropertyChanged += qb.EncounterData_VSPropertyChanged;

            qb.HiddenOverride = qb.SetHiddenOverride();
            return qb;
        }
    }
}