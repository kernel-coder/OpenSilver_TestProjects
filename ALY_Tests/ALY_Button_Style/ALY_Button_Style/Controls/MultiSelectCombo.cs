using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;
using GalaSoft.MvvmLight;


namespace Virtuoso.Core.Controls
{
    public class MultiSelectCombo : System.Windows.Controls.ComboBox, ICleanup
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

        public MultiSelectCombo()
        {
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreMultiSelectComboStyle"]; }
            catch { }
            _doubleClickTimer = new DispatcherTimer();
            _doubleClickTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _doubleClickTimer.Tick += new EventHandler(DoubleClick_Timer);
            this.DropDownClosed += new EventHandler(MultiSelectCombo_DropDownClosed);
            this.KeyDown += new KeyEventHandler(MultiSelectCombo_KeyDown);
        }
        public void Cleanup()
        {
            _doubleClickTimer.Tick -= DoubleClick_Timer;
            this.DropDownClosed -= MultiSelectCombo_DropDownClosed;
            this.KeyDown -= MultiSelectCombo_KeyDown;
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
            scrollViewer = (ScrollViewer)GetTemplateChild("ScrollViewer");
        }

        void MultiSelectCombo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab && e.Key != Key.Enter)
                e.Handled = true;
        }

        public static DependencyProperty CodeDelimiterProperty =
          DependencyProperty.Register("CodeDelimiter", typeof(string), typeof(Virtuoso.Core.Controls.MultiSelectCombo), null);
        public string CodeDelimiter
        {
            get { return ((string)(base.GetValue(MultiSelectCombo.CodeDelimiterProperty)) == null) ? "|" : (string)(base.GetValue(MultiSelectCombo.CodeDelimiterProperty)); }
            set { base.SetValue(Virtuoso.Core.Controls.MultiSelectCombo.CodeDelimiterProperty, value); }
        }

        public static DependencyProperty TextDelimiterProperty =
          DependencyProperty.Register("TextDelimiter", typeof(string), typeof(Virtuoso.Core.Controls.MultiSelectCombo), null);
        public string TextDelimiter
        {
            get { return ((string)(base.GetValue(MultiSelectCombo.TextDelimiterProperty)) == null) ? "; " : (string)(base.GetValue(MultiSelectCombo.TextDelimiterProperty)); }
            set { base.SetValue(Virtuoso.Core.Controls.MultiSelectCombo.TextDelimiterProperty, value); }
        }

        public static DependencyProperty SelectedKeysProperty = 
            DependencyProperty.Register("SelectedKeys", typeof(object), typeof(Virtuoso.Core.Controls.MultiSelectCombo),
            new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.MultiSelectCombo)o).SetSelectedItemsFromKeys();
            }));
        public object SelectedKeys
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.MultiSelectCombo.SelectedKeysProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.MultiSelectCombo.SelectedKeysProperty, value);
            }
        }

        public static DependencyProperty SelectedValuesProperty =
         DependencyProperty.Register("SelectedValues", typeof(object), typeof(Virtuoso.Core.Controls.MultiSelectCombo),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.MultiSelectCombo)o).SetupSelectedValues();
          }));
        public object SelectedValues
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.MultiSelectCombo.SelectedValuesProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.MultiSelectCombo.SelectedValuesProperty, value);
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
                if ((controlListBox == null) || (controlListBox.SelectedControlListBox == null)) return false;
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
            foreach (var item in controlListBox.SelectedItems)
            {
                description = item.GetType().GetProperty(controlListBox.DisplayMemberPath).GetValue(item, null).ToString().Trim();
                key = item.GetType().GetProperty(controlListBox.SelectedValuePath).GetValue(item, null).ToString();

                rDesc = (rDesc == null) ? description : rDesc + TextDelimiter + description;
                rKeys += key + CodeDelimiter;
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
            else
            {
                var keys = SelectedKeys.ToString().Split('|');
                foreach (var item in controlListBox.ItemsSource)
                {
                    string key = item.GetType().GetProperty(controlListBox.SelectedValuePath).GetValue(item, null).ToString();
                    if (keys.Contains(key))
                        controlListBox.SelectedItems.Add(item);
                }
            }
            SetSelectedValuesFromListBox(true);
        }

        private void SetSelectedItemsFromCodes()
        {
            if (controlListBox == null) ApplyTemplate();

            if (SelectedValues == null || string.IsNullOrEmpty(SelectedValues.ToString()))
                controlListBox.SelectedItems.Clear();
            else
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
        
        private void MultiSelectCombo_DropDownClosed(object sender, EventArgs e)
        {
            if (isDoubleClickClose) SetSelectedItemsFromKeys();
            isDoubleClickClose = false;
            if (MyDropDownClosed != null) MyDropDownClosed(sender, e);
        }
    }
}

