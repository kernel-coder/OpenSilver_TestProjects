#region Usings

using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;

#endregion

namespace Virtuoso.Core.Navigation
{
    public class NonLinearNavigationActivePages : INonLinearNavigationActivePages
    {
        LogWriter LogWriter { get; set; }

        #region Declarations

        IDictionary<String, DependencyObject> _pages = new Dictionary<String, DependencyObject>();

        #endregion Declarations

        #region Properties

        public IDictionary<string, DependencyObject> Pages => _pages;

        #endregion Properties

        #region Constructor

        public NonLinearNavigationActivePages()
        {
            LogWriter = EnterpriseLibraryContainer.Current.GetInstance<LogWriter>();
        }

        #endregion Constructor

        #region Methods

        public void RekeyPage(string currentURIString, string newURIString)
        {
            if (Pages.ContainsKey(currentURIString))
            {
                var content = Pages[currentURIString];

                Log(string.Format("[RekeyPage] Removing from Pages: currentURIString: {0}", currentURIString));

                Pages.Remove(currentURIString); //TODO: shouldn't the 'content' of this Pages[uri] be 'cleaned' up?

                Log(string.Format("[RekeyPage] Adding to Pages: newURIString: {0}", newURIString));

                if (Pages.ContainsKey(newURIString) == false)
                {
                    Pages.Add(newURIString, content);
                }
            }
        }

        private void Log(string msg)
        {
            LogWriter.Write(
                msg,
                new[] { GetType().ToString() }, //category
                0, //priority
                0, //eventid
                TraceEventType.Information);
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                "----------------------------------------------------------------------------------------------------------------------------");
            System.Diagnostics.Debug.WriteLine(msg);
            System.Diagnostics.Debug.WriteLine(
                "----------------------------------------------------------------------------------------------------------------------------");
#endif
        }

        public void RemovePage(string uriString)
        {
            if (Pages.ContainsKey(uriString) == false)
            {
                return;
            }

            var target = Pages[uriString];
            var pb = target as View.PageBase;
            if (pb != null)
            {
                bool OKToRemove = pb.Cleanup();
                if (OKToRemove)
                {
                    EnterpriseLibraryContainer.Current.GetInstance<LogWriter>().Write(
                        string.Format("[RemovePage] Removing from Pages: uriString: {0}", uriString),
                        new[] { GetType().ToString() }, //category
                        0, //priority
                        0, //eventid
                        TraceEventType.Information);

                    Pages.Remove(uriString);
                    pb = null;
                }
            }
            else
            {
                Pages.Remove(uriString); //remove this page from the navigation cache
            }
        }

        public String GetCurrentSource(String parentUriOriginalString)
        {
            if (Pages.ContainsKey(parentUriOriginalString))
            {
                var nk =
                    Pages[parentUriOriginalString]
                        .GetValue(NonLinearNavigationContentLoader.NavigateKeyProperty) as NavigateKey;
                if (nk != null)
                {
                    return nk.CurrentSource;
                }
            }

            return String.Empty;
        }

        public Object GetPage(String targetUriOriginalString)
        {
            if (Pages.ContainsKey(targetUriOriginalString))
            {
                return GetLastChainedView(targetUriOriginalString, targetUriOriginalString);
            }

            return null;
        }

        public Int32 GetActiveCountForApplicaitonSuite(String applicationSuite)
        {
            Int32 count = 0;
            foreach (var item in Pages.Values)
            {
                var nk = item.GetValue(NonLinearNavigationContentLoader.NavigateKeyProperty) as NavigateKey;
                if (nk != null && nk.IsChainable && nk.ApplicationSuite != null &&
                    nk.ApplicationSuite.Equals(applicationSuite))
                {
                    count += 1;
                }
            }

            return count;
        }

        public Int32 GetActiveCountForView(Type view)
        {
            Int32 count = 0;
            foreach (var item in Pages.Values)
            {
                var nk = item.GetValue(NonLinearNavigationContentLoader.NavigateKeyProperty) as NavigateKey;
                if (nk != null && nk.ViewType == view)
                {
                    count += 1;
                }
            }

            return count;
        }

        Object GetLastChainedView(String targetUriOriginalString, String baseOriginalString)
        {
            var target = Pages[targetUriOriginalString];
            var nk = target.GetValue(NonLinearNavigationContentLoader.NavigateKeyProperty) as NavigateKey;
            if (nk != null)
            {
                if (nk.IsChainable)
                {
                    foreach (var contentPage in Pages.Values)
                    {
                        var contentNavigateKey =
                            contentPage.GetValue(NonLinearNavigationContentLoader.NavigateKeyProperty) as NavigateKey;
                        if (contentNavigateKey != null &&
                            contentNavigateKey.ParentUriOriginalString.Equals(nk.UriString))
                        {
                            return GetLastChainedView(contentNavigateKey.UriString, baseOriginalString);
                        }
                    }
                }

                if (targetUriOriginalString.Equals(baseOriginalString))
                {
                    return target;
                }

                try
                {
                    return new Uri(nk.CurrentSource, UriKind.Absolute);
                }
                catch (Exception)
                {
                    return new Uri(nk.CurrentSource, UriKind.Relative);
                }
            }

            return target;
        }

        #endregion Methods
    }
}