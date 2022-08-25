#region Usings

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class MSPQuestionnaire : AttatchedForm
    {
        public MSPQuestionnaire(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void EncounterData_PropertyChanged_Manager(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "TextData")
            {
                return;
            }

            if (DynamicFormViewModel == null)
            {
                return;
            }

            if (DynamicFormViewModel.MSPManager == null)
            {
                return;
            }

            DynamicFormViewModel.MSPManager.MSPPopulated = (EncounterData.TextData == "1" ? true : false);
            this.RaisePropertyChangedLambda(p => p.Hidden);
            if (EncounterData.TextData == "1")
            {
                AttatchedFormCommand.Execute(null);
            }
        }

        public override bool ValidateAttachedForm()
        {
            bool AllValid = true;
            if (EncounterData == null)
            {
                return AllValid;
            }

            if (EncounterData.Validate())
            {
                if (EncounterData.TextData == "0" && String.IsNullOrEmpty(EncounterData.Text2Data))
                {
                    EncounterData.ValidationErrors.Add(
                        new ValidationResult("Medicare Secondary Questionnaire Reason is Required.",
                            new[] { "Text2Data" }));
                    AllValid = false;
                }
            }

            return AllValid;
        }

        public override void RegisterSection(SectionUI sec)
        {
            if (DynamicFormViewModel != null && DynamicFormViewModel.MSPManager != null)
            {
                DynamicFormViewModel.MSPManager.RegisterSection(sec);
            }

            foreach (var qu in sec.Questions)
            {
                if (qu.Question.DataTemplate.ToLower() == "signature")
                {
                    qu.HiddenOverride = true;
                }

                var qwm = qu as QuestionWithManager;
                if (qwm != null)
                {
                    DynamicFormViewModel.MSPManager.UpdateRegisteredQuestionsOnLoad((QuestionWithManager)qu);
                }
            }
        }

        public override void SetAttachedFormDefinition()
        {
            if (DynamicFormViewModel.MSPManager != null)
            {
                DynamicFormViewModel.MSPManager.AttatchedFormDef = CurrentForm;
            }
        }

        public override void Cleanup()
        {
            try
            {
                EncounterData.PropertyChanged -= EncounterData_PropertyChanged_Manager;
            }
            catch (Exception)
            {
            }

            base.Cleanup();
        }
    }

    public class MSPQuestionnaireFactory
    {
        public static MSPQuestionnaire Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            MSPQuestionnaire qb = new MSPQuestionnaire(__FormSectionQuestionKey)
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
                CurrentPatient = vm.CurrentPatient,
                FormSection = formsection,
                CopyForward = copyforward,
            };

            qb.SetupData(vm, formsection, qgkey, copyforward, q);

            if (qb.EncounterData != null)
            {
                qb.EncounterData.PropertyChanged += qb.EncounterData_PropertyChanged_Manager;
            }

            return qb;
        }
    }
}