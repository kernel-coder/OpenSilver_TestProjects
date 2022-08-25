namespace Virtuoso.Core.Behaviors
{
   using Microsoft.Expression.Interactivity.Core;
   using System.Windows;
   using System.Windows.Interactivity;

   /// <summary>
   /// Represents temporary workaround to avoid TransitionEffect exceptions in OpenSilver.
   /// To take effect you must place the behavior AFTER the xaml code that creates TransitionEffect.
   /// </summary>
   public class TransitionEffectBehavior : Behavior<FrameworkElement>
   {
      protected override void OnAttached()
      {
         base.OnAttached();

#if OPENSILVER
         RemoveTransitionEffects();
#endif
      }

      private void RemoveTransitionEffects()
      {
         foreach(VisualStateGroup group in VisualStateManager.GetVisualStateGroups(AssociatedObject))
         {
            foreach(VisualTransition transition in group.Transitions)
            {
               ExtendedVisualStateManager.SetTransitionEffect(transition, null);
            }
         }
      }
   }
}
