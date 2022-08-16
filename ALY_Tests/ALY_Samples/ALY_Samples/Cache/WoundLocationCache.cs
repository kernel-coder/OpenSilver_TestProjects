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
    [ExportMetadata("CacheName", ReferenceTableName.WoundLocation)]
    [Export(typeof(ICache))]
    public class WoundLocationCache : ReferenceCacheBase<WoundLocation>
    {
        public static WoundLocationCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.WoundLocations;

        [ImportingConstructor]
        public WoundLocationCache(ILogger logManager)
            : base(logManager, ReferenceTableName.WoundLocation, "003")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("WoundLocationCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.WoundLocation;
            RequireCacheRecords = true;
        }

        protected override EntityQuery<WoundLocation> GetEntityQuery()
        {
            return Context.GetWoundLocationQuery();
        }

        public List<WoundLocation> GetWoundLocations()
        {
            EnsureCacheReady();
            var ret = Current.Context.WoundLocations.ToList();
            return ret;
        }

        public WoundLocation GetWoundLocationFromKey(int key)
        {
            EnsureCacheReady();
            var ret = Current.Context.WoundLocations.FirstOrDefault(w => w.WoundLocationKey == key);
            return ret;
        }

        public WoundPolygon GetWoundPolygonFromKey(int key)
        {
            EnsureCacheReady();
            var ret = Current.Context.WoundPolygons.FirstOrDefault(w => w.WoundPolygonKey == key);
            return ret;
        }

        public string GetWoundLocationDescriptionFromKey(int key)
        {
            WoundLocation w = GetWoundLocationFromKey(key);
            return ((w == null) || (string.IsNullOrWhiteSpace(w.Description)))
                ? ("Other [" + key + "]")
                : w.Description;
        }
    }
}