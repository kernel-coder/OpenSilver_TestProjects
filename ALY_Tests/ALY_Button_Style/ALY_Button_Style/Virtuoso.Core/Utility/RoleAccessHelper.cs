#region Usings

using System.Collections.Generic;
using System.Linq;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Utility
{
    public class RoleAccessHelper
    {
        private static readonly RoleAccess roleAccess = new RoleAccess();

        public static bool CheckPermission(string resource, bool needtolookup = true)
        {
            if (resource != "Surveyor" && WebContext.Current.User.DeltaAdmin)
            {
                return true;
            }

            List<string> items = new List<string>();

            if (needtolookup)
            {
                var prop = typeof(RoleAccess).GetProperties().FirstOrDefault(p => p.Name.Equals(resource));
                if (prop == null)
                {
                    return false;
                }

                var val = prop.GetValue(null, null);
                var str = val.ToString();
                items = str.Split('|').ToList();
            }
            else
            {
                items = resource.Split('|').ToList();
            }

            return CheckPermission(items);
        }

        public static bool CheckPermission(List<string> item)
        {
            if (WebContext.Current.User.Roles != null)
            {
                foreach (var r in WebContext.Current.User.Roles)
                    if (item.Contains(r) || item.Contains("All"))
                    {
                        return true;
                    }
            }

            return false;
        }

        public static bool IsSurveyor => (CheckPermission(RoleAccess.Surveyor, false));

        public RoleAccess RoleAccess => roleAccess;
    }
}