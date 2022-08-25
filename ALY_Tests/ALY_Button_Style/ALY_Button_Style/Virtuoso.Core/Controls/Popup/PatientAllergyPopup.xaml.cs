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
using GalaSoft.MvvmLight;

namespace Virtuoso.Core.Controls
{
    public partial class PatientAllergyPopup : UserControl, ICleanup
    {
        public PatientAllergyPopup()
        {
            InitializeComponent();
        }

        private void patientAllergyGrid_BindingValidationError(object sender, ValidationErrorEventArgs e)
        {
            ValidationSummaryItem valsumremove =
                valSum.Errors.FirstOrDefault(v => v.Message.Equals(e.Error.ErrorContent.ToString()));

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

        public void Cleanup()
        {
        }
    }
}