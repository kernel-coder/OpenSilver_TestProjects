#region Usings

using System.ComponentModel;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class DeathNote : QuestionBase
    {
        public DeathNote(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void Encounter_VSPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // hide or show/add the Death Note section and questions.
            if (e.PropertyName != "DeathNote")
            {
                return;
            }

            if (EncounterData != null && Encounter != null)
            {
                EncounterData.TextData =
                    (Encounter.DeathNote == null ? "0" : (Encounter.DeathNote == true ? "1" : "0"));
            }

            ProcessUIChanges();
        }

        private void ProcessUIChanges()
        {
            if (Admission != null && Encounter != null && Encounter.DeathNote != true)
            {
                Admission.DeathDate = null;
                Admission.DeathTime = null;
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                Messenger.Default.Send((Encounter.DeathNote == null ? false : (bool)Encounter.DeathNote),
                    string.Format("RefreshFilteredSections{0}", Encounter.EncounterID.ToString().Trim()));
            });
        }

        public override void Cleanup()
        {
            if (Encounter != null)
            {
                Encounter.PropertyChanged -= Encounter_VSPropertyChanged;
            }

            base.Cleanup();
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;

            // Override this method so nothing is required for this question through questionbase.
            return AllValid;
        }
    }

    public class DeathNoteFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            DeathNote qb = new DeathNote(__FormSectionQuestionKey)
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

            if (qb.Encounter != null && qb.Encounter.DeathNote != true && qb.Admission != null &&
                qb.Admission.Encounter != null)
            {
                // Death note can only appear on one encounter.
                if (qb.Admission.Encounter.Any(e =>
                        e.Inactive == false && e.DeathNote == true && e.EncounterKey != qb.Encounter.EncounterKey))
                {
                    qb.Encounter.DeathNote = false;
                    if (qb.Question != null)
                    {
                        qb.Question.ProtectedOverride = true;
                    }
                }
            }

            if (qb.Encounter != null && qb.Encounter.EncounterStatus != (int)EncounterStatusType.Completed)
            {
                qb.Encounter.PropertyChanged += qb.Encounter_VSPropertyChanged;
            }

            // Used for the QuestionNotificationLogic ONLY!
            if (qb.EncounterData == null)
            {
                qb.EncounterData = new EncounterData();
            }

            qb.EncounterData.TextData =
                (qb.Encounter.DeathNote == null ? "0" : (qb.Encounter.DeathNote == true ? "1" : "0"));

            return qb;
        }
    }
}