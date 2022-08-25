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
    ///   An INavigationContentLoader that throws an UnauthorizedAccessException if the user does not meet the requirements
    ///   specified for the page they are trying to navigate to.
    /// </summary>
    [ContentProperty("Authorizer")]
    public class PreInitContentLoader : ContentLoaderBase
    {
        /// <summary>
        ///   The Authorizer that will be used to authorize the Principal.
        /// </summary>
        public static readonly DependencyProperty AuthorizerProperty =
            DependencyProperty.Register("Authorizer",
                typeof(IConfigurationAuthorizer),
                typeof(PreInitContentLoader),
                new PropertyMetadata(null));

        /// <summary>
        ///   The INavigationContentLoader being wrapped by the AuthContentLoader.
        /// </summary>
        public static readonly DependencyProperty ContentLoaderProperty =
            DependencyProperty.Register("ContentLoader",
                typeof(INavigationContentLoader),
                typeof(PreInitContentLoader),
                new PropertyMetadata(null));

        private static readonly INavigationContentLoader DefaultLoader = new PageResourceContentLoader();

        /// <summary>
        ///   The Authorizer that will be used to authorize the Principal.
        /// </summary>
        public IConfigurationAuthorizer Authorizer
        {
            get { return (IConfigurationAuthorizer)GetValue(AuthorizerProperty); }
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
            return new PreInitLoader(this);
        }

        private class PreInitLoader : LoaderBase
        {
            #region Fields (3)

            private INavigationContentLoader _Loader;
            private readonly PreInitContentLoader _Parent;
            private IAsyncResult _Result;

            #endregion Fields

            #region Constructors (1)

            public PreInitLoader(PreInitContentLoader parent)
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

                if (_Parent.Authorizer != null)
                {
                    try
                    {
                        _Parent.Authorizer.CheckConfiguration();
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