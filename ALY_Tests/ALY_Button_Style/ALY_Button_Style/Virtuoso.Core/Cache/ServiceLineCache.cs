#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ria.Sync;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.ServiceLine)]
    [Export(typeof(ICache))]
    public class ServiceLineCache : ReferenceCacheBase<ServiceLine>
    {
        public static ServiceLineCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.ServiceLines;

        public VirtuosoDomainContext CacheContext => Context;

        [ImportingConstructor]
        public ServiceLineCache(ILogger logManager)
            : base(logManager, ReferenceTableName.ServiceLine, "023")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("ServiceLineCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.ServiceLine;
            Ticks = 0;
            Context = new VirtuosoDomainContext();
        }

        protected override EntityQuery<ServiceLine> GetEntityQuery()
        {
            return Context.GetServiceLineQuery();
        }

        private Dictionary<int, List<ServiceLine>> ____GetActiveUserServiceLinePlusMeCache;

        public Dictionary<int, List<ServiceLine>> GetActiveUserServiceLinePlusMeCache
        {
            get
            {
                if (____GetActiveUserServiceLinePlusMeCache == null)
                {
                    ____GetActiveUserServiceLinePlusMeCache = new Dictionary<int, List<ServiceLine>>();
                }

                return ____GetActiveUserServiceLinePlusMeCache;
            }
        }

        public override void PreReload()
        {
            // Clear any cached data
            if (GetActiveUserServiceLinePlusMeCache != null)
            {
                GetActiveUserServiceLinePlusMeCache.Clear();
            }
        }

        public static List<ServiceLine> GetServiceLines(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            var ret = Current.Context.ServiceLines.OrderBy(s => s.Name).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new ServiceLine { ServiceLineKey = 0, Name = " " });
            }

            return ret;
        }

        public static List<int> GetServiceLineKeysFromServiceLineGroupingKeys(List<int> serviceLineGroupingKeys)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return new List<int>();
            }

            List<ServiceLineGrouping> groups = Current.Context.ServiceLineGroupings
                .Where(slg => serviceLineGroupingKeys.Contains(slg.ServiceLineGroupingKey)).ToList();
            List<int> ret = groups.Select(g => g.ServiceLineKey).Distinct().ToList();
            return ret;
        }

        public static List<ServiceLine> GetActiveUserServiceLinePlusMe(int? serviceLineKey, bool includeEmpty = false)
        {
            List<ServiceLine> slList = new List<ServiceLine>();
            Current?.EnsureCacheReady();
            if ((Current != null) && (Current.Context != null) && (Current.Context.ServiceLines != null))
            {
                slList = GetActiveUserServiceLinePlusMeFromCache(serviceLineKey, includeEmpty);
                if (slList == null)
                {
                    slList = GetActiveUserServiceLinePlusMeInternal(serviceLineKey, includeEmpty);

                    var key = serviceLineKey.GetValueOrDefault();
                    Current.GetActiveUserServiceLinePlusMeCache[key] = slList;
                    if (includeEmpty)
                    {
                        IncludeEmptyServiceLine(slList);
                    }
                }
                else
                {
                    //Found in cache - optionally add empty option
                    if (includeEmpty)
                    {
                        IncludeEmptyServiceLine(slList);
                    }
                }
            }

            return slList;
        }

        private static List<ServiceLine> IncludeEmptyServiceLine(List<ServiceLine> slList)
        {
            if (slList != null && slList.Any())
            {
                if (slList.First().ServiceLineKey != 0)
                {
                    slList.Insert(0, new ServiceLine { ServiceLineKey = 0, Name = " " });
                }
            }
            else
            {
                slList = new List<ServiceLine>();
                slList.Insert(0, new ServiceLine { ServiceLineKey = 0, Name = " " });
            }

            return slList;
        }

        private static List<ServiceLine> GetActiveUserServiceLinePlusMeFromCache(int? serviceLineKey, bool includeEmpty)
        {
            int key = serviceLineKey.GetValueOrDefault();

            if (Current.GetActiveUserServiceLinePlusMeCache.ContainsKey(key))
            {
                var slList = Current.GetActiveUserServiceLinePlusMeCache[key];
                return slList;
            }

            return null;
        }

        private static List<ServiceLine> GetActiveUserServiceLinePlusMeInternal(int? serviceLineKey, bool includeEmpty)
        {
            Current?.EnsureCacheReady();
            List<ServiceLine> slList = new List<ServiceLine>();
            int slKey = (serviceLineKey == null) ? 0 : (int)serviceLineKey;
            UserProfile up = UserCache.Current.GetCurrentUserProfile();

            if (up == null || up.UserProfileServiceLine == null)
            {
                slList = Current.Context.ServiceLines.Where(s => (s.ServiceLineKey == slKey)).OrderBy(s => s.Name)
                    .ToList();
            }
            else
            {
                var today = DateTime.Today;
                List<ServiceLine> slListAll = Current.Context.ServiceLines
                    .Where(s => (s.Inactive == false) || (s.ServiceLineKey == slKey))
                    .OrderBy(s => s.Name)
                    .ToList();
                if (slListAll != null)
                {
                    // Return the subset of ServiceLines attached to this user.
                    foreach (ServiceLine sl in slListAll)
                    {
                        if (sl.ServiceLineKey == slKey)
                        {
                            slList.Add(sl);
                        }
                        else
                        {
                            var upsl = up.UserProfileServiceLine
                                .Where(w => w.ServiceLineKey == sl.ServiceLineKey && w.UserHasRights && w.IsEffectiveOnDate(today))
                                .FirstOrDefault();
                            if (upsl != null)

                            {
                                var x = GetServiceLineFromKey(upsl.ServiceLineKey);
                                slList.Add(x);
                            }
                            else
                            {
                                var Keys = up.ServiceLineGroupIdsUserCanSee;
                                var upslgsl = Current.Context.ServiceLineGroupings
                                    .Where(w => Keys.Contains(w.ServiceLineGroupingKey) && !w.Inactive)
                                    .Select(s => s.ServiceLine)
                                    .Distinct();
                                foreach (var sls in upslgsl)
                                {
                                    if (sls.ServiceLineKey == sl.ServiceLineKey)
                                    {
                                        slList.Add(sls);
                                        break;
                                    }
                                }
                            }
                        }
                    } // foreach (ServiceLine sl in slListAll)
                }
            }

            return slList;
        }

        public static List<ServiceLine> GetActiveServiceLinesPlusMe(int? serviceLineKey, bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            List<ServiceLine> slList = new List<ServiceLine>();
            int slKey = (serviceLineKey == null) ? 0 : (int)serviceLineKey;
            slList = Current.Context.ServiceLines.Where(s => (s.Inactive == false) || (s.ServiceLineKey == slKey))
                .OrderBy(s => s.Name).ToList();
            if (includeEmpty)
            {
                slList.Insert(0, new ServiceLine { ServiceLineKey = 0, Name = " " });
            }

            return slList;
        }

        public static List<ServiceLineGrouping> GetActiveAndPermissedServiceLineForCurrentUser(
            bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null)
                || (Current.Context == null)
                || (Current.Context.ServiceLines == null)
               )
            {
                return null;
            }

            UserProfile up = UserCache.Current.GetCurrentUserProfile();

            var ids = up.ServiceLineGroupIdsUserCanSee;

            List<ServiceLineGrouping> slgListAll = Current.Context.ServiceLineGroupings
                .Where(s => ids.Contains(s.MyServiceLineGroupingKey)).OrderBy(s => s.Name).ToList();

            if (includeEmpty)
            {
                slgListAll.Insert(0, new ServiceLineGrouping { ServiceLineGroupingKey = 0, Name = " " });
            }

            return slgListAll;
        }

        public static List<ServiceLineGrouping> GetActiveUserServiceLineGroupingForServiceLinePlusMe(
            int? serviceLineKey, int? serviceLineGroupingKey, bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null)
                || (Current.Context == null)
                || (Current.Context.ServiceLines == null)
               )
            {
                return null;
            }

            List<ServiceLineGrouping> slgList = new List<ServiceLineGrouping>();
            int slKey = serviceLineKey ?? 0;
            int slgKey = serviceLineGroupingKey ?? 0;
            UserProfile up = UserCache.Current.GetCurrentUserProfile();
            if ((up == null) || (up.UserProfileServiceLine == null))
            {
                slgList = Current.Context.ServiceLineGroupings
                    .Where(s => (s.ServiceLineGroupingKey == slgKey) && (serviceLineKey == slKey)).OrderBy(s => s.Name)
                    .ToList();
            }
            else
            {
                List<ServiceLineGrouping> slgListAll = Current.Context.ServiceLineGroupings
                    .Where(s => (((s.Inactive == false) || (s.ServiceLineGroupingKey == slgKey)) &&
                                 (s.ServiceLineKey == slKey))).OrderBy(s => s.Name).ToList();
                if (slgListAll != null)
                {
                    var ids = up.ServiceLineGroupIdsUserCanSee;
                    slgList = slgListAll.Where(w =>
                        (w.MyServiceLineGroupingKey == slgKey || ids.Contains(w.MyServiceLineGroupingKey)) &&
                        w.MyServiceLineKey == slKey).ToList();
                }
            }

            if (includeEmpty)
            {
                slgList.Insert(0, new ServiceLineGrouping { ServiceLineGroupingKey = 0, Name = " " });
            }

            return slgList;
        }

        public static string GetNameFromServiceLineKey(int? ServiceLineKey)
        {
            Current?.EnsureCacheReady();
            string name = null;

            if ((Current == null)
                || (Current.Context == null)
                || (Current.Context.ServiceLines == null)
               )
            {
                return null;
            }

            ServiceLine serviceLine = Current.Context.ServiceLines.FirstOrDefault(sl => sl.ServiceLineKey == ServiceLineKey);

            if (serviceLine != null)
            {
                name = serviceLine.Name;
            }

            return name;
        }

        public static string GetConsentTextFromServiceLineKey(int? ServiceLineKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            ServiceLine serviceLine = Current.Context.ServiceLines.FirstOrDefault(sl => sl.ServiceLineKey == ServiceLineKey);
            if (serviceLine == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(serviceLine.ConsentText))
            {
                return null;
            }

            return serviceLine.ConsentText;
        }

        public static bool GetIncludeWitnessSignatureFromServiceLineKey(int? ServiceLineKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return false;
            }

            ServiceLine serviceLine = Current.Context.ServiceLines.FirstOrDefault(sl => sl.ServiceLineKey == ServiceLineKey);
            if (serviceLine == null)
            {
                return false;
            }

            return serviceLine.IncludeWitnessSignature;
        }

        public static string GetReleaseInformationQuestionTextFromServiceLineKey(int? ServiceLineKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            ServiceLine serviceLine = Current.Context.ServiceLines.FirstOrDefault(sl => sl.ServiceLineKey == ServiceLineKey);
            if (serviceLine == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(serviceLine.ReleaseInformationQuestionText))
            {
                return null;
            }

            return serviceLine.ReleaseInformationQuestionText;
        }

        public static string GetAdvancedDirectivesTextFromServiceLineKey(int? ServiceLineKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            ServiceLine serviceLine = Current.Context.ServiceLines.FirstOrDefault(sl => sl.ServiceLineKey == ServiceLineKey);
            if (serviceLine == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(serviceLine.AdvancedDirectivesText))
            {
                return null;
            }

            return serviceLine.AdvancedDirectivesText;
        }

        public static string GetCoveredByTextFromServiceLineKey(int? ServiceLineKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            ServiceLine serviceLine = Current.Context.ServiceLines.FirstOrDefault(sl => sl.ServiceLineKey == ServiceLineKey);
            if (serviceLine == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(serviceLine.CoveredByText))
            {
                return null;
            }

            return serviceLine.CoveredByText;
        }

        public static string GetLiabilityTextFromServiceLineKey(int? ServiceLineKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            ServiceLine serviceLine = Current.Context.ServiceLines.FirstOrDefault(sl => sl.ServiceLineKey == ServiceLineKey);
            if (serviceLine == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(serviceLine.LiabilityText))
            {
                return null;
            }

            return serviceLine.LiabilityText;
        }

        public bool ServiceLineHospicePreEvalRequired(int? ServiceLineKey)
        {
            ServiceLine sl = GetServiceLineFromKey(ServiceLineKey);
            if (sl == null)
            {
                return TenantSettingsCache.Current.TenantSettingHospicePreEvalRequired;
            }

            return (sl.NonHospicePreEvalRequired == false)
                ? TenantSettingsCache.Current.TenantSettingHospicePreEvalRequired
                : sl.NonHospicePreEvalRequired;
        }

        public bool ServiceLineNonHospicePreEvalRequired(int? ServiceLineKey)
        {
            ServiceLine sl = GetServiceLineFromKey(ServiceLineKey);
            if (sl == null)
            {
                return TenantSettingsCache.Current.TenantSettingNonHospicePreEvalRequired;
            }

            return (sl.NonHospicePreEvalRequired == false)
                ?
                // sll 
                // MethodOfPay Visibility tests was failing becasue TenantSettingsCache.Current was null and it was causing a fault when
                // referencing TenantSettingsCache.Current.TenantSettingNonHospicePreEvalRequired.  it will now return false if
                // TenantSettingsCache.Current.TenantSettingNonHospicePreEvalRequired is null.
                ((TenantSettingsCache.Current != null) && TenantSettingsCache.Current.TenantSettingNonHospicePreEvalRequired)
                : sl.NonHospicePreEvalRequired;
        }

        public static List<ServiceLineGrouping> GetAllActiveUserServiceLineGroupingPlusMe(int? ServiceLineGroupingKey,
            bool includeEmpty = false, UserProfile userProfile = null)
        {
            Current?.EnsureCacheReady();
            if ((Current == null)
                || (Current.Context == null)
                || (Current.Context.ServiceLines == null)
               )
            {
                return null;
            }

            List<ServiceLineGrouping> slgList = new List<ServiceLineGrouping>();
            UserProfile up = (userProfile == null) ? UserCache.Current.GetCurrentUserProfile() : userProfile;
            if ((up != null) && (up.UserProfileServiceLine != null))
            {
                List<ServiceLineGrouping> slgListAll = Current.Context.ServiceLineGroupings
                    .Where(s => s.Inactive == false).OrderBy(s => s.Name).ToList();
                if (slgListAll != null)
                {
                    // return the subset of ServiceLines attached to this user.
                    var ids = up.ServiceLineGroupIdsUserCanSee;
                    foreach (ServiceLineGrouping slg in slgListAll)
                        if (slg.ServiceLineGroupingKey == ServiceLineGroupingKey)
                        {
                            slgList.Add(slg);
                        }
                        else if (ids.Contains(slg.MyServiceLineGroupingKey))
                        {
                            slgList.Add(slg);
                        }
                }
            }

            if (includeEmpty)
            {
                slgList.Insert(0, new ServiceLineGrouping { ServiceLineGroupingKey = 0, Name = " " });
            }

            return slgList;
        }

        public static List<ServiceLineGrouping> GetAllUserServiceLineGroupingPlusMe(int? ServiceLineGroupingKey,
            bool includeEmpty = false, UserProfile userProfile = null)
        {
            Current?.EnsureCacheReady();
            if ((Current == null)
                || (Current.Context == null)
                || (Current.Context.ServiceLines == null)
               )
            {
                return null;
            }

            List<ServiceLineGrouping> slgList = new List<ServiceLineGrouping>();
            UserProfile up = (userProfile == null) ? UserCache.Current.GetCurrentUserProfile() : userProfile;
            if ((up != null) && (up.UserProfileServiceLine != null))
            {
                List<ServiceLineGrouping> slgListAll =
                    Current.Context.ServiceLineGroupings.OrderBy(s => s.Name).ToList();
                if (slgListAll != null)
                {
                    // Return the subset of ServiceLines attached to this user.
                    var ids = up.ServiceLineGroupIdsUserCanSee;
                    foreach (ServiceLineGrouping slg in slgListAll)
                        if (slg.ServiceLineGroupingKey == ServiceLineGroupingKey)
                        {
                            slgList.Add(slg);
                        }
                        else if (ids.Contains(slg.MyServiceLineGroupingKey))
                        {
                            slgList.Add(slg);
                        }
                }
            }

            if (includeEmpty)
            {
                slgList.Insert(0, new ServiceLineGrouping { ServiceLineGroupingKey = 0, Name = " " });
            }

            return slgList;
        }

        public static List<ServiceLine> GetActiveServiceLines(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            var ret = Current.Context.ServiceLines.Where(s => s.Inactive == false).OrderBy(s => s.Name).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new ServiceLine { ServiceLineKey = 0, Name = " " });
            }

            return ret;
        }

        public static async Task<List<ServiceLine>> GetActiveServiceLinesWithNewContext(bool includeEmpty = false)
        {
            // Get all service lines with a new context for each user. caurni00 - 1/19/15 - 17872
            var newContext = new VirtuosoDomainContext();

            var cache = await RIACacheManager.Initialize(
                Path.Combine(Current.ApplicationStore, Constants.REFERENCE_DATA_STORE_FOLDER),
                Current.CacheName,
                Constants.ENTITY_TYPENAME_FORMAT,
                true); //NOTE: can throw DirectoryNotFoundException

            // Load the data into newContext via the cache
            await cache.Load(newContext);

            if (newContext.ServiceLines == null)
            {
                return null;
            }

            var ret = newContext.ServiceLines.Where(s => s.Inactive == false).OrderBy(s => s.Name).ToList();

            if (includeEmpty)
            {
                ret.Insert(0, new ServiceLine { ServiceLineKey = 0, Name = " " });
            }

            return ret;
        }

        public static List<ServiceLineGrouping> GetActiveServiceLineGroupings(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLineGroupings == null))
            {
                return null;
            }

            var ret = Current.Context.ServiceLineGroupings.Where(s => s.Inactive == false).OrderBy(s => s.Name)
                .ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new ServiceLineGrouping { ServiceLineGroupingKey = 0, Name = " " });
            }

            return ret;
        }

        public static ServiceLine GetServiceLineFromKey(int? serviceLineKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            if (serviceLineKey == null)
            {
                return null;
            }

            ServiceLine sl =
                (from s in Current.Context.ServiceLines.AsQueryable()
                 where (s.ServiceLineKey == serviceLineKey)
                 select s).FirstOrDefault();
            if ((sl == null) && (serviceLineKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error ServiceLineCache.GetServiceLineFromKey: ServiceLineKey {0} is not defined.  Contact your system administrator.",
                    serviceLineKey.ToString()));
            }

            return sl;
        }

        public static ServiceLineGrouping GetServiceLineGroupingFromKey(int? serviceLineGroupingKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            if (serviceLineGroupingKey == null)
            {
                return null;
            }

            ServiceLineGrouping sl =
                (from s in Current.Context.ServiceLineGroupings.AsQueryable()
                 where (s.ServiceLineGroupingKey == serviceLineGroupingKey)
                 select s).FirstOrDefault();
            if ((sl == null) && (serviceLineGroupingKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error ServiceLineCache.GetServiceLineGroupingFromKey: ServiceLineGroupingKey {0} is not defined.  Contact your system administrator.",
                    serviceLineGroupingKey.ToString()));
            }

            return sl;
        }

        public static ServiceLineGroupHeader GetServiceLineGroupHeaderFromKey(int? serviceLineGroupHeaderKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            if (serviceLineGroupHeaderKey == null)
            {
                return null;
            }

            ServiceLineGroupHeader slgh = (from s in Current.Context.ServiceLineGroupHeaders.AsQueryable()
                                           where (s.ServiceLineGroupHeaderKey == serviceLineGroupHeaderKey)
                                           select s).FirstOrDefault();
            if ((slgh == null) && (serviceLineGroupHeaderKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error ServiceLineCache.GetServiceLineGroupHeaderFromKey: ServiceLineGroupHeaderKey {0} is not defined.  Contact your system administrator.",
                    serviceLineGroupHeaderKey.ToString()));
            }

            return slgh;
        }

        public static int[] GetServiceTypeDependencyKeysFromServiceLineGroupingKeys(int[] currentgroups)
        {
            Current?.EnsureCacheReady();
            if ((currentgroups == null) || (currentgroups.Any() == false))
            {
                return null;
            }

            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceTypeGroupings == null))
            {
                return null;
            }

            int[] sl_Keys = (from s in Current.Context.ServiceTypeGroupings.AsQueryable()
                             where (((s.Inactive == false) && currentgroups.Contains(s.ServiceLineGroupingKey)))
                             select s.ServiceTypeKey).ToArray();
            if (sl_Keys.Any() == false)
            {
                return null;
            }

            return sl_Keys;
        }

        public static ServiceLineGroupHeader GetParentServiceLineGroupHeaderFromKey(int? serviceLineGroupHeaderKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            if (serviceLineGroupHeaderKey == null)
            {
                return null;
            }

            ServiceLineGroupHeader slgh = (from s in Current.Context.ServiceLineGroupHeaders.AsQueryable()
                                           where (s.ServiceLineGroupHeaderKey == serviceLineGroupHeaderKey)
                                           select s).FirstOrDefault();
            if ((slgh == null) && (serviceLineGroupHeaderKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error ServiceLineCache.GetParentServiceLineGroupHeaderFromKey: ServiceLineGroupHeaderKey {0} is not defined.  Contact your system administrator.",
                    serviceLineGroupHeaderKey.ToString()));
            }

            if (slgh == null)
            {
                return null;
            }

            if (slgh.SequenceNumber == 0)
            {
                return null;
            }

            ServiceLineGroupHeader slghp = (from s in Current.Context.ServiceLineGroupHeaders.AsQueryable()
                                            where ((s.ServiceLineKey == slgh.ServiceLineKey) && (s.SequenceNumber == slgh.SequenceNumber - 1))
                                            select s).FirstOrDefault();
            if (slghp == null)
            {
                MessageBox.Show(String.Format(
                    "Error ServiceLineCache.GetParentServiceLineGroupHeaderFromKey: Parent header for  ServiceLineGroupHeaderKey {0} is not defined.  Contact your system administrator.",
                    serviceLineGroupHeaderKey.ToString()));
            }

            return slghp;
        }

        public static string GetServiceLineGroupingDescriptionFromKey(int? serviceLineGroupingKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            ServiceLineGrouping slg = GetServiceLineGroupingFromKey(serviceLineGroupingKey);
            if (slg == null)
            {
                return null;
            }

            ServiceLine sl = GetServiceLineFromKey(slg.ServiceLineKey);
            if (sl == null)
            {
                return null;
            }

            String slghDesc = "";
            if (slg.ServiceLineGroupHeaderKey.HasValue)
            {
                slghDesc = GetServiceLineGroupHeaderDescriptionFromKey((int)slg.ServiceLineGroupHeaderKey);
            }

            string serviceLineGroupingLabel = ((slghDesc == null) || (slg.Name == null))
                ? slghDesc
                : (((slg.Name.ToLower().StartsWith(slghDesc.ToLower() + " ")) ||
                    (slg.Name.ToLower().EndsWith(" " + slghDesc.ToLower())) ||
                    (slg.Name.ToLower().Contains(" " + slghDesc.ToLower() + " ")))
                    ? null
                    : slghDesc + " ");
            return string.Format("{0} {1}{2}", sl.Name, serviceLineGroupingLabel, slg.Name);
        }

        public static string GetServiceLineGroupHeaderDescriptionFromKey(int? serviceLineGroupHeaderKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ServiceLines == null))
            {
                return null;
            }

            if (serviceLineGroupHeaderKey == null)
            {
                return null;
            }

            ServiceLineGroupHeader sl = (from s in Current.Context.ServiceLineGroupHeaders.AsQueryable()
                                         where (s.ServiceLineGroupHeaderKey == serviceLineGroupHeaderKey)
                                         select s).FirstOrDefault();
            if ((sl == null) && (serviceLineGroupHeaderKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error ServiceLineCache.GetServiceLineGroupHeaderDescriptionFromKey: ServiceLineGroupHeaderKey {0} is not defined.  Contact your system administrator.",
                    serviceLineGroupHeaderKey.ToString()));
            }

            return sl.GroupHeaderLabel;
        }

        public static bool IsActiveUserInServiceLineGrouping(int serviceLineGroupingKey)
        {
            Current?.EnsureCacheReady();
            if ((serviceLineGroupingKey <= 0) || (Current == null) || (Current.Context == null) ||
                (Current.Context.ServiceLines == null))
            {
                return false;
            }

            UserProfile up = UserCache.Current.GetCurrentUserProfile();
            if (up == null)
            {
                return false;
            }

            return up.IsOversiteOrCanVisitOrOwnerInHeirachy(serviceLineGroupingKey);
        }

        public static bool IsServiceLineGroupingInactive(int? serviceLineGroupingKey)
        {
            Current?.EnsureCacheReady();
            if ((serviceLineGroupingKey <= 0) || (Current == null) || (Current.Context == null) ||
                (Current.Context.ServiceLines == null))
            {
                return false;
            }

            var grouping = GetServiceLineGroupingFromKey(serviceLineGroupingKey);
            return grouping == null || grouping.Inactive;
        }

        public DateTime? GoLiveDateForAdmission(IServiceLineGroupingService admission, int serviceLineKey)
        {
            var grp1 = admission.CurrentGroup;
            var slgKey1 = grp1?.ServiceLineGroupingKey ?? 0;
            var grp2 = admission.CurrentGroup2;
            var slgKey2 = grp2?.ServiceLineGroupingKey ?? 0;
            var grp3 = admission.CurrentGroup3;
            var slgKey3 = grp3?.ServiceLineGroupingKey ?? 0;
            var grp4 = admission.CurrentGroup4;
            var slgKey4 = grp4?.ServiceLineGroupingKey ?? 0;
            var grp5 = admission.CurrentGroup5;
            var slgKey5 = grp5?.ServiceLineGroupingKey ?? 0;
            var goLiveDate =
                Current.GoLiveDateForServiceLine(serviceLineKey, slgKey1, slgKey2, slgKey3, slgKey4, slgKey5);
            return goLiveDate;
        }

        public DateTime? GoLiveDateForServiceLine(int? serviceLineKey, int slgKey0, int slgKey1, int slgKey2,
            int slgKey3, int slgKey4)
        {
            Current?.EnsureCacheReady();
            if ((serviceLineKey.GetValueOrDefault() <= 0) || (Current == null) || (Current.Context == null) ||
                (Current.Context.ServiceLines == null))
            {
                return null;
            }

            var sl = Current.Context.ServiceLines.FirstOrDefault(s => s.ServiceLineKey == serviceLineKey.GetValueOrDefault());
            if (sl == null)
            {
                return null;
            }

            if (sl.GoLiveLevel.HasValue)
            {
                // NOTE: GoLiveDate specified at the group level
                var lvl = sl.GoLiveLevel; //0 - 4
                switch (lvl)
                {
                    case 0:
                        return GetNthGoLiveDate(slgKey0);
                    case 1:
                        return GetNthGoLiveDate(slgKey1);
                    case 2:
                        return GetNthGoLiveDate(slgKey2);
                    case 3:
                        return GetNthGoLiveDate(slgKey3);
                    case 4:
                        return GetNthGoLiveDate(slgKey4);
                    default:
                        return null;
                }
            }

            // NOTE: GoLiveDate NOT specified at the group level
            return sl.GoLiveDate;
        }

        private static DateTime? GetNthGoLiveDate(int slgKey)
        {
            Current?.EnsureCacheReady();
            if (slgKey <= 0)
            {
                return null;
            }

            var _golivedateSLG = Current.Context.ServiceLineGroupings.FirstOrDefault(s => s.ServiceLineGroupingKey == slgKey);
            if (_golivedateSLG == null)
            {
                return null;
            }

            return _golivedateSLG.GoLiveDate;
        }
    }
}