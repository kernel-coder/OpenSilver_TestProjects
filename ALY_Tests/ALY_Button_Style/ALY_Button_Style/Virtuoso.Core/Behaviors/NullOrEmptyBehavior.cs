#region Usings

using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

#endregion

namespace Virtuoso.Core.Behaviors
{
    public class NullOrEmptyBehavior : Behavior<TextBox>
    {
        public static readonly DependencyProperty ReplaceValueProperty =
            DependencyProperty.Register("ReplaceValue", typeof(string), typeof(NullOrEmptyBehavior),
                new PropertyMetadata(string.Empty));

        public string ReplaceValue
        {
            get { return (string)GetValue(ReplaceValueProperty); }
            set { SetValue(ReplaceValueProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.LostFocus += TextBox_LostFocus;
        }

        protected override void OnDetaching()
        {
            base.OnAttached();
            AssociatedObject.LostFocus -= TextBox_LostFocus;
        }

        void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = ReplaceValue;
            }
        }
    }
}