namespace Virtuoso.Maintenance.Controls
{
    public partial class PatientPainLocationUserControl : PatientPainLocationUserControlBase
    {
        public PatientPainLocationUserControl()
        {
            InitializeComponent();
#if OPENSILVER
            MainGrid.CustomLayout = true;
            MainGrid.ClipToBounds = true;
#endif
        }
    }
}