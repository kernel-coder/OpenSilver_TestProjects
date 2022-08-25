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
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.User)]
    [Export(typeof(ICache))]
    public class UserCache : ReferenceDataCacheBase
    {
        public static UserCache Current { get; private set; }

        [ImportingConstructor]
        public UserCache(ILogger logManager)
            : base(logManager, ReferenceTableName.User, "029")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("UserCache already initialized.");
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

        void UpdateLocalCacheFromDatabase(DateTime new_anchor, DateTime client_anchor, Action callback)
        {
            var query = Context.GetUserProfileForCacheQuery(client_anchor);
            Context.Load(query, LoadBehavior.RefreshCurrent, OnLoaded,
                new { ClientAnchor = client_anchor, NewAnchor = new_anchor, Callback = callback });
        }

        private void LoadFromDisk(Action callback)
        {
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    var cache = await RIACacheManager.Initialize(
                        Path.Combine(ApplicationStore, Constants.REFERENCE_DATA_STORE_FOLDER), CacheName,
                        Constants.ENTITY_TYPENAME_FORMAT, true); //NOTE: can throw DirectoryNotFoundException
                    await cache.Load(Context, true);

                    BuildSearchDataStructures();

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        isLoading = false;
                        TotalRecords = Context.UserProfiles.Count;

                        if (TotalRecords <= 0)
                        {
                            Log(TraceEventType.Error, "0 users loaded from user cache.");
                        }

                        callback?.Invoke();
                    });
                }
                catch (DirectoryNotFoundException __directoryNotFoundException)
                {
                    //doesn't mean there is a problem necessarily, probably means that there was no data returned from the server, so nothing was saved to disk - e.g. no files
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        isLoading = false;
                        TotalRecords = Context.UserProfiles.Count();

                        Log(TraceEventType.Information,
                            string.Format(
                                "{0} Cache.  Directory not found.  Check that data was returned from server.  Possible that no data saved to disk.",
                                CacheName));

                        if (TotalRecords <= 0)
                        {
                            Log(TraceEventType.Error, "No users loaded from user cache.");

                            if (__directoryNotFoundException != null)
                            {
                                Log(TraceEventType.Error, "No users loaded from user cache.",
                                    __directoryNotFoundException);
                            }
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

        /// <summary>
        /// For use with RIA services
        /// </summary>
        /// <param name="operation"></param>
        private async void OnLoaded(LoadOperation<UserProfile> operation)
        {
            DateTime? client_anchor = null;
            DateTime? new_anchor = null;
            Action callback = null;
            if (operation.UserState != null)
            {
                dynamic obj = operation.UserState;
                client_anchor = obj.ClientAnchor;
                new_anchor = obj.NewAnchor;
                callback = obj.Callback;
            }

            if (operation.HasError)
            {
                Log(TraceEventType.Error, "Error loading user cache.", operation.Error);

                operation.MarkErrorAsHandled();

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
                if (DateTime.MinValue.Equals(client_anchor.GetValueOrDefault()))
#endif
                {
                    await PurgeAndSave();

                    BuildSearchDataStructures();

                    Deployment.Current.Dispatcher.BeginInvoke(async () =>
                    {
                        TotalRecords = Context.UserProfiles.Count;
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
                    if (operation.AllEntities.Any())
                    {
                        await IncrementalSave(operation.AllEntities);

                        Deployment.Current.Dispatcher.BeginInvoke(async () =>
                        {
                            TotalRecords = Context.UserProfiles.Count;
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
            //Update .dat files on disk
            var cache = await RIACacheManager.Initialize(
                Path.Combine(ApplicationStore, Constants.REFERENCE_DATA_STORE_FOLDER), CacheName,
                Constants.ENTITY_TYPENAME_FORMAT, true); //NOTE: can throw DirectoryNotFoundException
            await cache.Update(updates); //loads indexes into memory, updates .dat file, updates in-memory indexes

            //Since .dat files updated, load from disk
            await cache.Load(ctx: Context,
                loadIndexes: false); //FYI: not loading indexes, skips entities flagged for delete (Ignore()==TRUE)
            //PurgeAndSave();  // save to disk (.dat as well as indexes

            BuildSearchDataStructures();
        }
#endif

        //Used when a UserProfile is modified in UserProfile Maintenance
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
            //    TotalRecords = Context.UserProfiles.Count();
            //    if (callback != null)
            //        callback();
            //});

            var client_anchor = await GetAnchor();
            UpdateLocalCacheFromDatabase(DateTime.UtcNow, client_anchor, callback);
        }

        public Dictionary<Guid, UserProfile> UserProfileDictionary = new Dictionary<Guid, UserProfile>();

        private void BuildSearchDataStructures()
        {
            UserProfileDictionary = Context.UserProfiles.ToDictionary(fsq => fsq.UserId);
        }
    }
}