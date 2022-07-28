namespace Virtuoso.Server.Data
{
    public partial class CMSForm
    {
        // Used to display * inSearchResultsView
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