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
    public partial class HISPromptForRFA : ChildWindow
    {
        public HISPromptForRFA()
        {
            HISPromptForRFASetup();
        }

        private Admission _Admission = null;

        public HISPromptForRFA(Admission pAdmission)
        {
            _Admission = pAdmission;
            HISPromptForRFASetup();
        }

        private void HISPromptForRFASetup()
        {
            InitializeComponent();
            rfa01.IsChecked = true;
            SetVersionBlirb();
            SetAdmitStatusBlirb();
        }

        private string _RFA = "01";

        public string RFA
        {
            get { return _RFA; }
        }

        public DateTime M0090
        {
            get { return DateTime.Today.Date; }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidRFA() == false) return;
            this.DialogResult = true;
        }

        private string CR = char.ToString('\r');

        private bool ValidRFA()
        {
            if (string.IsNullOrWhiteSpace(RFA)) return false;
            string errorMessage = null;
            if ((_Admission.AdmissionWasAdmitted == false) && (RFA == "01"))
            {
                errorMessage =
                    "ALERT:  Responses to items on the HIS Admission should be based on data that has been documented in the clinical record." +
                    CR +
                    "If the Initial Nursing Assessment can not be initiated, enter a dash (-) in item A0245 to indicate that no Nursing " +
                    CR +
                    "Assessment will be completed for this admission.  Verify there is documentation in the clinical record as to the reason." +
                    CR + CR +
                    "The HIS Admission cannot be opened until the patient has been admitted." + CR;
            }
            else if ((_Admission.AdmissionWasDischarged == false) && (RFA == "09"))
            {
                errorMessage =
                    "Responses to items on the HIS Discharge should be based on discharge data that has been documented in the clinical record." +
                    CR + "The HIS Discharge cannot be opened until the patient has been discharged." + CR;
            }

            if (string.IsNullOrWhiteSpace(errorMessage)) return true;

            NavigateCloseDialog d = new NavigateCloseDialog();
            if (d == null) return true;

            d.ErrorMessage = errorMessage;
            d.Width = double.NaN;
            d.Height = double.NaN;
            d.ErrorQuestion = null;
            d.Title = ((RFA == "01") ? "Patient has not been admitted" : "Patient has not been discharged");
            d.HasCloseButton = false;
            d.OKLabel = "OK";
            d.NoVisible = false;
            d.Show();
            return false;
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
            SetVersionBlirb();
        }

        private void SetVersionBlirb()
        {
            OasisVersion ov =
                OasisCache.GetOasisVersionBySYSCDandEffectiveDate("HOSPICE", _Admission.HISTargetDate(_RFA));
            versionBlirb.ParagraphText = string.Format("( HIS Version {0}, Item Set {1} )",
                (((ov == null) || (ov.VersionCD2 == null)) ? "?" : ov.VersionCD2.Trim()),
                (((ov == null) || (ov.VersionCD1 == null)) ? "?" : ov.VersionCD1.Trim()));
        }

        private void SetAdmitStatusBlirb()
        {
            string blirb = "Patient not yet admitted";
            if (_Admission.AdmissionWasAdmitted == true)
            {
                if (_Admission.AdmissionWasDischarged)
                    blirb = "Patient Admitted on " + ((DateTime)_Admission.AdmitDateTime).Date.ToShortDateString() +
                            ", discharged on " + ((DateTime)_Admission.DischargeDateTime).Date.ToShortDateString();
                else
                    blirb = "Patient Admitted on " + ((DateTime)_Admission.AdmitDateTime).Date.ToShortDateString() +
                            ", not yet discharged";
            }

            admitStatusBlirb.ParagraphText = blirb;
        }
    }
}