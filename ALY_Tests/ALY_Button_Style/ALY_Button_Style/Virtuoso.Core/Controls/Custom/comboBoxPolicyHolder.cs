using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Controls
{
    public partial class comboBoxPolicyHolder : System.Windows.Controls.ComboBox, ICleanup
    {
        private TextBlock contactTextBlock = null;
        public ListBox contactListBox = null;
        private Button contactCloseButton = null;
        public RadioButton contactRadioSelf = null;
        public RadioButton contactRadioContact = null;
        public RadioButton contactRadioOther = null;
        private StackPanel contactOtherPanel = null;
        private TextBox contactOtherFirstName = null;
        private TextBox contactOtherLastName = null;
        private TextBox contactAddress1 = null;
        private TextBox contactAddress2 = null;
        private TextBox contactCity = null;
        private autoCompleteCombo contactState = null;
        private TextBox contactZipcode = null;
        private vDatePicker contactBirthDate = null;
        private codeLookup contactGender = null;
        private TextBlock contactSameAddr = null;
        private CheckBox contactSameAddrchbox = null;
        private TextBox insuranceNumber = null;
        private TextBox groupNumber = null;
        private TextBox groupName = null;

        private Popup contactPopup = null;

        private Grid advancedPanel = null;
        private Grid advAddressPanel = null;
        private Grid advOtherPanel = null;
        private Grid contactOtherGrid = null;
        private Grid addressSameAsGrid = null;
        public comboBoxPolicyHolder()
        {
            this.Loaded += new RoutedEventHandler(comboBoxPolicyHolder_Loaded);
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreComboBoxPolicyHolder"]; }
            catch { }
        }
        public void Cleanup()
        {
            this.Loaded -= comboBoxPolicyHolder_Loaded;
            if (contactPopup != null)
            {
                try { contactPopup.Opened -= contactPopup_Opened; }
                catch { }
            }
            if (contactListBox != null)
            {
                try { contactListBox.SelectionChanged -= contactListBox_SelectionChanged; }
                catch { }
                try { contactListBox.MouseLeftButtonUp -= contactListBox_MouseLeftButtonUp; }
                catch { }
            }
            if (contactSameAddrchbox != null)
            {
                try { contactSameAddrchbox.Checked -= contactSameAddrchbox_CheckChanged; }
                catch { }
                try { contactSameAddrchbox.Unchecked -= contactSameAddrchbox_CheckChanged; }
                catch { }
            }
            if (contactRadioContact != null)
            {
                try { contactRadioContact.Checked -= contactRadioContact_Checked; }
                catch { }
                try { contactRadioOther.Checked -= contactRadioOther_Checked; }
                catch { }
            }
            if (contactBirthDate != null)
            {
                try { contactBirthDate.SelectedDateChanged -= contactBirthDate_DateChanged; }
                catch { }
            }
            if (contactCloseButton != null)
            {
                try { contactCloseButton.Click -= contactCloseButton_Click; }
                catch { }
            }
            VirtuosoObjectCleanupHelper.CleanupAll(this);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            contactPopup = (Popup)GetTemplateChild("Popup");
            advancedPanel = (Grid)GetTemplateChild("AdvancedPanel");
            advAddressPanel = (Grid)GetTemplateChild("AdvAddressPanel");
            addressSameAsGrid = (Grid)GetTemplateChild("AddressSameAsGrid");
            advOtherPanel = (Grid)GetTemplateChild("AdvOtherPanel");
            contactSameAddr = (TextBlock)GetTemplateChild("SameAddressLabel");
            contactSameAddrchbox = (CheckBox)GetTemplateChild("SameAddress");
            if (contactPopup != null)
            {
                try { contactPopup.Closed -= contactPopup_Closed; }
                catch { }
                contactPopup.Closed += new EventHandler(contactPopup_Closed);
                try { contactPopup.Opened -= contactPopup_Opened; }
                catch { }
                contactPopup.Opened += new EventHandler(contactPopup_Opened);
                if (!AdvancedMode)
                {
                    contactSameAddr.Visibility = Visibility.Collapsed;
                    contactSameAddrchbox.Visibility = Visibility.Collapsed;
                    advancedPanel.Visibility = Visibility.Collapsed;
                }
            }
            contactTextBlock = (TextBlock)GetTemplateChild("ContactTextBlock");
            contactListBox = (ListBox)GetTemplateChild("ContactListBox");
            if (contactListBox != null)
            {
                // Set/reset SelectionChanged event
                
                try { contactListBox.SelectionChanged -= contactListBox_SelectionChanged; }
                catch { }
                contactListBox.SelectionChanged += new SelectionChangedEventHandler(contactListBox_SelectionChanged);
                try { contactListBox.MouseLeftButtonUp -= contactListBox_MouseLeftButtonUp; }
                catch { }
                contactListBox.MouseLeftButtonUp += new MouseButtonEventHandler(contactListBox_MouseLeftButtonUp);
                try { contactSameAddrchbox.Checked -= contactSameAddrchbox_CheckChanged; }
                catch { }
                contactSameAddrchbox.Checked += new RoutedEventHandler(contactSameAddrchbox_CheckChanged);
                try { contactSameAddrchbox.Unchecked -= contactSameAddrchbox_CheckChanged; }
                catch { }
                contactSameAddrchbox.Unchecked += new RoutedEventHandler(contactSameAddrchbox_CheckChanged);
                contactListBox.Visibility = Visibility.Collapsed;
            }
            contactRadioSelf = (RadioButton)GetTemplateChild("ContactRadioSelf");
            if (contactRadioSelf != null)
            {
                // Set/reset Checked event
                try { contactRadioSelf.Checked -= contactRadioSelf_Checked; }
                catch { }
                contactRadioSelf.Checked += new RoutedEventHandler(contactRadioSelf_Checked);
            }
            contactRadioContact = (RadioButton)GetTemplateChild("ContactRadioContact");
            if (contactRadioContact != null)
            {
                // Set/reset Checked event
                try { contactRadioContact.Checked -= contactRadioContact_Checked; }
                catch { }
                contactRadioContact.Checked += new RoutedEventHandler(contactRadioContact_Checked);
            }
            contactRadioOther = (RadioButton)GetTemplateChild("ContactRadioOther");
            if (contactRadioOther != null)
            {
                // Set/reset Checked event
                try { contactRadioOther.Checked -= contactRadioOther_Checked; }
                catch { }
                contactRadioOther.Checked += new RoutedEventHandler(contactRadioOther_Checked);
            }
            contactOtherPanel = (StackPanel)GetTemplateChild("ContactOtherPanel");
            if (contactOtherPanel != null)
            {
                contactOtherPanel.Visibility = Visibility.Collapsed;
            }
            contactOtherGrid = (Grid)GetTemplateChild("ContactOtherGrid");
            if (contactOtherGrid != null)
            {
                contactOtherGrid.Visibility = Visibility.Collapsed;
            }
            contactOtherFirstName = (TextBox)GetTemplateChild("ContactOtherFirstName");
            contactOtherLastName = (TextBox)GetTemplateChild("ContactOtherLastName");
            contactAddress1 = (TextBox)GetTemplateChild("ContactAddress1");
            contactAddress2 = (TextBox)GetTemplateChild("ContactAddress2");
            contactCity = (TextBox)GetTemplateChild("ContactCity");
            contactState = (autoCompleteCombo)GetTemplateChild("ContactState");
            contactZipcode = (TextBox)GetTemplateChild("ContactZipCode");
            contactBirthDate = (vDatePicker)GetTemplateChild("ContactBirthDate");
            if (contactBirthDate != null)
            {
                // Set/reset Checked event
                try
                {
                    contactBirthDate.SelectedDateChanged -= contactBirthDate_DateChanged;
                }
                catch { }
                contactBirthDate.SelectedDateChanged += new EventHandler<SelectionChangedEventArgs>(contactBirthDate_DateChanged);
            }
            contactGender = (codeLookup)GetTemplateChild("ContactGender");
            insuranceNumber = (TextBox)GetTemplateChild("ContactInsNum");
            groupName = (TextBox)GetTemplateChild("ContactGroupName");
            groupNumber = (TextBox)GetTemplateChild("ContactGroupNum");
            contactCloseButton = (Button)GetTemplateChild("ContactCloseButton");
            if (contactCloseButton != null)
            {
                // Set/reset Click event
                try { contactCloseButton.Click -= contactCloseButton_Click; }
                catch { }
                contactCloseButton.Click += new RoutedEventHandler(contactCloseButton_Click);
            }
            if (SelectedLastName != null)
            {
                string s = SelectedLastName as string;
                if (string.IsNullOrWhiteSpace(s) == false) SetupSelection();
            }
        }
        public void contactBirthDate_DateChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedBirthDate = contactBirthDate.SelectedDate;
        }
        public void comboBoxPolicyHolder_Loaded(object sender, RoutedEventArgs e)
        {
        }
        public void contactSameAddrchbox_CheckChanged(object sender, RoutedEventArgs e)
        {
            SetAddressPanelVisibility();
        }

        private void SetAddressPanelVisibility()
        {
            bool same = contactSameAddrchbox.IsChecked == null ? false : (bool)contactSameAddrchbox.IsChecked;
            if (same)
            {
                advAddressPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                advAddressPanel.Visibility = Visibility.Visible;
            }
        }
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.ApplyTemplate();
            if (contactRadioContact != null)
            {
                System.Collections.IEnumerable isList = (ItemsSource == null) ? null : ItemsSource;
                bool found = (isList == null) ? false : isList.Cast<PatientContact>().Any();
                contactRadioContact.IsEnabled = found;
                contactRadioContact.Content = (found) ? "Contact" : "Contact (none defined)";
            }
            SetupSelection();
        }

        private string _self = "Self";
        public static DependencyProperty SelfProperty =
         DependencyProperty.Register("Self", typeof(string), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.comboBoxPolicyHolder)o).SetupSelf();
          }));

        public string Self
        {
            get { return ((string)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelfProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelfProperty, value); }
        }
        private void SetupSelf()
        {
            _self = Self;
        }

        private bool _advancedMode = false;
        public static DependencyProperty AdvancedModeProperty =
         DependencyProperty.Register("AdvancedMode", typeof(bool), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.comboBoxPolicyHolder)o).SetupAdvancedMode();
          }));

        public bool AdvancedMode
        {
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.AdvancedModeProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.AdvancedModeProperty, value); }
        }
        private void SetupAdvancedMode()
        {
            _advancedMode = AdvancedMode;
        }

        private bool _includeRelationship = false;
        public static DependencyProperty IncludeRelationshipProperty =
         DependencyProperty.Register("IncludeRelationship", typeof(bool), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.comboBoxPolicyHolder)o).SetupIncludeRelationship();
          }));

        public bool IncludeRelationship
        {
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.IncludeRelationshipProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.IncludeRelationshipProperty, value); }
        }
        private void SetupIncludeRelationship()
        {
            _includeRelationship = IncludeRelationship;
        }

        public static DependencyProperty SelectedFirstNameProperty =
         DependencyProperty.Register("SelectedFirstName", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.comboBoxPolicyHolder)o).SetupSelection();
          }));

        public object SelectedFirstName
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedFirstNameProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedFirstNameProperty, value); }
        }
        public static DependencyProperty SelectedLastNameProperty =
         DependencyProperty.Register("SelectedLastName", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.comboBoxPolicyHolder)o).SetupSelection();
          }));

        public object SelectedLastName
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedLastNameProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedLastNameProperty, value); }
        }

        #region Advanced Mode Properties
        public static DependencyProperty SelectedAddress1Property =
         DependencyProperty.Register("SelectedAddress1", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder), null);

        public object SelectedAddress1
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedAddress1Property))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedAddress1Property, value); }
        }
        //
        public static DependencyProperty SelectedAddress2Property =
         DependencyProperty.Register("SelectedAddress2", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder), null);

        public object SelectedAddress2
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedAddress2Property))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedAddress2Property, value); }
        }
        //
        public static DependencyProperty SelectedCityProperty =
         DependencyProperty.Register("SelectedCity", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder), null);

        public object SelectedCity
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedCityProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedCityProperty, value); }
        }
        //
        public static DependencyProperty SelectedStateProperty =
         DependencyProperty.Register("SelectedState", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder), null);

        public object SelectedState
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedStateProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedStateProperty, value); }
        }
        //
        public static DependencyProperty SelectedZipCodeProperty =
         DependencyProperty.Register("SelectedZipCode", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder), null);

        public object SelectedZipCode
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedZipCodeProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedZipCodeProperty, value); }
        }
        //
        public static DependencyProperty SelectedBirthDateProperty =
         DependencyProperty.Register("SelectedBirthDate", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder), null);

        public object SelectedBirthDate
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedBirthDateProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedBirthDateProperty, value); }
        }
        //
        public static DependencyProperty SelectedGenderProperty =
         DependencyProperty.Register("SelectedGender", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder), null);

        public object SelectedGender
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedGenderProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedGenderProperty, value); }
        }
        //
        public static DependencyProperty SelectedSameAsPatProperty =
         DependencyProperty.Register("SelectedSameAsPat", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder), null);

        public object SelectedSameAsPat
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedSameAsPatProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedSameAsPatProperty, value); }
        }
        //
        public static DependencyProperty SelectedInsNumProperty =
         DependencyProperty.Register("SelectedInsNum", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder), null);

        public object SelectedInsNum
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedInsNumProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedInsNumProperty, value); }
        }
        //
        public static DependencyProperty SelectedGroupNameProperty =
         DependencyProperty.Register("SelectedGroupName", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder), null);

        public object SelectedGroupName
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedGroupNameProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedGroupNameProperty, value); }
        }
        //
        public static DependencyProperty SelectedGroupNumberProperty =
         DependencyProperty.Register("SelectedGroupNumber", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder), null);

        public object SelectedGroupNumber
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedGroupNumberProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectedGroupNumberProperty, value); }
        }
        public static DependencyProperty SelectionTypeProperty =
         DependencyProperty.Register("SelectionType", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxPolicyHolder), null);

        public object SelectionType
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectionTypeProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxPolicyHolder.SelectionTypeProperty, value); }
        }
        #endregion

        bool skipSetupSelection = false;

        private void SetupSelection()
        {
            if (skipSetupSelection) { return; }
            if (contactTextBlock != null) { contactTextBlock.Text = ""; }
            if (contactRadioSelf != null) { contactRadioSelf.IsChecked = false; }
            if (contactRadioContact != null) { contactRadioContact.IsChecked = false; }
            if (contactRadioOther != null) { contactRadioOther.IsChecked = false; }
            if (contactListBox != null)
            {
                contactListBox.Visibility = Visibility.Collapsed;
                try { contactListBox.SelectedItem = null; }
                catch { }
            }
            if (contactOtherPanel != null) { contactOtherPanel.Visibility = Visibility.Collapsed; }
            if (contactOtherGrid != null) { contactOtherGrid.Visibility = Visibility.Collapsed; }
            if (contactOtherFirstName != null) { contactOtherFirstName.Text = ""; }
            if (contactOtherLastName != null) { contactOtherLastName.Text = ""; }

            // We need to port nulls to empty strings to get required validation to work
            if (SelectedLastName == null)
            {
                SelectedLastName = "";
                return;
            }
            if (SelectedFirstName == null)
            {
                SelectedFirstName = "";
                return;
            }

            string firstName = (SelectedFirstName == null) ? "" : SelectedFirstName.ToString().Trim();
            string lastName = (SelectedLastName == null) ? "" : SelectedLastName.ToString().Trim();
            string address1 = (SelectedAddress1 == null) ? "" : SelectedAddress1.ToString().Trim();
            string address2 = (SelectedAddress2 == null) ? "" : SelectedAddress2.ToString().Trim();
            string city = (SelectedCity == null) ? "" : SelectedCity.ToString().Trim();
            int? state = (SelectedState == null) ? null : (int?)SelectedState;
            string zipcode = (SelectedZipCode == null) ? "" : SelectedZipCode.ToString().Trim();
            DateTime? birthdate = (SelectedBirthDate == null || SelectedBirthDate.ToString() == "") ? null : (DateTime?)SelectedBirthDate;
            int? gender = (SelectedGender == null) ? null : (int?)SelectedGender;
            bool sameAsPat = (SelectedSameAsPat == null) ? false : (bool)SelectedSameAsPat;
            string insNumStr = (SelectedInsNum == null) ? "" : SelectedInsNum.ToString().Trim();
            string groupNameStr = (SelectedGroupName == null) ? "" : SelectedGroupName.ToString().Trim();
            string groupNumStr = (SelectedGroupNumber == null) ? "" : SelectedGroupNumber.ToString().Trim();

            if (string.IsNullOrEmpty(lastName) && string.IsNullOrEmpty(firstName)) { return; }
            // Remove the event handlers to prevent recursion.
            if (contactListBox != null)
            {
                try { contactListBox.SelectionChanged -= contactListBox_SelectionChanged; }
                catch { }
            } 
            if (contactRadioSelf != null) 
            {
                try { contactRadioSelf.Checked -= contactRadioSelf_Checked; }
                catch { }
            }
            if (contactRadioContact != null) 
            {
                try { contactRadioContact.Checked -= contactRadioContact_Checked; }
                catch { }
            }
            if (contactRadioOther != null) 
            {
                try { contactRadioOther.Checked -= contactRadioOther_Checked; }
                catch { }
            } 

            // Use Try/Catch to further avoid  recursion
            try
            {
                if (lastName.ToUpper().Equals(_self.ToUpper()))
                {
                    if (contactTextBlock != null) { contactTextBlock.Text = _self; }
                    if (contactRadioSelf != null) { contactRadioSelf.IsChecked = true; }
                }
                else
                {
                    // Scan the contact list - if the name is in there check it
                    bool found = false;
                    if (contactGender != null) contactGender.SelectedKey = gender;
                    if (contactBirthDate != null) contactBirthDate.DateObject = birthdate;
                    if (insuranceNumber != null) insuranceNumber.Text = insNumStr;
                    if (groupName != null) groupName.Text = groupNameStr;
                    if (groupNumber != null) groupNumber.Text = groupNumStr;

                    if (this.ItemsSource != null)
                    {
                        int i = 0;
                        foreach (PatientContact pc in this.ItemsSource)
                        {
                            if ((lastName.ToUpper().Equals(GetContactLastNameWWORelationship(pc).ToUpper())) && (firstName.ToUpper().Equals(pc.FirstName.ToUpper())))
                            {
                                found = true;
                                if (contactTextBlock != null) { contactTextBlock.Text = GetContactFullNameInformalWWORelationship(pc); }
                                if (contactRadioContact != null) { contactRadioContact.IsChecked = true; }
                                SetContactRadioButtonSelectionMode();
                                if (contactListBox != null)
                                {
                                    contactListBox.Visibility = Visibility.Visible ;
                                    try { contactListBox.SelectedItem = pc; }
                                    catch { }
                                    try { contactListBox.SelectedIndex = i; }
                                    catch { }
                                }
                                break;
                            }
                            i++;
                        }
                    }
                    if (found == false)
                    {
                        if ((!string.IsNullOrEmpty(firstName)) || (!string.IsNullOrEmpty(lastName)))
                        {
                            if (contactRadioOther != null) { contactRadioOther.IsChecked = true; }
                            if (contactOtherPanel != null) { contactOtherPanel.Visibility = Visibility.Visible; }
                            if (contactOtherGrid != null) { contactOtherGrid.Visibility = Visibility.Visible; }
                            if (contactOtherFirstName != null) contactOtherFirstName.Text = firstName;
                            if (contactOtherLastName != null) contactOtherLastName.Text = lastName;
                            if (contactAddress1 != null) contactAddress1.Text = address1;
                            if (contactAddress2 != null) contactAddress2.Text = address2;
                            if (contactCity != null) contactCity.Text = city;
                            if (contactState != null) contactState.SelectedValue = state;
                            if (contactZipcode != null) contactZipcode.Text = zipcode;
                            if (contactSameAddrchbox != null) contactSameAddrchbox.IsChecked = sameAsPat;
                            SetContactTextBlockFromContactOther();
                        }
                    }
                }
            }
            catch { }
            // Reestablish the event handlers.
            if (contactListBox != null) contactListBox.SelectionChanged += new SelectionChangedEventHandler(contactListBox_SelectionChanged);
            if (contactRadioSelf != null) contactRadioSelf.Checked += new RoutedEventHandler(contactRadioSelf_Checked);
            if (contactRadioContact != null) contactRadioContact.Checked += new RoutedEventHandler(contactRadioContact_Checked);
            if (contactRadioOther != null) contactRadioOther.Checked += new RoutedEventHandler(contactRadioOther_Checked);
        }
        private void contactListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if ((contactListBox.SelectedItem != null) && ((bool)contactRadioContact.IsChecked))
                {
                    PatientContact pc = contactListBox.SelectedItem as PatientContact;
                    if (contactTextBlock != null) { contactTextBlock.Text = GetContactFullNameInformalWWORelationship(pc); }
                    SelectedLastName = GetContactLastNameWWORelationship(pc);
                    SelectedFirstName = pc.FirstName;
                    SelectedAddress1 = pc.Address1;
                    SelectedAddress2 = pc.Address2;
                    SelectedCity = pc.City;
                    SelectedState = pc.StateCode;
                    contactState.SelectedValue = pc.StateCode;
                    SelectedZipCode = pc.ZipCode;
                    SetContactRadioButtonSelectionMode();
                    if(!AdvancedMode)
                        this.IsDropDownOpen = false;
                }
            }
            catch { }
        }
        private string GetContactLastNameWWORelationship(PatientContact pc)
        {
            return (_includeRelationship) ? pc?.LastNameWithRelationship : pc?.LastName;
        }
        private string GetContactFullNameInformalWWORelationship(PatientContact pc)
        {
            return (_includeRelationship) ? pc?.FullNameInformalWithRelationship : pc?.FullNameInformal;
        }
        private void contactRadioSelf_Checked(object sender, RoutedEventArgs e)
        {
            SelectedFirstName = null;
            SelectedLastName = _self;
            if (contactTextBlock != null) { contactTextBlock.Text = _self; }
            this.IsDropDownOpen = false;
            if (contactListBox != null) 
            { 
                contactListBox.Visibility = Visibility.Collapsed;
                try { contactListBox.SelectedItem = null; }
                catch { }
            }
            if (contactOtherPanel != null) { contactOtherPanel.Visibility = Visibility.Collapsed; }
        }
        private void contactRadioContact_Checked(object sender, RoutedEventArgs e)
        {
            if (contactListBox != null)
            {
                contactListBox.Visibility = Visibility.Visible;
                //DS 9009 071414
                if (contactOtherPanel != null)
                {
                    contactOtherPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SetContactRadioButtonSelectionMode()
        {

            if (AdvancedMode)
            {
                if (contactOtherPanel != null) { contactOtherPanel.Visibility = Visibility.Visible; }
                if (contactOtherGrid != null) { contactOtherGrid.Visibility = Visibility.Collapsed; }
                if (advancedPanel != null) { advancedPanel.Visibility = Visibility.Visible; }
                if (advAddressPanel != null) { advAddressPanel.Visibility = Visibility.Collapsed; }
                if (addressSameAsGrid != null) { addressSameAsGrid.Visibility = Visibility.Collapsed; }
                if (advOtherPanel != null) { advOtherPanel.Visibility = Visibility.Visible; }
            }
            else
                if (contactOtherPanel != null) { contactOtherPanel.Visibility = Visibility.Collapsed; }
        }
        private void contactRadioOther_Checked(object sender, RoutedEventArgs e)
        {
            //SelectedLastName = "";
            //SelectedFirstName = "";
            if (contactTextBlock != null) { contactTextBlock.Text = ""; }
            if (contactListBox != null) 
            {
                try { contactListBox.SelectedItem = null; }
                catch { }
                contactListBox.Visibility = Visibility.Collapsed; 
            }
            if (contactOtherPanel != null) { contactOtherPanel.Visibility = Visibility.Visible; }
            if (contactOtherGrid != null) { contactOtherGrid.Visibility = Visibility.Visible; }
            if (AdvancedMode)
            {
                if (advancedPanel != null) { advancedPanel.Visibility = Visibility.Visible; }
                if (advAddressPanel != null) { advAddressPanel.Visibility = Visibility.Visible; }
                if (advOtherPanel != null) { advOtherPanel.Visibility = Visibility.Visible; }
                if (addressSameAsGrid != null) { addressSameAsGrid.Visibility = Visibility.Visible; }
            }
            contactRadioOther.Focus();
        }
        private void contactCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.IsDropDownOpen = false;
        }
        void contactListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!AdvancedMode)
                this.IsDropDownOpen = false;
        }
        private void SetContactTextBlockFromContactOther()
        {
            string firstName = "";
            string lastName = "";
            if (contactOtherFirstName != null) firstName = (contactOtherFirstName.Text == null) ? "" : contactOtherFirstName.Text.Trim();
            if (contactOtherLastName != null) lastName = (contactOtherLastName.Text == null) ? "" : contactOtherLastName.Text.Trim();
            if (contactTextBlock != null)
            {
                contactTextBlock.Text = firstName + " " + lastName;
            }
        }
        private void contactPopup_Closed(object sender, EventArgs e)
        {
            if (contactRadioOther != null)
            {
                if (contactRadioOther.IsChecked == true)
                {
                    SetContactTextBlockFromContactOther();
                    skipSetupSelection = true;
                    SelectedFirstName = (contactOtherFirstName.Text == null) ? "" : contactOtherFirstName.Text.Trim();
                    SelectedLastName = (contactOtherLastName.Text == null) ? "" : contactOtherLastName.Text.Trim();
                    SelectedSameAsPat = (contactSameAddrchbox == null) ? false : contactSameAddrchbox.IsChecked;
                    if (!((bool)SelectedSameAsPat))
                    {
                        SelectedAddress1 = (String.IsNullOrEmpty(contactAddress1.Text)) ? null : contactAddress1.Text.Trim();
                        SelectedAddress2 = (String.IsNullOrEmpty(contactAddress2.Text)) ? null : contactAddress2.Text.Trim();
                        SelectedCity = (String.IsNullOrEmpty(contactCity.Text)) ? null : contactCity.Text.Trim();
                        SelectedState = contactState.SelectedValue;
                        SelectedZipCode = (String.IsNullOrEmpty(contactZipcode.Text)) ? null : contactZipcode.Text.Trim();
                    }
                    else
                    {
                        SelectedAddress1 = null;
                        SelectedAddress2 = null;
                        SelectedCity = null;
                        SelectedState = null;
                        SelectedZipCode = null;
                    }
                    SetAdvancedPanelDataOnClose();
                    skipSetupSelection = false;
                }
                if (contactRadioContact.IsChecked == true || contactRadioSelf.IsChecked == true)
                {
                    SetAdvancedPanelDataOnClose();
                }
            }
            SetupSelection();
            SetInsuredTypeOnClose();
        }
        private void SetInsuredTypeOnClose()
        {
            if (contactRadioContact.IsChecked == true)
            {
                SelectionType = "C";
            }
            else if (contactRadioSelf.IsChecked == true)
            {
                SelectionType = "S";
            }
            else
            {
                SelectionType = "X";
            }
        }
        private void SetAdvancedPanelDataOnClose()
        {
            if (contactRadioSelf.IsChecked == true)
            {
                SelectedGender = null;
                SelectedBirthDate = null;
                SelectedInsNum = null;
                SelectedGroupName = null;
                SelectedGroupNumber = null;
            }
            else
            {
                //SelectedFirstName = (contactOtherFirstName.Text == null) ? "" : contactOtherFirstName.Text.Trim();
                //SelectedLastName = (contactOtherLastName.Text == null) ? "" : contactOtherLastName.Text.Trim();
                SelectedGender = contactGender.SelectedKey;
                SelectedInsNum = String.IsNullOrEmpty(insuranceNumber.Text) ? null : insuranceNumber.Text.Trim();
                if (groupName != null)
                {
                    SelectedGroupName = String.IsNullOrEmpty(groupName.Text) ? null : groupName.Text.Trim();
                }
                if (groupNumber != null)
                {
                    SelectedGroupNumber = String.IsNullOrEmpty(groupNumber.Text) ? null : groupNumber.Text.Trim();
                }
            }
        }
        private void contactPopup_Opened(object sender, EventArgs e)
        {
            if (contactRadioSelf != null) contactRadioSelf.Content = _self;
        }
    }
}

