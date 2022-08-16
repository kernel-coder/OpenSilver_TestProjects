#region Usings

using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Server.Data;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Framework
{
    public class PhysicianDataProvider : IPhysicianDataProvider
    {
        public Physician GetPhysicianFromKey(int physicianKey)
        {
            return PhysicianCache.Current.GetPhysicianFromKey(physicianKey);
        }
    }
}