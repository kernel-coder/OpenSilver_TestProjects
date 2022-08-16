using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Virtuoso.Server.Data
{
    public partial class AdmissionMedicationMAR
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

    public partial class AdmissionSiteOfService
    {
        [RoundtripOriginal]
        [DataMember]
#if !SILVERLIGHT
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
#endif
        public DateTimeOffset? SiteOfServiceFromDateTimeOffSet
        {
            get
            {
                return (SiteOfServiceFromDatePart == null || SiteOfServiceFromTimePart == null || SiteOfServiceFromOffSetPart == null) ? (DateTimeOffset?)null : new DateTimeOffset(this.SiteOfServiceFromDatePart.Value.Year, this.SiteOfServiceFromDatePart.Value.Month, this.SiteOfServiceFromDatePart.Value.Day, this.SiteOfServiceFromTimePart.Value.Hour, this.SiteOfServiceFromTimePart.Value.Minute, 0, this.SiteOfServiceFromOffSetPart.Value);
            }
            set
            {
                if (value == null)
                {
                    SiteOfServiceFromDatePart = (DateTime?)null;
                    SiteOfServiceFromTimePart = (DateTime?)null;
                    SiteOfServiceFromOffSetPart = (TimeSpan?)null;
                }
                else
                {
                    DateTimeOffset v = (DateTimeOffset)value;
                    SiteOfServiceFromDatePart = v.DateTime;
                    SiteOfServiceFromTimePart = v.DateTime;
                    SiteOfServiceFromOffSetPart = v.Offset;
                }
            }
        }
        public DateTimeOffset? SiteOfServiceThruDateTimeOffSet
        {
            get
            {
                return (SiteOfServiceThruDatePart == null || SiteOfServiceThruTimePart == null || SiteOfServiceThruOffSetPart == null) ? (DateTimeOffset?)null : new DateTimeOffset(this.SiteOfServiceThruDatePart.Value.Year, this.SiteOfServiceThruDatePart.Value.Month, this.SiteOfServiceThruDatePart.Value.Day, this.SiteOfServiceThruTimePart.Value.Hour, this.SiteOfServiceThruTimePart.Value.Minute, 0, this.SiteOfServiceThruOffSetPart.Value);
            }
            set
            {
                if (value == null)
                {
                    SiteOfServiceThruDatePart = (DateTime?)null;
                    SiteOfServiceThruTimePart = (DateTime?)null;
                    SiteOfServiceThruOffSetPart = (TimeSpan?)null;
                }
                else
                {
                    DateTimeOffset v = (DateTimeOffset)value;
                    SiteOfServiceThruDatePart = v.DateTime;
                    SiteOfServiceThruTimePart = v.DateTime;
                    SiteOfServiceThruOffSetPart = v.Offset;
                }
            }
        }
    }

    public partial class AdmissionDisciplineFrequency
    {
        //[DataMember]
        //[System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsPRN
        {
            //// Dummy setter so our 'cloning' methods don't blow up.
            //set { bool i = value; }
            get { return CycleCode == "ASNEEDED" ? true : false; }
        }
    }

}
