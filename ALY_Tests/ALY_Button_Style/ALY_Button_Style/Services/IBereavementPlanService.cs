#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IBereavementPlanService : IModelDataService<BereavementPlan>, ICleanup
    {
        PagedEntityCollectionView<BereavementPlan> BereavementPlans { get; }
    }
}