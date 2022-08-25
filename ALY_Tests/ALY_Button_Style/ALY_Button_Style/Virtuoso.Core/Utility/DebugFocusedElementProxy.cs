#region Usings

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

#endregion

namespace Virtuoso.Core.Utility
{
    // FYI This control is used to help debug focus issues.  It is instantiated in FooterUserControl, but commented out until needed.
    public class DebugFocusedElementProxy : FrameworkElement, INotifyPropertyChanged
    {
        private string _FocusedElementDescription;

        public string FocusedElementDescription
        {
            get { return _FocusedElementDescription; }
            set
            {
                _FocusedElementDescription = value;
                OnNotifyPropertyChanged("FocusedElementDescription");
            }
        }

        public DebugFocusedElementProxy()
        {
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += (o, ea) =>
            {
                var fe = FocusManager.GetFocusedElement();
                if (fe == null)
                {
                    FocusedElementDescription = "None";
                }
                else
                {
                    var element = fe as FrameworkElement;
                    if (string.IsNullOrEmpty(element.Name))
                    {
                        FocusedElementDescription = fe.GetType().Name;
                    }
                    else
                    {
                        FocusedElementDescription = element.Name;
                    }
                }
            };
            timer.Start();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        void OnNotifyPropertyChanged(string nomPropriete)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(nomPropriete));
            }
        }

        #endregion
    }
}