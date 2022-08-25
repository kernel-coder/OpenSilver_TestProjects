using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Virtuoso.Core.Controls
{
    //https://social.msdn.microsoft.com/Forums/vstudio/en-US/07c9fd71-4f9c-45c2-939f-d876d0c19e23/datagrid-template-column-visibility-binding-silverlight?forum=wpf
    public class ExtendedDataGridTextColumn : DataGridTextColumn
    {
        public static readonly DependencyProperty BindableVisibilityProperty = DependencyProperty.Register(
            "BindableVisibility", typeof(Visibility), typeof(ExtendedDataGridTextColumn), new PropertyMetadata(Visibility.Visible, new PropertyChangedCallback(OnVisibilityChanged)));
        
        public Visibility BindableVisibility
        {
            get
            {
                return (Visibility)this.GetValue(BindableVisibilityProperty);
            }
            set
            {
                this.SetValue(BindableVisibilityProperty, value);
            }
        }
        private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ExtendedDataGridTextColumn)d).Visibility = (Visibility)e.NewValue;
        }
    }
}