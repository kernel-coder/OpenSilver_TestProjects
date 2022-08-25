using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Model;
using Virtuoso.Portable.Model;

namespace Virtuoso.Core.Controls
{

    public class AddressMappingControl : ListBox
    {
        public Boolean CBSAVisible 
        {
            get { return (Boolean)this.GetValue(CBSAVisibleProperty); }
            set { this.SetValue(CBSAVisibleProperty, value); }
        }
        public static readonly DependencyProperty CBSAVisibleProperty = DependencyProperty.Register(
          "CBSAVisible", typeof(Boolean), typeof(AddressMappingControl), new PropertyMetadata(true));


        public ComboBox ControlComboBox = null;
        public StackPanel CBSAContainer = null;
        private bool _matchAllSearchParms;
        private string _localCounty = "";
        private String _stateSearchParm = "";
        private String _zipCodeSearchParm = "";
        public String zipCodeSearchParm
        {
            get {
                if (_zipCodeSearchParm == null)
                    _zipCodeSearchParm = ""; //SetSearchParms() sets to null when adding a new PatientAddress, then SearchForAddress() was dereferencing that null
                return _zipCodeSearchParm; 
            }
            set { 
                _zipCodeSearchParm = value; 
            }
        }
        private DateTime? _effectiveFromSearchParm;
        private DateTime? _effectiveToSearchParm;

        public AddressMappingControl()
        {
            Style = (Style)System.Windows.Application.Current.Resources["CoreAddressMapStyle"];

            KeyUp += ControlKeyUp;

            this.IsTabStop = false;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ControlComboBox = (ComboBox)GetTemplateChild("ControlComboBox");
            if (ControlComboBox != null)
            {
                ControlComboBox.SelectionChanged -= ControlComboBox_SelectionChanged;
                ControlComboBox.SelectionChanged += ControlComboBox_SelectionChanged;
                ControlComboBox.KeyUp -= ControlKeyUp;
                ControlComboBox.KeyUp += ControlKeyUp;
                ControlComboBox.MouseEnter -= ControlComboBox_MouseEnter;
                ControlComboBox.MouseEnter += ControlComboBox_MouseEnter;
            }

            CBSAContainer = (StackPanel)GetTemplateChild("CBSAContainer");
            if (CBSAContainer != null)
            {
                CBSAContainer.KeyUp -= ControlKeyUp;
                CBSAContainer.KeyUp += ControlKeyUp;

                if (this.CBSAVisible) this.CBSAContainer.Visibility = System.Windows.Visibility.Visible;
                else this.CBSAContainer.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        void ControlComboBox_MouseEnter(object sender, MouseEventArgs e)
        {
            if (ControlComboBox.SelectedItem == null && ControlComboBox.Items != null && ControlComboBox.Items.Count > 0)
            {
                ControlComboBox.IsDropDownOpen = true;
            }
        }

        private void RefreshSearchList(bool matchAll)
        {
            _matchAllSearchParms = matchAll;
            Deployment.Current.Dispatcher.BeginInvoke(SearchForAddress);
        }

        private async void ControlComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ControlComboBox.SelectedItem != null)
            {
                var ic = ControlComboBox.SelectedItem as CachedAddressMapping;
                if (ic != null)
                {
                    State = CodeLookupCache.GetKeyFromCode("STATE", ic.State);
                    County = ic.CountyCode;
                    if (zipCodeSearchParm.Length < 10 || City == null) City = await SetCity(City, ic);
                    CBSAHomeHealth = ic.CBSAHomeHealth;
                    CBSAHospice = ic.CBSAHospice;
                    if (zipCodeSearchParm.Length < 10 || ZipCode == null) ZipCode = ic.ZipCode;
                    DataContext =  ic;
                }
            }
            else
            {
                var cachedAddressMapping = new CachedAddressMapping
                {
                    City = City == null ? null : City.ToString(),
                    State = State == null ? null : State.ToString(),
                    ZipCode = ZipCode == null ? null : ZipCode.ToString()
                };

                DataContext = cachedAddressMapping;
            }
        }
        private async Task<string> SetCity(object pCurrentCity, CachedAddressMapping cam)
        {
            // Keep the pCurrentCity passed if is still a match - otherwise use the selected CachedAddressMapping city - e.g.,
            // ZipCode State County    City
            // 16667   PA    BEDFORD   OSTERBURG
            // 16667   PA    BEDFORD   ST CLAIRSVILLE
            // if ST CLAIRSVILLE was picked before BEDFORD - don't override city to OSTERBURG
            string currentCity = (pCurrentCity == null) ? null : pCurrentCity.ToString().Trim().ToUpper();
            if (cam == null) return null;
            if (string.IsNullOrWhiteSpace(currentCity) == true) return cam.City;
            IEnumerable camMatchIEnumerable = await AddressMappingCache.Current.GetZIPCodes();
            if (camMatchIEnumerable == null) return cam.City;
            List<ZIPCode> camMatchList = camMatchIEnumerable.Cast<ZIPCode>().ToList();
            if (camMatchList == null) return cam.City;
            ZIPCode camMatchOnCity = camMatchList.Where(p => ((p.ZipCode == cam.ZipCode.Substring(0, 5)) && (p.State == cam.State) && (p.County == cam.CountyCode) && (p.City.Trim().ToUpper() == currentCity) )).FirstOrDefault();
            if (camMatchOnCity != null) return currentCity;
            return cam.City;
        }

