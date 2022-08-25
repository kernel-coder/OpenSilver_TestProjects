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
using Virtuoso.Core.Cache.Extensions;

namespace Virtuoso.Core.Controls
{
    public partial class AdmissionCommunicationPopup : UserControl
    {
        public AdmissionCommunicationPopup()
        {
            InitializeComponent();
        }

        private void AdmissionCommunicationGrid_BindingValidationError(object sender, ValidationErrorEventArgs e)
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

        private void buttonPhysicianDetails_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null) return;
            int physicianKey = 0;
            try
            {
                physicianKey = Int32.Parse(b.Tag.ToString());
            }
            catch
            {
            }

            if (physicianKey <= 0) return;
            Physician physician = PhysicianCache.Current.GetPhysicianFromKey(physicianKey);
            if (physician == null) return;
            PhysicianDetailsDialog d = new PhysicianDetailsDialog(physician);
            d.Show();
        }
    }
}