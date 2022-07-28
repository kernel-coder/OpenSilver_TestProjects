using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Virtuoso.Server.Data
{
    public partial class AdmissionCommunication
    {
        [RoundtripOriginal]
        [DataMember]
#if !SILVERLIGHT
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
#endif
        public DateTimeOffset CompletedDateTimeOffSet
        {
            get
            {
                if (this.CompletedDatePart.HasValue && this.CompletedTimePart.HasValue)
                    return new DateTimeOffset(this.CompletedDatePart.Value.Year, 
                                              this.CompletedDatePart.Value.Month, 
                                              this.CompletedDatePart.Value.Day, 
                                              this.CompletedTimePart.Value.Hour, 
                                              this.CompletedTimePart.Value.Minute, 
                                              0, 
                                              this.CompletedOffSetPart.Value);

                return new DateTimeOffset();
            }
            set
            {
                CompletedDatePart = value.DateTime;
                CompletedTimePart = value.DateTime;
                CompletedOffSetPart = value.Offset;
            }
        }
    }
}
