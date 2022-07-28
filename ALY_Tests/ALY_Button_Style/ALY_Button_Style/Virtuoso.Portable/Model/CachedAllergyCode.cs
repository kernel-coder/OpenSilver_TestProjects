using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Virtuoso.Portable.Model
{
    [ProtoContract(SkipConstructor = true)]
    public class CachedAllergyCode
    {
        [ProtoMember(1)]
        public int AllergyCodeKey { get; set; }
        [ProtoMember(2)]
        public string DisplayName { get; set; }
        [ProtoMember(3)]
        public string UNII { get; set; }
        [ProtoMember(4)]
        public string SubstanceName { get; set; }  //currently using this 'description' value throughout the application
        [ProtoMember(5)]
        public string PreferredSubstanceName { get; set; }  //currently using this 'description' value throughout the application
        [ProtoMember(6)]
        public Nullable<DateTime> EffectiveFrom { get; set; }
        [ProtoMember(7)]
        public Nullable<DateTime> EffectiveThru { get; set; }
        [ProtoMember(8)]
        public string FullText { get; set; }
    }
}
