#region Usings

using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Occasional;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionGoal
    {
        private int __cleanupCount;
        private EncounterGoal _CurrentEncounterGoal;
        private string _ElementText = "";

        public new bool HasValidationErrors
        {
            get
            {
                if (OneGoalElementRequired != null)
                {
                    return true;
                }

                if (ValidationErrors == null)
                {
                    return false;
                }

                return ValidationErrors.Any() == false ? false : true;
            }
        }

        public bool HasIncludeonPOCGoalElements
        {
            get
            {
                if (AdmissionGoalElement == null)
                {
                    return false;
                }

                return AdmissionGoalElement.Where(ge => ge.IncludeonPOC).Any();
            }
        }

        public string ValidationSummary
        {
            get
            {
                if (ValidationErrors == null || ValidationErrors.Any() == false)
                {
                    return GoalElementsInError;
                }

                var CR = char.ToString('\r');
                var vs = "";
                foreach (var vr in ValidationErrors)
                {
                    if (string.IsNullOrWhiteSpace(vr.ErrorMessage))
                    {
                        continue;
                    }

                    if (vs.Contains(vr.ErrorMessage))
                    {
                        continue;
                    }

                    vs = string.IsNullOrWhiteSpace(vs) ? vr.ErrorMessage : vs + CR + vr.ErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(GoalElementsInError) == false)
                {
                    vs = vs + CR + GoalElementsInError;
                }

                return string.IsNullOrWhiteSpace(vs) ? null : vs;
            }
        }

        public string OneGoalElementRequired
        {
            get
            {
                if (CanManipulateGoalElements && CurrentEncounterIsAssistant == false &&
                    AdmissionGoalElement.Any() == false)
                {
                    return "At least one goal element is required";
                }

                return null;
            }
        }

        public string GoalElementsInError =>
            FilteredGoalElementsHasValidationErrors == false ? null : "Goal Element(s) Are In Error";

        public string HasValidationErrorsToolTip
        {
            get
            {
                var errors = ValidationErrors == null ? false : ValidationErrors.Any() == false ? false : true;
                var childErrors = FilteredGoalElementsHasValidationErrors;
                if (errors && childErrors == false)
                {
                    return "Edit Goal (goal in error).  " + OneGoalElementRequired;
                }

                if (errors == false && childErrors)
                {
                    return "Edit/View Goal (goal element(s) in error).  " + OneGoalElementRequired;
                }

                if (errors && childErrors)
                {
                    return "Edit Goal (goal in error and goal element(s) in error).  " + OneGoalElementRequired;
                }

                if (OneGoalElementRequired != null)
                {
                    return "Edit Goal (goal in error).  " + OneGoalElementRequired;
                }

                return "Edit/View Goal";
            }
        }

        public string DisciplineCode
        {
            get
            {
                var g = GoalCache.GetGoalFromKey(GoalKey);
                if (g == null)
                {
                    return "Multi";
                }

                var dList = g.DisciplineInGoal?.ToList();
                if (dList == null || dList.Count() != 1)
                {
                    return "Multi";
                }

                var disciplineKey = dList.Select(dig => dig.DisciplineKey).First();
                var d = DisciplineCache.GetDisciplineFromKey(disciplineKey);
                if (d == null || string.IsNullOrWhiteSpace(d.Code))
                {
                    return "Multi";
                }

                return d.Code;
            }
        }

        public string GoalCode
        {
            get
            {
                var g = GoalCache.GetGoalFromKey(GoalKey);
                if (g == null || string.IsNullOrWhiteSpace(g.CodeValue))
                {
                    return null;
                }

                return g.CodeValue;
            }
        }

        public string GoalDescription
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GoalText) == false)
                {
                    return GoalText;
                }

                var g = GoalCache.GetGoalFromKey(GoalKey);
                if (g != null && string.IsNullOrWhiteSpace(g.LongDescription) == false)
                {
                    return g.LongDescription;
                }

                if (g != null && string.IsNullOrWhiteSpace(g.ShortDescription) == false)
                {
                    return g.ShortDescription;
                }

                return null;
            }
        }

        public string FinalStatusAndDate
        {
            get
            {
                if (Discontinued)
                {
                    return "Discontinued " + FormatDateString(DiscontinuedDate);
                }

                if (Resolved)
                {
                    return "Resolved " + FormatDateString(ResolvedDate);
                }

                if (Unattainable)
                {
                    return "Unattainable " + FormatDateString(UnattainableDate);
                }

                if (Inactivated)
                {
                    return "Inactivated " + FormatDateString(InactivatedDate);
                }

                return null;
            }
        }

        public GoalManager GoalManager { get; set; }
        public CollectionViewSource FilteredGoalElements { get; set; }

        public bool HasFilteredGoalElements
        {
            get
            {
                if (FilteredGoalElements == null || FilteredGoalElements.View == null)
                {
                    return false;
                }

                return FilteredGoalElements.View.Cast<AdmissionGoalElement>().Any();
            }
        }

        private bool FilteredGoalElementsHasValidationErrors
        {
            get
            {
                if (FilteredGoalElements == null || FilteredGoalElements.View == null)
                {
                    return false;
                }

                return FilteredGoalElements.View.Cast<AdmissionGoalElement>().Where(age => age.HasValidationErrors).Any();
            }
        }

        public EncounterGoal CurrentEncounterGoal
        {
            get { return _CurrentEncounterGoal; }
            set
            {
                _CurrentEncounterGoal = value;
                RaisePropertyChanged("CurrentEncounterGoal");
            }
        }

        public bool ShowShortLongTerm
        {
            get
            {
                if (GoalManager == null)
                {
                    return false;
                }

                return GoalManager.ShowShortLongTerm(this);
            }
        }

        public bool CurrentEncounterIsAssistantNonHospice
        {
            get
            {
                // remove Required for Discharge for Hospice
                if (Admission != null && Admission.HospiceAdmission)
                {
                    return true;
                }

                return CurrentEncounterGoal?.EncounterIsAssistant ?? true;
            }
        }

        public bool CurrentEncounterIsAssistant => CurrentEncounterGoal?.EncounterIsAssistant ?? true;

        public bool EncounterIsInEdit
        {
            get
            {
                if (GoalManager == null)
                {
                    return false;
                }

                if (GoalManager.CurrentEncounter == null)
                {
                    return false;
                }

                if (GoalManager.CurrentEncounter.EncounterBy == WebContext.Current.User.MemberID &&
                    (GoalManager.CurrentEncounter.PreviousEncounterStatus == (int)EncounterStatusType.Edit ||
                     GoalManager.CurrentEncounter.PreviousEncounterStatus == (int)EncounterStatusType.CoderReviewEdit ||
                     GoalManager.CurrentEncounter.PreviousEncounterStatus == (int)EncounterStatusType.OASISReviewEdit ||
                     GoalManager.CurrentEncounter.PreviousEncounterStatus ==
                     (int)EncounterStatusType.OASISReviewEditRR))
                {
                    return true;
                }

                if (GoalManager.CurrentEncounter.UserIsPOCOrderReviewerAndInPOCOrderReview)
                {
                    return true;
                }

                return false;
            }
        }

        public bool AddedThisEncounter
        {
            get
            {
                if (GoalManager == null)
                {
                    return false;
                }

                if (GoalManager.CurrentEncounter == null)
                {
                    return false;
                }

                if (GoalManager.CurrentEncounter.EncounterKey == AddedFromEncounterKey)
                {
                    return true;
                }

                return false;
            }
        }

        public Guid? AddedEncounterBy => AddedBy;

        public DateTime? AddedEncounterDate => AddedDate?.Date;

        public bool DiscontinuedThisEncounter
        {
            get
            {
                if (GoalManager == null)
                {
                    return false;
                }

                if (GoalManager.CurrentEncounter == null)
                {
                    return false;
                }

                if (GoalManager.CurrentEncounter.EncounterKey == DiscontinuedFromEncounterKey)
                {
                    return true;
                }

                return false;
            }
        }

        public Guid? DiscontinuedEncounterBy
        {
            get
            {
                if (GoalManager == null)
                {
                    return null;
                }

                if (GoalManager.CurrentEncounter == null)
                {
                    return null;
                }

                if (GoalManager.CurrentEncounter.EncounterKey != DiscontinuedFromEncounterKey)
                {
                    return null;
                }

                return DiscontinuedBy;
            }
        }

        public DateTime? DiscontinuedEncounterDate
        {
            get
            {
                if (GoalManager == null)
                {
                    return null;
                }

                if (GoalManager.CurrentEncounter == null)
                {
                    return null;
                }

                if (GoalManager.CurrentEncounter.EncounterKey != DiscontinuedFromEncounterKey)
                {
                    return null;
                }

                return DiscontinuedDate == null ? (DateTime?)null : ((DateTimeOffset)DiscontinuedDate).Date;
            }
        }

        public bool CanEditGoalText
        {
            get
            {
                if (EncounterIsInEdit == false)
                {
                    return false;
                }

                if (IsPlanOfCare)
                {
                    return AllowEdit || MustEdit ? true : false;
                }

                if ((AllowEdit || MustEdit) == false)
                {
                    return false;
                }

                if (IsNew)
                {
                    return true;
                }

                if (AddedFromEncounterKey <= 0)
                {
                    return true;
                }

                if (AddedThisEncounter)
                {
                    return true;
                }

                return false;
            }
        }

        public bool AllowEdit
        {
            get
            {
                var g = GoalCache.GetGoalFromKey(GoalKey);
                if (g == null)
                {
                    return false;
                }

                return g.AllowEdit;
            }
        }

        public bool MustEdit
        {
            get
            {
                var g = GoalCache.GetGoalFromKey(GoalKey);
                if (g == null)
                {
                    return false;
                }

                return g.MustEdit;
            }
        }

        public bool IsGoalTextParameterized
        {
            get
            {
                if (MustEdit == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GoalText))
                {
                    return false;
                }

                for (var i = 2; i < 11; i++)
                    if (GoalText.Contains("{" + i.ToString().Trim() + "}"))
                    {
                        return true;
                    }

                return false;
            }
        }

        public bool IsGoalTextChangedFromLongDescription
        {
            get
            {
                if (MustEdit == false)
                {
                    return true;
                }

                var g = GoalCache.GetGoalFromKey(GoalKey);
                if (g == null)
                {
                    return true;
                }

                var longDescription = string.IsNullOrWhiteSpace(g.LongDescription) ? "" : g.LongDescription.Trim();
                var goalText = string.IsNullOrWhiteSpace(GoalText) ? "" : GoalText.Trim();
                return longDescription != goalText;
            }
        }

        public string ShortLongTermGoalText
        {
            get
            {
                if (TenantSettingsCache.Current.TenantSettingPOCGoalIdentification)
                {
                    if (ShortTermGoal == true)
                    {
                        return "Short term: " + GoalText;
                    }

                    if (LongTermGoal == true)
                    {
                        return "Long term: " + GoalText;
                    }
                }

                return GoalText;
            }
        }

        public string ElementText
        {
            get
            {
                // ElementText should only be shown for HospiceAdmissions
                if (Admission == null || !Admission.HospiceAdmission)
                {
                    return "";
                }

                if (FilteredGoalElements == null || FilteredGoalElements.View == null)
                {
                    return "";
                }

                var l = FilteredGoalElements.View.SourceCollection;

                if (l == null)
                {
                    return "";
                }

                _ElementText = "";
                foreach (var item in l)
                {
                    var t = item as AdmissionGoalElement;

                    // Filter out duplicate GoalElementTexts
                    if (t != null &&
                        !string.IsNullOrEmpty(t.GoalElementText) &&
                        !_ElementText.Contains(t.GoalElementText))
                    {
                        _ElementText += t.GoalElementText + Environment.NewLine;
                    }
                }

                return _ElementText;
            }
            set
            {
                _ElementText = value;
                RaisePropertyChanged("ElementText");
            }
        }

        public string GoalStatus
        {
            get
            {
                if (Discontinued)
                {
                    return "Discontinued";
                }

                if (Resolved)
                {
                    return "Resolved";
                }

                if (Unattainable)
                {
                    return "Unattainable";
                }

                if (Inactivated)
                {
                    return "Inactivated";
                }

                return null;
            }
        }

        public RelayCommand<AdmissionGoal> RemoveGoalCommand { get; set; }

        public RelayCommand<AdmissionGoal> OpenGoalElementCommand { get; set; }

        public RelayCommand<AdmissionGoalElement> CopyGoalElementCommand { get; set; }

        public void RaisePropertyChangedHasValidationErrors()
        {
            RaisePropertyChanged("HasValidationErrors");
            RaisePropertyChanged("HasValidationErrorsToolTip");
            RaisePropertyChanged("ValidationSummary");
        }

        public void RaisePropertyChangedAfterEdit()
        {
            RaisePropertyChanged("DisciplineCode");
            RaisePropertyChanged("GoalCode");
            RaisePropertyChanged("GoalDescription");
            RaisePropertyChanged("AddedDate");
            RaisePropertyChanged("FinalStatusAndDate");
            RaisePropertyChangedHasValidationErrors();
        }

        private string FormatDateString(DateTime? date)
        {
            if (date == null)
            {
                return "?";
            }

            return ((DateTime)date).Date.ToShortDateString().Trim();
        }

        public void RaisePropertyChangedFilteredGoalElements()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                RaisePropertyChanged("HasFilteredGoalElements");
                RaisePropertyChanged("FilteredGoalElements");
                RaisePropertyChanged("CanEditNonText");
            });
        }

        public void RaisePropertyChanged()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { RaisePropertyChanged(null); });
        }

        public void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            if (GoalManager != null)
            {
                GoalManager.Cleanup();
            }

            GoalManager = null;
            try
            {
                if (AdmissionGoalElement != null)
                {
                    AdmissionGoalElement.ForEach(age => age.Cleanup());
                }
            }
            catch
            {
            }

            try
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        if (FilteredGoalElements != null)
                        {
                            FilteredGoalElements.Filter -= FilteredGoalElements_Filter;
                        }
                    }
                    catch
                    {
                    }

                    try
                    {
                        if (FilteredGoalElements != null)
                        {
                            FilteredGoalElements.Source = null;
                        }
                    }
                    catch
                    {
                    }
                });
            }
            catch
            {
            }
        }

        public bool ActiveAsOfDate(DateTime? date)
        {
            if (DiscontinuedAsOfDate(date))
            {
                return false;
            }

            if (ResolvedAsOfDate(date))
            {
                return false;
            }

            if (UnattainableAsOfDate(date))
            {
                return false;
            }
            if (InactivatedAsOfDate(date))
            {
                return false;
            }

            return true;
        }

        public bool DiscontinuedAsOfDate(DateTime? date)
        {
            if (Discontinued == false)
            {
                return false;
            }

            if (date == null || DiscontinuedDate == null)
            {
                return true;
            }

            if (((DateTime)DiscontinuedDate).Date < ((DateTime)date).Date)
            {
                return true;
            }

            return false;
        }

        public bool ResolvedAsOfDate(DateTime? date)
        {
            if (Resolved == false)
            {
                return false;
            }

            if (date == null || ResolvedDate == null)
            {
                return true;
            }

            if (((DateTime)ResolvedDate).Date < ((DateTime)date).Date)
            {
                return true;
            }

            return false;
        }

        public bool UnattainableAsOfDate(DateTime? date)
        {
            if (Unattainable == false)
            {
                return false;
            }

            if (date == null || UnattainableDate == null)
            {
                return true;
            }

            if (((DateTime)UnattainableDate).Date < ((DateTime)date).Date)
            {
                return true;
            }

            return false;
        }
        public bool InactivatedAsOfDate(DateTime? date)
        {
            if (Inactivated == false)
            {
                return false;
            }

            if (date == null || InactivatedDate == null)
            {
                return true;
            }

            if (((DateTime)InactivatedDate).Date < ((DateTime)date).Date)
            {
                return true;
            }

            return false;
        }

        partial void OnShortTermGoalChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ShortTermGoal == true)
            {
                LongTermGoal = false;
            }

            if (GoalManager != null)
            {
                GoalManager.FilterDischargeGoals();
            }
        }

        partial void OnLongTermGoalChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (LongTermGoal == true)
            {
                ShortTermGoal = false;
            }

            if (GoalManager != null)
            {
                GoalManager.FilterDischargeGoals();
            }
        }

        public void FilterGoalElements(GoalManager gm)
        {
            GoalManager = gm;

            FilteredGoalElements = new CollectionViewSource();
            FilteredGoalElements.Source = AdmissionGoalElement;
            FilteredGoalElements.Filter += FilteredGoalElements_Filter;
            RaisePropertyChanged("FilteredGoalElements");
            RaisePropertyChanged("HasFilteredGoalElements");
        }

        private void FilteredGoalElements_Filter(object s, FilterEventArgs e)
        {
            if (GoalManager == null)
            {
                e.Accepted = false;
                return;
            }

            var age = e.Item as AdmissionGoalElement;
            if (age.CurrentEncounterGoalElement == null)
            {
                age.CurrentEncounterGoalElement = age.EncounterGoalElement.FirstOrDefault(p => p.EncounterKey == GoalManager.CurrentEncounter.EncounterKey);
            }

            if (GoalManager.PlanHistoryChecked && !age.Superceded)
            {
                e.Accepted = true;
                return;
            }

            if (age.IsNew)
            {
                e.Accepted = true;
                return;
            }

            if (!GoalManager.CurrentEncounter.EncounterGoalElement.Any(eg =>
                    eg.AdmissionGoalElementKey == age.AdmissionGoalElementKey))
            {
                e.Accepted = false;
                return;
            }

            if (!GoalManager.CurrentEncounter.FullValidation && !GoalManager.CurrentEncounter.EncounterIsEval &&
                GoalManager.VisitPlanChecked)
            {
                e.Accepted = IsGoalElementVisibleInVisitMode(age);
                return;
            }

            if (age != null && age.CurrentEncounterGoalElement != null)
            {
                // remove prior Unattainable, Inactivated, Discontinued & Resolved goal elements)
                if (GoalManager.DisciplinePlanChecked || GoalManager.InterdisciplinaryPlanChecked)
                {
                    if (GoalManager.CurrentEncounter.EncounterKey != age.CurrentEncounterGoalElement.EncounterKey)
                    {
                        if (age.Unattainable || age.Inactivated || age.Resolved || age.Discontinued)
                        {
                            e.Accepted = false;
                            return;
                        }
                    }
                }
            }
            else if (age != null && age.CurrentEncounterGoalElement == null)
            {
                e.Accepted = false;
                return;
            }

            e.Accepted = true;
        }

        public bool IsGoalElementPlannedForThisEncounter(AdmissionGoalElement age)
        {
            if (GoalManager == null)
            {
                return false;
            }

            var previous = Admission.Encounter
                .Where(p => !p.IsNew && p.EncounterKey != GoalManager.CurrentEncounter.EncounterKey &&
                            p.EncounterOrTaskStartDateAndTime <=
                            GoalManager.CurrentEncounter.EncounterOrTaskStartDateAndTime &&
                            GoalManager.CurrentEncounter.DisciplineKey == p.DisciplineKey &&
                            !p.IsPlanOfCareOrIsTeamMeetingOrIsOrderEntryOrIsAttempted)
                .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime)
                .FirstOrDefault();

            if (previous == null)
            {
                return false;
            }

            return previous.EncounterGoalElement.Where(p =>
                (p.AdmissionGoalElementKey == age.AdmissionGoalElementKey && p.Planned /*&& !Superceded */
                 || p.Planned && p.AdmissionGoalElement != null &&
                 p.AdmissionGoalElement.HistoryKey == age.HistoryKey && age.HistoryKey != null
                 || p.Planned && p.AdmissionGoalElement != null &&
                 p.AdmissionGoalElement.AdmissionGoalElementKey == age.HistoryKey && age.HistoryKey != null)
                && GoalManager.CurrentEncounter.EncounterGoalElement.Any(eg =>
                    eg.AdmissionGoalElementKey == age.AdmissionGoalElementKey)).Any();
        }

        public bool IsGoalElementVisibleInVisitMode(AdmissionGoalElement age)
        {
            if (GoalManager == null)
            {
                return false;
            }

            var wanttoaccept = false;
            var previous = Admission.Encounter
                .Where(p => !p.IsNew && p.EncounterKey != GoalManager.CurrentEncounter.EncounterKey &&
                            p.EncounterOrTaskStartDateAndTime <=
                            GoalManager.CurrentEncounter.EncounterOrTaskStartDateAndTime &&
                            GoalManager.CurrentEncounter.DisciplineKey == p.DisciplineKey &&
                            !p.IsPlanOfCareOrIsTeamMeetingOrIsOrderEntryOrIsAttempted)
                .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime)
                .FirstOrDefault();

            if (previous == null)
            {
                wanttoaccept = false;
                return wanttoaccept;
            }

            //pull in by default if goal element was planned for the next encounter
            wanttoaccept = previous.EncounterGoalElement.Where(p =>
                (p.AdmissionGoalElementKey == age.AdmissionGoalElementKey &&
                 p.Planned /*&& !Superceded */
                 || p.Planned && p.AdmissionGoalElement != null &&
                 p.AdmissionGoalElement.HistoryKey == age.HistoryKey && age.HistoryKey != null
                 || p.Planned && p.AdmissionGoalElement != null &&
                 p.AdmissionGoalElement.AdmissionGoalElementKey == age.HistoryKey && age.HistoryKey != null)
                && GoalManager.CurrentEncounter.EncounterGoalElement.Any(eg =>
                    eg.AdmissionGoalElementKey == age.AdmissionGoalElementKey)).Any();

            //// These are just used to help debug and determine why an element is or is not appearing
            ////var criteria1 = previous.EncounterGoalElement.Where(p => ((p.AdmissionGoalElementKey == age.AdmissionGoalElementKey && p.Planned == true)));
            ////var criteria2 = previous.EncounterGoalElement.Where(p => (p.Planned == true && p.AdmissionGoalElement != null && p.AdmissionGoalElement.HistoryKey == age.HistoryKey && age.HistoryKey != null));
            ////var criteria3 = previous.EncounterGoalElement.Where(p => (p.Planned == true && p.AdmissionGoalElement != null && p.AdmissionGoalElement.AdmissionGoalElementKey == age.HistoryKey && age.HistoryKey != null));

            var prev_age = previous.EncounterGoalElement.Where(g =>
                    g.AdmissionGoalElement != null && (g.AdmissionGoalElementKey == age.HistoryKey ||
                                                       g.AdmissionGoalElement.HistoryKey == age.HistoryKey &&
                                                       age.HistoryKey != null))
                .OrderByDescending(r => r.AdmissionGoalElementKey)
                .FirstOrDefault();

            // accept it if it was added or discontinued during this encounter, or is marked as modified
            wanttoaccept = wanttoaccept ||
                           age.AddedFromEncounterKey == GoalManager.CurrentEncounter.EncounterKey &&
                           age.HistoryKey == null || age.HasChanges;
            wanttoaccept = wanttoaccept ||
                           age.DiscontinuedFromEncounterKey == GoalManager.CurrentEncounter.EncounterKey &&
                           age.HistoryKey == null || age.HasChanges;
            // accept it if the modifiable fields are different then they were in the previous encounter (needed because of partial save)
            wanttoaccept = wanttoaccept || GoalManager.CurrentEncounter.EncounterGoalElement.Any(ege =>
                ege.AdmissionGoalElementKey == age.AdmissionGoalElementKey
                && (ege.Planned || ege.Addressed || prev_age != null && prev_age.AdmissionGoalElement != null
                && (prev_age.AdmissionGoalElement.Unattainable != age.Unattainable ||
                    prev_age.AdmissionGoalElement.Inactivated != age.Inactivated ||
                    prev_age.AdmissionGoalElement.Resolved != age.Resolved ||
                    prev_age.AdmissionGoalElement.Discontinued != age.Discontinued ||
                    prev_age.AdmissionGoalElement.GoalElementText != age.GoalElementText)));

            return wanttoaccept;
        }

        public AdmissionGoal CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newgoal = (AdmissionGoal)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newgoal);
            if (newgoal.HistoryKey == null)
            {
                newgoal.HistoryKey = AdmissionGoalKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newgoal;
        }

        public void FormatGoalText()
        {
            if (QuestionTemplate != null)
            {
                var original = GoalCache.GetGoalFromKey(GoalKey).LongDescription;
                if (QuestionTemplate.StartsWith("PTLevelofAssist") && original.Contains("{4}"))
                {
                    var QuestionValuetoUse = FormatParameter;
                    var FormatValuetoUse = FormatParameter;

                    if (!string.IsNullOrEmpty(QuestionResponse))
                    {
                        QuestionValuetoUse = CodeLookupCache.GetCodeFromKey(Convert.ToInt32(QuestionResponse));
                    }
                    else
                    {
                        QuestionValuetoUse = string.Empty;
                    }

                    if (!string.IsNullOrEmpty(FormatParameter))
                    {
                        FormatValuetoUse = CodeLookupCache.GetCodeFromKey(Convert.ToInt32(FormatParameter));
                    }
                    else
                    {
                        FormatValuetoUse = string.Empty;
                    }

                    if (FormatValuetoUse.Equals("Independent"))
                    {
                        FormatSubParameter = string.Empty;
                    }

                    if (!string.IsNullOrEmpty(QuestionResponse) && !string.IsNullOrEmpty(FormatParameter) &&
                        !string.IsNullOrEmpty(TimeFrame) && !string.IsNullOrEmpty(QuestionReason))
                    {
                        if (original.Contains("{5}"))
                        {
                            GoalText = string.Format(original, QuestionSubResponse, QuestionValuetoUse,
                                FormatSubParameter, FormatValuetoUse, QuestionReason, TimeFrame);
                        }
                        else
                        {
                            GoalText = string.Format(original, QuestionSubResponse, QuestionValuetoUse,
                                FormatSubParameter, FormatValuetoUse, TimeFrame);
                        }
                    }
                }
                else if ((QuestionTemplate.Equals("Text") || QuestionTemplate.Equals("CodeLookupMulti")) &&
                         original.Contains("{3}"))
                {
                    if (!string.IsNullOrEmpty(QuestionResponse) && !string.IsNullOrEmpty(FormatParameter) &&
                        !string.IsNullOrEmpty(TimeFrame) && !string.IsNullOrEmpty(QuestionReason))
                    {
                        GoalText = string.Format(original, QuestionResponse, FormatParameter, QuestionReason,
                            TimeFrame);
                    }
                }
                else if (QuestionTemplate.Equals("Text") && original.Contains("{2}"))
                {
                    if (!string.IsNullOrEmpty(QuestionResponse) && !string.IsNullOrEmpty(FormatParameter) &&
                        !string.IsNullOrEmpty(TimeFrame))
                    {
                        GoalText = string.Format(original, QuestionResponse, FormatParameter, TimeFrame);
                    }
                }
                else if (QuestionTemplate.Equals("Pain") && original.Contains("{2}"))
                {
                    if (!string.IsNullOrEmpty(QuestionSubResponse) && !string.IsNullOrEmpty(FormatParameter) &&
                        !string.IsNullOrEmpty(TimeFrame))
                    {
                        GoalText = string.Format(original, QuestionSubResponse, FormatParameter, TimeFrame);
                    }
                }
                else if (QuestionTemplate.Equals("DisciplineRefer"))
                {
                    GoalText = string.Format(original, QuestionResponse);
                }
                else if (QuestionTemplate.Equals("DateOnly"))
                {
                    if (original.Contains("{1}"))
                    {
                        if (!string.IsNullOrEmpty(TimeFrame) && !string.IsNullOrEmpty(QuestionReason))
                        {
                            GoalText = string.Format(original, QuestionReason, TimeFrame);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(TimeFrame))
                        {
                            GoalText = string.Format(original, TimeFrame);
                        }
                    }
                }
                else
                {
                    QuestionTemplate = string.Empty;
                    GoalText = original;
                }

                if (string.IsNullOrWhiteSpace(GoalText))
                {
                    GoalText = original;
                }
            }
        }

        partial void OnRequiredForDischargeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (GoalManager != null)
            {
                GoalManager.FilterDischargeGoals();
            }
        }

        partial void OnResolvedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Resolved)
            {
                ResolvedDate = DateTime.Now.Date;
                ResolvedBy = WebContext.Current.User.MemberID;
            }
            else
            {
                ResolvedDate = null;
                ResolvedBy = null;
            }

            RaisePropertyChanged("GoalStatus");
            RaisePropertyChanged("FinalStatusAndDate");
            RaisePropertyChanged("ShowUnattainable");
            RaisePropertyChanged("ShowInactivated");
        }

        partial void OnDiscontinuedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Discontinued)
            {
                if (GoalManager == null)
                {
                    return;
                }

                DiscontinuedDate = DateTime.Now.Date;
                DiscontinuedBy = WebContext.Current.User.MemberID;
                DiscontinuedFromEncounterKey = GoalManager == null || GoalManager.CurrentEncounter == null
                    ? (int?)null
                    : GoalManager.CurrentEncounter.EncounterKey;
            }
            else
            {
                DiscontinuedDate = null;
                DiscontinuedBy = null;
                DiscontinuedFromEncounterKey = null;
                ;
            }

            RaisePropertyChanged("DiscontinuedThisEncounter");
            RaisePropertyChanged("DiscontinuedEncounterBy");
            RaisePropertyChanged("DiscontinuedEncounterDate");
            RaisePropertyChanged("GoalStatus");
            RaisePropertyChanged("ShowUnattainable");
            RaisePropertyChanged("ShowInactivated");
        }
        public bool ShowUnattainable => !Resolved && !Discontinued && !Inactivated;
        public bool ShowInactivated => !Resolved && !Discontinued && !Unattainable;

        private bool _PreviousUnattainable = false;
        public bool PreviousUnattainable
        {
            get { return _PreviousUnattainable; }
            set
            {
                _PreviousUnattainable = value;
                RaisePropertyChanged("PreviousUnattainable");
            }
        }

        private bool _PreviousInactivated = false;
        public bool PreviousInactivated
        {
            get { return _PreviousInactivated; }
            set
            {
                _PreviousInactivated = value;
                RaisePropertyChanged("PreviousInactivated");
            }
        }

        partial void OnUnattainableChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Unattainable)
            {
                UnattainableDate = DateTime.Now.Date;
                UnattainableBy = WebContext.Current.User.MemberID;
            }
            else
            {
                UnattainableReason = 0;
                UnattainableDate = null;
                UnattainableBy = null;

                foreach (var age in AdmissionGoalElement)
                {
                    if (age.CurrentEncounterGoalElement != null && age.InheritedUnattainable)
                    {
                        age.Unattainable = false;
                        age.InheritedUnattainable = false;
                    }
                }
            }

            RaisePropertyChanged("GoalStatus");
            RaisePropertyChanged("FinalStatusAndDate");
            RaisePropertyChanged("ShowUnattainable");
            RaisePropertyChanged("ShowInactivated");
        }

        partial void OnUnattainableReasonChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (UnattainableReason.HasValue && UnattainableReason > 0)
            {
                foreach (var age in AdmissionGoalElement)
                {
                    if (age.CurrentEncounterGoalElement != null && !age.Resolved && !age.Unattainable &&
                        !age.Discontinued)
                    {
                        age.InheritedUnattainable = true;
                        age.Unattainable = true;
                        age.UnattainableReason = UnattainableReason;
                    }
                    else if (age.InheritedUnattainable)
                    {
                        age.UnattainableReason = UnattainableReason;
                    }
                }
            }
        }

        partial void OnInactivatedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Inactivated)
            {
                InactivatedDate = DateTime.Now.Date;
                InactivatedBy = WebContext.Current.User.MemberID;
            }
            else
            {
                InactivatedReason = null;
                InactivatedDate = null;
                InactivatedBy = null;

                foreach (var age in AdmissionGoalElement)
                {
                    if (age.CurrentEncounterGoalElement != null && age.InheritedInactivated)
                    {
                        age.Inactivated = false;
                        age.InheritedInactivated = false;
                    }
                }
            }

            RaisePropertyChanged("GoalStatus");
            RaisePropertyChanged("FinalStatusAndDate");
            RaisePropertyChanged("ShowUnattainable");
            RaisePropertyChanged("ShowInactivated");
        }

        partial void OnInactivatedReasonChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(InactivatedReason) == false)
            {
                foreach (var age in AdmissionGoalElement)
                {
                    if (age.CurrentEncounterGoalElement != null && !age.Resolved && !age.Unattainable && !age.Inactivated && !age.Discontinued)
                    {
                        age.InheritedInactivated = true;
                        age.Inactivated = true;
                        age.InactivatedReason = InactivatedReason;
                    }
                    else if (age.InheritedInactivated)
                    {
                        age.InactivatedReason = InactivatedReason;
                    }
                }
            }
        }

        #region Properties

        private bool _ReEvaluate;

        public bool ReEvaluate
        {
            get { return _ReEvaluate; }
            set
            {
                _ReEvaluate = value;
                RaisePropertyChanged("ReEvaluate");
            }
        }

        private string _QuestionLabel;

        public string QuestionLabel
        {
            get { return _QuestionLabel; }
            set
            {
                _QuestionLabel = value;
                RaisePropertyChanged("QuestionLabel");
            }
        }

        private string _QuestionTemplate;

        public string QuestionTemplate
        {
            get { return _QuestionTemplate; }
            set
            {
                _QuestionTemplate = value;
                RaisePropertyChanged("QuestionTemplate");
            }
        }

        public string QuestionTemplateEdit => QuestionTemplate == "Pain" ? null : QuestionTemplate;

        private string _QuestionLookupType;

        public string QuestionLookupType
        {
            get { return _QuestionLookupType; }
            set
            {
                _QuestionLookupType = value;
                RaisePropertyChanged("QuestionLookupType");
            }
        }

        private string _QuestionResponse;

        public string QuestionResponse
        {
            get { return _QuestionResponse; }
            set
            {
                if (_QuestionResponse != value)
                {
                    _QuestionResponse = value;
                    RaisePropertyChanged("QuestionResponse");
                    FormatGoalText();
                }
            }
        }

        private string _QuestionSubResponse;

        public string QuestionSubResponse
        {
            get { return _QuestionSubResponse; }
            set
            {
                if (_QuestionSubResponse != value)
                {
                    _QuestionSubResponse = value;
                    RaisePropertyChanged("QuestionSubResponse");
                    FormatGoalText();
                }
            }
        }

        private string _QuestionReason;

        public string QuestionReason
        {
            get { return _QuestionReason; }
            set
            {
                if (_QuestionReason != value)
                {
                    _QuestionReason = value;
                    RaisePropertyChanged("QuestionReason");
                    FormatGoalText();
                }
            }
        }

        private int? _ResponseKey;

        public int? ResponseKey
        {
            get { return _ResponseKey; }
            set
            {
                _ResponseKey = value;
                RaisePropertyChanged("ResponseKey");
            }
        }

        private string _FormatParameter;

        public string FormatParameter
        {
            get { return _FormatParameter; }
            set
            {
                if (_FormatParameter != value)
                {
                    _FormatParameter = value;
                    RaisePropertyChanged("FormatParameter");
                    FormatGoalText();
                }
            }
        }

        private string _FormatSubParameter;

        public string FormatSubParameter
        {
            get { return _FormatSubParameter; }
            set
            {
                if (_FormatSubParameter != value)
                {
                    _FormatSubParameter = value;
                    RaisePropertyChanged("FormatSubParameter");
                    FormatGoalText();
                }
            }
        }

        private string _TimeFrame;

        public string TimeFrame
        {
            get { return _TimeFrame; }
            set
            {
                if (_TimeFrame != value)
                {
                    _TimeFrame = Convert.ToDateTime(value).ToShortDateString();
                    RaisePropertyChanged("TimeFrame");
                    FormatGoalText();
                }
            }
        }

        private bool _TextChanged;

        public bool TextChanged
        {
            get { return _TextChanged; }
            set
            {
                _TextChanged = value;
                RaisePropertyChanged("TextChanged");
            }
        }

        public bool DeleteEnabled
        {
            get
            {
                // if a brand new add in a post edit status - allow delete until the first OK
                if (GoalManager == null || GoalManager.CurrentEncounter == null)
                {
                    return false;
                }

                if (GoalManager.CurrentEncounter.EncounterStatus > (int)EncounterStatusType.Edit &&
                    GoalManager.CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed && IsNew &&
                    AddedFromEncounterKey == GoalManager.CurrentEncounter.EncounterKey && !Superceded)
                {
                    return true;
                }

                if (GoalManager.CurrentEncounter.EncounterStatus > (int)EncounterStatusType.Edit)
                {
                    return false;
                }

                return (IsNew || AddedFromEncounterKey == GoalManager.CurrentEncounter.EncounterKey) && !Superceded &&
                       !Resolved && !Unattainable && !Inactivated && !Discontinued;
            }
        }

        public bool IsAttempted => CurrentEncounterGoal != null && CurrentEncounterGoal.Encounter != null &&
                                   CurrentEncounterGoal.Encounter.FormKey != null && DynamicFormCache
                                       .GetFormByKey((int)CurrentEncounterGoal.Encounter.FormKey).IsAttempted;

        public bool IsOrderEntry => CurrentEncounterGoal != null && CurrentEncounterGoal.Encounter != null &&
                                    CurrentEncounterGoal.Encounter.FormKey != null && DynamicFormCache
                                        .GetFormByKey((int)CurrentEncounterGoal.Encounter.FormKey).IsOrderEntry;

        public bool IsPlanOfCare => CurrentEncounterGoal != null && CurrentEncounterGoal.Encounter != null &&
                                    CurrentEncounterGoal.Encounter.FormKey != null && DynamicFormCache
                                        .GetFormByKey((int)CurrentEncounterGoal.Encounter.FormKey).IsPlanOfCare;

        public bool IsTeamMeeting => CurrentEncounterGoal != null && CurrentEncounterGoal.Encounter != null &&
                                     CurrentEncounterGoal.Encounter.FormKey != null && DynamicFormCache
                                         .GetFormByKey((int)CurrentEncounterGoal.Encounter.FormKey).IsTeamMeeting;

        public bool IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry => IsPlanOfCare || IsTeamMeeting || IsOrderEntry;

        public bool IsPlanOfCareOrIsTeamMeetingOrIsOrderEntryOrIsAttempted =>
            IsPlanOfCare || IsTeamMeeting || IsOrderEntry || IsAttempted;

        public bool CanEdit
        {
            get
            {
                if (CurrentEncounterGoal != null && CurrentEncounterGoal.Encounter != null &&
                    CurrentEncounterGoal.Encounter.Inactive)
                {
                    return false;
                }

                if (IsAttempted)
                {
                    return false;
                }

                if (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry)
                {
                    return AllowEdit || MustEdit ? true : false;
                }

                var canEdit = !Superceded && ServiceTypesMatch;

                if (canEdit == false)
                {
                    return false;
                }

                return AllowEdit || MustEdit ? true : false;
            }
        }

        public bool CanManipulateGoalElements
        {
            get
            {
                if (CurrentEncounterGoal != null && CurrentEncounterGoal.Encounter != null &&
                    CurrentEncounterGoal.Encounter.Inactive)
                {
                    return false;
                }

                if (IsAttempted)
                {
                    return false;
                }

                if (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry)
                {
                    return true;
                }

                var canEdit = !Superceded && ServiceTypesMatch;
                return canEdit;
            }
        }

        public bool CanEditNonText
        {
            get
            {
                if (CurrentEncounterGoal != null && CurrentEncounterGoal.Encounter != null &&
                    CurrentEncounterGoal.Encounter.Inactive)
                {
                    return false;
                }

                if (IsAttempted)
                {
                    return false;
                }

                if (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry)
                {
                    return true;
                }

                var canEdit = !Superceded && ServiceTypesMatch;

                return canEdit;
            }
        }

        public bool ShowDischargePlan
        {
            get
            {
                if (IsOrderEntry)
                {
                    return false;
                }

                if (Admission == null)
                {
                    return false;
                }

                if (Admission.IsHomeHealth == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool CanEditDischargePlan
        {
            get
            {
                if (ShowDischargePlan == false)
                {
                    return false;
                }

                if (ServiceTypesMatch == false)
                {
                    return false;
                }

                if (CurrentEncounterGoal == null)
                {
                    return true;
                }

                if (CurrentEncounterGoal.EncounterIsAssistant)
                {
                    return false;
                }

                return true;
            }
        }

        private Goal _Goal;

        public Goal Goal
        {
            get
            {
                if (_Goal == null)
                {
                    _Goal = GoalCache.GetGoalFromKey(GoalKey);
                }

                return _Goal;
            }
        }

        public bool ServiceTypesMatch
        {
            get
            {
                var matchingST = false;
                if (CurrentEncounterGoal == null && EncounterGoal != null)
                {
                    CurrentEncounterGoal = EncounterGoal.FirstOrDefault();
                }

                if (CurrentEncounterGoal == null || Goal == null)
                {
                    // must have been added in this encounter.
                    matchingST = true;
                }
                else
                {
                    if (CurrentEncounterGoal != null && CurrentEncounterGoal.Encounter != null)
                    {
                        matchingST = Goal.DisciplineInGoal.Where(dg =>
                                dg.DisciplineKey ==
                                ServiceTypeCache.GetDisciplineKey((int)CurrentEncounterGoal.Encounter.ServiceTypeKey)).Any();
                    }

                    // If none assigned it's good for all disciplines
                    if (Goal.DisciplineInGoal.Any() == false)
                    {
                        matchingST = true;
                    }

                    matchingST = matchingST || AddedFromEncounterKey == CurrentEncounterGoal.EncounterKey;

                    // TODO:check superceded On parent
                }

                return matchingST;
            }
        }

        #endregion
    }
}