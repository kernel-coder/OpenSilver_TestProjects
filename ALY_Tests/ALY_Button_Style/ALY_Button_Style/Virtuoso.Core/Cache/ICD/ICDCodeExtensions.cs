#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Virtuoso.Client.Cache;
using Virtuoso.Core.Model;
using Virtuoso.Portable.Model;

#endregion

namespace Virtuoso.Core.Cache.ICD.Extensions
{
    //This extension class should work for ICDCM9Cache and ICDCM10Cache AND ICDPCS9Cache and ICDPCS10Cache
    public static class ICDCodeSearchExtensions
    {
        //searches DataList
        public static async Task<CachedICDCode> GetICDCodeByCode(this FlatFileCacheBase<CachedICDCode> me, string code)
        {
            await me.EnsureDataLoadedFromDisk();

            var qry = //from CachedICDCode icdCode in DatabaseWrapper.Current
                from icdCode in me.Data
                where icdCode.Code == code
                select icdCode;
            return qry.FirstOrDefault();
        }

        public static async Task<CachedICDCode> GetICDCodeByICDCodeKey(this FlatFileCacheBase<CachedICDCode> me,
            int ICDCodeKey)
        {
            await me.EnsureDataLoadedFromDisk();

            var qry = //from CachedICDCode icdCode in DatabaseWrapper.Current
                from icdCode in me.Data
                where icdCode.ICDCodeKey == ICDCodeKey
                select icdCode;
            return qry.FirstOrDefault();
        }

        //searches DataList
        public static async Task<string> GetDescriptionByCode(this FlatFileCacheBase<CachedICDCode> me, string code)
        {
            await me.EnsureDataLoadedFromDisk();

            var ret =
                (from myobj in me.Data
                    where myobj.Code == code
                    select myobj).FirstOrDefault();

            if (ret != null)
            {
                return ret.Short;
            }

            return String.Empty;
        }

        //searches DataList
        public static async Task<List<CachedICDCode>> Search(this FlatFileCacheBase<CachedICDCode> me,
            ICDSearchData searchContext)
        {
            await me.EnsureDataLoadedFromDisk();

            var text = searchContext.SearchValues;

            var searchPiecesLower = text
                .Split(default(string[]), StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.ToLowerInvariant()).ToArray();

            //User Story 3727: Add the ICD10 Category Group/Category/Sub Category to Crescendo clinical - search text requires at least 3 characters
            if ((string.IsNullOrWhiteSpace(searchContext.CategoryMaxCode)) &&
                (string.IsNullOrWhiteSpace(searchContext.CategoryMinCode)) &&
                (string.IsNullOrWhiteSpace(searchContext.SubCategoryMaxCode)) &&
                (string.IsNullOrWhiteSpace(searchContext.SubCategoryMaxCode)))
            {
                if (string.IsNullOrEmpty(searchContext.SearchValues))
                {
                    return null;
                }

                if (searchContext.SearchValues.Length < 3)
                {
                    return null;
                }
            }

            IQueryable<CachedICDCode> dbQuery = me.Data.AsQueryable();

            if (string.IsNullOrEmpty(searchContext.SubCategoryMinCode) == false &&
                string.IsNullOrEmpty(searchContext.SubCategoryMaxCode) == false)
            {
                dbQuery = await me.GetCategoryQuery(dbQuery, searchContext.SubCategoryMinCode,
                    searchContext.SubCategoryMaxCode);
                if ((string.IsNullOrEmpty(searchContext.SearchValues) == false) &&
                    (string.IsNullOrEmpty(searchPiecesLower[0]) == false))
                {
                    dbQuery = await me.GetSearchTextQuery(dbQuery, searchPiecesLower[0]);
                }
            }
            else if (string.IsNullOrEmpty(searchContext.CategoryMinCode) == false &&
                     string.IsNullOrEmpty(searchContext.CategoryMaxCode) == false)
            {
                dbQuery = await me.GetCategoryQuery(dbQuery, searchContext.CategoryMinCode,
                    searchContext.CategoryMaxCode);
                if ((string.IsNullOrEmpty(searchContext.SearchValues) == false) &&
                    (string.IsNullOrEmpty(searchPiecesLower[0]) == false))
                {
                    dbQuery = await me.GetSearchTextQuery(dbQuery, searchPiecesLower[0]);
                }
            }
            else
            {
                dbQuery = await me.GetSearchTextQuery(dbQuery, searchPiecesLower[0]);
            }

            var databaseResult = dbQuery.ToList();

            if (string.IsNullOrEmpty(searchContext.SearchValues) == false)
            {
                for (int i = 1; i < searchPiecesLower.Length; i++) //FYI: skipping the first searchPiecesLower
                    //Filter previous results with the next 'piece' of search criteria
                    databaseResult = (from icd in databaseResult
                        where icd.FullText.Contains(searchPiecesLower[i].ToLower())
                        select icd).ToList();
            }

            //filter and sort results
            DateTime today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
            DateTime effectiveThru = (searchContext.ShowLastYearCodes == false) ? today : today.AddYears(-1);

            List<CachedICDCode> ret = databaseResult
                .Where(i => ((i.EffectiveFrom <= today) && (i.EffectiveThru.HasValue == false ||
                                                            i.EffectiveThru > effectiveThru)
                                                        && (i.RequiresAdditionalDigit == null ||
                                                            i.RequiresAdditionalDigit == false)))
                .OrderBy(i => i.Code)
                .ThenBy(i => i.Description)
                .ToList();
            return ret;
        }

