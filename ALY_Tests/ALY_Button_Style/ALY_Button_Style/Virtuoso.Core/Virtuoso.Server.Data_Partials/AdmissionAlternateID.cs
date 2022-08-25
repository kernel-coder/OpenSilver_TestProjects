#region Usings

using System;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionAlternateID
    {
        public string DropdownText => Issuer + (string.IsNullOrWhiteSpace(Issuer) ? "" : " : ") + TypeCode + " - " +
                                      Identifier + (IsInactiveBindTarget ? " - (inactive)" : "");

        public bool IsInactiveBindTarget
        {
            get { return InactiveDateTime.HasValue; }
            set
            {
                if (value)
                {
                    if (!InactiveDateTime.HasValue)
                    {
                        InactiveDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                    }
                }
                else
                {
                    InactiveDateTime = null;
                    RaisePropertyChanged("DropdownText");
                }
            }
        }

        public string AlternateIDLabel => Issuer + (string.IsNullOrWhiteSpace(Issuer) ? "" : " ") + TypeCode;

        partial void OnIssuerChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DropdownText");
        }

        partial void OnTypeCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DropdownText");
        }

        partial void OnIdentifierChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DropdownText");
        }

        partial void OnInactiveDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DropdownText");
        }

        public bool IsActiveAsOfDate(DateTime date)
        {
            if (IssueDateTime == null && ExpireDateTime == null)
            {
                return true;
            }

            if (ExpireDateTime == null && IssueDateTime != null && ((DateTime)IssueDateTime).Date <= date)
            {
                return true;
            }

            if (IssueDateTime == null && ExpireDateTime != null && ((DateTime)ExpireDateTime).Date >= date)
            {
                return true;
            }

            if (IssueDateTime != null && ((DateTime)IssueDateTime).Date <= date &&
                ExpireDateTime != null && ((DateTime)ExpireDateTime).Date >= date)
            {
                return true;
            }

            return false;
        }
    }
}