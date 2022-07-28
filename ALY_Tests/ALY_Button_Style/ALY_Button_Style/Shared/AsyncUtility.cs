using System;
using System.Threading;
using System.Threading.Tasks;

namespace Virtuoso.Client.Utils
{
    public class AsyncUtility
    {
        public static TaskScheduler TaskScheduler
        {
            get
            {
#if OPENSILVER // to fix error 'The current SynchronizationContext may not be used as a TaskScheduler.'
                return TaskScheduler.Default;
#else
                return TaskScheduler.FromCurrentSynchronizationContext();
#endif
            }
        }

        public static Task TaskFromResult()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        public static Task<T> TaskFromResult<T>(T instance)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(instance);
            return tcs.Task;
        }

        /// <summary>
        /// Run the delegate inside an explicit separate thread.  Otherwise equivalent to Run().
        /// </summary>
        /// <param name="callback"></param>
        public static void RunAsync(Func<System.Threading.Tasks.Task> callback)
        {
            ThreadPool.QueueUserWorkItem(_ => Run(callback));
        }

        /// <summary>
        /// Use this method to call an async method from the main thread.  
        /// Note that this will still execute the method asynchronously, but will handle exceptions for you automatically.
        /// async/await is preferred due to automatic exception unwrapping
        /// </summary>
        /// <param name="callback"></param>
        public static void Run(Func<System.Threading.Tasks.Task> callback)
        {
            var taskExecution = callback();
            taskExecution.ContinueWith((res) =>
            {
                if (res.IsFaulted)
                {
                    AsyncUtility.RunOnMainThread(() => { throw res.Exception; });
                }
            });
        }

        public static Task Delay(int millisecondsTimeout)
        {
#if !OPENSILVER
            var tcs = new TaskCompletionSource<bool>();
            //Task.Factory.StartNew(() => {
            //    Thread.Sleep(millisecondsTimeout);
            //    tcs.SetResult(true);
            //});
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(millisecondsTimeout);
                tcs.SetResult(true);
            });
            return tcs.Task;
#else
            return System.Threading.Tasks.Task.Delay(1);
#endif
        }

        public static void RunOnMainThread(Action callback)
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => callback());
        }
    }
}
