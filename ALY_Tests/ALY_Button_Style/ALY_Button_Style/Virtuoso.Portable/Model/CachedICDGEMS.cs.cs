using ProtoBuf;
using System;

namespace Virtuoso.Portable.Model
{
    [ProtoContract(SkipConstructor = true)]
    public class CachedICDGEMS
    {
        [ProtoMember(1)]
        public int ICDGEMSKey { get; set; }
        [ProtoMember(2)]
        public int Version { get; set; }
        [ProtoMember(3)]
        public string Code9 { get; set; }
        [ProtoMember(4)]
        public string Code10 { get; set; }
        [ProtoMember(5)]
        public string Short9 { get; set; }
        [ProtoMember(6)]
        public string Short10 { get; set; }
        [ProtoMember(7)]
        public bool ApproximateFlag { get; set; }
        [ProtoMember(8)]
        public bool NoMapFlag { get; set; }
        [ProtoMember(9)]
        public bool CombinationFlag { get; set; }
        [ProtoMember(10)]
        public short Scenario { get; set; }
        [ProtoMember(11)]
        public short ChoiceList { get; set; }
    }
}