#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.CensusTract)]
    [Export(typeof(ICache))]
    public class CensusTractCache : ReferenceCacheBase<CensusTract>
    {
        public static CensusTractCache Current { get; private set; }

        public VirtuosoDomainContext CacheContext => Context;

        [ImportingConstructor]
        public CensusTractCache(ILogger logManager)
            : base(logManager, ReferenceTableName.CensusTract, "005")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("CensusTractCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.CensusTract;
        }

        protected override EntitySet EntitySet => Context.CensusTracts;

        protected override EntityQuery<CensusTract> GetEntityQuery()
        {
            return Context.GetCensusTractQuery();
        }

        public static List<CensusTractMapping> GetMappingForCensusTractTextAndDate(string CensusTractText, DateTime Dt)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.CensusTracts == null))
            {
                return null;
            }

            List<CensusTractMapping> ret = new List<CensusTractMapping>();

            foreach (CensusTract ct in Current.Context.CensusTracts
                         .Where(t => (t.CensusTractText == CensusTractText) && (!t.Inactive)))
                ret.AddRange(ct.CensusTractMapping
                    .Where(ctm => (ctm.EffectiveFromDate <= Dt)
                                  && ((!ctm.EffectiveThruDate.HasValue)
                                      || (ctm.EffectiveThruDate >= Dt))));

            return ret;
        }

        public static List<CensusTract> GetCensusTracts(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.CensusTracts == null))
            {
                return null;
            }

            var ret = Current.Context.CensusTracts.OrderBy(s => s.CensusTractText).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new CensusTract { CensusTractKey = 0, CensusTractText = string.Empty });
            }

            return ret;
        }

        public static List<CensusTract> GetCensusTractByZipCodeParts(string zipCode)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.CensusTracts == null))
            {
                return null;
            }

            return Current.Context.CensusTracts
                .Where(g => g.ZipCode == zipCode && g.Inactive == false)
                .OrderBy(g => g.ZipCode)
                .ThenBy(g => g.Plus4)
                .ToList();
        }

        public static IEnumerable<CensusTract> GetCensusTracts(string censustracttext)
        {
            Current?.EnsureCacheReady();
            if (string.IsNullOrEmpty(censustracttext) || (Current == null) || (Current.Context == null) ||
                (Current.Context.CensusTracts == null))
            {
                return null;
            }

            return Current.Context.CensusTracts.Where(s => s.CensusTractText == censustracttext);
        }

        public static CensusTract GetCensusTract(int? censustractkey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.CensusTracts == null))
            {
                return null;
            }

            return Current.Context.CensusTracts.FirstOrDefault(s => s.CensusTractKey == censustractkey);
        }
    }
}