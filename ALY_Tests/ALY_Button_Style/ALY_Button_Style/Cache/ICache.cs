#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ria.Sync;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using OpenRiaServices.DomainServices.Client;
using Ria.Common;
using Virtuoso.Client.Core;
using Virtuoso.Client.Offline;
using Virtuoso.Client.Infrastructure.Storage;
using Virtuoso.Client.Utils;
using Virtuoso.Core.Services;
using Virtuoso.Portable.Database;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Cache
{
   public interface IVirtuosoCacheMetadata
   {
      string CacheName { get; }
   }

   public interface ICache
    {
        string Version { get; }
        string CacheName { get; }
        Task Load(DateTime? lastUpdatedDate, bool isOnline, Action callback, bool force = false);
        void Reload(Action callback);
        ICacheManagement GetManagementInterface();
    }

    public interface ICacheManagement
    {
        void Initialize(bool deferLoad);
        Task LoadFromDisk();
    }

    public abstract class CacheBase : ICache
    {
        protected EntityManager EntityManager => EntityManager.Current;

        public string Version { get; internal set; }
        public string CacheName { get; internal set; }

        protected string SaveFileWithoutExtension { get; private set; }
        protected string ApplicationStore { get; private set; }
        protected bool isLoading = false;
        protected bool CreateFile;
        protected DateTime? LastUpdatedDate;
        protected long Ticks;

        private int _TotalRecords;

        protected int TotalRecords
        {
            get { return _TotalRecords; }
            set { _TotalRecords = value; }
        }

        private readonly ILogger _logManager;

        protected CacheBase(ILogger logManager)
        {
            _logManager = logManager;

            ApplicationStore = ApplicationStoreInfo.GetUserStoreForApplication();
        }

        protected CacheBase(ILogger logManager, string cacheName, string version)
        {
            _logManager = logManager;

            Version = version;
            CacheName = cacheName;

            SaveFileWithoutExtension = string.Format("{0}Cache-{1}", CacheName, Version);

            ApplicationStore = ApplicationStoreInfo.GetUserStoreForApplication();
        }

        public virtual void PreReload()
        {
        }

        public void Reload(Action callback)
        {
            PreReload();
            Load(LastUpdatedDate, EntityManager.IsOnline, callback, true)
                .ContinueWith(task => { },
                    TaskContinuationOptions.ExecuteSynchronously); // Do nothing - use callback instead
        }

        public void Reload(EntityChangeSet changeSet, Action callback)
        {
            Update(LastUpdatedDate, changeSet, callback);
        }

        public abstract Task Load(DateTime? lastUpdatedDate, bool isOnline, Action callback, bool force = false);

        public virtual void Update(DateTime? lastUpdatedDate, EntityChangeSet changeSet, Action callback)
        {
            //Must override in derived class
        }

        protected void Log(TraceEventType level, string message, Exception exception = null)
        {
            string category = string.Format("{0}-CACHE", CacheName);

            if (exception != null)
            {
                _logManager.Log(level, category, message, exception);
            }
            else
            {
                _logManager.Log(level, category, message);
            }
        }

        protected async Task<bool> RefreshReferenceCacheAsync(long ticks)
        {
            if (ticks == 0)
            {
                return true;
            }

            var directoryToSearch = VirtuosoStorageContext.Current.GetKeyFromParts(ApplicationStore, Constants.REFERENCE_DATA_STORE_FOLDER);
            var allFiles = (await VirtuosoStorageContext.Current.EnumerateDirectories(directoryToSearch));
            var files = allFiles
                .Where(directory =>
                    directory.Name.Equals(String.Format("{0}.{1}", SaveFileWithoutExtension, ticks.ToString()),
                        StringComparison.OrdinalIgnoreCase));

            var _ret = (files.Any() == false);
            return _ret;
        }

        protected async Task ClearCache(bool checkDirectories = false, string subFolder = "")
        {
            // Purge legacy files from isolated storage
            foreach (var file in (await VirtuosoStorageContext.CurrentIsolatedStorage.EnumerateFiles(null))
                     .Where(file => file.Name.StartsWith(CacheName + "Cache", StringComparison.OrdinalIgnoreCase)))
                await VirtuosoStorageContext.CurrentIsolatedStorage.DeleteFile(file.FullName);

            // Purge existing cache from ApplicationStore
            foreach (var file in (await VirtuosoStorageContext.Current.EnumerateFiles(ApplicationStore))
                     .Where(key => key.Name.StartsWith(String.Format("{0}Cache", CacheName))))
                try
                {
                    await VirtuosoStorageContext.Current.DeleteFile(file.FullName);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(
                        String.Format("File delete failed for file: {0}.  Exception: {1}", file, e.ToString()));
                }

            // Purge directory style caches from the root application folder - should really ever see these in production.
            foreach (var directory in (await VirtuosoStorageContext.Current.EnumerateDirectories(ApplicationStore))
                     .Where(d => d.Name.StartsWith(String.Format("{0}Cache", CacheName),
                         StringComparison.OrdinalIgnoreCase)))
                try
                {
                    await VirtuosoStorageContext.Current.DeleteDirectory(directory.FullName);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(
                        String.Format("directory delete failed for file: {0}.  Exception: {1}", directory,
                            e.ToString()));
                }

            if (checkDirectories)
            {
                var sf = ApplicationStore;
                if (string.IsNullOrWhiteSpace(subFolder) == false)
                {
                    sf = VirtuosoStorageContext.Current.GetKeyFromParts(sf, subFolder);
                }

                foreach (var directory in (await VirtuosoStorageContext.Current.EnumerateDirectories(sf))
                         .Where(d => d.Name.StartsWith(String.Format("{0}Cache", CacheName),
                             StringComparison.OrdinalIgnoreCase)))
                    try
                    {
                        await VirtuosoStorageContext.Current.DeleteDirectory(directory.FullName);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(String.Format(
                            "directory delete failed for file: {0}.  Exception: {1}", directory, e.ToString()));
                    }
            }
        }

        private async Task RetryOnIOException(Func<Task> action)
        {
            try
            {
                await action.Invoke();
            }
            catch (System.IO.IOException) //System.IO.IOException: The operation completed successfully
            {
                try
                {
                    await action.Invoke();
                }
                catch
                {
                }
            }
        }

        protected async Task RemovePriorVersion()
        {
            await RemovePriorVersion(string.Format("{0}Cache", CacheName), SaveFileWithoutExtension);
        }

        private async Task RemovePriorVersion(string searchPrefix, string currentCacheName)
        {
            if (string.IsNullOrEmpty(CacheName))
            {
                throw new InvalidOperationException("Must set CacheName before calling RemovePriorVersion()");
            }

            await RetryOnIOException(async () =>
                await PurgeOLDCacheFilesInOOBDirectory(searchPrefix, currentCacheName));

            var directoriesToDelete = new List<string>();

            await RetryOnIOException(async () =>
                await PurgeDirectoryStyleCaches(searchPrefix, currentCacheName, directoriesToDelete));

            await RetryOnIOException(async () =>
                await PurgeSubDirectoryStyleCaches(searchPrefix, currentCacheName, directoriesToDelete));

            if (directoriesToDelete.Any())
            {
                await RetryOnIOException(async () => await DeleteCacheDirectories(directoriesToDelete));
            }
        }

        private static async Task DeleteCacheDirectories(List<string> directoriesToDelete)
        {
            foreach (var dir in directoriesToDelete)
                try
                {
                    await VirtuosoStorageContext.Current.DeleteDirectory(dir);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(
                        String.Format("directory delete failed for file: {0}.  Exception: {1}", dir, e.ToString()));
                }
        }

        private async Task PurgeOLDCacheFilesInOOBDirectory(string searchPrefix, string currentCacheName)
        {
            // Remove OLD cache file(s) in OOB directory location
            var oobFiles = (await VirtuosoStorageContext.Current.EnumerateFiles(ApplicationStore))
                .Where(key => key.Name.StartsWith(searchPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (oobFiles.Any())
            {
                foreach (var file in from f in oobFiles
                         where (System.IO.Path.GetFileNameWithoutExtension(f.Name)).Equals(currentCacheName) == false
                         select f)
                    // FYI: file = "C:\\Users\\{login}\\Documents\\{application name}\\{tenant}\\CodeLookupCache-001.634727131932987599"
                    await VirtuosoStorageContext.Current.DeleteFile(file.FullName);
            }
        }

        private async Task PurgeDirectoryStyleCaches(string searchPrefix, string currentCacheName,
            List<string> directoriesToDelete)
        {
            // Purge directory style caches from the root application folder - should really ever see these in production.
            var dirFiles =
                (await VirtuosoStorageContext.Current
                    .EnumerateDirectories(
                        ApplicationStore)) // "C:\\Users\\user\\AppData\\Local\\Delta Health Technologies\\Crescendo\\localutest"
                .Where(key =>
                    key.Name.StartsWith(searchPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (dirFiles.Any())
            {
                foreach (var directory in from d in dirFiles
                         where (System.IO.Path.GetFileNameWithoutExtension(d.Name)).Equals(currentCacheName) == false
                         select d)
                    try
                    {
                        directoriesToDelete.Add(directory.FullName);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(String.Format(
                            "directory delete failed for file: {0}.  Exception: {1}", directory, e.ToString()));
                    }
            }
        }

        private async Task PurgeSubDirectoryStyleCaches(string searchPrefix, string currentCacheName,
            List<string> directoriesToDelete)
        {
            // Purge sub-directory style caches from the root\Constants.REFERENCE_DATA_STORE_FOLDER folder
            var sf = ApplicationStore;
            var subFolder = Constants.REFERENCE_DATA_STORE_FOLDER;
            if (string.IsNullOrWhiteSpace(subFolder) == false)
            {
                sf = VirtuosoStorageContext.Current.GetKeyFromParts(sf, subFolder);
            }

            // E.G. sf = "C:\\Users\\user\\AppData\\Local\\Delta Health Technologies\\Crescendo\\localutest\\ReferenceData"

            var subDirFiles = (await VirtuosoStorageContext.Current.EnumerateDirectories(sf))
                .Where(dir =>
                    dir.Name.StartsWith(string.Format("{0}Cache", CacheName), StringComparison.OrdinalIgnoreCase))
                .ToList(); //String.Format("{0}Cache*", CacheName));

            if (subDirFiles.Any())
            {
                // E.G. subDirFiles[1] - "C:\\Users\\user\\AppData\\Local\\Delta Health Technologies\\Crescendo\\localutest\\ReferenceData\\CodeLookupCache-006.635875328898930000"
                foreach (var directory in from d in subDirFiles
                         where (System.IO.Path.GetFileNameWithoutExtension(d.Name)).Equals(currentCacheName) ==
                               false // Does GetFileNameWithoutExtension work for directories?
                         select d)
                    try
                    {
                        directoriesToDelete.Add(directory.FullName);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(String.Format(
                            "directory delete failed for file: {0}.  Exception: {1}", directory, e.ToString()));
                    }
            }
        }

        public ICacheManagement GetManagementInterface()
        {
            var mgmt = this as ICacheManagement;
            if (mgmt != null)
            {
                return mgmt;
            }

            return null;
        }

        protected async Task<bool> CacheExists()
        {
            var folder_name = string.Empty;
            // Do we have a folder name prefixed with SaveFileWithoutExtension or are any of those folders empty
            foreach (var directory in (await VirtuosoStorageContext.Current.EnumerateDirectories(
                         VirtuosoStorageContext.Current.GetKeyFromParts(ApplicationStore,
                             Constants.REFERENCE_DATA_STORE_FOLDER)))
                    )
            {
                var _directory_name_only_no_path = System.IO.Path.GetFileName(directory.Name);
                if (_directory_name_only_no_path.StartsWith(SaveFileWithoutExtension))
                {
                    folder_name = directory.FullName;
                }
            }

            if (string.IsNullOrWhiteSpace(folder_name) == false)
            {
                // Check if folder is empty
                var have_files = (await VirtuosoStorageContext.Current.EnumerateFiles(folder_name)).Any()
                    ? true
                    : false;
                return have_files;
            }

            return false;
        }
    }

    public abstract class ReferenceDataCacheBase : ICache
    {
        protected EntityManager EntityManager => EntityManager.Current;

        protected StorageStatsImpl CacheInfo;
        protected string StoreFolder;
        public string Version { get; internal set; }
        public string CacheName { get; internal set; }

        protected string SaveFileWithoutExtension { get; private set; }
        protected string ApplicationStore { get; private set; }
        protected bool isLoading = false;
        protected bool CreateFile;
        protected DateTime? LastUpdatedDate;
        public VirtuosoDomainContext Context { get; set; }
        protected long Ticks;

        private int _TotalRecords;

        protected int TotalRecords
        {
            get { return _TotalRecords; }
            set { _TotalRecords = value; }
        }

        private readonly ILogger _logManager;

        public ReferenceDataCacheBase(ILogger logManager, string cacheName, string version)
        {
            _logManager = logManager;

            Version = version;
            CacheName = cacheName;

            SaveFileWithoutExtension = string.Format("{0}Cache-{1}", CacheName, Version);

            ApplicationStore = ApplicationStoreInfo.GetUserStoreForApplication();

            //C:\Users\<user>\AppData\Local\Delta Health Technologies\Crescendo\local01\ReferenceData
            //C:\Users\<user>\AppData\Local\Delta Health Technologies\Crescendo\local01\ReferenceData\PhysicianCache-005
#if OPENSILVER
            StoreFolder = VirtuosoStorageContext.Current.GetKeyFromParts(Constants.REFERENCE_DATA_STORE_FOLDER, SaveFileWithoutExtension);
#else
            StoreFolder = VirtuosoStorageContext.Current.GetKeyFromParts(ApplicationStore,
                Constants.REFERENCE_DATA_STORE_FOLDER, SaveFileWithoutExtension);
#endif

            CacheInfo = new StorageStatsImpl(ApplicationStoreInfo.GetUserStoreForApplication(StoreFolder),
                ".config"); //NOTE: renaming the configuration file, so that it is not processed along with other .dat files, e.g. those containing serialized EntityStateSets
            Ticks = 0;
            Context = new VirtuosoDomainContext();
        }

        public void Reload(Action callback)
        {
            Load(LastUpdatedDate, EntityManager.IsOnline, callback, true)
                .ContinueWith(task => { }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public void Reload(EntityChangeSet changeSet, Action callback)
        {
            AsyncUtility.Run(() => Update(LastUpdatedDate, changeSet, callback));
        }

        public abstract Task Load(DateTime? lastUpdatedDate, bool isOnline, Action callback, bool force = false);

        public virtual Task Update(DateTime? lastUpdatedDate, EntityChangeSet changeSet, Action callback)
        {
            return AsyncUtility.TaskFromResult();
        }

        protected void Log(TraceEventType level, string message, Exception exception = null)
        {
            string category = string.Format("{0}-CACHE", CacheName);

            if (exception != null)
            {
                _logManager.Log(level, category, message, exception);
            }
            else
            {
                _logManager.Log(level, category, message);
            }
        }

        protected async Task PurgeAndSave()
        {
            await ClearCache();
            var cacheFolder = VirtuosoStorageContext.Current.GetKeyFromParts(ApplicationStore, Constants.REFERENCE_DATA_STORE_FOLDER, SaveFileWithoutExtension);
            var cache = await RIACacheManager.Initialize(cacheFolder, Constants.ENTITY_TYPENAME_FORMAT);
            await cache.Save(Context, true);
        }

        protected async Task<bool> ShouldRefreshCache(DateTime? lastUpdatedDate)
        {
            long server_ticks = (lastUpdatedDate.HasValue) ? lastUpdatedDate.Value.Ticks : 0;
            bool ret = true;

            var stats = await CacheInfo.GetStats(CacheName);

            if (
                (stats.CacheLoadCompleted == false) || // previous download failed to complete
                (stats.Anchor.Ticks < server_ticks) ||
                // (stats.TotalRecords == 0) ||  // FYI: when defer load - TotalRecords may be zero, even though have data on disk
                ((await DataFilesExist()) == false) ||
                (server_ticks == 0)
                // data could have been cleared on server - so this will purge and re-fetch data to client
            )
            {
                stats.LastUpdatedDate = lastUpdatedDate;
                stats.Ticks = server_ticks;
                stats.CacheLoadCompleted = false;

                await CacheInfo.SaveStats(stats);

                ret = true; // Don't have state stored - so update cache
            }
            else
            {
                ret = false;
            }

            return ret;
        }

        async Task<bool> DataFilesExist()
        {
            string subFolder = Constants.REFERENCE_DATA_STORE_FOLDER;
            var sf = ApplicationStore;
            if (string.IsNullOrWhiteSpace(subFolder) == false)
            {
                sf = VirtuosoStorageContext.Current.GetKeyFromParts(sf, subFolder);
            }

            var cacheLocation =
                VirtuosoStorageContext.Current.GetKeyFromParts(sf,
                    SaveFileWithoutExtension); // e.g. C:\Users\<user>\AppData\Local\Delta Health Technologies\Crescendo\local01\ReferenceData\PhysicianCache-005
            var c = (await VirtuosoStorageContext.Current.EnumerateFiles(cacheLocation))
                .Where(key => key.FullName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                .Any();
            return c;
        }

        protected async Task<DateTime> GetAnchor(bool ignoreCacheLoadCompleted = true)
        {
#if OPENSILVER
            // This method is only called when updating the cache.
            // With OS, we don't have a way to do incremental updates, so always pull the full cache
            return DateTime.MinValue;
#endif

            var stats = await CacheInfo.GetStats(CacheName);

            if (ignoreCacheLoadCompleted && stats.TotalRecords > 0)
            {
                if (stats.Anchor.Equals(DateTime.MinValue) == false)
                {
                    return stats.Anchor;
                }
            }

            if (stats.TotalRecords == 0)
            {
                return DateTime.MinValue;
            }

            return (stats.CacheLoadCompleted) ? stats.Anchor : DateTime.MinValue;
        }

        protected async Task ClearCache()
        {
            string[] extensions = { ".dat", ".dat.idx" };
            var cacheFolder = VirtuosoStorageContext.Current.GetKeyFromParts(ApplicationStore,
                Constants.REFERENCE_DATA_STORE_FOLDER, SaveFileWithoutExtension);
            foreach (var file in (await VirtuosoStorageContext.Current.EnumerateFiles(cacheFolder)).Where(f =>
                         extensions.Any(ext => f.Path.EndsWith(ext))))
                try
                {
                    await VirtuosoStorageContext.Current.DeleteFile(file.FullName);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(
                        String.Format("file delete failed for file: {0}.  Exception: {1}", file, e.ToString()));
                }
        }

        private async Task RetryOnIOException(Func<Task> action)
        {
            try
            {
                await action.Invoke();
            }
            catch (System.IO.IOException) // System.IO.IOException: The operation completed successfully
            {
                try
                {
                    await action.Invoke();
                }
                catch
                {
                }
            }
        }

        protected async Task RemovePriorVersion()
        {
            await RemovePriorVersion(string.Format("{0}Cache*", CacheName), SaveFileWithoutExtension);
        }

        private async Task RemovePriorVersion(string searchPattern, string currentCacheName)
        {
            if (string.IsNullOrEmpty(CacheName))
            {
                throw new InvalidOperationException("Must set CacheName before calling RemovePriorVersion()");
            }

            await RetryOnIOException(
                async () => await PurgeOLDCacheFilesInOOBDirectory(searchPattern, currentCacheName));

            var directoriesToDelete = new List<string>();

            await RetryOnIOException(async () =>
                await PurgeDirectoryStyleCaches(searchPattern, currentCacheName, directoriesToDelete));

            await RetryOnIOException(async () =>
                await PurgeSubDirectoryStyleCaches(searchPattern, currentCacheName, directoriesToDelete));

            if (directoriesToDelete.Any())
            {
                await RetryOnIOException(async () => await DeleteCacheDirectories(directoriesToDelete));
            }
        }

        private static async Task DeleteCacheDirectories(List<string> directoriesToDelete)
        {
            foreach (var dir in directoriesToDelete)
                try
                {
                    await VirtuosoStorageContext.Current.DeleteDirectory(dir);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(
                        String.Format("directory delete failed for file: {0}.  Exception: {1}", dir, e.ToString()));
                }
        }

        private async Task PurgeSubDirectoryStyleCaches(string searchPrefix, string currentCacheName,
            List<string> directoriesToDelete)
        {
            // Purge sub-directory style caches from the root\Constants.REFERENCE_DATA_STORE_FOLDER folder
            var sf = ApplicationStore;
            var subFolder = Constants.REFERENCE_DATA_STORE_FOLDER;
            if (string.IsNullOrWhiteSpace(subFolder) == false)
            {
                sf = VirtuosoStorageContext.Current.GetKeyFromParts(sf, subFolder);
            }

            var subDirFiles = (await VirtuosoStorageContext.Current.EnumerateDirectories(sf))
                .Where(dir => dir.Name.StartsWith(searchPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var directory in from d in subDirFiles
                     where (System.IO.Path.GetFileName(d.Name)).Equals(currentCacheName) == false
                     select d)
                try
                {
                    directoriesToDelete.Add(directory.FullName);
                    //Directory.Delete(directory, true);
                    //if (Directory.Exists(directory))
                    //    System.Diagnostics.Debug.WriteLine(String.Format("directory {0} was not deleted.", directory));
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(
                        String.Format("directory delete failed for file: {0}.  Exception: {1}", directory,
                            e.ToString()));
                }
        }

        private async Task PurgeDirectoryStyleCaches(string searchPrefix, string currentCacheName,
            List<string> directoriesToDelete)
        {
            // Purge directory style caches from the root application folder - should really ever see these in production.
            var dirFiles = (await VirtuosoStorageContext.Current.EnumerateDirectories(ApplicationStore))
                .Where(dir => dir.Name.StartsWith(searchPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var directory in from d in dirFiles
                     where
                         (System.IO.Path.GetFileNameWithoutExtension(d.Name)).Equals(currentCacheName) == false ||
                         d.Equals(currentCacheName) == false
                     select d)
                try
                {
                    directoriesToDelete.Add(directory.FullName);
                    //Directory.Delete(directory, true);
                    //if (Directory.Exists(directory))
                    //    System.Diagnostics.Debug.WriteLine(String.Format("directory {0} was not deleted.", directory));
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(
                        String.Format("directory delete failed for file: {0}.  Exception: {1}", directory,
                            e.ToString()));
                }
        }

        private async Task PurgeOLDCacheFilesInOOBDirectory(string keyPrefix, string currentCacheName)
        {
            // Remove OLD cache file(s) in OOB directory location
            var oobFiles =
                (await VirtuosoStorageContext.Current
                    .EnumerateFiles(ApplicationStore)) // FYI: only works for TrustedApplications
                .Where(key => key.Name.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in from f in oobFiles
                     where (System.IO.Path.GetFileNameWithoutExtension(f.Name)).Equals(currentCacheName) == false ||
                           f.Equals(currentCacheName) == false
                     select f)
                // FYI: file = "C:\\Users\\{login}\\Documents\\{application name}\\{tenant}\\CodeLookupCache-001.634727131932987599"
                await VirtuosoStorageContext.Current.DeleteFile(file.FullName);
        }

        public ICacheManagement GetManagementInterface()
        {
            var mgmt = this as ICacheManagement;
            if (mgmt != null)
            {
                return mgmt;
            }

            return null;
        }
    }
}

namespace Virtuoso.Core.Cache
{
    public interface ICacheV2<T> where T : Entity
    {
        Task Load(bool isOline, Action<ServiceLoadResult<T>> callback);
        int TotalRecords { get; set; }
    }

    public abstract class CacheBaseV2<T> : ICacheV2<T> where T : Entity
    {
        public EntityManager EntityManager => EntityManager.Current;

        public string Version { get; internal set; }
        public string SaveFileWithoutExtension { get; internal set; }
        public string CacheName { get; internal set; }
        public string ApplicationStore { get; internal set; }
        protected bool isLoading = false;
        protected long Ticks;

        private int _TotalRecords;

        public int TotalRecords
        {
            get { return _TotalRecords; }
            set
            {
                _TotalRecords = value;
                NotifyPropertyChanged("TotalRecords");
            }
        }

        private readonly ILogger _logManager;

        public CacheBaseV2(ILogger logManager)
        {
            _logManager = logManager;

            ApplicationStore = ApplicationStoreInfo.GetUserStoreForApplication();
        }

        public CacheBaseV2(ILogger logManager, string cacheName, string version)
        {
            _logManager = logManager;

            Version = version;
            CacheName = cacheName;

            SaveFileWithoutExtension = string.Format("{0}Cache-{1}", CacheName, Version);

            ApplicationStore = ApplicationStoreInfo.GetUserStoreForApplication();
        }

        public async Task Reload(Action<ServiceLoadResult<T>> callback)
        {
            await Load(EntityManager.IsOnline, callback);
        }

        public abstract Task Load(bool isOnline, Action<ServiceLoadResult<T>> callback);

        protected async Task<IEnumerable<string>> GetCurrentCacheFileName()
        {
            return (await VirtuosoStorageContext.Current.EnumerateFiles(ApplicationStore))
                .Where(key => key.Name.StartsWith(CacheName + "Cache", StringComparison.OrdinalIgnoreCase))
                .Select(key => key.FullName);
        }

        protected void Log(TraceEventType level, string category, string message, Exception exception)
        {
            if (exception != null)
            {
                _logManager.Log(level, category, message, exception);
            }
            else
            {
                _logManager.Log(level, category, message);
            }
        }

        protected async Task<string> PurgeAndGetCacheFileName()
        {
            // Purge legacy files from isolated storage
            foreach (var file in (await VirtuosoStorageContext.CurrentIsolatedStorage.EnumerateFiles(null))
                     .Where(file => file.Name.StartsWith(CacheName + "Cache", StringComparison.OrdinalIgnoreCase)))
                await VirtuosoStorageContext.CurrentIsolatedStorage.DeleteFile(file.FullName);

            // Purge existing cache from ApplicationStore
            foreach (var file in (await VirtuosoStorageContext.Current.EnumerateFiles(ApplicationStore))
                     .Where(key =>
                         key.Name.StartsWith(String.Format("{0}Cache", CacheName), StringComparison.OrdinalIgnoreCase)))
                try
                {
                    await VirtuosoStorageContext.Current.DeleteFile(file.FullName);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(
                        String.Format("File delete failed for file: {0}.  Exception: {1}", file, e.ToString()));
                }

            // Create Save file name
            string cacheFile = System.IO.Path.Combine(
                ApplicationStore,
                String.Format("{0}.{1}", SaveFileWithoutExtension, Ticks.ToString()));

            return cacheFile;
        }

        public async Task RemovePriorVersion()
        {
            await RemovePriorVersion(string.Format("{0}Cache", CacheName), SaveFileWithoutExtension);
        }

        private async Task RemovePriorVersion(string searchPattern, string currentCacheName)
        {
            if (string.IsNullOrEmpty(CacheName))
            {
                throw new InvalidOperationException("Must set CacheName before calling RemovePriorVersion()");
            }

            await PurgeOLDCacheFilesInOOBDirectory(searchPattern, currentCacheName);
        }

        private async Task PurgeOLDCacheFilesInOOBDirectory(string searchPrefix, string currentCacheName)
        {
            // Remove OLD cache file(s) in OOB directory location
            var oobFiles =
                (await VirtuosoStorageContext.Current
                    .EnumerateFiles(ApplicationStore)) // FYI: only works for TrustedApplications
                .Where(key => key.Name.StartsWith(searchPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in from f in oobFiles
                     where (System.IO.Path.GetFileNameWithoutExtension(f.Name)).Equals(currentCacheName) == false
                     select f)
                // FYI: file = "C:\\Users\\{login}\\Documents\\{application name}\\{tenant}\\CodeLookupCache-001.634727131932987599"
                await VirtuosoStorageContext.Current.DeleteFile(file.FullName);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                if (Deployment.Current.Dispatcher.CheckAccess())
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
                else
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    });
                }
            }
        }
    }
}