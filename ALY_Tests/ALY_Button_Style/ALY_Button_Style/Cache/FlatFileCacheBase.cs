#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Infrastructure.Storage;
using Virtuoso.Client.Utils;
using Virtuoso.Core;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Portable.Database;
using Virtuoso.Client.Core;
using Virtuoso.Core.Helpers;

#endregion

namespace Virtuoso.Client.Cache
{
    public abstract class FlatFileCacheBase<T> : CacheBase, ICacheManagement
    {
        public IDatabaseWrapper DatabaseWrapper { get; set; }
        protected readonly List<T> _DataList;
        public IQueryable<T> Data => _DataList.AsQueryable();
        protected bool DeferLoad { get; set; }
        protected int LoadedRecordNumber;
        private int _RunningTotal;

        public int RunningTotal
        {
            get { return _RunningTotal; }
            set { _RunningTotal = value; }
        }

        public T NewObject()
        {
            T instance = Activator.CreateInstance<T>();
            return instance;
        }

        private bool IsGEMS
        {
            get
            {
                var instance = NewObject();
                Virtuoso.Portable.Model.CachedICDGEMS ci = instance as Virtuoso.Portable.Model.CachedICDGEMS;
                return (ci == null) ? false : true;
            }
        }

        protected FlatFileCacheBase(ILogger logManager)
            : base(logManager)
        {
            try
            {
                _DataList = new List<T>();
                DeferLoad = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw;
            }
        }

        protected FlatFileCacheBase(ILogger logManager, bool deferLoad)
            : base(logManager)
        {
            try
            {
                _DataList = new List<T>();
                DeferLoad = deferLoad;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public override async Task Load(DateTime? lastUpdatedDate, bool isOnline, Action callback, bool force = false)
        {
            if (IsGEMS)
            {
                Log(TraceEventType.Information, "Bypassing ICD GEMS Cache load " + CacheName);
                callback?.Invoke();
                return;
            }

            Log(TraceEventType.Start, "BEGIN Load()");

            LastUpdatedDate = lastUpdatedDate;
            Ticks = LastUpdatedDate?.Ticks ?? 0;

            TotalRecords = 0;

            try
            {
                await DatabaseWrapper.Start();
                DatabaseWrapper.Initialize();
            }
            catch (Exception e)
            {
                Log(TraceEventType.Critical, "Error opening database", e);
                throw;
            }

            if (isLoading)
            {
                Log(TraceEventType.Information, "Still loading cache " + CacheName);
                callback?.Invoke();
                return;
            }

            isLoading = true;

            if ((isOnline && Ticks > 0)
                || (Ticks == 0 && isOnline &&
                    (await CacheExists()) ==
                    false)) // Ticks = 0, but online, got LastUpdatedDdate = NULL from GetReferenceDataInfo, still need to query server for data to build cache
            {
                if (await ShouldRefreshCache(LastUpdatedDate)) //RefreshCache will handle a NULL LastUpdatedDate
                {
                    Log(TraceEventType.Verbose, "Rebuilding " + CacheName + " cache from server");
                    isLoading = true;
                    DateTime client_anchor = await GetAnchor();
                    await CreateFlatFileFromContext(DateTime.UtcNow, client_anchor, callback);
                }
                else
                {
                    Log(TraceEventType.Verbose, "Loading " + CacheName + " cache from disk.  Network is available");
                    isLoading = true;
                    LoadFromDisk(callback);
                }
            }
            else
            {
                Log(TraceEventType.Verbose, "Loading " + CacheName + " cache from disk.  Network is not available");
                isLoading = true;
                LoadFromDisk(callback);
            }

            Log(TraceEventType.Stop, "End Load()");
        }

        public async Task EnsureDataLoadedFromDisk()
        {
            if ((DeferLoad) || (_DataList.Any() == false)) // Most likely DeferLoad == true
            {
                TotalRecords = 0;
                await LoadData();
                var stats = await DatabaseWrapper.Storage.GetStats(CacheName);
                stats.TotalRecords = TotalRecords;
                await DatabaseWrapper.Storage.SaveStats(stats);
                DeferLoad = false;
            }
        }

        protected async Task<bool> ShouldRefreshCache(DateTime? lastUpdatedDate)
        {
            long server_ticks = (lastUpdatedDate.HasValue) ? lastUpdatedDate.Value.Ticks : 0;
            bool ret = true;

            var stats = await DatabaseWrapper.Storage.GetStats(CacheName);

            if (
                (stats.CacheLoadCompleted == false) || // NOTE: previous download failed to complete
                (stats.Anchor.Ticks < server_ticks) ||

                //(stats.TotalRecords == 0) ||  //FYI: when defer load - TotalRecords may be zero, even though have data on disk
                (await VirtuosoStorageContext.Current.Exists(DatabaseWrapper.Storage.DataSetPath()) == false) ||
                (server_ticks == 0)
            // NOTE: data could have been cleared on server - so this will purge and re-fetch data to client
            )
            {
                stats.LastUpdatedDate = lastUpdatedDate;
                stats.Ticks = server_ticks;
                stats.TotalRecords =
                    0; // NOTE: for this cache, we want to reset the record count, because if the server date changes, we want to refetch all records
                stats.CacheLoadCompleted = false;

                await DatabaseWrapper.Storage.SaveStats(stats);

                ret = true; // don't have state stored - so update cache
            }
            else
            {
                ret = false;
            }

            return ret;
        }

        protected async Task<DateTime> GetAnchor()
        {
            var stats = await DatabaseWrapper.Storage.GetStats(CacheName);
            if (stats.TotalRecords == 0)
            {
                return DateTime.MinValue;
            }

            return (stats.CacheLoadCompleted) ? stats.Anchor : DateTime.MinValue;
        }

        protected async Task CreateFlatFileFromContext(DateTime new_anchor, DateTime client_anchor, Action callback)
        {
            try
            {
                await PurgeAndSave();

                await DownloadFileAsync(new_anchor, callback);
            }
            catch (Exception e)
            {
                Log(TraceEventType.Critical, "Error creating cache", e);

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    isLoading = false;
                    callback?.Invoke();
                });
            }
        }

