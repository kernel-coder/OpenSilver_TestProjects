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
    [ExportMetadata("CacheName", ReferenceTableName.Facility)]
    [Export(typeof(ICache))]
    public class FacilityCache : ReferenceCacheBase<Facility>
    {
        public static FacilityCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.Facilities;

        [ImportingConstructor]
        public FacilityCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Facility, "006")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("FacilityCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.Facility;
        }

        protected override EntityQuery<Facility> GetEntityQuery()
        {
            return Context.GetFacilityQuery();
        }

        public static List<FacilityBranch> GetFacilityBranches(int? facilityKey, bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            var ret = Current.Context.FacilityBranches.Where(x => !x.Inactive).Where(x => x.FacilityKey == facilityKey)
                .OrderBy(fb => fb.BranchName).ToList();
            if (facilityKey == null)
            {
                ret = Current.Context.FacilityBranches.ToList();
            }

            if (includeEmpty)
            {
                ret.Insert(0, new FacilityBranch { FacilityBranchKey = 0, BranchName = " " });
            }

            return ret;
        }

        public static string GetFacilityBranchName(int? FacilityBranchKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            var ret = Current.Context.FacilityBranches.FirstOrDefault(x => x.FacilityBranchKey == FacilityBranchKey);
            if (ret != null)
            {
                return ret.BranchName;
            }

            return string.Empty;
        }

        public static List<FacilityBranch> GetActiveFacilityBranchesAndMe(int? facilityKey, int? branchkey,
            bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            int key = branchkey ?? 0;
            var ret = Current.Context.FacilityBranches.Where(x =>
                x.FacilityKey == facilityKey && (x.Inactive == false || x.FacilityBranchKey == key)).ToList();

            return ret;
        }

        public static List<FacilityBranch> GetActiveBranches(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            var ret = Current.Context.FacilityBranches.Where(x => x.Inactive == false).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new FacilityBranch { FacilityBranchKey = 0, BranchName = " " });
            }

            return ret;
        }

        public static List<Facility> GetFacilitiesWithActiveBranches(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            var ret = Current.Context.Facilities
                .Where(b => b.FacilityBranch.Any())
                .OrderBy(p => p.Name).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Facility { FacilityKey = 0, Name = " " });
            }

            return ret;
        }

        public static List<FacilityBranch> GetActiveBranchesAndMe(int? branchkey, bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            int key = branchkey ?? 0;
            var ret = Current.Context.FacilityBranches.Where(x => x.Inactive == false || x.FacilityBranchKey == key)
                .ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new FacilityBranch { FacilityBranchKey = 0, BranchName = " " });
            }

            return ret;
        }

        public static List<Facility> GetFacilities(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            var ret = Current.Context.Facilities.OrderBy(p => p.Name).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Facility { FacilityKey = 0, Name = " " });
            }

            return ret;
        }

        public static List<Facility> GetActiveFacilities(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            var ret = Current.Context.Facilities.Where(p => p.Inactive == false).OrderBy(p => p.Name).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Facility { FacilityKey = 0, Name = " " });
            }

            return ret;
        }

        public static List<Facility> GetActiveFacilitiesByType(int? facilityType, bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            var ret = Current.Context.Facilities.Where(p => p.Inactive == false && p.Type == facilityType)
                .OrderBy(p => p.Name).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Facility { FacilityKey = 0, Name = " " });
            }

            return ret;
        }

        public static List<FacilityBranch> GetActiveFacilityBranchesByFacilityType(int? facilityTypeKey,
            bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            var ret = Current.Context.FacilityBranches.Where(x => !x.Inactive)
                .Where(x => x.Facility.Type == facilityTypeKey && x.Facility.Inactive == false)
                .OrderBy(fb => fb.BranchName).ToList(); //DS 0506
            if (includeEmpty)
            {
                ret.Insert(0, new FacilityBranch { FacilityBranchKey = 0, BranchName = " " });
            }

            return ret;
        }

        public static List<Facility> GetActiveFacilitiesPlusMe(int? facilityKey, bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            int key = facilityKey ?? 0;
            var ret = Current.Context.Facilities.Where(p => ((p.Inactive == false) || (p.FacilityKey == key)))
                .OrderBy(p => p.Name).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Facility { FacilityKey = 0, Name = " " });
            }

            return ret;
        }

        public static Facility GetFacilityFromKey(int? facilityKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            if (facilityKey == null)
            {
                return null;
            }

            Facility f =
                (from c in Current.Context.Facilities.AsQueryable() where (c.FacilityKey == facilityKey) select c)
                .FirstOrDefault();
            if ((f == null) && (facilityKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error FacilityCache.GetFacilityFromKey: FacilityKey {0} is not defined.  Contact your system administrator.",
                    facilityKey.ToString()));
            }

            return f;
        }

        public static string GetFacilityNameFromKey(int? facilityKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            if (facilityKey == null)
            {
                return null;
            }

            Facility f =
                (from c in Current.Context.Facilities.AsQueryable() where (c.FacilityKey == facilityKey) select c)
                .FirstOrDefault();
            if ((f == null) && (facilityKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error FacilityCache.GetFacilityFromKey: FacilityKey {0} is not defined.  Contact your system administrator.",
                    facilityKey.ToString()));
            }

            return f?.Name;
        }

        public static FacilityBranch GetFacilityBranchFromKey(int? facilitybranchKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            if (facilitybranchKey == null)
            {
                return null;
            }

            FacilityBranch f =
                (from c in Current.Context.FacilityBranches.AsQueryable()
                    where (c.FacilityBranchKey == facilitybranchKey)
                    select c).FirstOrDefault();
            if ((f == null) && (facilitybranchKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error FacilityCache.GetFacilityBranchFromKey: FacilityKey {0} is not defined.  Contact your system administrator.",
                    facilitybranchKey.ToString()));
            }

            return f;
        }

        public static string GetPatientIDLabelFromKey(object facilityKey, object patientIDLabelNumber)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Facilities == null))
            {
                return null;
            }

            if (facilityKey == null)
            {
                return null;
            }

            if (facilityKey.ToString().Trim() == String.Empty)
            {
                return null;
            }

            int key = Convert.ToInt32(facilityKey);
            if (key == 0)
            {
                return null;
            }

            Facility f = GetFacilityFromKey(key);
            if (f == null)
            {
                return null;
            }

            if (patientIDLabelNumber == null)
            {
                return null;
            }

            if (patientIDLabelNumber.ToString().Trim() == String.Empty)
            {
                return null;
            }

            int n = Convert.ToInt32(patientIDLabelNumber);
            if ((n < 1) || (n > 3))
            {
                return null;
            }

            return (n == 1) ? f.PatientID1Label : (n == 2) ? f.PatientID2Label : f.PatientID3Label;
        }
    }
}