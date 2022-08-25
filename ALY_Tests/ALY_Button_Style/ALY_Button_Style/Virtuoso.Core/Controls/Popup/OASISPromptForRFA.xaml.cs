using System;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.ViewModel;
using System.Windows.Input;
using Virtuoso.Server.Data;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.Cache;

namespace Virtuoso.Core.Controls
{
    public partial class OASISPromptForRFA : ChildWindow
    {
        public OASISPromptForRFA()
        {
            InitializeComponent();
            m0090Date.DateObject = DateTime.Today.Date;
            rfa01.IsChecked = true;
            SetVersionBlirb();
        }

        private string _RFA = "01";

        public string RFA
        {
            get { return _RFA; }
        }

        public DateTime M0090
        {
            get
            {
                DateTime? retDate = m0090Date.DateObject;
                if ((retDate == null) || (retDate == DateTime.MinValue)) return DateTime.Today.Date;
                return ((DateTime)m0090Date.DateObject).Date;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
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

        private void rfa_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton r = sender as RadioButton;
            if (r == null) return;
            _RFA = r.Tag as string;
        }

        private void m0090Date_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            SetVersionBlirb();
        }

        private void SetVersionBlirb()
        {
            OasisVersion ov = OasisCache.GetOasisVersionBySYSCDandEffectiveDate("OASIS", M0090);
            versionBlirb.ParagraphText = string.Format("( OASIS Version {0}, Item Set {1} )",
                (((ov == null) || (ov.VersionCD2 == null)) ? "?" : ov.VersionCD2.Trim()),
                (((ov == null) || (ov.VersionCD1 == null)) ? "?" : ov.VersionCD1.Trim()));
        }
    }
}