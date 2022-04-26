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

namespace ALY_Button_Style
{
    public partial class CaseLoadPM 
    {
        public string FullNameInformal { get; set; }

        public string FullNameWithMRN { get; set; }

        public string AdmissionStatusAndDate { get; set; }

        public string LastVisitAndClinician { get; set; }

        public string LastVisitDateForCurrentUser { get; set; }

        public int PatientKey { get; set; }

        public int AdmissionKey { get; set; }
        public DateTime SOCDate { get; set; }
        public string CareCoordinator { get; set; }
        public string ServiceLineName { get; set; }

        public bool Exclude { get; set; } = false;
    }

    public partial class DataGridTest : UserControl
    {
        public DataGridTest()
        {
            this.InitializeComponent();
            gridTest.IsAutoHeightOnCustomLayout = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var list = new List<CaseLoadPM>();
            int count = 200;
            try
            {
                count = Convert.ToInt32(tbCount.Text);
            }
            catch { }


            for (int i = 0; i < count; i++)
            {
                list.Add(new CaseLoadPM()
                {
                    FullNameInformal = $"Informal {i + 1}",
                    FullNameWithMRN = $"MRN {i + 1}",
                    CareCoordinator = $"Coordinator {i + 1}",
                    SOCDate = DateTime.Now,
                    LastVisitAndClinician = $"LastVisit {i + 1}",
                    ServiceLineName = $"Line {i + 1}",
                    AdmissionStatusAndDate = DateTime.Now.ToString(),
                    LastVisitDateForCurrentUser = DateTime.Now.ToString()
                });
            }

            var watch = new Stopwatch();
            watch.Start();            
            gridTest.ItemsSource = list;
            Console.WriteLine($"Row count is {count}, time took: {watch.Elapsed.TotalSeconds}");
            Debug.WriteLine($"Row count is {count}, time took: {watch.Elapsed.TotalSeconds}");
            watch.Stop();
        }
    }
}
