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

namespace FileDialogs
{
    public partial class SaveFileDialogEx : ChildWindow
    {
        public event EventHandler Accept;
        public event EventHandler Cancel;

        public SaveFileDialogEx()
        {
            this.InitializeComponent();
            Loaded += SaveFileDialogEx_Loaded;
        }

        private List<string> _filters = new List<string>();
        private void SaveFileDialogEx_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
            tbFilename.Text = DefaultFileName;
            var parts = Filter.Split('|');
            var filters = new List<string>();
            int currentIndex = -1;
            for (int i = 0; i < parts.Length; i += 2)
            {
                filters.Add(parts[i]);
                _filters.Add(parts[i + 1]);
                if (parts[i + 1].EndsWith(DefaultExt)) currentIndex = i;
            }
            cmbTypes.ItemsSource = filters;
            cmbTypes.SelectedIndex = currentIndex;
        }
        public string DefaultFileName { get; set; }
        public string DefaultExt { get; set; }

        public string SafeFilename { get; private set; }
        public string Filter { get; set; }

        private void Button_CancelClick(object sender, RoutedEventArgs e)
        {            
            this.DialogResult = false;
            this.Close();
            Cancel?.Invoke(this, new EventArgs());
        }

        private void Button_SaveClick(object sender, RoutedEventArgs e)
        {
            if (cmbTypes.SelectedIndex < 0 || string.IsNullOrEmpty(tbFilename.Text) || string.IsNullOrEmpty(tbFilename.Text.Trim())) return;
            SafeFilename = tbFilename.Text.Trim();
            var filter = _filters[cmbTypes.SelectedIndex];
            filter = filter.Replace("*", "");
            if (!tbFilename.Text.EndsWith(filter) && _filters[cmbTypes.SelectedIndex] != "*.*")
            {
                SafeFilename += filter;
            }            
            this.DialogResult = true;
            this.Close();
            Accept?.Invoke(this, new EventArgs());
        }

        public bool? ShowDialog()
        {
            this.Show();
            return true;
        }

        public void SaveTextToFile(string text, string filename)
        {

            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(filename))
            {
                OpenSilver.Interop.ExecuteJavaScript(@"
                    var blob = new Blob([$0], { type: ""text/plain;charset=utf-8""});
                    saveAs(blob, $1)", text, filename);
            }
        }
    }
}
