#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using CB = System.Windows.Controls.ComboBox;

#endregion

namespace Virtuoso.Core.Behaviors
{
    //http://blogs.msdn.com/b/kylemc/archive/2010/06/18/combobox-sample-for-ria-services.aspx

    public static class ComboBox
    {
        public enum ComboBoxMode
        {
            Default = 0,
            Async,
            AsyncEager,
        }

        #region Mode

        public static DependencyProperty ModeProperty =
            DependencyProperty.RegisterAttached(
                "Mode",
                typeof(ComboBoxMode),
                typeof(ComboBox),
                new PropertyMetadata(ComboBoxMode.Default, ModePropertyChanged));

        public static ComboBoxMode GetMode(DependencyObject target)
        {
            return (ComboBoxMode)target.GetValue(ModeProperty);
        }

        public static void SetMode(DependencyObject target, ComboBoxMode mode)
        {
            target.SetValue(ModeProperty, mode);
        }

        private static void ModePropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Add a shim that manages the ItemsSource, SeletedItem, and SelectedValue bindings. The shim 
            // may eagerly select the selected item while the list is loading and will refresh the bindings 
            // when the load completes. 
            if (!DesignerProperties.IsInDesignTool && ((ComboBoxMode)e.NewValue != ComboBoxMode.Default))
            {
                CB comboBox = sender as CB;
                if (comboBox != null)
                {
                    Shim.Create(comboBox, (ComboBoxMode)e.NewValue);
                }
            }
        }

        #endregion

        public class Shim : INotifyPropertyChanged
        {
            private readonly BindingListener _bindingListener;

            // VM properties 
            private List<object> _items = new List<object>();
            private object _selectedItem;
            private string _displayMemberPath;

            // Specifies whether the shim is working with underlying bindings for SelectedValue 
            private bool _selectedValueMode;

            // Specifies whether the SelectedItem is eagerly selected before it is available in the Items 
            private bool _useEagerSelection;

            // Suppresses the SelectedItem setter when making updates to the Items collection 
            private bool _ignoreCallback;

            public static Shim Create(CB comboBox, ComboBoxMode mode)
            {
                Shim shim = new Shim();
                shim.Initialize(comboBox, (mode == ComboBoxMode.AsyncEager));
                return shim;
            }

            private Shim()
            {
                _bindingListener = new BindingListener(this);
            }

            private void Initialize(CB comboBox, bool useEagerSelection)
            {
                // Add to visual tree to enable ElementName binding 
                comboBox.Tag = _bindingListener;

                _useEagerSelection = useEagerSelection;

                BindingExpression be = comboBox.GetBindingExpression(ItemsControl.ItemsSourceProperty);
                if (be != null)
                {
                    BindingOperations.SetBinding(_bindingListener, BindingListener.ItemsSourceProperty,
                        be.ParentBinding);
                    BindingOperations.SetBinding(comboBox, ItemsControl.ItemsSourceProperty,
                        new Binding("Items") { Source = this });
                }

                be = comboBox.GetBindingExpression(Selector.SelectedItemProperty);
                if (be != null)
                {
                    BindingOperations.SetBinding(_bindingListener, BindingListener.SelectedItemProperty,
                        be.ParentBinding);
                    BindingOperations.SetBinding(comboBox, Selector.SelectedItemProperty,
                        new Binding("SelectedItem") { Source = this, Mode = BindingMode.TwoWay });
                }

                be = comboBox.GetBindingExpression(Selector.SelectedValueProperty);
                if (be != null)
                {
                    // We'll always bind the ComboBox to SelectedItem, but we'll map it to the existing SelectedValue binding 
                    _selectedValueMode = true;

                    BindingOperations.SetBinding(_bindingListener, BindingListener.SelectedValueProperty,
                        be.ParentBinding);
                    comboBox.ClearValue(Selector.SelectedValueProperty);

                    // Bind SelectedValuePath 
                    be = comboBox.GetBindingExpression(Selector.SelectedValuePathProperty);
                    if (be != null)
                    {
                        BindingOperations.SetBinding(_bindingListener, BindingListener.SelectedValuePathProperty,
                            be.ParentBinding);
                        comboBox.ClearValue(Selector.SelectedValuePathProperty);
                    }
                    else
                    {
                        _bindingListener.SelectedValuePath = comboBox.SelectedValuePath;
                        comboBox.SelectedValuePath = null;
                    }

                    // Bind DisplayMemberPath 
                    be = comboBox.GetBindingExpression(Selector.DisplayMemberPathProperty);
                    if (be != null)
                    {
                        BindingOperations.SetBinding(_bindingListener, BindingListener.DisplayMemberPathProperty,
                            be.ParentBinding);
                    }
                    else
                    {
                        _bindingListener.DisplayMemberPath = comboBox.DisplayMemberPath;
                    }

                    BindingOperations.SetBinding(comboBox, Selector.DisplayMemberPathProperty,
                        new Binding("DisplayMemberPath") { Source = this });

                    // Check selection mode and path properties 
                    if (_useEagerSelection &&
                        (_bindingListener.DisplayMemberPath != _bindingListener.SelectedValuePath))
                    {
                        throw new InvalidOperationException(
                            "Cannot use eager selection when the DisplayMemberPath and the SelectedValuePath differ. Try using basic ComboBoxMode.Async selection instead.");
                    }

                    BindingOperations.SetBinding(comboBox, Selector.SelectedItemProperty,
                        new Binding("SelectedItem") { Source = this, Mode = BindingMode.TwoWay });
                }
            }

