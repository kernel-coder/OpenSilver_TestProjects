using System;
using GalaSoft.MvvmLight;

namespace Virtuoso.Maintenance.Controls
{
    public partial class PatientContactUserControl : PatientContactUserControlBase, ICleanup
    {
        public PatientContactUserControl()
        {
            InitializeComponent();

            this.ItemSelected += new EventHandler(UserControl_ItemSelected);
        }

        void UserControl_ItemSelected(object sender, EventArgs e)
        {
            this.DetailAreaScrollViewer.ScrollToVerticalOffset(0);
            Virtuoso.Core.Utility.SetFocusHelper.SelectFirstEditableWidget(this);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            this.ItemSelected -= UserControl_ItemSelected;
        }
    }
}