        public static async Task<IQueryable<CachedICDCode>> GetCategoryQuery(this FlatFileCacheBase<CachedICDCode> me,
            IQueryable<CachedICDCode> qry, string minCode, string maxCode)
        {
            await me.EnsureDataLoadedFromDisk();
            var ret = qry
                .Where(c =>
                    (string.Compare(c.Code.Substring(0, Math.Min(c.Code.Length, minCode.Length)), minCode) >= 0) &&
                    (string.Compare(c.Code.Substring(0, Math.Min(c.Code.Length, maxCode.Length)), maxCode) <= 0))
                .AsQueryable();
            return ret;
        }

        //searches DataList
        public static async Task<IQueryable<CachedICDCode>> GetSearchTextQuery(this FlatFileCacheBase<CachedICDCode> me,
            IQueryable<CachedICDCode> qry, string text)
        {
            await me.EnsureDataLoadedFromDisk();
            var ret = qry
                .Where(i => (i.FullText.Contains(text)))
                .AsQueryable();
            return ret;
        }

        //searches DataList
        public static async Task<IEnumerable> SearchTake(this FlatFileCacheBase<CachedICDCode> me, string text,
            int take, int pVersion, bool pIncludeEcodes = true, bool pIncludeVcodes = true,
            bool pIncludeDummyCodes = true)
        {
            await me.EnsureDataLoadedFromDisk();
            IEnumerable<CachedICDCode> items = null;
            if (!string.IsNullOrEmpty(text))
            {
                var searchpieces = text.Split(' ').ToList();
                searchpieces.ForEach(p => p.ToLower());

                foreach (var searchpiece in searchpieces)
                {
                    var piece = searchpiece.ToLower();

                    if (items == null)
                    {
                        items = me.Data.Where(i => (i.FullText.Contains(piece))).Select(i => i)
                            .AsEnumerable(); //execute query against database
                    }
                    else
                    {
                        items = items.Where(i => (i.FullText.Contains(piece))).Select(i => i)
                            .AsEnumerable(); //execute query against list - reducing it
                    }
                }
            }
            else
            {
                items = me.Data.Select(i => i).AsEnumerable();
            }

            if ((pVersion == 9) && (pIncludeEcodes == false))
            {
                items = items.Where(i => (i.Code.Trim().ToUpper().StartsWith("E") == false)).Distinct().AsEnumerable();
            }

            if ((pVersion == 9) && (pIncludeVcodes == false))
            {
                items = items.Where(i => (i.Code.Trim().ToUpper().StartsWith("V") == false)).Distinct().AsEnumerable();
            }

            if ((pVersion == 10) && (pIncludeEcodes == false))
            {
                items = items.Where(i =>
                        ((i.Code.Trim().ToUpper().StartsWith("V") == false) &&
                         (i.Code.Trim().ToUpper().StartsWith("W") == false) &&
                         (i.Code.Trim().ToUpper().StartsWith("X") == false) &&
                         (i.Code.Trim().ToUpper().StartsWith("Y") == false)))
                    .Distinct().AsEnumerable();
            }

            if ((pVersion == 10) && (pIncludeVcodes == false))
            {
                items = items.Where(i => (i.Code.Trim().ToUpper().StartsWith("Z") == false)).Distinct().AsEnumerable();
            }

            if (pIncludeDummyCodes == false)
            {
                items = items.Where(i => (i.Code.Trim().ToUpper().StartsWith("000.00") == false)).Distinct()
                    .AsEnumerable();
            }

            items = items
                .Where(i => ((i.EffectiveFrom <= DateTime.UtcNow) &&
                             (i.EffectiveThru.HasValue == false || i.EffectiveThru > DateTime.UtcNow)))
                .Distinct()
                .Take(take + 2)
                .OrderBy(i => i.Code);

            if (items.Count() >= take)
            {
                CachedICDCode cic = new CachedICDCode
                    { Code = "...", ICDCodeKey = 0, Short = "<Over " + take + " matches, narrow search criteria...>" };
                List<CachedICDCode> list = items.ToList();
                list.Add(cic);
                items = list.AsEnumerable();
            }

            return items;
        }
    }
}