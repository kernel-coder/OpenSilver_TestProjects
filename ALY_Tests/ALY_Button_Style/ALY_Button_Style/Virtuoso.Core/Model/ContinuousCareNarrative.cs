#region Usings

using System;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Model
{
    public class ContinuousCareNarrative : QuestionUI
    {
        public ContinuousCareNarrative(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public RelayCommand AddEntry_Command { set; get; }
        public EncounterNarrative EncounterNarrative { get; set; }
        public string NarrativeHistoryParagraphText => Encounter.NarrativeHistoryParagraphText;

        public void ContinuousCareNarrativeSetup()
        {
            EncounterNarrative = new EncounterNarrative();
            AddEntry_Command = new RelayCommand(() => { AddEntryCommand(); });
        }

        public void AddEntryCommand()
        {
            if (EncounterNarrative == null)
            {
                EncounterNarrative = new EncounterNarrative();
            }

            if (String.IsNullOrWhiteSpace(EncounterNarrative.NarrativeText))
            {
                NavigateCloseDialog d = new NavigateCloseDialog
                {
                    Width = double.NaN,
                    Height = double.NaN,
                    ErrorMessage = "The existing Entry field is required before you can add a new Entry.",
                    ErrorQuestion = null,
                    Title = "Add Entry Warning",
                    HasCloseButton = false,
                    NoVisible = false,
                    OKLabel = "OK"
                };
                d.Show();
                return;
            }

            ApplyEncounterNarrativeIfNeedBe();
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            ApplyEncounterNarrativeIfNeedBe();
            return true;
        }

        public void ApplyEncounterNarrativeIfNeedBe()
        {
            if (EncounterNarrative == null)
            {
                EncounterNarrative = new EncounterNarrative();
            }

            if (Encounter.EncounterNarrative == null)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(EncounterNarrative.NarrativeText))
            {
                EncounterNarrative.NarrativeText = null;
            }

            if (EncounterNarrative.NarrativeText == null)
            {
                return;
            }

            EncounterNarrative.NarrativeDateTime = DateTime.Now;
            EncounterNarrative.NarrativeBy = WebContext.Current.User.MemberID;
            Encounter.EncounterNarrative.Add(EncounterNarrative);
            EncounterNarrative = new EncounterNarrative();
            RaisePropertyChanged("EncounterNarrative");
            RaisePropertyChanged("NarrativeHistoryParagraphText");
        }

        public bool ShowNewEntry
        {
            get
            {
                if ((DynamicFormViewModel == null) || DynamicFormViewModel.IsReadOnlyEncounter)
                {
                    return false;
                }

                return (!Protected);
            }
        }
    }

    public class ContinuousCareNarrativeFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            ContinuousCareNarrative ccn = new ContinuousCareNarrative(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm
            };
            ccn.ContinuousCareNarrativeSetup();
            return ccn;
        }
    }
}