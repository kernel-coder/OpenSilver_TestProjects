using GalaSoft.MvvmLight;
using System;
using System.Windows;
using System.Windows.Controls;
using Virtuoso.Core.ViewModel;

namespace Virtuoso.Core.Controls
{
    public partial class HospiceFindingsPopup : UserControl, ICleanup
    {
        public HospiceFindingsPopup()
        {
            InitializeComponent();
        }

        public virtual void Cleanup()
        {
        }
    }
}