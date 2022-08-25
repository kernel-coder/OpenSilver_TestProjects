#region Usings

using Virtuoso.Core.Cache;
using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class InsuranceAddress
    {
        public string CityStateZip => FormatHelper.FormatCityStateZip(City, StateCodeCode, ZipCode);

        public string StateCodeCode => CodeLookupCache.GetCodeFromKey(StateCode);

        public string TypeDescription => CodeLookupCache.GetCodeDescriptionFromKey(Type);

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

        partial void OnTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TypeDescription");
        }
    }
}