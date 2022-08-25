namespace Virtuoso.Server.Data
{
    public partial class InsuranceGroup
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
    }
}