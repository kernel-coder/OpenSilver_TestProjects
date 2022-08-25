#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Ria.Sync;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.Goal)]
    [Export(typeof(ICache))]
    public class GoalCache : ReferenceCacheBase
    {
        public static GoalCache Current { get; private set; }
        private Action Callback;

        [ImportingConstructor]
        public GoalCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Goal, "006")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("GoalCache already initialized.");
            }

            Current = this;
        }

        public override async System.Threading.Tasks.Task Load(DateTime? lastUpdatedDate, bool isOnline,
            Action callback, bool force = false)
        {
            Callback = callback;

            LastUpdatedDate = lastUpdatedDate;
            Ticks = LastUpdatedDate?.Ticks ?? 0;
            TotalRecords = 0;

            await RemovePriorVersion();

            if (isLoading)
            {
                return;
            }

            isLoading = true;

            Context.EntityContainer.Clear();

            if ((isOnline && Ticks > 0)
                || (Ticks == 0 && isOnline &&
                    await CacheExists() ==
                    false)) //Ticks = 0, but online, got LastUpdatedDdate = NULL from GetReferenceDataInfo, still need to query server for data to build cache
            {
                if ((await RefreshReferenceCacheAsync(Ticks)) || force)
                {
                    DomainContextLoadBatch batch = new DomainContextLoadBatch(DataLoadComplete);

                    batch.Add(Context.Load(Context.GetGoalQuery(), LoadBehavior.RefreshCurrent, false));
                    batch.Add(Context.Load(Context.GetGoalElementQuery(), LoadBehavior.RefreshCurrent, false));
                }
                else
                {
                    LoadFromDisk(callback);
                }
            }
            else
            {
                LoadFromDisk(callback);
            }
        }

        private async void DataLoadComplete(DomainContextLoadBatch batch)
        {
            if (batch.FailedOperationCount > 0)
            {
                foreach (var fop in batch.FailedOperations)
                    if (fop.HasError)
                    {
                        Log(TraceEventType.Warning, "Goal cache load error", fop.Error);
                    }

                Context.EntityContainer.Clear();
                LoadFromDisk(Callback);
            }
            else
            {
                await PurgeAndSave();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    TotalRecords = 0;
                    foreach (var _set in Context.EntityContainer.EntitySets)
                        TotalRecords += _set.Count;
                    isLoading = false;
                    Callback?.Invoke();
                    Messenger.Default.Send(CacheName, "CacheLoaded");
                });
            }
        }

        protected override void OnRIACacheLoaded()
        {
            TotalRecords = Context.Goals.Count();
        }

        public static List<Goal> GetGoals(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            var ret = (from g in Current.Context.Goals.OrderBy(p => p.LongDescription) select g).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Goal { GoalKey = 0, LongDescription = " ", ShortDescription = " " });
            }

            return ret;
        }

        public static List<Goal> GetActiveGoals(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            var ret = (from g in Current.Context.Goals.Where(p => p.Inactive == false).OrderBy(p => p.LongDescription)
                select g).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Goal { GoalKey = 0, LongDescription = " ", ShortDescription = " " });
            }

            return ret;
        }

        public static Goal GetGoalFromKey(int? goalKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            if (goalKey == null)
            {
                return null;
            }

            Goal f = (from c in Current.Context.Goals.Where(p => p.GoalKey == goalKey) select c).FirstOrDefault();
            if ((f == null) && (goalKey != 0))
            {
                MessageBox.Show(String.Format(
                    "Error GoalCache.GetGoalFromKey: GoalKey {0} is not defined.  Contact your system administrator.",
                    goalKey.ToString()));
            }

            return f;
        }

        public static List<DisciplineInGoal> GetGoalDisciplinesFromGoalKey(int? goalKey)
        {
            Current?.EnsureCacheReady();
            Goal g = GetGoalFromKey(goalKey);
            if (g == null)
            {
                return null;
            }

            if (g.DisciplineInGoal == null)
            {
                return null;
            }

            if (g.DisciplineInGoal.Any() == false)
            {
                return null;
            }

            return g.DisciplineInGoal.ToList();
        }

        public static List<DisciplineInGoalElement> GetGoalElementsDisciplinesFromGoalKey(int? goalelementKey)
        {
            Current?.EnsureCacheReady();
            if (goalelementKey == null)
            {
                return null;
            }

            GoalElement g = GetGoalElementByKey((int)goalelementKey);
            if (g == null)
            {
                return null;
            }

            if (g.DisciplineInGoalElement == null)
            {
                return null;
            }

            if (g.DisciplineInGoalElement.Any() == false)
            {
                return null;
            }

            return g.DisciplineInGoalElement.ToList();
        }

        public static List<GoalElement> GetGoalElementsFromSearch(string search)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            IEnumerable<GoalElement> query = Current.Context.GoalElements;

            if (!string.IsNullOrEmpty(search))
            {
                var searchpieces = search.Split(' ').ToList();

                foreach (var searchpiece in searchpieces)
                {
                    var piece = searchpiece.ToLower();

                    query = query.Where(ge =>
                        ge.ShortDescription.ToLower().Contains(piece) || ge.LongDescription.ToLower().Contains(piece));
                }
            }

            var ret = (from g in query.OrderBy(p => p.LongDescription) select g).ToList();

            return ret;
        }

        public static List<GoalElement> GetGoalElementsFromSearch(string ShortDescription, string LongDescription,
            bool RequiresOrdersOnly, bool StartsWith)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            IEnumerable<GoalElement> query = Current.Context.GoalElements;


            if (!string.IsNullOrEmpty(ShortDescription))
            {
                string shortDesc = ShortDescription.ToLower();
                query = query.Where(g => (StartsWith
                                          && (g.ShortDescription.Substring(0, shortDesc.Length).ToLower() == shortDesc)
                                         )
                                         || (!StartsWith
                                             && (g.ShortDescription.ToLower().Contains(shortDesc))
                                         )
                );
            }

            if (!string.IsNullOrEmpty(LongDescription))
            {
                string longDesc = LongDescription.ToLower();
                query = query.Where(g => (StartsWith
                                          && (g.LongDescription.Substring(0, longDesc.Length).ToLower() == longDesc)
                                         )
                                         || (!StartsWith
                                             && (g.LongDescription.ToLower().Contains(longDesc))
                                         )
                );
            }

            if (RequiresOrdersOnly)
            {
                query = query.Where(g => g.Orders);
            }

            var ret = (from g in query.OrderBy(p => p.LongDescription) select g).ToList();

            return ret;
        }

        public static List<GoalElement> GetGoalElementsFromDiscriptionSearch(string ShortDescription,
            string LongDescription, bool RequiresOrdersOnly, bool StartsWith)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            IEnumerable<GoalElement> query = Current.Context.GoalElements;

            if (!string.IsNullOrEmpty(ShortDescription))
            {
                string shortDesc = ShortDescription.ToLower();

                query = query.Where(ge => (StartsWith
                                           && (ge.ShortDescription.Substring(0, shortDesc.Length).ToLower()
                                               .Contains(shortDesc))
                                          )
                                          || (!StartsWith
                                              && (ge.ShortDescription.ToLower().Contains(shortDesc))
                                          )
                );
            }

            if (!string.IsNullOrEmpty(LongDescription))
            {
                string longDesc = LongDescription.ToLower();

                query = query.Where(ge => (StartsWith
                                           && (ge.LongDescription.Substring(0, longDesc.Length).ToLower()
                                               .Contains(longDesc))
                                          )
                                          || (!StartsWith
                                              && (ge.LongDescription.ToLower().Contains(longDesc))
                                          )
                );
            }

            if (RequiresOrdersOnly)
            {
                query = query.Where(g => g.Orders);
            }

            var ret = (from g in query.OrderBy(p => p.LongDescription) select g).ToList();

            return ret;
        }

        public static QuestionGoal GetQuestionGoalByKey(int questiongoalKey)
        {
            Current?.EnsureCacheReady();
            if ((DynamicFormCache.Current == null) || (DynamicFormCache.Current.Context == null) ||
                (DynamicFormCache.Current.Context.Questions == null))
            {
                return null;
            }

            return Current.Context.QuestionGoals.Where(p => p.QuestionGoalKey == questiongoalKey).FirstOrDefault();
        }

        public static GoalElement GetGoalElementByKey(int goalElementKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            return Current.Context.GoalElements.Where(g => g.GoalElementKey == goalElementKey).FirstOrDefault();
        }

        public static bool IsDuplicateGoalElement(GoalElement goalElement)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.GoalElements == null))
            {
                return false;
            }

            if (goalElement == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(goalElement.ShortDescription))
            {
                return false;
            }

            return Current.Context.GoalElements.Where(p =>
                ((p.GoalElementKey != goalElement.GoalElementKey) && (p.ShortDescription != null) &&
                 (p.ShortDescriptionFirst20.ToUpper() == goalElement.ShortDescriptionFirst20.ToUpper()))).Any();
        }

        public static bool IsTherapyGoal(int goalKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Goals == null))
            {
                return false;
            }

            List<DisciplineInGoal> digList =
                Current.Context.DisciplineInGoals.Where(d => d.GoalKey == goalKey).ToList();
            if (digList == null)
            {
                return false;
            }

            foreach (DisciplineInGoal dig in digList)
            {
                Discipline dd = Current.Context.Disciplines.Where(d => d.DisciplineKey == dig.DisciplineKey)
                    .FirstOrDefault();
                if ((dd != null) && (dd.IsTherapyDiscipline))
                {
                    return true;
                }
            }

            return false;
        }
    }
}