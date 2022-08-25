#region Usings

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using System.Xml;
using System.Xml.Linq;
using Virtuoso.Client.Infrastructure;
using Virtuoso.Core;

#endregion

namespace Virtuoso.Client.Core
{
    public class ApplicationFolders
    {
        public Environment.SpecialFolder ApplicationStoreSpecialFolder { get; set; }
        public Uri BaseUri { get; set; }
        public String Root { get; set; }

        // NOTE: TenantRoot = full sub domain, so that we can simultaneously support distinct installs from the following:
        // Given URL = ident.crescendoit.com     - return ident
        // Given URL = ident.app.crescendoit.com - return ident.app
        //
        // On the backend, these will both talk to the same database.  On the frontend, these need to be separate installs.
        // Given URL = ident.crescendoit.com     - install to C:\Users\<user>\AppData\Local\Delta Health Technologies\Crescendo\ident
        // Given URL = ident.app.crescendoit.com - install to C:\Users\<user>\AppData\Local\Delta Health Technologies\Crescendo\ident.app
        public String TenantRoot { get; set; }

        // NOTE: This will equal TenantRoot
        //       Also applicable - ApplicationStoreInfo.AppName is another pass thru for first part of subdomain.
        public String ApplicationID { get; set; }

        public String ApplicationStore { get; set; }
        public String ApplicationStoreSubFolder { get; set; }
        public String ApplicationStoreRefData { get; set; }
        public String ApplicationStoreLogs { get; set; }
        public String ApplicationStoreErrorDetailLogs { get; set; }
    }

    enum SubDomainType
    {
        COMPLETE,
        FIRST_COMPONENT
    }

    public static class ApplicationStoreInfo
    {
        //DS 14201 10/21/14 
        public class NavigationBookmark
        {
            private string _navigationLocation;
            private DateTime _clientDateTime;

            public NavigationBookmark(string navigationLocation)
            {
                _navigationLocation = navigationLocation;
                _clientDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            }

            public string NavigationLocation
            {
                get { return _navigationLocation; }
                set { _navigationLocation = value; }
            }

            public DateTime ClientDateTime
            {
                get { return _clientDateTime; }
                set { _clientDateTime = value; }
            }
        }

        static ApplicationStoreInfo()
        {
            if (Application.Current.IsRunningOutOfBrowserOrOpenSilver())
            {
                var applicationFolders = GetApplicationFolders(Environment.SpecialFolder.LocalApplicationData);

                ApplicationStoreSpecialFolder = applicationFolders.ApplicationStoreSpecialFolder;
                ApplicationID = applicationFolders.ApplicationID;
                ApplicationStoreSubFolder = applicationFolders.ApplicationStoreSubFolder;
                ApplicationStore = applicationFolders.ApplicationStore;
                ApplicationStoreRefData = applicationFolders.ApplicationStoreRefData;
                ApplicationStoreLogs = applicationFolders.ApplicationStoreLogs;
                ApplicationStoreErrorDetailLogs = applicationFolders.ApplicationStoreErrorDetailLogs;
            }
        }

#if !OPENSILVER
        public static void CreateDirectories()
        {
            if (Application.Current.IsRunningOutOfBrowser)
            {
                if (Directory.Exists(ApplicationStore) == false)
                {
                    Directory.CreateDirectory(ApplicationStore); //create all directories and subdirectories
                }

                if (Directory.Exists(ApplicationStoreRefData) == false)
                {
                    Directory.CreateDirectory(ApplicationStoreRefData);
                }

                // Put logs in a different place so users can delete the application store folder as necessary
                // to delete cache, but not wipe out logs.
                if (Directory.Exists(ApplicationStoreLogs) == false)
                {
                    Directory.CreateDirectory(ApplicationStoreLogs);
                }

                if (Directory.Exists(ApplicationStoreErrorDetailLogs) == false)
                {
                    Directory.CreateDirectory(ApplicationStoreErrorDetailLogs);
                }
            }
        }
#endif

        public static ApplicationFolders GetApplicationFolders(Environment.SpecialFolder specialFolder)
        {
            var applicationFolders = new ApplicationFolders();

            if (Application.Current.IsRunningOutOfBrowserOrOpenSilver())
            {
                applicationFolders.Root =
                    Path.Combine(Environment.GetFolderPath(specialFolder), CompanyName, ApplicationName);
                applicationFolders.ApplicationStoreSpecialFolder = specialFolder;

                applicationFolders.BaseUri = Application.Current.Host.Source;

                // Given URL = ident.crescendoit.com     - return ident
                // Given URL = ident.app.crescendoit.com - return ident.app
                applicationFolders.TenantRoot =
                    GetSubDomainFromURL(applicationFolders.BaseUri,
                        SubDomainType.COMPLETE); //determine the sub domain name from the BaseUri

                try
                {
                    //INFO: 'Tenant' name for unit tests equal the assembly name
                    if (applicationFolders.TenantRoot == null &&
                        applicationFolders.BaseUri.Scheme.Equals("file") &&
                        applicationFolders.BaseUri.LocalPath.EndsWith(".Tests.xap"))
                    {
                        applicationFolders.TenantRoot =
                            Path.GetFileNameWithoutExtension(applicationFolders.BaseUri.LocalPath);
                    }
                }
                catch (Exception e)
                {
                    var msg = e.Message;
                    throw;
                }

                applicationFolders.ApplicationID = String.Format("{0}", applicationFolders.TenantRoot);
                applicationFolders.ApplicationStoreSubFolder =
                    Path.Combine(CompanyName, ApplicationName, applicationFolders.ApplicationID);

                applicationFolders.ApplicationStore = Path.Combine(
                    Environment.GetFolderPath(applicationFolders.ApplicationStoreSpecialFolder),
                    CompanyName,
                    ApplicationName,
                    applicationFolders.ApplicationID);

                applicationFolders.ApplicationStoreRefData = Path.Combine(
                    Environment.GetFolderPath(applicationFolders.ApplicationStoreSpecialFolder),
                    CompanyName,
                    ApplicationName,
                    applicationFolders.ApplicationID,
                    "ReferenceData");

                // Put logs in a different place so users can delete the application store folder as necessary
                // to delete cache, but not wipe out logs.
                applicationFolders.ApplicationStoreLogs = Path.Combine(
                    Environment.GetFolderPath(applicationFolders.ApplicationStoreSpecialFolder),
                    CompanyName,
                    ApplicationName,
                    "Logs",
                    applicationFolders.ApplicationID);

                applicationFolders.ApplicationStoreErrorDetailLogs = Path.Combine(
                    Environment.GetFolderPath(applicationFolders.ApplicationStoreSpecialFolder),
                    CompanyName,
                    ApplicationName,
                    "ErrorDetailLogs",
                    applicationFolders.ApplicationID);
            }

            return applicationFolders;
        }

