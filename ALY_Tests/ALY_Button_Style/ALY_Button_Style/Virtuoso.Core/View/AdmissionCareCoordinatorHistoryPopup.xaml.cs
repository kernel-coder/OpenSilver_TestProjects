using GalaSoft.MvvmLight;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Virtuoso.Core.View
{
    public partial class AdmissionCareCoordinatorHistoryPopup : UserControl, ICleanup
    {
        public AdmissionCareCoordinatorHistoryPopup()
        {
            InitializeComponent();
        }

        public virtual void Cleanup()
        {
        }
    }
}