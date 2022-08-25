using OpenRiaServices.DomainServices.Client;
using System;
using System.Windows;

namespace Virtuoso.Client.Core
{
    public static class ApplicationExtensions
    {
        public static bool IsRunningOutOfBrowserOrOpenSilver(this Application application)
        {
#if OPENSILVER
            return true;
#endif

            return application.IsRunningOutOfBrowser;
        }

        /// <summary>
        /// Gets server base URI from OpenRIA client factory.
        /// </summary>
        /// <param name="app">The application</param>
        /// <returns>Server base URI.</returns>
        public static Uri GetServerBaseUri(this Application app) => ((DomainClientFactory)DomainContext.DomainClientFactory).ServerBaseUri;
    }
}
