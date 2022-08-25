using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Virtuoso.Core.Services;
using Virtuoso.Server.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Virtuoso.Core.Cache;
using GalaSoft.MvvmLight;


namespace Virtuoso.Core.Controls
{
    public class FunctionalDeficitMulti : System.Windows.Controls.ComboBox, ICleanup
    {
        public event EventHandler SelectedTextChanged;
        private List<String> _itemsSource = null;
        private List<String> _codeDescriptions = new List<String>();
        private string _itemNone = null;
        private bool _loaded = false;
        private TextBlock controlTextBlock = null;
        private ScrollViewer scrollViewer = null;
        private TextBox controlOtherTextBox = null;
        private StackPanel controlOtherStackPanel = null;
        private StackPanel controlOtherStackPanel2 = null;
        private ListBox controlListBox = null;
        private Popup controlPopup = null;
        private Button controlCloseButton = null;
        private static string OTHER = "Other";
        private DispatcherTimer _doubleClickTimer;
        private bool isDoubleClickClose = false;

        public FunctionalDeficitMulti()
        {
            this.SingleSelect = false;
            this.Loaded += new RoutedEventHandler(FunctionalDeficitMulti_Loaded);
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreFunctionalDeficitMultiStyle"]; }
            catch { }
            _doubleClickTimer = new DispatcherTimer();
            _doubleClickTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _doubleClickTimer.Tick += new EventHandler(DoubleClick_Timer);
            this.DropDownOpened += new EventHandler(FunctionalDeficitMulti_DropDownOpened);
            this.DropDownClosed += new EventHandler(FunctionalDeficitMulti_DropDownClosed);
        }
        public void Cleanup()
        {
            this.Loaded += new RoutedEventHandler(FunctionalDeficitMulti_Loaded);
            _doubleClickTimer.Tick -= DoubleClick_Timer;
            this.DropDownOpened -= FunctionalDeficitMulti_DropDownOpened;
            this.DropDownClosed -= FunctionalDeficitMulti_DropDownClosed;
            if (_codeDescriptions != null) _codeDescriptions.Clear();
            _codeDescriptions = null;
            if (_itemsSource != null) _itemsSource.Clear();
            _itemsSource = null;
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
            }
            controlOtherStackPanel = (StackPanel)GetTemplateChild("ControlOtherStackPanel");
            controlOtherStackPanel2 = (StackPanel)GetTemplateChild("ControlOtherStackPanel2");
        }

