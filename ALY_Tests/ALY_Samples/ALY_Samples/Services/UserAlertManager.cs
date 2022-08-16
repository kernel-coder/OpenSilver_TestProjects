#region Usings

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Core.Storage;
using Virtuoso.Client.Offline;
using Virtuoso.Core.Alerts;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Events;
using Virtuoso.Core.Interface;
using Virtuoso.Core.Occasional;
using Virtuoso.Linq;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export]
    public class UserAlertManager : ICleanup
    {
        private static readonly object _lock = new object();

        private static UserAlertManager instance;

        public static UserAlertManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_lock)
                    {
                        if (instance == null)
                        {
                            Lazy<UserAlertManager> Export = Virtuoso.Client.Core.VirtuosoContainer.Current
                                .GetExport<UserAlertManager>();
                            instance = Export.Value;
                        }
                    }
                }

                return instance;
            }
        }

        [ImportingConstructor]
#if OPENSILVER
        public UserAlertManager(VirtuosoApplicationConfiguration config, AlertManagerService alertManagerService)
#else
        private UserAlertManager(VirtuosoApplicationConfiguration config, AlertManagerService alertManagerService)
#endif
        {
            Configuration = config;
            AlertManagerService = alertManagerService;

            AlertManagerService.LoadAlertNotificationsComplete += AlertManagerService_LoadAlertNotificationsComplete;
            AlertManagerService.OnUserAlertsJoinCountLoaded += AlertManagerService_OnUserAlertsJoinCountLoaded;
            AlertManagerService.OnClientLoaded += AlertManagerService_OnClientLoaded;
            AlertManagerService.OnAlertContextSaved += AlertManagerService_OnAlertContextSaved;
        }

        private SortDescription PreviousSort = new SortDescription("DueDate", ListSortDirection.Ascending);
        private AlertManagerService AlertManagerService { get; set; }
        private VirtuosoApplicationConfiguration Configuration { get; set; }

        public EntitySet<UserAlertsJoin> AlertsContext_UserAlertsJoins => AlertManagerService.AlertsContext_UserAlertsJoins;

        private DateTime LastUpdate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        private static String AllValue = "All";
        private String PreviousSelectedPatient = "All";
        private String PreviousSelectedCareCoordinator = "All";
        private readonly List<OfflineTaskKey> OfflineTaskKeys = new List<OfflineTaskKey>();

        private bool _IsBusy;

        public bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                if (_IsBusy != value)
                {
                    _IsBusy = value;
                    Messenger.Default.Send<bool>(true, "AlertManagerIsBusyChanged");
                }
            }
        }

        private bool isLoadAlerts = false;
        private bool isLoadAlertsCount = false;

        #region Properties

        public int DisplayedAlerts
        {
            get
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[UserAlertManager] {nameof(DisplayedAlerts)}.  Calling DisplayReportedAlertList");
                int ret = 0;
                var listReference =
                    DisplayReportedAlertList; //grab reference once - this thing (DisplayReportedAlertList) is a getter that creates a new list everytime it is called...
                if (listReference == null)
                    ret = 0;
                else
                {
                    ret = listReference.SourceCollection.Cast<UserAlertsJoin>().Count();
                }

                System.Diagnostics.Debug.WriteLine($"[UserAlertManager] {nameof(DisplayedAlerts)} = {ret}");
                return ret;
            }
        }

        public int TotalAlerts
        {
            get
            {
                if ((AlertManagerService == null) ||
                    (AlertManagerService.AlertsContext_UserAlertsJoinCounts == null)) return 0;
                UserAlertsJoinCount u = AlertManagerService.AlertsContext_UserAlertsJoinCounts.FirstOrDefault(p => p.ExceptAlertKey == 0);
                return u?.Count ?? (int)0;
            }
        }

        public bool HasAlerts => (TotalAlerts > 0) ? true : false;

        private UserAlertsJoinCount _SelectedAlertsDisplay;

        public UserAlertsJoinCount SelectedAlertsDisplay
        {
            get { return _SelectedAlertsDisplay; }
            set { SetSelectedAlertsDisplay(value, true); }
        }

        public UserAlertsJoinCount SelectedAlertsDisplayWithoutMessaging
        {
            get { return _SelectedAlertsDisplay; }
            set { SetSelectedAlertsDisplay(value, false); }
        }

        private String _SelectedPatientDisplay;

        public String SelectedPatientDisplay
        {
            get { return _SelectedPatientDisplay; }
            set
            {
                if (value == null) value = AllValue;
                SetSelectedPatientDisplay(value, true);
            }
        }

        private String _SelectedCareCoordinatorDisplay;

        public String SelectedCareCoordinatorDisplay
        {
            get { return _SelectedCareCoordinatorDisplay; }
            set
            {
                if (value == null) value = AllValue;
                SetSelectedCareCoordinatorDisplay(value, true);
            }
        }

        private ServiceLine _SelectedServiceLineDisplay;

        public ServiceLine SelectedServiceLineDisplay
        {
            get { return _SelectedServiceLineDisplay; }
            set
            {
                _SelectedServiceLineDisplay = value;
                SendAlertsChangedMessage(new NullConcurrencyConflict());
            }
        }

        private ServiceLineGrouping _SelectedServiceLineGroup;

        public ServiceLineGrouping SelectedServiceLineGroup
        {
            get { return _SelectedServiceLineGroup; }
            set
            {
                _SelectedServiceLineGroup = value;
                SendAlertsChangedMessage(new NullConcurrencyConflict());
            }
        }

        #endregion

        #region Collections

        private readonly CollectionViewSource _SelectedAlertsView = new CollectionViewSource();

        public ICollectionView SelectedAlertsView => _SelectedAlertsView.View;

        private AlertSearchCriteria SearchCriteria = new AlertSearchCriteria();

        public void SetSearchCriteria(AlertSearchCriteria crit)
        {
            SearchCriteria = crit;
        }

        public PagedCollectionView DisplayReportedAlertList
        {
            get
            {
                var showAllDisciplines = (SearchCriteria != null && SearchCriteria.SelectedDisciplines != null)
                    ? ShowAllDisciplines()
                    : true;

                var returnAlertList = AlertManagerService.AlertsContext_UserAlertsJoins
                    .Where(sl =>
                        (SelectedAlertsDisplay == null) || (sl.SourceAlertKey == SelectedAlertsDisplay.ExceptAlertKey))

                    .WhereIf(
                        SearchCriteria.CareCoordinatorKey.HasValue &&
                        SearchCriteria.CareCoordinatorKey.Value.Equals(Guid.Empty) == false,
                        sl => sl.CareCoordinatorId.HasValue == false || sl.CareCoordinatorId.GetValueOrDefault() ==
                            SearchCriteria.CareCoordinatorKey)
                    .WhereIf(SearchCriteria.PatientKey > 0,
                        sl => sl.PatientKey.GetValueOrDefault() == SearchCriteria.PatientKey)
                    .WhereIf(SearchCriteria.SelectedDisciplines != null && !showAllDisciplines,
                        sl => sl.DisciplineKey.HasValue &&
                              SearchCriteria.SelectedDisciplines.Contains(sl.DisciplineKey.Value))
                    .WhereIf(SearchCriteria.ServiceLineKey > 0,
                        sl => sl.ServiceLineKey == SearchCriteria.ServiceLineKey)
                    .WhereIf(SearchCriteria.InsuranceKey > 0, sl => sl.HIBInsuranceKey == SearchCriteria.InsuranceKey)
                    .WhereIf(SearchCriteria.ServiceLineGroup1Keys != null,
                        sl => sl.ServiceLineGroup0Key.HasValue &&
                              SearchCriteria.ServiceLineGroup1Keys.Contains(sl.ServiceLineGroup0Key.Value))
                    .WhereIf(SearchCriteria.ServiceLineGroup2Keys != null,
                        sl => sl.ServiceLineGroup1Key.HasValue &&
                              SearchCriteria.ServiceLineGroup2Keys.Contains(sl.ServiceLineGroup1Key.Value))
                    .WhereIf(SearchCriteria.ServiceLineGroup3Keys != null,
                        sl => sl.ServiceLineGroup2Key.HasValue &&
                              SearchCriteria.ServiceLineGroup3Keys.Contains(sl.ServiceLineGroup2Key.Value))
                    .WhereIf(SearchCriteria.ServiceLineGroup4Keys != null,
                        sl => sl.ServiceLineGroup3Key.HasValue &&
                              SearchCriteria.ServiceLineGroup4Keys.Contains(sl.ServiceLineGroup3Key.Value))
                    .WhereIf(SearchCriteria.ServiceLineGroup5Keys != null,
                        sl => sl.ServiceLineGroup4Key.HasValue &&
                              SearchCriteria.ServiceLineGroup5Keys.Contains(sl.ServiceLineGroup4Key.Value))
                    .Where(sl => sl.UserAlertStatusKey != SearchCriteria.ExcludeKey)
                    .Where(sl => (sl.UserAlertStatus != null && sl.UserAlertStatus.FulfilledDate == null));

                PagedCollectionView pagedView = new PagedCollectionView(returnAlertList);
                (pagedView.SortDescriptions as INotifyCollectionChanged).CollectionChanged +=
                    (object sender, NotifyCollectionChangedEventArgs e) =>
                    {
                        // This gets fired multiple times based on the previous sort and new sort
                        var sorts = sender as System.ComponentModel.SortDescriptionCollection;
                        if (sorts != null && sorts.Any() == true) PreviousSort = sorts[0];
                    };
                pagedView.SortDescriptions.Add(PreviousSort);

                var count = pagedView.SourceCollection.Cast<UserAlertsJoin>().Count();
                System.Diagnostics.Debug.WriteLine(
                    $"[UserAlertManager]  {nameof(DisplayReportedAlertList)}:Created new PagedCollectionView...    Count = {count}");

                return pagedView;
            }
        }

        private bool ShowAllDisciplines()
        {
            var ret = SearchCriteria.SelectedDisciplines.Any(d => d == -99) ||
                      SearchCriteria.SelectedDisciplines.Any() == false;
            return ret;
        }

        private readonly CollectionViewSource _SelectedAlerts = new CollectionViewSource();

        public ICollectionView SelectedAlerts => _SelectedAlerts.View;

        public IEnumerable<String> SelectedPatientsView
        {
            get
            {
                var interimList = AlertManagerService.AlertsContext_UserAlertsJoins
                    .Where(sl => sl.PatientKey.HasValue && ((SelectedAlertsDisplay == null) ||
                                                            (sl.SourceAlertKey ==
                                                             SelectedAlertsDisplay.ExceptAlertKey) ||
                                                            (SelectedAlertsDisplay.DisplayName == AllValue)))
                    .Select(sl => sl.PatientName).Distinct().ToList();
                interimList.Insert(0, AllValue);

                return interimList;
            }
        }

        public IEnumerable<ServiceLine> SelectedServiceLinesView
        {
            get
            {
                var interimList = ServiceLineCache.GetActiveUserServiceLinePlusMe(null, false);
                interimList.Insert(0, new ServiceLine { Name = AllValue });

                return interimList;
            }
        }

        public IEnumerable<ServiceLineGrouping> AvailableServiceLineGroups
        {
            get
            {
                return new List<ServiceLineGrouping> { new ServiceLineGrouping { Name = AllValue } }
                    .Union(AlertManagerService.AlertsContext_UserAlertsJoins
                        .Select(u => ServiceLineCache.GetServiceLineGroupingFromKey(u.MaxServiceLineGroupingKey))
                        .Where(u => u != null)
                    );
            }
        }

        #endregion

        #region Events

        private bool _RefreshAlerts = false;

        private void AlertManagerService_OnUserAlertsJoinCountLoaded(object sender, EntityEventArgs<UserAlertsJoinCount> e)
        {
            System.Diagnostics.Debug.WriteLine($"[UserAlertManager] {nameof(AlertManagerService_OnUserAlertsJoinCountLoaded)} Complete");

            IsBusy = false;
            if (e.Error == null)
            {
                SetupSystemExceptionsAndAlerts();
                ResetUserAlerts();
                if (_RefreshAlerts)
                {
                    _RefreshAlerts = false;
                    LoadAlerts(EntityManager.Current.IsOnline);
                }
            }

            isLoadAlertsCount = false;
        }

        private void SetupSystemExceptionsAndAlerts()
        {
            int prevKey = SelectedAlertsDisplay?.ExceptAlertKey ?? 0;
            UserAlertsJoinCount sea = null;
            _SelectedAlertsView.Source = null;
            _SelectedAlertsView.SortDescriptions.Clear();
            if ((AlertManagerService != null) && (AlertManagerService.AlertsContext_UserAlertsJoinCounts != null))
            {
                _SelectedAlertsView.Source = AlertManagerService.AlertsContext_UserAlertsJoinCounts.Where(p => p.ExceptAlertKey != 0).ToList();
                SelectedAlertsView.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));
                SelectedAlertsView.Refresh();
                sea = GetUserAlertsJoinCountByKey(prevKey);
            }

            SetSelectedAlertsDisplay(sea, false);
        }

        public UserAlertsJoinCount GetUserAlertsJoinCountByKey(int key)
        {
            if ((AlertManagerService == null) || (AlertManagerService.AlertsContext_UserAlertsJoinCounts == null) ||
                (key <= 0)) return null;
            return AlertManagerService.AlertsContext_UserAlertsJoinCounts.FirstOrDefault(p => p.ExceptAlertKey == key);
        }

        private void AlertManagerService_LoadAlertNotificationsComplete(object sender, EntityEventArgs<UserAlertsJoin> e)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[UserAlertManager] {nameof(AlertManagerService_LoadAlertNotificationsComplete)} Complete");

            isLoadAlerts = false;
            IsBusy = false;
            if (e.Error == null)
            {
                SelectedServiceLineGroup = null;
                ResetUserAlerts();
                SendAlertsChangedMessage(new NullConcurrencyConflict());
            }
            else
            {
                MessageBox.Show(e.Error.ToString());
            }
        }

        private void ResetUserAlerts()
        {
            string prevPatient = PreviousSelectedPatient ?? AllValue;
            SetSelectedPatientDisplay(prevPatient, false);

            string prevCareCoordinator =
                PreviousSelectedCareCoordinator ?? AllValue;
            SetSelectedCareCoordinatorDisplay(prevCareCoordinator, false);

            Messenger.Default.Send<bool>(true, "AlertsChanged");
            LastUpdate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        }

        private void AlertManagerService_OnClientLoaded(object sender, ErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[UserAlertManager] {nameof(AlertManagerService_OnClientLoaded)} Complete");

            IsBusy = false;
        }

        private void AlertManagerService_OnAlertContextSaved(object sender, ErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[UserAlertManager] {nameof(AlertManagerService_OnAlertContextSaved)} Complete");

            IsBusy = false;

            //NOTE: e.Message will equal "Submit operation failed due to conflicts..." when MarkAlertAsRemoved() called to mark/update a UserAlertStatus row that no longer exists, which 
            //      will cause a DbUpdateConcurrencyException on the server
            if (e.Message != null &&
                e.Message.Equals(
                    "Submit operation failed due to conflicts. Please inspect Entity.EntityConflict for each entity in EntitiesInError for more information."))
            {
                var _UserAlertStatusKey = 0;
                if (e.UserState != null)
                {
                    int _key = 0;
                    if (Int32.TryParse(e.UserState.ToString(), out _key))
                    {
                        _UserAlertStatusKey = _key;
                    }
                }

                SendAlertsChangedMessage(new ConcurrencyConflict(_UserAlertStatusKey));
            }
            else
            {
                SendAlertsChangedMessage(new NullConcurrencyConflict());
            }
        }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task ProcessOfflineTasks()
        {
            var task_keys =
                await DynamicFormSipManager.Instance.GetTaskKeys(OfflineStoreType.CACHE | OfflineStoreType.SAVE);
            OfflineTaskKeys.Clear();
            OfflineTaskKeys.AddRange(task_keys);
        }

        private void SetSelectedAlertsDisplay(UserAlertsJoinCount value, bool SendNotification)
        {
            _SelectedAlertsDisplay = value;
            if ((SendNotification) && (isLoadAlertsCount == false)) LoadAlertsCount(true);
        }

        private void SetSelectedPatientDisplay(String value, bool SendNotification)
        {
            _SelectedPatientDisplay = value;
            if (SendNotification) SendAlertsChangedMessage(new NullConcurrencyConflict());
        }

        private void SetSelectedCareCoordinatorDisplay(String value, bool SendNotification)
        {
            _SelectedCareCoordinatorDisplay = value;
            if (SendNotification) SendAlertsChangedMessage(new NullConcurrencyConflict());
        }

        private void SendAlertsChangedMessage(ConcurrencyConflict __concurrencyConflict)
        {
            Messenger.Default.Send<int>(__concurrencyConflict.KeyAsInt, "AlertFilterChanged");
        }

        private void SetupFilterSelectionList()
        {
            if (_SelectedAlertsView != null)
            {
                _SelectedAlertsView.Filter += ((sa, e) =>
                {
                    var sl = e.Item as UserAlertsJoinCount;
                    if (AlertManagerService.AlertsContext_UserAlertsJoins.FirstOrDefault(uj =>
                            uj.SourceAlertKey == sl.ExceptAlertKey) != null)
                    {
                        e.Accepted = true;
                    }
                    else
                    {
                        e.Accepted = false;
                    }
                });
            }
        }

        // This used to be in a lock statement.  Doesn't seem needed as it's only called on constructor or navigation events.
        public async System.Threading.Tasks.Task LoadAlertsCountData()
        {
            if (EntityManager.Current.IsOnline)
            {
                LoadAlertsCount(false);
            }
            else
            {
                IsBusy = true;

                var loadFromCache =
                    await AlertManagerSipManager.Instance.FormPersisted(WebContext.Current.User.MemberID,
                        OfflineStoreType.CACHE);
                if (loadFromCache)
                {
                    var cacheFile = AlertManagerSipManager.Instance.GetFileName(WebContext.Current.User.MemberID,
                        OfflineStoreType.CACHE);
                    var CareTaker = new PersistentAlertManagerCareTaker(new EncryptedFileStore(cacheFile));
                    CareTaker.Load();
                    AlertManagerService.LoadFromMementoAsync(CareTaker.Memento);
                    await ProcessOfflineTasks();
                    IsBusy = false;
                }
                else
                {
                    IsBusy = false;
                    //ERROR - we're on the Alert page when we had no network and it wasn't ever cached...
                    //Do not show message if offline - we're currently hiding the alert
                    //MessageBox.Show("Cannot load alert data, because there is no network connectivity and the data was never saved locally.");
                }
            }
        }

        //How is this different from LoadAlertsCountData()
        //Comment from PatientHomeViewModel -  //TODO - send message to alert manager to load alerts or otherwise background this...either way do not make home page wait...
        public void LoadAlertsCount(bool refreshAlerts)
        {
            if (isLoadAlertsCount) return;
            PreviousSelectedPatient = SelectedPatientDisplay;
            PreviousSelectedCareCoordinator = SelectedCareCoordinatorDisplay;
            bool IsOnline = EntityManager.Current.IsOnline;
            if (IsOnline)
            {
                IsBusy = true;
                isLoadAlertsCount = true;
                _RefreshAlerts = refreshAlerts;
                // empty the collection first
                UserProfile up = UserCache.Current.GetCurrentUserProfile();
                if (up != null)
                {
                    AlertManagerService.AlertsContext_UserAlertsJoinCounts.Clear();
                    if (EntityManager.Current.IsOnline)
                    {
                        AlertManagerService.GetUserAlertsJoinCountAsync();
                    }
                }
            }
        }

        private void LoadAlerts(bool IsOnline)
        {
            if (isLoadAlerts) return;
            isLoadAlerts = true;
            PreviousSelectedPatient = SelectedPatientDisplay;
            PreviousSelectedCareCoordinator = SelectedCareCoordinatorDisplay;
            if (IsOnline)
            {
                IsBusy = true;
                // empty the collection first
                UserProfile up = UserCache.Current.GetCurrentUserProfile();
                if (up != null)
                {
                    AlertManagerService.AlertsContext_UserAlertsJoins.Clear();
                    AlertManagerService.AlertsContext_Tasks.Clear();
                    AlertManagerService.AlertsContext_Encounters.Clear();
                    AlertManagerService.AlertsContext_UserAlertStatus.Clear();
                    if (EntityManager.Current.IsOnline)
                    {
                        int key = (SelectedAlertsDisplay == null) ? 0 : SelectedAlertsDisplay.ExceptAlertKey;
                        if (key <= 0) key = 0;
                        AlertManagerService.GetExceptionsAndAlertsForUserAsync(up.UserId, key);
                    }
                }
            }
        }

        public void MarkAlertAsRemoved(UserAlertsJoin JoinRow)
        {
            if (JoinRow == null) return;
            var SourceRow = JoinRow.UserAlertStatus;
            if (SourceRow == null) return;
            var updRow = AlertManagerService.AlertsContext_UserAlertStatus.FirstOrDefault(u => u.UserAlertStatusKey == SourceRow.UserAlertStatusKey);
            if (updRow == null) return;

            updRow.BeginEditting();
            updRow.FulfilledDate = DateTime.UtcNow;
            updRow.CancelStatus = 0; // mark Alert as Cancelled by User
            updRow.EndEditting();

            AlertManagerService.AlertsContext_SubmitChanges(updRow.UserAlertStatusKey);
        }

        public void MarkAlertAsFlagged(UserAlertsJoin JoinRow, bool Flagged)
        {
            lock (_lock)
            {
                if (JoinRow == null) return;
                var SourceRow = JoinRow.UserAlertStatus;
                if (SourceRow == null) return;
                var updRow = AlertManagerService.AlertsContext_UserAlertStatus.FirstOrDefault(u => u.UserAlertStatusKey == SourceRow.UserAlertStatusKey);
                if (updRow == null) return;

                updRow.BeginEditting();

                if (Flagged) updRow.FlaggedDateTime = DateTime.UtcNow;
                else updRow.FlaggedDateTime = null;

                updRow.EndEditting();
                AlertManagerService.AlertsContext_SubmitChanges(updRow.UserAlertStatusKey);
            }
        }

        public AlertManagerMemento GetMemento()
        {
            lock (_lock)
            {
                return ((IAlertManagerMemento)AlertManagerService).GetMemento();
            }
        }

        public void Cleanup()
        {

        }

        #endregion
    }
}