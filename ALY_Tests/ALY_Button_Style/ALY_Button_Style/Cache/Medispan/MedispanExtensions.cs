#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Virtuoso.Client.Cache;
using Virtuoso.Portable.Database;
using Virtuoso.Portable.Model;

#endregion

namespace Virtuoso.Core.Cache.Medispan.Extensions
{
    public static class MedispanMedicationCacheExtensions
    {
        public static CachedMediSpanMedication GetEntityFromRecordSet(
            this FlatFileCacheBase<CachedMediSpanMedication> me, RecordSet recordSet)
        {
            var entity = me.NewObject();
            Portable.Extensions.MedispanMedicationCacheExtensions
                .RecordSetToCachedMediSpanMedication(recordSet, entity);
            return entity;
        }

        public static async Task<List<CachedMediSpanMedication>> Search(
            this FlatFileCacheBase<CachedMediSpanMedication> me, string texts, int take = 101)
        {
            await me.EnsureDataLoadedFromDisk();

            string[] delimiters = { " " };
            string[] searchpieces = texts.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (searchpieces.Length == 0)
            {
                return null;
            }

            if (me.Data == null)
            {
                return null;
            }

            List<CachedMediSpanMedication> items = (from med in me.Data
                where med.Name.ToLower().Contains(searchpieces[0].ToLower())
                select med).ToList();

            for (int i = 1; i < searchpieces.Length; i++)
                items = (from med in items
                    where med.Name.ToLower().Contains(searchpieces[i].ToLower())
                    select med).ToList();
            // remove any med that has been obsolete for over a year
            items = (from med in items
                where ((med.ODate == null) || ((med.ODate != null) && (med.ODate > DateTime.Today.AddDays(-366))))
                select med).ToList();
            items = items.Distinct().OrderBy(m => m.Name).ToList();

            return (items.Any() == false) ? null : items.OrderBy(i => i.MedType).Take(take).ToList();
        }

        public static async Task<List<CachedMediSpanMedication>> SearchRoutedDrugsOnly(
            this FlatFileCacheBase<CachedMediSpanMedication> me, string texts, int take = 101)
        {
            await me.EnsureDataLoadedFromDisk();

            string[] delimiters = { " " };
            string[] searchpieces = texts.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (searchpieces.Length == 0)
            {
                return null;
            }

            if (me.Data == null)
            {
                return null;
            }

            List<CachedMediSpanMedication> items = (from med in me.Data
                where ((med.Name.ToLower().Contains(searchpieces[0].ToLower())) && (med.DDID == null))
                select med).ToList();

            for (int i = 1; i < searchpieces.Length; i++)
                items = (from med in items
                    where med.Name.ToLower().Contains(searchpieces[i].ToLower())
                    select med).ToList();
            // remove any med that has been obsolete for over a year
            items = (from med in items
                where ((med.ODate == null) || ((med.ODate != null) && (med.ODate > DateTime.Today.AddDays(-366))))
                select med).ToList();
            items = items.Distinct().OrderBy(m => m.Name).ToList();

            return (items.Any() == false) ? null : items.OrderBy(i => i.MedType).Take(take).ToList();
        }

        public static async Task<List<CachedMediSpanMedication>> SearchDDIDDrugsOnly(
            this FlatFileCacheBase<CachedMediSpanMedication> me, string texts, int take = 101)
        {
            await me.EnsureDataLoadedFromDisk();

            string[] delimiters = { " " };
            string[] searchpieces = texts.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (searchpieces.Length == 0)
            {
                return null;
            }

            if (me.Data == null)
            {
                return null;
            }

            List<CachedMediSpanMedication> items = (from med in me.Data
                where ((med.Name.ToLower().Contains(searchpieces[0].ToLower())) && (med.DDID != null))
                select med).ToList();

            for (int i = 1; i < searchpieces.Length; i++)
                items = (from med in items
                    where med.Name.ToLower().Contains(searchpieces[i].ToLower())
                    select med).ToList();
            // remove any med that has been obsolete for over a year
            items = (from med in items
                where ((med.ODate == null) || ((med.ODate != null) && (med.ODate > DateTime.Today.AddDays(-366))))
                select med).ToList();
            items = items.Distinct().OrderBy(m => m.Name).ToList();

            return (items.Any() == false) ? null : items.OrderBy(i => i.MedType).Take(take).ToList();
        }

        public static async Task<CachedMediSpanMedication> GetMediSpanMedicationByMedispanMedicationKey(
            this FlatFileCacheBase<CachedMediSpanMedication> me, int? MedispanMedicationKey)
        {
            await me.EnsureDataLoadedFromDisk();

            if (MedispanMedicationKey == null)
            {
                return null;
            }

            if (me.Data == null)
            {
                return null;
            }

            CachedMediSpanMedication m = (from med in me.Data
                where med.MedKey == (int)MedispanMedicationKey
                select med).FirstOrDefault();
            return m;
        }
    }
}