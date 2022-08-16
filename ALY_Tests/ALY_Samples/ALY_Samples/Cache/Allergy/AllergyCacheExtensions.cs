#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Virtuoso.Client.Cache;
using Virtuoso.Portable.Database;
using Virtuoso.Portable.Model;

#endregion

namespace Virtuoso.Core.Cache.Allergy.Extensions
{
    public static class AllergyCacheExtensions
    {
        public static CachedAllergyCode GetEntityFromRecordSet(this FlatFileCacheBase<CachedAllergyCode> me,
            RecordSet recordSet)
        {
            var entity = me.NewObject();
            Portable.Extensions.AllergyCacheExtensions.RecordSetToCachedAllergyCode(recordSet, entity);
            return entity;
        }

        public static async Task<List<CachedAllergyCode>> Search(this FlatFileCacheBase<CachedAllergyCode> me,
            string text, bool isCode)
        {
            await me.EnsureDataLoadedFromDisk();

            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            text = text.ToLowerInvariant();

            var searchPiecesLower = text
                .Split(default(string[]), StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.ToLowerInvariant()).ToArray();

            if (searchPiecesLower.Length == 0)
            {
                return null; //DO NOT return all objects from database
            }

            IQueryable<CachedAllergyCode> dbQuery = null;
            if (isCode)
            {
                dbQuery = me.Data.Where(i => (i.UNII == text));
            }
            else
            {
                dbQuery = me.Data.Where(i => (i.FullText.Contains(text)));
            }

            var databaseResult = dbQuery.ToList();

            for (int i = 1; i < searchPiecesLower.Length; i++) //FYI: skipping the first searchPiecesLower
            {
                //Filter previous results with the next 'piece' of search criteria
                databaseResult = (from icd in databaseResult
                    where icd.FullText.Contains(searchPiecesLower[i].ToLower())
                    select icd).ToList();
            }

            //filter and sort results
            var ret = databaseResult
                .Where(i => ((i.EffectiveFrom <= DateTime.UtcNow) &&
                             (i.EffectiveThru.HasValue == false || i.EffectiveThru > DateTime.UtcNow)))
                .OrderBy(i => i.DisplayName)
                .ThenBy(i => i.SubstanceName)
                .ToList();
            return ret;
        }
    }
}