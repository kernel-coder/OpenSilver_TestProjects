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
    public partial class InsuranceVerificationStatusPopup : ChildWindow
    {
        private string _Comment = "";

        public string Comment
        {
            get { return _Comment; }
            set { _Comment = value; }
        }

        public InsuranceVerificationStatusPopup()
        {
            InitializeComponent();
            this.HasCloseButton = false;
            DataContext = this;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}