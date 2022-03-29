#region Usings

using System.Windows.Controls;
using System.Windows.Interactivity;

#endregion

namespace Virtuoso.Core.Behaviors
{
    public class SetFocusTrigger : TargetedTriggerAction<Control>
    {
        protected override void Invoke(object parameter)
        {
            if (Target == null)
            {
                return;
            }

            Target.Focus();
        }
    }
}