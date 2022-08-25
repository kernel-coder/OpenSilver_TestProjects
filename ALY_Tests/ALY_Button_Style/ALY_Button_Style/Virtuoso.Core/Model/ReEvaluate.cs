#region Usings

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class ReEvaluate : QuestionBase
    {
        string __Label;

        public override string Label
        {
            get { return __Label; }
            set { __Label = value; }
        }

        public string ReevaluatePopupLabel => Label;
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

                if (DataTemplateHelper.IsDataTemplateLoaded(PopupDataTemplate))
                {

                }

                return DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }

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

        public ReEvaluate(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            ReEvaluateCommand = new RelayCommand(() =>
            {
                if (ReEvalSection == null)
                {
                    var formsection =
                        DynamicFormCache.GetFormSectionByKey(EncounterData.ReEvaluateFormSectionKey.Value);

                    if (!Loading)
                    {
                        OpenReEval = true;
                    }

                    ReEvalSections = new ObservableCollection<SectionUI>();
                    DynamicFormViewModel.ProcessFormSectionQuestions(formsection, ReEvalSections, true, Loading);
                    ReEvalSection = ReEvalSections.FirstOrDefault();

                    if (!Convert.ToBoolean(EncounterData.BoolData))
                    {
                        // New logic as of DE 35240/35029 - for copyForward and Re-Evaluate copyForward:
                        //   Find MOST RECENT past encounter that has this section (whether it is a Re-Eval section, or a real form section),
                        //   and copy forward all (answered) questions from that MOST RECENT completion of the section.
                        //   For example, whether it is the 'Current Function Status' Re-evaluate section on a PT Visit (with a green check box)
                        //   or whether it is the actual 'Current Function Status' section on a PT Eval.
                        // Notice:
                        // - This code (always did) compare SectionKey NOT FormSectionKey making it discipline agnostic.
                        //   The implication being that if someone defines a 'Current Function Status' on an SN form,
                        //   maybe with a completely different question set - this logic may attempt to copy forward from the SN, not the PT,
                        //   depending on which was the MOST RECENT past encounter that completed the like-named section - feature? bug?. 
                        //   Maybe a bug in this example?, not so much a bug w.r.t. something like the 'Skin/Integument' section.
                        // - This CopyForwardfromEncounter logic does NOT (never has) considered the copyForward setting of the given questions.
                        //   Re-Eval, by nature, assumes ALL fields copyForward - right, wrong or indifferent, it is been this way since the Ken-days.
                        //   I fear that if we took copyForward into account now, all hell would break loose 
                        //   (for example, the model 'Endocrine' section has nothing marked as copyForward)
                        // To REALLY fix this in the future - fix is a strong word... 
                        // To REALLY make this more correct in the future:
                        // (1) Get rid of the 'sparse-array' saving of encounter data, i.e., save 'empty/null' data as well as 'real' data
                        //     This way if a question response goes from 'A' to 'B' to null, the next copy forward copies null, NOT 'B'.
                        //     Also, you get the current re-eval CopyForwardfromEncounter logic for free, since if you OK a section we save the
                        //     WHOLE section - not just the questions that were answered - and we copyForward the WHOLE section. 
                        // (2) When (1) is done, we can remove ALLLL this CopyForwardfromEncounter logic here in Re-evaluate,
                        //     relying solely on the CopyForwardLastAnswered logic that already took place in the factory create (above).
                        //     ( ASSUMING THE Encounter.IsNew check is replaced with Encounter.IsNewOrIsNewReEvaluationSection(formsection) )
                        //     This never worked as intended anyway, with 'sparse-array' we never copyForwarded the WHOLE section.
                        //     During re-eval we do the (sparse) CopyForwardLastAnswered first, then override portions with the 
                        //     (sparse) CopyForwardfromEncounter logic.
                        //     We may get a 'more-correct' CopyForward if we also do step (3):
                        // (3) Add a Question.CopyForward nullable bit column, 
                        //     - If set Y/N (non-null), the system will honor it, and not allow a user to override it in form maintenance.
                        //     - If null, we would allow the copyForwardness of a question based on its use within a given form section - like today.
                        //     This would allow more control over copyForward - allowing delta/model to enforce copyForward when we want to, 
                        //     while still allowing the agency to choose, when we choose not to enforce it.  
                        //     Also, given step (2), we would - by default - honor copyForward during the re-evaluate, regardless of who enforced it.
                        //
                        // One big advantage: one set of rules for ALL copyForward cases.
                        // The big down fall: one set of rules for ALL copyForward cases. 
                        //
                        // If we make these changes, we lose: 'Re-Eval, by nature, assumes ALL fields copyForward' – that may be a big deal.
                        // 1) You could argue the case that the copyForward settings are really only intended to rule over copyFormard
                        //    from an Assessment to a re-Assessment/Resumption - during a true reassessment of the given section/ body system...
                        //    not during a re-eval from visit to visit, when we are updating NOT reassessing.
                        //    That is sort of how it is used today... accidently, since we have the two sets of rules.
                        // 2) We would have lots of forms and fields to review - like the model 'Endocrine' section mentioned above, 
                        //    not to mention all the custom forms.
                        // 3) This dissertation only addresses QuestionBase (generic questions) there are about 30 factories that override
                        //    and would need reviewed

                        Encounter en = null;
                        EncounterData ed = null;
                        foreach (Encounter item in
                                 Admission.Encounter
                                     .Where(e => ((e.Form != null) && (e.EncounterKey != Encounter.EncounterKey) &&
                                                  (!e.IsNew) && (!e.Form.IsTransfer)))
                                     .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                        {
                            int reEvalSectionKey =
                                DynamicFormCache.GetSectionKeyFromFormSectionKey(EncounterData.ReEvaluateFormSectionKey
                                    .Value);
                            // First look for a completed re-eval section in this encounter
                            // Like the 'Current Function Status' Re-evaluate section on a PTVisit (with a green check box)
                            ed = item.EncounterData.FirstOrDefault(p => p.QuestionKey == null 
                                                                        && p.BoolData == true 
                                                                        && DynamicFormCache.GetSectionKeyFromFormSectionKey(p.ReEvaluateFormSectionKey.Value) == reEvalSectionKey);
                            if (ed != null)
                            {
                                en = item;
                                break;
                            }

                            // Then look for ANY data against the given section in this encounter
                            // This takes care of the case where the section is sited on the form aas a real section, not a reEval section -
                            // Like the 'Current Function Status' section on a PTEval
                            ed = item.EncounterData.FirstOrDefault(p => ((p.QuestionKey != null) 
                                                                         && (p.ReEvaluateFormSectionKey == null) 
                                                                         && (p.SectionKey == reEvalSectionKey)));
                            if (ed != null)
                            {
                                en = item;
                                break;
                            }
                            // loop back and try next most recent encounter
                        }

                        if (en != null)
                        {
                            foreach (var q in ReEvalSection.Questions) q.CopyForwardfromEncounter(en);
                        }
                    }

                    foreach (var q in ReEvalSection.Questions) q.PreProcessing();

                    //backup original in case we need to revert back to it
                    foreach (var q in ReEvalSection.Questions) q.BackupEntity(false);

                    Loading = false;
                }
                else
                {
                    OpenReEval = true;
                }

                if (OpenReEval)
                {
                    DynamicFormViewModel.PopupDataContext = this;
                }
            });

            OK_Command = new RelayCommand(async () =>
            {
                if (!Protected)
                {
                    ReEvalSection.Questions.ForEach(p => p.BackupEntity(false));
                    EncounterData.BoolData = true;
                }

                OpenReEval = false;
                DynamicFormViewModel.PopupDataContext = null;
                await DynamicFormViewModel.AutoSave_Command("ReEvaluate");
            });

            Cancel_Command = new RelayCommand(() =>
            {
                ReEvalSection.Questions.ForEach(p => p.BackupEntity(true));
                OpenReEval = false;
                DynamicFormViewModel.PopupDataContext = null;
            });
        }

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

                if (DynamicFormViewModel != null)
                {
                    DynamicFormViewModel.SectionCrossFieldValidate(ref AllValid, ReEvalSection);
                }
            }

            return AllValid;
        }

        public override void Cleanup()
        {
            if (ReEvalSection != null && ReEvalSection.Questions != null)
            {
                foreach (var q in ReEvalSection.Questions) q.Cleanup();
            }

            base.Cleanup();
            if (ReEvalFormModel != null)
            {
                ReEvalFormModel.Cleanup();
            }
        }
    }

    public class ReEvaluateFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterData ed = vm.CurrentEncounter.EncounterData
                .Where(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey)
                .Where(x => x.SectionKey == formsection.Section.SectionKey)
                .Where(x => x.QuestionGroupKey == qgkey)
                .Where(x => x.ReEvaluateFormSectionKey == formsection.FormSectionKey)
                .FirstOrDefault();

            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey,
                    ReEvaluateFormSectionKey = formsection.FormSectionKey
                };
                vm.CurrentEncounter.EncounterData.Add(ed);
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            ReEvaluate r = new ReEvaluate(__FormSectionQuestionKey)
            {
                ReEvalFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = ed,
                OasisManager = vm.CurrentOasisManager,
            };

            return r;
        }
    }
}