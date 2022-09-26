using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ALY_Button_Style
{
    public partial class CDComboTest : ChildWindow
    {
        public CDComboTest()
        {
            InitializeComponent();
           // cmb1.KeepHiddenInFirstRender = true;
            DataContext = new CDComboTestVM();
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

    public class CDComboTestVM : INotifyPropertyChanged
    {
        public CDComboTestVM()
        {
            for (int i = 1; i <= 100; i++)
            {
                Items.Add($"Item {i}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public List<string> Items { get; } = new List<string>();
    }
}

