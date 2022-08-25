using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Virtuoso.Core.View;
using Virtuoso.Server.Data;
using Virtuoso.Core.Controls;


namespace Virtuoso.Core.Controls
{
    public partial class IVSitePopup : UserControl
    {
        public IVSitePopup()
        {
            InitializeComponent();
        }

        private void ivGrid_BindingValidationError(object sender, ValidationErrorEventArgs e)
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
    }
}