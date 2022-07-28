namespace Virtuoso.Server.Data
{
    public partial class ReferralSource
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

        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(FullName))
                {
                    return string.Format("Referral Contact {0}", FullName.Trim());
                }

                return IsNew ? "New Referral Contact" : "Edit Referral Contact";
            }
        }

        public string FullName
        {
            get
            {
                var name = (LastName + ", " + FirstName).Trim();
                if (name == "," || name == "")
                {
                    name = " ";
                }

                return name;
            }
        }

        public string FullNameInformal
        {
            get
            {
                var name = string.Format("{0} {1}", FirstName, LastName).Trim();
                if (name == null)
                {
                    return " ";
                }

                if (name.Trim() == "")
                {
                    return " ";
                }

                return name.Trim();
            }
        }

        partial void OnLastNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullName");
            RaisePropertyChanged("TabHeader");
        }

        partial void OnFirstNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullName");
            RaisePropertyChanged("TabHeader");
        }
    }
}