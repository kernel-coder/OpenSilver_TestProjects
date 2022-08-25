using System.Windows;
using System.Windows.Controls;

namespace Virtuoso.Core.Controls
{
    public class FocusHelper : Control
    {
        public static readonly DependencyProperty TargetElementProperty =
            DependencyProperty.Register(
                "TargetElement",
                typeof(Control),
                typeof(FocusHelper),
                null);

        public Control TargetElement
        {
            get { return (Control)GetValue(TargetElementProperty); }
            set { SetValue(TargetElementProperty, value); }
        }

        public static readonly DependencyProperty SetFocusProperty = DependencyProperty.Register(
            "SetFocus",
            typeof(bool),
            typeof(FocusHelper),
            new PropertyMetadata(false, FocusChanged));

        public bool SetFocus
        {
            get { return (bool)GetValue(SetFocusProperty); }
            set { SetValue(SetFocusProperty, value); }
        }

        private static void FocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var targetElement = d.GetValue(TargetElementProperty) as Control;
            if (targetElement == null || e.NewValue == null || (!((bool)e.NewValue)))
            {
                return;
            }
            targetElement.Focus();
            d.SetValue(SetFocusProperty, false);
        }

    }
}
