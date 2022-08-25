#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Itenso.TimePeriod;
using RiaServicesContrib.Extensions;
using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public class AuthDistributionViewModel : NotifyDataErrorInfoViewModelBase
    {
        #region PROPERTIES

        public class Distribution : GalaSoft.MvvmLight.ViewModelBase
        {
            Action NumberChangedAction;

            public Distribution(bool trackingEnabled = false)
            {
                TrackingEnabled = trackingEnabled;
            }

            public Distribution(Action numberChangedAction, bool trackingEnabled = false)
            {
                TrackingEnabled = trackingEnabled;
                NumberChangedAction = numberChangedAction;
            }

            public int? AdmissionAuthorizationDetailKey { get; set; }

            decimal __NumberPerCycle;

            public decimal NumberPerCycle
            {
                get { return __NumberPerCycle; }
                set
                {
                    if (value != __NumberPerCycle)
                    {
                        __NumberPerCycle = value;
                        if (TrackingEnabled)
                        {
                            __Dirty = true;
                        }

                        RaisePropertyChanged(() => NumberPerCycle);
                        if (NumberChangedAction != null)
                        {
                            NumberChangedAction.Invoke();
                        }
                    }
                }
            }

            string __Cycle { get; set; }

            public string Cycle
            {
                get { return __Cycle; }
                set
                {
                    if (value != __Cycle)
                    {
                        __Cycle = value;
                        if (TrackingEnabled)
                        {
                            __Dirty = true;
                        }

                        RaisePropertyChanged(() => Cycle);
                    }
                }
            }

            DateTime __FromDateTime;

            public DateTime FromDateTime
            {
                get { return __FromDateTime; }
                set
                {
                    if (value != __FromDateTime)
                    {
                        __FromDateTime = value;
                        if (TrackingEnabled)
                        {
                            __Dirty = true;
                        }

                        RaisePropertyChanged(() => FromDateTime);
                    }
                }
            }

            string __FromDate;

            public string FromDate
            {
                get { return __FromDate; }
                set
                {
                    if (value != __FromDate)
                    {
                        __FromDate = value;
                        if (TrackingEnabled)
                        {
                            __Dirty = true;
                        }

                        RaisePropertyChanged(() => FromDate);
                    }
                }
            }

            DateTime __ThruDateTime;

            public DateTime ThruDateTime
            {
                get { return __ThruDateTime; }
                set
                {
                    if (value != __ThruDateTime)
                    {
                        __ThruDateTime = value;
                        if (TrackingEnabled)
                        {
                            __Dirty = true;
                        }

                        RaisePropertyChanged(() => ThruDateTime);
                    }
                }
            }

            string __ThruDate;

            public string ThruDate
            {
                get { return __ThruDate; }
                set
                {
                    if (value != __ThruDate)
                    {
                        __ThruDate = value;
                        if (TrackingEnabled)
                        {
                            __Dirty = true;
                        }

                        RaisePropertyChanged(() => ThruDate);
                    }
                }
            }

            string __AuthCount;

            public string AuthCount
            {
                get { return __AuthCount; }
                set
                {
                    if (value != __AuthCount)
                    {
                        __AuthCount = value;
                        if (TrackingEnabled)
                        {
                            __Dirty = true;
                        }

                        RaisePropertyChanged(() => AuthCount);
                    }
                }
            }

            string __AuthCountLastUpdate;

            public string AuthCountLastUpdate
            {
                get { return __AuthCountLastUpdate; }
                set
                {
                    if (value != __AuthCountLastUpdate)
                    {
                        __AuthCountLastUpdate = value;
                        if (TrackingEnabled)
                        {
                            __Dirty = true;
                        }

                        RaisePropertyChanged(() => AuthCountLastUpdate);
                    }
                }
            }

            public bool TrackingEnabled { get; set; }
            private bool __Dirty;

            public bool IsDirty()
            {
                return __Dirty;
            }

            public Distribution Copy()
            {
                //NOTE: all types are value types or immutable
                return (Distribution)MemberwiseClone();
            }
        }

        public AuthMode AuthMode { get; set; }
        bool __IsEditMode;

        public bool IsEditMode
        {
            get { return __IsEditMode; }
            set
            {
                if (value != __IsEditMode)
                {
                    __IsEditMode = value;
                    RaisePropertyChanged(() => IsEditMode);
                }
            }
        }

        bool __AllowBuildDistribution;

        public bool AllowBuildDistribution
        {
            get { return __AllowBuildDistribution; }
            set
            {
                if (value != __AllowBuildDistribution)
                {
                    __AllowBuildDistribution = value;
                    RaisePropertyChanged(() => AllowBuildDistribution);
                }
            }
        }

        bool? _dialogResult;

        public bool? DialogResult
        {
            get { return _dialogResult; }
            set
            {
                _dialogResult = value;
                RaisePropertyChanged("DialogResult");
            }
        }

        private AdmissionAuthorizationInstance CurrentAdmissionAuthorizationInstance { get; set; }
        private bool _HaveDistribution;

        public bool HaveDistribution
        {
            get { return _HaveDistribution; }
            set
            {
                _HaveDistribution = value;
                RaisePropertyChanged("HaveDistribution");
            }
        }

        private string __BuildDistributionCommandText;

        public string BuildDistributionCommandText
        {
            get { return __BuildDistributionCommandText; }
            set
            {
                __BuildDistributionCommandText = value;
                RaisePropertyChanged("BuildDistributionCommandText");
            }
        }

        public RelayCommand BuildDistributionCommand { get; protected set; }
        int __DistCycleKeyUsedForDistribution;
        int __DistCycleKey;

        public int DistCycleKey
        {
            get { return __DistCycleKey; }
            set
            {
                if (value != __DistCycleKey)
                {
                    __DistCycleKey = value;
                    RaisePropertyChanged(() => DistCycleKey);
                }
            }
        }

        decimal __NumberPerCycleUsedForDistribution;
        decimal __NumberPerCycle;

        public decimal NumberPerCycle
        {
            get { return __NumberPerCycle; }
            set
            {
                if (value != __NumberPerCycle)
                {
                    __NumberPerCycle = value;
                    RaisePropertyChanged(() => NumberPerCycle);
                }
            }
        }

        private decimal DistributedAmount;
        string __DistributedAmountStr;

        public string DistributedAmountStr
        {
            get { return __DistributedAmountStr; }
            set
            {
                __DistributedAmountStr = value;
                ValidateDistributedAmount();
                RaisePropertyChanged("DistributedAmountStr");
            }
        }

        string __AuthorizationAmountStr;

        public string AuthorizationAmountStr
        {
            get { return __AuthorizationAmountStr; }
            set
            {
                if (value != __AuthorizationAmountStr)
                {
                    __AuthorizationAmountStr = value;
                    RaisePropertyChanged(() => AuthorizationAmountStr);
                }
            }
        }

        private bool AllowExtensionToDistribution { get; set; }

        private List<Distribution> __DistributionCollectionBackingCollection;
        private CollectionViewSource __DistributionCollection;

        public CollectionViewSource DistributionCollection
        {
            get { return __DistributionCollection; }
            set
            {
                __DistributionCollection = value;
                RaisePropertyChanged("DistributionCollection");
            }
        }

        public Distribution SelectedDistribution { get; set; }
        public static string dateFormat = "MM/dd/yyyy";
        public RelayCommand SaveDistributionCommand { get; protected set; }
        private bool _Save_DistributionCommandVisible;

        public bool Save_DistributionCommandVisible
        {
            get { return _Save_DistributionCommandVisible; }
            set
            {
                _Save_DistributionCommandVisible = value;
                RaisePropertyChanged("Save_DistributionCommandVisible");
            }
        }

        public RelayCommand CancelDistributionCommand { get; protected set; }

        #endregion PROPERTIES

        public AuthDistributionViewModel(AdmissionAuthorizationInstance instance, List<Distribution> distribution,
            AuthMode mode, bool allowExtensionToDistribution)
        {
            CurrentAdmissionAuthorizationInstance = instance;
            AllowExtensionToDistribution = allowExtensionToDistribution;
            AuthMode = mode;
            AllowBuildDistribution = true;
            IsEditMode = (AuthMode == AuthMode.EDIT);
            HaveDistribution = false;

            BuildDistributionCommandText = "Build Distribution";
            AuthorizationAmountStr = string.Format(
                "Total # Distributed must match {0:0.00} (Authorization Amount on Authorization).",
                CurrentAdmissionAuthorizationInstance.AuthorizationAmount);

            __DistributionCollectionBackingCollection = new List<Distribution>();
            DistributionCollection = new CollectionViewSource();
            DistributionCollection.SortDescriptions.Add(
                new SortDescription("FromDateTime", System.ComponentModel.ListSortDirection.Ascending));

            SetupCommands();

            if (distribution != null)
            {
                SetAuthorizationDetailDistribution(distribution);

                BuildDistributionCommandText = Set_BuildDistributionCommandText();
                CalculateDistributedAmount();

                //leave false when initializing for an EDIT - validation code might set to true when setting up the distribution
                Save_DistributionCommandVisible = false;

                //Re-enble Re-Build Distribution button when in EDIT MODE
                if (mode == AuthMode.EDIT)
                {
                    //Do not use IsEdit to control when to allow re-build on EDIT - that is needed for conditionally showing other widgets when in EDIT mode
                    AllowBuildDistribution = false;

                    //The Re-build distribution button will be accessible if the from and or through date of the Authorization on the Authorization detail
                    //screen was modified.
                    if (AllowExtensionToDistribution)
                    {
                        AllowBuildDistribution = false;
                    }
                    else
                    {
                        var changesOnlyState =
                            CurrentAdmissionAuthorizationInstance.ExtractState(RiaServicesContrib.ExtractType
                                .ChangesOnlyState);
                        if (changesOnlyState.Keys.Contains("EffectiveFromDate") ||
                            changesOnlyState.Keys.Contains("EffectiveToDate"))
                        {
                            AllowBuildDistribution = true;
                        }
                    }
                }

                RaiseDistributionCreatedEvent();
            }
        }

        private string Set_BuildDistributionCommandText()
        {
            return AllowExtensionToDistribution ? "Extend Distribution" : "Re-Build Distribution";
        }

        private void SetupCommands()
        {
            BuildDistributionCommand = new RelayCommand(() =>
            {
                if (DialogResult.HasValue)
                {
                    return;
                }

                if (IsValidForBuildingDistribution())
                {
                    HaveDistribution = true;
                    BuildDistribution();
                    __DistCycleKeyUsedForDistribution = DistCycleKey;
                    __NumberPerCycleUsedForDistribution = NumberPerCycle;
                    BuildDistributionCommandText = Set_BuildDistributionCommandText();
                    Save_DistributionCommandVisible = true;
                    RaiseDistributionCreatedEvent();
                }
            }, () =>
            {
                var ret = (AllowBuildDistribution || AllowExtensionToDistribution) && DialogResult.HasValue == false;
                return ret;
            });

            SaveDistributionCommand = new RelayCommand(() =>
            {
                if (DialogResult.HasValue)
                {
                    return;
                }

                if (ValidateDistributedAmount())
                {
                    MessengerInstance.Send(GetAuthorizationDetailDistribution(),
                        CurrentAdmissionAuthorizationInstance.AdmissionAuthorizationInstanceKey);
                    DialogResult = true;
                }
            }, () => DialogResult.HasValue == false);

            CancelDistributionCommand = new RelayCommand(() =>
            {
                if (DialogResult.HasValue)
                {
                    return;
                }

                MessengerInstance.Send<List<Distribution>>(null,
                    CurrentAdmissionAuthorizationInstance.AdmissionAuthorizationInstanceKey);
                DialogResult = false;
            }, () => DialogResult.HasValue == false);
        }

        //Check constraints for generating a distribution
        public bool IsValidForBuildingDistribution()
        {
            var __IsValidForBuildingDistribution = true;
            ClearErrorFromProperty("DistCycleKey");
            var distCycleLookup = CodeLookupCache.GetCodeLookupFromKey(DistCycleKey);
            if (distCycleLookup == null)
            {
                __IsValidForBuildingDistribution = false;
                AddErrorForProperty("DistCycleKey", "Distribution Cycle is required. ");
            }

            ClearErrorFromProperty("NumberPerCycle");
            if (NumberPerCycle <= 0)
            {
                __IsValidForBuildingDistribution = false;
                AddErrorForProperty("NumberPerCycle", "Number per Cycle is required.");
            }

            //NOTE: could have other errors - allow re-distribution if there are other errors - only disallow when DistCycleKey and/or NumberPerCycle are invalid
            return __IsValidForBuildingDistribution;
        }

        private bool DeferCalculateDistributedAmount;

        void CalculateDistributedAmount()
        {
            if (DeferCalculateDistributedAmount)
            {
                return;
            }

            DistributedAmount =
                DistributionCollection.View.Cast<Distribution>().Select(d => d.NumberPerCycle).Sum(); //cache it
            DistributedAmountStr = string.Format("{0:0.00}", DistributedAmount);
            ValidateDistributedAmount();
            Save_DistributionCommandVisible = (HasErrors == false);
        }

        //validate a distribution - e.g. ensure the distributed amounts are less than the authorization amount on the header/instance
        public bool ValidateDistributedAmount()
        {
            var __IsDistributionValid = true;
            ClearErrorFromProperty("DistributedAmountStr");

            var _total = CurrentAdmissionAuthorizationInstance.AuthorizationAmount;
            if (DistributedAmount != _total)
            {
                __IsDistributionValid = false;
                AddErrorForProperty("DistributedAmountStr",
                    string.Format("Total # Distributed must match {0:0.00} (Authorization Amount on Authorization).",
                        CurrentAdmissionAuthorizationInstance.AuthorizationAmount));
            }

            Save_DistributionCommandVisible = (HasErrors == false);
            return __IsDistributionValid;
        }

        //called by Build distribution button clicked
        private void BuildDistribution()
        {
            if (AllowExtensionToDistribution)
            {
                __Extend_Distribution();
            }
            else
            {
                __Build_Distribution();
            }
        }

        private void __Extend_Distribution()
        {
            var distCycleLookup = CodeLookupCache.GetCodeLookupFromKey(DistCycleKey);

            if (distCycleLookup == null)
            {
                throw new ArgumentException("Cycle cannot be null");
            }

            List<Tuple<DateTime, DateTime>> range = GetDistributionDateRange(distCycleLookup);

            //1.) check if the last auth in our distribution needs to have it's end date updated
            var last = DistributionCollection.View.Cast<Distribution>().LastOrDefault();
            var last_match_new_range = range.Where(d => d.Item1.Date == last.FromDateTime.Date).FirstOrDefault();
            if (last != null && last_match_new_range != null &&
                last.ThruDateTime.Date != last_match_new_range.Item2.Date)
            {
                last.ThruDateTime = last_match_new_range.Item2;
                last.ThruDate = last_match_new_range.Item2.ToString(dateFormat);
            }

            //2.) add the new date ranges to the existing distribution
            range.Where(r => r.Item1.Date > last.FromDateTime.Date)
                .ToList()
                .ForEach(t =>
                {
                    System.Diagnostics.Debug.WriteLine("FROM: {0}\tTHRU: {1}", t.Item1, t.Item2);

                    __DistributionCollectionBackingCollection.Add(
                        new Distribution(numberChangedAction: CalculateDistributedAmount, trackingEnabled: true)
                        {
                            //NOTE: building a brand new distribution - AdmissionAuthorizationDetailKey will be NULL
                            Cycle = distCycleLookup.CodeDescription,
                            FromDateTime = t.Item1,
                            FromDate = t.Item1.ToString(dateFormat),
                            ThruDateTime = t.Item2,
                            ThruDate = t.Item2.ToString(dateFormat)
                        });
                });

            DistributionCollection.View.Refresh();
            RaisePropertyChanged("DistributionCollection");

            AllowExtensionToDistribution = false;
            BuildDistributionCommand.RaiseCanExecuteChanged();

            CalculateDistributedAmount();
        }

        private void __Build_Distribution()
        {
            var distCycleLookup = CodeLookupCache.GetCodeLookupFromKey(DistCycleKey);

            if (distCycleLookup == null)
            {
                throw new ArgumentException("Cycle cannot be null");
            }

            DistributionCollection.Source = null;
            __DistributionCollectionBackingCollection.Clear();
            RaisePropertyChanged("DistributionCollection");

            List<Tuple<DateTime, DateTime>> range = GetDistributionDateRange(distCycleLookup);

            __DistributionCollectionBackingCollection = new List<Distribution>();
            range.ToList().ForEach(t =>
            {
                __DistributionCollectionBackingCollection.Add(
                    new Distribution(numberChangedAction: CalculateDistributedAmount, trackingEnabled: true)
                    {
                        //NOTE: building a brand new distribution - AdmissionAuthorizationDetailKey will be NULL
                        Cycle = distCycleLookup.CodeDescription,
                        FromDateTime = t.Item1,
                        FromDate = t.Item1.ToString(dateFormat),
                        ThruDateTime = t.Item2,
                        ThruDate = t.Item2.ToString(dateFormat)
                    });
            });
            DistributionCollection.Source = __DistributionCollectionBackingCollection;
            RaisePropertyChanged("DistributionCollection");

            //NOTE: Distribute amountPerCycle over periods not to exceed TotalAuthorized

            decimal amount_left = CurrentAdmissionAuthorizationInstance.AuthorizationAmount;
            decimal running_total = 0.0m;
            DeferCalculateDistributedAmount = true;
            foreach (var d in DistributionCollection.View.Cast<Distribution>())
            {
                d.NumberPerCycle = Math.Min(NumberPerCycle, amount_left);
                amount_left -= NumberPerCycle;
                running_total += NumberPerCycle;
                if (running_total > CurrentAdmissionAuthorizationInstance.AuthorizationAmount)
                {
                    break;
                }
            }

            DeferCalculateDistributedAmount = false;
            CalculateDistributedAmount();
        }

        private void RaiseDistributionCreatedEvent()
        {
            Messenger.Default.Send(new AuthorizationDistributionCreated { EventName = Constants.Application.AuthorizationDistributionCreated });
        }

        //Called by constructor when passed a distribution from the underlying VM
        private void SetAuthorizationDetailDistribution(List<Distribution> distribution)
        {
            if (distribution == null)
            {
                return;
            }

            HaveDistribution = true;
            DeferCalculateDistributedAmount = true;

            __DistributionCollectionBackingCollection = new List<Distribution>();
            distribution.ForEach(d =>
            {
                var copy = new Distribution(numberChangedAction: CalculateDistributedAmount, trackingEnabled: false)
                {
                    AdmissionAuthorizationDetailKey = d.AdmissionAuthorizationDetailKey,
                    NumberPerCycle = d.NumberPerCycle,
                    Cycle = d.Cycle,
                    FromDateTime = d.FromDateTime,
                    FromDate = d.FromDate,
                    ThruDateTime = d.ThruDateTime,
                    ThruDate = d.ThruDate,
                    AuthCount = d.AuthCount,
                    AuthCountLastUpdate = d.AuthCountLastUpdate,
                    TrackingEnabled = true
                };
                __DistributionCollectionBackingCollection.Add(copy);
            });

            DistributionCollection.Source = __DistributionCollectionBackingCollection;
            RaisePropertyChanged("DistributionCollection");
            DeferCalculateDistributedAmount = false;

            __DistCycleKeyUsedForDistribution = DistCycleKey = CurrentAdmissionAuthorizationInstance.DistCycleKey.GetValueOrDefault();
            __NumberPerCycleUsedForDistribution = NumberPerCycle = CurrentAdmissionAuthorizationInstance.NumberPerCycle.GetValueOrDefault();
        }

        //Called by SaveDistributionCommand to send a copy of the distribution to the underlying VM via MVVMLight Messenger.
        private List<Distribution> GetAuthorizationDetailDistribution()
        {
            List<Distribution> ret = new List<Distribution>();
            foreach (var d in DistributionCollection.View.Cast<Distribution>())
            {
                var copy = d.Copy();
                ret.Add(copy);
            }

            //Make sure end user didn't switch the DistributionCycle after they distributed and before a save.
            //E.G. Set DistributionCycle to Weekly, then distribute, then change DistributionCycle dropdown to Monthly, then save.
            //     We want the saved DistCycleKey on the instance to match those 
            CurrentAdmissionAuthorizationInstance.DistCycleKey = __DistCycleKeyUsedForDistribution;
            CurrentAdmissionAuthorizationInstance.NumberPerCycle = __NumberPerCycleUsedForDistribution;

            return ret;
        }

        private List<Tuple<DateTime, DateTime>> GetDistributionDateRange(CodeLookup distCycleLookup)
        {
            var fromdate = CurrentAdmissionAuthorizationInstance.EffectiveFromDate.Date;

            //NOTE: EffectiveToDate is NULLABLE, however it must be valued before generating a distributions
            var thrudate = CurrentAdmissionAuthorizationInstance.EffectiveToDate.GetValueOrDefault().Date;

            if (distCycleLookup.Code.ToLower().Contains("week"))
            {
                DayOfWeek startWeekDay = Get_StartWeekDay();
                return CalcWeekDateRange(fromdate, thrudate, startWeekDay).ToList();
            }

            return CalcMonthDateRange(fromdate, thrudate).ToList();
        }

        private DayOfWeek Get_StartWeekDay()
        {
            DayOfWeek weekStartDate = DayOfWeek.Sunday;
            string setting = TenantSettingsCache.Current.TenantSetting.WeekStartDay;
            switch (setting)
            {
                case "MONDAY":
                    weekStartDate = DayOfWeek.Monday;
                    break;
                case "TUESDAY":
                    weekStartDate = DayOfWeek.Tuesday;
                    break;
                case "WEDNESDAY":
                    weekStartDate = DayOfWeek.Wednesday;
                    break;
                case "THURSDAY":
                    weekStartDate = DayOfWeek.Thursday;
                    break;
                case "FRIDAY":
                    weekStartDate = DayOfWeek.Friday;
                    break;
                case "SATURDAY":
                    weekStartDate = DayOfWeek.Saturday;
                    break;
                default:
                    weekStartDate = DayOfWeek.Sunday;
                    break;
            }

            return weekStartDate;
        }

        public override void Cleanup()
        {
            base.Cleanup();

            DistributionCollection.Source = null;
            __DistributionCollectionBackingCollection.Clear();
            RaisePropertyChanged("DistributionCollection");

            MessengerInstance.Unregister(this);
        }

        private IEnumerable<Tuple<DateTime, DateTime>> CalcMonthDateRange(DateTime start, DateTime end)
        {
            Month startMonth = new Month(start);
            var realStartDate = start;
            var startMonthDate = startMonth.Start.Date;
            var endMonthDate = startMonth.End.Date;
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Start Date {0} - End Date {1}", start.ToString("MM/dd/yyyy"), end.ToString("MM/dd/yyyy"));
            System.Diagnostics.Debug.WriteLine("Initial Start Month Date {0}", startMonthDate.ToString("MM/dd/yyyy"));
#endif
            var bail = false;
            while (bail == false)
            {
                if (endMonthDate > end) //check for termination?
                {
                    endMonthDate = end;
                    bail = true;
                }

                //needed this in month generator for when the passed enddate is on a month end boundary
                if (realStartDate > endMonthDate)
                {
                    break;
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine("{0} - {1}", realStartDate.ToString("MM/dd/yyyy"),
                    endMonthDate.ToString("MM/dd/yyyy"));
#endif
                yield return Tuple.Create(realStartDate, endMonthDate);
                startMonth = startMonth.GetNextMonth();
                realStartDate = startMonthDate = startMonth.Start.Date;
                endMonthDate = startMonth.End.Date;
            }
        }

        private IEnumerable<Tuple<DateTime, DateTime>> CalcWeekDateRange(DateTime start, DateTime end,
            DayOfWeek firstDayOfWeek = DayOfWeek.Sunday)
        {
            var startWeekDate = TimeTool.GetStartOfWeek(start, firstDayOfWeek);
            var endWeekDate = startWeekDate.AddDays(6);
            var realStartDate = start;
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Start Date {0} - End Date {1} - {2}", start.ToString("MM/dd/yyyy"),
                end.ToString("MM/dd/yyyy"), firstDayOfWeek);
            System.Diagnostics.Debug.WriteLine("Initial Start Week Date {0}", startWeekDate.ToString("MM/dd/yyyy"));
#endif
            var bail = false;
            while (bail == false)
            {
                if (endWeekDate > end) //check for termination?
                {
                    endWeekDate = end;
                    bail = true;
                }

                //needed this in month generator for when the passed enddate is on a month end boundary
                if (realStartDate > endWeekDate) 
                {
                    break;
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine("{0} - {1}", realStartDate.ToString("MM/dd/yyyy"),
                    endWeekDate.ToString("MM/dd/yyyy"));
#endif
                yield return Tuple.Create(realStartDate, endWeekDate);

                realStartDate = startWeekDate = startWeekDate.AddDays(7);
                endWeekDate = startWeekDate.AddDays(6);
            }
        }
    }
}