#region Usings

using System;
using System.Collections.Generic;
using System.Windows;

#endregion

namespace Virtuoso.Core.Navigation
{
    public interface INonLinearNavigationActivePages
    {
        IDictionary<String, DependencyObject> Pages { get; }
        String GetCurrentSource(String parentUriOriginalString);
        Object GetPage(String targetUriOriginalString);

        void RekeyPage(string currentURIString, string newURIString);
        void RemovePage(string uriString);

        /// <summary>
        /// Gets the count for applicaiton suite for Chainable active views.
        /// </summary>
        /// <param name="applicationSuite">The application suite.</param>
        /// <returns>Count of Chainable active views</returns>
        Int32 GetActiveCountForApplicaitonSuite(String applicationSuite);

        /// <summary>
        /// Gets the active count for application view for Chainable active views.
        /// </summary>
        /// <param name="view">The view type.</param>
        /// <returns>Count of Chainable active views</returns>
        Int32 GetActiveCountForView(Type view);
    }
}