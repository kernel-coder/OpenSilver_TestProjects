#region Usings

using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Framework
{
    public class WoundDataProvider : IWoundDataProvider
    {
        bool IWoundDataProvider.IsTypeBurn(AdmissionWoundSite admissionWoundSite)
        {
            var _TypeCode = admissionWoundSite.WoundTypeCode;
            return (_TypeCode == null) ? false : _TypeCode.ToLower().Equals("burn");
        }

        bool IWoundDataProvider.IsTypePressureUlcer(AdmissionWoundSite admissionWoundSite)
        {
            var _TypeCode = admissionWoundSite.WoundTypeCode;
            return (_TypeCode == null) ? false : _TypeCode.ToLower().Equals("pressureulcer");
        }

        bool IWoundDataProvider.IsTypeSurgicalWound(AdmissionWoundSite admissionWoundSite)
        {
            var _TypeCode = admissionWoundSite.WoundTypeCode;
            return (_TypeCode == null) ? false : _TypeCode.ToLower().Equals("surgicalwound");
        }

        bool IWoundDataProvider.IsTypeStasisUlcer(AdmissionWoundSite admissionWoundSite)
        {
            var _TypeCode = admissionWoundSite.WoundTypeCode;
            return (_TypeCode == null)
                ? false
                : (_TypeCode.ToLower().Equals("venousulcer") || _TypeCode.ToLower().Equals("arterialulcer"));
        }

        bool IWoundDataProvider.IsObservable(AdmissionWoundSite admissionWoundSite)
        {
            var _WoundStatusCode = CodeLookupCache.GetCodeFromKey(admissionWoundSite.WoundStatus);
            return (string.IsNullOrWhiteSpace(_WoundStatusCode)) ? true :
                (_WoundStatusCode.ToLower().Equals("observable")) ? true : false;
        }
    }
}