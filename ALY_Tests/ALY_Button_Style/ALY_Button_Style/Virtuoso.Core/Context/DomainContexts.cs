#region Usings

using System;
using Virtuoso.Core.Utility;

#endregion

namespace Virtuoso.Services.Web
{
    public partial class VirtuosoDomainContext
    {
        partial void OnCreated()
        {
            TimeSpan _timespan = new TimeSpan(0, 15, 0); //hours, minutes, seconds
            WcfTimeoutUtility.ChangeWcfSendTimeout(this, _timespan);
        }

        //Cannot get RIA to code generate this - so manually adding it to the Context.  Need this defined for serialization
        public global::OpenRiaServices.DomainServices.Client.EntitySet<Server.Data.ApplicationSetting>
            ApplicationSettings => EntityContainer.GetEntitySet<Server.Data.ApplicationSetting>();
    }
}

namespace Virtuoso.Services.Authentication
{
    public partial class AuthenticationContext
    {
        partial void OnCreated()
        {
            TimeSpan _timespan = new TimeSpan(0, 15, 0);
            WcfTimeoutUtility.ChangeWcfSendTimeout(this, _timespan);
        }
    }
}

namespace Virtuoso.Services
{
    public partial class PingContext
    {
        partial void OnCreated()
        {
            TimeSpan _timespan = new TimeSpan(0, 15, 0);
            WcfTimeoutUtility.ChangeWcfSendTimeout(this, _timespan);
        }
    }
}

namespace Virtuoso.Services.Configuration
{
    public partial class ReferenceContext
    {
        partial void OnCreated()
        {
            TimeSpan _timespan = new TimeSpan(0, 15, 0);
            WcfTimeoutUtility.ChangeWcfSendTimeout(this, _timespan);
        }
    }
}

namespace Virtuoso.Services
{
    public partial class EmailContext
    {
        partial void OnCreated()
        {
            TimeSpan _timespan = new TimeSpan(0, 15, 0);
            WcfTimeoutUtility.ChangeWcfSendTimeout(this, _timespan);
        }
    }
}