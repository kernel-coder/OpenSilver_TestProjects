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
    public partial class PhysicianFaciltyInfoChildWindow : ChildWindow
    {
        public PhysicianFaciltyInfoChildWindow()
        {
            InitializeComponent();
        }

        public PhysicianFaciltyInfoChildWindow(string title, string text, bool showbuttons)
        {
            InitializeComponent();

            this.Title = title;
            this.Text_Message.Text = text;

            if (!showbuttons)
            {
                this.OKButton.Visibility = Visibility.Collapsed;
                this.CancelButton.Visibility = Visibility.Collapsed;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}