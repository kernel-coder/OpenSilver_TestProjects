#region Usings

using System;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class BereavementActivity
    {
        public static string ALL_ACTIVITIES = "All Activities";
        public static string UNPLANNED_ACTIVITIES = "Unplanned Activities";
        private string _ActivityDescriptionFull;
        private string _EventTypeCode;
        private string _EventTypeCodeDescription;
        private bool _IsSelected;
        private string _TimePointCode;
        private string _TimePointCodeDescription;

        public string InactiveBlirb
        {
            get
            {
                if (Inactive == false)
                {
                    return null;
                }

                var UserName = InactiveBy != null ? UserCache.Current.GetFormalNameFromUserId(InactiveBy) : "?";
                var DateFormatted = InactiveDate.HasValue ? InactiveDate.Value.ToString("MM/dd/yyyy") : "?";
                return "Inactivated By " + UserName + " on " + DateFormatted;
            }
        }

        public string InactiveIndicator => Inactive ? "(Inactive)" : null;

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

        public string ActivityDescriptionFull
        {
            get
            {
                if (_ActivityDescriptionFull != null)
                {
                    return _ActivityDescriptionFull;
                }

                if (ActivityDescription == ALL_ACTIVITIES)
                {
                    return ALL_ACTIVITIES;
                }

                if (ActivityDescription == UNPLANNED_ACTIVITIES)
                {
                    return UNPLANNED_ACTIVITIES;
                }

                var e = EventTypeCode;
                var t = TimePointCodeDescription;
                _ActivityDescriptionFull = ActivityDescription + (string.IsNullOrWhiteSpace(e) ? "" : " - " + e);
                if (string.IsNullOrWhiteSpace(t) == false)
                {
                    _ActivityDescriptionFull = _ActivityDescriptionFull + " (" + t.Trim() + ")";
                }
                else if (TimePointDate != null)
                {
                    _ActivityDescriptionFull = _ActivityDescriptionFull + " (" +
                                               ((DateTime)TimePointDate).Date.ToShortDateString() + ")";
                }

                return _ActivityDescriptionFull;
            }
        }

        public string EventTypeCode
        {
            get
            {
                if (_EventTypeCode != null)
                {
                    return _EventTypeCode;
                }

                var d = CodeLookupCache.GetCodeFromKey(EventTypeKey);
                _EventTypeCode = string.IsNullOrWhiteSpace(d) ? "" : d.Trim();
                return _EventTypeCode;
            }
        }

        public string EventTypeCodeDescription
        {
            get
            {
                if (_EventTypeCodeDescription != null)
                {
                    return _EventTypeCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(EventTypeKey);
                _EventTypeCodeDescription = string.IsNullOrWhiteSpace(d) ? "" : d.Trim();
                return _EventTypeCodeDescription;
            }
        }

        public string TimePointDescription
        {
            get
            {
                if (TimePointDate == null)
                {
                    return TimePointCodeDescription;
                }

                return ((DateTime)TimePointDate).Date.ToShortDateString();
            }
        }

        public DateTime TimePointSortDate
        {
            get
            {
                // For calendar date activities - return that date
                if (TimePointDate != null)
                {
                    return ((DateTime)TimePointDate).Date;
                }

                // For timePoint activities, return the TimePointCode converted to a today-based date
                return DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date
                    .AddMonths(ConvertTimePointCodeToMonthOffset);
            }
        }

        public int TimePointSortTimePoint
        {
            get
            {
                // If dates happen to be the same - we want to sort by Immediate TimePoints, followed by CalendarDate TimePoints, Followed by Month TimePoints
                var code = TimePointCode;
                if (code == "I")
                {
                    return 0;
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    return 1;
                }

                return 2;
            }
        }

        private string TimePointCode
        {
            get
            {
                if (_TimePointCode != null)
                {
                    return _TimePointCode;
                }

                var d = CodeLookupCache.GetCodeFromKey(TimePointKey);
                _TimePointCode = string.IsNullOrWhiteSpace(d) ? null : d.Trim();
                return _TimePointCode;
            }
        }

        private string TimePointCodeDescription
        {
            get
            {
                if (_TimePointCodeDescription != null)
                {
                    return _TimePointCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(TimePointKey);
                _TimePointCodeDescription = string.IsNullOrWhiteSpace(d) ? "" : d.Trim();
                return _TimePointCodeDescription;
            }
        }

        private int ConvertTimePointCodeToMonthOffset
        {
            get
            {
                var code = TimePointCode;
                if (string.IsNullOrWhiteSpace(code))
                {
                    return 0;
                }

                // Skim off Immediate - 
                if (code == "I")
                {
                    return 0;
                }

                // Assume code is now of format "M" + n - where n = 1 thru 13 - e.g., "M1", "M2"..."M13"
                var month = 0;
                try
                {
                    month = int.Parse(code.Replace("M", ""));
                }
                catch
                {
                }

                if (month <= 0)
                {
                    return 0;
                }

                return month - 1;
            }
        }

        public bool IsSelected
        {
            get { return _IsSelected; }
            set
            {
                _IsSelected = value;
                RaisePropertyChanged("IsSelected");
            }
        }

        partial void OnInactiveChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            InactiveDate = Inactive ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified) : (DateTime?)null;
            InactiveBy = Inactive ? WebContext.Current.User.MemberID : (Guid?)null;
            RaisePropertyChanged("InactiveBlirb");
            RaisePropertyChanged("InactiveIndicator");
            RaisePropertyChanged("IsInactiveIndicator");
        }

        partial void OnEventTypeKeyChanged()
        {
            _EventTypeCode = null;
            _EventTypeCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EventTypeCode");
            RaisePropertyChanged("EventTypeCodeDescription");
        }

        partial void OnTimePointKeyChanged()
        {
            _TimePointCode = null;
            _TimePointCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            if (TimePointKey != null)
            {
                TimePointDate = null;
            }

            RaisePropertyChanged("TimePointCode");
            RaisePropertyChanged("TimePointCodeDescription");
            RaisePropertyChanged("TimePointDescription");
            RaisePropertyChanged("TimePointSortDate");
            RaisePropertyChanged("TimePointSortTimePoint");
        }

        partial void OnTimePointDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (TimePointDate != null)
            {
                TimePointKey = null;
            }

            RaisePropertyChanged("TimePointDescription");
            RaisePropertyChanged("TimePointSortDate");
            RaisePropertyChanged("TimePointSortTimePoint");
        }

        public DateTime TimePointDueDate(DateTime deathDate)
        {
            // For calendar date activities - return that date
            if (TimePointDate != null)
            {
                return ((DateTime)TimePointDate).Date;
            }

            // For timePoint activities, return the TimePointCode converted to a death-based date
            return deathDate.Date.AddMonths(ConvertTimePointCodeToMonthOffset);
        }

        public bool IsTimePointDateOutsideScope(DateTime deathDate)
        {
            if (TimePointDate == null)
            {
                return false;
            }

            if (((DateTime)TimePointDate).Date < deathDate.Date)
            {
                return true;
            }

            if (((DateTime)TimePointDate).Date > deathDate.Date.AddMonths(13))
            {
                return true;
            }

            return false;
        }

        public DateTime TimePointLateDate(DateTime deathDate)
        {
            // For calendar date activities - return that date
            if (TimePointDate != null)
            {
                return ((DateTime)TimePointDate).Date.AddDays(1).Date;
            }

            // For timePoint activities, return the TimePointCode converted to a dueDate-based date
            var dueDate = TimePointDueDate(deathDate);
            return TimePointCode == "I" ? dueDate.Date.AddDays(2) : dueDate.Date.AddMonths(1);
        }
    }

    public partial class BereavementPlan
    {
        private string _BereavementLocationCodeDescription;
        private string _BereavementSourceCodeDescription;
        private string _LevelOfBereavementServicesDescription;
        private string _RiskRangeDescription;

        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(BereavementPlanCode))
                {
                    return string.Format("Bereavement Plan {0}", BereavementPlanCode.Trim());
                }

                return IsNew ? "New Bereavement Plan" : "Edit Bereavement Plan";
            }
        }

        public string InactiveBlirb
        {
            get
            {
                if (Inactive == false)
                {
                    return null;
                }

                var UserName = InactiveBy != null ? UserCache.Current.GetFormalNameFromUserId(InactiveBy) : "?";
                var DateFormatted = InactiveDate.HasValue ? InactiveDate.Value.ToString("MM/dd/yyyy") : "?";
                return "Inactivated By " + UserName + " on " + DateFormatted;
            }
        }

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

        public string BereavementSourceCodeDescription
        {
            get
            {
                if (_BereavementSourceCodeDescription != null)
                {
                    return _BereavementSourceCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(BereavementSourceKey);
                _BereavementSourceCodeDescription = string.IsNullOrWhiteSpace(d) ? "Unknown" : d.Trim();
                return _BereavementSourceCodeDescription;
            }
        }

        public string BereavementLocationCodeDescription
        {
            get
            {
                if (_BereavementLocationCodeDescription != null)
                {
                    return _BereavementLocationCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(BereavementLocationKey);
                _BereavementLocationCodeDescription = string.IsNullOrWhiteSpace(d) ? "Unknown" : d.Trim();
                return _BereavementLocationCodeDescription;
            }
        }

        public string LevelOfBereavementServicesDescription
        {
            get
            {
                if (_LevelOfBereavementServicesDescription != null)
                {
                    return _LevelOfBereavementServicesDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(LevelOfBereavementServicesKey);
                _LevelOfBereavementServicesDescription = string.IsNullOrWhiteSpace(d) ? "Unknown" : d.Trim();
                return _LevelOfBereavementServicesDescription;
            }
        }

        public string RiskRangeDescription
        {
            get
            {
                if (_RiskRangeDescription != null)
                {
                    return _RiskRangeDescription;
                }

                var rr = RiskRangeKey == -1 ? "Not Assessed" : DynamicFormCache.GetRiskRangeLabelByKey(RiskRangeKey);
                _RiskRangeDescription = string.IsNullOrWhiteSpace(rr) ? "Unknown" : rr;
                return _RiskRangeDescription;
            }
        }

        public string RiskOrLSN => RiskRangeDescription;

        partial void OnInactiveChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            InactiveDate = Inactive ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified) : (DateTime?)null;
            InactiveBy = Inactive ? WebContext.Current.User.MemberID : (Guid?)null;
            RaisePropertyChanged("InactiveBlirb");
            RaisePropertyChanged("InactiveIndicator");
            RaisePropertyChanged("IsInactiveIndicator");
        }

        partial void OnBereavementSourceKeyChanged()
        {
            _BereavementSourceCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("BereavementSourceCodeDescription");
        }

        partial void OnBereavementLocationKeyChanged()
        {
            _BereavementLocationCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("BereavementLocationCodeDescription");
        }

        partial void OnLevelOfBereavementServicesKeyChanged()
        {
            _LevelOfBereavementServicesDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            if (LevelOfBereavementServicesKey != null)
            {
                RiskRangeKey = null;
            }

            RaisePropertyChanged("LevelOfBereavementServicesDescription");
            RaisePropertyChanged("RiskOrLSN");
        }

        partial void OnRiskRangeKeyChanged()
        {
            _RiskRangeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            if (RiskRangeKey != null)
            {
                LevelOfBereavementServicesKey = null;
            }

            RaisePropertyChanged("RiskRangeDescription");
            RaisePropertyChanged("RiskOrLSN");
        }
    }

    public partial class BereavementPlanActivity
    {
        private BereavementActivity _CacheBA;

        public BereavementActivity CacheBA
        {
            get
            {
                if (_CacheBA != null)
                {
                    return _CacheBA;
                }

                _CacheBA = BereavementActivityCache.GetBereavementActivityByKey(BereavementActivityKey);
                return _CacheBA;
            }
        }

        public string InactiveIndicator => _CacheBA?.InactiveIndicator;

        partial void OnDeletedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            DeletedDate = Deleted ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified) : (DateTime?)null;
            DeletedBy = Deleted ? WebContext.Current.User.MemberID : (Guid?)null;
        }

        partial void OnBereavementActivityKeyChanged()
        {
            _CacheBA = null;
        }
    }
}