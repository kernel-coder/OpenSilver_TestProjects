#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Ria.Sync;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Occasional;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IDynamicFormService))]
    public class DynamicFormService : PatientService, IDynamicFormService, IServiceProvider
    {
        static readonly LogWriter logWriter;

        static DynamicFormService()
        {
            logWriter = EnterpriseLibraryContainer.Current.GetInstance<LogWriter>();
        }

        public static int? FindQuestionID(FormSection formsection, int pqgkey, int qgkey, int sequence, Question q)
        {
            var formSectionKey = formsection?.FormSectionKey ?? -111;
            var questionKey = q?.QuestionKey ?? -111;

            Log(
                $"FindQuestionID[001]: FormSectionKey={formSectionKey}, pqgkey={pqgkey}, qgkey={qgkey}, sequence={sequence}, QuestionKey={questionKey}",
                "WS_TRACE");

            try
            {
                if (q == null || q.QuestionKey < 1)
                {
                    return Constants.DynamicForm.NonValidFormSectionQuestionKey;
                }

                var __FormSectionQuestionKey = formsection?.FormSectionQuestion
                    ?.Where(fsq => fsq.QuestionKey == q.QuestionKey).Select(fsq => fsq.FormSectionQuestionKey).First();

                return __FormSectionQuestionKey;
            }
            catch (Exception e)
            {
                Log($"FindQuestionID[001]: exception={e.Message}", "WS_TRACE", TraceEventType.Error);

                return Constants.DynamicForm.NonValidFormSectionQuestionKey;
            }
        }

        public DomainContext GetContext()
        {
            return Context;
        }

        SimpleServiceProvider ServiceProvider { get; set; }

        public event EventHandler<SHPAlertsRequestArgs> OnSHPAlertsRequestLoaded;

        private int CurrentTaskKey;

        [ImportingConstructor]
        public DynamicFormService(IUriService _uriService = null)
            : base(_uriService)
        {
            ServiceProvider = new SimpleServiceProvider();
            ServiceProvider.AddService<IVirtuosoContextProvider>(new VirtuosoContextProvider(Context));
        }

        private int _CurrentFormKey = -1;

        public int CurrentFormKey
        {
            get { return _CurrentFormKey; }
            set
            {
                _CurrentFormKey = value;
                RaisePropertyChanged("CurrentForm");
            }
        }

        public List<ChangeHistory> OrdersTrackingChangeHistory => Context.ChangeHistories.ToList();
        public Form CurrentForm => DynamicFormCache.GetFormByKey(CurrentFormKey);

        public Patient CurrentPatient => Context.Patients.FirstOrDefault();

        public Admission CurrentAdmission => Context.Admissions.FirstOrDefault();

        public Task CurrentTask
        {
            get { return Context.Tasks.FirstOrDefault(p => p.TaskKey == CurrentTaskKey); }
        }

        public List<AuthOrderTherapyPOCO_CView> CurrentAuthMappings => Context.AuthOrderTherapyPOCO_CViews.ToList();

        #region PagedModelBase Members

        public override void LoadData()
        {
            if (IsLoading || Context == null)
            {
                return;
            }

            IsLoading = true;

            GetAsync();
        }

        #endregion

        #region IModelDataService<Entity> Members

        public void GetAsyncByKeys(int patientkey, int admissionkey, int formkey, int taskkey, bool takeOffline)
        {
            CurrentTaskKey = taskkey;

            Context.RejectChanges();
            Context.EntityContainer.Clear();
            //TaskContext.RejectChanges();
            //TaskContext.EntityContainer.Clear();

            IsLoading = true;

            CurrentFormKey = formkey;

            var patquery =
                Context.GetEncounterHistoryByAdmissionKeyQuery(patientkey, admissionkey, taskkey, takeOffline);

            var insauthquery = Context.GetInsuranceAuthOrderTherapyPOCOs_AuthsOnlyQuery(admissionkey);
            var changehistory = Context.GetChangeHistoryForOrdersTrackingStatusQuery(admissionkey);

            //var taskquery = TaskContext.GetTaskByAdmissionQuery(admissionkey, CurrentTaskKey);

            DomainContextLoadBatch batch = new DomainContextLoadBatch(DataLoadComplete);

            batch.Add(Context.Load(patquery, LoadBehavior.RefreshCurrent, false));
            batch.Add(Context.Load(insauthquery, LoadBehavior.RefreshCurrent, false));

            var form = DynamicFormCache.GetFormByKey(formkey);

            if (form != null && form.IsOrderEntry)
            {
                batch.Add(Context.Load(changehistory, LoadBehavior.RefreshCurrent, false));
            }
            //batch.Add(TaskContext.Load<Task>(taskquery, LoadBehavior.RefreshCurrent, false));
        }

        private void DataLoadComplete(DomainContextLoadBatch batch)
        {
            Log("DataLoadComplete[002]", "WS_TRACE");

            List<Exception> LoadErrors = new List<Exception>();

            if (batch.FailedOperationCount > 0)
            {
                foreach (var fop in batch.FailedOperations)
                    if (fop.HasError)
                    {
                        Log($"DataLoadComplete[002]: fop.Error={fop.Error}", "WS_TRACE");
                        LoadErrors.Add(fop.Error);
                    }
            }

            IsLoading = true;

            if (OnMultiLoaded != null)
            {
                Dispatcher.BeginInvoke(() => { OnMultiLoaded(this, new MultiErrorEventArgs(LoadErrors)); });
            }
        }

        public event EventHandler<MultiErrorEventArgs> OnMultiLoaded;

        public void SaveMultiAsync()
        {
            Log("SaveMultiAsync[003]", "WS_TRACE");
            SaveMultiAsync(null);
        }

        public void SaveMultiAsync(Action preSubmitAction)
        {
            Log("SaveMultiAsync[004]", "WS_TRACE");

            IsLoading = true;

            try
            {
                DomainContextSubmitBatch batch = new DomainContextSubmitBatch(DataSubmitComplete);

                if (Context.HasChanges)
                {
                    UpdateCurrentCertCycleBit();
                    if (preSubmitAction != null)
                    {
                        preSubmitAction.Invoke();
                    }

                    batch.Add(
                        Context.SubmitChanges(results =>
                        {
                            if (results.HasError)
                            {
                                results.MarkErrorAsHandled();
                            }
                        }, null)
                    );
                }

                if (batch.PendingOperationCount == 0)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        List<Exception> Errors = new List<Exception>();
                        List<string> EntityErrors = new List<string>();

                        base.OnMultiSavedChanged(new MultiErrorEventArgs(Errors, EntityErrors));
                    });
                }
            }
            catch (Exception e)
            {
                Log($"SaveMultiAsync[004]: exception={e.Message}", "WS_TRACE", TraceEventType.Error);

                IsLoading = false;

                Dispatcher.BeginInvoke(() =>
                {
                    List<Exception> Errors = new List<Exception>();
                    List<string> EntityErrors = new List<string>();
                    Errors.Add(e);
                    base.OnMultiSavedChanged(new MultiErrorEventArgs(Errors, EntityErrors));
                });

                throw;
            }
        }

        private void DataSubmitComplete(DomainContextSubmitBatch batch)
        {
            Log("SaveMultiAsync[005]", "WS_TRACE");

            List<Exception> Errors = new List<Exception>();
            List<string> EntityErrors = new List<string>();

            if (batch.FailedOperationCount > 0)
            {
                foreach (var fop in batch.FailedOperations)
                {
                    if (fop.HasError)
                    {
                        Log($"SaveMultiAsync[005]: fop.Error={fop.Error}", "WS_TRACE");
                        Errors.Add(fop.Error);
                    }

                    foreach (var entity in fop.EntitiesInError)
                    {
                        string name = entity.GetType().Name;
                        foreach (var err in entity.ValidationErrors)
                        {
                            Log($"SaveMultiAsync[005]: entity.ValidationError={name + ": " + err.ErrorMessage}",
                                "WS_TRACE", TraceEventType.Error);
                            //bfmbfm - Derive more beef to the EntityErrors text from the EntitiesInError data - especially w.r.t. EncounterData Question and Section info
                            EntityErrors.Add(name + ": " + err.ErrorMessage);
                        }
                    }
                }
            }

            IsLoading = false;

            Dispatcher.BeginInvoke(() => { base.OnMultiSavedChanged(new MultiErrorEventArgs(Errors, EntityErrors)); });
        }

        public void RejectMultiChanges()
        {
            Log("RejectMultiChanges[006]", "WS_TRACE");

            if (Context != null)
            {
                Context.RejectChanges();
                Context.RejectChanges(); // need to do this twice because the Encounter.EncounterCycleStartDate setter wakes up on the first rejectChanges - changing the AdmissionCertification row - so the context.HasChanges gets reset and the form don't close
            }
            //TaskContext.RejectChanges();
        }

        #endregion

        public new Tuple<string, EntityChangeSet>[] CheckChanges()
        {
            EntityChangeSet changeSet1 = Context.EntityContainer.GetChanges();

            var _ret = new[]
            {
                new Tuple<string, EntityChangeSet>("Context", changeSet1),
            };

            return _ret;
        }

        public new bool ContextHasChanges => Context?.HasChanges ?? false;

        void FormContext_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        void TaskContext_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public new void Cleanup()
        {
            Log("Cleanup[007]", "WS_TRACE");

            base.Cleanup();

            if (Context != null)
            {
                Context.EntityContainer.Clear();
                Context = null;
            }

            VirtuosoObjectCleanupHelper.CleanupAll(this);
        }

        public void getSHPAlertsRequest(string userid, int? OASISHeaderKey, string B1, string casemanager,
            string clinician, string referralsource, string team, string reference, string primarypayername,
            string priorinpatientfacility, object userstate)
        {
            Context.SHPAlertsRequest(userid, OASISHeaderKey, B1, casemanager, clinician, referralsource, team,
                reference, primarypayername, priorinpatientfacility, SHPCompleted, userstate);
        }

        private void SHPCompleted(InvokeOperation<string> result)
        {
            OnSHPAlertsRequestLoaded(result.UserState, new SHPAlertsRequestArgs { Response = result.Value });
        }

        public void RemoveTasksAfterDischarge(DateTime dischargedate, int? disciplinekey = null,
            DateTime? pocTaskCutoffDate = null)
        {
            Log("RemoveTasksAfterDischarge[008]", "WS_TRACE");

            List<Task> tasks = Context.Tasks
                .Where(p => p.TaskKey != CurrentTaskKey &&
                            (p.Encounter == null || (p.Encounter.FirstOrDefault() != null &&
                                                     p.Encounter.FirstOrDefault().EncounterStatus ==
                                                     (int)EncounterStatusType.None)) &&
                            !p.TaskEndDateTime.HasValue &&
                            (!disciplinekey.HasValue || ServiceTypeCache.GetDisciplineKey(p.ServiceTypeKey.Value) ==
                                disciplinekey) &&
                            (!p.ServiceTypeKey.HasValue ||
                             ServiceTypeCache.AllowAfterDischarge(p.ServiceTypeKey.Value) == false) &&
                            p.TaskStartDateTime.DateTime.Date.CompareTo(dischargedate.Date.AddDays(1)) >= 0)
                .ToList();
            if (tasks != null)
            {
                UserProfile up = UserCache.Current.GetCurrentUserProfile();
                foreach (Task item in tasks)
                {
                    var cancelReason = CodeLookupCache.GetKeyFromCode("CancelReason", "Discharge");
                    item.CanceledAt = DateTime.UtcNow;
                    if (up != null)
                    {
                        item.CanceledBy = up.UserId;
                    }

                    item.CancelReasonKey = cancelReason;
                }
            }

            if ((disciplinekey.HasValue == false) && (pocTaskCutoffDate != null))
            {
                tasks = Context.Tasks
                    .Where(p => p.TaskKey != CurrentTaskKey &&
                                (p.Encounter == null || (p.Encounter.FirstOrDefault() != null &&
                                                         p.Encounter.FirstOrDefault().EncounterStatus ==
                                                         (int)EncounterStatusType.None)) &&
                                !p.TaskEndDateTime.HasValue &&
                                p.TaskIsPlanOfCare &&
                                p.TaskStartDateTime.DateTime.Date.CompareTo(((DateTime)pocTaskCutoffDate).Date) >= 0)
                    .ToList();
                if (tasks != null)
                {
                    UserProfile up = UserCache.Current.GetCurrentUserProfile();
                    foreach (Task item in tasks)
                    {
                        var cancelReason = CodeLookupCache.GetKeyFromCode("CancelReason", "Discharge");
                        item.CanceledAt = DateTime.UtcNow;
                        if (up != null)
                        {
                            item.CanceledBy = up.UserId;
                        }

                        item.CancelReasonKey = cancelReason;
                    }
                }
            }
        }

        public void DischargeAllDisciplines(DateTime dischargedate, int? dischargeReason, string SummaryOfCareNarrative)
        {
            Log("DischargeAllDisciplines[009]", "WS_TRACE");
            CurrentAdmission.DischargeAllDisciplines(dischargedate, dischargeReason, SummaryOfCareNarrative);
        }

        public void EndDateAllFCDOrdersForDiscipline(int? disciplineKey, DateTime endDate, bool endDateAll)
        {
            Log("EndDateAllFCDOrdersForDiscipline[010]", "WS_TRACE");
            CurrentAdmission.EndDateAllFCDOrdersForDiscipline(disciplineKey, endDate, endDateAll);
        }

        public void RemoveOrderEntryCoSignature(OrderEntryCoSignature entity)
        {
            Context.OrderEntryCoSignatures.Remove(entity);
        }

        public void RemoveSignature(EncounterSignature entity)
        {
            Context.EncounterSignatures.Remove(entity);
        }

        public void RemovePatientTranslator(PatientTranslator entity)
        {
            Context.PatientTranslators.Remove(entity);
        }

        public void RemoveEncounterReview(EncounterReview entity)
        {
            try
            {
                Context.EncounterReviews.Remove(entity);
            }
            // may already be removed
            catch
            {
            }
        }

        public void RemoveEncounterOasis(EncounterOasis entity)
        {
            try
            {
                Context.EncounterOasis.Remove(entity);
            }
            // may already be removed
            catch
            {
            }
        }

        public void RemoveEncounterAddendum(EncounterAddendum entity)
        {
            try
            {
                Context.EncounterAddendums.Remove(entity);
            }
            // may already be removed
            catch
            {
            }
        }

        public void RemoveEncounterNarrative(EncounterNarrative entity)
        {
            try
            {
                Context.EncounterNarratives.Remove(entity);
            }
            // may already be removed
            catch
            {
            }
        }

        public void RemovePatientDiagnosisComment(PatientDiagnosisComment entity)
        {
            try
            {
                foreach (EncounterDiagnosisComment e in entity.EncounterDiagnosisComment) Remove(e);
            }
            catch
            {
            }

            try
            {
                Context.PatientDiagnosisComments.Remove(entity);
            }
            // may already be removed
            catch
            {
            }
        }

        public void RemoveAdmissionConsent(AdmissionConsent entity)
        {
            try
            {
                Context.AdmissionConsents.Remove(entity);
            }
            // may already be removed
            catch
            {
            }
        }

        public void RemoveEncounterHospiceElectionAddendumMedication(EncounterHospiceElectionAddendumMedication entity)
        {
            try
            {
                Context.EncounterHospiceElectionAddendumMedications.Remove(entity);
            }
            // may already be removed
            catch
            {
            }
        }

        public void RemoveEncounterHospiceElectionAddendumService(EncounterHospiceElectionAddendumService entity)
        {
            try
            {
                Context.EncounterHospiceElectionAddendumServices.Remove(entity);
            }
            // may already be removed
            catch
            {
            }
        }

        public void RemoveAdmissionProductCode(AdmissionProductCode entity)
        {
            try
            {
                Context.AdmissionProductCodes.Remove(entity);
            }
            // may already be removed
            catch
            {
            }
        }

        public void RemoveAdmissionDisciplineFrequency(AdmissionDisciplineFrequency entity)
        {
            try
            {
                Context.AdmissionDisciplineFrequencies.Remove(entity);
            }
            // may already be removed
            catch
            {
            }
        }

        public void RemoveEncounterSupervision(EncounterSupervision entity)
        {
            try
            {
                Context.EncounterSupervisions.Remove(entity);
            }
            // may already be removed
            catch
            {
            }
        }

        public void RemoveEncounterNPWT(EncounterNPWT entity)
        {
            try
            {
                Context.EncounterNPWTs.Remove(entity);
            }
            // may already be removed
            catch
            {
            }
        }

        public void UpdateCurrentCertCycleBit()
        {
            Log("UpdateCurrentCertCycleBit[011]", "WS_TRACE");

            if (CurrentForm.IsPlanOfCare && Context.Encounters.HasChanges)
            {
                // Only try to advance if all POC's are signed
                if (!(Context.Encounters.Where(ec =>
                        (ec.FormKey != null && DynamicFormCache.IsPlanOfCare((int)ec.FormKey))
                        && (!ec.Signed || ec.EncounterStatus != (int)EncounterStatusType.Completed)).Any()))
                {
                    var LastPOCList = Context.Encounters.Where(ec =>
                            (ec.FormKey != null && DynamicFormCache.IsPlanOfCare((int)ec.FormKey))
                            && ec.Signed && ec.EncounterStatus == (int)EncounterStatusType.Completed)
                        .OrderByDescending(eo => eo.EncounterOrTaskStartDateAndTime);

                    if (LastPOCList != null && LastPOCList.Any())
                    {
                        var LastPOC = LastPOCList.FirstOrDefault();
                        if ((LastPOC.HasChanges || LastPOC.IsNew) &&
                            LastPOC.GetIsEncounterInRecertWindow(LastPOC.Admission, LastPOC))
                        {
                            // Check to see if we should even try
                            var CurrentPeriodByDate =
                                LastPOC.Admission.GetAdmissionCertForDate(LastPOC.EncounterOrTaskStartDateAndTime
                                    .GetValueOrDefault().DateTime);
                            var NextPeriodByDate =
                                (CurrentPeriodByDate != null && CurrentPeriodByDate.PeriodEndDate.HasValue)
                                    ? LastPOC.Admission.GetAdmissionCertForDate(CurrentPeriodByDate.PeriodEndDate.Value
                                        .AddDays(1))
                                    : null;
                            // if the current and next are the same, bail.
                            if (CurrentPeriodByDate != null && NextPeriodByDate != null &&
                                CurrentPeriodByDate.AdmissionCertKey == NextPeriodByDate.AdmissionCertKey)
                            {
                                return;
                            }

                            // They're different, so continue trying to advance.
                            var OldCert = LastPOC.Admission.AdmissionCertification.FirstOrDefault(ac => ac.IsCurrentCert);

                            // Can't find a signed POC
                            if (NextPeriodByDate == null && OldCert == null)
                            {
                                return;
                            }
                            // Can't find the old one, but found a new one
                            if (OldCert == null)
                            {
                                NextPeriodByDate.IsCurrentCert = true;
                            }
                            // Found both and they aren't the same
                            else if (OldCert.AdmissionCertKey != NextPeriodByDate.AdmissionCertKey)
                            {
                                NextPeriodByDate.IsCurrentCert = true;
                            }
                        }
                    }
                }
            }
        }

        public void HavenValidateB1RecordAsync(string B1Record, string PPSModelVersion, int OasisVersionKey)
        {
            Log("HavenValidateB1RecordAsync[012]", "WS_TRACE");

            Context.HavenValidateB1Record(B1Record, PPSModelVersion, OasisVersionKey, CallHavenReturned, null);
        }

        public event Action<InvokeOperation<HavenReturnWrapper>> CallHavenReturned;

        public void RefreshPatientAddress(int PatientKey)
        {
            Log("RefreshPatientAddress[013]", "WS_TRACE");

            Context.Load(
                Context.GetPatientAddressForPatientKeyQuery(PatientKey),
                LoadBehavior.RefreshCurrent,
                lo =>
                {
                    if (Context != null && Context.PatientAddresses != null && Context.PatientAddresses.Any())
                    {
                        Messenger.Default.Send(PatientKey, "PatientAddressRefreshed");
                    }
                },
                null);
        }

        public void RefreshAdmissionCoverage(int AdmissionKey)
        {
            Log("RefreshAdmissionCoverage[014]", "WS_TRACE");

            Context.Load(
                Context.GetAdmissionCoverageForAdmissionQuery(AdmissionKey),
                LoadBehavior.RefreshCurrent,
                lo =>
                {
                    if (Context != null && Context.AdmissionCoverages != null && Context.AdmissionCoverages.Any())
                    {
                        Messenger.Default.Send(AdmissionKey, "AdmissionCoverageRefreshed");
                    }
                },
                null);
        }

        public void RefreshAdmissionPhysician(int AdmissionKey)
        {
            Log("RefreshAdmissionPhysician[015]", "WS_TRACE");

            Context.Load(
                Context.GetAdmissionPhysicianForAdmissionQuery(AdmissionKey),
                LoadBehavior.RefreshCurrent,
                lo =>
                {
                    if (Context != null && Context.AdmissionPhysicians != null && Context.AdmissionPhysicians.Any())
                    {
                        Messenger.Default.Send(AdmissionKey, "AdmissionPhysicianRefreshed");
                    }
                },
                null);
        }

        public void RefreshPatientFacilityStay(int PatientKey)
        {
            Log("RefreshPatientFacilityStay[016]", "WS_TRACE");

            Context.Load(
                Context.GetPatientFacilityStayForPatientQuery(PatientKey),
                LoadBehavior.RefreshCurrent,
                lo =>
                {
                    if (Context != null && Context.PatientFacilityStays != null)
                    {
                        Messenger.Default.Send(PatientKey, "PatientFacilityStayRefreshed");
                    }
                },
                null);
        }

        public async System.Threading.Tasks.Task Save(int taskKey, OfflineStoreType location)
        {
            Log($"Save[017]: taskKey={taskKey}, location={location}", "WS_TRACE");

            var __cacheFolder = DynamicFormSipManager.GetCacheFolder(taskKey, location);

            SecureRIASingleCacheManager cache = null;

            if (location == OfflineStoreType.AUTOSAVE)
            {
                string p = "Patient" + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss");
                cache = cache = new SecureRIASingleCacheManager(
                    Path.Combine(__cacheFolder, p),
                    Constants.ENTITY_TYPENAME_FORMAT);
            }
            else
            {
                cache = new SecureRIASingleCacheManager(
                    Path.Combine(__cacheFolder, "Patient"),
                    Constants.ENTITY_TYPENAME_FORMAT);
            }

            await cache.Save(Context);
        }

        //Used by DynamicFormViewModel to load restore the context for an instance of DynamicFormService
        public async System.Threading.Tasks.Task Load(int taskKey, OfflineStoreType location)
        {
            Log($"Load[018]: taskKey={taskKey}, location={location}", "WS_TRACE");

            CurrentTaskKey = taskKey;
            IsLoading = true;
            List<Exception> LoadErrors = new List<Exception>();
            try
            {
                var __cacheFolder = DynamicFormSipManager.GetCacheFolder(taskKey, location);

                var cache = new SecureRIASingleCacheManager(
                    Path.Combine(__cacheFolder, "Patient"),
                    Constants.ENTITY_TYPENAME_FORMAT);

                await cache.Load(Context);
            }
            catch (Exception e)
            {
                LoadErrors.Add(e);
            }

            IsLoading = false;
            if (OnMultiLoaded != null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    OnMultiLoaded(this, new MultiErrorEventArgs(LoadErrors, DataLoadType.LOCAL));
                });
            }
        }

        public async System.Threading.Tasks.Task LoadFromDashboard(int admissionKey, int taskKey,
            OfflineStoreType location)
        {
            Log($"LoadFromDashboard[019]: admissionKey={admissionKey}, taskKey ={taskKey}, location={location}",
                "WS_TRACE");

            CurrentTaskKey = taskKey;
            IsLoading = true;
            List<Exception> LoadErrors = new List<Exception>();
            try
            {
                var __cacheFolder = DynamicFormSipManager.GetCacheFolder(admissionKey, location, "{0}-DB");

                var cache = new SecureRIASingleCacheManager(
                    Path.Combine(__cacheFolder, "Patient"),
                    Constants.ENTITY_TYPENAME_FORMAT);

                await cache.Load(Context);
            }
            catch (Exception e)
            {
                Log($"LoadFromDashboard[019]: exception={e.Message}", "WS_TRACE", TraceEventType.Error);
                LoadErrors.Add(e);
            }

            IsLoading = false;
            if (OnMultiLoaded != null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    OnMultiLoaded(this, new MultiErrorEventArgs(LoadErrors, DataLoadType.LOCAL));
                });
            }
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            return ServiceProvider.GetService(serviceType);
        }

        public void CommitAllOpenEdits()
        {
            Log("CommitAllOpenEdits[020]", "WS_TRACE");

            CommitAllOpenEdits(Context);
            //this.CommitAllOpenEdits(this.TaskContext);
        }

        public new bool OpenOrInvalidObjects(string tag = "", bool log = false)
        {
            return OpenOrInvalidObjects(Context, tag, log);
        }

        public event Action<InvokeOperation<string>> ConvertOASISB1ToC1FixedReturned;

        public void ConvertOASISB1ToC1Fixed(string B1Record)
        {
            Log("ConvertOASISB1ToC1Fixed[021]", "WS_TRACE");

            Context.ConvertOASISB1ToC1Fixed(B1Record, ConvertOASISB1ToC1FixedReturned, null);
        }

        public event Action<InvokeOperation<byte[]>> ConvertOASISB1ToC1PPSReturned;

        public byte[] ConvertOASISB1ToC1PPS(string B1Record, string PPSPlusVendorKey)
        {
            Log("ConvertOASISB1ToC1PPS[022]", "WS_TRACE");

            Context.ConvertOASISB1ToC1PPSPlus(B1Record, PPSPlusVendorKey, ConvertOASISB1ToC1PPSReturned, null);
            return null;
        }

        public void Add<T>(T entity) where T : Entity
        {
            GetContext().EntityContainer.GetEntitySet<T>().Add(entity);
        }

        private static void Log(string message, string subCategory,
            TraceEventType traceEventType = TraceEventType.Information)
        {
            string category = "DynamicFormService";

            var __category = string.IsNullOrEmpty(subCategory)
                ? category
                : string.Format("{0}-{1}", category, subCategory);
            logWriter.Write(message,
                new[] { __category }, //category
                0, //priority
                0, //eventid
                traceEventType);
        }
    }
}