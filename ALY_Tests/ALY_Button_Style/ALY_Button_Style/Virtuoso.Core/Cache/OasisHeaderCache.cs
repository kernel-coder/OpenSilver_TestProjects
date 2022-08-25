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
    [ExportMetadata("CacheName", ReferenceTableName.OasisHeader)]
    [Export(typeof(ICache))]
    public class OasisHeaderCache : ReferenceCacheBase<OasisHeader>
    {
        public static OasisHeaderCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.OasisHeaders;

        [ImportingConstructor]
        public OasisHeaderCache(ILogger logManager)
            : base(logManager, ReferenceTableName.OasisHeader, "005")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("OasisHeaderCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.OasisHeader;
        }

        protected override EntityQuery<OasisHeader> GetEntityQuery()
        {
            return Context.GetOasisHeaderQuery();
        }

        public static List<OasisHeader> GetOasisHeaders(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.OasisHeaders == null))
            {
                return null;
            }

            var ret = Current.Context.OasisHeaders.OrderBy(p => p.OasisHeaderName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new OasisHeader { OasisHeaderKey = 0, OasisHeaderName = " " });
            }

            return ret;
        }

        public static bool OasisHeaderNameInUse(OasisHeader newOasisHeader)
        {
            Current?.EnsureCacheReady();
            if (newOasisHeader == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(newOasisHeader.OasisHeaderName))
            {
                return false;
            }

            if ((Current == null) || (Current.Context == null) || (Current.Context.OasisHeaders == null))
            {
                return false;
            }

            OasisHeader v = Current.Context.OasisHeaders.Where(p =>
                (p.OasisHeaderName == newOasisHeader.OasisHeaderName) &&
                (p.OasisHeaderKey != newOasisHeader.OasisHeaderKey)).FirstOrDefault();
            return (v == null) ? false : true;
        }

        public static bool OasisHeaderIDsInUse(OasisHeader newOasisHeader)
        {
            Current?.EnsureCacheReady();
            if (newOasisHeader == null)
            {
                return false;
            }

            if ((Current == null) || (Current.Context == null) || (Current.Context.OasisHeaders == null))
            {
                return false;
            }

            OasisHeader v = Current.Context.OasisHeaders.Where(p =>
                (p.Inactive == false) &&
                ((p.NPI == newOasisHeader.NPI) && (p.CMSCertificationNumber == newOasisHeader.CMSCertificationNumber) &&
                 (p.HHAAgencyID == newOasisHeader.HHAAgencyID) &&
                 (p.BranchIDNumber == newOasisHeader.BranchIDNumber)) &&
                (p.OasisHeaderKey != newOasisHeader.OasisHeaderKey)).FirstOrDefault();
            return (v == null) ? false : true;
        }

        public static List<OasisHeader> GetActiveOasisHeaders(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.OasisHeaders == null))
            {
                return null;
            }

            var ret = Current.Context.OasisHeaders.Where(p => p.Inactive == false).OrderBy(p => p.OasisHeaderName)
                .ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new OasisHeader { OasisHeaderKey = 0, OasisHeaderName = " " });
            }

            return ret;
        }

        public static List<OasisHeader> GetActiveOasisHeadersPlusMe(int? oasisHeaderKey, bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.OasisHeaders == null))
            {
                return null;
            }

            int key = oasisHeaderKey ?? 0;
            var ret = Current.Context.OasisHeaders.Where(p => ((p.Inactive == false) || (p.OasisHeaderKey == key)))
                .OrderBy(p => p.OasisHeaderName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new OasisHeader { OasisHeaderKey = 0, OasisHeaderName = " " });
            }

            return ret;
        }

        public static OasisHeader GetOasisHeaderFromKey(int? oasisHeaderKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.OasisHeaders == null))
            {
                return null;
            }

            if (oasisHeaderKey == null)
            {
                return null;
            }

            OasisHeader oh =
                (from c in Current.Context.OasisHeaders.AsQueryable()
                    where (c.OasisHeaderKey == oasisHeaderKey)
                    select c).FirstOrDefault();
            if ((oh == null) && (oasisHeaderKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error OasisHeaderCache.GetOasisHeaderFromKey: OasisHeaderKey {0} is not defined.  Contact your system administrator.",
                    oasisHeaderKey.ToString()));
            }

            return oh;
        }
    }
}