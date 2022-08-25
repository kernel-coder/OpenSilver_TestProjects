#region Usings

using System;
using System.Windows.Controls;

#endregion

namespace Virtuoso.Core.Application
{
    [CLSCompliant(true)]
    public interface IApplicationState
    {

        bool IsOutOfBroswer { get; }

        void SetApplicationVisual(UserControl view);

        void SetReady();

        void EnqueueBusy();

        void DequeueBusy();
    }
}