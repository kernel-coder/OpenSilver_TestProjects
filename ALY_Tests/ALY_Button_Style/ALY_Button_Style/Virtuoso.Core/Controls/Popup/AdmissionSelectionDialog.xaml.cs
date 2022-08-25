using System;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.ViewModel;
using System.Windows.Input;
using Virtuoso.Server.Data;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Navigation;
using OpenRiaServices.DomainServices.Client;
using System.Windows.Media;
using Virtuoso.Core.Utility;

namespace Virtuoso.Core.Controls
{
    public class AdmissionDialogItem
    {
        public int AdmissionKey { get; set; }
        public int PatientKey { get; set; }

        //Binding="{Binding ServiceLineKey, Converter={StaticResource ServiceLineNameFromKeyConverter}}"
        public int ServiceLineKey { get; set; }

        //Binding="{Binding ReferDateTime, StringFormat='{}{0:MM/dd/yyyy}'}"
        public DateTime? ReferDateTime { get; set; }

        //Binding="{Binding SOCDate, StringFormat='{}{0:MM/dd/yyyy}'}"
        public DateTime? SOCDate { get; set; }

        //Binding="{Binding DischargeDateTime, StringFormat='{}{0:MM/dd/yyyy}'}"
        public DateTime? DischargeDateTime { get; set; }

        //Binding="{Binding AdmissionStatus, Converter={StaticResource CodeLookupConverter}, ConverterParameter=AdmissionStatus.Description}"
        public int AdmissionStatus { get; set; }

        //Binding="{Binding AdmissionID}"
        public string AdmissionID { get; set; }

        //Visibility="{Binding IsNew, Converter={StaticResource VisibilityConverter}}"/>
        public EntityState EntityState { get; set; }

        public bool IsNew { get; set; }

        public bool CanSelect { get; set; }
    }

    public partial class AdmissionSelectionDialog : ChildWindow
    {
        public RelayCommand SelectCommand { get; protected set; }
        private string newURI = null;

        public AdmissionSelectionDialog()
        {
            AdmissionSelectionDialog_Base();
        }

        public AdmissionSelectionDialog(NavigateKey NavigateKeyParm)
        {
            NavigationKey = NavigateKeyParm;
            NavHelper = new NavigationHelper(NavigationKey);
            AdmissionSelectionDialog_Base();
        }

        private void AdmissionSelectionDialog_Base()
        {
            InitializeComponent();
            DataContext = this;
            SelectCommand = new RelayCommand(
                () =>
                {
                    Select_Click(this, new RoutedEventArgs());
                } //, () => SelectedItem != null  //if we have no currently selected item - then do not enable the Edit button!
            );
        }

        public NavigateKey NavigationKey { get; set; }
        NavigationHelper NavHelper;

        public bool AreOtherNewAdmitsOpen(int? PatKeyParm)
        {
            if (NavHelper == null) return true;
            return NavHelper.AreOtherNewAdmissionsOpen(PatKeyParm);
        }

