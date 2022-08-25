#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class AttatchedFormFactory
    {
        public static AttatchedForm Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            AttatchedForm qb = new AttatchedForm(__FormSectionQuestionKey)
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

            return qb;
        }
    }

    public class AttatchedForm : QuestionBase
    {
        public virtual bool ValidateAttachedForm()
        {
            return true;
        }

        public virtual void RegisterSection(SectionUI sec)
        {
        }

        public virtual void SetAttachedFormDefinition()
        {
        }

        public virtual void SetupDataComplete()
        {
        }

        public FormSection FormSection { get; set; }
        public bool CopyForward { get; set; }

        public string AttatchedFormPopupLabel => Label + SectionLabel;
        private string _PopupDataTemplate = "AttatchedFormPopupDataTemplate";

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

        int _CMSFormKey;

        public int CMSFormKey
        {
            get { return _CMSFormKey; }
            set
            {
                _CMSFormKey = value;
                RaisePropertyChanged("CMSFormKey");
            }
        }

        int _CurrentFormKey;

        public int CurrentFormKey
        {
            get { return _CurrentFormKey; }
            set { _CurrentFormKey = value; }
        }

        string SectionLabel
        {
            get
            {
                if (AttatchedFormSection == null)
                {
                    return "";
                }

                return "(" + AttatchedFormSection.Label + ")";
            }
        }

        // These is needed for the print template
        public Form CurrentForm { get; set; }

        public Patient CurrentPatient { get; set; }

        //
        public bool CanEdit { get; set; }

        private bool _StartedFromAdmission;

        public bool StartedFromAdmission
        {
            get { return _StartedFromAdmission; }
            set { _StartedFromAdmission = value; }
        }

        public IDynamicFormService AttatchedFormModel { get; set; }
        public IEnumerable<FormSection> DynamicFormSections { get; set; }

        ObservableCollection<SectionUI> _AttatchedFormSections;

        public ObservableCollection<SectionUI> AttatchedFormSections
        {
            get { return _AttatchedFormSections; }
            set
            {
                _AttatchedFormSections = value;
                this.RaisePropertyChangedLambda(p => p.AttatchedFormSections);
            }
        }

        List<LoadedSectionDef> LoadedFormSections = new List<LoadedSectionDef>();
        SectionUI _AttatchedFormSection;

        public SectionUI AttatchedFormSection
        {
            get { return _AttatchedFormSection; }
            set
            {
                _AttatchedFormSection = value;
                this.RaisePropertyChangedLambda(p => p.AttatchedFormSection);
            }
        }

        public void UpdateStatusMessage(bool parm)
        {
            this.RaisePropertyChangedLambda(p => p.StatusMessage);
        }

        private bool _OpenAttatchedForm;

        public bool OpenAttatchedForm
        {
            get { return _OpenAttatchedForm; }
            set
            {
                if (_OpenAttatchedForm != value)
                {
                    _OpenAttatchedForm = value;
                    this.RaisePropertyChangedLambda(p => p.OpenAttatchedForm);
                }
            }
        }

        public int CurrentSectionNumber { get; set; }

        public bool NextVisible
        {
            get
            {
                // If the next form has any visible questions then show the next button
                var nextForm = LoadedFormSections.ElementAtOrDefault(CurrentSectionNumber + 1);
                if (nextForm == null)
                {
                    return false;
                }

                var t = nextForm.SectionList.Any(a => a.Questions.Any(b => b.Hidden == false));
                return t;
            }
        }

        public bool PreviousVisible => CurrentSectionNumber > 0;

        public string StatusMessage
        {
            get
            {
                if (DynamicFormViewModel.MSPManager == null)
                {
                    return "";
                }

                return DynamicFormViewModel.MSPManager.StatusMessage;
            }
        }

        public RelayCommand AttatchedFormCommand { get; protected set; }
        public RelayCommand OK_Command { get; protected set; }
        public RelayCommand Cancel_Command { get; protected set; }
        public RelayCommand Next_Command { get; protected set; }
        public RelayCommand Previous_Command { get; protected set; }
        public RelayCommand SSRS_Print_AttachedFrom { get; protected set; }
        public bool CurrentUserIsSurveyor => RoleAccessHelper.IsSurveyor;

        public void SetupData(DynamicFormViewModel vm, FormSection formsection, int qgkey, bool copyforward, Question q)
        {
            if (vm == null || formsection == null || q == null)
            {
                return;
            }

            CMSFormKey = vm.CMSFormKey;
            CanEdit = !Protected;

            EncounterData ed = vm.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey,
                    EncounterKey = vm.CurrentEncounter.EncounterKey
                };
                EncounterData = ed;

                if (Encounter.IsNew && copyforward)
                {
                    CopyForwardLastInstance();
                }
            }
            else
            {
                EncounterData = ed;
            }

            var fsq = formsection.FormSectionQuestion.Where(fs => fs.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (fsq != null)
            {
                CurrentFormKey = CMSFormKey;
                if (CurrentFormKey == 0)
                {
                    var fKeyRow = fsq.GetQuestionAttributeForName("FormKey");
                    if (fKeyRow != null)
                    {
                        CurrentFormKey = Convert.ToInt32(fKeyRow.AttributeValue);
                    }
                }

                if (CurrentFormKey > 0)
                {
                    // Attatched form for the print templates
                    CurrentForm = DynamicFormCache.GetFormByKey(CurrentFormKey);
                    if (CurrentForm == null)
                    {
                        MessageBox.Show("Attached Form with Key=" + CurrentFormKey +
                                        " not found.  Contact AlayaCare support.");
                    }
                    else
                    {
                        SetAttachedFormDefinition();
                        DynamicFormSections = DynamicFormCache.GetFormByKey(CurrentFormKey).FormSection
                            .OrderBy(fs => fs.Sequence).ToList();
                        if (!Encounter.EncounterAttachedForm.Any(f => f.FormKey == CurrentFormKey))
                        {
                            EncounterAttachedForm af = new EncounterAttachedForm();
                            af.FormKey = CurrentFormKey;
                            af.QuestionKey = q.QuestionKey;
                            af.FormSectionKey = formsection.FormSectionKey;
                            Encounter.EncounterAttachedForm.Add(af);
                        }
                    }
                }
            }

            if (!ed.IsNew)
            {
                if (DynamicFormSections != null)
                {
                    foreach (var sect in DynamicFormSections) LoadNewSection(sect);
                    CurrentSectionNumber = 0;
                    NavigateToSection();
                }
            }

            SetupDataComplete();

            Messenger.Default.Register<int>(this, "ForceAdvanceToFormSection",
                item => { AdvanceToFormSection(item); });

            Messenger.Default.Register<bool>(this, "UpdateStatusMessage",
                item => { UpdateStatusMessage(item); });
        }

        public AttatchedForm(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            SetupCommands();
        }

        private void SetupCommands()
        {
            AttatchedFormCommand = new RelayCommand(() =>
            {
                if (AttatchedFormSection == null)
                {
                    CurrentSectionNumber = 0;
                    if (DynamicFormSections == null)
                    {
                        return;
                    }

                    var firstSection = DynamicFormSections.FirstOrDefault();

                    if (firstSection == null)
                    {
                        return;
                    }

                    foreach (var sect in DynamicFormSections) LoadNewSection(sect);

                    CurrentSectionNumber = 0;
                    NavigateToSection();
                }

                DynamicFormViewModel.PopupDataContext = this;

                RefreshAttachedFormSignature();
            });

            OK_Command = new RelayCommand(() =>
            {
                if (!Protected)
                {
                    AttatchedFormSection.Questions.ForEach(p => p.BackupEntity(false));
                    EncounterData.BoolData = true;

                    // necessary to mark ED as an AttachedForm, used to display AttachedForms in AdmissionDocumentation
                    EncounterData.TextData = "1";
                }

                DynamicFormViewModel.PopupDataContext = null;
                if (StartedFromAdmission)
                {
                    Messenger.Default.Send(Admission.AdmissionKey, "DynamicFormPopupClosed");
                }
            });

            Cancel_Command = new RelayCommand(() =>
            {
                foreach (var sec in LoadedFormSections)
                    sec.SectionList.ForEach(p => p.Questions.ForEach(q => q.BackupEntity(true)));

                OpenAttatchedForm = false;
                DynamicFormViewModel.PopupDataContext = null;
                if (StartedFromAdmission)
                {
                    Messenger.Default.Send(Admission.AdmissionKey, "DynamicFormPopupClosed");
                }
            });

            Next_Command = new RelayCommand(() =>
            {
                if (CurrentSectionNumber + 1 != DynamicFormSections.Count())
                {
                    CurrentSectionNumber++;
                    NavigateToSection();
                }
            });

            Previous_Command = new RelayCommand(() =>
            {
                if (CurrentSectionNumber > 0)
                {
                    CurrentSectionNumber--;
                    NavigateToSection();
                }
            });
            SSRS_Print_AttachedFrom = new RelayCommand(() =>
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.FormModel == null))
                {
                    return;
                }

                if (CMSFormKey > 0)
                {
                    DynamicFormViewModel.PrintSSRSDynamicForm(CMSFormKey, Admission.PatientKey, Encounter.EncounterKey,
                        Admission.AdmissionKey, false);
                }
                else if (CurrentFormKey > 0)
                {
                    DynamicFormViewModel.PrintSSRSDynamicForm(CurrentFormKey, Admission.PatientKey,
                        Encounter.EncounterKey, Admission.AdmissionKey, false);
                }
            });
        }

        private void RefreshAttachedFormSignature()
        {
            DynamicFormViewModel.SlaveValue++; // force an update to the Signature on the AttachedForm
        }

        public void NavigateToSection()
        {
            var nextSection = DynamicFormSections.ElementAtOrDefault(CurrentSectionNumber);
            if (nextSection != null && !LoadedFormSections.Any(ls => ls.FormSectionKey == nextSection.FormSectionKey))
            {
                LoadNewSection(nextSection);
            }
            else if (nextSection != null)
            {
                AttatchedFormSections = LoadedFormSections.Where(ls => ls.FormSectionKey == nextSection.FormSectionKey)
                    .FirstOrDefault().SectionList;
                if (AttatchedFormSections != null)
                {
                    AttatchedFormSection = AttatchedFormSections.FirstOrDefault();
                }
            }

            this.RaisePropertyChangedLambda(p => p.AttatchedFormPopupLabel);
            this.RaisePropertyChangedLambda(p => p.PreviousVisible);
            this.RaisePropertyChangedLambda(p => p.NextVisible);

            RefreshAttachedFormSignature();
        }

        public void AdvanceToFormSection(int sectionParm)
        {
            if (sectionParm > 1 && sectionParm != CurrentSectionNumber)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    CurrentSectionNumber = sectionParm - 1;
                    NavigateToSection();
                });
            }
        }

        private void EncounterSignatureChanged(Encounter e)
        {
        }

        public void LoadNewSection(FormSection formsection)
        {
            AttatchedFormSections = new ObservableCollection<SectionUI>();
            // load into the print collection
            DynamicFormViewModel.ProcessFormSectionQuestions(formsection, AttatchedFormSections, true, false, false,
                null, false);
            if (_AttatchedFormSections == null)
            {
                return;
            }

            foreach (var sec in AttatchedFormSections)
            {
                RegisterSection(sec);

                foreach (var qu in sec.Questions)
                    if (qu.Question.DataTemplate.ToLower() == "signature")
                    {
                        var temp = qu as Signature;

                        qu.Encounter = DynamicFormViewModel.ParentSignatureQuestion.Encounter;

                        if (temp != null)
                        {
                            temp.IsReadOnly = true;
                        }
                    }
            }

            LoadedSectionDef ls = new LoadedSectionDef();
            ls.FormSectionKey = formsection.FormSectionKey;
            ls.SectionList = AttatchedFormSections;
            LoadedFormSections.Add(ls);

            AttatchedFormSection = AttatchedFormSections.FirstOrDefault();
            //backup original in case we need to revert back to it
            if (AttatchedFormSection != null && AttatchedFormSection.Questions != null)
            {
                AttatchedFormSection.Questions.ForEach(p => p.BackupEntity(false));
            }
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;

            if (LoadedFormSections != null)
            {
                foreach (var lfs in LoadedFormSections)
                {
                    foreach (var section in lfs.SectionList)
                    {
                        foreach (var q in section.Questions) AllValid = q.Validate(out SubSections) && AllValid;
                    }
                }
            }

            return AllValid && ValidateAttachedForm() && base.Validate(out SubSections);
        }

        public override void Cleanup()
        {
            Messenger.Default.Unregister<int>(this, "ForceAdvanceToFormSection");
            Messenger.Default.Unregister<bool>(this, "UpdateStatusMessage");
            Messenger.Default.Unregister<bool>(this);
            base.Cleanup();
        }
    }
}