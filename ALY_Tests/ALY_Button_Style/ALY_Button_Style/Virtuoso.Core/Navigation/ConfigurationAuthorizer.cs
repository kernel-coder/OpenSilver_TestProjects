#region Usings

using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows;
using Virtuoso.Client.Core;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;

#endregion

namespace Virtuoso.Core.Navigation
{
    #region Using Directives

    #endregion

    public class ConfigurationAuthorizer : DependencyObject, IConfigurationAuthorizer
    {
        public VirtuosoApplicationConfiguration Configuration { get; set; }

        #region INavigationAuthorizer Members

        public void CheckConfiguration()
        {
            Check();
        }

        private void Check()
        {
            if (Configuration == null)
            {
                if (!DesignerProperties.IsInDesignTool)
                {
                    Configuration = VirtuosoContainer.Current.GetExport<VirtuosoApplicationConfiguration>().Value;
                }
            }

            bool initialized = Configuration.ApplicationInitialized;

            if (!initialized)
            {
                throw new ConfigurationException("Cannot access because application has not been initialized");
            }
        }

        #endregion
    }
}