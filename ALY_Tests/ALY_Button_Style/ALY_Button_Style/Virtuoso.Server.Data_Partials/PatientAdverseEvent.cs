#region Usings

using System;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Occasional;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class PatientAdverseEvent
    {
        public string EventTypeCodeDescription
        {
            get
            {
                var ret = EventTypeKey.HasValue && EventTypeKey > 0
                    ? CodeLookupCache.GetCodeDescriptionFromKey(EventTypeKey)
                    : null;
                return string.IsNullOrWhiteSpace(ret) ? "??" : ret;
            }
        }

        public string EventDateFormatted
        {
            get
            {
                if (EventDate == null)
                {
                    return null;
                }

                return ((DateTime)EventDate).Date.ToShortDateString();
            }
        }

        public bool NotWitnessedByAgency
        {
            get { return WitnessedByAgency != null && !WitnessedByAgency.Value; }
            set
            {
                if (value)
                {
                    WitnessedByAgency = false;
                }
            }
        }

        public string WitnessedByAgencyFormatted =>
            WitnessedByAgency == null || WitnessedByAgency == false ? "No" : "Yes";

        public string WitnessedByFormatted =>
            string.IsNullOrWhiteSpace(WitnessedBy) ? "Not witnessed by agency" : WitnessedBy;

        public string DocumentedByFormatted
        {
            get
            {
                if (DocumentedBy == null)
                {
                    return null;
                }

                return UserCache.Current.GetFormalNameFromUserId(DocumentedBy);
            }
        }

        public string DocumentedDateTimeFormatted
        {
            get
            {
                if (DocumentedDateTime == null)
                {
                    return null;
                }

                var dateTime = Convert.ToDateTime(((DateTimeOffset)DocumentedDateTime).DateTime).ToShortDateString();
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    dateTime = dateTime + " " + Convert.ToDateTime(((DateTimeOffset)DocumentedDateTime).DateTime)
                        .ToString("HHmm");
                }
                else
                {
                    dateTime = dateTime + " " + Convert.ToDateTime(((DateTimeOffset)DocumentedDateTime).DateTime)
                        .ToShortTimeString();
                }

                if (string.IsNullOrWhiteSpace(dateTime))
                {
                    return null;
                }

                return dateTime;
            }
        }

        public PatientAdverseEvent CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newpae = (PatientAdverseEvent)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newpae);
            if (newpae.HistoryKey == null)
            {
                newpae.HistoryKey = PatientAdverseEventKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newpae;
        }

        partial void OnEventTypeKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EventTypeCodeDescription");
        }

        partial void OnEventDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EventDateFormatted");
        }

        partial void OnWitnessedByAgencyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (WitnessedByAgency == null || WitnessedByAgency == false)
            {
                WitnessedBy = null;
            }

            RaisePropertyChanged("NotWitnessedByAgency");
            RaisePropertyChanged("WitnessedByFormatted");
            RaisePropertyChanged("WitnessedByAgencyFormatted");
        }

        partial void OnWitnessedByChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("WitnessedByFormatted");
            RaisePropertyChanged("WitnessedByAgencyFormatted");
        }

        partial void OnDocumentedByChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DocumentedByFormatted");
        }

        partial void OnDocumentedDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DocumentedDateTimeFormatted");
        }
    }
}