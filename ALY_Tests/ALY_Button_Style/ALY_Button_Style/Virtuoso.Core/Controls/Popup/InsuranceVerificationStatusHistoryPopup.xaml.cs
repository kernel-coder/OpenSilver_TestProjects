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

namespace Virtuoso.Core.Controls
{
    public partial class InsuranceVerificationStatusHistoryPopup : ChildWindow
    {
        private List<InsuranceVerificationWorklistHistory> _WorklistHistory = null;

        public List<InsuranceVerificationWorklistHistory> WorklistHistory
        {
            get { return _WorklistHistory; }
            set { _WorklistHistory = value; }
        }

        public InsuranceVerificationStatusHistoryPopup()
        {
            InitializeComponent();
            DataContext = this;
        }

        public InsuranceVerificationStatusHistoryPopup(List<InsuranceVerificationWorklistHistory> inList)
        {
            InitializeComponent();
            DataContext = this;
            this._WorklistHistory = inList;
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