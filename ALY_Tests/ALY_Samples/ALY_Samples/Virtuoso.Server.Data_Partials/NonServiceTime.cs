#region Usings

using System;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class EncounterNonServiceTime
    {
        private DateTime _endDatePart;

        private DateTime _endTimePart;

        private DateTime _startDatePart;

        private DateTime _startTimePart;

        public bool MileageIsVisible
        {
            get
            {
                if (NonServiceTypeKey == null || NonServiceTypeKey == 0)
                {
                    return false;
                }

                var nst = NonServiceTypeCache.GetNonServiceTypeFromKey(NonServiceTypeKey);
                if (nst == null)
                {
                    return false;
                }

                return nst.IncMileage;
            }
        }

        public bool IgnoreChanges { get; set; } = true;

        public string NonServiceTypeDescription
        {
            get
            {
                string desc = null;
                desc = NonServiceTypeCache.GetNonServiceTypeDescFromKey(NonServiceTypeKey);
                return desc;
            }
        }

        public DateTime StartTimePart
        {
            get { return _startTimePart; }
            set
            {
                _startTimePart = value == null ? DateTime.Now : value;
                RaisePropertyChanged("StartTimePart");
                SetEndTime();
            }
        }

        public DateTime StartDatePart
        {
            get { return _startDatePart; }
            set
            {
                _startDatePart = value == null ? DateTime.Now : value;
                RaisePropertyChanged("StartDatePart");
                SetEndTime();
            }
        }

        public TimeSpan StartOffSetPart { get; set; }

        public DateTimeOffset StartDateTimeOffSet
        {
            get
            {
                return new DateTimeOffset(StartDatePart.Year, StartDatePart.Month, StartDatePart.Day,
                    StartTimePart.Hour, StartTimePart.Minute, 0, StartOffSetPart);
            }
            set
            {
                IgnoreChanges = true;
                if (value == null)
                {
                    StartTimePart = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                    StartOffSetPart = DateTimeOffset.Now.Offset;
                    // do this last so the calculations are correct.
                    IgnoreChanges = false;
                    StartDatePart = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                }
                else
                {
                    var v = value;
                    StartTimePart = v.DateTime;
                    StartOffSetPart = v.Offset;
                    // do this last so the calculations are correct.
                    IgnoreChanges = false;
                    StartDatePart = v.DateTime;
                }

                StartTime = StartDateTimeOffSet;
            }
        }

        public DateTime EndTimePart
        {
            get { return _endTimePart; }
            set
            {
                _endTimePart = value == null ? DateTime.Now : value;
                RaisePropertyChanged("EndTimePart");
                SetDuration();
            }
        }

        public DateTime EndDatePart
        {
            get { return _endDatePart; }
            set
            {
                _endDatePart = value == null ? DateTime.Now : value;
                RaisePropertyChanged("EndDatePart");
                SetDuration();
            }
        }

        public TimeSpan EndOffSetPart { get; set; }

        public DateTimeOffset EndDateTimeOffSet
        {
            get
            {
                return new DateTimeOffset(EndDatePart.Year, EndDatePart.Month, EndDatePart.Day, EndTimePart.Hour,
                    EndTimePart.Minute, 0, EndOffSetPart);
            }
            set
            {
                IgnoreChanges = true;
                if (value == null)
                {
                    EndTimePart = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                    EndOffSetPart = DateTimeOffset.Now.Offset;
                    // do this last so the calculations are correct.
                    IgnoreChanges = false;
                    EndDatePart = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                }
                else
                {
                    var v = value;
                    EndTimePart = v.DateTime;
                    EndOffSetPart = v.Offset;
                    // do this last so the calculations are correct.
                    IgnoreChanges = false;
                    EndDatePart = v.DateTime;
                }

                EndTime = EndDateTimeOffSet;
            }
        }

        partial void OnNonServiceTypeKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (MileageIsVisible)
            {
                SetDistanceScale();
            }
            else
            {
                Distance = null;
                DistanceScale = null;
            }

            RaisePropertyChanged("NonServiceTypeDescription");
            RaisePropertyChanged("MileageIsVisible");
        }

        private void SetDistanceScale()
        {
            if (string.IsNullOrWhiteSpace(DistanceScale))
            {
                DistanceScale = TenantSettingsCache.Current.TenantSettingDistanceTraveledMeasure;
            }
        }

        partial void OnStartTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetEndTime();
        }

        partial void OnDurationChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetEndTime();
        }

        private void SetEndTime()
        {
            if (IgnoreChanges)
            {
                return;
            }

            if (StartDateTimeOffSet == DateTimeOffset.MinValue)
            {
                return;
            }

            if (EndDateTimeOffSet == DateTimeOffset.MinValue)
            {
                return;
            }

            if (StartDateTimeOffSet != null && Duration != null && Duration > 0
               )
            {
                // Only set it if it is different so we don't get stuck in an infinite loop.
                var tmpEndTime = StartDateTimeOffSet.AddMinutes((double)Duration);
                if (tmpEndTime != EndDateTimeOffSet)
                {
                    EndDateTimeOffSet = StartDateTimeOffSet.AddMinutes((double)Duration);
                }
            }
            else
            {
                EndDateTimeOffSet = StartDateTimeOffSet;
            }

            RaisePropertyChanged("StartTime");
            RaisePropertyChanged("EndTime");
            RaisePropertyChanged("Duration");
            RaisePropertyChanged("EndTimePart");
            RaisePropertyChanged("EndDatePart");
            RaisePropertyChanged("EndOffSetPart");
        }

        private void SetDuration()
        {
            if (IgnoreChanges)
            {
                return;
            }

            if (StartDateTimeOffSet == DateTimeOffset.MinValue)
            {
                return;
            }

            if (EndDateTimeOffSet == DateTimeOffset.MinValue)
            {
                return;
            }

            if (StartDateTimeOffSet != null
                && EndDateTimeOffSet != null
               )
            {
                // Only set it if it is different so we don't get stuck in an infinite loop.
                var tmpDuration = (EndDateTimeOffSet - StartDateTimeOffSet).TotalMinutes;
                if (tmpDuration != Duration)
                {
                    Duration = Convert.ToInt32(tmpDuration);
                }
            }

            RaisePropertyChanged("StartTime");
            RaisePropertyChanged("EndTime");
            RaisePropertyChanged("Duration");
            RaisePropertyChanged("EndTimePart");
            RaisePropertyChanged("EndDatePart");
            RaisePropertyChanged("EndOffSetPart");
        }
    }
}