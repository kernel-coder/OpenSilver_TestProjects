#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Offline;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Model;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IGoalManagement
    {
        void UpdateGoals(QuestionUI q, string response, string subresponse, string reason, bool remove, int? keytouse);
        bool ValidateGoals(bool isAssistant, bool validatePlannedGoalElementsAreAddressed);
        CollectionViewSource PrintCollection { get; set; }
        bool AcceptGoalForFilter(AdmissionGoal ag, bool forceVisitPlan = false, bool forceInterDisc = false);
        RelayCommand CloseGoalCommand { get; set; }
        RelayCommand CloseGoalElementCommand { get; set; }
        event EventHandler PopupDataTemplateChanged;
        event EventHandler ShortLongTermWarningMessageChanged;
        string PopupDataTemplate { get; set; }
        string ShortLongTermWarningMessage { get; set; }

        void DataTemplateLoaded();
    }

    public class GoalManagerService
    {
        VirtuosoDomainContext Context = new VirtuosoDomainContext();

        #region Single Discipline Controler

        public System.Threading.Tasks.Task<IEnumerable<Goal>> GetDisciplineInGoalByDisciplineKeyAsync(
            int ServiceTypeKey, string ShortDescription, string LongDescription, bool StartsWith)
        {
            if (EntityManager.Current.IsOnline)
            {
                return GetDisciplineInGoalByDisciplineKeyOnlineAsync(ServiceTypeKey, ShortDescription, LongDescription,
                    StartsWith);
            }

            return GetGoalByDisciplineKeyOfflineAsync(ServiceTypeKey, ShortDescription, LongDescription, StartsWith);
        }

        public System.Threading.Tasks.Task<IEnumerable<GoalElement>> GetDisciplineInGoalElementByDisciplineKeyAsync(
            int ServiceTypeKey, string ShortDescription, string LongDescription, bool RequiresOrdersOnly,
            bool StartsWith)
        {
            if (EntityManager.Current.IsOnline)
            {
                return GetDisciplineInGoalElementByDisciplineKeyOnlineAsync(ServiceTypeKey, ShortDescription,
                    LongDescription, RequiresOrdersOnly, StartsWith);
            }

            return GetGoalElementByDisciplineKeyOfflineAsync(ServiceTypeKey, ShortDescription, LongDescription,
                RequiresOrdersOnly, StartsWith);
        }

        #endregion

        #region Multi Discipline Controler

        public System.Threading.Tasks.Task<IEnumerable<Goal>> GetDisciplineInGoalByDisciplineKeyListAsync(
            List<int> DisciplineKeyList, string ShortDescription, string LongDescription, bool StartsWith)
        {
            if (EntityManager.Current.IsOnline)
            {
                return GetDisciplineInGoalByDisciplineKeyListOnlineAsync(DisciplineKeyList, ShortDescription,
                    LongDescription, StartsWith);
            }

            return GetGoalByDisciplineKeyListOfflineAsync(DisciplineKeyList, ShortDescription, LongDescription,
                StartsWith);
        }

        public System.Threading.Tasks.Task<IEnumerable<GoalElement>> GetDisciplineInGoalElementByDisciplineKeyListAsync(
            List<int> DisciplineKeyList, string ShortDescription, string LongDescription, bool RequiresOrdersOnly,
            bool StartsWith)
        {
            if (EntityManager.Current.IsOnline)
            {
                return GetDisciplineInGoalElementByDisciplineKeyListOnlineAsync(DisciplineKeyList, ShortDescription,
                    LongDescription, RequiresOrdersOnly, StartsWith);
            }

            return GetGoalElementByDisciplineKeyListOfflineAsync(DisciplineKeyList, ShortDescription, LongDescription,
                RequiresOrdersOnly, StartsWith);
        }

        #endregion

        #region OFFLINE Single Discipline

        public System.Threading.Tasks.Task<IEnumerable<Goal>> GetGoalByDisciplineKeyOfflineAsync(int ServiceTypeKey,
            string ShortDescription, string LongDescription, bool StartsWith)
        {
            var taskCompletionSource = new System.Threading.Tasks.TaskCompletionSource<IEnumerable<Goal>>();
            var _disciplineKey = ServiceTypeCache.GetDisciplineKey(ServiceTypeKey);
            var goals = GoalCache.GetGoals();
            if (goals != null)
            {
                var _ret = goals
                    .Where(g => g.DisciplineInGoal.Any(dig => dig.DisciplineKey == _disciplineKey)
                                || !g.DisciplineInGoal.Any()
                    )
                    .Where(g => g.LockDescription == false && !g.Inactive);
                //if (!string.IsNullOrEmpty(GoalSearchString))
                //{
                //    var _searchText = GoalSearchString.ToLower();
                //    _ret = goals.Where(g => g.ShortDescription.ToLower().Contains(_searchText) || g.LongDescription.ToLower().Contains(_searchText));
                //}


                if (!string.IsNullOrEmpty(ShortDescription))
                {
                    string shortDesc = ShortDescription.ToLower();
                    _ret = _ret.Where(g => (StartsWith
                                            && (g.ShortDescription.ToLower().Substring(0, shortDesc.Length) ==
                                                shortDesc)
                                           )
                                           || (!StartsWith
                                               && g.ShortDescription.ToLower().Contains(shortDesc)
                                           )
                    );
                }

                if (!string.IsNullOrEmpty(LongDescription))
                {
                    string longDesc = LongDescription.ToLower();
                    _ret = _ret.Where(g => (StartsWith
                                            && (g.LongDescription.ToLower().Substring(0, longDesc.Length) == longDesc)
                                           )
                                           || (!StartsWith
                                               && g.LongDescription.ToLower().Contains(longDesc)
                                           )
                    );
                }

                taskCompletionSource.TrySetResult(_ret.ToList());
            }
            else
            {
                taskCompletionSource.TrySetResult(new List<Goal>());
            }

            return taskCompletionSource.Task;
        }

        private System.Threading.Tasks.Task<IEnumerable<GoalElement>> GetGoalElementByDisciplineKeyOfflineAsync(
            int ServiceTypeKey, string ShortDescription, string LongDescription, bool RequiresOrdersOnly,
            bool StartsWith)
        {
            var taskCompletionSource = new System.Threading.Tasks.TaskCompletionSource<IEnumerable<GoalElement>>();
            var _disciplineKey = ServiceTypeCache.GetDisciplineKey(ServiceTypeKey);
            var ret = GoalCache.GetGoalElementsFromDiscriptionSearch(ShortDescription, LongDescription,
                RequiresOrdersOnly, StartsWith);
            if (ret != null)
            {
                var _ret = (ret.Where(g => !g.Inactive &&
                                           (g.DisciplineInGoalElement.Any(dige => dige.DisciplineKey == _disciplineKey)
                                            || !g.DisciplineInGoalElement.Any()
                                           ))
                    ).ToList();
                taskCompletionSource.TrySetResult(_ret);
            }
            else
            {
                taskCompletionSource.TrySetResult(new List<GoalElement>());
            }

            return taskCompletionSource.Task;
        }

        #endregion

        #region OFFLINE Multi Discipline

        public System.Threading.Tasks.Task<IEnumerable<Goal>> GetGoalByDisciplineKeyListOfflineAsync(
            List<int> DisciplineList, string ShortDescription, string LongDescription, bool StartsWith)
        {
            var taskCompletionSource = new System.Threading.Tasks.TaskCompletionSource<IEnumerable<Goal>>();
            var goals = GoalCache.GetGoals();
            if (goals != null)
            {
                var _ret = goals
                    .Where(g => !g.Inactive &&
                                (g.DisciplineInGoal.Any(dig => DisciplineList.Contains(dig.DisciplineKey))
                                 || !g.DisciplineInGoal.Any()
                                ))
                    .Where(g => g.LockDescription == false);
                //if (!string.IsNullOrEmpty(GoalSearchString))
                //{
                //    var _searchText = GoalSearchString.ToLower();
                //    _ret = goals.Where(g => g.ShortDescription.ToLower().Contains(_searchText) || g.LongDescription.ToLower().Contains(_searchText));
                //}

                string shortDesc = null;
                string longDesc = null;
                if (!string.IsNullOrEmpty(ShortDescription))
                {
                    shortDesc = ShortDescription.ToLower();
                    _ret = _ret.Where(g => (StartsWith
                                            && (g.ShortDescription.ToLower().Substring(0, shortDesc.Length) ==
                                                shortDesc)
                                           )
                                           || (!StartsWith
                                               && g.ShortDescription.ToLower().Contains(shortDesc)
                                           )
                    );
                }


                if (!string.IsNullOrEmpty(LongDescription))
                {
                    longDesc = LongDescription.ToLower();
                    _ret = _ret.Where(g => (StartsWith
                                            && (g.LongDescription.ToLower().Substring(0, longDesc.Length) == longDesc)
                                           )
                                           || (!StartsWith
                                               && g.LongDescription.ToLower().Contains(longDesc)
                                           )
                    );
                }

                taskCompletionSource.TrySetResult(_ret.ToList());
            }
            else
            {
                taskCompletionSource.TrySetResult(new List<Goal>());
            }

            return taskCompletionSource.Task;
        }

        private System.Threading.Tasks.Task<IEnumerable<GoalElement>> GetGoalElementByDisciplineKeyListOfflineAsync(
            List<int> DisciplineList, string ShortDescription, string LongDescription, bool RequiresOrdersOnly,
            bool StartsWith)
        {
            var taskCompletionSource = new System.Threading.Tasks.TaskCompletionSource<IEnumerable<GoalElement>>();
            var ret = GoalCache.GetGoalElementsFromSearch(ShortDescription, LongDescription, RequiresOrdersOnly,
                StartsWith);
            if (ret != null)
            {
                var _ret = (ret.Where(g => !g.Inactive &&
                                           (g.DisciplineInGoalElement.Any(dige =>
                                                DisciplineList.Contains(dige.DisciplineKey))
                                            || !g.DisciplineInGoalElement.Any()
                                           ))
                    ).ToList();
                taskCompletionSource.TrySetResult(_ret);
            }
            else
            {
                taskCompletionSource.TrySetResult(new List<GoalElement>());
            }

            return taskCompletionSource.Task;
        }

        #endregion

        #region ONLINE Single Discipline

        private System.Threading.Tasks.Task<IEnumerable<Goal>> GetDisciplineInGoalByDisciplineKeyOnlineAsync(
            int ServiceTypeKey, string ShortDescription, string LongDescription, bool StartsWith)
        {
            Context.Goals.Clear();

            var dscpkey = ServiceTypeCache.GetDisciplineKey(ServiceTypeKey);

            var query = Context.GetDisciplineInGoalByDisciplineKeyQuery(dscpkey.Value)
                .OrderBy(p => p.LongDescription);

            //if (string.IsNullOrEmpty(GoalSearchString))
            //{
            //    query = query.Where(i => i.LockDescription == false);
            //}
            //else
            //{
            //    string[] searchpieces = GoalSearchString.Split(' ');
            //    foreach (var searchpiece in searchpieces)
            //    {
            //        string piece = searchpiece;

            //        if (!string.IsNullOrEmpty(piece))
            //            query = query.Where(i => i.LockDescription == false && (i.ShortDescription.Contains(piece) || i.LongDescription.Contains(piece)));
            //    }
            //}

            string shortDesc = null;
            string longDesc = null;

            query = query.Where(g => !g.LockDescription && !g.Inactive);

            if (!string.IsNullOrEmpty(ShortDescription))
            {
                shortDesc = ShortDescription.ToLower();
                query = query.Where(g => (StartsWith
                                          && (g.ShortDescription.ToLower().Substring(0, shortDesc.Length) == shortDesc)
                                         )
                                         || (!StartsWith
                                             && g.ShortDescription.Contains(shortDesc)
                                         )
                );
            }


            if (!string.IsNullOrEmpty(LongDescription))
            {
                longDesc = LongDescription.ToLower();
                query = query.Where(g => (StartsWith
                                          && (g.LongDescription.ToLower().Substring(0, longDesc.Length) == longDesc)
                                         )
                                         || (!StartsWith
                                             && g.LongDescription.Contains(longDesc)
                                         )
                );
            }

            return
                DomainContextExtension.LoadAsync(Context,
                    query); //return Context.LoadAsync(query);  //IEnumerable<Goal>
        }

        private System.Threading.Tasks.Task<IEnumerable<GoalElement>>
            GetDisciplineInGoalElementByDisciplineKeyOnlineAsync(int ServiceTypeKey, string ShortDescription,
                string LongDescription, bool RequiresOrdersOnly, bool StartsWith)
        {
            Context.GoalElements.Clear();

            var dscpkey = ServiceTypeCache.GetDisciplineKey(ServiceTypeKey);

            var query = Context.GetDisciplineInGoalElementByDisciplineKeyQuery(dscpkey.Value)
                .Where(g => !g.Inactive)
                .OrderBy(p => p.LongDescription);

            if (!string.IsNullOrEmpty(ShortDescription))
            {
                string shortDesc = null;
                shortDesc = ShortDescription.ToLower();
                query = query.Where(g => (StartsWith
                                          && (g.ShortDescription.Substring(0, shortDesc.Length).ToLower() == shortDesc)
                                         )
                                         || (!StartsWith
                                             && g.ShortDescription.ToLower().Contains(shortDesc)
                                         )
                );
            }

            if (!string.IsNullOrEmpty(LongDescription))
            {
                string longDesc = null;
                longDesc = LongDescription.ToLower();
                query = query.Where(g => (StartsWith
                                          && (g.LongDescription.Substring(0, longDesc.Length).ToLower() == longDesc)
                                         )
                                         || (!StartsWith
                                             && g.LongDescription.ToLower().Contains(longDesc)
                                         )
                );
            }

            if (RequiresOrdersOnly)
            {
                query = query.Where(g => g.Orders == true);
            }

            return DomainContextExtension.LoadAsync(Context, query); //return Context.LoadAsync(query);  
        }

        #endregion

        #region ONLINE Multi Discipline

        private System.Threading.Tasks.Task<IEnumerable<Goal>> GetDisciplineInGoalByDisciplineKeyListOnlineAsync(
            List<int> DisciplineList, string ShortDescription, string LongDescription, bool StartsWith)
        {
            Context.Goals.Clear();
            var query = Context.GetDisciplineInGoalByDisciplineKeyListQuery(DisciplineList)
                .OrderBy(p => p.LongDescription);

            query = query.Where(g => !g.LockDescription && !g.Inactive);

            if (!string.IsNullOrEmpty(ShortDescription))
            {
                string shortDesc = ShortDescription.ToLower();
                query = query.Where(g => (StartsWith
                                          && g.ShortDescription.Substring(0, ShortDescription.Length).ToLower() ==
                                          shortDesc
                                         )
                                         || (!StartsWith
                                             && g.ShortDescription.ToLower().Contains(shortDesc)
                                         )
                );
            }

            if (!string.IsNullOrEmpty(LongDescription))
            {
                string longDesc = LongDescription.ToLower();
                query = query.Where(g => (StartsWith
                                          && g.LongDescription.Substring(0, LongDescription.Length).ToLower() ==
                                          longDesc
                                         )
                                         || (!StartsWith
                                             && g.LongDescription.ToLower().Contains(longDesc)
                                         )
                );
            }

            return DomainContextExtension.LoadAsync(Context, query); //return Context.LoadAsync(query); 
        }

        private System.Threading.Tasks.Task<IEnumerable<GoalElement>>
            GetDisciplineInGoalElementByDisciplineKeyListOnlineAsync(List<int> DisciplineList, string ShortDescription,
                string LongDescription, bool RequiresOrdersOnly, bool StartsWith)
        {
            Context.GoalElements.Clear();

            var query = Context.GetDisciplineInGoalElementByDisciplineKeyListQuery(DisciplineList)
                .Where(g => !g.Inactive)
                .OrderBy(p => p.LongDescription);

            if (!string.IsNullOrEmpty(ShortDescription))
            {
                string shortDesc = null;
                shortDesc = ShortDescription.ToLower();
                query = query.Where(g => (StartsWith
                                          && (g.ShortDescription.Substring(0, shortDesc.Length).ToLower() == shortDesc)
                                         )
                                         || (!StartsWith
                                             && g.ShortDescription.ToLower().Contains(shortDesc)
                                         )
                );
            }

            if (!string.IsNullOrEmpty(LongDescription))
            {
                string longDesc = null;
                longDesc = LongDescription.ToLower();
                query = query.Where(g => (StartsWith
                                          && (g.LongDescription.Substring(0, longDesc.Length).ToLower() == longDesc)
                                         )
                                         || (!StartsWith
                                             && g.LongDescription.ToLower().Contains(longDesc)
                                         )
                );
            }

            if (RequiresOrdersOnly)
            {
                query = query.Where(g => g.Orders == true);
            }

            return DomainContextExtension.LoadAsync(Context, query); //return Context.LoadAsync(query);  
        }

        #endregion


        int __cleanupCount;

        public void Cleanup()
        {
            ++__cleanupCount;
            if (__cleanupCount > 1)
            {
                return;
            }

            if (Context != null)
            {
                Context.EntityContainer.Clear();
            }

            Context = null;
        }
    }

    public class GoalManager : GenericBase, IGoalManagement
    {
        public static GoalManager Create(IDynamicFormService m, DynamicFormViewModel vm)
        {
            return new GoalManager(m, vm);
        }

        public IDynamicFormService Model { get; set; }
        public DynamicFormViewModel ViewModel { get; set; }
        public Admission CurrentAdmission { get; set; }
        public Encounter CurrentEncounter { get; set; }
        public GoalManagerService GoalManagerService = new GoalManagerService();

        private bool _canCancelGoalEdit = true;

        public bool CanCancelGoalEdit
        {
            get { return _canCancelGoalEdit; }
            set
            {
                _canCancelGoalEdit = value;
                this.RaisePropertyChangedLambda(p => p.CanCancelGoalEdit);
            }
        }

        private bool _showAllGoalElements = true;

        public bool ShowAllGoalElements
        {
            get { return _showAllGoalElements; }
            set
            {
                _showAllGoalElements = value;
                this.RaisePropertyChangedLambda(p => p.ShowAllGoalElements);
                this.RaisePropertyChangedLambda(p => p.ShowAddGoalElement);
                this.RaisePropertyChangedLambda(p => p.FilteredGoalElements);
            }
        }

        public bool ShowAddGoalElement
        {
            get
            {
                if (CurrentAdmissionGoal == null)
                {
                    return false;
                }

                return ShowAllGoalElements ? false : true;
            }
        }

        private void RaisePropertyChangedFilteredGoalElements()
        {
            this.RaisePropertyChangedLambda(p => p.ShowAllGoalElements);
            this.RaisePropertyChangedLambda(p => p.ShowAddGoalElement);
            this.RaisePropertyChangedLambda(p => p.FilteredGoalElements);
        }

        public List<AdmissionGoalElement> FilteredGoalElements
        {
            get
            {
                if (ShowAllGoalElements)
                {
                    return FilteredGoalElementsAll;
                }

                return FilteredGoalElementsCurrentGoal;
            }
        }

        private List<AdmissionGoalElement> FilteredGoalElementsAll
        {
            get
            {
                if ((FilteredGoals == null) || (FilteredGoals.View == null))
                {
                    return null;
                }

                List<AdmissionGoal> fgList = FilteredGoals.View.Cast<AdmissionGoal>().ToList();
                if ((fgList == null) || (fgList.Any() == false))
                {
                    return null;
                }

                List<AdmissionGoalElement> fgeList = new List<AdmissionGoalElement>();
                foreach (AdmissionGoal ag in fgList)
                {
                    if ((ag.FilteredGoalElements == null) || (ag.FilteredGoalElements.View == null))
                    {
                        return null;
                    }

                    List<AdmissionGoalElement> ageList =
                        ag.FilteredGoalElements.View.Cast<AdmissionGoalElement>().ToList();
                    if ((ageList == null) || (ageList.Any() == false))
                    {
                        continue;
                    }

                    foreach (AdmissionGoalElement age in ageList) fgeList.Add(age);
                }

                return ((fgeList == null) || (fgeList.Any() == false)) ? null : fgeList;
            }
        }

        public List<AdmissionGoalElement> FilteredGoalElementsCurrentGoal
        {
            get
            {
                if ((CurrentAdmissionGoal == null) || (CurrentAdmissionGoal.FilteredGoalElements == null) ||
                    (CurrentAdmissionGoal.FilteredGoalElements.View == null))
                {
                    return null;
                }

                List<AdmissionGoalElement> fgeList = CurrentAdmissionGoal.FilteredGoalElements.View
                    .Cast<AdmissionGoalElement>().ToList();
                return ((fgeList == null) || (fgeList.Any() == false)) ? null : fgeList;
            }
        }

        public CollectionViewSource FilteredGoals { get; set; }

        public bool HasFilteredGoals
        {
            get
            {
                if ((FilteredGoals == null) || (FilteredGoals.View == null))
                {
                    return false;
                }

                return FilteredGoals.View.Cast<AdmissionGoal>().Any();
            }
        }

        public CollectionViewSource PrintCollection { get; set; }
        public CollectionViewSource POCFilteredGoals { get; set; }
        public ICollectionView POCFilteredGoalsView => POCFilteredGoals.View;
        public CollectionViewSource POCFilteredGoalElements { get; set; }
        public ICollectionView POCFilteredGoalElementsView => POCFilteredGoalElements.View;
        public CollectionViewSource FilteredDischargeGoals { get; set; }

        public void CreatePOCOrdersForDiscPrintList()
        {
            if (CurrentAdmission == null)
            {
                return;
            }

            if (CurrentAdmission.AdmissionDisciplineFrequency == null)
            {
                return;
            }

            if (_pocOrdersForDiscPrintList == null)
            {
                _pocOrdersForDiscPrintList = new List<POCOrdersForDiscAndTreatment>();
            }

            _pocOrdersForDiscPrintList.Clear();

            // Add a row for each Discipline Order
            POCOrdersForDiscAndTreatment odt = null;
            Form cForm = null;
            if (CurrentEncounter != null && CurrentEncounter.FormKey > 0)
            {
                cForm = DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey);
            }

            var Cert = (CurrentEncounter == null || CurrentEncounter.EncounterPlanOfCare == null)
                ? null
                : CurrentEncounter.EncounterPlanOfCare.FirstOrDefault();
            foreach (var adf in CurrentAdmission.AdmissionDisciplineFrequency.Where(
                         a => CurrentEncounter != null
                             && (CurrentEncounter.EncounterDisciplineFrequency.Any(ed =>
                                 ed.DispFreqKey == a.DisciplineFrequencyKey && !ed.Inactive)) || a.IsNew))
            {
                // Don't show frequencies that don't apply to this cert period.
                if (cForm != null && Cert != null && cForm.IsPlanOfCare)
                {
                    if (adf.StartDate > Cert.CertificationThruDate || adf.EndDate < Cert.CertificationFromDate)
                    {
                        continue;
                    }
                }

                if (_pocOrdersForDiscPrintList.Any(p => p.DisciplineKey == adf.DisciplineKey))
                {
                    var item = _pocOrdersForDiscPrintList.FirstOrDefault(p => p.DisciplineKey == adf.DisciplineKey);
                    if (item != null)
                    {
                        item.DisciplineFrequencies.Add(adf);
                    }
                }
                else
                {
                    odt = new POCOrdersForDiscAndTreatment();
                    odt.DisciplineKey = adf.DisciplineKey;
                    odt.DisciplineCode = DisciplineCache.GetCodeFromKey(adf.DisciplineKey);
                    odt.DisciplineFrequencies = new List<AdmissionDisciplineFrequency>();
                    odt.StartDate = adf.StartDate;
                    odt.CurrentAdmission = CurrentAdmission;
                    _pocOrdersForDiscPrintList.Add(odt);
                    odt.AdmissionGoalElements = new List<AdmissionGoalElement>();
                    odt.DisciplineFrequencies.Add(adf);
                }
            }

            // Now add rows to the discipline order rows where the disciplines match.
            foreach (var ge in goalElementList.Where(ord => ord.GoalElementText.Length > 1))
            {
                foreach (var dt in POCOrdersForDiscPrintList)
                {
                    if (dt.DisciplineFrequencies == null)
                    {
                        continue;
                    }

                    if (CurrentEncounter != null && !CurrentEncounter.IsNew &&
                        CurrentEncounter.EncounterGoalElementDiscipline != null)
                    {
                        if (CurrentEncounter.EncounterGoalElementDiscipline.Where(dge =>
                                dge.AdmissionGoalElementKey == ge.AdmissionGoalElementKey).Any() == false ||
                            CurrentEncounter.EncounterGoalElementDiscipline.Any(dge =>
                                dge.DisciplineKey == dt.DisciplineKey
                                && ge.AdmissionGoalElementKey == dge.AdmissionGoalElementKey))
                        {
                            // Exclude GoalElements that have been resolved, discontinued, unattained or inactivated in a previous AdmissionCertification
                            if (((ge.ResolvedDate.HasValue == false ||
                                  (CurrentEncounter.EncounterPlanOfCare.FirstOrDefault() != null && ge.ResolvedDate >=
                                      CurrentEncounter.EncounterPlanOfCare.FirstOrDefault().CertificationFromDate))) &&
                                ((ge.DiscontinuedDate.HasValue == false ||
                                  (CurrentEncounter.EncounterPlanOfCare.FirstOrDefault() != null &&
                                   ge.DiscontinuedDate >= CurrentEncounter.EncounterPlanOfCare.FirstOrDefault()
                                       .CertificationFromDate))) &&
                                ((ge.UnattainableDate.HasValue == false ||
                                  (CurrentEncounter.EncounterPlanOfCare.FirstOrDefault() != null &&
                                   ge.UnattainableDate >= CurrentEncounter.EncounterPlanOfCare.FirstOrDefault()
                                       .CertificationFromDate))) &&
                                ((ge.InactivatedDate.HasValue == false ||
                                  (CurrentEncounter.EncounterPlanOfCare.FirstOrDefault() != null &&
                                   ge.InactivatedDate >= CurrentEncounter.EncounterPlanOfCare.FirstOrDefault()
                                       .CertificationFromDate))))
                            {
                                dt.AdmissionGoalElements.Add(ge);
                            }
                        }
                    }
                    else
                    {
                        // fall back logic
                        var cacheGoalElement = GoalCache.GetGoalElementByKey(ge.GoalElementKey);
                        if (cacheGoalElement == null)
                        {
                            continue;
                        }

                        if (dt.AdmissionGoalElements == null)
                        {
                            continue;
                        }

                        if (cacheGoalElement.DisciplineInGoalElement == null)
                        {
                            continue;
                        }

                        if (cacheGoalElement.DisciplineInGoalElement.Any() == false ||
                            cacheGoalElement.DisciplineInGoalElement.Any(dge => dge.DisciplineKey == dt.DisciplineKey))
                        {
                            // Exclude GoalElements that have been resolved or discontinued in a previous AdmissionCertification
                            if (((ge.ResolvedDate.HasValue == false ||
                                  (CurrentEncounter.EncounterPlanOfCare.FirstOrDefault() != null && ge.ResolvedDate >=
                                      CurrentEncounter.EncounterPlanOfCare.FirstOrDefault().CertificationFromDate))) &&
                                ((ge.DiscontinuedDate.HasValue == false ||
                                  (CurrentEncounter.EncounterPlanOfCare.FirstOrDefault() != null &&
                                   ge.DiscontinuedDate >= CurrentEncounter.EncounterPlanOfCare.FirstOrDefault()
                                       .CertificationFromDate))) &&
                                ((ge.UnattainableDate.HasValue == false ||
                                  (CurrentEncounter.EncounterPlanOfCare.FirstOrDefault() != null &&
                                   ge.UnattainableDate >= CurrentEncounter.EncounterPlanOfCare.FirstOrDefault()
                                       .CertificationFromDate))) &&
                                ((ge.InactivatedDate.HasValue == false ||
                                  (CurrentEncounter.EncounterPlanOfCare.FirstOrDefault() != null &&
                                   ge.InactivatedDate >= CurrentEncounter.EncounterPlanOfCare.FirstOrDefault()
                                       .CertificationFromDate))))
                            {
                                dt.AdmissionGoalElements.Add(ge);
                            }
                        }
                    }
                }
            }

            this.RaisePropertyChangedLambda(p => p.POCOrdersForDiscPrintList);
        }

        private List<POCOrdersForDiscAndTreatment> _pocOrdersForDiscPrintList;

        public List<POCOrdersForDiscAndTreatment> POCOrdersForDiscPrintList
        {
            get { return _pocOrdersForDiscPrintList; }
            set { _pocOrdersForDiscPrintList = value; }
        }

        private List<AdmissionGoalElement> goalElementList;

        private AdmissionGoal _CurrentAdmissionGoal;

        public AdmissionGoal CurrentAdmissionGoal
        {
            get { return _CurrentAdmissionGoal; }
            set
            {
                if (_CurrentAdmissionGoal == value)
                {
                    return;
                }

                _CurrentAdmissionGoal = value;
                this.RaisePropertyChangedLambda(p => p.CurrentAdmissionGoal);

                if (CurrentAdmissionGoal != null)
                {
                    CurrentAdmissionGoal.FilterGoalElements(this);
                    CurrentAdmissionGoal.RaisePropertyChanged();
                }

                RaisePropertyChangedFilteredGoalElements();
            }
        }

        private AdmissionGoalElement _CurrentAdmissionGoalElement;

        public AdmissionGoalElement CurrentAdmissionGoalElement
        {
            get { return _CurrentAdmissionGoalElement; }
            set
            {
                _CurrentAdmissionGoalElement = value;
                this.RaisePropertyChangedLambda(p => p.CurrentAdmissionGoalElement);
                if (CurrentAdmissionGoalElement != null)
                {
                    CurrentAdmissionGoalElement.RaisePropertyChanged();
                }
            }
        }

        public string LotsOfSpaces => new String(' ', 2048);

        private bool _VisitPlanChecked;

        public bool VisitPlanChecked
        {
            get { return _VisitPlanChecked; }
            set
            {
                _VisitPlanChecked = value;
                this.RaisePropertyChangedLambda(p => p.VisitPlanChecked);
                if (value)
                {
                    FilterAll();
                }
            }
        }

        private bool _InterdisciplinaryPlanChecked;

        public bool InterdisciplinaryPlanChecked
        {
            get { return _InterdisciplinaryPlanChecked; }
            set
            {
                _InterdisciplinaryPlanChecked = value;
                this.RaisePropertyChangedLambda(p => p.InterdisciplinaryPlanChecked);
                if (value)
                {
                    FilterAll();
                }
            }
        }

        private bool _DisciplinePlanChecked;

        public bool DisciplinePlanChecked
        {
            get { return _DisciplinePlanChecked; }
            set
            {
                _DisciplinePlanChecked = value;
                this.RaisePropertyChangedLambda(p => p.DisciplinePlanChecked);
                if (value)
                {
                    FilterAll();
                }
            }
        }

        private bool _PlanHistoryChecked;

        public bool PlanHistoryChecked
        {
            get { return _PlanHistoryChecked; }
            set
            {
                _PlanHistoryChecked = value;
                this.RaisePropertyChangedLambda(p => p.PlanHistoryChecked);
                if (value)
                {
                    FilterAll();
                }
            }
        }

        private bool _AsAppearOnPOCChecked;

        public bool AsAppearOnPOCChecked
        {
            get { return _AsAppearOnPOCChecked; }
            set
            {
                _AsAppearOnPOCChecked = value;
                this.RaisePropertyChangedLambda(p => p.AsAppearOnPOCChecked);
            }
        }

        private bool _ShowGoals = true;

        public bool ShowGoals
        {
            get { return _ShowGoals; }
            set
            {
                _ShowGoals = value;
                this.RaisePropertyChangedLambda(p => p.ShowGoals);
                this.RaisePropertyChangedLambda(p => p.ShowGoalsInvalid);
                this.RaisePropertyChangedLambda(p => p.ShowGoalsLabel);
                this.RaisePropertyChangedLambda(p => p.GoalListHeight);
                this.RaisePropertyChangedLambda(p => p.GoalElementListHeight);
                ShowAllGoalElements = true;
            }
        }

        public bool ShowGoalsInvalid
        {
            get
            {
                if (ShowGoals)
                {
                    return false;
                }

                if ((FilteredGoals == null) || (FilteredGoals.View == null))
                {
                    return false;
                }

                List<AdmissionGoal> gList = FilteredGoals.View.Cast<AdmissionGoal>().ToList();
                if ((gList == null) || (gList.Any() == false))
                {
                    return false;
                }

                bool any = gList.Any(g => g.HasValidationErrors);
                return any;
            }
        }

        public string ShowGoalsLabel => (ShowGoals) ? "Hide Goals" : "Show Goals";

        public GridLength GoalListHeight
        {
            get
            {
                GridLength height = new GridLength(((ShowGoals) ? 2 : 0), GridUnitType.Star);
                return height;
            }
        }

        public GridLength GoalElementListHeight
        {
            get
            {
                GridLength height = new GridLength(((ShowGoals) ? 3 : 5), GridUnitType.Star);
                return height;
            }
        }

        #region Search Properties

        ObservableCollection<Goal> _SearchGoals;

        public ObservableCollection<Goal> SearchGoals
        {
            get { return _SearchGoals; }
            set
            {
                if (_SearchGoals != value)
                {
                    _SearchGoals = value;
                    this.RaisePropertyChangedLambda(p => p.SearchGoals);
                }
            }
        }

        ObservableCollection<GoalElement> _SearchGoalElements;

        public ObservableCollection<GoalElement> SearchGoalElements
        {
            get { return _SearchGoalElements; }
            set
            {
                if (_SearchGoalElements != value)
                {
                    _SearchGoalElements = value;
                    this.RaisePropertyChangedLambda(p => p.SearchGoalElements);
                }
            }
        }

        private Goal _SelectedGoal;

        public Goal SelectedGoal
        {
            get { return _SelectedGoal; }
            set
            {
                if (_SelectedGoal != value)
                {
                    _SelectedGoal = value;

                    this.RaisePropertyChangedLambda(p => p.SelectedGoal);
                    AddGoalCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private GoalElement _SelectedGoalElement;

        public GoalElement SelectedGoalElement
        {
            get { return _SelectedGoalElement; }
            set
            {
                if (_SelectedGoalElement != value)
                {
                    _SelectedGoalElement = value;

                    this.RaisePropertyChangedLambda(p => p.SelectedGoalElement);
                    AddGoalElementCommand.RaiseCanExecuteChanged();
                }
            }
        }

        //private string _GoalSearchString;
        //public string GoalSearchString
        //{
        //    get { return _GoalSearchString; }
        //    set
        //    {
        //        if (_GoalSearchString != value)
        //        {
        //            _GoalSearchString = value;
        //            this.RaisePropertyChangedLambda(p => p.GoalSearchString);
        //            SearchGoalCommand.RaiseCanExecuteChanged();
        //        }
        //    }
        //}

        private string _ShortDescription;

        public string ShortDescription
        {
            get { return _ShortDescription; }
            set
            {
                if (_ShortDescription != value)
                {
                    _ShortDescription = value;
                    this.RaisePropertyChangedLambda(p => p.ShortDescription);
                    SearchGoalCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _LongDescription;

        public string LongDescription
        {
            get { return _LongDescription; }
            set
            {
                if (_LongDescription != value)
                {
                    _LongDescription = value;
                    this.RaisePropertyChangedLambda(p => p.LongDescription);
                    SearchGoalCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _SearchType = "Includes";

        public string SearchType
        {
            get { return _SearchType; }
            set
            {
                if (_SearchType != value)
                {
                    _SearchType = value;
                    this.RaisePropertyChangedLambda(p => p.SearchType);
                    SearchGoalCommand.RaiseCanExecuteChanged();
                }
            }
        }


        //private string _GoalElementSearchString;
        //public string GoalElementSearchString
        //{
        //    get { return _GoalElementSearchString; }
        //    set
        //    {
        //        if (_GoalElementSearchString != value)
        //        {
        //            _GoalElementSearchString = value;
        //            this.RaisePropertyChangedLambda(p => p.GoalElementSearchString);
        //            SearchGoalElementCommand.RaiseCanExecuteChanged();
        //        }
        //    }
        //}

        private string _ElementShortDescription;

        public string ElementShortDescription
        {
            get { return _ElementShortDescription; }
            set
            {
                if (_ElementShortDescription != value)
                {
                    _ElementShortDescription = value;
                    this.RaisePropertyChangedLambda(p => p.ElementShortDescription);
                    SearchGoalElementCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _ElementLongDescription;

        public string ElementLongDescription
        {
            get { return _ElementLongDescription; }
            set
            {
                if (_ElementLongDescription != value)
                {
                    _ElementLongDescription = value;
                    this.RaisePropertyChangedLambda(p => p.ElementLongDescription);
                    SearchGoalElementCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public event EventHandler PopupDataTemplateChanged;
        List<string> PopupDataTemplateStack = new List<string>();
        private string _PopupDataTemplate;

        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                if (value == null)
                {
                    if (PopupDataTemplateStack.Any())
                    {
                        _PopupDataTemplate = PopupDataTemplateStack.Last();
                        PopupDataTemplateStack.Remove(_PopupDataTemplate);
                    }
                    else
                    {
                        _PopupDataTemplate = null;
                    }
                }
                else
                {
                    if (PopupDataTemplateStack.Any() && (PopupDataTemplateStack.Contains(value)))
                    {
                        PopupDataTemplateStack.Remove(value); // backing up a level
                    }
                    else
                    {
                        if (_PopupDataTemplate != null)
                        {
                            PopupDataTemplateStack.Add(_PopupDataTemplate); // going down a level
                        }
                    }

                    _PopupDataTemplate = value;
                }

                if (PopupDataTemplateChanged != null)
                {
                    PopupDataTemplateChanged(this, EventArgs.Empty);
                }

                this.RaisePropertyChangedLambda(p => p.PopupDataTemplate);
            }
        }

        private bool _AddGoalVisible;

        public bool AddGoalVisible
        {
            get { return _AddGoalVisible; }
            set
            {
                if (_AddGoalVisible != value)
                {
                    _AddGoalVisible = value;
                    PopupDataTemplate = _AddGoalVisible ? "AddGoalPopupDataTemplate" : null;
                    this.RaisePropertyChangedLambda(p => p.AddGoalVisible);
                }
            }
        }

        private bool _AddGoalElementVisible;

        public bool AddGoalElementVisible
        {
            get { return _AddGoalElementVisible; }
            set
            {
                if (_AddGoalElementVisible != value)
                {
                    _AddGoalElementVisible = value;
                    PopupDataTemplate = _AddGoalElementVisible ? "AddGoalElementPopupDataTemplate" : null;
                    this.RaisePropertyChangedLambda(p => p.AddGoalElementVisible);
                }
            }
        }

        #endregion

        #region Search Commands

        public RelayCommand OpenGoalCommand { get; set; }

        public RelayCommand AddGoalCommand { get; set; }

        public RelayCommand CloseGoalCommand { get; set; }

        public RelayCommand SearchGoalCommand { get; protected set; }

        public RelayCommand ClearGoalCommand { get; protected set; }

        public RelayCommand AddGoalElementCommand { get; set; }

        public RelayCommand CloseGoalElementCommand { get; set; }

        public RelayCommand SearchGoalElementCommand { get; protected set; }

        public RelayCommand ClearGoalElementCommand { get; protected set; }
        public RelayCommand<AdmissionGoal> AddAdmissionGoalElementCommand { get; protected set; }
        public RelayCommand ShowGoalsCommand { get; protected set; }

        public RelayCommand<AdmissionGoal> EditGoalCommand { get; protected set; }
        public RelayCommand CancelEditGoalCommand { get; protected set; }
        public RelayCommand<AdmissionGoal> DeleteGoalCommand { get; protected set; }
        public RelayCommand OKEditGoalCommand { get; protected set; }
        public RelayCommand<AdmissionGoalElement> EditGoalElementCommand { get; protected set; }
        public RelayCommand CancelEditGoalElementCommand { get; protected set; }
        public RelayCommand<AdmissionGoalElement> DeleteGoalElementCommand { get; protected set; }
        public RelayCommand OKEditGoalElementCommand { get; protected set; }

        #endregion

        public GoalManager(IDynamicFormService m, DynamicFormViewModel vm)
        {
            Model = m;
            ViewModel = vm;

            CurrentAdmission = vm.CurrentAdmission;
            CurrentEncounter = vm.CurrentEncounter;

            SetupCommands();
            SetupFilterGoals();
            if (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntryOrIsAttempted)
            {
                InterdisciplinaryPlanChecked = true;
            }
            else if (CurrentEncounter.IsCompleted)
            {
                VisitPlanChecked = true;
            }
            else if (CurrentEncounter.FullValidation == false)
            {
                DisciplinePlanChecked = true;
            }
            else
            {
                InterdisciplinaryPlanChecked = true;
            }
        }

        public void FilterAll(bool loading = false, bool setupcommands = false)
        {
            foreach (var ag in CurrentAdmission.AdmissionGoal.Reverse())
            {
                ag.FilterGoalElements(this);

                if (setupcommands)
                {
                    SetupGoalCommands(ag);

                    foreach (var age in ag.AdmissionGoalElement)
                    {
                        var goalElement = GoalCache.GetGoalElementByKey(age.GoalElementKey);
                        if (goalElement != null)
                        {
                            age.POCProtected = goalElement.Orders;
                        }
                        else
                        {
                            age.POCProtected = false;
                        }

                        SetupGoalElementCommands(age);
                    }
                }
            }

            FilterGoals();
            SendGoalElementRefreshMessage();
            RaisePropertyChangedFilteredGoalElements();
        }

        public void SetupFilterGoals()
        {
            foreach (var ag in CurrentAdmission.AdmissionGoal.Reverse()) ag.FilterGoalElements(this);

            FilteredGoals = new CollectionViewSource();
            FilteredGoals.Source = CurrentAdmission.AdmissionGoal;
            FilteredGoals.Filter += (s, e) =>
            {
                bool wanttoaccept = true;

                AdmissionGoal ag = e.Item as AdmissionGoal;
                wanttoaccept = AcceptGoalForFilter(ag);

                e.Accepted = wanttoaccept;
            };
            this.RaisePropertyChangedLambda(p => p.FilteredGoals);
            this.RaisePropertyChangedLambda(p => p.HasFilteredGoals);

            PrintCollection = new CollectionViewSource();
            PrintCollection.Source = CurrentAdmission.AdmissionGoal;
            PrintCollection.Filter += (s, e) =>
            {
                bool wanttoaccept = true;

                AdmissionGoal ag = e.Item as AdmissionGoal;
                if (ag.CurrentEncounterGoal == null)
                {
                    ag.CurrentEncounterGoal = ag.EncounterGoal.FirstOrDefault(p => p.Encounter.EncounterKey == CurrentEncounter.EncounterKey);
                }

                wanttoaccept = AcceptGoalForFilter(ag, true);

                e.Accepted = wanttoaccept;
            };
            this.RaisePropertyChangedLambda(p => p.PrintCollection);

            POCFilteredGoals = new CollectionViewSource();
            POCFilteredGoals.Source = CurrentAdmission.AdmissionGoal;
            POCFilteredGoals.Filter += (s, e) =>
            {
                AdmissionGoal g = e.Item as AdmissionGoal;
                e.Accepted = (isAdmissionGoalinEncounter(g) && g.HasIncludeonPOCGoalElements);
            };
            this.RaisePropertyChangedLambda(p => p.POCFilteredGoals);

            FilteredDischargeGoals = new CollectionViewSource();
            FilteredDischargeGoals.Source = CurrentAdmission.AdmissionGoal;
            FilteredDischargeGoals.Filter += (s, e) =>
            {
                AdmissionGoal g = e.Item as AdmissionGoal;

                // No discharge goals for Hospice Admission
                if (g != null && g.Admission != null && g.Admission.HospiceAdmission)
                {
                    e.Accepted = false;
                    return;
                }

                if (isAdmissionGoalinEncounter(g) == false)
                {
                    e.Accepted = false;
                    return;
                }

                e.Accepted = g.RequiredForDischarge;
            };

            RefreshgoalElementList();

            this.RaisePropertyChangedLambda(p => p.FilteredDischargeGoals);
            this.RaisePropertyChangedLambda(p => p.POCFilteredGoalElements);
            CreatePOCOrdersForDiscPrintList();
        }

        public void FilterGoals()
        {
            if (FilteredGoals != null)
            {
                FilteredGoals.View.Refresh();
            }

            this.RaisePropertyChangedLambda(p => p.FilteredGoals);
            this.RaisePropertyChangedLambda(p => p.HasFilteredGoals);

            if (PrintCollection != null)
            {
                PrintCollection.View.Refresh();
            }

            this.RaisePropertyChangedLambda(p => p.PrintCollection);

            if (POCFilteredGoals.View != null)
            {
                POCFilteredGoals.View.Refresh();
            }

            this.RaisePropertyChangedLambda(p => p.POCFilteredGoals);

            FilterDischargeGoals();

            RefreshgoalElementList();

            this.RaisePropertyChangedLambda(p => p.FilteredDischargeGoals);
            this.RaisePropertyChangedLambda(p => p.POCFilteredGoalElements);
            CreatePOCOrdersForDiscPrintList();
        }

        public void FilterDischargeGoals()
        {
            if (FilteredDischargeGoals != null)
            {
                FilteredDischargeGoals.View.Refresh();
            }

            this.RaisePropertyChangedLambda(p => p.FilteredDischargeGoals);
            ValidateGoalsWarnings();
        }

        public bool AcceptGoalForFilter(AdmissionGoal ag, bool forceVisitPlan = false, bool forceInterDisc = false)
        {
            bool wanttoaccept = true;

            if (ag.CurrentEncounterGoal == null)
            {
                ag.CurrentEncounterGoal = ag.EncounterGoal.FirstOrDefault(p => p.Encounter.EncounterKey == CurrentEncounter.EncounterKey);
            }

            if (PlanHistoryChecked && !ag.Superceded)
            {
            }
            else if (ag.IsNew)
            {
            }
            else if (CurrentEncounter.EncounterGoal.All(eg => eg.AdmissionGoalKey != ag.AdmissionGoalKey))
            {
                return false;
            }
            else if (!CurrentEncounter.FullValidation && !CurrentEncounter.EncounterIsEval &&
                     ((VisitPlanChecked || forceVisitPlan) && (!forceInterDisc)))
            {
                wanttoaccept = GoalIsVisibleInVisitView(ag);
            }
            else if (ag != null && ag.CurrentEncounterGoal == null)
            {
                wanttoaccept = false;
            }
            else if (DisciplinePlanChecked && (!forceInterDisc))
            {
                // filter out goals for other disciplines (keep our discipline's goals and non-discipline specifi goals
                if (CurrentEncounter.ServiceTypeKey != null)
                {
                    int? disciplineKey = ServiceTypeCache.GetDisciplineKey(CurrentEncounter.ServiceTypeKey.Value);
                    if (disciplineKey != null)
                    {
                        List<DisciplineInGoal> digList = GoalCache.GetGoalDisciplinesFromGoalKey(ag.GoalKey);
                        if (digList != null)
                        {
                            DisciplineInGoal dig = digList.FirstOrDefault(d => d.DisciplineKey == disciplineKey);
                            if (dig == null)
                            {
                                wanttoaccept = false;
                            }
                        }
                    }
                }
            }

            return wanttoaccept;
        }

        public bool ShowShortLongTerm(AdmissionGoal g)
        {
            if (g == null)
            {
                return false;
            }

            // if already collected - show it
            if ((g.ShortTermGoal == true) || (g.LongTermGoal == true))
            {
                return true;
            }

            if (g.Admission == null)
            {
                return false;
            }

            if (g.Admission.HospiceAdmission)
            {
                return false;
            }

            if (CurrentEncounter == null)
            {
                return false;
            }

            if (CurrentEncounter.ServiceType == null)
            {
                return false;
            }

            if (CurrentEncounter.ServiceType.Discipline == null)
            {
                return false;
            }

            // Only show checkbox on POC and Therapy evals, visits and resumptions
            if (IsPlanOfCareOrTherapyEvalVisitResumption == false)
            {
                return false;
            }

            // Only valid to show on therapy goals 
            bool show = GoalCache.IsTherapyGoal(g.GoalKey);
            return show;
        }

        public bool GoalIsVisibleInVisitView(AdmissionGoal ag)
        {
            bool wanttoaccept = false;
            var previous = CurrentAdmission.Encounter
                .Where(e => e.EncounterStatus !=
                            (int)EncounterStatusType
                                .None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                .Where(p => !p.IsNew && p.EncounterKey != CurrentEncounter.EncounterKey &&
                            p.EncounterOrTaskStartDateAndTime <= CurrentEncounter.EncounterOrTaskStartDateAndTime &&
                            CurrentEncounter.DisciplineKey == p.DisciplineKey &&
                            !p.IsPlanOfCareOrIsTeamMeetingOrIsOrderEntryOrIsAttempted) //using GetValueOrDefault() because we now have Encounters with no FormKey - E.G. Encounter Type Documentation added in ADMIN Maint.
                .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime)
                .FirstOrDefault();
            if (previous == null)
            {
                wanttoaccept = false;
            }
            else
            {
                //pull in by default if any goal elements were planned for the next encounter
                wanttoaccept = previous.EncounterGoalElement.Where(p =>
                    ((p.AdmissionGoalElement != null &&
                      p.AdmissionGoalElement.AdmissionGoalKey == ag.AdmissionGoalKey &&
                      p.Planned)
                     || (p.Planned && p.AdmissionGoalElement != null && p.AdmissionGoalElement.AdmissionGoal != null &&
                         p.AdmissionGoalElement.AdmissionGoal.HistoryKey == ag.HistoryKey && ag.HistoryKey != null))
                    && CurrentEncounter.EncounterGoal.Any(eg => eg.AdmissionGoalKey == ag.AdmissionGoalKey)).Any();
                // anything with changes during this encounter should also appear on the visit plan, regardless of what tab the change originated from.{
                wanttoaccept = ag.HasChanges || wanttoaccept ||
                               (ag.AddedFromEncounterKey == CurrentEncounter.EncounterKey && ag.HistoryKey == null)
                               || ((ag.AdmissionGoalElement != null) && (ag.AdmissionGoalElement.Any(a =>
                                   a.HasChanges || ag.IsGoalElementVisibleInVisitMode(a))));
            }

            return wanttoaccept;
        }

        private void RefreshgoalElementList()
        {
            POCFilteredGoalElements = null;
            goalElementList = null;

            POCFilteredGoalElements = new CollectionViewSource();
            goalElementList = new List<AdmissionGoalElement>();
            foreach (AdmissionGoal g in CurrentAdmission.AdmissionGoal)
                if (isAdmissionGoalinEncounter(g))
                {
                    // Do all filtering here - up front - because we need to deal with duplicate elements ( same element in multiple goals)
                    foreach (AdmissionGoalElement e in g.AdmissionGoalElement)
                    {
                        //if (e.Superceded) continue;
                        if (!isAdmissionGoalElementinEncounter(e))
                        {
                            continue;
                        }

                        if (e.IncludeonPOC == false)
                        {
                            continue;
                        }

                        if (e.EntityState == EntityState.Deleted || e.EntityState == EntityState.Detached)
                        {
                            continue;
                        }

                        if (e.ResolvedDate != null)
                        {
                            if ((DateTime)e.ResolvedDate < GoalAndElementDateToUse)
                            {
                                continue;
                            }
                        }

                        if (e.DiscontinuedDate != null)
                        {
                            if ((DateTime)e.DiscontinuedDate < GoalAndElementDateToUse)
                            {
                                continue;
                            }
                        }

                        if (e.UnattainableDate != null)
                        {
                            if ((DateTime)e.UnattainableDate < GoalAndElementDateToUse)
                            {
                                continue;
                            }
                        }

                        if (e.InactivatedDate != null)
                        {
                            if ((DateTime)e.InactivatedDate < GoalAndElementDateToUse)
                            {
                                continue;
                            }
                        }

                        if (!goalElementList.Contains(e))
                        {
                            goalElementList.Add(e);
                        }
                    }
                }

            POCFilteredGoalElements.Source = goalElementList;
            POCFilteredGoalElements.Filter += (s, e) => { e.Accepted = true; };
        }

        private DateTime GoalAndElementDateToUse
        {
            get
            {
                var dateToUse = EncounterDate();
                if (IsPlanOfCare)
                {
                    var Cert = (CurrentEncounter == null || CurrentEncounter.EncounterPlanOfCare == null)
                        ? null
                        : CurrentEncounter.EncounterPlanOfCare.FirstOrDefault();
                    if (Cert != null && Cert.CertificationFromDate != null)
                    {
                        dateToUse = (DateTime)Cert.CertificationFromDate;
                    }
                }

                return dateToUse;
            }
        }

        private DateTime EncounterDate()
        {
            DateTime encounterDate = DateTime.Today;
            if (CurrentEncounter != null)
            {
                if ((CurrentEncounter.EncounterIsOrderEntry) && (CurrentEncounter.CurrentOrderEntry != null) &&
                    (CurrentEncounter.CurrentOrderEntry.CompletedDate != null))
                {
                    encounterDate = CurrentEncounter.CurrentOrderEntry.CompletedDate.Value.Date;
                }
                else if (CurrentEncounter.EncounterStartDate.HasValue &&
                         CurrentEncounter.EncounterStartDate.Value != DateTime.MinValue)
                {
                    encounterDate = CurrentEncounter.EncounterStartDate.Value.Date;
                }
            }

            return encounterDate;
        }

        private bool isAdmissionGoalinEncounter(AdmissionGoal g)
        {
            if (g == null)
            {
                return false;
            }

            //if (g.Superceded) return false;
            // If we have an Encounter, only include the AdmissionGoal if it is in this encounter
            if (g.IsNew)
            {
                return true;
            }

            if ((CurrentEncounter != null) && (CurrentEncounter.EncounterGoal != null))
            {
                EncounterGoal eg = CurrentEncounter.EncounterGoal.Where(p => p.AdmissionGoalKey == g.AdmissionGoalKey)
                    .FirstOrDefault();
                if (eg == null)
                {
                    return false;
                }
            }

            if (g.ResolvedDate != null)
            {
                if ((DateTime)g.ResolvedDate < GoalAndElementDateToUse)
                {
                    return false; // If this Goal has been resolved in a prior cert period do not display it
                }
            }

            if (g.DiscontinuedDate != null)
            {
                if ((DateTime)g.DiscontinuedDate < GoalAndElementDateToUse)
                {
                    return false; // If this Goal has been discontinued in a prior cert period do not display it
                }
            }

            if (g.UnattainableDate != null)
            {
                if ((DateTime)g.UnattainableDate < GoalAndElementDateToUse)
                {
                    return false;
                }
            }

            if (g.InactivatedDate != null)
            {
                if ((DateTime)g.InactivatedDate < GoalAndElementDateToUse)
                {
                    return false;
                }
            }

            return true;
        }

        private bool isAdmissionGoalElementinEncounter(AdmissionGoalElement g)
        {
            if (g == null)
            {
                return false;
            }

            // If we have an Encounter, only include the AdmissionGoal if it is in this encounter
            if (g.IsNew)
            {
                return true;
            }

            if ((CurrentEncounter != null) && (CurrentEncounter.EncounterGoalElement != null))
            {
                EncounterGoalElement eg = CurrentEncounter.EncounterGoalElement.FirstOrDefault(p => p.AdmissionGoalElementKey == g.AdmissionGoalElementKey);
                if (eg == null)
                {
                    return false;
                }
            }

            if (g.ResolvedDate != null)
            {
                if ((DateTime)g.ResolvedDate < GoalAndElementDateToUse)
                {
                    return false;
                }
            }

            if (g.DiscontinuedDate != null)
            {
                if ((DateTime)g.DiscontinuedDate < GoalAndElementDateToUse)
                {
                    return false;
                }
            }

            if (g.UnattainableDate != null)
            {
                if ((DateTime)g.UnattainableDate < GoalAndElementDateToUse)
                {
                    return false;
                }
            }

            if (g.InactivatedDate != null)
            {
                if ((DateTime)g.InactivatedDate < GoalAndElementDateToUse)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsOrderEntryOrOrderEntryVO
        {
            get
            {
                if (IsOrderEntry)
                {
                    return true;
                }

                if ((ViewModel == null) || (ViewModel.CurrentOrderEntryManager == null))
                {
                    return false;
                }

                return ViewModel.CurrentOrderEntryManager.IsVO;
            }
        }

        public bool IsOrderEntry => (CurrentEncounter != null && CurrentEncounter.FormKey != null &&
                                     DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey).IsOrderEntry);

        public bool IsAttempted => (CurrentEncounter != null && CurrentEncounter.FormKey != null &&
                                    DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey).IsAttempted);

        public bool IsPlanOfCare => (CurrentEncounter != null && CurrentEncounter.FormKey != null &&
                                     DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey).IsPlanOfCare);

        public bool IsTeamMeeting => (CurrentEncounter != null && CurrentEncounter.FormKey != null &&
                                      DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey).IsTeamMeeting);

        public bool IsTherapyEvalVisitResumption
        {
            get
            {
                if (CurrentEncounter == null)
                {
                    return false;
                }

                if (CurrentEncounter.ServiceType == null)
                {
                    return false;
                }

                if (CurrentEncounter.ServiceType.Discipline == null)
                {
                    return false;
                }

                if (CurrentEncounter.ServiceType.Discipline.IsTherapyDiscipline == false)
                {
                    return false;
                }

                Form f = DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey);
                if (f == null)
                {
                    return false;
                }

                if ((f.IsEval == false) && (f.IsVisit == false) && (f.IsResumption == false))
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsPlanOfCareOrTherapyEvalVisitResumption => (IsPlanOfCare || IsTherapyEvalVisitResumption);

        public bool IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry => (IsPlanOfCare || IsTeamMeeting || IsOrderEntry);

        public bool IsPlanOfCareOrIsTeamMeetingOrIsOrderEntryOrIsAttempted =>
            (IsPlanOfCare || IsTeamMeeting || IsOrderEntry || IsAttempted);

        public bool ShowResolvedColumn
        {
            get
            {
                if (IsPlanOfCare)
                {
                    return false;
                }

                if (CurrentEncounterIsAssistant)
                {
                    return false;
                }

                return true;
            }
        }

        private bool IsPlanOfCareOrIsTeamMeeting => (IsPlanOfCare || IsTeamMeeting);

        private bool CurrentEncounterIsAssistant => CurrentEncounter?.IsAssistant ?? true;

        public bool IsAssistant
        {
            get
            {
                if (CurrentEncounter == null)
                {
                    return true;
                }

                return DisciplineCache.GetIsAssistantFromKey(ServiceTypeCache
                    .GetDisciplineKey(CurrentEncounter.ServiceTypeKey.Value).Value);
            }
        }

        public void SetupCommands()
        {
            OpenGoalCommand = new RelayCommand(() => { AddGoalVisible = true; });

            AddGoalCommand = new RelayCommand(() =>
            {
                AdmissionGoal ag = new AdmissionGoal
                {
                    GoalKey = SelectedGoal.GoalKey,
                    GoalText = SelectedGoal.LongDescription,
                    QuestionLabel = SelectedGoal.CodeValue,
                    AddedBy = WebContext.Current.User.MemberID,
                    AddedDate = DateTime.Now,
                    UpdatedBy = CurrentEncounter.UpdatedBy,
                    UpdatedDate = DateTime.Now,
                };
                if ((CurrentEncounter != null && (CurrentEncounter.EncounterKey > 0)))
                {
                    ag.AddedFromEncounterKey = CurrentEncounter.EncounterKey;
                }

                if (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntryOrIsAttempted ||
                    UserCache.Current.GetCurrentUserProfile().UserIsASupervisor)
                {
                    List<int> discList = GetDisciplineListForGoalSearch();
                    if (discList == null)
                    {
                        discList = new List<int>();
                    }

                    foreach (var ge in SelectedGoal.GoalElementInGoal
                                 .Where(p => p.GoalElement.DisciplineInGoalElement.Any(d => d.DisciplineInGoalElementInList(discList)) 
                                             || p.GoalElement.DisciplineInGoalElement.Any() == false))
                    {
                        AdmissionGoalElement age = new AdmissionGoalElement
                        {
                            GoalElementKey = ge.GoalElementKey,
                            GoalElementText = ge.GoalElement.LongDescription,
                            POCOverrideCode = ge.GoalElement.POCOverrideCode,
                            IncludeonPOC = CurrentAdmission.HospiceAdmission || ge.GoalElement.Orders,
                            POCProtected = ge.GoalElement.Orders,
                            AddedBy = WebContext.Current.User.MemberID,
                            AddedDate = DateTime.Now,
                            UpdatedBy = CurrentEncounter.UpdatedBy,
                            UpdatedDate = DateTime.Now,
                        };
                        if ((CurrentEncounter != null && (CurrentEncounter.EncounterKey > 0)))
                        {
                            age.AddedFromEncounterKey = CurrentEncounter.EncounterKey;
                        }

                        ag.AdmissionGoalElement.Add(age);
                        CurrentEncounter.AdmissionGoalElement.Add(age);

                        EncounterGoalElement ege = new EncounterGoalElement();
                        age.EncounterGoalElement.Add(ege);
                        CurrentEncounter.EncounterGoalElement.Add(ege);
                        ege.PopuulateEncounterGoalElementDisciplines(age.GoalElementDisciplineKeys);
                        age.CurrentEncounterGoalElement = ege;

                        SetupGoalElementCommands(age);
                    }
                }
                else
                {
                    var dscpkey = ServiceTypeCache.GetDisciplineKey(CurrentEncounter.ServiceTypeKey.Value);

                    foreach (var ge in SelectedGoal.GoalElementInGoal.Where(p =>
                                 p.GoalElement.DisciplineInGoalElement.Any(d => d.DisciplineKey == dscpkey) ||
                                 p.GoalElement.DisciplineInGoalElement.Any() == false))
                    {
                        AdmissionGoalElement age = new AdmissionGoalElement
                        {
                            GoalElementKey = ge.GoalElementKey,
                            GoalElementText = IsPlanOfCare
                                ? ge.GoalElement.POCOvrTextOrLongDescription
                                : ge.GoalElement.LongDescription,
                            POCOverrideCode = ge.GoalElement.POCOverrideCode,
                            IncludeonPOC = CurrentAdmission.HospiceAdmission || ge.GoalElement.Orders,
                            POCProtected = ge.GoalElement.Orders,
                            AddedBy = WebContext.Current.User.MemberID,
                            AddedDate = DateTime.Now,
                            UpdatedBy = CurrentEncounter.UpdatedBy,
                            UpdatedDate = DateTime.Now,
                        };
                        if ((CurrentEncounter != null && (CurrentEncounter.EncounterKey > 0)))
                        {
                            age.AddedFromEncounterKey = CurrentEncounter.EncounterKey;
                        }

                        ag.AdmissionGoalElement.Add(age);
                        CurrentEncounter.AdmissionGoalElement.Add(age);

                        EncounterGoalElement ege = new EncounterGoalElement();
                        age.EncounterGoalElement.Add(ege);
                        CurrentEncounter.EncounterGoalElement.Add(ege);
                        ege.PopuulateEncounterGoalElementDisciplines(age.GoalElementDisciplineKeys);
                        age.CurrentEncounterGoalElement = ege;

                        SetupGoalElementCommands(age);
                    }
                }

                EncounterGoal eg = new EncounterGoal();
                ag.EncounterGoal.Add(eg);
                CurrentEncounter.EncounterGoal.Add(eg);

                SetupGoalCommands(ag);

                CurrentAdmission.AdmissionGoal.Add(ag);
                CurrentEncounter.AdmissionGoal.Add(ag);
                ag.FilterGoalElements(this);
                SendGoalElementRefreshMessage();
                if ((ViewModel != null) && (ViewModel.CurrentOrderEntryManager != null))
                {
                    ViewModel.CurrentOrderEntryManager.UpdateGeneratedOrderText();
                }

                ValidateGoal(ag, IsAssistant, false);
                ag.RaisePropertyChangedAfterEdit();
                foreach (AdmissionGoalElement age in ag.AdmissionGoalElement)
                {
                    ValidateGoalElement(age.AdmissionGoal, age, IsAssistant);
                    age.RaisePropertyChangedAfterEdit();
                }

                this.RaisePropertyChangedLambda(p => p.HasFilteredGoals);
                ag.RaisePropertyChangedFilteredGoalElements();
                RaisePropertyChangedFilteredGoalElements();
            }, () => SelectedGoal != null);

            CloseGoalCommand = new RelayCommand(() =>
            {
                ClearGoalCommand.Execute(null);
                ValidateGoalsWarnings();
                AddGoalVisible = false;
            });

            SearchGoalCommand = new RelayCommand(() =>
            {
                if (CurrentAdmission != null && (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry ||
                                                 UserCache.Current.GetCurrentUserProfile().UserIsASupervisor))
                {
                    List<Int32> discList = GetDisciplineListForGoalSearch();
                    GoalManagerService.GetDisciplineInGoalByDisciplineKeyListAsync(discList, ShortDescription,
                            LongDescription, (SearchType == "StartsWith"))
                        .ContinueWith(t => { OnGoalsLoaded(t.Result); },
                            System.Threading.CancellationToken.None,
                            System.Threading.Tasks.TaskContinuationOptions.None,
                            Client.Utils.AsyncUtility.TaskScheduler);
                }
                else
                {
                    GoalManagerService.GetDisciplineInGoalByDisciplineKeyAsync(CurrentEncounter.ServiceTypeKey.Value,
                            ShortDescription, LongDescription, (SearchType == "StartsWith"))
                        .ContinueWith(t => { OnGoalsLoaded(t.Result); },
                            System.Threading.CancellationToken.None,
                            System.Threading.Tasks.TaskContinuationOptions.None,
                            Client.Utils.AsyncUtility.TaskScheduler);
                }
            });

            ClearGoalCommand = new RelayCommand(() =>
            {
                ShortDescription = string.Empty;
                LongDescription = string.Empty;
                SearchType = "Includes";
                if ((SearchGoals != null) && SearchGoals.Any())
                {
                    SearchGoals.Clear();
                }
            }, () => SearchGoals != null);

            AddGoalElementCommand = new RelayCommand(() => { AddGoalElement_Command(SelectedGoalElement); },
                () => SelectedGoalElement != null);

            CloseGoalElementCommand = new RelayCommand(() =>
            {
                ClearGoalElementCommand.Execute(null);
                AddGoalElementVisible = false;
            });

            SearchGoalElementCommand = new RelayCommand(() =>
            {
                bool RequiresOrdersOnly = IsOrderEntry;
                if (CurrentAdmission != null && (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry ||
                                                 UserCache.Current.GetCurrentUserProfile().UserIsASupervisor))
                {
                    List<Int32> discList = GetDisciplineListForGoalSearch();
                    GoalManagerService.GetDisciplineInGoalElementByDisciplineKeyListAsync(discList,
                            ElementShortDescription, ElementLongDescription, RequiresOrdersOnly,
                            (SearchType == "StartsWith"))
                        .ContinueWith(t => { OnGoalElementsLoaded(t.Result); },
                            System.Threading.CancellationToken.None,
                            System.Threading.Tasks.TaskContinuationOptions.None,
                            Client.Utils.AsyncUtility.TaskScheduler);
                }
                else
                {
                    GoalManagerService.GetDisciplineInGoalElementByDisciplineKeyAsync(
                            CurrentEncounter.ServiceTypeKey.Value, ElementShortDescription, ElementLongDescription,
                            RequiresOrdersOnly, (SearchType == "StartsWith"))
                        .ContinueWith(t => { OnGoalElementsLoaded(t.Result); },
                            System.Threading.CancellationToken.None,
                            System.Threading.Tasks.TaskContinuationOptions.None,
                            Client.Utils.AsyncUtility.TaskScheduler);
                }
            });

            ClearGoalElementCommand = new RelayCommand(() =>
            {
                ElementShortDescription = string.Empty;
                ElementLongDescription = string.Empty;
                SearchType = "Includes";
                if ((SearchGoalElements != null) && SearchGoalElements.Any())
                {
                    SearchGoalElements.Clear();
                }
            }, () => SearchGoalElements != null);

            AddAdmissionGoalElementCommand = new RelayCommand<AdmissionGoal>(AddAdmissionGoalElement_Command);
            ShowGoalsCommand = new RelayCommand(ShowGoals_Command);
            EditGoalCommand = new RelayCommand<AdmissionGoal>(EditGoal_Command);
            CancelEditGoalCommand = new RelayCommand(CancelEditGoal_Command);
            DeleteGoalCommand = new RelayCommand<AdmissionGoal>(DeleteGoal_Command);
            OKEditGoalCommand = new RelayCommand(async () => { await OKEditGoal_Command(); });
            EditGoalElementCommand = new RelayCommand<AdmissionGoalElement>(EditGoalElement_Command);
            CancelEditGoalElementCommand = new RelayCommand(CancelEditGoalElement_Command);
            DeleteGoalElementCommand = new RelayCommand<AdmissionGoalElement>(DeleteGoalElement_Command);
            OKEditGoalElementCommand = new RelayCommand(async () => { await OKEditGoalElement_Command(); });
        }

        private void AddGoalElement_Command(GoalElement newGoalElement)
        {
            SelectedGoalElement = newGoalElement;
            if (SelectedGoalElement == null)
            {
                return;
            }

            AdmissionGoalElement age = new AdmissionGoalElement
            {
                GoalElementKey = SelectedGoalElement.GoalElementKey,
                GoalElementText = IsPlanOfCare
                    ? SelectedGoalElement.POCOvrTextOrLongDescription
                    : SelectedGoalElement.LongDescription,
                POCOverrideCode = SelectedGoalElement.POCOverrideCode,
                IncludeonPOC = CurrentAdmission.HospiceAdmission || SelectedGoalElement.Orders,
                POCProtected = SelectedGoalElement.Orders,
                AddedBy = WebContext.Current.User.MemberID,
                AddedDate = DateTime.Now,
                UpdatedBy = CurrentEncounter.UpdatedBy,
                UpdatedDate = DateTime.Now,
            };
            if ((CurrentEncounter != null && (CurrentEncounter.EncounterKey > 0)))
            {
                age.AddedFromEncounterKey = CurrentEncounter.EncounterKey;
            }

            CurrentAdmissionGoal.AdmissionGoalElement.Add(age);
            CurrentEncounter.AdmissionGoalElement.Add(age);

            EncounterGoalElement ege = new EncounterGoalElement();
            age.EncounterGoalElement.Add(ege);
            CurrentEncounter.EncounterGoalElement.Add(ege);
            ege.PopuulateEncounterGoalElementDisciplines(age.GoalElementDisciplineKeys);
            age.CurrentEncounterGoalElement = ege;

            RefreshgoalElementList();
            CurrentAdmissionGoal.FilterGoalElements(this);
            if (POCFilteredGoalElementsView != null)
            {
                POCFilteredGoalElementsView.Refresh();
            }

            if (POCFilteredGoalsView != null)
            {
                POCFilteredGoalsView.Refresh();
            }

            Messenger.Default.Send(true, "RefreshGoalElements");

            SetupGoalElementCommands(age);

            if ((ViewModel != null) && (ViewModel.CurrentOrderEntryManager != null))
            {
                ViewModel.CurrentOrderEntryManager.UpdateGeneratedOrderText();
            }

            ValidateGoalElement(age.AdmissionGoal, age, IsAssistant);
            age.RaisePropertyChangedAfterEdit();
            ValidateGoal(CurrentAdmissionGoal, IsAssistant, false);
            CurrentAdmissionGoal.RaisePropertyChangedAfterEdit();

            RaisePropertyChanged("FilteredGoalElementsCurrentGoal");
            CurrentAdmissionGoal.RaisePropertyChangedFilteredGoalElements();
            RaisePropertyChangedFilteredGoalElements();
            CanCancelGoalEdit = false;
        }

        private void ShowGoals_Command()
        {
            ShowGoals = (!ShowGoals);
        }

        private void AddAdmissionGoalElement_Command(AdmissionGoal ag)
        {
            if (PlanHistoryChecked)
            {
                return;
            }

            if (ag == null)
            {
                return;
            }

            CurrentAdmissionGoal = ag;
            AddGoalElementVisible = true;
        }

        private void EditGoal_Command(AdmissionGoal ag)
        {
            if (PlanHistoryChecked)
            {
                return;
            }

            if (ag == null)
            {
                return;
            }

            CanCancelGoalEdit = true;
            CurrentAdmissionGoal = ag;
            CurrentAdmissionGoal.BeginEditting();
            CurrentAdmissionGoal.PreviousUnattainable = CurrentAdmissionGoal.Unattainable;
            CurrentAdmissionGoal.PreviousInactivated = CurrentAdmissionGoal.Inactivated;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                RaisePropertyChanged("FilteredGoalElementsCurrentGoal");
            });
            Deployment.Current.Dispatcher.BeginInvoke(() => { PopupDataTemplate = "EditGoalPopupDataTemplate"; });
        }

        private void CancelEditGoal_Command()
        {
            if (CurrentAdmissionGoal != null)
            {
                CurrentAdmissionGoal.CancelEditting();
            }

            PopupDataTemplate = null;
        }

        private void DeleteGoal_Command(AdmissionGoal ag)
        {
            CurrentAdmissionGoal = ag;
            if (CurrentAdmissionGoal == null)
            {
                return;
            }

            NavigateCloseDialog d = new NavigateCloseDialog
            {
                Width = double.NaN,
                Height = double.NaN,
                ErrorMessage = "Goal: " + CurrentAdmissionGoal.GoalDescription,
                ErrorQuestion = "Delete this Goal?",
                Title = "Delete Confirmation",
                HasCloseButton = false
            };

            d.Closed += OnDeleteGoalClosed;
            d.Show();
        }

        private void OnDeleteGoalClosed(object s, EventArgs err)
        {
            var dialog = (ChildWindow)s;
            dialog.Closed -= OnDeleteGoalClosed;

            if (dialog.DialogResult == false)
            {
                return;
            }

            try
            {
                CurrentAdmissionGoal.CancelEditting();
                RemoveGoal_Command(CurrentAdmissionGoal);
            }
            catch
            {
            }

            if (PopupDataTemplate == "EditGoalPopupDataTemplate")
            {
                PopupDataTemplate = null;
            }
        }

        private async System.Threading.Tasks.Task OKEditGoal_Command()
        {
            if (CurrentAdmissionGoal == null)
            {
                return;
            }

            bool goalValid = ValidateGoal(CurrentAdmissionGoal, IsAssistant, false);
            CurrentAdmissionGoal.RaisePropertyChangedAfterEdit();
            if (goalValid == false)
            {
                return;
            }

            CurrentAdmissionGoal.EndEditting();
            //PropagateGoalStatus(CurrentAdmissionGoal);
            PopupDataTemplate = null;
            if (ViewModel != null)
            {
                await ViewModel.AutoSave_Command("GoalManagerEditGoal");
            }
        }

        private void EditGoalElement_Command(AdmissionGoalElement age)
        {
            if (PlanHistoryChecked)
            {
                return;
            }

            if (age == null)
            {
                return;
            }

            CurrentAdmissionGoalElement = age;
            CurrentAdmissionGoalElement.BeginEditting();
            CurrentAdmissionGoalElement.PreviousUnattainable = CurrentAdmissionGoalElement.Unattainable;
            CurrentAdmissionGoalElement.PreviousInactivated = CurrentAdmissionGoalElement.Inactivated;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                PopupDataTemplate = "EditGoalElementPopupDataTemplate";
            });
        }

        private void CancelEditGoalElement_Command()
        {
            if (CurrentAdmissionGoalElement != null)
            {
                CurrentAdmissionGoalElement.CancelEditting();
            }

            PopupDataTemplate = null;
        }

        private void DeleteGoalElement_Command(AdmissionGoalElement age)
        {
            CurrentAdmissionGoalElement = age;
            if (CurrentAdmissionGoalElement == null)
            {
                return;
            }

            NavigateCloseDialog d = new NavigateCloseDialog();
            if (d == null)
            {
                return;
            }

            d.Width = double.NaN;
            d.Height = double.NaN;
            d.ErrorMessage = "Goal Element: " + CurrentAdmissionGoalElement.GoalElementDescription;
            d.ErrorQuestion = "Delete this Goal Element?";
            d.Title = "Delete Confirmation";
            d.HasCloseButton = false;
            d.Closed += OnDeleteGoalElementClosed;
            d.Show();
        }

        private void OnDeleteGoalElementClosed(object s, EventArgs err)
        {
            var dialog = (ChildWindow)s;
            dialog.Closed -= OnDeleteGoalElementClosed;

            if (dialog.DialogResult == false)
            {
                return;
            }

            try
            {
                CurrentAdmissionGoalElement.CancelEditting();
                AdmissionGoal mAdmissionGoal = CurrentAdmissionGoalElement.AdmissionGoal;
                RemoveGoalElement_Command(CurrentAdmissionGoalElement);
                if (mAdmissionGoal != null)
                {
                    ValidateGoal(mAdmissionGoal, IsAssistant, false);
                    mAdmissionGoal.RaisePropertyChangedAfterEdit();
                    CanCancelGoalEdit = false;
                }
            }
            catch
            {
            }

            RaisePropertyChanged("FilteredGoalElementsCurrentGoal");
            if (PopupDataTemplate == "EditGoalElementPopupDataTemplate")
            {
                PopupDataTemplate = null;
            }
        }

        private async System.Threading.Tasks.Task OKEditGoalElement_Command()
        {
            if (CurrentAdmissionGoalElement == null)
            {
                return;
            }

            bool goalElementValid = ValidateGoalElement(CurrentAdmissionGoalElement.AdmissionGoal, CurrentAdmissionGoalElement, IsAssistant);
            CurrentAdmissionGoalElement.RaisePropertyChangedAfterEdit();
            if (goalElementValid == false)
            {
                return;
            }

            CurrentAdmissionGoalElement.EndEditting();
            CanCancelGoalEdit = false;
            if (CurrentAdmissionGoalElement.AdmissionGoal != null)
            {
                ValidateGoal(CurrentAdmissionGoalElement.AdmissionGoal, IsAssistant, false);
                CurrentAdmissionGoalElement.AdmissionGoal.RaisePropertyChangedAfterEdit();
            }
            PropagateGoalElementStatus(CurrentAdmissionGoalElement.AdmissionGoal, CurrentAdmissionGoalElement);
            PopupDataTemplate = null;
            if (ViewModel != null)
            {
                await ViewModel.AutoSave_Command("GoalManagerEditGoalElement");
            }
        }

        private List<Int32> GetDisciplineListForGoalSearch()
        {
            var DateToUse = EncounterDate();
            var up = UserCache.Current.GetCurrentUserProfile();
            List<Int32> discList = null;
            ;
            if (IsPlanOfCare && CurrentEncounter != null && CurrentEncounter.EncounterPlanOfCare != null
                && CurrentEncounter.EncounterPlanOfCare.FirstOrDefault() != null)
            {
                var epc = CurrentEncounter.EncounterPlanOfCare.FirstOrDefault();
                discList = CurrentAdmission.AdmissionDiscipline.Where(ad =>
                        ad.AdmissionStatusCode != "N" && ad.ReferDate != null &&
                        ad.ReferDate <= epc.CertificationThruDate
                        && (ad.DischargeDateTime == null || ad.DischargeDateTime >= epc.CertificationFromDate))
                    .Select(s => s.DisciplineKey).ToList();
            }
            else if (IsTeamMeeting || IsOrderEntry)
            {
                if (CurrentAdmission.ActiveAdmissionDisciplines != null)
                {
                    discList = CurrentAdmission.ActiveAdmissionDisciplines.Select(s => s.DisciplineKey)
                        .ToList(); // maybe something slightly different for team meating?
                }
            }
            else if (up != null && up.UserIsASupervisor)
            {
                int? dscpkey = (CurrentEncounter == null || CurrentEncounter.ServiceTypeKey == null)
                    ? null
                    : ServiceTypeCache.GetDisciplineKey(CurrentEncounter.ServiceTypeKey.Value);
                if (CurrentAdmission.ActiveAdmissionDisciplines != null)
                {
                    discList = CurrentAdmission.ActiveAdmissionDisciplines.Where(ad => UserCache.Current
                            .GetCurrentUserProfile().DisciplineInUserProfile
                            .Any(d => d.IsSupervisor && d.DisciplineKey == ad.DisciplineKey))
                        .Select(s => s.DisciplineKey).ToList();
                    if (dscpkey != null && discList != null && !discList.Contains((int)dscpkey))
                    {
                        discList.Add((int)dscpkey);
                    }
                }
            }
            else
            {
                discList = CurrentAdmission.AdmissionDiscipline.Where(ad => ad.ReferDate != null &&
                                                                            ad.ReferDate <= DateToUse
                                                                            && (ad.DischargeDateTime == null ||
                                                                                ad.DischargeDateTime >= DateToUse))
                    .Select(s => s.DisciplineKey).ToList();
            }

            // just in case anything down stream assumes a list will be there
            // and we dont have one, create an empty one.
            if (discList == null)
            {
                discList = new List<int>();
            }

            return discList;
        }

        public void SetupGoalCommands(AdmissionGoal ag)
        {
            ag.RemoveGoalCommand = new RelayCommand<AdmissionGoal>(agoal => { RemoveGoal_Command(agoal); });
            ag.OpenGoalElementCommand = new RelayCommand<AdmissionGoal>(agoal =>
            {
                CurrentAdmissionGoal = agoal;
                AddGoalElementVisible = true;
            });
            ag.CopyGoalElementCommand = new RelayCommand<AdmissionGoalElement>(agoale =>
            {
                if ((agoale == null) || (agoale.AdmissionGoal == null))
                {
                    return;
                }

                GoalElement newGoalElement = GoalCache.GetGoalElementByKey(agoale.GoalElementKey);
                AddGoalElement_Command(newGoalElement);
            });
        }

        public void RemoveGoal_Command(AdmissionGoal agoal)
        {
            agoal.ValidationErrors.Clear();
            ((IPatientService)Model).Remove(agoal);
            SendGoalElementRefreshMessage();
            this.RaisePropertyChangedLambda(p => p.HasFilteredGoals);
            if (CurrentAdmissionGoal != null)
            {
                CurrentAdmissionGoal.RaisePropertyChangedFilteredGoalElements();
            }

            RaisePropertyChangedFilteredGoalElements();
        }

        private void SendGoalElementRefreshMessage()
        {
            RefreshgoalElementList();
            Messenger.Default.Send(true, "RefreshGoalElements");
        }

        public void SetupGoalElementCommands(AdmissionGoalElement age)
        {
            age.RemoveGoalElementCommand = new RelayCommand<AdmissionGoalElement>(RemoveGoalElement_Command);
        }

        private void RemoveGoalElement_Command(AdmissionGoalElement agoale)
        {
            agoale.ValidationErrors.Clear();
            ((IPatientService)Model).Remove(agoale);
            SendGoalElementRefreshMessage();
            if ((ViewModel != null) && (ViewModel.CurrentOrderEntryManager != null))
            {
                ViewModel.CurrentOrderEntryManager.UpdateGeneratedOrderText();
            }

            if (CurrentAdmissionGoal != null)
            {
                CurrentAdmissionGoal.RaisePropertyChangedFilteredGoalElements();
            }

            RaisePropertyChangedFilteredGoalElements();
        }

        public void DataTemplateLoaded()
        {

        }

        private void OnGoalsLoaded(IEnumerable<Goal> goals)
        {
            SearchGoals = new ObservableCollection<Goal>(goals);
            ClearGoalCommand.RaiseCanExecuteChanged();
        }

        private void OnGoalElementsLoaded(IEnumerable<GoalElement> goalElements)
        {
            SearchGoalElements = new ObservableCollection<GoalElement>(goalElements);
            ClearGoalElementCommand.RaiseCanExecuteChanged();
        }

        public void UpdateGoals(QuestionUI q, string response, string subresponse, string reason, bool remove,
            int? keytouse)
        {
            if (TenantSettingsCache.Current.TenantSettingCarePlanMap == false)
            {
                return;
            }

            if (CurrentEncounter != null && CurrentEncounter.IsAssistant)
            {
                return; //Don't map goals if the servicetype and user are assistants
            }

            if (remove || reason.Equals("None"))
            {
                foreach (var item in q.Question.QuestionGoal.Where(p => p.TenantID == CurrentAdmission.TenantID))
                {
                    //Only remove those AdmissionGoal rows that we (GoalManager) added
                    //Rows with null QuestionKey were manually added by end user
                    var goals = CurrentAdmission.AdmissionGoal
                        .Where(g => g.QuestionGoalKey == item.QuestionGoalKey && g.IsNew).ToList();
                    goals.ForEach(g => ((IPatientService)Model).Remove(g));
                }
            }
            else
            {
                //Add AdmissionGoal
                //Flag AdmissionGoal as changed
                var dscpkey = ServiceTypeCache.GetDisciplineKey(CurrentEncounter.ServiceTypeKey.Value);

                foreach (var qg in q.Question.QuestionGoal.Where(p => p.TenantID == CurrentAdmission.TenantID))
                {
                    var goal = GoalCache.GetGoalFromKey(qg.GoalKey);
                    if (!goal.Inactive && (goal.DisciplineInGoal.Where(d => d.DisciplineKey == dscpkey).Any() ||
                                           goal.DisciplineInGoal.Any() == false))
                    {
                        if (qg.Condition == null || response.ToLower().Contains(qg.Condition.ToLower()) ||
                            reason.ToLower().Contains(qg.Condition.ToLower()))
                        {
                            AdmissionGoal ag = CurrentAdmission.AdmissionGoal
                                .Where(g => g.QuestionGoalKey == qg.QuestionGoalKey).FirstOrDefault();
                            if (ag == null)
                            {
                                //don't have, so ADD
                                ag = new AdmissionGoal
                                {
                                    GoalKey = qg.GoalKey,
                                    QuestionGoalKey = qg.QuestionGoalKey,
                                    QuestionLabel = "Target " + q.Label +
                                                    (string.IsNullOrEmpty(qg.Condition) ? "" : "  / " + qg.Condition),
                                    QuestionTemplate = string.IsNullOrEmpty(qg.TemplateOverride)
                                        ? q.Question.DataTemplate
                                        : qg.TemplateOverride,
                                    QuestionLookupType = q.Question.LookupType,
                                    QuestionResponse = response,
                                    QuestionSubResponse = subresponse,
                                    QuestionReason = (!string.IsNullOrEmpty(qg.Condition) && reason.ToLower().Contains(qg.Condition.ToLower()))
                                            ? qg.Condition
                                            : reason,
                                    FormatParameter = response,
                                    FormatSubParameter = subresponse,
                                    ResponseKey = keytouse,
                                    AddedBy = WebContext.Current.User.MemberID,
                                    AddedDate = DateTime.Now,
                                    UpdatedBy = CurrentEncounter.UpdatedBy,
                                    UpdatedDate = DateTime.Now,
                                };
                                if ((CurrentEncounter != null && (CurrentEncounter.EncounterKey > 0)))
                                {
                                    ag.AddedFromEncounterKey = CurrentEncounter.EncounterKey;
                                }

                                foreach (var ge in goal.GoalElementInGoal.Where(p =>
                                             !p.GoalElement.Inactive &&
                                             (p.GoalElement.DisciplineInGoalElement.Any(d => d.DisciplineKey == dscpkey) ||
                                              p.GoalElement.DisciplineInGoalElement.Any() == false)))
                                {
                                    AdmissionGoalElement age = new AdmissionGoalElement
                                    {
                                        GoalElementKey = ge.GoalElementKey,
                                        GoalElementText = IsPlanOfCare
                                            ? ge.GoalElement.POCOvrTextOrLongDescription
                                            : ge.GoalElement.LongDescription,
                                        POCOverrideCode = ge.GoalElement.POCOverrideCode,
                                        IncludeonPOC = CurrentAdmission.HospiceAdmission || ge.GoalElement.Orders,
                                        POCProtected = ge.GoalElement.Orders,
                                        AddedBy = WebContext.Current.User.MemberID,
                                        AddedDate = DateTime.Now,
                                        UpdatedBy = CurrentEncounter.UpdatedBy,
                                        UpdatedDate = DateTime.Now,
                                    };
                                    if ((CurrentEncounter != null && (CurrentEncounter.EncounterKey > 0)))
                                    {
                                        age.AddedFromEncounterKey = CurrentEncounter.EncounterKey;
                                    }

                                    EncounterGoalElement ege = new EncounterGoalElement();
                                    age.EncounterGoalElement.Add(ege);
                                    CurrentEncounter.EncounterGoalElement.Add(ege);
                                    age.CurrentEncounterGoalElement = ege;

                                    ag.AdmissionGoalElement.Add(age);
                                    CurrentEncounter.AdmissionGoalElement.Add(age);
                                    ege.PopuulateEncounterGoalElementDisciplines(age.GoalElementDisciplineKeys);

                                    SetupGoalElementCommands(age);
                                }

                                EncounterGoal eg = new EncounterGoal();
                                ag.EncounterGoal.Add(eg);
                                CurrentEncounter.EncounterGoal.Add(eg);

                                SetupGoalCommands(ag);

                                CurrentAdmission.AdmissionGoal.Add(ag);
                                CurrentEncounter.AdmissionGoal.Add(ag);

                                ValidateGoal(ag, IsAssistant, false);
                                ag.RaisePropertyChangedAfterEdit();
                                foreach (AdmissionGoalElement age in ag.AdmissionGoalElement)
                                {
                                    ValidateGoalElement(age.AdmissionGoal, age, IsAssistant);
                                    age.RaisePropertyChangedAfterEdit();
                                }

                                ag.FilterGoalElements(this);
                            }
                            else
                            {
                                //Have they modified something from what we built, so set ReEvaluate
                                if (ag.TextChanged || ag.AdmissionGoalElement.Any(p => p.TextChanged || p.Save == false))
                                {
                                    ag.ReEvaluate = true;
                                }
                                else
                                {
                                    ag.QuestionResponse = response;
                                    ag.QuestionSubResponse = subresponse;
                                    ag.QuestionReason =
                                        (!string.IsNullOrEmpty(qg.Condition) &&
                                         reason.ToLower().Contains(qg.Condition.ToLower()))
                                            ? qg.Condition
                                            : reason;

                                    //They haven't completed the data template to fully populate the question yet so update the defaults
                                    if (string.IsNullOrEmpty(ag.GoalText))
                                    {
                                        ag.FormatParameter = response;
                                        ag.FormatSubParameter = subresponse;
                                        ag.ResponseKey = keytouse;
                                    }
                                }

                                ValidateGoal(ag, IsAssistant, false);
                                ag.RaisePropertyChangedAfterEdit();
                            }
                        }
                        else
                        {
                            AdmissionGoal eg = CurrentAdmission.AdmissionGoal.FirstOrDefault(g => g.QuestionGoalKey == qg.QuestionGoalKey && g.IsNew);
                            if ((eg != null) && (Model != null))
                            {
                                CurrentAdmission.AdmissionGoal.Remove(eg);
                                //To fix: The INSERT statement conflicted with the FOREIGN KEY constraint "FK_AdmissionGroup_AdmissionKey". The conflict occurred in database "CrescendoLive", table "dbo.Admission", column 'AdmissionKey'.  The statement has been terminated.
                                ((IPatientService)Model).Remove(eg);
                            }
                        }
                    }
                }
            }

            if ((ViewModel != null) && (ViewModel.CurrentOrderEntryManager != null))
            {
                ViewModel.CurrentOrderEntryManager.UpdateGeneratedOrderText();
            }

            this.RaisePropertyChangedLambda(p => p.HasFilteredGoals);
            if (CurrentAdmissionGoal != null)
            {
                CurrentAdmissionGoal.RaisePropertyChangedFilteredGoalElements();
            }

            RaisePropertyChangedFilteredGoalElements();
        }

        public event EventHandler ShortLongTermWarningMessageChanged;
        private string _shortLongTermWarningMessage;

        public string ShortLongTermWarningMessage
        {
            get { return _shortLongTermWarningMessage; }
            set
            {
                _shortLongTermWarningMessage = value;
                if (ShortLongTermWarningMessageChanged != null)
                {
                    ShortLongTermWarningMessageChanged(this, EventArgs.Empty);
                }

                this.RaisePropertyChangedLambda(p => p.ShortLongTermWarningMessage);
            }
        }

        public bool ValidateGoalsWarnings()
        {
            string message = string.Empty;
            if (IsPlanOfCareOrTherapyEvalVisitResumption)
            {
                bool termGoalsPresent = FilteredGoals.View.Cast<AdmissionGoal>().ToList()
                    .Any(a => (a.ShowShortLongTerm && (a.Superceded == false)));
                if (termGoalsPresent)
                {
                    bool shortTermGoals = FilteredGoals.View.Cast<AdmissionGoal>().ToList()
                        .Any(a => ((a.ShortTermGoal == true) && (a.Superceded == false)));
                    bool longTermGoals = FilteredGoals.View.Cast<AdmissionGoal>().ToList()
                        .Any(a => ((a.LongTermGoal == true) && (a.Superceded == false)));
                    if (!shortTermGoals && !longTermGoals)
                    {
                        message = "WARNING: No short or long term goals are defined.";
                    }
                    else if (shortTermGoals && !longTermGoals)
                    {
                        message = "WARNING: No long term goals are defined.";
                    }
                    else if (!shortTermGoals)
                    {
                        message = "WARNING: No short term goals are defined.";
                    }
                }
            }

            ShortLongTermWarningMessage = string.IsNullOrWhiteSpace(message) ? null : message;
            ;
            return string.IsNullOrWhiteSpace(message) ? true : false;
        }

        bool areGoalElementChanges;

        public bool ValidateGoals(bool isAssistant, bool validatePlannedGoalElementsAreAddressed)
        {
            bool AllValid = true;
            areGoalElementChanges = false;

            foreach (var ag in CurrentAdmission.AdmissionGoal)
            {
                var goalValid = ValidateGoal(ag, isAssistant, true, validatePlannedGoalElementsAreAddressed);
                if (goalValid == false)
                {
                    AllValid = false;
                }

                ag.RaisePropertyChangedHasValidationErrors();
            }

            // Way to pick up GoalElement text changes
            if ((AllValid) && (areGoalElementChanges) && (ViewModel != null) &&
                (ViewModel.CurrentOrderEntryManager != null))
            {
                ViewModel.CurrentOrderEntryManager.UpdateGeneratedOrderText();
            }

            return AllValid;
        }

        public bool ValidateGoal(AdmissionGoal ag, bool isAssistant, bool validateGoalElements, bool validatePlannedGoalElementsAreAddressed = false)
        {
            bool AllValid = true;
            ag.ValidationErrors.Clear();
            if (ag.GoalManager == null)
            {
                ag.GoalManager = this;
            }

            if (ag.Superceded)
            {
                ag.RaisePropertyChangedHasValidationErrors();
                this.RaisePropertyChangedLambda(p => p.ShowGoalsInvalid);
                return AllValid;
            }

            if (!ag.Validate())
            {
                AllValid = false;
            }

            if (ag.CanEditGoalText && !isAssistant && string.IsNullOrEmpty(ag.GoalText))
            {
                AllValid = false;
                ViewModel.ValidEnoughToSave = false;
                ag.ValidationErrors.Add(
                    new ValidationResult(("Before the form can be saved, the Goal Text is required"),
                        new[] { "GoalText" }));
            }

            if (ag.CanEditGoalText && !isAssistant &&
                ((ag.IsGoalTextChangedFromLongDescription == false) || ag.IsGoalTextParameterized))
            {
                AllValid = false;
                ViewModel.ValidEnoughToSave = false;
                ag.ValidationErrors.Add(new ValidationResult(
                    ("Before the form can be completed and saved, the goal text must be modified to reflect the needs for this case, including defining the parameter values."),
                    new[] { "GoalText" }));
            }

            if ((ag.Unattainable) && (!ag.UnattainableReason.HasValue || ag.UnattainableReason == 0))
            {
                AllValid = false;
                ag.ValidationErrors.Add(new ValidationResult(("Unattainable Reason is needed"),
                    new[] { "UnattainableReason" }));
            }

            if (string.IsNullOrWhiteSpace(ag.InactivatedReason)) ag.InactivatedReason = null;
            if ((ag.Inactivated) && (ag.InactivatedReason == null))
            {
                AllValid = false;
                ag.ValidationErrors.Add(new ValidationResult(("Inactivated Reason is needed"),
                    new[] { "InactivatedReason" }));
            }

            string oneGoalElementRequired = ag.OneGoalElementRequired;
            if (string.IsNullOrWhiteSpace(oneGoalElementRequired) == false)
            {
                AllValid = false;
                ag.ValidationErrors.Add(new ValidationResult((oneGoalElementRequired), new[] { "GoalText" }));
            }

            if (validateGoalElements)
            {
                foreach (var age in ag.AdmissionGoalElement)
                {
                    var goalElementValid = ValidateGoalElement(ag, age, isAssistant);
                    if (validatePlannedGoalElementsAreAddressed)
                    {
                        goalElementValid &= ValidatePlannedGoalElementAddressed(ag, age);
                    }

                    if (goalElementValid == false)
                    {
                        AllValid = false;
                    }

                    age.RaisePropertyChangedHasValidationErrors();
                }
            }

            ag.RaisePropertyChangedHasValidationErrors();
            this.RaisePropertyChangedLambda(p => p.ShowGoalsInvalid);

            return AllValid;
        }
        // void PropagateGoalStatus(AdmissionGoal ag)
        // {
        // This case in handled in the AdmissionGoal entity where both unattainable and inactivated are propagated
        // into any active child goal elements via OnChanged() events.
        // Unattainable worked this way in the past and appears to continue to work now -
        // If there are issues w.r.t G/GE save logic (cloning/reject-changes in dynamic form - that re-invokes OnChanged() logic)
        // we can move the propagation code to here (for Resolved, Discontinued, Inactivated and Unattainable)
        // }
        public bool ValidateGoalElement(AdmissionGoal ag, AdmissionGoalElement age, bool isAssistant)
        {
            bool AllValid = true;
            age.ValidationErrors.Clear();
            if (age.CurrentEncounterGoalElement != null)
            {
                age.CurrentEncounterGoalElement.ValidationErrors.Clear();
            }

            if (age.Superceded)
            {
                return AllValid;
            }

            if ((age.IsNew) || (age.IsModified))
            {
                areGoalElementChanges = true;
            }

            if (!age.Validate())
            {
                AllValid = false;
            }

            if (age.CanEditGoalElementText && !isAssistant && string.IsNullOrEmpty(age.GoalElementText))
            {
                AllValid = false;
                ViewModel.ValidEnoughToSave = false;
                age.ValidationErrors.Add(new ValidationResult(
                    ("Before the form can be saved, the Goal Element Text is required"), new[] { "GoalElementText" }));
            }

            if ((age.IsNew || ag.IsGoalElementVisibleInVisitMode(age)) && (age.CanEditGoalElementText && !isAssistant &&
                                                                           ((age
                                                                                 .IsGoalElementTextChangedFromLongDescription ==
                                                                             false) ||
                                                                            age.IsGoalElementTextParameterized)))
            {
                AllValid = false;
                ViewModel.ValidEnoughToSave = false;
                age.ValidationErrors.Add(new ValidationResult(
                    ("Before the form can be completed and saved, the goal element text must be modified to reflect the needs for this case, including defining the parameter values."),
                    new[] { "GoalElementText" }));
            }

            if ((age.IsNew || ag.IsGoalElementVisibleInVisitMode(age)) && age.Unattainable &&
                ((age.UnattainableReason.HasValue == false) || (age.UnattainableReason == 0)))
            {
                AllValid = false;
                age.ValidationErrors.Add(new ValidationResult(("Unattainable Reason is needed"),
                    new[] { "UnattainableReason" }));
            }

            if (string.IsNullOrWhiteSpace(age.InactivatedReason)) age.InactivatedReason = null;
            if ((age.IsNew || ag.IsGoalElementVisibleInVisitMode(age)) && age.Inactivated &&
                (age.InactivatedReason == null))
            {
                AllValid = false;
                age.ValidationErrors.Add(new ValidationResult(("Inactivated Reason is needed"),
                    new[] { "InactivatedReason" }));
            }

            if ((ag.IsGoalElementVisibleInVisitMode(age) ||
                 (CurrentEncounter.EncounterGoalElement.Any(eg =>
                     eg.AdmissionGoalElementKey == age.AdmissionGoalElementKey))) &&
                age.CurrentEncounterGoalElement != null && age.CurrentEncounterGoalElement.Addressed &&
                String.IsNullOrWhiteSpace(age.CurrentEncounterGoalElement.Comment))
            {
                if (age.CurrentEncounterGoalElement != null)
                {
                    AllValid = false;
                    age.CurrentEncounterGoalElement.ValidationErrors.Add(new ValidationResult(
                        ("Patient's response to Goal Element / Progress toward Goal is required"),
                        new[] { "Comment" }));
                }
            }

            return AllValid;
        }
        public void PropagateGoalElementStatus(AdmissionGoal ag, AdmissionGoalElement age)
        {
            // if this is the last active goal element propagate the status of inactive or unattainable onto the parent goal
            // unless any are resolved - then mark the goal as resolved
            if ((ag == null) || (age == null)) return;
            bool anyActive = ag.AdmissionGoalElement.Where(p => !p.Superceded && !p.Resolved && !p.Discontinued && !p.Unattainable && !p.Inactivated).Any();
            if (anyActive) return;
            bool anyResolved = ag.AdmissionGoalElement.Where(p => !p.Superceded && p.Resolved).Any();
            if ((age.PreviousUnattainable == false) && (age.Unattainable == true))
            {
                // last active goal element has been newly unattainable 
                if (anyResolved) // propagate resolved to goal
                {
                    ag.Resolved = true;
                    ag.ResolvedDate = age.UnattainableDate;
                    ag.ResolvedBy = age.UnattainableBy;
                    return;
                }
                // propagate unattainable to goal
                ag.Unattainable = true;
                ag.UnattainableDate = age.UnattainableDate;
                ag.UnattainableBy = age.UnattainableBy;
                ag.UnattainableReason = age.UnattainableReason;
                return;
            }
            if ((age.PreviousInactivated == false) && (age.Inactivated == true))
            {
                // last active goal element has been newly inactivated 
                if (anyResolved) // propagate resolved to goal
                {
                    ag.Resolved = true;
                    ag.ResolvedDate = age.InactivatedDate;
                    ag.ResolvedBy = age.InactivatedBy;
                    return;
                }
                // propagate inactivated to goal
                ag.Inactivated = true;
                ag.InactivatedDate = age.InactivatedDate;
                ag.InactivatedBy = age.InactivatedBy;
                ag.InactivatedReason = age.InactivatedReason;
                return;
            }

        }

        public bool ValidatePlannedGoalElementAddressed(AdmissionGoal ag, AdmissionGoalElement age)
        {
            if (ag.IsGoalElementPlannedForThisEncounter(age) &&
                CurrentEncounter.EncounterGoalElement.Any(eg =>
                    eg.AdmissionGoalElementKey == age.AdmissionGoalElementKey) &&
                age.CurrentEncounterGoalElement != null &&
                !(age.CurrentEncounterGoalElement.Addressed ||
                  age.Resolved ||
                  age.Unattainable ||
                  age.Discontinued ||
                  age.Inactivated))
            {
                age.CurrentEncounterGoalElement.ValidationErrors.Add(
                    new ValidationResult(
                        "All goal elements with a yellow highlight must be addressed",
                        new[] { "PlannedButUnaddressedGoalElement", }));
                return false;
            }

            return true;
        }

        int __cleanupCount;

        public override void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            FilteredGoals.Source = null;
            FilteredDischargeGoals.Source = null;
            PrintCollection.Source = null;
            POCFilteredGoals.Source = null;
            POCFilteredGoalElements.Source = null;

            if (GoalManagerService != null)
            {
                GoalManagerService.Cleanup();
                GoalManagerService = null;
            }

            Model = null;
            if (goalElementList != null)
            {
                goalElementList.ForEach(ge => ge.Cleanup());
                goalElementList.Clear();
                goalElementList = null;
            }

            base.Cleanup();
        }
    }
}