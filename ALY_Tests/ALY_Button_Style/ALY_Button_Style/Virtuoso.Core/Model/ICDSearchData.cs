namespace Virtuoso.Core.Model
{
    public class ICDSearchData
    {
        public string SearchValues { get; set; }
        public string ICDMode { get; set; }
        public int Version { get; set; }
        public string CategoryMinCode { get; set; }
        public string CategoryMaxCode { get; set; }
        public string SubCategoryMinCode { get; set; }
        public string SubCategoryMaxCode { get; set; }
        public bool ShowLastYearCodes { get; set; }
    }
}