#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.GuardArea)]
    [Export(typeof(ICache))]
    public class GuardAreaCache : ReferenceCacheBase<GuardArea>
    {
        public static GuardAreaCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.GuardAreas;

        [ImportingConstructor]
        public GuardAreaCache(ILogger logManager)
            : base(logManager, ReferenceTableName.GuardArea, "004")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("GuardAreaCache already initialized.");
            }

            Current = this;
        }

        protected override EntityQuery<GuardArea> GetEntityQuery()
        {
            return Context.GetGuardAreaQuery();
        }

        public static List<GuardArea> GetGuardArea()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.GuardAreas == null))
            {
                return null;
            }

            return Current.Context.GuardAreas.OrderBy(g => g.ZipCode).ThenBy(g => g.Plus4).ToList();
        }

        public static List<GuardArea> GetGuardAreas(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.GuardAreas == null))
            {
                return null;
            }

            var ret = Current.Context.GuardAreas.OrderBy(g => g.ZipCode).ThenBy(g => g.Plus4).ToList();
            if (includeEmpty)
            {
                ret.Insert(0,
                    new GuardArea
                        { GuardAreaKey = 0, GuardAreaID = string.Empty, ZipCode = string.Empty, Plus4 = String.Empty });
            }

            return ret;
        }

        public static List<GuardArea> GetGuardAreaByZipCodeParts(string zipCode, string plus4)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.GuardAreas == null))
            {
                return null;
            }

            return Current.Context.GuardAreas
                .Where(g => g.ZipCode == zipCode && g.Plus4 == plus4 && g.Inactive == false)
                .OrderBy(g => g.ZipCode).ThenBy(g => g.Plus4).ToList();
        }

        public static List<GuardArea> GetGuardAreaZipCodesByState(int? stateCode, bool includeEmpty)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) ||
                (Current.Context.GuardAreas == null))
            {
                return null;
            }

            var ret = GetGuardAreas().DistinctBy(x => x.ZipCode).Where(s => s.StateCode == stateCode)
                .OrderBy(g => g.ZipCode).ToList();
            if (includeEmpty)
            {
                ret.Insert(0,
                    new GuardArea
                    {
                        GuardAreaKey = 0, GuardAreaID = string.Empty, StateCode = 0, ZipCode = string.Empty,
                        Plus4 = String.Empty
                    });
            }

            return ret;
        }

        public static List<GuardArea> GetGuardAreaZipCodes(bool includeEmpty)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) ||
                (Current.Context.GuardAreas == null))
            {
                return null;
            }

            var ret = GetGuardAreas().DistinctBy(x => x.ZipCode).OrderBy(g => g.ZipCode).ToList();
            if (includeEmpty)
            {
                ret.Insert(0,
                    new GuardArea
                    {
                        GuardAreaKey = 0, GuardAreaID = string.Empty, StateCode = 0, ZipCode = string.Empty,
                        Plus4 = String.Empty
                    });
            }

            return ret;
        }

        public static List<GuardArea> GetGuardAreaStates(bool includeEmpty)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) ||
                (Current.Context.GuardAreas == null))
            {
                return null;
            }

            var ret = GetGuardAreas().DistinctBy(g => g.State).OrderBy(g => g.State).ToList();
            if (includeEmpty)
            {
                ret.Insert(0,
                    new GuardArea
                    {
                        GuardAreaKey = 0, GuardAreaID = string.Empty, StateCode = 0, ZipCode = string.Empty,
                        Plus4 = String.Empty
                    });
            }

            return ret;
        }

        public static List<GuardArea> GetGuardAreaByGuardID(string guardAreaID)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.GuardAreas == null))
            {
                return null;
            }

            return Current.Context.GuardAreas
                .Where(g => g.GuardAreaID == guardAreaID)
                .OrderBy(g => g.ZipCode)
                .ThenBy(g => g.Plus4)
                .ToList();
        }

        //Called by maintenance screen after code saved to server database
        public async System.Threading.Tasks.Task UpdateGuardAreaCache(GuardArea guardArea)
        {
            await PurgeAndSave();
            Messenger.Default.Send(guardArea, "GuardAreaNewStateCode");
            Messenger.Default.Send(guardArea, "GuardAreaNewZipCode");
        }
    }
}