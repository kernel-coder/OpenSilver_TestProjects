using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Virtuoso.Client.Offline;
using Virtuoso.Controls;
using Virtuoso.Core.Interface;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Controls
{
    //http://www.codeproject.com/KB/silverlight/AutoComplete_ComboBox.aspx
    public class SmartCombo : AutoCompleteBox, ICustomCtrlContentPresenter, ICleanup
    {
        enum SearchCategory
        {
            FacilitySearch,
            PhysicianSearch,
            EmployerSearch,
            InsuranceSearch,
            CensusTractSearch,
            OrderTrackingGroupSearch,
            UserProfileSearch,
            UserProfileWithServiceLineSearch,
            SupplySearch
        }

        bool isUpdatingDPs = false;
        SearchDialog window = null;
        private string SearchDataType = "";
        private bool AllowAdd = false;

        private SearchCategory SmartSearchCategory;

        #region "Dependency Values"

        #region "DP - IsContains"

        //KSM 05302014
        public static DependencyProperty IsContainsSearchProperty =
            DependencyProperty.Register("IsContainsSearch", typeof(object), typeof(SmartCombo), new PropertyMetadata(false));

        public bool IsContainsSearch
        {
            get { return ((bool)(base.GetValue(SmartCombo.IsContainsSearchProperty))); }
            set { base.SetValue(SmartCombo.IsContainsSearchProperty, value); }
        }
        //KSM

        #endregion

        #region "DP - IsTabStopCustom"

        public static DependencyProperty IsTabStopCustomProperty =
           DependencyProperty.Register("IsTabStopCustom", typeof(object), typeof(SmartCombo), null);
        public bool IsTabStopCustom
        {
            get { return ((bool)(base.GetValue(IsTabStopCustomProperty))); }
            set { base.SetValue(IsTabStopCustomProperty, value); }
        }

        #endregion

        #region "DP - IncludeSystemSearch"

        public static DependencyProperty IncludeSystemSearchProperty =
           DependencyProperty.Register("IncludeSystemSearch", typeof(object), typeof(SmartCombo), new PropertyMetadata(true));
        public bool IncludeSystemSearch
        {
            get { return ((bool)(base.GetValue(IncludeSystemSearchProperty))); }
            set { base.SetValue(IncludeSystemSearchProperty, value); }
        }

        #endregion

        #region "DP - OverrideItemSource"

        public static DependencyProperty OverrideItemSourceProperty =
           DependencyProperty.Register("OverrideItemSource", typeof(object), typeof(SmartCombo), new PropertyMetadata(false));
        public bool OverrideItemSource
        {
            get { return ((bool)(base.GetValue(OverrideItemSourceProperty))); }
            set { base.SetValue(OverrideItemSourceProperty, value); }
        }

        #endregion

        #region "DP - IncludeMRU"

        public static DependencyProperty IncludeMRUProperty =
            DependencyProperty.Register("IncludeMRU", typeof(object), typeof(SmartCombo), new PropertyMetadata(false));
        public bool IncludeMRU
        {
            get { return ((bool)(base.GetValue(IncludeMRUProperty))); }
            set { base.SetValue(IncludeMRUProperty, value); }
        }

        #endregion

        #region "DP - IncludeEmpty"

        public static DependencyProperty IncludeEmptyProperty =
           DependencyProperty.Register("IncludeEmpty", typeof(object), typeof(SmartCombo), new PropertyMetadata(false));
        public bool IncludeEmpty
        {
            get { return ((bool)(base.GetValue(IncludeEmptyProperty))); }
            set { base.SetValue(IncludeEmptyProperty, value); }
        }

        #endregion

        #region "DP - EmptySelection"

        public static DependencyProperty EmptySelectionValueProperty =
           DependencyProperty.Register("EmptySelectionValue", typeof(object), typeof(SmartCombo), null);
        public object EmptySelectionValue
        {
            get { return ((object)(base.GetValue(EmptySelectionValueProperty))); }
            set { base.SetValue(EmptySelectionValueProperty, value); }
        }

        #endregion

        #region "DP - SelectedValue"

        /// <summary>
        /// SelectedValue Dependency Property
        /// </summary>
        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register("SelectedValue",
                    typeof(object),
                    typeof(SmartCombo),
                    new PropertyMetadata(new PropertyChangedCallback(OnSelectedValueChanged))
                    );

        /// <summary>
        /// Gets or sets the SelectedValue property.
        /// </summary>
        public object SelectedValue
        {
            get { return GetValue(SelectedValueProperty); }
            set { SetValue(SelectedValueProperty, value); }
        }

        /// <summary>
        /// Handles changes to the SelectedValue property.
        /// </summary>
        private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SmartCombo)d).OnSelectedValueChanged(e);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the SelectedValue property.
        /// </summary>
        protected virtual void OnSelectedValueChanged(DependencyPropertyChangedEventArgs e)
        {
            SetSelectedItemUsingSelectedValueDP();
        }
        private void SetSelectedItemUsingSelectedValueDP()
        {
            switch (SmartSearchCategory)
            {
                case SearchCategory.UserProfileSearch:
                    SetSelectedItemUsingSelectedValueDPAsGuid();
                    break;
                case SearchCategory.UserProfileWithServiceLineSearch:
                    LoadSearchItems(); //to ensure that this.ItemSource has Inactived Users - Bug 40074:<Alpha> Care Coordinator is blank in the admission referral - if the user is flagged as "inactive"
                    SetCustomFilter();
                    SetSelectedItemUsingSelectedValueDPAsGuid();
                    break;
                default:
                    SetSelectedItemUsingSelectedValueDPAsInt();
                    break;
            }
        }
        //selects the item whose value is given in SelectedValueDP
        private void SetSelectedItemUsingSelectedValueDPAsInt()
        {
            if (!this.isUpdatingDPs)
            {
                if (this.ItemsSource != null)
                {
                    //In testing BUG 12166, changed this code when discovered that Text was not clearing when binding to a new item after selecting an existing item
                    var isSelectedValueInt32 = this.SelectedValue is Int32;
                    var isEmptyValueInt32 = EmptySelectionValue is Int32;  //in XAML EmptySelectionValue="0" is not Int32, is a string
                    if (!isEmptyValueInt32 && EmptySelectionValue is string)
                    {
                        var _emptySelectionValueAsInt = 0;
                        if (Int32.TryParse(EmptySelectionValue as string, out _emptySelectionValueAsInt))
                            isEmptyValueInt32 = true;
                    }
                    var selectedValueAsInt = Convert.ToInt32(this.SelectedValue);
                    var allNULL = (EmptySelectionValue == null && this.SelectedValue == null);
                    var selectedValueMatchesEmptyValue = (
                        isSelectedValueInt32
                        && selectedValueAsInt <= 0 //Convert.ToInt32(this.SelectedValue) <= 0 
                        && isEmptyValueInt32 //EmptySelectionValue is Int32 <--will never be an int, is always a string...typically="0", because that's all you can set in XAML
                        && (Convert.ToInt32(EmptySelectionValue) == selectedValueAsInt));
                    //FYI: if selectedValue is empty or matches the 'empty' value, then clear the display and remove the current selection
                    if (allNULL || selectedValueMatchesEmptyValue)
                    {
                        this.Text = string.Empty;
                        this.SelectedItem = null;
                    }
                    else
                    {
                        object selectedValue = GetValue(SelectedValueProperty);
                        string propertyPath = this.SelectedValuePath;
                        if (selectedValue != null && !(string.IsNullOrEmpty(propertyPath)))
                        {
                            /// loop through each item in the item source 
                            /// and see if its <SelectedValuePathProperty> == SelectedValue
                            foreach (object item in this.ItemsSource)
                            {
                                PropertyInfo propertyInfo = item.GetType().GetProperty(propertyPath);
                                if (propertyInfo.GetValue(item, null).Equals(selectedValue))
                                {
                                    this.SelectedItem = item;
                                    break;
                                }
                            }
                            //KSM12182014
                            if (selectedValue.Equals(0))
                            {
                                this.SelectedItem = null;
                                this.Text = string.Empty;                                                             
                            }
                            //KSM
                        }
                    }
                }
            }
        }

        private void SetSelectedItemUsingSelectedValueDPAsGuid()
        {
            if (!this.isUpdatingDPs)
            {
                if (this.ItemsSource != null)
                {
                    string selectedValueString = (this.SelectedValue == null) ? string.Empty : this.SelectedValue.ToString();
                    Guid selectedValueGuid = Guid.Empty;
                    Guid.TryParse(selectedValueString as string, out selectedValueGuid);
                    if ((selectedValueGuid == null) || (selectedValueGuid == Guid.Empty))
                    {
                        this.Text = string.Empty;
                        this.SelectedItem = null;
                    }
                    else
                    {
                        object selectedValue = GetValue(SelectedValueProperty);
                        selectedValueString = (selectedValue == null) ? string.Empty : selectedValue.ToString();
                        selectedValueGuid = Guid.Empty;
                        Guid.TryParse(selectedValueString as string, out selectedValueGuid);
                        string propertyPath = this.SelectedValuePath;
                        if (selectedValue != null && !(string.IsNullOrEmpty(propertyPath)))
                        {
                            /// find selectedvalue in the item source 
                            foreach (object item in this.ItemsSource)
                            {
                                PropertyInfo propertyInfo = item.GetType().GetProperty(propertyPath);
                                object p = propertyInfo.GetValue(item, null);
                                Guid? g = p as Guid?;
                                if ((g != null) && (g == selectedValueGuid))
                                {
                                    this.SelectedItem = item;
                                    break;
                                }
                            }
                            if ((selectedValueGuid == null) || (selectedValueGuid == Guid.Empty))
                            {
                                this.SelectedItem = null;
                                this.Text = string.Empty;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region "DP - SelectedValuePath"

        /// <summary>
        /// SelectedValuePath Dependency Property
        /// </summary>
        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register("SelectedValuePath",
                    typeof(string),
                    typeof(SmartCombo),
                    null
                    );

        /// <summary>
        /// Gets or sets the SelectedValuePath property.
        /// </summary>
        public string SelectedValuePath
        {
            get { return GetValue(SelectedValuePathProperty) as string; }
            set { SetValue(SelectedValuePathProperty, value); }
        }

        #endregion

        #region "DP - ServiceLineTypeFilter"

        public static DependencyProperty ServiceLineTypeFilterProperty =
            DependencyProperty.Register("ServiceLineTypeFilter", typeof(int?), typeof(SmartCombo), null );

        public int? ServiceLineTypeFilter
        {
            get { return ((int?)(base.GetValue(SmartCombo.ServiceLineTypeFilterProperty))); }
            set { base.SetValue(SmartCombo.ServiceLineTypeFilterProperty, ServiceLineTypeFilterProperty); }
        }

        #endregion

        #region "DP - FilterPath"

        /// <summary>
        /// FilterPath Dependency Property
        /// </summary>
        public static readonly DependencyProperty FilterPathProperty =
            DependencyProperty.Register("FilterPath",
                    typeof(string),
                    typeof(SmartCombo),
                    null
                    );

        /// <summary>
        /// Gets or sets the FilterPath property.
        /// </summary>
        public string FilterPath
        {
            get { return GetValue(FilterPathProperty) as string; }
            set { SetValue(FilterPathProperty, value); }
        }

        #endregion

        #region "DP - SearchType"

        /// <summary>
        /// SearchType Dependency Property
        /// </summary>
        public static readonly DependencyProperty SearchTypeProperty =
            DependencyProperty.Register("SearchType",
                    typeof(string),
                    typeof(SmartCombo),
                    null
                    );

        /// <summary>
        /// Gets or sets the SearchType property.
        /// </summary>
        public string SearchType
        {
            get { return GetValue(SearchTypeProperty) as string; }
            set
            {
                SetValue(SearchTypeProperty, value);
                switch (SearchType)
                {
                    case "ActiveFacilities":
                        SmartSearchCategory = SearchCategory.FacilitySearch;
                        SearchDataType = "Facility";
                        break;

                    case "Physician":
                        SmartSearchCategory = SearchCategory.PhysicianSearch;
                        SearchDataType = "Physician";
                        //AllowAdd = true;  //Bug 21306:<R35>Physician DDL showing Add new - use end user's role(Maintenance)
                        break;

                    case "Employer":
                        SmartSearchCategory = SearchCategory.EmployerSearch;
                        SearchDataType = "Employer";
                        break;

                    case "Insurance":
                        SmartSearchCategory = SearchCategory.InsuranceSearch;
                        SearchDataType = "Insurance";
                        break;

                    case "Facility":
                        SmartSearchCategory = SearchCategory.FacilitySearch;
                        SearchDataType = "Facility";
                        AllowAdd = false;
                        break;

                    case "CensusTract":
                        SmartSearchCategory = SearchCategory.CensusTractSearch;
                        SearchDataType = "Census Tract";
                        AllowAdd = true;
                        break;

                    case "OrderTrackingGroup":
                        SmartSearchCategory = SearchCategory.OrderTrackingGroupSearch;
                        SearchDataType = "TrackingGroups";  // DE43893 - Was 'OrderTrackingGroup'
                        break;

                    case "UserProfile":
                        SmartSearchCategory = SearchCategory.UserProfileSearch;
                        SearchDataType = "UserProfile";
                        AllowAdd = false;
                        break;

                    case "UserProfileWithServiceLine":
                        SmartSearchCategory = SearchCategory.UserProfileWithServiceLineSearch;
                        SearchDataType = "UserProfileWithServiceLine";
                        AllowAdd = false;
                        break;
                    case "Supply":
                        SmartSearchCategory = SearchCategory.SupplySearch;
                        SearchDataType = "Supply";
                        break;

                    default:
                        throw new InvalidDataException("An invalid Search type of " + SearchType + " was passed to instantiate SmartCombo control.");
                        //break;
                }

                // Moved to deal with the selected item for inactive items.
                //LoadSearchItems();
                //SetCustomFilter();
            }
        }

        #endregion

        #region "DP - SelectedServiceLine"

        /// <summary>
        /// SelectedValue Dependency Property
        /// </summary>
        public static readonly DependencyProperty SelectedServiceLineProperty =
            DependencyProperty.Register("SelectedServiceLine",
                    typeof(object),
                    typeof(SmartCombo),
                    new PropertyMetadata(new PropertyChangedCallback(OnSelectedServiceLineChanged))
                    );

        /// <summary>
        /// Gets or sets the SelectedServiceLine property.
        /// </summary>
        public object SelectedServiceLine
        {
            get { return GetValue(SelectedServiceLineProperty); }
            set { SetValue(SelectedServiceLineProperty, value); }
        }

        /// <summary>
        /// Handles changes to the SelectedValue property.
        /// </summary>
        private static void OnSelectedServiceLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SmartCombo)d).OnSelectedServiceLineChanged(e);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the SelectedValue property.
        /// </summary>
        protected virtual void OnSelectedServiceLineChanged(DependencyPropertyChangedEventArgs e)
        {
            if (SelectedServiceLine != null)
            {
                var key = (int?)SelectedServiceLine;
                var buttonSearch = (Button)GetTemplateChild("ButtonComboSearch");
                if (buttonSearch != null)
                {
                    buttonSearch.IsEnabled = (key.HasValue);
                }
            }
            LoadSearchItems();
            SetCustomFilter();
            SetSelectedItemUsingSelectedValueDP();  //ServiceLine changed - make sure 
        }

        #endregion

        #endregion

        public SmartCombo()
            : base()
        {
            //KSM03062015
            // This is important if user is allowed to add records thru samrt search combo, 
            // please add messaging to the "save Completed" event on the veiw model  or control logic
            // Otherwise the itemsource does not update when records are added even though the cache does update
            //Add:
            // Messenger.Default.Send(true, "CacheChanged");
            //KSM
            this.IsTabStopCustom = true; 
            this.DefaultStyleKey = typeof(SmartCombo);
            Messenger.Default.Register<bool>(this, "CacheChanged", (b) => {
                LoadSearchItems();
                SetCustomFilter();
                SetSelectedItemUsingSelectedValueDP();
            });
        }

        void Current_NetworkAvailabilityChanged(object sender, Client.Offline.Events.NetworkAvailabilityEventArgs e)
        {
            SetButtonSearchVisibility(e.IsAvailable);
        }

        private void SetButtonSearchVisibility(bool isOnline)
        {
            //disable/enable buttonSearch when offline/online
            var buttonSearch = (Button)GetTemplateChild("ButtonComboSearch");
            if (buttonSearch != null)
            {
                buttonSearch.Visibility = isOnline ? Visibility.Visible : Visibility.Collapsed;
            }            
        }

        private void LoadSearchItems()
        {
            if (!OverrideItemSource)
            {
                switch (SmartSearchCategory)
                {
                    case SearchCategory.FacilitySearch:
                        ItemsSource = new FacilitySmartSearch<Facility>().SmartSearchData;
                        break;

                    case SearchCategory.PhysicianSearch:
                        List<Physician> psList = new PhysicianSmartSearch<Physician>(IncludeEmpty, (SelectedValue as int? == null ? null : (int?)SelectedValue)).SmartSearchData;
                        if ((psList != null) && (ServiceLineTypeFilter != null)) ItemsSource = psList.Where(s => (((ServiceLineTypeFilter & s.ServiceLineTypeUseBits) != 0) || (s.PhysicianKey == (int?)SelectedValue))).ToList(); 
                        else ItemsSource = ItemsSource = psList;
                        break;

                    case SearchCategory.EmployerSearch:
                        ItemsSource = new EmployerSmartSearch<Employer>().SmartSearchData;
                        break;

                    case SearchCategory.InsuranceSearch:
                        ItemsSource = new InsuranceSmartSearch<Insurance>().SmartSearchData;
                        break;

                    case SearchCategory.CensusTractSearch:
                        ItemsSource = new CensusTractSmartSearch<CensusTract>().SmartSearchData;
                        break;

                    case SearchCategory.OrderTrackingGroupSearch:
                        ItemsSource = new OrderTrackingGroupSmartSearch<OrderTrackingGroup>().SmartSearchData;
                        break;

                    case SearchCategory.UserProfileSearch:
                        ItemsSource = new UserProfileSmartSearch<UserProfile>(
                            IncludeEmpty, 
                            (SelectedValue as Guid? == null ? null : (Guid?)SelectedValue)).SmartSearchData;
                        break;

                    case SearchCategory.UserProfileWithServiceLineSearch:
                        ItemsSource = new UserProfileWithServiceLineSearch<UserProfile>(
                            IncludeEmpty,
                            (SelectedValue as Guid? == null ? null : (Guid?)SelectedValue),
                            (SelectedServiceLine as int? == null ? null : (int?)SelectedServiceLine)).SmartSearchData;
                        break;
                    case SearchCategory.SupplySearch:
                        ItemsSource = new SupplySmartSearch<Supply>().SmartSearchData;
                        break;

                    default:
                        throw new InvalidDataException("An invalid Search type of " + SmartSearchCategory.ToString() + " was passed to LoadSearchItems.");
                }
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            SetElementEvents();
#if OPENSILVER
            var listBox = GetTemplateChild("Selector") as ListBox;
            if (listBox != null)
            {
                listBox.CustomLayout = true;
            }

            var popupBorder = GetTemplateChild("PopupBorder") as Border;
            if (popupBorder != null)
            {
                Canvas.SetTop(popupBorder, 30);
            }
#endif
            LoadSearchItems();
            SetCustomFilter();
            SetSelectedItemUsingSelectedValueDP();

            var buttonSearch = (Button)GetTemplateChild("ButtonComboSearch");
            if (buttonSearch != null)
            {
                if (IncludeSystemSearch)
                {
                    buttonSearch.Visibility = Visibility.Visible;
                }
                else
                {
                    buttonSearch.Visibility = Visibility.Collapsed;
                }
            }

            SetButtonSearchVisibility(EntityManager.Current.IsOnline);
            EntityManager.Current.NetworkAvailabilityChanged += Current_NetworkAvailabilityChanged;

            //Set the default to collapsed for the display 
            HideMRU();
            if (IncludeMRU == true)
            {
                var itemsMRU = (ListBox)GetTemplateChild("SelectorMRU");
                if (itemsMRU != null)
                {
                    switch (SmartSearchCategory)
                    {
                        case SearchCategory.EmployerSearch:
                            itemsMRU.ItemsSource = new EmployerSmartSearch<Employer>().SmartSearchDataMRU;
                            break;

                        case SearchCategory.FacilitySearch:
                            itemsMRU.ItemsSource = new FacilitySmartSearch<Facility>().SmartSearchDataMRU;
                            break;

                        case SearchCategory.PhysicianSearch:
                            List<Physician> psList = new PhysicianSmartSearch<Physician>().SmartSearchDataMRU;
                            if ((psList != null) && (ServiceLineTypeFilter != null)) itemsMRU.ItemsSource = psList.Where(s => ((ServiceLineTypeFilter & s.ServiceLineTypeUseBits) != 0)).ToList();
                            else itemsMRU.ItemsSource = psList;
                            break;

                        case SearchCategory.CensusTractSearch:
                            itemsMRU.ItemsSource = new CensusTractSmartSearch<CensusTract>().SmartSearchDataMRU;
                            break;

                        case SearchCategory.UserProfileSearch:
                            itemsMRU.ItemsSource = new UserProfileSmartSearch<UserProfile>().SmartSearchDataMRU;
                            break;

                        case SearchCategory.UserProfileWithServiceLineSearch:
                            itemsMRU.ItemsSource = new UserProfileWithServiceLineSearch<UserProfile>().SmartSearchDataMRU;
                            break;

                        case SearchCategory.SupplySearch:
                            itemsMRU.ItemsSource = new SupplySmartSearch<Supply>().SmartSearchDataMRU;
                            break;

                        default:
                            throw new InvalidDataException("An invalid Search type of " + SmartSearchCategory.ToString() + " was passed to load MRU Search Items.");
                            //break;
                    }
                    ShowMRU();
                }
            }
        }

        #region "User Interaction Events"

        void checkBoxIncludeMRU_Click(object sender, RoutedEventArgs e)
        {
            var checkIncludeMRU = (CheckBox)sender;
            if (checkIncludeMRU.IsChecked == true)
            {
                ShowMRU();
            }
            else
            {
                HideMRU();
            }
        }

        void itemsMRU_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = ((ListBox)(sender)).SelectedItem;
            {

                switch (SmartSearchCategory)
                {
                    case SearchCategory.EmployerSearch:
                        SelectedValue = ((Employer)selected).EmployerKey;
                        break;

                    case SearchCategory.FacilitySearch:
                        SelectedValue = ((Facility)selected).FacilityKey;
                        break;

                    case SearchCategory.PhysicianSearch:
                        SelectedValue = ((Physician)selected).PhysicianKey;
                        break;

                    case SearchCategory.CensusTractSearch:
                        SelectedValue = ((CensusTract)selected).CensusTractKey;
                        break;

                    case SearchCategory.UserProfileSearch:
                        SelectedValue = ((UserProfile)selected).UserId;
                        break;

                    case SearchCategory.UserProfileWithServiceLineSearch:
                        SelectedValue = ((UserProfile)selected).UserId;
                        break;

                    case SearchCategory.SupplySearch:
                        SelectedValue = ((Supply)selected).SupplyKey;
                        break;

                    default:
                        throw new InvalidDataException("An invalid Search type of " + SmartSearchCategory.ToString() + " was passed to itemsMRU_SelectionChanged.");
                        //break;
                }

                SetSelectedItemUsingSelectedValueDP();

                var toggle = GetTemplateChild("DropDownToggle") as ToggleButton;
                if (toggle != null)
                {
                    ToggleParentAutoCompleteDropDown(toggle);
                }
            }
        }

        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            if (window != null)
            {
                SearchPanelViewModel vmw = window.ParentViewModel as SearchPanelViewModel;
                if (vmw != null)
                {
                    vmw.AllowAddNewOverride = AllowAdd;
                    vmw.ServiceLineTypeFilter = ServiceLineTypeFilter;
                    vmw.ItemSelected -= vm_ItemSelected;
                }
                window.Cleanup();
                window = null;
            }

            window = new SearchDialog();
            if (SearchDataType == "UserProfileWithServiceLine")
                window.CurrentSearchOverride = "UserProfile";
            else
                window.CurrentSearchOverride = SearchDataType;
            SearchPanelViewModel vm = window.ParentViewModel as SearchPanelViewModel;
            if (vm != null)
            {
                vm.AllowAddNewOverride = AllowAdd;
                vm.ServiceLineTypeFilter = ServiceLineTypeFilter;
                vm.ItemSelected += vm_ItemSelected;
            }
            window.InitSearch(restoreSearchState:true, serviceLineKey: (int?)this.SelectedServiceLine);
            window.Closed += new EventHandler(
                (s, eArgs) =>
                {
                    if (window != null)
                    {
                        SearchPanelViewModel vmw = window.ParentViewModel as SearchPanelViewModel;
                        if (vmw != null)
                        {
                            vmw.AllowAddNewOverride = AllowAdd;
                            vm.ServiceLineTypeFilter = ServiceLineTypeFilter;
                            vmw.ItemSelected -= vm_ItemSelected;
                        }
                        window.Cleanup();
                        window = null;
                    }
                }
            );
            window.Show();
        }

        private void vm_ItemSelected(object sender, MenuEventArgs e)
        {

            //PatientSearch LastSelection = null;

            switch (SmartSearchCategory)
            {
                case SearchCategory.UserProfileSearch:
                    SelectedValue = e.ID;
                    SetSelectedItemUsingSelectedValueDPAsGuid();
                break;
                case SearchCategory.UserProfileWithServiceLineSearch:
                    SelectedValue = e.ID;
                    SetSelectedItemUsingSelectedValueDPAsGuid();
                break;
                default:
                    int id = 0;
                    if (int.TryParse(e.ID, out id))
                    {
                        SelectedValue = id;
                        SetSelectedItemUsingSelectedValueDPAsInt();
                    }
                    break;
            }
            window.Close();
        }

        private void DropDownToggle_Click(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            ToggleParentAutoCompleteDropDown(fe);
        }

        #endregion

        #region "Element Visibility"

        private void ToggleParentAutoCompleteDropDown(FrameworkElement fe)
        {

            AutoCompleteBox autoCompleteBox = null;
            while (fe != null && autoCompleteBox == null)
            {
                fe = VisualTreeHelper.GetParent(fe) as FrameworkElement;
                autoCompleteBox = fe as AutoCompleteBox;
            }

            if (autoCompleteBox != null)
            {
                autoCompleteBox.IsDropDownOpen = !autoCompleteBox.IsDropDownOpen;
            }

        }

        public void ShowMRU()
        {
            var stackPanelMRUContainer = (StackPanel)GetTemplateChild("StackPanelMRUContainer");
            if (stackPanelMRUContainer != null)
            {
                stackPanelMRUContainer.Visibility = Visibility.Visible;
            }
        }

        public void HideMRU()
        {
            var stackPanelMRUContainer = (StackPanel)GetTemplateChild("StackPanelMRUContainer");
            if (stackPanelMRUContainer != null)
            {
                stackPanelMRUContainer.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        protected virtual void SetCustomFilter()
        {
            //custom logic: how to autocomplete 
            this.ItemFilter = (prefix, item) =>
            {
                //return all items for empty prefix
                if (string.IsNullOrEmpty(prefix))
                    return true;

                //return all items if a record is already selected
                if (this.SelectedItem != null)
                    if (this.SelectedItem.ToString() == prefix)
                        return true;

                if (ServiceLineTypeFilter != null)
                {
                    //ServiceLineTypeFilter only applies to Physician for now
                    Physician p = item as Physician;
                    if (p != null)
                    {
                        if ((ServiceLineTypeFilter & p.ServiceLineTypeUseBits) == 0)
                        {
                            return false;
                        }
                    }
                }

                //Add logic to handle invlid FilterPath values
                //Should we add multiple filter paths?
                var objectFilterValue = item.ToString().ToLower();
                if (FilterPath != null)
                {
                    objectFilterValue = item.GetType().GetProperty(FilterPath).GetValue(item, null).ToString().ToLower();
                }

                if (!IsContainsSearch)
                {
                    if (prefix.Length == 1)
                        return objectFilterValue.StartsWith(prefix.ToLower());
                    else
                        return objectFilterValue.Contains(prefix.ToLower());
                }
                else return objectFilterValue.Contains(prefix.ToLower());
            };
        }

        //highlighting logic
        protected override void OnPopulated(PopulatedEventArgs e)
        {
            base.OnPopulated(e);
            ListBox listBox = GetTemplateChild("Selector") as ListBox;
            if (listBox != null)
            {
                //highlight the selected item, if any
                if (this.ItemsSource != null && this.SelectedItem != null)
                {
                    listBox.SelectedItem = this.SelectedItem;
                    listBox.Dispatcher.BeginInvoke(delegate
                    {
                        listBox.UpdateLayout();
                        listBox.ScrollIntoView(listBox.SelectedItem);
                    });
                }
            }
        }

        protected override void OnDropDownClosed(RoutedPropertyChangedEventArgs<bool> e)
        {
            base.OnDropDownClosed(e);
            UpdateCustomDPs();
        }

        public void UpdateCustomDPs()
        {
            //flag to ensure that that we dont reselect the selected item
            this.isUpdatingDPs = true;

            //if a new item is selected or the user blanked out the selection, update the DP
            if (this.SelectedItem != null || this.Text == string.Empty)
            {
                //update the SelectedValue DP 
                string propertyPath = this.SelectedValuePath;
                if (!string.IsNullOrEmpty(propertyPath))
                {
                    if (this.SelectedItem != null)
                    {
                        PropertyInfo propertyInfo = this.SelectedItem.GetType().GetProperty(propertyPath);

                        //get property from selected item
                        object propertyValue = propertyInfo.GetValue(this.SelectedItem, null);

                        //update the datacontext
                        this.SelectedValue = propertyValue;
                    }
                    else
                    {
                        this.SelectedValue = null;
                    }
                }
            }
            else
            {
                if (this.GetBindingExpression(SelectedValueProperty) != null)
                {
                    this.isUpdatingDPs = false;
                    SetSelectedItemUsingSelectedValueDP();
                }
            }

            this.isUpdatingDPs = false;
        }

        public void SetElementEvents()
        {
            var toggle = (ToggleButton)GetTemplateChild("DropDownToggle");
            if (toggle != null)
            {
                toggle.Click += DropDownToggle_Click;
            }

            var checkBoxIncludeMRU = (CheckBox)GetTemplateChild("CheckBoxIncludeMRU");
            if (checkBoxIncludeMRU != null)
            {
                checkBoxIncludeMRU.Click += checkBoxIncludeMRU_Click;
            }

            var buttonSearch = (Button)GetTemplateChild("ButtonComboSearch");
            if (buttonSearch != null)
            {
                buttonSearch.Click += ButtonSearch_Click;
            }

            var itemsMRU = (ListBox)GetTemplateChild("SelectorMRU");
            if (itemsMRU != null)
            {
                itemsMRU.SelectionMode = SelectionMode.Single;
                itemsMRU.SelectionChanged += itemsMRU_SelectionChanged;
            }
        }

        public void Cleanup()
        {
            if (window != null)
            {
                SearchPanelViewModel vm = window.ParentViewModel as SearchPanelViewModel;
                if (vm != null)
                {
                    vm.AllowAddNewOverride = AllowAdd;
                    vm.ServiceLineTypeFilter = ServiceLineTypeFilter;
                    vm.ItemSelected -= vm_ItemSelected;
                }
                window.Cleanup();
                window = null;
            }

            var toggle = (ToggleButton)GetTemplateChild("DropDownToggle");
            if (toggle != null)
            {
                toggle.Click -= DropDownToggle_Click;
            }

            var buttonSearch = (Button)GetTemplateChild("ButtonComboSearch");
            if (buttonSearch != null)
            {
                buttonSearch.Click -= ButtonSearch_Click;
            }

            var mruListBox = GetTemplateChild("SelectorMRU") as ListBox;
            mruListBox?.Items.Clear();

            var itemsList = this.ItemsSource as IList;
            itemsList?.Clear();

            EntityManager.Current.NetworkAvailabilityChanged -= Current_NetworkAvailabilityChanged;

            Messenger.Default.Unregister(this);
        }
    }
}