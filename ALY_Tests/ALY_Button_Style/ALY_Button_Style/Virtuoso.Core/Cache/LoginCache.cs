#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Windows;
using OpenRiaServices.Data.DomainServices;
using OpenRiaServices.DomainServices.Client;
using Ria.Common;
using Virtuoso.Client.Core;
using Virtuoso.Common;
using Virtuoso.Core.Services;
using Virtuoso.Metrics;
using Virtuoso.Services.Core.Model;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Cache
{
    [Export(typeof(LoginCache))]
    public class LoginCache : CacheBaseV2<User>, ICorrelationID, IMetricsTimer
    {
        MetricsTimerHelper LoadEvent { get; set; }
        CorrelationIDHelper CorrelationIDHelper { get; set; }
        List<QueuedLogData> MetricsTimerData { get; set; }

        private static LoginCache Current { get; set; }
        private VirtuosoDomainContext Context;
        private List<User> _userAccounts;

        private List<User> UserAccounts
        {
            get
            {
                if (_userAccounts == null)
                {
                    _userAccounts = new List<User>();
                }

                return _userAccounts;
            }
        }

        DateTime NewClientAnchor { get; set; }

        [ImportingConstructor]
        public LoginCache(ILogger logManager)
            : base(logManager, "Login", "006")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("LoginCache already initialized.");
            }

            Current = this;
            Ticks = 0;

            CorrelationIDHelper = new CorrelationIDHelper();
            MetricsTimerData = new List<QueuedLogData>();

            var BaseUri = System.Windows.Application.Current.GetServerBaseUri();
            Context = new VirtuosoDomainContext(new Uri(BaseUri,
                "Virtuoso-Services-Web-VirtuosoDomainService.svc")); //using alternate constructor, so that it can run in a thread
        }

        private async System.Threading.Tasks.Task LoadFromDiskInternal()
        {
            var _diskEntities = await CacheHelper.Load<User>(ApplicationStore, CacheName); //pre load cache
            UserAccounts.Clear();
            foreach (var _current in _diskEntities)
                UserAccounts.Add(_current);
        }

        public System.Threading.Tasks.Task<ServiceLoadResult<User>> GetUserAccountByDeltaAsync(bool _isOnline)
        {
            //NOTE: in order to background the 'entire' WCF service call - create the Task in a thread           
            var source = new System.Threading.Tasks.TaskCompletionSource<ServiceLoadResult<User>>();
            System.Threading.Tasks.Task.Factory.StartNew(async () =>
            {
                try
                {
                    await Load(_isOnline, slr =>
                    {
                        if (slr.Cancelled)
                        {
                            source.SetCanceled();
                        }

                        if (slr.Error != null)
                        {
                            source.SetException(slr.Error);
                        }

                        if (source.Task.IsFaulted == false)
                        {
                            source.SetResult(slr);
                        }
                    });
                }
                catch (Exception exc)
                {
                    source.SetException(exc);
                }
            });
            return source.Task;
        }

        public override async System.Threading.Tasks.Task Load(bool _isOnline, Action<ServiceLoadResult<User>> callback)
        {
            await RemovePriorVersion();

            if (isLoading)
            {
                callback?.Invoke(
                    new ServiceLoadResult<User>(
                        UserAccounts,
                        UserAccounts.Count,
                        new List<System.ComponentModel.DataAnnotations.ValidationResult>(),
                        new InvalidOperationException(
                            "Cannot load LoginCache.  Reason: LoginCache is currently loading."),
                        false, //op.IsCanceled,
                        null //op.UserState
                    ));
                return;
            }

            isLoading = true;

            try
            {
                if (_isOnline)
                {
                    TotalRecords = 0;
                    NewClientAnchor = DateTime.UtcNow;
                    Ticks = NewClientAnchor.Ticks;

                    // Load whatever we have on disk
                    var _diskEntities = await CacheHelper.Load<User>(ApplicationStore, CacheName);

                    if (_diskEntities.Any()) // If we have cached users, submit to server to refresh local cache
                    {
                        LoadEvent = new MetricsTimerHelper(new StopWatchFactory(), CorrelationIDHelper,
                            Logging.Context.LoginCache_Load);

                        await Context
                            .LoadAsync(Context.GetUserAccountByTenantIDNoAuthQuery(
                                _diskEntities.Select(u => u.MemberID)))
                            .ContinueWith(async lop =>
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
                                        System.Diagnostics.Debug.WriteLine(
                                            "[003] EventType: {0}\tTimestamp: {1}\tData:{2}", _context.EventType,
                                            _context.Timestamp, log);
                                        MetricsTimerData.Add(new QueuedLogData { EventContext = _context, Data = log });
                                    }
                                }

                                if (lop.IsFaulted)
                                {
                                    Context.EntityContainer.Clear();
                                    LoadFromDisk(callback);
                                }
                                else
                                {
                                    var _serverEntities = lop.Result.ToList();
                                    _serverEntities.ForEach(_serverEntityRef =>
                                    {
                                        var _diskEntityRef = _diskEntities
                                            .Where(u => u.MemberID == _serverEntityRef.UserId).FirstOrDefault();
                                        if (_diskEntityRef != null) //update local reference with what is on server
                                        {
                                            _diskEntityRef.AccountLocked = _serverEntityRef.AccountLocked;
                                            _diskEntityRef.DeltaAdmin = _serverEntityRef.DeltaAdmin;
                                            _diskEntityRef.DeltaUser = _serverEntityRef.DeltaUser;
                                            _diskEntityRef.FirstName = _serverEntityRef.FirstName;
                                            _diskEntityRef.Inactive = _serverEntityRef.Inactive;
                                            _diskEntityRef.InactiveDate = _serverEntityRef.InactiveDate;
                                            _diskEntityRef.LastName = _serverEntityRef.LastName;
                                            _diskEntityRef.MiddleName = _serverEntityRef.MiddleName;
                                            _diskEntityRef.PhotoThumbnail = _serverEntityRef.PhotoThumbnail;
                                            _diskEntityRef.UserName = _serverEntityRef.UserName;
                                            _serverEntityRef.PerformanceMonitor = _serverEntityRef.PerformanceMonitor;
                                        }
                                    });
                                    foreach (var diskEntity in _diskEntities)
                                        await AddInternal(diskEntity, false); //add entity, defer save to disk

                                    await PurgeAndSave(); //save this.UserAccounts to disk
                                    LoadFromDisk(callback);
                                }
                            });
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
            catch (Exception)
            {
                LoadFromDisk(callback);
            }
        }

        //NOTE: LoadFromDisk DOES NOT restore Context from file
        private void LoadFromDisk(Action<ServiceLoadResult<User>> callback)
        {
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    await LoadFromDiskInternal();

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        isLoading = false;
                        TotalRecords = UserAccounts.Count;

                        callback?.Invoke(
                            new ServiceLoadResult<User>(
                                UserAccounts,
                                UserAccounts.Count,
                                new List<System.ComponentModel.DataAnnotations.ValidationResult>(),
                                null, //op.Error,
                                false, //op.IsCanceled,
                                null //op.UserState
                            ));
                    });
                }
                catch (Exception e)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        isLoading = false;
                        TotalRecords = 0;
                        callback?.Invoke(
                            new ServiceLoadResult<User>(
                                UserAccounts,
                                UserAccounts.Count,
                                new List<System.ComponentModel.DataAnnotations.ValidationResult>(),
                                new InvalidOperationException(string.Format("Cannot load LoginCache.  Reason: {0}.",
                                    e.ToString())),
                                false, //op.IsCanceled,
                                null //op.UserState
                            ));
                    });
                }
            });
        }

        private async System.Threading.Tasks.Task PurgeAndSave()
        {
            var cacheFile = await PurgeAndGetCacheFileName();

            await CacheHelper.Save(UserAccounts, cacheFile);
        }

        public static User GetUserProfileFromUserID(string userID, bool showMessageBox = true)
        {
            if ((Current == null))
            {
                return null;
            }

            if (string.IsNullOrEmpty(userID))
            {
                return null;
            }

            Guid _id;
            if (Guid.TryParse(userID, out _id))
            {
                User u = (from c in Current.UserAccounts.AsQueryable() where (c.MemberID == _id) select c)
                    .FirstOrDefault();
                if ((u == null) && (string.IsNullOrEmpty(userID) == false) && showMessageBox)
                {
                    MessageBox.Show(String.Format(
                        "Error LoginCache.GetUserProfileFromUserID: userID {0} is not defined.  Contact your system administrator.",
                        userID));
                }

                return u;
            }

            if (showMessageBox)
            {
                MessageBox.Show(String.Format(
                    "Error LoginCache.GetUserProfileFromUserID: userID {0} is not in correct format.  Contact your system administrator.",
                    userID));
            }

            return null;
        }

        public User GetUserProfileFromUserName(string userName)
        {
            if ((Current == null))
            {
                return null;
            }

            if (string.IsNullOrEmpty(userName))
            {
                return null;
            }

            User userAccount = UserAccounts.Where(u => u.UserName.ToUpper() == userName.ToUpper()).FirstOrDefault();

            return userAccount;
        }

        private ServiceLoadResult<T> CreateResult<T>(LoadOperation<T> op, bool returnEditableCollection = false)
            where T : Entity
        {
            if (op.HasError)
            {
                op.MarkErrorAsHandled();
            }

            return new ServiceLoadResult<T>(
                returnEditableCollection
                    ? new EntityList<T>(Context.EntityContainer.GetEntitySet<T>(), op.Entities)
                    : op.Entities,
                op.TotalEntityCount,
                op.ValidationErrors,
                op.Error,
                op.IsCanceled,
                op.UserState);
        }

        public void LockAccount(Guid userId, Action<ServiceInvokeResult<bool>> callback, object state)
        {
            Context.LockAccount(userId,
                so => { callback(CreateResult(so)); }, state);
        }

        public void IsNewPasswordValid(
            string newPassword,
            string oldPassword,
            string passwordHistory,
            int lengthtouse,
            Action<ServiceInvokeResult<Server.Data.PasswordPOCO>> callback, object state)
        {
            Context.IsNewPasswordValid(
                newPassword,
                oldPassword,
                passwordHistory,
                lengthtouse,
                so => { callback(CreateResult(so)); }, state);
        }

        public void SetSecurityQuestion(
            Guid userID,
            string securityQuestion,
            string securityAnswer,
            Action<ServiceInvokeResult<bool>> callback, object state)
        {
            Context.SetSecurityQuestion(
                userID,
                securityQuestion,
                securityAnswer,
                so => { callback(CreateResult(so)); }, state);
        }

        public void SetPassword(
            Guid userID,
            string rawpassword,
            string hashedpassword,
            DateTime? passwordChangeDate,
            bool passwordReset,
            int passwordHistoryLength,
            Action<ServiceInvokeResult<bool>> callback, object state)
        {
            Context.SetPassword(
                userID,
                rawpassword,
                hashedpassword,
                passwordChangeDate,
                passwordReset,
                passwordHistoryLength,
                so => { callback(CreateResult(so)); }, state);
        }

        private ServiceInvokeResult<T> CreateResult<T>(InvokeOperation<T> op)
        {
            if (op.HasError)
            {
                op.MarkErrorAsHandled();
            }

            return new ServiceInvokeResult<T>(
                op.Value,
                op.ValidationErrors,
                op.Error,
                op.IsCanceled,
                op.UserState);
        }

        private ServiceSubmitChangesResult CreateResult(SubmitOperation op)
        {
            if (op.HasError)
            {
                op.MarkErrorAsHandled();
            }

            return new ServiceSubmitChangesResult(
                op.ChangeSet,
                op.EntitiesInError,
                op.Error,
                op.IsCanceled,
                op.UserState);
        }

        public async System.Threading.Tasks.Task AddUser(User user)
        {
            await Current.AddInternal(user);
        }

        private async System.Threading.Tasks.Task AddInternal(User user, bool purgeAndSave = true)
        {
            //ensure user is added to cache
            var cachedUser = UserAccounts.Where(u => u.MemberID == user.MemberID).FirstOrDefault();
            if (cachedUser == null)
            {
                user.CacheDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                UserAccounts.Add(user);
            }
            else
            {
                UserAccounts.Remove(cachedUser);
                user.CacheDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                UserAccounts.Add(user);
            }

            if (purgeAndSave)
            {
                //Write cache to disk
                await PurgeAndSave();
            }
        }

        public static IEnumerable<User> GetUserAccounts()
        {
            foreach (var e in Current.UserAccounts) yield return e.Clone();
        }

        public static bool IsCachedAccount(Guid memberID)
        {
            return Current.UserAccounts.Any(c => c.MemberID == memberID);
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