using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ServiceModel.Security;
using System.Windows.Threading;

namespace ALY_Button_Style
{
    public partial class MainPage : UserControl
    {
        private DispatcherTimer _timer;
        public MainPage()
        {
            this.InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            storyTest.Begin();
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            System.Diagnostics.Debug.WriteLine($"UserListRoot Max Width = {UserListRoot.MaxWidth}");
        }
    }
}
