using System.Windows;
using System.Windows.Controls;
using Virtuoso.Core.Cache;

namespace Virtuoso.Core.Controls
{
    public partial class TaskPopup : UserControl
    {
        public TaskPopup()
        {
            InitializeComponent();

            this.Loaded += TaskPopup_Loaded;
            this.Unloaded += TaskPopup_Unloaded;

#if OPENSILVER
            this.comboCareGiver.CustomLayout = true;
            this.comboCareGiver.MaxDropDownHeight = 300;
#endif

        }

        private void TaskPopup_Unloaded(object sender, RoutedEventArgs e)
        {
            this.PatientCombo.Cleanup();
            this.AdmissionCombo.Cleanup();
            this.CancelCombo.Cleanup();
        }

        void TaskPopup_Loaded(object sender, RoutedEventArgs e)
        {
            //NOTE: setting Format in XAML was raising an error...setting in code-behind seems to work fine...

            if ((bool)TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
            {
                TaskTimePicker.Format = new CustomTimeFormat("HHmm");
                TaskTimePicker2.Format = new CustomTimeFormat("HHmm");
            }
            else
            {
                TaskTimePicker.Format = new ShortTimeFormat();
                TaskTimePicker2.Format = new ShortTimeFormat();
            }
        }
    }
}