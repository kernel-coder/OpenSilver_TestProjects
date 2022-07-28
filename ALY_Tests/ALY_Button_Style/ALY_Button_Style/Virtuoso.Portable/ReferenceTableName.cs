using System;
using System.Linq;
using System.Reflection;

namespace Virtuoso.Portable
{
    public class ReferenceTableName //type-safe string enum pattern...
    {
        public static ReferenceTableName Create(string value)
        {
            var _value = ReferenceTableName.GetReferenceTableName(value);
            if (_value != null)
                return new ReferenceTableName(_value);
            else
                throw new ArgumentOutOfRangeException(value);
        }

        public string Value { get; internal set; }

        public ReferenceTableName(string value)
        {
            var _value = GetReferenceTableName(value);
            if (_value != null)
                Value = _value;
            else
                throw new ArgumentOutOfRangeException(value);
        }

        public static bool Exists(string value)
        {
            Type type = typeof(ReferenceTableName);
            var fields = from field in type.GetFields() select field;
            var ret = fields.Any(p => p.Name.ToLower().Equals(value.ToLower()));
            return ret;
        }

        public static string GetReferenceTableName(string value)
        {
            Type type = typeof(ReferenceTableName);
            var fields = from field in type.GetFields() select field;
            var fieldInfo = fields
                .Where(p => p.Name.ToLower().Equals(value.ToLower()))
                .FirstOrDefault();
            if (fieldInfo != null)
                return fieldInfo.Name;
            else
                return null;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(this.Value))
                return string.Empty; //base.ToString();
            else
                return this.Value;
        }
        //Need to be const instead of static readonly to use in MEF attributes...

        public const string Unknown = "Unknown";
        public const string BereavementActivity = "BereavementActivity";
        public const string BereavementPlan = "BereavementPlan";
        public const string CacheConfiguration = "CacheConfiguration";
        public const string CodeLookup = "CodeLookup";
        public const string ComfortPack = "ComfortPack";
        public const string CensusTract = "CensusTract";
        public const string Delta = "Delta";
        public const string TenantSetting = "TenantSetting";
        public const string Discipline = "Discipline";
        public const string Equipment = "Equipment";
        public const string ServiceType = "ServiceType";
        public const string Supply = "Supply";
        public const string Physician = "Physician";
        public const string Facility = "Facility";
        public const string FacilityBranch = "FacilityBranch";
        public const string Insurance = "Insurance";
        public const string ReferralSource = "ReferralSource";
        public const string Goal = "Goal";
        public const string Allergy = "Allergy";
        public const string MediSpanMedication = "MediSpanMedication";
        public const string User = "User";
        public const string Form = "Form";
        public const string Oasis = "Oasis";
        public const string FunctionalDeficit = "FunctionalDeficit";
        public const string Role = "Role";
        public const string SystemExceptionsAndAlerts = "SystemExceptionsAndAlerts";
        public const string ServiceLine = "ServiceLine";
        public const string AddressMapping = "AddressMapping";
        public const string NonServiceType = "NonServiceType";
        public const string Vendor = "Vendor";
        public const string OasisHeader = "OasisHeader";
        public const string ErrorDetail = "ErrorDetail";
        public const string Employer = "Employer";
        public const string ICDCM9 = "ICDCM9";
        public const string ICDCM10 = "ICDCM10";
        public const string ICDPCS9 = "ICDPCS9";
        public const string ICDPCS10 = "ICDPCS10";
        public const string ICDGEMS9 = "ICDGEMS9";
        public const string ICDGEMS10 = "ICDGEMS10";
        public const string ICDCategory = "ICDCategory";
        public const string HighRiskMedication = "HighRiskMedication";
        public const string GuardArea = "GuardArea";
        public const string TrackingGroup = "TrackingGroup";
        public const string CMSForm = "CMSForm";
        public const string RuleDefinition = "RuleDefinition";
        //public const string PerformanceMonitor = "PerformanceMonitor";
        public const string InsuranceGroup = "InsuranceGroup";
        public const string WoundLocation = "WoundLocation";
    }
}
