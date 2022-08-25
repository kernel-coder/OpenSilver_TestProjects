using GalaSoft.MvvmLight;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Virtuoso.Core.View
{
    public partial class AdmissionReferralNotesPopup : UserControl, ICleanup
    {
        public AdmissionReferralNotesPopup()
        {
            InitializeComponent();
        }

        public virtual void Cleanup()
        {
        }
    }
}