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
    }
    public partial class DataGridTest : UserControl
    {
        public DataGridTest()
        {

            try
            {
                this.InitializeComponent();
                var list = new List<CaseLoadPM>();
                for(int i = 0; i < 2; i++)
                {
                    list.Add(new CaseLoadPM()
                    {
                        FullNameInformal = $"Informal {i + 1}",
                        FullNameWithMRN = $"MRN {i + 1}",
                        AdmissionStatusAndDate = DateTime.Now.ToString(),
                        LastVisitDateForCurrentUser = DateTime.Now.ToString()
                    });
                }
                gridTest.ItemsSource = list;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
