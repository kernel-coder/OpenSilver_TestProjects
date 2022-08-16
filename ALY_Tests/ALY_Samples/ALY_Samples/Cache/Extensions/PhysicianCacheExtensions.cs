#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache.Extensions
{
    public static class PhysicianCacheExtensions
    {
        public static List<Physician> GetPhysicians(this PhysicianCache me, bool includeEmpty = false)
        {
            me?.EnsureCacheReady();
            if ((me == null) || (me.Context == null) || (me.Context.Physicians == null))
            {
                return null;
            }

            var ret = me.Context.Physicians.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Physician { PhysicianKey = 0, LastName = " ", FirstName = " " });
            }

            return ret;
        }

        public static List<Physician> GetActivePhysicians(this PhysicianCache me, bool includeEmpty = false)
        {
            me?.EnsureCacheReady();
            if ((me == null) || (me.Context == null) || (me.Context.Physicians == null))
            {
                return null;
            }

            var ret = me.Context.Physicians.Where(p => p.Inactive == false).OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Physician { PhysicianKey = 0, LastName = " ", FirstName = " " });
            }

            return ret;
        }

        public static List<Physician> GetActivePhysiciansPlusMe(this PhysicianCache me, int? physicianKey,
            bool includeEmpty = false)
        {
            me?.EnsureCacheReady();
            if ((me == null) || (me.Context == null) || (me.Context.Physicians == null))
            {
                return null;
            }

            int key = (physicianKey == null) ? 0 : (int)physicianKey;
            var ret = me.Context.Physicians.Where(p => (p.Inactive == false) || (p.PhysicianKey == key))
                .OrderBy(p => p.FullNameWithSuffix).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Physician { PhysicianKey = 0, LastName = " ", FirstName = " " });
            }

            return ret;
        }

        public static Physician GetPhysicianFromNPI(this PhysicianCache me, string npi)
        {
            me?.EnsureCacheReady();
            try
            {
                if ((me == null) || (me.Context == null) || (me.Context.Physicians == null))
                {
                    return null;
                }

                if (npi == null)
                {
                    return null;
                }

                Physician p = (from c in me.Context.Physicians.AsQueryable() where (c.NPI.Equals(npi)) select c)
                    .FirstOrDefault();
                if ((p == null) && (npi != null))
                {
                    MessageBox.Show(String.Format(
                        "Error PhysicianCache.GetPhysicianFromNPI: NPI {0} is not defined.  Contact your system administrator.",
                        npi));
                }

                return p;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Physician GetPhysicianFromKey(this PhysicianCache me, int? physicianKey)
        {
            me?.EnsureCacheReady();
            if ((me == null) || (me.Context == null) || (me.Context.Physicians == null))
            {
                return null;
            }

            if (physicianKey == null)
            {
                return null;
            }

            Physician p =
                (from c in me.Context.Physicians.AsQueryable() where (c.PhysicianKey == physicianKey) select c)
                .FirstOrDefault();
            if ((p == null) && (physicianKey != 0) && (physicianKey != -1))
            {
                MessageBox.Show(String.Format(
                    "Error PhysicianCache.GetPhysicianFromKey: PhysicianKey {0} is not defined.  Contact your system administrator.",
                    physicianKey.ToString()));
            }

            return p;
        }

        public static PhysicianAddress GetPhysicianAddressFromKey(this PhysicianCache me, int? physicianAddressKey)
        {
            me?.EnsureCacheReady();
            if ((me == null) || (me.Context == null) || (me.Context.Physicians == null))
            {
                return null;
            }

            if (physicianAddressKey == null)
            {
                return null;
            }

            PhysicianAddress p =
                (from c in me.Context.PhysicianAddresses.AsQueryable()
                    where (c.PhysicianAddressKey == physicianAddressKey)
                    select c).FirstOrDefault();
            if ((p == null) && (physicianAddressKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error PhysicianCache.GetPhysicianAddressFromKey: PhysicianAddressKey {0} is not defined.  Contact your system administrator.",
                    physicianAddressKey.ToString()));
            }

            return p;
        }

        public static string GetPhysicianAddressTextFromKey(this PhysicianCache me, int? physicianAddressKey)
        {
            me?.EnsureCacheReady();
            PhysicianAddress pa = me.GetPhysicianAddressFromKey(physicianAddressKey);
            if (pa == null)
            {
                return "none";
            }

            return string.Format("{0} {1}, {2}", pa.Address1, pa.Address2, pa.CityStateZip2);
        }

        public static string GetPhysicianFullNameInformalWithSuffixFromKey(this PhysicianCache me, int? physicianKey)
        {
            me?.EnsureCacheReady();
            Physician p = me.GetPhysicianFromKey(physicianKey);
            return p?.FullNameInformalWithSuffix;
        }

        public static string GetPhysicianFullNameWithSuffixFromKey(this PhysicianCache me, int? physicianKey)
        {
            me?.EnsureCacheReady();
            Physician p = me.GetPhysicianFromKey(physicianKey);
            return p?.FullNameWithSuffix;
        }

        public static bool GetPhysicianPECOSFromKey(this PhysicianCache me, int? physicianKey)
        {
            me?.EnsureCacheReady();
            Physician p = me.GetPhysicianFromKey(physicianKey);
            return p?.PECOS ?? true;
        }

        public static int? GetPhysicianKeyFromNPI(this PhysicianCache me, string npi)
        {
            me?.EnsureCacheReady();
            Physician p = me.GetPhysicianFromNPI(npi);
            return p?.PhysicianKey as int?;
        }

        public static string GetPhysicianNPIFromKey(this PhysicianCache me, int? physicianKey)
        {
            me?.EnsureCacheReady();
            Physician p = me.GetPhysicianFromKey(physicianKey);
            return p?.NPI;
        }

        public static List<PhysicianAddress> GetActivePhysicianAddressesForPhysician(this PhysicianCache me,
            int phyKeyParm)
        {
            me?.EnsureCacheReady();
            if ((me == null) || (me.Context == null) || (me.Context.PhysicianAddresses == null))
            {
                return null;
            }

            var ret = me.Context.PhysicianAddresses.Where(p => !p.Inactive && p.PhysicianKey == phyKeyParm).ToList();
            return ret;
        }

        public static List<PhysicianAddress> GetActivePhysicianAddressesForPhysicianPlusMe(this PhysicianCache me,
            int phyKeyParm)
        {
            me?.EnsureCacheReady();
            if ((me == null) || (me.Context == null) || (me.Context.PhysicianAddresses == null))
            {
                return null;
            }

            var ret = me.Context.PhysicianAddresses
                .Where(p => (!p.Inactive && p.PhysicianKey == phyKeyParm) || (p.PhysicianKey == phyKeyParm)).ToList();
            return ret;
        }

        public static List<PhysicianAddress> GetPhysicianAddressesWithTrackingGroups(this PhysicianCache me)
        {
            me?.EnsureCacheReady();
            if ((me == null) || (me.Context == null) || (me.Context.PhysicianAddresses == null))
            {
                return null;
            }

            var ret = me.Context.PhysicianAddresses.Where(p => !p.Inactive && p.TrackingGroup.HasValue).ToList();
            return ret;
        }

        private static int? TeamMeetingPhysicianKey;

        public static int? GetTeamMeetingPhysicianKey(this PhysicianCache me)
        {
            me?.EnsureCacheReady();
            return TeamMeetingPhysicianKey;
        }

        public static void SetTeamMeetingPhysicianKey(this PhysicianCache me, int? pTeamMeetingPhysicianKey)
        {
            me?.EnsureCacheReady();
            if (pTeamMeetingPhysicianKey != -1)
            {
                TeamMeetingPhysicianKey =
                    pTeamMeetingPhysicianKey; // we don't propagate 'Not Applicable' "physician" across TMs in the worklist
            }
        }
    }
}