#region Usings

using System;
using System.ComponentModel;
using Virtuoso.Core.Events;

#endregion

namespace Virtuoso.Core.Framework
{
    public interface IMultiModelDataService
    {
        bool IsLoading { get; set; }
        bool IsSubmitting { get; }
        event EventHandler<MultiErrorEventArgs> OnMultiLoaded;
        event EventHandler<MultiErrorEventArgs> OnMultiSaved;
        event PropertyChangedEventHandler PropertyChanged;
        void SaveMultiAsync();
        void SaveMultiAsync(Action preSubmitAction);
        void RejectMultiChanges();
        bool ContextHasChanges { get; }
    }
}