        public virtual Task DownloadAdditionalFilesAsync(DateTime new_anchor)
        {
            var taskCompletionSource = new TaskCompletionSource<Boolean>();
            taskCompletionSource.TrySetResult(true);
            return taskCompletionSource.Task;
        }

        async Task DownloadFileAsync(DateTime new_anchor, Action callback)
        {
            try
            {
#if OPENSILVER
                var response = await ApiHelper.MakeApiCall($"ReferenceCache/{CacheName}", query: "?format=bin");
#else
                var response = await ApiHelper.MakeApiCall($"ReferenceCache/{CacheName}");
#endif

                var fileName = DatabaseWrapper.Storage.DataSetPath();
                var bytes = await response.Content.ReadAsByteArrayAsync();
                await VirtuosoStorageContext.Current.WriteToFile(fileName, bytes);

                TotalRecords = 0;

                await DownloadAdditionalFilesAsync(new_anchor);

                if (DeferLoad == false)
                {
                    await LoadData();
                }

                await DatabaseWrapper.Storage.SaveStats(CacheName, new_anchor, LastUpdatedDate, true,
                    TotalRecords); // Note: if DeferLoad then TotalRecords = 0, even though may have thousands on disk...

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    isLoading = false;
                    callback?.Invoke();
                    Messenger.Default.Send(CacheName, "CacheLoaded");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);

                // Downloading file failed - attempt to load from disk
                isLoading = true;
                _DataList.Clear();
                LoadFromDisk(callback);
            }
        }

        protected void LoadFromDisk(Action callback)
        {
            TotalRecords = RunningTotal = 0;
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    _DataList.Clear();

                    if (DeferLoad == false)
                    {
                        await LoadData();
                    }

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        isLoading = false;
                        TotalRecords = RunningTotal;
                        if (DeferLoad == false)
                        {
                            System.Diagnostics.Debug.WriteLine(string.Format(
                                "Total number of records in " + CacheName + " database from disk.  Total: {0}",
                                TotalRecords));
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("DeferLoad is TRUE.  " + CacheName +
                                                               " data will be loaded on first SEARCH.");
                        }

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

        public virtual Task LoadRelatedData()
        {
            return AsyncUtility.TaskFromResult();
        }

        public virtual void LoadDataComplete()
        {
        }
        protected async Task LoadData()
        {
            try
            {
                LoadedRecordNumber = 0;
                await LoadRelatedData(); // First, load other/related data from disk into the cache's private VirtuosoDomainContext.
                                         // Currently only AllergyCache manages data in addition to the core server side cache data.
#if OPENSILVER
                var fileData = await DatabaseWrapper.Storage.LoadFile();
                var stream = new System.IO.MemoryStream(fileData);
                var entities = ProtoBuf.Serializer.Deserialize<T[]>(stream);
                foreach (var entity in entities)
                {
                    LoadedRecordNumber++;
                    OnEntityDeserialized(entity);
                    RunningTotal++;
                    _DataList.Add(entity);
                }
#else
                await DatabaseWrapper.Storage.Load(DeserializeDataInternal); // Second, load the core server side cache from disk into memory.
#endif
                LoadDataComplete();
                TotalRecords = LoadedRecordNumber;
            }
            catch (Exception err)
            {
                Log(TraceEventType.Error, "Loading " + CacheName + " cache from disk failed.", err);
            }
        }

        protected virtual void OnEntityDeserialized(T entity) { }

#if !OPENSILVER
        protected abstract void DeserializeData(RecordSet recordSet);

        void DeserializeDataInternal(RecordSet recordSet, string inputData)
        {
            LoadedRecordNumber++;
            try
            {
                DeserializeData(recordSet);
                RunningTotal++;
            }
            catch (Exception err)
            {
                Log(TraceEventType.Error,
                    string.Format("Failed to parse. Record No: {0}, Data: {1}", LoadedRecordNumber, inputData), err);

                foreach (var r in recordSet.Fields)
                    System.Diagnostics.Debug.WriteLine(string.Format("field: {0}", r.Value));
            }
        }  
#endif

        protected async Task PurgeAndSave()
        {
            var file = DatabaseWrapper.Storage.DataSetPath();
            try
            {
                await VirtuosoStorageContext.Current.DeleteFile(file);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("File delete failed for file: {0}.  Exception: {1}",
                    file, e.ToString()));
            }
        }

        #region ICacheManagement

        void ICacheManagement.Initialize(bool deferLoad)
        {
            DeferLoad = deferLoad;

            isLoading = false;
            TotalRecords = 0;

            if (DatabaseWrapper.Storage == null)
            {
                try
                {
                    DatabaseWrapper.Initialize();
                }
                catch (Exception e)
                {
                    Log(TraceEventType.Critical, "Error opening database", e);
                    throw;
                }
            }
        }

        async Task ICacheManagement.LoadFromDisk()
        {
            TotalRecords = RunningTotal = 0;
            _DataList.Clear();

            if (DeferLoad == false)
            {
                await LoadData();
            }

            TotalRecords = RunningTotal;
        }

        #endregion ICacheManagement
    }
}