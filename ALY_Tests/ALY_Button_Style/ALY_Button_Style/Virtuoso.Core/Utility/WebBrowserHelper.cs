#region Usings

using System;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Controls;

#endregion

namespace Virtuoso.Core.Utility
{
    public class WebBrowserHelper
    {
        public static void Show(string appPath, string queryString)
        {
            var useEmbeddedWebBrowser = TenantSettingsCache.Current.TenantSetting.UseEmbeddedWebBrowser;
            if (System.Windows.Application.Current.Host.Source != null)
            {
                var uriBuilder = new UriBuilder(System.Windows.Application.Current.Host.Source)
                {
                    Path = appPath,
                    Query = queryString
                };
                if (useEmbeddedWebBrowser)
                {
                    new WebBrowserWindow(uriBuilder.Uri).Show();
                }
                else
                {
                    WebBrowserOpener.OpenURL(uriBuilder.Uri);
                }
            }
        }

        public static void Show(string navigateString, UriKind uriKind = UriKind.Absolute)
        {
            var useEmbeddedWebBrowser = TenantSettingsCache.Current.TenantSetting.UseEmbeddedWebBrowser;
            if (useEmbeddedWebBrowser)
            {
                new WebBrowserWindow(new Uri(navigateString, uriKind)).Show();
            }
            else
            {
                WebBrowserOpener.OpenURL(navigateString);
            }
        }

        public static void Show(Uri navigateURI)
        {
            var useEmbeddedWebBrowser = TenantSettingsCache.Current.TenantSetting.UseEmbeddedWebBrowser;
            if (useEmbeddedWebBrowser)
            {
                new WebBrowserWindow(navigateURI).Show();
            }
            else
            {
                WebBrowserOpener.OpenURL(navigateURI);
            }
        }
    }

    public class HyperlinkButtonWrapper : System.Windows.Controls.HyperlinkButton
    {
        public void OpenURL(string navigateUri)
        {
            OpenURL(new Uri(navigateUri, UriKind.Absolute));
        }

        public void OpenURL(Uri navigateUri)
        {
            NavigateUri = navigateUri;
            TargetName = "_blank";
            base.OnClick();
        }
    }

    public class WebBrowserOpener
    {
        public static void OpenURL(string navigateUri)
        {
            HyperlinkButtonWrapper hlbw = new HyperlinkButtonWrapper();
            hlbw.OpenURL(navigateUri);
        }

        public static void OpenURL(Uri navigateUri)
        {
            HyperlinkButtonWrapper hlbw = new HyperlinkButtonWrapper();
            hlbw.OpenURL(navigateUri);
        }
    }
}