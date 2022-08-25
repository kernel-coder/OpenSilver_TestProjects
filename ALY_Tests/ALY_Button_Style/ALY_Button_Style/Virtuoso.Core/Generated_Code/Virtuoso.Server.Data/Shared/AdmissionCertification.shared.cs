using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Virtuoso.Server.Data
{
    public partial class AdmissionCertification
    {
        public String DisplayDescription
        {
            get
            {
                return "Period Number : " + PeriodNumber + " - "
                + (PeriodStartDate == null ? "" : ((DateTime)PeriodStartDate).ToShortDateString()) + " Thru "
                + (PeriodEndDate == null ? "" : ((DateTime)PeriodEndDate).ToShortDateString());
            }
        }
    }
}

