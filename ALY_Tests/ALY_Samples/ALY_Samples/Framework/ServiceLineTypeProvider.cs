#region Usings

using Virtuoso.Core.Cache;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Framework
{
    public class ServiceLineTypeProvider : IServiceLineTypeProvider
    {
        private int _getServiceLineTypeFromServiceLineKey(int? key)
        {
            Server.Data.ServiceLine SL = ServiceLineCache.GetServiceLineFromKey(key);
            if (SL == null)
            {
                return 0;
            }

            return SL.ServiceLineType;
        }

        int IServiceLineTypeProvider.GetServiceLineTypeFromServiceLineKey(int? key)
        {
            return _getServiceLineTypeFromServiceLineKey(key);
        }

        int IServiceLineTypeProvider.GetServiceLineTypeFromAdmission(Server.Data.Admission admission)
        {
            return admission.ServiceLineType;
        }
    }
}