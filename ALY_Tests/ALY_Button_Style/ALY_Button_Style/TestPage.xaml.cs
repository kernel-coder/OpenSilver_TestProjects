using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;

namespace ALY_Button_Style
{
    public partial class TestPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public TestPage()
        {
            this.InitializeComponent();
            //cmbTest.CustomLayout = true;
            for(int i = 0; i < 20; i++)
            {
                //ComboItems.Add($"Item {i + 1}");
            }

            this.DataContext = this;
            Loaded += TestPage_Loaded;
        }

        protected void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public List<string> ComboItems { get; } = new List<string>();
        private void TestPage_Loaded(object sender, RoutedEventArgs e)
        {
            var list = new List<HomeScreenTaskPM>();
            int count = 2;

            try
            {
                count = Convert.ToInt32(10);
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

                }); ;
            }

            //_t0 = OpenSilver.Profiler.StartMeasuringTime();
            dg.ItemsSource = list;
            gridIncomplete.ItemsSource = list;
        }
        private void HtmlDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var items = dg.SelectedItems;
            //var indexes = dg.SelectedIndexes;
        }

        private void dg_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //dg.Columns[2].Header = "Hello world";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //dg.SelectedIndex = 3;
            //double aw = cmbTest.ActualWidth;
            //double ah = cmbTest.ActualHeight;
            //Size aS = cmbTest.ActualSize;

        }
    }
}
