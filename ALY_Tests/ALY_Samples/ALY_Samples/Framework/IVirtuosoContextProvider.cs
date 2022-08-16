#region Usings

using System.Collections.Generic;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Validation
{
    public interface IVirtuosoContextProvider
    {
        IEnumerable<AdmissionDiscipline> AdmissionDisciplinesByAdmissionKey(int AdmissionKey);
    }
}