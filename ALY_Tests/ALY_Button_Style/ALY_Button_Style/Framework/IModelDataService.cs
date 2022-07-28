#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Virtuoso.Core.Events;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Framework
{
    public interface IModelDataService<T>
    {
        void Add(T entity);
        void Remove(T entity);
        bool IsLoading { get; set; }
        bool PendingSubmit { get; }
        IEnumerable<T> Items { get; }
        List<SearchParameter> SearchParameters { get; set; }
        event EventHandler<EntityEventArgs<T>> OnLoaded;
        event EventHandler<ErrorEventArgs> OnSaved;
        event PropertyChangedEventHandler PropertyChanged;
        void Clear();
        void GetAsync();
        void GetSearchAsync(bool isSystemSearch);
        bool SaveAllAsync();
        void RejectChanges();
        bool ContextHasChanges { get; }
        VirtuosoDomainContext Context { get; set; }
    }
}