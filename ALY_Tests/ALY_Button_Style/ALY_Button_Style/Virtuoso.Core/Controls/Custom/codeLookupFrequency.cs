using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Virtuoso.Core.Services;
using Virtuoso.Server.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Virtuoso.Core.Cache;
using System.Threading;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Media;


namespace Virtuoso.Core.Controls
{
    public class ItemFrequency : GalaSoft.MvvmLight.ViewModelBase 
    {
        public string Label1 { get; set; }
        public string Label2 { get; set; }
        public string Label3 { get; set; }
        public bool IsData1 { get; set; }
        public bool IsData2 { get; set; }
        public bool IsOther { get; set; }
        private string _data1 = null;
        public string Data1 { get { return _data1; } set { _data1 = value; RaisePropertyChanged("Data1"); } }
        private string _data2 = null;
        public string Data2 { get { return _data2; } set { _data2 = value; RaisePropertyChanged("Data2"); } }
        public int DataPoints { get; set; }
        public int? Sequence { get; set; }
        public string FormatString { get; set; }
        public int CodeLookupKey { get; set; }
    }
    public class codeLookupFrequency : System.Windows.Controls.ComboBox
    {
        private TextBlock controlTextBlock = null;
        private ListBox controlListBox = null;
        private Button controlCloseButton = null;
        //private TextBox controlTextBoxData1 = null;
        public List<ItemFrequency> CodeLookupFrequencys { get ; set; }
        public codeLookupFrequency()
        {
            LoadItemFrequencys();
            this.Loaded += new RoutedEventHandler(codeLookupFrequency_Loaded);
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreCodeLookupFrequencyStyle"]; }
            catch { }
            this.DropDownOpened += new EventHandler(codeLookupFrequency_DropDownOpened);
            this.DropDownClosed += new EventHandler(codeLookupFrequency_DropDownClosed);

        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            controlTextBlock = (TextBlock)GetTemplateChild("ControlTextBlock");
            string s = FrequencyDescription as string;
            if (controlTextBlock != null) { controlTextBlock.Text = (s == null) ? "" : s.ToString(); }
            controlListBox = (ListBox)GetTemplateChild("ControlListBox");
            if (controlListBox != null)
            {
                // Set/reset events
                try { controlListBox.SelectionChanged -= controlListBox_SelectionChanged; }
                catch { }
                controlListBox.SelectionChanged += new SelectionChangedEventHandler(controlListBox_SelectionChanged);
                try { controlListBox.GotFocus -= controlListBox_GotFocus; }
                catch { }
                controlListBox.GotFocus += new RoutedEventHandler(controlListBox_GotFocus);
                try { controlListBox.KeyUp -= controlListBox_KeyUp; }
                catch { }
                controlListBox.KeyUp += new KeyEventHandler(controlListBox_KeyUp);
            }
            controlCloseButton = (Button)GetTemplateChild("ControlCloseButton");
            if (controlCloseButton != null)
            {
                // Set/reset events
                try { controlCloseButton.Click -= controlCloseButton_Click; }
                catch { }
                controlCloseButton.Click += new RoutedEventHandler(controlCloseButton_Click);
            }
        }
        private void controlListBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e == null) return;
            if (e.Key != Key.Enter) return;
            IsDropDownOpen = false;
        }
        void controlListBox_GotFocus(object sender, RoutedEventArgs e)
        { 
            var item = FindAncester<ListBoxItem>((DependencyObject)e.OriginalSource); 
            if (item != null) item.IsSelected = true;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                TextBox t = e.OriginalSource as TextBox;
                if (t != null) t.Focus();
            });
        }  
        T FindAncester<T>(DependencyObject current) where T : DependencyObject 
        { 
            current = VisualTreeHelper.GetParent(current); 
            while (current != null) 
            { 
                if (current is T) 
                { 
                    return (T)current; 
                } 
                current = VisualTreeHelper.GetParent(current); 
            };     
            return null; 
        }  
        private void codeLookupFrequency_Loaded(object sender, RoutedEventArgs e)
        {
            if (controlTextBlock == null) this.ApplyTemplate();
            if (controlListBox != null) controlListBox.ItemsSource = CodeLookupFrequencys;
        }
        private void LoadItemFrequencys()
        {
            CodeLookupFrequencys = new List<ItemFrequency>();
            List<CodeLookup> clList = CodeLookupCache.GetCodeLookupsFromType("MEDFREQUENCY");
            if (clList == null) return;
            foreach (CodeLookup cl in clList )
            {
                ItemFrequency clf = DeriveItemFrequencyFromCodeLookup(cl);
                if (clf != null) CodeLookupFrequencys.Add(clf);
            }
        }
        private ItemFrequency DeriveItemFrequencyFromCodeLookup(CodeLookup cl)
        {
            if (cl == null) return null;
            string[] delimiters = { "|" };
            string[] pieces = cl.Code.Split(delimiters, StringSplitOptions.None);
            if ((pieces.Length == 0) || (pieces.Length > 5))
            {
                MessageBox.Show(String.Format("Error codeLookupFrequency.LoadItemsSource: Frequency {0} is ill defined.  Contact your system administrator.", cl.Code));
                return null;
            }
            ItemFrequency clf = new ItemFrequency();
            clf.Label1 = pieces[0];
            clf.IsOther = (clf.Label1 == "Other");
            if ((pieces.Length >= 2) && (pieces[1]) != "{0}")
            {
                MessageBox.Show(String.Format("Error codeLookupFrequency.LoadItemsSource: Frequency {0} is ill defined.  Contact your system administrator.", cl.Code));
                return null;
            }
            clf.IsData1 = (pieces.Length >= 2);
            if (pieces.Length >= 3) clf.Label2 = pieces[2];
            if ((pieces.Length >= 4) && (pieces[3]) != "{1}")
            {
                MessageBox.Show(String.Format("Error codeLookupFrequency.LoadItemsSource: Frequency {0} is ill defined.  Contact your system administrator.", cl.Code));
                return null;
            }
            clf.IsData2 = (pieces.Length >= 4);
            if (pieces.Length >= 5) clf.Label3 = pieces[4];
            clf.DataPoints = (clf.IsData1 == false) ? 0 : ((clf.IsData2 == false) ? 1 : 2);
            clf.Sequence = cl.Sequence;
            clf.CodeLookupKey = cl.CodeLookupKey;
            clf.FormatString = cl.Code.Replace("|", " ").Trim();
            return clf;
        }
        public static DependencyProperty FrequencyDescriptionProperty =
         DependencyProperty.Register("FrequencyDescription", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupFrequency), null);

        public object FrequencyDescription
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupFrequency.FrequencyDescriptionProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.codeLookupFrequency.FrequencyDescriptionProperty, value);
            }
        }
        public static DependencyProperty FrequencyDataProperty =
         DependencyProperty.Register("FrequencyData", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupFrequency),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.codeLookupFrequency)o).SetupFrequencyDescriptionFromFrequencyData();
          }));

        public object FrequencyData
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupFrequency.FrequencyDataProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.codeLookupFrequency.FrequencyDataProperty, value);
            }
        }
        private void SetupFrequencyDescriptionFromFrequencyData()
        {
            if (controlTextBlock == null) ApplyTemplate();

            string frequencyData = FrequencyData as string;
            string frequencyDescription = null;
            if (string.IsNullOrWhiteSpace(frequencyData) == false)
            {
                string[] frequencyDataDelimiter = { "|" };
                string[] pieces = frequencyData.Split(frequencyDataDelimiter, StringSplitOptions.None);
                int key = 0;
                try { key = Int32.Parse(pieces[0]); }
                catch { }
                ItemFrequency clf = DeriveItemFrequencyFromCodeLookup(CodeLookupCache.GetCodeLookupFromKey(key));
                if (clf != null)
                {
                    string data1 = (pieces.Length >= 2) ? pieces[1] : "";
                    string data2 = (pieces.Length >= 3) ? pieces[2] : "";
                    if (clf.IsOther)
                    {
                        frequencyDescription = data1;
                    }
                    else
                    {
                        if (clf.DataPoints == 0) frequencyDescription = clf.FormatString;
                        else if (clf.DataPoints == 1) frequencyDescription = string.Format(clf.FormatString, data1);
                        else frequencyDescription = string.Format(clf.FormatString, data1, data2);
                    }
                }
            }
            if (controlTextBlock != null) controlTextBlock.Text = (frequencyDescription == null) ? "" : frequencyDescription;
        }
        private void SetupPopupControlsFromFrequencyData()
        {
            if (controlTextBlock == null) ApplyTemplate();

            string frequencyData = FrequencyData as string;
            if (frequencyData == null) frequencyData = "";
            string[] frequencyDataDelimiter = { "|" };
            string[] pieces = frequencyData.Split(frequencyDataDelimiter, StringSplitOptions.None);
            string codeLookupKey = (pieces.Length >= 1) ? pieces[0] : "";
            string data1 = (pieces.Length >= 2) ? ((pieces[1] == "?") ? "" : pieces[1]) : "";
            string data2 = (pieces.Length >= 3) ? ((pieces[2] == "?") ? "" : pieces[2]) : "";
            foreach (ItemFrequency clf in CodeLookupFrequencys)
            {
                clf.Data1 = (clf.CodeLookupKey.ToString() == codeLookupKey) ? data1 : null;
                clf.Data2 = (clf.CodeLookupKey.ToString() == codeLookupKey) ? data2 : null;
            }
            ItemFrequency i = (string.IsNullOrWhiteSpace(codeLookupKey)) ? null : CodeLookupFrequencys.Where(c => c.CodeLookupKey.ToString() == codeLookupKey).FirstOrDefault();
            if (controlListBox != null) controlListBox.SelectedItem = i;
        }
        private void SetupFrequencyDataFromPopupControls()
        {
            string frequencyData = null;
            string frequencyDescription = null;

            if ((controlListBox != null) && (controlListBox.SelectedItem != null))
            {
                ItemFrequency clf = controlListBox.SelectedItem as ItemFrequency;
                if (clf != null)
                {
                    string data1 = (string.IsNullOrWhiteSpace(clf.Data1)) ? "?" : clf.Data1;
                    string data2 = (string.IsNullOrWhiteSpace(clf.Data2)) ? "?" : clf.Data2;
                    frequencyData = clf.CodeLookupKey.ToString() + "|" + data1 + "|" + data2;
                    if (clf.IsOther)
                    {
                        frequencyDescription = data1;
                    }
                    else
                    {
                        if (clf.DataPoints == 0) frequencyDescription = clf.FormatString;
                        else if (clf.DataPoints == 1) frequencyDescription = string.Format(clf.FormatString, data1);
                        else frequencyDescription = string.Format(clf.FormatString, data1, data2);
                    }
                }
            }

            if (controlTextBlock != null) controlTextBlock.Text = frequencyDescription;
            FrequencyData = (frequencyData == null) ? "" : frequencyData;
            FrequencyDescription = (frequencyDescription == null) ? "" : frequencyDescription;
        }
        private void controlCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.IsDropDownOpen = false;
        }        
        private void codeLookupFrequency_DropDownClosed(object sender, EventArgs e)
        {
            SetupFrequencyDataFromPopupControls();
        }
        private void controlListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (controlListBox == null) return;
            ItemFrequency item = controlListBox.SelectedItem as ItemFrequency;
            if (item == null) return;
            if (item.DataPoints == 0) this.IsDropDownOpen = false;
        }
        private void codeLookupFrequency_DropDownOpened(object sender, EventArgs e)
        {
            SetupPopupControlsFromFrequencyData();
        }
    }
}

