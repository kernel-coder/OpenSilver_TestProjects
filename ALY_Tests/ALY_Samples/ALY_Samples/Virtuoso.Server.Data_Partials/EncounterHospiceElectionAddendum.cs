#region Usings

using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class EncounterHospiceElectionAddendum
    {
        private bool canEdit;
        private bool paperClip;

        public bool RequiresSignature
        {
            get
            {
                if (string.IsNullOrWhiteSpace(RequestedBy))
                {
                    return false;
                }

                if (RequestedBy == "Patient" || RequestedBy == "Representative")
                {
                    return true;
                }

                return false;
            }
        }

        public bool PaperClip
        {
            get { return paperClip; }
            set
            {
                paperClip = value;
                RaisePropertyChanged("PaperClip");
            }
        }

        public bool CanEdit
        {
            get { return canEdit; }
            set
            {
                canEdit = value;
                RaisePropertyChanged("CanEdit");
            }
        }

        public string AddendumPurposeBlirb =>
            Version == 1
                ? "The purpose of this addendum is to notify the requesting Medicare beneficiary (or representative), in writing, of those conditions, items, services, and drugs not covered by the hospice because the hospice has determined they are unrelated to the terminal illness and related conditions.  If this notification is requested on the effective date of the hospice election (that is, on the start date of hospice care), the hospice must provide you this form within <Bold>5</Bold> days.  If this form is requested at any point after the start date of hospice care, the hospice must provide you this form within <Bold>3</Bold> days."
                : "The purpose of this addendum is to notify the requesting Medicare beneficiary (or representative), in writing, of those conditions, items, services, and drugs not covered by the hospice because the hospice has determined they are unrelated to the terminal illness and related conditions.  If you request the addendum within the first <Bold>5</Bold> days of the hospice election (that is, in the first <Bold>5</Bold> days of the hospice election date), the hospice must provide you with this form within <Bold>5</Bold> days of the date of the request.  If you request this form at any point after the first <Bold>5</Bold> days of the hospice election, the hospice must provide you this form within <Bold>3</Bold> days.";

        public string ProxyDatedSignaturePresent
        {
            get { return DatedSignaturePresent == null ? null : DatedSignaturePresent == true ? "1" : "0"; }
            set
            {
                DatedSignaturePresent = value == null ? (bool?)null : value == "1" ? true : false;
                RaisePropertyChanged("DatedSignaturePresent");
            }
        }

        public bool ShowSigned => DatedSignaturePresent == null ? false : DatedSignaturePresent == true;

        public bool ShowNotSigned => DatedSignaturePresent == null ? false : DatedSignaturePresent == false;

        public string ReasonNotSignedDescription
        {
            get
            {
                if (ReasonNotSigned == null || ReasonNotSigned < 1)
                {
                    return "??";
                }

                var description = CodeLookupCache.GetCodeDescriptionFromKey(ReasonNotSigned);
                return string.IsNullOrWhiteSpace(description) ? "??" : description.Trim();
            }
        }

        public bool UsingRefusalReason
        {
            get
            {
                if (ShowNotSigned == false)
                {
                    return false;
                }

                if (ReasonNotSigned == null)
                {
                    return false;
                }

                var cl = CodeLookupCache.GetCodeLookupFromKey(ReasonNotSigned);
                if (cl == null || string.IsNullOrWhiteSpace(cl.ApplicationData))
                {
                    return false;
                }

                return cl.ApplicationData.Trim() == "1";
            }
        }

        partial void OnRequestedByChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("RequiresSignature");
        }

        partial void OnVersionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("AddendumPurposeBlirb");
        }

        partial void OnDatedSignaturePresentChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowSigned");
            RaisePropertyChanged("ShowNotSigned");
            RaisePropertyChanged("UsingRefusalReason");
        }

        partial void OnReasonNotSignedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ReasonNotSignedDescription");
            RaisePropertyChanged("UsingRefusalReason");
        }
    }

    public partial class EncounterHospiceElectionAddendumDiagnosis
    {
        public string GroupName => "RadioDiagnosisRelated" + Sequence;
    }
}