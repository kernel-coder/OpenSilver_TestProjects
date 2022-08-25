#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.View;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Model
{
    public class SignatureOrderEntryGeneratedItem : GenericBase, ICleanup
    {
        public IOrderEntry CurrentOrderEntry { get; set; }
        public bool Enabled { get; set; }
        public string Label { get; set; }
        SectionUI _ReEvalSection;

        public SectionUI ReEvalSection
        {
            get { return _ReEvalSection; }
            set
            {
                _ReEvalSection = value;
                RaisePropertyChanged("ReEvalSection");
            }
        }

        private string CR = char.ToString('\r');

        public bool IsChecked
        {
            get
            {
                if (CurrentOrderEntry == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(CurrentOrderEntry.GeneratedOrderText))
                {
                    return false;
                }

                if (Label == "Referral / Visit Frequency Orders")
                {
                    return (CurrentOrderEntry.IsGeneratedVisitFrequency || CurrentOrderEntry.IsGeneratedReferral);
                }

                if (Label == "Goal and Treatment Orders")
                {
                    return CurrentOrderEntry.IsGeneratedGoals;
                }

                if (Label == "Labs/Test Orders")
                {
                    return CurrentOrderEntry.IsGeneratedLabs;
                }

                if (Label == "Initial Order for Start of Care")
                {
                    return CurrentOrderEntry.IsGeneratedInitialServiceOrder;
                }

                if (Label == "Medication Orders")
                {
                    return CurrentOrderEntry.IsGeneratedMedications;
                }

                if (Label == "Supplies / Equipment")
                {
                    return (CurrentOrderEntry.IsGeneratedEquipment || CurrentOrderEntry.IsGeneratedSupply ||
                            CurrentOrderEntry.IsGeneratedSupplyEquipment);
                }

                if (Label == "Other Orders")
                {
                    return CurrentOrderEntry.IsGeneratedOther;
                }

                if (Label == "Recertification Order")
                {
                    return CurrentOrderEntry.IsGeneratedRecertificationOrder;
                }

                return false;
            }
        }

        public void Refresh()
        {
            RaisePropertyChanged("IsChecked");
        }

        public override void Cleanup()
        {
            if (ReEvalSection != null)
            {
                ReEvalSection.Cleanup();
            }

            ReEvalSection = null;
            base.Cleanup();
        }
    }

    public class SignatureOrderEntry : QuestionUI, ISignature
    {
        public SignatureOrderEntry(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        #region ISignature

        public void SetupEncounterCollectedBy(string M0080, bool IsOasisActive, bool ChangeFlag)
        {
        }

        public void CalculateNewEncounterStatus(bool Signed)
        {
            if ((Encounter != null) && (Encounter.EncounterStatus == (int)EncounterStatusType.Edit) && (Signed))
            {
                Encounter.EncounterStatus = (int)EncounterStatusType.Completed;
            }
        }

        public bool GetTakeOffHold()
        {
            return true;
        }

        #endregion ISignature

        public IDynamicFormService Model => DynamicFormViewModel.FormModel;
        public OrderEntry CurrentOrderEntry { get; set; }
        public OrderEntryManager OrderEntryManager { get; set; }
        public RelayCommand<AdmissionPhysician> PhysicianDetailsCommand { get; set; }
        public RelayCommand<SignatureOrderEntryGeneratedItem> ReEvaluateCommand { get; set; }
        public RelayCommand RefreshOrderTextCommand { get; set; }
        public RelayCommand<string> RefreshOverrideTextCommand { get; protected set; }
        public RelayCommand OK_Command { get; set; }
        public RelayCommand Cancel_Command { get; set; }

        public int? SigningPhysicianKey
        {
            get
            {
                int? signingPhysicianKey = null;

                if ((CurrentOrderEntry != null)
                    && (CurrentOrderEntry.SigningPhysician != null)
                   )
                {
                    signingPhysicianKey = CurrentOrderEntry.SigningPhysician.PhysicianKey;
                }

                return signingPhysicianKey;
            }
            set
            {
                if (CurrentOrderEntry != null)
                {
                    if (value != CurrentOrderEntry.SigningPhysicianKey)
                    {
                        CurrentOrderEntry.SigningPhysicianKey = value;
                        //CreateEncounterAddendum();
                        RaisePropertyChanged("SigningPhysicianKey");
                        if (DynamicFormViewModel != null)
                        {
                            DynamicFormViewModel.RaisePropertyChangedPrintStuff();
                        }
                    }
                }
            }
        }

        #region Signing Physician Address

        private void SetupSelectedAdmissionPhysician()
        {
            SelectedAdmissionPhysician = SigningAdmissionPhysician;
        }

        public bool SignedByPhysicianChecked
        {
            get { return CurrentOrderEntry.SignedByPhysicianChecked; }
            set
            {
                CurrentOrderEntry.SignedByPhysicianChecked = value;
                RaisePropertyChanged("SignedByPhysicianChecked");
            }
        }

        public Physician SigningPhysician
        {
            get
            {
                if (CurrentOrderEntry != null)
                {
                    return PhysicianCache.Current.GetPhysicianFromKey(CurrentOrderEntry.SigningPhysicianKey);
                }

                return null;
            }
        }

        public string SentBy
        {
            get
            {
                if (CurrentOrderEntry == null)
                {
                    return null;
                }

                if (DynamicFormViewModel.OrdersTrackingChangeHistory == null)
                {
                    return null;
                }

                OrdersTracking ot = (CurrentOrderEntry.OrdersTracking == null)
                    ? null
                    : CurrentOrderEntry.OrdersTracking.Where(o => o.OrderEntryKey == CurrentOrderEntry.OrderEntryKey)
                        .FirstOrDefault();
                if (ot != null)
                {
                    List<ChangeHistory> chHistory = DynamicFormViewModel.OrdersTrackingChangeHistory.Where(h =>
                        h.UpdatedTable == "OrdersTracking" && h.UpdatedTableKey == ot.OrdersTrackingKey).ToList();
                    ChangeHistory ch = chHistory
                        .Where(a => a.ChangeHistoryDetail.Where(h => h.ChangedColumn == "Status" && h.NewValue == "40")
                            .Any()).OrderByDescending(h => h.ChangeHistoryKey).FirstOrDefault();
                    if (ch != null)
                    {
                        return UserCache.Current.GetFullNameFromUserId(ch.UpdatedBy);
                    }
                }

                return "Sent By Not Found";
            }
        }

        private bool _SentGridVisibility;

        public bool SentGridVisibility
        {
            get
            {
                if (CurrentOrderEntry == null || CurrentOrderEntry.OrderStatus == (int)OrderStatusType.Voided)
                {
                    return false;
                }

                return _SentGridVisibility;
            }
            set
            {
                _SentGridVisibility = value;
                RaisePropertyChanged("SentGridVisibility");
            }
        }

        public DateTime? OrderSentDate
        {
            get
            {
                if (CurrentOrderEntry == null)
                {
                    return null;
                }

                if (DynamicFormViewModel.OrdersTrackingChangeHistory == null)
                {
                    return null;
                }

                if (CurrentOrderEntry.OrderSentDate == null)
                {
                    OrdersTracking ot = (CurrentOrderEntry.OrdersTracking == null)
                        ? null
                        : CurrentOrderEntry.OrdersTracking
                            .Where(o => o.OrderEntryKey == CurrentOrderEntry.OrderEntryKey).FirstOrDefault();
                    if (ot != null)
                    {
                        List<ChangeHistory> chHistory = DynamicFormViewModel.OrdersTrackingChangeHistory.Where(h =>
                            h.UpdatedTable == "OrdersTracking" && h.UpdatedTableKey == ot.OrdersTrackingKey).ToList();
                        ChangeHistory ch = chHistory
                            .Where(a => a.ChangeHistoryDetail
                                .Where(h => h.ChangedColumn == "Status" && h.NewValue == "40").Any())
                            .OrderByDescending(h => h.ChangeHistoryKey).FirstOrDefault();
                        ChangeHistoryDetail chd = ((ch == null) || (ch.ChangeHistoryDetail == null))
                            ? null
                            : ch.ChangeHistoryDetail.Where(h => h.ChangedColumn == "StatusDate").FirstOrDefault();
                        if (chd != null)
                        {
                            DateTime? dt = null;
                            try
                            {
                                dt = Convert.ToDateTime(chd.NewValue);
                            }
                            catch
                            {
                            }

                            if (dt != null)
                            {
                                CurrentOrderEntry.OrderSentDate = dt;
                            }
                        }
                    }
                }

                RefreshSentGridVisibility();
                return CurrentOrderEntry.OrderSentDate;
            }
        }

        private void RefreshSentGridVisibility()
        {
            if (CurrentOrderEntry.OrdersTracking.Any() == false ||
                CurrentOrderEntry.OrdersTracking.FirstOrDefault() == null)
            {
                SentGridVisibility = false;
                return;
            }

            if (CurrentOrderEntry.OrdersTracking.FirstOrDefault().Status == (int)OrdersTrackingStatus.Void)
            {
                SentGridVisibility = false;
                return;
            }

            SentGridVisibility =
                (CurrentOrderEntry.OrdersTracking.FirstOrDefault().Status >= (int)OrdersTrackingStatus.Sent) &&
                CurrentOrderEntry.OrderSentDate.HasValue;
        }

        public AdmissionPhysician SigningAdmissionPhysician
        {
            get
            {
                if (CurrentOrderEntry != null && internalPhysicianList != null)
                {
                    var ap = internalPhysicianList
                        .Where(a => a.PhysicianKey == CurrentOrderEntry.SigningPhysicianKey &&
                                    a.PhysicianAddressKey == CurrentOrderEntry.SigningPhysicianAddressKey)
                        .FirstOrDefault();
                    // not found, use the first one.
                    if (ap == null)
                    {
                        ap = internalPhysicianList
                            .Where(a => a.PhysicianKey == CurrentOrderEntry.SigningPhysicianKey)
                            .FirstOrDefault();
                    }

                    if (ap == null && CurrentOrderEntry != null &&
                        CurrentOrderEntry.SigningPhysicianKey !=
                        null) // the physician no longer exists on the admission
                    {
                        AdmissionPhysician apnew = new AdmissionPhysician();
                        apnew.PhysicianKey = (int)CurrentOrderEntry.SigningPhysicianKey;
                        internalPhysicianList.Add(apnew);
                        SigningPhysicianList.Refresh();
                        ap = apnew;
                    }

                    return ap;
                }

                return null;
            }
        }

        private AdmissionPhysician _selectedAdmissionPhysician;

        public AdmissionPhysician SelectedAdmissionPhysician
        {
            get { return _selectedAdmissionPhysician; }
            set
            {
                _selectedAdmissionPhysician = value;
                SetEncounterAdmissionPhysicianAddressKey();
                RaiseSignedInformationPropertyChanged();
            }
        }

        private void RaiseSignedInformationPropertyChanged()
        {
            RaisePropertyChanged("SignPhyAddress1");
            RaisePropertyChanged("SignPhyAddress2");
            RaisePropertyChanged("SignPhyCityStateZip");
            RaisePropertyChanged("SignPhyPhoneNumber");
            RaisePropertyChanged("SignPhyFaxNumber");
            RaisePropertyChanged("SelectedAdmissionPhysician");
        }

        private void SetEncounterAdmissionPhysicianAddressKey()
        {
            int? addrKey = SelectedAdmissionPhysician == null ? null : SelectedAdmissionPhysician.PhysicianAddressKey;
            if ((addrKey == null) && (SelectedAdmissionPhysician != null))
            {
                Physician p = PhysicianCache.Current.GetPhysicianFromKey(SelectedAdmissionPhysician.PhysicianKey);
                if ((p != null) && (p.MainAddress != null))
                {
                    addrKey = p.MainAddress.PhysicianAddressKey;
                }
            }

            if ((CurrentOrderEntry != null) && (CurrentOrderEntry.SigningPhysicianAddressKey != addrKey))
            {
                CurrentOrderEntry.SigningPhysicianAddressKey = addrKey;
            }

            if (Encounter == null)
            {
                return;
            }

            if (Encounter.IsNew)
            {
                return;
            }

            if (Encounter.EncounterAdmission == null)
            {
                return;
            }

            var ea = Encounter.EncounterAdmission.FirstOrDefault();
            if (ea == null)
            {
                return;
            }

            ea.SigningPhysicianKey =
                SelectedAdmissionPhysician == null ? (int?)null : SelectedAdmissionPhysician.PhysicianKey;
            if (ea.SigningPhysicianAddressKey != addrKey)
            {
                ea.SigningPhysicianAddressKey = addrKey;
            }
        }

        public String SignPhyAddress1
        {
            get
            {
                if (SigningPhysician == null)
                {
                    return null;
                }

                if (SelectedAdmissionPhysician == null)
                {
                    if (SigningPhysician.MainAddress == null)
                    {
                        return null;
                    }

                    return SigningPhysician.MainAddress.Address1;
                }

                return SelectedAdmissionPhysician.PhysicianAddressOrMain.Address1;
            }
        }

        public String SignPhyAddress2
        {
            get
            {
                if (SigningPhysician == null)
                {
                    return null;
                }

                if (SelectedAdmissionPhysician == null)
                {
                    if (SigningPhysician.MainAddress == null)
                    {
                        return null;
                    }

                    return SigningPhysician.MainAddress.Address2;
                }

                return SelectedAdmissionPhysician.PhysicianAddressOrMain.Address2;
            }
        }

        public String SignPhyCityStateZip
        {
            get
            {
                if (SigningPhysician == null)
                {
                    return null;
                }

                if (SelectedAdmissionPhysician == null)
                {
                    if (SigningPhysician.MainAddress == null)
                    {
                        return null;
                    }

                    return SigningPhysician.MainAddress.CityStateZip;
                }

                return SelectedAdmissionPhysician.PhysicianAddressOrMain.CityStateZip;
            }
        }

        public String SignPhyPhoneNumber
        {
            get
            {
                if (SigningPhysician == null)
                {
                    return null;
                }

                if (SelectedAdmissionPhysician == null)
                {
                    if (SigningPhysician.MainPhone == null)
                    {
                        return null;
                    }

                    return SigningPhysician.MainPhone.Number;
                }

                return SelectedAdmissionPhysician.PhysicianAddressOrMain.PhoneNumber;
            }
        }

        public String SignPhyPhoneNumberExtension
        {
            get
            {
                if (SigningPhysician == null)
                {
                    return null;
                }

                if (SelectedAdmissionPhysician == null)
                {
                    if (SigningPhysician.MainPhone == null)
                    {
                        return null;
                    }

                    return SigningPhysician.MainPhone.Extension;
                }

                return SelectedAdmissionPhysician.PhysicianAddressOrMain.PhoneExtension;
            }
        }

        public String SignPhyFaxNumber
        {
            get
            {
                if (SigningPhysician == null)
                {
                    return null;
                }

                if (SelectedAdmissionPhysician == null)
                {
                    var FaxRow = SigningPhysician.GetPhoneByType("FAX");
                    if (FaxRow == null)
                    {
                        return null;
                    }

                    return FaxRow.Number;
                }

                return SelectedAdmissionPhysician.PhysicianAddressOrMain.Fax;
            }
        }

        #endregion

        private void CreateEncounterAddendum()
        {
            if ((OriginalEncounterStatus == (int)EncounterStatusType.Completed)
                && (CurrentOrderEntry != null)
                && (CurrentOrderEntry.SigningPhysicianKey != OriginalSigningPhysicianKey)
               )
            {
                EncounterAddendum ea = Encounter.EncounterAddendum.Where(add => add.IsNew).FirstOrDefault();

                if (ea == null)
                {
                    ea = new EncounterAddendum();
                }

                Physician oldPhysician = PhysicianCache.Current.GetPhysicianFromKey(OriginalSigningPhysicianKey);
                Physician newPhysician =
                    PhysicianCache.Current.GetPhysicianFromKey(CurrentOrderEntry.SigningPhysicianKey);
                ea.EncounterKey = Encounter.EncounterKey;
                ea.AddendumText = "Changed physician from " + oldPhysician.FormattedName + " to " +
                                  newPhysician.FormattedName;
                Encounter.EncounterAddendum.Add(ea);
            }
        }

        public bool ShowAddendumHistory
        {
            get
            {
                bool show = false;

                if ((Encounter != null)
                    && (Encounter.EncounterAddendum != null)
                    && Encounter.EncounterAddendum.Any()
                   )
                {
                    show = true;
                }

                return show;
            }
        }

        public ICollectionView SigningPhysicianList => signingPhysicianList.View;
        private CollectionViewSource signingPhysicianList = new CollectionViewSource();

        int? origPhyKey;

        public void RefreshPhysicians()
        {
            origPhyKey = SigningPhysicianKey;
            SigningPhysicianKey = null;
            SetupSigningPhysicianList();
            SigningPhysicianList.Refresh();
            SigningPhysicianKey = origPhyKey;
            origPhyKey = null;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.RaisePropertyChangedLambda(p => p.SigningPhysicianList);
            });
        }

        private int? origStatus;
        private string origReviewComments;

        public int GetOriginalSigningPhysicianKey()
        {
            return OriginalSigningPhysicianKey;
        }

        private int OriginalSigningPhysicianKey;
        private int OriginalEncounterStatus;
        private List<AdmissionPhysician> internalPhysicianList;

        public void SignatureOrderEntrySetup()
        {
            Messenger.Default.Register<int>(this,
                "AdmissionPhysician_FormUpdate",
                AdmissionKey => { RefreshPhysicians(); });

            if (OrderEntryManager != null)
            {
                OrderEntryManager.CompletedDateChanged += OrderEntryManager_CompletedDateChanged;
            }

            if (OrderEntryManager != null)
            {
                CurrentOrderEntry = OrderEntryManager.CurrentOrderEntry;
            }

            // If we are adding a new OrderEntry, do not default the SigningPhysicianKey and default the OrderEntry version
            DefaultCurrentOrderEntryValues();

            if (CurrentOrderEntry != null)
            {
                CurrentOrderEntry.PreviousGeneratedOther = CurrentOrderEntry.GeneratedOther;
                CurrentOrderEntry.PreviousGeneratedInitialServiceOrder = CurrentOrderEntry.GeneratedInitialServiceOrder;
                CurrentOrderEntry.PreviousGeneratedRecertificationOrder =
                    CurrentOrderEntry.GeneratedRecertificationOrder;
                CurrentOrderEntry.PreviousOrderStatus = CurrentOrderEntry.OrderStatus;
            }

            SetupSigningPhysicianList();
            if (Encounter != null)
            {
                OriginalEncounterStatus = Encounter.EncounterStatus;
            }

            if ((CurrentOrderEntry != null)
                && (CurrentOrderEntry.SigningPhysician != null)
               )
            {
                OriginalSigningPhysicianKey = CurrentOrderEntry.SigningPhysician.PhysicianKey;
            }

            Messenger.Default.Register<Encounter>(this, "EncounterSignatureChanged", e => EncounterSignatureChanged(e));

            if (CurrentOrderEntry != null)
            {
                CurrentOrderEntry.PropertyChanged += CurrentOrderEntry_PropertyChanged;
                origStatus = CurrentOrderEntry.OrderStatus;
                origReviewComments = CurrentOrderEntry.ReviewComment;
            }

            RefreshPhysicians();
            PhysicianDetailsCommand = new RelayCommand<AdmissionPhysician>(physician =>
            {
                if (physician == null)
                {
                    return;
                }

                PhysicianDisplay pd = new PhysicianDisplay
                    { Physician = physician.PhysicianProxy, AdmissionPhysician = physician };
                PhysicianDetailsDialog d = new PhysicianDetailsDialog(pd);
                d.Show();
            });
            ReEvaluateCommand = new RelayCommand<SignatureOrderEntryGeneratedItem>(generatedItem =>
            {
                if (generatedItem == null)
                {
                    return;
                }

                CurrentGeneratedItem = generatedItem;
                if (PartialValidateOrderEntry() == false)
                {
                    return;
                }

                if (CurrentGeneratedItem.Label == "Other Orders")
                {
                    CurrentOrderEntry.PreviousGeneratedOther = CurrentOrderEntry.GeneratedOther;
                }

                if (CurrentGeneratedItem.Label == "Initial Order for Start of Care")
                {
                    CurrentOrderEntry.PreviousGeneratedInitialServiceOrder =
                        CurrentOrderEntry.GeneratedInitialServiceOrder;
                }

                if (CurrentGeneratedItem.Label == "Recertification Order")
                {
                    CurrentOrderEntry.PreviousGeneratedRecertificationOrder =
                        CurrentOrderEntry.GeneratedRecertificationOrder;
                }

                ReevaluatePopupLabel = CurrentGeneratedItem.Label;
                ReEvalSection = CurrentGeneratedItem.ReEvalSection;
                DynamicFormViewModel.PopupDataContext = this;
            });
            OK_Command = new RelayCommand(() =>
            {
                if (ReEvalSection != null)
                {
                    if (ValidateReEvalSection(ReEvalSection) == false)
                    {
                        return;
                    }
                }

                if (CurrentGeneratedItem != null)
                {
                    if (OrderEntryManager != null)
                    {
                        OrderEntryManager.UpdateGeneratedOrderText();
                    }

                    CurrentGeneratedItem.Refresh();
                    CurrentGeneratedItem = null;
                }

                DynamicFormViewModel.PopupDataContext = null;
            });

            Cancel_Command = new RelayCommand(() =>
            {
                if (ReEvalSection != null)
                {
                    if (ValidateReEvalSection(ReEvalSection) == false)
                    {
                        return;
                    }
                }

                if (CurrentGeneratedItem != null)
                {
                    if (CurrentGeneratedItem.Label == "Other Orders")
                    {
                        CurrentOrderEntry.GeneratedOther = CurrentOrderEntry.PreviousGeneratedOther;
                    }

                    if (CurrentGeneratedItem.Label == "Initial Order for Start of Care")
                    {
                        CurrentOrderEntry.GeneratedInitialServiceOrder =
                            CurrentOrderEntry.PreviousGeneratedInitialServiceOrder;
                    }

                    if (CurrentGeneratedItem.Label == "Recertification Order")
                    {
                        CurrentOrderEntry.GeneratedRecertificationOrder =
                            CurrentOrderEntry.PreviousGeneratedRecertificationOrder;
                    }

                    CurrentGeneratedItem.Refresh();
                    CurrentGeneratedItem = null;
                }

                DynamicFormViewModel.PopupDataContext = null;
            });
            RefreshOrderTextCommand = new RelayCommand(() =>
            {
                if (CurrentOrderEntry != null)
                {
                    if (CurrentOrderEntry.CanEditOrderData)
                    {
                        CurrentOrderEntry.OrderText = CurrentOrderEntry.GeneratedOrderText;
                    }
                }
            });
            RefreshOverrideTextCommand = new RelayCommand<string>(whatToRefresh =>
            {
                if ((CurrentOrderEntry == null) || (CurrentOrderEntry.CanEditOrderData == false) ||
                    (string.IsNullOrWhiteSpace(whatToRefresh)))
                {
                    return;
                }

                if ((whatToRefresh == "Referral") && (CurrentOrderEntry.CanRefreshOverrideReferral))
                {
                    CurrentOrderEntry.OverrideReferral = CurrentOrderEntry.GeneratedReferral;
                }
                else if ((whatToRefresh == "VisitFrequency") && (CurrentOrderEntry.CanRefreshOverrideVisitFrequency))
                {
                    CurrentOrderEntry.OverrideVisitFrequency = CurrentOrderEntry.GeneratedVisitFrequency;
                }
                else if ((whatToRefresh == "Goals") && (CurrentOrderEntry.CanRefreshOverrideGoals))
                {
                    CurrentOrderEntry.OverrideGoals = CurrentOrderEntry.GeneratedGoals;
                }
                else if ((whatToRefresh == "Labs") && (CurrentOrderEntry.CanRefreshOverrideLabs))
                {
                    CurrentOrderEntry.OverrideLabs = CurrentOrderEntry.GeneratedLabs;
                }
                else if ((whatToRefresh == "InitialServiceOrder") &&
                         (CurrentOrderEntry.CanRefreshOverrideInitialServiceOrder))
                {
                    CurrentOrderEntry.OverrideInitialServiceOrder =
                        (string.IsNullOrWhiteSpace(CurrentOrderEntry.GeneratedInitialServiceOrder) == false)
                            ? "Initial Order for Start of Care:" + CR + CurrentOrderEntry.GeneratedInitialServiceOrder
                            : null;
                }
                else if ((whatToRefresh == "Medications") && (CurrentOrderEntry.CanRefreshOverrideMedications))
                {
                    CurrentOrderEntry.OverrideMedications = CurrentOrderEntry.GeneratedMedications;
                }
                else if ((whatToRefresh == "Equipment") && (CurrentOrderEntry.CanRefreshOverrideEquipment))
                {
                    CurrentOrderEntry.OverrideEquipment = CurrentOrderEntry.GeneratedEquipment;
                }
                else if ((whatToRefresh == "Supply") && (CurrentOrderEntry.CanRefreshOverrideSupply))
                {
                    CurrentOrderEntry.OverrideSupply = CurrentOrderEntry.GeneratedSupply;
                }
                else if ((whatToRefresh == "Other") && (CurrentOrderEntry.CanRefreshOverrideOther))
                {
                    CurrentOrderEntry.OverrideOther =
                        (string.IsNullOrWhiteSpace(CurrentOrderEntry.GeneratedOther) == false)
                            ? "Other Orders:" + CR + CurrentOrderEntry.GeneratedOther
                            : null;
                }
                else if ((whatToRefresh == "RecertificationOrder") &&
                         (CurrentOrderEntry.CanRefreshOverrideRecertificationOrder))
                {
                    CurrentOrderEntry.OverrideRecertificationOrder =
                        (string.IsNullOrWhiteSpace(CurrentOrderEntry.GeneratedRecertificationOrder) == false)
                            ? "Recertification Order:" + CR + CurrentOrderEntry.GeneratedRecertificationOrder
                            : null;
                }
            });
            if (CurrentOrderEntry == null)
            {
                MessageBox.Show(
                    "SignatureOrderEntry.SignatureOrderEntrySetup Error:  CurrentOrderEntry is null, SignatureOrderEntry question is only val1d with OrderEntry Forms, contact AlayaCare support.");
            }

            SignatureOrderEntrySetupGeneratedSections();

            SetupSelectedAdmissionPhysician();
        }

        private void DefaultCurrentOrderEntryValues()
        {
            if ((CurrentOrderEntry != null) && (CurrentOrderEntry.IsNew))
            {
                CurrentOrderEntry.OrderEntryVersion = 2;
                CurrentOrderEntry.SigningPhysicianKey = null;
            }
        }

        private string CR = char.ToString('\r');

        public void SetupSigningPhysicianList()
        {
            signingPhysicianList.SortDescriptions.Clear();
            signingPhysicianList.SortDescriptions.Add(new SortDescription("PhysicianName",
                ListSortDirection.Ascending));

            internalPhysicianList = new List<AdmissionPhysician>();

            // Add an empty (nullable) item to Physician list
            internalPhysicianList.Add(new AdmissionPhysician
            {
                AdmissionPhysicianKey = 0
            });

            // Pull remaining Physicians from the Admission
            if (OrderEntryManager != null && OrderEntryManager.CurrentAdmission != null &&
                OrderEntryManager.CurrentAdmission.AdmissionPhysician != null)
            {
                internalPhysicianList.AddRange(OrderEntryManager.CurrentAdmission.AdmissionPhysician.ToList());
            }

            signingPhysicianList.Source = internalPhysicianList;
            signingPhysicianList.Filter += signingPhysicianList_Filter;
        }

        void CurrentOrderEntry_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CoSign")
            {
                RaisePropertyChanged("IsCoSignVisible");
            }
            else if (e.PropertyName == "IsReviewed")
            {
                RaisePropertyChanged("IsReviewOrderVisible");
            }
        }

        void signingPhysicianList_Filter(object sender, FilterEventArgs e)
        {
            AdmissionPhysician phy = e.Item as AdmissionPhysician;

            if (phy == null)
            {
                e.Accepted = false;
            }
            else
            {
                if ((origPhyKey != null)
                    && (origPhyKey == phy.PhysicianKey)
                   )
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = (!phy.Inactive);

                    if (e.Accepted)
                    {
                        e.Accepted = (CurrentOrderEntry != null);
                    }

                    if (e.Accepted)
                    {
                        e.Accepted = (CurrentOrderEntry != null)
                                     && (CurrentOrderEntry.CompletedDate != null);
                    }

                    if (e.Accepted)
                    {
                        e.Accepted = ((phy.PhysicianEffectiveFromDate == null)
                                      || (phy.PhysicianEffectiveFromDate.Date <=
                                          CurrentOrderEntry.CompletedDate.Value.Date)
                            );
                    }

                    if (e.Accepted)
                    {
                        e.Accepted = ((phy.PhysicianEffectiveThruDate == null)
                                      || (phy.PhysicianEffectiveThruDate.Value.Date >=
                                          CurrentOrderEntry.CompletedDate.Value.Date)
                            );
                    }
                }
            }
        }

        void OrderEntryManager_CompletedDateChanged(object sender, EventArgs e)
        {
            RefreshPhysicians();
            if ((CurrentOrderEntry.CompletedDate != null) && (Encounter != null) &&
                (OriginalEncounterStatus != (int)EncounterStatusType.Completed))
            {
                if (Encounter.EncounterStartDate != CurrentOrderEntry.CompletedDate)
                {
                    Encounter.EncounterStartDate = CurrentOrderEntry.CompletedDate;
                }

                if (Encounter.EncounterStartTime != CurrentOrderEntry.CompletedDate)
                {
                    Encounter.EncounterStartTime = CurrentOrderEntry.CompletedDate;
                }

                if (Encounter.EncounterEndDate != CurrentOrderEntry.CompletedDate)
                {
                    Encounter.EncounterEndDate = CurrentOrderEntry.CompletedDate;
                }

                if (Encounter.EncounterEndTime != CurrentOrderEntry.CompletedDate)
                {
                    Encounter.EncounterEndTime = CurrentOrderEntry.CompletedDate;
                }
            }
        }

        private bool PartialValidateOrderEntry()
        {
            bool isValid = true;
            if (CurrentOrderEntry == null)
            {
                return isValid;
            }

            ClearErrorsForOrderEntrySignature();
            if ((CurrentOrderEntry.SigningPhysicianKey == null) || (CurrentOrderEntry.SigningPhysicianKey <= 0))
            {
                AddErrorForProperty("SigningPhysicianKey",
                    "The Signing Physician is required before Order Types can be added");
                isValid = false;
            }

            if (CurrentOrderEntry.CompletedDate == null)
            {
                CurrentOrderEntry.ValidationErrors.Add(new System.ComponentModel.DataAnnotations.ValidationResult(
                    "The Completed Date/Time is required before Order Types can be added", new[] { "CompletedDate" }));
                isValid = false;
            }
            else
            {
                if (CurrentOrderEntry.IsVoided == false)
                {
                    if ((Admission != null) && (Admission.DischargeDateTime != null) &&
                        (((DateTime)Admission.DischargeDateTime).Date <
                         ((DateTimeOffset)CurrentOrderEntry.CompletedDate).Date))
                    {
                        CurrentOrderEntry.ValidationErrors.Add(
                            new System.ComponentModel.DataAnnotations.ValidationResult(
                                "The Completed Date/Time cannot be after the discharge date.",
                                new[] { "CompletedDate" }));
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        private SignatureOrderEntryGeneratedItem CurrentGeneratedItem;

        #region GeneratedSections

        string _ReevaluatePopupLabel = "ReEvaluatePopupDataTemplate";

        public string ReevaluatePopupLabel
        {
            get { return _ReevaluatePopupLabel; }
            set
            {
                _ReevaluatePopupLabel = value;
                RaisePropertyChanged("ReevaluatePopupLabel");
            }
        }

        private string _PopupDataTemplate = "ReEvaluatePopupDataTemplate";

        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                _PopupDataTemplate = value;
                RaisePropertyChanged("PopupDataTemplate");
                RaisePropertyChanged("PopupDataTemplateLoaded");
            }
        }

        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject PopupDataTemplateLoaded
        {
            get
            {
                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }

                //Causing infinate binding loop - if (DataTemplateHelper.IsDataTemplateLoaded(PopupDataTemplate))
                //{
                //    Deployment.Current.Dispatcher.BeginInvoke(() =>
                //    {
                //        this.RaisePropertyChanged(null);
                //    });
                //}
                return DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }

        ObservableCollection<SignatureOrderEntryGeneratedItem> _GeneratedSectionsList;

        public ObservableCollection<SignatureOrderEntryGeneratedItem> GeneratedSectionsList
        {
            get { return _GeneratedSectionsList; }
            set
            {
                _GeneratedSectionsList = value;
                RaisePropertyChanged("GeneratedSectionsList");
            }
        }

        SectionUI _ReEvalSection;

        public SectionUI ReEvalSection
        {
            get { return _ReEvalSection; }
            set
            {
                _ReEvalSection = value;
                this.RaisePropertyChangedLambda(p => p.ReEvalSection);
            }
        }

        private void SignatureOrderEntrySetupGeneratedSections()
        {
            ObservableCollection<SignatureOrderEntryGeneratedItem> gList =
                new ObservableCollection<SignatureOrderEntryGeneratedItem>();
            int sequence = 4;

            AddGeneratedSection(gList, sequence++, "Referral / Visit Frequency Orders", "AdmissionDisciplineFrequency",
                "PatientCollectionBase");
            AddGeneratedSection(gList, sequence++, "Goal and Treatment Orders", "Rehab", "Rehab");
            AddGeneratedSection(gList, sequence++, "Labs/Test Orders", "Labs", "PatientCollectionBase");
            AddGeneratedSection(gList, sequence++, "Initial Order for Start of Care",
                "OrderEntryGeneratedInitialServiceOrder", "QuestionBase");
            AddGeneratedSection(gList, sequence++, "Medication Orders", "Medication", "PatientCollectionBase");
            AddGeneratedSection(gList, sequence++, "Supplies / Equipment", "OrderEntrySupplyEquipment",
                "OrderEntrySupplyEquipment");
            AddGeneratedSection(gList, sequence++, "Other Orders", "OrderEntryGeneratedOther", "QuestionBase");
            AddGeneratedSection(gList, sequence++, "Recertification Order", "OrderEntryGeneratedRecertificationOrder",
                "QuestionBase");

            GeneratedSectionsList = gList;
        }

        private void AddGeneratedSection(ObservableCollection<SignatureOrderEntryGeneratedItem> gList, int sequence,
            string label, string dataTemplate, string backingFactory)
        {
            ObservableCollection<SectionUI> sections = new ObservableCollection<SectionUI>();
            ProcessFormGeneratedSectionUI(sections, true, false, sequence++, label, dataTemplate, backingFactory);
            SignatureOrderEntryGeneratedItem gi = new SignatureOrderEntryGeneratedItem
            {
                Label = label,
                CurrentOrderEntry = CurrentOrderEntry,
                ReEvalSection = sections.FirstOrDefault(),
            };
            if (gi.ReEvalSection != null)
            {
                gi.ReEvalSection.IsOrderEntry = true;
            }

            gList.Add(gi);
        }

        private void ProcessFormGeneratedSectionUI(ObservableCollection<SectionUI> sections, bool hidelabel,
            bool addtoprint, int sequence, string label, string dataTemplate, string backingFactory)
        {
            ObservableCollection<QuestionUI> ocq = new ObservableCollection<QuestionUI>();
            Section s = new Section { SectionKey = 0, TenantID = Encounter.TenantID, };
            FormSection section = new FormSection
            {
                FormSectionKey = 0, FormKey = (int)Encounter.FormKey, Section = s, SectionKey = 0, Sequence = sequence
            };

            var task_key = (Encounter == null) ? 0 : Encounter.TaskKey.GetValueOrDefault();
            SectionUI sui = new SectionUI(task_key) { Label = label, PatientDemographics = false, Questions = ocq };

            Question temp = new Question
                { Label = label, DataTemplate = "SectionLabel", BackingFactory = "SectionLabel" };
            var qsl = DynamicFormViewModel.DynamicQuestionFactory(section, -1, -1, section.SectionKey.Value, false,
                temp, ocq);
            qsl.Question = temp;
            qsl.IndentLevel = 0;
            qsl.Label = label;
            qsl.Hidden = hidelabel;

            sections.Add(sui);
            ocq.Add(qsl);

            if (label == "Referral / Visit Frequency Orders")
            {
                Question oq = new Question
                {
                    QuestionKey = 0, Label = "Discipline Referrals", DataTemplate = "DisciplineReferral",
                    BackingFactory = "DisciplineReferral"
                };
                oq.ProtectedOverride = false;
                QuestionUI q =
                    DynamicFormViewModel.DynamicQuestionFactory(section, -1, -1, section.Sequence, false, oq, ocq);
                q.GoalManager = DynamicFormViewModel.CurrentGoalManager;
                q.IndentLevel = 1;
                q.Label = oq.Label;
                q.Required = false;
                q.OasisManager = DynamicFormViewModel.CurrentOasisManager;
                q.SetupMessages();
                ocq.Add(q);
            }

            Question oq2 = new Question
                { QuestionKey = 0, Label = label, DataTemplate = dataTemplate, BackingFactory = backingFactory };
            oq2.ProtectedOverride = false;
            QuestionUI q2 =
                DynamicFormViewModel.DynamicQuestionFactory(section, -1, -1, section.Sequence, false, oq2, ocq);
            q2.GoalManager = DynamicFormViewModel.CurrentGoalManager;
            q2.IndentLevel = 1;
            q2.Label = oq2.Label;
            q2.Required = false;
            q2.OasisManager = DynamicFormViewModel.CurrentOasisManager;
            q2.SetupMessages();
            ocq.Add(q2);
        }

        #endregion GeneratedSections

        public bool IsSignatureEnabled
        {
            get
            {
                if (OriginalEncounterStatus == (int)EncounterStatusType.Edit)
                {
                    return (Encounter.EncounterBy == WebContext.Current.User.MemberID) ? true : false;
                }

                return false;
            }
        }

        public bool IsSignatureVisible => true;

        public bool IsReviewOrderVisible
        {
            get
            {
                bool isReviewOrderVisible = false;

                if (origStatus == (int)OrderStatusType.OrderEntryReview)
                {
                    isReviewOrderVisible = RoleAccessHelper.CheckPermission(RoleAccess.OrderEntryReviewer, false);
                }
                else if (((origStatus == (int)OrderStatusType.Completed)
                          || (origStatus == (int)OrderStatusType.SigningPhysicianVerified)
                          || (origStatus == (int)OrderStatusType.Voided)
                         )
                         && !string.IsNullOrEmpty(origReviewComments)
                        )
                {
                    isReviewOrderVisible = true;
                }

                return isReviewOrderVisible;
            }
        }

        public bool IsCoSignVisible
        {
            get
            {
                bool isCoSignVisible = IsReviewOrderVisible;

                if (isCoSignVisible)
                {
                    isCoSignVisible = CurrentOrderEntry.IsReviewed;
                }

                return isCoSignVisible;
            }
        }

        public void EncounterSignatureChanged(Encounter e)
        {
            if (e == null)
            {
                return;
            }

            if (e != Encounter)
            {
            }
        }

        public override bool CopyForwardLastInstance()
        {
            return false;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
        }

        public override void BackupEntity(bool restore)
        {
        }

        public bool ValidateReEvalSection(SectionUI reEvalSection)
        {
            bool returnStatus = true;
            foreach (QuestionUI q in reEvalSection.Questions)
            {
                string ErrorSection = string.Empty;

                if (!q.Validate(out ErrorSection))
                {
                    returnStatus = false;
                }
            }

            return returnStatus;
        }

        public override bool Validate(out string SubSections)
        {
            bool returnStatus = true;

            SubSections = string.Empty;
            ValidationError = string.Empty;

            PreValidate();

            foreach (SignatureOrderEntryGeneratedItem gi in GeneratedSectionsList)
            {
                foreach (QuestionUI q in gi.ReEvalSection.Questions)
                {
                    string ErrorSection = string.Empty;

                    if (!q.Validate(out ErrorSection))
                    {
                        if (string.IsNullOrEmpty(SubSections))
                        {
                            SubSections = gi.ReEvalSection.Label;
                        }

                        returnStatus = false;
                    }
                }
            }

            // refresh order text incase of override edits
            if (OrderEntryManager != null)
            {
                OrderEntryManager.UpdateGeneratedOrderText();
            }

            // Clear errors, will re-add if necessary
            ClearErrorsForOrderEntrySignature();

            VirtuosoEntity veCurrentOrderEntry = CurrentOrderEntry;
            if (veCurrentOrderEntry != null)
            {
                returnStatus = CurrentOrderEntry.Validate();
            }

            if (CurrentOrderEntry.IsVoided == false && CurrentOrderEntry.CompletedDate != null)
            {
                if ((Admission != null) && (Admission.DischargeDateTime != null) &&
                    (((DateTime)Admission.DischargeDateTime).Date <
                     ((DateTimeOffset)CurrentOrderEntry.CompletedDate).Date))
                {
                    CurrentOrderEntry.ValidationErrors.Add(new System.ComponentModel.DataAnnotations.ValidationResult(
                        "The Completed Date/Time cannot be after the discharge date.", new[] { "CompletedDate" }));
                    returnStatus = false;
                }
            }

            // SigningPhysician is required
            if (!CurrentOrderEntry.IsVoided)
            {
                if (!CurrentOrderEntry.SigningPhysicianKey.HasValue || CurrentOrderEntry.SigningPhysicianKey.Value == 0)
                {
                    AddErrorForProperty("SigningPhysicianKey", "The Signing Physician field is required.");
                    returnStatus = false;
                }
            }

            if (returnStatus == false)
            {
                ValidateFailed();
            }

            return returnStatus;
        }

        private void ClearErrorsForOrderEntrySignature()
        {
            CurrentOrderEntry.ValidationErrors.Clear();
            ClearErrorFromProperty("SigningPhysicianKey");
        }

        public void PreValidate()
        {
            if (CurrentOrderEntry == null)
            {
                return;
            }

            if (CurrentOrderEntry.IsVoided)
            {
                if (Admission != null)
                {
                    if (Admission.AdmissionDisciplineFrequency != null)
                    {
                        var adfList = Admission.AdmissionDisciplineFrequency.Where(adf =>
                            (adf.AddedFromEncounterKey == CurrentOrderEntry.AddedFromEncounterKey)
                            && (!adf.Inactive)
                        );
                        foreach (AdmissionDisciplineFrequency adf in adfList)
                        {
                            adf.BeginEditting();
                            adf.Inactive = true;
                            adf.InactiveDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                            adf.EndEditting();
                        }
                    }
                }
            }


            if (CurrentOrderEntry.CoSign == false)
            {
                ClearCoSignature();
            }

            CurrentOrderEntry.PreviousOrderStatus = CurrentOrderEntry.OrderStatus;
            if (CurrentOrderEntry.IsVoided)
            {
                CurrentOrderEntry.OrderStatus = (int)OrderStatusType.Voided;
            }
            else if (CurrentOrderEntry.IsSigningPhysicianVerified)
            {
                CurrentOrderEntry.OrderStatus = (int)OrderStatusType.SigningPhysicianVerified;
            }
            else if (CurrentOrderEntry.IsReviewed)
            {
                CurrentOrderEntry.OrderStatus = (int)OrderStatusType.Completed;
            }
            else if (CurrentOrderEntry.IsSigned)
            {
                if (SetUpToUseOrderEntryReviewers)
                {
                    if (Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed)
                    {
                        CurrentOrderEntry.OrderStatus = (UsingOrderEntryReviewers)
                            ? (int)OrderStatusType.OrderEntryReview
                            : (int)OrderStatusType.Completed;
                    }
                    else
                    {
                        CurrentOrderEntry.OrderStatus = (CurrentOrderEntry.IsReviewed)
                            ? (int)OrderStatusType.Completed
                            : (int)OrderStatusType.OrderEntryReview;
                    }
                }
                else
                {
                    CurrentOrderEntry.OrderStatus = (int)OrderStatusType.Completed;
                }
            }
            else
            {
                CurrentOrderEntry.OrderStatus = (int)OrderStatusType.InProcess;
            }

            CurrentOrderEntry.RaiseChanged();
        }

        private void ClearCoSignature()
        {
            if (OrderEntryManager == null)
            {
                return;
            }

            if (OrderEntryManager.FormModel == null)
            {
                return;
            }

            if (CurrentOrderEntry.OrderEntryCoSignature == null)
            {
                return;
            }

            OrderEntryCoSignature s = CurrentOrderEntry.OrderEntryCoSignature.FirstOrDefault();
            if (s != null)
            {
                OrderEntryManager.FormModel.RemoveOrderEntryCoSignature(s);
            }
        }

        public bool UsingOrderEntryReviewers
        {
            get
            {
                // if user is an OrderEntryReviewer we are not using them (they can police themselves)
                if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEntryReviewer, false))
                {
                    return false;
                }

                return SetUpToUseOrderEntryReviewers;
            }
        }

        public bool SetUpToUseOrderEntryReviewers
        {
            get
            {
                // Check if user, serviceLineGrouping, or tenant is using OrderEntryReviewer
                UserProfile up = UserCache.Current.GetUserProfileFromUserId(Encounter.EncounterBy);
                if (up != null)
                {
                    if (up.UsingOrderEntryReviewers)
                    {
                        return true;
                    }
                }

                if (Admission != null)
                {
                    if (Admission.UsingOrderEntryReviewersAtServiceLineGroupings(null))
                    {
                        return true;
                    }
                }

                if (TenantSettingsCache.Current.TenantSetting.UsingOrderEntryReviewers)
                {
                    return true;
                }

                return false;
            }
        }

        public void ValidateFailed()
        {
            if (CurrentOrderEntry == null)
            {
                return;
            }

            CurrentOrderEntry.OrderStatus = CurrentOrderEntry.PreviousOrderStatus;
            CurrentOrderEntry.RaiseChanged();
        }

        public Virtuoso.Services.Core.Model.User User => WebContext.Current.User;

        public override void Cleanup()
        {
            if (OrderEntryManager != null)
            {
                OrderEntryManager.CompletedDateChanged -= OrderEntryManager_CompletedDateChanged;
            }

            Messenger.Default.Unregister<int>(this, "AdmissionPhysician_FormUpdate");
            Messenger.Default.Unregister<Encounter>(this, "EncounterSignatureChanged");
            Messenger.Default.Unregister(this);

            if (ReEvalSection != null)
            {
                ReEvalSection.Cleanup();
            }

            ReEvalSection = null;
            if (CurrentGeneratedItem != null)
            {
                CurrentGeneratedItem.Cleanup();
            }

            CurrentGeneratedItem = null;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (GeneratedSectionsList != null)
                {
                    GeneratedSectionsList.ForEach(s => s.Cleanup());
                }

                if (CurrentOrderEntry != null)
                {
                    CurrentOrderEntry = null;
                }
            });
            base.Cleanup();
        }

        public override QuestionUI Clone()
        {
            SignatureOrderEntry pcb = new SignatureOrderEntry(__FormSectionQuestionKey)
            {
                Question = Question,
                IndentLevel = IndentLevel,
                Patient = Patient,
                Encounter = Encounter,
                Admission = Admission,
                DynamicFormViewModel = DynamicFormViewModel,
            };
            pcb.IsClonedQuestion = true;
            pcb.CurrentOrderEntry = CurrentOrderEntry;

            return pcb;
        }

        // TODO: Next time we modify a question to add this functionality, move GetFaxingPhysician() methods from all Questions to QuestionUI or its base

        public FaxingPhysician? GetFaxingPhysician()
        {
            if (CurrentOrderEntry == null)
            {
                return null;
            }

            if (CurrentOrderEntry.SigningPhysicianKey == null)
            {
                return null;
            }

            if (CurrentOrderEntry.SigningPhysicianAddressKey == null)
            {
                return null;
            }

            PhysicianAddress physicianAddress =
                PhysicianCache.Current.GetPhysicianAddressFromKey(CurrentOrderEntry.SigningPhysicianAddressKey);
            if (physicianAddress != null && string.IsNullOrWhiteSpace(physicianAddress.Fax) == false)
            {
                return new FaxingPhysician
                    { PhysicianKey = SigningPhysicianKey.Value, FaxNumber = physicianAddress.Fax };
            }

            return null;
        }
    }

    public class SignatureOrderEntryFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            SignatureOrderEntry s = new SignatureOrderEntry(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                OrderEntryManager = vm.CurrentOrderEntryManager,
                DynamicFormViewModel = vm,
            };
            vm.SignatureQuestion = s;
            s.SignatureOrderEntrySetup();
            return s;
        }
    }
}