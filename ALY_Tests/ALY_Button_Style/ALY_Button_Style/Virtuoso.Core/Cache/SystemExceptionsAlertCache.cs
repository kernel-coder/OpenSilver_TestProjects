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
    [ExportMetadata("CacheName", ReferenceTableName.SystemExceptionsAndAlerts)]
    [Export(typeof(ICache))]
    public class SystemExceptionsAlertCache : ReferenceCacheBase<SystemExceptionsAndAlerts>
    {
        public static SystemExceptionsAlertCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.SystemExceptionsAndAlerts;

        [ImportingConstructor]
        public SystemExceptionsAlertCache(ILogger logManager)
            : base(logManager, ReferenceTableName.SystemExceptionsAndAlerts, "006")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("SystemExceptionsAlertCache already initialized.");
            }

            Current = this;
        }

        protected override EntityQuery<SystemExceptionsAndAlerts> GetEntityQuery()
        {
            return Context.GetSystemExceptionsAndAlertsQuery();
        }

        public static List<SystemExceptionsAndAlerts> GetExceptionsAndAlerts(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.SystemExceptionsAndAlerts == null))
            {
                return null;
            }

            var ret = Current.Context.SystemExceptionsAndAlerts.OrderBy(p => p.DisplayName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new SystemExceptionsAndAlerts { ExceptAlertKey = 0, DisplayName = " " });
            }

            return ret;
        }

        public static string GetDisplayNameFromKey(int alert_Key)
        {
            SystemExceptionsAndAlerts sa = GetExceptionsAndAlertsFromKey(alert_Key);
            return sa?.DisplayName;
        }

        public static int GeKeyFromDisplayName(string displayName)
        {
            SystemExceptionsAndAlerts sa = GetExceptionsAndAlertsFromDisplayName(displayName);
            return sa?.ExceptAlertKey ?? 0;
        }

        public static SystemExceptionsAndAlerts GetExceptionsAndAlertsFromKey(int? exceptAlertKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.SystemExceptionsAndAlerts == null))
            {
                return null;
            }

            if (exceptAlertKey == null)
            {
                return null;
            }

            SystemExceptionsAndAlerts sa =
                (from c in Current.Context.SystemExceptionsAndAlerts.AsQueryable()
                    where (c.ExceptAlertKey == exceptAlertKey)
                    select c).FirstOrDefault();
            if ((sa == null) && (exceptAlertKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error SystemExceptionsAlertCache.GetExceptionsAndAlertsFromKey: ExceptAlertKey {0} is not defined.  Contact your system administrator.",
                    exceptAlertKey.ToString()));
            }

            return sa;
        }

        public static SystemExceptionsAndAlerts GetExceptionsAndAlertsFromDisplayName(string displayName)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.SystemExceptionsAndAlerts == null))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                return null;
            }

            SystemExceptionsAndAlerts sa =
                (from c in Current.Context.SystemExceptionsAndAlerts.AsQueryable()
                    where (c.DisplayName == displayName)
                    select c).FirstOrDefault();
            if (sa == null)
            {
                MessageBox.Show(String.Format(
                    "Error SystemExceptionsAlertCache.GetExceptionsAndAlertsFromDisplayName: DisplayName '{0}' is not defined.  Contact your system administrator.",
                    displayName));
            }

            return sa;
        }
    }
}