﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System;
using System.Collections.Specialized;
using System.Collections.ObjectModel;


namespace Virtuoso.Core.Controls
{
    public partial class EditTabItem : TabItem, GalaSoft.MvvmLight.ICleanup
    {
        ValidationSummary __ValidationSummary { get; set; }

        public EditTabItem()
        {
            InitializeComponent();
            IsErrors = false;
        }
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            //__ValidationSummary = ValidationSummaryHelper.GetValidationSummary(newContent as FrameworkElement);
            if (__ValidationSummary == null) return;
            __ValidationSummary.Errors.CollectionChanged += Errors_CollectionChanged;
        }
        private void Errors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ObservableCollection<ValidationSummaryItem>  errors = sender as ObservableCollection<ValidationSummaryItem>;
            IsErrors = (errors.Count == 0) ? false : true;
        }
        #region IsErrors dependency property
        public bool? IsErrors
        {
            get { return (bool?)GetValue(IsErrorsProperty) == null ? false : (bool?)GetValue(IsErrorsProperty); }
            set { SetValue(IsErrorsProperty, value); }
        }
        public static readonly DependencyProperty IsErrorsProperty =
            DependencyProperty.Register("IsErrors", typeof(bool?), typeof(EditTabItem), new PropertyMetadata(null, new PropertyChangedCallback(IsEditIsErrorsChanged)));
        #endregion

        #region IsEdit dependency property
        public bool? IsEdit
        {
            get { return (bool?)GetValue(IsEditProperty) == null ? false : (bool?)GetValue(IsEditProperty); }
            set { SetValue(IsEditProperty, value); }
        }
        public static readonly DependencyProperty IsEditProperty =
            DependencyProperty.Register("IsEdit", typeof(bool?), typeof(EditTabItem), new PropertyMetadata(null, new PropertyChangedCallback(IsEditIsErrorsChanged)));
        #endregion

        private static void IsEditIsErrorsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            EditTabItem me = sender as EditTabItem;
            if (me == null) return;
            me.IsEditButton = ((bool)me.IsEdit || (bool)me.IsErrors) ? true : false; 
        }

        #region IsEditButton dependency property
        public bool? IsEditButton
        {
            get { return (bool?)GetValue(IsEditButtonProperty) == null ? false : (bool?)GetValue(IsEditButtonProperty); }
            set { SetValue(IsEditButtonProperty, value); }
        }
        public static readonly DependencyProperty IsEditButtonProperty =
            DependencyProperty.Register("IsEditButton", typeof(bool?), typeof(EditTabItem), null);
        #endregion

        public void Cleanup()
        {
            if (__ValidationSummary != null && __ValidationSummary.Errors != null)
            {
                __ValidationSummary.Errors.CollectionChanged -= Errors_CollectionChanged;
            }
        }
    }
}