#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Occasional.Model;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.View;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Model
{
    public interface IValidateVitalsReadingDateTime
    {
        DateTime? ReadingDateTime { get; set; }
        int Version { get; set; }
        ICollection<ValidationResult> ValidationErrors { get; }
    }

    public class SectionUI : GalaSoft.MvvmLight.ViewModelBase
    {
        public const string NPWTMessage =
            "This section is used in the event that a covered Home Health skill was performed during the visit and was documented as the Service Type but the Application/Reapplication of Disposable NPWT was also performed during the visit.  Check the box to document the Application/Reapplication of Disposable NPWT, select the appropriate NPWT skill performed from the drop down, and enter the <Underline>total</Underline> time for the NPWT procedure (including the time spent on wound assessment, dressing application and wound care teaching)";

        // NOTE: only using as an 'instance key' for updating the SectionUI.Label when the ServiceTypeKey is changed
        // Should only have one instance of DynamicFormViewModel in memory for any given 'TaskKey'
        private int TaskKey;

        public SectionUI(int taskKey)
        {
            TaskKey = taskKey;
            Messenger.Default.Register<ServiceTypeKeyChangedEvent>(this,
                Constants.DomainEvents.ServiceTypeKeyChanged,
                evt =>
                {
                    if (evt == null)
                    {
                        return;
                    }

                    if (TaskKey == 0)
                    {
                        return;
                    }

                    if (evt.TaskKey == 0)
                    {
                        return;
                    }

                    // Need to know if this service type key change is for "MY INSTANCE" of DynamicFormViewModel
                    if (TaskKey == evt.TaskKey)
                    {
                        var st = ServiceTypeCache.GetServiceTypeFromKey(evt.NewServiceTypeKey);
                        LabelLine2 = (st == null ? "Unknown" : st.Description);
                        RaisePropertyChanged("LabelLine2");
                    }
                });
        }

        private bool _IsOrderEntry;

        public bool IsOrderEntry
        {
            get { return _IsOrderEntry; }
            set
            {
                _IsOrderEntry = value;
                RaisePropertyChanged("IsOrderEntry");
            }
        }

        private bool _IsSupervision;

        public bool IsSupervision
        {
            get { return _IsSupervision; }
            set
            {
                _IsSupervision = value;
                RaisePropertyChanged("IsSupervision");
            }
        }

        public string Label { get; set; }
        public string LabelLine2 { get; set; }
        private string _oasisLabel;

        public string OasisLabel
        {
            get { return _oasisLabel; }
            set
            {
                _oasisLabel = value;
                RaisePropertyChanged("OasisLabel");
            }
        }

        private bool _isOasis;

        public bool IsOasis
        {
            get { return _isOasis; }
            set
            {
                _isOasis = value;
                RaisePropertyChanged("IsOasis");
            }
        }

        private bool _isOasisAlert;

        public bool IsOasisAlert
        {
            get { return _isOasisAlert; }
            set
            {
                _isOasisAlert = value;
                RaisePropertyChanged("IsOasisAlert");
            }
        }

        private bool _isSectionNoteVisible;

        public bool IsSectionNoteVisible
        {
            get { return _isSectionNoteVisible; }
            set
            {
                _isSectionNoteVisible = value;
                RaisePropertyChanged("IsSectionNoteVisible");
            }
        }

        private bool _isICDNoteVisible;

        public bool IsICDNoteVisible
        {
            get { return _isICDNoteVisible; }
            set
            {
                _isICDNoteVisible = value;
                RaisePropertyChanged("IsICDNoteVisible");
            }
        }

        public bool PatientDemographics { get; set; }
        private ScrollBarVisibility _OuterScrollVisibility = ScrollBarVisibility.Auto;

        public ScrollBarVisibility OuterScrollVisibility
        {
            get { return _OuterScrollVisibility; }
            set { _OuterScrollVisibility = value; }
        }

        public ObservableCollection<QuestionUI> Questions { get; set; }
        private bool _errors;

        public bool Errors
        {
            get { return _errors; }
            set
            {
                _errors = value;
                base.RaisePropertyChanged("Errors");
            }
        }

        private bool _AlwaysHideSection;

        public bool AlwaysHideSection
        {
            get { return _AlwaysHideSection; }
            set
            {
                _AlwaysHideSection = value;
                RaisePropertyChanged("IsSectionVisible");
            }
        }

        public bool IsSectionVisible
        {
            get
            {
                var _ret = Questions.Any(q => q.Hidden == false && q.IsSectionLabelQuestion == false);
                bool isVisible = Questions == null ? false : _ret;
                return isVisible && !AlwaysHideSection;
            }
        }

        int __cleanupCount;

        public override void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            if (Questions != null)
            {
                Questions.ForEach(q => q.Cleanup());
                Questions.Clear();
                Questions = null;
            }

            Messenger.Default.Unregister(this);
            base.Cleanup();
        }
    }

    [DataContract]
    public class QuestionUI : GenericBase, INotifyDataErrorInfo
    {
        protected int? __FormSectionQuestionKey { get; private set; } // Needed for serialization of question data
        public int? QuestionID => __FormSectionQuestionKey;

        public QuestionUI(int? formSectionQuestionKey)
        {
            __FormSectionQuestionKey = formSectionQuestionKey;
        }

        public bool IsNewEncounter
        {
            get
            {
                if (DynamicFormViewModel == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.AddingNewEncounter)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsNewEncounterOrSection
        {
            get
            {
                if (DynamicFormViewModel == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.AddingNewEncounter)
                {
                    return true;
                }

                if ((Encounter == null) || (Encounter.EncounterData == null))
                {
                    return false;
                }

                bool isNewSection = Encounter.EncounterData.Where(e =>
                    ((e.EncounterKey == Encounter.EncounterKey) && (e.SectionKey == Section.SectionKey) &&
                     (e.QuestionGroupKey == -1) && (e.QuestionKey == null) &&
                     ((e.BoolData == null) || (e.BoolData == false)))).Any();
                return isNewSection;
            }
        }

        public Object AdmissionOrEncounterAdmission
        {
            get
            {
                if (Encounter == null)
                {
                    return Admission;
                }

                if (Encounter.EncounterAdmission == null)
                {
                    return Admission;
                }

                EncounterAdmission currentEncounterAdmission = Encounter.EncounterAdmission.FirstOrDefault();
                if (currentEncounterAdmission == null)
                {
                    return Admission;
                }

                currentEncounterAdmission.Admission = Admission;
                if (Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return currentEncounterAdmission;
                }

                return Admission;
            }
        }

        public Object AdmissionDisciplineOrEncounterAdmission
        {
            get
            {
                if (Encounter == null)
                {
                    return AdmissionDiscipline;
                }

                if (Encounter.EncounterAdmission == null)
                {
                    return AdmissionDiscipline;
                }

                EncounterAdmission currentEncounterAdmission = Encounter.EncounterAdmission.FirstOrDefault();
                if (currentEncounterAdmission == null)
                {
                    return AdmissionDiscipline;
                }

                if (Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return currentEncounterAdmission;
                }

                return AdmissionDiscipline;
            }
        }

        public DynamicFormViewModel DynamicFormViewModel { get; set; }
        public Section Section { get; set; }
        public int ParentGroupKey { get; set; }
        public int QuestionGroupKey { get; set; }
        private Question _question;
        private bool _pageBreakBefore;

        public bool PageBreakBefore
        {
            get { return _pageBreakBefore; }
            set { _pageBreakBefore = value; }
        }

        private bool _pageBreakAfter;

        public bool PageBreakAfter
        {
            get { return _pageBreakAfter; }
            set { _pageBreakAfter = value; }
        }

        private bool _isOasisQuestion;

        public bool IsOasisQuestion
        {
            get { return _isOasisQuestion; }
            set { _isOasisQuestion = value; }
        }

        public Question Question
        {
            get { return _question; }
            set
            {
                _question = value;

                if (_question != null && _question.DataTemplate != null && _question.DataTemplate.Equals("ServiceType"))
                {
                    Messenger.Default.Unregister<ServiceTypeKeyChangedEvent>(this,
                        Constants.DomainEvents.ServiceTypeKeyChanged);

                    //If the end user changes the service type key, then sync that change to the associated Task
                    Messenger.Default.Register<ServiceTypeKeyChangedEvent>(
                        this,
                        Constants.DomainEvents.ServiceTypeKeyChanged,
                        evt =>
                        {
                            if (evt == null)
                            {
                                return;
                            }

                            if (Encounter == null)
                            {
                                return;
                            }

                            if (Encounter.Task == null)
                            {
                                return;
                            }

                            // NOTE: only sync change to Task if the ServiceTypeKeyChanged was a change for "OUR" instance of DynamicFormViewModel
                            // E.G. there can be multiple instances of DynamicFormViewModel open in memory
                            if (Encounter.EncounterKey == evt.EncounterKey)
                            {
                                // Need this check to stop the stack from overflowing - propertychanged handler keeps getting triggered...
                                if (Encounter.Task.ServiceTypeKey.GetValueOrDefault() != evt.NewServiceTypeKey)
                                {
                                    Encounter.Task.ServiceTypeKey = evt.NewServiceTypeKey;
                                }
                            }
                        });
                }

                this.RaisePropertyChangedLambda(p => p.Question);
                this.RaisePropertyChangedLambda(p => p.Protected);
            }
        }

        public int Sequence { get; set; }
        public int QuestionSequenceWithinSection { get; set; }
        public virtual string Label { get; set; }
        public int IndentLevel { get; set; }
        public ICollectionView PrintCollection { get; set; }
        Patient _Patient;

        public Patient Patient
        {
            get { return _Patient; }
            set
            {
                _Patient = value;
                this.RaisePropertyChangedLambda(p => p.Patient);
            }
        }

        private Admission _Admission;

        public Admission Admission
        {
            get { return _Admission; }
            set
            {
                if (value != _Admission)
                {
                    _Admission = value;
                }

                this.RaisePropertyChangedLambda(p => p.Admission);
                this.RaisePropertyChangedLambda(p => p.IsHospiceAdmission);
            }
        }

        public Admission CurrentAdmission => Admission;
        public bool IsHospiceAdmission => IsHospiceServiceLine;

        public bool IsHospiceServiceLine
        {
            get
            {
                if (Admission == null)
                {
                    return false;
                }

                return Admission.HospiceAdmission;
            }
        }

        private AdmissionDiscipline _AdmissionDiscipline;

        public AdmissionDiscipline AdmissionDiscipline
        {
            get { return _AdmissionDiscipline; }
            set
            {
                if (value != _AdmissionDiscipline)
                {
                    _AdmissionDiscipline = value;
                }

                this.RaisePropertyChangedLambda(p => p.AdmissionDiscipline);
            }
        }

        private Encounter _Encounter;

        public Encounter Encounter
        {
            get { return _Encounter; }
            set
            {
                _Encounter = value;
                if (Encounter != null)
                {
                    OriginalFormStatus = Encounter.EncounterStatus;
                    this.RaisePropertyChangedLambda(p => p.Encounter);
                }
            }
        }

        public IGoalManagement GoalManager { get; set; }
        public OasisManager OasisManager { get; set; }
        public RelayCommand ProcessGoals { get; set; }

        private bool _Required;

        public bool Required
        {
            get { return _Required; }
            set
            {
                if (_Required != value)
                {
                    _Required = value;

                    this.RaisePropertyChangedLambda(p => p.Required);
                }
            }
        }

        public bool ConditionalRequired;
        private int OriginalFormStatus;

        public bool Protected
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

                if (DynamicFormViewModel != null && DynamicFormViewModel.IsReadOnlyEncounter)
                {
                    return true;
                }

                // check if question is protected by definition (metadata)
                if (ProtectedOverride)
                {
                    return true;
                }

                // check if question  protection has been overridden from the default
                if (ProtectedOverrideRunTime != null)
                {
                    return (bool)ProtectedOverrideRunTime;
                }

                // fall into default protection processing
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

                if (Encounter.UserIsPOCOrderReviewerAndInPOCOrderReview)
                {
                    return false;
                }

                // interim save logic
                if (Encounter.EncounterStatus != OriginalFormStatus)
                {
                    return false;
                }

                //Not saved to server, so allow edits regardless of status
                if (Encounter.EncounterKey <= 0)
                {
                    return false;
                }

                // all other cases are protected:
                //   completed forms 
                //    ones in CoderReview or OASISReview - unless overridden by a particular question (ProtectedOverrideRunTime = true)
                return true;
            }
        }

        public bool ProtectedOverride
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

                if (Question == null)
                {
                    return false;
                }

                return Question.ProtectedOverride;
            }
        }

        private bool? _ProtectedOverrideRunTime;

        public bool? ProtectedOverrideRunTime
        {
            get { return _ProtectedOverrideRunTime; }
            set
            {
                _ProtectedOverrideRunTime = value;
                this.RaisePropertyChangedLambda(p => p.ProtectedOverrideRunTime);
                this.RaisePropertyChangedLambda(p => p.Protected);
            }
        }

        private bool _Hidden;

        public bool Hidden
        {
            get
            {
                var _ret = HiddenOverride == null
                    ? (_Hidden || HideSectionOverride)
                    : ((bool)HiddenOverride || HideSectionOverride);
                return _ret;
            }
            set
            {
                if (_Hidden != value)
                {
                    _Hidden = value;

                    this.RaisePropertyChangedLambda(p => p.Hidden);
                }
            }
        }

        private bool? _HiddenOverride;

        public bool? HiddenOverride
        {
            get { return _HiddenOverride; }
            set
            {
                if (_HiddenOverride != value)
                {
                    _HiddenOverride = value;
                    this.RaisePropertyChangedLambda(p => p.Hidden);
                }
            }
        }

        private bool _HideSectionOverride;

        public bool HideSectionOverride
        {
            get { return _HideSectionOverride; }
            set
            {
                if (_HideSectionOverride != value)
                {
                    _HideSectionOverride = value;
                    this.RaisePropertyChangedLambda(p => p.Hidden);
                }
            }
        }

        public bool IsSectionLabelQuestion
        {
            get
            {
                if ((Question.BackingFactory == "SectionLabel") && (Question.DataTemplate == "SectionLabel"))
                {
                    return true;
                }

                if ((Question.BackingFactory == "OasisSectionLabel") && (Question.DataTemplate == "OasisSectionLabel"))
                {
                    return true;
                }

                return false;
            }
        }

        private bool _ConditionalHidden;

        public bool ConditionalHidden
        {
            get { return _ConditionalHidden; }
            set
            {
                _ConditionalHidden = value;
                this.RaisePropertyChangedLambda(p => p.ConditionalHidden);
            }
        }

        private string _ValidationError = string.Empty;

        public string ValidationError
        {
            get { return _ValidationError; }
            set
            {
                if (_ValidationError != value)
                {
                    _ValidationError = value;

                    this.RaisePropertyChangedLambda(p => p.ValidationError);
                }
            }
        }

        public RelayCommand<int> ReportedByOpened { get; set; }

        public RelayCommand<int> ReportedByClosed { get; set; }

        public RelayCommand QuestionValueChanged { get; set; }
        public RelayCommand<string> GraphCommand { get; set; }

        public List<PropertyInfo> GetAllPropertiess(Type classObj, int levels)
        {
            levels++;
            List<PropertyInfo> localFields = new List<PropertyInfo>();

            var tmpList = classObj
                .GetProperties().OfType<PropertyInfo>().Where(p => p.CanWrite
                                                                   && (p.PropertyType == typeof(int)
                                                                       || p.PropertyType == typeof(int)
                                                                       || p.PropertyType == typeof(Int32)
                                                                       || p.PropertyType == typeof(string)
                                                                       || p.PropertyType == typeof(String)
                                                                       || p.PropertyType == typeof(bool))).ToList();
            localFields.AddRange(tmpList);

            return localFields;
        }

        public virtual Dictionary<String, String> SaveOfflineState()
        {
            Type myObj = GetType();
            Dictionary<String, String> retDict = new Dictionary<string, string>();
            var fields = GetAllPropertiess(myObj, 1);

            foreach (var rc in fields)
                if (!retDict.ContainsKey(rc.Name))
                {
                    try
                    {
                        var rcValue = rc.GetValue(this, null);
                        if (rcValue != null)
                        {
                            retDict.Add(rc.Name, rcValue.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        var msg = e.Message;
                    } // ignore and don't add the item if the value is null;
                }

            return retDict;
        }

        public virtual void RestoreOfflineState(DynamicFormInfo state)
        {
            if (state == null)
            {
                return;
            }

            if (state.SavedQuestionState == null)
            {
                return;
            }

            if (state.SavedQuestionState.SavedQuestions == null)
            {
                return;
            }

            if (Question == null)
            {
                return;
            }

            if (QuestionID.GetValueOrDefault(0) > 0)
            {
                var questionKey = QuestionID.Value;
                if (!state.SavedQuestionState.SavedQuestions.ContainsKey(questionKey))
                {
                    return;
                }

                var savedQuestion = state.SavedQuestionState.SavedQuestions[questionKey];

                Type myObj = GetType();
                var fields = GetAllPropertiess(myObj, 1);

                foreach (var rc in fields)
                    try
                    {
                        if (savedQuestion.ContainsKey(rc.Name))
                        {
                            var quesValue = savedQuestion[rc.Name];
                            if (quesValue != null)
                            {
                                rc.SetValue(this, Convert.ChangeType(quesValue, rc.PropertyType, null), null);
                            }
                        }
                    }
                    catch
                    {
                    } // ignore and don't try to restore the item if the value is null;
            }
        }

        public virtual void RefreshPrintCollection()
        {
        }

        private bool _isClonedQuestion;

        public bool IsClonedQuestion
        {
            get { return _isClonedQuestion; }
            set { _isClonedQuestion = value; }
        }

        private bool _canTrimPrintCollection;

        public bool CanTrimPrintCollection
        {
            get { return _canTrimPrintCollection; }
            set { _canTrimPrintCollection = value; }
        }

        public virtual void SetupMessages()
        {
            foreach (var item in Question.DestinationQuestionNotification)
                switch (item.MessageType)
                {
                    case "SkipCodeLookup":
                        Messenger.Default.Register<int?[]>(this,
                            string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, Encounter.AdmissionKey,
                                Encounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey), message =>
                            {
                                // if the DestinationParentKey in the notifications does not have a value (null)
                                // or the QuestionGroupKey for this question = the DestinationParentKey in the notification
                                if (!message[0].HasValue || QuestionGroupKey == message[0])
                                {
                                    // hide if BuildSkipandRequiredMessage returned true (the checkbox is checked)
                                    bool hide = Convert.ToBoolean(message[1]);
                                    if (hide)
                                    {
                                        ClearEntity();
                                    }

                                    Hidden = hide;
                                }
                            });
                        break;
                    case "Skip":
                        Messenger.Default.Register<int?[]>(this,
                            string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, Encounter.AdmissionKey,
                                Encounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey), message =>
                            {
                                // if the DestinationParentKey in the notifications does not have a value (null)
                                // or the QuestionGroupKey for this question = the DestinationParentKey in the notification
                                if (!message[0].HasValue || QuestionGroupKey == message[0])
                                {
                                    // hide if BuildSkipandRequiredMessage returned true (the checkbox is checked)
                                    bool hide = Convert.ToBoolean(message[1]);
                                    if (hide)
                                    {
                                        ClearEntity();
                                    }

                                    Hidden = hide;
                                }
                            });
                        break;
                    case "Required":
                        Messenger.Default.Register<int?[]>(this,
                            string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, Encounter.AdmissionKey,
                                Encounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey), message =>
                            {
                                if (!message[0].HasValue || QuestionGroupKey == message[0])
                                {
                                    bool req = Convert.ToBoolean(message[1]);

                                    ConditionalRequired = req;
                                }
                            });
                        break;
                    case "Tinetti":
                        Messenger.Default.Register<int[]>(this,
                            string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, Encounter.AdmissionKey,
                                Encounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey), message =>
                            {
                                if (QuestionGroupKey == message[0] || message[3] == 28)
                                {
                                    ProcessTinettiMessage(message[1], message[2], message[3]);
                                }
                            });
                        break;
                    case "BMI":
                        Messenger.Default.Register<string[]>(this,
                            string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, Encounter.AdmissionKey,
                                Encounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey),
                            message => { ProcessBMIMessage(message); });
                        break;
                    case "Amputation":
                        Messenger.Default.Register<int[]>(this,
                            string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, Encounter.AdmissionKey,
                                Encounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey),
                            message => { ProcessAmputationMessage(message); });
                        break;
                }

            ReportedByOpened = new RelayCommand<int>(mydata => { PreviousReportedBy = mydata; });

            ReportedByClosed = new RelayCommand<int>(mydata =>
            {
                if (PreviousReportedBy != mydata && PreviousReportedBy.HasValue && PreviousReportedBy != 0)
                {
                    string previousReportedBy = "";

                    if (PreviousReportedBy.HasValue && PreviousReportedBy.Value != 0)
                    {
                        previousReportedBy =
                            CodeLookupCache.GetCodeDescriptionFromKey("ESASReported", PreviousReportedBy.Value);
                    }
                    else
                    {
                        previousReportedBy = "Unanswered";
                    }

                    NavigateCloseDialog d = CreateDialogue(null,
                        "There can only be one answer for all the 'Reported By' questions within this encounter. 'Reported By' was previously answered with '" +
                        previousReportedBy +
                        "', changing this value here will change it for all ESAS 'Reported By' questions in this encounter. Do you want to proceed?");
                    if (d != null)
                    {
                        d.Closed += (s, err) =>
                        {
                            if (s != null)
                            {
                                var _ret = ((ChildWindow)s).DialogResult;
                                if (_ret == false) //user chose NOT to change the value
                                {
                                    Encounter.ReportedBy =
                                        PreviousReportedBy; // rollback their change to the previous value
                                }
                            }
                        };
                        d.Show();
                    }
                }
            });

            QuestionValueChanged = new RelayCommand(() =>
            {
                foreach (var item in Question.SourceQuestionNotification)
                    switch (item.MessageType)
                    {
                        case "SkipCodeLookup":
                            if (!Hidden)
                            {
                                int?[] skip = new int?[2]
                                {
                                    item.DestinationParentKey, Convert.ToInt32(BuildSkipandRequiredMessage(item, true))
                                };
                                Messenger.Default.Send(skip,
                                    string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, Encounter.AdmissionKey,
                                        Encounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey));
                            }

                            break;
                        case "Skip":
                            if (!Hidden)
                            {
                                int?[] skip = new int?[2]
                                {
                                    item.DestinationParentKey, Convert.ToInt32(BuildSkipandRequiredMessage(item, true))
                                };
                                Messenger.Default.Send(skip,
                                    string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, Encounter.AdmissionKey,
                                        Encounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey));
                            }

                            break;
                        case "Required":
                            if (!Hidden)
                            {
                                int?[] req = new int?[2]
                                    { item.DestinationParentKey, Convert.ToInt32(BuildSkipandRequiredMessage(item)) };
                                Messenger.Default.Send(req,
                                    string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, Encounter.AdmissionKey,
                                        Encounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey));
                            }

                            break;
                        case "Tinetti":
                            if (Convert.ToInt32(item.DataValue) < 28 || QuestionGroupKey == item.SourceParentKey)
                            {
                                int[] tinetti = new int[4]
                                {
                                    QuestionGroupKey, item.SourceQuestionKey, BuildTinettiMessage(item),
                                    Convert.ToInt32(item.DataValue)
                                };
                                Messenger.Default.Send(tinetti,
                                    string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, Encounter.AdmissionKey,
                                        Encounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey));
                            }

                            break;
                        case "Amputation"
                            : //amputation doesn't fall thru here and has it's own relay command to initiate message
                            break;
                        case "BMI":
                            Messenger.Default.Send(
                                new string[3] { Question.Label, BuildBMIMessage(true), BuildBMIMessage(false) },
                                string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, Encounter.AdmissionKey,
                                    Encounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey));
                            break;
                    }
            });
            GraphCommand = new RelayCommand<string>(reading =>
            {
                DynamicFormGraph dfg = new DynamicFormGraph(Admission, Encounter, reading);
                dfg.Show();
            });
        }

        private int? PreviousReportedBy;

        public NavigateCloseDialog CreateDialogue(String Title, String Msg)
        {
            NavigateCloseDialog d = new NavigateCloseDialog
            {
                Width = 600,
                Height = double.NaN,
                ErrorMessage = Msg,
                ErrorQuestion = null,
                Title = Title, //Title property displays in large black text
                HasCloseButton = false
            };

            return d;
        }

        public virtual void PreProcessing()
        {
        }

        public virtual void PostSaveProcessing()
        {
        }

        public virtual bool BuildSkipandRequiredMessage(QuestionNotification qn, bool skipmessage = false)
        {
            return false;
        }

        public virtual int BuildTinettiMessage(QuestionNotification qn)
        {
            return 0;
        }

        public virtual string BuildBMIMessage(bool value)
        {
            return string.Empty;
        }

        public virtual void ProcessTinettiMessage(int QuestionKey, int Score, int PossibleScore)
        {
        }

        public virtual void ProcessAmputationMessage(int[] message)
        {
        }

        public virtual void ProcessBMIMessage(string[] message)
        {
        }

        public virtual bool CopyForwardLastInstance()
        {
            return false;
        }

        public virtual void CopyForwardfromEncounter(Encounter e)
        {
        }

        public virtual void BackupEntity(bool restore)
        {
        }

        public virtual void ClearEntity()
        {
        }

        public virtual bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            return true;
        }

        public bool ValidateVitalsReadingDateTime(IValidateVitalsReadingDateTime item)
        {
            if (item == null)
            {
                return true;
            }

            if ((item.ReadingDateTime == DateTime.MinValue) || (item.Version == 1))
            {
                item.ReadingDateTime = null;
            }

            if (Protected)
            {
                return true; // no validation if we can't change anything (out of clinican edit)
            }

            if (item.ReadingDateTime == null)
            {
                return true;
            }

            // we have a reading datetime - sync its date with the Encounter start date
            // we need to do this since we only collect a time and we have to assume the date is of this encounter
            DateTime startDateTime = (Encounter.EncounterOrTaskStartDateAndTime.HasValue)
                ? ((DateTimeOffset)Encounter.EncounterOrTaskStartDateAndTime).DateTime
                : DateTime.Now;
            int days = Convert.ToInt32(startDateTime.Date.Subtract(((DateTime)item.ReadingDateTime).Date).TotalDays);
            DateTime dt = item.ReadingDateTime.Value;
            dt = dt.AddDays(days);
            if (dt < startDateTime)
            {
                dt = dt.AddDays(1); // To support encounters that span midnight
            }

            item.ReadingDateTime = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
            // validate it
            if ((item.ReadingDateTime < startDateTime) || (item.ReadingDateTime > DateTime.Now))
            {
                item.ValidationErrors.Add(new ValidationResult(
                    "The Reading Time cannot be before the Encounter Start Time and cannot be in the future.",
                    new[] { "ReadingDateTimeTimePart" }));
                return false;
            }

            if ((Encounter.EncounterEndDate.HasValue) && (Encounter.EncounterEndTime.HasValue) &&
                (Encounter.EncounterEndDateAndTime.Value.DateTime < item.ReadingDateTime))
            {
                item.ValidationErrors.Add(new ValidationResult(
                    "The Reading Time cannot be after the Encounter End Time.", new[] { "ReadingDateTimeTimePart" }));
                return false;
            }

            return true;
        }

        public virtual QuestionUI Clone()
        {
            return null;
        }

        int __cleanupCount;

        public override void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            Messenger.Default.Unregister(this);
            DynamicFormViewModel = null;
            if (DataTemplateHelper != null)
            {
                DataTemplateHelper.Cleanup();
            }

            base.Cleanup();
        }

        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject QuestionDataTemplateLoaded
        {
            get
            {
                if (Question == null)
                {
                    return null;
                }

                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }

                if (DataTemplateHelper.IsDataTemplateLoaded(Question.DataTemplate))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        RaisePropertyChanged(null);
                        if ((OasisManager != null) && Question.DataTemplate.StartsWith("OasisQuestion"))
                        {
                            OasisManager.OasisAnswerResponseChanged();
                        }
                    });
                }

                return DataTemplateHelper.LoadDataTemplate(Question.DataTemplate);
            }
        }

        public DependencyObject QuestionDataTemplateLoadedPopup
        {
            get
            {
                if (Question == null)
                {
                    return null;
                }

                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }

                return DataTemplateHelper.LoadDataTemplate(Question.DataTemplate);
            }
        }

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        protected readonly Dictionary<string, List<string>> _currentErrors = new Dictionary<string, List<string>>();

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

        protected void FireErrorsChanged(string property)
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

    public class QuestionBase : QuestionUI
    {
        public bool IsOnline => EntityManager.IsOnline;
        public RelayCommand DataTemplateLoaded { get; set; }
        public AdmissionPhysicianFacade AdmissionPhysician { get; set; }

        public QuestionBase(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            DataTemplateLoaded = new RelayCommand(() =>
            {
                ProtectedOverrideRunTime = SetupOrderEntryProtectedOverrideRunTime();
                if (OrderEntryManager != null)
                {
                    this.RaisePropertyChangedLambda(p => p.Protected);
                }
            });
            ProcessGoals = new RelayCommand(() =>
            {
                string response = string.Empty;
                string subresponse = string.Empty;
                string reason = string.Empty;
                bool remove = false;
                int? keytouse = null;
                bool callcommand = false;
                if (Question == null)
                {
                    return; // happens during cleanup routine.
                }

                if (Question.DataTemplate.Equals("Text") && EncounterData != null && EncounterData.TextData != null)
                {
                    //NOTE: ProcessGoals is called on LostFocus for the binding to EncounterData.TextData in DataTemplate x:Key="Text"
                    EncounterData.TextData = EncounterData.TextData.Trim();
                }

                if (Question.DataTemplate.Equals("Text") && !string.IsNullOrEmpty(Question.LookupType))
                {
                    callcommand = true;
                    if (!string.IsNullOrEmpty(EncounterData.TextData) &&
                        !string.IsNullOrEmpty(EncounterData.FuncDeficit))
                    {
                        response = EncounterData.TextData;
                        reason = EncounterData.FuncDeficit;
                    }
                    else
                    {
                        remove = true;
                    }
                }
                else if (Question.DataTemplate.StartsWith("PTLevelofAssist"))
                {
                    callcommand = true;
                    if (EncounterData.IntData > 0 && !string.IsNullOrEmpty(EncounterData.TextData) &&
                        !string.IsNullOrEmpty(EncounterData.FuncDeficit))
                    {
                        response = EncounterData.IntData.ToString();
                        subresponse = EncounterData.TextData;
                        reason = EncounterData.FuncDeficit;
                        keytouse = EncounterData.IntData;
                    }
                    else
                    {
                        remove = true;
                    }
                }
                else if (Question.DataTemplate.Equals("CodeLookupMulti"))
                {
                    callcommand = true;
                    if (!string.IsNullOrEmpty(EncounterData.TextData) &&
                        !string.IsNullOrEmpty(EncounterData.FuncDeficit))
                    {
                        response = EncounterData.TextData;
                        reason = EncounterData.FuncDeficit;
                    }
                    else
                    {
                        remove = true;
                    }
                }
                else if (Question.DataTemplate.Equals("Stairs"))
                {
                    callcommand = true;
                    if (EncounterData.IntData > 0)
                    {
                        response = CodeLookupCache.GetCodeLookupFromKey(EncounterData.IntData.Value).CodeDescription;
                        reason = response;
                    }
                    else
                    {
                        remove = true;
                    }
                }
                else if (Question.DataTemplate.Equals("DisciplineRefer"))
                {
                    callcommand = true;

                    foreach (var s in DynamicFormViewModel.Sections)
                        if (s.Label.Equals("Re-Evaluate"))
                        {
                            IEnumerable<ReEvaluate> re = s.Questions.OfType<ReEvaluate>();
                            foreach (ReEvaluate r in re)
                                if (r.ReEvalSection != null)
                                {
                                    QuestionBase qb = (QuestionBase)r.ReEvalSection.Questions
                                        .Where(p => p.Label.Equals(Label)).FirstOrDefault();
                                    if (qb != null)
                                    {
                                        response = !string.IsNullOrEmpty(response)
                                            ? response + "  " + qb.EncounterData.TextData
                                            : qb.EncounterData.TextData;
                                    }
                                }
                        }
                        else
                        {
                            QuestionBase qb = (QuestionBase)s.Questions
                                .Where(p => p.Label != null && p.Label.Equals(Label)).FirstOrDefault();
                            if (qb != null)
                            {
                                response = !string.IsNullOrEmpty(response)
                                    ? response + "  " + qb.EncounterData.TextData
                                    : qb.EncounterData.TextData;
                            }
                        }

                    if (string.IsNullOrEmpty(response))
                    {
                        remove = true;
                    }
                }

                if (callcommand)
                {
                    GoalManager.UpdateGoals(this, response, subresponse, reason, remove, keytouse);
                }
            });
        }

        public bool NotesExist => String.IsNullOrEmpty(QuestionNotes) ? false : true;

        public bool CanPerformNPWT
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                return Encounter.CanPerformNPWT;
            }
        }

        public EncounterAttempted EncounterAttempted =>
            (DynamicFormViewModel == null) ? null : DynamicFormViewModel.CurrentEncounterAttempted;

        public virtual string QuestionNotesOverride { get; set; }

        public String QuestionNotes
        {
            get
            {
                if (!String.IsNullOrEmpty(QuestionNotesOverride))
                {
                    return QuestionNotesOverride;
                }

                FormSectionQuestion fsq = null;
                FormSection formsect = null;
                if (Question.FormSectionQuestion.Count == 1)
                {
                    fsq = Question.FormSectionQuestion.FirstOrDefault();
                }

                if (fsq == null && DynamicFormViewModel.CurrentForm != null)
                {
                    formsect = DynamicFormViewModel.CurrentForm.FormSection
                        .Where(fs => fs.SectionKey == Section.SectionKey).FirstOrDefault();
                    if (formsect != null)
                    {
                        fsq = formsect.FormSectionQuestion.Where(f => f.QuestionKey == Question.QuestionKey)
                            .FirstOrDefault();
                    }
                }

                if (fsq == null && formsect == null)
                {
                    return null;
                }

                var nte = fsq.GetQuestionAttributeForName("NOTE");
                if (nte == null)
                {
                    return null;
                }

                return nte.AttributeValue;
            }
        }

        public bool HasInfoIcon
        {
            get
            {
                if ((Question == null) || string.IsNullOrWhiteSpace(Question.SubLookupType))
                {
                    return false;
                }

                return true;
            }
        }

        public string InfoIconText
        {
            get
            {
                if ((Question == null) || string.IsNullOrWhiteSpace(Question.SubLookupType))
                {
                    return null;
                }

                return Question.SubLookupType;
            }
        }

        public IDynamicFormService Model => DynamicFormViewModel.FormModel;
        private EncounterData _EncounterData;

        public EncounterData EncounterData
        {
            get { return _EncounterData; }
            set
            {
                _EncounterData = value;
                this.RaisePropertyChangedLambda(p => p.EncounterData);
            }
        }

        public EncounterData BackupEncounterData { get; set; }
        public Dictionary<int, int> TotalScores = new Dictionary<int, int>();

        public override void PreProcessing()
        {
            if (QuestionValueChanged != null)
            {
                QuestionValueChanged.Execute(this);
            }

            if ((Question.QuestionOasisMapping != null) && (Encounter.IsNew))
            {
                if (Question.QuestionOasisMapping.Any() && (OasisManager != null))
                {
                    OasisManager.QuestionOasisMappingChanged(Question, EncounterData);
                }
            }
        }

        public void Setup()
        {
            // Setup the admission physician facade
            bool useThisEncounter = false;
            if (Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
            {
                useThisEncounter = true;
            }

            AdmissionPhysician = new AdmissionPhysicianFacade(useThisEncounter);
            AdmissionPhysician.Admission = Admission;
            AdmissionPhysician.Encounter = Encounter;
            //

            if (Question.DataTemplate.Equals("URL"))
            {
                Messenger.Default.Register<bool>(this, Constants.Messaging.NetworkAvailability,
                    IsAvailable => { RaisePropertyChanged("IsOnline"); });
            }

            if ((Question.DataTemplate.Equals("DischargeReason") == false) &&
                (Question.DataTemplate.Equals("DischargeReasonHomeHealth") == false))
            {
                return;
            }

            if (DynamicFormViewModel.CurrentForm.IsDischarge == false)
            {
                return;
            }

            if (Patient == null)
            {
                return;
            }

            if (AdmissionDiscipline == null)
            {
                return;
            }

            if (DynamicFormViewModel.CurrentForm.IsDischarge == false)
            {
                return;
            }

            if ((Patient.DeathDate == null) || (Patient.DeathDate == null))
            {
                return;
            }

            if (AdmissionDiscipline.DischargeReasonKey != null)
            {
                return;
            }

            AdmissionDiscipline.DischargeReasonKey = (int)CodeLookupCache.GetKeyFromCode("PATDISCHARGEREASON", "20");
        }

        public override bool BuildSkipandRequiredMessage(QuestionNotification qn, bool skipmessage = false)
        {
            try
            {
                if (qn.DataValue == null)
                {
                    if (qn.DataMember.Equals("BoolData"))
                    {
                        if ((bool)EncounterData.GetType().GetProperty(qn.DataMember).GetValue(EncounterData, null))
                        {
                            return true;
                        }

                        return false;
                    }

                    if (qn.DataMember.Equals("IntData"))
                    {
                        if ((int)EncounterData.GetType().GetProperty(qn.DataMember).GetValue(EncounterData, null) > 0)
                        {
                            return true;
                        }

                        return false;
                    }

                    if ((qn.DataMember.Equals("TextData")) || (qn.DataMember.Equals("Text2Data")))
                    {
                        if (string.IsNullOrWhiteSpace(EncounterData.TextData))
                        {
                            return false;
                        }

                        if (string.IsNullOrWhiteSpace(qn.DataValue))
                        {
                            return false;
                        }

                        if (EncounterData.TextData.Trim().ToLower() == qn.DataValue.Trim().ToLower())
                        {
                            return true;
                        }

                        return false;
                    }
                }
                else
                {
                    if (qn.DataMember.Equals("CodeDescription"))
                    {
                        // Currently, CodeLookups are always stored in IntData in an EncounterData row

                        // Our DataValue for CodeLookup should have the form "CodeType|Code"
                        string[] values = qn.DataValue.Split('|');
                        string temp = CodeLookupCache.GetCodeDescriptionFromKey(
                            Convert.ToInt32(
                                EncounterData.GetType().GetProperty("IntData").GetValue(EncounterData, null)));

                        if (values == null)
                        {
                            return false;
                        }

                        return !values.Contains(temp);
                    }

                    if (qn.DataMember.Equals("IntData"))
                    {
                        string[] values = qn.DataValue.Split('|');
                        string temp = CodeLookupCache.GetCodeFromKey(Convert.ToInt32(EncounterData.GetType()
                            .GetProperty(qn.DataMember).GetValue(EncounterData, null)));
                        if (values.Contains(temp))
                        {
                            return true;
                        }

                        return false;
                    }

                    if (qn.DataValue.StartsWith("!%"))
                    {
                        string temp = qn.DataValue.Substring(2);
                        var data = EncounterData.GetType().GetProperty(qn.DataMember).GetValue(EncounterData, null);
                        if (data != null && data.ToString().Contains(temp))
                        {
                            return true;
                        }

                        return false;
                    }

                    if (qn.DataValue.StartsWith("!"))
                    {
                        string temp = qn.DataValue.Substring(1);
                        var data = EncounterData.GetType().GetProperty(qn.DataMember).GetValue(EncounterData, null);
                        if (temp.Equals(data) || (data == null && skipmessage))
                        {
                            return false;
                        }

                        return true;
                    }

                    if (qn.DataValue.StartsWith("^"))
                    {
                        string temp = qn.DataValue.Substring(1);
                        var data = EncounterData.GetType().GetProperty(qn.DataMember).GetValue(EncounterData, null);
                        if (data != null && data.ToString().StartsWith(temp))
                        {
                            return false;
                        }

                        return true;
                    }

                    if (qn.DataValue.StartsWith("%"))
                    {
                        string temp = qn.DataValue.Substring(1);
                        var data = EncounterData.GetType().GetProperty(qn.DataMember).GetValue(EncounterData, null);
                        if (data != null && data.ToString().Contains(temp))
                        {
                            return false;
                        }

                        return true;
                    }
                    else
                    {
                        var o = EncounterData.GetType().GetProperty(qn.DataMember).GetValue(EncounterData, null);
                        string data = o as string;
                        if ((qn.DataValue.StartsWith("|")) && (string.IsNullOrWhiteSpace(data)))
                        {
                            return false;
                        }

                        string[] values = qn.DataValue.Split('|');
                        if (values.Contains(data) || ((data == null || string.IsNullOrEmpty(data)) && skipmessage))
                        {
                            return true;
                        }

                        return false;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public override int BuildTinettiMessage(QuestionNotification qn)
        {
            var value = EncounterData.GetType().GetProperty(qn.DataMember).GetValue(EncounterData, null);
            if (value == null || (int)value == 0)
            {
                return 0;
            }

            return CodeLookupCache.GetSequenceFromKey((int)value).Value - 1;
        }

        public override void ProcessTinettiMessage(int QuestionKey, int Score, int PossibleScore)
        {
            if (TotalScores.ContainsKey(QuestionKey))
            {
                TotalScores.Remove(QuestionKey);
            }

            TotalScores.Add(QuestionKey, Score);

            if (!DynamicFormViewModel.IsBusy)
            {
                EncounterData.TextData = string.Format("{0} / {1}", TotalScores.Sum(p => p.Value), PossibleScore);
            }
        }

        public void CopyProperties(EncounterData source)
        {
            EncounterData.TextData = source.TextData;
            EncounterData.Text2Data = source.Text2Data;
            EncounterData.Text3Data = source.Text3Data;
            EncounterData.Text4Data = source.Text4Data;
            EncounterData.Text5Data = source.Text5Data;

            EncounterData.DateTimeData = source.DateTimeData;
            EncounterData.AddedDateTime = source.AddedDateTime;

            EncounterData.GuidData = source.GuidData;

            EncounterData.IntData = source.IntData;
            EncounterData.Int2Data = source.Int2Data;

            EncounterData.BoolData = source.BoolData;
            EncounterData.Bool2Data = source.Bool2Data;
            EncounterData.Bool3Data = source.Bool3Data;

            EncounterData.RealData = source.RealData;

            EncounterData.FuncDeficit = source.FuncDeficit;

            EncounterData.SignatureData = source.SignatureData;

            if (Question.QuestionOasisMapping != null)
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

        public override bool CopyForwardLastInstance()
        {
            if ((Question.DataTemplate == "ESASReported") && (Section != null))
            {
                // BUG 9714 - HIS Comment text displaying incorrectly-flow from HSN Eval is incorrect
                // Note: this is probably a bigger issue than just the generic question with datatemplate 'ESASReported'.
                // This question is a use-case where a given question is used in different form sections and
                // we want to copy forward that question ONLY within the context of that given section.
                // We should probably do this for any other generic - yet - section-specific questions,
                // but I don't want to generically override all copy forward logic - as other questions may BE section agnostic.
                // So - coding for the confines of BUG 9714 - just for question where DataTemplate = "ESASReported" - 
                // hunt for a default for this QuestionKey - in the SAME named section - on a previous form
                foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                             .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                {
                    EncounterData previous = item.EncounterData.Where(d =>
                            ((d.SectionKey == Section.SectionKey) && (d.QuestionKey == Question.QuestionKey)))
                        .FirstOrDefault();
                    if (previous != null)
                    {
                        CopyProperties(previous);
                        return true;
                        ;
                    }
                }

                return false;
            }

            int questionkey = DynamicFormCache.GetQuestionKeyByLabel("Orientation");
            foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                EncounterData previous = item.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey)
                    .FirstOrDefault();
                if (previous == null && Question.Label == "Orientation No Deficit")
                {
                    previous = item.EncounterData.Where(d => d.QuestionKey == questionkey).FirstOrDefault();
                }

                if (previous != null)
                {
                    CopyProperties(previous);
                    return true;
                    ;
                }
            }

            return false;
        }

        public void CopyForwardLastTeamMeetingNote()
        {
            if ((Admission == null) || (Admission.Encounter == null) || (EncounterData == null))
            {
                return;
            }

            var q = DynamicFormCache.GetQuestionByDataTemplate("TeamMeetingDates").FirstOrDefault();
            if (q == null)
            {
                return;
            }

            var e = Admission.Encounter
                .Where(p =>
                    ((!p.IsNew) && (p.EncounterIsTeamMeeting && p.EncounterTeamMeeting.FirstOrDefault() != null)))
                .OrderByDescending(
                    a => a.EncounterTeamMeeting.FirstOrDefault().AdmissionTeamMeeting.LastTeamMeetingDate)
                .FirstOrDefault();

            if (e != null)
            {
                EncounterData.TextData = e.TeamMeetingNote;
            }
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
            if (ReEvalSectionCopyForward == false)
            {
                return;
            }

            EncounterData previous = e.EncounterData.Where(p =>
                p.QuestionKey == Question.QuestionKey && p.QuestionGroupKey == QuestionGroupKey &&
                p.Section.Label == Section.Label).FirstOrDefault();
            if (previous != null)
            {
                CopyProperties(previous);
            }
        }

        private bool ReEvalSectionCopyForward
        {
            get
            {
                // By default - ALLL data in Reevaluate sections copy forward (regardless on copy forward settings in FormMaintenance)
                // That feels wrong to me - but we will leave it that way and just change the ESAS-2 (PerceptionOf), Mid Arm Circumference (MidArmCircumference) and Abdominal Girth questions as per PBI 2436
                if (Question == null)
                {
                    return true;
                }

                if (ReEvalSectionForceCopyForward(Question))
                {
                    return true;
                }

                if (Question.FormSectionQuestion == null)
                {
                    return true;
                }

                FormSectionQuestion fsq = Question.FormSectionQuestion.FirstOrDefault();
                if (fsq == null)
                {
                    return true;
                }

                if (fsq.CopyForward)
                {
                    return true;
                }

                // The only 'override' on ReEvaluate section copyforward is for the ESAS-2 (PerceptionOf), Mid Arm Circumference (MidArmCircumference) and Abdominal Girth questions - they are the ONLY ones that don't copy forward by default 
                return false;
            }
        }

        private bool ReEvalSectionForceCopyForward(Question q)
        {
            // By default - ALLL data in Reevaluate sections copy forward (regardless on copy forward settings in FormMaintenance)
            // That feels wrong to me - but we will leave it that way and just change the ESAS-2 (PerceptionOf), Mid Arm Circumference (MidArmCircumference) and Abdominal Girth questions as per PBI 2436
            if (Question == null)
            {
                return true;
            }

            if (Question.DataTemplate == "PerceptionOf")
            {
                return false;
            }

            if (Question.DataTemplate == "MidArmCircumference")
            {
                return false;
            }

            if ((Question.DataTemplate == "IntegerWithGraph") && (Question.Label == "Abdominal Girth"))
            {
                return false;
            }

            return true;
        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                var previous = (EncounterData)Clone(BackupEncounterData);
                //need to copy so raise property changes gets called - can't just copy the entire object
                CopyProperties(previous);
            }
            else
            {
                BackupEncounterData = (EncounterData)Clone(EncounterData);
            }
        }

        public override void ClearEntity()
        {
            EncounterData.BoolData = null;
            EncounterData.DateTimeData = null;
            EncounterData.IntData = null;
            EncounterData.Int2Data = null;
            EncounterData.RealData = null;
            EncounterData.TextData = String.Empty;
            EncounterData.Text2Data = String.Empty;
            EncounterData.Text3Data = String.Empty;
            EncounterData.Text4Data = String.Empty;
            EncounterData.AddedDateTime = null;
            EncounterData.GuidData = null;
            EncounterData.FuncDeficit = String.Empty;
            EncounterData.SignatureData = null;
        }

        int __cleanupCount;

        public override void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            Messenger.Default.Unregister(this);
            if (EncounterData != null)
            {
                try
                {
                    EncounterData.PropertyChanged -= EncounterData_PropertyChanged;
                }
                catch
                {
                }
            }

            if (Encounter != null)
            {
                try
                {
                    Encounter.PropertyChanged -= Encounter_PropertyChanged;
                }
                catch
                {
                }
            }

            base.Cleanup();
        }

        private bool Validate_DischargeReasonKey()
        {
            if (Patient == null)
            {
                return true;
            }

            if (AdmissionDiscipline == null)
            {
                return true;
            }

            if (!AdmissionDiscipline.DischargeReasonKey.HasValue)
            {
                var memberNames = new[] { "DischargeReasonKey" };
                AdmissionDiscipline.ValidationErrors.Add(new ValidationResult(
                    "You must choose a discharge reason in order to discharge a discipline.", memberNames));
                return false;
            }

            Patient.ValidateState_DeathDateRequired = false;
            Patient.ValidationErrors.Clear();
            if (AdmissionDiscipline.DischargeReasonKey != null)
            {
                if (AdmissionDiscipline.DischargeReasonKey ==
                    (int)CodeLookupCache.GetKeyFromCode("PATDISCHARGEREASON", "20"))
                {
                    Patient.ValidateState_DeathDateRequired = true;
                }
                else
                {
                    Patient.DeathDate = null;
                }
            }

            bool ret = Patient.Validate();

            //DischargeReason - "Expired"
            if (ret && Patient.ValidateState_DeathDateRequired && Admission != null)
            {
                Admission.DeathDate = Patient.DeathDate;
            }

            Patient.ValidateState_DeathDateRequired = false;
            return ret;
        }

        private DateTime minSQLDateTime = new DateTime(1753, 1, 1);

        public override bool Validate(out string SubSections)
        {
            try
            {
                SubSections = string.Empty;

                if ((EncounterData == null) || (Question.DataTemplate.Equals("URL")))
                {
                    return true; // Labels and URLs
                }

                if (EncounterData != null)
                {
                    EncounterData.ValidationErrors.Clear();
                }

                if (Question.DataTemplate.Equals("DateUnknown")) // clear for required checking
                {
                    if (EncounterData.DateTimeData == DateTime.MinValue)
                    {
                        EncounterData.DateTimeData = null;
                    }

                    if (EncounterData.BoolData == false)
                    {
                        EncounterData.BoolData = null;
                    }
                }

                if (Question.DataTemplate.Equals("Date"))
                {
                    // Date validations can be triggered by SubLookupTypes
                    if (string.IsNullOrEmpty(Question.SubLookupType) == false &&
                        Question.SubLookupType.Equals("NoFutureDateValidation"))
                    {
                        if (EncounterData.DateTimeData.HasValue &&
                            EncounterData.DateTimeData.Value.Date > DateTime.Now.Date)
                        {
                            string error = "'" + Question.Label + "' cannot have a future date.";

                            EncounterData.ValidationErrors.Add(new ValidationResult(error, new[] { "DateTimeData" }));
                            return false;
                        }
                    }
                }

                if (Question.DataTemplate.Equals("OrderEntryOther"))
                {
                    return true;
                }

                if (Question.DataTemplate.Equals("ReferralReason") && Encounter.ServiceTypeKey.HasValue)
                {
                    return true;
                }

                if (Question.DataTemplate.Equals("ServiceType") && Encounter.ServiceTypeKey.HasValue)
                {
                    return true;
                }

                if (Question.DataTemplate.Equals("ServiceDate"))
                {
                    var ret = true;
                    if ((Encounter.FullValidationNTUC) ||
                        ((Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed) &&
                         RoleAccessHelper.CheckPermission(RoleAccess.Admin, false)))
                    {
                        if (Encounter.EncounterStartDate.HasValue == false)
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult("Service date is required.",
                                new[] { "EncounterStartDate" }));
                            ret = false;
                        }

                        if (Encounter.EncounterStartTime.HasValue == false)
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult("Service start time is required.",
                                new[] { "EncounterStartTime" }));
                            ret = false;
                        }

                        if (Encounter.EncounterStartDate.GetValueOrDefault().DateTime.Date > DateTime.Now.Date)
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult("Service date cannot be in the future.",
                                new[] { "EncounterStartDate" }));
                            ret = false;
                        }

                        var dynamicFormService = DynamicFormViewModel.FormModel as IServiceProvider;
                        var virtuosoContextProvider =
                            dynamicFormService.GetService(typeof(IVirtuosoContextProvider)) as IVirtuosoContextProvider;
                        if (virtuosoContextProvider != null)
                        {
                            var _admissionDiscipline = virtuosoContextProvider
                                .AdmissionDisciplinesByAdmissionKey(Encounter.AdmissionKey)
                                .Where(ad => ad.AdmissionDisciplineKey == Encounter.AdmissionDisciplineKey)
                                .FirstOrDefault();
                            if (_admissionDiscipline != null)
                            {
                                if (DynamicFormCache.IsEval(Encounter.FormKey.GetValueOrDefault()) ||
                                    DynamicFormCache.IsResumption(Encounter.FormKey.GetValueOrDefault()))
                                {
                                    //US 4131 - MUST include validation on the service date to make sure that the date that the clinician puts in is on or after 
                                    //          the discipline referral date for a ISEVAL or ISRESUME
                                    if (Encounter.EncounterStartDate.GetValueOrDefault().Date <
                                        _admissionDiscipline.ReferDateTime.GetValueOrDefault().Date)
                                    {
                                        Encounter.ValidationErrors.Add(new ValidationResult(
                                            string.Format(
                                                "The service date must be on or after the discipline referral date ({0:d})",
                                                _admissionDiscipline.ReferDateTime.GetValueOrDefault().Date),
                                            new[] { "EncounterStartDate" }));
                                        ret = false;
                                    }
                                }

                                if (
                                    DynamicFormCache.IsVisit(Encounter.FormKey.GetValueOrDefault()) ||
                                    DynamicFormCache.IsTransfer(Encounter.FormKey.GetValueOrDefault()) ||
                                    DynamicFormCache.IsDischarge(Encounter.FormKey.GetValueOrDefault()))
                                {
                                    //US 4131 - MUST include validation on the service date to make sure that the date that the clinician puts in is on or after the 
                                    //          discipline admit date for a ISVISIT, ISTRANSFER, or ISDISCHARGE
                                    if (Encounter.EncounterStartDate.GetValueOrDefault().Date < _admissionDiscipline
                                            .DisciplineAdmitDateTime.GetValueOrDefault().Date)
                                    {
                                        Encounter.ValidationErrors.Add(new ValidationResult(
                                            string.Format(
                                                "The service date must be on or after the discipline admit date ({0:d})",
                                                _admissionDiscipline.DisciplineAdmitDateTime.GetValueOrDefault().Date),
                                            new[] { "EncounterStartDate" }));
                                        ret = false;
                                    }
                                }

                                if (ret && Encounter.FullValidation && (DynamicFormViewModel.CurrentForm.IsEval ||
                                                                        DynamicFormViewModel.CurrentForm.IsResumption)
                                    && AdmissionDiscipline.Admitted &&
                                    AdmissionDiscipline.DisciplineAdmitDateTime != null &&
                                    Encounter.EncounterStartDate != null
                                    && AdmissionDiscipline.DisciplineAdmitDateTime.GetValueOrDefault().Date !=
                                    Encounter.EncounterStartDate.GetValueOrDefault().Date)
                                {
                                    Encounter.ValidationErrors.Add(new ValidationResult(
                                        string.Format("Service Date must match the Discipline Admit Date ({0:d}).",
                                            _admissionDiscipline.DisciplineAdmitDateTime.GetValueOrDefault().Date),
                                        new[] { "EncounterStartDate" }));
                                    ret = false;
                                }

                                if (ret && Encounter.FullValidation && (DynamicFormViewModel.CurrentForm.IsEval ||
                                                                        DynamicFormViewModel.CurrentForm.IsResumption)
                                    && AdmissionDiscipline.NotTaken && AdmissionDiscipline.NotTakenDateTime != null &&
                                    Encounter.EncounterStartDate != null
                                    && AdmissionDiscipline.NotTakenDateTime.GetValueOrDefault().Date !=
                                    Encounter.EncounterStartDate.GetValueOrDefault().Date)
                                {
                                    Encounter.ValidationErrors.Add(new ValidationResult(
                                        string.Format("Service Date must match the Not Admitted Date ({0:d}).",
                                            _admissionDiscipline.NotTakenDateTime.GetValueOrDefault().Date),
                                        new[] { "EncounterStartDate" }));
                                    ret = false;
                                }
                            }
                        }
                    }

                    return ret;
                }

                if (Question.DataTemplate.Equals("ServiceEndTime"))
                {
                    var ret = true; //default to everything is valid...
                    if ((Encounter.FullValidationNTUC) ||
                        ((Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed) &&
                         RoleAccessHelper.CheckPermission(RoleAccess.Admin, false)))
                    {
                        if (Encounter.EncounterEndDate.HasValue == false)
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult("Service end date is required.",
                                new[] { "EncounterEndDate" }));
                            ret = false;
                        }

                        if (Encounter.EncounterEndTime.HasValue == false)
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult("Service end time is required.",
                                new[] { "EncounterEndTime" }));
                            ret = false;
                        }

                        if (Encounter.EncounterEndDate.GetValueOrDefault().DateTime.Date > DateTime.Now.Date)
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult(
                                "Service end date cannot be in the future.", new[] { "EncounterEndDate" }));
                            ret = false;
                        }

                        if (Encounter.EncounterActualTime > 0)
                        {
                            if (Encounter.EncounterActualTime > 1440)
                            {
                                Encounter.ValidationErrors.Add(new ValidationResult(
                                    "Total Minutes cannot exceed 24 hours.", new[] { "EncounterActualTime" }));
                                ret = false;
                            }
                        }
                        else
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult("Total Minutes must be greater than 0.",
                                new[] { "EncounterActualTime" }));
                            if (Encounter.EncounterActualTime < 0)
                            {
                                Encounter.ValidationErrors.Add(new ValidationResult(
                                    "Service date must be greater than service end date.",
                                    new[] { "EncounterStartDate" }));
                            }

                            ret = false;
                        }
                    }

                    return ret;
                }

                if (Question.DataTemplate.Equals("PlaceofService"))
                {
                    var ret = true;
                    if (Encounter.FullValidationNTUC)
                    {
                        if (Encounter.PatientAddressKey.HasValue == false || (Encounter.PatientAddressKey.HasValue &&
                                                                              Encounter.PatientAddressKey.Value <= 0))
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult(
                                (string.Format("{0} is required", Label)), new[] { "PatientAddressKey" }));
                            ret = false;
                        }
                    }

                    return ret;
                }

                if (Question.DataTemplate.Equals("Distance"))
                {
                    var ret = true;
                    if (Encounter.FullValidationNTUC)
                    {
                        if (string.IsNullOrEmpty(Encounter.DistanceScale))
                        {
                            Encounter.DistanceScale = TenantSettingsCache.Current.TenantSettingDistanceTraveledMeasure;
                        }

                        if (Encounter.Distance.HasValue == false || string.IsNullOrEmpty(Encounter.DistanceScale))
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult(
                                (string.Format("{0} and scale are required", Label)),
                                new[] { "Distance", "DistanceScale" }));
                            ret = false;
                        }
                    }

                    return ret;
                }

                if (Question.DataTemplate.Equals("ServiceTypeAttempted"))
                {
                    return true;
                }

                if (Question.DataTemplate.Equals("CodeLookupAttempted"))
                {
                    var ret = true;
                    if (EncounterAttempted != null)
                    {
                        EncounterAttempted.ValidationErrors.Clear();
                    }

                    if (Encounter.FullValidation)
                    {
                        if ((EncounterAttempted != null) && (EncounterAttempted.Reason.HasValue == false))
                        {
                            EncounterAttempted.ValidationErrors.Add(
                                new ValidationResult("Reason Attempted is a required field", new[] { "Reason" }));
                            ret = false;
                        }
                    }

                    return ret;
                }

                if (Question.DataTemplate.Equals("Trauma"))
                {
                    if (!Admission.ValidateTrauma())
                    {
                        if (DynamicFormViewModel != null)
                        {
                            DynamicFormViewModel.ValidEnoughToSave = false;
                        }

                        return false;
                    }

                    if (Encounter.FullValidation)
                    {
                        Admission
                            .Validate(); //trigger other trauma related fields to validate, to signal if section in error

                        if ((Admission.HasTrauma.HasValue == false) && (Admission.HospiceAdmission == false))
                        {
                            //Manually add error validation for HasTrauma
                            Admission.ValidationErrors.Add(new ValidationResult("Trauma Y/N is required",
                                new[] { "HasTrauma" }));
                        }

                        var trauma_fields = new List<string>
                        {
                            "HasTrauma", "TraumaDate", "TraumaStateCode", "TraumaType"
                        };

                        var have_trauma_errors =
                            Admission.ValidationErrors.Where(e => e.MemberNames.Any(m => trauma_fields.Contains(m)));
                        var error_count = have_trauma_errors.Count();

                        return (error_count > 0) ? false : true;
                    }

                    return true;
                }

                if (DynamicFormViewModel.CurrentForm.IsDischarge && Question.DataTemplate.Equals("DischargeDate"))
                {
                    return DynamicModelValidations.ValidateDischargeDate(AdmissionDiscipline);
                }

                if (DynamicFormViewModel.CurrentForm.IsTransfer && Question.DataTemplate.Equals("DischargeDate") &&
                    (Encounter.EncounterTransfer.FirstOrDefault() != null)
                    && Encounter.EncounterTransfer.FirstOrDefault().TransferDate != null)
                {
                    EncounterTransfer et = Encounter.EncounterTransfer.FirstOrDefault();
                    if ((et.TransferDate != minSQLDateTime) && (et.TransferDate == DateTime.MinValue))
                    {
                        et.TransferDate = minSQLDateTime;
                    }

                    if (et.TransferDate == minSQLDateTime)
                    {
                        string[] memberNames = { "TransferDate" };
                        et.ValidationErrors.Add(new ValidationResult("The Transfer Date field is required.",
                            memberNames));
                        DynamicFormViewModel.ValidEnoughToSave = false;
                        return false;
                    }

                    if ((et.TransferDate != minSQLDateTime) && et.TransferReasonKey <= 0)
                    {
                        string[] memberNames = { "TransferReasonKey" };
                        et.ValidationErrors.Add(new ValidationResult(
                            "You must choose a transfer reason in order to transfer a discipline.", memberNames));
                        return false;
                    }

                    if (et.TransferDate != minSQLDateTime && AdmissionDiscipline.DisciplineAdmitDateTime.HasValue
                                                          && et.TransferDate.Date < AdmissionDiscipline
                                                              .DisciplineAdmitDateTime.Value.Date)
                    {
                        string[] memberNames = { "TransferDate" };
                        et.ValidationErrors.Add(new ValidationResult("Transfer Date cannot be before the Admit Date.",
                            memberNames));
                        return false;
                    }

                    return true;
                }

                if (DynamicFormViewModel.CurrentForm.IsDischarge &&
                    (Question.DataTemplate.Equals("DischargeReason") ||
                     Question.DataTemplate.Equals("DischargeReasonHomeHealth")) &&
                    AdmissionDiscipline.DischargeReasonKey.HasValue)
                {
                    return Validate_DischargeReasonKey();
                }

                if (DynamicFormViewModel.CurrentForm.IsTransfer && Question.DataTemplate.Equals("DischargeDate") &&
                    DynamicFormViewModel.CurrentEncounterTransfer.TransferDate != null)
                {
                    return true;
                }

                if (DynamicFormViewModel.CurrentForm.IsTransfer &&
                    (Question.DataTemplate.Equals("DischargeReason") ||
                     Question.DataTemplate.Equals("DischargeReasonHomeHealth")) &&
                    DynamicFormViewModel.CurrentEncounterTransfer.TransferReasonKey > 0)
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(EncounterData.TextData) ||
                    ((!string.IsNullOrEmpty(EncounterData.Text2Data)) &&
                     (Question.DataTemplate.Equals("MidArmCircumference"))) || EncounterData.IntData.HasValue ||
                    EncounterData.BoolData.HasValue || EncounterData.DateTimeData.HasValue ||
                    EncounterData.RealData.HasValue ||
                    (EncounterData.SignatureData != null) || !string.IsNullOrEmpty(EncounterData.FuncDeficit) ||
                    EncounterData.ReEvaluateFormSectionKey.HasValue)
                {
                    if (Question.DataTemplate.Equals("Surface"))
                    {
                        if (EncounterData.Int2Data.HasValue && (EncounterData.IntData < 1 ||
                                                                CodeLookupCache.GetCodeDescriptionFromKey(
                                                                    Question.LookupType, EncounterData.IntData.Value) !=
                                                                "Stairs"))
                        {
                            EncounterData.Int2Data = null;
                        }
                    }
                    else if (Question.DataTemplate.StartsWith("PTLevelofAssist"))
                    {
                        if (!EncounterData.IntData.HasValue ||
                            CodeLookupCache.GetCodeFromKey(Question.LookupType, EncounterData.IntData.Value) ==
                            "Independent")
                        {
                            EncounterData.TextData = string.Empty;
                            EncounterData.Text2Data = string.Empty;
                            EncounterData.Text3Data = string.Empty;
                            EncounterData.Text4Data = string.Empty;
                            EncounterData.GuidData = null;
                            EncounterData.FuncDeficit = string.Empty;
                        }
                        else if (CodeLookupCache.GetCodeFromKey(Question.LookupType, EncounterData.IntData.Value) !=
                                 "Independent" && string.IsNullOrEmpty(EncounterData.TextData))
                        {
                            EncounterData.ValidationErrors.Add(new ValidationResult(
                                (string.Format("{0} requires Continuous or Intermittent to be valued", Label)),
                                new[] { "TextData" }));
                            return false;
                        }
                    }
                    else if (Question.DataTemplate.StartsWith("OTLevelofAssist"))
                    {
                        if (!EncounterData.IntData.HasValue ||
                            CodeLookupCache.GetCodeFromKey(Question.LookupType, EncounterData.IntData.Value) ==
                            "Independent")
                        {
                            EncounterData.TextData = string.Empty;
                            EncounterData.Text2Data = string.Empty;
                            EncounterData.Text3Data = string.Empty;
                            EncounterData.Text4Data = string.Empty;
                            EncounterData.GuidData = null;
                        }

                        else if (EncounterData.IntData.HasValue &&
                                 CodeLookupCache.GetCodeFromKey(Question.LookupType, EncounterData.IntData.Value) !=
                                 "Independent")
                        {
                            bool AllValid = true;

                            if (string.IsNullOrEmpty(EncounterData.TextData))
                            {
                                EncounterData.ValidationErrors.Add(new ValidationResult(
                                    (string.Format("{0} requires Continuous or Intermittent to be valued", Label)),
                                    new[] { "TextData" }));
                                AllValid = false;
                            }

                            if (string.IsNullOrEmpty(EncounterData.Text2Data))
                            {
                                EncounterData.ValidationErrors.Add(new ValidationResult(
                                    (string.Format("{0} requires Reason Assistance Needed to be valued", Label)),
                                    new[] { "Text2Data" }));
                                AllValid = false;
                            }

                            if (string.IsNullOrEmpty(EncounterData.Text3Data))
                            {
                                EncounterData.ValidationErrors.Add(new ValidationResult(
                                    (string.Format("{0} requires Explain Assistance Needed to be valued", Label)),
                                    new[] { "Text3Data" }));
                                AllValid = false;
                            }

                            if (!AllValid)
                            {
                                return AllValid;
                            }
                        }
                    }
                    else if (Question.DataTemplate.Equals("TextandDate"))
                    {
                        if (string.IsNullOrEmpty(EncounterData.TextData) || !EncounterData.DateTimeData.HasValue)
                        {
                            EncounterData.ValidationErrors.Add(new ValidationResult(
                                (string.Format("{0} requires Type and Last Change Date to be valued", Label)),
                                new[] { "TextData", "DateTimeData" }));
                            return false;
                        }
                    }
                    else if (Question.DataTemplate.Equals("Oxygen"))
                    {
                        if (EncounterData.TextData != "1")
                        {
                            EncounterData.Text2Data = null;
                            EncounterData.Text3Data = null;
                            EncounterData.Text4Data = null;
                            EncounterData.GuidData = null;
                        }
                    }
                    else if (Question.DataTemplate.Equals("NutritionRoute"))
                    {
                        if (EncounterData.TextData != "OtherEnteral")
                        {
                            EncounterData.Text2Data = null;
                            EncounterData.Text3Data = null;
                            EncounterData.Text4Data = null;
                        }

                        if (EncounterData.TextData == "OtherEnteral")
                        {
                            if (string.IsNullOrWhiteSpace(EncounterData.Text2Data))
                            {
                                EncounterData.ValidationErrors.Add(new ValidationResult(
                                    (string.Format("{0} of Other Enteral requires Type to be valued", Label)),
                                    new[] { "Text2Data", "Text2Data" }));
                                return false;
                            }
                        }
                    }
                    else if (Question.DataTemplate.Equals("Religion"))
                    {
                        if (Patient != null)
                        {
                            Patient.Religion = EncounterData.IntData; // copy form data to patient
                        }
                    }
                    else if ((Question.DataTemplate.Equals("HospiceChurch")) ||
                             (Question.DataTemplate.Equals("HospiceFuneral")))
                    {
                        if (EncounterData.IntData != 1)
                        {
                            EncounterData.TextData = null;
                            EncounterData.Text2Data = null;
                            EncounterData.Text3Data = null;
                            EncounterData.Text4Data = null;
                        }

                        if (EncounterData.IntData == 1)
                        {
                            if ((string.IsNullOrWhiteSpace(EncounterData.Text4Data) == false) &&
                                (Encounter.FullValidation))
                            {
                                string phone = EncounterData.Text4Data.Replace(".", "");
                                if ((phone.Length != 7) && (phone.Length != 10))
                                {
                                    EncounterData.ValidationErrors.Add(new ValidationResult(
                                        "Invalid phone number format, must be 999.9999 or 999.999.9999",
                                        new[] { "Text4Data" }));
                                    return false;
                                }
                            }
                        }
                    }
                    else if (Question.DataTemplate.Equals("HealthTest"))
                    {
                        if (EncounterData.IntData == 1 && (string.IsNullOrEmpty(EncounterData.TextData) ||
                                                           string.IsNullOrEmpty(EncounterData.Text2Data)))
                        {
                            EncounterData.ValidationErrors.Add(new ValidationResult(
                                (string.Format("{0} requires Test and Result to be valued", Label)),
                                new[] { "TextData", "Text2Data" }));
                            return false;
                        }
                    }
                    else if (Question.DataTemplate.StartsWith("YesNoWideLabel"))
                    {
                        // clear 'On file' if question was answered no
                        if (EncounterData.TextData == "0")
                        {
                            EncounterData.Int2Data = null;
                        }
                    }
                    else if (Question.Label.Equals("Date of Last BM"))
                    {
                        if (EncounterData.DateTimeData != null)
                        {
                            if (((DateTime)EncounterData.DateTimeData).Date > DateTime.Today)
                            {
                                EncounterData.ValidationErrors.Add(
                                    new ValidationResult("Date of Last BM cannot be a future date",
                                        new[] { "DateTimeData" }));
                                return false;
                            }
                        }
                    }
                    else if (Question.DataTemplate.Equals("CodeLookupTMApprovedRadioHorizontal"))
                    {
                        var ret = true;
                        if (string.IsNullOrEmpty(EncounterData.TextData))
                        {
                            EncounterData.ValidationErrors.Add(
                                new ValidationResult("Plan of Care Reviewed is a required field",
                                    new[] { "TextData" }));
                            ret = false;
                        }

                        if ((string.IsNullOrEmpty(EncounterData.TextData) == false) &&
                            (EncounterData.TextData == "ApprovedWithChanges") &&
                            string.IsNullOrEmpty(EncounterData.Text2Data))
                        {
                            EncounterData.ValidationErrors.Add(
                                new ValidationResult("Care Plan Changes is a required field", new[] { "Text2Data" }));
                            ret = false;
                        }

                        if ((string.IsNullOrEmpty(EncounterData.TextData) == false) &&
                            (EncounterData.TextData == "Approved") &&
                            (string.IsNullOrEmpty(EncounterData.Text2Data) == false))
                        {
                            EncounterData.Text2Data = null;
                        }

                        if (ret == false)
                        {
                            return ret;
                        }
                    }

                    if (EncounterData.IsNew)
                    {
                        Encounter.EncounterData.Add(EncounterData);
                    }

                    return true;
                }

                if (((Required && Encounter.FullValidation) || ConditionalRequired) && !Hidden &&
                    !Protected) // no use to require if it is protected or hidden
                {
                    if (Question.DataTemplate.Equals("ServiceType"))
                    {
                        if (Encounter.FullValidationNTUC)
                        {
                            Encounter.ValidationErrors.Add(
                                new ValidationResult((string.Format("{0} is required", Label)),
                                    new[] { "ServiceTypeKey" }));
                        }
                    }
                    else if (Question.DataTemplate.Equals("ServiceDate"))
                    {
                        if (Encounter.FullValidationNTUC)
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult(
                                (string.Format("{0} is required", Label)), new[] { "EncounterStartDate" }));
                        }
                    }
                    else if (Question.DataTemplate.Equals("ServiceEndTime"))
                    {
                        if (Encounter.FullValidationNTUC)
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult(
                                (string.Format("{0} is required", Label)), new[] { "EncounterEndDate" }));
                        }
                    }
                    else if (Question.DataTemplate.Equals("PlaceofService"))
                    {
                        if (Encounter.FullValidationNTUC)
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult(
                                (string.Format("{0} is required", Label)), new[] { "PatientAddressKey" }));
                        }
                    }
                    else if (Question.DataTemplate.Equals("Distance"))
                    {
                        if (Encounter.FullValidationNTUC)
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult(
                                (string.Format("{0} and scale are required", Label)),
                                new[] { "Distance", "DistanceScale" }));
                        }
                    }
                    else if (Question.DataTemplate.Equals("SignatureData"))
                    {
                        EncounterData.ValidationErrors.Add(
                            new ValidationResult((string.Format("{0} is required", Label)), new[] { "SignatureData" }));
                    }
                    else if (DynamicFormViewModel.CurrentForm.IsDischarge &&
                             Question.DataTemplate.Equals("DischargeDate"))
                    {
                        AdmissionDiscipline.ValidationErrors.Add(
                            new ValidationResult((string.Format("{0} is required", Label)),
                                new[] { "DischargeDateTime" }));
                    }
                    else if (DynamicFormViewModel.CurrentForm.IsDischarge &&
                             (Question.DataTemplate.Equals("DischargeReason") ||
                              Question.DataTemplate.Equals("DischargeReasonHomeHealth")))
                    {
                        AdmissionDiscipline.ValidationErrors.Add(
                            new ValidationResult((string.Format("{0} is required", Label)),
                                new[] { "DischargeReasonKey" }));
                        Patient.DeathDate = null;
                    }

                    else if (DynamicFormViewModel.CurrentForm.IsTransfer &&
                             Question.DataTemplate.Equals("DischargeDate"))
                    {
                        DynamicFormViewModel.CurrentEncounterTransfer.ValidationErrors.Add(
                            new ValidationResult((string.Format("{0} is required", Label)), new[] { "TransferDate" }));
                    }
                    else if (DynamicFormViewModel.CurrentForm.IsTransfer &&
                             (Question.DataTemplate.Equals("DischargeReason") ||
                              Question.DataTemplate.Equals("DischargeReasonHomeHealth")))
                    {
                        DynamicFormViewModel.CurrentEncounterTransfer.ValidationErrors.Add(
                            new ValidationResult((string.Format("{0} is required", Label)),
                                new[] { "TransferReasonKey" }));
                    }
                    else if (Question.DataTemplate.Equals("DateUnknown"))
                    {
                        if ((EncounterData.DateTimeData == null) && (EncounterData.BoolData != true) &&
                            (Hidden == false) && (Protected == false))
                        {
                            EncounterData.ValidationErrors.Add(new ValidationResult(
                                (string.Format("{0} is required or Unknown must be checked", Label)),
                                new[] { "DateTimeData", "BoolData" }));
                            return false;
                        }

                        return true;
                    }
                    else
                    {
                        if ((Encounter.FullValidation) && (Question.DataTemplate.Equals("GroupLabel") == false))
                        {
                            EncounterData.ValidationErrors.Add(new ValidationResult(
                                (string.Format("{0} is required", Label)),
                                new[]
                                {
                                    "TextData", "IntData", "DateTimeData", "BoolData", "RealData", "SignatureData"
                                }));
                            return false;
                        }

                        return true; // NTUC case - generic EncounterData is not required
                    }

                    return false;
                }

                if (EncounterData.EntityState == EntityState.Modified)
                {
                    Encounter.EncounterData.Remove(EncounterData);
                    EncounterData = new EncounterData
                    {
                        SectionKey = EncounterData.SectionKey, QuestionGroupKey = EncounterData.QuestionGroupKey,
                        QuestionKey = EncounterData.QuestionKey
                    };
                }

                return true;
            }
            catch (Exception e)
            {
                string message = "QuestionBase.Validate Exception: " + e.Message + ": ";
                if (Question == null)
                {
                    message = message + ", Question Null";
                }
                else
                {
                    message = message + ", Question QuestionKey=" + Question.QuestionKey;
                    message = message + ", Question DataTemplate=" +
                              ((Question.DataTemplate == null) ? null : Question.DataTemplate);
                    message = message + ", Question Label=" + ((Question.Label == null) ? null : Question.Label);
                }

                if (Encounter == null)
                {
                    message = message + ", Encounter Null";
                }
                else
                {
                    message = message + ", Encounter EncounterKey=" + Encounter.EncounterKey;
                    message = message + ", Encounter EncounterActualTime=" + ((Encounter.EncounterActualTime == null)
                        ? null
                        : Encounter.EncounterActualTime.ToString());
                    message = message + ", Encounter Distance=" +
                              ((Encounter.Distance == null) ? null : Encounter.Distance.ToString());
                    message = message + ", Encounter DistanceScale=" +
                              ((Encounter.DistanceScale == null) ? null : Encounter.DistanceScale);
                }

                if (EncounterData == null)
                {
                    message = message + ", EncounterData Null";
                }
                else
                {
                    message = message + ", EncounterData EncounterDataKey=" + EncounterData.EncounterDataKey;
                    message = message + ", EncounterData DateTimeData=" + ((EncounterData.DateTimeData == null)
                        ? null
                        : EncounterData.DateTimeData.ToString());
                    message = message + ", EncounterData TextData=" +
                              ((EncounterData.TextData == null) ? null : EncounterData.TextData);
                }

                if (Admission == null)
                {
                    message = message + ", Admission Null";
                }
                else
                {
                    message = message + ", Admission AdmissionKey=" + Admission.AdmissionKey;
                }

                if (AdmissionDiscipline == null)
                {
                    message = message + ", AdmissionDiscipline Null";
                }
                else
                {
                    message = message + ", AdmissionDiscipline AdmissionDisciplineKey=" +
                              AdmissionDiscipline.AdmissionDisciplineKey;
                }

                if (DynamicFormViewModel == null)
                {
                    message = message + ", DynamicFormViewModel Null";
                }
                else
                {
                    message = message + ", DynamicFormViewModel Not Null";
                    if (DynamicFormViewModel.CurrentForm == null)
                    {
                        message = message + ", DynamicFormViewModel.CurrentForm Null";
                    }
                    else
                    {
                        message = message + ", DynamicFormViewModel.CurrentForm Not Null";
                    }
                }

                throw new Exception(message);
            }
        }

        public void Encounter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ServiceTypeKey")
            {
                if (Encounter == null)
                {
                    return;
                }

                Messenger.Default.Send(
                    new ServiceTypeKeyChangedEvent(
                        Encounter.TaskKey.GetValueOrDefault(),
                        Encounter.EncounterKey,
                        Encounter.ServiceTypeKey.GetValueOrDefault()),
                    Constants.DomainEvents.ServiceTypeKeyChanged);
            }
        }

        public void EncounterData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("TextData"))
            {
                if ((Question.DataTemplate.Equals("CodeLookupRadioHorizontal")) ||
                    (Question.DataTemplate.Equals("CodeLookupTMApprovedRadioHorizontal")) ||
                    (Question.DataTemplate.Equals("YesNo")) ||
                    (Question.DataTemplate.Equals("YesNoWideLabel")) ||
                    (Question.DataTemplate.Equals("YesNoWideLabelWithOnFile")) ||
                    (Question.DataTemplate.Equals("HospiceElectionStatement")) ||
                    (Question.DataTemplate.Equals("Oxygen")) ||
                    (Question.DataTemplate.Equals("YesNoInfoNoComment")))
                {
                    if (QuestionValueChanged != null)
                    {
                        QuestionValueChanged.Execute(sender);
                    }
                }
            }

            if (e.PropertyName.Equals("IntData"))
            {
                if (Question.DataTemplate.Equals("Equipment"))
                {
                    RaisePropertyChanged("EquipmentFromQuestions");
                }

                if (Question.DataTemplate.Equals("BradenScore") && OasisManager != null)
                {
                    OasisManager.BradenScoreOasisMappingChanged(Question, EncounterData);
                }
            }

            if (e.PropertyName.Equals("DateTimeData") && Question.DataTemplate.Equals("DateUnknown"))
            {
                if (EncounterData.DateTimeData.HasValue)
                {
                    EncounterData.BoolData = false;
                }
            }
            else if (e.PropertyName.Equals("BoolData") && Question.DataTemplate.Equals("DateUnknown"))
            {
                if (EncounterData.BoolData.HasValue && EncounterData.BoolData.Value)
                {
                    EncounterData.DateTimeData = null;
                }
            }

            if ((Question != null && Question.QuestionOasisMapping != null) && Question.QuestionOasisMapping.Any() &&
                (OasisManager != null))
            {
                if ((e.PropertyName.Equals("BoolData")) ||
                    (e.PropertyName.Equals("Bool2Data")) ||
                    (e.PropertyName.Equals("Bool3Data")) ||
                    (e.PropertyName.Equals("DateTimeData")) ||
                    (e.PropertyName.Equals("IntData")) ||
                    (e.PropertyName.Equals("Int2Data")) ||
                    (e.PropertyName.Equals("TextData")) ||
                    (e.PropertyName.Equals("Text2Data")) ||
                    (e.PropertyName.Equals("Text3Data")) ||
                    (e.PropertyName.Equals("Text4Data")) ||
                    (e.PropertyName.Equals("RealData")))
                {
                    OasisManager.QuestionOasisMappingChanged(Question, EncounterData);
                }
            }

            if (OasisManager != null)
            {
                OasisManager.HISQuestionChanged(Question, EncounterData);
            }
        }

        public IEnumerable<Equipment> EquipmentFromQuestions
        {
            get
            {
                //Question is not maintained in factory but QuestionKey is
                var tmpEquip = EquipmentCache.GetEquipment()
                    .Where(eq => eq.ItemCode.ToLower() != "none" && Encounter.EncounterData
                        .Where(e => e.QuestionKey.HasValue && DynamicFormCache.GetQuestionByKey(e.QuestionKey.Value).DataTemplate == "Equipment")
                        .Any(equ => equ.IntData == eq.EquipmentKey));
                return tmpEquip;
            }
        }

        public virtual void ApplyDefaults()
        {
            if (EncounterData == null)
            {
                return;
            }

            if (Question.DataTemplate.StartsWith("Religion"))
            {
                if ((Patient != null) && (EncounterData != null))
                {
                    EncounterData.IntData = Patient.Religion;
                }

                return;
            }

            if (Question.DataTemplate.Equals("Date"))
            {
                if ((EncounterData != null) && (string.IsNullOrWhiteSpace(Question.SubLookupType) == false))
                {
                    if (Question.SubLookupType.ToLower().Equals("today"))
                    {
                        EncounterData.DateTimeData = DateTime.Today;
                    }
                    else if (Question.SubLookupType.ToLower().Equals("now"))
                    {
                        EncounterData.DateTimeData = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                    }
                }

                return;
            }

            if (Question.DataTemplate.Equals("Text"))
            {
                if (Question.Label.Equals("Disciplines Ordered on Referral") && (Admission != null))
                {
                    ICollection<AdmissionDiscipline> adList = Admission.ActiveAdmissionDisciplinesReferred;
                    string text = "";
                    if (adList != null)
                    {
                        foreach (AdmissionDiscipline ad in adList)
                            text = text + ((string.IsNullOrEmpty(text)) ? "" : ",  ") + ad.DisciplineDescription;
                    }

                    EncounterData.TextData = text;
                }
                else if ((string.IsNullOrWhiteSpace(Question.Label) == false) &&
                         (Question.Label.StartsWith("Summary of Care Narrative")))
                {
                    ApplyDischargeSummaryOfCareNarrativeDefault();
                }

                return;
            }

            if (Question.DataTemplate.StartsWith("YesNoWideLabel"))
            {
                // apply any default - default type is in q.SubLookupType
                if (string.IsNullOrWhiteSpace(Question.SubLookupType) == false)
                {
                    if ((Question.SubLookupType.ToLower().Equals("patientcontactproxy")) && (Patient != null))
                    {
                        if (Patient.PatientContact != null)
                        {
                            // There are only 2 roles: 'Health Care Proxy' and 'Power of Attorney' - if either is speficied default the question to yes
                            EncounterData.TextData =
                                (Patient.PatientContact.Where(pc => ((pc.Inactive == false) && (pc.Role != null)))
                                    .Any() == false)
                                    ? null
                                    : "1";
                        }
                    }
                    else if ((Question.SubLookupType.ToLower().Equals("patientdnr")) && (Patient != null))
                    {
                        // if any active DNRs default the question to yes
                        EncounterData.TextData = (Patient.ActiveDNRs == null) ? null : "1";
                    }
                }
            }
        }

        private void ApplyDischargeSummaryOfCareNarrativeDefault()
        {
            // for discipline discharge - the text is populated from the most recent Clinical Visit Summary from the last visit from this discharged discipline (HCFA code)
            // for agency discharge - the text is populated from the most recent Clinical Visit Summary from the last visit from the last skilled discipline (HCFA code A,B,C or D)
            if ((Admission == null) || (Admission.AdmissionDiscipline == null) || (AdmissionDiscipline == null))
            {
                return;
            }

            string HCFACodes = AdmissionDiscipline.AdmissionDisciplineHCFACode;
            if (HCFACodes == null)
            {
                HCFACodes = "";
            }

            List<Encounter> eList = Admission.GetEncounters(DateTime.MinValue, null);
            if (eList == null)
            {
                return;
            }

            // Note - there are two flavors of a 'Clinical Visit Summary'
            int questionkey1 = DynamicFormCache.GetQuestionKeyByLabelAndDataTemplate("clinical visit summary", "text");
            int questionkey2 =
                DynamicFormCache.GetQuestionKeyByLabelAndDataTemplate("clinical visit summary", "textwithinfoicon");
            foreach (Encounter e in eList)
            {
                EncounterData edcvs = e.EncounterData.FirstOrDefault(ed => (ed.IsClincialVisitSummary(questionkey1) || ed.IsClincialVisitSummary(questionkey2)));
                if ((edcvs != null) && (string.IsNullOrWhiteSpace(edcvs.TextData) == false))
                {
                    AdmissionDiscipline ead = Admission.AdmissionDiscipline.FirstOrDefault(p => p.AdmissionDisciplineKey == e.AdmissionDisciplineKey);
                    if (ead != null && ead.AdmissionDisciplineHCFACode != null && (HCFACodes.Contains(ead.AdmissionDisciplineHCFACode)))
                    {
                        EncounterData.TextData = edcvs.TextData;
                        return;
                    }
                }
            }
        }

        public string SignatureLabel
        {
            get
            {
                if (((Label.ToLower() == "patient") || (Label.ToLower() == "patient signature")))
                {
                    return Patient?.FullName;
                }

                if (((Label.ToLower() == "clinician") || (Label.ToLower() == "clinician signature")))
                {
                    return (Encounter == null)
                        ? null
                        : UserCache.Current.GetFormalNameFromUserId(Encounter.EncounterBy);
                }

                if (((Label.ToLower() == "medical director signature") || (Label.ToLower() == "medical director")))
                {
                    return GetMedicalDirectorName;
                }

                return null;
            }
        }

        private string GetMedicalDirectorName
        {
            get
            {
                string mdName = "Medical Director / Hospice Physician unknown";
                if ((Encounter == null) || (Admission == null))
                {
                    return mdName;
                }

                bool useThisEncounter =
                    (Encounter.EncounterStatus == (int)EncounterStatusType.Completed) ? true : false;
                AdmissionPhysicianFacade ap = new AdmissionPhysicianFacade(useThisEncounter);
                ap.Admission = Admission;
                ap.Encounter = Encounter;
                AdmissionPhysician apMedicalDirector = ap.ActiveAdmissionPhysicians
                    .FirstOrDefault(p => (p.PhysicianType == CodeLookupCache.GetKeyFromCode("PHTP", "MedDirect")));
                if (apMedicalDirector == null)
                {
                    return mdName;
                }

                return PhysicianCache.Current.GetPhysicianFullNameInformalWithSuffixFromKey(apMedicalDirector
                    .PhysicianKey);
            }
        }

        private readonly string[] NumericDelimiter = { "|" };

        public string NumericFormat
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Question.SubLookupType))
                {
                    return null;
                }

                string[] pieces = Question.SubLookupType.Split(NumericDelimiter, StringSplitOptions.None);
                if (pieces.Length < 1)
                {
                    return null;
                }

                return pieces[0];
            }
        }

        private readonly int MAX_NumericWidth = 300;
        private readonly int MAX_CharWidth = 15;

        public int NumericWidth
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Question.SubLookupType))
                {
                    return MAX_NumericWidth;
                }

                string[] pieces = Question.SubLookupType.Split(NumericDelimiter, StringSplitOptions.None);
                if (pieces.Length < 1)
                {
                    return MAX_NumericWidth;
                }

                return ((pieces[0].Length * MAX_CharWidth) > MAX_NumericWidth)
                    ? MAX_NumericWidth
                    : pieces[0].Length * MAX_CharWidth;
            }
        }

        public string NumericLabelSuffix
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Question.SubLookupType))
                {
                    return null;
                }

                string[] pieces = Question.SubLookupType.Split(NumericDelimiter, StringSplitOptions.None);
                if (pieces.Length < 2)
                {
                    return null;
                }

                return pieces[1];
            }
        }

        public bool IsFormIsVisitTeleMonitoring
        {
            get
            {
                if (DynamicFormViewModel == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm == null)
                {
                    return false;
                }

                return (DynamicFormViewModel.CurrentForm.IsVisitTeleMonitoring) ? true : false;
            }
        }

        public string NumericInfo
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Question.SubLookupType))
                {
                    return null;
                }

                string[] pieces = Question.SubLookupType.Split(NumericDelimiter, StringSplitOptions.None);
                if (pieces.Length < 3)
                {
                    return null;
                }

                return pieces[2];
            }
        }

        public OrderEntryManager OrderEntryManager { get; set; }

        public bool? SetupMedicalPrognosisHiddenOverride()
        {
            if ((Question == null) || (Admission == null))
            {
                return null;
            }

            if (((Question.Label == "Medical Prognosis") && (Question.DataTemplate == "CodeLookup") &&
                 (Question.BackingFactory == "QuestionBase")) == false)
            {
                return null;
            }

            // Hide if not homehealth or homecare
            if (Admission.IsHomeHealth)
            {
                return null;
            }

            if (Admission.IsHomeCare)
            {
                return null;
            }

            return true;
        }

        public bool? SetupURLHiddenOverride()
        {
            if (Question == null)
            {
                return null;
            }

            if (((Question.DataTemplate == "URL") && (Question.BackingFactory == "QuestionBase")) == false)
            {
                return null;
            }

            // Hide if no URL
            if (string.IsNullOrWhiteSpace(Question.LookupType) == false)
            {
                return null;
            }

            return true;
        }

        public bool? SetupOrderEntryProtectedOverrideRunTime()
        {
            // If not an order - do not override Protection (VO orders don't count)
            if (OrderEntryManager == null)
            {
                return null;
            }

            if (OrderEntryManager.IsVO)
            {
                return null;
            }

            if (Encounter == null)
            {
                return null;
            }

            if (Encounter.EncounterIsOrderEntry == false)
            {
                return null;
            }

            // Everything is protected on inactive forms
            if (Encounter.Inactive)
            {
                return true;
            }

            if (OrderEntryManager.CurrentOrderEntry == null)
            {
                return true;
            }

            // the clinician who 'owns' the order can edit it if its in an edit state (and not voided)
            if ((Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                (Encounter.EncounterStatus == (int)EncounterStatusType.Edit))
            {
                return (OrderEntryManager.CurrentOrderEntry.OrderStatus == (int)OrderStatusType.Voided) ? true : false;
            }

            // anyone with OrderEntry role when the form is in orderentry review
            return (OrderEntryManager.CurrentOrderEntry.CanEditOrderReviewed) ? false : true;
        }


        public bool? SetupServiceDateProtectedOverrideRunTime()
        {
            // Must allow the  System Administrator role to change a service date, service start time, 
            // service end date and service end time for a completed encounter.
            if (Encounter == null)
            {
                return null;
            }

            // Everything is protected on inactive forms
            if (Encounter.Inactive)
            {
                return true;
            }

            var serviceTemplates = new List<string> { "ServiceDate", "ServiceEndTime", "PlaceofService" };
            if (serviceTemplates.Any(st => Question.DataTemplate.Equals(st)))
            {
                if (RoleAccessHelper.CheckPermission(RoleAccess.Admin, false))
                {
                    return false;
                }
            }

            return null;
        }

        public bool? ProtectDischargeDateOverrideRunTimeIndr()
        {
            // Only System administrator - Edit of a discipline discharge form and change of the discharge date will 
            // update the discipline discharge date (as it displays in the Patient | Admission | Services screen) 

            if (DynamicFormViewModel.CurrentForm != null && !DynamicFormViewModel.CurrentForm.IsDischarge)
            {
                return null;
            }

            if (!(Question.DataTemplate.Equals("DischargeDate") || Question.DataTemplate.Equals("DischargeReason") ||
                  Question.DataTemplate.Equals("DischargeReasonHomeHealth")))
            {
                return null;
            }

            if (!RoleAccessHelper.CheckPermission(RoleAccess.Admin, needtolookup: false))
            {
                return null;
            }

            //If this is the IsDischarge form 
            // and if this is the DischargeDate or DischargeReason control 
            // and the operator has a System Administrator role 
            // then remove edit protection from the Discarge Date control
            // by overriding the controls' Runtime Protection indicator
            return false;
        }
    }

    public class QuestionBaseFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            QuestionBase qb = new QuestionBase(__FormSectionQuestionKey)
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
                OrderEntryManager = vm.CurrentOrderEntryManager,
            };
            if (qb.HiddenOverride == null)
            {
                qb.HiddenOverride = qb.SetupMedicalPrognosisHiddenOverride();
            }

            if (qb.HiddenOverride == null)
            {
                qb.HiddenOverride = qb.SetupURLHiddenOverride();
            }

            if (qb.ProtectedOverrideRunTime == null)
            {
                qb.ProtectedOverrideRunTime = qb.SetupOrderEntryProtectedOverrideRunTime();
            }

            if (qb.ProtectedOverrideRunTime == null)
            {
                qb.ProtectedOverrideRunTime = qb.SetupServiceDateProtectedOverrideRunTime();
            }

            if (qb.ProtectedOverrideRunTime == null)
            {
                qb.ProtectedOverrideRunTime = qb.ProtectDischargeDateOverrideRunTimeIndr();
            }

            // Override default protection on ICD fields - allowing users with the ICDCoder role to edit them if the encounter is in CodeReview state
            if ((q.DataTemplate == "SingleDiagnosis") && (vm.CurrentEncounter != null) &&
                (vm.CurrentEncounter.Inactive == false))
            {
                if ((vm.CurrentEncounter.EncounterStatus == (int)EncounterStatusType.CoderReview) &&
                    RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false))
                {
                    qb.ProtectedOverrideRunTime = false;
                }

                if (vm.CurrentEncounter.UserIsPOCOrderReviewerAndInPOCOrderReview)
                {
                    qb.ProtectedOverrideRunTime = false;
                }
            }

            EncounterData ed = vm.CurrentEncounter.EncounterData
                .FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey);
            if ((ed == null)
                || (qb.Encounter.EncounterIsPlanOfCare
                    && (qb.Encounter.EncounterStatus == (int)EncounterStatusType.Edit)
                    && copyforward
                    && qb.DynamicFormViewModel != null && qb.DynamicFormViewModel.RefreshCopyForwardData
                )
               )
            {
                if (qb.Encounter.EncounterIsPlanOfCare
                    && (qb.Encounter.EncounterStatus == (int)EncounterStatusType.Edit)
                    && (copyforward)
                    && (ed != null)
                   )
                {
                    vm.FormModel.Remove(ed);
                }

                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
                qb.EncounterData = ed;
                qb.ApplyDefaults();

                if ((copyforward) && ((qb.Encounter.IsNew) || ((qb.Encounter.EncounterIsPlanOfCare) &&
                                                               (qb.Encounter.EncounterStatus ==
                                                                (int)EncounterStatusType.Edit))))
                {
                    qb.CopyForwardLastInstance();
                }

                if ((qb.Encounter.IsNew) && ((q.Label == "Previous IDG Note") || (q.Label == "Previous IDT Note")) &&
                    (q.DataTemplate == "Text"))
                {
                    qb.CopyForwardLastTeamMeetingNote();
                }
            }
            else
            {
                qb.EncounterData = ed;
            }

            ed.PropertyChanged += qb.EncounterData_PropertyChanged;
            qb.Encounter.PropertyChanged += qb.Encounter_PropertyChanged;

            qb.Setup();

            return qb;
        }
    }

    public class QuestionWithManagerFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            QuestionWithManager qb = new QuestionWithManager(__FormSectionQuestionKey)
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
            };

            EncounterData ed = vm.CurrentEncounter.EncounterData
                .FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
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
            }

            ed.PropertyChanged += qb.EncounterData_PropertyChanged_Manager;
            qb.DynamicFormViewModel.MSPManager.RegisterQuestion(qb, formsection.FormSectionKey);
            return qb;
        }
    }

    public class QuestionWithManager : QuestionBase, IMSPQuestionaireManager
    {
        public QuestionWithManager(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void EncounterData_PropertyChanged_Manager(object sender, PropertyChangedEventArgs e)
        {
            if (DynamicFormViewModel?.MSPManager == null)
            {
                return;
            }

            if (e.PropertyName == "IsInvalid")
            {
                return;
            }

            DynamicFormViewModel.MSPManager.UpdateRegisteredQuestions(this);
        }

        public override String QuestionNotesOverride
        {
            get
            {
                if (DynamicFormViewModel?.MSPManager == null)
                {
                    return "";
                }

                if (DynamicFormViewModel.MSPManager.AttatchedFormDef == null)
                {
                    return "";
                }

                FormSectionQuestion fsq = null;
                FormSection formsect = null;
                if (Question != null && Question.FormSectionQuestion != null && Question.FormSectionQuestion.Count == 1)
                {
                    fsq = Question.FormSectionQuestion.FirstOrDefault();
                }

                if (fsq == null && DynamicFormViewModel.CurrentForm != null &&
                    DynamicFormViewModel.MSPManager.AttatchedFormDef.FormSection != null)
                {
                    formsect = DynamicFormViewModel.MSPManager.AttatchedFormDef.FormSection.FirstOrDefault(fs => fs.SectionKey == Section.SectionKey);
                    if (formsect != null && formsect.SortedFormSectionQuestion != null)
                    {
                        fsq = formsect.FormSectionQuestion.FirstOrDefault(f => f.QuestionKey == Question.QuestionKey);
                    }
                }

                if (fsq == null && formsect == null)
                {
                    return null;
                }

                var nte = fsq.GetQuestionAttributeForName("NOTE");
                if (nte == null)
                {
                    return null;
                }

                return nte.AttributeValue;
            }
        }

        int __cleanupCount;

        public override void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            if (EncounterData != null)
            {
                try
                {
                    EncounterData.PropertyChanged -= EncounterData_PropertyChanged_Manager;
                }
                catch
                {
                }
            }

            base.Cleanup();
        }
    }

    public class QuestionBaseDynamic : QuestionBase
    {
        public QuestionBaseDynamic(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void ApplyDefaults()
        {
            // LookupType examples: Patient.FullNameInformal, Patient.BirthDate, Admission.MRNdashAdmissionID, Patient.SSN, etc...
            if ((EncounterData == null) || (Question == null) || string.IsNullOrEmpty(Question.LookupType))
            {
                return;
            }

            if (Question.LookupType.Trim().StartsWith("DefaultProperty"))
            {
                ApplyDefaultFromProperty(Question.LookupType.Trim());
                return;
            }

            try
            {
                var parts = Question.LookupType.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                {
                    return;
                }

                if (parts[0].Equals("patient", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (Patient != null)
                    {
                        var nameProperty = Patient.GetType()
                            .GetProperties()
                            .FirstOrDefault(p => p.Name.Equals(parts[1], StringComparison.InvariantCultureIgnoreCase));
                        if (nameProperty != null)
                        {
                            var propValue = nameProperty.GetValue(Patient, null);
                            if (propValue != null)
                            {
                                EncounterData.TextData = propValue.ToString();
                            }
                        }
                    }

                    return;
                }

                if (parts[0].Equals("admission", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (Admission != null)
                    {
                        var nameProperty = Admission.GetType()
                            .GetProperties()
                            .FirstOrDefault(p => p.Name.Equals(parts[1], StringComparison.InvariantCultureIgnoreCase));
                        if (nameProperty != null)
                        {
                            var propValue = nameProperty.GetValue(Admission, null);
                            if (propValue != null)
                            {
                                EncounterData.TextData = propValue.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private bool ApplyDefaultFromProperty(string propertyName)
        {
            PropertyInfo property = GetType().GetProperty(propertyName);
            if (property == null)
            {
                MessageBox.Show(String.Format(
                    "Error: QuestionBaseDynamic.ApplyDefaultFromProperty: {0} is not defined.  Contact your system administrator.",
                    propertyName));
                return false;
            }

            bool status = false;
            try
            {
                status = (bool)property.GetValue(this, null);
            }
            catch
            {
            }

            return status;
        }

        public bool DefaultPropertyHEASummary
        {
            get
            {
                EncounterData.TextData = "No Election Addendum information available.";
                if ((Admission == null) || (Admission.Encounter == null) || (EncounterData == null))
                {
                    return false;
                }

                List<Encounter> eList =
                    Admission.Encounter.Where(e => ((e.HistoryKey == null) &&
                                                    (e.EncounterStatus == (int)EncounterStatusType.Completed) &&
                                                    (e.Inactive == false) &&
                                                    (e.EncounterHEADatedSignaturePresent != null))).ToList();
                if (eList.Count == 0)
                {
                    return true;
                }

                List<EncounterHospiceElectionAddendum> eheaList = new List<EncounterHospiceElectionAddendum>();
                foreach (Encounter e in eList)
                {
                    if (e.EncounterHEADatedSignaturePresent != null)
                    {
                        eheaList.Add(e.EncounterHEADatedSignaturePresent);
                    }
                }

                if (eheaList.Count == 0)
                {
                    return true;
                }

                eheaList = eheaList.OrderBy(e => e.DateFurnished != null).ToList();
                string text = string.Empty;
                foreach (EncounterHospiceElectionAddendum ehea in eheaList)
                {
                    if (text != string.Empty)
                    {
                        text = text + "<LineBreak/><LineBreak/>";
                    }

                    text = text + ((ehea.DatedSignaturePresent == true)
                        ? (string.Format(
                            "<Bold> Signature Furnished on {0} from {1}</Bold><LineBreak/>     Signature Date:  {2}",
                            ((ehea.DateFurnished == null) ? "??" : ehea.DateFurnished.Value.ToShortDateString()),
                            ehea.RequestedBy,
                            ((ehea.SignatureDate == null) ? "??" : ehea.SignatureDate.Value.ToShortDateString())))
                        : (string.Format(
                            "<Bold> No Signature Furnished on {0} from {1}</Bold><LineBreak/>     Reason Addendum not Signed:  {2}{3}",
                            ((ehea.DateFurnished == null) ? "??" : ehea.DateFurnished.Value.ToShortDateString()),
                            ehea.RequestedBy,
                            ehea.ReasonNotSignedDescription,
                            ((string.IsNullOrWhiteSpace(ehea.RefusalReason))
                                ? string.Empty
                                : ":  " + ehea.RefusalReason))));
                }

                EncounterData.TextData = text;
                return true;
            }
        }
    }

    public class QuestionBaseDynamicFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            QuestionBaseDynamic qb = new QuestionBaseDynamic(__FormSectionQuestionKey)
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
                OrderEntryManager = vm.CurrentOrderEntryManager,
            };

            EncounterData ed = vm.CurrentEncounter.EncounterData
                .FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey &&
                                     x.SectionKey == formsection.Section.SectionKey && x.QuestionGroupKey == qgkey &&
                                     x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                if (formsection.SectionKey != null)
                {
                    ed = new EncounterData
                    {
                        SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                    };
                }

                qb.EncounterData = ed;
            }
            else
            {
                qb.EncounterData = ed;
            }

            if (qb.Encounter.EncounterStatus == (int)EncounterStatusType.Edit && qb.DynamicFormViewModel != null)
            {
                qb.ApplyDefaults(); //refresh read-only defaults if still in EDIT mode
            }

            return qb;
        }
    }

    public class LoadedSectionDef
    {
        public int FormSectionKey;
        public ObservableCollection<SectionUI> SectionList;
    }

    public class SectionLabel : QuestionBase
    {
        public SectionLabel(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public bool AreEncounterReviews
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if (Encounter.EncounterStatus == (int)EncounterStatusType.Edit)
                {
                    return false;
                }

                if (Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return false;
                }

                return Label == "General" && Encounter.AreEncounterReviews;
            }
        }

        public bool IsEditablePOC
        {
            get
            {
                bool isEditablePOC = DynamicFormViewModel != null && DynamicFormViewModel.CurrentForm != null &&
                                     DynamicFormViewModel.CurrentForm.IsPlanOfCare
                                     && DynamicFormViewModel.CurrentForm.Description.Contains("Edit");

                return isEditablePOC;
            }
        }

        int __cleanupCount;

        public override void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            if (EncounterReview != null)
            {
                EncounterReview.PropertyChanged -= EncounterReview_PropertyChanged;
            }

            if (EncounterData != null)
            {
                try
                {
                    EncounterData.PropertyChanged -= EncounterData_PropertyChanged;
                }
                catch
                {
                }
            }

            if (Admission != null)
            {
                try
                {
                    Admission.PropertyChanged -= Admission_PropertyChanged;
                }
                catch
                {
                }
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

        public override void ClearEntity()
        {
            base.ClearEntity();
            SectionUI = null;
        }

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

                if ((string.IsNullOrWhiteSpace(_SectionLabel) == false) &&
                    (_SectionLabel.Trim().ToLower() == "60-day summary"))
                {
                    PageBreakBefore = true;
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

            SetupSectionHidden();
            if (Admission != null)
            {
                Admission.PropertyChanged += Admission_PropertyChanged;
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
            if (IsEncounterReviewSectionNoteEnabled == false)
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

        private void Admission_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("PatientInsuranceKey"))
            {
                SetupSectionHidden();
            }
        }

        private void SetupSectionHidden()
        {
            bool alwaysHideSection = DynamicFormViewModel?.HideSection(Section) ?? true;
            if (SectionUI != null)
            {
                SectionUI.AlwaysHideSection = alwaysHideSection;
            }

            ConditionalHidden = alwaysHideSection;
            HideSectionOverride = alwaysHideSection;
            HiddenOverride = alwaysHideSection;
            this.RaisePropertyChangedLambda(p => p.Hidden);
            if (DynamicFormViewModel != null)
            {
                DynamicFormViewModel.ProcessFilteredSections();
            }
        }
    }

    public class SectionLabelFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            SectionLabel sl = new SectionLabel(__FormSectionQuestionKey)
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
            };

            EncounterData ed = vm.CurrentEncounter.EncounterData
                .FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                if (formsection.SectionKey != null)
                {
                    ed = new EncounterData
                    {
                        SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                    };
                }

                sl.EncounterData = ed;

                if (sl.Encounter.IsNew && copyforward)
                {
                    sl.CopyForwardLastInstance();
                }
            }
            else
            {
                sl.EncounterData = ed;
            }

            ed.PropertyChanged += sl.EncounterData_PropertyChanged;
            sl.Setup();

            sl.SectionLabelSetup(vm.CurrentSectionUI);
            if (sl.IsEditablePOC && sl.Question != null && sl.Question.Label.ToUpper() == "ADDENDUM")
            {
                sl.ConditionalHidden = true;
            }

            return sl;
        }
    }
}