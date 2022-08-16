#region Usings

using System;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class HighRiskMedication
    {
        public string ServiceLineName => ServiceLineCache.GetNameFromServiceLineKey(ServiceLineKey);

        public bool Inactive => EffectiveFromDate.HasValue && DateTime.Now.Date < EffectiveFromDate.Value ||
                                EffectiveThruDate.HasValue && DateTime.Now.Date > EffectiveThruDate.Value;

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

        partial void OnServiceLineKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ServiceLineName");
        }
    }
}