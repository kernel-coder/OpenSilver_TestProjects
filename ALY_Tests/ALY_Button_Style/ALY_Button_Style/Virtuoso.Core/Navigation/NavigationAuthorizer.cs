#region Usings

using System;
using System.Windows;

#endregion

namespace Virtuoso.Core.Navigation
{
    #region Using Directives

    #endregion

    /// <summary>
    ///   A default authorizer for the AuthContentLoader that mimics the behavior of ASP.NET's web.config authorization markup.
    /// </summary>
    public class NavigationAuthorizer : DependencyObject, INavigationAuthorizer
    {
        #region INavigationAuthorizer Members

        /// <summary>
        ///   Checks whether the principal has sufficient authorization to access the Uri being loaded by the AuthContentLoader.
        ///   If the principal is authorized, this method should simply return.  Otherwise, it should throw.
        /// </summary>
        /// <param name = "principal">The user credentials against which to check.</param>
        /// <param name = "targetUri">The Uri being loaded.</param>
        /// <param name = "currentUri">The current Uri from which the new Uri is being loaded.</param>
        //public void CheckAuthorization(IPrincipal principal, Uri targetUri, Uri currentUri)
        public void CheckAuthorization(Uri targetUri, Uri currentUri)
        {
            //foreach (var rule in this.Rules.Where(rule => rule.Matches(targetUri)))
            //{
            //    rule.Check(principal);
            //    return;
            //}
            //this.Check(principal);
            Check();
        }

        /// <summary>
        ///   Checks the principal against the parts of the rule and throws if the principal is unauthorized.
        /// </summary>
        /// <param name = "principal">The principal whose credentials are being checked.</param>
        //private void Check(IPrincipal principal)
        private void Check()
        {
            bool authenticated = Virtuoso.Services.Authentication.WebContext.Current.User.IsAuthenticated;

            //if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)  //principal.Identity.IsAuthenticated is always NULL...
            if (!authenticated)
            {
                throw new UnauthorizedAccessException("Cannot access because principle is not authenticated");
            }

            //if (this.Parts == null || this.Parts.Any() == false)
            //    return;
            //foreach (var rule in this.Parts)
            //{
            //    if (rule.IsAllowed(principal))
            //        return;
            //    if (rule.IsDenied(principal))
            //        throw new UnauthorizedAccessException(string.Format(ErrorStringPattern, this.UriPattern));
            //}
            //throw new UnauthorizedAccessException(string.Format(ErrorStringPattern, this.UriPattern));
        }

        #endregion
    }
}