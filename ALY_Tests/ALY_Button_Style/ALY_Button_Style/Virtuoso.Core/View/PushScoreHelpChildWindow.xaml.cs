using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.View
{
    public partial class PushScoreHelpChildWindow : ChildWindow
    {
        public PushScoreHelpChildWindow(AdmissionWoundSite wound)
        {
            InitializeComponent();
            if (wound == null) return;
            Border b = null;
            if (wound.PushSurfaceArea != null)
            {
                if ((int)wound.PushSurfaceArea == 0) b = borderPushSurfaceArea0;
                else if ((int)wound.PushSurfaceArea == 1) b = borderPushSurfaceArea1;
                else if ((int)wound.PushSurfaceArea == 2) b = borderPushSurfaceArea2;
                else if ((int)wound.PushSurfaceArea == 3) b = borderPushSurfaceArea3;
                else if ((int)wound.PushSurfaceArea == 4) b = borderPushSurfaceArea4;
                else if ((int)wound.PushSurfaceArea == 5) b = borderPushSurfaceArea5;
                else if ((int)wound.PushSurfaceArea == 6) b = borderPushSurfaceArea6;
                else if ((int)wound.PushSurfaceArea == 7) b = borderPushSurfaceArea7;
                else if ((int)wound.PushSurfaceArea == 8) b = borderPushSurfaceArea8;
                else if ((int)wound.PushSurfaceArea == 9) b = borderPushSurfaceArea9;
                else if ((int)wound.PushSurfaceArea == 10) b = borderPushSurfaceArea10;
                if (b != null)
                {
                    b.BorderThickness = new Thickness(4);
                    try
                    {
                        b.BorderBrush = (Brush)System.Windows.Application.Current.Resources["OasisMoneyColorBrush"];
                    }
                    catch
                    {
                    }
                }

                textBlockPushSurfaceArea.Text = wound.PushSurfaceArea.ToString();
            }

            if (wound.PushExudateAmount != null)
            {
                if ((int)wound.PushExudateAmount == 0) b = borderPushExudateAmount0;
                else if ((int)wound.PushExudateAmount == 1) b = borderPushExudateAmount1;
                else if ((int)wound.PushExudateAmount == 2) b = borderPushExudateAmount2;
                else if ((int)wound.PushExudateAmount == 3) b = borderPushExudateAmount3;
                if (b != null)
                {
                    b.BorderThickness = new Thickness(4);
                    try
                    {
                        b.BorderBrush = (Brush)System.Windows.Application.Current.Resources["OasisMoneyColorBrush"];
                    }
                    catch
                    {
                    }
                }

                textBlockPushExudateAmount.Text = wound.PushExudateAmount.ToString();
            }

            if (wound.PushTissueType != null)
            {
                if ((int)wound.PushTissueType == 0) b = borderPushTissueType0;
                else if ((int)wound.PushTissueType == 1) b = borderPushTissueType1;
                else if ((int)wound.PushTissueType == 2) b = borderPushTissueType2;
                else if ((int)wound.PushTissueType == 3) b = borderPushTissueType3;
                else if ((int)wound.PushTissueType == 4) b = borderPushTissueType4;
                if (b != null)
                {
                    b.BorderThickness = new Thickness(4);
                    try
                    {
                        b.BorderBrush = (Brush)System.Windows.Application.Current.Resources["OasisMoneyColorBrush"];
                    }
                    catch
                    {
                    }
                }

                textBlockPushTissueType.Text = wound.PushTissueType.ToString();
            }

            if (wound.PushScore != null) textBlockPushScore.Text = wound.PushScore.ToString();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}