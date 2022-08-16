#region Usings

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;

#endregion

namespace Virtuoso.Core.Behaviors
{
    public class CheckBoxChangedBehavior : Behavior<CheckBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Checked += AssociatedObject_Checked;
            AssociatedObject.Unchecked += AssociatedObject_Checked;
        }

        void AssociatedObject_Checked(object sender, RoutedEventArgs e)
        {
            BindingExpression binding = AssociatedObject.GetBindingExpression(CheckBox.IsCheckedProperty);
            if (binding != null)
            {
                binding.UpdateSource();
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Checked -= AssociatedObject_Checked;
            AssociatedObject.Unchecked -= AssociatedObject_Checked;
        }
    }

    public class RadioButtonChangedBehavior : Behavior<RadioButton>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Checked += AssociatedObject_Checked;
            AssociatedObject.Unchecked += AssociatedObject_Checked;
        }

        void AssociatedObject_Checked(object sender, RoutedEventArgs e)
        {
            BindingExpression binding = AssociatedObject.GetBindingExpression(RadioButton.IsCheckedProperty);
            if (binding != null)
            {
                binding.UpdateSource();
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Checked -= AssociatedObject_Checked;
            AssociatedObject.Unchecked -= AssociatedObject_Checked;
        }
    }
}