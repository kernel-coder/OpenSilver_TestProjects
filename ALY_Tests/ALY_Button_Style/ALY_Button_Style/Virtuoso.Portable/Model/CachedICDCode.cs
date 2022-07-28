using ProtoBuf;
using System;

namespace Virtuoso.Portable.Model
{
    [ProtoContract(SkipConstructor = true)]
    public class CachedICDCode
    {
        [ProtoMember(1)]
        public int ICDCodeKey { get; set; }
        //public string DisplayName { get; set; }
        [ProtoMember(2)]
        public string Code { get; set; }
        [ProtoMember(3)]
        public int Version { get; set; }
        [ProtoMember(4)]
        public string Short { get; set; }
        [ProtoMember(5)]
        public Nullable<DateTime> EffectiveFrom { get; set; }
        [ProtoMember(6)]
        public Nullable<DateTime> EffectiveThru { get; set; }

        //[Ignore]
        //FYI: do not map this property to the flat file...
        //public string Description { get { return string.IsNullOrEmpty(DisplayName) ? Short : DisplayName; } }
        public string Description { get { return Short; } }

        [ProtoMember(7)]
        public short GEMSCount { get; set; }
        [ProtoMember(8)]
        public bool Diagnosis { get; set; }
        [ProtoMember(9)]
        public bool? RequiresAdditionalDigit { get; set; }
        [ProtoMember(10)]
        public string PDGMClinicalGroup { get; set; }
        [ProtoMember(11)]
        public string PDGMComorbidityGroup { get; set; }
        [ProtoMember(12)]
        public string FullText { get; set; }
        public string EffectiveThruString { get { return (EffectiveThru == null) ? null : ((DateTime)EffectiveThru).ToShortDateString(); } }
    }
}
