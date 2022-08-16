using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public interface IWoundDataProvider
    {
        bool IsTypeBurn(AdmissionWoundSite admissionWoundSite);
        bool IsTypePressureUlcer(AdmissionWoundSite admissionWoundSite);
        bool IsTypeSurgicalWound(AdmissionWoundSite admissionWoundSite);
        bool IsTypeStasisUlcer(AdmissionWoundSite admissionWoundSite);
        bool IsObservable(AdmissionWoundSite admissionWoundSite);
    }
}
