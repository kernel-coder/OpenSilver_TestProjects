using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Virtuoso.Server.Data;
using Virtuoso.Core.Cache;
using System.Linq;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Model;
using Virtuoso.Core.Services;
using System.ComponentModel;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Utility;

namespace Virtuoso.Core.Controls
{
    public partial class comboBoxServiceLineGrouping : System.Windows.Controls.ComboBox, INotifyDataErrorInfo, ICleanup
    {
        private TextBlock admissionGroupTextBlock = null;

        public ListBox admissionGroupListBox = null;
        private Button admissionGroupCloseButton = null;
       // private vAsyncComboBox groupingComboBox = null;
        private HyperlinkButton addNew = null;
        private ListBox popupListBox = null;

        private Popup serviceLinePopup = null;
        
        public comboBoxServiceLineGrouping()
        {
            this._currentErrors = new Dictionary<string, List<string>>();
            this.Loaded += new RoutedEventHandler(comboBoxServiceLineGrouping_Loaded);
            this.Style = (Style)System.Windows.Application.Current.Resources["CoreComboBoxServiceLineGrouping"];
            ServiceLineGroupingSetup();
        }
        public void Cleanup()
        {
            this.Loaded -= comboBoxServiceLineGrouping_Loaded;
            UnsubscribeEvent(() => admissionGroupListBox.MouseLeftButtonUp -= admissionGroupListBox_MouseLeftButtonUp);
            UnsubscribeEvent(() => serviceLinePopup.Closed -= admissionGroupPopup_Closed);
            UnsubscribeEvent(() => serviceLinePopup.Opened -= admissionGroupPopup_Opened);
            admissionGroupCloseButton = (Button)GetTemplateChild("AdmissionGroupCloseButton");
            if (admissionGroupCloseButton != null)
            {
                // Set/reset Click event
                UnsubscribeEvent(() => admissionGroupCloseButton.Click -= admissionGroupCloseButton_Click);
            }
            admissionGroupCloseButton = (Button)GetTemplateChild("AdmissionGroupCloseButton");
            if (admissionGroupCloseButton != null)
            {
                // Set/reset Click event
                UnsubscribeEvent(() => admissionGroupCloseButton.Click -= admissionGroupCloseButton_Click);
            }
            admissionGroupListBox = (ListBox)GetTemplateChild("AdmissionGroupListBox");
            if (admissionGroupListBox != null)
            {
                UnsubscribeEvent(() => admissionGroupListBox.MouseLeftButtonUp -= admissionGroupListBox_MouseLeftButtonUp);
            }
            if (_popupProvider != null) _popupProvider.SetPopupChildComboBoxInput(null);
            if (_popupProvider != null) _popupProvider.SetPopupChildTextBoxInput(null);
            if (_popupProvider != null) _popupProvider.SetPopupChild(null);
            TriggerButton = null;
            PopupControl = null;
            SetupPopupProvider();

            VirtuosoObjectCleanupHelper.CleanupAll(this);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            serviceLinePopup = (Popup)GetTemplateChild("Popup");
            addNew = (HyperlinkButton)GetTemplateChild("controlHyperlinkButton");
            if (serviceLinePopup != null)
            {
                UnsubscribeEvent(() => serviceLinePopup.Closed -= admissionGroupPopup_Closed);
                serviceLinePopup.Closed += new EventHandler(admissionGroupPopup_Closed);
                UnsubscribeEvent(() => serviceLinePopup.Opened -= admissionGroupPopup_Opened);
                serviceLinePopup.Opened += new EventHandler(admissionGroupPopup_Opened);
            }
            popupListBox = (ListBox)GetTemplateChild("controlListBox");
            admissionGroupTextBlock = (TextBlock)GetTemplateChild("AdmissionGroupTextBlock");
            if (admissionGroupTextBlock != null) { admissionGroupTextBlock.Text = ""; }
            GetAdmissionGroupListBoxRef();
            admissionGroupCloseButton = (Button)GetTemplateChild("AdmissionGroupCloseButton");
            if (admissionGroupCloseButton != null)
            {
                // Set/reset Click event
                UnsubscribeEvent(() => admissionGroupCloseButton.Click -= admissionGroupCloseButton_Click);
                admissionGroupCloseButton.Click += new RoutedEventHandler(admissionGroupCloseButton_Click);
            }
            admissionGroupCloseButton = (Button)GetTemplateChild("AdmissionGroupCloseButton");
            if (admissionGroupCloseButton != null)
            {
                // Set/reset Click event
                UnsubscribeEvent(() => admissionGroupCloseButton.Click -= admissionGroupCloseButton_Click);
                admissionGroupCloseButton.Click += new RoutedEventHandler(admissionGroupCloseButton_Click);
            }
        }
        
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Don't allow the user to key through the control.  It causes display issues.
            if (e.Key == Key.Down) this.IsDropDownOpen = true;
            if (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Left || e.Key == Key.Right)
                e.Handled = true;
            else
                base.OnKeyDown(e);
        }

