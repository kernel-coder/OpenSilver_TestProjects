#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class FrequenceCycleDuration : QuestionUI
    {
        // NOTE :
        // This class was lifted and put in place for the new FCD Question Type.  Any changes here need analyzed to see if a 
        // similar change needs made in that class as well.  MaintenanceUserControls.cs -> VisitFrequencyUserControlBase
        private bool isLoading = true;

        public bool IsLoading
        {
            get { return isLoading; }
            set { isLoading = value; }
        }

        public IDynamicFormService Model => DynamicFormViewModel.FormModel;

        public FrequenceCycleDuration(DynamicFormViewModel vm, int? formSectionQuestionKey) : base(
            formSectionQuestionKey)
        {
            Admission = vm.CurrentAdmission;
            Encounter = vm.CurrentEncounter;
            Patient = vm.CurrentPatient;
            DynamicFormViewModel = vm;
            SelectedFCD = Encounter.AdmissionDisciplineFrequency.LastOrDefault(adf => !adf.Inactive);
            _FCDListView.Source = Admission.AdmissionDisciplineFrequency;
            FCDListView.Filter = item =>
            {
                AdmissionDisciplineFrequency adf = item as AdmissionDisciplineFrequency;
                return FilterItems(adf);
            };
            SetupCommands();
        }

        private void SetupCommands()
        {
            AddFCD = new RelayCommand(() =>
                {
                    AdmissionDisciplineFrequency newFCD = new AdmissionDisciplineFrequency();
                    if ((Encounter != null && (Encounter.EncounterKey > 0)))
                    {
                        newFCD.AddedFromEncounterKey = Encounter.EncounterKey;
                    }

                    if (Encounter.ServiceTypeKey != null)
                    {
                        newFCD.DisciplineKey =
                            (Int32)ServiceTypeCache.GetDisciplineKey((Int32)Encounter.ServiceTypeKey);
                    }

                    AdmissionCertification ac =
                        Admission.GetAdmissionCertForDate(Encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault()
                            .DateTime);

                    if (ac != null)
                    {
                        if (FCDList.Any(fcd => (fcd.DisciplineKey == newFCD.DisciplineKey) && !fcd.Inactive && !fcd.Superceded))
                        {
                            AdmissionDisciplineFrequency adf = FCDList.Where(fcd =>
                                    (fcd.DisciplineKey == newFCD.DisciplineKey)
                                    && !fcd.Inactive && !fcd.Superceded).OrderByDescending(f => f.StartDate)
                                .FirstOrDefault();
                            newFCD.StartDate = adf.EndDate.HasValue ? adf.EndDate.Value.AddDays(1) : adf.StartDate;
                        }
                        else
                        {
                            newFCD.StartDate = Admission.CurrentCert.PeriodStartDate;
                        }
                    }

                    newFCD.Admission = Admission;
                    Admission.AdmissionDisciplineFrequency.Add(newFCD);
                    RaisePropertyChanged("FCDList");
                    RaisePropertyChanged("FCDListView");
                    SelectedFCD = FCDList.LastOrDefault();
                    RaisePropertyChanged("SelectedFCD");
                },
                () => CanAddRows);
            DeleteFCD = new RelayCommand<AdmissionDisciplineFrequency>(item =>
            {
                if (item != null)
                {
                    if (item.IsNew)
                    {
                        FCDList.Remove(item);
                        Model.RemoveAdmissionDisciplineFrequency(item);
                    }
                    else
                    {
                        item.Inactive = true;
                        item.InactiveDate = DateTime.UtcNow;
                        if (Encounter != null && Encounter.EncounterDisciplineFrequency != null)
                        {
                            var encounterFreq = Encounter.EncounterDisciplineFrequency
                                .FirstOrDefault(edf => edf.DispFreqKey == selectedFCD.DisciplineFrequencyKey);
                            if (encounterFreq != null)
                            {
                                encounterFreq.Inactive = true;
                                encounterFreq.InactiveDate = SelectedFCD.InactiveDate;
                            }
                        }
                    }

                    FCDListView.Refresh();
                    RaisePropertyChanged("FCDList");
                    RaisePropertyChanged("FCDListView");
                    SelectedFCD = FCDList.LastOrDefault(fcd => !fcd.Inactive && !fcd.Superceded);
                    RaisePropertyChanged("SelectedFCD");
                    RaisePropertyChanged("CanDeleteRow");
                }
            }, item => CanDeleteRow);
        }

        public bool FilterItems(AdmissionDisciplineFrequency adf)
        {
            if (adf.Inactive)
            {
                return false;
            }

            if (adf.DisplayDisciplineFrequencyText.Contains("<New Frequency>"))
            {
                return true;
            }

            // remove rows that are being superceded and replaced.
            if (adf.DisciplineFrequencyKey > 0)
            {
                if (Admission.AdmissionDisciplineFrequency.Any(p => p.OriginatingDisciplineFrequencyKey == (adf.DisciplineFrequencyKey)))
                {
                    return false;
                }
            }

            if (Encounter.EncounterDisciplineFrequency.Any(p => p.DispFreqKey == adf.DisciplineFrequencyKey))
            {
                return true;
            }

            return false;
        }

        public bool CanAddRows
        {
            get
            {
                bool canAddRows = false;
                Form f = Question.FormSectionQuestion.FirstOrDefault().FormSection.Form;
                canAddRows = (f.IsVisitTeleMonitoring == false) &&
                             (f.IsOrderEntry || f.IsPlanOfCare || f.IsTeamMeeting || f.IsResumption || f.IsEval ||
                              f.IsVisit || f.IsBasicVisit) && CanEditRows;
                return canAddRows;
            }
        }

        public bool CanEditRows => !Protected;

        public bool CanDeleteRow
        {
            get
            {
                if (SelectedFCD == null)
                {
                    return false;
                }

                return CanEditSelectedOrder 
                       && !SelectedFCD.Inactive 
                       && (SelectedFCD.IsNew || SelectedFCD.AddedFromEncounterKey == (Encounter?.EncounterKey ?? -1))
                       && (Encounter.EncounterStatus != (int)EncounterStatusType.Completed)
                       && !Encounter.Signed;
            }
        }

        public bool CanEditSelectedOrder
        {
            get
            {
                if ((Admission == null) || (Encounter == null) || (Encounter.ServiceTypeKey == null))
                {
                    return false;
                }

                if (Encounter.Inactive)
                {
                    return false;
                }

                return SelectedFCD != null && CanEditFCD(SelectedFCD);
            }
        }

        public bool DisplayRecertCheckBox
        {
            get
            {
                if (DynamicFormViewModel == null || DynamicFormViewModel.CurrentForm == null)
                {
                    return false;
                }

                return (DynamicFormViewModel.CurrentForm.IsVisitTeleMonitoring == false) &&
                       (Encounter.EncounterIsInRecertWindow && (DynamicFormViewModel.CurrentForm.IsEval ||
                                                                DynamicFormViewModel.CurrentForm.IsResumption ||
                                                                DynamicFormViewModel.CurrentForm.IsVisit));
            }
        }

        public bool CanEditFCD(AdmissionDisciplineFrequency FCDParm)
        {
            var isEditable = DynamicFormViewModel != null && DynamicFormViewModel.CurrentForm != null
                                                          && (DynamicFormViewModel.CurrentForm.IsEval ||
                                                              DynamicFormViewModel.CurrentForm.IsResumption
                                                              || (DynamicFormViewModel.CurrentForm.IsPlanOfCare &&
                                                                  DynamicFormViewModel.CurrentForm.Description.Contains(
                                                                      "Edit")));
            if (Encounter != null)
            {
                isEditable = isEditable || FCDParm.AddedFromEncounterKey == Encounter.EncounterKey;
            }

            return (isEditable && !Protected && ((FCDParm.DisciplineKey ==
                                                  (Int32)ServiceTypeCache.GetDisciplineKey(
                                                      (Int32)Encounter.ServiceTypeKey)
                                                  || Admission.CareCoordinator ==
                                                  UserCache.Current.GetCurrentUserProfile().UserId)));
        }

        public bool CanEditEndDate
        {
            get
            {
                if (Encounter.Inactive)
                {
                    return false;
                }

                bool canEditEndDate = !(!AllowEndDateAfterCertCycle
                                        && (selectedFCD != null)
                                        && (selectedFCD.EndDate != null)
                                        && (CalculatedEndDate != null)
                                        && (CalculatedEndDate >= Encounter.EncounterCycleEndDate));

                return canEditEndDate;
            }
        }

        public bool AllowEndDateAfterCertCycle
        {
            get { return allowEndDateAfterCertCycle; }
            set
            {
                allowEndDateAfterCertCycle = value;
                SelectedFCD.EndDate = CalculatedEndDate;

                this.RaisePropertyChangedLambda(p => p.AllowEndDateAfterCertCycle);
                this.RaisePropertyChangedLambda(p => p.CanEditEndDate);
            }
        }

        private bool allowEndDateAfterCertCycle;

        public DateTime? CalculatedEndDate
        {
            get
            {
                DateTime? calculatedEndDate = null;

                if (IsLoading || DynamicFormViewModel.IsBusy)
                {
                    calculatedEndDate = SelectedFCD.EndDate;
                }
                else
                {
                    if (SelectedFCD.StartDate.HasValue && !string.IsNullOrEmpty(SelectedFCD.DurationCode) &&
                        SelectedFCD.DurationNumber.HasValue)
                    {
                        if (SelectedFCD.DurationCode == "DAY")
                        {
                            calculatedEndDate =
                                SelectedFCD.StartDate.Value.AddDays((double)(SelectedFCD.DurationNumber - 1));
                        }
                        else if (SelectedFCD.DurationCode == "WEEK")
                        {
                            int intWeekStartDate = 0;
                            string weekStartDate = TenantSettingsCache.Current.TenantSetting.WeekStartDay.ToUpper();
                            switch (weekStartDate)
                            {
                                case "MONDAY":
                                    intWeekStartDate = 1;
                                    break;
                                case "TUESDAY":
                                    intWeekStartDate = 2;
                                    break;
                                case "WEDNESDAY":
                                    intWeekStartDate = 3;
                                    break;
                                case "THURSDAY":
                                    intWeekStartDate = 4;
                                    break;
                                case "FRIDAY":
                                    intWeekStartDate = 5;
                                    break;
                                case "SATURDAY":
                                    intWeekStartDate = 6;
                                    break;
                                default:
                                    intWeekStartDate = 0;
                                    break;
                            }

                            calculatedEndDate =
                                SelectedFCD.StartDate.Value.AddDays((double)((SelectedFCD.DurationNumber * 7) - 1));

                            int calcDay = (int)calculatedEndDate.Value.DayOfWeek;

                            if (calcDay != (intWeekStartDate - 1)
                                && !((calcDay == 6)
                                     && (intWeekStartDate == 0)
                                    )
                               )
                            {
                                if (intWeekStartDate > calcDay)
                                {
                                    calcDay += 7;
                                }

                                calculatedEndDate =
                                    calculatedEndDate.Value.AddDays(-((calcDay - intWeekStartDate) + 1));
                            }
                        }
                        else if (SelectedFCD.DurationCode == "MONTH")
                        {
                            calculatedEndDate = SelectedFCD.StartDate.Value.AddMonths(SelectedFCD.DurationNumber.Value)
                                .AddDays(-1);
                        }
                        else if (SelectedFCD.DurationCode == "YEAR")
                        {
                            calculatedEndDate = SelectedFCD.StartDate.Value.AddYears(SelectedFCD.DurationNumber.Value)
                                .AddDays(-1);
                        }
                    }

                    DateTime? myEndDate = GetEndOfCertPeriod(SelectedFCD.StartDate);

                    if (myEndDate != null)
                    {
                        if ((calculatedEndDate != null)
                            && (!AllowEndDateAfterCertCycle)
                            && Encounter.EncounterCycleStartDate.HasValue
                            && (calculatedEndDate > myEndDate)
                           )
                        {
                            calculatedEndDate = myEndDate;
                        }
                    }

                    if (calculatedEndDate == null)
                    {
                        calculatedEndDate = SelectedFCD.EndDate;
                    }
                }

                return calculatedEndDate;
            }
        }

        private DateTime? GetEndOfCertPeriod(DateTime? StartDate)
        {
            DateTime? endDate = null;

            if (StartDate.HasValue)
            {
                if (Admission.CanModifyCertCycle)
                {
                    if (Encounter != null)
                    {
                        if ((Encounter.EncounterCycleStartDate <= StartDate)
                            && ((Encounter.EncounterCycleEndDate == null)
                                || (Encounter.EncounterCycleEndDate >= StartDate)
                            )
                           )
                        {
                            endDate = Encounter.EncounterCycleEndDate;
                        }
                    }
                }
                else
                {
                    if (Admission != null)
                    {
                        if (Admission.AdmissionCertification != null)
                        {
                            var end = Admission.AdmissionCertification.Where(ac =>
                                (ac.PeriodStartDate <= StartDate) &&
                                ((ac.PeriodEndDate == null) || (ac.PeriodEndDate >= StartDate)));
                            if (end.Any())
                            {
                                endDate = end.First().PeriodEndDate;
                            }
                        }
                    }
                }
            }

            return endDate;
        }

        public EntityCollection<AdmissionDisciplineFrequency> FCDList => Admission.AdmissionDisciplineFrequency;
        public CollectionViewSource _FCDListView = new CollectionViewSource();
        public ICollectionView FCDListView => _FCDListView.View;
        private AdmissionDisciplineFrequency selectedFCD;

        public AdmissionDisciplineFrequency SelectedFCD
        {
            get { return selectedFCD; }
            set
            {
                IsLoading = true;

                if (selectedFCD != null)
                {
                    selectedFCD.PropertyChanged -= selectedFCD_PropertyChanged;
                }

                selectedFCD = value;

                if (selectedFCD != null)
                {
                    selectedFCD.PropertyChanged += selectedFCD_PropertyChanged;
                    AllowEndDateAfterCertCycle = selectedFCD.EndDate != GetEndOfCertPeriod(SelectedFCD.StartDate);
                }


                RaisePropertyChanged("SelectedFCD");
                RaisePropertyChanged("CanEditSelectedOrder");
                RaisePropertyChanged("CanDeleteRow");

                IsLoading = false;
            }
        }

        void selectedFCD_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == "StartDate")
                || (e.PropertyName == "DurationCode")
                || (e.PropertyName == "DurationNumber")
               )
            {
                SelectedFCD.EndDate = CalculatedEndDate;

                AllowEndDateAfterCertCycle = SelectedFCD.EndDate != GetEndOfCertPeriod(SelectedFCD.StartDate);

                this.RaisePropertyChangedLambda(p => p.CanEditEndDate);
            }
        }

        public RelayCommand AddFCD { get; set; }
        public RelayCommand<AdmissionDisciplineFrequency> DeleteFCD { get; set; }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            if (Encounter.FullValidation == false)
            {
                return true;
            }

            bool AllValid = true;
            foreach (var item in FCDList)
            {
                item.ValidationErrors.Clear();

                // a complete visit frequency consists of a  Frequency, Duration and Purpose
                if ((item.FrequencyMax != null) || (item.DurationCode != null) || (item.DurationNumber != null) ||
                    (item.Purpose != null)
                    || item.StartDate.HasValue || item.FrequencyMin != null || item.EndDate.HasValue ||
                    (Required && Encounter.FullValidation))
                {
                    if (!(item.Validate() && RunEntityLevelValidations(item)))
                    {
                        AllValid = false;
                        ValidationError = "Visit Frequency is Invalid";
                    }
                }
                else
                {
                    if (item.EntityState == EntityState.Modified || item.EntityState == EntityState.New)
                    {
                        FCDList.Remove(item);
                        Model.RemoveAdmissionDisciplineFrequency(item);
                    }
                }
            }

            foreach (var item in FCDList.Reverse())
                if ((item.FrequencyMax == null) && (item.DurationCode == null) && (item.DurationNumber == null) &&
                    (item.Purpose == null)
                    && item.FrequencyMin == null && !item.StartDate.HasValue && !item.EndDate.HasValue)
                {
                    try
                    {
                        FCDList.Remove(item);
                    }
                    catch
                    {
                    }
                }

            int? DiscKey = Encounter.ServiceTypeKey == null
                ? null
                : ServiceTypeCache.GetDisciplineKey((int)Encounter.ServiceTypeKey);
            if ((DynamicFormViewModel.CurrentForm.IsEval || DynamicFormViewModel.CurrentForm.IsResumption) &&
                (!FCDList.Where(fcd => fcd.DisciplineKey == DiscKey).Any() || FCDList.Any() == false))
            {
                if (Encounter.FullValidation)
                {
                    Encounter.ValidationErrors.Add(new ValidationResult("At least one visit frequency is required",
                        new[] { "EncounterKey" }));
                    AllValid = false;
                }
            }

            return AllValid;
        }

        public bool RunEntityLevelValidations(AdmissionDisciplineFrequency CurrentVisitFrequency)
        {
            // Don't validate entries that don't apply to our discipline.
            if (!CanEditFCD(CurrentVisitFrequency))
            {
                return true;
            }

            if (CurrentVisitFrequency.Superceded || CurrentVisitFrequency.Inactive)
            {
                return true;
            }

            // Don't validate entries that are going to be inactivated.
            if (CurrentVisitFrequency.Inactive)
            {
                return true;
            }

            // Missing Fields validation
            // When cycle code == ASNEEDED Require: FrequencyMax               CycleCode  DurationNumber  DurationCode  StartDate  EndDate  Purpose
            // When cycle code != ASNEEDED Require: FrequencyMax  CycleNumber  CycleCode  DurationNumber  DurationCode  StartDate  EndDate
            if (CurrentVisitFrequency.FrequencyMin == 0)
            {
                CurrentVisitFrequency.FrequencyMin = null;
            }

            if (CurrentVisitFrequency.FrequencyMax == 0)
            {
                CurrentVisitFrequency.FrequencyMax = null;
            }

            if (CurrentVisitFrequency.CycleNumber == 0)
            {
                CurrentVisitFrequency.CycleNumber = null;
            }

            if (string.IsNullOrWhiteSpace(CurrentVisitFrequency.CycleCode))
            {
                CurrentVisitFrequency.CycleCode = null;
            }

            if (CurrentVisitFrequency.DurationNumber == 0)
            {
                CurrentVisitFrequency.DurationNumber = null;
            }

            if (string.IsNullOrWhiteSpace(CurrentVisitFrequency.DurationCode))
            {
                CurrentVisitFrequency.DurationCode = null;
            }

            if (CurrentVisitFrequency.StartDate == DateTime.MinValue)
            {
                CurrentVisitFrequency.StartDate = null;
            }

            if (CurrentVisitFrequency.EndDate == DateTime.MinValue)
            {
                CurrentVisitFrequency.EndDate = null;
            }

            if (string.IsNullOrWhiteSpace(CurrentVisitFrequency.Purpose))
            {
                CurrentVisitFrequency.Purpose = null;
            }

            if (CurrentVisitFrequency.Hours == 0)
            {
                CurrentVisitFrequency.Hours = null;
            }

            List<string> memberNames = new List<string>();

            if (CurrentVisitFrequency.FrequencyMax == null)
            {
                memberNames.Add("FrequencyMax");
            }

            if ((CurrentVisitFrequency.IsPRN_Client == false) && (CurrentVisitFrequency.CycleNumber == null))
            {
                memberNames.Add("CycleNumber");
            }

            if (CurrentVisitFrequency.CycleCode == null)
            {
                memberNames.Add("CycleCode");
            }

            if (CurrentVisitFrequency.DurationNumber == null)
            {
                memberNames.Add("DurationNumber");
            }

            if (CurrentVisitFrequency.DurationCode == null)
            {
                memberNames.Add("DurationCode");
            }

            if (CurrentVisitFrequency.StartDate == null)
            {
                memberNames.Add("StartDate");
            }

            if (CurrentVisitFrequency.EndDate == null)
            {
                memberNames.Add("EndDate");
            }

            if (CurrentVisitFrequency.IsPRN_Client && (CurrentVisitFrequency.Purpose == null))
            {
                memberNames.Add("Purpose");
            }

            if (memberNames.Any())
            {
                string errMsg = CurrentVisitFrequency.IsPRN_Client
                    ? "You must specify a full visit frequency including Max, Cycle Code, Duration, Duration Code, Start Date, End Date and Purpose"
                    : "You must specify a full visit frequency including Max, Cycle, Cycle Code, Duration, Duration Code, Start Date and End Date";
                CurrentVisitFrequency.ValidationErrors.Add(new ValidationResult(errMsg, memberNames));
                return false;
            }

            // Crossed Dates
            if (CurrentVisitFrequency.StartDate > CurrentVisitFrequency.EndDate)
            {
                memberNames = new List<string> { "DispDisplayDisciplineFrequencyText", "StartDate", "EndDate" };
                CurrentVisitFrequency.ValidationErrors.Add(
                    new ValidationResult("Frequency Start date cannot be later than the End Date.", memberNames));
                return false;
            }

            // Crossed Min/Max
            if (CurrentVisitFrequency.FrequencyMin > CurrentVisitFrequency.FrequencyMax)
            {
                memberNames = new List<string> { "DispDisplayDisciplineFrequencyText", "FrequencyMin", "FrequencyMax" };
                CurrentVisitFrequency.ValidationErrors.Add(
                    new ValidationResult("Frequency Min cannot be greater than the Frequency Max.", memberNames));
                return false;
            }

            //Overlapping dates
            IQueryable<AdmissionDisciplineFrequency> adf = null;
            adf = FCDList.AsQueryable();

            if (adf != null && CurrentVisitFrequency.IsPRN_Client == false)
            {
                if (adf.Any(df => df.DisciplineFrequencyKey != CurrentVisitFrequency.DisciplineFrequencyKey &&
                                  !df.Inactive
                                  && df.DisciplineKey == CurrentVisitFrequency.DisciplineKey && df.IsPRN_Client == false
                                  && df.Superceded == false && CurrentVisitFrequency.Superceded == false
                                  && (df.OriginatingDisciplineFrequencyKey) !=
                                  CurrentVisitFrequency.DisciplineFrequencyKey // ignore rows about to be superceded

                                  // All non null dates
                                  && ((CurrentVisitFrequency.StartDate <= df.EndDate &&
                                       CurrentVisitFrequency.EndDate >= df.StartDate)
                                      // row passed in has null thru date
                                      || (CurrentVisitFrequency.EndDate == null &&
                                          df.EndDate >= CurrentVisitFrequency.StartDate)
                                      // row passed in has non null
                                      || (df.EndDate == null && CurrentVisitFrequency.EndDate >= df.StartDate)
                                      // both have non null thru dates
                                      || (df.EndDate == null && CurrentVisitFrequency.EndDate == null)
                                  )))
                {
                    memberNames = new List<string> { "DispDisplayDisciplineFrequencyText", "StartDate", "EndDate" };
                    CurrentVisitFrequency.ValidationErrors.Add(
                        new ValidationResult("Frequency and Duration items must not overlap", memberNames));
                    return false;
                }
            }

            if (!CurrentVisitFrequency.Superceded)
            {
                if (CurrentVisitFrequency.Admission != null)
                {
                    bool success = true;

                    IQueryable<AdmissionCertification> admitCert = null;
                    admitCert = CurrentAdmission.AdmissionCertification.AsQueryable();

                    if (admitCert != null)
                    {
                        if (CurrentAdmission != null && CurrentAdmission.CanModifyCertCycle)
                        {
                            if ((admitCert.Any() == false)
                                || !admitCert.Any(p => p.PeriodStartDate <= CurrentVisitFrequency.StartDate
                                                       && ((p.PeriodEndDate == null)
                                                           || (p.PeriodEndDate >= CurrentVisitFrequency.StartDate)
                                                       )
                                )
                               )
                            {
                                success = false;
                            }
                        }
                        else
                        {
                            if ((CurrentVisitFrequency.Admission.AdmissionCertification.Any() == false)
                                || (CurrentVisitFrequency.StartDate < CurrentVisitFrequency.Admission
                                    .AdmissionCertification.OrderBy(p => p.PeriodStartDate).FirstOrDefault()
                                    .PeriodStartDate)
                                || (CurrentVisitFrequency.StartDate > CurrentVisitFrequency.Admission
                                    .AdmissionCertification.OrderByDescending(p => p.PeriodStartDate).FirstOrDefault()
                                    .PeriodEndDate)
                               )
                            {
                                success = false;
                            }
                        }

                        if (!success)
                        {
                            memberNames = new List<string>
                                { "DispDisplayDisciplineFrequencyText", "StartDate", "EndDate" };
                            CurrentVisitFrequency.ValidationErrors.Add(new ValidationResult(
                                "All Frequency and Duration items must start within a defined certification period",
                                memberNames));
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }

    public class FrequencyCycleDurationFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            FrequenceCycleDuration fcd = new FrequenceCycleDuration(vm, __FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Admission = vm.CurrentAdmission
            };
            return fcd;
        }
    }
}