using System;
using System.Windows.Controls;
using System.Linq;

namespace Virtuoso.Core.Controls
{
    public partial class AdmissionConsentPopup : UserControl
    {
        public AdmissionConsentPopup()
        {
            InitializeComponent();
        }

        private void AdmissionConsentGrid_BindingValidationError(object sender, ValidationErrorEventArgs e)
        {
            ValidationSummaryItem valsumremove =
                valSum.Errors.Where(v => v.Message.Equals(e.Error.ErrorContent.ToString())).FirstOrDefault();

            switch (e.Action)
            {
                case ValidationErrorEventAction.Removed:
                    valSum.Errors.Remove(valsumremove);
                    break;
                case ValidationErrorEventAction.Added:
                    if (valsumremove == null)
                    {
                        ValidationSummaryItem vsi = new ValidationSummaryItem()
                            { Message = e.Error.ErrorContent.ToString(), Context = e.OriginalSource };
                        vsi.Sources.Add(new ValidationSummaryItemSource(String.Empty, e.OriginalSource as Control));

                        valSum.Errors.Add(vsi);

                        e.Handled = true;
                    }

                    break;
            }
        }
    }
}