#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Core;
using Virtuoso.Core;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Interface;
using Virtuoso.Core.Model;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class Encounter : IDashboardSortable
    {
        private int __cleanupCount;
        private List<string> _changedProperties;
        private int? _disciplineKey;
        private Guid _EncounterID = Guid.NewGuid();
        private bool? _encounterIsAttempted;
        private bool? _encounterIsCOTI;
        private bool? _encounterIsDischarge;
        private bool? _encounterIsEval;
        private bool? _encounterIsHis;
        private bool? _encounterIsHospiceElectionAddendum;
        private bool? _encounterIsOasis;
        private bool? _encounterIsOrderEntry;
        private bool? _encounterIsPreEval;
        private bool? _encounterIsResumption;
        private bool? _encounterIsTeamMeeting;
        private bool? _encounterIsVisit;
        private bool? _encounterIsVisitTeleMonitoring;
        private bool _IsReviewed;

        private int _OriginalEncounterStatus;

        private CollectionViewSource _PatientAddressCollectionView;
        private Guid? _PreviousEncounterCollectedBy;
        private int _PreviousEncounterStatus = (int)EncounterStatusType.Edit;
        private int? _ScheduledServiceEncounterKey;
        private int _ServiceLineKey;

        public bool HasVO
        {
            get
            {
                if (OrderEntryVO == null)
                {
                    return false;
                }

                return OrderEntryVO.Any();
            }
        }

        public override bool IsNew =>
            EntityState ==
            EntityState.New || //For DFVM: will never be 'New'. Encounters are created on the server when Task is created.
            EntityState == EntityState.Detached ||
            OriginalEncounterStatus ==
            (int)EncounterStatusType.None; //Encounters created on the server - EncounterStatus initialized to 0

        public string EncounterStatusDescription
        {
            get
            {
                switch (EncounterStatus)
                {
                    case (int)EncounterStatusType.CoderReview:
                        return "Diagnosis Review";
                    case (int)EncounterStatusType.CoderReviewEdit:
                        return "Post Diagnosis Edit";
                    case (int)EncounterStatusType.OASISReview:
                        return SYS_CDIsHospice ? "HIS Review" : "OASIS Review";
                    case (int)EncounterStatusType.OASISReviewEdit:
                        return SYS_CDIsHospice ? "Post HIS Edit" : "Post OASIS Edit";
                    case (int)EncounterStatusType.OASISReviewEditRR:
                        return SYS_CDIsHospice ? "Post HIS Edit" : "Post OASIS Edit";
                    case (int)EncounterStatusType.Completed:
                        return "Complete";
                    case (int)EncounterStatusType.POCOrderReview:
                        return "Order Review";
                    case (int)EncounterStatusType.CoderReviewEditRR:
                        return "Post Diagnosis Edit";
                    case (int)EncounterStatusType.HISReview:
                        return "HIS Review";
                    case (int)EncounterStatusType.HISReviewEdit:
                        return "Post HIS Edit";
                    case (int)EncounterStatusType.HISReviewEditRR:
                        return "Post HIS Edit";
                    case (int)EncounterStatusType.None:
                    case (int)EncounterStatusType.Edit:
                    default:
                        return "Started";
                }
            }
        }

        public string EncounterStatusDescriptionRichText
        {
            get
            {
                var status = EncounterStatusDescription;
                if (string.IsNullOrWhiteSpace(status))
                {
                    return null;
                }

                if (status != "Started" && status != "Complete")
                {
                    status = "in " + status;
                }

                return string.Format("<Bold><Run Foreground=\"Red\">{0}</Run></Bold>", status);
            }
        }

        public string EncounterStatusDescription2
        {
            get
            {
                switch (EncounterStatus)
                {
                    case (int)EncounterStatusType.CoderReview:
                        return "ICD Review";
                    case (int)EncounterStatusType.OASISReview:
                        return SYS_CDIsHospice ? "HIS Review" : "OASIS Review";
                    case (int)EncounterStatusType.Completed:
                        return "Complete";
                    case (int)EncounterStatusType.POCOrderReview:
                        return "Order Review";
                    case (int)EncounterStatusType.HISReview:
                        return "HIS Review";
                    case (int)EncounterStatusType.CoderReviewEdit:
                    case (int)EncounterStatusType.OASISReviewEdit:
                    case (int)EncounterStatusType.OASISReviewEditRR:
                    case (int)EncounterStatusType.CoderReviewEditRR:
                    case (int)EncounterStatusType.HISReviewEdit:
                    case (int)EncounterStatusType.HISReviewEditRR:
                    case (int)EncounterStatusType.None:
                    case (int)EncounterStatusType.Edit:
                    default:
                        return "Started";
                }
            }
        }

        public string OASISthumbnail
        {
            get
            {
                if (IsEncounterOasisActive == false)
                {
                    return null;
                }

                var eo = MostRecentEncounterOasis;
                if (eo == null)
                {
                    return null;
                }

                return string.Format("\n\t\t\t\t\t - with {0} {1} survey, M0090 of {2}", SYS_CD, eo.RFADescription,
                    eo.M0090 == null ? "???" : ((DateTime)eo.M0090).ToString("MM/dd/yyyy"));
            }
        }

        public bool SYS_CDIsHospice
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SYS_CD))
                {
                    return false;
                }

                return SYS_CD == "HOSPICE" ? true : false;
            }
        }

        public string CertificationPeriod
        {
            get
            {
                if (Admission == null)
                {
                    return "unknown";
                }

                var oe = CurrentOrderEntry;
                if (oe == null)
                {
                    return "unknown";
                }

                var orderDate = oe.CompletedDate == null ? DateTime.Today.Date : oe.CompletedDate.Value.Date;
                var ac = Admission.GetAdmissionCertForDate(orderDate.Date);
                if (ac == null)
                {
                    return "unknown";
                }

                return (ac.PeriodStartDate == null ? "?" : ac.PeriodStartDate.Value.ToString("MM/dd/yyyy")) + " - " +
                       (ac.PeriodEndDate == null ? "?" : ac.PeriodEndDate.Value.ToString("MM/dd/yyyy"));
            }
        }

        public string SYS_CDDescription
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SYS_CD))
                {
                    return null;
                }

                return SYS_CDIsHospice ? "HIS" : SYS_CD;
            }
        }

        public int EncounterStatusMark
        {
            get
            {
                if (SYS_CDIsHospice)
                {
                    if (EncounterStatus == (int)EncounterStatusType.OASISReview)
                    {
                        return (int)EncounterStatusType.HISReview;
                    }

                    if (EncounterStatus == (int)EncounterStatusType.OASISReviewEdit)
                    {
                        return (int)EncounterStatusType.HISReviewEdit;
                    }

                    if (EncounterStatus == (int)EncounterStatusType.OASISReviewEditRR)
                    {
                        return (int)EncounterStatusType.HISReviewEditRR;
                    }
                }

                return EncounterStatus;
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

                var dateTime = Convert.ToDateTime(((DateTimeOffset)ReviewDate).DateTime).ToString("MM/dd/yyyy");
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

                return string.Format("POC Order reviewed on {0}  by {1}", dateTime,
                    UserCache.Current.GetFormalNameFromUserId(ReviewBy));
            }
        }

        public bool IsReviewedVisible
        {
            get
            {
                if (PreviousEncounterStatus == (int)EncounterStatusType.POCOrderReview)
                {
                    return true;
                }

                if (IsReviewed)
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanEditPOCOrderReviewed
        {
            get
            {
                if (Inactive)
                {
                    return false;
                }

                if (IsReviewedVisible == false)
                {
                    return false;
                }

                // can only review in review status - (implies once reviewed - can't go back to unreviewed)
                if (PreviousEncounterStatus != (int)EncounterStatusType.POCOrderReview)
                {
                    return false;
                }

                // if user has OrderEntryReviewer role they can review
                return RoleAccessHelper.CheckPermission(RoleAccess.POCOrderReviewer, false) ? true : false;
            }
        }

        public bool IsCoSigned
        {
            get
            {
                if (EncounterCoSignature == null)
                {
                    return false;
                }

                if (EncounterCoSignature.Any() == false)
                {
                    return false;
                }

                return EncounterCoSignature.First().Signature != null;
            }
        }

        public Guid EncounterID => _EncounterID == Guid.Empty ? _EncounterID = Guid.NewGuid() : _EncounterID;

        public int? EncounterOrTaskServiceTypeKey => ServiceTypeKey != null ? ServiceTypeKey : Task?.ServiceTypeKey;

        public string ServiceTypeDescription
        {
            get
            {
                var stKey = EncounterOrTaskServiceTypeKey;
                if (stKey == null)
                {
                    return "Service Type ?";
                }

                var st = ServiceTypeCache.GetServiceTypeFromKey((int)ServiceTypeKey);
                if (st == null)
                {
                    return "Service Type ?";
                }

                if (st.IsAttempted)
                {
                    return AttemptedServiceTypeDescription;
                }

                return string.IsNullOrWhiteSpace(st.Description) ? "Service Type ?" : st.Description;
            }
        }

        public string ScheduledServiceDescription =>
            string.Format("{0}  on  {1}", ServiceTypeDescription, TaskStartDateAndTimeString);

        public int? ScheduledServiceEncounterKey
        {
            get { return _ScheduledServiceEncounterKey; }
            set
            {
                _ScheduledServiceEncounterKey = value;
                RaisePropertyChanged("ScheduledServiceEncounterKey");
            }
        }

        public string AttemptedServiceTypeDescription
        {
            get
            {
                if (EncounterAttempted == null)
                {
                    return "Attempted Visit";
                }

                var ea = EncounterAttempted.FirstOrDefault();
                if (ea == null)
                {
                    return "Attempted Visit";
                }

                var st = ServiceTypeCache.GetServiceTypeFromKey(ea.ServiceTypeKey);
                if (st == null)
                {
                    return "Attempted Visit";
                }

                return "Attempted " + (string.IsNullOrWhiteSpace(st.Description) ? "Visit" :
                    st.Description.ToLower().Trim() == "attempted" ? "Visit" : st.Description);
            }
        }

        public bool IsAssistant
        {
            get
            {
                // Is assistant in this context means two things.  The service type is an assistant
                // and the user is an assistant.  If the service type is and the user is not, then the 
                // user still has the 'elevated' rights.
                if (ServiceTypeKey == null)
                {
                    return true; // most restrictive
                }

                var st = ServiceTypeCache.GetServiceTypeFromKey((int)ServiceTypeKey);
                if (st == null)
                {
                    return true; // most restrictive
                }

                var up = UserCache.Current.GetCurrentUserProfile();
                if (up == null)
                {
                    return true; // most restrictive
                }

                var diup = up.DisciplineInUserProfile.Where(d => d.DisciplineKey == st.DisciplineKey).FirstOrDefault();
                return st.IsAssistant && (diup == null || diup.IsAssistant);
            }
        }

        public bool CanPerformNPWT
        {
            get
            {
                if (EncounterIsEval == false && EncounterIsVisit == false && EncounterIsResumption == false)
                {
                    return false;
                }

                if (IsSkilledNursingServiceType)
                {
                    return true; // Applicable to RN and LPN
                }

                if ((IsPTServiceType || IsOTServiceType) && IsAssistant == false)
                {
                    return true; // Applicable to OT and PT - but not OTA and PTA 
                }

                return false;
            }
        }

        public bool IsBigFourServiceType
        {
            get
            {
                if (IsSkilledNursingServiceType || IsPTServiceType || IsOTServiceType || IsSLPServiceType)
                {
                    return true;
                }

                return false;
            }
        }

        public string RNorLPN
        {
            get
            {
                if (IsSkilledNursingServiceType == false)
                {
                    return null;
                }

                if (ServiceTypeKey == null)
                {
                    return null;
                }

                var st = ServiceTypeCache.GetServiceTypeFromKey((int)ServiceTypeKey);
                if (st == null)
                {
                    return null;
                }

                return st.IsAssistant ? "LPN" : "RN";
            }
        }

        public string DisciplineCode =>
            DisciplineKey == null ? null : DisciplineCache.GetCodeFromKey((int)DisciplineKey);

        public string HCFACode
        {
            get
            {
                if (ServiceTypeKey == null)
                {
                    return null;
                }

                var rHCFACode = ServiceTypeCache.GetHCFACodeFromKey((int)ServiceTypeKey);
                if (string.IsNullOrWhiteSpace(rHCFACode))
                {
                    return null;
                }

                return rHCFACode;
            }
        }

        public bool IsSkilledNursingServiceType => IsHCFACode("A");

        public bool IsPTServiceType => IsHCFACode("B");

        public bool IsSLPServiceType => IsHCFACode("C");

        public bool IsOTServiceType => IsHCFACode("D");

        public Guid? PreviousEncounterCollectedBy
        {
            get
            {
                if (_PreviousEncounterCollectedBy == null)
                {
                    _PreviousEncounterCollectedBy = EncounterCollectedBy;
                }

                return _PreviousEncounterCollectedBy;
            }
            set
            {
                _PreviousEncounterCollectedBy = value;
                RaisePropertyChanged("PreviousEncounterCollectedBy");
            }
        }

        public string POCPhysicianSignatureAddendumText
        {
            get
            {
                if (EncounterAdmission == null)
                {
                    return null;
                }

                if (CanEditPOCProtectedPhysician == false)
                {
                    return null;
                }

                var ea = EncounterAdmission.FirstOrDefault();
                if (ea == null)
                {
                    return null;
                }

                return ea.AddendumText;
            }
        }

        public bool CanEditPOCProtectedPhysician
        {
            get
            {
                if (PreviousEncounterStatus == (int)EncounterStatusType.POCOrderReview ||
                    PreviousEncounterStatus == (int)EncounterStatusType.Completed)
                {
                    var signed = EncounterPlanOfCare.FirstOrDefault() == null
                        ? false
                        : EncounterPlanOfCare.FirstOrDefault().SignedDate.HasValue;
                    if (EncounterIsPlanOfCare && !signed)
                    {
                        // As a OrderReviewer or sysadmin I need to be able to change the clinician if POC not Voided or Printed 
                        if (RoleAccessHelper.CheckPermission(RoleAccess.Admin, false))
                        {
                            return true;
                        }

                        if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEntryReviewer, false))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public bool ShowGuardOrEscort
        {
            get
            {
                var show = false;

                if (Admission != null)
                {
                    var sl = ServiceLineCache.GetServiceLineFromKey(Admission.ServiceLineKey);

                    if (sl != null
                        && sl.GuardOrEscortOverride
                       )
                    {
                        show = true;
                    }
                }

                return show;
            }
        }

        public bool ShowMethodOfPay
        {
            get
            {
                var show = false;

                var up = UserCache.Current.GetUserProfileFromUserId(EncounterBy);

                if (up != null)
                {
                    var code = CodeLookupCache.GetCodeFromKey(up.MethodOfPayKey);

                    if (!string.IsNullOrEmpty(code)
                        && code.ToUpper() == "SAL"
                       )
                    {
                        if (Admission != null)
                        {
                            var sl = ServiceLineCache.GetServiceLineFromKey(Admission.ServiceLineKey);

                            if (sl != null
                                && sl.PayrollOverride
                               )
                            {
                                show = true;
                            }
                        }
                    }
                }

                return show;
            }
        }

        public bool MethodOfPayProtected =>
            // Method of Pay should only be editable when EncounterStatus is in an edit mode
            EncounterStatus == (int)EncounterStatusType.Edit ||
            EncounterStatus == (int)EncounterStatusType.CoderReviewEdit ||
            EncounterStatus == (int)EncounterStatusType.OASISReviewEdit;

        public bool PreviousEncounterStatusIsInEdit
        {
            get
            {
                if (PreviousEncounterStatus == (int)EncounterStatusType.Edit ||
                    PreviousEncounterStatus == (int)EncounterStatusType.CoderReviewEdit ||
                    PreviousEncounterStatus == (int)EncounterStatusType.OASISReviewEdit ||
                    PreviousEncounterStatus == (int)EncounterStatusType.OASISReviewEditRR)
                {
                    return true;
                }

                return false;
            }
        }

        public int PreviousEncounterStatus
        {
            get
            {
                //  Has to do with client-side properties not persisting across the initial save (subsequent saves are fine) of a new entity - 
                //  On deserialization neither the constructors nor the field initializers are called and a "blank" un-initialized object is used instead.
                //  http://stackoverflow.com/questions/9419743/field-initializer-in-c-sharp-class-not-run-when-deserializing
                //  FormatterServices.GetUninitializedObject() will create an instance without calling a constructor.
                //  http://stackoverflow.com/questions/178645/how-does-wcf-deserialization-instantiate-objects-without-calling-a-constructor

                if (_PreviousEncounterStatus == (int)EncounterStatusType.None)
                {
                    _PreviousEncounterStatus = EncounterStatus; // override to encounterStatus if DNE
                }

                if (_PreviousEncounterStatus == (int)EncounterStatusType.None)
                {
                    _PreviousEncounterStatus =
                        (int)EncounterStatusType.Edit; // override to edit if encounterStatus DNE
                }

                return _PreviousEncounterStatus;
            }
            set
            {
                _PreviousEncounterStatus = value;
                RaisePropertyChanged("PreviousEncounterStatus");
                RaisePropertyChanged("CanEditPOCOrderReviewed");
                RaisePropertyChanged("FullValidation");
            }
        }

        public bool CanEditCompleteCMS => SYS_CDIsHospice ? CanEditCompleteHIS : CanEditCompleteOASIS;

        public bool CanEditCompleteHIS
        {
            get
            {
                if (RoleAccessHelper.CheckPermission(RoleAccess.HISCoordinator, false))
                {
                    return true;
                }

                if (EncounterBy != null && EncounterBy == WebContext.Current.User.MemberID &&
                    RoleAccessHelper.CheckPermission(RoleAccess.HISEntry, false))
                {
                    return true;
                }

                if (EncounterCollectedBy != null && EncounterCollectedBy == WebContext.Current.User.MemberID &&
                    RoleAccessHelper.CheckPermission(RoleAccess.HISEntry, false))
                {
                    return true;
                }

                if (PreviousEncounterCollectedBy != null &&
                    PreviousEncounterCollectedBy == WebContext.Current.User.MemberID &&
                    RoleAccessHelper.CheckPermission(RoleAccess.HISEntry, false))
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanEditCompleteOASIS
        {
            get
            {
                if (RoleAccessHelper.CheckPermission(RoleAccess.OASISCoordinator, false))
                {
                    return true;
                }

                if (EncounterBy != null && EncounterBy == WebContext.Current.User.MemberID &&
                    RoleAccessHelper.CheckPermission(RoleAccess.OASISEntry, false))
                {
                    return true;
                }

                if (EncounterCollectedBy != null && EncounterCollectedBy == WebContext.Current.User.MemberID &&
                    RoleAccessHelper.CheckPermission(RoleAccess.OASISEntry, false))
                {
                    return true;
                }

                if (PreviousEncounterCollectedBy != null &&
                    PreviousEncounterCollectedBy == WebContext.Current.User.MemberID &&
                    RoleAccessHelper.CheckPermission(RoleAccess.OASISEntry, false))
                {
                    return true;
                }

                return false;
            }
        }

        public bool CMSCoordinatorCanEdit => SYS_CDIsHospice ? HISCoordinatorCanEdit : OASISCoordinatorCanEdit;

        public bool HISCoordinatorCanEdit
        {
            get
            {
                if (PreviousEncounterStatus != (int)EncounterStatusType.OASISReview)
                {
                    return false;
                }

                if (RoleAccessHelper.CheckPermission(RoleAccess.HISCoordinator, false) == false)
                {
                    return false;
                }

                if (TenantSettingsCache.Current.TenantSettingHISCoordinatorCanEdit == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool OASISCoordinatorCanEdit
        {
            get
            {
                if (PreviousEncounterStatus != (int)EncounterStatusType.OASISReview)
                {
                    return false;
                }

                if (RoleAccessHelper.CheckPermission(RoleAccess.OASISCoordinator, false) == false)
                {
                    return false;
                }

                if (TenantSettingsCache.Current.TenantSettingOASISCoordinatorCanEdit == false)
                {
                    return false;
                }

                return true;
            }
        }

        public AdmissionDiscipline CurrentAdmissionDiscipline { get; set; }

        public bool IsNTUCEval
        {
            get
            {
                if (EncounterIsEval == false)
                {
                    return false;
                }

                if (CurrentAdmissionDiscipline == null)
                {
                    return false;
                }

                return CurrentAdmissionDiscipline.NotTaken;
            }
        }

        public bool FullValidation
        {
            get
            {
                if (IsNTUCEval)
                {
                    return false;
                }

                if (PreviousEncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return false;
                }

                if (EncounterStatus == (int)EncounterStatusType.CoderReview)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.OASISReview)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.POCOrderReview)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return true;
                }

                return false;
            }
        }

        public bool FullValidationNTUC
        {
            get
            {
                if (PreviousEncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return false;
                }

                if (EncounterStatus == (int)EncounterStatusType.CoderReview)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.OASISReview)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.POCOrderReview)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return true;
                }

                return false;
            }
        }

        public bool FullValidationAndNew
        {
            get
            {
                if (IsNTUCEval)
                {
                    return false;
                }

                if (PreviousEncounterStatus == (int)EncounterStatusType.Edit)
                {
                    return true;
                }

                return FullValidation;
            }
        }

        public bool FullValidationOASIS
        {
            get
            {
                if (EncounterStatus == (int)EncounterStatusType.CoderReview)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.POCOrderReview)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.OASISReview)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsCompleted => EncounterStatus == (int)EncounterStatusType.Completed;

        public bool IsEncounterOasis
        {
            get
            {
                if (EncounterOasis == null)
                {
                    return false;
                }

                var eo = EncounterOasis.OrderBy(e => e.AddedDate).FirstOrDefault();
                if (eo == null)
                {
                    return false;
                }

                if (eo.BypassFlag == null)
                {
                    return false;
                }

                if (eo.BypassFlag == true)
                {
                    return false;
                }

                return true;
            }
        }

        private bool ServiceTypeCoderReviewWithoutOasis
        {
            get
            {
                if (ServiceTypeKey == null)
                {
                    return false;
                }

                var st = ServiceTypeCache.GetServiceTypeFromKey((int)ServiceTypeKey);
                if (st == null)
                {
                    return false;
                }

                return st.CoderReviewWithoutOasis;
            }
        }

        public bool IsEncounterOasisRequireICDCoderReviewOrServiceTypeOverride
        {
            get
            {
                if (ServiceTypeCoderReviewWithoutOasis)
                {
                    return true;
                }

                if (IsEncounterOasis == false)
                {
                    return false;
                }

                if (SYS_CDIsHospice)
                {
                    return false;
                }

                var eo = EncounterOasis.OrderBy(e => e.AddedDate).FirstOrDefault();
                if (eo == null)
                {
                    return false;
                }

                if ((eo.RFA == "01" || eo.RFA == "03" || eo.RFA == "04" || eo.RFA == "05") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsEncounterOasisBypass
        {
            get
            {
                if (EncounterOasis == null)
                {
                    return false;
                }

                var eo = EncounterOasis.OrderBy(e => e.AddedDate).FirstOrDefault();
                if (eo == null)
                {
                    return false;
                }

                if (eo.BypassFlag == null)
                {
                    return false;
                }

                if (eo.BypassFlag == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsUnfulfilledOtherFollowUpOasis
        {
            get
            {
                if (EncounterIsOasis)
                {
                    return false;
                }

                if (EncounterOasis == null)
                {
                    return false;
                }

                var eo = EncounterOasis.OrderBy(e => e.AddedDate).FirstOrDefault();
                if (eo == null)
                {
                    return false;
                }

                if (eo.RFA == "05" && IsEncounterOasisBypass)
                {
                    return true;
                }

                return false;
            }
        }

        public string OasisBlirb
        {
            get
            {
                if (EncounterOasis == null)
                {
                    return null;
                }

                if (IsUnfulfilledOtherFollowUpOasis)
                {
                    return null;
                }

                return IsEncounterOasis ? SYS_CDDescription :
                    IsEncounterOasisBypass ? SYS_CDDescription + "-BYPASS" : null;
            }
        }

        public string EncounterSuffix
        {
            get
            {
                if (IsEncounterOasisActive)
                {
                    return EncounterOasisRFADescription;
                }

                if (OrderEntry == null)
                {
                    return null;
                }

                if (EncounterIsOrderEntry == false)
                {
                    return null;
                }

                var oe = OrderEntry.FirstOrDefault(p => p.HistoryKey == null);
                return oe?.CompletedOnText;
            }
        }

        public string EncounterOasisRFADescription
        {
            get
            {
                if (EncounterOasis == null)
                {
                    return null;
                }

                var eo = EncounterOasis.OrderBy(e => e.AddedDate).FirstOrDefault();
                if (eo == null)
                {
                    return null;
                }

                if (eo.BypassFlag == null)
                {
                    return null;
                }

                if (eo.BypassFlag == true)
                {
                    return null;
                }

                return eo.RFADescription;
            }
        }

        public bool IsEncounterOasisActive
        {
            get
            {
                if (EncounterOasis == null)
                {
                    return false;
                }

                var eo = EncounterOasis.OrderByDescending(e => e.AddedDate).FirstOrDefault();
                if (eo == null)
                {
                    return false;
                }

                if (eo.BypassFlag == null)
                {
                    return false;
                }

                if (eo.BypassFlag == true)
                {
                    return false;
                }

                if (eo.InactiveDate != null)
                {
                    return false;
                }

                if (eo.REC_ID == "X1")
                {
                    return false;
                }

                return true;
            }
        }

        public EncounterOasis MostRecentEncounterOasis
        {
            get
            {
                if (IsEncounterOasis == false)
                {
                    return null;
                }

                if (EncounterOasis == null)
                {
                    return null;
                }

                var eo = EncounterOasis.Where(e => e.BypassFlag == false)
                    .OrderByDescending(e => e.AddedDate)
                    .ThenBy(e => e.REC_ID)
                    .FirstOrDefault();
                return eo;
            }
        }

        public EncounterOasis EncounterOasisWithHIPPS
        {
            get
            {
                if (Inactive)
                {
                    return null;
                }

                if (IsEncounterOasisActive == false)
                {
                    return null;
                }

                if (EncounterOasis == null)
                {
                    return null;
                }

                var eo = MostRecentEncounterOasis;
                if (eo == null)
                {
                    return null;
                }

                if (eo.M0090 == null || eo.HIPPSCode == null || eo.SYS_CD != "OASIS" || eo.REC_ID != "B1")
                {
                    return null;
                }

                return eo;
            }
        }

        public EncounterOasis EncounterOasisScoreable
        {
            get
            {
                if (Inactive)
                {
                    return null;
                }

                if (IsEncounterOasisActive == false)
                {
                    return null;
                }

                if (EncounterOasis == null)
                {
                    return null;
                }

                var eo = MostRecentEncounterOasis;
                if (eo.M0090 == null || eo.SYS_CD != "OASIS" || eo.REC_ID != "B1")
                {
                    return null;
                }

                return eo;
            }
        }

        public bool AnyEncounterOasisCMSTransmitted
        {
            get
            {
                if (IsEncounterOasis == false)
                {
                    return false;
                }

                if (EncounterOasis == null)
                {
                    return false;
                }

                var eo = EncounterOasis.FirstOrDefault(e => e.CMSTransmission);
                return eo != null;
            }
        }

        public List<EncounterReview> EncounterReviewList
        {
            get
            {
                if (EncounterReview == null)
                {
                    return null;
                }

                if (EncounterStatus == (int)EncounterStatusType.Edit)
                {
                    return null;
                }

                if (PreviousEncounterStatus == (int)EncounterStatusType.Edit)
                {
                    return null;
                }

                return EncounterReview.Where(er => er.IsNew == false && er.SectionLabel == null)
                    .OrderBy(er => er.ReviewUTCDateTime).ToList();
            }
        }

        public bool AreEncounterReviews
        {
            get
            {
                var erl = EncounterReviewList;
                return erl == null ? false : erl.Any() == false ? false : true;
            }
        }

        public bool AreEncounterReviewSectionNotes
        {
            get
            {
                var erlsn = EncounterReview.Where(er => er.SectionLabel != null).OrderBy(er => er.ReviewUTCDateTime)
                    .ToList();
                return erlsn == null ? false : erlsn.Any() == false ? false : true;
            }
        }

        public bool AreDummyICDsPresent => false;

        public bool UsingDiagnosisCoders
        {
            get
            {
                // if already in DiagnosisCoders mode - return true
                if (EncounterStatus == (int)EncounterStatusType.CoderReview)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.CoderReviewEdit)
                {
                    return true;
                }

                // if user is an ICDCoder we are not using them (they can police themselves)
                if (SYS_CDIsHospice)
                {
                    return false; // no review during HIS surveys
                }

                if (RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false))
                {
                    return false;
                }

                // Check if user, serviceLineGrouping, or tenant is using DiagnosisCoders
                var up = UserCache.Current.GetUserProfileFromUserId(EncounterBy);
                if (up != null)
                {
                    if (up.UsingDiagnosisCoders)
                    {
                        return true;
                    }
                }

                if (Admission != null)
                {
                    if (Admission.UsingDiagnosisCodersAtServiceLineGroupings(null))
                    {
                        return true;
                    }
                }

                if (TenantSettingsCache.Current.TenantSetting.UsingDiagnosisCoders)
                {
                    return true;
                }

                return false;
            }
        }

        public bool UsingCMSCoordinator => SYS_CDIsHospice ? UsingHISCoordinator : UsingOASISCoordinator;

        public bool UsingHISCoordinator
        {
            get
            {
                // if already in HISCoordinator mode - return true
                if (EncounterStatus == (int)EncounterStatusType.OASISReview)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.OASISReviewEdit)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.OASISReviewEditRR)
                {
                    return true;
                }

                // if user is an OASISCoordinator we are not using them (they can police themselves)
                if (RoleAccessHelper.CheckPermission(RoleAccess.HISCoordinator, false))
                {
                    if (PreviousEncounterStatus != (int)EncounterStatusType.CoderReview)
                    {
                        return false; // unless they are currently wearing a coder review hat                   
                    }
                }

                // if Most Recent survey is inactive or mark not for transmit - we're not using Coordinators
                if (MostRecentEncounterOasis != null)
                {
                    if (MostRecentEncounterOasis.InactiveDate != null)
                    {
                        return false;
                    }

                    if (MostRecentEncounterOasis.REC_ID == "X1")
                    {
                        return false;
                    }
                }

                // Check if user, serviceLineGrouping, or tenant is using HISCoordinator
                var up = UserCache.Current.GetUserProfileFromUserId(EncounterBy);
                if (up != null)
                {
                    if (up.UsingHISCoordinator)
                    {
                        return true;
                    }
                }

                if (Admission != null)
                {
                    if (Admission.UsingHISCoordinatorAtServiceLineGroupings(null))
                    {
                        return true;
                    }
                }

                if (TenantSettingsCache.Current.TenantSetting.UsingHISCoordinator)
                {
                    return true;
                }

                return false;
            }
        }

        public bool UsingOASISCoordinator
        {
            get
            {
                // if already in OASISCoordinator mode - return true
                if (EncounterStatus == (int)EncounterStatusType.OASISReview)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.OASISReviewEdit)
                {
                    return true;
                }

                if (EncounterStatus == (int)EncounterStatusType.OASISReviewEditRR)
                {
                    return true;
                }

                // if user is an OASISCoordinator we are not using them (they can police themselves)
                if (RoleAccessHelper.CheckPermission(RoleAccess.OASISCoordinator, false))
                {
                    if (PreviousEncounterStatus != (int)EncounterStatusType.CoderReview)
                    {
                        return false; // unless they are currently wearing a coder review hat                   
                    }
                }

                // if Most Recent survey is inactive or mark not for transmit - we're not using Coordinators
                if (MostRecentEncounterOasis != null)
                {
                    if (MostRecentEncounterOasis.InactiveDate != null)
                    {
                        return false;
                    }

                    if (MostRecentEncounterOasis.REC_ID == "X1")
                    {
                        return false;
                    }
                }

                // Check if user, serviceLineGrouping, or tenant is using OASISCoordinator
                var up = UserCache.Current.GetUserProfileFromUserId(EncounterBy);
                if (up != null)
                {
                    if (up.UsingOASISCoordinator)
                    {
                        return true;
                    }
                }

                if (Admission != null)
                {
                    if (Admission.UsingOASISCoordinatorAtServiceLineGroupings(null))
                    {
                        return true;
                    }
                }

                if (TenantSettingsCache.Current.TenantSetting.UsingOASISCoordinator)
                {
                    return true;
                }

                return false;
            }
        }

        public bool UsingPOCOrderReviewers
        {
            get
            {
                // if already in POCOrderReviewer mode - return true
                if (PreviousEncounterStatus == (int)EncounterStatusType.POCOrderReview)
                {
                    return true;
                }

                // if user is a POCOrderReviewer we are not using them (they can police themselves)
                if (RoleAccessHelper.CheckPermission(RoleAccess.POCOrderReviewer, false))
                {
                    return false;
                }

                // Check if user, serviceLineGrouping, or tenant is using POCOrderReviewer
                var up = UserCache.Current.GetUserProfileFromUserId(EncounterBy);
                if (up != null)
                {
                    if (up.UsingPOCOrderReviewers)
                    {
                        return true;
                    }
                }

                if (Admission != null)
                {
                    if (Admission.UsingPOCOrderReviewersAtServiceLineGroupings(null))
                    {
                        return true;
                    }
                }

                if (TenantSettingsCache.Current.TenantSetting.UsingPOCOrderReviewers)
                {
                    return true;
                }

                return false;
            }
        }

        public UserProfile EncounterByUserProfile => UserCache.Current.GetUserProfileFromUserId(EncounterBy);

        public bool EncounterByIsHospiceMedicalDirector =>
            UserCache.Current.UserIdIsHospiceMedicalDirector(EncounterBy);

        public bool EncounterByIsHospicePhysician => UserCache.Current.UserIdIsHospicePhysician(EncounterBy);

        public bool EncounterByIsHospiceNursePractitioner =>
            UserCache.Current.UserIdIsHospiceNursePractitioner(EncounterBy);

        public bool UserIsPOCOrderReviewerAndInPOCOrderReview
        {
            get
            {
                // if not in POCOrderReviewer mode - return true
                if (PreviousEncounterStatus != (int)EncounterStatusType.POCOrderReview)
                {
                    return false;
                }

                // if user is a POCOrderReviewer return true
                if (RoleAccessHelper.CheckPermission(RoleAccess.POCOrderReviewer, false))
                {
                    return true;
                }

                return false;
            }
        }

        public DateTime TimeOfEncounterStartDateChanged { get; set; }
        public DateTime TimeOfEncounterStartTimeChanged { get; set; }
        public DateTime TimeOfEncounterEndDateChanged { get; set; }
        public DateTime TimeOfEncounterEndTimeChanged { get; set; }
        public DateTime TimeOfEncounterPatientAddressKeyChanged { get; set; }
        public DateTime TimeOfEncounterActualTimeChanged { get; set; }

        public string EncounterStartDateAndTimeText => DateTimeOffsetText(EncounterStartDateAndTime);

        public string EncounterEndDateAndTimeText => DateTimeOffsetText(EncounterEndDateAndTime);

        public AdmissionCertification EncounterCertCycle
        {
            get
            {
                AdmissionCertification ac = null;

                DateTime? startDate = null;

                if (Admission != null)
                {
                    if (EncounterOrTaskStartDateAndTime.HasValue)
                    {
                        startDate = EncounterOrTaskStartDateAndTime.Value.Date;
                    }

                    if (EncounterIsInRecertWindow && startDate.HasValue)
                    {
                        if (EncounterIsPlanOfCare)
                        {
                            if (TenantSettingsCache.Current != null)
                            {
                                double recertWindow = TenantSettingsCache.Current.DisciplineRecertWindowWithDefault;
                                if (Admission.ServiceLineKey > 0)
                                {
                                    var serviceLine = ServiceLineCache.GetServiceLineFromKey(Admission.ServiceLineKey);
                                    var serviceLineDisciplineRecertWindow =
                                        serviceLine.DisciplineRecertWindow.GetValueOrDefault();
                                    if (serviceLineDisciplineRecertWindow > 0)
                                    {
                                        recertWindow = serviceLineDisciplineRecertWindow;
                                    }
                                }

                                startDate = startDate.Value.AddDays(recertWindow);
                            }
                        }
                    }

                    if (startDate.HasValue)
                    {
                        ac = Admission.GetAdmissionCertForDate(startDate.Value);
                    }
                }

                return ac;
            }
        }

        public int? EncounterCyclePdNum
        {
            get
            {
                if (EncounterCertCycle != null)
                {
                    return EncounterCertCycle.PeriodNumber;
                }

                return Admission == null ? null : Admission.StartPeriodNumber;
            }
            set
            {
                if (EncounterCertCycle != null && value != null)
                {
                    EncounterCertCycle.PeriodNumber = (int)value;
                }

                if (Admission != null && value != null)
                {
                    Admission.CurrentCertPeriodNumber = value;
                    CertManager.CalcDatesForPdNum(Admission, EncounterCertCycle);
                    if (Admission.FirstCert != null && Admission.FirstCert == EncounterCertCycle)
                    {
                        Admission.StartPeriodNumber = value;
                    }

                    if (Admission.FirstCert == null || EncounterCertCycle == null)
                    {
                        Admission.StartPeriodNumber = value;
                    }
                }

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    RaisePropertyChanged("EncounterCycleStartDate");
                    RaisePropertyChanged("EncounterCycleEndDate");
                });
            }
        }

        public DateTime? EncounterCycleStartDate
        {
            get
            {
                if (EncounterCertCycle != null)
                {
                    return EncounterCertCycle.PeriodStartDate;
                }

                return null;
            }
            set
            {
                if (value != null)
                {
                    if (EncounterCertCycle != null)
                    {
                        EncounterCertCycle.PeriodStartDate = value;
                        if (EncounterCertCycle.PeriodStartDate.HasValue)
                        {
                            EncounterCertCycle.SetPeriodEndDateForRow();
                        }
                    }

                    RaisePropertyChanged("EncounterCycleEndDate");
                }
            }
        }

        public DateTime? EncounterCycleEndDate
        {
            get
            {
                if (EncounterCertCycle != null)
                {
                    return EncounterCertCycle.PeriodEndDate;
                }

                return null;
            }
        }

        public DateTime? EncounterResumptionDate
        {
            get
            {
                if (EncounterResumption == null)
                {
                    return null;
                }

                var er = EncounterResumption.FirstOrDefault();
                if (er == null)
                {
                    return null;
                }

                return er.ResumptionDate;
            }
        }

        public bool EncounterIsQualifiedDiscipline
        {
            get
            {
                if (IsSkilledNursingServiceType)
                {
                    return true;
                }

                if (IsPTServiceType)
                {
                    return true;
                }

                if (IsSLPServiceType)
                {
                    return true;
                }

                if (IsOTServiceType)
                {
                    return true;
                }

                return false;
            }
        }

        public bool EncounterIsOasis
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsOasis == null)
                {
                    _encounterIsOasis = DynamicFormCache.IsOasis(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsOasis == null ? false : (bool)_encounterIsOasis;
            }
        }

        public bool EncounterIsHis
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsHis == null)
                {
                    _encounterIsHis = DynamicFormCache.IsHIS(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsHis == null ? false : (bool)_encounterIsHis;
            }
        }

        public bool EncounterIsDischarge
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsDischarge == null)
                {
                    _encounterIsDischarge = DynamicFormCache.IsDischarge(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsDischarge == null ? false : (bool)_encounterIsDischarge;
            }
        }

        public bool EncounterIsAttempted
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsAttempted == null)
                {
                    _encounterIsAttempted = DynamicFormCache.IsAttempted(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsAttempted == null ? false : (bool)_encounterIsAttempted;
            }
        }

        public bool EncounterIsOrderEntry
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsOrderEntry == null)
                {
                    _encounterIsOrderEntry = DynamicFormCache.IsOrderEntry(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsOrderEntry == null ? false : (bool)_encounterIsOrderEntry;
            }
        }

        public bool EncounterIsHospiceElectionAddendum
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsHospiceElectionAddendum == null)
                {
                    _encounterIsHospiceElectionAddendum = DynamicFormCache.IsHospiceElectionAddendum(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsHospiceElectionAddendum == null ? false : (bool)_encounterIsHospiceElectionAddendum;
            }
        }

        public bool EncounterIsVisitTeleMonitoring
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsVisitTeleMonitoring == null)
                {
                    _encounterIsVisitTeleMonitoring = DynamicFormCache.IsVisitTeleMonitoring(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsVisitTeleMonitoring == null ? false : (bool)_encounterIsVisitTeleMonitoring;
            }
        }

        public OrderEntry CurrentOrderEntry
        {
            get
            {
                if (OrderEntry == null)
                {
                    return null;
                }

                return OrderEntry.Where(p => p.HistoryKey == null).FirstOrDefault();
            }
        }

        public EncounterPlanOfCare MyEncounterPlanOfCare
        {
            get
            {
                if (EncounterPlanOfCare == null)
                {
                    return null;
                }

                return EncounterPlanOfCare.FirstOrDefault();
            }
        }

        public bool EncounterIsPlanOfCare
        {
            get
            {
                if (EncounterPlanOfCare == null)
                {
                    return false;
                }

                return EncounterPlanOfCare.FirstOrDefault() == null ? false : true;
            }
        }

        public bool EncounterIsPreEval
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsPreEval == null)
                {
                    _encounterIsPreEval = DynamicFormCache.IsPreEval(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsPreEval == null ? false : (bool)_encounterIsPreEval;
            }
        }

        public bool EncounterIsCOTI
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsCOTI == null)
                {
                    _encounterIsCOTI = DynamicFormCache.IsCOTI(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsCOTI ?? false;
            }
        }

        public bool EncounterIsEval
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsEval == null)
                {
                    _encounterIsEval = DynamicFormCache.IsEval(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsEval == null ? false : (bool)_encounterIsEval;
            }
        }

        public bool EncounterIsVisit
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsVisit == null)
                {
                    _encounterIsVisit = DynamicFormCache.IsVisit(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsVisit == null ? false : (bool)_encounterIsVisit;
            }
        }

        public bool EncounterIsResumption
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsResumption == null)
                {
                    _encounterIsResumption = DynamicFormCache.IsResumption(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsResumption == null ? false : (bool)_encounterIsResumption;
            }
        }

        public bool EncounterIsInEdit
        {
            get
            {
                var _status = ((EncounterStatusType)EncounterStatus).ToString();
                if (_status.Equals("None", StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }

                if (Enum.GetNames(typeof(EncounterStatusType)).Where(e =>
                        e.Contains("Edit") && e.Equals(_status, StringComparison.InvariantCultureIgnoreCase))
                    .Any())
                {
                    return true;
                }

                return false;
            }
        }

        public bool EncounterIsTeamMeeting
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (_encounterIsTeamMeeting == null)
                {
                    _encounterIsTeamMeeting = DynamicFormCache.IsTeamMeeting(FormKey == null
                        ? (int)ServiceTypeCache.GetFormKey((int)ServiceTypeKey)
                        : (int)FormKey);
                }

                return _encounterIsTeamMeeting == null ? false : (bool)_encounterIsTeamMeeting;
            }
        }

        public bool PrintAsSSRS
        {
            get
            {
                if (FormKey == null)
                {
                    return false;
                }

                return DynamicFormCache.PrintAsSSRS((int)FormKey);
            }
        }

        public bool IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry =>
            EncounterIsPlanOfCare || EncounterIsTeamMeeting || EncounterIsOrderEntry;

        public bool IsPlanOfCareOrIsTeamMeetingOrIsOrderEntryOrIsAttempted => EncounterIsPlanOfCare ||
                                                                              EncounterIsTeamMeeting ||
                                                                              EncounterIsOrderEntry ||
                                                                              EncounterIsAttempted;

        public bool EncounterIsTransfer
        {
            get
            {
                if (EncounterTransfer == null)
                {
                    return false;
                }

                return EncounterTransfer.FirstOrDefault() == null ? false : true;
            }
        }

        public bool EncounterIsInRecertWindow
        {
            get
            {
                if (Admission == null)
                {
                    return false;
                }

                return GetIsEncounterInRecertWindow(Admission, this);
            }
        }

        public string EncounterOasisRFA
        {
            get
            {
                if (EncounterOasis == null)
                {
                    return null;
                }

                if (EncounterOasis.Any() == false)
                {
                    return null;
                }

                var eo = EncounterOasis.Where(e => e.InactiveDate == null && e.REC_ID != "X1")
                    .OrderByDescending(e => e.AddedDate).FirstOrDefault();
                if (eo == null)
                {
                    return null;
                }

                if (eo.BypassFlag == null)
                {
                    return null;
                }

                if (eo.BypassFlag == true)
                {
                    return null;
                }

                return eo.RFA;
            }
        }

        public DateTime? EncounterOasisM0090
        {
            get
            {
                if (EncounterOasis == null)
                {
                    return null;
                }

                var eo = EncounterOasis.Where(e => e.InactiveDate == null && e.REC_ID != "X1")
                    .OrderByDescending(e => e.AddedDate).FirstOrDefault();
                if (eo == null)
                {
                    return null;
                }

                return eo.M0090;
            }
        }

        public DateTime? EncounterOasisFirstAddedDate
        {
            get
            {
                if (EncounterOasis == null)
                {
                    return null;
                }

                var eo = EncounterOasis.Where(e => e.InactiveDate == null && e.REC_ID != "X1").OrderBy(e => e.AddedDate)
                    .FirstOrDefault();
                if (eo == null)
                {
                    return null;
                }

                return eo.AddedDate;
            }
        }

        public string EncounterOasisAlertDescription
        {
            get
            {
                if (EncounterOasisRFA != "06" && EncounterOasisRFA != "07" && EncounterOasisRFA != "08" &&
                    EncounterOasisRFA != "09")
                {
                    return "OASIS Alerts are not active for this encounter";
                }

                var eoa = GetFirstEncounterOasisAlert();
                if (eoa == null)
                {
                    return "No OASIS Alerts detected for this encounter";
                }

                var startEncounter = Admission.Encounter.Where(e => e.EncounterKey == eoa.EncounterStartKey)
                    .FirstOrDefault();
                return string.Format("Beginning OASIS assessment - RFA {0} completed on {1} {2}",
                    startEncounter.EncounterOasisRFA,
                    ((DateTime)startEncounter.EncounterOasisM0090).ToString("MM/dd/yyyy"),
                    eoa.ST_Episode ? "(with Follow-up survey since)" : "");
            }
        }

        public string EncounterFormDescription
        {
            get
            {
                if (Form == null)
                {
                    return "Unknown Form";
                }

                return Form.Description;
            }
        }

        public string ActivationHistoryMostRecentText
        {
            get
            {
                var ea = EncounterActivationHistory == null
                    ? null
                    : EncounterActivationHistory.Where(e => e.EncounterActivationHistoryKey > 0)
                        .OrderByDescending(e => e.ActivationDateTime).FirstOrDefault();
                if (ea == null)
                {
                    return null;
                }

                return ea == null ? null : ea.ActivationHistoryText.Replace("\r", "  ");
            }
        }

        public string ActivationHistoryParagraphText
        {
            get
            {
                var eaList = EncounterActivationHistory == null
                    ? null
                    : EncounterActivationHistory.Where(e => e.EncounterActivationHistoryKey > 0)
                        .OrderByDescending(e => e.ActivationDateTime).ToList();
                if (eaList == null || eaList.Any() == false)
                {
                    return "No activation history";
                }

                string activationHistory = null;
                foreach (var ea in eaList)
                {
                    var text = ea.ActivationHistoryText;

                    activationHistory = activationHistory +
                                        (string.IsNullOrWhiteSpace(activationHistory) == false ? "\r\r" : "") + text;
                }

                return activationHistory == null ? null : activationHistory.Replace("\r", "<LineBreak />");
            }
        }

        public string ActivationHistoryParagraphTextMoreThanOne
        {
            get
            {
                var eaList = EncounterActivationHistory == null
                    ? null
                    : EncounterActivationHistory.Where(e => e.EncounterActivationHistoryKey > 0)
                        .OrderByDescending(e => e.ActivationDateTime).ToList();
                if (eaList == null || eaList.Count < 2)
                {
                    return null;
                }

                string activationHistory = null;
                foreach (var ea in eaList)
                {
                    var text = ea.ActivationHistoryText;

                    activationHistory = activationHistory +
                                        (string.IsNullOrWhiteSpace(activationHistory) == false ? "\r\r" : "") + text;
                }

                return activationHistory == null ? null : activationHistory.Replace("\r", "<LineBreak />");
            }
        }

        public string ActivationHistoryParagraphTextOneOrMore
        {
            get
            {
                var eaList = EncounterActivationHistory == null
                    ? null
                    : EncounterActivationHistory.Where(e => e.EncounterActivationHistoryKey > 0)
                        .OrderByDescending(e => e.ActivationDateTime).ToList();
                if (eaList == null || eaList.Any() == false)
                {
                    return null;
                }

                string activationHistory = null;
                foreach (var ea in eaList)
                {
                    var text = ea.ActivationHistoryText;

                    activationHistory = activationHistory +
                                        (string.IsNullOrWhiteSpace(activationHistory) == false ? "\r\r" : "") + text;
                }

                return activationHistory == null ? null : activationHistory.Replace("\r", "<LineBreak />");
            }
        }

        public bool CanInactivate
        {
            get
            {
                if ((EncounterStatus == (int)EncounterStatusType.Edit ||
                     EncounterStatus == (int)EncounterStatusType.CoderReview ||
                     EncounterStatus == (int)EncounterStatusType.CoderReviewEdit) == false)
                {
                    return false;
                }

                var isInRole = RoleAccessHelper.CheckPermission("Admin");
                return isInRole ? true : false;
            }
        }

        public string AddendumHistoryParagraphText
        {
            get
            {
                if (EncounterAddendum == null)
                {
                    return "No addendum history.";
                }

                var eaList = EncounterAddendum.Where(e => e.EncounterAddendumKey > 0)
                    .OrderByDescending(e => e.UpdatedDate).ToList();
                if (eaList == null || eaList.Any() == false)
                {
                    return "No addendum history.";
                }

                string addendumHistory = null;
                var useMilitaryTime = TenantSettingsCache.Current.TenantSetting.UseMilitaryTime;
                foreach (var ea in eaList)
                {
                    var HaveUser = ea.UpdatedBy != Guid.Empty;
                    var userData = XamlHelper.EncodeAsXaml(ea.AddendumText);
                    addendumHistory = addendumHistory +
                                      string.Format("<Bold>By {0}, on {1} {2}</Bold>   {3}<LineBreak/>",
                                          HaveUser
                                              ? ea.UpdatedBy == null ? "Unknown" :
                                              UserCache.Current.GetFormalNameFromUserId(ea.UpdatedBy)
                                              : "Unknown",
                                          ea.UpdatedDate.ToLocalTime().ToString("MM/dd/yyyy"),
                                          useMilitaryTime
                                              ? ea.UpdatedDate.ToLocalTime().ToString("HHmm")
                                              : ea.UpdatedDate.ToLocalTime().ToShortTimeString(),
                                          userData);
                }

                return addendumHistory;
            }
        }

        public string NarrativeHistoryParagraphText
        {
            get
            {
                if (EncounterNarrative == null)
                {
                    return "No narrative history.";
                }

                var enList = EncounterNarrative.Where(e => e.NarrativeText != null)
                    .OrderByDescending(e => e.NarrativeDateTime).ToList();
                if (enList == null || enList.Any() == false)
                {
                    return "No narrative history.";
                }

                string narrativeHistory = null;
                var useMilitaryTime = TenantSettingsCache.Current.TenantSetting.UseMilitaryTime;
                foreach (var en in enList)
                {
                    var userData = XamlHelper.EncodeAsXaml(en.NarrativeText);
                    narrativeHistory = narrativeHistory +
                                       string.Format(
                                           "<Bold>By {0}, on {1} {2}</Bold><LineBreak/>   {3}<LineBreak/><LineBreak/>",
                                           en.NarrativeBy == null || en.NarrativeBy == Guid.Empty
                                               ? "Unknown"
                                               : UserCache.Current.GetFormalNameFromUserId(en.NarrativeBy),
                                           en.NarrativeDateTime.ToString("MM/dd/yyyy"),
                                           useMilitaryTime
                                               ? en.NarrativeDateTime.ToString("HHmm")
                                               : en.NarrativeDateTime.ToShortTimeString(),
                                           userData);
                }

                return narrativeHistory;
            }
        }

        public int ServiceLineKey
        {
            get { return _ServiceLineKey; }
            set
            {
                _ServiceLineKey = value;
                RaisePropertyChanged("ServiceLineKey");
            }
        }

        public int? DisciplineKey
        {
            get
            {
                // Only look this information up in the cache if it hasn't been done already.
                // there is a performance hit for repetitive linq queries against entitysets that constantly go after the cache.
                // Look it up once and store it in the private field.
                if (ServiceTypeKey == null)
                {
                    return null;
                }

                if (_disciplineKey == null)
                {
                    _disciplineKey = ServiceTypeCache.GetDisciplineKey((int)ServiceTypeKey);
                }

                return _disciplineKey;
            }
        }

        public double BodySurfaceArea { get; set; }

        public float? WeightKG { get; set; }

        public EntityCollection<PatientAddress> PatientAddressCollectionSource =>
            (EntityCollection<PatientAddress>)_PatientAddressCollectionView.Source;

        public ICollectionView PatientAddressCollectionView => _PatientAddressCollectionView?.View;

        public DateTime? CreatedDate
        {
            get
            {
                DateTime? createdDate = null;
                // As we use the Created date in AdmissionDocumentation (and to place the necounter within a CertCycle -
                // try to use the date of the action first (the M0090Date, Transfer Date...) before trying to use the encounter (visit stats) date
                //createdDate = (EncounterStartDateAndTime == null) ? (DateTime?)null : EncounterStartDateAndTime.Value.DateTime;
                // Physician Face To Face   ???
                if (createdDate == null && FormKey != null)
                {
                    if (DynamicFormCache.IsDischarge((int)FormKey))
                    {
                        // Discharge - EncounterAdmission.DischargeDateTime
                        var ea = EncounterAdmission.FirstOrDefault();
                        if (ea != null)
                        {
                            createdDate = ea.DischargeDateTime;
                        }
                    }
                    else if (DynamicFormCache.IsOasis((int)FormKey) || DynamicFormCache.IsHIS((int)FormKey))
                    {
                        // HIS Admission - EncounterOasis.M0090
                        // HIS Discharge - EncounterOasis.M0090
                        // OASIS         - EncounterOasis.M0090
                        if (IsEncounterOasisActive && MostRecentEncounterOasis != null)
                        {
                            createdDate = MostRecentEncounterOasis.M0090;
                        }
                    }
                    else if (EncounterIsTeamMeeting)
                    {
                        // Hospice Team Meeting - EncounterTeamMeeting.AdmissionTeamMeeting.LastTeamMeetingDate
                        var et = EncounterTeamMeeting.FirstOrDefault();
                        if (et != null && et.AdmissionTeamMeeting != null)
                        {
                            createdDate = et.AdmissionTeamMeeting.LastTeamMeetingDate;
                        }
                    }
                    else if (EncounterIsOrderEntry)
                    {
                        // Order Entry - OrderEntry.CompletedDate
                        var oe = CurrentOrderEntry;
                        if (oe != null && oe.CompletedDate != null)
                        {
                            createdDate = oe.CompletedDate.Value.Date;
                        }
                    }
                    else if (EncounterIsPlanOfCare)
                    {
                        // Plan of Care - EncounterPlanOfCare.CertificationFromDate
                        var ep = EncounterPlanOfCare.FirstOrDefault();
                        if (ep != null)
                        {
                            createdDate = ep.CertificationFromDate;
                        }
                    }
                    else if (EncounterIsTransfer)
                    {
                        // Transfer - EncounterTransfer.TransferDate
                        var et = EncounterTransfer.FirstOrDefault();
                        if (et != null)
                        {
                            createdDate = et.TransferDate;
                        }
                    }
                    else if (EncounterIsResumption)
                    {
                        // Resumption - EncounterResumption.ResumptionDate
                        var er = EncounterResumption.FirstOrDefault();
                        if (er != null)
                        {
                            createdDate = er.ResumptionDate;
                        }
                    }
                }

                if (createdDate != null)
                {
                    return (DateTime)createdDate;
                }

                createdDate = EncounterOrTaskStartDateAndTime == null
                    ? EncounterDateTime
                    : EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime;
                if (createdDate != null)
                {
                    return (DateTime)createdDate;
                }

                return createdDate;
            }
        }

        public DateTimeOffset? EncounterOrTaskStartDateAndTime
        {
            get
            {
                if (EncounterStartDate.HasValue)
                {
                    var ret = EncounterStartDate;
                    if (EncounterStartTime.HasValue)
                    {
                        ret = ret.Value.Add(EncounterStartTime.Value.TimeOfDay);
                    }

                    return ret;
                }

                if (Task != null)
                {
                    return Task.TaskStartDateTime;
                }

                return null;
            }
        }

        public DateTimeOffset? TaskStartDateTime => Task?.TaskStartDateTime;

        public string TaskStartDateAndTimeString
        {
            get
            {
                if (TaskStartDateTime == null)
                {
                    return "??";
                }

                var dt = Convert.ToDateTime(((DateTimeOffset)TaskStartDateTime).DateTime);
                var dtString = dt.ToString("MM/dd/yyyy");
                dtString = dtString + " " + (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime
                    ? dt.ToString("HHmm")
                    : dt.ToShortTimeString());
                return string.IsNullOrWhiteSpace(dtString) ? "??" : dtString;
            }
        }

        public bool TrackChangedProperties { get; set; }

        public List<string> ChangedProperties
        {
            get
            {
                if (_changedProperties == null)
                {
                    _changedProperties = new List<string>();
                }

                return _changedProperties;
            }
        }

        [DataMember]
        public int OriginalEncounterStatus
        {
            get { return _OriginalEncounterStatus; }
            set
            {
                _OriginalEncounterStatus = value;
                RaisePropertyChanged("OriginalEncounterStatus");
            }
        }

        public string SummaryOfCareNarrative
        {
            get
            {
                if (EncounterData == null)
                {
                    return null;
                }

                var questionKey = DynamicFormCache.GetQuestionKeyByLabel("Reason for Discharge/Transfer Narrative");
                var ed = EncounterData.Where(p => p.IsSummaryOfCareNarrative(questionKey)).FirstOrDefault();
                if (ed != null)
                {
                    return ed.TextData;
                }

                questionKey = DynamicFormCache.GetQuestionKeyByLabelStartsWith("Summary of Care Narrative");
                ed = EncounterData.Where(p => p.IsSummaryOfCareNarrative(questionKey)).FirstOrDefault();
                if (ed != null)
                {
                    return ed.TextData;
                }

                return null;
            }
        }

        public Form MyForm
        {
            get
            {
                if (FormKey == null || FormKey <= 0)
                {
                    return null;
                }

                return DynamicFormCache.GetFormByKey((int)FormKey);
            }
        }

        public bool ShowOverrideInsurance
        {
            get
            {
                // If we were previously using OverrideInsurance for this (past-edit) encounter - honor it (and override - if any)
                if (OverrideInsuranceKey != null)
                {
                    return true;
                }

                // Inedit - recalculate whether we are using OverrideInsurance
                var saveOverrideInsuranceKey = OverrideInsuranceKey;
                if (EncounterStatus != (int)EncounterStatusType.Edit)
                {
                    return false;
                }

                if (Admission == null || Admission.AdmissionCoverage == null || Task == null)
                {
                    return false;
                }

                if (ServiceTypeNonBillable)
                {
                    return false;
                }

                if (UsingOverrideInsurance == false)
                {
                    return false;
                }

                if (saveOverrideInsuranceKey != ExpectedPayerInsuranceKey)
                {
                    OverrideInsuranceKey = saveOverrideInsuranceKey;
                }

                if (OverrideInsuranceListAvailable)
                {
                    return true;
                }

                return false;
            }
        }

        public bool UsingOverrideInsurance
        {
            get
            {
                var up = UserCache.Current.GetUserProfileFromUserId(EncounterBy);
                if (up == null)
                {
                    return false;
                }

                return up.OverrideInsurance;
            }
        }

        public bool ServiceTypeNonBillable
        {
            get
            {
                if (ServiceTypeKey == null)
                {
                    return false;
                }

                var st = ServiceTypeCache.GetServiceTypeFromKey((int)ServiceTypeKey);
                if (st == null)
                {
                    return false;
                }

                return st.NonBillable;
            }
        }

        public string ExpectedInsuranceName
        {
            get
            {
                if (ExpectedPayerInsuranceKey == null)
                {
                    return null;
                }

                var i = InsuranceCache.GetInsuranceFromKey(ExpectedPayerInsuranceKey);
                if (i == null)
                {
                    return null;
                }

                return i.Name;
            }
        }

        private int? ExpectedPayerInsuranceKey => Task == null ? null : Task.ExpectedPayerInsuranceKey;

        private Insurance OverrideInsurance
        {
            get
            {
                if (OverrideInsuranceKey == null)
                {
                    return null;
                }

                var i = InsuranceCache.GetInsuranceFromKey(OverrideInsuranceKey);
                return i;
            }
        }

        public bool OverrideInsuranceListAvailable
        {
            get
            {
                var oList = OverrideInsuranceList;
                if (oList == null)
                {
                    return false;
                }

                if (ExpectedPayerInsuranceKey == null && oList.Count <= 2)
                {
                    return false;
                }

                if (ExpectedPayerInsuranceKey != null && oList.Count <= 1)
                {
                    return false;
                }

                return true;
            }
        }

        public List<OverrideInsuranceItem> OverrideInsuranceList
        {
            get
            {
                var iList = new List<OverrideInsuranceItem>();
                iList.Add(new OverrideInsuranceItem { InsuranceKey = null, InsuranceName = " " });
                if (OverrideInsurance != null)
                {
                    iList.Add(new OverrideInsuranceItem
                    { InsuranceKey = OverrideInsurance.InsuranceKey, InsuranceName = OverrideInsurance.Name });
                }

                if (Admission == null || Admission.AdmissionCoverage == null)
                {
                    return iList;
                }

                DateTime? serviceDate = null;
                if (EncounterOrTaskStartDateAndTime != null)
                {
                    serviceDate = ((DateTimeOffset)EncounterOrTaskStartDateAndTime).Date;
                }

                if (serviceDate == null)
                {
                    serviceDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);
                }

                serviceDate = ((DateTime)serviceDate).Date;
                foreach (var ac in Admission.AdmissionCoverage.Where(p => p.StartDate.Date <= serviceDate).Where(p =>
                             p.EndDate == null || p.EndDate != null && ((DateTime)p.EndDate).Date >= serviceDate))
                    foreach (var aci in ac.AdmissionCoverageInsurance.Where(p => !p.Inactive))
                        if (aci.PatientInsurance != null &&
                            aci.PatientInsurance.InsuranceKey != ExpectedPayerInsuranceKey &&
                            iList.Where(p => p.InsuranceKey == aci.PatientInsurance.InsuranceKey).Any() == false)
                        {
                            iList.Add(new OverrideInsuranceItem
                            {
                                InsuranceKey = aci.PatientInsurance.InsuranceKey,
                                InsuranceName = aci.PatientInsurance.InsuranceName
                            });
                        }

                iList = iList.OrderBy(p => p.InsuranceName).ToList();
                if (OverrideInsuranceKey != null &&
                    iList.Where(p => p.InsuranceKey == OverrideInsuranceKey).Any() == false)
                {
                    OverrideInsuranceKey = null;
                }

                return iList;
            }
        }

        public int GetVitalsVersion => VitalsVersion == null || VitalsVersion <= 1 ? 1 : (int)VitalsVersion;

        public EncounterHospiceElectionAddendum EncounterHEADatedSignaturePresent
        {
            get
            {
                if (EncounterHospiceElectionAddendum == null)
                {
                    return null;
                }

                return EncounterHospiceElectionAddendum.Where(e =>
                    e.RequiresSignature && e.DateFurnished != null && e.DatedSignaturePresent != null).FirstOrDefault();
            }
        }

        public DateTime SortDate => EncounterDateTime.Date;

        partial void OnServiceTypeKeyChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            Messenger.Default.Send(
                new ServiceTypeKeyChangedEvent(
                    TaskKey.GetValueOrDefault(),
                    EncounterKey,
                    ServiceTypeKey.GetValueOrDefault()),
                Constants.DomainEvents.ServiceTypeKeyChanged);
        }

        public void CopyForwardRiskAssessment(Encounter fromEncounter, int riskAssessmentKey, Guid? riskForID)
        {
            if (EncounterRisk == null)
            {
                return;
            }

            var erList = EncounterRisk
                .Where(er => er.RiskAssessmentKey == riskAssessmentKey && er.RiskForID == riskForID).ToList();
            if (erList != null && erList.Any())
            {
                return;
            }

            // Not already copied - do it now
            if (fromEncounter == null || fromEncounter.EncounterRisk == null ||
                fromEncounter.EncounterRisk.Any() == false)
            {
                return;
            }

            erList = fromEncounter.EncounterRisk
                .Where(p => p.RiskAssessmentKey == riskAssessmentKey && p.RiskForID == riskForID).ToList();
            if (erList == null || erList.Any() == false)
            {
                return;
            }

            foreach (var er in erList)
            {
                var erCopy = new EncounterRisk { EncounterKey = EncounterKey };
                erCopy.CopyFromAll(er);
                EncounterRisk.Add(erCopy);
            }
        }

        partial void OnReviewDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            IsReviewed = ReviewDate == null ? false : true;
        }

        public void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            if (_PatientAddressCollectionView != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (_PatientAddressCollectionView != null)
                    {
                        _PatientAddressCollectionView.Filter -= _PatientAddressCollectionView_Filter;
                    }

                    if (_PatientAddressCollectionView != null)
                    {
                        _PatientAddressCollectionView.Source = null;
                    }
                });
            }
        }

        public bool ServiceTypeDescriptionContains(string contains)
        {
            var std = ServiceTypeDescription;
            if (string.IsNullOrWhiteSpace(std) || string.IsNullOrWhiteSpace(contains))
            {
                return false;
            }

            return std.ToLower().Contains(contains.ToLower()) ? true : false;
        }

        public bool IsHCFACode(string pHCFACode)
        {
            if (string.IsNullOrWhiteSpace(pHCFACode))
            {
                return false;
            }

            var eHCFACode = HCFACode;
            if (string.IsNullOrWhiteSpace(eHCFACode))
            {
                return false;
            }

            return eHCFACode.Trim().ToLower() == pHCFACode.Trim().ToLower() ? true : false;
        }

        public void RaiseProperyChangesMostRecentEncounterOasis()
        {
            RaisePropertyChanged("MostRecentEncounterOasis");
        }

        public void RefreshEncounterReviewList()
        {
            RaisePropertyChanged("EncounterReviewList");
            RaisePropertyChanged("AreEncounterReviews");
        }

        public EncounterReview GetEncounterReviewForSection(string label)
        {
            if (EncounterReview == null)
            {
                return null;
            }

            return EncounterReview.Where(er => er.SectionLabel == label).FirstOrDefault();
        }

        public bool CMSReviewAndCoordinator(int originalEncounterStatus)
        {
            if (!SYS_CDIsHospice && originalEncounterStatus == (int)EncounterStatusType.OASISReview &&
                RoleAccessHelper.CheckPermission(RoleAccess.OASISCoordinator, false))
            {
                return true;
            }

            if (SYS_CDIsHospice && originalEncounterStatus == (int)EncounterStatusType.OASISReview &&
                RoleAccessHelper.CheckPermission(RoleAccess.HISCoordinator, false))
            {
                return true;
            }

            return false;
        }

        partial void OnEncounterStartDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            TimeOfEncounterStartDateChanged = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            CalculateEncounterActualTime();
            if (Admission != null)
            {
                Admission.AdmissionGroupDate =
                    EncounterStartDate.HasValue ? EncounterStartDate.Value.Date : DateTime.Today;
            }

            FilterPatientAddressCollectionView();
            RaisePropertyChanged("CurrentGroup");
            RaisePropertyChanged("CurrentGroup2");
            RaisePropertyChanged("CurrentGroup3");
            RaisePropertyChanged("CurrentGroup4");
            RaisePropertyChanged("CurrentGroup5");
        }

        partial void OnEncounterStartTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            TimeOfEncounterStartTimeChanged = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            CalculateEncounterActualTime();
        }

        [OnDeserialized]
        public new void OnDeserialized(StreamingContext context)
        {
            OriginalEncounterStatus = EncounterStatus;
        }

        partial void OnEncounterEndDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            TimeOfEncounterEndDateChanged = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            CalculateEncounterActualTime();
            FilterPatientAddressCollectionView();
        }

        partial void OnEncounterEndTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            TimeOfEncounterEndTimeChanged = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            CalculateEncounterActualTime();
        }

        private string DateTimeOffsetText(DateTimeOffset? dto)
        {
            if (dto == null)
            {
                return "";
            }

            var dt = ((DateTimeOffset)dto).DateTime;
            return dt.ToShortDateString() + " " + (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime
                ? dt.ToString("HHmm")
                : dt.ToShortTimeString());
        }

        partial void OnPatientAddressKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            TimeOfEncounterPatientAddressKeyChanged = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        }

        partial void OnEncounterActualTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            TimeOfEncounterActualTimeChanged = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        }

        public bool EncounterIsPlanOfCareInCertCycle(AdmissionCertification ac)
        {
            if (ac == null)
            {
                return false;
            }

            if (EncounterPlanOfCare == null)
            {
                return false;
            }

            var ep = EncounterPlanOfCare.FirstOrDefault();
            if (ep == null)
            {
                return false;
            }

            if ((ep.CertificationFromDate == null ? (DateTime?)null : ((DateTime)ep.CertificationFromDate).Date) ==
                (ac.PeriodStartDate == null ? (DateTime?)null : ((DateTime)ac.PeriodStartDate).Date) &&
                (ep.CertificationThruDate == null ? (DateTime?)null : ((DateTime)ep.CertificationThruDate).Date) ==
                (ac.PeriodEndDate == null ? (DateTime?)null : ((DateTime)ac.PeriodEndDate).Date))
            {
                return true;
            }

            return false;
        }

        public bool GetIsEncounterInRecertWindow(Admission adm, Encounter ec)
        {
            //This method is needed for cases when the encounter hasn't yet been added to the context
            var retval = false;
            if (adm == null || ec == null)
            {
                return false;
            }

            var acert = adm.GetAdmissionCertForDate(ec.EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime);
            if (acert == null)
            {
                return false;
            }

            if (ec.EncounterOrTaskStartDateAndTime != null)
            {
                var defaultDisciplineRecertWindow =
                    (double)TenantSettingsCache.Current.DisciplineRecertWindowWithDefault;
                if (adm.ServiceLineKey > 0)
                {
                    // Make Parameter for Recert Window at Service Line Level function
                    // Update the discipline recert window logic within Crescendo
                    var serviceLine = ServiceLineCache.GetServiceLineFromKey(adm.ServiceLineKey);
                    var serviceLineDisciplineRecertWindow = serviceLine.DisciplineRecertWindow.GetValueOrDefault();
                    if (serviceLineDisciplineRecertWindow > 0)
                    {
                        defaultDisciplineRecertWindow = serviceLineDisciplineRecertWindow;
                    }
                }

                retval =
                    (DateTimeOffset)ec.EncounterOrTaskStartDateAndTime.Value.Date.AddDays(
                        defaultDisciplineRecertWindow) >= acert.PeriodEndDate;
            }

            return retval;
        }

        public EncounterOasisAlert GetFirstEncounterOasisAlert()
        {
            if (EncounterOasisAlert == null)
            {
                return null;
            }

            if (EncounterOasisAlert.Any() == false)
            {
                return null;
            }

            return EncounterOasisAlert.FirstOrDefault();
        }

        public void SetupPatientAddressCollectionView(EntityCollection<PatientAddress> source)
        {
            if (_PatientAddressCollectionView == null)
            {
                _PatientAddressCollectionView = new CollectionViewSource();
                _PatientAddressCollectionView.SortDescriptions.Add(new SortDescription("EffectiveFromDate",
                    ListSortDirection.Descending));
                _PatientAddressCollectionView.Filter += _PatientAddressCollectionView_Filter;
            }

            _PatientAddressCollectionView.Source = source;
        }

        public void FilterPatientAddressCollectionView()
        {
            try
            {
                if (PatientAddressCollectionView != null)
                {
                    PatientAddressCollectionView.Refresh();
                    if (PatientAddressKey.HasValue == false) //Only default PlaceOfService if not already set.
                    {
                        DefaultPatientAddressKey();
                    }
                    else
                    {
                        var filteredAddresses = PatientAddressCollectionView.Cast<PatientAddress>();
                        if (filteredAddresses != null && filteredAddresses.Any(patientAddress =>
                                patientAddress.PatientAddressKey == PatientAddressKey) == false)
                        {
                            //reset PatientAddressKey - it is no longer valid w/r to the EncounterStartDate and list of valid Patient addresses
                            PatientAddressKey = null;
                            DefaultPatientAddressKey();
                        }
                    }

                    RaisePropertyChanged("PatientAddressCollectionView");
                    RaisePropertyChanged("PatientAddressKey");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void DefaultPatientAddressKey()
        {
            if (IsNew == false || PatientAddressKey.HasValue)
            {
                return;
            }

            PatientAddressKey = null;
            var list = PatientAddressCollectionView.Cast<PatientAddress>();
            if (list == null)
            {
                return;
            }

            // Try temporary first
            if (list.Any(pa => pa.IsTypeTemporary))
            {
                PatientAddressKey = list.First(pa => pa.IsTypeTemporary).PatientAddressKey;
            }

            // No temporary, try facility
            if (list.Any(pa => pa.IsTypeFacility))
            {
                PatientAddressKey = list.First(pa => pa.IsTypeFacility).PatientAddressKey;
            }

            if (PatientAddressKey.HasValue)
            {
                return;
            }

            // No temporary or facility, try home
            if (list.Any(pa => pa.IsTypeHome))
            {
                PatientAddressKey = list.First(pa => pa.IsTypeHome).PatientAddressKey;
            }
        }

        private void _PatientAddressCollectionView_Filter(object sender, FilterEventArgs e)
        {
            var pa = e.Item as PatientAddress;

            //it was saved to the database, so populate the drop down so that the patient address shows in the UI
            if (EncounterStatus == (int)EncounterStatusType.Completed && !IsNew &&
                ChangedProperties.Any(s => s.Equals("EncounterStartDate") || s.Equals("EncounterStartTime")) == false &&
                PatientAddressKey.HasValue && pa.PatientAddressKey == PatientAddressKey.Value)
            {
                e.Accepted = true;
            }
            else if (pa.Inactive)
            {
                e.Accepted = false;
            }
            else if (pa.IsTypeBilling)
            {
                e.Accepted = false;
            }
            else
            {
                if (pa.EffectiveFromDate.GetValueOrDefault().Date <= EncounterStartDate.GetValueOrDefault().Date &&
                    (pa.EffectiveThruDate.HasValue == false || pa.EffectiveThruDate.Value.Date >=
                        EncounterStartDate.GetValueOrDefault().Date))
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        public void CalculateEncounterActualTime()
        {
            if (EncounterStartDate.HasValue && EncounterStartTime.HasValue && EncounterEndDate.HasValue &&
                EncounterEndTime.HasValue)
            {
                EncounterActualTime = (int)EncounterEndDateAndTime.Value.DateTime
                    .Subtract(EncounterStartDateAndTime.Value.DateTime).TotalMinutes;
            }
            else
            {
                EncounterActualTime = 0;
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (IsReadOnly)
            {
                return;
            }

            if (TrackChangedProperties)
            {
                this.TrackChangedProperties(ChangedProperties, e.PropertyName);
            }

            base.OnPropertyChanged(e);
        }

        public void RefreshDisciplineSummaryEncounterData(DateTime? dischargeDateTime)
        {
            if (EncounterData == null)
            {
                return;
            }

            var q = DynamicFormCache.GetSingleQuestionByDataTemplate("DisciplineSummary");
            if (q == null)
            {
                return;
            }

            var edList = EncounterData.Where(p => p.QuestionKey == q.QuestionKey && p.BoolData == true).ToList();
            if (edList == null)
            {
                return;
            }

            foreach (var ed in edList)
                ed.Text3Data = dischargeDateTime == null
                    ? "*"
                    : "Discharged " + ((DateTime)dischargeDateTime).Date.ToShortDateString();
        }
    }

    public class OverrideInsuranceItem
    {
        public int? InsuranceKey { get; set; }
        public string InsuranceName { get; set; }
    }

    public partial class EncounterAttachedForm
    {
        public bool IsCMSForm
        {
            get
            {
                var f = DynamicFormCache.GetFormByKey(FormKey);
                return f?.IsCMSForm ?? false;
            }
        }

        public bool HaveAnyQuestionsBeenAnswered()
        {
            var Ans = false;

            if (Encounter == null)
            {
                return Ans;
            }

            if (Encounter.EncounterData == null)
            {
                return Ans;
            }

            Ans = DynamicFormCache.GetFormByKey(FormKey).FormSection
                .Any(s => Encounter.EncounterData.Any(ed => ed.SectionKey == s.SectionKey));

            return Ans;
        }
    }

    public partial class EncounterResumption
    {
        partial void OnResumptionDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (AdmissionKey != null)
            {
                Messenger.Default.Send(this, "OasisEncounterResumptionChanged");
            }
        }

        partial void OnVerbalResumptionDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (AdmissionKey != null)
            {
                Messenger.Default.Send(this, "OasisEncounterResumptionChanged");
            }
        }

        partial void OnResumptionReferralDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (AdmissionKey != null)
            {
                Messenger.Default.Send(this, "OasisEncounterResumptionChanged");
            }
        }
    }

    public partial class EncounterTransfer
    {
        public string TransferReasonCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(TransferReasonKey);

        partial void OnTransferDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            Messenger.Default.Send(this, "OasisEncounterTransferChanged");
        }

        partial void OnPlannedTransferChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (PlannedTransfer == null || PlannedTransfer == false)
            {
                return;
            }

            TransferAwareDate = null;
            PatientCareAtAware = false;
        }

        partial void OnTransferReasonKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TransferReasonCodeDescription");
        }

        public void RaisePropertyChangedPatientFacilityStayKey()
        {
            RaisePropertyChanged("PatientFacilityStayKey");
        }
    }

    public partial class EncounterWeight
    {
        public bool HasBMI => BMIValue != null;

        public bool HasBSA => BSAValue != null;

        public string BMIThumbNail
        {
            get
            {
                if (HasBMI)
                {
                    return BMIValue.Value.ToString();
                }

                return string.Empty;
            }
        }

        public string BSAThumbNail
        {
            get
            {
                if (HasBSA)
                {
                    return string.Format("{0} {1}", BSAValue.Value, BSAScale);
                }

                return string.Empty;
            }
        }

        public string WeightThumbNail
        {
            get
            {
                if (WeightValue.HasValue && WeightValue.Value > 0)
                {
                    var ws = WeightScale;

                    if (ws == null)
                    {
                        ws = string.Empty;
                    }
                    else
                    {
                        ws = ws.Trim().ToLower();
                    }

                    return string.Format("{0:0.00} {1}", WeightValue.Value, ws);
                }

                return string.Empty;
            }
        }

        public bool IsWeightLB
        {
            get
            {
                if (WeightValue == null || WeightValue.Value == 0)
                {
                    return false;
                }

                if (WeightScale == null)
                {
                    return true;
                }

                return !WeightScale.Trim().ToLower().Equals("kg");
            }
        }

        public bool IsWeightKG
        {
            get
            {
                if (WeightValue == null || WeightValue.Value == 0)
                {
                    return false;
                }

                if (WeightScale == null)
                {
                    return false;
                }

                return WeightScale.Trim().ToLower().Equals("kg");
            }
        }

        public bool IsHeightIn
        {
            get
            {
                if (HeightValue == null || HeightValue.Value == 0)
                {
                    return false;
                }

                if (HeightScale == null)
                {
                    return true;
                }

                return HeightScale.Trim().ToLower().Equals("in");
            }
        }

        public bool IsHeightCm
        {
            get
            {
                if (HeightValue == null || HeightValue.Value == 0)
                {
                    return false;
                }

                if (HeightScale == null)
                {
                    return false;
                }

                return HeightScale.Trim().ToLower().Equals("cm");
            }
        }

        public bool IsHeightM
        {
            get
            {
                if (HeightValue == null || HeightValue.Value == 0)
                {
                    return false;
                }

                if (HeightScale == null)
                {
                    return false;
                }

                return HeightScale.Trim().ToLower().Equals("m");
            }
        }

        public float? WeightLB
        {
            get
            {
                if (WeightValue == null || WeightValue.Value == 0)
                {
                    return null;
                }

                if (IsWeightLB)
                {
                    return WeightValue;
                }

                var f = WeightValue.Value / 0.45359237F;
                return float.Parse(string.Format("{0:0.00}", f));
            }
        }

        public float? WeightKG
        {
            get
            {
                if (WeightValue == null || WeightValue.Value == 0)
                {
                    return null;
                }

                if (IsWeightKG)
                {
                    return WeightValue;
                }

                var f = WeightValue.Value * 0.45359237F;
                return float.Parse(string.Format("{0:0.00}", f));
            }
        }

        public string HeightInInches
        {
            get
            {
                var heightIn = HeightValue;
                if (heightIn == null || heightIn == 0)
                {
                    return null;
                }

                if (IsHeightIn == false)
                {
                    if (IsHeightCm)
                    {
                        heightIn = heightIn * 0.39370F;
                    }
                    else if (IsHeightM)
                    {
                        heightIn = heightIn * 039.370F;
                    }
                    else
                    {
                        return null;
                    }

                    heightIn = heightIn + 0.5F;
                }

                return string.Format("{0:00}", (float)heightIn);
            }
        }

        public string WeightInPounds
        {
            get
            {
                var weightLB = WeightLB;
                if (weightLB == null || weightLB == 0)
                {
                    return null;
                }

                if (IsWeightLB == false)
                {
                    weightLB = weightLB + 0.5F;
                }

                return string.Format("{0:000}", (float)weightLB);
            }
        }

        public bool IsVersion1 => Version == 1;

        public bool ShowReportedHeight => IsVersion1;

        public string HeightPrompt => GetHeightPrompt(IsVersion1);

        public string WeightPrompt => GetWeightPrompt(IsVersion1);

        public string ReportedHeightPrompt => GetReportedHeightPrompt(IsVersion1);

        public string UsualWeightPrompt => GetUsualWeightPrompt(IsVersion1);

        public static string GetHeightPrompt(bool IsVersion1)
        {
            return IsVersion1 ? "Height" : "Measured Height";
        }

        public static string GetWeightPrompt(bool IsVersion1)
        {
            return IsVersion1 ? "Weight" : "Measured Weight";
        }

        public static string GetReportedHeightPrompt(bool IsVersion1)
        {
            return IsVersion1 ? string.Empty : "Reported Height";
        }

        public static string GetUsualWeightPrompt(bool IsVersion1)
        {
            return IsVersion1 ? "Usual Weight" : "Reported/Usual Weight";
        }
    }

    public partial class EncounterData
    {
        private SectionUI _ReEvalSection;

        private ObservableCollection<SectionUI> _ReEvalSections;
        public RelayCommand ViewSummaryOfCareNarrative_Command { get; set; }

        public string GuidDataFormalName
        {
            get
            {
                if (GuidData == null || GuidData == Guid.Empty)
                {
                    return "?";
                }

                var name = UserCache.Current.GetFormalNameFromUserId(GuidData);
                return string.IsNullOrWhiteSpace(name) ? "?" : name;
            }
        }

        public bool IsPainFollowupQuestionIn48To72HourWindow
        {
            get
            {
                if (DateTimeData == null)
                {
                    return false; //Initial Pain Question's service datetime is null
                }

                if (AddedDateTime == null)
                {
                    return false; //This Pain Question's service datetime
                }

                var fromDT = ((DateTime)DateTimeData).AddHours(48);
                if ((DateTime)AddedDateTime < fromDT)
                {
                    return false;
                }

                var toDT = ((DateTime)DateTimeData).AddHours(72);
                if (toDT.Date != toDT)
                {
                    toDT = toDT.Date.AddDays(1).Date.AddMinutes(-1);
                }

                if ((DateTime)AddedDateTime > toDT)
                {
                    return false;
                }

                return true;
            }
        }

        public ObservableCollection<SectionUI> ReEvalSections
        {
            get { return _ReEvalSections; }
            set
            {
                _ReEvalSections = value;
                RaisePropertyChanged("ReEvalSections");
            }
        }

        public SectionUI ReEvalSection
        {
            get { return _ReEvalSection; }
            set
            {
                _ReEvalSection = value;
                RaisePropertyChanged("ReEvalSection");
            }
        }

        public string CodeLookup { get; set; }

        public string IntDataCode
        {
            get
            {
                if (IntData == null)
                {
                    return null;
                }

                return CodeLookupCache.GetCodeFromKey((int)IntData);
            }
        }

        public string IntDataCodeDescription
        {
            get
            {
                if (IntData == null)
                {
                    return null;
                }

                return CodeLookupCache.GetCodeDescriptionFromKey((int)IntData);
            }
        }

        public string Int2DataCode
        {
            get
            {
                if (Int2Data == null)
                {
                    return null;
                }

                return CodeLookupCache.GetCodeFromKey((int)Int2Data);
            }
        }

        public string Int2DataCodeDescription
        {
            get
            {
                if (Int2Data == null)
                {
                    return null;
                }

                return CodeLookupCache.GetCodeDescriptionFromKey((int)Int2Data);
            }
        }

        public int ReEvaluateFormSectionSequence
        {
            get
            {
                if (ReEvaluateFormSectionKey.HasValue == false)
                {
                    return 1;
                }

                var fs = DynamicFormCache.GetFormSectionByKey((int)ReEvaluateFormSectionKey);
                if (fs == null)
                {
                    return 1;
                }

                return fs.Sequence;
            }
        }

        public bool HasRiskLevelScore => string.IsNullOrWhiteSpace(Text2Data) ? false :
            Text2Data.Trim().ToLower() == "none" ? false : true;

        public bool HasSummaryOfCareNarrative => string.IsNullOrWhiteSpace(Text4Data) ? false : true;

        public void ViewSummaryOfCareNarrativeCommand()
        {
            if (string.IsNullOrWhiteSpace(Text4Data))
            {
                return;
            }

            var d = new NavigateCloseDialog();
            d.Width = double.NaN;
            d.Height = double.NaN;
            d.ErrorMessage = Text4Data;
            d.ErrorQuestion = null;
            d.Title = "Summary of Care";
            d.HasCloseButton = true;
            d.NoVisible = false;
            d.YesVisible = false;
            if (d != null)
            {
                d.Closed += (s, err) => { };
                d.Show();
            }
        }

        public void RaiseEvents()
        {
            RaisePropertyChanged(null);
            RaisePropertyChanged("SignatureData");
        }

        public string TextDataCodes(string codeType)
        {
            string returnString = null;
            if (string.IsNullOrWhiteSpace(TextData))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(codeType))
            {
                return null;
            }

            string[] delimiter = { " - " };
            var splitTextData = TextData.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            foreach (var codeDescription in splitTextData)
            {
                var code = CodeLookupCache.GetCodeFromDescription(codeType, codeDescription);
                if (!string.IsNullOrWhiteSpace(code))
                {
                    returnString = returnString + "|" + code;
                }
            }

            return returnString == null ? null : returnString + "|";
        }

        public bool IsPatientCPR(int questionKey)
        {
            if (QuestionKey != questionKey)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(TextData))
            {
                return false;
            }

            return true;
        }

        public bool IsPatientLST(int questionKey)
        {
            if (QuestionKey != questionKey)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(TextData))
            {
                return false;
            }

            return true;
        }

        public bool IsPatientHOSP(int questionKey)
        {
            if (QuestionKey != questionKey)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(TextData))
            {
                return false;
            }

            return true;
        }

        public bool IsDiscussSpiritualPatient(int questionKey)
        {
            if (QuestionKey != questionKey)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(TextData))
            {
                return false;
            }

            return true;
        }

        public bool IsDiscussSpiritualCaregiver(int questionKey)
        {
            if (QuestionKey != questionKey)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(TextData))
            {
                return false;
            }

            return true;
        }

        public bool IsDyspnea(int questionKey)
        {
            if (QuestionKey != questionKey)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(TextData))
            {
                return false;
            }

            return true;
        }

        public bool IsSummaryOfCareNarrative(int questionKey)
        {
            if (QuestionKey != questionKey)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(TextData))
            {
                return false;
            }

            return true;
        }

        public bool IsEdema(int questionKey)
        {
            if (QuestionKey != questionKey)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(IntDataCodeDescription))
            {
                return false;
            }

            return true;
        }

        public bool IsReceivedFrom(int questionKey)
        {
            if (QuestionKey != questionKey)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(IntDataCodeDescription))
            {
                return false;
            }

            return true;
        }

        public bool IsIntegerLabel(int questionKey)
        {
            if (QuestionKey != questionKey)
            {
                return false;
            }

            if (IntData == null)
            {
                return false;
            }

            return true;
        }

        public bool IsClincialVisitSummary(int questionKey)
        {
            if (QuestionKey != questionKey)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(TextData))
            {
                return false;
            }

            return true;
        }

        public bool IsBilateralAmputee(int questionkey)
        {
            if (QuestionKey != questionkey)
            {
                return false;
            }

            ;
            if (string.IsNullOrWhiteSpace(IntDataCode))
            {
                return false;
            }

            if (IntDataCode.ToLower().StartsWith("no"))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(Int2DataCode))
            {
                return false;
            }

            if (Int2DataCode.ToLower().StartsWith("no"))
            {
                return false;
            }

            return true;
        }

        partial void OnText2DataChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("HasRiskLevelScore");
        }
    }

    public partial class EncounterGait
    {
        partial void OnDistanceChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            CalcGaitVelocityAndCadence();
        }

        public void CalcGaitVelocityAndCadence()
        {
            if (Distance.HasValue && TimetoTravel.HasValue)
            {
                GaitVelocity = (float)Distance / TimetoTravel;
            }

            var roundedGaitVelocity = Convert.ToDecimal(GaitVelocity);
            roundedGaitVelocity = decimal.Round(roundedGaitVelocity, 2);
            if (GaitVelocity != null)
            {
                var gaitVelocity = GaitVelocity.Value;
                float.TryParse(roundedGaitVelocity.ToString(CultureInfo.InvariantCulture), out gaitVelocity);
                GaitVelocity = gaitVelocity;
            }

            if (NumberofSteps.HasValue && TimetoTravel.HasValue)
            {
                Cadence = (float)NumberofSteps * 60 / TimetoTravel;
            }

            var roundedCadence = Convert.ToDecimal(Cadence);
            roundedCadence = decimal.Round(roundedCadence, 0);
            if (Cadence != null)
            {
                var cadence = Cadence.Value;
                float.TryParse(roundedCadence.ToString(CultureInfo.InvariantCulture), out cadence);
                Cadence = cadence;
            }
        }

        partial void OnNumberofStepsChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            CalcGaitVelocityAndCadence();
        }

        partial void OnTimetoTravelChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            CalcGaitVelocityAndCadence();
        }
    }

    public partial class EncounterGoal
    {
        public bool EncounterIsAssistant => Encounter?.IsAssistant ?? true;
    }

    public partial class EncounterGoalElement
    {
        public bool EncounterIsAssistant => Encounter?.IsAssistant ?? true;

        public bool IsM1510R1
        {
            get
            {
                if (AdmissionGoalElement == null)
                {
                    return false;
                }

                if (AdmissionGoalElement.GoalElement == null)
                {
                    return false;
                }

                if (Addressed == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M1510R1|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsM1510R3
        {
            get
            {
                if (AdmissionGoalElement == null)
                {
                    return false;
                }

                if (AdmissionGoalElement.GoalElement == null)
                {
                    return false;
                }

                if (Addressed == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M1510R3|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsM1510R4
        {
            get
            {
                if (AdmissionGoalElement == null)
                {
                    return false;
                }

                if (AdmissionGoalElement.GoalElement == null)
                {
                    return false;
                }

                if (Addressed == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M1510R4|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsM2015
        {
            get
            {
                if (AdmissionGoalElement == null)
                {
                    return false;
                }

                if (AdmissionGoalElement.GoalElement == null)
                {
                    return false;
                }

                if (Addressed == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M2015|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsM2400a
        {
            get
            {
                if (AdmissionGoalElement == null)
                {
                    return false;
                }

                if (AdmissionGoalElement.IncludeonPOC == false)
                {
                    return false;
                }

                if (AdmissionGoalElement.GoalElement == null)
                {
                    return false;
                }

                if (Addressed == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M2400A|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsM2400b
        {
            get
            {
                if (AdmissionGoalElement == null)
                {
                    return false;
                }

                if (AdmissionGoalElement.IncludeonPOC == false)
                {
                    return false;
                }

                if (AdmissionGoalElement.GoalElement == null)
                {
                    return false;
                }

                if (Addressed == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M2400B|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsM2400c
        {
            get
            {
                if (AdmissionGoalElement == null)
                {
                    return false;
                }

                if (AdmissionGoalElement.IncludeonPOC == false)
                {
                    return false;
                }

                if (AdmissionGoalElement.GoalElement == null)
                {
                    return false;
                }

                if (Addressed == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M2400C|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsM2400d
        {
            get
            {
                if (AdmissionGoalElement == null)
                {
                    return false;
                }

                if (AdmissionGoalElement.IncludeonPOC == false)
                {
                    return false;
                }

                if (AdmissionGoalElement.GoalElement == null)
                {
                    return false;
                }

                if (Addressed == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M2400D|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsM2400e
        {
            get
            {
                if (AdmissionGoalElement == null)
                {
                    return false;
                }

                if (AdmissionGoalElement.IncludeonPOC == false)
                {
                    return false;
                }

                if (AdmissionGoalElement.GoalElement == null)
                {
                    return false;
                }

                if (Addressed == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M2400E|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsM2400f1
        {
            get
            {
                if (AdmissionGoalElement == null)
                {
                    return false;
                }

                if (AdmissionGoalElement.IncludeonPOC == false)
                {
                    return false;
                }

                if (AdmissionGoalElement.GoalElement == null)
                {
                    return false;
                }

                if (Addressed == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M2400F1|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsM2400f2
        {
            get
            {
                if (AdmissionGoalElement == null)
                {
                    return false;
                }

                if (AdmissionGoalElement.IncludeonPOC == false)
                {
                    return false;
                }

                if (AdmissionGoalElement.GoalElement == null)
                {
                    return false;
                }

                if (Addressed == false)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + AdmissionGoalElement.GoalElement.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M2400F2|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        partial void OnAddressedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (!Addressed)
            {
                Comment = string.Empty;
            }

            if (AdmissionGoalElement != null)
            {
                if (AdmissionGoalElement.CurrentEncounterGoalElement == null)
                {
                    AdmissionGoalElement.CurrentEncounterGoalElement = this;
                }

                AdmissionGoalElement.RaisePropertyChangedGoalElementStatus();
            }
        }

        public void PopuulateEncounterGoalElementDisciplines(List<int> DiscList, DomainContext context = null)
        {
            if (DiscList == null)
            {
                return;
            }

            EncounterGoalElementDiscipline ege;
            foreach (var disc in DiscList)
            {
                ege = new EncounterGoalElementDiscipline();

                ege.DisciplineKey = disc;
                ege.EncounterKey = EncounterKey;

                if (context != null)
                {
                    context.EntityContainer.GetEntitySet<EncounterGoalElementDiscipline>().Add(ege);
                }

                EncounterGoalElementDiscipline.Add(ege);

                if (Encounter != null && Encounter.EncounterGoalElementDiscipline != null)
                {
                    Encounter.EncounterGoalElementDiscipline.Add(ege);
                }

                ege.AdmissionGoalElementKey = AdmissionGoalElementKey;
            }
        }
    }

    public partial class EncounterPain
    {
        private bool _IsValidating;
        private int? _painScore10;
        private int? _painScoreFACES;
        private int? _painScoreFLACC;
        private int? _painScorePAINAD;
        public int AdmissionKey { get; set; }

        public bool IsValidating
        {
            get { return _IsValidating; }
            set
            {
                _IsValidating = value;
                RaisePropertyChanged("IsValidating");
            }
        }

        public int? PainScore10
        {
            get { return _painScore10; }
            set
            {
                _painScore10 = value;
                RaisePropertyChanged("PainScore10");
                if (IsValidating == false)
                {
                    Messenger.Default.Send(this,
                        string.Format("OasisEncounterPainScoreChanged{0}", AdmissionKey.ToString().Trim()));
                }
            }
        }

        public int? PainScoreFLACC
        {
            get { return _painScoreFLACC; }
            set
            {
                _painScoreFLACC = value;
                RaisePropertyChanged("PainScoreFLACC");
                RaisePropertyChanged("PainScoreFLACCButton");
                if (IsValidating == false)
                {
                    Messenger.Default.Send(this,
                        string.Format("OasisEncounterPainScoreChanged{0}", AdmissionKey.ToString().Trim()));
                }
            }
        }

        public int? PainScorePAINAD
        {
            get { return _painScorePAINAD; }
            set
            {
                _painScorePAINAD = value;
                RaisePropertyChanged("PainScorePAINAD");
                RaisePropertyChanged("PainScorePAINADButton");
                if (IsValidating == false)
                {
                    Messenger.Default.Send(this,
                        string.Format("OasisEncounterPainScoreChanged{0}", AdmissionKey.ToString().Trim()));
                }
            }
        }

        public int? PainScoreFACES
        {
            get { return _painScoreFACES; }
            set
            {
                _painScoreFACES = value;
                RaisePropertyChanged("PainScoreFACES");
                RaisePropertyChanged("PainScoreFacesButton");
                if (IsValidating == false)
                {
                    Messenger.Default.Send(this,
                        string.Format("OasisEncounterPainScoreChanged{0}", AdmissionKey.ToString().Trim()));
                }
            }
        }

        public int? PainScoreInt
        {
            get
            {
                if (PainScore == null)
                {
                    return null;
                }

                int? painScoreInt = null;
                try
                {
                    painScoreInt = int.Parse(PainScore);
                }
                catch
                {
                }

                return painScoreInt;
            }
        }

        public string PainScoreFacesButton => PainScoreFACES == null ? "FACES Eval - Not done" : "FACES Eval - Done";

        public string PainScoreFLACCButton => PainScoreFLACC == null ? "FLACC Eval - Not done" : "FLACC Eval - Done";

        public string PainScorePAINADButton =>
            PainScorePAINAD == null ? "PAINAD Eval - Not done" : "PAINAD Eval - Done";

        public bool IsPain
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PainScore))
                {
                    return false;
                }

                return PainScore.Trim().Equals("0") ? false : true;
            }
        }

        public string ThumbNail => string.Format("Score {0} {1}", PainScore, PainScaleDescription);

        public string PainScaleCode
        {
            get
            {
                if (string.IsNullOrEmpty(PainScale))
                {
                    return null;
                }

                var key = 0;
                try
                {
                    key = int.Parse(PainScale);
                }
                catch
                {
                }

                return CodeLookupCache.GetCodeFromKey(key);
            }
        }

        public string PainScaleDescription
        {
            get
            {
                if (string.IsNullOrEmpty(PainScale))
                {
                    return null;
                }

                var key = 0;
                try
                {
                    key = int.Parse(PainScale);
                }
                catch
                {
                }

                return CodeLookupCache.GetCodeDescriptionFromKey(key);
            }
        }

        partial void OnPainScoreChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (IsValidating == false)
            {
                Messenger.Default.Send(this,
                    string.Format("OasisEncounterPainScoreChanged{0}", AdmissionKey.ToString().Trim()));
            }
        }

        partial void OnPainScaleChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (IsValidating == false)
            {
                Messenger.Default.Send(this,
                    string.Format("OasisEncounterPainScoreChanged{0}", AdmissionKey.ToString().Trim()));
            }
        }
    }

    public partial class EncounterPlanOfCare
    {
        public string CertificationPeriodsLabel
        {
            get
            {
                if (Encounter == null ||
                    Encounter.Admission == null ||
                    Encounter.Admission.HospiceAdmission == false ||
                    Encounter.Admission.AdmissionCertification == null)
                {
                    return "Certification Period";
                }

                var ac = Encounter.Admission.AdmissionCertification
                    .Where(a => CertificationFromDate == a.PeriodStartDate && CertificationThruDate == a.PeriodEndDate)
                    .FirstOrDefault();
                return "Benefit Period Number " + (ac == null ? "1" : ac.PeriodNumber.ToString());
            }
        }

        public DateTime? PeriodStartDate { get; internal set; }

        public string SendBlirb
        {
            get
            {
                if (MailedDate == null && MailedBy == null)
                {
                    return null;
                }

                var user = UserCache.Current.GetFormalNameFromUserId(MailedBy);
                return string.Format("Sent, record by: {0} on {1}", user == null ? "?" : user,
                    MailedDate == null ? "?" : ((DateTimeOffset)MailedDate).Date.ToShortDateString());
            }
        }

        public string VerifiedPhysicianSignatureBlirb
        {
            get
            {
                if (SignedDate == null)
                {
                    return null;
                }

                var user = UserCache.Current.GetFormalNameFromUserId(SignedBy);
                return string.Format("Verified Physician Signature, record by: {0} on {1}", user == null ? "?" : user,
                    SignedDate == null ? "?" : ((DateTimeOffset)SignedDate).Date.ToShortDateString());
            }
        }
    }

    public partial class EncounterPlanOfCareOrder
    {
    }

    public partial class EncounterVisitFrequency
    {
        public string VisitFrequencyText =>
            string.Format("{0} for {1} for {2}",
                Frequency == null ? "" : Frequency,
                Duration == null ? "" : Duration,
                Purpose == null ? "" : Purpose);

        partial void OnFrequencyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("VisitFrequencyText");
        }

        partial void OnDurationChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("VisitFrequencyText");
        }

        partial void OnPurposeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("VisitFrequencyText");
        }
    }

    public partial class EncounterNPWT
    {
        public string ProcedureCode => ProcedureKey == null ? null : CodeLookupCache.GetCodeFromKey((int)ProcedureKey);

        public string ProcedureCodeDescription =>
            ProcedureKey == null ? null : CodeLookupCache.GetCodeDescriptionFromKey((int)ProcedureKey);

        partial void OnProcedureKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ProcedureCode");
            RaisePropertyChanged("ProcedureCodeDescription");
        }

        public bool ValidateNPWTFields(int VisitTotalMinutes)
        {
            ValidationErrors.Clear();
            if (ProcedurePerformedFlag == false)
            {
                ProcedureKey = null;
                TotalTime = null;
                return true;
            }

            if (ProcedureKey <= 0)
            {
                ProcedureKey = null;
            }

            var allValid = true;
            if (ProcedureKey == null)
            {
                ValidationErrors.Add(new ValidationResult("The Which Procedure was Performed? field is required.",
                    new[] { "ProcedureKey" }));
                allValid = false;
            }

            if (TotalTime == null)
            {
                ValidationErrors.Add(new ValidationResult("The Total Time field is required.", new[] { "TotalTime" }));
                allValid = false;
            }
            else if (TotalTime == 0)
            {
                ValidationErrors.Add(new ValidationResult(
                    "The Total Time field is required and must be greater than zero.", new[] { "TotalTime" }));
                allValid = false;
            }
            else if (TotalTime >= VisitTotalMinutes)
            {
                ValidationErrors.Add(new ValidationResult(
                    "The Total Time field must be less than the Visit Total Minutes.", new[] { "TotalTime" }));
                allValid = false;
            }

            return allValid;
        }
    }

    public partial class EncounterOasis
    {
        public bool IsOASISVersionDorHigher
        {
            get
            {
                var ov = OasisCache.GetOasisVersionByVersionKey(OasisVersionKey);
                if (ov == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(ov.VersionCD2))
                {
                    return false;
                }

                if (ov.VersionCD2 == "2.00" || ov.VersionCD2 == "02.00" ||
                    ov.VersionCD2 == "2.10" || ov.VersionCD2 == "02.10" ||
                    ov.VersionCD2 == "2.11" || ov.VersionCD2 == "02.11" ||
                    ov.VersionCD2 == "2.12" || ov.VersionCD2 == "02.12" ||
                    ov.VersionCD2 == "2.20" || ov.VersionCD2 == "02.20" ||
                    ov.VersionCD2 == "2.21" || ov.VersionCD2 == "02.21")
                {
                    return false;
                }

                return true;
            }
        }

        public string HHRGCode
        {
            get
            {
                if (string.IsNullOrWhiteSpace(HIPPSCode))
                {
                    return null;
                }

                if (HIPPSCode.Trim().Length < 4)
                {
                    return null;
                }

                // Derive the HHRG value from the HIPPS code passed.
                //
                // HIPPs code of form: 'xcfsn' where:
                //
                //   OASIS Version 01.60 (and higher):
                //     x - the grouping step (1-5) or comorbidity tier (A-D) - unused in HHRG calculation
                //     c = [A-C] - which map to HHRG clinical score: A=C1,B=C2,C=C3
                //     f = [F-H] - which map to HHRG financial score: F=F1,G=F2,H=F3
                //     s = [K-P] - which map to HHRG service score:  K=S1,L=S2,M=S3,N=S4,P=S5 - Note //O// is omitted since it looks like zero (0)
                //     n = Suppy code [S-X] or a number - unused in HHRG calculation
                //    HHRG value of form: //CxFySx// where:
                //     C,F,S = hard coded for Clinical, Financial, Service
                //     x     = Clinical  score 1 thru 3
                //     y     = Financial score 1 thru 3
                //     z     = Service   score 1 thru 5
                // So... given a HIPPS code, we can programatically derive the correspoding HHRG value

                try
                {
                    // Map clinical [A-C] -> [C1-C3]
                    var intClinical = HIPPSCode[1] - 'A' + 1;
                    if (intClinical < 1 || intClinical > 3)
                    {
                        intClinical = 1;
                    }

                    // Map financial [F-H] -> [F1-F3]
                    var intFinancial = HIPPSCode[2] - 'F' + 1;
                    if (intFinancial < 1 || intFinancial > 3)
                    {
                        intFinancial = 1;
                    }

                    // Map service [K-P] -> [S1-S5]
                    var intService = HIPPSCode[3] - 'K' + 1;
                    // Skip letter 'O'
                    if (intService == 6)
                    {
                        intService = 5;
                    }

                    if (intService < 1 || intService > 5)
                    {
                        intService = 1;
                    }

                    var code = string.Format("C{0}F{1}S{2}", intClinical.ToString().Trim(),
                        intFinancial.ToString().Trim(), intService.ToString().Trim());
                    return code;
                }
                catch
                {
                    return null;
                }
            }
        }

        public string CORRECTION_NUM
        {
            get
            {
                var ol = OasisCache.GetOasisLayoutByCMSFieldNoMessageBox(OasisVersionKey, "CORRECTION_NUM");
                if (ol == null)
                {
                    ol = OasisCache.GetOasisLayoutByCMSFieldNoMessageBox(OasisVersionKey, "CRCTN_NUM");
                }

                string num = null;
                try
                {
                    num = ol == null ? null : B1Record.Substring(ol.StartPos - 1, ol.Length);
                }
                catch
                {
                }

                return string.IsNullOrWhiteSpace(num) ? "??" : num;
            }
        }

        public string ITM_SBST_CD
        {
            get
            {
                if (SYS_CDIsHospice)
                {
                    if (REC_ID == "X1")
                    {
                        return "XX";
                    }

                    if (RFA == "01")
                    {
                        return "HA";
                    }

                    if (RFA == "09")
                    {
                        return "HD";
                    }
                }
                else
                {
                    if (REC_ID == "X1")
                    {
                        return "XX";
                    }

                    return RFA;
                }

                return null;
            }
        }

        public bool SYS_CDIsHospice
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SYS_CD))
                {
                    return false;
                }

                return SYS_CD == "HOSPICE" ? true : false;
            }
        }

        public string SYS_CDDescription
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SYS_CD))
                {
                    return null;
                }

                return SYS_CDIsHospice ? "HIS" : SYS_CD;
            }
        }

        public bool IsMedicareOrMedicaid
        {
            get
            {
                if (SYS_CDIsHospice)
                {
                    if (IsBitSet("A1400A"))
                    {
                        return true;
                    }

                    if (IsBitSet("A1400B"))
                    {
                        return true;
                    }

                    if (IsBitSet("A1400C"))
                    {
                        return true;
                    }

                    if (IsBitSet("A1400D"))
                    {
                        return true;
                    }
                }
                else
                {
                    if (IsBitSet("M0150_CPAY_MCARE_FFS"))
                    {
                        return true;
                    }

                    if (IsBitSet("M0150_CPAY_MCARE_HMO"))
                    {
                        return true;
                    }

                    if (IsBitSet("M0150_CPAY_MCAID_FFS"))
                    {
                        return true;
                    }

                    if (IsBitSet("M0150_CPAY_MCAID_HMO"))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public string RFADescription
        {
            get
            {
                var rfaDescription = OasisCache.GetOasisSurveyRFADescriptionByRFA(OasisVersionKey, RFA);
                var suffix = "";
                if (InactiveDate != null)
                {
                    suffix = "  (Marked no transmit)";
                }
                else if (REC_ID == "X1")
                {
                    suffix = "  (Inactivated)";
                }

                return rfaDescription + suffix;
            }
        }

        public string VersionBlirb
        {
            get
            {
                var ov = OasisCache.GetOasisVersionByVersionKey(OasisVersionKey);
                if (ov == null)
                {
                    return null;
                }

                return string.Format("( {0} Version {1}, Item Set {2} )", SYS_CDDescription,
                    ov.VersionCD2 == null ? "?" : ov.VersionCD2.Trim(),
                    ov.VersionCD1 == null ? "?" : ov.VersionCD1.Trim());
            }
        }

        public bool CanMarkNoTransmit
        {
            get
            {
                if (Encounter != null)
                {
                    if (Encounter.EncounterStatus != (int)EncounterStatusType.Completed)
                    {
                        return false;
                    }

                    if (Encounter.AnyEncounterOasisCMSTransmitted)
                    {
                        return false;
                    }
                }

                return REC_ID == "B1" && CMSTransmission == false && InactiveDate == null ? true : false;
            }
        }

        public bool CanMarkTransmit
        {
            get
            {
                if (Encounter != null)
                {
                    if (Encounter.EncounterStatus != (int)EncounterStatusType.Completed)
                    {
                        return false;
                    }

                    if (Encounter.AnyEncounterOasisCMSTransmitted)
                    {
                        return false;
                    }
                }

                return REC_ID == "B1" && CMSTransmission == false && InactiveDate != null ? true : false;
            }
        }

        public bool CanInactivate
        {
            get
            {
                if (Encounter != null)
                {
                    if (Encounter.EncounterStatus != (int)EncounterStatusType.Completed)
                    {
                        return false;
                    }
                }

                return REC_ID == "B1" && CMSTransmission ? true : false;
            }
        }

        public bool CanActivate
        {
            get
            {
                if (Encounter != null)
                {
                    if (Encounter.EncounterStatus != (int)EncounterStatusType.Completed)
                    {
                        return false;
                    }
                }

                return REC_ID == "X1" && CMSTransmission == false && X1ForKeyChange == false ? true : false;
            }
        }

        public string ServiceLineGroupingLabelAndName
        {
            get
            {
                var slg = ServiceLineCache.GetServiceLineGroupingFromKey(ServiceLineGroupingKey);
                if (slg == null)
                {
                    return "??";
                }

                return string.IsNullOrWhiteSpace(slg.LabelAndName) ? "??" : slg.LabelAndName;
            }
        }

        public string PatientFullName =>
            Encounter == null || Encounter.Patient == null || string.IsNullOrWhiteSpace(Encounter.Patient.FullName)
                ? "??"
                : Encounter.Patient.FullName;

        public string PatientMRN =>
            Encounter == null || Encounter.Patient == null || string.IsNullOrWhiteSpace(Encounter.Patient.MRN)
                ? "??"
                : Encounter.Patient.MRN;

        public bool CanRetransmit => Superceeded ? false : true;

        public string PPSPlusVendorKey
        {
            get
            {
                var oh = OasisHeaderCache.GetOasisHeaderFromKey(OasisHeaderKey);
                if (oh == null)
                {
                    return null;
                }

                return oh.PPSPlusAuthKey;
            }
        }

        public bool IsOasisAnswerChecked(int oasisVersionKey, string CMSField)
        {
            if (B1Record == null)
            {
                return false;
            }

            var ol = OasisCache.GetOasisLayoutByCMSFieldNoMessageBox(oasisVersionKey, CMSField);
            if (ol == null)
            {
                return false;
            }

            return B1Record.Substring(ol.StartPos - 1, 1) == "1";
        }

        partial void OnBypassFlagChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (BypassFlag == null || BypassFlag == false)
            {
                BypassReason = null;
            }
            else if (BypassFlag == true)
            {
                OnHold = false;
                HavenValidationErrors = false;
                HavenValidationErrorsComment = null;
            }
        }

        partial void OnHavenValidationErrorsChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (HavenValidationErrors == false)
            {
                HavenValidationErrorsComment = null;
            }
        }

        private bool IsBitSet(string CMSField)
        {
            var ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, CMSField);
            if (ol == null)
            {
                return false;
            }

            var bit = B1Record.Substring(ol.StartPos - 1, ol.Length);
            if (string.IsNullOrWhiteSpace(bit))
            {
                return false;
            }

            return bit.Equals("1") ? true : false;
        }

        partial void OnRFAChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("RFADescription");
        }

        partial void OnOasisVersionKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("VersionBlirb");
        }
    }

    public partial class EncounterOasisPotential
    {
        public string PatientName
        {
            get
            {
                var name = string.Format("{0}{1},{2}{3}",
                    LastName == null ? "" : LastName.Trim(),
                    Suffix == null ? "" : " " + Suffix.Trim(),
                    FirstName == null ? "" : " " + FirstName.Trim(),
                    MiddleName == null ? "" : " " + MiddleName.Trim()).Trim();
                if (name == "," || name == "")
                {
                    name = " ";
                }

                if (name == "All,")
                {
                    name = "All";
                }

                return name;
            }
        }

        public bool IsActiveUserInServiceLineGrouping =>
            ServiceLineCache.IsActiveUserInServiceLineGrouping((int)ServiceLineGroupingKey);

        public string OASISRFADescription => SYS_CD == "OASIS"
            ? OasisCache.GetOASISOasisSurveyRFADescriptionLongByRFA(RFA)
            : OasisCache.GetHISOasisSurveyRFADescriptionLongByRFA(RFA);
    }

    public partial class OasisFile
    {
        public string CreatedDateTime
        {
            get
            {
                var date = CMSTransmitDateTime.ToString("MM/dd/yyyy");
                var time = "";
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    time = Convert.ToDateTime(CMSTransmitDateTime).ToString("HHmm");
                }
                else
                {
                    time = Convert.ToDateTime(CMSTransmitDateTime).ToShortTimeString();
                }

                return date + " " + time;
            }
        }

        public string CreatedBy
        {
            get
            {
                if (UserId == null)
                {
                    return null;
                }

                return UserCache.Current.GetFormalNameFromUserId(UserId);
            }
        }
    }

    public partial class EncounterOasisAlert
    {
        public string OasisAlertMeasureDescription
        {
            get
            {
                var oa = OasisCache.GetOasisAlertsByOasisAlertKey(OasisAlertKey);
                if (oa == null)
                {
                    return string.Format("OasisAlert for key {0} is undefined.", OasisAlertKey.ToString());
                }

                return oa.MeasureDescription;
            }
        }

        public string OasisAlertDomain
        {
            get
            {
                var oa = OasisCache.GetOasisAlertsByOasisAlertKey(OasisAlertKey);
                if (oa == null)
                {
                    return string.Format(
                        "Error EncounterOasisAlert.OasisAlertDomain: OasisAlert for key {0} is undefined.",
                        OasisAlertKey.ToString());
                }

                return oa.Domain;
            }
        }

        public string OasisAlertType
        {
            get
            {
                var oa = OasisCache.GetOasisAlertsByOasisAlertKey(OasisAlertKey);
                if (oa == null)
                {
                    return string.Format(
                        "Error EncounterOasisAlert.OasisAlertCategory: OasisAlert for key {0} is undefined.",
                        OasisAlertKey.ToString());
                }

                return oa.AlertType;
            }
        }

        public int OasisAlertSequence
        {
            get
            {
                var oa = OasisCache.GetOasisAlertsByOasisAlertKey(OasisAlertKey);
                if (oa == null)
                {
                    return 0;
                }

                return oa.Sequence;
            }
        }

        public EncounterOasisAlert ThisEncounterOasisAlert => this;
    }

    public interface IEncounterRisk
    {
        bool IsSelected { get; set; }
        bool IsTotal { get; set; }
        int RiskAssessmentKey { get; set; }
        Guid? RiskForID { get; set; }
        int? CodeLookupKey { get; set; }
        int? RiskGroupKey { get; set; }
        int? RiskQuestionKey { get; set; }
        int? RiskRangeKey { get; set; }
        int? Score { get; set; }
        void CopyFrom(EncounterRisk copyFrom);
    }

    public partial class EncounterRisk : IEncounterRisk
    {
        public bool IsM2400b
        {
            get
            {
                var ra = DynamicFormCache.GetRiskAssessmentByKey(RiskAssessmentKey);
                if (ra == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(ra.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + ra.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M2400B|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsM2400c
        {
            get
            {
                var ra = DynamicFormCache.GetRiskAssessmentByKey(RiskAssessmentKey);
                if (ra == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(ra.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + ra.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M2400C|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsM2400e
        {
            get
            {
                var ra = DynamicFormCache.GetRiskAssessmentByKey(RiskAssessmentKey);
                if (ra == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(ra.OasisLookbackReferenceCodes))
                {
                    return false;
                }

                var _codes = "|" + ra.OasisLookbackReferenceCodes.ToUpper() + "|";
                if (_codes.Contains("|M2400E|") == false)
                {
                    return false;
                }

                return true;
            }
        }

        public string RiskAssessmentDescription
        {
            get
            {
                var ra = DynamicFormCache.GetRiskAssessmentByKey(RiskAssessmentKey);
                if (ra == null)
                {
                    return "Risk Assessment";
                }

                if (string.IsNullOrWhiteSpace(ra.Label))
                {
                    return "Risk Assessment";
                }

                return ra.Label;
            }
        }

        public string RiskRangeDescription
        {
            get
            {
                if (RiskRangeKey.HasValue == false)
                {
                    return "";
                }

                var rr = DynamicFormCache.GetRiskRangeByKey(RiskRangeKey.GetValueOrDefault());
                if (rr == null)
                {
                    return "";
                }

                return string.IsNullOrWhiteSpace(rr.Label) ? "" : rr.Label;
            }
        }

        public bool IsRisk
        {
            get
            {
                var rrDesc = RiskRangeDescription;
                if (string.IsNullOrWhiteSpace(rrDesc))
                {
                    return false;
                }

                return rrDesc.Trim().ToLower().StartsWith("no") ? false : true;
            }
        }

        public void CopyFrom(EncounterRisk copyFrom)
        {
            Comment = copyFrom.Comment;
            CodeLookupKey = copyFrom.CodeLookupKey;
            IsSelected = copyFrom.IsSelected;
            RiskRangeKey = copyFrom.RiskRangeKey;
            Score = copyFrom.Score;
            IsTotal = copyFrom.IsTotal;
        }

        partial void OnCodeLookupKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            Comment = CodeLookupCache.GetCodeDescriptionFromKey(CodeLookupKey);
        }

        public void CopyFromAll(EncounterRisk copyFrom)
        {
            RiskForID = copyFrom.RiskForID;
            RiskAssessmentKey = copyFrom.RiskAssessmentKey;
            RiskQuestionKey = copyFrom.RiskQuestionKey;
            RiskGroupKey = copyFrom.RiskGroupKey;
            Comment = copyFrom.Comment;
            CodeLookupKey = copyFrom.CodeLookupKey;
            IsSelected = copyFrom.IsSelected;
            RiskRangeKey = copyFrom.RiskRangeKey;
            Score = copyFrom.Score;
            IsTotal = copyFrom.IsTotal;
        }
    }

    public partial class EncounterBP : IValidateVitalsReadingDateTime
    {
        private bool _ReadingDateTimeHadValue;

        public string ThumbNail => string.Format("{0}/{1} {2} side {3} {4}", BPSystolic.ToString(),
            BPDiastolic.ToString(), BPSideDescription,
            BPLocationDescription == null ? "" : ", " + BPLocationDescription,
            VitalsLocationDescription == null ? "" : ", " + VitalsLocationDescription);

        public string BPSideDescription => BPSide.ToUpper().Equals("L") ? "Left" : "Right";

        public string BPLocationDescription => CodeLookupCache.GetCodeDescriptionFromKey(BPLocation);

        public string VitalsLocationDescription => Location;

        public bool ReadingDateTimeHadValue
        {
            get { return _ReadingDateTimeHadValue; }
            set
            {
                _ReadingDateTimeHadValue = value;
                RaisePropertyChanged("ReadingDateTimeHadValue");
            }
        }

        public DateTime? ReadingDateTimeTimePart
        {
            get { return ReadingDateTime; }
            set
            {
                if (Version < 2)
                {
                    return;
                }

                if (value == null || value == DateTime.MinValue)
                {
                    ReadingDateTime = null;
                }
                else
                {
                    var baseDT = ReadingDateTime == null || ReadingDateTime == DateTime.MinValue
                        ? DateTime.Now
                        : ReadingDateTime.Value;
                    ReadingDateTime = new DateTime(baseDT.Year, baseDT.Month, baseDT.Day, value.Value.Hour,
                        value.Value.Minute, 0);
                }

                RaisePropertyChanged("ReadingDateTimeTimePart");
            }
        }

        public bool IsVersion1 => Version == 1;

        partial void OnBPSystolicChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnBPDiastolicChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnBPSideChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnBPLocationChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnBPPositionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnLocationChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        public void SetReadingDateTime()
        {
            if (Version < 2)
            {
                return;
            }

            if (ReadingDateTime == DateTime.MinValue)
            {
                ReadingDateTime = null;
            }

            if (BPSystolic < 0)
            {
                BPSystolic = 0;
            }

            if (BPDiastolic < 0)
            {
                BPDiastolic = 0;
            }

            if (string.IsNullOrEmpty(BPSide))
            {
                BPSide = null;
            }

            if (BPLocation <= 0)
            {
                BPLocation = null;
            }

            if (BPPosition <= 0)
            {
                BPPosition = null;
            }

            if (string.IsNullOrEmpty(Location))
            {
                Location = null;
            }

            // if all 'pieces' are empty - clear reading date/time as well - if any 'pieces' are valued - set reading date/time if its null
            if (BPSystolic == 0 && BPDiastolic == 0 && BPSide == null && BPLocation == null && BPPosition == null &&
                Location == null)
            {
                ReadingDateTime = null;
            }
            else
            {
                if (ReadingDateTime == null && ReadingDateTimeHadValue == false)
                {
                    var dt = DateTime.Now;
                }
            }
        }

        partial void OnReadingDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ReadingDateTime.HasValue)
            {
                //monitor because we don't want to reset ReadingDateTime if USER cleared it
                ReadingDateTimeHadValue = true;
            }

            RaisePropertyChanged("ReadingDateTimeTimePart");
        }

        public DateTime? GetReadingDateTime(bool IsTeleMonitoring)
        {
            if (IsTeleMonitoring && BPDateTime.HasValue)
            {
                return BPDateTime;
            }

            return ReadingDateTime;
        }
    }

    public partial class EncounterCBG
    {
        public string ThumbNail => string.Format("{0}", CBG.ToString());

        public bool IsPostprandial => TestType == "Postprandial";

        partial void OnTestTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (HoursPP.HasValue && TestType == "Fasting")
            {
                HoursPP = null;
            }

            RaisePropertyChanged("IsPostprandial");
        }
    }

    public partial class EncounterPulse : IValidateVitalsReadingDateTime
    {
        private bool _ReadingDateTimeHadValue;

        public string ThumbNail => string.Format("{0} Rhythm {1}, Mode {2} {3}", PulseRate.ToString(),
            PulseRhythmDescription, PulseModeDescription,
            PulseLocationDescription == null ? "" : ", " + PulseLocationDescription);

        public string PulseRhythmDescription => CodeLookupCache.GetCodeDescriptionFromKey(PulseRhythm);

        public string PulseModeDescription => CodeLookupCache.GetCodeDescriptionFromKey(PulseMode);

        public string PulseLocationDescription => Location;

        public bool ReadingDateTimeHadValue
        {
            get { return _ReadingDateTimeHadValue; }
            set
            {
                _ReadingDateTimeHadValue = value;
                RaisePropertyChanged("ReadingDateTimeHadValue");
            }
        }

        public DateTime? ReadingDateTimeTimePart
        {
            get { return ReadingDateTime; }
            set
            {
                if (Version < 2)
                {
                    return;
                }

                if (value == null || value == DateTime.MinValue)
                {
                    ReadingDateTime = null;
                }
                else
                {
                    var baseDT = ReadingDateTime == null || ReadingDateTime == DateTime.MinValue
                        ? DateTime.Now
                        : ReadingDateTime.Value;
                    ReadingDateTime = new DateTime(baseDT.Year, baseDT.Month, baseDT.Day, value.Value.Hour,
                        value.Value.Minute, 0);
                }

                RaisePropertyChanged("ReadingDateTimeTimePart");
            }
        }

        public bool IsVersion1 => Version == 1;

        partial void OnPulseRateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnPulseRhythmChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnPulseModeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnLocationChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        public void SetReadingDateTime()
        {
            if (Version < 2)
            {
                return;
            }

            // tidy the data
            if (ReadingDateTime == DateTime.MinValue)
            {
                ReadingDateTime = null;
            }

            if (PulseRate < 0)
            {
                PulseRate = 0;
            }

            if (PulseRhythm < 0)
            {
                PulseRhythm = 0;
            }

            if (PulseMode < 0)
            {
                PulseMode = 0;
            }

            // if all 'pieces' are empty - clear reading date/time as well - if any 'pieces' are valued - set reading date/time if its null
            if (PulseRate == 0 && PulseRhythm == 0 && PulseMode == 0 && Location == null)
            {
                ReadingDateTime = null;
            }
            else
            {
                if (ReadingDateTime == null && ReadingDateTimeHadValue == false)
                {
                    var dt = DateTime.Now;
                }
            }
        }

        partial void OnReadingDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ReadingDateTime.HasValue)
            {
                //monitor because we don't want to reset ReadingDateTime if USER cleared it
                ReadingDateTimeHadValue = true;
            }

            RaisePropertyChanged("ReadingDateTimeTimePart");
        }

        public DateTime? GetReadingDateTime(bool IsTeleMonitoring)
        {
            if (IsTeleMonitoring && PulseDateTime.HasValue)
            {
                return PulseDateTime;
            }

            return ReadingDateTime;
        }
    }

    public partial class EncounterResp : IValidateVitalsReadingDateTime
    {
        private bool _ReadingDateTimeHadValue;

        public string ThumbNail => string.Format("{0} Rhythm {1} {2}", RespRate.ToString(), RespRhythmDescription,
            RespLocationDescription == null ? "" : ", " + RespLocationDescription);

        public string RespRhythmDescription => CodeLookupCache.GetCodeDescriptionFromKey(RespRhythm);

        public string RespLocationDescription => Location;

        public bool ReadingDateTimeHadValue
        {
            get { return _ReadingDateTimeHadValue; }
            set
            {
                _ReadingDateTimeHadValue = value;
                RaisePropertyChanged("ReadingDateTimeHadValue");
            }
        }

        public DateTime? ReadingDateTimeTimePart
        {
            get { return ReadingDateTime; }
            set
            {
                if (Version < 2)
                {
                    return;
                }

                if (value == null || value == DateTime.MinValue)
                {
                    ReadingDateTime = null;
                }
                else
                {
                    var baseDT = ReadingDateTime == null || ReadingDateTime == DateTime.MinValue
                        ? DateTime.Now
                        : ReadingDateTime.Value;
                    ReadingDateTime = new DateTime(baseDT.Year, baseDT.Month, baseDT.Day, value.Value.Hour,
                        value.Value.Minute, 0);
                }

                RaisePropertyChanged("ReadingDateTimeTimePart");
            }
        }

        public bool IsVersion1 => Version == 1;

        partial void OnRespRateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnRespRhythmChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnLocationChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        public void SetReadingDateTime()
        {
            if (Version < 2)
            {
                return;
            }

            if (ReadingDateTime == DateTime.MinValue)
            {
                ReadingDateTime = null;
            }

            if (RespRate < 0)
            {
                RespRate = 0;
            }

            if (RespRhythm < 0)
            {
                RespRhythm = 0;
            }

            if (string.IsNullOrEmpty(Location))
            {
                Location = null;
            }

            // if all 'pieces' are empty - clear reading date/time as well - if any 'pieces' are valued - set reading date/time if its null
            if (RespRate == 0 && RespRhythm == 0 && Location == null)
            {
                ReadingDateTime = null;
            }
            else
            {
                if (ReadingDateTime == null && ReadingDateTimeHadValue == false)
                {
                    var dt = DateTime.Now;
                }
            }
        }

        partial void OnReadingDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ReadingDateTime.HasValue)
            {
                //monitor because we don't want to reset ReadingDateTime if USER cleared it
                ReadingDateTimeHadValue = true;
            }

            RaisePropertyChanged("ReadingDateTimeTimePart");
        }

        public DateTime? GetReadingDateTime(bool IsTeleMonitoring)
        {
            if (IsTeleMonitoring && RespDateTime.HasValue)
            {
                return RespDateTime;
            }

            return ReadingDateTime;
        }
    }

    public partial class EncounterSpo2 : IValidateVitalsReadingDateTime
    {
        private bool _ReadingDateTimeHadValue;

        public string ThumbNail => string.Format("{0}% {1}", Spo2Percent.ToString(), Spo2LocationDescription);

        public string Spo2LocationDescription => Location;

        public bool ReadingDateTimeHadValue
        {
            get { return _ReadingDateTimeHadValue; }
            set
            {
                _ReadingDateTimeHadValue = value;
                RaisePropertyChanged("ReadingDateTimeHadValue");
            }
        }

        public DateTime? ReadingDateTimeTimePart
        {
            get { return ReadingDateTime; }
            set
            {
                if (Version < 2)
                {
                    return;
                }

                if (value == null || value == DateTime.MinValue)
                {
                    ReadingDateTime = null;
                }
                else
                {
                    var baseDT = ReadingDateTime == null || ReadingDateTime == DateTime.MinValue
                        ? DateTime.Now
                        : ReadingDateTime.Value;
                    ReadingDateTime = new DateTime(baseDT.Year, baseDT.Month, baseDT.Day, value.Value.Hour,
                        value.Value.Minute, 0);
                }

                RaisePropertyChanged("ReadingDateTimeTimePart");
            }
        }

        public bool IsVersion1 => Version == 1;

        partial void OnSpo2PercentChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnLocationChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        public void SetReadingDateTime()
        {
            if (Version < 2)
            {
                return;
            }

            // tidy the data
            if (ReadingDateTime == DateTime.MinValue)
            {
                ReadingDateTime = null;
            }

            if (Spo2Percent < 0)
            {
                Spo2Percent = 0;
            }

            if (string.IsNullOrEmpty(Location))
            {
                Location = null;
            }

            // if all 'pieces' are empty - clear reading date/time as well - if any 'pieces' are valued - set reading date/time if its null
            if (Spo2Percent == 0 && Location == null)
            {
                ReadingDateTime = null;
            }
            else
            {
                if (ReadingDateTime == null && ReadingDateTimeHadValue == false)
                {
                    var dt = DateTime.Now;
                }
            }
        }

        partial void OnReadingDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ReadingDateTime.HasValue)
            {
                //monitor because we don't want to reset ReadingDateTime if USER cleared it
                ReadingDateTimeHadValue = true;
            }

            RaisePropertyChanged("ReadingDateTimeTimePart");
        }

        public DateTime? GetReadingDateTime(bool IsTeleMonitoring)
        {
            if (IsTeleMonitoring && Spo2DateTime.HasValue)
            {
                return Spo2DateTime;
            }

            return ReadingDateTime;
        }
    }

    public partial class EncounterSupply
    {
        public decimal? Charge => OverrideChg ?? SupplyCharge;

        public decimal? Allowance => OverrideAllow ?? SupplyAllow;

        partial void OnSupplyKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            UpdateSupplyCharge();
        }

        private void UpdateSupplyCharge()
        {
            var sup = SupplyCache.GetSupplies().Where(s => s.SupplyKey == SupplyKey).FirstOrDefault();
            if (sup != null)
            {
                SupplyCharge = decimal.Multiply(sup.StdPackCharge == null ? 1 : (decimal)sup.StdPackCharge,
                    SupplyQty == null ? 1 : (decimal)SupplyQty);
                SupplyUnitsKey = sup.PackageCode;
            }
        }

        partial void OnSupplyQtyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            UpdateSupplyCharge();
        }

        partial void OnSupplyAllowChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("Allowance");
        }

        partial void OnSupplyChargeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("Charge");
        }

        partial void OnOverrideAllowChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("Allowance");
        }

        partial void OnOverrideChgChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("Charge");
        }
    }

    public partial class EncounterSupervision
    {
        private bool _canChangeDiscipline = true;

        public bool CanChangeDiscipline
        {
            get { return _canChangeDiscipline; }
            set
            {
                _canChangeDiscipline = value;
                RaisePropertyChanged("CanChangeDiscipline");
            }
        }

        public bool IsEmployeeNameVisible => EmployeePresent == null ? false : (bool)EmployeePresent;

        public bool ShowNewAideFields
        {
            get
            {
                // Only Show New Aide Stuff for version 2 and beyond
                if (Version == 1)
                {
                    return false;
                }

                return DisciplineCache.GetIsAideFromKey(DisciplineKey);
            }
        }

        public string AideServicesLabel => "Evaluation of Aide Services";

        public string AideFollowsCarePlanLabel =>
            "Aide(s) follow the patient's plan of care for completion of tasks assigned by the RN or other appropriate skilled professionals";

        public string AideCommunicatesLabel =>
            "Aide(s) maintain an open communication process with the patient, representative (if any), caregivers and family";

        public string AideCompetentLabel => "Aide(s) demonstrate competency with assigned tasks";

        public string AideCompliesLabel =>
            "Aide(s) comply with infection prevention and control policies and procedures";

        public string AideReportsLabel => "Aide(s) report changes in patient's condition";
        public string AideHonorsLabel => "Aide(s) honor patient's rights";
        public string AideEffectiveLabel => "Aide(s) furnish care in a safe and effective manner";

        partial void OnEmployeePresentChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsEmployeeNameVisible");
        }

        partial void OnVersionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowNewAideFields");
        }

        partial void OnDisciplineKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowNewAideFields");
        }

        private void ClearNewAideFields()
        {
            AideFollowsCarePlanKey = null;
            AideFollowsCarePlanComment = null;
            AideCommunicatesKey = null;
            AideCommunicatesComment = null;
            AideCompetentKey = null;
            AideCompetentComment = null;
            AideCompliesKey = null;
            AideCompliesComment = null;
            AideReportsKey = null;
            AideReportsComment = null;
            AideHonorsKey = null;
            AideHonorsComment = null;
            AideEffectiveKey = null;
            AideEffectiveComment = null;
        }

        public bool ValidateNewAideFields()
        {
            if (ShowNewAideFields == false)
            {
                ClearNewAideFields();
                return true;
            }

            FollowingCarePlan = null;
            if (AideFollowsCarePlanKey == 0)
            {
                AideFollowsCarePlanKey = null;
            }

            if (string.IsNullOrWhiteSpace(AideFollowsCarePlanComment))
            {
                AideFollowsCarePlanComment = null;
            }

            if (AideCommunicatesKey == 0)
            {
                AideCommunicatesKey = null;
            }

            if (string.IsNullOrWhiteSpace(AideCommunicatesComment))
            {
                AideCommunicatesComment = null;
            }

            if (AideCompetentKey == 0)
            {
                AideCompetentKey = null;
            }

            if (string.IsNullOrWhiteSpace(AideCompetentComment))
            {
                AideCompetentComment = null;
            }

            if (AideCompliesKey == 0)
            {
                AideCompliesKey = null;
            }

            if (string.IsNullOrWhiteSpace(AideCompliesComment))
            {
                AideCompliesComment = null;
            }

            if (AideReportsKey == 0)
            {
                AideReportsKey = null;
            }

            if (string.IsNullOrWhiteSpace(AideReportsComment))
            {
                AideReportsComment = null;
            }

            if (AideHonorsKey == 0)
            {
                AideHonorsKey = null;
            }

            if (string.IsNullOrWhiteSpace(AideHonorsComment))
            {
                AideHonorsComment = null;
            }

            if (AideEffectiveKey == 0)
            {
                AideEffectiveKey = null;
            }

            if (string.IsNullOrWhiteSpace(AideEffectiveComment))
            {
                AideEffectiveComment = null;
            }

            var allValid = true;
            if (AideFollowsCarePlanKey == null)
            {
                ValidationErrors.Add(new ValidationResult("The Aide(s) Follow Plan of Care field is required.",
                    new[] { "AideFollowsCarePlanKey" }));
                allValid = false;
            }

            if (AideCommunicatesKey == null)
            {
                ValidationErrors.Add(new ValidationResult("The Aide(s) Communicate field is required.",
                    new[] { "AideCommunicatesKey" }));
                allValid = false;
            }

            if (AideCompetentKey == null)
            {
                ValidationErrors.Add(new ValidationResult("The Aide(s) Competent field is required.",
                    new[] { "AideCompetentKey" }));
                allValid = false;
            }

            if (AideCompliesKey == null)
            {
                ValidationErrors.Add(new ValidationResult("The Aide(s) Comply field is required.",
                    new[] { "AideCompliesKey" }));
                allValid = false;
            }

            if (AideReportsKey == null)
            {
                ValidationErrors.Add(new ValidationResult("The Aide(s) Report field is required.",
                    new[] { "AideReportsKey" }));
                allValid = false;
            }

            if (AideHonorsKey == null)
            {
                ValidationErrors.Add(new ValidationResult("The Aide(s) Honor field is required.",
                    new[] { "AideHonorsKey" }));
                allValid = false;
            }

            if (AideEffectiveKey == null)
            {
                ValidationErrors.Add(new ValidationResult("The Aide(s) Safe and Effective field is required.",
                    new[] { "AideEffectiveKey" }));
                allValid = false;
            }

            return allValid;
        }
    }

    public partial class EncounterPTINR
    {
        public string ThumbNail => string.Format("{0}/{1}", PTSeconds.ToString(), INRRatio.ToString());
    }

    public partial class EncounterTemp : IValidateVitalsReadingDateTime
    {
        private bool _ReadingDateTimeHadValue;

        public bool IsTempC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TempScale))
                {
                    return false;
                }

                return TempScale.Trim().ToLower().Equals("c") ? true : false;
            }
        }

        public bool IsTempF
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TempScale))
                {
                    return true;
                }

                return TempScale.Trim().ToLower().Equals("f") ? true : false;
            }
        }

        public float? TempC
        {
            get
            {
                if (IsTempC)
                {
                    return Temp;
                }

                if (Temp == 32)
                {
                    return 0;
                }

                var f = (Temp - 32) * 5 / 9;
                return float.Parse(string.Format("{0:0.00}", f));
            }
        }

        public float? TempF
        {
            get
            {
                if (IsTempF)
                {
                    return Temp;
                }

                if (Temp == 32)
                {
                    return 0;
                }

                var f = Temp * 9 / 5 + 32;
                return float.Parse(string.Format("{0:0.00}", f));
            }
        }

        public string ThumbNail => string.Format("{0:0.00} {1} {2}", Temp.ToString(), TempScale, TempModeDescription);

        public string TempModeDescription => CodeLookupCache.GetCodeDescriptionFromKey(TempMode);

        public bool ReadingDateTimeHadValue
        {
            get { return _ReadingDateTimeHadValue; }
            set
            {
                _ReadingDateTimeHadValue = value;
                RaisePropertyChanged("ReadingDateTimeHadValue");
            }
        }

        public DateTime? ReadingDateTimeTimePart
        {
            get { return ReadingDateTime; }
            set
            {
                if (Version < 2)
                {
                    return;
                }

                if (value == null || value == DateTime.MinValue)
                {
                    ReadingDateTime = null;
                }
                else
                {
                    var baseDT = ReadingDateTime == null || ReadingDateTime == DateTime.MinValue
                        ? DateTime.Now
                        : ReadingDateTime.Value;
                    ReadingDateTime = new DateTime(baseDT.Year, baseDT.Month, baseDT.Day, value.Value.Hour,
                        value.Value.Minute, 0);
                }

                RaisePropertyChanged("ReadingDateTimeTimePart");
            }
        }

        public bool IsVersion1 => Version == 1;

        partial void OnTempChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnTempScaleChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        partial void OnTempModeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetReadingDateTime();
        }

        public void SetReadingDateTime()
        {
            if (Version < 2)
            {
                return;
            }

            if (ReadingDateTime == DateTime.MinValue)
            {
                ReadingDateTime = null;
            }

            if (Temp < 0)
            {
                Temp = 0;
            }

            if (string.IsNullOrEmpty(TempScale))
            {
                TempScale = null;
            }

            if (TempMode <= 0)
            {
                TempMode = 0;
            }

            // if all 'pieces' are empty - clear reading date/time as well - if any 'pieces' are valued - set reading date/time if its null
            if (Temp == 0 && TempScale == null && TempMode == 0)
            {
                ReadingDateTime = null;
            }
            else
            {
                if (ReadingDateTime == null && ReadingDateTimeHadValue == false)
                {
                    var dt = DateTime.Now;
                }
            }
        }

        partial void OnReadingDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ReadingDateTime.HasValue)
            {
                //monitor because we don't want to reset ReadingDateTime if USER cleared it
                ReadingDateTimeHadValue = true;
            }

            RaisePropertyChanged("ReadingDateTimeTimePart");
        }

        public DateTime? GetReadingDateTime(bool IsTeleMonitoring)
        {
            if (IsTeleMonitoring && TempDateTime.HasValue)
            {
                return TempDateTime;
            }

            return ReadingDateTime;
        }
    }

    public partial class EncounterReview
    {
        private bool _ShowNote;

        public bool ShowNotes
        {
            get { return _ShowNote; }
            set
            {
                _ShowNote = value;
                RaisePropertyChanged("ShowNotes");
            }
        }

        public string ReviewText => GetReviewText(ReviewType, ReviewDateTime, ReviewBy, Encounter);

        private string SYS_CDDescription(Encounter encounter)
        {
            if (encounter == null)
            {
                return "";
            }

            return encounter.SYS_CDDescription;
        }

        public string GetReviewTypeDescription(int reviewType, Encounter encounter)
        {
            switch ((EncounterReviewType)reviewType)
            {
                case EncounterReviewType.KeepInReview:
                    return "Keep in-review, further changes needed";
                case EncounterReviewType.ReadyForCoderReviewPreSign:
                    return "Ready for diagnosis review";
                case EncounterReviewType.ReadyForCoderReviewPostSign:
                    return "Ready for diagnosis review";
                case EncounterReviewType.PassedCoderReviewToReEdit:
                    return "Passed diagnosis review, return to Clinician";
                case EncounterReviewType.PassedCoderReviewToOASISReview:
                    return "Passed diagnosis review, ready for " + SYS_CDDescription(encounter) + " review";
                case EncounterReviewType.PassedCoderReviewToComplete:
                    return "Passed diagnosis review, form is complete";
                case EncounterReviewType.ReadyForOASISReview:
                    return "Ready for " + SYS_CDDescription(encounter) + " review";
                case EncounterReviewType.PassedOASISReview:
                    return "Passed " + SYS_CDDescription(encounter) + " review, form is complete";
                case EncounterReviewType.FailedOASISReview:
                    return "Failed " + SYS_CDDescription(encounter) + " review";
                case EncounterReviewType.FailedOASISReviewRR:
                    return "Failed " + SYS_CDDescription(encounter) + " review, return for re-review";
                case EncounterReviewType.ReadyForOASISReReview:
                    return "Ready for " + SYS_CDDescription(encounter) + " re-review";
                case EncounterReviewType.NoOASISReReview:
                    return "No " + SYS_CDDescription(encounter) + " re-review necessary, form is complete";
                case EncounterReviewType.NoOASISReReviewAgree:
                    return "I agree with the " + SYS_CDDescription(encounter) +
                           " edits applied and that form is complete - No " + SYS_CDDescription(encounter) +
                           " re-review is necessary";
                case EncounterReviewType.ReleaseNoOASISReview:
                    return "Release with no  " + SYS_CDDescription(encounter) + "  review, form is complete";
                default:
                    return "Encounter review type unknown";
            }
        }

        public string GetReviewText(int reviewType, DateTimeOffset? reviewDateTime, Guid? reviewBy, Encounter encounter)
        {
            var dateTime = reviewDateTime == null
                ? ""
                : ((DateTimeOffset)reviewDateTime).DateTime.ToString("MM/dd/yyyy");
            if (string.IsNullOrWhiteSpace(dateTime) == false)
            {
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    dateTime = "on " + dateTime + " " + ((DateTimeOffset)reviewDateTime).DateTime.ToString("HHmm");
                }
                else
                {
                    dateTime = "on " + dateTime + " " + ((DateTimeOffset)reviewDateTime).DateTime.ToShortTimeString();
                }
            }

            return reviewBy == null || reviewBy == Guid.Empty
                ? string.Format("{0} {1}", GetReviewTypeDescription(reviewType, encounter), dateTime)
                : string.Format("{0} {1} by {2}", GetReviewTypeDescription(reviewType, encounter), dateTime,
                    UserCache.Current.GetFormalNameFromUserId(reviewBy));
        }
    }

    public partial class EncounterActivationHistory
    {
        private string _ActivationHistoryParagraphText;
        private bool _IsReactivation;

        private string ActivationByText
        {
            get
            {
                if (ActivationBy == null || ActivationBy == Guid.Empty)
                {
                    return "?";
                }

                var name = UserCache.Current.GetFormalNameFromUserId(ActivationBy);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return "?";
                }

                return name;
            }
        }
        private string NewEncounterStatusText
        {
            get
            {
                if (NewEncounterStatus == 1) return "Started";
                else if (NewEncounterStatus == 2) return "Coder Review";
                else if (NewEncounterStatus == -2) return "Coder Review PostSig";
                else if (NewEncounterStatus == 4) return "OASIS Review";
                else return "?";
            }
        }
        
        public string ActivationHistoryText
        {
            get
            {
                var useMilitaryTime = TenantSettingsCache.Current.TenantSetting.UseMilitaryTime;
                if ((EditType == "A") && (Inactive))
                {
                    return
                        string.Format("  Inactive as of {0} {1} by {2} - {3}",
                            ActivationDateTime != null
                                ? ((DateTime)ActivationDateTime).ToString("MM/dd/yyyy")
                                : "Date?",
                            ActivationDateTime != null
                                ? useMilitaryTime ? ((DateTime)ActivationDateTime).ToLocalTime().ToString("HHmm") :
                                ((DateTime)ActivationDateTime).ToLocalTime().ToShortTimeString()
                                : "Time?",
                            ActivationByText,
                            ActivationReason);
                }
                else if ((EditType == "A") && (Inactive == false))
                {
                    return
                        string.Format("  Reactivated as of {0} {1} by {2} - {3}",
                            ActivationDateTime != null ? ((DateTime)ActivationDateTime).ToString("MM/dd/yyyy") : "Date?",
                            ActivationDateTime != null
                                ? useMilitaryTime ? ((DateTime)ActivationDateTime).ToLocalTime().ToString("HHmm") :
                                ((DateTime)ActivationDateTime).ToLocalTime().ToShortTimeString()
                                : "Time?",
                            ActivationByText,
                            ActivationReason);
                }
                else if ((EditType == "R"))
                {
                    return
                        string.Format("  On {0} {1} {2} reset visit to {3} - {4}",
                            ActivationDateTime != null ? ((DateTime)ActivationDateTime).ToString("MM/dd/yyyy") : "Date?",
                            ActivationDateTime != null
                                ? useMilitaryTime ? ((DateTime)ActivationDateTime).ToLocalTime().ToString("HHmm") :
                                ((DateTime)ActivationDateTime).ToLocalTime().ToShortTimeString()
                                : "Time?",
                            ActivationByText,
                            NewEncounterStatusText,
                            ActivationReason);
                }
                return "?";
            }
        }

        public bool IsReactivation
        {
            get { return _IsReactivation; }
            set
            {
                _IsReactivation = value;
                RaisePropertyChanged("IsReactivation");
            }
        }

        public string InactivationLabel => IsReactivation ? "Reactivate" : "Inactivate";

        public string InactivationReasonLabel => IsReactivation ? "Reactivation Reason" : "Inactivation Reason";

        public string ActivationHistoryParagraphText
        {
            get { return _ActivationHistoryParagraphText; }
            set
            {
                _ActivationHistoryParagraphText = value;
                RaisePropertyChanged("ActivationHistoryParagraphText");
            }
        }
    }

    public partial class EncounterAttempted
    {
        public string ServiceTypeDescription
        {
            get
            {
                var st = ServiceTypeCache.GetServiceTypeFromKey(ServiceTypeKey);
                if (st == null)
                {
                    return "Attempted Service Type ?";
                }

                return string.IsNullOrWhiteSpace(st.Description)
                    ? "Attempted Service Type ?"
                    : "Attempted " + st.Description;
            }
        }

        partial void OnServiceTypeKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ServiceTypeDescription");
        }
    }

    public partial class EncounterESASFindings
    {
        private string _TooltipBlirb;

        public string DateString =>
            EncounterStartDateTime == null ? null : EncounterStartDateTime.DateTime.ToString("M/d");

        public string TooltipBlirb
        {
            get
            {
                var dateTime = EncounterStartDateTime == null
                    ? ""
                    : EncounterStartDateTime.DateTime.ToString("MM/dd/yyyy");
                if (string.IsNullOrWhiteSpace(dateTime) == false)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        dateTime = dateTime + " " + EncounterStartDateTime.DateTime.ToString("HHmm");
                    }
                    else
                    {
                        dateTime = dateTime + " " + EncounterStartDateTime.DateTime.ToShortTimeString();
                    }
                }

                _TooltipBlirb = string.Format("{0} on {1} by {2}", ServiceType, dateTime,
                    UserCache.Current.GetFormalNameFromUserId(EncounterBy));
                return _TooltipBlirb;
            }
        }
    }

    public partial class EncounterHospiceFindings
    {
        private string _TooltipBlirb;

        public string DateString =>
            EncounterStartDateTime == null ? null : EncounterStartDateTime.DateTime.ToString("M/d");

        public string TooltipBlirb
        {
            get
            {
                var dateTime = EncounterStartDateTime == null
                    ? ""
                    : EncounterStartDateTime.DateTime.ToString("MM/dd/yyyy");
                if (string.IsNullOrWhiteSpace(dateTime) == false)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        dateTime = dateTime + " " + EncounterStartDateTime.DateTime.ToString("HHmm");
                    }
                    else
                    {
                        dateTime = dateTime + " " + EncounterStartDateTime.DateTime.ToShortTimeString();
                    }
                }

                _TooltipBlirb = string.Format("{0} on {1} by {2}", ServiceType, dateTime,
                    UserCache.Current.GetFormalNameFromUserId(EncounterBy));
                return _TooltipBlirb;
            }
        }
    }
}