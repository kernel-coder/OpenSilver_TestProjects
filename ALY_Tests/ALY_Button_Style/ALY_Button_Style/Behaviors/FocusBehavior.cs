#region Usings

using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

#endregion

namespace Virtuoso.Core.Behaviors
{
    public class FocusBehavior : Behavior<Control>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.GotFocus += (sender, args) => IsFocused = true;
            AssociatedObject.LostFocus += (sender, a) => IsFocused = false;
            AssociatedObject.Loaded += (o, a) =>
            {
                if (HasInitialFocus || IsFocused)
                {
                    AssociatedObject.Focus();
                }
            };
            if (IsFocused)
            {
                SetControlFocus();
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.IsEnabledChanged -= AssociatedObject_IsEnabledChanged;
        }

        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.Register(
                "IsFocused",
                typeof(bool),
                typeof(FocusBehavior),
                new PropertyMetadata(false, IsFocusedChanged)
            );

        private static void IsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                ((FocusBehavior)d).SetControlFocus();
            }
        }

        static Control LastFocusControl;

        public void SetControlFocus()
        {
            if (AssociatedObject is Control)
            {
                LastFocusControl = AssociatedObject;
                //Focus will succeed only if AssociatedObject is Visible, is TabStop and is Enabled
                if (AssociatedObject.Visibility == Visibility.Collapsed)
                {
                    IsFocused = false;
                }

                if (!AssociatedObject.IsTabStop)
                {
                    IsFocused = false;
                }

                //Postpone Focus if control not enable at this time.
                if (!AssociatedObject.IsEnabled)
                {
                    AssociatedObject.IsEnabledChanged += AssociatedObject_IsEnabledChanged;
                }

                var hasFocus = AssociatedObject.Focus();
                IsFocused = hasFocus;
            }
        }

        public
            void AssociatedObject_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AssociatedObject.IsEnabledChanged -= AssociatedObject_IsEnabledChanged;
            if (AssociatedObject is Control)
            {
                SetControlFocus();
            }
        }

        public bool IsFocused
        {
            get { return (bool)GetValue(IsFocusedProperty); }
            set { SetValue(IsFocusedProperty, value); }
        }

        public static readonly DependencyProperty HasInitialFocusProperty =
            DependencyProperty.Register(
                "HasInitialFocus",
                typeof(bool),
                typeof(FocusBehavior),
                new PropertyMetadata(false, null));

        public bool HasInitialFocus
        {
            get { return (bool)GetValue(HasInitialFocusProperty); }
            set { SetValue(HasInitialFocusProperty, value); }
        }
    }
}