namespace Virtuoso.Server.Data
{
    public partial class FunctionalDeficit
    {
        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Description))
                {
                    return Description.Trim();
                }

                return IsNew ? "New Functional Deficit" : "Edit Functional Deficit";
            }
        }

        partial void OnDescriptionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TabHeader");
        }
    }
}