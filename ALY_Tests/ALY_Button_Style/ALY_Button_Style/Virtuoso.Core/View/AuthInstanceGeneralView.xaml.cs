﻿using GalaSoft.MvvmLight;
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
using Virtuoso.Core.ViewModel;

namespace Virtuoso.Core.View
{
    public partial class AuthInstanceGeneralView : UserControl, ICleanup
    {
        public AuthInstanceGeneralView()
        {
            InitializeComponent();
            this.Unloaded += AuthInstanceGeneralView_Unloaded;
        }

        private void AuthInstanceGeneralView_Unloaded(object sender, RoutedEventArgs e)
        {
            Cleanup();
        }

        public void Cleanup()
        {
            this.receivedBySmartCombo.Cleanup();
        }

        //private void UserControl_Loaded(object sender, RoutedEventArgs e)
        //{
        //    var dc = this.DataContext as AuthDetailGeneralViewModel;
        //    if (dc != null)
        //    {
        //        dc.SetViewDependencyObject(this);
        //    }
        //}
    }
}