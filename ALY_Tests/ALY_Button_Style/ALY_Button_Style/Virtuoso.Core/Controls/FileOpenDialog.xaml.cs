using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;

namespace Virtuoso.Core.Controls
{
    public partial class FileOpenDialog : UserControl
    {
        public FileOpenDialog()
        {
            InitializeComponent();
        }

        
        public event EventHandler<FileSelectedEventArgs> Selected = null;


#if OPENSILVER
        private async void btnFileOpen_Click(object sender, RoutedEventArgs e)
        {
            if(OpenFile)
            {
                var dialog = new FileDialogs.OpenFileDialog() { Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*" };

                if(await dialog.ShowDialog() == true)
                {
                    var firstFile = dialog.Files.FirstOrDefault();
                    if(firstFile != null)
                    {
                        // TODO: As we can't have full file path in browser, we have to think of something here?
                        FileName = firstFile.Name;
                        if(Selected != null)
                        {
                            FileSelectedEventArgs eventArgs = new FileSelectedEventArgs();
                            eventArgs.FileName = textFileName.Text;
                            Selected(this, eventArgs);
                        }
                    }
                }
            }
            else
            {
                var dlg = new FileDialogs.SaveFileDialog() {
                    Title = "Save As",
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultFileName = FileName,
                    DefaultExt = ".txt" };
                await dlg.ShowAndWait();
                if(dlg.DialogResult == true && !string.IsNullOrEmpty(dlg.SafeFileName))
                {
                    FileName = dlg.SafeFileName;
                    if(Selected != null)
                    {
                        FileSelectedEventArgs eventArgs = new FileSelectedEventArgs();
                        eventArgs.FileName = textFileName.Text;
                        Selected(this, eventArgs);
                    }
                }
            }

        }
#else
        private void btnFileOpen_Click(object sender, RoutedEventArgs e)
        {
            if(OpenFile)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                /// dialog.OverwritePrompt = false;
                dialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

                if (dialog.ShowDialog().Value)
                {
                    FileInfo fi = dialog.File;
                    FileName = fi.FullName;
                    if(Selected!=null)
                    {
                        FileSelectedEventArgs eventArgs = new FileSelectedEventArgs();
                        eventArgs.FileName = textFileName.Text;
                        Selected(this, eventArgs);
                    }
                }
            }
            else
            {
                SaveFileDialog dialog = new SaveFileDialog();
                /// dialog.OverwritePrompt = false;
                dialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

                if (dialog.ShowDialog().Value)
                {
                    FileName = dialog.SafeFileName;
                    if(Selected!=null)
                    {
                        FileSelectedEventArgs eventArgs = new FileSelectedEventArgs();
                        eventArgs.FileName = textFileName.Text;
                        Selected(this, eventArgs);
                    }
                }
            }
        }
#endif
        public static DependencyProperty FileNameProperty = DependencyProperty.Register("FileName", typeof(object), typeof(FileOpenDialog), null);

        public string FileName
        {
            get 
            { 
                string s = ((string)(base.GetValue(FileOpenDialog.FileNameProperty))); 
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set 
            { 
                base.SetValue(FileNameProperty, value);
                textFileName.Text = value;
            }
        }

        public static DependencyProperty OpenFileProperty = DependencyProperty.Register("OpenFile", typeof(bool), typeof(FileOpenDialog), null);

        public bool OpenFile
        {
            get
            {
                return (bool)base.GetValue(OpenFileProperty);
            }
            set
            {
                base.SetValue(OpenFileProperty, value);
            }
        }

    }

    public class FileSelectedEventArgs : EventArgs
    {
        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
            }
        }
        private string fileName = null;
    }

}
