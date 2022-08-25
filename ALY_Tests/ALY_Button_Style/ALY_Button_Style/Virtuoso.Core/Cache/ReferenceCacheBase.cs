#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows;
using System.Windows.Ria.Sync;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Core;
using Virtuoso.Client.Infrastructure;
using Virtuoso.Client.Utils;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Cache
{
    public abstract class ReferenceCacheBase : CacheBase
    {
        public VirtuosoDomainContext Context;
        protected RIACacheManager CacheStore = null;

        protected bool _isReady;

        protected ReferenceCacheBase(ILogger logManager, string cacheName, string version) : base(logManager, cacheName,
            version)
        {
            Ticks = 0;
            Context = new VirtuosoDomainContext();
        }

        protected async System.Threading.Tasks.Task PurgeAndSave()
        {
            await ClearCache(true, Constants.REFERENCE_DATA_STORE_FOLDER);

            var cacheFile = Path.Combine(
                ApplicationStore,
                Constants.REFERENCE_DATA_STORE_FOLDER,
                String.Format("{0}.{1}", SaveFileWithoutExtension, Ticks.ToString()));

            var cache = await RIACacheManager.Initialize(cacheFile, Constants.ENTITY_TYPENAME_FORMAT);
            await cache.Save(Context);
        }

        protected virtual void OnRIACacheLoaded() { }

        protected virtual void OnCacheNotFoundException() { }

        protected void EnsureCacheReady()
        {
            if (TotalRecords == 0 && !_isReady)
            {
                CacheStore.EnsureLoadedIntoContext(Context);
                OnRIACacheLoaded();
                _isReady = true;
            }
        }

        protected virtual void LoadFromDisk(Action callback)
        {
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    if (CacheStore == null)
                    {
                        CacheStore = await RIACacheManager.Initialize(
                                Path.Combine(ApplicationStore, Constants.REFERENCE_DATA_STORE_FOLDER), CacheName,
                                Constants.ENTITY_TYPENAME_FORMAT, true); //NOTE: can throw DirectoryNotFoundException
                    }
                    CacheStore.DeferContextImport = true;
                    await CacheStore.Load(Context);

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        isLoading = false;
                        callback?.Invoke();
                    });
                }
                catch (CacheNotFoundException)
                {
                    //doesn't mean there is a problem necessarily, probably means that there was no data returned from the server, so nothing was saved to disk - e.g. no files
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        isLoading = false;
                        OnCacheNotFoundException();
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
    }

    public abstract class ReferenceCacheBase<T> : ReferenceCacheBase where T : Entity
    {
        protected ReferenceCacheBase(ILogger logManager, string cacheName, string version) : base(logManager, cacheName,
            version)
        {
        }

        protected bool RequireCacheRecords { get; set; }
        protected abstract EntitySet EntitySet { get; }
        protected abstract EntityQuery<T> GetEntityQuery();

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
                    var query = GetEntityQuery();
                    Context.Load(query, OnLoaded, callback);
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

        private async System.Threading.Tasks.Task OnLoaded(Action callback, Exception error)
        {
            if (error != null)
            {
                Log(TraceEventType.Error, $"There was an error loading the {CacheName} cache.", error);

                LoadFromDisk(callback);
            }
            else
            {
                // NOTE: In Silverlight, PurgeAndSave() will create a folder on disk for the cache, regardless of whether
                //       that cache had records or not.  Some caches legitimately have no data server side.
                //       In these cases you will have a folder on disk with no .dat files.
                await PurgeAndSave();

                OnCacheSaved();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    TotalRecords = EntitySet.Count;

                    if (TotalRecords == 0)
                    {
                        // This cache is `Ready` with ZERO records, Context will be empty.  
                        // Set _isReady to short circuit EnsureCacheReady().
                        // Alternatively, in the future, we might create an empty file in IndexedDB for OpenSilver.
                        _isReady = true;
                    }

                    isLoading = false;
                    if (callback != null)
                    {
                        callback();
                    }

                    Messenger.Default.Send(CacheName, "CacheLoaded");
                });
            }
        }

        private async void OnLoaded(LoadOperation<T> operation)
        {
            Action callback = null;
            if (operation.UserState != null)
                callback = ((Action)operation.UserState);

            if (operation.HasError)
                operation.MarkErrorAsHandled();

            await OnLoaded(callback, operation.Error);
        }

        protected virtual void OnCacheSaved()
        {
        }

        protected override void OnCacheNotFoundException()
        {
            TotalRecords = EntitySet.Count;
            Log(TraceEventType.Information,
                string.Format(
                    "{0} Cache.  Directory not found.  Probably no data returned from server, so no data saved to disk.",
                    CacheName));
        }

        protected override void OnRIACacheLoaded()
        {
            TotalRecords = EntitySet.Count;

            if (TotalRecords <= 0 && RequireCacheRecords)
            {
                Log(TraceEventType.Error, $"No records loaded from {CacheName} cache.");
            }
        }
    }
}