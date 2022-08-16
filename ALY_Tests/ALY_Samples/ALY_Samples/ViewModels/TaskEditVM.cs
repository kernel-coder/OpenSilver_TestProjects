#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;

#endregion

namespace Virtuoso.Home.V2.ViewModel
{
    public class MyTaskContext : OpenRiaServices.DomainServices.Client.DomainContext
    {
        public MyTaskContext(DomainClient domainClient) : base(domainClient)
        {
        }

        protected override EntityContainer CreateEntityContainer()
        {
            throw new NotImplementedException();
        }
    }
    //[PartCreationPolicy(CreationPolicy.NonShared)]
    //[Export]
    public class TaskEditVM : EntityBase
    {
        //private Metrics.CorrelationIDHelper CorrelationID { get; set; }
        //VirtuosoDomainContext TaskContext = new VirtuosoDomainContext();
        MyTaskContext TaskContext = new MyTaskContext(null);
        bool __TaskVMIsOnlineBackingField;

        bool TaskVMIsOnline
        {
            get { return __TaskVMIsOnlineBackingField; }
            set
            {
                __TaskVMIsOnlineBackingField = value;
                this.RaisePropertyChangedLambda(p => p.TaskVMIsOnline);
            }
        }

        CommandManager CommandManager { get; set; }
        //ILogger Logger { get; set; }
        string LogCategory { get; set; }

        //[ImportingConstructor]
        public TaskEditVM()
        {
            //CorrelationID = new Metrics.CorrelationIDHelper();
            IsBusy = true;
           // Logger = logger;
            LogCategory = GetType().ToString();

            SetupCommands();

            CommandManager = new CommandManager(this);

            TaskVMIsOnline = true; // EntityManager.Current.IsOnline;

            //Messenger.Default.Register<bool>(this, Constants.Messaging.NetworkAvailability,
            //    IsAvailable =>
            //    {
            //        try
            //        {
            //            TaskVMIsOnline = IsAvailable;
            //            CommandManager.RaiseCanExecuteChanged();
            //        }
            //        catch (Exception)
            //        {
            //        }
            //    });

            var contextServiceProvider = new SimpleServiceProvider();

            TaskContext.ValidationContext = new ValidationContext(this, contextServiceProvider, null);
        }

        private int? InitialPatientKey;
        private int? InitialAdmissionKey;
        private bool isLoading = true;

        public void InitTask(int _taskKey, int? _patientKey, int? _admissionKey, int? _svcKey, bool _deleteTask = false,
            bool _overrideTaskInfo = false)
        {
            InitialPatientKey = _patientKey;
            InitialAdmissionKey = _admissionKey;
            //TaskContext.Tasks.Clear();

            DeletingTask = _deleteTask;
            OverrideTaskInfo = _overrideTaskInfo;

            GetTask(_taskKey, _patientKey, _admissionKey, _svcKey);
        }

        void GetTask(int taskKey, int? _patientKey, int? _admissionKey, int? _svcKey)
        {

        }

        void SetupCommands()
        {
            SaveTaskCommand = new RelayCommand(() =>
            {
               // if (ValidateTask())
                {
                   // SaveTask();
                }
            }, () =>
            {
                if (TaskVMIsOnline == false)
                {
                    return false;
                }

                return HasErrors == false;
            });

           /// CloseTaskCommand = new RelayCommand(() => { CloseDialog(false); });
            //SetupTaskCommentCommands();
        }

        public int EmptyPatientKey { get; set; }
        private bool IsScheduler => false; // (RoleAccessHelper.CheckPermission(RoleAccess.TaskScheduler, false));

        private bool IsHospiceElectionAddendum => false;
        //(RoleAccessHelper.CheckPermission(RoleAccess.HospiceElectionAddendum, false));

        private bool IsSchedulerOrOpsManager => false;
        // (IsScheduler || RoleAccessHelper.CheckPermission(RoleAccess.Ops, false));

        private bool IsSystemAdministrator => false;// (RoleAccessHelper.CheckPermission(RoleAccess.Admin, false));

        private bool IsPatientSelected(int? patientKey)
        {
            bool patientSelected = true;
            if (patientKey == null || patientKey.GetValueOrDefault() <= 0 ||
                patientKey.GetValueOrDefault() == EmptyPatientKey)
            {
                patientSelected = false;
            }

            return patientSelected;
        }

        private int? PatientKeyValue(int? selectedPatientKey)
        {
            int? patientKey = selectedPatientKey;

            if (!IsPatientSelected(patientKey))
            {
                patientKey = EmptyPatientKey;
            }

            return patientKey;
        }

        //private void OnTaskLoaded(LoadOperation<Task> operation)
        //{
        //    IsBusy = false;
        //    CurrentTask = TaskContext.Tasks.FirstOrDefault() ?? CurrentTask;

        //    __PatientKeyBackingField = CurrentTask.PatientKey;
        //    __AdmissionKeyBackingField = CurrentTask.AdmissionKey;
        //    if (IsPatientSelected(CurrentTask.PatientKey))
        //    {
        //        __PatientKeyBackingField = CurrentTask.PatientKey;
        //    }

        //    if (CurrentTask.NonServiceTypeKey.HasValue && CurrentTask.NonServiceTypeKey.Value != 0)
        //    {
        //        __LookupKeyBackingField = String.Format("{0}-ACTIVITY", CurrentTask.NonServiceTypeKey);
        //    }
        //    else if (CurrentTask.ServiceTypeKey.HasValue && CurrentTask.ServiceTypeKey.Value != 0)
        //    {
        //        __LookupKeyBackingField = String.Format("{0}-SERVICE", CurrentTask.ServiceTypeKey);
        //    }
        //    else
        //    {
        //        __LookupKeyBackingField = "";
        //    }

        //    UserID = CurrentTask.UserID;
        //    TaskStartDate = CurrentTask.TaskStartDateTime; //init date/time immediately - looks w
        //    TaskEndDate = CurrentTask.TaskEndDateTime;
        //    LoadComments(CurrentTask.Patient, true);
        //    // Include only if the user is a system administrator and the Patient has been selected)
        //    bool includesDischarged = IsSystemAdministrator && IsPatientSelected(__PatientKeyBackingField);
        //    int? searchPatientKey = (IsPatientSelected(PatientKey)) ? PatientKey : null;
        //    var qry = TaskContext.GetPatientsForTaskCreationTPQuery(!IsSchedulerOrOpsManager, includesDischarged,
        //        searchPatientKey,
        //        ((CurrentTask == null) ? 0 : ((CurrentTask.AdmissionKey == null) ? 0 : (int)CurrentTask.AdmissionKey)));
        //    IsBusy = true;
        //    TaskContext.Load(qry, OnPatientsLoaded, null);
        //}

        //private void OnPatientsLoaded(LoadOperation<TaskPatient> operation)
        //{
        //    try
        //    {
        //        isLoading = false;

        //        SetupFilteredPatientCollection();
        //        SetupFilteredLookupCollection();
        //        SetupFilteredUserCollection();

        //        SetupKeys();

        //        SetSelectedAdmission();

        //        FilterAvailableUsers();
        //        FilterAvailableTaskList();

        //        this.RaisePropertyChangedLambda(p => p.LookupKey);
        //        this.RaisePropertyChangedLambda(p => p.IsNonService);
        //        this.RaisePropertyChangedLambda(p => p.MileageIsVisible);
        //        if ((MileageIsVisible) && (CurrentTask != null) && (CurrentTask.DistanceScale == null))
        //        {
        //            CurrentTask.DistanceScale = TenantSettingsCache.Current.TenantSettingDistanceTraveledMeasure;
        //        }

        //        if (IsPatientSelected(InitialPatientKey))
        //        {
        //            List<TaskPatient> patList = TaskContext.TaskPatients.ToList();
        //            TaskPatient tp = patList.Where(p => p.PatientKey == (int)InitialPatientKey).FirstOrDefault();
        //            if ((tp != null) && (tp.PatientKey > 0))
        //            {
        //                PatientKey = tp.PatientKey;
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        IsBusy = false;
        //    }
        //}

        //private void SetupFilteredPatientCollection()
        //{
        //    List<TaskPatient> patList = TaskContext.TaskPatients.ToList();
        //    EmptyPatientKey = 0;

        //    if (!IsPatientSelected(PatientKey))
        //    {
        //        patList.Insert(0, new TaskPatient { PatientKey = EmptyPatientKey, FullNameWithMRN = "All" });
        //        FilteredPatientCollection = patList;
        //    }
        //    else
        //    {
        //        FilteredPatientCollection = patList.Where(p => p.PatientKey == PatientKey).ToList();
        //    }
        //}

        private bool _EncounterStarted;

        public bool EncounterStarted
        {
            get { return _EncounterStarted; }
            set
            {
                _EncounterStarted = value;
                this.RaisePropertyChangedLambda(p => p.EncounterStarted);
            }
        }

        private bool SetupingUpKeys = true;

        //private void SetupKeys()
        //{
        //    if (CurrentTask.PatientKey.GetValueOrDefault() > 0)
        //    {
        //        PatientKey = CurrentTask.PatientKey;
        //        if (TaskContext.TaskPatients != null && TaskContext.TaskPatients.Any() == false)
        //        {
        //            PatientKey = 0;
        //        }

        //        AdmissionKey = CurrentTask.AdmissionKey;

        //        //setting the AdmissionKey will clear LookupKey and add error 'Activity is required'
        //        //clearing the error here.  NOTE: bottom of this function initializes LookupKey backing field
        //        //hate this 'hack/fix', but a better would be to refactor this UI and VM completely
        //        ClearErrorFromProperty("LookupKey");
        //    }

        //    CancelReasonKey = CurrentTask.CancelReasonKey;
        //    Notes = CurrentTask.Notes;
        //    UserID = CurrentTask.UserID;
        //    // Setup private _TaskStartDate and _TaskEndDate so we do not trigger TaskDuration  calculation
        //    _TaskStartDate = CurrentTask.TaskStartDateTime;
        //    this.RaisePropertyChangedLambda(p => p.TaskStartDate);
        //    _TaskEndDate = CurrentTask.TaskEndDateTime;
        //    this.RaisePropertyChangedLambda(p => p.TaskEndDate);
        //    TaskDuration = CurrentTask.TaskDuration;

        //    if (CurrentTask.PatientKey.GetValueOrDefault() > 0)
        //    {
        //        TaskPatient patient = null;
        //        if (IsPatientSelected(PatientKey))
        //        {
        //            patient = TaskContext.TaskPatients
        //                .Where(p => p.PatientKey == CurrentTask.PatientKey)
        //                .FirstOrDefault();
        //        }

        //        if (patient != null)
        //        {
        //            var encounterTaskStarted = ((TaskContext == null) || (TaskContext.TaskEncounters == null))
        //                ? null
        //                : TaskContext.TaskEncounters
        //                    .Where(ec => ec.TaskKey != null && ec.TaskKey == CurrentTask.TaskKey).FirstOrDefault();
        //            if (encounterTaskStarted != null)
        //            {
        //                EncounterStarted = true;
        //            }
        //            else
        //            {
        //                EncounterStarted = false;
        //            }
        //        }
        //        else
        //        {
        //            EncounterStarted = false;

        //            if (CurrentTask.IsNew && !OverrideTaskInfo)
        //            {
        //                PatientKey = EmptyPatientKey;
        //            }
        //        }
        //    }

        //    if (CurrentTask.NonServiceTypeKey.HasValue && CurrentTask.NonServiceTypeKey.Value != 0)
        //    {
        //        __LookupKeyBackingField = String.Format("{0}-ACTIVITY", CurrentTask.NonServiceTypeKey);
        //    }
        //    else if (CurrentTask.ServiceTypeKey.HasValue && CurrentTask.ServiceTypeKey.Value != 0)
        //    {
        //        __LookupKeyBackingField = String.Format("{0}-SERVICE", CurrentTask.ServiceTypeKey);
        //    }
        //    else
        //    {
        //        __LookupKeyBackingField = "";
        //    }

        //    SetupingUpKeys = false;
        //    if ((CurrentTask.TaskKey < 0) && (CurrentTask.PatientKey != null))
        //    {
        //        GetPatientComments(CurrentTask.PatientKey); // Create task from alerts - load patient comments
        //    }
        //}

        //private int[] CurrentAdmissionGroups
        //{
        //    get
        //    {
        //        if (SelectedAdmission == null)
        //        {
        //            return new int[0];
        //        }

