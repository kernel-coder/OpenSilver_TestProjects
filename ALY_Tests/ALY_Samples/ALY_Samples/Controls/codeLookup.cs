using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
//using Virtuoso.Core.Services;
using Virtuoso.Server.Data;
using System.ComponentModel;
using System.Windows.Data;
//using Virtuoso.Core.Cache;
using GalaSoft.MvvmLight;
//using Virtuoso.Core.Utility;

//<VirtuosoCoreControls:codeLookup 
//    x:Name="MyName" Width="60"
//    CodeType="STATE"
//    SelectedKey="{Binding MyKey, Mode=TwoWay}"
//    FormatOfSelection="Code" 
//    FormatOfDropDown="CodeDashDescription" />

namespace Virtuoso.Core.Controls
{
    //http://www.lhotka.net/weblog/CommentView,guid,f3353b7c-a1b5-41f2-a9bf-00f0c4e6a999.aspx

    public enum CodeLookupFormat { Code, CodeDashDescription, Description };
    public enum CodeLookupTF { True, False };
    public class codeLookup : System.Windows.Controls.ComboBox, ICleanup
    {
        private string SelectedDisplayMemberPath = "DisplayMember";
        private string SelectedKeyPath = "NullableCodeLookupKey";
        private ICollectionView _itemSource;
        private List<CodeLookup> _itemSourceOriginal;
        private List<string> _ExcludedCodes = new List<string>();
        private bool _includeNullItem = true;
        private bool _loaded = false;
        private string strSearch = null;

