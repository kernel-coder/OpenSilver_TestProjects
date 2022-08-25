using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;
using System.Collections.Generic;
using System.Windows.Data;
using System.ComponentModel;
using System.Linq;
using System;
using Virtuoso.Core.Services;

namespace Virtuoso.Core.View
{
    public partial class PatientLabSelectionDialog : ChildWindow
    {
        private AdmissionCommunication admissionCommunication = null;
        private Admission admission = null;
        private IPatientService model = null;
        private List<PatientLab> AllowablePatientLabs;
        private CollectionViewSource _PatientLabCollectionView;

        private ICollectionView PatientLabCollectionView
        {
            get { return (_PatientLabCollectionView == null) ? null : _PatientLabCollectionView.View; }
        }

        public PatientLabSelectionDialog(IPatientService m, AdmissionCommunication ac, Admission a)
        {
            model = m;
            admissionCommunication = ac;
            admission = a;
            InitializeComponent();
            if (model == null) return;
            if (admissionCommunication == null) return;
            if (admission == null) return;
            if (admission.Patient == null) return;
            this.Title = admission.Patient.FullNameInformal + " Labs";
            SetupPatientLabCollectionView();
            if (AllowablePatientLabs.Any() == false)
            {
                labListBox.Visibility = Visibility.Collapsed;
                txtBlockNoLabs.Visibility = Visibility.Visible;
                okButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                labListBox.Visibility = Visibility.Visible;
                txtBlockNoLabs.Visibility = Visibility.Collapsed;
                this.labListBox.ItemsSource = PatientLabCollectionView;
                okButton.Visibility = Visibility.Visible;

                // populate selected items
                if ((labListBox.ItemsSource != null) && (admissionCommunication.AdmissionCommunicationLab != null))
                {
                    foreach (PatientLab pl in labListBox.ItemsSource)
                    {
                        AdmissionCommunicationLab acl = admissionCommunication.AdmissionCommunicationLab
                            .Where(c => c.PatientLabKey == pl.PatientLabKey).FirstOrDefault();

                        if (acl != null)
                        {
                            pl.IsSelected = true;
                            labListBox.SelectedItems.Add(pl);
                        }
                    }
                }
            }
        }

        private void SetupPatientLabCollectionView()
        {
            AllowablePatientLabs = new List<PatientLab>();
            if (admissionCommunication == null) return;
            DateTime completedDate = (admissionCommunication.CompletedDatePart == null)
                ? DateTime.Today
                : (DateTime)admissionCommunication.CompletedDatePart;

            if (admission.Patient.PatientLab == null) return;
            if (admission.Patient.PatientLab.Any() == false) return;
            AllowablePatientLabs = admission.Patient.PatientLab
                .Where(p => (p.TestDate.GetValueOrDefault().Date >= completedDate.AddDays(-61).Date) &&
                            (p.Result != null)).OrderByDescending(p => p.TestDate).ToList();
            if (AllowablePatientLabs == null)
            {
                AllowablePatientLabs = new List<PatientLab>();
                return;
            }

            foreach (PatientLab pl in AllowablePatientLabs)
            {
                pl.IsSelected = false;
            }

            _PatientLabCollectionView = new CollectionViewSource();
            _PatientLabCollectionView.SortDescriptions.Add(
                new SortDescription("TestDate", ListSortDirection.Descending));
            _PatientLabCollectionView.Source = AllowablePatientLabs;

            _PatientLabCollectionView.Filter += (s, args) =>
            {
                args.Accepted = true;
                return;
            };
            PatientLabCollectionView.Refresh();
            _PatientLabCollectionView.View.MoveCurrentToFirst();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (admissionCommunication.AdmissionCommunicationLab != null)
            {
                foreach (AdmissionCommunicationLab acl in admissionCommunication.AdmissionCommunicationLab.Reverse())
                    model.Remove(acl);
            }

            if (labListBox.SelectedItems != null)
            {
                foreach (PatientLab pl in labListBox.SelectedItems)
                {
                    AdmissionCommunicationLab acl = new AdmissionCommunicationLab();
                    acl.PatientLabKey = pl.PatientLabKey;
                    admissionCommunication.AdmissionCommunicationLab.Add(acl);
                    admission.AdmissionCommunicationLab.Add(acl);
                }
            }

            admissionCommunication.SetupAdmissionCommunicationLabCollectionView(admission);
            this.DialogResult = true;
        }

        private void labListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null)
                foreach (PatientLab pl in e.AddedItems)
                    pl.IsSelected = true;
            if (e.RemovedItems != null)
                foreach (PatientLab pl in e.RemovedItems)
                    pl.IsSelected = false;
        }
    }
}