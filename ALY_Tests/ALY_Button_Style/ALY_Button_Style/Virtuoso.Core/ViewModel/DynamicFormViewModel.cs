#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using HavenBridgeService;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Utils;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Model;
using Virtuoso.Core.Navigation;
using Virtuoso.Core.Occasional;
using Virtuoso.Core.Occasional.Model;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.View;
using Virtuoso.Helpers;
using Virtuoso.Metrics;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public interface IPatientMenuSupport
    {
        Patient CurrentPatient { get; set; }
        NavigateKey NavigateKey { get; set; }
        bool IsBusy { get; set; }
    }

    class EncounterOriginal
    {
        public DateTimeOffset? EncounterStartDate { get; set; }
        public DateTimeOffset? EncounterStartTime { get; set; }
        public DateTimeOffset? EncounterEndDate { get; set; }
        public DateTimeOffset? EncounterEndTime { get; set; }
        public int? PatientAddressKey { get; set; }
        public int? EncounterActualTime { get; set; }
        public int? AdmissionDisciplineKey { get; set; }
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export]
    public class DynamicFormViewModel : ViewModelBase, IPatientMenuSupport, IParentViewModel, INavigateClose
    {
        public bool CurrentUserIsSurveyor => RoleAccessHelper.IsSurveyor;
        IFaxService FaxService { get; set; }

        INavigationService NavigationService { get; set; }

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

        bool AdministrativeServiceDateOverride;
        
        EncounterOriginal EncounterOriginal = new EncounterOriginal();
        static LogWriter logWriter;

        static DynamicFormViewModel()
        {
            logWriter = EnterpriseLibraryContainer.Current.GetInstance<LogWriter>();
        }

        public void FormNavigateBack()
        {
            //Notify OTWorklist of our exit passing ChangePhysicianOrdersTrackingKey to relay any physician changes
            Messenger.Default.Send(ChangePhysicianOrdersTrackingKey, "OrdersTrackingChangePhysicianReturn");
            NavigateBack();
        }

        public void NavigateClose()
        {
            AsyncUtility.Run(async () =>
            {
                if (EntityManager.IsOnline)
                {
                    await HandleOnlineClose();
                }
                else
                {
                    await HandleOfflineClose();
                }
            });
        }

        private bool doNavigateBackOnFormSaved;

        private async System.Threading.Tasks.Task HandleOnlineClose()
        {
            if ((FormModel != null && FormModel.ContextHasChanges) && (force_exit_application_after_save == false) &&
                !IsReadOnlyEncounter && (CurrentUserIsSurveyor == false))
            {
#if DEBUG
                CheckModelForChanges("HandleOnlineClose");
#endif
                NavigateCloseDialog navigateCloseDialog =
                    CreateDialogue("Close form and lose any unsaved changes?", null);
                if (navigateCloseDialog != null)
                {
                    navigateCloseDialog.Closed += OnlineNavigateCloseDialogOnClosed;
                    navigateCloseDialog.Show();
                }
            }
            else
            {
                if (FormModel != null)
                {
                    FormModel.RejectMultiChanges();
                }

                await DeleteDynamicForm(OfflineStoreType
                    .SAVE); //No changes in the context, so need to delete the SIP'd encounter data
                FormNavigateBack();
            }
        }

        private async void OnlineNavigateCloseDialogOnClosed(object sender, EventArgs e)
        {
            var dialog = sender as NavigateCloseDialog;
            dialog.Closed -= OnlineNavigateCloseDialogOnClosed;
            var _ret = dialog.DialogResult;
            if (_ret == true) //user chose to close form and lose any unsaved changes
            {
                //Note: need to do this so that when we navigate back into our VM, 
                //it will be in a state to be removed and it's Cleanup() method executed.
                if (FormModel != null)
                {
                    FormModel.RejectMultiChanges();
                }

                if (ShouldCancelTask())
                {
                    CancelGeneratedTask();
                }
                else
                {
                    await DeleteDynamicForm(OfflineStoreType.SAVE);
                    FormNavigateBack();
                }
            }
        }

        private bool ShouldCancelTask()
        {
            if (CurrentForm == null)
            {
                return false;
            }

            if (CurrentEncounter == null)
            {
                return false;
            }

            var isNew = (CurrentEncounter.IsNew ||
                         CurrentEncounter.EncounterStatus == (int)EncounterStatusType.None); //Not saved to server
            var isCancelableTask = CurrentForm.IsOasis || CurrentForm.IsHIS || CurrentForm.IsOrderEntry ||
                                   TeamMeetingCreatedByWorkList();
            var ret = (isNew && isCancelableTask);
            return ret;
        }

        private void
            CancelGeneratedTask() //NOTE: prior to calling this function, change were rejected - if (FormModel != null) FormModel.RejectMultiChanges();
        {
            // Remove the task if cancelling a new Oasis or OrderEntry form/encounter
            var cancelReason = CodeLookupCache.GetKeyFromCode("CancelReason", "Inactive");
            CurrentTask.CanceledAt = DateTime.UtcNow;
            CurrentTask.CanceledBy = WebContext.Current.User.MemberID;
            CurrentTask.CancelReasonKey = cancelReason;
            HomePageAgencyOpsRefreshOption = HomePageAgencyOpsRefreshOptionEnum.SingleTask;
            FormModel.SaveMultiAsync(() => LogChangeSet("CancelGeneratedTask"));
            doNavigateBackOnFormSaved = true;
        }

        private bool TeamMeetingCreatedByWorkList()
        {
            if (CurrentForm.IsTeamMeeting && NavigateKey.ParentUriOriginalString.Contains("TeamMeetingWorkList"))
            {
                return true;
            }

            return false;
        }

        private async System.Threading.Tasks.Task HandleOfflineClose()
        {
            if (FormModel.ContextHasChanges)
            {
#if DEBUG
                CheckModelForChanges("HandleOfflineClose");
#endif
                NavigateCloseDialog navigateCloseDialog = CreateDialogue("Close and save form locally?",
                    "NOTE: if you answer NO, any changes made since last save will be lost.");
                if (navigateCloseDialog != null)
                {
                    navigateCloseDialog.Closed += OfflineNavigateCloseDialogOnClosed;
                    navigateCloseDialog.Show();
                }
            }
            else
            {
                await DeleteDynamicForm(OfflineStoreType
                    .SAVE); //No changes in the context, so need to delete the SIP'd encounter data
                FormNavigateBack();
            }
        }

        private async void OfflineNavigateCloseDialogOnClosed(object sender, EventArgs e)
        {
            var dialog = sender as NavigateCloseDialog;
            dialog.Closed -= OfflineNavigateCloseDialogOnClosed;
            bool? _ret = dialog.DialogResult;
            if (_ret == true)
            {
                await OKProcessing(false); //perform an offline, e.g. silent save to disk...
            }

            FormNavigateBack();
        }


        int __cleanupCount;

        public override void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            Messenger.Default.Unregister<Exception>(Constants.Messaging.UnhandledException);
            Messenger.Default.Unregister(this);

            EntityManager.NetworkAvailabilityChanged -= Current_NetworkAvailabilityChanged;

            if (!HideFromNavigation)
            {
                if (BackgroundService.IsBackground == false)
                {
                    Messenger.Default.Send(new ContextSensitiveArgs { ViewModel = this },
                        "RemoveFromContextSensitiveMenu");
                }
            }


            if (BackgroundService.IsBackground == false)
            {
                LaunchHavenErrorWindow(null);
            }

            if (CurrentEncounter != null)
            {
                if (CurrentEncounter.TrackChangedProperties)
                {
                    CurrentEncounter.PropertyChanged -= CurrentEncounter_PropertyChanged;
                }

                CurrentEncounter.TrackChangedProperties = false;
                CurrentEncounter.ChangedProperties.Clear();
            }

            if (DFControlManager != null)
            {
                DFControlManager.Cleanup();
                DFControlManager = null;
            }

            SelectedSection?.Cleanup();

            foreach (var section in Sections)
            {
                if (section.Questions != null)
                {
                    foreach (var q in section.Questions)
                    {
                        q.Cleanup();
                        q.Encounter = null;
                    }

                    section.Questions.Clear();
                }

                section.Cleanup();
            }

            if (QuestionMasterList != null)
            {
                foreach (var questionUi in QuestionMasterList) questionUi.Cleanup();

                QuestionMasterList.Clear();
                QuestionMasterList = null;
            }

            StopAnimation -= DynamicFormViewModel_StopAnimation;

            if (BackgroundService.IsBackground == false)
            {
                //Show SIP and Server Message
                ShowToastNotifications();
            }

            if (ReEvaluateQuestions != null)
            {
                ReEvaluateQuestions.ForEach(r => r.Cleanup());
                ReEvaluateQuestions.Clear();
                ReEvaluateQuestions = null;
            }

            if (Sections != null)
            {
                Sections.Clear();
                Sections = null;
            }

            if (SignatureQuestion != null)
            {
                SignatureQuestion.Cleanup();
                SignatureQuestion = null;
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (_FilteredSections != null)
                {
                    _FilteredSections.Source = null;
                }

                if (_CurrentFilteredAdmissionDiagnosis != null)
                {
                    _CurrentFilteredAdmissionDiagnosis.Source = null;
                }
            });

            if (MSPManager != null)
            {
                MSPManager.Cleanup();
                MSPManager = null;
            }

            if (CurrentGoalManager != null)
            {
                CurrentGoalManager.Cleanup();
                CurrentGoalManager = null;
            }

            if (CurrentOasisManager != null)
            {
                CurrentOasisManager.Cleanup();
                CurrentOasisManager = null;
            }

            if (CurrentOrderEntryManager != null)
            {
                CurrentOrderEntryManager.Cleanup();
                CurrentOrderEntryManager = null;
            }

            if (CurrentAdmission != null)
            {
                CurrentAdmission.Cleanup();
                CurrentAdmission = null;
            }

            if (CurrentEncounter != null)
            {
                CurrentEncounter.Cleanup();
                CurrentEncounter = null;
            }

            if (FormModel != null)
            {
                FormModel.ConvertOASISB1ToC1FixedReturned -= FormServ_ConvertOASISB1ToC1FixedReturned;
                FormModel.ConvertOASISB1ToC1PPSReturned -= FormServ_ConvertOASISB1ToC1PPSReturned;
                FormModel.GetSSRSPDFDynamicFormReturned -= FormServ_GetSSRSPDFDynamicFormReturned;
                FormModel.CallHavenReturned -= FormServ_CallHavenReturned;

                FormModel.RejectMultiChanges();
                FormModel.OnMultiLoaded -= ProcessForm;
                FormModel.OnMultiSaved -= FormSaved;
                FormModel.Cleanup();
                FormModel = null;
            }

            base.Cleanup();
        }

        public override bool CanExit()
        {
#if DEBUG
            CheckModelForChanges("CanExit");
#endif
            if (FormModel.ContextHasChanges)
            {
                if (EntityManager.IsOnline)
                {
                    return false;
                }

                return true;
            }

            return true;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void CheckModelForChanges(string tag = "")
        {
            if (FormModel.ContextHasChanges)
            {
                Log(
                    "\r\n--------------------------------------------------------------------------------------------------\r\n");

                var entityChangeSets = FormModel.CheckChanges();
                Log(DomainContextChangeHelper.EntityChangeSetInformation(entityChangeSets));
                foreach (var _ecs in entityChangeSets)
                {
                    var _ec = _ecs.Item2;
                    foreach (var _AddedEntity in _ec.AddedEntities)
                        Log(DomainContextChangeHelper.EntityChangeInformation(_ecs.Item1, _AddedEntity,
                            "ADDED Entity"));
                    foreach (var _ModifiedEntity in _ec.ModifiedEntities)
                        Log(DomainContextChangeHelper.EntityChangeInformation(_ecs.Item1, _ModifiedEntity,
                            "MODIFIED Entity"));
                    foreach (var _RemovedEntity in _ec.RemovedEntities)
                        Log(DomainContextChangeHelper.EntityChangeInformation(_ecs.Item1, _RemovedEntity,
                            "REMOVED Entity"));
                }

                Log(
                    "\r\n--------------------------------------------------------------------------------------------------\r\n");
            }
        }

        public SectionUI CurrentSectionUI;
        ObservableCollection<QuestionUI> QuestionMasterList = new ObservableCollection<QuestionUI>();

        public QuestionUI ParentSignatureQuestion
        {
            get
            {
                var q = QuestionMasterList.Where(a => a.Question.DataTemplate.ToLower() == "signature");

                return q.FirstOrDefault();
            }
        }

        public IBackgroundService BackgroundService { get; set; }

        public IDynamicFormService FormModel { get; set; }

        //public IReportFormatting ReportFormatModel { get; set; }
        public VirtuosoApplicationConfiguration Configuration { get; set; }

        IOrderTrackingGroupService _orderTrackingService;

        public IOrderTrackingGroupService OrderTrackingService
        {
            get { return _orderTrackingService; }
            private set
            {
                _orderTrackingService = value;
                this.RaisePropertyChangedLambda(e => e.OrderTrackingService);
            }
        }

        private DateTimeOffset? PreviousPOCMailedDate;
        private Guid? PreviousPOCMailedBy;
        private DateTimeOffset? PreviousPOCSignedDate;
        private Guid? PreviousPOCSignedBy;

        public bool IsSIPSave;
        public int PreviousEncounterStatus;
        private bool PreviousSigned;

        private int InterimEncounterStatus;
        private bool InterimSigned;

        #region Interim Status Data

        private int PreviousAdmissionStatus;
        private DateTime? PreviousAdmissionDateTime;
        private DateTime? PreviousAdmissionDischargeDateTime;
        private DateTime? PreviousAdmissionNotTakenDateTime;
        private String PreviousAdmitNotTakenReason;
        private String PreviousPreEvalStatus;
        private String PreviousPreEvalOnHoldReason;
        private DateTime? PreviousPreEvalOnHoldDateTime;
        private DateTime? PreviousPreEvalFollowUpDate;
        private String PreviousPreEvalFollowUpComments;

        private int? PreviousAdmissionDiscStatus;
        private DateTime? PreviousAdmissionDiscAdmitDateTime;
        private DateTime? PreviousAdmissionDiscDischargeDateTime;
        private DateTime? PreviousAdmissionDiscNotTakenDateTime;
        private String PreviousAdmitDiscNotTakenReason;

        private int InterimAdmissionStatus;
        private DateTime? InterimAdmissionDateTime;
        private DateTime? InterimAdmissionDischargeDateTime;
        private DateTime? InterimAdmissionNotTakenDateTime;
        private String InterimAdmitNotTakenReason;
        private String InterimPreEvalStatus;
        private String InterimPreEvalOnHoldReason;
        private DateTime? InterimPreEvalOnHoldDateTime;
        private DateTime? InterimPreEvalFollowUpDate;
        private String InterimPreEvalFollowUpComments;

        private int? InterimAdmissionDiscStatus;
        private DateTime? InterimAdmissionDiscAdmitDateTime;
        private DateTime? InterimAdmissionDiscDischargeDateTime;
        private DateTime? InterimAdmissionDiscNotTakenDateTime;
        private String InterimAdmitDiscNotTakenReason;
        private DateTime? StoredAdmissionGroupDate;

        #endregion

        private bool _isAddendum;

        public bool IsAddendum
        {
            get { return _isAddendum; }
            set
            {
                _isAddendum = value;
                this.RaisePropertyChangedLambda(p => p.IsAddendum);
            }
        }

        private bool _OK_CommandVisible = true;

        public bool OK_CommandVisible
        {
            get { return _OK_CommandVisible; }
            set
            {
                _OK_CommandVisible = value;
                this.RaisePropertyChangedLambda(p => p.OK_CommandVisible);
            }
        }

        private ISignature _SignatureQuestion;

        public ISignature SignatureQuestion
        {
            get { return _SignatureQuestion; }
            set
            {
                _SignatureQuestion = value;
                this.RaisePropertyChangedLambda(p => p.SignatureQuestion);
            }
        }

        private bool _AddingNewEncounter;

        public bool AddingNewEncounter
        {
            get { return _AddingNewEncounter; }
            set
            {
                _AddingNewEncounter = value;
                this.RaisePropertyChangedLambda(p => p.AddingNewEncounter);
            }
        }

        bool ParamsInitialized;
        public int ServiceTypeKey;
        int FormKey;
        public int? PatientKey { get; set; }
        int AdmissionKey;

        int ____taskKey;

        int TaskKey
        {
            get { return ____taskKey; }
            set { ____taskKey = value; }
        }

        private bool _CMSProtectSOCDate;

        public bool CMSProtectSOCDate
        {
            get { return _CMSProtectSOCDate; }
            set
            {
                _CMSProtectSOCDate = value;
                RaisePropertyChanged("CMSProtectSOCDate");
            }
        }
        
        List<EncounterSignature> SignaturesToAdd = new List<EncounterSignature>();
        private bool AllValid = true;

        private HavenReturn _HavenReturn;

        public HavenReturn HavenReturn
        {
            get { return _HavenReturn; }
            set
            {
                _HavenReturn = value;
                this.RaisePropertyChangedLambda(p => p.HavenReturn);
            }
        }

        private List<HavenErrorItem> _HavenErrorList;

        public List<HavenErrorItem> HavenErrorList
        {
            get { return _HavenErrorList; }
            set
            {
                _HavenErrorList = value;
                this.RaisePropertyChangedLambda(p => p.HavenErrorList);
            }
        }

        private Window _HavenErrorWindow;

        private void LaunchHavenErrorWindow(List<HavenErrorItem> havenErrorList)
        {
            if ((CurrentOasisManager == null) || (CurrentOasisManager.CurrentEncounterOasis == null))
            {
                return;
            }

            if (CurrentOasisManager.CurrentEncounterOasis.SYS_CDIsHospice)
            {
                return;
            }

            if ((havenErrorList == null) || (havenErrorList.Any() == false))
            {
                if (_HavenErrorWindow != null)
                {
                    try
                    {
                        _HavenErrorWindow.Close();
                    }
                    catch
                    {
                    }
                }

                _HavenErrorWindow = null;
            }
            else
            {
                HavenErrorWindow heWindow = null;
                if (_HavenErrorWindow == null)
                {
                    _HavenErrorWindow = new Window();
                    _HavenErrorWindow.Closing += HavenErrorWindow_Closing;
                    heWindow = new HavenErrorWindow();
                    _HavenErrorWindow.Content = heWindow;
                    _HavenErrorWindow.Width = 1024;
                    _HavenErrorWindow.Height = 576;
                    _HavenErrorWindow.Title = "OASIS Errors on " +
                                              CurrentOasisManager.CurrentEncounterOasis.RFADescription +
                                              " survey for " + CurrentPatient.FirstName + " " + CurrentPatient.LastName;
                }

                heWindow = _HavenErrorWindow.Content as HavenErrorWindow;
                if (heWindow != null)
                {
                    HavenErrorWindowViewModel vm = heWindow.DataContext as HavenErrorWindowViewModel;
                    if (vm != null)
                    {
                        vm.HavenErrorList = havenErrorList;
                    }
                }

                _HavenErrorWindow.Visibility = Visibility.Visible;
                _HavenErrorWindow.WindowState = WindowState.Normal;
                _HavenErrorWindow.Activate();
            }
        }

        void HavenErrorWindow_Closing(object sender, ClosingEventArgs e)
        {
            if (_HavenErrorWindow != null)
            {
                _HavenErrorWindow.Closing -= HavenErrorWindow_Closing;
                _HavenErrorWindow = null;
            }
        }

        private void SetupHavenErrorList(HavenReturn havenReturn)
        {
            if (havenReturn == null)
            {
                HavenErrorList = null;
                return;
            }

            if (((havenReturn.ErrorCodes == null) || (havenReturn.ErrorCodes.Any() == false)) ||
                ((havenReturn.ErrorDescriptions == null) || (havenReturn.ErrorDescriptions.Any() == false)) ||
                ((havenReturn.ErrorTypes == null) || (havenReturn.ErrorTypes.Any() == false)))
            {
                HavenErrorList = null;
                return;
            }

            int count = (havenReturn.ErrorCodes.Count >= havenReturn.ErrorDescriptions.Count)
                ? havenReturn.ErrorCodes.Count
                : havenReturn.ErrorDescriptions.Count;
            count = (count >= havenReturn.ErrorTypes.Count) ? count : havenReturn.ErrorTypes.Count;
            HavenErrorList = new List<HavenErrorItem>();
            for (int i = 0; i < count; i++)
            {
                string errorCode = havenReturn.ErrorCodes[i].ToLower();
                string errorDescription = havenReturn.ErrorDescriptions[i].ToLower();
                string errorType = havenReturn.ErrorTypes[i].ToLower();
                if ((errorType == "error") || (errorType == "fatal"))
                {
                    if ((errorDescription.Contains("oasis data string is not 1448 bytes in length") == false) &&
                        (errorCode.Contains("hipps_code") == false) &&
                        (errorCode.Contains("hipps_version") == false))
                    {
                        HavenErrorList.Add(new HavenErrorItem
                        {
                            ErrorCode = havenReturn.ErrorCodes[i], ErrorDescription = havenReturn.ErrorDescriptions[i],
                            ErrorType = havenReturn.ErrorTypes[i],
                        });
                    }
                }
            }

            if (HavenErrorList.Any() == false)
            {
                HavenErrorList = null;
            }
        }

        public DynamicFormControlManager DFControlManager;
        public MSPQuestionaireManager MSPManager;
        private bool _hideFromNavigation;

        public bool HideFromNavigation
        {
            get { return _hideFromNavigation; }
            set { _hideFromNavigation = value; }
        }

        void Current_NetworkAvailabilityChanged(object sender, Client.Offline.Events.NetworkAvailabilityEventArgs e)
        {
            RaisePropertyChangedPrintStuff();
        }

        [ImportingConstructor]
        public DynamicFormViewModel(IDynamicFormService _formmodel, IFaxService faxService,
            INavigationService navigationService, VirtuosoApplicationConfiguration config,
            IBackgroundService _backgroundService, IOrderTrackingGroupService _orderTrackingService)
            : base(new ListUIManager())
        {
            FaxService = faxService;
            NavigationService = navigationService;
            ScreenBusyEvent = new MetricsTimerHelper(new StopWatchFactory(), CorrelationIDHelper, Logging.Context.Busy,
                autoStart: false);

            Messenger.Default.Register<Exception>(this, Constants.Messaging.UnhandledException,
                s => { IsBusy = false; });

            EntityManager.NetworkAvailabilityChanged += Current_NetworkAvailabilityChanged;

            if (_formmodel != null)
            {
                _formmodel.ConvertOASISB1ToC1FixedReturned += FormServ_ConvertOASISB1ToC1FixedReturned;
            }

            if (_formmodel != null)
            {
                _formmodel.ConvertOASISB1ToC1PPSReturned += FormServ_ConvertOASISB1ToC1PPSReturned;
            }

            if (_formmodel != null)
            {
                _formmodel.GetSSRSPDFDynamicFormReturned += FormServ_GetSSRSPDFDynamicFormReturned;
            }

            if (_formmodel != null)
            {
                _formmodel.CallHavenReturned += FormServ_CallHavenReturned;
            }

            SavedDataType = "Task"; //this will prepend the save message after data is saved
            DFControlManager = new DynamicFormControlManager();
            MSPManager = new MSPQuestionaireManager();

            OnCallLoaded = new RelayCommand<EncounterData>(ed =>
            {
                if (ed.BoolData == null)
                {
                    ed.BoolData = false;
                }
            });

            Debug_Command = new RelayCommand(
                () => { CheckModelForChanges("Debug_Command"); });

            Save_Command = new RelayCommand(
                async () =>
                {
                    UserHitOK = true;
                    var _isOnline = EntityManager.IsOnline;
                    await OKProcessing(_isOnline, OfflineStoreType.SAVE, true);
                });

            Cancel_Command = new RelayCommand(
                () => { NavigateClose(); });

            SSRSPrint_Command = new RelayCommand(
                () =>
                {
                    if (IsOffLine("Reports are not available when application is offline."))
                    {
                        return;
                    }

                    PrintSSRSDynamicForm(FormKey, PatientKey == null ? 0 : (int)PatientKey,
                        CurrentEncounter.EncounterKey, AdmissionKey, HideOasisQuestions);
                },
                () =>
                {
                    PrintOrFaxVisibleError = string.Empty;
                    if (CurrentForm == null || CurrentEncounter == null)
                    {
                        return false;
                    }

                    var print_button_enabled = PrintOrFaxButtonDisabled(PrintVisible, true);
                    Log($"[SSRSPrint_Command] CanExecute: {print_button_enabled}, PatientKey={PatientKey}",
                        "FAX_TRACE");
                    return print_button_enabled;
                });

            Fax_Command = new RelayCommand(
                () =>
                {
                    if (IsOffLine("Faxing is not available when application is offline."))
                    {
                        return;
                    }

                    FaxDynamicForm(FormKey, PatientKey == null ? 0 : (int)PatientKey, CurrentEncounter.EncounterKey,
                        AdmissionKey, HideOasisQuestions);
                },
                () =>
                {
                    PrintOrFaxVisibleError = string.Empty;
                    if (CurrentForm == null || CurrentEncounter == null)
                    {
                        return false;
                    }

                    var fax_button_enabled = PrintOrFaxButtonDisabled(FaxVisible, false);
                    Log($"[Fax_Command] CanExecute: {fax_button_enabled}, PatientKey={PatientKey}", "FAX_TRACE");
                    return fax_button_enabled;
                });

            StopAnimation += DynamicFormViewModel_StopAnimation;

            BackgroundService = _backgroundService;

            FormModel = _formmodel;
            if (BackgroundService == null)
            {
                BackgroundService = new BackgroundService();
            }

            if (BackgroundService.IsBackground == false)
            {
                FormModel.OnMultiLoaded += ProcessForm;
                FormModel.OnMultiSaved += FormSaved;
            }

            if (_orderTrackingService != null)
            {
                OrderTrackingService = _orderTrackingService;
                OrderTrackingService.OnOrdersTrackingRowRefreshed += OrderTrackingService_OnOrdersTrackingRowRefreshed;
            }

            Configuration = config;
        }

        public async System.Threading.Tasks.Task AutoSave_Command(string callsite)
        {
            Log($"AutoSave-{callsite}", "WS_TRACE");

            if (TenantSettingsCache.Current.TenantSettingAutosaveFrequency != -1)
            {
                return;
            }

            if ((CurrentEncounter == null) || (CurrentEncounter.PreviousEncounterStatusIsInEdit == false))
            {
                return;
            }

            UserHitOK = true;
            var _isOnline = EntityManager.IsOnline;
            IsBusy = true;
            StoredAdmissionGroupDate = CurrentAdmission.AdmissionGroupDate;
            SetupAttemptedEncounterPart2();
            await SaveDynamicForm(
                (CurrentPatient != null) ? CurrentPatient.PatientKey : -1,
                (CurrentEncounter != null) ? CurrentEncounter.EncounterKey : -1,
                (CurrentEncounter != null) ? CurrentEncounter.EncounterStatus : (int)EncounterStatusType.Edit,
                OfflineStoreType.SAVE,
                true);

            IsBusy = false;
        }


        bool UserHitOK;
        int AutosaveFrequencyTenantSetting = 5;

        private bool CanAutoSaveNow()
        {
            return (AutosaveFrequencyTenantSetting > 0) &&
                   (CurrentEncounter != null) &&
                   (!CurrentEncounter.IsReadOnly) &&
                   (!CurrentEncounter.Inactive) &&
                   (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed);
        }

        private void SleepSeconds(int seconds)
        {
            Thread.Sleep(1000 * seconds);
        }

        private void RefreshSaveMessage()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { RaisePropertyChanged("LastSaveDateTime"); });
        }

        private DateTime? _lastSaveDateTime;

        public DateTime? lastSaveDateTime
        {
            get { return _lastSaveDateTime; }
            set { _lastSaveDateTime = value; }
        }

        public string LastSaveDateTime
        {
            get
            {
                if (CurrentEncounter != null && lastSaveDateTime.HasValue)
                {
                    var ce = CurrentEncounter;
                    if ((ce.EncounterStatus != (int)EncounterStatusType.Completed) && !ce.Inactive && !ce.IsReadOnly)
                    {
                        AutosaveFrequencyTenantSetting = TenantSettingsCache.Current.TenantSettingAutosaveFrequency;
                        if (AutosaveFrequencyTenantSetting > 0)
                        {
                            return (lastSaveDateTime.HasValue)
                                ? "Last Save Date: " + lastSaveDateTime.Value.ToString("MM/dd/yy hh:mm tt")
                                : "";
                        }
                    }
                }

                return "";
            }
        }

        public void PrintSSRSDynamicForm(int formKey, int patientKey, int encounterKey, int admissionKey,
            bool hideOasisQuestions)
        {
            IsBusy = true;
            FormModel.GetSSRSPDFDynamicForm(formKey, patientKey, encounterKey, admissionKey, hideOasisQuestions);
        }

        public FaxingPhysician? GetFaxingPhysician()
        {
            if (QuestionMasterList == null || QuestionMasterList.ToList().Count == 0)
            {
                return null;
            }

            if (CurrentAdmission == null)
            {
                return null;
            }

            if (CurrentForm == null)
            {
                return null;
            }

            if (CurrentEncounter == null)
            {
                return null;
            }

            if (CurrentForm.IsCOTI)
            {
                var faxingPhysicianForCOTI = GetFaxingPhysicianCOTI();
                if (faxingPhysicianForCOTI.HasValue)
                {
                    Log(
                        $"[DynamicFormViewModel.GetFaxingPhysician] COTI - PatientKey: {PatientKey}, EncounterKey: {CurrentEncounter.EncounterKey}, PhysicianKey: {faxingPhysicianForCOTI.Value.PhysicianKey}, FaxNumber: {faxingPhysicianForCOTI.Value.FaxNumber}",
                        "FAX_TRACE");
                }
                else
                {
                    Log(
                        $"[DynamicFormViewModel.GetFaxingPhysician] COTI - PatientKey: {PatientKey}, EncounterKey: {CurrentEncounter.EncounterKey}, NO Faxing Physician",
                        "FAX_TRACE");
                }

                return faxingPhysicianForCOTI;
            }

            if (CurrentForm.IsOrderEntry)
            {
                var faxingPhysicianForOrderEntry = GetFaxingPhysicianOrderEntry();
                if (faxingPhysicianForOrderEntry.HasValue)
                {
                    Log(
                        $"[DynamicFormViewModel.GetFaxingPhysician] OrderEntry - PatientKey: {PatientKey}, EncounterKey: {CurrentEncounter.EncounterKey}, PhysicianKey: {faxingPhysicianForOrderEntry.Value.PhysicianKey}, FaxNumber: {faxingPhysicianForOrderEntry.Value.FaxNumber}",
                        "FAX_TRACE");
                }
                else
                {
                    Log(
                        $"[DynamicFormViewModel.GetFaxingPhysician] OrderEntry - PatientKey: {PatientKey}, EncounterKey: {CurrentEncounter.EncounterKey}, NO Faxing Physician",
                        "FAX_TRACE");
                }

                return faxingPhysicianForOrderEntry;
            }

            if (CurrentForm.IsPlanOfCare)
            {
                var faxingPhysicianForPlanOfCare = GetFaxingPhysicianPlanOfCare();
                if (faxingPhysicianForPlanOfCare.HasValue)
                {
                    Log(
                        $"[DynamicFormViewModel.GetFaxingPhysician] PlanOfCare - PatientKey: {PatientKey}, EncounterKey: {CurrentEncounter.EncounterKey}, PhysicianKey: {faxingPhysicianForPlanOfCare.Value.PhysicianKey}, FaxNumber: {faxingPhysicianForPlanOfCare.Value.FaxNumber}",
                        "FAX_TRACE");
                }
                else
                {
                    Log(
                        $"[DynamicFormViewModel.GetFaxingPhysician] PlanOfCare - PatientKey: {PatientKey}, EncounterKey: {CurrentEncounter.EncounterKey}, NO Faxing Physician",
                        "FAX_TRACE");
                }

                return faxingPhysicianForPlanOfCare;
            }

            return null;
        }

        public FaxingPhysician? GetFaxingPhysicianCOTI()
        {
            var ques = QuestionMasterList.ToList().Where(q => q.Label.Equals("Certification Statement"))
                .FirstOrDefault();
            if (ques != null && CurrentAdmission.HospiceAdmission)
            {
                var hospiceAdmissionCOTIQuestion = ques as HospiceAdmissionCOTI;
                if (hospiceAdmissionCOTIQuestion != null)
                {
                    var currentAdmissionCOTI = hospiceAdmissionCOTIQuestion.AdmissionCOTI;
                    if (currentAdmissionCOTI != null)
                    {
                        FaxingPhysician? faxingPhysician = hospiceAdmissionCOTIQuestion.GetFaxingPhysician();
                        if (faxingPhysician != null &&
                            string.IsNullOrWhiteSpace(faxingPhysician.Value.FaxNumber) == false)
                        {
                            return faxingPhysician;
                        }
                    }
                }
            }

            return null;
        }

        public FaxingPhysician? GetFaxingPhysicianOrderEntry()
        {
            var ques = QuestionMasterList.ToList().Where(q => q.Label.Equals("SignatureOrderEntry")).FirstOrDefault();
            if (ques != null) // FYI: All service lines save physiciankey to OrderEntry.SigningPhysicianKey
            {
                var signatureOrderEntryQuestion = ques as SignatureOrderEntry;
                if (signatureOrderEntryQuestion != null)
                {
                    var currentOrderEntry = signatureOrderEntryQuestion.CurrentOrderEntry;
                    if (currentOrderEntry != null)
                    {
                        // If the order is Voided - return null for 'faxing physician' - to have the effect of hiding the Fax button - e.g. setting FaxVisible to FALSE
                        OrdersTracking ot = (currentOrderEntry.OrdersTracking == null)
                            ? null
                            : currentOrderEntry.OrdersTracking
                                .Where(o => o.OrderEntryKey == currentOrderEntry.OrderEntryKey).FirstOrDefault();
                        if (ot != null)
                        {
                            if (ot.Status != (int)OrdersTrackingStatus.Void)
                            {
                                FaxingPhysician? faxingPhysician = signatureOrderEntryQuestion.GetFaxingPhysician();
                                if (faxingPhysician != null &&
                                    string.IsNullOrWhiteSpace(faxingPhysician.Value.FaxNumber) == false)
                                {
                                    return faxingPhysician;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public FaxingPhysician? GetFaxingPhysicianPlanOfCare()
        {
            var ques = QuestionMasterList.ToList().Where(q => q.Label.Equals("POC Physician Information"))
                .FirstOrDefault();
            if (ques == null)
            {
                return null;
            }

            if (CurrentAdmission.HospiceAdmission)
            {
                var pocHospiceBaseQuestion = ques as POCHospiceBase;
                if (pocHospiceBaseQuestion != null)
                {
                    FaxingPhysician? faxingPhysician = pocHospiceBaseQuestion.GetFaxingPhysician();
                    if (faxingPhysician != null && string.IsNullOrWhiteSpace(faxingPhysician.Value.FaxNumber) == false)
                    {
                        return faxingPhysician;
                    }
                }
            }
            else // HomeHealth
            {
                var pocBaseQuestion = ques as POCBase;
                if (pocBaseQuestion != null)
                {
                    FaxingPhysician? faxingPhysician = pocBaseQuestion.GetFaxingPhysician();
                    if (faxingPhysician != null && string.IsNullOrWhiteSpace(faxingPhysician.Value.FaxNumber) == false)
                    {
                        return faxingPhysician;
                    }
                }
            }

            return null;
        }

        public void FaxDynamicForm(int formKey, int patientKey, int encounterKey, int admissionKey,
            bool hideOasisQuestions)
        {
            if (FaxService != null && FormModel != null && FaxVisible)
            {
                var faxingPhysician = GetFaxingPhysician();
                if (faxingPhysician.HasValue && string.IsNullOrWhiteSpace(faxingPhysician.Value.FaxNumber) == false)
                {
                    IsBusy = true;
                    // NOTE: This code will be called when faxing a POC, OrderEntry, and CTI.                    
                    FaxService.FaxDocument(new EncounterFaxParameters
                        {
                            FaxNumber = faxingPhysician.Value.FaxNumber,
                            PhysicianKey = faxingPhysician.Value.PhysicianKey,
                            FormKey = formKey,
                            PatientKey = patientKey,
                            EncounterKey = encounterKey,
                            AdmissionKey = admissionKey,
                            HideOasisQuestions = hideOasisQuestions
                        },
                        () => { IsBusy = false; },
                        () => { IsBusy = false; });
                }
            }
        }

        void OrderTrackingService_OnOrdersTrackingRowRefreshed(object sender, EntityEventArgs e)
        {
            IsBusy = false;
        }

        bool force_exit_application_after_save;

        void DynamicFormViewModel_StopAnimation(object sender, EventArgs e)
        {
            if (force_exit_application_after_save)
            {
                if (FormModel != null)
                {
                    FormModel
                        .RejectMultiChanges(); //since we're force exiting - reject changes so that the form closing code doesn't prompt end user to lose changes
                }

                Cancel_Command.Execute(null);
            }
        }

        private bool IsHavenValidationOn
        {
            get
            {
                if (TenantSettingsCache.Current.TenantSetting.UsingHavenValidation == false)
                {
                    return false;
                }

                if (CurrentPatient == null)
                {
                    MessageBox.Show("Error OASIS Validation: Internal setup error, CurrentPatient is null");
                    return false;
                }

                if (CurrentAdmission == null)
                {
                    MessageBox.Show("Error OASIS Validation: Internal setup error, CurrentAdmission is null");
                    return false;
                }

                if (CurrentEncounter == null)
                {
                    MessageBox.Show("Error OASIS Validation: Internal setup error, CurrentEncounter is null");
                    return false;
                }

                if (CurrentEncounter.SYS_CDIsHospice)
                {
                    return false;
                }

                if (CurrentOasisManager == null)
                {
                    return false;
                }

                if (CurrentOasisManager.CurrentEncounterOasis == null)
                {
                    return false;
                }

                if (CurrentOasisManager.CurrentEncounterOasis.BypassFlag == true)
                {
                    return false;
                }

                if (CurrentOasisManager.CurrentEncounterOasis.REC_ID == "X1")
                {
                    return false;
                }
                
                if (CurrentOasisManager.RFA == null)
                {
                    MessageBox.Show("Error OASIS Validation: Internal setup error, RFA is null");
                    return false;
                }

                return true;
            }
        }

        private bool IsHavenValidationOnDuringOKProcessing
        {
            get
            {
                if (IsHavenValidationOn == false)
                {
                    return false;
                }

                if ((CurrentEncounter.EncounterStatus == (int)EncounterStatusType.CoderReview) ||
                    (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.OASISReview))
                {
                    if (CurrentEncounter.EncounterBy == WebContext.Current.User.MemberID)
                    {
                        return true;
                    }
                }
                else if (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    if (PreviousEncounterStatus == (int)EncounterStatusType.Completed)
                    {
                        // haven validate - if its a coordinator/entry user re-editing a completed surveys 
                        if (CurrentEncounter.CanEditCompleteCMS)
                        {
                            return true;
                        }
                    }
                    else //if (PreviousEncounterStatus != (int)EncounterStatusType.Completed)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private bool IsHavenValidationOnDuringEncounterStartUp
        {
            get
            {
                if (IsHavenValidationOn == false)
                {
                    return false;
                }

                return IsOASISReeditingCompletedSurvey;
            }
        }

        private bool IsOASISReeditingCompletedSurvey
        {
            get
            {
                if (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    if (PreviousEncounterStatus == (int)EncounterStatusType.Completed)
                    {
                        if (CurrentEncounter.CanEditCompleteCMS)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public async System.Threading.Tasks.Task OKProcessing(bool isOnline, OfflineStoreType offlineLocation = OfflineStoreType.SAVE, bool show8HourWarning = false)
        {
            Log(
                $"OKProcessing BEGIN: isOnline={isOnline}, offlineLocation={offlineLocation}, show8HourWarning={show8HourWarning}",
                "WS_TRACE");

            IsBusy = true;
            StoredAdmissionGroupDate = CurrentAdmission.AdmissionGroupDate;

            SetupAttemptedEncounterPart2();

            await SaveDynamicForm(
                (CurrentPatient != null) ? CurrentPatient.PatientKey : -1,
                (CurrentEncounter != null) ? CurrentEncounter.EncounterKey : -1,
                (CurrentEncounter != null) ? CurrentEncounter.EncounterStatus : (int)EncounterStatusType.Edit,
                offlineLocation,
                false);

            //Process Entered Form Data - deciding which to keep/save and which to throw away because question was not answered
            AllValid = true;
            ValidationMessage = string.Empty;
            CurrentEncounter.ValidationErrors.Clear();
            AdmissionCOTI ac = ((CurrentAdmission == null) || (CurrentEncounter.AdmissionCOTI == null))
                ? null
                : CurrentEncounter.AdmissionCOTI.FirstOrDefault();
            if (ac != null)
            {
                ac.ValidationErrors.Clear();
            }

            if (CurrentEncounterTransfer != null)
            {
                CurrentEncounterTransfer.ValidationErrors.Clear();
            }

            if (CurrentAdmissionDiscipline != null)
            {
                CurrentAdmissionDiscipline.ValidationErrors.Clear();
            }

            CurrentEncounter.Signed = HasSignatureOrIsComplete(CurrentEncounter);

            // Setup new EncounterStatus if need be
            if (SignatureQuestion != null)
            {
                SignatureQuestion.CalculateNewEncounterStatus(CurrentEncounter.Signed);
            }

            // insure we tidy up the b1record when we call haven and/or fully validate OASIS
            if (CurrentOasisManager != null)
            {
                CurrentOasisManager.DefaultQuestions((IsHavenValidationOnDuringOKProcessing) && (isOnline));
            }

            if (CurrentOasisManager != null)
            {
                CurrentOasisManager.SetPPSModelVersion();
            }

            // Async call HAVEN validation if need be
            HavenReturn = null;
            if ((IsHavenValidationOnDuringOKProcessing) && (isOnline))
            {
                string b1Record = CurrentOasisManager.CurrentEncounterOasis.B1Record;

                // Haven will convert the b1record to a c1record if necessary, logic was moved to the db
                if (CurrentOasisManager != null)
                {
                    CallHaven(b1Record, CurrentOasisManager.GetPPSModelVersion,
                        CurrentOasisManager.CurrentEncounterOasis.OasisVersionKey);
                }
            }
            else
            {
                if ((CurrentOasisManager != null) && (CurrentOasisManager.CurrentEncounterOasisIsBypassed == false))
                {
                    CurrentOasisManager.ClearHavenErrors();
                }

                SetupHavenErrorList(HavenReturn);

                await OKProcessingPart2(isOnline, offlineLocation, show8HourWarning);
            }

            Log("OKProcessing END", "WS_TRACE");
        }

        void FormServ_ConvertOASISB1ToC1PPSReturned(InvokeOperation<byte[]> obj)
        {
            IsBusy = false;
            if (obj.Error != null)
            {
                MessageBox.Show("Error.ConvertOASISB1ToC1Fixed.  Returned exception " + obj.Error.Message +
                                ", contact AlayaCare support.  Assuming no OASIS validation errors exist.");
                CurrentOasisManager.ClearHavenErrors();
                return;
            }

            string error = Encoding.UTF8.GetString(obj.Value, 0, obj.Value.Length);
            if (error != null && !error.StartsWith("%PDF"))
            {
                MessageBox.Show("Error: Unable to get analyzed PDF: " + error);
                return;
            }

            // Recieved valid PDF
            byte[] ourPDF = obj.Value;

            // Display pdf via ShellExecute
            if (ourPDF != null)
            {
                ShellExecuteInterop.OpenPDF(ourPDF, "PPS Plus Analyzer");
            }
        }

        void FormServ_GetSSRSPDFDynamicFormReturned(InvokeOperation<byte[]> obj)
        {
            IsBusy = false;
            if (obj.Error != null)
            {
                MessageBox.Show("Error.GetSSRSPDFDynamicFormReturned.  Returned exception " + obj.Error.Message +
                                ", contact AlayaCare support.");
                obj.MarkErrorAsHandled();
                return;
            }

            string error = Encoding.UTF8.GetString(obj.Value, 0, obj.Value.Length);
            if (error != null && !error.StartsWith("%PDF"))
            {
                MessageBox.Show("Error: Unable to get SSRS report PDF: " + error);
                obj.MarkErrorAsHandled();
                return;
            }

            // Recieved valid PDF
            byte[] ourPDF = obj.Value;
            if (ourPDF != null)
            {
                ShellExecuteInterop.OpenPDF(ourPDF,
                    string.Format("SSRSPDFDocument-{0}",
                        ((CurrentForm == null) ? "0" : CurrentForm.FormKey.ToString().Trim())));
            }

            UpdateOrdersTracking();
        }

        async void FormServ_ConvertOASISB1ToC1FixedReturned(InvokeOperation<string> obj)
        {
            if (obj.Error != null)
            {
                MessageBox.Show("Error.ConvertOASISB1ToC1Fixed.  Returned exception" + obj.Error.Message +
                                ", contact AlayaCare support.  Assuming no OASIS validation errors exist.");
                HavenReturn = null;
                SetupHavenErrorList(HavenReturn);
                CurrentOasisManager.ClearHavenErrors();
                var _isOnline = EntityManager.IsOnline;
                await OKProcessingPart2(_isOnline, OfflineStoreType.SAVE, true);
                return;
            }

            string B1Record = obj.Value;
            if ((string.IsNullOrWhiteSpace(B1Record) == false) && (B1Record.Length == 3258))
            {
                B1Record = B1Record.Substring(0, B1Record.Length - 3) + '%';
            }

            if (CurrentOasisManager != null)
            {
                CallHaven(B1Record, CurrentOasisManager.GetPPSModelVersion,
                    CurrentOasisManager.CurrentEncounterOasis.OasisVersionKey);
            }
        }

        private void CallHaven(string B1Record, string PPSModelVersion, int OasisVersionKey)
        {
            FormModel.HavenValidateB1RecordAsync(B1Record, PPSModelVersion, OasisVersionKey);
        }

        private async void FormServ_CallHavenReturned(InvokeOperation<HavenReturnWrapper> obj)
        {
            string errorMessage = null;
            HavenReturn = new HavenReturn();

            if (obj == null || obj.Value == null)
            {
                errorMessage =
                    "Error.HavenValidateB1RecordAsync.  Haven returned no result due to a communication error or a timeout.  OASIS Validation has been bypassed.  Assuming no validation errors exist.";
            }
            else
            {
                HavenReturnWrapper result = obj.Value;

                if (result.ErrorCodes != null)
                {
                    HavenReturn.ErrorCodes = result.ErrorCodes.ToList();
                }

                if (result.ErrorDescriptions != null)
                {
                    HavenReturn.ErrorDescriptions = result.ErrorDescriptions.ToList();
                }

                if (result.ErrorDescriptions != null)
                {
                    HavenReturn.ErrorTypes = result.ErrorTypes.ToList();
                }

                if (result.ExceptionMessage != null)
                {
                    HavenReturn.ExceptionMessage = result.ExceptionMessage;
                }
            }

            if (HavenReturn == null)
            {
                errorMessage =
                    "Error.HavenValidateB1RecordAsync.  Haven returned no result due to a communication error or a timeout.  OASIS Validation has been bypassed.  Assuming no validation errors exist.";
            }
            else if (string.IsNullOrWhiteSpace(HavenReturn.ExceptionMessage) == false)
            {
                if ((HavenReturn.ExceptionMessage.ToLower().Contains("record length is invalid")) ||
                    (HavenReturn.ExceptionMessage.ToLower().Contains("unable to location properties file")) ||
                    (HavenReturn.ExceptionMessage.ToLower().Contains("havenvalidateb1record exception:")) ||
                    (HavenReturn.ExceptionMessage.ToLower().Contains("validated1 error")))
                {
                    errorMessage = "Error.HavenValidateB1RecordAsync.  Returned HavenReturn exception" +
                                   HavenReturn.ExceptionMessage +
                                   ", contact AlayaCare support.  Assuming no validation errors exist.";
                }

                HavenReturn = null;
            }
            else if ((HavenReturn.ErrorCodes == null) || (HavenReturn.ErrorCodes.Any() == false))
            {
                HavenReturn = null;
            }

            if (string.IsNullOrWhiteSpace(errorMessage) == false)
            {
                MessageBox.Show(errorMessage);
            }

            SetupHavenErrorList(HavenReturn);
            if (HavenErrorList == null)
            {
                CurrentOasisManager.ClearHavenErrors();
            }
            else
            {
                // Haven errors
                CurrentOasisManager.CurrentEncounterOasis.HavenValidationErrors = true;
                if (CurrentOasisManager.CurrentEncounterOasis.OnHold == false)
                {
                    if (SignatureQuestion != null)
                    {
                        if (SignatureQuestion.GetTakeOffHold() == false)
                        {
                            CurrentOasisManager.CurrentEncounterOasis.OnHold = true;
                        }
                    }
                    else
                    {
                        CurrentOasisManager.CurrentEncounterOasis.OnHold = true;
                    }
                }
            }

            var _isOnline = EntityManager.IsOnline;
            await OKProcessingPart2(_isOnline, OfflineStoreType.SAVE, true);
        }

        public async System.Threading.Tasks.Task OKProcessingPart2(bool isOnline, OfflineStoreType offlineLocation = OfflineStoreType.SAVE, bool show8HourWarning = false)
        {
            Log(
                $"OKProcessingPart2 BEGIN: isOnline={isOnline}, offlineLocation={offlineLocation}, show8HourWarning={show8HourWarning}",
                "WS_TRACE");

            isSaving = true;
            AllValid = ValidateSections();

            Log($"OKProcessingPart2: AllValid={AllValid}", "WS_TRACE");

            if ((!AllValid) && (PreviousEncounterStatus == (int)EncounterStatusType.Completed))
            {
                IsBusy = false;
                LaunchHavenErrorWindow(HavenErrorList);
                CurrentEncounter.Signed = PreviousSigned;
                CurrentEncounter.EncounterStatus = PreviousEncounterStatus;

                Log("OKProcessingPart2 END: NOT AllValid, NO save, returning", "WS_TRACE");

                return;
            }

            if (!ValidEnoughToSave)
            {
                IsBusy = false;
                CurrentEncounter.Signed = PreviousSigned;
                CurrentEncounter.EncounterStatus = PreviousEncounterStatus;
                //preventive measure in case something goes wrong, white screen, SL runtime dumps, etc...

                await SaveDynamicForm(
                    (CurrentPatient != null) ? CurrentPatient.PatientKey : -1,
                    (CurrentEncounter != null) ? CurrentEncounter.EncounterKey : -1,
                    (CurrentEncounter != null) ? CurrentEncounter.EncounterStatus : (int)EncounterStatusType.Edit,
                    offlineLocation,
                    false);

                if (ValidEnoughToSaveShowErrorDialog)
                {
                    NavigateCloseDialog d = new NavigateCloseDialog();
                    if (d != null)
                    {
                        d.NoVisible = false;
                        d.YesButton.Content = "OK";
                        d.Title = "Warning";
                        d.Width = double.NaN;
                        d.Height = double.NaN;
                        d.ErrorMessage =
                            "This Encounter contains validation errors preventing the form save from completing." +
                            Environment.NewLine + "The Form data has NOT been saved.";
                        d.Show();
                    }
                }

                Log("OKProcessingPart2 END: (!ValidEnoughToSave)", "WS_TRACE");
                return;
            }

            if (!AllValid)
            {
                // Remove all signatures and set the status back if all validations didn't pass, then continue with the save.
                SignaturesToAdd.Clear();
                foreach (EncounterSignature es in CurrentEncounter.EncounterSignature)
                    if (es.IsNew)
                    {
                        FormModel.RemoveSignature(es);
                        SignaturesToAdd.Add(es);
                    }

                InterimSigned = CurrentEncounter.Signed;
                InterimEncounterStatus = CurrentEncounter.EncounterStatus;

                CurrentEncounter.Signed = PreviousSigned;
                CurrentEncounter.EncounterStatus = PreviousEncounterStatus;
            }

            NavigateCloseDialog ncd = new NavigateCloseDialog();
            if ((show8HourWarning) && (ncd != null) && WarnForEncounterActualTimeTooBig(AllValid))
            {
                ncd.Title = "Alert: Encounter visit time exceeds 8 hours";
                ncd.Width = double.NaN;
                ncd.Height = double.NaN;
                ncd.ButtonYes.Content = "Yes";
                ncd.ButtonYes.Width = double.NaN;
                ncd.NoVisible = true;
                ncd.ButtonNo.Content = "No";
                ncd.ButtonNo.Width = double.NaN;
                ncd.ErrorMessage =
                    String.Format("Service Start Date/Time:    {0}{1}", CurrentEncounter.EncounterStartDateAndTimeText,
                        Environment.NewLine) +
                    String.Format("Service End Date/Time:      {0}{1}", CurrentEncounter.EncounterEndDateAndTimeText,
                        Environment.NewLine) +
                    String.Format("Total Visit Time in minutes: {0}{1}{2}",
                        CurrentEncounter.EncounterActualTime.ToString().Trim(), Environment.NewLine,
                        Environment.NewLine) +
                    String.Format("Is this correct? {0}{1}", Environment.NewLine, Environment.NewLine);
                ncd.Closed += async (s, err) =>
                {
                    NavigateCloseDialog _ncd = (NavigateCloseDialog)s;
                    // if the user is fine with the 8+ hour actual time, log that fact and continue OKpocessing, otherwise return the user to the form
                    if (_ncd.DialogResult.GetValueOrDefault())
                    {
                        LogChangeHistoryForEncounterActualTimeTooBig();
                        await OKProcessingPart3(isOnline, offlineLocation);
                        return;
                    }

                    // Return to the form
                    CurrentEncounter.Signed = PreviousSigned;
                    CurrentEncounter.EncounterStatus = PreviousEncounterStatus;
                    if (PreviousEncounterStatus != (int)EncounterStatusType.Completed) // Don't SIP completed forms
                    {
                        await SaveDynamicForm(
                            (CurrentPatient != null) ? CurrentPatient.PatientKey : -1,
                            (CurrentEncounter != null) ? CurrentEncounter.EncounterKey : -1,
                            (CurrentEncounter != null)
                                ? CurrentEncounter.EncounterStatus
                                : (int)EncounterStatusType.Edit,
                            offlineLocation,
                            false);
                    }

                    IsBusy = false;
                };
                ncd.Show();
            }
            else
            {
                await OKProcessingPart3(isOnline, offlineLocation);
            }

            Log("OKProcessingPart2 END", "WS_TRACE");
        }

        private bool WarnForEncounterActualTimeTooBig(bool AllValid)
        {
            // Don't warn if form is inactive
            if (CurrentEncounter.Inactive)
            {
                return false;
            }

            // Don't warn if form did not pass validation
            if (AllValid == false)
            {
                return false;
            }

            // Don't warn if actual time is not too big
            // Assume that if an EncounterActualTime is calculated that this is an encounter that collects visit statistics
            if ((CurrentEncounter.EncounterActualTime == null) || (CurrentEncounter.EncounterActualTime <= 480))
            {
                return false;
            }

            // Display warning if we are dealing with an interested user
            // Warn if the form is in Clinician edit and it is that clinician's form and we are doing a Full Validation - Incude NTUC because it collects visit stats
            if (CurrentEncounter.PreviousEncounterStatusIsInEdit &&
                (CurrentEncounter.FullValidationNTUC || CurrentEncounter.Signed) &&
                ((CurrentEncounter.EncounterBy == WebContext.Current.User.MemberID)))
            {
                return true;
            }

            // Warn if this is an administrator editing a completed form - as they have access to the visit statistics data - like Encounter Actual Time
            if ((CurrentEncounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed) &&
                RoleAccessHelper.CheckPermission(RoleAccess.Admin, false))
            {
                return true;
            }

            return false;
        }

        private void LogChangeHistoryForEncounterActualTimeTooBig()
        {
            if (FormModel == null)
            {
                return;
            }

            ChangeHistory ch = new ChangeHistory
            {
                TenantID = CurrentEncounter.TenantID, UpdatedTable = "Encounter",
                UpdatedTableKey = CurrentEncounter.EncounterKey
            };
            FormModel.AddChangeHistory(ch);
            ChangeHistoryDetail chd1 = new ChangeHistoryDetail
            {
                TenantID = CurrentEncounter.TenantID, ChangedColumn = "EncounterActualTime",
                OriginalValue = CurrentEncounter.EncounterActualTime.ToString().Trim(),
                NewValue = CurrentEncounter.EncounterActualTime.ToString().Trim()
            };
            ch.ChangeHistoryDetail.Add(chd1);
        }

        public async System.Threading.Tasks.Task OKProcessingPart3(bool isOnline,
            OfflineStoreType offlineLocation = OfflineStoreType.SAVE)
        {
            Log($"OKProcessingPart3 BEGIN: isOnline={isOnline}, offlineLocation={offlineLocation}", "WS_TRACE");

            if ((AllValid == false) && (CurrentEncounter != null) && (CurrentEncounter.EncounterReview != null))
            {
                // backoff any form level EncounterReview rows 
                foreach (EncounterReview er in CurrentEncounter.EncounterReview.Reverse())
                    if ((er.IsNew) && (er.ReviewType != (int)EncounterReviewType.SectionReview))
                    {
                        FormModel.RemoveEncounterReview(er);
                    }
            }

            if ((CurrentEncounter.EncounterStatus == (int)EncounterStatusType.None ||
                 CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Edit) || !AllValid)
            {
                IsSIPSave = true;
                Log("OKProcessingPart3: SaveAdmissionStatusData", "WS_TRACE");
                SaveAdmissionStatusData();
            }

            if (isOnline) //only create 'history' rows when online and can save to server...
            {
                if (CurrentOasisManager != null)
                {
                    CurrentOasisManager.BypassMapping = true;
                }

                Log("OKProcessingPart3: SaveAllergies...SaveAdmissionEquipment", "WS_TRACE");

                SaveAllergies();
                SaveDiagnosis();
                SaveMedications();
                SaveLevelOfCare();
                SaveLabs();
                SavePain();
                SaveIVs();
                SaveWounds();
                SaveGoals();
                SaveDisciplineFrequencies();
                SaveInfections2();
                SaveInfections();
                SavePatientAdverseEvents();
                SaveAdmissionConsent();
                SaveAdmissionEquipment();
                if (CurrentOasisManager != null)
                {
                    CurrentOasisManager.BypassMapping = false;
                }
            }

            CommitAllOpenEdits();

            SetCompletedIfNeedBe();

            if (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed)
            {
                if ((CurrentEncounter.EncounterIsOrderEntry) && (CurrentEncounter.CurrentOrderEntry != null))
                {
                    if ((!CurrentTask.TaskEndDateTime.HasValue) && (CurrentEncounter.CurrentOrderEntry.OrderStatus ==
                                                                    (int)OrderStatusType.Voided))
                    {
                        CurrentTask.TaskEndDateTime = DateTimeOffset.Now;
                    }
                    else if ((CurrentTask.TaskEndDateTime.HasValue) &&
                             (CurrentEncounter.CurrentOrderEntry.OrderStatus != (int)OrderStatusType.Voided))
                    {
                        CurrentTask.TaskEndDateTime = null;
                        CurrentTask.CanceledAt = null;
                    }
                }
            }

            if (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
            {
                RaisePropertyChangedPrintStuff();


                if (CurrentAdmission.HospiceAdmission && (CurrentAdmission.DeathDate != null) &&
                    (CurrentEncounter.EncounterIsTeamMeeting))
                {
                    CurrentAdmission.UpdatedDate =
                        DateTime.Now; // Tickle me a teamMeeting after death so the server calls sp_CreateSurvivorsFromPotentialSurvivors
                }
                // Note - if we are not going to auto-close on change to EncounterStatus 
                //        we will need to iterate over the quesions and paise "Protected" property changed

                if (!CurrentTask.TaskEndDateTime.HasValue)
                {
                    CurrentTask.TaskEndDateTime = DateTimeOffset.Now;
                    CurrentTask.CanceledAt = null;
                    CurrentTask.CancelReasonKey = null;

                    EncounterTransfer transfer = GetMostRecentTransfer();

                    if (CurrentForm.IsDischarge)
                    {
                        if (CurrentAdmissionDiscipline.DischargeDateTime.HasValue &&
                            (CurrentEncounter.EncounterStartDate.HasValue == false))
                        {
                            CurrentEncounter.EncounterStartDate =
                                ((DateTime)CurrentAdmissionDiscipline.DischargeDateTime).Date;
                            CurrentEncounter.EncounterStartTime = new DateTime(1900, 1, 1);
                        }

                        if ((CurrentAdmission.HospiceAdmission == false) &&
                            (CurrentAdmissionDiscipline.DischargeDateTime.HasValue))
                        {
                            HomeHealthDischargeRoutine();
                        }
                        else if (CurrentAdmission.HospiceAdmission && (isHospiceDisciplineDischarge))
                        {
                            HospiceDisciplineDischargeRoutine();
                        }
                        else if (CurrentAdmission.HospiceAdmission && (isHospiceAgencyDischarge))
                        {
                            HospiceAgencyDischargeRoutine();
                        }

                        CurrentEncounter.RefreshDisciplineSummaryEncounterData(CurrentAdmissionDiscipline
                            .DischargeDateTime);
                    }
                    else if (CurrentForm.IsTransfer && CurrentEncounterTransfer.TransferDate != null)
                    {
                        if ((CurrentEncounterTransfer.TransferDate != null) &&
                            (CurrentEncounter.EncounterStartDate.HasValue == false))
                        {
                            CurrentEncounter.EncounterStartDate = CurrentEncounterTransfer.TransferDate.Date;
                            CurrentEncounter.EncounterStartTime = new DateTime(1900, 1, 1);
                        }

                        CurrentAdmission.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Transferred;
                        FormModel.DischargeAllDisciplines(CurrentEncounterTransfer.TransferDate,
                            CurrentEncounterTransfer.TransferReasonKey, CurrentEncounter.SummaryOfCareNarrative);
                        FormModel.RemoveTasksAfterDischarge(CurrentEncounterTransfer.TransferDate, null,
                            GetPOCRecertTaskCutoffDate());
                        if (CurrentEncounterTransfer.TransferDate != null)
                        {
                            FormModel.EndDateAllFCDOrdersForDiscipline(CurrentAdmissionDiscipline.DisciplineKey,
                                CurrentEncounterTransfer.TransferDate, true);
                        }

                        CurrentEncounter.RefreshDisciplineSummaryEncounterData(CurrentEncounterTransfer.TransferDate);
                    }
                    else if (CurrentForm.IsResumption || (
                                 CurrentForm.IsEval &&
                                 transfer != null &&
                                 CurrentEncounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date >=
                                 transfer.TransferDate.Date &&
                                 !CurrentAdmission.Encounter
                                     .Where(ec => ec.Form != null)
                                     .Where(e => e.EncounterStatus !=
                                                 (int)EncounterStatusType
                                                     .None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                                     .Where(ec =>
                                         ec.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date >=
                                         transfer.TransferDate.Date &&
                                         ec.EncounterKey != CurrentEncounter.EncounterKey &&
                                         (ec.Form.IsEval || ec.Form.IsResumption))
                                     .Any()
                             )
                            )
                    {
                        if (CurrentForm.IsResumption)
                        {
                            EncounterResumption er = CurrentEncounter.EncounterResumption.FirstOrDefault();
                            if ((er != null) && (er.ResumptionDate != null) &&
                                (CurrentEncounter.EncounterStartDate.HasValue == false))
                            {
                                CurrentEncounter.EncounterStartDate = ((DateTime)er.ResumptionDate).Date;
                                CurrentEncounter.EncounterStartTime = new DateTime(1900, 1, 1);
                            }
                        }

                        CurrentAdmission.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Resumed;
                        CurrentAdmissionDiscipline.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Admitted;
                    }
                    else
                    {
                        // Admit the Admission if this discipline is currently admited AND it hasn't already been done already.
                        if (CurrentAdmissionDiscipline.DisciplineAdmitDateTime.HasValue
                            && !CurrentAdmissionDiscipline.NotTakenDateTime.HasValue &&
                            !CurrentAdmissionDiscipline.DischargeDateTime.HasValue
                            && (!CurrentAdmission.AdmitDateTime.HasValue)
                            && CurrentAdmission.AdmissionStatus !=
                            (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "A")
                            && CurrentAdmission.AdmissionStatus !=
                            (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "M"))
                        {
                            // This code isn't hit during an eval because of logic in the Validate method of the AdmissionStatus backing class
                            CurrentAdmission.AdmissionStatus = CurrentAdmissionDiscipline.AdmissionStatus.Value;
                            CurrentAdmission.AdmitDateTime =
                                CurrentAdmissionDiscipline.DisciplineAdmitDateTime.Value.Date;
                        }
                    }
                }

                AdmissionPhysicianFacade phyf = new AdmissionPhysicianFacade(useEncounterAdmission: false);
                phyf.Admission = CurrentAdmission;
                phyf.Encounter = CurrentEncounter;

                EncounterAdmission ea = CurrentEncounter.EncounterAdmission.FirstOrDefault();
                if (ea == null)
                {
                    ea = new EncounterAdmission();
                    CurrentEncounter.EncounterAdmission.Add(ea);
                    ea.RefreshEncounterAdmissionFromAdmission(CurrentAdmission, phyf,
                        CurrentAdmissionDiscipline); // Copy Admission data to Encounter
                }
                else if (PreviousEncounterStatus != (int)EncounterStatusType.Completed)
                {
                    ea.RefreshEncounterAdmissionFromAdmission(CurrentAdmission, phyf,
                        CurrentAdmissionDiscipline); // Copy Admission data to Encounter
                }

                // Copy off Admission Discipline data in addition to Admission Data to EncounterAdmission.
                if (ea != null)
                {
                    SyncEncounterAdmissionAndOrderEntry();
                    ea.DisciplineStatus = CurrentAdmissionDiscipline.AdmissionStatus;
                    ea.DisciplineAdmitDateTime = CurrentAdmissionDiscipline.DisciplineAdmitDateTime.HasValue
                        ? CurrentAdmissionDiscipline.DisciplineAdmitDateTime.Value.Date
                        : CurrentAdmissionDiscipline.DisciplineAdmitDateTime;
                    ea.Admitted = false;
                    ea.NotTaken = false;
                    if (CurrentAdmissionDiscipline.AdmissionStatusCode == "A" || CurrentAdmissionDiscipline
                                                                                  .AdmissionStatusCode == "M"
                                                                              || ((CurrentForm.IsEval ||
                                                                                      CurrentForm.IsResumption) &&
                                                                                  CurrentAdmissionDiscipline.Admitted))
                    {
                        ea.Admitted = true;
                    }

                    if (CurrentAdmissionDiscipline.AdmissionStatusCode == "N"
                        || ((CurrentForm.IsEval || CurrentForm.IsResumption) && CurrentAdmissionDiscipline.NotTaken))
                    {
                        ea.NotTaken = true;
                        ea.NotTakenReason = CurrentAdmissionDiscipline.NotTakenReason;
                    }
                }
            }

            // If the admission is admitted (implies the Discipline EVAL if going to complete stats) - or -
            // this is an HSN Eval that is will be admitted whose status is going past edit (either review or complete)
            // then create the initial AdmissionTeamMeeting row so the patient gets on the TeamMeeting worklist
            // Note - the only way it can go to review is if it will be admitted - careful of case where it goes to complete due to NTUC
            // I.e., don't want to wait till the AdmissionDiscipline eval is complete - they want patients in review to appear on the worklist as well
            if ((CurrentAdmission.AdmissionStatus == (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "A")) ||
                ((CurrentAdmissionDiscipline.AdmissionStatus !=
                  (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "N")) &&
                 (CurrentAdmissionDiscipline.AdmissionDisciplineIsSN) &&
                 (CurrentEncounter.EncounterStatus > 1) && (CurrentForm.IsEval)))
            {
                // Add a new AdmissionTeamMeeting row for tracking if one doesn't already exist.
                InitializeTeamMeeting();
            }

            if (CurrentOasisManager != null)
            {
                CurrentOasisManager.OKProcessing(AllValid, isOnline);
                CurrentOasisManager.IsBusy = true;
            }

            if (CurrentOrderEntryManager != null)
            {
                // OrderEntryManager is responsible for firing DynamicForm save
                Func<System.Threading.Tasks.Task> callback = () => FinalSave(isOnline);
                await CurrentOrderEntryManager.OKProcessing(AllValid, isOnline, callback);
            }
            else
            {
                // Save DynamicForm and SubmitChanges
                await FinalSave(isOnline);
            }

            Log("OKProcessingPart3 END", "WS_TRACE");
        }

        private void SetCompletedIfNeedBe()
        {
            if ((CurrentForm == null) || (CurrentEncounter == null))
            {
                return;
            }

            if ((CurrentForm.IsEval == false) && (CurrentForm.IsVisit == false) && (CurrentForm.IsResumption == false))
            {
                return;
            }

            if (PreviousEncounterStatus > 1)
            {
                return;
            }

            if (CurrentEncounter.EncounterStatus <= (int)EncounterStatusType.Edit)
            {
                return;
            }

            // Set CompletedDateTime and CompletedBy as the encounter goes from edit to a review or complete
            CurrentEncounter.CompletedDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            CurrentEncounter.CompletedBy = WebContext.Current.User.MemberID;
        }

        private DateTime? GetAdmissionDischargeDateTime()
        {
            DateTime? dischageDate = null;
            if ((CurrentAdmission != null) && (CurrentAdmission.AdmissionDiscipline != null))
            {
                dischageDate = CurrentAdmission.AdmissionDiscipline
                    .Where(ad => ad.DischargeDateTime.HasValue && !ad.NotTakenDateTime.HasValue)
                    .Select(ad => ad.DischargeDateTime).Max();
            }

            if (dischageDate != null)
            {
                return ((DateTime)dischageDate).Date;
            }

            if ((CurrentAdmissionDiscipline != null) && (CurrentAdmissionDiscipline.DischargeDateTime != null))
            {
                return ((DateTime)CurrentAdmissionDiscipline.DischargeDateTime).Date;
            }

            return DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
        }

        private void HomeHealthDischargeRoutine()
        {
            CurrentAdmissionDiscipline.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Discharged;

            if (CurrentAdmissionDiscipline.AgencyDischarge)
            {
                CurrentAdmissionDiscipline.SummaryOfCareNarrative =
                    (CurrentEncounter == null) ? null : CurrentEncounter.SummaryOfCareNarrative;
                // New agency discharge
                if (CurrentAdmission != null)
                {
                    if ((CurrentPatient != null) && (CurrentPatient.DeathDate != null) &&
                        (CurrentAdmission.DeathDate == null))
                    {
                        CurrentAdmission.DeathDate = CurrentPatient.DeathDate;
                        CurrentAdmission.DeathTime = ((DateTime)CurrentPatient.DeathDate).Date;
                    }

                    // propagate discharge (or NTUC) status to each admission discipline
                    if (CurrentAdmission.AdmissionDiscipline != null)
                    {
                        foreach (AdmissionDiscipline ad in CurrentAdmission.AdmissionDiscipline)
                            if (ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Discharged)
                            {
                                // Already discharged - confirm discharge dependent fields
                                if (ad.DischargeDateTime == null)
                                {
                                    ad.DischargeDateTime = CurrentAdmissionDiscipline.DischargeDateTime;
                                }

                                if (ad.DischargeReasonKey == null)
                                {
                                    ad.DischargeReasonKey = CurrentAdmissionDiscipline.DischargeReasonKey;
                                }

                                if (ad.SummaryOfCareNarrative == null)
                                {
                                    ad.SummaryOfCareNarrative = (CurrentEncounter == null)
                                        ? null
                                        : CurrentEncounter.SummaryOfCareNarrative;
                                }

                                ad.NotTakenDateTime = null;
                                ad.NotTakenReason = null;
                            }
                            else if (ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_NotTaken)
                            {
                                // Already notTaken - confirm notTaken dependent fields
                                ad.DischargeDateTime = null;
                                ad.DischargeReasonKey = null;
                                ad.SummaryOfCareNarrative = null;
                                if (ad.NotTakenDateTime == null)
                                {
                                    ad.NotTakenDateTime = CurrentAdmissionDiscipline.DischargeDateTime;
                                }

                                if (ad.NotTakenReason == null)
                                {
                                    ad.NotTakenReason = "Home Health Agency Discharge of " +
                                                        CurrentAdmissionDiscipline.ReasonDCCodeDescription;
                                }
                            }
                            else if ((ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Referred) ||
                                     (ad.AdmissionStatus ==
                                      AdmissionStatusHelper
                                          .AdmissionStatus_OnHold)) // OnHold should never happen - as it is an admission status only
                            {
                                ad.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_NotTaken;
                                ad.DischargeDateTime = null;
                                ad.DischargeReasonKey = null;
                                ad.SummaryOfCareNarrative = null;
                                ad.NotTakenDateTime = CurrentAdmissionDiscipline.DischargeDateTime;
                                ad.NotTakenReason = "Home Health Agency Discharge of " +
                                                    CurrentAdmissionDiscipline.ReasonDCCodeDescription;
                            }
                            else if ((ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Admitted) ||
                                     (ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Transferred) ||
                                     (ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Resumed))
                            {
                                ad.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Discharged;
                                ad.DischargeDateTime = CurrentAdmissionDiscipline.DischargeDateTime;
                                ad.DischargeReasonKey = CurrentAdmissionDiscipline.DischargeReasonKey;
                                ad.SummaryOfCareNarrative = (CurrentEncounter == null)
                                    ? null
                                    : CurrentEncounter.SummaryOfCareNarrative;
                                ad.NotTakenDateTime = null;
                                ad.NotTakenReason = null;
                            }
                    }

                    CurrentAdmission.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Discharged;
                    CurrentAdmission.DischargeDateTime = GetAdmissionDischargeDateTime();
                    CurrentAdmission.DischargeReasonKey = CurrentAdmissionDiscipline.DischargeReasonKey;
                    CurrentAdmission.NotTakenDateTime = null;
                    CurrentAdmission.NotTakenReason = null;
                }

                // cancel tasks and end date FCDs
                if (FormModel != null)
                {
                    FormModel.RemoveTasksAfterDischarge((DateTime)CurrentAdmissionDiscipline.DischargeDateTime, null,
                        GetPOCRecertTaskCutoffDate());
                }

                if (FormModel != null)
                {
                    FormModel.EndDateAllFCDOrdersForDiscipline(CurrentAdmissionDiscipline.DisciplineKey,
                        (DateTime)CurrentAdmissionDiscipline.DischargeDateTime, true);
                }
            }
            else if (!CurrentAdmission.AdmissionDiscipline
                         .Where(p => !p.DischargeDateTime.HasValue && !p.NotTakenDateTime.HasValue).Any())
            {
                // Old Agency discharge - becuase last discipline admitted
                DateTime? disch = GetAdmissionDischargeDateTime();
                if (disch != null)
                {
                    CurrentAdmission.AdmissionStatus = CurrentAdmissionDiscipline.AdmissionStatus.Value;
                    CurrentAdmission.DischargeDateTime = disch.Value;
                    CurrentAdmission.DischargeReasonKey = CurrentAdmissionDiscipline.DischargeReasonKey;
                    CurrentAdmission.NotTakenDateTime = null;
                    CurrentAdmission.NotTakenReason = null;
                    CurrentAdmissionDiscipline.SummaryOfCareNarrative = (CurrentEncounter == null)
                        ? null
                        : CurrentEncounter.SummaryOfCareNarrative;

                    FormModel.RemoveTasksAfterDischarge(CurrentAdmissionDiscipline.DischargeDateTime.Value, null,
                        GetPOCRecertTaskCutoffDate());
                    if (CurrentAdmissionDiscipline.DischargeDateTime.HasValue)
                    {
                        FormModel.EndDateAllFCDOrdersForDiscipline(CurrentAdmissionDiscipline.DisciplineKey,
                            CurrentAdmissionDiscipline.DischargeDateTime.Value, true);
                    }
                }
            }
            else
            {
                CurrentAdmissionDiscipline.SummaryOfCareNarrative =
                    (CurrentEncounter == null) ? null : CurrentEncounter.SummaryOfCareNarrative;
                FormModel.RemoveTasksAfterDischarge(CurrentAdmissionDiscipline.DischargeDateTime.Value,
                    CurrentAdmissionDiscipline.DisciplineKey);
                if (CurrentAdmissionDiscipline.DischargeDateTime.HasValue)
                {
                    FormModel.EndDateAllFCDOrdersForDiscipline(CurrentAdmissionDiscipline.DisciplineKey,
                        CurrentAdmissionDiscipline.DischargeDateTime.Value, false);
                }
            }
        }

        private bool isHospiceDisciplineDischarge
        {
            get
            {
                if ((CurrentEncounter == null) || (CurrentAdmissionDiscipline == null) ||
                    (CurrentAdmissionDiscipline.HospiceDisciplineDischarge == null))
                {
                    return false;
                }

                if (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed)
                {
                    return false;
                }

                // Ignore previous Status - because of false-positive w.r.t. offline autoupload
                Server.Data.HospiceDisciplineDischarge hdd = CurrentAdmissionDiscipline.HospiceDisciplineDischarge
                    .FirstOrDefault();
                if (hdd == null)
                {
                    return false;
                }

                return true;
            }
        }

        private bool isHospiceAgencyDischarge
        {
            get
            {
                if ((CurrentEncounter == null) || (CurrentEncounter.AdmissionHospiceDischarge == null))
                {
                    return false;
                }

                if (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed)
                {
                    return false;
                }

                // Ignore previous Status - because of false-positive w.r.t. offline autoupload
                Server.Data.AdmissionHospiceDischarge ahd = CurrentEncounter.AdmissionHospiceDischarge.FirstOrDefault();
                if (ahd == null)
                {
                    return false;
                }

                return true;
            }
        }

        private void HospiceDisciplineDischargeRoutine()
        {
            if (isHospiceDisciplineDischarge == false)
            {
                return;
            }

            if ((CurrentAdmissionDiscipline != null) && (CurrentEncounter != null))
            {
                Server.Data.HospiceDisciplineDischarge hdd = CurrentAdmissionDiscipline.HospiceDisciplineDischarge
                    .Where(a => a.AddedFromEncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                if (hdd != null)
                {
                    CurrentAdmissionDiscipline.DischargeDateTime = (hdd.DisciplineDischargeDate == null)
                        ? (DateTime?)null
                        : ((DateTimeOffset)hdd.DisciplineDischargeDate).Date;
                    CurrentAdmissionDiscipline.DischargeReasonKey = hdd.ConvertedDisciplineDischargeReason;
                    CurrentAdmissionDiscipline.NotTakenDateTime = null;
                    CurrentAdmissionDiscipline.NotTakenReason = null;
                }
            }

            if (CurrentAdmissionDiscipline != null)
            {
                CurrentAdmissionDiscipline.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Discharged;
                // Just discharge current discipline
                FormModel.RemoveTasksAfterDischarge(CurrentAdmissionDiscipline.DischargeDateTime.Value,
                    CurrentAdmissionDiscipline.DisciplineKey);
                if (CurrentAdmissionDiscipline.DischargeDateTime.HasValue)
                {
                    FormModel.EndDateAllFCDOrdersForDiscipline(CurrentAdmissionDiscipline.DisciplineKey,
                        CurrentAdmissionDiscipline.DischargeDateTime.Value, false);
                }
            }
        }

        private void HospiceAgencyDischargeRoutine()
        {
            if (isHospiceAgencyDischarge == false)
            {
                return;
            }

            Server.Data.AdmissionHospiceDischarge ahd = CurrentEncounter.AdmissionHospiceDischarge.FirstOrDefault();
            if (ahd == null)
            {
                return;
            }

            ahd.TidyUpData();
            // propagate death date to patient   
            if (CurrentPatient != null)
            {
                CurrentPatient.DeathDate = (ahd.DeathDateTime == null)
                    ? (DateTime?)null
                    : ((DateTimeOffset)ahd.DeathDateTime).Date;
            }

            if (CurrentAdmission != null)
            {
                // propagate discharge (or NTUC) status to each admission discipline
                if (CurrentAdmission.AdmissionDiscipline != null)
                {
                    foreach (AdmissionDiscipline ad in CurrentAdmission.AdmissionDiscipline)
                        if ((ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Discharged) ||
                            (ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_NotTaken))
                        {
                        }
                        else if ((ad.AdmissionDisciplineIsPhysicianServices) &&
                                 ((ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Referred) ||
                                  (ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Admitted) ||
                                  (ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Transferred) ||
                                  (ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Resumed)))
                        {
                            ad.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Discharged;
                            ad.DischargeDateTime = ahd.DischargeDate;
                            ad.DischargeReasonKey = ahd.ConvertedDischargeReason;
                            ad.NotTakenDateTime = null;
                            ad.NotTakenReason = null;
                        }
                        else if ((ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Referred) ||
                                 (ad.AdmissionStatus ==
                                  AdmissionStatusHelper
                                      .AdmissionStatus_OnHold)) // OnHold should never happen - as it is an admission status only
                        {
                            ad.DischargeDateTime = null;
                            ad.DischargeReasonKey = null;
                            ad.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_NotTaken;
                            ad.NotTakenDateTime = ahd.DischargeDate;
                            ad.NotTakenReason = "Hospice Agency Discharge of " + ahd.DischargeReasonDesc;
                        }
                        else if ((ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Admitted) ||
                                 (ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Transferred) ||
                                 (ad.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Resumed))
                        {
                            ad.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Discharged;
                            ad.DischargeDateTime = ahd.DischargeDate;
                            ad.DischargeReasonKey = ahd.ConvertedDischargeReason;
                            ad.NotTakenDateTime = null;
                            ad.NotTakenReason = null;
                        }
                }

                // propagate death date time and discharge status to admission
                CurrentAdmission.DeathDate = (ahd.DeathDateTime == null)
                    ? (DateTime?)null
                    : ((DateTimeOffset)ahd.DeathDateTime).Date;
                CurrentAdmission.DeathTime = (ahd.DeathDateTime == null) ? null : ahd.DeathDateTime;
                CurrentAdmission.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Discharged;
                CurrentAdmission.DischargeDateTime = GetAdmissionDischargeDateTime();
                CurrentAdmission.DischargeReasonKey = ahd.ConvertedDischargeReason;
                CurrentAdmission.NotTakenDateTime = null;
                CurrentAdmission.NotTakenReason = null;
            }

            // cancel tasks and end date FCDs
            if (FormModel != null)
            {
                FormModel.RemoveTasksAfterDischarge(ahd.DischargeDate.Value, null, GetPOCRecertTaskCutoffDate());
            }

            if (FormModel != null)
            {
                FormModel.EndDateAllFCDOrdersForDiscipline(CurrentAdmissionDiscipline.DisciplineKey,
                    ahd.DischargeDate.Value, true);
            }
        }

        private async System.Threading.Tasks.Task FinalSave(bool isOnline)
        {
            Log($"FinalSave BEGIN: Save DynamicForm and SubmitChanges, isOnline={isOnline}", "WS_TRACE");

            HomePageAgencyOpsRefreshOption = CurrentAdmission.AdmissionGroupChanged
                ? HomePageAgencyOpsRefreshOptionEnum.All
                : HomePageAgencyOpsRefreshOptionEnum.SingleTask;

            if (AllValid)
            {
                CreatePOCTaskIfNeedBeForAdmission();
                CreatePOCTaskIfNeedBeForRecertification();
                UpdateHospiceElectionAddendumIfNeedBe();
            }

            RefreshAdmissionCOTISignatureDateIfNeedBe();

            RefreshOrdersTrackingRow();
            //preventive measure in case something goes wrong, white screen, SL runtime dumps, etc...
            //SaveDynamicForm(this.CurrentPatient.PatientKey, this.CurrentEncounter.EncounterKey, this.CurrentEncounter.ENcounterStatus, OfflineStoreType.SAVE);

            // Save should be taken care of in OKProcessing, DynamicFormInfo object does not play well with reverted values
            await SaveDynamicForm(
                (CurrentPatient != null) ? CurrentPatient.PatientKey : -1,
                (CurrentEncounter != null) ? CurrentEncounter.EncounterKey : -1,
                (CurrentEncounter != null) ? CurrentEncounter.EncounterStatus : (int)EncounterStatusType.Edit,
                OfflineStoreType.SAVE,
                false);

#if DEBUG
            CheckModelForChanges("OKProcessingPart2");
#endif
            if (isOnline)
            {
                if (AdministrativeServiceDateOverride)
                {
                    var ret = ValidateUpdateDisciplineAdmissionKey();
                    if (string.IsNullOrEmpty(ret))
                    {
                        FormModel.SaveMultiAsync(() =>
                        {
                            Log("FinalSave: FinalSave000", "WS_TRACE");
                            LogChangeSet("FinalSave000");
                        });
                    }
                    else
                    {
                        var msgBox = new VirtuosoMessageBox(ret);
                        msgBox.Closed += (s, err) =>
                        {
                            FormModel.SaveMultiAsync(() =>
                            {
                                Log("FinalSave: FinalSave001", "WS_TRACE");
                                LogChangeSet("FinalSave001");
                            });
                        };
                        msgBox.Show();
                    }
                }
                else
                {
                    FormModel.SaveMultiAsync(() =>
                    {
                        Log("FinalSave: FinalSave002", "WS_TRACE");
                        LogChangeSet("FinalSave002");
                    });
                }
            }
            else
            {
                await FormSavedInternal(_is_Online: isOnline);
                DataSaved();
            }

            Log("FinalSave END", "WS_TRACE");
        }

        private void CreatePOCTaskIfNeedBeForAdmission()
        {
            // if form isEval and POC Task does not exists yet and this is the first admitted SN,PT,OT or SLP discipline - create the POC task
            if ((CurrentEncounter == null) || (CurrentForm == null) || (CurrentAdmission == null) ||
                (CurrentAdmission.Task == null) || (CurrentAdmissionDiscipline == null))
            {
                return;
            }

            if (CurrentForm.IsEval == false)
            {
                return;
            }
            
            if (CurrentAdmission.InsuranceRequiresDisciplineOrders == false)
            {
                return;
            }

            if (CurrentEncounter.PreviousEncounterStatusIsInEdit == false)
            {
                return;
            }

            if (CurrentEncounter.FullValidation == false)
            {
                return;
            }

            if (CurrentAdmission.Task.Where(t => (t.TaskIsPlanOfCare && (t.CanceledAt == null))).Any())
            {
                return;
            }

            if (CurrentAdmissionDiscipline.AdmissionStatusIsAdmitted == false)
            {
                return;
            }

            if (CurrentAdmissionDiscipline.AdmissionDisciplineIsOTorPTorSLPorSN == false)
            {
                return;
            }

            CreatePOCTask();
        }

        private void CreatePOCTaskIfNeedBeForRecertification()
        {
            // When an admission is still in a status of admitted and the admissioncertificationperiod has reached the Discipline Recertification window
            // (ServiceLine.DisciplineRecertWindow OR TenantSetting.DisciplineRecertWindow) and the next AdmissionCertification period row exists -
            // automatically create The Plan of Care task is d for the admission for the upcoming cert period

            // Flunk conditions....
            if ((CurrentEncounter == null) || (CurrentForm == null) || (CurrentAdmission == null) ||
                (CurrentAdmission.Task == null) || (CurrentAdmissionDiscipline == null))
            {
                return;
            }

            if ((CurrentForm.IsEval == false) && (CurrentForm.IsResumption == false) && (CurrentForm.IsVisit == false))
            {
                return;
            }

            if (CurrentAdmission.HospiceAdmission)
            {
                return;
            }

            if (CurrentAdmission.InsuranceRequiresDisciplineOrders == false)
            {
                return;
            }

            if (CurrentEncounter.PreviousEncounterStatusIsInEdit == false)
            {
                return;
            }

            if (CurrentEncounter.FullValidation == false)
            {
                return;
            }

            if ((CurrentAdmissionDiscipline.AdmissionStatusIsAdmitted == false) &&
                (CurrentAdmissionDiscipline.AdmissionStatusIsResumed == false))
            {
                return;
            }

            if (CurrentAdmissionDiscipline.AdmissionDisciplineIsOTorPTorSLPorSN == false)
            {
                return;
            }

            if (CurrentEncounter.GetIsEncounterInRecertWindow(CurrentAdmission, CurrentEncounter) == false)
            {
                return; // Flunk if encounter not in Discipline Recertification window
            }

            DateTime _certDateCheck = CurrentEncounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date
                .AddDays(DisciplineRecertWindow);
            AdmissionCertification nextAC = CurrentAdmission.GetAdmissionCertForDate(_certDateCheck, false);
            if (nextAC?.PeriodStartDate == null)
            {
                return; // Flunk if next AdmissionCertification period row d.n.e.
            }

            // Flunk if POC alread exists
            if (CurrentAdmission.Task.Where(t =>
                    (t.TaskIsPlanOfCare && (t.CanceledAt == null) && (t.TaskStartDateTime >=
                                                                      nextAC.PeriodStartDate.Value.Date.AddDays(
                                                                          -DisciplineRecertWindow)))).Any())
            {
                return;
            }

            CreatePOCTask();
        }

        private void CreatePOCTask()
        {
            ServiceLine sl = ServiceLineCache.GetServiceLineFromKey(CurrentAdmission.ServiceLineKey);
            if (sl == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sl.PlanOfCareTasks) == false)
            {
                string[] pocTasks = sl.PlanOfCareTasks.ToUpper()
                    .Split(pipeDelimiters, StringSplitOptions.RemoveEmptyEntries);
                if (pocTasks.Length != 0)
                {
                    bool pocCreated = false;
                    foreach (string t in pocTasks)
                    {
                        if (t == "A")
                        {
                            pocCreated = CreatePOCTaskForHCFACode(null); // PlanOfCareTasks = A = CareCoordinator
                        }
                        else if (t == "B")
                        {
                            pocCreated = CreatePOCTaskForHCFACode("A"); // PlanOfCareTasks = B = SN
                        }
                        else if (t == "C")
                        {
                            pocCreated = CreatePOCTaskForHCFACode("B"); // PlanOfCareTasks = C = PT
                        }
                        else if (t == "D")
                        {
                            pocCreated = CreatePOCTaskForHCFACode("D"); // PlanOfCareTasks = D = SLP
                        }
                        else if (t == "E")
                        {
                            pocCreated = CreatePOCTaskForHCFACode("C"); // PlanOfCareTasks = E = OT
                        }

                        if (pocCreated)
                        {
                            return;
                        }
                    }
                }
            }

            // fell thru - create POC - assigned to the care coordinator against the CurrentAdmissionDiscipline
            CreatePOCTaskForHCFACode(null);
        }

        private void UpdateHospiceElectionAddendumIfNeedBe()
        {
            if ((CurrentEncounter == null) || (CurrentForm == null) || (CurrentForm.IsHospiceElectionAddendum == false))
            {
                return;
            }

            EncounterAdmission
                ea = CurrentEncounter.EncounterAdmission.FirstOrDefault(); // so we wait until the form is complete
            if (ea == null)
            {
                return;
            }

            EncounterHospiceElectionAddendum ehea = CurrentEncounter.EncounterHospiceElectionAddendum.FirstOrDefault();
            if ((ehea == null) || ehea.RequiresSignature)
            {
                return;
            }

            int? NonHospiceKey = CodeLookupCache.GetCodeLookupKeyFromCodeTypeAndCode("ADNOTSIGN", "NonHospice");
            if (NonHospiceKey == null)
            {
                return;
            }

            // auso set HospiceElectionAddendum as No Dated Signature present - reason NonHospice
            ehea.DateFurnished = ehea.CreateDate;
            ehea.DatedSignaturePresent = false;
            ehea.SignatureDate = null;
            ehea.ReasonNotSigned = NonHospiceKey;
            ehea.RefusalReason = null;
        }

        private void RefreshAdmissionCOTISignatureDateIfNeedBe()
        {
            if ((CurrentEncounter == null) || (CurrentForm == null) || (CurrentAdmission == null) ||
                (CurrentAdmission.AdmissionCOTI == null) || (CurrentEncounter.AdmissionCOTI == null))
            {
                return;
            }

            if (CurrentForm.IsCOTI == false)
            {
                return;
            }

            AdmissionCOTI ac = CurrentEncounter.AdmissionCOTI.FirstOrDefault();
            if (ac == null)
            {
                return;
            }

            if (ac.SignatureDate != null)
            {
                return;
            }

            if (AllValid == false)
            {
                return;
            }

            if (CurrentEncounter.PreviousEncounterStatusIsInEdit == false)
            {
                return;
            }

            if (CurrentEncounter.FullValidation == false)
            {
                return;
            }

            if (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed)
            {
                return;
            }

            EncounterSignature es = (CurrentEncounter.EncounterSignature == null)
                ? null
                : CurrentEncounter.EncounterSignature.FirstOrDefault();
            if ((es != null) && (es.SignatureDate != null))
            {
                ac.SignatureDate = es.SignatureDate;
            }

            if (ac.SignatureDate != null)
            {
                return;
            }

            ac.SignatureDate = ac.AttestationDate;
        }

        private string[] pipeDelimiters = { "|" };

        private bool CreatePOCTaskForHCFACode(string hcfaCode)
        {
            if ((CurrentAdmission == null) || (CurrentAdmission.Task == null) || (CurrentAdmissionDiscipline == null))
            {
                return false;
            }

            Task t = new Task
            {
                TaskStartDateTime = DateTimeOffset.Now, PatientKey = CurrentAdmission.PatientKey,
                AdmissionKey = CurrentAdmission.AdmissionKey
            };
            if (string.IsNullOrWhiteSpace(hcfaCode))
            {
                // create POC - assigned to the care coordinator against the CurrentAdmissionDiscipline
                // iff the care coordinator has access to that discipline
                if (CurrentAdmission.CareCoordinator == null)
                {
                    return false;
                }

                if (UserCache.Current.IsDisciplineInUserProfile(CurrentAdmissionDiscipline.DisciplineKey,
                        (Guid)CurrentAdmission.CareCoordinator) == false)
                {
                    return false;
                }

                ServiceType st =
                    ServiceTypeCache.GetPOCServiceTypeForDiscipline(CurrentAdmissionDiscipline.DisciplineKey);
                if (st == null)
                {
                    return false;
                }

                t.UserID = (Guid)CurrentAdmission.CareCoordinator;
                t.ServiceTypeKey = st.ServiceTypeKey;
                CurrentAdmission.Task.Add(t);
                return true;
            }

            // Create POC task - against the discipline of the given hcfaCode passed
            List<AdmissionDiscipline> adList = CurrentAdmission.AdmissionDiscipline.Where(p =>
                ((p.AdmissionStatusIsAdmitted || p.AdmissionStatusIsResumed) &&
                 p.AdmissionDisciplineIsHCFACode(hcfaCode) && (p.PrimaryCareGiver != null))).ToList();
            if (adList == null)
            {
                return false;
            }

            foreach (AdmissionDiscipline ad in adList)
            {
                ServiceType st = ServiceTypeCache.GetPOCServiceTypeForDiscipline(ad.DisciplineKey);
                if (st == null)
                {
                    return false;
                }

                t.UserID = (Guid)ad.PrimaryCareGiver;
                t.ServiceTypeKey = st.ServiceTypeKey;
                CurrentAdmission.Task.Add(t);
                return true;
            }

            return false;
        }

        private void InitializeTeamMeeting()
        {
            if (CurrentAdmission.SOCDate == null)
            {
                return;
            }

            // Pass true to the calculate to use the earliest possible date instead of the schedule.
            if (CurrentForm.IsEval && CurrentAdmission.HospiceAdmission &&
                CurrentAdmission.AdmissionTeamMeeting.Any() == false)
            {
                DateTime? nextDate = null;
                var sl = CurrentAdmission.HospiceServiceLineGroupHeader;
                if (sl != null && sl.ServiceLineGrouping != null && sl.ServiceLineGrouping.Any())
                {
                    var slg = CurrentAdmission.GetServiceLineGroupingForTeamMeeting((DateTime)CurrentAdmission.SOCDate);
                    if (slg != null)
                    {
                        DateTime tomorrow = DateTime.Today.AddDays(1).Date;
                        DateTime socDatePlusOne = (CurrentAdmission.SOCDate == null)
                            ? tomorrow
                            : ((((DateTime)CurrentAdmission.SOCDate).Date < tomorrow)
                                ? tomorrow
                                : ((DateTime)CurrentAdmission.SOCDate).Date);
                        nextDate = DateCalculationHelper.CalculateNextTeamMeetingDate(socDatePlusOne,
                            slg.ServiceLineGroupingKey, CurrentPatient.LastName, true);
                    }
                }

                if (nextDate != null)
                {
                    AdmissionTeamMeeting at = new AdmissionTeamMeeting
                    {
                        LastTeamMeetingDate = InitialFirstTeamMeetingDate,
                        NextTeamMeetingDate = nextDate,
                    };
                    CurrentAdmission.AdmissionTeamMeeting.Add(at);
                }
            }
        }

        public DateTime InitialFirstTeamMeetingDate
        {
            get
            {
                if ((CurrentAdmission == null) && (CurrentAdmission.SOCDate != null))
                {
                    return ((DateTime)CurrentAdmission.SOCDate).Date;
                }

                return DateTime.Today.Date;
            }
        }


        public bool ValidEnoughToSave = true;
        public bool ValidEnoughToSaveShowErrorDialog = true;

        private bool ValidateSections()
        {
            ValidEnoughToSave = true;
            ValidEnoughToSaveShowErrorDialog = true;
            bool AllValid = true;
            List<SectionUI> sectionList = Sections.ToList();
            foreach (var section in sectionList) section.Errors = false;

            bool isOasisValidated = false;
            foreach (SectionUI section in sectionList)
                ValidateIndividualSection(ref AllValid, ref isOasisValidated, section);
            if (MSPManager != null)
            {
                foreach (var section in MSPManager.SectionsToValidate)
                    ValidateIndividualSection(ref AllValid, ref isOasisValidated, section);
            }

            return AllValid;
        }

        private void ValidateIndividualSection(ref bool AllValid, ref bool isOasisValidated, SectionUI section)
        {
            if ((section == null) || (section.IsSectionVisible == false) || section.AlwaysHideSection ||
                (section.Questions == null))
            {
                return;
            }

            foreach (QuestionUI q in section.Questions)
            {
                string SubSections = string.Empty;

                if ((section.IsOasis) && (section.IsOasisAlert == false))
                {
                    if (q.Question.DataTemplate == "OasisSectionLabel")
                    {
                        q.Validate(out SubSections); // needed - not really to validate it - but to add and EncounterReview rows
                    }

                    // Need to validate  once - when the first  section is encountered - (note: which is ALWAYS before the generic  Alters section)
                    if ((CurrentOasisManager != null) && (isOasisValidated == false))
                    {
                        isOasisValidated = true;
                        string oasisSections = null;
                        if (CurrentOasisManager.Validate(Sections.ToList(), out oasisSections) == false)
                        {
                            AllValid = false;

                            if (!ValidationMessage.Contains(oasisSections))
                            {
                                if (string.IsNullOrEmpty(ValidationMessage))
                                {
                                    if (CurrentEncounter.Signed)
                                    {
                                        ValidationMessage = "Encounter has not been completed.  ";
                                    }

                                    ValidationMessage +=
                                        "Please inspect the following highlighted section(s) for errors: " +
                                        oasisSections;
                                }
                                else
                                {
                                    ValidationMessage += ", " + oasisSections;
                                }
                            }
                        }
                    }
                }
                else
                {
                    //only validate if the section was active when entering the form 
                    //and we only know that based on the current state of the print button

                    //The Signed bit won’t work because we need to know the state the form was in when it was opened – or 
                    //actually the state of the form the last time it was saved.  Meaning if you open a new form and sign it all at 
                    //once we want to run all the validations because all the fields were available when opened and not use whether 
                    //or not it is currently signed (being signed makes a difference in the validations but not in determining whether 
                    //to run the validations).  

                    if (!q.Validate(out SubSections))
                    {
                        section.Errors = true;
                        string message = (section.IsOasis == false)
                            ? section.Label
                            : CurrentEncounter.SYS_CDDescription + " " + section.Label;
                        if (!string.IsNullOrEmpty(SubSections))
                        {
                            message += "(" + SubSections + ")";
                        }

                        if (!ValidationMessage.Contains(message))
                        {
                            if (string.IsNullOrEmpty(ValidationMessage))
                            {
                                if (CurrentEncounter.Signed)
                                {
                                    ValidationMessage = "Encounter has not been completed.  ";
                                }

                                ValidationMessage +=
                                    "Please inspect the following highlighted section(s) for errors: " + message;
                            }
                            else
                            {
                                ValidationMessage += ", " + message;
                            }
                        }

                        AllValid = false;
                    }
                }
            }

            SectionCrossFieldValidate(ref AllValid, section);
        }

        public void SectionCrossFieldValidate(ref bool AllValid, SectionUI section)
        {
            if ((CurrentEncounter == null) || (CurrentEncounter.FullValidation == false) || (section == null) ||
                (section.IsSectionVisible == false) || section.AlwaysHideSection || (section.Questions == null))
            {
                return;
            }

            string methodName = section.Label.Replace(" ", "");
            methodName = methodName.Replace("/", "");
            MethodInfo crossFieldValidate = GetType().GetMethod("SectionCrossFieldValidate" + methodName);
            if (crossFieldValidate == null)
            {
                return;
            }

            try
            {
                if ((bool)crossFieldValidate.Invoke(this, new Object[] { section }) == false)
                {
                    AllValid = false;
                    section.Errors = true;
                    string message = (section.IsOasis == false)
                        ? section.Label
                        : CurrentEncounter.SYS_CDDescription + " " + section.Label;
                    if (!ValidationMessage.Contains(message))
                    {
                        if (string.IsNullOrEmpty(ValidationMessage))
                        {
                            if (CurrentEncounter.Signed)
                            {
                                ValidationMessage = "Encounter has not been completed.  ";
                            }

                            ValidationMessage += "Please inspect the following highlighted section(s) for errors: " +
                                                 message;
                        }
                        else
                        {
                            ValidationMessage += ", " + message;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public bool SectionCrossFieldValidateEligibilityStatus(SectionUI section)
        {
            bool sectionValid = true;
            // Under Criteria 1: Confined to the Home: either a) Due to illness or injury, or b) Patient's condition, must be answered.
            QuestionBase DueToIllness = GetQuestion(section, "a) Due to illness or injury, the patient requires",
                "CodeLookupMultiComment", "QuestionBase");
            QuestionBase PatientCondition = GetQuestion(section, "b) Patient's condition is such that leaving home",
                "CodeLookupMultiComment", "QuestionBase");
            if ((DueToIllness?.EncounterData != null) &&
                string.IsNullOrWhiteSpace(DueToIllness.EncounterData.TextData) &&
                (PatientCondition?.EncounterData != null) &&
                string.IsNullOrWhiteSpace(PatientCondition.EncounterData.TextData))
            {
                DueToIllness.EncounterData.ValidationErrors.Add(new ValidationResult(
                    "Under Criteria 1: Confined to the Home: either a) Due to illness or injury, or b) Patient's condition, must be answered.",
                    new[] { "TextData" }));
                PatientCondition.EncounterData.ValidationErrors.Add(new ValidationResult(
                    "Under Criteria 1: Confined to the Home: either a) Due to illness or injury, or b) Patient's condition, must be answered.",
                    new[] { "TextData" }));
                sectionValid = false;
            }

            // Under Medical Necessity: at least one of the four questions: Hospitalization due to, Exacerbation of, Event or New Diagnosis, must be answered.
            QuestionBase Hospitalization = GetQuestion(section, "Hospitalization due to", "Text", "QuestionBase");
            QuestionBase Exacerbation = GetQuestion(section, "Exacerbation of", "Text", "QuestionBase");
            QuestionBase Event = GetQuestion(section, "Event", "Text", "QuestionBase");
            QuestionBase NewDiagnosis = GetQuestion(section, "New Diagnosis", "Text", "QuestionBase");
            if ((Hospitalization?.EncounterData != null) &&
                string.IsNullOrWhiteSpace(Hospitalization.EncounterData.TextData) &&
                (Exacerbation?.EncounterData != null) &&
                string.IsNullOrWhiteSpace(Exacerbation.EncounterData.TextData) &&
                (Event?.EncounterData != null) && string.IsNullOrWhiteSpace(Event.EncounterData.TextData) &&
                (NewDiagnosis?.EncounterData != null) && string.IsNullOrWhiteSpace(NewDiagnosis.EncounterData.TextData))
            {
                Hospitalization.EncounterData.ValidationErrors.Add(new ValidationResult(
                    "Under Medical Necessity: at least one of the four questions: Hospitalization due to, Exacerbation of, Event or New Diagnosis, must be answered.",
                    new[] { "TextData" }));
                Exacerbation.EncounterData.ValidationErrors.Add(new ValidationResult(
                    "Under Medical Necessity: at least one of the four questions: Hospitalization due to, Exacerbation of, Event or New Diagnosis, must be answered.",
                    new[] { "TextData" }));
                Event.EncounterData.ValidationErrors.Add(new ValidationResult(
                    "Under Medical Necessity: at least one of the four questions: Hospitalization due to, Exacerbation of, Event or New Diagnosis, must be answered.",
                    new[] { "TextData" }));
                NewDiagnosis.EncounterData.ValidationErrors.Add(new ValidationResult(
                    "Under Medical Necessity: at least one of the four questions: Hospitalization due to, Exacerbation of, Event or New Diagnosis, must be answered.",
                    new[] { "TextData" }));
                sectionValid = false;
            }

            return sectionValid;
        }

        private QuestionBase GetQuestion(SectionUI Section, string Label, string DataTemplate, string BackingFactory)
        {
            QuestionBase qb = null;
            try
            {
                QuestionUI qui = Section.Questions.Where(q =>
                        ((q.Question != null) && (q.Hidden == false) && (q.Protected == false) &&
                         (q.Question.Label != null) && (q.Question.Label.StartsWith(Label)) &&
                         (q.Question.DataTemplate == DataTemplate) && (q.Question.BackingFactory == BackingFactory)))
                    .FirstOrDefault();
                qb = (qui == null) ? null : qui as QuestionBase;
            }
            catch
            {
            }

            return qb;
        }

        private void SaveDisciplineFrequencies()
        {
            // iterate backwards to avoid contentsion with new version adds
            foreach (var curr_frequency in CurrentAdmission.AdmissionDisciplineFrequency.ToList())
            {
                AdmissionDisciplineFrequency freq = curr_frequency;
                if (freq.IsNew)
                {
                    EncounterDisciplineFrequency eexist = freq.EncounterDisciplineFrequency
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    freq.CancelEditting();
                    if (freq.HistoryKey == null)
                    {
                        CurrentEncounter.AdmissionDisciplineFrequency.Add(freq); // to fill out AddedFromEncounterKey
                    }

                    EncounterDisciplineFrequency edf = new EncounterDisciplineFrequency();
                    CurrentEncounter.EncounterDisciplineFrequency.Add(edf);
                    freq.EncounterDisciplineFrequency.Add(edf);
                    CurrentAdmission.EncounterDisciplineFrequency.Add(edf);
                    CurrentPatient.EncounterDisciplineFrequency.Add(edf);
                }
                else if (freq.IsModified && !freq.Inactive /* && !freq.IsInvalid */)
                {
                    // remove the old version from the encounter
                    EncounterDisciplineFrequency edf = CurrentEncounter.EncounterDisciplineFrequency
                        .Where(p => p.DispFreqKey == freq.DisciplineFrequencyKey).FirstOrDefault();
                    if (edf != null)
                    {
                        // clone for history if need be - remove the old version from the encounter
                        AdmissionDisciplineFrequency newadf = freq.CreateNewVersion();
                        if (CurrentEncounter != null)
                        {
                            newadf.AddedFromEncounterKey = CurrentEncounter.EncounterKey;
                        }

                        ((IPatientService)FormModel).Remove(edf);

                        // add the new version to the encounter
                        EncounterDisciplineFrequency edf2 = new EncounterDisciplineFrequency();
                        CurrentEncounter.EncounterDisciplineFrequency.Add(edf2);
                        newadf.EncounterDisciplineFrequency.Add(edf2);
                        CurrentAdmission.EncounterDisciplineFrequency.Add(edf2);
                        CurrentPatient.EncounterDisciplineFrequency.Add(edf2);

                        // otherwise FCDs added in AdmissionMaint get there AddedFromEncounterKey set here on a edit - CurrentEncounter.AdmissionDisciplineFrequency.Add(newadf); - 
                        // finally - and this MUST BE LAST - add it to the patient - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentAdmission.AdmissionDisciplineFrequency.Add(newadf);

                        var sectionUI = Sections.Where(s =>
                                s.Questions.Any(q =>
                                    !(String.IsNullOrEmpty(q.Label)) && q.Label.Equals("Visit Frequency ")))
                            .FirstOrDefault();
                        if (sectionUI != null)
                        {
                            var questionUI = sectionUI.Questions.Where(q => q.Label.Equals("Visit Frequency "))
                                .FirstOrDefault();
                            var vstUI = questionUI as FrequenceCycleDuration;
                            vstUI.FCDListView.Refresh();
                        }
                    }
                }
            }
        }

        private void SaveGoals()
        {
            foreach (var curr_goal in CurrentAdmission.AdmissionGoal)
            {
                foreach (var curr_goalelement in curr_goal.AdmissionGoalElement.ToList())
                    if ((curr_goalelement.HasChanges) &&
                        (curr_goalelement.AddedFromEncounterKey != CurrentEncounter.EncounterKey) &&
                        (curr_goalelement.IsNew == false) && (curr_goalelement.AdmissionGoalElementKey > 0))
                    {
                        if (curr_goalelement.CurrentEncounterGoalElement == null)
                        {
                            curr_goalelement.CurrentEncounterGoalElement = curr_goalelement.EncounterGoalElement
                                .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                        }

                        bool attached = curr_goalelement.CurrentEncounterGoalElement != null;
                        var OldGoalElement = curr_goalelement.CurrentEncounterGoalElement;
                        // remove the old version from the encounter
                        if (attached)
                        {
                            ((IPatientService)FormModel).Remove(curr_goalelement.CurrentEncounterGoalElement);
                            curr_goalelement.CurrentEncounterGoalElement = null;
                        }

                        // add the new version to the encounter
                        AdmissionGoalElement newgoalelement = curr_goalelement.CreateNewVersion();
                        if (attached)
                        {
                            EncounterGoalElement ege = new EncounterGoalElement();
                            if (OldGoalElement != null)
                            {
                                ege.Addressed = OldGoalElement.Addressed;
                                ege.Planned = OldGoalElement.Planned;
                                ege.Comment = OldGoalElement.Comment;
                            }

                            CurrentEncounter.EncounterGoalElement.Add(ege);

                            newgoalelement.CurrentEncounterGoalElement = ege;
                            newgoalelement.EncounterGoalElement.Add(ege);
                            ege.PopuulateEncounterGoalElementDisciplines(newgoalelement.GoalElementDisciplineKeys);
                        }

                        curr_goal.AdmissionGoalElement.Add(newgoalelement);
                    }

                if (curr_goal.HasChanges && curr_goal.AddedFromEncounterKey != CurrentEncounter.EncounterKey)
                {
                    if (curr_goal.CurrentEncounterGoal == null)
                    {
                        curr_goal.CurrentEncounterGoal = curr_goal.EncounterGoal
                            .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    }

                    if (curr_goal.CurrentEncounterGoal != null)
                    {
                        // remove the old version from the encounter
                        ((IPatientService)FormModel).Remove(curr_goal.CurrentEncounterGoal);
                    }

                    curr_goal.CurrentEncounterGoal = null;

                    // add the new version to the encounter
                    EncounterGoal eg = new EncounterGoal();
                    CurrentEncounter.EncounterGoal.Add(eg);

                    AdmissionGoal newgoal = curr_goal.CreateNewVersion();
                    newgoal.CurrentEncounterGoal = eg;
                    newgoal.EncounterGoal.Add(eg);
                    CurrentAdmission.AdmissionGoal.Add(newgoal);

                    foreach (var age in curr_goal.AdmissionGoalElement)
                    {
                        EncounterGoalElement ege = new EncounterGoalElement();
                        age.CurrentEncounterGoalElement = age.EncounterGoalElement
                            .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                        if (age.CurrentEncounterGoalElement != null)
                        {
                            ege.Addressed = age.CurrentEncounterGoalElement.Addressed;
                            ege.Comment = age.CurrentEncounterGoalElement.Comment;
                            ege.Planned = age.CurrentEncounterGoalElement.Planned;

                            ((IPatientService)FormModel).Remove(age.CurrentEncounterGoalElement);

                            // add the new version to the encounter
                            CurrentEncounter.EncounterGoalElement.Add(ege);
                        }

                        AdmissionGoalElement newgoalelement = age.CreateNewVersion();
                        if (age.CurrentEncounterGoalElement != null)
                        {
                            newgoalelement.CurrentEncounterGoalElement = ege;
                            newgoalelement.EncounterGoalElement.Add(ege);
                        }

                        newgoal.AdmissionGoalElement.Add(newgoalelement);
                        ege.PopuulateEncounterGoalElementDisciplines(age.GoalElementDisciplineKeys);
                    }
                }
            }
        }

        private void CreateNewVersionWounds()
        {
            List<AdmissionWoundSite> awsList = CurrentAdmission.AdmissionWoundSite.Where(p => !p.Superceded).ToList();
            foreach (AdmissionWoundSite wound in awsList)
                if (((wound.HealedDate != null) && (wound.HealedLocked == false)) || (wound.Depth != null) ||
                    (wound.Width != null) || (wound.Length != null))
                {
                    AdmissionWoundSite newwound = wound.CreateNewVersion();
                    if ((newwound.HealedDate != null) && (newwound.HealedLocked == false))
                    {
                        newwound.HealedLocked = true;
                    }

                    if (newwound.Depth != null)
                    {
                        newwound.Depth = null;
                    }

                    if (newwound.Width != null)
                    {
                        newwound.Width = null;
                    }

                    if (newwound.Length != null)
                    {
                        newwound.Length = null;
                    }

                    newwound.Superceded = false;
                    CurrentAdmission.AdmissionWoundSite.Add(newwound);
                }
        }

        private void SaveIVs()
        {
            List<AdmissionIVSite> aisList = CurrentAdmission.AdmissionIVSite.ToList();
            foreach (var curr_iv in aisList)
            {
                AdmissionIVSite iv = curr_iv;
                if (iv.IsNew)
                {
                    EncounterIVSite eexist = iv.EncounterIVSite
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    iv.CancelEditting();
                    if (iv.HistoryKey == null)
                    {
                        CurrentEncounter.AdmissionIVSite.Add(iv); // to fill out AddedFromEncounterKey
                    }

                    //  if not part of the encounter already - add it
                    if (CurrentEncounter.EncounterIVSite.Where(p => p.AdmissionIVSite == iv).FirstOrDefault() == null)
                    {
                        EncounterIVSite ei = new EncounterIVSite();
                        CurrentEncounter.EncounterIVSite.Add(ei);
                        iv.EncounterIVSite.Add(ei);
                    }
                }
                else if ((iv.HasChanges) &&
                         (CurrentEncounter.EncounterIVSite.Where(p => p.AdmissionIVSite == iv).FirstOrDefault() !=
                          null)) // HasChanges and is part of the encounter
                {
                    // remove the old version from the encounter
                    EncounterIVSite eiv = CurrentEncounter.EncounterIVSite.Where(p => p.AdmissionIVSite == iv)
                        .FirstOrDefault();
                    if (eiv != null)
                    {
                        // clone for history if need be - remove the old version from the encounter
                        AdmissionIVSite newiv = iv.CreateNewVersion();
                        ((IPatientService)FormModel).Remove(eiv);
                        // add the new version to the encounter
                        EncounterIVSite ei = new EncounterIVSite();
                        CurrentEncounter.EncounterIVSite.Add(ei);
                        newiv.EncounterIVSite.Add(ei);
                        // finally - and this MUST BE LAST - add it to the admission - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentAdmission.AdmissionIVSite.Add(newiv);
                    }
                }
            }
        }

        private void SaveWounds()
        {
            List<AdmissionWoundSite> awsList = CurrentAdmission.AdmissionWoundSite.ToList();
            foreach (var curr_wound in awsList)
            {
                AdmissionWoundSite wound = curr_wound;
                if (wound.IsNew)
                {
                    EncounterWoundSite eexist = wound.EncounterWoundSite
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    wound.CancelEditting();
                    if (wound.HistoryKey == null)
                    {
                        CurrentEncounter.AdmissionWoundSite.Add(wound); // to fill out AddedFromEncounterKey
                    }

                    if (CurrentEncounter.EncounterWoundSite.Where(p => p.AdmissionWoundSite == wound)
                            .FirstOrDefault() == null) //  if not part of the encounter already - add it
                    {
                        EncounterWoundSite ew = new EncounterWoundSite();
                        CurrentEncounter.EncounterWoundSite.Add(ew);
                        wound.EncounterWoundSite.Add(ew);
                    }
                }
                else if ((wound.HasChanges) && (CurrentEncounter.EncounterWoundSite
                                                    .Where(p => p.AdmissionWoundSite == wound).FirstOrDefault() !=
                                                null)) // HasChanges and is part of the encounter
                {
                    // clone for history if need be - remove the old version from the encounter
                    AdmissionWoundSite newwound = wound.CreateNewVersion();
                    // remove the old version from the encounter
                    EncounterWoundSite ewound = CurrentEncounter.EncounterWoundSite
                        .Where(p => p.AdmissionWoundSite == wound).FirstOrDefault();
                    if (ewound != null)
                    {
                        ((IPatientService)FormModel).Remove(ewound);
                    }

                    // add the new version to the encounter
                    EncounterWoundSite ew = new EncounterWoundSite();
                    CurrentEncounter.EncounterWoundSite.Add(ew);
                    newwound.EncounterWoundSite.Add(ew);
                    // finally - and this MUST BE LAST - add it to the admission - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                    CurrentAdmission.AdmissionWoundSite.Add(newwound);
                }
            }
        }

        private void SaveLevelOfCare()
        {
            // iterate backwards to avoid contentsion with new version adds
            //for (int i = CurrentAdmission.AdmissionLevelOfCare.Count - 1; i >= 0; i--)
            foreach (var curr_loc in CurrentAdmission.AdmissionLevelOfCare.ToList())
            {
                AdmissionLevelOfCare loc = curr_loc;
                if (loc.IsNew)
                {
                    EncounterLevelOfCare eexist = loc.EncounterLevelOfCare
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    loc.CancelEditting();
                    if (loc.HistoryKey == null)
                    {
                        CurrentEncounter.AdmissionLevelOfCare.Add(loc); // to fill out AddedFromEncounterKey
                    }

                    EncounterLevelOfCare eloc = new EncounterLevelOfCare();
                    CurrentEncounter.EncounterLevelOfCare.Add(eloc);
                    loc.EncounterLevelOfCare.Add(eloc);
                }
                else if (loc.HasChanges)
                {
                    // remove the old version from the encounter
                    EncounterLevelOfCare eloc = loc.EncounterLevelOfCare
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eloc != null)
                    {
                        // clone for history if need be - remove the old version from the encounter
                        AdmissionLevelOfCare newloc = loc.CreateNewVersion();
                        ((IPatientService)FormModel).Remove(eloc);
                        // add the new version to the encounter
                        EncounterLevelOfCare el = new EncounterLevelOfCare();
                        CurrentEncounter.EncounterLevelOfCare.Add(el);
                        newloc.EncounterLevelOfCare.Add(el);
                        // finally - and this MUST BE LAST - add it to the admission - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentAdmission.AdmissionLevelOfCare.Add(newloc);
                    }
                }
            }
        }

        private void SaveInfections2()
        {
            // iterate backwards to avoid contentsion with new version adds
            //for (int i = CurrentAdmission.AdmissionLevelOfCare.Count - 1; i >= 0; i--)
            foreach (var curr_inf in CurrentAdmission.Patient.PatientInfection.ToList())
            {
                PatientInfection inf = curr_inf; // CurrentAdmission.AdmissionLevelOfCare.ToList()[i];
                if (inf.IsNew /*&& !loc.IsInvalid*/)
                {
                    EncounterPatientInfection eexist = inf.EncounterPatientInfection
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    inf.CancelEditting();
                    if (inf.HistoryKey == null)
                    {
                        CurrentEncounter.PatientInfection.Add(inf); // to fill out AddedFromEncounterKey
                    }

                    EncounterPatientInfection einf = new EncounterPatientInfection();
                    CurrentEncounter.EncounterPatientInfection.Add(einf);
                    inf.EncounterPatientInfection.Add(einf);
                }
                else if (inf.HasChanges)
                {
                    // remove the old version from the encounter
                    EncounterPatientInfection einf = inf.EncounterPatientInfection
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (einf != null)
                    {
                        // clone for history if need be - remove the old version from the encounter
                        PatientInfection newinf = inf.CreateNewVersion();
                        ((IPatientService)FormModel).Remove(einf);
                        // add the new version to the encounter
                        EncounterPatientInfection ai = new EncounterPatientInfection();
                        CurrentEncounter.EncounterPatientInfection.Add(ai);
                        newinf.EncounterPatientInfection.Add(ai);
                        // finally - and this MUST BE LAST - add it to the patient - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentPatient.PatientInfection.Add(newinf);
                        RaisePropertyChanged("CurrentPatient.PatientInfection");
                    }
                }
            }
        }

        private void SaveInfections()
        {
            // iterate backwards to avoid contentsion with new version adds
            //for (int i = CurrentAdmission.AdmissionLevelOfCare.Count - 1; i >= 0; i--)
            foreach (var curr_inf in CurrentAdmission.AdmissionInfection.ToList())
            {
                AdmissionInfection inf = curr_inf; // CurrentAdmission.AdmissionLevelOfCare.ToList()[i];
                if (inf.IsNew /*&& !loc.IsInvalid*/)
                {
                    EncounterInfection eexist = inf.EncounterInfection
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    inf.CancelEditting();
                    if (inf.HistoryKey == null)
                    {
                        CurrentEncounter.AdmissionInfection.Add(inf); // to fill out AddedFromEncounterKey
                    }

                    EncounterInfection einf = new EncounterInfection();
                    CurrentEncounter.EncounterInfection.Add(einf);
                    inf.EncounterInfection.Add(einf);
                }
                else if (inf.HasChanges)
                {
                    // remove the old version from the encounter
                    EncounterInfection einf = inf.EncounterInfection
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (einf != null)
                    {
                        // clone for history if need be - remove the old version from the encounter
                        AdmissionInfection newinf = inf.CreateNewVersion();
                        ((IPatientService)FormModel).Remove(einf);
                        // add the new version to the encounter
                        EncounterInfection ai = new EncounterInfection();
                        CurrentEncounter.EncounterInfection.Add(ai);
                        newinf.EncounterInfection.Add(ai);
                        // finally - and this MUST BE LAST - add it to the admission - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentAdmission.AdmissionInfection.Add(newinf);
                        RaisePropertyChanged("CurrentAdmission.AdmissionInfection");
                    }
                }
            }
        }

        private void SavePatientAdverseEvents()
        {
            // iterate backwards to avoid contentsion with new version adds
            foreach (var curr_pae in CurrentAdmission.Patient.PatientAdverseEvent.ToList())
            {
                PatientAdverseEvent pae = curr_pae;
                if (pae.IsNew)
                {
                    EncounterPatientAdverseEvent eexist = pae.EncounterPatientAdverseEvent
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    pae.CancelEditting();
                    if (pae.HistoryKey == null)
                    {
                        CurrentEncounter.PatientAdverseEvent.Add(pae); // to fill out AddedFromEncounterKey
                    }

                    EncounterPatientAdverseEvent epae = new EncounterPatientAdverseEvent();
                    CurrentEncounter.EncounterPatientAdverseEvent.Add(epae);
                    pae.EncounterPatientAdverseEvent.Add(epae);
                }
                else if (pae.HasChanges)
                {
                    // remove the old version from the encounter
                    EncounterPatientAdverseEvent epae = pae.EncounterPatientAdverseEvent
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (epae != null)
                    {
                        // clone for history if need be - remove the old version from the encounter
                        PatientAdverseEvent newpae = pae.CreateNewVersion();
                        ((IPatientService)FormModel).Remove(epae);
                        // add the new version to the encounter
                        EncounterPatientAdverseEvent newepae = new EncounterPatientAdverseEvent();
                        CurrentEncounter.EncounterPatientAdverseEvent.Add(newepae);
                        newpae.EncounterPatientAdverseEvent.Add(newepae);
                        // finally - and this MUST BE LAST - add it to the patient - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentPatient.PatientAdverseEvent.Add(newpae);
                        RaisePropertyChanged("CurrentPatient.PatientAdverseEvent");
                    }
                }
            }
        }

        private void SaveLabs()
        {
            // iterate backwards to avoid contentsion with new version adds
            //for (int i = CurrentAdmission.AdmissionLevelOfCare.Count - 1; i >= 0; i--)
            foreach (var curr_pl in CurrentPatient.PatientLab.ToList())
            {
                PatientLab pl = curr_pl;
                if (pl.IsNew)
                {
                    EncounterLab eexist = pl.EncounterLab.Where(p => p.EncounterKey == CurrentEncounter.EncounterKey)
                        .FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    pl.CancelEditting();
                    if (pl.HistoryKey == null)
                    {
                        CurrentEncounter.PatientLab.Add(pl); // to fill out AddedFromEncounterKey
                    }

                    EncounterLab el = new EncounterLab();
                    CurrentEncounter.EncounterLab.Add(el);
                    pl.EncounterLab.Add(el);
                }
                else if (pl.HasChanges)
                {
                    // remove the old version from the encounter
                    EncounterLab el = pl.EncounterLab.Where(p => p.EncounterKey == CurrentEncounter.EncounterKey)
                        .FirstOrDefault();
                    if (el != null)
                    {
                        // clone for history if need be - remove the old version from the encounter
                        PatientLab newpl = pl.CreateNewVersion();
                        ((IPatientService)FormModel).Remove(el);
                        // add the new version to the encounter
                        EncounterLab newel = new EncounterLab();
                        CurrentEncounter.EncounterLab.Add(newel);
                        newpl.EncounterLab.Add(newel);
                        // finally - and this MUST BE LAST - add it to the patient - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentPatient.PatientLab.Add(newpl);
                    }
                }
            }
        }

        private void SavePain()
        {
            // iterate backwards to avoid contentsion with new version adds
            foreach (var curr_pain in CurrentAdmission.AdmissionPainLocation.ToList())
            {
                AdmissionPainLocation pain = curr_pain;
                if (pain.IsNew)
                {
                    EncounterPainLocation eexist = pain.EncounterPainLocation
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    pain.CancelEditting();
                    if (pain.HistoryKey == null)
                    {
                        CurrentEncounter.AdmissionPainLocation.Add(pain); // to fill out AddedFromEncounterKey
                    }

                    EncounterPainLocation ep = new EncounterPainLocation();
                    CurrentEncounter.EncounterPainLocation.Add(ep);
                    pain.EncounterPainLocation.Add(ep);
                }
                else if (pain.HasChanges)
                {
                    // remove the old version from the encounter
                    EncounterPainLocation epain = pain.EncounterPainLocation
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (epain != null)
                    {
                        // clone for history if need be - remove the old version from the encounter
                        AdmissionPainLocation newpain = pain.CreateNewVersion();
                        ((IPatientService)FormModel).Remove(epain);
                        // add the new version to the encounter
                        EncounterPainLocation ep = new EncounterPainLocation();
                        CurrentEncounter.EncounterPainLocation.Add(ep);
                        newpain.EncounterPainLocation.Add(ep);
                        // finally - and this MUST BE LAST - add it to the admission - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentAdmission.AdmissionPainLocation.Add(newpain);
                    }
                }
            }
        }

        private void SaveMedications()
        {
            // iterate backwards to avoid contentsion with new version adds
            foreach (var curr_medication in CurrentPatient.PatientMedication.ToList())
            {
                PatientMedication med = curr_medication;
                if (med.IsNew)
                {
                    EncounterMedication eexist = med.EncounterMedication
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    med.CancelEditting();
                    if (med.HistoryKey == null)
                    {
                        CurrentEncounter.PatientMedication.Add(med); // to fill out AddedFromEncounterKey
                    }

                    EncounterMedication em = new EncounterMedication();
                    CurrentEncounter.EncounterMedication.Add(em);
                    med.EncounterMedication.Add(em);
                    CurrentPatient.EncounterMedication.Add(em);
                }
                else if (med.HasChanges)
                {
                    // remove the old version from the encounter
                    EncounterMedication emed = med.EncounterMedication
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (emed != null)
                    {
                        // clone for history if need be - remove the old version from the encounter
                        PatientMedication newmed = med.CreateNewVersion();
                        ((IPatientService)FormModel).Remove(emed);
                        // move any PatientMedicationAdministrationMed for this med to the newmed
                        if (CurrentEncounter != null)
                        {
                            List<PatientMedicationAdministrationMed> pmamlist = med.PatientMedicationAdministrationMed
                                .Where(m => m.EncounterKey == CurrentEncounter.EncounterKey).ToList();
                            foreach (PatientMedicationAdministrationMed pmam in pmamlist)
                            {
                                med.PatientMedicationAdministrationMed.Remove(pmam);
                                newmed.PatientMedicationAdministrationMed.Add(pmam);
                            }
                        }

                        // move any PatientMedicationReconcileMed for this med to the newmed
                        if (CurrentEncounter != null)
                        {
                            List<PatientMedicationReconcileMed> pmrmlist = med.PatientMedicationReconcileMed
                                .Where(m => m.EncounterKey == CurrentEncounter.EncounterKey).ToList();
                            foreach (PatientMedicationReconcileMed pmrm in pmrmlist)
                            {
                                med.PatientMedicationReconcileMed.Remove(pmrm);
                                newmed.PatientMedicationReconcileMed.Add(pmrm);
                            }
                        }

                        // move any PatientMedicationTeachingMed for this med to the newmed
                        if (CurrentEncounter != null)
                        {
                            List<PatientMedicationTeachingMed> pmtmlist = med.PatientMedicationTeachingMed
                                .Where(m => m.EncounterKey == CurrentEncounter.EncounterKey).ToList();
                            foreach (PatientMedicationTeachingMed pmtm in pmtmlist)
                            {
                                med.PatientMedicationTeachingMed.Remove(pmtm);
                                newmed.PatientMedicationTeachingMed.Add(pmtm);
                            }
                        }

                        // move any PatientMedicationManagementMed for this med to the newmed
                        if (CurrentEncounter != null)
                        {
                            List<PatientMedicationManagementMed> pmtmlist = med.PatientMedicationManagementMed
                                .Where(m => m.EncounterKey == CurrentEncounter.EncounterKey).ToList();
                            foreach (PatientMedicationManagementMed pmmm in pmtmlist)
                            {
                                med.PatientMedicationManagementMed.Remove(pmmm);
                                newmed.PatientMedicationManagementMed.Add(pmmm);
                            }
                        }

                        // copy any PatientMedicationSlidingScale for this med to the newmed
                        List<PatientMedicationSlidingScale> sslist = med.PatientMedicationSlidingScale.ToList();
                        if ((sslist != null) && sslist.Any())
                        {
                            foreach (PatientMedicationSlidingScale ss in sslist)
                                newmed.PatientMedicationSlidingScale.Add(ss.CreateNewVersion());
                        }

                        // move any AdmissionMedicationMARs for this med to the newmed  //bfm
                        if ((med != null) && (med.AdmissionMedicationMAR != null) && (newmed != null) &&
                            (newmed.AdmissionMedicationMAR != null))
                        {
                            List<AdmissionMedicationMAR> ammlist = med.AdmissionMedicationMAR
                                .Where(amm => amm.AddedFromEncounterKey == CurrentEncounter.EncounterKey).Reverse().ToList();
                            if ((ammlist != null) && ammlist.Any())
                            {
                                foreach (AdmissionMedicationMAR amm in ammlist)
                                {
                                    med.AdmissionMedicationMAR.Remove(amm);
                                    amm.PatientMedicationKey = 0;
                                    newmed.AdmissionMedicationMAR.Add(amm);
                                }
                            }
                        }

                        // add the new version to the encounter
                        EncounterMedication em = new EncounterMedication();
                        CurrentEncounter.EncounterMedication.Add(em);
                        newmed.EncounterMedication.Add(em);
                        CurrentPatient.EncounterMedication.Add(em);
                        // finally - and this MUST BE LAST - add it to the patient - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentPatient.PatientMedication.Add(newmed);
                    }
                }
            }

            // If the encounter was not yet completed - set/reset the HighRiskMedication for each medication in this encounter
            if (CurrentEncounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed)
            {
                foreach (EncounterMedication em in CurrentEncounter.EncounterMedication)
                {
                    PatientMedication pm = CurrentPatient.PatientMedication
                        .Where(p => p.PatientMedicationKey == em.MedicationKey).FirstOrDefault();
                    em.HighRiskMedication =
                        (pm != null) ? pm.IsHighRiskMedicationFromEncounter(CurrentEncounter) : false;
                }
            }
        }

        private void SaveDiagnosis()
        {
            if ((CurrentAdmission != null) && (CurrentEncounter != null) && (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed))
            {
                CurrentAdmission.Resequence(CurrentEncounter); // Keep this resequence here for diagnosis changes outside the edit popup
            }

            // iterate backwards to avoid contentsion with new version adds
            foreach (AdmissionDiagnosis curr_diagnosis in CurrentAdmission.AdmissionDiagnosis.Reverse())
            {
                if (curr_diagnosis.IsNew && (curr_diagnosis.RemovedDate != null))
                {
                    ((IPatientService)FormModel).Remove(curr_diagnosis);
                }
            }

            var diagnosisListToProcess = CurrentAdmission.AdmissionDiagnosis.ToList();
            DateTime now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            foreach (AdmissionDiagnosis curr_diagnosis in diagnosisListToProcess)
            {
                AdmissionDiagnosis diagnosis = curr_diagnosis;
                if (diagnosis.IsNew)
                {
                    EncounterDiagnosis eexist = diagnosis.EncounterDiagnosis
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    diagnosis.EndEditting();
                    if (diagnosis.HistoryKey == null)
                    {
                        CurrentEncounter.AdmissionDiagnosis.Add(diagnosis); // to fill out AddedFromEncounterKey
                    }

                    EncounterDiagnosis ed = new EncounterDiagnosis();
                    CurrentEncounter.EncounterDiagnosis.Add(ed);
                    diagnosis.EncounterDiagnosis.Add(ed);
                    CurrentAdmission.EncounterDiagnosis.Add(ed);
                }
                else if (diagnosis.HasChanges)
                {
                    // clone for history if need be - remove the old version from the encounter
                    EncounterDiagnosis ediag = diagnosis.EncounterDiagnosis
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (ediag != null)
                    {
                        AdmissionDiagnosis newDiagnosis = diagnosis.CreateNewVersion(CurrentEncounter.EncounterKey);
                        ((IPatientService)FormModel).Remove(ediag);
                        // add the new version to the encounter (only if it was there originally)
                        EncounterDiagnosis ed = new EncounterDiagnosis();
                        CurrentEncounter.EncounterDiagnosis.Add(ed);
                        newDiagnosis.EncounterDiagnosis.Add(ed);
                        CurrentAdmission.EncounterDiagnosis.Add(ed);
                        // finally - and this MUST BE LAST - add it to the patient - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentAdmission.AdmissionDiagnosis.Add(newDiagnosis);
                    }
                }
            }

            // Remove empty Diagnosis Comments
            List<PatientDiagnosisComment> pdcList =
                CurrentPatient.PatientDiagnosisComment.Where(p =>
                    (string.IsNullOrWhiteSpace(p.Comment) &&
                     (p.IsNew || (p.AddedFromEncounterKey == CurrentEncounter.EncounterKey)))).ToList();
            foreach (PatientDiagnosisComment diagnosisComment in pdcList)
                FormModel.RemovePatientDiagnosisComment(diagnosisComment);

            List<PatientDiagnosisComment> diagnosisCommentListToProcess =
                CurrentPatient.PatientDiagnosisComment.ToList();
            foreach (PatientDiagnosisComment diagnosisComment in diagnosisCommentListToProcess)
                if (diagnosisComment.IsNew)
                {
                    try
                    {
                        diagnosisComment.CancelEditting();
                    }
                    catch
                    {
                    }

                    CurrentEncounter.PatientDiagnosisComment.Add(diagnosisComment);
                    EncounterDiagnosisComment edc = new EncounterDiagnosisComment();
                    CurrentEncounter.EncounterDiagnosisComment.Add(edc);
                    diagnosisComment.EncounterDiagnosisComment.Add(edc);
                    CurrentPatient.EncounterDiagnosisComment.Add(edc);
                }
        }

        private void SaveAllergies()
        {
            // iterate backwards to avoid contentsion with new version adds
            //for (int i = CurrentPatient.PatientAllergy.Count - 1; i >= 0; i--)
            var allergyListToProcess = CurrentPatient.PatientAllergy.Where(p => (!p.Inactive)).ToList();
            foreach (var curr_allergy in allergyListToProcess)
            {
                PatientAllergy allergy = curr_allergy;
                if (allergy.IsNew)
                {
                    EncounterAllergy eexist = allergy.EncounterAllergy
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    allergy.EndEditting();
                    if (allergy.HistoryKey == null)
                    {
                        CurrentEncounter.PatientAllergy.Add(allergy); // to fill out AddedFromEncounterKey
                    }

                    EncounterAllergy ea = new EncounterAllergy();
                    CurrentEncounter.EncounterAllergy.Add(ea);
                    allergy.EncounterAllergy.Add(ea);
                    CurrentPatient.EncounterAllergy.Add(ea);
                }
                else if (allergy.HasChanges)
                {
                    EncounterAllergy eAllergy = allergy.EncounterAllergy
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eAllergy != null)
                    {
                        // clone for history if need be - remove the old version from the encounter
                        PatientAllergy newAllergy = allergy.CreateNewVersion();
                        ((IPatientService)FormModel).Remove(eAllergy);
                        // add the new version to the encounter
                        EncounterAllergy ea = new EncounterAllergy();
                        CurrentEncounter.EncounterAllergy.Add(ea);
                        newAllergy.EncounterAllergy.Add(ea);
                        CurrentPatient.EncounterAllergy.Add(ea);
                        // finally - and this MUST BE LAST - add it to the patient - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentPatient.PatientAllergy.Add(newAllergy);
                    }
                }
            }
        }

        private void SaveAdmissionConsent()
        {
            // iterate backwards to avoid contentsion with new version adds
            var admissionConsentListToProcess = CurrentAdmission.AdmissionConsent.ToList();
            foreach (var curr_consentMeeting in admissionConsentListToProcess)
            {
                AdmissionConsent consent = curr_consentMeeting;
                if (consent.IsNew)
                {
                    EncounterConsent eexist = consent.EncounterConsent
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    consent.EndEditting();

                    EncounterConsent ec = new EncounterConsent();
                    CurrentEncounter.EncounterConsent.Add(ec);
                    consent.EncounterConsent.Add(ec);
                }
                else if (consent.HasChanges)
                {
                    // remove the old version from the encounter
                    EncounterConsent eConsent = consent.EncounterConsent
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eConsent != null)
                    {
                        // clone for history if need be - remove the old version from the encounter
                        AdmissionConsent newConsent = consent.CreateNewVersion();
                        ((IPatientService)FormModel).Remove(eConsent);
                        // add the new version to the encounter
                        EncounterConsent ec = new EncounterConsent();
                        CurrentEncounter.EncounterConsent.Add(ec);
                        newConsent.EncounterConsent.Add(ec);
                        // finally - and this MUST BE LAST - add it to the patient - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentAdmission.AdmissionConsent.Add(newConsent);
                    }
                }
            }
        }

        private void SaveAdmissionEquipment()
        {
            // iterate backwards to avoid contentsion with new version adds
            var equipmentListToProcess = CurrentAdmission.AdmissionEquipment.ToList();
            foreach (var e in equipmentListToProcess)
            {
                AdmissionEquipment equip = e;
                if (equip.IsNew)
                {
                    EncounterEquipment eexist = equip.EncounterEquipment
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eexist != null)
                    {
                        continue;
                    }

                    equip.EndEditting();
                    EncounterEquipment eq = new EncounterEquipment();
                    CurrentEncounter.EncounterEquipment.Add(eq);
                    equip.EncounterEquipment.Add(eq);
                }
                else if (equip.HasChanges)
                {
                    EncounterEquipment eEquip = equip.EncounterEquipment
                        .Where(p => p.EncounterKey == CurrentEncounter.EncounterKey).FirstOrDefault();
                    if (eEquip != null)
                    {
                        // clone for history if need be - remove the old version from the encounter
                        AdmissionEquipment newEquip = equip.CreateNewVersion();
                        ((IPatientService)FormModel).Remove(eEquip);
                        EncounterEquipment ec = new EncounterEquipment();
                        CurrentEncounter.EncounterEquipment.Add(ec);
                        newEquip.EncounterEquipment.Add(ec);

                        // finally - and this MUST BE LAST - add it to the patient - this is what rebinds the (Filtered)ItemsSource via CollectionChanged
                        CurrentAdmission.AdmissionEquipment.Add(newEquip);
                    }
                }
            }
        }

        private void CommitAllOpenEdits()
        {
#if DEBUG
            CheckModelForChanges("CommitAllOpenEdits");
#endif
            FormModel.CommitAllOpenEdits();
        }

        private void SaveAdmissionStatusData()
        {
            // save off changed admission status
            InterimAdmissionStatus = CurrentAdmission.AdmissionStatus;
            InterimAdmissionDateTime = CurrentAdmission.AdmitDateTime;
            InterimAdmissionDischargeDateTime = CurrentAdmission.DischargeDateTime;
            InterimAdmissionNotTakenDateTime = CurrentAdmission.NotTakenDateTime;
            InterimAdmitNotTakenReason = CurrentAdmission.NotTakenReason;
            InterimPreEvalStatus = CurrentAdmission.PreEvalStatus;
            InterimPreEvalOnHoldReason = CurrentAdmission.PreEvalOnHoldReason;
            InterimPreEvalOnHoldDateTime = CurrentAdmission.PreEvalOnHoldDateTime;
            InterimPreEvalFollowUpDate = CurrentAdmission.PreEvalFollowUpDate;
            InterimPreEvalFollowUpComments = CurrentAdmission.PreEvalFollowUpComments;

            // save off changed admission discipline status
            InterimAdmissionDiscStatus = CurrentAdmissionDiscipline.AdmissionStatus;
            InterimAdmissionDiscAdmitDateTime = CurrentAdmissionDiscipline.DisciplineAdmitDateTime;
            InterimAdmissionDiscDischargeDateTime = CurrentAdmissionDiscipline.DischargeDateTime;
            InterimAdmissionDiscNotTakenDateTime = CurrentAdmissionDiscipline.NotTakenDateTime;
            InterimAdmitDiscNotTakenReason = CurrentAdmissionDiscipline.NotTakenReason;

            // reset current admssion to previous
            CurrentAdmission.AdmissionStatus = (PreviousAdmissionStatus != 0)
                ? PreviousAdmissionStatus
                : CurrentAdmission.AdmissionStatus;
            CurrentAdmission.AdmitDateTime = PreviousAdmissionDateTime;
            CurrentAdmission.DischargeDateTime = PreviousAdmissionDischargeDateTime;
            CurrentAdmission.NotTakenDateTime = PreviousAdmissionNotTakenDateTime;
            CurrentAdmission.NotTakenReason = PreviousAdmitNotTakenReason;
            CurrentAdmission.PreEvalStatus = PreviousPreEvalStatus;
            CurrentAdmission.PreEvalOnHoldReason = PreviousPreEvalOnHoldReason;
            CurrentAdmission.PreEvalOnHoldDateTime = PreviousPreEvalOnHoldDateTime;
            CurrentAdmission.PreEvalFollowUpDate = PreviousPreEvalFollowUpDate;
            CurrentAdmission.PreEvalFollowUpComments = PreviousPreEvalFollowUpComments;

            // reset current admission discipline to current
            // Keep CurrentAdmissionDiscipline.DisciplineAdmitDateTime from updating EncounterResumption.ResumptionDate
            EncounterResumption er = CurrentAdmissionDiscipline.EncounterResumption;
            CurrentAdmissionDiscipline.InSaveAdmissionStatusData = true;
            CurrentAdmissionDiscipline.EncounterResumption = null;
            CurrentAdmissionDiscipline.DisciplineAdmitDateTime = PreviousAdmissionDiscAdmitDateTime;
            CurrentAdmissionDiscipline.EncounterResumption = er;
            CurrentAdmissionDiscipline.DischargeDateTime = PreviousAdmissionDiscDischargeDateTime;
            CurrentAdmissionDiscipline.NotTakenDateTime = PreviousAdmissionDiscNotTakenDateTime;
            CurrentAdmissionDiscipline.NotTakenReason = PreviousAdmitDiscNotTakenReason;
            CurrentAdmissionDiscipline.AdmissionStatus = (PreviousAdmissionDiscStatus != 0)
                ? PreviousAdmissionDiscStatus
                : CurrentAdmissionDiscipline.AdmissionStatus;
            CurrentAdmissionDiscipline.InSaveAdmissionStatusData = false;

            // stop server validations from firing.
            CurrentAdmission.ValidateState_IsEval = (CurrentForm.IsEval || CurrentForm.IsResumption);
            CurrentAdmission.ValidateState_IsEvalFullValidation = (CurrentEncounter.FullValidation &&
                                                                   (CurrentForm.IsEval || CurrentForm.IsResumption));
            CurrentAdmission.ValidateState_IsPreEval = (CurrentEncounter.FullValidation && CurrentForm.IsPreEval);
        }

        public void SaveInitialStatusData()
        {
            // reset current admssion to previous
            PreviousAdmissionStatus = CurrentAdmission.AdmissionStatus;
            PreviousAdmissionDateTime = CurrentAdmission.AdmitDateTime;
            PreviousAdmissionDischargeDateTime = CurrentAdmission.DischargeDateTime;
            PreviousAdmissionNotTakenDateTime = CurrentAdmission.NotTakenDateTime;
            PreviousAdmitNotTakenReason = CurrentAdmission.NotTakenReason;
            PreviousPreEvalStatus = CurrentAdmission.PreEvalStatus;
            PreviousPreEvalOnHoldReason = CurrentAdmission.PreEvalOnHoldReason;
            PreviousPreEvalOnHoldDateTime = CurrentAdmission.PreEvalOnHoldDateTime;
            PreviousPreEvalFollowUpDate = CurrentAdmission.PreEvalFollowUpDate;
            PreviousPreEvalFollowUpComments = CurrentAdmission.PreEvalFollowUpComments;

            // reset current admission discipline to current
            PreviousAdmissionDiscStatus = CurrentAdmissionDiscipline.AdmissionStatus;
            PreviousAdmissionDiscAdmitDateTime = CurrentAdmissionDiscipline.DisciplineAdmitDateTime;
            PreviousAdmissionDiscDischargeDateTime = CurrentAdmissionDiscipline.DischargeDateTime;
            PreviousAdmissionDiscNotTakenDateTime = CurrentAdmissionDiscipline.NotTakenDateTime;
            PreviousAdmitDiscNotTakenReason = CurrentAdmissionDiscipline.NotTakenReason;
        }

        private void RestoreAdmissionStatusData()
        {
            // reset current admssion to saved
            CurrentAdmission.AdmissionStatus = InterimAdmissionStatus;
            CurrentAdmission.AdmitDateTime = InterimAdmissionDateTime;
            CurrentAdmission.DischargeDateTime = InterimAdmissionDischargeDateTime;
            CurrentAdmission.NotTakenDateTime = InterimAdmissionNotTakenDateTime;
            CurrentAdmission.NotTakenReason = InterimAdmitNotTakenReason;
            CurrentAdmission.PreEvalStatus = InterimPreEvalStatus;
            CurrentAdmission.PreEvalOnHoldReason = InterimPreEvalOnHoldReason;
            CurrentAdmission.PreEvalOnHoldDateTime = InterimPreEvalOnHoldDateTime;
            CurrentAdmission.PreEvalFollowUpDate = InterimPreEvalFollowUpDate;
            CurrentAdmission.PreEvalFollowUpComments = InterimPreEvalFollowUpComments;

            // reset current admission discipline to saved

            CurrentAdmissionDiscipline.InSaveAdmissionStatusData = true;
            CurrentAdmissionDiscipline.AdmissionStatus = InterimAdmissionDiscStatus;
            if (CurrentAdmissionDiscipline.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Admitted)
            {
                CurrentAdmissionDiscipline.Admitted = true;
            }

            if (CurrentAdmissionDiscipline.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_NotTaken)
            {
                CurrentAdmissionDiscipline.NotTaken = true;
            }

            CurrentAdmissionDiscipline.DisciplineAdmitDateTime = InterimAdmissionDiscAdmitDateTime;
            CurrentAdmissionDiscipline.DischargeDateTime = InterimAdmissionDiscDischargeDateTime;
            CurrentAdmissionDiscipline.NotTakenDateTime = InterimAdmissionDiscNotTakenDateTime;
            CurrentAdmissionDiscipline.NotTakenReason = InterimAdmitDiscNotTakenReason;
            CurrentAdmissionDiscipline.InSaveAdmissionStatusData = false;

            // reset server validations from firing.
            CurrentAdmission.ValidateState_IsEval = (CurrentForm.IsEval || CurrentForm.IsResumption);
            CurrentAdmission.ValidateState_IsEvalFullValidation = (CurrentEncounter.FullValidation &&
                                                                   (CurrentForm.IsEval || CurrentForm.IsResumption));
            CurrentAdmission.ValidateState_IsPreEval = (CurrentEncounter.FullValidation && CurrentForm.IsPreEval);
        }

        int RetryCount;

        async void FormSaved(object sender, MultiErrorEventArgs e)
        {
            Log("FormSaved", "WS_TRACE");

            HomePageAgencyOpsRefreshOption = CurrentAdmission.AdmissionGroupChanged
                ? HomePageAgencyOpsRefreshOptionEnum.All
                : HomePageAgencyOpsRefreshOptionEnum.SingleTask;

            if (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.None || CurrentEncounter.EncounterKey <= 0)
            {
                ViewModelMode = ViewModelMode.ADD;
            }
            else
            {
                ViewModelMode = ViewModelMode.EDIT;
            }

            if (e.Errors != null &&
                e.Errors.Any() &&
                (e.Errors.Any(excep => excep.Message.Contains("NotFound")) || e.Errors.Any(excep =>
                    excep.Message.Contains("HttpWebRequest_WebException_RemoteServer")))
                )
            {
                if (RetryCount < 2) //initialized to 0 - so max of 3 retry attempts
                {
                    RetryCount++;
                    Thread.Sleep(1000 * 1);
                    IsBusy = true;
                    FormModel.SaveMultiAsync(() =>
                    {
                        Log($"FormSaved: FormSaved000, RetryCount={RetryCount}", "WS_TRACE");
                        LogChangeSet("FormSaved000");
                    });
                }
                else
                {
                    isSaving = false;
                    IsBusy = isSaving || isSavingReportData;
                    IsSIPSave = false;

                    NavigateCloseDialog d = new NavigateCloseDialog();
                    if (d != null)
                    {
                        d.Closed += (s, err) =>
                        {
                            RetryCount = 0;
                            var _s = (NavigateCloseDialog)s;
                            var ret = _s.DialogResult.GetValueOrDefault();

                            CurrentAdmission.InDynamicForm = true;

                            if (ret) //Retry
                            {
                                IsBusy = true;
                                FormModel.SaveMultiAsync(() =>
                                {
                                    Log("FormSaved: FormSaved001, Retry", "WS_TRACE");

                                    LogChangeSet("FormSaved001");
                                });
                            }
                            else //go into disconnected
                            {
                                Log("FormSaved: Unable to contact server. Go into disconnected.", "WS_TRACE",
                                    TraceEventType.Warning);

                                EntityManager.WorkOffline = true;
                                DataSaved();
                            }
                        };
                        d.NoVisible = true;
                        d.YesButton.Content = "Retry";
                        d.NoButton.Content = "Work Disconnected";
                        d.YesButton.Width = double.NaN;
                        d.NoButton.Width = double.NaN;
                        d.Title = "Warning";
                        d.Width = double.NaN;
                        d.Height = double.NaN;
                        d.ErrorMessage = string.Format("{0}Unable to contact server.{0}", Environment.NewLine);
                        d.Show();
                    }
                }
            }
            else
            {
                RetryCount = 0;

                bool exit = false;
                if (StoredAdmissionGroupDate.HasValue)
                {
                    CurrentAdmission.AdmissionGroupDate = (DateTime)StoredAdmissionGroupDate;
                }

                if (e.Errors.Any())
                {
                    foreach (Exception ex in e.Errors)
                    {
                        Log(string.Format("FormSaved: e.Error={0}, InnerException={1}",
                                ex.Message,
                                (ex.InnerException == null) ? "none" : ex.InnerException.Message), "WS_TRACE",
                            TraceEventType.Warning);

                        MessageBox.Show(String.Format(
                            "DynamicFormViewModel.FormSaved Exception: {0}, Inner Exception: {1} ",
                            ex.Message,
                            (ex.InnerException == null) ? "none" : ex.InnerException.Message));
                    }

                    if (e.EntityErrors != null)
                    {
                        foreach (string s in e.EntityErrors)
                        {
                            Log($"FormSaved: e.EntityError={s}", "WS_TRACE", TraceEventType.Warning);

                            MessageBox.Show(String.Format("DynamicFormViewModel.FormSaved EntityError: {0}", s));
                        }
                    }

                    if (CurrentEncounter != null)
                    {
                        CurrentEncounter.EncounterStatus = PreviousEncounterStatus;
                        CurrentEncounter.Signed = PreviousSigned;
                    }

                    CurrentAdmission.PreEvalStatus = PreviousPreEvalStatus;
                    CurrentAdmission.AdmissionStatus = (PreviousAdmissionStatus != 0)
                        ? PreviousAdmissionStatus
                        : CurrentAdmission.AdmissionStatus;
                    CurrentAdmissionDiscipline.AdmissionStatus = (PreviousAdmissionDiscStatus != 0)
                        ? PreviousAdmissionDiscStatus
                        : CurrentAdmissionDiscipline.AdmissionStatus;
                    if (CurrentOasisManager != null)
                    {
                        if (CurrentOasisManager.CurrentEncounterOasis != null)
                        {
                            // if we removed the new re-edit prior to the attempted save (bacuse no OASIS changes) - reinstate the new re-edit oasis
                            if (CurrentOasisManager.CurrentEncounterOasis.EncounterOasisKey > 0)
                            {
                                CurrentOasisManager.CurrentEncounterOasis = CurrentOasisManager.StartNewOasisEdit();
                            }
                        }
                    }
                }
                else
                {
                    await SaveDynamicForm(
                        (CurrentPatient != null) ? CurrentPatient.PatientKey : -1,
                        (CurrentEncounter != null) ? CurrentEncounter.EncounterKey : -1,
                        (CurrentEncounter != null) ? CurrentEncounter.EncounterStatus : (int)EncounterStatusType.Edit,
                        OfflineStoreType.CACHE,
                        false);

                    await DeleteDynamicForm(OfflineStoreType.SAVE);

                    if (CurrentEncounter != null && PreviousEncounterStatus != CurrentEncounter.EncounterStatus)
                    {
                        exit = true;
                    }

                    if ((CurrentEncounter != null &&
                         CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed) && (AllValid))
                    {
                        exit = true;
                    }

                    if (SignatureQuestion != null)
                    {
                        SignatureQuestion.PostSaveProcessing();
                    }

                    QuestionMasterList.ForEach(q => q.PostSaveProcessing());
                }

                await FormSavedInternal(e.Errors.Count, exit);
            }
        }

        /*
         * NOTE: this function is called when online and offline.
         *       Do not code anything that would not work when offline.
         *       Do not delete the SIP data from the .SAVE folder.
         */
        private async System.Threading.Tasks.Task FormSavedInternal(int _serverErrorCount = 0, bool _exit = false,
            bool _is_Online = true)
        {
            Log($"FormSavedInternal: _serverErrorCount={_serverErrorCount}, _exit={_exit}, _is_Online={_is_Online}",
                "WS_TRACE");

            int DocumentationChangedEncounterStatus = (CurrentEncounter == null) ? 0 : CurrentEncounter.EncounterStatus;
            bool DataSavedRun = false;
            // Put signatures back for the forms that we are allowing to save with validation errors (without signatures)
            if (IsSIPSave)
            {
                RestoreAdmissionStatusData();
            }

            if (SignaturesToAdd.Any())
            {
                foreach (EncounterSignature es in SignaturesToAdd) CurrentEncounter.EncounterSignature.Add(es);
                SlaveValue++; // force an update to the Signature 
                NavigateCloseDialog d = new NavigateCloseDialog();
                if (d != null)
                {
                    DataSavedRun = true;
                    d.Closed += (s, err) =>
                    {
                        if (_serverErrorCount == 0)
                        {
                            DataSaved();
                        }
                    };
                    d.NoVisible = false;
                    d.YesButton.Content = "OK";
                    d.Title = "Warning";
                    d.Width = double.NaN;
                    d.Height = double.NaN;
                    d.ErrorMessage = ((_serverErrorCount == 0)
                        ? String.Format(
                            "This Encounter contains validation errors and is not yet complete.{0}The Form data has been saved, but the signature has not been applied.",
                            Environment.NewLine)
                        : String.Format(
                            "This Encounter contains validation errors and is not yet complete.{0}The Form data has NOT been saved.",
                            Environment.NewLine));
                    d.Show();
                }

                SignaturesToAdd.Clear();
                // Re-validate the form (some validation errors are removed by the save)
                CurrentEncounter.Signed = InterimSigned;
                CurrentEncounter.EncounterStatus = InterimEncounterStatus;
                if (!IsSIPSave)
                {
                    RestoreAdmissionStatusData();
                }
            }
            else if ((PreviousEncounterStatus == (int)EncounterStatusType.OASISReview) ||
                     (PreviousEncounterStatus == (int)EncounterStatusType.OASISReviewEdit) ||
                     (PreviousEncounterStatus == (int)EncounterStatusType.OASISReviewEditRR) ||
                     (PreviousEncounterStatus == (int)EncounterStatusType.Completed))
            {
                // Re-validate the form (some validation errors are removed by the save)
                if (CurrentEncounter != null)
                {
                    CurrentEncounter.Signed = InterimSigned;
                    CurrentEncounter.EncounterStatus = InterimEncounterStatus;
                }
            }

            if (BackgroundService.IsBackground == false && _serverErrorCount == 0 &&
                AllValid) // Do not message if there were server errors
            {
                Messenger.Default.Send(
                    DocumentationChangedEvent.Create(
                        patientKey: CurrentAdmission?.PatientKey,
                        admissionKey: CurrentAdmission?.AdmissionKey,
                        encounterKey: CurrentEncounter?.EncounterKey,
                        taskKey: CurrentTask?.TaskKey,
                        encounterStatus: DocumentationChangedEncounterStatus,
                        homePageAgencyOpsRefreshOption: HomePageAgencyOpsRefreshOption),
                    Constants.Messaging.DocumentationChanged);

                if (CurrentPatient != null)
                {
                    Messenger.Default.Send(CurrentPatient.PatientKey,
                        Constants.Messaging.RefreshMaintenancePatientAdmissions);
                    Messenger.Default.Send(CurrentPatient.PatientKey,
                        Constants.Messaging.AdmissionDocumentationChanged);
                }
            }

            if (CurrentGoalManager != null)
            {
                CurrentGoalManager.FilterAll(true);
            }

            if (DFControlManager != null)
            {
                DFControlManager.RefreshAllControls();
            }

            isSaving = false;
            IsBusy = isSaving || isSavingReportData;
            IsSIPSave = false;

            if (CurrentOasisManager != null)
            {
                CurrentOasisManager.IsBusy = false;
            }

            if (_exit)
            {
                if (CurrentForm.IsPlanOfCare && CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    force_exit_application_after_save = false;
                }
                else
                {
                    force_exit_application_after_save = true;
                }

                if (DataSavedRun == false)
                {
                    if (_serverErrorCount == 0)
                    {
                        DataSaved();
                        DataSavedRun = true;
                    }
                }

                if (CurrentForm.IsPlanOfCare && CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed && !force_exit_application_after_save)
                {
                    RefreshFormControlsAfterSave();
                }
            }
            else
            {
                if (DataSavedRun == false)
                {
                    if (_serverErrorCount == 0)
                    {
                        DataSaved();
                        DataSavedRun = true;
                    }
                }

                LaunchHavenErrorWindow(HavenErrorList);
                if (doNavigateBackOnFormSaved == false)
                {
                    RefreshAdmissionPhysicianData();
                }

                // Setup new EncounterStatus if need be
                if (SignatureQuestion != null)
                {
                    SignatureQuestion.CalculateNewEncounterStatus(CurrentEncounter.Signed);
                }

                ValidateSections();

                if (CurrentEncounter != null)
                {
                    CurrentEncounter.EncounterStatus = PreviousEncounterStatus;
                    CurrentEncounter.Signed = PreviousSigned;
                }
            }

            LogChangeSet("FormSavedInternal"); //Log changes to Task when Offline

            if (doNavigateBackOnFormSaved)
            {
                doNavigateBackOnFormSaved = false;
                if (_is_Online)
                {
                    await DeleteDynamicForm(OfflineStoreType.SAVE);
                }

                NavigateBack();
            }

            if (CurrentAdmission != null)
            {
                CurrentAdmission.InDynamicForm = true;
            }
        }

        private void RefreshFormControlsAfterSave()
        {
            var sectionUI = Sections.Where(s => s.Questions.Any(q => !(String.IsNullOrEmpty(q.Label)) && q.Label.Equals("Physician Information"))).FirstOrDefault();
            if (sectionUI != null)
            {
                var questionUI = sectionUI.Questions.Where(q => q.Label.Equals("POC Physician Information")).FirstOrDefault();
                if (questionUI != null)
                {
                    var phyUI = questionUI as POCBase;
                    if (phyUI != null)
                    {
                        phyUI.RefreshPageBindings();
                    }
                }
            }
        }

        private int? ChangePhysicianOrdersTrackingKey;

        private void RefreshOrdersTrackingRow()
        {
            ChangePhysicianOrdersTrackingKey = null;
            Entity order = null;
            int? orderKey = null;
            int? orderType = null;
            int? admissionCertKey = null;
            int? physicianKey = null;
            PhysicianAddress physicianAddr = null;
            DateTime? orderDate = null;
            Guid? clinicianID = null;
            bool inactive = false;
            IsBusy = true;

            if (GetOrdersTrackingDataForRefresh(out order, out orderKey, out orderType, out admissionCertKey,
                    out physicianKey, out physicianAddr, out orderDate, out clinicianID, out inactive))
            {
                if (orderKey.HasValue
                    && orderType.HasValue
                    && admissionCertKey.HasValue
                    && orderType.HasValue
                    && physicianKey.HasValue
                    && orderDate.HasValue
                   )
                {
                    int? physicianAddrKey = (physicianAddr == null) ? null : physicianAddr.PhysicianAddressKey as int?;
                    OrdersTrackingManager otm = new OrdersTrackingManager();
                    OrdersTracking ot = otm.RefreshTrackingRow(order, orderKey.Value, CurrentEncounter,
                        CurrentAdmission, null, physicianAddr, admissionCertKey.Value, orderType.Value,
                        physicianKey.Value, physicianAddrKey,
                        orderDate.Value, clinicianID, inactive, false);
                    // Note - COTIs come back signed
                    // Update InterimOrderBatchDetail if need be on change of physician
                    if ((CurrentAdmission != null) && (CurrentAdmission.InterimOrderBatchDetail != null) &&
                        (ot != null) && (orderType.HasValue) && (orderType == (int)OrderTypesEnum.InterimOrder) &&
                        (orderKey.HasValue) && ot.Status == (int)OrdersTrackingStatus.Complete)
                    {
                        List<InterimOrderBatchDetail> bdList = (CurrentAdmission.InterimOrderBatchDetail.Where(d =>
                            ((d.RemovedFromBatchDate == null) && (d.OrderEntryKey == orderKey)))).ToList();
                        if (bdList != null)
                        {
                            foreach (InterimOrderBatchDetail bd in bdList)
                                bd.RemovedFromBatchDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                        }
                    }

                    // Set ChangePhysicianOrdersTrackingKey to notify OTWorklist of physician changes
                    ChangePhysicianOrdersTrackingKey = (ot == null) ? (int?)null : ot.OrdersTrackingKey;
                }
            }
        }

        private bool GetOrdersTrackingDataForRefresh(out Entity Order, out int? orderKey, out int? orderType,
            out int? admissionCertKey, out int? physicianKey,
            out PhysicianAddress physicianAddress, out DateTime? orderDate, out Guid? clinicianId, out bool inactive)
        {
            SyncEncounterAdmissionAndOrderEntry();
            bool okayToRefresh = false;
            Order = null;
            orderKey = null;
            orderType = null;
            admissionCertKey = null;
            physicianKey = null;
            physicianAddress = null;
            orderDate = null;
            clinicianId = null;
            inactive = false;

            if ((CurrentEncounter != null)
                && (CurrentForm != null)
                && (CurrentForm.IsPlanOfCare)
                && (CurrentEncounter.EncounterOrTaskStartDateAndTime.HasValue)
               )
            {
                orderType = (int)OrderTypesEnum.POC;
                Admission adm = CurrentEncounter.Admission;
                EncounterPlanOfCare poc = CurrentEncounter.EncounterPlanOfCare.FirstOrDefault();
                EncounterAdmission ea = CurrentEncounter.EncounterAdmission.FirstOrDefault();
                if ((poc != null) && (ea != null) && ((adm != null)))
                {
                    Order = poc;
                    AdmissionCertification ac = adm.AdmissionCertification
                        .Where(a => a.PeriodStartDate == poc.CertificationFromDate).FirstOrDefault();
                    if (ac != null)
                    {
                        admissionCertKey = ac.AdmissionCertKey;
                        orderKey = poc.EncounterPlanOfCareKey;
                        physicianKey = (adm.HospiceAdmission == false)
                            ? ea.SigningPhysicianKey
                            : ea.AttendingPhysicianKey;
                        Physician phy = PhysicianCache.Current.GetPhysicianFromKey(physicianKey);
                        physicianAddress = PhysicianCache.Current.GetPhysicianAddressFromKey(
                            ((adm.HospiceAdmission == false)
                                ? ea.SigningPhysicianAddressKey
                                : ea.AttendingPhysicianAddressKey));
                        if ((physicianAddress == null) && (phy != null))
                        {
                            physicianAddress = phy.MainAddress;
                        }

                        orderDate = CurrentEncounter.EncounterDateTime.Date;
                        clinicianId = CurrentEncounter.EncounterBy;
                        okayToRefresh = true;
                    }
                }

                inactive = CurrentEncounter.Inactive;
            }
            else if ((CurrentEncounter != null) && (CurrentForm != null) && (CurrentEncounter.EncounterIsOrderEntry) &&
                     (CurrentEncounter.EncounterOrTaskStartDateAndTime.HasValue))
            {
                orderType = (int)OrderTypesEnum.InterimOrder;
                EncounterAdmission ea = CurrentEncounter.EncounterAdmission.FirstOrDefault();
                OrderEntry oe = CurrentEncounter.OrderEntry.Where(o => o.HistoryKey == null).FirstOrDefault();
                if (ea != null && oe != null)
                {
                    Order = oe;
                    if (CurrentAdmission.CurrentCert != null)
                    {
                        admissionCertKey = CurrentAdmission.CurrentCert.AdmissionCertKey;
                    }

                    orderKey = oe.OrderEntryKey;
                    physicianKey = oe.SigningPhysicianKey;
                    Physician phy = PhysicianCache.Current.GetPhysicianFromKey(physicianKey);
                    physicianAddress = PhysicianCache.Current.GetPhysicianAddressFromKey(oe.SigningPhysicianAddressKey);
                    if ((physicianAddress == null) && (phy != null))
                    {
                        physicianAddress = phy.MainAddress;
                    }

                    orderDate = (oe.CompletedDate == null)
                        ? CurrentEncounter.EncounterDateTime.Date
                        : ((DateTimeOffset)oe.CompletedDate).Date;
                    clinicianId = CurrentEncounter.EncounterBy;
                    okayToRefresh = true;
                }

                inactive = CurrentEncounter.Inactive;
            }
            else if ((CurrentEncounter != null) && (CurrentForm != null) &&
                     (CurrentEncounter.EncounterIsHospiceElectionAddendum))
            {
                orderType = (int)OrderTypesEnum.HospiceElectionAddendum;
                EncounterAdmission
                    ea = CurrentEncounter.EncounterAdmission.FirstOrDefault(); // so we wait until the form is complete
                EncounterHospiceElectionAddendum
                    eh = CurrentEncounter.EncounterHospiceElectionAddendum.FirstOrDefault();
                if ((ea != null) && (eh != null) && eh.RequiresSignature)
                {
                    Order = CurrentEncounter;
                    if (CurrentAdmission.CurrentCert != null)
                    {
                        admissionCertKey = CurrentAdmission.CurrentCert.AdmissionCertKey;
                    }

                    orderKey = eh.EncounterKey;
                    physicianKey = ea.SigningPhysicianKey;
                    Physician phy = PhysicianCache.Current.GetPhysicianFromKey(physicianKey);
                    physicianAddress = PhysicianCache.Current.GetPhysicianAddressFromKey(ea.SigningPhysicianAddressKey);
                    if ((physicianAddress == null) && (phy != null))
                    {
                        physicianAddress = phy.MainAddress;
                    }

                    orderDate = ((DateTimeOffset)eh.CreateDate).Date;
                    clinicianId = CurrentEncounter.EncounterBy;
                    okayToRefresh = true;
                }

                inactive = CurrentEncounter.Inactive;
            }

            else if ((CurrentEncounter != null) && (CurrentForm != null) && (CurrentForm.IsCOTI) &&
                     (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed))
            {
                orderType = (int)OrderTypesEnum.CoTI;
                EncounterAdmission ea = CurrentEncounter.EncounterAdmission.FirstOrDefault();
                AdmissionCOTI ac = CurrentEncounter.AdmissionCOTI.FirstOrDefault();
                if (ea != null && ac != null)
                {
                    Order = ac;
                    admissionCertKey = ac.AdmissionCertKey;

                    orderKey = ac.AdmissionCOTIKey;
                    physicianKey = ac.SigningPhysicianKey;
                    Physician phy = PhysicianCache.Current.GetPhysicianFromKey(physicianKey);
                    physicianAddress = PhysicianCache.Current.GetPhysicianAddressFromKey(ac.SigningPhysicianAddressKey);
                    if ((physicianAddress == null) && (phy != null))
                    {
                        physicianAddress = phy.MainAddress;
                    }

                    orderDate = (ac.AttestationDate == null)
                        ? CurrentEncounter.EncounterDateTime.Date
                        : ((DateTime)ac.AttestationDate).Date;
                    clinicianId = CurrentEncounter.EncounterBy;
                    okayToRefresh = true;
                }

                inactive = CurrentEncounter.Inactive;
            }
            else if ((CurrentEncounter != null)
                     && (CurrentForm != null)
                     && (CurrentForm.Description == "Physician Face To Face")
                     && (CurrentEncounter.Form != null)
                     && (CurrentEncounter.EncounterOrTaskStartDateAndTime.HasValue)
                    )
            {
                orderType = (int)OrderTypesEnum.FaceToFaceEncounter;
                OrderEntry oe = CurrentAdmission.OrderEntry.Where(o => o.HistoryKey == null).FirstOrDefault();
                EncounterAdmission ea = CurrentEncounter.EncounterAdmission.FirstOrDefault();
                Order = oe;
                if (CurrentAdmission.CurrentCert != null)
                {
                    admissionCertKey = CurrentAdmission.CurrentCert.AdmissionCertKey;
                }

                orderKey = CurrentEncounter.EncounterKey;
                physicianKey = ea.SigningPhysicianKey;
                Physician phy = PhysicianCache.Current.GetPhysicianFromKey(physicianKey);
                physicianAddress = PhysicianCache.Current.GetPhysicianAddressFromKey(ea.SigningPhysicianAddressKey);
                if ((physicianAddress == null) && (phy != null))
                {
                    physicianAddress = phy.MainAddress;
                }

                orderDate = CurrentEncounter.EncounterDateTime.Date;
                clinicianId = CurrentEncounter.EncounterBy;
                okayToRefresh = true;
                inactive = CurrentEncounter.Inactive;
            }

            return okayToRefresh;
        }

        private void SyncEncounterAdmissionAndOrderEntry()
        {
            if ((CurrentEncounter == null) || (CurrentOrderEntryManager == null))
            {
                return;
            }

            if (CurrentOrderEntryManager.CurrentIOrderEntry == null)
            {
                return;
            }

            if (CurrentOrderEntryManager.IsVO && (CurrentOrderEntryManager.CurrentOrderEntryVO == null))
            {
                return;
            }

            if (CurrentOrderEntryManager.IsVO && (CurrentOrderEntryManager.CurrentOrderEntryVO != null) &&
                CurrentOrderEntryManager.CurrentOrderEntryVO.DiscardFlag)
            {
                return;
            }

            if (CurrentOrderEntryManager.CurrentIOrderEntry.SigningPhysicianKey == null)
            {
                CurrentOrderEntryManager.CurrentIOrderEntry.SigningPhysicianAddressKey = null;
            }
            else
            {
                int? addrKey = null;
                if ((CurrentAdmission != null) && (CurrentAdmission.AdmissionPhysician != null))
                {
                    AdmissionPhysician ap = CurrentAdmission.AdmissionPhysician.Where(a =>
                            a.PhysicianKey == CurrentOrderEntryManager.CurrentIOrderEntry.SigningPhysicianKey)
                        .FirstOrDefault();
                    if (ap != null)
                    {
                        addrKey = ap.PhysicianAddressKey;
                    }
                }

                if (addrKey == null)
                {
                    Physician p =
                        PhysicianCache.Current.GetPhysicianFromKey(CurrentOrderEntryManager.CurrentIOrderEntry
                            .SigningPhysicianKey);
                    if ((p != null) && (p.MainAddress != null))
                    {
                        addrKey = p.MainAddress.PhysicianAddressKey;
                    }
                }

                CurrentOrderEntryManager.CurrentIOrderEntry.SigningPhysicianAddressKey = addrKey;
            }

            EncounterAdmission ea = CurrentEncounter.EncounterAdmission.FirstOrDefault();
            if (ea == null)
            {
                return;
            }

            ea.SigningPhysicianKey = CurrentOrderEntryManager.CurrentIOrderEntry.SigningPhysicianKey;
            ea.SigningPhysicianAddressKey = CurrentOrderEntryManager.CurrentIOrderEntry.SigningPhysicianAddressKey;
        }

        public AdmissionCertification GetAdmissionCertForEncounter()
        {
            if (CurrentEncounter == null)
            {
                return null;
            }

            if (CurrentEncounter.EncounterOrTaskStartDateAndTime == null)
            {
                return null;
            }

            AdmissionCertification ec =
                CurrentAdmission.GetAdmissionCertForDate(CurrentEncounter.EncounterOrTaskStartDateAndTime.Value.Date,
                    false);
            if (CurrentEncounter.GetIsEncounterInRecertWindow(CurrentAdmission, CurrentEncounter))
            {
                ec = GetEncounterCertCycleToUseForPOC();
            }

            return ec;
        }

        public void SetTaskKey(int TaskKeyParm)
        {
            TaskKey = TaskKeyParm;
        }

        public void ProcessFormForUnitTesting()
        {
            List<Exception> errors = new List<Exception>();
            var args = new MultiErrorEventArgs(errors);

            ProcessForm(this, args);
        }

        async void ProcessForm(object sender, MultiErrorEventArgs e)
        {
#if DEBUG
            CheckModelForChanges("ProcessForm");
#endif
            if ((e.Errors.Any() == false) && (e.EntityErrors.Any() == false))
            {
                SetupTaskAndEncounter();

                if (!IsTaskValid())
                {
                    return;
                }

                await ProcessFormInternal(e.DataLoadType);
            }
            else
            {
                IsBusy = false;
                if (BackgroundService.IsBackground == false)
                {
                    foreach (var error in e.Errors)
                        MessageBox.Show(string.Format("Error: {0}", error.Message));
                    foreach (var entity_error in e.EntityErrors)
                        MessageBox.Show(string.Format("Entity Error: {0}", entity_error));
                }
            }

            if (AutoSaveAfterLoad && EntityManager.IsOnline)
            {
                Log("ProcessForm: Will Auto Save", "WS_TRACE");

                Metrics.Ria.NullMetricLogger.Log("[000] Will Auto Save", CorrelationIDHelper, EntityManager.IsOnline,
                    "DynamicFormViewModel.ProcessForm", CachedURI);

                AutoSaveAfterLoad = false;
                // AutoSaves will happen when the client and server encounters are not synced, we must resync them!
                IsBusy = true;
                Deployment.Current.Dispatcher.BeginInvoke(async () =>
                {
                    RaisePropertyChangedPrintStuff();

                    await OKProcessing(EntityManager.IsOnline, OfflineStoreType.SAVE, true);
                });
            }
            else
            {
                RaisePropertyChangedPrintStuff();
            }
        }

        public void RaisePropertyChangedPrintStuff()
        {
            this.RaisePropertyChangedLambda(p => p.CanContinue);
            this.RaisePropertyChangedLambda(p => p.HasOasisSections);
            this.RaisePropertyChangedLambda(p => p.PrintVisibleSSRS);
            this.RaisePropertyChangedLambda(p => p.PrintVisible);
            this.RaisePropertyChangedLambda(p => p.FaxVisible);

            SSRSPrint_Command.RaiseCanExecuteChanged();
            Fax_Command.RaiseCanExecuteChanged();
        }

        // Used to extract status changes in IsOrderEntry forms
        public List<ChangeHistory> OrdersTrackingChangeHistory;

        private void SetupTaskAndEncounter()
        {
            // Setup the form and the task;
            CurrentTask = FormModel.CurrentTask;
            OrdersTrackingChangeHistory = FormModel.OrdersTrackingChangeHistory;

            if (FormModel.CurrentAuthMappings != null)
            {
                _AuthOrders = FormModel.CurrentAuthMappings.ToList();
            }

            CurrentPatient = FormModel.CurrentPatient;
            CurrentAdmission = FormModel.CurrentAdmission;
            CurrentForm = FormModel.CurrentForm;
            // for attached forms - the formkey has already been overwritten
            if ((CurrentForm != null) && (CurrentForm.IsAttachedForm))
            {
                if (CurrentForm.IsCMSForm)
                {
                    CMSFormKey = CurrentForm.FormKey;
                }

                return;
            }

            // override form from encounter if need be
            CurrentForm = null;
            Encounter e = CurrentPatient.Encounter.Where(p => p.TaskKey == TaskKey && p.HistoryKey.HasValue == false)
                .FirstOrDefault();
            if (e != null)
            {
                ServiceTypeKey = (e.ServiceTypeKey == null) ? 0 : (int)e.ServiceTypeKey;
                FormKey = (e.FormKey == null) ? 0 : (int)e.FormKey;
                CurrentForm = e.MyForm;
            }

            if (CurrentForm == null)
            {
                CurrentForm = FormModel.CurrentForm;
                FormKey = CurrentForm.FormKey;
            }
        }

        private bool IsTaskValid()
        {
            if (IsReadOnlyEncounter)
            {
                return true;
            }

            bool validTask = true;
            bool removeTask = false;
            // Verify the OASIS task was created by the app and not from a third party.
            if (CurrentForm != null && CurrentForm.IsOasis)
            {
                string rfa = null;
                rfa = (CurrentTask == null)
                    ? null
                    : ((string.IsNullOrWhiteSpace(CurrentTask.OasisRFA)) ? null : CurrentTask.OasisRFA);
                if (rfa == null)
                {
                    // This oasis must have been scheduled by a third party application.  Close and cancel the task.
                    ErrorMessage = "OASIS tasks must be created from Admission Maintenance.  This is an invalid Task.";
                    validTask = false;
                }
            }

            // if it isn't a valid task, cancel all work and alert the user.
            if (!validTask)
            {
                var dvalue = TenantSettingsCache.Current.TenantSettingDistanceTraveledMeasure;
                CurrentEncounter = new Encounter
                {
                    DistanceScale = dvalue
                };
                if (removeTask)
                {
                    CancelGeneratedTask();
                }

                CanContinue = false;
                IsBusy = false;
                doNavigateBackOnFormSaved = false;
            }

            return validTask;
        }

        async System.Threading.Tasks.Task ProcessFormInternal(DataLoadType dataLoadType)
        {
            Sections.Clear();

            if (CurrentForm == null)
            {
                return;
            }

            if (HtmlPage.IsEnabled)
            {
                HtmlPage.Document.SetProperty("title", CurrentForm.Description + " Documentation Page");
            }

            if (BackgroundService.IsBackground == false)
            {
                Messenger.Default.Register<int>(this,
                    "AdmissionCoverageRefreshed",
                    AdmissionKey => { Messenger.Default.Send(AdmissionKey, "AdmissionCoverage_FormUpdate"); });
                Messenger.Default.Register<int>(this,
                    "AdmissionPhysicianRefreshed",
                    AdmissionKey =>
                    {
                        //var attached_count = CurrentAdmission.AdmissionPhysician.Count();
                        Messenger.Default.Send(AdmissionKey, "AdmissionPhysician_FormUpdate");
                    });

                //Messenger.Default.Send(CurrentAdmission.AdmissionCoverage, CurrentAdmission.AdmissionKey);
                Messenger.Default.Register<EntityCollection<AdmissionCoverage>>(this, CurrentAdmission.AdmissionKey,
                    a => { RefreshAdmissionCoverageData(); });
                //Messenger.Default.Send(CurrentAdmission.AdmissionPhysician, CurrentAdmission.AdmissionKey);
                Messenger.Default.Register<EntityCollection<AdmissionPhysician>>(this, CurrentAdmission.AdmissionKey,
                    a => { RefreshAdmissionPhysicianData(); });

                Messenger.Default.Register<PatientAddress>(this, pa => { RefreshPatientAddress(); });

                Messenger.Default.Register<int>(this,
                    "PatientAddressRefreshed",
                    PatientKey => { Messenger.Default.Send(PatientKey, "PatientAddress_FormUpdate"); });
                Messenger.Default.Register<int>(this, "RefreshMaintenancePatient", i => RefreshPatient(i));

                Messenger.Default.Register<int>(this,
                    string.Format("OasisVersionChanged{0}", CurrentAdmission.AdmissionKey.ToString().Trim()),
                    encounterOASISKey => OasisVersionChanged(encounterOASISKey));
            }

            //NOTE: EncounterKey = -1 for new Task - Encounter never saved.
            //      EncounterKey will be some negative value if never saved to server, but new'd up on the client and saved locally
            //      loadType will be DataLoadType.Local when the form is loaded from disk
            if (dataLoadType == DataLoadType.LOCAL)
            {
                CurrentEncounter = CurrentPatient.Encounter
                    .Where(p => p.TaskKey == TaskKey && p.HistoryKey.HasValue == false)
                    .FirstOrDefault();
                if (CurrentEncounter == null)
                {
                    CurrentEncounter = CurrentPatient.Encounter.Where(e => e.EncounterKey < 0).FirstOrDefault();
                }
            }

            if (CurrentEncounter == null)
            {
                CurrentEncounter = CurrentPatient.Encounter.Where(p => p.TaskKey == TaskKey && p.HistoryKey.HasValue == false).FirstOrDefault();
            }

            RefreshAdmissionDataOnLoad();

            AddingNewEncounter = false;

            if ((CurrentEncounter == null) ||
                (CurrentEncounter.EncounterStatus ==
                 (int)EncounterStatusType.None)) //Create Encounter when Create Task
            {
                //NOTE: Encounters added at time of Task creation will have an Encounter.EncounterStatus == EncounterStatusType.None
                AddingNewEncounter = true;
            }

            if (AddingNewEncounter == false)
            {
                if (SetupAttemptedEncounterPart1() == false)
                {
                    CanContinue = false;
                    // NOTE: ErrorMessage was setup in SetupAttemptedVisit();
                    IsBusy = false;
                    return;
                }

                if (DetermineCurrentAdmissionDiscipline() < 0)
                {
                    IsBusy = false;
                    return;
                }

                CurrentEncounter.CurrentAdmissionDiscipline = CurrentAdmissionDiscipline;
                if ((CurrentEncounter.EncounterIsTeamMeeting) &&
                    (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed))
                {
                    CurrentEncounter.EncounterBy =
                        WebContext.Current.User.MemberID; // for now - override TeamMeeting to user currently editing it
                }

                PostEncounterSetup();
                SetupExistingEncounter();

                ViewModelMode = ViewModelMode.EDIT;
            }
            else
            {
                if (CurrentTask.UserID != WebContext.Current.User.MemberID && !IsReadOnlyEncounter)
                {
                    CanContinue = false;
                    ErrorMessage = "This task is not assigned to you";
                    IsBusy = false;
                    return;
                }

                if (CurrentEncounter == null)
                {
                    CurrentEncounter = new Encounter();
                    CurrentTask.Encounter.Add(CurrentEncounter);
                }

                CurrentEncounter.VitalsVersion =
                    2; // to pass along to discrete vitals for ReadingDateTime collection on any new forms
                if (SetupAttemptedEncounterPart1() == false)
                {
                    CanContinue = false;
                    // ErrorMessage was setup in SetupAttemptedVisit();
                    IsBusy = false;
                    return;
                }

                CurrentEncounter.EncounterDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                if (CurrentEncounter.ServiceTypeKey.GetValueOrDefault() <= 0)
                {
                    Metrics.Ria.NullMetricLogger.Log(
                        string.Format("[001] CurrentEncounter.ServiceTypeKey.{0} = ServiceTypeKey.{1}",
                            CurrentEncounter.ServiceTypeKey, ServiceTypeKey), CorrelationIDHelper,
                        EntityManager.IsOnline, "DynamicFormViewModel.ProcessFormInternal", CachedURI);

                    CurrentEncounter.ServiceTypeKey = ServiceTypeKey; //is a new encounter - init to what was scheduled
                }
                else
                {
                    Metrics.Ria.NullMetricLogger.Log(
                        string.Format("[004] ServiceTypeKey.{0} = CurrentTask.ServiceTypeKey.Value.{1}", ServiceTypeKey,
                            CurrentTask.ServiceTypeKey.Value), CorrelationIDHelper, EntityManager.IsOnline,
                        "DynamicFormViewModel.ProcessFormInternal", CachedURI);

                    //They could have changed the service type - make sure service type on encounter matches the service type on the task
                    //Clincian Cannot Open SN LPN Visits - REASON - changed Task.ServiceTypeKey
                    ServiceTypeKey = CurrentTask.ServiceTypeKey.Value;

                    Metrics.Ria.NullMetricLogger.Log(
                        string.Format(
                            "[009] CurrentEncounter.ServiceTypeKey.{0} = CurrentTask.ServiceTypeKey.Value.{1}",
                            CurrentEncounter.ServiceTypeKey, CurrentTask.ServiceTypeKey.Value), CorrelationIDHelper,
                        EntityManager.IsOnline, "DynamicFormViewModel.ProcessFormInternal", CachedURI);

                    CurrentEncounter.ServiceTypeKey = CurrentTask.ServiceTypeKey.Value;
                    CurrentEncounter.FormKey = ServiceTypeCache.GetFormKey(CurrentTask.ServiceTypeKey.Value);
                }

                if (PatientKey.GetValueOrDefault() == 0)
                {
                    PatientKey = CurrentEncounter.PatientKey;
                }

                if (AdmissionKey == 0)
                {
                    AdmissionKey = CurrentEncounter.AdmissionKey;
                }

                if (TaskKey == 0)
                {
                    TaskKey = CurrentEncounter.TaskKey.GetValueOrDefault();
                }

                CurrentEncounter.FormKey = CurrentForm.FormKey;

                ViewModelMode = ViewModelMode.ADD;

                CurrentEncounter.EncounterStatus = (int)EncounterStatusType.Edit;
                CurrentEncounter.EncounterBy = WebContext.Current.User.MemberID;

                // need this for the print preview
                CurrentEncounter.UpdatedBy = WebContext.Current.User.MemberID;
                CurrentEncounter.DistanceScale = TenantSettingsCache.Current.TenantSettingDistanceTraveledMeasure;

                if (DetermineCurrentAdmissionDiscipline() < 0)
                {
                    IsBusy = false;
                    return;
                }

                CurrentEncounter.CurrentAdmissionDiscipline = CurrentAdmissionDiscipline;
                CurrentEncounter.AdmissionDisciplineKey = ((CurrentAdmissionDiscipline.IsNew)
                    ? (int?)null
                    : CurrentAdmissionDiscipline.AdmissionDisciplineKey);
                PostEncounterSetup();

                Encounter mostRecentDisciplineEncounter = CurrentAdmission.Encounter
                    .Where(p => p.EncounterKey != CurrentEncounter.EncounterKey && p.DisciplineServiceNumber.HasValue &&
                                p.ServiceType != null && p.ServiceType.Discipline != null &&
                                p.ServiceType.Discipline.HCFACode != null
                                && p.EncounterOrTaskStartDateAndTime <= CurrentEncounter.EncounterOrTaskStartDateAndTime
                                && p.ServiceType.DisciplineKey == CurrentEncounter.DisciplineKey)
                    .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime)
                    .FirstOrDefault();

                // an employee that is an assistant cannot do evals, resumptions, discharges, transfers or POCs
                if (CurrentForm != null && ServiceTypeKey > 0)
                {
                    var st = ServiceTypeCache.GetServiceTypeFromKey(ServiceTypeKey);
                    var up = UserCache.Current.GetCurrentUserProfile();
                    var IsOrderEntryAndHavePermission = CurrentForm.IsOrderEntry &&
                                                        RoleAccessHelper.CheckPermission(RoleAccess.OrderEdit, false);
                    IsOrderEntryAndHavePermission = IsOrderEntryAndHavePermission ||
                                                    (CurrentForm.IsOasis &&
                                                     RoleAccessHelper.CheckPermission(RoleAccess.OASISEntry, false)) ||
                                                    (CurrentForm.IsHIS &&
                                                     RoleAccessHelper.CheckPermission(RoleAccess.HISEntry, false));

                    var userIsAssistantOfServiceType =
                        TaskSchedulingHelper.UserIsAssistantOfServiceType(st.DisciplineKey, up);

                    if (st != null && up != null)
                    {
                        if (
                            !IsOrderEntryAndHavePermission &&
                            (
                                (userIsAssistantOfServiceType && !CurrentForm.IsVisit) ||
                                (!CurrentForm.IsOrderEntry && !CurrentForm.IsOasis && !CurrentForm.IsHIS &&
                                 !CurrentForm.IsHospiceF2F && !CurrentForm.IsVerbalCOTI && !CurrentForm.IsCOTI &&
                                 !TaskSchedulingHelper.UserCanPerformServiceType(st, up))
                            )
                        )
                        {
                            if (CurrentForm.IsAttempted == false)
                            {
                                ErrorMessage = "You do not have the appropriate permissions to perform this encounter.";
                                CanContinue = false;
                                IsBusy = false;
                                return;
                            }
                        }
                    }
                }

                if ((CurrentForm != null) && CurrentForm.IsHospiceElectionAddendum)
                {
                    if (RoleAccessHelper.CheckPermission(RoleAccess.HospiceElectionAddendum, false) == false)
                    {
                        ErrorMessage =
                            "You do not have the appropriate permissions (Hospice Election Addendum role) to perform this encounter.";
                        CanContinue = false;
                        IsBusy = false;
                        return;
                    }
                }

                if ((CurrentForm != null) && CurrentForm.IsHospiceElectionAddendum && (CurrentAdmission != null) &&
                    (CurrentAdmission.HIBInsuranceElectionAddendumAvailable == false))
                {
                    ErrorMessage =
                        "The Admission HIB Insurance must be setup to support Election Addendum Available to perform this encounter.";
                    CanContinue = false;
                    IsBusy = false;
                    return;
                }

                if ((CurrentAdmission != null) && (CurrentForm != null) && CurrentAdmission.PreEvalRequired)
                {
                    ErrorMessage = null;
                    if ((CurrentForm.IsPreEval) && (CurrentAdmission.AdmissionStatusCode == "A"))
                    {
                        ErrorMessage = "Once the patient is admitted a pre-admission encounter cannot be performed.";
                    }
                    else if ((CurrentForm != null) && (CurrentForm.IsOasis == false) && (CurrentForm.IsHIS == false) &&
                             (CurrentForm.IsOrderEntry == false) && (CurrentForm.IsAttempted == false) &&
                             (CurrentForm.IsHospiceF2F == false) && (CurrentForm.IsVerbalCOTI == false) &&
                             (CurrentForm.IsCOTI == false) && (CurrentForm.IsTeamMeeting == false) &&
                             (CurrentForm.IsPreEval == false))
                    {
                        bool isCompletedPreEvalOnFile =
                            (CurrentAdmission.Encounter.Where(p =>
                                    p.EncounterStatus == (int)EncounterStatusType.Completed && p.EncounterIsPreEval)
                                .FirstOrDefault() == null)
                                ? false
                                : true;
                        if ((isCompletedPreEvalOnFile == false) || (CurrentAdmission.AdmissionStatusCode == "H"))
                        {
                            ErrorMessage =
                                "A pre-admission encounter must be completed before starting another encounter";
                        }
                    }

                    if (string.IsNullOrWhiteSpace(ErrorMessage) == false)
                    {
                        CanContinue = false;
                        IsBusy = false;
                        return;
                    }
                }

                if ((CurrentForm != null) &&
                    (CurrentForm.IsOasis == false) && (CurrentForm.IsHIS == false) &&
                    (CurrentForm.IsOrderEntry == false) && (CurrentForm.IsTeamMeeting == false) &&
                    (CurrentForm.IsAttempted == false) && (CurrentForm.IsHospiceF2F == false) &&
                    (CurrentForm.IsVerbalCOTI == false) && (CurrentForm.IsCOTI == false) &&
                    (CurrentForm.IsWOCN == false) && (CurrentForm.IsPlanOfCare == false) &&
                    (CurrentForm.IsHospiceElectionAddendum == false))
                {
                    if (mostRecentDisciplineEncounter != null)
                    {
                        CanContinue = (mostRecentDisciplineEncounter.EncounterStatus == (int)EncounterStatusType.Edit)
                            ? false
                            : true;

                        if (!CanContinue)
                        {
                            string activityPrefix = "Visit";

                            if (mostRecentDisciplineEncounter.Form.IsPlanOfCare)
                            {
                                activityPrefix = "Plan of Care";
                            }
                            else if (mostRecentDisciplineEncounter.Form.IsOrderEntry)
                            {
                                activityPrefix = "Order";
                            }
                            else if (mostRecentDisciplineEncounter.Form.IsTeamMeeting)
                            {
                                activityPrefix = "Team Meeting";
                            }
                            else if (mostRecentDisciplineEncounter.Form.IsOasis)
                            {
                                activityPrefix = "Oasis";
                            }
                            else if (mostRecentDisciplineEncounter.Form.IsHIS)
                            {
                                activityPrefix = "HIS";
                            }
                            else if (mostRecentDisciplineEncounter.Form.IsEval)
                            {
                                activityPrefix = "Evaluation";
                            }
                            else if (mostRecentDisciplineEncounter.Form.IsDischarge)
                            {
                                activityPrefix = "Discharge";
                            }
                            else if (mostRecentDisciplineEncounter.Form.IsTransfer)
                            {
                                activityPrefix = "Transfer";
                            }
                            else if (mostRecentDisciplineEncounter.Form.IsResumption)
                            {
                                activityPrefix = "Resumption";
                            }

                            ErrorMessage = string.Format(
                                "A previous {0} dated {1} must be completed prior to starting another encounter.",
                                activityPrefix,
                                mostRecentDisciplineEncounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault()
                                    .DateTime.ToShortDateString());
                            IsBusy = false;
                            return;
                        }
                    }
                }

                SetupNewEncounter();
            } //end new encounter

            if ((CurrentOrderEntryManager != null) && (CurrentForm != null) && CurrentForm.IsAttempted)
            {
                CurrentOrderEntryManager.ForceDiscardAttemptedVisit();
            }

            CurrentEncounter.ServiceLineKey = CurrentAdmission.ServiceLineKey;
            if (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.None ||
                CurrentEncounter.EncounterKey <= 0)
            {
                if (CurrentEncounter.EncounterIsOrderEntry == false)
                {
                    IfLastIndexSelectedGoToFirstTab = true;
                }
            }

            if (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed)
            {
                CurrentEncounter.NewDiagnosisVersion = true;
            }

            // previous status is stored in and re-loaded from the offline file.
            if (!_FormLoadedFromDisk)
            {
                PreviousEncounterStatus = CurrentEncounter.EncounterStatus;
            }

            PreviousSigned = CurrentEncounter.Signed;
            CurrentEncounter.PreviousEncounterStatus = PreviousEncounterStatus;
            CurrentEncounter.PreviousEncounterCollectedBy = CurrentEncounter.EncounterCollectedBy;
            SaveInitialStatusData();

            SetupOKCommandVisibility();

            CurrentEncounterPlanOfCare = CurrentEncounter.EncounterPlanOfCare.FirstOrDefault();
            if (CurrentEncounterPlanOfCare != null && CurrentForm.IsPlanOfCare)
            {
                PreviousPOCMailedDate = CurrentEncounterPlanOfCare.MailedDate;
                PreviousPOCMailedBy = CurrentEncounterPlanOfCare.MailedBy;
                PreviousPOCSignedDate = CurrentEncounterPlanOfCare.SignedDate;
                PreviousPOCSignedBy = CurrentEncounterPlanOfCare.SignedBy;
                CurrentAdmission.SetProviderFacility(CurrentEncounterPlanOfCare.CertificationFromDate);
                if (CurrentAdmission.ProviderFacility == null)
                {
                    DateTime dt = (CurrentEncounter.EncounterOrTaskStartDateAndTime == null)
                        ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date
                        : ((DateTimeOffset)CurrentEncounter.EncounterOrTaskStartDateAndTime).Date;
                    CurrentAdmission.SetProviderFacility(dt, true);
                }
            }
            else
            {
                CurrentAdmission.SetProviderFacility(((CurrentEncounter.EncounterOrTaskStartDateAndTime == null)
                    ? DateTime.Today.Date
                    : CurrentEncounter.EncounterOrTaskStartDateAndTime.Value.Date));
            }

            CurrentEncounterTransfer = CurrentEncounter.EncounterTransfer.FirstOrDefault();

            //Create the GoalManager - for REHAB section
            CurrentGoalManager = GoalManager.Create(FormModel, this);
            //Create the OASIS Manager here -  WarnDischargeReferredButNotAssessed may override bypass
            CurrentOasisManager = OasisManager.Create(this);

            WarnDischargeReferredButNotAssessed();

            if (AddingNewEncounter == false)
            {
                SetupExistingEncounterRefreshDiagnosis();
            }

            if (BackgroundService.IsBackground == false)
            {
                Messenger.Default.Register<bool>(this,
                    string.Format("OasisBypassFlagChanged{0}", CurrentOasisManager.OasisManagerGuid.ToString().Trim()),
                    b => OasisBypassFlagChanged(b));
                Messenger.Default.Register<int>(this,
                    string.Format("OasisAlertsChanged{0}", CurrentOasisManager.OasisManagerGuid.ToString().Trim()),
                    i => OasisAlertsChanged(i));
                Messenger.Default.Register<bool>(this,
                    string.Format("DoSupervisionWithVisitChanged{0}", CurrentEncounter.EncounterID.ToString().Trim()),
                    b => DoSupervisionWithVisitChanged(b));
                Messenger.Default.Register<bool>(this,
                    string.Format("RefreshFilteredSections{0}", CurrentEncounter.EncounterID.ToString().Trim()),
                    b => RefreshFilteredSections(b));
            }

            EncounterSetup();
            OasisSetup(AddingNewEncounter);
            ProcessFormSections(CurrentForm, Sections, false, true);

            // Lite up ICDCode sections of interest
            if (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.CoderReview)
            {
                foreach (SectionUI s in Sections)
                    if (s.Label.ToLower().Trim() == "pre-admission/history" ||
                        s.Label.ToLower().Trim() == "medical diagnosis(es)" ||
                        s.Label.ToLower().Trim() == "patient history & icd")
                    {
                        s.IsICDNoteVisible = true;
                    }
            }

            if (CurrentForm.IsAuthorizationRequest ==
                false) //keep first section selected if type is AuthorizationRequest - form has a signature section, but no patient demographics section...
            {
                SelectedSection = Sections.ElementAt(((Sections.Count() == 1) ? 0 : 1));
            }

            CurrentGoalManager.FilterAll(true, true);

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //NOTE: ProcessFormSections might navigateback/close/cleanup - which sets FormModel to NULL, had this happen for a task
            if ((dataLoadType == DataLoadType.SERVER) && (FormModel != null))
            {
                //Cache the form on open in case they immediately go offline
                await SaveDynamicForm(
                    (CurrentPatient != null) ? CurrentPatient.PatientKey : -1,
                    (CurrentEncounter != null) ? CurrentEncounter.EncounterKey : -1,
                    (CurrentEncounter != null) ? CurrentEncounter.EncounterStatus : (int)EncounterStatusType.Edit,
                    OfflineStoreType.CACHE,
                    false);
            }
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            if (CurrentAdmission != null)
            {
                CurrentAdmission.InDynamicForm = true;
            }

            IsBusy = false;
        }

        private bool SetupAttemptedEncounterPart1()
        {
            // Split into 2 parts because:
            // We want to create the EncounterAttempted row when we launch the form, but not attach it to the Encounter until OK is pressed,
            // Otherwise, offline - a canceled attempt would not be possible - as we update the encounter/task cache now, during initialization, before the first OK is pressed
            // Insure we have encoungh toys to play with, an encounter and a task
            if (CurrentEncounter == null)
            {
                ErrorMessage = "Error: Setting up Attempted Visit - No Current Encounter.  contact AlayaCare support";
                return false;
            }

            if (CurrentEncounter.EncounterAttempted == null)
            {
                ErrorMessage =
                    "Error: Setting up Attempted Visit - No EncounterAttempted defined.  contact AlayaCare support";
                return false;
            }

            if (CurrentTask == null)
            {
                ErrorMessage = "Error: Setting up Attempted Visit - No Current Task.  contact AlayaCare support";
                return false;
            }

            ServiceType attemptedServiceType = null;
            Form attemptedForm = null;
            // It we are already an atempted visit - note so and exit
            CurrentEncounterAttempted = CurrentEncounter.EncounterAttempted.FirstOrDefault();
            if (CurrentEncounterAttempted != null)
            {
                ServiceTypeKey = (int)CurrentEncounter.ServiceTypeKey;
                FormKey = (int)CurrentEncounter.FormKey;
                CurrentForm = DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey);
                // Recheck and insure servicetype is attempted
                attemptedServiceType = ServiceTypeCache.GetServiceTypeFromKey((int)CurrentEncounter.ServiceTypeKey);
                bool IsAttemptedST = (attemptedServiceType == null) ? false : attemptedServiceType.IsAttempted;
                if (IsAttemptedST == false)
                {
                    attemptedServiceType = ServiceTypeCache.GetAttemptedServiceType();
                    if (attemptedServiceType == null)
                    {
                        ErrorMessage =
                            "Error: Setting up Attempted Visit - No Attempted Service Type defined.  contact AlayaCare support";
                        return false;
                    }

                    Metrics.Ria.NullMetricLogger.Log(
                        string.Format("[002] ServiceTypeKey.{0} = attemptedServiceType.ServiceTypeKey.{1}",
                            ServiceTypeKey, attemptedServiceType.ServiceTypeKey), CorrelationIDHelper,
                        EntityManager.IsOnline, "DynamicFormViewModel.SetupAttemptedEncounterPart1", CachedURI);

                    ServiceTypeKey = attemptedServiceType.ServiceTypeKey;

                    FormKey = (int)attemptedServiceType.FormKey;
                    CurrentForm = DynamicFormCache.GetFormByKey((int)attemptedServiceType.FormKey);
                }

                // Recheck and insure form is attempted
                attemptedForm = DynamicFormCache.GetFormByKey((int)attemptedServiceType.FormKey);
                bool IsAttemptedF = (attemptedForm == null) ? false : attemptedForm.IsAttempted;
                if (IsAttemptedF == false)
                {
                    attemptedForm = DynamicFormCache.GetAttemptedForm();
                    if (attemptedForm == null)
                    {
                        ErrorMessage =
                            "Error: Setting up Attempted Visit - No Attempted Form defined.  contact AlayaCare support";
                        return false;
                    }

                    FormKey = attemptedForm.FormKey;
                    CurrentForm = attemptedForm;
                }

                return true;
            }

            // Make sure conditions are right for an attempt - i.e., we were launched as an attempt, and we have the right encounter status, the right user and the right task type 
            if (IsAttemptedEncounter == false)
            {
                return true;
            }

            if ((CurrentEncounter.EncounterStatus != (int)EncounterStatusType.None) &&
                (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Edit))
            {
                return true;
            }

            if (CurrentTask.UserID != WebContext.Current.User.MemberID)
            {
                return true;
            }

            if (CurrentTask.TaskIsEvalOrVisitOrResumption == false)
            {
                return true;
            }

            // we fell thru, set up and attempted visit
            attemptedServiceType = ServiceTypeCache.GetAttemptedServiceType();
            if (attemptedServiceType == null)
            {
                ErrorMessage =
                    "Error: Setting up Attempted Visit - No Attempted Service Type defined.  contact AlayaCare support";
                return false;
            }

            attemptedForm = (attemptedServiceType.FormKey == null)
                ? null
                : DynamicFormCache.GetFormByKey((int)attemptedServiceType.FormKey);
            if ((attemptedForm == null) || (attemptedForm.IsAttempted == false))
            {
                ErrorMessage = "Error: Setting up Attempted Visit - No Attempted Form defined.  contact AlayaCare support";
                return false;
            }

            CurrentEncounterAttempted = new EncounterAttempted
            {
                EncounterKey = CurrentEncounter.EncounterKey,
                TenantID = CurrentEncounter.TenantID,
                ServiceTypeKey = ((CurrentTask.ServiceTypeKey == null) ? 0 : (int)CurrentTask.ServiceTypeKey),
                TaskStartDateTime = CurrentTask.TaskStartDateTime,
                TaskDuration = CurrentTask.TaskDuration,
                UserID = CurrentTask.UserID
            };
            CurrentForm = attemptedForm;
            return true;
        }

        private void SetupAttemptedEncounterPart2()
        {
            if ((CurrentEncounter == null) || (CurrentEncounter.EncounterAttempted == null) || (CurrentTask == null) ||
                (CurrentEncounterAttempted == null))
            {
                return;
            }

            ServiceType attemptedServiceType = ServiceTypeCache.GetAttemptedServiceType();

            Form attemptedForm = ((attemptedServiceType == null) || (attemptedServiceType.FormKey == null))
                ? null
                : DynamicFormCache.GetFormByKey((int)attemptedServiceType.FormKey);
            if ((attemptedForm == null) || (attemptedForm.IsAttempted == false))
            {
                return;
            }

            if ((CurrentEncounter.EncounterAttempted.Contains(CurrentEncounterAttempted) == false) && (UserHitOK))
            {
                CurrentEncounter.EncounterAttempted.Add(CurrentEncounterAttempted);

                Metrics.Ria.NullMetricLogger.Log(
                    string.Format("[003] CurrentEncounter.ServiceTypeKey.{0} = attemptedServiceType.ServiceTypeKey.{1}",
                        CurrentEncounter.ServiceTypeKey, attemptedServiceType.ServiceTypeKey), CorrelationIDHelper,
                    EntityManager.IsOnline, "DynamicFormViewModel.SetupAttemptedEncounterPart2", CachedURI);

                CurrentEncounter.ServiceTypeKey = attemptedServiceType.ServiceTypeKey;

                CurrentEncounter.FormKey = CurrentForm.FormKey;
                ;

                Metrics.Ria.NullMetricLogger.Log(
                    string.Format("[005] ServiceTypeKey.{0} = (int)CurrentEncounter.ServiceTypeKey.{1}", ServiceTypeKey,
                        (int)CurrentEncounter.ServiceTypeKey), CorrelationIDHelper, EntityManager.IsOnline,
                    "DynamicFormViewModel.SetupAttemptedEncounterPart2", CachedURI);

                ServiceTypeKey = (int)CurrentEncounter.ServiceTypeKey;

                FormKey = (int)CurrentEncounter.FormKey;
                CurrentTask.IsAttempted = true;
                CurrentTask.ServiceTypeKey = attemptedServiceType.ServiceTypeKey;
            }
        }

        public string POCSendAddendum
        {
            get
            {
                if (CurrentEncounterPlanOfCare == null)
                {
                    return null;
                }

                if ((PreviousPOCMailedDate == null) && (CurrentEncounterPlanOfCare.MailedDate == null))
                {
                    return null;
                }

                if ((PreviousPOCMailedDate == CurrentEncounterPlanOfCare.MailedDate) &&
                    (PreviousPOCMailedBy == CurrentEncounterPlanOfCare.MailedBy))
                {
                    return null;
                }

                // POC mailed - and/or mailed data changed
                return CurrentEncounterPlanOfCare.SendBlirb;
            }
        }

        public string POCSignedAddendum
        {
            get
            {
                if (CurrentEncounterPlanOfCare == null)
                {
                    return null;
                }

                if ((PreviousPOCSignedDate == null) && (CurrentEncounterPlanOfCare.SignedDate == null))
                {
                    return null;
                }

                if ((PreviousPOCSignedDate == CurrentEncounterPlanOfCare.SignedDate) &&
                    (PreviousPOCSignedBy == CurrentEncounterPlanOfCare.SignedBy))
                {
                    return null;
                }

                // POC signed -and/or signed data changed
                return CurrentEncounterPlanOfCare.VerifiedPhysicianSignatureBlirb;
            }
        }

        public int DetermineCurrentAdmissionDiscipline()
        {
            var ret = 0;
            if (CurrentEncounter != null)
            {
                DateTimeOffset DateToUse = (CurrentEncounter.EncounterOrTaskStartDateAndTime.HasValue == false)
                    ? CurrentTask.TaskStartDateTime
                    : CurrentEncounter.EncounterOrTaskStartDateAndTime.Value;
                DateToUse = DateToUse.Date;
                CurrentAdmissionDiscipline = CurrentAdmission.AdmissionDiscipline
                    .Where(ad => ad.AdmissionDisciplineKey == CurrentEncounter.AdmissionDisciplineKey).FirstOrDefault();
                if (CurrentAdmissionDiscipline == null)
                {
                    CurrentAdmissionDiscipline = CurrentAdmission.AdmissionDiscipline.Where(ad =>
                            ad.DisciplineKey.Equals(
                                ServiceTypeCache.GetDisciplineKey(CurrentTask.ServiceTypeKey.Value)) &&
                            (DateToUse >= ad.ReferDate))
                        .OrderByDescending(ad => ad.ReferDateTime).FirstOrDefault();
                }

                if (IsReadOnlyEncounter && CurrentAdmissionDiscipline == null)
                {
                    CurrentAdmissionDiscipline = CurrentEncounter.AdmissionDiscipline_1.Where(ad =>
                            ad.DisciplineKey.Equals(
                                ServiceTypeCache.GetDisciplineKey(CurrentTask.ServiceTypeKey.Value)) &&
                            (DateToUse >= ad.ReferDate))
                        .OrderByDescending(ad => ad.ReferDateTime).FirstOrDefault();
                }

                CurrentEncounter.CurrentAdmissionDiscipline = CurrentAdmissionDiscipline;
            }
            else
            {
                if (CurrentEncounter.AdmissionDisciplineKey.HasValue)
                {
                    CurrentAdmissionDiscipline = CurrentAdmission.AdmissionDiscipline
                        .Where(ad => ad.AdmissionDisciplineKey == CurrentEncounter.AdmissionDisciplineKey)
                        .FirstOrDefault();
                }
                else
                {
                    var DateToUse2 = CurrentTask.TaskStartDateTime;
                    if (DateToUse2 != null)
                    {
                        DateToUse2 = DateToUse2.Date;
                    }

                    CurrentAdmissionDiscipline = CurrentAdmission.AdmissionDiscipline.Where(ad =>
                            ad.DisciplineKey.Equals(
                                ServiceTypeCache.GetDisciplineKey(CurrentTask.ServiceTypeKey.Value)) &&
                            (DateToUse2 >= ad.ReferDate))
                        .OrderByDescending(ad => ad.ReferDateTime).FirstOrDefault();
                }
            }

            if (CurrentAdmissionDiscipline != null)
            {
                if (BackgroundService.IsBackground == false)
                {
                    Messenger.Default.Register<AdmissionDiscipline>(this,
                        string.Format("FormAdmissionDisciplineChanged{0}",
                            CurrentAdmissionDiscipline.AdmissionKey.ToString().Trim()),
                        a => SetupAdmissionDisciplineChanged(a));
                }
            }
            else if ((CurrentAdmissionDiscipline == null) && (CurrentForm.IsOasis == false) &&
                     (CurrentForm.IsHIS == false) && (CurrentForm.IsOrderEntry == false) &&
                     (CurrentForm.IsAttempted == false) && (CurrentForm.IsTeamMeeting == false) &&
                     (CurrentForm.IsHospiceF2F == false) && (CurrentForm.IsVerbalCOTI == false) &&
                     (CurrentForm.IsCOTI == false))
            {
                CanContinue = false;
                ErrorMessage = "Unable to locate an admission discipline record for the encounter.";
                ret = -1;
            }
            else if ((CurrentAdmissionDiscipline == null) && (CurrentForm.IsOasis || CurrentForm.IsHIS ||
                                                              CurrentForm.IsOrderEntry || CurrentForm.IsAttempted ||
                                                              CurrentForm.IsTeamMeeting || CurrentForm.IsHospiceF2F ||
                                                              CurrentForm.IsVerbalCOTI || CurrentForm.IsCOTI))
            {
                CurrentAdmissionDiscipline = new AdmissionDiscipline();
                CurrentAdmissionDiscipline.AgencyDischarge = false;
                CurrentAdmissionDiscipline.OverrideAgencyDischarge = false;
            }

            return ret;
        }

        private void SetupOKCommandVisibility()
        {
            if (CurrentUserIsSurveyor)
            {
                // the form has been launched as read only
                OK_CommandVisible = false;
            }
            else if (IsReadOnlyEncounter)
            {
                // the form has been launched as read only
                OK_CommandVisible = false;
            }
            else if (_FormLoadedFromDisk)
            {
                // if we saved it offline they should be able to 
                // save it again to submit it (even if they signed it previously offline).
                OK_CommandVisible = true;
            }
            else if (CurrentEncounter.Inactive)
            {
                MessageBox.Show("Form is inactive, no form edits are allowed.");
                OK_CommandVisible = false;
            }
            else if ((CurrentEncounter.EncounterStatus == (int)EncounterStatusType.CoderReview) &&
                     (RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false)) == false)
            {
                MessageBox.Show("Form is awaiting diagnosis code review, no form edits are allowed.");
                OK_CommandVisible = false;
            }
            else if ((CurrentEncounter.SYS_CDIsHospice == false) &&
                     (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.OASISReview) &&
                     (RoleAccessHelper.CheckPermission(RoleAccess.OASISCoordinator, false)) == false)
            {
                MessageBox.Show("Form is awaiting OASIS coordinator review, no form edits are allowed.");
                OK_CommandVisible = false;
            }
            else if ((CurrentEncounter.SYS_CDIsHospice) &&
                     (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.OASISReview) &&
                     (RoleAccessHelper.CheckPermission(RoleAccess.HISCoordinator, false)) == false)
            {
                MessageBox.Show("Form is awaiting HIS coordinator review, no form edits are allowed.");
                OK_CommandVisible = false;
            }
            else if ((CurrentEncounter.EncounterStatus == (int)EncounterStatusType.POCOrderReview) &&
                     (RoleAccessHelper.CheckPermission(RoleAccess.POCOrderReviewer, false)) == false)
            {
                MessageBox.Show("Form is awaiting POC order review, no form edits are allowed.");
                OK_CommandVisible = false;
            }
            else if ((CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Edit) ||
                     (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.CoderReviewEdit) ||
                     (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.OASISReviewEdit) ||
                     (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.OASISReviewEditRR))
            {
                if (CurrentEncounter.EncounterBy != WebContext.Current.User.MemberID)
                {
                    MessageBox.Show("Form is incomplete, only the owning Clinician can perform edits.");
                    OK_CommandVisible = false;
                }
            }
            else if (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed &&
                     CurrentEncounter.EncounterKey >= 0 && RoleAccessHelper.CheckPermission(RoleAccess.Admin, false))
            {
                // Must allow the  System Administrator role to change a service date, service start time, service end date and service end time for a completed encounter.
                AdministrativeServiceDateOverride = true;
                OK_CommandVisible = true;
                EncounterOriginal.EncounterStartDate = CurrentEncounter.EncounterStartDate;
                EncounterOriginal.EncounterStartTime = CurrentEncounter.EncounterStartTime;
                EncounterOriginal.EncounterEndDate = CurrentEncounter.EncounterEndDate;
                EncounterOriginal.EncounterEndTime = CurrentEncounter.EncounterEndTime;
                EncounterOriginal.PatientAddressKey = CurrentEncounter.PatientAddressKey;
                EncounterOriginal.EncounterActualTime = CurrentEncounter.EncounterActualTime;
                EncounterOriginal.AdmissionDisciplineKey = CurrentEncounter.AdmissionDisciplineKey;
                CurrentEncounter.TrackChangedProperties = true;
                CurrentEncounter.PropertyChanged += CurrentEncounter_PropertyChanged;
            }

            // support changes to discharged Patients by SysAdmin
            if (CurrentForm.IsDischarge 
                && CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed 
                && RoleAccessHelper.CheckPermission(RoleAccess.Admin, false))
            {
                CurrentAdmissionDiscipline.PropertyChanged += Discharge_PropertyChanged;
            }
        }

        public bool DischargeWasChanged;

        void CurrentEncounter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GenerateAddendumTextForServiceDateChange(e.PropertyName);
        }

        void Discharge_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DischargeDateTime")
            {
                var propertiesChangedList = new List<PropertyChange<object>>();
                var origDischargeDate = ((AdmissionDiscipline)CurrentAdmissionDiscipline.GetOriginal() == null
                    ? null
                    : ((AdmissionDiscipline)CurrentAdmissionDiscipline.GetOriginal()).DischargeDateTime);
                var currDischargeDate = CurrentAdmissionDiscipline.DischargeDateTime;
                if (origDischargeDate != currDischargeDate)
                {
                    var propertyDetails = CurrentAdmissionDiscipline.GetType().GetProperty("DischargeDateTime");
                    var dischargeDateChange = new PropertyChange<object>(propertyDetails, origDischargeDate,
                        currDischargeDate,
                        location: CurrentAdmissionDiscipline.DisciplineDescription);
                    propertiesChangedList.Add(dischargeDateChange);
                    var addendum = (Addendum)Addendum;
                    if (addendum != null)
                    {
                        addendum.LogPropertyChanges(UserCache.Current.GetCurrentUserProfile(),
                            TenantSettingsCache.Current, propertiesChangedList);
                    }
                }

                if (CurrentAdmissionDiscipline.DischargeDateTime.HasValue)
                {
                    CurrentAdmissionDiscipline.AdmissionStatus =
                        (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "D");
                }

                DischargeWasChanged = true;
            }

            if (e.PropertyName == "DischargeReasonKey")
            {
                var propertiesChangedList = new List<PropertyChange<object>>();
                var origDischargeReasonKey =
                    ((AdmissionDiscipline)CurrentAdmissionDiscipline.GetOriginal()).DischargeReasonKey;
                var currDischargeReasonKey = CurrentAdmissionDiscipline.DischargeReasonKey;
                if (origDischargeReasonKey != currDischargeReasonKey)
                {
                    var propertyDetails = CurrentAdmissionDiscipline.GetType().GetProperty("DischargeReasonKey");
                    var dischargeReasonChange = new PropertyChange<object>(propertyDetails, origDischargeReasonKey,
                        currDischargeReasonKey,
                        location: CurrentAdmissionDiscipline.DisciplineDescription);
                    propertiesChangedList.Add(dischargeReasonChange);
                    var addendum = (Addendum)Addendum;
                    if (addendum != null)
                    {
                        addendum.LogPropertyChanges(UserCache.Current.GetCurrentUserProfile(),
                            TenantSettingsCache.Current, propertiesChangedList);
                    }
                }

                DischargeWasChanged = true;
            }

            if (DischargeWasChanged)
            {
                PropagateDischargeChangeToAdmission();
                DischargeWasChanged = false;
            }
        }

        public void PropagateDischargeChangeToAdmission()
        {
            if (CurrentForm.IsDischarge &&
                CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed &&
                RoleAccessHelper.CheckPermission(RoleAccess.Admin, false))
            {
                //TODO: confirm discipline changes need to be in the addendum or not
                var propertiesChangedList = CurrentAdmission.OverrideAdmissionDischargedDate();
                var addendum = (Addendum)Addendum;
                if (addendum != null)
                {
                    addendum.LogPropertyChanges(UserCache.Current.GetCurrentUserProfile(), TenantSettingsCache.Current,
                        propertiesChangedList);
                }
            }
        }
        
        void GenerateAddendumTextForServiceDateChange(string propChangeName)
        {
            var watching = new List<string>
            {
                "EncounterStartDate", "EncounterStartTime", "EncounterEndDate", "EncounterEndTime",
                "EncounterActualTime", "PatientAddressKey"
            };
            if (watching.Any(prop => propChangeName.Equals(prop)))
            {
                bool useMilitaryTime = TenantSettingsCache.Current.TenantSetting.UseMilitaryTime;
                //NOTE: Must allow the  System Administrator role to change a service date, service start time, 
                //      service end date and service end time for a completed encounter.
                //      Changes will write to the addendum for the encounter with the user, date time, and what the data was changed from and to.
                if (Addendum is Addendum)
                {
                    var addendum = (Addendum)Addendum;
                    StringBuilder sb = new StringBuilder();
                    CurrentEncounter.ChangedProperties.ForEach(prop =>
                    {
                        if (prop.Equals("EncounterStartDate") &&
                            EncounterOriginal.EncounterStartDate.Equals(CurrentEncounter.EncounterStartDate) == false)
                        {
                            sb.AppendLine(
                                string.Format("Start Date changed from {0:MM/dd/yy} to {1:MM/dd/yy}",
                                    EncounterOriginal.EncounterStartDate,
                                    CurrentEncounter.EncounterStartDate
                                ));
                        }
                        
                        if (prop.Equals("EncounterStartTime") && EncounterOriginal.EncounterStartTime !=
                            CurrentEncounter.EncounterStartTime)
                        {
                            sb.AppendLine(string.Format("Start Time changed from {0} to {1}",
                                GetTimeText(useMilitaryTime, EncounterOriginal.EncounterStartTime),
                                GetTimeText(useMilitaryTime, CurrentEncounter.EncounterStartTime)));
                        }

                        if (prop.Equals("EncounterEndDate") &&
                            EncounterOriginal.EncounterEndDate.Equals(CurrentEncounter.EncounterEndDate) == false)
                        {
                            sb.AppendLine(
                                string.Format("End Date changed from {0:MM/dd/yy} to {1:MM/dd/yy}",
                                    EncounterOriginal.EncounterEndDate,
                                    CurrentEncounter.EncounterEndDate
                                ));
                        }

                        if (prop.Equals("EncounterEndTime") &&
                            EncounterOriginal.EncounterEndTime.Equals(CurrentEncounter.EncounterEndTime) == false)
                        {
                            sb.AppendLine(string.Format("End Time changed from {0} to {1}",
                                GetTimeText(useMilitaryTime, EncounterOriginal.EncounterEndTime),
                                GetTimeText(useMilitaryTime, CurrentEncounter.EncounterEndTime)));
                        }

                        if (prop.Equals("EncounterActualTime") &&
                            EncounterOriginal.EncounterActualTime.Equals(CurrentEncounter.EncounterActualTime) == false)
                        {
                            sb.AppendLine(
                                string.Format("Total Minutes changed from {0} to {1}",
                                    EncounterOriginal.EncounterActualTime.GetValueOrDefault(),
                                    CurrentEncounter.EncounterActualTime.GetValueOrDefault()));
                        }

                        if (prop.Equals("PatientAddressKey") &&
                            EncounterOriginal.PatientAddressKey != CurrentEncounter.PatientAddressKey)
                        {
                            sb.AppendLine("Patient Address changed");
                            sb.AppendLine("from: ");
                            sb.AppendLine(string.Format("{0}",
                                GetPatientAddressAddendumText(EncounterOriginal.PatientAddressKey
                                    .GetValueOrDefault())));
                            sb.AppendLine("to: ");
                            sb.AppendLine(string.Format("{0}",
                                GetPatientAddressAddendumText(CurrentEncounter.PatientAddressKey.GetValueOrDefault())));
                        }
                    });
                    if (sb.Length > 0)
                    {
                        addendum.AddendumText = sb.ToString();
                    }
                }
            }
        }

        private string GetTimeText(bool useMilitaryTime, DateTimeOffset? _time)
        {
            if (_time.HasValue)
            {
                return useMilitaryTime ? _time.Value.ToString("HHmm") : _time.Value.DateTime.ToShortTimeString();
            }

            return null;
        }

        string GetPatientAddressAddendumText(int _PatientAddressKey)
        {
            if (_PatientAddressKey == 0)
            {
                return "";
            }

            var _indent = "        ";
            var _sb = new StringBuilder();
            if (CurrentEncounter.PatientAddressCollectionSource != null)
            {
                var _address = CurrentEncounter.PatientAddressCollectionSource
                    .Where(a => a.PatientAddressKey == _PatientAddressKey).FirstOrDefault();
                if (_address == null)
                {
                    return "";
                }

                var _cldConverter = new Converters.CodeLookupDescriptionFromKeyConverter();
                var _type = (string)_cldConverter.Convert(_address.Type, typeof(int?), null, null);

                if (_address.FacilityKey.HasValue)
                {
                    var _facilityDescr = new Converters.FacilityNameFromKeyConverter();
                    _sb.AppendLine(string.Format("{0}({1}) {2}", _indent, _type,
                        _facilityDescr.Convert(_address.FacilityKey, typeof(int?), null, null)));
                    if (!string.IsNullOrWhiteSpace(_address.RoomAptUnitWithPrefix))
                    {
                        _sb.AppendLine(string.Format("{0}{1}", _indent, _address.RoomAptUnitWithPrefix));
                    }
                }
                else
                {
                    _sb.AppendLine(string.Format("{0}({1})", _indent, _type));
                }

                _sb.AppendLine(string.Format("{0}{1}", _indent, _address.Address1));
                if (!string.IsNullOrWhiteSpace(_address.Address2))
                {
                    _sb.AppendLine(string.Format("{0}{1}", _indent, _address.Address2));
                }

                _sb.AppendLine(string.Format("{0}{1}", _indent, _address.CityStateZip));
                _sb.AppendLine(string.Format("{0}{1:d} - {2:d}", _indent, _address.EffectiveFromDate,
                    _address.EffectiveThruDate));
            }

            return _sb.ToString();
        }

        string ValidateUpdateDisciplineAdmissionKey()
        {
            var ret = string.Empty;
            // NOTE: If the form IsEval or IsResumption the service date will impact the Discipline admit date ( should be equal).  
            //       THis will feed into teh US 4245 for the SOC date and Cert periods.
            //       Notify the System Administrator that the dates are not equal and that this is the first service 
            //       for the discipline and that the admit date will be changed.
            if ((DynamicFormCache.IsEval(CurrentEncounter.FormKey.GetValueOrDefault())) ||
                (DynamicFormCache.IsResumption(CurrentEncounter.FormKey.GetValueOrDefault())))
            {
                //Start Date changed - update/validate the AdmissionDiscipline
                if (EncounterOriginal.EncounterStartDate.Equals(CurrentEncounter.EncounterStartDate) == false)
                {
                    var _currentAdmissionDiscipline = CurrentAdmission.AdmissionDiscipline
                        .Where(ad =>
                            ad.AdmissionDisciplineKey == CurrentEncounter.AdmissionDisciplineKey.GetValueOrDefault())
                        .FirstOrDefault();
                    {
                        //This is an EVAL - does the Encounter.EncounterStartDate equal the AdmissionDiscipline.ReferDateTime?
                        bool DisplayMessage = false;
                        string ChangeWhat = "";
                        if (_currentAdmissionDiscipline != null && !_currentAdmissionDiscipline.NotTaken &&
                            CurrentEncounter.EncounterStartDate.GetValueOrDefault().Date.Equals(
                                _currentAdmissionDiscipline.DisciplineAdmitDateTime.GetValueOrDefault().Date) == false)
                        {
                            DisplayMessage = true;
                            ChangeWhat = "Discipline Admit Date";
                        }

                        if (_currentAdmissionDiscipline != null && _currentAdmissionDiscipline.NotTaken &&
                            CurrentEncounter.EncounterStartDate.GetValueOrDefault().Date
                                .Equals(_currentAdmissionDiscipline.NotTakenDateTime.GetValueOrDefault().Date) == false)
                        {
                            DisplayMessage = true;
                            ChangeWhat = "Not Admitted Date";
                        }

                        if (DisplayMessage)
                        {
                            ret = string.Format(
                                "The {0} {1:MM/dd/yy} and the service date {2:MM/dd/yy} do not match. The {0} will be changed to the service date.",
                                ChangeWhat,
                                _currentAdmissionDiscipline.DisciplineAdmitDateTime.GetValueOrDefault().Date,
                                CurrentEncounter.EncounterStartDate.GetValueOrDefault().Date);

                            //append this data change to the addendum text.
                            var addendum = (Addendum)Addendum;
                            addendum.AddendumText += string.Format("{0} changed from {1:MM/dd/yy} to {2:MM/dd/yy}",
                                ChangeWhat,
                                _currentAdmissionDiscipline.DisciplineAdmitDateTime.GetValueOrDefault().Date,
                                CurrentEncounter.EncounterStartDate.GetValueOrDefault().Date);
                            if (ChangeWhat == "Discipline Admit Date")
                            {
                                _currentAdmissionDiscipline.DisciplineAdmitDateTime =
                                    CurrentEncounter.EncounterStartDate.GetValueOrDefault().Date;
                            }
                            else
                            {
                                _currentAdmissionDiscipline.NotTakenDateTime =
                                    CurrentEncounter.EncounterStartDate.GetValueOrDefault().Date;
                            }
                        }
                    }
                }
            }

            return ret;
        }

        public bool IsAssistantEncounter
        {
            get
            {
                var st = CurrentEncounter.ServiceType == null
                    ? (CurrentEncounter.ServiceTypeKey == null
                        ? null
                        : ServiceTypeCache.GetServiceTypeFromKey((int)CurrentEncounter.ServiceTypeKey))
                    : CurrentEncounter.ServiceType;
                // Only show the prompt if they haven't been asked before && the service type isn't listed as an assistant
                return st != null && st.IsAssistant;
            }
        }

        private void SetupNewEncounter()
        {
            CertManager.CreateCertIfNecessary(CurrentForm, CurrentAdmission, CurrentEncounter);
            AdmissionCertification acert =
                CurrentAdmission.GetAdmissionCertForDate(CurrentEncounter.EncounterOrTaskStartDateAndTime
                    .GetValueOrDefault().DateTime);

            // Discipline Recert question
            if (!CurrentAdmission.HideCertPeriods)
            {
                if (CurrentEncounter.GetIsEncounterInRecertWindow(CurrentAdmission, CurrentEncounter) &&
                    CurrentEncounter.ServiceTypeKey != null && (CurrentForm.IsVisitTeleMonitoring == false) &&
                    (CurrentForm.IsEval || CurrentForm.IsResumption || CurrentForm.IsVisit))
                {
                    Encounter ec = CurrentAdmission.Encounter
                        .Where(e => e.EncounterStatus !=
                                    (int)EncounterStatusType
                                        .None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                        .OrderByDescending(oe => oe.EncounterOrTaskStartDateAndTime)
                        .Where(ect =>
                            ect.EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime >= acert.PeriodStartDate &&
                            ect.EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime <= acert.PeriodEndDate
                            && ect.ServiceTypeKey != null
                            && ServiceTypeCache.GetDisciplineKey((int)ect.ServiceTypeKey) ==
                            ServiceTypeCache.GetDisciplineKey((int)CurrentEncounter.ServiceTypeKey)
                            && ect.ReCertDiscipline != null)
                        .FirstOrDefault();

                    // Only show the prompt if they haven't been asked before && the service type isn't listed as an assistant
                    if (ec == null && !IsAssistantEncounter)
                    {
                        InsuranceCertDefinition def = null;
                        // Missing HIB Number selection in Admission/Referral
                        if (CurrentAdmission.PatientInsurance != null)
                        {
                            def = CurrentAdmission.PatientInsurance.Insurance.GetNextCertDefinition(acert.PeriodNumber);
                        }

                        var converter = new Converters.DocumentDescriptionPlusSuffix();
                        var ret = (string)converter.Convert(CurrentTask, null, null, null);

                        String Msg = string.Format(
                            "For {0} and {1} on {2}.\nWill this discipline be part of the next certification period?",
                            CurrentPatient.FullNameInformal,
                            ret,
                            CurrentTask.TaskStartEnd);
                        String Ques = "";
                        if (def != null)
                        {
                            Ques = ((DateTime)acert.PeriodEndDate).AddDays(1).ToShortDateString()
                                   + " - "
                                   + ((DateTime)acert.PeriodEndDate).AddDays(def.Duration).ToShortDateString();
                        }

                        if (!HideFromNavigation)
                        {
                            NavigateCloseDialog d = CreateQuestionDialogue(Msg, "Continue?", Ques);
                            if (d != null)
                            {
                                d.Closed += (s, err) =>
                                {
                                    CurrentEncounter.ReCertDiscipline = ((ChildWindow)s).DialogResult;
                                };
                                d.Show();
                            }
                        }
                    }
                }
            }

            WarnPOCorTMReferredButNotAssessed(acert);

            if (!CurrentForm.IsPlanOfCare && !CurrentForm.IsTransfer && !CurrentForm.IsTeamMeeting &&
                !CurrentForm.IsOrderEntry && !CurrentForm.IsAttempted && !CurrentForm.IsHospiceF2F &&
                !CurrentForm.IsVerbalCOTI && !CurrentForm.IsCOTI
                && CurrentEncounter.ServiceTypeKey != null)
            {
                // Get the Discipline Counts
                ServiceType st = null;
                if (CurrentEncounter.ServiceTypeKey.HasValue)
                {
                    st = ServiceTypeCache.GetServiceTypeFromKey((int)CurrentEncounter.ServiceTypeKey);
                }

                var DiscCount = CurrentAdmission.Encounter
                    //Exclude Encounters that are place holders for Tasks that haven't been started yet and exclude this encounter
                    .Where(e => e.EncounterStatus != (int)EncounterStatusType.None &&
                                e.EncounterKey != CurrentEncounter.EncounterKey)
                    .Where(p => !p.Inactive && p.ServiceType != null && p.ServiceType.Discipline != null &&
                                p.ServiceType.Discipline.HCFACode != null
                                && !p.ServiceType.NonBillable
                                && p.ServiceType.Discipline.HCFACode.Equals(
                                    ServiceTypeCache.GetHCFACodeFromKey(CurrentEncounter.ServiceTypeKey.Value)))
                    .Count();

                if (st != null && !st.NonBillable)
                {
                    CurrentEncounter.DisciplineServiceNumber = DiscCount + 1;
                }

                // Get the Therapy Counts
                if (st != null && st.Discipline != null && st.Discipline.IsTherapyDiscipline)
                {
                    var _startDate = CurrentEncounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date;
                    var certPeriod = CurrentAdmission.GetAdmissionCertForDate(_startDate);

                    var TherapyCount = CurrentAdmission.Encounter
                        //Exclude Encounters that are place holders for Tasks that haven't been started yet and exclude this encounter
                        .Where(e => e.EncounterStatus != (int)EncounterStatusType.None &&
                                    e.EncounterKey != CurrentEncounter.EncounterKey)
                        .Where(p => !p.Inactive && p.ServiceType != null && p.ServiceType.Discipline != null &&
                                    p.ServiceType.Discipline.HCFACode != null
                                    && certPeriod != null
                                    && _startDate <= certPeriod.PeriodEndDate
                                    && _startDate >= certPeriod.PeriodStartDate
                                    && p.ServiceType.Discipline.IsTherapyDiscipline && !p.ServiceType.NonBillable &&
                                    p.ServiceType.CountForTherapy).Count();

                    if (st.CountForTherapy)
                    {
                        CurrentEncounter.TherapyServiceNumber = TherapyCount + 1;
                    }
                }
            }

            if (CurrentForm.IsPlanOfCare)
            {
                EncounterPlanOfCare epc = new EncounterPlanOfCare();
                // Get most recent EncounterResumption and set EncounterPlanOfCare.VerbalResumptionDate and EncounterPlanOfCare.ResumptionDate
                EncounterResumption er = GetMostRecentResumption();
                if ((er != null) && (CurrentAdmission.HospiceAdmission))
                {
                    epc.VerbalResumptionDate = er.VerbalResumptionDate;
                    epc.ResumptionDate = er.ResumptionDate;
                }

                SetPoCCertificationDates(epc);
                SetPhysicianNarrative(epc);
                epc.UpdatedBy = WebContext.Current.User.MemberID;

                CurrentEncounter.EncounterPlanOfCare.Add(epc);

                CopyForwardADFandSuppliesForPOC(epc, false);
                CopyForwardLimitations(epc, false);
            }

            if (CurrentForm.IsTransfer)
            {
                EncounterTransfer et = new EncounterTransfer();
                CurrentAdmission.EncounterTransfer.Add(et);
                CurrentEncounter.EncounterTransfer.Add(et);
            }

            if (CurrentForm.IsTeamMeeting)
            {
                EncounterTeamMeeting eTM = new EncounterTeamMeeting();
                CurrentEncounter.EncounterTeamMeeting.Add(eTM);
            }

            CurrentPatient.Encounter.Add(CurrentEncounter);
            CurrentAdmission.Encounter.Add(CurrentEncounter);
            CopyForward(false);
        }

        private void WarnPOCorTMReferredButNotAssessed(AdmissionCertification acert)
        {
            // Warn the POC creator if not all active discipline evaluations have been completed
            if ((CurrentForm.IsPlanOfCare == false) && (CurrentForm.IsTeamMeeting == false))
            {
                return;
            }

            if (HideFromNavigation)
            {
                return;
            }

            // switch acert to the next period for the remainder of the checks if we are in the recert window.
            AdmissionCertification Nextacert = (CurrentEncounter.EncounterIsInRecertWindow)
                ? Nextacert = CurrentAdmission.GetAdmissionCertForDate(acert.PeriodStartDate.Value.AddDays(1))
                : acert;
            List<AdmissionDiscipline> discNotComplete = new List<AdmissionDiscipline>();
            if (CurrentAdmission.ActiveAdmissionDisciplines == null)
            {
                return;
            }

            foreach (AdmissionDiscipline ad in CurrentAdmission.ActiveAdmissionDisciplines)
            {
                if (DisciplineCache.GetDisciplineFromKey(ad.DisciplineKey).EvalServiceTypeOptional)
                {
                    continue;
                }

                // If the Admission Discipline is only referred before or during the current cert period - it qualifies 
                if (ad.AdmissionStatusCode == "R")
                {
                    if (((ad.ReferDate == null) || (Nextacert == null) || (Nextacert.PeriodEndDate == null)) ||
                        (((DateTime)ad.ReferDate).Date <= ((DateTime)Nextacert.PeriodEndDate).Date))
                    {
                        discNotComplete.Add(ad);
                    }

                    continue;
                }

                //Admission Discipline is in the process of being admitted or is admitted - find out
                Encounter inProcessEvalOrResumption = ad.Encounter.Where(p =>
                    p.Task != null && p.Task.CanceledBy == null && p.Task.TaskEndDateTime == null &&
                    ((p.EncounterIsEval || p.EncounterIsResumption)) &&
                    (p.EncounterStatus != (int)EncounterStatusType.None) && (p.IsCompleted == false)).FirstOrDefault();
                if (inProcessEvalOrResumption != null)
                {
                    if (Nextacert == null)
                    {
                        discNotComplete.Add(ad); // No cert row, so assume one big cert period 
                    }
                    else
                    {
                        // Only look at evals and resumptions started before or during the current cert period
                        AdmissionCertification ac = CurrentAdmission.GetAdmissionCertForDate(inProcessEvalOrResumption
                            .EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime);
                        if (((ac == null) || (ac.PeriodStartDate == null) || (Nextacert.PeriodStartDate == null)) ||
                            (ac.PeriodStartDate <= Nextacert.PeriodStartDate))
                        {
                            discNotComplete.Add(ad);
                        }
                    }
                }
            }

            ShowDisciplinesNotCompleteMessage(discNotComplete, Nextacert);
        }

        private void WarnDischargeReferredButNotAssessed()
        {
            // Warn the Discharge creator if not all active discipline evaluations have been completed
            // Only applicable to HomeHealth Agency Discharges using the new ReasonForDischarge question
            if ((CurrentAdmission == null) || (CurrentAdmissionDiscipline == null) || (CurrentForm == null) ||
                (CurrentEncounter == null))
            {
                return;
            }

            if ((CurrentEncounter.EncounterStatus != (int)EncounterStatusType.None) &&
                (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Edit))
            {
                return;
            }

            if (CurrentForm.IsDischarge == false)
            {
                return;
            }

            if (CurrentAdmission.HospiceAdmission)
            {
                return;
            }

            if (HideFromNavigation)
            {
                return;
            }

            if (CurrentAdmission.ActiveAdmissionDisciplines == null)
            {
                return;
            }

            if (CurrentForm.FormContainsQuestion(
                    DynamicFormCache.GetSingleQuestionByDataTemplate("ReasonForDischarge")) == false)
            {
                return;
            }

            if (CurrentAdmission.CalculateAgencyDischarge(CurrentAdmissionDiscipline, false) == false)
            {
                if (CurrentOasisManager != null)
                {
                    CurrentOasisManager.ForceBypassDischarge();
                }

                return;
            }

            List<AdmissionDiscipline> discNotComplete = CurrentAdmission.ActiveAdmissionDisciplines
                .Where(nc =>
                    ((DisciplineCache.GetDisciplineFromKey(nc.DisciplineKey).EvalServiceTypeOptional == false) &&
                     (nc.AdmissionDisciplineKey != CurrentAdmissionDiscipline.AdmissionDisciplineKey) &&
                     (nc.AdmissionStatusCode ==
                      "R"))) //NOTE: exclude Hospice Physician Services which is admitted without an eval on file and exclude ourselves                      
                .ToList();
            ShowDisciplinesNotCompleteMessage(discNotComplete, null);
        }

        private void ShowDisciplinesNotCompleteMessage(List<AdmissionDiscipline> discNotComplete,
            AdmissionCertification Nextacert)
        {
            if ((discNotComplete == null) || (discNotComplete.Any() == false))
            {
                return;
            }

            String Msg = "";
            String missingDisc = "";
            string tDate, tUser;
            string richText = string.Empty;

            richText = string.Format("\t{0} {1} ", CurrentEncounter.EncounterDateTime.ToShortDateString(),
                GetPlainTextFromHtml(CurrentEncounter.ServiceTypeDescription));

            string formDescription = "Encounter";
            if (CurrentForm.IsPlanOfCare)
            {
                formDescription = "Certification Plan of Care";
            }
            else if (CurrentForm.IsTeamMeeting)
            {
                formDescription = "Team Meeting";
            }
            else if (CurrentForm.IsDischarge)
            {
                formDescription = "Discharge";
            }

            Msg = "\t\tThe following disciplines have been referred but not yet assessed for admission:\n\n";
            bool areAssessmentsStarted = false;
            foreach (var missing in discNotComplete)
            {
                Encounter e = missing.Encounter.Where(p =>
                    p.Task != null && p.Task.CanceledBy == null && p.Task.TaskEndDateTime == null &&
                    ((p.EncounterIsEval || p.EncounterIsResumption)) &&
                    (p.EncounterStatus != (int)EncounterStatusType.None) && (p.IsCompleted == false)).FirstOrDefault();
                Task eTask = (e == null) ? null : e.Task;
                if (eTask == null)
                {
                    eTask = GetUnstartedTaskForDiscipline(missing.DisciplineKey, Nextacert);
                }

                DateTime? date = EncounterOrTaskDate(e, eTask);
                tDate = GetPlainTextFromHtml(FormatDate(date));
                tUser = (eTask == null)
                    ? "???"
                    : GetPlainTextFromHtml(UserCache.Current.GetFullNameWithSuffixFromUserId(eTask.UserID));
                if ((e == null) || ((e != null) && (e.EncounterStatus == (int)EncounterStatusType.None)))
                {
                    missingDisc = string.Format("\t\t\t\t <Bold> {0} </Bold> - Assessment scheduled for {1} by {2}\n",
                        GetPlainTextFromHtml(DisciplineCache.GetDescriptionFromKey(missing.DisciplineKey)), tDate,
                        tUser);
                }
                else
                {
                    missingDisc = string.Format("\t\t\t\t <Bold> {0} </Bold> - Assessment {1} for {2} by {3}\n",
                        GetPlainTextFromHtml(DisciplineCache.GetDescriptionFromKey(missing.DisciplineKey)),
                        e.EncounterStatusDescriptionRichText, tDate, tUser);
                    if (IsSkilledTask(e, eTask))
                    {
                        areAssessmentsStarted = true;
                    }
                }

                if (IsSkilledTaskScheduledOnOrBeforeToday(e, eTask))
                {
                    areAssessmentsStarted = true;
                }

                Msg += missingDisc;
            }

            string question = "Proceed?";
            if ((CurrentForm.IsPlanOfCare) || (CurrentForm.IsTeamMeeting))
            {
                question = "Document the " + formDescription + " without these discipline assessments completed?\n";
            }
            else if (CurrentForm.IsDischarge)
            {
                CurrentAdmissionDiscipline.OverrideAgencyDischarge = false;
                if (areAssessmentsStarted)
                {
                    question = "Proceed with " + GetPlainTextFromHtml(CurrentEncounter.ServiceTypeDescription) + "?";
                    // back off the agency discharge 
                    CurrentAdmissionDiscipline.AgencyDischarge = false;
                    CurrentAdmissionDiscipline.OverrideAgencyDischarge = true;
                }
                else
                {
                    question =
                        "<Bold><Run Foreground =\"Red\">Completing this Discharge Summary will mark the disciplines not yet admitted as Not Taken and the admission will be discharged.  Proceed with agency discharge?</Run></Bold>";
                }
            }

            if ((CurrentOasisManager != null) && (CurrentForm.IsDischarge))
            {
                if (CurrentAdmissionDiscipline.AgencyDischarge == false)
                {
                    CurrentOasisManager.ForceBypassDischarge();
                }
                else
                {
                    CurrentOasisManager.ForceUnBypassDischarge();
                }
            }

            NavigateCloseDialogWithRich d =
                CreateQuestionDialogueWithRich(Msg, "Continue?", question, richText, formDescription + "\n");
            if (d == null)
            {
                return;
            }

            d.Closed += (s, err) =>
            {
                if (((ChildWindow)s).DialogResult == false)
                {
                    //Note: need to do this so that when the NavigateBack() functionality calls back into our VM, it will be in a state to be removed and it's Cleanup() method executed.
                    FormModel.RejectMultiChanges();
                    NavigateBack();
                }
            };
            d.Show();
        }

        private string GetPlainTextFromHtml(string HTMLText)
        {
            if (string.IsNullOrWhiteSpace(HTMLText))
            {
                return "";
            }

            // remove HTML tokens
            string retText = HTMLText.Replace("<", "");
            retText = retText.Replace(">", "");
            retText = retText.Replace(";", "");
            retText = retText.Replace("\"", "");
            retText = retText.Replace("&", "");
            retText = retText.Replace("=", "");
            retText = retText.Replace("'", "&apos;");
            return retText;
        }

        private static string HtmlDecode(string text)
        {
            throw new NotImplementedException();
        }

        private DateTime? EncounterOrTaskDate(Encounter e, Task t)
        {
            if ((e != null) && (e.EncounterOrTaskStartDateAndTime.HasValue))
            {
                return ((DateTimeOffset)e.EncounterOrTaskStartDateAndTime).DateTime;
            }

            if (t != null)
            {
                return t.TaskStartDateTime.DateTime;
            }

            return null;
        }

        private string FormatDate(DateTime? date)
        {
            if ((date == null) || (date == DateTime.MinValue))
            {
                return "???";
            }

            bool useMilitaryTime = TenantSettingsCache.Current.TenantSetting.UseMilitaryTime;
            string stringDate = date.GetValueOrDefault().Date.ToString("MM/dd/yyyy") + " " +
                                ((useMilitaryTime)
                                    ? date.GetValueOrDefault().ToString("HHmm")
                                    : date.GetValueOrDefault().ToShortTimeString());
            return stringDate;
        }

        private bool IsSkilledTask(Encounter e, Task t)
        {
            int? serviceTypeKey = ((e != null) && (e.ServiceTypeKey != null))
                ? e.ServiceTypeKey
                : (((t != null) && (t.ServiceTypeKey != null)) ? t.ServiceTypeKey : null);
            if (serviceTypeKey == null)
            {
                return false;
            }

            int? dKey = ServiceTypeCache.GetDisciplineKey((int)serviceTypeKey);
            if (dKey == null)
            {
                return false;
            }

            Discipline d = DisciplineCache.GetDisciplineFromKey((int)dKey);
            if ((d == null) || (d.DisciplineIsSkilled == false))
            {
                return false;
            }

            return true;
        }

        private bool IsSkilledTaskScheduledOnOrBeforeToday(Encounter e, Task t)
        {
            // bail out if task is scheduled after today
            DateTime? date = EncounterOrTaskDate(e, t);
            if ((date == null) || (((DateTime)date).Date > DateTime.Today.Date))
            {
                return false;
            }

            return IsSkilledTask(e, t);
        }

        private Task GetUnstartedTaskForDiscipline(int? disciplineKey, AdmissionCertification nextacert)
        {
            //A task does not exist with an in-process eval or resumption encounter hanging off it - see if an unstarted task exists
            //Assume no cert row, so assume one big cert period and look for at least one eval task for the discipline.
            if ((CurrentAdmission == null) || (CurrentAdmission.Task == null) || (CurrentAdmission.Encounter == null))
            {
                return null;
            }

            List<Task> tList =
                CurrentAdmission.Task
                    .Where(t => ((t.TaskEndDateTime == null) && (t.TaskIsEval || t.TaskIsResumption) &&
                                 (t.ServiceTypeKey != null) &&
                                 (t.ServiceTypeDisciplineKey == disciplineKey) && (t.CanceledAt == null) &&
                                 (CurrentAdmission.Encounter
                                     .Where(e => e.EncounterStatus !=
                                                 (int)EncounterStatusType
                                                     .None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                                     .Where(e => e.TaskKey == t.TaskKey).Any() == false)))
                    .ToList();
            if ((nextacert != null) && (tList != null) && (CurrentAdmission != null))
            {
                // Only look at eval tasks for the cert cycle that applies to this plan of care.
                tList = tList.Where(t =>
                    (CurrentAdmission.GetAdmissionCertForDate(t.TaskStartDateTime.DateTime).AdmissionCertKey ==
                     nextacert.AdmissionCertKey)).ToList();
            }

            return (tList == null) ? null : tList.FirstOrDefault();
        }

        private void CopyForwardADFandSuppliesForPOC(EncounterPlanOfCare epc, bool deleteExisting)
        {
            if (deleteExisting)
            {
                if ((CurrentEncounter != null)
                    && (CurrentEncounter.EncounterVisitFrequency != null)
                   )
                {
                    while (CurrentEncounter.EncounterVisitFrequency.Any())
                    {
                        EncounterVisitFrequency encVF = CurrentEncounter.EncounterVisitFrequency.First();
                        FormModel.Remove(encVF);
                        CurrentEncounter.EncounterVisitFrequency.Remove(encVF);
                    }
                }

                if ((CurrentEncounter != null)
                    && (CurrentEncounter.EncounterSupply != null)
                   )
                {
                    while (CurrentEncounter.EncounterSupply.Any())
                    {
                        EncounterSupply encSup = CurrentEncounter.EncounterSupply.First();
                        FormModel.Remove(encSup);
                        CurrentEncounter.EncounterSupply.Remove(encSup);
                    }
                }
            }

            // Copy forward visit frequencies from most recent encounter that contains them
            foreach (var item in CurrentAdmission.Encounter
                         .Where(e => e.EncounterStatus !=
                                     (int)EncounterStatusType
                                         .None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime)
                         .Where(enc => enc.ReCertDiscipline == true || enc.ReCertDiscipline == null))
                if (item.EncounterVisitFrequency.Any())
                {
                    foreach (var vf in item.EncounterVisitFrequency)
                        if (vf != null)
                        {
                            EncounterVisitFrequency evf = new EncounterVisitFrequency();
                            evf.Frequency = vf.Frequency;
                            evf.Duration = vf.Duration;
                            evf.Purpose = vf.Purpose;
                            CurrentEncounter.EncounterVisitFrequency.Add(evf);
                        }

                    break;
                }

            CopyForwardEncounterSupplies();
        }

        private void CopyForwardEncounterSupplies()
        {
            // Copy forward distinct set of supplies across ALLL encounter - regardless of discipline or cert cycle
            foreach (Encounter e in CurrentAdmission.Encounter
                         .Where(e => e.EncounterStatus != (int)EncounterStatusType.None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                         .Where(p => p.EncounterIsPlanOfCare == false)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                if (e.EncounterSupply.Any())
                {
                    foreach (var sup in e.EncounterSupply)
                        if ((sup != null) &&
                            (CurrentEncounter.EncounterSupply.Where(s => s.SupplyKey == sup.SupplyKey).Any() == false))
                        {
                            EncounterSupply newSup = new EncounterSupply();
                            newSup.InsuranceKey = sup.InsuranceKey;
                            newSup.LocationKey = sup.LocationKey;
                            newSup.LotNumber = sup.LotNumber;
                            newSup.OverrideAllow = sup.OverrideAllow;
                            newSup.OverrideChg = sup.OverrideChg;
                            newSup.SequenceNo = sup.SequenceNo;
                            newSup.SupplyAllow = sup.SupplyAllow;
                            newSup.SupplyCharge = sup.SupplyCharge;
                            newSup.SupplyKey = sup.SupplyKey;
                            newSup.SupplyQty = sup.SupplyQty;
                            newSup.SupplyUnitsKey = sup.SupplyUnitsKey;
                            CurrentEncounter.EncounterSupply.Add(newSup);
                        }
                }
            }
        }

        private void CopyForwardLimitations(EncounterPlanOfCare epc, bool deleteExisting)
        {
            List<string> safetyMeasures = new List<string>();
            List<string> funcDeficits = new List<string>();

            if (deleteExisting)
            {
                if (epc != null)
                {
                    epc.POCSafetyMeasures = null;
                    epc.POCLimitations = null;
                }
            }

            // Copy forward safety measure questions
            var safetyQ = DynamicFormCache.GetQuestionByLabelContains("Safety");
            foreach (var item in CurrentAdmission.Encounter
                         .Where(p => ((p.IsNew == false) && (p.EncounterIsPlanOfCare == false)))
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                var previousList =
                    item.EncounterData.Where(d => safetyQ.Where(sq => sq.QuestionKey == d.QuestionKey).Any());
                foreach (EncounterData previous in previousList)
                    if ((string.IsNullOrWhiteSpace(previous.TextData) == false) &&
                        (!safetyMeasures.Contains(previous.TextData)))
                    {
                        safetyMeasures.Add(previous.TextData);
                    }
            }

            foreach (Section s in DynamicFormCache.GetSections())
            {
                Encounter en = GetMostRecentEncounterForSection(s.SectionKey);
                if (en == null)
                {
                    continue;
                }

                if (en.EncounterData != null)
                {
                    // Append safety measures
                    foreach (Question eq in DynamicFormCache.GetEquipmentQuestions())
                    {
                        IEnumerable<EncounterData> edsList = en.EncounterData.Where(smed =>
                            smed.SectionKey == s.SectionKey && smed.QuestionKey == eq.QuestionKey);
                        foreach (EncounterData ed in edsList)
                            if (ed.IntData != null)
                            {
                                string smDescription = string.Format("{0} for {1}",
                                    EquipmentCache.GetDescriptionFromKey((int)ed.IntData),
                                    EquipmentCache.GetEquipmentDescriptionFromType(eq.LookupType));
                                if (!safetyMeasures.Contains(smDescription))
                                {
                                    safetyMeasures.Add(smDescription);
                                }
                            }
                    }

                    // Append function deficits
                    IEnumerable<EncounterData> edfList = en.EncounterData.Where(fed =>
                        fed.SectionKey == s.SectionKey && fed.FuncDeficit != null);
                    foreach (EncounterData ed in edfList)
                        if ((!funcDeficits.Contains(ed.FuncDeficit)) && (ed.FuncDeficit.ToLower() != "none"))
                        {
                            funcDeficits.Add(ed.FuncDeficit);
                        }
                }

                // Append gait function deficits
                if (en.EncounterGait != null)
                {
                    IEnumerable<EncounterGait> egList = en.EncounterGait.Where(feg =>
                        feg.SectionKey == s.SectionKey && feg.FuncDeficit != null);
                    foreach (EncounterGait eg in egList)
                        if ((!funcDeficits.Contains(eg.FuncDeficit)) && (eg.FuncDeficit.ToLower() != "none"))
                        {
                            funcDeficits.Add(eg.FuncDeficit);
                        }
                }
            }

            // Add pain functional deficits
            if (CurrentEncounter.EncounterPainLocation != null)
            {
                IEnumerable<EncounterPainLocation> eplList =
                    CurrentEncounter.EncounterPainLocation.Where(
                        ep => ep.AdmissionPainLocation.PainInterference != null);
                foreach (EncounterPainLocation epl in eplList)
                    if ((!funcDeficits.Contains(epl.AdmissionPainLocation.PainInterference)) &&
                        (epl.AdmissionPainLocation.PainInterference.ToLower() != "none"))
                    {
                        funcDeficits.Add(epl.AdmissionPainLocation.PainInterference);
                    }
            }

            if ((safetyMeasures != null) && safetyMeasures.Any())
            {
                safetyMeasures.Sort();
                foreach (string sm in safetyMeasures)
                    epc.POCSafetyMeasures = (epc.POCSafetyMeasures == null) ? sm : epc.POCSafetyMeasures + "|" + sm;
            }

            if ((funcDeficits != null) && funcDeficits.Any())
            {
                funcDeficits.Sort();
                foreach (string fd in funcDeficits)
                    epc.POCLimitations = (epc.POCLimitations == null) ? fd : epc.POCLimitations + "|" + fd;
            }
        }

        private void SetPoCCertificationDates(EncounterPlanOfCare epc)
        {
            AdmissionCertification ac = GetAdmissionCertForEncounter();

            if ((epc != null) && (ac != null))
            {
                epc.CertificationFromDate =
                    (ac.PeriodStartDate.HasValue ? ac.PeriodStartDate.Value.Date : (DateTime?)null);
                epc.CertificationThruDate = (ac.PeriodEndDate.HasValue ? ac.PeriodEndDate.Value.Date : (DateTime?)null);
            }
        }

        private void SetPhysicianNarrative(EncounterPlanOfCare epc)
        {
            if ((epc == null) || (CurrentEncounter == null))
            {
                return;
            }

            if (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
            {
                return;
            }

            if ((string.IsNullOrEmpty(epc.PhysicianSignatureNarrative) == false) &&
                (epc.PhysicianSignatureNarrative.Contains("{0}") == false) &&
                (epc.PhysicianSignatureNarrative.ToLower().Contains("(date)") == false))
            {
                return;
            }

            AdmissionCertification ac = GetAdmissionCertForEncounter();
            int periodNumber = ((ac == null) || (ac.PeriodNumber == 0)) ? 1 : ac.PeriodNumber;
            DateTime? periodStartDate = epc.PeriodStartDate;
            if ((periodStartDate == null) && (ac != null))
            {
                periodStartDate = ac.PeriodStartDate;
            }

            if (periodStartDate == null)
            {
                periodStartDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
            }

            string blirb = CurrentAdmission.AdmissionInsurancePOCCertStatement(periodNumber, periodStartDate);
            if (string.IsNullOrWhiteSpace(blirb))
            {
                blirb = (CurrentAdmission.HospiceAdmission)
                    ? "I certify/recertify that this patient is confined to his or her home and needs intermittent skilled nursing, physical therapy and/or speech therapy, or continues to need occupational therapy."
                    : GetDefaultHomeHealthStatement(periodNumber);
            }

            if ((blirb != null) && blirb.Contains("{0}"))
            {
                string F2FDate = ((CurrentAdmission == null) || (CurrentAdmission.FaceToFaceEncounterDate == null))
                    ? "(Date)"
                    : ((DateTime)CurrentAdmission.FaceToFaceEncounterDate).Date.ToShortDateString();
                blirb = blirb.Replace("{0}", F2FDate);
            }

            epc.PhysicianSignatureNarrative = blirb;
        }

        private string GetDefaultHomeHealthStatement(int periodNumber)
        {
            if (periodNumber <= 1)
            {
                return
                    "I certify this patient is confined to his/her home and needs intermittent skilled nursing care, physical therapy and/or speech therapy, or continues to need occupational therapy.  The patient is under my care, and I have authorized the services on this plan, and will periodically review the plan.  The patient had a face-to-face encounter with an allowed provider type on {0} and the encounter was related to the primary reason for home health care.";
            }

            return
                "I re-certify this patient is confined to his/her home and needs intermittent skilled nursing care, physical therapy and/or speech therapy, or continues to need occupational therapy for the period of this plan.  The patient is under my care, and I have authorized the services on this plan, and will periodically review the plan.";
        }

        private void SetupExistingEncounter()
        {
            CertManager.CreateCertIfNecessary(CurrentForm, CurrentAdmission, CurrentEncounter);
            if ((CurrentEncounter != null) && (CurrentForm != null) && CurrentForm.IsPlanOfCare &&
                (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Edit))
            {
                EncounterPlanOfCare epc = CurrentEncounter.EncounterPlanOfCare.FirstOrDefault();
                if (epc != null)
                {
                    SetPoCCertificationDates(epc);
                    SetPhysicianNarrative(epc);
                }
            }

            CopyForward(true);
        }

        private void SetupExistingEncounterRefreshDiagnosis()
        {
            if (EntityManager.IsOnline == false)
            {
                return;
            }

            if (DiagnosesHasChanged == false)
            {
                return;
            }

            if (IsReadOnlyEncounter)
            {
                return; // do not refresh diagnosis in a read-only encounter, just display for historical reasons
            }

            // Remove encounter diagnosis
            foreach (EncounterDiagnosis ed in CurrentEncounter.EncounterDiagnosis.Reverse())
                try
                {
                    CurrentEncounter.EncounterDiagnosis.Remove(ed);
                }
                catch
                {
                }

            // Refresh encounter diagnosis from current diagnosis
            var existingdiags = CurrentAdmission.AdmissionDiagnosis.Where(p => !p.Superceded);
            foreach (var diag in existingdiags)
            {
                EncounterDiagnosis ed2 = new EncounterDiagnosis();
                CurrentAdmission.EncounterDiagnosis.Add(ed2);
                CurrentEncounter.EncounterDiagnosis.Add(ed2);
                diag.EncounterDiagnosis.Add(ed2);
            }

            // let the user know
            if (!HideFromNavigation)
            {
                NavigateCloseDialog d = new NavigateCloseDialog
                {
                    Width = double.NaN,
                    Height = double.NaN,
                    NoVisible = false,
                    YesButton =
                    {
                        Content = "OK"
                    },
                    Title = "Refreshing Diagnosis(es)",
                    ErrorMessage = "The Diagnosis(es) have changed, refreshing from the admission.",
                    HasCloseButton = false
                };

                d.Show();
            }

            if (CurrentOasisManager != null)
            {
                CurrentOasisManager.SetupAdmissionDiagnosis(CurrentAdmission);
            }
        }

        private bool DiagnosesHasChanged
        {
            get
            {
                if (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return false;
                }

                if ((CurrentAdmission == null) || (CurrentAdmission.AdmissionDiagnosis == null))
                {
                    return false;
                }

                if ((CurrentEncounter == null) || (CurrentEncounter.EncounterDiagnosis == null))
                {
                    return false;
                }

                foreach (EncounterDiagnosis ed in CurrentEncounter.EncounterDiagnosis)
                {
                    AdmissionDiagnosis ad = CurrentAdmission.AdmissionDiagnosis
                        .Where(a => a.AdmissionDiagnosisKey == ed.DiagnosisKey).FirstOrDefault();
                    if (ad == null)
                    {
                        return true;
                    }

                    if (ad.Superceded)
                    {
                        return true;
                    }
                }

                List<AdmissionDiagnosis> currentDiags = CurrentAdmission.AdmissionDiagnosis
                    .Where(p => ((p.Superceded == false) && (p.AdmissionDiagnosisKey > 0))).ToList();
                foreach (AdmissionDiagnosis ad2 in currentDiags)
                    if (CurrentEncounter.EncounterDiagnosis.Where(e => e.DiagnosisKey == ad2.AdmissionDiagnosisKey)
                            .FirstOrDefault() == null)
                    {
                        return true;
                    }

                return false;
            }
        }

        private void PostEncounterSetup()
        {
            CurrentEncounter.SetupPatientAddressCollectionView(CurrentPatient.PatientAddress);
            CurrentEncounter.FilterPatientAddressCollectionView();
        }

        private bool HasInitialDataChanged()
        {
            bool changed = false;

            var existingallergies = CurrentPatient.PatientAllergy.Where(p => !p.Superceded && (!p.Inactive));
            if (existingallergies.Any(existing =>
                    !(CurrentEncounter.EncounterAllergy.Select(ea => ea.AllergyKey)
                        .Contains(existing.PatientAllergyKey))))
            {
                changed = true;
            }

            if (!changed)
            {
                var existingdiags = CurrentAdmission.AdmissionDiagnosis.Where(p => !p.Superceded);
                if (existingdiags.Any(existing =>
                        !(CurrentEncounter.EncounterDiagnosis.Select(ea => ea.DiagnosisKey)
                            .Contains(existing.AdmissionDiagnosisKey))))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var existingdiagComments = CurrentPatient.PatientDiagnosisComment;
                if (existingdiagComments.Any(existing =>
                        !(CurrentEncounter.EncounterDiagnosisComment.Select(ea => ea.PatientDiagnosisCommentKey)
                            .Contains(existing.PatientDiagnosisCommentKey))))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var existingmeds = CurrentPatient.PatientMedication.Where(p => !p.Superceded);
                if (CurrentForm.IsPlanOfCare)
                {
                    var ec = GetAdmissionCertForEncounter();
                    if (ec != null)
                    {
                        existingmeds = existingmeds.Where(p =>
                            (p.Superceded == false) &&
                            (p.IsPOCMedication(ec.PeriodStartDate, ec.PeriodEndDate, CurrentEncounter)));
                    }
                }

                if (existingmeds.Any(existing =>
                        !(CurrentEncounter.EncounterMedication.Select(ea => ea.MedicationKey)
                            .Contains(existing.PatientMedicationKey))))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var existinglocs = CurrentAdmission.AdmissionLevelOfCare.Where(p => !p.Superceded);
                if (existinglocs.Any(existing =>
                        !(CurrentEncounter.EncounterLevelOfCare.Select(ea => ea.AdmissionLevelOfCareKey)
                            .Contains(existing.AdmissionLevelOfCareKey))))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var existingpains = CurrentAdmission.AdmissionPainLocation.Where(p => !p.Superceded);
                if (existingpains.Any(existing =>
                        !(CurrentEncounter.EncounterPainLocation.Select(ea => ea.AdmissionPainLocationKey)
                            .Contains(existing.AdmissionPainLocationKey))))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var existingivs =
                    CurrentAdmission.AdmissionIVSite.Where(p => ((!p.Superceded) && (p.DeletedDate == null)));
                if (existingivs.Any(existing =>
                        !(CurrentEncounter.EncounterIVSite.Select(ea => ea.AdmissionIVSiteKey)
                            .Contains(existing.AdmissionIVSiteKey))))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var existingwounds = CurrentAdmission.AdmissionWoundSite.Where(p => !p.Superceded);
                if (existingwounds.Any(existing =>
                        !(CurrentEncounter.EncounterWoundSite.Select(ea => ea.AdmissionWoundSiteKey)
                            .Contains(existing.AdmissionWoundSiteKey))))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var existingInfections2 = CurrentPatient.PatientInfection.Where(p => !p.Superceded);
                if (existingInfections2.Any(existing =>
                        !(CurrentEncounter.EncounterPatientInfection.Select(ea => ea.PatientInfectionKey)
                            .Contains(existing.PatientInfectionKey))))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var existingAdverseEvent = CurrentPatient.PatientAdverseEvent.Where(p => !p.Superceded);
                if (existingAdverseEvent.Any(existing =>
                        !(CurrentEncounter.EncounterPatientAdverseEvent.Select(ea => ea.PatientAdverseEventKey)
                            .Contains(existing.PatientAdverseEventKey))))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var existingInfections = CurrentAdmission.AdmissionInfection.Where(p => !p.Superceded);
                if (existingInfections.Any(existing =>
                        !(CurrentEncounter.EncounterInfection.Select(ea => ea.AdmissionInfectionKey)
                            .Contains(existing.AdmissionInfectionKey))))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var existingLabs = CurrentPatient.PatientLab.Where(p => !p.Superceded);
                if (existingLabs.Any(existing =>
                        !(CurrentEncounter.EncounterLab.Select(ea => ea.PatientLabKey)
                            .Contains(existing.PatientLabKey))))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var existinggoals = CurrentAdmission.AdmissionGoal.Where(p =>
                    !p.Superceded && !p.Resolved && !p.Unattainable && !p.Discontinued && !p.Inactivated);
                if (CurrentForm != null && CurrentForm.IsPlanOfCare)
                {
                    existinggoals = CurrentAdmission.AdmissionGoal.Where(p =>
                        !p.Superceded && !p.Resolved && !p.Unattainable && !p.Discontinued && !p.Inactivated
                        && p.AdmissionGoalElement != null && p.AdmissionGoalElement.Any(ge => ge.IncludeonPOC));
                }

                if (existinggoals.Any(existing =>
                        !(CurrentEncounter.EncounterGoal.Select(ea => ea.AdmissionGoalKey)
                            .Contains(existing.AdmissionGoalKey))))
                {
                    changed = true;
                }

                if (!changed)
                {
                    List<int> existingelms = new List<int>();
                    existinggoals.ForEach(goal => existingelms.AddRange(goal.AdmissionGoalElement.Where(age =>
                        !age.Superceded && !age.Resolved && !age.Unattainable && !age.Discontinued && !age.Inactivated
                        && ((age.GoalElement.Orders && CurrentForm.IsPlanOfCare)
                            || ((!age.GoalElement.Orders) && (!CurrentForm.IsPlanOfCare))
                        )
                    ).Select(elm => elm.AdmissionGoalElementKey)));
                    if (existingelms.Any(existing => !(CurrentEncounter.EncounterGoalElement.Select(
                            ea => ea.AdmissionGoalElementKey
                        ).Contains(existing))))
                    {
                        changed = true;
                    }
                }
            }

            // rows from incomplete encounters are added back in the new visit frequency control.  MaintenanceUserControls.cs -> VisitFrequencyUserControlBase
            if (!changed)
            {
                var existingadf = CurrentAdmission.AdmissionDisciplineFrequency.Where(p => !p.Superceded && !p.Inactive
                    // Only include FCD's from encounters that have been signed.  FCD's from non signed encounters may be invalid.
                    && (p.AddedFromEncounterKey == null
                        || (p.AddedFromEncounterKey != null
                            && p.Encounter != null
                            && p.Encounter.EncounterStatus == (int)EncounterStatusType.Completed
                        )
                    )
                );
                if (existingadf.Any(existing =>
                        !(CurrentEncounter.EncounterDisciplineFrequency.Select(ea => ea.DispFreqKey)
                            .Contains(existing.DisciplineFrequencyKey))))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var existingac = CurrentAdmission.AdmissionConsent.Where(p => !p.Superceded && !p.Inactive);
                if (existingac.Any(existing =>
                        !(CurrentEncounter.EncounterConsent.Select(ea => ea.AdmissionConsentKey)
                            .Contains(existing.AdmissionConsentKey))))
                {
                    changed = true;
                }
            }

            if (CurrentForm.IsPlanOfCare)
            {
                if (!changed)
                {
                    var existingEquip =
                        CurrentAdmission.AdmissionEquipment.Where(e => (!e.Superceded) && (!e.Inactive));
                    if (existingEquip.Any(existing =>
                            !(CurrentEncounter.EncounterEquipment.Select(ea => ea.AdmissionEquipmentKey)
                                .Contains(existing.AdmissionEquipmentKey))))
                    {
                        changed = true;
                    }
                }
            }

            if (CurrentForm.IsPlanOfCare)
            {
                if (!changed)
                {
                    foreach (Encounter e in CurrentAdmission.Encounter
                                 .Where(e => e.EncounterStatus !=
                                             (int)EncounterStatusType
                                                 .None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                                 .Where(p => p.EncounterIsPlanOfCare == false)
                                 .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                        if (e.EncounterSupply.Any())
                        {
                            foreach (var sup in e.EncounterSupply)
                                if ((sup != null) && (CurrentEncounter.EncounterSupply
                                        .Where(s => s.SupplyKey == sup.SupplyKey).Any() == false))
                                {
                                    changed = true;
                                    break; // break out of the loop; no need to keep looking.
                                }

                            if (changed)
                            {
                                break; // break out of the outer loop too;  No need to keep looking.
                            }
                        }
                }
            }
            return changed;
        }

        private void CopyForward(bool forUpdate)
        {
            //Create OrderEntry manager if need be (needed by CopyForward)
            CurrentOrderEntryManager = OrderEntryManager.Create(this);
            if (forUpdate)
            {
                if (OkToRefreshEncounterData())
                {
                    foreach (EncounterAllergy ea in CurrentEncounter.EncounterAllergy) FormModel.Remove(ea);
                    foreach (EncounterDiagnosis ed in CurrentEncounter.EncounterDiagnosis) FormModel.Remove(ed);
                    foreach (EncounterDiagnosisComment edc in CurrentEncounter.EncounterDiagnosisComment) FormModel.Remove(edc);
                    foreach (EncounterLevelOfCare eloc in CurrentEncounter.EncounterLevelOfCare) FormModel.Remove(eloc);
                    foreach (EncounterPainLocation em in CurrentEncounter.EncounterPainLocation) FormModel.Remove(em);
                    foreach (EncounterMedication em in CurrentEncounter.EncounterMedication) FormModel.Remove(em);
                    foreach (EncounterIVSite em in CurrentEncounter.EncounterIVSite) FormModel.Remove(em);
                    foreach (EncounterWoundSite em in CurrentEncounter.EncounterWoundSite) FormModel.Remove(em);
                    foreach (EncounterLab em in CurrentEncounter.EncounterLab) FormModel.Remove(em);
                    foreach (EncounterGoalElement em in CurrentEncounter.EncounterGoalElement) FormModel.Remove(em);
                    foreach (EncounterGoal em in CurrentEncounter.EncounterGoal) FormModel.Remove(em);
                    foreach (EncounterDisciplineFrequency em in CurrentEncounter.EncounterDisciplineFrequency) FormModel.Remove(em);
                    foreach (EncounterStartDisciplineFrequency em in CurrentEncounter.EncounterStartDisciplineFrequency) FormModel.Remove(em);
                    foreach (EncounterConsent em in CurrentEncounter.EncounterConsent) FormModel.Remove(em);
                    foreach (EncounterEquipment em in CurrentEncounter.EncounterEquipment) FormModel.Remove(em);
                    foreach (EncounterSupply es in CurrentEncounter.EncounterSupply) FormModel.Remove(es);
                }
                else
                {
                    return;
                }
            }

            // Copy forward patient and admission data (icds, meds, wounds, etc...) from 'current' start data
            var existingallergies = CurrentPatient.PatientAllergy.Where(p => !p.Superceded && (!p.Inactive));
            foreach (var allergy in existingallergies)
            {
                EncounterAllergy ea = new EncounterAllergy();
                FormModel.GetContext().EntityContainer.GetEntitySet<EncounterAllergy>().Add(ea);
                CurrentPatient.EncounterAllergy.Add(ea);
                CurrentEncounter.EncounterAllergy.Add(ea);
                allergy.EncounterAllergy.Add(ea);
            }

            var existingdiags = CurrentAdmission.AdmissionDiagnosis.Where(p => !p.Superceded);
            foreach (var diag in existingdiags)
            {
                EncounterDiagnosis ed = new EncounterDiagnosis();
                FormModel.GetContext().EntityContainer.GetEntitySet<EncounterDiagnosis>().Add(ed);
                CurrentAdmission.EncounterDiagnosis.Add(ed);
                CurrentEncounter.EncounterDiagnosis.Add(ed);
                diag.EncounterDiagnosis.Add(ed);
            }

            var existingdiagComments = CurrentPatient.PatientDiagnosisComment;
            foreach (PatientDiagnosisComment diagComment in existingdiagComments)
            {
                EncounterDiagnosisComment edc = new EncounterDiagnosisComment();
                FormModel.GetContext().EntityContainer.GetEntitySet<EncounterDiagnosisComment>().Add(edc);
                CurrentPatient.EncounterDiagnosisComment.Add(edc);
                CurrentEncounter.EncounterDiagnosisComment.Add(edc);
                diagComment.EncounterDiagnosisComment.Add(edc);
            }

            var existingmeds = CurrentPatient.PatientMedication.Where(p => !p.Superceded);
            //exclude Meds on the POC that don't start on or after the cert period start date.
            if (CurrentForm.IsPlanOfCare)
            {
                var ec = GetAdmissionCertForEncounter();
                if (ec != null)
                {
                    existingmeds = existingmeds.Where(p =>
                        (p.Superceded == false) &&
                        (p.IsPOCMedication(ec.PeriodStartDate, ec.PeriodEndDate, CurrentEncounter)));
                }
            }

            foreach (var med in existingmeds)
            {
                EncounterMedication em = new EncounterMedication();
                FormModel.GetContext().EntityContainer.GetEntitySet<EncounterMedication>().Add(em);
                CurrentPatient.EncounterMedication.Add(em);
                CurrentEncounter.EncounterMedication.Add(em);
                med.EncounterMedication.Add(em);
                if (CurrentOrderEntryManager != null)
                {
                    EncounterStartMedication esm = new EncounterStartMedication
                        { MedicationEndDate = med.MedicationEndDateTime };
                    FormModel.GetContext().EntityContainer.GetEntitySet<EncounterStartMedication>().Add(esm);
                    CurrentPatient.EncounterStartMedication.Add(esm);
                    CurrentEncounter.EncounterStartMedication.Add(esm);
                    med.EncounterStartMedication.Add(esm);
                }
            }

            var existinglocs = CurrentAdmission.AdmissionLevelOfCare.Where(p => !p.Superceded);
            foreach (var pain in existinglocs)
            {
                EncounterLevelOfCare el = new EncounterLevelOfCare();
                CurrentEncounter.EncounterLevelOfCare.Add(el);
                pain.EncounterLevelOfCare.Add(el);
            }

            var existingpains = CurrentAdmission.AdmissionPainLocation.Where(p => !p.Superceded);
            foreach (var pain in existingpains)
            {
                EncounterPainLocation ep = new EncounterPainLocation();
                CurrentEncounter.EncounterPainLocation.Add(ep);
                pain.EncounterPainLocation.Add(ep);
            }

            var existingivs = CurrentAdmission.AdmissionIVSite.Where(p => ((!p.Superceded) && (p.DeletedDate == null)));
            foreach (var iv in existingivs)
            {
                EncounterIVSite ei = new EncounterIVSite();
                CurrentEncounter.EncounterIVSite.Add(ei);
                iv.EncounterIVSite.Add(ei);
            }

            CreateNewVersionWounds();
            var existingwounds = CurrentAdmission.AdmissionWoundSite.Where(p => !p.Superceded);
            foreach (var wound in existingwounds)
            {
                EncounterWoundSite ew = new EncounterWoundSite();
                CurrentEncounter.EncounterWoundSite.Add(ew);
                wound.EncounterWoundSite.Add(ew);
            }

            var existingInfections2 = CurrentPatient.PatientInfection.Where(p => !p.Superceded);
            foreach (var inf in existingInfections2)
            {
                EncounterPatientInfection ei = new EncounterPatientInfection();
                ei.PatientInfection = inf;
                CurrentEncounter.EncounterPatientInfection.Add(ei);
                inf.EncounterPatientInfection.Add(ei);
            }

            var existingInfections = CurrentAdmission.AdmissionInfection.Where(p => !p.Superceded);
            foreach (var inf in existingInfections)
            {
                EncounterInfection ei = new EncounterInfection();
                ei.AdmissionInfection = inf;
                CurrentEncounter.EncounterInfection.Add(ei);
                inf.EncounterInfection.Add(ei);
            }

            var existingPatientAdverseEvent = CurrentPatient.PatientAdverseEvent.Where(p => !p.Superceded);
            foreach (var pae in existingPatientAdverseEvent)
            {
                EncounterPatientAdverseEvent epae = new EncounterPatientAdverseEvent();
                epae.PatientAdverseEvent = pae;
                CurrentEncounter.EncounterPatientAdverseEvent.Add(epae);
                pae.EncounterPatientAdverseEvent.Add(epae);
            }

            var existingLabs = CurrentPatient.PatientLab.Where(p => !p.Superceded);
            foreach (var pl in existingLabs)
            {
                EncounterLab el = new EncounterLab();
                el.PatientLab = pl;
                CurrentEncounter.EncounterLab.Add(el);
                pl.EncounterLab.Add(el);
            }

            //need to show resolved AND discontinued AND unattainable AND inactivated Goals and Goal Elements on the POC - withing the OC cert range
            var existingGoals = CurrentAdmission.AdmissionGoal.Where(p =>
                !p.Superceded && 
                (!p.Resolved || CurrentForm.IsPlanOfCare) &&
                (!p.Discontinued || CurrentForm.IsPlanOfCare) && 
                (!p.Unattainable || CurrentForm.IsPlanOfCare) && 
                (!p.Inactivated || CurrentForm.IsPlanOfCare));

            if (CurrentForm.IsPlanOfCare)
            {
                //exclude goal and goal elements on the POC that don't require orders.
                if (!CurrentAdmission.HospiceAdmission)
                {
                    existingGoals =
                        existingGoals.Where(p => (p.HasIncludeonPOCGoalElements || p.RequiredForDischargePlan));
                }

                if ((CurrentEncounter != null) && (CurrentEncounter.EncounterPlanOfCare != null) &&
                    (CurrentEncounter.EncounterPlanOfCare.FirstOrDefault() != null))
                {
                    EncounterPlanOfCare ePOC = CurrentEncounter.EncounterPlanOfCare.FirstOrDefault();
                    DateTime certFromDate = (ePOC == null)
                        ? DateTime.Today.Date
                        : ((ePOC.CertificationFromDate == null)
                            ? DateTime.Today.Date
                            : ((DateTime)ePOC.CertificationFromDate).Date);
                    // Exclude Goal that have been resolved, discontinued, unattainable or inactivated prior to this POC sect
                    existingGoals = existingGoals.Where(p => p.ActiveAsOfDate(certFromDate)).ToList();
                }
            }

            foreach (var goal in existingGoals)
            {
                EncounterGoal eg = new EncounterGoal();
                FormModel.GetContext().EntityContainer.GetEntitySet<EncounterGoal>().Add(eg);
                CurrentEncounter.EncounterGoal.Add(eg);
                goal.EncounterGoal.Add(eg);

                //need to show resolved AND discontinued AND unattainable AND inactivatedGoals and Goal Elements on the POC
                var existingGoalElements = goal.AdmissionGoalElement.Where(p =>
                    !p.Superceded && 
                    (!p.Resolved || CurrentForm.IsPlanOfCare) &&
                    (!p.Discontinued || CurrentForm.IsPlanOfCare) && 
                    (!p.Unattainable || CurrentForm.IsPlanOfCare) &&
                    (!p.Inactivated || CurrentForm.IsPlanOfCare));

                foreach (var age in existingGoalElements)
                    //exclude goal and goal elements on the POC that don't require orders unless we are a Hospice Admission
                    if ((CurrentForm.IsPlanOfCare == false) ||
                        age.GoalElement.Orders ||
                        (CurrentForm.IsPlanOfCare && CurrentAdmission.HospiceAdmission) ||
                        (CurrentForm.IsPlanOfCare && goal.RequiredForDischargePlan))
                    {
                        EncounterGoalElement ege = new EncounterGoalElement();
                        FormModel.GetContext().EntityContainer.GetEntitySet<EncounterGoalElement>().Add(ege);
                        CurrentEncounter.EncounterGoalElement.Add(ege);
                        age.EncounterGoalElement.Add(ege);
                        ege.PopuulateEncounterGoalElementDisciplines(age.GoalElementDisciplineKeys,
                            FormModel.GetContext());
                        age.CurrentEncounterGoalElement = ege;
                    }
            }

            // rows from incomplete encounters are added back in the new visit frequency control.  MaintenanceUserControls.cs -> VisitFrequencyUserControlBase
            var existingadf = CurrentAdmission.AdmissionDisciplineFrequency.Where(p => !p.Superceded && !p.Inactive
                // Only include FCD's from encounters that have been signed.  FCD's from non signed encounters may be invalid.
                && (p.AddedFromEncounterKey == null
                    || ((CurrentEncounter != null)
                        && (p.AddedFromEncounterKey == CurrentEncounter.EncounterKey)
                    )
                    || (p.AddedFromEncounterKey != null
                        && p.Encounter != null
                        && p.Encounter.EncounterStatus == (int)EncounterStatusType.Completed
                    )
                )
            );
            foreach (var adf in existingadf)
            {
                EncounterDisciplineFrequency edf = new EncounterDisciplineFrequency();
                edf.AdmissionKey = CurrentAdmission.AdmissionKey;
                FormModel.GetContext().EntityContainer.GetEntitySet<EncounterDisciplineFrequency>().Add(edf);
                CurrentEncounter.EncounterDisciplineFrequency.Add(edf);
                CurrentPatient.EncounterDisciplineFrequency.Add(edf);
                adf.EncounterDisciplineFrequency.Add(edf);
                if (CurrentOrderEntryManager != null)
                {
                    EncounterStartDisciplineFrequency esdf = new EncounterStartDisciplineFrequency
                        { DisplayDisciplineFrequencyText = adf.DisplayDisciplineFrequencyText };
                    esdf.AdmissionKey = CurrentAdmission.AdmissionKey;
                    esdf.PatientKey = CurrentAdmission.PatientKey;
                    FormModel.GetContext().EntityContainer.GetEntitySet<EncounterStartDisciplineFrequency>().Add(esdf);
                    CurrentEncounter.EncounterStartDisciplineFrequency.Add(esdf);
                    CurrentPatient.EncounterStartDisciplineFrequency.Add(esdf);
                    adf.EncounterStartDisciplineFrequency.Add(esdf);
                }
            }

            // rows from incomplete encounters are added back in the new visit frequency control.  MaintenanceUserControls.cs -> VisitFrequencyUserControlBase
            var existingac = CurrentAdmission.AdmissionConsent.Where(p => !p.Superceded && !p.Inactive);
            foreach (var ac in existingac)
            {
                EncounterConsent ec = new EncounterConsent();
                CurrentEncounter.EncounterConsent.Add(ec);
                ac.EncounterConsent.Add(ec);
            }

            // Equipment Orders are currently not going to be copied forward. -- On anything other than POC's
            if (CurrentForm.IsPlanOfCare)
            {
                var existingEquip = CurrentAdmission.AdmissionEquipment.Where(e => (!e.Superceded) && (!e.Inactive));
                foreach (AdmissionEquipment equip in existingEquip)
                {
                    EncounterEquipment eq = new EncounterEquipment();
                    CurrentEncounter.EncounterEquipment.Add(eq);
                    equip.EncounterEquipment.Add(eq);
                }
            }

            if (CurrentForm.IsPlanOfCare)
            {
                CopyForwardEncounterSupplies();
            }
        }

        public bool OkToRefreshEncounterData()
        {
            // Also called from PatientCollectionBase->AddRowsFromIncompleteEncounters()
            if (CurrentForm.IsPlanOfCare
                && ((CurrentEncounter.EncounterStatus == (int)EncounterStatusType.None) ||
                    (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Edit))
                && HasInitialDataChanged())
            {
                return true;
            }

            if (CurrentForm.IsHIS && (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed) &&
                (CurrentEncounter.MostRecentEncounterOasis != null) &&
                (CurrentEncounter.MostRecentEncounterOasis.RFA == "09"))
            {
                return true;
            }

            return false;
        }

        private bool _RefreshCopyForwardData;

        public bool RefreshCopyForwardData
        {
            get { return _RefreshCopyForwardData; }
            set { _RefreshCopyForwardData = value; }
        }

        private void EncounterSetup()
        {
            if (CurrentPatient != null)
            {
                foreach (PatientAllergy a in CurrentPatient.PatientAllergy.Where(p => (!p.Inactive)))
                    a.CurrentEncounter = CurrentEncounter;
                foreach (AdmissionDiagnosis d in CurrentAdmission.AdmissionDiagnosis)
                    d.CurrentEncounter = CurrentEncounter;
                foreach (PatientMedication m in CurrentPatient.PatientMedication) m.CurrentEncounter = CurrentEncounter;
            }

            if (CurrentAdmission != null)
            {
                foreach (AdmissionLevelOfCare l in CurrentAdmission.AdmissionLevelOfCare)
                    l.CurrentEncounter = CurrentEncounter;
                foreach (AdmissionPainLocation p in CurrentAdmission.AdmissionPainLocation)
                    p.CurrentEncounter = CurrentEncounter;
                foreach (AdmissionWoundSite w in CurrentAdmission.AdmissionWoundSite)
                    w.CurrentEncounter = CurrentEncounter;
            }
        }

        private void RefreshAdmissionDataOnLoad()
        {
            if ((FormModel != null) && (EntityManager.IsOnline))
            {
                if ((CurrentAdmission != null)
                    && (CurrentAdmission.PatientInsurance != null)
                    && (CurrentAdmission.PatientInsurance.InsuranceKey.HasValue)
                    && (!CurrentAdmission.FaceToFaceEncounter.HasValue)
                   )
                {
                    Insurance i = InsuranceCache.GetInsuranceFromKey(CurrentAdmission.PatientInsurance.InsuranceKey);
                    if ((i != null)
                        && i.FaceToFaceOnAdmit
                       )
                    {
                        CurrentAdmission.BeginEditting();
                        CurrentAdmission.FaceToFaceEncounter =
                            CodeLookupCache.GetKeyFromCode("FACETOFACE", "DoWithCert");
                        CurrentAdmission.EndEditting();
                    }
                }
            }
        }

        private void RefreshAdmissionPhysicianData()
        {
            if ((FormModel != null) && (EntityManager.IsOnline))
            {
                if (CurrentAdmission != null)
                {
                    FormModel.RefreshAdmissionPhysician(CurrentAdmission.AdmissionKey);
                }
            }
        }

        private void RefreshAdmissionCoverageData()
        {
            if ((FormModel != null) && (EntityManager.IsOnline))
            {
                if (CurrentAdmission != null)
                {
                    FormModel.RefreshAdmissionCoverage(CurrentAdmission.AdmissionKey);
                }
            }
        }

        private void RefreshPatient(int patientKey)
        {
            if ((FormModel != null) && (EntityManager.IsOnline))
            {
                if ((CurrentPatient != null) && (CurrentPatient.PatientKey == patientKey))
                {
                    FormModel.RefreshPatientFacilityStay(CurrentPatient.PatientKey);
                }
            }
        }

        private void RefreshPatientAddress()
        {
            if ((FormModel != null) && (EntityManager.IsOnline))
            {
                if (CurrentAdmission != null)
                {
                    FormModel.RefreshPatientAddress(CurrentPatient.PatientKey);
                }
            }
        }

        private NavigateCloseDialogWithRich CreateQuestionDialogueWithRich(String Msg, String Title, String Question,
            String RichText, string Header)
        {
            NavigateCloseDialogWithRich d = new NavigateCloseDialogWithRich
            {
                LayoutRoot =
                {
                    Margin = new Thickness(5)
                },
                Width = double.NaN,
                Height = double.NaN,
                ErrorMessageTextBox =
                {
                    ParagraphText = Msg
                },
                ErrorRichTextMessage =
                {
                    ParagraphText = RichText
                },
                ErrorQuestionRichTextMessage =
                {
                    ParagraphText = Question
                },
                ErrorMessageHeader = Header,
                Title = "Continue?"
            };

            return d;
        }

        private NavigateCloseDialog CreateQuestionDialogue(String Msg, String Title, String Question)
        {
            NavigateCloseDialog d = new NavigateCloseDialog
            {
                LayoutRoot =
                {
                    Margin = new Thickness(5)
                },
                Width = double.NaN,
                Height = double.NaN,
                ErrorMessage = Msg,
                ErrorQuestion = Question,
                Title = "Continue?"
            };

            return d;
        }

        public bool HasSignatureOrIsComplete(Encounter _encounter)
        {
            var have_signature = false;
            var encounterSignature = _encounter.EncounterSignature.FirstOrDefault();
            if (encounterSignature != null)
            {
                have_signature = (encounterSignature.Signature != null);
            }

            return have_signature || _encounter.EncounterStatus == (int)EncounterStatusType.Completed;
        }

        private int _SlaveValue;

        public int SlaveValue
        {
            get { return _SlaveValue; }
            set
            {
                _SlaveValue = value;
                RaisePropertyChanged("SlaveValue");
            }
        }

        private Encounter GetMostRecentEncounterForSection(int sectionKey)
        {
            foreach (Encounter en in CurrentAdmission.Encounter
                         .Where(e => e.EncounterStatus != (int)EncounterStatusType.None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime)
                         .Where(enc => enc.ReCertDiscipline == true || enc.ReCertDiscipline == null))
            {
                EncounterData ed = en.EncounterData.Where(p => p.SectionKey == sectionKey).FirstOrDefault();
                if (ed != null)
                {
                    return en;
                }
            }

            return null;
        }

        private EncounterResumption GetMostRecentResumption()
        {
            return CurrentAdmission.MostRecentResumption;
        }

        private DateTime? GetPOCRecertTaskCutoffDate()
        {
            // we want to remove any non-started recert POCS - i.e., those for the 2nd and beyond cert periods - taking DisciplineRecertWindow into account
            if (CurrentAdmission == null)
            {
                return null;
            }

            AdmissionCertification ac = CurrentAdmission.GetAdmissionCertificationByPeriodNumber(1);
            if ((ac == null) || (ac.PeriodEndDate == null))
            {
                return null;
            }

            DateTime cutoffDate = ac.PeriodEndDate.GetValueOrDefault().Date.AddDays(-(DisciplineRecertWindow + 1));
            return cutoffDate;
        }

        private EncounterTransfer GetMostRecentTransfer()
        {
            if (CurrentAdmission == null)
            {
                return null;
            }

            Encounter en = CurrentAdmission
                .Encounter
                .Where(p => p.Form != null)
                .Where(p => p.EncounterOrTaskStartDateAndTime != null && p.Form.IsTransfer)
                .OrderBy(p => p.EncounterOrTaskStartDateAndTime)
                .FirstOrDefault();

            if (en == null)
            {
                return null;
            }

            return en.EncounterTransfer.FirstOrDefault();
        }

        private List<QuestionUI> ReEvaluateQuestions = new List<QuestionUI>();

        public bool HideSection(Section section)
        {
            if (section == null)
            {
                return true;
            }

            if (section.IsMedicalEligibilitySection)
            {
                if ((CurrentAdmission != null) && (CurrentAdmission.PatientInsuranceKey != null))
                {
                    PatientInsurance pi = CurrentPatient.PatientInsurance
                        .Where(p => p.PatientInsuranceKey == CurrentAdmission.PatientInsuranceKey).FirstOrDefault();
                    Insurance i = (pi == null) ? null : InsuranceCache.GetInsuranceFromKey(pi.InsuranceKey);
                    if ((i != null) && (i.IsMedicareEligibilityRequired))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public void ProcessFormSections(Form f, ObservableCollection<SectionUI> sections, bool hidelabel,
            bool addtoprint)
        {
            //Build form UI
            ReEvaluateQuestions.Clear();
            foreach (var fs in f.FormSection.OrderBy(p => p.Sequence))
                ProcessFormSectionQuestions(fs, sections, hidelabel, addtoprint);
            ProcessFormAddendumSectionUI(sections, hidelabel, addtoprint, addendumSequence);

            foreach (var section in sections)
            {
                if ((f.IsVisit || f.IsResumption || f.IsDischarge) && (f.IsVisitTeleMonitoring == false) &&
                    section.Label.Equals("Re-Evaluate"))
                {
                    if (f.SourceEvalKey.HasValue && (CurrentEncounter.IsNew ||
                                                     CurrentEncounter.EncounterStatus == (int)EncounterStatusType.None))
                    {
                        int sectionQuestionSequence = 1;
                        foreach (var fs in DynamicFormCache.GetReEvaluateFormSectionsByFormKey(f.SourceEvalKey.Value))
                            if (HideSection(fs.Section) == false)
                            {
                                Question temp = new Question
                                {
                                    Label = fs.Section.Label, DataTemplate = "ReEvaluate", BackingFactory = "ReEvaluate"
                                };
                                var field = DynamicQuestionFactory(fs, -1, -1, sectionQuestionSequence++, false, temp,
                                    section.Questions);
                                field.Question = temp;
                                field.IndentLevel = 1;
                                if (fs.Section != null)
                                {
                                    field.Label = fs.Section.Label;
                                }

                                field.Hidden = hidelabel;
                                field.SetupMessages();
                                section.Questions.Add(field);
                            }
                    }
                    else //historically we need to get what sections were present based on what is in encounterdata
                    {
                        foreach (var ed in CurrentEncounter.EncounterData
                                     .Where(p => p.ReEvaluateFormSectionKey.HasValue)
                                     .OrderBy(p => p.ReEvaluateFormSectionSequence))
                        {
                            var fs = DynamicFormCache.GetFormSectionByKey(ed.ReEvaluateFormSectionKey.Value);

                            if ((HideSection(fs.Section) == false) ||
                                (ed.BoolData ==
                                 true)) // force show if there is already data against the section (i.e., green check box)
                            {
                                Question temp = new Question
                                {
                                    Label = fs.Section.Label, DataTemplate = "ReEvaluate", BackingFactory = "ReEvaluate"
                                };
                                var label = DynamicQuestionFactory(fs, -1, -1, fs.Sequence, false, temp,
                                    section.Questions);
                                label.Question = temp;
                                label.IndentLevel = 1;
                                if (ed.Section != null)
                                {
                                    label.Label = ed.Section.Label;
                                }
                                else
                                {
                                    //Lookup section...ed.Section is null...why null for some and not all EncounterData rows saved to disk?
                                    label.Label = fs.Section.Label;
                                }

                                label.Hidden = hidelabel;
                                section.Questions.Add(label);
                            }
                        }
                    }
                }

                foreach (var q in section.Questions)
                {
                    q.PreProcessing();
                    if (dynamicFormState != null)
                    {
                        q.RestoreOfflineState(dynamicFormState);
                    }
                }
            }

            ProcessFilteredSections();
        }

        public bool IsBereavementRiskAssessment;
        public Guid? RiskForID;

        private bool IsSignatureSection(string labelToUse)
        {
            if (labelToUse.Equals("Signature"))
            {
                return true;
            }

            if (labelToUse.Equals("Provider Information") && (CurrentForm != null) &&
                (CurrentForm.IsDischarge || CurrentForm.IsTransfer))
            {
                return true;
            }

            if (labelToUse.Equals("Patient Information") && (CurrentForm != null) && CurrentForm.IsCOTI)
            {
                return true;
            }

            if (labelToUse.Equals("Verbal Certification of Terminal Illness") && (CurrentForm != null) &&
                CurrentForm.IsVerbalCOTI)
            {
                return true;
            }

            if (labelToUse.Equals("Face-to-Face Encounter") && (CurrentForm != null) && CurrentForm.IsHospiceF2F)
            {
                return true;
            }

            if (labelToUse.Equals("Progress Note") && (CurrentForm != null) && CurrentForm.IsBasicVisit)
            {
                return true;
            }

            return false;
        }

        public void ProcessFormSectionQuestions(FormSection fs, ObservableCollection<SectionUI> sections,
            bool hidelabel, bool addtoprint, bool pIsBereavementRiskAssessment = false, Guid? pRiskForID = null,
            bool includeDynamicSection = true)
        {
            IsBereavementRiskAssessment = pIsBereavementRiskAssessment;
            RiskForID = pRiskForID;
            RaisePropertyChangedPrintStuff();

            if ((fs.Section != null) && fs.Section.IsOasisSection)
            {
                if ((CurrentOasisManager != null) && CurrentOasisManager.IsOasisActive)
                {
                    OasisSurveyGroup g = CurrentOasisManager.OasisSurveyGroups
                        .Where(osg => osg.SectionLabel == fs.Section.OasisLabel).FirstOrDefault();
                    if (g != null)
                    {
                        ObservableCollection<QuestionUI> ocq = new ObservableCollection<QuestionUI>();
                        Section s = new Section { SectionKey = 0, TenantID = CurrentForm.TenantID, };
                        FormSection OasisFormSection = new FormSection
                        {
                            FormSectionKey = 0, FormKey = CurrentForm.FormKey, Section = s, SectionKey = 0,
                            Sequence = fs.Sequence
                        };
                        CurrentOasisQuestionKey = 0;
                        CurrentSectionUI = new SectionUI(TaskKey)
                        {
                            Label = g.SectionLabel, PatientDemographics = false, Questions = ocq, IsOasis = true,
                            OasisLabel = CurrentEncounter.SYS_CDDescription
                        };
                        Question temp = new Question
                        {
                            Label = g.Label, DataTemplate = "OasisSectionLabel", BackingFactory = "OasisSectionLabel"
                        };
                        var label = DynamicQuestionFactory(OasisFormSection, -1, -1, OasisFormSection.SectionKey.Value,
                            false, temp, ocq);
                        label.Question = temp;
                        label.IndentLevel = 0;
                        label.Label = g.Label;
                        label.Hidden = hidelabel;

                        sections.Add(CurrentSectionUI);

                        ocq.Add(label);

                        OasisSurveyGroup og = OasisCache.GetOasisSurveyGroupByKey(g.OasisSurveyGroupKey);
                        if (og == null)
                        {
                            return;
                        }

                        if (og.OasisSurveyGroupQuestion == null)
                        {
                            return;
                        }

                        bool wasTrackingSheetAdded = false;
                        Question oq = null;
                        foreach (OasisSurveyGroupQuestion gq in og.OasisSurveyGroupQuestion.OrderBy(o =>
                                     o.OasisQuestion.Sequence))
                        {
                            if (gq.OasisQuestion == null)
                            {
                                continue;
                            }

                            CurrentOasisManager.OasisQuestions.Add(gq.OasisQuestion);
                            if (gq.OasisQuestion.CachedOasisLayout == null)
                            {
                                continue;
                            }

                            CurrentOasisSurveyGroupKey = gq.OasisSurveyGroupKey;
                            CurrentOasisQuestionKey = gq.OasisQuestionKey;
                            oq = CurrentOasisManager.GetOasisDynamicQuestion(gq, wasTrackingSheetAdded);
                            if (oq != null)
                            {
                                if ((gq.OasisQuestion.CachedOasisLayout.Type == (int)OasisType.TrackingSheet) ||
                                    (gq.OasisQuestion.CachedOasisLayout.Type == (int)OasisType.HISTrackingSheet))
                                {
                                    wasTrackingSheetAdded = true;
                                }

                                oq.ProtectedOverride = false;
                                var field = DynamicQuestionFactory(OasisFormSection, -1, -1, OasisFormSection.Sequence,
                                    false, oq, ocq);
                                field.GoalManager = CurrentGoalManager;
                                field.IndentLevel = 1;
                                field.Required = gq.OasisQuestion.Required;
                                field.SetupMessages();
                                ocq.Add(field);
                            }
                        }
                    }
                }
            }
            else if (fs.Section != null)
            {
                ObservableCollection<QuestionUI> q = new ObservableCollection<QuestionUI>();
                string LabelToUse = fs.Section.Label;
                string LabelToUse2 = "";
                bool patientDemographics = false;

                if (LabelToUse.Equals("PatientDemographics", StringComparison.OrdinalIgnoreCase))
                {
                    LabelToUse = CurrentPatient.FullNameInformal + Environment.NewLine + "\t" +
                                 CurrentPatient.MRNDescription;
                    AdmissionCertification cs = GetEncounterCertCycleToUse();

                    var st = (CurrentEncounter == null || CurrentEncounter.ServiceTypeKey == null)
                        ? null
                        : ServiceTypeCache.GetServiceTypeFromKey((int)CurrentEncounter.ServiceTypeKey);
                    if (cs != null)
                    {
                        if (!CurrentAdmission.HideCertPeriods)
                        {
                            LabelToUse = LabelToUse
                                         + Environment.NewLine + "\t"
                                         + (cs.PeriodStartDate == null
                                             ? ""
                                             : ((DateTime)cs.PeriodStartDate).ToShortDateString())
                                         + (cs.PeriodStartDate == null ? "" : " Thru ")
                                         + (cs.PeriodEndDate == null
                                             ? ""
                                             : ((DateTime)cs.PeriodEndDate).ToShortDateString());
                        }
                    }

                    LabelToUse2 = (st == null ? "Unknown" : st.Description);
                    patientDemographics = true;
                }

                SectionUI MySectionUI = new SectionUI(TaskKey)
                    { Label = LabelToUse, PatientDemographics = patientDemographics, Questions = q };
                if (MySectionUI != null && !String.IsNullOrEmpty(LabelToUse2))
                {
                    MySectionUI.LabelLine2 = LabelToUse2;
                }

                CurrentSectionUI = MySectionUI;
                Question temp = new Question
                    { Label = LabelToUse, DataTemplate = "SectionLabel", BackingFactory = "SectionLabel" };

                // Assume signature will be last section - and add others before it if need be
                int sectionSequence = fs.Sequence;
                if (IsSignatureSection(LabelToUse) && includeDynamicSection)
                {
                    ProcessFormSupervisionSectionUI(sections, hidelabel, addtoprint, sectionSequence);
                    sectionSequence = sectionSequence + 1; // make room for the supervision question
                    ProcessFormOrderEntryVOSectionUI(sections, hidelabel, false, sectionSequence);
                    sectionSequence = sectionSequence + 1; // make room for the OrderEntryVO question
                    ProcessFormOasisSectionUI(sections, hidelabel, addtoprint, sectionSequence);
                    sectionSequence = sectionSequence + 20; //make room for the section(s)
                }

                addendumSequence = sectionSequence + 4; //incase we need addendum

                var label = DynamicQuestionFactory(fs, -1, -1, sectionSequence, false, temp, q);
                label.Question = temp;
                label.IndentLevel = 0;
                label.Label = fs.Section.Label;
                label.Hidden = hidelabel;

                if (!patientDemographics)
                {
                    q.Add(label);
                }

                sections.Add(MySectionUI);

                foreach (var sq in fs.FormSectionQuestion.OrderBy(p => p.Sequence))
                    if (sq.QuestionGroupKey > 0)
                    {
                        ProcessQuestionGroup(fs, -1, sq.QuestionGroup.QuestionGroupQuestion, sq.Sequence, q,
                            sq.QuestionGroup.QuestionGroupKey,
                            string.IsNullOrEmpty(sq.LabelOverride) ? sq.QuestionGroup.Label : sq.LabelOverride, 0,
                            hidelabel, addtoprint);
                    }
                    else if (sq.QuestionKey > 0)
                    {
                        if (sq.Question == null)
                        {
                            MessageBox.Show(
                                "Error DynamicFormViewModel.ProcessFormSectionQuestions: Dynamic Form Question with QuesionKey = '" +
                                sq.QuestionKey + "' does not exist.  contact AlayaCare support.");
                            return;
                        }

                        if (sq.Question.DataTemplateNewDiagnosisVersionOverride(CurrentEncounter.NewDiagnosisVersion) ==
                            false)
                        {
                            sq.Question.ProtectedOverride = sq.ProtectedOverride;
                            var field = DynamicQuestionFactory(fs, -1, -1, fs.Sequence, sq.CopyForward, sq.Question, q);
                            field.GoalManager = CurrentGoalManager;
                            field.OasisManager = CurrentOasisManager;
                            field.IndentLevel = 1;
                            field.Label = !string.IsNullOrEmpty(sq.LabelOverride)
                                ? sq.LabelOverride
                                : sq.Question.Label;
                            field.Required = sq.Required;
                            field.SetupMessages();
                            q.Add(field);
                            //INCLUDE For MSP cleanup
                            QuestionMasterList.Add(field);
                        }
                        else
                        {
                            Question cloneQCM = (Question)Clone(sq.Question);
                            cloneQCM.DataTemplate = sq.Question.DataTemplate + "CM";
                            Question cloneQPCS = (Question)Clone(sq.Question);
                            cloneQPCS.DataTemplate = sq.Question.DataTemplate + "PCS";

                            cloneQCM.ProtectedOverride = sq.ProtectedOverride;
                            var fieldCM = DynamicQuestionFactory(fs, -1, -1, fs.Sequence, sq.CopyForward, cloneQCM, q);
                            fieldCM.GoalManager = CurrentGoalManager;
                            fieldCM.OasisManager = CurrentOasisManager;
                            fieldCM.IndentLevel = 1;
                            fieldCM.Label = !string.IsNullOrEmpty(sq.LabelOverride) ? sq.LabelOverride : cloneQCM.Label;
                            fieldCM.Required = sq.Required;
                            fieldCM.SetupMessages();
                            q.Add(fieldCM);
                            //INCLUDE For MSP cleanup
                            QuestionMasterList.Add(fieldCM);
                            cloneQPCS.ProtectedOverride = sq.ProtectedOverride;
                            var fieldPCS =
                                DynamicQuestionFactory(fs, -1, -1, fs.Sequence, sq.CopyForward, cloneQPCS, q);
                            fieldPCS.GoalManager = CurrentGoalManager;
                            fieldPCS.OasisManager = CurrentOasisManager;
                            fieldPCS.IndentLevel = 1;
                            fieldPCS.Label = !string.IsNullOrEmpty(sq.LabelOverride)
                                ? sq.LabelOverride
                                : cloneQPCS.Label;
                            fieldPCS.Required = sq.Required;
                            fieldPCS.SetupMessages();
                            q.Add(fieldPCS);
                            //INCLUDE For MSP cleanup
                            QuestionMasterList.Add(fieldPCS);
                        }
                    }

                if (IsSignatureSection(LabelToUse))
                {
                    if ((CurrentOasisManager != null) && (CurrentEncounter != null))
                    {
                        CurrentOasisManager.SetupEncounterCollectedBy();
                    }
                }
            }
            else if (fs.RiskAssessment != null)
            {
                ObservableCollection<QuestionUI> q = new ObservableCollection<QuestionUI>();
                Question temp = new Question
                    { Label = fs.RiskAssessment.Label, DataTemplate = "RiskAssessment", BackingFactory = "Risk" };

                var risk = DynamicQuestionFactory(fs, -1, -1, fs.Sequence, false, temp, q);
                risk.Question = temp;
                risk.IndentLevel = 0;
                risk.Label = fs.RiskAssessment.Label;
                risk.Hidden = hidelabel;
                risk.OasisManager = CurrentOasisManager;
                q.Add(risk);
                //QuestionMasterList.Add(risk); //not needed for cleanup?
                QuestionMasterList.Add(risk); // needed for the new print project.

                sections.Add(new SectionUI(TaskKey)
                {
                    Label = fs.RiskAssessment.Label, PatientDemographics = false, Questions = q,
                    OuterScrollVisibility = ScrollBarVisibility.Disabled
                });
            }
        }

        public AdmissionCertification GetAdmissionCertificationFor60DaySummary()
        {
            if ((CurrentAdmission == null) || (CurrentEncounter == null))
            {
                return null;
            }

            // Get the POC Date To Use
            DateTime POCdate = CurrentEncounter.EncounterOrTaskStartDateAndTime == null
                ? DateTime.Today.Date
                : CurrentEncounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date;

            // Get the certification period for the POC date
            AdmissionCertification ac = CurrentAdmission.GetAdmissionCertForDate(POCdate, false);

            if (ac == null)
            {
                return ac;
            }

            // If the Date is inside the recert window we should use the next cert cycle
            if (POCdate.AddDays(DisciplineRecertWindow).Date > ((DateTime)ac.PeriodEndDate).Date)
            {
                return CurrentAdmission.GetAdmissionCertificationByPeriodNumber(ac.PeriodNumber + 1);
                ; // In '7-day-window' use the next cert
            }

            // Other wise use the cert period for the POCDate
            return ac;
        }

        private AdmissionCertification GetEncounterCertCycleToUse()
        {
            CurrentAdmission.DateForCertCycleDisplay =
                CurrentEncounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date;

            AdmissionCertification cs = CurrentAdmission.CurrentCert;
            return cs;
        }

        private AdmissionCertification GetEncounterCertCycleToUseForPOC()
        {
            var _startDate = CurrentEncounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date;
            DateTime dt = _startDate;

            dt = dt.AddDays(DisciplineRecertWindow);
            CurrentAdmission.DateForCertCycleDisplay = _startDate;

            AdmissionCertification cs = CurrentAdmission.CurrentCert;

            if (CurrentForm.IsPlanOfCare &&
                CurrentEncounter.GetIsEncounterInRecertWindow(CurrentAdmission, CurrentEncounter) &&
                dt > CurrentAdmission.CurrentCert.PeriodEndDate)
            {
                dt = CurrentAdmission.CurrentCert.PeriodEndDate.Value.AddDays(1);
                cs = CurrentAdmission.GetAdmissionCertForDate(dt);
            }

            return cs;
        }

        private double DisciplineRecertWindow
        {
            get
            {
                double disciplineRecertWindow = TenantSettingsCache.Current.DisciplineRecertWindowWithDefault;
                if (CurrentAdmission.ServiceLineKey > 0)
                {
                    ServiceLine serviceLine = ServiceLineCache.GetServiceLineFromKey(CurrentAdmission.ServiceLineKey);
                    double serviceLineDisciplineRecertWindow = serviceLine.DisciplineRecertWindow.GetValueOrDefault();
                    if (serviceLineDisciplineRecertWindow > 0)
                    {
                        disciplineRecertWindow = serviceLineDisciplineRecertWindow;
                    }
                }

                return disciplineRecertWindow;
            }
        }

        private string OfflineAddendumText { get; set; }
        private DynamicFormInfo dynamicFormState { get; set; }

        public QuestionUI Addendum { get; set; }

        private void ProcessFormAddendumSectionUI(ObservableCollection<SectionUI> sections, bool hidelabel,
            bool addtoprint, int sequence)
        {
            if (CurrentUserIsSurveyor)
            {
                return;
            }

            if ((CurrentEncounter == null) || (CurrentEncounter.EncounterAddendum == null))
            {
                return; // no toys to play with
            }

            if (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed)
            {
                return; // No addendums unless the encounter is complete - and its not an order
            }

            if (CurrentEncounter.EncounterKey <= 0)
            {
                return; //No addendums if encounter signed and not yet saved to the server
            }

            if ((CurrentForm != null) && (CurrentForm.IsCMSForm))
            {
                return; // No addendum on CMSForms
            }

            if ((CurrentForm != null) && (CurrentForm.IsAuthorizationRequest))
            {
                return; // No addendum on Authorization Request forms
            }

            if (AutoSaveAfterLoad && (CurrentEncounter.EncounterAddendum.Where(a => a.IsNew).Any() == false))
            {
                return; //No addendums if auto saving (from offline) and don't already have an addendum - 
            }

            IsAddendum = true;
            ObservableCollection<QuestionUI> ocq = new ObservableCollection<QuestionUI>();
            Section s = new Section { SectionKey = 0, TenantID = CurrentForm.TenantID, };
            FormSection addendumSection = new FormSection
                { FormSectionKey = 0, FormKey = CurrentForm.FormKey, Section = s, SectionKey = 0, Sequence = sequence };
            CurrentSectionUI = new SectionUI(TaskKey)
                { Label = "Addendum", PatientDemographics = false, Questions = ocq };
            Question temp = new Question
                { Label = "Addendum", DataTemplate = "SectionLabel", BackingFactory = "SectionLabel" };
            var label = DynamicQuestionFactory(addendumSection, -1, -1, addendumSection.SectionKey.Value, false, temp,
                ocq);
            label.Question = temp;
            label.IndentLevel = 0;
            label.Label = "Addendum";
            label.Hidden = hidelabel;

            sections.Add(CurrentSectionUI);

            ocq.Add(label);

            Question oq = new Question
                { QuestionKey = 0, Label = "Addendum", DataTemplate = "Addendum", BackingFactory = "Addendum" };
            oq.ProtectedOverride = false;
            Addendum = DynamicQuestionFactory(addendumSection, -1, -1, addendumSection.Sequence, false, oq, ocq);
            foreach (var ea in CurrentEncounter.EncounterAddendum)
                if (ea.IsNew &&
                    ea.AddendumText.Equals(
                        OfflineAddendumText)) //remove the existing EncounterAddendum that is new, because Addendum.Validate() will re-add it...
                {
                    ((Addendum)Addendum).AddendumText = OfflineAddendumText;
                    CurrentEncounter.EncounterAddendum.Remove(ea);
                    FormModel.RemoveEncounterAddendum(ea);
                    break; //should only ever be one that is new
                }

            Addendum.GoalManager = CurrentGoalManager;
            Addendum.IndentLevel = 1;
            Addendum.Required = true;
            Addendum.ProtectedOverrideRunTime = false;
            Addendum.OasisManager = CurrentOasisManager;
            Addendum.SetupMessages();
            ocq.Add(Addendum);
        }

        private void ProcessFormSupervisionSectionUI(ObservableCollection<SectionUI> sections, bool hidelabel,
            bool addtoprint, int sequence)
        {
            if (CurrentEncounter.EncounterIsOrderEntry)
            {
                return;
            }

            if (CurrentForm.IsOasis)
            {
                return;
            }

            if (CurrentForm.IsHIS)
            {
                return;
            }

            if (CurrentForm.IsAuthorizationRequest)
            {
                return;
            }

            if (CurrentForm.IsCOTI)
            {
                return;
            }

            if (CurrentForm.IsVerbalCOTI)
            {
                return;
            }

            ObservableCollection<QuestionUI> ocq = new ObservableCollection<QuestionUI>();
            Section s = new Section { SectionKey = 0, TenantID = CurrentForm.TenantID, };
            FormSection supervisionSection = new FormSection
                { FormSectionKey = 0, FormKey = CurrentForm.FormKey, Section = s, SectionKey = 0, Sequence = sequence };
            CurrentSectionUI = new SectionUI(TaskKey)
                { Label = "Supervision", PatientDemographics = false, Questions = ocq };
            CurrentSectionUI.IsSupervision = true;
            Question temp = new Question
                { Label = "Supervision", DataTemplate = "SectionLabel", BackingFactory = "SectionLabel" };
            var label = DynamicQuestionFactory(supervisionSection, -1, -1, supervisionSection.SectionKey.Value, false,
                temp, ocq);
            label.Question = temp;
            label.IndentLevel = 0;
            label.Label = "Supervision";
            label.Hidden = hidelabel;

            sections.Add(CurrentSectionUI);

            ocq.Add(label);

            Question oq = new Question
            {
                QuestionKey = 0, Label = "Supervision", DataTemplate = "Supervision", BackingFactory = "Supervision"
            };
            oq.ProtectedOverride = false;
            var supSection =
                DynamicQuestionFactory(supervisionSection, -1, -1, supervisionSection.Sequence, false, oq, ocq);
            supSection.IndentLevel = 1;
            ocq.Add(supSection);
        }

        private void ProcessFormOrderEntryVOSectionUI(ObservableCollection<SectionUI> sections, bool hidelabel,
            bool addtoprint, int sequence)
        {
            if (CurrentOrderEntryManager == null)
            {
                return;
            }

            if (CurrentOrderEntryManager.IsVO == false)
            {
                return;
            }

            ObservableCollection<QuestionUI> ocq = new ObservableCollection<QuestionUI>();
            Section s = new Section { SectionKey = 0, TenantID = CurrentForm.TenantID, };
            FormSection voSection = new FormSection
                { FormSectionKey = 0, FormKey = CurrentForm.FormKey, Section = s, SectionKey = 0, Sequence = sequence };
            CurrentSectionUI = new SectionUI(TaskKey) { Label = "Order", PatientDemographics = false, Questions = ocq };
            Question temp = new Question
                { Label = "Order", DataTemplate = "SectionLabel", BackingFactory = "SectionLabel" };
            var label = DynamicQuestionFactory(voSection, -1, -1, voSection.SectionKey.Value, false, temp, ocq);
            label.Question = temp;
            label.IndentLevel = 0;
            label.Label = "Order";
            label.Hidden = hidelabel;
            sections.Add(CurrentSectionUI);
            ocq.Add(label);

            Question oq = new Question
            {
                QuestionKey = 0, Label = "Order", DataTemplate = "SignatureOrderEntryVO",
                BackingFactory = "SignatureOrderEntryVO"
            };
            oq.ProtectedOverride = false;
            var voQuestion = DynamicQuestionFactory(voSection, -1, -1, voSection.Sequence, false, oq, ocq);
            voQuestion.IndentLevel = 1;
            ocq.Add(voQuestion);
        }

        private bool FilterItems(object item)
        {
            // Note - No such thing as surgical ICDs in Virtuoso for us to filter out
            AdmissionDiagnosis pd = item as AdmissionDiagnosis;
            if (pd == null)
            {
                return false;
            }

            // If we have an Encounter and the item is not new, only include the item if it is in this encounter
            if ((CurrentEncounter != null) && (!pd.IsNew))
            {
                EncounterDiagnosis ed = CurrentEncounter.EncounterDiagnosis
                    .Where(p => p.AdmissionDiagnosis.AdmissionDiagnosisKey == pd.AdmissionDiagnosisKey)
                    .FirstOrDefault();
                if (ed == null)
                {
                    return false;
                }
            }

            if (pd.Superceded)
            {
                return false;
            }

            if (pd.RemovedDate != null)
            {
                return false;
            }

            if ((pd.Version != 9) && (pd.Version != 10))
            {
                return false;
            }

            if (pd.DiagnosisStartDate != null && (DateTime)pd.DiagnosisStartDate > DateTime.Today)
            {
                return false;
            }

            if (pd.DiagnosisEndDate != null && (DateTime)pd.DiagnosisEndDate < DateTime.Today)
            {
                return false;
            }

            return true;
        }

        public void OasisSetup(bool AddingNewEncounter)
        {
            // Bypass checks if there already is one
            if (CurrentEncounter.EncounterOasis.Any() == false)
            {
                if (AddingNewEncounter == false)
                {
                    return; // Only calculate if we should be doing a survey on brand new encounters
                }

                if (CurrentForm == null)
                {
                    return;
                }

                if (CurrentAdmission == null)
                {
                    return;
                }

                bool doingOASIS = OasisSetupOASIS(AddingNewEncounter);
                if (doingOASIS == false)
                {
                    OasisSetupHIS(AddingNewEncounter); // check HIS
                }
            }
            else
            {
                // we are doing a survey
                CurrentOasisManager.CurrentEncounterOasis = CurrentOasisManager.StartNewOasisEdit();
                TurnOffOasisIfAttempted();
                CurrentOasisManager.OasisQuestions = new List<Server.Data.OasisQuestion>();
            }
        }

        private void TurnOffOasisIfAttempted()
        {
            if ((CurrentOasisManager == null) || (CurrentOasisManager.CurrentEncounterOasis == null) ||
                (CurrentForm == null))
            {
                return;
            }

            if (CurrentForm.IsAttempted == false)
            {
                return;
            }

            if (CurrentOasisManager.CurrentEncounterOasis.BypassFlag == true)
            {
                return;
            }

            CurrentOasisManager.CurrentEncounterOasis.BypassFlag = true;
            CurrentOasisManager.CurrentEncounterOasis.BypassReason = "Attempted Visit";
        }

        public bool OasisSetupHIS(bool AddingNewEncounter)
        {
            if (CanPerformHIS() == false)
            {
                return false;
            }

            if (CurrentForm == null)
            {
                return false;
            }

            string rfa = null;
            if (CurrentForm.IsHIS)
            {
                rfa = (CurrentTask == null)
                    ? null
                    : ((string.IsNullOrWhiteSpace(CurrentTask.OasisRFA)) ? null : CurrentTask.OasisRFA);
                if ((rfa == null) && (FormModel != null) && (Cancel_Command != null))
                {
                    //data was modified and now we're closing the form w/o telling the end user why...confusing...
                    FormModel.RejectMultiChanges(); //since we're force exiting - reject changes so that the form closing code doesn't prompt end user to lose changes
                    Cancel_Command.Execute(null);
                }
            }
            else if (CurrentForm.IsTeamMeeting)
            {
                return false; // TODO maybe prompt in teammeeting to do the rfa 01 or 09 RFA if not already on file or in process
            }
            else
            {
                return false;
            }

            // Fell thru - we are doing a new HIS
            if (CurrentOasisManager == null)
            {
                return false;
            }

            CurrentOasisManager.StartNewOasis("HOSPICE", rfa, false);
            CurrentOasisManager.OasisQuestions = new List<Server.Data.OasisQuestion>();
            return true;
        }

        private bool CanPerformHIS()
        {
            if (CurrentForm == null)
            {
                return false;
            }

            if (IsAssistantEncounter)
            {
                return false; // Assistants can't do HIS
            }

            if (!CurrentAdmission.HospiceAdmission)
            {
                return false;
            }

            if (CurrentForm.IsHIS)
            {
                return true;
            }

            //bfm team meeting check?
            return false;
        }

        public bool OasisSetupOASIS(bool AddingNewEncounter)
        {
            if (CanPerformOASIS() == false)
            {
                return false;
            }

            bool forceBypass = false;
            Encounter eSOC = CurrentAdmission.Encounter
                .Where(e => e.EncounterStatus !=
                            (int)EncounterStatusType
                                .None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                .Where(eo => ((eo.EncounterOasisRFA == "01")
                              && (eo.EncounterOasisM0090 != null)
                              && (eo.EncounterKey != CurrentEncounter.EncounterKey)
                              && eo.IsEncounterOasisActive))
                .OrderByDescending(eo => eo.EncounterOasisM0090).FirstOrDefault();
            bool isSOConFile = (eSOC == null) ? false : true;
            Encounter eMostRecentOASIS = CurrentAdmission.Encounter
                .Where(e => e.EncounterStatus !=
                            (int)EncounterStatusType
                                .None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                .Where(eo => ((eo.EncounterOasisM0090 != null)
                              && (eo.EncounterKey != CurrentEncounter.EncounterKey)
                              && eo.IsEncounterOasisActive))
                .OrderByDescending(eo => eo.EncounterOasisM0090).ThenByDescending(eo => eo.EncounterOasisFirstAddedDate)
                .FirstOrDefault();
            bool isLastSurveyTransfer = (eMostRecentOASIS == null)
                ? false
                : (((eMostRecentOASIS.EncounterOasisRFA == "06") || (eMostRecentOASIS.EncounterOasisRFA == "07"))
                    ? true
                    : false);

            bool isAnySurveyOnFile = (eMostRecentOASIS == null) ? false : true;
            string rfa = null;
            if (CurrentForm.IsOasis)
            {
                rfa = (CurrentTask == null)
                    ? null
                    : ((string.IsNullOrWhiteSpace(CurrentTask.OasisRFA)) ? null : CurrentTask.OasisRFA);
                if (rfa == null)
                {
                    FormModel
                        .RejectMultiChanges(); //since we're force exiting - reject changes so that the form closing code doesn't prompt end user to lose changes
                    Cancel_Command.Execute(null);
                }
            }
            else if (CurrentForm.IsEval)
            {
                if (isLastSurveyTransfer)
                {
                    rfa = "03"; // Force a ROC from this eval after a transfer
                }
                else if ((isSOConFile == false) && (CurrentAdmission != null) &&
                         (CurrentAdmission.SOCBeforeGoLiveDate == false) &&
                         (CurrentAdmission.SOCMoreThanXDaysAgo(20) == false))
                {
                    rfa = "01"; // Force a SOC if within 20 days of the SOC
                }
                else if (isAnySurveyOnFile == false)
                {
                    rfa = (OasisFollowUpDue(isAnySurveyOnFile))
                        ? "04"
                        : "05"; // Some sort of follow-up is the next 'logical' survey
                }
                else
                {
                    return false;
                }
            }
            else if (CurrentForm.IsResumption)
            {
                if (isLastSurveyTransfer)
                {
                    rfa = "03"; // Force a ROC after a transfer
                }
                else if (isAnySurveyOnFile == false)
                {
                    rfa = "03"; // Force a ROC if no surveys on file - its the next 'logical' survey
                }
                else
                {
                    return false;
                }
            }
            else if ((CurrentForm.IsVisit) && (CurrentForm.IsVisitTeleMonitoring == false))
            {
                if ((isSOConFile == false) && (CurrentAdmission != null) &&
                    (CurrentAdmission.SOCBeforeGoLiveDate == false) &&
                    (CurrentAdmission.SOCMoreThanXDaysAgo(20) == false))
                {
                    rfa = "01"; // Force a SOC if within 20 days of the SOC
                }
                else if (isLastSurveyTransfer)
                {
                    rfa = "03"; // Force a ROC from this visit after a transfer
                }
                else
                {
                    rfa = (OasisFollowUpDue(isAnySurveyOnFile))
                        ? "04"
                        : "05"; // Some sort of follow-up is the next 'logical' survey
                }
            }
            else if (CurrentForm.IsTransfer)
            {
                rfa = "06"; // Default to RFA 06, OasisManager.IsTransfer allows the user to change to RFA 07 on the tracking sheet
            }
            else if (CurrentForm.IsDischarge)
            {
                // Bypass requirement of a discharge OASIS if we are discharging on the same day as the SOC
                bool isLastSurveySOC = (eMostRecentOASIS == null)
                    ? false
                    : ((eMostRecentOASIS.EncounterOasisRFA == "01") ? true : false);
                if ((isLastSurveySOC) && (eMostRecentOASIS.EncounterOasisM0090 != null) &&
                    (((DateTime)eMostRecentOASIS.EncounterOasisM0090).Date == DateTime.Today.Date))
                {
                    return false;
                }

                // If there is another AdmissionDiscipline that isn't discharged then do not add the Oasis section
                // where OasisBypass == false verifies that the specific discipline IS AN OASIS DISCIPLINE

                var AtLeastOneNonDischargedOasisDiscipline = CurrentAdmission.AdmissionDiscipline.Where(a =>
                        a.DisciplineKey != _CurrentAdmissionDiscipline.DisciplineKey && a.IsOASISBypass == false &&
                        a.AdmissionStatusCode != "D" && a.AdmissionStatusCode != "R" && a.AdmissionStatusCode != "N")
                    .FirstOrDefault();

                if ((AtLeastOneNonDischargedOasisDiscipline != null) && (CurrentPatient.DeathDate == null))
                {
                    forceBypass = true; // Force on death - the user can always un-bypass
                }

                rfa = (CurrentPatient.DeathDate != null) ? "08" : "09";
            }
            else
            {
                return false;
            }

            // Fell thru - we are doing a new OASIS
            CurrentOasisManager.StartNewOasis("OASIS", rfa, CurrentForm.IsOasis);
            CurrentOasisManager.OasisQuestions = new List<Server.Data.OasisQuestion>();
            if (forceBypass)
            {
                CurrentOasisManager.ForceBypassDischarge();
            }

            return true;
        }

        private bool CanPerformOASIS()
        {
            if (CurrentForm == null)
            {
                return false;
            }

            if (CurrentForm.IsOasis)
            {
                return true;
            }

            if (DisciplineCache.GetDisciplineFromKey(ServiceTypeCache.GetDisciplineKey(ServiceTypeKey).Value)
                .OASISBypass)
            {
                return false; //discipline doesn't allow OASIS
            }

            if (CurrentAdmission.HospiceAdmission)
            {
                return false;
            }

            if (IsAssistantEncounter)
            {
                return false; // Do not add the sections if this is an assistant encounter
            }

            if (CurrentAdmission.PerformOasis)
            {
                return true; // Force 
            }

            ServiceLine sl = ServiceLineCache.GetServiceLineFromKey(CurrentAdmission.ServiceLineKey);
            if (sl == null)
            {
                return false;
            }

            if (sl.OasisServiceLine == false)
            {
                return false;
            }

            if (IsUnder18)
            {
                return false;
            }

            if (IsPregnant())
            {
                return false;
            }

            if (CurrentPatient.PatientInsurance == null)
            {
                return false;
            }

            PatientInsurance pi = CurrentPatient.PatientInsurance
                .Where(i => ((i.Inactive == false) && (i.HistoryKey == null) && (i.PatientInsuranceKey != 0) &&
                             ((i.EffectiveFromDate <= DateTime.UtcNow) && (i.EffectiveThruDate.HasValue == false ||
                                                                           i.EffectiveThruDate > DateTime.UtcNow))) &&
                            i.OASIS)
                .FirstOrDefault();
            return (pi == null) ? false : true;
        }

        private bool IsUnder18
        {
            get
            {
                if (CurrentPatient == null)
                {
                    return false;
                }

                return CurrentPatient.IsUnder18;
            }
        }

        private bool IsPregnant()
        {
            if (CurrentPatient == null)
            {
                return false;
            }

            if (CurrentAdmission.AdmissionDiagnosis == null)
            {
                return false;
            }

            _CurrentFilteredAdmissionDiagnosis.Source = CurrentAdmission.AdmissionDiagnosis;
            CurrentFilteredAdmissionDiagnosis.SortDescriptions.Add(new SortDescription("Sequence", ListSortDirection.Ascending));
            CurrentFilteredAdmissionDiagnosis.Filter = FilterItems;
            CurrentFilteredAdmissionDiagnosis.Refresh();
            foreach (AdmissionDiagnosis pd in CurrentFilteredAdmissionDiagnosis)
            {
                if (pd.IsPregnant)
                {
                    return true;
                }
            }

            return false;
        }

        private void ProcessFormOasisSectionUI(ObservableCollection<SectionUI> sections, bool hidelabel, bool addtoprint, int sequence)
        {
            if (CurrentOasisManager == null)
            {
                return;
            }

            if (CurrentOasisManager.IsOasisActive == false)
            {
                return;
            }

            OasisSequence = sequence;
            LoadOasisSections(sections, hidelabel, addtoprint, OasisSequence);
            if (CurrentOasisManager != null)
            {
                CurrentOasisManager.Setup(AddingNewEncounter, CurrentAdmissionDiscipline);
            }
        }

        private bool OasisFollowUpDue(bool isAnySurveyOnFile)
        {
            // Calculates earliest fudd >=  today-5.

            if (CurrentAdmission == null)
            {
                return false;
            }

            if (CurrentAdmission.SOCDate == null)
            {
                return false;
            }

            if (CurrentAdmission.CertificationPeriodDuration == null)
            {
                return false;
            }

            DateTime today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
            DateTime tempDate;
            DateTime socDate = ((DateTime)CurrentAdmission.SOCDate).Date;
            DateTime fuddDate = socDate.Date;
            int certificationPeriod = (CurrentAdmission.CertificationPeriodDuration == null)
                ? 60
                : (int)CurrentAdmission.CertificationPeriodDuration;
            int badgerDays =
                isAnySurveyOnFile
                    ? (certificationPeriod - 15)
                    : 5; // if taking on admission mid-stream into oasis - must do next logical survey within 5 days
            if (badgerDays > 45)
            {
                badgerDays = 45; // badger for at most, 45 days after the fact
            }

            // Check if still in SOC window
            if (socDate > today)
            {
                return false;
            }

            if (socDate >= today.AddDays(-5))
            {
                return false;
            }

            bool found = false;
            while (found == false)
            {
                fuddDate = fuddDate.AddDays(certificationPeriod);
                tempDate = fuddDate.AddDays(-5);
                if (today < tempDate)
                {
                    return false; // Follow-Up Survey not due until fuddDate
                }

                // see if today is in the 'window' for this next possible calculated fudd (the tempDate) 
                // (5 days before - until badgerDays after) 
                if ((today >= tempDate) && (today <= tempDate.AddDays(badgerDays + 5)))
                {
                    fuddDate = tempDate;
                    found = true;
                }
            }

            // Got next potential FUDD
            if (today < fuddDate)
            {
                return false; // Follow-Up Survey not due until fuddDate
            }

            // Get most recent FU and ROC
            Encounter eROC = CurrentAdmission.Encounter
                .Where(e => e.EncounterStatus !=
                            (int)EncounterStatusType
                                .None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                .Where(eo => ((eo.EncounterOasisRFA == "03")
                              && (eo.EncounterOasisM0090 != null)
                              && (eo.EncounterKey != CurrentEncounter.EncounterKey)
                              && eo.IsEncounterOasisActive
                              && (eo.EncounterOasisM0090 >= fuddDate)
                              && (eo.EncounterOasisM0090 <= fuddDate.AddDays(4))))
                .OrderByDescending(eo => eo.EncounterOasisM0090).FirstOrDefault();
            Encounter eFU = CurrentAdmission.Encounter
                .Where(e => e.EncounterStatus !=
                            (int)EncounterStatusType
                                .None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                .Where(eo => ((eo.EncounterOasisRFA == "04")
                              && (eo.EncounterOasisM0090 != null)
                              && (eo.EncounterKey != CurrentEncounter.EncounterKey)
                              && eo.IsEncounterOasisActive))
                .OrderByDescending(eo => eo.EncounterOasisM0090).FirstOrDefault();
            // if ROC used for FU - override most recent FU 
            if (eROC != null)
            {
                eFU = eROC;
            }

            if (eFU == null)
            {
                return true;
            }

            if (eFU.EncounterOasisM0090 == null)
            {
                return true;
            }

            if ((eFU.EncounterOasisM0090 >= fuddDate) && (eFU.EncounterOasisM0090 <= fuddDate.AddDays(4 + badgerDays)))
            {
                // FU or ROC was already done in the Follow-Up Window
                return false;
            }

            if (today > fuddDate.AddDays(4 + badgerDays))
            {
                // if we are past the badger window - quit bothering
                return false;
            }

            return true;
        }

        private void LoadOasisSections(ObservableCollection<SectionUI> sections, bool hidelabel, bool addtoprint,
            int sequence)
        {
            SectionUI signatureSectionUI = sections.Where(ss => ss.Label == "Signature").LastOrDefault();
            if ((signatureSectionUI == null) && (CurrentForm != null) &&
                (CurrentForm.IsDischarge || CurrentForm.IsTransfer))
            {
                signatureSectionUI = sections.Where(ss => ss.Label == "Provider Information").LastOrDefault();
            }

            if ((signatureSectionUI == null) && (CurrentForm != null) && CurrentForm.IsCOTI)
            {
                signatureSectionUI = sections.Where(ss => ss.Label == "Patient Information").LastOrDefault();
            }

            if ((signatureSectionUI == null) && (CurrentForm != null) && CurrentForm.IsVerbalCOTI)
            {
                signatureSectionUI = sections.Where(ss => ss.Label == "Verbal Certification of Terminal Illness")
                    .LastOrDefault();
            }

            if ((signatureSectionUI == null) && (CurrentForm != null) && CurrentForm.IsHospiceF2F)
            {
                signatureSectionUI = sections.Where(ss => ss.Label == "Face-to-Face Encounter").LastOrDefault();
            }

            if ((signatureSectionUI == null) && (CurrentForm != null) && CurrentForm.IsBasicVisit)
            {
                signatureSectionUI = sections.Where(ss => ss.Label == "Progress Note").LastOrDefault();
            }

            CurrentOasisManager.SetupMessaging();

            if (CurrentOasisManager.OasisSurveyGroups == null)
            {
                return;
            }

            if (CurrentOasisManager.OasisSurveyGroups.Any() == false)
            {
                return;
            }

            int seq = sequence;
            foreach (OasisSurveyGroup g in CurrentOasisManager.OasisSurveyGroups)
            {
                SectionUI oasisSectionUI =
                    sections.Where(os => ((os.Label == g.SectionLabel) && os.IsOasis)).FirstOrDefault();
                if (oasisSectionUI != null)
                {
                    continue;
                }

                ObservableCollection<QuestionUI> ocq = new ObservableCollection<QuestionUI>();
                Section s = new Section { SectionKey = 0, TenantID = CurrentForm.TenantID, };
                FormSection OasisFormSection = new FormSection
                {
                    FormSectionKey = 0, FormKey = CurrentForm.FormKey, Section = s, SectionKey = 0, Sequence = seq++
                };
                CurrentOasisQuestionKey = 0;
                CurrentSectionUI = new SectionUI(TaskKey)
                {
                    Label = g.SectionLabel, PatientDemographics = false, Questions = ocq, IsOasis = true,
                    OasisLabel = CurrentEncounter.SYS_CDDescription
                };
                Question temp = new Question
                    { Label = g.Label, DataTemplate = "OasisSectionLabel", BackingFactory = "OasisSectionLabel" };
                var label = DynamicQuestionFactory(OasisFormSection, -1, -1, OasisFormSection.SectionKey.Value, false,
                    temp, ocq);
                label.Question = temp;
                label.IndentLevel = 0;
                label.Label = g.Label;
                label.Hidden = hidelabel;

                if (signatureSectionUI == null)
                {
                    sections.Add(CurrentSectionUI);
                }
                else
                {
                    int insertwhere = sections.IndexOf(signatureSectionUI);
                    sections.Insert(insertwhere, CurrentSectionUI);
                }

                ocq.Add(label);
                OasisSurveyGroup og = OasisCache.GetOasisSurveyGroupByKey(g.OasisSurveyGroupKey);
                if (og == null)
                {
                    return;
                }

                if (og.OasisSurveyGroupQuestion == null)
                {
                    return;
                }

                bool wasTrackingSheetAdded = false;
                Question oq = null;
                foreach (OasisSurveyGroupQuestion gq in og.OasisSurveyGroupQuestion.OrderBy(o =>
                             o.OasisQuestion.Sequence))
                {
                    if (gq.OasisQuestion == null)
                    {
                        continue;
                    }

                    CurrentOasisManager.OasisQuestions.Add(gq.OasisQuestion);
                    if (gq.OasisQuestion.CachedOasisLayout == null)
                    {
                        continue;
                    }

                    CurrentOasisSurveyGroupKey = gq.OasisSurveyGroupKey;
                    CurrentOasisQuestionKey = gq.OasisQuestionKey;
                    oq = CurrentOasisManager.GetOasisDynamicQuestion(gq, wasTrackingSheetAdded);
                    if (oq != null)
                    {
                        if ((gq.OasisQuestion.CachedOasisLayout.Type == (int)OasisType.TrackingSheet) ||
                            (gq.OasisQuestion.CachedOasisLayout.Type == (int)OasisType.HISTrackingSheet))
                        {
                            wasTrackingSheetAdded = true;
                        }

                        oq.ProtectedOverride = false;
                        var field = DynamicQuestionFactory(OasisFormSection, -1, -1, OasisFormSection.Sequence, false,
                            oq, ocq);
                        field.GoalManager = CurrentGoalManager;
                        field.IndentLevel = 1;
                        field.Required = gq.OasisQuestion.Required;
                        field.SetupMessages();
                        ocq.Add(field);
                    }
                }
            }

            AddOasisAlertsSection(sections, signatureSectionUI, ++seq, hidelabel);
        }

        private void AddOasisAlertsSection(ObservableCollection<SectionUI> sections, SectionUI signatureSectionUI,
            int sequence, bool hidelabel)
        {
            ObservableCollection<QuestionUI> ocq = new ObservableCollection<QuestionUI>();
            Section s = new Section { SectionKey = 0, TenantID = CurrentForm.TenantID, };
            FormSection OasisFormSection = new FormSection
                { FormSectionKey = 0, FormKey = CurrentForm.FormKey, Section = s, SectionKey = 0, Sequence = sequence };
            CurrentOasisQuestionKey = 0;
            CurrentSectionUI = new SectionUI(TaskKey)
            {
                Label = "Alerts", PatientDemographics = false, Questions = ocq, IsOasis = true, IsOasisAlert = true,
                OasisLabel = CurrentEncounter.SYS_CDDescription
            };
            Question temp = new Question
                { Label = "OASIS Alerts", DataTemplate = "OasisSectionLabel", BackingFactory = "OasisSectionLabel" };
            var label = DynamicQuestionFactory(OasisFormSection, -1, -1, OasisFormSection.SectionKey.Value, false, temp,
                ocq);
            label.Question = temp;
            label.IndentLevel = 0;
            label.Label = "OASIS Alerts";
            label.Hidden = hidelabel;

            if (signatureSectionUI == null)
            {
                sections.Add(CurrentSectionUI);
            }
            else
            {
                int insertwhere = sections.IndexOf(signatureSectionUI);
                sections.Insert(insertwhere, CurrentSectionUI);
            }

            ocq.Add(label);

            Question oq = new Question
                { QuestionKey = 0, Label = "Alerts", DataTemplate = "OasisAlerts", BackingFactory = "OasisAlerts" };
            oq.ProtectedOverride = false;
            var field = DynamicQuestionFactory(OasisFormSection, -1, -1, OasisFormSection.Sequence, false, oq, ocq);
            field.GoalManager = CurrentGoalManager;
            field.IndentLevel = 1;
            field.Required = false;
            field.OasisManager = CurrentOasisManager;
            field.SetupMessages();
            ocq.Add(field);
        }

        public void SetupAdmissionDisciplineChanged(AdmissionDiscipline admissionDiscipline)
        {
            // check for change of RFA
            if (CurrentOasisManager.IsBusy)
            {
                return;
            }

            if (admissionDiscipline != CurrentAdmissionDiscipline)
            {
                return;
            }

            if (CurrentEncounter.EncounterOasis.Any() == false)
            {
                return;
            }

            if (CurrentOasisManager.MappingAllowedClinicianBypassOASISAssist == false)
            {
                return;
            }

            if (!CurrentOasisManager.IsOasisActive)
            {
                return;
            }

            if (CurrentEncounter.SYS_CDIsHospice)
            {
                return;
            }

            string newRFA = null;
            if ((CurrentOasisManager.RFA == "08") || (CurrentOasisManager.RFA == "09"))
            {
                newRFA = (admissionDiscipline.DischargeReasonCode == null) ? "09" :
                    (admissionDiscipline.DischargeReasonCode.Equals("20")) ? "08" : "09";
                if (CurrentOasisManager.RFA == newRFA)
                {
                    return;
                }
            }
            else
            {
                return;
            }

            // Changing from rfa 08 to 09 or visa-versa - cleanup old sections
            List<SectionUI> oasisSectionsList = Sections.Where(s => s.IsOasis).ToList();
            if (oasisSectionsList != null)
            {
                SectionUI s = null;
                for (int i = 0; i < oasisSectionsList.Count(); i++)
                {
                    s = oasisSectionsList[i];
                    Sections.Remove(s);
                }
            }

            CurrentOasisManager.RFA = newRFA;

            CurrentOasisManager.CleanupOasisForVersionChange();
            LoadOasisSections(Sections, false, true, OasisSequence);
            CurrentOasisManager.ChangedDischargeRFA(newRFA, CurrentAdmissionDiscipline, CurrentPatient);
            this.RaisePropertyChangedLambda(p => p.FilteredSections);
        }

        public void OasisVersionChanged(int encounterOasisKey)
        {
            if ((CurrentOasisManager == null) || (CurrentOasisManager.CurrentEncounterOasis == null) ||
                (CurrentOasisManager.CurrentEncounterOasis.EncounterOasisKey != encounterOasisKey))
            {
                return;
            }

            if (CurrentOasisManager.IsBusy)
            {
                return;
            }

            if (!CurrentOasisManager.IsOasisActive)
            {
                return;
            }

            if (CurrentEncounter.SYS_CDIsHospice)
            {
                return;
            }

            // Changing oasisVersion - cleanup old sections
            List<SectionUI> oasisSectionsList = Sections.Where(s => s.IsOasis).ToList();
            if (oasisSectionsList != null)
            {
                SectionUI s = null;
                for (int i = 0; i < oasisSectionsList.Count(); i++)
                {
                    s = oasisSectionsList[i];
                    Sections.Remove(s);
                }
            }

            CurrentOasisManager.CleanupOasisForVersionChange();
            LoadOasisSections(Sections, false, true, OasisSequence);
            // Finish up oasis question processing
            CurrentOasisManager.ChangedOasisVersion();
            if (FilteredSections != null)
            {
                FilteredSections.Refresh();
            }

            this.RaisePropertyChangedLambda(p => p.FilteredSections);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (Sections == null)
                {
                    return;
                }

                SectionUI s = Sections.Where(p => (p.IsOasis && (p.Label.Equals("Tracking Sheet")))).FirstOrDefault();
                if (s == null)
                {
                    return;
                }

                SelectedSection = s;
            });
        }

        void ProcessQuestionGroup(FormSection formsection, int pqgkey, EntityCollection<QuestionGroupQuestion> qgq,
            int sequence, ObservableCollection<QuestionUI> qui, int groupkey, string grouplabel, int indentlevel,
            bool hidelabel, bool addtoprint)
        {
            Question temp = new Question
                { Label = grouplabel, DataTemplate = "GroupLabel", BackingFactory = "QuestionBase" };
            var label = DynamicQuestionFactory(formsection, pqgkey, groupkey, 0, false, temp, qui);
            label.Question = temp;
            label.IndentLevel = indentlevel + 1;
            label.Label = grouplabel;
            qui.Add(label);
            //MLL//QuestionMasterList.Add(label); // not needed for cleanup
            QuestionMasterList.Add(label); // needed for the new print project.

            indentlevel++;
            foreach (var q in qgq.OrderBy(p => p.Sequence))
                if (q.ChildGroupKey > 0)
                {
                    ProcessQuestionGroup(formsection, q.QuestionGroupKey, q.ChildGroup.QuestionGroupQuestion,
                        q.Sequence, qui, q.ChildGroup.QuestionGroupKey, q.ChildGroup.Label, indentlevel, hidelabel,
                        addtoprint);
                }
                else if (q.QuestionKey > 0)
                {
                    q.Question.ProtectedOverride = q.ProtectedOverride;
                    var field = DynamicQuestionFactory(formsection, pqgkey, q.QuestionGroupKey, sequence, q.CopyForward,
                        q.Question, qui);
                    field.GoalManager = CurrentGoalManager;
                    field.OasisManager = CurrentOasisManager;
                    field.IndentLevel = indentlevel + 1;
                    field.Label = string.IsNullOrEmpty(q.LabelOverride) ? q.Question.Label : q.LabelOverride;
                    field.Required = q.Required;
                    field.SetupMessages();
                    qui.Add(field);
                    QuestionMasterList.Add(field); // needed for the new print project.
                }
        }

        public QuestionUI DynamicQuestionFactory(FormSection formsection, int pqgkey, int qgkey, int sequence,
            bool copyforward, Question q, object ocq)
        {
            String AssemblyQualifiedNameFormat =
                "Virtuoso.Core.Model.{0}Factory, Virtuoso.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            string typestr = String.Format(AssemblyQualifiedNameFormat, q.BackingFactory);
            Type factoryClass = Type.GetType(typestr);
            if (factoryClass != null)
            {
                MethodInfo m = factoryClass.GetMethod("Create");
                if (m != null)
                {
                    QuestionUI qUI = (QuestionUI)m.Invoke(null,
                        new Object[] { FormModel, this, formsection, pqgkey, qgkey, sequence, copyforward, q });
                    qUI.QuestionSequenceWithinSection = QuestionSequenceWithinSection(ocq);
                    return qUI;
                }

                throw new Exception(String.Format("Invalid factory for class {0}", q.BackingFactory));
            }

            throw new Exception(String.Format("No factory for class {0}", q.BackingFactory));
        }

        private int QuestionSequenceWithinSection(object ocq)
        {
            ObservableCollection<QuestionUI> ocqo = ocq as ObservableCollection<QuestionUI>;
            if (ocqo != null)
            {
                return ocqo.Count() + 1;
            }

            List<QuestionUI> ocql = ocq as List<QuestionUI>;
            if (ocql != null)
            {
                return ocql.Count() + 1;
            }

            return +1;
        }

        private string GetDocumentDescription()
        {
            var task = CurrentTask;
            if (task == null)
            {
                return "<No description for task activity>";
            }

            if (task.NonServiceTypeKey.HasValue)
            {
                var cd = NonServiceTypeCache.GetNonServiceTypeDescFromKey(task.NonServiceTypeKey.Value);
                if (String.IsNullOrEmpty(cd))
                {
                    return String.Format("<NO Description for activity code: {0}>", task.NonServiceTypeKey.Value);
                }

                return cd;
            }

            if ((task.PatientKey.HasValue) && (task.ServiceTypeKey.HasValue))
            {
                var sd = ServiceTypeCache.GetDescriptionFromKey(task.ServiceTypeKey.Value);
                if (String.IsNullOrEmpty(sd))
                {
                    return String.Format("<NO Description for service type: {0}>", task.ServiceTypeKey.Value);
                }

                return sd;
            }

            return "<No description for task activity>";
        }

        #region Properties

        public GoalManager CurrentGoalManager { get; set; }
        public OasisManager CurrentOasisManager { get; set; }
        public OrderEntryManager CurrentOrderEntryManager { get; set; }

        ObservableCollection<SectionUI> _Sections = new ObservableCollection<SectionUI>();

        public ObservableCollection<SectionUI> Sections
        {
            get { return _Sections; }
            set
            {
                _Sections = value;
                this.RaisePropertyChangedLambda(p => p.Sections);
            }
        }

        //private CollectionViewSource _FilteredSections = new CollectionViewSource(); //throws invalid cross thread access violation when created in background thread
        private CollectionViewSource _FilteredSectionsBackingStore;

        private CollectionViewSource _FilteredSections
        {
            get
            {
                if (_FilteredSectionsBackingStore == null)
                {
                    _FilteredSectionsBackingStore = new CollectionViewSource();
                }

                return _FilteredSectionsBackingStore;
            }
        }

        public ICollectionView FilteredSections => _FilteredSections.View;

        public void ProcessFilteredSections()
        {
            _FilteredSections.Source = Sections;
            //FilteredSections.SortDescriptions.Add(new SortDescription("Sequence", ListSortDirection.Ascending));
            FilteredSections.Filter = FilterSectionItems;
            FilteredSections.Refresh();
            this.RaisePropertyChangedLambda(p => p.FilteredSections);
        }

        private bool FilterSectionItems(object item)
        {
            SectionUI s = item as SectionUI;
            // Do not display Addendum on readonly forms
            //if (s.Label.Equals("Addendum") && IsReadOnlyEncounter) return false;
            // Ask the questions in the sections if they should display
            if (!s.IsSectionVisible)
            {
                return false;
            }

            // always display non oasis sections and the tracking sheet
            if ((s.IsOasis == false) && (s.IsOasisAlert == false) && (s.IsSupervision == false))
            {
                return true;
            }

            if (s.Label.Equals("Tracking Sheet"))
            {
                return true;
            }

            //if (s.Label.Equals("Administrative Information")) return true; // bfm only if support HIS bypass in team meating
            // hide all other oasis section on bypass
            if (s.IsOasis && OasisBypassFlag == true)
            {
                return false;
            }

            // hide the supervision section if they haven't chosen to do it with this visit.
            if (s.IsSupervision && DoSupWithVisitFlag == false)
            {
                return false;
            }

            // aditionally, hide alerts section if there are no alerts
            //US2092 - Deactivate Alerts //if (s.IsOasisAlert == true) return (OasisAlertsCount == 0) ? false : true;
            if (s.IsOasisAlert)
            {
                return false;
            }

            return true;
        }

        public bool FilterSectionUI(SectionUI sectionUI)
        {
            return FilterSectionItems(sectionUI);
        }

        private int addendumSequence = 1;
        private int OasisSequence;
        private bool? OasisBypassFlag = false;

        public void OasisBypassFlagChanged(bool BypassFlag)
        {
            OasisBypassFlag = BypassFlag;
            if (FilteredSections != null)
            {
                FilteredSections.Refresh();
                this.RaisePropertyChangedLambda(p => p.FilteredSections);
            }
        }

        private bool? DoSupWithVisitFlag = false;

        public void DoSupervisionWithVisitChanged(bool DoSupFlag)
        {
            DoSupWithVisitFlag = DoSupFlag;
            if (FilteredSections != null)
            {
                FilteredSections.Refresh();
                this.RaisePropertyChangedLambda(p => p.FilteredSections);
            }
        }

        public void RefreshFilteredSections(bool noUsed)
        {
            if (FilteredSections != null)
            {
                FilteredSections.Refresh();
                this.RaisePropertyChangedLambda(p => p.FilteredSections);
            }
        }

        private int OasisAlertsCount;

        public void OasisAlertsChanged(int alertsCount)
        {
            OasisAlertsCount = alertsCount;
            if (FilteredSections != null)
            {
                FilteredSections.Refresh();
                this.RaisePropertyChangedLambda(p => p.FilteredSections);
            }
        }

        //private CollectionViewSource _CurrentFilteredAdmissionDiagnosis = new CollectionViewSource(); //throws invalid cross thread access violation when created in background thread
        private CollectionViewSource _CurrentFilteredAdmissionDiagnosisBackingStore;

        private CollectionViewSource _CurrentFilteredAdmissionDiagnosis
        {
            get
            {
                if (_CurrentFilteredAdmissionDiagnosisBackingStore == null)
                {
                    _CurrentFilteredAdmissionDiagnosisBackingStore = new CollectionViewSource();
                }

                return _CurrentFilteredAdmissionDiagnosisBackingStore;
            }
        }

        private ICollectionView CurrentFilteredAdmissionDiagnosis => _CurrentFilteredAdmissionDiagnosis.View;

        SectionUI _SelectedSection;

        public SectionUI SelectedSection
        {
            get { return _SelectedSection; }
            set
            {
                _SelectedSection = value;
                this.RaisePropertyChangedLambda(p => p.SelectedSection);
            }
        }

        private Form _CurrentForm;

        public Form CurrentForm
        {
            get { return _CurrentForm; }
            set
            {
                if (_CurrentForm != value)
                {
                    _CurrentForm = value;

                    this.RaisePropertyChangedLambda(p => p.CurrentForm);
                }
            }
        }

        private List<AuthOrderTherapyPOCO_CView> _AuthOrders;

        public List<GetInsuranceAuthOrderTherapyViewByServiceTypeKey_Result> AuthOrders =>
            ((_AuthOrders == null || _AuthOrders.Any() == false)
                ? new List<GetInsuranceAuthOrderTherapyViewByServiceTypeKey_Result>()
                : _AuthOrders.FirstOrDefault().RequireAuthList.ToList());

        private Patient _CurrentPatient;

        public Patient CurrentPatient
        {
            get { return _CurrentPatient; }
            set
            {
                if (_CurrentPatient != value)
                {
                    _CurrentPatient = value;

                    this.RaisePropertyChangedLambda(p => p.CurrentPatient);
                }

                if (!HideFromNavigation)
                {
                    if (BackgroundService.IsBackground == false)
                    {
                        Messenger.Default.Send(new ContextSensitiveArgs { ViewModel = this },
                            "SetContextSensitiveMenu");
                    }
                }
            }
        }

        private Admission _CurrentAdmission;

        public Admission CurrentAdmission
        {
            get { return _CurrentAdmission; }
            set
            {
                if (_CurrentAdmission != value)
                {
                    _CurrentAdmission = value;
                    //if (_CurrentAdmission != null) _CurrentAdmission.InDynamicForm = true;
                    this.RaisePropertyChangedLambda(p => p.CurrentAdmission);
                }
            }
        }

        private AdmissionDiscipline _CurrentAdmissionDiscipline;

        public AdmissionDiscipline CurrentAdmissionDiscipline
        {
            get { return _CurrentAdmissionDiscipline; }
            set
            {
                if (_CurrentAdmissionDiscipline != value)
                {
                    _CurrentAdmissionDiscipline = value;

                    this.RaisePropertyChangedLambda(p => p.CurrentAdmissionDiscipline);
                }
            }
        }

        private Encounter _CurrentEncounter;

        public Encounter CurrentEncounter
        {
            get { return _CurrentEncounter; }
            set
            {
                if (_CurrentEncounter != value)
                {
                    _CurrentEncounter = value;
                    lastSaveDateTime = null;
                    RefreshSaveMessage();
                    this.RaisePropertyChangedLambda(p => p.CurrentEncounter);
                }
            }
        }

        private EncounterAttempted _CurrentEncounterAttempted;

        public EncounterAttempted CurrentEncounterAttempted
        {
            get { return _CurrentEncounterAttempted; }
            set
            {
                if (_CurrentEncounterAttempted != value)
                {
                    _CurrentEncounterAttempted = value;
                    this.RaisePropertyChangedLambda(p => p.CurrentEncounterAttempted);
                }
            }
        }

        public int CurrentOasisSurveyGroupKey { get; set; }
        public int CurrentOasisQuestionKey { get; set; }

        private EncounterTransfer _CurrentEncounterTransfer;

        public EncounterTransfer CurrentEncounterTransfer
        {
            get { return _CurrentEncounterTransfer; }
            set
            {
                if (_CurrentEncounterTransfer != value)
                {
                    _CurrentEncounterTransfer = value;

                    this.RaisePropertyChangedLambda(p => p.CurrentEncounterTransfer);
                }
            }
        }

        private EncounterPlanOfCare _CurrentEncounterPlanOfCare;

        public EncounterPlanOfCare CurrentEncounterPlanOfCare
        {
            get { return _CurrentEncounterPlanOfCare; }
            set
            {
                if (_CurrentEncounterPlanOfCare != value)
                {
                    _CurrentEncounterPlanOfCare = value;

                    this.RaisePropertyChangedLambda(p => p.CurrentEncounterPlanOfCare);
                }
            }
        }

        private Task _CurrentTask;

        public Task CurrentTask
        {
            get { return _CurrentTask; }
            set
            {
                if (_CurrentTask != value)
                {
                    _CurrentTask = value;

                    this.RaisePropertyChangedLambda(p => p.CurrentTask);
                }
            }
        }

        public RelayCommand Debug_Command { get; protected set; }

        public RelayCommand Save_Command { get; protected set; }

        public RelayCommand Cancel_Command { get; protected set; }

        public RelayCommand SSRSPrint_Command { get; protected set; }

        public RelayCommand Fax_Command { get; protected set; }

        string _ErrorMessage;

        public string ErrorMessage
        {
            get { return _ErrorMessage; }
            set
            {
                _ErrorMessage = value;
                this.RaisePropertyChangedLambda(p => p.ErrorMessage);
            }
        }

        string _ValidationMessage = string.Empty;

        public string ValidationMessage
        {
            get { return _ValidationMessage; }
            set
            {
                _ValidationMessage = value;
                this.RaisePropertyChangedLambda(p => p.ValidationMessage);
            }
        }

        private bool _hideOasisQuestions;

        public bool HideOasisQuestions
        {
            get { return _hideOasisQuestions; }
            set
            {
                _hideOasisQuestions = value;
                this.RaisePropertyChangedLambda(p => p.HideOasisQuestions);
            }
        }

        public bool HasOasisSections
        {
            get
            {
                if (CurrentEncounter == null)
                {
                    return false;
                }

                if (CurrentEncounter.SYS_CDIsHospice)
                {
                    return false;
                }

                return Sections == null ? false : Sections.Any(s => s.IsOasis);
            }
        }

        private string _FaxVisibleError = string.Empty;

        public string PrintOrFaxVisibleError
        {
            get { return _FaxVisibleError; }
            set
            {
                _FaxVisibleError = value;
                this.RaisePropertyChangedLambda(p => p.PrintOrFaxVisibleError);
            }
        }

        public bool FaxVisible
        {
            get
            {
                if (!TenantSettingsCache.Current.DocumentFaxingAndTrackingEnabled)
                {
                    return false;
                }

                if (!EntityManager.IsOnline)
                {
                    return false;
                }

                if (CurrentForm == null || CurrentEncounter == null)
                {
                    return false;
                }

                //// Logic for visibility of the Fax button - Interim Order / Order Entry
                //if (CurrentForm.IsOrderEntry && CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
                //{
                //    var faxingPhysician = GetFaxingPhysician(); // Must have a Signing Physician Fax Number
                //    return faxingPhysician.HasValue;
                //}

                // Logic for visibility of the Fax button - Plan Of Care
                if ((CurrentForm.IsPlanOfCare || CurrentForm.IsOrderEntry || CurrentForm.IsCOTI) &&
                    CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    var faxingPhysician = GetFaxingPhysician(); // Must have a Signing Physician Fax Number
                    if (faxingPhysician.HasValue)
                    {
                        Log(
                            $"[FaxVisible] Physician: {faxingPhysician.Value.PhysicianKey}, Fax: {faxingPhysician.Value.FaxNumber}",
                            "FAX_TRACE");
                    }

                    return faxingPhysician.HasValue;
                }

                return
                    false; // NOTE: default is to not show the Fax button, because we're currently only supporting faxing of POC forms
            }
        }

        private bool PrintOrFaxButtonDisabled(bool isVisible, bool defaultReturn)
        {
            PrintOrFaxVisibleError = string.Empty;
            if (CurrentForm == null || CurrentEncounter == null)
            {
                return false;
            }

            if (CurrentForm.IsCOTI && CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
            {
                if (isVisible)
                {
                    var faxingPhysician = GetFaxingPhysician();
                    if (faxingPhysician.HasValue)
                    {
                        var OriginalSigningPhysicianKey = faxingPhysician.Value.PhysicianKey;

                        var ques = QuestionMasterList.ToList().Where(q => q.Label.Equals("Certification Statement"))
                            .FirstOrDefault();
                        if (ques != null &&
                            CurrentAdmission !=
                            null) // FYI: All service lines save physiciankey to OrderEntry.SigningPhysicianKey
                        {
                            var hospiceAdmissionCOTIQuestion = ques as HospiceAdmissionCOTI;
                            if (hospiceAdmissionCOTIQuestion != null)
                            {
                                OriginalSigningPhysicianKey =
                                    hospiceAdmissionCOTIQuestion.GetOriginalSigningPhysicianKey();
                            }
                        }

                        if ((faxingPhysician.Value.PhysicianKey != OriginalSigningPhysicianKey)
                            || (faxingPhysician.Value.PhysicianKey != OriginalSigningPhysicianKey)
                           )
                        {
                            PrintOrFaxVisibleError =
                                "* Signing Physician changed. You must save before printing or faxing.";
                            return false;
                        }
                    }
                }

                return isVisible;
            }

            if (CurrentForm.IsOrderEntry && CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
            {
                if (isVisible)
                {
                    var faxingPhysician = GetFaxingPhysician();
                    if (faxingPhysician.HasValue)
                    {
                        var OriginalSigningPhysicianKey = faxingPhysician.Value.PhysicianKey;

                        var ques = QuestionMasterList.ToList().Where(q => q.Label.Equals("SignatureOrderEntry"))
                            .FirstOrDefault();
                        if (ques != null &&
                            CurrentAdmission !=
                            null) // FYI: All service lines save physiciankey to OrderEntry.SigningPhysicianKey
                        {
                            var signatureOrderEntryQuestion = ques as SignatureOrderEntry;
                            if (signatureOrderEntryQuestion != null)
                            {
                                var currentOrderEntry = signatureOrderEntryQuestion.CurrentOrderEntry;
                                if (currentOrderEntry != null)
                                {
                                    OriginalSigningPhysicianKey =
                                        signatureOrderEntryQuestion.GetOriginalSigningPhysicianKey();
                                }
                            }
                        }

                        if ((faxingPhysician.Value.PhysicianKey != OriginalSigningPhysicianKey)
                            || (faxingPhysician.Value.PhysicianKey != OriginalSigningPhysicianKey)
                           )
                        {
                            PrintOrFaxVisibleError =
                                "* Signing Physician changed. You must save before printing or faxing.";
                            return false;
                        }
                    }
                }

                return isVisible;
            }

            if (CurrentForm.IsPlanOfCare && CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
            {
                // Logic for disabling the Print/Fax buttons - currently only for POC type forms
                if (isVisible)
                {
                    EncounterPlanOfCare epc = CurrentEncounter.EncounterPlanOfCare.FirstOrDefault();
                    if (epc == null)
                    {
                        return false;
                    }

                    var currentEncounterAdmission = CurrentEncounter.EncounterAdmission.FirstOrDefault();
                    if (currentEncounterAdmission == null)
                    {
                        return false;
                    }

                    var origEncounterAdmission = (EncounterAdmission)currentEncounterAdmission.GetOriginal();

                    var faxingPhysician = GetFaxingPhysician();
                    if (faxingPhysician.HasValue)
                    {
                        if (CurrentAdmission.HospiceAdmission)
                        {
                            if ((origEncounterAdmission != null && faxingPhysician.Value.PhysicianKey !=
                                    origEncounterAdmission.AttendingPhysicianKey)
                                || (origEncounterAdmission == null && faxingPhysician.Value.PhysicianKey !=
                                    currentEncounterAdmission.AttendingPhysicianKey)
                               )
                            {
                                PrintOrFaxVisibleError =
                                    "* Attending Physician changed. You must save before printing or faxing.";
                                return false;
                            }
                        }
                        else
                        {
                            if ((origEncounterAdmission != null && faxingPhysician.Value.PhysicianKey !=
                                    origEncounterAdmission.SigningPhysicianKey)
                                || (origEncounterAdmission == null && faxingPhysician.Value.PhysicianKey !=
                                    currentEncounterAdmission.SigningPhysicianKey)
                               )
                            {
                                PrintOrFaxVisibleError =
                                    "* Signing Physician changed. You must save before printing or faxing.";
                                return false;
                            }
                        }
                    }
                }

                return isVisible;
            }

            return defaultReturn;
        }

        public bool PrintVisible
        {
            get
            {
                if (CurrentUserIsSurveyor)
                {
                    return false;
                }

                var ssrs = PrintVisibleSSRS;
                var fax = FaxVisible;
                System.Diagnostics.Debug.WriteLine($"(PrintVisibleSSRS: {ssrs} || FaxVisible: {fax})");
                return ssrs || fax;
            }
        }

        public bool PrintVisibleSSRS
        {
            get
            {
                if (CurrentUserIsSurveyor)
                {
                    return false;
                }

                if (CurrentEncounter == null)
                {
                    return false;
                }

                var isSSRS = CurrentEncounter.FormKey != null &&
                             (DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey).PrintAsSSRS == 1 ||
                              DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey).PrintAsSSRS == 2);
                this.RaisePropertyChangedLambda(p => p.PrintCommandLabel);
                return isSSRS;
            }
        }

        public string PrintCommandLabel
        {
            get
            {
                if (CurrentEncounter == null)
                {
                    return "Print";
                }

                return (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
                    ? "Print"
                    : "Print Preview";
            }
        }

        public bool IsOffLine(string Message)
        {
            if (EntityManager.IsOnline)
            {
                return false;
            }

            NavigateCloseDialog d = new NavigateCloseDialog();
            if (d != null)
            {
                d.NoVisible = false;
                d.YesButton.Content = "OK";
                d.Title = "No network connectivity.";
                d.Width = double.NaN;
                d.Height = double.NaN;
                d.ErrorMessage = "No network connectivity.  " + Message;
                d.Show();
            }

            return true;
        }

        bool _CanContinue = true;

        public bool CanContinue
        {
            get
            {
                //#if DEBUG
                //                return true;
                //#else
                return _CanContinue;
                //#endif
            }
            set
            {
                _CanContinue = value;
                this.RaisePropertyChangedLambda(p => p.CanContinue);
            }
        }

        public bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
            set { this.RaisePropertyChangedLambda(p => p.IsDebug); }
        }

        string ___cachedURI { get; set; } //Use CachedURI!

        string CachedURI
        {
            get
            {
                if (NavigationService == null)
                {
                    return "/DynamicForm";
                }

                if (string.IsNullOrWhiteSpace(___cachedURI))
                {
                    ___cachedURI = NavigationService.CurrentSource.ToString();
                }

                if (string.IsNullOrWhiteSpace(___cachedURI))
                {
                    return "/DynamicForm";
                }

                return ___cachedURI;
            }
        }

        public bool IsBusy
        {
            get
            {
                var ret = (FormModel == null) ? false : FormModel.IsLoading;
                return ret;
            }
            set
            {
                if (FormModel != null)
                {
                    if (FormModel.IsLoading != value)
                    {
                        FormModel.IsLoading = value;
                    }

                    if (value)
                    {
                        if (NavigationService != null)
                        {
                            ScreenBusyEvent.Start();
                        }
                    }
                    else
                    {
                        if (NavigationService != null)
                        {
                            //First time in, uri = "/DynamicForm/23160/138783/5093/2227/145443"
                            //Navigate away from and then come back...uri = "/Virtuoso.Home.2;component/Views/DynamicForm.xaml?patient=23160&admission=138783&form=5093&service=2227&task=145443"
                            //NavigationService is reverting back to the mapped uri when navigate away and back:
                            //      Uri = "/DynamicForm/{patient}/{admission}/{form}/{service}/{task}"
                            //      MappedUri = "/Virtuoso.Home.2;component/Views/DynamicForm.xaml?patient={patient}&amp;admission={admission}&amp;form={form}&amp;service={service}&amp;task={task}" />
                            //To fix: cache it on first hit.
                            //if (string.IsNullOrWhiteSpace(this.CachedURI))
                            //    this.CachedURI = this.NavigationService.CurrentSource.ToString();
                            ScreenBusyEvent.Stop(CachedURI, EntityManager.IsOnline);
                        }
                    }
                }

                this.RaisePropertyChangedLambda(p => p.IsBusy);
            }
        }

        #endregion

        bool isSaving;
        bool isSavingReportData = false;

        bool AutoSaveAfterLoad;

        /* Set via bit in URI (Shell.xaml: DynamicFormReadOnly) extracted in OnNavigatedTo()
         *      Sets OkButtonVisibility = false
         *      Sets Protected bit on all questions = true
         *      Filters 'Addendum' from FormSections */
        public bool IsReadOnlyEncounter;
        public bool IsAttemptedEncounter;

        public override async System.Threading.Tasks.Task OnNavigatedTo(object param)
        {
            Log($"OnNavigatedTo: ParamsInitialized={ParamsInitialized}", "WS_TRACE");

            this.RaisePropertyChangedLambda(p => p.IsBusy);
            if (!ParamsInitialized)
            {
                foreach (var item in (Dictionary<string, string>)param)
                    try
                    {
                        switch (item.Key)
                        {
                            case "service":
                                Metrics.Ria.NullMetricLogger.Log(
                                    string.Format("[006] ServiceTypeKey.{0} = Convert.ToInt32(item.Value).{1}",
                                        ServiceTypeKey, Convert.ToInt32(item.Value)), CorrelationIDHelper,
                                    EntityManager.IsOnline, "DynamicFormViewModel.OnNavigatedTo", CachedURI);
                                ServiceTypeKey = Convert.ToInt32(item.Value);
                                break;
                            case "form":
                                FormKey = Convert.ToInt32(item.Value);
                                break;
                            case "patient":
                                PatientKey = Convert.ToInt32(item.Value);
                                break;
                            case "admission":
                                AdmissionKey = Convert.ToInt32(item.Value);
                                break;
                            case "task":
                                TaskKey = Convert.ToInt32(item.Value);
                                break;
                            case "readonly":
                                IsReadOnlyEncounter = (Convert.ToInt32(item.Value) == 1);
                                break;
                            case "attempted":
                                IsAttemptedEncounter = (Convert.ToInt32(item.Value) == 1);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Log($"OnNavigatedTo: exception={e.Message}", "WS_TRACE", TraceEventType.Error);
                        throw new Exception("DynamicFormViewModel.OnNavigatedTo:  No " + item.Key +
                                            " defined for this dynamic form");
                    }

                ParamsInitialized = true;

                AutoSaveAfterLoad = DynamicFormSipManager.Instance.GetAutoSave(TaskKey);

                Log($"OnNavigatedTo: AutoSaveAfterLoad={AutoSaveAfterLoad}", "WS_TRACE");

                if (FormModel != null)
                {
                    FormModel.CurrentFormKey = FormKey;
                    Log($"OnNavigatedTo: FormModel.CurrentFormKey={FormKey}", "WS_TRACE");
                }

                IsBusy = true;

                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                var offlineInfo = await DynamicFormSipManager.Instance.FormPersisted(TaskKey, OfflineStoreType.SAVE,
                    deleteFileWhenVersionDoesNotMatchAssembly: true);
                var loadFromSave = offlineInfo.FormPersisted;

                Log($"OnNavigatedTo: loadFromSave={loadFromSave}, IsReadOnlyEncounter={IsReadOnlyEncounter}",
                    "WS_TRACE");

                if (loadFromSave && IsReadOnlyEncounter)
                {
                    //override dashboard/read-only info if encounter have .SAVE data and was launched from the patient dashboard
                    IsReadOnlyEncounter = false;
                }

                if (loadFromSave) //If found in .SAVE - then LOAD from .SAVE, regardless of whether IsReadOnlyEncounter == TRUE
                {
                    IsReadOnlyEncounter =
                        false; //If opened from dashboard, allow loading from .SAVE, E.G. disable read only
                    await RestoreDynamicForm(TaskKey, OfflineStoreType.SAVE);
                }
                else
                {
                    if (EntityManager.IsOnline)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            //Never saved in process and we're online - go ahead and load the data from the server
                            // takeOffline = false because we want the query to not send wound photos - they will be lazy loaded on demand
                            GetData(PatientKey.GetValueOrDefault(), AdmissionKey, FormKey, TaskKey, takeOffline: false);
                        });
                    }
                    else
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(async () =>
                        {
                            // Never saved in process and have no network, better hope it was cached..
                            // However, if IsReadOnlyEncounter == TRUE, then encounter will be cached in .CACHE\"{0}-DB"
                            bool loadFromCache = false;

                            var offlineByTaskKey = await DynamicFormSipManager.Instance.FormPersisted(TaskKey,
                                OfflineStoreType.CACHE, deleteFileWhenVersionDoesNotMatchAssembly: true);
                            loadFromCache = offlineByTaskKey.FormPersisted;

                            if (loadFromCache)
                            {
                                await RestoreDynamicForm(TaskKey, OfflineStoreType.CACHE);
                            }
                            else if (IsReadOnlyEncounter)
                            {
                                // Load encounter from folder:
                                // C:\Users\<user>\AppData\Local\Delta Health Technologies\Crescendo\<tenant>\.Cache\<AdmissionKey>-DB
                                var dashboardPersisted =
                                    await DynamicFormSipManager.Instance.DashboardPersisted(AdmissionKey,
                                        OfflineStoreType.CACHE);
                                if (dashboardPersisted)
                                {
                                    await RestoreDynamicFormFromDashboard(AdmissionKey, TaskKey,
                                        OfflineStoreType.CACHE);
                                }
                                else
                                {
                                    PrintCannotOpenFormMessage();
                                }
                            }
                            else
                            {
                                PrintCannotOpenFormMessage();
                            }
                        });
                    }
                }
            }
        }

        void PrintCannotOpenFormMessage()
        {
            Log(
                "PrintCannotOpenFormMessage: Cannot open form, because there is no network connectivity and the form was never saved locally.",
                "WS_TRACE");

            //ERROR - somehow we launched a form when we had no network and it wasn't ever saved in process or cached...
            MessageBox.Show(
                "Cannot open form, because there is no network connectivity and the form was never saved locally.");
        }

        public void GetData(
            int patientKey,
            int admissionKey,
            int formKey,
            int taskKey,
            bool takeOffline,
            Dictionary<string, int?> param = null)
        {
            Log(
                $"GetData: patientKey={patientKey}, admissionKey={admissionKey}, formKey={formKey}, taskKey={taskKey}, takeOffline={takeOffline}",
                "WS_TRACE");

            if (param != null)
            {
                foreach (var item in param)
                    try
                    {
                        switch (item.Key)
                        {
                            case "service":
                                Metrics.Ria.NullMetricLogger.Log(
                                    string.Format("[007] ServiceTypeKey.{0} = item.Value.GetValueOrDefault().{1}",
                                        ServiceTypeKey, item.Value.GetValueOrDefault()), CorrelationIDHelper,
                                    EntityManager.IsOnline, "DynamicFormViewModel.OnNavigatedTo", CachedURI);
                                ServiceTypeKey = item.Value.GetValueOrDefault();
                                break;
                            case "form":
                                FormKey = item.Value.GetValueOrDefault();
                                break;
                            case "patient":
                                PatientKey = item.Value.GetValueOrDefault();
                                break;
                            case "admission":
                                AdmissionKey = item.Value.GetValueOrDefault();
                                break;
                            case "task":
                                TaskKey = item.Value.GetValueOrDefault();
                                break;
                        }
                    }
                    catch
                    {
                        throw new Exception("DynamicFormViewModel.GetData:  No " + item.Key +
                                            " defined for this dynamic form");
                    }
            }

            FormModel.GetAsyncByKeys(patientKey, admissionKey, formKey, taskKey, takeOffline);
        }

        private bool _FormLoadedFromDisk;

        public override void OnNavigatingFrom(ref bool cancel)
        {
            Log($"OnNavigatingFrom: cancel={cancel}, CanContinue={CanContinue}", "WS_TRACE");

            if (!CanContinue)
            {
                try
                {
                    if (NavigateKey != null && CurrentEncounter != null)
                    {
                        if (CurrentEncounter.EncounterKey <= 0)
                        {
                            NavigateKey.Mode = Constants.ADDING;
                        }
                        else
                        {
                            NavigateKey.Mode = Constants.EDITING;
                        }

                        if (CurrentEncounter != null)
                        {
                            NavigateKey.Key = CurrentEncounter.EncounterKey.ToString();
                            NavigateKey.SubKey = CurrentTask.TaskKey.ToString();
                        }

                        if (CurrentAdmission != null && CurrentAdmission.Patient != null)
                        {
                            NavigateKey.Title = CurrentAdmission.Patient.FullName;
                        }
                        else
                        {
                            NavigateKey.Title = "Unknown";
                        }

                        NavigateKey.IsChainable = false; //Why would you ever 'chain' the views?
                        NavigateKey.ApplicationSuite = GetDocumentDescription();
                        if (CurrentAdmission != null && CurrentAdmission.ServiceLine != null)
                        {
                            var sl = CurrentAdmission.ServiceLine != null
                                ? CurrentAdmission.ServiceLine
                                : ServiceLineCache.GetServiceLineFromKey(CurrentAdmission.ServiceLineKey);
                            if (sl != null)
                            {
                                NavigateKey.ServiceLine = sl.Name;
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            else
            {
                if (NavigateKey != null && CurrentEncounter != null)
                {
                    if (CurrentEncounter.EncounterKey <= 0)
                    {
                        NavigateKey.Mode = Constants.ADDING;
                    }
                    else
                    {
                        NavigateKey.Mode = Constants.EDITING;
                    }

                    if (CurrentEncounter != null)
                    {
                        NavigateKey.Key = CurrentEncounter.EncounterKey.ToString();
                        NavigateKey.SubKey = CurrentTask.TaskKey.ToString();
                    }

                    if (CurrentAdmission != null && CurrentAdmission.Patient != null)
                    {
                        NavigateKey.Title = CurrentAdmission.Patient.FullName;
                    }
                    else
                    {
                        NavigateKey.Title = "Unknown";
                    }

                    NavigateKey.IsChainable = false; //Why would you ever 'chain' the views?
                    NavigateKey.ApplicationSuite = GetDocumentDescription();
                    if (CurrentAdmission != null &&
                        (CurrentAdmission.ServiceLine != null || CurrentAdmission.ServiceLineKey >= 0))
                    {
                        var sl = CurrentAdmission.ServiceLine != null
                            ? CurrentAdmission.ServiceLine
                            : ServiceLineCache.GetServiceLineFromKey(CurrentAdmission.ServiceLineKey);
                        if (sl != null)
                        {
                            NavigateKey.ServiceLine = sl.Name;
                        }
                    }
                }
            }
        }

        bool _IsPopupVisible;

        public bool IsPopupVisible
        {
            get { return _IsPopupVisible; }
            set
            {
                _IsPopupVisible = value;
                this.RaisePropertyChangedLambda(p => p.IsPopupVisible);
            }
        }

        List<object> PopupDataContextStack = new List<object>();
        object _PopupDataContext;

        public object PopupDataContext
        {
            get { return _PopupDataContext; }
            set
            {
                if (value == null)
                {
                    if (PopupDataContextStack.Any())
                    {
                        _PopupDataContext = PopupDataContextStack.Last();
                        PopupDataContextStack.Remove(_PopupDataContext);
                    }
                    else
                    {
                        _PopupDataContext = null;
                    }

                    this.RaisePropertyChangedLambda(p => p.PopupDataContext);
                    IsPopupVisible = (_PopupDataContext == null) ? true : false;
                    IsPopupVisible = (_PopupDataContext == null) ? false : true;
                }
                else
                {
                    if (PopupDataContextStack.Any() && (PopupDataContextStack.Contains(value)))
                    {
                        PopupDataContextStack.Remove(value); // backing up a level
                    }
                    else
                    {
                        if (_PopupDataContext != null)
                        {
                            PopupDataContextStack.Add(_PopupDataContext); // going down a level
                        }
                    }

                    _PopupDataContext = value;
                    IsPopupVisible = false;
                    IsPopupVisible = true;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        this.RaisePropertyChangedLambda(p => p.PopupDataContext);
                    });
                }
            }
        }

        private HomePageAgencyOpsRefreshOptionEnum HomePageAgencyOpsRefreshOption =
            HomePageAgencyOpsRefreshOptionEnum.None;

        private void UpdateOrdersTracking()
        {
            UpdateOrdersTrackingCOTI();
            UpdateOrdersTrackingPOC();
            UpdateOrdersTrackingHospiceElectionAddendum();
        }

        private void UpdateOrdersTrackingCOTI()
        {
            if ((CurrentEncounter == null) || (CurrentEncounter.AdmissionCOTI == null))
            {
                return;
            }

            if ((CurrentEncounter.EncounterIsCOTI == false) ||
                (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed))
            {
                return;
            }

            // indicate POC as printed and set associated OrdersTracking row status as Sent
            AdmissionCOTI ac = CurrentEncounter.AdmissionCOTI.FirstOrDefault();
            if (ac == null)
            {
                return;
            }

            OrdersTrackingManager otm = new OrdersTrackingManager();
            otm.SetTrackingRowToSent(ac, (int)OrderTypesEnum.CoTI, ac.AdmissionCOTIKey);
            if (EntityManager.IsOnline)
            {
                IsBusy = true;
                HomePageAgencyOpsRefreshOption = HomePageAgencyOpsRefreshOptionEnum.None;
                FormModel.SaveMultiAsync(() => LogChangeSet("UpdateOrderTrackingCOTI"));
            }
        }

        private void UpdateOrdersTrackingPOC()
        {
            if ((CurrentEncounter == null) || (CurrentEncounter.EncounterPlanOfCare == null))
            {
                return;
            }

            if ((CurrentEncounter.EncounterIsPlanOfCare == false) ||
                (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed))
            {
                return;
            }

            // indicate POC as printed and set associated OrdersTracking row status as Sent
            EncounterPlanOfCare epc = CurrentEncounter.EncounterPlanOfCare.FirstOrDefault();
            if (epc == null)
            {
                return;
            }

            OrdersTrackingManager otm = new OrdersTrackingManager();
            otm.SetTrackingRowToSent(epc, (int)OrderTypesEnum.POC, epc.EncounterPlanOfCareKey);
            // also set all F2F records associated with this admission to be marked as sent, if necessary - and NOT already signed
            if ((epc.PrintF2FwithPOC == true) && (CurrentAdmission != null) &&
                (CurrentAdmission.HospiceAdmission == false) && (CurrentAdmission.OrdersTracking != null) &&
                CurrentAdmission.OrdersTracking.Any())
            {
                foreach (OrdersTracking ot in CurrentAdmission.OrdersTracking.Where(a =>
                             ((a.OrderType == (int)OrderTypesEnum.FaceToFace ||
                               a.OrderType == (int)OrderTypesEnum.FaceToFaceEncounter)) && (a.Inactive == false)))
                    // Calling this with an OrderKey = -1, this appears to need to be refactored -- OrderKey is not used in this Method or any dependent methods
                    if (ot.Status != (int)OrdersTrackingStatus.Signed)
                    {
                        otm.SetTrackingRowToSent(ot, ot.OrderType, -1);
                    }
            }

            if (EntityManager.IsOnline)
            {
                IsBusy = true;
                HomePageAgencyOpsRefreshOption = HomePageAgencyOpsRefreshOptionEnum.None;
                FormModel.SaveMultiAsync(() => LogChangeSet("UpdateOrderTrackingPOC"));
            }
        }

        private void UpdateOrdersTrackingHospiceElectionAddendum()
        {
            if ((CurrentEncounter == null) || (CurrentEncounter.EncounterHospiceElectionAddendum == null))
            {
                return;
            }

            if ((CurrentEncounter.EncounterIsHospiceElectionAddendum == false) ||
                (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed))
            {
                return;
            }

            // set associated OrdersTracking row status as Sent
            EncounterHospiceElectionAddendum eh = CurrentEncounter.EncounterHospiceElectionAddendum.FirstOrDefault();
            if (eh == null)
            {
                return;
            }

            if (eh.RequiresSignature == false)
            {
                return;
            }

            OrdersTrackingManager otm = new OrdersTrackingManager();
            otm.SetTrackingRowToSent(CurrentEncounter, (int)OrderTypesEnum.HospiceElectionAddendum,
                CurrentEncounter.EncounterKey);
            if (EntityManager.IsOnline)
            {
                IsBusy = true;
                HomePageAgencyOpsRefreshOption = HomePageAgencyOpsRefreshOptionEnum.None;
                FormModel.SaveMultiAsync(() => LogChangeSet("UpdateOrderTrackingHospiceElectionAddendum"));
            }
        }

        public async System.Threading.Tasks.Task SaveDynamicFormAutomated(OfflineStoreType location)
        {
            if (FormModel == null)
            {
                return;
            }

            Log($"SaveDynamicFormAutomated: location={location}", "WS_TRACE");

            var _currentPatient = FormModel.CurrentPatient;
            var _currentTask = FormModel.CurrentTask;
            
            var _currentEncounter = _currentPatient.Encounter.Where(p => p.TaskKey == TaskKey).FirstOrDefault();

            if (_currentTask != null && _currentEncounter == null)
            {
                _currentEncounter = _currentPatient.Encounter.Where(e => e.TaskKey == _currentTask.TaskKey)
                    .FirstOrDefault();
            }

            if (_currentEncounter == null)
            {
                _currentEncounter = _currentPatient.Encounter.Where(e => e.EncounterKey < 0).FirstOrDefault();
            }

            CurrentPatient = _currentPatient;
            CurrentEncounter = _currentEncounter;
            CurrentTask = _currentTask;
            await SaveDynamicForm(
                (_currentPatient != null) ? _currentPatient.PatientKey : -1,
                (_currentEncounter != null) ? _currentEncounter.EncounterKey : -1,
                (_currentEncounter != null) ? _currentEncounter.EncounterStatus : (int)EncounterStatusType.Edit,
                location,
                false);
        }

        private async System.Threading.Tasks.Task SaveDynamicForm(int PatientKey, int EncounterKey, int EncounterStatus,
            OfflineStoreType location, bool isAutoSave)
        {
            Log(
                $"SaveDynamicForm: PatientKey={PatientKey}, EncounterKey={EncounterKey}, EncounterStatus={EncounterStatus}, location={location}, isAutoSave={isAutoSave}",
                "WS_TRACE");

            if (IsReadOnlyEncounter)
            {
                return;
            }

            if (CurrentAdmission != null) //when called from the Task cache dialog - will have no CurrentAdmission
            {
                CurrentAdmission.InDynamicForm = false;
            }

            //NOTE: ProcessFormSections might navigateback/close/cleanup - which sets FormModel to NULL, had this happen for a task
            if (FormModel != null)
            {
                DynamicFormQuestionState quesDictionary = new DynamicFormQuestionState();
                foreach (var ques in QuestionMasterList.ToList())
                {
                    var castQuestion = ques;
                    if (castQuestion.QuestionID.HasValue)
                    {
                        Dictionary<string, string> props = null;
                        int key = key = castQuestion.QuestionID.Value;
                        props = castQuestion.SaveOfflineState();
                        if (!quesDictionary.SavedQuestions.ContainsKey(key))
                        {
                            quesDictionary.SavedQuestions.Add(key, props);
                        }
                    }
#if DEBUG
                    // When in DEBUG display to developer questions that will not be saved to disk for offline
                    if (castQuestion.QuestionID.HasValue == false)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "\n------------------------------------------------------------------------------------------------------------");

                        if (castQuestion.Question != null)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "Question missing ID/KEY and will not be saved for offline.  QuestionKey: {0} \t Label: {1} \t DataTemplate: {2} \t BackingFactory: {3}",
                                castQuestion.Question.QuestionKey, castQuestion.Question.Label,
                                castQuestion.Question.DataTemplate, castQuestion.Question.BackingFactory);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "Question missing ID/KEY and will not be saved for offline.  Question is NULL",
                                castQuestion.ToString());
                        }

                        //Some identifying attributes for determining whether this is something that needs looked at more closely
                        System.Diagnostics.Debug.WriteLine("\t QuestionID: {0}", castQuestion.QuestionID);
                        System.Diagnostics.Debug.WriteLine("\t Label: {0}", castQuestion.Label);
                        System.Diagnostics.Debug.WriteLine("\t IsSectionLabelQuestion: {0}",
                            castQuestion.IsSectionLabelQuestion);
                        System.Diagnostics.Debug.WriteLine("\t IsClonedQuestion: {0}", castQuestion.IsClonedQuestion);
                        System.Diagnostics.Debug.WriteLine("\t IsHospiceAdmission: {0}",
                            castQuestion.IsHospiceAdmission);
                        System.Diagnostics.Debug.WriteLine("\t IsHospiceServiceLine: {0}",
                            castQuestion.IsHospiceServiceLine);
                        System.Diagnostics.Debug.WriteLine("\t IsNewEncounter: {0}", castQuestion.IsNewEncounter);
                        System.Diagnostics.Debug.WriteLine("\t IsNewEncounterOrSection: {0}",
                            castQuestion.IsNewEncounterOrSection);

                        System.Diagnostics.Debug.WriteLine("\t Hidden: {0}", castQuestion.Hidden);
                        System.Diagnostics.Debug.WriteLine("\t HiddenOverride: {0}", castQuestion.HiddenOverride);
                        System.Diagnostics.Debug.WriteLine("\t HideSectionOverride: {0}",
                            castQuestion.HideSectionOverride);
                        System.Diagnostics.Debug.WriteLine("\t Required: {0}", castQuestion.Required);

                        if (castQuestion.Section != null)
                        {
                            System.Diagnostics.Debug.WriteLine("\n\t\t Section.Label: {0}", castQuestion.Section.Label);
                            System.Diagnostics.Debug.WriteLine("\t\t Section.SectionKey: {0}",
                                castQuestion.Section.SectionKey);
                            System.Diagnostics.Debug.WriteLine("\t\t Section.IsOasisSection): {0}",
                                castQuestion.Section.IsOasisSection);
                            System.Diagnostics.Debug.WriteLine("\t\t Section.OasisLabel: {0}",
                                castQuestion.Section.OasisLabel);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("\n\t\t This question has no Section object...");
                        }

                        System.Diagnostics.Debug.WriteLine("\n");
                    }
#endif
                }

                var _currTask = FormModel.CurrentTask;
                var formInfo = new DynamicFormInfo
                {
                    TaskKey = _currTask.TaskKey,

                    //NOTE: if end user changed the Service Type on the Service Type question in Section: Signature - then this.ServiceTypeKey will not match ServiceTypeKey on the Encounter.
                    //      this.ServiceTypeKey is the value passed to DynamicForm
                    ServiceTypeKey =
                        (CurrentEncounter.ServiceTypeKey.GetValueOrDefault() != ServiceTypeKey &&
                         CurrentEncounter.ServiceTypeKey.GetValueOrDefault() != 0)
                            ? CurrentEncounter.ServiceTypeKey.GetValueOrDefault()
                            : ServiceTypeKey,
                    AdmissionKey = AdmissionKey,
                    FormKey = FormKey,
                    LastLocalSaveDate = null,
                    PatientKey = PatientKey,
                    EncounterKey = EncounterKey,
                    EncounterStatus = EncounterStatus,
                    PreviousEncounterStatus = PreviousEncounterStatus,
                    PatientName = (CurrentPatient == null) ? string.Empty : CurrentPatient.FullNameInformal,
                    AddendumText = (Addendum == null) ? string.Empty : ((Addendum)Addendum).AddendumText,
                    SavedQuestionState = quesDictionary
                };

#if DEBUG
                Stopwatch timer = Stopwatch.StartNew();
#endif
                var _taskKey = FormModel.CurrentTask.TaskKey;
                await DynamicFormSipManager.Instance.Save(_taskKey, location, formInfo);
                await FormModel.Save(_taskKey, location);

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    lastSaveDateTime = formInfo.LastLocalSaveDate;
                    RefreshSaveMessage();
                });
#if DEBUG
                timer.Stop();
                var seconds_timer = timer.ElapsedMilliseconds / 1000D;
                System.Diagnostics.Debug.WriteLine(string.Format(
                    "DynamicFormViewModel.SaveDynamicForm().  Elapsed seconds: {0}", seconds_timer.ToString("0.##")));
#endif
            }
        }

        public int? GetAttemptedServiceTypeKey(Encounter e)
        {
            // nice - it won't be attached until the first OK
            if ((e == null) || (e.EncounterAttempted == null))
            {
                return null;
            }

            EncounterAttempted ea = e.EncounterAttempted.FirstOrDefault();
            if (ea == null)
            {
                return null;
            }

            return ea.ServiceTypeKey;
        }

        //NOTE: this function returns EncounterKey
        private async System.Threading.Tasks.Task RestoreDynamicForm(int taskKey, OfflineStoreType location)
        {
            Log($"RestoreDynamicForm: taskKey={taskKey}, location={location}", "WS_TRACE");

#if DEBUG
            Stopwatch timer = Stopwatch.StartNew();
#endif
            var info = await DynamicFormSipManager.Instance.GetDynamicInfo(taskKey, location,
                deleteFileWhenVersionDoesNotMatchAssembly: true);

            if (info != null)
            {
                OfflineAddendumText = info.AddendumText;
                dynamicFormState = info;

                await FormModel.Load(taskKey, location);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    lastSaveDateTime = info.LastLocalSaveDate;
                    RefreshSaveMessage();
                });
                PreviousEncounterStatus = info.PreviousEncounterStatus > 1 ? info.PreviousEncounterStatus : 1;
            }
            else
            {
                PreviousEncounterStatus = 1;
            }

            _FormLoadedFromDisk = true;
#if DEBUG
            timer.Stop();
            var seconds_timer = timer.ElapsedMilliseconds / 1000D;
            Log($"RestoreDynamicForm: Elapsed seconds={seconds_timer.ToString("0.##")}", "WS_TRACE");
            System.Diagnostics.Debug.WriteLine(string.Format("DynamicFormViewModel.RestoreDynamicForm().  Elapsed seconds: {0}", seconds_timer.ToString("0.##")));
#endif
        }

        private async System.Threading.Tasks.Task RestoreDynamicFormFromDashboard(int admissionKey, int taskKey, OfflineStoreType location)
        {
            Log($"RestoreDynamicFormFromDashboard: admissionKey={admissionKey}, taskKey={taskKey}, location ={location}", "WS_TRACE");

#if DEBUG
            Stopwatch timer = Stopwatch.StartNew();
#endif
            await FormModel.LoadFromDashboard(admissionKey, taskKey, location);

            _FormLoadedFromDisk = true;
#if DEBUG
            timer.Stop();
            var seconds_timer = timer.ElapsedMilliseconds / 1000D;
            System.Diagnostics.Debug.WriteLine(string.Format("DynamicFormViewModel.RestoreDynamicFormFromDashboard().  Elapsed seconds: {0}", seconds_timer.ToString("0.##")));
#endif
        }

        async System.Threading.Tasks.Task DeleteDynamicForm(OfflineStoreType location)
        {
            Log($"DeleteDynamicForm: location ={location}", "WS_TRACE", TraceEventType.Warning);

            await DynamicFormSipManager.Instance.RemoveFromDisk(TaskKey, location);
        }

        private NavigateCloseDialog CreateDialogue(String Title, String Msg)
        {
            NavigateCloseDialog d = new NavigateCloseDialog
            {
                Width = double.NaN,
                Height = double.NaN,
                ErrorMessage = Msg,
                ErrorQuestion = null,
                Title = Title, //Title property displays in large black text
                HasCloseButton = false
            };
            return d;
        }

        private void ShowToastNotifications()
        {
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    var infoMessage = await ApplicationMessaging.GetApplicationInfoMessage();
                    if (string.IsNullOrEmpty(infoMessage) == false)
                    {
                        Client.Core.UIThread.Invoke(() => Client.Services.NotificationService.ShowToast(infoMessage));
                    }
                }
                catch (Exception ex)
                {
                    Log(string.Format("[ShowToastNotifications] Exception: {0}", ex.ToString()), string.Empty,
                        TraceEventType.Error);
                }
            });
        }

        private void Log(string message, string subCategory = "",
            TraceEventType traceEventType = TraceEventType.Information)
        {
            string category = "DynamicFormViewModel";

            var __category = string.IsNullOrEmpty(subCategory)
                ? category
                : string.Format("{0}-{1}", category, subCategory);
            logWriter.Write(message,
                new[] { __category }, //category
                0, //priority
                0, //eventid
                traceEventType);
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                "----------------------------------------------------------------------------------------------------------------------------");
            System.Diagnostics.Debug.WriteLine("Dynamic Form View Model:");
            System.Diagnostics.Debug.WriteLine(message);
            System.Diagnostics.Debug.WriteLine(
                "----------------------------------------------------------------------------------------------------------------------------");
#endif
        }

        public RelayCommand<EncounterData> OnCallLoaded { get; set; }

        private void LogChangeSet(string context)
        {
            Metrics.Ria.ChangeSetLogger.Log(
                CorrelationIDHelper,
                FormModel.GetContext(),
                new List<string> { "Task", "Encounter" }, //or null to log every entity type
                EntityManager.IsOnline,
                string.Format("DynamicFormViewModel.{0}", context),
                CachedURI);
        }
    }
}