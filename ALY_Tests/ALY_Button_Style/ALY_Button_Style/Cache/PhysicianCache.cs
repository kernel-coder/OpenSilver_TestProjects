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
using Virtuoso.Client.Infrastructure;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.Physician)]
    [Export(typeof(ICache))]
    public class PhysicianCache : ReferenceDataCacheBase
    {
        private RIACacheManager _cacheStore;
        private bool _isReady;

        public static PhysicianCache Current { get; private set; }

        [ImportingConstructor]
        public PhysicianCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Physician, "011")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("PhysicianCache already initialized.");
            }

            Current = this;
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

            if ((isOnline) && ((Ticks > 0)))
            {
                if (await ShouldRefreshCache(LastUpdatedDate))
                {
                    Context.EntityContainer.Clear();
                    var client_anchor = await GetAnchor();
                    UpdateLocalCacheFromDatabase(DateTime.UtcNow, client_anchor, callback);
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

        public void EnsureCacheReady()
        {
            if(TotalRecords == 0 && !_isReady)
            {
                _cacheStore.EnsureLoadedIntoContext(Context);
                OnRIACacheLoaded();
                _isReady = true;
            }
        }

        void UpdateLocalCacheFromDatabase(DateTime new_anchor, DateTime client_anchor, Action callback)
        {
            var query = Context.GetPhysicianQuery(client_anchor);
            Context.Load(query, LoadBehavior.RefreshCurrent, OnLoaded,
                new { ClientAnchor = client_anchor, NewAnchor = new_anchor, Callback = callback });
        }

        private void OnRIACacheLoaded()
        {
            TotalRecords = Context.Physicians.Count;
        }

        private void LoadFromDisk(Action callback)
        {
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    if(_cacheStore == null)
                    {
                        _cacheStore = await RIACacheManager.Initialize(
                            Path.Combine(ApplicationStore, Constants.REFERENCE_DATA_STORE_FOLDER), CacheName,
                            Constants.ENTITY_TYPENAME_FORMAT, true); //NOTE: can throw DirectoryNotFoundException
                    }

                    _cacheStore.DeferContextImport = true;
                    await _cacheStore.Load(Context);

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
                        TotalRecords = Context.Physicians.Count();
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

      /// <summary>
      /// Used with RIA services
      /// </summary>
      /// <param name="operation"></param>
      private async void OnLoaded(LoadOperation<Physician> operation)
      {
         DateTime? client_anchor = null;
         DateTime? new_anchor = null;
         Action callback = null;
         if(operation.UserState != null)
         {
            dynamic obj = operation.UserState;
            client_anchor = obj.ClientAnchor;
            new_anchor = obj.NewAnchor;
            callback = obj.Callback;
         }

         if(operation.HasError)
         {
            operation.MarkErrorAsHandled();
         }

         await OnLoaded(callback, new_anchor, client_anchor, operation.Error, operation.AllEntities);
      }

      private async System.Threading.Tasks.Task OnLoaded(Action callback, DateTime? new_anchor, DateTime? client_anchor, Exception ex, IEnumerable<Entity> incrementalEntities = null)
        {
            if (ex != null)
            {
                var stats = await CacheInfo.GetStats(CacheName);
                stats.CacheLoadCompleted = false; // An error occurred, force cache refresh on next start
                await CacheInfo.SaveStats(stats);

                LoadFromDisk(callback);
            }
            else
            {
#if OPENSILVER
                if (true)
#else
                if (DateTime.MinValue.Equals((client_anchor)))
#endif
                {
                    await PurgeAndSave();

                    Deployment.Current.Dispatcher.BeginInvoke(async () =>
                    {
                        TotalRecords = Context.Physicians.Count;
                        isLoading = false;
                        if (new_anchor != null)
                        {
                            await CacheInfo.SaveStats(CacheName, new_anchor.Value, LastUpdatedDate, true,
                                TotalRecords);
                            callback?.Invoke();
                        }

                        Messenger.Default.Send(CacheName, "CacheLoaded");
                    });
                }
#if !OPENSILVER
                else
                {
                    if (incrementalEntities.Any())
                    {
                        await IncrementalSave(incrementalEntities);

                        Deployment.Current.Dispatcher.BeginInvoke(async () =>
                        {
                            TotalRecords = Context.Physicians.Count;
                            isLoading = false;
                            if (new_anchor != null)
                            {
                                await CacheInfo.SaveStats(CacheName, new_anchor.Value, LastUpdatedDate, true,
                                    TotalRecords);
                                callback?.Invoke();
                            }

                            Messenger.Default.Send(CacheName, "CacheLoaded");
                        });
                    }
                    else
                    {
                        LoadFromDisk(callback);
                    }
                }
#endif
            }
        }

#if !OPENSILVER
        private async System.Threading.Tasks.Task IncrementalSave(IEnumerable<Entity> updates)
        {
            // Update .dat files on disk
            var cache = await RIACacheManager.Initialize(
                Path.Combine(ApplicationStore, Constants.REFERENCE_DATA_STORE_FOLDER), CacheName,
                Constants.ENTITY_TYPENAME_FORMAT, true); //NOTE: can throw DirectoryNotFoundException
            await cache.Update(updates); // Loads indexes into memory, updates .dat file, updates in-memory indexes

            // Since .dat files updated, load from disk
            await cache.Load(ctx: Context,
                loadIndexes: false); //FYI: Not loading indexes, skips entities flagged for delete (Ignore()==TRUE)
            await PurgeAndSave(); // Save to disk (.dat as well as indexes
        }
#endif

        //Used when a Physician is modified in Physician Maintenance
        public override async System.Threading.Tasks.Task Update(DateTime? lastUpdatedDate, EntityChangeSet changeSet,
            Action callback)
        {
            //J.E. PROBLEM with current RIACacheManager.Update(DomainContext, ChangeSet) - added entities in the change set are added to the context - CANNOT have NEW entities in a cache's Context
            //var diskManager = new RIACacheManager(Path.Combine(this.ApplicationStore, Constants.REFERENCE_DATA_STORE_FOLDER), CacheName, Constants.ENTITY_TYPENAME_FORMAT, true); //NOTE: can throw DirectoryNotFoundException
            //diskManager.Update(this.Context, changeSet);
            //this.PurgeAndSave();  //Use existing code to save Context to disk and re-build indexes
            //Deployment.Current.Dispatcher.BeginInvoke(() =>
            //{
            //    isLoading = false;
            //    TotalRecords = Context.Physicians.Count();
            //    if (callback != null)
            //        callback();
            //});

            var client_anchor = await GetAnchor();
            UpdateLocalCacheFromDatabase(DateTime.UtcNow, client_anchor, callback);
        }
    }
}