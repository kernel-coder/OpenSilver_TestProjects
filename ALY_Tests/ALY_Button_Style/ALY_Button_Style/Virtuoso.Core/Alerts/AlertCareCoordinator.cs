#region Usings

using System;
using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Core.Alerts
{
    public class AlertCareCoordinator : IComparable
    {
        public Guid? UserId { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string FriendlyName { get; set; }
        public string Suffix { get; set; }

        public string FullName => FormatHelper.FormatName(LastName, FirstName, MiddleName);

        public string FullNameInformal
        {
            get
            {
                var name = string.Format("{0} {1}", !string.IsNullOrEmpty(FriendlyName) ? FriendlyName : FirstName,
                    LastName).Trim();
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

        public string FullNameNoComma => FullName.TrimEnd(Convert.ToChar(","));

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

        public int CompareTo(object obj)
        {
            var compareObj = obj as AlertCareCoordinator;
            if (compareObj == null)
            {
                return 1; // A null value means that this object is greater.
            }

            return FullNameWithSuffix.CompareTo(compareObj.FullNameWithSuffix);
        }

        private string __GetNameWithSuffix()
        {
            var __firstName = string.Format("{0}{1}",
                FirstName == null ? "" : " " + FirstName.Trim(),
                MiddleName == null ? "" : " " + MiddleName.Trim());

            if (FriendlyName != null && FriendlyName.Trim() != "")
            {
                __firstName = " " + FriendlyName;
            }

            return string.Format("{0}{1},{2}",
                LastName == null ? "" : LastName.Trim(),
                Suffix == null ? "" : " " + Suffix.Trim(),
                __firstName);
        }

        public override string ToString()
        {
            return FullNameWithSuffix;
        }

        public override int GetHashCode()
        {
            return FullNameWithSuffix.GetHashCode();
        }

        public bool Equals(AlertCareCoordinator other)
        {
            if (other == null)
            {
                return false;
            }

            return FullNameWithSuffix.Equals(other.FullNameWithSuffix);
        }
    }
}