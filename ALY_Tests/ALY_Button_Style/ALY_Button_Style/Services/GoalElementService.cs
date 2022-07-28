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
    [Export(typeof(IGoalElementService))]
    public class GoalElementService : PagedModelBase, IGoalElementService
    {
        public VirtuosoDomainContext Context { get; set; }
        private int _dscp_key;
        private string _short_description;

        public GoalElementService()
        {
            Context = new VirtuosoDomainContext();
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

        #region IModelDataService<GoalElement> Members

        public void Add(GoalElement entity)
        {
            Context.GoalElements.Add(entity);
        }

        public void Remove(GoalElement entity)
        {
            Context.GoalElements.Remove(entity);
        }

        public void Remove(DisciplineInGoalElement entity)
        {
            Context.DisciplineInGoalElements.Remove(entity);
        }

        public void GetAsync(int dscp_key, string short_description)
        {
            _dscp_key = dscp_key;
            _short_description = short_description;
            LoadData();
        }

        public void GetSearchAsync(bool isSystemSearch)
        {
            //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
            //          when isSystemSearch == false, then Inactive checkbox removed from search criteria; however 
            //          we want to always assume that it is checked - e.g. add Inactive==false to query.

            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.GoalElements.Clear();

                int disciplineKey = 0;
                var param = SearchParameters.Where(i => i.Field.Equals("DisciplineKey")).FirstOrDefault();
                if (param != null)
                {
                    disciplineKey = Convert.ToInt32(param.Value);
                }

                var query = Context.GetGoalElementForSearchQuery(disciplineKey);

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
                            case "GoalElementKey":
                                query = query.Where(p => p.GoalElementKey == Convert.ToInt32(searchvalue));
                                break;
                            case "ShortDescription":
                                shortDesc = searchvalue.ToLower();
                                break;
                            case "LongDescription":
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


                //if (_dscp_key > 0)
                //    query = query.Where(g => g.DisciplineKey == _dscp_key);

                if (string.IsNullOrEmpty(_short_description) == false)
                {
                    query = query.Where(g => g.ShortDescription.ToLower().StartsWith(_short_description.ToLower()));
                }

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public void GetAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.GoalElements.Clear();

                var query = Context.GetGoalElementQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "GoalElementKey":
                                query = query.Where(p => p.GoalElementKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Description":
                                query = query.Where(p =>
                                    p.ShortDescription.ToLower().Contains(searchvalue.ToLower()) ||
                                    p.LongDescription.ToLower().Contains(searchvalue.ToLower()));
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

                //if (_dscp_key > 0)
                //    query = query.Where(g => g.DisciplineKey == _dscp_key);

                if (string.IsNullOrEmpty(_short_description) == false)
                {
                    query = query.Where(g => g.ShortDescription.ToLower().StartsWith(_short_description.ToLower()));
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

        public IEnumerable<GoalElement> Items => Context.GoalElements;

        PagedEntityCollectionView<GoalElement> _GoalElements;

        public PagedEntityCollectionView<GoalElement> GoalElements
        {
            get { return _GoalElements; }
            set
            {
                if (_GoalElements != value)
                {
                    _GoalElements = value;
                    this.RaisePropertyChanged(p => p._GoalElements);
                }
            }
        }

        public event EventHandler<EntityEventArgs<GoalElement>> OnLoaded;

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

        public void Clear()
        {
            Dispatcher.BeginInvoke(() =>
            {
                _dscp_key = 0;
                _short_description = string.Empty;
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
            GoalElements.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}