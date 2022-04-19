using System;
using System.Collections.Generic;
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

            //for (int i = 0; i < 30; i++)
            //{
            //    searchItemComboBox.Items.Add($"Item {i + 1}");
            //}

            //searchItemComboBox.SelectedIndex = 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var list = new List<CaseLoadPM>();
            for (int i = 0; i < 200; i++)
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

            gridTest.ItemsSource = list;
        }
    }
}
