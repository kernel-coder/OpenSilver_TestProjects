using System;
using System.IO;
using System.Net;
using System.Windows;
using Virtuoso.Client.Core;
using Virtuoso.ViewModel;

namespace Virtuoso.Core.View
{
    public partial class HelpPDF : PageBase
    {
        public HelpPDF()
        {
            InitializeComponent();
            this.DataContext = VirtuosoContainer.Current.GetExport<HelpPDFViewModel>().Value;
        }

        public bool LoadPDF()
        {
            bool retval = false;
            if (DataContext != null)
            {
                retval = true;
                try
                {
                    this.IsEnabled = false;
                    this.Opacity = 10;
                    MyBrowser.Navigate(((HelpPDFViewModel)DataContext).PDFDocumentToDisplay);
                }
                catch (Exception e)
                {
                    BusyIndicator.IsBusy = false;
                    stackPanelLoading.Visibility = System.Windows.Visibility.Collapsed;
                    MessageBox.Show(e.Message);
                }
            }

            return retval;
        }

        private void MyBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            BusyIndicator.IsBusy = false;
            stackPanelLoading.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}