        private void UnsubscribeEvent(Action unregistrationCallback)
        {
            try
            {
                unregistrationCallback();
            }
            catch { }
        }

        private void GetAdmissionGroupListBoxRef()
        {
            admissionGroupListBox = (ListBox)GetTemplateChild("AdmissionGroupListBox");
            if (admissionGroupListBox != null)
            {
                UnsubscribeEvent(() => admissionGroupListBox.MouseLeftButtonUp -= admissionGroupListBox_MouseLeftButtonUp);
                admissionGroupListBox.MouseLeftButtonUp += new MouseButtonEventHandler(admissionGroupListBox_MouseLeftButtonUp);
            }
        }
        public void comboBoxServiceLineGrouping_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
        protected void ValidateGroups()
        {
            if (CurrentAdmission == null || CurrentAdmission.AdmissionGroup == null) return;
            var ag = CurrentAdmission.AdmissionGroup.FirstOrDefault();
            if (ag == null) return;
            CurrentAdmission.ValidationErrors.Clear();
            foreach (var g in CurrentAdmission.AdmissionGroup) { g.ValidationErrors.Clear(); };
            if (!ag.Admission.ValidateAdmissionGroupOverlap())
            {
                foreach (var g in CurrentAdmission.AdmissionGroup)
                {
                    foreach(var m in g.ValidationErrors)
                    {
                        ((Admission)ag.Admission).ValidationErrors.Add(new ValidationResult (m.ErrorMessage, m.MemberNames));
                    }
                }
            }
        }
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.ApplyTemplate();
            SetupSelection();
        }

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Count() > 0);
                return (ret.Count() > 0) ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Count() > 0)
                return _currentErrors[propertyName];
            else
                return null;
        }

        public bool HasErrors
        {
            get
            {
                return (_currentErrors.Where(c => c.Value.Count > 0).Count() > 0);
            }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
        
        #region Dependancy Properties
        public static DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxServiceLineGrouping), null);
        public object Model
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.ModelProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.ModelProperty, value); }
        }

        public static DependencyProperty GroupLevelNumberProperty =
            DependencyProperty.Register("GroupLevelNumber", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxServiceLineGrouping), null);
        public int? GroupLevelNumber
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.GroupLevelNumberProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.GroupLevelNumberProperty, value); }
        }

        public static DependencyProperty CurrentAdmissionProperty =
            DependencyProperty.Register("CurrentAdmission", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxServiceLineGrouping), new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.comboBoxServiceLineGrouping)o).SetupSelection();
            }));
        public Admission CurrentAdmission
        {
            get { return ((Admission)(base.GetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.CurrentAdmissionProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.CurrentAdmissionProperty, value); }
        }

        public static DependencyProperty ServiceLineKeyProperty =
            DependencyProperty.Register("ServiceLineKey", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxServiceLineGrouping), null);
        public int? ServiceLineKey
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.ServiceLineKeyProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.ServiceLineKeyProperty, value); }
        }
        public static DependencyProperty SelectedGroupHeaderKeyProperty =
            DependencyProperty.Register("SelectedGroupHeaderKey", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxServiceLineGrouping), new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.comboBoxServiceLineGrouping)o).SetupSelection();
            }));
        public int? SelectedGroupHeaderKey
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.SelectedGroupHeaderKeyProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.SelectedGroupHeaderKeyProperty, value); }
        }
        public static DependencyProperty SelectedServiceLineGroupingKeyProperty =
            DependencyProperty.Register("SelectedServiceLineGroupingKey", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxServiceLineGrouping), new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.comboBoxServiceLineGrouping)o).SetupSelection();
            }));
        public int? SelectedServiceLineGroupingKey
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.SelectedServiceLineGroupingKeyProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.SelectedServiceLineGroupingKeyProperty, value);  }
        }

        public static DependencyProperty BindingKeyForErrorNotifyProperty =
            DependencyProperty.Register("BindingKeyForErrorNotify", typeof(object), typeof(Virtuoso.Core.Controls.comboBoxServiceLineGrouping), null);
        public int? BindingKeyForErrorNotify
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.BindingKeyForErrorNotifyProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.comboBoxServiceLineGrouping.BindingKeyForErrorNotifyProperty, value); }
        }
        #endregion

        #region ServiceLine Logic
        public String AddString
        {
            get
            {
                var ServiceLineGroupHeader = GetNthServiceLineGroupHeader((int)GroupLevelNumber);
                return "Add " + (ServiceLineGroupHeader == null ? "" : ServiceLineGroupHeader.GroupHeaderLabel);
            }
        }
        public IEnumerable<ServiceLine> AllServiceLines
        {
            get { return ServiceLineCache.GetActiveServiceLinesPlusMe(ServiceLineKey, false); }
        }
        public ServiceLine SelectedServiceLine
        {
            get { return ServiceLineKey > 0 ? AllServiceLines.Where(s => s.ServiceLineKey == ServiceLineKey).FirstOrDefault() : null; }
        }
        public ObservableCollection<ServiceLineGrouping> ServiceLineGroupingList
        {
            get
            {
                if (SelectedServiceLine == null) return null;
                var ServiceLineGroupHeader = GetNthServiceLineGroupHeader((int)GroupLevelNumber);
                if (ServiceLineGroupHeader == null) return null;
                return new ObservableCollection<ServiceLineGrouping>(ServiceLineGroupHeader.ServiceLineGrouping);
            }
        }
        public ObservableCollection<ServiceLineGrouping> ServiceLineGroupingListDropDown
        {
            get
            {
                if (SelectedServiceLine == null) return null;
                var ServiceLineGroupHeader = GetNthServiceLineGroupHeader((int)GroupLevelNumber);
                if (ServiceLineGroupHeader == null) return null;
                return new ObservableCollection<ServiceLineGrouping>(ServiceLineGroupHeader.ServiceLineGrouping.Where(slg => slg.Inactive || !slg.Inactive || slg.ServiceLineGroupingKey == SelectedServiceLineGroupingKey));
            }
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
        #endregion
        
        bool skipSetupSelection = false;
        private void SetupSelection()
        {
            if (skipSetupSelection) { return; }
            if (admissionGroupListBox == null)
            {
                GetAdmissionGroupListBoxRef();
            }
            if (admissionGroupTextBlock != null) { admissionGroupTextBlock.Text = ""; }
            if (SelectedServiceLineGroupingKey == null || SelectedServiceLineGroupingKey == 0) return;
            if (CurrentAdmission == null || CurrentAdmission.AdmissionGroup == null || CurrentAdmission.AdmissionGroup
                        .Where(agg => agg.GroupHeaderKey == ServiceLineCache.GetServiceLineGroupingFromKey(SelectedServiceLineGroupingKey).ServiceLineGroupHeaderKey)
                        .Count() == 0)
            {
                return;
            }
            if (admissionGroupListBox != null && SelectedServiceLineGroupingKey != null)
            {
                if (admissionGroupListBox == null) return;
                if (admissionGroupTextBlock == null) return;
                if (ServiceLineGroupingListDropDown == null) return;
                var SelectedRow = ServiceLineGroupingListDropDown.Where(s => s.ServiceLineGroupingKey == SelectedServiceLineGroupingKey).FirstOrDefault();
                if (SelectedRow != null)
                {
                    admissionGroupTextBlock.Text = SelectedRow.ServiceLineGroupNameWithInactive;
                }
            }
        }
        private void SetSelectedByDate()
        {
            if( CurrentAdmission == null || CurrentAdmission.AdmissionGroup == null) return;
            var SelItem = GetSelectedAdmissionGroup();
            if (SelItem != null) SelectedServiceLineGroupingKey = SelItem.ServiceLineGroupingKey;
        }
        public AdmissionGroup GetSelectedAdmissionGroup()
        {
            return CurrentAdmission.AdmissionGroup
                .Where(ag => ag.GroupHeaderKey == SelectedGroupHeaderKey
                 && (((CurrentAdmission.AdmissionGroupDate == null || CurrentAdmission.AdmissionGroupDate == DateTime.MinValue))
                        || CurrentAdmission.AdmissionGroupDate != null && CurrentAdmission.AdmissionGroupDate != DateTime.MinValue
                        && ag.StartDate <= CurrentAdmission.AdmissionGroupDate.Date && (ag.EndDate >= CurrentAdmission.AdmissionGroupDate.Date || ag.EndDate == null)))
                .OrderByDescending(a => a.StartDate).ThenBy(a1 => a1.AdmissionGroupKey).FirstOrDefault();
        }
        private void admissionGroupCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.IsDropDownOpen = false;
        }
        void admissionGroupListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }
        private void admissionGroupPopup_Closed(object sender, EventArgs e)
        {
            ValidateGroups();
            SetSelectedByDate();
        }
        private void admissionGroupPopup_Opened(object sender, EventArgs e)
        {
            if(addNew != null) addNew.Content = AddString;
            if (popupListBox != null) popupListBox.ItemsSource = ServiceLineGroupingList;
            ResetItemSource();
                
        }
        #region Add Popup
        private FrameworkElement TriggerButton;
        private Popup PopupControl;
        private PopupProvider _popupProvider;
        private void SetupPopupProvider()
        {
            if ((TriggerButton == null) || (PopupControl == null))
            {
                _popupProvider = null;
                return;
            }
            else
                _popupProvider = new PopupProvider(TriggerButton, TriggerButton, PopupControl, null, Direction.Bottom);

        }

        public RelayCommand<comboBoxServiceLineGrouping> AddAdmissionGroupingCommand { get; set; }
        public RelayCommand<AdmissionGroup> DeleteAdmissionGroupingCommand { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildUnLoaded { get; set; }
        
        private void ServiceLineGroupingSetup()
        {
            AddAdmissionGroupingCommand = new RelayCommand<comboBoxServiceLineGrouping>((newServiceLineGrouping) =>
            {
                if (_popupProvider != null) _popupProvider.TriggerClick();
            });
            DeleteAdmissionGroupingCommand = new RelayCommand<AdmissionGroup>((AdmissionGroupingRow) =>
            {
                AdmissionGroup slgc = (AdmissionGroup)AdmissionGroupingRow;
                if (slgc == null) return;
                
                CurrentAdmission.AdmissionGroup.Remove(slgc);
                if (Model != null) ((IPatientService)Model).Remove(slgc);
                ResetItemSource();
                SetupSelection();
            });
            PopupTriggerLoaded = new RelayCommand<FrameworkElement>((frameworkElement) =>
            {
                TriggerButton = frameworkElement;
                SetupPopupProvider();
            });
            PopupTriggerUnLoaded = new RelayCommand<FrameworkElement>((frameworkElement) =>
            {
                //TriggerButton = null;
                //SetupPopupProvider();
            });
            PopupLoaded = new RelayCommand<FrameworkElement>((frameworkElement) =>
            {
                PopupControl = frameworkElement as Popup;
                SetupPopupProvider();
            });
            PopupUnLoaded = new RelayCommand<FrameworkElement>((frameworkElement) =>
            {
                //PopupControl = null;
                //SetupPopupProvider();
            });
            PopupChildLoaded = new RelayCommand<FrameworkElement>((frameworkElement) =>
            {
                if (_popupProvider != null) _popupProvider.SetPopupChild(frameworkElement);
            });
            PopupChildUnLoaded = new RelayCommand<FrameworkElement>((frameworkElement) =>
            {
                //if (_popupProvider != null) _popupProvider.SetPopupChild(null);
            });

        }
        
        private ServiceLineGrouping _SelectedServiceLineGroupItem = null;
        public ServiceLineGrouping SelectedServiceLineGroupItem
        {
            get { return _SelectedServiceLineGroupItem; }
            set
            {
                _SelectedServiceLineGroupItem = value;
                if (_popupProvider != null) _popupProvider.BeginClosingPopup();
                if (_SelectedServiceLineGroupItem != null && CurrentAdmission != null && CurrentAdmission.AdmissionGroup != null)
                {
                    var prev = CurrentAdmission.AdmissionGroup
                        .Where(ag => ag.GroupHeaderKey == SelectedServiceLineGroupItem.ServiceLineGroupHeaderKey
                            && !ag.EndDate.HasValue).FirstOrDefault();
                    DateTime? eDate = DateTime.Today.AddDays(-1);
                    if (prev != null && prev.EndDate == null)
                        prev.EndDate = (eDate >= prev.StartDate ? eDate : null);

                    AdmissionGroup newGroup = new AdmissionGroup() 
                    { 
                        ServiceLineGroupingKey = _SelectedServiceLineGroupItem.ServiceLineGroupingKey,
                        StartDate = DateTime.Today.Date,
                    };
                    if (CurrentAdmission.AdmissionGroup
                        .Where(agg => agg.GroupHeaderKey == SelectedServiceLineGroupItem.ServiceLineGroupHeaderKey)
                        .Count() == 0
                        && admissionGroupTextBlock != null)
                    {
                        admissionGroupTextBlock.Text = SelectedServiceLineGroupItem.Name;
                        SelectedGroupHeaderKey = SelectedServiceLineGroupItem.ServiceLineGroupHeaderKey;
                        SelectedServiceLineGroupingKey = SelectedServiceLineGroupItem.ServiceLineGroupingKey;
                    }

                    CurrentAdmission.AdmissionGroup.Add(newGroup);
                    ResetItemSource();
                    

                }
            }
        }

        private void ResetItemSource()
        {
            if (admissionGroupListBox != null && CurrentAdmission != null)
            {
                admissionGroupListBox.ItemsSource = null;
                admissionGroupListBox.ItemsSource = CurrentAdmission.AdmissionGroup
                    .Where(ag => ag.GroupHeaderKey == SelectedGroupHeaderKey)
                    .OrderByDescending(a => a.StartDate)
                    .ThenByDescending(a2 => a2.AdmissionGroupKey);
            }
        }
        #endregion
    }
}

