#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.Insurance)]
    [Export(typeof(ICache))]
    public class InsuranceCache : ReferenceCacheBase<Insurance>
    {
        public static InsuranceCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.Insurances;

        [ImportingConstructor]
        public InsuranceCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Insurance, "019")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("InsuranceCache already initialized.");
            }

            Current = this;
        }

        protected override EntityQuery<Insurance> GetEntityQuery()
        {
            return Context.GetInsuranceQuery();
        }

        public static List<Insurance> GetInsurances()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Insurances == null))
            {
                return null;
            }

            return Current.Context.Insurances.OrderBy(p => p.Name).ToList();
        }

        public static List<Insurance> GetActiveInsurances()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Insurances == null))
            {
                return null;
            }

            return Current.Context.Insurances.Where(p => p.Inactive == false).OrderBy(p => p.Name).ToList();
        }

        public static List<Insurance> GetInsurancesForInterface(string Interface)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Insurances == null))
            {
                return null;
            }

            var IPD_VendorKey = Current.Context.InsuranceParameterDefinitions
                .Where(w => w.ParameterType == "Eligibility" && w.Code == "Vendor")
                .Select(s => s.InsuranceParameterDefinitionKey)
                .FirstOrDefault();

            var IPD_EnabledKey = Current.Context.InsuranceParameterDefinitions
                .Where(w => w.ParameterType == "Eligibility" && w.Code == "Enabled")
                .Select(s => s.InsuranceParameterDefinitionKey)
                .FirstOrDefault();

            var EnabledInsuranceKeys = Current.Context.InsuranceParameters
                .Where(w => w.ParameterKey == IPD_EnabledKey && w.Value == "True")
                .Select(s => s.InsuranceKey)
                .ToList();

            if (IPD_VendorKey != 0) // If Agency has more than one vendor then limit to chosen vendor
            {
                EnabledInsuranceKeys = Current.Context.InsuranceParameters
                    .Where(w => w.ParameterKey == IPD_VendorKey && w.Value == Interface &&
                                EnabledInsuranceKeys.Contains(w.InsuranceKey))
                    .Select(s => s.InsuranceKey)
                    .ToList();
            }

            return Current.Context.Insurances
                .Where(p => p.Inactive == false && EnabledInsuranceKeys.Contains(p.InsuranceKey))
                .OrderBy(p => p.Name)
                .ToList();
        }

        public static List<Insurance> GetActiveInsurances(int? andMeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Insurances == null))
            {
                return null;
            }

            return Current.Context.Insurances.Where(a => (a.Inactive == false) || a.InsuranceKey == andMeKey)
                .OrderBy(a => a.Name).ToList();
        }

        public static Insurance GetInsuranceFromKey(int? insuranceKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Insurances == null))
            {
                return null;
            }

            if (insuranceKey == null)
            {
                return null;
            }

            Insurance i =
                (from c in Current.Context.Insurances.AsQueryable() where (c.InsuranceKey == insuranceKey) select c)
                .FirstOrDefault();
            if ((i == null) && (insuranceKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error InsuranceCache.GetInsuranceName: InsuranceKey {0} is not defined.  Contact your system administrator.",
                    insuranceKey.ToString()));
            }

            return i;
        }

        public static string GetInsuranceNameFromKey(int? insuranceKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Insurances == null))
            {
                return null;
            }

            if (insuranceKey == null)
            {
                return null;
            }

            Insurance i = GetInsuranceFromKey(insuranceKey);
            return i?.Name;
        }

        public static List<InsuranceCertDefinition> GetInsuranceCertDefs(int? insuranceKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Insurances == null))
            {
                return null;
            }

            if (insuranceKey == null)
            {
                return null;
            }

            List<InsuranceCertDefinition> ic = Current.Context.InsuranceCertDefinitions
                .Where(c => c.InsuranceKey == insuranceKey).ToList();
            if ((ic == null) && (insuranceKey != 0))
            {
                return null;
            }

            return ic;
        }

        public static string GetInsuranceCertStatement(int? insuranceKey, int periodNumber, DateTime periodStartDate)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Insurances == null) ||
                (Current.Context.InsuranceCertStatements == null))
            {
                return null;
            }

            if (insuranceKey == null)
            {
                return null;
            }

            if (periodNumber <= 1)
            {
                InsuranceCertStatement cs = Current.Context.InsuranceCertStatements
                    .Where(c => ((c.InsuranceKey == insuranceKey) && (c.InactiveDate == null) &&
                                 (periodStartDate >= c.EffectiveFromDate))).OrderByDescending(c => c.EffectiveFromDate)
                    .FirstOrDefault();
                if ((cs == null) || (string.IsNullOrWhiteSpace(cs.CertStatement)))
                {
                    return null;
                }

                return cs.CertStatement.Trim();
            }

            InsuranceRecertStatement rs = Current.Context.InsuranceRecertStatements
                .Where(c => ((c.InsuranceKey == insuranceKey) && (c.InactiveDate == null) &&
                             (periodStartDate >= c.EffectiveFromDate))).OrderByDescending(c => c.EffectiveFromDate)
                .FirstOrDefault();
            if ((rs == null) || (string.IsNullOrWhiteSpace(rs.RecertStatement)))
            {
                return null;
            }

            return rs.RecertStatement.Trim();
        }

        public static InsuranceParameterDefinition GetInsuranceParameterDefinitionForCode(string ParameterType,
            string Code)
        {
            Current?.EnsureCacheReady();
            var def = Current.Context.InsuranceParameterDefinitions.Where(ipd =>
                ipd.Code == Code && ipd.ParameterType == ParameterType);
            return (def == null) ? null : def.FirstOrDefault();
        }

        public static List<InsuranceParameter> GetInsuranceParametersForInsurance(int insuranceKey)
        {
            Current?.EnsureCacheReady();
            // Done this way so we can step through and test during development... could be written tighter.
            var result = new List<InsuranceParameter>();
            var addthese = Current.Context.InsuranceParameters.Where(ip => ip.InsuranceKey == insuranceKey).ToList();

            if (addthese != null)
            {
                foreach (var i in addthese) result.Add(i);
            }

            return result;
        }

        public static string GetInsuranceParameterValue(int insuranceKey, string ParameterType, string Code)
        {
            Current?.EnsureCacheReady();
            var x = GetInsuranceParametersForInsurance(insuranceKey).Where(w =>
                w.InsuranceParameterDefinition.ParameterType == ParameterType &&
                w.InsuranceParameterDefinition.Code == Code).FirstOrDefault();
            return (x != null) ? x.Value : null;
        }

        public static bool IsInsurancePDGM(int insuranceKey, DateTime? effectiveDate)
        {
            Current?.EnsureCacheReady();
            DateTime date = (effectiveDate == null)
                ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date
                : ((DateTime)effectiveDate).Date;
            List<InsurancePPSParameter> ippList = Current.Context.InsurancePPSParameters
                .Where(ip => ip.InsuranceKey == insuranceKey).ToList();
            if (ippList == null)
            {
                return false;
            }

            InsurancePPSParameter ipp = ippList
                .Where(i => ((i.ParameterName == "PPS Model Version") &&
                             ((i.StartDate == null) ||
                              ((i.StartDate != null) && (((DateTime)i.StartDate).Date <= date))) &&
                             ((i.EndDate == null) || ((i.EndDate != null) && (((DateTime)i.EndDate).Date >= date)))))
                .FirstOrDefault();
            if (ipp == null)
            {
                return false;
            }

            int version = 0;
            try
            {
                version = Int32.Parse(ipp.ParameterValue);
            }
            catch
            {
            }

            return (version >= 3);
        }

        public static string GetPPSModelVersion(int insuranceKey, DateTime? effectiveDate)
        {
            Current?.EnsureCacheReady();
            DateTime date = (effectiveDate == null)
                ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date
                : ((DateTime)effectiveDate).Date;
            List<InsurancePPSParameter> ippList = Current.Context.InsurancePPSParameters
                .Where(ip => ip.InsuranceKey == insuranceKey).ToList();
            if (ippList == null)
            {
                return null;
            }

            InsurancePPSParameter ipp = ippList
                .Where(i => ((i.ParameterName == "PPS Model Version") &&
                             ((i.StartDate == null) ||
                              ((i.StartDate != null) && (((DateTime)i.StartDate).Date <= date))) &&
                             ((i.EndDate == null) || ((i.EndDate != null) && (((DateTime)i.EndDate).Date >= date)))))
                .FirstOrDefault();
            if (ipp == null)
            {
                return null;
            }

            return (string.IsNullOrWhiteSpace(ipp.ParameterValue)) ? null : ipp.ParameterValue.Trim();
        }

        public static bool IsInsurancePDGMHIPPS(int insuranceKey, DateTime? effectiveDate)
        {
            Current?.EnsureCacheReady();
            DateTime date = (effectiveDate == null)
                ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date
                : ((DateTime)effectiveDate).Date;
            List<InsurancePPSParameter> ippList = Current.Context.InsurancePPSParameters
                .Where(ip => ip.InsuranceKey == insuranceKey).ToList();
            if (ippList == null)
            {
                return false;
            }

            InsurancePPSParameter ipp = ippList
                .Where(i => ((i.ParameterName == "PDGMHIPPS") &&
                             ((i.StartDate == null) ||
                              ((i.StartDate != null) && (((DateTime)i.StartDate).Date <= date))) &&
                             ((i.EndDate == null) || ((i.EndDate != null) && (((DateTime)i.EndDate).Date >= date)))))
                .FirstOrDefault();
            if ((ipp == null) || (string.IsNullOrWhiteSpace(ipp.ParameterValue)))
            {
                return false;
            }

            return (ipp.ParameterValue.Trim().ToUpper().StartsWith("Y")) ? true : false;
        }

        public static List<InsurancePPSParameter> GetActiveInsurancePDGMList(DateTime? effectiveDate)
        {
            Current?.EnsureCacheReady();
            DateTime date = (effectiveDate == null)
                ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date
                : ((DateTime)effectiveDate).Date;
            List<InsurancePPSParameter> ippList = Current.Context.InsurancePPSParameters
                .Where(i => ((i.ParameterName == "PPS Model Version") && (i.ValueInt >= 3) &&
                             (i.InsuranceName != null) &&
                             ((i.StartDate == null) ||
                              ((i.StartDate != null) && (((DateTime)i.StartDate).Date <= date))) &&
                             ((i.EndDate == null) || ((i.EndDate != null) && (((DateTime)i.EndDate).Date >= date)))))
                .OrderBy(i => i.InsuranceName)
                .ToList();
            return ((ippList == null) || (ippList.Count == 0)) ? null : ippList;
        }
    }
}