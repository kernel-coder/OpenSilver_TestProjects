#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Model;
using Virtuoso.Core.Utility;
using Virtuoso.Core.View;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Services
{
    public class OasisAuditError
    {
        public string Question { get; set; }
        public string ErrorParagraphText { get; set; }
    }

    public enum OasisType
    {
        TrackingSheet = 0,
        Filler = 1,
        HardCoded = 2,
        Text = 3,
        TextNAUK = 4,
        Date = 5,
        DateNAUK = 6,
        Radio = 7,
        CheckBoxHeader = 8,
        CheckBox = 9,
        CheckBoxExclusive = 10,
        ICD = 11,
        ICDMedical = 12,
        PatientName = 13,
        RadioHorizontal = 14,
        PressureUlcer = 15,
        Subfield = 16,
        LivingArrangement = 17,
        DepressionScreening = 18,
        PriorADL = 19,
        PoorMed = 20,
        CareManagement = 21,
        Synopsys = 22,
        RadioWithDate = 23,
        WoundDimension = 24,
        ICD10 = 25,
        ICD10Medical = 26,
        PressureUlcerWorse = 27,
        CareManagement_C1 = 28,
        HISTrackingSheet = 29,
        PressureUlcer_C1 = 30,
        PressureUlcer_C2 = 31,
        PressureUlcerWorse_C2 = 32,
        HeightWeight_C2 = 33,
        GG0170C_C2 = 34,
        ServiceUtilization_10 = 35,
        ServiceUtilization_30 = 36,
        ICD10MedicalV2 = 37,
        TopLegend = 38, // like CareManagement_C1 - GG0130 and GG0170  
        PressureUlcer_C3 = 39, //M1311
        LeftLegend = 40 // like CareManagement_C1 - GG0100 and J1900
    }

    public enum OasisMappingType
    {
        EverHavePain = 1,
        LivingArraggementsA = 2,
        LivingArraggementsB = 3,
        CodeLookupRadio = 4,
        CodeLookupRadioNull = 5,
        CodeLookupMultiCheck = 6,
        CodeLookupMultiCheckNull = 7,
        CodeLookupMultiRadio = 8,
        CodeLookupMultiRadioNullAll = 9,
        CodeLookupMultiRadioNull = 10,
        CodeLookupRadioTextNotNull = 11,
        Grooming = 12,
        Catheter = 13,
        ReceivedFrom = 14,
        Date = 15,
        CodeLookupCheckBoxPart = 16,
        ICD = 17,
        GreaterThan = 18,
        LessThan = 19,
        CodeLookupRadioText = 20,
        ICD10 = 21
    }

    public class OasisManagerQuestion : GalaSoft.MvvmLight.ViewModelBase
    {
        int __cleanupCount;

        public override void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            if (OasisManagerAnswers != null)
            {
                OasisManagerAnswers.ForEach(oma => oma.Cleanup());
                OasisManagerAnswers.Clear();
            }

            base.Cleanup();
        }

        public OasisManagerQuestion()
        {
        }

        public OasisManagerQuestion(OasisQuestion q, OasisManager m)
        {
            _OasisQuestion = q;
            _OasisManager = m;
            m.OasisManagerQuestions.Add(this);
        }

        private bool _hidden;

        public bool Hidden
        {
            get { return _hidden; }
            set
            {
                _hidden = value;
                if (_hidden)
                {
                    foreach (OasisAnswer oa in _OasisQuestion.OasisAnswer) OasisManager.ClearResponse(oa, true);
                    if (_OasisQuestion.IsType(OasisType.PressureUlcer))
                    {
                        OasisManager.RaiseIsWoundDimensionsVisibleChanged();
                    }
                }

                if (_OasisQuestion.Question.Equals("J1900"))
                {
                    OasisManager.ApplyJ1900DashSkipLogic(_OasisQuestion, _hidden);
                }

                base.RaisePropertyChanged("Hidden");
                if (OnHiddenChanged != null)
                {
                    OnHiddenChanged(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler OnHiddenChanged;

        public bool HiddenOverride
        {
            get
            {
                if (OasisManager == null)
                {
                    return _hidden;
                }

                if (OasisManager.CurrentEncounterOasis == null)
                {
                    return _hidden;
                }

                if (OasisManager.CurrentEncounterOasis.BypassFlag == null)
                {
                    return _hidden;
                }

                if (OasisManager.CurrentEncounterOasis.BypassFlag == true)
                {
                    return true;
                }

                return _hidden;
            }
        }

        private string _errorText;

        public string ErrorText
        {
            get { return _errorText; }
            set
            {
                _errorText = value;
                base.RaisePropertyChanged("ErrorText");
            }
        }

        public bool IsLookbackQuestion
        {
            get
            {
                // Note M2400/M2401 lookback is not handled at the question level - its handled at the answer level
                if (OasisQuestion == null)
                {
                    return false;
                }

                if (OasisManager == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(OasisQuestion.Question))
                {
                    return false;
                }

                return OasisManager.IsLookbackQuestion(OasisQuestion.Question);
            }
        }

        public bool IsLookbackQuestionM2400orM2401
        {
            get
            {
                if (OasisQuestion == null)
                {
                    return false;
                }

                if (OasisManager == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(OasisQuestion.Question))
                {
                    return false;
                }

                return OasisManager.IsLookbackQuestionM2400orM2401(OasisQuestion.Question);
            }
        }

        private OasisQuestion _OasisQuestion;
        public OasisQuestion OasisQuestion => _OasisQuestion;
        private OasisManager _OasisManager;
        public OasisManager OasisManager => _OasisManager;
        private List<OasisManagerAnswer> _OasisManagerAnswers = new List<OasisManagerAnswer>();

        public List<OasisManagerAnswer> OasisManagerAnswers
        {
            get { return _OasisManagerAnswers; }
            set { _OasisManagerAnswers = value; }
        }
    }

    public class OasisManagerAnswer : GalaSoft.MvvmLight.ViewModelBase
    {
        public OasisManagerAnswer(OasisAnswer a, OasisManager m, bool answerProtected)
        {
            _OasisAnswer = a;
            _OasisManager = m;
            // if Most Recent survey is inactive or mark not for transmit - override and protect everything
            if ((_OasisManager != null) && (_OasisManager.CurrentEncounter != null) &&
                (_OasisManager.CurrentEncounter.MostRecentEncounterOasis != null))
            {
                if (_OasisManager.CurrentEncounter.MostRecentEncounterOasis.InactiveDate != null)
                {
                    Protected = true;
                }
                else if (_OasisManager.CurrentEncounter.MostRecentEncounterOasis.REC_ID == "X1")
                {
                    Protected = true;
                }
                else
                {
                    Protected = answerProtected;
                }
            }
            else
            {
                Protected = answerProtected;
            }

            Messenger.Default.Register<OasisAnswer>(this,
                string.Format("OasisAnswerResponse{0}_{1}", a.CachedOasisLayout.CMSField.Trim(),
                    m.OasisManagerGuid.ToString().Trim()), s => OasisAnswerResponseChanged());
            Messenger.Default.Register<OasisAnswer>(this,
                string.Format("OasisAnswerResponse{0}_{1}", "AllCMSFields", m.OasisManagerGuid.ToString().Trim()),
                s => OasisAnswerResponseChanged());
        }

        public void OasisAnswerResponseChanged()
        {
            base.RaisePropertyChanged("ICDResponse");
            base.RaisePropertyChanged("A0245CheckBoxResponse");
            base.RaisePropertyChanged("CheckBoxResponse");
            base.RaisePropertyChanged("DateResponse");
            base.RaisePropertyChanged("RadioResponse");
            base.RaisePropertyChanged("TextResponse");
        }

        private OasisAnswer _OasisAnswer;
        public OasisAnswer OasisAnswer => _OasisAnswer;
        private OasisManagerAnswer _OasisManagerAnswerChildDate;

        public OasisManagerAnswer OasisManagerAnswerChildDate
        {
            get { return _OasisManagerAnswerChildDate; }
            set { _OasisManagerAnswerChildDate = value; }
        }

        private bool _ShowPHQsInstructions;

        public bool ShowPHQsInstructions
        {
            get { return _ShowPHQsInstructions; }
            set { _ShowPHQsInstructions = value; }
        }

        private List<OasisManagerAnswer> _OasisManagerAnswerPHQs;

        public List<OasisManagerAnswer> OasisManagerAnswerPHQs
        {
            get { return _OasisManagerAnswerPHQs; }
            set { _OasisManagerAnswerPHQs = value; }
        }

        private OasisManager _OasisManager;
        public OasisManager OasisManager => _OasisManager;

        public string OasisRadioGroupName => "OasisRadioGroupName" + OasisAnswer.OasisQuestionKey.ToString().Trim();

        public bool CheckBoxResponse
        {
            get { return OasisManager.GetCheckBoxResponse(OasisAnswer, false); }
            set { OasisManager.SetCheckBoxResponse(value, OasisAnswer); }
        }

        public bool A0245CheckBoxResponse
        {
            get { return OasisManager.GetA0245CheckBoxResponse(OasisAnswer, false); }
            set { OasisManager.SetA0245CheckBoxResponse(value, OasisAnswer); }
        }

        public DateTime? DateResponse
        {
            get { return OasisManager.GetDateResponse(OasisAnswer, false); }
            set { OasisManager.SetDateResponse(value, OasisAnswer); }
        }

        public string ICDResponse
        {
            get { return OasisManager.GetICDResponse(OasisAnswer, false); }
            set { OasisManager.SetICDResponse(value, OasisAnswer); }
        }

        public bool RadioResponse
        {
            get { return OasisManager.GetRadioResponse(OasisAnswer, false); }
            set { OasisManager.SetRadioResponse(value, OasisAnswer); }
        }

        public string TextResponse
        {
            get { return OasisManager.GetTextResponse(OasisAnswer, false); }
            set { OasisManager.SetTextResponse(value, OasisAnswer); }
        }

        public bool IsM2200 => (OasisAnswer.OasisQuestion.Question == "M2200");
        private bool _Protected;

        public bool Protected
        {
            get { return _Protected; }
            set
            {
                _Protected = value;
                RaisePropertyChanged("Protected");
            }
        }

        private bool _enabled = true;

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                base.RaisePropertyChanged("Enabled");
            }
        }

        public string AnswerLabel => (OasisAnswer == null) ? null : OasisAnswer.AnswerLabel;

        int __cleanupCount;

        public override void Cleanup()
        {
            ++__cleanupCount;
            
            if (__cleanupCount > 1)
            {
                return;
            }

            if (OasisManagerAnswerChildDate != null)
            {
                OasisManagerAnswerChildDate.Cleanup();
                OasisManagerAnswerChildDate = null;
            }

            if (OasisManagerAnswerPHQs != null)
            {
                OasisManagerAnswerPHQs.ForEach(ma => ma.Cleanup());
                OasisManagerAnswerPHQs.Clear();
                OasisManagerAnswerPHQs = null;
            }

            base.Cleanup();
        }
    }

    public class OasisManager : GenericBase
    {
        public string ConstructorTag { get; internal set; }
        private Guid _SessionID;

        private Guid SessionID
        {
            get
            {
                if (_SessionID == Guid.Empty)
                {
                    _SessionID = Guid.NewGuid();
                }

                return _SessionID;
            }
        }

        private static string A0245DASH = "-       ";
        private static string BIRTHDAYDASH = "--------";
        private static string OASIS_DASH = "-";
        private static string OASIS_SPACE = " ";
        private static string OASIS_ZERO = "0";
        private static string OASIS_ONE = "1";
        private static char OASIS_DASHCHAR = '-';
        private static string OASIS_EQUAL = "=";
        private static char OASIS_EQUALCHAR = '=';
        public Admission CurrentAdmission { get; set; }
        private Patient CurrentPatient { get; set; }
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

        private EncounterOasis _CurrentEncounterOasis;

        public EncounterOasis CurrentEncounterOasis
        {
            get { return _CurrentEncounterOasis; }
            set
            {
                _CurrentEncounterOasis = value;
                RaisePropertyChanged("CurrentEncounterOasis");
            }
        }

        public void OasisAnswerResponseChanged()
        {
            Messenger.Default.Send(new OasisAnswer(),
                string.Format("OasisAnswerResponse{0}_{1}", "AllCMSFields", OasisManagerGuid.ToString().Trim()));
        }

        public void SetBypassFlag(bool bypassFlag)
        {
            if (CurrentEncounterOasis == null)
            {
                return;
            }

            CurrentEncounterOasis.BypassFlag = bypassFlag;
            Messenger.Default.Send(((CurrentEncounterOasis.BypassFlag == true) ? true : false),
                string.Format("OasisBypassFlagChanged{0}", OasisManagerGuid.ToString().Trim()));
            OasisAlertCheckAllMeasures();
        }

        public void ForceBypassNTUC()
        {
            if (CurrentEncounterOasis == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentEncounterOasis.BypassReason))
            {
                CurrentEncounterOasis.BypassReason = "Patient not taken under care";
            }

            if (CurrentEncounterOasis.BypassFlag == false)
            {
                SetBypassFlag(true);
            }
        }

        public void ForceUnBypassNTUC()
        {
            if (CurrentEncounterOasis == null)
            {
                return;
            }

            if (CurrentEncounterOasis.BypassFlag == false)
            {
                return;
            }

            if (CurrentEncounterOasis.BypassReason != "Patient not taken under care")
            {
                return;
            }

            SetBypassFlag(false);
        }

        public void ForceBypassDischarge()
        {
            if (CurrentEncounterOasis == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentEncounterOasis.BypassReason))
            {
                CurrentEncounterOasis.BypassReason = "Other OASIS disciplines are currently active";
            }

            if (CurrentEncounterOasis.BypassFlag == false)
            {
                SetBypassFlag(true);
            }
        }

        public void ForceUnBypassDischarge()
        {
            if (CurrentEncounterOasis == null)
            {
                return;
            }

            if (CurrentEncounterOasis.BypassFlag == false)
            {
                return;
            }

            if (CurrentEncounterOasis.BypassReason.StartsWith("Other OASIS disciplines are currently") == false)
            {
                return;
            }

            SetBypassFlag(false);
        }

        public bool CurrentEncounterOasisIsBypassed
        {
            get
            {
                if (CurrentEncounterOasis == null)
                {
                    return true;
                }

                return (CurrentEncounterOasis.BypassFlag == true) ? true : false;
            }
        }

        public bool DoesInsuranceRequireRFAReview
        {
            get
            {
                if (CurrentPatient == null)
                {
                    return false;
                }

                if (CurrentEncounterOasis == null)
                {
                    return false;
                }

                if (CurrentEncounterOasis.BypassFlag == true)
                {
                    return false;
                }

                if (CurrentEncounterOasis.SYS_CDIsHospice)
                {
                    return true;
                }

                _m0090Date =
                    GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"));
                if (_m0090Date == null)
                {
                    _m0090Date = DateTime.Today.Date;
                }

                if (CurrentPatient.PatientInsurance == null)
                {
                    return false;
                }

                List<PatientInsurance> list = CurrentPatient.PatientInsurance
                    .Where(i => ((i.Inactive == false) && (i.HistoryKey == null) && (i.PatientInsuranceKey != 0) &&
                                 ((i.EffectiveFromDate.HasValue == false ||
                                   ((DateTime)i.EffectiveFromDate).Date <= _m0090Date) &&
                                  (i.EffectiveThruDate.HasValue == false ||
                                   ((DateTime)i.EffectiveThruDate).Date > _m0090Date))))
                    .OrderBy(i => i.InsuranceTypeKey).ToList();
                if ((list != null) && list.Any())
                {
                    foreach (PatientInsurance pi in list)
                        if (pi.IsOASISReviewRequiredForRFA(RFA))
                        {
                            return true;
                        }
                }

                return false;
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

        public string BestGuessCorrectionNumOASIS
        {
            get
            {
                if (CurrentEncounter == null)
                {
                    return "00";
                }

                if (CurrentEncounter.EncounterOasis == null)
                {
                    return "00";
                }

                EncounterOasis eo = CurrentEncounter.EncounterOasis.Where(e => e.CMSTransmission)
                    .OrderByDescending(e => e.AddedDate).FirstOrDefault();
                if (eo == null)
                {
                    return "00";
                }

                // Record has been transmitted - increment the correction number
                int c = 0;
                try
                {
                    c = Int32.Parse(GetResponseB1Record(
                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "CORRECTION_NUM"), eo.B1Record));
                }
                catch
                {
                }

                return String.Format("{0:00}", ++c);
            }
        }

        public string BestGuessCorrectionNumHIS
        {
            get
            {
                if (CurrentEncounter == null)
                {
                    return "00";
                }

                if (CurrentEncounter.EncounterOasis == null)
                {
                    return "00";
                }

                EncounterOasis eo = CurrentEncounter.EncounterOasis.Where(e => e.CMSTransmission)
                    .OrderByDescending(e => e.AddedDate).FirstOrDefault();
                if (eo == null)
                {
                    return "00";
                }

                // Record has been transmitted - increment the correction number
                int c = 0;
                try
                {
                    c = Int32.Parse(
                        GetResponseB1Record(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "CRCTN_NUM"),
                            eo.B1Record));
                }
                catch
                {
                }

                return String.Format("{0:00}", ++c);
            }
        }

        public EncounterOasis GetCurrentEncounterOasis()
        {
            if (CurrentEncounter == null)
            {
                return null;
            }

            if (CurrentEncounter.EncounterOasis == null)
            {
                return null;
            }

            if (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed == false)
            {
                return CurrentEncounter.EncounterOasis.Where(e => ((e.InactiveDate == null) && (e.REC_ID != "X1")))
                    .OrderBy(e => e.AddedDate).FirstOrDefault();
            }

            // re-edit - return most recent if have edit role - else return original
            if (CurrentEncounter.CanEditCompleteCMS)
            {
                return CurrentEncounter.EncounterOasis.Where(e => ((e.InactiveDate == null) && (e.REC_ID != "X1")))
                    .OrderByDescending(e => e.AddedDate).FirstOrDefault();
            }

            return CurrentEncounter.EncounterOasis.Where(e => ((e.InactiveDate == null) && (e.REC_ID != "X1")))
                .OrderBy(e => e.AddedDate).FirstOrDefault();
        }

        public Encounter MostRecentOasisEncounter
        {
            get
            {
                if ((CurrentAdmission == null) || (CurrentAdmission.Encounter == null))
                {
                    return null;
                }

                return CurrentAdmission.Encounter
                    .Where(eo =>
                        ((eo.EncounterOasisRFA != null) && (eo.EncounterOasisM0090 != null) &&
                         (eo.EncounterKey != CurrentEncounter.EncounterKey) && eo.IsEncounterOasisActive))
                    .OrderByDescending(eo => eo.EncounterOasisM0090).FirstOrDefault();
            }
        }

        public Encounter MostRecentSOCROCOasisEncounter
        {
            get
            {
                if ((CurrentAdmission == null) || (CurrentAdmission.Encounter == null))
                {
                    return null;
                }

                return CurrentAdmission.Encounter
                    .Where(eo => ((eo.EncounterOasisRFA != null) &&
                                  ((eo.EncounterOasisRFA == "01") || (eo.EncounterOasisRFA == "03")) &&
                                  (eo.EncounterOasisM0090 != null) &&
                                  (eo.EncounterKey != CurrentEncounter.EncounterKey) && eo.IsEncounterOasisActive))
                    .OrderByDescending(eo => eo.EncounterOasisM0090).FirstOrDefault();
            }
        }

        public Encounter MostRecentOasisEncounterLookback =>
            (IsOASISVersionC2orHigher) ? MostRecentSOCROCOasisEncounter : MostRecentOasisEncounter;

        public DateTime? StartDateLookback =>
            (MostRecentOasisEncounterLookback == null || MostRecentOasisEncounterLookback.EncounterOasisM0090 == null)
                ? null
                : MostRecentOasisEncounterLookback.EncounterOasisM0090;

        public DateTime? EndDateLookback => (CurrentEncounterOasis == null) ? DateTime.Today :
            (CurrentEncounterOasis.M0090 == null) ? DateTime.Today : CurrentEncounterOasis.M0090;

        private string HCFACode { get; set; }

        void OasisManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentAdmission")
            {
                AdmissionPhysician.Admission = CurrentAdmission;
            }

            if (e.PropertyName == "CurrentEncounter")
            {
                AdmissionPhysician.Encounter = CurrentEncounter;
            }
        }

        public AdmissionPhysicianFacade AdmissionPhysician { get; internal set; }

        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                base.RaisePropertyChanged("IsBusy");
            }
        }

        public bool HiddenOverride
        {
            get
            {
                if (CurrentEncounterOasis == null)
                {
                    return false;
                }

                if (CurrentEncounterOasis.BypassFlag == null)
                {
                    return false;
                }

                return (bool)CurrentEncounterOasis.BypassFlag;
            }
        }

        private int _PreviousEncounterStatus = (int)EncounterStatusType.None;

        public int PreviousEncounterStatus
        {
            get { return _PreviousEncounterStatus; }
            set
            {
                _PreviousEncounterStatus = value;
                base.RaisePropertyChanged("PreviousEncounterStatus");
                base.RaisePropertyChanged("MappingAllowedClinicianBypassOASISAssist");
                base.RaisePropertyChanged("MappingAllowedClinician");
                base.RaisePropertyChanged("MappingAllowedClinicianOrICDCoder");
                base.RaisePropertyChanged("MappingAllowedClinicianOrICDCoderBypassOASISAssist");
                base.RaisePropertyChanged("MappingAllowedClinicianOrOasisCoordinatorReEdit");
                base.RaisePropertyChanged("MappingAllowedClinicianOrOasisCoordinatorReEditBypassOASISAssist");
            }
        }

        public bool MappingAllowedClinicianBypassOASISAssist
        {
            get
            {
                if ((CurrentEncounter.EncounterBy == WebContext.Current.User.MemberID) &&
                    ((PreviousEncounterStatus == (int)EncounterStatusType.Edit) ||
                     (PreviousEncounterStatus == (int)EncounterStatusType.CoderReviewEdit) ||
                     (PreviousEncounterStatus == (int)EncounterStatusType.OASISReviewEdit) ||
                     (PreviousEncounterStatus == (int)EncounterStatusType.OASISReviewEditRR)))
                {
                    // generally - only the owing clinician can make changes to the encounter that forward (map) to the attached survey
                    return true;
                }

                return false;
            }
        }

        public bool MappingAllowedClinician
        {
            get
            {
                if (TenantSettingsCache.Current.TenantSettingOASISAssist == false)
                {
                    return false;
                }

                return MappingAllowedClinicianBypassOASISAssist;
            }
        }

        public bool MappingAllowedClinicianOrICDCoder
        {
            get
            {
                if (TenantSettingsCache.Current.TenantSettingOASISAssist == false)
                {
                    return false;
                }

                return MappingAllowedClinicianOrICDCoderBypassOASISAssist;
            }
        }

        public bool MappingAllowedClinicianOrICDCoderBypassOASISAssist
        {
            get
            {
                if (MappingAllowedClinicianBypassOASISAssist)
                {
                    return true;
                }

                // one override allowed - if in code review and user has CodeReview role - allow the mapping
                if ((PreviousEncounterStatus == (int)EncounterStatusType.CoderReview) &&
                    RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false))
                {
                    return true;
                }

                return false;
            }
        }

        public bool MappingAllowedClinicianOrOasisCoordinatorReEditBypassOASISAssist
        {
            get
            {
                if (MappingAllowedClinicianBypassOASISAssist)
                {
                    return true;
                }

                // two override allowed
                if (CurrentEncounter == null)
                {
                    return false;
                }

                // 1) oasis reviews when can edit survey
                if (CurrentEncounter.CMSCoordinatorCanEdit)
                {
                    return true;
                }

                // 2) if General re-edit allow the mapping
                if ((PreviousEncounterStatus == (int)EncounterStatusType.Completed) &&
                    (CurrentEncounter.CanEditCompleteCMS))
                {
                    return true;
                }

                return false;
            }
        }

        public bool MappingAllowedClinicianOrOasisCoordinatorReEdit
        {
            get
            {
                if (TenantSettingsCache.Current.TenantSettingOASISAssist == false)
                {
                    return false;
                }

                return MappingAllowedClinicianOrOasisCoordinatorReEditBypassOASISAssist;
            }
        }

        public string RFA
        {
            get
            {
                if (CurrentEncounterOasis == null)
                {
                    return null;
                }

                OasisAnswer a = OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey,
                    ((CurrentEncounterOasis.SYS_CDIsHospice) ? "A0250" : "M0100_ASSMT_REASON"), 1);
                if (a == null)
                {
                    return "01";
                }

                return CurrentEncounterOasis.B1Record
                    .Substring(a.CachedOasisLayout.StartPos - 1, a.CachedOasisLayout.Length).Trim();
            }
            set
            {
                if ((RFA != value))
                {
                    // Assume 0n format
                    if (CurrentEncounterOasis == null)
                    {
                        return;
                    }

                    int rfa = 1;
                    try
                    {
                        rfa = Int32.Parse(value);
                    }
                    catch
                    {
                    }

                    if ((CurrentEncounterOasis.SYS_CDIsHospice) && (rfa == 9))
                    {
                        rfa = 2;
                    }

                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey,
                            ((CurrentEncounterOasis.SYS_CDIsHospice) ? "A0250" : "M0100_ASSMT_REASON"), rfa));
                    RaisePropertyChanged("IsTransfer");
                }
            }
        }

        public bool IsTransfer => ((RFA == "06") || (RFA == "07")) ? true : false;

        private int _OasisVersionKey;

        public int OasisVersionKey
        {
            get { return _OasisVersionKey; }
            set
            {
                _OasisVersionKey = value;
                RaisePropertyChanged("IsOASISVersionC2orHigher");
                RaisePropertyChanged("IsHISVersion2orHigher");
            }
        }

        public bool IsOASISVersionC2orHigher
        {
            get
            {
                if (string.IsNullOrWhiteSpace(VersionCD2))
                {
                    return false;
                }

                if ((VersionCD2 == "2.00") || (VersionCD2 == "02.00") ||
                    (VersionCD2 == "2.10") || (VersionCD2 == "02.10") ||
                    (VersionCD2 == "2.11") || (VersionCD2 == "02.11") ||
                    (VersionCD2 == "2.12") || (VersionCD2 == "02.12"))
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsOASISVersionD1orHigher
        {
            get
            {
                if (string.IsNullOrWhiteSpace(VersionCD2))
                {
                    return false;
                }

                if ((VersionCD2 == "2.00") || (VersionCD2 == "02.00") ||
                    (VersionCD2 == "2.10") || (VersionCD2 == "02.10") ||
                    (VersionCD2 == "2.11") || (VersionCD2 == "02.11") ||
                    (VersionCD2 == "2.12") || (VersionCD2 == "02.12") ||
                    (VersionCD2 == "2.20") || (VersionCD2 == "02.20") ||
                    (VersionCD2 == "2.21") || (VersionCD2 == "02.21") ||
                    (VersionCD2 == "2.30") || (VersionCD2 == "02.30"))
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsHISVersion2orHigher
        {
            get
            {
                if (string.IsNullOrWhiteSpace(VersionCD2))
                {
                    return false;
                }

                if (VersionCD2 == "1.00")
                {
                    return false;
                }

                return true;
            }
        }

        private bool _OasisVersionUsingICD10;

        public bool OasisVersionUsingICD10
        {
            get { return _OasisVersionUsingICD10; }
            set { _OasisVersionUsingICD10 = value; }
        }

        private string VersionCD2 { get; set; }
        public IDynamicFormService FormModel { get; set; }
        private bool _BypassMapping;

        public bool BypassMapping
        {
            get { return _BypassMapping; }
            set { _BypassMapping = value; }
        }

        public void SetupMessaging()
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            Messenger.Default.Register<Patient>(this, "OasisSetupPatientDefaults",
                p => SetupPatientDefaults2(p, false));
            Messenger.Default.Register<int>(this, "SetupAdmissionOasisHeaderDefaults",
                admissionKey => SetupAdmissionOasisHeaderDefaultsOASIS(admissionKey));
            Messenger.Default.Register<Admission>(this, "OasisSetupAdmissionDefaults",
                a => SetupAdmissionDefaults(a, false, false));
            Messenger.Default.Register<AdmissionDiscipline>(this,
                string.Format("OasisAdmissionDisciplineChanged{0}", CurrentAdmission.AdmissionKey.ToString().Trim()),
                a => SetupAdmissionDisciplineChanged(a));
            Messenger.Default.Register<EncounterResumption>(this, "OasisEncounterResumptionChanged",
                er => SetupEncounterResumptionDefaults(er));
            Messenger.Default.Register<Admission>(this,
                string.Format("OasisDiagnosisChanged{0}", CurrentAdmission.AdmissionKey.ToString().Trim()),
                a => SetupAdmissionDiagnosis(a));
            Messenger.Default.Register<Patient>(this,
                string.Format("OasisMedicationChanged{0}", CurrentAdmission.PatientKey.ToString().Trim()),
                p => SetupPatientMedication(p));
            Messenger.Default.Register<Admission>(this,
                string.Format("OasisPainLocationChanged{0}", CurrentAdmission.AdmissionKey.ToString().Trim()),
                a => SetupAdmissionPainLocation(a));
            Messenger.Default.Register<Admission>(this,
                string.Format("OasisWoundSiteChanged{0}", CurrentAdmission.AdmissionKey.ToString().Trim()),
                a => SetupAdmissionWoundSite(a));
            Messenger.Default.Register<EncounterPain>(this,
                string.Format("OasisEncounterPainScoreChanged{0}", CurrentAdmission.AdmissionKey.ToString().Trim()),
                ep => SetupEncounterPain(ep));
            Messenger.Default.Register<EncounterTransfer>(this, "OasisEncounterTransferChanged",
                ep => SetupEncounterTransfer(ep));
        }

        private DateTime EffectiveDate(string sys_cd, string rfa)
        {
            if ((string.IsNullOrWhiteSpace(sys_cd) == false) && (sys_cd.Trim().ToUpper() == "HOSPICE"))
            {
                return (CurrentAdmission == null) ? DateTime.Today.Date : CurrentAdmission.HISTargetDate(rfa);
            }

            DateTimeOffset? effectiveDateOffset =
                (CurrentAdmission == null) ? null : CurrentEncounter.EncounterOrTaskStartDateAndTime;
            return (effectiveDateOffset == null) ? DateTime.Today.Date : ((DateTimeOffset)effectiveDateOffset).Date;
        }

        public void StartNewOasis(string sys_cd, string rfa, bool CurrentFormIsOasis)
        {
            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentAdmission == null)
            {
                return;
            }

            OasisVersion ov = OasisCache.GetOasisVersionBySYSCDandEffectiveDate(sys_cd, EffectiveDate(sys_cd, rfa));
            if (ov == null)
            {
                return;
            }

            OasisVersionKey = ov.OasisVersionKey;
            OasisVersionUsingICD10 = ov.UsingICD10;
            VersionCD2 = ov.VersionCD2;
            EncounterOasis newEncounterOasis = new EncounterOasis
            {
                SYS_CD = sys_cd, BypassFlag = false, OasisVersionKey = OasisVersionKey,
                B1Record = new String(' ', GetB1RecordLength(OasisVersionKey)), OnHold = false,
                HavenValidationErrors = false, MedicareOrMedicaid = false, UpdatedDate = DateTime.UtcNow,
                AddedDate = DateTime.UtcNow
            };
            newEncounterOasis.RFA = rfa;
            CurrentEncounter.SYS_CD = sys_cd;
            CurrentEncounter.EncounterOasis.Add(newEncounterOasis);
            CurrentEncounterOasis = newEncounterOasis;
            RFA = rfa;
            if ((CurrentEncounterOasis.RFA == "05") && (CurrentFormIsOasis == false))
            {
                CurrentEncounterOasis.BypassFlag = true;
                CurrentEncounterOasis.BypassReason = "Do not perform Other Follow-up OASIS";
            }
        }

        private int GetB1RecordLength(int OasisVersionKey)
        {
            return OasisCache.GetOasisLayoutMaxEndPos(OasisVersionKey);
        }

        public EncounterOasis StartNewOasisEdit()
        {
            EncounterOasis currentEncounterOasis = GetCurrentEncounterOasis();

            if (CurrentEncounter == null)
            {
                return currentEncounterOasis;
            }

            if (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed == false)
            {
                return currentEncounterOasis;
            }

            if (CurrentEncounter.MostRecentEncounterOasis != null)
            {
                // if inactive or marked not transmit - don't start a new one
                if (CurrentEncounter.MostRecentEncounterOasis.InactiveDate != null)
                {
                    return currentEncounterOasis;
                }

                ;
                if (CurrentEncounter.MostRecentEncounterOasis.REC_ID == "X1")
                {
                    return currentEncounterOasis;
                }

                ;
            }

            // if not a coordinator - cannot edit an oasis- so don't create a new version
            if (CurrentEncounter.CanEditCompleteCMS == false)
            {
                return currentEncounterOasis;
            }

            // Encounter is complete and we have a user with role doing the edit - create a new version if not bypassed 
            if (GetCurrentEncounterOasis().BypassFlag == true)
            {
                return currentEncounterOasis;
            }

            EncounterOasis newEncounterOasis = (EncounterOasis)Clone(currentEncounterOasis);
            newEncounterOasis.EncounterOasisKey = 0;
            if (newEncounterOasis.CMSTransmission)
            {
                newEncounterOasis.CMSTransmission = false;
                if (newEncounterOasis.SYS_CDIsHospice)
                {
                    string bestGuessCorrectionNumHIS = BestGuessCorrectionNumHIS;
                    SetResponse(bestGuessCorrectionNumHIS,
                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "CRCTN_NUM"), newEncounterOasis);
                    SetResponse(((bestGuessCorrectionNumHIS == "00") ? "1" : "2"),
                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0050"), newEncounterOasis);
                }
                else
                {
                    string bestGuessCorrectionNumOASIS = BestGuessCorrectionNumOASIS;
                    SetResponse(bestGuessCorrectionNumOASIS,
                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "CORRECTION_NUM"), newEncounterOasis);
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "TRANS_TYPE_CD"))
                    {
                        SetResponse(((bestGuessCorrectionNumOASIS == "00") ? "1" : "2"),
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "TRANS_TYPE_CD"), newEncounterOasis);
                    }
                }
            }

            // Inherit OnHold and Haven error status
            newEncounterOasis.EncounterKey = currentEncounterOasis.EncounterKey;
            newEncounterOasis.OnHold = currentEncounterOasis.OnHold;
            newEncounterOasis.HavenValidationErrors = currentEncounterOasis.HavenValidationErrors;
            newEncounterOasis.HavenValidationErrorsComment = currentEncounterOasis.HavenValidationErrorsComment;
            newEncounterOasis.InactiveDate = null;
            newEncounterOasis.InactiveBy = null;
            newEncounterOasis.UpdatedDate = DateTime.UtcNow;
            newEncounterOasis.AddedDate = newEncounterOasis.AddedDate.AddSeconds(1); // just so it is most recent
            CurrentEncounter.EncounterOasis.Add(newEncounterOasis);
            return newEncounterOasis;
        }

        public void ClearHavenErrors()
        {
            if (CurrentEncounterOasis == null)
            {
                return;
            }

            CurrentEncounterOasis.OnHold = false;
            CurrentEncounterOasis.HavenValidationErrors = false;
        }

        public string GetPPSModelVersion
        {
            get
            {
                if (CurrentEncounterOasis == null)
                {
                    return null;
                }

                return CurrentEncounterOasis.PPSModelVersion;
            }
        }

        public void SetPPSModelVersion()
        {
            if (CurrentEncounterOasis == null)
            {
                return;
            }

            CurrentEncounterOasis.DrivingPatientInsuranceKey = null;
            CurrentEncounterOasis.PPSModelVersion = null;
            if ((CurrentAdmission == null) || (CurrentPatient?.PatientInsurance == null))
            {
                return;
            }

            CurrentEncounterOasis.DrivingPatientInsuranceKey = CurrentAdmission.PatientInsuranceKey;
            PatientInsurance pi = CurrentPatient.PatientInsurance
                .Where(p => p.PatientInsuranceKey == CurrentEncounterOasis.DrivingPatientInsuranceKey).FirstOrDefault();
            if (pi == null)
            {
                return;
            }

            // Pecking order: Check for PPSModelVersion 3, then PDGMHIPPS, then default to PPSModelVersion 2
            if (InsuranceCache.IsInsurancePDGM((int)pi.InsuranceKey, CurrentEncounterOasis.M0090))
            {
                CurrentEncounterOasis.PPSModelVersion =
                    InsuranceCache.GetPPSModelVersion((int)pi.InsuranceKey, CurrentEncounterOasis.M0090);
            }
            else if (InsuranceCache.IsInsurancePDGMHIPPS((int)pi.InsuranceKey, CurrentEncounterOasis.M0090))
            {
                CurrentEncounterOasis.PPSModelVersion = "PDGMHIPPS";
            }
            else
            {
                CurrentEncounterOasis.PPSModelVersion = "2";
            }
        }

        public void OKProcessing(bool isAllValid, bool isOnLine)
        {
            if ((CurrentEncounter == null) || (CurrentEncounter.EncounterOasis == null))
            {
                return;
            }

            foreach (EncounterOasis eo in CurrentEncounter.EncounterOasis)
                if (eo.HasChanges || eo.IsNew || ((eo.Superceeded == false) &&
                                                  (eo.EncounterStatus != CurrentEncounter.EncounterStatus)))
                {
                    eo.EncounterStatus = CurrentEncounter.EncounterStatus;
                }

            CheckOKProcessingGoingToOASISReview();
            CheckOKProcessingGoingToComplete(isAllValid, isOnLine);
            CheckOKProcessingPostComplete();
        }

        private void CheckOKProcessingGoingToOASISReview()
        {
            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.PreviousEncounterStatus == (int)EncounterStatusType.OASISReview)
            {
                return;
            }

            if (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.OASISReview)
            {
                return;
            }

            if (CurrentEncounterOasis == null)
            {
                return;
            }

            if (CurrentEncounterOasis.BypassFlag == true)
            {
                return;
            }

            CurrentEncounterOasis.PreReviewRecord = CurrentEncounterOasis.B1Record;
        }

        private void CheckOKProcessingGoingToComplete(bool isAllValid, bool isOnLine)
        {
            if (isAllValid == false)
            {
                return;
            }

            if (isOnLine == false)
            {
                return;
            }

            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.IsUnfulfilledOtherFollowUpOasis == false)
            {
                return;
            }

            if (CurrentEncounter.PreviousEncounterStatus == CurrentEncounter.EncounterStatus)
            {
                return;
            }

            if (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed)
            {
                return;
            }

            if (CurrentEncounterOasis == null)
            {
                return;
            }

            if (FormModel == null)
            {
                return;
            }

            FormModel.RemoveEncounterOasis(CurrentEncounterOasis);
        }

        private void CheckOKProcessingPostComplete()
        {
            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed)
            {
                return;
            }

            if (CurrentEncounter.CanEditCompleteCMS == false)
            {
                return;
            }

            // Encounter is complete and we have a user with role doing the edit - check new version if not bypassed 
            if (CurrentEncounterOasis == null)
            {
                return;
            }

            if (CurrentEncounterOasis.BypassFlag == true)
            {
                return;
            }

            List<EncounterOasis> eoList = CurrentEncounter.EncounterOasis.OrderByDescending(e => e.AddedDate).ToList();
            if (eoList == null)
            {
                return;
            }

            if (eoList.Count < 2)
            {
                return;
            }

            if (eoList[0].EncounterOasisKey > 0)
            {
                return;
            }

            if (FormModel == null)
            {
                return;
            }

            if ((eoList[0].B1Record == eoList[1].B1Record) && (eoList[0].OnHold == eoList[1].OnHold) &&
                (eoList[0].HavenValidationErrors == eoList[1].HavenValidationErrors) &&
                (eoList[0].HavenValidationErrorsComment == eoList[1].HavenValidationErrorsComment))
            {
                // no changes to the survey - don't create a new row
                FormModel.RemoveEncounterOasis(eoList[0]);
            }
            else
            {
                // survey changes - we're creating a new row - so superceed all the old ones - but potentially the new X1 for key change
                foreach (EncounterOasis eo in CurrentEncounter.EncounterOasis)
                    if ((eo.Superceeded == false) && (eo != eoList[0]))
                    {
                        if (((eo.REC_ID == "X1") && (eo.X1ForKeyChange = true) && (eo.CMSTransmission == false)) ==
                            false)
                        {
                            eo.Superceeded = true;
                        }
                    }
            }
        }

        public EncounterOasis CloneMostRecentEncounterOasisX1OASIS(Encounter encounter,
            EncounterOasis newEncounterOasis)
        {
            string bestGuessCorrectionNumOASIS = BestGuessCorrectionNumOASIS; // this will increment the inactivation
            EncounterOasis currEncounterOasis = encounter.MostRecentEncounterOasis;
            if (currEncounterOasis == null)
            {
                return null;
            }

            if (currEncounterOasis.SYS_CDIsHospice)
            {
                return null;
            }

            newEncounterOasis.REC_ID = "X1";
            newEncounterOasis.EncounterOasisKey = 0;
            newEncounterOasis.Superceeded = false;
            newEncounterOasis.InactiveDate = null;
            newEncounterOasis.InactiveBy = null;
            newEncounterOasis.BypassFlag = false;
            newEncounterOasis.CMSTransmission = false;
            newEncounterOasis.OnHold = false;
            newEncounterOasis.HavenValidationErrors = false;

            newEncounterOasis.B1Record = new String(' ', GetB1RecordLength(currEncounterOasis.OasisVersionKey));
            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "REC_ID"))
            {
                SetResponse("X1", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "REC_ID"), newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "HHA_AGENCY_ID"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "HHA_AGENCY_ID"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "MASK_VERSION_CD"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "MASK_VERSION_CD"),
                    currEncounterOasis, newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD1"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD1"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD2"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD2"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFTW_ID"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTW_ID"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFT_VER"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFT_VER"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "DATA_END"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "DATA_END"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "DATA_END_INDICATOR"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "DATA_END_INDICATOR"),
                    currEncounterOasis, newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "CRG_RTN"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "CRG_RTN"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "LN_FD"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "LN_FD"), currEncounterOasis,
                    newEncounterOasis);
            }

            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0030_START_CARE_DT"),
                currEncounterOasis, newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0032_ROC_DT"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0032_ROC_DT_NA"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0040_PAT_FNAME"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0040_PAT_LNAME"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0064_SSN"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0064_SSN_UK"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0066_PAT_BIRTH_DT"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0069_PAT_GENDER"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"),
                currEncounterOasis, newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0100_ASSMT_REASON"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0906_DC_TRAN_DTH_DT"),
                currEncounterOasis, newEncounterOasis);

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "ASMT_SYS_CD"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ASMT_SYS_CD"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "TRANS_TYPE_CD"))
            {
                SetResponse("3", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "TRANS_TYPE_CD"),
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "ITM_SBST_CD"))
            {
                SetResponse(newEncounterOasis.ITM_SBST_CD,
                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SBST_CD"), newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "ITM_SET_VRSN_CD"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SET_VRSN_CD"),
                    currEncounterOasis, newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SPEC_VRSN_CD"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SPEC_VRSN_CD"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "CORRECTION_NUM"))
            {
                SetResponse(bestGuessCorrectionNumOASIS,
                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "CORRECTION_NUM"), newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "STATE_CD"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "STATE_CD"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M0014_BRANCH_STATE"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0014_BRANCH_STATE"),
                    currEncounterOasis,
                    newEncounterOasis); // we don't need it - but we default the STATE_CD from here if need be
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "NATL_PRVDR_ID"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "NATL_PRVDR_ID"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFW_ID"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFW_ID"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFW_NAME"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFW_NAME"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFW_EMAIL_ADR"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFW_EMAIL_ADR"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFW_PROD_NAME"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFW_PROD_NAME"), currEncounterOasis,
                    newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFW_PROD_VRSN_CD"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFW_PROD_VRSN_CD"),
                    currEncounterOasis, newEncounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "ACY_DOC_CD"))
            {
                CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ACY_DOC_CD"), currEncounterOasis,
                    newEncounterOasis);
            }

            return newEncounterOasis;
        }

        public EncounterOasis CloneMostRecentEncounterOasisX1HIS(Encounter encounter, EncounterOasis newEncounterOasis)
        {
            EncounterOasis currEncounterOasis = encounter.MostRecentEncounterOasis;
            if (currEncounterOasis == null)
            {
                return null;
            }

            if (currEncounterOasis.SYS_CDIsHospice == false)
            {
                return null;
            }

            newEncounterOasis.REC_ID = "X1";
            newEncounterOasis.EncounterOasisKey = 0;
            newEncounterOasis.Superceeded = false;
            newEncounterOasis.InactiveDate = null;
            newEncounterOasis.InactiveBy = null;
            newEncounterOasis.BypassFlag = false;
            newEncounterOasis.CMSTransmission = false;
            newEncounterOasis.OnHold = false;
            newEncounterOasis.HavenValidationErrors = false;

            newEncounterOasis.B1Record = new String(' ', GetB1RecordLength(currEncounterOasis.OasisVersionKey));
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SET_SYS_CD"), currEncounterOasis,
                newEncounterOasis);
            SetResponse(newEncounterOasis.ITM_SBST_CD,
                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SBST_CD"), newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SET_VRSN_CD"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SPEC_VRSN_CD"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "CRCTN_NUM"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "STATE_CD"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "FAC_ID"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_VNDR_ID"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_VNDR_NAME"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_VNDR_EMAIL_ADR"),
                currEncounterOasis, newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_PROD_NAME"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_PROD_VRSN_CD"), currEncounterOasis,
                newEncounterOasis);
            SetResponse("3", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0050"), newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0220"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0250"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0270"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0500A"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0500C"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0600A"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0800"), currEncounterOasis,
                newEncounterOasis);
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0900"), currEncounterOasis,
                newEncounterOasis);
            newEncounterOasis.Z0500A = WebContext.Current.User.MemberID; //Keep Z0500A the same ??
            CopyResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "Z0500B"), currEncounterOasis,
                newEncounterOasis);
            return newEncounterOasis;
        }

        private string CR = "<LineBreak />";
        private string currB1;
        private string prevB1;
        private string currQuestion;
        private string prevQuestion;
        private string currResp;
        private string prevResp;
        private string oasisAddendum;

        private void OasisAddendumCheckQuesionForChange()
        {
            if (currQuestion != prevQuestion)
            {
                oasisAddendum = oasisAddendum + currQuestion + CR;
                prevQuestion = currQuestion;
            }
        }

        private void OasisAddendumCheckCheckForChange(OasisManagerQuestion omq, bool errorIfNotFound = true)
        {
            bool found = false;
            foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                switch ((OasisType)oamc.OasisAnswer.CachedOasisLayout.Type)
                {
                    case OasisType.CheckBox:
                    case OasisType.CheckBoxExclusive:
                        found = true;
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            // Ignore the 'dash' and '=' fields
                            if ((oamc.OasisAnswer.CachedOasisLayout.CMSField != "M1028_ACTV_DIAG_DASH1") &&
                                (oamc.OasisAnswer.CachedOasisLayout.CMSField != "M1028_ACTV_DIAG_DASH2") &&
                                (oamc.OasisAnswer.CachedOasisLayout.CMSField != "GG0110DASH") &&
                                (oamc.OasisAnswer.CachedOasisLayout.CMSField != "M1030_IGNORE_EQUAL") &&
                                (oamc.OasisAnswer.CachedOasisLayout.CMSField != "M2200_THER_NEED_IGNORE_EQUAL"))
                            {
                                currResp = GetResponseB1Record(oamc.OasisAnswer.CachedOasisLayout, currB1);
                                prevResp = GetResponseB1Record(oamc.OasisAnswer.CachedOasisLayout, prevB1);
                                // Ignore unless there was a check (one) involved e.g., ingore zero to dash, zero to blank, dash to zero, dash to blank, blank to zero, blank to dash
                                bool ignoreChange = ((prevResp != OASIS_ONE) && (currResp != OASIS_ONE));
                                if ((currResp != prevResp) && (ignoreChange == false))
                                {
                                    OasisAddendumCheckQuesionForChange();
                                    oasisAddendum = oasisAddendum + "- " +
                                                    ((currResp == "1") ? "Checked:" : "Unchecked:") + "  " +
                                                    oamc.OasisAnswer.AnswerLabel + " - " +
                                                    OasisAddendumStripRichText(oamc.OasisAnswer.AnswerText) + CR;
                                }
                            }
                            else
                            {
                                // special dash check
                                if ((oamc.OasisAnswer.CachedOasisLayout.CMSField == "M1028_ACTV_DIAG_DASH1") ||
                                    (oamc.OasisAnswer.CachedOasisLayout.CMSField == "GG0110DASH"))
                                {
                                    currResp = GetResponseB1Record(oamc.OasisAnswer.CachedOasisLayout, currB1);
                                    if (currResp != null)
                                    {
                                        currResp = currResp.Substring(0, 1);
                                    }

                                    prevResp = GetResponseB1Record(oamc.OasisAnswer.CachedOasisLayout, prevB1);
                                    if (prevResp != null)
                                    {
                                        prevResp = prevResp.Substring(0, 1);
                                    }

                                    if ((currResp != null) && (prevResp != null))
                                    {
                                        if (((prevResp != OASIS_DASH) && (currResp == OASIS_DASH)) ||
                                            ((prevResp == OASIS_DASH) && (currResp != OASIS_DASH)))
                                        {
                                            oasisAddendum = oasisAddendum + "- " +
                                                            ((currResp == OASIS_DASH) ? "Checked:" : "Unchecked:") +
                                                            "  " + oamc.OasisAnswer.AnswerLabel + " - " +
                                                            OasisAddendumStripRichText(oamc.OasisAnswer.AnswerText) +
                                                            CR;
                                        }
                                    }
                                }
                                // special '=' check
                                else if ((oamc.OasisAnswer.CachedOasisLayout.CMSField == "M1030_IGNORE_EQUAL") ||
                                         (oamc.OasisAnswer.CachedOasisLayout.CMSField ==
                                          "M2200_THER_NEED_IGNORE_EQUAL"))
                                {
                                    currResp = GetResponseB1Record(oamc.OasisAnswer.CachedOasisLayout, currB1);
                                    if (currResp != null)
                                    {
                                        currResp = currResp.Substring(0, 1);
                                    }

                                    prevResp = GetResponseB1Record(oamc.OasisAnswer.CachedOasisLayout, prevB1);
                                    if (prevResp != null)
                                    {
                                        prevResp = prevResp.Substring(0, 1);
                                    }

                                    if ((currResp != null) && (prevResp != null))
                                    {
                                        if (((prevResp != OASIS_EQUAL) && (currResp == OASIS_EQUAL)) ||
                                            ((prevResp == OASIS_EQUAL) && (currResp != OASIS_EQUAL)))
                                        {
                                            oasisAddendum = oasisAddendum + "- " +
                                                            ((currResp == OASIS_EQUAL) ? "Checked:" : "Unchecked:") +
                                                            "  " + oamc.OasisAnswer.AnswerLabel + " - " +
                                                            OasisAddendumStripRichText(oamc.OasisAnswer.AnswerText) +
                                                            CR;
                                        }
                                    }
                                }
                            }
                        }

                        break;
                }

            if (found == false)
            {
                if (errorIfNotFound)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisManager.OasisAddendumCheckCheckForChange: {0} is not a valid question.  Contact your system administrator.",
                        omq.OasisQuestion.Question));
                }
            }
        }

        private void OasisAddendumCheckCheckForChange(OasisAnswer oa)
        {
            if ((oa != null) && (oa.CachedOasisLayout != null))
            {
                switch ((OasisType)oa.CachedOasisLayout.Type)
                {
                    case OasisType.CheckBox:
                    case OasisType.CheckBoxExclusive:
                        currResp = GetResponseB1Record(oa.CachedOasisLayout, currB1);
                        prevResp = GetResponseB1Record(oa.CachedOasisLayout, prevB1);
                        if (currResp != prevResp)
                        {
                            OasisAddendumCheckQuesionForChange();
                            oasisAddendum = oasisAddendum + "- " + ((currResp == "1") ? "Checked:" : "Unchecked:") +
                                            "  " + oa.AnswerLabel + " - " + OasisAddendumStripRichText(oa.AnswerText) +
                                            CR;
                        }

                        break;
                }
            }
        }

        private void OasisAddendumCheckRadioForChange(OasisManagerQuestion omq, bool errorIfNotFound = true,
            OasisManagerAnswer overridwOamc = null)
        {
            OasisManagerAnswer oamc = null;
            if (overridwOamc == null)
            {
                oamc = omq.OasisManagerAnswers.Where(a =>
                    (((OasisType)a.OasisAnswer.CachedOasisLayout.Type == OasisType.Radio) ||
                     ((OasisType)a.OasisAnswer.CachedOasisLayout.Type == OasisType.RadioHorizontal) ||
                     ((OasisType)a.OasisAnswer.CachedOasisLayout.Type == OasisType.RadioWithDate)) ||
                    ((OasisType)a.OasisAnswer.CachedOasisLayout.Type == OasisType.LivingArrangement)).FirstOrDefault();
            }
            else
            {
                oamc = overridwOamc;
            }

            if ((oamc != null) && (oamc.OasisAnswer != null) && (oamc.OasisAnswer.CachedOasisLayout != null))
            {
                currResp = GetResponseB1Record(oamc.OasisAnswer.CachedOasisLayout, currB1);
                prevResp = GetResponseB1Record(oamc.OasisAnswer.CachedOasisLayout, prevB1);
                if (currResp != prevResp)
                {
                    string currRespTrim = TrimRadioResponse(currResp);
                    string prevRespTrim = TrimRadioResponse(prevResp);
                    OasisManagerAnswer omac = omq.OasisManagerAnswers.Where(o =>
                            ((o.OasisAnswer.AnswerLabel == currResp) || (o.OasisAnswer.AnswerLabel == currRespTrim)))
                        .FirstOrDefault();
                    OasisManagerAnswer omap = omq.OasisManagerAnswers.Where(o =>
                            ((o.OasisAnswer.AnswerLabel == prevResp) || (o.OasisAnswer.AnswerLabel == prevRespTrim)))
                        .FirstOrDefault();
                    OasisAddendumCheckQuesionForChange();
                    if ((omac != null) && (omap != null))
                    {
                        oasisAddendum = oasisAddendum + "- " + "Changed " + omap.OasisAnswer.AnswerLabel + " - " +
                                        OasisAddendumStripRichText(omap.OasisAnswer.AnswerText) + ", to " +
                                        omac.OasisAnswer.AnswerLabel + " - " +
                                        OasisAddendumStripRichText(omac.OasisAnswer.AnswerText) + CR;
                    }
                    else if (omac == null)
                    {
                        oasisAddendum = oasisAddendum + "- " + "Changed " + omap.OasisAnswer.AnswerLabel + " - " +
                                        OasisAddendumStripRichText(omap.OasisAnswer.AnswerText) + ", to null" + CR;
                    }
                    else
                    {
                        oasisAddendum = oasisAddendum + "- " + "Changed " + "null to " + omac.OasisAnswer.AnswerLabel +
                                        " - " + OasisAddendumStripRichText(omac.OasisAnswer.AnswerText) + CR;
                    }
                }
            }
            else
            {
                if (errorIfNotFound)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisManager.OasisAddendumCheckRadioForChange: {0} is not a valid question.  Contact your system administrator.",
                        omq.OasisQuestion.Question));
                }
            }
        }

        private void OasisAddendumCheckResponseForChange(OasisManagerQuestion omq, OasisLayout ol, string labelOverride,
            bool trim = true)
        {
            currResp = GetResponseB1Record(ol, currB1);
            prevResp = GetResponseB1Record(ol, prevB1);
            if (currResp != prevResp)
            {
                // do this twice = some mite have two leading zeros
                if (trim)
                {
                    currResp = TrimRadioResponse(currResp);
                    prevResp = TrimRadioResponse(prevResp);
                    currResp = TrimRadioResponse(currResp);
                    prevResp = TrimRadioResponse(prevResp);
                }

                OasisAddendumCheckQuesionForChange();
                if (string.IsNullOrWhiteSpace(currResp))
                {
                    oasisAddendum = oasisAddendum + "- " + "Changed " + labelOverride + " from " + prevResp + ", to null" + CR;
                }
                else if (string.IsNullOrWhiteSpace(prevResp))
                {
                    oasisAddendum = oasisAddendum + "- " + "Changed " + labelOverride + " from null to " + currResp + CR;
                }
                else
                {
                    oasisAddendum = oasisAddendum + "- " + "Changed " + labelOverride + " from " + prevResp + " to " + currResp + CR;
                }
            }
        }

        private void OasisAddendumCheckTextForChange(OasisManagerQuestion omq, bool errorIfNotFound = true,
            string overrideLabel = null, OasisManagerAnswer overrideOasisManagerAnswer = null)
        {
            bool found = false;
            OasisManagerAnswer oamc = overrideOasisManagerAnswer;
            if (oamc == null)
            {
                oamc = omq.OasisManagerAnswers.Where(a =>
                    (((OasisType)a.OasisAnswer.CachedOasisLayout.Type == OasisType.Text) ||
                     ((OasisType)a.OasisAnswer.CachedOasisLayout.Type == OasisType.TextNAUK) ||
                     ((OasisType)a.OasisAnswer.CachedOasisLayout.Type == OasisType.ICD) ||
                     ((OasisType)a.OasisAnswer.CachedOasisLayout.Type == OasisType.ICD10))).FirstOrDefault();
            }

            if (oamc != null)
            {
                switch ((OasisType)oamc.OasisAnswer.CachedOasisLayout.Type)
                {
                    case OasisType.ICD:
                    case OasisType.ICD10:
                    case OasisType.Text:
                    case OasisType.TextNAUK:
                        found = true;
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            currResp = GetResponseB1Record(oamc.OasisAnswer.CachedOasisLayout, currB1);
                            prevResp = GetResponseB1Record(oamc.OasisAnswer.CachedOasisLayout, prevB1);
                            if (currResp != prevResp)
                            {
                                OasisAddendumCheckQuesionForChange();
                                string label = (overrideLabel == null) ? "text" : overrideLabel;
                                oasisAddendum = oasisAddendum + "- " + "Changed " + label + " from '" +
                                                ((prevResp == null) ? "null" : prevResp) + "' to '" +
                                                ((currResp == null) ? "null" : currResp) + "'" + CR;
                            }
                        }

                        break;
                }
            }

            if ((found == false) && (errorIfNotFound))
            {
                MessageBox.Show(String.Format(
                    "Error OasisManager.OasisAddendumCheckTextForChange: {0} is not a valid question.  Contact your system administrator.",
                    omq.OasisQuestion.Question));
            }
        }

        private void OasisAddendumCheckTextForChange(OasisAnswer oa)
        {
            if ((oa != null) && (oa.CachedOasisLayout != null))
            {
                switch ((OasisType)oa.CachedOasisLayout.Type)
                {
                    case OasisType.ICD:
                    case OasisType.ICD10:
                    case OasisType.Text:
                    case OasisType.TextNAUK:
                        currResp = GetResponseB1Record(oa.CachedOasisLayout, currB1);
                        prevResp = GetResponseB1Record(oa.CachedOasisLayout, prevB1);
                        if (currResp != prevResp)
                        {
                            OasisAddendumCheckQuesionForChange();
                            oasisAddendum = oasisAddendum + "- " + "Changed text from '" +
                                            ((prevResp == null) ? "null" : prevResp) + "' to '" +
                                            ((currResp == null) ? "null" : currResp) + "'" + CR;
                        }

                        break;
                }
            }
        }

        private void OasisAddendumCheckDateForChange(OasisManagerQuestion omq, bool errorIfNotFound = true)
        {
            bool found = false;
            foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                switch ((OasisType)oamc.OasisAnswer.CachedOasisLayout.Type)
                {
                    case OasisType.Date:
                    case OasisType.DateNAUK:
                        found = true;
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            currResp = GetResponseB1Record(oamc.OasisAnswer.CachedOasisLayout, currB1);
                            prevResp = GetResponseB1Record(oamc.OasisAnswer.CachedOasisLayout, prevB1);
                            if (currResp != prevResp)
                            {
                                OasisAddendumCheckQuesionForChange();
                                oasisAddendum = oasisAddendum + "- " + "Changed date from " +
                                                ((prevResp == null) ? "null" : OasisAddendumFormatDate(prevResp)) +
                                                " to " + ((currResp == null)
                                                    ? "null"
                                                    : OasisAddendumFormatDate(currResp)) + CR;
                            }
                        }

                        break;
                }

            if ((found == false) && (errorIfNotFound))
            {
                MessageBox.Show(String.Format(
                    "Error OasisManager.OasisAddendumCheckDateForChange: {0} is not a valid question.  Contact your system administrator.",
                    omq.OasisQuestion.Question));
            }
        }

        private void OasisAddendumCheckDateForChange(OasisAnswer oa)
        {
            if ((oa != null) && (oa.CachedOasisLayout != null))
            {
                switch ((OasisType)oa.CachedOasisLayout.Type)
                {
                    case OasisType.Date:
                    case OasisType.DateNAUK:
                        if (oa.CachedOasisLayout != null)
                        {
                            currResp = GetResponseB1Record(oa.CachedOasisLayout, currB1);
                            prevResp = GetResponseB1Record(oa.CachedOasisLayout, prevB1);
                            if (currResp != prevResp)
                            {
                                OasisAddendumCheckQuesionForChange();
                                oasisAddendum = oasisAddendum + "- " + "Changed date from " +
                                                ((prevResp == null) ? "null" : OasisAddendumFormatDate(prevResp)) +
                                                " to " + ((currResp == null)
                                                    ? "null"
                                                    : OasisAddendumFormatDate(currResp)) + CR;
                            }
                        }

                        break;
                }
            }
        }

        private string TrimRadioResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            string r = text.Trim();
            if (r.Length != 2)
            {
                return r;
            }

            if (r == "00")
            {
                return "0";
            }

            if (r.StartsWith("0"))
            {
                return r.Replace("0", "");
            }

            return r;
        }

        private string OasisAddendumStripRichText(string text)
        {
            if (text == null)
            {
                return "";
            }

            string t = text
                .Replace("<Bold>", "").Replace("</Bold>", "").Replace("<bold>", "").Replace("</bold>", "")
                .Replace("<Underline>", "").Replace("</Underline>", "").Replace("<underline>", "")
                .Replace("</underline>", "").Trim();
            string[] delimiter = { "<LineB" };
            string[] split = t.Split(delimiter, StringSplitOptions.None);
            return split[0].Trim() + ((split.Length > 1) ? "..." : "");
        }

        private string OasisAddendumFormatDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (text.Equals(BIRTHDAYDASH))
            {
                return null; // Birthdate can contain dashes
            }

            if (text.Equals(A0245DASH))
            {
                return "dash"; // Birthdate can contain a dash
            }

            string stringDate = text.Substring(4, 2) + "/" + text.Substring(6, 2) + "/" + text.Substring(0, 4);
            DateTime date = DateTime.Today;
            return DateTime.TryParse(stringDate, out date) ? stringDate : null;
        }

        public string OasisAddendum(bool isOASISReview)
        {
            if (CurrentEncounter == null)
            {
                return null;
            }

            if (isOASISReview)
            {
                // We create an addendum for review iff: we are an Coordinator with 'canEdit' and the survey is in Review
                if (CurrentEncounter.CMSCoordinatorCanEdit == false)
                {
                    return null;
                }
            }
            else
            {
                // Insure Encounter is complete and we have a user with role doing the edit
                if ((CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed) == false)
                {
                    return null;
                }

                if (CurrentEncounter.CanEditCompleteCMS == false)
                {
                    return null;
                }
            }

            // check new version if not bypassed 
            if (CurrentEncounterOasis == null)
            {
                return null;
            }

            if (CurrentEncounterOasis.BypassFlag == true)
            {
                return null;
            }

            if (OasisManagerQuestions == null)
            {
                return null;
            }

            if (OasisManagerQuestions.Any() == false)
            {
                return null;
            }

            List<EncounterOasis> eoList = CurrentEncounter.EncounterOasis
                .Where(e => ((e.InactiveDate == null) && (e.REC_ID != "X1"))).OrderByDescending(e => e.AddedDate)
                .ToList();
            if (eoList == null)
            {
                return null;
            }

            oasisAddendum = "";
            prevQuestion = null;
            if (isOASISReview)
            {
                if (eoList.Any() == false)
                {
                    return null;
                }

                if (eoList[0].B1Record == eoList[0].PreReviewRecord)
                {
                    return null;
                }

                currB1 = eoList[0].B1Record;

                // Find Previous B1 Record, attempt to find the most recent PreReviewRecord
                var prev = eoList.Where(a => a.PreReviewRecord != null).OrderByDescending(a => a.AddedDate)
                    .FirstOrDefault();

                if (prev != null)
                {
                    prevB1 = prev.PreReviewRecord;
                }

                if (prevB1 == null)
                {
                    // B1 Record not found, default to B1 on most recent encounter
                    if (prev != null)
                    {
                        prevB1 = prev.B1Record;
                    }

                    // Last failsafe is to default to the current B1, shouldn't ever get hit
                    if (prevB1 == null)
                    {
                        prevB1 = currB1;
                    }
                }
            }
            else
            {
                if (eoList.Count < 2)
                {
                    return null;
                }

                if (eoList[0].EncounterOasisKey > 0)
                {
                    return null;
                }

                if (eoList[0].B1Record == eoList[1].B1Record)
                {
                    return null;
                }

                currB1 = eoList[0].B1Record;
                prevB1 = eoList[1].B1Record;
            }

            foreach (OasisManagerQuestion omq in OasisManagerQuestions)
            {
                currQuestion = omq.OasisQuestion.Question;

                switch ((OasisType)omq.OasisQuestion.CachedOasisLayout.Type)
                {
                    case OasisType.Text:
                        OasisAddendumCheckTextForChange(omq);
                        break;
                    case OasisType.Date:
                        OasisAddendumCheckDateForChange(omq);
                        break;
                    case OasisType.Radio:
                    case OasisType.RadioHorizontal:
                        OasisAddendumCheckRadioForChange(omq);
                        break;
                    case OasisType.LivingArrangement:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1100_PTNT_LVG_STUTN"),
                            "living arrangement", false);
                        break;
                    case OasisType.TrackingSheet:
                        foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                            OasisAddendumCheckTextForChange(omq, false, null, oamc);
                        OasisAddendumCheckDateForChange(omq, false);
                        OasisAddendumCheckCheckForChange(omq, false);
                        OasisAddendumCheckRadioForChange(omq, false);
                        break;
                    case OasisType.CheckBoxHeader:
                        OasisAddendumCheckCheckForChange(omq);
                        break;
                    case OasisType.DateNAUK:
                        OasisAddendumCheckDateForChange(omq);
                        OasisAddendumCheckCheckForChange(omq);
                        break;
                    case OasisType.TextNAUK:
                        OasisAddendumCheckTextForChange(omq);
                        OasisAddendumCheckCheckForChange(omq);
                        break;
                    case OasisType.ICD:
                    case OasisType.ICD10:
                        foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                            if (((OasisType)oamc.OasisAnswer.CachedOasisLayout.Type == OasisType.ICD) ||
                                ((OasisType)oamc.OasisAnswer.CachedOasisLayout.Type == OasisType.ICD10))
                            {
                                OasisAddendumCheckTextForChange(omq, false, oamc.OasisAnswer.AnswerText, oamc);
                            }

                        OasisAddendumCheckCheckForChange(omq, false);
                        break;
                    case OasisType.ICDMedical:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1020_PRIMARY_DIAG_ICD"),
                            "a. primary diagnosis", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1020_PRIMARY_DIAG_SEVERITY"),
                            "a. primary diagnosis severity");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_A3"),
                            "a. payment diagnosis 3", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_A4"),
                            "a. payment diagnosis 4", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG1_ICD"),
                            "b. other diagnosis", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG1_SEVERITY"),
                            "b. other diagnosis severity");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_B3"),
                            "b. payment diagnosis 3", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_B4"),
                            "b. payment diagnosis 4", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG2_ICD"),
                            "c. other diagnosis", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG2_SEVERITY"),
                            "c. other diagnosis severity");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_C3"),
                            "c. payment diagnosis 3", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_C4"),
                            "c. payment diagnosis 4", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG3_ICD"),
                            "d. other diagnosis", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG3_SEVERITY"),
                            "d. other diagnosis severity");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_D3"),
                            "d. payment diagnosis 3", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_D4"),
                            "d. payment diagnosis 4", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG4_ICD"),
                            "e. other diagnosis", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG4_SEVERITY"),
                            "e. other diagnosis severity");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_E3"),
                            "e. payment diagnosis 3", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_E4"),
                            "e. payment diagnosis 4", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG5_ICD"),
                            "f. other diagnosis", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG5_SEVERITY"),
                            "f. other diagnosis severity");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_F3"),
                            "f. payment diagnosis 3", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_F4"),
                            "f. payment diagnosis 4", false);
                        break;
                    case OasisType.ICD10Medical:
                    case OasisType.ICD10MedicalV2:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1021_PRIMARY_DIAG_ICD"),
                            "a. primary diagnosis", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1021_PRIMARY_DIAG_SEVERITY"),
                            "a. primary diagnosis severity");
                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_A3"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_A3"),
                                "a. optional diagnosis 3", false);
                        }

                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_A4"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_A4"),
                                "a. optional diagnosis 4", false);
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG1_ICD"),
                            "b. other diagnosis", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG1_SEVERITY"),
                            "b. other diagnosis severity");
                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_B3"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_B3"),
                                "b. optional diagnosis 3", false);
                        }

                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_B4"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_B4"),
                                "b. optional diagnosis 4", false);
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG2_ICD"),
                            "c. other diagnosis", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG2_SEVERITY"),
                            "c. other diagnosis severity");
                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_C3"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_C3"),
                                "c. optional diagnosis 3", false);
                        }

                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_C4"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_C4"),
                                "c. optional diagnosis 4", false);
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG3_ICD"),
                            "d. other diagnosis", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG3_SEVERITY"),
                            "d. other diagnosis severity");
                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_D3"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_D3"),
                                "d. optional diagnosis 3", false);
                        }

                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_D4"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_D4"),
                                "d. optional diagnosis 4", false);
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG4_ICD"),
                            "e. other diagnosis", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG4_SEVERITY"),
                            "e. other diagnosis severity");
                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_E3"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_E3"),
                                "e. optional diagnosis 3", false);
                        }

                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_E4"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_E4"),
                                "e. optional diagnosis 4", false);
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG5_ICD"),
                            "f. other diagnosis", false);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG5_SEVERITY"),
                            "f. other diagnosis severity");
                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_F3"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_F3"),
                                "f. optional diagnosis 3", false);
                        }

                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_F4"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_F4"),
                                "f. optional diagnosis 4", false);
                        }

                        break;
                    case OasisType.RadioWithDate:
                        OasisAddendumCheckRadioForChange(omq);
                        if (omq.OasisManagerAnswers != null)
                        {
                            if (omq.OasisManagerAnswers.Count >= 2)
                            {
                                if (omq.OasisManagerAnswers[1].OasisManagerAnswerChildDate != null)
                                {
                                    OasisAddendumCheckDateForChange(omq.OasisManagerAnswers[1]
                                        .OasisManagerAnswerChildDate.OasisAnswer);
                                }
                            }
                        }

                        break;
                    case OasisType.Synopsys:
                        if (omq.OasisQuestion.Question == "M2250")
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2250_PLAN_SMRY_PTNT_SPECF"),
                                "a.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2250_PLAN_SMRY_DBTS_FT_CARE"),
                                "b.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2250_PLAN_SMRY_FALL_PRVNT"),
                                "c.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2250_PLAN_SMRY_DPRSN_INTRVTN"),
                                "d.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2250_PLAN_SMRY_PAIN_INTRVTN"),
                                "e.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2250_PLAN_SMRY_PRSULC_PRVNT"),
                                "f.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2250_PLAN_SMRY_PRSULC_TRTMT"),
                                "g.");
                        }
                        else if (omq.OasisQuestion.Question == "M2400")
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2400_INTRVTN_SMRY_DBTS_FT"),
                                "a.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2400_INTRVTN_SMRY_FALL_PRVNT"),
                                "b.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2400_INTRVTN_SMRY_DPRSN"), "c.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2400_INTRVTN_SMRY_PAIN_MNTR"),
                                "d.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2400_INTRVTN_SMRY_PRSULC_PRVN"),
                                "e.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2400_INTRVTN_SMRY_PRSULC_WET"),
                                "f.");
                        }
                        else if (omq.OasisQuestion.Question == "M2401")
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2401_INTRVTN_SMRY_DBTS_FT"),
                                "a.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2401_INTRVTN_SMRY_FALL_PRVNT"),
                                "b.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2401_INTRVTN_SMRY_DPRSN"), "c.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2401_INTRVTN_SMRY_PAIN_MNTR"),
                                "d.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2401_INTRVTN_SMRY_PRSULC_PRVN"),
                                "e.");
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2401_INTRVTN_SMRY_PRSULC_WET"),
                                "f.");
                        }

                        break;
                    case OasisType.PriorADL:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1900_PRIOR_ADLIADL_SELF"), "a.");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1900_PRIOR_ADLIADL_AMBLTN"), "b.");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1900_PRIOR_ADLIADL_TRNSFR"), "c.");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1900_PRIOR_ADLIADL_HSEHOLD"), "d.");
                        break;
                    case OasisType.PoorMed:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2040_PRIOR_MGMT_ORAL_MDCTN"), "a.");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2040_PRIOR_MGMT_INJCTN_MDCTN"),
                            "b.");
                        break;
                    case OasisType.CareManagement:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2100_CARE_TYPE_SRC_ADL"), "a.");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2100_CARE_TYPE_SRC_IADL"), "b.");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2100_CARE_TYPE_SRC_MDCTN"), "c.");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2100_CARE_TYPE_SRC_PRCDR"), "d.");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2100_CARE_TYPE_SRC_EQUIP"), "e.");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2100_CARE_TYPE_SRC_SPRVSN"), "f.");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2100_CARE_TYPE_SRC_ADVCY"), "g.");
                        break;
                    case OasisType.CareManagement_C1:
                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_ADL"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_ADL"), "a.");
                        }

                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_IADL"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_IADL"), "b.");
                        }

                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_MDCTN"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_MDCTN"),
                                "c.");
                        }

                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_PRCDR"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_PRCDR"),
                                "d.");
                        }

                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_EQUIP"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_EQUIP"),
                                "e.");
                        }

                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_SPRVSN"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_SPRVSN"),
                                "f.");
                        }

                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_ADVCY"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_ADVCY"),
                                "g.");
                        }

                        break;
                    case OasisType.DepressionScreening:
                        OasisManagerAnswer overrideOamc = omq.OasisManagerAnswers.Where(a =>
                            (((OasisType)a.OasisAnswer.CachedOasisLayout.Type == OasisType.Radio)) &&
                            (a.OasisAnswer.Sequence == 1)).FirstOrDefault();
                        OasisAddendumCheckRadioForChange(omq, true, overrideOamc);
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1730_PHQ2_LACK_INTRST"), "PHQ-2 a.");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1730_PHQ2_DPRSN"), "PHQ-2 b.");
                        break;
                    case OasisType.PressureUlcer:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NBR_PRSULC_STG2"),
                            "a. Stage II current");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NBR_STG2_AT_SOC_ROC"),
                            "a. Stage II past");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NBR_PRSULC_STG3"),
                            "b. Stage III current");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NBR_STG3_AT_SOC_ROC"),
                            "b. Stage III past");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NBR_PRSULC_STG4"),
                            "c. Stage IV current");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NBR_STG4_AT_SOC_ROC"),
                            "c. Stage IV past");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NSTG_DRSG"),
                            "d.1 Unstageable current");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NSTG_DRSG_SOC_ROC"),
                            "d.1 Unstageable past");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NSTG_CVRG"),
                            "d.2 Unstageable current");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NSTG_CVRG_SOC_ROC"),
                            "d.2 Unstageable past");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NSTG_DEEP_TISUE"),
                            "d.3 Unstageable current");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NSTG_DEEP_TISUE_SOC_ROC"),
                            "d.3 Unstageable past");
                        break;
                    case OasisType.WoundDimension:
                        if (oasisAddendum.Contains(omq.OasisQuestion.Question) == false)
                        {
                            if (omq.OasisQuestion.Question == "M1310")
                            {
                                OasisAddendumCheckResponseForChange(omq,
                                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1310_PRSR_ULCR_LNGTH"),
                                    "wound length", false);
                            }
                            else if (omq.OasisQuestion.Question == "M1312")
                            {
                                OasisAddendumCheckResponseForChange(omq,
                                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1312_PRSR_ULCR_WDTH"),
                                    "wound width", false);
                            }
                            else if (omq.OasisQuestion.Question == "M1314")
                            {
                                OasisAddendumCheckResponseForChange(omq,
                                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1314_PRSR_ULCR_DEPTH"),
                                    "wound depth", false);
                            }
                        }

                        break;
                    case OasisType.HISTrackingSheet:
                        foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                            OasisAddendumCheckTextForChange(omq, false, null, oamc);
                        OasisAddendumCheckDateForChange(omq, false);
                        OasisAddendumCheckCheckForChange(omq, false);
                        OasisAddendumCheckRadioForChange(omq, false);
                        break;
                    case OasisType.PressureUlcerWorse:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1309_NBR_NEW_WRS_PRSULC_STG2"),
                            "a. Stage II");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1309_NBR_NEW_WRS_PRSULC_STG2"),
                            "b. Stage III");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1309_NBR_NEW_WRS_PRSULC_STG2"),
                            "c. Stage IV");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1309_NBR_NEW_WRS_PRSULC_NSTG"),
                            "d. Unstageable");
                        break;
                    case OasisType.PressureUlcer_C1:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NBR_PRSULC_STG2"),
                            "a. Stage II");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NBR_PRSULC_STG3"),
                            "b. Stage III");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NBR_PRSULC_STG4"),
                            "c. Stage IV");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NSTG_DRSG"), "d.1 Unstageable");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NSTG_CVRG"), "d.2 Unstageable");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NSTG_DEEP_TISUE"),
                            "d.3 Unstageable");
                        break;
                    case OasisType.PressureUlcer_C2:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NBR_PRSULC_STG2_A1"),
                            "A1. Stage 2 current");
                        if ((RFA == "04") || (RFA == "05") || (RFA == "09"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NBR_ULC_SOCROC_STG2_A2"),
                                "A2. Stage 2 past");
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NBR_PRSULC_STG3_B1"),
                            "B1. Stage 3 current");
                        if ((RFA == "04") || (RFA == "05") || (RFA == "09"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NBR_ULC_SOCROC_STG3_B2"),
                                "B2. Stage 3 past");
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NBR_PRSULC_STG4_C1"),
                            "C1. Stage 4 current");
                        if ((RFA == "04") || (RFA == "05") || (RFA == "09"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NBR_ULC_SOCROC_STG4_C2"),
                                "C2. Stage 4 past");
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NSTG_DRSG_D1"),
                            "D1 Unstageable non-removable dressing current");
                        if ((RFA == "04") || (RFA == "05") || (RFA == "09"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NSTG_DRSG_SOCROC_D2"),
                                "D2 Unstageable non-removable dressing past");
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NSTG_CVRG_E1"),
                            "E1 Unstageable slough/eschar current");
                        if ((RFA == "04") || (RFA == "05") || (RFA == "09"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NSTG_CVRG_SOCROC_E2"),
                                "E2 Unstageable slough/eschar past");
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NSTG_DEEP_TSUE_F1"),
                            "F1 Unstageable deep tissue injury current");
                        if ((RFA == "04") || (RFA == "05") || (RFA == "09"))
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NSTG_DEEP_TSUE_SOCROC_F2"),
                                "F2 Unstageable deep tissue injury past");
                        }

                        break;
                    case OasisType.PressureUlcer_C3:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NBR_PRSULC_STG2_A1"),
                            "A1. Stage 2 current");
                        if (RFA == "09")
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NBR_ULC_SOCROC_STG2_A2"),
                                "A2. Stage 2 past");
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NBR_PRSULC_STG3_B1"),
                            "B1. Stage 3 current");
                        if (RFA == "09")
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NBR_ULC_SOCROC_STG3_B2"),
                                "B2. Stage 3 past");
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NBR_PRSULC_STG4_C1"),
                            "C1. Stage 4 current");
                        if (RFA == "09")
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NBR_ULC_SOCROC_STG4_C2"),
                                "C2. Stage 4 past");
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NSTG_DRSG_D1"),
                            "D1 Unstageable non-removable dressing current");
                        if (RFA == "09")
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NSTG_DRSG_SOCROC_D2"),
                                "D2 Unstageable non-removable dressing past");
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NSTG_CVRG_E1"),
                            "E1 Unstageable slough/eschar current");
                        if (RFA == "09")
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NSTG_CVRG_SOCROC_E2"),
                                "E2 Unstageable slough/eschar past");
                        }

                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NSTG_DEEP_TSUE_F1"),
                            "F1 Unstageable deep tissue injury current");
                        if (RFA == "09")
                        {
                            OasisAddendumCheckResponseForChange(omq,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1311_NSTG_DEEP_TSUE_SOCROC_F2"),
                                "F2 Unstageable deep tissue injury past");
                        }

                        break;
                    case OasisType.PressureUlcerWorse_C2:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_STG2_A"),
                            "a. Stage 2");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_STG3_B"),
                            "b. Stage 3");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_STG4_C"),
                            "c. Stage 4");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_NSTG_DRSG_D"),
                            "d. Unstageable non-removable dressing");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_NSTG_CVRG_E"),
                            "e. Unstageable slough/eschar");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_NSTG_TSUE_F"),
                            "f. Unstageable deep tissue injury");
                        break;
                    case OasisType.HeightWeight_C2:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1060_HEIGHT_A"), "a. Height");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1060_WEIGHT_B"), "b. Weight");
                        break;
                    case OasisType.GG0170C_C2:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "GG0170C_MOBILITY_SOCROC_PERF"),
                            "1. SOC/ROC Performance");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "GG0170C_MOBILITY_DSCHG_GOAL"),
                            "2. Discharge Goal");
                        break;
                    case OasisType.TopLegend:
                    case OasisType.LeftLegend:
                        int? maxRow = omq.OasisManagerAnswers.Max(m => m.OasisAnswer.SubQuestionRow);
                        int? maxColumn = omq.OasisManagerAnswers.Max(m => m.OasisAnswer.SubQuestionColumn);
                        if ((maxRow == null) || (maxColumn == null))
                        {
                            break;
                        }

                        for (int row = 1; row <= (int)maxRow; row++)
                        {
                            for (int column = 1; column <= (int)maxColumn; column++)
                            {
                                OasisManagerAnswer omaAnchor = omq.OasisManagerAnswers
                                    .Where(p => ((p.OasisAnswer.SubQuestionRow == row) &&
                                                 (p.OasisAnswer.SubQuestionColumn == column)))
                                    .OrderBy(p => p.OasisAnswer.Sequence).FirstOrDefault();
                                if (omaAnchor != null)
                                {
                                    OasisAddendumCheckResponseForChange(omq, omaAnchor.OasisAnswer.CachedOasisLayout,
                                        omaAnchor.OasisAnswer.SubQuestionLabelAndTextShort(omq));
                                }
                            }
                        }

                        break;
                    case OasisType.ServiceUtilization_10:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010A1"), "A. RN on A0270");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010A2"), "A. RN on A0270-1");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010A3"), "A. RN on A0270-2");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010B1"), "B. Physician on A0270");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010B2"), "B. Physician on A0270-1");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010B3"), "B. Physician on A0270-2");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010C1"), "C. MSW on A0270");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010C2"), "C. MSW on A0270-1");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010C3"), "C. MSW on A0270-2");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010D1"), "D. SC on A0270");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010D2"), "D. SC on A0270-1");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010D3"), "D. SC on A0270-2");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010E1"), "E. LPN on A0270");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010E2"), "E. LPN on A0270-1");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010E3"), "E. LPN on A0270-2");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010F1"), "F. Aide on A0270");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010F2"), "F. Aide on A0270-1");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5010F3"), "F. Aide on A0270-2");
                        break;
                    case OasisType.ServiceUtilization_30:
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030A1"), "A. RN on A0270-3");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030A2"), "A. RN on A0270-4");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030A3"), "A. RN on A0270-5");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030A4"), "A. RN on A0270-6");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030B1"), "B. Physician on A0270-3");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030B2"), "B. Physician on A0270-4");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030B3"), "B. Physician on A0270-5");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030B4"), "B. Physician on A0270-6");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030C1"), "C. MSW on A0270-3");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030C2"), "C. MSW on A0270-4");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030C3"), "C. MSW on A0270-5");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030C4"), "C. MSW on A0270-6");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030D1"), "D. SC on A0270-3");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030D2"), "D. SC on A0270-4");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030D3"), "D. SC on A0270-5");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030D4"), "D. SC on A0270-6");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030E1"), "E. LPN on A0270-3");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030E2"), "E. LPN on A0270-4");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030E3"), "E. LPN on A0270-5");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030E4"), "E. LPN on A0270-6");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030F1"), "F. Aide on A0270-3");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030F2"), "F. Aide on A0270-4");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030F3"), "F. Aide on A0270-5");
                        OasisAddendumCheckResponseForChange(omq,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "O5030F4"), "F. Aide on A0270-6");
                        break;
                    default:
                        MessageBox.Show(String.Format(
                            "Error OasisManager.ValidateQuestion: {0} is not a valid question.  Contact your system administrator.",
                            omq.OasisQuestion.Question));
                        break;
                }
            }

            return (string.IsNullOrWhiteSpace(oasisAddendum)) ? null : oasisAddendum;
        }

        public string OasisAddendumInactivateOASIS(EncounterOasis currEncounterOasis, EncounterOasis prevEncounterOasis)
        {
            if (currEncounterOasis == null)
            {
                return null;
            }

            if (prevEncounterOasis == null)
            {
                return null;
            }

            oasisAddendum = "";
            currB1 = currEncounterOasis.B1Record;
            prevB1 = prevEncounterOasis.B1Record;
            prevQuestion = null;

            currQuestion = "M0030";
            OasisAddendumCheckDateForChange(
                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0030_START_CARE_DT"));
            currQuestion = "M0040";
            OasisAddendumCheckTextForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0040_PAT_FNAME"));
            OasisAddendumCheckTextForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0040_PAT_MI"));
            OasisAddendumCheckTextForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0040_PAT_LNAME"));
            OasisAddendumCheckTextForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0040_PAT_SUFFIX"));
            currQuestion = "M0064";
            OasisAddendumCheckTextForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0064_SSN"));
            OasisAddendumCheckCheckForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0064_SSN_UK"));
            currQuestion = "M0066";
            OasisAddendumCheckDateForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0066_PAT_BIRTH_DT"));
            currQuestion = "M0069";
            currResp = GetResponseB1Record(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0069_PAT_GENDER"),
                currB1);
            prevResp = GetResponseB1Record(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0069_PAT_GENDER"),
                prevB1);
            if (currResp != prevResp)
            {
                OasisAddendumCheckQuesionForChange();
                oasisAddendum = oasisAddendum +
                                ((currResp == "1")
                                    ? "Changed 2 - Female to 1 - Male"
                                    : "Changed 1 - Male to 2 - Female") + CR;
            }

            return (string.IsNullOrWhiteSpace(oasisAddendum)) ? null : oasisAddendum;
        }

        public string OasisAddendumInactivateHIS(EncounterOasis currEncounterOasis, EncounterOasis prevEncounterOasis)
        {
            if (currEncounterOasis == null)
            {
                return null;
            }

            if (prevEncounterOasis == null)
            {
                return null;
            }

            oasisAddendum = "";
            currB1 = currEncounterOasis.B1Record;
            prevB1 = prevEncounterOasis.B1Record;
            prevQuestion = null;

            currQuestion = "A0220";
            OasisAddendumCheckDateForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0220"));
            currQuestion = "A0270";
            OasisAddendumCheckDateForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0270"));
            currQuestion = "A0500";
            OasisAddendumCheckTextForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0500A"));
            OasisAddendumCheckTextForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0500C"));
            currQuestion = "A0600A";
            OasisAddendumCheckTextForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0600A"));
            currQuestion = "A0800";
            currResp = GetResponseB1Record(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0800"), currB1);
            prevResp = GetResponseB1Record(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0800"), prevB1);
            if (currResp != prevResp)
            {
                OasisAddendumCheckQuesionForChange();
                oasisAddendum = oasisAddendum +
                                ((currResp == "1")
                                    ? "Changed 2 - Female to 1 - Male"
                                    : "Changed 1 - Male to 2 - Female") + CR;
            }

            currQuestion = "A0900";
            OasisAddendumCheckDateForChange(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0900"));
            return (string.IsNullOrWhiteSpace(oasisAddendum)) ? null : oasisAddendum;
        }

        public void ChangedDischargeRFA(string newRFA, AdmissionDiscipline currentAdmissionDiscipline,
            Patient currentPatient)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinicianBypassOASISAssist)
            {
                return;
            }

            string prevB1Record = CurrentEncounterOasis.B1Record;
            CurrentEncounterOasis.B1Record = new String(' ', GetB1RecordLength(CurrentEncounterOasis.OasisVersionKey));
            RFA = newRFA;
            SetupDefaultsOASIS(true);
            CopyResponse("M0090", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"),
                prevB1Record);

            if (!MappingAllowedClinician)
            {
                return;
            }

            if (IsQuestionInSurveyNotHidden("M0903"))
            {
                CopyResponse("M0903", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0903_LAST_HOME_VISIT"),
                    prevB1Record);
            }

            // Note - If need be - M0906 was set by SetupDefaults.SetupPatientDefault2 for RFA 08 as the Death Date
            //      - Here - set it to the _dischargeDate for RFA 09
            if (IsQuestionInSurveyNotHidden("M0906") && ((RFA == "09")))
            {
                if (currentAdmissionDiscipline.DischargeDateTime != null)
                {
                    SetDateResponse(currentAdmissionDiscipline.DischargeDateTime,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0906_DC_TRAN_DTH_DT"));
                }
            }

            OasisAlertCheckAllMeasures();
        }

        public void ChangedOasisVersion()
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            ApplySkipLogic();

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAlertCheckAllMeasures();
        }

        public static OasisManager Create(DynamicFormViewModel vm)
        {
            return new OasisManager(vm);
        }

        public static OasisManager Create(Patient pCurrentPatient, Admission pCurrentAdmission,
            Encounter pCurrentEncounter)
        {
            return new OasisManager(pCurrentPatient, pCurrentAdmission, pCurrentEncounter);
        }

        private DynamicFormViewModel DynamicFormViewModel;

        public OasisManager(DynamicFormViewModel vm)
        {
            ConstructorTag = "DynamicForm";
            DynamicFormViewModel = vm;
            CurrentAdmission = vm.CurrentAdmission;
            CurrentPatient = vm.CurrentPatient;
            CurrentEncounter = vm.CurrentEncounter;
            CurrentForm = vm.CurrentForm;
            PreviousEncounterStatus = vm.PreviousEncounterStatus;

            AdmissionPhysician = new AdmissionPhysicianFacade();
            AdmissionPhysician.Admission = vm.CurrentAdmission;
            AdmissionPhysician.Encounter = vm.CurrentEncounter;
            PropertyChanged += OasisManager_PropertyChanged;

            FormModel = vm.FormModel;
            if (CurrentEncounter != null)
            {
                if (CurrentEncounter.EncounterOasis.Any())
                {
                    CurrentEncounterOasis = GetCurrentEncounterOasis();
                    if (CurrentEncounterOasis != null)
                    {
                        OasisVersion ov = OasisCache.GetOasisVersionByVersionKey(CurrentEncounterOasis.OasisVersionKey);
                        OasisVersionKey = CurrentEncounterOasis.OasisVersionKey;
                        VersionCD2 = (ov == null) ? null : ov.VersionCD2;
                        OasisVersionUsingICD10 = (ov == null) ? false : ov.UsingICD10;
                        RFA = CurrentEncounterOasis.RFA;
                    }
                }
            }

            int? disciplineKey = vm.CurrentTask == null
                ? null
                : ServiceTypeCache.GetDisciplineKey((int)vm.CurrentTask.ServiceTypeKey);
            if (disciplineKey != null)
            {
                Discipline d = DisciplineCache.GetDisciplineFromKey((int)disciplineKey);
                if (d != null)
                {
                    HCFACode = d.HCFACode;
                }
            }
        }

        public OasisManager(Patient pCurrentPatient, Admission pCurrentAdmission, Encounter pCurrentEncounter)
        {
            ConstructorTag = "NonDynamicForm";
            CurrentAdmission = pCurrentAdmission;
            CurrentPatient = pCurrentPatient;
            CurrentEncounter = pCurrentEncounter;

            AdmissionPhysician = new AdmissionPhysicianFacade();
            AdmissionPhysician.Admission = pCurrentAdmission;
            AdmissionPhysician.Encounter = pCurrentEncounter;
            PropertyChanged += OasisManager_PropertyChanged;

            FormModel = null;
            if (CurrentEncounter != null)
            {
                if (CurrentEncounter.EncounterOasis.Any())
                {
                    CurrentEncounterOasis = GetCurrentEncounterOasis();
                    if (CurrentEncounterOasis != null)
                    {
                        OasisVersion ov = OasisCache.GetOasisVersionByVersionKey(CurrentEncounterOasis.OasisVersionKey);
                        OasisVersionKey = CurrentEncounterOasis.OasisVersionKey;
                        VersionCD2 = (ov == null) ? null : ov.VersionCD2;
                        RFA = CurrentEncounterOasis.RFA;
                    }
                }
            }
        }

        public EncounterOasis EncounterOasisUpdateForKeyChangeOASIS(EncounterOasis encounterOasis)
        {
            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "CORRECTION_NUM"))
            {
                SetTextResponseB1("00", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "CORRECTION_NUM"),
                    encounterOasis);
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "TRANS_TYPE_CD"))
            {
                SetTextResponseB1("1", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "TRANS_TYPE_CD"),
                    encounterOasis);
            }

            SetDateResponseB1(CurrentAdmission.SOCDate,
                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0030_START_CARE_DT"), encounterOasis);
            SetTextResponseB1(CurrentPatient.FirstName,
                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0040_PAT_FNAME"), encounterOasis);
            SetTextResponseB1(CurrentPatient.MiddleName,
                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0040_PAT_MI"), encounterOasis);
            SetTextResponseB1(CurrentPatient.LastName,
                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0040_PAT_LNAME"), encounterOasis);
            SetTextResponseB1(CurrentPatient.Suffix,
                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0040_PAT_SUFFIX"), encounterOasis);

            if (string.IsNullOrWhiteSpace(CurrentPatient.SSN))
            {
                SetCheckBoxResponseB1(true, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0064_SSN_UK"),
                    encounterOasis);
            }
            else
            {
                SetCheckBoxResponseB1(false, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0064_SSN_UK"),
                    encounterOasis);
            }

            SetTextResponseB1(CurrentPatient.SSN, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0064_SSN"),
                encounterOasis);

            SetDateResponseB1(CurrentPatient.BirthDate,
                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0066_PAT_BIRTH_DT"), encounterOasis);
            SetTextResponseB1("1", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0069_PAT_GENDER"),
                encounterOasis);
            if (CurrentPatient.GenderCode != null)
            {
                if (CurrentPatient.GenderCode.ToLower() == "f")
                {
                    SetTextResponseB1("2", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0069_PAT_GENDER"),
                        encounterOasis);
                }
            }

            return encounterOasis;
        }

        public EncounterOasis EncounterOasisUpdateForKeyChangeHIS(EncounterOasis encounterOasis)
        {
            SetDateResponseB1(CurrentAdmission.SOCDate, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0220"),
                encounterOasis);
            if (encounterOasis.RFA == "01")
            {
                ClearResponse(OasisCache.GetOasisAnswerByCMSField(encounterOasis.OasisVersionKey, "A0270"));
            }
            else
            {
                if (CurrentAdmission.DischargeDateTime != null)
                {
                    SetDateResponse(CurrentAdmission.DischargeDateTime,
                        OasisCache.GetOasisAnswerByCMSField(encounterOasis.OasisVersionKey, "A0270"));
                }
            }

            SetTextResponseB1(CurrentPatient.FirstName, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0500A"),
                encounterOasis);
            SetTextResponseB1(CurrentPatient.LastName, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0500C"),
                encounterOasis);
            SetTextResponseB1(CurrentPatient.SSN, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0600A"),
                encounterOasis);
            SetTextResponseB1("1", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0800"), encounterOasis);
            if (CurrentPatient.GenderCode != null)
            {
                if (CurrentPatient.GenderCode.ToLower() == "f")
                {
                    SetTextResponseB1("2", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0800"),
                        encounterOasis);
                }
            }

            SetDateResponseB1(CurrentPatient.BirthDate, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0900"),
                encounterOasis);
            encounterOasis.Z0500A = WebContext.Current.User.MemberID;
            //Keep Z0500B the same 
            return encounterOasis;
        }

        public List<OasisSurveyGroup> OasisSurveyGroups =>
            OasisCache.GetOasisSurveyGroupByOasisVersionKeyAndRFA(OasisVersionKey, RFA);

        private List<OasisQuestion> _OasisQuestions;

        public List<OasisQuestion> OasisQuestions
        {
            get { return _OasisQuestions; }
            set { _OasisQuestions = value; }
        }

        private List<OasisManagerQuestion> _OasisManagerQuestions = new List<OasisManagerQuestion>();

        public List<OasisManagerQuestion> OasisManagerQuestions
        {
            get { return _OasisManagerQuestions; }
            set { _OasisManagerQuestions = value; }
        }

        private Guid _OasisManagerGuid = Guid.NewGuid();
        public Guid OasisManagerGuid => _OasisManagerGuid;

        private bool IsQuestionInSurvey(string question)
        {
            if (OasisQuestions == null)
            {
                return false;
            }

            if (OasisQuestions.Any() == false)
            {
                return false;
            }

            OasisQuestion oq = OasisQuestions.Where(q => (q.Question == question)).FirstOrDefault();
            return (oq == null) ? false : true;
        }

        private bool IsQuestionInSurveyNotHidden(string question)
        {
            if (IsQuestionInSurvey(question) == false)
            {
                return false;
            }

            OasisManagerQuestion omq = OasisManagerQuestions.Where(q => (q.OasisQuestion.Question == question))
                .FirstOrDefault();
            if (omq != null)
            {
                return (omq.Hidden) ? false : true;
            }

            return true;
        }

        private OasisQuestion GetQuestionInSurvey(string question)
        {
            if (OasisQuestions == null)
            {
                return null;
            }

            if (OasisQuestions.Any() == false)
            {
                return null;
            }

            OasisQuestion oq = OasisQuestions.Where(q => (q.Question == question)).FirstOrDefault();
            return oq;
        }

        private OasisQuestion GetQuestionInSurveyNotHidden(string question)
        {
            OasisQuestion oq = GetQuestionInSurvey(question);
            if (oq == null)
            {
                return null;
            }

            OasisManagerQuestion omq = OasisManagerQuestions.Where(q => (q.OasisQuestion.Question == question))
                .FirstOrDefault();
            if (omq != null)
            {
                return (omq.Hidden) ? null : oq;
            }

            return oq;
        }

        public bool Validate(List<SectionUI> sections, out string OasisSections)
        {
            OasisSections = string.Empty;
            if (CurrentEncounterOasis != null)
            {
                CurrentEncounterOasis.ValidationErrors.Clear();
            }

            if (!IsOasisActive)
            {
                return true;
            }

            if (CurrentEncounterOasis == null)
            {
                return true;
            }

            if (OasisManagerQuestions == null)
            {
                return true;
            }

            if (OasisManagerQuestions.Any() == false)
            {
                return true;
            }


            bool valid = true;
            string OasisSection = null;
            CurrentEncounterOasis.ValidationErrors.Clear();
            if (CurrentEncounterOasis.Validate(new ValidationContext(CurrentEncounterOasis)) == false)
            {
                bool trackingInError = false;
                if (CurrentEncounterOasis.ValidationErrors != null)
                {
                    foreach (ValidationResult vr in CurrentEncounterOasis.ValidationErrors)
                        if (vr.ErrorMessage.ToLower().Contains("bypass reason"))
                        {
                            trackingInError = true;
                        }
                }

                if (trackingInError)
                {
                    SectionUI sectionUI =
                        sections.Where(s => (s.Label.ToLower().Contains("tracking"))).FirstOrDefault();
                    if (sectionUI != null)
                    {
                        sectionUI.Errors = true;
                        OasisSections = OasisSections + ((OasisSections == null)
                            ? CurrentEncounterOasis.SYS_CDDescription + " " + sectionUI.Label
                            : ", " + CurrentEncounterOasis.SYS_CDDescription + " " + sectionUI.Label);
                    }

                    valid = false;
                }
            }

            // Validate OASIS Validation Errors Comment
            if ((CurrentEncounterOasis.BypassFlag == false) && (CurrentEncounterOasis.Encounter != null) &&
                (CurrentEncounterOasis.Encounter.Signed || (CurrentEncounterOasis.Encounter.EncounterStatus > 1)) &&
                CurrentEncounterOasis.HavenValidationErrors &&
                string.IsNullOrWhiteSpace(CurrentEncounterOasis.HavenValidationErrorsComment))
            {
                string[] memberNames = { "HavenValidationErrorsComment" };
                CurrentEncounterOasis.ValidationErrors.Add(
                    new ValidationResult("The OASIS Validation Errors Comment field is required", memberNames));
                valid = false;
            }

            // Validate Oasis Bypass
            if ((CurrentEncounterOasis.BypassFlag == true) &&
                (String.IsNullOrEmpty(CurrentEncounterOasis.BypassReason)))
            {
                string[] memberNames = { "BypassReason" };
                CurrentEncounterOasis.ValidationErrors.Add(new ValidationResult("The Bypass Reason field is required.",
                    memberNames));
                SectionUI sectionUI = sections.Where(s => (s.Label.ToLower().Contains("tracking"))).FirstOrDefault();
                if (sectionUI != null)
                {
                    sectionUI.Errors = true;
                    OasisSections = OasisSections + ((OasisSections == null)
                        ? "OASIS " + sectionUI.Label
                        : ", OASIS " + sectionUI.Label);
                }

                valid = false;
            }

            if (CurrentEncounter.FullValidationOASIS == false)
            {
                return valid;
            }

            List<OasisAuditError> oasisAuditErrors = OasisAuditsCheck();

            foreach (OasisManagerQuestion omq in OasisManagerQuestions)
                if (ValidateQuestion(omq, sections, oasisAuditErrors, out OasisSection) == false)
                {
                    if (OasisSection != null)
                    {
                        SectionUI sectionUI = sections.Where(s => ((s.Label == OasisSection) && s.IsOasis))
                            .FirstOrDefault();
                        if (sectionUI != null)
                        {
                            sectionUI.Errors = true;
                        }

                        if ((OasisSections == null) || (OasisSections.Contains(OasisSection) == false))
                        {
                            OasisSections = OasisSections + ((OasisSections == null)
                                ? CurrentEncounter.SYS_CDDescription + " " + OasisSection
                                : ", " + CurrentEncounter.SYS_CDDescription + " " + OasisSection);
                        }
                    }

                    valid = false;
                }
            
            return valid;
        }

        public void DefaultQuestions(bool forceDefault)
        {
            if (!IsOasisActive)
            {
                return;
            }

            if (CurrentEncounterOasis == null)
            {
                return;
            }

            if (OasisManagerQuestions == null)
            {
                return;
            }

            if (OasisManagerQuestions.Any() == false)
            {
                return;
            }

            foreach (OasisManagerQuestion omq in OasisManagerQuestions) DefaultQuestion(omq);
        }

        private List<OasisAuditError> OasisAuditsCheck()
        {
            List<OasisAuditError> oasisAuditErrors = new List<OasisAuditError>();
            return oasisAuditErrors; // NOOP - per BonnieY request

            //try
            //{
            //    OasisAudits oasisAudits = new OasisAudits();
            //    XDocument xDoc = oasisAudits.CheckForAuditsXml(CurrentEncounterOasis.B1Record);

            //    if (xDoc != null)
            //    {
            //        var edits = from e in xDoc.Descendants("edit")
            //                    select new
            //                    {
            //                        Header = e.Element("header") == null ? "" : e.Element("header").Value,
            //                        Reason = e.Element("reason") == null ? "" : e.Element("reason").Value,
            //                        Oasis = e.Element("reason") == null ? null : e.Descendants("oasis"),
            //                        Explanation = e.Element("explanation") == null ? "" : e.Element("explanation").Value,
            //                    };

            //        foreach (var es in edits)
            //        {
            //            string error = es.Header;
            //            if (string.IsNullOrWhiteSpace(es.Reason) == false)
            //                error = error + ((string.IsNullOrWhiteSpace(error)) ? "" : "<LineBreak />") + es.Reason;
            //            if (es.Oasis != null)
            //            {
            //                foreach (XElement xo in es.Oasis)
            //                {
            //                    string question = xo.Attribute("item") == null ? "" : xo.Attribute("item").Value;
            //                    string questionText = xo.Value == null ? "" : xo.Value;
            //                    if (string.IsNullOrWhiteSpace(questionText)) questionText = question;
            //                    if (string.IsNullOrWhiteSpace(questionText) == false)
            //                        error = error + ((string.IsNullOrWhiteSpace(error)) ? "" : "<LineBreak />") + questionText;
            //                }
            //            }
            //            if (string.IsNullOrWhiteSpace(es.Explanation) == false)
            //                error = error + ((string.IsNullOrWhiteSpace(error)) ? "" : "<LineBreak />") + es.Explanation;

            //            if ((es.Oasis != null) && (string.IsNullOrWhiteSpace(error) == false))
            //            {
            //                foreach (XElement xo in es.Oasis)
            //                {
            //                    string question = xo.Attribute("item") == null ? "" : xo.Attribute("item").Value;
            //                    if (question.Length > 5) question = question.Substring(0, 5);
            //                    if ((question == "M1022") || (question == "M1024")) question = "M1020";
            //                    if ((question == "M1023") || (question == "M1025")) question = "M1021";
            //                    oasisAuditErrors.Add(new OasisAuditError() { Question = question, ErrorParagraphText = error });
            //                }
            //            }
            //        }
            //    }
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(String.Format("Error OasisManager.OasisAuditsCheck: Exception: {0}.  Contact your system administrator.", e.Message));

            //}
            //return oasisAuditErrors;
        }

        private string[] M1311SequenceDesc =
        {
            "", "A1. Stage 2", "A2. Stage 2 at SOC/ROC", "B1. Stage 3", "B2. Stage 3 at SOC/ROC", "C1. Stage 4",
            "C2. Stage 4 at SOC/ROC", "D1. Unstageable non-removable dressing",
            "D2. Unstageable non-removable dressing at SOC/ROC", "E1. Unstageable slough/eschar",
            "E2. Unstageable slough/eschar at SOC/ROC", "F1. Unstageable deep tissue injury",
            "F2. Unstageable deep tissue injury at SOC/ROC"
        };

        private OasisManagerAnswer GetOasisManagerAnswerAtSequence(OasisManagerQuestion omq, int Sequence)
        {
            if ((omq == null) || omq.OasisManagerAnswers == null)
            {
                return null;
            }

            foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                if ((oamc != null) && (oamc.OasisAnswer != null) && (oamc.OasisAnswer.CachedOasisLayout != null) &&
                    (oamc.OasisAnswer.Sequence == Sequence))
                {
                    return oamc;
                }

            return null;
        }

        public bool ValidateQuestion(OasisManagerQuestion omq, List<SectionUI> sections,
            List<OasisAuditError> oasisAuditErrors, out string OasisSection)
        {
            OasisManagerAnswer oa = null;
            OasisLayout ol = null;
            string r = null;
            OasisSection = null;
            bool found = false;
            omq.ErrorText = null;
            if (CurrentEncounterOasis.BypassFlag == true)
            {
                return true;
            }

            if (omq.Hidden)
            {
                return true;
            }

            if (omq.OasisQuestion == null)
            {
                return true;
            }

            if (omq.OasisQuestion.CachedOasisLayout == null)
            {
                return true;
            }

            string question = (string.IsNullOrWhiteSpace(omq.OasisQuestion.Question))
                ? "?"
                : omq.OasisQuestion.Question;
            switch ((OasisType)omq.OasisQuestion.CachedOasisLayout.Type)
            {
                case OasisType.Text:
                case OasisType.Date:
                case OasisType.Radio:
                case OasisType.RadioHorizontal:
                case OasisType.LivingArrangement:
                    oa = omq.OasisManagerAnswers.FirstOrDefault();
                    if ((oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null) &&
                        (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null))
                    {
                        omq.ErrorText = ErrorTextRequired(question);
                    }
                    else if ((oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null))
                    {
                        ValidateQuestionSpecial(question, omq);
                    }

                    break;
                case OasisType.TrackingSheet:
                    if (question == "M0010" || question == "M0014" || question == "M0066")
                    {
                        // M0010, M0014 and M0066 are optional
                    }
                    else if (question == "M0040")
                    {
                        // Lastname is required
                        ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0040_PAT_LNAME");
                        if (GetResponse(ol) == null)
                        {
                            omq.ErrorText = string.Format("The M0040 patient last name question is required", question);
                        }
                    }
                    else if (question == "M0016" || question == "M0020" || question == "M0030" || question == "M0050" ||
                             question == "M0060" || question == "M0069" || question == "M0080" || question == "M0090" ||
                             question == "M00100")
                    {
                        oa = omq.OasisManagerAnswers.FirstOrDefault();
                        if (oa != null)
                        {
                            if (oa.OasisAnswer != null)
                            {
                                if (oa.OasisAnswer.CachedOasisLayout != null)
                                {
                                    if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                    {
                                        omq.ErrorText = ErrorTextRequired(question);
                                    }
                                }
                            }
                        }
                    }
                    else if (question == "M0018" || question == "M0032" || question == "M0063" || question == "M0064" ||
                             question == "M0065")
                    {
                        oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.IsType(OasisType.CheckBoxExclusive)))
                            .FirstOrDefault();
                        if (oa != null)
                        {
                            if (oa.OasisAnswer != null)
                            {
                                if (oa.OasisAnswer.CachedOasisLayout != null)
                                {
                                    string check = GetResponse(oa.OasisAnswer.CachedOasisLayout);
                                    if (check == null)
                                    {
                                        SetCheckBoxResponse(false, oa.OasisAnswer);
                                    }

                                    if (check != "1")
                                    {
                                        OasisManagerAnswer oaDateOrText = omq.OasisManagerAnswers
                                            .Where(a => (a.OasisAnswer.IsType(OasisType.CheckBoxExclusive) == false))
                                            .FirstOrDefault();
                                        if (oaDateOrText != null)
                                        {
                                            if (oaDateOrText.OasisAnswer != null)
                                            {
                                                if (oaDateOrText.OasisAnswer.CachedOasisLayout != null)
                                                {
                                                    if (GetResponse(oaDateOrText.OasisAnswer.CachedOasisLayout) == null)
                                                    {
                                                        omq.ErrorText = ErrorTextRequired(question);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    break;
                case OasisType.CheckBoxHeader:
                    found = false;
                    foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            r = GetResponse(oamc.OasisAnswer.CachedOasisLayout);
                            if ((r == "1") || (r == OASIS_DASH) || (r == OASIS_EQUAL))
                            {
                                found = true;
                            }

                            if ((oamc.OasisAnswer.CachedOasisLayout.CMSField == "M1028_ACTV_DIAG_NONE") && (r == "00"))
                            {
                                found = true; // "00" is a valid response
                            }

                            if ((oamc.OasisAnswer.CachedOasisLayout.CMSField == "GG0110DASH") &&
                                (r == new String(OASIS_DASHCHAR, oamc.OasisAnswer.CachedOasisLayout.Length)))
                            {
                                found = true; // all dashes is valid response
                            }

                            if ((r == null) && (oamc.OasisAnswer.CachedOasisLayout.CMSField != "GG0110DASH"))
                            {
                                SetCheckBoxResponse(false, oamc.OasisAnswer);
                            }
                        }

                    if (found == false)
                    {
                        omq.ErrorText =
                            ErrorTextRequired(
                                question); // M1028 is required as well (we embedded "00" and dash in the list of valid checkbox responses)
                    }

                    break;
                case OasisType.DateNAUK:
                case OasisType.TextNAUK:
                    oa = omq.OasisManagerAnswers.Where(a =>
                            ((a.OasisAnswer.IsType(OasisType.CheckBoxExclusive) && (a.OasisAnswer.RFAs != null))))
                        .FirstOrDefault();
                    if ((oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null))
                    {
                        string check = GetResponse(oa.OasisAnswer.CachedOasisLayout);
                        if (check == null)
                        {
                            SetCheckBoxResponse(false, oa.OasisAnswer);
                        }

                        if (check != "1")
                        {
                            OasisManagerAnswer oaDateOrText = omq.OasisManagerAnswers
                                .Where(a => (a.OasisAnswer.IsType(OasisType.CheckBoxExclusive) == false))
                                .FirstOrDefault();
                            if ((oaDateOrText != null) && (oaDateOrText.OasisAnswer != null) &&
                                (oaDateOrText.OasisAnswer.CachedOasisLayout != null))
                            {
                                if (GetResponse(oaDateOrText.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = ErrorTextRequired(question);
                                }
                            }
                        }
                    }

                    break;
                case OasisType.ICD:
                case OasisType.ICD10:
                    List<OasisManagerAnswer> oaList = omq.OasisManagerAnswers
                        .Where(a => (a.OasisAnswer.IsType(OasisType.CheckBoxExclusive))).ToList();
                    found = false;
                    foreach (OasisManagerAnswer oamc in oaList)
                        if (oamc != null)
                        {
                            if (oamc.OasisAnswer != null)
                            {
                                if (oamc.OasisAnswer.CachedOasisLayout != null)
                                {
                                    r = GetResponse(oamc.OasisAnswer.CachedOasisLayout);
                                    if (r == "1")
                                    {
                                        found = true;
                                    }

                                    if (r == null)
                                    {
                                        SetCheckBoxResponse(false, oamc.OasisAnswer);
                                    }
                                }
                            }
                        }

                    if (found == false)
                    {
                        oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 1)).FirstOrDefault();
                        if (oa != null)
                        {
                            if (oa.OasisAnswer != null)
                            {
                                if (oa.OasisAnswer.CachedOasisLayout != null)
                                {
                                    if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                    {
                                        omq.ErrorText = ErrorTextRequired(question);
                                    }
                                    else
                                    {
                                        if ((CurrentEncounter.FullValidationOASIS &&
                                             (CurrentEncounter.EncounterStatus !=
                                              (int)EncounterStatusType.CoderReview)) ||
                                            (CurrentEncounter.EncounterStatus ==
                                             (int)EncounterStatusType.CoderReviewEdit))
                                        {
                                            oaList = omq.OasisManagerAnswers.Where(a =>
                                                ((a.OasisAnswer.IsType(OasisType.ICD)) ||
                                                 (a.OasisAnswer.IsType(OasisType.ICD10)))).ToList();
                                            foreach (OasisManagerAnswer oamc in oaList)
                                                if (oamc != null)
                                                {
                                                    if (oamc.OasisAnswer != null)
                                                    {
                                                        if (oamc.OasisAnswer.CachedOasisLayout != null)
                                                        {
                                                            r = GetResponse(oamc.OasisAnswer.CachedOasisLayout);
                                                            if (r != null)
                                                            {
                                                                if (r.Contains("000.00"))
                                                                {
                                                                    omq.ErrorText = ErrorTextDummyDiagnosis(question);
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    break;
                case OasisType.ICDMedical:
                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1020_PRIMARY_DIAG_ICD");
                    r = GetResponse(ol);
                    if (r == null)
                    {
                        omq.ErrorText = string.Format("The M1020 question is required", question);
                    }
                    else if (r.Contains("V") == false)
                    {
                        ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1020_PRIMARY_DIAG_SEVERITY");
                        if (GetResponse(ol) == null)
                        {
                            omq.ErrorText = string.Format("The M1020 severity question is required", question);
                        }
                    }

                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG1_ICD");
                    r = GetResponse(ol);
                    if (r != null)
                    {
                        if ((r.Contains("E") || r.Contains("V")) == false)
                        {
                            ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG1_SEVERITY");
                            if (GetResponse(ol) == null)
                            {
                                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                "The M1022b severity question is required";
                            }
                        }
                    }

                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG2_ICD");
                    r = GetResponse(ol);
                    if (r != null)
                    {
                        if ((r.Contains("E") || r.Contains("V")) == false)
                        {
                            ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG2_SEVERITY");
                            if (GetResponse(ol) == null)
                            {
                                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                "The M1022c severity question is required";
                            }
                        }
                    }

                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG3_ICD");
                    r = GetResponse(ol);
                    if (r != null)
                    {
                        if ((r.Contains("E") || r.Contains("V")) == false)
                        {
                            ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG3_SEVERITY");
                            if (GetResponse(ol) == null)
                            {
                                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                "The M1022d severity question is required";
                            }
                        }
                    }

                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG4_ICD");
                    r = GetResponse(ol);
                    if (r != null)
                    {
                        if ((r.Contains("E") || r.Contains("V")) == false)
                        {
                            ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG4_SEVERITY");
                            if (GetResponse(ol) == null)
                            {
                                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                "The M1022e severity question is required";
                            }
                        }
                    }

                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG5_ICD");
                    r = GetResponse(ol);
                    if (r != null)
                    {
                        if ((r.Contains("E") || r.Contains("V")) == false)
                        {
                            ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1022_OTH_DIAG5_SEVERITY");
                            if (GetResponse(ol) == null)
                            {
                                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                "The M1022f severity question is required";
                            }
                        }
                    }

                    if ((CurrentEncounter.FullValidationOASIS &&
                         (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.CoderReview)) ||
                        (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.CoderReviewEdit))
                    {
                        oaList = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.IsType(OasisType.ICD))).ToList();
                        foreach (OasisManagerAnswer oamc in oaList)
                            if (oamc != null)
                            {
                                if (oamc.OasisAnswer != null)
                                {
                                    if (oamc.OasisAnswer.CachedOasisLayout != null)
                                    {
                                        r = GetResponse(oamc.OasisAnswer.CachedOasisLayout);
                                        if (r != null)
                                        {
                                            if (r.Contains("000.00"))
                                            {
                                                omq.ErrorText = omq.ErrorText +
                                                                ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                                ErrorTextDummyDiagnosis(question);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                    }

                    break;
                case OasisType.ICD10Medical:
                case OasisType.ICD10MedicalV2:
                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1021_PRIMARY_DIAG_ICD");
                    r = GetResponse(ol);
                    if (r == null)
                    {
                        omq.ErrorText = string.Format("The M1021 question is required", question);
                    }
                    else if (ICD10StartsWithVWXYZ(r) == false)
                    {
                        ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1021_PRIMARY_DIAG_SEVERITY");
                        if (GetResponse(ol) == null)
                        {
                            omq.ErrorText = string.Format("The M1021 severity question is required", question);
                        }
                    }

                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG1_ICD");
                    r = GetResponse(ol);
                    if ((string.IsNullOrWhiteSpace(r) == false) && (ICD10StartsWithVWXYZ(r) == false))
                    {
                        ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG1_SEVERITY");
                        if (GetResponse(ol) == null)
                        {
                            omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                            "The M1023b severity question is required";
                        }
                    }

                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG2_ICD");
                    r = GetResponse(ol);
                    if ((string.IsNullOrWhiteSpace(r) == false) && (ICD10StartsWithVWXYZ(r) == false))
                    {
                        ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG2_SEVERITY");
                        if (GetResponse(ol) == null)
                        {
                            omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                            "The M1023c severity question is required";
                        }
                    }

                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG3_ICD");
                    r = GetResponse(ol);
                    if ((string.IsNullOrWhiteSpace(r) == false) && (ICD10StartsWithVWXYZ(r) == false))
                    {
                        ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG3_SEVERITY");
                        if (GetResponse(ol) == null)
                        {
                            omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                            "The M1023d severity question is required";
                        }
                    }

                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG4_ICD");
                    r = GetResponse(ol);
                    if ((string.IsNullOrWhiteSpace(r) == false) && (ICD10StartsWithVWXYZ(r) == false))
                    {
                        ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG4_SEVERITY");
                        if (GetResponse(ol) == null)
                        {
                            omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                            "The M1023e severity question is required";
                        }
                    }

                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG5_ICD");
                    r = GetResponse(ol);
                    if ((string.IsNullOrWhiteSpace(r) == false) && (ICD10StartsWithVWXYZ(r) == false))
                    {
                        ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1023_OTH_DIAG5_SEVERITY");
                        if (GetResponse(ol) == null)
                        {
                            omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                            "The M1023f severity question is required";
                        }
                    }

                    if ((CurrentEncounter.FullValidationOASIS &&
                         (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.CoderReview)) ||
                        (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.CoderReviewEdit))
                    {
                        oaList = omq.OasisManagerAnswers.Where(a =>
                                ((a.OasisAnswer.IsType(OasisType.ICD)) || (a.OasisAnswer.IsType(OasisType.ICD10))))
                            .ToList();
                        foreach (OasisManagerAnswer oamc in oaList)
                            if (oamc != null)
                            {
                                if (oamc.OasisAnswer != null)
                                {
                                    if (oamc.OasisAnswer.CachedOasisLayout != null)
                                    {
                                        r = GetResponse(oamc.OasisAnswer.CachedOasisLayout);
                                        if (r != null)
                                        {
                                            if (r.Contains("000.00"))
                                            {
                                                omq.ErrorText = omq.ErrorText +
                                                                ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                                ErrorTextDummyDiagnosis(question);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                    }

                    break;
                case OasisType.RadioWithDate:
                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.IsType(OasisType.RadioWithDate)))
                        .FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                r = GetResponse(oa.OasisAnswer.CachedOasisLayout);
                                if (r == null)
                                {
                                    omq.ErrorText = ErrorTextRequired(question);
                                }
                                else
                                {
                                    if (r == "02")
                                    {
                                        ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey,
                                            "M1307_OLDST_STG2_ONST_DT");
                                        if (GetResponse(ol) == null)
                                        {
                                            omq.ErrorText =
                                                string.Format("The {0} question pressure ulcer date is required",
                                                    question);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    break;
                case OasisType.Synopsys:
                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 1)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = string.Format("The {0}a question is required", question);
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 4)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    string.Format("The {0}b question is required", question);
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 7)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    string.Format("The {0}c question is required", question);
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 10)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    string.Format("The {0}d question is required", question);
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 13)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    string.Format("The {0}e question is required", question);
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 16)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    string.Format("The {0}f question is required", question);
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 19)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    string.Format("The {0}g question is required", question);
                                }
                            }
                        }
                    }

                    break;
                case OasisType.PriorADL:
                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 1)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = "The M1900a question is required";
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 4)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    "The M1900b question is required";
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 7)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    "The M1900c question is required";
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 10)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    "The M1900d question is required";
                                }
                            }
                        }
                    }

                    break;
                case OasisType.PoorMed:
                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 1)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = "The M2040a question is required";
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 5)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    "The M2040b question is required";
                                }
                            }
                        }
                    }

                    break;
                case OasisType.CareManagement:
                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 1)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = "The M2100a question is required";
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 7)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    "The M2100b question is required";
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 13)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    "The M2100c question is required";
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 19)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    "The M2100d question is required";
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 25)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    "The M2100e question is required";
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 31)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    "The M2100f question is required";
                                }
                            }
                        }
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 37)).FirstOrDefault();
                    if (oa != null)
                    {
                        if (oa.OasisAnswer != null)
                        {
                            if (oa.OasisAnswer.CachedOasisLayout != null)
                            {
                                if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    "The M2100g question is required";
                                }
                            }
                        }
                    }

                    break;
                case OasisType.CareManagement_C1:
                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 1)).FirstOrDefault();
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_ADL") &&
                        (oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null) &&
                        (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null))
                    {
                        omq.ErrorText = "The M2102a question is required";
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 6)).FirstOrDefault();
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_IADL") &&
                        (oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null) &&
                        (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null))
                    {
                        omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                        "The M2102b question is required";
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 11)).FirstOrDefault();
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_MDCTN") &&
                        (oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null) &&
                        (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null))
                    {
                        omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                        "The M2102c question is required";
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 16)).FirstOrDefault();
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_PRCDR") &&
                        (oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null) &&
                        (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null))
                    {
                        omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                        "The M2102d question is required";
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 21)).FirstOrDefault();
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_EQUIP") &&
                        (oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null) &&
                        (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null))
                    {
                        omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                        "The M2102e question is required";
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 26)).FirstOrDefault();
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_SPRVSN") &&
                        (oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null) &&
                        (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null))
                    {
                        omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                        "The M2102f question is required";
                    }

                    oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.Sequence == 31)).FirstOrDefault();
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M2102_CARE_TYPE_SRC_ADVCY") &&
                        (oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null) &&
                        (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null))
                    {
                        omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                        "The M2102g question is required";
                    }

                    break;
                case OasisType.DepressionScreening:
                    ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1730_STDZ_DPRSN_SCRNG");
                    if (ol != null)
                    {
                        r = GetResponse(ol);
                        if (r == null)
                        {
                            omq.ErrorText = ErrorTextRequired(question);
                        }
                        else
                        {
                            if (r == "01")
                            {
                                ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1730_PHQ2_LACK_INTRST");
                                if (GetResponse(ol) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    "The M1730 PHQ a) question is required";
                                }

                                ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1730_PHQ2_DPRSN");
                                if (GetResponse(ol) == null)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    "The M1730 PHQ b) question is required";
                                }
                            }
                        }
                    }

                    break;
                case OasisType.PressureUlcer:
                    foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            if (GetResponse(oamc.OasisAnswer.CachedOasisLayout) == null)
                            {
                                string[] sequenceDesc =
                                {
                                    "", "a. Stage II column 1", "a. Stage II column 2", "b. Stage III column 1",
                                    "b. Stage III column 2", "c. Stage IV column 1", "c. Stage IV column 2",
                                    "d.1 Unstageable column 1", "d.1 Unstageable column 2", "d.2 Unstageable column 1",
                                    "d.2 Unstageable column 2", "d.3 Unstageable column 1", "d.3 Unstageable column 2"
                                };
                                int seq = oamc.OasisAnswer.Sequence;
                                if ((seq % 2 == 1) || ((RFA == "04" || RFA == "05" || RFA == "09") && (seq % 2 == 0)))
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    string.Format("The M1308 {0} question is required",
                                                        sequenceDesc[seq]);
                                }
                            }
                        }

                    break;
                case OasisType.WoundDimension:
                    if (IsWoundDimensionsVisible)
                    {
                        oa = omq.OasisManagerAnswers.FirstOrDefault();
                        if (oa != null)
                        {
                            if (oa.OasisAnswer != null)
                            {
                                if (oa.OasisAnswer.CachedOasisLayout != null)
                                {
                                    if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                                    {
                                        omq.ErrorText = ErrorTextRequired(question);
                                    }
                                }
                            }
                        }
                    }

                    break;
                case OasisType.HISTrackingSheet:
                    if (question == "A0100A" || question == "A0600A" || question == "A0600B" || question == "A0700")
                    {
                        // are optional
                    }
                    else if (question == "A0500")
                    {
                        // Firstname is required
                        ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0500A");
                        if (GetResponse(ol) == null)
                        {
                            omq.ErrorText =
                                string.Format("The A0500 question, patient first and last name are required", question);
                        }

                        ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0500C");
                        if (GetResponse(ol) == null)
                        {
                            omq.ErrorText =
                                string.Format("The A0500 question, patient first and last name are required", question);
                        }
                    }
                    else if (question == "A0100B" || ((question == "A0205") && (RFA == "01")) || question == "A0220" ||
                             ((question == "A0245") && (RFA == "01")) || question == "A0250" ||
                             ((question == "A0270") && (RFA == "09")) || ((question == "A0550") && (RFA == "01")) ||
                             question == "A0800" || question == "A0900")
                    {
                        // are required
                        oa = omq.OasisManagerAnswers.FirstOrDefault();
                        if ((oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null) &&
                            (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null))
                        {
                            omq.ErrorText = ErrorTextRequired(question);
                        }
                    }

                    break;
                case OasisType.PressureUlcerWorse:
                    foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            if (GetResponse(oamc.OasisAnswer.CachedOasisLayout) == null)
                            {
                                string[] sequenceDesc =
                                    { "", "a. Stage II", "b. Stage III", "c. Stage IV", "d. Unstageable" };
                                int seq = oamc.OasisAnswer.Sequence;
                                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                string.Format("The M1309 {0} question is required", sequenceDesc[seq]);
                            }
                        }

                    break;
                case OasisType.PressureUlcer_C1:
                    foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            if (GetResponse(oamc.OasisAnswer.CachedOasisLayout) == null)
                            {
                                string[] sequenceDesc =
                                {
                                    "", "a. Stage II", "b. Stage III", "c. Stage IV", "d.1 Unstageable",
                                    "d.2 Unstageable", "d.3 Unstageable"
                                };
                                int seq = oamc.OasisAnswer.Sequence;
                                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                string.Format("The M1308 {0} question is required", sequenceDesc[seq]);
                            }
                        }

                    break;
                case OasisType.PressureUlcer_C2:
                    foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            if (GetResponse(oamc.OasisAnswer.CachedOasisLayout) == null)
                            {
                                int seq = oamc.OasisAnswer.Sequence;
                                if ((seq % 2 == 1) || ((RFA == "04" || RFA == "05" || RFA == "09") && (seq % 2 == 0)))
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    string.Format("The M1311 {0} question is required",
                                                        M1311SequenceDesc[seq]);
                                }
                            }
                        }

                    break;
                case OasisType.PressureUlcer_C3:
                    foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            if (GetResponse(oamc.OasisAnswer.CachedOasisLayout) == null)
                            {
                                int seq = oamc.OasisAnswer.Sequence;
                                if (seq % 2 == 1)
                                {
                                    omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                    string.Format("The M1311 {0} question is required",
                                                        M1311SequenceDesc[seq]);
                                }

                                if ((RFA == "09") && (seq % 2 == 0))
                                {
                                    // for discharge the number at SOC/ROC is only required if the number at discharge is not zero
                                    OasisManagerAnswer omaDSCH = GetOasisManagerAnswerAtSequence(omq, (seq - 1));
                                    if (omaDSCH != null)
                                    {
                                        string response = GetResponse(omaDSCH.OasisAnswer.CachedOasisLayout);
                                        if (string.IsNullOrWhiteSpace(response) ||
                                            ((string.IsNullOrWhiteSpace(response) == false) &&
                                             (response.Trim() != "0") && (response.Trim() != "00")))
                                        {
                                            omq.ErrorText = omq.ErrorText +
                                                            ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                            string.Format("The M1311 {0} question is required",
                                                                M1311SequenceDesc[seq]);
                                        }
                                    }
                                }
                            }
                        }

                    break;
                case OasisType.PressureUlcerWorse_C2:
                    foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            if (GetResponse(oamc.OasisAnswer.CachedOasisLayout) == null)
                            {
                                string[] sequenceDesc =
                                {
                                    "", "a. Stage 2", "b. Stage 3", "c. Stage 4",
                                    "d. Unstageable non-removable dressing", "e. Unstageable slough/eschar",
                                    "f. deep tissue injury"
                                };
                                int seq = oamc.OasisAnswer.Sequence;
                                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                string.Format(
                                                    "The M1313 {0} question is required, it must be a whole number or must be a dash (-) to indicate 'Not Assessed (no information)'",
                                                    sequenceDesc[seq]);
                            }
                        }

                    break;
                case OasisType.HeightWeight_C2:
                    foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            if (GetResponse(oamc.OasisAnswer.CachedOasisLayout) == null)
                            {
                                string[] sequenceDesc = { "", "a. Height", "b. Weight" };
                                int seq = oamc.OasisAnswer.Sequence;
                                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                string.Format(
                                                    "The M1060 {0} question is required, it must be a whole number or must be a dash (-) to indicate 'Not Assessed (no information)'",
                                                    sequenceDesc[seq]);
                            }
                        }

                    break;
                case OasisType.GG0170C_C2:
                    oa = omq.OasisManagerAnswers
                        .Where(o => o.OasisAnswer.CachedOasisLayout.CMSField == "GG0170C_MOBILITY_SOCROC_PERF")
                        .FirstOrDefault();
                    if ((oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null))
                    {
                        if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                        {
                            omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                            "The GG0170c 1. SOC/ROC Performance question is required";
                        }
                    }

                    oa = omq.OasisManagerAnswers
                        .Where(o => o.OasisAnswer.CachedOasisLayout.CMSField == "GG0170C_MOBILITY_DSCHG_GOAL")
                        .FirstOrDefault();
                    if ((oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null))
                    {
                        if (GetResponse(oa.OasisAnswer.CachedOasisLayout) == null)
                        {
                            omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                            "The GG0170c 2. Discharge Goal question is required";
                        }
                    }

                    break;
                case OasisType.TopLegend:
                case OasisType.LeftLegend:
                    int? maxRow = omq.OasisManagerAnswers.Max(m => m.OasisAnswer.SubQuestionRow);
                    int? maxColumn = omq.OasisManagerAnswers.Max(m => m.OasisAnswer.SubQuestionColumn);
                    if ((maxRow == null) || (maxColumn == null))
                    {
                        break;
                    }

                    for (int row = 1; row <= (int)maxRow; row++)
                    {
                        for (int column = 1; column <= (int)maxColumn; column++)
                        {
                            OasisManagerAnswer omaAnchor = omq.OasisManagerAnswers
                                .Where(p => ((p.OasisAnswer.SubQuestionRow == row) &&
                                             (p.OasisAnswer.SubQuestionColumn == column)))
                                .OrderBy(p => p.OasisAnswer.Sequence).FirstOrDefault();
                            if ((omaAnchor != null) && omaAnchor.Enabled &&
                                (GetResponse(omaAnchor.OasisAnswer.CachedOasisLayout) == null))
                            {
                                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                "The " + omaAnchor.OasisAnswer.SubQuestionLabelAndTextShort(omq) +
                                                " subquestion is required";
                            }
                        }
                    }

                    break;
                case OasisType.ServiceUtilization_10:
                    foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            if (GetResponse(oamc.OasisAnswer.CachedOasisLayout) == null)
                            {
                                string[] sequenceDesc =
                                {
                                    "", "A. RN on A0270", "A. RN on A0270-1", "A. RN on A0270-2",
                                    "B. Physician on A0270", "B. Physician on A0270-1", "B. Physician on A0270-2",
                                    "C. MSW on A0270", "C. MSW on A0270-1", "C. MSW on A0270-2", "D. SC on A0270",
                                    "D. SC on A0270-1", "D. SC on A0270-2", "E. LPN on A0270", "E. LPN on A0270-1",
                                    "E. LPN on A0270-2", "F. Aide on A0270", "F. Aide on A0270-1", "F. Aide on A0270-2"
                                };
                                int seq = oamc.OasisAnswer.Sequence;
                                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                string.Format(
                                                    "The O5010 {0} question is required, it must be a whole number 0 thru 9",
                                                    sequenceDesc[seq]);
                            }
                        }

                    break;
                case OasisType.ServiceUtilization_30:
                    foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            if (GetResponse(oamc.OasisAnswer.CachedOasisLayout) == null)
                            {
                                string[] sequenceDesc =
                                {
                                    "", "A. RN on A0270-3", "A. RN on A0270-4", "A. RN on A0270-5", "A. RN on A0270-6",
                                    "B. Physician on A0270-3", "B. Physician on A0270-4", "B. Physician on A0270-5",
                                    "B. Physician on A0270-6", "C. MSW on A0270-3", "C. MSW on A0270-4",
                                    "C. MSW on A0270-5", "C. MSW on A0270-6", "D. SC on A0270-3", "D. SC on A0270-4",
                                    "D. SC on A0270-5", "D. SC on A0270-6", "E. LPN on A0270-3", "E. LPN on A0270-4",
                                    "E. LPN on A0270-5", "E. LPN on A0270-6", "F. Aide on A0270-3",
                                    "F. Aide on A0270-4", "F. Aide on A0270-5", "F. Aide on A0270-6"
                                };
                                int seq = oamc.OasisAnswer.Sequence;
                                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                                string.Format(
                                                    "The O5030 {0} question is required, it must be a whole number 0 thru 9",
                                                    sequenceDesc[seq]);
                            }
                        }

                    break;
                default:
                    MessageBox.Show(String.Format(
                        "Error OasisManager.ValidateQuestion: {0} is not a valid question.  Contact your system administrator.",
                        omq.OasisQuestion.Question));
                    break;
            }

            List<OasisAuditError> oaeList = oasisAuditErrors.Where(oae => oae.Question == question).ToList();
            if (oaeList != null)
            {
                foreach (OasisAuditError oae in oaeList)
                    omq.ErrorText = omq.ErrorText +
                                    ((string.IsNullOrWhiteSpace(omq.ErrorText)) ? "" : "<LineBreak /><LineBreak />") +
                                    oae.ErrorParagraphText;
            }

            if ((omq.ErrorText != null))
            {
                OasisSurveyGroupQuestion osg = omq.OasisQuestion.OasisSurveyGroupQuestion
                    .Where(o => o.OasisSurveyGroup.OasisSurvey.RFA == RFA).FirstOrDefault();
                if (osg != null)
                {
                    OasisSection = osg.OasisSurveyGroup.SectionLabel;
                }
            }

            return (omq.ErrorText == null) ? true : false;
        }

        public void ValidateQuestionSpecial(string Question, OasisManagerQuestion omq)
        {
            if (Question == "J0900C" || Question == "J0905")
            {
                ValidateQuestionSpecialJ0900CJ0905(Question, omq);
            }
        }

        public void ValidateQuestionSpecialJ0900CJ0905(string Question, OasisManagerQuestion omq)
        {
            // FATAL -  If J0900C = [1,2,3] then J0905 must be equal to [1] 
            if (IsQuestionInSurveyNotHidden("J0900C") == false)
            {
                return;
            }

            if (IsQuestionInSurveyNotHidden("J0905") == false)
            {
                return;
            }

            if (IsHISVersion2orHigher == false)
            {
                return;
            }

            OasisManagerQuestion oq = OasisManagerQuestions.Where(q => (q.OasisQuestion.Question == "J0900C"))
                .FirstOrDefault();
            if (oq == null)
            {
                return;
            }

            OasisManagerAnswer oa = oq.OasisManagerAnswers.FirstOrDefault();
            if ((oa == null) || (oa.OasisAnswer == null) || (oa.OasisAnswer.CachedOasisLayout == null))
            {
                return;
            }

            string J0900C = GetResponse(oa.OasisAnswer.CachedOasisLayout);
            oq = OasisManagerQuestions.Where(q => (q.OasisQuestion.Question == "J0905")).FirstOrDefault();
            if (oq == null)
            {
                return;
            }

            oa = oq.OasisManagerAnswers.FirstOrDefault();
            if ((oa == null) || (oa.OasisAnswer == null) || (oa.OasisAnswer.CachedOasisLayout == null))
            {
                return;
            }

            string J0905 = GetResponse(oa.OasisAnswer.CachedOasisLayout);
            if (((J0900C == "1") || (J0900C == "2") || (J0900C == "3")) && (J0905 != "1"))
            {
                omq.ErrorText = omq.ErrorText + ((omq.ErrorText == null) ? "" : "<LineBreak />") +
                                "If J0900C = [1,2,3] then J0905 must be equal to [1]";
            }
        }

        public void DefaultQuestion(OasisManagerQuestion omq)
        {
            OasisManagerAnswer oa = null;
            string r = null;
            if (CurrentEncounterOasis.BypassFlag == true)
            {
                return;
            }

            if (omq.Hidden)
            {
                return;
            }

            if (omq.OasisQuestion == null)
            {
                return;
            }

            if (omq.OasisQuestion.CachedOasisLayout == null)
            {
                return;
            }

            string question = (string.IsNullOrWhiteSpace(omq.OasisQuestion.Question))
                ? "?"
                : omq.OasisQuestion.Question;
            switch ((OasisType)omq.OasisQuestion.CachedOasisLayout.Type)
            {
                case OasisType.Text:
                case OasisType.Date:
                case OasisType.Radio:
                case OasisType.RadioHorizontal:
                case OasisType.LivingArrangement:
                    break;
                case OasisType.TrackingSheet:
                    if (question == "M0018" || question == "M0032" || question == "M0063" || question == "M0064" ||
                        question == "M0065")
                    {
                        oa = omq.OasisManagerAnswers.Where(a => (a.OasisAnswer.IsType(OasisType.CheckBoxExclusive)))
                            .FirstOrDefault();
                        if ((oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null))
                        {
                            string check = GetResponse(oa.OasisAnswer.CachedOasisLayout);
                            if (check == null)
                            {
                                SetCheckBoxResponse(false, oa.OasisAnswer);
                            }
                        }
                    }

                    break;
                case OasisType.CheckBoxHeader:
                    foreach (OasisManagerAnswer oamc in omq.OasisManagerAnswers)
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            r = GetResponse(oamc.OasisAnswer.CachedOasisLayout);
                            if (r == null)
                            {
                                SetCheckBoxResponse(false, oamc.OasisAnswer);
                            }
                        }

                    break;
                case OasisType.DateNAUK:
                case OasisType.TextNAUK:
                    oa = omq.OasisManagerAnswers.Where(a =>
                            ((a.OasisAnswer.IsType(OasisType.CheckBoxExclusive) && (a.OasisAnswer.RFAs != null))))
                        .FirstOrDefault();
                    if ((oa != null) && (oa.OasisAnswer != null) && (oa.OasisAnswer.CachedOasisLayout != null))
                    {
                        string check = GetResponse(oa.OasisAnswer.CachedOasisLayout);
                        if (check == null)
                        {
                            SetCheckBoxResponse(false, oa.OasisAnswer);
                        }
                    }

                    break;
                case OasisType.ICD:
                case OasisType.ICD10:
                    List<OasisManagerAnswer> oaList = omq.OasisManagerAnswers
                        .Where(a => (a.OasisAnswer.IsType(OasisType.CheckBoxExclusive))).ToList();
                    foreach (OasisManagerAnswer oamc in oaList)
                        if ((oamc != null) && (oamc.OasisAnswer != null) &&
                            (oamc.OasisAnswer.CachedOasisLayout != null))
                        {
                            r = GetResponse(oamc.OasisAnswer.CachedOasisLayout);
                            if (r == null)
                            {
                                SetCheckBoxResponse(false, oamc.OasisAnswer);
                            }
                        }

                    break;
                case OasisType.HeightWeight_C2:
                case OasisType.ICDMedical:
                case OasisType.ICD10Medical:
                case OasisType.ICD10MedicalV2:
                case OasisType.RadioWithDate:
                case OasisType.Synopsys:
                case OasisType.PriorADL:
                case OasisType.PoorMed:
                case OasisType.CareManagement:
                case OasisType.CareManagement_C1:
                case OasisType.DepressionScreening:
                case OasisType.PressureUlcer:
                case OasisType.WoundDimension:
                case OasisType.HISTrackingSheet:
                case OasisType.PressureUlcerWorse:
                case OasisType.PressureUlcer_C1:
                case OasisType.PressureUlcer_C2:
                case OasisType.PressureUlcerWorse_C2:
                case OasisType.ServiceUtilization_10:
                case OasisType.ServiceUtilization_30:
                case OasisType.GG0170C_C2:
                case OasisType.TopLegend:
                case OasisType.LeftLegend:
                case OasisType.PressureUlcer_C3:
                    break;
            }
        }

        private string ErrorTextRequired(string question)
        {
            return string.Format("The {0} question is required", question);
        }

        private string ErrorTextDummyDiagnosis(string question)
        {
            return string.Format("The dummy diagnosis (000.00) is not allowed", question);
        }

        private void InitializeB1RecordOASIS()
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (GetResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "CORRECTION_NUM")) != null)
            {
                return;
            }

            CurrentEncounterOasis.REC_ID = "B1";
            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "REC_ID"))
            {
                SetResponse("B1", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "REC_ID"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "CORRECTION_NUM"))
            {
                SetResponse("00", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "CORRECTION_NUM"));
            }

            OasisVersion ov = OasisCache.GetOasisVersionByVersionKey(OasisVersionKey);
            if ((ov != null) && (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD1")))
            {
                SetResponse(ov.VersionCD1, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD1"));
            }

            if ((ov != null) && (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD2")))
            {
                SetResponse(ov.VersionCD2, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD2"));
            }

            if ((ov != null) && (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "ITM_SET_VRSN_CD")))
            {
                SetResponse(ov.VersionCD1, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SET_VRSN_CD"));
            }

            if ((ov != null) && (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SPEC_VRSN_CD")))
            {
                SetResponse(ov.VersionCD2, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SPEC_VRSN_CD"));
            }

            Delta delta = DeltaCache.GetDelta();
            if (delta != null)
            {
                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFTW_ID"))
                {
                    SetResponse(delta.SFW_ID, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTW_ID"));
                }

                string virtuosoVersion = null;
                if ((DynamicFormViewModel != null) && (DynamicFormViewModel.Configuration != null))
                {
                    virtuosoVersion = DynamicFormViewModel.Configuration.VirtuosoVersion;
                }

                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFT_VER"))
                {
                    SetResponse(((string.IsNullOrWhiteSpace(virtuosoVersion)) ? "01.01" : virtuosoVersion),
                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFT_VER"));
                }

                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_VNDR_ID"))
                {
                    SetResponse(delta.SFW_ID, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_VNDR_ID"));
                }

                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_VNDR_NAME"))
                {
                    SetResponse(delta.SFW_NAME,
                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_VNDR_NAME"));
                }

                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_VNDR_EMAIL_ADR"))
                {
                    SetResponse(delta.SFW_EMAIL_ADR,
                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_VNDR_EMAIL_ADR"));
                }

                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_PROD_NAME"))
                {
                    SetResponse("Crescendo", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_PROD_NAME"));
                }

                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_PROD_VRSN_CD"))
                {
                    SetResponse(((string.IsNullOrWhiteSpace(virtuosoVersion)) ? "01.01" : virtuosoVersion),
                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_PROD_VRSN_CD"));
                }

                if ((string.IsNullOrWhiteSpace(delta.SFW_ID) == false) &&
                    (string.IsNullOrWhiteSpace(delta.SFW_NAME) == false) &&
                    (string.IsNullOrWhiteSpace(delta.SFW_EMAIL_ADR) == false))
                {
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFW_ID"))
                    {
                        SetResponse(delta.SFW_ID, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFW_ID"));
                    }

                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFW_NAME"))
                    {
                        SetResponse(delta.SFW_NAME, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFW_NAME"));
                    }

                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFW_EMAIL_ADR"))
                    {
                        SetResponse(delta.SFW_EMAIL_ADR,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFW_EMAIL_ADR"));
                    }

                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFW_PROD_NAME"))
                    {
                        SetResponse("Crescendo", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFW_PROD_NAME"));
                    }

                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SFW_PROD_VRSN_CD"))
                    {
                        SetResponse(((string.IsNullOrWhiteSpace(virtuosoVersion)) ? "01.01" : virtuosoVersion),
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFW_PROD_VRSN_CD"));
                    }
                }
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "DATA_END"))
            {
                SetResponse("%", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "DATA_END"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "DATA_END_INDICATOR"))
            {
                SetResponse("%", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "DATA_END_INDICATOR"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "CRG_RTN"))
            {
                SetResponse(Char.ToString('\r'),
                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "CRG_RTN")); // ASCII 013
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "LN_FD"))
            {
                SetResponse(char.ToString('\n'),
                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "LN_FD")); //ASCII 010
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "ASMT_SYS_CD"))
            {
                SetResponse("OASIS", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ASMT_SYS_CD"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "TRANS_TYPE_CD"))
            {
                SetResponse("1", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "TRANS_TYPE_CD"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "ITM_SBST_CD"))
            {
                SetResponse(CurrentEncounterOasis.ITM_SBST_CD,
                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SBST_CD"));
            }

            if (RFA != "03") // Do not default M0032 ROC Date to Not Applicable on Resumption of Care Oasis records
            {
                SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0032_ROC_DT_NA"));
            }

            if (HCFACode == "B")
            {
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M0080_ASSESSOR_DISCIPLINE", 2));
            }
            else if (HCFACode == "C")
            {
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M0080_ASSESSOR_DISCIPLINE", 3));
            }
            else if (HCFACode == "D")
            {
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M0080_ASSESSOR_DISCIPLINE", 4));
            }
            else
            {
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M0080_ASSESSOR_DISCIPLINE", 1));
            }

            if ((CurrentEncounter == null) || (CurrentEncounter.EncounterOrTaskStartDateAndTime == null))
            {
                SetDateResponse(DateTime.Today.Date,
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"), false);
            }
            else
            {
                SetDateResponse(((DateTimeOffset)CurrentEncounter.EncounterOrTaskStartDateAndTime).Date,
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"), false);
            }
        }

        private string[] OasisICDField => (OasisVersionUsingICD10) ? OasisICD10Field : OasisICD9Field;

        private string[] OasisICDSeverityField =>
            (OasisVersionUsingICD10) ? OasisICD10SeverityField : OasisICD9SeverityField;

        private string[] OasisICDPmt3Field => (OasisVersionUsingICD10) ? OasisICD10Pmt3Field : OasisICD9Pmt3Field;
        private string[] OasisICDPmt4Field => (OasisVersionUsingICD10) ? OasisICD10Pmt4Field : OasisICD9Pmt4Field;

        private string[] OasisICD9Field =
        {
            "M1020_PRIMARY_DIAG_ICD", "M1022_OTH_DIAG1_ICD", "M1022_OTH_DIAG2_ICD", "M1022_OTH_DIAG3_ICD",
            "M1022_OTH_DIAG4_ICD", "M1022_OTH_DIAG5_ICD"
        };

        private string[] OasisICD9SeverityField =
        {
            "M1020_PRIMARY_DIAG_SEVERITY", "M1022_OTH_DIAG1_SEVERITY", "M1022_OTH_DIAG2_SEVERITY",
            "M1022_OTH_DIAG3_SEVERITY", "M1022_OTH_DIAG4_SEVERITY", "M1022_OTH_DIAG5_SEVERITY"
        };

        private string[] OasisICD9Pmt3Field =
        {
            "M1024_PMT_DIAG_ICD_A3", "M1024_PMT_DIAG_ICD_B3", "M1024_PMT_DIAG_ICD_C3", "M1024_PMT_DIAG_ICD_D3",
            "M1024_PMT_DIAG_ICD_E3", "M1024_PMT_DIAG_ICD_F3"
        };

        private string[] OasisICD9Pmt4Field =
        {
            "M1024_PMT_DIAG_ICD_A4", "M1024_PMT_DIAG_ICD_B4", "M1024_PMT_DIAG_ICD_C4", "M1024_PMT_DIAG_ICD_D4",
            "M1024_PMT_DIAG_ICD_E4", "M1024_PMT_DIAG_ICD_F4"
        };

        private string[] OasisICD10Field =
        {
            "M1021_PRIMARY_DIAG_ICD", "M1023_OTH_DIAG1_ICD", "M1023_OTH_DIAG2_ICD", "M1023_OTH_DIAG3_ICD",
            "M1023_OTH_DIAG4_ICD", "M1023_OTH_DIAG5_ICD"
        };

        private string[] OasisICD10SeverityField =
        {
            "M1021_PRIMARY_DIAG_SEVERITY", "M1023_OTH_DIAG1_SEVERITY", "M1023_OTH_DIAG2_SEVERITY",
            "M1023_OTH_DIAG3_SEVERITY", "M1023_OTH_DIAG4_SEVERITY", "M1023_OTH_DIAG5_SEVERITY"
        };

        private string[] OasisICD10Pmt3Field =
        {
            "M1025_OPT_DIAG_ICD_A3", "M1025_OPT_DIAG_ICD_B3", "M1025_OPT_DIAG_ICD_C3", "M1025_OPT_DIAG_ICD_D3",
            "M1025_OPT_DIAG_ICD_E3", "M1025_OPT_DIAG_ICD_F3"
        };

        private string[] OasisICD10Pmt4Field =
        {
            "M1025_OPT_DIAG_ICD_A4", "M1025_OPT_DIAG_ICD_B4", "M1025_OPT_DIAG_ICD_C4", "M1025_OPT_DIAG_ICD_D4",
            "M1025_OPT_DIAG_ICD_E4", "M1025_OPT_DIAG_ICD_F4"
        };

        public bool DoingSurveyOASIS
        {
            get
            {
                if (IsOasisActive == false)
                {
                    return false;
                }

                if (CurrentEncounter.SYS_CDIsHospice)
                {
                    return false;
                }

                if (CurrentEncounterOasis == null)
                {
                    return false;
                }

                if (CurrentEncounterOasis.BypassFlag == true)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsOasisActive
        {
            get
            {
                if (IsBusy)
                {
                    return false;
                }

                if (CurrentPatient == null)
                {
                    MessageBox.Show("Error OasisManager: Internal setup error, CurrentPatient is null");
                    return false;
                }

                if (CurrentAdmission == null)
                {
                    MessageBox.Show("Error OasisManager: Internal setup error, CurrentAdmission is null");
                    return false;
                }

                if (CurrentEncounter == null)
                {
                    MessageBox.Show("Error OasisManager: Internal setup error, CurrentEncounter is null");
                    return false;
                }

                if (CurrentEncounterOasis == null)
                {
                    return false;
                }

                if (RFA == null)
                {
                    MessageBox.Show("Error OasisManager: Internal setup error, RFA is null");
                    return false;
                }

                return true;
            }
        }

        private void CleanupOasisQuestionAnswer()
        {
            if (OasisManagerQuestions != null)
            {
                foreach (OasisManagerQuestion q in OasisManagerQuestions)
                {
                    Messenger.Default.Unregister(q);
                    if (q.OasisManagerAnswers != null)
                    {
                        foreach (OasisManagerAnswer a in q.OasisManagerAnswers) Messenger.Default.Unregister(a);
                    }
                }
            }

            if (OasisManagerQuestions != null)
            {
                OasisManagerQuestions.ForEach(om => om.Cleanup());
                OasisManagerQuestions.Clear();
            }

            OasisManagerQuestions = new List<OasisManagerQuestion>();
            if (OasisQuestions != null)
            {
                OasisQuestions.Clear();
            }

            OasisQuestions = new List<OasisQuestion>();
        }

        public void CleanupOasisForVersionChange()
        {
            CleanupOasisQuestionAnswer();
        }

        private void CleanupOasis()
        {
            CleanupOasisQuestionAnswer();
            Messenger.Default.Unregister(this);
        }

        int __cleanupCount;

        public override void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            try
            {
                PropertyChanged -= OasisManager_PropertyChanged;
            }
            catch
            {
            }

            try
            {
                CleanupOasis();
            }
            catch
            {
            }

            if (CurrentOasisAlertManager != null)
            {
                CurrentOasisAlertManager.Cleanup();
            }

            CurrentOasisAlertManager = null;
            if (OasisManagerQuestions != null)
            {
                OasisManagerQuestions.ForEach(omq => omq.Cleanup());
                OasisManagerQuestions.Clear();
            }

            if (OasisQuestions != null)
            {
                OasisQuestions.Clear();
            }

            if (AdmissionPhysician != null)
            {
                AdmissionPhysician = null;
            }

            base.Cleanup();
        }

        private bool _isHeartFailureICD;

        public bool IsHeartFailureICD
        {
            get { return _isHeartFailureICD; }
            set
            {
                _isHeartFailureICD = value;
                base.RaisePropertyChanged("IsHeartFailureICD");
            }
        }

        private bool _isDiabeticICD;

        public bool IsDiabeticICD
        {
            get { return _isDiabeticICD; }
            set
            {
                _isDiabeticICD = value;
                base.RaisePropertyChanged("IsDiabeticICD");
            }
        }

        private bool _isDepressionICD;

        public bool IsDepressionICD
        {
            get { return _isDepressionICD; }
            set
            {
                _isDepressionICD = value;
                base.RaisePropertyChanged("IsDiabeticICD");
            }
        }

        private bool _isPDVorPADICD;

        public bool IsPDVorPADICD
        {
            get { return _isPDVorPADICD; }
            set
            {
                _isPDVorPADICD = value;
                base.RaisePropertyChanged("IsPDVorPADICD");
            }
        }

        public void SetupAdmissionDiagnosis(Admission admission)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (BypassMapping)
            {
                return;
            }

            IsDepressionICD = false;
            IsDiabeticICD = false;
            IsHeartFailureICD = false;
            IsPDVorPADICD = false;
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinicianOrICDCoderBypassOASISAssist)
            {
                return;
            }

            if (CurrentAdmission != admission)
            {
                return;
            }

            if (admission.AdmissionDiagnosis == null)
            {
                return;
            }

            ProcessFilteredAdmissionDiagnosisItems(admission.AdmissionDiagnosis,
                GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT")));
            if (CurrentFilteredAdmissionDiagnosis == null)
            {
                return;
            }

            int i = 0;
            string icd;
            string prevB1Record = CurrentEncounterOasis.B1Record;
            foreach (AdmissionDiagnosis pd in CurrentFilteredAdmissionDiagnosis)
            {
                if (pd.IsDepression)
                {
                    IsDepressionICD = true;
                }

                if (pd.IsDiabetic)
                {
                    IsDiabeticICD = true;
                }

                if (pd.IsHeartFailure)
                {
                    IsHeartFailureICD = true;
                }

                if (pd.IsPDVorPAD)
                {
                    IsPDVorPADICD = true;
                }
            }

            if ((RFA != "01") && (RFA != "03") && (RFA != "04") && (RFA != "05"))
            {
                return;
            }

            foreach (AdmissionDiagnosis pd in CurrentFilteredAdmissionDiagnosis)
                if (IsSurgicalICD(pd) == false)
                {
                    icd = GetResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDField[i]));
                    if (pd.Code != icd)
                    {
                        SetICDResponse(pd.Code, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, OasisICDField[i]));
                        DefaultDiagFromMostRecentB1Record(pd.Code, i, prevB1Record);
                    }

                    i++;
                    if (i > 5)
                    {
                        break;
                    }
                }

            for (int j = i; j <= 5; j++)
            {
                SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDField[j]));
                SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDSeverityField[j]));
                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt3Field[j]))
                {
                    SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt3Field[j]));
                }

                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt4Field[j]))
                {
                    SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt4Field[j]));
                }
            }

            if (IsQuestionInSurveyNotHidden("M1028"))
            {
                // Honor the dashes if you overwrote to them
                if (OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1028_ACTV_DIAG_NA", false) != null)
                {
                    if (GetCheckBoxResponse(
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1028_ACTV_DIAG_NA", false), false) ==
                        false)
                    {
                        // Default PVD_PAD and/or DM - or NONE if neither of those
                        SetCheckBoxResponse(IsPDVorPADICD,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1028_ACTV_DIAG_PVD_PAD"));
                        SetCheckBoxResponse(IsDiabeticICD,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1028_ACTV_DIAG_DM"));
                        if ((IsPDVorPADICD == false) && (IsDiabeticICD == false))
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1028_ACTV_DIAG_NONE"));
                        }
                    }
                }

                if (OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1028_ACTV_DIAG_DASH1", false) != null)
                {
                    if (GetCheckBoxResponse(
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1028_ACTV_DIAG_DASH1", false),
                            false) == false)
                    {
                        // Default PVD_PAD and/or DM - or NONE if neither of those
                        SetCheckBoxResponse(IsPDVorPADICD,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1028_ACTV_DIAG_PVD_PAD"));
                        SetCheckBoxResponse(IsDiabeticICD,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1028_ACTV_DIAG_DM"));
                        if ((IsPDVorPADICD == false) && (IsDiabeticICD == false))
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1028_ACTV_DIAG_NOA", false));
                        }
                    }
                }
            }
            // No special M1030 defaulting w.r.t M1030_IGNORE_EQUAL
        }

        private bool IsSurgicalICD(AdmissionDiagnosis icd)
        {
            if (icd == null)
            {
                return false;
            }

            return (icd.Diagnosis == false) ? true : false;
        }

        public void SetupPatientMedication(Patient patient)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (BypassMapping)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            if (CurrentPatient != patient)
            {
                return;
            }

            if (patient.PatientMedication == null)
            {
                return;
            }

            ProcessFilteredPatientMedicationItems(patient.PatientMedication);

            // Bypass setup unless new encounter 
            if (IsQuestionInSurveyNotHidden("M1032"))
            {
                bool response = false;
                if (CurrentFilteredPatientMedication != null)
                {
                    if (CurrentFilteredPatientMedication.OfType<PatientMedication>().Count() > 4)
                    {
                        response = true;
                    }
                }

                SetCheckBoxResponse(response,
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1032_HOSP_RISK_5PLUS_MDCTN"));
            }
        }

        public void SetupAdmissionPainLocation(Admission admission)
        {
            // M1240  rfa 01,03
            // M1242  rfa 01,03,04,05,09
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (BypassMapping)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden("M1242"))
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            if (CurrentAdmission != admission)
            {
                return;
            }

            if (admission.AdmissionPainLocation == null)
            {
                return;
            }

            ProcessFilteredAdmissionPainLocationItems(admission.AdmissionPainLocation);
            if (CurrentFilteredAdmissionPainLocation == null)
            {
                return;
            }

            IQueryable<AdmissionPainLocation> painList =
                CurrentFilteredAdmissionPainLocation.OfType<AdmissionPainLocation>().AsQueryable();
            if (painList == null)
            {
                return;
            }

            if (painList.Any() == false)
            {
                return;
            }

            AdmissionPainLocation apl = painList.Where(p => (p.PainInterferenceIsNullOrWhiteSpaceOrNone == false))
                .FirstOrDefault();
            if (apl == null)
            {
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1242_PAIN_FREQ_ACTVTY_MVMT", 2));
                return;
            }

            int countLess = painList.Where(p => (p.PainFrequencyLess == true)).Count();
            int countDaily = painList.Where(p => (p.PainFrequencyDaily == true)).Count();
            int countAll = painList.Where(p => (p.PainFrequencyAll == true)).Count();
            if ((countDaily + countAll == 0) && (countLess > 0))
            {
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1242_PAIN_FREQ_ACTVTY_MVMT", 3));
                return;
            }

            if ((countDaily > 0) && (countAll == 0))
            {
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1242_PAIN_FREQ_ACTVTY_MVMT", 4));
                return;
            }

            if (countAll > 0)
            {
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1242_PAIN_FREQ_ACTVTY_MVMT", 5));
            }
        }

        public void SetupAdmissionWoundSite(Admission admission)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (BypassMapping)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            if (CurrentAdmission != admission)
            {
                return;
            }

            if (admission.AdmissionWoundSite == null)
            {
                return;
            }

            ProcessFilteredAdmissionWoundSiteItems(admission.AdmissionWoundSite);
            if (CurrentFilteredAdmissionWoundSite == null)
            {
                return;
            }

            IQueryable<AdmissionWoundSite> woundList =
                CurrentFilteredAdmissionWoundSite.OfType<AdmissionWoundSite>().AsQueryable();
            if (woundList == null)
            {
                return;
            }

            string prevB1Record = CurrentEncounterOasis.B1Record;

            // M1300, M1302  rfa 01/03 
            // M1306, M1308orM1311, M1322, M1324 M1330, M1332, M1334, M1340, M1342, M1350  rfa 01/03/04/05/09  M1308orM1311 column 2, 04/05/09 only
            // M1307  rfa 09
            // M1310,M1312,M1214,M1320  rfa 01/03/09

            // M1300_PRSR_ULCR_RISK_ASMT is set thru generic mapping via braden scale quesion 
            // M1302_RISK_OF_PRSR_ULCR is cleared if need be when M1300_PRSR_ULCR_RISK_ASMT was set 

            // M1306
            if (IsQuestionInSurveyNotHidden("M1306"))
            {
                AdmissionWoundSite awsM1306 = woundList.Where(w => (w.IsUnhealingPressureUlcerStageIIorHigher == true))
                    .FirstOrDefault();
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1306_UNHLD_STG2_PRSR_ULCR",
                        ((awsM1306 == null) ? 1 : 2)));

                if (awsM1306 != null)
                {
                    // Answer the rest of the Unhealed Pressure Ulcer StageII or Higher questions

                    if (IsQuestionInSurveyNotHidden("M1307"))
                    {
                        // What if multiples were arred on same encounter - this takes only the first one added
                        AdmissionWoundSite awsM1307 = woundList.Where(w => (w.IsUnhealingPressureUlcerStageII == true))
                            .OrderByDescending(w => w.UpdatedDate).FirstOrDefault();
                        if (awsM1307 == null)
                        {
                            // NA - No non-epith Stage II
                            SetRadioResponse(true,
                                OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey,
                                    "M1307_OLDST_STG2_AT_DSCHRG", 3));
                        }
                        else
                        {
                            if (CurrentAdmission.Encounter != null)
                            {
                                // Most recent SOC/ROC
                                Encounter e = MostRecentSOCROCOasisEncounter;
                                if (e != null)
                                {
                                    if (e.EncounterWoundSite != null)
                                    {
                                        EncounterWoundSite ewsM1307_2 = e.EncounterWoundSite.Where(ew =>
                                                ((ew.AdmissionWoundSiteKey == awsM1307.AdmissionWoundSiteKey) ||
                                                 (ew.AdmissionWoundSiteKey == awsM1307.HistoryKey) ||
                                                 (ew.AdmissionWoundSite.HistoryKey == awsM1307.HistoryKey)))
                                            .FirstOrDefault();
                                        if (ewsM1307_2 == null)
                                        {
                                            // 2 - Developed since
                                            SetRadioResponse(true,
                                                OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey,
                                                    "M1307_OLDST_STG2_AT_DSCHRG", 2));
                                            SetDateResponse(awsM1307.UpdatedDate,
                                                OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey,
                                                    "M1307_OLDST_STG2_ONST_DT", 4));
                                        }
                                        else
                                        {
                                            // 1- was present
                                            SetRadioResponse(true,
                                                OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey,
                                                    "M1307_OLDST_STG2_AT_DSCHRG", 1));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (IsQuestionInSurveyNotHidden("M1308"))
                    {
                        int M1308_NBR_PRSULC_STG2 =
                            woundList.Where(w => (w.IsUnhealingPressureUlcerStageII == true)).Count();
                        SetTextResponse(M1308_NBR_PRSULC_STG2.ToString(),
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1308_NBR_PRSULC_STG2"));
                        int M1308_NBR_PRSULC_STG3 =
                            woundList.Where(w => (w.IsUnhealingPressureUlcerStageIII == true)).Count();
                        SetTextResponse(M1308_NBR_PRSULC_STG3.ToString(),
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1308_NBR_PRSULC_STG3"));
                        int M1308_NBR_PRSULC_STG4 =
                            woundList.Where(w => (w.IsUnhealingPressureUlcerStageIV == true)).Count();
                        SetTextResponse(M1308_NBR_PRSULC_STG4.ToString(),
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1308_NBR_PRSULC_STG4"));
                        int M1308_NSTG_DRSG =
                            woundList.Where(w => (w.IsUnhealingPressureUlcerNSTG_DRSG == true)).Count();
                        SetTextResponse(M1308_NSTG_DRSG.ToString(),
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1308_NSTG_DRSG"));
                        int M1308_NSTG_CVRG =
                            woundList.Where(w => (w.IsUnhealingPressureUlcerNSTG_CVRG == true)).Count();
                        SetTextResponse(M1308_NSTG_CVRG.ToString(),
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1308_NSTG_CVRG"));
                        int M1308_NSTG_DEEP_TISUE =
                            woundList.Where(w => (w.IsUnhealingPressureUlcerNSTG_DEEP_TISUE == true)).Count();
                        SetTextResponse(M1308_NSTG_DEEP_TISUE.ToString(),
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1308_NSTG_DEEP_TISUE"));

                        if (((RFA == "04") || (RFA == "05") || (RFA == "09")) &&
                            (OasisCache.DoesExistGetOasisAnswerByCMSField(OasisVersionKey,
                                "M1308_NBR_STG2_AT_SOC_ROC")))
                        {
                            int M1308_NBR_STG2_AT_SOC_ROC = 0;
                            int M1308_NBR_STG3_AT_SOC_ROC = 0;
                            int M1308_NBR_STG4_AT_SOC_ROC = 0;
                            int M1308_NSTG_DRSG_SOC_ROC = 0;
                            int M1308_NSTG_CVRG_SOC_ROC = 0;
                            int M1308_NSTG_DEEP_TISUE_SOC_ROC = 0;
                            if (CurrentAdmission.Encounter != null)
                            {
                                // Most recent SOC/ROC
                                Encounter e = MostRecentSOCROCOasisEncounter;
                                if (e != null)
                                {
                                    if (e.EncounterWoundSite != null)
                                    {
                                        if (M1308_NBR_PRSULC_STG2 > 0)
                                        {
                                            M1308_NBR_STG2_AT_SOC_ROC = TallyM1308orM1311Column2(e,
                                                woundList.Where(w => (w.IsUnhealingPressureUlcerStageII == true)));
                                        }

                                        if (M1308_NBR_PRSULC_STG3 > 0)
                                        {
                                            M1308_NBR_STG3_AT_SOC_ROC = TallyM1308orM1311Column2(e,
                                                woundList.Where(w => (w.IsUnhealingPressureUlcerStageIII == true)));
                                        }

                                        if (M1308_NBR_PRSULC_STG4 > 0)
                                        {
                                            M1308_NBR_STG4_AT_SOC_ROC = TallyM1308orM1311Column2(e,
                                                woundList.Where(w => (w.IsUnhealingPressureUlcerStageIV == true)));
                                        }

                                        if (M1308_NSTG_DRSG > 0)
                                        {
                                            M1308_NSTG_DRSG_SOC_ROC = TallyM1308orM1311Column2(e,
                                                woundList.Where(w => (w.IsUnhealingPressureUlcerNSTG_DRSG == true)));
                                        }

                                        if (M1308_NSTG_CVRG > 0)
                                        {
                                            M1308_NSTG_CVRG_SOC_ROC = TallyM1308orM1311Column2(e,
                                                woundList.Where(w => (w.IsUnhealingPressureUlcerNSTG_CVRG == true)));
                                        }

                                        if (M1308_NSTG_DEEP_TISUE > 0)
                                        {
                                            M1308_NSTG_DEEP_TISUE_SOC_ROC = TallyM1308orM1311Column2(e,
                                                woundList.Where(
                                                    w => (w.IsUnhealingPressureUlcerNSTG_DEEP_TISUE == true)));
                                        }
                                    }
                                }
                            }

                            SetTextResponse(M1308_NBR_STG2_AT_SOC_ROC.ToString(),
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1308_NBR_STG2_AT_SOC_ROC"));
                            SetTextResponse(M1308_NBR_STG3_AT_SOC_ROC.ToString(),
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1308_NBR_STG3_AT_SOC_ROC"));
                            SetTextResponse(M1308_NBR_STG4_AT_SOC_ROC.ToString(),
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1308_NBR_STG4_AT_SOC_ROC"));
                            SetTextResponse(M1308_NSTG_DRSG_SOC_ROC.ToString(),
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1308_NSTG_DRSG_SOC_ROC"));
                            SetTextResponse(M1308_NSTG_CVRG_SOC_ROC.ToString(),
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1308_NSTG_CVRG_SOC_ROC"));
                            SetTextResponse(M1308_NSTG_DEEP_TISUE_SOC_ROC.ToString(),
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1308_NSTG_DEEP_TISUE_SOC_ROC"));
                        }

                        if (((RFA == "01") || (RFA == "03") || (RFA == "09")) &&
                            (OasisCache.DoesExistGetOasisAnswerByCMSField(OasisVersionKey, "M1310_PRSR_ULCR_LNGTH")))
                        {
                            AdmissionWoundSite awsM1310 = woundList
                                .Where(w => (w.IsUnhealingPressureUlcerM1310 == true))
                                .OrderByDescending(w => (w.SurfaceArea))
                                .FirstOrDefault();
                            if (awsM1310 != null)
                            {
                                SetTextResponse(awsM1310.Length,
                                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1310_PRSR_ULCR_LNGTH"));
                                SetTextResponse(awsM1310.Width,
                                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1312_PRSR_ULCR_WDTH"));
                                SetTextResponse(awsM1310.Depth,
                                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1314_PRSR_ULCR_DEPTH"));
                            }
                        }
                    }

                    if (IsQuestionInSurveyNotHidden("M1311"))
                    {
                        int M1311_NBR_PRSULC_STG2 =
                            woundList.Where(w => (w.IsUnhealingPressureUlcerStageII == true)).Count();
                        SetTextResponse(M1311_NBR_PRSULC_STG2.ToString(),
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1311_NBR_PRSULC_STG2_A1"));
                        int M1311_NBR_PRSULC_STG3 =
                            woundList.Where(w => (w.IsUnhealingPressureUlcerStageIII == true)).Count();
                        SetTextResponse(M1311_NBR_PRSULC_STG3.ToString(),
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1311_NBR_PRSULC_STG3_B1"));
                        int M1311_NBR_PRSULC_STG4 =
                            woundList.Where(w => (w.IsUnhealingPressureUlcerStageIV == true)).Count();
                        SetTextResponse(M1311_NBR_PRSULC_STG4.ToString(),
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1311_NBR_PRSULC_STG4_C1"));
                        int M1311_NSTG_DRSG =
                            woundList.Where(w => (w.IsUnhealingPressureUlcerNSTG_DRSG == true)).Count();

                        int M1311_NSTG_CVRG =
                            woundList.Where(w => (w.IsUnhealingPressureUlcerNSTG_CVRG == true)).Count();

                        int M1311_NSTG_DEEP_TISUE =
                            woundList.Where(w => (w.IsUnhealingPressureUlcerNSTG_DEEP_TISUE == true)).Count();
                        SetTextResponse(M1311_NSTG_DEEP_TISUE.ToString(),
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1311_NSTG_DEEP_TSUE_F1"));

                        if (((RFA == "04") || (RFA == "05") || (RFA == "09")) &&
                            (OasisCache.DoesExistGetOasisAnswerByCMSField(OasisVersionKey,
                                "M1311_NBR_ULC_SOCROC_STG2_A2")))
                        {
                            int M1311_NBR_STG2_AT_SOC_ROC = 0;
                            int M1311_NBR_STG3_AT_SOC_ROC = 0;
                            int M1311_NBR_STG4_AT_SOC_ROC = 0;
                            int M1311_NSTG_DRSG_SOC_ROC = 0;
                            int M1311_NSTG_CVRG_SOC_ROC = 0;
                            int M1311_NSTG_DEEP_TISUE_SOC_ROC = 0;
                            if (CurrentAdmission.Encounter != null)
                            {
                                // Most recent SOC/ROC
                                Encounter e = MostRecentSOCROCOasisEncounter;
                                if (e != null)
                                {
                                    if (e.EncounterWoundSite != null)
                                    {
                                        if (M1311_NBR_PRSULC_STG2 > 0)
                                        {
                                            M1311_NBR_STG2_AT_SOC_ROC = TallyM1308orM1311Column2(e,
                                                woundList.Where(w => (w.IsUnhealingPressureUlcerStageII == true)));
                                        }

                                        if (M1311_NBR_PRSULC_STG3 > 0)
                                        {
                                            M1311_NBR_STG3_AT_SOC_ROC = TallyM1308orM1311Column2(e,
                                                woundList.Where(w => (w.IsUnhealingPressureUlcerStageIII == true)));
                                        }

                                        if (M1311_NBR_PRSULC_STG4 > 0)
                                        {
                                            M1311_NBR_STG4_AT_SOC_ROC = TallyM1308orM1311Column2(e,
                                                woundList.Where(w => (w.IsUnhealingPressureUlcerStageIV == true)));
                                        }

                                        if (M1311_NSTG_DRSG > 0)
                                        {
                                            M1311_NSTG_DRSG_SOC_ROC = TallyM1308orM1311Column2(e,
                                                woundList.Where(w => (w.IsUnhealingPressureUlcerNSTG_DRSG == true)));
                                        }

                                        if (M1311_NSTG_CVRG > 0)
                                        {
                                            M1311_NSTG_CVRG_SOC_ROC = TallyM1308orM1311Column2(e,
                                                woundList.Where(w => (w.IsUnhealingPressureUlcerNSTG_CVRG == true)));
                                        }

                                        if (M1311_NSTG_DEEP_TISUE > 0)
                                        {
                                            M1311_NSTG_DEEP_TISUE_SOC_ROC = TallyM1308orM1311Column2(e,
                                                woundList.Where(
                                                    w => (w.IsUnhealingPressureUlcerNSTG_DEEP_TISUE == true)));
                                        }
                                    }
                                }
                            }

                            SetTextResponse(M1311_NBR_STG2_AT_SOC_ROC.ToString(),
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1311_NBR_ULC_SOCROC_STG2_A2"));
                            SetTextResponse(M1311_NBR_STG3_AT_SOC_ROC.ToString(),
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1311_NBR_ULC_SOCROC_STG3_B2"));
                            SetTextResponse(M1311_NBR_STG4_AT_SOC_ROC.ToString(),
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1311_NBR_ULC_SOCROC_STG4_C2"));
                            SetTextResponse(M1311_NSTG_DEEP_TISUE_SOC_ROC.ToString(),
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1311_NSTG_DEEP_TSUE_SOCROC_F2"));
                        }
                    }

                    if (IsQuestionInSurveyNotHidden("M1309"))
                    {
                        SetupAdmissionWoundSiteM1309orM1313("M1309");
                    }

                    if (IsQuestionInSurveyNotHidden("M1313"))
                    {
                        SetupAdmissionWoundSiteM1309orM1313("M1313");
                    }

                    if (IsQuestionInSurveyNotHidden("M1320"))
                    {
                        // if there is zero or one default the answer - otherwise honor users last response if its not NA
                        int countM1320 = woundList.Where(w =>
                            (w.IsUnhealedPressureUlcerStageIIorHigherObservable && w.MostProblematic)).Count();
                        if (countM1320 == 0)
                        {
                            SetRadioResponse(true,
                                OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey,
                                    "M1320_STUS_PRBLM_PRSR_ULCR", 5));
                        }
                        else if (countM1320 == 1)
                        {
                            AdmissionWoundSite awsM1320 = woundList.Where(w =>
                                    (w.IsUnhealedPressureUlcerStageIIorHigherObservable && w.MostProblematic))
                                .FirstOrDefault();
                            if (awsM1320 != null)
                            {
                                SetRadioResponse(true,
                                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey,
                                        "M1320_STUS_PRBLM_PRSR_ULCR", awsM1320.M1320sequence));
                            }
                        }
                        else
                        {
                            // Copy all but NA response
                            if (GetRadioResponse(OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey,
                                    "M1320_STUS_PRBLM_PRSR_ULCR", 5)) == false)
                            {
                                CopyResponse("M1320",
                                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1320_STUS_PRBLM_PRSR_ULCR"),
                                    prevB1Record);
                            }
                        }
                    }
                }
            }

            if (IsQuestionInSurveyNotHidden("M1322"))
            {
                int count = woundList.Where(w => (w.IsPressureUlcerStageI == true)).Count();
                if (count > 4)
                {
                    count = 4;
                }

                count++;
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1322_NBR_PRSULC_STG1", count));
            }

            if (IsQuestionInSurveyNotHidden("M1324"))
            {
                // if there is zero or one default the answer - otherwise honor users last response if its not NA
                int countM1324 = woundList.Where(w =>
                    ((w.IsUnhealingStageablePressureUlcerObservable == true) && (w.MostProblematic))).Count();
                if (countM1324 == 0)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1324_STG_PRBLM_ULCER", 5));
                }
                else if (countM1324 == 1)
                {
                    AdmissionWoundSite awsM1324 = woundList.Where(w =>
                            ((w.IsUnhealingStageablePressureUlcerObservable == true) && (w.MostProblematic)))
                        .FirstOrDefault();
                    if (awsM1324 != null)
                    {
                        SetRadioResponse(true,
                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1324_STG_PRBLM_ULCER",
                                awsM1324.M1324sequence));
                    }
                }
                else
                {
                    // Copy all but NA response
                    if (GetRadioResponse(
                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1324_STG_PRBLM_ULCER",
                                5)) == false)
                    {
                        CopyResponse("M1324",
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1324_STG_PRBLM_ULCER"),
                            prevB1Record);
                    }
                    else
                    {
                        ClearResponse(
                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1324_STG_PRBLM_ULCER", 5),
                            false, true);
                    }
                }
            }

            if (IsQuestionInSurveyNotHidden("M1330"))
            {
                AdmissionWoundSite awsM1330 = woundList.Where(w => (w.IsTypeStasisUlcer == true)).FirstOrDefault();
                if (awsM1330 == null)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1330_STAS_ULCR_PRSNT", 1));
                }
                else
                {
                    int observableCount = woundList.Where(w => (w.IsStasisUlcerObservable == true)).Count();
                    int unobservableCount = woundList.Where(w => (w.IsStasisUlcerUnobservable == true)).Count();
                    if ((observableCount > 0) && (unobservableCount > 0))
                    {
                        SetRadioResponse(true,
                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1330_STAS_ULCR_PRSNT",
                                2));
                    }
                    else if (observableCount > 0)
                    {
                        SetRadioResponse(true,
                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1330_STAS_ULCR_PRSNT",
                                3));
                    }
                    else if (unobservableCount > 0)
                    {
                        SetRadioResponse(true,
                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1330_STAS_ULCR_PRSNT",
                                4));
                    }

                    if (observableCount > 0)
                    {
                        if (IsQuestionInSurveyNotHidden("M1332"))
                        {
                            if (observableCount > 4)
                            {
                                observableCount = 4;
                            }

                            SetRadioResponse(true,
                                OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1332_NBR_STAS_ULCR",
                                    observableCount));
                        }

                        if (IsQuestionInSurveyNotHidden("M1334"))
                        {
                            // if there is one default the answer - otherwise honor users last response
                            if (observableCount == 1)
                            {
                                AdmissionWoundSite awsM1334 = woundList
                                    .Where(w => (w.IsUnhealedStatisUlcerObservable && w.MostProblematic))
                                    .FirstOrDefault();
                                if (awsM1334 != null)
                                {
                                    int sequence = OasisCache.IsOasisVersion2point0(OasisVersionKey)
                                        ? awsM1334.M1334sequence
                                        : awsM1334.M1334sequence -
                                          1; // version 2.10 + removed sequence 0 (newly epithilized) and resequenced
                                    if (OasisCache.DoesExistGetOasisAnswerByCMSFieldAndSequence(OasisVersionKey,
                                            "M1334_STUS_PRBLM_STAS_ULCR", sequence))
                                    {
                                        SetRadioResponse(true,
                                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey,
                                                "M1334_STUS_PRBLM_STAS_ULCR", sequence));
                                    }
                                }
                            }
                            else
                            {
                                CopyResponse("M1334",
                                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1334_STUS_PRBLM_STAS_ULCR"),
                                    prevB1Record);
                            }
                        }
                    }
                }
            }

            if (IsQuestionInSurveyNotHidden("M1340"))
            {
                AdmissionWoundSite awsM1340 = woundList.Where(w => (w.IsTypeSurgicalWound == true)).FirstOrDefault();
                if (awsM1340 == null)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1340_SRGCL_WND_PRSNT", 1));
                }
                else
                {
                    int observableCount = woundList.Where(w => (w.IsSurgicalWoundObservable == true)).Count();
                    int unobservableCount = woundList.Where(w => (w.IsSurgicalWoundUnobservable == true)).Count();
                    if (observableCount > 0)
                    {
                        SetRadioResponse(true,
                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1340_SRGCL_WND_PRSNT",
                                2));
                    }
                    else if (unobservableCount > 0)
                    {
                        SetRadioResponse(true,
                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1340_SRGCL_WND_PRSNT",
                                3));
                    }

                    if ((observableCount > 0) && (IsQuestionInSurveyNotHidden("M1342")))
                    {
                        // if there is one default the answer - otherwise honor users last response
                        if (observableCount == 1)
                        {
                            AdmissionWoundSite awsM1342 = woundList
                                .Where(w => (w.IsUnhealedSurgicalWoundObservable && w.MostProblematic))
                                .FirstOrDefault();
                            if (awsM1342 != null)
                            {
                                SetRadioResponse(true,
                                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey,
                                        "M1342_STUS_PRBLM_SRGCL_WND", awsM1342.M1342sequence));
                            }
                        }
                        else
                        {
                            CopyResponse("M1342",
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1342_STUS_PRBLM_SRGCL_WND"),
                                prevB1Record);
                        }
                    }
                }
            }

            if (IsQuestionInSurveyNotHidden("M1350"))
            {
                AdmissionWoundSite awsM1350 = woundList.Where(w => (w.IsOther == true)).FirstOrDefault();
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1350_LESION_OPEN_WND",
                        ((awsM1350 == null) ? 1 : 2)));
            }
        }

        public List<AdmissionWoundSite> GetM1309orM1313WorseningWoundList()
        {
            if ((CurrentAdmission == null) || (CurrentAdmission.AdmissionWoundSite == null) ||
                (CurrentAdmission.Encounter == null))
            {
                return null;
            }

            Encounter e = MostRecentSOCROCOasisEncounter;
            if ((e == null) || (e.EncounterWoundSite == null))
            {
                return null;
            }

            ProcessFilteredAdmissionWoundSiteItems(CurrentAdmission.AdmissionWoundSite);
            if (CurrentFilteredAdmissionWoundSite == null)
            {
                return null;
            }

            List<AdmissionWoundSite> woundList =
                CurrentFilteredAdmissionWoundSite.OfType<AdmissionWoundSite>().AsQueryable().ToList();
            if (woundList != null)
            {
                foreach (AdmissionWoundSite aws in woundList)
                    aws.M1309orM1313ThumbNail = aws.GetM1309orM1313ThumbNail(e);
                woundList = woundList.Where(w => (string.IsNullOrWhiteSpace(w.M1309orM1313ThumbNail) == false))
                    .ToList();
            }

            return woundList;
        }

        private void SetupAdmissionWoundSiteM1309orM1313(string question)
        {
            int M1309orM1313_NBR_NEW_WRS_PRSULC_STG2 = 0;
            int M1309orM1313_NBR_NEW_WRS_PRSULC_STG3 = 0;
            int M1309orM1313_NBR_NEW_WRS_PRSULC_STG4 = 0;
            int M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DRSG = 0;
            int M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_CVRG = 0;
            int M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DEEP_TISUE = 0;

            Encounter e = MostRecentSOCROCOasisEncounter;
            List<AdmissionWoundSite> woundList = GetM1309orM1313WorseningWoundList();
            if (woundList != null)
            {
                foreach (AdmissionWoundSite aws in woundList)
                    if (string.IsNullOrWhiteSpace(aws.M1309orM1313_NBR_NEW_WRS_PRSULC_STG2ThumbNail(e)) == false)
                    {
                        M1309orM1313_NBR_NEW_WRS_PRSULC_STG2++;
                    }
                    else if (string.IsNullOrWhiteSpace(aws.M1309orM1313_NBR_NEW_WRS_PRSULC_STG3ThumbNail(e)) == false)
                    {
                        M1309orM1313_NBR_NEW_WRS_PRSULC_STG3++;
                    }
                    else if (string.IsNullOrWhiteSpace(aws.M1309orM1313_NBR_NEW_WRS_PRSULC_STG4ThumbNail(e)) == false)
                    {
                        M1309orM1313_NBR_NEW_WRS_PRSULC_STG4++;
                    }
                    else if (string.IsNullOrWhiteSpace(aws.M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DRSGThumbNail(e)) ==
                             false)
                    {
                        M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DRSG++;
                    }
                    else if (string.IsNullOrWhiteSpace(aws.M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_CVRGThumbNail(e)) ==
                             false)
                    {
                        M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_CVRG++;
                    }
                    else if (string.IsNullOrWhiteSpace(
                                 aws.M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DEEP_TISUEThumbNail(e)) == false)
                    {
                        M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DEEP_TISUE++;
                    }
            }

            if (question == "M1309")
            {
                SetTextResponse(M1309orM1313_NBR_NEW_WRS_PRSULC_STG2.ToString(),
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1309_NBR_NEW_WRS_PRSULC_STG2"));
                SetTextResponse(M1309orM1313_NBR_NEW_WRS_PRSULC_STG3.ToString(),
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1309_NBR_NEW_WRS_PRSULC_STG3"));
                SetTextResponse(M1309orM1313_NBR_NEW_WRS_PRSULC_STG4.ToString(),
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1309_NBR_NEW_WRS_PRSULC_STG4"));
                SetTextResponse(M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_CVRG.ToString(),
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1309_NBR_NEW_WRS_PRSULC_NSTG"));
            }
            else if (question == "M1313") // don't override existing DASH answers to zero
            {
                if ((M1309orM1313_NBR_NEW_WRS_PRSULC_STG2 == 0) &&
                    (GetTextResponse(
                         OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_STG2_A")) !=
                     OASIS_DASH))
                {
                    SetTextResponse(M1309orM1313_NBR_NEW_WRS_PRSULC_STG2.ToString(),
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_STG2_A"));
                }

                if ((M1309orM1313_NBR_NEW_WRS_PRSULC_STG3 == 0) &&
                    (GetTextResponse(
                         OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_STG3_B")) !=
                     OASIS_DASH))
                {
                    SetTextResponse(M1309orM1313_NBR_NEW_WRS_PRSULC_STG3.ToString(),
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_STG3_B"));
                }

                if ((M1309orM1313_NBR_NEW_WRS_PRSULC_STG4 == 0) &&
                    (GetTextResponse(
                         OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_STG4_C")) !=
                     OASIS_DASH))
                {
                    SetTextResponse(M1309orM1313_NBR_NEW_WRS_PRSULC_STG4.ToString(),
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_STG4_C"));
                }

                if ((M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DRSG == 0) &&
                    (GetTextResponse(
                         OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_NSTG_DRSG_D")) !=
                     OASIS_DASH))
                {
                    SetTextResponse(M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DRSG.ToString(),
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_NSTG_DRSG_D"));
                }

                if ((M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_CVRG == 0) &&
                    (GetTextResponse(
                         OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_NSTG_CVRG_E")) !=
                     OASIS_DASH))
                {
                    SetTextResponse(M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_CVRG.ToString(),
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_NSTG_CVRG_E"));
                }

                if ((M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DEEP_TISUE == 0) &&
                    (GetTextResponse(
                         OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_NSTG_TSUE_F")) !=
                     OASIS_DASH))
                {
                    SetTextResponse(M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DEEP_TISUE.ToString(),
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1313_NW_WS_PRSULC_NSTG_TSUE_F"));
                }
            }
        }

        private int TallyM1308orM1311Column2(Encounter mostRecentSOCROCEncounter,
            IQueryable<AdmissionWoundSite> M1308orM1311woundList)
        {
            if (M1308orM1311woundList == null)
            {
                return 0;
            }

            int count = 0;
            foreach (AdmissionWoundSite aws in M1308orM1311woundList)
            {
                EncounterWoundSite ewsM1308orM1311 = mostRecentSOCROCEncounter.EncounterWoundSite.Where(ew =>
                    ((ew.AdmissionWoundSiteKey == aws.AdmissionWoundSiteKey) ||
                     (ew.AdmissionWoundSiteKey == aws.HistoryKey) ||
                     (ew.AdmissionWoundSite.HistoryKey == aws.HistoryKey))).FirstOrDefault();
                if (ewsM1308orM1311 != null)
                {
                    count++;
                }
            }

            return count;
        }

        private void DefaultDiagFromMostRecentB1Record(string icd, int occur, string prevB1Record)
        {
            string icdFind;
            // first attempt to find the ICD in current OASIS survey - but at a different localing in the list
            for (int i = 0; i <= 5; i++)
            {
                icdFind = GetResponseB1Record(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDField[i]),
                    prevB1Record);
                if (icd == icdFind)
                {
                    SetResponse(
                        GetResponseB1RecordNoTrim(
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDSeverityField[i]),
                            prevB1Record),
                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDSeverityField[occur]));
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt3Field[occur]))
                    {
                        SetResponse(
                            GetResponseB1RecordNoTrim(
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt3Field[i]),
                                prevB1Record),
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt3Field[occur]));
                    }

                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt4Field[occur]))
                    {
                        SetResponse(
                            GetResponseB1RecordNoTrim(
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt4Field[i]),
                                prevB1Record),
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt4Field[occur]));
                    }

                    return;
                }
            }

            // ICD not found in current OASIS survey - loop thru prior EncounterOasis records of same version
            IEnumerable<Encounter> eList = CurrentAdmission.Encounter
                .Where(eo =>
                    (((eo.EncounterOasisRFA == "01") || (eo.EncounterOasisRFA == "03") ||
                      (eo.EncounterOasisRFA == "04") || (eo.EncounterOasisRFA == "05")) &&
                     (eo.EncounterOasisM0090 != null) && (eo.EncounterKey != CurrentEncounter.EncounterKey) &&
                     eo.IsEncounterOasisActive))
                .OrderByDescending(eo => eo.EncounterOasisM0090);
            if (eList != null)
            {
                foreach (Encounter e in eList)
                {
                    EncounterOasis eo = null;
                    if (e.EncounterOasis != null)
                    {
                        eo = e.EncounterOasis
                            .Where(x => ((x.OasisVersionKey == OasisVersionKey) && (x.InactiveDate == null) &&
                                         (x.REC_ID != "X1"))).OrderByDescending(x => x.AddedDate).FirstOrDefault();
                    }

                    if (eo != null)
                    {
                        for (int i = 0; i <= 5; i++)
                        {
                            icdFind = GetResponseB1Record(
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDField[i]), eo.B1Record);
                            if (icd == icdFind)
                            {
                                SetResponse(
                                    GetResponseB1RecordNoTrim(
                                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDSeverityField[i]),
                                        eo.B1Record),
                                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDSeverityField[occur]));
                                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey,
                                        OasisICDPmt3Field[occur]))
                                {
                                    SetResponse(
                                        GetResponseB1RecordNoTrim(
                                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt3Field[i]),
                                            eo.B1Record),
                                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt3Field[occur]));
                                }

                                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey,
                                        OasisICDPmt4Field[occur]))
                                {
                                    SetResponse(
                                        GetResponseB1RecordNoTrim(
                                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt4Field[i]),
                                            eo.B1Record),
                                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt4Field[occur]));
                                }

                                return;
                            }
                        }
                    }
                }
            }

            // still not found - default to nulls
            SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDSeverityField[occur]));
            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt3Field[occur]))
            {
                SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt3Field[occur]));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt4Field[occur]))
            {
                SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, OasisICDPmt4Field[occur]));
            }
        }

        public void SetupEncounterCollectedBy()
        {
            string M0080 = null;
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (IsOasisActive)
            {
                M0080 = GetResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0080_ASSESSOR_DISCIPLINE"));
            }

            if ((DynamicFormViewModel != null) && (DynamicFormViewModel.SignatureQuestion != null))
            {
                DynamicFormViewModel.SignatureQuestion.SetupEncounterCollectedBy(M0080, IsOasisActive, false);
            }
        }

        public void Setup(bool AddingNewEncounter, AdmissionDiscipline currentAdmissionDiscipline)
        {
            if (CurrentEncounterOasis == null)
            {
                return;
            }

            if (CurrentEncounterOasis.SYS_CDIsHospice)
            {
                SetupDefaultsHIS(AddingNewEncounter, currentAdmissionDiscipline);
                ApplySkipLogic();
            }
            else
            {
                SetupDefaultsOASIS(AddingNewEncounter);
                OasisAlertCheckAllMeasures();
            }
        }

        private void SetupDefaultsOASIS(bool AddingNewEncounter)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            SetupPatientDefaults(CurrentPatient, CurrentAdmission, true, AddingNewEncounter);
            SetupAdmissionDefaults(CurrentAdmission, true, AddingNewEncounter);
            if (AddingNewEncounter)
            {
                SetupEncounterDefaults(CurrentEncounter);
            }

            SetupLookbacks(AddingNewEncounter);
            Messenger.Default.Send(((CurrentEncounterOasis.BypassFlag == true) ? true : false),
                string.Format("OasisBypassFlagChanged{0}", OasisManagerGuid.ToString().Trim()));
            ApplySkipLogic();
        }

        private void SetupPatientDefaults(Patient patient, Admission admission, bool processChildren,
            bool AddingNewEncounter)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            // This setup is called from property changed events as well - so insure we are working with the proper patient
            if (!MappingAllowedClinicianOrOasisCoordinatorReEditBypassOASISAssist)
            {
                return;
            }

            if (CurrentAdmission.PatientKey != patient.PatientKey)
            {
                return;
            }

            InitializeB1RecordOASIS();
            SetTextResponse(patient.MRN, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0020_PAT_ID"));
            SetTextResponse(patient.FirstName, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0040_PAT_FNAME"));
            SetTextResponse(patient.MiddleName, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0040_PAT_MI"));
            SetTextResponse(patient.LastName, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0040_PAT_LNAME"));
            SetTextResponse(patient.Suffix, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0040_PAT_SUFFIX"));
            _m0090Date =
                GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"));
            PatientAddress pa = patient.MainAddress(_m0090Date);
            if (pa == null)
            {
                ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0050_PAT_ST"), true, true);
                ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0060_PAT_ZIP"), true, true);
            }
            else
            {
                SetTextResponse(pa.StateCodeCode, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0050_PAT_ST"));
                SetTextResponse(pa.ZipCode, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0060_PAT_ZIP"));
            }

            SetupPatientInsurance(patient, admission);
            if (string.IsNullOrWhiteSpace(patient.SSN))
            {
                SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0064_SSN_UK"));
            }
            else
            {
                SetTextResponse(patient.SSN, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0064_SSN"));
            }

            SetDateResponse(patient.BirthDate,
                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0066_PAT_BIRTH_DT"));
            SetRadioResponse(true,
                OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M0069_PAT_GENDER", 1));
            if (patient.GenderCode != null)
            {
                if (patient.GenderCode.ToLower() == "f")
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M0069_PAT_GENDER", 2));
                }
            }

            if (GetQuestionInSurveyNotHidden("M0140") != null)
            {
                string races = (string.IsNullOrWhiteSpace(patient.Races)) ? null : patient.Races.ToLower();
                if (races != null)
                {
                    if (((races.Contains("american indian")) || (races.Contains("alaska"))) ||
                        ((races.Contains("asia")) && (!races.Contains("caucasia"))) ||
                        ((races.Contains("black")) || (races.Contains("africa"))) ||
                        ((races.Contains("hispanic")) || (races.Contains("latin"))) ||
                        ((races.Contains("hawaii")) || (races.Contains("pacific"))) ||
                        ((races.Contains("white")) || (races.Contains("caucasia"))))
                    {
                        ResetAllAnswers(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0140_ETHNIC_AI_AN"));
                        if ((races.Contains("american indian")) || (races.Contains("alaska")))
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0140_ETHNIC_AI_AN"));
                        }

                        if ((races.Contains("asia")) && (!races.Contains("caucasia")))
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0140_ETHNIC_ASIAN"));
                        }

                        if ((races.Contains("black")) || (races.Contains("africa")))
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0140_ETHNIC_BLACK"));
                        }

                        if ((races.Contains("hispanic")) || (races.Contains("latin")))
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0140_ETHNIC_HISP"));
                        }

                        if ((races.Contains("hawaii")) || (races.Contains("pacific")))
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0140_ETHNIC_NH_PI"));
                        }

                        if ((races.Contains("white")) || (races.Contains("caucasia")))
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0140_ETHNIC_WHITE"));
                        }
                    }
                }
            }

            SetupPatientDefaults2(patient, AddingNewEncounter);
            if (processChildren)
            {
                if (AddingNewEncounter)
                {
                    SetupPatientMedication(patient);
                }
            }
        }

        private DateTime? _deathDate;

        private void SetupPatientInsurance(Patient patient, Admission admission)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            // Refresh on every edit – and / or every change of M0090 – provided the ‘editor’ has ‘access’ to these field
            // Until the OASIS is completed
            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.EncounterStatus == (int)EncounterStatusType.Completed)
            {
                return;
            }

            if (MappingAllowedClinicianOrOasisCoordinatorReEditBypassOASISAssist == false)
            {
                return;
            }

            _m0090Date =
                GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"));
            if (_m0090Date == null)
            {
                _m0090Date = DateTime.Today.Date;
            }

            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0063_MEDICARE_NA"));
            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0065_MEDICAID_NA"));

            // calculate M0150 responses
            if ((patient.PatientInsurance != null) && (admission.AdmissionCoverage != null))
            {
                List<string> insuranceTypeList = new List<string>();
                List<AdmissionCoverage> acList =
                    admission.AdmissionCoverage.Where(ac => ac.IsActiveAsOfDate(_m0090Date)).ToList();
                if ((acList != null) && acList.Any())
                {
                    foreach (AdmissionCoverage ac in acList)
                    {
                        List<AdmissionCoverageInsurance> aciList = ac.AdmissionCoverageInsurance
                            .Where(aci => (aci.HistoryKey == null) && (aci.Inactive == false)).ToList();
                        if ((aciList != null) && aciList.Any())
                        {
                            foreach (AdmissionCoverageInsurance aci in aciList)
                            {
                                PatientInsurance pi = patient.PatientInsurance.Where(i =>
                                    ((i.Inactive == false) && (i.HistoryKey == null) &&
                                     (i.PatientInsuranceKey == aci.PatientInsuranceKey) &&
                                     ((i.EffectiveFromDate.HasValue == false) ||
                                      (((DateTime)i.EffectiveFromDate).Date <= _m0090Date)) &&
                                     ((i.EffectiveThruDate.HasValue == false) ||
                                      (((DateTime)i.EffectiveThruDate).Date >= _m0090Date)))).FirstOrDefault();
                                if ((pi != null) && (insuranceTypeList.Contains(pi.InsuranceTypeCode) == false))
                                {
                                    insuranceTypeList.Add(pi.InsuranceTypeCode);
                                }
                            }
                        }
                    }
                }

                if (insuranceTypeList.Any())
                {
                    SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_NONE"));
                    SetCheckBoxResponse(false,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_MCARE_FFS"));
                    SetCheckBoxResponse(false,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_MCARE_HMO"));
                    SetCheckBoxResponse(false,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_MCAID_FFS"));
                    SetCheckBoxResponse(false,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_MCAID_HMO"));
                    SetCheckBoxResponse(false,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_WRKCOMP"));
                    SetCheckBoxResponse(false,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_TITLEPGMS"));
                    SetCheckBoxResponse(false,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_OTH_GOVT"));
                    SetCheckBoxResponse(false,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_PRIV_INS"));
                    SetCheckBoxResponse(false,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_PRIV_HMO"));
                    SetCheckBoxResponse(false,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_SELFPAY"));
                    SetCheckBoxResponse(false,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_OTHER"));
                    if ((RFA == "01") || (RFA == "03"))
                    {
                        SetCheckBoxResponse(false,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_UK"));
                    }

                    foreach (string insuranceType in insuranceTypeList)
                        if (insuranceType == "1")
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_MCARE_FFS"));
                        }
                        else if (insuranceType == "2")
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_MCARE_HMO"));
                        }
                        else if (insuranceType == "3")
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_MCAID_FFS"));
                        }
                        else if (insuranceType == "4")
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_MCAID_HMO"));
                        }
                        else if (insuranceType == "5")
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_WRKCOMP"));
                        }
                        else if (insuranceType == "6")
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_TITLEPGMS"));
                        }
                        else if (insuranceType == "7")
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_OTH_GOVT"));
                        }
                        else if (insuranceType == "8")
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_PRIV_INS"));
                        }
                        else if (insuranceType == "9")
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_PRIV_HMO"));
                        }
                        else if (insuranceType == "10")
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_SELFPAY"));
                        }
                        else if (insuranceType == "11")
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_OTHER"));
                        }
                        else
                        {
                            SetCheckBoxResponse(true,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0150_CPAY_OTHER"));
                        }
                }
            }

            if (((RFA == "01") || (RFA == "03")) == false)
            {
                // M0150_CPAY_UK is only applicable to RFA 01,03
                OasisLayout oasisLayout = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0150_CPAY_UK");
                CurrentEncounterOasis.B1Record =
                    CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, oasisLayout.Length);
                CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1,
                    new String(' ', oasisLayout.Length));
            }

            // calculate M0063 and M0065 
            if (patient.PatientInsurance != null)
            {
                List<PatientInsurance> list = patient.PatientInsurance
                    .Where(i => ((i.Inactive == false) && (i.HistoryKey == null) && (i.PatientInsuranceKey != 0) &&
                                 ((i.EffectiveFromDate.HasValue == false ||
                                   ((DateTime)i.EffectiveFromDate).Date <= _m0090Date) &&
                                  (i.EffectiveThruDate.HasValue == false ||
                                   ((DateTime)i.EffectiveThruDate).Date >= _m0090Date))))
                    .OrderBy(i => i.InsuranceTypeKey).ToList();
                if ((list != null) && list.Any())
                {
                    bool foundMedicare = false;
                    bool foundMedicaid = false;
                    foreach (PatientInsurance pi in list)
                        if ((pi.InsuranceTypeCode == "1") && (foundMedicare == false) &&
                            (string.IsNullOrWhiteSpace(pi.InsuranceNumber) == false))
                        {
                            foundMedicare = true;
                            SetTextResponse(pi.InsuranceNumber,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0063_MEDICARE_NUM"));
                        }
                        else if ((pi.InsuranceTypeCode == "3") && (foundMedicaid == false) &&
                                 (string.IsNullOrWhiteSpace(pi.InsuranceNumber) == false))
                        {
                            foundMedicaid = true;
                            SetTextResponse(pi.InsuranceNumber,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0065_MEDICAID_NUM"));
                        }
                }
            }
        }

        private void SetupPatientDefaults2(Patient patient, bool AddingNewEncounter)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            _deathDate = patient.DeathDate;
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinicianOrOasisCoordinatorReEdit)
            {
                return;
            }

            if (RFA != "08")
            {
                return;
            }

            // This setup is called from property changed events as well - so insure we are working with the proper patient
            if (CurrentPatient.PatientKey != patient.PatientKey)
            {
                return;
            }

            if ((IsQuestionInSurveyNotHidden("M0906")) && (patient.DeathDate != null))
            {
                SetDateResponse(patient.DeathDate,
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0906_DC_TRAN_DTH_DT"));
            }
        }

        private void SetupAdmissionDefaults(Admission admission, bool processChildren, bool AddingNewEncounter)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            // This setup is called from property changed events as well - so insure we are working with the proper admission
            if (!MappingAllowedClinicianOrOasisCoordinatorReEditBypassOASISAssist)
            {
                return;
            }

            if (CurrentAdmission.AdmissionKey != admission.AdmissionKey)
            {
                return;
            }

            SetupAdmissionOasisHeaderDefaultsOASIS(admission.AdmissionKey);

            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0018_PHYSICIAN_UK"));
            if (AdmissionPhysician.SigningPhysician != null)
            {
                if (string.IsNullOrWhiteSpace(AdmissionPhysician.SigningPhysician.NPI) == false)
                {
                    SetTextResponse(AdmissionPhysician.SigningPhysician.NPI,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0018_PHYSICIAN_ID"));
                }
            }

            if (admission.SOCDate != null)
            {
                SetDateResponse(admission.SOCDate,
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0030_START_CARE_DT"));
            }

            // do not redefault M0032 on general edits 
            if (PreviousEncounterStatus != (int)EncounterStatusType.Completed)
            {
                if (RFA == "01")
                {
                    SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0032_ROC_DT_NA"));
                }
                else if (RFA != "03")
                {
                    //set to most recent ROC if there is one
                    SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0032_ROC_DT_NA"));
                    DateTime? m0090Date =
                        GetDateResponse(
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"));
                    if (m0090Date == null)
                    {
                        m0090Date = DateTime.Today;
                    }

                    Encounter e = admission.Encounter
                        .Where(ae => (ae.EncounterResumptionDate.HasValue && (ae.EncounterResumptionDate <= m0090Date)))
                        .OrderByDescending(ae => (ae.EncounterResumptionDate))
                        .FirstOrDefault();
                    if (e != null)
                    {
                        SetDateResponse(e.EncounterResumptionDate,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0032_ROC_DT"));
                    }
                }
            }

            if (!MappingAllowedClinicianOrOasisCoordinatorReEdit)
            {
                //Initially perform ICD mapping even with mapping off
                if (processChildren)
                {
                    if (AddingNewEncounter)
                    {
                        SetupAdmissionDiagnosis(admission);
                    }
                }

                return;
            }

            //do not redefault M0102/M0104 on general edits (post complete)
            if ((PreviousEncounterStatus != (int)EncounterStatusType.Completed) && (RFA == "01"))
            {
                SetCheckBoxResponse(true,
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0102_PHYSN_ORDRD_SOCROC_DT_NA"));
                if (admission.PhysicianOrderedSOCDate != null)
                {
                    SetDateResponse(admission.PhysicianOrderedSOCDate,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0102_PHYSN_ORDRD_SOCROC_DT"));
                }
                else
                {
                    SetDateResponse(admission.InitialReferralDate,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0104_PHYSN_RFRL_DT"));
                }
            }

            if (IsQuestionInSurveyNotHidden("M0903"))
            {
                if (GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0903_LAST_HOME_VISIT")) ==
                    null)
                {
                    Encounter e = admission.Encounter
                        .Where(ae =>
                            ae.EncounterStatus ==
                            (int)EncounterStatusType.Completed) // Only take into account Encounters that are COMPLETED
                        .Where(ae => (ae.EncounterIsEval || ae.EncounterIsVisit || ae.EncounterIsResumption))
                        .OrderByDescending(ae => (ae.EncounterOrTaskStartDateAndTime))
                        .FirstOrDefault();
                    if (e != null)
                    {
                        SetDateResponse(e.EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0903_LAST_HOME_VISIT"));
                    }
                }
            }
            
            if (processChildren)
            {
                if (AddingNewEncounter)
                {
                    SetupAdmissionDiagnosis(admission);
                    SetupAdmissionPainLocation(admission);
                    SetupAdmissionWoundSite(admission);
                    WeightOasisMapping(null);
                }
            }
        }

        private void SetupAdmissionOasisHeaderDefaultsOASIS(int admissionKey)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            // This setup is called from property changed events as well - so insure we are working with the proper admission
            if (!MappingAllowedClinicianOrOasisCoordinatorReEditBypassOASISAssist)
            {
                return;
            }

            if (CurrentAdmission == null)
            {
                return;
            }

            if (CurrentAdmission.AdmissionKey != admissionKey)
            {
                return;
            }

            // Default OasisHeader info to null
            CurrentEncounterOasis.OasisHeaderKey = null;
            CurrentEncounterOasis.ServiceLineGroupingKey = null;
            CurrentEncounterOasis.HHA_AGENCY_ID = null;
            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "HHA_AGENCY_ID"))
            {
                SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "HHA_AGENCY_ID"));
            }

            CurrentEncounterOasis.NPI = null;
            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "NATL_PROV_ID"))
            {
                SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "NATL_PROV_ID"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "NATL_PRVDR_ID"))
            {
                SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "NATL_PRVDR_ID"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "STATE_CD"))
            {
                SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "STATE_CD"));
            }

            CurrentEncounterOasis.FED_ID = null;
            ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0010_CCN"), true);
            ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0014_BRANCH_STATE"), true);
            ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0016_BRANCH_ID"), true);

            // Get controlling ServiceLineGrouping and OasisHeader
            DateTime surveyDate = (CurrentEncounterOasis.M0090 != null)
                ? (DateTime)CurrentEncounterOasis.M0090
                : ((CurrentEncounter == null)
                    ? DateTime.Today
                    : ((CurrentEncounter.EncounterStartDate == null)
                        ? DateTime.Today
                        : CurrentEncounter.EncounterStartDate.GetValueOrDefault().Date));
            ServiceLineGrouping slg = CurrentAdmission.GetFirstServiceLineGroupWithOasisHeader(surveyDate);
            if (slg == null)
            {
                MessageBox.Show(
                    "There is no Service Line Grouping defined for this admission/encounter with a controlling CMS Header,  M0010, M0014 and M0016 will not be valued.");
                return;
            }

            OasisHeader oh = OasisHeaderCache.GetOasisHeaderFromKey(slg.OasisHeaderKey);
            if (oh == null)
            {
                MessageBox.Show(
                    "There is no Service Line Grouping defined for this admission/encounter with a controlling CMS Header,  M0010, M0014 and M0016 will not be valued.");
                return;
            }

            CurrentEncounterOasis.OasisHeaderKey = oh.OasisHeaderKey;
            CurrentEncounterOasis.ServiceLineGroupingKey = slg.ServiceLineGroupingKey;
            if (string.IsNullOrWhiteSpace(oh.HHAAgencyID) == false)
            {
                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "HHA_AGENCY_ID"))
                {
                    SetResponse(oh.HHAAgencyID, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "HHA_AGENCY_ID"));
                }

                CurrentEncounterOasis.HHA_AGENCY_ID = oh.HHAAgencyID;
            }

            if (string.IsNullOrWhiteSpace(oh.NPI) == false)
            {
                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "NATL_PROV_ID"))
                {
                    SetResponse(oh.NPI, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "NATL_PROV_ID"));
                }

                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "NATL_PRVDR_ID"))
                {
                    SetResponse(oh.NPI, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "NATL_PRVDR_ID"));
                }

                CurrentEncounterOasis.NPI = oh.NPI;
            }

            if (string.IsNullOrWhiteSpace(oh.CMSCertificationNumber) == false)
            {
                SetResponse(oh.CMSCertificationNumber,
                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0010_CCN"));
                CurrentEncounterOasis.FED_ID =
                    GetResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0010_CCN"));
            }

            if (oh.BranchState != null)
            {
                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "STATE_CD"))
                {
                    SetResponse(oh.BranchStateCode, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "STATE_CD"));
                }

                if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "STATE_CD"))
                {
                    // 2.10, 2.11 - only set M0014 if M0016 not N or P
                    if ((string.IsNullOrWhiteSpace(oh.BranchIDNumber) == false) &&
                        (oh.BranchIDNumber.ToUpper() != "N") && (oh.BranchIDNumber.ToUpper() != "P"))
                    {
                        if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M0014_BRANCH_STATE"))
                        {
                            SetResponse(oh.BranchStateCode,
                                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0014_BRANCH_STATE"));
                        }
                    }
                }
                else
                {
                    // 02.00 - always set M0014
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M0014_BRANCH_STATE"))
                    {
                        SetResponse(oh.BranchStateCode,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0014_BRANCH_STATE"));
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(oh.BranchIDNumber) == false)
            {
                SetResponse(oh.BranchIDNumber, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M0016_BRANCH_ID"));
            }

            Messenger.Default.Send(true, string.Format("OasisHeaderChanged{0}", OasisManagerGuid.ToString().Trim()));
        }

        private DateTime? _dischargeDate;

        private void SetupAdmissionDisciplineChanged(AdmissionDiscipline admissionDiscipline)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            _dischargeDate = admissionDiscipline.DischargeDateTime;
            if (!IsOasisActive)
            {
                return;
            }

            // This setup is called from property changed events as well - so insure we are working with the proper patient
            if (!MappingAllowedClinicianBypassOASISAssist)
            {
                return;
            }

            if (CurrentAdmission.AdmissionKey != admissionDiscipline.AdmissionKey)
            {
                return;
            }

            if (IsQuestionInSurveyNotHidden("M0906") && ((RFA == "09")))
            {
                if (_dischargeDate != null)
                {
                    SetDateResponse(_dischargeDate,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0906_DC_TRAN_DTH_DT"));
                }
            }
        }

        private void SetupEncounterDefaults(Encounter encounter)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            // This setup is called from property changed events as well - so insure we are working with the proper patient
            if (!MappingAllowedClinician)
            {
                return;
            }

            // M1240  rfa 01,03
            // M1242  rfa 01,03,04,05,09
            if (IsQuestionInSurveyNotHidden("M1240"))
            {
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1240_FRML_PAIN_ASMT", 1));
            }
        }

        public void QuestionOasisMappingChanged(Question question, EncounterData encounterData)
        {
            // Questions with multiple versions: M1050, M1051, M1500, M1501, M1600, M1620, M2110, M2410

            if ((question == null) || (encounterData == null))
            {
                return;
            }

            if (question.QuestionOasisMapping == null)
            {
                return;
            }

            if (CurrentEncounterOasis == null)
            {
                return;
            }

            foreach (QuestionOasisMapping m in question.QuestionOasisMapping)
            {
                OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
                if (oa == null)
                {
                    continue;
                }

                OasisLayout ol = OasisCache.GetOasisLayoutByKey(oa.OasisLayoutKey, false);
                if (ol == null)
                {
                    continue;
                }

                if (ol.OasisVersionKey != OasisVersionKey)
                {
                    continue;
                }

                switch ((OasisMappingType)m.MappingType)
                {
                    case OasisMappingType.EverHavePain:
                        EverHavePainResponseChanged(encounterData.TextData);
                        break;
                    case OasisMappingType.LivingArraggementsA:
                        LivingArraggementsAChanged(m, encounterData.IntData);
                        break;
                    case OasisMappingType.LivingArraggementsB:
                        LivingArraggementsBChanged(m, encounterData.IntData);
                        break;
                    case OasisMappingType.CodeLookupRadio:
                        CodeLookupRadioChanged(m, encounterData.IntData);
                        break;
                    case OasisMappingType.CodeLookupRadioNull:
                        CodeLookupRadioNullChanged(m, encounterData.IntData);
                        break;
                    case OasisMappingType.CodeLookupMultiCheck:
                        CodeLookupMultiCheckChanged(m, encounterData.TextDataCodes(question.LookupType));
                        break;
                    case OasisMappingType.CodeLookupMultiCheckNull:
                        CodeLookupMultiCheckNullChanged(m, encounterData.TextDataCodes(question.LookupType));
                        break;
                    case OasisMappingType.CodeLookupMultiRadio:
                        CodeLookupMultiRadioChanged(m, encounterData.TextDataCodes(question.LookupType));
                        break;
                    case OasisMappingType.CodeLookupMultiRadioNullAll:
                        CodeLookupMultiRadioNullAllChanged(m, encounterData.TextDataCodes(question.LookupType));
                        break;
                    case OasisMappingType.CodeLookupMultiRadioNull:
                        CodeLookupMultiRadioNullChanged(m, encounterData.TextDataCodes(question.LookupType));
                        break;
                    case OasisMappingType.CodeLookupRadioTextNotNull:
                        CodeLookupRadioTextNotNullChanged(m, encounterData.TextData);
                        break;
                    case OasisMappingType.Grooming:
                        GroomingChanged(m, question, encounterData);
                        break;
                    case OasisMappingType.Catheter:
                        CatheterChanged(m, question, encounterData);
                        break;
                    case OasisMappingType.ReceivedFrom:
                        ReceivedFromChanged(encounterData);
                        break;
                    case OasisMappingType.Date:
                        DateChanged(m, question, encounterData);
                        break;
                    case OasisMappingType.CodeLookupCheckBoxPart:
                        CodeLookupCheckBoxPartChanged(m, encounterData.TextData);
                        break;
                    case OasisMappingType.ICD:
                        ICDChanged(m, encounterData);
                        break;
                    case OasisMappingType.GreaterThan:
                        GreaterThanChanged(m, encounterData.TextData);
                        break;
                    case OasisMappingType.LessThan:
                        LessThanChanged(m, encounterData.TextData);
                        break;
                    case OasisMappingType.CodeLookupRadioText:
                        CodeLookupRadioTextChanged(m, encounterData.TextData);
                        break;
                    case OasisMappingType.ICD10:
                        ICD10Changed(m, encounterData);
                        break;

                    default:
                        MessageBox.Show(String.Format(
                            "Error OasisManager.QuestionOasisMappingChanged: Invalid MappingType {0} for QuestionKey {1}.  Contact your system administrator.",
                            m.MappingType.ToString(), question.QuestionKey.ToString()));
                        break;
                }
            }
        }

        public void BradenScoreOasisMappingChanged(Question question, EncounterData encounterData)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            if (IsQuestionInSurveyNotHidden("M1300"))
            {
                // Set M1300 to 2 as the clinician has used a standardized tool (Braden Scale)
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1300_PRSR_ULCR_RISK_ASMT", 3));
            }

            if (IsQuestionInSurveyNotHidden("M1302"))
            {
                // Set M1302
                if (encounterData.IntData < 19)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1302_RISK_OF_PRSR_ULCR",
                            2)); // true
                }
                else
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1302_RISK_OF_PRSR_ULCR",
                            1)); // false
                }
            }
        }

        public void HeightWeightOasisMappingChanged(EncounterWeight ew)
        {
            if (ew == null)
            {
                return;
            }

            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            if (IsQuestionInSurveyNotHidden("M1060"))
            {
                // Don't overide a user entered dash for height with an empty string
                OasisAnswer oaHeight = OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1060_HEIGHT_A");
                if ((string.IsNullOrWhiteSpace(ew.HeightInInches) == false) ||
                    (string.IsNullOrWhiteSpace(ew.HeightInInches) && (GetTextResponse(oaHeight) != OASIS_DASH)))
                {
                    SetTextResponse(ew.HeightInInches, oaHeight);
                }

                WeightOasisMapping(ew);
            }
        }

        private void WeightOasisMapping(EncounterWeight ew)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            if (IsQuestionInSurveyNotHidden("M1060") == false)
            {
                return;
            }

            OasisAnswer oaWeight = OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1060_WEIGHT_B");
            // if weight passed and not empty - use that
            if ((ew != null) && (string.IsNullOrWhiteSpace(ew.WeightInPounds) == false))
            {
                SetTextResponse(ew.WeightInPounds, oaWeight);
                return;
            }

            // otherwise look for most recent weight in last 30 days
            if (CurrentAdmission == null)
            {
                return;
            }

            _m0090Date =
                GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"));
            if (_m0090Date == null)
            {
                _m0090Date = DateTime.Today.Date;
            }

            string weightInPounds = CurrentAdmission.GetMostRecentWeightInPounds(_m0090Date, 30);
            // Don't overide a user entered dash for weight with an empty string
            if ((string.IsNullOrWhiteSpace(weightInPounds) == false) ||
                (string.IsNullOrWhiteSpace(weightInPounds) && (GetTextResponse(oaWeight) != OASIS_DASH)))
            {
                SetTextResponse(weightInPounds, oaWeight);
            }
        }

        public void RiskOasisMappingChanged(Question question, EncounterRisk encounterRisk)
        {
            if ((question == null) || (encounterRisk == null))
            {
                return;
            }

            if (CurrentEncounterOasis == null)
            {
                return;
            }

            if (IsQuestionInSurveyNotHidden("M1910"))
            {
                if (encounterRisk.Score == null)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1910_MLT_FCTR_FALL_RISK_ASMT",
                            1));
                }
                else if (encounterRisk.Score <= 3)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1910_MLT_FCTR_FALL_RISK_ASMT",
                            2));
                }
                else
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1910_MLT_FCTR_FALL_RISK_ASMT",
                            3));
                }
            }
        }

        public bool IsLookbackQuestion(string question)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return false;
            }

            if (!IsOasisActive)
            {
                return false;
            }

            if (!MappingAllowedClinicianBypassOASISAssist)
            {
                return false;
            }

            if ((CurrentEncounterOasis.RFA != "06") && (CurrentEncounterOasis.RFA != "07") &&
                (CurrentEncounterOasis.RFA != "08") && (CurrentEncounterOasis.RFA != "09"))
            {
                return false;
            }

            if ((question.Equals("M1309")) || (question.Equals("M1313")))
            {
                return true;
            }

            if (question.Equals("M1500") || question.Equals("M1501") || question.Equals("M1510") ||
                question.Equals("M1511"))
            {
                return IsHeartFailureICD;
            }

            return (question.Equals("M2004") || question.Equals("M2005") || question.Equals("M2015") ||
                    question.Equals("M2016") || question.Equals("M2300") || question.Equals("M2301"))
                ? true
                : false;
        }

        public bool IsLookbackQuestionM2400orM2401(string question)
        {
            if (!IsOasisActive)
            {
                return false;
            }

            if (!MappingAllowedClinicianBypassOASISAssist)
            {
                return false;
            }

            return ((question.StartsWith("M2400")) || (question.StartsWith("M2401"))) ? true : false;
        }

        private void SetupLookbacks(bool AddingNewEncounter)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            SetupLookbackM1500orM1501(AddingNewEncounter);
            SetupLookbackM2004orM2005(AddingNewEncounter);
            SetupLookbackM2015orM2016(AddingNewEncounter);
            SetupLookbackM2300orM2301(AddingNewEncounter);
            SetupLookbackM2400orM2401a(AddingNewEncounter);
            SetupLookbackM2400orM2401b(AddingNewEncounter);
            SetupLookbackM2400orM2401c(AddingNewEncounter);
            SetupLookbackM2400orM2401d(AddingNewEncounter);
            SetupLookbackM2400orM2401e(AddingNewEncounter);
            SetupLookbackM2400orM2401f(AddingNewEncounter);
        }

        private void SetupLookbackM1500orM1501(bool AddingNewEncounter)
        {
            // Handles M1500orM1501 and M1510orM1511

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            // Bypass setup unless adding new encounter
            if (AddingNewEncounter == false)
            {
                return;
            }

            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.IsNew == false)
            {
                return;
            }

            if (CurrentEncounter.EncounterKey > 0 && CurrentEncounter.IsNew == false)
            {
                return;
            }

            OasisQuestion oqM1500orM1501 = GetQuestionInSurveyNotHidden("M1500");
            if (oqM1500orM1501 == null)
            {
                oqM1500orM1501 = GetQuestionInSurveyNotHidden("M1501");
                if (oqM1500orM1501 == null)
                {
                    return;
                }
            }

            if (oqM1500orM1501.OasisAnswer == null)
            {
                return;
            }

            // Set/Clear N/A to M1500orM1501 based on HeartFalure ICD, this will clear M1510orM1511 as well 
            OasisAnswer oa = oqM1500orM1501.OasisAnswer.Where(o => o.Sequence == 4).FirstOrDefault();
            if (oa != null)
            {
                SetRadioResponse((IsHeartFailureICD == false), oa);
            }

            if (IsHeartFailureICD == false)
            {
                return;
            }

            //Dyspnea
            List<EncounterData> edListDyspnea = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetEncounterDataDyspnea(StartDateLookback, EndDateLookback);
            int dyspneaCount = (edListDyspnea == null) ? 0 : edListDyspnea.Count();
            List<EncounterData> edListDyspneaNone = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetEncounterDataDyspneaNone(StartDateLookback, EndDateLookback);
            int dyspneaNoneCount = (edListDyspneaNone == null) ? 0 : edListDyspneaNone.Count();

            //Edema
            List<EncounterData> edListEdema = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetEncounterDataEdema(StartDateLookback, EndDateLookback);
            int edemaCount = (edListEdema == null) ? 0 : edListEdema.Count();
            List<EncounterData> edListEdemaNone = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetEncounterDataEdemaNone(StartDateLookback, EndDateLookback);
            int edemaNoneCount = (edListEdemaNone == null) ? 0 : edListEdemaNone.Count();

            //WeightGain
            var edListWeight = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetEncounterDataWeight(StartDateLookback, EndDateLookback);
            bool WeightGain = false;
            if (edListWeight != null)
            {
                if (edListWeight.Count > 1)
                {
                    float initialWeight = (float)edListWeight.FirstOrDefault().WeightLB;
                    float maxWeight = (float)edListWeight.Max(e => e.WeightLB);
                    if (initialWeight < maxWeight)
                    {
                        WeightGain = true;
                    }
                }
            }

            // Set Not Assessed if no Dyspnea or Edema, Set No if all Dyspnea and Edema are none/no, otherwise set yes
            oa = null;
            if (WeightGain)
            {
                oa = oqM1500orM1501.OasisAnswer.Where(o => o.Sequence == 2).FirstOrDefault();
            }
            else if (dyspneaCount + edemaCount == 0)
            {
                oa = oqM1500orM1501.OasisAnswer.Where(o => o.Sequence == 3).FirstOrDefault();
            }
            else if ((dyspneaCount == dyspneaNoneCount) && (edemaCount == edemaNoneCount))
            {
                oa = oqM1500orM1501.OasisAnswer.Where(o => o.Sequence == 1).FirstOrDefault();
            }
            else
            {
                oa = oqM1500orM1501.OasisAnswer.Where(o => o.Sequence == 2).FirstOrDefault();
            }

            if (oa != null)
            {
                SetRadioResponse(true, oa);
            }

            OasisQuestion oqM1510orM1511 = GetQuestionInSurveyNotHidden("M1510");
            if (oqM1510orM1511 == null)
            {
                oqM1510orM1511 = GetQuestionInSurveyNotHidden("M1511");
                if (oqM1510orM1511 == null)
                {
                    return;
                }
            }

            if (oqM1510orM1511.OasisAnswer == null)
            {
                return;
            }

            List<OrderEntry> oeListM1510R1 = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetOrderEntryM1510R1(StartDateLookback, EndDateLookback);
            List<OrderEntry> oeListM1510R5 = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetOrderEntryM1510R5(StartDateLookback, EndDateLookback);
            List<EncounterGoalElement> egeListM1510R1 = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetEncounterGoalElementM1510R1(StartDateLookback, EndDateLookback);
            List<EncounterGoalElement> egeListM1510R3 = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetEncounterGoalElementM1510R3(StartDateLookback, EndDateLookback);
            List<EncounterGoalElement> egeListM1510R4 = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetEncounterGoalElementM1510R4(StartDateLookback, EndDateLookback);
            if ((oeListM1510R1 != null) || (egeListM1510R1 != null))
            {
                OasisAnswer oaR1 = oqM1510orM1511.OasisAnswer.Where(o => o.Sequence == 2).FirstOrDefault();
                if (oaR1 != null)
                {
                    SetCheckBoxResponse(true, oaR1);
                }
            }

            if (egeListM1510R3 != null)
            {
                OasisAnswer oaR5 = oqM1510orM1511.OasisAnswer.Where(o => o.Sequence == 4).FirstOrDefault();
                if (oaR5 != null)
                {
                    SetCheckBoxResponse(true, oaR5);
                }
            }

            if (egeListM1510R4 != null)
            {
                OasisAnswer oaR5 = oqM1510orM1511.OasisAnswer.Where(o => o.Sequence == 5).FirstOrDefault();
                if (oaR5 != null)
                {
                    SetCheckBoxResponse(true, oaR5);
                }
            }

            if (oeListM1510R5 != null)
            {
                OasisAnswer oaR5 = oqM1510orM1511.OasisAnswer.Where(o => o.Sequence == 6).FirstOrDefault();
                if (oaR5 != null)
                {
                    SetCheckBoxResponse(true, oaR5);
                }
            }
        }

        private void SetupLookbackM2004orM2005(bool AddingNewEncounter)
        {
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            // Bypass setup unless adding new encounter
            if (AddingNewEncounter == false)
            {
                return;
            }

            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.IsNew == false)
            {
                return;
            }

            if (CurrentEncounter.EncounterKey > 0 && CurrentEncounter.IsNew == false)
            {
                return;
            }

            OasisQuestion oq = GetQuestionInSurveyNotHidden("M2004");
            if (oq == null)
            {
                oq = GetQuestionInSurveyNotHidden("M2005");
                if (oq == null)
                {
                    return;
                }
            }

            if (oq.OasisAnswer == null)
            {
                return;
            }


            List<OrderEntry> listM2004 = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetOrderEntryM2004(StartDateLookback, EndDateLookback);
            if (listM2004 != null)
            {
                OasisAnswer oaR1 = oq.OasisAnswer.Where(o => o.Sequence == 2).FirstOrDefault();
                if (oaR1 != null)
                {
                    SetRadioResponse(true, oaR1);
                }
            }
        }

        private void SetupLookbackM2015orM2016(bool AddingNewEncounter)
        {
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            // Bypass setup unless adding new encounter
            if (AddingNewEncounter == false)
            {
                return;
            }

            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.IsNew == false)
            {
                return;
            }

            if (CurrentEncounter.EncounterKey > 0 && CurrentEncounter.IsNew == false)
            {
                return;
            }

            OasisQuestion oq = GetQuestionInSurveyNotHidden("M2015");
            if (oq == null)
            {
                oq = GetQuestionInSurveyNotHidden("M2016");
                if (oq == null)
                {
                    return;
                }
            }

            OasisAnswer oa = null;
            bool found = false;
            if (CurrentFilteredPatientMedication != null)
            {
                foreach (PatientMedication p in CurrentFilteredPatientMedication)
                {
                    found = true;
                    break;
                }
            }

            if (found == false)
            {
                oa = oq.OasisAnswer.Where(o => o.Sequence == 3).FirstOrDefault();
            }
            else
            {
                List<EncounterGoalElement> egeListkM2015 = (CurrentAdmission == null)
                    ? null
                    : CurrentAdmission.GetEncounterGoalElementM2015(StartDateLookback, EndDateLookback);
                if (egeListkM2015 == null)
                {
                    oa = null; // don't assume NO if lookbacks exist
                }
                else
                {
                    oa = oq.OasisAnswer.Where(o => o.Sequence == 2).FirstOrDefault();
                }
            }

            if (oa != null)
            {
                SetRadioResponse(true, oa);
            }
        }

        private void SetupLookbackM2300orM2301(bool AddingNewEncounter)
        {
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            // Bypass setup unless adding new encounter
            if (AddingNewEncounter == false)
            {
                return;
            }

            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.IsNew == false)
            {
                return;
            }

            if (CurrentEncounter.EncounterKey > 0 && CurrentEncounter.IsNew == false)
            {
                return;
            }

            OasisQuestion oq = GetQuestionInSurveyNotHidden("M2300");
            if (oq == null)
            {
                oq = GetQuestionInSurveyNotHidden("M2301");
                if (oq == null)
                {
                    return;
                }
            }

            List<OrderEntry> oeListkM2300Y = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetOrderEntryM2300Y(StartDateLookback, EndDateLookback);
            List<OrderEntry> oeListkM2300N = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetOrderEntryM2300N(StartDateLookback, EndDateLookback);

            OasisAnswer oa = null;
            if (oeListkM2300Y != null)
            {
                oa = oq.OasisAnswer.Where(o => o.Sequence == 3).FirstOrDefault();
            }
            else if (oeListkM2300N != null)
            {
                oa = oq.OasisAnswer.Where(o => o.Sequence == 2).FirstOrDefault();
            }

            if (oa != null)
            {
                SetRadioResponse(true, oa);
            }
        }

        private void SetupLookbackM2400orM2401a(bool AddingNewEncounter)
        {
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            // Bypass setup unless adding new encounter
            if (AddingNewEncounter == false)
            {
                return;
            }

            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.IsNew == false)
            {
                return;
            }

            if (CurrentEncounter.EncounterKey > 0 && CurrentEncounter.IsNew == false)
            {
                return;
            }

            OasisQuestion oq = GetQuestionInSurveyNotHidden("M2400");
            if (oq == null)
            {
                oq = GetQuestionInSurveyNotHidden("M2401");
                if (oq == null)
                {
                    return;
                }
            }

            EncounterData edba = CurrentAdmission.GetEncounterBilateralAmputee(DateTime.MinValue, EndDateLookback);
            if ((IsDiabeticICD == false) || (edba != null))
            {
                OasisAnswer oaNA = oq.OasisAnswer.Where(o => o.Sequence == 3).FirstOrDefault();
                if (oaNA != null)
                {
                    SetRadioResponse(true, oaNA);
                }

                return;
            }

            List<EncounterGoalElement> egeListkM2400a = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetEncounterGoalElementM2400a(StartDateLookback, EndDateLookback);
            OasisAnswer oa = null;
            if (egeListkM2400a == null)
            {
                oa = oq.OasisAnswer.Where(o => o.Sequence == 1).FirstOrDefault();
            }
            else
            {
                oa = oq.OasisAnswer.Where(o => o.Sequence == 2).FirstOrDefault();
            }

            if (oa != null)
            {
                SetRadioResponse(true, oa);
            }
        }

        private void SetupLookbackM2400orM2401b(bool AddingNewEncounter)
        {
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            // Bypass setup unless adding new encounter
            if (AddingNewEncounter == false)
            {
                return;
            }

            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.IsNew == false)
            {
                return;
            }

            if (CurrentEncounter.EncounterKey > 0 && CurrentEncounter.IsNew == false)
            {
                return;
            }

            OasisQuestion oq = GetQuestionInSurveyNotHidden("M2400");
            if (oq == null)
            {
                oq = GetQuestionInSurveyNotHidden("M2401");
                if (oq == null)
                {
                    return;
                }
            }

            EncounterRisk er = CurrentAdmission.GetRiskAssessmentM2400b(StartDateLookback, EndDateLookback);
            if (er != null)
            {
                OasisAnswer oa = null;
                if (er.IsRisk)
                {
                    List<EncounterGoalElement> egeListkM2400b = (CurrentAdmission == null)
                        ? null
                        : CurrentAdmission.GetEncounterGoalElementM2400b(StartDateLookback, EndDateLookback);

                    if (egeListkM2400b == null)
                    {
                        oa = oq.OasisAnswer.Where(o => o.Sequence == 4).FirstOrDefault();
                    }
                    else
                    {
                        oa = oq.OasisAnswer.Where(o => o.Sequence == 5).FirstOrDefault();
                    }
                }
                else
                {
                    oa = oq.OasisAnswer.Where(o => o.Sequence == 6).FirstOrDefault();
                }

                if (oa != null)
                {
                    SetRadioResponse(true, oa);
                }
            }
        }

        private void SetupLookbackM2400orM2401c(bool AddingNewEncounter)
        {
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            // Bypass setup unless adding new encounter
            if (AddingNewEncounter == false)
            {
                return;
            }

            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.IsNew == false)
            {
                return;
            }

            if (CurrentEncounter.EncounterKey > 0 && CurrentEncounter.IsNew == false)
            {
                return;
            }

            OasisQuestion oq = GetQuestionInSurveyNotHidden("M2400");
            if (oq == null)
            {
                oq = GetQuestionInSurveyNotHidden("M2401");
                if (oq == null)
                {
                    return;
                }
            }

            if ((IsDepressionICD == false))
            {
                OasisAnswer oaNA = oq.OasisAnswer.Where(o => o.Sequence == 9).FirstOrDefault();
                if (oaNA != null)
                {
                    SetRadioResponse(true, oaNA);
                }

                return;
            }

            EncounterRisk er = CurrentAdmission.GetRiskAssessmentM2400c(StartDateLookback, EndDateLookback);
            if (er != null)
            {
                OasisAnswer oa = null;
                if (er.IsRisk)
                {
                    List<EncounterGoalElement> egeListkM2400c = (CurrentAdmission == null)
                        ? null
                        : CurrentAdmission.GetEncounterGoalElementM2400c(StartDateLookback, EndDateLookback);

                    if (egeListkM2400c == null)
                    {
                        oa = oq.OasisAnswer.Where(o => o.Sequence == 7).FirstOrDefault();
                    }
                    else
                    {
                        oa = oq.OasisAnswer.Where(o => o.Sequence == 8).FirstOrDefault();
                    }
                }
                else
                {
                    oa = oq.OasisAnswer.Where(o => o.Sequence == 9).FirstOrDefault();
                }

                if (oa != null)
                {
                    SetRadioResponse(true, oa);
                }
            }
        }

        private void SetupLookbackM2400orM2401d(bool AddingNewEncounter)
        {
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            // Bypass setup unless adding new encounter
            if (AddingNewEncounter == false)
            {
                return;
            }

            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.IsNew == false)
            {
                return;
            }

            if (CurrentEncounter.EncounterKey > 0 && CurrentEncounter.IsNew == false)
            {
                return;
            }

            OasisQuestion oq = GetQuestionInSurveyNotHidden("M2400");
            if (oq == null)
            {
                oq = GetQuestionInSurveyNotHidden("M2401");
                if (oq == null)
                {
                    return;
                }
            }

            //Pain
            List<EncounterPain> edListPain = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetEncounterPain(StartDateLookback, EndDateLookback);
            if ((edListPain == null))
            {
                OasisAnswer oaNA = oq.OasisAnswer.Where(o => o.Sequence == 12).FirstOrDefault();
                if (oaNA != null)
                {
                    SetRadioResponse(true, oaNA);
                }

                return;
            }

            List<EncounterGoalElement> egeListkM2400d = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetEncounterGoalElementM2400d(StartDateLookback, EndDateLookback);
            OasisAnswer oa = null;
            if (egeListkM2400d == null)
            {
                oa = oq.OasisAnswer.Where(o => o.Sequence == 10).FirstOrDefault();
            }
            else
            {
                oa = oq.OasisAnswer.Where(o => o.Sequence == 11).FirstOrDefault();
            }

            if (oa != null)
            {
                SetRadioResponse(true, oa);
            }
        }

        private void SetupLookbackM2400orM2401e(bool AddingNewEncounter)
        {
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            // DE4108 - Bypass setup unless adding new encounter
            if (AddingNewEncounter == false)
            {
                return;
            }

            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.IsNew == false)
            {
                return;
            }

            if (CurrentEncounter.EncounterKey > 0 && CurrentEncounter.IsNew == false)
            {
                return;
            }

            OasisQuestion oq = GetQuestionInSurveyNotHidden("M2400");
            if (oq == null)
            {
                oq = GetQuestionInSurveyNotHidden("M2401");
                if (oq == null)
                {
                    return;
                }
            }

            EncounterRisk er = CurrentAdmission.GetRiskAssessmentM2400e(StartDateLookback, EndDateLookback);
            if (er != null)
            {
                OasisAnswer oa = null;
                if (er.IsRisk)
                {
                    List<EncounterGoalElement> egeListkM2400e = (CurrentAdmission == null)
                        ? null
                        : CurrentAdmission.GetEncounterGoalElementM2400e(StartDateLookback, EndDateLookback);

                    if (egeListkM2400e == null)
                    {
                        oa = oq.OasisAnswer.Where(o => o.Sequence == 13).FirstOrDefault();
                    }
                    else
                    {
                        oa = oq.OasisAnswer.Where(o => o.Sequence == 14).FirstOrDefault();
                    }
                }
                else
                {
                    oa = oq.OasisAnswer.Where(o => o.Sequence == 15).FirstOrDefault();
                }

                if (oa != null)
                {
                    SetRadioResponse(true, oa);
                }
            }
        }

        private void SetupLookbackM2400orM2401f(bool AddingNewEncounter)
        {
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            // Bypass setup unless adding new encounter
            if (AddingNewEncounter == false)
            {
                return;
            }

            if (CurrentEncounter == null)
            {
                return;
            }

            if (CurrentEncounter.IsNew == false)
            {
                return;
            }

            if (CurrentEncounter.EncounterKey > 0 && CurrentEncounter.IsNew == false)
            {
                return;
            }

            OasisQuestion oq = GetQuestionInSurveyNotHidden("M2400");
            if (oq == null)
            {
                oq = GetQuestionInSurveyNotHidden("M2401");
                if (oq == null)
                {
                    return;
                }
            }

            IQueryable<AdmissionWoundSite> woundList = (CurrentFilteredAdmissionWoundSite == null)
                ? null
                : CurrentFilteredAdmissionWoundSite.OfType<AdmissionWoundSite>().AsQueryable();
            List<AdmissionWoundSite> unhealedPressureUlcerWoundList = (woundList == null)
                ? null
                : woundList.Where(w => (w.IsUnhealingPressureUlcer == true)).ToList();
            if ((unhealedPressureUlcerWoundList == null))
            {
                OasisAnswer oaNA = oq.OasisAnswer.Where(o => o.Sequence == 18).FirstOrDefault();
                if (oaNA != null)
                {
                    SetRadioResponse(true, oaNA);
                }

                return;
            }

            List<EncounterGoalElement> egeListkM2400f = (CurrentAdmission == null)
                ? null
                : CurrentAdmission.GetEncounterGoalElementM2400f(StartDateLookback, EndDateLookback);
            OasisAnswer oa = null;
            if (egeListkM2400f == null)
            {
                oa = oq.OasisAnswer.Where(o => o.Sequence == 16).FirstOrDefault();
            }
            else
            {
                oa = oq.OasisAnswer.Where(o => o.Sequence == 17).FirstOrDefault();
            }

            if (oa != null)
            {
                SetRadioResponse(true, oa);
            }
        }

        public void LookbackShowPopup(string question)
        {
            OasisLookbackChildWindow ol = new OasisLookbackChildWindow(this, question);
            ol.Show();
        }

        private bool _everHavePain;

        private void EverHavePainResponseChanged(string textData)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            // M1240  rfa 01,03
            // M1242  rfa 01,03,04,05,09
            _everHavePain = (textData == null) ? false : textData.Equals("0") ? false : true;
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            if (IsQuestionInSurveyNotHidden("M1240"))
            {
                if (!_everHavePain)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1240_FRML_PAIN_ASMT", 1));
                }
                else
                {
                    SetupEncounterPain(_encounterPain);
                }
            }

            if (IsQuestionInSurveyNotHidden("M1242"))
            {
                SetupAdmissionPainLocation(CurrentAdmission);
            }
        }

        private int? _livingArraggementsA;

        private void LivingArraggementsAChanged(QuestionOasisMapping m, int? intData)
        {
            if (intData == null)
            {
                _livingArraggementsA = null;
            }

            CodeLookup cl = (intData == null) ? null : CodeLookupCache.GetCodeLookupFromKey((int)intData);
            _livingArraggementsA = (cl == null) ? null : cl.Sequence;
            LivingArraggementsChanged(m);
        }

        private int? _livingArraggementsB;

        private void LivingArraggementsBChanged(QuestionOasisMapping m, int? intData)
        {
            if (intData == null)
            {
                _livingArraggementsB = null;
            }

            CodeLookup cl = (intData == null) ? null : CodeLookupCache.GetCodeLookupFromKey((int)intData);
            _livingArraggementsB = (cl == null) ? null : cl.Sequence;
            LivingArraggementsChanged(m);
        }

        private void LivingArraggementsChanged(QuestionOasisMapping m)
        {
            if (!IsOasisActive)
            {
                return;
            }
        }

        private void CodeLookupRadioChanged(QuestionOasisMapping m, int? intData)
        {
            // If form codelookup data is a specific code, set appropriate radio response
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (intData == null)
            {
                return;
            }

            CodeLookup cl = CodeLookupCache.GetCodeLookupFromKey((int)intData);
            if (cl == null)
            {
                return;
            }

            SetRadioResponse(cl.Code.ToLower().Equals(m.MappingData.ToLower()) ? true : false, oa);
        }

        private void CodeLookupRadioTextChanged(QuestionOasisMapping m, string textData)
        {
            // If form codelookup (textdata) is a specific code, set appropriate radio response
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(textData))
            {
                return;
            }

            if (textData.ToLower().Equals(m.MappingData.ToLower()))
            {
                SetRadioResponse(true, oa);
            }
            else if (GetRadioResponse(oa))
            {
                ClearResponse(oa, true, true);
            }
        }

        private void CodeLookupRadioNullChanged(QuestionOasisMapping m, int? intData)
        {
            // If form codelookup data is null, set appropriate radio response
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (intData == null)
            {
                SetRadioResponse(true, oa);
            }
            else if (intData == 0)
            {
                SetRadioResponse(true, oa);
            }
        }

        private void CodeLookupMultiCheckChanged(QuestionOasisMapping m, string textData)
        {
            // If form multi codelookup data contains value, set appropriate check response
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (textData == null)
            {
                return;
            }

            SetCheckBoxResponse(textData.ToLower().Contains("|" + m.MappingData.ToLower() + "|") ? true : false, oa);
        }

        private void CodeLookupMultiCheckNullChanged(QuestionOasisMapping m, string textData)
        {
            // If form multi codelookup data is null, set appropriate check response
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(textData))
            {
                SetCheckBoxResponse(true, oa);
            }
        }

        private void CodeLookupMultiRadioChanged(QuestionOasisMapping m, string textData)
        {
            // If form multi codelookup data contains the given value(s), set appropriate radio response - supports and (&&) and or (||) operators
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (textData == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(textData))
            {
                return;
            }

            string[] andDelimiter = { " && " };
            string[] orDelimiter = { " || " };
            string[] splitAndMappingData = m.MappingData.Split(andDelimiter, StringSplitOptions.RemoveEmptyEntries);
            string[] splitOrMappingData = m.MappingData.Split(orDelimiter, StringSplitOptions.RemoveEmptyEntries);
            if (splitAndMappingData.Length > 1)
            {
                bool and = true;
                foreach (string s in splitAndMappingData)
                {
                    and = textData.ToLower().Contains("|" + s.ToLower() + "|") ? true : false;
                    if (and == false)
                    {
                        break;
                    }
                }

                SetRadioResponse(and, oa);
            }
            else if (splitOrMappingData.Length > 1)
            {
                bool or = false;
                foreach (string s in splitOrMappingData)
                {
                    or = textData.ToLower().Contains("|" + s.ToLower() + "|") ? true : false;
                    if (or)
                    {
                        break;
                    }
                }

                SetRadioResponse(or, oa);
            }
            else
            {
                SetRadioResponse(textData.ToLower().Contains("|" + m.MappingData.ToLower() + "|") ? true : false, oa);
            }
        }

        private void CodeLookupMultiRadioNullAllChanged(QuestionOasisMapping m, string textData)
        {
            // If form multi codelookup data is null, clear all radio response
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            // clear all responses on null data
            if (string.IsNullOrWhiteSpace(textData))
            {
                foreach (OasisAnswer a in oa.OasisQuestion.OasisAnswer) SetRadioResponse(false, a);
            }
        }

        private void CodeLookupMultiRadioNullChanged(QuestionOasisMapping m, string textData)
        {
            // If form multi codelookup data is null, clear radio response
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(textData))
            {
                SetRadioResponse(true, oa);
            }
        }

        private void CodeLookupRadioTextNotNullChanged(QuestionOasisMapping m, string textData)
        {
            // If form text data is not null, set appropriate radio response
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            SetRadioResponse(((string.IsNullOrWhiteSpace(textData)) ? false : true), oa);
        }

        private string _gBrushingTeeth = "";
        private string _gHairCare = "";
        private string _gMakeup = "";
        private string _gShaving = "";

        private void GroomingChanged(QuestionOasisMapping m, Question question, EncounterData encounterData)
        {
            // If the response to any Grooming question Level of Assistance is Dependent, response 3 is checked.
            // If the response to any Grooming question Level of Assistance is Minimal, Moderate, or Maximum, response 2 is checked.
            // If the response to any Grooming question Level of Assistance is Independent, response 0 is checked.

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            CodeLookup cl = null;
            if (encounterData.IntData != null)
            {
                cl = CodeLookupCache.GetCodeLookupFromKey((int)encounterData.IntData);
            }

            string code = (cl == null) ? "" : cl.Code.ToLower();
            if (question.Label == "Brushing Teeth")
            {
                _gBrushingTeeth = code;
            }
            else if (question.Label == "Hair Care")
            {
                _gHairCare = code;
            }
            else if (question.Label == "Makeup")
            {
                _gMakeup = code;
            }
            else if (question.Label == "Shaving")
            {
                _gShaving = code;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (_gBrushingTeeth.StartsWith("dependent") || _gHairCare.StartsWith("dependent") ||
                _gMakeup.StartsWith("dependent") || _gShaving.StartsWith("dependent"))
            {
                oa = OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1800_CRNT_GROOMING", 4);
                if (oa != null)
                {
                    SetRadioResponse(true, oa);
                }
            }
            else if (_gBrushingTeeth.StartsWith("minimal") || _gHairCare.StartsWith("minimal") ||
                     _gMakeup.StartsWith("minimal") || _gShaving.StartsWith("minimal") ||
                     _gBrushingTeeth.StartsWith("moderate") || _gHairCare.StartsWith("moderate") ||
                     _gMakeup.StartsWith("moderate") || _gShaving.StartsWith("moderate") ||
                     _gBrushingTeeth.StartsWith("maximum") || _gHairCare.StartsWith("maximum") ||
                     _gMakeup.StartsWith("maximum") || _gShaving.StartsWith("maximum"))
            {
                oa = OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1800_CRNT_GROOMING", 3);
                if (oa != null)
                {
                    SetRadioResponse(true, oa);
                }
            }
            else if (_gBrushingTeeth.StartsWith("independent") || _gHairCare.StartsWith("independent") ||
                     _gMakeup.StartsWith("independent") || _gShaving.StartsWith("independent"))
            {
                oa = OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1800_CRNT_GROOMING", 1);
                if (oa != null)
                {
                    SetRadioResponse(true, oa);
                }
            }
        }

        private string _cCatheter = "";
        private string _cUrination = "";

        private void CatheterChanged(QuestionOasisMapping m, Question question, EncounterData encounterData)
        {
            // If indwelling catheter is yes, response 3 is checked. 
            // If indwelling catheter  = NO and Urination is 'incontinent' or 'Stress Incontinence', response 2 is checked. 
            // If indwelling catheter = NO and Urination is not 'incontinent' or 'Stress incontinence', response 1 is checked. 

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }


            if (question.Label == "Indwelling Catheter")
            {
                _cCatheter = encounterData.TextData;
            }
            else if (question.Label == "Urination")
            {
                _cUrination = encounterData.TextDataCodes("Urination");
            }

            _cCatheter = (_cCatheter == null) ? "" : _cCatheter.ToLower();
            _cUrination = (_cUrination == null) ? "" : _cUrination.ToLower();
            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (_cCatheter.Equals("1"))
            {
                oa = OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1610_UR_INCONT", 3);
                if (oa != null)
                {
                    SetRadioResponse(true, oa);
                }
            }
            else if (_cCatheter.Equals("0") &&
                     (_cUrination.Contains("|incontinent|") || _cUrination.Contains("|stress")))
            {
                oa = OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1610_UR_INCONT", 2);
                if (oa != null)
                {
                    SetRadioResponse(true, oa);
                }
            }
            else if (_cCatheter.Equals("0") &&
                     ((_cUrination.Contains("|incontinent|") || _cUrination.Contains("|stress")) == false))
            {
                oa = OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1610_UR_INCONT", 1);
                if (oa != null)
                {
                    SetRadioResponse(true, oa);
                }
            }
        }

        private void ReceivedFromChanged(EncounterData encounterData)
        {
            // Mark all that apply from the encounters over the last 14 days
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1000_DC_LTC_14_DA", 1);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }


            if (CurrentAdmission == null)
            {
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_LTC_14_DA"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_SNF_14_DA"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_IPPS_14_DA"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_LTCH_14_DA"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_IRF_14_DA"));
                SetCheckBoxResponse(false,
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_PSYCH_14_DA"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_OTH_14_DA"));
            }

            _m0090Date =
                GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"));
            if (_m0090Date == null)
            {
                _m0090Date = DateTime.Today.Date;
            }

            List<EncounterData> edList =
                CurrentAdmission.GetEncounterDataReceivedFrom(encounterData, ((DateTime)_m0090Date).AddDays(-13),
                    _m0090Date);
            if (edList == null)
            {
                SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_NONE_14_DA"));
                return;
            }

            bool found = false;
            foreach (EncounterData ed in edList)
                if (ed.TextData != null)
                {
                    if (ed.TextData == "1")
                    {
                        SetCheckBoxResponse(true,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_LTC_14_DA"));
                        found = true;
                    }
                    else if (ed.TextData == "2")
                    {
                        SetCheckBoxResponse(true,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_SNF_14_DA"));
                        found = true;
                    }
                    else if (ed.TextData == "3")
                    {
                        SetCheckBoxResponse(true,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_IPPS_14_DA"));
                        found = true;
                    }
                    else if (ed.TextData == "4")
                    {
                        SetCheckBoxResponse(true,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_LTCH_14_DA"));
                        found = true;
                    }
                    else if (ed.TextData == "5")
                    {
                        SetCheckBoxResponse(true,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_IRF_14_DA"));
                        found = true;
                    }
                    else if (ed.TextData == "6")
                    {
                        SetCheckBoxResponse(true,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_PSYCH_14_DA"));
                        found = true;
                    }
                    else if (ed.TextData == "7")
                    {
                        SetCheckBoxResponse(true,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_OTH_14_DA"));
                        found = true;
                    }
                    else if (ed.TextData.ToLower() == "other")
                    {
                        SetCheckBoxResponse(true,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_OTH_14_DA"));
                        found = true;
                    }
                }

            if (found == false)
            {
                SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1000_DC_NONE_14_DA"));
            }
        }

        private void DateChanged(QuestionOasisMapping m, Question question, EncounterData encounterData)
        {
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (encounterData == null)
            {
                return;
            }

            // Handle CheckBoxExclusive cases
            SetDateResponse(encounterData.DateTimeData, oa);
            bool unknown = (encounterData.BoolData == null) ? false : (bool)encounterData.BoolData;
            if (((encounterData.DateTimeData == null) || (unknown)) && (oa.IsType(OasisType.DateNAUK)))
            {
                SetAnswersNAUK(OasisCache.GetAllAnswersButMe(oa));
            }
        }

        private void CodeLookupCheckBoxPartChanged(QuestionOasisMapping m, string textData)
        {
            // If form codelookup data is a specific code, set appropriate check response, othewrwise clear it
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (textData == null)
            {
                SetCheckBoxResponse(false, oa);
                return;
            }

            SetCheckBoxResponse(textData.ToLower().Equals(m.MappingData.ToLower()) ? true : false, oa);
        }

        private void ICDChanged(QuestionOasisMapping m, EncounterData encounterData)
        {
            // encounterData.TextData is an ICD9
            // encounterData.Text2Data is an ICD10
            if (encounterData == null)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinicianOrICDCoder)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            SetICDResponse(encounterData.TextData, oa);
        }

        private void ICD10Changed(QuestionOasisMapping m, EncounterData encounterData)
        {
            // encounterData.TextData is an ICD9
            // encounterData.Text2Data is an ICD10
            if (encounterData == null)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinicianOrICDCoder)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(encounterData.Text2Data) == false)
            {
                SetICDResponse(encounterData.Text2Data, oa);
            }
            else
            {
                // If we are changing M1011 to null - only do so if we do not have an existing M1011 value
                string icd10 = GetICDResponse(oa, false);
                if (string.IsNullOrWhiteSpace(icd10))
                {
                    SetICDResponse(encounterData.Text2Data, oa);
                }
            }
        }

        private void GreaterThanChanged(QuestionOasisMapping m, string textData)
        {
            // if (textData > MappingData) set radio response - otherwise clear it
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(textData))
            {
                return;
            }

            float md = 0;
            float td = 0;
            try
            {
                md = float.Parse(m.MappingData);
            }
            catch
            {
                return;
            }

            try
            {
                td = float.Parse(textData);
            }
            catch
            {
                return;
            }

            SetRadioResponse((td > md), oa);
        }

        private void LessThanChanged(QuestionOasisMapping m, string textData)
        {
            // if (textData < MappingData) set radio response - otherwise clear it
            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            OasisAnswer oa = OasisCache.GetOasisAnswerByKey((int)m.OasisAnswerKey, false);
            if (oa == null)
            {
                return;
            }

            if (!IsQuestionInSurveyNotHidden(oa.OasisQuestion.Question))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(textData))
            {
                return;
            }

            float md = 0;
            float td = 0;
            try
            {
                md = float.Parse(m.MappingData);
            }
            catch
            {
                return;
            }

            try
            {
                td = float.Parse(textData);
            }
            catch
            {
                return;
            }

            SetRadioResponse((td < md), oa);
        }

        private EncounterPain _encounterPain;

        public void SetupEncounterPain(EncounterPain encounterPain)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            // This setup is called from property changed events as well - so insure we are working with the proper patient
            if (!MappingAllowedClinician)
            {
                return;
            }

            if (encounterPain == null)
            {
                return;
            }

            if (CurrentAdmission.AdmissionKey != encounterPain.AdmissionKey)
            {
                return;
            }

            _encounterPain = encounterPain;
            if (IsQuestionInSurveyNotHidden("M1240"))
            {
                int? painscore = null;
                int codekey = string.IsNullOrWhiteSpace(encounterPain.PainScale)
                    ? 0
                    : Int32.Parse(encounterPain.PainScale);
                string scale = CodeLookupCache.GetCodeFromKey("PAINSCALE", codekey);

                if (scale == "10")
                {
                    try
                    {
                        painscore = int.Parse(CodeLookupCache.GetCodeFromKey(encounterPain.PainScore10));
                    }
                    catch
                    {
                    }
                }
                else if (scale == "FLACC")
                {
                    painscore = encounterPain.PainScoreFLACC;
                }
                else if (scale == "PAINAD")
                {
                    painscore = encounterPain.PainScorePAINAD;
                }
                else if (scale == "FACES")
                {
                    try
                    {
                        painscore = int.Parse(CodeLookupCache.GetCodeFromKey(encounterPain.PainScoreFACES));
                    }
                    catch
                    {
                    }
                }

                if (painscore == null)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1240_FRML_PAIN_ASMT", 1));
                }
                else if (painscore < 7)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1240_FRML_PAIN_ASMT", 2));
                }
                else
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1240_FRML_PAIN_ASMT", 3));
                }
            }
        }

        public void SetupEncounterTransfer(EncounterTransfer encounterTransfer)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            // This setup is called from property changed events as well - so insure we are working with the proper patient
            if (!MappingAllowedClinician)
            {
                return;
            }

            if (CurrentEncounter == null)
            {
                return;
            }

            if (encounterTransfer == null)
            {
                return;
            }

            if (CurrentAdmission.AdmissionKey != encounterTransfer.AdmissionKey)
            {
                return;
            }

            EncounterTransfer et = CurrentEncounter.EncounterTransfer.FirstOrDefault();
            if (et == null)
            {
                return;
            }

            if (et != encounterTransfer)
            {
                return;
            }

            if ((IsQuestionInSurveyNotHidden("M0906")) && ((RFA == "06") || (RFA == "07")))
            {
                SetDateResponse(encounterTransfer.TransferDate,
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0906_DC_TRAN_DTH_DT"));
            }
        }

        public void SetupEncounterResumptionDefaults(EncounterResumption encounterResumption)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinicianBypassOASISAssist)
            {
                return;
            }

            // This setup is called from property changed events as well - so insure we are working with the proper patient
            if (CurrentAdmission.AdmissionKey != encounterResumption.AdmissionKey)
            {
                return;
            }

            if (RFA == "03")
            {
                SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0032_ROC_DT_NA"));
                if (encounterResumption.ResumptionDate != null)
                {
                    SetDateResponse(encounterResumption.ResumptionDate,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0032_ROC_DT"));
                }
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            if (RFA == "03")
            {
                SetCheckBoxResponse(true,
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0102_PHYSN_ORDRD_SOCROC_DT_NA"));
                if (encounterResumption.VerbalResumptionDate != null)
                {
                    SetDateResponse(encounterResumption.VerbalResumptionDate,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0102_PHYSN_ORDRD_SOCROC_DT"));
                }
                else
                {
                    SetDateResponse(encounterResumption.ResumptionReferralDate,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0104_PHYSN_RFRL_DT"));
                }
            }
        }

        private void ApplySkipLogic()
        {
            if (!IsOasisActive)
            {
                return;
            }

            RaiseIsWoundDimensionsVisibleChanged();
            foreach (OasisQuestion oq in OasisQuestions)
            {
                if (oq.OasisAnswer == null)
                {
                    continue;
                }

                if (oq.OasisAnswer.Any() == false)
                {
                    continue;
                }

                IEnumerable<OasisAnswer> oaList = oq.OasisAnswer.Where(a => (a.GotoSequence != null));
                if (oaList == null)
                {
                    continue;
                }

                foreach (OasisAnswer oa in oaList)
                    if ((oa.IsType(OasisType.Date)) || (oa.IsType(OasisType.DateNAUK)))
                    {
                        DateTime? dt = GetDateResponse(oa);
                        if (dt != null)
                        {
                            SetDateResponse(dt, oa);
                        }
                    }
                    else if (oa.IsType(OasisType.Radio))
                    {
                        bool r = GetRadioResponse(oa);
                        if (r)
                        {
                            SetRadioResponse(r, oa);
                        }
                    }
                    else if ((oa.IsType(OasisType.CheckBox)) || (oa.IsType(OasisType.CheckBoxExclusive)))
                    {
                        bool r = GetCheckBoxResponse(oa);
                        if (r)
                        {
                            SetCheckBoxResponse(r, oa);
                        }
                    }
                    else
                    {
                        MessageBox.Show(String.Format(
                            "Error OasisManager.ApplySkipLogic: {0} cannot support goto.  Contact your system administrator.",
                            oa.CachedOasisLayout.CMSField));
                    }
            }

            ApplySubFieldSkipLogic();
        }

        private void ApplySubFieldSkipLogic()
        {
            if (!IsOasisActive)
            {
                return;
            }

            foreach (OasisQuestion oq in OasisQuestions)
            {
                if (oq.OasisAnswer == null)
                {
                    continue;
                }

                if (oq.OasisAnswer.Any() == false)
                {
                    continue;
                }

                IEnumerable<OasisAnswer> oaList = oq.OasisAnswer.Where(a => (a.SubQuestionSkipFields != null));
                if (oaList == null)
                {
                    continue;
                }

                foreach (OasisAnswer oa in oaList)
                    if (oa.IsType(OasisType.PressureUlcer_C3))
                    {
                        string r = GetTextResponse(oa);
                        if (string.IsNullOrWhiteSpace(r) == false)
                        {
                            SetTextResponse(r, oa);
                        }
                    }
                    else if ((oa.IsType(OasisType.TopLegend)) || (oa.IsType(OasisType.LeftLegend)))
                    {
                        string r = GetTextResponse(oa);
                        if (string.IsNullOrWhiteSpace(r) == false)
                        {
                            SetTextResponse(r, oa, false);
                        }
                    }
                    else
                    {
                        MessageBox.Show(String.Format(
                            "Error OasisManager.ApplySubFieldLogic: {0} cannot support goto.  Contact your system administrator.",
                            oa.CachedOasisLayout.CMSField));
                    }
            }
        }

        private void NotifyAnswerResponseChanged(OasisAnswer oasisAnswer)
        {
            Messenger.Default.Send(oasisAnswer,
                string.Format("OasisAnswerResponse{0}_{1}", oasisAnswer.CachedOasisLayout.CMSField.Trim(),
                    OasisManagerGuid.ToString().Trim()));
        }

        private void NotifyQuestionHiddenChanged(OasisQuestion oasisQuestion, bool visible)
        {
            OasisManagerQuestion omq = OasisManagerQuestions.Where(q => (q.OasisQuestion == oasisQuestion))
                .FirstOrDefault();
            if (omq != null)
            {
                omq.Hidden = visible;
                OasisAlertCheckMeasuresForQuestion(oasisQuestion.OasisQuestionKey);
            }
        }

        private bool N0500Ais1orN0510Ais1
        {
            get
            {
                if (GetQuestionInSurveyNotHidden("N0500A") == null)
                {
                    return false;
                }

                if (GetQuestionInSurveyNotHidden("N0510A") == null)
                {
                    return false;
                }

                string N0500A = GetResponseB1Record(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "N0500A"),
                    CurrentEncounterOasis.B1Record);
                string N0510A = GetResponseB1Record(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "N0510A"),
                    CurrentEncounterOasis.B1Record);
                if ((N0500A == "1") || (N0510A == "1"))
                {
                    return true;
                }

                return false;
            }
        }

        private bool N0520Ais2
        {
            get
            {
                if (GetQuestionInSurveyNotHidden("N0520A") == null)
                {
                    return false;
                }

                string N0520A = GetResponseB1Record(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "N0520A"),
                    CurrentEncounterOasis.B1Record);
                if ((N0520A == "2"))
                {
                    return true;
                }

                return false;
            }
        }

        private int? GetGotoSequence(OasisAnswer oasisAnswer)
        {
            int? gotoSequence = GetGotoSequenceInternal(oasisAnswer);
            // special case Compound skip logic for N0520A - Was bowel regimen initiated or continued - Complete iff  N0500A=1 OR N0510A =1 - else goto Z0500
            if ((oasisAnswer.CachedOasisLayout.CMSField.Equals("N0500A")) ||
                (oasisAnswer.CachedOasisLayout.CMSField.Equals("N0510A")))
            {
                if (N0500Ais1orN0510Ais1)
                {
                    // Unskip N0520A, and maybe N0520B - depending on N0520A response
                    OasisAnswer oaN0510B = OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "N0510B");
                    int gotoEnd = (N0520Ais2) ? 14052002 : 26050002;
                    CheckForGoto(oaN0510B, gotoEnd, null);
                }
                else
                {
                    // Skip N0520A,N0520B
                    OasisAnswer oaN0510B = OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "N0510B");
                    CheckForGoto(oaN0510B, 14052001, 26050002);
                }
            }

            return gotoSequence;
        }

        private int? GetGotoSequenceInternal(OasisAnswer oasisAnswer)
        {
            if (oasisAnswer.CachedOasisLayout.CMSField.Contains("M2430_"))
            {
                return null; // this very special case will be handled elsewhere
            }

            if (oasisAnswer.CachedOasisLayout.CMSField.Contains("M1020_") ||
                oasisAnswer.CachedOasisLayout.CMSField.Contains("M1022_") ||
                oasisAnswer.CachedOasisLayout.CMSField.Contains("M1024_"))
            {
                return null; // skim off ICD9 1020
            }

            if (oasisAnswer.CachedOasisLayout.CMSField.Contains("M1021_") ||
                oasisAnswer.CachedOasisLayout.CMSField.Contains("M1023_") ||
                oasisAnswer.CachedOasisLayout.CMSField.Contains("M1025_"))
            {
                return null; // skim off ICD10 1021
            }

            if (oasisAnswer.IsType(OasisType.CheckBoxExclusive))
            {
                return (GetCheckBoxResponse(oasisAnswer)) ? oasisAnswer.GotoSequence : null;
            }

            if (oasisAnswer.IsType(OasisType.Date) || oasisAnswer.IsType(OasisType.DateNAUK))
            {
                if (oasisAnswer.GotoSequence == null)
                {
                    return null;
                }

                return (GetDateResponse(oasisAnswer) != null) ? oasisAnswer.GotoSequence : null;
            }

            if (oasisAnswer.IsType(OasisType.Radio) || oasisAnswer.IsType(OasisType.RadioHorizontal) ||
                oasisAnswer.IsType(OasisType.LivingArrangement) || oasisAnswer.IsType(OasisType.RadioWithDate))
            {
                if (oasisAnswer.OasisQuestion.OasisAnswer != null)
                {
                    foreach (OasisAnswer oa in oasisAnswer.OasisQuestion.OasisAnswer.OrderBy(o => o.Sequence))
                        if (oa.IsType(OasisType.Date) == false)
                        {
                            if (GetRadioResponse(oa))
                            {
                                return oa.GotoSequence;
                            }
                        }
                }

                return null;
            }

            if (oasisAnswer.GotoSequence != null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisManager.GetGotoSequence: {0} cannot support goto.  Contact your system administrator.",
                    oasisAnswer.CachedOasisLayout.CMSField));
                return null;
            }

            return null;
        }

        private void CheckForGoto(OasisAnswer oasisAnswer, int? startGoTo, int? endGoTo)
        {
            if ((startGoTo == null) && (endGoTo == null))
            {
                return;
            }

            if ((startGoTo == null) || (startGoTo == endGoTo))
            {
                MakeQuestionsHidden(true, oasisAnswer.OasisQuestion.Sequence + 1, (int)endGoTo - 1);
            }
            else if (endGoTo == null)
            {
                MakeQuestionsHidden(false, oasisAnswer.OasisQuestion.Sequence + 1, (int)startGoTo - 1);
            }
            else if (startGoTo < endGoTo)
            {
                MakeQuestionsHidden(true, (int)startGoTo, (int)endGoTo - 1);
            }
            else
            {
                MakeQuestionsHidden(false, (int)endGoTo, (int)startGoTo - 1);
            }
        }

        private void MakeQuestionsHidden(bool hidden, int startGoTo, int endGoTo)
        {
            if (OasisQuestions == null)
            {
                return;
            }

            List<OasisQuestion> loq = OasisQuestions.Where(o => (o.Sequence >= startGoTo) && (o.Sequence <= endGoTo))
                .OrderBy(o => (o.Sequence)).ToList();
            if (loq == null)
            {
                return;
            }

            foreach (OasisQuestion oq in loq) NotifyQuestionHiddenChanged(oq, hidden);
        }

        public void ClearResponse(OasisAnswer oasisAnswer, bool notifyAnswerResponseChanged = false,
            bool checkMeasures = false)
        {
            if (CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (oasisAnswer == null)
            {
                return;
            }

            if (oasisAnswer.CachedOasisLayout == null)
            {
                return;
            }

            CurrentEncounterOasis.B1Record =
                CurrentEncounterOasis.B1Record.Remove(oasisAnswer.CachedOasisLayout.StartPos - 1,
                    oasisAnswer.CachedOasisLayout.Length);
            CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Insert(
                oasisAnswer.CachedOasisLayout.StartPos - 1, new String(' ', oasisAnswer.CachedOasisLayout.Length));
            if (notifyAnswerResponseChanged)
            {
                NotifyAnswerResponseChanged(oasisAnswer);
            }

            if (checkMeasures)
            {
                OasisAlertCheckMeasuresForQuestion(oasisAnswer.OasisQuestionKey);
            }
        }

        public bool GetCheckBoxResponse(OasisAnswer oasisAnswer, bool showMessage = true)
        {
            if (!IsOasisActive)
            {
                return false;
            }

            if ((oasisAnswer == null) || (oasisAnswer.CachedOasisLayout == null))
            {
                return false;
            }

            OasisLayout oasisLayout = oasisAnswer.CachedOasisLayout;
            OasisType oasisType = (OasisType)oasisAnswer.CachedOasisLayout.Type;
            if (((oasisType == OasisType.CheckBox) || (oasisType == OasisType.CheckBoxExclusive)) == false)
            {
                if (showMessage)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisManager.GetCheckBoxResponse: {0} is not a CheckBox type.  Contact your system administrator.",
                        oasisLayout.CMSField));
                }

                return false;
            }

            if (oasisLayout.CMSField == "M1028_ACTV_DIAG_NONE")
            {
                return (CurrentEncounterOasis.B1Record.Substring(oasisLayout.StartPos - 1, oasisLayout.Length) ==
                        new String('0', oasisLayout.Length));
            }

            if (oasisLayout.CMSField == "M1028_ACTV_DIAG_NA")
            {
                return (CurrentEncounterOasis.B1Record.Substring(oasisLayout.StartPos - 1, oasisLayout.Length) ==
                        new String(OASIS_DASHCHAR, oasisLayout.Length));
            }

            if (oasisLayout.CMSField == "M1028_ACTV_DIAG_DASH1")
            {
                return (CurrentEncounterOasis.B1Record.Substring(oasisLayout.StartPos - 1, oasisLayout.Length) ==
                        new String(OASIS_DASHCHAR, oasisLayout.Length));
            }

            if (oasisLayout.CMSField == "M1028_ACTV_DIAG_DASH2")
            {
                return (CurrentEncounterOasis.B1Record.Substring(oasisLayout.StartPos - 1, oasisLayout.Length) ==
                        OASIS_DASH);
            }

            if (oasisLayout.CMSField == "GG0110DASH")
            {
                return (CurrentEncounterOasis.B1Record.Substring(oasisLayout.StartPos - 1, oasisLayout.Length) ==
                        new String(OASIS_DASHCHAR, oasisLayout.Length));
            }

            if (oasisLayout.CMSField == "M1030_IGNORE_EQUAL")
            {
                return (CurrentEncounterOasis.B1Record.Substring(oasisLayout.StartPos - 1, oasisLayout.Length) ==
                        new String(OASIS_EQUALCHAR, oasisLayout.Length));
            }

            if (oasisLayout.CMSField == "M2200_THER_NEED_IGNORE_EQUAL")
            {
                return (CurrentEncounterOasis.B1Record.Substring(oasisLayout.StartPos - 1, 1) ==
                        new String(OASIS_EQUALCHAR, 1));
            }

            return (CurrentEncounterOasis.B1Record.Substring(oasisLayout.StartPos - 1, 1) == "1");
        }

        public void SetCheckBoxResponse(bool response, OasisAnswer oasisAnswer)
        {
            if (CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if ((oasisAnswer == null) || (oasisAnswer.CachedOasisLayout == null))
            {
                return;
            }

            OasisLayout oasisLayout = oasisAnswer.CachedOasisLayout;
            OasisType oasisType = (OasisType)oasisAnswer.CachedOasisLayout.Type;
            if (((oasisType == OasisType.CheckBox) || (oasisType == OasisType.CheckBoxExclusive)) == false)
            {
                MessageBox.Show(String.Format(
                    "Error OasisManager.SetCheckBoxResponse: {0} is not a CheckBox type.  Contact your system administrator.",
                    oasisLayout.CMSField));
                return;
            }

            if (oasisLayout.CMSField.StartsWith("M1028"))
            {
                M1028SetCheckBoxResponse(response, oasisAnswer);
                return;
            }

            if (oasisLayout.CMSField.StartsWith("M1030") && IsOASISVersionD1orHigher)
            {
                M1030SetCheckBoxResponse(response, oasisAnswer);
                return;
            }

            if (oasisLayout.CMSField == "M2200_THER_NEED_IGNORE_EQUAL")
            {
                M2200_THER_NEED_IGNORE_EQUALSetCheckBoxResponse(response, oasisAnswer);
                return;
            }

            int? startGoTo = GetGotoSequence(oasisAnswer);
            if (oasisLayout.CMSField == "GG0110DASH")
            {
                CurrentEncounterOasis.B1Record =
                    CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, oasisLayout.Length);
                CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1,
                    new String(((response) ? OASIS_DASHCHAR : '0'), oasisLayout.Length));
            }
            else
            {
                CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 1);
                CurrentEncounterOasis.B1Record =
                    CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, ((response) ? "1" : "0"));
            }

            if ((oasisLayout.CMSField.ToUpper().StartsWith("M0150")) ||
                (oasisLayout.CMSField.ToUpper().StartsWith("A1400")))
            {
                CurrentEncounterOasis.MedicareOrMedicaid = CurrentEncounterOasis.IsMedicareOrMedicaid;
            }

            // Handle CheckBoxExclusive cases - bypassing special GG0110DASH as it was completely handled above
            if (response)
            {
                List<OasisAnswer> list = (oasisType == OasisType.CheckBoxExclusive)
                    ? OasisCache.GetAllAnswersButMe(oasisAnswer)
                    : OasisCache.GetAllExclusiveAnswersButMe(oasisAnswer);
                ResetAnswers(oasisAnswer, list);
                if ((oasisLayout.CMSField.StartsWith("GG0110")) && (oasisType != OasisType.CheckBoxExclusive))
                {
                    ResetAnswersFromDash(oasisAnswer, OasisCache.GetAllAnswersButMe(oasisAnswer));
                }
            }

            CheckForGoto(oasisAnswer, startGoTo, GetGotoSequence(oasisAnswer));
            OasisAlertCheckMeasuresForQuestion(oasisAnswer.OasisQuestionKey);
        }

        public void M1028SetCheckBoxResponse(bool response, OasisAnswer oasisAnswer)
        {
            List<OasisAnswer> list = null;
            OasisLayout oasisLayout = oasisAnswer.CachedOasisLayout;
            if (response)
            {
                if (oasisLayout.CMSField == "M1028_ACTV_DIAG_NONE")
                {
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 2);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, OASIS_ZERO + OASIS_ZERO);
                }
                else if (oasisLayout.CMSField == "M1028_ACTV_DIAG_NA")
                {
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 2);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, OASIS_DASH + OASIS_DASH);
                }
                else if (oasisLayout.CMSField == "M1028_ACTV_DIAG_DASH1")
                {
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 2);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, OASIS_DASH + OASIS_DASH);
                    OasisLayout ol =
                        OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1028_ACTV_DIAG_DASH2", "B1", false);
                    // dash M1028_ACTV_DIAG_NOA as well
                    if (ol != null)
                    {
                        CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(ol.StartPos - 1, 1);
                        CurrentEncounterOasis.B1Record =
                            CurrentEncounterOasis.B1Record.Insert(ol.StartPos - 1, OASIS_DASH);
                    }
                }
                else if (oasisLayout.CMSField == "M1028_ACTV_DIAG_NOA")
                {
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 1);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, "1");
                    list = OasisCache.GetAllNonExclusiveAnswersButMe(oasisAnswer);
                    if (list != null)
                    {
                        foreach (OasisAnswer oa in list)
                        {
                            CurrentEncounterOasis.B1Record =
                                CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                            CurrentEncounterOasis.B1Record =
                                CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1, OASIS_ZERO);
                        }
                    }
                }
                else
                {
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 1);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, "1");
                    list = OasisCache.GetAllNonExclusiveAnswersButMe(oasisAnswer);
                    if (list != null)
                    {
                        foreach (OasisAnswer oa in list)
                            if (GetResponse(oa.CachedOasisLayout) == OASIS_DASH)
                            {
                                CurrentEncounterOasis.B1Record =
                                    CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                                CurrentEncounterOasis.B1Record =
                                    CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1,
                                        OASIS_ZERO);
                            }
                    }

                    OasisAnswer oaNOA =
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1028_ACTV_DIAG_NOA", false);
                    if (oaNOA != null)
                    {
                        CurrentEncounterOasis.B1Record =
                            CurrentEncounterOasis.B1Record.Remove(oaNOA.CachedOasisLayout.StartPos - 1, 1);
                        CurrentEncounterOasis.B1Record =
                            CurrentEncounterOasis.B1Record.Insert(oaNOA.CachedOasisLayout.StartPos - 1, OASIS_ZERO);
                    }
                }
            }
            else if (response == false)
            {
                if (oasisLayout.CMSField == "M1028_ACTV_DIAG_NONE")
                {
                    list = OasisCache.GetAllNonExclusiveAnswersButMe(oasisAnswer);
                    if (list != null)
                    {
                        foreach (OasisAnswer oa in list)
                        {
                            CurrentEncounterOasis.B1Record =
                                CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                            CurrentEncounterOasis.B1Record =
                                CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1, OASIS_SPACE);
                        }
                    }
                }

                if ((oasisLayout.CMSField == "M1028_ACTV_DIAG_NA") || (oasisLayout.CMSField == "M1028_ACTV_DIAG_DASH1"))
                {
                    list = OasisCache.GetAllNonExclusiveAnswersButMe(oasisAnswer);
                    if (list != null)
                    {
                        foreach (OasisAnswer oa in list)
                        {
                            CurrentEncounterOasis.B1Record =
                                CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                            CurrentEncounterOasis.B1Record =
                                CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1, OASIS_SPACE);
                        }
                    }
                }
                else
                {
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 1);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, OASIS_ZERO);
                    list = OasisCache.GetAllNonExclusiveAnswersButMe(oasisAnswer);
                    if (list != null)
                    {
                        foreach (OasisAnswer oa in list)
                            if (GetResponse(oa.CachedOasisLayout) != "1")
                            {
                                CurrentEncounterOasis.B1Record =
                                    CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 1);
                                CurrentEncounterOasis.B1Record =
                                    CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, OASIS_SPACE);
                                CurrentEncounterOasis.B1Record =
                                    CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                                CurrentEncounterOasis.B1Record =
                                    CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1,
                                        OASIS_SPACE);
                            }
                    }
                }
            }

            list = OasisCache.GetAllAnswers(oasisAnswer);
            if (list == null)
            {
                return;
            }

            foreach (OasisAnswer oa in list) NotifyAnswerResponseChanged(oa);
        }

        public void M1030SetCheckBoxResponse(bool response, OasisAnswer oasisAnswer)
        {
            List<OasisAnswer> list = null;
            OasisLayout oasisLayout = oasisAnswer.CachedOasisLayout;
            if (response)
            {
                if (oasisLayout.CMSField == "M1030_IGNORE_EQUAL")
                {
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 1);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, OASIS_EQUAL);
                    list = OasisCache.GetAllNonExclusiveAnswersButMe(oasisAnswer);
                    if (list != null)
                    {
                        foreach (OasisAnswer oa in list)
                        {
                            CurrentEncounterOasis.B1Record =
                                CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                            CurrentEncounterOasis.B1Record =
                                CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1, OASIS_EQUAL);
                        }
                    }
                }
                else if (oasisLayout.CMSField == "M1030_THH_NONE_ABOVE")
                {
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 1);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, "1");
                    list = OasisCache.GetAllNonExclusiveAnswersButMe(oasisAnswer);
                    if (list != null)
                    {
                        foreach (OasisAnswer oa in list)
                        {
                            CurrentEncounterOasis.B1Record =
                                CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                            CurrentEncounterOasis.B1Record =
                                CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1, OASIS_ZERO);
                        }
                    }
                }
                else
                {
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 1);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, "1");

                    list = OasisCache.GetAllNonExclusiveAnswersButMe(oasisAnswer);
                    if (list != null)
                    {
                        foreach (OasisAnswer oa in list)
                        {
                            string resp = GetResponse(oa.CachedOasisLayout);
                            if ((resp == null) || (resp == OASIS_EQUAL))
                            {
                                CurrentEncounterOasis.B1Record =
                                    CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                                CurrentEncounterOasis.B1Record =
                                    CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1,
                                        OASIS_ZERO);
                            }
                        }
                    }

                    OasisAnswer oaNOA =
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1030_THH_NONE_ABOVE", false);
                    if (oaNOA != null)
                    {
                        CurrentEncounterOasis.B1Record =
                            CurrentEncounterOasis.B1Record.Remove(oaNOA.CachedOasisLayout.StartPos - 1, 1);
                        CurrentEncounterOasis.B1Record =
                            CurrentEncounterOasis.B1Record.Insert(oaNOA.CachedOasisLayout.StartPos - 1, OASIS_ZERO);
                    }
                }
            }
            else if (response == false)
            {
                if (oasisLayout.CMSField == "M1030_IGNORE_EQUAL")
                {
                    list = OasisCache.GetAllNonExclusiveAnswersButMe(oasisAnswer);
                    if (list != null)
                    {
                        foreach (OasisAnswer oa in list)
                        {
                            CurrentEncounterOasis.B1Record =
                                CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                            CurrentEncounterOasis.B1Record =
                                CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1, OASIS_SPACE);
                        }
                    }

                    OasisAnswer oaNOA =
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1030_THH_NONE_ABOVE", false);
                    if (oaNOA != null)
                    {
                        CurrentEncounterOasis.B1Record =
                            CurrentEncounterOasis.B1Record.Remove(oaNOA.CachedOasisLayout.StartPos - 1, 1);
                        CurrentEncounterOasis.B1Record =
                            CurrentEncounterOasis.B1Record.Insert(oaNOA.CachedOasisLayout.StartPos - 1, OASIS_SPACE);
                    }
                }
                else
                {
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 1);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, OASIS_ZERO);
                    list = OasisCache.GetAllNonExclusiveAnswersButMe(oasisAnswer);
                    if (list != null)
                    {
                        foreach (OasisAnswer oa in list)
                        {
                            string resp = GetResponse(oa.CachedOasisLayout);
                            if ((resp != "0") && (resp != "1"))
                            {
                                CurrentEncounterOasis.B1Record =
                                    CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                                CurrentEncounterOasis.B1Record =
                                    CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1,
                                        OASIS_SPACE);
                            }
                        }
                    }
                }
            }

            list = OasisCache.GetAllAnswers(oasisAnswer);
            if (list == null)
            {
                return;
            }

            foreach (OasisAnswer oa in list) NotifyAnswerResponseChanged(oa);
        }

        public void M2200_THER_NEED_IGNORE_EQUALSetCheckBoxResponse(bool response, OasisAnswer oasisAnswer)
        {
            List<OasisAnswer> list = OasisCache.GetAllAnswers(oasisAnswer);
            if (list == null)
            {
                return;
            }

            OasisAnswer oaM2200_THER_NEED_NBR = list.Where(oa => oa.CachedOasisLayout.CMSField == "M2200_THER_NEED_NBR")
                .FirstOrDefault();

            OasisLayout oasisLayout = oasisAnswer.CachedOasisLayout;
            if (response)
            {
                CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 1);
                CurrentEncounterOasis.B1Record =
                    CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, OASIS_EQUAL);
                if (oaM2200_THER_NEED_NBR?.CachedOasisLayout != null)
                {
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(
                        oaM2200_THER_NEED_NBR.CachedOasisLayout.StartPos - 1,
                        oaM2200_THER_NEED_NBR.CachedOasisLayout.Length);
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Insert(
                        oaM2200_THER_NEED_NBR.CachedOasisLayout.StartPos - 1,
                        OASIS_EQUAL + new String(' ', oaM2200_THER_NEED_NBR.CachedOasisLayout.Length - 1));
                }
            }
            else
            {
                CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 1);
                CurrentEncounterOasis.B1Record =
                    CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, OASIS_SPACE);
                if (oaM2200_THER_NEED_NBR?.CachedOasisLayout != null)
                {
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Remove(
                        oaM2200_THER_NEED_NBR.CachedOasisLayout.StartPos - 1,
                        oaM2200_THER_NEED_NBR.CachedOasisLayout.Length);
                    CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Insert(
                        oaM2200_THER_NEED_NBR.CachedOasisLayout.StartPos - 1,
                        new String(' ', oaM2200_THER_NEED_NBR.CachedOasisLayout.Length));
                }
            }

            foreach (OasisAnswer oa in list) NotifyAnswerResponseChanged(oa);
        }

        public Question GetOasisDynamicQuestion(OasisSurveyGroupQuestion gq, bool wasTrackingSheetAdded)
        {
            switch ((OasisType)gq.OasisQuestion.CachedOasisLayout.Type)
            {
                case OasisType.Radio:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionRadio",
                        BackingFactory = "OasisQuestionRadio"
                    };
                case OasisType.CheckBoxHeader:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionCheckBox",
                        BackingFactory = "OasisQuestionCheckBox"
                    };
                case OasisType.Date:
                case OasisType.DateNAUK:
                    return (gq.OasisQuestion.Question == "Z0500B")
                        ? new Question
                        {
                            QuestionKey = 0, Label = gq.OasisQuestion.Question,
                            DataTemplate = "OasisQuestionDateZ0500B", BackingFactory = "OasisQuestionDate"
                        }
                        : new Question
                        {
                            QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionDate",
                            BackingFactory = "OasisQuestionDate"
                        };
                case OasisType.TrackingSheet:
                    if (wasTrackingSheetAdded == false)
                    {
                        return new Question
                        {
                            QuestionKey = 0, Label = gq.OasisQuestion.Question,
                            DataTemplate = "OasisQuestionTrackingSheet", BackingFactory = "OasisQuestionTrackingSheet"
                        };
                    }

                    return null;
                case OasisType.Text:
                case OasisType.TextNAUK:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionText",
                        BackingFactory = "OasisQuestionText"
                    };
                case OasisType.ICD:
                    return (gq.OasisQuestion.Question == "M1012")
                        ? new Question
                        {
                            QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionICDM1012",
                            BackingFactory = "OasisQuestionICD"
                        }
                        : new Question
                        {
                            QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionICD",
                            BackingFactory = "OasisQuestionICD"
                        };
                case OasisType.ICDMedical:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionICDMedical",
                        BackingFactory = "OasisQuestionICDMedical"
                    };
                case OasisType.ICD10:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionICD10",
                        BackingFactory = "OasisQuestionICD10"
                    };
                case OasisType.ICD10Medical:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionICD10Medical",
                        BackingFactory = "OasisQuestionICD10Medical"
                    };
                case OasisType.ICD10MedicalV2:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionICD10MedicalV2", BackingFactory = "OasisQuestionICD10Medical"
                    };
                case OasisType.RadioWithDate:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionRadioWithDate",
                        BackingFactory = "OasisQuestionRadio"
                    };
                case OasisType.RadioHorizontal:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionRadioHorizontal", BackingFactory = "OasisQuestionRadio"
                    };
                case OasisType.LivingArrangement:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionLivingArrangement", BackingFactory = "OasisQuestionRadio"
                    };
                case OasisType.Synopsys:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = GetOasisQuestionSynopsysDataTemplate(), BackingFactory = "OasisQuestionRadio"
                    };
                case OasisType.PriorADL:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionPriorADL",
                        BackingFactory = "OasisQuestionRadio"
                    };
                case OasisType.PoorMed:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionPoorMed",
                        BackingFactory = "OasisQuestionRadio"
                    };
                case OasisType.CareManagement:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionCareManagement", BackingFactory = "OasisQuestionRadio"
                    };
                case OasisType.CareManagement_C1:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionCareManagement_C1", BackingFactory = "OasisQuestionRadio"
                    };
                case OasisType.DepressionScreening:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionDepressionScreening", BackingFactory = "OasisQuestionRadio"
                    };
                case OasisType.PressureUlcer:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionPressureUlcer",
                        BackingFactory = "OasisQuestionPressureUlcer"
                    };
                case OasisType.Filler:
                case OasisType.WoundDimension:
                    // WoundDimension handled by OasisTypeOasisQuestionPressureUlcer
                    return null;
                case OasisType.HISTrackingSheet:
                    if (wasTrackingSheetAdded == false)
                    {
                        return new Question
                        {
                            QuestionKey = 0, Label = gq.OasisQuestion.Question,
                            DataTemplate = "OasisQuestionHISTrackingSheet",
                            BackingFactory = "OasisQuestionHISTrackingSheet"
                        };
                    }

                    return null;
                case OasisType.PressureUlcerWorse:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionPressureUlcerWorse",
                        BackingFactory = "OasisQuestionPressureUlcerWorse"
                    };
                case OasisType.PressureUlcer_C1:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionPressureUlcer_C1", BackingFactory = "OasisQuestionPressureUlcer_C1"
                    };
                case OasisType.PressureUlcer_C2:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionPressureUlcer_C2", BackingFactory = "OasisQuestionPressureUlcer_C2"
                    };
                case OasisType.PressureUlcer_C3:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionPressureUlcer_C3", BackingFactory = "OasisQuestionPressureUlcer_C3"
                    };
                case OasisType.PressureUlcerWorse_C2:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionPressureUlcerWorse_C2",
                        BackingFactory = "OasisQuestionPressureUlcerWorse_C2"
                    };
                case OasisType.HeightWeight_C2:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionHeightWeight_C2", BackingFactory = "OasisQuestionHeightWeight_C2"
                    };
                case OasisType.GG0170C_C2:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionGG0170C_C2",
                        BackingFactory = "OasisQuestionGG0170C_C2"
                    };
                case OasisType.ServiceUtilization_10:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionServiceUtilization_10",
                        BackingFactory = "OasisQuestionServiceUtilization_10"
                    };
                case OasisType.ServiceUtilization_30:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question,
                        DataTemplate = "OasisQuestionServiceUtilization_30",
                        BackingFactory = "OasisQuestionServiceUtilization_30"
                    };
                case OasisType.TopLegend:
                case OasisType.LeftLegend:
                    return new Question
                    {
                        QuestionKey = 0, Label = gq.OasisQuestion.Question, DataTemplate = "OasisQuestionTopLegend",
                        BackingFactory = "OasisQuestionTopLegend"
                    };
                default:
                    MessageBox.Show(String.Format(
                        "Error DynamicFormViewModel.ProcessFormOasisGroupUI: {0} is not a valid question.  Contact your system administrator.",
                        gq.OasisQuestion.Question));
                    return null;
            }
        }

        private string GetOasisQuestionSynopsysDataTemplate()
        {
            if ((IsOASISVersionD1orHigher == false) || (((RFA == "06") || (RFA == "07") || (RFA == "09")) == false))
            {
                return "OasisQuestionSynopsys";
            }

            return "OasisQuestionSynopsysV2";
        }

        public bool GetA0245CheckBoxResponse(OasisAnswer oasisAnswer, bool showMessage = true)
        {
            if (!IsOasisActive)
            {
                return false;
            }

            if ((oasisAnswer == null) || (oasisAnswer.CachedOasisLayout == null))
            {
                return false;
            }

            OasisLayout oasisLayout = oasisAnswer.CachedOasisLayout;
            OasisType oasisType = (OasisType)oasisAnswer.CachedOasisLayout.Type;
            if ((oasisType == OasisType.Date) == false)
            {
                if (showMessage)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisManager.GetA0245CheckBoxResponse: {0} is not a CheckBox Date type.  Contact your system administrator.",
                        oasisLayout.CMSField));
                }

                return false;
            }

            string response = CurrentEncounterOasis.B1Record.Substring(oasisLayout.StartPos - 1, oasisLayout.Length);
            if (string.IsNullOrWhiteSpace(response))
            {
                return false;
            }

            if (response == A0245DASH)
            {
                return true;
            }

            return false;
        }

        public void SetA0245CheckBoxResponse(bool response, OasisAnswer oasisAnswer)
        {
            if (CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if ((oasisAnswer == null) || (oasisAnswer.CachedOasisLayout == null))
            {
                return;
            }

            OasisLayout oasisLayout = oasisAnswer.CachedOasisLayout;
            OasisType oasisType = (OasisType)oasisAnswer.CachedOasisLayout.Type;
            if ((oasisType == OasisType.Date) == false)
            {
                MessageBox.Show(String.Format(
                    "Error OasisManager.SetA0245CheckBoxResponse: {0} is not a CheckBox Date type.  Contact your system administrator.",
                    oasisLayout.CMSField));
                return;
            }

            SetResponse(((response) ? A0245DASH : null), oasisAnswer.CachedOasisLayout);
            NotifyAnswerResponseChanged(oasisAnswer);
            OasisAlertCheckMeasuresForQuestion(oasisAnswer.OasisQuestionKey);
        }

        public void SetCheckBoxResponseB1(bool response, OasisLayout oasisLayout, EncounterOasis encounterOasis)
        {
            if (encounterOasis.IsReadOnly)
            {
                return;
            }

            if (oasisLayout == null)
            {
                return;
            }

            if (oasisLayout.CMSField == "GG0110DASH")
            {
                encounterOasis.B1Record = encounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, oasisLayout.Length);
                encounterOasis.B1Record = encounterOasis.B1Record.Insert(oasisLayout.StartPos - 1,
                    new String(((response) ? OASIS_DASHCHAR : '0'), oasisLayout.Length));
            }
            else
            {
                encounterOasis.B1Record = encounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, 1);
                encounterOasis.B1Record =
                    encounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, ((response) ? "1" : "0"));
            }
        }

        public DateTime? GetDateResponse(OasisAnswer oasisAnswer, bool showmsg = true)
        {
            if (!IsOasisActive)
            {
                return null;
            }

            // B1 format yyyyMMdd, DateTime format M/D/yyyy
            if (oasisAnswer == null)
            {
                return null;
            }

            if (oasisAnswer.CachedOasisLayout == null)
            {
                return null;
            }

            if ((((oasisAnswer.IsType(OasisType.Date) || oasisAnswer.IsType(OasisType.DateNAUK)) == false)) ||
                (oasisAnswer.CachedOasisLayout.Length != 8))
            {
                if (showmsg)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisManager.GetDateResponse: {0} is not a Date type.  Contact your system administrator.",
                        oasisAnswer.CachedOasisLayout.CMSField));
                }

                return null;
            }

            string stringDate = (CurrentEncounterOasis.B1Record.Substring(oasisAnswer.CachedOasisLayout.StartPos - 1,
                oasisAnswer.CachedOasisLayout.Length));
            if (string.IsNullOrWhiteSpace(stringDate))
            {
                return null;
            }

            if (stringDate.Equals(BIRTHDAYDASH))
            {
                return null; // Birthdate can contain dashes
            }

            if (stringDate.Equals(A0245DASH))
            {
                return null; // A0245 can contain a dash
            }

            stringDate = stringDate.Substring(4, 2) + "/" + stringDate.Substring(6, 2) + "/" +
                         stringDate.Substring(0, 4);
            DateTime date = DateTime.Today;
            return DateTime.TryParse(stringDate, out date) ? date.Date : (DateTime?)null;
        }

        public void SetDateResponse(DateTime? response, OasisAnswer oasisAnswer, bool raisePropertyChanged = true)
        {
            if (CurrentEncounterOasis == null || CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            // B1 format yyyyMMdd, DateTime format M/D/yyyy
            if (oasisAnswer == null)
            {
                return;
            }

            if (oasisAnswer.CachedOasisLayout == null)
            {
                return;
            }

            var beginDateDiff =
                GetOasisDateChangeEvent("BEGIN", "SetDateResponse", oasisAnswer.CachedOasisLayout.CMSField);
            if ((((oasisAnswer.IsType(OasisType.Date) || oasisAnswer.IsType(OasisType.DateNAUK)) == false)) ||
                (oasisAnswer.CachedOasisLayout.Length != 8))
            {
                MessageBox.Show(String.Format(
                    "Error OasisManager.SetDateResponse: {0} is not a Date type.  Contact your system administrator.",
                    oasisAnswer.CachedOasisLayout.CMSField));
                return;
            }

            int? startGoTo = GetGotoSequence(oasisAnswer);
            if ((oasisAnswer.CachedOasisLayout.CMSField.ToUpper().StartsWith("M0090")) ||
                (oasisAnswer.CachedOasisLayout.CMSField.ToUpper().StartsWith("Z0500B")))
            {
                CurrentEncounterOasis.M0090 = null;
            }

            ClearResponse(oasisAnswer);
            bool notifyAnswerResponseChanged = false;
            if (((response == null) || (response == null)) == false)
            {
                if (oasisAnswer.CachedOasisLayout.CMSField.ToUpper().StartsWith("M0090"))
                {
                    response = CheckD1ResetM0090((DateTime)response);
                    notifyAnswerResponseChanged = true;
                }

                string stringDate = string.Format("{0:yyyy}{1:MM}{2:dd}", ((DateTime)response), ((DateTime)response),
                    ((DateTime)response));
                CurrentEncounterOasis.B1Record =
                    CurrentEncounterOasis.B1Record.Remove(oasisAnswer.CachedOasisLayout.StartPos - 1,
                        oasisAnswer.CachedOasisLayout.Length);
                CurrentEncounterOasis.B1Record =
                    CurrentEncounterOasis.B1Record.Insert(oasisAnswer.CachedOasisLayout.StartPos - 1, stringDate);
                if ((oasisAnswer.CachedOasisLayout.CMSField.ToUpper().StartsWith("M0090")) ||
                    (oasisAnswer.CachedOasisLayout.CMSField.ToUpper().StartsWith("Z0500B")))
                {
                    CurrentEncounterOasis.M0090 = ((DateTime)response).Date;
                }

                // Handle CheckBoxExclusive cases
                ResetAnswers(oasisAnswer, OasisCache.GetAllAnswersButMe(oasisAnswer));
                // Handle special case of M1307 answers interaction
                if (oasisAnswer.CachedOasisLayout.CMSField == "M1307_OLDST_STG2_ONST_DT")
                {
                    OasisAnswer oasisAnswerParentRadio =
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1307_OLDST_STG2_AT_DSCHRG",
                            2);
                    SetRadioResponse(true, oasisAnswerParentRadio);
                    NotifyAnswerResponseChanged(oasisAnswerParentRadio);
                }
            }
            else if (oasisAnswer.CachedOasisLayout.CMSField == "M0066_PAT_BIRTH_DT") // null birthdate to dashses
            {
                SetResponse(BIRTHDAYDASH, oasisAnswer.CachedOasisLayout);
            }
            else if (oasisAnswer.CachedOasisLayout.CMSField == "A0245") // null A0245 to a dash 
            {
                NotifyAnswerResponseChanged(oasisAnswer);
            }

            if (oasisAnswer.CachedOasisLayout.CMSField == "M0090_INFO_COMPLETED_DT")
            {
                ChangeVersionIfWeCan();
            }

            CheckForGoto(oasisAnswer, startGoTo, GetGotoSequence(oasisAnswer));
            if ((raisePropertyChanged) && (oasisAnswer.CachedOasisLayout.CMSField == "M0090_INFO_COMPLETED_DT"))
            {
                SetupAdmissionOasisHeaderDefaultsOASIS(CurrentAdmission.AdmissionKey);
                // Don't let M0090 effect these on a re-edit
                if ((MappingAllowedClinician) && (CurrentForm != null) &&
                    (CurrentForm.IsOasis == false)) // In standalone - don't overwrite from initial settings
                {
                    SetupAdmissionDiagnosis(CurrentAdmission);
                    SetupPatientMedication(CurrentPatient);
                    SetupAdmissionPainLocation(CurrentAdmission);
                    SetupAdmissionWoundSite(CurrentAdmission);
                }

                SetupPatientInsurance(CurrentPatient, CurrentAdmission);
            }

            OasisAlertCheckMeasuresForQuestion(oasisAnswer.OasisQuestionKey);

            var endDateDiff = GetOasisDateChangeEvent("END", "SetDateResponse", oasisAnswer.CachedOasisLayout.CMSField);
            if (notifyAnswerResponseChanged)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => NotifyAnswerResponseChanged(oasisAnswer));
            }
        }

        private DateTime CheckD1ResetM0090(DateTime M0090)
        {
            // OASIS - D1 update for support of PDGM has been announced and will be required for 'Transition Recertifications (RFA 4)" 
            // when the M0090 date for the assessment would normally have been 12/27/19 - 12/31/19, 
            // and the assessment would have been for a payment episode that begins January 1, 2020 or later.
            // Requirements:
            // - Allow the user - for and RFA4 being entered in the aforementioned time frame - to be collected on and OASIS - D1 data set 
            //   and to be submitted with an artificial M0090 date of 1 / 1 / 2020.
            // - RFA4 that meet the aforementioned criteria will NOT be submitted to CMS prior to 1 / 1 / 2020.

            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CD != "OASIS") ||
                (string.IsNullOrWhiteSpace(RFA)))
            {
                return M0090;
            }

            if (CurrentAdmission?.SOCDate == null)
            {
                return M0090;
            }

            if (RFA != "04")
            {
                return M0090;
            }

            OasisVersion ov = OasisCache.GetOasisVersionBySYSCDandVersionCD1("OASIS", "D1-012020");
            if ((ov == null) || (ov.EffectiveDate == null))
            {
                return M0090;
            }

            DateTime effectiveDate = ((DateTime)ov.EffectiveDate).Date;
            if (M0090.Date < effectiveDate.AddDays(-5).Date)
            {
                return M0090;
            }

            if (M0090.Date > effectiveDate.AddDays(-1).Date)
            {
                return M0090;
            }

            if (AssessmentForPaymentEpisodeOnOrAfterEffectiveDate(((DateTime)CurrentAdmission.SOCDate).Date, M0090,
                    effectiveDate) == false)
            {
                return M0090;
            }

            return effectiveDate;
        }

        private bool AssessmentForPaymentEpisodeOnOrAfterEffectiveDate(DateTime SOCDate, DateTime M0090,
            DateTime EffectiveDate)
        {
            DateTime fuddDate = SOCDate.Date;
            while (fuddDate < EffectiveDate) fuddDate = fuddDate.AddDays(60);
            DateTime
                FiveDateBefore =
                    fuddDate.AddDays(
                        -5); // Get 5 day window start date of payment episode that begins on or after the passed effective date (model is January 1, 2020).
            return
                (M0090 < FiveDateBefore)
                    ? false
                    : true; // if M0090 is on or after that date we are an AssessmentForPaymentEpisodeOnOrAfterEffectiveDate
        }

        private bool ChangeVersionIfWeCan()
        {
            if (ChangeVersionIfWeCanEASY())
            {
                return true;
            }

            return ChangeVersionIfWeCanHARD();
        }

        private bool ChangeVersionIfWeCanEASY()
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.M0090 == null) ||
                (CurrentEncounterOasis.SYS_CD != "OASIS") || (string.IsNullOrWhiteSpace(RFA)))
            {
                return false;
            }

            OasisVersion ov = OasisCache.GetOasisVersionBySYSCDandEffectiveDate(CurrentEncounterOasis.SYS_CD,
                (DateTime)CurrentEncounterOasis.M0090);
            if (ov == null)
            {
                return false;
            }

            if ((OasisVersionKey == ov.OasisVersionKey) && (VersionCD2 == ov.VersionCD2))
            {
                return false;
            }

            // Only allow OASIS version changes from 2.20 (C2-012017) to 2.21 (C2-012018) and visa-versa... as they are effectively the same version
            // To allow other version (or RFGA) changes, there would be much more work to do - including form sections and question response copying and re-mapping...
            if ((VersionCD2 != "2.20") && (VersionCD2 != "2.21"))
            {
                return false;
            }

            if ((ov.VersionCD2 != "2.20") && (ov.VersionCD2 != "2.21"))
            {
                return false;
            }

            OasisVersionKey = ov.OasisVersionKey;
            VersionCD2 = ov.VersionCD2;
            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD1"))
            {
                SetResponse(ov.VersionCD1, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD1"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD2"))
            {
                SetResponse(ov.VersionCD2, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD2"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "ITM_SET_VRSN_CD"))
            {
                SetResponse(ov.VersionCD1, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SET_VRSN_CD"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SPEC_VRSN_CD"))
            {
                SetResponse(ov.VersionCD2, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SPEC_VRSN_CD"));
            }

            CurrentEncounterOasis.OasisVersionKey = OasisVersionKey;
            return true;
        }

        private bool ChangeVersionIfWeCanHARD()
        {
            if (SurveyEverBeenTransmitted || (CurrentEncounterOasis == null) || (CurrentEncounterOasis.M0090 == null) ||
                (CurrentEncounterOasis.SYS_CD != "OASIS") || (string.IsNullOrWhiteSpace(RFA)))
            {
                return false;
            }

            OasisVersion ov = OasisCache.GetOasisVersionBySYSCDandEffectiveDate(CurrentEncounterOasis.SYS_CD,
                (DateTime)CurrentEncounterOasis.M0090);
            if (ov == null)
            {
                return false;
            }

            if ((OasisVersionKey == ov.OasisVersionKey) && (VersionCD2 == ov.VersionCD2))
            {
                return false;
            }

            // Only allow OASIS version changes from 2.21 (C2-012018) to 2.30 (D-012019) 2.31 (D1-012020) and visa-versa... as they are effectively the same version  
            if ((VersionCD2 != "2.20") && (VersionCD2 != "2.21") && (VersionCD2 != "2.30") && (VersionCD2 != "2.31"))
            {
                return false;
            }

            if ((ov.VersionCD2 != "2.20") && (ov.VersionCD2 != "2.21") && (ov.VersionCD2 != "2.30") &&
                (ov.VersionCD2 != "2.31"))
            {
                return false;
            }

            // There are two ways we can do this - 
            // 1) copy the version-common fields from the old version B1Record to the new version B1Record
            // 2) create a new version B1record from scratch - and call the initialization/default routines to setup the data fro the new version
            //  - given the simularties between 2.20/2.21 and 2.30/2.31 - we went with option 1 - as we get more preservation of data
            string prevB1Record = CurrentEncounterOasis.B1Record;
            string rfa = RFA;
            CurrentEncounterOasis.B1Record = new String(' ', GetB1RecordLength(CurrentEncounterOasis.OasisVersionKey));
            List<OasisLayout> olListCopyTo = OasisCache.GetAllAnswersByVersionAndRFA(ov.OasisVersionKey, rfa);
            if (olListCopyTo == null)
            {
                return false;
            }

            OasisLayout olCopyFrom = null;
            foreach (OasisLayout olCopyTo in olListCopyTo)
            {
                olCopyFrom =
                    OasisCache.GetOasisLayoutByCMSFieldAndRFA(OasisVersionKey, olCopyTo.CMSField, rfa, "B1", false);
                if (olCopyFrom != null)
                {
                    SetResponse(GetResponseB1Record(olCopyFrom, prevB1Record), olCopyTo);
                }
            }

            // Override the version into in the new record
            OasisVersionKey = ov.OasisVersionKey;
            VersionCD2 = ov.VersionCD2;
            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD1"))
            {
                SetResponse(ov.VersionCD1, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD1"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD2"))
            {
                SetResponse(ov.VersionCD2, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "VERSION_CD2"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "ITM_SET_VRSN_CD"))
            {
                SetResponse(ov.VersionCD1, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SET_VRSN_CD"));
            }

            if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "SPEC_VRSN_CD"))
            {
                SetResponse(ov.VersionCD2, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SPEC_VRSN_CD"));
            }

            CurrentEncounterOasis.OasisVersionKey = OasisVersionKey;
            // Call back to Dynamic Form to reconstitute the OASIS questions from the new question set for this version
            Messenger.Default.Send(CurrentEncounterOasis.EncounterOasisKey,
                string.Format("OasisVersionChanged{0}",
                    ((CurrentAdmission != null) ? CurrentAdmission.AdmissionKey.ToString().Trim() : "0")));

            return true;
        }

        private bool SurveyEverBeenTransmitted
        {
            get
            {
                if (CurrentEncounter?.EncounterOasis == null)
                {
                    return false;
                }

                bool surveyEverBeenTransmitted = CurrentEncounter.EncounterOasis.Where(o => o.CMSTransmission).Any();
                return surveyEverBeenTransmitted;
            }
        }

        public void SetDateResponseB1(DateTime? response, OasisLayout oasisLayout, EncounterOasis encounterOasis)
        {
            if (encounterOasis.IsReadOnly)
            {
                return;
            }

            if (oasisLayout == null)
            {
                return;
            }

            if ((oasisLayout.CMSField.ToUpper().StartsWith("M0090")) ||
                (oasisLayout.CMSField.ToUpper().StartsWith("Z0500B")))
            {
                encounterOasis.M0090 = null;
            }

            ClearResponseB1(oasisLayout, encounterOasis);
            if (((response == null) || (response == null)) == false)
            {
                string stringDate = string.Format("{0:yyyy}{1:MM}{2:dd}", ((DateTime)response), ((DateTime)response),
                    ((DateTime)response));
                encounterOasis.B1Record = encounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, oasisLayout.Length);
                encounterOasis.B1Record = encounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, stringDate);
                if ((oasisLayout.CMSField.ToUpper().StartsWith("M0090")) ||
                    (oasisLayout.CMSField.ToUpper().StartsWith("Z0500B")))
                {
                    encounterOasis.M0090 = ((DateTime)response).Date;
                }
            }
            else if (oasisLayout.CMSField == "M0066_PAT_BIRTH_DT") // null birthdate to dashses
            {
                SetTextResponseB1(BIRTHDAYDASH, oasisLayout, encounterOasis);
            }
            else if (oasisLayout.CMSField == "A0245") // null A0245 to a dash 
            {
                SetTextResponseB1(A0245DASH, oasisLayout, encounterOasis);
            }
        }

        public bool GetRadioResponse(OasisAnswer oasisAnswer, bool showmsg = true)
        {
            if (!IsOasisActive)
            {
                return false;
            }

            if ((oasisAnswer == null) || (oasisAnswer.CachedOasisLayout == null))
            {
                return false;
            }

            OasisLayout oasisLayout = oasisAnswer.CachedOasisLayout;
            OasisType oasisType = (OasisType)oasisAnswer.CachedOasisLayout.Type;
            if (((oasisType == OasisType.Radio) || (oasisType == OasisType.RadioHorizontal) ||
                 (oasisType == OasisType.LivingArrangement) || (oasisType == OasisType.RadioWithDate)) == false)
            {
                if (showmsg)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisManager.GetRadioResponse: {0} is not a RadioButton type.  Contact your system administrator.",
                        oasisLayout.CMSField));
                }

                return false;
            }

            string value = oasisAnswer.AnswerLabel;
            if (value.Length < oasisLayout.Length) // Assumes radio fields have a length of 1 or 2 ONLY
            {
                if ((value != OASIS_DASH) && (value != OASIS_EQUAL))
                {
                    value = "0" + value;
                }
                else
                {
                    value = value + " ";
                }
            }

            bool ret = (value == CurrentEncounterOasis.B1Record.Substring(oasisLayout.StartPos - 1, oasisLayout.Length))
                ? true
                : false;
            return ret;
        }

        public void SetRadioResponse(bool response, OasisAnswer oasisAnswer)
        {
            if (CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if ((oasisAnswer == null) || (oasisAnswer.CachedOasisLayout == null))
            {
                return;
            }

            OasisLayout oasisLayout = oasisAnswer.CachedOasisLayout;
            OasisType oasisType = (OasisType)oasisAnswer.CachedOasisLayout.Type;
            if (((oasisType == OasisType.Radio) || (oasisType == OasisType.RadioHorizontal) ||
                 (oasisType == OasisType.LivingArrangement) || (oasisType == OasisType.RadioWithDate)) == false)
            {
                MessageBox.Show(String.Format(
                    "Error OasisManager.SetRadioResponse: {0} is not a RadioButton type.  Contact your system administrator.",
                    oasisLayout.CMSField));
                return;
            }

            if (response)
            {
                int? startGoTo = GetGotoSequence(oasisAnswer);
                string value = oasisAnswer.AnswerLabel;
                if (value.Length < oasisLayout.Length) // Assumes radio fields have a length of 1 or 2 ONLY
                {
                    if ((value != OASIS_DASH) && (value != OASIS_EQUAL))
                    {
                        value = "0" + value;
                    }
                    else
                    {
                        value = value + " ";
                    }
                }

                CurrentEncounterOasis.B1Record =
                    CurrentEncounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, value.Length);
                CurrentEncounterOasis.B1Record = CurrentEncounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, value);
                CheckForGoto(oasisAnswer, startGoTo, GetGotoSequence(oasisAnswer));
                // Handle very special case for M2410
                if (oasisAnswer.GotoSequence == 2430)
                {
                    MakeQuestionsHidden(true, 2440, 2440);
                }

                if (oasisAnswer.GotoSequence == 2440)
                {
                    MakeQuestionsHidden(false, 2440, 2440);
                }

                if ((oasisLayout.CMSField == "M0100_ASSMT_REASON") || (oasisLayout.CMSField == "A0250"))
                {
                    CurrentEncounterOasis.RFA = GetResponse(oasisLayout);
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "ITM_SBST_CD"))
                    {
                        SetResponse(CurrentEncounterOasis.ITM_SBST_CD,
                            OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SBST_CD"));
                    }
                }

                if (oasisLayout.CMSField == "M0080_ASSESSOR_DISCIPLINE")
                {
                    if ((DynamicFormViewModel != null) && (DynamicFormViewModel.SignatureQuestion != null))
                    {
                        DynamicFormViewModel.SignatureQuestion.SetupEncounterCollectedBy(value, true, true);
                    }

                    if ((CurrentEncounter != null) && (CurrentForm != null) && CurrentForm.IsOasis)
                    {
                        ServiceType serviceType = GetOASISServiceType(value);
                        if ((serviceType != null) && (CurrentEncounter.ServiceTypeKey != serviceType.ServiceTypeKey))
                        {
                            CurrentEncounter.ServiceTypeKey = serviceType.ServiceTypeKey;
                            if (CurrentEncounter.Task != null)
                            {
                                CurrentEncounter.Task.ServiceTypeKey = serviceType.ServiceTypeKey;
                            }
                        }
                    }
                }
            }

            // Handle special case of M1307 answers interaction
            if ((!response) && (oasisLayout.CMSField == "M1307_OLDST_STG2_AT_DSCHRG") && (oasisAnswer.Sequence == 2))
            {
                ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1307_OLDST_STG2_ONST_DT"), true);
            }

            // Handle special case of M1730 answers interaction
            if ((!response) && (oasisLayout.CMSField == "M1730_STDZ_DPRSN_SCRNG") && (oasisAnswer.Sequence == 2))
            {
                ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1730_PHQ2_LACK_INTRST"), true);
                ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1730_PHQ2_DPRSN"), true);
            }

            if ((response) && ((oasisLayout.CMSField == "M1730_PHQ2_LACK_INTRST") ||
                               (oasisLayout.CMSField == "M1730_PHQ2_DPRSN")))
            {
                OasisAnswer oasisAnswerParentRadio =
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "M1730_STDZ_DPRSN_SCRNG", 2);
                SetRadioResponse(true, oasisAnswerParentRadio);
                NotifyAnswerResponseChanged(oasisAnswerParentRadio);
            }

            OasisAlertCheckMeasuresForQuestion(oasisAnswer.OasisQuestionKey);
        }

        public string GetTextResponse(OasisAnswer oasisAnswer, bool showmsg = true)
        {
            if (!IsOasisActive)
            {
                return null;
            }

            if (oasisAnswer == null)
            {
                return null;
            }

            if (oasisAnswer.CachedOasisLayout == null)
            {
                return null;
            }

            if (((oasisAnswer.IsType(OasisType.Text) || oasisAnswer.IsType(OasisType.TextNAUK) ||
                  oasisAnswer.IsType(OasisType.PressureUlcer) || oasisAnswer.IsType(OasisType.PressureUlcerWorse) ||
                  oasisAnswer.IsType(OasisType.PressureUlcer_C1) || oasisAnswer.IsType(OasisType.WoundDimension) ||
                  oasisAnswer.IsType(OasisType.PressureUlcer_C2) || oasisAnswer.IsType(OasisType.PressureUlcer_C3) ||
                  oasisAnswer.IsType(OasisType.PressureUlcerWorse_C2) ||
                  oasisAnswer.IsType(OasisType.HeightWeight_C2) || oasisAnswer.IsType(OasisType.GG0170C_C2) ||
                  oasisAnswer.IsType(OasisType.TopLegend) || oasisAnswer.IsType(OasisType.LeftLegend) ||
                  oasisAnswer.IsType(OasisType.ServiceUtilization_10) ||
                  oasisAnswer.IsType(OasisType.ServiceUtilization_30)) == false))
            {
                if (showmsg)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisManager.GetTextResponse: {0} is not a Text type.  Contact your system administrator.",
                        oasisAnswer.CachedOasisLayout.CMSField));
                }

                return null;
            }

            string text = (CurrentEncounterOasis.B1Record.Substring(oasisAnswer.CachedOasisLayout.StartPos - 1,
                oasisAnswer.CachedOasisLayout.Length));
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            string trimText = text.Trim();
            if (((oasisAnswer.CachedOasisLayout.CMSField == "M0060_PAT_ZIP") ||
                 (oasisAnswer.CachedOasisLayout.CMSField == "A0550")) && (trimText.Length > 5))
            {
                return trimText.Substring(0, 5) + "-" + trimText.Substring(5, trimText.Length - 5);
            }

            if ((oasisAnswer.IsType(OasisType.HeightWeight_C2) ||
                 oasisAnswer.IsType(OasisType.PressureUlcerWorse_C2)) && (trimText == OASIS_DASH))
            {
                return trimText;
            }

            if (oasisAnswer.IsType(OasisType.GG0170C_C2))
            {
                return text;
            }

            if ((oasisAnswer.CachedOasisLayout.CMSField == "M2200_THER_NEED_NBR") && (trimText == OASIS_EQUAL))
            {
                return null;
            }

            return trimText;
        }

        public void SetTextResponse(string response, OasisAnswer oasisAnswer, bool overrideSubFieldDASHes = true)
        {
            if (CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (oasisAnswer == null)
            {
                return;
            }

            if (oasisAnswer.CachedOasisLayout == null)
            {
                return;
            }

            if (((oasisAnswer.IsType(OasisType.Text) || oasisAnswer.IsType(OasisType.TextNAUK) ||
                  oasisAnswer.IsType(OasisType.PressureUlcer) || oasisAnswer.IsType(OasisType.PressureUlcerWorse) ||
                  oasisAnswer.IsType(OasisType.PressureUlcer_C1) || oasisAnswer.IsType(OasisType.WoundDimension) ||
                  oasisAnswer.IsType(OasisType.PressureUlcer_C2) || oasisAnswer.IsType(OasisType.PressureUlcer_C3) ||
                  oasisAnswer.IsType(OasisType.PressureUlcerWorse_C2) ||
                  oasisAnswer.IsType(OasisType.HeightWeight_C2) || oasisAnswer.IsType(OasisType.GG0170C_C2) ||
                  oasisAnswer.IsType(OasisType.TopLegend) || oasisAnswer.IsType(OasisType.LeftLegend) ||
                  oasisAnswer.IsType(OasisType.ServiceUtilization_10) ||
                  oasisAnswer.IsType(OasisType.ServiceUtilization_30)) == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisManager.SetTextResponse: {0} is not a Text type.  Contact your system administrator.",
                    oasisAnswer.CachedOasisLayout.CMSField));
                return;
            }

            string text = null;
            ClearResponse(oasisAnswer);
            string resp = response;

            if (!string.IsNullOrWhiteSpace(resp))
            {
                text = resp.Trim().ToUpper();
                if ((oasisAnswer.CachedOasisLayout.CMSField == "M0060_PAT_ZIP") ||
                    (oasisAnswer.CachedOasisLayout.CMSField == "A0550"))
                {
                    text = text.Replace("-", "");
                }
                else if (oasisAnswer.IsType(OasisType.WoundDimension))
                {
                    double doubleText = 0;
                    try
                    {
                        doubleText = Double.Parse(text);
                    }
                    catch
                    {
                    }

                    text = string.Format("{0:00.0}", doubleText);
                }
                else if ((oasisAnswer.IsType(OasisType.PressureUlcer)) ||
                         (oasisAnswer.IsType(OasisType.PressureUlcerWorse)) ||
                         (oasisAnswer.IsType(OasisType.PressureUlcer_C1)) ||
                         (oasisAnswer.IsType(OasisType.PressureUlcer_C2)))
                {
                    int intText = 0;
                    try
                    {
                        intText = Int32.Parse(text);
                    }
                    catch
                    {
                    }

                    text = string.Format("{0:00}", intText);
                }
                else if (oasisAnswer.IsType(OasisType.PressureUlcer_C3))
                {
                    if (text == OASIS_DASH)
                    {
                        text = OASIS_DASH + new String(' ', (oasisAnswer.CachedOasisLayout.Length - 1));
                    }
                    else
                    {
                        int intText = 0;
                        try
                        {
                            intText = Int32.Parse(text);
                        }
                        catch
                        {
                        }

                        text = string.Format("{0:00}", intText);
                    }
                }
                else if (oasisAnswer.OasisQuestion.Question == "M2200")
                {
                    if (text != OASIS_EQUAL)
                    {
                        int intText = 0;
                        try
                        {
                            intText = Int32.Parse(text);
                        }
                        catch
                        {
                        }

                        text = string.Format("{0:000}", intText);
                    }
                }
                else if (oasisAnswer.IsType(OasisType.HeightWeight_C2))
                {
                    if (text == OASIS_DASH)
                    {
                        text = OASIS_DASH + new String(' ', (oasisAnswer.CachedOasisLayout.Length - 1));
                    }
                    else
                    {
                        int intText = 0;
                        try
                        {
                            intText = Int32.Parse(text);
                        }
                        catch
                        {
                        }

                        if (oasisAnswer.CachedOasisLayout.Length == 2)
                        {
                            text = (intText == 0) ? OASIS_DASH : string.Format("{0:00}", intText);
                        }
                        else if (oasisAnswer.CachedOasisLayout.Length == 3)
                        {
                            text = (intText == 0) ? OASIS_DASH : string.Format("{0:000}", intText);
                        }
                    }
                }
                else if (oasisAnswer.IsType(OasisType.PressureUlcerWorse_C2))
                {
                    if (text == OASIS_DASH)
                    {
                        text = OASIS_DASH + new String(' ', (oasisAnswer.CachedOasisLayout.Length - 1));
                    }
                    else
                    {
                        int intText = 0;
                        try
                        {
                            intText = Int32.Parse(text);
                        }
                        catch
                        {
                        }

                        if (oasisAnswer.CachedOasisLayout.Length == 2)
                        {
                            text = string.Format("{0:00}", intText);
                        }
                        else if (oasisAnswer.CachedOasisLayout.Length == 3)
                        {
                            text = string.Format("{0:000}", intText);
                        }
                    }
                }

                if (text.Length > oasisAnswer.CachedOasisLayout.Length)
                {
                    text = text.Substring(0, oasisAnswer.CachedOasisLayout.Length);
                }
                else if (text.Length < oasisAnswer.CachedOasisLayout.Length)
                {
                    text = text + new String(' ', (oasisAnswer.CachedOasisLayout.Length - (text.Length)));
                }

                CurrentEncounterOasis.B1Record =
                    CurrentEncounterOasis.B1Record.Remove(oasisAnswer.CachedOasisLayout.StartPos - 1, text.Length);
                CurrentEncounterOasis.B1Record =
                    CurrentEncounterOasis.B1Record.Insert(oasisAnswer.CachedOasisLayout.StartPos - 1, text);
            }

            // Handle CheckBoxExclusive cases
            if ((oasisAnswer.IsType(OasisType.PressureUlcer) == false) &&
                (oasisAnswer.IsType(OasisType.PressureUlcerWorse) == false) &&
                (oasisAnswer.IsType(OasisType.PressureUlcer_C1) == false) &&
                (oasisAnswer.IsType(OasisType.PressureUlcer_C2) == false) &&
                (oasisAnswer.IsType(OasisType.PressureUlcer_C3) == false) &&
                (oasisAnswer.IsType(OasisType.PressureUlcerWorse_C2) == false) &&
                (oasisAnswer.IsType(OasisType.HeightWeight_C2) == false) &&
                (oasisAnswer.IsType(OasisType.GG0170C_C2) == false) &&
                (oasisAnswer.IsType(OasisType.TopLegend) == false) &&
                (oasisAnswer.IsType(OasisType.LeftLegend) == false) &&
                (oasisAnswer.IsType(OasisType.ServiceUtilization_10) == false) &&
                (oasisAnswer.IsType(OasisType.ServiceUtilization_30) == false) &&
                (oasisAnswer.OasisQuestion.Question != "M0040") && (oasisAnswer.OasisQuestion.Question != "A0500"))
            {
                ResetAnswers(oasisAnswer, OasisCache.GetAllAnswersButMe(oasisAnswer));
                if (text == null)
                {
                    OasisAlertCheckMeasuresForQuestion(oasisAnswer.OasisQuestionKey);
                    return;
                }
            }

            // Raise property changed on self to echo any formatting changes (eg, addition of leading zeroes)
            if ((text != null) && (response != text))
            {
                NotifyAnswerResponseChanged(oasisAnswer);
            }

            if ((oasisAnswer.IsType(OasisType.PressureUlcer)) && (!IsWoundDimensionsVisible) &&
                (OasisCache.DoesExistGetOasisAnswerByCMSField(OasisVersionKey, "M1310_PRSR_ULCR_LNGTH")))
            {
                ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1310_PRSR_ULCR_LNGTH"), true,
                    true);
                ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1312_PRSR_ULCR_WDTH"), true, true);
                ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1314_PRSR_ULCR_DEPTH"), true,
                    true);
            }

            if (oasisAnswer.IsType(OasisType.PressureUlcer))
            {
                RaiseIsWoundDimensionsVisibleChanged();
            }

            OasisAlertCheckMeasuresForQuestion(oasisAnswer.OasisQuestionKey);
            // Apply disable logic
            if (oasisAnswer.IsType(OasisType.PressureUlcer_C3))
            {
                ApplySubFieldSkipLogicM1311(oasisAnswer, text);
            }
            else if ((oasisAnswer.IsType(OasisType.LeftLegend)) || (oasisAnswer.IsType(OasisType.TopLegend)))
            {
                ApplySubFieldSkipLogicLegend(oasisAnswer, text, overrideSubFieldDASHes);
            }
        }

        private void ApplySubFieldSkipLogicM1311(OasisAnswer oasisAnswer, string text)
        {
            if ((oasisAnswer == null) || (oasisAnswer.OasisQuestion == null) ||
                (string.IsNullOrWhiteSpace(oasisAnswer.SubQuestionSkipFields)))
            {
                return;
            }

            OasisManagerQuestion omq = OasisManagerQuestions.Where(q => (q.OasisQuestion == oasisAnswer.OasisQuestion))
                .FirstOrDefault();
            if ((omq == null) || (omq.OasisManagerAnswers == null))
            {
                return;
            }

            OasisManagerAnswer omaToSkipAnchor = omq.OasisManagerAnswers
                .Where(o => o.OasisAnswer.CachedOasisLayout.CMSField == oasisAnswer.SubQuestionSkipFields)
                .FirstOrDefault();
            if ((omaToSkipAnchor == null) || (omaToSkipAnchor.OasisAnswer == null))
            {
                return;
            }

            // omaToSkip is the answer to disbale or not
            string t = (string.IsNullOrWhiteSpace(text)) ? null : text.Trim();
            if (t == null)
            {
                omaToSkipAnchor.Enabled = true;
            }
            else if ((t == "0") || (t == "00"))
            {
                SetTextResponseB1("", omaToSkipAnchor.OasisAnswer.CachedOasisLayout, CurrentEncounterOasis);
                omaToSkipAnchor.Enabled = false;
            }
            else if (t == OASIS_DASH) // bfmbfm don't know whether to 'skip' column 2 with a hat '^' or a dash?
            {
                SetTextResponseB1(OASIS_DASH, omaToSkipAnchor.OasisAnswer.CachedOasisLayout, CurrentEncounterOasis);
                omaToSkipAnchor.Enabled = false;
            }
            else
            {
                omaToSkipAnchor.Enabled = true;
            }

            NotifyAnswerResponseChanged(omaToSkipAnchor.OasisAnswer);
        }

        private void ApplySubFieldSkipLogicLegend(OasisAnswer oasisAnswer, string text, bool overrideSubFieldDASHes)
        {
            string trimText = (string.IsNullOrWhiteSpace(text)) ? null : text.Trim();
            if ((oasisAnswer == null) || (oasisAnswer.OasisQuestion == null))
            {
                return;
            }

            OasisManagerQuestion omq = OasisManagerQuestions.Where(q => (q.OasisQuestion == oasisAnswer.OasisQuestion))
                .FirstOrDefault();
            if ((omq == null) || (omq.OasisManagerAnswers == null))
            {
                return;
            }

            // get the specific OasisManagerAnswer for the OasisAnswer and text passed
            OasisManagerAnswer omaME = omq.OasisManagerAnswers.Where(o =>
                ((o.OasisAnswer.CachedOasisLayout.CMSField == oasisAnswer.CachedOasisLayout.CMSField) &&
                 (o.OasisAnswer.AnswerLabel == trimText))).FirstOrDefault();
            if ((omaME == null) || (omaME.OasisAnswer == null))
            {
                return;
            }

            // figure out if we are enabling or disabling the skipfields
            bool enable = ((omaME == null) || (omaME.OasisAnswer == null))
                ? true
                : (string.IsNullOrWhiteSpace(omaME.OasisAnswer.SubQuestionSkipFields));
            string subQuestionSkipFields = ((omaME == null) || (omaME.OasisAnswer == null))
                ? null
                : omaME.OasisAnswer.SubQuestionSkipFields;
            if (enable)
            {
                // assume that the dash field for the subquestion (subquestion is determined by the CMSField) has the superset skip logic - if no dash response- just get first assume that all fields for the subquestion have the same skip logic (or none)
                OasisManagerAnswer oma = omq.OasisManagerAnswers.Where(o =>
                        ((o.OasisAnswer.CachedOasisLayout.CMSField == oasisAnswer.CachedOasisLayout.CMSField) &&
                         (o.OasisAnswer.SubQuestionSkipFields != null) && (o.OasisAnswer.AnswerLabel == OASIS_DASH)))
                    .FirstOrDefault();
                if (oma == null)
                {
                    oma = omq.OasisManagerAnswers.Where(o =>
                        ((o.OasisAnswer.CachedOasisLayout.CMSField == oasisAnswer.CachedOasisLayout.CMSField) &&
                         (o.OasisAnswer.SubQuestionSkipFields != null))).FirstOrDefault();
                }

                subQuestionSkipFields = (oma == null) ? null : oma.OasisAnswer.SubQuestionSkipFields;
            }

            if (subQuestionSkipFields == null)
            {
                return;
            }

            string[] pipe = { "|" };
            string[] skipFieldsArray = subQuestionSkipFields.Split(pipe, StringSplitOptions.RemoveEmptyEntries);
            if (skipFieldsArray.Length == 0)
            {
                return;
            }

            foreach (string skipField in skipFieldsArray)
            {
                OasisManagerAnswer omaToSkipAnchor = omq.OasisManagerAnswers
                    .Where(o => o.OasisAnswer.CachedOasisLayout.CMSField == skipField)
                    .OrderBy(o => o.OasisAnswer.Sequence).FirstOrDefault();
                if ((omaToSkipAnchor == null) || (omaToSkipAnchor.OasisAnswer == null))
                {
                    continue;
                }

                if (enable)
                {
                    string r = GetTextResponse(omaToSkipAnchor.OasisAnswer);
                    if ((r == OASIS_DASH) && overrideSubFieldDASHes)
                    {
                        SetTextResponse(null, omaToSkipAnchor.OasisAnswer, overrideSubFieldDASHes);
                    }

                    omaToSkipAnchor.Enabled = true;
                }
                else
                {
                    string r = null; // Assume propogating null/blank/hat to the skipped questions
                    if ((trimText == OASIS_DASH))
                    {
                        r = OASIS_DASH; // if response passed is a dash - propogate it to the skipped questions
                    }
                    else if (oasisAnswer.SubQuestionColumn == 2)
                    {
                        r = text; // If this is a column 2 skip propagate the response to the skipped questions (ie., 07,09,10,88) overriding the null - leaving the DASH
                    }

                    SetTextResponse(r, omaToSkipAnchor.OasisAnswer, overrideSubFieldDASHes);
                    omaToSkipAnchor.Enabled = false;
                }

                NotifyAnswerResponseChanged(omaToSkipAnchor.OasisAnswer);
            }

            // cascade existing responses again to set/reset nested enabled property
            foreach (string skipField in skipFieldsArray)
            {
                OasisManagerAnswer omaToSkipAnchor = omq.OasisManagerAnswers
                    .Where(o => o.OasisAnswer.CachedOasisLayout.CMSField == skipField)
                    .OrderBy(o => o.OasisAnswer.Sequence).FirstOrDefault();
                if ((omaToSkipAnchor == null) || (omaToSkipAnchor.OasisAnswer == null))
                {
                    continue;
                }

                string r = GetTextResponse(omaToSkipAnchor.OasisAnswer);
                r = (r == null) ? null : r.Trim();
                SetTextResponse(r, omaToSkipAnchor.OasisAnswer, overrideSubFieldDASHes);
                NotifyAnswerResponseChanged(omaToSkipAnchor.OasisAnswer);
            }
        }

        public void SetTextResponseB1(string response, OasisLayout oasisLayout, EncounterOasis encounterOasis)
        {
            if (encounterOasis.IsReadOnly)
            {
                return;
            }

            if (oasisLayout == null)
            {
                return;
            }

            string text = null;
            ClearResponseB1(oasisLayout, encounterOasis);
            if (!string.IsNullOrWhiteSpace(response))
            {
                text = response.Trim().ToUpper();
                if ((oasisLayout.CMSField == "M0060_PAT_ZIP") || (oasisLayout.CMSField == "A0550"))
                {
                    text = text.Replace("-", "");
                }

                if (text.Length > oasisLayout.Length)
                {
                    text = text.Substring(0, oasisLayout.Length);
                }

                if (text.Length > oasisLayout.Length)
                {
                    text = text.Substring(0, oasisLayout.Length);
                }
                else if (text.Length < oasisLayout.Length)
                {
                    text = text + new String(' ', (oasisLayout.Length - (text.Length)));
                }

                encounterOasis.B1Record = encounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, text.Length);
                encounterOasis.B1Record = encounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, text);
            }
        }

        public void ClearResponseB1(OasisLayout oasisLayout, EncounterOasis encounterOasis)
        {
            if (encounterOasis.IsReadOnly)
            {
                return;
            }

            if (oasisLayout == null)
            {
                return;
            }

            encounterOasis.B1Record = encounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, oasisLayout.Length);
            encounterOasis.B1Record =
                encounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, new String(' ', oasisLayout.Length));
        }

        private string GetResponseB1Record(OasisLayout oasisLayout, string b1Record)
        {
            if (oasisLayout == null)
            {
                return null;
            }

            string text = (b1Record.Substring(oasisLayout.StartPos - 1, oasisLayout.Length));
            return (string.IsNullOrWhiteSpace(text)) ? null : text.Trim();
        }

        private string GetResponseB1RecordNoTrim(OasisLayout oasisLayout, string b1Record)
        {
            if (oasisLayout == null)
            {
                return null;
            }

            return (b1Record.Substring(oasisLayout.StartPos - 1, oasisLayout.Length));
        }

        public string GetResponse(OasisLayout oasisLayout)
        {
            return GetResponseB1Record(oasisLayout, CurrentEncounterOasis.B1Record);
        }

        public void SetResponse(string response, OasisLayout oasisLayout, EncounterOasis pEncounterOasis = null)
        {
            EncounterOasis encounterOasis = (pEncounterOasis == null) ? CurrentEncounterOasis : pEncounterOasis;
            if (encounterOasis.IsReadOnly)
            {
                return;
            }

            if (oasisLayout == null)
            {
                return;
            }

            encounterOasis.B1Record = encounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, oasisLayout.Length);
            encounterOasis.B1Record =
                encounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, new String(' ', oasisLayout.Length));
            if ((oasisLayout.CMSField.ToUpper().StartsWith("M0090")) ||
                (oasisLayout.CMSField.ToUpper().StartsWith("Z0500B")))
            {
                encounterOasis.M0090 = null;
            }

            if ((oasisLayout.CMSField.ToUpper().StartsWith("M0150")) ||
                (oasisLayout.CMSField.ToUpper().StartsWith("A1400")))
            {
                encounterOasis.MedicareOrMedicaid = false;
            }

            if (response == null)
            {
                return;
            }

            if (response.Length == 0)
            {
                return;
            }

            string text = response;
            if (text.Length > oasisLayout.Length)
            {
                text = text.Substring(0, oasisLayout.Length);
            }

            encounterOasis.B1Record = encounterOasis.B1Record.Remove(oasisLayout.StartPos - 1, text.Length);
            encounterOasis.B1Record = encounterOasis.B1Record.Insert(oasisLayout.StartPos - 1, text);
            if ((oasisLayout.CMSField.ToUpper().StartsWith("M0090")) ||
                (oasisLayout.CMSField.ToUpper().StartsWith("Z0500B")))
            {
                encounterOasis.M0090 =
                    GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, oasisLayout.CMSField));
            }

            if ((oasisLayout.CMSField.ToUpper().StartsWith("M0150")) ||
                (oasisLayout.CMSField.ToUpper().StartsWith("A1400")))
            {
                encounterOasis.MedicareOrMedicaid = encounterOasis.IsMedicareOrMedicaid;
            }
        }

        private void CopyResponse(string question, OasisLayout oasisLayout, string prevB1Record)
        {
            SetResponse(GetResponseB1RecordNoTrim(oasisLayout, prevB1Record), oasisLayout);
            OasisManagerQuestion omq = (OasisManagerQuestions == null)
                ? null
                : OasisManagerQuestions.Where(q => (q.OasisQuestion.Question == question)).FirstOrDefault();
            if (omq != null)
            {
                OasisAlertCheckMeasuresForQuestion(omq.OasisQuestion.OasisQuestionKey);
            }
        }

        private void CopyResponse(OasisLayout oasisLayout, EncounterOasis prevEncounterOasis,
            EncounterOasis currEncounterOasis)
        {
            SetResponse(GetResponseB1RecordNoTrim(oasisLayout, prevEncounterOasis.B1Record), oasisLayout,
                currEncounterOasis);
        }

        public string GetICDResponse(OasisAnswer oasisAnswer, bool showmsg = true)
        {
            if (!IsOasisActive)
            {
                return null;
            }

            if (oasisAnswer == null)
            {
                return null;
            }

            if (oasisAnswer.CachedOasisLayout == null)
            {
                return null;
            }

            if ((oasisAnswer.IsType(OasisType.ICD) == false) && (oasisAnswer.IsType(OasisType.ICD10) == false))
            {
                if (showmsg)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisManager.GetICDResponse: {0} is not an ICD type.  Contact your system administrator.",
                        oasisAnswer.CachedOasisLayout.CMSField));
                }

                return null;
            }

            string text = CurrentEncounterOasis.B1Record.Substring(oasisAnswer.CachedOasisLayout.StartPos - 1,
                oasisAnswer.CachedOasisLayout.Length).Trim();
            return (string.IsNullOrWhiteSpace(text)) ? null : text;
        }

        public void SetICDResponse(string response, OasisAnswer oasisAnswer)
        {
            if (CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (oasisAnswer == null)
            {
                return;
            }

            if (oasisAnswer.CachedOasisLayout == null)
            {
                return;
            }

            if ((oasisAnswer.IsType(OasisType.ICD) == false) && (oasisAnswer.IsType(OasisType.ICD10) == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisManager.SetICDResponse: {0} is not an ICD type.  Contact your system administrator.",
                    oasisAnswer.CachedOasisLayout.CMSField));
                return;
            }

            ClearResponse(oasisAnswer);
            if (oasisAnswer.IsType(OasisType.ICD))
            {
                SetICD9Response(response, oasisAnswer);
            }
            else
            {
                SetICD10Response(response, oasisAnswer);
            }
        }

        private void SetICD9Response(string response, OasisAnswer oasisAnswer)
        {
            if (string.IsNullOrWhiteSpace(response) && (oasisAnswer.OasisQuestion.Question != "M1020"))
            {
                // fill the hole, if any, left by clearing this ICD
                OasisAnswer prevoa = oasisAnswer;
                List<OasisAnswer> l = OasisCache.GetAllICDAnswersAfterMe(oasisAnswer);
                if (l == null)
                {
                    return;
                }

                foreach (OasisAnswer oa in l)
                {
                    string nextICD = CurrentEncounterOasis.B1Record.Substring(oa.CachedOasisLayout.StartPos - 1,
                        oa.CachedOasisLayout.Length);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Remove(prevoa.CachedOasisLayout.StartPos - 1, nextICD.Length);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(prevoa.CachedOasisLayout.StartPos - 1, nextICD);
                    prevoa = oa;
                }

                ClearResponse(prevoa);
                NotifyAnswerResponseChanged(oasisAnswer);
                foreach (OasisAnswer oa in l) NotifyAnswerResponseChanged(oa);
                return;
            }

            if (string.IsNullOrWhiteSpace(response) && (oasisAnswer.OasisQuestion.Question == "M1020"))
            {
                if (oasisAnswer.CachedOasisLayout.CMSField == "M1020_PRIMARY_DIAG_ICD")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1020_PRIMARY_DIAG_SEVERITY"),
                        true);
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_A3"), true);
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_A4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1022_OTH_DIAG1_ICD")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1022_OTH_DIAG1_SEVERITY"),
                        true);
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_B3"), true);
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_B4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1022_OTH_DIAG2_ICD")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1022_OTH_DIAG2_SEVERITY"),
                        true);
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_C3"), true);
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_C4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1022_OTH_DIAG3_ICD")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1022_OTH_DIAG3_SEVERITY"),
                        true);
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_D3"), true);
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_D4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1022_OTH_DIAG4_ICD")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1022_OTH_DIAG4_SEVERITY"),
                        true);
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_E3"), true);
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_E4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1022_OTH_DIAG5_ICD")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1022_OTH_DIAG5_SEVERITY"),
                        true);
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_F3"), true);
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_F4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1024_PMT_DIAG_ICD_A3")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_A4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1024_PMT_DIAG_ICD_B3")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_B4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1024_PMT_DIAG_ICD_C3")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_C4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1024_PMT_DIAG_ICD_D3")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_D4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1024_PMT_DIAG_ICD_E3")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_E4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1024_PMT_DIAG_ICD_F3")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1024_PMT_DIAG_ICD_F4"), true);
                }

                ClearResponse(oasisAnswer);
                NotifyAnswerResponseChanged(oasisAnswer);
                return;
            }

            string text = response.Trim();
            string[] parts = text.Split('.');
            // B1 ICD formats: '  99.9 ', '  99.99', ' 999.  ', ' 999.9 ', ' 999.99', ' V99.  ', ' V99.9 ', ' V99.99', 'E999.  ', 'E999.9 ', 'E999.99', 
            if ((text.StartsWith("E") && ((text.Length > 7) || (parts[0].Length != 4))) ||
                ((text.StartsWith("E") == false) &&
                 ((text.Length > 6) || ((parts[0].Length != 2) && (parts[0].Length != 3)))) ||
                (text.Contains(".") == false))
            {
                MessageBox.Show(String.Format(
                    "Error OasisManager.SetICDResponse: {0} is not a valid ICD9 code for OASIS.  Contact your system administrator.",
                    text));
                return;
            }

            if (text.StartsWith("E") == false)
            {
                text = " " + text;
            }

            if (parts[0].Length == 2)
            {
                text = " " + text;
            }

            CurrentEncounterOasis.B1Record =
                CurrentEncounterOasis.B1Record.Remove(oasisAnswer.CachedOasisLayout.StartPos - 1, text.Length);
            CurrentEncounterOasis.B1Record =
                CurrentEncounterOasis.B1Record.Insert(oasisAnswer.CachedOasisLayout.StartPos - 1, text);
            // Handle severity disabled for E/V-code
            if (oasisAnswer.OasisQuestion.Question == "M1020")
            {
                if ((oasisAnswer.CachedOasisLayout.CMSField == "M1020_PRIMARY_DIAG_ICD") && text.Contains("V"))
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1020_PRIMARY_DIAG_SEVERITY"),
                        true);
                }
                else if ((oasisAnswer.CachedOasisLayout.CMSField == "M1022_OTH_DIAG1_ICD") &&
                         (text.Contains("E") || text.Contains("V")))
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1022_OTH_DIAG1_SEVERITY"),
                        true);
                }
                else if ((oasisAnswer.CachedOasisLayout.CMSField == "M1022_OTH_DIAG2_ICD") &&
                         (text.Contains("E") || text.Contains("V")))
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1022_OTH_DIAG2_SEVERITY"),
                        true);
                }
                else if ((oasisAnswer.CachedOasisLayout.CMSField == "M1022_OTH_DIAG3_ICD") &&
                         (text.Contains("E") || text.Contains("V")))
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1022_OTH_DIAG3_SEVERITY"),
                        true);
                }
                else if ((oasisAnswer.CachedOasisLayout.CMSField == "M1022_OTH_DIAG4_ICD") &&
                         (text.Contains("E") || text.Contains("V")))
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1022_OTH_DIAG4_SEVERITY"),
                        true);
                }
                else if ((oasisAnswer.CachedOasisLayout.CMSField == "M1022_OTH_DIAG5_ICD") &&
                         (text.Contains("E") || text.Contains("V")))
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1022_OTH_DIAG5_SEVERITY"),
                        true);
                }
            }

            // Handle CheckBoxExclusive cases
            ResetAnswers(oasisAnswer, OasisCache.GetAllExclusiveAnswersButMe(oasisAnswer));
            NotifyAnswerResponseChanged(oasisAnswer);
        }

        private void SetICD10Response(string response, OasisAnswer oasisAnswer)
        {
            if (string.IsNullOrWhiteSpace(response) && (oasisAnswer.OasisQuestion.Question != "M1021"))
            {
                // fill the hole, if any, left by clearing this ICD
                OasisAnswer prevoa = oasisAnswer;
                List<OasisAnswer> l = OasisCache.GetAllICDAnswersAfterMe(oasisAnswer);
                if (l == null)
                {
                    return;
                }

                foreach (OasisAnswer oa in l)
                {
                    string nextICD = CurrentEncounterOasis.B1Record.Substring(oa.CachedOasisLayout.StartPos - 1,
                        oa.CachedOasisLayout.Length);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Remove(prevoa.CachedOasisLayout.StartPos - 1, nextICD.Length);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(prevoa.CachedOasisLayout.StartPos - 1, nextICD);
                    prevoa = oa;
                }

                ClearResponse(prevoa);
                NotifyAnswerResponseChanged(oasisAnswer);
                foreach (OasisAnswer oa in l) NotifyAnswerResponseChanged(oa);
                return;
            }

            if (string.IsNullOrWhiteSpace(response) && (oasisAnswer.OasisQuestion.Question == "M1021"))
            {
                if (oasisAnswer.CachedOasisLayout.CMSField == "M1021_PRIMARY_DIAG_ICD")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1021_PRIMARY_DIAG_SEVERITY"),
                        true);
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_A3"))
                    {
                        ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_A3"),
                            true);
                    }

                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_A4"))
                    {
                        ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_A4"),
                            true);
                    }
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1023_OTH_DIAG1_ICD")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1023_OTH_DIAG1_SEVERITY"),
                        true);
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_B3"))
                    {
                        ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_B3"),
                            true);
                    }

                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_B4"))
                    {
                        ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_B4"),
                            true);
                    }
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1023_OTH_DIAG2_ICD")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1023_OTH_DIAG2_SEVERITY"),
                        true);
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_C3"))
                    {
                        ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_C3"),
                            true);
                    }

                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_C4"))
                    {
                        ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_C4"),
                            true);
                    }
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1023_OTH_DIAG3_ICD")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1023_OTH_DIAG3_SEVERITY"),
                        true);
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_D3"))
                    {
                        ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_D3"),
                            true);
                    }

                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_D4"))
                    {
                        ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_D4"),
                            true);
                    }
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1023_OTH_DIAG4_ICD")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1023_OTH_DIAG4_SEVERITY"),
                        true);
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_E3"))
                    {
                        ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_E3"),
                            true);
                    }

                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_E4"))
                    {
                        ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_E4"),
                            true);
                    }
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1023_OTH_DIAG5_ICD")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1023_OTH_DIAG5_SEVERITY"),
                        true);
                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_F3"))
                    {
                        ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_F3"),
                            true);
                    }

                    if (OasisCache.DoesExistGetOasisLayoutByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_F4"))
                    {
                        ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_F4"),
                            true);
                    }
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1025_OPT_DIAG_ICD_A3")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_A4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1025_OPT_DIAG_ICD_B3")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_B4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1025_OPT_DIAG_ICD_C3")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_C4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1025_OPT_DIAG_ICD_D3")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_D4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1025_OPT_DIAG_ICD_E3")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_E4"), true);
                }
                else if (oasisAnswer.CachedOasisLayout.CMSField == "M1025_OPT_DIAG_ICD_F3")
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1025_OPT_DIAG_ICD_F4"), true);
                }

                ClearResponse(oasisAnswer);
                NotifyAnswerResponseChanged(oasisAnswer);
                return;
            }

            string text = response.Trim();
            string[] parts = text.Split('.');
            // B1 ICD formats: '999.    ','999.9   ','999.99  ','999.999 ','999.9999' 
            if ((text.Contains(".") == false) || (parts[0].Length != 3) || (parts[1].Length > 4))
            {
                MessageBox.Show(String.Format(
                    "Error OasisManager.SetICDResponse: {0} is not a valid ICD10 code for OASIS.  Contact your system administrator.",
                    text));
                return;
            }

            CurrentEncounterOasis.B1Record =
                CurrentEncounterOasis.B1Record.Remove(oasisAnswer.CachedOasisLayout.StartPos - 1, text.Length);
            CurrentEncounterOasis.B1Record =
                CurrentEncounterOasis.B1Record.Insert(oasisAnswer.CachedOasisLayout.StartPos - 1, text);
            // Handle severity disabled for VWXYZ-code
            if (oasisAnswer.OasisQuestion.Question == "M1021")
            {
                if ((oasisAnswer.CachedOasisLayout.CMSField == "M1021_PRIMARY_DIAG_ICD") && ICD10StartsWithVWXYZ(text))
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1021_PRIMARY_DIAG_SEVERITY"),
                        true);
                }
                else if ((oasisAnswer.CachedOasisLayout.CMSField == "M1023_OTH_DIAG1_ICD") &&
                         ICD10StartsWithVWXYZ(text))
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1023_OTH_DIAG1_SEVERITY"),
                        true);
                }
                else if ((oasisAnswer.CachedOasisLayout.CMSField == "M1023_OTH_DIAG2_ICD") &&
                         ICD10StartsWithVWXYZ(text))
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1023_OTH_DIAG2_SEVERITY"),
                        true);
                }
                else if ((oasisAnswer.CachedOasisLayout.CMSField == "M1023_OTH_DIAG3_ICD") &&
                         ICD10StartsWithVWXYZ(text))
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1023_OTH_DIAG3_SEVERITY"),
                        true);
                }
                else if ((oasisAnswer.CachedOasisLayout.CMSField == "M1023_OTH_DIAG4_ICD") &&
                         ICD10StartsWithVWXYZ(text))
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1023_OTH_DIAG4_SEVERITY"),
                        true);
                }
                else if ((oasisAnswer.CachedOasisLayout.CMSField == "M1023_OTH_DIAG5_ICD") &&
                         ICD10StartsWithVWXYZ(text))
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M1023_OTH_DIAG5_SEVERITY"),
                        true);
                }
            }

            // Handle CheckBoxExclusive cases
            ResetAnswers(oasisAnswer, OasisCache.GetAllExclusiveAnswersButMe(oasisAnswer));
            NotifyAnswerResponseChanged(oasisAnswer);
        }

        private bool ICD10StartsWithVWXYZ(string icd)
        {
            if (string.IsNullOrWhiteSpace(icd))
            {
                return false;
            }

            if (icd.ToUpper().StartsWith("V") || icd.ToUpper().StartsWith("W") || icd.ToUpper().StartsWith("X") ||
                icd.ToUpper().StartsWith("Y") || icd.ToUpper().StartsWith("Z"))
            {
                return true;
            }

            return false;
        }

        private void ResetAllAnswers(OasisAnswer oasisAnswer)
        {
            List<OasisAnswer> l = new List<OasisAnswer>();
            l.Add(oasisAnswer);
            ResetAnswers(oasisAnswer, l);
            ResetAnswers(oasisAnswer, OasisCache.GetAllAnswersButMe(oasisAnswer));
        }

        private void ResetAnswers(OasisAnswer me, List<OasisAnswer> l)
        {
            if (CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (l == null)
            {
                return;
            }

            foreach (OasisAnswer oa in l)
            {
                if ((me.CachedOasisLayout.CMSField != "GG0110DASH") &&
                    (oa.CachedOasisLayout.CMSField != "GG0110DASH") &&
                    (oa.CachedOasisLayout.CMSField != "M2200_THER_NEED_IGNORE_EQUAL"))
                {
                    // GG0110DASH was skimmed ff in main SetCheckBoxResponse code
                    int? startGoTo = GetGotoSequence(oa);
                    ClearResponse(oa);
                    if (oa.IsType(OasisType.CheckBox) || oa.IsType(OasisType.CheckBoxExclusive))
                    {
                        CurrentEncounterOasis.B1Record =
                            CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                        CurrentEncounterOasis.B1Record =
                            CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1, OASIS_ZERO);
                    }

                    if (oa.IsType(OasisType.CheckBoxExclusive) || oa.IsType(OasisType.DateNAUK))
                    {
                        CheckForGoto(oa, startGoTo, GetGotoSequence(oa));
                    }
                }

                NotifyAnswerResponseChanged(oa);
            }
        }

        private void ResetAnswersFromDash(OasisAnswer me, List<OasisAnswer> l)
        {
            if (CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (l == null)
            {
                return;
            }

            foreach (OasisAnswer oa in l)
            {
                string r = GetResponse(oa.CachedOasisLayout);
                if ((me.CachedOasisLayout.CMSField != "GG0110DASH") &&
                    (oa.CachedOasisLayout.CMSField != "GG0110DASH") && (r == OASIS_DASH))
                {
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1, OASIS_ZERO);
                    NotifyAnswerResponseChanged(oa);
                }
            }
        }

        private void SetAnswersNAUK(List<OasisAnswer> l)
        {
            if (CurrentEncounterOasis.IsReadOnly)
            {
                return;
            }

            if (l == null)
            {
                return;
            }

            foreach (OasisAnswer oa in l)
            {
                int? startGoTo = GetGotoSequence(oa);
                ClearResponse(oa);
                if (oa.IsType(OasisType.CheckBox) || oa.IsType(OasisType.CheckBoxExclusive))
                {
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Remove(oa.CachedOasisLayout.StartPos - 1, 1);
                    CurrentEncounterOasis.B1Record =
                        CurrentEncounterOasis.B1Record.Insert(oa.CachedOasisLayout.StartPos - 1, "1");
                }

                if (oa.IsType(OasisType.CheckBoxExclusive) || oa.IsType(OasisType.DateNAUK))
                {
                    CheckForGoto(oa, startGoTo, GetGotoSequence(oa));
                }

                NotifyAnswerResponseChanged(oa);
            }
        }

        public void RaiseIsWoundDimensionsVisibleChanged()
        {
            RaisePropertyChanged("IsWoundDimensionsVisible");
        }

        public void ApplyJ1900DashSkipLogic(OasisQuestion oasisQuestionJ1900, bool hideJ1900)
        {
            // If J1800 was answered with a dash- make all J1900 responses dashs as well
            // If we are unhiding J1900 and it is currently a dash - clear it
            OasisQuestion oasisQuestionJ1800 = GetQuestionInSurvey("J1800");
            OasisAnswer oaJ1800 = oasisQuestionJ1800?.OasisAnswer?.FirstOrDefault();
            if (oaJ1800 == null)
            {
                return;
            }

            bool isJ1800DASH = (GetResponse(oaJ1800.CachedOasisLayout) == OASIS_DASH);
            foreach (OasisAnswer oa in oasisQuestionJ1900.OasisAnswer)
                if (hideJ1900 && (isJ1800DASH))
                {
                    SetResponse(OASIS_DASH, oa.CachedOasisLayout);
                }
                else if ((hideJ1900 == false) && (GetResponse(oa.CachedOasisLayout) == OASIS_DASH))
                {
                    ClearResponse(oa, true);
                }
        }

        public bool IsWoundDimensionsVisible
        {
            get
            {
                // rules  a) If M1308_NBR_PRSULC_STG3, M1308_NBR_PRSULC_STG4, or M1308_NSTG_CVRG are greater than 0 (zero), 
                //           then M1310_PRSR_ULCR_LNGTH, M1312_PRSR_ULCR_WDTH, and M1314_PRSR_ULCR_DEPTH cannot be blank.
                //        b) If M1308_NBR_PRSULC_STG3, M1308_NBR_PRSULC_STG4 and M1308_NSTG_CVRG are equal to 0 (zero), 
                //           then M1310_PRSR_ULCR_LNGTH, M1312_PRSR_ULCR_WDTH, and M1314_PRSR_ULCR_DEPTH must all be skipped (blank).

                if (!IsOasisActive)
                {
                    return false;
                }

                if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
                {
                    return false;
                }

                if (((RFA == "01") || (RFA == "03") || (RFA == "09")) == false)
                {
                    return false;
                }

                int count = 0;
                OasisLayout ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NBR_PRSULC_STG3");
                string text = CurrentEncounterOasis.B1Record.Substring(ol.StartPos - 1, ol.Length);
                try
                {
                    count = count + Int32.Parse(text);
                }
                catch
                {
                }

                ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NBR_PRSULC_STG4");
                text = CurrentEncounterOasis.B1Record.Substring(ol.StartPos - 1, ol.Length);
                try
                {
                    count = count + Int32.Parse(text);
                }
                catch
                {
                }

                ol = OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "M1308_NSTG_CVRG");
                text = CurrentEncounterOasis.B1Record.Substring(ol.StartPos - 1, ol.Length);
                try
                {
                    count = count + Int32.Parse(text);
                }
                catch
                {
                }

                return (count == 0) ? false : true;
            }
        }

        private DateTime? _m0090Date;

        private EntityCollection<AdmissionDiagnosis> CurrentAdmissionDiagnosis { get; set; }
        private CollectionViewSource _CurrentFilteredAdmissionDiagnosis = new CollectionViewSource();
        public ICollectionView CurrentFilteredAdmissionDiagnosis => _CurrentFilteredAdmissionDiagnosis.View;

        private void ProcessFilteredAdmissionDiagnosisItems(EntityCollection<AdmissionDiagnosis> pd, DateTime? date)
        {
            _m0090Date = (date != null) ? ((DateTime)date).Date : DateTime.Today.Date;
            if (pd == CurrentAdmissionDiagnosis)
            {
                if (CurrentFilteredAdmissionDiagnosis != null)
                {
                    CurrentFilteredAdmissionDiagnosis.Refresh();
                    return;
                }
            }

            CurrentAdmissionDiagnosis = pd;
            if (CurrentAdmissionDiagnosis == null)
            {
                return;
            }

            _CurrentFilteredAdmissionDiagnosis.Source = CurrentAdmissionDiagnosis;
            CurrentFilteredAdmissionDiagnosis.SortDescriptions.Add(new SortDescription("DiagnosisStatus",
                ListSortDirection.Ascending));
            CurrentFilteredAdmissionDiagnosis.SortDescriptions.Add(new SortDescription("Sequence",
                ListSortDirection.Ascending));
            CurrentFilteredAdmissionDiagnosis.Filter = FilterItems;
            CurrentFilteredAdmissionDiagnosis.Refresh();
        }

        private EntityCollection<PatientMedication> CurrentPatientMedication { get; set; }
        private CollectionViewSource _CurrentFilteredPatientMedication = new CollectionViewSource();
        public ICollectionView CurrentFilteredPatientMedication => _CurrentFilteredPatientMedication.View;

        public ServiceType GetOASISServiceType(string M0080)
        {
            ServiceType serviceType = null;
            if (string.IsNullOrWhiteSpace(M0080))
            {
                return null;
            }

            string stCode = null;
            if (M0080 == "01")
            {
                stCode = "SN OASIS";
            }
            else if (M0080 == "02")
            {
                stCode = "PT OASIS";
            }
            else if (M0080 == "03")
            {
                stCode = "SLP OASIS";
            }
            else if (M0080 == "04")
            {
                stCode = "OT OASIS";
            }

            serviceType = (stCode == null)
                ? null
                : ServiceTypeCache.GetActiveServiceTypes().Where(st => st.Code == stCode && st.IsOasis)
                    .FirstOrDefault();
            if (serviceType == null)
            {
                MessageBox.Show(stCode + " service type does not exist, contact AlayaCare support");
                return null;
            }

            return serviceType;
        }

        public void ProcessFilteredPatientMedicationItems()
        {
            if (CurrentPatient == null)
            {
                return;
            }

            ProcessFilteredPatientMedicationItems(CurrentPatient.PatientMedication);
        }

        private void ProcessFilteredPatientMedicationItems(EntityCollection<PatientMedication> pm)
        {
            _m0090Date =
                GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"));
            if (_m0090Date == null)
            {
                _m0090Date = DateTime.Today.Date;
            }

            if (pm == CurrentPatientMedication)
            {
                if (CurrentFilteredPatientMedication != null)
                {
                    CurrentFilteredPatientMedication.Refresh();
                    return;
                }
            }

            CurrentPatientMedication = pm;
            if (CurrentPatientMedication == null)
            {
                return;
            }

            _CurrentFilteredPatientMedication.Source = CurrentPatientMedication;
            CurrentFilteredPatientMedication.SortDescriptions.Add(new SortDescription("MedicationStatus",
                ListSortDirection.Ascending));
            CurrentFilteredPatientMedication.SortDescriptions.Add(new SortDescription("MedicationName",
                ListSortDirection.Ascending));
            CurrentFilteredPatientMedication.Refresh();
            CurrentFilteredPatientMedication.Filter = FilterItems;
        }

        private EntityCollection<AdmissionPainLocation> CurrentAdmissionPainLocation { get; set; }
        private CollectionViewSource _CurrentFilteredAdmissionPainLocation = new CollectionViewSource();
        public ICollectionView CurrentFilteredAdmissionPainLocation => _CurrentFilteredAdmissionPainLocation.View;

        private void ProcessFilteredAdmissionPainLocationItems(EntityCollection<AdmissionPainLocation> apl)
        {
            _m0090Date =
                GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"));
            if (_m0090Date == null)
            {
                _m0090Date = DateTime.Today.Date;
            }

            if (apl == CurrentAdmissionPainLocation)
            {
                if (CurrentFilteredAdmissionPainLocation != null)
                {
                    CurrentFilteredAdmissionPainLocation.Refresh();
                    return;
                }
            }

            CurrentAdmissionPainLocation = apl;
            if (CurrentAdmissionPainLocation == null)
            {
                return;
            }

            _CurrentFilteredAdmissionPainLocation.Source = CurrentAdmissionPainLocation;
            CurrentFilteredAdmissionPainLocation.SortDescriptions.Add(new SortDescription("PainSite",
                ListSortDirection.Ascending));
            CurrentFilteredAdmissionPainLocation.Refresh();
            CurrentFilteredAdmissionPainLocation.Filter = FilterItems;
        }

        private EntityCollection<AdmissionWoundSite> CurrentAdmissionWoundSite { get; set; }
        private CollectionViewSource _CurrentFilteredAdmissionWoundSite = new CollectionViewSource();
        public ICollectionView CurrentFilteredAdmissionWoundSite => _CurrentFilteredAdmissionWoundSite.View;

        private void ProcessFilteredAdmissionWoundSiteItems(EntityCollection<AdmissionWoundSite> aws)
        {
            _m0090Date =
                GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "M0090_INFO_COMPLETED_DT"));
            if (_m0090Date == null)
            {
                _m0090Date = DateTime.Today.Date;
            }

            if (aws == CurrentAdmissionWoundSite)
            {
                if (CurrentFilteredAdmissionWoundSite != null)
                {
                    CurrentFilteredAdmissionWoundSite.Refresh();
                    return;
                }
            }

            CurrentAdmissionWoundSite = aws;
            if (CurrentAdmissionWoundSite == null)
            {
                return;
            }

            _CurrentFilteredAdmissionWoundSite.Source = CurrentAdmissionWoundSite;
            CurrentFilteredAdmissionWoundSite.SortDescriptions.Add(new SortDescription("Number",
                ListSortDirection.Ascending));
            CurrentFilteredAdmissionWoundSite.Refresh();
            CurrentFilteredAdmissionWoundSite.Filter = FilterItems;
        }

        private bool FilterItems(object item)
        {
            var properties = item.GetType().GetProperties();

            VirtuosoEntity v = item as VirtuosoEntity;
            // If we have an Encounter and the item is not new, only include the item if it is in this encounter
            if ((CurrentEncounter != null) && (!v.IsNew))
            {
                AdmissionDiagnosis pd = item as AdmissionDiagnosis;
                if (pd != null)
                {
                    EncounterDiagnosis ed = CurrentEncounter.EncounterDiagnosis
                        .Where(p => p.AdmissionDiagnosis.AdmissionDiagnosisKey == pd.AdmissionDiagnosisKey)
                        .FirstOrDefault();
                    if (ed == null)
                    {
                        return false;
                    }
                }

                PatientMedication pm = item as PatientMedication;
                if (pm != null)
                {
                    EncounterMedication em = CurrentEncounter.EncounterMedication
                        .Where(p => p.PatientMedication.PatientMedicationKey == pm.PatientMedicationKey)
                        .FirstOrDefault();
                    if (em == null)
                    {
                        return false;
                    }
                }

                AdmissionPainLocation ap = item as AdmissionPainLocation;
                if (ap != null)
                {
                    EncounterPainLocation ep = CurrentEncounter.EncounterPainLocation.Where(p =>
                            p.AdmissionPainLocation.AdmissionPainLocationKey == ap.AdmissionPainLocationKey)
                        .FirstOrDefault();
                    if (ep == null)
                    {
                        return false;
                    }
                }

                AdmissionWoundSite aw = item as AdmissionWoundSite;
                if (aw != null)
                {
                    EncounterWoundSite ew = CurrentEncounter.EncounterWoundSite
                        .Where(p => p.AdmissionWoundSite.AdmissionWoundSiteKey == aw.AdmissionWoundSiteKey)
                        .FirstOrDefault();
                    if (ew == null)
                    {
                        return false;
                    }
                }
            }

            // exclude Healed wounds and wounds of type other
            AdmissionWoundSite aw2 = item as AdmissionWoundSite;
            if (aw2 != null)
            {
                if (((aw2.HealedDate == null) || (aw2.HealedDate == null)) == false)
                {
                    return false;
                }
            }

            // exclude removed diagnosis, and surgicals
            AdmissionDiagnosis ad2 = item as AdmissionDiagnosis;
            if (ad2 != null)
            {
                if (ad2.RemovedDate != null)
                {
                    return false;
                }

                if (ad2.Diagnosis == false)
                {
                    return false;
                }
            }

            // exclude ICDs by versuib unless HIS
            int icdVersion = (OasisVersionUsingICD10) ? 10 : 9;
            if ((ad2 != null) && ((CurrentEncounterOasis == null) ||
                                  ((CurrentEncounterOasis != null) &&
                                   (CurrentEncounterOasis.SYS_CDIsHospice == false))))
            {
                if (ad2.Version != icdVersion)
                {
                    return false;
                }
            }

            if (_m0090Date == null)
            {
                _m0090Date = DateTime.Today.Date;
            }

            var prop = properties.Where(p => p.Name.Equals("EffectiveFrom", StringComparison.OrdinalIgnoreCase) ||
                                             p.Name.EndsWith("StartDate", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (prop != null)
            {
                var value = item.GetType().GetProperty(prop.Name).GetValue(item, null);
                if (value != null && ((DateTime)value).Date > _m0090Date)
                {
                    return false;
                }
            }

            prop = properties.Where(p => p.Name.Equals("EffectiveThru", StringComparison.OrdinalIgnoreCase) ||
                                         p.Name.EndsWith("EndDate", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (prop != null)
            {
                var value = item.GetType().GetProperty(prop.Name).GetValue(item, null);
                if (value != null && ((DateTime)value).Date < _m0090Date)
                {
                    return false;
                }
            }

            return true;
        }

        private OasisAlertManager CurrentOasisAlertManager;

        public void OasisAlertCheckAllMeasures()
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (CurrentOasisAlertManager == null)
            {
                CurrentOasisAlertManager = OasisAlertManager.Create(OasisManagerGuid, CurrentEncounter, FormModel);
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            if (CurrentOasisAlertManager != null)
            {
                CurrentOasisAlertManager.OasisAlertCheckAllMeasures();
            }
        }

        private void OasisAlertCheckMeasuresForQuestion(int OasisQuestionKey)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice))
            {
                return;
            }

            if (CurrentOasisAlertManager == null)
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            if (!MappingAllowedClinician)
            {
                return;
            }

            if (CurrentOasisAlertManager != null)
            {
                CurrentOasisAlertManager.OasisAlertCheckMeasuresForQuestion(OasisQuestionKey);
            }
        }

        public void OasisAlertCheckBypass()
        {
            if (!IsOasisActive)
            {
                return;
            }

            if (CurrentOasisAlertManager != null)
            {
                CurrentOasisAlertManager.OasisAlertCheckBypass();
            }
        }

        private void SetupDefaultsHIS(bool AddingNewEncounter, AdmissionDiscipline currentAdmissionDiscipline)
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice == false))
            {
                return;
            }

            Messenger.Default.Send(((CurrentEncounterOasis.BypassFlag == true) ? true : false),
                string.Format("OasisBypassFlagChanged{0}",
                    OasisManagerGuid.ToString()
                        .Trim())); // incase we ever support HIS bypass - like if we add it to team meeting

            DateTime now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            if (!MappingAllowedClinicianOrOasisCoordinatorReEditBypassOASISAssist)
            {
                // Z0500A/B - Signature/Date verifying record completion - override to the reviewer if in OasisReview 
                if ((PreviousEncounterStatus == (int)EncounterStatusType.OASISReview) &&
                    RoleAccessHelper.CheckPermission(RoleAccess.HISCoordinator, false))
                {
                    CurrentEncounterOasis.Z0500A = WebContext.Current.User.MemberID;
                    SetDateResponse(now.Date, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "Z0500B"), false);
                }

                return;
            }

            if (CurrentPatient == null)
            {
                return;
            }

            if (CurrentAdmission == null)
            {
                return;
            }

            InitializeB1RecordHIS();
            SetupAdmissionOasisHeaderDefaultsHIS();
            // A0205 - Site of Service at Admission - the OasisAnswer sequence is 1-to-1 with the SiteOfService codelookup code. e.g., answer 01 = code '01'
            // only default initially - if A0205 is subsequently changed in the survey - honor that change
            if ((GetQuestionInSurveyNotHidden("A0205") != null) && (AddingNewEncounter == true))
            {
                AdmissionSiteOfService asos = CurrentAdmission.GetAdmissionSiteOfService(((CurrentAdmission.SOCDate == null) ? now : CurrentAdmission.SOCDate));
                if (asos != null)
                {
                    int sequence = -1;
                    if (int.TryParse(asos.SiteOfServiceCode, out sequence))
                    {
                        if (sequence > 0)
                        {
                            SetRadioResponse(true,
                                OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A0205", sequence));
                        }
                    }
                }
            }

            // A0220 - Admission date
            if (CurrentAdmission.SOCDate != null)
            {
                SetDateResponse(CurrentAdmission.SOCDate,
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0220"));
            }

            // A0270 - Discharge date
            if (GetQuestionInSurveyNotHidden("A0270") != null)
            {
                if (CurrentAdmission.DischargeDateTime != null)
                {
                    SetDateResponse(CurrentAdmission.DischargeDateTime,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0270"));
                }
            }

            // A0500 - legal name of patient
            SetTextResponse(CurrentPatient.FirstName, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0500A"));
            SetTextResponse(CurrentPatient.MiddleName, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0500B"));
            SetTextResponse(CurrentPatient.LastName, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0500C"));
            SetTextResponse(CurrentPatient.Suffix, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0500D"));

            // A0550 - Patient ZIP Code
            if (GetQuestionInSurveyNotHidden("A0550") != null)
            {
                PatientAddress pa = CurrentPatient.MainAddress(null);
                if (pa == null)
                {
                    ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0550"), true, true);
                }
                else
                {
                    SetTextResponse(pa.ZipCode, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0550"));
                }
            }

            // A0600A - SSN
            SetTextResponse(CurrentPatient.SSN, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0600A"));

            // A0600B - Medicare number
            // A0700  - Medicaid number
            _m0090Date = GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "Z0500B"));
            if (_m0090Date == null)
            {
                _m0090Date = DateTime.Today.Date;
            }

            if ((CurrentPatient.PatientInsurance != null) && (AddingNewEncounter))
            {
                List<PatientInsurance> list = CurrentPatient.PatientInsurance
                    .Where(i => ((i.Inactive == false) && (i.HistoryKey == null) && (i.PatientInsuranceKey != 0) &&
                                 ((i.EffectiveFromDate.HasValue == false ||
                                   ((DateTime)i.EffectiveFromDate).Date <= _m0090Date) &&
                                  (i.EffectiveThruDate.HasValue == false ||
                                   ((DateTime)i.EffectiveThruDate).Date > _m0090Date))))
                    .OrderByDescending(i => i.InsuranceTypeKey).ToList();
                if ((list != null) && list.Any())
                {
                    bool foundMedicare = false;
                    bool foundMedicaid = false;
                    foreach (PatientInsurance pi in list)
                        if (((pi.InsuranceTypeCode == "1") || (pi.InsuranceTypeCode == "12")) &&
                            (foundMedicare == false) && (string.IsNullOrWhiteSpace(pi.InsuranceNumber) == false))
                        {
                            // A0600B - Medicare number
                            foundMedicare = true;
                            SetTextResponse(pi.InsuranceNumber,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0600B"));
                        }
                        else if (((pi.InsuranceTypeCode == "3") || (pi.InsuranceTypeCode == "13")) &&
                                 (foundMedicaid == false) && (string.IsNullOrWhiteSpace(pi.InsuranceNumber) == false))
                        {
                            // A0700 - Medicaid number
                            foundMedicaid = true;
                            SetTextResponse(pi.InsuranceNumber,
                                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0700"));
                        }
                }
            }

            // A0800 - Gender
            SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A0800", 1));
            if (CurrentPatient.GenderCode != null)
            {
                if (CurrentPatient.GenderCode.ToLower() == "f")
                {
                    SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A0800", 2));
                }
            }

            // A0900 - Birth date
            SetDateResponse(CurrentPatient.BirthDate, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0900"));
            // A1000 - Race/ethnicity
            if (GetQuestionInSurveyNotHidden("A1000") != null)
            {
                string races = (string.IsNullOrWhiteSpace(CurrentPatient.Races))
                    ? null
                    : CurrentPatient.Races.ToLower();
                if (races != null)
                {
                    if (((races.Contains("american indian")) || (races.Contains("alaska"))) ||
                        ((races.Contains("asia")) && (!races.Contains("caucasia"))) ||
                        ((races.Contains("black")) || (races.Contains("africa"))) ||
                        ((races.Contains("hispanic")) || (races.Contains("latin"))) ||
                        ((races.Contains("hawaii")) || (races.Contains("pacific"))) ||
                        ((races.Contains("white")) || (races.Contains("caucasia"))))
                    {
                        ResetAllAnswers(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1000A"));
                        if ((races.Contains("american indian")) || (races.Contains("alaska")))
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1000A"));
                        }

                        if ((races.Contains("asia")) && (!races.Contains("caucasia")))
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1000B"));
                        }

                        if ((races.Contains("black")) || (races.Contains("africa")))
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1000C"));
                        }

                        if ((races.Contains("hispanic")) || (races.Contains("latin")))
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1000D"));
                        }

                        if ((races.Contains("hawaii")) || (races.Contains("pacific")))
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1000E"));
                        }

                        if ((races.Contains("white")) || (races.Contains("caucasia")))
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1000F"));
                        }
                    }
                }
            }

            // A1400 - Payor Information
            if ((GetQuestionInSurveyNotHidden("A1400") != null) && (AddingNewEncounter) &&
                (CurrentPatient.PatientInsurance != null))
            {
                List<PatientInsurance> list = CurrentPatient.PatientInsurance
                    .Where(i => ((i.Inactive == false) && (i.HistoryKey == null) && (i.PatientInsuranceKey != 0) &&
                                 ((i.EffectiveFromDate.HasValue == false ||
                                   ((DateTime)i.EffectiveFromDate).Date <= _m0090Date) &&
                                  (i.EffectiveThruDate.HasValue == false ||
                                   ((DateTime)i.EffectiveThruDate).Date > _m0090Date))))
                    .OrderByDescending(i => i.InsuranceTypeKey).ToList();
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400A"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400B"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400C"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400D"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400G"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400H"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400I"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400J"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400K"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400X"));
                SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400Y"));
                if ((list == null) || ((list != null) && (list.Any() == false)))
                {
                    SetCheckBoxResponse(false, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400K"));
                }
                else if ((list != null) && list.Any())
                {
                    foreach (PatientInsurance pi in list)
                        if ((pi.InsuranceTypeCode == "1") || (pi.InsuranceTypeCode == "12"))
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400A"));
                        }
                        else if (pi.InsuranceTypeCode == "2")
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400B"));
                        }
                        else if ((pi.InsuranceTypeCode == "3") || (pi.InsuranceTypeCode == "13"))
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400C"));
                        }
                        else if (pi.InsuranceTypeCode == "4")
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400D"));
                        }
                        else if (pi.InsuranceTypeCode == "5") // WRKCOMP not supported in HIS - so check other
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400Y"));
                        }
                        else if (pi.InsuranceTypeCode == "6") // TITLEPGMS not supported in HIS - so check other
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400Y"));
                        }
                        else if (pi.InsuranceTypeCode == "7")
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400G"));
                        }
                        else if (pi.InsuranceTypeCode == "8")
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400H"));
                        }
                        else if (pi.InsuranceTypeCode == "9")
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400I"));
                        }
                        else if (pi.InsuranceTypeCode == "10")
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400J"));
                        }
                        else if (pi.InsuranceTypeCode == "11")
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400Y"));
                        }
                        else
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A1400Y"));
                        }
                }
            }

            // Z0500A/B - Signature/Date verifying record completion - override to the reviewer if in OasisReview 
            if (PreviousEncounterStatus == (int)EncounterStatusType.OASISReview)
            {
                CurrentEncounterOasis.Z0500A = WebContext.Current.User.MemberID;
                SetDateResponse(DateTime.Today.Date, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "Z0500B"),
                    false);
            }

            if (AddingNewEncounter)
            {
                SetupAdmissionDefaultsHIS(
                    currentAdmissionDiscipline); // default most of the rest only during the inital survey load
            }

            if ((CurrentEncounter != null) && (CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed))
            {
                SetupAdmissionDefaultsHIS_Osection(); // Always default / redefault O section until form is complete
            }
        }

        private void InitializeB1RecordHIS()
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice == false))
            {
                return;
            }

            if (CurrentEncounterOasis.REC_ID == "B1")
            {
                return;
            }

            CurrentEncounterOasis.REC_ID = "B1";
            SetResponse(CurrentEncounterOasis.SYS_CD,
                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SET_SYS_CD"));
            if (string.IsNullOrWhiteSpace(CurrentEncounterOasis.ITM_SBST_CD))
            {
                MessageBox.Show(
                    "Error OasisManager.InitializeB1RecordHIS: null ITM_SBST_CD.  Contact your system administrator.");
            }

            SetResponse(CurrentEncounterOasis.ITM_SBST_CD,
                OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SBST_CD"));

            OasisVersion ov = OasisCache.GetOasisVersionByVersionKey(OasisVersionKey);
            if (ov != null)
            {
                SetResponse(ov.VersionCD1, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "ITM_SET_VRSN_CD"));
            }

            if (ov != null)
            {
                SetResponse(ov.VersionCD2, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SPEC_VRSN_CD"));
            }

            SetResponse("00", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "CRCTN_NUM"));

            Delta delta = DeltaCache.GetDelta();
            if (delta != null)
            {
                SetResponse(delta.SFW_ID, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_VNDR_ID"));
                SetResponse(delta.SFW_NAME, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_VNDR_NAME"));
                SetResponse(delta.SFW_EMAIL_ADR,
                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_VNDR_EMAIL_ADR"));
                SetResponse("Crescendo", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_PROD_NAME"));

                string virtuosoVersion = null;
                if ((DynamicFormViewModel != null) && (DynamicFormViewModel.Configuration != null))
                {
                    virtuosoVersion = DynamicFormViewModel.Configuration.VirtuosoVersion;
                }

                SetResponse(((string.IsNullOrWhiteSpace(virtuosoVersion)) ? "01.01" : virtuosoVersion),
                    OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "SFTWR_PROD_VRSN_CD"));
            }

            // A0050 - Type of record
            SetResponse("1", OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0050"));
            // Z0500A/B - Signature/Date verifying record completion
            CurrentEncounterOasis.Z0500A = (CurrentEncounter == null)
                ? WebContext.Current.User.MemberID
                : CurrentEncounter.EncounterBy;
            if ((CurrentEncounter == null) || (CurrentEncounter.EncounterOrTaskStartDateAndTime == null))
            {
                SetDateResponse(DateTime.Today.Date, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "Z0500B"),
                    false);
            }
            else
            {
                SetDateResponse(((DateTimeOffset)CurrentEncounter.EncounterOrTaskStartDateAndTime).Date,
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "Z0500B"), false);
            }
        }

        private void SetupAdmissionOasisHeaderDefaultsHIS()
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice == false))
            {
                return;
            }

            if (!IsOasisActive)
            {
                return;
            }

            // Default OasisHeader info to null
            CurrentEncounterOasis.OasisHeaderKey = null;
            CurrentEncounterOasis.ServiceLineGroupingKey = null;
            CurrentEncounterOasis.HHA_AGENCY_ID = null;
            SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "STATE_CD"));
            SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "FAC_ID"));
            CurrentEncounterOasis.NPI = null;
            SetResponse(null, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0100A"));
            CurrentEncounterOasis.FED_ID = null;
            ClearResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0100B"), true);

            // Get controlling ServiceLineGrouping and OasisHeader
            DateTime surveyDate = (CurrentEncounterOasis.M0090 != null)
                ? (DateTime)CurrentEncounterOasis.M0090
                : ((CurrentEncounter == null)
                    ? DateTime.Today
                    : ((CurrentEncounter.EncounterStartDate == null)
                        ? DateTime.Today
                        : CurrentEncounter.EncounterStartDate.GetValueOrDefault().Date));
            ServiceLineGrouping slg = CurrentAdmission.GetFirstServiceLineGroupWithOasisHeader(surveyDate);
            if (slg == null)
            {
                MessageBox.Show(
                    "There is no Service Line Grouping defined for this admission/encounter with a controlling CMS Header, STATE_CD, FAC_ID, A0100A and A0100B will not be valued.");
                return;
            }

            OasisHeader oh = OasisHeaderCache.GetOasisHeaderFromKey(slg.OasisHeaderKey);
            if (oh == null)
            {
                MessageBox.Show(
                    "There is no Service Line Grouping defined for this admission/encounter with a controlling CMS Header, STATE_CD, FAC_ID, A0100A and A0100B will not be valued.");
                return;
            }

            CurrentEncounterOasis.OasisHeaderKey = oh.OasisHeaderKey;
            CurrentEncounterOasis.ServiceLineGroupingKey = slg.ServiceLineGroupingKey;
            if (oh.BranchState != null)
            {
                SetResponse(oh.BranchStateCode, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "STATE_CD"));
            }

            if (string.IsNullOrWhiteSpace(oh.HHAAgencyID) == false)
            {
                SetResponse(oh.HHAAgencyID, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "FAC_ID"));
                CurrentEncounterOasis.HHA_AGENCY_ID =
                    GetResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "FAC_ID"));
            }

            // A0100A - NPI
            if (string.IsNullOrWhiteSpace(oh.NPI) == false)
            {
                SetResponse(oh.NPI, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0100A"));
                CurrentEncounterOasis.NPI = GetResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0100A"));
            }

            // A0100B - CMS certification number
            if (string.IsNullOrWhiteSpace(oh.CMSCertificationNumber) == false)
            {
                SetResponse(oh.CMSCertificationNumber, OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0100B"));
                CurrentEncounterOasis.FED_ID =
                    GetResponse(OasisCache.GetOasisLayoutByCMSField(OasisVersionKey, "A0100B"));
            }

            Messenger.Default.Send(true, string.Format("OasisHeaderChanged{0}", OasisManagerGuid.ToString().Trim()));
        }

        private void SetupAdmissionDefaultsHIS(AdmissionDiscipline currentAdmissionDiscipline)
        {
            // A0205 - RFA 01 only - no default

            // A0245 - Date initial nursing assessment initiated
            if ((GetQuestionInSurveyNotHidden("A0245") != null) && (CurrentAdmission != null) &&
                (CurrentAdmission.Encounter != null))
            {
                Encounter snEval = CurrentAdmission.Encounter
                    .Where(e => ((e.Inactive == false) && (e.EncounterIsEval) && (e.IsSkilledNursingServiceType) &&
                                 (e.EncounterOrTaskStartDateAndTime != null)))
                    .OrderBy(e => e.EncounterOrTaskStartDateAndTime).FirstOrDefault();
                if (snEval != null)
                {
                    SetDateResponse(((DateTimeOffset)snEval.EncounterOrTaskStartDateAndTime).Date,
                        OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "A0245"));
                }
            }

            // A1802 - Admitted from
            if (GetQuestionInSurveyNotHidden("A1802") != null)
            {
                string soaCode = CurrentAdmission.SourceOfAdmissionCode;
                if (soaCode == "01")
                {
                    SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A1802", 1));
                }
                else if (soaCode == "02")
                {
                    SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A1802", 2));
                }
                else if (soaCode == "03")
                {
                    SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A1802", 3));
                }
                else if (soaCode == "04")
                {
                    SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A1802", 4));
                }
                else if (soaCode == "05")
                {
                    SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A1802", 5));
                }
                else if (soaCode == "06")
                {
                    SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A1802", 6));
                }
                else if (soaCode == "07")
                {
                    SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A1802", 7));
                }
                else if (soaCode == "08")
                {
                    SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A1802", 8));
                }
                else if (soaCode == "09")
                {
                    SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A1802", 9));
                }
                else if (soaCode == "10")
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A1802", 10));
                }
                else if (soaCode == "99")
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A1802", 11));
                }
            }

            // A2115 - Reason for discharge
            if (GetQuestionInSurveyNotHidden("A2115") != null)
            {
                bool A2115Set = false;
                // check AdmissionHospiceDischarge first
                if ((CurrentAdmission != null) && (CurrentAdmission.AdmissionHospiceDischarge != null))
                {
                    Server.Data.AdmissionHospiceDischarge ahd = CurrentAdmission.AdmissionHospiceDischarge
                        .OrderByDescending(a => a.AdmissionHospiceDischargeKey).FirstOrDefault();
                    if (ahd != null)
                    {
                        string drCode = (string.IsNullOrWhiteSpace(ahd.DischargeReasonCode))
                            ? ""
                            : ahd.DischargeReasonCode.ToLower();
                        A2115Set = SetupA2115(drCode, "");
                    }
                }

                // then check currentAdmission
                if (((A2115Set == false) && CurrentAdmission != null))
                {
                    string drCode = (string.IsNullOrWhiteSpace(CurrentAdmission.DischargeReasonCode))
                        ? ""
                        : CurrentAdmission.DischargeReasonCode.ToLower();
                    string drDesc = (string.IsNullOrWhiteSpace(CurrentAdmission.DischargeReasonCodeDescription))
                        ? ""
                        : CurrentAdmission.DischargeReasonCodeDescription.ToLower();
                    A2115Set = SetupA2115(drCode, drDesc);
                }

                // then check currentAdmissionDiscipline if need be
                if ((A2115Set == false) && (currentAdmissionDiscipline != null))
                {
                    string drCode = (string.IsNullOrWhiteSpace(currentAdmissionDiscipline.DischargeReasonCode))
                        ? ""
                        : currentAdmissionDiscipline.DischargeReasonCode.ToLower();
                    string drDesc =
                        (string.IsNullOrWhiteSpace(currentAdmissionDiscipline.DischargeReasonCodeDescription))
                            ? ""
                            : currentAdmissionDiscipline.DischargeReasonCodeDescription.ToLower();
                    A2115Set = SetupA2115(drCode, drDesc);
                }
            }

            if (CurrentEncounterOasis.RFA != "01")
            {
                return;
            }

            // The rest of the defaults are only applicable to HIS admissions
            SetupAdmissionDefaultsHIS_Fsection();
            SetupAdmissionDefaultsHIS_Isection();
            SetupAdmissionDefaultsHIS_Jsection();
            SetupAdmissionDefaultsHIS_Nsection();
        }

        private bool SetupA2115(string drCode, string drDesc)
        {
            if (drCode.Equals("hospdied40") || drCode.Equals("hospdied41") || drCode.Equals("hospdied42") ||
                drCode.Equals("expired"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A2115", 1));
                return true;
            }

            if (drCode.Equals("hosrevoke") || drCode.Equals("revoked"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A2115", 2));
                return true;
            }

            if (drCode.Equals("notterminal") || drCode.Equals("01"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A2115", 3));
                return true;
            }

            if (drCode.Equals("hosmoved") || drCode.Equals("moved"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A2115", 4));
                return true;
            }

            if (drCode.Equals("hospicxfer") || drCode.Equals("hospxfrhom") || drCode.Equals("hospxfrfac") ||
                drCode.Equals("transferred") || drCode.Equals("50") || drCode.Equals("51"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A2115", 5));
                return true;
            }

            if (drCode.Equals("hoscause") || drCode.Equals("forcause"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A2115", 6));
                return true;
            }

            if (drDesc.Contains("expire") || drDesc.Contains("died"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A2115", 1));
                return true;
            }

            if (drDesc.Contains("revoke"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A2115", 2));
                return true;
            }

            if (drDesc.Contains("no longer terminally ill"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A2115", 3));
                return true;
            }

            if (drDesc.Contains("moved"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A2115", 4));
                return true;
            }

            if ((drDesc.Contains("transfer") && drDesc.Contains("hospice")) || (drDesc.Contains("xfer")))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A2115", 5));
                return true;
            }

            if (drDesc.Contains("discharge") && drDesc.Contains("cause"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "A2115", 6));
                return true;
            }

            return false;
        }

        private bool SetupAdmissionDefault_HISSetDate(string cmsDateField, string dateLabel, DateTime startDate,
            DateTime? endDate)
        {
            List<EncounterData> edList = CurrentAdmission.GetEncounterDataByLabel(dateLabel, startDate, endDate);
            if (edList == null)
            {
                return false;
            }

            EncounterData edMR = edList.FirstOrDefault(); // use most recent answer to ask date question 
            if (edMR == null)
            {
                return false;
            }

            SetDateResponse(edMR.DateTimeData.GetValueOrDefault().Date,
                OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, cmsDateField), false);
            return true;
        }

        private void SetupAdmissionDefault_HISSetAskedAbout(string cmsAskField, string askLabel, string cmsDateField,
            string dateLabel, DateTime startDate, DateTime? endDate)
        {
            List<EncounterData> edList = CurrentAdmission.GetEncounterDataByLabel(askLabel, startDate, endDate);
            if (edList == null)
            {
                return;
            }

            EncounterData edMR = edList.FirstOrDefault(); // use most recent answer to ask question 
            if (edMR == null)
            {
                return;
            }

            string code = edMR.IntDataCode;
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            if (code.ToLower().Equals("no"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, cmsAskField, 1));
            }
            else if (code.ToLower().Equals("yes/discussed"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, cmsAskField, 2));
                SetupAdmissionDefault_HISSetDate(cmsDateField, dateLabel, startDate, endDate);
            }
            else if (code.ToLower().Contains("yes/refused"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, cmsAskField, 3));
                SetupAdmissionDefault_HISSetDate(cmsDateField, dateLabel, startDate, endDate);
            }
        }

        private bool SetupAdmissionDefault_HISSetYesNo(string cmsYesNoField, string yesNoLabel, string cmsDateField,
            string dateLabel, DateTime startDate, DateTime? endDate)
        {
            // Return true is asked and the answer was yes - otherwise return false
            List<EncounterData> edList = CurrentAdmission.GetEncounterDataByLabel(yesNoLabel, startDate, endDate);
            if (edList == null)
            {
                return false;
            }

            EncounterData edMR = edList.FirstOrDefault(); // use most recent answer to yesNo question 
            if (edMR == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(edMR.TextData))
            {
                return false;
            }

            if (edMR.TextData.ToLower().Equals("0"))
            {
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, cmsYesNoField, 1));
                return false;
            }

            if (edMR.TextData.ToLower().Equals("1"))
            {
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, cmsYesNoField, 2));
                // If present - populate the date field on a yes response
                if (cmsDateField != null)
                {
                    SetupAdmissionDefault_HISSetDate(cmsDateField, dateLabel, startDate, endDate);
                }

                return true;
            }

            return false;
        }

        private void SetupAdmissionDefault_HISJ2040(DateTime startDate, DateTime? endDate)
        {
            List<EncounterData> edList = CurrentAdmission.GetEncounterDataByLabelAndLookupType(
                "Was treatment for shortness of breath initiated?", "SOBTreatment", startDate, endDate);
            if (edList == null)
            {
                return;
            }

            EncounterData edMR = edList.FirstOrDefault(); // use most recent answer to question 
            if (edMR == null)
            {
                return;
            }

            string code = edMR.IntDataCode;
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            if (code.ToLower().Equals("no"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J2040A", 1));
            }
            else if (code.ToLower().Equals("no, declined"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J2040A", 2));
            }
            else if (code.ToLower().Equals("yes"))
            {
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J2040A", 3));
                SetupAdmissionDefault_HISSetDate("J2040B", "Date treatment for shortness of breath initiated",
                    startDate, endDate);
                // J2040C - Type(s) of treatment for shortness of breath initiated
                edList = CurrentAdmission.GetEncounterDataByLabelAndLookupType(
                    "Type(s) of treatment for shortness of breath initiated", "TreatmentTypeSOB", startDate, endDate);
                if (edList == null)
                {
                    return;
                }

                edMR = edList.FirstOrDefault(); // use most recent answer to ask question 
                if (edMR == null)
                {
                    return;
                }

                string textData = edMR.TextData;
                if (string.IsNullOrWhiteSpace(textData))
                {
                    return;
                }

                if (textData.ToLower().Contains("opiods"))
                {
                    SetCheckBoxResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J2040C1", 1));
                }

                if (textData.ToLower().Contains("other medication"))
                {
                    SetCheckBoxResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J2040C2", 2));
                }

                if (textData.ToLower().Contains("oxygen"))
                {
                    SetCheckBoxResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J2040C3", 3));
                }

                if (textData.ToLower().Contains("non-medication"))
                {
                    SetCheckBoxResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J2040C4", 4));
                }
            }
        }

        private void SetupAdmissionDefaultsHIS_Fsection()
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice == false))
            {
                return;
            }

            if (CurrentEncounterOasis.RFA != "01")
            {
                return;
            }

            DateTime startDate = DateTime.MinValue;
            DateTime? endDate = GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "Z0500B"));
            if (endDate == null)
            {
                endDate = DateTime.Today.Date;
            }

            // F2000 - CPR preferences - use new question format
            SetupAdmissionDefault_HISSetAskedAbout(
                "F2000A", "Was the patient/responsible party asked about preference regarding the use of CPR?",
                "F2000B",
                "Date the patient/responsible party was first asked about preference regarding the use of CPR",
                startDate, endDate);
            // F2100 - Other life sustaining treatment preferences - use new question format
            SetupAdmissionDefault_HISSetAskedAbout(
                "F2100A",
                "Was the patient/responsible party asked about preferences regarding life-sustaining treatments other than CPR?",
                "F2100B",
                "Date the patient/responsible party was first asked about preferences regarding life-sustaining treatments other than CPR",
                startDate, endDate);
            // F2200 - Hospitalization preferences - try new question format first
            SetupAdmissionDefault_HISSetAskedAbout(
                "F2200A", "Was the patient/responsible party asked about preferences regarding hospitalization?",
                "F2200B",
                "Date the patient/responsible party was first asked about preferences regarding hospitalization",
                startDate, endDate);
            // F3000 Spiritual/existential concerns - Check patient first - then caregiver - use new question format 
            SetupAdmissionDefault_HISSetAskedAbout(
                "F3000A", "Was the patient and/or caregiver asked about spiritual/existential concerns?",
                "F3000B", "Date the patient and/or caregiver was first asked about spiritual/existential concerns",
                startDate, endDate);
        }

        private void SetupAdmissionDefaultsHIS_Isection()
        {
            // I0010 - Principal diagnosis
            // only check first icd 9 and/or 10
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice == false))
            {
                return;
            }

            if (CurrentEncounterOasis.RFA != "01")
            {
                return;
            }

            if ((CurrentAdmission == null) || (CurrentAdmission.AdmissionDiagnosis == null))
            {
                return;
            }

            ProcessFilteredAdmissionDiagnosisItems(CurrentAdmission.AdmissionDiagnosis,
                GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "Z0500B")));
            bool checkedPrimary9 = false;
            bool checkedPrimary10 = false;
            foreach (AdmissionDiagnosis ad in CurrentFilteredAdmissionDiagnosis)
            {
                if (ad.IsCancer)
                {
                    if (((ad.Version == 9) && (!checkedPrimary9)) || ((ad.Version == 10) && (!checkedPrimary10)))
                    {
                        SetRadioResponse(true,
                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "I0010", 1));
                    }

                    return;
                }

                if (ad.IsDementiaAlzheimer)
                {
                    if (((ad.Version == 9) && (!checkedPrimary9)) || ((ad.Version == 10) && (!checkedPrimary10)))
                    {
                        SetRadioResponse(true,
                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "I0010", 2));
                    }

                    return;
                }

                if (ad.Version == 9)
                {
                    checkedPrimary9 = true;
                }
                else if (ad.Version == 10)
                {
                    checkedPrimary10 = true;
                }
            }

            SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "I0010", 3));
        }

        private void SetupAdmissionDefaultsHIS_Jsection()
        {
            bool IsPainAnActiveProblem = false;
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice == false))
            {
                return;
            }

            if (CurrentEncounterOasis.RFA != "01")
            {
                return;
            }

            DateTime startDate = DateTime.MinValue;
            DateTime? endDate = GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "Z0500B"));
            if (endDate == null)
            {
                endDate = DateTime.Today.Date;
            }

            // J0900 - Pain screening
            EncounterPain epFirst = CurrentAdmission.GetFirstEncounterPainOfAdmission(startDate, endDate);
            if (epFirst != null)
            {
                // "Pain Assessment" field (painScore) is completed, code J0900A with "1" for Yes. 
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0900A", 2));
                // J0900A was answered "1" Yes, code J0900B with date of the initial Nursing Assessment.
                SetDateResponse(epFirst.Encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date,
                    OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "J0900B"), false);
                // J0900A was answered "1" Yes, code J0900C based on score entered into the standardized pain tool:
                // - Score of "0", code J0900C with "0" None, and Skip to question J2030orJ0905(V1orV2) - Screening for Shortness of Breath)
                // - Scoree between 1 - 3, code J0900C with "1" Mild
                // - Score between 4 - 6, code J0900C with "2" Moderate (4-6 per Bug 30928)
                // - Score between 7 - 10, code J0900C with "3" Severe (7-10 per Bug 30928)
                // - Score is null, code J0900C with "9" Pain Not Rated
                // - In Crescendo, Pain screening is a required field so should never be a null here but must consider the posibility when coding. 
                if (epFirst.PainScoreInt == null)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0900C", 5));
                }
                else if (epFirst.PainScoreInt == 0)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0900C", 1));
                }
                else if (epFirst.PainScoreInt <= 3)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0900C", 2));
                    IsPainAnActiveProblem = true;
                }
                else if (epFirst.PainScoreInt <= 6)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0900C", 3));
                    IsPainAnActiveProblem = true;
                }
                else
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0900C", 4));
                    IsPainAnActiveProblem = true;
                }

                // HIS V1 - If J0900C NOT coded with "0" None, code J0900D based in the standardized pain tool selected 
                // HIS V1 - Code J0900D based in the standardized pain tool selected 
                if (((epFirst.PainScoreInt != 0) && (IsHISVersion2orHigher == false)) || IsHISVersion2orHigher)
                {
                    // We currently only use four standardized pain assessment tools in Crescendo. 
                    // Default J0900D based on the values below, but allow the item to be edited
                    // - If "0 thru 10" scale used, code J0900D with "1" Numeric
                    // - If "Wong-Baker FACES" tool used, code J0900D with "3" Patient Visual
                    // - If "FLACC Scale" used, code J0900D with "4" Staff Observation
                    // - If "PAINAD Scale" used, code J0900D with "4" Staff Observation
                    string painScaleCode = epFirst.PainScaleCode;
                    if (string.IsNullOrWhiteSpace(painScaleCode) == false)
                    {
                        if (painScaleCode.ToUpper() == "10")
                        {
                            SetRadioResponse(true,
                                OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0900D", 1));
                        }
                        else if (painScaleCode.ToUpper() == "FACES")
                        {
                            SetRadioResponse(true,
                                OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0900D", 3));
                        }
                        else if (painScaleCode.ToUpper() == "FLACC")
                        {
                            SetRadioResponse(true,
                                OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0900D", 4));
                        }
                        else if (painScaleCode.ToUpper() == "PAINAD")
                        {
                            SetRadioResponse(true,
                                OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0900D", 4));
                        }
                    }

                    // J0910 - Comprehensive pain assessment
                    Encounter e = CurrentAdmission.GetFirstEncounterPainLocationOfAdmission(startDate, endDate);
                    if (e != null)
                    {
                        // Pain Locations exist, code J0910A with "1" for Yes. 
                        SetRadioResponse(true,
                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0910A", 2));
                        // J0910A was answered "1" Yes, code J0910B with date of the initial Pain Location collection
                        SetDateResponse(epFirst.Encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date,
                            OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "J0910B"), false);
                        // The specific data points of J0910C can be directly related to fields within Pain Locations. 
                        // Use data entered in the related field in Pain Locations to code J0910C. 
                        // These field relationships are as follows;
                        // HIS Item J0910C Check Box                  Corresponding field in Crescendo
                        // 1. Location                                PainLocation column - foreignKey, required - always set
                        // 2. Severity                                Pain Assessment field -  (painScore) completed - always set
                        // 3. Character                               PainQuality column
                        // 4. Duration                                PainFrequency column
                        // 5. Frequency                               PainFrequency column
                        // 6. What relieves/worsens pain              PainAlleviating and/or PainAggravating columns
                        // 7. Effect on function or quality of life   PainInterference column
                        // 9. None of the above                       Since the Pain Locations screen is required and the fields listed above are all required 
                        //                                            on that screen, Crescendo patients who have identified Pain will always have a 
                        //                                            comprehensive Pain Assessment completed. Because of this, a code of “9” None of the above, 
                        //                                            should never be valid for J0910.
                        SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "J0910C1"));
                        SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "J0910C2"));
                        if (e.EncounterPainLocation.Where(ews =>
                                (ews.AdmissionPainLocation.DeletedDate == null) &&
                                (string.IsNullOrWhiteSpace(ews.AdmissionPainLocation.PainQuality) == false)).Any())
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "J0910C3"));
                        }

                        if (e.EncounterPainLocation.Where(ews =>
                                (ews.AdmissionPainLocation.DeletedDate == null) &&
                                (string.IsNullOrWhiteSpace(ews.AdmissionPainLocation.PainFrequency) == false)).Any())
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "J0910C4"));
                        }

                        if (e.EncounterPainLocation.Where(ews =>
                                (ews.AdmissionPainLocation.DeletedDate == null) &&
                                (string.IsNullOrWhiteSpace(ews.AdmissionPainLocation.PainFrequency) == false)).Any())
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "J0910C5"));
                        }

                        if (e.EncounterPainLocation.Where(ews =>
                                (ews.AdmissionPainLocation.DeletedDate == null) &&
                                (string.IsNullOrWhiteSpace(ews.AdmissionPainLocation.PainAlleviating) == false)).Any())
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "J0910C6"));
                        }

                        if (e.EncounterPainLocation.Where(ews =>
                                (ews.AdmissionPainLocation.DeletedDate == null) &&
                                (string.IsNullOrWhiteSpace(ews.AdmissionPainLocation.PainAggravating) == false)).Any())
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "J0910C6"));
                        }

                        if (e.EncounterPainLocation.Where(ews =>
                                (ews.AdmissionPainLocation.DeletedDate == null) &&
                                (string.IsNullOrWhiteSpace(ews.AdmissionPainLocation.PainInterference) == false)).Any())
                        {
                            SetCheckBoxResponse(true, OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "J0910C7"));
                        }

                        ;
                    }
                    else
                    {
                        // Pain Locations do not exist, code J0910A with "0" for No. 
                        SetRadioResponse(true,
                            OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0910A", 1));
                    }
                }
            }
            else
            {
                // "Pain Assessment" field (painScore) is NOT completed, code J0900A with "0" for No. 
                // (If answer "0" No, then skip to Item J2030orJ0905(V1orV2)-Screening for Shortness of Breath)
                SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0900A", 1));
            }

            // J0905(V2) - Pain Active Problem
            if (GetQuestionInSurveyNotHidden("J0905") != null)
            {
                if (IsPainAnActiveProblem)
                {
                    SetRadioResponse(true, OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0905", 2));
                }
                else
                {
                    SetupAdmissionDefault_HISSetYesNo("J0905",
                        "Pain Active Problem - Is pain an active problem for the patient?", null, null, startDate,
                        endDate);
                }
            }

            // J2030 - Screening for shortness of breath
            if (SetupAdmissionDefault_HISSetYesNo(
                    "J2030A", "Was the patient screened for shortness of breath (dyspnea)?",
                    "J2030B", "Date of first screening for shortness of breath", startDate, endDate))
            {
                // J2030A = Yes -> setup J2030C - Did the screening indicate the patient had shortness of breath?
                if (SetupAdmissionDefault_HISSetYesNo(
                        "J2030C", "Did the screening indicate the patient had shortness of breath?",
                        null, null, startDate, endDate))
                {
                    // J2030C = Yes -> setup J2040 
                    SetupAdmissionDefault_HISJ2040(startDate, endDate);
                }
            }
        }

        private void SetupAdmissionDefaultsHIS_Nsection()
        {
            if ((CurrentEncounterOasis == null) || (CurrentEncounterOasis.SYS_CDIsHospice == false))
            {
                return;
            }

            if (CurrentEncounterOasis.RFA != "01")
            {
                return;
            }

            DateTime startDate = DateTime.MinValue;
            DateTime? endDate = GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "Z0500B"));
            if (endDate == null)
            {
                endDate = DateTime.Today.Date;
            }

            // N0500 - Was a scheduled opioid initiated or continued?
            if (SetupAdmissionDefault_HISSetYesNo("N0500A", "Was a scheduled opioid initiated or continued?", "N0500B",
                    "Date scheduled opioid initiated or continued?", startDate, endDate))
            {
                // If opioids are used - assume pain is an active problem
                if (GetQuestionInSurveyNotHidden("J0905") != null)
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "J0905",
                            2)); // CMS gives a warning
                }
            }

            // Setup N0510 - Was a PRN opioid initiated or continued?
            if (SetupAdmissionDefault_HISSetYesNo(
                    "N0510A", "Was a PRN opioid initiated or continued?",
                    "N0510B", "Date PRN opioid initiated or continued", startDate, endDate))
            {
                // N0510A = Yes -> Setup N0520 - Was a Bowel Regimen Initiated/Continued?
                List<EncounterData> edList = CurrentAdmission.GetEncounterDataByLabelAndLookupType(
                    "Was a Bowel Regimen Initiated/Continued?", "BowelRegimenInitiate", startDate, endDate);
                if (edList == null)
                {
                    return;
                }

                EncounterData edMR = edList.FirstOrDefault(); // use most recent answer to question 
                if (edMR == null)
                {
                    return;
                }

                string code = edMR.IntDataCode;
                if (string.IsNullOrWhiteSpace(code))
                {
                    return;
                }

                if (code.ToLower().Equals("0"))
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "N0520A", 1));
                }
                else if (code.ToLower().Equals("1"))
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "N0520A", 2));
                }
                else if (code.ToLower().Equals("2"))
                {
                    SetRadioResponse(true,
                        OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "N0520A", 3));
                    SetupAdmissionDefault_HISSetDate("N0520B", "Date Bowel Regimen Initiated/Continued", startDate,
                        endDate);
                }
            }
        }

        private void SetupAdmissionDefaultsHIS_Osection()
        {
            // Default/redefault service utilization until EncounterStatus is Complete
            // In case more hospice visits have gone complete since the form was started
            // leave physician blank during newEncounter defaulting, as there is currently no way to default them - otherwise leave the users responses as-is  

            if (PreviousEncounterStatus == (int)EncounterStatusType.Completed)
            {
                return;
            }

            if (CurrentAdmission == null)
            {
                return;
            }

            DateTime A0270 = (CurrentAdmission.DischargeDateTime == null)
                ? DateTime.Today.Date
                : ((DateTime)CurrentAdmission.DischargeDateTime).Date;

            if (GetQuestionInSurveyNotHidden("O5000") != null)
            {
                bool onlyRoutineInLast3Days = CurrentAdmission.OnlyRoutineCareInDateRange(A0270.AddDays(-2), A0270);
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "O5000",
                        ((onlyRoutineInLast3Days) ? 1 : 2)));
            }

            if (GetQuestionInSurveyNotHidden("O5010") != null)
            {
                List<Encounter> eList =
                    CurrentAdmission.GetEvalAndVisitEncountersInclusive(A0270.AddDays(-2), A0270, true);
                if (eList == null)
                {
                    eList = new List<Encounter>();
                }

                eList = eList.Where(e =>
                        ((e.ServiceTypeDescriptionContains("phone") == false) && (e.ServiceTypeNonBillable == false)))
                    .ToList();

                SetTextResponse(GetVisitCount(eList, "A", "RN", A0270), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010A1"));
                SetTextResponse(GetVisitCount(eList, "A", "RN", A0270.AddDays(-1)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010A2"));
                SetTextResponse(GetVisitCount(eList, "A", "RN", A0270.AddDays(-2)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010A3"));
                SetTextResponse(GetVisitCount(eList, "P", null, A0270), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010B1"));
                SetTextResponse(GetVisitCount(eList, "P", null, A0270.AddDays(-1)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010B2"));
                SetTextResponse(GetVisitCount(eList, "P", null, A0270.AddDays(-2)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010B3"));
                SetTextResponse(GetVisitCount(eList, "E", null, A0270), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010C1"));
                SetTextResponse(GetVisitCount(eList, "E", null, A0270.AddDays(-1)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010C2"));
                SetTextResponse(GetVisitCount(eList, "E", null, A0270.AddDays(-2)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010C3"));
                SetTextResponse(GetVisitCount(eList, "S", null, A0270), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010D1"));
                SetTextResponse(GetVisitCount(eList, "S", null, A0270.AddDays(-1)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010D2"));
                SetTextResponse(GetVisitCount(eList, "S", null, A0270.AddDays(-2)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010D3"));
                SetTextResponse(GetVisitCount(eList, "A", "LPN", A0270), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010E1"));
                SetTextResponse(GetVisitCount(eList, "A", "LPN", A0270.AddDays(-1)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010E2"));
                SetTextResponse(GetVisitCount(eList, "A", "LPN", A0270.AddDays(-2)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010E3"));
                SetTextResponse(GetVisitCount(eList, "F", null, A0270), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010F1"));
                SetTextResponse(GetVisitCount(eList, "F", null, A0270.AddDays(-1)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010F2"));
                SetTextResponse(GetVisitCount(eList, "F", null, A0270.AddDays(-2)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5010F3"));
            }

            if (GetQuestionInSurveyNotHidden("O5020") != null)
            {
                bool onlyRoutineInLast7Days = CurrentAdmission.OnlyRoutineCareInDateRange(A0270.AddDays(-6), A0270);
                SetRadioResponse(true,
                    OasisCache.GetOasisAnswerByCMSFieldAndSequence(OasisVersionKey, "O5020",
                        ((onlyRoutineInLast7Days) ? 1 : 2)));
            }

            if (GetQuestionInSurveyNotHidden("O5030") != null)
            {
                List<Encounter> eList =
                    CurrentAdmission.GetEvalAndVisitEncountersInclusive(A0270.AddDays(-6), A0270.AddDays(-3), true);
                if (eList == null)
                {
                    eList = new List<Encounter>();
                }

                eList = eList.Where(e =>
                        ((e.ServiceTypeDescriptionContains("phone") == false) && (e.ServiceTypeNonBillable == false)))
                    .ToList();

                SetTextResponse(GetVisitCount(eList, "A", "RN", A0270.AddDays(-3)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030A1"));
                SetTextResponse(GetVisitCount(eList, "A", "RN", A0270.AddDays(-4)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030A2"));
                SetTextResponse(GetVisitCount(eList, "A", "RN", A0270.AddDays(-5)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030A3"));
                SetTextResponse(GetVisitCount(eList, "A", "RN", A0270.AddDays(-6)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030A4"));
                SetTextResponse(GetVisitCount(eList, "P", null, A0270.AddDays(-3)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030B1"));
                SetTextResponse(GetVisitCount(eList, "P", null, A0270.AddDays(-4)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030B2"));
                SetTextResponse(GetVisitCount(eList, "P", null, A0270.AddDays(-5)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030B3"));
                SetTextResponse(GetVisitCount(eList, "P", null, A0270.AddDays(-6)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030B4"));
                SetTextResponse(GetVisitCount(eList, "E", null, A0270.AddDays(-3)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030C1"));
                SetTextResponse(GetVisitCount(eList, "E", null, A0270.AddDays(-4)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030C2"));
                SetTextResponse(GetVisitCount(eList, "E", null, A0270.AddDays(-5)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030C3"));
                SetTextResponse(GetVisitCount(eList, "E", null, A0270.AddDays(-6)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030C4"));
                SetTextResponse(GetVisitCount(eList, "S", null, A0270.AddDays(-3)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030D1"));
                SetTextResponse(GetVisitCount(eList, "S", null, A0270.AddDays(-4)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030D2"));
                SetTextResponse(GetVisitCount(eList, "S", null, A0270.AddDays(-5)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030D3"));
                SetTextResponse(GetVisitCount(eList, "S", null, A0270.AddDays(-6)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030D4"));
                SetTextResponse(GetVisitCount(eList, "A", "LPN", A0270.AddDays(-3)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030E1"));
                SetTextResponse(GetVisitCount(eList, "A", "LPN", A0270.AddDays(-4)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030E2"));
                SetTextResponse(GetVisitCount(eList, "A", "LPN", A0270.AddDays(-5)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030E3"));
                SetTextResponse(GetVisitCount(eList, "A", "LPN", A0270.AddDays(-6)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030E4"));
                SetTextResponse(GetVisitCount(eList, "F", null, A0270.AddDays(-3)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030F1"));
                SetTextResponse(GetVisitCount(eList, "F", null, A0270.AddDays(-4)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030F2"));
                SetTextResponse(GetVisitCount(eList, "F", null, A0270.AddDays(-5)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030F3"));
                SetTextResponse(GetVisitCount(eList, "F", null, A0270.AddDays(-6)), OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, "O5030F4"));
            }
        }

        private string GetVisitCount(List<Encounter> eList, string HCFACode, string RNorLPN, DateTime date)
        {
            if (eList == null)
            {
                return "0";
            }

            int count = eList.Where(e =>
                (e.IsHCFACode(HCFACode) && (e.RNorLPN == RNorLPN) &&
                 (e.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date == date))).Count();

            if (count > 9)
            {
                count = 9;
            }

            return string.Format("{0:0}", count);
        }

        #region OASIS_DATE_CHANGE_TRACE_CODE

        private string[] _OASIS_DATE_FIELDS;

        private string[] OASIS_DATE_FIELDS
        {
            get
            {
                if (_OASIS_DATE_FIELDS == null)
                {
                    _OASIS_DATE_FIELDS = new[] { "M0030", "M0032", "M0090", "M0906" };
                }

                return _OASIS_DATE_FIELDS;
            }
        }

        private OasisDateChangeEvent GetOasisDateChangeEvent(string tag, string method, string cmsField)
        {
            try
            {
                if ((string.IsNullOrEmpty(cmsField) == false) && OASIS_DATE_FIELDS.Any(s => cmsField.Contains(s)))
                {
                    var _data = CreateOasisDateChangeEvent(tag, method, cmsField);
                    return _data;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        private OasisDateChangeEvent CreateOasisDateChangeEvent(string tag, string method, string cmsField)
        {
            try
            {
                var _data = new OasisDateChangeEvent(ConstructorTag, method + tag, cmsField, CurrentPatient,
                    CurrentAdmission, CurrentEncounter, CurrentEncounterOasis, CurrentForm,
                    cmsfield =>
                    {
                        return GetDateResponse(OasisCache.GetOasisAnswerByCMSField(OasisVersionKey, cmsfield, false),
                            false);
                    });
                return _data;
            }
            catch (Exception)
            {
            }

            return null;
        }

        private OasisDateDiffEvent CreateOasisDateDiffEvent(OasisDateChangeEvent beginDateDiff,
            OasisDateChangeEvent endDateDiff)
        {
            try
            {
                //if beginDateDiff fields differ from endDateDiff fields then return an OasisDateDiffEvent event, else return null
                if (
                    beginDateDiff.Parsed_M0030 != endDateDiff.Parsed_M0030 ||
                    beginDateDiff.Parsed_M0030_START_CARE_DT != endDateDiff.Parsed_M0030_START_CARE_DT ||
                    beginDateDiff.Parsed_M0032 != endDateDiff.Parsed_M0032 ||
                    beginDateDiff.Parsed_M0032_ROC_DT != endDateDiff.Parsed_M0032_ROC_DT ||
                    beginDateDiff.Parsed_M0032_ROC_DT_NA != endDateDiff.Parsed_M0032_ROC_DT_NA ||
                    beginDateDiff.Parsed_M0906_DC_TRAN_DTH_DT != endDateDiff.Parsed_M0906_DC_TRAN_DTH_DT ||
                    beginDateDiff.Parsed_M0090 != endDateDiff.Parsed_M0090 ||
                    beginDateDiff.Parsed_M0090_INFO_COMPLETED_DT != endDateDiff.Parsed_M0090_INFO_COMPLETED_DT
                )
                {
                    var _diff = new OasisDateDiffEvent
                    {
                        CreatedDate = DateTime.Now,
                        ConstructorTag = beginDateDiff.ConstructorTag,
                        PatientKey = beginDateDiff.PatientKey,
                        EncounterKey = beginDateDiff.EncounterKey,
                        RFA = beginDateDiff.RFA,
                        EncounterStatus = beginDateDiff.EncounterStatus,

                        Before_Parsed_M0030 = beginDateDiff.Parsed_M0030,
                        Before_Parsed_M0030_START_CARE_DT = beginDateDiff.Parsed_M0030_START_CARE_DT,
                        Before_Parsed_M0032 = beginDateDiff.Parsed_M0032,
                        Before_Parsed_M0032_ROC_DT = beginDateDiff.Parsed_M0032_ROC_DT,
                        Before_Parsed_M0032_ROC_DT_NA = beginDateDiff.Parsed_M0032_ROC_DT_NA,
                        Before_Parsed_M0906_DC_TRAN_DTH_DT = beginDateDiff.Parsed_M0906_DC_TRAN_DTH_DT,
                        Before_Parsed_M0090 = beginDateDiff.Parsed_M0090,
                        Before_Parsed_M0090_INFO_COMPLETED_DT = beginDateDiff.Parsed_M0090_INFO_COMPLETED_DT,

                        After_Parsed_M0030 = endDateDiff.Parsed_M0030,
                        After_Parsed_M0030_START_CARE_DT = endDateDiff.Parsed_M0030_START_CARE_DT,
                        After_Parsed_M0032 = endDateDiff.Parsed_M0032,
                        After_Parsed_M0032_ROC_DT = endDateDiff.Parsed_M0032_ROC_DT,
                        After_Parsed_M0032_ROC_DT_NA = endDateDiff.Parsed_M0032_ROC_DT_NA,
                        After_Parsed_M0906_DC_TRAN_DTH_DT = endDateDiff.Parsed_M0906_DC_TRAN_DTH_DT,
                        After_Parsed_M0090 = endDateDiff.Parsed_M0090,
                        After_Parsed_M0090_INFO_COMPLETED_DT = endDateDiff.Parsed_M0090_INFO_COMPLETED_DT
                    };
                    return _diff;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        public class OasisDateDiffEvent
        {
            public DateTime CreatedDate { get; set; }
            public string ConstructorTag { get; set; }
            public int PatientKey { get; set; }
            public int EncounterKey { get; set; }
            public string RFA { get; set; }

            public int EncounterStatus { get; set; }

            public DateTime? Before_Parsed_M0030 { get; set; }
            public DateTime? Before_Parsed_M0030_START_CARE_DT { get; set; }
            public DateTime? Before_Parsed_M0032 { get; set; }
            public DateTime? Before_Parsed_M0032_ROC_DT { get; set; }
            public DateTime? Before_Parsed_M0032_ROC_DT_NA { get; set; }
            public DateTime? Before_Parsed_M0906_DC_TRAN_DTH_DT { get; set; }
            public DateTime? Before_Parsed_M0090 { get; set; }
            public DateTime? Before_Parsed_M0090_INFO_COMPLETED_DT { get; set; }

            public DateTime? After_Parsed_M0030 { get; set; }
            public DateTime? After_Parsed_M0030_START_CARE_DT { get; set; }
            public DateTime? After_Parsed_M0032 { get; set; }
            public DateTime? After_Parsed_M0032_ROC_DT { get; set; }
            public DateTime? After_Parsed_M0032_ROC_DT_NA { get; set; }
            public DateTime? After_Parsed_M0906_DC_TRAN_DTH_DT { get; set; }
            public DateTime? After_Parsed_M0090 { get; set; }
            public DateTime? After_Parsed_M0090_INFO_COMPLETED_DT { get; set; }
        }

        public class OasisDateChangeEvent
        {
            public OasisDateChangeEvent()
            {
                CreatedDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            }

            public OasisDateChangeEvent(string constructorTag, string tag, string cmsField, Patient currentPatient,
                Admission currentAdmission, Encounter currentEncounter, EncounterOasis currentEncounterOasis,
                Form currentForm, Func<string, DateTime?> getDateResponse)
            {
                CreatedDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                try
                {
                    URI = System.Windows.Application.Current.Host.Source.ToString();
                }
                catch (Exception)
                {
                }

                TenantID = (currentPatient != null) ? currentPatient.TenantID : -1;

                ConstructorTag = constructorTag;
                Tag = tag;
                CMSField = cmsField;
                PatientKey = (currentPatient != null) ? currentPatient.PatientKey : -1;
                AdmissionKey = (currentAdmission != null) ? currentAdmission.AdmissionKey : -1;
                TaskKey = (currentEncounter != null) ? currentEncounter.TaskKey : -1;
                EncounterKey = (currentEncounter != null) ? currentEncounter.EncounterKey : -1;
                FormKey = (currentForm != null) ? currentForm.FormKey : -1;

                RFA = currentEncounterOasis.RFA;

                SOCDate = currentAdmission.SOCDate;
                EncounterStatus = currentEncounter.EncounterStatus;

                EncounterStartDate = currentEncounter.EncounterStartDate;
                EncounterStartTime = currentEncounter.EncounterStartTime;

                M0090 = currentEncounterOasis.M0090;
                B1Record = currentEncounterOasis.B1Record;

                Parsed_M0030 = getDateResponse("M0030");
                Parsed_M0030_START_CARE_DT = getDateResponse("M0030_START_CARE_DT");
                Parsed_M0032 = getDateResponse("M0032");
                Parsed_M0032_ROC_DT = getDateResponse("M0032_ROC_DT");
                Parsed_M0032_ROC_DT_NA = getDateResponse("M0032_ROC_DT_NA");
                Parsed_M0090 = getDateResponse("M0090");
                Parsed_M0090_INFO_COMPLETED_DT = getDateResponse("M0090_INFO_COMPLETED_DT");
                Parsed_M0906_DC_TRAN_DTH_DT = getDateResponse("M0906_DC_TRAN_DTH_DT");
            }

            public DateTime CreatedDate { get; set; }
            public string URI { get; set; }
            public int TenantID { get; set; }

            public string ConstructorTag { get; set; }
            public string Tag { get; set; }
            public string CMSField { get; set; }

            public int PatientKey { get; set; }
            public int AdmissionKey { get; set; }
            public int? TaskKey { get; set; }
            public int EncounterKey { get; set; }
            public int FormKey { get; set; }

            public int EncounterStatus { get; set; }

            public string RFA { get; set; }

            public DateTime? SOCDate { get; set; }

            public DateTime? Parsed_M0030 { get; set; }
            public DateTime? Parsed_M0030_START_CARE_DT { get; set; }

            public DateTime? Parsed_M0032 { get; set; }
            public DateTime? Parsed_M0032_ROC_DT { get; set; }
            public DateTime? Parsed_M0032_ROC_DT_NA { get; set; }

            public DateTime? Parsed_M0906_DC_TRAN_DTH_DT { get; set; }

            public DateTime? Parsed_M0090 { get; set; }
            public DateTime? Parsed_M0090_INFO_COMPLETED_DT { get; set; }

            public DateTimeOffset? EncounterStartDate { get; set; }
            public DateTimeOffset? EncounterStartTime { get; set; }

            public DateTime? M0090 { get; set; }
            public string B1Record { get; set; }
        }

        #endregion OASIS_DATE_CHANGE_TRACE_CODE

        internal void HISQuestionChanged(Question question, EncounterData ed)
        {

        }
    }
}