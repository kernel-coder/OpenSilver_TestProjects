using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Virtuoso.Server.Data;

namespace ALY_Button_Style
{
    public partial class HomeScreenTaskPM
    {
        public DateTime TaskStartDateTime { get; set; }
        public string TaskStartEnd { get; set; }
        public string TaskStartTime { get; set; }

        public string TaskEndDateTime { get; set; }


        public string FullNameInformal { get; set; }

        public string FullNameWithMRN { get; set; }

        public string AdmissionStatusAndDate { get; set; }

        public string Clinician { get; set; }

        public string LastVisitDateForCurrentUser { get; set; }

        public System.Nullable<int> PatientKey { get; set; }

        public int AdmissionKey { get; set; }
        public DateTime SOCDate { get; set; }
        public string CareCoordinator { get; set; }
        public string ServiceLineName { get; set; }

        public bool Exclude { get; set; } = false;

        public string DocumentDescriptionSortable { get; set; }
        public string TaskCommentsShort { get; set; }

        public EncounterStatusType TaskStatus { get; set; }
        public OrderStatusType OrderEntryStatus { get; set; }

        public string AssessmentColor { get; set; }
        public System.Nullable<bool> Translator { get; set; }
        public System.Nullable<bool> AssessmentOverdue { get; set; }
        public bool CanAttemptTask { get; set; }
    }

    public partial class DataGridTest : UserControl
    {
        public DataGridTest()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var list = new List<HomeScreenTaskPM>();
            int count = 200;
            try
            {
                count = Convert.ToInt32(tbCount.Text);
            }
            catch { }


            for (int i = 0; i < count; i++)
            {
                list.Add(new HomeScreenTaskPM()
                {
                    PatientKey = i,
                    TaskStartDateTime = DateTime.Now,
                    TaskStartTime = DateTime.Now.ToString(),
                    TaskStartEnd = DateTime.Now.ToString(),
                    TaskEndDateTime = DateTime.Now.ToString(),
                    FullNameInformal = $"Informal {i + 1}",
                    FullNameWithMRN = $"MRN {i + 1}",
                    CareCoordinator = $"Coordinator {i + 1}",
                    SOCDate = DateTime.Now,
                    Clinician = $"LastVisit {i + 1}",
                    ServiceLineName = $"Line {i + 1}",
                    AdmissionStatusAndDate = DateTime.Now.ToString(),
                    LastVisitDateForCurrentUser = DateTime.Now.ToString(),
                    DocumentDescriptionSortable = $"Doc desc current user {i}",
                    TaskCommentsShort = $"Task comment short {i}",

                    TaskStatus = EncounterStatusType.CoderReview,
                    OrderEntryStatus = OrderStatusType.InProcess,
                    AssessmentColor = "#FFFF0000",
                    Translator = true,
                    AssessmentOverdue = true,
                    CanAttemptTask = true,

                });;
            }

            //_t0 = OpenSilver.Profiler.StartMeasuringTime();
            gridIncomplete.ItemsSource = list;
        }

        private long _t0;
        private void Button_Click2(object sender, RoutedEventArgs e)
        {
            //OpenSilver.Profiler.StopMeasuringTime("Time it takes to execute a loop with 10000 items", _t0);
        }
    }
}
