#region Usings

using System;
using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Core.Alerts
{
    public class AlertPatient : IComparable
    {
        private string __fullNameWithMRN = string.Empty;
        public int PatientKey { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
        public string Suffix { get; set; }
        public string MRN { get; set; }

        public string FullName => FormatHelper.FormatName(LastName, FirstName, MiddleName);

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

        public string FullNameNoComma => FullName.TrimEnd(Convert.ToChar(","));

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

        public int CompareTo(object obj)
        {
            var compareObj = obj as AlertPatient;
            if (compareObj == null)
            {
                return 1; // A null value means that this object is greater.
            }

            return FullNameWithMRN.CompareTo(compareObj.FullNameWithMRN);
        }

        private string __GetNameWithSuffix()
        {
            var __firstName = string.Format("{0}{1}",
                FirstName == null ? "" : " " + FirstName.Trim(),
                MiddleName == null ? "" : " " + MiddleName.Trim());

            if (NickName != null && NickName.Trim() != "")
            {
                __firstName = " " + NickName;
            }

            return string.Format("{0}{1},{2}",
                LastName == null ? "" : LastName.Trim(),
                Suffix == null ? "" : " " + Suffix.Trim(),
                __firstName);
        }

        public override string ToString()
        {
            return FullNameWithMRN;
        }

        public override int GetHashCode()
        {
            return FullNameWithMRN.GetHashCode();
        }

        public bool Equals(AlertPatient other)
        {
            if (other == null)
            {
                return false;
            }

            return FullNameWithMRN.Equals(other.FullNameWithMRN);
        }
    }
}