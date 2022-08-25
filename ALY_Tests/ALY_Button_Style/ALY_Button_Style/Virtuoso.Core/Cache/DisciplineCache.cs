#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.Discipline)]
    [Export(typeof(ICache))]
    public class DisciplineCache : ReferenceCacheBase<Discipline>
    {
        Dictionary<int, Discipline> DisciplineDictionary = new Dictionary<int, Discipline>();

        private void BuildSearchDataStructures()
        {
            DisciplineDictionary = Context.Disciplines.ToDictionary(d => d.DisciplineKey);
        }

        public static DisciplineCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.Disciplines;

        public VirtuosoDomainContext CacheContext => Context;

        [ImportingConstructor]
        public DisciplineCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Discipline, "014")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("DisciplineCache already initialized.");
            }

            Current = this;
        }

        protected override EntityQuery<Discipline> GetEntityQuery()
        {
            return Context.GetDisciplineForCacheQuery();
        }

        protected override void OnRIACacheLoaded()
        {
            BuildSearchDataStructures();
        }

        protected override void OnCacheSaved()
        {
            BuildSearchDataStructures();
        }

        public static List<Discipline> GetDisciplines(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Disciplines == null))
            {
                return null;
            }

            var ret = Current.Context.Disciplines.OrderBy(p => p.Code).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Discipline { DisciplineKey = 0, Code = " ", Description = " " });
            }

            return ret;
        }

        public static List<Discipline> GetActiveDisciplines(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Disciplines == null))
            {
                return null;
            }

            var ret = Current.Context.Disciplines.Where(p => p.Inactive == false).OrderBy(p => p.Code).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Discipline { DisciplineKey = 0, Code = " ", Description = " " });
            }

            return ret;
        }

        public static Discipline GetDisciplineFromKey(int dscp_Key)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Disciplines == null))
            {
                return null;
            }

            Discipline dscp = null;
            Current.DisciplineDictionary.TryGetValue(dscp_Key, out dscp);
            return dscp;
        }

        public static string GetDescriptionFromKey(int dscp_Key)
        {
            Current?.EnsureCacheReady();
            Discipline dscp = GetDisciplineFromKey(dscp_Key);
            return dscp?.Description;
        }

        public static string GetCodeFromKey(int dscp_Key)
        {
            Current?.EnsureCacheReady();
            Discipline dscp = GetDisciplineFromKey(dscp_Key);
            return dscp?.Code;
        }

        public static string GetHCFACodeFromKey(int dscp_Key)
        {
            Current?.EnsureCacheReady();
            Discipline dscp = GetDisciplineFromKey(dscp_Key);
            return (dscp == null) ? "" : dscp.HCFACode ?? "";
        }

        public static bool GetIsDisciplineTherapyFromKey(int dscp_Key)
        {
            Current?.EnsureCacheReady();
            Discipline dscp = GetDisciplineFromKey(dscp_Key);
            string[] therapycodes = { "B", "C", "D" };
            return (dscp != null) && therapycodes.Contains(dscp.HCFACode);
        }

        public static bool GetIsAideFromKey(int dscp_Key)
        {
            Current?.EnsureCacheReady();
            Discipline dscp = GetDisciplineFromKey(dscp_Key);
            string[] aideCodes = { "F" };
            return (dscp != null) && aideCodes.Contains(dscp.HCFACode);
        }

        public static bool GetIsAssistantFromKey(int dscp_Key)
        {
            Current?.EnsureCacheReady();
            Discipline d = GetDisciplineFromKey(dscp_Key);
            if (d == null)
            {
                return false;
            }

            if (d.EvalServiceTypeOptional)
            {
                return false; //It is OK if no Evals are defined for the DSCP - e.g. Hospice Physician Services
            }

            foreach (int? formkey in d.ServiceType.Where(p => p.FormKey != null).Select(p => p.FormKey))
            {
                if ((DynamicFormCache.IsEval(formkey.Value)) || (DynamicFormCache.IsPreEval(formkey.Value)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}