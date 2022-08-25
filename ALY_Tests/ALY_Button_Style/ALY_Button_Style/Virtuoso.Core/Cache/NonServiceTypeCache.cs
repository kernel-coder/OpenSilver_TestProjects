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
    [ExportMetadata("CacheName", ReferenceTableName.NonServiceType)]
    [Export(typeof(ICache))]
    public class NonServiceTypeCache : ReferenceCacheBase<NonServiceType>
    {
        public static NonServiceTypeCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.NonServiceTypes;

        [ImportingConstructor]
        public NonServiceTypeCache(ILogger logManager)
            : base(logManager, ReferenceTableName.NonServiceType, "006")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("NonServiceTypeCache already initialized.");
            }

            Current = this;
        }

        protected override EntityQuery<NonServiceType> GetEntityQuery()
        {
            return Context.GetNonServiceTypeQuery();
        }

        public static List<NonServiceType> GetNonServiceTypes()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.NonServiceTypes == null))
            {
                return null;
            }

            return Current.Context.NonServiceTypes.OrderBy(p => p.Description).ToList();
        }

        public static List<NonServiceType> GetNonServiceTypesActive()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.NonServiceTypes == null))
            {
                return null;
            }

            return Current.Context.NonServiceTypes.Where(p => p.Inactive == false).OrderBy(p => p.Description).ToList();
        }

        public static NonServiceType GetNonServiceTypeFromKey(int? NonServiceTypeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.NonServiceTypes == null))
            {
                return null;
            }

            if (NonServiceTypeKey == null)
            {
                return null;
            }

            NonServiceType i =
                (from c in Current.Context.NonServiceTypes.AsQueryable()
                    where (c.NonServiceTypeKey == NonServiceTypeKey)
                    select c).FirstOrDefault();
            if ((i == null) && (NonServiceTypeKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error NonServiceTypeCache.GetINonServiceTypeName: NonServiceTypeKey {0} is not defined.  Contact your system administrator.",
                    NonServiceTypeKey.ToString()));
            }

            return i;
        }

        public static string GetNonServiceTypeDescFromKey(int? NonServiceTypeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.NonServiceTypes == null))
            {
                return null;
            }

            if (NonServiceTypeKey == null)
            {
                return null;
            }

            NonServiceType i = GetNonServiceTypeFromKey(NonServiceTypeKey);
            return i?.Description;
        }
    }
}