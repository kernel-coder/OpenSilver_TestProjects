#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Virtuoso.Client.Cache;
using Virtuoso.Core.Model;
using Virtuoso.Core.Services;
using Virtuoso.Portable;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.AddressMapping)]
    [Export(typeof(ICache))]
    public class AddressMappingCache : ICache, ICacheManagement, IAddressMap
    {
        public static AddressMappingCache Current { get; private set; }
        ICache AddressMapStrategy { get; set; }

        [ImportingConstructor]
        public AddressMappingCache(ILogger logManager, IAddressMapFactory AddressMapFactory)
        {
            if (Current == this)
            {
                throw new InvalidOperationException("AddressMappingCache already initialized.");
            }

            Current = this;
            AddressMapStrategy = AddressMapFactory.GetStrategyImpl(logManager);
        }

        #region ICache Interface

        public string Version => AddressMapStrategy.Version;

        public string CacheName => AddressMapStrategy.CacheName;

        public async Task Load(DateTime? lastUpdatedDate, bool isOnline, Action callback, bool force)
        {
            await AddressMapStrategy.Load(lastUpdatedDate, isOnline, callback, force);
        }

        public void Reload(Action callback)
        {
            AddressMapStrategy.Reload(callback);
        }

        public ICacheManagement GetManagementInterface()
        {
            var mgmt = AddressMapStrategy as ICacheManagement;
            if (mgmt != null)
            {
                return mgmt;
            }

            return null;
        }

        void ICacheManagement.Initialize(bool deferLoad)
        {
            var mgmt = AddressMapStrategy as ICacheManagement;
            if (mgmt != null)
            {
                mgmt.Initialize(deferLoad);
            }
            else
            {
                throw new NotImplementedException("AddressMapStrategy does not implement ICacheManagement");
            }
        }

        async Task ICacheManagement.LoadFromDisk()
        {
            var mgmt = AddressMapStrategy as ICacheManagement;
            if (mgmt != null)
            {
                await mgmt.LoadFromDisk();
            }
            else
            {
                throw new NotImplementedException("AddressMapStrategy does not implement ICacheManagement");
            }
        }

        #endregion

        #region IAddressMap

        async Task<IEnumerable> IAddressMap.Search(string zip, string state, DateTime? effectiveFrom,
            DateTime? effectiveTo, int take)
        {
            return await ((IAddressMap)AddressMapStrategy).Search(zip, state, effectiveFrom, effectiveTo, take);
        }

        public async Task<IEnumerable<COUNTYCode>> GetCOUNTYCodes()
        {
            return await ((IAddressMap)AddressMapStrategy).GetCOUNTYCodes();
        }

        public async Task<IEnumerable<ZIPCode>> GetZIPCodes()
        {
            return await ((IAddressMap)AddressMapStrategy).GetZIPCodes();
        }

        public List<USAState> USAStates => ((IAddressMap)AddressMapStrategy).USAStates;

        #endregion
    }


    public class AddressMappingSearch
    {
        public static async Task<IEnumerable> Search(string zip, string state, DateTime? effectiveFrom,
            DateTime? effectiveTo, int take = 300)
        {
            return await ((IAddressMap)AddressMappingCache.Current).Search(zip, state, effectiveFrom, effectiveTo,
                take);
        }
    }
}