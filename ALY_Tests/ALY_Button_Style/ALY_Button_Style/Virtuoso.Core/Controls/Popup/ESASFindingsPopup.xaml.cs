using GalaSoft.MvvmLight;
using System;
using System.Windows;
using System.Windows.Controls;
using Virtuoso.Core.ViewModel;

namespace Virtuoso.Core.Controls
{
    public partial class ESASFindingsPopup : UserControl, ICleanup
    {
        public ESASFindingsPopup()
        {
            InitializeComponent();
        }

        public virtual void Cleanup()
        {
        }
    }
}