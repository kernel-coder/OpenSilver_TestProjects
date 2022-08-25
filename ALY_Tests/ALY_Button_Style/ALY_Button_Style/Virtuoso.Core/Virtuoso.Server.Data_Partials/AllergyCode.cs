#region Usings

using System;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AllergyCode
    {
        public bool Inactive => EffectiveFrom.HasValue && DateTime.Now.Date < EffectiveFrom.Value ||
                                EffectiveThru.HasValue && DateTime.Now.Date > EffectiveThru.Value;

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

        public string Description => string.IsNullOrEmpty(DisplayName) ? PreferredSubstanceName : DisplayName;
    }
}