#region Usings

using System.ComponentModel.Composition;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Client.Cache
{
    public interface IAddressMapFactory
    {
        ICache GetStrategyImpl(Virtuoso.Core.Services.ILogger logManager);
    }

    [Export(typeof(IAddressMapFactory))]
    public class AddressMapFactory : IAddressMapFactory
    {
        public ICache GetStrategyImpl(Virtuoso.Core.Services.ILogger logManager)
        {
            return new AddressMapFlatFileStrategyV2(logManager);
        }
    }
}