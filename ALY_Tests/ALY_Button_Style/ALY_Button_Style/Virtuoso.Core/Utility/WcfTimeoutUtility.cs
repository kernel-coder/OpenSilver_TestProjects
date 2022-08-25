#region Usings

using System;
using System.Reflection;
using System.ServiceModel;
using OpenRiaServices.DomainServices.Client;

#endregion

namespace Virtuoso.Core.Utility
{
    //http://blogs.msdn.com/b/kylemc/archive/2010/11/03/how-to-change-the-request-timeout-for-wcf-ria-services.aspx
    public static class WcfTimeoutUtility
    {
        public static void ChangeWcfSendTimeout(DomainContext context,
            TimeSpan sendTimeout)
        {
#if !OPENSILVER
            PropertyInfo channelFactoryProperty = context.DomainClient.GetType().GetProperty("ChannelFactory");

            //NOTE: when mocking the DomainClient for unit testing - may not have a ChannelFactory property

            ChannelFactory factory = (ChannelFactory)channelFactoryProperty?.GetValue(context.DomainClient, null);
            if (factory?.Endpoint.Binding != null)
            {
                factory.Endpoint.Binding.SendTimeout = sendTimeout;
            }
#endif
        }
    }
}