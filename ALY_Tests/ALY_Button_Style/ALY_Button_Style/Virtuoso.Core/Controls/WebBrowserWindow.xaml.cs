using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Virtuoso.Core.Controls
{
    public partial class WebBrowserWindow : ChildWindow
    {
        int ApplicationPadding { get { return 50; } }

        public WebBrowserWindow(Uri navigateURI)
        {
            InitializeComponent();

            SizeWindow(System.Windows.Application.Current.MainWindow.Width, System.Windows.Application.Current.MainWindow.Height);

            this.webBrowser.Navigate(navigateURI);

            Messenger.Default.Register<Size>(this, Constants.Application.Resize, 
                (newSize) => { 
                    SizeWindow(newSize.Width, newSize.Height);
                });

            this.Closed += new EventHandler(WebBrowserWindow_Closed);

            Messenger.Default.Register<bool>(this, Constants.Application.Authenticated,
                (authenticated) =>
                {
                    if (authenticated)
                    {
                        this.webBrowser.Visibility = System.Windows.Visibility.Visible;
                        this.webBrowserBrushContainer.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else
                    {
                        this.webBrowserBrushContainer.Visibility = System.Windows.Visibility.Visible;
                        this.webBrowser.Visibility = System.Windows.Visibility.Collapsed;                        
                    }
                });
        }
        void WebBrowserWindow_Closed(object sender, EventArgs e)
        {
            Messenger.Default.Unregister(this);
        }
        private void SizeWindow(double width, double height)
        {
            this.Width = width - ApplicationPadding;
            this.Height = height - ApplicationPadding;            
        }
    }
}

