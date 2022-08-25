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
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Messaging;
using System.ComponentModel.Composition.Hosting;
using Virtuoso.Core.ViewModel;
using Virtuoso.Client.Core;

namespace Virtuoso.Core.Controls
{
    public partial class PatientInfoWindow : ChildWindow
    {
        public PatientInfoWindow()
        {
            InitializeComponent();
            this.DataContext = VirtuosoContainer.Current.GetExport<PatientInfoViewModel>().Value;
        }

        // Offer ESC key support for closing the ChildWindow:
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }
    }
}

