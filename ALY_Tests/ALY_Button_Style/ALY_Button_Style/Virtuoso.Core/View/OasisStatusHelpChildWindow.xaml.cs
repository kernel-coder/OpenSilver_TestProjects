using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.View
{
    public partial class OasisStatusHelpChildWindow : ChildWindow
    {
        public OasisStatusHelpChildWindow(AdmissionWoundSite wound)
        {
            InitializeComponent();
            if (wound == null) return;
            if (wound.OasisStatus == null) return;
            Border b = null;
            if ((int)wound.OasisStatus == 1) b = borderOasisStatus1;
            else if ((int)wound.OasisStatus == 2) b = borderOasisStatus2;
            else if ((int)wound.OasisStatus == 3) b = borderOasisStatus3;
            else if ((int)wound.OasisStatus == 4) b = borderOasisStatus4;
            if (b != null)
            {
                b.BorderThickness = new Thickness(5);
                try
                {
                    b.BorderBrush = (Brush)System.Windows.Application.Current.Resources["OasisMoneyColorBrush"];
                }
                catch
                {
                }
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}