using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//InsuranceCoordinator.shared.cs -> server.data.shared
namespace Virtuoso.Server.Data
{
    public enum WorklistType
    {
        UnverifiedInsurances = 1,
        EligibilityValidations = 2,
        NoActiveCoveragePlan = 3,
        AuthorizationAlerts = 4,
        NoAuthOnFile = 5
    };

    public enum InsuranceVerificationStatus
    {
        InProcess = 1,
        ReturnToWorkList = 2,
        Processed = 3
    };

    // a partial class needs to be defined for the client to have access to the enums
    // this class has no functionality and is not implemented anywhere else
    public partial class InsuranceCoordinator
    {
    }
}
