using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Net;
using Virtuoso.Server.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Virtuoso.Core.Controls
{
    public partial class OrdersTrackingBatchPopup : UserControl
    {
        public OrdersTrackingBatchPopup()
        {
            InitializeComponent();
        }

        private double FUDGE = (3 * 36) + 10 + 40;
        private double MINheight = 60;

        private void BatchPopupBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                double newHeight = BatchPopupBorder.ActualHeight - FUDGE;
                if (newHeight < MINheight) newHeight = MINheight;
                InterimOrdersReadyToBatchListDataGrid.Height = newHeight;
                InterimOrdersReadyToBatchListDataGrid.UpdateLayout();
                BatchesThatNeedDocumentListDataGrid.Height = newHeight;
                BatchesThatNeedDocumentListDataGrid.UpdateLayout();
                BatchesThatNeedToPrintListDataGrid.Height = newHeight;
                BatchesThatNeedToPrintListDataGrid.UpdateLayout();
            });
        }
    }
}