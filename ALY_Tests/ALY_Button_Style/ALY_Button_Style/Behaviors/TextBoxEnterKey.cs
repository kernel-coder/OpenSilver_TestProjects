#region Usings

using System.Windows.Input;
using System.Windows.Interactivity;
using Virtuoso.Core.Controls;

#endregion

namespace Virtuoso.Core.Behaviors
{
    public class TextBoxEnterButtonInvoke : TargetedTriggerAction<vButton>
    {
        protected override void Invoke(object parameter)
        {
            KeyEventArgs keyEventArgs = parameter as KeyEventArgs;
            if ((Target != null) && (Target.IsEnabled) && (keyEventArgs != null) && (keyEventArgs.Key == Key.Enter))
            {
                Target.vButtonOnClick();
            }
        }
    }
}