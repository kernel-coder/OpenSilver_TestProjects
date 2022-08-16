using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Virtuoso.Portable.Model
{
    [ProtoContract(SkipConstructor = true)]
    public class CachedAddressMapping
    {
        [ProtoMember(1)]
        public int AddressMapKey { get; set; }
        [ProtoMember(2)]
        public String CBSAHomeHealth { get; set; }
        [ProtoMember(3)]
        public String CBSAHospice { get; set; }
        [ProtoMember(4)]
        public DateTime CBSAHomeHealthEffectiveFrom { get; set; }
        [ProtoMember(5)]
        public DateTime CBSAHomeHealthEffectiveTo { get; set; }
        [ProtoMember(6)]
        public DateTime CBSAHospiceEffectiveFrom { get; set; }
        [ProtoMember(7)]
        public DateTime CBSAHospiceEffectiveTo { get; set; }
        [ProtoMember(8)]
        public String CountyCode { get; set; }
        [ProtoMember(9)]
        public String ZipCode { get; set; }
        [ProtoMember(10)]
        public string City { get; set; }
        [ProtoMember(11)]
        public String State { get; set; }
    }

    public class AddressComparer : EqualityComparer<CachedAddressMapping>
    {
        public override bool Equals(CachedAddressMapping b1, CachedAddressMapping b2)
        {
            if (b1.CBSAHomeHealth == b2.CBSAHomeHealth
                && b1.CBSAHospice == b2.CBSAHospice
                && b1.ZipCode == b2.ZipCode.Substring(0,5)
                && b1.CountyCode == b2.CountyCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode(CachedAddressMapping ca)
        {
            return (ca.CBSAHomeHealth + ca.CBSAHospice + ca.ZipCode + ca.CountyCode).GetHashCode();
        }
    }
}
