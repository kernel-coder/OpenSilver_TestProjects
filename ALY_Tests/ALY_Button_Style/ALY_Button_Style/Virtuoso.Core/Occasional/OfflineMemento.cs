#region Usings

using System;
using System.Collections.Generic;

#endregion

namespace Virtuoso.Core.Occasional
{
    public class IDGeneratorMemento
    {
        public int Seed { get; set; }
    }

    public enum PatientHomeMementoEnum
    {
        Task,
        Patient,
        Note,
        Alert
    }

    public class AlertManagerMemento
    {
        public Dictionary<PatientHomeMementoEnum, String> DomainContextJSONDictionary { get; set; }

        public AlertManagerMemento()
        {
            DomainContextJSONDictionary = new Dictionary<PatientHomeMementoEnum, String>();
        }
    }
}