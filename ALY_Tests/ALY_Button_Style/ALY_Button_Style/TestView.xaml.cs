using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace ALY_Button_Style
{
    public partial class TestView : UserControl
    {
        public TestView()
        {
            this.InitializeComponent();
        }

        private int ItemsToAdd = 1;
        private int[] Steps = { 1, 3, 5, 7, 10, 15, 20, 30, 40, 55, 80 };
        private int StepIndex = 0;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (StepIndex >= Steps.Length) return;

            ItemsToAdd = Steps[StepIndex];
            StepIndex++;

            gridContent.Children.Clear();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            ListBox lb = new ListBox(); 
            for(int i = 1; i <= ItemsToAdd; i++)
            {
                lb.Items.Add("Item " +  i.ToString());
            }
            //gridContent.Children.Add(new ListTimePickerPopup());
            var t1 = watch.Elapsed.TotalSeconds;
            gridContent.Children.Add(lb);
            var t2 = watch.Elapsed.TotalSeconds;
            watch.Stop();
            System.Diagnostics.Debug.WriteLine($"Measure {ItemsToAdd}: {t1}, {t2}");
        }
    }
}
