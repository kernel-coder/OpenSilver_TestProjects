#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Ria.Sync;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.CodeLookup)]
    [Export(typeof(ICache))]
    public class CodeLookupCache : ReferenceCacheBase<CodeLookupHeader>
    {
        public static CodeLookupCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.CodeLookupHeaders;

        [ImportingConstructor]
        public CodeLookupCache(ILogger logManager)
            : base(logManager, ReferenceTableName.CodeLookup, "007")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("CodeLookupCache already initialized.");
            }

            Current = this;
            RequireCacheRecords = true;
        }

        protected override EntityQuery<CodeLookupHeader> GetEntityQuery()
        {
            return Context.GetCodeLookupHeaderQuery();
        }

        protected override void OnRIACacheLoaded()
        {
            BuildSearchDataStructures();
        }

        protected override void OnCacheSaved()
        {
            BuildSearchDataStructures();
        }

        Dictionary<int, CodeLookup> CodeLookupDictionary = new Dictionary<int, CodeLookup>();

        private void BuildSearchDataStructures()
        {
            CodeLookupDictionary = Context.CodeLookups.ToDictionary(fsq => fsq.CodeLookupKey);
        }

        public static List<Virtuoso.Portable.Utility.DropDownItem> GetGoalElementResponseTypes()
        {
            var ret = new List<Virtuoso.Portable.Utility.DropDownItem>();

            ret.Add(new Virtuoso.Portable.Utility.DropDownItem { Id = 1, Description = "Yes, No, Declined" });
            ret.Add(new Virtuoso.Portable.Utility.DropDownItem { Id = 2, Description = "Integer" });
            ret.Add(new Virtuoso.Portable.Utility.DropDownItem { Id = 3, Description = "Decimal" });
            ret.Add(new Virtuoso.Portable.Utility.DropDownItem { Id = 4, Description = "Fraction" });
            ret.Add(new Virtuoso.Portable.Utility.DropDownItem { Id = 5, Description = "True or False" });

            return ret;
        }

        public static List<CodeLookupHeader> GetCodeLookupHeaders(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.CodeLookupHeaders == null))
            {
                return null;
            }

            var ret = Current.Context.CodeLookupHeaders.OrderBy(p => p.CodeType).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new CodeLookupHeader { CodeLookupHeaderKey = 0, CodeType = " " });
            }

            return ret;
        }

        public static CodeLookupHeader GetCodeLookupHeaderFromKey(int key)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.CodeLookupHeaders == null))
            {
                return null;
            }

            var ret = Current.Context.CodeLookupHeaders.FirstOrDefault(ch => ch.CodeLookupHeaderKey == key);
            return ret;
        }

        public static List<CodeLookup> GetChildrenFromKey(int key)
        {
            Current?.EnsureCacheReady();
            return Current.Context.CodeLookups.Where(w => w.ParentCodeLookupKey == key).ToList();
        }

        public static CodeLookupHeader GetCodeLookupHeaderForType(String CodeType)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.CodeLookupHeaders == null))
            {
                return null;
            }

            if (String.IsNullOrEmpty(CodeType))
            {
                return null;
            }

            var ret = Current.Context.CodeLookupHeaders
                .FirstOrDefault(ch => ch.CodeType.ToLower() == CodeType.ToLower());

            return ret;
        }

        public static string GetCodeLookupHeaderDescriptionFromType(
            string codeType)
        {
            Current?.EnsureCacheReady();
            if ((codeType == null) || (Current == null) || (Current.Context == null))
            {
                return null;
            }

            CodeLookupHeader codeLookupType =
                (from h in Current.Context.CodeLookupHeaders
                 where (h.CodeType.ToUpper() == codeType.ToUpper())
                 select h).FirstOrDefault();
            return codeLookupType?.CodeTypeDescription;
        }

        public static List<CodeLookup> GetCodeLookupsFromType(
            string codeType,
            bool orderByCodeDescription = false,
            bool includeInactive = false,
            bool includeEmpty = false,
            int? sequence = null,
            int plusMeKey = 0)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            if (codeType == null)
            {
                return null;
            }

            CodeLookupHeader codeLookupType =
                (from h in Current.Context.CodeLookupHeaders
                 where (h.CodeType.ToUpper() == codeType.ToUpper())
                 select h).FirstOrDefault();
            if (codeLookupType == null)
            {
                return null;
            }

            IQueryable<CodeLookup> ret = codeLookupType.CodeLookup.AsQueryable();

            if (!includeInactive)
            {
                ret = ret.Where(c => ((c.Inactive == false) || (c.CodeLookupKey == plusMeKey)));
            }
            
            if (sequence != null)
            {
                ret = ret.Where(c => c.Sequence <= sequence);
            }

            List<CodeLookup> orderedRet;
            if (codeLookupType.Sequenced)
            {
                orderedRet = ret
                    .OrderBy(c => c.SequenceSortable)
                    .ThenBy(c => orderByCodeDescription ? c.CodeDescription : c.Code)
                    .ToList();
            }
            else
            {
                orderedRet = ret
                    .OrderBy(c => orderByCodeDescription ? c.CodeDescription : c.Code)
                    .ThenBy(c => c.SequenceSortable)
                    .ToList();
            }

            if (includeEmpty)
            {
                orderedRet.Insert(0, new CodeLookup { Code = " ", CodeDescription = " ", CodeLookupKey = 0 });
            }

            return orderedRet;
        }

        public static List<CodeLookup> GetCodeLookupsFromTypeAndApplicationData(
            string codeType,
            string applicationData,
            bool orderByCodeDescription = false,
            bool includeInactive = false,
            bool includeEmpty = false,
            int? sequence = null,
            int plusMeKey = 0)
        {
            Current?.EnsureCacheReady();
            if (string.IsNullOrWhiteSpace(applicationData))
            {
                return GetCodeLookupsFromType(codeType, orderByCodeDescription, includeInactive, includeEmpty, sequence,
                    plusMeKey);
            }

            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            if (codeType == null)
            {
                return null;
            }

            CodeLookupHeader codeLookupType =
                (from h in Current.Context.CodeLookupHeaders
                 where (h.CodeType.ToUpper() == codeType.ToUpper())
                 select h).FirstOrDefault();
            if (codeLookupType == null)
            {
                return null;
            }

            var ret = codeLookupType.CodeLookup.Where(c => c.ApplicationDataContains(applicationData));
            
            if (!includeInactive)
            {
                ret = ret.Where(c => c.Inactive == false || c.CodeLookupKey == plusMeKey);
            }

            if (sequence != null)
            {
                ret = ret.Where(c => c.Sequence <= sequence);
            }

            List<CodeLookup> orderedRet;
            if (codeLookupType.Sequenced)
            {
                orderedRet = ret.OrderBy(c => c.SequenceSortable)
                    .ThenBy(c => orderByCodeDescription ? c.CodeDescription : c.Code)
                    .ToList();
            }
            else
            {
                orderedRet = ret.OrderBy(c => orderByCodeDescription ? c.CodeDescription : c.Code)
                    .ThenBy(c => c.SequenceSortable)
                    .ToList();
            }
            
            if (includeEmpty)
            {
                orderedRet.Insert(0, new CodeLookup { Code = " ", CodeDescription = " ", CodeLookupKey = 0 });
            }

            return orderedRet;
        }

        public static async Task<List<CodeLookup>> GetCodeLookupsFromTypeWithNewContext(
            string codeType,
            bool orderByCodeDescription = false,
            bool includeInactive = false,
            bool includeEmpty = false,
            int? sequence = null,
            int plusMeKey = 0)
        {
            var newContext = new VirtuosoDomainContext();

            var cache = await RIACacheManager.Initialize(
                Path.Combine(Current.ApplicationStore, Constants.REFERENCE_DATA_STORE_FOLDER),
                Current.CacheName,
                Constants.ENTITY_TYPENAME_FORMAT,
                true); //NOTE: can throw DirectoryNotFoundException

            // Load the data into newContext via the cache
            await cache.Load(newContext);

            if (newContext.CodeLookupHeaders == null)
            {
                return null;
            }

            if (codeType == null)
            {
                return null;
            }

            CodeLookupHeader codeLookupType =
                (from h in newContext.CodeLookupHeaders where (h.CodeType.ToUpper() == codeType.ToUpper()) select h)
                .FirstOrDefault();

            if (codeLookupType == null)
            {
                return null;
            }

            var ret = codeLookupType.CodeLookup.AsQueryable();

            if (!includeInactive)
            {
                ret = ret.Where(c => c.Inactive == false || c.CodeLookupKey == plusMeKey);
            }

            if (sequence != null)
            {
                ret = ret.Where(c => c.Sequence <= sequence);
            }

            List<CodeLookup> orderedRet;

            if (codeLookupType.Sequenced)
            {
                orderedRet = ret.OrderBy(c => c.SequenceSortable)
                    .ThenBy(c => orderByCodeDescription ? c.CodeDescription : c.Code)
                    .ToList();
            }
            else
            {
                orderedRet = ret.OrderBy(c => orderByCodeDescription ? c.CodeDescription : c.Code)
                    .ThenBy(c => c.Sequence)
                    .ToList();
            }

            if (includeEmpty)
            {
                orderedRet.Insert(0, new CodeLookup { Code = " ", CodeDescription = " ", CodeLookupKey = 0 });
            }
            
            return orderedRet;
        }

        public static string GetCodeFromKey(string codeType, int codeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            CodeLookup codeLookup = GetCodeLookupFromKey(codeKey);
            return codeLookup?.Code;
        }

        public static int? GetCodeLookupKeyFromCodeTypeAndCode(string codeType, string code)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            List<CodeLookup> codeLookups = GetCodeLookupsFromType(codeType, false, true);
            if (codeLookups == null)
            {
                return null;
            }

            CodeLookup codeLookup =
                (from c in codeLookups.AsQueryable() where (c.Code.ToLower() == code.ToLower()) select c)
                .FirstOrDefault();
            return codeLookup?.CodeLookupKey;
        }

        public static int? GetKeyFromCode(string codeType, string code)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            List<CodeLookup> codeLookups = GetCodeLookupsFromType(codeType, false, true);
            if (codeLookups == null)
            {
                return null;
            }

            CodeLookup codeLookup =
                (from c in codeLookups.AsQueryable() where (c.Code.ToLower() == code.ToLower()) select c)
                .FirstOrDefault();
            return codeLookup?.CodeLookupKey;
        }

        public static int? GetKeyFromCodeDescription(string codeType, string codeDescription)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(codeDescription))
            {
                return null;
            }

            List<CodeLookup> codeLookups = GetCodeLookupsFromType(codeType, false, true);
            if (codeLookups == null)
            {
                return null;
            }

            CodeLookup codeLookup =
                (from c in codeLookups.AsQueryable()
                 where (c.CodeDescription.ToLower() == codeDescription.ToLower())
                 select c).FirstOrDefault();
            return codeLookup?.CodeLookupKey;
        }

        public static string GetDescriptionFromCode(string codeType, string code)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            List<CodeLookup> codeLookups = GetCodeLookupsFromType(codeType, false, true);
            if (codeLookups == null)
            {
                return null;
            }

            CodeLookup codeLookup =
                (from c in codeLookups.AsQueryable() where (c.Code.ToLower() == code.ToLower()) select c)
                .FirstOrDefault();
            return codeLookup?.CodeDescription;
        }

        public static string GetCodeFromDescription(string codeType, string description)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return null;
            }

            List<CodeLookup> codeLookups = GetCodeLookupsFromType(codeType);
            if (codeLookups == null)
            {
                return null;
            }

            CodeLookup codeLookup =
                (from c in codeLookups.AsQueryable()
                 where (c.CodeDescription.ToLower() == description.ToLower())
                 select c).FirstOrDefault();
            return codeLookup?.Code;
        }

        public static bool GetSingleSelectFromDescription(string codeType, string description)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return false;
            }

            List<CodeLookup> codeLookups = GetCodeLookupsFromType(codeType);
            if (codeLookups == null)
            {
                return false;
            }

            CodeLookup codeLookup =
                (from c in codeLookups.AsQueryable()
                 where ((c.CodeDescription.Trim().ToLower() == description.Trim().ToLower()) &&
                        (c.SingleSelect == true))
                 select c).FirstOrDefault();
            return (codeLookup != null);
        }

        public static string GetCodeDescriptionFromKey(string codeType, int codeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            CodeLookup codeLookup = GetCodeLookupFromKey(codeKey);
            return codeLookup?.CodeDescription;
        }

        public static CodeLookup GetCodeLookupFromKey(int codeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            CodeLookup codeLookup = null;
            Current.CodeLookupDictionary.TryGetValue(codeKey, out codeLookup);
            return codeLookup;
        }

        public static string FormatParsedValues(Tuple<List<CodeLookup>, List<string>> values, string textdelimiter)
        {
            string result = string.Empty;
            foreach (var item in values.Item1)
                result += ((result != string.Empty) ? textdelimiter : string.Empty) + item.CodeDescription;
            foreach (string s in values.Item2)
                result += ((result != string.Empty) ? textdelimiter : string.Empty) + "Other: " + s;
            return result;
        }

        public static Tuple<List<CodeLookup>, List<string>> ParseKeysToDescriptions(string values, Char splitDelimiter)
        {
            var Others = new List<string>();
            var Found = new List<CodeLookup>();

            if (!string.IsNullOrEmpty(values))
            {
                var valuesSplit = values.Split(splitDelimiter);
                Array.Sort(valuesSplit);
                foreach (string c in valuesSplit)
                {
                    string cTrim = c.Trim();
                    int ikey;
                    if (int.TryParse(cTrim, out ikey))
                    {
                        var item = GetCodeLookupFromKey(ikey);
                        if (!Found.Any(a => a.CodeLookupKey == item.CodeLookupKey))
                        {
                            Found.Add(item);
                        }
                    }
                    else
                    {
                        if (cTrim.Length > 2 && cTrim.Substring(0, 1).Equals("\"") &&
                            cTrim.Substring(cTrim.Length - 1, 1).Equals("\""))
                        {
                            Others.Add(cTrim.Substring(1, cTrim.Length - 2));
                        }
                    }
                }
            }

            return new Tuple<List<CodeLookup>, List<string>>(Found, Others);
        }

        public static int? GetSequenceFromKey(int? codeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            if (codeKey == null)
            {
                return null;
            }

            if (codeKey == 0)
            {
                return null;
            }

            CodeLookup cl = Current.Context.CodeLookups.FirstOrDefault(p => p.CodeLookupKey == codeKey);
            if (cl == null)
            {
                return null;
            }

            return cl.Sequence;
        }

        public static string GetCodeFromKey(int codeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            CodeLookup codeLookup = null;
            Current.CodeLookupDictionary.TryGetValue(codeKey, out codeLookup);
            return codeLookup?.Code;
        }

        public static string GetCodeFromKey(int? codeKey)
        {
            int key = codeKey ?? 0;
            return GetCodeFromKey(key);
        }

        public static CodeLookup GetCodeLookupFromKey(int? codeKey)
        {
            int key = codeKey ?? 0;
            return GetCodeLookupFromKey(key);
        }

        public static string GetCodeDescriptionFromKey(int? codeKey)
        {
            int key = codeKey ?? 0;
            return GetCodeDescriptionFromKey(key);
        }

        public static string GetCodeDescriptionFromKey(int codeKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            CodeLookup codeLookup = GetCodeLookupFromKey(codeKey);
            return codeLookup?.CodeDescription;
        }

        public static int? GetCodeLookupKeyFromApplicationData(string CodeType, string ApplicationData)
        {
            Current?.EnsureCacheReady();
            if (string.IsNullOrWhiteSpace(ApplicationData))
            {
                return null;
            }

            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            List<CodeLookup> codeLookupList = GetCodeLookupsFromType(CodeType);
            if (codeLookupList == null)
            {
                return null;
            }

            CodeLookup codeLookup = codeLookupList.Where(p => p.ApplicationData == ApplicationData).FirstOrDefault();
            if (codeLookup == null)
            {
                return null;
            }

            return codeLookup.CodeLookupKey;
        }
        private static int? _OrderTypePOC = null;
        public static int? GetOrderTypePOC()
        {
            if (_OrderTypePOC != null) return _OrderTypePOC;
            List<CodeLookup> clList = GetCodeLookupsFromType("OrderType");
            _OrderTypePOC = clList.Where(c => c.Code == "POC").Select(c => c.CodeLookupKey).First();
            return _OrderTypePOC;
        }
        private static int? _OrderTypeF2F = null;
        public static int? GetOrderTypeF2F()
        {
            if (_OrderTypeF2F != null) return _OrderTypeF2F;
            List<CodeLookup> clList = GetCodeLookupsFromType("OrderType");
            _OrderTypeF2F = clList.Where(c => c.Code == "F2F").Select(c => c.CodeLookupKey).First();
            return _OrderTypeF2F;
        }
        private static int? _OrderTypeCTI = null;
        public static int? GetOrderTypeCTI()
        {
            if (_OrderTypeCTI != null) return _OrderTypeCTI;
            List<CodeLookup> clList = GetCodeLookupsFromType("OrderType");
            _OrderTypeCTI = clList.Where(c => c.Code == "CTI").Select(c => c.CodeLookupKey).First();
            return _OrderTypeCTI;
        }
        private static int? _OrderTypeHospF2F = null;
        public static int? GetOrderTypeHospF2F()
        {
            if (_OrderTypeHospF2F != null) return _OrderTypeHospF2F;
            List<CodeLookup> clList = GetCodeLookupsFromType("OrderType");
            _OrderTypeHospF2F = clList.Where(c => c.Code == "HospF2F").Select(c => c.CodeLookupKey).First();
            return _OrderTypeHospF2F;
        }
        private static int? _OrderTypeInterim = null;
        public static int? GetOrderTypeInterim()
        {
            if (_OrderTypeInterim != null) return _OrderTypeInterim;
            List<CodeLookup> clList = GetCodeLookupsFromType("OrderType");
            _OrderTypeInterim = clList.Where(c => c.Code == "Interim").Select(c => c.CodeLookupKey).First();
            return _OrderTypeInterim;
        }
        private static int? _RuleCycleDays = null;
        public static int? GetRuleCycleDays()
        {
            if (_RuleCycleDays != null) return _RuleCycleDays;
            List<CodeLookup> clList = GetCodeLookupsFromType("RuleCycle");
            _RuleCycleDays = clList.Where(c => c.Code == "Days").Select(c => c.CodeLookupKey).First();
            return _RuleCycleDays;
        }
        private static int? _RuleCycleWeeks = null;
        public static int? GetRuleCycleWeeks()
        {
            if (_RuleCycleWeeks != null) return _RuleCycleWeeks;
            List<CodeLookup> clList = GetCodeLookupsFromType("RuleCycle");
            _RuleCycleWeeks = clList.Where(c => c.Code == "Weeks").Select(c => c.CodeLookupKey).First();
            return _RuleCycleWeeks;
        }
        private static int? _RuleCycleMonths = null;
        public static int? GetRuleCycleMonths()
        {
            if (_RuleCycleMonths != null) return _RuleCycleMonths;
            List<CodeLookup> clList = GetCodeLookupsFromType("RuleCycle");
            _RuleCycleMonths = clList.Where(c => c.Code == "Months").Select(c => c.CodeLookupKey).First();
            return _RuleCycleMonths;
        }
    }
}