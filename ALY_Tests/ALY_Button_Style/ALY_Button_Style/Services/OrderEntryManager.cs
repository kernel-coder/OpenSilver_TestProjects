#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Services
{
    public class OrderEntryGoalElementItem
    {
        public string Description { get; set; }
        public string Discipline { get; set; }
        public bool Discontinued { get; set; }
    }

    public class OrderEntryManager : GenericBase
    {
        private static string DEFAULTDISCARDREASON = "No orders were generated during this encounter";
        public event EventHandler CompletedDateChanged;
        private bool _IsVO;

        public bool IsVO
        {
            get { return _IsVO; }
            set
            {
                _IsVO = value;
                RaisePropertyChanged("IsVO");
            }
        }

        private Admission _CurrentAdmission;

        public Admission CurrentAdmission
        {
            get { return _CurrentAdmission; }
            set
            {
                _CurrentAdmission = value;
                RaisePropertyChanged("CurrentAdmission");
            }
        }

        private Encounter _CurrentEncounter;

        public Encounter CurrentEncounter
        {
            get { return _CurrentEncounter; }
            set
            {
                _CurrentEncounter = value;
                RaisePropertyChanged("CurrentEncounter");
            }
        }

        private Form _CurrentForm;

        public Form CurrentForm
        {
            get { return _CurrentForm; }
            set
            {
                _CurrentForm = value;
                RaisePropertyChanged("CurrentForm");
            }
        }

        private Patient _CurrentPatient;

        public Patient CurrentPatient
        {
            get { return _CurrentPatient; }
            set
            {
                _CurrentPatient = value;
                RaisePropertyChanged("CurrentPatient");
            }
        }

        private OrderEntry _CurrentOrderEntry;

        public OrderEntry CurrentOrderEntry
        {
            get { return _CurrentOrderEntry; }
            private set
            {
                _CurrentOrderEntry = value;
                if (_CurrentOrderEntry != null)
                {
                    origSigningPhysicianKey = _CurrentOrderEntry.SigningPhysicianKey;
                    origVoidDate = _CurrentOrderEntry.VoidDate;
                    origOrderSent = _CurrentOrderEntry.OrderSent;
                    origSigningPhysicianVerifiedDate = _CurrentOrderEntry.SigningPhysicianVerifiedDate;
                    _CurrentOrderEntry.PropertyChanged += _CurrentOrderEntry_PropertyChanged;
                }

                RaisePropertyChanged("CurrentOrderEntry");
            }
        }

        void _CurrentOrderEntry_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SigningPhysicianKey")
            {
                RaisePropertyChanged("AddendumText");
            }
            else if (e.PropertyName == "CompletedDate")
            {
                if (CompletedDateChanged != null)
                {
                    CompletedDateChanged(this, EventArgs.Empty);
                }
            }
        }

        private OrderEntryVO _CurrentOrderEntryVO;

        public OrderEntryVO CurrentOrderEntryVO
        {
            get { return _CurrentOrderEntryVO; }
            private set
            {
                _CurrentOrderEntryVO = value;
                RaisePropertyChanged("CurrentOrderEntryVO");
            }
        }

        public IOrderEntry CurrentIOrderEntry
        {
            get
            {
                if (IsVO)
                {
                    return CurrentOrderEntryVO;
                }

                return CurrentOrderEntry;
            }
        }

        private int? origSigningPhysicianKey;
        private DateTimeOffset? origVoidDate;
        private bool origOrderSent;
        private DateTimeOffset? origSigningPhysicianVerifiedDate;
        private Guid _OrderEntryManagerGuid = Guid.NewGuid();
        public Guid OrderEntryManagerGuid => _OrderEntryManagerGuid;

        private string CR = char.ToString('\r');

        public string AddendumText
        {
            get
            {
                string text = null;

                if ((CurrentOrderEntry != null) && (IsVO == false) &&
                    (CurrentOrderEntry.SigningPhysicianKey != origSigningPhysicianKey))
                {
                    Physician oldPhysician = PhysicianCache.Current.GetPhysicianFromKey(origSigningPhysicianKey);
                    Physician newPhysician =
                        PhysicianCache.Current.GetPhysicianFromKey(CurrentOrderEntry.SigningPhysicianKey);
                    text = "Changed physician from " + ((oldPhysician == null) ? "?" : oldPhysician.FormattedName) +
                           " to " + ((newPhysician == null) ? "?" : newPhysician.FormattedName);
                }

                if ((CurrentOrderEntry != null) && (IsVO == false) && (CurrentOrderEntry.VoidDate != origVoidDate) &&
                    (CurrentOrderEntry.VoidText != null))
                {
                    text = text + ((text != null) ? CR : "") + CurrentOrderEntry.VoidText;
                }

                if ((CurrentOrderEntry != null) && (IsVO == false) && (CurrentOrderEntry.OrderSent != origOrderSent) &&
                    (CurrentOrderEntry.SentText != null))
                {
                    text = text + ((text != null) ? CR : "") + CurrentOrderEntry.SentText;
                }

                if ((CurrentOrderEntry != null) && (IsVO == false) &&
                    (CurrentOrderEntry.SigningPhysicianVerifiedDate != origSigningPhysicianVerifiedDate) &&
                    (CurrentOrderEntry.SigningPhysicianVerifiedText != null))
                {
                    text = text + ((text != null) ? CR : "") + CurrentOrderEntry.SigningPhysicianVerifiedText;
                }

                return text;
            }
        }

        private DynamicFormViewModel DynamicFormViewModel;
        public IDynamicFormService FormModel { get; set; }

        private OrderEntryManager(DynamicFormViewModel vm, bool isVO)
        {
            IsVO = isVO;
            DynamicFormViewModel = vm;
            CurrentAdmission = vm.CurrentAdmission;
            CurrentPatient = vm.CurrentPatient;
            CurrentEncounter = vm.CurrentEncounter;
            CurrentForm = vm.CurrentForm;
            FormModel = vm.FormModel;
        }

        public static OrderEntryManager Create(DynamicFormViewModel vm)
        {
            OrderEntryManager oem = CreateOrderEntry(vm);
            if (oem != null)
            {
                return oem;
            }

            return CreateOrderEntryVO(vm);
        }

        private static OrderEntryManager CreateOrderEntry(DynamicFormViewModel vm)
        {
            if (vm == null)
            {
                return null;
            }

            if ((vm.CurrentAdmission == null) || (vm.CurrentEncounter == null) || (vm.CurrentForm == null) ||
                (vm.CurrentPatient == null))
            {
                return null;
            }

            if (vm.CurrentForm.IsOrderEntry == false)
            {
                return null;
            }

            OrderEntryManager oem = new OrderEntryManager(vm, false);
            oem.CurrentOrderEntry = oem.GetOrCreateOrderEntry();
            return oem;
        }

        private bool ServiceOrdersHeldUntilReviewed
        {
            get
            {
                // Check if user, serviceLineGrouping, or tenant is using ServiceOrdersHeldUntilReviewed
                if (CurrentEncounter != null)
                {
                    UserProfile up = UserCache.Current.GetUserProfileFromUserId(CurrentEncounter.EncounterBy);
                    if (up != null)
                    {
                        if (up.ServiceOrdersHeldUntilReviewed)
                        {
                            return true;
                        }
                    }
                }

                if (CurrentAdmission != null)
                {
                    if (CurrentAdmission.ServiceOrdersHeldUntilReviewedAtServiceLineGroupings(null))
                    {
                        return true;
                    }
                }

                if (TenantSettingsCache.Current.TenantSetting.ServiceOrdersHeldUntilReviewed)
                {
                    return true;
                }

                return false;
            }
        }

        private OrderEntry GetOrCreateOrderEntry()
        {
            if ((CurrentEncounter == null) || (CurrentEncounter.OrderEntry == null))
            {
                return null;
            }

            OrderEntry orderEntry = CurrentEncounter.OrderEntry.FirstOrDefault(o => o.HistoryKey == null);
            if (orderEntry != null)
            {
                orderEntry.IsReviewed = (orderEntry.OrderStatus == (int)OrderStatusType.Completed) ||
                                        (orderEntry.OrderStatus == (int)OrderStatusType.SigningPhysicianVerified);
                return orderEntry;
            }

            orderEntry = new OrderEntry
            {
                PatientKey = CurrentPatient.PatientKey,
                Patient = CurrentPatient,
                AdmissionKey = CurrentAdmission.AdmissionKey,
                OrderStatus = (int)OrderStatusType.InProcess,
                CompletedDate = DateTimeOffset.Now,
                CompletedBy = WebContext.Current.User.MemberID,
                OrderEntryVersion = 2,
                ServiceOrdersHeldUntilReviewed = ServiceOrdersHeldUntilReviewed
            };
            if (CurrentEncounter != null)
            {
                CurrentEncounter.OrderEntry.Add(orderEntry);
            }

            if (CurrentAdmission != null)
            {
                CurrentAdmission.OrderEntry.Add(orderEntry);
            }

            if (CurrentEncounter != null)
            {
                if (CurrentEncounter.EncounterStartDate != orderEntry.CompletedDate)
                {
                    CurrentEncounter.EncounterStartDate = orderEntry.CompletedDate;
                }

                if (CurrentEncounter.EncounterStartTime != orderEntry.CompletedDate)
                {
                    CurrentEncounter.EncounterStartTime = orderEntry.CompletedDate;
                }

                if (CurrentEncounter.EncounterEndDate != orderEntry.CompletedDate)
                {
                    CurrentEncounter.EncounterEndDate = orderEntry.CompletedDate;
                }

                if (CurrentEncounter.EncounterEndTime != orderEntry.CompletedDate)
                {
                    CurrentEncounter.EncounterEndTime = orderEntry.CompletedDate;
                }
            }

            if ((CurrentEncounter != null && (CurrentEncounter.EncounterKey > 0)))
            {
                orderEntry.AddedFromEncounterKey = CurrentEncounter.EncounterKey;
            }

            orderEntry.SigningPhysicianKey = null;
            orderEntry.IsReviewed = false;
            orderEntry.DisplayProviderSection = true;
            if ((orderEntry.ServiceLineGroupingZero != null) &&
                (string.IsNullOrWhiteSpace(orderEntry.ServiceLineGroupingZero.Fax) == false))
            {
                orderEntry.ProviderFax = orderEntry.ServiceLineGroupingZero.Fax;
            }
            else
            {
                ServiceLine sl = ServiceLineCache.GetServiceLineFromKey(CurrentAdmission.ServiceLineKey);
                if (sl != null)
                {
                    orderEntry.ProviderFax = sl.Fax;
                }
            }

            ServiceLineGrouping slg =
                CurrentAdmission.GetFirstServiceLineGroupWithOasisHeader(DateTime
                    .SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date);
            if (slg != null)
            {
                OasisHeader oh = OasisHeaderCache.GetOasisHeaderFromKey(slg.OasisHeaderKey);
                if (oh != null)
                {
                    orderEntry.ProviderCMSCertificationNumber = oh.CMSCertificationNumber;
                }
            }

            return orderEntry;
        }

        private static OrderEntryManager CreateOrderEntryVO(DynamicFormViewModel vm)
        {
            if (vm == null)
            {
                return null;
            }

            if ((vm.CurrentAdmission == null) || (vm.CurrentEncounter == null) || (vm.CurrentForm == null) ||
                (vm.CurrentPatient == null))
            {
                return null;
            }

            OrderEntryManager oem = new OrderEntryManager(vm, true);
            oem.CurrentOrderEntryVO = oem.GetOrderEntryVO();
            if (oem.CurrentOrderEntryVO != null)
            {
                return oem;
            }

            if (oem.IncludeNewVO == false)
            {
                return null;
            }

            oem.CurrentOrderEntryVO = oem.CreateOrderEntryVO();
            if (oem.CurrentOrderEntryVO == null)
            {
                return null;
            }

            return oem;
        }

        private bool IncludeNewVO
        {
            get
            {
                // If the Plan of Care has already been completed (or in POCOrderReview) for the certification cycle and we are documenting an Evaluation/Assessment/Visit/Resumption
                // include an Interim Order with the encounter - i.e., return true

                Func<Form, bool> IsValidForm = form =>
                {
                    if (form.IsEval)
                    {
                        return true;
                    }

                    if (form.IsVisit)
                    {
                        return true;
                    }

                    if (form.IsResumption)
                    {
                        return true;
                    }

                    return false;
                };

                if (!IsValidForm(CurrentForm))
                {
                    return false;
                }

                if (CurrentAdmission.Encounter == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentAdmissionDiscipline == null)
                {
                    return false;
                }

                bool has_clinician_role = RoleAccessHelper.CheckPermission("Clinician", false);
                bool has_order_entry_role = RoleAccessHelper.CheckPermission("Order Entry", false);
                if (has_clinician_role == false || has_order_entry_role == false)
                {
                    return false;
                }

                if (CurrentForm.IsEval)
                {
                    if (DynamicFormViewModel.CurrentAdmissionDiscipline.AdmissionStatusCode != "R")
                    {
                        return false;
                    }
                }
                else if (CurrentForm.IsVisit)
                {
                    if (DynamicFormViewModel.CurrentAdmissionDiscipline.AdmissionStatusCode != "A")
                    {
                        return false;
                    }
                }
                else if (CurrentForm.IsResumption)
                {
                    if (DynamicFormViewModel.CurrentAdmissionDiscipline.AdmissionStatusCode != "R")
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentEncounter.EncounterIsInEdit)
                {
                    if (owningPOC == null)
                    {
                        return false;
                    }

                    ServiceType VOServiceType = GetOrderServiceType();
                    if ((VOServiceType == null) || (VOServiceType.FormKey == null))
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }
        }

        private Encounter owningPOC
        {
            get
            {
                if ((DynamicFormViewModel == null) || (CurrentAdmission == null) ||
                    (CurrentAdmission.Encounter == null))
                {
                    return null;
                }

                AdmissionCertification ac = DynamicFormViewModel.GetAdmissionCertForEncounter();
                if (ac == null)
                {
                    return null;
                }

                // DE 48367 - For Hospice - we only need a POC for the initial cert cycle - so for subsequent cycles - assume we have one
                if (CurrentAdmission.HospiceAdmission && (CurrentAdmission.FirstCert != ac))
                {
                    return new Encounter();
                }

                Encounter ePOC = CurrentAdmission.Encounter.Where(e => (
                    (e.Inactive == false) &&
                    (e.HistoryKey == null) &&
                    e.EncounterIsPlanOfCare &&
                    ((e.EncounterStatus == (int)EncounterStatusType.Completed) ||
                     (e.EncounterStatus == (int)EncounterStatusType.POCOrderReview)) &&
                    (e.EncounterIsPlanOfCareInCertCycle(ac)))).FirstOrDefault();
                return ePOC;
            }
        }

        private ServiceType GetOrderServiceType()
        {
            ServiceType serviceType = ServiceTypeCache.GetActiveServiceTypes().FirstOrDefault(st => st.IsOrderEntry);
            if ((serviceType == null) || (serviceType.FormKey == null))
            {
                MessageBox.Show("Order Entry service type does not exist, contact AlayaCare support");
                return null;
            }

            return serviceType;
        }

        private OrderEntryVO GetOrderEntryVO()
        {
            if ((CurrentEncounter == null) || (CurrentEncounter.OrderEntryVO == null))
            {
                return null;
            }

            OrderEntryVO orderEntryVO = CurrentEncounter.OrderEntryVO.FirstOrDefault();
            if (orderEntryVO == null)
            {
                return null;
            }

            orderEntryVO.IsReviewed = false;
            return orderEntryVO;
        }

        private OrderEntryVO CreateOrderEntryVO()
        {
            OrderEntryVO orderEntryVO = GetOrderEntryVO();
            if (orderEntryVO != null)
            {
                return orderEntryVO;
            }

            AdmissionCertification ac = DynamicFormViewModel.GetAdmissionCertForEncounter();
            if (ac == null)
            {
                return null;
            }

            ServiceType VOServiceType = GetOrderServiceType();
            if ((VOServiceType == null) || (VOServiceType.FormKey == null))
            {
                return null;
            }

            orderEntryVO = new OrderEntryVO
            {
                PatientKey = CurrentPatient.PatientKey,
                Patient = CurrentPatient,
                AdmissionKey = CurrentAdmission.AdmissionKey,
                OrderStatus = (int)OrderStatusType.InProcess,
                CompletedDate = DateTimeOffset.Now,
                CompletedBy = WebContext.Current.User.MemberID,
                AdmissionCertKey = ac.AdmissionCertKey,
                OrderEntryVersion = 2,
                ServiceTypeKey = VOServiceType.ServiceTypeKey,
                FormKey = (int)VOServiceType.FormKey,
                DiscardFlag = true,
                DiscardReason = DEFAULTDISCARDREASON,
                ServiceOrdersHeldUntilReviewed = ServiceOrdersHeldUntilReviewed
            };
            if (CurrentEncounter != null)
            {
                CurrentEncounter.OrderEntryVO.Add(orderEntryVO);
            }

            if (CurrentAdmission != null)
            {
                CurrentAdmission.OrderEntryVO.Add(orderEntryVO);
            }

            if ((CurrentEncounter != null && (CurrentEncounter.EncounterKey > 0)))
            {
                orderEntryVO.AddedFromEncounterKey = CurrentEncounter.EncounterKey;
            }

            orderEntryVO.SigningPhysicianKey = null;
            orderEntryVO.IsReviewed = false;
            orderEntryVO.DisplayProviderSection = true;
            if ((orderEntryVO.ServiceLineGroupingZero != null) &&
                (string.IsNullOrWhiteSpace(orderEntryVO.ServiceLineGroupingZero.Fax) == false))
            {
                orderEntryVO.ProviderFax = orderEntryVO.ServiceLineGroupingZero.Fax;
            }
            else
            {
                ServiceLine sl = ServiceLineCache.GetServiceLineFromKey(CurrentAdmission.ServiceLineKey);
                if (sl != null)
                {
                    orderEntryVO.ProviderFax = sl.Fax;
                }
            }

            ServiceLineGrouping slg =
                CurrentAdmission.GetFirstServiceLineGroupWithOasisHeader(DateTime
                    .SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date);
            if (slg != null)
            {
                OasisHeader oh = OasisHeaderCache.GetOasisHeaderFromKey(slg.OasisHeaderKey);
                if (oh != null)
                {
                    orderEntryVO.ProviderCMSCertificationNumber = oh.CMSCertificationNumber;
                }
            }

            return orderEntryVO;
        }

        int __cleanupCount;

        public override void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            Messenger.Default.Unregister(this);
            FormModel = null;
            CurrentAdmission = null;
            CurrentEncounter = null;
            base.Cleanup();
        }

        public void UpdateGeneratedOrderText()
        {
            if (((CurrentIOrderEntry != null) && (CurrentIOrderEntry.CanEditOrderData || CanEditOrderData)) == false)
            {
                return;
            }

            if ((CurrentEncounter == null) || (CurrentPatient == null))
            {
                return;
            }

            if (CurrentEncounter.HasVO &&
                (CurrentEncounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed))
            {
                return;
            }

            bool wasGeneratedOrderTextNull = (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedOrderText));
            if (CurrentIOrderEntry.OrderEntryVersion == 1)
            {
                UpdateGeneratedOrderTextVersion1();
            }
            else
            {
                UpdateGeneratedOrderTextVersion2();
            }

            OrderEntryVO oeVO = CurrentIOrderEntry as OrderEntryVO;
            if ((oeVO != null) && IsVO && wasGeneratedOrderTextNull &&
                (string.IsNullOrWhiteSpace(oeVO.GeneratedOrderText) == false) &&
                (oeVO.DiscardReason == DEFAULTDISCARDREASON))
            {
                oeVO.DiscardFlag = false;
                oeVO.DiscardReason = null;
            }
        }

        private void UpdateGeneratedOrderTextVersion1()
        {
            string orderText = string.Empty;
            bool isGeneratedOrderTextEqualToOrderText = CurrentIOrderEntry.IsGeneratedOrderTextEqualToOrderText;
            string generatedSupplyText = null;
            string generatedEquipmentText = null;

            CurrentIOrderEntry.GeneratedReferral = GeneratedReferralText;
            CurrentIOrderEntry.IsGeneratedReferral = (!string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedReferral));
            CurrentIOrderEntry.GeneratedVisitFrequency = GeneratedVisitFrequencyText;
            CurrentIOrderEntry.IsGeneratedVisitFrequency = (!string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedVisitFrequency));
            CurrentIOrderEntry.GeneratedGoals = GeneratedGoalText;
            CurrentIOrderEntry.IsGeneratedGoals = (!string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedGoals));
            CurrentIOrderEntry.GeneratedLabs = GeneratedLabText;
            CurrentIOrderEntry.IsGeneratedLabs = (!string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedLabs));
            CurrentIOrderEntry.IsGeneratedInitialServiceOrder = (!string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedInitialServiceOrder));
            CurrentIOrderEntry.GeneratedMedications = GeneratedMedicationText;
            CurrentIOrderEntry.IsGeneratedMedications = (!string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedMedications));
            CurrentIOrderEntry.IsGeneratedOther = (!string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedOther));
            CurrentIOrderEntry.IsGeneratedRecertificationOrder = (!string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedRecertificationOrder));
            generatedEquipmentText = GeneratedEquipmentTextVersion1;
            generatedSupplyText = GeneratedSupplyTextVersion1;
            CurrentIOrderEntry.GeneratedSupplyEquipment = generatedEquipmentText;
            if (!string.IsNullOrEmpty(generatedEquipmentText)
                && !string.IsNullOrEmpty(generatedSupplyText)
               )
            {
                CurrentIOrderEntry.GeneratedSupplyEquipment += CR + generatedSupplyText;
            }
            else
            {
                CurrentIOrderEntry.GeneratedSupplyEquipment += generatedSupplyText;
            }

            CurrentIOrderEntry.IsGeneratedSupplyEquipment = (!string.IsNullOrEmpty(CurrentIOrderEntry.GeneratedSupplyEquipment));

            if (CurrentIOrderEntry.IsGeneratedReferral)
            {
                orderText = orderText + ((string.IsNullOrWhiteSpace(orderText)) ? "" : CR) + "Discipline Referrals:" +
                            CR + CurrentIOrderEntry.GeneratedReferral;
            }

            if (CurrentIOrderEntry.IsGeneratedVisitFrequency)
            {
                orderText = orderText + ((string.IsNullOrWhiteSpace(orderText)) ? "" : CR) +
                            "Discipline Visit Frequencies:" + CR + CurrentIOrderEntry.GeneratedVisitFrequency;
            }

            if (CurrentIOrderEntry.IsGeneratedGoals)
            {
                orderText = orderText + ((string.IsNullOrWhiteSpace(orderText)) ? "" : CR) + "Goals and Treatments:" +
                            CR + CurrentIOrderEntry.GeneratedGoals;
            }

            if (CurrentIOrderEntry.IsGeneratedLabs)
            {
                orderText = orderText + ((string.IsNullOrWhiteSpace(orderText)) ? "" : CR) + "Labs/Tests:" + CR +
                            CurrentIOrderEntry.GeneratedLabs;
            }

            if (CurrentIOrderEntry.IsGeneratedInitialServiceOrder)
            {
                if (string.IsNullOrWhiteSpace(orderText))
                {
                    orderText = CurrentIOrderEntry.GeneratedInitialServiceOrder;
                }
                else
                {
                    orderText = orderText + CR + "Initial Order for Start of Care:" + CR + "     " +
                                CurrentIOrderEntry.GeneratedInitialServiceOrder;
                }
            }

            if (CurrentIOrderEntry.IsGeneratedMedications)
            {
                orderText = orderText + ((string.IsNullOrWhiteSpace(orderText)) ? "" : CR) + "Medications:" + CR +
                            CurrentIOrderEntry.GeneratedMedications;
            }

            if (CurrentIOrderEntry.IsGeneratedOther)
            {
                if (string.IsNullOrWhiteSpace(orderText))
                {
                    orderText = CurrentIOrderEntry.GeneratedOther;
                }
                else
                {
                    orderText = orderText + CR + "Other:" + CR + "     " + CurrentIOrderEntry.GeneratedOther;
                }
            }

            if (CurrentIOrderEntry.IsGeneratedRecertificationOrder)
            {
                if (string.IsNullOrWhiteSpace(orderText))
                {
                    orderText = CurrentIOrderEntry.GeneratedRecertificationOrder;
                }
                else
                {
                    orderText = orderText + CR + "Recertification Order:" + CR + "     " +
                                CurrentIOrderEntry.GeneratedRecertificationOrder;
                }
            }

            if (CurrentIOrderEntry.IsGeneratedSupplyEquipment)
            {
                orderText = orderText + ((string.IsNullOrWhiteSpace(orderText)) ? "" : CR) + "Supplies / Equipment:" +
                            CR + CurrentIOrderEntry.GeneratedSupplyEquipment;
            }

            CurrentIOrderEntry.GeneratedOrderText = (string.IsNullOrWhiteSpace(orderText)) ? null : orderText.Trim();
            if (isGeneratedOrderTextEqualToOrderText)
            {
                CurrentIOrderEntry.OrderText = CurrentIOrderEntry.GeneratedOrderText;
            }
        }

        private void UpdateGeneratedOrderTextVersion2()
        {
            string generatedOrderText = string.Empty;
            string overrideOrderText = string.Empty;
            bool isGeneratedEqualOverride = false;
            isGeneratedEqualOverride = CurrentIOrderEntry.IsGeneratedReferralEqualToOverride;
            string admitResumeText = (IsEval) ? GeneratedAdmitText : ((IsResumption) ? GeneratedResumptionText : null);
            string generatedText = GeneratedReferralText;
            if (string.IsNullOrWhiteSpace(generatedText) == false)
            {
                generatedText = "Discipline Referrals:" + CR + generatedText;
            }

            if (string.IsNullOrWhiteSpace(admitResumeText) == false)
            {
                generatedText = admitResumeText + ((string.IsNullOrWhiteSpace(generatedText)) ? "" : CR) +
                                generatedText;
            }

            CurrentIOrderEntry.IsGeneratedReferral = (!string.IsNullOrWhiteSpace(generatedText));
            CurrentIOrderEntry.GeneratedReferral = (CurrentIOrderEntry.IsGeneratedReferral) ? generatedText : null;
            if (isGeneratedEqualOverride)
            {
                CurrentIOrderEntry.OverrideReferral = CurrentIOrderEntry.GeneratedReferral;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedReferral) == false)
            {
                generatedOrderText = generatedOrderText + ((string.IsNullOrWhiteSpace(generatedOrderText)) ? "" : CR) +
                                     CurrentIOrderEntry.GeneratedReferral;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.OverrideReferral) == false)
            {
                overrideOrderText = overrideOrderText + ((string.IsNullOrWhiteSpace(overrideOrderText)) ? "" : CR) +
                                    CurrentIOrderEntry.OverrideReferral;
            }

            isGeneratedEqualOverride = CurrentIOrderEntry.IsGeneratedVisitFrequencyEqualToOverride;
            generatedText = GeneratedVisitFrequencyText;
            CurrentIOrderEntry.IsGeneratedVisitFrequency = (!string.IsNullOrWhiteSpace(generatedText));
            CurrentIOrderEntry.GeneratedVisitFrequency = (CurrentIOrderEntry.IsGeneratedVisitFrequency)
                ? "Discipline Visit Frequencies:" + CR + generatedText
                : null;
            if (isGeneratedEqualOverride)
            {
                CurrentIOrderEntry.OverrideVisitFrequency = CurrentIOrderEntry.GeneratedVisitFrequency;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedVisitFrequency) == false)
            {
                generatedOrderText = generatedOrderText + ((string.IsNullOrWhiteSpace(generatedOrderText)) ? "" : CR) +
                                     CurrentIOrderEntry.GeneratedVisitFrequency;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.OverrideVisitFrequency) == false)
            {
                overrideOrderText = overrideOrderText + ((string.IsNullOrWhiteSpace(overrideOrderText)) ? "" : CR) +
                                    CurrentIOrderEntry.OverrideVisitFrequency;
            }

            isGeneratedEqualOverride = CurrentIOrderEntry.IsGeneratedGoalsEqualToOverride;
            generatedText = GeneratedGoalText;
            CurrentIOrderEntry.IsGeneratedGoals = (!string.IsNullOrWhiteSpace(generatedText));
            CurrentIOrderEntry.GeneratedGoals = (CurrentIOrderEntry.IsGeneratedGoals)
                ? "Goals and Treatments:" + CR + generatedText
                : null;
            if (isGeneratedEqualOverride)
            {
                CurrentIOrderEntry.OverrideGoals = CurrentIOrderEntry.GeneratedGoals;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedGoals) == false)
            {
                generatedOrderText = generatedOrderText + ((string.IsNullOrWhiteSpace(generatedOrderText)) ? "" : CR) +
                                     CurrentIOrderEntry.GeneratedGoals;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.OverrideGoals) == false)
            {
                overrideOrderText = overrideOrderText + ((string.IsNullOrWhiteSpace(overrideOrderText)) ? "" : CR) +
                                    CurrentIOrderEntry.OverrideGoals;
            }

            isGeneratedEqualOverride = CurrentIOrderEntry.IsGeneratedLabsEqualToOverride;
            generatedText = GeneratedLabText;
            CurrentIOrderEntry.IsGeneratedLabs = (!string.IsNullOrWhiteSpace(generatedText));
            CurrentIOrderEntry.GeneratedLabs =
                (CurrentIOrderEntry.IsGeneratedLabs) ? "Labs/Tests:" + CR + generatedText : null;
            if (isGeneratedEqualOverride)
            {
                CurrentIOrderEntry.OverrideLabs = CurrentIOrderEntry.GeneratedLabs;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedLabs) == false)
            {
                generatedOrderText = generatedOrderText + ((string.IsNullOrWhiteSpace(generatedOrderText)) ? "" : CR) +
                                     CurrentIOrderEntry.GeneratedLabs;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.OverrideLabs) == false)
            {
                overrideOrderText = overrideOrderText + ((string.IsNullOrWhiteSpace(overrideOrderText)) ? "" : CR) +
                                    CurrentIOrderEntry.OverrideLabs;
            }

            CurrentIOrderEntry.IsGeneratedInitialServiceOrder = (!string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedInitialServiceOrder));
            if (CurrentIOrderEntry.IsPreviousGeneratedInitialServiceOrderEqualToOverride)
            {
                CurrentIOrderEntry.OverrideInitialServiceOrder =
                    (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedInitialServiceOrder) == false)
                        ? "Initial Order for Start of Care:" + CR + CurrentIOrderEntry.GeneratedInitialServiceOrder
                        : null;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedInitialServiceOrder) == false)
            {
                generatedOrderText = generatedOrderText + ((string.IsNullOrWhiteSpace(generatedOrderText)) ? "" : CR) +
                                     "Initial Order for Start of Care:" + CR +
                                     CurrentIOrderEntry.GeneratedInitialServiceOrder;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.OverrideInitialServiceOrder) == false)
            {
                overrideOrderText = overrideOrderText + ((string.IsNullOrWhiteSpace(overrideOrderText)) ? "" : CR) +
                                    CurrentIOrderEntry.OverrideInitialServiceOrder;
            }

            CurrentIOrderEntry.PreviousGeneratedInitialServiceOrder = CurrentIOrderEntry.GeneratedInitialServiceOrder;

            isGeneratedEqualOverride = CurrentIOrderEntry.IsGeneratedMedicationsEqualToOverride;
            generatedText = GeneratedMedicationText;
            CurrentIOrderEntry.IsGeneratedMedications = (!string.IsNullOrWhiteSpace(generatedText));
            CurrentIOrderEntry.GeneratedMedications = (CurrentIOrderEntry.IsGeneratedMedications)
                ? "Medications:" + CR + generatedText
                : null;
            if (isGeneratedEqualOverride)
            {
                CurrentIOrderEntry.OverrideMedications = CurrentIOrderEntry.GeneratedMedications;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedMedications) == false)
            {
                generatedOrderText = generatedOrderText + ((string.IsNullOrWhiteSpace(generatedOrderText)) ? "" : CR) +
                                     CurrentIOrderEntry.GeneratedMedications;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.OverrideMedications) == false)
            {
                overrideOrderText = overrideOrderText + ((string.IsNullOrWhiteSpace(overrideOrderText)) ? "" : CR) +
                                    CurrentIOrderEntry.OverrideMedications;
            }

            isGeneratedEqualOverride = CurrentIOrderEntry.IsGeneratedEquipmentEqualToOverride;
            generatedText = GeneratedEquipmentTextVersion2;
            CurrentIOrderEntry.IsGeneratedEquipment = (!string.IsNullOrWhiteSpace(generatedText));
            CurrentIOrderEntry.GeneratedEquipment =
                (CurrentIOrderEntry.IsGeneratedEquipment) ? "Equipment:" + CR + generatedText : null;
            if (isGeneratedEqualOverride)
            {
                CurrentIOrderEntry.OverrideEquipment = CurrentIOrderEntry.GeneratedEquipment;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedEquipment) == false)
            {
                generatedOrderText = generatedOrderText + ((string.IsNullOrWhiteSpace(generatedOrderText)) ? "" : CR) +
                                     CurrentIOrderEntry.GeneratedEquipment;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.OverrideEquipment) == false)
            {
                overrideOrderText = overrideOrderText + ((string.IsNullOrWhiteSpace(overrideOrderText)) ? "" : CR) +
                                    CurrentIOrderEntry.OverrideEquipment;
            }

            isGeneratedEqualOverride = CurrentIOrderEntry.IsGeneratedSupplyEqualToOverride;
            generatedText = GeneratedSupplyTextVersion2;
            CurrentIOrderEntry.IsGeneratedSupply = (!string.IsNullOrWhiteSpace(generatedText));
            CurrentIOrderEntry.GeneratedSupply =
                (CurrentIOrderEntry.IsGeneratedSupply) ? "Supplies:" + CR + generatedText : null;
            if (isGeneratedEqualOverride)
            {
                CurrentIOrderEntry.OverrideSupply = CurrentIOrderEntry.GeneratedSupply;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedSupply) == false)
            {
                generatedOrderText = generatedOrderText + ((string.IsNullOrWhiteSpace(generatedOrderText)) ? "" : CR) +
                                     CurrentIOrderEntry.GeneratedSupply;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.OverrideSupply) == false)
            {
                overrideOrderText = overrideOrderText + ((string.IsNullOrWhiteSpace(overrideOrderText)) ? "" : CR) +
                                    CurrentIOrderEntry.OverrideSupply;
            }

            CurrentIOrderEntry.IsGeneratedOther = (!string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedOther));
            if (CurrentIOrderEntry.IsPreviousGeneratedOtherEqualToOverride)
            {
                CurrentIOrderEntry.OverrideOther =
                    (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedOther) == false)
                        ? "Other Orders:" + CR + CurrentIOrderEntry.GeneratedOther
                        : null;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedOther) == false)
            {
                generatedOrderText = generatedOrderText + ((string.IsNullOrWhiteSpace(generatedOrderText)) ? "" : CR) +
                                     "Other Orders:" + CR + CurrentIOrderEntry.GeneratedOther;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.OverrideOther) == false)
            {
                overrideOrderText = overrideOrderText + ((string.IsNullOrWhiteSpace(overrideOrderText)) ? "" : CR) +
                                    CurrentIOrderEntry.OverrideOther;
            }

            CurrentIOrderEntry.PreviousGeneratedOther = CurrentIOrderEntry.GeneratedOther;

            CurrentIOrderEntry.IsGeneratedRecertificationOrder = (!string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedRecertificationOrder));
            if (CurrentIOrderEntry.IsPreviousGeneratedRecertificationOrderEqualToOverride)
            {
                CurrentIOrderEntry.OverrideRecertificationOrder =
                    (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedRecertificationOrder) == false)
                        ? "Recertification Order:" + CR + CurrentIOrderEntry.GeneratedRecertificationOrder
                        : null;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.GeneratedRecertificationOrder) == false)
            {
                generatedOrderText = generatedOrderText + ((string.IsNullOrWhiteSpace(generatedOrderText)) ? "" : CR) +
                                     "Recertification Order:" + CR + CurrentIOrderEntry.GeneratedRecertificationOrder;
            }

            if (string.IsNullOrWhiteSpace(CurrentIOrderEntry.OverrideRecertificationOrder) == false)
            {
                overrideOrderText = overrideOrderText + ((string.IsNullOrWhiteSpace(overrideOrderText)) ? "" : CR) +
                                    CurrentIOrderEntry.OverrideRecertificationOrder;
            }

            CurrentIOrderEntry.PreviousGeneratedRecertificationOrder = CurrentIOrderEntry.GeneratedRecertificationOrder;

            CurrentIOrderEntry.GeneratedOrderText =
                (string.IsNullOrWhiteSpace(generatedOrderText)) ? null : generatedOrderText.Trim();
            CurrentIOrderEntry.OrderText =
                (string.IsNullOrWhiteSpace(overrideOrderText)) ? null : overrideOrderText.Trim();
        }

        public bool CanEditOrderData
        {
            get
            {
                if (CurrentIOrderEntry == null)
                {
                    return false;
                }

                if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEntryReviewer, false) &&
                    (CurrentIOrderEntry.PreviousOrderStatus == (int)OrderStatusType.OrderEntryReview))
                {
                    return true;
                }

                if ((CurrentIOrderEntry.CompletedBy == WebContext.Current.User.MemberID) &&
                    (CurrentIOrderEntry.PreviousOrderStatus == (int)OrderStatusType.InProcess))
                {
                    return true;
                }

                if (CurrentEncounter == null)
                {
                    return false;
                }

                if (CurrentEncounter.HasVO &&
                    (CurrentEncounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed))
                {
                    return true;
                }

                return false;
            }
        }

        private bool IsEval
        {
            get
            {
                if (IsVO == false)
                {
                    return false;
                }

                if (DynamicFormViewModel == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm.IsEval)
                {
                    return true;
                }

                return false;
            }
        }

        private string GeneratedAdmitText
        {
            get
            {
                if (IsVO == false)
                {
                    return null;
                }

                if (DynamicFormViewModel == null)
                {
                    return null;
                }

                if (DynamicFormViewModel.CurrentAdmissionDiscipline == null)
                {
                    return "?? admitted on ??";
                }

                DateTime? admitDate = DynamicFormViewModel.CurrentAdmissionDiscipline.DisciplineAdmitDateTime;
                if ((admitDate == null) && (CurrentEncounter != null))
                {
                    admitDate = (CurrentEncounter.EncounterOrTaskStartDateAndTime == null)
                        ? (DateTime?)null
                        : ((DateTimeOffset)CurrentEncounter.EncounterOrTaskStartDateAndTime).Date;
                }

                if (admitDate == null)
                {
                    admitDate = DateTime.Today.Date;
                }

                string disciplineCode = DynamicFormViewModel.CurrentAdmissionDiscipline.DisciplineCode;
                if (string.IsNullOrWhiteSpace(disciplineCode))
                {
                    disciplineCode = "??";
                }

                return disciplineCode + " admitted on " + ((DateTime)admitDate).ToShortDateString();
            }
        }

        private bool IsResumption
        {
            get
            {
                if (IsVO == false)
                {
                    return false;
                }

                if (DynamicFormViewModel == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm.IsResumption)
                {
                    return true;
                }

                return false;
            }
        }

        private string GeneratedResumptionText
        {
            get
            {
                if (IsVO == false)
                {
                    return null;
                }

                if (DynamicFormViewModel == null)
                {
                    return null;
                }

                if (DynamicFormViewModel.CurrentAdmissionDiscipline == null)
                {
                    return "?? resumed on ??";
                }

                DateTime? resumeDate = DynamicFormViewModel.CurrentAdmissionDiscipline.DisciplineAdmitDateTime;
                if ((resumeDate == null) && (CurrentEncounter != null))
                {
                    resumeDate = (CurrentEncounter.EncounterOrTaskStartDateAndTime == null)
                        ? (DateTime?)null
                        : ((DateTimeOffset)CurrentEncounter.EncounterOrTaskStartDateAndTime).Date;
                }

                if (resumeDate == null)
                {
                    resumeDate = DateTime.Today.Date;
                }

                string disciplineCode = DynamicFormViewModel.CurrentAdmissionDiscipline.DisciplineCode;
                if (string.IsNullOrWhiteSpace(disciplineCode))
                {
                    disciplineCode = "??";
                }

                string resumeText = disciplineCode + " resumed on " + ((DateTime)resumeDate).ToShortDateString();
                AdmissionCertification ac = DynamicFormViewModel.GetAdmissionCertForEncounter();
                if (ac == null)
                {
                    return resumeText;
                }

                return resumeText + " for " + ((CurrentAdmission.HospiceAdmission) ? "benefit" : "certification") +
                       " period " + ac.PeriodStartThruEndBlirb;
            }
        }

        private string GeneratedReferralText
        {
            get
            {
                if ((CurrentIOrderEntry == null) || (CurrentEncounter == null) || (CurrentAdmission == null) ||
                    (CurrentAdmission.AdmissionDiscipline == null))
                {
                    return null;
                }

                string generatedText = string.Empty;

                List<AdmissionDiscipline> adList = CurrentAdmission.AdmissionDiscipline
                    .Where(p => ((p.IsNew) || (p.AddedFromEncounterKey == CurrentEncounter.EncounterKey)))
                    .OrderBy(p => p.DisciplineDescription).ToList();

                foreach (AdmissionDiscipline ad in adList)
                    generatedText = generatedText + ((string.IsNullOrWhiteSpace(generatedText)) ? "" : CR) + "     " +
                                    ad.ReferralDescriptionForOrderEntry;
                return (string.IsNullOrWhiteSpace(generatedText)) ? null : generatedText;
            }
        }

        private string GeneratedVisitFrequencyText
        {
            get
            {
                if ((CurrentIOrderEntry == null) || (CurrentEncounter == null) ||
                    (CurrentEncounter.EncounterDisciplineFrequency == null) || (CurrentAdmission == null) ||
                    (CurrentAdmission.AdmissionDisciplineFrequency == null))
                {
                    return null;
                }

                string generatedText = string.Empty;

                List<AdmissionDisciplineFrequency> adfNewList = CurrentAdmission.AdmissionDisciplineFrequency
                    .Where(p => ((p.IsNew) || (p.AddedFromEncounterKey == CurrentEncounter.EncounterKey)))
                    .OrderBy(p => p.DisciplineDescription).ThenBy(p => p.StartDate).ToList();
                if (adfNewList != null)
                {
                    foreach (AdmissionDisciplineFrequency adf in adfNewList)
                    {
                        if (DuplicateADF(adfNewList, adf))
                        {
                            continue; // bypass this adf - there is a later version against this encounter
                        }

                        EncounterStartDisciplineFrequency esdf = GetMyEncounterStartDisciplineFrequency(adf);
                        if (esdf != null)
                        {
                            continue; // bypass this adf - it is an edit of an existing adf - not a newly added adf
                        }

                        generatedText = generatedText + ((string.IsNullOrWhiteSpace(generatedText)) ? "" : CR) +
                                        "     " + adf.DisplayDisciplineFrequencyText.Trim() +
                                        (string.IsNullOrWhiteSpace(adf.Purpose) ? "" : (CR + "     " + adf.Purpose));
                    }
                }

                List<AdmissionDisciplineFrequency> adfExistingList = new List<AdmissionDisciplineFrequency>();
                foreach (EncounterDisciplineFrequency edf in CurrentEncounter.EncounterDisciplineFrequency)
                {
                    AdmissionDisciplineFrequency adf = CurrentAdmission.AdmissionDisciplineFrequency
                        .FirstOrDefault(p => ((p.DisciplineFrequencyKey == edf.DispFreqKey) && (p.Inactive == false)));
                    if (adf != null && adfExistingList.FirstOrDefault(p => p.DisciplineFrequencyKey == adf.DisciplineFrequencyKey) == null)
                    {
                        adfExistingList.Add(adf);
                    }
                }


                foreach (AdmissionDisciplineFrequency adf in adfExistingList)
                {
                    EncounterStartDisciplineFrequency esdf = GetMyEncounterStartDisciplineFrequency(adf);
                    if (esdf != null && esdf.DisplayDisciplineFrequencyText != adf.DisplayDisciplineFrequencyText)
                    {
                        generatedText = generatedText + ((string.IsNullOrWhiteSpace(generatedText)) ? "" : CR) +
                                        "     Changed " + esdf.DisplayDisciplineFrequencyText + "  to  " +
                                        adf.DisplayDisciplineFrequencyText;
                    }
                }

                return (string.IsNullOrWhiteSpace(generatedText)) ? null : generatedText;
            }
        }

        private bool DuplicateADF(List<AdmissionDisciplineFrequency> adfList, AdmissionDisciplineFrequency adf)
        {
            if ((adfList == null) || (adf == null))
            {
                return false;
            }

            // Check for a newer version of this ADF in the list passed - thru the ADF history
            AdmissionDisciplineFrequency duplicateADF = adfList
                .FirstOrDefault(a => ((a.DisciplineFrequencyKey > adf.DisciplineFrequencyKey) 
                                      && ((a.HistoryKey == adf.DisciplineFrequencyKey) || (((a.HistoryKey != null) 
                                          && (a.HistoryKey == adf.HistoryKey))))));
            if (duplicateADF != null)
            {
                return true;
            }

            return false;
        }

        private string GeneratedEquipmentTextVersion1
        {
            get
            {
                string generatedText = null;
                if ((CurrentIOrderEntry == null) || (CurrentEncounter == null) ||
                    (CurrentEncounter.EncounterEquipment == null) || (CurrentAdmission == null) ||
                    (CurrentAdmission.AdmissionEquipment == null))
                {
                    return null;
                }

                List<AdmissionEquipment> equipNewList = CurrentAdmission.AdmissionEquipment.Where(p => (((p.IsNew)
                                || (p.EncounterEquipment.Any(ee => ee.EncounterKey == CurrentEncounter.EncounterKey)
                                ) && (p.Inactive == false)
                            )
                        )
                    )
                    .OrderBy(p => p.EquipmentDescription).ToList();

                foreach (AdmissionEquipment ae in equipNewList)
                {
                    generatedText = generatedText + ((string.IsNullOrWhiteSpace(generatedText)) ? "" : CR) +
                                    "     " + ae.EquipmentDescription.Trim()
                                    + ((string.IsNullOrEmpty(ae.Comments)) ? null : " " + ae.Comments.Trim());
                }

                return (string.IsNullOrWhiteSpace(generatedText)) ? null : "Equipment Ordered: " + CR + generatedText;
            }
        }

        private string GeneratedEquipmentTextVersion2
        {
            get
            {
                string generatedText = null;
                if ((CurrentIOrderEntry == null) || (CurrentEncounter == null) ||
                    (CurrentEncounter.EncounterEquipment == null) || (CurrentAdmission == null) ||
                    (CurrentAdmission.AdmissionEquipment == null))
                {
                    return null;
                }

                List<AdmissionEquipment> equipNewList = CurrentAdmission.AdmissionEquipment.Where(p => (((p.IsNew)
                                || (p.EncounterEquipment.Any(ee => ee.EncounterKey == CurrentEncounter.EncounterKey)
                                ) && (p.Inactive == false)
                            )
                        )
                    )
                    .OrderBy(p => p.EquipmentDescription).ToList();
                foreach (AdmissionEquipment ae in equipNewList)
                {
                    generatedText = generatedText + ((string.IsNullOrWhiteSpace(generatedText)) ? "" : CR) +
                                    "     " + ae.EquipmentDescription.Trim()
                                    + ((string.IsNullOrEmpty(ae.Comments)) ? null : " " + ae.Comments.Trim());
                }

                return (string.IsNullOrWhiteSpace(generatedText)) ? null : generatedText;
            }
        }

        private string GeneratedSupplyTextVersion1
        {
            get
            {
                string generatedText = null;
                if ((CurrentIOrderEntry == null) || (CurrentEncounter == null) ||
                    (CurrentEncounter.EncounterSupply == null))
                {
                    return null;
                }

                List<EncounterSupply> supplyNewList = CurrentEncounter.EncounterSupply.ToList();
                if (supplyNewList != null)
                {
                    foreach (EncounterSupply ae in supplyNewList)
                    {
                        Supply s = SupplyCache.GetSupplies().FirstOrDefault(su => su.SupplyKey == ae.SupplyKey);
                        if (s != null)
                        {
                            generatedText = generatedText + ((string.IsNullOrWhiteSpace(generatedText)) ? "" : CR) +
                                            "     " + s.Description1.Trim();
                        }
                    }
                }

                return (string.IsNullOrWhiteSpace(generatedText)) ? null : "Supply Ordered: " + CR + generatedText;
            }
        }

        private string GeneratedSupplyTextVersion2
        {
            get
            {
                string generatedText = null;
                if ((CurrentIOrderEntry == null) || (CurrentEncounter == null) ||
                    (CurrentEncounter.EncounterSupply == null))
                {
                    return null;
                }

                List<EncounterSupply> supplyNewList = CurrentEncounter.EncounterSupply.ToList();
                foreach (EncounterSupply ae in supplyNewList)
                {
                    Supply s = SupplyCache.GetSupplies().FirstOrDefault(su => su.SupplyKey == ae.SupplyKey);
                    if (s != null)
                    {
                        generatedText = generatedText + ((string.IsNullOrWhiteSpace(generatedText)) ? "" : CR) +
                                        "     " + s.Description1.Trim();
                    }
                }

                return (string.IsNullOrWhiteSpace(generatedText)) ? null : generatedText;
            }
        }

        private EncounterStartDisciplineFrequency GetMyEncounterStartDisciplineFrequency(
            AdmissionDisciplineFrequency adf)
        {
            // Return the EncounterStartDisciplineFrequency that I am a descendent of
            if ((CurrentAdmission == null) || (CurrentAdmission.AdmissionDisciplineFrequency == null) ||
                (CurrentEncounter == null) || (CurrentEncounter.EncounterStartDisciplineFrequency == null))
            {
                return null;
            }

            foreach (EncounterStartDisciplineFrequency esdf in CurrentEncounter.EncounterStartDisciplineFrequency)
            {
                AdmissionDisciplineFrequency esdfAdmissionDisciplineFrequency = CurrentAdmission.AdmissionDisciplineFrequency
                    .FirstOrDefault(p => p.DisciplineFrequencyKey == esdf.DispFreqKey);
                if (esdfAdmissionDisciplineFrequency == null)
                {
                    return null;
                }

                if (esdfAdmissionDisciplineFrequency.DisciplineFrequencyKey == adf.DisciplineFrequencyKey)
                {
                    return esdf;
                }

                if ((adf.HistoryKey == null) &&
                    (adf.DisciplineFrequencyKey == esdfAdmissionDisciplineFrequency.DisciplineFrequencyKey))
                {
                    return esdf;
                }

                if ((adf.HistoryKey != null) &&
                    (adf.HistoryKey == esdfAdmissionDisciplineFrequency.DisciplineFrequencyKey))
                {
                    return esdf;
                }

                if ((adf.HistoryKey != null) && (adf.HistoryKey == esdfAdmissionDisciplineFrequency.HistoryKey))
                {
                    return esdf;
                }
            }

            return null;
        }

        private string GeneratedGoalText
        {
            get
            {
                if ((CurrentIOrderEntry == null) || (CurrentEncounter == null) || (CurrentAdmission == null) ||
                    (CurrentAdmission.AdmissionGoal == null))
                {
                    return null;
                }

                string generatedText = string.Empty;

                string description = null;
                List<OrderEntryGoalElementItem> geList = new List<OrderEntryGoalElementItem>();
                foreach (AdmissionGoal ag in CurrentAdmission.AdmissionGoal)
                    if (ag.AdmissionGoalElement != null)
                    {
                        List<AdmissionGoalElement> ageList = ag.AdmissionGoalElement.Where(p =>
                            ((p.AddedFromEncounterKey == CurrentEncounter.EncounterKey) || ((p.Discontinued) &&
                                (p.DiscontinuedFromEncounterKey == CurrentEncounter.EncounterKey)))).ToList();
                        foreach (AdmissionGoalElement age in ageList)
                        {
                            EncounterGoalElement ege = null;
                            if ((CurrentEncounter != null) && (CurrentEncounter.EncounterGoalElement != null))
                            {
                                ege = CurrentEncounter.EncounterGoalElement
                                    .FirstOrDefault(eg => eg.AdmissionGoalElementKey == age.AdmissionGoalElementKey);
                            }

                            GoalElement ge = GoalCache.GetGoalElementByKey(age.GoalElementKey);
                            if ((ege != null) && (ge != null) && ge.Orders)
                            {
                                description = (String.IsNullOrWhiteSpace(ge.POCOverrideText))
                                    ? age.GoalElementText
                                    : ge.POCOverrideText;
                                if (geList.Any(p => (p.Description == description)) == false)
                                {
                                    geList.Add(new OrderEntryGoalElementItem
                                    {
                                        Discipline = "",
                                        Description = description,
                                        Discontinued = age.Discontinued
                                    });
                                }
                            }
                        }
                    }

                geList = geList.OrderBy(p => p.Discipline).ThenBy(p => p.Description).ToList();
                foreach (OrderEntryGoalElementItem gei in geList)
                    generatedText = generatedText + ((string.IsNullOrWhiteSpace(generatedText)) ? "" : CR) + "     " +
                                    ((gei.Discontinued) ? "Discontinued: " : "Added: ") + gei.Description;

                return (string.IsNullOrWhiteSpace(generatedText)) ? null : generatedText;
            }
        }

        private string GeneratedLabText
        {
            get
            {
                if ((CurrentIOrderEntry == null) || (CurrentEncounter == null) || (CurrentPatient == null) ||
                    (CurrentPatient.PatientLab == null))
                {
                    return null;
                }

                string generatedText = string.Empty;

                List<PatientLab> plList = CurrentPatient.PatientLab
                    .Where(p => ((p.IsNew) || (p.AddedFromEncounterKey == CurrentEncounter.EncounterKey)))
                    .OrderByDescending(p => p.OrderDate).ThenBy(p => p.Category).ThenBy(p => p.Test).ToList();

                foreach (PatientLab pl in plList)
                    generatedText = generatedText + ((string.IsNullOrWhiteSpace(generatedText)) ? "" : CR) + "     " +
                                    pl.Category.Trim() + " - " + pl.Test.Trim() +
                                    GetDateTimeFormatted(pl.TestDate, "on") + ((string.IsNullOrWhiteSpace(pl.Comment))
                                        ? ""
                                        : ", Comments: " + pl.Comment);
                return (string.IsNullOrWhiteSpace(generatedText)) ? null : generatedText;
            }
        }

        private string GeneratedMedicationText
        {
            get
            {
                if ((CurrentIOrderEntry == null) || (CurrentEncounter == null) ||
                    (CurrentEncounter.EncounterMedication == null) || (CurrentPatient == null) ||
                    (CurrentPatient.PatientLab == null))
                {
                    return null;
                }

                string generatedText = string.Empty;

                List<PatientMedication> pmNewList = CurrentPatient.PatientMedication
                    .Where(p => (((p.IsNew) || (p.AddedFromEncounterKey == CurrentEncounter.EncounterKey)) && (p.AddedInError == false)))
                    .OrderByDescending(p => p.MedicationName)
                    .ThenBy(p => p.MedicationStartDateTime)
                    .ToList();

                foreach (PatientMedication pm in pmNewList)
                {
                    if (DuplicateMed(pmNewList, pm))
                    {
                        continue; // bypass this med - there is a later version against this encounter
                    }

                    EncounterStartMedication esm = GetMyEncounterStartMedication(pm);
                    if (esm != null)
                    {
                        continue; // bypass this med - it is an edit of an existing med - not a newly added med
                    }

                    generatedText = generatedText
                                    + ((string.IsNullOrWhiteSpace(generatedText)) ? "" : CR)
                                    + "     " + pm.MedicationDescription
                                    + GetDateTimeFormatted(pm.MedicationStartDateTime, ", Start", true)
                                    + GetDateTimeFormatted(pm.MedicationEndDateTime, "End", true)
                                    + ((string.IsNullOrWhiteSpace(pm.Comment)) ? "" : ", Comments: " + pm.Comment);
                    if (pm.IsIV)
                    {
                        generatedText += GetIVFieldInfomation(pm);
                    }
                }

                List<PatientMedication> pmExistingList = new List<PatientMedication>();
                foreach (EncounterMedication em in CurrentEncounter.EncounterMedication)
                {
                    PatientMedication pm = CurrentPatient.PatientMedication.FirstOrDefault(p => ((p.PatientMedicationKey == em.MedicationKey) && (p.AddedInError == false)));

                    if ((pm != null) && (pmExistingList.FirstOrDefault(p => p.PatientMedicationKey == pm.PatientMedicationKey) == null))
                    {
                        pmExistingList.Add(pm);
                    }
                }

                foreach (PatientMedication pm in pmExistingList)
                {
                    EncounterStartMedication esm = GetMyEncounterStartMedication(pm);
                    if ((esm != null) &&
                        (((esm.MedicationEndDate == null) && (pm.MedicationEndDateTime != null)) ||
                         ((esm.MedicationEndDate != null) && (pm.MedicationEndDateTime != null) &&
                          ((DateTime)esm.MedicationEndDate) != (DateTime)pm.MedicationEndDateTime)))
                    {
                        generatedText = generatedText
                                        + ((string.IsNullOrWhiteSpace(generatedText)) ? "" : CR)
                                        + "     Discontinued " + pm.MedicationDescription
                                        + GetDateTimeFormatted(pm.MedicationStartDateTime, ", Start", true)
                                        + GetDateTimeFormatted(pm.MedicationEndDateTime, "End", true)
                                        + ((string.IsNullOrWhiteSpace(pm.Comment))
                                            ? ""
                                            : ", Comments: "
                                              + pm.Comment);
                        if (pm.IsIV)
                        {
                            generatedText += GetIVFieldInfomation(pm);
                        }
                    }
                }

                return (string.IsNullOrWhiteSpace(generatedText)) ? null : generatedText;
            }
        }

        private bool DuplicateMed(List<PatientMedication> pmList, PatientMedication pm)
        {
            if ((pmList == null) || (pm == null))
            {
                return false;
            }

            // Check for a newer version of this med in the list passed - thru the medication history
            PatientMedication duplicateMed = pmList.FirstOrDefault(p => 
                (p.PatientMedicationKey > pm.PatientMedicationKey) 
                && (p.HistoryKey == pm.PatientMedicationKey || ((p.HistoryKey != null) && (p.HistoryKey == pm.HistoryKey))));
            if (duplicateMed != null)
            {
                return true;
            }

            return false;
        }

        private string GetIVFieldInfomation(PatientMedication med)
        {
            var output = string.Empty;
            if (med.IsIV)
            {
                //NOTE: couldn't use StringBuilder.AppendLine() - got line spaces between rows
                //NOTE: GeneratedMedicationText indents Medication string 5 spaces.  Indent 5 more so that IV information is indented under the medication.
                output += string.Format("\r\t\t\t{0, -32}\t\t\t\t\t\t{1}", "IV Concentration", med.IVConcentration);
                output += string.Format("\r\t\t\t{0, -32}\t\t\t\t\t\t{1}", "IV Rate(ml/hr)",
                    (med.IVRate.HasValue) ? med.IVRate.ToString() : string.Empty);
                output += string.Format("\r\t\t\t{0, -32}\t\t\t\t{1}", "First Dose in Controlled Setting",
                    med.IVFirstInControlledSetting.ToString());
                output += string.Format("\r\t\t\t{0, -32}\t\t\t\t{1}", "Continuous or Intermittent",
                    med.IVContinuousString);
                output += string.Format("\r\t\t\t{0, -32}\t\t\t\t\t\t\t{1}", "IV Type", med.IVTypeString);
            }
            
            return output;
        }

        private EncounterStartMedication GetMyEncounterStartMedication(PatientMedication pm)
        {
            // Return the EncounterStartMedication that I am a descendent of
            if ((CurrentPatient == null) || (CurrentPatient.PatientMedication == null) || (CurrentEncounter == null) ||
                (CurrentEncounter.EncounterStartMedication == null))
            {
                return null;
            }

            foreach (EncounterStartMedication esm in CurrentEncounter.EncounterStartMedication)
            {
                PatientMedication esmPatientMedication = CurrentPatient.PatientMedication.FirstOrDefault(p => p.PatientMedicationKey == esm.MedicationKey);
                if (esmPatientMedication == null)
                {
                    return null;
                }

                if (esmPatientMedication.PatientMedicationKey == pm.PatientMedicationKey)
                {
                    return esm;
                }

                if ((pm.HistoryKey == null) && (pm.PatientMedicationKey == esmPatientMedication.PatientMedicationKey))
                {
                    return esm;
                }

                if ((pm.HistoryKey != null) && (pm.HistoryKey == esmPatientMedication.PatientMedicationKey))
                {
                    return esm;
                }

                if ((pm.HistoryKey != null) && (pm.HistoryKey == esmPatientMedication.HistoryKey))
                {
                    return esm;
                }
            }

            return null;
        }

        private string GetDateTimeFormatted(DateTime? dateTime, string prefix, bool includeTime = false)
        {
            string date = (dateTime == null) ? "" : Convert.ToDateTime((DateTime)dateTime).ToShortDateString();
            string time = "";
            if ((dateTime != null) && (includeTime))
            {
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    time = Convert.ToDateTime(dateTime).ToString("HHmm");
                }
                else
                {
                    time = Convert.ToDateTime(dateTime).ToShortTimeString();
                }
            }

            string retDT = (date + " " + time).Trim();
            return (string.IsNullOrWhiteSpace(retDT)) ? "" : " " + prefix + " " + retDT;
        }

        internal async System.Threading.Tasks.Task OKProcessing(bool AllValid, bool isOnline, Func<System.Threading.Tasks.Task> myAction)
        {
            if (AllValid)
            {
                await myAction();
            }
            else
            {
                await myAction();
            }
        }

        internal void AlreadySignedOrder(Action myAction)
        {
            // Logic is outlined in wireframe of US 28772
            bool AdmissionIsAdmitted = CurrentAdmission.SOCDate.HasValue &&
                                       CurrentAdmission.AdmissionStatus ==
                                       (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "A");

            // If Admission is not admitted, perform special logic
            if (!AdmissionIsAdmitted && !CurrentOrderEntry.SignedByPhysicianChecked)
            {
                ShowErrorDialog(myAction);
            }
            else if (CurrentOrderEntry.SignedByPhysicianChecked)
            {
                CurrentOrderEntry.OrderStatus = (int)OrderStatusType.SigningPhysicianVerified;
                CurrentOrderEntry.SigningPhysicianVerifiedDate = DateTimeOffset.UtcNow.Date;

                // myAction is responsible for saving DynamicForm
                myAction();
            }
            else
            {
                myAction();
            }
        }

        private void ShowErrorDialog(Action myAction)
        {
            NavigateCloseDialog d = new NavigateCloseDialog
            {
                NoVisible = true,
                YesButton =
                {
                    Content = "Send Interim Order for Signature",
                    Width = double.NaN
                },
                NoButton =
                {
                    Content = "ONLY include on Plan of Care",
                    Width = double.NaN
                },
                Title = "Message",
                Width = double.NaN,
                Height = double.NaN,
                HasCloseButton = false,
                ErrorMessage = "The order date is prior to the SOC date for this admission OR the SOC date has not yet been established. Do you want this order to be sent to the physician for signature or ONLY include it on the Plan of Care?"
            };

            d.Closed += (s, err) =>
            {
                if (s != null)
                {
                    var _ret = ((ChildWindow)s).DialogResult;
                    if (_ret == false) // The user selected 'Do not send for Signature'
                    {
                        CurrentOrderEntry.OrderStatus = (int)OrderStatusType.SigningPhysicianVerified;
                        CurrentOrderEntry.SigningPhysicianVerifiedDate = DateTimeOffset.UtcNow.Date;
                    }
                    else
                    {
                        CurrentOrderEntry.OrderStatus = (int)OrderStatusType.Completed;
                    }

                    // Fire action to save DynamicForm
                    myAction();
                }
            };

            d.Show();
        }

        public void ForceDiscardAttemptedVisit()
        {
            if (CurrentOrderEntryVO == null)
            {
                return;
            }

            if (CurrentOrderEntryVO.CanEditOrder == false)
            {
                return;
            }

            CurrentOrderEntryVO.Signature = null;
            CurrentOrderEntryVO.DiscardFlag = true;
            if (string.IsNullOrWhiteSpace(CurrentOrderEntryVO.DiscardReason))
            {
                CurrentOrderEntryVO.DiscardReason = "Attempted Visit";
            }
        }

        public void ForceDiscardNotTaken()
        {
            if (CurrentOrderEntryVO == null)
            {
                return;
            }

            if (CurrentOrderEntryVO.CanEditOrder == false)
            {
                return;
            }

            CurrentOrderEntryVO.Signature = null;
            CurrentOrderEntryVO.DiscardFlag = true;
            if (string.IsNullOrWhiteSpace(CurrentOrderEntryVO.DiscardReason))
            {
                CurrentOrderEntryVO.DiscardReason = "Patient not taken under care";
            }
        }

        public void ForceUnDiscardNotTaken()
        {
            if (CurrentOrderEntryVO == null)
            {
                return;
            }

            if (CurrentOrderEntryVO.CanEditOrder == false)
            {
                return;
            }

            if (CurrentOrderEntryVO.DiscardFlag == false)
            {
                return;
            }

            if (CurrentOrderEntryVO.DiscardReason != "Patient not taken under care")
            {
                return;
            }

            CurrentOrderEntryVO.DiscardFlag = false;
            CurrentOrderEntryVO.DiscardReason = null;
        }
    }
}