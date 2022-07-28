#region Usings

using Virtuoso.Core.Cache;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Framework
{
    public class PatientContactDataProvider : IPatientContactDataProvider
    {
        string IPatientContactDataProvider.PatientContact1KeyLabel(int? key)
        {
            string _AdvancedDirectiveTypeCode = CodeLookupCache.GetCodeFromKey(key);
            if (string.IsNullOrWhiteSpace(_AdvancedDirectiveTypeCode))
            {
                return null;
            }

            if (_AdvancedDirectiveTypeCode.ToLower() == "healthcareproxy")
            {
                return "Health Care Proxy";
            }

            if (_AdvancedDirectiveTypeCode.ToLower() == "healthcarepoa")
            {
                return "Health Care Agent";
            }

            return null;
        }

        string IPatientContactDataProvider.PatientContact2KeyLabel(int? key)
        {
            string _AdvancedDirectiveTypeCode = CodeLookupCache.GetCodeFromKey(key);
            if (string.IsNullOrWhiteSpace(_AdvancedDirectiveTypeCode))
            {
                return null;
            }

            if (_AdvancedDirectiveTypeCode.ToLower() == "healthcareproxy")
            {
                return "Alternative Health Care Proxy";
            }

            if (_AdvancedDirectiveTypeCode.ToLower() == "healthcarepoa")
            {
                return "Alternative Health Care Agent";
            }

            return null;
        }

        string IPatientContactDataProvider.PatientContact3KeyLabel(int? key)
        {
            string _AdvancedDirectiveTypeCode = CodeLookupCache.GetCodeFromKey(key);
            if (string.IsNullOrWhiteSpace(_AdvancedDirectiveTypeCode))
            {
                return null;
            }

            if (_AdvancedDirectiveTypeCode.ToLower() == "healthcareproxy")
            {
                return "Second Alternative Health Care Proxy";
            }

            if (_AdvancedDirectiveTypeCode.ToLower() == "healthcarepoa")
            {
                return "Second Alternative Health Care Agent";
            }

            return null;
        }
    }
}