        public void InitAdmissionSelection(Patient patient)
        {
            //string uri;
            bool canAdd = true;
            //var slgList = ServiceLineCache.GetAllActiveUserServiceLineGroupingPlusMe(null).Where(slg => slg.ServiceLineGroupHeader.SequenceNumber == 0);
            // Show the admission if the user has permission to any level, not just the first level.
            var slgList =
                ServiceLineCache
                    .GetAllUserServiceLineGroupingPlusMe(null); // include inactive SLGs for legacy admissions
            List<int> slgKeyList = (from slg in slgList select slg.ServiceLineGroupingKey).Distinct().ToList();
            List<int> slKeyList = (from slg in slgList select slg.ServiceLineKey).Distinct().ToList();

            var allAdmissions = patient.Admission
                .Where(q => q.HistoryKey == null)
                .OrderByDescending(q => q.ReferDateTime)
                //.Select(a => AdmissionToAdmissionDialogItem(a))
                .ToList();

            //List<Admission> l = patient.Admission.Where(q => q.HistoryKey == null).OrderByDescending(q => q.ReferDateTime).ToList();
            if (allAdmissions != null)
            {
                if (allAdmissions.Any() == false) allAdmissions = null;
                else
                    canAdd = slKeyList.Any(sl => !patient.Admission.Any(q => (sl == q.ServiceLineKey)
                                                                             && ((q.AdmissionStatusCode == "A")
                                                                                 || (q.AdmissionStatusCode == "H")
                                                                                 || (q.AdmissionStatusCode == "R")
                                                                                 || (q.AdmissionStatusCode == "M")
                                                                             )
                                                                             && (!q.HistoryKey.HasValue)
                        )
                    );
            }

            List<AdmissionDialogItem> itemsSource = new List<AdmissionDialogItem>();

            if (canAdd && !AreOtherNewAdmitsOpen(patient.PatientKey))
            {
                newAdmission.Visibility = Visibility.Visible;
                //Uri="/MaintenanceAdmission/{tab}/{patient}/{admission}"
                //Tab 1 (SelectedIndex = 0) – Details (Patient)
                //Tab 2 – Admission/Referral
                //Tab 3 – Physicians
                //Tab 4 – Services
                //Tab 5 - Coverages
                //Tab 6 – Authorizations
                //Tab 7 – Order Entry
                //Tab 8 – Communications
                //Tab 9 – Documentation
                //Tab 10 – OASIS
                newURI = NavigationUriBuilder.Instance.GetAdmissionMaintenanceURI(1, patient.PatientKey, 0);
            }

            var accessbileAdmissions = patient.Admission.Where(q => ((slKeyList != null) &&
                                                                     (slKeyList.Contains(q.ServiceLineKey))
                                                                     && ((q.AdmissionGroup.Any() == false) ||
                                                                         (q.AdmissionGroup.Any(ag =>
                                                                             !ag.HistoryKey.HasValue &&
                                                                             slgKeyList.Contains(
                                                                                 ag.ServiceLineGroupingKey))))
                                                                     && (q.HistoryKey == null)))
                .OrderByDescending(q => q.ReferDateTime)
                .ThenBy(a => ServiceLineCache.GetNameFromServiceLineKey(a.ServiceLineKey))
                .ThenByDescending(a => a.ReferDateTime)
                .ToList();

            accessbileAdmissions.ForEach(a => itemsSource.Add(AdmissionToAdmissionDialogItem(a, true)));

            var NewAdmit = NavHelper.GetEntityObject(Constants.APPLICATION_ADMISSION, 0, true) as Admission;
            if (NewAdmit != null && NewAdmit.PatientKey == patient.PatientKey)
            {
                itemsSource.Add(AdmissionToAdmissionDialogItem(NewAdmit, true));
            }

            this.Title = "Select admission for " + patient.FullNameInformal;
            admissionListBox.ItemsSource = itemsSource; // l;

            if (allAdmissions != null)
            {
                if (allAdmissions.Any() == true)
                {
                    admissionListBox.SelectedIndex = 0;
                }
            }
        }

        private static AdmissionDialogItem AdmissionToAdmissionDialogItem(Admission a, bool canSelect = false)
        {
            return new AdmissionDialogItem()
            {
                AdmissionKey = a.AdmissionKey,
                PatientKey = a.PatientKey,
                AdmissionID = a.AdmissionID,
                AdmissionStatus = a.AdmissionStatus,
                CanSelect = canSelect,
                //FontStyle=FontStyles.Italic,        //"Italic",
                //Foreground="DarkGray",// Colors.DarkGray    ,//"DarkSlateGray",
                DischargeDateTime = a.DischargeDateTime,
                ReferDateTime = a.ReferDateTime,
                ServiceLineKey = a.ServiceLineKey,
                SOCDate = a.SOCDate,
                EntityState = a.EntityState,
                IsNew = a.IsNew
            };
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
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

        private void newAdmission_Click(object sender, RoutedEventArgs e)
        {
            Uri u = new Uri(newURI, UriKind.Relative);
            Messenger.Default.Send<Uri>(u, "NavigationRequest");
            this.DialogResult = true;
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (admissionListBox.SelectedItem == null) return;
            var admission = admissionListBox.SelectedItem as AdmissionDialogItem; // Admission;
            if (admission == null) return;
            if (admission.CanSelect == false) return;
            string uri =
                NavigationUriBuilder.Instance.GetAdmissionMaintenanceURI(1, admission.PatientKey,
                    admission.AdmissionKey);
            if (String.IsNullOrEmpty(uri) == false)
            {
                Uri u = new Uri(uri, UriKind.Relative);
                Messenger.Default.Send<Uri>(u, "NavigationRequest");
            }

            this.DialogResult = true;
        }
    }

    public class AdmissionSelectionItem
    {
        public string AdmissionStatusText { get; set; }
        public string URI { get; set; }
    }
}