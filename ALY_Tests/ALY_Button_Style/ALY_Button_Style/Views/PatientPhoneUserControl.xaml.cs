using System;
using System.Collections.ObjectModel;

namespace Virtuoso.Maintenance.Controls
{
    public partial class PatientPhoneUserControl : PatientPhoneUserControlBase
    {
        public PatientPhoneUserControl()
        {
            InitializeComponent();
            DataContext = this;
            ItemsSource = new ObservableCollection< Server.Data.PatientPhone>();
            this.ItemSelected += new EventHandler(UserControl_ItemSelected);
        }

        void UserControl_ItemSelected(object sender, EventArgs e)
        {
            this.DetailAreaScrollViewer.ScrollToVerticalOffset(0);
        }

        public override void Cleanup()
        {
            this.ItemSelected -= UserControl_ItemSelected;
            base.Cleanup();
        }
    }
}