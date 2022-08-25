#region Usings

using System;
using System.Windows.Controls;

#endregion

namespace Virtuoso.Core.Navigation
{
    public interface INavigationService
    {
        Uri CurrentSource { get; }
        void Navigate(string path);
        void Navigate(string path, params object[] args);
        void Refresh();
        void SetFrame(Frame frame);
    }
}