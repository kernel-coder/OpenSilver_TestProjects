#region Usings

using System.Windows;
using System.Windows.Interactivity;

#endregion

namespace Virtuoso.Core.Behaviors
{
    public class RightMouseClickDisableBehavior : Behavior<UIElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.MouseRightButtonDown += (s, e) =>
            {
#if !DEBUG
            e.Handled = true;
#endif
            };
        }
    }
}