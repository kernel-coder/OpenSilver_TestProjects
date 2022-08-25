#region Usings

using Virtuoso.Core.Cache;
using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class OasisHeader
    {
        partial void OnOasisHeaderNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TabHeader");
        }

        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(OasisHeaderName))
                {
                    return string.Format("CMS Header - {0}", OasisHeaderName.Trim());
                }

                return (IsNew) ? "New CMS Header" : "Edit CMS Header";
            }
        }

        partial void OnCityChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
        }

        partial void OnStateCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
            RaisePropertyChanged("StateCodeCode");
        }

        partial void OnZipCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
        }

        public string StateCodeCode => CodeLookupCache.GetCodeFromKey(StateCode);
        public string CityStateZip => FormatHelper.FormatCityStateZip(City, StateCodeCode, ZipCode);

        partial void OnBranchStateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("BranchStateCode");
        }

        public string BranchStateCode => CodeLookupCache.GetCodeFromKey(BranchState);
    }
}