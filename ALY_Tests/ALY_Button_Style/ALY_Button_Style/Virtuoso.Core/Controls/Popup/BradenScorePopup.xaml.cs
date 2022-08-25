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

namespace Virtuoso.Core.Controls
{
    public partial class BradenScorePopup : UserControl
    {
        public BradenScorePopup()
        {
            InitializeComponent();
        }

        private void UserControl_BindingValidationError(object sender, ValidationErrorEventArgs e)
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