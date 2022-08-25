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
    public partial class ResultingFunctionalDeficit : UserControl
    {
        public ResultingFunctionalDeficit()
        {
            InitializeComponent();
        }
        public static DependencyProperty IsHospiceAdmissionProperty =
         DependencyProperty.Register("IsHospiceAdmission", typeof(bool), typeof(Virtuoso.Core.Controls.ResultingFunctionalDeficit),
            new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.ResultingFunctionalDeficit)o).IsHospiceAdmissionChanged(); ;
            }));

        public bool IsHospiceAdmission
        {
            get { return ((bool)(base.GetValue(ResultingFunctionalDeficit.IsHospiceAdmissionProperty))); }
            set
            {
                base.SetValue(ResultingFunctionalDeficit.IsHospiceAdmissionProperty, value);
            }
        }
        private void IsHospiceAdmissionChanged()
        {
            // Hide if hospice
            ResultingFunctionalDeficitGrid.Visibility = (IsHospiceAdmission) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
