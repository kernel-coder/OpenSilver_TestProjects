using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Virtuoso.Client.Core.Controls
{
    public partial class NotifyContent : UserControl, INotifyPropertyChanged
    {
        public NotifyContent()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                this.DataContext = this;
            };
        }
        public string DisplayText
        {
            get { return ((string)(base.GetValue(Virtuoso.Client.Core.Controls.NotifyContent.DisplayTextProperty))); }
            set { base.SetValue(Virtuoso.Client.Core.Controls.NotifyContent.DisplayTextProperty, value); }
        }
        public static DependencyProperty DisplayTextProperty =
          DependencyProperty.Register("DisplayText", typeof(string), typeof(Virtuoso.Client.Core.Controls.NotifyContent), new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Client.Core.Controls.NotifyContent)o).SetDisplayText();
          }));

        public void SetDisplayText()
        {
            displayText = DisplayText;
            txtRichText.Visibility = Visibility.Collapsed;
            txtText.Visibility = Visibility.Collapsed;
            try
            {
                txtRichText.Xaml = "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph xml:space=\"preserve\">" + DisplayText + "</Paragraph></Section>";
                txtRichText.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
                txtText.Text = DisplayText;
                txtText.Visibility = Visibility.Visible;
            }

            FirePropertyChanged("DisplayText");
        }


        void FirePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        string displayText;
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