        //        var ret = SelectedAdmission.AdmissionGroup.Where(p =>
        //                ((p.StartDate.HasValue && ((DateTime)p.StartDate).Date <= TaskStartDate.Date) ||
        //                 !p.StartDate.HasValue) &&
        //                ((p.EndDate.HasValue && ((DateTime)p.EndDate).Date >= TaskStartDate.Date) ||
        //                 !p.EndDate.HasValue))
        //            .Select(ag => ag.ServiceLineGroupingKey).ToArray();
        //        return ret;
        //    }
        //}

        //public void SetupFilteredLookupCollection()
        //{
        //    var _clPM = (from c in ServiceTypeCache.GetActiveServiceTypes().Where(s => s.FormKey != null)
        //                 select new LookupPM
        //                 {
        //                     LookupKey = String.Format("{0}-SERVICE",
        //                         c.ServiceTypeKey), //keys across different tables will not be unique
        //                     CodeType = "SERVICE",
        //                     DatabaseKey = c.ServiceTypeKey,
        //                     ServiceIsAttempted = TaskSchedulingHelper.IsAttempted(c.ServiceTypeKey),
        //                     ServiceIsHIS = TaskSchedulingHelper.IsHIS(c.ServiceTypeKey),
        //                     ServiceIsOASIS = TaskSchedulingHelper.IsOASIS(c.ServiceTypeKey),
        //                     ServiceIsOrderEntry = TaskSchedulingHelper.IsOrderEntry(c.ServiceTypeKey),
        //                     ServiceIsPreEval = TaskSchedulingHelper.IsPreEval(c.ServiceTypeKey),
        //                     ServiceIsEval = TaskSchedulingHelper.IsEval(c.ServiceTypeKey),
        //                     ServiceIsResumption = TaskSchedulingHelper.IsResumption(c.ServiceTypeKey),
        //                     ServiceIsDischarge = TaskSchedulingHelper.IsDischarge(c.ServiceTypeKey),
        //                     ServiceIsTransfer = TaskSchedulingHelper.IsTransfer(c.ServiceTypeKey),
        //                     ServiceIsCOTI = TaskSchedulingHelper.IsCOTI(c.ServiceTypeKey),
        //                     ServiceIsVerbalCOTI = TaskSchedulingHelper.IsVerbalCOTI(c.ServiceTypeKey),
        //                     ServiceIsHospiceF2F = TaskSchedulingHelper.IsHospiceF2F(c.ServiceTypeKey),
        //                     ServiceIsHospicePhysicianEncounter =
        //                         TaskSchedulingHelper.IsHospicePhysicianEncounter(c.ServiceTypeKey),
        //                     ServiceIsHospiceElectionAddendum = TaskSchedulingHelper.IsHospiceElectionAddendum(c.ServiceTypeKey),
        //                     ServiceIsPlanOfCare = TaskSchedulingHelper.IsPlanOfCare(c.ServiceTypeKey),
        //                     ServiceIsTeamMeeting = TaskSchedulingHelper.IsTeamMeeting(c.ServiceTypeKey),
        //                     ServiceIsVisit = TaskSchedulingHelper.IsVisit(c.ServiceTypeKey),
        //                     ServiceIsFinancialUseOnly = TaskSchedulingHelper.IsFinancialUseOnly(c.ServiceTypeKey),
        //                     ServiceIsSchedulable = TaskSchedulingHelper.IsSchedulable(c.ServiceTypeKey),
        //                     Description = c.Description,
        //                     Duration = c.BaseTime.HasValue ? c.BaseTime.Value : 0,
        //                     DisciplineKey = c.DisciplineKey,
        //                     HCFACode = DisciplineCache.GetHCFACodeFromKey(c.DisciplineKey)
        //                 }).ToList();
        //    var _l = (from nst in NonServiceTypeCache.GetNonServiceTypesActive()
        //              select new LookupPM
        //              {
        //                  LookupKey = String.Format("{0}-ACTIVITY", nst.NonServiceTypeKey), //keys across different tables will not be unique
        //                  CodeType = "ACTIVITY",
        //                  DatabaseKey = nst.NonServiceTypeKey,
        //                  ServiceIsAttempted = false,
        //                  ServiceIsHIS = false,
        //                  ServiceIsOASIS = false,
        //                  ServiceIsOrderEntry = false,
        //                  ServiceIsPreEval = false,
        //                  ServiceIsEval = false,
        //                  ServiceIsResumption = false,
        //                  ServiceIsDischarge = false,
        //                  ServiceIsFinancialUseOnly = false,
        //                  ServiceIsSchedulable = true,
        //                  Description = nst.Description,
        //                  Duration = 0
        //              }).ToList();
        //    _clPM.AddRange(_l);

        //    var _x = _clPM.ToList();
        //    FilteredLookupCollection.Source = _x;

        //    if (FilteredLookupCollection != null)
        //    {
        //        FilteredLookupCollection.Filter += (s, e) =>
        //        {
        //            LookupPM item = e.Item as LookupPM;
        //            if ((PatientKey.HasValue) && (PatientKey > 0)) //a patient is selected
        //            {
        //                if (item.CodeType.Equals("SERVICE")) //patient selected - only allow patient activities
        //                {
        //                    // you must select an admission.
        //                    if (AdmissionKey <= 0 || SelectedAdmission == null)
        //                    {
        //                        e.Accepted = false;
        //                        Logger.Info(LogCategory,
        //                            string.Format("TaskFilter: Flunk {0} - no admission selected", item.Description));
        //                        return;
        //                    }

        //                    if (CurrentTask != null && CurrentTask.ServiceTypeKey.Equals(item.DatabaseKey) &&
        //                        CurrentTask.AdmissionKey.Equals(AdmissionKey))
        //                    {
        //                        e.Accepted = true;
        //                        Logger.Info(LogCategory,
        //                            string.Format("TaskFilter: Pass {0} - Existing Task edit", item.Description));
        //                        return;
        //                    }

        //                    if (UserCanVisitAdmission == false)
        //                    {
        //                        e.Accepted = false;
        //                        Logger.Info(LogCategory,
        //                            string.Format(
        //                                "TaskFilter: Flunk {0} - UserProfileCanVisit for AdmissionGroupLeaf failed",
        //                                item.Description));
        //                        return;
        //                    }

        //                    e.Accepted = true;
        //                    Logger.Info(LogCategory,
        //                        string.Format(
        //                            "TaskFilter: Pass {0} {1} - assume pass unless overwritten in later checks",
        //                            item.DisciplineKey, item.Description));

        //                    // If using ServiceTypeDependency and this ServiceType not in list - flunk it
        //                    if ((ServiceTypeDependencyKeys != null) &&
        //                        (ServiceTypeDependencyKeys.Contains(item.DatabaseKey) == false))
        //                    {
        //                        e.Accepted = false;

        //                        //TaskFilter: Flunk Certification of Terminal Illness - ServiceTypeDependency check failed
        //                        Logger.Info(LogCategory,
        //                            string.Format("TaskFilter: Flunk {0} - ServiceTypeDependency check failed",
        //                                item.Description));

        //                        return;
        //                    }

        //                    var patient = TaskContext.TaskPatients.Where(p => p.PatientKey == PatientKey)
        //                        .FirstOrDefault();
        //                    if (patient == null) // patient wasn't loaded by the query for some reason.
        //                    {
        //                        e.Accepted = false;
        //                        Logger.Info(LogCategory,
        //                            string.Format("TaskFilter: Flunk {0} - TaskPatient selection failed",
        //                                item.Description));
        //                        return;
        //                    }

        //                    var CurrentAdmission = SelectedAdmission;

        //                    var CurrentAdmissionDiscipline = CurrentAdmission.AdmissionDiscipline
        //                        .Where(p => !p.NotTakenDateTime.HasValue && p.DisciplineKey == item.DisciplineKey)
        //                        .OrderByDescending(a => a.ReferDateTime).FirstOrDefault();

        //                    //no open discipline record for this item no need to continue
        //                    if (CurrentAdmissionDiscipline == null)
        //                    {
        //                        e.Accepted = false;
        //                        Logger.Info(LogCategory,
        //                            string.Format("TaskFilter: Flunk {0} {1} - Open AdmissionDiscipline check failed",
        //                                item.DisciplineKey, item.Description));
        //                        return;
        //                    }

        //                    // Filter out Attempted Service Types
        //                    if (item.ServiceIsAttempted)
        //                    {
        //                        e.Accepted = false;
        //                        Logger.Info(LogCategory,
        //                            string.Format("TaskFilter: Flunk {0} - TaskIsAttempted", item.Description));
        //                        return;
        //                    }

        //                    // Filter out OASIS Service Types
        //                    if (item.ServiceIsOASIS)
        //                    {
        //                        e.Accepted = false;
        //                        Logger.Info(LogCategory,
        //                            string.Format("TaskFilter: Flunk {0} - TaskIsOASIS", item.Description));
        //                        return;
        //                    }

        //                    // Filter out HIS Service Types
        //                    if (item.ServiceIsHIS)
        //                    {
        //                        e.Accepted = false;
        //                        Logger.Info(LogCategory,
        //                            string.Format("TaskFilter: Flunk {0} - TaskIsHIS", item.Description));
        //                        return;
        //                    }

        //                    // Filter out POC Service Types
        //                    if (item.ServiceIsPlanOfCare)
        //                    {
        //                        if ((CurrentAdmission != null) &&
        //                            (CurrentAdmission.InsuranceRequiresDisciplineOrders == false))
        //                        {
        //                            e.Accepted = false;
        //                            Logger.Info(LogCategory,
        //                                string.Format(
        //                                    "TaskFilter: Flunk {0} - TaskIsPlanOfCare and Admission HIB Insurance does not requires Discipline Orders",
        //                                    item.Description));
        //                            return;
        //                        }
        //                    }

        //                    // Filter out Is Financial Use Only Types
        //                    if (item.ServiceIsFinancialUseOnly)
        //                    {
        //                        e.Accepted = false;
        //                        Logger.Info(LogCategory,
        //                            string.Format("TaskFilter: Flunk {0} - IsFinancialUseOnly", item.Description));
        //                        return;
        //                    }

        //                    // Filter out OrderEntry Service Types
        //                    if (item.ServiceIsOrderEntry)
        //                    {
        //                        e.Accepted = false;
        //                        Logger.Info(LogCategory,
        //                            string.Format("TaskFilter: Flunk {0} - TaskIsOrderEntry", item.Description));
        //                        return;
        //                    }

        //                    bool isPatientTransferred = false;
        //                    bool isPatientDischarged = false;

        //                    if (CurrentAdmission != null)
        //                    {
        //                        // Transferred is (Status T=Transfered) or (Status R=Refered with a transfer encounter of file)
        //                        isPatientTransferred =
        //                            ((CurrentAdmission.AdmissionStatus ==
        //                              AdmissionStatusHelper.AdmissionStatus_Referred &&
        //                              CurrentAdmission.EncounterTransfer != null) ||
        //                             (CurrentAdmission.AdmissionStatus ==
        //                              AdmissionStatusHelper.AdmissionStatus_Transferred));
        //                        isPatientDischarged = CurrentAdmission.AdmissionStatus ==
        //                                              AdmissionStatusHelper.AdmissionStatus_Discharged;
        //                    }

        //                    UserProfile up = UserCache.Current.GetUserProfileFromUserId(UserID, false);
        //                    if (up == null)
        //                    {
        //                        up = UserCache.Current.GetCurrentUserProfile();
        //                    }

        //                    bool CanPerfom = CanPerformST(up, item);

        //                    bool ownerOrOversite = false;
        //                    int[] currentgroups = CurrentAdmissionGroups;
        //                    int[] ownerOrOversiteGroup =
        //                        meUserProfile.ServiceLineGroupIdsUserIsOwnerOrOverride.ToArray();
        //                    if (ownerOrOversiteGroup.Any())
        //                    {
        //                        if (currentgroups != null && ownerOrOversiteGroup.Intersect(currentgroups).Any())
        //                        {
        //                            ownerOrOversite = true;
        //                        }
        //                    }

        //                    // check for oversite at the admission serviceLine level
        //                    if ((ownerOrOversite == false) && (CurrentAdmission.ServiceLineKey > 0))
        //                    {
        //                        ownerOrOversite = meUserProfile.UserProfileServiceLine.Where(p =>
        //                            ((p.ServiceLineKey == CurrentAdmission.ServiceLineKey) && (p.Oversite))).Any();
        //                    }

