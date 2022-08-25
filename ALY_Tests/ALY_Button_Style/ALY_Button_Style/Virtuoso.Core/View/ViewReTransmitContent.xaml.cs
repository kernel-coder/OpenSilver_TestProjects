using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using System;

namespace Virtuoso.Core.View
{
    public partial class ViewReTransmitContent : ChildWindow
    {
        public ViewReTransmitContent(List<EncounterOasis> encounterOasisList)
        {
            InitializeComponent();
            if (encounterOasisList != null)
                Title = "Re-Transmit File Preview (" + encounterOasisList.Count +
                        ((encounterOasisList.Count == 1) ? " Survey)" : " Surveys)");
            SurveyGrid.ItemsSource = encounterOasisList;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}