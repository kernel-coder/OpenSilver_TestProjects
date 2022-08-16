#region Usings

using System;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionCoverage
    {
        public string CoverageTypeDescription
        {
            get
            {
                var description = CodeLookupCache.GetCodeDescriptionFromKey(CoverageTypeKey);
                return description;
            }
        }

        public DateTime? StartDateForSort => StartDate == DateTime.MinValue ? DateTime.MaxValue : StartDate;

        public string FromThru
        {
            get
            {
                var fromThru = StartDate == null || StartDate == DateTime.MinValue ? "" : StartDate.ToShortDateString();
                fromThru += EndDate == null ? "" : " - " + EndDate.Value.ToShortDateString();
                return fromThru;
            }
        }

        partial void OnCoverageTypeKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CoverageTypeDescription");
        }

        partial void OnStartDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FromThru");
        }

        partial void OnEndDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FromThru");
        }

        public void RaiseChanged()
        {
            RaisePropertyChanged("CoverageTypeKey");
            RaisePropertyChanged("IsSigned");
            RaisePropertyChanged("StartDate");
            RaisePropertyChanged("EndDate");
            RaisePropertyChanged("DualEligible");
            RaisePropertyChanged("FromThru");
        }

        public void RaiseDatesChanged()
        {
            RaisePropertyChanged("StartDate");
            RaisePropertyChanged("EndDate");
        }

        public bool IsActiveAsOfDate(DateTime? pDate)
        {
            if (HistoryKey != null)
            {
                return false;
            }

            var date = pDate?.Date ?? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;

            if (StartDate.Date > date)
            {
                return false;
            }

            if (EndDate == null)
            {
                return true;
            }

            if (((DateTime)EndDate).Date < date)
            {
                return false;
            }

            return true;
        }
    }
}