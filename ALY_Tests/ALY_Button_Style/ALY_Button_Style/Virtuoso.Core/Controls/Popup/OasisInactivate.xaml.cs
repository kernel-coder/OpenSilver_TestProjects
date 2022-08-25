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
    public partial class OASISInactivate : ChildWindow
    {
        public OASISInactivate(AdmissionDocumentationItem item, bool Inactivate, string OasisAddendum)
        {
            InitializeComponent();
            Title = (Inactivate) ? "Inactivate this survey ?" : "Reactivate this survey ?";
            if ((item != null) && (item.Encounter != null) && (item.Encounter.MostRecentEncounterOasis != null))
            {
                surveyTextBlock.Text = item.Encounter.MostRecentEncounterOasis.RFADescription;
                if (item.Encounter.MostRecentEncounterOasis.M0090 != null)
                    surveyTextBlock.Text = surveyTextBlock.Text + " survey, completed on " +
                                           ((DateTime)item.Encounter.MostRecentEncounterOasis.M0090)
                                           .ToShortDateString();
            }

            inactivateRadioButton.IsChecked = true;
            inactivateForKeyChangeRadioButton.IsChecked = false;
            if (OasisAddendum == null)
            {
                inactivateForKeyChangeNoteTextBlock.Text =
                    "( Note - no patient demographic changes were made to warrant a survey inactivation for a key change )";
                inactivateForKeyChangeNoteTextBlock.Visibility = Visibility.Visible;
            }
        }

        public bool InactivateForKeyChange
        {
            get { return (inactivateForKeyChangeRadioButton.IsChecked == true) ? true : false; }
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