        private void ClearCounty()
        {
            County = null;
            CBSAHomeHealth = null;
            CBSAHospice = null;
        }

        private void ControlKeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Delete) || (e.Key == Key.Back))
            {
                ClearSearchParms();
            }
        }

        private void ClearSearchParms()
        {
            State = null;
            County = null;
            City = null;
            CBSAHomeHealth = null;
            CBSAHospice = null;
            ZipCode = null;
            if (ControlComboBox != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    ControlComboBox.ItemsSource = null;
                });
            }
            DataContext = null;
            DataContext = new CachedAddressMapping();
        }

        private void SearchForAddress()
        {
            if (ControlComboBox == null) ApplyTemplate();

            if (SearchParmsHaveChanged() && ValidateSearchParms())
            {
                SetSearchParms();

                if (ControlComboBox != null && SearchParmsHaveValue())
                {
                    ControlComboBox.ItemsSource = null;

                    ThreadPool.QueueUserWorkItem(async _ =>
                    {
                        var itemsSource = await AddressMappingSearch.Search(
                            zipCodeSearchParm.Substring(0, 5),
                            _stateSearchParm,
                            _effectiveFromSearchParm,
                            _effectiveToSearchParm);

                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            ControlComboBox.ItemsSource = itemsSource;

                            var items = ControlComboBox.ItemsSource as IList<CachedAddressMapping> ??
                                                  ControlComboBox.ItemsSource.Cast<CachedAddressMapping>().ToList();
                            
                            var count = items.Count();
                            switch (count)
                            {
                                case 0:
                                    ClearCounty();
                                    break;
                                case 1:
                                    var fitem = items.FirstOrDefault();
                                    if (zipCodeSearchParm.Length == 10) fitem.ZipCode = zipCodeSearchParm;
                                    ControlComboBox.SelectedItem = fitem;

                                    break;
                                default:
                                    if (count > 1)
                                    {
                                        if (County == null)
                                        {
                                            ControlComboBox.IsDropDownOpen = true;
                                        }
                                        else
                                        {
                                            if (items.Any(c => c.CountyCode == (string)County) && !_zipCodeChanged)
                                            {
                                                fitem =  items.FirstOrDefault(c => c.CountyCode == (string)County);
                                                if (zipCodeSearchParm.Length == 10) fitem.ZipCode = zipCodeSearchParm;
                                                ControlComboBox.SelectedItem = fitem;
                                            }
                                            else
                                            {
                                                ControlComboBox.IsDropDownOpen = true;
                                            }
                                        }
                                    }
                                    break;
                            }
                        });
                    });
                }
            }
        }

        private bool SearchParmsHaveValue()
        {
            if (_matchAllSearchParms)
            {
                return !String.IsNullOrEmpty(zipCodeSearchParm) &&
                        zipCodeSearchParm.Length > 4 &&
                       !String.IsNullOrEmpty(_stateSearchParm);
            }

            return !String.IsNullOrEmpty(zipCodeSearchParm);
        }

        private void SetSearchParms()
        {
            zipCodeSearchParm = ZipCode as string;
            _stateSearchParm = State == null ? "" : CodeLookupCache.GetCodeFromKey("STATE", (int)State);
            _effectiveFromSearchParm = EffectiveFromDate as DateTime?;
            _effectiveToSearchParm = EffectiveThruDate as DateTime?;
            _localCounty = County as string;
            if (ZipCode == null && City == null && State == null)
            {
                ClearSearchParms();
            }
        }

        private bool _zipCodeChanged = false;

        private bool SearchParmsHaveChanged()
        {
            _zipCodeChanged = zipCodeSearchParm != (string)ZipCode;

            return zipCodeSearchParm != (string)ZipCode ||
                   _effectiveFromSearchParm != (DateTime?)EffectiveFromDate ||
                   _effectiveToSearchParm != (DateTime?)EffectiveThruDate ||
                   _localCounty != (string)County ||
                   _stateSearchParm != GetStateCode();
        }

        private string GetStateCode()
        {
            return State != null ? CodeLookupCache.GetCodeFromKey("STATE", (int)State) : "";
        }

        private bool ValidateSearchParms()
        {
            if (ZipCode != null)
            {
                var postalCodeRegex = new Regex(@"^\d{5}$|^\d{5}-\d{4}$", RegexOptions.CultureInvariant);
                var m = postalCodeRegex.Match(ZipCode.ToString());
                if (!m.Success) return false;
            }

            return true;
        }

        #region Dependency Properties

        public static DependencyProperty CBSAHospiceProperty =
            DependencyProperty.Register("CBSAHospice", typeof(string), typeof(AddressMappingControl), null);

        public static DependencyProperty CBSAHomeHealthProperty =
            DependencyProperty.Register("CBSAHomeHealth", typeof(string), typeof(AddressMappingControl), null);

        public static DependencyProperty CountyProperty =
            DependencyProperty.Register("County", typeof(string), typeof(AddressMappingControl), null);

        public static DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(int?), typeof(AddressMappingControl),
                                        new PropertyMetadata((o, e) => ((AddressMappingControl)o).RefreshSearchList(true)));

        public static DependencyProperty CityProperty =
            DependencyProperty.Register("City", typeof(string), typeof(AddressMappingControl),
                                        new PropertyMetadata((o, e) => ((AddressMappingControl)o).RefreshSearchList(true)));

        public static DependencyProperty ZipCodeProperty =
            DependencyProperty.Register("ZipCode", typeof(object), typeof(AddressMappingControl),
                                        new PropertyMetadata((o, e) => ((AddressMappingControl)o).RefreshSearchList(false)));

        public static DependencyProperty EffectiveFromDateProperty =
            DependencyProperty.Register("EffectiveFromDate", typeof(object), typeof(AddressMappingControl),
                                        new PropertyMetadata((o, e) => ((AddressMappingControl)o).RefreshSearchList(true)));

        public static DependencyProperty EffectiveThruDateProperty =
            DependencyProperty.Register("EffectiveThruDate", typeof(object), typeof(AddressMappingControl),
                                        new PropertyMetadata((o, e) => ((AddressMappingControl)o).RefreshSearchList(true)));

        public object CBSAHospice
        {
            get
            {
                var s = ((string)(GetValue(CBSAHospiceProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set { SetValue(CBSAHospiceProperty, value); }
        }

        public object CBSAHomeHealth
        {
            get
            {
                var s = ((string)(GetValue(CBSAHomeHealthProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set { SetValue(CBSAHomeHealthProperty, value); }
        }

        public object State
        {
            get
            {
                var s = ((int?)(GetValue(StateProperty)));
                return s;
            }
            set { SetValue(StateProperty, value); }
        }

        public object County
        {
            get
            {
                var s = ((string)(GetValue(CountyProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set { SetValue(CountyProperty, value); }
        }

        public object City
        {
            get
            {
                var s = ((string)(GetValue(CityProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set 
            {
                SetValue(CityProperty, value);
            }
        }

        public object ZipCode
        {
            get
            {
                var s = ((string)(GetValue(ZipCodeProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set { SetValue(ZipCodeProperty, value); }
        }

        public object EffectiveFromDate
        {
            get
            {
                var effectiveFrom = ((DateTime?)(GetValue(EffectiveFromDateProperty)));
                return effectiveFrom;
            }
            set { SetValue(EffectiveFromDateProperty, value); }
        }

        public object EffectiveThruDate
        {
            get
            {
                var effectiveTo = ((DateTime?)(GetValue(EffectiveThruDateProperty)));
                return effectiveTo;
            }
            set { SetValue(EffectiveThruDateProperty, value); }
        }

        #endregion
    }
}