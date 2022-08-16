#region Usings

using System;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IGoalService : IModelDataService<Goal>, ICleanup
    {
        PagedEntityCollectionView<Goal> Goals { get; }
        void Remove(DisciplineInGoal entity);
        void Remove(GoalElementInGoal entity);
        void Remove(QuestionGoal entity);
        void Remove(QuestionGoalMapping entity);

        PagedEntityCollectionView<GoalElement> GoalElements { get; }
        void GetSearchAsync(int DisciplineKey, string ShortDescription, string LongDescription, string SearchType);
        void ClearSearch();
        event EventHandler<Events.EntityEventArgs<GoalElement>> OnSearchLoaded;
    }
}