#region Usings

using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using SLaB.Navigation.ContentLoaders.Utilities;

#endregion

namespace Virtuoso.Core.Navigation
{
    /// <summary>
    ///   INavigationAuthorizor is used by the AuthContentLoader to check to see whether navigation should be allowed based upon
    ///   user credentials.
    /// </summary>
    public interface INavigationAuthorizer
    {
        /// <summary>
        ///   Checks whether the principal has sufficient authorization to access the Uri being loaded by the AuthContentLoader.
        ///   If the principal is authorized, this method should simply return.  Otherwise, it should throw.
        /// </summary>
        /// <param name = "targetUri">The Uri being loaded.</param>
        /// <param name = "currentUri">The current Uri from which the new Uri is being loaded.</param>
        //void CheckAuthorization(IPrincipal principal, Uri targetUri, Uri currentUri);
        void CheckAuthorization(Uri targetUri, Uri currentUri);
    }

    /// <summary>
    ///   An INavigationContentLoader that throws an UnauthorizedAccessException if the user does not meet the requirements
    ///   specified for the page they are trying to navigate to.
    /// </summary>
    [ContentProperty("Authorizer")]
    public class AuthContentLoader : ContentLoaderBase
    {
        /// <summary>
        ///   The Authorizer that will be used to authorize the Principal.
        /// </summary>
        public static readonly DependencyProperty AuthorizerProperty =
            DependencyProperty.Register("Authorizer",
                typeof(INavigationAuthorizer),
                typeof(AuthContentLoader),
                new PropertyMetadata(null));

        /// <summary>
        ///   The INavigationContentLoader being wrapped by the AuthContentLoader.
        /// </summary>
        public static readonly DependencyProperty ContentLoaderProperty =
            DependencyProperty.Register("ContentLoader",
                typeof(INavigationContentLoader),
                typeof(AuthContentLoader),
                new PropertyMetadata(null));

        private static readonly INavigationContentLoader DefaultLoader = new PageResourceContentLoader();

        /// <summary>
        ///   The Authorizer that will be used to authorize the Principal.
        /// </summary>
        public INavigationAuthorizer Authorizer
        {
            get { return (INavigationAuthorizer)GetValue(AuthorizerProperty); }
            set { SetValue(AuthorizerProperty, value); }
        }

        /// <summary>
        ///   The INavigationContentLoader being wrapped by the AuthContentLoader.
        /// </summary>
        public INavigationContentLoader ContentLoader
        {
            get { return (INavigationContentLoader)GetValue(ContentLoaderProperty); }
            set { SetValue(ContentLoaderProperty, value); }
        }

        /// <summary>
        ///   Gets a value that indicates whether the specified URI can be loaded.
        /// </summary>
        /// <param name = "targetUri">The URI to test.</param>
        /// <param name = "currentUri">The URI that is currently loaded.</param>
        /// <returns>true if the URI can be loaded; otherwise, false.</returns>
        public override bool CanLoad(Uri targetUri, Uri currentUri)
        {
            return (ContentLoader ?? DefaultLoader).CanLoad(targetUri, currentUri);
        }

        /// <summary>
        ///   Creates an instance of a LoaderBase that will be used to handle loading.
        /// </summary>
        /// <returns>An instance of a LoaderBase.</returns>
        protected override LoaderBase CreateLoader()
        {
            return new AuthLoader(this);
        }

        private class AuthLoader : LoaderBase
        {
            #region Fields (3)

            private INavigationContentLoader _Loader;
            private readonly AuthContentLoader _Parent;
            private IAsyncResult _Result;

            #endregion Fields

            #region Constructors (1)

            public AuthLoader(AuthContentLoader parent)
            {
                _Parent = parent;
            }

            #endregion Constructors

            #region Methods (2)

            // Public Methods (2) 

            public override void Cancel()
            {
                _Loader.CancelLoad(_Result);
            }

            public override void Load(Uri targetUri, Uri currentUri)
            {
                _Loader = _Parent.ContentLoader ?? DefaultLoader;

                bool is_logout = targetUri.OriginalString.EndsWith("Logout.xaml");

                if (
                    (_Parent.Authorizer != null) &&
                    (is_logout == false) //not authorizing the Logout page...
                )
                {
                    try
                    {
                        //this._Parent.Authorizer.CheckAuthorization(this._Parent.Principal, targetUri, currentUri);
                        _Parent.Authorizer.CheckAuthorization(targetUri, currentUri);
                    }
                    catch (Exception e)
                    {
                        Error(e);
                        return;
                    }
                }

                try
                {
                    _Result = _Loader.BeginLoad(targetUri,
                        currentUri,
                        res =>
                        {
                            try
                            {
                                var loadResult = _Loader.EndLoad(res);
                                if (loadResult.RedirectUri != null)
                                {
                                    Complete(loadResult.RedirectUri);
                                }
                                else
                                {
                                    Complete(loadResult.LoadedContent);
                                }
                            }
                            catch (Exception e)
                            {
                                Error(e);
                            }
                        },
                        null);
                }
                catch (Exception e)
                {
                    Error(e);
                }
            }

            #endregion Methods
        }
    }
}