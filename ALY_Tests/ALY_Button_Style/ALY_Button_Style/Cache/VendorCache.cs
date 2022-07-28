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
    [ExportMetadata("CacheName", ReferenceTableName.Vendor)]
    [Export(typeof(ICache))]
    public class VendorCache : ReferenceCacheBase<Vendor>
    {
        public static VendorCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.Vendors;

        [ImportingConstructor]
        public VendorCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Vendor, "005")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("VendorCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.Vendor;
        }

        protected override EntityQuery<Vendor> GetEntityQuery()
        {
            return Context.GetVendorQuery();
        }

        public static List<Vendor> GetVendors(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Vendors == null))
            {
                return null;
            }

            var ret = Current.Context.Vendors.OrderBy(p => p.VendorName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Vendor { VendorKey = 0, VendorName = " " });
            }

            return ret;
        }

        public static bool VendorNameInUse(Vendor newVendor)
        {
            Current?.EnsureCacheReady();
            if (newVendor == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(newVendor.VendorName))
            {
                return false;
            }

            if ((Current == null) || (Current.Context == null) || (Current.Context.Vendors == null))
            {
                return false;
            }

            Vendor v = Current.Context.Vendors.FirstOrDefault(p => (p.VendorName == newVendor.VendorName) && (p.VendorKey != newVendor.VendorKey));
            return (v != null);
        }

        public static List<Vendor> GetActiveVendors(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Vendors == null))
            {
                return null;
            }

            var ret = Current.Context.Vendors.Where(p => p.Inactive == false).OrderBy(p => p.VendorName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Vendor { VendorKey = 0, VendorName = " " });
            }

            return ret;
        }

        public static List<Vendor> GetActiveVendorsPlusMe(int? vendorKey, bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Vendors == null))
            {
                return null;
            }

            int key = vendorKey ?? 0;
            var ret = Current.Context.Vendors.Where(p => ((p.Inactive == false) || (p.VendorKey == key)))
                .OrderBy(p => p.VendorName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Vendor { VendorKey = 0, VendorName = " " });
            }

            return ret;
        }

        public static Vendor GetVendorFromKey(int? vendorKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Vendors == null))
            {
                return null;
            }

            if (vendorKey == null)
            {
                return null;
            }

            Vendor f = (from c in Current.Context.Vendors.AsQueryable() where (c.VendorKey == vendorKey) select c)
                .FirstOrDefault();
            if ((f == null) && (vendorKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error VendorCache.GetVendorFromKey: VendorKey {0} is not defined.  Contact your system administrator.",
                    vendorKey.ToString()));
            }

            return f;
        }
    }
}