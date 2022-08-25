#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
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
    public class SignatureOrderEntryVO : QuestionUI
    {
        public SignatureOrderEntryVO(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public IDynamicFormService Model => DynamicFormViewModel.FormModel;
        public OrderEntryVO CurrentOrderEntryVO { get; set; }
        public OrderEntryManager OrderEntryManager { get; set; }
        public RelayCommand<AdmissionPhysician> PhysicianDetailsCommand { get; set; }
        public RelayCommand RefreshOrderTextCommand { get; set; }
        public RelayCommand LoadedSignatureOrderEntryVOCommand { get; set; }
        public RelayCommand<string> RefreshOverrideTextCommand { get; protected set; }
        public RelayCommand<SignatureOrderEntryGeneratedItem> ReEvaluateCommand { get; set; }

        public RelayCommand OK_Command { get; set; }
        public RelayCommand Cancel_Command { get; set; }

        public int? SigningPhysicianKey
        {
            get
            {
                int? signingPhysicianKey = null;

                if ((CurrentOrderEntryVO != null)
                    && (CurrentOrderEntryVO.SigningPhysician != null)
                   )
                {
                    signingPhysicianKey = CurrentOrderEntryVO.SigningPhysician.PhysicianKey;
                }

                return signingPhysicianKey;
            }
            set
            {
                if (CurrentOrderEntryVO != null)
                {
                    if (value != CurrentOrderEntryVO.SigningPhysicianKey)
                    {
                        CurrentOrderEntryVO.SigningPhysicianKey = value;
                        RaisePropertyChanged("SigningPhysicianKey");
                    }
                }
            }
        }

        #region Signing Physician Address

        private void SetupSelectedAdmissionPhysician()
        {
            SelectedAdmissionPhysician = SigningAdmissionPhysician;
        }

        public Physician SigningPhysician
        {
            get
            {
                if (CurrentOrderEntryVO != null)
                {
                    return PhysicianCache.Current.GetPhysicianFromKey(CurrentOrderEntryVO.SigningPhysicianKey);
                }

                return null;
            }
        }

        public AdmissionPhysician SigningAdmissionPhysician
        {
            get
            {
                if (CurrentOrderEntryVO != null && internalPhysicianList != null)
                {
                    var ap = internalPhysicianList
                        .Where(a => a.PhysicianKey == CurrentOrderEntryVO.SigningPhysicianKey &&
                                    a.PhysicianAddressKey == CurrentOrderEntryVO.SigningPhysicianAddressKey)
                        .FirstOrDefault();
                    // not found, use the first one.
                    if (ap == null)
                    {
                        ap = internalPhysicianList
                            .Where(a => a.PhysicianKey == CurrentOrderEntryVO.SigningPhysicianKey)
                            .FirstOrDefault();
                    }

                    if (ap == null && CurrentOrderEntryVO != null &&
                        CurrentOrderEntryVO.SigningPhysicianKey !=
                        null) // the physician no longer exists on the admission
                    {
                        AdmissionPhysician apnew = new AdmissionPhysician();
                        apnew.PhysicianKey = (int)CurrentOrderEntryVO.SigningPhysicianKey;
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

            if ((CurrentOrderEntryVO != null) && (CurrentOrderEntryVO.SigningPhysicianAddressKey != addrKey))
            {
                CurrentOrderEntryVO.SigningPhysicianAddressKey = addrKey;
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

            ea.SigningPhysicianKey = SelectedAdmissionPhysician?.PhysicianKey;
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

        public ICollectionView SigningPhysicianList => signingPhysicianList.View;
        private CollectionViewSource signingPhysicianList = new CollectionViewSource();

        int? origPhyKey;

        public void RefreshPhysicians()
        {
            origPhyKey = SigningPhysicianKey;
            SigningPhysicianKey = null;
            SigningPhysicianList.Refresh();
            SigningPhysicianKey = origPhyKey;
            origPhyKey = null;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.RaisePropertyChangedLambda(p => p.SigningPhysicianList);
            });
        }

        private int OriginalSigningPhysicianKey;
        private List<AdmissionPhysician> internalPhysicianList;

        public void SignatureOrderEntryVOSetup()
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
                CurrentOrderEntryVO = OrderEntryManager.CurrentOrderEntryVO;
            }

            if ((CurrentOrderEntryVO != null) && (CurrentOrderEntryVO.IsNew))
            {
                CurrentOrderEntryVO.OrderEntryVersion = 2;
            }

            if (CurrentOrderEntryVO != null)
            {
                CurrentOrderEntryVO.PreviousGeneratedOther = CurrentOrderEntryVO.GeneratedOther;
                CurrentOrderEntryVO.PreviousGeneratedInitialServiceOrder =
                    CurrentOrderEntryVO.GeneratedInitialServiceOrder;
                CurrentOrderEntryVO.PreviousGeneratedRecertificationOrder =
                    CurrentOrderEntryVO.GeneratedRecertificationOrder;
            }

            SetupSigningPhysicianList();
            if ((CurrentOrderEntryVO != null)
                && (CurrentOrderEntryVO.SigningPhysician != null)
               )
            {
                OriginalSigningPhysicianKey = CurrentOrderEntryVO.SigningPhysician.PhysicianKey;
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
                    CurrentOrderEntryVO.PreviousGeneratedOther = CurrentOrderEntryVO.GeneratedOther;
                }

                if (CurrentGeneratedItem.Label == "Initial Order for Start of Care")
                {
                    CurrentOrderEntryVO.PreviousGeneratedInitialServiceOrder =
                        CurrentOrderEntryVO.GeneratedInitialServiceOrder;
                }

                if (CurrentGeneratedItem.Label == "Recertification Order")
                {
                    CurrentOrderEntryVO.PreviousGeneratedRecertificationOrder =
                        CurrentOrderEntryVO.GeneratedRecertificationOrder;
                }

                ReevaluatePopupLabel = CurrentGeneratedItem.Label;
                ReEvalSection = CurrentGeneratedItem.ReEvalSection;
                DynamicFormViewModel.PopupDataContext = this;
            }, item => true);
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
                        CurrentOrderEntryVO.GeneratedOther = CurrentOrderEntryVO.PreviousGeneratedOther;
                    }

                    if (CurrentGeneratedItem.Label == "Initial Order for Start of Care")
                    {
                        CurrentOrderEntryVO.GeneratedInitialServiceOrder =
                            CurrentOrderEntryVO.PreviousGeneratedInitialServiceOrder;
                    }

                    if (CurrentGeneratedItem.Label == "Recertification Order")
                    {
                        CurrentOrderEntryVO.GeneratedRecertificationOrder =
                            CurrentOrderEntryVO.PreviousGeneratedRecertificationOrder;
                    }

                    CurrentGeneratedItem.Refresh();
                    CurrentGeneratedItem = null;
                }

                DynamicFormViewModel.PopupDataContext = null;
            });
            RefreshOrderTextCommand = new RelayCommand(() =>
            {
                if (CurrentOrderEntryVO != null)
                {
                    if (CurrentOrderEntryVO.CanEditOrderData)
                    {
                        CurrentOrderEntryVO.OrderText = CurrentOrderEntryVO.GeneratedOrderText;
                    }
                }
            });
            LoadedSignatureOrderEntryVOCommand = new RelayCommand(() =>
            {
                // refresh order text incase of new generated text
                if (OrderEntryManager != null)
                {
                    OrderEntryManager.UpdateGeneratedOrderText();
                }

                if (GeneratedSectionsList != null)
                {
                    foreach (SignatureOrderEntryGeneratedItem gi in GeneratedSectionsList) gi.Refresh();
                }
            });
            RefreshOverrideTextCommand = new RelayCommand<string>(whatToRefresh =>
            {
                if ((CurrentOrderEntryVO == null) || (CurrentOrderEntryVO.CanEditOrderData == false) ||
                    (string.IsNullOrWhiteSpace(whatToRefresh)))
                {
                    return;
                }

                if ((whatToRefresh == "Referral") && (CurrentOrderEntryVO.CanRefreshOverrideReferral))
                {
                    CurrentOrderEntryVO.OverrideReferral = CurrentOrderEntryVO.GeneratedReferral;
                }
                else if ((whatToRefresh == "VisitFrequency") && (CurrentOrderEntryVO.CanRefreshOverrideVisitFrequency))
                {
                    CurrentOrderEntryVO.OverrideVisitFrequency = CurrentOrderEntryVO.GeneratedVisitFrequency;
                }
                else if ((whatToRefresh == "Goals") && (CurrentOrderEntryVO.CanRefreshOverrideGoals))
                {
                    CurrentOrderEntryVO.OverrideGoals = CurrentOrderEntryVO.GeneratedGoals;
                }
                else if ((whatToRefresh == "Labs") && (CurrentOrderEntryVO.CanRefreshOverrideLabs))
                {
                    CurrentOrderEntryVO.OverrideLabs = CurrentOrderEntryVO.GeneratedLabs;
                }
                else if ((whatToRefresh == "InitialServiceOrder") &&
                         (CurrentOrderEntryVO.CanRefreshOverrideInitialServiceOrder))
                {
                    CurrentOrderEntryVO.OverrideInitialServiceOrder =
                        (string.IsNullOrWhiteSpace(CurrentOrderEntryVO.GeneratedInitialServiceOrder) == false)
                            ? "Initial Order for Start of Care:" + CR + CurrentOrderEntryVO.GeneratedInitialServiceOrder
                            : null;
                }
                else if ((whatToRefresh == "Medications") && (CurrentOrderEntryVO.CanRefreshOverrideMedications))
                {
                    CurrentOrderEntryVO.OverrideMedications = CurrentOrderEntryVO.GeneratedMedications;
                }
                else if ((whatToRefresh == "Equipment") && (CurrentOrderEntryVO.CanRefreshOverrideEquipment))
                {
                    CurrentOrderEntryVO.OverrideEquipment = CurrentOrderEntryVO.GeneratedEquipment;
                }
                else if ((whatToRefresh == "Supply") && (CurrentOrderEntryVO.CanRefreshOverrideSupply))
                {
                    CurrentOrderEntryVO.OverrideSupply = CurrentOrderEntryVO.GeneratedSupply;
                }
                else if ((whatToRefresh == "Other") && (CurrentOrderEntryVO.CanRefreshOverrideOther))
                {
                    CurrentOrderEntryVO.OverrideOther =
                        (string.IsNullOrWhiteSpace(CurrentOrderEntryVO.GeneratedOther) == false)
                            ? "Other Orders:" + CR + CurrentOrderEntryVO.GeneratedOther
                            : null;
                }
                else if ((whatToRefresh == "RecertificationOrder") &&
                         (CurrentOrderEntryVO.CanRefreshOverrideRecertificationOrder))
                {
                    CurrentOrderEntryVO.OverrideRecertificationOrder =
                        (string.IsNullOrWhiteSpace(CurrentOrderEntryVO.GeneratedRecertificationOrder) == false)
                            ? "Recertification Order:" + CR + CurrentOrderEntryVO.GeneratedRecertificationOrder
                            : null;
                }
            });
            if (CurrentOrderEntryVO == null)
            {
                MessageBox.Show(
                    "SignatureOrderEntryVO.SignatureOrderEntryVOSetup Error:  CurrentOrderEntryVO is null, SignatureOrderEntryVO question is only val1d with OrderEntry Forms, contact AlayaCare support.");
            }

            SignatureOrderEntryVOSetupGeneratedSections();

            SetupSelectedAdmissionPhysician();
        }

        private string CR = char.ToString('\r');

        public void SetupSigningPhysicianList()
        {
            signingPhysicianList.SortDescriptions.Add(new SortDescription("PhysicianName",
                ListSortDirection.Ascending));
            if (internalPhysicianList == null)
            {
                internalPhysicianList = new List<AdmissionPhysician>();
            }

            if (OrderEntryManager != null && OrderEntryManager.CurrentAdmission != null &&
                OrderEntryManager.CurrentAdmission.AdmissionPhysician != null)
            {
                internalPhysicianList = OrderEntryManager.CurrentAdmission.AdmissionPhysician.ToList();
            }

            signingPhysicianList.Source = internalPhysicianList;
            signingPhysicianList.Filter += signingPhysicianList_Filter;
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
                        e.Accepted = (CurrentOrderEntryVO != null);
                    }

                    if (e.Accepted)
                    {
                        e.Accepted = (CurrentOrderEntryVO != null)
                                     && (CurrentOrderEntryVO.CompletedDate != null);
                    }

                    if (e.Accepted)
                    {
                        e.Accepted = ((phy.PhysicianEffectiveFromDate == null)
                                      || (phy.PhysicianEffectiveFromDate.Date <=
                                          CurrentOrderEntryVO.CompletedDate.Value.Date)
                            );
                    }

                    if (e.Accepted)
                    {
                        e.Accepted = ((phy.PhysicianEffectiveThruDate == null)
                                      || (phy.PhysicianEffectiveThruDate.Value.Date >=
                                          CurrentOrderEntryVO.CompletedDate.Value.Date)
                            );
                    }
                }
            }
        }

        private bool PartialValidateOrderEntry()
        {
            bool isValid = true;
            if (CurrentOrderEntryVO == null)
            {
                return isValid;
            }

            ClearErrorsForOrderEntrySignature();
            if ((CurrentOrderEntryVO.SigningPhysicianKey == null) || (CurrentOrderEntryVO.SigningPhysicianKey <= 0))
            {
                AddErrorForProperty("SigningPhysicianKey",
                    "The Signing Physician is required before Order Types can be added");
                isValid = false;
            }

            if (CurrentOrderEntryVO.CompletedDate == null)
            {
                CurrentOrderEntryVO.ValidationErrors.Add(new System.ComponentModel.DataAnnotations.ValidationResult(
                    "The Completed Date/Time is required before Order Types can be added", new[] { "CompletedDate" }));
                isValid = false;
            }
            else
            {
                if (CurrentOrderEntryVO.IsVoided == false)
                {
                    if ((Admission != null) && (Admission.DischargeDateTime != null) &&
                        (((DateTime)Admission.DischargeDateTime).Date <
                         ((DateTimeOffset)CurrentOrderEntryVO.CompletedDate).Date))
                    {
                        CurrentOrderEntryVO.ValidationErrors.Add(
                            new System.ComponentModel.DataAnnotations.ValidationResult(
                                "The Completed Date/Time cannot be after the discharge date.",
                                new[] { "CompletedDate" }));
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        void OrderEntryManager_CompletedDateChanged(object sender, EventArgs e)
        {
            RefreshPhysicians();
            if ((CurrentOrderEntryVO.CompletedDate != null) && (Encounter != null))
            {
                if (Encounter.EncounterStartDate != CurrentOrderEntryVO.CompletedDate)
                {
                    Encounter.EncounterStartDate = CurrentOrderEntryVO.CompletedDate;
                }

                if (Encounter.EncounterStartTime != CurrentOrderEntryVO.CompletedDate)
                {
                    Encounter.EncounterStartTime = CurrentOrderEntryVO.CompletedDate;
                }

                if (Encounter.EncounterEndDate != CurrentOrderEntryVO.CompletedDate)
                {
                    Encounter.EncounterEndDate = CurrentOrderEntryVO.CompletedDate;
                }

                if (Encounter.EncounterEndTime != CurrentOrderEntryVO.CompletedDate)
                {
                    Encounter.EncounterEndTime = CurrentOrderEntryVO.CompletedDate;
                }
            }
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

                // refresh order text incase of new generated text
                if (OrderEntryManager != null)
                {
                    OrderEntryManager.UpdateGeneratedOrderText();
                }

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

        private void SignatureOrderEntryVOSetupGeneratedSections()
        {
            ObservableCollection<SignatureOrderEntryGeneratedItem> gList =
                new ObservableCollection<SignatureOrderEntryGeneratedItem>();
            int sequence = 4;

            AddGeneratedSection(gList, sequence++, "Referral / Visit Frequency Orders", "AdmissionDisciplineFrequency",
                "PatientCollectionBase", true);
            AddGeneratedSection(gList, sequence++, "Goal and Treatment Orders", "Rehab", "Rehab", false);
            AddGeneratedSection(gList, sequence++, "Labs/Test Orders", "Labs", "PatientCollectionBase", false);
            AddGeneratedSection(gList, sequence++, "Initial Order for Start of Care",
                "OrderEntryGeneratedInitialServiceOrder", "QuestionBase", true);
            AddGeneratedSection(gList, sequence++, "Medication Orders", "Medication", "PatientCollectionBase", false);
            AddGeneratedSection(gList, sequence++, "Supplies / Equipment", "OrderEntrySupplyEquipment",
                "OrderEntrySupplyEquipment", false);
            AddGeneratedSection(gList, sequence++, "Other Orders", "OrderEntryGeneratedOther", "QuestionBase", true);
            AddGeneratedSection(gList, sequence++, "Recertification Order", "OrderEntryGeneratedRecertificationOrder",
                "QuestionBase", true);

            GeneratedSectionsList = gList;
        }

        private void AddGeneratedSection(ObservableCollection<SignatureOrderEntryGeneratedItem> gList, int sequence,
            string label, string dataTemplate, string backingFactory, bool enabled)
        {
            SectionUI section = null;
            if (enabled)
            {
                ObservableCollection<SectionUI> sections = new ObservableCollection<SectionUI>();
                ProcessFormGeneratedSectionUI(sections, true, false, sequence++, label, dataTemplate, backingFactory);
                section = sections.FirstOrDefault();
            }

            SignatureOrderEntryGeneratedItem gi = new SignatureOrderEntryGeneratedItem
            {
                Label = label,
                CurrentOrderEntry = CurrentOrderEntryVO,
                ReEvalSection = section,
                Enabled = enabled
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

            if (label == "Referral / Visit Frequency Orders") //only thing that is enabled on VOs
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

            // refresh order text incase of new generated text or override edits
            if (OrderEntryManager != null)
            {
                OrderEntryManager.UpdateGeneratedOrderText();
            }

            ClearErrorsForOrderEntrySignature();

            if (CurrentOrderEntryVO != null)
            {
                returnStatus = CurrentOrderEntryVO.Validate();
            }

            if (returnStatus)
            {
                returnStatus = ValidateOrderEntryVO();
            }

            if (CurrentOrderEntryVO.IsVoided == false && CurrentOrderEntryVO.CompletedDate != null)
            {
                if ((Admission != null) && (Admission.DischargeDateTime != null) &&
                    (((DateTime)Admission.DischargeDateTime).Date <
                     ((DateTimeOffset)CurrentOrderEntryVO.CompletedDate).Date))
                {
                    CurrentOrderEntryVO.ValidationErrors.Add(new System.ComponentModel.DataAnnotations.ValidationResult(
                        "The Completed Date/Time cannot be after the admission discharge date.",
                        new[] { "CompletedDate" }));
                    returnStatus = false;
                }
            }

            if (returnStatus == false)
            {
                ValidateFailed();
            }

            return returnStatus;
        }

        private bool ValidateOrderEntryVO()
        {
            if ((CurrentOrderEntryVO == null) || (CurrentOrderEntryVO.VoidDate != null))
            {
                return true;
            }

            bool isValid = true;

            if ((CurrentOrderEntryVO.DiscardFlag == false) && (CurrentOrderEntryVO.Signature != null))
            {
                if (CurrentOrderEntryVO.ReadBack && string.IsNullOrEmpty(CurrentOrderEntryVO.ReadTo))
                {
                    CurrentOrderEntryVO.ValidationErrors.Add(
                        new System.ComponentModel.DataAnnotations.ValidationResult(
                            "Read To is required when Read Back checked.", new[] { "ReadTo" }));
                    isValid = false;
                }

                if (CurrentOrderEntryVO.CompletedDate == null)
                {
                    CurrentOrderEntryVO.ValidationErrors.Add(
                        new System.ComponentModel.DataAnnotations.ValidationResult(
                            "The Completed Date/Time is required.", new[] { "CompletedDate" }));
                    isValid = false;
                }

                if ((Admission != null) && (Admission.DischargeDateTime != null) &&
                    (((DateTime)Admission.DischargeDateTime).Date <
                     ((DateTimeOffset)CurrentOrderEntryVO.CompletedDate).Date))
                {
                    CurrentOrderEntryVO.ValidationErrors.Add(new System.ComponentModel.DataAnnotations.ValidationResult(
                        "The Completed Date/Time cannot be after the discharge date.", new[] { "CompletedDate" }));
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(CurrentOrderEntryVO.OrderText))
                {
                    if (CurrentOrderEntryVO.OrderEntryVersion == 1)
                    {
                        CurrentOrderEntryVO.ValidationErrors.Add(
                            new System.ComponentModel.DataAnnotations.ValidationResult("Order Text is required.",
                                new[] { "OrderText" }));
                        isValid = false;
                    }
                    else
                    {
                        CurrentOrderEntryVO.ValidationErrors.Add(
                            new System.ComponentModel.DataAnnotations.ValidationResult("Order Text is required.",
                                new[] { "GeneratedOrderText" }));
                        isValid = false;
                    }
                }

                int key = CurrentOrderEntryVO.SigningPhysicianKey ?? 0;
                if (key <= 0)
                {
                    AddErrorForProperty("SigningPhysicianKey", "The Signing Physician field is required.");
                    isValid = false;
                }
            }

            if (CurrentOrderEntryVO.DiscardFlag)
            {
                if (string.IsNullOrEmpty(CurrentOrderEntryVO.DiscardReason))
                {
                    CurrentOrderEntryVO.ValidationErrors.Add(
                        new System.ComponentModel.DataAnnotations.ValidationResult("Discard Reason is required.",
                            new[] { "DiscardReason" }));
                    isValid = false;
                }
            }

            if ((Encounter != null) && (Encounter.FullValidationNTUC) && (CurrentOrderEntryVO.DiscardFlag == false) &&
                (CurrentOrderEntryVO.Signature == null))
            {
                CurrentOrderEntryVO.ValidationErrors.Add(
                    new System.ComponentModel.DataAnnotations.ValidationResult("Signature or Discard is required.",
                        new[] { "DiscardFlag", "Signature" }));
                isValid = false;
            }

            return isValid;
        }

        private void ClearErrorsForOrderEntrySignature()
        {
            CurrentOrderEntryVO.ValidationErrors.Clear();
            ClearErrorFromProperty("SigningPhysicianKey");
        }

        public void PreValidate()
        {
            if (CurrentOrderEntryVO == null)
            {
                return;
            }

            if ((Encounter != null) && (Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed) &&
                (Encounter.EncounterBy == WebContext.Current.User.MemberID))
            {
                if (CurrentOrderEntryVO.Signature != null)
                {
                    if (SetUpToUseOrderEntryReviewers)
                    {
                        CurrentOrderEntryVO.OrderStatus = (UsingOrderEntryReviewers)
                            ? (int)OrderStatusType.OrderEntryReview
                            : (int)OrderStatusType.Completed;
                    }
                    else
                    {
                        CurrentOrderEntryVO.OrderStatus = (int)OrderStatusType.Completed;
                    }
                }
                else
                {
                    CurrentOrderEntryVO.OrderStatus = (int)OrderStatusType.InProcess;
                }
            }

            CurrentOrderEntryVO.RaiseChanged();
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
            if (CurrentOrderEntryVO == null)
            {
                return;
            }

            if (Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed)
            {
                CurrentOrderEntryVO.OrderStatus = CurrentOrderEntryVO.OrderStatus = (int)OrderStatusType.InProcess;
            }

            CurrentOrderEntryVO.RaiseChanged();
        }

        public Virtuoso.Services.Core.Model.User User => WebContext.Current.User;

        public override void Cleanup()
        {
            if (OrderEntryManager != null)
            {
                OrderEntryManager.CompletedDateChanged += OrderEntryManager_CompletedDateChanged;
            }

            Messenger.Default.Unregister<int>(this, "AdmissionPhysician_FormUpdate");
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
            if (OrderEntryManager != null)
            {
                OrderEntryManager.Cleanup();
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (GeneratedSectionsList != null)
                {
                    GeneratedSectionsList.ForEach(s => s.Cleanup());
                }

                if (CurrentOrderEntryVO != null)
                {
                    CurrentOrderEntryVO = null;
                }
            });
            base.Cleanup();
        }

        public override QuestionUI Clone()
        {
            return null;
        }
    }

    public class SignatureOrderEntryVOFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            SignatureOrderEntryVO s = new SignatureOrderEntryVO(__FormSectionQuestionKey)
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
            s.SignatureOrderEntryVOSetup();
            return s;
        }
    }
}