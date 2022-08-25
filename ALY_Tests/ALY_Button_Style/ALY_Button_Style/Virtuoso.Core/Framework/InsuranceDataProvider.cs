#region Usings

using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Framework
{
    public class InsuranceDataProvider : IInsuranceDataProvider
    {
        public Insurance GetInsuranceFromInsuranceKey(int? key)
        {
            return InsuranceCache.GetInsuranceFromKey(key);
        }
    }
}