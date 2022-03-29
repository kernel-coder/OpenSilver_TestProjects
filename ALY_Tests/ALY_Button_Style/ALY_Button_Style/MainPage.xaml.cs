using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ALY_Button_Style
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            this.InitializeComponent();

            //userListBox.ItemsSource = new List<object> { new object(), new object() };
            // Enter construction logic here...

            SizeChanged += MainPage_SizeChanged;
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 0 &&  e.NewSize.Width != double.NaN)
            {
                //widthKeyFrame.Value = e.NewSize.Width;
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            ///widthKeyFrame.Value = LayoutRoot.ActualWidth;
            storyTest.Begin();
        }
    }
}