        //                    if ((((item.ServiceIsCOTI) || (item.ServiceIsVerbalCOTI)) &&
        //                         (isPatientDischarged == false)) || (item.ServiceIsHospiceF2F) ||
        //                        (item.ServiceIsHospicePhysicianEncounter))
        //                    {
        //                        // Assume not scheduling for others - its just you doing an ad-hoc task for yourself
        //                        if (UserID != meUserProfile.UserId)
        //                        {
        //                            e.Accepted = false;
        //                            Logger.Info(LogCategory,
        //                                string.Format(
        //                                    "TaskFilter: Flunk {0} - For Verbal CTI, CTI, F2F and PS Encounter - you can only schedule yourself",
        //                                    item.Description));
        //                            return;
        //                        }

        //                        if ((CanPerfom == false) || (UserCache.Current.GetCurrentUserProfile()
        //                                .DisciplineInUserProfile.Where(d => d.DisciplineKey == item.DisciplineKey)
        //                                .Any() == false))
        //                        {
        //                            e.Accepted = false;
        //                            Logger.Info(LogCategory,
        //                                string.Format(
        //                                    "TaskFilter: Flunk {0} - For Verbal CTI - you need the discipline (usually SN and/or SW) and for CTI, F2F and PS Encounter - you need the discipline (usually PS)",
        //                                    item.Description));
        //                            return;
        //                        }

        //                        if ((item.ServiceIsCOTI) &&
        //                            (UserCache.Current.UserIdIsHospiceMedicalDirector(up.UserId) == false) &&
        //                            (UserCache.Current.UserIdIsHospicePhysician(up.UserId) == false))
        //                        {
        //                            e.Accepted = false;
        //                            Logger.Info(LogCategory,
        //                                string.Format(
        //                                    "TaskFilter: Flunk {0} - For CTI - you need the med director or physician role",
        //                                    item.Description));
        //                            return;
        //                        }

        //                        if ((item.ServiceIsHospiceF2F) &&
        //                            (UserCache.Current.UserIdIsHospiceMedicalDirector(up.UserId) == false) &&
        //                            (UserCache.Current.UserIdIsHospicePhysician(up.UserId) == false) &&
        //                            (UserCache.Current.UserIdIsHospiceNursePractitioner(up.UserId) == false))
        //                        {
        //                            e.Accepted = false;
        //                            Logger.Info(LogCategory,
        //                                string.Format(
        //                                    "TaskFilter: Flunk {0} - For F2F - you need the med director, physician role or nurse practitioner role",
        //                                    item.Description));
        //                            return;
        //                        }

        //                        if ((item.ServiceIsHospicePhysicianEncounter) &&
        //                            (UserCache.Current.UserIdIsHospiceMedicalDirector(up.UserId) == false) &&
        //                            (UserCache.Current.UserIdIsHospicePhysician(up.UserId) == false) &&
        //                            (UserCache.Current.UserIdIsHospiceNursePractitioner(up.UserId) == false))
        //                        {
        //                            e.Accepted = false;
        //                            Logger.Info(LogCategory,
        //                                string.Format(
        //                                    "TaskFilter: Flunk {0} - For CTI - you need the med director, physician role or nurse practitioner role",
        //                                    item.Description));
        //                            return;
        //                        }

        //                        e.Accepted = true;
        //                        return;
        //                    }

        //                    if ((!ownerOrOversite) && (!IsSchedulerOrOpsManager) && !(CanPerfom &&
        //                            UserCache.Current.GetCurrentUserProfile().DisciplineInUserProfile
        //                                .Where(d => d.DisciplineKey == item.DisciplineKey).Any()))
        //                    {
        //                        e.Accepted = false;
        //                        Logger.Info(LogCategory,
        //                            string.Format(
        //                                "TaskFilter: Flunk {0} - IsSchedulerOrOpsManager and UserCanPerformTask and DisciplineInUserProfile failed",
        //                                item.Description));
        //                    }
        //                    else
        //                    {
        //                        if ((CanPerfom == false) && (UserID != null) && (UserID != meUserProfile.UserId))
        //                        {
        //                            e.Accepted = false;
        //                            Logger.Info(LogCategory,
        //                                string.Format(
        //                                    "TaskFilter: Flunk {0} - User is NOT the logged in user and has no access to the ServiceType/Discipline",
        //                                    item.Description));
        //                            return;
        //                        }

        //                        if ((CanPerfom == false) && (UserID != null) && (UserID == meUserProfile.UserId) &&
        //                            (ownerOrOversite == false) && (IsSchedulerOrOpsManager == false))
        //                        {
        //                            e.Accepted = false;
        //                            Logger.Info(LogCategory,
        //                                string.Format(
        //                                    "TaskFilter: Flunk {0} - User IS the logged in user with no oversite or scheduling and has no access to the ServiceType/Discipline",
        //                                    item.Description));
        //                            return;
        //                        }

        //                        if (isPatientDischarged)
        //                        {
        //                            e.Accepted = (item.Description.ToLower().Contains("poc") ||
        //                                          item.Description.ToLower().Contains("plan of care") ||
        //                                          item.ServiceIsPlanOfCare ||
        //                                          (item.ServiceIsCOTI) ||
        //                                          (item.ServiceIsVerbalCOTI));
        //                            if ((e.Accepted) && ((item.ServiceIsCOTI) || (item.ServiceIsVerbalCOTI)))
        //                            {
        //                                // Assume not scheduling for others - its just you doing an ad-hoc task for yourself
        //                                if ((item.ServiceIsCOTI) &&
        //                                    (UserCache.Current.UserIdIsHospiceMedicalDirector(UserID) == false) &&
        //                                    (UserCache.Current.UserIdIsHospicePhysician(UserID) == false))
        //                                {
        //                                    e.Accepted = false;
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Flunk {0} - For CTI - you need the med director or physician role",
        //                                            item.Description));
        //                                    return;
        //                                }

        //                                Logger.Info(LogCategory,
        //                                    string.Format(
        //                                        "TaskFilter: Pass {0} - PatientDischarge and TaskIsPOC, Verbal CTO or CTI",
        //                                        item.Description));
        //                                return;
        //                            }

        //                            if (e.Accepted)
        //                            {
        //                                Logger.Info(LogCategory,
        //                                    string.Format(
        //                                        "TaskFilter: Pass {0} - PatientDischarge and TaskIsPOC, Verbal CTO or CTI",
        //                                        item.Description));
        //                            }
        //                            else
        //                            {
        //                                Logger.Info(LogCategory,
        //                                    string.Format(
        //                                        "TaskFilter: Flunk {0} - PatientDischarge and TaskIsPOC, Verbal CTO or CTI",
        //                                        item.Description));
        //                            }
        //                        }
        //                        else if (isPatientTransferred)
        //                        {
        //                            // patient is transferred, only allow servicetypes mapped to eval or resumption forms
        //                            if (IsSystemAdministrator && IsPatientSelected(PatientKey)) //DS - 3558
        //                            {
        //                                e.Accepted = ((item.ServiceIsResumption) || (item.ServiceIsEval)) ||
        //                                             (item.Description.ToLower().Contains("poc") ||
        //                                              item.Description.ToLower().Contains("plan of care"));
        //                                if (e.Accepted)
        //                                {
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Pass {0} - IsSysAdmin and PatientTransfer and TaskIsResumptionOrIsEvalIsPOC",
        //                                            item.Description));
        //                                }
        //                                else
        //                                {
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Flunk {0} - IsSysAdmin and PatientTransfer and TaskIsResumptionOrIsEvalIsPOC",
        //                                            item.Description));
        //                                }
        //                            }
        //                            else
        //                            {
        //                                e.Accepted = ((item.ServiceIsResumption) || (item.ServiceIsEval));
        //                                if (e.Accepted)
        //                                {
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Pass {0} - IsNotSysAdmin and PatientTransfer and TaskIsResumptionOrIsEval",
        //                                            item.Description));
        //                                }
        //                                else
        //                                {
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Flunk {0} - IsNotSysAdmin and PatientTransfer and TaskIsResumptionOrIsEval",
        //                                            item.Description));
        //                                }
        //                            }
        //                        }
        //                        else
        //                        {
        //                            var have_eval = ((TaskContext == null) || (TaskContext.TaskEncounters == null))
        //                                ? false
        //                                : TaskContext.TaskEncounters
        //                                    .Where(ec => ec.AdmissionKey == CurrentAdmission.AdmissionKey)
        //                                    .Where(ec => ec.DisciplineKey == item.DisciplineKey)
        //                                    .Where(ec => ec.EncounterStartTime.HasValue)
        //                                    .Where(ec =>
        //                                        CurrentAdmissionDiscipline.ReferDateTime.Value.Date.CompareTo(
        //                                            ec.EncounterStartTime.Value.Date) <= 0)
        //                                    .Any(ec => ec.MyForm != null &&
        //                                               (ec.MyForm.IsEval || ec.MyForm.IsResumption));

        //                            // If the soc date is prior to the go live date and the Discipline Admit Date is populated and Prior to the go live date,
        //                            // treat the admission like it already has the eval done, even if it isn't in our list.
        //                            //var goLiveDate = ServiceLineCache.Current.GoLiveDateForAdmission(CurrentAdmission, CurrentAdmission.ServiceLineKey);
        //                            if (!have_eval &&
        //                                CurrentAdmission.SOCDate.HasValue &&
        //                                CurrentAdmission.SOCDate <= goLiveDate &&
        //                                CurrentAdmissionDiscipline != null &&
        //                                CurrentAdmissionDiscipline.DisciplineAdmitDateTime.HasValue &&
        //                                CurrentAdmissionDiscipline.DisciplineAdmitDateTime <= goLiveDate)
        //                            {
        //                                have_eval = true;
        //                            }

        //                            if (item.ServiceIsHospiceElectionAddendum)
        //                            {
        //                                // Assume not scheduling for others - its just you doing an ad-hoc task for yourself
        //                                if (UserID != meUserProfile.UserId)
        //                                {
        //                                    e.Accepted = false;
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Flunk {0} - For Hospice Election Addendum Encounter - you can only schedule yourself",
        //                                            item.Description));
        //                                    return;
        //                                }

        //                                if (have_eval == false)
        //                                {
        //                                    e.Accepted = false;
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Flunk {0} - For Hospice Election Addendum you must be admitted to the discipline",
        //                                            item.Description));
        //                                    return;
        //                                }

        //                                if ((CanPerfom == false) || (IsHospiceElectionAddendum == false))
        //                                {
        //                                    e.Accepted = false;
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Flunk {0} - For Hospice Election Addendum you need the discipline (usually HSN) and the Hospice Election Addendum role",
        //                                            item.Description));
        //                                    return;
        //                                }

        //                                if ((CurrentAdmission != null) &&
        //                                    (CurrentAdmission.HIBInsuranceElectionAddendumAvailable == false))
        //                                {
        //                                    e.Accepted = false;
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Flunk {0} - For Hospice Election Addendum you need the Admission HIB Insurance to support ElectionAddendumAvailable",
        //                                            item.Description));
        //                                    return;
        //                                }
        //                            }

