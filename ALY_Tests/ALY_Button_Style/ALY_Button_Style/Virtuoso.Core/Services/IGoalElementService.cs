#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IGoalElementService : IModelDataService<GoalElement>, ICleanup
    {
        void GetAsync(int dscp_key, string short_description);

        PagedEntityCollectionView<GoalElement> GoalElements { get; }
        void Remove(DisciplineInGoalElement entity);
    }
}