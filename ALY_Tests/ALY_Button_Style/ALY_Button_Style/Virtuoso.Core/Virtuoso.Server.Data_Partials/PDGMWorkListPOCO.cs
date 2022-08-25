#region Usings

using System;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Portable.Extensions;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public enum M1033StartPos
    {
        M1033_HOSP_RISK_HSTRY_FALLS_StartPos = 0,
        M1033_HOSP_RISK_WEIGHT_LOSS_StartPos = 1,
        M1033_HOSP_RISK_MLTPL_HOSPZTN_StartPos = 2,
        M1033_HOSP_RISK_MLTPL_ED_VISIT_StartPos = 3,
        M1033_HOSP_RISK_MNTL_BHV_DCLN_StartPos = 4,
        M1033_HOSP_RISK_COMPLIANCE_StartPos = 5,
        M1033_HOSP_RISK_5PLUS_MDCTN_StartPos = 6
    }

    public partial class PDGMWorkListPOCO
    {
        private bool _ReviewedSelect;

        private bool MyLoaded;
        private Guid? MyReviewBy;
        private DateTimeOffset? MyReviewDateTime;
        private bool? MyReviewed;

        public bool IsInFiveDayWindow
        {
            get
            {
                if (PPSBillEndDate == null)
                {
                    return false;
                }

                var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
                return today.AddDays(5) > ((DateTime)PPSBillEndDate).Date ? true : false;
            }
        }

        public string BillingPeriodDates
        {
            get
            {
                var startDate = PPSBillStartDate == null
                    ? "?? "
                    : ((DateTime)PPSBillStartDate).Date.ToShortDateString();
                var endDate = PPSBillEndDate == null ? "?? " : ((DateTime)PPSBillEndDate).Date.ToShortDateString();
                return string.Format("{0} - {1}", startDate, endDate);
            }
        }

        public string MRNAdmissionID => string.Format("{0} - {1}", MRN, AdmissionID);

        public string BillingPeriodAndDates => string.Format("{0}: {1}", SequenceNum.ToString(), BillingPeriodDates);

        public string PatientFullName => FormatHelper.FormatName(LastName, FirstName, MiddleName);

        public string PatientFullNameWithMRN => string.Format("{0} - {1}", PatientFullName, MRN);

        public string AdmissionStatusDescription => CodeLookupCache.GetCodeDescriptionFromKey(AdmissionStatus);

        public string FinalIndicatorDescription => FinalIndicator ? "(final)" : null;

        public int M1033Score
        {
            get
            {
                if (string.IsNullOrWhiteSpace(M1033))
                {
                    return 0;
                }

                var countM1033 = 0;
                // If 3 or fewer items are checked in M1033 (excluding 8, 9 or 10) then 0 points are allocated
                // If more than 3 items are checked in M1033 (excluding 8, 9, or 10) then 11 points are allocated.
                if (IsM1033AnswerChecked((int)M1033StartPos.M1033_HOSP_RISK_5PLUS_MDCTN_StartPos))
                {
                    countM1033++;
                }

                if (IsM1033AnswerChecked((int)M1033StartPos.M1033_HOSP_RISK_COMPLIANCE_StartPos))
                {
                    countM1033++;
                }

                if (IsM1033AnswerChecked((int)M1033StartPos.M1033_HOSP_RISK_HSTRY_FALLS_StartPos))
                {
                    countM1033++;
                }

                if (IsM1033AnswerChecked((int)M1033StartPos.M1033_HOSP_RISK_MLTPL_ED_VISIT_StartPos))
                {
                    countM1033++;
                }

                if (IsM1033AnswerChecked((int)M1033StartPos.M1033_HOSP_RISK_MLTPL_HOSPZTN_StartPos))
                {
                    countM1033++;
                }

                if (IsM1033AnswerChecked((int)M1033StartPos.M1033_HOSP_RISK_MNTL_BHV_DCLN_StartPos))
                {
                    countM1033++;
                }

                if (IsM1033AnswerChecked((int)M1033StartPos.M1033_HOSP_RISK_WEIGHT_LOSS_StartPos))
                {
                    countM1033++;
                }

                return countM1033;
            }
        }

        public int M1033Filter => M1033Score < 4 ? 0 : 11;

        public int? PeriodDay
        {
            get
            {
                if (PPSBillStartDate == null)
                {
                    return null;
                }

                var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
                if (today < ((DateTime)PPSBillStartDate).Date)
                {
                    return 1;
                }

                return (today - ((DateTime)PPSBillStartDate).Date).Days + 1;
            }
        }

        public bool HasICDChanged
        {
            get
            {
                if (string.IsNullOrWhiteSpace(InitialICDCode))
                {
                    return false;
                }

                return InitialICDCode != CurrentICDCode;
            }
        }

        public string CurrentICDCodeToolTip
        {
            get
            {
                if (string.IsNullOrWhiteSpace(InitialICDCode) && string.IsNullOrWhiteSpace(CurrentICDCode))
                {
                    return "A Primary Diagnosis has not yet been assigned to this billing period.";
                }

                if (string.IsNullOrWhiteSpace(InitialICDCode))
                {
                    return
                        "An initial Primary Diagnosis, covering the billing start date, was not assigned for this billing period.";
                }

                if (HasICDChanged)
                {
                    return "The Primary Diagnosis code was changed from " + InitialICDCode + " to " + CurrentICDCode +
                           " after the start of this billing period.";
                }

                return "The Primary Diagnosis has not changed within this billing period.";
            }
        }

        public string ReviewedToolTip
        {
            get
            {
                if (MyReviewed == null || MyReviewed == false)
                {
                    return null;
                }

                var dateTime = MyReviewDateTime == null
                    ? ""
                    : ((DateTimeOffset)MyReviewDateTime).DateTime.ToString("MM/dd/yyyy");
                if (string.IsNullOrWhiteSpace(dateTime) == false)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        dateTime = dateTime + " " + ((DateTimeOffset)MyReviewDateTime).DateTime.ToString("HHmm");
                    }
                    else
                    {
                        dateTime = dateTime + " " + ((DateTimeOffset)MyReviewDateTime).DateTime.ToShortTimeString();
                    }
                }
                else
                {
                    dateTime = "??";
                }

                var reviewBy = MyReviewBy == null
                    ? "??"
                    : UserCache.Current.GetFullNameWithSuffixFromUserId(MyReviewBy);
                return string.Format("Reviewed by {0} on {1}", reviewBy, dateTime);
            }
        }

        public string DeconstructedHIPPSCode
        {
            get
            {
                string deconstructedHIPPS = null;
                if (string.IsNullOrWhiteSpace(TimingAdmissionSource) == false)
                {
                    deconstructedHIPPS = deconstructedHIPPS + " Source/Timing: " + TimingAdmissionSource + ",";
                }

                if (string.IsNullOrWhiteSpace(ClinicalGroup) == false)
                {
                    deconstructedHIPPS = deconstructedHIPPS + " Clinical Group: " + ClinicalGroup + ",";
                }

                if (string.IsNullOrWhiteSpace(FunctionalLevel) == false)
                {
                    deconstructedHIPPS = deconstructedHIPPS + " Func Level: " + FunctionalLevel + ",";
                }

                if (ComorbidityAdjustment != null)
                {
                    deconstructedHIPPS = deconstructedHIPPS + " Comorbidity: " + ComorbidityAdjustment + ",";
                }

                if (LUPAVisitThreshold != null)
                {
                    deconstructedHIPPS = deconstructedHIPPS + " LUPA Threshold: " + LUPAVisitThreshold + ",";
                }

                if (CaseMixWeight != null)
                {
                    deconstructedHIPPS = deconstructedHIPPS + " CM Weight: " + CaseMixWeight;
                }

                if (string.IsNullOrWhiteSpace(FinalIndicatorDescription) == false)
                {
                    deconstructedHIPPS = deconstructedHIPPS + "  " + FinalIndicatorDescription;
                }

                return string.IsNullOrWhiteSpace(deconstructedHIPPS)
                    ? null
                    : "   Deconstructed HIPPS - " + deconstructedHIPPS;
            }
        }

        public bool HasDeconstructedHIPPSCode => string.IsNullOrWhiteSpace(DeconstructedHIPPSCode) ? false : true;

        public bool ReviewedSelect
        {
            get
            {
                if (MyLoaded == false)
                {
                    MyReviewed = Reviewed;
                    MyReviewBy = ReviewBy;
                    MyReviewDateTime = ReviewDateTime;
                    _ReviewedSelect = Reviewed == null || Reviewed == false ? false : true;
                    MyLoaded = true;
                }

                return _ReviewedSelect;
            }
            set
            {
                if (_ReviewedSelect == value)
                {
                    return;
                }

                _ReviewedSelect = value;
                MyReviewed = _ReviewedSelect;
                MyReviewBy = WebContext.Current.User.MemberID;
                MyReviewDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                var abr = new AdmissionBillingReview
                {
                    AdmissionBillingKey = AdmissionBillingKey,
                    Reviewed = (bool)MyReviewed,
                    ReviewBy = (Guid)MyReviewBy,
                    ReviewDateTime = (DateTimeOffset)MyReviewDateTime
                };
                RaisePropertyChanged("ReviewedSelect");
                Messenger.Default.Send(abr, "SaveAdmissionBillingReview");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => { RaisePropertyChanged("ReviewedToolTip"); });
                });
            }
        }

        private bool IsM1033AnswerChecked(int startPos)
        {
            return M1033.Substring(startPos, 1) == "1";
        }
    }
}