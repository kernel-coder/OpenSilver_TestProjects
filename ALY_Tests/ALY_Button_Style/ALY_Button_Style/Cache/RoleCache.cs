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
    [ExportMetadata("CacheName", ReferenceTableName.Role)]
    [Export(typeof(ICache))]
    public class RoleCache : ReferenceCacheBase<Role>
    {
        public static RoleCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.Roles;

        [ImportingConstructor]
        public RoleCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Role, "005")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("RoleCache already initialized.");
            }

            Current = this;
        }

        protected override EntityQuery<Role> GetEntityQuery()
        {
            return Context.GetRoleQuery();
        }

        public static List<Role> GetRoles()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Roles == null))
            {
                return null;
            }

            return Current.Context.Roles.OrderBy(p => p.RoleName).ToList();
        }

        public static Role GetRoleFromKey(int rolekey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Roles == null))
            {
                return null;
            }

            return Current.Context.Roles.FirstOrDefault(p => p.RoleKey == rolekey);
        }

        public static int? GetRoleKeyFromRoleName(string roleName)
        {
            Current?.EnsureCacheReady();
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return null;
            }

            if ((Current == null) || (Current.Context == null) || (Current.Context.Roles == null))
            {
                return null;
            }

            Role r = Current.Context.Roles.Where(p => p.RoleName.Trim().ToLower() == roleName.Trim().ToLower())
                .FirstOrDefault();
            return r?.RoleKey;
        }

        public static string GetRoleNameFromKey(int rolekey)
        {
            Current?.EnsureCacheReady();
            Role role = GetRoleFromKey(rolekey);
            return role?.RoleName;
        }
    }
}