            public IEnumerable Items => _items;

            public object SelectedItem
            {
                get { return _selectedItem; }
                set
                {
                    if (!_ignoreCallback && (_selectedItem != value))
                    {
                        _selectedItem = value;
                        if (_selectedValueMode)
                        {
                            _bindingListener.SelectedValue = GetValue(value, _bindingListener.SelectedValuePath);
                        }
                        else
                        {
                            _bindingListener.SetValue(BindingListener.SelectedItemProperty, value);
                        }

                        RaisePropertyChanged("SelectedItem");
                    }
                }
            }

            public string DisplayMemberPath => _displayMemberPath;

            private void HandleItemsSourceChanged(DependencyPropertyChangedEventArgs e)
            {
                UpdateSubscription(e.OldValue as INotifyCollectionChanged, e.NewValue as INotifyCollectionChanged);
                SyncToItemsSource();
            }

            private void HandleSelectedItemChanged(DependencyPropertyChangedEventArgs e)
            {
                SyncToSelectedItem();
            }

            private void HandleSelectedValueChanged(DependencyPropertyChangedEventArgs e)
            {
                SyncToSelectedValue();
            }

            private void HandleSelectedValuePathChanged(DependencyPropertyChangedEventArgs e)
            {
                SyncToSelectedValue();
            }

            private void HandleDisplayMemberPathChanged(DependencyPropertyChangedEventArgs e)
            {
                _displayMemberPath = _bindingListener.DisplayMemberPath;
                RaisePropertyChanged("DisplayMemberPath");
            }

            private void UpdateSubscription(INotifyCollectionChanged oldIncc, INotifyCollectionChanged newIncc)
            {
                if (oldIncc != null)
                {
                    oldIncc.CollectionChanged -= OnCollectionChanged;
                }

                if (newIncc != null)
                {
                    newIncc.CollectionChanged += OnCollectionChanged;
                }
            }

            private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                SyncToItemsSource();
            }

            private void SyncToItemsSource()
            {
                _items = (_bindingListener.ItemsSource == null)
                    ? new List<object>()
                    : _bindingListener.ItemsSource.Cast<object>().ToList();

                _ignoreCallback = true;
                RaisePropertyChanged("Items");
                _ignoreCallback = false;

                if (_selectedValueMode)
                {
                    // Reset selected item and path from the value specified during eager selection 
                    _selectedItem = GetSelectedItemFromSelectedValue();
                    _displayMemberPath = _bindingListener.DisplayMemberPath;
                    RaisePropertyChanged("DisplayMemberPath");
                }

                RaisePropertyChanged("SelectedItem");
            }

            private void SyncToSelectedItem()
            {
                if (_useEagerSelection && !_items.Contains(_bindingListener.SelectedItem))
                {
                    // Eagerly add SelectedItem to the list and assume it will be valid once the items are updated 
                    _items = new List<object> { _bindingListener.SelectedItem };

                    _ignoreCallback = true;
                    RaisePropertyChanged("Items");
                    _ignoreCallback = false;
                }

                _selectedItem = _bindingListener.SelectedItem;
                RaisePropertyChanged("SelectedItem");
            }

            private void SyncToSelectedValue()
            {
                object selectedItem = GetSelectedItemFromSelectedValue();
                if (_useEagerSelection && (selectedItem == null))
                {
                    // Eagerly add SelectedValue to the list and assume it will be valid once the items are updated 
                    _items = new List<object> { _bindingListener.SelectedValue };

                    _ignoreCallback = true;
                    RaisePropertyChanged("Items");
                    _ignoreCallback = false;

                    // Clear the DisplayMemberPath for eager selection so the binding displays the SelectedValue literally 
                    _selectedItem = _bindingListener.SelectedValue;
                    _displayMemberPath = null;
                    RaisePropertyChanged("DisplayMemberPath");
                    RaisePropertyChanged("SelectedItem");
                }
                else
                {
                    // Reset the DisplayMemberPath in case it was changed during eager selection 
                    _selectedItem = selectedItem;
                    _displayMemberPath = _bindingListener.DisplayMemberPath;
                    RaisePropertyChanged("DisplayMemberPath");
                    RaisePropertyChanged("SelectedItem");
                }
            }

