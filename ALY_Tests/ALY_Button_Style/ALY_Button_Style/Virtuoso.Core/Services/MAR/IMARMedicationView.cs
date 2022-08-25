using System.Collections.Generic;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Services.MAR
{
    public interface IMARMedicationView
    {
        List<PatientMedication> GetMARMedicationList(bool returnDiscontinuedMedications = false);
    }
}