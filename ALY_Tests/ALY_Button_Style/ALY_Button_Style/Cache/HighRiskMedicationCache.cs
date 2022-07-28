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
    [ExportMetadata("CacheName", ReferenceTableName.HighRiskMedication)]
    [Export(typeof(ICache))]
    public class HighRiskMedicationCache : ReferenceCacheBase<HighRiskMedication>
    {
        public static HighRiskMedicationCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.HighRiskMedications;

        [ImportingConstructor]
        public HighRiskMedicationCache(ILogger logManager)
            : base(logManager, ReferenceTableName.HighRiskMedication, "004")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("HighRiskMedicationCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.HighRiskMedication;
        }

        protected override EntityQuery<HighRiskMedication> GetEntityQuery()
        {
            return Context.GetHighRiskMedicationQuery();
        }

        public static List<HighRiskMedication> GetHighRiskMedications(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.HighRiskMedications == null))
            {
                return null;
            }

            var ret = Current.Context.HighRiskMedications.OrderBy(p => p.MedicationName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new HighRiskMedication { HighRiskMedicationKey = 0, MedicationName = " " });
            }

            return ret;
        }

        public static HighRiskMedication GetHighRiskMedicationFromKey(int? highRiskMedicationKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.HighRiskMedications == null))
            {
                return null;
            }

            if (highRiskMedicationKey == null)
            {
                return null;
            }

            HighRiskMedication m =
                (from c in Current.Context.HighRiskMedications.AsQueryable()
                    where (c.HighRiskMedicationKey == highRiskMedicationKey)
                    select c).FirstOrDefault();
            if ((m == null) && (highRiskMedicationKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error HighRiskMedicationCache.GetHighRiskMedicationFromKey: HighRiskMedicationKey {0} is not defined.  Contact your system administrator.",
                    highRiskMedicationKey.ToString()));
            }

            return m;
        }

        public static HighRiskMedication GetHighRiskMedicationByMediSpanMedicationKeyAndServiceLineKey(
            int? mediSpanMedicationKey, int? serviceLineKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.HighRiskMedications == null))
            {
                return null;
            }

            HighRiskMedication m =
                (from c in Current.Context.HighRiskMedications.AsQueryable()
                    where ((c.MediSpanMedicationKey == mediSpanMedicationKey) && (c.ServiceLineKey == serviceLineKey))
                    select c).FirstOrDefault();
            return m;
        }

        public static HighRiskMedication GetHighRiskMedicationByRDIDAndServiceLineKey(int? RDID, int? serviceLineKey)
        {
            Current?.EnsureCacheReady();
            if (RDID == null)
            {
                return null;
            }

            if ((Current == null) || (Current.Context == null) || (Current.Context.HighRiskMedications == null))
            {
                return null;
            }

            HighRiskMedication m =
                (from c in Current.Context.HighRiskMedications.AsQueryable()
                    where ((c.RDID == (int)RDID) && (c.ServiceLineKey == serviceLineKey))
                    select c).FirstOrDefault();
            return m;
        }

        public static bool IsHighRiskMedicationActive(HighRiskMedication med, DateTime? date = null)
        {
            if (med == null)
            {
                return true;
            }

            DateTime myDate = date ?? DateTime.Today.Date;
            return (((med.EffectiveFromDate == null) ||
                     ((med.EffectiveFromDate != null) && (med.EffectiveFromDate <= myDate))) &&
                    ((med.EffectiveThruDate == null) ||
                     ((med.EffectiveThruDate != null) && (med.EffectiveThruDate >= myDate))));
        }
    }
}