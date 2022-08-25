using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.View
{
    public partial class PatientContactDetailsDialog : ChildWindow
    {
        public PatientContactDetailsDialog(PatientContact patientContact)
        {
            InitializeComponent();
            if (patientContact == null) return;
            this.Title = patientContact.FullNameInformal;
            this.DataContext = patientContact;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}