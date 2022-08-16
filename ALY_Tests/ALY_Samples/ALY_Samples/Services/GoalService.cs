#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IGoalService))]
    public class GoalService : PagedModelBase, IGoalService
    {
        public VirtuosoDomainContext Context { get; set; }

        public GoalService()
        {
            Context = new VirtuosoDomainContext();
            Goals = new PagedEntityCollectionView<Goal>(Context.Goals, this);

            GoalElements = new PagedEntityCollectionView<GoalElement>(Context.GoalElements, this);

            Context.PropertyChanged += Context_PropertyChanged;
        }

        #region PagedModelBase Members

        public override void LoadData()
        {
            if (IsLoading || Context == null)
            {
                return;
            }

            IsLoading = true;

            GetAsync();
        }

        #endregion

        #region IModelDataService<Goal> Members

        public void Add(Goal entity)
        {
            Context.Goals.Add(entity);
        }

        public void Remove(Goal entity)
        {
            Context.Goals.Remove(entity);
        }

        public void Remove(GoalElementInGoal entity)
        {
            Context.GoalElementInGoals.Remove(entity);
        }

        public void Remove(DisciplineInGoal entity)
        {
            Context.DisciplineInGoals.Remove(entity);
        }

        public void Remove(QuestionGoal entity)
        {
            Context.QuestionGoals.Remove(entity);
        }

        public void Remove(QuestionGoalMapping entity)
        {
            Context.QuestionGoalMappings.Remove(entity);
        }

        public void GetAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.Goals.Clear();

                var query = Context.GetGoalQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "GoalKey":
                                query = query.Where(p => p.GoalKey == Convert.ToInt32(searchvalue));
                                break;
                            case "CodeValue":
                                query = query.Where(p => p.CodeValue.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Description":
                                query = query.Where(p => p.LongDescription.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Inactive":
                                bool inactive = Convert.ToBoolean(searchvalue);
                                if (!inactive)
                                {
                                    query = query.Where(p => p.Inactive == inactive);
                                }

                                break;
                        }
                    }
                }
                else
                {
                    query = query.Where(p => p.Inactive == false);
                }

                query.IncludeTotalCount = true;

                if (PageSize > 0)
                {
                    query = query.Skip(PageSize * PageIndex);
                    query = query.Take(PageSize);
                }

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public IEnumerable<Goal> Items => Context.Goals;

        PagedEntityCollectionView<Goal> _Goals;

        public PagedEntityCollectionView<Goal> Goals
        {
            get { return _Goals; }
            set
            {
                if (_Goals != value)
                {
                    _Goals = value;
                    this.RaisePropertyChanged(p => p.Goals);
                }
            }
        }

        public event EventHandler<EntityEventArgs<Goal>> OnLoaded;

        public event EventHandler<ErrorEventArgs> OnSaved;

        public bool SaveAllAsync()
        {
            var open_or_invalid = OpenOrInvalidObjects(Context);
            if (open_or_invalid) //TODO: should we raise/return an error or something???
            {
                PendingSubmit = true;
                return false;
            }

            PendingSubmit = false;

            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                IsLoading = true;
                Context.SubmitChanges(g => HandleErrorResults(g, OnSaved), null);
            });

            return true;
        }

        public void RejectChanges()
        {
            Context.RejectChanges();
        }

        #endregion

        #region IModelDataService<GoalElement> Members

        public event EventHandler<EntityEventArgs<GoalElement>> OnSearchLoaded;

        PagedEntityCollectionView<GoalElement> _GoalElements;

        public PagedEntityCollectionView<GoalElement> GoalElements
        {
            get { return _GoalElements; }
            set
            {
                if (_GoalElements != value)
                {
                    _GoalElements = value;
                    this.RaisePropertyChanged(p => p.GoalElements);
                }
            }
        }

        //Used by global search dialog
        public void GetSearchAsync(bool isSystemSearch)
        {
            //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
            //          when isSystemSearch == false, then Inactive checkbox removed from search criteria; however 
            //          we want to always assume that it is checked - e.g. add Inactive==false to query.

            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Clear();

                int disciplineKey = 0;
                var param = SearchParameters.Where(i => i.Field.Equals("DisciplineKey")).FirstOrDefault();
                if (param != null)
                {
                    disciplineKey = Convert.ToInt32(param.Value);
                }

                var query = Context.GetGoalForSearchQuery(disciplineKey);

                if (SearchParameters.Any())
                {
                    string shortDesc = null;
                    string longDesc = null;
                    bool startsWith = false;

                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "GoalKey":
                                query = query.Where(p => p.GoalKey == Convert.ToInt32(searchvalue));
                                break;
                            case "CodeValue":
                                query = query.Where(p => p.CodeValue.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "ShortDescription":
                                //query = query.Where(p => p.ShortDescription.ToLower().Contains(searchvalue.ToLower()));
                                shortDesc = searchvalue.ToLower();
                                break;
                            case "LongDescription":
                                //query = query.Where(p => p.LongDescription.ToLower().Contains(searchvalue.ToLower()));
                                longDesc = searchvalue.ToLower();
                                break;
                            case "StartsWith":
                                startsWith = Convert.ToBoolean(searchvalue);
                                break;
                            case "Inactive":
                                bool inactive = Convert.ToBoolean(searchvalue);
                                if (!inactive)
                                {
                                    query = query.Where(p => p.Inactive == inactive);
                                }

                                break;
                        }
                    }

                    if (isSystemSearch == false)
                    {
                        //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
                        query = query.Where(p => p.Inactive == false);
                    }

                    if (!string.IsNullOrEmpty(shortDesc))
                    {
                        if (startsWith)
                        {
                            query = query.Where(p =>
                                p.ShortDescription.Substring(0, shortDesc.Length).ToLower() == shortDesc.ToLower());
                        }
                        else
                        {
                            query = query.Where(p => p.ShortDescription.ToLower().Contains(shortDesc.ToLower()));
                        }
                    }

                    if (!string.IsNullOrEmpty(longDesc))
                    {
                        if (startsWith)
                        {
                            query = query.Where(p =>
                                p.LongDescription.Substring(0, longDesc.Length).ToLower() == longDesc.ToLower());
                        }
                        else
                        {
                            query = query.Where(p => p.LongDescription.ToLower().Contains(longDesc.ToLower()));
                        }
                    }
                }
                else
                {
                    query = query.Where(p => p.Inactive == false);
                }

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded, false),
                    null
                );
            });
        }

        //Used by popups on forms
        public void GetSearchAsync(int DisciplineKey, string ShortDescription, string LongDescription,
            string SearchType)
        {
            GetInternalSearchAsync(DisciplineKey, ShortDescription, LongDescription, SearchType);
        }

        private void GetInternalSearchAsync(int DisciplineKey, string ShortDescription, string LongDescription,
            string SearchType)
        {
            Dispatcher.BeginInvoke(() =>
            {
                var query = Context.GetGoalElementQuery().OrderBy(p => p.LongDescription);

                if (!string.IsNullOrEmpty(ShortDescription))
                {
                    string shortDesc = ShortDescription.ToLower();
                    query = query.Where(g => ((SearchType == "StartsWith")
                                              && (g.ShortDescription.Substring(0, shortDesc.Length).ToLower() ==
                                                  shortDesc)
                                             )
                                             || ((SearchType != "StartsWith")
                                                 && (g.ShortDescription.ToLower().Contains(shortDesc))
                                             )
                    );
                }

                if (!string.IsNullOrEmpty(LongDescription))
                {
                    string longDesc = LongDescription.ToLower();
                    query = query.Where(g => ((SearchType == "StartsWith")
                                              && (g.LongDescription.Substring(0, longDesc.Length).ToLower() == longDesc)
                                             )
                                             || ((SearchType != "StartsWith")
                                                 && (g.LongDescription.ToLower().Contains(longDesc))
                                             )
                    );
                }

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnSearchLoaded, false),
                    null
                );
            });
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
        }

        public void ClearSearch()
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.GoalElements.Clear();
                this.RaisePropertyChanged(p => p.GoalElements);
            });
        }

        #endregion
        
        public bool ContextHasChanges => Context.HasChanges;

        void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public void Cleanup()
        {
            Goals.Cleanup();
            GoalElements.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}