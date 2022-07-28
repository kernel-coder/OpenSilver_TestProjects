using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Virtuoso.Server.Data
{
    public enum PatientCaregiverClinicianType
    {
        Patient = 0,
        Caregiver = 1,
        Clinician = 2
    }

    public partial class PatientContact
    {
        public string FullNameInformal
        {
            get
            {
                if (string.IsNullOrEmpty(this.FirstName))
                {
                    return this.LastName.Trim();
                }
                else
                {
                    return (this.FirstName.Trim() + " " + this.LastName).Trim();
                }
            }
        }
    }

    public partial class Patient
    {
        public string FullNameWithSuffix
        {
            get
            {
                string name = String.Format("{0}{1},{2}{3}",
                    ((this.LastName == null) ? "" : this.LastName.Trim()),
                    ((this.Suffix == null) ? "" : " " + this.Suffix.Trim()),
                    ((this.FirstName == null) ? "" : " " + this.FirstName.Trim()),
                    ((this.MiddleName == null) ? "" : " " + this.MiddleName.Trim())).Trim();
                if ((name == ",") || (name == "")) name = " ";
                if (name == "All,") name = "All";
                return name;
            }
        }

        [Display(Name = "Formal Name")]
        public string FullName
        {
            get
            {
                string name = String.Format("{0}, {1} {2}", this.LastName, this.FirstName, !string.IsNullOrEmpty(this.MiddleName) ? this.MiddleName : "").Trim();
                if ((name == ",") || (name == "")) name = " ";
                return name;
            }
        }

        [Display(Name = "Informal Name", ShortName = "Patient")]
        public string FullNameInformal
        {
            get
            {
                string name = String.Format("{0} {1}", !string.IsNullOrEmpty(this.NickName) ? this.NickName : this.FirstName, this.LastName).Trim();
                if (name == null) return " ";
                if (name.Trim() == "") return " ";
                return name.Trim();
            }
        }

        [Display(Name = "Formal Name", ShortName = "Patient")]
        public string FullNameFormal
        {
            get
            {
                if (string.IsNullOrEmpty(this.FirstName))
                {
                    return this.LastName.Trim();
                }
                else
                {
                    return (this.FirstName.Trim() + " " + this.LastName).Trim();
                }
            }
        }

        public string FormattedName
        {
            get
            {
                string formattedName = (!string.IsNullOrWhiteSpace(this.MiddleName)) ? this.FirstName + " " + this.MiddleName : this.FirstName;
                formattedName = formattedName.Trim() + " " + this.LastName.Trim() + " " + this.Suffix;
                return formattedName;
            }
        }

        private bool _ValidateState_DeathDateRequired = false;

        [DataMember]
#if !SILVERLIGHT
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
#endif
        public bool ValidateState_DeathDateRequired
        {
            get { return this._ValidateState_DeathDateRequired; }
            set { this._ValidateState_DeathDateRequired = value; }
        }
    }

    public partial class PatientAdvancedDirective
    {
        [RoundtripOriginal]
        [DataMember]
#if !SILVERLIGHT
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
#endif
        public DateTimeOffset? ReviewedDateTimeOffSet
        {
            get
            {
                return (ReviewedDatePart == null || ReviewedTimePart == null || ReviewedOffSetPart == null) ? (DateTimeOffset?)null : new DateTimeOffset(this.ReviewedDatePart.Value.Year, this.ReviewedDatePart.Value.Month, this.ReviewedDatePart.Value.Day, this.ReviewedTimePart.Value.Hour, this.ReviewedTimePart.Value.Minute, 0, this.ReviewedOffSetPart.Value);
            }
            set
            {
                if (value == null)
                {
                    ReviewedDatePart = (DateTime?)null;
                    ReviewedTimePart = (DateTime?)null;
                    ReviewedOffSetPart = (TimeSpan?)null;
                }
                else
                {
                    DateTimeOffset v = (DateTimeOffset)value;
                    ReviewedDatePart = v.DateTime;
                    ReviewedTimePart = v.DateTime;
                    ReviewedOffSetPart = v.Offset;
                }
            }
        }

        [RoundtripOriginal]
        [DataMember]
#if !SILVERLIGHT
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
#endif
        public DateTimeOffset? NewReviewedDateTimeOffSet
        {
            get
            {
                return (NewReviewedDatePart == null || NewReviewedTimePart == null || NewReviewedOffSetPart == null) ? (DateTimeOffset?)null : new DateTimeOffset(this.NewReviewedDatePart.Value.Year, this.NewReviewedDatePart.Value.Month, this.NewReviewedDatePart.Value.Day, this.NewReviewedTimePart.Value.Hour, this.NewReviewedTimePart.Value.Minute, 0, this.NewReviewedOffSetPart.Value);
            }
            set
            {
                if (value == null)
                {
                    NewReviewedDatePart = (DateTime?)null;
                    NewReviewedTimePart = (DateTime?)null;
                    NewReviewedOffSetPart = (TimeSpan?)null;
                }
                else
                {
                    DateTimeOffset v = (DateTimeOffset)value;
                    NewReviewedDatePart = v.DateTime;
                    NewReviewedTimePart = v.DateTime;
                    NewReviewedOffSetPart = v.Offset;
                }
            }
        }
    }

    public partial class PatientMedication
    {
        [RoundtripOriginal]
        [DataMember]
#if !SILVERLIGHT
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
#endif
        public DateTimeOffset? DisposedDateTimeOffSet
        {
            get
            {
                return (DisposedDatePart == null || DisposedTimePart == null || DisposedOffSetPart == null) ? (DateTimeOffset?)null : new DateTimeOffset(this.DisposedDatePart.Value.Year, this.DisposedDatePart.Value.Month, this.DisposedDatePart.Value.Day, this.DisposedTimePart.Value.Hour, this.DisposedTimePart.Value.Minute, 0, this.DisposedOffSetPart.Value);
            }
            set
            {
                if (value == null)
                {
                    DisposedDatePart = (DateTime?)null;
                    DisposedTimePart = (DateTime?)null;
                    DisposedOffSetPart = (TimeSpan?)null;
                }
                else
                {
                    DateTimeOffset v = (DateTimeOffset)value;
                    DisposedDatePart = v.DateTime;
                    DisposedTimePart = v.DateTime;
                    DisposedOffSetPart = v.Offset;
                }
            }
        }

        public bool IsMediSpanMedication
        {
            get
            {
                return (MediSpanMedicationKey == null) ? false : true;
            }
        }

        public bool IsPrescription
        {
            get
            {
                return (MedicationRXType == 1) ? true : false;
            }
        }

        public bool IsOverTheCounter
        {
            get
            {
                return (MedicationRXType == 2) ? true : false;
            }
        }

        public bool IsPrescriptionDataAllowed
        {
            get
            {
                if (IsMediSpanMedication == false) return true;
                return (IsOverTheCounter) ? false : true;
            }
        }
        public DateTime? MedicationStartDateTime
        {
            get
            {
                if (MedicationStartTime != null) return MedicationStartTime;
                return MedicationStartDate;
            }
        }
        public DateTime? MedicationEndDateTime
        {
            get
            {
                if (MedicationEndTime != null) return MedicationEndTime;
                return MedicationEndDate;
            }
        }
        public void TidyMedicationDateTimePreValidate()
        {
            if (MedicationStartDate == DateTime.MinValue) MedicationStartDate = null;
            if (MedicationStartDate != null) MedicationStartDate = ((DateTime)MedicationStartDate).Date;
            if (MedicationStartTime == DateTime.MinValue) MedicationStartTime = null;

            if (MedicationEndDate == DateTime.MinValue) MedicationEndDate = null;
            if (MedicationEndDate != null) MedicationEndDate = ((DateTime)MedicationEndDate).Date;
            if (MedicationEndTime == DateTime.MinValue) MedicationEndTime = null;
        }
        public void TidyMedicationDateTimePostValidate()
        {
            if (MedicationStartDate == DateTime.MinValue) MedicationStartDate = null;
            if (MedicationStartDate != null) MedicationStartDate = ((DateTime)MedicationStartDate).Date;
            if ((MedicationStartDate == null) || (MedicationStartTime == DateTime.MinValue)) MedicationStartTime = null;
            if (MedicationStartTime != null)
            {
                MedicationStartTime = new DateTime(
                MedicationStartDate.Value.Year, MedicationStartDate.Value.Month, MedicationStartDate.Value.Day,
                MedicationStartTime.Value.Hour, MedicationStartTime.Value.Minute, 0);
            }
            if (MedicationEndDate == DateTime.MinValue) MedicationEndDate = null;
            if (MedicationEndDate != null) MedicationEndDate = ((DateTime)MedicationEndDate).Date;
            if ((MedicationEndDate == null) || (MedicationEndTime == DateTime.MinValue)) MedicationEndTime = null;
            if (MedicationEndTime != null)
            {
                MedicationEndTime = new DateTime(
                MedicationEndDate.Value.Year, MedicationEndDate.Value.Month, MedicationEndDate.Value.Day,
                MedicationEndTime.Value.Hour, MedicationEndTime.Value.Minute, 0);
            }
        }
    }

    public partial class PatientMedicationAdministration
    {
        [RoundtripOriginal]
        [DataMember]
#if !SILVERLIGHT
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
#endif
        public DateTimeOffset? AdministrationDateTimeOffSet
        {
            get
            {
                return (AdministrationDatePart == null || AdministrationTimePart == null || AdministrationOffSetPart == null) ? (DateTimeOffset?)null : new DateTimeOffset(this.AdministrationDatePart.Value.Year, this.AdministrationDatePart.Value.Month, this.AdministrationDatePart.Value.Day, this.AdministrationTimePart.Value.Hour, this.AdministrationTimePart.Value.Minute, 0, this.AdministrationOffSetPart.Value);
            }
            set
            {
                if (value == null)
                {
                    AdministrationDatePart = (DateTime?)null;
                    AdministrationTimePart = (DateTime?)null;
                    AdministrationOffSetPart = (TimeSpan?)null;
                }
                else
                {
                    DateTimeOffset v = (DateTimeOffset)value;
                    AdministrationDatePart = v.DateTime;
                    AdministrationTimePart = v.DateTime;
                    AdministrationOffSetPart = v.Offset;
                }
            }
        }
    }

    public partial class PatientMedicationReconcile
    {
        [RoundtripOriginal]
        [DataMember]
#if !SILVERLIGHT
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
#endif
        public DateTimeOffset? ReconcileDateTimeOffSet
        {
            get
            {
                return (ReconcileDatePart == null || ReconcileTimePart == null || ReconcileOffSetPart == null) ? (DateTimeOffset?)null : new DateTimeOffset(this.ReconcileDatePart.Value.Year, this.ReconcileDatePart.Value.Month, this.ReconcileDatePart.Value.Day, this.ReconcileTimePart.Value.Hour, this.ReconcileTimePart.Value.Minute, 0, this.ReconcileOffSetPart.Value);
            }
            set
            {
                if (value == null)
                {
                    ReconcileDatePart = (DateTime?)null;
                    ReconcileTimePart = (DateTime?)null;
                    ReconcileOffSetPart = (TimeSpan?)null;
                }
                else
                {
                    DateTimeOffset v = (DateTimeOffset)value;
                    ReconcileDatePart = v.DateTime;
                    ReconcileTimePart = v.DateTime;
                    ReconcileOffSetPart = v.Offset;
                }
            }
        }
    }
    
    public partial class PatientMedicationTeaching
    {
        [RoundtripOriginal]
        [DataMember]
#if !SILVERLIGHT
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
#endif
        public DateTimeOffset? TeachingDateTimeOffSet
        {
            get
            {
                return (TeachingDatePart == null || TeachingTimePart == null || TeachingOffSetPart == null) ? (DateTimeOffset?)null : new DateTimeOffset(this.TeachingDatePart.Value.Year, this.TeachingDatePart.Value.Month, this.TeachingDatePart.Value.Day, this.TeachingTimePart.Value.Hour, this.TeachingTimePart.Value.Minute, 0, this.TeachingOffSetPart.Value);
            }
            set
            {
                if (value == null)
                {
                    TeachingDatePart = (DateTime?)null;
                    TeachingTimePart = (DateTime?)null;
                    TeachingOffSetPart = (TimeSpan?)null;
                }
                else
                {
                    DateTimeOffset v = (DateTimeOffset)value;
                    TeachingDatePart = v.DateTime;
                    TeachingTimePart = v.DateTime;
                    TeachingOffSetPart = v.Offset;
                }
            }
        }
    }

    public partial class PatientMedicationManagement
    {
        [RoundtripOriginal]
        [DataMember]
#if !SILVERLIGHT
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
#endif
        public DateTimeOffset? ManagementDateTimeOffSet
        {
            get
            {
                return (ManagementDatePart == null || ManagementTimePart == null || ManagementOffSetPart == null) ? (DateTimeOffset?)null : new DateTimeOffset(this.ManagementDatePart.Value.Year, this.ManagementDatePart.Value.Month, this.ManagementDatePart.Value.Day, this.ManagementTimePart.Value.Hour, this.ManagementTimePart.Value.Minute, 0, this.ManagementOffSetPart.Value);
            }
            set
            {
                if (value == null)
                {
                    ManagementDatePart = (DateTime?)null;
                    ManagementTimePart = (DateTime?)null;
                    ManagementOffSetPart = (TimeSpan?)null;
                }
                else
                {
                    DateTimeOffset v = (DateTimeOffset)value;
                    ManagementDatePart = v.DateTime;
                    ManagementTimePart = v.DateTime;
                    ManagementOffSetPart = v.Offset;
                }
            }
        }
    }
}
