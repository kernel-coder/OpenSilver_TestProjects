using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OSFControls
{
    internal class HtmlDataGridOptions
    {
        [JsonProperty("paging")]
        public bool AllowPaging { get; set; }

        [JsonProperty("pageLength")]
        public int PageSize { get;set; }

        [JsonProperty("searching")]
        public bool AllowSearching { get; set; }

        [JsonProperty("ordering")]
        public bool CanSortColumn { get; set; }

        [JsonProperty("colReorder")]
        public bool CanReorderColumn { get; set; }

        public IEnumerable<HtmlDataGridColumn> Columns { get; set; }

        [JsonProperty("data")]
        public IEnumerable Data { get; set; }

        public double? ScrollY { get; set; }
        
        public bool AutoWidth { get; set; }

        [JsonProperty("select")]
        public HtmlDataGridSelectionOption SelectionOption { get; set; }
        
        [JsonProperty("columnDefs")]
        public IEnumerable<HtmlDataGridColumnDefinition> ColumnDefitions { get; set; }

        [JsonProperty("lengthChange")]
        public bool CanChangePageSize { get; set; } = false;

        [JsonProperty("info")]
        public bool ShowInfo { get; set; }

        [JsonProperty("headerCallback")]
        public string HeaderCallBack { get; set; }
    }
}
