using System;
using System.Linq;
using System.Collections.Generic;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Services.Authentication;

namespace Virtuoso.Core.Controls
{

    public class InsuranceSmartSearch<TEntity> : ISmartSearch<TEntity>
    {
        public List<TEntity> SmartSearchData { get; set; }
        public List<TEntity> SmartSearchDataMRU { get; set; }

        public InsuranceSmartSearch()
        {
            LoadSmartSearchData(false, (int?)null);
        }

        public void LoadSmartSearchData(bool includeEmpty, object selectedValue)
        {
            SmartSearchData = InsuranceCache.GetActiveInsurances((int?)selectedValue) as List<TEntity>;
        }

        public void SaveToMRU(TEntity MRUContents)
        {
            throw new System.NotImplementedException();
        }
                    
    }

    public class EmployerSmartSearch<TEntity> : ISmartSearch<TEntity>
    {
        public List<TEntity> SmartSearchData { get; set; }
        public List<TEntity> SmartSearchDataMRU { get; set; }

        public EmployerSmartSearch()
        {
            LoadSmartSearchData(false, (int?)null);
        }

        public void LoadSmartSearchData(bool includeEmpty, object selectedValue)
        {
            SmartSearchData = EmployerCache.GetActiveEmployersPlusMe((int?)selectedValue, includeEmpty) as List<TEntity>;
            //SmartSearchDataMRU = EmployerCache.GetActiveEmployers(includeEmpty).GetRange(2, 2) as List<TEntity>;          
        }

        public void SaveToMRU(TEntity MRUContents)
        {
            throw new System.NotImplementedException();
        }

    }

    public class FacilitySmartSearch<TEntity> : ISmartSearch<TEntity>
    {
        public void SaveToMRU(TEntity MRUContents)
        {
            throw new System.NotImplementedException();
        }

        public List<TEntity> SmartSearchData { get; set; }
        public List<TEntity> SmartSearchDataMRU { get; set; }

        public FacilitySmartSearch()
        {
            LoadSmartSearchData(false, (int?)null);
        }

        public void LoadSmartSearchData(bool includeEmpty, object selectedValue)
        {
            SmartSearchData = FacilityCache.GetActiveFacilitiesPlusMe((int?)selectedValue, includeEmpty) as List<TEntity>;
            //SmartSearchDataMRU = FacilityCache.GetActiveFacilities(includeEmpty).GetRange(3, 4) as List<TEntity>;
        }
        

    }

    public class PhysicianSmartSearch<TEntity> : ISmartSearch<TEntity>
    {


        public List<TEntity> SmartSearchData { get; set; }
        public List<TEntity> SmartSearchDataMRU { get; set; }

        public PhysicianSmartSearch()
        {
            LoadSmartSearchData(false, (int?)null);
        }
        public PhysicianSmartSearch(bool includeEmpty, object selectedValue)
        {
            LoadSmartSearchData(includeEmpty, (int?)selectedValue);
        }

        public void LoadSmartSearchData(bool includeEmpty, object selectedValue)
        {
            //The primary reason that this is necessary is because there is not a way to determine this programattically
            SmartSearchData = PhysicianCache.Current.GetActivePhysiciansPlusMe((int?)selectedValue, includeEmpty) as List<TEntity>;
            //SmartSearchDataMRU = PhysicianCache.GetActivePhysicians(includeEmpty).GetRange(3, 3) as List<TEntity>;
        }

        public void SaveToMRU(TEntity MRUContents)
        {
            throw new System.NotImplementedException();
        }

    }

    public class CensusTractSmartSearch<TEntity> : ISmartSearch<TEntity>
    {


        public List<TEntity> SmartSearchData { get; set; }
        public List<TEntity> SmartSearchDataMRU { get; set; }

        public CensusTractSmartSearch()
        {
            LoadSmartSearchData(false, (int?)null);
        }
        public CensusTractSmartSearch(bool includeEmpty, object selectedValue)
        {
            LoadSmartSearchData(includeEmpty, (int?)selectedValue);
        }

        public void LoadSmartSearchData(bool includeEmpty, object selectedValue)
        {
            //The primary reason that this is necessary is because there is not a way to determine this programattically
            SmartSearchData = CensusTractCache.GetCensusTracts(includeEmpty) as List<TEntity>;
            //SmartSearchDataMRU = CensusTractCache.GetActiveCensusTracts(includeEmpty).GetRange(3, 3) as List<TEntity>;
        }

        public void SaveToMRU(TEntity MRUContents)
        {
            throw new System.NotImplementedException();
        }

    }

    public class OrderTrackingGroupSmartSearch<TEntity> : ISmartSearch<TEntity>
    {
        public List<TEntity> SmartSearchData { get; set; }
        public List<TEntity> SmartSearchDataMRU { get; set; }

        public OrderTrackingGroupSmartSearch()
        {
            LoadSmartSearchData(false, (int?)null);
        }
        public OrderTrackingGroupSmartSearch(bool includeEmpty, object selectedValue)
        {
            LoadSmartSearchData(includeEmpty, (int?)selectedValue);
        }

