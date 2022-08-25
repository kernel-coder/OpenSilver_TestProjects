using System;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.ViewModel;
using System.Windows.Input;
using Virtuoso.Server.Data;
using System.Collections.Generic;
using System.Linq;

namespace Virtuoso.Core.Controls
{
    public partial class OASISMarkNoTransmit : ChildWindow
    {
        public OASISMarkNoTransmit(AdmissionDocumentationItem item, string title)
        {
            InitializeComponent();
            Title = title;
            if ((item != null) && (item.Encounter != null) && (item.Encounter.MostRecentEncounterOasis != null))
            {
                surveyTextBlock.Text = item.Encounter.MostRecentEncounterOasis.RFADescription;
                if (item.Encounter.MostRecentEncounterOasis.M0090 != null)
                    surveyTextBlock.Text = surveyTextBlock.Text + " survey, completed on " +
                                           ((DateTime)item.Encounter.MostRecentEncounterOasis.M0090)
                                           .ToShortDateString();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
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
    }
}