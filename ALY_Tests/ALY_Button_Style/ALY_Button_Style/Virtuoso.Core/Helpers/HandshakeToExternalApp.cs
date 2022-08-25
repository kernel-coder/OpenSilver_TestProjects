#region Usings

using System;
using System.Linq;
using System.Windows.Browser;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.View;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Helpers
{
    public class HandshakeToExternalApp
    {
        public HandshakeToExternalApp(UserProfile user)
        {
            UserName = user.UserName;
            UserId = user.UserId;
            IsEmployee = user.IsEmployee;
            IsTeamScheduler = user.HasRole("Team Scheduler");
        }

        public string UserName { get; private set; }
        public Guid UserId { get; private set; }
        public bool IsEmployee { get; private set; }
        public bool IsTeamScheduler { get; private set; }

        private string CrescendoDirectory => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                                             @"\Delta Health Technologies\Crescendo\";

        private bool SSMUseSubdomain => TenantSettingsCache.GetSSMUseSubDomain();

        private void Launch(string appPath, string queryString)
        {
            UserSessionService _userSessionService = new UserSessionService();
            _userSessionService.CreateNewSession(UserName, UserId, SessionId =>
            {
                var _queryString = String.Format(queryString, HttpUtility.UrlEncode(SessionId.ToString()));
                WebBrowserHelper.Show(appPath, _queryString);
            });
        }

        private void LaunchURI(string _uri, string queryString)
        {
            UserSessionService _userSessionService = new UserSessionService();
            _userSessionService.CreateNewSession(UserName, UserId, SessionId =>
            {
                string _queryString = (queryString == null)
                    ? ""
                    : String.Format(queryString, HttpUtility.UrlEncode(SessionId.ToString()));
                WebBrowserHelper.Show(_uri + _queryString);
            });
        }

        public void LaunchDashboards()
        {
            var _appPath = String.Format("/{0}/", (SSMUseSubdomain) ? Client.Core.ApplicationStoreInfo.AppName : "ct");
            Launch(_appPath, "s={0}&m=dashboard");
        }

        public void LaunchSSMReports()
        {
            var _appPath = String.Format("/{0}/", (SSMUseSubdomain) ? Client.Core.ApplicationStoreInfo.AppName : "ct");
            Launch(_appPath, "s={0}&m=reports");
        }

        public void LaunchIntake()
        {
            var _appPath = "/Intake/User/Login";
            Launch(_appPath, "Session={0}");
        }
        public void LaunchToolChoice()
        {
            if (this.IsEmployee)
            {
                // PBI 56130
                // IsEmployee should direct the IsEmployee user to Tempo and auto-authenticate them into Tempo.
                this.LaunchCrescendoPortal();
            }
            else
            {
                BackOfficeLaunchDialog cw = new BackOfficeLaunchDialog();
                cw.Show();
            }
        }
        public void LaunchAdminPortal()
        {
            var _appPath = String.Format("/{0}/{1}/CRAuth.aspx", (SSMUseSubdomain) ? Virtuoso.Client.Core.ApplicationStoreInfo.AppName : "AM", "Admin");
            Launch(_appPath, String.Format("u={0}&t=", HttpUtility.UrlEncode(this.UserName)) + "{0}");
        }
        public void LaunchCrescendoPortal()
        {
            string _appName = Client.Core.ApplicationStoreInfo.AppName;

            //_appName = "spttcs00";
            string _uri = String.Format("https://{0}.app.deltahealth.com/Tempo/Auth/Clinical", _appName);

            var hostOverride = TenantSettingsCache.Current
                .TenantSetting
                .ApplicationSetting
                .FirstOrDefault(a => a.Key == Constants.AppSettings.HOST_OVERRIDE);

            // If wanting remote Tempo given HOST_OVERRIDE, else need more config/code to choose between local
            // or remote versions of any application that this class it attempting to launch
            // E.G. if we wanted to launch the localhost instance of Tempo - we need another key to include the port number.
            if (hostOverride != null && !string.IsNullOrEmpty(hostOverride.Value))
            {
                _uri = String.Format("https://{0}/Tempo/Auth/Clinical", hostOverride.Value);
            }

            LaunchURI(_uri, String.Format("?u={0}&s=", HttpUtility.UrlEncode(UserName)) + "{0}");
        }
    }
}