        public static async Task DeleteDotFolderContent()
        {
            var applicationFoldersLocalApplicationData =
                GetApplicationFolders(Environment.SpecialFolder.LocalApplicationData);
            await FileUtility.DeleteFilesInFolder(Path.Combine(new[]
                { applicationFoldersLocalApplicationData.ApplicationStore, Constants.PRIVATE_APPDATA_FOLDER }));
            await FileUtility.DeleteFilesInFolder(Path.Combine(new[]
                { applicationFoldersLocalApplicationData.ApplicationStore, Constants.SAVE_FOLDER }));
            await FileUtility.DeleteFilesInFolder(Path.Combine(new[]
                { applicationFoldersLocalApplicationData.ApplicationStore, Constants.CACHE_FOLDER }));
            await FileUtility.DeleteFilesInFolder(Path.Combine(new[]
                { applicationFoldersLocalApplicationData.ApplicationStore, Constants.DATA_STORE_FOLDER }));
            await FileUtility.DeleteFilesInFolder(Path.Combine(new[]
                { applicationFoldersLocalApplicationData.ApplicationStore, Constants.AUTOSAVE_FOLDER }));
        }

        public static Uri Host => Application.Current.Host.Source;

        public static string AppName =>
            // Given URL = ident.crescendoit.com     - return ident
            // Given URL = ident.app.crescendoit.com - return ident
            GetSubDomainFromURL(Host);

        public static string CompanyName => "Delta Health Technologies";

        public static string ApplicationName => "Crescendo";

        private static string ApplicationID;
        private static string ApplicationStore = null; //disk location for application storage

        public static string ApplicationStoreRefData { get; set; }

        public static string ApplicationStoreLogs { get; set; }
        public static string ApplicationStoreErrorDetailLogs { get; set; }

        public static string ApplicationStoreSubFolder { get; set; }
        public static Environment.SpecialFolder ApplicationStoreSpecialFolder { get; set; }

        //DS 14201 10/21/14 
        public static NavigationBookmark ApplicationNavBookmark { get; set; }

        public static string GetUserStoreForApplication(string subFolder = null)
        {
            if (ApplicationStore == null)
            {
                throw new InvalidOperationException("ApplicationStore is accessed before it is set");
            }

            if (String.IsNullOrEmpty(subFolder))
            {
                return ApplicationStore;
            }

            var subFolderPath = Path.Combine(ApplicationStore, subFolder);

#if OPENSILVER
            // Path.Combine combines duplicate paths, apparently if there is a drive, but does not work for our "virtual" case
            if (subFolder?.StartsWith(ApplicationStore) == true) {
                subFolderPath = subFolder;
            }
#else
            if (Directory.Exists(subFolderPath) == false)
            {
                var directoryInfo =
                    Directory.CreateDirectory(subFolderPath); //create all directories and subdirectories
                if (subFolder.StartsWith("."))
                {
                    directoryInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                }
            }
#endif
            return subFolderPath;
        }

        private static string GetSubDomainFromURL(Uri url, SubDomainType parseType = SubDomainType.FIRST_COMPONENT)
        {
            string host = url.Host;

            if (host.Equals("localhost"))
            {
                return host;
            }

            switch (parseType)
            {
                case (SubDomainType.COMPLETE):
                    // Given URL = ident.crescendoit.com     - return ident
                    // Given URL = ident.app.crescendoit.com - return ident.app                 
#if OPENSILVER // handle debug hosts to avoid IndexOutOfRangeException
                    if (!host.Contains("."))
                    {
                        return host;
                    }
#endif
                    int lastIndex = host.LastIndexOf(".");
                    int index = host.LastIndexOf(".", lastIndex - 1);
                    string retAll = host.Substring(0, index);
                    return retAll;
                default: // E.G. parseType = SubDomainType.FIRST_COMPONENT
                    // Given URL = ident.crescendoit.com     - return ident
                    // Given URL = ident.app.crescendoit.com - return ident
                    string retFirst = host.Split('.')[0];
                    return retFirst;
            }
        }
    }
}
