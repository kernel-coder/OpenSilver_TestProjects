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
using Virtuoso.Client.Infrastructure.Browser;

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
        private IndexedDbFileStorage _storage;
        private const string DgDbKey = "datagrid-sample-data";
        public DataGridTest()
        {
            this.InitializeComponent();
            var cache = new IndexedDbCache();
            _storage = new IndexedDbFileStorage(cache);            
            gridTest.IsAutoHeightOnCustomLayout = true;
            Loaded += DataGridTest_Loaded;
        }

        private double _lastStamp = 0;
        private void LogTimeDiff(string msg, Stopwatch watch, bool isFirst = false)
        {
            double time = isFirst ? watch.Elapsed.TotalSeconds : watch.Elapsed.TotalSeconds - _lastStamp;
            _lastStamp = watch.Elapsed.TotalSeconds;
            msg = msg + $" {time:0.##} s";
            Console.WriteLine(msg);
            Debug.WriteLine(msg);
        }

        private async void DataGridTest_Loaded(object sender, RoutedEventArgs e)
        {
            if (!await _storage.Exists(DgDbKey))
            {
                Button_Click_WRITE(null, null);
            }
        }

        private string GetSizeForString(string str)
        {
            double bc = str.Length * sizeof(Char);

            if (bc < 1024)
            {
                return $"{(int)bc} B";
            }
            else if (bc < 1024 * 1024)
            {
                return $"{bc/1024:0.##} KB";
            }
            else if (bc < 1024 * 1024 * 1024)
            {
                return $"{bc / (1024 * 1024):0.##} MB";
            }

            return $"{(int)bc} B";
        }

        private async void Button_Click_WRITE(object sender, RoutedEventArgs e)
        {
            await _storage.DeleteFile(DgDbKey);
            if (!await _storage.Exists(DgDbKey))
            {
                var watch = new Stopwatch();
                watch.Start();

                try
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
                    LogTimeDiff("Prepartion took", watch, true);
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
                    LogTimeDiff($"Json size {GetSizeForString(json)}, serialization took", watch);
                    await _storage.WriteToFile(DgDbKey, json);
                    LogTimeDiff("Writting took", watch);
                }
                catch { }
                finally
                {
                    watch.Stop();
                }
            }
        }
        private async void Button_Click_READ(object sender, RoutedEventArgs e)
        {
            var watch = new Stopwatch();
            watch.Start();

            try
            {
                var json = await _storage.Read(DgDbKey);
                if (string.IsNullOrEmpty(json)) return;

                LogTimeDiff("Reading took", watch, true);
                var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CaseLoadPM>>(json);
                LogTimeDiff($"Json size {GetSizeForString(json)}, serialization took", watch);

                gridTest.ItemsSource = list;
                LogTimeDiff($"Row count is {list.Count}, time took", watch);

            }
            catch { }
            finally
            {
                watch.Stop();
            }
        }
    }
}
