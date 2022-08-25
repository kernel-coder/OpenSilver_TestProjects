#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Occasional;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionGoalElement
    {
        private EncounterGoalElement _CurrentEncounterGoalElement;
        private GoalElement _GoalElement;

        private bool _InheritedUnattainable;

        private bool _InheritedInactivated;

        private bool _POCProtected;

        private bool _TextChanged;
        private bool aCloning;  // changed from isCloning for RIA alphabetical reasons

        public new bool HasValidationErrors
        {
            get
            {
                if (ValidationErrors != null && ValidationErrors.Any())
                {
                    return true;
                }

                if (CurrentEncounterGoalElement != null && CurrentEncounterGoalElement.ValidationErrors != null &&
                    CurrentEncounterGoalElement.ValidationErrors.Any(e =>
                        e.MemberNames.Contains("PlannedButUnaddressedGoalElement")))
                {
                    return true;
                }

                return false;
            }
        }

        public string ValidationSummary
        {
            get
            {
                var CR = char.ToString('\r');
                var vs = "";
                if (ValidationErrors != null)
                {
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
                }

                if (CurrentEncounterGoalElement != null && CurrentEncounterGoalElement.ValidationErrors != null)
                {
                    foreach (var vr in CurrentEncounterGoalElement.ValidationErrors)
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
                }

                return string.IsNullOrWhiteSpace(vs) ? null : vs;
            }
        }

        public string DisciplineCode
        {
            get
            {
                var ge = GoalCache.GetGoalElementByKey(GoalElementKey);
                if (ge == null)
                {
                    return "Multi";
                }

                var dList = ge.DisciplineInGoalElement?.ToList();
                if (dList == null || dList.Count() != 1)
                {
                    return "Multi";
                }

                var disciplineKey = dList.Select(dige => dige.DisciplineKey).First();
                var d = DisciplineCache.GetDisciplineFromKey(disciplineKey);
                if (d == null || string.IsNullOrWhiteSpace(d.Code))
                {
                    return "Multi";
                }

                return d.Code;
            }
        }

        public string GoalElementDescription
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GoalElementText) == false)
                {
                    return GoalElementText;
                }

                var ge = GoalCache.GetGoalElementByKey(GoalElementKey);
                if (ge != null && string.IsNullOrWhiteSpace(ge.LongDescription) == false)
                {
                    return ge.LongDescription;
                }

                if (ge != null && string.IsNullOrWhiteSpace(ge.ShortDescription) == false)
                {
                    return ge.ShortDescription;
                }

                return null;
            }
        }

        public string GoalElementDescriptionPOC
        {
            get
            {
                var ge = GoalCache.GetGoalElementByKey(GoalElementKey);
                if (ge != null && string.IsNullOrWhiteSpace(ge.POCOverrideText) == false)
                {
                    return ge.POCOverrideText;
                }

                if (string.IsNullOrWhiteSpace(GoalElementText) == false)
                {
                    return GoalElementText;
                }

                if (ge != null && string.IsNullOrWhiteSpace(ge.LongDescription) == false)
                {
                    return ge.LongDescription;
                }

                if (ge != null && string.IsNullOrWhiteSpace(ge.ShortDescription) == false)
                {
                    return ge.ShortDescription;
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

        public EncounterGoalElement CurrentEncounterGoalElement
        {
            get { return _CurrentEncounterGoalElement; }
            set
            {
                _CurrentEncounterGoalElement = value;
                RaisePropertyChanged("CurrentEncounterGoalElement");
            }
        }

        public bool CurrentEncounterIsAssistant => CurrentEncounterGoalElement == null
            ? true
            : CurrentEncounterGoalElement.EncounterIsAssistant;

        public bool DeleteEnabled
        {
            get
            {
                if (CurrentEncounterGoalElement == null)
                {
                    return false;
                }

                // if a brand new add in a post edit status - allow delete until the first OK
                if (CurrentEncounterGoalElement.Encounter.EncounterStatus > (int)EncounterStatusType.Edit &&
                    CurrentEncounterGoalElement.Encounter.EncounterStatus != (int)EncounterStatusType.Completed &&
                    IsNew && AddedFromEncounterKey == CurrentEncounterGoalElement.Encounter.EncounterKey &&
                    !Superceded)
                {
                    return true;
                }

                if (CurrentEncounterGoalElement.Encounter.EncounterStatus > (int)EncounterStatusType.Edit)
                {
                    return false;
                }

                return (IsNew || AddedFromEncounterKey == CurrentEncounterGoalElement.EncounterKey) &&
                       !Resolved && !Unattainable && !Inactivated && !Discontinued && !Superceded;
            }
        }

        public bool IsAttempted =>
            CurrentEncounterGoalElement != null && CurrentEncounterGoalElement.Encounter != null
                                                && CurrentEncounterGoalElement.Encounter.FormKey != null &&
                                                DynamicFormCache
                                                    .GetFormByKey((int)CurrentEncounterGoalElement.Encounter.FormKey)
                                                    .IsAttempted;

        public bool IsOrderEntry =>
            CurrentEncounterGoalElement != null && CurrentEncounterGoalElement.Encounter != null
                                                && CurrentEncounterGoalElement.Encounter.FormKey != null &&
                                                DynamicFormCache
                                                    .GetFormByKey((int)CurrentEncounterGoalElement.Encounter.FormKey)
                                                    .IsOrderEntry;

        public bool IsPlanOfCare =>
            CurrentEncounterGoalElement != null && CurrentEncounterGoalElement.Encounter != null
                                                && CurrentEncounterGoalElement.Encounter.FormKey != null &&
                                                DynamicFormCache
                                                    .GetFormByKey((int)CurrentEncounterGoalElement.Encounter.FormKey)
                                                    .IsPlanOfCare;

        public bool IsTeamMeeting =>
            CurrentEncounterGoalElement != null && CurrentEncounterGoalElement.Encounter != null
                                                && CurrentEncounterGoalElement.Encounter.FormKey != null &&
                                                DynamicFormCache
                                                    .GetFormByKey((int)CurrentEncounterGoalElement.Encounter.FormKey)
                                                    .IsTeamMeeting;

        public bool IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry => IsPlanOfCare || IsTeamMeeting || IsOrderEntry;

        public bool IsPlanOfCareOrIsTeamMeetingOrIsOrderEntryOrIsAttempted =>
            IsPlanOfCare || IsTeamMeeting || IsOrderEntry || IsAttempted;

        public bool IsPlanOfCareOrIsTeamMeeting => IsPlanOfCare || IsTeamMeeting;

        public string POCOvrTextOrGoalElementText
        {
            get
            {
                var retString = GoalElementText;
                if (IsPlanOfCare)
                {
                    retString = string.IsNullOrEmpty(GoalElementFromCache.POCOverrideText)
                        ? GoalElementText
                        : GoalElementFromCache.POCOverrideText;
                }

                return retString;
            }
        }

        public bool EncounterIsInEdit
        {
            get
            {
                if (AdmissionGoal == null)
                {
                    return false;
                }

                if (AdmissionGoal.GoalManager == null)
                {
                    return false;
                }

                if (AdmissionGoal.GoalManager.CurrentEncounter == null)
                {
                    return false;
                }

                if (AdmissionGoal.GoalManager.CurrentEncounter.EncounterBy == WebContext.Current.User.MemberID &&
                    (AdmissionGoal.GoalManager.CurrentEncounter.PreviousEncounterStatus ==
                     (int)EncounterStatusType.Edit ||
                     AdmissionGoal.GoalManager.CurrentEncounter.PreviousEncounterStatus ==
                     (int)EncounterStatusType.CoderReviewEdit ||
                     AdmissionGoal.GoalManager.CurrentEncounter.PreviousEncounterStatus ==
                     (int)EncounterStatusType.OASISReviewEdit ||
                     AdmissionGoal.GoalManager.CurrentEncounter.PreviousEncounterStatus ==
                     (int)EncounterStatusType.OASISReviewEditRR))
                {
                    return true;
                }

                if (AdmissionGoal.GoalManager.CurrentEncounter.UserIsPOCOrderReviewerAndInPOCOrderReview)
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
                if (AdmissionGoal == null)
                {
                    return false;
                }

                if (AdmissionGoal.GoalManager == null)
                {
                    return false;
                }

                if (AdmissionGoal.GoalManager.CurrentEncounter == null)
                {
                    return false;
                }

                if (AdmissionGoal.GoalManager.CurrentEncounter.EncounterKey == AddedFromEncounterKey)
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
                if (AdmissionGoal == null)
                {
                    return false;
                }

                if (AdmissionGoal.GoalManager == null)
                {
                    return false;
                }

                if (AdmissionGoal.GoalManager.CurrentEncounter == null)
                {
                    return false;
                }

                if (AdmissionGoal.GoalManager.CurrentEncounter.EncounterKey == DiscontinuedFromEncounterKey)
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
                if (AdmissionGoal == null)
                {
                    return null;
                }

                if (AdmissionGoal.GoalManager == null)
                {
                    return null;
                }

                if (AdmissionGoal.GoalManager.CurrentEncounter == null)
                {
                    return null;
                }

                if (AdmissionGoal.GoalManager.CurrentEncounter.EncounterKey != DiscontinuedFromEncounterKey)
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
                if (AdmissionGoal == null)
                {
                    return null;
                }

                if (AdmissionGoal.GoalManager == null)
                {
                    return null;
                }

                if (AdmissionGoal.GoalManager.CurrentEncounter == null)
                {
                    return null;
                }

                if (AdmissionGoal.GoalManager.CurrentEncounter.EncounterKey != DiscontinuedFromEncounterKey)
                {
                    return null;
                }

                return DiscontinuedDate == null ? (DateTime?)null : ((DateTimeOffset)DiscontinuedDate).Date;
            }
        }

        public bool CanEditGoalElementText
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

        public GoalElement GoalElement
        {
            get
            {
                if (_GoalElement == null)
                {
                    _GoalElement = GoalCache.GetGoalElementByKey(GoalElementKey);
                }

                return _GoalElement;
            }
        }

        public bool CanEdit
        {
            get
            {
                if (AdmissionGoal != null && AdmissionGoal.GoalManager != null &&
                    AdmissionGoal.GoalManager.CurrentEncounter != null &&
                    AdmissionGoal.GoalManager.CurrentEncounter.Inactive)
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

                var matchingST = false;
                var addedThisEncounter = false;
                if (CurrentEncounterGoalElement == null && EncounterGoalElement != null)
                {
                    CurrentEncounterGoalElement = EncounterGoalElement.FirstOrDefault();
                }

                if (CurrentEncounterGoalElement == null || GoalElement == null)
                {
                    // must have been added in this encounter.
                    matchingST = true;
                }
                else
                {
                    if (AddedFromEncounterKey == _CurrentEncounterGoalElement.EncounterKey ||
                        _CurrentEncounterGoalElement.EncounterKey == null ||
                        CurrentEncounterGoalElement.Encounter == null)
                    {
                        matchingST = true;
                        addedThisEncounter = true;
                    }
                    else
                    {
                        matchingST = GoalElement.DisciplineInGoalElement
                            .Where(dg => dg.DisciplineKey == ServiceTypeCache.GetDisciplineKey((int)CurrentEncounterGoalElement.Encounter.ServiceTypeKey)).Any();

                        // If none assigned it's good for all disciplines
                        if (GoalElement.DisciplineInGoalElement.Any() == false)
                        {
                            matchingST = true;
                        }

                        matchingST = matchingST || AddedFromEncounterKey == CurrentEncounterGoalElement.EncounterKey;
                    }

                    // TODO:check superceded On parent
                }

                var canEdit = addedThisEncounter
                    ? true
                    : !Superceded && matchingST && (AdmissionGoal == null
                        ? true
                        : AdmissionGoal.CanManipulateGoalElements && (AllowEdit || MustEdit));
                if (canEdit == false)
                {
                    return false;
                }

                return AllowEdit || MustEdit ? true : false;
            }
        }

        public bool CanEditNonText
        {
            get
            {
                if (AdmissionGoal != null && AdmissionGoal.GoalManager != null &&
                    AdmissionGoal.GoalManager.CurrentEncounter != null &&
                    AdmissionGoal.GoalManager.CurrentEncounter.Inactive)
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

                var matchingST = false;
                var addedThisEncounter = false;
                if (CurrentEncounterGoalElement == null && EncounterGoalElement != null)
                {
                    CurrentEncounterGoalElement = EncounterGoalElement.FirstOrDefault();
                }

                if (CurrentEncounterGoalElement == null || GoalElement == null)
                {
                    // must have been added in this encounter.
                    matchingST = true;
                }
                else
                {
                    if (AddedFromEncounterKey == _CurrentEncounterGoalElement.EncounterKey ||
                        _CurrentEncounterGoalElement.EncounterKey == null ||
                        CurrentEncounterGoalElement.Encounter == null)
                    {
                        matchingST = true;
                        addedThisEncounter = true;
                    }
                    else
                    {
                        matchingST = GoalElement.DisciplineInGoalElement.Where(dg => dg.DisciplineKey
                            == ServiceTypeCache.GetDisciplineKey((int)CurrentEncounterGoalElement.Encounter
                                .ServiceTypeKey)).Any();

                        // If none assigned it's good for all disciplines
                        if (GoalElement.DisciplineInGoalElement.Any() == false)
                        {
                            matchingST = true;
                        }

                        matchingST = matchingST || AddedFromEncounterKey == CurrentEncounterGoalElement.EncounterKey;
                    }
                    //TODO:check superceded On parent
                }

                var canEdit = addedThisEncounter
                    ? true
                    : !Superceded && matchingST && (AdmissionGoal == null ? true : AdmissionGoal.CanEditNonText);
                return canEdit;
            }
        }

        public bool CanEditAddressed
        {
            get
            {
                if (IsAttempted)
                {
                    return false;
                }

                if (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry)
                {
                    return true;
                }

                var matchingST = false;
                var addedThisEncounter = false;
                if (CurrentEncounterGoalElement == null && EncounterGoalElement != null)
                {
                    CurrentEncounterGoalElement = EncounterGoalElement.FirstOrDefault();
                }

                if (CurrentEncounterGoalElement == null || GoalElement == null)
                {
                    // must have been added in this encounter.
                    matchingST = true;
                }
                else
                {
                    if (AddedFromEncounterKey == _CurrentEncounterGoalElement.EncounterKey ||
                        _CurrentEncounterGoalElement.EncounterKey == null ||
                        CurrentEncounterGoalElement.Encounter == null)
                    {
                        matchingST = true;
                        addedThisEncounter = true;
                    }
                    else
                    {
                        matchingST = GoalElement.DisciplineInGoalElement.Where(dg => dg.DisciplineKey
                            == ServiceTypeCache.GetDisciplineKey((int)CurrentEncounterGoalElement.Encounter
                                .ServiceTypeKey)).Any();

                        // If none assigned it's good for all disciplines
                        if (GoalElement.DisciplineInGoalElement.Any() == false)
                        {
                            matchingST = true;
                        }

                        matchingST = matchingST || AddedFromEncounterKey == CurrentEncounterGoalElement.EncounterKey;
                    }

                    // TODO:check superceded On parent
                }

                var canEdit = addedThisEncounter
                    ? true
                    : matchingST && (AdmissionGoal == null ? true : AdmissionGoal.CanEditNonText);
                return canEdit;
            }
        }

        public bool AllowEdit
        {
            get
            {
                var g = GoalCache.GetGoalElementByKey(GoalElementKey);
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
                var g = GoalCache.GetGoalElementByKey(GoalElementKey);
                if (g == null)
                {
                    return false;
                }

                return g.MustEdit;
            }
        }

        public bool IsGoalElementTextParameterized
        {
            get
            {
                if (MustEdit == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GoalElementText))
                {
                    return false;
                }

                for (var i = 2; i < 11; i++)
                    if (GoalElementText.Contains("{" + i.ToString().Trim() + "}"))
                    {
                        return true;
                    }

                return false;
            }
        }

        public bool IsGoalElementTextChangedFromLongDescription
        {
            get
            {
                if (MustEdit == false)
                {
                    return true;
                }

                var g = GoalCache.GetGoalElementByKey(GoalElementKey);
                if (g == null)
                {
                    return true;
                }

                var longDescription = string.IsNullOrWhiteSpace(g.LongDescription) ? "" : g.LongDescription.Trim();
                var goalElementText = string.IsNullOrWhiteSpace(GoalElementText) ? "" : GoalElementText.Trim();
                return longDescription != goalElementText;
            }
        }

        public bool CanPlan
        {
            get
            {
                if (CurrentEncounterGoalElement == null)
                {
                    return false;
                }

                if (IsAttempted)
                {
                    return false;
                }

                if (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry)
                {
                    return false;
                }

                return !Resolved && !Unattainable && !Inactivated && !Discontinued;
            }
        }

        public bool TextChanged
        {
            get { return _TextChanged; }
            set
            {
                _TextChanged = value;
                RaisePropertyChanged("TextChanged");
            }
        }

        public bool POCProtected
        {
            get { return _POCProtected || !CanEdit; }
            set
            {
                _POCProtected = value;
                RaisePropertyChanged("POCProtected");
            }
        }

        public bool InheritedUnattainable
        {
            get { return _InheritedUnattainable || !CanEditNonText; }
            set
            {
                _InheritedUnattainable = value;
                RaisePropertyChanged("InheritedUnattainable");
            }
        }

        public bool InheritedInactivated
        {
            get { return _InheritedInactivated || !CanEditNonText; }
            set
            {
                _InheritedInactivated = value;
                RaisePropertyChanged("InheritedInactivated");
            }
        }
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

        public string GoalElementStatus
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

                if (CurrentEncounterGoalElement != null && CurrentEncounterGoalElement.Addressed)
                {
                    return "Addressed";
                }

                if (CurrentEncounterGoalElement != null && CurrentEncounterGoalElement.Planned)
                {
                    return "Planned";
                }

                return null;
            }
        }

        public bool IsGoalElementPlannedForThisEncounter
        {
            get
            {
                if (AdmissionGoal == null)
                {
                    return false;
                }

                return AdmissionGoal.IsGoalElementPlannedForThisEncounter(this);
            }
        }

        public int PlannedForThisEncounterOrAddressed
        {
            get
            {
                if (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntryOrIsAttempted)
                {
                    return 0;
                }

                if (CurrentEncounterGoalElement != null && CurrentEncounterGoalElement.Addressed)
                {
                    return 2;
                }

                if (AdmissionGoal.IsGoalElementPlannedForThisEncounter(this))
                {
                    return 1;
                }

                ;
                return 0;
            }
        }

        public bool ShowResolvedColumn
        {
            get
            {
                if (AdmissionGoal == null)
                {
                    return false;
                }

                if (AdmissionGoal.GoalManager == null)
                {
                    return false;
                }

                return AdmissionGoal.GoalManager.ShowResolvedColumn;
            }
        }

        public bool ShowResolved => !Unattainable && !Inactivated && !Discontinued;

        public bool ShowUnattainable => !Resolved && !Discontinued && !Inactivated;
        public bool ShowInactivated => !Resolved && !Discontinued && !Unattainable;

        public bool ShowDiscontinued
        {
            get
            {
                if (Resolved || Unattainable || Inactivated)
                {
                    return false;
                }

                var ge = GoalCache.GetGoalElementByKey(GoalElementKey);
                if (ge == null || ge.Orders == false)
                {
                    return false;
                }

                if (IsNew || CurrentEncounterGoalElement == null ||
                    AddedFromEncounterKey == CurrentEncounterGoalElement.EncounterKey)
                {
                    return false;
                }

                if (AdmissionGoal == null)
                {
                    return false;
                }

                if (AdmissionGoal.GoalManager == null)
                {
                    return false;
                }
                if (AdmissionGoal.GoalManager.IsTeamMeeting) return true;

                return AdmissionGoal.GoalManager.IsOrderEntryOrOrderEntryVO;
            }
        }

        public GoalElement GoalElementFromCache => GoalCache.GetGoalElementByKey(GoalElementKey);

        public RelayCommand<AdmissionGoalElement> RemoveGoalElementCommand { get; set; }

        public List<int> GoalElementDisciplineKeys
        {
            get
            {
                var DiscKeyRet = new List<int>();

                var cacheGoalElement = GoalCache.GetGoalElementByKey(GoalElementKey);
                if (cacheGoalElement != null && cacheGoalElement.DisciplineInGoalElement != null)
                {
                    foreach (var disc in cacheGoalElement.DisciplineInGoalElement)
                        DiscKeyRet.Add(disc.DisciplineKey);
                }

                return DiscKeyRet;
            }
        }

        public void RaisePropertyChanged()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { RaisePropertyChanged(null); });
        }

        public void RaisePropertyChangedHasValidationErrors()
        {
            RaisePropertyChanged("HasValidationErrors");
            RaisePropertyChanged("ValidationSummary");
        }

        public void RaisePropertyChangedAfterEdit()
        {
            RaisePropertyChanged("IsGoalElementPlannedForThisEncounter");
            RaisePropertyChanged("DisciplineCode");
            RaisePropertyChanged("GoalElementDescription");
            RaisePropertyChanged("GoalElementDescriptionPOC");
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

        public void Cleanup()
        {
            _CurrentEncounterGoalElement = null;
        }

        public AdmissionGoalElement CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            aCloning = true;
            var newgoalelement = (AdmissionGoalElement)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newgoalelement);
            if (newgoalelement.HistoryKey == null)
            {
                newgoalelement.HistoryKey = AdmissionGoalElementKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            aCloning = false;
            newgoalelement.aCloning = false;
            return newgoalelement;
        }


        partial void OnResolvedChanged()
        {
            if (IsDeserializing || aCloning)
            {
                return;
            }

            if (AdmissionGoal != null)
            {
                if (Resolved)
                {
                    ResolvedDate = DateTime.Now.Date;
                    ResolvedBy = WebContext.Current.User.MemberID;
                    var anyOpen = AdmissionGoal.AdmissionGoalElement
                        .Where(p => !p.Superceded && !p.Resolved && !p.Discontinued && !p.Unattainable && !p.Inactivated).Any();
                    if (!anyOpen)
                    {
                        AdmissionGoal.Resolved = true;
                        AdmissionGoal.ResolvedDate = DateTime.Now.Date;
                        AdmissionGoal.ResolvedBy = WebContext.Current.User.MemberID;
                    }

                    if (CurrentEncounterGoalElement != null)
                    {
                        CurrentEncounterGoalElement.Planned = false;
                    }
                }
                else
                {
                    ResolvedDate = null;
                    ResolvedBy = null;

                    AdmissionGoal.Resolved = false;
                    AdmissionGoal.ResolvedDate = null;
                    AdmissionGoal.ResolvedBy = null;
                }

                RaisePropertyChanged("CanPlan");
                RaisePropertyChanged("ShowResolved");
                RaisePropertyChanged("ShowUnattainable");
                RaisePropertyChanged("ShowInactivated");
                RaisePropertyChanged("ShowDiscontinued");
                RaisePropertyChanged("GoalElementStatus");
                RaisePropertyChanged("FinalStatusAndDate");
            }
        }

        partial void OnDiscontinuedChanged()
        {
            if (IsDeserializing || aCloning)
            {
                return;
            }

            if (AdmissionGoal != null)
            {
                if (Discontinued)
                {
                    DiscontinuedDate = DateTime.Now.Date;
                    DiscontinuedBy = WebContext.Current.User.MemberID;
                    DiscontinuedFromEncounterKey =
                        AdmissionGoal.GoalManager == null || AdmissionGoal.GoalManager.CurrentEncounter == null
                            ? (int?)null
                            : AdmissionGoal.GoalManager.CurrentEncounter.EncounterKey;
                    var anyOpen = AdmissionGoal.AdmissionGoalElement
                        .Where(p => !p.Superceded && !p.Resolved && !p.Discontinued && !p.Unattainable && !p.Inactivated).Any();
                    if (!anyOpen)
                    {
                        var anyResolved = AdmissionGoal.AdmissionGoalElement.Where(p => !p.Superceded && p.Resolved)
                            .Any();
                        if (anyResolved)
                        {
                            AdmissionGoal.Resolved = true;
                            AdmissionGoal.ResolvedDate = DateTime.Now.Date;
                            AdmissionGoal.ResolvedBy = WebContext.Current.User.MemberID;
                        }

                        var anyResolvedOrUnattainableOrInactivated = AdmissionGoal.AdmissionGoalElement
                            .Where(p => !p.Superceded && (p.Resolved || p.Unattainable || p.Inactivated)).Any();
                        if (anyResolvedOrUnattainableOrInactivated == false) // means allDiscontinued
                        {
                            AdmissionGoal.Discontinued = true;
                            AdmissionGoal.DiscontinuedDate = DateTime.Now.Date;
                            AdmissionGoal.DiscontinuedBy = WebContext.Current.User.MemberID;
                            AdmissionGoal.DiscontinuedFromEncounterKey =
                                AdmissionGoal.GoalManager == null || AdmissionGoal.GoalManager.CurrentEncounter == null
                                    ? (int?)null
                                    : AdmissionGoal.GoalManager.CurrentEncounter.EncounterKey;
                        }
                    }

                    if (CurrentEncounterGoalElement != null)
                    {
                        CurrentEncounterGoalElement.Planned = false;
                    }
                }
                else
                {
                    DiscontinuedDate = null;
                    DiscontinuedBy = null;
                    DiscontinuedFromEncounterKey = null;

                    AdmissionGoal.Discontinued = false;
                    AdmissionGoal.DiscontinuedDate = null;
                    AdmissionGoal.DiscontinuedBy = null;
                    AdmissionGoal.DiscontinuedFromEncounterKey = null;

                    AdmissionGoal.Resolved = false;
                    AdmissionGoal.ResolvedDate = null;
                    AdmissionGoal.ResolvedBy = null;
                }

                RaisePropertyChanged("CanPlan");
                RaisePropertyChanged("ShowResolved");
                RaisePropertyChanged("ShowUnattainable");
                RaisePropertyChanged("ShowInactivated");
                RaisePropertyChanged("ShowDiscontinued");
                RaisePropertyChanged("DiscontinuedThisEncounter");
                RaisePropertyChanged("DiscontinuedEncounterBy");
                RaisePropertyChanged("DiscontinuedEncounterDate");
                RaisePropertyChanged("GoalElementStatus");
            }
        }

        partial void OnUnattainableChanged()
        {
            if (IsDeserializing || aCloning)
            {
                return;
            }

            if (AdmissionGoal != null)
            {
                if (Unattainable)
                {
                    UnattainableDate = DateTime.Now.Date;
                    UnattainableBy = WebContext.Current.User.MemberID;

                    if (CurrentEncounterGoalElement != null)
                    {
                        CurrentEncounterGoalElement.Planned = false;
                    }
                }
                else
                {
                    UnattainableReason = 0;
                    UnattainableDate = null;
                    UnattainableBy = null;
                }

                RaisePropertyChanged("CanPlan");
                RaisePropertyChanged("ShowResolved");
                RaisePropertyChanged("ShowUnattainable");
                RaisePropertyChanged("ShowInactivated");
                RaisePropertyChanged("ShowDiscontinued");
                RaisePropertyChanged("GoalElementStatus");
                RaisePropertyChanged("FinalStatusAndDate");
            }
        }
        partial void OnUnattainableReasonChanged()
        {
            if (IsDeserializing || aCloning)
            {
                return;
            }
            RaisePropertyChanged("FinalStatusAndDate");
        }
        partial void OnInactivatedChanged()
        {
            if (IsDeserializing || aCloning)
            {
                return;
            }

            if (AdmissionGoal != null)
            {
                if (Inactivated)
                {
                    InactivatedDate = DateTime.Now.Date;
                    InactivatedBy = WebContext.Current.User.MemberID;

                    if (CurrentEncounterGoalElement != null)
                    {
                        CurrentEncounterGoalElement.Planned = false;
                    }
                }
                else
                {
                    InactivatedReason = null;
                    InactivatedDate = null;
                    InactivatedBy = null;
                }

                RaisePropertyChanged("CanPlan");
                RaisePropertyChanged("ShowResolved");
                RaisePropertyChanged("ShowUnattainable");
                RaisePropertyChanged("ShowInactivated");
                RaisePropertyChanged("ShowDiscontinued");
                RaisePropertyChanged("GoalElementStatus");
                RaisePropertyChanged("FinalStatusAndDate");
            }
        }
        partial void OnInactivatedReasonChanged()
        {
            if (IsDeserializing || aCloning)
            {
                return;
            }
            RaisePropertyChanged("FinalStatusAndDate");
        }

        public void RaisePropertyChangedGoalElementStatus()
        {
            RaisePropertyChanged("GoalElementStatus");
            RaisePropertyChanged("PlannedForThisEncounterOrAddressed");
        }
    }
}