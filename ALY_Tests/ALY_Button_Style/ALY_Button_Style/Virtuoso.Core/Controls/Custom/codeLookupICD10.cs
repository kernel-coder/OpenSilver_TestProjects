using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Virtuoso.Client.Utils;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.ICD.Extensions;
using Virtuoso.Portable.Model;

namespace Virtuoso.Core.Controls
{
    public partial class codeLookupICD10 : System.Windows.Controls.ComboBox
    {
        private TextBlock controlTextBlock = null;
        private TextBox controlSearchTextBox = null;
        private TextBlock controlWatermarkTextBlock = null;
        private TextBlock controlVersionTextBlock = null;
        public ListBox controlListBox = null;
        private Button controlCloseButton = null;
        private Popup popup = null;

        public codeLookupICD10()
        {
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CorecodeLookupICDStyle"]; }
            catch { }
            this.DropDownOpened += new EventHandler(codeLookupICD10_DropDownOpened);
            this.KeyUp += new KeyEventHandler(this_KeyUp);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            popup = (Popup)GetTemplateChild("Popup");
            if (popup != null)
            {
                // Set/reset Opened event
                try { popup.Opened -= popup_Opened; }
                catch { }
                popup.Opened += new EventHandler(popup_Opened);
            }

            controlTextBlock = (TextBlock)GetTemplateChild("ControlTextBlock");
            string s = ICDCode as string;
            if (controlTextBlock != null)
            {
                controlTextBlock.Text = (s == null) ? "" : s.ToString();
            }

            controlListBox = (ListBox)GetTemplateChild("ControlListBox");
            if (controlListBox != null)
            {
                try { controlListBox.SelectionChanged -= controlListBox_SelectionChanged; }
                catch { }
                controlListBox.SelectionChanged += new SelectionChangedEventHandler(controlListBox_SelectionChanged);
            }

            controlCloseButton = (Button)GetTemplateChild("ControlCloseButton");
            if (controlCloseButton != null)
            {
                try { controlCloseButton.Click -= controlCloseButton_Click; }
                catch { }
                controlCloseButton.Click += new RoutedEventHandler(controlCloseButton_Click);
            }

            controlSearchTextBox = (TextBox)GetTemplateChild("ControlSearchTextBox");
            if (controlSearchTextBox != null)
            {
                try { controlSearchTextBox.KeyUp -= controlSearchTextBox_KeyUp; }
                catch { }
                controlSearchTextBox.KeyUp += new KeyEventHandler(controlSearchTextBox_KeyUp);
                try { controlSearchTextBox.GotFocus -= controlSearchTextBox_GotFocus; }
                catch { }
                controlSearchTextBox.GotFocus += new RoutedEventHandler(controlSearchTextBox_GotFocus);
            }
            controlWatermarkTextBlock = (TextBlock)GetTemplateChild("ControlWatermarkTextBlock");
            controlVersionTextBlock = (TextBlock)GetTemplateChild("ControlVersionTextBlock");
            if (controlVersionTextBlock != null) controlVersionTextBlock.Text = "10";
        }

        public static DependencyProperty ICDCodeProperty =
         DependencyProperty.Register("ICDCode", typeof(object), typeof(codeLookupICD10),
          new PropertyMetadata((o, e) =>
          {
              AsyncUtility.Run(() => ((codeLookupICD10)o).SetupICDCode());
          }));

        public object ICDCode
        {
            get { string s = ((string)(base.GetValue(codeLookupICD10.ICDCodeProperty))); return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }
            set { base.SetValue(codeLookupICD10.ICDCodeProperty, value); }
        }

