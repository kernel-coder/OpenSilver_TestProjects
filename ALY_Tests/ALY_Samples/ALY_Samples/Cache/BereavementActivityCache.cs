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
    [ExportMetadata("CacheName", ReferenceTableName.BereavementActivity)]
    [Export(typeof(ICache))]
    public class BereavementActivityCache : ReferenceCacheBase<BereavementActivity>
    {
        public static BereavementActivityCache Current { get; private set; }

        [ImportingConstructor]
        public BereavementActivityCache(ILogger logManager)
            : base(logManager, ReferenceTableName.BereavementActivity, "001")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("BereavementActivityCache already initialized.");
            }

            Current = this;
        }

        protected override EntitySet EntitySet => Context.BereavementActivities;

        protected override EntityQuery<BereavementActivity> GetEntityQuery()
        {
            return Context.GetBereavementActivityQuery();
        }

        public static List<BereavementActivity> GetBereavementActivities(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.BereavementActivities == null))
            {
                return null;
            }

            var ret = Current.Context.BereavementActivities.OrderBy(p => p.ActivityDescription).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new BereavementActivity { BereavementActivityKey = 0, ActivityDescription = " " });
            }

            return ret;
        }

        public static BereavementActivity GetBereavementActivityByKey(int? bereavementActivityKey)
        {
            Current?.EnsureCacheReady();
            if (!bereavementActivityKey.HasValue || Current == null || Current.Context == null ||
                Current.Context.BereavementActivities == null)
            {
                return null;
            }
            
            BereavementActivity ba = Current.Context.BereavementActivities
                .Where(p => p.BereavementActivityKey == bereavementActivityKey).FirstOrDefault();
            return ba;
        }
    }
}