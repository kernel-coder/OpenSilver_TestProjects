#region Usings

using System;
using System.ComponentModel.Composition;
using System.Windows.Controls;

#endregion

namespace Virtuoso.Core.Navigation
{
    /// <summary>
    /// Wrapper for <see cref="Frame"/> to abstract the navigation process.
    /// </summary>
    [Export(typeof(INavigationService))]
    public class NavigationService : INavigationService
    {
        /// <summary>
        /// The frame doing the actual navigation.
        /// </summary>
        private Frame navigationFrame;

        public Uri CurrentSource => navigationFrame.CurrentSource;

        /// <summary>
        /// Navigate to a given path.
        /// </summary>
        /// <param name="path"></param>
        public void Navigate(string path)
        {
            navigationFrame.Navigate(new Uri(path, UriKind.Relative));
        }

        /// <summary>
        /// Navigate to a given path and process some arguments.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="args"></param>
        public void Navigate(string path, params object[] args)
        {
            Navigate(String.Format(path, args));
        }

        public void Refresh()
        {
            navigationFrame.Refresh();
        }

        /// <summary>
        /// Update the frame in the navigation service
        /// </summary>
        public void SetFrame(Frame frame)
        {
            this.navigationFrame = frame;
        }
    }
}