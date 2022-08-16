using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OS_SelectionStartTest
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void TextBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            var txt = sender as TextBox;
           this.ListBox1.Items.Insert(0,$"Selection Starts at { txt.SelectionStart}");
        }
    }
}