        public codeLookup()
        {
            this.Loaded += new RoutedEventHandler(codeLookup_Loaded);
            this.KeyUp += new KeyEventHandler(codeLookup_KeyUp);
            this.DropDownOpened += new EventHandler(codeLookup_DropDownOpened);
            this.DropDownClosed += new EventHandler(codeLookup_DropDownClosed);
            this.SelectionChanged += new SelectionChangedEventHandler(codeLookup_SelectionChanged);
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreComboBoxStyle"]; }
            catch { }
            this.DisplayMemberPath = "DisplayMember";
            this.SelectedDisplayMemberPath = "DisplayMember";
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ScrollViewer p = (ScrollViewer)GetTemplateChild("ScrollViewer");
            if (p != null)
            {
                // Set/reset SelectionChanged event
                try { p.KeyUp -= codeLookup_KeyUp; }
                catch { }
                p.KeyUp += new KeyEventHandler(codeLookup_KeyUp);
            }
        }
        void codeLookup_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_loaded) LoadCodes();
        }
        void codeLookup_KeyUp(object sender, KeyEventArgs e)
        {
            if (IsDropDownOpen == false)
            {
                strSearch = null;
            }
            if (e.Key == Key.Delete)
            {
                strSearch = null;
            }
            else if
                (
                    (e.Key >= Key.A && e.Key <= Key.Z) ||
                    (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                    (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
                )
            {
                var keyStringVal = e.Key.ToString();
                if (e.Key >= Key.D0 && e.Key <= Key.D9)
                    keyStringVal = ((int)e.Key - 20).ToString();
                if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
                    keyStringVal = ((int)e.Key - 68).ToString();
                strSearch = strSearch + keyStringVal.ToUpper();
                this.IsDropDownOpen = true;
                var sel = (from item in Items
                           where GetDisplayMemberToUpper(item).Trim().StartsWith(strSearch)
                           select item).FirstOrDefault();
                if (sel != null)
                {
                    if (Deployment.Current.CheckAccess())
                    {
                        var dispatcher = Deployment.Current.Dispatcher;
                        if (dispatcher.CheckAccess())
                        {
                            dispatcher.BeginInvoke(() =>
                            {
                                SelectedItem = sel;
                                if (SkipUpdateLayout == false)
                                {
                                    try { this.UpdateLayout(); }
                                    catch { }
                                }
                            });
                        }
                    }
                    else
                    {
                        SelectedItem = sel;
                        if (SkipUpdateLayout == false)
                        {
                            try { this.UpdateLayout(); }
                            catch { }
                        }
                    }
                }
            }
            e.Handled = false;
        }
        void codeLookup_DropDownOpened(object sender, EventArgs e)
        {
            SetDisplayMembers(FormatOfDropDown);
        }
        void codeLookup_DropDownClosed(object sender, EventArgs e)
        {
            strSearch = null;
            SetDisplayMembers(FormatOfSelection);
        }

        void codeLookup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //this.IsDropDownOpen = false;
            SelectedKey = (e.AddedItems.Count > 0) ? GetSelectedKey(e.AddedItems[0]) : (int?)null;
        }

        private void LoadCodes()
        {
            _loaded = true;

        }

        private void SetDisplayMembers(CodeLookupFormat format)
        {
            if (this.Items != null)
            {
                foreach (CodeLookup cl in this.Items)
                {
                    if (format == CodeLookupFormat.CodeDashDescription) { cl.DisplayMember = cl.CodeDashDescription; }
                    else if (format == CodeLookupFormat.Description) { cl.DisplayMember = cl.CodeDescription; }
                    else { cl.DisplayMember = cl.Code; }
                    if (string.IsNullOrEmpty(cl.DisplayMember)) { cl.DisplayMember = " "; }
                    cl.RaisePropertyChangedDisplayMember();
                }
            }
        }

        public static DependencyProperty SkipUpdateLayoutProperty =
          DependencyProperty.Register("SkipUpdateLayout", typeof(bool), typeof(Virtuoso.Core.Controls.codeLookup), null);
        public bool SkipUpdateLayout
        {
            get
            {
                return ((bool)(base.GetValue(codeLookup.SkipUpdateLayoutProperty)));
            }
            set
            {
                base.SetValue(codeLookup.SkipUpdateLayoutProperty, value);
            }
        }

        public static DependencyProperty CodeTypeProperty =
          DependencyProperty.Register("CodeType", typeof(string), typeof(Virtuoso.Core.Controls.codeLookup),
            new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.codeLookup)o).SetCodeType();
            }));

        public string CodeType
        {
            get
            {
                return ((string)(base.GetValue(codeLookup.CodeTypeProperty)));
            }
            set
            {
                base.SetValue(codeLookup.CodeTypeProperty, value);
            }
        }
        private void SetCodeType()
        {
            if (_loaded) { LoadCodes(); }  // If already did the initial load - reload on change of type
        }
        public static DependencyProperty ApplicationDataProperty =
          DependencyProperty.Register("ApplicationData", typeof(string), typeof(Virtuoso.Core.Controls.codeLookup),
            new PropertyMetadata((o, e) =>
            {
                ((Virtuoso.Core.Controls.codeLookup)o).SetApplicationData();
            }));

        public string ApplicationData
        {
            get
            {
                return ((string)(base.GetValue(codeLookup.ApplicationDataProperty)));
            }
            set
            {
                base.SetValue(codeLookup.ApplicationDataProperty, value);
            }
        }
        private void SetApplicationData()
        {
            if (_loaded) { LoadCodes(); }  // If already did the initial load - reload on change of type
        }
        public static DependencyProperty IncludeNullItemProperty =
          DependencyProperty.Register("IncludeNullItem", typeof(CodeLookupTF), typeof(Virtuoso.Core.Controls.codeLookup), 
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.codeLookup)o).SetIncludeNullItem();
          }));
        private void SetIncludeNullItem()
        {
            CodeLookupTF i = (CodeLookupTF)(base.GetValue(codeLookup.IncludeNullItemProperty));
            _includeNullItem = (i == CodeLookupTF.True) ? true : false;
            AddOrRemoveNullItem();
        }

        public bool IncludeNullItem
        {
            get
            {
                // If the property was never set - return true
                return _includeNullItem;
            }
            set
            {
                _includeNullItem = value;
                AddOrRemoveNullItem();
            }
        }

        public string ExcludedCodes
        {
            get
            {
                string ret = "";

                foreach (var item in _ExcludedCodes)
                {
                    ret += item;
                }

                return ret;
            }
            set
            {
                _ExcludedCodes.Clear();

                if(value != null)
                {
                    string[] t = value.Split('|');

                    foreach (var item in t)
                    {
                        _ExcludedCodes.Add(item);
                    }
                }

                return;
            }
        }

        public static DependencyProperty FormatOfDropDownProperty =
          DependencyProperty.Register("FormatOfDropDown", typeof(CodeLookupFormat), typeof(Virtuoso.Core.Controls.codeLookup),
                    new PropertyMetadata((o, e) =>
                    {
                        ((Virtuoso.Core.Controls.codeLookup)o).SetFormatOfDropDown();
                    }));
        private void SetFormatOfDropDown()
        {
        }

        public CodeLookupFormat FormatOfDropDown
        {
            get
            {
                CodeLookupFormat f = (CodeLookupFormat)(base.GetValue(codeLookup.FormatOfDropDownProperty));
                if ((f != CodeLookupFormat.Code) && (f != CodeLookupFormat.CodeDashDescription) && (f != CodeLookupFormat.Description))
                    return CodeLookupFormat.Code;
                else
                    return ((CodeLookupFormat)(base.GetValue(codeLookup.FormatOfDropDownProperty)));
            }
            set
            {
                base.SetValue(codeLookup.FormatOfDropDownProperty, value);
                this.DisplayMemberPath = "DisplayMember";
                if (_loaded) { LoadCodes(); }  // If already did the initial load - reload on change of type
            }
        }

        public static DependencyProperty FormatOfSelectionProperty =
          DependencyProperty.Register("FormatOfSelection", typeof(CodeLookupFormat), typeof(Virtuoso.Core.Controls.codeLookup),
                    new PropertyMetadata((o, e) =>
                    {
                        ((Virtuoso.Core.Controls.codeLookup)o).SetFormatOfSelection();
                    }));
        private void SetFormatOfSelection()
        {
            SetDisplayMembers(FormatOfSelection);
        }
        public CodeLookupFormat FormatOfSelection
        {
            get
            {
                CodeLookupFormat f = (CodeLookupFormat)(base.GetValue(codeLookup.FormatOfSelectionProperty));
                if ((f != CodeLookupFormat.Code) && (f != CodeLookupFormat.CodeDashDescription) && (f != CodeLookupFormat.Description))
                    return CodeLookupFormat.Code;
                else
                    return ((CodeLookupFormat)(base.GetValue(codeLookup.FormatOfSelectionProperty)));
            }
            set
            {
                base.SetValue(codeLookup.FormatOfSelectionProperty, value);
                SetDisplayMembers(value);
            }
        }

        public static readonly DependencyProperty SortOrderProperty =
            DependencyProperty.Register("SortOrder", typeof(string), typeof(Virtuoso.Core.Controls.codeLookup), null);
        public string SortOrder
        {
            get
            {
                return (string)GetValue(SortOrderProperty);
            }
            set
            {
                SetValue(SortOrderProperty, value);
            }
        }

        public static DependencyProperty SelectedKeyProperty =
          DependencyProperty.Register("SelectedKey", typeof(int?), typeof(Virtuoso.Core.Controls.codeLookup),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.codeLookup)o).SetSelectionFromKey();
          }));

        public int? SelectedKey
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.codeLookup.SelectedKeyProperty))); }
            set { if (!_loaded) LoadCodes(); base.SetValue(Virtuoso.Core.Controls.codeLookup.SelectedKeyProperty, value); }
        }
        public static DependencyProperty SequenceFilterKeyProperty =
          DependencyProperty.Register("SequenceFilterKey", typeof(int?), typeof(Virtuoso.Core.Controls.codeLookup),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.codeLookup)o).SetSequenceFilterKey();
          }));

        public int? SequenceFilterKey
        {
            get { return ((int?)(base.GetValue(Virtuoso.Core.Controls.codeLookup.SequenceFilterKeyProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.codeLookup.SequenceFilterKeyProperty, value); }
        }
        private void SetSequenceFilterKey()
        {
            LoadCodes();
        }
        private int? GetSelectedKey(object item)
        {
            return (int?)item.GetType().GetProperty(SelectedKeyPath).GetValue(item, null);
        }

        private string GetDisplayMemberToUpper(object item)
        {
            return (string)item.GetType().GetProperty(SelectedDisplayMemberPath).GetValue(item, null).ToString().ToUpper();
        }

        bool inHere = false;
        private void SetSelectionFromKey()
        {
            if (inHere) { return; }
            inHere = true;
            if (!_loaded) LoadCodes();
            var value = SelectedKey;
            if (Items.Count > 0)
            {
                CodeLookup sel = null;
                sel = (CodeLookup)(from item in Items
                                   where GetSelectedKey(item) == value
                                   select item).FirstOrDefault();
                CodeLookup cl = (CodeLookup)sel;
                if ((sel == null) && (value != null))
                {

                }
                else if ((sel != null) && (!cl.Inactive))
                {
                    // Once user selects a non-Inactive item - remove the Inactive item from the list if its in there
                    RemoveInvalidItem();
                }
                SelectedItem = sel;
            }
            else
            {
                SelectedItem = null;
                RemoveInvalidItem();
            }
            inHere = false;
        }
        private void AddInvalidItem(int pInvalidKey)
        {
            if (pInvalidKey == 0) return;
            List<CodeLookup> temp = new List<CodeLookup>();
            AddNullItem();
            CodeLookup cli = null; // CodeLookupCache.GetCodeLookupFromKey(pInvalidKey);
            if (((cli == null) && (_includeNullItem == false)) || ((cli == null) && (_includeNullItem == true)))
            {
                cli = new CodeLookup();
                cli.CodeLookupKey = pInvalidKey;
                cli.Code = String.Format("<Key={0}>", pInvalidKey.ToString().Trim());
                cli.CodeDescription = String.Format("<CodeLookup {0} Key={1}>", this.CodeType, pInvalidKey.ToString().Trim());
                cli.CodeDashDescription = String.Format("{0} - {1}", cli.Code, cli.CodeDescription);
                cli.Inactive = true;
            }
            if (cli != null) temp.Add(cli);
            foreach (CodeLookup clo in _itemSourceOriginal)
            {
                temp.Add(clo);
            }
            _itemSource = new PagedCollectionView(temp);
            ResetItemsSource(pInvalidKey);
        }

        private void RemoveInvalidItem()
        {
            if (_itemSourceOriginal == null) { return; }
            bool _found = false;
            foreach (CodeLookup cl in _itemSourceOriginal)
            {
                if (cl.Inactive) { _found = true; break; }
            }
            if (!_found) { return; }

            int? currentKey = SelectedKey;
            _itemSource = new PagedCollectionView(_itemSourceOriginal);
            ResetItemsSource(currentKey);
        }

        private void AddOrRemoveNullItem()
        {
            if (_itemSourceOriginal == null) { return; }
            int? currentKey = SelectedKey;
            if (_includeNullItem)
            {
                // Add null item as the first item - if it does not yet exist
                bool _found = false;
                foreach (CodeLookup cl in _itemSourceOriginal)
                {
                    if (string.Compare(cl.Code, "") == 0) { _found = true; break; }
                }
                if (_found) { return; }

                //_itemSource = new List<CodeLookup>();
                AddNullItem();
                //foreach (CodeLookup clo in _itemSourceOriginal)
                //{
                //    _itemSource.Add(clo);
                //}
                ResetItemsSource(currentKey);
            }
            else
            {
                // remove null item if it exisits
                int i = 0;
                foreach (CodeLookup cl in _itemSource)
                {
                    if (string.Compare(cl.Code, "") == 0)
                    {
                        _itemSourceOriginal.Remove(cl);
                        break;
                    }
                    i++;
                }
                ResetItemsSource(currentKey);
            }
        }
        private void AddNullItem()
        {
            if (!_includeNullItem) { return; }
            CodeLookup cl = new CodeLookup();
            cl.Code = "";
            cl.CodeDashDescription = "";
            cl.CodeDescription = "";
            cl.NullableCodeLookupKey = null;
            cl.CodeLookupKey = 0;
            cl.DisplayMember = "";
            cl.Inactive = false;
            _itemSourceOriginal.Insert(0, cl);
        }

        private void ResetItemsSource(int? pCurrentKey)
        {
            if (_itemSource == null) return;
            _itemSource.Refresh();
            if (_itemSource.SortDescriptions.Count() == 0)
            {
                _itemSource.SortDescriptions.Add(new SortDescription("Inactive", ListSortDirection.Ascending));
                _itemSource.SortDescriptions.Add(new SortDescription("CodeDescription", ListSortDirection.Ascending));
            }
            this.ItemsSource = _itemSource;
            this.OnItemsChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
            SetDisplayMembers(FormatOfSelection);
            SelectedKey = pCurrentKey;
        }
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.NewItems == null && e.OldItems == null && this.ItemsSource == null)
                {
                    return; //Need to bail - likely in Cleanup method
                }
                base.OnItemsChanged(e);
                SetSelectionFromKey();
            }
            catch (Exception ex)
            {
				System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        // Hide DisplayMemberPath and ItemsSource fields and properties
        // This doesn't hide them - but you can't use them in the xaml
        //private new static DependencyProperty DisplayMemberPathProperty =
        //  DependencyProperty.Register("DisplayMemberPath", typeof(string), typeof(Virtuoso.Core.Controls.codeLookup), null);
        //private new string DisplayMemberPath
        //{
        //    get { return base.DisplayMemberPath; }
        //    set { base.DisplayMemberPath = value; }
        //}
        private new static DependencyProperty ItemsSourceProperty =
         DependencyProperty.Register("ItemsSourcePath", typeof(System.Collections.IEnumerable), typeof(Virtuoso.Core.Controls.codeLookup), null);
        private new System.Collections.IEnumerable ItemsSource
        {
            get { return base.ItemsSource; }
            set { base.ItemsSource = value; }
        }
        public void Cleanup()
        {
            this.Loaded -= codeLookup_Loaded;
            this.KeyUp -= codeLookup_KeyUp;
            this.DropDownOpened -= codeLookup_DropDownOpened;
            this.DropDownClosed -= codeLookup_DropDownClosed;
            this.SelectionChanged -= codeLookup_SelectionChanged;

            this._itemSourceOriginal?.Clear();
            this._itemSourceOriginal = null;

            if (_ExcludedCodes != null) this._ExcludedCodes.Clear();
            _ExcludedCodes = null;

            var itemSourceList = this.ItemsSource as ICollectionView;
            if (itemSourceList != null)
            {
                (itemSourceList.SourceCollection as IList)?.Clear();
                this.ItemsSource = null;
            }
        }
    }
}
