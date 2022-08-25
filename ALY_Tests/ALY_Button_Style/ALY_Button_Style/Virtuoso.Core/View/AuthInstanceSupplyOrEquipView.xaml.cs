using GalaSoft.MvvmLight;
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
    public partial class AuthInstanceSupplyOrEquipView : UserControl, ICleanup
    {
        public Guid ID { get; set; }

        public AuthInstanceSupplyOrEquipView()
        {
            InitializeComponent();
            ID = Guid.NewGuid();
            this.Unloaded += AuthInstanceSupplyOrEquipView_Unloaded;
        }

        private void AuthInstanceSupplyOrEquipView_Unloaded(object sender, RoutedEventArgs e)
        {
            Cleanup();
        }

        public void Cleanup()
        {
            System.Diagnostics.Debug.WriteLine("AuthInstanceSupplyOrEquipView Cleanup - {0}", this.ID);
            receivedBySmartCombo.Cleanup();
        }

        //private void UserControl_Loaded(object sender, RoutedEventArgs e)
        //{
        //    var dc = this.DataContext as AuthDetailSupplyOrEquipViewModel;
        //    if (dc != null)
        //    {
        //        dc.SetViewDependencyObject(this);
        //    }        

        //}
    }
}