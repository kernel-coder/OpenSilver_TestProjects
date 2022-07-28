#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.ComfortPack)]
    [Export(typeof(ICache))]
    public class ComfortPackCache : ReferenceCacheBase<ComfortPack>
    {
        public static ComfortPackCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.ComfortPacks;

        [ImportingConstructor]
        public ComfortPackCache(ILogger logManager)
            : base(logManager, ReferenceTableName.ComfortPack, "001")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("ComfortPackCache already initialized.");
            }

            Current = this;
        }

        protected override EntityQuery<ComfortPack> GetEntityQuery()
        {
            return Context.GetComfortPackQuery();
        }
        
        public static List<ComfortPack> GetActiveComfortPacks(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ComfortPacks == null))
            {
                return null;
            }

            List<ComfortPack> cpList = Current.Context.ComfortPacks
                .Where(p => ((p.Inactive == false) && (p.HistoryKey == null))).OrderBy(p => p.ComfortPackDescription)
                .ToList();
            if (includeEmpty)
            {
                cpList.Insert(0, new ComfortPack { ComfortPackKey = 0, ComfortPackDescription = " " });
            }

            if ((cpList == null) || (cpList.Count() == 0))
            {
                return null;
            }

            return cpList;
        }

        public static List<ComfortPack> GetAllComfortPacks(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.ComfortPacks == null))
            {
                return null;
            }

            List<ComfortPack> cpList = Current.Context.ComfortPacks.Where(p => (p.HistoryKey == null))
                .OrderBy(p => p.ComfortPackDescription).ToList();
            if (includeEmpty)
            {
                cpList.Insert(0, new ComfortPack { ComfortPackKey = 0, ComfortPackDescription = " " });
            }

            if ((cpList == null) || (cpList.Count() == 0))
            {
                return null;
            }

            return cpList;
        }

        public static List<ComfortPack> GetAllComfortPacksWithMedsForHospice()
        {
            Current?.EnsureCacheReady();
            List<ComfortPack> cpList = GetActiveComfortPacks();
            if ((cpList == null) || (cpList.Count() == 0))
            {
                return null;
            }

            cpList = cpList.Where(p => (p.IsValidForHospice && p.HasMedications)).OrderBy(p => p.ComfortPackDescription)
                .ToList();
            if ((cpList == null) || (cpList.Count() == 0))
            {
                return null;
            }

            return cpList;
        }

        public static ComfortPack GetComfortPackByKey(int? comfortPackKey)
        {
            Current?.EnsureCacheReady();
            if (!comfortPackKey.HasValue || Current == null || Current.Context == null ||
                Current.Context.ComfortPacks == null)
            {
                return null;
            }

            ComfortPack cp = Current.Context.ComfortPacks.Where(p => p.ComfortPackKey == comfortPackKey)
                .FirstOrDefault();
            return cp;
        }

        public static List<ComfortPackMedication> GetActiveMedicationsByComfortPackKey(int? comfortPackKey)
        {
            Current?.EnsureCacheReady();
            DateTime today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
            if (!comfortPackKey.HasValue || Current == null || Current.Context == null ||
                Current.Context.ComfortPacks == null)
            {
                return null;
            }

            ComfortPack cp = Current.Context.ComfortPacks.Where(p => p.ComfortPackKey == comfortPackKey)
                .FirstOrDefault();
            if ((cp == null) || (cp.ComfortPackMedication == null))
            {
                return null;
            }

            List<ComfortPackMedication> cpmList = cp.ComfortPackMedication.Where(p =>
                    ((p.HistoryKey == null) &&
                     ((p.EffectiveFromDate.HasValue == false) || (p.EffectiveFromDate <= today)) &&
                     ((p.EffectiveThruDate.HasValue == false) || (p.EffectiveThruDate > today)) &&
                     ((p.ObsoleteDate.HasValue == false) || (p.ObsoleteDate > today))))
                .OrderBy(p => p.MedicationName).ToList();
            if ((cpmList == null) || (cpmList.Count() == 0))
            {
                return null;
            }

            return cpmList;
        }
    }
}