#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Model
{
    public class OasisGroup : QuestionBase
    {
        public int CurrentOasisSurveyGroupKey { get; set; }
        public IDynamicFormService OasisFormModel { get; set; }
        public DynamicFormViewModel Vm { get; set; }
        private ObservableCollection<SectionUI> _OasisGroupSections;

        public ObservableCollection<SectionUI> OasisGroupSections
        {
            get { return _OasisGroupSections; }
            set
            {
                _OasisGroupSections = value;
                this.RaisePropertyChangedLambda(p => p.OasisGroupSections);
            }
        }

        private SectionUI _OasisGroupSection;

        public SectionUI OasisGroupSection
        {
            get { return _OasisGroupSection; }
            set
            {
                _OasisGroupSection = value;
                this.RaisePropertyChangedLambda(p => p.OasisGroupSection);
            }
        }

        private bool _OasisGroupOpen;

        public bool OasisGroupOpen
        {
            get { return _OasisGroupOpen; }
            set
            {
                if (_OasisGroupOpen != value)
                {
                    _OasisGroupOpen = value;
                    this.RaisePropertyChangedLambda(p => p.OasisGroupOpen);
                }
            }
        }

        public RelayCommand OasisGroupCommand { get; protected set; }

        public RelayCommand OK_Command { get; protected set; }

        public RelayCommand Cancel_Command { get; protected set; }

        public OasisGroup(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            OasisGroupCommand = new RelayCommand(() => { OasisGroupOpen = true; });

            OK_Command = new RelayCommand(() => { OasisGroupOpen = false; });

            Cancel_Command = new RelayCommand(() => { OasisGroupOpen = false; });
        }

        public void ProcessOasisGroup()
        {
            OasisGroupSections = new ObservableCollection<SectionUI>();
            if (OasisGroupSections.Any() == false)
            {
                return;
            }

            OasisGroupSection = OasisGroupSections.FirstOrDefault();
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            if (OasisGroupOpen)
            {
                OK_Command.Execute(null);
            }

            return true;
        }
    }

    public class OasisGroupFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisGroup og = new OasisGroup(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                OasisManager = vm.CurrentOasisManager,
                Vm = vm
            };
            og.ProcessOasisGroup();
            return og;
        }
    }

    public class OasisQuestionBase : QuestionBase
    {
        private static string _wideLabel = new string(' ', 400);
        public string WideLabel => _wideLabel;

        public bool PrintNewRadioFormat => (OasisManager == null) ? false : OasisManager.IsOASISVersionC2orHigher;

        public bool ProtectedOverrideICD
        {
            get
            {
                if (Encounter == null)
                {
                    return true;
                }

                // Everything is protected on an inactive form
                if (Encounter.Inactive)
                {
                    return true;
                }

                if (Encounter.MostRecentEncounterOasis != null)
                {
                    // override default protection - protecting field if survey is inactive or marked not for transmit
                    if (Encounter.MostRecentEncounterOasis.InactiveDate != null)
                    {
                        return true;
                    }

                    if (Encounter.MostRecentEncounterOasis.REC_ID == "X1")
                    {
                        return true;
                    }
                }

                // override default protection, allowing ICDCoder to do the CoderReview
                if ((Encounter.PreviousEncounterStatus == (int)EncounterStatusType.CoderReview) &&
                    RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false))
                {
                    return false;
                }

                if (Encounter.OASISCoordinatorCanEdit)
                {
                    return false;
                }

                if (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.OASISReview)
                {
                    return true;
                }

                // override default protection, allowing anyone with role when the form is completed
                if ((Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed) &&
                    (Encounter.CanEditCompleteOASIS))
                {
                    return false;
                }

                return Protected;
            }
        }

        public IDynamicFormService OasisFormModel { get; set; }
        public int CurrentOasisSurveyGroupKey { get; set; }
        public int CurrentOasisQuestionKey { get; set; }
        private OasisManagerQuestion _OasisManagerQuestion;

        public OasisManagerQuestion OasisManagerQuestion
        {
            get { return _OasisManagerQuestion; }
            set
            {
                _OasisManagerQuestion = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestion);
            }
        }

        public List<OasisManagerAnswer> OasisManagerAnswers
        {
            get { return OasisManagerQuestion.OasisManagerAnswers; }
            set
            {
                if (OasisManagerQuestion != null)
                {
                    OasisManagerQuestion.OasisManagerAnswers = value;
                }

                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswers);
            }
        }

        public bool OtherFollowUpFlag
        {
            get
            {
                if (OasisManager == null)
                {
                    return false;
                }

                if (OasisManager.CurrentEncounterOasis == null)
                {
                    return false;
                }

                if (OasisManager.CurrentEncounterOasis.BypassFlag == null)
                {
                    return true;
                }

                return (bool)OasisManager.CurrentEncounterOasis.BypassFlag ? false : true;
            }
            set
            {
                if (OasisManager == null)
                {
                    return;
                }

                if (OasisManager.CurrentEncounterOasis == null)
                {
                    return;
                }

                OasisManager.SetBypassFlag(!value);
                OasisManager.CurrentEncounterOasis.BypassReason =
                    (value) ? null : "Do not perform Other Follow-up OASIS";
                this.RaisePropertyChangedLambda(p => p.BypassFlag);
            }
        }

        public bool BypassFlag
        {
            get
            {
                if (OasisManager == null)
                {
                    return false;
                }

                if (OasisManager.CurrentEncounterOasis == null)
                {
                    return false;
                }

                if (OasisManager.CurrentEncounterOasis.BypassFlag == null)
                {
                    return false;
                }

                return (bool)OasisManager.CurrentEncounterOasis.BypassFlag;
            }
            set
            {
                if (OasisManager == null)
                {
                    return;
                }

                if (OasisManager.CurrentEncounterOasis == null)
                {
                    return;
                }

                OasisManager.SetBypassFlag(value);
                this.RaisePropertyChangedLambda(p => p.BypassFlag);
            }
        }

        public OasisQuestionBase(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            IsOasisQuestion = true;
        }

        public virtual void OnProcessOasisQuestion()
        {
        }

        public void ProcessOasisQuestion()
        {
            ProtectedOverrideRunTime = SetupProtectedOverrideRunTime();
            if (CurrentOasisQuestionKey <= 0)
            {
                return;
            }

            OasisManagerQuestion =
                new OasisManagerQuestion(OasisCache.GetOasisQuestionByKey(CurrentOasisQuestionKey), OasisManager);
            if (OasisManagerQuestion.OasisQuestion.OasisAnswer != null)
            {
                foreach (OasisAnswer oa in OasisManagerQuestion.OasisQuestion.OasisAnswer.OrderBy(o => o.Sequence))
                    OasisManagerQuestion.OasisManagerAnswers.Add(new OasisManagerAnswer(oa, OasisManager, Protected));
            }

            if (OasisManagerQuestion != null)
            {
                OasisManagerQuestion.OnHiddenChanged += OasisManagerQuestionOnHiddenChanged;
            }

            OnProcessOasisQuestion();
        }

        private void OasisManagerQuestionOnHiddenChanged(object sender, EventArgs e)
        {
            if (OasisManagerQuestion != null)
            {
                Hidden = OasisManagerQuestion.Hidden; // propagate Hidden from OASIS manager to the form question
                if (DynamicFormViewModel != null)
                {
                    DynamicFormViewModel
                        .RefreshFilteredSections(
                            true); // causes infinate loop on Re-Eval section launch if move to QuestionBase // tell form navigation panel about it
                }
            }
        }

        public bool? SetupProtectedOverrideRunTime()
        {
            if (Encounter == null)
            {
                return null;
            }

            // Everything is protected on inactive forms
            if (Encounter.Inactive)
            {
                return true;
            }

            if (Encounter.MostRecentEncounterOasis != null)
            {
                // override default protection - protecting field if survey is inactive or marked not for transmit
                if (Encounter.MostRecentEncounterOasis.InactiveDate != null)
                {
                    return true;
                }

                if (Encounter.MostRecentEncounterOasis.REC_ID == "X1")
                {
                    return true;
                }
            }

            // the clinician who 'owns' the form can edit it if its in one of the clinical edit states
            if (Encounter.EncounterBy == WebContext.Current.User.MemberID)
            {
                if (Encounter.EncounterStatus == (int)EncounterStatusType.Edit)
                {
                    return false;
                }

                if (Encounter.EncounterStatus == (int)EncounterStatusType.CoderReviewEdit)
                {
                    return false;
                }

                if (Encounter.EncounterStatus == (int)EncounterStatusType.OASISReviewEdit)
                {
                    return false;
                }

                if (Encounter.EncounterStatus == (int)EncounterStatusType.OASISReviewEditRR)
                {
                    return false;
                }
            }

            if (Encounter.CMSCoordinatorCanEdit)
            {
                return false;
            }

            // anyone with role when the form is completed
            if ((Encounter.EncounterStatus == (int)EncounterStatusType.Completed) &&
                (Encounter.CanEditCompleteCMS))
            {
                return false;
            }

            // all other cases are protected:
            //   completed forms 
            //    ones in CoderReview or OASISReview - unless overridden by a particular question (ProtectedOverrideRunTime = true)
            return true;
        }
    }

    public class OasisQuestionBaseFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionBase oq = new OasisQuestionBase(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisSectionLabel : OasisQuestionBase
    {
        public OasisSectionLabel(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void Cleanup()
        {
            if (EncounterReview != null)
            {
                EncounterReview.PropertyChanged -= EncounterReview_PropertyChanged;
            }

            base.Cleanup();
        }

        public int NoteIndentLevel => 1;
        private EncounterReview _EncounterReview;

        public EncounterReview EncounterReview
        {
            get { return _EncounterReview; }
            set
            {
                _EncounterReview = value;
                this.RaisePropertyChangedLambda(p => p.EncounterReview);
            }
        }

        private string _SectionLabel
        {
            get
            {
                if (SectionUI == null)
                {
                    return null;
                }

                if (SectionUI.IsOasis && !SectionUI.IsOasisAlert)
                {
                    return oasisLabel + " " + SectionUI.Label;
                }

                return SectionUI.Label;
            }
        }

        private string oasisLabel
        {
            get
            {
                if (Encounter == null)
                {
                    return null;
                }

                return Encounter.SYS_CDDescription;
            }
        }

        public bool IsEncounterReviewSectionNoteEnabled
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                return Encounter.CMSReviewAndCoordinator(OriginalEncounterStatus);
            }
        }

        public bool IsEncounterReviewSectionNoteVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if (Encounter.CMSReviewAndCoordinator(OriginalEncounterStatus))
                {
                    return true;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.OASISReviewEdit) &&
                    (EncounterReview != null) &&
                    (string.IsNullOrWhiteSpace(EncounterReview.ReviewComment) == false))
                {
                    return true;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.OASISReviewEditRR) &&
                    (EncounterReview != null) &&
                    (string.IsNullOrWhiteSpace(EncounterReview.ReviewComment) == false))
                {
                    return true;
                }

                if (OriginalEncounterStatus != (int)EncounterStatusType.Completed)
                {
                    return false;
                }

                if (EncounterReview == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(EncounterReview.ReviewComment))
                {
                    return false;
                }

                if (EncounterReview.ShowNotes)
                {
                    return true;
                }

                return false;
            }
        }

        private SectionUI SectionUI;
        private int OriginalEncounterStatus;

        public void SectionLabelSetup(SectionUI sectionUI)
        {
            if (Encounter != null)
            {
                OriginalEncounterStatus = Encounter.EncounterStatus;
            }

            SectionUI = sectionUI;
            if (SectionUI != null)
            {
                if (Encounter != null)
                {
                    EncounterReview = Encounter.GetEncounterReviewForSection(_SectionLabel);
                }
            }

            if ((EncounterReview == null) && (IsEncounterReviewSectionNoteEnabled))
            {
                EncounterReview = new EncounterReview { ShowNotes = IsEncounterReviewSectionNoteEnabled };
            }

            if (EncounterReview != null)
            {
                EncounterReview.PropertyChanged += EncounterReview_PropertyChanged;
                EncounterReviewRaiseChanged();
            }
        }

        private void EncounterReview_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName.Equals("ShowNotes")) || (e.PropertyName.Equals("ReviewComment")))
            {
                EncounterReviewRaiseChanged();
            }
        }

        private void EncounterReviewRaiseChanged()
        {
            RaisePropertyChanged("IsEncounterReviewSectionNoteVisible");
            if ((EncounterReview == null) || (SectionUI == null))
            {
                return;
            }

            SectionUI.IsSectionNoteVisible =
                ((EncounterReview.ShowNotes) && (string.IsNullOrWhiteSpace(EncounterReview.ReviewComment) == false))
                    ? true
                    : false;
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            CreateEncounterReview();
            return true;
        }

        private void CreateEncounterReview()
        {
            if (Encounter == null)
            {
                return;
            }

            if (!Encounter.CMSReviewAndCoordinator(OriginalEncounterStatus))
            {
                return;
            }

            if (EncounterReview == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(EncounterReview.ReviewComment))
            {
                if (EncounterReview.IsNew == false)
                {
                    Model.RemoveEncounterReview(EncounterReview);
                }
            }
            else
            {
                EncounterReview.ReviewBy = WebContext.Current.User.MemberID;
                EncounterReview.ReviewDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                EncounterReview.ReviewType = (int)EncounterReviewType.SectionReview;
                EncounterReview.ReviewUTCDateTime = DateTime.UtcNow;
                EncounterReview.SectionLabel = _SectionLabel;
                if ((EncounterReview.IsNew) && (Encounter.EncounterReview.Contains(EncounterReview) == false))
                {
                    Encounter.EncounterReview.Add(EncounterReview);
                }
            }
        }
    }

    public class OasisSectionLabelFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisSectionLabel osl = new OasisSectionLabel(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            osl.ProcessOasisQuestion();
            osl.SectionLabelSetup(vm.CurrentSectionUI);
            return osl;
        }
    }

    public class OasisQuestionDefault : OasisQuestionBase
    {
        public OasisQuestionDefault(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }
    }

    public class OasisQuestionDefaultFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionDefault oq = new OasisQuestionDefault(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionCheckBox : OasisQuestionBase
    {
        public override void Cleanup()
        {
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }

        public OasisQuestionCheckBox(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            Messenger.Default.Register<bool>(this,
                string.Format("OasisBypassFlagChanged{0}", OasisManager.OasisManagerGuid.ToString().Trim()),
                b => OasisBypassFlagChanged(b));
        }

        public void OasisBypassFlagChanged(bool BypassFlag)
        {
            this.RaisePropertyChangedLambda(p => p.BypassFlag);
        }
    }

    public class OasisQuestionCheckBoxFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionCheckBox oq = new OasisQuestionCheckBox(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionDate : OasisQuestionBase
    {
        private OasisManagerAnswer _OasisManagerAnswerDate;

        public OasisManagerAnswer OasisManagerAnswerDate
        {
            get { return _OasisManagerAnswerDate; }
            set
            {
                _OasisManagerAnswerDate = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerDate);
            }
        }

        private OasisManagerAnswer _OasisManagerAnswerDateNAUK;

        public OasisManagerAnswer OasisManagerAnswerDateNAUK
        {
            get { return _OasisManagerAnswerDateNAUK; }
            set
            {
                _OasisManagerAnswerDateNAUK = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerDateNAUK);
            }
        }

        public OasisQuestionDate(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManagerAnswers == null) || (OasisManagerAnswers.Any() == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionDate.OnProcessOasisQuestion: {0} is not a valid Date type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            if (((OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.Date) ||
                  OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.DateNAUK)) == false) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.Date) && (OasisManagerAnswers.Count != 1)) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.DateNAUK) && (OasisManagerAnswers.Count != 2)))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionDate.OnProcessOasisQuestion: {0} is not a valid Date type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            OasisManagerAnswerDate = OasisManagerAnswers[0];
            if (OasisManagerAnswers.Count == 2)
            {
                if (OasisManagerAnswers[1].OasisAnswer.IsType(OasisType.CheckBoxExclusive) == false)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisQuestionDate.OnProcessOasisQuestion: {0} is not a valid DateNAUK type.  Contact your system administrator.",
                        OasisManagerQuestion.OasisQuestion.Question));
                    return;
                }

                OasisManagerAnswerDateNAUK = OasisManagerAnswers[1];
            }
        }
    }

    public class OasisQuestionDateFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionDate oq = new OasisQuestionDate(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionRadio : OasisQuestionBase
    {
        #region LookbackCommand

        public RelayCommand<string> LookbackCommand { get; set; }

        public void LookbackCommandOpened(string question)
        {
            if (OasisManager == null)
            {
                return;
            }

            OasisManager.LookbackShowPopup(question);
        }

        private bool CanLookbackCommand(string question)
        {
            return true;
        }

        #endregion

        public OasisQuestionRadio(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            LookbackCommand = new RelayCommand<string>(s => LookbackCommandOpened(s), s => CanLookbackCommand(s));
        }

        public string RadioResponseAnswerLabel
        {
            get
            {
                if (OasisManagerAnswers == null)
                {
                    return null;
                }

                foreach (OasisManagerAnswer oma in OasisManagerAnswers)
                    if (oma.RadioResponse)
                    {
                        return (oma.OasisAnswer == null) ? "?" : oma.OasisAnswer.AnswerLabel;
                    }

                return null;
            }
        }

        public string RadioResponseAnswerTextPrint
        {
            get
            {
                if (OasisManagerAnswers == null)
                {
                    return null;
                }

                foreach (OasisManagerAnswer oma in OasisManagerAnswers)
                    if (oma.RadioResponse)
                    {
                        return (oma.OasisAnswer == null) ? "?" : oma.OasisAnswer.AnswerTextPrint;
                    }

                return null;
            }
        }

        public override void OnProcessOasisQuestion()
        {
            if (OasisManagerQuestion.OasisQuestion.IsType(OasisType.RadioWithDate))
            {
                if ((OasisManagerAnswers.Count != 4) ||
                    (OasisManagerAnswers[3].OasisAnswer.IsType(OasisType.Date) == false))
                {
                    MessageBox.Show(String.Format(
                        "Error OasisQuestionRadio.OnProcessOasisQuestion: {0} is not a valid RadioWithDate type.  Contact your system administrator.",
                        OasisManagerQuestion.OasisQuestion.Question));
                    return;
                }

                OasisManagerAnswers[1].OasisManagerAnswerChildDate = OasisManagerAnswers[3];
                OasisManagerAnswers.RemoveAt(3);
            }
            else if (OasisManagerQuestion.OasisQuestion.IsType(OasisType.DepressionScreening))
            {
                if (OasisManagerAnswers.Count != 14)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisQuestionRadio.OnProcessOasisQuestion: {0} is not a valid DepressionScreening type.  Contact your system administrator.",
                        OasisManagerQuestion.OasisQuestion.Question));
                    return;
                }

                OasisManagerAnswers[1].OasisManagerAnswerPHQs = OasisManagerAnswers
                    .Where(o => o.OasisAnswer.Sequence > 4).OrderBy(o => o.OasisAnswer.Sequence).ToList();
                OasisManagerAnswers = OasisManagerAnswers.Where(o => o.OasisAnswer.Sequence <= 4)
                    .OrderBy(o => o.OasisAnswer.Sequence).ToList();
                OasisManagerAnswers[1].ShowPHQsInstructions =
                    ((OasisManagerQuestion.OasisQuestion.Question.Contains("M1730")) &&
                     (OasisManagerQuestion.OasisQuestion.QuestionText != null) &&
                     (OasisManagerQuestion.OasisQuestion.QuestionText
                         .Contains("validated"))); // cheap way to version M1730 verbage
            }
        }
    }

    public class OasisQuestionRadioFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionRadio oq = new OasisQuestionRadio(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionTrackingSheet : OasisQuestionBase
    {
        public override void Cleanup()
        {
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }

        public void ProcessOasisQuestionTrackingSheet()
        {
            Messenger.Default.Register<bool>(this,
                string.Format("OasisHeaderChanged{0}", OasisManager.OasisManagerGuid.ToString().Trim()),
                b => OasisHeaderChanged(b));
        }

        public string M0069RadioResponseAnswerLabel
        {
            get
            {
                if (OasisManagerAnswerM0069s == null)
                {
                    return null;
                }

                foreach (OasisManagerAnswer oma in OasisManagerAnswerM0069s)
                    if (oma.RadioResponse)
                    {
                        return (oma.OasisAnswer == null) ? "?" : oma.OasisAnswer.AnswerLabel;
                    }

                return null;
            }
        }

        public string M0069RadioResponseAnswerTextPrint
        {
            get
            {
                if (OasisManagerAnswerM0069s == null)
                {
                    return null;
                }

                foreach (OasisManagerAnswer oma in OasisManagerAnswerM0069s)
                    if (oma.RadioResponse)
                    {
                        return (oma.OasisAnswer == null) ? "?" : oma.OasisAnswer.AnswerTextPrint;
                    }

                return null;
            }
        }

        public string M0080RadioResponseAnswerLabel
        {
            get
            {
                if (OasisManagerAnswerM0080s == null)
                {
                    return null;
                }

                foreach (OasisManagerAnswer oma in OasisManagerAnswerM0080s)
                    if (oma.RadioResponse)
                    {
                        return (oma.OasisAnswer == null) ? "?" : oma.OasisAnswer.AnswerLabel;
                    }

                return null;
            }
        }

        public string M0080RadioResponseAnswerTextPrint
        {
            get
            {
                if (OasisManagerAnswerM0080s == null)
                {
                    return null;
                }

                foreach (OasisManagerAnswer oma in OasisManagerAnswerM0080s)
                    if (oma.RadioResponse)
                    {
                        return (oma.OasisAnswer == null) ? "?" : oma.OasisAnswer.AnswerTextPrint;
                    }

                return null;
            }
        }

        public string M0100RadioResponseAnswerLabel
        {
            get
            {
                if (OasisManagerAnswerM0100s == null)
                {
                    return null;
                }

                foreach (OasisManagerAnswer oma in OasisManagerAnswerM0100s)
                    if (oma.RadioResponse)
                    {
                        return (oma.OasisAnswer == null) ? "?" : oma.OasisAnswer.AnswerLabel;
                    }

                return null;
            }
        }

        public string M0100RadioResponseAnswerTextPrint
        {
            get
            {
                if (OasisManagerAnswerM0100s == null)
                {
                    return null;
                }

                foreach (OasisManagerAnswer oma in OasisManagerAnswerM0100s)
                    if (oma.RadioResponse)
                    {
                        return (oma.OasisAnswer == null) ? "?" : oma.OasisAnswer.AnswerTextPrint;
                    }

                return null;
            }
        }

        public void OasisHeaderChanged(bool Flag)
        {
            this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0010s);
            this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0014s);
            this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0016s);
        }

        public bool CanCorrectionNum
        {
            get
            {
                if (OasisManager == null)
                {
                    return false;
                }

                if (Encounter == null)
                {
                    return true;
                }

                // Only allow view of CorrectionNum on completed forms
                return (Encounter.EncounterStatus == (int)EncounterStatusType.Completed) ? true : false;
            }
        }

        public bool ProtectedCorrectionNum
        {
            get
            {
                if (OasisManager == null)
                {
                    return true;
                }

                if (Encounter == null)
                {
                    return true;
                }

                // Everything is protected on an inactive form
                if (Encounter.Inactive)
                {
                    return true;
                }

                if (Encounter.MostRecentEncounterOasis != null)
                {
                    // override default protection - protecting field if survey is inactive or marked not for transmit
                    if (Encounter.MostRecentEncounterOasis.InactiveDate != null)
                    {
                        return true;
                    }

                    if (Encounter.MostRecentEncounterOasis.REC_ID == "X1")
                    {
                        return true;
                    }
                }

                // Only allow edit of CorrectionNum on completed forms for users with role 
                if ((Encounter.EncounterStatus == (int)EncounterStatusType.Completed) &&
                    (Encounter.CanEditCompleteOASIS))
                {
                    return false;
                }

                return true;
            }
        }

        public string CorrectionNum
        {
            get
            {
                if (OasisManager == null)
                {
                    return null;
                }

                string text =
                    OasisManager.GetResponse(
                        OasisCache.GetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "CORRECTION_NUM"));
                return (string.IsNullOrWhiteSpace(text)) ? null : text;
            }
            set
            {
                if (OasisManager == null)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    string bestGuessCorrectionNumOASIS = OasisManager.BestGuessCorrectionNumOASIS;
                    OasisManager.SetResponse(bestGuessCorrectionNumOASIS,
                        OasisCache.GetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "CORRECTION_NUM"));
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "TRANS_TYPE_CD"))
                    {
                        OasisManager.SetResponse(((bestGuessCorrectionNumOASIS == "00") ? "1" : "2"),
                            OasisCache.GetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "TRANS_TYPE_CD"));
                    }

                    Deployment.Current.Dispatcher.BeginInvoke(() => { RaisePropertyChanged("CorrectionNum"); });
                }
                else
                {
                    int intText = 0;
                    try
                    {
                        intText = Int32.Parse(value);
                    }
                    catch
                    {
                    }

                    string cnum = string.Format("{0:00}", intText);
                    OasisManager.SetResponse(cnum,
                        OasisCache.GetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "CORRECTION_NUM"));
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "TRANS_TYPE_CD"))
                    {
                        OasisManager.SetResponse(((cnum == "00") ? "1" : "2"),
                            OasisCache.GetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "TRANS_TYPE_CD"));
                    }
                }
            }
        }

        public string CorrectionNumHint => "( Based on CMS transmissions, the correction number should be " +
                                           OasisManager.BestGuessCorrectionNumOASIS + " )";

        public bool ShowOtherFollowUp
        {
            get
            {
                if (OasisManager == null)
                {
                    return false;
                }

                if (OasisManager.CurrentForm != null)
                {
                    if (OasisManager.CurrentForm.IsOasis)
                    {
                        return false;
                    }
                }

                return (OasisManager.RFA == "05") ? true : false;
            }
        }

        public bool ShowBypass
        {
            get
            {
                if (OasisManager == null)
                {
                    return false;
                }

                if (OasisManager.CurrentForm != null)
                {
                    if (OasisManager.CurrentForm.IsOasis)
                    {
                        return true;
                    }
                }

                return ((OasisManager.RFA == "01") || (OasisManager.RFA == "03") || (OasisManager.RFA == "04") ||
                        (OasisManager.RFA == "08") || (OasisManager.RFA == "09"))
                    ? true
                    : false;
            }
        }

        public bool ProtectedOtherFollowUpFlag
        {
            get
            {
                if (OasisManager == null)
                {
                    return true;
                }

                if (Encounter == null)
                {
                    return true;
                }

                // Everything is protected on an inactive form
                if (Encounter.Inactive)
                {
                    return true;
                }

                if (Encounter.MostRecentEncounterOasis != null)
                {
                    // override default protection - protecting field if survey is inactive or marked not for transmit
                    if (Encounter.MostRecentEncounterOasis.InactiveDate != null)
                    {
                        return true;
                    }

                    if (Encounter.MostRecentEncounterOasis.REC_ID == "X1")
                    {
                        return true;
                    }
                }

                // the clinician who 'owns' the form can bypass it if its in one of the clinical edit states
                if (Encounter.EncounterBy == WebContext.Current.User.MemberID)
                {
                    if (Encounter.EncounterStatus == (int)EncounterStatusType.Edit)
                    {
                        return false;
                    }

                    if (Encounter.EncounterStatus == (int)EncounterStatusType.CoderReviewEdit)
                    {
                        return false;
                    }

                    if (Encounter.EncounterStatus == (int)EncounterStatusType.OASISReviewEdit)
                    {
                        return false;
                    }

                    if (Encounter.EncounterStatus == (int)EncounterStatusType.OASISReviewEditRR)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool ProtectedBypassFlag
        {
            get
            {
                if (OasisManager == null)
                {
                    return true;
                }

                if (Encounter == null)
                {
                    return true;
                }

                // Everything is protected on an inactive form
                if (Encounter.Inactive)
                {
                    return true;
                }

                if (Encounter.MostRecentEncounterOasis != null)
                {
                    // override default protection - protecting field if survey is inactive or marked not for transmit
                    if (Encounter.MostRecentEncounterOasis.InactiveDate != null)
                    {
                        return true;
                    }

                    if (Encounter.MostRecentEncounterOasis.REC_ID == "X1")
                    {
                        return true;
                    }
                }

                // the clinician who 'owns' the form can bypass it if its in one of the clinical edit states
                if (Encounter.EncounterBy == WebContext.Current.User.MemberID)
                {
                    if (Encounter.EncounterStatus == (int)EncounterStatusType.Edit)
                    {
                        return false;
                    }

                    if (Encounter.EncounterStatus == (int)EncounterStatusType.CoderReviewEdit)
                    {
                        return false;
                    }

                    if (Encounter.EncounterStatus == (int)EncounterStatusType.OASISReviewEdit)
                    {
                        return false;
                    }

                    if (Encounter.EncounterStatus == (int)EncounterStatusType.OASISReviewEditRR)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0010;

        public OasisManagerQuestion OasisManagerQuestionM0010
        {
            get { return _OasisManagerQuestionM0010; }
            set
            {
                _OasisManagerQuestionM0010 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0010);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0010s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0010s
        {
            get { return _OasisManagerAnswerM0010s; }
            set
            {
                _OasisManagerAnswerM0010s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0010s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0014;

        public OasisManagerQuestion OasisManagerQuestionM0014
        {
            get { return _OasisManagerQuestionM0014; }
            set
            {
                _OasisManagerQuestionM0014 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0014);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0014s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0014s
        {
            get { return _OasisManagerAnswerM0014s; }
            set
            {
                _OasisManagerAnswerM0014s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0014s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0016;

        public OasisManagerQuestion OasisManagerQuestionM0016
        {
            get { return _OasisManagerQuestionM0016; }
            set
            {
                _OasisManagerQuestionM0016 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0016);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0016s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0016s
        {
            get { return _OasisManagerAnswerM0016s; }
            set
            {
                _OasisManagerAnswerM0016s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0016s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0018;

        public OasisManagerQuestion OasisManagerQuestionM0018
        {
            get { return _OasisManagerQuestionM0018; }
            set
            {
                _OasisManagerQuestionM0018 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0018);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0018s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0018s
        {
            get { return _OasisManagerAnswerM0018s; }
            set
            {
                _OasisManagerAnswerM0018s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0018s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0020;

        public OasisManagerQuestion OasisManagerQuestionM0020
        {
            get { return _OasisManagerQuestionM0020; }
            set
            {
                _OasisManagerQuestionM0020 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0020);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0020s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0020s
        {
            get { return _OasisManagerAnswerM0020s; }
            set
            {
                _OasisManagerAnswerM0020s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0020s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0030;

        public OasisManagerQuestion OasisManagerQuestionM0030
        {
            get { return _OasisManagerQuestionM0030; }
            set
            {
                _OasisManagerQuestionM0030 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0030);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0030s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0030s
        {
            get { return _OasisManagerAnswerM0030s; }
            set
            {
                _OasisManagerAnswerM0030s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0030s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0032;

        public OasisManagerQuestion OasisManagerQuestionM0032
        {
            get { return _OasisManagerQuestionM0032; }
            set
            {
                _OasisManagerQuestionM0032 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0032);
            }
        }

        public bool ProtectedOverrideM0032
        {
            get
            {
                if (OasisManager == null)
                {
                    return true;
                }

                if (OasisManager.CurrentEncounter == null)
                {
                    return true;
                }

                // Everything is protected on an inactive form
                if (OasisManager.CurrentEncounter.Inactive)
                {
                    return true;
                }

                if (Protected)
                {
                    return true;
                }

                // DE 2833 - Allow M0032 edits on all RFA types but 01
                if (OasisManager.RFA == "01")
                {
                    return true;
                }

                if (Encounter.OASISCoordinatorCanEdit)
                {
                    return false;
                }

                if (OasisManager.CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return (Encounter.CanEditCompleteOASIS) ? false : true;
                }

                return false;
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0032s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0032s
        {
            get { return _OasisManagerAnswerM0032s; }
            set
            {
                _OasisManagerAnswerM0032s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0032s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0040;

        public OasisManagerQuestion OasisManagerQuestionM0040
        {
            get { return _OasisManagerQuestionM0040; }
            set
            {
                _OasisManagerQuestionM0040 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0040);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0040s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0040s
        {
            get { return _OasisManagerAnswerM0040s; }
            set
            {
                _OasisManagerAnswerM0040s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0040s);
            }
        }

        public string OasisManagerAnswerM0040FormattedName
        {
            get
            {
                string formattedName = null;
                try
                {
                    string f = OasisManagerAnswerM0040s[0].TextResponse;
                    f = (string.IsNullOrWhiteSpace(f)) ? "" : f.Trim();
                    string m = OasisManagerAnswerM0040s[1].TextResponse;
                    m = (string.IsNullOrWhiteSpace(m)) ? "" : " " + m + ".";
                    string l = OasisManagerAnswerM0040s[2].TextResponse;
                    l = (string.IsNullOrWhiteSpace(l)) ? "" : l.Trim();
                    string s = OasisManagerAnswerM0040s[3].TextResponse;
                    s = (string.IsNullOrWhiteSpace(s)) ? "" : s.Trim();
                    formattedName = f + m + " " + l + " " + s;
                }
                catch
                {
                }

                return formattedName;
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0050;

        public OasisManagerQuestion OasisManagerQuestionM0050
        {
            get { return _OasisManagerQuestionM0050; }
            set
            {
                _OasisManagerQuestionM0050 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0050);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0050s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0050s
        {
            get { return _OasisManagerAnswerM0050s; }
            set
            {
                _OasisManagerAnswerM0050s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0050s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0060;

        public OasisManagerQuestion OasisManagerQuestionM0060
        {
            get { return _OasisManagerQuestionM0060; }
            set
            {
                _OasisManagerQuestionM0060 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0060);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0060s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0060s
        {
            get { return _OasisManagerAnswerM0060s; }
            set
            {
                _OasisManagerAnswerM0060s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0060s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0063;

        public OasisManagerQuestion OasisManagerQuestionM0063
        {
            get { return _OasisManagerQuestionM0063; }
            set
            {
                _OasisManagerQuestionM0063 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0063);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0063s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0063s
        {
            get { return _OasisManagerAnswerM0063s; }
            set
            {
                _OasisManagerAnswerM0063s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0063s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0064;

        public OasisManagerQuestion OasisManagerQuestionM0064
        {
            get { return _OasisManagerQuestionM0064; }
            set
            {
                _OasisManagerQuestionM0064 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0064);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0064s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0064s
        {
            get { return _OasisManagerAnswerM0064s; }
            set
            {
                _OasisManagerAnswerM0064s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0064s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0065;

        public OasisManagerQuestion OasisManagerQuestionM0065
        {
            get { return _OasisManagerQuestionM0065; }
            set
            {
                _OasisManagerQuestionM0065 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0065);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0065s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0065s
        {
            get { return _OasisManagerAnswerM0065s; }
            set
            {
                _OasisManagerAnswerM0065s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0065s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0066;

        public OasisManagerQuestion OasisManagerQuestionM0066
        {
            get { return _OasisManagerQuestionM0066; }
            set
            {
                _OasisManagerQuestionM0066 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0066);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0066s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0066s
        {
            get { return _OasisManagerAnswerM0066s; }
            set
            {
                _OasisManagerAnswerM0066s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0066s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0069;

        public OasisManagerQuestion OasisManagerQuestionM0069
        {
            get { return _OasisManagerQuestionM0069; }
            set
            {
                _OasisManagerQuestionM0069 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0069);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0069s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0069s
        {
            get { return _OasisManagerAnswerM0069s; }
            set
            {
                _OasisManagerAnswerM0069s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0069s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0080;

        public OasisManagerQuestion OasisManagerQuestionM0080
        {
            get { return _OasisManagerQuestionM0080; }
            set
            {
                _OasisManagerQuestionM0080 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0080);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0080s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0080s
        {
            get { return _OasisManagerAnswerM0080s; }
            set
            {
                _OasisManagerAnswerM0080s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0080s);
            }
        }

        public bool ProtectedOverrideM0080
        {
            get
            {
                if (OasisManager == null)
                {
                    return true;
                }

                if ((OasisManager.CurrentEncounter == null) || (OasisManager.CurrentEncounter.SYS_CDIsHospice))
                {
                    return true;
                }

                // Everything is protected on an inactive form
                if (OasisManager.CurrentEncounter.Inactive)
                {
                    return true;
                }

                if (Protected)
                {
                    return true;
                }

                if (Encounter.OASISCoordinatorCanEdit)
                {
                    return false;
                }

                if (OasisManager.CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return (OasisManager.CurrentEncounter.CanEditCompleteOASIS) ? false : true;
                }

                if (OasisManager.CurrentForm == null)
                {
                    return true;
                }

                return (OasisManager.CurrentForm.IsOasis) ? false : true;
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0090;

        public OasisManagerQuestion OasisManagerQuestionM0090
        {
            get { return _OasisManagerQuestionM0090; }
            set
            {
                _OasisManagerQuestionM0090 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0090);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0090s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0090s
        {
            get { return _OasisManagerAnswerM0090s; }
            set
            {
                _OasisManagerAnswerM0090s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0090s);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionM0100;

        public OasisManagerQuestion OasisManagerQuestionM0100
        {
            get { return _OasisManagerQuestionM0100; }
            set
            {
                _OasisManagerQuestionM0100 = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionM0100);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerM0100s = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerM0100s
        {
            get { return _OasisManagerAnswerM0100s; }
            set
            {
                _OasisManagerAnswerM0100s = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerM0100s);
            }
        }

        public OasisQuestionTrackingSheet(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            OasisManagerQuestionM0010 = SetupQuestion("M0010", OasisManagerAnswerM0010s, 1);
            OasisManagerQuestionM0014 = SetupQuestion("M0014", OasisManagerAnswerM0014s, 1);
            OasisManagerQuestionM0016 = SetupQuestion("M0016", OasisManagerAnswerM0016s, 1);
            OasisManagerQuestionM0018 = SetupQuestion("M0018", OasisManagerAnswerM0018s, 2);
            OasisManagerQuestionM0020 = SetupQuestion("M0020", OasisManagerAnswerM0020s, 1);
            OasisManagerQuestionM0030 = SetupQuestion("M0030", OasisManagerAnswerM0030s, 1);
            OasisManagerQuestionM0032 = SetupQuestion("M0032", OasisManagerAnswerM0032s, 2);
            OasisManagerQuestionM0040 = SetupQuestion("M0040", OasisManagerAnswerM0040s, 4);
            OasisManagerQuestionM0050 = SetupQuestion("M0050", OasisManagerAnswerM0050s, 1);
            OasisManagerQuestionM0060 = SetupQuestion("M0060", OasisManagerAnswerM0060s, 1);
            OasisManagerQuestionM0063 = SetupQuestion("M0063", OasisManagerAnswerM0063s, 2);
            OasisManagerQuestionM0064 = SetupQuestion("M0064", OasisManagerAnswerM0064s, 2);
            OasisManagerQuestionM0065 = SetupQuestion("M0065", OasisManagerAnswerM0065s, 2);
            OasisManagerQuestionM0066 = SetupQuestion("M0066", OasisManagerAnswerM0066s, 1);
            OasisManagerQuestionM0069 = SetupQuestion("M0069", OasisManagerAnswerM0069s, 2);
            OasisManagerQuestionM0080 = SetupQuestion("M0080", OasisManagerAnswerM0080s, 4);
            OasisManagerQuestionM0090 = SetupQuestion("M0090", OasisManagerAnswerM0090s, 1);
            OasisManagerQuestionM0100 = SetupQuestion("M0100", OasisManagerAnswerM0100s, 8);
        }

        private OasisManagerQuestion SetupQuestion(string question, List<OasisManagerAnswer> a, int answerCount)
        {
            OasisManagerQuestion q =
                new OasisManagerQuestion(OasisCache.GetOasisQuestionByQuestion(OasisManager.OasisVersionKey, question),
                    OasisManager);
            if (q == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionTrackingSheet.OnProcessOasisQuestion: {0} is not a valid Tracking Sheet question.  Contact your system administrator.",
                    question));
                return null;
            }

            if (q.OasisQuestion.OasisAnswer != null)
            {
                OasisManagerAnswer oma = null;
                foreach (OasisAnswer oa in q.OasisQuestion.OasisAnswer.OrderBy(o => o.Sequence))
                {
                    oma = new OasisManagerAnswer(oa, OasisManager, Protected);
                    a.Add(oma);
                    q.OasisManagerAnswers.Add(oma);
                }
            }

            if (a.Count != answerCount)
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionTrackingSheet.OnProcessOasisQuestion: {0} is not a valid Tracking Sheet question.  Contact your system administrator.",
                    question));
            }

            //OasisManager.OasisManagerQuestions.Add(q);
            return q;
        }
    }

    public class OasisQuestionTrackingSheetFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionTrackingSheet oq = new OasisQuestionTrackingSheet(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager,
            };
            oq.ProcessOasisQuestion();
            oq.ProcessOasisQuestionTrackingSheet();
            return oq;
        }
    }

    public class OasisQuestionICD : OasisQuestionBase
    {
        public bool ProtectedOverrideICDOccurrence
        {
            get
            {
                if (Encounter == null)
                {
                    return true;
                }

                // Everything is protected on an inactive form
                if (Encounter.Inactive)
                {
                    return true;
                }

                if (Encounter.MostRecentEncounterOasis != null)
                {
                    // override default protection - protecting field if survey is inactive or marked not for transmit
                    if (Encounter.MostRecentEncounterOasis.InactiveDate != null)
                    {
                        return true;
                    }

                    if (Encounter.MostRecentEncounterOasis.REC_ID == "X1")
                    {
                        return true;
                    }
                }

                if (Encounter.OASISCoordinatorCanEdit)
                {
                    return false;
                }

                // override default protection, allowing anyone with role when the form is completed
                if ((Encounter.EncounterStatus == (int)EncounterStatusType.Completed) &&
                    (Encounter.CanEditCompleteOASIS))
                {
                    return false;
                }

                // override default protection, allowing ICDCoder to do the CoderReview
                // even ICDCoders doing the CoderReview do not have access to M1010a
                if ((Encounter.EncounterStatus == (int)EncounterStatusType.CoderReview) &&
                    RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false))
                {
                    //DE1999 - Always allow edit - if (OasisManagerQuestion.OasisQuestion.Question == "M1010") return true;
                    return false;
                }

                return Protected;
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerICDs;

        public List<OasisManagerAnswer> OasisManagerAnswerICDs
        {
            get { return _OasisManagerAnswerICDs; }
            set
            {
                _OasisManagerAnswerICDs = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerICDs);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerCheckBoxes;

        public List<OasisManagerAnswer> OasisManagerAnswerCheckBoxes
        {
            get { return _OasisManagerAnswerCheckBoxes; }
            set
            {
                _OasisManagerAnswerCheckBoxes = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerCheckBoxes);
            }
        }

        public string ICDCodeHeader => (OasisManagerQuestion.OasisQuestion.Question == "M1012")
            ? "Procedure Code"
            : "ICD-9-C M Code";

        public string ICDCodeDescriptionHeader => (OasisManagerQuestion.OasisQuestion.Question == "M1010")
            ?
            "Inpatient Facility Diagnosis"
            : (OasisManagerQuestion.OasisQuestion.Question == "M1012")
                ? "Inpatient Procedure"
                : (OasisManagerQuestion.OasisQuestion.Question == "M1016")
                    ? "Changed Medical Regimen Diagnosis"
                    : "ICD-9-C M Description";

        public OasisQuestionICD(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            int icdType = (int)OasisType.ICD;
            OasisManagerAnswerICDs = OasisManagerAnswers.Where(o => o.OasisAnswer.CachedOasisLayout.Type == icdType)
                .OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerICDs != null) && (OasisManagerAnswerICDs.Any() == false))
            {
                OasisManagerAnswerICDs = null;
            }

            OasisManagerAnswerCheckBoxes = OasisManagerAnswers
                .Where(o => o.OasisAnswer.CachedOasisLayout.Type != icdType).OrderBy(o => o.OasisAnswer.Sequence)
                .ToList();
            if ((OasisManagerAnswerCheckBoxes != null) && (OasisManagerAnswerCheckBoxes.Any() == false))
            {
                OasisManagerAnswerCheckBoxes = null;
            }

            if (OasisManagerAnswerICDs == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionICD.OnProcessOasisQuestion: {0} is not a valid ICD type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
            }
        }
    }

    public class OasisQuestionICDFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionICD oq = new OasisQuestionICD(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionICDMedical : OasisQuestionBase
    {
        private List<OasisManagerAnswer> _OasisManagerAnswerICDs;

        public List<OasisManagerAnswer> OasisManagerAnswerICDs
        {
            get { return _OasisManagerAnswerICDs; }
            set
            {
                _OasisManagerAnswerICDs = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerICDs);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerSeverityA;

        public List<OasisManagerAnswer> OasisManagerAnswerSeverityA
        {
            get { return _OasisManagerAnswerSeverityA; }
            set
            {
                _OasisManagerAnswerSeverityA = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerSeverityA);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerSeverityB;

        public List<OasisManagerAnswer> OasisManagerAnswerSeverityB
        {
            get { return _OasisManagerAnswerSeverityB; }
            set
            {
                _OasisManagerAnswerSeverityB = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerSeverityB);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerSeverityC;

        public List<OasisManagerAnswer> OasisManagerAnswerSeverityC
        {
            get { return _OasisManagerAnswerSeverityC; }
            set
            {
                _OasisManagerAnswerSeverityC = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerSeverityC);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerSeverityD;

        public List<OasisManagerAnswer> OasisManagerAnswerSeverityD
        {
            get { return _OasisManagerAnswerSeverityD; }
            set
            {
                _OasisManagerAnswerSeverityD = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerSeverityD);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerSeverityE;

        public List<OasisManagerAnswer> OasisManagerAnswerSeverityE
        {
            get { return _OasisManagerAnswerSeverityE; }
            set
            {
                _OasisManagerAnswerSeverityE = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerSeverityE);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerSeverityF;

        public List<OasisManagerAnswer> OasisManagerAnswerSeverityF
        {
            get { return _OasisManagerAnswerSeverityF; }
            set
            {
                _OasisManagerAnswerSeverityF = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerSeverityF);
            }
        }

        public OasisQuestionICDMedical(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            int icdType = (int)OasisType.ICD;
            OasisManagerAnswerICDs = OasisManagerAnswers.Where(o => o.OasisAnswer.CachedOasisLayout.Type == icdType)
                .OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerICDs != null) && (OasisManagerAnswerICDs.Any() == false))
            {
                OasisManagerAnswerICDs = null;
            }

            OasisManagerAnswerSeverityA = OasisManagerAnswers
                .Where(o => (o.OasisAnswer.CachedOasisLayout.Type != icdType) && (o.OasisAnswer.Sequence >= 19) &&
                            (o.OasisAnswer.Sequence <= 23)).OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerSeverityA != null) && (OasisManagerAnswerSeverityA.Any() == false))
            {
                OasisManagerAnswerSeverityA = null;
            }

            OasisManagerAnswerSeverityB = OasisManagerAnswers
                .Where(o => (o.OasisAnswer.CachedOasisLayout.Type != icdType) && (o.OasisAnswer.Sequence >= 24) &&
                            (o.OasisAnswer.Sequence <= 28)).OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerSeverityB != null) && (OasisManagerAnswerSeverityB.Any() == false))
            {
                OasisManagerAnswerSeverityB = null;
            }

            OasisManagerAnswerSeverityC = OasisManagerAnswers
                .Where(o => (o.OasisAnswer.CachedOasisLayout.Type != icdType) && (o.OasisAnswer.Sequence >= 29) &&
                            (o.OasisAnswer.Sequence <= 33)).OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerSeverityC != null) && (OasisManagerAnswerSeverityC.Any() == false))
            {
                OasisManagerAnswerSeverityB = null;
            }

            OasisManagerAnswerSeverityD = OasisManagerAnswers
                .Where(o => (o.OasisAnswer.CachedOasisLayout.Type != icdType) && (o.OasisAnswer.Sequence >= 34) &&
                            (o.OasisAnswer.Sequence <= 38)).OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerSeverityD != null) && (OasisManagerAnswerSeverityD.Any() == false))
            {
                OasisManagerAnswerSeverityD = null;
            }

            OasisManagerAnswerSeverityE = OasisManagerAnswers
                .Where(o => (o.OasisAnswer.CachedOasisLayout.Type != icdType) && (o.OasisAnswer.Sequence >= 39) &&
                            (o.OasisAnswer.Sequence <= 43)).OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerSeverityE != null) && (OasisManagerAnswerSeverityE.Any() == false))
            {
                OasisManagerAnswerSeverityE = null;
            }

            OasisManagerAnswerSeverityF = OasisManagerAnswers
                .Where(o => (o.OasisAnswer.CachedOasisLayout.Type != icdType) && (o.OasisAnswer.Sequence >= 44) &&
                            (o.OasisAnswer.Sequence <= 48)).OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerSeverityF != null) && (OasisManagerAnswerSeverityF.Any() == false))
            {
                OasisManagerAnswerSeverityF = null;
            }

            if ((OasisManagerAnswerICDs == null) || (OasisManagerAnswerSeverityA == null))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionICDMedical.OnProcessOasisQuestion: {0} is not a valid ICD type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            if ((OasisManagerAnswerICDs.Count != 18) || (OasisManagerAnswerSeverityA.Count != 5) ||
                (OasisManagerAnswerSeverityB.Count != 5) || (OasisManagerAnswerSeverityC.Count != 5) ||
                (OasisManagerAnswerSeverityD.Count != 5) || (OasisManagerAnswerSeverityE.Count != 5) ||
                (OasisManagerAnswerSeverityF.Count != 5))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionICDMedical.OnProcessOasisQuestion: {0} is not a valid ICD type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
            }
        }
    }

    public class OasisQuestionICDMedicalFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionICDMedical oq = new OasisQuestionICDMedical(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionICD10 : OasisQuestionBase
    {
        public bool ProtectedOverrideICDOccurrence
        {
            get
            {
                if (Encounter == null)
                {
                    return true;
                }

                // Everything is protected on an inactive form
                if (Encounter.Inactive)
                {
                    return true;
                }

                if (Encounter.MostRecentEncounterOasis != null)
                {
                    // override default protection - protecting field if survey is inactive or marked not for transmit
                    if (Encounter.MostRecentEncounterOasis.InactiveDate != null)
                    {
                        return true;
                    }

                    if (Encounter.MostRecentEncounterOasis.REC_ID == "X1")
                    {
                        return true;
                    }
                }

                if (Encounter.OASISCoordinatorCanEdit)
                {
                    return false;
                }

                // override default protection, allowing anyone with role when the form is completed
                if ((Encounter.EncounterStatus == (int)EncounterStatusType.Completed) &&
                    (Encounter.CanEditCompleteOASIS))
                {
                    return false;
                }

                // override default protection, allowing ICDCoder to do the CoderReview
                // even ICDCoders doing the CoderReview do not have access to M1011a
                if ((Encounter.EncounterStatus == (int)EncounterStatusType.CoderReview) &&
                    RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false))
                {
                    return false;
                }

                return Protected;
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerICDs;

        public List<OasisManagerAnswer> OasisManagerAnswerICDs
        {
            get { return _OasisManagerAnswerICDs; }
            set
            {
                _OasisManagerAnswerICDs = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerICDs);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerCheckBoxes;

        public List<OasisManagerAnswer> OasisManagerAnswerCheckBoxes
        {
            get { return _OasisManagerAnswerCheckBoxes; }
            set
            {
                _OasisManagerAnswerCheckBoxes = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerCheckBoxes);
            }
        }

        public string ICDCodeHeader => "ICD-10-CM Code";

        public string ICDCodeDescriptionHeader => (OasisManagerQuestion.OasisQuestion.Question == "M1011")
            ?
            "Inpatient Facility Diagnosis"
            : (OasisManagerQuestion.OasisQuestion.Question == "M1017")
                ? "Changed Medical Regimen Diagnosis"
                : "ICD-10-CM Description";

        public OasisQuestionICD10(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            int icdType = (int)OasisType.ICD10;
            OasisManagerAnswerICDs = OasisManagerAnswers.Where(o => o.OasisAnswer.CachedOasisLayout.Type == icdType)
                .OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerICDs != null) && (OasisManagerAnswerICDs.Any() == false))
            {
                OasisManagerAnswerICDs = null;
            }

            OasisManagerAnswerCheckBoxes = OasisManagerAnswers
                .Where(o => o.OasisAnswer.CachedOasisLayout.Type != icdType).OrderBy(o => o.OasisAnswer.Sequence)
                .ToList();
            if ((OasisManagerAnswerCheckBoxes != null) && (OasisManagerAnswerCheckBoxes.Any() == false))
            {
                OasisManagerAnswerCheckBoxes = null;
            }

            if (OasisManagerAnswerICDs == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionICD10.OnProcessOasisQuestion: {0} is not a valid ICD type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
            }
        }
    }

    public class OasisQuestionICD10Factory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionICD10 oq = new OasisQuestionICD10(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionICD10Medical : OasisQuestionBase
    {
        private List<OasisManagerAnswer> _OasisManagerAnswerICDs;

        public List<OasisManagerAnswer> OasisManagerAnswerICDs
        {
            get { return _OasisManagerAnswerICDs; }
            set
            {
                _OasisManagerAnswerICDs = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerICDs);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerSeverityA;

        public List<OasisManagerAnswer> OasisManagerAnswerSeverityA
        {
            get { return _OasisManagerAnswerSeverityA; }
            set
            {
                _OasisManagerAnswerSeverityA = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerSeverityA);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerSeverityB;

        public List<OasisManagerAnswer> OasisManagerAnswerSeverityB
        {
            get { return _OasisManagerAnswerSeverityB; }
            set
            {
                _OasisManagerAnswerSeverityB = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerSeverityB);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerSeverityC;

        public List<OasisManagerAnswer> OasisManagerAnswerSeverityC
        {
            get { return _OasisManagerAnswerSeverityC; }
            set
            {
                _OasisManagerAnswerSeverityC = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerSeverityC);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerSeverityD;

        public List<OasisManagerAnswer> OasisManagerAnswerSeverityD
        {
            get { return _OasisManagerAnswerSeverityD; }
            set
            {
                _OasisManagerAnswerSeverityD = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerSeverityD);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerSeverityE;

        public List<OasisManagerAnswer> OasisManagerAnswerSeverityE
        {
            get { return _OasisManagerAnswerSeverityE; }
            set
            {
                _OasisManagerAnswerSeverityE = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerSeverityE);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerSeverityF;

        public List<OasisManagerAnswer> OasisManagerAnswerSeverityF
        {
            get { return _OasisManagerAnswerSeverityF; }
            set
            {
                _OasisManagerAnswerSeverityF = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerSeverityF);
            }
        }

        public OasisQuestionICD10Medical(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            int icdType = (int)OasisType.ICD10;
            OasisManagerAnswerICDs = OasisManagerAnswers.Where(o => o.OasisAnswer.CachedOasisLayout.Type == icdType)
                .OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerICDs != null) && (OasisManagerAnswerICDs.Any() == false))
            {
                OasisManagerAnswerICDs = null;
            }

            OasisManagerAnswerSeverityA = OasisManagerAnswers
                .Where(o => (o.OasisAnswer.CachedOasisLayout.Type != icdType) && (o.OasisAnswer.Sequence >= 19) &&
                            (o.OasisAnswer.Sequence <= 23)).OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerSeverityA != null) && (OasisManagerAnswerSeverityA.Any() == false))
            {
                OasisManagerAnswerSeverityA = null;
            }

            OasisManagerAnswerSeverityB = OasisManagerAnswers
                .Where(o => (o.OasisAnswer.CachedOasisLayout.Type != icdType) && (o.OasisAnswer.Sequence >= 24) &&
                            (o.OasisAnswer.Sequence <= 28)).OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerSeverityB != null) && (OasisManagerAnswerSeverityB.Any() == false))
            {
                OasisManagerAnswerSeverityB = null;
            }

            OasisManagerAnswerSeverityC = OasisManagerAnswers
                .Where(o => (o.OasisAnswer.CachedOasisLayout.Type != icdType) && (o.OasisAnswer.Sequence >= 29) &&
                            (o.OasisAnswer.Sequence <= 33)).OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerSeverityC != null) && (OasisManagerAnswerSeverityC.Any() == false))
            {
                OasisManagerAnswerSeverityB = null;
            }

            OasisManagerAnswerSeverityD = OasisManagerAnswers
                .Where(o => (o.OasisAnswer.CachedOasisLayout.Type != icdType) && (o.OasisAnswer.Sequence >= 34) &&
                            (o.OasisAnswer.Sequence <= 38)).OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerSeverityD != null) && (OasisManagerAnswerSeverityD.Any() == false))
            {
                OasisManagerAnswerSeverityD = null;
            }

            OasisManagerAnswerSeverityE = OasisManagerAnswers
                .Where(o => (o.OasisAnswer.CachedOasisLayout.Type != icdType) && (o.OasisAnswer.Sequence >= 39) &&
                            (o.OasisAnswer.Sequence <= 43)).OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerSeverityE != null) && (OasisManagerAnswerSeverityE.Any() == false))
            {
                OasisManagerAnswerSeverityE = null;
            }

            OasisManagerAnswerSeverityF = OasisManagerAnswers
                .Where(o => (o.OasisAnswer.CachedOasisLayout.Type != icdType) && (o.OasisAnswer.Sequence >= 44) &&
                            (o.OasisAnswer.Sequence <= 48)).OrderBy(o => o.OasisAnswer.Sequence).ToList();
            if ((OasisManagerAnswerSeverityF != null) && (OasisManagerAnswerSeverityF.Any() == false))
            {
                OasisManagerAnswerSeverityF = null;
            }

            if ((OasisManagerAnswerICDs == null) || (OasisManagerAnswerSeverityA == null))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionICD10Medical.OnProcessOasisQuestion: {0} is not a valid ICD type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            if ((OasisManagerAnswerSeverityA.Count != 5) || (OasisManagerAnswerSeverityB.Count != 5) ||
                (OasisManagerAnswerSeverityC.Count != 5) || (OasisManagerAnswerSeverityD.Count != 5) ||
                (OasisManagerAnswerSeverityE.Count != 5) || (OasisManagerAnswerSeverityF.Count != 5))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionICD10Medical.OnProcessOasisQuestion: {0} is not a valid ICD type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
            }
        }
    }

    public class OasisQuestionICD10MedicalFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionICD10Medical oq = new OasisQuestionICD10Medical(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionText : OasisQuestionBase
    {
        private OasisManagerAnswer _OasisManagerAnswerText;

        public OasisManagerAnswer OasisManagerAnswerText
        {
            get { return _OasisManagerAnswerText; }
            set
            {
                _OasisManagerAnswerText = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerText);
            }
        }

        private OasisManagerAnswer _OasisManagerAnswerTextNAUK;

        public OasisManagerAnswer OasisManagerAnswerTextNAUK
        {
            get { return _OasisManagerAnswerTextNAUK; }
            set
            {
                _OasisManagerAnswerTextNAUK = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerTextNAUK);
            }
        }

        private OasisManagerAnswer _OasisManagerAnswerTextEQUAL;

        public OasisManagerAnswer OasisManagerAnswerTextEQUAL
        {
            get { return _OasisManagerAnswerTextEQUAL; }
            set
            {
                _OasisManagerAnswerTextEQUAL = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerTextEQUAL);
            }
        }

        public OasisQuestionText(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManagerAnswers == null) || (OasisManagerAnswers.Any() == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionText.OnProcessOasisQuestion: {0} is not a valid Text type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            if (((OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.Text) ||
                  OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.TextNAUK)) == false) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.Text) && (OasisManagerAnswers.Count != 1)) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.TextNAUK) && (OasisManagerAnswers.Count > 3)))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionText.OnProcessOasisQuestion: {0} is not a valid Text type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            OasisManagerAnswerText = OasisManagerAnswers[0];
            if (OasisManagerAnswers.Count == 2)
            {
                if (OasisManagerAnswers[1].OasisAnswer.IsType(OasisType.CheckBoxExclusive) == false)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisQuestionText.OnProcessOasisQuestion: {0} is not a valid TextNAUK type.  Contact your system administrator.",
                        OasisManagerQuestion.OasisQuestion.Question));
                    return;
                }

                OasisManagerAnswerTextNAUK = OasisManagerAnswers[1];
            }

            if (OasisManagerAnswers.Count == 3)
            {
                if (OasisManagerAnswers[1].OasisAnswer.IsType(OasisType.CheckBoxExclusive) == false)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisQuestionText.OnProcessOasisQuestion: {0} is not a valid TextNAUK type.  Contact your system administrator.",
                        OasisManagerQuestion.OasisQuestion.Question));
                    return;
                }

                OasisManagerAnswerTextNAUK = OasisManagerAnswers[1];
                if (OasisManagerAnswers[2].OasisAnswer.IsType(OasisType.CheckBoxExclusive) == false)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisQuestionText.OnProcessOasisQuestion: {0} is not a valid TextEQUAL type.  Contact your system administrator.",
                        OasisManagerQuestion.OasisQuestion.Question));
                    return;
                }

                OasisManagerAnswerTextEQUAL = OasisManagerAnswers[2];
            }
        }
    }

    public class OasisQuestionTextFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionText oq = new OasisQuestionText(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionPressureUlcer : OasisQuestionBase
    {
        public bool IsPressureUlcerColumn2 =>
            ((OasisManager.RFA == "04") || (OasisManager.RFA == "05") || (OasisManager.RFA == "09"));

        private List<OasisManagerQuestion> _OasisManagerQuestionWoundDimensions;

        public List<OasisManagerQuestion> OasisManagerQuestionWoundDimensions
        {
            get { return _OasisManagerQuestionWoundDimensions; }
            set
            {
                _OasisManagerQuestionWoundDimensions = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionWoundDimensions);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerWoundDimensions;

        public List<OasisManagerAnswer> OasisManagerAnswerWoundDimensions
        {
            get { return _OasisManagerAnswerWoundDimensions; }
            set
            {
                _OasisManagerAnswerWoundDimensions = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerWoundDimensions);
            }
        }

        public OasisQuestionPressureUlcer(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManagerAnswers == null) || (OasisManagerAnswers.Count != 12) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.PressureUlcer) == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionPressureUlcer.OnProcessOasisQuestion: {0} is not a valid PressureUlcer type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            if ((OasisManager.RFA == "01") || (OasisManager.RFA == "03") || (OasisManager.RFA == "09"))
            {
                OasisManagerQuestionWoundDimensions = new List<OasisManagerQuestion>();
                OasisManagerQuestionWoundDimensions.Add(new OasisManagerQuestion(
                    OasisCache.GetOasisQuestionByQuestion(OasisManager.OasisVersionKey, "M1310"), OasisManager));
                OasisManagerQuestionWoundDimensions.Add(new OasisManagerQuestion(
                    OasisCache.GetOasisQuestionByQuestion(OasisManager.OasisVersionKey, "M1312"), OasisManager));
                OasisManagerQuestionWoundDimensions.Add(new OasisManagerQuestion(
                    OasisCache.GetOasisQuestionByQuestion(OasisManager.OasisVersionKey, "M1314"), OasisManager));
                foreach (OasisManagerQuestion omq in OasisManagerQuestionWoundDimensions)
                    OasisManager.OasisManagerQuestions.Add(omq);

                OasisManagerAnswer oma = null;
                OasisManagerAnswerWoundDimensions = new List<OasisManagerAnswer>();
                oma = new OasisManagerAnswer(
                    OasisCache.GetOasisAnswersByQuestionKey(OasisManagerQuestionWoundDimensions[0].OasisQuestion
                        .OasisQuestionKey).FirstOrDefault(), OasisManager, Protected);
                OasisManagerAnswerWoundDimensions.Add(oma);
                OasisManagerQuestionWoundDimensions[0].OasisManagerAnswers.Add(oma);
                oma = new OasisManagerAnswer(
                    OasisCache.GetOasisAnswersByQuestionKey(OasisManagerQuestionWoundDimensions[1].OasisQuestion
                        .OasisQuestionKey).FirstOrDefault(), OasisManager, Protected);
                OasisManagerAnswerWoundDimensions.Add(oma);
                OasisManagerQuestionWoundDimensions[1].OasisManagerAnswers.Add(oma);
                oma = new OasisManagerAnswer(
                    OasisCache.GetOasisAnswersByQuestionKey(OasisManagerQuestionWoundDimensions[2].OasisQuestion
                        .OasisQuestionKey).FirstOrDefault(), OasisManager, Protected);
                OasisManagerAnswerWoundDimensions.Add(oma);
                OasisManagerQuestionWoundDimensions[2].OasisManagerAnswers.Add(oma);

                if ((OasisManagerQuestionWoundDimensions == null) || (OasisManagerQuestionWoundDimensions.Count != 3) ||
                    (OasisManagerQuestionWoundDimensions[0].OasisQuestion == null) ||
                    (OasisManagerQuestionWoundDimensions[0].OasisQuestion.IsType(OasisType.WoundDimension) == false))
                {
                    MessageBox.Show(String.Format(
                        "Error OasisQuestionPressureUlcer.OnProcessOasisQuestion: {0} is not a valid PressureUlcer/WoundDimension type.  Contact your system administrator.",
                        OasisManagerQuestion.OasisQuestion.Question));
                }
            }
        }
    }

    public class OasisQuestionPressureUlcerFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionPressureUlcer oq = new OasisQuestionPressureUlcer(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionPressureUlcer_C1 : OasisQuestionBase
    {
        public OasisQuestionPressureUlcer_C1(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManagerAnswers == null) || (OasisManagerAnswers.Count != 6) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.PressureUlcer_C1) == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionPressureUlcer_C1.OnProcessOasisQuestion: {0} is not a valid PressureUlcer_C1 type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
            }
        }
    }

    public class OasisQuestionPressureUlcer_C1Factory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionPressureUlcer_C1 oq = new OasisQuestionPressureUlcer_C1(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionPressureUlcer_C2 : OasisQuestionBase
    {
        public OasisQuestionPressureUlcer_C2(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManagerAnswers == null) || (OasisManagerAnswers.Count != 12) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.PressureUlcer_C2) == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionPressureUlcer_C2.OnProcessOasisQuestion: {0} is not a valid PressureUlcer_C2 type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
            }
        }

        public bool ShowPart2 =>
            ((OasisManager.RFA == "04") || (OasisManager.RFA == "05") || (OasisManager.RFA == "09"));
    }

    public class OasisQuestionPressureUlcer_C2Factory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionPressureUlcer_C2 oq = new OasisQuestionPressureUlcer_C2(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionPressureUlcer_C3 : OasisQuestionBase
    {
        public OasisQuestionPressureUlcer_C3(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManagerAnswers == null) || (OasisManagerAnswers.Count != 12) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.PressureUlcer_C3) == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionPressureUlcer_C3.OnProcessOasisQuestion: {0} is not a valid PressureUlcer_C3 type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
            }
        }

        public bool ShowPart2 => (OasisManager.RFA == "09");
    }

    public class OasisQuestionPressureUlcer_C3Factory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionPressureUlcer_C3 oq = new OasisQuestionPressureUlcer_C3(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionPressureUlcerWorse : OasisQuestionBase
    {
        public OasisQuestionPressureUlcerWorse(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManagerAnswers == null) || (OasisManagerAnswers.Count != 4) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.PressureUlcerWorse) == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionPressureUlcerWorse.OnProcessOasisQuestion: {0} is not a valid PressureUlcerWorse type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
            }
        }
    }

    public class OasisQuestionPressureUlcerWorseFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionPressureUlcerWorse oq = new OasisQuestionPressureUlcerWorse(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionPressureUlcerWorse_C2 : OasisQuestionBase
    {
        public OasisQuestionPressureUlcerWorse_C2(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManagerAnswers == null) || (OasisManagerAnswers.Count != 6) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.PressureUlcerWorse_C2) == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionPressureUlcerWorse_C2.OnProcessOasisQuestion: {0} is not a valid PressureUlcerWorse_C2 type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
            }
        }
    }

    public class OasisQuestionPressureUlcerWorse_C2Factory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionPressureUlcerWorse_C2 oq = new OasisQuestionPressureUlcerWorse_C2(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionHeightWeight_C2 : OasisQuestionBase
    {
        public OasisQuestionHeightWeight_C2(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManagerAnswers == null) || (OasisManagerAnswers.Count != 2) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.HeightWeight_C2) == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionHeightWeight_C2.OnProcessOasisQuestion: {0} is not a valid HeightWeight_C2 type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
            }
        }
    }

    public class OasisQuestionHeightWeight_C2Factory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionHeightWeight_C2 oq = new OasisQuestionHeightWeight_C2(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionGG0170C_C2 : OasisQuestionBase
    {
        private List<OasisManagerAnswer> _OasisManagerAnswersGG0170C;

        public List<OasisManagerAnswer> OasisManagerAnswersGG0170C
        {
            get { return _OasisManagerAnswersGG0170C; }
            set
            {
                _OasisManagerAnswersGG0170C = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswersGG0170C);
            }
        }

        private List<OasisAnswer> _ResponseListGG0170C_MOBILITY_SOCROC_PERF;

        public List<OasisAnswer> ResponseListGG0170C_MOBILITY_SOCROC_PERF
        {
            get { return _ResponseListGG0170C_MOBILITY_SOCROC_PERF; }
            set
            {
                _ResponseListGG0170C_MOBILITY_SOCROC_PERF = value;
                this.RaisePropertyChangedLambda(p => p.ResponseListGG0170C_MOBILITY_SOCROC_PERF);
            }
        }

        private OasisManagerAnswer _ResponseGG0170C_MOBILITY_SOCROC_PERF;

        public OasisManagerAnswer ResponseGG0170C_MOBILITY_SOCROC_PERF
        {
            get { return _ResponseGG0170C_MOBILITY_SOCROC_PERF; }
            set
            {
                _ResponseGG0170C_MOBILITY_SOCROC_PERF = value;
                this.RaisePropertyChangedLambda(p => p.ResponseGG0170C_MOBILITY_SOCROC_PERF);
            }
        }

        private List<OasisAnswer> _ResponseListGG0170C_MOBILITY_DSCHG_GOAL;

        public List<OasisAnswer> ResponseListGG0170C_MOBILITY_DSCHG_GOAL
        {
            get { return _ResponseListGG0170C_MOBILITY_DSCHG_GOAL; }
            set
            {
                _ResponseListGG0170C_MOBILITY_DSCHG_GOAL = value;
                this.RaisePropertyChangedLambda(p => p.ResponseListGG0170C_MOBILITY_DSCHG_GOAL);
            }
        }

        private OasisManagerAnswer _ResponseGG0170C_MOBILITY_DSCHG_GOAL;

        public OasisManagerAnswer ResponseGG0170C_MOBILITY_DSCHG_GOAL
        {
            get { return _ResponseGG0170C_MOBILITY_DSCHG_GOAL; }
            set
            {
                _ResponseGG0170C_MOBILITY_DSCHG_GOAL = value;
                this.RaisePropertyChangedLambda(p => p.ResponseGG0170C_MOBILITY_DSCHG_GOAL);
            }
        }

        public OasisQuestionGG0170C_C2(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManager == null) || (OasisManagerAnswers == null) || (OasisManagerAnswers.Any() == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionGG0170C_C2.OnProcessOasisQuestion: {0} is not a valid type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            if ((OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.GG0170C_C2) == false) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.GG0170C_C2) && (OasisManagerAnswers.Count != 17)))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionGG0170C_C2.OnProcessOasisQuestion: {0} is not a valid type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            OasisLayout ol =
                OasisCache.GetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "GG0170C_MOBILITY_SOCROC_PERF");
            if (ol == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionGG0170C_C2.OnProcessOasisQuestion: {0} is not a valid type, missing GG0170C_MOBILITY_SOCROC_PERF OASIS layout.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            OasisManagerAnswersGG0170C = OasisManagerAnswers
                .Where(o => o.OasisAnswer.OasisLayoutKey == ol.OasisLayoutKey).OrderBy(o => o.OasisAnswer.Sequence)
                .ToList();
            if ((OasisManagerAnswersGG0170C == null) || OasisManagerAnswersGG0170C.Count != 10)
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionGG0170C_C2.OnProcessOasisQuestion: {0} is not a valid type, missing GG0170C_MOBILITY_SOCROC_PERF OASIS answers.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            ResponseGG0170C_MOBILITY_SOCROC_PERF = OasisManagerAnswersGG0170C.FirstOrDefault();
            ResponseListGG0170C_MOBILITY_SOCROC_PERF = OasisManagerAnswersGG0170C
                .Where(o => o.OasisAnswer.OasisLayoutKey == ol.OasisLayoutKey).Select(o => o.OasisAnswer).ToList();

            ol = OasisCache.GetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "GG0170C_MOBILITY_DSCHG_GOAL");
            if (ol == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionGG0170C_C2.OnProcessOasisQuestion: {0} is not a valid type, missing GG0170C_MOBILITY_DSCHG_GOAL OASIS layout.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            List<OasisManagerAnswer> goalList = OasisManagerAnswers
                .Where(o => o.OasisAnswer.OasisLayoutKey == ol.OasisLayoutKey).OrderBy(o => o.OasisAnswer.Sequence)
                .ToList();
            if ((goalList == null) || goalList.Count != 7)
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionGG0170C_C2.OnProcessOasisQuestion: {0} is not a valid type, missing GG0170C_MOBILITY_DSCHG_GOAL OASIS answers.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            ResponseGG0170C_MOBILITY_DSCHG_GOAL = goalList.Where(o => o.OasisAnswer.OasisLayoutKey == ol.OasisLayoutKey)
                .FirstOrDefault();
            ResponseListGG0170C_MOBILITY_DSCHG_GOAL = goalList
                .Where(o => o.OasisAnswer.OasisLayoutKey == ol.OasisLayoutKey).Select(o => o.OasisAnswer).ToList();
        }
    }

    public class OasisQuestionGG0170C_C2Factory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionGG0170C_C2 oq = new OasisQuestionGG0170C_C2(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionServiceUtilization_10 : OasisQuestionBase
    {
        public OasisQuestionServiceUtilization_10(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManagerAnswers == null) || (OasisManagerAnswers.Count != 18) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.ServiceUtilization_10) == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionServiceUtilization_10.OnProcessOasisQuestion: {0} is not a valid ServiceUtilization_10 type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
            }
        }
    }

    public class OasisQuestionServiceUtilization_10Factory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionServiceUtilization_10 oq = new OasisQuestionServiceUtilization_10(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisQuestionServiceUtilization_30 : OasisQuestionBase
    {
        public OasisQuestionServiceUtilization_30(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManagerAnswers == null) || (OasisManagerAnswers.Count != 24) ||
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.ServiceUtilization_30) == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionServiceUtilization_30.OnProcessOasisQuestion: {0} is not a valid ServiceUtilization_30 type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
            }
        }
    }

    public class OasisQuestionServiceUtilization_30Factory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionServiceUtilization_30 oq = new OasisQuestionServiceUtilization_30(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }

    public class OasisAlerts : QuestionUI
    {
        public override void Cleanup()
        {
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }

        public OasisAlerts(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private CollectionViewSource _FilteredOasisAlerts = new CollectionViewSource();
        public ICollectionView FilteredOasisAlerts => _FilteredOasisAlerts.View;

        public void ProcessFilteredOasisAlerts()
        {
            if (Encounter == null)
            {
                return;
            }

            if (Encounter.EncounterOasisAlert == null)
            {
                return;
            }

            _FilteredOasisAlerts.Source = Encounter.EncounterOasisAlert;
            _FilteredOasisAlerts.SortDescriptions.Add(new SortDescription("OasisAlertSequence",
                ListSortDirection.Ascending));
            FilteredOasisAlerts.Refresh();
            this.RaisePropertyChangedLambda(p => p.FilteredOasisAlerts);
            Messenger.Default.Register<int>(this,
                string.Format("OasisAlertsChanged{0}", OasisManager.OasisManagerGuid.ToString().Trim()),
                i => OasisAlertsChanged(i));
        }

        private void OasisAlertsChanged(int alertsCount)
        {
            if (FilteredOasisAlerts != null)
            {
                FilteredOasisAlerts.Refresh();
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            if (Encounter == null)
            {
                return true;
            }

            if (Encounter.EncounterOasisAlert == null)
            {
                return true;
            }

            if (OasisManager != null)
            {
                OasisManager.OasisAlertCheckBypass();
            }

            bool AllValid = true;
            foreach (EncounterOasisAlert eoa in Encounter.EncounterOasisAlert)
            {
                eoa.ValidationErrors.Clear();
                if (eoa.Validate() == false)
                {
                    AllValid = false;
                }
            }

            return AllValid;
        }
    }

    public class OasisAlertsFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisAlerts oa = new OasisAlerts(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
            };
            oa.ProcessFilteredOasisAlerts();
            return oa;
        }
    }

    public class OasisQuestionHISTrackingSheet : OasisQuestionBase
    {
        public override void Cleanup()
        {
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }

        public void ProcessOasisQuestionHISTrackingSheet()
        {
            Messenger.Default.Register<bool>(this,
                string.Format("OasisHeaderChanged{0}", OasisManager.OasisManagerGuid.ToString().Trim()),
                b => OasisHeaderChanged(b));
        }

        public void OasisHeaderChanged(bool Flag)
        {
            this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0100ATs);
            this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0100BTs);
        }

        public bool CanCorrectionNum
        {
            get
            {
                if (OasisManager == null)
                {
                    return false;
                }

                if (Encounter == null)
                {
                    return true;
                }

                // Only allow view of CorrectionNum on completed forms
                return (Encounter.EncounterStatus == (int)EncounterStatusType.Completed) ? true : false;
            }
        }

        public bool ProtectedCorrectionNum
        {
            get
            {
                if (OasisManager == null)
                {
                    return true;
                }

                if (Encounter == null)
                {
                    return true;
                }

                // Everything is protected on an inactive form
                if (Encounter.Inactive)
                {
                    return true;
                }

                if (Encounter.MostRecentEncounterOasis != null)
                {
                    // override default protection - protecting field if survey is inactive or marked not for transmit
                    if (Encounter.MostRecentEncounterOasis.InactiveDate != null)
                    {
                        return true;
                    }

                    if (Encounter.MostRecentEncounterOasis.REC_ID == "X1")
                    {
                        return true;
                    }
                }

                // Only allow edit of CorrectionNum on completed forms for users with role 
                if ((Encounter.EncounterStatus == (int)EncounterStatusType.Completed) &&
                    (Encounter.CanEditCompleteHIS))
                {
                    return false;
                }

                return true;
            }
        }

        public string CorrectionNum
        {
            get
            {
                if (OasisManager == null)
                {
                    return null;
                }

                string text =
                    OasisManager.GetResponse(
                        OasisCache.GetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "CRCTN_NUM"));
                return (string.IsNullOrWhiteSpace(text)) ? null : text;
            }
            set
            {
                if (OasisManager == null)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    string bestGuessCorrectionNumHIS = OasisManager.BestGuessCorrectionNumHIS;
                    OasisManager.SetResponse(bestGuessCorrectionNumHIS,
                        OasisCache.GetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "CRCTN_NUM"));
                    OasisManager.SetResponse(((bestGuessCorrectionNumHIS == "00") ? "1" : "2"),
                        OasisCache.GetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "A0050"));
                    Deployment.Current.Dispatcher.BeginInvoke(() => { RaisePropertyChanged("CorrectionNum"); });
                }
                else
                {
                    int intText = 0;
                    try
                    {
                        intText = Int32.Parse(value);
                    }
                    catch
                    {
                    }

                    string text = string.Format("{0:00}", intText);
                    OasisManager.SetResponse(text,
                        OasisCache.GetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "CRCTN_NUM"));
                    OasisManager.SetResponse(((text == "00") ? "1" : "2"),
                        OasisCache.GetOasisLayoutByCMSField(OasisManager.OasisVersionKey, "A0050"));
                }
            }
        }

        public string CorrectionNumHint => "( Based on HIS transmissions, the correction number should be " +
                                           OasisManager.BestGuessCorrectionNumHIS + " )";

        public bool ShowBypass => false;

        public bool ProtectedBypassFlag => true;

        private OasisManagerQuestion _OasisManagerQuestionA0100AT;

        public OasisManagerQuestion OasisManagerQuestionA0100AT
        {
            get { return _OasisManagerQuestionA0100AT; }
            set
            {
                _OasisManagerQuestionA0100AT = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0100AT);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0100ATs = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0100ATs
        {
            get { return _OasisManagerAnswerA0100ATs; }
            set
            {
                _OasisManagerAnswerA0100ATs = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0100ATs);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0100BT;

        public OasisManagerQuestion OasisManagerQuestionA0100BT
        {
            get { return _OasisManagerQuestionA0100BT; }
            set
            {
                _OasisManagerQuestionA0100BT = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0100BT);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0100BTs = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0100BTs
        {
            get { return _OasisManagerAnswerA0100BTs; }
            set
            {
                _OasisManagerAnswerA0100BTs = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0100BTs);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0205T;

        public OasisManagerQuestion OasisManagerQuestionA0205T
        {
            get { return _OasisManagerQuestionA0205T; }
            set
            {
                _OasisManagerQuestionA0205T = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0205T);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0205Ts = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0205Ts
        {
            get { return _OasisManagerAnswerA0205Ts; }
            set
            {
                _OasisManagerAnswerA0205Ts = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0205Ts);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0220T;

        public OasisManagerQuestion OasisManagerQuestionA0220T
        {
            get { return _OasisManagerQuestionA0220T; }
            set
            {
                _OasisManagerQuestionA0220T = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0220T);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0220Ts = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0220Ts
        {
            get { return _OasisManagerAnswerA0220Ts; }
            set
            {
                _OasisManagerAnswerA0220Ts = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0220Ts);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0245T;

        public OasisManagerQuestion OasisManagerQuestionA0245T
        {
            get { return _OasisManagerQuestionA0245T; }
            set
            {
                _OasisManagerQuestionA0245T = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0245T);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0245Ts = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0245Ts
        {
            get { return _OasisManagerAnswerA0245Ts; }
            set
            {
                _OasisManagerAnswerA0245Ts = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0245Ts);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0250T;

        public OasisManagerQuestion OasisManagerQuestionA0250T
        {
            get { return _OasisManagerQuestionA0250T; }
            set
            {
                _OasisManagerQuestionA0250T = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0250T);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0250Ts = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0250Ts
        {
            get { return _OasisManagerAnswerA0250Ts; }
            set
            {
                _OasisManagerAnswerA0250Ts = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0250Ts);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0270T;

        public OasisManagerQuestion OasisManagerQuestionA0270T
        {
            get { return _OasisManagerQuestionA0270T; }
            set
            {
                _OasisManagerQuestionA0270T = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0270T);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0270Ts = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0270Ts
        {
            get { return _OasisManagerAnswerA0270Ts; }
            set
            {
                _OasisManagerAnswerA0270Ts = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0270Ts);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0500T;

        public OasisManagerQuestion OasisManagerQuestionA0500T
        {
            get { return _OasisManagerQuestionA0500T; }
            set
            {
                _OasisManagerQuestionA0500T = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0500T);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0500Ts = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0500Ts
        {
            get { return _OasisManagerAnswerA0500Ts; }
            set
            {
                _OasisManagerAnswerA0500Ts = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0500Ts);
            }
        }

        public string OasisManagerAnswerA0500TFormattedName
        {
            get
            {
                string formattedName = null;
                try
                {
                    string f = OasisManagerAnswerA0500Ts[0].TextResponse;
                    f = (string.IsNullOrWhiteSpace(f)) ? "" : f.Trim();
                    string m = OasisManagerAnswerA0500Ts[1].TextResponse;
                    m = (string.IsNullOrWhiteSpace(m)) ? "" : " " + m + ".";
                    string l = OasisManagerAnswerA0500Ts[2].TextResponse;
                    l = (string.IsNullOrWhiteSpace(l)) ? "" : l.Trim();
                    string s = OasisManagerAnswerA0500Ts[3].TextResponse;
                    s = (string.IsNullOrWhiteSpace(s)) ? "" : s.Trim();
                    formattedName = f + m + " " + l + " " + s;
                }
                catch
                {
                }

                return formattedName;
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0550T;

        public OasisManagerQuestion OasisManagerQuestionA0550T
        {
            get { return _OasisManagerQuestionA0550T; }
            set
            {
                _OasisManagerQuestionA0550T = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0550T);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0550Ts = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0550Ts
        {
            get { return _OasisManagerAnswerA0550Ts; }
            set
            {
                _OasisManagerAnswerA0550Ts = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0550Ts);
            }
        }

        public bool ShowA0550
        {
            get
            {
                if (OasisManager == null)
                {
                    return false;
                }

                if (OasisManager.IsHISVersion2orHigher == false)
                {
                    return false;
                }

                if (OasisManager.RFA == "01")
                {
                    return true;
                }

                return false;
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0600AT;

        public OasisManagerQuestion OasisManagerQuestionA0600AT
        {
            get { return _OasisManagerQuestionA0600AT; }
            set
            {
                _OasisManagerQuestionA0600AT = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0600AT);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0600ATs = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0600ATs
        {
            get { return _OasisManagerAnswerA0600ATs; }
            set
            {
                _OasisManagerAnswerA0600ATs = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0600ATs);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0600BT;

        public OasisManagerQuestion OasisManagerQuestionA0600BT
        {
            get { return _OasisManagerQuestionA0600BT; }
            set
            {
                _OasisManagerQuestionA0600BT = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0600BT);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0600BTs = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0600BTs
        {
            get { return _OasisManagerAnswerA0600BTs; }
            set
            {
                _OasisManagerAnswerA0600BTs = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0600BTs);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0700T;

        public OasisManagerQuestion OasisManagerQuestionA0700T
        {
            get { return _OasisManagerQuestionA0700T; }
            set
            {
                _OasisManagerQuestionA0700T = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0700T);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0700Ts = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0700Ts
        {
            get { return _OasisManagerAnswerA0700Ts; }
            set
            {
                _OasisManagerAnswerA0700Ts = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0700Ts);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0800T;

        public OasisManagerQuestion OasisManagerQuestionA0800T
        {
            get { return _OasisManagerQuestionA0800T; }
            set
            {
                _OasisManagerQuestionA0800T = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0800T);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0800Ts = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0800Ts
        {
            get { return _OasisManagerAnswerA0800Ts; }
            set
            {
                _OasisManagerAnswerA0800Ts = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0800Ts);
            }
        }

        private OasisManagerQuestion _OasisManagerQuestionA0900T;

        public OasisManagerQuestion OasisManagerQuestionA0900T
        {
            get { return _OasisManagerQuestionA0900T; }
            set
            {
                _OasisManagerQuestionA0900T = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerQuestionA0900T);
            }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerA0900Ts = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswerA0900Ts
        {
            get { return _OasisManagerAnswerA0900Ts; }
            set
            {
                _OasisManagerAnswerA0900Ts = value;
                this.RaisePropertyChangedLambda(p => p.OasisManagerAnswerA0900Ts);
            }
        }

        public OasisQuestionHISTrackingSheet(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            OasisManagerQuestionA0100AT = SetupQuestion("A0100A", OasisManagerAnswerA0100ATs, 1);
            OasisManagerQuestionA0100BT = SetupQuestion("A0100B", OasisManagerAnswerA0100BTs, 1);
            OasisManagerQuestionA0205T = SetupQuestion("A0205", OasisManagerAnswerA0205Ts, 10);
            OasisManagerQuestionA0220T = SetupQuestion("A0220", OasisManagerAnswerA0220Ts, 1);
            OasisManagerQuestionA0245T = SetupQuestion("A0245", OasisManagerAnswerA0245Ts, 1);
            OasisManagerQuestionA0250T = SetupQuestion("A0250", OasisManagerAnswerA0250Ts, 2);
            OasisManagerQuestionA0270T = SetupQuestion("A0270", OasisManagerAnswerA0270Ts, 1);
            OasisManagerQuestionA0500T = SetupQuestion("A0500", OasisManagerAnswerA0500Ts, 4);
            if (ShowA0550)
            {
                OasisManagerQuestionA0550T = SetupQuestion("A0550", OasisManagerAnswerA0550Ts, 1);
            }

            OasisManagerQuestionA0600AT = SetupQuestion("A0600A", OasisManagerAnswerA0600ATs, 1);
            OasisManagerQuestionA0600BT = SetupQuestion("A0600B", OasisManagerAnswerA0600BTs, 1);
            OasisManagerQuestionA0700T = SetupQuestion("A0700", OasisManagerAnswerA0700Ts, 1);
            OasisManagerQuestionA0800T = SetupQuestion("A0800", OasisManagerAnswerA0800Ts, 2);
            OasisManagerQuestionA0900T = SetupQuestion("A0900", OasisManagerAnswerA0900Ts, 1);
        }

        private OasisManagerQuestion SetupQuestion(string question, List<OasisManagerAnswer> a, int answerCount)
        {
            OasisManagerQuestion q =
                new OasisManagerQuestion(OasisCache.GetOasisQuestionByQuestion(OasisManager.OasisVersionKey, question),
                    OasisManager);
            if (q == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionHISTrackingSheet.OnProcessOasisQuestion.SetupQuestion: {0} is not a valid HIS Tracking Sheet question.  Contact your system administrator.",
                    question));
                return null;
            }

            if (q.OasisQuestion.OasisAnswer != null)
            {
                OasisManagerAnswer oma = null;
                foreach (OasisAnswer oa in q.OasisQuestion.OasisAnswer.OrderBy(o => o.Sequence))
                {
                    oma = new OasisManagerAnswer(oa, OasisManager, Protected);
                    a.Add(oma);
                    q.OasisManagerAnswers.Add(oma);
                }
            }

            if (a.Count != answerCount)
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionHISTrackingSheet.OnProcessOasisQuestion.SetupQuestion: {0} is not a valid HIS Tracking Sheet question.  Contact your system administrator.",
                    question));
            }

            //OasisManager.OasisManagerQuestions.Add(q);
            return q;
        }
    }

    public class OasisQuestionHISTrackingSheetFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionHISTrackingSheet oq = new OasisQuestionHISTrackingSheet(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager,
            };
            oq.ProcessOasisQuestion();
            oq.ProcessOasisQuestionHISTrackingSheet();
            return oq;
        }
    }

    public class OasisSubQuestion : GalaSoft.MvvmLight.ViewModelBase, INotifyDataErrorInfo
    {
        private OasisManagerAnswer _SubQuestionColumn1AnswerAnchor;

        public OasisManagerAnswer SubQuestionColumn1AnswerAnchor
        {
            get { return _SubQuestionColumn1AnswerAnchor; }
            set
            {
                _SubQuestionColumn1AnswerAnchor = value;
                RaisePropertyChanged("SubQuestionColumn1AnswerAnchor");
            }
        }

        private List<OasisManagerAnswer> _SubQuestionColumn1AnswerList;

        public List<OasisManagerAnswer> SubQuestionColumn1AnswerList
        {
            get { return _SubQuestionColumn1AnswerList; }
            set
            {
                _SubQuestionColumn1AnswerList = value;
                RaisePropertyChanged("_SubQuestionColumn1AnswerList");
            }
        }

        private OasisManagerAnswer _SubQuestionColumn2AnswerAnchor;

        public OasisManagerAnswer SubQuestionColumn2AnswerAnchor
        {
            get { return _SubQuestionColumn2AnswerAnchor; }
            set
            {
                _SubQuestionColumn2AnswerAnchor = value;
                RaisePropertyChanged("SubQuestionColumn2AnswerAnchor");
            }
        }

        private List<OasisManagerAnswer> _SubQuestionColumn2AnswerList;

        public List<OasisManagerAnswer> SubQuestionColumn2AnswerList
        {
            get { return _SubQuestionColumn2AnswerList; }
            set
            {
                _SubQuestionColumn2AnswerList = value;
                RaisePropertyChanged("_ubQuestionColumn2AnswerList");
            }
        }

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors = new Dictionary<string, List<string>>();

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void ClearErrors()
        {
            _currentErrors.Clear();
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
    }

    public class OasisQuestionTopLegend : OasisQuestionBase
    {
        private List<OasisSubQuestion> _OasisSubQuestionList;

        public List<OasisSubQuestion> OasisSubQuestionList
        {
            get { return _OasisSubQuestionList; }
            set
            {
                _OasisSubQuestionList = value;
                RaisePropertyChanged("OasisSubQuestionList");
            }
        }

        public string CodingRulesHeader => "Coding Rules:";

        public bool HasOasisQuestionCodingRule => (OasisQuestionCodingRuleList == null) ? false : true;

        public List<OasisQuestionCodingRule> OasisQuestionCodingRuleList
        {
            get
            {
                if ((OasisManagerQuestion == null) ||
                    (OasisManagerQuestion.OasisQuestion.OasisQuestionCodingRule == null) ||
                    (OasisManagerQuestion.OasisQuestion.OasisQuestionCodingRule.Any() == false))
                {
                    return null;
                }

                List<OasisQuestionCodingRule> list = OasisManagerQuestion.OasisQuestion.OasisQuestionCodingRule
                    .OrderBy(p => p.Sequence).ToList();
                return ((list == null || list.Any() == false)) ? null : list;
            }
        }

        public OasisQuestionTopLegend(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void OnProcessOasisQuestion()
        {
            if ((OasisManager == null) || (OasisManagerQuestion == null) || (OasisManagerAnswers == null) ||
                (OasisManagerAnswers.Any() == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionTopLegend.OnProcessOasisQuestion: {0} is not a valid type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            if ((OasisManagerQuestion.OasisQuestion.CachedOasisLayout.Type != (int)OasisType.TopLegend) &&
                (OasisManagerQuestion.OasisQuestion.CachedOasisLayout.Type != (int)OasisType.LeftLegend))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionTopLegend.OnProcessOasisQuestion: {0} is not a valid type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            if ((OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.TopLegend) == false) &&
                (OasisManagerAnswers[0].OasisAnswer.IsType(OasisType.LeftLegend) == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionTopLegend.OnProcessOasisQuestion: {0} is not a valid type.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            int? maxRow = OasisManagerAnswers.Max(m => m.OasisAnswer.SubQuestionRow);
            int? maxColumn = OasisManagerAnswers.Max(m => m.OasisAnswer.SubQuestionColumn);
            if ((maxRow == null) || (maxColumn == null) || ((int)maxRow == 0) || ((int)maxColumn == 0))
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionTopLegend.OnProcessOasisQuestion: {0} is not a valid type, it contains no SubQuestions.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            if ((int)maxColumn > 2)
            {
                MessageBox.Show(String.Format(
                    "Error OasisQuestionTopLegend.OnProcessOasisQuestion: {0} is not a valid type, it contains more than two SubQuestion columns.  Contact your system administrator.",
                    OasisManagerQuestion.OasisQuestion.Question));
                return;
            }

            List<OasisSubQuestion> osqList = new List<OasisSubQuestion>();
            for (int row = 1; row <= (int)maxRow; row++)
            {
                OasisSubQuestion osq = new OasisSubQuestion();
                for (int column = 1; column <= (int)maxColumn; column++)
                {
                    OasisManagerAnswer omaAnchor = OasisManagerAnswers
                        .Where(p => ((p.OasisAnswer.SubQuestionRow == row) &&
                                     (p.OasisAnswer.SubQuestionColumn == column))).OrderBy(p => p.OasisAnswer.Sequence)
                        .FirstOrDefault();
                    if ((omaAnchor == null) &&
                        (column % 2 ==
                         1)) // only really need an anchor for column 1 - i.e., GG0170Q, GG0170RR1 only have one column
                    {
                        MessageBox.Show(String.Format(
                            "Error OasisQuestionTopLegend.OnProcessOasisQuestion: {0} is not a valid type, corrupted SubQuestions.  Contact your system administrator.",
                            OasisManagerQuestion.OasisQuestion.Question));
                        return;
                    }

                    if (column % 2 == 1) // Column 1
                    {
                        osq.SubQuestionColumn1AnswerAnchor = omaAnchor;
                        osq.SubQuestionColumn1AnswerList = OasisManagerAnswers
                            .Where(p => ((p.OasisAnswer.SubQuestionRow == row) &&
                                         (p.OasisAnswer.SubQuestionColumn == column)))
                            .OrderBy(p => p.OasisAnswer.Sequence).ToList();
                    }
                    else // Column 2
                    {
                        osq.SubQuestionColumn2AnswerAnchor = omaAnchor;
                        osq.SubQuestionColumn2AnswerList = OasisManagerAnswers
                            .Where(p => ((p.OasisAnswer.SubQuestionRow == row) &&
                                         (p.OasisAnswer.SubQuestionColumn == column)))
                            .OrderBy(p => p.OasisAnswer.Sequence).ToList();
                    }
                }

                osqList.Add(osq);
            }

            OasisSubQuestionList = osqList;
        }
    }

    public class OasisQuestionTopLegendFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OasisQuestionTopLegend oq = new OasisQuestionTopLegend(__FormSectionQuestionKey)
            {
                OasisFormModel = m,
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                CurrentOasisSurveyGroupKey = vm.CurrentOasisSurveyGroupKey,
                CurrentOasisQuestionKey = vm.CurrentOasisQuestionKey,
                OasisManager = vm.CurrentOasisManager
            };
            oq.ProcessOasisQuestion();
            return oq;
        }
    }
}