        //                            //per item logic
        //                            if (item.ServiceIsResumption)
        //                            {
        //                                e.Accepted =
        //                                    false; // Only allow resumption if isPatientTransferred - skimmed off above
        //                                Logger.Info(LogCategory,
        //                                    string.Format(
        //                                        "TaskFilter: Flunk {0} - PatientNotTransferOrDischarge and IsResumption",
        //                                        item.Description));
        //                            }
        //                            else if (item.ServiceIsEval == false) //item is NOT a servicetype mapped to eval form
        //                            {
        //                                e.Accepted = have_eval;
        //                                if (e.Accepted)
        //                                {
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Pass {0} - PatientNotTransferOrDischarge and TaskNotEval and EvalOnFile({1})",
        //                                            item.Description, have_eval));
        //                                }
        //                                else
        //                                {
        //                                    //TaskFilter: Flunk Certification of Terminal Illness - PatientNotTransferOrDischarge and TaskNotEval and EvalOnFile(False)
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Flunk {0} - PatientNotTransferOrDischarge and TaskNotEval and EvalOnFile({1})",
        //                                            item.Description, have_eval));
        //                                }
        //                            }
        //                            else if (item.ServiceIsEval)
        //                            {
        //                                if (have_eval || !CurrentAdmission.AdmissionDiscipline.Where(ad =>
        //                                        ad.DisciplineKey == item.DisciplineKey &&
        //                                        !ad.DisciplineAdmitDateTime.HasValue &&
        //                                        !ad.DischargeDateTime.HasValue && !ad.NotTakenDateTime.HasValue).Any())
        //                                {
        //                                    e.Accepted = false;
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Flunk {0} - PatientNotTransferOrDischarge and TaskEval and (EvalOnFile({1}) or DispIsAdmitOrDischargeOrNTUC",
        //                                            item.Description, have_eval));
        //                                }
        //                            }
        //                        }
        //                    }

        //                    // Last ditch ... If Scheduling is in use, then only Appointmate can schedule 'Most' services.
        //                    // Make pre-eval discipline agnostic
        //                    int OnHoldStatus = (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "H");
        //                    var have_preeval = ((TaskContext == null) || (TaskContext.TaskEncounters == null))
        //                        ? false
        //                        : TaskContext.TaskEncounters
        //                            .Where(ec =>
        //                                (ec.AdmissionKey == CurrentAdmission.AdmissionKey) &&
        //                                (CurrentAdmission.AdmissionStatus != OnHoldStatus))
        //                            .Any(ec => ec.MyForm != null && ec.MyForm.IsPreEval);

        //                    if (e.Accepted)
        //                    {
        //                        if (TenantSettingsCache.Current.TenantSettingIsForeignSchedulingInUse)
        //                        {
        //                            if (item.DisciplineKey == null)
        //                            {
        //                                Logger.Info(LogCategory,
        //                                    string.Format("TaskFilter: Flunk {0} - SchedulingInUse", item.Description));
        //                                e.Accepted = false;
        //                            }
        //                            else
        //                            {
        //                                e.Accepted = TaskSchedulingHelper.CanCVScheduleTask(
        //                                    CurrentAdmission.AdmissionDisciplineFrequency, item.DatabaseKey,
        //                                    TaskStartDate.DateTime, item.ServiceIsSchedulable);
        //                                if (e.Accepted)
        //                                {
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Pass {0} - SchedulingInUse and CrescencoCanScheduleThisTask",
        //                                            item.Description));
        //                                }
        //                                else
        //                                {
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "TaskFilter: Flunk {0} - SchedulingInUse and CrescencoCanScheduleThisTask",
        //                                            item.Description));
        //                                }
        //                            }
        //                        }
        //                        else
        //                        {
        //                            // do we really want to accept?
        //                            // If Admssion.PreEvalRequired 

        //                            if ((item.ServiceIsPreEval) &&
        //                                (CurrentAdmission.PreEvalRequired ==
        //                                 false)) // not using PreEvals for this admission
        //                            {
        //                                Logger.Info(LogCategory,
        //                                    string.Format("TaskFilter: Flunk {0} - TaskIsPreEval and NotPreEvalInUse",
        //                                        item.Description));
        //                                e.Accepted = false;
        //                            }
        //                            else if ((item.ServiceIsPreEval) && (CurrentAdmission.PreEvalRequired) &&
        //                                     (have_preeval)) // Using PreEvals for this admission - but already have one on file
        //                            {
        //                                Logger.Info(LogCategory,
        //                                    string.Format(
        //                                        "TaskFilter: Flunk {0} - TaskIsPreEval and PreEvalInUse and PreEvalOnFile",
        //                                        item.Description));
        //                                e.Accepted = false;
        //                            }
        //                            else if ((!item.ServiceIsPreEval) && (CurrentAdmission.PreEvalRequired) &&
        //                                     (have_preeval ==
        //                                      false)) // Using PreEvals for this admissiion - don't have one on file and I'm not a Pre-Eval
        //                            {
        //                                Logger.Info(LogCategory,
        //                                    string.Format(
        //                                        "TaskFilter: Flunk {0} - NotTaskIsPreEval and PreEvalInUse and NotPreEvalOnFile",
        //                                        item.Description));
        //                                e.Accepted = false;
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if ((item.ServiceIsPreEval) && (CurrentAdmission.PreEvalRequired) &&
        //                            (have_preeval == false)) // using PreEvals for this admission - and don't have one
        //                        {
        //                            Logger.Info(LogCategory,
        //                                string.Format(
        //                                    "TaskFilter: Pass {0} - TaskIsPreEval and PreEvalInUse and NotPreEvalOnFile",
        //                                    item.Description));
        //                            e.Accepted = true;
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    Logger.Info(LogCategory,
        //                        string.Format("TaskFilter: Flunk {0} - TaskIsNonService", item.Description));
        //                    e.Accepted = false;
        //                }
        //            }
        //            else
        //            {
        //                e.Accepted =
        //                    item.CodeType.Equals("ACTIVITY"); //patient NOT selected, only allow non-patient activities
        //            }

        //            Logger.Info(LogCategory,
        //                string.Format("TaskFilter: RETURN {0} {1} - Accepted = {2}", item.DisciplineKey,
        //                    item.Description, e.Accepted));
        //        };
        //    }
        //}

        private bool RefreshingFilterAvailableTaskList;

        //private void FilterAvailableTaskList()
        //{
        //    if (isLoading)
        //    {
        //        return;
        //    }

        //    if (FilteredLookupCollection == null || FilteredLookupCollection.View == null)
        //    {
        //        return;
        //    }

        //    if (RefreshingFilterAvailableTaskList || RefreshingFilterAvailableUsers)
        //    {
        //        return;
        //    }

        //    RefreshingFilterAvailableTaskList = true;
        //    Logger.Info(LogCategory, "FilterAvailableTaskList: Start");
        //    var __patientSelected = IsPatientSelected(PatientKey);
        //    UserCanVisitAdmission = __patientSelected ? InitializeUserCanVisitAdmission() : true;
        //    UserCanVisitErrorVisible = !UserCanVisitAdmission && IsPatientSelected(PatientKey);
        //    SetupServiceTypeDependency();
        //    string prevLookupKey = LookupKey;

        //    int oldPos = GetPositionOfLookupKey(prevLookupKey);
        //    FilteredLookupCollection.View.Refresh();
        //    int newPos = GetPositionOfLookupKey(prevLookupKey);
        //    bool Dispatching = false;

        //    // if we have a user and the selected servicetype is not longer in the list - clear it
        //    if ((string.IsNullOrWhiteSpace(prevLookupKey) == false) && (UserID != null))
        //    {
        //        List<LookupPM> lpList = FilteredLookupCollection.View.OfType<LookupPM>().ToList();
        //        LookupPM lp = lpList.Where(p => p.LookupKey == prevLookupKey).FirstOrDefault();
        //        if ((lp == null) || (newPos == 0))
        //        {
        //            __LookupKeyBackingField = "";
        //            RaisePropertyChanged("LookupKey");
        //        }
        //        else if ((oldPos != newPos) && (newPos != 0))
        //        {
        //            __LookupKeyBackingField = "";
        //            RaisePropertyChanged("LookupKey");

        //            Dispatching = true;
        //            Deployment.Current.Dispatcher.BeginInvoke(() =>
        //            {
        //                __LookupKeyBackingField = prevLookupKey;
        //                RaisePropertyChanged("LookupKey");
        //                RefreshingFilterAvailableTaskList = false;
        //            });
        //        }
        //    }

        //    if (Dispatching == false)
        //    {
        //        RefreshingFilterAvailableTaskList = false;
        //    }

        //    Logger.Info(LogCategory, "FilterAvailableTaskList: End");
        //}

        //private int GetPositionOfLookupKey(string lookupKey)
        //{
        //    if ((FilteredLookupCollection == null) || (FilteredLookupCollection.View == null))
        //    {
        //        return 0;
        //    }

        //    List<LookupPM> lpList = FilteredLookupCollection.View.OfType<LookupPM>().ToList();
        //    if (lpList == null)
        //    {
        //        return 0;
        //    }

        //    int i = 0;
        //    foreach (LookupPM lp in lpList)
        //    {
        //        i++;
        //        if (lp.LookupKey == lookupKey)
        //        {
        //            return i;
        //        }
        //    }

        //    return 0;
        //}

        private bool __UserCanVisitAdmissionBackingField;

        private bool UserCanVisitAdmission
        {
            get { return __UserCanVisitAdmissionBackingField; }
            set { __UserCanVisitAdmissionBackingField = value; }
        }

        //allowSelectedUserOnTaskCheck = true - allows Task.UserID to bind to combobox when open a task
        //set allowSelectedUserOnTaskCheck to false on SAVE to force check against service line groups...
        //private bool InitializeUserCanVisitAdmission(bool allowSelectedUserOnTaskCheck = true)
        //{
        //    if ((UserID == null) && (FilteredUserCollection.View.Cast<UserProfile>().Count() > 1))
        //    {
        //        return true; // multiple users and no one selected - not sure until a user is selected - assume true
        //    }

        //    Guid? userID = (UserID == null) ? WebContext.Current.User.MemberID : UserID;
        //    if ((userID == null) || (SelectedAdmission == null))
        //    {
        //        return false;
        //    }

        //    //Only give a pass if the user on the task equals the user selected in the UI for non-patient related tasks and
        //    //when the task is first opened, else check ServiceLineGroup permissions
        //    if ((allowSelectedUserOnTaskCheck) && CurrentTask != null && CurrentTask.UserID.Equals(userID) &&
        //        CurrentTask.TaskKey > 0)
        //    {
        //        return true;
        //    }

        //    UserProfile up1 = UserCache.Current.GetUserProfileFromUserId(userID);
        //    if (up1 == null)
        //    {
        //        return false;
        //    }

        //    int[] canVisitGroups = up1.ServiceLineGroupIdsUserCanVisit.ToArray();

        //    if ((canVisitGroups.Any() == false) || (canVisitGroups.Intersect(CurrentAdmissionGroups).Any() == false))
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        private int[] ServiceTypeDependencyKeys;

        private void SetupServiceTypeDependency()
        {
            ServiceTypeDependencyKeys = null;
            //if (SelectedAdmission == null)
            {
                return;
            }

            if (FilteredLookupCollection.View == null)
            {
                return;
            }

           // if (CurrentAdmissionGroups.Any() == false)
            {
                return;
            }

            //ServiceTypeDependencyKeys =
            //    ServiceLineCache.GetServiceTypeDependencyKeysFromServiceLineGroupingKeys(CurrentAdmissionGroups);
        }

       // private UserProfile meUserProfile = UserCache.Current.GetCurrentUserProfile();

        //public void SetupFilteredUserCollection()
        //{
        //    FilteredUserCollection.SortDescriptions.Add(new SortDescription("FullName", ListSortDirection.Ascending));

        //    var EditingTask = (CurrentTask.TaskKey > 0);

        //    FilteredUserCollection.Source = UserCache.Current.GetUsers(true)
        //        .Where(p => ((p.Inactive == false) ||
        //                     ((CurrentTask != null) &&
        //                      (CurrentTask.UserID ==
        //                       p.UserId)))); //The only inactive user allowed is the one the task is assigned to

        //    FilteredUserCollection.Filter += (s, e) =>
        //    {
        //        e.Accepted = false;
        //        UserProfile up = e.Item as UserProfile;
        //        if (up == null)
        //        {
        //            return;
        //        }

        //        up = UserCache.Current.GetUserProfileFromUserId(up.NullableUserId);
        //        if (up == null)
        //        {
        //            return;
        //        }

        //        if ((PatientKey.HasValue) && (PatientKey > 0))
        //        {
        //            // Only allow DeltaUsers if the currently logged in user is also a DeltaUser, ditto for DeltaAdmin
        //            if ((WebContext.Current.User.DeltaUser == false) && up.DeltaUser && (CurrentTask != null) &&
        //                (CurrentTask.UserID != up.UserId))
        //            {
        //                Logger.Info(LogCategory,
        //                    string.Format(
        //                        "UserFilter: Flunk {0} - Logged in User is not a DeltaUser, so disallow DeltaUsers",
        //                        up.FullName));
        //                return;
        //            }

        //            if ((WebContext.Current.User.DeltaAdmin == false) && up.DeltaAdmin && (CurrentTask != null) &&
        //                (CurrentTask.UserID != up.UserId))
        //            {
        //                Logger.Info(LogCategory,
        //                    string.Format(
        //                        "UserFilter: Flunk {0} - Logged in User is not a DeltaAdmin, so disallow DeltaAdmins",
        //                        up.FullName));
        //                return;
        //            }

        //            var selected_patient =
        //                TaskContext.TaskPatients.Where(p => p.PatientKey == PatientKey).FirstOrDefault();
        //            if (selected_patient != null)
        //            {
        //                var selected_admission = SelectedAdmission;
        //                if (selected_admission != null)
        //                {
        //                    if (CurrentTask != null && CurrentTask.UserID.Equals(up.UserId) && CurrentTask.TaskKey > 0)
        //                    {
        //                        e.Accepted = true;
        //                        Logger.Info(LogCategory,
        //                            string.Format("UserFilter: Pass {0} - Existing Task edit", up.FullName));
        //                        return;
        //                    }

        //                    bool ownerOrOversite = false;
        //                    int[] currentgroups = selected_admission.AdmissionGroup.Where(p =>
        //                        ((p.StartDate.HasValue && ((DateTime)p.StartDate).Date <= TaskStartDate.Date) ||
        //                         !p.StartDate.HasValue) &&
        //                        ((p.EndDate.HasValue && ((DateTime)p.EndDate).Date >= TaskStartDate.Date) ||
        //                         !p.EndDate.HasValue)).Select(ag => ag.ServiceLineGroupingKey).ToArray();

        //                    int[] ownerOrOversiteGroup =
        //                        meUserProfile.ServiceLineGroupIdsUserIsOwnerOrOverride.ToArray();
        //                    if (ownerOrOversiteGroup.Any())
        //                    {
        //                        if (currentgroups != null && ownerOrOversiteGroup.Intersect(currentgroups).Any())
        //                        {
        //                            ownerOrOversite = true;
        //                        }
        //                    }

        //                    // check for oversite at the admission serviceLine level
        //                    if ((ownerOrOversite == false) && (selected_admission.ServiceLineKey > 0))
        //                    {
        //                        ownerOrOversite = meUserProfile.UserProfileServiceLine.Where(p =>
        //                            ((p.ServiceLineKey == selected_admission.ServiceLineKey) && (p.Oversite))).Any();
        //                    }

        //                    int[] canVisitGroups = up.ServiceLineGroupIdsUserCanVisit.ToArray();
        //                    if ((canVisitGroups.Any() == false) ||
        //                        (canVisitGroups.Intersect(currentgroups).Any() == false))
        //                    {
        //                        e.Accepted = false;
        //                        Logger.Info(LogCategory,
        //                            string.Format("UserFilter: Flunk {0} - UserCanVisitAdmissionGroupLeaf",
        //                                up.FullName));
        //                        return;
        //                    }

        //                    //if I'm the coordinator, group ownerOrOversite or I have the scheduler role, I can create tasks for other users
        //                    if (meUserProfile.UserId == up.UserId || IsSchedulerOrOpsManager || ownerOrOversite ||
        //                        (selected_admission.CareCoordinator.HasValue &&
        //                         selected_admission.CareCoordinator.Value.Equals(meUserProfile.UserId)))
        //                    {
        //                        if (!string.IsNullOrEmpty(LookupKey) && LookupKey.Contains("-SERVICE"))
        //                        {
        //                            var lookup_key_value = LookupKey.Split('-');
        //                            var disc = ServiceTypeCache.GetDisciplineKey(Convert.ToInt32(lookup_key_value[0]));
        //                            var svctype =
        //                                ServiceTypeCache.GetServiceTypeFromKey(Convert.ToInt32(lookup_key_value[0]));
        //                            var CanPerformST = UserCanPerformServiceType(svctype, up);

        //                            if (CanPerformST)
        //                            {
        //                                if (IsSchedulerOrOpsManager)
        //                                {
        //                                    e.Accepted = true;
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "UserFilter: Pass {0} - TaskSelected and UserCanPerformTask and IamSchedulerOrOpsManager",
        //                                            up.FullName));
        //                                }
        //                                else if (
        //                                    up.DisciplineInUserProfile
        //                                        .Where(p => disc.HasValue && p.DisciplineKey == disc.Value).Any() &&
        //                                    (currentgroups == null || up.ServiceLineGroupIdsUserCanVisit
        //                                        .Where(p => currentgroups.Contains(p)).Any())
        //                                )
        //                                {
        //                                    e.Accepted = true;
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "UserFilter: Pass {0} - TaskSelected and UserCanPerformTask and NotIamSchedulerOrOpsManager and DisciplineInUserProfile and UserCanVisitAdmissionGroupLeaf",
        //                                            up.FullName));
        //                                }
        //                            }
        //                            else
        //                            {
        //                                if (up.UserId == CurrentTask.UserID && !CurrentTask.IsNew)
        //                                {
        //                                    e.Accepted = true;
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "UserFilter: Pass {0} - TaskSelected and EditExistingTask and NotUserCanPerformTask",
        //                                            up.FullName));
        //                                }
        //                                else
        //                                {
        //                                    e.Accepted = false;
        //                                    Logger.Info(LogCategory,
        //                                        string.Format(
        //                                            "UserFilter: Flunk {0} - TaskSelected and EditExistingTask and NotUserCanPerformTask",
        //                                            up.FullName));
        //                                }
        //                            }
        //                        }
        //                        else
        //                        {
        //                            if (FilteredLookupCollection != null && FilteredLookupCollection.View != null)
        //                            {
        //                                foreach (LookupPM item in FilteredLookupCollection.View)
        //                                {
        //                                    bool CanPerfom = CanPerformST(up, item);
        //                                    if (CanPerfom && IsSchedulerOrOpsManager && up.Inactive == false)
        //                                    {
        //                                        e.Accepted = true;
        //                                        Logger.Info(LogCategory,
        //                                            string.Format(
        //                                                "UserFilter: Pass {0} - NotTaskSelected and UserCanPerformSomeTaskInList and IamSchedulerOrOpsManager",
        //                                                up.FullName));
        //                                        break;
        //                                    }

        //                                    if (
        //                                        CanPerfom && up.DisciplineInUserProfile
        //                                            .Where(p => p.DisciplineKey == item.DisciplineKey).Any() &&
        //                                        //(currentgroups == null || up.UserProfileGroup.Where(p => currentgroups.Contains(p.ServiceLineGroupingKey) && p.CanVisit && !p.EndDate.HasValue).Any())
        //                                        (currentgroups == null || up.ServiceLineGroupIdsUserCanVisit
        //                                            .Where(p => currentgroups.Contains(p)).Any())
        //                                    )
        //                                    {
        //                                        e.Accepted = true;
        //                                        Logger.Info(LogCategory,
        //                                            string.Format(
        //                                                "UserFilter: Pass {0} - NotTaskSelected and UserCanPerformSomeTaskInList and NotIamSchedulerOrOpsManager and DisciplineInUserProfile and UserCanVisitAdmissionGroupLeaf",
        //                                                up.FullName));
        //                                        break;
        //                                    }
        //                                    // All non service time tasks should be allowed.

        //                                    if (CanPerfom && !item.DisciplineKey.HasValue &&
        //                                        meUserProfile.UserId == up.UserId)
        //                                    {
        //                                        e.Accepted = true;
        //                                        Logger.Info(LogCategory,
        //                                            string.Format("UserFilter: Pass {0} - NotTaskSelected",
        //                                                up.FullName));
        //                                        break;
        //                                    }
        //                                }
        //                            }
        //                            else
        //                            {
        //                                e.Accepted = false;
        //                                Logger.Info(LogCategory,
        //                                    string.Format("UserFilter: Flunk {0} - NoTaskList", up.FullName));
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        else if (meUserProfile.UserId == up.UserId)
        //        {
        //            e.Accepted = true;
        //            Logger.Info(LogCategory,
        //                string.Format("UserFilter: Pass {0} - No Patient Selected and User Matches Logged in User",
        //                    up.FullName));
        //        }
        //    };
        //}

        private bool RefreshingFilterAvailableUsers;

        //public void FilterAvailableUsers()
        //{
        //    if (isLoading)
        //    {
        //        return;
        //    }

        //    if (_FilteredUserCollection.View == null)
        //    {
        //        return;
        //    }

        //    if (RefreshingFilterAvailableTaskList || RefreshingFilterAvailableUsers)
        //    {
        //        return;
        //    }

        //    RefreshingFilterAvailableUsers = true;
        //    Logger.Info(LogCategory, "FilterAvailableUsers: Start");
        //    var currentUserID = UserID;
        //    UserID = null;

        //    _FilteredUserCollection.View.Refresh();

        //    UserProfile firstUserProfile = null;
        //    int acceptedcount = 0;
        //    foreach (UserProfile item in FilteredUserCollection.View)
        //    {
        //        if (acceptedcount == 0)
        //        {
        //            firstUserProfile = item;
        //        }

        //        acceptedcount++;
        //        if (item.UserId.Equals(currentUserID))
        //        {
        //            UserID = currentUserID;
        //        }
        //    }

        //    if ((acceptedcount == 1) &&
        //        (firstUserProfile == meUserProfile)) //need more than yourself to have user combo visible
        //    {
        //        UserVisible = false;
        //        FilteredUserCollection.View.MoveCurrentToFirst();
        //        UserID = meUserProfile.UserId; //if your're the only one in the list then auto select you
        //    }
        //    else if (acceptedcount == 0)
        //    {
        //        UserVisible = false;
        //    }
        //    else if (CurrentTask.IsNew && !UserCanChangeCaregiver)
        //    {
        //        UserID = meUserProfile.UserId; //if the user can't change the employee, try to default to that employee.
        //        if (acceptedcount > 1)
        //        {
        //            UserVisible = true;
        //        }
        //    }
        //    else
        //    {
        //        if (FilteredUserCollection.View.Cast<UserProfile>().Count() == 1)
        //        {
        //            FilteredUserCollection.View.MoveCurrentToFirst();
        //        }

        //        UserVisible = true;
        //    }

        //    if ((LookupKey != null && LookupKey.Contains("-ACTIVITY")) || !PatientKey.HasValue)
        //    {
        //        UserVisible = false;
        //        UserCanVisitErrorVisible = false;
        //        RaisePropertyChanged("UserVisible");
        //        RaisePropertyChanged("UserCanVisitErrorVisible");
        //        RaisePropertyChanged("UserCanChangeCaregiver");
        //    }

        //    Logger.Info(LogCategory, "FilterAvailableUsers: End");
        //    RefreshingFilterAvailableUsers = false;
        //}

        //private bool CanPerformST(UserProfile up, LookupPM item)
        //{
        //    bool CanPerfom = true;
        //    if (item.CodeType == "SERVICE")
        //    {
        //        var st = ServiceTypeCache.GetServiceTypeFromKey(item.DatabaseKey);
        //        if (st != null)
        //        {
        //            CanPerfom = UserCanPerformServiceType(st, up);
        //        }
        //    }

        //    return CanPerfom;
        //}

        //public bool UserCanPerformServiceType(ServiceType st, UserProfile up)
        //{
        //    return TaskSchedulingHelper.UserCanPerformServiceType(st, up);
        //}

        //private bool ValidateTask()
        //{
        //    if (DeletingTask)
        //    {
        //        CurrentTask.CanceledBy = WebContext.Current.User.MemberID;
        //        CurrentTask.CanceledAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        //    }

        //    if (!UserID.HasValue)
        //    {
        //        MessageBox.Show("Please select a Caregiver to perform this activity");
        //        return false;
        //    }

        //    var _patientSelected = IsPatientSelected(PatientKey);
        //    var _userCanVisit =
        //        InitializeUserCanVisitAdmission(allowSelectedUserOnTaskCheck: true); // give selected user a pass

        //    if (!Validate())
        //    {
        //        return false;
        //    }

        //    if ((PatientKey.HasValue) && (PatientKey > 0)) //a patient is selected
        //    {
        //        CurrentTask.PatientKey = PatientKey;

        //        var selected_patient = TaskContext.TaskPatients.Where(p => p.PatientKey == PatientKey).FirstOrDefault();
        //        if (selected_patient != null)
        //        {
        //            var selected_admission = SelectedAdmission;
        //            if (selected_admission != null)
        //            {
        //                CurrentTask.AdmissionKey = selected_admission.AdmissionKey;
        //            }
        //            else
        //            {
        //                MessageBox.Show(String.Format(
        //                    "TaskViewModel.SaveTask: Failed to find admission for patient key: {0}",
        //                    selected_patient.PatientKey));
        //            }
        //        }
        //        else
        //        {
        //            MessageBox.Show("TaskViewModel.SaveTask: Failed to find patient");
        //        }
        //    }
        //    else
        //    {
        //        CurrentTask.PatientKey = null;
        //    }

        //    if (!LookupKey.Contains("-"))
        //    {
        //        return false;
        //    }

        //    var lookup_key_value = LookupKey.Split('-');
        //    if (lookup_key_value[1].Equals("SERVICE"))
        //    {
        //        CurrentTask.NonServiceTypeKey = null;
        //        CurrentTask.ServiceTypeKey = Int32.Parse(lookup_key_value[0]);
        //    }
        //    else
        //    {
        //        CurrentTask.NonServiceTypeKey = Int32.Parse(lookup_key_value[0]);
        //        CurrentTask.ServiceTypeKey = null;
        //    }

        //    CurrentTask.UserID = UserID.Value;
        //    CurrentTask.CancelReasonKey = CancelReasonKey;
        //    CurrentTask.Notes = Notes;
        //    CurrentTask.TaskDuration = TaskDuration;
        //    CurrentTask.TaskStartDateTime = TaskStartDate;
        //    CurrentTask.TaskEndDateTime = CurrentTask.NonServiceTypeKey != null
        //        ? TaskEndDate
        //        : CurrentTask.TaskEndDateTime = null;
        //    return true;
        //}

        //private void SaveTask()
        //{
        //    if (EntityManager.Current.IsOnline && TaskContext.HasChanges)
        //    {
        //        IsBusy = true;
        //        if (CurrentTask.NonServiceTypeKey.HasValue)
        //        {
        //            CurrentTask.AdmissionKey = null;
        //        }

        //        //Moved out of setter for VM.UserId - where it was toggling Encounter.EncounterBy from a Guid to NULL and back when changing ServiceTypeKey
        //        //By placing the code here - only 'sync' the EncounterBy once if there was a change to the Task.  This will minimize updates to Encounter
        //        if (CurrentTask.Encounter != null)
        //        {
        //            Encounter enc = CurrentTask.Encounter.FirstOrDefault();
        //            if (enc != null)
        //            {
        //                //FYI - J.E. 11/8/2017 - Neither myself nor Bernie know why the code is sync'ing Task.UserId to Encounter.EncounterBy
        //                //Only allow set if not deleting task - can only set Cancel fields on Task when deleting a Task
        //                //A better fix to this would require a complete refactor to the UI and VM
        //                //Need a UI specific to the function of 'cancel a task' - with a VM that only updated minimum data
        //                if ((enc.EncounterBy.Equals(UserID) == false) && DeletingTask == false)
        //                {
        //                    enc.EncounterBy = UserID; //only trigger an update to Encounter if the value is different
        //                }
        //            }
        //        }

        //        Metrics.Ria.ChangeSetLogger.Log(
        //            CorrelationID,
        //            TaskContext,
        //            new List<string> { "Task", "Encounter" },
        //            EntityManager.Current.IsOnline,
        //            "TaskEditVM.SaveTask",
        //            "/TaskEdit");

        //        TaskContext.SubmitChanges(so =>
        //        {
        //            if (so.Error != null)
        //            {
        //                var _ErrorResults = new List<String>();
        //                _ErrorResults.Add(so.Error.Message);
        //                foreach (var entity in so.EntitiesInError)
        //                {
        //                    foreach (var err in entity.ValidationErrors)
        //                        _ErrorResults.Add(err.ErrorMessage);
        //                }

        //                foreach (var err in _ErrorResults)
        //                    MessageBox.Show(err);
        //            }

        //            IsBusy = false;
        //            CloseDialog(true);
        //        }, null);
        //    }
        //}

        //private List<TaskPatient> _FilteredPatientCollection = new List<TaskPatient>();

        //public List<TaskPatient> FilteredPatientCollection
        //{
        //    get { return _FilteredPatientCollection; }
        //    set
        //    {
        //        _FilteredPatientCollection = value;
        //        this.RaisePropertyChangedLambda(p => p.FilteredPatientCollection);
        //    }
        //}

        private CollectionViewSource _FilteredLookupCollection = new CollectionViewSource();

        public CollectionViewSource FilteredLookupCollection
        {
            get { return _FilteredLookupCollection; }
            set
            {
                _FilteredLookupCollection = value;
                this.RaisePropertyChangedLambda(p => p.FilteredLookupCollection);
            }
        }

        private CollectionViewSource _FilteredUserCollection = new CollectionViewSource();

        public CollectionViewSource FilteredUserCollection
        {
            get { return _FilteredUserCollection; }
            set
            {
                _FilteredUserCollection = value;
                this.RaisePropertyChangedLambda(p => p.FilteredUserCollection);
            }
        }

        private bool overrideTaskInfo;

        public bool OverrideTaskInfo
        {
            get { return overrideTaskInfo; }
            set
            {
                overrideTaskInfo = value;
                this.RaisePropertyChangedLambda(p => p.OverrideTaskInfo);
            }
        }

        private int? __PatientKeyBackingField;

        //public int? PatientKey
        //{
        //    get { return __PatientKeyBackingField; }
        //    set
        //    {
        //        if (AskingPatientCommentChangeComfirmationQuestion(value))
        //        {
        //            return;
        //        }

        //        if ((value != __PatientKeyBackingField) && (SetupingUpKeys == false))
        //        {
        //            GetPatientComments(
        //                value); // load new patient comments on change of patient - it will then ContinueWithPatientKeyChange
        //        }
        //        else
        //        {
        //            ContinueWithPatientKeyChange(value);
        //        }
        //    }
        //}

        //private void ContinueWithPatientKeyChange(int? newKeyValue)
        //{
        //    int? prevPatientKey = __PatientKeyBackingField;
        //    __PatientKeyBackingField = newKeyValue;
        //    this.RaisePropertyChangedLambda(p => p.PatientKey);
        //    if (SelectedTaskPatient != null && SelectedTaskPatient.TaskAdmissions != null &&
        //        SelectedTaskPatient.TaskAdmissions.Count() == 1)
        //    {
        //        var __taskAdmission = SelectedTaskPatient.TaskAdmissions.FirstOrDefault();
        //        if (__taskAdmission != null)
        //        {
        //            AdmissionKey = __taskAdmission.AdmissionKey;
        //        }
        //        else
        //        {
        //            AdmissionKey = null;
        //        }
        //    }
        //    else if (SelectedTaskPatient != null && (SelectedTaskPatient.TaskAdmissions == null ||
        //                                             SelectedTaskPatient.TaskAdmissions.Any() == false))
        //    {
        //        if (prevPatientKey != __PatientKeyBackingField)
        //        {
        //            LoadPatientAdmissions();
        //        }
        //    }
        //    else
        //    {
        //        AdmissionKey = null;
        //    }

        //    this.RaisePropertyChangedLambda(p => p.SelectedTaskPatient);
        //    this.RaisePropertyChangedLambda(p => p.AdmissionKey);
        //}

        //private void LoadPatientAdmissions()
        //{
        //    bool includesDischarged = IsSystemAdministrator && IsPatientSelected(InitialPatientKey);
        //    int? searchPatientKey = (IsPatientSelected(SelectedTaskPatient.PatientKey))
        //        ? SelectedTaskPatient.PatientKey
        //        : -99;
        //    //var qry = TaskContext.GetPatientsForTaskCreationTAQuery(!IsSchedulerOrOpsManager, includesDischarged,
        //    //    searchPatientKey, 0);
        //    IsBusy = true;
        //    //TaskContext.Load(qry, OnAdmissionsLoaded, null);
        //}

        //private void OnAdmissionsLoaded(LoadOperation<TaskAdmission> operation)
        //{
        //    try
        //    {
        //        if (SelectedTaskPatient != null && SelectedTaskPatient.TaskAdmissions != null &&
        //            SelectedTaskPatient.TaskAdmissions.Count() == 1)
        //        {
        //            var __taskAdmission = SelectedTaskPatient.TaskAdmissions.FirstOrDefault();
        //            if (__taskAdmission != null)
        //            {
        //                AdmissionKey = __taskAdmission.AdmissionKey;
        //            }
        //            else
        //            {
        //                AdmissionKey = null;
        //            }
        //        }
        //        else
        //        {
        //            AdmissionKey = null;
        //        }
        //    }
        //    finally
        //    {
        //        IsBusy = false;
        //    }
        //}

        //private int? __AdmissionKeyBackingField;

        //public int? AdmissionKey
        //{
        //    get { return __AdmissionKeyBackingField; }
        //    set
        //    {
        //        if ((__AdmissionKeyBackingField == value) && (__AdmissionKeyBackingField != null))
        //        {
        //            return;
        //        }

        //        __AdmissionKeyBackingField = value;

        //        SetSelectedAdmission();

        //        FilterAvailableTaskList();
        //        FilterAvailableUsers();
        //        this.RaisePropertyChangedLambda(p => p.AdmissionKey);

        //        // Check if last activity key is in view - it will always be in the source collection...
        //        // Always clear the task and make them re-pick
        //        LookupKey = "";
        //        this.RaisePropertyChangedLambda(p => p.LookupKey);
        //    }
        //}

        //public TaskAdmission SelectedAdmission { get; set; }
        public DateTime? goLiveDate { get; set; }

        //public void SetSelectedAdmission()
        //{
        //    goLiveDate = null;
        //    if (AdmissionKey <= 0)
        //    {
        //        SelectedAdmission = null;
        //        return;
        //    }

        //    if (SelectedTaskPatient == null)
        //    {
        //        SelectedAdmission = null;
        //        return;
        //    }

        //    if (SelectedTaskPatient.TaskAdmissions == null)
        //    {
        //        SelectedAdmission = null;
        //        return;
        //    }

        //    SelectedAdmission = SelectedTaskPatient.TaskAdmissions.Where(ta => ta.AdmissionKey == AdmissionKey)
        //        .FirstOrDefault();
        //    if (SelectedAdmission != null)
        //    {
        //        goLiveDate =
        //            ServiceLineCache.Current.GoLiveDateForAdmission(SelectedAdmission,
        //                SelectedAdmission.ServiceLineKey);
        //    }

        //    ;
        //}

        //public TaskPatient SelectedTaskPatient
        //{
        //    get
        //    {
        //        if (PatientKey == null)
        //        {
        //            return null;
        //        }

        //        if (FilteredPatientCollection == null)
        //        {
        //            return null;
        //        }

        //        TaskPatient tp = FilteredPatientCollection.Where(pl => pl.PatientKey == PatientKey).FirstOrDefault();
        //        return tp;
        //    }
        //}

        private Guid? __UserIDBackingField;

        //public Guid? UserID
        //{
        //    get { return __UserIDBackingField; }
        //    set
        //    {
        //        bool userChanged = (__UserIDBackingField == value) ? false : true;
        //        __UserIDBackingField = value;
        //        if (CurrentTask.Encounter != null)
        //        {
        //            Encounter enc = CurrentTask.Encounter.FirstOrDefault();
        //            if (enc != null)
        //            {
        //                //only allow set if not deleting task - can only set Cancel fields on Task when deleting a Task
        //                //A better fix to this would require a complete refactor to the UI and VM
        //                //Need a UI specific to the function of 'cancel a task' - with a VM that only updated minimum data
        //                if ((enc.EncounterBy.Equals(value) == false) && DeletingTask == false)
        //                {
        //                    enc.EncounterBy = value; //only trigger an update to Encounter if the value is different
        //                }
        //            }
        //        }

        //        this.RaisePropertyChangedLambda(p => p.UserID);
        //        if (userChanged)
        //        {
        //            FilterAvailableTaskList();
        //        }
        //    }
        //}

        private bool _UserCanVisitErrorVisible;

        //public bool UserCanVisitErrorVisible
        //{
        //    get { return _UserCanVisitErrorVisible; }
        //    set
        //    {
        //        _UserCanVisitErrorVisible = value;

        //        UserVisible = !(_UserCanVisitErrorVisible);

        //        this.RaisePropertyChangedLambda(p => p.UserCanVisitErrorVisible);
        //        this.RaisePropertyChangedLambda(p => p.UserLabelVisible);
        //    }
        //}

        private bool _UserVisible;

        //public bool UserVisible
        //{
        //    get { return _UserVisible; }
        //    set
        //    {
        //        _UserVisible = value;
        //        this.RaisePropertyChangedLambda(p => p.UserVisible);
        //        this.RaisePropertyChangedLambda(p => p.UserLabelVisible);
        //        this.RaisePropertyChangedLambda(p => p.UserCanChangeCaregiver);
        //    }
        //}

        //public bool UserCanChangeCaregiver
        //{
        //    get
        //    {
        //        // only allow the user to be changed if the user is sys admin, Schduler or Foreign scheduling isn't in use
        //        var ret = UserVisible && (IsSystemAdministrator || IsScheduler ||
        //                                  !TenantSettingsCache.Current.TenantSettingIsForeignSchedulingInUse);
        //        return ret;
        //    }
        //}

        //public bool UserLabelVisible
        //{
        //    get
        //    {
        //        if (UserCanVisitErrorVisible)
        //        {
        //            return false;
        //        }

        //        return UserVisible;
        //    }
        //}

        //private TaskAdmission _CurrentSelectedAdmit;

        //public TaskAdmission CurrentSelectedAdmit
        //{
        //    get { return _CurrentSelectedAdmit; }
        //    set
        //    {
        //        if (_CurrentSelectedAdmit != value)
        //        {
        //            _CurrentSelectedAdmit = value;
        //            this.RaisePropertyChangedLambda(p => p.CurrentSelectedAdmit);
        //        }
        //    }
        //}

        //private Task _CurrentTask;

        //public Task CurrentTask
        //{
        //    get { return _CurrentTask; }
        //    set
        //    {
        //        if (_CurrentTask != value)
        //        {
        //            _CurrentTask = value;
        //            this.RaisePropertyChangedLambda(p => p.CurrentTask);
        //        }
        //    }
        //}

        private int? _CancelReasonKey;

        public int? CancelReasonKey
        {
            get { return _CancelReasonKey; }
            set
            {
                ClearErrorFromProperty("CancelReasonKey");

                _CancelReasonKey = value;

                if (DeletingTask)
                {
                    if (!_CancelReasonKey.HasValue || _CancelReasonKey.Value == 0)
                    {
                        AddErrorForProperty("CancelReasonKey", "Cancel reason is required");
                    }
                }

                SaveTaskCommand.RaiseCanExecuteChanged();

                this.RaisePropertyChangedLambda(p => p.CancelReasonKey);
            }
        }

        private string __LookupKeyBackingField;

        //public string LookupKey
        //{
        //    get { return __LookupKeyBackingField; }
        //    set
        //    {
        //        ClearErrorFromProperty("LookupKey");

        //        if (__LookupKeyBackingField != value)
        //        {
        //            Deployment.Current.Dispatcher.BeginInvoke(() =>
        //            {
        //                FilterAvailableUsers();
        //                RaisePropertyChanged("FilteredUserCollectionView");
        //            });
        //        }

        //        __LookupKeyBackingField = value;

        //        if (String.IsNullOrEmpty(__LookupKeyBackingField))
        //        {
        //            AddErrorForProperty("LookupKey",
        //                "Activity is required"); //FYI: LookupKey is a non-patient related activity or a patient related service type...
        //        }
        //        else
        //        {
        //            LookupPM lpm = FilteredLookupCollection.View.OfType<LookupPM>().Where(c => c.LookupKey == value)
        //                .FirstOrDefault();
        //            TaskDuration = (lpm == null) ? 0 : lpm.Duration.Value;
        //        }

        //        SaveTaskCommand.RaiseCanExecuteChanged();

        //        this.RaisePropertyChangedLambda(p => p.LookupKey);
        //        this.RaisePropertyChangedLambda(p => p.IsNonService);
        //        this.RaisePropertyChangedLambda(p => p.MileageIsVisible);
        //        if ((MileageIsVisible) && (CurrentTask != null) && (CurrentTask.DistanceScale == null))
        //        {
        //            CurrentTask.DistanceScale = TenantSettingsCache.Current.TenantSettingDistanceTraveledMeasure;
        //        }
        //    }
        //}

        private string _Notes;

        public string Notes
        {
            get { return _Notes; }
            set
            {
                _Notes = value;
                this.RaisePropertyChangedLambda(p => p.Notes);
            }
        }

        private int _TaskDuration;

        public int TaskDuration
        {
            get { return _TaskDuration; }
            set
            {
                _TaskDuration = value;
                this.RaisePropertyChangedLambda(p => p.TaskDuration);
                //CalculateTimeFromDuration();
            }
        }

        private DateTimeOffset _TaskStartDate;

        //[Display(Name = "Start Date/Time")]
        //[CustomValidation(typeof(DateValidations), "DateTimeOffsetValid")]
        //public DateTimeOffset TaskStartDate
        //{
        //    get { return _TaskStartDate; }
        //    set
        //    {
        //        ClearErrorFromProperty("TaskStartDate");
        //        _TaskStartDate = value;

        //        if (_TaskStartDate > DateTimeOffset.Now.AddMonths(18))
        //        {
        //            AddErrorForProperty("TaskStartDate",
        //                "A task cannot be created for more than 540 days in the future");
        //        }

        //        if (TenantSettingsCache.Current.TenantSettingIsForeignSchedulingInUse)
        //        {
        //            FilterAvailableTaskList();
        //        }

        //        bool hasErrors = false;
        //        ICollection<ValidationResult> validationResults = new List<ValidationResult>();
        //        if (Validator.TryValidateObject(this, TaskContext.ValidationContext, validationResults, true) == false)
        //        {
        //            foreach (ValidationResult error in validationResults)
        //                if (error.MemberNames.Any(n => n.Equals("TaskStartDate")))
        //                {
        //                    AddErrorForProperty("TaskStartDate",
        //                        error.ErrorMessage);
        //                    hasErrors = true;
        //                }
        //        }

        //        this.RaisePropertyChangedLambda(p => p.TaskStartDate);
        //        if (IsNonService)
        //        {
        //            _TaskDuration = (hasErrors) ? 0 : CalculateTaskDuration();
        //            this.RaisePropertyChangedLambda(p => p.TaskDuration);
        //        }
        //    }
        //}

        private DateTimeOffset? _TaskEndDate;

        //[Display(Name = "End Date/Time")]
        //[CustomValidation(typeof(DateValidations), "DateTimeOffsetValid")]
        //public DateTimeOffset? TaskEndDate
        //{
        //    get { return _TaskEndDate; }
        //    set
        //    {
        //        ClearErrorFromProperty("TaskEndDate");
        //        _TaskEndDate = value;

        //        bool hasErrors = false;
        //        ICollection<ValidationResult> validationResults = new List<ValidationResult>();
        //        if (Validator.TryValidateObject(this, TaskContext.ValidationContext, validationResults, true) == false)
        //        {
        //            foreach (ValidationResult error in validationResults)
        //                if (error.MemberNames.Any(n => n.Equals("TaskEndDate")))
        //                {
        //                    AddErrorForProperty("TaskEndDate", error.ErrorMessage);
        //                    hasErrors = true;
        //                }
        //        }

        //        DefaultTaskEndDateIfMinValue();
        //        this.RaisePropertyChangedLambda(p => p.TaskEndDate);
        //        if (IsNonService)
        //        {
        //            _TaskDuration = (hasErrors) ? 0 : CalculateTaskDuration();
        //            this.RaisePropertyChangedLambda(p => p.TaskDuration);
        //        }
        //    }
        //}

        //private void DefaultTaskEndDateIfMinValue()
        //{
        //    if (IsNonService == false)
        //    {
        //        return;
        //    }

        //    if (_TaskEndDate == null)
        //    {
        //        return;
        //    }

        //    if ((((DateTimeOffset)_TaskEndDate).Date != new DateTime(1, 1, 1)) &&
        //        (((DateTimeOffset)_TaskEndDate).Date != new DateTime(1900, 1, 1)))
        //    {
        //        return;
        //    }

        //    _TaskEndDate = new DateTime(TaskStartDate.Year, TaskStartDate.Month, TaskStartDate.Day,
        //        _TaskEndDate.Value.Hour, _TaskEndDate.Value.Minute, _TaskEndDate.Value.Second);
        //}

        //private int CalculateTaskDuration()
        //{
        //    if ((TaskStartDate != null) && (TaskStartDate.DateTime != DateTime.MinValue) && (TaskEndDate != null) &&
        //        (((DateTimeOffset)TaskEndDate).DateTime != DateTime.MinValue))
        //    {
        //        return (int)TaskEndDate.Value.DateTime.Subtract(TaskStartDate.DateTime).TotalMinutes;
        //    }

        //    return 0;
        //}

        private DateTimeHelper _DateTimeHelper = new DateTimeHelper();

        public DateTimeHelper DateTimeHelper
        {
            get { return _DateTimeHelper; }
            set
            {
                _DateTimeHelper = value;
                this.RaisePropertyChangedLambda(p => p.DateTimeHelper);
            }
        }

        private bool _IsBusy;

        public bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                _IsBusy = value;
                this.RaisePropertyChangedLambda(p => p.IsBusy);
            }
        }

        private bool _DeletingTask;

        public bool DeletingTask
        {
            get { return _DeletingTask; }
            set
            {
                _DeletingTask = value;
                this.RaisePropertyChangedLambda(p => p.DeletingTask);
            }
        }

        //public bool IsNonService => ((PatientKey.HasValue) && (PatientKey > 0)) ? false : true;

        //public bool MileageIsVisible
        //{
        //    get
        //    {
        //        if (CurrentTask != null && CurrentTask.Distance > 0)
        //        {
        //            return true;
        //        }

        //        if (!IsNonService)
        //        {
        //            return false;
        //        }

        //        if (FilteredLookupCollection == null)
        //        {
        //            return false;
        //        }

        //        if (LookupKey == null)
        //        {
        //            return false;
        //        }

        //        if (LookupKey.Contains("-SERVICE"))
        //        {
        //            return false;
        //        }

        //        LookupPM lpm = FilteredLookupCollection.View.OfType<LookupPM>().Where(c => c.LookupKey == LookupKey)
        //            .FirstOrDefault();
        //        if (lpm != null)
        //        {
        //            var nst = NonServiceTypeCache.GetNonServiceTypeFromKey(lpm.DatabaseKey);
        //            if (nst != null && nst.IncMileage)
        //            {
        //                return true;
        //            }
        //        }

        //        return false;
        //    }
        //}

        //public void CalculateTimeFromDuration()
        //{
        //    if (LookupKey != null && LookupKey.Length > 2)
        //    {
        //        var lookup_key_value = LookupKey.Split('-');
        //        if (lookup_key_value[1].Equals("SERVICE"))
        //        {
        //            return; // Don't calculate the end date for normal encounters, only non service time.
        //        }

        //        if (TaskStartDate != null && TaskDuration > 0)
        //        {
        //            TaskEndDate = TaskStartDate.AddMinutes(TaskDuration);
        //        }
        //        else
        //        {
        //            TaskEndDate = null;
        //        }

        //        this.RaisePropertyChangedLambda(p => p.TaskEndDate);
        //    }
        //}

        public RelayCommand SaveTaskCommand { get; protected set; }

        public RelayCommand CloseTaskCommand { get; protected set; }

        //public bool Validate()
        //{
        //    bool AllValid = true;
        //    if (CurrentTask.Distance == null)
        //    {
        //        CurrentTask.DistanceScale = null;
        //    }

        //    if (CurrentTask.Distance != null && CurrentTask.DistanceScale == null)
        //    {
        //        MessageBox.Show("Please select a distance scale.");
        //        AllValid = false;
        //    }

        //    if (IsNonService && TaskDuration > 1440)
        //    {
        //        // Do not allow the user to key in duration >24 hours for non service types, caurni00 - 1/28/2015 - 14803
        //        var Msg = "Non-Service Types duration cannot cannot exceed 24 hours (1440 minutes).";
        //        MessageBox.Show(Msg);
        //        AllValid = false;
        //    }

        //    if (IsNonService && TaskDuration < 0)
        //    {
        //        // Do not allow the user to key in duration >24 hours for non service types, caurni00 - 1/28/2015 - 14803
        //        var Msg = "Non-Service Types duration must be greater then 0.";
        //        MessageBox.Show(Msg);
        //        AllValid = false;
        //    }

        //    if (AllValid)
        //    {
        //        ValidateComments();
        //    }

        //    return AllValid;
        //}

        bool? _dialogResult;

        public bool? DialogResult
        {
            get { return _dialogResult; }
            set
            {
                _dialogResult = value;
                RaisePropertyChanged("DialogResult");
            }
        }

        //private void CloseDialog(bool ret)
        //{
        //    if (IsBusy)
        //    {
        //        return;
        //    }

        //    Logger = null;

        //    Messenger.Default.Unregister<bool>(this, Constants.Messaging.NetworkAvailability);
        //    Messenger.Default.Unregister(this);

        //    CommandManager.CleanUp();
        //    CommandManager = null;

        //    FilteredPatientCollection = null;
        //    FilteredLookupCollection.Source = null;
        //    FilteredUserCollection.Source = null;

        //    TaskContext.EntityContainer.Clear();

        //    TaskContext = null;

        //    base.Cleanup();

        //    DialogResult = ret;
        //}

        //#region TaskComments

        //private IEnumerable<CommentItem> _CommentItems;

        //public IEnumerable<CommentItem> CommentItems
        //{
        //    get { return _CommentItems; }
        //    set
        //    {
        //        if (_CommentItems == value)
        //        {
        //            return;
        //        }

        //        _CommentItems = value;
        //        this.RaisePropertyChangedLambda(p => p.CommentItems);
        //        this.RaisePropertyChangedLambda(p => p.CommentTabLabel);
        //    }
        //}

        //private CommentItem _CurrentCommentItem;

        //public CommentItem CurrentCommentItem
        //{
        //    get { return _CurrentCommentItem; }
        //    set
        //    {
        //        if (_CurrentCommentItem == value)
        //        {
        //            return;
        //        }

        //        _CurrentCommentItem = value;
        //        this.RaisePropertyChangedLambda(p => p.CurrentCommentItem);
        //    }
        //}

        //public string CommentTabLabel
        //{
        //    get
        //    {
        //        int _CommentCount = (CommentItems == null) ? 0 : CommentItems.Count();
        //        if (_CommentCount > 0)
        //        {
        //            return "Comments - " + _CommentCount;
        //        }

        //        return "Comments - None";
        //    }
        //}

        //public RelayCommand AddVisitComment_Command { get; protected set; }
        //public RelayCommand AddImportantComment_Command { get; protected set; }
        //public RelayCommand<CommentItem> DeleteComment_Command { get; protected set; }

        //private void SetupTaskCommentCommands()
        //{
        //    AddVisitComment_Command = new RelayCommand(() => { AddVisitCommentCommand(); });
        //    AddImportantComment_Command = new RelayCommand(() => { AddImportantCommentCommand(); });
        //    DeleteComment_Command = new RelayCommand<CommentItem>(item => { DeleteCommentCommand(item); });
        //}

        //public void AddVisitCommentCommand()
        //{
        //    AddComment(new CommentItem(new TaskComment
        //    { TaskKey = CurrentTask.TaskKey, EntryDateTime = DateTime.Now }));
        //}

        //public void AddImportantCommentCommand()
        //{
        //    if (PatientKey < 1)
        //    {
        //        return;
        //    }

        //    PatientComment pc = new PatientComment { EntryDateTime = DateTime.Now, ImportantComment = true };
        //    pc.BeginEditting();
        //    AddComment(new CommentItem(pc));
        //}

        //private void AddComment(CommentItem newItem)
        //{
        //    List<CommentItem> commentItems = new List<CommentItem>();
        //    commentItems.Add(newItem);
        //    if (CommentItems != null)
        //    {
        //        foreach (CommentItem ci in CommentItems) commentItems.Add(ci);
        //    }

        //    CommentItems = null;
        //    Deployment.Current.Dispatcher.BeginInvoke(() => { CommentItems = commentItems; });
        //}

        //public void DeleteCommentCommand(CommentItem item)
        //{
        //    if (item == null)
        //    {
        //        return;
        //    }

        //    CurrentCommentItem = item;
        //    if (string.IsNullOrWhiteSpace(CurrentCommentItem.Comment))
        //    {
        //        DeleteComment(CurrentCommentItem);
        //        return;
        //    }

        //    NavigateCloseDialog d = new NavigateCloseDialog();
        //    if (d == null)
        //    {
        //        return;
        //    }

        //    d.Width = double.NaN;
        //    d.Height = double.NaN;
        //    d.ErrorMessage = CommentThumbNail(item.Comment);
        //    d.ErrorQuestion = "Delete this Comment?";
        //    d.Title = "Delete Comment Confirmation";
        //    d.HasCloseButton = false;
        //    d.Closed += OnDeleteCommentClosed;
        //    d.Show();
        //}

        //private static string CR = char.ToString('\r');

        //private string CommentThumbNail(string comment)
        //{
        //    string s = comment.Trim();
        //    if (string.IsNullOrWhiteSpace(s))
        //    {
        //        return "<no comment>";
        //    }

        //    if (s.Contains(CR))
        //    {
        //        s = s.Substring(0, s.IndexOf(CR));
        //        if (s.Length > 40)
        //        {
        //            s = s.Substring(0, 40);
        //        }

        //        return s + "...";
        //    }

        //    if (s.Length > 40)
        //    {
        //        s = s.Substring(0, 40) + "...";
        //    }

        //    return s;
        //}

        //private void OnDeleteCommentClosed(object s, EventArgs err)
        //{
        //    var dialog = (ChildWindow)s;
        //    dialog.Closed -= OnDeleteCommentClosed;

        //    if (dialog.DialogResult == false)
        //    {
        //        return;
        //    }

        //    if (CurrentCommentItem != null)
        //    {
        //        DeleteComment(CurrentCommentItem);
        //    }
        //}

        //private void DeleteComment(CommentItem deleteItem)
        //{
        //    deleteItem.Deleted = true;
        //    List<CommentItem> commentItems = new List<CommentItem>();
        //    if (CommentItems != null)
        //    {
        //        foreach (CommentItem ci in CommentItems)
        //            if (ci != deleteItem)
        //            {
        //                commentItems.Add(ci);
        //            }
        //    }

        //    CommentItems = commentItems;
        //}

        //private Patient CurrentCommentPatient;

        //private void LoadComments(Patient pPatient, bool initialLoad)
        //{
        //    CurrentCommentPatient = pPatient;
        //    List<CommentItem> commentItems = new List<CommentItem>();
        //    if ((CurrentTask != null) && (CurrentTask.TaskComment != null))
        //    {
        //        if (initialLoad) // Initially load from data source
        //        {
        //            foreach (TaskComment tc in CurrentTask.TaskComment)
        //                if (tc.Deleted == false)
        //                {
        //                    commentItems.Add(new CommentItem(tc));
        //                }
        //        }
        //        else // otherwise load back from the UI collection currently in edit
        //        {
        //            if (CommentItems != null)
        //            {
        //                foreach (CommentItem ci in CommentItems)
        //                    if (ci.IsPatient == false)
        //                    {
        //                        commentItems.Add(ci);
        //                    }
        //            }
        //        }
        //    }

        //    if ((CurrentCommentPatient != null) && (CurrentCommentPatient.PatientComment != null))
        //    {
        //        foreach (PatientComment pc in CurrentCommentPatient.PatientComment)
        //        {
        //            pc.BeginEditting();
        //            if ((pc.Deleted == false) && pc.ImportantComment)
        //            {
        //                commentItems.Add(new CommentItem(pc));
        //            }
        //        }
        //    }

        //    CommentItems = commentItems;
        //}

        //private bool PatientCommentChanges
        //{
        //    get
        //    {
        //        if (CommentItems == null)
        //        {
        //            return false;
        //        }

        //        foreach (CommentItem ci in CommentItems)
        //        {
        //            if (ci.IsPatient == false)
        //            {
        //                continue;
        //            }

        //            if (ci.IsNew && ci.Deleted)
        //            {
        //                continue;
        //            }

        //            if (ci.IsNew && string.IsNullOrWhiteSpace(ci.Comment))
        //            {
        //                continue;
        //            }

        //            if ((ci.IsNew == false) && (ci.HasChanges == false))
        //            {
        //                continue;
        //            }

        //            return true;
        //        }

        //        return false;
        //    }
        //}

        //private int? __ChangeToPatientKey;

        //private bool AskingPatientCommentChangeComfirmationQuestion(int? changeToPatientKey)
        //{
        //    if (__ChangeToPatientKey == changeToPatientKey)
        //    {
        //        return true; // skip double load
        //    }

        //    __ChangeToPatientKey = changeToPatientKey;
        //    if (PatientCommentChanges == false)
        //    {
        //        return false;
        //    }

        //    NavigateCloseDialog d = new NavigateCloseDialog();
        //    if (d == null)
        //    {
        //        return false;
        //    }

        //    d.Width = double.NaN;
        //    d.Height = double.NaN;
        //    d.ErrorMessage = "Patient Comments have been added or modified.";
        //    d.ErrorQuestion = "Discard these changes?";
        //    d.Title = "Change Patient Confirmation";
        //    d.HasCloseButton = false;
        //    d.Closed += OnPatientChangeConfirmationClosed;
        //    d.Show();
        //    return true;
        //}

        //private void OnPatientChangeConfirmationClosed(object s, EventArgs err)
        //{
        //    var dialog = (ChildWindow)s;
        //    dialog.Closed -= OnPatientChangeConfirmationClosed;
        //    if (dialog.DialogResult == false)
        //    {
        //        this.RaisePropertyChangedLambda(p => p.PatientKey); // set the patient back
        //        return;
        //    }

        //    GetPatientComments(__ChangeToPatientKey);
        //}

        //private void GetPatientComments(int? __ChangeToPatientKey)
        //{
        //    CurrentPatientCommentCancelEditting();
        //    if (__ChangeToPatientKey ==
        //        null) // dont fetch PatientComments from server if going back to a NonServiceime task
        //    {
        //        LoadComments(null, false);
        //        ContinueWithPatientKeyChange(__ChangeToPatientKey);
        //        return;
        //    }

        //    var qry = TaskContext.GetPatientCommentByPatientKeyQuery((int)__ChangeToPatientKey);
        //    IsBusy = true;
        //    TaskContext.Load(qry, OnGetPatientCommentByPatientKeyLoaded, null);
        //}

        //private void OnGetPatientCommentByPatientKeyLoaded(LoadOperation<Patient> operation)
        //{
        //    try
        //    {
        //        Patient pPatient = TaskContext.Patients.Where(p => p.PatientKey == (int)__ChangeToPatientKey)
        //            .FirstOrDefault() ?? null;
        //        LoadComments(pPatient, false);
        //        IsBusy = false;
        //        ContinueWithPatientKeyChange(__ChangeToPatientKey);
        //    }
        //    finally
        //    {
        //        IsBusy = false;
        //    }
        //}

        //private void CurrentPatientCommentCancelEditting()
        //{
        //    if ((CurrentCommentPatient != null) && (CurrentCommentPatient.PatientComment != null))
        //    {
        //        foreach (PatientComment pc in CurrentCommentPatient.PatientComment)
        //            try
        //            {
        //                pc.CancelEditting();
        //            }
        //            catch
        //            {
        //            }
        //    }
        //}

        //private void ValidateComments()
        //{
        //    if (IsNonService == false)
        //    {
        //        CurrentTask.Notes = null;
        //    }

        //    if (CommentItems == null)
        //    {
        //        return;
        //    }

        //    foreach (CommentItem ci in CommentItems)
        //    {
        //        if (ci.IsNew && ci.Deleted)
        //        {
        //            continue;
        //        }

        //        if (ci.IsNew && string.IsNullOrWhiteSpace(ci.Comment))
        //        {
        //            continue;
        //        }

        //        if (ci.CommentObject == null)
        //        {
        //            continue;
        //        }

        //        if ((ci.IsPatient) && (PatientKey > 0))
        //        {
        //            PatientComment pc = ci.CommentObject as PatientComment;
        //            if (pc == null)
        //            {
        //                continue;
        //            }

        //            pc.Comment = pc.Comment.Trim();
        //            pc.PatientKey = (int)PatientKey;
        //            if (pc.HasChanges)
        //            {
        //                pc.EntryDateTime = DateTime.Now;
        //            }

        //            if (pc.IsNew)
        //            {
        //                TaskContext.PatientComments.Add(pc);
        //                try
        //                {
        //                    pc.EndEditting();
        //                }
        //                catch
        //                {
        //                }
        //            }
        //        }
        //        else if (ci.IsPatient == false)
        //        {
        //            TaskComment tc = ci.CommentObject as TaskComment;
        //            if (tc == null)
        //            {
        //                continue;
        //            }

        //            tc.Comment = tc.Comment.Trim();
        //            tc.TaskKey = CurrentTask.TaskKey;
        //            if (tc.HasChanges)
        //            {
        //                tc.EntryDateTime = DateTime.Now;
        //            }

        //            if (ci.IsNew)
        //            {
        //                CurrentTask.TaskComment.Add(tc);
        //            }
        //        }
        //    }

        //    if ((CurrentCommentPatient != null) && (CurrentCommentPatient.PatientComment != null))
        //    {
        //        foreach (PatientComment pc in CurrentCommentPatient.PatientComment)
        //            try
        //            {
        //                pc.EndEditting();
        //            }
        //            catch
        //            {
        //            }
        //    }
        //}

        //#endregion TaskComments
    }
}