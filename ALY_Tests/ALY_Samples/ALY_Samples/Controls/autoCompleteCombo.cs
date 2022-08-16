using GalaSoft.MvvmLight;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Virtuoso.Core.Interface;

namespace Virtuoso.Core.Controls
{
    //http://www.codeproject.com/KB/silverlight/AutoComplete_ComboBox.aspx
    public class autoCompleteCombo : AutoCompleteBox, ICustomCtrlContentPresenter, ICleanup
    {
        bool isUpdatingDPs = false;
        //KSM 05302014
        public static DependencyProperty IsContainsSearchProperty =
         DependencyProperty.Register("IsContainsSearch", typeof(object), typeof(Virtuoso.Core.Controls.autoCompleteCombo),  new PropertyMetadata(false));
        public bool IsContainsSearch
        {
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.autoCompleteCombo.IsContainsSearchProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.autoCompleteCombo.IsContainsSearchProperty, value); }
        }
        //KSM

        public static DependencyProperty IsTabStopCustomProperty =
           DependencyProperty.Register("IsTabStopCustom", typeof(object), typeof(Virtuoso.Core.Controls.autoCompleteCombo), null);
        public bool IsTabStopCustom
        {
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.autoCompleteCombo.IsTabStopCustomProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.autoCompleteCombo.IsTabStopCustomProperty, value); }
        }
        public static DependencyProperty EmptySelectionValueProperty =
           DependencyProperty.Register("EmptySelectionValue", typeof(object), typeof(Virtuoso.Core.Controls.autoCompleteCombo), null);
        public object EmptySelectionValue
        {
            get { return ((object)(base.GetValue(Virtuoso.Core.Controls.autoCompleteCombo.EmptySelectionValueProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.autoCompleteCombo.EmptySelectionValueProperty, value); }
        }
        public autoCompleteCombo()
            : base()
        {
            SetCustomFilter();
            this.IsTabStopCustom = true;
            this.DefaultStyleKey = typeof(autoCompleteCombo);
        }
        public void Cleanup()
        {
            ToggleButton toggle = (ToggleButton)GetTemplateChild("DropDownToggle");
            if (toggle != null)
            {
                toggle.Click -= DropDownToggle_Click;
            }
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ToggleButton toggle = (ToggleButton)GetTemplateChild("DropDownToggle");
            if (toggle != null)
            {
                toggle.Click += DropDownToggle_Click;
            }
        }

        private void DropDownToggle_Click(object sender, RoutedEventArgs e)
        {
            //this.Focus();
            FrameworkElement fe = sender as FrameworkElement;
            AutoCompleteBox acb = null;
            while (fe != null && acb == null)
            {
                fe = VisualTreeHelper.GetParent(fe) as FrameworkElement;
                acb = fe as AutoCompleteBox;
            }
            if (acb != null)
            {
                acb.IsDropDownOpen = !acb.IsDropDownOpen;
            }
        }

        protected virtual void SetCustomFilter()
        {
            //custom logic: how to autocomplete 
            this.ItemFilter = (prefix, item) =>
            {
                //return all items for empty prefix
                if (string.IsNullOrEmpty(prefix))
                    return true;

                //return all items if a record is already selected
                if (this.SelectedItem != null)
                    if (this.SelectedItem.ToString() == prefix)
                        return true;

                //KSM05302014
                ////else return items that contains prefex
                //if (prefix.Length == 1)
                //    return item.ToString().ToLower().StartsWith(prefix.ToLower());
                //else
                //    return item.ToString().ToLower().Contains(prefix.ToLower());
                if (!IsContainsSearch)
                {
                    if (prefix.Length == 1)
                        return item.ToString().ToLower().StartsWith(prefix.ToLower());
                    else
                        return item.ToString().ToLower().Contains(prefix.ToLower());
                }
                else return item.ToString().ToLower().Contains(prefix.ToLower()); 
                //KSM
            };
        }

        //highlighting logic
        protected override void OnPopulated(PopulatedEventArgs e)
        {
            base.OnPopulated(e);
            ListBox listBox = GetTemplateChild("Selector") as ListBox;
            if (listBox != null)
            {
                //highlight the selected item, if any
                if (this.ItemsSource != null && this.SelectedItem != null)
                {
                    listBox.SelectedItem = this.SelectedItem;
                    listBox.Dispatcher.BeginInvoke(delegate
                    {
                        listBox.UpdateLayout();
                        listBox.ScrollIntoView(listBox.SelectedItem);
                    });
                }
            }
        }

        protected override void OnDropDownClosed(RoutedPropertyChangedEventArgs<bool> e)
        {
            base.OnDropDownClosed(e);
            UpdateCustomDPs();

            this.Focus();
        }

        private void UpdateCustomDPs()
        {
            //flag to ensure that that we dont reselect the selected item
            this.isUpdatingDPs = true;

            //if a new item is selected or the user blanked out the selection, update the DP
            if (this.SelectedItem != null || this.Text == string.Empty)
            {
                //update the SelectedValue DP 
                string propertyPath = this.SelectedValuePath;
                if (!string.IsNullOrEmpty(propertyPath))
                {
                    if (this.SelectedItem != null)
                    {
                        PropertyInfo propertyInfo = this.SelectedItem.GetType().GetProperty(propertyPath);

                        //get property from selected item
                        object propertyValue = propertyInfo.GetValue(this.SelectedItem, null);

                        //update the datacontext
                        this.SelectedValue = propertyValue;
                    }
                    else
                    {
                        this.SelectedValue = null;
                    }
                }
            }
            else
            {
                if (this.GetBindingExpression(SelectedValueProperty) != null)
                {
                    this.isUpdatingDPs = false;
                    SetSelectemItemUsingSelectedValueDP();
                }
            }

            this.isUpdatingDPs = false;
        }

        #region SelectedValue

        /// <summary>
        /// SelectedValue Dependency Property
        /// </summary>
        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register("SelectedValue",
                    typeof(object),
                    typeof(autoCompleteCombo),
                    new PropertyMetadata(new PropertyChangedCallback(OnSelectedValueChanged))
                    );

        /// <summary>
        /// Gets or sets the SelectedValue property.
        /// </summary>
        public object SelectedValue
        {
            get { return GetValue(SelectedValueProperty); }
            set { SetValue(SelectedValueProperty, value); }
        }

        /// <summary>
        /// Handles changes to the SelectedValue property.
        /// </summary>
        private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((autoCompleteCombo)d).OnSelectedValueChanged(e);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the SelectedValue property.
        /// </summary>
        protected virtual void OnSelectedValueChanged(DependencyPropertyChangedEventArgs e)
        {
            SetSelectemItemUsingSelectedValueDP();
        }

        //selects the item whose value is given in SelectedValueDP
        public void SetSelectemItemUsingSelectedValueDP()
        {
            if (!this.isUpdatingDPs)
            {
                if (this.ItemsSource != null)
                {     
                    //// if selectedValue is empty, remove the current selection
                    if (EmptySelectionValue == null && this.SelectedValue == null 
                            || ( this.SelectedValue is Int32 && Convert.ToInt32(this.SelectedValue) <= 0 && EmptySelectionValue is Int32 
                                            && Convert.ToInt32(EmptySelectionValue) != Convert.ToInt32(this.SelectedValue)))
                    {
                        this.Text = string.Empty;
                        this.SelectedItem = null;
                    }
                    else
                    {
                        object selectedValue = GetValue(SelectedValueProperty);
                        string propertyPath = this.SelectedValuePath;
                        if (selectedValue != null && !(string.IsNullOrEmpty(propertyPath)))
                        {
                            if (this.ItemsSource.Cast<object>().Count() == 0)
                            {
                                this.Text = string.Empty;
                                this.SelectedItem = null;
                            }
                            /// loop through each item in the item source 
                            /// and see if its <SelectedValuePathProperty> == SelectedValue
                            foreach (object item in this.ItemsSource)
                            {
                                PropertyInfo propertyInfo = item.GetType().GetProperty(propertyPath);
                                if (propertyInfo.GetValue(item, null).Equals(selectedValue))
                                {
                                    this.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region SelectedValuePath

        /// <summary>
        /// SelectedValuePath Dependency Property
        /// </summary>
        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register("SelectedValuePath",
                    typeof(string),
                    typeof(autoCompleteCombo),
                    null
                    );

        /// <summary>
        /// Gets or sets the SelectedValuePath property.
        /// </summary>
        public string SelectedValuePath
        {
            get { return GetValue(SelectedValuePathProperty) as string; }
            set { SetValue(SelectedValuePathProperty, value); }
        }

        #endregion
    }
}