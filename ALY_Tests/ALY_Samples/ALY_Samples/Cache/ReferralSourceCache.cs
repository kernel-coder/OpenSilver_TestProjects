#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.ReferralSource)]
    [Export(typeof(ICache))]
    public class ReferralSourceCache : ReferenceCacheBase<ReferralSource>
    {
        public static ReferralSourceCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.ReferralSources;

        [ImportingConstructor]
        public ReferralSourceCache(ILogger logManager)
            : base(logManager, ReferenceTableName.ReferralSource, "003")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("ReferralSourceCache already initialized.");
            }

            Current = this;
        }

        protected override EntityQuery<ReferralSource> GetEntityQuery()
        {
            return Context.GetReferralSourceQuery();
        }

        public static List<ReferralSource> GetReferralSources(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ReferralSources == null))
            {
                return null;
            }

            var ret = Current.Context.ReferralSources.OrderBy(p => p.FullName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new ReferralSource { ReferralSourceKey = 0, LastName = " ", FirstName = " " });
            }

            return ret;
        }

        public static int? GetFacilityKeyFromReferralSource(int? rskey)
        {
            Current?.EnsureCacheReady();
            if (!rskey.HasValue || Current == null || Current.Context == null ||
                Current.Context.ReferralSources == null)
            {
                return null;
            }

            ReferralSource rs = Current.Context.ReferralSources.Where(p => p.ReferralSourceKey == rskey)
                .FirstOrDefault();
            return (rs == null) ? null : rs.FacilityKey;
        }

        public static string GetReferralSourceDescFromKey(int? rskey)
        {
            Current?.EnsureCacheReady();
            if (!rskey.HasValue || Current == null || Current.Context == null ||
                Current.Context.ReferralSources == null)
            {
                return null;
            }

            ReferralSource rs = Current.Context.ReferralSources.Where(p => p.ReferralSourceKey == rskey)
                .FirstOrDefault();
            return (rs == null) ? "" : rs.FullName;
        }
    }
}