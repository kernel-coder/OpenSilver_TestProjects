#region Usings

using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class Employer
    {
        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(EmployerName))
                {
                    return string.Format("Employer {0}", EmployerName.Trim());
                }

                return IsNew ? "New Employer" : "Edit Facility";
            }
        }

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

    public partial class EmployerContact
    {
        public string FullNameInformal
        {
            get
            {
                if (string.IsNullOrEmpty(FirstName))
                {
                    return LastName.Trim();
                }

                return (FirstName.Trim() + " " + LastName).Trim();
            }
        }

        public string FullName => FormatHelper.FormatName(LastName, FirstName, MiddleInitial);

        partial void OnLastNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullNameInformal");
            RaisePropertyChanged("FullName");
        }

        partial void OnFirstNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullNameInformal");
            RaisePropertyChanged("FullName");
        }

        partial void OnMiddleInitialChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullNameInformal");
            RaisePropertyChanged("FullName");
        }
    }
}