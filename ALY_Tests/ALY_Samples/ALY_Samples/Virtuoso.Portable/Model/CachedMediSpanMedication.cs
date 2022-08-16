using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Virtuoso.Portable.Model
{
    public enum CachedMediSpanMedicationType
    {
        DisposibleDrug = 1,
        RoutedDrug = 2
    }

    [ProtoContract(SkipConstructor = true)]
    public class CachedMediSpanMedication
    {
        [ProtoMember(1)]
        public int MedKey { get; set; }
        [ProtoMember(2)]
        public Nullable<int> DDID { get; set; }
        [ProtoMember(3)]
        public int RDID { get; set; }
        [ProtoMember(4)]
        public int MedType { get; set; }
        [ProtoMember(5)]
        public string Name { get; set; }
        [ProtoMember(6)]
        public string MedUnit { get; set; }
        [ProtoMember(7)]
        public string Route { get; set; }
        [ProtoMember(8)]
        public int RXType { get; set; }
        [ProtoMember(9)]
        public bool MedNarcotic { get; set; }
        [ProtoMember(10)]
        public Nullable<DateTime> ODate { get; set; }
    }
}
