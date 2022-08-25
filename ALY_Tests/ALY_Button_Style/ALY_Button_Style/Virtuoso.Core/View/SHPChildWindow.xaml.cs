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

namespace Virtuoso.Core.View
{
    public partial class SHPChildWindow : ChildWindow
    {
        public SHPChildWindow()
        {
            InitializeComponent();
        }

        public SHPChildWindow(string title, string text)
        {
            InitializeComponent();

            this.Title = title;
            this.grid_url.Visibility = Visibility.Collapsed;
            this.Text_Message.Text = text;
        }

        public SHPChildWindow(string title, Uri URL)
        {
            InitializeComponent();

            this.Title = title;
            this.grid_url.Visibility = Visibility.Visible;
            this.hyper_URL.NavigateUri = URL;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void hyper_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void hyper_URL_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}