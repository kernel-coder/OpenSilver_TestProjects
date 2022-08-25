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

namespace Virtuoso.Core.Controls
{
    public partial class TitleMessageTextBox : ChildWindow
    {
        public string OKButtonLabel { get; set; }
        public string CancelButtonLabel { get; set; }
        public string MbTitle { get; set; }
        public string Message { get; set; }
        public bool OneButton { get; set; }

        public TitleMessageTextBox(string title, string message, string okButtonLabel = "OK",
            string cancelButtonLabel = "Cancel", bool oneButton = false)
        {
            Title = title;
            OKButtonLabel = okButtonLabel;
            CancelButtonLabel = cancelButtonLabel;
            Message = message;
            OneButton = oneButton;
            InitializeComponent();
            DataContext = this;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void OneButton_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}