#region Usings

using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Framework
{
    public class NonServiceTypeDataProvider : INonServiceTypeDataProvider
    {
        IQueryable<NonServiceType> INonServiceTypeDataProvider.GetDuplicateNonServiceType(
            NonServiceType SelectedNonServiceType)
        {
            IQueryable<NonServiceType> nonServiceType = null;

            if (!SelectedNonServiceType.Inactive)
            {
                nonServiceType = NonServiceTypeCache.GetNonServiceTypes()
                    .Where(n => (n.NonServiceTypeID == SelectedNonServiceType.NonServiceTypeID)
                                && !n.Inactive
                    ).AsQueryable();
            }

            return nonServiceType;
        }
    }
}