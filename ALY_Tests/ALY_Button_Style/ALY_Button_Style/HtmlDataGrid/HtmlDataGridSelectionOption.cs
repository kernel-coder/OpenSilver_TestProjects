using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace OSFControls
{
    public enum HtmlDataGridSelectionMode
    {
        [EnumMember(Value = "single")]
        Single,

        [EnumMember(Value = "multi")]
        Multiple,

        [EnumMember(Value = "os")]
        System
    }

    internal class HtmlDataGridSelectionOption
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public HtmlDataGridSelectionMode Style { get; set; }

        public HtmlDataGridSelectionOption(HtmlDataGridSelectionMode style)
        {
            Style = style;
        }
    }
}
