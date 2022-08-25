using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using System;
using Virtuoso.Core.Utility;

namespace Virtuoso.Core.View
{
    public partial class ViewOASISTransmissionContent : ChildWindow
    {
        public int[] a;

        private List<EncounterOasisPotential> _EncounterOasisPotentiallist;

        public List<EncounterOasisPotential> EncounterOasisPotentiallist
        {
            get { return _EncounterOasisPotentiallist; }
            set { _EncounterOasisPotentiallist = value; }
        }

        private List<EncounterOasisPotential> _EncounterOasisPotentialOnHoldlist;

        public List<EncounterOasisPotential> EncounterOasisPotentialOnHoldlist
        {
            get { return _EncounterOasisPotentialOnHoldlist; }
            set { _EncounterOasisPotentialOnHoldlist = value; }
        }

        public ViewOASISTransmissionContent(int[] serviceLineGroupingKeyArray, List<EncounterOasisPotential> eoplist)
        {
            InitializeComponent();

            //Scrub out any ServiceLineGrouping in the eoplist that is not in the serviceLineGroupingKeyArray.
            EncounterOasisPotentiallist = eoplist.Where(eop =>
                    eop.ServiceLineGroupingKey != null &&
                    (serviceLineGroupingKeyArray.Contains((int)eop.ServiceLineGroupingKey)) && (eop.OnHold == false))
                .ToList();
            EncounterOasisPotentialOnHoldlist = eoplist.Where(eop =>
                    eop.ServiceLineGroupingKey != null &&
                    (serviceLineGroupingKeyArray.Contains((int)eop.ServiceLineGroupingKey)) && (eop.OnHold == true))
                .ToList();
            LoadList();
        }

        private void LoadList()
        {
            SurveyGrid.ItemsSource = EncounterOasisPotentiallist;
            OnHoldGrid.ItemsSource = EncounterOasisPotentialOnHoldlist;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void EditSurvey_Click(object sender, RoutedEventArgs e)
        {
            HyperlinkButton h = sender as HyperlinkButton;
            if (sender != null)
            {
                EncounterOasisPotential a = h.Tag as EncounterOasisPotential;
                if (a != null)
                {
                    //in case encounter saved under different form definition than current
                    string uri = NavigationUriBuilder.Instance.GetDynamicFormURI(
                        a.PatientKey,
                        a.AdmissionKey,
                        a.FormKey,
                        a.ServiceTypeKey,
                        a.TaskKey);

                    Messenger.Default.Send<Uri>(new Uri(uri, UriKind.Relative), "NavigationRequest");
                    Close();
                }
            }
        }
    }
}