#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Events;
using Virtuoso.Core.Occasional.Model;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.View;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;
//EnumerableExtensions

#endregion

namespace Virtuoso.Core.Model
{
    public interface ISignature
    {
        void SetupEncounterCollectedBy(string M0080, bool IsOasisActive, bool ChangeFlag);
        void CalculateNewEncounterStatus(bool Signed);
        bool GetTakeOffHold();
        void PostSaveProcessing();
        void Cleanup();
    }

    public class Signature : QuestionUI, ISignature
    {
        public override void Cleanup()
        {
            Messenger.Default.Unregister<Encounter>(this, "EncounterSignatureChanged");
            Messenger.Default.Unregister<Encounter>(this);

            try
            {
                if ((DynamicFormViewModel != null) && (DynamicFormViewModel.FormModel != null))
                {
                    DynamicFormViewModel.FormModel.OnSHPAlertsRequestLoaded -= FormModel_OnSHPAlertsRequestLoaded;
                }
            }
            catch
            {
            }

            base.Cleanup();
        }

        public IDynamicFormService Model => DynamicFormViewModel.FormModel;

        public bool ShowSHPAlerts
        {
            get
            {
                if (OasisManager == null)
                {
                    return false;
                }

                if (OasisManager.DoingSurveyOASIS == false)
                {
                    return false;
                }

                if (IsReadOnly)
                {
                    return
                        false; // If the SignatureControl is read only then do not show, currently used in AttachedForm
                }

                bool SHPAlertActive = (TenantSettingsCache.Current.TenantSetting.UsingSHPIntegratedAlerts.HasValue) 
                                      && TenantSettingsCache.Current.TenantSetting.UsingSHPIntegratedAlerts.Value;
                return SHPAlertActive;
            }
        }

        public bool ShowPPSPlus
        {
            get
            {
                if (OasisManager == null)
                {
                    return false;
                }

                if (OasisManager.DoingSurveyOASIS == false)
                {
                    return false;
                }

                if (IsReadOnly)
                {
                    return
                        false; // If the SignatureControl is read only then do not show, currently used in AttachedForm
                }

                return TenantSettingsCache.Current.TenantSetting.UsingPPSPlus;
            }
        }

        public Signature(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            SHPAlertsCommand = new RelayCommand(() =>
            {
                if (ShowSHPAlerts == false)
                {
                    return;
                }

                if (DynamicFormViewModel == null)
                {
                    return;
                }

                if (DynamicFormViewModel.IsOffLine(
                        "The SHP Alerts service is not available when application is offline."))
                {
                    return;
                }

                var EO = DynamicFormViewModel.CurrentEncounter.EncounterOasis.LastOrDefault(a => a.Superceeded == false);

                DynamicFormViewModel.FormModel.OnSHPAlertsRequestLoaded -= FormModel_OnSHPAlertsRequestLoaded;
                DynamicFormViewModel.FormModel.OnSHPAlertsRequestLoaded += FormModel_OnSHPAlertsRequestLoaded;

                var user = UserCache.Current.GetCurrentUserProfile();

                string ClinicianName = "";
                string CaseManager = "";
                if (OasisManager != null && OasisManager.CurrentEncounter != null)
                {
                    ClinicianName = UserCache.Current.GetFullNameFromUserId(OasisManager.CurrentEncounter.EncounterBy);
                }

                if (OasisManager != null && OasisManager.CurrentAdmission != null)
                {
                    CaseManager =
                        UserCache.Current.GetFullNameFromUserId(OasisManager.CurrentAdmission.CareCoordinator);
                }

                DynamicFormViewModel.FormModel.getSHPAlertsRequest(user.UserName, EO.OasisHeaderKey, EO.B1Record,
                    CaseManager, ClinicianName, null, null, "Ref#", null, null, this);
            });

            PPSPlusCommand = new RelayCommand(() =>
            {
                if (ShowPPSPlus == false)
                {
                    return;
                }

                if (DynamicFormViewModel == null)
                {
                    return;
                }

                if (DynamicFormViewModel.IsOffLine(
                        "The PPS Plus Analyzer service is not available when application is offline."))
                {
                    return;
                }

                EncounterOasis EO = null;
                if (DynamicFormViewModel != null && DynamicFormViewModel.CurrentEncounter != null &&
                    DynamicFormViewModel.CurrentEncounter.EncounterOasis != null)
                {
                    EO = DynamicFormViewModel.CurrentEncounter.EncounterOasis.LastOrDefault(a => a.Superceeded == false);
                }

                if (EO != null)
                {
                    DynamicFormViewModel.IsBusy = true;

                    // Convert B1 record and query PPS Plus webservice, returns PDF and displays it
                    DynamicFormViewModel.FormModel.ConvertOASISB1ToC1PPS(EO.B1Record, EO.PPSPlusVendorKey);
                }
            });

            ShowNotes_Command = new RelayCommand(
                () =>
                {
                    if (ShowNotesLabel == SHOWNOTES)
                    {
                        ShowNotes = true;
                        ShowNotesLabel = HIDENOTES;
                    }
                    else
                    {
                        ShowNotes = false;
                        ShowNotesLabel = SHOWNOTES;
                    }
                },
                () => { return IsShowNotesButtonVisible; });
        }

        void FormModel_OnSHPAlertsRequestLoaded(object sender, SHPAlertsRequestArgs e)
        {
            if (e.Response.Contains("Failed:"))
            {
                SHPChildWindow me = new SHPChildWindow("SHP Errors", e.Response);
                me.Show();
            }
            else
            {
                var u = new Uri(e.Response);

                SHPChildWindow me = new SHPChildWindow("SHP Alerts URL", u);

                me.Show();
            }
        }

        public RelayCommand ShowNotes_Command { get; set; }

        public RelayCommand SHPAlertsCommand { set; get; }
        public RelayCommand PPSPlusCommand { set; get; }

        public bool IsShowNotesButtonVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if (OriginalEncounterStatus != (int)EncounterStatusType.Completed)
                {
                    return false;
                }

                if (IsReadOnly)
                {
                    return
                        false; // If the SignatureControl is read only then do not show, currently used in AttachedForm
                }

