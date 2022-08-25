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
using Virtuoso.Core.Utility;
using System.Linq;
using GalaSoft.MvvmLight;


namespace Virtuoso.Core.Controls
{
    public class DisciplineMultiSelectCombo : System.Windows.Controls.ComboBox, ICleanup
    {
        public event EventHandler SelectedTextChanged;
        public event EventHandler MyDropDownClosed;
        private TextBlock controlTextBlock = null;
        private ScrollViewer scrollViewer = null;
        private ListBox controlListBox = null;
        private Popup controlPopup = null;
        private Button controlCloseButton = null;
        private DispatcherTimer _doubleClickTimer;
        private bool isDoubleClickClose = false;

        public DisciplineMultiSelectCombo()
        {
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreMultiSelectComboStyle"]; }
            catch { }

            _doubleClickTimer = new DispatcherTimer();
#if !OPENSILVER
            _doubleClickTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
#else
            _doubleClickTimer.Interval = new TimeSpan(0, 0, 0, 0, 300);
#endif
            _doubleClickTimer.Tick += new EventHandler(DoubleClick_Timer);
            this.DropDownClosed += new EventHandler(DisciplineMultiSelectCombo_DropDownClosed);
            this.KeyDown += new KeyEventHandler(DisciplineMultiSelectCombo_KeyDown);
        }
            public void Cleanup()
        {
            _doubleClickTimer.Tick -= DoubleClick_Timer;
            this.DropDownClosed -= DisciplineMultiSelectCombo_DropDownClosed;
            this.KeyDown -= DisciplineMultiSelectCombo_KeyDown;
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
                ClearSelectedItemsIfNullItemSelected();
                SetSelectedValuesFromListBox();
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
#if OPENSILVER
            controlPopup.CustomLayout=true;
            controlListBox.Style = null;
#endif
            scrollViewer = (ScrollViewer)GetTemplateChild("ScrollViewer");
        }

