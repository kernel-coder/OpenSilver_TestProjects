using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.View
{
    public partial class PatientDNRDialog : ChildWindow
    {
        public PatientDNRDialog(Patient patient)
        {
            InitializeComponent();
            if (patient == null) return;
            this.Title = patient.FullNameInformal + " DNR Orders";
            dnrListBox.ItemsSource = patient.ActiveDNRs;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}