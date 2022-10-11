using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace FileDialogs
{
    public class OpenFileDialog
    {
        public event EventHandler<FileOpenedEventArgs> FileOpened;
        public event EventHandler FileOpenFinished;
        public event EventHandler FileOpenCanceled;
        private object inputElement;

        private ResultKind _resultKind;
        private string _resultKindStr;

        private List<FileOpenedEventArgs> _files = new List<FileOpenedEventArgs>();
        public List<FileOpenedEventArgs> Files { get { return _files; } }
        public FileOpenedEventArgs File { get { return _files.FirstOrDefault(); } }
        public ResultKind ResultKind
        {
            get { return _resultKind; }
            set
            {
                _resultKind = value;
                _resultKindStr = value.ToString();
            }
        }

        public OpenFileDialog()
        {
            inputElement = CSHTML5.Interop.ExecuteJavaScript(@"
                var el = document.createElement('input');
                el.setAttribute('type', 'file');
                el.setAttribute('id', 'inputId');
                el.style.display = 'none';
                document.body.appendChild(el);");
            ResultKind = ResultKind.Text;

            FileOpened += (s, e) =>
            {
                _files.Add(e);
            };

            FileOpenFinished += (s, e) =>
            {
                _showDialogTask?.SetResult(true);
            };

            FileOpenCanceled += (s, e) =>
            {
                _showDialogTask?.SetResult(false);
            };
        }

        private void SetupListeners()
        {
            Action<object, string> onFileOpened = (result, name) =>
            {
                if (this.FileOpened != null)
                {
                    this.FileOpened(this, new FileOpenedEventArgs(result, name, this.ResultKind));
                }
            };

            Action onFileOpenFinished = () =>
            {
                if (this.FileOpenFinished != null)
                {
                    this.FileOpenFinished(this, new EventArgs());
                }
            };

            Action onFileOpenCanceled = () =>
            {
                if (this.FileOpenCanceled != null)
                {
                    this.FileOpenCanceled(this, new EventArgs());
                }
            };

            // Listen to the "change" property of the "input" element, and call the callback:
            CSHTML5.Interop.ExecuteJavaScript(@"
                $0.addEventListener(""click"", function(e) {
                    document.body.onfocus = function() {
                        document.body.onfocus = null;
                        setTimeout(() => { 
                            if (document.getElementById('inputId').value.length) {
                            }
                            else
                            {
                                var cancelCallback = $3;
                                cancelCallback();
                            }
                            document.getElementById('inputId').remove();
                        }, 1000);
                    }
                });
                $0.addEventListener(""change"", function(e) {
                    if(!e) {
                      e = window.event;
                    }
                    var fullPath = $0.value;
                    var filename = '';
                    if (fullPath) {
                        var startIndex = (fullPath.indexOf('\\') >= 0 ? fullPath.lastIndexOf('\\') : fullPath.lastIndexOf('/'));
                        filename = fullPath.substring(startIndex);
                        if (filename.indexOf('\\') === 0 || filename.indexOf('/') === 0) {
                            filename = filename.substring(1);
                        }
                    }
                    var input = e.target;
                    var reader = new FileReader();

                    function readNext(i) {
                      var file = input.files[i];
                      reader.onload = function() {
                        var callback = $1;
                        var finishCallback = $2;
                        var result = reader.result;
                        callback(result, file.name);

                        if (input.files.length > i + 1)
                        {
                          readNext(i + 1);
                        }
                        else
                        {
                          //document.getElementById('inputId').remove();
                          finishCallback();
                        }
                      };
                      var resultKind = $5;
                      if (resultKind == 'DataURL') {
                        reader.readAsDataURL(file);
                      }
                      else {
                        reader.readAsText(file);
                      }
                      var isRunningInTheSimulator = $4;
                      if (isRunningInTheSimulator) {
                          alert(""The file open dialog is not supported in the Simulator. Please test in the browser instead."");
                      }
                    }
                    readNext(0);
                });", inputElement, onFileOpened, onFileOpenFinished, onFileOpenCanceled, CSHTML5.Interop.IsRunningInTheSimulator_WorkAround, _resultKindStr);
        }

        void SetFilter(string filter)
        {
            if (String.IsNullOrEmpty(filter))
            {
                return;
            }

            string[] splitted = filter.Split('|');
            List<string> itemsKept = new List<string>();
            if (splitted.Length == 1)
            {
                itemsKept.Add(splitted[0]);
            }
            else
            {
                for (int i = 1; i < splitted.Length; i += 2)
                {
                    itemsKept.Add(splitted[i]);
                }
            }
            string filtersInHtml5 = String.Join(",", itemsKept).Replace("*", "").Replace(";", ",");

            // Apply the filter:
            if (!string.IsNullOrWhiteSpace(filtersInHtml5))
            {
                CSHTML5.Interop.ExecuteJavaScript(@"$0.accept = $1", inputElement, filtersInHtml5);
            }
            else
            {
                CSHTML5.Interop.ExecuteJavaScript(@"$0.accept = """"", inputElement);
            }
        }

        private bool _multiselect = false;
        public bool Multiselect
        {
            get { return _multiselect; }
            set
            {
                _multiselect = value;

                if (_multiselect)
                {
                    CSHTML5.Interop.ExecuteJavaScript(@"$0.setAttribute('multiple', 'multiple');", inputElement);
                }
            }
        }

        private string _filter;
        public string Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                SetFilter(_filter);
            }
        }

        private TaskCompletionSource<bool> _showDialogTask;
        public Task<bool> ShowDialog()
        {
            _files.Clear();
            _showDialogTask = new TaskCompletionSource<bool>();

            SetupListeners();
            CSHTML5.Interop.ExecuteJavaScript("document.getElementById('inputId').click();");
            return _showDialogTask.Task;
        }
    }

    public class FileOpenedEventArgs : EventArgs
    {
        /// <summary>
        /// Only available if the property "ResultKind" was set to "Text".
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// Only available if the property "ResultKind" was set to "DataURL".
        /// </summary>
        public readonly string Data;
        public readonly byte[] Buffer;

        public readonly string Name;
        public readonly string Extension;

        public FileOpenedEventArgs(object result, string name, ResultKind resultKind)
        {
            Name = name;
            Extension = System.IO.Path.GetExtension(name.ToLower());

            if (resultKind == ResultKind.Text)
            {
                this.Text = (result ?? "").ToString();
            }
            else if (resultKind == ResultKind.DataURL)
            {
                this.Data = (result ?? "").ToString();
                int startIdx = this.Data.IndexOf(";base64,");
                if(startIdx >= 0)
                {
                    this.Data = this.Data.Remove(0, startIdx + ";base64,".Length);
                    this.Buffer = System.Convert.FromBase64String(this.Data);
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    public enum ResultKind
    {
        Text, DataURL
    }
}