        public void LoadSmartSearchData(bool includeEmpty, object selectedValue)
        {
            //SmartSearchData = TrackingGroupCache.GetActiveTrackingGroupsForFaciltyBranch((int?)selectedValue) as List<TEntity>;
            SmartSearchData = TrackingGroupCache.GetActiveTrackingGroupsPlusMe(null) as List<TEntity>;
        }

        public void SaveToMRU(TEntity MRUContents)
        {
            throw new System.NotImplementedException();
        }

    }

    public class UserProfileSmartSearch<TEntity> : ISmartSearch<TEntity>
    {
        public List<TEntity> SmartSearchData { get; set; }
        public List<TEntity> SmartSearchDataMRU { get; set; }

        public UserProfileSmartSearch()
        {
            LoadSmartSearchData(false, (Guid?)null);
        }
        public UserProfileSmartSearch(bool includeEmpty, object selectedValue)
        {
            LoadSmartSearchData(includeEmpty, (Guid?)selectedValue);
        }

        public void LoadSmartSearchData(bool includeEmpty, object selectedValue)
        {
            //The primary reason that this is necessary is because there is not a way to determine this programattically
            var query = UserCache.Current.GetActiveUsersPlusMe((Guid?)selectedValue, includeEmpty).ToList();

            //if not a DeltaAdmin or DeltaUser, filter out DeltaAdmin and DeltaUser users
            if (!(WebContext.Current.User.DeltaAdmin || WebContext.Current.User.DeltaUser))
                query = query.Where(p => (p.DeltaAdmin == false && p.DeltaUser == false) || p.UserId == ((Guid?)selectedValue).GetValueOrDefault()).ToList();

            var qry = query as List<TEntity>;
            SmartSearchData = qry;
        }

        public void SaveToMRU(TEntity MRUContents)
        {
            throw new System.NotImplementedException();
        }

    }

    public class UserProfileWithServiceLineSearch<TEntity> : ISmartSearch<TEntity>
    {
        private int? ServiceLineKey { get; set; }
        public List<TEntity> SmartSearchData { get; set; }
        public List<TEntity> SmartSearchDataMRU { get; set; }

        public UserProfileWithServiceLineSearch()
        {
            LoadSmartSearchData(false, (Guid?)null);
        }
        public UserProfileWithServiceLineSearch(bool includeEmpty, object selectedValue, int? serviceLineKey)
        {
            ServiceLineKey = serviceLineKey;
            LoadSmartSearchData(includeEmpty, (Guid?)selectedValue);
        }

        public void LoadSmartSearchData(bool includeEmpty, object selectedValue)
        {
            //The primary reason that this is necessary is because there is not a way to determine this programattically
            var query = UserCache.Current.GetActiveUsersPlusMe((Guid?)selectedValue, includeEmpty).ToList();

            //Note: for care coordinator smart combo - first time in will be NULL, then second time ServiceLineKey will be ZERO - which will disallow any users in smart combo drop down
            if (ServiceLineKey.HasValue)
            {
                Nullable<Guid> selectedGUID = ((Guid?)selectedValue);
                //FYI - if ServiceLineKey.HasValue - only return users which have access to that service line.  NOTE: ServiceLineKey.Value may be 0 - in which case, no users will be found
                query = query.Where
                    (up => 
                        (selectedGUID != null && up.UserId == selectedGUID.GetValueOrDefault()) 
                        || up.MyUserProfileServiceLines.Where(upsl => upsl.MyServiceLineKey == ServiceLineKey.Value).Count() > 0
                        || ServiceLineCache.GetServiceLineKeysFromServiceLineGroupingKeys(up.ServiceLineGroupIdsUserCanSee).Contains(ServiceLineKey.Value)
                    )
                    .ToList();

            }

            //if not a DeltaAdmin or DeltaUser, filter out DeltaAdmin and DeltaUser users
            if (!(WebContext.Current.User.DeltaAdmin || WebContext.Current.User.DeltaUser))
                query = query.Where(p => (p.DeltaAdmin == false && p.DeltaUser == false) || p.UserId == ((Guid?)selectedValue).GetValueOrDefault()).ToList();

            var qry = query as List<TEntity>;
            SmartSearchData = qry;
        }

        public void SaveToMRU(TEntity MRUContents)
        {
            throw new System.NotImplementedException();
        }

    }

    public class SupplySmartSearch<TEntity> : ISmartSearch<TEntity>
    {
        public void SaveToMRU(TEntity MRUContents)
        {
            throw new System.NotImplementedException();
        }

        public List<TEntity> SmartSearchData { get; set; }
        public List<TEntity> SmartSearchDataMRU { get; set; }

        public SupplySmartSearch()
        {
            LoadSmartSearchData(false, (int?)null);
        }

        public void LoadSmartSearchData(bool includeEmpty, object selectedValue)
        {
            SmartSearchData = SupplyCache.GetSupplies() as List<TEntity>;
        }
    }
}
