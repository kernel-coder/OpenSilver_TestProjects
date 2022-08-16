using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public interface IInsuranceDataProvider
    {
        Insurance GetInsuranceFromInsuranceKey(int? key);
    }
}