                return (Encounter.AreEncounterReviews || Encounter.AreEncounterReviewSectionNotes) ? true : false;
            }
        }

        private static string SHOWNOTES = "Show Notes";
        private static string HIDENOTES = "Hide Notes";
        private string _ShowNotesLabel = SHOWNOTES;

        public string ShowNotesLabel
        {
            get { return _ShowNotesLabel; }
            set
            {
                _ShowNotesLabel = value;
                RaisePropertyChanged("ShowNotesLabel");
            }
        }

        private bool _ShowNotes;

        public bool ShowNotes
        {
            get { return _ShowNotes; }
            set
            {
                _ShowNotes = value;
                if (Encounter != null)
                {
                    foreach (EncounterReview er in Encounter.EncounterReview) er.ShowNotes = value;
                }

                RaisePropertyChanged("ShowNotes");
            }
        }

        private int OriginalEncounterStatus;

        public void SignatureSetup()
        {
            if (Encounter != null)
            {
                OriginalEncounterStatus = Encounter.EncounterStatus;
            }

            if (OasisManager != null)
            {
                Messenger.Default.Register<bool>(this,
                    string.Format("OasisBypassFlagChanged{0}", OasisManager.OasisManagerGuid.ToString().Trim()),
                    OasisBypassFlagChanged);
            }

            Messenger.Default.Register<Encounter>(this, "EncounterSignatureChanged", EncounterSignatureChanged);
            ShowNotes = (Encounter != null) && ((OriginalEncounterStatus != (int)EncounterStatusType.Completed) ? true : false);
            if ((OasisManager != null) && (OasisManager.CurrentEncounterOasisIsBypassed == false))
            {
                if (OasisManager.CurrentEncounterOasis.OnHold)
                {
                    KeepOnHold = true;
                }
            }

            SignatureSetupHospiceElectionAddendum();
        }

        public override void RestoreOfflineState(DynamicFormInfo state)
        {
            base.RestoreOfflineState(state);
            if (state.PreviousEncounterStatus > 0)
            {
                OriginalEncounterStatus = state.PreviousEncounterStatus;
            }
        }

        public override void PostSaveProcessing()
        {
            ShowNotes = (Encounter != null) && ((OriginalEncounterStatus != (int)EncounterStatusType.Completed) ? true : false);
        }

        public void OasisBypassFlagChanged(bool BypassFlag)
        {
            ResetAllCheckBoxes();
            RaisePropertyChangedAllCheckBoxes();
        }

        public void EncounterSignatureChanged(Encounter e)
        {
            if ((IsReadyForOASISReviewVisible == false) && (IsReadyForCoderReviewPostSignVisible == false))
            {
                return;
            }

            if (e == null)
            {
                return;
            }

            if (e != Encounter)
            {
                return;
            }

            if (IsReadyForOASISReviewVisible)
            {
                ReadyForOASISReview = (Encounter.EncounterSignature.FirstOrDefault() != null);
            }
            else if (IsReadyForCoderReviewPostSignVisible)
            {
                ReadyForCoderReviewPostSign = (Encounter.EncounterSignature.FirstOrDefault() != null);
            }
        }

        private bool UsingDiagnosisCoders
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) || (Encounter == null))
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm.IsAttempted)
                {
                    return false;
                }

                return Encounter.UsingDiagnosisCoders;
            }
        }

        public bool IsSignatureVisible
        {
            get
            {
                if (ShowPhysicianSignature)
                {
                    return false;
                }

                if (OriginalEncounterStatus == (int)EncounterStatusType.Edit)
                {
                    if (IsReadyForCoderReviewPreSignVisible)
                    {
                        return false;
                    }

                    if (IsReadyForCoderReviewPostSignVisible)
                    {
                        return true;
                    }

                    if (IsReadyForOASISReviewVisible)
                    {
                        return true;
                    }

                    return true;
                }

                if (IsPassedCoderReviewToReEditVisible)
                {
                    return false;
                }

                if (IsPassedCoderReviewToOASISReviewVisible)
                {
                    return true;
                }

                if (IsPassedCoderReviewToCompleteVisible)
                {
                    return true;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.CoderReviewEdit) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID))
                {
                    return true;
                }

                return true;
            }
        }

        public bool ShowPhysicianSignature
        {
            get
            {
                // we show the physician signature line only for COTIs that were generated from a verbal COTI
                AdmissionCOTI ac = AdmissionCOTI;
                if (ac == null)
                {
                    return false;
                }

                if (ac.IsF2F)
                {
                    return false;
                }

                if (ac.IsVerbalCOTI)
                {
                    return false;
                }

                if (ac.VerbalCOTIEncounterKey == null)
                {
                    return false;
                }

                return true;
            }
        }

        public AdmissionCOTI AdmissionCOTI
        {
            get
            {
                // we show the physician signature line only for COTIs that were generated from a verbal COTI
                if ((Admission == null) || (Admission.AdmissionCOTI == null) || (Encounter == null) ||
                    (Encounter.AdmissionCOTI == null))
                {
                    return null;
                }

                AdmissionCOTI ac = Encounter.AdmissionCOTI.FirstOrDefault();
                return ac;
            }
        }

        public bool IgnoreSOCMismatch
        {
            get { return Admission.IgnoreSOCMismatch; }
            set
            {
                Admission.IgnoreSOCMismatch = value;
                RaisePropertyChanged("IsSignatureEnabled");
            }
        }

        private static string ROLE_Physician = "Physician";

        public string SignatureLabel
        {
            get
            {
                if ((DynamicFormViewModel != null) && (DynamicFormViewModel.CurrentForm != null) &&
                    DynamicFormViewModel.CurrentForm.IsOasis)
                {
                    return "Data Entered By";
                }

                if (Encounter == null)
                {
                    return Label;
                }

                if (Encounter.EncounterBy == null)
                {
                    return Label;
                }

                if (UserCache.Current.UserIdIsHospiceMedicalDirector(Encounter.EncounterBy))
                {
                    return "Medical Director Signature";
                }

                if (UserCache.Current.UserIdIsHospicePhysician(Encounter.EncounterBy))
                {
                    return "Hospice Physician Signature";
                }

                if (UserCache.Current.UserIdIsHospiceNursePractitioner(Encounter.EncounterBy))
                {
                    return "Nurse Practitioner Signature";
                }

                return (UserCache.Current.IsUserInRole(
                    UserCache.Current.GetUserProfileFromUserId(Encounter.EncounterBy), ROLE_Physician))
                    ? "Physician Signature"
                    : Label;
            }
        }

        public bool IsSignatureEnabled
        {
            get
            {
                if (OriginalEncounterStatus == (int)EncounterStatusType.Edit)
                {
                    if (IsReadyForCoderReviewPreSignVisible)
                    {
                        return false;
                    }

                    if (IsReadyForCoderReviewPostSignVisible)
                    {
                        return true;
                    }

                    if (IsReadyForOASISReviewVisible)
                    {
                        return true;
                    }

                    return (Encounter.EncounterBy == WebContext.Current.User.MemberID) ? true : false;
                }

                if (IsPassedCoderReviewToReEditVisible)
                {
                    return false;
                }

                if (IsPassedCoderReviewToOASISReviewVisible)
                {
                    return false;
                }

                if (IsPassedCoderReviewToCompleteVisible)
                {
                    return false;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.CoderReviewEdit) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsKeepInReviewVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.CoderReview) &&
                    (RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false)))
                {
                    return true;
                }

                if (Encounter.CMSReviewAndCoordinator(OriginalEncounterStatus))
                {
                    return true;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.OASISReviewEditRR) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID))
                {
                    return true;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.OASISReviewEdit) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsReadyForCoderReviewPreSignVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.Edit) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                    (UsingDiagnosisCoders) &&
                    (TenantSettingsCache.Current.TenantSettingDiagnosisCodersPostSignature == false) &&
                    (Encounter.IsEncounterOasisRequireICDCoderReviewOrServiceTypeOverride))
                {
                    return true;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.Edit) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                    (UsingDiagnosisCoders) &&
                    (TenantSettingsCache.Current.TenantSettingDiagnosisCodersPostSignature == false) &&
                    (Encounter.AreDummyICDsPresent))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsReadyForCoderReviewPostSignVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.Edit) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                    (UsingDiagnosisCoders) &&
                    TenantSettingsCache.Current.TenantSettingDiagnosisCodersPostSignature &&
                    (Encounter.IsEncounterOasisRequireICDCoderReviewOrServiceTypeOverride))
                {
                    return true;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.Edit) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                    (UsingDiagnosisCoders) &&
                    TenantSettingsCache.Current.TenantSettingDiagnosisCodersPostSignature &&
                    (Encounter.AreDummyICDsPresent))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsPassedCoderReviewToReEditVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.CoderReview) && (Encounter.Signed == false) &&
                    (RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false)))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsPassedCoderReviewToOASISReviewVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.CoderReview) && Encounter.Signed &&
                    IsOASISReviewActiveForEncounter &&
                    (RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false)))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsPassedCoderReviewToCompleteVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.CoderReview) && Encounter.Signed &&
                    (IsOASISReviewActiveForEncounter == false) &&
                    (RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false)))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsReadyForOASISReviewVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.Edit) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                    ((UsingDiagnosisCoders == false) || (UsingDiagnosisCoders &&
                                                         ((Encounter
                                                               .IsEncounterOasisRequireICDCoderReviewOrServiceTypeOverride ==
                                                           false)))) &&
                    IsOASISReviewActiveForEncounter)
                {
                    return true;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.CoderReviewEdit) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                    IsOASISReviewActiveForEncounter)
                {
                    return true;
                }

                return false;
            }
        }

        private bool IsOASISReviewActiveForEncounter
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if (OasisManager == null)
                {
                    return false;
                }

                if (Encounter.UsingCMSCoordinator &&
                    OasisManager.DoesInsuranceRequireRFAReview &&
                    (Encounter.IsEncounterOasis) &&
                    (Encounter.IsEncounterOasisBypass == false))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsReadyForOASISReReviewVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.OASISReviewEditRR) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID))
                {
                    return true;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.OASISReviewEdit) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsReadyForPOCOrderReview
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if (Encounter.Inactive)
                {
                    return false;
                }

                if (((OriginalEncounterStatus == (int)EncounterStatusType.Edit) ||
                     (OriginalEncounterStatus == (int)EncounterStatusType.CoderReviewEdit) ||
                     (OriginalEncounterStatus == (int)EncounterStatusType.CoderReviewEditRR)) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                    Encounter.UsingPOCOrderReviewers &&
                    (Encounter.EncounterIsPlanOfCare))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsPassedOASISReviewVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                return Encounter.CMSReviewAndCoordinator(OriginalEncounterStatus);
            }
        }

        public bool IsFailedOASISReviewVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                return Encounter.CMSReviewAndCoordinator(OriginalEncounterStatus);
            }
        }

        public bool IsFailedOASISReviewRRVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                return Encounter.CMSReviewAndCoordinator(OriginalEncounterStatus);
            }
        }

        public bool IsReleaseNoOASISReviewVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                return Encounter.CMSReviewAndCoordinator(OriginalEncounterStatus);
            }
        }

        public bool IsNoOASISReReviewVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.OASISReviewEdit) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID))
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsNoOASISReReviewAgreeVisible
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                bool coordinatorCanEdit = (Encounter.SYS_CDIsHospice)
                    ? TenantSettingsCache.Current.TenantSettingHISCoordinatorCanEdit
                    : TenantSettingsCache.Current.TenantSettingOASISCoordinatorCanEdit;
                if (coordinatorCanEdit == false)
                {
                    return false;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.OASISReviewEdit) &&
                    (Encounter.EncounterBy == WebContext.Current.User.MemberID))
                {
                    return true;
                }

                return false;
            }
        }

        private DateTimeOffset? _ReviewDateTime;

        private bool _KeepInReview;

        public bool KeepInReview
        {
            get { return _KeepInReview; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }

                _KeepInReview = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _ReadyForCoderReviewPreSign;

        public bool ReadyForCoderReviewPreSign
        {
            get { return _ReadyForCoderReviewPreSign; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }
                else
                {
                    EncounterReview er = FindOurEncounterReview((int)EncounterReviewType.ReadyForCoderReviewPreSign);
                    if (er != null)
                    {
                        Model.RemoveEncounterReview(er);
                        if (Encounter != null)
                        {
                            Encounter.RefreshEncounterReviewList();
                        }
                    }
                }

                _ReadyForCoderReviewPreSign = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _ReadyForCoderReviewPostSign;

        public bool ReadyForCoderReviewPostSign
        {
            get { return _ReadyForCoderReviewPostSign; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }
                else
                {
                    EncounterReview er = FindOurEncounterReview((int)EncounterReviewType.ReadyForCoderReviewPostSign);
                    if (er != null)
                    {
                        Model.RemoveEncounterReview(er);
                        if (Encounter != null)
                        {
                            Encounter.RefreshEncounterReviewList();
                        }
                    }
                }

                _ReadyForCoderReviewPostSign = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _PassedCoderReviewToReEdit;

        public bool PassedCoderReviewToReEdit
        {
            get { return _PassedCoderReviewToReEdit; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }

                _PassedCoderReviewToReEdit = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _PassedCoderReviewToOASISReview;

        public bool PassedCoderReviewToOASISReview
        {
            get { return _PassedCoderReviewToOASISReview; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }

                _PassedCoderReviewToOASISReview = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _PassedCoderReviewToComplete;

        public bool PassedCoderReviewToComplete
        {
            get { return _PassedCoderReviewToComplete; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }

                _PassedCoderReviewToComplete = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _ReadyForOASISReview;

        public bool ReadyForOASISReview
        {
            get { return _ReadyForOASISReview; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }

                _ReadyForOASISReview = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _PassedOASISReview;

        public bool PassedOASISReview
        {
            get { return _PassedOASISReview; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }

                _PassedOASISReview = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _FailedOASISReview;

        public bool FailedOASISReview
        {
            get { return _FailedOASISReview; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }

                _FailedOASISReview = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _FailedOASISReviewRR;

        public bool FailedOASISReviewRR
        {
            get { return _FailedOASISReviewRR; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }

                _FailedOASISReviewRR = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _ReleaseNoOASISReview;

        public bool ReleaseNoOASISReview
        {
            get { return _ReleaseNoOASISReview; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }

                _ReleaseNoOASISReview = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _ReadyForOASISReReview;

        public bool ReadyForOASISReReview
        {
            get { return _ReadyForOASISReReview; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }

                _ReadyForOASISReReview = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _NoOASISReReview;

        public bool NoOASISReReview
        {
            get { return _NoOASISReReview; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }

                _NoOASISReReview = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private bool _NoOASISReReviewAgree;

        public bool NoOASISReReviewAgree
        {
            get { return _NoOASISReReviewAgree; }
            set
            {
                if (value)
                {
                    ResetAllCheckBoxes();
                }

                _NoOASISReReviewAgree = value;
                RaisePropertyChangedAllCheckBoxes();
            }
        }

        private void ResetAllCheckBoxes()
        {
            _KeepInReview = false;
            _ReadyForCoderReviewPreSign = false;
            _ReadyForCoderReviewPostSign = false;
            _PassedCoderReviewToReEdit = false;
            _PassedCoderReviewToOASISReview = false;
            _PassedCoderReviewToComplete = false;
            _ReadyForOASISReview = false;
            _PassedOASISReview = false;
            _FailedOASISReview = false;
            _FailedOASISReviewRR = false;
            _ReleaseNoOASISReview = false;
            _ReadyForOASISReReview = false;
            _NoOASISReReview = false;
            _NoOASISReReviewAgree = false;
        }

        private void RaisePropertyChangedAllCheckBoxes()
        {
            _ReviewDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            RaisePropertyChanged("KeepInReview");
            RaisePropertyChanged("KeepInReviewLabel");
            RaisePropertyChanged("ReadyForCoderReviewPreSign");
            RaisePropertyChanged("ReadyForCoderReviewPreSignLabel");
            RaisePropertyChanged("ReadyForCoderReviewPostSign");
            RaisePropertyChanged("ReadyForCoderReviewPostSignLabel");
            RaisePropertyChanged("PassedCoderReviewToReEdit");
            RaisePropertyChanged("PassedCoderReviewToReEditLabel");
            RaisePropertyChanged("PassedCoderReviewToOASISReview");
            RaisePropertyChanged("PassedCoderReviewToOASISReviewLabel");
            RaisePropertyChanged("PassedCoderReviewToComplete");
            RaisePropertyChanged("PassedCoderReviewToCompleteLabel");
            RaisePropertyChanged("ReadyForOASISReview");
            RaisePropertyChanged("ReadyForOASISReviewLabel");
            RaisePropertyChanged("PassedOASISReview");
            RaisePropertyChanged("PassedOASISReviewLabel");
            RaisePropertyChanged("FailedOASISReview");
            RaisePropertyChanged("FailedOASISReviewLabel");
            RaisePropertyChanged("FailedOASISReviewRR");
            RaisePropertyChanged("FailedOASISReviewRRLabel");
            RaisePropertyChanged("ReleaseNoOASISReview");
            RaisePropertyChanged("ReleaseNoOASISReviewLabel");
            RaisePropertyChanged("ReadyForOASISReReview");
            RaisePropertyChanged("ReadyForOASISReReviewLabel");
            RaisePropertyChanged("NoOASISReReview");
            RaisePropertyChanged("NoOASISReReviewLabel");
            RaisePropertyChanged("NoOASISReReviewAgree");
            RaisePropertyChanged("NoOASISReReviewAgreeLabel");
            RaisePropertyChanged("IsSignatureVisible");
            RaisePropertyChanged("ShowPhysicianSignature");
            RaisePropertyChanged("AdmissionCOTI");
            RaisePropertyChanged("IsSignatureEnabled");
            RaisePropertyChanged("IsHavenValidationErrors");
            RaisePropertyChanged("CanEditHavenValidationErrorsComment");
            RaisePropertyChanged("IsOASISReeditingCompletedSurvey");
            RaisePropertyChanged("IsNonOASISReeditingCompletedSurvey");
            RaisePropertyChanged("OnHoldPhrase");
        }

        private string GetLabel(EncounterReviewType reviewType, bool value)
        {
            EncounterReview er = new EncounterReview();
            return er.GetReviewText((int)reviewType, ((value == false) ? null : _ReviewDateTime),
                ((value == false) ? (Guid?)null : WebContext.Current.User.MemberID), Encounter);
        }

        public string KeepInReviewLabel => GetLabel(EncounterReviewType.KeepInReview, _KeepInReview);

        public string ReadyForCoderReviewPreSignLabel => GetLabel(EncounterReviewType.ReadyForCoderReviewPreSign,
            _ReadyForCoderReviewPreSign);

        public string ReadyForCoderReviewPostSignLabel => GetLabel(EncounterReviewType.ReadyForCoderReviewPostSign,
            _ReadyForCoderReviewPostSign);

        public string PassedCoderReviewToReEditLabel =>
            GetLabel(EncounterReviewType.PassedCoderReviewToReEdit, _PassedCoderReviewToReEdit);

        public string PassedCoderReviewToOASISReviewLabel =>
            GetLabel(EncounterReviewType.PassedCoderReviewToOASISReview, _PassedCoderReviewToOASISReview);

        public string PassedCoderReviewToCompleteLabel => GetLabel(EncounterReviewType.PassedCoderReviewToComplete,
            _PassedCoderReviewToComplete);

        public string ReadyForOASISReviewLabel =>
            GetLabel(EncounterReviewType.ReadyForOASISReview, _ReadyForOASISReview);

        public string PassedOASISReviewLabel => GetLabel(EncounterReviewType.PassedOASISReview, _PassedOASISReview);
        public string FailedOASISReviewLabel => GetLabel(EncounterReviewType.FailedOASISReview, _FailedOASISReview);

        public string FailedOASISReviewRRLabel =>
            GetLabel(EncounterReviewType.FailedOASISReviewRR, _FailedOASISReviewRR);

        public string ReleaseNoOASISReviewLabel =>
            GetLabel(EncounterReviewType.ReleaseNoOASISReview, _ReleaseNoOASISReview);

        public string ReadyForOASISReReviewLabel =>
            GetLabel(EncounterReviewType.ReadyForOASISReReview, _ReadyForOASISReReview);

        public string NoOASISReReviewLabel => GetLabel(EncounterReviewType.NoOASISReReview, _NoOASISReReview);

        public string NoOASISReReviewAgreeLabel =>
            GetLabel(EncounterReviewType.NoOASISReReviewAgree, _NoOASISReReviewAgree);

        public string ReadyForCoderReviewPreSignNotes { get; set; }
        public string ReadyForCoderReviewPostSignNotes { get; set; }
        public string PassedCoderReviewToReEditNotes { get; set; }
        public string PassedCoderReviewToOASISReviewNotes { get; set; }
        public string PassedCoderReviewToCompleteNotes { get; set; }
        public string ReadyForOASISReviewNotes { get; set; }
        public string PassedOASISReviewNotes { get; set; }
        public string FailedOASISReviewNotes { get; set; }
        public string FailedOASISReviewRRNotes { get; set; }
        public string ReleaseNoOASISReviewNotes { get; set; }
        public string ReadyForOASISReReviewNotes { get; set; }
        public string NoOASISReReviewNotes { get; set; }
        public string NoOASISReReviewAgreeNotes { get; set; }

        EncounterData CopyProperties(EncounterData source)
        {
            EncounterData EncounterData = new EncounterData();
            EncounterData.TextData = source.TextData;
            EncounterData.Text2Data = source.Text2Data;
            EncounterData.Text3Data = source.Text3Data;
            EncounterData.Text4Data = source.Text4Data;
            EncounterData.AddedDateTime = source.AddedDateTime;
            EncounterData.GuidData = source.GuidData;
            EncounterData.IntData = source.IntData;
            EncounterData.Int2Data = source.Int2Data;
            EncounterData.BoolData = source.BoolData;
            EncounterData.DateTimeData = source.DateTimeData;
            EncounterData.RealData = source.RealData;
            EncounterData.FuncDeficit = source.FuncDeficit;
            EncounterData.SignatureData = source.SignatureData;
            EncounterData.SectionKey = Section.SectionKey;
            EncounterData.QuestionGroupKey = QuestionGroupKey;
            EncounterData.QuestionKey = Question.QuestionKey;
            return EncounterData;
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

        public override bool Validate(out string SubSections)
        {
            bool returnStatus = true;
            SubSections = string.Empty;

            ValidationError = string.Empty;

            if (IsKeepInReviewVisible &&
                (KeepInReview == false) && (ReadyForCoderReviewPreSign == false) &&
                (ReadyForCoderReviewPostSign == false) && (PassedCoderReviewToReEdit == false) &&
                (PassedCoderReviewToOASISReview == false) && (PassedCoderReviewToComplete == false) &&
                (ReadyForOASISReview == false) && (PassedOASISReview == false) && (FailedOASISReview == false) &&
                (FailedOASISReviewRR == false) && (ReleaseNoOASISReview == false) && (ReadyForOASISReReview == false) &&
                (NoOASISReReview == false) && (NoOASISReReviewAgree == false))
            {
                ValidationError = "One of the following review actions must be checked.";
                returnStatus = false;
            }

            if ((Encounter.Signed) && (Encounter.IsEncounterOasisActive) && (DynamicFormViewModel != null) &&
                (DynamicFormViewModel.CurrentForm != null) && (DynamicFormViewModel.CurrentForm.IsOasis) &&
                (Encounter.EncounterCollectedBy == null))
            {
                Encounter.ValidationErrors.Add(new ValidationResult("Data Collected By is required",
                    new[] { "EncounterCollectedBy" }));
                returnStatus = false;
            }

            OnHoldValidationError = null;

            if ((OasisManager != null) && (OasisManager.CurrentEncounterOasisIsBypassed == false))
            {
                bool signatureInError = false;
                if (OasisManager.CurrentEncounterOasis.ValidationErrors != null)
                {
                    foreach (ValidationResult vr in OasisManager.CurrentEncounterOasis.ValidationErrors)
                        if (vr.ErrorMessage.ToLower().Contains("oasis validation errors comment"))
                        {
                            signatureInError = true;
                        }
                }

                if (signatureInError)
                {
                    returnStatus = false;
                }

                if ((IsHavenValidationErrors) && (IsOASISReeditingCompletedSurvey) && (KeepOnHold == false) &&
                    (TakeOffHold == false))
                {
                    OnHoldValidationError = "One of the following On-Hold actions must be checked.";
                    returnStatus = false;
                }
            }

            if ((Encounter.CoSign) && (Encounter.IsCoSigned == false))
            {
                Encounter.ValidationErrors.Add(
                    new ValidationResult("Co-Signature is required when Co-Sign Order? is checked",
                        new[] { "CoSign" }));
                if (DynamicFormViewModel != null)
                {
                    DynamicFormViewModel.ValidEnoughToSave = false;
                    DynamicFormViewModel.ValidEnoughToSaveShowErrorDialog = false;
                }

                returnStatus = false;
            }

            CreateEncounterReview();

            RaisePropertyChanged("IsHavenValidationErrors");
            RaisePropertyChanged("CanEditHavenValidationErrorsComment");
            RaisePropertyChanged("IsOASISReeditingCompletedSurvey");
            RaisePropertyChanged("IsNonOASISReeditingCompletedSurvey");
            RaisePropertyChanged("OnHoldPhrase");

            // Add Review Addendum if need be
            if (returnStatus && Encounter.CMSCoordinatorCanEdit && (Encounter.EncounterAddendum != null))
            {
                EncounterAddendum ea = Encounter.EncounterAddendum.FirstOrDefault();
                if ((ea != null) && string.IsNullOrWhiteSpace(OasisAddendum))
                {
                    if (Model != null)
                    {
                        Model.RemoveEncounterAddendum(ea);
                    }

                    ea = null;
                }

                if (string.IsNullOrWhiteSpace(OasisAddendum) == false)
                {
                    if (ea == null)
                    {
                        ea = new EncounterAddendum();
                        Encounter.EncounterAddendum.Add(ea);
                    }

                    ea.AddendumText = Encounter.SYS_CDDescription + " Review Changes" + "\r" +
                                      OasisAddendum.Replace("<LineBreak />", "\r");
                }
            }

            return returnStatus;
        }

        private void CreateEncounterReview()
        {
            int? reviewType = GetEncounterReviewType();
            EncounterReview r = FindOurEncounterReview(reviewType);
            if (reviewType == null)
            {
                if (r != null)
                {
                    Model.RemoveEncounterReview(r);
                }
            }
            else
            {
                if (r == null)
                {
                    r = new EncounterReview();
                    Encounter.EncounterReview.Add(r);
                }

                r.ReviewBy = WebContext.Current.User.MemberID;
                r.ReviewComment = GetEncounterReviewComment();
                r.ReviewDateTime = (_ReviewDateTime == null) ? DateTime.Now : (DateTimeOffset)_ReviewDateTime;
                r.ReviewType = (int)reviewType;
                r.ReviewUTCDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            }
        }

        private EncounterReview FindOurEncounterReview(int? reviewType)
        {
            EncounterReview r = Encounter.EncounterReview.FirstOrDefault(er => (er.IsNew && (er.SectionLabel == null)));
            if (r != null)
            {
                return r;
            }

            if (reviewType == null)
            {
                return null;
            }

            r = Encounter.EncounterReview.Where(er => (er.SectionLabel == null))
                .OrderByDescending(er => er.ReviewUTCDateTime).FirstOrDefault();
            if (r == null)
            {
                return null;
            }

            switch (reviewType)
            {
                case (int)EncounterReviewType.ReadyForCoderReviewPreSign:
                    if (r.ReviewType != (int)EncounterReviewType.ReadyForCoderReviewPreSign)
                    {
                        r = null;
                    }

                    break;
                case (int)EncounterReviewType.ReadyForCoderReviewPostSign:
                    if (r.ReviewType != (int)EncounterReviewType.ReadyForCoderReviewPostSign)
                    {
                        r = null;
                    }

                    break;
                case (int)EncounterReviewType.PassedCoderReviewToReEdit:
                    if (r.ReviewType != (int)EncounterReviewType.PassedCoderReviewToReEdit)
                    {
                        r = null;
                    }

                    break;
                case (int)EncounterReviewType.PassedCoderReviewToOASISReview:
                    if (r.ReviewType != (int)EncounterReviewType.PassedCoderReviewToOASISReview)
                    {
                        r = null;
                    }

                    break;
                case (int)EncounterReviewType.PassedCoderReviewToComplete:
                    if (r.ReviewType != (int)EncounterReviewType.PassedCoderReviewToComplete)
                    {
                        r = null;
                    }

                    break;
                case (int)EncounterReviewType.ReadyForOASISReview:
                    if (r.ReviewType != (int)EncounterReviewType.ReadyForOASISReview)
                    {
                        r = null;
                    }

                    break;
                case (int)EncounterReviewType.PassedOASISReview:
                case (int)EncounterReviewType.FailedOASISReview:
                case (int)EncounterReviewType.FailedOASISReviewRR:
                case (int)EncounterReviewType.ReleaseNoOASISReview:
                    if (((r.ReviewType == (int)EncounterReviewType.PassedOASISReview) ||
                         (r.ReviewType == (int)EncounterReviewType.FailedOASISReview) ||
                         (r.ReviewType == (int)EncounterReviewType.FailedOASISReviewRR) ||
                         (r.ReviewType == (int)EncounterReviewType.ReleaseNoOASISReview)) == false)
                    {
                        r = null;
                    }

                    break;
                case (int)EncounterReviewType.ReadyForOASISReReview:
                case (int)EncounterReviewType.NoOASISReReview:
                case (int)EncounterReviewType.NoOASISReReviewAgree:
                    if (((r.ReviewType == (int)EncounterReviewType.ReadyForOASISReReview) ||
                         (r.ReviewType == (int)EncounterReviewType.NoOASISReReview) ||
                         (r.ReviewType == (int)EncounterReviewType.NoOASISReReviewAgree)) == false)
                    {
                        r = null;
                    }

                    break;
            }

            return r;
        }

        private int? GetEncounterReviewType()
        {
            if (ReadyForCoderReviewPreSign)
            {
                return (int)EncounterReviewType.ReadyForCoderReviewPreSign;
            }

            if (ReadyForCoderReviewPostSign)
            {
                return (int)EncounterReviewType.ReadyForCoderReviewPostSign;
            }

            if (PassedCoderReviewToReEdit)
            {
                return (int)EncounterReviewType.PassedCoderReviewToReEdit;
            }

            if (PassedCoderReviewToOASISReview)
            {
                return (int)EncounterReviewType.PassedCoderReviewToOASISReview;
            }

            if (PassedCoderReviewToComplete)
            {
                return (int)EncounterReviewType.PassedCoderReviewToComplete;
            }

            if (ReadyForOASISReview)
            {
                return (int)EncounterReviewType.ReadyForOASISReview;
            }

            if (PassedOASISReview)
            {
                return (int)EncounterReviewType.PassedOASISReview;
            }

            if (FailedOASISReview)
            {
                return (int)EncounterReviewType.FailedOASISReview;
            }

            if (FailedOASISReviewRR)
            {
                return (int)EncounterReviewType.FailedOASISReviewRR;
            }

            if (ReleaseNoOASISReview)
            {
                return (int)EncounterReviewType.ReleaseNoOASISReview;
            }

            if (ReadyForOASISReReview)
            {
                return (int)EncounterReviewType.ReadyForOASISReReview;
            }

            if (NoOASISReReview)
            {
                return (int)EncounterReviewType.NoOASISReReview;
            }

            if (NoOASISReReviewAgree)
            {
                return (int)EncounterReviewType.NoOASISReReviewAgree;
            }

            return null;
        }

        private string GetEncounterReviewComment()
        {
            if (ReadyForCoderReviewPreSign)
            {
                return ReadyForCoderReviewPreSignNotes;
            }

            if (ReadyForCoderReviewPostSign)
            {
                return ReadyForCoderReviewPostSignNotes;
            }

            if (PassedCoderReviewToReEdit)
            {
                return PassedCoderReviewToReEditNotes;
            }

            if (PassedCoderReviewToOASISReview)
            {
                return PassedCoderReviewToOASISReviewNotes;
            }

            if (PassedCoderReviewToComplete)
            {
                return PassedCoderReviewToCompleteNotes;
            }

            if (ReadyForOASISReview)
            {
                return ReadyForOASISReviewNotes;
            }

            if (PassedOASISReview)
            {
                return PassedOASISReviewNotes;
            }

            if (FailedOASISReview)
            {
                return FailedOASISReviewNotes;
            }

            if (FailedOASISReviewRR)
            {
                return FailedOASISReviewRRNotes;
            }

            if (ReleaseNoOASISReview)
            {
                return ReleaseNoOASISReviewNotes;
            }

            if (ReadyForOASISReReview)
            {
                return ReadyForOASISReReviewNotes;
            }

            if (NoOASISReReview)
            {
                return NoOASISReReviewNotes;
            }

            if (NoOASISReReviewAgree)
            {
                return NoOASISReReviewAgreeNotes;
            }

            return null;
        }

        public void CalculateNewEncounterStatus(bool Signed)
        {
            if (Encounter == null)
            {
                return;
            }

            int newStatus = Encounter.EncounterStatus;
            if (Encounter.EncounterStatus == (int)EncounterStatusType.Edit)
            {
                if (Encounter.EncounterIsEval && Encounter.AdmissionDiscipline != null &&
                    Encounter.AdmissionDiscipline.NotTaken)
                {
                    // If we are performing an assessment or eval encounter AND we do not admit the discipline, we should skip over the coder
                    //  encounter statuses and go straight to completed - PROVIDED the form is signed
                    if (Signed)
                    {
                        newStatus = (int)EncounterStatusType.Completed;
                    }
                }
                else if (ReadyForCoderReviewPreSign)
                {
                    newStatus = (int)EncounterStatusType.CoderReview;
                }
                else if (ReadyForCoderReviewPostSign)
                {
                    newStatus = (int)EncounterStatusType.CoderReview;
                }
                else if (ReadyForOASISReview)
                {
                    newStatus = (int)EncounterStatusType.OASISReview;
                }
                else if (Signed)
                {
                    if (IsReadyForPOCOrderReview)
                    {
                        newStatus = (int)EncounterStatusType.POCOrderReview;
                    }
                    else
                    {
                        newStatus = (int)EncounterStatusType.Completed;
                    }
                }
            }
            else if (Encounter.EncounterStatus == (int)EncounterStatusType.CoderReview)
            {
                if (PassedCoderReviewToReEdit)
                {
                    newStatus = (int)EncounterStatusType.CoderReviewEdit;
                }
                else if (PassedCoderReviewToOASISReview)
                {
                    newStatus = (int)EncounterStatusType.OASISReview;
                }
                else if (PassedCoderReviewToComplete)
                {
                    newStatus = (int)EncounterStatusType.Completed;
                }
            }
            else if (Encounter.EncounterStatus == (int)EncounterStatusType.CoderReviewEdit)
            {
                if (ReadyForOASISReview)
                {
                    newStatus = (int)EncounterStatusType.OASISReview;
                }
                else if (Signed)
                {
                    if (IsReadyForPOCOrderReview)
                    {
                        newStatus = (int)EncounterStatusType.POCOrderReview;
                    }
                    else
                    {
                        newStatus = (int)EncounterStatusType.Completed;
                    }
                }
            }
            else if (Encounter.EncounterStatus == (int)EncounterStatusType.OASISReview)
            {
                if (PassedOASISReview)
                {
                    newStatus = (int)EncounterStatusType.Completed;
                }
                else if (FailedOASISReview)
                {
                    newStatus = (int)EncounterStatusType.OASISReviewEdit;
                }
                else if (FailedOASISReviewRR)
                {
                    newStatus = (int)EncounterStatusType.OASISReviewEditRR;
                }
                else if (ReleaseNoOASISReview)
                {
                    newStatus = (int)EncounterStatusType.Completed;
                }
            }
            else if (Encounter.EncounterStatus == (int)EncounterStatusType.OASISReviewEditRR)
            {
                if (ReadyForOASISReReview)
                {
                    newStatus = (int)EncounterStatusType.OASISReview;
                }
            }
            else if (Encounter.EncounterStatus == (int)EncounterStatusType.OASISReviewEdit)
            {
                if (ReadyForOASISReReview)
                {
                    newStatus = (int)EncounterStatusType.OASISReview;
                }
                else if (NoOASISReReview)
                {
                    newStatus = (int)EncounterStatusType.Completed;
                }
                else if (NoOASISReReviewAgree)
                {
                    newStatus = (int)EncounterStatusType.Completed;
                }
            }
            else if (Encounter.EncounterStatus == (int)EncounterStatusType.POCOrderReview)
            {
                if (Encounter.IsReviewed)
                {
                    newStatus = (int)EncounterStatusType.Completed;
                }
            }

            Encounter.EncounterStatus = newStatus;
        }

        public bool IsHavenValidationErrors
        {
            get
            {
                if ((OasisManager == null) || (OasisManager.CurrentEncounterOasis == null))
                {
                    return false;
                }

                if (OasisManager.CurrentEncounterOasisIsBypassed)
                {
                    return false;
                }

                if (OasisManager.CurrentEncounterOasis.SYS_CDIsHospice)
                {
                    return false;
                }

                return OasisManager.CurrentEncounterOasis.HavenValidationErrors;
            }
        }

        public bool CanEditHavenValidationErrorsComment
        {
            get
            {
                if (Encounter.SYS_CDIsHospice)
                {
                    return false;
                }

                if (IsHavenValidationErrors == false)
                {
                    return false;
                }

                if ((OriginalEncounterStatus == (int)EncounterStatusType.Completed)
                    || (OriginalEncounterStatus == (int)EncounterStatusType.OASISReview)
                    || (OriginalEncounterStatus == (int)EncounterStatusType.OASISReviewEdit)
                    || (OriginalEncounterStatus == (int)EncounterStatusType.OASISReviewEditRR)
                   )
                {
                    // Coordinator is editing a survey
                    if (Encounter.CanEditCompleteOASIS)
                    {
                        return true;
                    }
                }
                else
                {
                    // Original user is still editing
                    if (Encounter.EncounterBy == WebContext.Current.User.MemberID)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsOASISReeditingCompletedSurvey
        {
            get
            {
                if (Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    if (OriginalEncounterStatus == (int)EncounterStatusType.Completed)
                    {
                        if (Encounter.CanEditCompleteCMS)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public bool IsNonOASISReeditingCompletedSurvey
        {
            get
            {
                if (Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    if (OriginalEncounterStatus == (int)EncounterStatusType.Completed)
                    {
                        if (Encounter.CanEditCompleteCMS == false)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public string OnHoldPhrase
        {
            get
            {
                if ((OasisManager == null) || (OasisManager.CurrentEncounterOasis == null))
                {
                    return null;
                }

                if (OasisManager.CurrentEncounterOasisIsBypassed)
                {
                    return null;
                }

                if (OasisManager.CurrentEncounterOasis.SYS_CDIsHospice)
                {
                    return null;
                }

                if (OasisManager.CurrentEncounterOasis.HavenValidationErrors == false)
                {
                    return null;
                }

                return (OasisManager.CurrentEncounterOasis.OnHold)
                    ? "Survey is currently On-Hold"
                    : "Survey has been taken Off-hold";
            }
        }

        private bool _TakeOffHold;

        public bool TakeOffHold
        {
            get { return _TakeOffHold; }
            set
            {
                _TakeOffHold = value;
                if (_TakeOffHold)
                {
                    KeepOnHold = false;
                    if ((OasisManager != null) && (OasisManager.CurrentEncounterOasisIsBypassed == false))
                    {
                        OasisManager.CurrentEncounterOasis.OnHold = false;
                    }
                }

                RaisePropertyChanged("TakeOffHold");
            }
        }

        public bool GetTakeOffHold()
        {
            return TakeOffHold;
        }

        private bool _KeepOnHold;

        public bool KeepOnHold
        {
            get { return _KeepOnHold; }
            set
            {
                _KeepOnHold = value;
                if (_KeepOnHold)
                {
                    TakeOffHold = false;
                    if ((OasisManager != null) && (OasisManager.CurrentEncounterOasisIsBypassed == false))
                    {
                        OasisManager.CurrentEncounterOasis.OnHold = true;
                    }
                }

                RaisePropertyChanged("KeepOnHold");
            }
        }

        private string _OnHoldValidationError;

        public string OnHoldValidationError
        {
            get { return _OnHoldValidationError; }
            set
            {
                if (_OnHoldValidationError != value)
                {
                    _OnHoldValidationError = value;

                    this.RaisePropertyChangedLambda(p => p.OnHoldValidationError);
                }
            }
        }

        public string OasisChangesLabel
        {
            get
            {
                if (Encounter == null)
                {
                    return "OASIS Review Changes";
                }

                return Encounter.SYS_CDDescription + " Review Changes";
            }
        }

        public string OasisAddendum
        {
            get
            {
                if (OasisManager == null)
                {
                    return null;
                }

                if (Encounter == null)
                {
                    return null;
                }

                if (Encounter.CMSCoordinatorCanEdit == false)
                {
                    return null;
                }

                return OasisManager.OasisAddendum(true);
            }
        }

        public bool ProtectedOverrideM0080
        {
            get
            {
                if (OasisManager == null)
                {
                    return true;
                }

                if (OasisManager.CurrentEncounter == null)
                {
                    return true;
                }

                if (OasisManager.CurrentEncounter.Inactive)
                {
                    return true;
                }

                if (Protected == false)
                {
                    return false;
                }

                if (OasisManager.CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return (!OasisManager.CurrentEncounter.CanEditCompleteOASIS);
                }

                return Protected;
            }
        }

        private List<UserProfile> _EncounterCollectedByList;

        public List<UserProfile> EncounterCollectedByList
        {
            get { return _EncounterCollectedByList; }
            set
            {
                _EncounterCollectedByList = value;
                RaisePropertyChanged("EncounterCollectedByList");
            }
        }

        public void SetupEncounterCollectedBy(string M0080, bool IsOasisActive, bool ChangeFlag)
        {
            if (Encounter == null)
            {
                return;
            }

            if (Encounter.SYS_CDIsHospice)
            {
                return;
            }

            ServiceType serviceType = OasisManager?.GetOASISServiceType(M0080);
            if ((!IsOasisActive) || (serviceType == null))
            {
                Encounter.EncounterCollectedBy = Encounter.EncounterBy;
                EncounterCollectedByList = new List<UserProfile>();
                UserProfile ul = UserCache.Current.GetUserProfileFromUserId(Encounter.EncounterBy);
                if (ul != null)
                {
                    EncounterCollectedByList.Add(ul);
                }

                RaisePropertyChanged("EncounterCollectedByList");
                return;
            }

            //Re-default 
            if (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed)
            {
                EncounterCollectedByList =
                    UserCache.Current.GetUserProfilePlusMeByDisciplineKey(serviceType.DisciplineKey,
                        Encounter.EncounterCollectedBy);
            }
            else
            {
                EncounterCollectedByList = UserCache.Current.GetUserProfileByDisciplineKey(serviceType.DisciplineKey);
            }

            if (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed)
            {
                return;
            }

            //Re-default EncounterCollectedBy 
            if ((Encounter.EncounterCollectedBy != null) &&
                (UserCache.Current.IsDisciplineInUserProfile(serviceType.DisciplineKey,
                    (Guid)Encounter.EncounterCollectedBy) == false))
            {
                Encounter.EncounterCollectedBy = null;
            }

            if ((Encounter.EncounterCollectedBy == null) && (Encounter.EncounterBy != null) &&
                UserCache.Current.IsDisciplineInUserProfile(serviceType.DisciplineKey, (Guid)Encounter.EncounterBy))
            {
                Encounter.EncounterCollectedBy = Encounter.EncounterBy;
            }
        }

        private bool _IsReadOnly;

        public bool IsReadOnly
        {
            get { return _IsReadOnly; }
            set { _IsReadOnly = value; }
        }

        #region HospiceElectionAddendum

        private EncounterHospiceElectionAddendum _CurrentEncounterHospiceElectionAddendum;

        public EncounterHospiceElectionAddendum CurrentEncounterHospiceElectionAddendum
        {
            get { return _CurrentEncounterHospiceElectionAddendum; }
            set
            {
                _CurrentEncounterHospiceElectionAddendum = value;
                RaisePropertyChanged("CurrentEncounterHospiceElectionAddendum");
            }
        }

        public bool ShowHospiceElectionAddendum => (CurrentEncounterHospiceElectionAddendum != null);

        public bool ShowNoHospiceElectionAddendumSignature
        {
            get
            {
                if (ShowHospiceElectionAddendum == false)
                {
                    return false;
                }

                return (CurrentEncounterHospiceElectionAddendum.DatedSignaturePresent == null) ? true : false;
            }
        }

        private void SignatureSetupHospiceElectionAddendum()
        {
            if ((DynamicFormViewModel == null) ||
                (DynamicFormViewModel.CurrentForm == null) ||
                (DynamicFormViewModel.CurrentForm.IsHospiceElectionAddendum == false) ||
                (DynamicFormViewModel.PreviousEncounterStatus != (int)EncounterStatusType.Completed) ||
                (Encounter == null) ||
                (Encounter.EncounterHospiceElectionAddendum == null))
            {
                return;
            }

            CurrentEncounterHospiceElectionAddendum = Encounter.EncounterHospiceElectionAddendum.FirstOrDefault();
        }

        #endregion
    }

    public class SignatureFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            Signature s = new Signature(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
            };
            vm.SignatureQuestion = s;
            s.SignatureSetup();
            return s;
        }
    }
}