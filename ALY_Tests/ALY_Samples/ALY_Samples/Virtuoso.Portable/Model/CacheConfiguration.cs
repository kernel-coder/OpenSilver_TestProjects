using System;

namespace Virtuoso.Portable.Model
{
    public class CacheConfiguration
    {
        public string Name { get; set; }
        public DateTime Anchor { get; set; }
        public Nullable<DateTime> LastUpdatedDate { get; set; }
        public long Ticks { get; set; }
        public int TotalRecords { get; set; }
        public bool CacheLoadCompleted { get; set; }
    }
}
