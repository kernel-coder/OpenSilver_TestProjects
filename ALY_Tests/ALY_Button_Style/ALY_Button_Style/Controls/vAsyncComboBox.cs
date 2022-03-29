using GalaSoft.MvvmLight;
using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Virtuoso.Core.Controls
{
    // http://info.titodotnet.com/2011/06/silverlight-4-combobox-selecteditem.html
    // In Silverlight 4/5, the SelectedItem is set to null when we update/change/re-bind the ItemsSource.

    public class vAsyncComboBox : ComboBox
    {
        public vAsyncComboBox()
        {
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreComboBoxStyle"]; }
            catch { }
            this.DropDownOpened += new EventHandler(vAsyncComboBox_DropDownOpened);
        }
        public void Cleanup()
        {
            this.DropDownOpened -= vAsyncComboBox_DropDownOpened;
        }
        // Update the current selected item when the ItemsSource changes.
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            object persistenceSelectedItem = null;
            if (this.ItemsSource != null)
            {
                foreach (object item in this.ItemsSource)
                {
                    if (item.Equals(this.SelectedItem)) persistenceSelectedItem = item;
                }
            }
            //base.OnItemsChanged(e); - if you leave this in here - the SelectedItem goes to null - then gets reset - trigger .HasChanges and Update (history) upon SaveAsync
            if (this.SelectedItem != persistenceSelectedItem) this.SelectedItem = persistenceSelectedItem;
        }
        public static DependencyProperty OverridePopupWidthProperty = DependencyProperty.Register("OverridePopupWidth", typeof(int?), typeof(Virtuoso.Core.Controls.vAsyncComboBox), null);

        public int? OverridePopupWidth
        {
            get { return ((int?)(base.GetValue(vAsyncComboBox.OverridePopupWidthProperty))); }
            set { base.SetValue(vAsyncComboBox.OverridePopupWidthProperty, value); }
        }
        private void vAsyncComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (OverridePopupWidth == null) return;
            Border border = (Border)GetTemplateChild("PopupBorder");
            if (border != null) border.Width = double.Parse(OverridePopupWidth.ToString());
        }
    }

    public class vAsyncComboBoxV2 : ComboBox, ICleanup
    {
        // https://connect.microsoft.com/VisualStudio/feedbackdetail/view/523394/silverlight-forum-combobox-selecteditem-binding
        // After change the ItemsSrouce the SelectedItem binding does not function as expected.
        // It is verified the ComboBox can find a match Item in it's new itemsSource.

        private BindingExpression bE;

        public vAsyncComboBoxV2()
        {
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreComboBoxStyle"]; }
            catch { }
            //this.SelectionChanged += new SelectionChangedEventHandler(XComboBox_SelectionChanged);
        }

        // Update the current selected item when the ItemsSource changes.
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            UpdateSelectedValue();
        }

        //void XComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    UpdateSelectedValue();
        //}

        void UpdateSelectedValue()
        {
            if (bE == null)
            {
                bE = this.GetBindingExpression(ComboBox.SelectedValueProperty);
            }
            else
            {
                if (this.GetBindingExpression(ComboBox.SelectedValueProperty) == null)
                {
                    this.SetBinding(ComboBox.SelectedValueProperty, bE.ParentBinding);
                }
            }
        }

        public void Cleanup()
        {
            this.ClearValue(ComboBox.SelectedValueProperty);
            this.ClearValue(ComboBox.SelectedValuePathProperty);
            this.bE = null;
        }
    }

    public class vAsyncComboBoxV3 : ComboBox
    {
        // To be used when the ItemSource is being changed. This control nulls the SelectedItem and then reevaluates it.
        // caurni00 - 2/16/15
        private BindingExpression bE;
        private BindingExpression selItem;

        public vAsyncComboBoxV3()
        {
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreComboBoxStyle"]; }
            catch { }
        }

        // Update the current selected item when the ItemsSource changes.
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            this.SelectedItem = null;
            UpdateSelectedValue();
            UpdateSelectedItem();
        }

        void UpdateSelectedValue()
        {
            if (bE == null)
            {
                bE = this.GetBindingExpression(ComboBox.SelectedValueProperty);
            }
            else
            {
                if (this.GetBindingExpression(ComboBox.SelectedValueProperty) == null)
                {
                    this.SetBinding(ComboBox.SelectedValueProperty, bE.ParentBinding);
                }
            }
        }

        void UpdateSelectedItem()
        {
            if (selItem == null)
            {
                selItem = this.GetBindingExpression(ComboBox.SelectedItemProperty);
            }
            else
            {
                if (this.GetBindingExpression(ComboBox.SelectedItemProperty) == null)
                {
                    this.SetBinding(ComboBox.SelectedItemProperty, selItem.ParentBinding);
                }
            }
        }
    }


    public class vAsyncToNullComboBox : ComboBox
    {
        public vAsyncToNullComboBox()
        {
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreComboBoxStyle"]; }
            catch { }
        }
        // Update the current selected item when the ItemsSource changes.
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.ItemsSource != null) this.SelectedItem = null;
        }
    }
}