            private object GetSelectedItemFromSelectedValue()
            {
                foreach (object item in Items)
                    if (Equals(GetValue(item, _bindingListener.SelectedValuePath), _bindingListener.SelectedValue))
                    {
                        return item;
                    }

                return null;
            }

            private static object GetValue(object item, string valuePath)
            {
                string[] paths = (valuePath == null) ? new string[0] : valuePath.Split('.');
                foreach (string path in paths)
                {
                    if (item == null)
                    {
                        break;
                    }

                    PropertyInfo property = item.GetType().GetProperty(path);
                    if (property == null)
                    {
                        item = null;
                        break;
                    }

                    item = property.GetValue(item, null);
                }

                return item;
            }

            #region INotifyPropertyChanged

            public event PropertyChangedEventHandler PropertyChanged;

            private void RaisePropertyChanged(string propertyName)
            {
                OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
            }

            private void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, e);
                }
            }

            #endregion

            #region BindingListener

            private class BindingListener : DependencyObject
            {
                public static readonly DependencyProperty ItemsSourceProperty =
                    DependencyProperty.Register(
                        "ItemsSource",
                        typeof(IEnumerable),
                        typeof(BindingListener),
                        new PropertyMetadata(null, HandleItemsSourceChanged));

                public static readonly DependencyProperty SelectedItemProperty =
                    DependencyProperty.Register(
                        "SelectedItem",
                        typeof(object),
                        typeof(BindingListener),
                        new PropertyMetadata(null, HandleSelectedItemChanged));

                public static readonly DependencyProperty SelectedValueProperty =
                    DependencyProperty.Register(
                        "SelectedValue",
                        typeof(object),
                        typeof(BindingListener),
                        new PropertyMetadata(null, HandleSelectedValueChanged));

                public static readonly DependencyProperty SelectedValuePathProperty =
                    DependencyProperty.Register(
                        "SelectedValuePath",
                        typeof(string),
                        typeof(BindingListener),
                        new PropertyMetadata(null, HandleSelectedValuePathChanged));

                public static readonly DependencyProperty DisplayMemberPathProperty =
                    DependencyProperty.Register(
                        "DisplayMemberPath",
                        typeof(string),
                        typeof(BindingListener),
                        new PropertyMetadata(null, HandleDisplayMemberPathChanged));

                private static void HandleItemsSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
                {
                    ((BindingListener)sender)._shim.HandleItemsSourceChanged(e);
                }

                private static void HandleSelectedItemChanged(object sender, DependencyPropertyChangedEventArgs e)
                {
                    ((BindingListener)sender)._shim.HandleSelectedItemChanged(e);
                }

                private static void HandleSelectedValueChanged(object sender, DependencyPropertyChangedEventArgs e)
                {
                    ((BindingListener)sender)._shim.HandleSelectedValueChanged(e);
                }

                private static void HandleSelectedValuePathChanged(object sender, DependencyPropertyChangedEventArgs e)
                {
                    ((BindingListener)sender)._shim.HandleSelectedValuePathChanged(e);
                }

                private static void HandleDisplayMemberPathChanged(object sender, DependencyPropertyChangedEventArgs e)
                {
                    ((BindingListener)sender)._shim.HandleDisplayMemberPathChanged(e);
                }

                public IEnumerable ItemsSource
                {
                    get { return (IEnumerable)GetValue(ItemsSourceProperty); }
                    set { SetValue(ItemsSourceProperty, value); }
                }

                public object SelectedItem
                {
                    get { return GetValue(SelectedItemProperty); }
                    set { SetValue(SelectedItemProperty, value); }
                }

                public object SelectedValue
                {
                    get { return GetValue(SelectedValueProperty); }
                    set { SetValue(SelectedValueProperty, value); }
                }

                public string SelectedValuePath
                {
                    get { return (string)GetValue(SelectedValuePathProperty); }
                    set { SetValue(SelectedValuePathProperty, value); }
                }

                public string DisplayMemberPath
                {
                    get { return (string)GetValue(DisplayMemberPathProperty); }
                    set { SetValue(DisplayMemberPathProperty, value); }
                }

                private readonly Shim _shim;

                public BindingListener(Shim shim)
                {
                    _shim = shim;
                }
            }

            #endregion
        }
    }
}