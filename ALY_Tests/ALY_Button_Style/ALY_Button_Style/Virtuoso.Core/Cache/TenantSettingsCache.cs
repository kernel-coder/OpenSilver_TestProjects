#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Ria.Sync;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using OpenRiaServices.DomainServices.Client;
using Ria.Common;
using Virtuoso.Client.Core;
using Virtuoso.Common;
using Virtuoso.Core.Services;
using Virtuoso.Metrics;
using Virtuoso.Portable;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Cache
{
    [Export(typeof(TenantSettingsCache))]
    public class TenantSettingsCache : ReferenceCacheBase, ICorrelationID, IMetricsTimer
    {
        MetricsTimerHelper LoadEvent { get; set; }
        private CorrelationIDHelper CorrelationIDHelper { get; set; }
        List<QueuedLogData> MetricsTimerData { get; set; }

        private static string CERTPOCTTYPECURRENT = "Current";

        private Tenant _Tenant;

        public Tenant Tenant
        {
            get
            {
                if (_Tenant == null)
                {
                    return Context.Tenants.FirstOrDefault();
                }

                return _Tenant;
            }
            private set { _Tenant = value; }
        }

        private TenantSetting _TenantSetting;

        public TenantSetting TenantSetting
        {
            get
            {
                if (_TenantSetting == null)
                {
                    return Context.TenantSettings.FirstOrDefault();
                }

                return _TenantSetting;
            }
            private set { _TenantSetting = value; }
        }

        #region PROPERTIES

        public int TenantSettingMedicationSearchTakeLimit
        {
            get
            {
                if (TenantSetting == null)
                {
                    return 101;
                }

                return TenantSetting.MedicationSearchTakeLimit;
            }
        }

        public bool TenantSettingDisableBrowserSingleInstance
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.DisableBrowserSingleInstance;
            }
        }

        public DateTime TenantSettingHospiceRegulation2021Date
        {
            get
            {
                if (TenantSetting == null)
                {
                    return new DateTime(2021, 10, 01);
                }

                return TenantSetting.HospiceRegulation2021Date.Date;
            }
        }

        public int TenantSettingHospiceElectionAddendumVersion =>
            (DateTime.Today.Date < TenantSettingHospiceRegulation2021Date.Date) ? 1 : 2;

        public int TenantSettingICDVersionDefault
        {
            get
            {
                DateTime Today = DateTime.Today.Date;
                if (Today < TenantSettingICD10PresentDate)
                {
                    return 9;
                }

                if (Today >= TenantSettingICD9CessationDate)
                {
                    return 10;
                }

                if (Today < TenantSettingICD10RequiredDate)
                {
                    return 9;
                }

                return 10; // if (Today < TenantSettingICD10RequiredDate) 
            }
        }

        public int TenantSettingRequiredICDVersion
        {
            get
            {
                DateTime Today = DateTime.Today.Date;
                return (Today < TenantSettingICD10RequiredDate) ? 9 : 10;
            }
        }

        public int TenantSettingRequiredICDVersionPrint(Encounter e)
        {
            DateTime? date = null;
            if ((e != null) && (e.EncounterStartDate != null))
            {
                date = ((DateTimeOffset)e.EncounterStartDate).Date;
            }

            return TenantSettingRequiredICDVersionDate(date);
        }

        public int TenantSettingRequiredICDVersionDate(DateTime? pDate)
        {
            DateTime? date = (pDate == null) ? DateTime.Today.Date : pDate;
            return (date < TenantSettingICD10RequiredDate) ? 9 : 10;
        }

        public int TenantSettingRequiredICDVersionDateTimeOffset(DateTimeOffset? pDate)
        {
            DateTime? date = (pDate == null) ? DateTime.Today.Date : pDate.Value.Date;
            return (date < TenantSettingICD10RequiredDate) ? 9 : 10;
        }

        public int TenantSettingRequiredICDVersionPrintPOC(Encounter e)
        {
            DateTime? date = null;
            if (e != null)
            {
                if (e.EncounterPlanOfCare != null)
                {
                    EncounterPlanOfCare epoc = e.EncounterPlanOfCare.FirstOrDefault();
                    if (epoc != null)
                    {
                        date = epoc.CertificationFromDate;
                    }
                }

                if ((date == null) && (e.EncounterStartDate != null))
                {
                    date = ((DateTimeOffset)e.EncounterStartDate).Date;
                }
            }

            if (date == null)
            {
                date = DateTime.Today.Date;
            }

            return (date < TenantSettingICD10POCPrintDate) ? 9 : 10;
        }

        public Visibility TenantSettingICDVersionVisible
        {
            get
            {
                DateTime Today = DateTime.Today.Date;
                if (Today < TenantSettingICD10PresentDate)
                {
                    return Visibility.Collapsed;
                }

                if (Today >= TenantSettingICD9CessationDate)
                {
                    return Visibility.Collapsed;
                }

                return Visibility.Visible;
            }
        }

        public bool TenantSettingUsingEnvelopeWindow
        {
            get
            {
                // Left is the default
                if (TenantSetting == null)
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(TenantSetting.EnvelopeWindow))
                {
                    return false;
                }

                return true;
            }
        }

        public bool TenantSettingIsEnvelopeWindowLeft
        {
            get
            {
                // Left is the default
                if (TenantSetting == null)
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(TenantSetting.EnvelopeWindow))
                {
                    return false;
                }

                return (TenantSetting.EnvelopeWindow.ToLower() == "left") ? true : false;
            }
        }

        public bool TenantSettingIsEnvelopeWindowRight
        {
            get
            {
                // Left is the default
                if (TenantSetting == null)
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(TenantSetting.EnvelopeWindow))
                {
                    return false;
                }

                return (TenantSetting.EnvelopeWindow.ToLower() == "right") ? true : false;
            }
        }

        public bool TenantSettingCarePlanMap
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return (TenantSetting.CarePlanMap);
            }
        }

        public bool TenantSettingHospicePreEvalRequired
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return (TenantSetting.HospicePreEvalRequired);
            }
        }

        public bool TenantSettingNonHospicePreEvalRequired
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return (TenantSetting.NonHospicePreEvalRequired);
            }
        }

        public bool TenantSettingNonHospiceResumptionPreEvalRequired
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return (TenantSetting.NonHospiceResumptionPreEvalRequired);
            }
        }

        public int ServiceLineTypeUseBits
        {
            get
            {
                bool canHaveHomeHealth = TenantSettingPurchasedHomeHealth;
                bool canHaveHospice = TenantSettingPurchasedHospice;
                bool canHaveHomeCare = TenantSettingPurchasedHomeCare;
                int homeHealthBit = (int)eServiceLineType.HomeHealth;
                int hospiceBit = (int)eServiceLineType.Hospice;
                int homeCareBit = (int)eServiceLineType.HomeCare;
                int mask = (canHaveHomeHealth ? homeHealthBit : 0) + (canHaveHospice ? hospiceBit : 0) +
                           (canHaveHomeCare ? homeCareBit : 0);
                return mask;
            }
        }

        public bool TenantSettingPurchasedHomeHealth
        {
            get
            {
                if (TenantSetting == null)
                {
                    return true;
                }

                return !TenantSetting.HospiceOnly; // Needs changed to appropriate flag value;
            }
        }

        public bool TenantSettingPurchasedHomeCare
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSettingPurchasedHomeHealth; // Needs changed to a real tenant setting;
            }
        }

        public bool TenantSettingPurchasedHospice
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.PurchasedHospice;
            }
        }

        public bool TenantSettingHasMultipleServiceLineTypeOptions => !TenantSettingIsHomeHealthOnly &&
                                                                      !TenantSettingIsHospiceOnly &&
                                                                      !TenantSettingIsHomeCareOnly;

        public bool TenantSettingIsHomeHealthOnly => TenantSettingPurchasedHomeHealth &&
                                                     !TenantSettingPurchasedHospice && !TenantSettingPurchasedHomeCare;

        public bool TenantSettingIsHospiceOnly => TenantSettingPurchasedHospice && !TenantSettingPurchasedHomeHealth &&
                                                  !TenantSettingPurchasedHomeCare;

        public bool TenantSettingIsHomeCareOnly => TenantSettingPurchasedHomeCare &&
                                                   !TenantSettingPurchasedHomeHealth && !TenantSettingPurchasedHospice;

        public int? TenantSettingServiceLineTypeUseBitsDefault
        {
            get
            {
                int homeHealthBit = (int)eServiceLineType.HomeHealth;
                int hospiceBit = (int)eServiceLineType.Hospice;
                int homeCareBit = (int)eServiceLineType.HomeCare;

                if (TenantSettingIsHomeHealthOnly)
                {
                    return homeHealthBit;
                }

                if (TenantSettingIsHospiceOnly)
                {
                    return hospiceBit;
                }

                if (TenantSettingIsHomeCareOnly)
                {
                    return homeCareBit;
                }

                return null;
            }
        }

        public bool TenantSettingIsForeignSchedulingInUse
        {
            get
            {
                if (TenantSetting == null)
                {
                    return true;
                }

                return (TenantSetting.UseScheduling) ? true : false;
            }
        }

        public bool TenantSettingAutoShowAlerts
        {
            get
            {
                if (TenantSetting == null)
                {
                    return true;
                }

                return (TenantSetting.AutoShowAlerts) ? true : false;
            }
        }

        public bool TenantSettingOASISAssist
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return (TenantSetting.OASISAssist) ? true : false;
            }
        }

        public DateTime TenantSettingICD10PresentDate
        {
            get
            {
                if (TenantSetting == null)
                {
                    return new DateTime(2013, 10, 01);
                }

                return TenantSetting.ICD10PresentDate;
            }
        }

        public bool TenantSettingIsUTEST
        {
            get
            {
#if DEBUG
                return true;
#else
                if (TenantSetting == null) return false;
                if (string.IsNullOrWhiteSpace(TenantSetting.Banner) == true) return false;
                if (TenantSetting.Banner.Trim().ToLower().StartsWith("utest ")) return true;
                return false;
#endif
            }
        }

        public DateTime TenantSettingICD10RequiredDate
        {
            get
            {
                if (TenantSetting == null)
                {
                    return new DateTime(2014, 09, 01);
                }

                return TenantSetting.ICD10RequiredDate;
            }
        }

        public DateTime TenantSettingICD9CessationDate
        {
            get
            {
                if (TenantSetting == null)
                {
                    return new DateTime(2015, 10, 01);
                }

                return TenantSetting.ICD9CessationDate;
            }
        }

        public DateTime TenantSettingICD10POCPrintDate
        {
            get
            {
                if (TenantSetting == null)
                {
                    return new DateTime(2015, 10, 01);
                }

                return TenantSetting.ICD10POCPrintDate;
            }
        }

        public DateTime TenantSettingICD10PreviousMedicalSettingRequiredDate
        {
            get
            {
                if (TenantSetting == null)
                {
                    return new DateTime(2015, 10, 01);
                }

                return TenantSetting.ICD10PreviousMedicalSettingRequiredDate;
            }
        }

        public int TenantSettingAuthorizationThreshold
        {
            get
            {
                if (TenantSetting == null)
                {
                    return 75; // 75 is default threshold value
                }

                return ((TenantSetting.AuthorizationThreshold == null)
                    ? 75
                    : (int)TenantSetting.AuthorizationThreshold);
            }
        }

        public int TenantSettingAutosaveFrequency
        {
            get
            {
                if (TenantSetting == null)
                {
                    return 5;
                }

                return TenantSetting.AutoSaveFrequency;
            }
        }

        public bool TenantSettingShowPatientDashboard
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.ShowPatientDashboard;
            }
        }

        public bool TenantSettingAuthorizationDistributionEnabled
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.AuthorizationDistributionEnabled;
            }
        }

        public string TenantSettingDistanceTraveledMeasure
        {
            get
            {
                if (TenantSetting == null)
                {
                    return "";
                }

                return TenantSetting.DistanceTraveledMeasure;
            }
        }

        public bool TenantSettingPOCGoalIdentification
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.POCGoalIdentification;
            }
        }

        public string TenantSettingCertPOCType
        {
            get
            {
                if (TenantSetting == null)
                {
                    return CERTPOCTTYPECURRENT;
                }

                return TenantSetting.CertPOCType;
            }
        }

        public bool TenantSettingCertPOCTypeIsCurrent =>
            (TenantSettingCertPOCType.ToLower() == "current") ? true : false;

        public bool TenantSettingCreatePOCAddendum
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.CreatePOCAddendum;
            }
        }

        public bool TenantSettingUsingDischargeWorklist
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.UsingDischargeWorklist;
            }
        }

        public bool TenantSettingUsingTransferWorklist
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.UsingTransferWorklist;
            }
        }

        public bool UsingAttemptedVisit
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.UsingAttemptedVisit;
            }
        }

        public bool UsingSepsis
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.UsingSepsis;
            }
        }
        public bool SSMEnabled
        {
            get
            {
                if (TenantSetting == null) return false;
                return TenantSetting.SSMEnabled;
            }
        }
        public DayOfWeek TenantSettingWeekStartDay
        {
            get
            {
                string day = "SUNDAY";
                if ((TenantSetting != null) && (string.IsNullOrWhiteSpace(TenantSetting.WeekStartDay) == false))
                {
                    day = TenantSetting.WeekStartDay;
                }

                switch (day.ToLower())
                {
                    case "monday":
                        return DayOfWeek.Monday;
                    case "tuesday":
                        return DayOfWeek.Tuesday;
                    case "wednesday":
                        return DayOfWeek.Wednesday;
                    case "thursday":
                        return DayOfWeek.Thursday;
                    case "friday":
                        return DayOfWeek.Friday;
                    case "saturday":
                        return DayOfWeek.Saturday;
                    default:
                        return DayOfWeek.Sunday;
                }
            }
        }

        public string TenantSettingWeekStartDayText
        {
            get
            {
                string day = "SUNDAY";
                if ((TenantSetting != null) && (string.IsNullOrWhiteSpace(TenantSetting.WeekStartDay) == false))
                {
                    day = TenantSetting.WeekStartDay;
                }

                switch (day.ToLower())
                {
                    case "monday":
                        return "Monday";
                    case "tuesday":
                        return "Tuesday";
                    case "wednesday":
                        return "Wednesday";
                    case "thursday":
                        return "Thursday";
                    case "friday":
                        return "Friday";
                    case "saturday":
                        return "Saturday";
                    default:
                        return "Sunday";
                }
            }
        }

        public bool TenantSettingPECOSAlert
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return (TenantSetting.PECOSAlert) ? true : false;
            }
        }

        public bool TenantSettingHasPurchasedTeamScheduling =>
            (TenantSetting == null) || TenantSetting.PurchasedTeamScheduling;

        public bool TenantSettingIsPurchasedTeleMonitoring =>
            (TenantSetting == null) || TenantSetting.PurchasedTeleMonitoring;

        public bool TenantSettingDiagnosisCodersPostSignature
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return (TenantSetting.DiagnosisCodersPostSignature) ? true : false;
            }
        }

        public bool TenantSettingMessageBrokerOrderPrint
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return (TenantSetting.MessageBrokerOrderPrint) ? true : false;
            }
        }

        public bool TenantSettingHISCoordinatorCanEdit
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return (TenantSetting.HISCoordinatorCanEdit) ? true : false;
            }
        }

        public bool TenantSettingOASISCoordinatorCanEdit
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return (TenantSetting.OASISCoordinatorCanEdit) ? true : false;
            }
        }

        public int DisciplineRecertWindowWithDefault
        {
            get
            {
                if (TenantSetting == null)
                {
                    return 7;
                }

                return TenantSetting.DisciplineRecertWindow == null ? 7 : (int)TenantSetting.DisciplineRecertWindow;
            }
        }

        public bool TenantSettingUseTrackingGroups
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.UseTrackingGroups;
            }
        }

        public bool TenantSettingAutoAssignTrackingGroups
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.AutoAssignTrackingGroups;
            }
        }

        public DateTime? TenantSettingOASISTransmissionBlackOutFromDate
        {
            get
            {
                if (TenantSetting == null)
                {
                    return null;
                }

                return TenantSetting.OASISTransmissionBlackOutFromDate;
            }
        }

        public DateTime? TenantSettingOASISTransmissionBlackOutThruDate
        {
            get
            {
                if (TenantSetting == null)
                {
                    return null;
                }

                return TenantSetting.OASISTransmissionBlackOutThruDate;
            }
        }

        public bool TenantSettingMyProductivityEnabled
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.MyProductivityEnabled;
            }
        }

        public int TenantSettingMyProductivityLookBackDays
        {
            get
            {
                if (TenantSetting == null)
                {
                    return 0;
                }

                return TenantSetting.MyProductivityLookBackDays;
            }
        }

        public bool DocumentFaxingAndTrackingEnabled
        {
            get
            {
                if (TenantSetting == null)
                {
                    return false;
                }

                return TenantSetting.DocumentFaxingAndTrackingEnabled;
            }
        }

        #endregion PROPERTIES

        public static TenantSettingsCache Current { get; private set; }

        [ImportingConstructor]
        public TenantSettingsCache(ILogger logManager)
            : base(logManager, ReferenceTableName.TenantSetting, "039")
        {
            if (Current == this)
            {
                throw new InvalidOperationException($"{nameof(TenantSettingsCache)} already initialized.");
            }

            Current = this;

            CorrelationIDHelper = new CorrelationIDHelper();
            MetricsTimerData = new List<QueuedLogData>();

            var BaseUri = System.Windows.Application.Current.GetServerBaseUri();
            Context = new VirtuosoDomainContext(new Uri(BaseUri,
                "Virtuoso-Services-Web-VirtuosoDomainService.svc")); //using alternate constructor, so that it can run in a thread
        }

        public System.Threading.Tasks.Task<bool> ClientValidAsync(bool _isOnline, string _assemblyFileVersionInfo)
        {
            if (_isOnline == false)
            {
                var taskCompletionSource = new System.Threading.Tasks.TaskCompletionSource<bool>();
                taskCompletionSource.TrySetResult(true);
                return taskCompletionSource.Task;
            }
            else
            {
                return this.Context.ClientValid(_assemblyFileVersionInfo)
                    .AsTask()
                    .ContinueWith((t) =>
                    {
                        return t.Result.Value;
                    });
            }
        }

        public System.Threading.Tasks.Task<ServiceLoadResult<TenantSetting>> GetTenantSettingAsync(bool _isOnline)
        {
            // NOTE: in order to background the 'entire' WCF service call - create the Task in a thread           
            var source = new System.Threading.Tasks.TaskCompletionSource<ServiceLoadResult<TenantSetting>>();
            System.Threading.Tasks.Task.Factory.StartNew(async () =>
            {
                try
                {
                    await Load(
                        DateTime.UtcNow,
                        _isOnline,
                        () =>
                        {
                            if (source.Task.IsCanceled)
                            {
                                source.SetCanceled();
                            }

                            if (source.Task.IsFaulted)
                            {
                                source.SetResult(new ServiceLoadResult<TenantSetting>(
                                    Context.TenantSettings.ToList(),
                                    Context.TenantSettings.Count(),
                                    new List<System.ComponentModel.DataAnnotations.ValidationResult>(),
                                    new InvalidOperationException(string.Format(
                                        $"Cannot load {nameof(TenantSettingsCache)}.  Reason: {0}.",
                                        source.Task.Exception.ToString())),
                                    cancelled: false,
                                    userState: null
                                ));
                            }

                            if (source.Task.IsFaulted == false)
                            {
                                if (isLoading)
                                {
                                    source.SetResult(new ServiceLoadResult<TenantSetting>(
                                        Context.TenantSettings.ToList(),
                                        Context.TenantSettings.Count(),
                                        new List<System.ComponentModel.DataAnnotations.ValidationResult>(),
                                        new InvalidOperationException(
                                            $"Cannot load {nameof(TenantSettingsCache)}.  Reason: {nameof(TenantSettingsCache)} is currently loading."),
                                        cancelled: false,
                                        userState: null
                                    ));
                                }
                                else
                                {
                                    source.SetResult(
                                        new ServiceLoadResult<TenantSetting>(
                                            Context.TenantSettings.ToList(),
                                            TotalRecords,
                                            new List<System.ComponentModel.DataAnnotations.ValidationResult>(),
                                            error: null,
                                            cancelled: false,
                                            userState: null
                                        ));
                                }
                            }
                        },
                        force: true);
                }
                catch (Exception exc)
                {
                    source.SetException(exc);
                }
            });
            return source.Task;
        }

        public override async System.Threading.Tasks.Task Load(DateTime? lastUpdatedDate, bool isOnline,
            Action callback, bool force = false)
        {
            LastUpdatedDate = lastUpdatedDate;
            Ticks = LastUpdatedDate?.Ticks ?? 0;
            TotalRecords = 0;

            await RemovePriorVersion();

            if (isLoading)
            {
                return;
            }

            isLoading = true;

            Context.EntityContainer.Clear();

            if ((isOnline && Ticks > 0)
                || (Ticks == 0 && isOnline &&
                    await CacheExists() ==
                    false)) //Ticks = 0, but online, got LastUpdatedDdate = NULL from GetReferenceDataInfo, still need to query server for data to build cache
            {
                if ((await RefreshReferenceCacheAsync(Ticks)) || force)
                {
                    LoadEvent = new MetricsTimerHelper(new StopWatchFactory(), CorrelationIDHelper,
                        Logging.Context.TenantSettingsCache_Load);
                    Context.Load(Context.GetTenantSettingNoAuthQuery(), OnLoaded, callback);
                }
                else
                {
                    LoadFromDisk(callback);
                }
            }
            else
            {
                LoadFromDisk(callback);
            }
        }

        protected void LoadFromDisk(Action callback)
        {
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    var cache = await RIACacheManager.Initialize(
                        Path.Combine(ApplicationStore, Constants.REFERENCE_DATA_STORE_FOLDER), CacheName,
                        Constants.ENTITY_TYPENAME_FORMAT, true); //NOTE: can throw DirectoryNotFoundException
                    await cache.Load(Context);

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        TenantSetting = Context.TenantSettings.FirstOrDefault();
                        isLoading = false;
                        if (TenantSetting != null)
                        {
                            Tenant = TenantSetting.Tenant;
                            TotalRecords = 1;
                        }
                        else
                        {
                            TotalRecords = 0;
                        }

                        callback?.Invoke();
                    });
                }
                catch (CacheNotFoundException)
                {
                    //doesn't mean there is a problem necessarily, probably means that there was no data returned from the server, so nothing was saved to disk - e.g. no files
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        isLoading = false;
                        TotalRecords = 0;
                        Log(TraceEventType.Information,
                            string.Format(
                                "{0} Cache.  Directory not found.  Probably no data returned from server, so no data saved to disk.",
                                CacheName));
                        callback?.Invoke();
                    });
                }
                catch (Exception e)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        isLoading = false;
                        Log(TraceEventType.Critical, string.Format("{0} load error", CacheName), e);
                        callback?.Invoke();
                    });
                }
            });
        }

        private async void OnLoaded(LoadOperation<TenantSetting> operation)
        {
            if (LoadEvent != null)
            {
                var log = LoadEvent.Stop(Logging.LocationOverride.Login, EntityManager.IsOnline,
                    autoQueue: false); //NOTE: since this is called from a thread, do not use NavigationService.
                if (string.IsNullOrEmpty(log) == false)
                {
                    var _context = new EventContext
                    {
                        EventType = EventType.ELAPSED, Timestamp = LoadEvent.StartDateTime(),
                        ConnectivityStatus = (EntityManager.IsOnline)
                            ? ConnectivityStatus.Online
                            : ConnectivityStatus.Offline
                    };
                    System.Diagnostics.Debug.WriteLine("[004] EventType: {0}\tTimestamp: {1}\tData:{2}",
                        _context.EventType, _context.Timestamp, log);
                    MetricsTimerData.Add(new QueuedLogData { EventContext = _context, Data = log });
                }
            }

            if (operation.HasError)
            {
                operation.MarkErrorAsHandled();
                if (operation.UserState != null)
                {
                    var callback = (Action)operation.UserState;
                    LoadFromDisk(callback);
                }
                else
                {
                    LoadFromDisk(null);
                }
            }
            else
            {
                await PurgeAndSave();

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    isLoading = false;
                    TenantSetting = operation.Entities.FirstOrDefault();

                    if (TenantSetting == null)
                    {
                        TotalRecords = 0;

                        TenantSetting = new TenantSetting
                        {
                            Banner = String.Empty,
                            CarePlanMap = true,
                            InvalidAttempts = 3,
                            HospicePreEvalRequired = false,
                            NonHospicePreEvalRequired = false,
                            NonHospiceResumptionPreEvalRequired = false,
                            PasswordHistory = 5,
                            PasswordDays = 30,
                            SingleLogin = false,
                            PurchasedMediSpan = false,
                            PurchasedHospice = false,
                            HospiceOnly = false,
                            PurchasedTeleMonitoring = false,
                            UseMilitaryTime = false,
                            DiagnosisCodersPostSignature = false,
                            MessageBrokerOrderPrint = false,
                            UsingDiagnosisCoders = false,
                            UsingHavenValidation = false,
                            HISCoordinatorCanEdit = false,
                            OASISCoordinatorCanEdit = false,
                            UsingHISCoordinator = false,
                            UsingOASISCoordinator = false,
                            UsingOrderEntryReviewers = false,
                            ServiceOrdersHeldUntilReviewed = false,
                            UsingPOCOrderReviewers = false,
                            OASISAssist = false,
                            HISHoldDays = 7,
                            OasisHoldDays = 7,
                            IsDemoEnvironment = false,
                            ContractServiceProvider = false,
                            LoginDomain = String.Empty,
                            Name = String.Empty,
                            PainScale = String.Empty,
                            TimeZone = String.Empty,
                            ICD10PresentDate = new DateTime(2015, 08, 03),
                            ICD10RequiredDate = new DateTime(2015, 08, 03),
                            ICD9CessationDate = new DateTime(2020, 10, 01),
                            ICD10POCPrintDate = new DateTime(2015, 10, 01),
                            ICD10PreviousMedicalSettingRequiredDate = new DateTime(2015, 10, 01),
                            CertPOCType = CERTPOCTTYPECURRENT,
                            CreatePOCAddendum = false,
                            UsingDischargeWorklist = false,
                            UsingTransferWorklist = false,
                            UsingAttemptedVisit = false,
                            UsingSepsis = false,
                            SSMEnabled = false,
                        };

                        Tenant = new Tenant
                        {
                            TenantName = "UNKOWN",
                            Offline = false,
                            OfflineMessage = String.Empty
                        };
                    }
                    else
                    {
                        Tenant = TenantSetting.Tenant;
                        TotalRecords = 1;
                    }

                    if (operation.UserState != null)
                    {
                        ((Action)operation.UserState)?.Invoke();
                    }

                    Messenger.Default.Send(CacheName, "CacheLoaded");
                });
            }
        }

        public static bool GetSSMUseSubDomain()
        {
            var SSMUseSubdomain = false;
            var __SSMUseSubdomainApplicationSetting =
                Current.TenantSetting.ApplicationSetting.FirstOrDefault(a =>
                    a.Key == Constants.AppSettings.SSMUseSubdomain);
            if (__SSMUseSubdomainApplicationSetting != null)
            {
                var __value = false;
                if (Boolean.TryParse(__SSMUseSubdomainApplicationSetting.Value, out __value))
                {
                    SSMUseSubdomain = __value;
                }
            }

            return SSMUseSubdomain;
        }

        void ICorrelationID.UseCorrelationID(CorrelationIDHelper correlationID)
        {
            CorrelationIDHelper = correlationID;
        }

        IEnumerable<QueuedLogData> IMetricsTimer.GetMetricsTimerData()
        {
            return MetricsTimerData;
        }
    }
}