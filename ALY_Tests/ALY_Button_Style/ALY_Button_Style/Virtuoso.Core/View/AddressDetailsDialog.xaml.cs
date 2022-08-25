using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.View
{
    public partial class AddressDetailsDialog : ChildWindow
    {
        public AddressDetailsDialog(PatientAddress patAddr)
        {
            InitializeComponent();
            this.DataContext = patAddr;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}