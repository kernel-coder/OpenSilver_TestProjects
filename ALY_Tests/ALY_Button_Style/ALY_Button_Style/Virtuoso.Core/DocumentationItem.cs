#region Usings

using System;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core
{
    public class DocumentationItem
    {
        public int SourceKey { get; set; }
        public DateTime Date { get; set; }
        public int PatientKey { get; set; }
        public int? AdmissionKey { get; set; }
        public int? ServiceTypeKey { get; set; }
        public int? TaskKey { get; set; }
        public int? FormKey { get; set; }
        public Encounter Encounter { get; set; }
        public int? EncounterKey { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool Signed { get; set; }
        public bool Addendum { get; set; }
        public int? AttachedFormKey { get; set; }
    }
}