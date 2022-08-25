using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;
using System.Linq;
using Virtuoso.Core.Cache;

namespace Virtuoso.Core.Controls
{
    public partial class comboBoxAdmissionGroupSets : System.Windows.Controls.ComboBox, ICleanup
    {
        private TextBlock displayTextBlock = null;

        private Button contactCloseButton = null;
       // private StackPanel groupingPanel = null;
        private vAsyncComboBox slComboBox1 = null;
        private vAsyncComboBox slComboBox2 = null;
        private vAsyncComboBox slComboBox3 = null;
        private vAsyncComboBox slComboBox4 = null;
        private vAsyncComboBox slComboBox5 = null;

        private Popup contactPopup = null;

        public comboBoxAdmissionGroupSets()
        {
            this.Loaded += new RoutedEventHandler(comboBoxAdmissionGroupSets_Loaded);
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreComboBoxAdmissionGroupSets"]; }
            catch { }
        }
        public void Cleanup()
        {
            this.Loaded -= comboBoxAdmissionGroupSets_Loaded;
            if (contactPopup != null)
            {
                try { contactPopup.Opened -= contactPopup_Opened; }
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
            slComboBox1 = (vAsyncComboBox)GetTemplateChild("GroupingComboBox");
            slComboBox2 = (vAsyncComboBox)GetTemplateChild("GroupingComboBox2");
            slComboBox3 = (vAsyncComboBox)GetTemplateChild("GroupingComboBox3");
            slComboBox4 = (vAsyncComboBox)GetTemplateChild("GroupingComboBox4");
            slComboBox5 = (vAsyncComboBox)GetTemplateChild("GroupingComboBox5");

            SetupGroupingComboEventHandlers();

            if (contactPopup != null)
            {
                try { contactPopup.Closed -= contactPopup_Closed; }
                catch { }
                contactPopup.Closed += new EventHandler(contactPopup_Closed);
                try { contactPopup.Opened -= contactPopup_Opened; }
                catch { }
                contactPopup.Opened += new EventHandler(contactPopup_Opened);
            }
            displayTextBlock = (TextBlock)GetTemplateChild("DisplayTextBlock");

            contactCloseButton = (Button)GetTemplateChild("ContactCloseButton");
            if (contactCloseButton != null)
            {
                // Set/reset Click event
                try { contactCloseButton.Click -= contactCloseButton_Click; }
                catch { }
                contactCloseButton.Click += new RoutedEventHandler(contactCloseButton_Click);
            }

            SetupSelection();
        }

        private void SetupGroupingComboEventHandlers()
        {
            if (slComboBox1 != null)
            {
                slComboBox1.SelectionChanged += new SelectionChangedEventHandler(slGroup1_SelectionChanged);
            }
            if (slComboBox2 != null)
            {
                slComboBox2.SelectionChanged += new SelectionChangedEventHandler(slGroup2_SelectionChanged);
            }
            if (slComboBox3 != null)
            {
                slComboBox3.SelectionChanged += new SelectionChangedEventHandler(slGroup3_SelectionChanged);
            }
            if (slComboBox4 != null)
            {
                slComboBox4.SelectionChanged += new SelectionChangedEventHandler(slGroup4_SelectionChanged);
            }
        }
        public void comboBoxAdmissionGroupSets_Loaded(object sender, RoutedEventArgs e)
        {
        }

        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.ApplyTemplate();
            SetupSelection();
        }
        void slGroup1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (slComboBox2 != null) slComboBox2.ItemsSource = ServiceLineGroupingListDropDown2;
            if ((ServiceLineGroupingListDropDown2 != null)
                && (ServiceLineGroupingListDropDown2.Count == 1)
               )
            {
                slComboBox2.SelectedIndex = 0;
            }
            SetDisplayText();
        }
        void slGroup2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (slComboBox3 != null) slComboBox3.ItemsSource = ServiceLineGroupingListDropDown3;
            if ((ServiceLineGroupingListDropDown3 != null)
                && (ServiceLineGroupingListDropDown3.Count == 1)
               )
            {
                slComboBox3.SelectedIndex = 0;
            }
            SetDisplayText();
        }
        void slGroup3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (slComboBox4 != null) slComboBox4.ItemsSource = ServiceLineGroupingListDropDown4;
            if ((ServiceLineGroupingListDropDown4 != null)
                && (ServiceLineGroupingListDropDown4.Count == 1)
               )
            {
                slComboBox4.SelectedIndex = 0;
            }
            SetDisplayText();
        }
        void slGroup4_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (slComboBox5 != null) slComboBox5.ItemsSource = ServiceLineGroupingListDropDown5;
            if ((ServiceLineGroupingListDropDown5 != null)
                && (ServiceLineGroupingListDropDown5.Count == 1)
               )
            {
                slComboBox5.SelectedIndex = 0;
            }
            SetDisplayText();
        }
        private void SetDisplayText()
        {
            if (displayTextBlock != null)
            {
                displayTextBlock.Text = "";
                AppendToDisplayText(slComboBox1);
                AppendToDisplayText(slComboBox2);
                AppendToDisplayText(slComboBox3);
                AppendToDisplayText(slComboBox4);
                AppendToDisplayText(slComboBox5);
            }
        }

        private void AppendToDisplayText(vAsyncComboBox cBoxParm)
        {
            try
            {

                if (cBoxParm != null && cBoxParm.SelectedItem != null)
                {
                    if(!String.IsNullOrEmpty(displayTextBlock.Text)) displayTextBlock.Text += " - ";
                    displayTextBlock.Text = displayTextBlock.Text + ((ServiceLineGrouping)cBoxParm.SelectedItem).ServiceLineGroupNameWithInactive;
                }
            }
            catch { }
        }
        #region Dependancy Properties
        private string _self = "Self";
        public static DependencyProperty SelfProperty =
         DependencyProperty.Register("Self", typeof(string), typeof(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.comboBoxAdmissionGroupSets)o).SetupSelf();
          }));

        public string Self
        {
            get { return ((string)(base.GetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelfProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelfProperty, value); }
        }
        private void SetupSelf()
        {
            _self = Self;
        }

        public static DependencyProperty CurrentAdmissionProperty =
         DependencyProperty.Register("CurrentAdmission", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.comboBoxAdmissionGroupSets)o).SetupSelection();
          }));

        public Admission CurrentAdmission
        {
            get { return ((Admission)(base.GetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.CurrentAdmissionProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.CurrentAdmissionProperty, value); }
        }

        public static DependencyProperty SelectedServiceLineGroupingKeyProperty =
            DependencyProperty.Register("SelectedServiceLineGroupingKey", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets), new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.comboBoxAdmissionGroupSets)o).SetupSelection();
            }));
        public int? SelectedServiceLineGroupingKey
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedServiceLineGroupingKeyProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedServiceLineGroupingKeyProperty, value); }
        }
        public static DependencyProperty SelectedServiceLineGrouping2KeyProperty =
            DependencyProperty.Register("SelectedServiceLineGrouping2Key", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets), new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.comboBoxAdmissionGroupSets)o).SetupSelection();
            }));
        public int? SelectedServiceLineGrouping2Key
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedServiceLineGrouping2KeyProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedServiceLineGrouping2KeyProperty, value); }
        }
        public static DependencyProperty SelectedServiceLineGrouping3KeyProperty =
            DependencyProperty.Register("SelectedServiceLineGrouping3Key", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets), new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.comboBoxAdmissionGroupSets)o).SetupSelection();
            }));
        public int? SelectedServiceLineGrouping3Key
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedServiceLineGrouping3KeyProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedServiceLineGrouping3KeyProperty, value); }
        }
        public static DependencyProperty SelectedServiceLineGrouping4KeyProperty =
           DependencyProperty.Register("SelectedServiceLineGrouping4Key", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets), new PropertyMetadata((o, e) =>
           {
               ((Virtuoso.Core.Controls.comboBoxAdmissionGroupSets)o).SetupSelection();
           }));
        public int? SelectedServiceLineGrouping4Key
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedServiceLineGrouping4KeyProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedServiceLineGrouping4KeyProperty, value); }
        }
        public static DependencyProperty SelectedServiceLineGrouping5KeyProperty =
           DependencyProperty.Register("SelectedServiceLineGrouping5Key", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets), new PropertyMetadata((o, e) =>
           {
               ((Virtuoso.Core.Controls.comboBoxAdmissionGroupSets)o).SetupSelection();
           }));
        public int? SelectedServiceLineGrouping5Key
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedServiceLineGrouping5KeyProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedServiceLineGrouping5KeyProperty, value); }
        }
        public static DependencyProperty SelectedStartDateProperty =
           DependencyProperty.Register("SelectedStartDate", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets), new PropertyMetadata((o, e) =>
           {
               ((Virtuoso.Core.Controls.comboBoxAdmissionGroupSets)o).SetupSelection();
           }));
        public DateTime? SelectedStartDate
        {
            get { return ((DateTime?)(base.GetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedStartDateProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedStartDateProperty, value); }
        }
        public static DependencyProperty SelectedEndDateProperty =
           DependencyProperty.Register("SelectedEndDate", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets), new PropertyMetadata((o, e) =>
           {
               ((Virtuoso.Core.Controls.comboBoxAdmissionGroupSets)o).SetupSelection();
           }));
        public DateTime? SelectedEndDate
        {
            get { return ((DateTime?)(base.GetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedEndDateProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxAdmissionGroupSets.SelectedEndDateProperty, value); }
        }
        #endregion

        bool skipSetupSelection = false;
        //bool inHere = false;

        private void SetupSelection()
        {
            if (skipSetupSelection) { return; }
            //if (inHere) { return; }
            //inHere = true;
            if (displayTextBlock != null) { displayTextBlock.Text = ""; }
            if (slComboBox1 != null) 
            {
                slComboBox1.ItemsSource = ServiceLineGroupingListDropDown;
            }
            if (slComboBox2 != null)
            {
                slComboBox2.ItemsSource = ServiceLineGroupingListDropDown2;
            }
            if (slComboBox3 != null)
            {
                slComboBox3.ItemsSource = ServiceLineGroupingListDropDown3;
            }
            if (slComboBox4 != null)
            {
                slComboBox4.ItemsSource = ServiceLineGroupingListDropDown4;
            }
            if (slComboBox5 != null)
            {
                slComboBox5.ItemsSource = ServiceLineGroupingListDropDown5;
            }

            // Remove the event handlers to prevent recursion.

            // Reestablish the event handlers.
            SetDisplayText();
            //inHere = false;
        }

        private void contactCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.IsDropDownOpen = false;
        }
        void contactListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //if (!AdvancedMode)
            //    this.IsDropDownOpen = false;
        }
        
        private void contactPopup_Closed(object sender, EventArgs e)
        {
            SetupSelection();
            this.Visibility = System.Windows.Visibility.Collapsed;
        }
        private void contactPopup_Opened(object sender, EventArgs e)
        {
            SetDisplayText();
        }
        public IEnumerable<ServiceLine> AllServiceLines
        {
            get { return ServiceLineCache.GetActiveServiceLinesPlusMe(ServiceLineKey, false); }
        }
        public int ServiceLineKey
        {
            get
            {
                return CurrentAdmission == null ? 0 : CurrentAdmission.ServiceLineKey;
            }
        }
        public ServiceLine SelectedServiceLine
        {
            get { return ServiceLineKey > 0 ? AllServiceLines.Where(s => s.ServiceLineKey == ServiceLineKey).FirstOrDefault() : null; }
        }
        private ServiceLineGroupHeader GetNthServiceLineGroupHeader(int HeaderToRetrieve)
        {
            if (SelectedServiceLine == null) return null;
            if (SelectedServiceLine.ServiceLineGroupHeader == null) return null;
            if (SelectedServiceLine.ServiceLineGroupHeader.Count() > HeaderToRetrieve)
                return SelectedServiceLine.ServiceLineGroupHeader.Where(gh => gh.SequenceNumber == HeaderToRetrieve).FirstOrDefault();
            else
                return null;
        }
        private ObservableCollection<ServiceLineGrouping> GetGroupingListForDropDown(int headerNumber, int? serviceLineGroupingKey, int? ParentKey)
        {
            if (CurrentAdmission == null) return null;
            var ServiceLineGroupHeader = GetNthServiceLineGroupHeader(headerNumber);
            if (ServiceLineGroupHeader == null) return null;

            IEnumerable<ServiceLineGrouping> returnServiceLineGroups = null;
            var servLineGroups = ServiceLineGroupHeader.ServiceLineGrouping
                                    .Where(slg => (slg.ServiceLineGroupingKey == SelectedServiceLineGroupingKey)
                                                        || (!slg.Inactive 
                                                                && ( ((( (ParentKey == null 
                                                                          || ParentKey == 0
                                                                         ) 
                                                                         && headerNumber == 0
                                                                       )
                                                                       || ( (ParentKey > 0)
                                                                            && (slg.ServiceLineGroupingParent1 != null)
                                                                            && slg.ServiceLineGroupingParent1.Any(p => (p.ParentServiceLineGroupingKey == ParentKey)
                                                                                                                       && ((!p.EffectiveFromDate.HasValue)
                                                                                                                           || (p.EffectiveFromDate.Value <= (SelectedStartDate.HasValue 
                                                                                                                                                                ? SelectedStartDate.Value 
                                                                                                                                                                : DateTime.Now.Date
                                                                                                                                                            )
                                                                                                                              )
                                                                                                                          )
                                                                                                                       && ((!p.EffectiveThruDate.HasValue)
                                                                                                                           || (p.EffectiveThruDate.Value >= (SelectedStartDate.HasValue 
                                                                                                                                                                ? SelectedStartDate.Value 
                                                                                                                                                                : DateTime.Now.Date
                                                                                                                                                            )
                                                                                                                              )
                                                                                                                          )
                                                                                                                 )
                                                                          )
                                                                      )
                                                                     )
                                                                   )
                                                           )
                                          );

            if (ServiceLineGroupHeader.CensusTractDependency)
            {
                if (SelectedStartDate.HasValue
                    && (CurrentAdmission != null)
                    && (CurrentAdmission.Patient != null)
                   )
                {
                    PatientAddress pa = CurrentAdmission.Patient.GetEffectiveAddressForDate(SelectedStartDate.Value);

                    if ((pa != null)
                        && !string.IsNullOrEmpty(pa.CensusTract)
                       )
                    {
                        var mapping = CensusTractCache.GetMappingForCensusTractTextAndDate(pa.CensusTract, SelectedStartDate.Value);
                        returnServiceLineGroups = servLineGroups.Where(grp => mapping.Select(m => m.ServiceLineGroupingKey).Contains(grp.ServiceLineGroupingKey));
                    }
                }
            }

            if ((returnServiceLineGroups == null)
               || (returnServiceLineGroups.Count() == 0)
              )
            {
                returnServiceLineGroups = servLineGroups;
            }

            return new ObservableCollection<ServiceLineGrouping>(returnServiceLineGroups);
        }
        public ObservableCollection<ServiceLineGrouping> ServiceLineGroupingListDropDown
        {
            get
            {
                return GetGroupingListForDropDown(0, SelectedServiceLineGroupingKey, null);
            }
        }
        public ObservableCollection<ServiceLineGrouping> ServiceLineGroupingListDropDown2
        {
            get
            {
                int? ParentKey = 0;
                try
                {
                    if (slComboBox1 != null) ParentKey = (int)slComboBox1.SelectedValue;
                }
                catch { }

                return GetGroupingListForDropDown(1, SelectedServiceLineGrouping2Key, ParentKey);
            }
        }
        public ObservableCollection<ServiceLineGrouping> ServiceLineGroupingListDropDown3
        {
            get
            {
                int? ParentKey = 0;
                try
                {
                    if (slComboBox2 != null) ParentKey = (int)slComboBox2.SelectedValue;
                }
                catch { }

                return GetGroupingListForDropDown(2, SelectedServiceLineGrouping3Key, ParentKey);
            }
        }
        public ObservableCollection<ServiceLineGrouping> ServiceLineGroupingListDropDown4
        {
            get
            {
                int? ParentKey = 0;
                try
                {
                    if (slComboBox3 != null) ParentKey = (int)slComboBox3.SelectedValue;
                }
                catch { }

                return GetGroupingListForDropDown(3, SelectedServiceLineGrouping4Key, ParentKey);
            }
        }
        public ObservableCollection<ServiceLineGrouping> ServiceLineGroupingListDropDown5
        {
            get
            {
                int? ParentKey = 0;
                try
                {
                    if (slComboBox4 != null) ParentKey = (int)slComboBox4.SelectedValue;
                }
                catch { }

                return GetGroupingListForDropDown(4, SelectedServiceLineGrouping5Key, ParentKey);
            }
        }
    }
}

