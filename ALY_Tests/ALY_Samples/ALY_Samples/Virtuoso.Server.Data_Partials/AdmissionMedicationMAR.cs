#region Usings

using System;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public enum MARDocumentState
    {
        // 1 = administered     = HighlightBrush
        // 2 = not administered = RedBrush
        // 3 = untouched        = GreenBrush
        Administered = 1,
        NotAdministered = 2,
        UnTouched = 3
    }

    public partial class AdmissionMedicationMAR
    {
        private string _NewAdministeredFollowUp;
        private bool _OtherEncounter;
        private bool _ShowNewAdministeredFollowUp;
        private string CR = char.ToString('\r');

        public bool OtherEncounter
        {
            get { return _OtherEncounter; }
            set
            {
                _OtherEncounter = value;
                RaisePropertyChanged("OtherEncounter");
            }
        }

        public string DocumentedByFommatted => UserIDFormatted(DocumentedBy);

        public string AdministrationDateTimeFormatted
        {
            get
            {
                var date = AdministrationDatePart == null
                    ? ""
                    : Convert.ToDateTime(AdministrationDatePart).ToShortDateString();
                var time = "";
                if (AdministrationTimePart != null)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        time = Convert.ToDateTime(AdministrationTimePart).ToString("HHmm");
                    }
                    else
                    {
                        time = Convert.ToDateTime(AdministrationTimePart).ToShortTimeString();
                    }
                }

                return date + " " + time;
            }
        }

        public string ScheduledAdministrationDateTimeFormatted => MARAdministrationTime + "  -  " +
                                                                  DateTimeOffsetFormatted(
                                                                      ScheduledAdministrationDateTime);

        public DateTime AdministrationDateTimeSort => AdministrationDatePart == null || AdministrationTimePart == null
            ? DateTime.MinValue
            : new DateTime(AdministrationDatePart.Value.Year, AdministrationDatePart.Value.Month,
                AdministrationDatePart.Value.Day, AdministrationTimePart.Value.Hour,
                AdministrationTimePart.Value.Minute, 0);

        public string MARblirb
        {
            get
            {
                var blirb = string.Empty;
                if (PRN == false)
                {
                    return MARAdministrationTime;
                }

                blirb = OtherEncounter ? "Last Dose prior shift" : "Dose this shift";
                blirb = blirb + " by " + DocumentedByFommatted;
                blirb = blirb + ",on " + AdministrationDateTimeFormatted;
                return blirb;
            }
        }

        public MARDocumentState DocumentedState
        {
            get
            {
                // 1 = administered     = HighlightBrush
                // 2 = not administered = RedBrush
                // 3 = untouched        = GreenBrush
                if (NotAdministered)
                {
                    return MARDocumentState.NotAdministered;
                }

                if (AdministrationDateTimeSort != DateTime.MinValue)
                {
                    return MARDocumentState.Administered;
                }

                return MARDocumentState.UnTouched;
            }
        }

        public bool ShowAdministeredFollowUp => string.IsNullOrWhiteSpace(AdministeredFollowUp) ? false : true;

        public string NewAdministeredFollowUp
        {
            get { return _NewAdministeredFollowUp; }
            set
            {
                _NewAdministeredFollowUp = value;
                RaisePropertyChanged("NewAdministeredFollowUp");
            }
        }

        public bool ShowNewAdministeredFollowUp
        {
            get { return _ShowNewAdministeredFollowUp; }
            set
            {
                _ShowNewAdministeredFollowUp = value;
                RaisePropertyChanged("ShowNewAdministeredFollowUp");
            }
        }

        public string NewAdministeredFollowUpByWhen => "Follow up by " +
                                                       UserIDFormatted(WebContext.Current.User.MemberID) + ", on " +
                                                       DateTimeOffsetFormatted(DateTime.SpecifyKind(DateTime.Now,
                                                           DateTimeKind.Unspecified));

        public string AdministeredFollowUpHistory => string.IsNullOrWhiteSpace(AdministeredFollowUp)
            ? "No follow up recorded"
            : AdministeredFollowUp;

        private string UserIDFormatted(Guid UserID)
        {
            var by = UserCache.Current.GetFormalNameFromUserId(UserID);
            if (string.IsNullOrWhiteSpace(by))
            {
                @by = "??";
            }

            return by;
        }

        partial void OnAdministrationDatePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("AdministrationDateTimeFormatted");
            RaisePropertyChanged("AdministrationDateTimeSort");
            RaisePropertyChanged("DocumentedState");
            if (AdministrationDatePart != null)
            {
                NotAdministered = false;
            }
        }

        partial void OnAdministrationTimePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("AdministrationDateTimeFormatted");
            RaisePropertyChanged("AdministrationDateTimeSort");
            RaisePropertyChanged("DocumentedState");
            if (AdministrationTimePart != null)
            {
                NotAdministered = false;
            }
        }

        partial void OnNotAdministeredChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (NotAdministered)
            {
                AdministrationDatePart = null;
                AdministrationTimePart = null;
            }

            if (NotAdministered == false)
            {
                NotAdministeredReason = null;
            }

            RaisePropertyChanged("DocumentedState");
        }

        private string DateTimeOffsetFormatted(DateTimeOffset? dto)
        {
            var date = dto == null ? "" : dto.Value.DateTime.ToShortDateString();
            var time = "";
            if (dto != null)
            {
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    time = dto.Value.DateTime.ToString("HHmm");
                }
                else
                {
                    time = dto.Value.DateTime.ToShortTimeString();
                }
            }

            return date + " " + time;
        }

        partial void OnAdministeredFollowUpChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("AdministeredFollowUpHistory");
            RaisePropertyChanged("ShowAdministeredFollowUp");
            RaisePropertyChanged("ShowAdministeredFollowUpHistory");
        }

        public void AppendAdministeredFollowUp(string newFollowUP)
        {
            if (PRN == false || string.IsNullOrWhiteSpace(newFollowUP))
            {
                return;
            }

            AdministeredFollowUp = AdministeredFollowUp +
                                   (string.IsNullOrWhiteSpace(AdministeredFollowUp) ? "" : "\r") +
                                   NewAdministeredFollowUpByWhen + " - " + newFollowUP.Trim();
        }
    }
}