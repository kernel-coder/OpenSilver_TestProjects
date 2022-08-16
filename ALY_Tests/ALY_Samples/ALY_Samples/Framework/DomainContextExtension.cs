#region Usings

using System;
using System.Collections.Generic;
using OpenRiaServices.DomainServices.Client;

#endregion

//http://blogs.msdn.com/b/kylemc/archive/2010/11/02/using-the-visual-studio-async-ctp-with-ria-services.aspx
public static class OperationExtensions
{
    public static System.Threading.Tasks.Task<T> AsTask<T>(this T operation) where T : OperationBase
    {
        System.Threading.Tasks.TaskCompletionSource<T> tcs =
            new System.Threading.Tasks.TaskCompletionSource<T>(operation.UserState);

        EventHandler handler = null;
        handler = (sender, e) =>
        {
            if (operation.HasError && !operation.IsErrorHandled)
            {
                tcs.TrySetException(operation.Error);
                operation.MarkErrorAsHandled();
            }
            else if (operation.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else
            {
                //The type you get back is Task<InvokeOperation<T>>.
                tcs.TrySetResult(operation);
            }

            operation.Completed -= handler;
        };

        operation.Completed += handler;

        return tcs.Task;
    }
}

//http://zormim.wordpress.com/2011/01/06/using-the-new-async-framework-with-ria/
public static class DomainContextExtension
{
    public static System.Threading.Tasks.Task<IEnumerable<T>> LoadAsync<T>(this DomainContext source,
        EntityQuery<T> query) where T : Entity
    {
        return LoadAsync(source, query, LoadBehavior.KeepCurrent);
    }

    public static System.Threading.Tasks.Task<IEnumerable<T>> LoadAsync<T>(this DomainContext source,
        EntityQuery<T> query, LoadBehavior loadBehavior) where T : Entity
    {
        System.Threading.Tasks.TaskCompletionSource<IEnumerable<T>> taskCompletionSource =
            new System.Threading.Tasks.TaskCompletionSource<IEnumerable<T>>(System.Threading.Tasks.TaskCreationOptions
                .AttachedToParent);

        source.Load(
            query,
            loadBehavior,
            loadOperation =>
            {
                if (loadOperation.HasError && !loadOperation.IsErrorHandled)
                {
                    taskCompletionSource.TrySetException(loadOperation.Error);
                    loadOperation.MarkErrorAsHandled();
                }
                else if (loadOperation.IsCanceled)
                {
                    taskCompletionSource.TrySetCanceled();
                }
                else
                {
                    taskCompletionSource.TrySetResult(loadOperation.Entities);
                }
            },
            null);

        return taskCompletionSource.Task;
    }
}