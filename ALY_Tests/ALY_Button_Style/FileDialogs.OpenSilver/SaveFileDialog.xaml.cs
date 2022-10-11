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
    public class FilterItem
    {
        public FilterItem(string name, string extensions)
        {
            Name = name;
            var parts = extensions.Split(';');
            foreach (var part in parts)
            {
                var idx = part.LastIndexOf('.');
                Extensions.Add(part.Substring(idx + 1));
            }
        }
        public string Name { get; private set; }
        public List<string> Extensions { get; }  = new List<string>();

        public bool ContainsExt(string ext)
        {
            foreach(var e in Extensions)
            {
                if (e.ToLower() == ext.ToLower())
                {
                    return true;
                }
            }
            return true;
        }
    }

    public partial class SaveFileDialog : ChildWindow
    {
        public event EventHandler Accept;
        public event EventHandler Cancel;

        
        public SaveFileDialog()
        {
            this.InitializeComponent();
            Loaded += SaveFileDialogEx_Loaded;
        }

        private List<FilterItem> _filters = new List<FilterItem>();
        private void SaveFileDialogEx_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
            tbFilename.Text = DefaultFileName;
            var parts = Filter.Split('|');
            _filters.Clear();
            int currentIndex = -1;
            for(int i = 0; i < parts.Length; i += 2)
            {
                var filter = new FilterItem(parts[i], parts[i + 1]);
                _filters.Add(filter);
                if(filter.ContainsExt(DefaultExt)) currentIndex = i;
            }
            cmbTypes.ItemsSource = _filters;
            cmbTypes.SelectedIndex = currentIndex;
        }
        public string DefaultFileName { get; set; }
        public string DefaultExt { get; set; }

        public string SafeFileName { get; private set; }
        public string Filter { get; set; }
        public int FilterIndex { get; set; } = 0;

        private void Button_CancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
            Cancel?.Invoke(this, new EventArgs());
        }

        private void Button_SaveClick(object sender, RoutedEventArgs e)
        {
            if((cmbTypes.SelectedIndex < 0 && string.IsNullOrEmpty(DefaultExt)) || string.IsNullOrEmpty(tbFilename.Text) || string.IsNullOrEmpty(tbFilename.Text.Trim())) return;            
           
            SafeFileName = tbFilename.Text.Trim();
            string currentExt = string.Empty;
            if (SafeFileName.Contains('.'))
            {
                var idx = SafeFileName.LastIndexOf('.');
                currentExt = SafeFileName.Substring(idx + 1);
                SafeFileName = SafeFileName.Remove(idx);

                if (cmbTypes.SelectedIndex >= 0)
                {
                    var filter = _filters[cmbTypes.SelectedIndex];
                    if (!filter.ContainsExt(currentExt))
                    {
                        currentExt = filter.Extensions.FirstOrDefault();
                    }
                }
                else
                {                    
                    currentExt = DefaultExt;
                }                
            }
            else
            {
                if (cmbTypes.SelectedIndex >= 0)
                {
                    var filter = _filters[cmbTypes.SelectedIndex];
                    currentExt = filter.Extensions.FirstOrDefault();                    
                }
                else
                {
                    currentExt = DefaultExt;
                }
            }

            SafeFileName += "." + currentExt;


            this.DialogResult = true;
            this.Close();
            Accept?.Invoke(this, new EventArgs());
        }

        public async System.Threading.Tasks.Task<bool> ShowDialog()
        {
            await this.ShowAndWait();
            return DialogResult != null && DialogResult.HasValue ? DialogResult.Value : false;
        }

        public void SaveTextToFile(string text, string filename)
        {
            if(!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(filename))
            {
                OpenSilver.Interop.ExecuteJavaScript(@"
                    var blob = new Blob([$0], { type: ""text/plain;charset=utf-8""});
                    saveAs(blob, $1)", text, filename);
            }
        }

        public static void SaveContentToFile(object content, string filename, string contentType = "text/plain;charset=utf-8")
        {
            if (content != null && !string.IsNullOrEmpty(filename))
            {
                string javaSript = @"var blob = new Blob([$0], { type: """;
                javaSript += contentType;
                javaSript += @"""}); saveAs(blob, $1)";
                OpenSilver.Interop.ExecuteJavaScript(javaSript, content, filename);
            }
        }

        public static void SaveBase64Content(string content, string filename, string contentType = "text/plain;charset=utf-8")
        {
            if (content != null && !string.IsNullOrEmpty(filename))
            {
                string javaSript = @"var raw = window.atob($0);
                    var rawLength = raw.length;
                    uInt8Array = new Uint8Array(rawLength);
                    for (let i = 0; i < rawLength; ++i) {
                        uInt8Array[i] = raw.charCodeAt(i);
                    }";
                javaSript += @"var blob = new Blob([uInt8Array], { type: """;
                javaSript += contentType;
                javaSript += @"""}); saveAs(blob, $1)";
                OpenSilver.Interop.ExecuteJavaScript(javaSript, content, filename);
            }
        }
    }
}
