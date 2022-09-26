using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Virtuoso.Core.Controls;
using Virtuoso.Core.View;
using Virtuoso.Home.V2.Controls;

namespace ALY_Button_Style
{
    public partial class TestView : UserControl
    {
        public TestView()
        {
            this.InitializeComponent();

            //rtb.ParagraphText = "A RichTextBox with <b>initial content</b> in it.";

            //for (int i = 0; i < 30; i++)
            //{
            //    comboMulti.Items.Add($"Item {i + 1}");

            //}
        }

        private int ItemsToAdd = 1;
        private int[] Steps = { 1, 3, 5, 7, 10, 15, 20, 30, 40, 55, 80 };
        private int StepIndex = 0;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //ComboItemsTestDlg dlg = new ComboItemsTestDlg();
            //dlg.IsModal = true;
            //dlg.Show();
            //return;
            //if (StepIndex >= Steps.Length) return;

            //ItemsToAdd = Steps[StepIndex];
            //StepIndex++;

            //gridContent.Children.Clear();
            //Stopwatch watch = new Stopwatch();
            //watch.Start();
            //ListBox lb = new ListBox(); 
            //for(int i = 1; i <= ItemsToAdd; i++)
            //{
            //    lb.Items.Add("Item " +  i.ToString());
            //}
            ////gridContent.Children.Add(new ListTimePickerPopup());
            //var t1 = watch.Elapsed.TotalSeconds;
            //gridContent.Children.Add(lb);
            //var t2 = watch.Elapsed.TotalSeconds;
            //watch.Stop();
            //System.Diagnostics.Debug.WriteLine($"Measure {ItemsToAdd}: {t1}, {t2}");
        }

        private async void Button_Click2(object sender, RoutedEventArgs e)
        {
            var dlg = new FileDialogs.SaveFileDialogEx();
            dlg.Title = "Save as dialog";
            dlg.Filter = "Comma-delimited files (*.csv)|*.csv|All files (*.*)|*.*";
            dlg.DefaultExt = "csv";
            dlg.DefaultFileName = "Def Filename";
            dlg.Accept += (s, ee) =>
            {
                dlg.SaveTextToFile("This is the content of the file", dlg.SafeFilename);
            };
            dlg.ShowDialog();

            //OpenSilver.Profiler.StopMeasuringTime("Time it takes to execute a loop with 10000 items", _t0);
            //var window = new CDComboTest();
            //window.Show();
            return;
            var http = new HttpClient();
            var pdfResponse = await http.GetAsync("http://localhost:55593/test.pdf");
            var buffer = await pdfResponse.Content.ReadAsByteArrayAsync();
            DownloadFile(buffer, "testcsv.pdf", true, "application/pdf", true);
        }


        private async void Button_Click3(object sender, RoutedEventArgs e)
        {
            //OpenSilver.Profiler.StopMeasuringTime("Time it takes to execute a loop with 10000 items", _t0);
            var window = new DynamicFormGraph(null, null, "Temperature");
            window.Show();
        }

        private void Button_Click4(object sender, RoutedEventArgs e)
        {
            ShowHelpDialog(Virtuoso.Server.Data.PatientInfection.POAHelp, "Present on Admission (POA)");
        }

        private void ShowHelpDialog(Paragraph templateContent, string title)
        {
            var helpDialog = new HelpPopupDialog(templateContent, title);
            helpDialog.Show();
        }

        private static void DownloadFile(byte[] data, string filename, bool download = true, string fileType = null, bool openInATab = false)
        {
            const string JS_DownloadFile = @"
                    document.FILE_Download = function(wasmArray) {
                        const dataPtr = Blazor.platform.getArrayEntryPtr(wasmArray, 0, 4);
                        const length = Blazor.platform.getArrayLength(wasmArray);
                        let data = new Uint8Array(Module.HEAP8.buffer, dataPtr, length * 4);
                        var blob;
                        if ($2) blob = new Blob([data], { type: $2});
                        else  blob = new Blob([data]);
                        let fileURL = URL.createObjectURL(blob);
                        if ($1) {
                            const link = document.createElement('a');
                            link.href = fileURL;
                            link.setAttribute('download', $0);
                            link.click();
                            link.remove();
                        }
                        if ($3) window.open(fileURL);
                        return 0;
                    }";
            OpenSilver.Interop.ExecuteJavaScript(JS_DownloadFile, filename, download, fileType, openInATab);
            //DotNetForHtml5.Core.INTERNAL_Simulator.JavaScriptExecutionHandler.InvokeUnmarshalled<byte[], object>("document.FILE_Download", data);
        }
    }
}
