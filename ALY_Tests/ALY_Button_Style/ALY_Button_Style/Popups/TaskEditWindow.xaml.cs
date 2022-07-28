using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Virtuoso.Home.V2.Controls
{
    public partial class TaskEditWindow : ChildWindow
    {
        int ApplicationPadding
        {
            get { return 50; }
        }

        int TaskKey { get; set; }
        int? PatientKey { get; set; }
        int? AdmissionKey { get; set; }
        int? ServiceTypeKey { get; set; }
        bool DeleteTask { get; set; }
        bool OverrideTaskInfo { get; set; }

        public TaskEditWindow(int _taskKey, int? _patientKey, int? _admissionKey, int? _svcKey,
            bool _deleteTask = false, bool _overrideTaskInfo = false)
        {
            InitializeComponent();

            TaskKey = _taskKey;
            PatientKey = _patientKey;
            AdmissionKey = _admissionKey;
            ServiceTypeKey = _svcKey;
            DeleteTask = _deleteTask;
            OverrideTaskInfo = _overrideTaskInfo;

            this.Loaded += TaskEditWindow_Loaded;
            this.Unloaded += TaskEditWindow_Unloaded;
        }

        public TaskEditWindow()
        {
            this.Loaded += TaskEditWindow_Loaded;
            this.Unloaded += TaskEditWindow_Unloaded;
        }

        private void TaskEditWindow_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        void TaskEditWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {

            });
        }


        // Offer ESC key support for closing the ChildWindow:
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }
    }
}