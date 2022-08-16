#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Virtuoso.Core;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Model;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Portable;
using Virtuoso.Portable.Database;

#endregion

namespace Virtuoso.Client.Cache
{
    public class AddressMapFlatFileStrategyV2 : FlatFileCacheBase<Virtuoso.Portable.Model.CachedAddressMapping>,
        IAddressMap
    {
        public AddressMapFlatFileStrategyV2(ILogger logManager)
            : base(logManager)
        {
            CacheName = ReferenceTableName.AddressMapping;
            DatabaseWrapper = DatabaseService.Current.DatabaseFor(VirtuosoDatabase.AddressMapping);
        }

#if !OPENSILVER
        protected override void DeserializeData(RecordSet rs)
        {
            var entity = NewObject();
            Portable.Extensions.AddressMapExtensions.RecordSetToCachedAddressMapping(rs, entity);
            _DataList.Add(entity);
        }
#endif

        #region IAddressMap

        public async Task<IEnumerable> Search(string zip, string state, DateTime? effectiveFrom, DateTime? effectiveTo,
            int take = 300)
        {
            //if (this._DataList.Any() == false)  //most likely DeferLoad == true
            //{
            //    TotalRecords = 0;
            //    this.LoadData();
            //    var stats = this.DatabaseWrapper.DBInfo.GetStats(this.CacheName);
            //    stats.TotalRecords = TotalRecords;
            //    this.DatabaseWrapper.DBInfo.SaveStats(stats);
            //}

            await EnsureDataLoadedFromDisk();

            //#if DEBUG
            //            Stopwatch timer = Stopwatch.StartNew();
            //#endif

            //            System.Diagnostics.Debug.WriteLine(string.Format("Begin SEARCH of address database.  Time: {0}", DateTime.Now));

            IEnumerable<Virtuoso.Portable.Model.CachedAddressMapping> items = null;
            if (!string.IsNullOrEmpty(zip) || !string.IsNullOrEmpty(state))
            {
                // start with all
                var qry = _DataList.AsQueryable();

                // filter by zip
                if (!string.IsNullOrEmpty(zip))
                {
                    qry = qry.Where(i => i.ZipCode != null && i.ZipCode.Contains(zip));
                }

                // filter by state
                if (!string.IsNullOrEmpty(state))
                {
                    qry = qry.Where(i => i.State != null && i.State.Equals(state));
                }

                items = qry
                    .Select(x => new Virtuoso.Portable.Model.CachedAddressMapping
                    {
                        //AddressKey = x.AddressMapKey,
                        AddressMapKey = x.AddressMapKey,
                        City = x.City,
                        State = x.State,
                        ZipCode = x.ZipCode,
                        CountyCode = x.CountyCode,
                        CBSAHospice = x.CBSAHospice,
                        CBSAHomeHealth = x.CBSAHomeHealth,
                        CBSAHomeHealthEffectiveFrom = (x.CBSAHomeHealthEffectiveFrom != null)
                            ? x.CBSAHomeHealthEffectiveFrom
                            : DateTime.MinValue,
                        CBSAHomeHealthEffectiveTo = (x.CBSAHomeHealthEffectiveTo != null)
                            ? x.CBSAHomeHealthEffectiveTo
                            : DateTime.MinValue,
                        CBSAHospiceEffectiveFrom = (x.CBSAHospiceEffectiveFrom != null)
                            ? x.CBSAHospiceEffectiveFrom
                            : DateTime.MinValue,
                        CBSAHospiceEffectiveTo = (x.CBSAHospiceEffectiveTo != null)
                            ? x.CBSAHospiceEffectiveTo
                            : DateTime.MinValue
                    })
                    .ToList();

                var addressMappings = SetCBSACodes(effectiveFrom, effectiveTo, items);

                items = addressMappings
                    .Distinct(new Virtuoso.Portable.Model.AddressComparer())
                    .Take(take)
                    .OrderBy(i => i.ZipCode);

                if (items.Count() >= take)
                {
                    var cic = new Virtuoso.Portable.Model.CachedAddressMapping
                    {
                        ZipCode = "...",
                        CBSAHospice = "...",
                        CBSAHomeHealth = "...",
                        City = "...",
                        CountyCode = "<Over " + take.ToString(CultureInfo.InvariantCulture) +
                                     " matches, narrow search criteria...>"
                    };
                    var list = items.ToList();
                    list.Add(cic);
                    items = list.AsEnumerable();
                }
            }
            else
            {
                //return all Addresses
                items = _DataList
                    .Select(x => new Virtuoso.Portable.Model.CachedAddressMapping
                    {
                        //AddressKey = x.AddressMapKey,
                        AddressMapKey = x.AddressMapKey,
                        City = x.City,
                        State = x.State,
                        ZipCode = x.ZipCode,
                        CountyCode = x.CountyCode,
                        CBSAHospice = x.CBSAHospice,
                        CBSAHomeHealth = x.CBSAHomeHealth,
                        CBSAHomeHealthEffectiveFrom = (x.CBSAHomeHealthEffectiveFrom != null)
                            ? x.CBSAHomeHealthEffectiveFrom
                            : DateTime.MinValue,
                        CBSAHomeHealthEffectiveTo = (x.CBSAHomeHealthEffectiveTo != null)
                            ? x.CBSAHomeHealthEffectiveTo
                            : DateTime.MinValue,
                        CBSAHospiceEffectiveFrom = (x.CBSAHospiceEffectiveFrom != null)
                            ? x.CBSAHospiceEffectiveFrom
                            : DateTime.MinValue,
                        CBSAHospiceEffectiveTo = (x.CBSAHospiceEffectiveTo != null)
                            ? x.CBSAHospiceEffectiveTo
                            : DateTime.MinValue
                    })
                    .ToList();
            }

            var cachedAddressMappings = items as IList<Virtuoso.Portable.Model.CachedAddressMapping> ?? items.ToList();

            //#if DEBUG
            //            timer.Stop();
            //            var milli = timer.ElapsedMilliseconds;
            //            var seconds = timer.ElapsedMilliseconds / 1000D;
            //#endif

            //            System.Diagnostics.Debug.WriteLine(string.Format("End SEARCH of address database.  Elapsed seconds: {0}", seconds.ToString("0.##")));

            var ret = cachedAddressMappings
                .Distinct()
                .Take(take)
                .OrderBy(i => i.ZipCode)
                .ThenBy(i => i.AddressMapKey);

            return ret;
        }

        private IEnumerable<ZIPCode> _ZIPCodes;

        public async Task<IEnumerable<ZIPCode>> GetZIPCodes()
        {
            if (_ZIPCodes == null)
            {
                await EnsureDataLoadedFromDisk();
                _ZIPCodes = _DataList.Where(a => !String.IsNullOrEmpty(a.ZipCode))
                    .Select(a => new ZIPCode
                        { ZipCode = a.ZipCode, State = a.State, City = a.City, County = a.CountyCode });
            }

            return _ZIPCodes;
        }

        private IEnumerable<COUNTYCode> _COUNTYCodes;

        public async Task<IEnumerable<COUNTYCode>> GetCOUNTYCodes()
        {
            if (_COUNTYCodes == null)
            {
                await EnsureDataLoadedFromDisk();
                _COUNTYCodes = _DataList.Where(a => !String.IsNullOrEmpty(a.CountyCode))
                    .Select(a => new COUNTYCode
                    {
                        County = a.CountyCode,
                        State = a.State
                    }).DistinctBy(x => x.County + x.State);
            }

            return _COUNTYCodes;
        }

        private List<USAState> _States;

        public List<USAState> USAStates
        {
            get
            {
                if (_States == null)
                {
                    //EnsureDataLoadedFromDisk();
                    //_States = this._DataList.Where(a => !String.IsNullOrEmpty(a.State)).Select(a => new USAState(a.State, null)).Distinct().ToList();
                    _States = new List<USAState>();
                    _States.Add(new USAState("AL", "Alabama"));
                    _States.Add(new USAState("AK", "Alaska"));
                    _States.Add(new USAState("AZ", "Arizona"));
                    _States.Add(new USAState("AR", "Arkansas"));
                    _States.Add(new USAState("CA", "California"));
                    _States.Add(new USAState("CO", "Colorado"));
                    _States.Add(new USAState("CT", "Connecticut"));
                    _States.Add(new USAState("DE", "Delaware"));
                    _States.Add(new USAState("DC", "District of Columbia"));
                    _States.Add(new USAState("FL", "Florida"));
                    _States.Add(new USAState("GA", "Georgia"));
                    _States.Add(new USAState("HI", "Hawaii"));
                    _States.Add(new USAState("ID", "Idaho"));
                    _States.Add(new USAState("IL", "Illinois"));
                    _States.Add(new USAState("IN", "Indiana"));
                    _States.Add(new USAState("IA", "Iowa"));
                    _States.Add(new USAState("KS", "Kansas"));
                    _States.Add(new USAState("KY", "Kentucky"));
                    _States.Add(new USAState("LA", "Louisiana"));
                    _States.Add(new USAState("ME", "Maine"));
                    _States.Add(new USAState("MD", "Maryland"));
                    _States.Add(new USAState("MI", "Michigan"));
                    _States.Add(new USAState("MN", "Minnesota"));
                    _States.Add(new USAState("MI", "Mississippi"));
                    _States.Add(new USAState("MO", "Missouri"));
                    _States.Add(new USAState("MT", "Montana"));
                    _States.Add(new USAState("NE", "Nebraska"));
                    _States.Add(new USAState("NV", "Nevada"));
                    _States.Add(new USAState("NH", "New Hampshire"));
                    _States.Add(new USAState("NJ", "New Jersey"));
                    _States.Add(new USAState("NM", "New Mexico"));
                    _States.Add(new USAState("NY", "New York"));
                    _States.Add(new USAState("NC", "North Carolina"));
                    _States.Add(new USAState("ND", "North Dakota"));
                    _States.Add(new USAState("OH", "Ohio"));
                    _States.Add(new USAState("OK", "Oklahoma"));
                    _States.Add(new USAState("OR", "Oregon"));
                    _States.Add(new USAState("PA", "Pennsylvania"));
                    _States.Add(new USAState("RI", "Rhode Island"));
                    _States.Add(new USAState("SC", "South Carolina"));
                    _States.Add(new USAState("SD", "South Dakota"));
                    _States.Add(new USAState("TN", "Tennessee"));
                    _States.Add(new USAState("TX", "Texas"));
                    _States.Add(new USAState("UT", "Utah"));
                    _States.Add(new USAState("VT", "Vermont"));
                    _States.Add(new USAState("VA", "Virginia"));
                    _States.Add(new USAState("WA", "Washington"));
                    _States.Add(new USAState("WV", "West Virginia"));
                    _States.Add(new USAState("WI", "Wisconsin"));
                    _States.Add(new USAState("WY", "Wyoming"));
                }

                return _States;
            }
        }


        ////private IEnumerable<USAState> _States;
        ////public IEnumerable<USAState> States {
        ////    get {
        ////        if (_States == null) {
        ////            EnsureDataLoadedFromDisk();
        ////            _States = this._DataList.Where(a => !String.IsNullOrEmpty(a.State)).Select(a => new USAState() { StateCode = a.State }).Distinct();
        ////        }
        ////        return _States;
        ////    }

        ////}

        #endregion

        #region Search Helpers

        //called by public SearchTakeZipCityState(...)
        private IEnumerable<Virtuoso.Portable.Model.CachedAddressMapping> SetCBSACodes(DateTime? effectiveFrom,
            DateTime? effectiveTo, IEnumerable<Virtuoso.Portable.Model.CachedAddressMapping> items)
        {
            var addressMappings = items as IList<Virtuoso.Portable.Model.CachedAddressMapping> ?? items.ToList();

            // Filter out AddressMappings that have an expired EffectiveTo date
            if (addressMappings != null)
            {
                addressMappings = addressMappings.Where(a =>
                        a.CBSAHomeHealthEffectiveTo >= DateTime.Now.Date &&
                        a.CBSAHospiceEffectiveTo >= DateTime.Now.Date)
                    .ToList();
            }

            var groupedAddressMappings = new List<Virtuoso.Portable.Model.CachedAddressMapping>();

            DateTime minDate = DateTime.Parse("1/1/1901");

            // check effective from
            var effectiveFromNow = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            if (effectiveFrom.HasValue)
            {
                effectiveFromNow = effectiveFrom.Value;
            }

            foreach (var addressMapping in addressMappings)
            {
                bool matchFound = false;
                foreach (var groupedAddressMapping in groupedAddressMappings)
                    if (MatchAddressMapping(addressMapping, groupedAddressMapping))
                    {
                        matchFound = true;
                    }

                if (!matchFound)
                {
                    groupedAddressMappings.Add(addressMapping);
                }
            }

            foreach (var groupedAddressMapping in groupedAddressMappings)
            {
                // Get the 'From' values for Home Health
                addressMappings = addressMappings.OrderBy(f => f.CBSAHomeHealthEffectiveFrom).ToList();

                foreach (var cachedAddressMapping in addressMappings)
                    if (MatchAddressMapping(cachedAddressMapping, groupedAddressMapping))
                    {
                        if (NoEffectiveDates(cachedAddressMapping, minDate))
                        {
                            groupedAddressMapping.CBSAHomeHealth = cachedAddressMapping.CBSAHomeHealth;
                        }

                        if (cachedAddressMapping.CBSAHomeHealthEffectiveFrom <= effectiveFromNow)
                        {
                            groupedAddressMapping.CBSAHomeHealth = cachedAddressMapping.CBSAHomeHealth;
                        }
                    }

                // Get the 'From' values for Hospice
                addressMappings = addressMappings.OrderBy(f => f.CBSAHospiceEffectiveFrom).ToList();

                foreach (var cachedAddressMapping in addressMappings)
                    if (MatchAddressMapping(cachedAddressMapping, groupedAddressMapping))
                    {
                        if (NoEffectiveDates(cachedAddressMapping, minDate))
                        {
                            groupedAddressMapping.CBSAHospice = cachedAddressMapping.CBSAHospice;
                        }

                        if (cachedAddressMapping.CBSAHospiceEffectiveFrom <= effectiveFromNow)
                        {
                            groupedAddressMapping.CBSAHospice = cachedAddressMapping.CBSAHospice;
                        }
                    }

                if (effectiveTo != null)
                {
                    // Get the 'To' values for Home Health
                    addressMappings = addressMappings.OrderBy(f => f.CBSAHomeHealthEffectiveTo).ToList();

                    foreach (var cachedAddressMapping in addressMappings)
                        if (MatchAddressMapping(cachedAddressMapping, groupedAddressMapping))
                        {
                            if (cachedAddressMapping.CBSAHomeHealthEffectiveTo > effectiveTo)
                            {
                                groupedAddressMapping.CBSAHomeHealth = cachedAddressMapping.CBSAHomeHealth;
                            }
                        }

                    // Get the 'To' values for Hospice
                    addressMappings = addressMappings.OrderBy(f => f.CBSAHospiceEffectiveTo).ToList();

                    foreach (var cachedAddressMapping in addressMappings)
                        if (MatchAddressMapping(cachedAddressMapping, groupedAddressMapping))
                        {
                            if (cachedAddressMapping.CBSAHospiceEffectiveTo > effectiveTo)
                            {
                                groupedAddressMapping.CBSAHospice = cachedAddressMapping.CBSAHospice;
                            }
                        }
                }
            }

            return groupedAddressMappings;
        }

        //called by private SetCBSACodes(...)
        private bool NoEffectiveDates(Virtuoso.Portable.Model.CachedAddressMapping cachedAddressMapping,
            DateTime minDate)
        {
            return cachedAddressMapping.CBSAHomeHealthEffectiveFrom <= minDate &&
                   cachedAddressMapping.CBSAHomeHealthEffectiveTo <= minDate &&
                   cachedAddressMapping.CBSAHospiceEffectiveFrom <= minDate &&
                   cachedAddressMapping.CBSAHospiceEffectiveTo <= minDate;
        }

        //called by private SetCBSACodes(...)
        private bool MatchAddressMapping(Virtuoso.Portable.Model.CachedAddressMapping addressMappingA,
            Virtuoso.Portable.Model.CachedAddressMapping addressMappingB)
        {
            return addressMappingA.CountyCode == addressMappingB.CountyCode &&
                   addressMappingA.City == addressMappingB.City &&
                   addressMappingA.State == addressMappingB.State &&
                   addressMappingA.ZipCode == addressMappingB.ZipCode;
        }

        #endregion
    }
}