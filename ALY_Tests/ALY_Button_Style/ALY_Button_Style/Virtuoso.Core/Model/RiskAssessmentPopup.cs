#region Usings

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class RiskAssessmentPopup : QuestionBase
    {
        public RiskAssessmentPopup(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        #region RiskAssessment

        public string ReevaluatePopupLabel =>
            (CurrentRiskAssessment == null) ? "Risk Assessment Unknown" : CurrentRiskAssessment.Label;

        private string _PopupDataTemplate = "ReEvaluatePopupDataTemplate";

        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                _PopupDataTemplate = value;
                RaisePropertyChanged("PopupDataTemplate");
                RaisePropertyChanged("PopupDataTemplateLoaded");
            }
        }

        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject PopupDataTemplateLoaded
        {
            get
            {
                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }

                return DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }

        //int _CMSFormKey = 0;
        public IDynamicFormService ReEvalFormModel { get; set; }

        ObservableCollection<SectionUI> _ReEvalSections;

        public ObservableCollection<SectionUI> ReEvalSections
        {
            get { return _ReEvalSections; }
            set
            {
                _ReEvalSections = value;
                this.RaisePropertyChangedLambda(p => p.ReEvalSections);
            }
        }

        SectionUI _ReEvalSection;

        public SectionUI ReEvalSection
        {
            get { return _ReEvalSection; }
            set
            {
                _ReEvalSection = value;
                this.RaisePropertyChangedLambda(p => p.ReEvalSection);
            }
        }

        private bool _OpenReEval;

        public bool OpenReEval
        {
            get { return _OpenReEval; }
            set
            {
                if (_OpenReEval != value)
                {
                    _OpenReEval = value;
                    this.RaisePropertyChangedLambda(p => p.OpenReEval);
                }
            }
        }

        public bool Loading { get; set; }

        public RelayCommand ReEvaluateCommand { get; protected set; }

        public RelayCommand OK_Command { get; protected set; }

        public RelayCommand Cancel_Command { get; protected set; }

        private int RiskAssessmentInUse;

        private void ReEvaluateSetup()
        {
            ReEvaluateCommand = new RelayCommand(() =>
            {
                if (ReEvalSection == null)
                {
                    RiskAssessment ra = CurrentRiskAssessment;
                    if (ra == null)
                    {
                        return;
                    }

                    RiskAssessmentInUse = ra.RiskAssessmentKey;
                    FormSection formsection = new FormSection
                    {
                        RiskAssessmentKey = ra.RiskAssessmentKey, RiskAssessment = ra, FormSectionKey = 0,
                        FormKey = DynamicFormViewModel.CurrentForm.FormKey, Section = null, SectionKey = 0, Sequence = 1
                    };

                    if (!Loading)
                    {
                        OpenReEval = true;
                    }

                    ReEvalSections = new ObservableCollection<SectionUI>();
                    DynamicFormViewModel.ProcessFormSectionQuestions(formsection, ReEvalSections, false, false, true);
                    ReEvalSection = ReEvalSections.FirstOrDefault();

                    if (!Convert.ToBoolean(EncounterData.BoolData))
                    {
                        //since we are re-evaluating a section we need to go and find the latest data
                        //look for a previous visit first and default to eval and no re-eval found
                        Encounter en = Admission.Encounter.Where(p => (!p.IsNew))
                            .OrderBy(p => p.EncounterOrTaskStartDateAndTime).FirstOrDefault();

                        foreach (var item in
                                 Admission.Encounter
                                     .Where(e => e.Form != null)
                                     .Where(p =>
                                         !p.IsNew &&
                                         !p.Form.IsPlanOfCare &&
                                         !p.Form.IsTransfer)
                                     .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                        {
                            EncounterRisk er = item.EncounterRisk.Where(p =>
                                    ((p.RiskAssessmentKey == ra.RiskAssessmentKey) && (p.RiskForID == null)))
                                .FirstOrDefault();
                            if (er != null)
                            {
                                en = item;
                                break;
                            }
                        }

                        if (en != null)
                        {
                            foreach (var q in ReEvalSection.Questions) q.CopyForwardfromEncounter(en);
                        }
                    }

                    foreach (var q in ReEvalSection.Questions) q.PreProcessing();
                    Loading = false;
                }
                else
                {
                    OpenReEval = true;
                }

                if (OpenReEval)
                {
                    //backup original in case we need to revert back to it
                    foreach (QuestionUI qui in ReEvalSection.Questions)
                    {
                        Risk quiR = qui as Risk;
                        if (quiR != null)
                        {
                            quiR.SaveRiskAssessmentEncounterRisks2();
                        }
                    }

                    DynamicFormViewModel.PopupDataContext = this;
                }
            });

            OK_Command = new RelayCommand(() =>
            {
                if (!Protected)
                {
                    if (EncounterData.IntData == null)
                    {
                        EncounterData.IntData = RiskAssessmentInUse;
                    }

                    foreach (QuestionUI qui in ReEvalSection.Questions)
                    {
                        Risk quiR = qui as Risk;
                        if (quiR != null)
                        {
                            if (RequireResponseOnPopup)
                            {
                                if (quiR.ValidateRequireResponseOnPopup() == false)
                                {
                                    return;
                                }
                            }

                            quiR.SaveRiskAssessmentEncounterRisks2();
                        }
                    }

                    EncounterData.BoolData = true;
                    FetchRiskAssessmentScore();
                }

                OpenReEval = false;
                DynamicFormViewModel.PopupDataContext = null;
            });

            Cancel_Command = new RelayCommand(() =>
            {
                foreach (QuestionUI qui in ReEvalSection.Questions)
                {
                    Risk quiR = qui as Risk;
                    if (quiR != null)
                    {
                        quiR.RestoreRiskAssessmentEncounterRisks2();
                    }
                }

                OpenReEval = false;
                DynamicFormViewModel.PopupDataContext = null;
            });
        }

        private bool RequireResponseOnPopup
        {
            get
            {
                if ((Question == null) || (string.IsNullOrWhiteSpace(Question.LookupType)))
                {
                    return false;
                }

                if (Question.LookupType == "Nutritional Risk Assessment")
                {
                    return true;
                }

                if (Question.LookupType == "Nutritional Risk Screening")
                {
                    return true;
                }

                if (Question.LookupType == "Abbey Pain Scale")
                {
                    return true;
                }

                return false;
            }
        }

        private void FetchRiskAssessmentScore()
        {
            if (ReEvalSection != null)
            {
                foreach (var q in ReEvalSection.Questions)
                {
                    Risk r = q as Risk;
                    if (r != null)
                    {
                        EncounterData.TextData = CurrentRiskAssessment.Label + " - Done" +
                                                 ((string.IsNullOrWhiteSpace(r.TotalRecord.Comment))
                                                     ? ""
                                                     : ((CurrentRiskAssessment.IncludeComment) ? ", w/comments" : ""));
                        if ((r.CurrentRiskRange != null) && (r.TotalRecord != null))
                        {
                            string score = (r.TotalRecord.Score == null)
                                ? ""
                                : r.TotalRecord.Score.ToString().Trim() + " - ";
                            EncounterData.Text2Data = score + r.CurrentRiskRange.Label;
                            EncounterData.Text3Data = (string.IsNullOrWhiteSpace(r.TotalRecord.Comment))
                                ? null
                                : r.TotalRecord.Comment.Trim();
                        }
                    }
                }
            }
        }

        public RiskAssessment CurrentRiskAssessment
        {
            get
            {
                RiskAssessment ra = null;
                if ((EncounterData != null) && (EncounterData.IntData != null))
                {
                    ra = DynamicFormCache.GetRiskAssessmentByKey((int)EncounterData.IntData);
                    if (ra != null)
                    {
                        return ra;
                    }
                }

                ra = DynamicFormCache.GetRiskAssessmentByLabel(Question.LookupType);
                if (ra != null)
                {
                    return ra;
                }

                MessageBox.Show("Error: RiskAssessmentPopup question: " + Question.LookupType +
                                " Risk Assessment definition not found.  contact AlayaCare support.");
                return null;
            }
        }

        private int RiskAssessmentKey => (CurrentRiskAssessment == null) ? 0 : CurrentRiskAssessment.RiskAssessmentKey;

        public override void PreProcessing()
        {
            if (EncounterData.BoolData.HasValue)
            {
                Loading = true;
                ReEvaluateCommand.Execute(null);
            }

            if ((Question.QuestionOasisMapping != null) && (Encounter.IsNew))
            {
                if (Question.QuestionOasisMapping.Any())
                {
                    if (OasisManager != null)
                    {
                        OasisManager.QuestionOasisMappingChanged(Question, EncounterData);
                    }
                }
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            if (OpenReEval)
            {
                OK_Command.Execute(null);
            }

            bool AllValid = base.Validate(out SubSections);

            if (ReEvalSection != null && AllValid && EncounterData.BoolData.HasValue && EncounterData.BoolData.Value)
            {
                foreach (var q in ReEvalSection.Questions)
                {
                    string ErrorSection = string.Empty;

                    if (!q.Validate(out ErrorSection))
                    {
                        if (string.IsNullOrEmpty(SubSections))
                        {
                            SubSections = ReEvalSection.Label;
                        }

                        AllValid = false;
                    }
                }
            }

            return AllValid;
        }

        public void RiskCleanup()
        {
            if (ReEvalSection != null && ReEvalSection.Questions != null)
            {
                foreach (var q in ReEvalSection.Questions) q.Cleanup();
            }
        }

        #endregion

        public override void Cleanup()
        {
            RiskCleanup();
            base.Cleanup();
            if (EncounterData != null)
            {
                EncounterData.PropertyChanged -= EncounterData_PropertyChanged;
            }
        }

        public void RiskAssessmentPopupSetup()
        {
            EncounterData ed = Encounter.EncounterData.FirstOrDefault(x => x.EncounterKey == Encounter.EncounterKey 
                                                                           && x.SectionKey == Section.SectionKey 
                                                                           && x.QuestionGroupKey == QuestionGroupKey 
                                                                           && x.QuestionKey == Question.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey, TextData = CurrentRiskAssessment.Label + " - Not Done",
                    Text2Data = "None"
                };
                EncounterData = ed;
                CopyForwardLastInstance();
            }
            else
            {
                EncounterData = ed;
            }

            ed.PropertyChanged += EncounterData_PropertyChanged;
            ReEvaluateSetup();
        }

        public override bool CopyForwardLastInstance()
        {
            foreach (Encounter previousE in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                EncounterData previousED = previousE.EncounterData.FirstOrDefault(d => d.QuestionKey == Question.QuestionKey);
                if (previousED != null)
                {
                    EncounterRisk previousER = previousE.EncounterRisk.FirstOrDefault(x => x.RiskForID == null 
                        && x.IsTotal 
                        && x.RiskAssessmentKey == RiskAssessmentKey 
                        && !x.RiskGroupKey.HasValue);

                    if (previousER != null)
                    {
                        CopyProperties(previousED);
                        Encounter.CopyForwardRiskAssessment(previousE, RiskAssessmentKey, null);
                        return true;
                    }
                }
            }

            return false;
        }

        public new void EncounterData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("TextData"))
            {
                QuestionValueChanged.Execute(sender);
            }
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
        }
    }

    public class RiskAssessmentPopupFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            RiskAssessmentPopup rap = new RiskAssessmentPopup(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                OasisManager = vm.CurrentOasisManager
            };
            rap.RiskAssessmentPopupSetup();
            return rap;
        }
    }
}