#region Usings

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Ria.Sync.Occasional;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client.ApplicationServices;
using Virtuoso.Client.Core;
using Virtuoso.Client.Offline;
using Virtuoso.Client.Utils;
using Virtuoso.Services.Authentication;
using Virtuoso.Services.Core.Model;
//DomainContext extension functions to save to disk

#endregion

namespace Virtuoso.Core
{
    public class OfflineAuthentication : FormsAuthentication
    {
        string LAST_LOGGED_IN_USER_FILE { get; set; }
        string KEY { get; set; }

        public static string LAST_LOGGED_IN_USER_ID { get; set; }
        public static string LAST_LOGGED_IN_USER_PWD { get; set; }

        private EntityManager EntityManager
        {
            get
            {
                //in a browser - probably the intall - do not instantiate EntityManager
                if ((System.Windows.Application.Current.IsRunningOutOfBrowser) == false)
                {
                    return null;
                }

                return EntityManager.Current;
            }
        }

        private DispatcherTimer idleTimer;
        private int minutesIdle;
        private bool idle;
        private bool attached;

        // hibernation / sleep timer
        private Timer hibernateTimer;
        DateTime lastTickTime;
        private SynchronizationContext uiContext;

        public OfflineAuthentication()
            : this(60)
        {
        }


        public OfflineAuthentication(int idleMinutes)
        {
            DomainContext = new AuthenticationContext();

            if (System.Windows.Application.Current.IsRunningOutOfBrowserOrOpenSilver()) //TODO: test the install from browser to see if we need to make this check?
            {
                SHA256Managed sha = new SHA256Managed();
                byte[] hash =
                    sha.ComputeHash(
                        UTF8Encoding.UTF8.GetBytes(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
#if !OPENSILVER
                KEY = Convert.ToBase64String(hash);
#endif
#if DEBUG
                LAST_LOGGED_IN_USER_FILE =
                    Path.Combine(ApplicationStoreInfo.GetUserStoreForApplication(Constants.PRIVATE_APPDATA_FOLDER),
                        Constants.AUTH_USER_SAVE_FILENAME);
#else
                LAST_LOGGED_IN_USER_FILE =
 Path.Combine(ApplicationStoreInfo.GetUserStoreForApplication(Constants.PRIVATE_APPDATA_FOLDER), Constants.AUTH_USER_SAVE_ENCRYPTED_FILENAME);
#endif
            }

            IdleMinutesBeforeTimeout = idleMinutes;
            idleTimer = new DispatcherTimer();
            idleTimer.Interval = TimeSpan.FromMinutes(1);
            idleTimer.Tick += idleTimer_Tick;

            // Used to detect sleep or hibernation.
            uiContext = SynchronizationContext.Current; // so we can call back on this thread later
            lastTickTime = DateTime.Now;
#if !OPENSILVER // Temporary until this can be put on a background thread (or Web Worker)
            hibernateTimer = new Timer(
                HibernateTimerOnTick,
                null,
                TimeSpan.FromSeconds(2), // first tick
                TimeSpan.FromSeconds(2)); // subsequent ticks
#endif
        }

        private void HibernateTimerOnTick(object param)
        {
            var intervalSinceLastTick = DateTime.Now - this.lastTickTime;

            // 2 seconds should have passed. If more than 30 passed, assume that
            // system is in hybernate or sleep state. As long as Blazor doesn't
            // support multiple threads, false positives could happen with the
            // single thread handling long CPU task and starving the message pump.
            if (intervalSinceLastTick > TimeSpan.FromSeconds(30))
            {
                AsyncUtility.RunOnMainThread(() =>
                {
                    // This should be running on the same thread as the viewmodel and UI.
                    if (this.idleTimer.IsEnabled)
                    {
                        this.StopInActivityTimer();
                        Messenger.Default.Send((int)Math.Round(intervalSinceLastTick.TotalMinutes),
                            "InactivityTimeout");
                    }
                });
            }

            lastTickTime = DateTime.Now;
        }

        public int IdleMinutesBeforeTimeout { get; set; }

        private void AttachEvents()
        {
            attached = true;
            System.Windows.Application.Current.RootVisual.MouseMove += RootVisual_MouseMove;
            System.Windows.Application.Current.RootVisual.KeyDown += RootVisual_KeyDown;
        }

        private void RootVisual_KeyDown(object sender, KeyEventArgs e)
        {
            idle = false;
        }

        private void RootVisual_MouseMove(object sender, MouseEventArgs e)
        {
            idle = false;
        }

        private void idleTimer_Tick(object sender, EventArgs e)
        {
            if (idle)
            {
                minutesIdle += idleTimer.Interval.Minutes;
                if (minutesIdle >= IdleMinutesBeforeTimeout)
                {
                    StopInActivityTimer();
                    Messenger.Default.Send(minutesIdle, "InactivityTimeout");
                }
            }
            else
            {
                minutesIdle = 0;
            }

            idle = true;
        }

        public void StartInActivityTimer()
        {
            if (!attached)
            {
                AttachEvents();
            }

            minutesIdle = 0;
#if !DEBUG
            idleTimer.Start();
#endif
        }

        public void StopInActivityTimer()
        {
            idleTimer.Stop();
        }

        protected override IPrincipal CreateDefaultUser()
        {
            if ((System.Windows.Application.Current.IsRunningOutOfBrowser) == false)
            {
                return base.CreateDefaultUser(); //in a browser - probably the intall - do not instantiate EntityManager
            }

            if (EntityManager
                .IsOnline) //checks if 'working offline' also...OK to do if OOB and end user choose to work offline
            {
                return base.CreateDefaultUser();
            }

            User user = new User() //User user = this.ReadUser();
            {
                TenantID = -1,
                Name = String.Empty //This makes the user object - NOT AUTHENTICATED - on the client side
            };
            return user;
        }

        protected override IAsyncResult BeginLogin(LoginParameters parameters, AsyncCallback callback, object state)
        {
            LAST_LOGGED_IN_USER_ID = parameters.UserName;
            LAST_LOGGED_IN_USER_PWD = parameters.Password;

            if ((System.Windows.Application.Current.IsRunningOutOfBrowser) == false)
            {
                return
                    base.BeginLogin(parameters, callback,
                        state); //in a browser - probably the intall - do not instantiate EntityManager
            }

            if (EntityManager.IsOnline)
            {
                return base.BeginLogin(parameters, async ar => {
                   // This is a callback that can be used in lieu of EndLogin.
                   // In the case of running out of browser false in EndLogin(), this is returned immediately above, and for the OfflineLoginAsyncResult,
                   // this code path is not executed (see lines below this one).
                   // This callback would only be executed if an online login is completed, similar to EndLogin().
                   await WriteUser();
                   callback(ar);
                }, state);
            }

            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            var offlineLoginAsyncResult = new OfflineLoginAsyncResult(callback, state);
            AsyncUtility.Run(async () =>
            {
               User userProfile = await ValidateCredentials(parameters.UserName, parameters.Password);
               offlineLoginAsyncResult.Complete(userProfile);
            });

            return offlineLoginAsyncResult;
        }

        protected override LoginResult EndLogin(IAsyncResult asyncResult)
        {
            if ((System.Windows.Application.Current.IsRunningOutOfBrowser) == false)
            {
                //in a browser - probably the intall, do not instantiate EntityManager or inactivity times
                return base.EndLogin(asyncResult);
            }

            if (asyncResult is OfflineLoginAsyncResult)
            {
                var offlineResult = asyncResult as OfflineLoginAsyncResult;
                User _user = null;
                if (offlineResult.User != null)
                {
                    _user = offlineResult.User;

                    StartInActivityTimer();
                }

                return new LoginResult(_user, _user != null);
            }

            var result = base.EndLogin(asyncResult);
            if (result.LoginSuccess)
            {
                StartInActivityTimer();
            }

            return result;
        }

        protected override IAsyncResult BeginLogout(AsyncCallback callback, object state)
        {
            if ((System.Windows.Application.Current.IsRunningOutOfBrowser) == false)
            {
                return
                    base.BeginLogout(callback,
                        state); //in a browser - probably the intall - do not instantiate EntityManager
            }

            if (EntityManager.IsOnline)
            {
                return base.BeginLogout(callback, state);
            }

            return new OfflineLogoutAsyncResult(callback, state).Complete();
        }

        protected override LogoutResult EndLogout(IAsyncResult asyncResult)
        {
            StopInActivityTimer();

            if ((System.Windows.Application.Current.IsRunningOutOfBrowser) == false)
            {
                return base.EndLogout(asyncResult); //in a browser - shouldn't ever be called - do not instantiate EntityManager
            }
            //else if (EntityManager.IsOnline)
            //    return base.EndLogout(asyncResult);
            //else
            //    return new LogoutResult(CreateDefaultUser()); //return new LogoutResult(WebContext.Current.User);                

            if (asyncResult is OfflineLogoutAsyncResult)
            {
                return new LogoutResult(CreateDefaultUser());
            }

            return base.EndLogout(asyncResult);
        }

#region ManageOfflineUser
        private async Task<User> ReadUser()
        {
#if DEBUG
            await DomainContext.RestoreFromFileStore(LAST_LOGGED_IN_USER_FILE, string.Empty);
#else
            await this.DomainContext.RestoreFromFileStore(LAST_LOGGED_IN_USER_FILE, KEY);
#endif
            //return this.DomainContext.EntityContainer.GetEntitySet<Virtuoso.Services.Core.Model.User>().SingleOrDefault();  //sequence contains more than one element - default user and last logged in user
            return DomainContext.EntityContainer.GetEntitySet<User>().Where(u => String.IsNullOrEmpty(u.Name) == false)
                .FirstOrDefault();
        }

        private async Task WriteUser()
        {
#if DEBUG
            await DomainContext.SaveToFileStore(LAST_LOGGED_IN_USER_FILE, string.Empty);
#else
            await this.DomainContext.SaveToFileStore(LAST_LOGGED_IN_USER_FILE, KEY);
#endif
        }

#endregion

#region Validation
        private async Task<User> ValidateCredentials(string name, string password)
        {
            var _user = Cache.LoginCache.GetUserProfileFromUserID(name);
            var _cachedUser = await ReadUser();
            if ((_user == null) || (_cachedUser == null))
            {
                return null;
            }

            var _valid = ValidatePassword(password, _user.Password);
            if ((_user != null) && _valid && _cachedUser.MemberID == _user.MemberID)
            {
                //FYI: need to return _cachedUser to have a 'User' object with Roles properly set - e.g. Roles property cannot be set on the client
                //     must have Roles, else once you log into the application, you will have nothing in the main menu...
                return _cachedUser;
            }

            return null;
        }

        private const int PasswordSaltSize = 8;
        private const int PasswordSaltLength = 12; // Accounts for base64 overhead

        private static string HashPassword(string password, string passwordSalt)
        {
            //string hash = FormsAuthentication.HashPasswordForStoringInConfigFile(password + passwordSalt, "SHA1");
            string hash = GetSHA1Hash(password + passwordSalt);

            // Embed the salt into the hash, so we can retrieve it later when validating
            // passwords.
            return hash + passwordSalt;
        }

        public static string GetSHA1Hash(string val)
        {
            byte[] data = Encoding.UTF8.GetBytes(val);
            SHA1 sha = new SHA1Managed();
            byte[] res = sha.ComputeHash(data);
            return BitConverter.ToString(res).Replace("-", "").ToUpper();
        }

        /// <summary>
        /// Validates the specified password by comparing with to a previously computed hash value.
        /// </summary>
        /// <param name="password">The password to validate.</param>
        /// <param name="storedPasswordHash">The previously computed password hash to compare against.</param>
        /// <returns>true if the password is valid; false otherwise.</returns>
        public static bool ValidatePassword(string password, string storedPasswordHash)
        {
            if (String.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException("password");
            }

            if (String.IsNullOrEmpty(storedPasswordHash))
            {
                throw new ArgumentNullException("storedPasswordHash");
            }

            // Extract the random password salt that was generated.
            string passwordSalt = storedPasswordHash.Substring(storedPasswordHash.Length - PasswordSaltLength);

            // Now take the password that is to be validated and hash it with the same salt,
            // and compare the two hashes.
            string passwordHash = HashPassword(password, passwordSalt);

            return (String.CompareOrdinal(passwordHash, storedPasswordHash) == 0);
        }

#endregion

#region OfflineAsyncResult

        private class OfflineLogoutAsyncResult : IAsyncResult
        {
            private readonly AsyncCallback _asyncCallback;
            private readonly object _asyncState;

            public OfflineLogoutAsyncResult(AsyncCallback asyncCallback, object asyncState)
            {
                _asyncCallback = asyncCallback;
                _asyncState = asyncState;
            }

            public object AsyncState => _asyncState;

            public WaitHandle AsyncWaitHandle
            {
                get { throw new NotImplementedException(); }
            }

            public bool CompletedSynchronously => false;

            public bool IsCompleted => true;

            public OfflineLogoutAsyncResult Complete()
            {
                //Application.Current.RootVisual.Dispatcher.BeginInvoke(() =>
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (_asyncCallback != null)
                    {
                        _asyncCallback(this);
                    }
                });
                return this;
            }
        }

        private class OfflineLoginAsyncResult : IAsyncResult
        {
            private readonly AsyncCallback _asyncCallback;
            private readonly object _asyncState;

            //public OfflineLoginAsyncResult(UserProfile userProfile, AsyncCallback asyncCallback, object asyncState)
            //public OfflineLoginAsyncResult(UserAccount userProfile, AsyncCallback asyncCallback, object asyncState)
            public OfflineLoginAsyncResult(AsyncCallback asyncCallback, object asyncState)
            {
                _asyncCallback = asyncCallback;
                _asyncState = asyncState;
            }

            //public UserProfile UserProfile
            //public UserAccount User
            public User User { get; private set; }

            public object AsyncState => _asyncState;

            public WaitHandle AsyncWaitHandle
            {
                get { throw new NotImplementedException(); }
            }

            public bool CompletedSynchronously => false;

            public bool IsCompleted { get; private set; }

            public OfflineLoginAsyncResult Complete(User user)
            {
                this.User = user;
                this.IsCompleted = true;

                //Application.Current.RootVisual.Dispatcher.BeginInvoke(() =>
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (_asyncCallback != null)
                    {
                        _asyncCallback(this);
                    }
                });
                return this;
            }
        }

#endregion
    }
}