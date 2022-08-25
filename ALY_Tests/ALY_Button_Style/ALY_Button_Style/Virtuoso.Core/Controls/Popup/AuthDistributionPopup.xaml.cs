using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Controls
{
    public partial class AuthDistributionPopup : ChildWindow
    {
        int ApplicationPadding
        {
            get { return 100; }
        }

        public AuthDistributionPopup(
            AdmissionAuthorizationInstance instance,
            List<Virtuoso.Core.ViewModel.AuthDistributionViewModel.Distribution> distribution,
            double height, double width,
            AuthMode mode,
            bool allowExtensionToDistribution)
        {
            InitializeComponent();

            //this.Height = height;
            //this.Width = width;

            //NOTE: do not use Size of System.Windows.Application.Current.MainWindow - doesn't work when end user scales their resolution
            SizeWindow(new Size(
                (System.Windows.Application.Current.RootVisual as FrameworkElement).ActualWidth,
                (System.Windows.Application.Current.RootVisual as FrameworkElement).ActualHeight));

            Messenger.Default.Register<Size>(this, Constants.Application.Resize,
                (newSize) => { SizeWindow(newSize); });

            var vm = new AuthDistributionViewModel(instance, distribution, mode, allowExtensionToDistribution);
            this.DataContext = vm;
        }

        private void SizeWindow(Size newSize)
        {
            //NOTE: System.Windows.Application.Current.MainWindow (Height/Width) doesn't work when end user adjusts their resolution
            //E.G. set 'Make text and other items larger or smaller' - 125%, 150%, 200%
            //var _w = System.Windows.Application.Current.MainWindow.Width;
            //var _h = System.Windows.Application.Current.MainWindow.Height;

            this.Height =
                newSize.Height -
                ApplicationPadding; //(Application.Current.RootVisual as FrameworkElement).ActualHeight - ApplicationPadding;
            this.Width =
                newSize.Width -
                ApplicationPadding; //(Application.Current.RootVisual as FrameworkElement).ActualWidth - ApplicationPadding;
        }

        // Offer ESC key support for closing the ChildWindow:
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        private void ChildWindow_Closing(object sender, CancelEventArgs e)
        {
            var VM = this.DataContext as AuthDistributionViewModel;
            if (VM != null)
            {
                VM.Cleanup();
            }
        }
    }
}