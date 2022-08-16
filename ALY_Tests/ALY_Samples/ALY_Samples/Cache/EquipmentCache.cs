#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.Equipment)]
    [Export(typeof(ICache))]
    public class EquipmentCache : ReferenceCacheBase<Equipment>
    {
        public static EquipmentCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.Equipments;

        [ImportingConstructor]
        public EquipmentCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Equipment, "005")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("EquipmentCache already initialized.");
            }

            Current = this;
        }

        protected override EntityQuery<Equipment> GetEntityQuery()
        {
            return Context.GetEquipmentQuery();
        }

        public static List<Equipment> GetEquipment()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Equipments == null))
            {
                return null;
            }

            return Current.Context.Equipments.OrderBy(p => p.Description1).ToList();
        }

        public static List<Equipment> GetEquipmentByType(string type, int PlusMeKey = -1, bool includeNullItem = true)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Equipments == null))
            {
                return null;
            }

            var query = Current.Context.Equipments.OrderBy(p => p.Description1).AsEnumerable();
            switch (type)
            {
                case "Gait":
                    query = query.Where(p => p.Gait || (p.EquipmentKey == PlusMeKey));
                    break;
                case "ChairArm":
                    query = query.Where(p => p.ChairArm || (p.EquipmentKey == PlusMeKey));
                    break;
                case "ChairNoArm":
                    query = query.Where(p => p.ChairNoArm || (p.EquipmentKey == PlusMeKey));
                    break;
                case "TubShower":
                    query = query.Where(p => p.TubShower || (p.EquipmentKey == PlusMeKey));
                    break;
                case "Toilet":
                    query = query.Where(p => p.Toilet || (p.EquipmentKey == PlusMeKey));
                    break;
                case "Bed":
                    query = query.Where(p => p.Bed || (p.EquipmentKey == PlusMeKey));
                    break;
                case "InfusionPump":
                    query = query.Where(p => p.InfusionPump || (p.EquipmentKey == PlusMeKey));
                    break;
            }

            var ret = query.ToList();
            if (includeNullItem)
            {
                ret.Insert(0, new Equipment { ItemCode = " ", Description1 = " ", EquipmentKey = 0 });
            }

            return ret;
        }

        public static string GetEquipmentDescriptionFromType(string type)
        {
            switch (type)
            {
                case "Gait":
                    return "gait";
                case "ChairArm":
                    return "chair w/arms";
                case "ChairNoArm":
                    return "chair w/o arms";
                case "TubShower":
                    return "bathtub and shower";
                case "Toilet":
                    return "toilet";
                case "Bed":
                    return "bed";
                case "InfusionPump":
                    return "infusion pump";
                default:
                    return type;
            }
        }

        public static string GetDescriptionFromKey(int key)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Equipments == null))
            {
                return String.Empty;
            }

            Equipment e = Current.Context.Equipments.Where(p => p.EquipmentKey == key).FirstOrDefault();
            return (e == null) ? null : e.Description1;
        }

        public static string GetEffectiveFromFromKey(int key)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Equipments == null))
            {
                return String.Empty;
            }

            Equipment e = Current.Context.Equipments.Where(p => p.EquipmentKey == key).FirstOrDefault();
            return (e == null) ? null : e.EffectiveFrom.ToString();
        }

        public static string GetEffectiveThruFromKey(int key)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Equipments == null))
            {
                return String.Empty;
            }

            Equipment e = Current.Context.Equipments.Where(p => p.EquipmentKey == key).FirstOrDefault();
            return (e == null) ? null : e.EffectiveThru.ToString();
        }
    }
}