#region Usings

using System.Windows.Controls;
using System.Windows.Interactivity;
using Virtuoso.Client.Core;
using Virtuoso.Core.Navigation;

#endregion

namespace Virtuoso.Core.Behaviors
{
    public class CompositionNavigationBehavior : Behavior<Frame>
    {
        /// <summary>
        /// Flag to prevent double processing of the behavior.
        /// </summary>
        private bool processed;

        /// <summary>
        /// Initialize the behavior by importing the required objects.
        /// </summary>
        public CompositionNavigationBehavior()
        {   
        }

        /// <summary>
        /// Once the frame has been attached, start processing it.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            if (!processed)
            {
                RegisterNavigationService();
                processed = true;
            }
        }

        /// <summary>
        /// Create and register a navigation service.
        /// </summary>
        private void RegisterNavigationService()
        {
            VirtuosoContainer.Current.GetExport<INavigationService>().Value.SetFrame(AssociatedObject);
        }
    }
}