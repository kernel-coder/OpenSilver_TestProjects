#region Usings

using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public class OrderTrackingManager
    {
        // the orders tracking group is primarily being built for VNSNY.  their "In State" group are those physicians that do not 
        // match any other group and are in "NY", "NJ" or "CT".  for now, the inStateList is being defaulted to those 3 states.
        // if more agencies start to use this, this list will need populated with different states.
        public List<string> inStateList = new List<string> { "NY", "NJ", "CT" };

        public int? FindDefaultTrackingGroupForPhysician(PhysicianAddress physicianAddress)
        {
            int? defaultTrackingGroup = null;

            if (TenantSettingsCache.Current.TenantSettingAutoAssignTrackingGroups)
            {
                if (physicianAddress.Type.HasValue
                    && ((!string.IsNullOrEmpty(physicianAddress.ZipCode))
                        || physicianAddress.FacilityBranchKey.HasValue
                    )
                   )
                {
                    string addressType = CodeLookupCache.GetCodeFromKey(physicianAddress.Type.Value);
                    if (!string.IsNullOrEmpty(addressType)
                        && ((addressType.ToUpper() == "MAIN")
                            || (addressType.ToUpper() == "BRANCH")
                        )
                       )
                    {
                        defaultTrackingGroup = FindDefaultTrackingGroup(
                            (physicianAddress.FacilityBranchRelated) ? physicianAddress.FacilityBranchKey : null,
                            physicianAddress.ZipCode, physicianAddress.County, physicianAddress.StateCode,
                            physicianAddress.PhysicianKey);
                    }
                }
            }

            return defaultTrackingGroup;
        }

        private int? FindDefaultTrackingGroup(int? facilityBranchKey, string zipCode, string county, int? state,
            int physicianKeyParm)
        {
            int? defaultTrackingGroup = null;
            List<OrderTrackingGroup> trackingGroupList = null;

            // if there the address contains a facilityBranchKey, search by facility branch first.
            if (facilityBranchKey.HasValue
                && (facilityBranchKey > 0)
               )
            {
                trackingGroupList = GetMatchingTrackingGroups(facilityBranchKey, null, null, null);
            }

            // if we didn't find a match on facility, and there is a zip code, search by zip code
            if (((trackingGroupList == null)
                 || (trackingGroupList.Any() == false)
                )
                && (!string.IsNullOrEmpty(zipCode))
               )
            {
                trackingGroupList = GetMatchingTrackingGroups(null, zipCode, null, null);
            }

            // if we didn't find a match on zip code, and there is a county, search by county
            if (((trackingGroupList == null)
                 || (trackingGroupList.Any() == false)
                )
                && (!string.IsNullOrEmpty(county))
               )
            {
                trackingGroupList = GetMatchingTrackingGroups(null, null, county, null);
            }

            // if we didn't find a match on county, and there is a state, search by state
            if (((trackingGroupList == null)
                 || (trackingGroupList.Any() == false)
                )
                && (state.HasValue
                    && (state > 0)
                )
               )
            {
                trackingGroupList = GetMatchingTrackingGroups(null, null, null, state);
            }

            if ((trackingGroupList != null)
                && trackingGroupList.Any()
               )
            {
                defaultTrackingGroup = SelectTrackingGroupFromList(trackingGroupList, physicianKeyParm);
            }

            // if there is still no match, and the state is in the inStateList, the default tracking group should be 
            // the "In State Group".
            if (!defaultTrackingGroup.HasValue
                || (defaultTrackingGroup <= 0)
               )
            {
                var states = inStateList.Select(s => CodeLookupCache.GetKeyFromCode("STATE", s))
                    .Where(cl => cl != null);
                if ((states != null)
                    && (states.Contains(state))
                   )
                {
                    var tristate = TrackingGroupCache.GetActiveTrackingGroupsPlusMe(null)
                        .Where(t => t.GroupID.ToUpper() == "TRI-STATE");

                    if ((tristate != null)
                        && tristate.Any()
                       )
                    {
                        defaultTrackingGroup = tristate.First().OrderTrackingGroupKey;
                    }
                }
            }

            // if we didn't match on anything else, default to the out of state group
            if (!defaultTrackingGroup.HasValue
                || (defaultTrackingGroup <= 0)
               )
            {
                var outside = TrackingGroupCache.GetActiveTrackingGroupsPlusMe(null)
                    .Where(t => t.GroupID.ToUpper() == "OUTSIDE OF TRI-STATE");

                if ((outside != null)
                    && outside.Any()
                   )
                {
                    defaultTrackingGroup = outside.First().OrderTrackingGroupKey;
                }
            }

            return defaultTrackingGroup;
        }

        private List<OrderTrackingGroup> GetMatchingTrackingGroups(int? facilityBranchKey, string zipCode,
            string county, int? state)
        {
            var trackingGroups = TrackingGroupCache.GetActiveTrackingGroupsPlusMe(null)
                .Where(pa => (((facilityBranchKey.HasValue)
                               && (pa.FacilityBranchKey == facilityBranchKey)
                              )
                              || (!string.IsNullOrEmpty(zipCode)
                                  && pa.OrderTrackingGroupDetail.Any(d => d.ZipCode == zipCode)
                              )
                              || (!string.IsNullOrEmpty(county)
                                  && pa.OrderTrackingGroupDetail.Any(d => d.County == county)
                              )
                              || (state.HasValue
                                  && pa.OrderTrackingGroupDetail.Any(d => d.State == state)
                              )
                              && !pa.Inactive
                    )
                );
            return (trackingGroups == null) ? null : trackingGroups.ToList();
        }

        private int? SelectTrackingGroupFromList(List<OrderTrackingGroup> TrackingGroupList, int PhysicianKeyParm)
        {
            int? defaultTrackingGroup = null;

            if ((TrackingGroupList == null)
                || (TrackingGroupList.Any() == false)
               )
            {
                // if there's nothing in the list, return null
                defaultTrackingGroup = null;
            }
            else if ((TrackingGroupList != null)
                     && (TrackingGroupList.Count == 1)
                    )
            {
                // if there's only one row, return the tracking group for that row
                defaultTrackingGroup = TrackingGroupList.First().OrderTrackingGroupKey;
            }
            else if ((TrackingGroupList != null)
                     && (TrackingGroupList.Count > 1)
                    )
            {
                var physWithTrackingGroups = PhysicianCache.Current.GetPhysicianAddressesWithTrackingGroups();
                // if there's more than one row, check to see if there are any that have not been assigned to a physician address.
                // if there are, order them by the group id and select the first row
                var notUsed = TrackingGroupList.Where(t =>
                    !physWithTrackingGroups.Any(p => (p.TrackingGroup == t.OrderTrackingGroupKey)));

                if ((notUsed != null)
                    && notUsed.Any()
                   )
                {
                    defaultTrackingGroup = notUsed.OrderBy(t => t.GroupID).First().OrderTrackingGroupKey;
                }

                // if there's more than one and all of the groups have been assigned to a physician, select the row that has been 
                // assigned to the least amount of physician addresses
                if (!defaultTrackingGroup.HasValue
                    || (defaultTrackingGroup <= 0)
                   )
                {
                    var phyAddr = physWithTrackingGroups
                        .Where(pa =>
                            (TrackingGroupList.Select(t => t.OrderTrackingGroupKey)
                                .Contains((pa.TrackingGroup.HasValue) ? pa.TrackingGroup.Value : 0))
                        )
                        .GroupBy(p => p.TrackingGroup)
                        .Select(g => new { TrackingGroup = g.Key, Count = g.Count() })
                        .OrderBy(c => c.Count)
                        .FirstOrDefault();

                    if (phyAddr != null)
                    {
                        defaultTrackingGroup = phyAddr.TrackingGroup;
                    }
                }
            }

            return defaultTrackingGroup;
        }
    }
}