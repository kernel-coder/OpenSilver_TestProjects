#region Usings

using System.Windows;
using System.Windows.Controls;

#endregion

namespace Virtuoso.Core.Helpers
{
    //http://kosiara87.blogspot.com/2012/02/silverlight-sdkdatagrid-column-header.html
    public static class DataGridColumnHelper
    {
        public static readonly DependencyProperty HeaderBindingProperty = DependencyProperty.RegisterAttached(
            "HeaderBinding",
            typeof(object),
            typeof(DataGridColumnHelper),
            new PropertyMetadata((o, e) => { HeaderBinding_PropertyChanged(o, e); }));

        public static object GetHeaderBinding(DependencyObject source)
        {
            return source.GetValue(HeaderBindingProperty);
        }

        public static void SetHeaderBinding(DependencyObject target, object value)
        {
            target.SetValue(HeaderBindingProperty, value);
        }

        private static void HeaderBinding_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGridColumn column = d as DataGridColumn;
            if (column == null)
            {
                return;
            }

            column.Header = e.NewValue;
        }
    }
}