#region Usings

using System;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IServiceLineGroupingService
    {
        AdmissionGroup CurrentGroup { get; }
        AdmissionGroup CurrentGroup2 { get; }
        AdmissionGroup CurrentGroup3 { get; }
        AdmissionGroup CurrentGroup4 { get; }
        AdmissionGroup CurrentGroup5 { get; }
        Guid? CareCoordinator { get; set; }
    }
}