        void FunctionalDeficitMulti_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_loaded) LoadCodes();
            if (SingleSelect == false) return;
            if (controlListBox == null) this.ApplyTemplate();
            if (controlListBox != null) controlListBox.SelectionMode = SelectionMode.Single;
        }

        private void LoadCodes()
        {
            _loaded = true;
            List<Virtuoso.Server.Data.FunctionalDeficit> _FunctionalDeficits = FunctionalDeficitCache.GetFunctionalDeficitsFromQuestionKey(QuestionKey);
            _codeDescriptions = new List<String>();
            if (_FunctionalDeficits != null) foreach (FunctionalDeficit cl in _FunctionalDeficits) _codeDescriptions.Add(cl.Description.Trim());
        }

        public static DependencyProperty QuestionKeyProperty =
          DependencyProperty.Register("QuestionKey", typeof(int), typeof(Virtuoso.Core.Controls.FunctionalDeficitMulti), null);
        public int QuestionKey
        {
            get { return ((int)(base.GetValue(FunctionalDeficitMulti.QuestionKeyProperty))); }
            set { base.SetValue(FunctionalDeficitMulti.QuestionKeyProperty, value); }
        }
        public static DependencyProperty IncludeOtherProperty =
          DependencyProperty.Register("IncludeOther", typeof(bool), typeof(Virtuoso.Core.Controls.FunctionalDeficitMulti), null);
        public bool IncludeOther
        {
            get { return ((bool)(base.GetValue(FunctionalDeficitMulti.IncludeOtherProperty))); }
            set { base.SetValue(FunctionalDeficitMulti.IncludeOtherProperty, value); }
        }
        public static DependencyProperty SingleSelectProperty =
          DependencyProperty.Register("SingleSelect", typeof(bool), typeof(Virtuoso.Core.Controls.FunctionalDeficitMulti), null);
        public bool SingleSelect
        {
            get { return ((bool)(base.GetValue(FunctionalDeficitMulti.SingleSelectProperty))); }
            set { base.SetValue(FunctionalDeficitMulti.SingleSelectProperty, value); }
        }

        public static DependencyProperty SkipNullItemProperty =
          DependencyProperty.Register("SkipNullItem", typeof(bool), typeof(Virtuoso.Core.Controls.FunctionalDeficitMulti), null);
        public bool SkipNullItem
        {
            get { return ((bool)(base.GetValue(FunctionalDeficitMulti.SkipNullItemProperty))); }
            set { base.SetValue(FunctionalDeficitMulti.SkipNullItemProperty, value); }
        }

        public static DependencyProperty CodeDelimiterProperty =
          DependencyProperty.Register("CodeDelimiter", typeof(string), typeof(Virtuoso.Core.Controls.FunctionalDeficitMulti), null);
        public string CodeDelimiter
        {
            get { return ((string)(base.GetValue(FunctionalDeficitMulti.CodeDelimiterProperty)) == null) ? "|" : (string)(base.GetValue(FunctionalDeficitMulti.CodeDelimiterProperty)); }
            set { base.SetValue(Virtuoso.Core.Controls.FunctionalDeficitMulti.CodeDelimiterProperty, value); }
        }

        public static DependencyProperty TextDelimiterProperty =
          DependencyProperty.Register("TextDelimiter", typeof(string), typeof(Virtuoso.Core.Controls.FunctionalDeficitMulti), null);
        public string TextDelimiter
        {
            get { return ((string)(base.GetValue(FunctionalDeficitMulti.TextDelimiterProperty)) == null) ? " - " : (string)(base.GetValue(FunctionalDeficitMulti.TextDelimiterProperty)); }
            set { base.SetValue(Virtuoso.Core.Controls.FunctionalDeficitMulti.TextDelimiterProperty, value); }
        }

        public static DependencyProperty SelectedValuesProperty =
         DependencyProperty.Register("SelectedValues", typeof(object), typeof(Virtuoso.Core.Controls.FunctionalDeficitMulti),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.FunctionalDeficitMulti)o).SetupSelectedValues();
          }));

        public object SelectedValues
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.FunctionalDeficitMulti.SelectedValuesProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.FunctionalDeficitMulti.SelectedValuesProperty, value);
            }
        }
        private void SetupSelectedValues()
        {
            if (controlTextBlock == null) ApplyTemplate();

            string s = SelectedValues as string;
            if (controlTextBlock != null) controlTextBlock.Text = (s == null) ? "" : s.ToString();
        }
        private void NoneCheck(SelectionChangedEventArgs e)
        {
            if ((e == null) || (controlListBox == null)) return;
            if (e.AddedItems.Count == 0) return;
            if (e.AddedItems.Contains(_itemNone))
            {
                // Remove all but none from selected items
                if (controlListBox.SelectionMode == SelectionMode.Multiple) controlListBox.SelectedItems.Clear();
                controlListBox.SelectedItems.Add(_itemNone);
            }
            else
            {
                // Remove None and Unknown from selected items - if it is in there
                try { controlListBox.SelectedItems.Remove(_itemNone); }
                catch { }
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
                if (e != null) NoneCheck(e);
                SetSelectedValuesFromListBox();
                if (controlOtherTextBox != null)
                {
                    foreach (string s in e.AddedItems)
                    {
                        if (s.ToUpper().Equals(OTHER.ToUpper()))
                        {
                            controlOtherTextBox.Visibility = System.Windows.Visibility.Visible;
                            controlOtherTextBox.UpdateLayout();
                            controlOtherTextBox.Focus();
                            if (scrollViewer != null) scrollViewer.ScrollToBottom();
                            isOther = true;
                        }
                    }
                }
                if (SelectedTextChanged != null)
                    SelectedTextChanged(this, new EventArgs());
            }
            catch { }
            inSelectionChanged = false;
            if (controlListBox == null) return;
            if ((controlListBox.SelectionMode == SelectionMode.Single) && (isOther == false))
            {
                isDoubleClickClose = false;
                this.IsDropDownOpen = false;
            }
        }
        private bool IsValueInSelectedItems(string value)
        {
            if (controlListBox == null) return false;
            if (controlListBox.SelectedItems == null) return false;
            foreach (string s in controlListBox.SelectedItems) if (s.ToUpper().Equals(value.ToUpper())) return true;
            return false;
        }
        private void SetSelectedValuesFromListBox()
        {
            if (controlListBox == null) return;
            //string code = null;
            string rDesc = null;
            bool isOther = false;
            // Iterate over items source rather than SelectedItems to get ordering
            foreach (string d in controlListBox.ItemsSource)
            {
                if (IsValueInSelectedItems(d))
                {
                    if (string.IsNullOrEmpty(d.Trim()))
                    {
                        SelectedValues = "";
                        if (controlTextBlock != null) controlTextBlock.Text = "";
                        if (controlOtherTextBox != null) controlOtherTextBox.Text = "";
                        if (controlListBox.SelectionMode == SelectionMode.Multiple) controlListBox.SelectedItems.Clear();
                        //isDoubleClickClose = false;   // Close dropdown when user chooses the null item
                        //this.IsDropDownOpen = false;
                        return;
                    }
                    else if ((d.ToUpper().Equals(OTHER.ToUpper())) && (IncludeOther))
                    {
                        if (controlOtherTextBox != null)
                        {
                            isOther = true;
                            if (!string.IsNullOrEmpty(controlOtherTextBox.Text))
                            {
                                rDesc = (rDesc == null) ? rDesc = controlOtherTextBox.Text.Trim() : rDesc = rDesc + TextDelimiter + controlOtherTextBox.Text.Trim();
                            }
                        }
                    }
                    else
                    {
                        rDesc = (rDesc == null) ? d : rDesc + TextDelimiter + d;
                    }
                }
            }
            if (controlTextBlock != null) controlTextBlock.Text = rDesc;
            if (controlOtherTextBox != null) controlOtherStackPanel.Visibility = (isOther) ? Visibility.Visible : Visibility.Collapsed;
            if (controlOtherStackPanel != null) controlOtherStackPanel.Visibility = (isOther) ? Visibility.Visible : Visibility.Collapsed;
            if (controlOtherStackPanel2 != null) controlOtherStackPanel2.Visibility = (isOther) ? Visibility.Collapsed : Visibility.Visible;
            SelectedValues = (rDesc == null) ? "" : rDesc;
        }
        private void controlCloseButton_Click(object sender, RoutedEventArgs e)
        {
            isDoubleClickClose = false;
            this.IsDropDownOpen = false;
        }
        private void FunctionalDeficitMulti_DropDownClosed(object sender, EventArgs e)
        {
            if (!isDoubleClickClose) SetSelectedValuesFromListBox();
            isDoubleClickClose = false;
            //this.Focus();
        }
        private void FunctionalDeficitMulti_DropDownOpened(object sender, EventArgs e)
        {
            // Set ListBox selected values
            if (controlListBox != null)
            {
                // Remove the event handler to prevent recursion.
                try { controlListBox.SelectionChanged -= controlListBox_SelectionChanged; }
                catch { }
            }
            isDoubleClickClose = false;
            string _SelectedValues = SelectedValues as String;
            string other = "";
            if (controlTextBlock == null) ApplyTemplate();
            // Setup ListBox values as:
            // - the null value, 
            // - followed by the FunctionalDeficit values (if any)
            // - folloowed by 'other' if we are to include it
            _itemsSource = new List<String>();

            if (!SkipNullItem)
                _itemsSource.Add(" ");

            _itemNone = null;
            foreach (string s in _codeDescriptions)
            {
                _itemsSource.Add(s);
                if (s.ToLower().Trim().StartsWith("none")) _itemNone = s;
            }
            if (IncludeOther) _itemsSource.Add(OTHER);

            // Set ListBox selected values
            if (controlListBox != null)
            {
                controlListBox.ItemsSource = _itemsSource;
                // Populate the listBox selections from the SelectedValues
                // Use Try/Catch to further avoid SelectionChanged recursion
                try
                {
                    _SelectedValues = (_SelectedValues == null) ? "" : _SelectedValues.ToString().Trim();
                    if (controlListBox.SelectionMode == SelectionMode.Multiple) controlListBox.SelectedItems.Clear();
                    if (!string.IsNullOrEmpty(_SelectedValues))
                    {
                        string[] delimiters = { TextDelimiter };
                        string[] valuesSplit = _SelectedValues.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                        Array.Sort(valuesSplit);
                        if (valuesSplit.Length != 0)
                        {
                            foreach (string c in valuesSplit)
                            {
                                string cTrim = c.Trim();
                                if (!string.IsNullOrEmpty(cTrim))
                                {
                                    if (IsValueInList(cTrim))
                                    {
                                        if (SingleSelect)
                                            controlListBox.SelectedItem = cTrim;
                                        else
                                            controlListBox.SelectedItems.Add(cTrim);
                                    }
                                    else
                                    {
                                        other = (other.Length == 0) ? other = cTrim : other = other + TextDelimiter + cTrim;
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
                            if (!IsValueInList(OTHER)) _itemsSource.Add(OTHER);  // Add 'Other' choice if we need to 
                            controlListBox.SelectedItems.Add(OTHER);
                            controlOtherStackPanel.Visibility = Visibility.Visible;
                            controlOtherStackPanel2.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                catch { }
            }
            //SelectedValues = _SelectedValues;
            if (controlTextBlock != null) controlTextBlock.Text = _SelectedValues;

            //this.Focus();
            //this.UpdateLayout();

            // Reestablish the event handler.
            if (controlListBox != null) controlListBox.SelectionChanged += new SelectionChangedEventHandler(controlListBox_SelectionChanged);
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
        }

        private bool IsValueInList(string value, bool includingOther = false)
        {
            if (_itemsSource == null) return false;
            if ((value.ToUpper().Equals(OTHER.ToUpper())) && (includingOther)) return false;
            foreach (string s in _itemsSource) if (s.ToUpper().Equals(value.ToUpper())) return true;
            return false;
        }
    }
}

