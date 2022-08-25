using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.View
{
    public partial class PhysicianDetailsDialog : ChildWindow
    {
        public PhysicianDetailsDialog(Physician physician)
        {
            InitializeComponent();
            if (physician == null) return;
            this.Title = physician.FullNameInformalWithSuffix;
            PhysicianDisplay pd = new PhysicianDisplay() { Physician = physician };
            this.DataContext = pd;
        }

        public PhysicianDetailsDialog(PhysicianDisplay physician)
        {
            InitializeComponent();
            if (physician == null) return;
            if (physician.Physician != null)
                this.Title = physician.Physician.FullNameInformalWithSuffix;
            this.DataContext = physician;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}