        string _MostRecentICDCode = null;
        private async Task SetupICDCode()
        {
            try
            {
                if (controlTextBlock == null) ApplyTemplate();
                string ic = ICDCode as string;
                if (string.IsNullOrWhiteSpace(ic))
                {
                    _MostRecentICDCode = "";
                    controlTextBlock.Text = "";
                    ICDShort = "";
                    return;
                }
                if (controlTextBlock != null) controlTextBlock.Text = ic.Trim();
                if (ic != _MostRecentICDCode)
                {
                    _MostRecentICDCode = ic;
                    //ThreadPool.QueueUserWorkItem(_ => SF Tickets WHHH15090 WILL15107 MAST15110
                    //{

                        CachedICDCode cic = (_IncludeSurgicalCodes == true) ? (await ICDPCS10Cache.Current.GetICDCodeByCode(ic)) : (await ICDCM10Cache.Current.GetICDCodeByCode(ic));

                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            ICDShort = (cic == null) ? "" : ((cic.Short == null) ? "" : cic.Short.Trim());
                        });
                    //});
                }
            }
            catch
            {
            }
        }

        public static DependencyProperty ICDShortProperty =
          DependencyProperty.Register("ICDShort", typeof(string), typeof(codeLookupICD10), null);
        public string ICDShort
        {
            get { string s = ((string)(base.GetValue(codeLookupICD10.ICDShortProperty))); return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }
            set { base.SetValue(codeLookupICD10.ICDShortProperty, value); }
        }

        public static DependencyProperty ICDCodeKeyProperty =
          DependencyProperty.Register("ICDCodeKey", typeof(int), typeof(codeLookupICD10), null);
        public int ICDCodeKey
        {
            get { int i = (int)base.GetValue(codeLookupICD10.ICDCodeKeyProperty); return i; }
            set { base.SetValue(codeLookupICD10.ICDCodeKeyProperty, value); }
        }

        private bool _IncludeDummyCodes = true;
        public static DependencyProperty IncludeDummyCodesProperty =
         DependencyProperty.Register("IncludeDummyCodes", typeof(CodeLookupTF), typeof(codeLookupICD10), null);
        public CodeLookupTF IncludeDummyCodes
        {
            get { return (_IncludeDummyCodes) ? CodeLookupTF.True : CodeLookupTF.False; }
            set { _IncludeDummyCodes = (value == CodeLookupTF.True) ? true : false; }
        }

        private bool _IncludeVWXYcodes = true;
        public static DependencyProperty IncludeVWXYcodesProperty =
         DependencyProperty.Register("IncludeVWXYcodes", typeof(CodeLookupTF), typeof(codeLookupICD10), null);
        public CodeLookupTF IncludeVWXYcodes
        {
            get { return (_IncludeVWXYcodes) ? CodeLookupTF.True : CodeLookupTF.False; }
            set { _IncludeVWXYcodes = (value == CodeLookupTF.True) ? true : false; }
        }

        private bool _IncludeZcodes = true;
        public static DependencyProperty IncludeZcodesProperty =
         DependencyProperty.Register("IncludeZcodes", typeof(CodeLookupTF), typeof(codeLookupICD10), null);
        public CodeLookupTF IncludeZcodes
        {
            get { return (_IncludeZcodes) ? CodeLookupTF.True : CodeLookupTF.False; }
            set { _IncludeZcodes = (value == CodeLookupTF.True) ? true : false; }
        }

        private bool _IncludeSurgicalCodes = true;
        public static DependencyProperty IncludeSurgicalCodesProperty =
         DependencyProperty.Register("IncludeSurgicalCodes", typeof(CodeLookupTF), typeof(codeLookupICD10), null);
        public CodeLookupTF IncludeSurgicalCodes
        {
            get { return (_IncludeSurgicalCodes) ? CodeLookupTF.True : CodeLookupTF.False; }
            set { _IncludeSurgicalCodes = (value == CodeLookupTF.True) ? true : false; }
        }

        private bool _IncludeProcedureCodes = true;
        public static DependencyProperty IncludeProcedureCodesProperty =
         DependencyProperty.Register("IncludeProcedureCodes", typeof(CodeLookupTF), typeof(codeLookupICD10), null);
        public CodeLookupTF IncludeProcedureCodes
        {
            get { return (_IncludeProcedureCodes) ? CodeLookupTF.True : CodeLookupTF.False; }
            set { _IncludeProcedureCodes = (value == CodeLookupTF.True) ? true : false; }
        }

        private void popup_Opened(object sender, EventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (controlSearchTextBox != null) controlSearchTextBox.Focus();
            });
        }
        private void controlCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.ItemsSource = null;
            this.IsDropDownOpen = false;
        }
        private void controlListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (controlListBox.SelectedItem != null)
                {
                    CachedICDCode ic = controlListBox.SelectedItem as CachedICDCode;
                    if (ic.ICDCodeKey <= 0) return;
                    if (controlTextBlock != null) { controlTextBlock.Text = ic.Code; }
                    _MostRecentICDCode = ic.Code;
                    ICDShort = ic.Short;
                    ICDCode = ic.Code;
                    ICDCodeKey = ic.ICDCodeKey;
                    this.IsDropDownOpen = false;
                    this.ItemsSource = null;
                }
            }
            catch { }
        }
        private void codeLookupICD10_DropDownOpened(object sender, EventArgs e)
        {
            //try { controlListBox.SelectionChanged -= controlListBox_SelectionChanged; } catch { }
            if (controlTextBlock == null) ApplyTemplate();
            if (controlListBox != null) controlListBox.ItemsSource = null;
            if (controlSearchTextBox != null)
            {
                controlSearchTextBox.IsTabStop = true;
                controlSearchTextBox.Text = "";
                if (controlWatermarkTextBlock != null) controlWatermarkTextBlock.Text = "Type ICD search criteria followed by <enter>";
                controlSearchTextBox.Focus();
            }
            //if (controlListBox != null)  controlListBox.SelectionChanged += new SelectionChangedEventHandler(controlListBox_SelectionChanged);
        }
        void this_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Delete) || (e.Key == Key.Back))
            {
                _MostRecentICDCode = null;
                ICDShort = null;
                ICDCode = null;
            }
        }

        void controlSearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (controlSearchTextBox == null) return;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                controlSearchTextBox.CaretBrush = new SolidColorBrush(Colors.Black);
            });
        }

        void controlSearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (controlListBox == null) return;
            if (e.Key != Key.Enter && e.Key != Key.Tab ) return;
            string _searchText = controlSearchTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                controlListBox.ItemsSource = null;
                controlSearchTextBox.Text = "";
                if (controlWatermarkTextBlock != null) controlWatermarkTextBlock.Text = "Type ICD search criteria followed by <enter>";
            }
            else
            {
                controlSearchTextBox.Text = "";
                if (controlWatermarkTextBlock != null) controlWatermarkTextBlock.Text = _searchText + " loading...";
                controlSearchTextBox.IsReadOnly = true;
                controlListBox.ItemsSource = null;

                ThreadPool.QueueUserWorkItem(async _ =>
                {
                    IEnumerable itemsSource = null;
                    try
                    { 
                        itemsSource = (_IncludeSurgicalCodes == true) ?
                            await ICDPCS10Cache.Current.SearchTake(_searchText, 300, 10, true, true, _IncludeDummyCodes) :
                        await ICDCM10Cache.Current.SearchTake(_searchText, 300, 10, _IncludeVWXYcodes, _IncludeZcodes, _IncludeDummyCodes); 
                    }
                    catch { }
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        controlListBox.ItemsSource = itemsSource;
                        controlSearchTextBox.IsReadOnly = false;
                        controlSearchTextBox.Text = _searchText;
                        if (controlWatermarkTextBlock != null) controlWatermarkTextBlock.Text = "";
                        if (controlListBox.ItemsSource == null)
                        {
                            controlSearchTextBox.Text = "";
                            if (controlWatermarkTextBlock != null) controlWatermarkTextBlock.Text = "No ICDs match the search criteria";
                        }
                        else
                        {
                            foreach (object o in controlListBox.ItemsSource)
                            {
                                controlListBox.ScrollIntoView(o);
                                return;
                            }
                            controlSearchTextBox.Text = "";
                            if (controlWatermarkTextBlock != null) controlWatermarkTextBlock.Text = "No ICDs match the search criteria";
                        }
                    });
                });
            }
        }

    }
}

