#region Usings

using Virtuoso.Core.Services;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class UserAlertsJoinCount
    {
        public string DisplayNamePlusCount => string.Format("{0} ({1})", DisplayName, Count);
    }

    public partial class UserAlertsJoin
    {
        private string __fullNameWithMRN = string.Empty;
        private bool _isFlagged;

        public string FullNameWithMRN
        {
            get
            {
                if (__fullNameWithMRN == string.Empty)
                {
                    var namewithSuffix = __GetNameWithSuffix();
                    var name =
                        string.Format("{0}{1}",
                            namewithSuffix,
                            MRN == null ? "" : " - " + MRN.Trim()
                        ).Trim();

                    if (name == "," || name == "")
                    {
                        name = " ";
                    }

                    if (name == "All,")
                    {
                        name = "All";
                    }

                    __fullNameWithMRN = name;
                }

                return __fullNameWithMRN;
            }
        }

        public string FullNameWithSuffix
        {
            get
            {
                var name = __GetNameWithSuffix();
                if (name == "," || name == "")
                {
                    name = " ";
                }

                if (name == "All,")
                {
                    name = "All";
                }

                return name;
            }
        }

        public string FullNameInformal
        {
            get
            {
                var name = string.Format("{0} {1}", !string.IsNullOrEmpty(NickName) ? NickName : FirstName, LastName)
                    .Trim();
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

        public string PatientName => FirstName + " " + LastName;

        public int? MaxServiceLineGroupingKey =>
            ServiceLineGroup4Key.HasValue
                ? ServiceLineGroup4Key
                : ServiceLineGroup3Key.HasValue
                    ? ServiceLineGroup3Key
                    : ServiceLineGroup2Key.HasValue
                        ? ServiceLineGroup2Key
                        : ServiceLineGroup1Key.HasValue
                            ? ServiceLineGroup1Key
                            : ServiceLineGroup0Key;

        public int IsDueOrLate
        {
            get
            {
                var retValue = 0;
                if (IsDue)
                {
                    retValue++;
                }

                if (IsLate)
                {
                    retValue++;
                }

                return retValue;
            }
        }

        public bool IsFlagged
        {
            get { return _isFlagged || FlaggedDateTime != null; }
            set
            {
                UserAlertManager.Instance.MarkAlertAsFlagged(this, value);
                _isFlagged = value;
            }
        }

        public string CareCoordinatorFullNameWithSuffix
        {
            get
            {
                var name = __CareCoordinatorGetNameWithSuffix();
                if (name == "," || name == "")
                {
                    name = " ";
                }

                if (name == "All,")
                {
                    name = "All";
                }

                return name;
            }
        }

        private string __GetNameWithSuffix()
        {
            var __firstName = string.Format("{0}{1}",
                FirstName == null ? "" : " " + FirstName.Trim(),
                MiddleInitial == null ? "" : " " + MiddleInitial.Trim());

            if (NickName != null && NickName.Trim() != "")
            {
                __firstName = " " + NickName;
            }

            return string.Format("{0}{1},{2}",
                LastName == null ? "" : LastName.Trim(),
                Suffix == null ? "" : " " + Suffix.Trim(),
                __firstName);
        }

        private string __CareCoordinatorGetNameWithSuffix()
        {
            var __firstName = string.Format("{0}{1}",
                CareCoordinatorFirstName == null ? "" : " " + CareCoordinatorFirstName.Trim(),
                CareCoordinatorMiddleName == null ? "" : " " + CareCoordinatorMiddleName.Trim());

            if (CareCoordinatorFriendlyName != null && CareCoordinatorFriendlyName.Trim() != "")
            {
                __firstName = " " + CareCoordinatorFriendlyName;
            }

            return string.Format("{0}{1},{2}",
                CareCoordinatorLastName == null ? "" : CareCoordinatorLastName.Trim(),
                CareCoordinatorSuffix == null ? "" : " " + CareCoordinatorSuffix.Trim(),
                __firstName);
        }
    }
}