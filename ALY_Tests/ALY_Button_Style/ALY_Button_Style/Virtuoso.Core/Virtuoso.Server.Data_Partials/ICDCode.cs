#region Usings

using System;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class ICDCode
    {
        public bool Inactive => DateTime.Now.Date < EffectiveFrom ||
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

        public string Description => Short;
    }
}