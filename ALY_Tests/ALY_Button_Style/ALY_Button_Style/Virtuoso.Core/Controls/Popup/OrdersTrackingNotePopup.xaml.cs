using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Net;
using Virtuoso.Server.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Virtuoso.Core.Controls
{
    public partial class OrdersTrackingNotePopup : UserControl
    {
        public OrdersTrackingNotePopup()
        {
            InitializeComponent();
        }

        private void OrdersTrackingNote_BindingValidationError(object sender, ValidationErrorEventArgs e)
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