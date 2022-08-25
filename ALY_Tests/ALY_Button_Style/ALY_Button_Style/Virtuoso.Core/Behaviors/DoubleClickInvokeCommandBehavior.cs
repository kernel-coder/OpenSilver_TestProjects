#region Usings

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

#endregion

namespace Virtuoso.Core.Behaviors
{
    public class DoubleClickInvokeCommandBehavior : Behavior<UIElement>
    {
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(DoubleClickInvokeCommandBehavior),
                new PropertyMetadata(null));

        public Object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(Object), typeof(DoubleClickInvokeCommandBehavior),
                new PropertyMetadata(null));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += OnClick;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= OnClick;
        }

        void OnClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
            {
                return;
            }

            if (Command == null || !Command.CanExecute(CommandParameter))
            {
                return;
            }

            Command.Execute(CommandParameter);
        }
    }
}