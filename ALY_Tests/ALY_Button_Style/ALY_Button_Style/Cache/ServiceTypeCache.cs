#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Services;
using Virtuoso.Helpers;
using Virtuoso.Portable;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.ServiceType)]
    [Export(typeof(ICache))]
    public class ServiceTypeCache : ReferenceCacheBase<ServiceType>
    {
        public static ServiceTypeCache Current { get; private set; }
        public VirtuosoDomainContext CacheContext => Context;

        [ImportingConstructor]
        public ServiceTypeCache(ILogger logManager)
            : base(logManager, ReferenceTableName.ServiceType, "016")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("ServiceTypeCache already initialized.");
            }

            Current = this;
        }

        protected override EntitySet EntitySet => Context.ServiceTypes;

        protected override EntityQuery<ServiceType> GetEntityQuery()
        {
            return Context.GetServiceTypeQuery();
        }

        protected override void OnRIACacheLoaded()
        {
            BuildSearchDataStructures();
        }

        protected override void OnCacheSaved()
        {
            BuildSearchDataStructures();
        }

        ServiceType[] SortedServiceTypeArray;
        Dictionary<int, ServiceType> ServiceTypeDictionary = new Dictionary<int, ServiceType>();
        ILookup<int, ServiceType> ServiceTypeLookup;

        private void BuildSearchDataStructures()
        {
            SortedServiceTypeArray = Context.ServiceTypes.OrderBy(fsq => fsq.ServiceTypeKey).ToArray();

            ServiceTypeDictionary = Context.ServiceTypes.ToDictionary(fsq => fsq.ServiceTypeKey);

            //https://stackoverflow.com/questions/38098928/ilookup-vs-dictionary?noredirect=1&lq=1
            //The main difference is in the implementation: the current default Lookup (created with ToLookup()) implementation of the item indexer [] will 
            //return an empty enumeration if the key is not found, where a dictionary will throw an exception. Of course since ILookup is an interface, 
            //it's up to the implementation how this is handled. 
            ServiceTypeLookup = Context.ServiceTypes.ToLookup(fsq => fsq.ServiceTypeKey);
        }


        public static List<ServiceType> GetServiceTypes()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return null;
            }

            return Current.Context.ServiceTypes.OrderBy(p => p.Code).ToList();
        }

        public static List<ServiceType> GetActiveServiceTypes()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return null;
            }

            return Current.Context.ServiceTypes.Where(p => p.Inactive == false).OrderBy(p => p.Code).ToList();
        }

        public static List<ServiceType> GetActiveServiceTypesPlusMe(int? serviceTypekey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return null;
            }

            return Current.Context.ServiceTypes
                .Where(p => ((p.Inactive == false) || (p.ServiceTypeKey == serviceTypekey))).OrderBy(p => p.Description)
                .ToList();
        }

        public static List<ServiceType> GetActiveTeleMonitoringVisitServiceTypesPlusMe(int? serviceTypekey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.UserProfiles == null))
            {
                return null;
            }

            List<ServiceType> stList = GetActiveServiceTypesPlusMe(serviceTypekey);
            if (stList == null)
            {
                return null;
            }

            List<ServiceType> retList = new List<ServiceType>();
            foreach (ServiceType st in stList)
            {
                if (st.FormKey == null)
                {
                    continue;
                }

                Discipline d = DisciplineCache.GetDisciplineFromKey(st.DisciplineKey);
                if (d == null)
                {
                    continue;
                }

                if (d.TeleMonitorDiscipline == false)
                {
                    continue;
                }

                if (DynamicFormCache.IsVisitTeleMonitoring((int)st.FormKey) == false)
                {
                    continue;
                }

                retList.Add(st);
            }

            return retList;
        }

        public static List<ServiceType> GetServiceTypesFilterByLikeForm(int servicetypekey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return null;
            }

            var formkey = GetFormKey(servicetypekey);
            var disciplinekey = GetDisciplineKey(servicetypekey);
            var ret = Current.Context.ServiceTypes
                .Where(p => ((p.Inactive == false) || (p.ServiceTypeKey == servicetypekey)) && p.FormKey == formkey &&
                            p.DisciplineKey == disciplinekey).OrderBy(p => p.Code).ToList();
            return ret;
        }

        public static ServiceType GetPOCServiceTypeForDiscipline(int disciplineKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return null;
            }

            List<ServiceType> stList = Current.Context.ServiceTypes
                .Where(p => (p.Inactive == false) && p.IsPlanOfCare && (p.DisciplineKey == disciplineKey)).ToList();

            if (stList.Count != 1)
            {
                return null;
            }

            return stList.FirstOrDefault();
        }

        public static List<ServiceType> GetServiceTypesFilterByLikeFormAndAssistant(int servicetypekey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return null;
            }

            var up = UserCache.Current.GetCurrentUserProfile();
            var formkey = GetFormKey(servicetypekey);
            var disciplinekey = GetDisciplineKey(servicetypekey);
            var ret = Current.Context.ServiceTypes
                .Where(p => !p.FinancialUseOnly)
                .Where(p => ((p.Inactive == false) || (p.ServiceTypeKey == servicetypekey))
                            && p.FormKey == formkey && p.DisciplineKey == disciplinekey &&
                            (TaskSchedulingHelper.UserCanPerformServiceType(p, up) ||
                             p.ServiceTypeKey == servicetypekey))
                .OrderBy(p => p.Code).ToList();
            return ret;
        }

        public static string GetDescriptionFromKey(int key)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return String.Empty;
            }

            ServiceType st = Current.Context.ServiceTypes.FirstOrDefault(p => p.ServiceTypeKey == key);
            return ((st == null) || (string.IsNullOrWhiteSpace(st.Description)))
                ? null
                : ((st.Description.ToLower().Trim() == "attempted") ? "Attempted Visit" : st.Description);
        }

        public static string GetDescriptionFromKeyWithOasisOverride(int key)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return String.Empty;
            }

            ServiceType st = Current.Context.ServiceTypes.FirstOrDefault(p => p.ServiceTypeKey == key);
            if (st == null)
            {
                return null;
            }

            if (st.IsOasis)
            {
                return "OASIS";
            }

            if (st.IsHIS)
            {
                return st.Description;
            }

            return st.Description;
        }

        public static int? GetDisciplineKey(int serviceTypeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return null;
            }

            ServiceType st = GetServiceTypeFromKey(serviceTypeKey);
            return st?.DisciplineKey;
        }

        public static bool AllowAfterDischarge(int serviceTypeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return false;
            }

            ServiceType st = GetServiceTypeFromKey(serviceTypeKey);
            return st?.AllowAfterDischarge ?? false;
        }

        public static ServiceType GetServiceTypeFromKey(int serviceTypeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return null;
            }

            ServiceType st = null;
            Current.ServiceTypeDictionary.TryGetValue(serviceTypeKey, out st);
            return st;
        }

        public static ServiceType GetAttemptedServiceType()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return null;
            }

            ServiceType st = Current.Context.ServiceTypes.FirstOrDefault(p => p.IsAttempted);
            return st;
        }

        public static int? GetFormKey(int serviceTypeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return null;
            }

            ServiceType st = GetServiceTypeFromKey(serviceTypeKey);
            return st?.FormKey;
        }

        public static string GetHCFACodeFromKey(int serviceTypeKey)
        {
            return DisciplineCache.GetHCFACodeFromKey(GetDisciplineKey(serviceTypeKey).Value);
        }

        public static bool GetIsDisciplineTherapyFromKey(int serviceTypeKey)
        {
            return DisciplineCache.GetIsDisciplineTherapyFromKey(GetDisciplineKey(serviceTypeKey).Value);
        }

        public static bool IsBillable(int key)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypes == null))
            {
                return true;
            }

            ServiceType st = Current.Context.ServiceTypes.FirstOrDefault(p => p.ServiceTypeKey == key);
            return !st?.NonBillable ?? true;
        }
    }
}