        void DisciplineMultiSelectCombo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab && e.Key != Key.Enter)
                e.Handled = true;
        }

        public static DependencyProperty CodeDelimiterProperty =
          DependencyProperty.Register("CodeDelimiter", typeof(string), typeof(Virtuoso.Core.Controls.DisciplineMultiSelectCombo), null);
        public string CodeDelimiter
        {
            get { return ((string)(base.GetValue(DisciplineMultiSelectCombo.CodeDelimiterProperty)) == null) ? "|" : (string)(base.GetValue(DisciplineMultiSelectCombo.CodeDelimiterProperty)); }
            set { base.SetValue(Virtuoso.Core.Controls.DisciplineMultiSelectCombo.CodeDelimiterProperty, value); }
        }

        public static DependencyProperty TextDelimiterProperty =
          DependencyProperty.Register("TextDelimiter", typeof(string), typeof(Virtuoso.Core.Controls.DisciplineMultiSelectCombo), null);
        public string TextDelimiter
        {
            get { return ((string)(base.GetValue(DisciplineMultiSelectCombo.TextDelimiterProperty)) == null) ? "; " : (string)(base.GetValue(DisciplineMultiSelectCombo.TextDelimiterProperty)); }
            set { base.SetValue(Virtuoso.Core.Controls.DisciplineMultiSelectCombo.TextDelimiterProperty, value); }
        }

        public static DependencyProperty SelectedKeysProperty = 
            DependencyProperty.Register("SelectedKeys", typeof(object), typeof(Virtuoso.Core.Controls.DisciplineMultiSelectCombo),
            new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.DisciplineMultiSelectCombo)o).SetSelectedItemsFromKeys();
            }));
        public object SelectedKeys
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.DisciplineMultiSelectCombo.SelectedKeysProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.DisciplineMultiSelectCombo.SelectedKeysProperty, value);
            }
        }
        public static DependencyProperty IncludeAllProperty =
         DependencyProperty.Register("IncludeAll", typeof(bool), typeof(Virtuoso.Core.Controls.DisciplineMultiSelectCombo),
          new PropertyMetadata((o, e) =>
          {
          }));
       // private bool _IncludeAll = false;
        public bool IncludeAll
        {
            get {
                bool s = ((bool)(base.GetValue(Virtuoso.Core.Controls.DisciplineMultiSelectCombo.IncludeAllProperty)));
                return s;
            }
            set {
                base.SetValue(Virtuoso.Core.Controls.DisciplineMultiSelectCombo.IncludeAllProperty, value);
            }
        }

        public static DependencyProperty SelectedValuesProperty =
         DependencyProperty.Register("SelectedValues", typeof(object), typeof(Virtuoso.Core.Controls.DisciplineMultiSelectCombo),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.DisciplineMultiSelectCombo)o).SetupSelectedValues();
          }));
        public object SelectedValues
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.DisciplineMultiSelectCombo.SelectedValuesProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {               

                base.SetValue(Virtuoso.Core.Controls.DisciplineMultiSelectCombo.SelectedValuesProperty, value);
            }
        }
 
        private void SetupSelectedValues()
        {
            if (controlTextBlock == null) ApplyTemplate();

            string s = SelectedValues as string;
            if (controlTextBlock != null) controlTextBlock.Text = (s == null) ? "" : s.ToString();

            SetSelectedItemsFromCodes();
        }
        private void ClearSelectedItemsIfNullItemSelected() 
        {
            if ((controlListBox == null) || (controlListBox.SelectedItems == null)) return;
            if (isNullItemSelected == false) return;
            controlListBox.SelectedItems.Clear();
        }
        private bool isNullItemSelected
        {
            get
            {
                if ((controlListBox == null) || (controlListBox.SelectedItems == null)) return false;
                foreach (var item in controlListBox.SelectedItems)
                {
                    string description = item.GetType().GetProperty(controlListBox.DisplayMemberPath).GetValue(item, null).ToString().Trim();
                    if (string.IsNullOrWhiteSpace(description) == true) return true;
                }
                return false;
            }
        }
        private void SetSelectedValuesFromListBox(bool ValuesOnly = false)
        {
            string description = null;
            string key = null;
            string rKeys = null;
            string rDesc = null;
            // Iterate over items source rather than SelectedItems to get ordering
            if (IncludeAll)
            {
                    var list = controlListBox.SelectedItems.Cast<Discipline>().ToList();
                    foreach (var item in list.OrderBy(d => d.DisciplineKey))
                    {
                        description = item.GetType().GetProperty(controlListBox.DisplayMemberPath).GetValue(item, null).ToString().Trim();
                        key = item.GetType().GetProperty(controlListBox.SelectedValuePath).GetValue(item, null).ToString();

                        if (controlListBox.SelectedItems.Count > 1 && description == "All" && list.Last().Description != "All")
                        {
                            continue;
                        }
                        else if (controlListBox.SelectedItems.Count > 1 && description == "All" && list.Last().Description == "All")
                        {
                            rKeys = key + CodeDelimiter;
                            rDesc = (rDesc == null) ? description : rDesc + TextDelimiter + description;
                            break;
                        }

                        rDesc = (rDesc == null) ? description : rDesc + TextDelimiter + description;
                        rKeys += key + CodeDelimiter;
                    }
            }
            else
            {
                foreach (var item in controlListBox.SelectedItems)
                {
                    description = item.GetType().GetProperty(controlListBox.DisplayMemberPath).GetValue(item, null).ToString().Trim();
                    key = item.GetType().GetProperty(controlListBox.SelectedValuePath).GetValue(item, null).ToString();

                    rDesc = (rDesc == null) ? description : rDesc + TextDelimiter + description;
                    rKeys += key + CodeDelimiter;
                }
            }
            if (controlTextBlock != null) controlTextBlock.Text = rDesc;
            SelectedValues = (rDesc == null) ? "" : rDesc;
            // needed to call this method from SetSelectItemsFromKeys to populate the description on load
            // Setting the Keys in that context caused a recursive loop that ends with a stack overflow
            if( !ValuesOnly ) 
                SelectedKeys = (rKeys == null) ? "" : rKeys;

            if (SelectedTextChanged != null)
                SelectedTextChanged(this, new EventArgs());
        }

        private void SetSelectedItemsFromKeys()
        {
            if (controlListBox == null) ApplyTemplate();

            if (SelectedKeys == null || string.IsNullOrEmpty(SelectedKeys.ToString()))
                controlListBox.SelectedItems.Clear();
            else if (controlListBox.ItemsSource != null)
            {
                var keys = SelectedKeys.ToString().Split('|');
                foreach (var item in controlListBox.ItemsSource)
                {
                    string key = item.GetType().GetProperty(controlListBox.SelectedValuePath).GetValue(item, null).ToString();
                    if (keys.Contains(key))
                        controlListBox.SelectedItems.Add(item);
                    else if (controlListBox.SelectedItems.Contains(item))
                        controlListBox.SelectedItems.Remove(item);
                        
                }
            }
            SetSelectedValuesFromListBox(true);
        }

        private void SetSelectedItemsFromCodes()
        {
            if (controlListBox == null) ApplyTemplate();

            if (SelectedValues == null || string.IsNullOrEmpty(SelectedValues.ToString()))
                controlListBox.SelectedItems.Clear();
            else if (controlListBox.ItemsSource != null)
            {
                char split = Convert.ToChar(TextDelimiter.Trim());
                var codes = SelectedValues.ToString().Replace(" ", "").Split(split);
                foreach (var item in controlListBox.ItemsSource)
                {
                    string code = item.GetType().GetProperty(controlListBox.DisplayMemberPath).GetValue(item, null).ToString();
                    if (codes.Contains(code))
                        controlListBox.SelectedItems.Add(item);
                }
            }
        }

        private void controlCloseButton_Click(object sender, RoutedEventArgs e)
        {
            isDoubleClickClose = false;
            this.IsDropDownOpen = false;
        }
        
        private void DisciplineMultiSelectCombo_DropDownClosed(object sender, EventArgs e)
        {
            if (isDoubleClickClose) SetSelectedItemsFromKeys();
            isDoubleClickClose = false;
            if (MyDropDownClosed != null) MyDropDownClosed(sender, e);
        }
    }
}

