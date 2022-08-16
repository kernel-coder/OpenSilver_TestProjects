#region Usings

using System.Windows;
using System.Windows.Controls;

#endregion

namespace Virtuoso.Core
{
    public static class BindableDialogResult
    {
        public static readonly DependencyProperty DialogResultProperty
            = DependencyProperty.RegisterAttached(
                "DialogResult",
                typeof(bool?),
                typeof(BindableDialogResult),
                new PropertyMetadata(OnSetDialogResultCallback));

        public static void SetDialogResult(ChildWindow childWindow, bool? dialogResult)
        {
            childWindow.SetValue(DialogResultProperty, dialogResult);
        }

        public static bool? GetDialogResult(ChildWindow childWindow)
        {
            return childWindow.GetValue(DialogResultProperty) as bool?;
        }

        private static void OnSetDialogResultCallback
            (DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ChildWindow childWindow = dependencyObject as ChildWindow;
            if (childWindow != null)
            {
                childWindow.DialogResult = e.NewValue as bool?;
            }
        }
    }
}