#region Usings

using System;
using System.Collections.Generic;

#endregion

namespace Virtuoso.Core.Alerts
{
    public class AlertSearchCriteria
    {
        public int PatientKey { get; set; }
        public Guid? CareCoordinatorKey { get; set; }
        public List<int> SelectedDisciplines { get; set; }

        public int ServiceLineKey { get; set; }
        public int InsuranceKey { get; set; }
        public List<int> ServiceLineGroup1Keys { get; set; }
        public List<int> ServiceLineGroup2Keys { get; set; }
        public List<int> ServiceLineGroup3Keys { get; set; }
        public List<int> ServiceLineGroup4Keys { get; set; }
        public List<int> ServiceLineGroup5Keys { get; set; }

        public int ExcludeKey { get; set; }
    }
}