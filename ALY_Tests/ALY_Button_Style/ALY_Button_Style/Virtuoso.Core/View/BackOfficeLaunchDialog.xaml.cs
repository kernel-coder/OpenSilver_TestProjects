using System.Windows.Controls;
using Virtuoso.Core.Cache;
using System.Windows;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Helpers;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System;
using System.Diagnostics;

namespace Virtuoso.Core.View
{
    public partial class BackOfficeLaunchDialog : ChildWindow
    {
        public BackOfficeLaunchDialog()
        {
            InitializeComponent();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Crescendo_Click(object sender, RoutedEventArgs e)
        {
            HandshakeToExternalApp p = new HandshakeToExternalApp(UserCache.Current.GetCurrentUserProfile());
            p.LaunchCrescendoPortal();
            this.DialogResult = false;
        }

        private void Calendar_Click(object sender, RoutedEventArgs e)
        {
            HandshakeToExternalApp p = new HandshakeToExternalApp(UserCache.Current.GetCurrentUserProfile());
            p.LaunchAdminPortal();
            this.DialogResult = false;
        }

        private void Crescendo_MouseEnter(object sender, MouseEventArgs e)
        {
            TempoButton.Opacity = 0.8;
        }

        private void Crescendo_MouseLeave(object sender, MouseEventArgs e)
        {
            TempoButton.Opacity = 1;
        }

        private void Calendar_MouseEnter(object sender, MouseEventArgs e)
        {
            CalendarButton.Opacity = 0.8;
        }

        private void Calendar_MouseLeave(object sender, MouseEventArgs e)
        {
            CalendarButton.Opacity = 1;
        }
    }
}
