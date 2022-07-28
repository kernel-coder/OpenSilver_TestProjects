#region Usings

using System.Collections;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class OrderTrackingGroup
    {
        // Used to display * in SearchResultsView
        public string IsInactiveIndicator
        {
            get
            {
                if (Inactive)
                {
                    return "*";
                }

                return string.Empty;
            }
        }

        public string FacilityName
        {
            get
            {
                var facility = FacilityCache.GetFacilityFromKey(FacilityKey);
                return facility == null ? null : facility.Name;
            }
        }

        public string FacilityBranchName
        {
            get
            {
                var branch = FacilityCache.GetFacilityBranchFromKey(FacilityBranchKey);
                return branch == null ? null : branch.BranchName;
            }
        }

        public void NotifyPropertyChanged(string PropertyName)
        {
            RaisePropertyChanged(PropertyName);
        }
    }

    public partial class OrderTrackingGroupDetail
    {
        private IEnumerable counties;

        public IEnumerable Counties
        {
            get { return counties; }
            set
            {
                counties = value;
                RaisePropertyChanged("Counties");
            }
        }
    }
}