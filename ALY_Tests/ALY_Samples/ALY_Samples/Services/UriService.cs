#region Usings

using System;
using System.ComponentModel.Composition;
using Virtuoso.Client.Core;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IUriService
    {
        Uri Uri { get; }
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IUriService))]
    public class UriService : IUriService
    {
        public UriService()
        {
            //NOTE: must set this in constructor.  For use by objects created on UI thread, but Uri property later accessed from a background thread...
            _Uri = System.Windows.Application.Current.GetServerBaseUri();
        }

        private Uri _Uri;
        public Uri Uri => _Uri;
    }
}