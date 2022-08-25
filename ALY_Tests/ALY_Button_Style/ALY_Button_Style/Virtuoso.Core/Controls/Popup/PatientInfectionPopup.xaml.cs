using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Virtuoso.Server.Data;
using Virtuoso.Core.Cache;
using Virtuoso.Core.View;

namespace Virtuoso.Core.Controls
{
    public partial class PatientInfectionPopup : UserControl
    {
        public PatientInfectionPopup()
        {
            InitializeComponent();
        }

        private void PatientInfection_BindingValidationError(object sender, ValidationErrorEventArgs e)
        {
            ValidationSummaryItem valsumremove =
                valSum.Errors.Where(v => v.Message.Equals(e.Error.ErrorContent.ToString())).FirstOrDefault();

            if (e.Action == ValidationErrorEventAction.Removed)
            {
                valSum.Errors.Remove(valsumremove);
            }
            else if (e.Action == ValidationErrorEventAction.Added)
            {
                if (valsumremove == null)
                {
                    ValidationSummaryItem vsi = new ValidationSummaryItem()
                        { Message = e.Error.ErrorContent.ToString(), Context = e.OriginalSource };
                    vsi.Sources.Add(new ValidationSummaryItemSource(String.Empty, e.OriginalSource as Control));

                    valSum.Errors.Add(vsi);

                    e.Handled = true;
                }
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Virtuoso.Core.Utility.SetFocusHelper.SelectFirstEditableWidget(this);
        }

        private void TransmissionPrecautionsHelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelpDialog(Virtuoso.Server.Data.PatientInfection.TransmissionPrecautionsHelp,
                "Transmission Precautions");
        }

        private void POAHelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelpDialog(Virtuoso.Server.Data.PatientInfection.POAHelp, "Present on Admission (POA)");
        }

        private void ShowHelpDialog(Paragraph templateContent, string title)
        {
            var helpDialog = new HelpPopupDialog(templateContent, title);
            helpDialog.Show();
        }
    }
}