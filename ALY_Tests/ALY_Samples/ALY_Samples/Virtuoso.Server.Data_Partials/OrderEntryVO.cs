#region Usings

using System;
using System.Collections.Generic;
using Virtuoso.Core;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Converters;
using Virtuoso.Core.Utility;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class OrderEntryVO : IOrderEntry
    {
        private bool _IsReviewed;

        private bool _IsVoided;
        private ServiceLineGrouping _ServiceLineGroupingZero;

        public ServiceLineGrouping ServiceLineGroupingZero
        {
            get
            {
                if (_ServiceLineGroupingZero != null)
                {
                    return _ServiceLineGroupingZero;
                }

                if (Admission == null)
                {
                    return null;
                }

                var admissionGroupDate = CompletedDate == null ? DateTime.Today.Date : CompletedDate.Value.Date;
                var ag = Admission.GetNthCurrentGroup(0, admissionGroupDate);
                if (ag == null)
                {
                    return null;
                }

                _ServiceLineGroupingZero = ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey);
                return _ServiceLineGroupingZero;
            }
        }

        public ServiceLine ServiceLine
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                var _ServiceLine = ServiceLineCache.GetServiceLineFromKey(Admission.ServiceLineKey);
                return _ServiceLine;
            }
        }

        public string AgencyFullAddress
        {
            get
            {
                var address = string.Empty;
                var CR = char.ToString('\r');
                if (!string.IsNullOrWhiteSpace(AgencyAddress1))
                {
                    address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + AgencyAddress1;
                }

                if (!string.IsNullOrWhiteSpace(AgencyAddress2))
                {
                    address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + AgencyAddress2;
                }

                if (!string.IsNullOrWhiteSpace(AgencyCityStateZip))
                {
                    address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + AgencyCityStateZip;
                }

                return address;
            }
        }

        public string AgencyFax => FormatPhoneNumber(ProviderFax);

        public bool ShowVOSignature => !DiscardFlag;

        public string AgencyName
        {
            get
            {
                if (ServiceLineGroupingZero != null &&
                    string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress1) == false &&
                    string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyName) == false)
                {
                    return ServiceLineGroupingZero.AgencyName;
                }

                return string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.Name)
                    ? "?"
                    : TenantSettingsCache.Current.TenantSetting.Name;
            }
        }

        public string AgencyAddress1
        {
            get
            {
                if (ServiceLineGroupingZero != null &&
                    string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress1) == false)
                {
                    return ServiceLineGroupingZero.AgencyAddress1;
                }

                if (ServiceLine != null && string.IsNullOrWhiteSpace(ServiceLine.Address1) == false)
                {
                    return ServiceLine.Address1;
                }

                return string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.Address1)
                    ? "?"
                    : TenantSettingsCache.Current.TenantSetting.Address1;
            }
        }

        public string AgencyAddress2
        {
            get
            {
                if (ServiceLineGroupingZero != null &&
                    string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress1) == false)
                {
                    return string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress2)
                        ? null
                        : ServiceLineGroupingZero.AgencyAddress2;
                }

                if (ServiceLine != null && string.IsNullOrWhiteSpace(ServiceLine.Address1) == false)
                {
                    return ServiceLine.Address2;
                }

                return string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.Address2)
                    ? null
                    : TenantSettingsCache.Current.TenantSetting.Address2;
            }
        }

        public string AgencyCityStateZip
        {
            get
            {
                if (ServiceLineGroupingZero != null &&
                    string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress1) == false)
                {
                    return string.Format("{0}, {1}     {2}",
                        string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyCity)
                            ? "City ?"
                            : ServiceLineGroupingZero.AgencyCity,
                        string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyStateCodeCode)
                            ? "State ?"
                            : ServiceLineGroupingZero.AgencyStateCodeCode,
                        string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyZipCode)
                            ? "ZipCode ?"
                            : ServiceLineGroupingZero.AgencyZipCode);
                }

                if (ServiceLine != null && string.IsNullOrWhiteSpace(ServiceLine.Address1) == false)
                {
                    return string.Format("{0}, {1}     {2}",
                        string.IsNullOrWhiteSpace(ServiceLine.City) ? "City ?" : ServiceLine.City,
                        string.IsNullOrWhiteSpace(ServiceLine.StateCodeCode) ? "State ?" : ServiceLine.StateCodeCode,
                        string.IsNullOrWhiteSpace(ServiceLine.ZipCode) ? "ZipCode ?" : ServiceLine.ZipCode);
                }

                return string.Format("{0}, {1}     {2}",
                    string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.City)
                        ? "City ?"
                        : TenantSettingsCache.Current.TenantSetting.City,
                    string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.StateCodeCode)
                        ? "State ?"
                        : TenantSettingsCache.Current.TenantSetting.StateCodeCode,
                    string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.ZipCode)
                        ? "ZipCode ?"
                        : TenantSettingsCache.Current.TenantSetting.ZipCode);
            }
        }

        public string AgencyPhone
        {
            get
            {
                if (ServiceLineGroupingZero != null &&
                    string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyPhoneNumber) == false)
                {
                    return FormatPhoneNumber(ServiceLineGroupingZero.AgencyPhoneNumber);
                }

                if (Admission == null)
                {
                    return null;
                }

                return Admission.ServiceLineFormattedPhoneNumber;
            }
        }

        public bool IsVoided
        {
            get { return _IsVoided; }
            set
            {
                _IsVoided = value;
                if (value)
                {
                    if (VoidDate == null)
                    {
                        VoidDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                        VoidBy = WebContext.Current.User.MemberID;
                    }
                }
                else
                {
                    VoidDate = null;
                    VoidBy = null;
                    VoidReason = null;
                }

                RaisePropertyChanged("IsVoided");
                RaisePropertyChanged("VoidText");
                RaisePropertyChanged("CanEditOrder");
                RaisePropertyChanged("CanEditOrderData");
                RaisePropertyChanged("CanRefreshOrderText");
                RaisePropertyChanged("CanRefreshOverrideReferral");
                RaisePropertyChanged("CanRefreshOverrideVisitFrequency");
                RaisePropertyChanged("CanRefreshOverrideGoals");
                RaisePropertyChanged("CanRefreshOverrideLabs");
                RaisePropertyChanged("CanRefreshOverrideInitialServiceOrder");
                RaisePropertyChanged("CanRefreshOverrideMedications");
                RaisePropertyChanged("CanRefreshOverrideEquipment");
                RaisePropertyChanged("CanRefreshOverrideSupply");
                RaisePropertyChanged("CanRefreshOverrideOther");
                RaisePropertyChanged("CanRefreshOverrideRecertificationOrder");
                RaisePropertyChanged("CanEditOrderReviewed");
                RaisePropertyChanged("CanEditOrderSignature");
                RaisePropertyChanged("CanEditOrderSigningPhysicianVerified");
                RaisePropertyChanged("CanFullEdit");
            }
        }

        public string CompletedOnText
        {
            get
            {
                if (CompletedDate == null)
                {
                    return null;
                }

                var dateTime = Convert.ToDateTime(((DateTimeOffset)CompletedDate).DateTime).ToShortDateString();
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    dateTime = dateTime + " " +
                               Convert.ToDateTime(((DateTimeOffset)CompletedDate).DateTime).ToString("HHmm");
                }
                else
                {
                    dateTime = dateTime + " " + Convert.ToDateTime(((DateTimeOffset)CompletedDate).DateTime)
                        .ToShortTimeString();
                }

                if (string.IsNullOrWhiteSpace(dateTime))
                {
                    return null;
                }

                return string.Format(" on {0}", dateTime);
            }
        }

        public string VoidText
        {
            get
            {
                if (VoidDate == null)
                {
                    return null;
                }

                var dateTime = Convert.ToDateTime(((DateTimeOffset)VoidDate).DateTime).ToShortDateString();
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    dateTime = dateTime + " " +
                               Convert.ToDateTime(((DateTimeOffset)VoidDate).DateTime).ToString("HHmm");
                }
                else
                {
                    dateTime = dateTime + " " +
                               Convert.ToDateTime(((DateTimeOffset)VoidDate).DateTime).ToShortTimeString();
                }

                if (string.IsNullOrWhiteSpace(dateTime))
                {
                    return null;
                }

                return string.Format("Order voided on {0}  by {1}", dateTime,
                    UserCache.Current.GetFormalNameFromUserId(VoidBy));
            }
        }

        public bool IsVoidedVisible => !IsNew;
        public string SigningPhysicianName =>
            PhysicianCache.Current.GetPhysicianFullNameInformalWithSuffixFromKey(SigningPhysicianKey);

        public Physician SigningPhysician
        {
            get
            {
                if (SigningPhysicianKey == null)
                {
                    return null;
                }

                return PhysicianCache.Current.GetPhysicianFromKey(SigningPhysicianKey);
            }
        }

        // Since it is now display only and only shows for legacy orders...
        public bool IsSigningPhysicianVerified
        {
            get { return SigningPhysicianVerifiedDate == null ? false : true; }
            set { }
        }

        public string SigningPhysicianVerifiedText
        {
            get
            {
                if (SigningPhysicianVerifiedDate == null)
                {
                    return null;
                }

                var dateTime = Convert.ToDateTime(((DateTimeOffset)SigningPhysicianVerifiedDate).DateTime)
                    .ToShortDateString();
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    dateTime = dateTime + " " + Convert
                        .ToDateTime(((DateTimeOffset)SigningPhysicianVerifiedDate).DateTime).ToString("HHmm");
                }
                else
                {
                    dateTime = dateTime + " " + Convert
                        .ToDateTime(((DateTimeOffset)SigningPhysicianVerifiedDate).DateTime).ToShortTimeString();
                }

                if (string.IsNullOrWhiteSpace(dateTime))
                {
                    return null;
                }

                return string.Format("Verified physician signed order on {0}  by {1}", dateTime,
                    UserCache.Current.GetFormalNameFromUserId(SigningPhysicianVerifiedBy));
            }
        }

        public bool IsSigningPhysicianVerifiedVisible
        {
            get
            {
                if (IsSigningPhysicianVerified)
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanEditOrdersVoided
        {
            get
            {
                if (OrderStatus == (int)OrderStatusType.Voided)
                {
                    return false; //cannot edit data on a voided order BUG 5525
                }

                if (IsOrderInactive)
                {
                    return false;
                }

                if (IsVoidedVisible == false)
                {
                    return false;
                }

                if (CanEditOrderData)
                {
                    return true;
                }

                // if user has OrderEntry or OrderEntryReviewer role they can Void
                if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEdit, false))
                {
                    return true;
                }

                if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEntryReviewer, false))
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanEditOrderSigningPhysicianVerified
        {
            get
            {
                if (OrderStatus == (int)OrderStatusType.Voided)
                {
                    return false; //cannot edit data on a voided order BUG 5525
                }

                if (IsOrderInactive)
                {
                    return false;
                }

                if (IsSigningPhysicianVerifiedVisible == false)
                {
                    return false;
                }

                // if user has OrderEntry or OrderEntryReviewer role they can VerifyPhysicianSignature
                if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEdit, false))
                {
                    return true;
                }

                if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEntryReviewer, false))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsReviewed
        {
            get { return _IsReviewed; }
            set
            {
                _IsReviewed = value;
                if (value)
                {
                    if (ReviewDate == null)
                    {
                        ReviewDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                        ReviewBy = WebContext.Current.User.MemberID;
                    }
                }
                else
                {
                    ReviewDate = null;
                    ReviewBy = null;
                    ReviewComment = null;
                    CoSign = false;
                }

                RaisePropertyChanged("IsReviewed");
                RaisePropertyChanged("ReviewText");
                RaisePropertyChanged("ReviewComment");
                RaisePropertyChanged("CoSign");
                RaisePropertyChanged("IsReviewedCommentsVisible");
            }
        }

        public string ReviewText
        {
            get
            {
                if (ReviewDate == null)
                {
                    return null;
                }

                var dateTime = Convert.ToDateTime(((DateTimeOffset)ReviewDate).DateTime).ToShortDateString();
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    dateTime = dateTime + " " +
                               Convert.ToDateTime(((DateTimeOffset)ReviewDate).DateTime).ToString("HHmm");
                }
                else
                {
                    dateTime = dateTime + " " +
                               Convert.ToDateTime(((DateTimeOffset)ReviewDate).DateTime).ToShortTimeString();
                }

                if (string.IsNullOrWhiteSpace(dateTime))
                {
                    return null;
                }

                return string.Format("Order reviewed on {0}  by {1}", dateTime,
                    UserCache.Current.GetFormalNameFromUserId(ReviewBy));
            }
        }

        public bool IsReviewedVisible
        {
            get
            {
                if (OrderStatus == (int)OrderStatusType.OrderEntryReview)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsReviewedCommentsVisible
        {
            get
            {
                var visible = OrderStatus == (int)OrderStatusType.OrderEntryReview && IsReviewed;
                if (OrderStatus == (int)OrderStatusType.OrderEntryReview)
                {
                    return true;
                }

                return visible;
            }
        }

        public bool IsOrderStatusInProcess => OrderStatus == (int)OrderStatusType.InProcess ? true : false;

        public bool IsOrderStatusOrderEntryReview =>
            OrderStatus == (int)OrderStatusType.OrderEntryReview ? true : false;

        public bool IsOrderStatusCompleted => OrderStatus == (int)OrderStatusType.Completed ? true : false;

        public bool IsOrderStatusSigningPhysicianVerified =>
            OrderStatus == (int)OrderStatusType.SigningPhysicianVerified ? true : false;

        public bool IsOrderStatusVoided => OrderStatus == (int)OrderStatusType.Voided ? true : false;

        public bool CanEditOrderReviewed
        {
            get
            {
                if (OrderStatus == (int)OrderStatusType.Voided)
                {
                    return false; //cannot edit data on a voided order BUG 5525
                }

                if (IsOrderInactive)
                {
                    return false;
                }

                if (IsReviewedVisible == false)
                {
                    return false;
                }

                // can only review in review status - (implies once reviewed - can't go back to unreviewed)
                if (OrderStatus != (int)OrderStatusType.OrderEntryReview)
                {
                    return false;
                }

                // if user has OrderEntryReviewer role they can review
                return RoleAccessHelper.CheckPermission(RoleAccess.OrderEntryReviewer, false) ? true : false;
            }
        }

        public bool CanPrint
        {
            get
            {
                if (OrderStatus == (int)OrderStatusType.Completed)
                {
                    return true;
                }

                if (OrderStatus == (int)OrderStatusType.SigningPhysicianVerified)
                {
                    return true;
                }

                if (OrderStatus == (int)OrderStatusType.Voided)
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanEditOrder
        {
            get
            {
                if (OrderStatus == (int)OrderStatusType.Voided)
                {
                    return false; //cannot edit data on a voided order BUG 5525
                }

                if (IsOrderInactive)
                {
                    return false;
                }

                if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEntryReviewer, false))
                {
                    return true;
                }

                if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEdit, false))
                {
                    return true;
                }

                return false;
            }
        }

        public string PreviousGeneratedInitialServiceOrder { get; set; }
        public string PreviousGeneratedOther { get; set; }
        public string PreviousGeneratedRecertificationOrder { get; set; }
        public int PreviousOrderStatus { get; set; }

        public bool IsGeneratedOrderTextEqualToOrderText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GeneratedOrderText) && string.IsNullOrWhiteSpace(OrderText))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(GeneratedOrderText) && string.IsNullOrWhiteSpace(OrderText) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GeneratedOrderText) == false && string.IsNullOrWhiteSpace(OrderText))
                {
                    return false;
                }

                if (GeneratedOrderText.Trim() == OrderText.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsGeneratedReferralEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GeneratedReferral) && string.IsNullOrWhiteSpace(OverrideReferral))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(GeneratedReferral) &&
                    string.IsNullOrWhiteSpace(OverrideReferral) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GeneratedReferral) == false &&
                    string.IsNullOrWhiteSpace(OverrideReferral))
                {
                    return false;
                }

                if (GeneratedReferral.Trim() == OverrideReferral.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsGeneratedVisitFrequencyEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GeneratedVisitFrequency) &&
                    string.IsNullOrWhiteSpace(OverrideVisitFrequency))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(GeneratedVisitFrequency) &&
                    string.IsNullOrWhiteSpace(OverrideVisitFrequency) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GeneratedVisitFrequency) == false &&
                    string.IsNullOrWhiteSpace(OverrideVisitFrequency))
                {
                    return false;
                }

                if (GeneratedVisitFrequency.Trim() == OverrideVisitFrequency.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsGeneratedGoalsEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GeneratedGoals) && string.IsNullOrWhiteSpace(OverrideGoals))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(GeneratedGoals) && string.IsNullOrWhiteSpace(OverrideGoals) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GeneratedGoals) == false && string.IsNullOrWhiteSpace(OverrideGoals))
                {
                    return false;
                }

                if (GeneratedGoals.Trim() == OverrideGoals.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsGeneratedLabsEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GeneratedLabs) && string.IsNullOrWhiteSpace(OverrideLabs))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(GeneratedLabs) && string.IsNullOrWhiteSpace(OverrideLabs) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GeneratedLabs) == false && string.IsNullOrWhiteSpace(OverrideLabs))
                {
                    return false;
                }

                if (GeneratedLabs.Trim() == OverrideLabs.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsGeneratedInitialServiceOrderEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GeneratedInitialServiceOrder) &&
                    string.IsNullOrWhiteSpace(OverrideInitialServiceOrder))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(GeneratedInitialServiceOrder) &&
                    string.IsNullOrWhiteSpace(OverrideInitialServiceOrder) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GeneratedInitialServiceOrder) == false &&
                    string.IsNullOrWhiteSpace(OverrideInitialServiceOrder))
                {
                    return false;
                }

                if ("Initial Order for Start of Care:" + char.ToString('\r') + GeneratedInitialServiceOrder.Trim() ==
                    OverrideInitialServiceOrder.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsPreviousGeneratedInitialServiceOrderEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PreviousGeneratedInitialServiceOrder) &&
                    string.IsNullOrWhiteSpace(OverrideInitialServiceOrder))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(PreviousGeneratedInitialServiceOrder) &&
                    string.IsNullOrWhiteSpace(OverrideInitialServiceOrder) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(PreviousGeneratedInitialServiceOrder) == false &&
                    string.IsNullOrWhiteSpace(OverrideInitialServiceOrder))
                {
                    return false;
                }

                if ("Initial Order for Start of Care:" + char.ToString('\r') +
                    PreviousGeneratedInitialServiceOrder.Trim() == OverrideInitialServiceOrder.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsGeneratedMedicationsEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GeneratedMedications) &&
                    string.IsNullOrWhiteSpace(OverrideMedications))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(GeneratedMedications) &&
                    string.IsNullOrWhiteSpace(OverrideMedications) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GeneratedMedications) == false &&
                    string.IsNullOrWhiteSpace(OverrideMedications))
                {
                    return false;
                }

                if (GeneratedMedications.Trim() == OverrideMedications.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsGeneratedEquipmentEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GeneratedEquipment) && string.IsNullOrWhiteSpace(OverrideEquipment))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(GeneratedEquipment) &&
                    string.IsNullOrWhiteSpace(OverrideEquipment) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GeneratedEquipment) == false &&
                    string.IsNullOrWhiteSpace(OverrideEquipment))
                {
                    return false;
                }

                if (GeneratedEquipment.Trim() == OverrideEquipment.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsGeneratedSupplyEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GeneratedSupply) && string.IsNullOrWhiteSpace(OverrideSupply))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(GeneratedSupply) && string.IsNullOrWhiteSpace(OverrideSupply) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GeneratedSupply) == false && string.IsNullOrWhiteSpace(OverrideSupply))
                {
                    return false;
                }

                if (GeneratedSupply.Trim() == OverrideSupply.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsGeneratedOtherEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GeneratedOther) && string.IsNullOrWhiteSpace(OverrideOther))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(GeneratedOther) && string.IsNullOrWhiteSpace(OverrideOther) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GeneratedOther) == false && string.IsNullOrWhiteSpace(OverrideOther))
                {
                    return false;
                }

                if ("Other Orders:" + char.ToString('\r') + GeneratedOther.Trim() == OverrideOther.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsPreviousGeneratedOtherEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PreviousGeneratedOther) && string.IsNullOrWhiteSpace(OverrideOther))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(PreviousGeneratedOther) &&
                    string.IsNullOrWhiteSpace(OverrideOther) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(PreviousGeneratedOther) == false &&
                    string.IsNullOrWhiteSpace(OverrideOther))
                {
                    return false;
                }

                if ("Other Orders:" + char.ToString('\r') + PreviousGeneratedOther.Trim() ==
                    OverrideOther.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsGeneratedRecertificationOrderEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GeneratedRecertificationOrder) &&
                    string.IsNullOrWhiteSpace(OverrideRecertificationOrder))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(GeneratedRecertificationOrder) &&
                    string.IsNullOrWhiteSpace(OverrideRecertificationOrder) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(GeneratedRecertificationOrder) == false &&
                    string.IsNullOrWhiteSpace(OverrideRecertificationOrder))
                {
                    return false;
                }

                if ("Recertification Order:" + char.ToString('\r') + GeneratedRecertificationOrder.Trim() ==
                    OverrideRecertificationOrder.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsPreviousGeneratedRecertificationOrderEqualToOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PreviousGeneratedRecertificationOrder) &&
                    string.IsNullOrWhiteSpace(OverrideRecertificationOrder))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(PreviousGeneratedRecertificationOrder) &&
                    string.IsNullOrWhiteSpace(OverrideRecertificationOrder) == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(PreviousGeneratedRecertificationOrder) == false &&
                    string.IsNullOrWhiteSpace(OverrideRecertificationOrder))
                {
                    return false;
                }

                if ("Recertification Order:" + char.ToString('\r') + PreviousGeneratedRecertificationOrder.Trim() ==
                    OverrideRecertificationOrder.Trim())
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanRefreshOrderText
        {
            get
            {
                if (CanEditOrderData == false)
                {
                    return false;
                }

                return IsGeneratedOrderTextEqualToOrderText ? false : true;
            }
        }

        public bool CanRefreshOverrideReferral
        {
            get
            {
                if (CanEditOrderData == false)
                {
                    return false;
                }

                return IsGeneratedReferralEqualToOverride ? false : true;
            }
        }

        public bool CanRefreshOverrideVisitFrequency
        {
            get
            {
                if (CanEditOrderData == false)
                {
                    return false;
                }

                return IsGeneratedVisitFrequencyEqualToOverride ? false : true;
            }
        }

        public bool CanRefreshOverrideGoals
        {
            get
            {
                if (CanEditOrderData == false)
                {
                    return false;
                }

                return IsGeneratedGoalsEqualToOverride ? false : true;
            }
        }

        public bool CanRefreshOverrideLabs
        {
            get
            {
                if (CanEditOrderData == false)
                {
                    return false;
                }

                return IsGeneratedLabsEqualToOverride ? false : true;
            }
        }

        public bool CanRefreshOverrideInitialServiceOrder
        {
            get
            {
                if (CanEditOrderData == false)
                {
                    return false;
                }

                return IsGeneratedInitialServiceOrderEqualToOverride ? false : true;
            }
        }

        public bool CanRefreshOverrideMedications
        {
            get
            {
                if (CanEditOrderData == false)
                {
                    return false;
                }

                return IsGeneratedMedicationsEqualToOverride ? false : true;
            }
        }

        public bool CanRefreshOverrideEquipment
        {
            get
            {
                if (CanEditOrderData == false)
                {
                    return false;
                }

                return IsGeneratedEquipmentEqualToOverride ? false : true;
            }
        }

        public bool CanRefreshOverrideSupply
        {
            get
            {
                if (CanEditOrderData == false)
                {
                    return false;
                }

                return IsGeneratedSupplyEqualToOverride ? false : true;
            }
        }

        public bool CanRefreshOverrideOther
        {
            get
            {
                if (CanEditOrderData == false)
                {
                    return false;
                }

                return IsGeneratedOtherEqualToOverride ? false : true;
            }
        }

        public bool CanRefreshOverrideRecertificationOrder
        {
            get
            {
                if (CanEditOrderData == false)
                {
                    return false;
                }

                return IsGeneratedRecertificationOrderEqualToOverride ? false : true;
            }
        }

        public bool ShowOverrideReferral =>
            string.IsNullOrWhiteSpace(GeneratedReferral) && string.IsNullOrWhiteSpace(OverrideReferral) ? false : true;

        public bool ShowOverrideVisitFrequency => string.IsNullOrWhiteSpace(GeneratedVisitFrequency) &&
                                                  string.IsNullOrWhiteSpace(OverrideVisitFrequency)
            ? false
            : true;

        public bool ShowOverrideGoals =>
            string.IsNullOrWhiteSpace(GeneratedGoals) && string.IsNullOrWhiteSpace(OverrideGoals) ? false : true;

        public bool ShowOverrideLabs =>
            string.IsNullOrWhiteSpace(GeneratedLabs) && string.IsNullOrWhiteSpace(OverrideLabs) ? false : true;

        public bool ShowOverrideInitialServiceOrder => string.IsNullOrWhiteSpace(GeneratedInitialServiceOrder) &&
                                                       string.IsNullOrWhiteSpace(OverrideInitialServiceOrder)
            ? false
            : true;

        public bool ShowOverrideMedications =>
            string.IsNullOrWhiteSpace(GeneratedMedications) && string.IsNullOrWhiteSpace(OverrideMedications)
                ? false
                : true;

        public bool ShowOverrideEquipment =>
            string.IsNullOrWhiteSpace(GeneratedEquipment) && string.IsNullOrWhiteSpace(OverrideEquipment)
                ? false
                : true;

        public bool ShowOverrideSupply =>
            string.IsNullOrWhiteSpace(GeneratedSupply) && string.IsNullOrWhiteSpace(OverrideSupply) ? false : true;

        public bool ShowOverrideOther =>
            string.IsNullOrWhiteSpace(GeneratedOther) && string.IsNullOrWhiteSpace(OverrideOther) ? false : true;

        public bool ShowOverrideRecertificationOrder => string.IsNullOrWhiteSpace(GeneratedRecertificationOrder) &&
                                                        string.IsNullOrWhiteSpace(OverrideRecertificationOrder)
            ? false
            : true;

        public bool CanEditPhysician
        {
            get
            {
                if (OrderStatus == (int)OrderStatusType.Voided)
                {
                    return false; //cannot edit data on a voided order BUG 5525
                }

                var canEdit = CanEditOrderData;

                if (!canEdit
                    && OrderStatus != (int)OrderStatusType.Voided
                   )
                {
                    if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEntryReviewer, false)
                        || RoleAccessHelper.CheckPermission(RoleAccess.Admin, false)
                       )
                    {
                        canEdit = !IsSigningPhysicianVerified;
                    }
                }

                return canEdit;
            }
        }

        public bool CanEditOrderData
        {
            get
            {
                if (OrderStatus == (int)OrderStatusType.Voided)
                {
                    return false; //cannot edit data on a voided order BUG 5525
                }

                if (IsOrderInactive)
                {
                    return false;
                }

                if (CompletedBy == WebContext.Current.User.MemberID &&
                    OrderStatus == (int)OrderStatusType.InProcess)
                {
                    return true;
                }

                if (CompletedBy == WebContext.Current.User.MemberID &&
                    OrderStatus == (int)OrderStatusType.OrderEntryReview)
                {
                    return true;
                }

                if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEntryReviewer, false) &&
                    OrderStatus == (int)OrderStatusType.OrderEntryReview)
                {
                    return true;
                }

                if (Encounter == null)
                {
                    return false;
                }

                if (CompletedBy == WebContext.Current.User.MemberID &&
                    Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed)
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanEditOrderSignature
        {
            get
            {
                if (OrderStatus == (int)OrderStatusType.Voided)
                {
                    return false; //cannot edit data on a voided order BUG 5525
                }

                if (IsOrderInactive)
                {
                    return false;
                }

                if (CompletedBy == WebContext.Current.User.MemberID &&
                    OrderStatus == (int)OrderStatusType.InProcess)
                {
                    return true;
                }

                return false;
            }
        }

        public void RaiseChanged()
        {
            RaisePropertyChanged("IsVoided");
            RaisePropertyChanged("IsSignedOrderEntrySignature");
            RaisePropertyChanged("IsSigned");
            RaisePropertyChanged("IsCoSigned");
            RaisePropertyChanged("CanPrint");
            RaisePropertyChanged("CanEditOrder");
            RaisePropertyChanged("CanEditOrderData");
            RaisePropertyChanged("CanRefreshOrderText");
            RaisePropertyChanged("CanEditOrderSignature");
            RaisePropertyChanged("ShowVOSignature");
            RaisePropertyChanged("CanEditOrderReviewed");
            RaisePropertyChanged("CanEditOrderSigningPhysicianVerified");
            RaisePropertyChanged("CanEditOrdersVoided");
            RaisePropertyChanged("IsVoidedVisible");
            RaisePropertyChanged("IsSigningPhysicianVerifiedVisible");
            RaisePropertyChanged("IsReviewedVisible");

            RaisePropertyChanged("IsGeneratedOrderTextEqualToOrderText");

            RaisePropertyChanged("IsGeneratedReferralEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideReferral");
            RaisePropertyChanged("ShowOverrideReferral");

            RaisePropertyChanged("IsGeneratedVisitFrequencyEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideVisitFrequency");
            RaisePropertyChanged("ShowOverrideVisitFrequency");

            RaisePropertyChanged("IsGeneratedGoalsEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideGoals");
            RaisePropertyChanged("ShowOverrideGoals");

            RaisePropertyChanged("IsGeneratedLabsEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideLabs");
            RaisePropertyChanged("ShowOverrideLabs");

            RaisePropertyChanged("IsGeneratedInitialServiceOrderEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideInitialServiceOrder");
            RaisePropertyChanged("ShowOverrideInitialServiceOrder");

            RaisePropertyChanged("IsGeneratedMedicationsEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideMedications");
            RaisePropertyChanged("ShowOverrideMedications");

            RaisePropertyChanged("IsGeneratedEquipmentEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideEquipment");
            RaisePropertyChanged("ShowOverrideEquipment");

            RaisePropertyChanged("IsGeneratedSupplyEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideSupply");
            RaisePropertyChanged("ShowOverrideSupply");

            RaisePropertyChanged("IsGeneratedOtherEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideOther");
            RaisePropertyChanged("ShowOverrideOther");

            RaisePropertyChanged("IsGeneratedRecertificationOrderEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideRecertificationOrder");
            RaisePropertyChanged("ShowOverrideRecertificationOrder");
        }

        public bool IsOrderInactive
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if (Encounter.Inactive)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsSigned => Signature == null ? false : true;


        int? IOrderEntry.OrderEntryVersion
        {
            get { return 2; }
            set { }
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }

            var pc = new PhoneConverter();
            if (pc == null)
            {
                return null;
            }

            var phoneObject = pc.Convert(phoneNumber, null, null, null);
            if (phoneObject != null)
            {
                if (string.IsNullOrWhiteSpace(phoneObject.ToString()) == false)
                {
                    return phoneObject.ToString();
                }
            }

            return null;
        }
        partial void OnVoidDateChanged()
        {
            IsVoided = VoidDate != null;
        }

        partial void OnReviewDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            IsReviewed = ReviewDate != null;
        }

        partial void OnGeneratedOrderTextChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedOrderTextEqualToOrderText");
            RaisePropertyChanged("CanRefreshOrderText");
        }

        partial void OnOrderTextChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedOrderTextEqualToOrderText");
            RaisePropertyChanged("CanRefreshOrderText");
        }

        partial void OnGeneratedReferralChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedReferralEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideReferral");
            RaisePropertyChanged("ShowOverrideReferral");
        }

        partial void OnOverrideReferralChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedReferralEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideReferral");
            RaisePropertyChanged("ShowOverrideReferral");
        }

        partial void OnGeneratedVisitFrequencyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedVisitFrequencyEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideVisitFrequency");
            RaisePropertyChanged("ShowOverrideVisitFrequency");
        }

        partial void OnOverrideVisitFrequencyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedVisitFrequencyEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideVisitFrequency");
            RaisePropertyChanged("ShowOverrideVisitFrequency");
        }

        partial void OnGeneratedGoalsChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedGoalsEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideGoals");
            RaisePropertyChanged("ShowOverrideGoals");
        }

        partial void OnOverrideGoalsChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedGoalsEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideGoals");
            RaisePropertyChanged("ShowOverrideGoals");
        }

        partial void OnGeneratedLabsChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedLabsEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideLabs");
            RaisePropertyChanged("ShowOverrideLabs");
        }

        partial void OnOverrideLabsChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedLabsEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideLabs");
            RaisePropertyChanged("ShowOverrideLabs");
        }

        partial void OnGeneratedInitialServiceOrderChanged()
        {
            if (IsDeserializing)
            {
                PreviousGeneratedInitialServiceOrder = GeneratedInitialServiceOrder;
                return;
            }

            RaisePropertyChanged("IsGeneratedInitialServiceOrderEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideInitialServiceOrder");
            RaisePropertyChanged("ShowOverrideInitialServiceOrder");
        }

        partial void OnOverrideInitialServiceOrderChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedInitialServiceOrderEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideInitialServiceOrder");
            RaisePropertyChanged("ShowOverrideInitialServiceOrder");
        }

        partial void OnGeneratedMedicationsChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedMedicationsEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideMedications");
            RaisePropertyChanged("ShowOverrideMedications");
        }

        partial void OnOverrideMedicationsChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedMedicationsEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideMedications");
            RaisePropertyChanged("ShowOverrideMedications");
        }

        partial void OnGeneratedEquipmentChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedEquipmentEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideEquipment");
            RaisePropertyChanged("ShowOverrideEquipment");
        }

        partial void OnOverrideEquipmentChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedEquipmentEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideEquipment");
            RaisePropertyChanged("ShowOverrideEquipment");
        }

        partial void OnGeneratedSupplyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedSupplyEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideSupply");
            RaisePropertyChanged("ShowOverrideSupply");
        }

        partial void OnOverrideSupplyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedSupplyEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideSupply");
            RaisePropertyChanged("ShowOverrideSupply");
        }

        partial void OnGeneratedOtherChanged()
        {
            if (IsDeserializing)
            {
                PreviousGeneratedOther = GeneratedOther;
                return;
            }

            RaisePropertyChanged("IsGeneratedOtherEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideOther");
            RaisePropertyChanged("ShowOverrideOther");
        }

        partial void OnOverrideOtherChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedOtherEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideOther");
            RaisePropertyChanged("ShowOverrideOther");
        }

        partial void OnGeneratedRecertificationOrderChanged()
        {
            if (IsDeserializing)
            {
                PreviousGeneratedRecertificationOrder = GeneratedRecertificationOrder;
                return;
            }

            RaisePropertyChanged("IsGeneratedRecertificationOrderEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideRecertificationOrder");
            RaisePropertyChanged("ShowOverrideRecertificationOrder");
        }

        partial void OnOverrideRecertificationOrderChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsGeneratedRecertificationOrderEqualToOverride");
            RaisePropertyChanged("CanRefreshOverrideRecertificationOrder");
            RaisePropertyChanged("ShowOverrideRecertificationOrder");
        }

        partial void OnDiscardFlagChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (DiscardFlag)
            {
                Signature = null;
            }

            if (DiscardFlag == false)
            {
                DiscardReason = null;
            }

            RaisePropertyChanged("ShowVOSignature");
        }
    }
}