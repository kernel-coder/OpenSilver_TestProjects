using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;
using Virtuoso.Core.Converters;

namespace Virtuoso.Core.View
{
    public partial class VaccinesImmunizationsHistory : ChildWindow
    {
        private Patient currentPatient = null;

        // private AdmissionWoundSite currentAdmissionWoundSite = null;
        // private Encounter currentEncounter = null;
        public List<GraphItem> GraphItems = new List<GraphItem>();
        public bool isCancel = false;

        public VaccinesImmunizationsHistory(Patient patient, int encounterKey)
        {
            currentPatient = patient;
            InitializeComponent();

            List<PatientImmunization> piList = ((patient == null) || (patient.PatientImmunization == null))
                ? null
                : patient.PatientImmunization
                    .Where(i => ((i.Inactive == false) && (i.AddedFromEncounterKey != encounterKey) &
                        (string.IsNullOrWhiteSpace(i.ImmunizationCodeDescription) == false)))
                    .OrderBy(i => i.ImmunizationCodeDescription).ThenByDescending(i => i.PatientImmunizationKey)
                    .ToList();
            if ((piList == null) || (piList.Any() == false))
            {
                noHistory.Visibility = Visibility.Visible;
            }
            else
            {
                historyDataGrid.Visibility = Visibility.Visible;
                historyDataGrid.ItemsSource = piList;
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}