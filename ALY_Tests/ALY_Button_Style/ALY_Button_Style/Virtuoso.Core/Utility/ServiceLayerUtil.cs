#region Usings

using System;
using Virtuoso.Client.Core;
using Virtuoso.Services;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Utility
{
    public static class ServiceLayerUtil
    {
        public static PingContext AvailabilityContext;
        public static VirtuosoDomainContext VirtuosoContext;
        public static Uri BaseUri { get; private set; }

        static ServiceLayerUtil()
        {
            BaseUri = System.Windows.Application.Current.GetServerBaseUri();
            AvailabilityContext =
                new PingContext(new Uri(BaseUri,
                    "Virtuoso-Services-PingService.svc")); //using alternate constructor, so that it can run in a thread            
            VirtuosoContext =
                new VirtuosoDomainContext(new Uri(BaseUri,
                    "Virtuoso-Services-Web-VirtuosoDomainService.svc")); //using alternate constructor, so that it can run in a thread            
        }

        public static System.Threading.Tasks.Task<bool> ServerAvailableAsync()
        {
            var source = new System.Threading.Tasks.TaskCompletionSource<bool>();
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                Guid token = Guid.NewGuid(); //different everytime, so not cached
                try
                {
                    AvailabilityContext.ServerCheck(token)
                        .AsTask()
                        .ContinueWith(t =>
                        {
                            if (t.IsCanceled)
                            {
                                source.SetCanceled();
                            }

                            if (t.Exception != null)
                            {
                                source.SetException(t.Exception);
                            }

                            if (source.Task.IsFaulted == false)
                            {
                                if (token.Equals(t.Result.Value)) //check that what was sent is what was received
                                {
                                    source.SetResult(true);
                                }
                                else
                                {
                                    source.SetResult(false);
                                }
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

        public static System.Threading.Tasks.Task<int> EncounterStatusAsync(int TaskKey, int EncounterKey,
            int EncounterStatus)
        {
            var source = new System.Threading.Tasks.TaskCompletionSource<int>();
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                try
                {
                    VirtuosoContext.GetEncounterStatus(TaskKey, EncounterKey, EncounterStatus)
                        .AsTask()
                        .ContinueWith(t =>
                        {
                            if (t.IsCanceled)
                            {
                                source.SetCanceled();
                            }

                            if (t.Exception != null)
                            {
                                source.SetException(t.Exception);
                            }

                            if (source.Task.IsFaulted == false)
                            {
                                source.SetResult(t.Result.Value);
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

        public static System.Threading.Tasks.Task<bool> IsEncounterSyncedAsync(int TaskKey, int EncounterKey,
            int EncounterStatus)
        {
            var source = new System.Threading.Tasks.TaskCompletionSource<bool>();
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                try
                {
                    VirtuosoContext.GetIsEncounterSynced(TaskKey, EncounterKey, EncounterStatus)
                        .AsTask()
                        .ContinueWith(t =>
                        {
                            if (t.IsCanceled)
                            {
                                source.SetCanceled();
                            }

                            if (t.Exception != null)
                            {
                                source.SetException(t.Exception);
                            }

                            if (source.Task.IsFaulted == false)
                            {
                                source.SetResult(t.Result.Value);
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
        
        public static System.Threading.Tasks.Task<bool> DiscardDataEventAsync(int EncounterKey)
        {
            var source = new System.Threading.Tasks.TaskCompletionSource<bool>();
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                try
                {
                    VirtuosoContext.DiscardDataEvent(JSONSerializer.SerializeToJsonString(EncounterKey), DateTime.Now)
                        .AsTask()
                        .ContinueWith(t =>
                        {
                            if (t.IsCanceled)
                            {
                                source.SetCanceled();
                            }

                            if (t.Exception != null)
                            {
                                source.SetException(t.Exception);
                            }

                            if (source.Task.IsFaulted == false)
                            {
                                source.SetResult(t.Result.Value);
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
    }
}