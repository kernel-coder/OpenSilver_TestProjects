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
    [ExportMetadata("CacheName", ReferenceTableName.Employer)]
    [Export(typeof(ICache))]
    public class EmployerCache : ReferenceCacheBase<Employer>
    {
        public static EmployerCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.Employers;

        [ImportingConstructor]
        public EmployerCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Employer, "003")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("EmployerCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.Employer;
        }

        protected override EntityQuery<Employer> GetEntityQuery()
        {
            return Context.GetEmployerQuery();
        }

        public static List<Employer> GetEmployers(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Employers == null))
            {
                return null;
            }

            var ret = Current.Context.Employers.OrderBy(p => p.EmployerName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Employer { EmployerKey = 0, EmployerName = " " });
            }

            return ret;
        }

        public static List<Employer> GetActiveEmployers(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Employers == null))
            {
                return null;
            }

            var ret = Current.Context.Employers.Where(p => p.Inactive == false).OrderBy(p => p.EmployerName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Employer { EmployerKey = 0, EmployerName = " " });
            }

            return ret;
        }

        public static List<Employer> GetActiveEmployersPlusMe(int? employerKey, bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Employers == null))
            {
                return null;
            }

            int key = (employerKey == null) ? 0 : (int)employerKey;
            var ret = Current.Context.Employers.Where(p => ((p.Inactive == false) || (p.EmployerKey == key)))
                .OrderBy(p => p.EmployerName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Employer { EmployerKey = 0, EmployerName = " " });
            }

            return ret;
        }

        public static Employer GetEmployerFromKey(int? employerKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Employers == null))
            {
                return null;
            }

            if (employerKey == null)
            {
                return null;
            }

            Employer f =
                (from c in Current.Context.Employers.AsQueryable() where (c.EmployerKey == employerKey) select c)
                .FirstOrDefault();
            if ((f == null) && (employerKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error EmployerCache.GetEmployerFromKey: EmployerKey {0} is not defined.  Contact your system administrator.",
                    employerKey.ToString()));
            }

            return f;
        }
    }
}