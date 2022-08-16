#region Usings

using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class DischargeTransferTask
    {
        private bool _Selected;

        public bool Selected
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                Messenger.Default.Send(true, "DischargeTransferTaskSelectedChanged");
                RaisePropertyChanged("Selected");
            }
        }

        public bool IsSummaryTypeTransfer => SummaryType == "T";

        public bool IsSummaryTypeDischarge => SummaryType == "D";

        public string SummaryTypeLong => SummaryType == "D" ? "Discharge" : "Transfer";

        public string SummaryName
        {
            get
            {
                if (IsSummaryTypeDischarge)
                {
                    return SummaryTypeLong + " on " + DischargeTransferDate.ToShortDateString() + "  *";
                }

                return (IsPlanned ? "Planned " : "Unplanned ") + SummaryTypeLong + " on " +
                       DischargeTransferDate.ToShortDateString() + (IsPlanned ? "  *" : "  **");
            }
        }

        public bool IsPlanned
        {
            get
            {
                if (SummaryType == "D")
                {
                    return true;
                }

                if (PlannedTransfer == null)
                {
                    return false;
                }

                return (bool)PlannedTransfer;
            }
        }

        public string PlannedTransferString
        {
            get
            {
                if (PlannedTransfer == null)
                {
                    return null;
                }

                return PlannedTransfer == true ? "Yes" : "No";
            }
        }

        public string ClinicianName
        {
            get
            {
                var clinicianName = UserCache.Current.GetFullNameWithSuffixFromUserId(EncounterBy);
                return clinicianName;
            }
        }

        public string PhysicianName
        {
            get
            {
                if (PhysicianKey == null || PhysicianKey < 0)
                {
                    return null;
                }

                var physicianName = PhysicianCache.Current.GetPhysicianFullNameWithSuffixFromKey(PhysicianKey);
                return physicianName;
            }
        }

        public string FacilityName
        {
            get
            {
                if (FacilityKey == null || FacilityKey < 0)
                {
                    return null;
                }

                var facilityName = FacilityCache.GetFacilityNameFromKey(FacilityKey);
                return facilityName;
            }
        }

        public string FacilityBranchName
        {
            get
            {
                if (FacilityBranchKey == null || FacilityBranchKey < 0)
                {
                    return null;
                }

                var branchName = FacilityCache.GetFacilityBranchName(FacilityBranchKey);
                return branchName;
            }
        }

        public string FacilityAndBranchName
        {
            get
            {
                if (FacilityKey == null || FacilityKey < 0)
                {
                    return null;
                }

                var facilityName = FacilityCache.GetFacilityNameFromKey(FacilityKey);
                var branchName = FacilityBranchName;
                if (string.IsNullOrWhiteSpace(branchName) == false)
                {
                    facilityName = facilityName + " - " + branchName;
                }

                return facilityName;
            }
        }

        public string SendTo
        {
            get
            {
                var sentTo = SummaryType == "D" ? string.IsNullOrWhiteSpace(PhysicianName) ? "?" :
                    PhysicianName :
                    string.IsNullOrWhiteSpace(FacilityAndBranchName) ? "?" : FacilityAndBranchName;
                return sentTo;
            }
        }

        public bool MarkedAsPrinted => PrintSentDate == null ? false : true;

        public void SetSelectedWithoutMessage(bool selected)
        {
            _Selected = selected;
            RaisePropertyChanged("Selected");
        }

        partial void OnPrintSentDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MarkedAsPrinted");
        }
    }
}