using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Interface;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Controls
{
    public class CodeLookupMultiItem
    {
        public string Description { get; set; }
        public string Code { get; set; }
        public string Key { get; set; }
    }
    public class codeLookupMulti : System.Windows.Controls.ComboBox, ICustomCtrlContentPresenter, ICleanup
    {
        public event EventHandler SelectedTextChanged;
        public event EventHandler ListDropDownClosed;
        public event EventHandler ListDropDownOpened;
        private List<CodeLookupMultiItem> _itemsSource = null;
        private List<CodeLookupMultiItem> _codeDescriptions = new List<CodeLookupMultiItem>();
        private bool _loaded = false;
        private TextBlock controlTextBlock = null;
        private ScrollViewer scrollViewer = null;
        private TextBox controlOtherTextBox = null;
        private StackPanel controlOtherStackPanel = null;
        private StackPanel controlOtherStackPanel2 = null;
        private ListBox controlListBox = null;
        private Popup controlPopup = null;
        private Button controlCloseButton = null;
        private string OTHER = "Other";
        private DispatcherTimer _doubleClickTimer;
        private bool isDoubleClickClose = false;

        public codeLookupMulti()
        {
            this.SingleSelect = false;
            this.IsTabStopCustom = false;
            SelectedValuesPath = null;
            this.Loaded += new RoutedEventHandler(codeLookupMulti_Loaded);
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreCodeLookupMultiStyle"]; }
            catch { }
            _doubleClickTimer = new DispatcherTimer();
            _doubleClickTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _doubleClickTimer.Tick += new EventHandler(DoubleClick_Timer);
            this.DropDownOpened += new EventHandler(codeLookupMulti_DropDownOpened);
            this.DropDownClosed += new EventHandler(codeLookupMulti_DropDownClosed);
        }
        public void Cleanup()
        {
            _doubleClickTimer.Tick -= DoubleClick_Timer;
            this.DropDownOpened -= codeLookupMulti_DropDownOpened;
            this.DropDownClosed -= codeLookupMulti_DropDownClosed;
            this.Loaded -= codeLookupMulti_Loaded;
            if (_codeDescriptions != null) _codeDescriptions.Clear();
            _codeDescriptions = null;
            if (_itemsSource != null) _itemsSource.Clear();
            _itemsSource = null;
            if (_CurrentFilteredPatientMedication != null) _CurrentFilteredPatientMedication.Source = null;
        }
        public static DependencyProperty IsTabStopCustomProperty =
           DependencyProperty.Register("IsTabStopCustom", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupMulti), null);
        public bool IsTabStopCustom
        {
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.codeLookupMulti.IsTabStopCustomProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.codeLookupMulti.IsTabStopCustomProperty, value); }
        }

        private void DoubleClick_Timer(object sender, EventArgs e)
        {
            _doubleClickTimer.Stop();
        }
        private void controlListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_doubleClickTimer.IsEnabled)
            {
                // Perform doubleclick - which closes the multiselect combobox dropdown
                isDoubleClickClose = true;
                this.IsDropDownOpen = false;
                _doubleClickTimer.Stop();
            }
            else
            {
                _doubleClickTimer.Start();
            }
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            controlTextBlock = (TextBlock)GetTemplateChild("ControlTextBlock");
            string s = SelectedValues as string;
            if (controlTextBlock != null) { controlTextBlock.Text = (s == null) ? "" : s.ToString(); } 
            controlListBox = (ListBox)GetTemplateChild("ControlListBox");
            if (controlListBox != null)
            {
                // Set/reset SelectionChanged event
                try { controlListBox.SelectionChanged -= controlListBox_SelectionChanged; }
                catch { }
                controlListBox.SelectionChanged += new SelectionChangedEventHandler(controlListBox_SelectionChanged);
                // Set/reset MouseLeftButtonUp event
                try { controlListBox.MouseLeftButtonUp -= controlListBox_MouseLeftButtonUp; }
                catch { }
                controlListBox.MouseLeftButtonUp += new MouseButtonEventHandler(controlListBox_MouseLeftButtonUp);
            }
            controlCloseButton = (Button)GetTemplateChild("ControlCloseButton");
            if (controlCloseButton != null)
            {
                // Set/reset Click event
                try { controlCloseButton.Click -= controlCloseButton_Click; }
                catch { }
                controlCloseButton.Click += new RoutedEventHandler(controlCloseButton_Click);
            }
            controlPopup = (Popup)GetTemplateChild("Popup");
            scrollViewer = (ScrollViewer)GetTemplateChild("ScrollViewer");
            controlOtherTextBox = (TextBox)GetTemplateChild("ControlOtherTextBox");
            if (controlOtherTextBox != null)
            {
                try { controlOtherTextBox.KeyUp -= controlOtherTextBox_KeyUp; }
                catch { }
                controlOtherTextBox.KeyUp += new KeyEventHandler(controlOtherTextBox_KeyUp);
                if ((MaxLength != null) && (MaxLength > 0)) controlOtherTextBox.MaxLength = (int)MaxLength;
            }
            controlOtherStackPanel = (StackPanel)GetTemplateChild("ControlOtherStackPanel");
            controlOtherStackPanel2 = (StackPanel)GetTemplateChild("ControlOtherStackPanel2");
        }

        void codeLookupMulti_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_loaded) LoadCodes();
            if (SingleSelect == false) return;
            if (controlListBox == null) this.ApplyTemplate();
            if (controlListBox != null) controlListBox.SelectionMode = SelectionMode.Single; 
        }

        private void LoadCodes()
        {
            _loaded = true;
            if (string.IsNullOrWhiteSpace(CodeType) == false)
            {
                if (CodeType.Trim().StartsWith("---"))
                {
                    if (CodeType.Trim().ToLower() == "---patientactivediagnosis")
                    {
                        LoadCodes_PatientActiveDiagnosis();
                    }
                    if (CodeType.Trim().ToLower() == "---patientmedications")
                    {
                        LoadCodes_PatientMedications();
                    }
                    else if (CodeType.Trim().ToLower() == "---responsibleperson")
                    {
                        LoadCodes_ResponsiblePerson();
                    }
                    else if (CodeType.Trim().ToLower() == "---referralsourcecategorytype")
                    {
                        LoadCodes_ReferralSourceCategoryTypes();
                    }
                    else if (CodeType.Trim().ToLower() == "---patientcontacts")
                    {
                        LoadCodes_PatientContacts();
                    }
                    else if (CodeType.Trim().ToLower() == "---patientcaregiversclinician")
                    {
                        LoadCodes_PatientCaregiversClinician(true);
                    }
                    else if (CodeType.Trim().ToLower() == "---patientandcaregivers")
                    {
                        LoadCodes_PatientCaregiversClinician(false);
                    }
                    else if (CodeType.Trim().ToLower() == "---employees")
                    {
                        LoadCodes_Employees();
                    }
                    else if (CodeType.Trim().ToLower() == "---pdgminsurances")
                    {
                        LoadCodes_PDGMInsurances();
                    }
                    else if (CodeType.Trim().ToLower().StartsWith("---serviceline"))
                    {
                        LoadCodes_ServiceLineGroupings();
                        if (controlTextBlock != null) controlTextBlock.Text = null;
                    }
                    else if (CodeType.Trim().ToLower() == "---facilities")
                    {
                        LoadCodes_Facilities();
                    }
                    else if (CodeType.Trim().ToLower() == "---facilitybranch")
                    {
                        LoadCodes_FacilityBranches();
                    }
                    else if (CodeType.Trim().ToLower() == "---pharmacyvendors")
                    {
                        LoadCodes_PharmacyVendors();
                    }
                    else if (CodeType.Trim().ToLower() == "---facilityreferralsources")
                    {
                        LoadCodes_FacilityReferralSources();
                    }
                    else if (CodeType.Trim().ToLower() == "---functionaldeficit")
                    {
                        LoadCodes_FunctionalDeficit();
                    }
                    else if (CodeType.Trim().ToLower() == "---infusionpump")
                    {
                        LoadCodes_InfusionPump();
                    }
                    else if (CodeType.Trim().ToLower() == "---equipmentall")
                    {
                        LoadCodes_EquipmentAll();
                    }
                    else if (CodeType.Trim().ToLower() == "---oasissurvey")
                    {
                        LoadCodes_OASISSurvey();
                    }
                    else if (CodeType.Trim().ToLower() == "---oasissurveyplusall")
                    {
                        LoadCodes_OASISSurveyPlusAll();
                    }
                    else if (CodeType.Trim().ToLower() == "---cmsforms")
                    {
                        LoadCodes_CMSForms();
                    }
                    else if (CodeType.Trim().ToLower() == "---bereavementactivityeventtype")
                    {
                        LoadCodes_BereavementActivityEventType();
                    }
                    else if (CodeType.Trim().ToLower() == "---userserviceline")
                    {
                        LoadCodes_UserServiceLine();
                    }
                    return;
                }
            }
            List<Virtuoso.Server.Data.CodeLookup> _codeLookups = CodeLookupCache.GetCodeLookupsFromType(CodeType, true);
            _codeDescriptions = new List<CodeLookupMultiItem>();
            if (_codeLookups != null)
            {
                foreach (CodeLookup cl in _codeLookups)
                {
                    if (cl.Code.Equals("Other", StringComparison.OrdinalIgnoreCase))
                        OTHER = cl.CodeDescription.Trim();

                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = GetCodeLookupMultiItemDescription(cl), Code = cl.Code.Trim(), Key = cl.CodeLookupKey.ToString() });
                }
            }
        }
        private string GetCodeLookupMultiItemDescription(CodeLookup cl)
        {
            if (cl == null) return null;
            if (string.IsNullOrWhiteSpace(SelectedValuesPath)) return cl.CodeDescription.Trim();
            if (SelectedValuesPath.ToLower().Trim() == "code") return cl.Code.Trim();
            return cl.CodeDescription.Trim();
        }
        private void LoadCodes_ResponsiblePerson()
        {
            List<PatientContact> _patientContacts = null;
            if (Patient != null)
            {
                if (Patient.PatientContact != null)
                {
                    _patientContacts = Patient.PatientContact.Where(p => p.Inactive == false).OrderBy(p => p.FullNameInformal).ToList();
                }
            }
            _codeDescriptions = new List<CodeLookupMultiItem>();
            _codeDescriptions.Add(new CodeLookupMultiItem() { Description = "Agency", Code = "Agency", Key = "Agency" });
            if (_patientContacts != null)
            {
                foreach (PatientContact pc in _patientContacts)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = pc.FullNameInformal.Trim(), Code = pc.PatientContactKey.ToString(), Key = pc.PatientContactKey.ToString() });
                }
            }
        }
        private void LoadCodes_ReferralSourceCategoryTypes()
        {
            List<Virtuoso.Server.Data.CodeLookup> _codeLookups = CodeLookupCache.GetCodeLookupsFromType("ReferralSourceCategory");
            _codeDescriptions = new List<CodeLookupMultiItem>();
            if (_codeLookups != null)
            {
                foreach (CodeLookup cl in _codeLookups)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = cl.CodeDescription.Trim(), Code = cl.Code.Trim(), Key = cl.CodeLookupKey.ToString() });
                }
            }
            _codeLookups = CodeLookupCache.GetCodeLookupsFromType("FacilityType");
            if (_codeLookups != null)
            {
                foreach (CodeLookup cl in _codeLookups)
                {
                    if (_codeDescriptions.Where(cd => cd.Code.Trim().ToLower() == cl.Code.Trim().ToLower()).FirstOrDefault() == null)
                    {
                        _codeDescriptions.Add(new CodeLookupMultiItem() { Description = cl.CodeDescription.Trim(), Code = cl.Code.Trim(), Key = cl.CodeLookupKey.ToString() });
                    }
                }
            }

        }
        private void LoadCodes_PatientContacts()
        {
            List<PatientContact> _patientContacts = null;
            _codeDescriptions = new List<CodeLookupMultiItem>();
            if (Patient != null)
            {
                if (Patient.PatientContact != null)
                {
                    string key = (SelectedKeys == null) ? "" : SelectedKeys.ToString();
                    _patientContacts = Patient.PatientContact.Where(p => (((p.Inactive == false) && (p.HistoryKey == null)) || (p.PatientContactKey.ToString() == key))).OrderBy(p => p.FullNameWithSuffix).ToList();
                }
            }
            if (_patientContacts != null)
            {
                foreach (PatientContact pc in _patientContacts)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = pc.FullNameWithSuffix.Trim(), Code = pc.PatientContactKey.ToString(), Key = pc.PatientContactKey.ToString() });
                }
            }
        }
        private void LoadCodes_PatientCaregiversClinician(bool includeClinician)
        {
            List<PatientContact> _patientContacts = null;
            _codeDescriptions = new List<CodeLookupMultiItem>();
            if (Patient != null)
            {
                if (Patient.PatientContact != null)
                {
                    string key = (SelectedKeys == null) ? "" : SelectedKeys.ToString();
                    _patientContacts = Patient.PatientContact.Where(p => (((p.Inactive == false) && (p.HistoryKey == null) && (p.Caregiver == 1)) || (p.PatientContactKey.ToString() == key))).OrderBy(p => p.FullNameWithSuffix).ToList();
                }

                _codeDescriptions.Add(new CodeLookupMultiItem()
                {
                    Description = "Patient\t" + Patient.FullName,
                    Code = ((int)PatientCaregiverClinicianType.Patient).ToString() + "|" + Patient.PatientKey.ToString(),
                    Key = ((int)PatientCaregiverClinicianType.Patient).ToString() + "|" + Patient.PatientKey.ToString()
                });
            }

            if (_patientContacts != null)
            {
                foreach (PatientContact pc in _patientContacts)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() 
                                { 
                                    Description = "Caregiver\t" + pc.FullNameWithSuffix.Trim(),
                                    Code = ((int)PatientCaregiverClinicianType.Caregiver).ToString() + "|" + pc.PatientContactKey.ToString(),
                                    Key = ((int)PatientCaregiverClinicianType.Caregiver).ToString() + "|" + pc.PatientContactKey.ToString() 
                                });
                }
            }

            if(includeClinician)
            {
                UserProfile up = UserCache.Current.GetCurrentUserProfile();
                if (up != null)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem()
                    {
                        Description = "Clinician\t" + up.FormalName,
                        Code = ((int)PatientCaregiverClinicianType.Clinician).ToString() + "|" + up.UserId.ToString(),
                        Key = ((int)PatientCaregiverClinicianType.Clinician).ToString() + "|" + up.UserId.ToString()
                    });
                }
            }
        }
        private void LoadCodes_Employees()
        {
            Guid? userId = SelectedKeys as Guid?;
            List<UserProfile> users = UserCache.Current.GetActiveUsersPlusMe(userId).Where(u => u.IsEmployee == true).ToList();
            _codeDescriptions = new List<CodeLookupMultiItem>();

            if (users != null)
            {
                users = users.OrderBy(u => u.FullNameWithSuffix).ToList();
                foreach (UserProfile u in users)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = u.FullNameWithSuffix.Trim(), Code = u.UserId.ToString(), Key = u.UserId.ToString() });
                }
            }
        }
        private void LoadCodes_PDGMInsurances()
        {
            List<InsurancePPSParameter> iList = InsuranceCache.GetActiveInsurancePDGMList(null);
            _codeDescriptions = new List<CodeLookupMultiItem>();

            if (iList != null)
            {
                foreach (InsurancePPSParameter i in iList)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = i.InsuranceName.Trim(), Code = i.InsuranceKey.ToString(), Key = i.InsuranceKey.ToString() });
                }
            }
        }

        private List<ServiceLineGrouping> potentialSLGs;
        private void LoadCodes_ServiceLineGroupings()
        {
            _codeDescriptions = new List<CodeLookupMultiItem>();
            if (CodeType.Trim().ToLower() == "---serviceline") return;
            string slKey = CodeType.Trim().ToLower().Replace("---serviceline", "");
            if (string.IsNullOrWhiteSpace(slKey) == true) return;
            int key = 0;
            if (int.TryParse(slKey, out key) == false) return;
            if (key == 0) return;

            if (potentialSLGs == null)
            {
                // Get list of SLGs - filter out duplicate named ones
                potentialSLGs = new List<ServiceLineGrouping>();
                List<ServiceLineGrouping> pslgList = ServiceLineCache.GetActiveServiceLineGroupings();
                if (pslgList != null)
                {
                    foreach (ServiceLineGrouping slg in pslgList) if (potentialSLGs.Where(p => ((p.ServiceLineKey == slg.ServiceLineKey) && (p.Name == slg.Name))).Any() == false) potentialSLGs.Add(slg);
                }
            }
            List<ServiceLineGrouping> slgList = potentialSLGs.Where(a => a.ServiceLineKey == key).OrderBy(a => a.Name).ToList();
            if (slgList != null)
            {
                foreach (ServiceLineGrouping slg in slgList)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = slg.Name.Trim(), Code = slg.Name.Trim(), Key = slg.Name.Trim() });
                }
                _codeDescriptions.Insert(0, new CodeLookupMultiItem() { Description = " ", Code = " ", Key = " "});
            }
        }
        private void LoadCodes_Facilities()
        {
            // assumes SelectionParameter = AdmissionReferral.ReferralSourceCategoryType - a CodeLookup.Code for CodeType='FacilityType'
            int? facilityTypeCodeLookupKey = CodeLookupCache.GetKeyFromCodeDescription("FacilityType", SelectionParameter);
            List<Facility> facilities = FacilityCache.GetActiveFacilities().Where(f => f.Type == facilityTypeCodeLookupKey || SelectionParameter == "ALL").ToList();
            _codeDescriptions = new List<CodeLookupMultiItem>();

            if (facilities != null)
            {
                foreach (Facility facility in facilities)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = facility.Name.Trim(), Code = facility.FacilityKey.ToString(), Key = facility.FacilityKey.ToString() });
                }
            }
        }
        private void LoadCodes_FacilityBranches()
        {
            int facilityKey = 0;
            try { facilityKey = Int32.Parse(SelectionParameter); }
            catch { }
            int facilitybranchKey = 0;
            try { facilitybranchKey = Int32.Parse(SelectedKeys.ToString()); }
            catch { }
            List<FacilityBranch> facilitybranches = FacilityCache.GetActiveBranchesAndMe(facilitybranchKey).Where(f => f.FacilityKey == facilityKey || SelectionParameter == "ALL").ToList();
            _codeDescriptions = new List<CodeLookupMultiItem>();

            if (facilitybranches != null)
            {
                foreach (FacilityBranch facility in facilitybranches)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = facility.BranchName.Trim(), Code = facility.FacilityBranchKey.ToString(), Key = facility.FacilityBranchKey.ToString() });
                }
            }
        }

        private void LoadCodes_PharmacyVendors()
        {
            var vendors = VendorCache.GetActiveVendors().Where(f => f.IsVendorTypePharmacy).ToList();
            _codeDescriptions = new List<CodeLookupMultiItem>();

            if (vendors != null)
            {
                foreach (var vendor in vendors)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem()
                        {
                            Description = vendor.VendorName.Trim(),
                            Code = vendor.Number,
                            Key = vendor.VendorKey.ToString(CultureInfo.InvariantCulture)
                        });
                }
            }
        }

        private void LoadCodes_FacilityReferralSources()
        {
            // assumes SelectionParameter = AdmissionReferral.FacilityKey - a Facility.FacilityKey'
            int facilityKey = 0;
            int? facilityBranchKey = null;
            int? selectedKey = null;
            try { selectedKey = (int)SelectedKeys; }
            catch { }
            try { facilityKey = Int32.Parse(SelectionParameter); }
            catch { }
            // assumes SelectionParameter2 = AdmissionReferral.FacilityBranchKey 
            try { facilityBranchKey = Int32.Parse(SelectionParameter2); }
            catch { }
            List<ReferralSource> referralSources = ReferralSourceCache.GetReferralSources()
                .Where(rs => rs.FacilityKey == facilityKey
                    && (rs.Inactive == false || (selectedKey > 0 && rs.ReferralSourceKey == selectedKey) )
                 && (facilityBranchKey == null || (facilityBranchKey != null && facilityBranchKey == rs.FacilityBranchKey))).ToList();
            _codeDescriptions = new List<CodeLookupMultiItem>();

            if (referralSources != null)
            {
                foreach (ReferralSource referralSource in referralSources)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = referralSource.FullName.Trim(), Code = referralSource.ReferralSourceKey.ToString(), Key = referralSource.ReferralSourceKey.ToString() });
                }
            }
        }
        private void LoadCodes_FunctionalDeficit()
        {

            var fd = FunctionalDeficitCache.GetFunctionalDeficits(true).ToList();
            _codeDescriptions = new List<CodeLookupMultiItem>();

            if (fd != null)
            {
                foreach (var deficit in fd)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = deficit.Description.Trim(), Code = deficit.Code.Trim(), Key = deficit.FunctionalDeficitKey.ToString() });
                }
            }
        }
        private void LoadCodes_InfusionPump()
        {

            int plusMeEquipmentKey = -1;
            try { plusMeEquipmentKey = Int32.Parse(SelectedKeys.ToString()); }
            catch { }
            List<Equipment> eList = EquipmentCache.GetEquipmentByType("InfusionPump", plusMeEquipmentKey, false).ToList();
            _codeDescriptions = new List<CodeLookupMultiItem>();

            if (eList != null)
            {
                foreach (Equipment e in eList)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = e.Description1.Trim(), Code = e.ItemCode.Trim(), Key = e.EquipmentKey.ToString() });
                }
            }
        }
        private void LoadCodes_EquipmentAll()
        {

            List<Equipment> eList = EquipmentCache.GetEquipmentByType("All",-1,false).ToList();
            _codeDescriptions = new List<CodeLookupMultiItem>();

            if (eList != null)
            {
                foreach (Equipment e in eList)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = e.Description1.Trim(), Code = e.ItemCode.Trim(), Key = e.EquipmentKey.ToString() });
                }
            }
        }
        private void LoadCodes_OASISSurvey()
        {
            List<OasisSurvey> osList = OasisCache.GetOasisSurveys();
            _codeDescriptions = new List<CodeLookupMultiItem>();

            if (osList != null)
            {
                foreach (OasisSurvey os in osList)
                {
                    if (_codeDescriptions.Where(o => o.Code == os.RFA.Trim()).Any() == false) _codeDescriptions.Add(new CodeLookupMultiItem() { Description = os.RFA.Trim() + " - " + os.RFADescription.Trim(), Code = os.RFA.Trim(), Key = os.RFA.ToString() });
                }
            }
        }
        private void LoadCodes_OASISSurveyPlusAll()
        {
            List<OasisSurvey> osList = OasisCache.GetOasisSurveys();
            _codeDescriptions = new List<CodeLookupMultiItem>();
            _codeDescriptions.Add(new CodeLookupMultiItem() { Description = "All RFAs", Code = "01,03,04,05,06,07,08,09", Key = "100",  });
            if (osList != null)
            {
                foreach (OasisSurvey os in osList)
                {
                    if (_codeDescriptions.Where(o => o.Code == os.RFA.Trim()).Any() == false) _codeDescriptions.Add(new CodeLookupMultiItem() { Description = os.RFA.Trim() + " - " + os.RFADescription.Trim(), Code = os.RFA.Trim(), Key = os.RFA.ToString() });
                }
            }
        }
        private void LoadCodes_CMSForms()
        {
            List<CMSForm> cfList = CMSFormCache.GetActiveVersionOfCMSForms(false, DateTime.Today.Date);
            _codeDescriptions = new List<CodeLookupMultiItem>();

            if (cfList != null)
            {
                foreach (CMSForm cf in cfList)
                {
                    if (_codeDescriptions.Where(o => o.Code == cf.Name.Trim()).Any() == false) _codeDescriptions.Add(new CodeLookupMultiItem() { Description = cf.Name.Trim() + " - " + cf.Description.Trim(), Code = cf.Name.Trim(), Key = cf.CMSFormKey.ToString() });
                }
            }
        }
        private CollectionViewSource _CurrentFilteredPatientMedication = new CollectionViewSource();
        private ICollectionView CurrentFilteredPatientMedication {get { return _CurrentFilteredPatientMedication.View; }}
        private DateTime medStartDate = DateTime.Today;
        private bool MedFilterItems(object item)
        {
            VirtuosoEntity v = item as VirtuosoEntity;
            // If we have an Encounter and the item is not new, only include the item if it is in this encounter
            PatientMedication pm = item as PatientMedication;
            if (pm != null)
            {
                if ((Encounter != null) && (!v.IsNew))
                {
                   EncounterMedication em = Encounter.EncounterMedication.Where(p => p.PatientMedication.PatientMedicationKey == pm.PatientMedicationKey).FirstOrDefault();
                    if (em == null) return false;
                }
                if (pm.MedicationStartDate != null && (DateTime)pm.MedicationStartDate > medStartDate) return false;
                if (pm.MedicationEndDate != null && (DateTime)pm.MedicationEndDate < medStartDate) return false;
            }
            return true;
        }

        private void LoadCodes_PatientActiveDiagnosis()
        {
            List<AdmissionDiagnosis> _ActiveDiagnosis = new List<AdmissionDiagnosis>(); 
            if (Patient != null)
            {
                _ActiveDiagnosis = Patient.AdmissionDiagnosis.Where(a => (a.Superceded == false) && (a.RemovedDate == null) && (a.Code != "000.00") && (a.Diagnosis == true) && (a.DiagnosisEndDate == null || ((DateTime)a.DiagnosisEndDate).Date >= DateTime.Today.Date)).OrderBy(a => a.Code).ToList();
            }
            // List is add distinct active diagnosis plus those already in the list - active or not - 9 or 10 - surgical or medical
            _codeDescriptions = new List<CodeLookupMultiItem>();
            foreach (AdmissionDiagnosis ad in _ActiveDiagnosis)
            {
                if (IsCodeInList(_codeDescriptions,ad.Code) == false)
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = ad.CodeAndDescription.Trim(), Code = ad.Code.Trim(), Key = ad.Code.Trim() });
                }
            }
            string sv = SelectedValues as string;
            if (string.IsNullOrWhiteSpace(sv) == false)
            {
                string[] delimiters = { TextDelimiter };
                string[] diagDelimiters = { " - " };
                string[] valuesSplit = sv.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                if (valuesSplit.Length != 0)
                {
                    Array.Sort(valuesSplit);
                    foreach (string diag in valuesSplit)
                    {
                        string[] diagSplit = diag.Split(diagDelimiters, StringSplitOptions.RemoveEmptyEntries);
                        if (diagSplit.Length >= 2)
                        {
                            if (IsCodeInList(_codeDescriptions, diagSplit[0]) == false)
                            {
                                _codeDescriptions.Add(new CodeLookupMultiItem() { Description = diag, Code = diagSplit[0], Key = diagSplit[0] });
                            }
                        }
                    }
                }
           }
        }
        private bool IsCodeInList(List<CodeLookupMultiItem> list, string code)
        {
            if ((list == null) || (list.Any() == false)) return false;
            if (string.IsNullOrWhiteSpace(code)) return true;
            foreach (CodeLookupMultiItem i in list)
            {
                if (i.Code.ToLower() == code.ToLower()) return true;
            }
            return false;
        }
        private void LoadCodes_PatientMedications()
        {
            _CurrentFilteredPatientMedication = new CollectionViewSource(); 
            if (Patient != null)
            {
                medStartDate = DateTime.Today;
                if ((Encounter != null) && (Encounter.EncounterStartDate.HasValue))
                {
                    medStartDate = (DateTime)Encounter.EncounterStartDate.Value.DateTime;
                }
                if (Patient.PatientMedication != null)
                {
                    _CurrentFilteredPatientMedication.Source = Patient.PatientMedication;
                    CurrentFilteredPatientMedication.SortDescriptions.Add(new SortDescription("MedicationName", ListSortDirection.Ascending));
                    CurrentFilteredPatientMedication.Filter = new Predicate<object>(MedFilterItems);
                    CurrentFilteredPatientMedication.Refresh();
                }
            }
            _codeDescriptions = new List<CodeLookupMultiItem>();
            if ((_CurrentFilteredPatientMedication != null) && (_CurrentFilteredPatientMedication.View != null))
            {

                foreach (PatientMedication pm in _CurrentFilteredPatientMedication.View.Cast<PatientMedication>())
                {
                    _codeDescriptions.Add(new CodeLookupMultiItem() { Description = pm.MedicationName.Trim(), Code = pm.MedicationName.Trim(), Key = pm.PatientMedicationKey.ToString() });
                }
            }
        }
        private void LoadCodes_BereavementActivityEventType()
        {
            List<Virtuoso.Server.Data.CodeLookup> _codeLookups = CodeLookupCache.GetCodeLookupsFromType("BereavementEventType");
            _codeDescriptions = new List<CodeLookupMultiItem>();
            _codeDescriptions.Add(new CodeLookupMultiItem() { Description = "All Event Types", Code = "All", Key = "1",  });
            if (_codeLookups != null)
            {
                foreach (CodeLookup cl in _codeLookups)
                {
                    if (cl.Code != "Enrollment") _codeDescriptions.Add(new CodeLookupMultiItem() { Description = cl.CodeDescription.Trim(), Code = cl.Code.Trim(), Key = cl.CodeLookupKey.ToString() });
                }
            }
        }
        private void LoadCodes_UserServiceLine()
        {
            List<ServiceLine> slList = ServiceLineCache.GetActiveUserServiceLinePlusMe(null);
            _codeDescriptions = new List<CodeLookupMultiItem>();

            if (slList != null)
            {
                foreach (ServiceLine sl in slList)
                {
                    if (_codeDescriptions.Where(o => o.Code == sl.Name.Trim()).Any() == false) _codeDescriptions.Add(new CodeLookupMultiItem() { Description = sl.Name.Trim(), Code = sl.Name.Trim(), Key = sl.ServiceLineKey.ToString() });
                }
            }
            _codeDescriptions.Add(new CodeLookupMultiItem() { Description = "All Service Lines", Code = "All", Key = "-1", });
        }

        public static DependencyProperty CodeTypeProperty =
         DependencyProperty.Register("CodeType", typeof(string), typeof(Virtuoso.Core.Controls.codeLookupMulti), 
            new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.codeLookupMulti)o).LoadCodes();
            }));

        public string CodeType
        {
            get { return ((string)(base.GetValue(codeLookupMulti.CodeTypeProperty))); }
            set
            {
                base.SetValue(codeLookupMulti.CodeTypeProperty, value);
            }
        }
        public static DependencyProperty MaxLengthProperty =
         DependencyProperty.Register("MaxLength", typeof(int?), typeof(Virtuoso.Core.Controls.codeLookupMulti), null);

        public int? MaxLength
        {
            get { return ((int?)(base.GetValue(codeLookupMulti.MaxLengthProperty))); }
            set
            {
                base.SetValue(codeLookupMulti.MaxLengthProperty, value);
            }
        }

        public static DependencyProperty PatientProperty =
         DependencyProperty.Register("Patient", typeof(Patient), typeof(Virtuoso.Core.Controls.codeLookupMulti),
            new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.codeLookupMulti)o).PatientChanged(); ;
            }));

        public Patient Patient
        {
            get { return ((Patient)(base.GetValue(codeLookupMulti.PatientProperty))); }
            set
            {
                base.SetValue(codeLookupMulti.PatientProperty, value);
            }
        }
        private void PatientChanged()
        {
            LoadCodes();
        }
        public static DependencyProperty SelectionParameterProperty =
         DependencyProperty.Register("SelectionParameter", typeof(string), typeof(Virtuoso.Core.Controls.codeLookupMulti),
            new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.codeLookupMulti)o).SelectionParameterChanged(); ;
            }));

        public string SelectionParameter
        {
            get { return ((string)(base.GetValue(codeLookupMulti.SelectionParameterProperty))); }
            set
            {
                base.SetValue(codeLookupMulti.SelectionParameterProperty, value);
            }
        }
        public static DependencyProperty SelectionParameter2Property =
         DependencyProperty.Register("SelectionParameter2", typeof(string), typeof(Virtuoso.Core.Controls.codeLookupMulti),
            new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.codeLookupMulti)o).SelectionParameterChanged(); ;
            }));

        public string SelectionParameter2
        {
            get { return ((string)(base.GetValue(codeLookupMulti.SelectionParameter2Property))); }
            set
            {
                base.SetValue(codeLookupMulti.SelectionParameter2Property, value);
            }
        }
        private void SelectionParameterChanged()
        {
            LoadCodes();
        }
        public static DependencyProperty EncounterProperty =
         DependencyProperty.Register("Encounter", typeof(Encounter), typeof(Virtuoso.Core.Controls.codeLookupMulti),
            new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.codeLookupMulti)o).EncounterChanged(); ;
            }));

        public Encounter Encounter
        {
            get { return ((Encounter)(base.GetValue(codeLookupMulti.EncounterProperty))); }
            set
            {
                base.SetValue(codeLookupMulti.EncounterProperty, value);
            }
        }
        private void EncounterChanged()
        {
            LoadCodes();
        }

        public static DependencyProperty IncludeOtherProperty =
          DependencyProperty.Register("IncludeOther", typeof(bool), typeof(Virtuoso.Core.Controls.codeLookupMulti), null);
        public bool IncludeOther
        {
            get { return ((bool)(base.GetValue(codeLookupMulti.IncludeOtherProperty))); }
            set { base.SetValue(codeLookupMulti.IncludeOtherProperty, value); }
        }
        public static DependencyProperty SingleSelectProperty =
          DependencyProperty.Register("SingleSelect", typeof(bool), typeof(Virtuoso.Core.Controls.codeLookupMulti), null);
        public bool SingleSelect
        {
            get { return ((bool)(base.GetValue(codeLookupMulti.SingleSelectProperty))); }
            set { base.SetValue(codeLookupMulti.SingleSelectProperty, value); }
        }

        public static DependencyProperty SkipNullItemProperty =
          DependencyProperty.Register("SkipNullItem", typeof(bool), typeof(Virtuoso.Core.Controls.codeLookupMulti), null);
        public bool SkipNullItem
        {
            get { return ((bool)(base.GetValue(codeLookupMulti.SkipNullItemProperty))); }
            set { base.SetValue(codeLookupMulti.SkipNullItemProperty, value); }
        }

        public static DependencyProperty CodeDelimiterProperty =
          DependencyProperty.Register("CodeDelimiter", typeof(string), typeof(Virtuoso.Core.Controls.codeLookupMulti), null);
        public string CodeDelimiter
        {
            get { return ((string)(base.GetValue(codeLookupMulti.CodeDelimiterProperty)) == null) ? "|" : (string)(base.GetValue(codeLookupMulti.CodeDelimiterProperty)); }
            set { base.SetValue(Virtuoso.Core.Controls.codeLookupMulti.CodeDelimiterProperty, value); }
        }

        public static DependencyProperty TextDelimiterProperty =
          DependencyProperty.Register("TextDelimiter", typeof(string), typeof(Virtuoso.Core.Controls.codeLookupMulti), null);
        public string TextDelimiter
        {
            get { return ((string)(base.GetValue(codeLookupMulti.TextDelimiterProperty)) == null) ? " - " : (string)(base.GetValue(codeLookupMulti.TextDelimiterProperty)); }
            set { base.SetValue(Virtuoso.Core.Controls.codeLookupMulti.TextDelimiterProperty, value); }
        }

        public static DependencyProperty SelectedCodesProperty =
         DependencyProperty.Register("SelectedCodes", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupMulti),null);

        public object SelectedCodes
        {
            get 
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupMulti.SelectedCodesProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set 
            {
                base.SetValue(Virtuoso.Core.Controls.codeLookupMulti.SelectedCodesProperty, value); 
            }
        }

        public static DependencyProperty SelectedCodesAndOtherProperty =
         DependencyProperty.Register("SelectedCodesAndOther", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupMulti), null);

        public object SelectedCodesAndOther
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupMulti.SelectedCodesAndOtherProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.codeLookupMulti.SelectedCodesAndOtherProperty, value);
            }
        }

        public static DependencyProperty SelectedKeysAndOtherProperty =
            DependencyProperty.Register("SelectedKeysAndOther", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupMulti), null);

        public object SelectedKeysAndOther
        {
            get
            {                
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupMulti.SelectedKeysAndOtherProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.codeLookupMulti.SelectedKeysAndOtherProperty, value);
            }
        }

        public static DependencyProperty SelectedKeysProperty = DependencyProperty.Register("SelectedKeys", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupMulti), null);

        public object SelectedKeys
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupMulti.SelectedKeysProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.codeLookupMulti.SelectedKeysProperty, value);
            }
        }
        public static DependencyProperty SelectedKeysOnCloseProperty = DependencyProperty.Register("SelectedKeysOnClose", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupMulti), null);

        public object SelectedKeysOnClose
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupMulti.SelectedKeysOnCloseProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.codeLookupMulti.SelectedKeysOnCloseProperty, value);
            }
        }

        public static DependencyProperty SelectedValuesPathProperty =
            DependencyProperty.Register("SelectedValuesPath", typeof(string), typeof(Virtuoso.Core.Controls.codeLookupMulti),
            new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.codeLookupMulti)o).LoadCodes();
            }));

        public string SelectedValuesPath
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupMulti.SelectedValuesPathProperty)));
                return string.IsNullOrWhiteSpace(s) ? "CodeDescription" : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.codeLookupMulti.SelectedValuesPathProperty, value);
            }
        }

        public static DependencyProperty SelectedValuesProperty =
         DependencyProperty.Register("SelectedValues", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupMulti),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.codeLookupMulti)o).SetupSelectedValues();
          }));

        public object SelectedValues
        {
            get
            {
                var dp = Virtuoso.Core.Controls.codeLookupMulti.SelectedValuesProperty;
                var o = base.GetValue(dp);
                string s = ((string)(o));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                var dp = Virtuoso.Core.Controls.codeLookupMulti.SelectedValuesProperty;
                base.SetValue(dp, value);
            }
        }
        private void SetupSelectedValues()
        {
            if (controlTextBlock == null)
            {
                ApplyTemplate();
            }

            string s = SelectedValues as string;
            if (controlTextBlock != null)
            {
                controlTextBlock.Text = (s == null) ? "" : s.ToString();
            }

            this.SelectedKeysAndOther = string.Empty;
        }
        private bool IsCodeSingleSelect(string description)
        {
            if ((CodeType.Trim().ToLower() == "---oasissurveyplusall") && (description == "All RFAs")) return true;
            if ((CodeType.Trim().ToLower() == "---bereavementactivityeventtype") && (description == "All Event Types")) return true;
            if ((CodeType.Trim().ToLower() == "---userserviceline") && (description == "All Service Lines")) return true;
            if (CodeType.Trim().StartsWith("---")) return false;
            return CodeLookupCache.GetSingleSelectFromDescription(CodeType, description);
        }
        private void SingleSelectCheck(SelectionChangedEventArgs e)
        {
            if ((e == null) || (controlListBox == null)) return;
            if (e.AddedItems.Count == 0) return;
            foreach (CodeLookupMultiItem ai in e.AddedItems)
            {
                if ((IsCodeSingleSelect(ai.Description) == true) && (controlListBox.SelectionMode == SelectionMode.Multiple))
                {
                    // Remove all but this (SingleSelect) item
                    controlListBox.SelectedItems.Clear();
                    if (SingleSelect)
                        controlListBox.SelectedItem = ai;
                    else
                        controlListBox.SelectedItems.Add(ai);
                }
                else
                {
                    // Remove singleselect item - if it is in there
                    for (int i = controlListBox.SelectedItems.Count-1; i >= 0; i--) 
                    {
                        CodeLookupMultiItem item = controlListBox.SelectedItems[i] as CodeLookupMultiItem;
                        if (item != null)
                        {
                            if (IsCodeSingleSelect(item.Description) == true)
                            {
                                controlListBox.SelectedItems.Remove(controlListBox.SelectedItems[i]);
                                break;
                            }
                        }
                    }
                }
            }
        }
        bool inSelectionChanged = false;
        private void controlListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isOther = false;
            if (_doubleClickTimer.IsEnabled) return;
            if (inSelectionChanged) return;
            inSelectionChanged = true;
            try
            {
                if (e != null) SingleSelectCheck(e);
                SetSelectedValuesFromListBox();
                if (controlOtherTextBox != null)
                {
                    foreach (CodeLookupMultiItem s in e.AddedItems)
                    {
                        if (s.Description.ToUpper().Trim().Equals(OTHER.ToUpper()))
                        {
                            controlOtherTextBox.Visibility = System.Windows.Visibility.Visible;
                            controlOtherTextBox.UpdateLayout();
                            controlOtherTextBox.Focus();
                            if (scrollViewer != null) scrollViewer.ScrollToBottom();
                            isOther = true;
                        }
                    }
                }
            }
            catch { }
            inSelectionChanged = false;
            if (controlOtherTextBox != null) controlOtherStackPanel.Visibility = (isOther) ? Visibility.Visible : Visibility.Collapsed;
            if (controlOtherStackPanel != null) controlOtherStackPanel.Visibility = (isOther) ? Visibility.Visible : Visibility.Collapsed;
            if (controlOtherStackPanel2 != null) controlOtherStackPanel2.Visibility = (isOther) ? Visibility.Collapsed : Visibility.Visible;

            if (controlListBox == null) return;
            if ((controlListBox.SelectionMode == SelectionMode.Single) && (isOther == false))
            {
                isDoubleClickClose = false;
                this.IsDropDownOpen = false;
            }
        }
        private bool IsValueInSelectedItems(CodeLookupMultiItem value)
        {
            if (controlListBox == null)
            {
                return false;
            }
            if (controlListBox.SelectedItems == null)
            {
                return false;
            }
            foreach (CodeLookupMultiItem s in controlListBox.SelectedItems)
            {
                if (s == value)
                {
                    return true;
                }
            }
            return false;
        }
        private void SetSelectedValuesFromListBox()
        {
            if (controlListBox == null) return;
            string rCodes = null;
            string rCodesAndOther = null;
            string rKeys = null;
            string code = null;
            string key = null;
            string rDesc = null;
            string rKeysAndOther = null;
            bool isOther = false;
            // Iterate over items source rather than SelectedItems to get ordering
            if ((controlListBox != null) && (controlListBox.ItemsSource != null))
            {
                foreach (CodeLookupMultiItem d in controlListBox.ItemsSource)
                {
                    if (IsValueInSelectedItems(d))
                    {
                        if (string.IsNullOrEmpty(d.Description.Trim()))
                        {
                            SelectedValues = "";
                            SelectedCodes = "";
                            SelectedCodesAndOther = "";
                            SelectedKeysAndOther = "";
                            SelectedKeys = null;
                            if (controlTextBlock != null) controlTextBlock.Text = "";
                            if (controlOtherTextBox != null) controlOtherTextBox.Text = "";
                            if (controlListBox.SelectionMode == SelectionMode.Multiple) controlListBox.SelectedItems.Clear();
                            //isDoubleClickClose = false;   // Close dropdown when user chooses the null item
                            //this.IsDropDownOpen = false;
                            return;
                        }
                        else if ((d.Description.ToUpper().Equals(OTHER.ToUpper())) && (IncludeOther))
                        {
                            if (controlOtherTextBox != null)
                            {
                                isOther = true;
                                if (!string.IsNullOrEmpty(controlOtherTextBox.Text))
                                {
                                    rDesc = (rDesc == null) ? rDesc = controlOtherTextBox.Text.Trim() : rDesc = rDesc + TextDelimiter + controlOtherTextBox.Text.Trim();
                                    rCodes = (rCodes == null) ? OTHER : rCodes + CodeDelimiter + OTHER;
                                    rCodesAndOther = (rCodesAndOther == null) ? controlOtherTextBox.Text : rCodesAndOther + CodeDelimiter + controlOtherTextBox.Text;
                                    rKeysAndOther = (rKeysAndOther == null) ? "\"" + controlOtherTextBox.Text + "\"" : rKeysAndOther + CodeDelimiter + "\"" + controlOtherTextBox.Text + "\"";
                                }
                            }
                        }
                        else
                        {
                            rDesc = (rDesc == null) ? d.Description : rDesc + TextDelimiter + d.Description;
                            code = d.Code;


                            if (!string.IsNullOrWhiteSpace(code))
                            {
                                rCodes = (rCodes == null) ? code : rCodes + CodeDelimiter + code;
                                rCodesAndOther = (rCodesAndOther == null) ? code : rCodesAndOther + CodeDelimiter + code;

                            }

                            key = d.Key;
                            if (string.IsNullOrWhiteSpace(key) == false)
                            {
                                rKeys = (rKeys == null) ? key : rKeys + CodeDelimiter + key;
                                rKeysAndOther = (rKeysAndOther == null) ? key : rKeysAndOther + CodeDelimiter + key;
                            }
                        }
                    }
                }
            }
            if (controlTextBlock != null) controlTextBlock.Text = rDesc;
            if (controlOtherStackPanel != null) controlOtherStackPanel.Visibility = (isOther) ? Visibility.Visible : Visibility.Collapsed;
            if (controlOtherStackPanel2 != null) controlOtherStackPanel2.Visibility = (isOther) ? Visibility.Collapsed : Visibility.Visible;
            if (rDesc != ((SelectedValues != null) ? SelectedValues.ToString() : "")) SelectedValues = (string.IsNullOrWhiteSpace(rDesc)) ? "" : rDesc;
            if (rCodes != ((SelectedCodes != null) ? SelectedCodes.ToString() : "")) SelectedCodes = (string.IsNullOrWhiteSpace(rCodes)) ? "" : rCodes;            
            if (rCodesAndOther != ((SelectedCodesAndOther != null) ? SelectedCodesAndOther.ToString() : "")) SelectedCodesAndOther = (string.IsNullOrWhiteSpace(rCodesAndOther)) ? "" : rCodesAndOther;            
            if (rKeysAndOther != ((SelectedKeysAndOther != null) ? SelectedKeysAndOther.ToString() : "")) SelectedKeysAndOther = (string.IsNullOrWhiteSpace(rKeysAndOther)) ? "" : rKeysAndOther;

            SelectedKeys = (string.IsNullOrWhiteSpace(rKeys)) ? null : rKeys;

            //moved where this is fired to account for text being keyed in as Other that may need a notification to fire
            if (SelectedTextChanged != null)
                SelectedTextChanged(this, new EventArgs());
        }
        private bool ObjectIsNullOrWhiteSpace(object o)
        {
            if (o == null) return false;
            string s = o as string;
            return string.IsNullOrWhiteSpace(s);
        }
        private void controlCloseButton_Click(object sender, RoutedEventArgs e)
        {
            isDoubleClickClose = false;
            this.IsDropDownOpen = false;
        }
        private void codeLookupMulti_DropDownClosed(object sender, EventArgs e)
        {
            if (!isDoubleClickClose) SetSelectedValuesFromListBox();
            SelectedKeysOnClose = SelectedKeys;
            isDoubleClickClose = false;
            if (ListDropDownClosed != null)
                ListDropDownClosed(this, new EventArgs());
            //this.Focus();
        }
        private void codeLookupMulti_DropDownOpened(object sender, EventArgs e)
        {
            // Set ListBox selected values
            if (controlListBox != null)
            {
                // Remove the event handler to prevent recursion.
                try { controlListBox.SelectionChanged -= controlListBox_SelectionChanged; }
                catch { }
            }
            if (controlOtherTextBox != null)
            {
                controlOtherTextBox.Width = this.ActualWidth - 50;
            }

            isDoubleClickClose = false;

            if (controlTextBlock == null)
            {
                ApplyTemplate();
            }
            // Setup ListBox values as:
            // - the null value, 
            // - followed by the codelookup values (if any)
            // - folloowed by 'other' if we are to include it
            _itemsSource = new List<CodeLookupMultiItem>();

            if (!SkipNullItem)
                _itemsSource.Add(new CodeLookupMultiItem() { Description = " ", Code = null, Key = null });

            foreach (CodeLookupMultiItem item in _codeDescriptions)
            {
                _itemsSource.Add(item);
            }
            LoadCodes();
            if (IncludeOther && (_itemsSource.Where(i => i.Description.ToLower().Trim() == OTHER.ToLower().Trim()).FirstOrDefault() == null))
            {
                _itemsSource.Add(new CodeLookupMultiItem() { Description = OTHER, Code = OTHER, Key = null });
            }

            string _SelectedValues = SelectedValues as String;

            // Set ListBox selected values
            if (controlListBox != null)
            {
                controlListBox.ItemsSource = _itemsSource;
                // Populate the listBox selections from the SelectedValues
                // Use Try/Catch to further avoid SelectionChanged recursion
                try
                {
                    string _DelimitedKeysAndOther = SelectedKeysAndOther as String;
                    string other = "";

                    _SelectedValues = (_SelectedValues == null) ? "" : _SelectedValues.ToString().Trim();
                    if (controlListBox.SelectionMode == SelectionMode.Multiple)
                    {
                        controlListBox.SelectedItems.Clear();
                    }

                    string[] delimiters = { TextDelimiter };

                    if (!string.IsNullOrEmpty(_DelimitedKeysAndOther))
                    {
                        var valuesSplit = _DelimitedKeysAndOther.Split('|');
                        Array.Sort(valuesSplit);
                        foreach (string c in valuesSplit)
                        {
                            string cTrim = c.Trim();
                            int ikey;
                            if (int.TryParse(cTrim, out ikey))
                            {
                                CodeLookupMultiItem item = GetValueInListByKey(ikey);
                                if (item != null)
                                {
                                    if (SingleSelect)
                                        controlListBox.SelectedItem = item;
                                    else
                                        controlListBox.SelectedItems.Add(item);
                                }
                            }
                            else
                            {
                                if (cTrim.Length > 2 && cTrim.Substring(0, 1).Equals("\"") && cTrim.Substring(cTrim.Length - 1, 1).Equals("\""))
                                {
                                    other = (other.Length > 0 ? other + TextDelimiter : string.Empty) + cTrim.Substring(1, cTrim.Length - 2);
                                }       
                            }
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(_SelectedValues))
                        {                           
                            string[] valuesSplit = _SelectedValues.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                            Array.Sort(valuesSplit);
                            if (valuesSplit.Length != 0)
                            {
                                foreach (string c in valuesSplit)
                                {
                                    string cTrim = c.Trim();
                                    if (!string.IsNullOrEmpty(cTrim))
                                    {
                                        CodeLookupMultiItem item = GetValueInListByDescription(cTrim);
                                        if (item != null)
                                        {
                                            if (SingleSelect)
                                                controlListBox.SelectedItem = item;
                                            else
                                                controlListBox.SelectedItems.Add(item);
                                        }
                                        else
                                        {
                                            other = (other.Length == 0) ? other = cTrim : other = other + TextDelimiter + cTrim;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // Setup Other
                    if ((controlOtherTextBox != null) && (controlOtherStackPanel != null) && (controlOtherStackPanel2 != null))
                    {
                        controlOtherTextBox.Text = other;
                        controlOtherStackPanel.Visibility = Visibility.Collapsed;
                        controlOtherStackPanel2.Visibility = Visibility.Visible;
                        if (!string.IsNullOrEmpty(other))
                        {
                            if (GetValueInListByDescription(OTHER) == null)
                            {
                                _itemsSource.Add(new CodeLookupMultiItem() { Description = OTHER, Code = OTHER, Key = null });   // Add 'Other' choice if we need to 
                            }
                            CodeLookupMultiItem o = _itemsSource.Where(s => s.Description.ToLower().Trim() == OTHER.ToLower().Trim()).FirstOrDefault();
                            if ((o != null) && (controlListBox != null))
                            {
                                if (SingleSelect)
                                    controlListBox.SelectedItem = o;
                                else
                                    controlListBox.SelectedItems.Add(o);
                            } 
                            controlOtherStackPanel.Visibility = Visibility.Visible;
                            controlOtherStackPanel2.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                catch (Exception)
                {
                    // ignore exceptions
                }
            }
            //SelectedValues = _SelectedValues;
            if (controlTextBlock != null)
            {
                controlTextBlock.Text = _SelectedValues;
            }

            //this.Focus();
            //this.UpdateLayout();

            // Reestablish the event handler and scroll to top
            if (controlListBox != null) controlListBox.SelectionChanged += new SelectionChangedEventHandler(controlListBox_SelectionChanged);
            if (scrollViewer != null) scrollViewer.ScrollToTop();
            if (ListDropDownOpened != null)
                ListDropDownOpened(this, new EventArgs());
        }
        void controlOtherTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            SetSelectedValuesFromListBox();
            // Must TabIndex/UpdateLayout/Focus to get focus back to Focus
            if (controlOtherTextBox != null)
            {
                controlOtherTextBox.TabIndex = 0;
                controlOtherTextBox.UpdateLayout();
                controlOtherTextBox.Focus();
            }
            if ((e.Key == Key.Enter) || (e.Key == Key.Escape) || (e.Key == Key.Tab))
            {
                this.IsDropDownOpen = false;
            }
        }

        private CodeLookupMultiItem GetValueInListByDescription(string value, bool includingOther = false)
        {
            if (_itemsSource == null)
            {
                return null;
            }

            if ((value.ToUpper().Equals(OTHER.ToUpper())) && (includingOther))
            {
                return null;
            }

            return _itemsSource.Where(i => i.Description.ToUpper() == value.ToUpper()).FirstOrDefault();
        }

        private CodeLookupMultiItem GetValueInListByKey(int value)
        {
            if (_itemsSource == null)
            {
                return null;
            }

            return _itemsSource.Where(i => i.Key == value.ToString()).FirstOrDefault();
        }

    }
}

