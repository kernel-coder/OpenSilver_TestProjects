#region Usings

using System;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionSiteOfService
    {
        public string SiteOfServiceCode
        {
            get
            {
                if (SiteOfServiceKey == null || SiteOfServiceKey <= 0)
                {
                    return null;
                }

                return CodeLookupCache.GetCodeFromKey((int)SiteOfServiceKey);
            }
        }

        public string SiteOfServiceCodeDescription
        {
            get
            {
                if (SiteOfServiceKey == null || SiteOfServiceKey <= 0)
                {
                    return null;
                }

                return CodeLookupCache.GetCodeDescriptionFromKey((int)SiteOfServiceKey);
            }
        }

        public bool SiteOfServiceIsApplicableToMAR =>
            // 06 - Hospice provided in Inpatient Hospice Facility
            // 10 - Hospice home care provided in a hospice facility
            SiteOfServiceCode == "06" || SiteOfServiceCode == "10";

        public string SiteOfServiceFromDateTimeFormatted
        {
            get
            {
                var date = SiteOfServiceFromDatePart == null
                    ? ""
                    : Convert.ToDateTime(SiteOfServiceFromDatePart).ToShortDateString();
                var time = "";
                if (SiteOfServiceFromTimePart != null)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        time = Convert.ToDateTime(SiteOfServiceFromTimePart).ToString("HHmm");
                    }
                    else
                    {
                        time = Convert.ToDateTime(SiteOfServiceFromTimePart).ToShortTimeString();
                    }
                }

                return date + " " + time;
            }
        }

        public DateTime SiteOfServiceFromDateTimeSort =>
            SiteOfServiceFromDatePart == null || SiteOfServiceFromTimePart == null
                ? DateTime.MinValue
                : new DateTime(SiteOfServiceFromDatePart.Value.Year, SiteOfServiceFromDatePart.Value.Month,
                    SiteOfServiceFromDatePart.Value.Day, SiteOfServiceFromTimePart.Value.Hour,
                    SiteOfServiceFromTimePart.Value.Minute, 0);

        public bool SiteOfServiceFromDateTimeHasValue => SiteOfServiceFromDateTimeSort != DateTime.MinValue;

        public string SiteOfServiceThruDateTimeFormatted
        {
            get
            {
                var date = SiteOfServiceThruDatePart == null
                    ? ""
                    : Convert.ToDateTime(SiteOfServiceThruDatePart).ToShortDateString();
                var time = "";
                if (SiteOfServiceThruTimePart != null)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        time = Convert.ToDateTime(SiteOfServiceThruTimePart).ToString("HHmm");
                    }
                    else
                    {
                        time = Convert.ToDateTime(SiteOfServiceThruTimePart).ToShortTimeString();
                    }
                }

                return date + " " + time;
            }
        }

        public DateTime SiteOfServiceThruDateTimeSort =>
            SiteOfServiceThruDatePart == null || SiteOfServiceThruTimePart == null
                ? DateTime.MinValue
                : new DateTime(SiteOfServiceThruDatePart.Value.Year, SiteOfServiceThruDatePart.Value.Month,
                    SiteOfServiceThruDatePart.Value.Day, SiteOfServiceThruTimePart.Value.Hour,
                    SiteOfServiceThruTimePart.Value.Minute, 0);

        public bool SiteOfServiceThruDateTimeHasValue => SiteOfServiceThruDateTimeSort != DateTime.MinValue;

        public override bool CanFullEdit => true;

        public override bool CanDelete
        {
            get
            {
                // Can delete new items
                if (IsOKed == false)
                {
                    return false;
                }

                if (IsNew || AdmissionSiteOfServiceKey <= 0)
                {
                    return true;
                }

                return false;
            }
        }

        partial void OnSiteOfServiceKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("SiteOfServiceCode");
            RaisePropertyChanged("SiteOfServiceCodeDescription");
            RaisePropertyChanged("SiteOfServiceIsApplicableToMAR");
        }

        partial void OnSiteOfServiceFromDatePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SiteOfServiceFromOffSetPart = ((DateTimeOffset)DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)).Offset;
            RaisePropertyChanged("SiteOfServiceFromDateTimeFormatted");
            RaisePropertyChanged("SiteOfServiceFromDateTimeSort");
        }

        partial void OnSiteOfServiceFromTimePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("SiteOfServiceFromDateTimeFormatted");
            RaisePropertyChanged("SiteOfServiceFromDateTimeSort");
        }

        partial void OnSiteOfServiceThruDatePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SiteOfServiceThruOffSetPart = ((DateTimeOffset)DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)).Offset;
            RaisePropertyChanged("SiteOfServiceThruDateTimeFormatted");
            RaisePropertyChanged("SiteOfServiceThruDateTimeSort");
        }

        partial void OnSiteOfServiceThruTimePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("SiteOfServiceThruDateTimeFormatted");
            RaisePropertyChanged("SiteOfServiceThruDateTimeSort");
        }

        private string DateTimeOffsetFormatted(DateTimeOffset? dto)
        {
            var date = dto == null ? "" : dto.Value.DateTime.ToShortDateString();
            var time = "";
            if (dto != null)
            {
                time = TenantSettingsCache.Current.TenantSetting.UseMilitaryTime 
                    ? dto.Value.DateTime.ToString("HHmm") 
                    : dto.Value.DateTime.ToShortTimeString();
            }

            return date + " " + time;
        }

        public bool ClientValidate()
        {
            var AllValid = true;

            if (SiteOfServiceKey == null)
            {
                string[] memberNames = { "SiteOfServiceKey" };
                var Msg = "A Site Of Service  is required.";
                ValidationErrors.Add(new ValidationResult(Msg, memberNames));

                AllValid = false;
            }

            if (ClientValidateSiteOfServiceFromDateTime() == false)
            {
                AllValid = false;
            }

            if (ClientValidateSiteOfServiceThruDateTime() == false)
            {
                AllValid = false;
            }

            return AllValid;
        }

        private bool ClientValidateSiteOfServiceFromDateTime()
        {
            var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            var success = true;

            if (SiteOfServiceFromDatePart == null || SiteOfServiceFromDatePart == DateTime.MinValue)
            {
                ValidationErrors.Add(new ValidationResult("Site Of Service From Date is required.",
                    new[] { "SiteOfServiceFromDatePart" }));
                success = false;
            }

            if (SiteOfServiceFromTimePart == null || SiteOfServiceFromTimePart == DateTime.MinValue)
            {
                ValidationErrors.Add(new ValidationResult("Site Of Service From Time is required.",
                    new[] { "SiteOfServiceFromTimePart" }));
                success = false;
            }

            if (success && SiteOfServiceFromDateTimeHasValue && SiteOfServiceFromDateTimeSort > now)
            {
                ValidationErrors.Add(new ValidationResult("Site Of Service From Date/Time cannot be in the future.",
                    new[] { "SiteOfServiceFromDatePart", "SiteOfServiceFromTimePart" }));
                success = false;
            }

            return success;
        }

        private bool ClientValidateSiteOfServiceThruDateTime()
        {
            var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            var success = true;

            var dateEntered = SiteOfServiceThruDatePart != null && SiteOfServiceThruDatePart != DateTime.MinValue;
            var timeEntered = SiteOfServiceThruTimePart != null && SiteOfServiceThruTimePart != DateTime.MinValue;
            if (dateEntered && !timeEntered || !dateEntered && timeEntered)
            {
                ValidationErrors.Add(new ValidationResult(
                    "Both a Site Of Service Thru Date and Time are required, or leave both blank.",
                    new[] { "SiteOfServiceThruDatePart", "SiteOfServiceThruTimePart" }));
                success = false;
            }

            if (SiteOfServiceThruDateTimeHasValue && SiteOfServiceThruDateTimeSort > now)
            {
                ValidationErrors.Add(new ValidationResult("Site Of Service Thru Date/Time cannot be in the future.",
                    new[] { "SiteOfServiceThruDatePart", "SiteOfServiceThruTimePart" }));
                success = false;
            }

            return success;
        }

        public void MyRejectChanges()
        {
            RejectChanges();
        }
    }
}