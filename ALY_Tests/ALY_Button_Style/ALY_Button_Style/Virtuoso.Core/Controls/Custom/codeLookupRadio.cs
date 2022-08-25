using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Virtuoso.Core.Services;
using Virtuoso.Server.Data;
using Virtuoso.Core.Cache;
using System.Windows.Data;

//   <VirtuosoCoreControls:codeLookupRadio  
//     CodeType="YESNO" 
//     Wrap="True" 
//     SelectedCode="{Binding MyCode, Mode=TwoWay}" />

namespace Virtuoso.Core.Controls
{
    //http://www.lhotka.net/weblog/CommentView,guid,f3353b7c-a1b5-41f2-a9bf-00f0c4e6a999.aspx

    public enum codeLookupRadioWrap { True, False };
    public class codeLookupRadioButton : System.Windows.Controls.RadioButton
    {
        public static DependencyProperty ParentControlProperty =
            DependencyProperty.Register("ParentControl", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupRadioButton), null);
        public bool ParentControl
        {
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.codeLookupRadioButton.ParentControlProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.codeLookupRadioButton.ParentControlProperty, value); }
        }
        public static DependencyProperty SelectedCodeProperty =
            DependencyProperty.Register("SelectedCode", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupRadioButton), null);
        public string SelectedCode
        {
            get { return ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupRadioButton.SelectedCodeProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.codeLookupRadioButton.SelectedCodeProperty, value); }
        }
    }

    public class codeLookupRadio : System.Windows.Controls.StackPanel 
    {
        private bool _loaded = false;
        private bool _wrap = false;
        WrapPanel wp = new WrapPanel();
        StackPanel sp = new StackPanel();
        public StackPanel InternalStackPanel
        {
            get { return sp; }
        }
        public WrapPanel InternalWrapPanel
        {
            get { return wp; }
        }
        public codeLookupRadio()
        {
            this.Loaded += new RoutedEventHandler(codeLookupRadio_Loaded);
        }
        void codeLookupRadio_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_loaded) LoadCodes();
        }
        private void LoadCodes()
        {
            _loaded = true;
            //this.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            //this.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            this.Children.Clear();
            wp.Children.Clear();
            sp.Children.Clear();
            this.Children.Add((_wrap) ? wp as UIElement : sp as UIElement );

            if (string.IsNullOrWhiteSpace(CodeType)) return;
            List<CodeLookup> _items = GetCodeLookupsFromType(CodeType);
            if (_items != null) foreach (CodeLookup cl in _items) AddCodeLookupRadioButton(cl);
            if (ParentControl != null)
            {
                // Don't fail if all that happens is we can't set focus.
                try { Utility.SetFocusHelper.SelectFirstEditableWidget(ParentControl); }
                catch { }
            }

        }
        private List<CodeLookup> GetCodeLookupsFromType(string codeType)
        {
            // codetype can either be:
            //  - an actual code type
            //  - a pipe delimited list of adhoc codes
            if (string.IsNullOrWhiteSpace(codeType)) return null;
            if (codeType.Contains("|") == false) return CodeLookupCache.GetCodeLookupsFromType(codeType, true);
            List<CodeLookup> items = new List<CodeLookup>();
            int i = 1;
            string[] delimiter = { "|" };
            string[] itemArray = codeType.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in itemArray) if (string.IsNullOrEmpty(item) == false) items.Add(new CodeLookup() { Code = item, CodeDescription = item, CodeLookupKey = i++ });
            return items;
        }
        codeLookupRadioButton AddCodeLookupRadioButton(CodeLookup cl)
        {
            codeLookupRadioButton b = new codeLookupRadioButton();
            b.Content = cl.CodeDescription.Trim();
            b.Tag = cl;
            // we created a bogus codeLookupRadioButton.SelectedCodeProperty so we can propagate the NotifyOnValidationError down to the actual radio buttons for UI error state
            BindingExpression be = this.GetBindingExpression(codeLookupRadio.SelectedCodeProperty);
            b.SetBinding(codeLookupRadioButton.SelectedCodeProperty, be.ParentBinding);
            b.Checked += new RoutedEventHandler(codeLookupRadio_Checked);
            if (ChildStyle != null) b.Style = ChildStyle;
            if (IsEnabled != null) b.IsEnabled = (bool)IsEnabled;
            int horizontalSpacing = (HorizontalSpacing == null) ? 20 : (int)HorizontalSpacing;
            if (_wrap) b.Margin = new Thickness(0, 0, horizontalSpacing, 0);
            ((_wrap) ? wp.Children as UIElementCollection : sp.Children as UIElementCollection).Add(b);
            return b;
        }
        void codeLookupRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (inSetSelectionFromCode) return;
            codeLookupRadioButton b = sender as codeLookupRadioButton;
            if (b != null)
            {
                CodeLookup cl = b.Tag as CodeLookup;
                SelectedCode = (cl == null) ? null : cl.Code.ToString().Trim();
                SelectedCodeChanged?.Invoke(this, new EventArgs());
            }
        }

        public static DependencyProperty CodeTypeProperty =
          DependencyProperty.Register("CodeType", typeof(string), typeof(Virtuoso.Core.Controls.codeLookupRadio), null);
        public string CodeType
        {
            get
            {
                return ((string)(base.GetValue(codeLookupRadio.CodeTypeProperty)));
            }
            set
            {
                base.SetValue(codeLookupRadio.CodeTypeProperty, value);
                if (_loaded) { LoadCodes(); }  // If already did the initial load - reload on change of CodeType
            }
        }
        public event EventHandler SelectedCodeChanged;
        public static DependencyProperty SelectedCodeProperty =
          DependencyProperty.Register("SelectedCode", typeof(string), typeof(Virtuoso.Core.Controls.codeLookupRadio),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.codeLookupRadio)o).SetSelectionFromCode();
          }));

        public string SelectedCode
        {
            get { return ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupRadio.SelectedCodeProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.codeLookupRadio.SelectedCodeProperty, value); }
        }

        public static DependencyProperty WrapProperty =
        DependencyProperty.Register("Wrap", typeof(codeLookupRadioWrap), typeof(Virtuoso.Core.Controls.codeLookupRadio), null);
        public codeLookupRadioWrap Wrap
        {
            get
            {
                return (_wrap) ? codeLookupRadioWrap.True : codeLookupRadioWrap.False;
            }
            set
            {
                _wrap = (value == codeLookupRadioWrap.True) ? true : false;
                if (_loaded) { LoadCodes(); }  // If already did the initial load - reload on change of Wrap
            }
        }
        public static DependencyProperty HorizontalSpacingProperty =
            DependencyProperty.Register("HorizontalSpacing", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupRadio), null);
        public int? HorizontalSpacing
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.codeLookupRadio.HorizontalSpacingProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.codeLookupRadio.HorizontalSpacingProperty, value); }
        }
        public static DependencyProperty ChildStyleProperty =
       DependencyProperty.Register("ChildStyle", typeof(Style), typeof(Virtuoso.Core.Controls.codeLookupRadio), null);
        public Style ChildStyle
        {
            get
            {
                return ((Style)(base.GetValue(codeLookupRadio.ChildStyleProperty)));
            }
            set
            {
                base.SetValue(codeLookupRadio.ChildStyleProperty, value);
            }
        }

        public static DependencyProperty IsEnabledProperty =
           DependencyProperty.Register("IsEnabled", typeof(bool?), typeof(Virtuoso.Core.Controls.codeLookupRadio), new PropertyMetadata((o, e) =>
           {
               ((Virtuoso.Core.Controls.codeLookupRadio)o).IsEnabledChanged();
           }));

        public bool? IsEnabled
        {
            get
            {
                return ((bool?)(base.GetValue(codeLookupRadio.IsEnabledProperty)));
            }
            set
            {
                base.SetValue(codeLookupRadio.IsEnabledProperty, value);
            }
        }
        private void IsEnabledChanged()
        {
            if (_wrap)
                foreach (UIElement e in wp.Children)
                {
                    codeLookupRadioButton r = e as codeLookupRadioButton;
                    if (r != null) r.IsEnabled = (bool)IsEnabled;
                }
            else
                foreach (UIElement e in sp.Children)
                {
                    codeLookupRadioButton r = e as codeLookupRadioButton;
                    if (r != null) r.IsEnabled = (bool)IsEnabled;
                }

        }
        public static DependencyProperty ParentControlProperty =
            DependencyProperty.Register("ParentControl", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupRadio), new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.codeLookupRadio)o).SetupParent();
            }));
        public FrameworkElement ParentControl
        {
            get { return ((FrameworkElement)(base.GetValue(Virtuoso.Core.Controls.codeLookupRadio.ParentControlProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.codeLookupRadio.ParentControlProperty, value); }
        }
        private void SetupParent()
        {
            if (ParentControl != null) Utility.SetFocusHelper.SelectFirstEditableWidget(ParentControl);
        }
        bool inSetSelectionFromCode = false;
        bool isChecked = false;
        private void SetSelectionFromCode()
        {
            if (!_loaded) { LoadCodes(); }  // If already did the initial load - reload on change of Wrap
            if (inSetSelectionFromCode) { return; }
            inSetSelectionFromCode = true;
            isChecked = false;
            string selectedCode = (String.IsNullOrWhiteSpace(SelectedCode)) ? "" : SelectedCode.Trim().ToLower();
            if (_wrap)
                foreach (UIElement e in wp.Children)
                {
                    SetRadioButton(e as codeLookupRadioButton, selectedCode);
                }
            else
                foreach (UIElement e in sp.Children)
                {
                    SetRadioButton(e as codeLookupRadioButton, selectedCode);
                }
            // Account for bad (invalid) data
            if (_wrap)
                foreach (UIElement e in wp.Children)
                {
                    if (RemoveInvalidCodeLookupRadioButton(e as codeLookupRadioButton)) break;
                }
            else
                foreach (UIElement e in sp.Children)
                {
                    if (RemoveInvalidCodeLookupRadioButton(e as codeLookupRadioButton)) break;
                }
 
            if ((!String.IsNullOrWhiteSpace(selectedCode)) && (!isChecked))
            {
                CodeLookup cl = new CodeLookup()
                {
                    CodeLookupKey = 0,
                    Code = selectedCode,
                    CodeDescription = selectedCode,
                    Inactive = true
                };
                SetRadioButton(AddCodeLookupRadioButton(cl), selectedCode);
            }
            inSetSelectionFromCode = false;
        }
        private bool RemoveInvalidCodeLookupRadioButton(codeLookupRadioButton b)
        {
            if (b == null) return false;
            CodeLookup cl = b.Tag as CodeLookup;
            if (cl.Inactive)
            {
                ((_wrap) ? wp.Children as UIElementCollection : sp.Children as UIElementCollection).Remove(b);
                return true;
            }
            return false;
        }
        private void SetRadioButton(codeLookupRadioButton b, string selectedCode)
        {
            if (b == null) return;
            CodeLookup cl = b.Tag as CodeLookup;
            b.IsChecked = (selectedCode == "") ? false : (cl == null) ? false : (selectedCode.Equals((String.IsNullOrWhiteSpace(cl.Code)) ? "" : cl.Code.Trim().ToLower()));
            if ((bool)b.IsChecked) isChecked = true;
        }
    }
}
