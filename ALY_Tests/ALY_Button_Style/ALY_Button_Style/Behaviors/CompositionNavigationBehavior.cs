#region Usings

using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Windows.Controls;
using System.Windows.Interactivity;
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
        /// The container that will allow instance registration.
        /// </summary>
        [Import]
        public CompositionContainer Container { get; set; }

        /// <summary>
        /// Initialize the behavior by importing the required objects.
        /// </summary>
        public CompositionNavigationBehavior()
        {
            if (!DesignerProperties.IsInDesignTool)
            {
                CompositionInitializer.SatisfyImports(this);
            }
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
            // Wrap the frame in a navigation service.
            var frame = AssociatedObject;
            var svc = new Navigation.NavigationService(frame);

            // Register the navigation service as a singleton instance.
            Container.ComposeExportedValue<INavigationService>(svc);
        }
    }
}