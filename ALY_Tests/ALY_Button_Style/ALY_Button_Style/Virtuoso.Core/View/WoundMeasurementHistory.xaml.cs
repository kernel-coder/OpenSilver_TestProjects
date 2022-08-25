using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;
using Virtuoso.Core.Converters;

namespace Virtuoso.Core.View
{
    public partial class WoundMeasurementHistory : ChildWindow
    {
        private Admission currentAdmission = null;
        private AdmissionWoundSite currentAdmissionWoundSite = null;
        private Encounter currentEncounter = null;
        public List<GraphItem> GraphItems = new List<GraphItem>();
        public bool isCancel = false;

        public WoundMeasurementHistory(AdmissionWoundSite wound, Encounter encounter)
        {
            currentAdmissionWoundSite = wound;
            currentAdmission = currentAdmissionWoundSite.Admission;
            currentEncounter = encounter;
            InitializeComponent();

            DateTime? startDate = DateTime.MinValue;
            DateTime? endDate = DateTime.MaxValue;

            List<WoundMeasurementHistoryItem> hList =
                currentAdmission.GetWoundMeasurementHistory(startDate, endDate, currentAdmissionWoundSite,
                    currentEncounter);
            if ((hList == null) || (hList.Any() == false))
            {
                noHistory.Visibility = Visibility.Visible;
            }
            else
            {
                woundMeasurementHistoryDataGrid.Visibility = Visibility.Visible;
                woundMeasurementHistoryDataGrid.ItemsSource = hList;
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}