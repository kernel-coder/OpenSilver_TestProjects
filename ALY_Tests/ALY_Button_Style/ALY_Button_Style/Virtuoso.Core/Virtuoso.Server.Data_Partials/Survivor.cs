#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Converters;
using Virtuoso.Portable.Extensions;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class DeceasedSearch
    {
        private string _BereavementLocationCodeDescription;
        private string _BereavementSourceCodeDescription;
        private string _DeceasedGenderCodeDescription;
        private string _RelationshipCodeDescription;

        public string SurvivorFullNameWithMiddleInitial
        {
            get
            {
                var name = string.Format("{0},{1}{2}",
                    string.IsNullOrWhiteSpace(SurvivorLastName) ? "" : SurvivorLastName.Trim(),
                    string.IsNullOrWhiteSpace(SurvivorFirstName) ? "" : " " + SurvivorFirstName.Trim(),
                    string.IsNullOrWhiteSpace(SurvivorMiddleInitial) ? "" : " " + SurvivorMiddleInitial.Trim() + ".");
                if (name == ",")
                {
                    name = " ";
                }

                return name;
            }
        }

        public string DeceasedFullNameWithMiddleInitial
        {
            get
            {
                var name = string.Format("{0},{1}{2}",
                    string.IsNullOrWhiteSpace(DeceasedLastName) ? "" : DeceasedLastName.Trim(),
                    string.IsNullOrWhiteSpace(DeceasedFirstName) ? "" : " " + DeceasedFirstName.Trim(),
                    string.IsNullOrWhiteSpace(DeceasedMiddleInitial) ? "" : " " + DeceasedMiddleInitial.Trim() + ".");
                if (name == ",")
                {
                    name = " ";
                }

                return name;
            }
        }

        public string DeceasedFullNameWithMiddleInitialAndMRN
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DeceasedMRN) == false)
                {
                    return DeceasedFullNameWithMiddleInitial + "  -  " + DeceasedMRN;
                }

                return DeceasedFullNameWithMiddleInitial;
            }
        }

        public string RelationshipAndDeceasedFullNameWithMiddleInitialAndDeathDate
        {
            get
            {
                if (string.IsNullOrWhiteSpace(RelationshipCodeDescription) == false)
                {
                    return RelationshipCodeDescription + " of " + DeceasedFullNameWithMiddleInitial + ", died " +
                           DeceasedDeathDateString;
                }

                return DeceasedFullNameWithMiddleInitial;
            }
        }

        public string BereavementSourceCodeDescription
        {
            get
            {
                if (_BereavementSourceCodeDescription != null)
                {
                    return _BereavementSourceCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(BereavementSourceKey);
                _BereavementSourceCodeDescription = string.IsNullOrWhiteSpace(d) ? "Unknown" : d.Trim();
                return _BereavementSourceCodeDescription;
            }
        }

        public string BereavementLocationCodeDescription
        {
            get
            {
                if (_BereavementLocationCodeDescription != null)
                {
                    return _BereavementLocationCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(BereavementLocationKey);
                _BereavementLocationCodeDescription = string.IsNullOrWhiteSpace(d) ? "Unknown" : d.Trim();
                return _BereavementLocationCodeDescription;
            }
        }

        public string RelationshipCodeDescription
        {
            get
            {
                if (_RelationshipCodeDescription != null)
                {
                    return _RelationshipCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(Relationship);
                _RelationshipCodeDescription = string.IsNullOrWhiteSpace(d) ? "Unknown relation" : d.Trim();
                return _RelationshipCodeDescription;
            }
        }

        public string DeceasedDeathDateString =>
            DeceasedDeathDate == null ? "??" : ((DateTime)DeceasedDeathDate).ToShortDateString();

        public string EnrollmentStartDateString
        {
            get
            {
                if (EnrollmentStartDate == null)
                {
                    return "Not enrolled";
                }

                if (DisenrollmentDate != null)
                {
                    return "Disenrolled";
                }

                return ((DateTime)EnrollmentStartDate).ToShortDateString();
            }
        }

        public DateTime EnrollmentStartDateSort
        {
            get
            {
                if (EnrollmentStartDate == null)
                {
                    return DateTime.MaxValue.AddDays(-1);
                }

                if (DisenrollmentDate != null)
                {
                    return DateTime.MaxValue;
                }

                return (DateTime)EnrollmentStartDate;
            }
        }

        public string MrnDashEnrollmentID =>
            DeceasedMRN + " - " + (EnrollmentID == null ? "?" : ((int)EnrollmentID).ToString());

        public string DeceasedGenderCodeDescription
        {
            get
            {
                if (_DeceasedGenderCodeDescription != null)
                {
                    return _DeceasedGenderCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(DeceasedGender);
                _DeceasedGenderCodeDescription = string.IsNullOrWhiteSpace(d) ? "Unknown" : d.Trim();
                return _DeceasedGenderCodeDescription;
            }
        }

        partial void OnBereavementSourceKeyChanged()
        {
            _BereavementSourceCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("BereavementSourceCodeDescription");
        }

        partial void OnBereavementLocationKeyChanged()
        {
            _BereavementLocationCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("BereavementLocationCodeDescription");
        }

        partial void OnRelationshipChanged()
        {
            _RelationshipCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("RelationshipCodeDescription");
            RaisePropertyChanged("RelationshipAndDeceasedFullNameWithMiddleInitialAndDeathDate");
        }

        partial void OnDeceasedDeathDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DeceasedDeathDateString");
        }

        partial void OnEnrollmentStartDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EnrollmentStartDateString");
            RaisePropertyChanged("EnrollmentStartDateSort");
        }

        partial void OnDisenrollmentDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EnrollmentStartDateString");
            RaisePropertyChanged("EnrollmentStartDateSort");
        }

        partial void OnEnrollmentIDChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MrnDashEnrollmentID");
        }

        partial void OnDeceasedGenderChanged()
        {
            _DeceasedGenderCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DeceasedGenderCodeDescription");
        }
    }

    public partial class Survivor
    {
        private string _BereavementLocationCodeDescription;
        private string _BereavementPlanDescription;
        private string _BereavementSourceCodeDescription;
        private string _DeceasedGenderCodeDescription;
        private string _RelationshipCodeDescription;

        public string SurvivorFullNameWithMiddleInitial
        {
            get
            {
                var name = string.Format("{0},{1}{2}",
                    string.IsNullOrWhiteSpace(SurvivorLastName) ? "" : SurvivorLastName.Trim(),
                    string.IsNullOrWhiteSpace(SurvivorFirstName) ? "" : " " + SurvivorFirstName.Trim(),
                    string.IsNullOrWhiteSpace(SurvivorMiddleInitial) ? "" : " " + SurvivorMiddleInitial.Trim() + ".");
                if (name == ",")
                {
                    name = " ";
                }

                return name;
            }
        }

        public string DeceasedFullNameWithMiddleInitial
        {
            get
            {
                var name = string.Format("{0},{1}{2}",
                    string.IsNullOrWhiteSpace(DeceasedLastName) ? "" : DeceasedLastName.Trim(),
                    string.IsNullOrWhiteSpace(DeceasedFirstName) ? "" : " " + DeceasedFirstName.Trim(),
                    string.IsNullOrWhiteSpace(DeceasedMiddleInitial) ? "" : " " + DeceasedMiddleInitial.Trim() + ".");
                if (name == ",")
                {
                    name = " ";
                }

                return name;
            }
        }

        public string DeceasedFullNameWithMiddleInitialAndMRN
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DeceasedMRN) == false)
                {
                    return DeceasedFullNameWithMiddleInitial + "  -  " + DeceasedMRN;
                }

                return DeceasedFullNameWithMiddleInitial;
            }
        }

        public string AddressFull
        {
            get
            {
                var address = string.Empty;
                var CR = char.ToString('\r');
                if (!string.IsNullOrWhiteSpace(Address1))
                {
                    address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + Address1;
                }

                if (!string.IsNullOrWhiteSpace(Address2))
                {
                    address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + Address2;
                }

                if (!string.IsNullOrWhiteSpace(CityStateZip))
                {
                    address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + CityStateZip;
                }

                return address;
            }
        }

        public string StateCodeCode => CodeLookupCache.GetCodeDescriptionFromKey(StateCode);

        public string CityStateZip => FormatHelper.FormatCityStateZip(City, StateCodeCode, ZipCode);

        public string PreferredPhoneNumber
        {
            get
            {
                string preferredPhone = null;
                var code = PreferredPhoneCode;
                if (string.IsNullOrWhiteSpace(code) == false)
                {
                    code = code.Trim().ToLower();
                    if (code == "home" && string.IsNullOrWhiteSpace(HomePhoneNumber) == false)
                    {
                        preferredPhone = "(home) " + PhoneConvert(HomePhoneNumber);
                    }
                    else if (code == "cell" && string.IsNullOrWhiteSpace(CellPhoneNumber) == false)
                    {
                        preferredPhone = "(cell) " + PhoneConvert(CellPhoneNumber);
                    }
                    else if (code == "work" && string.IsNullOrWhiteSpace(WorkPhoneNumber) == false)
                    {
                        preferredPhone = "(work) " + PhoneConvert(WorkPhoneNumber);
                        if (string.IsNullOrWhiteSpace(preferredPhone) == false)
                        {
                            preferredPhone = preferredPhone + (string.IsNullOrWhiteSpace(WorkPhoneExtension) == false
                                ? " x" + WorkPhoneExtension
                                : "");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(preferredPhone) == false)
                    {
                        return preferredPhone;
                    }
                }

                if (string.IsNullOrWhiteSpace(HomePhoneNumber) == false)
                {
                    preferredPhone = "(home) " + PhoneConvert(HomePhoneNumber);
                    if (string.IsNullOrWhiteSpace(preferredPhone) == false)
                    {
                        return preferredPhone;
                    }
                }

                if (string.IsNullOrWhiteSpace(CellPhoneNumber) == false)
                {
                    preferredPhone = "(cell) " + PhoneConvert(CellPhoneNumber);
                    if (string.IsNullOrWhiteSpace(preferredPhone) == false)
                    {
                        return preferredPhone;
                    }
                }

                if (string.IsNullOrWhiteSpace(WorkPhoneNumber) == false)
                {
                    preferredPhone = "(work) " + PhoneConvert(WorkPhoneNumber);
                    if (string.IsNullOrWhiteSpace(preferredPhone) == false)
                    {
                        preferredPhone = preferredPhone + (string.IsNullOrWhiteSpace(WorkPhoneExtension) == false
                            ? " x" + WorkPhoneExtension
                            : "");
                    }

                    if (string.IsNullOrWhiteSpace(preferredPhone) == false)
                    {
                        return preferredPhone;
                    }
                }

                return null;
            }
        }

        public string PreferredPhoneCode => CodeLookupCache.GetCodeFromKey(PreferredPhoneKey);

        public string RelationshipAndDeceasedFullNameWithMiddleInitialAndDeathDate
        {
            get
            {
                var s = RelationshipCodeDescription + " of " + DeceasedFullNameWithMiddleInitial + ", died " +
                        DeceasedDeathDateString;
                return s;
            }
        }

        public string BereavementSourceCodeDescription
        {
            get
            {
                if (_BereavementSourceCodeDescription != null)
                {
                    return _BereavementSourceCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(BereavementSourceKey);
                _BereavementSourceCodeDescription = string.IsNullOrWhiteSpace(d) ? "Unknown" : d.Trim();
                return _BereavementSourceCodeDescription;
            }
        }

        public string BereavementLocationCodeDescription
        {
            get
            {
                if (_BereavementLocationCodeDescription != null)
                {
                    return _BereavementLocationCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(BereavementLocationKey);
                _BereavementLocationCodeDescription = string.IsNullOrWhiteSpace(d) ? "Unknown" : d.Trim();
                return _BereavementLocationCodeDescription;
            }
        }

        public string RelationshipCodeDescription
        {
            get
            {
                if (_RelationshipCodeDescription != null)
                {
                    return _RelationshipCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(Relationship);
                _RelationshipCodeDescription = string.IsNullOrWhiteSpace(d) ? "Unknown relation" : d.Trim();
                return _RelationshipCodeDescription;
            }
        }

        public string DeceasedDeathDateString =>
            DeceasedDeathDate == null ? "??" : ((DateTime)DeceasedDeathDate).ToShortDateString();

        public string EnrollmentStartDateString
        {
            get
            {
                if (EnrollmentStartDate == null)
                {
                    return "Not enrolled";
                }

                if (DisenrollmentDate != null)
                {
                    return "Disenrolled";
                }

                return ((DateTime)EnrollmentStartDate).ToShortDateString();
            }
        }

        public DateTime EnrollmentStartDateSort
        {
            get
            {
                if (EnrollmentStartDate == null)
                {
                    return DateTime.MaxValue.AddDays(-1);
                }

                if (DisenrollmentDate != null)
                {
                    return DateTime.MaxValue;
                }

                return (DateTime)EnrollmentStartDate;
            }
        }

        public string MrnDashEnrollmentID =>
            DeceasedMRN + " - " + (EnrollmentID == null ? "?" : ((int)EnrollmentID).ToString());

        public string DeceasedGenderCodeDescription
        {
            get
            {
                if (_DeceasedGenderCodeDescription != null)
                {
                    return _DeceasedGenderCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(DeceasedGender);
                _DeceasedGenderCodeDescription = string.IsNullOrWhiteSpace(d) ? "Unknown" : d.Trim();
                return _DeceasedGenderCodeDescription;
            }
        }

        public SurvivorEncounterRisk TotalRecord
        {
            get
            {
                if (SurvivorEncounter == null)
                {
                    return null;
                }

                var se = SurvivorEncounter.OrderByDescending(p => p.SurvivorEncounterDateTime).FirstOrDefault();
                if (se == null || se.SurvivorEncounterRisk == null)
                {
                    return null;
                }

                return se.SurvivorEncounterRisk.Where(p => p.IsTotal).FirstOrDefault();
            }
        }

        public string RiskAssessmentButtonLabel
        {
            get
            {
                var tr = TotalRecord;
                if (tr == null)
                {
                    return "Risk Assessment - Not Done";
                }

                return "Risk Assessment - Done" + (string.IsNullOrWhiteSpace(tr.Comment) ? "" : " w/cmts");
            }
        }

        public string RiskLevelLabel
        {
            get
            {
                var tr = TotalRecord;
                if (tr == null)
                {
                    return "Risk Level: None";
                }

                var score = tr.Score == null ? "" : tr.Score.ToString().Trim() + " - ";
                return "Risk Level: " + score + (tr.RiskRangeDescription == null ? "" : tr.RiskRangeDescription);
            }
        }

        public string BereavementPlanDescription
        {
            get
            {
                if (_BereavementPlanDescription != null)
                {
                    return _BereavementPlanDescription;
                }

                _BereavementPlanDescription =
                    BereavementPlanCache.GetBereavementPlanDescriptionByKey(BereavementPlanKey);
                return _BereavementPlanDescription;
            }
        }

        public string BereavementPlanGenerateLabel => BereavementPlanKey == null ? "Generate Plan" : "Regenerate Plan";

        private string PhoneConvert(string number)
        {
            var pc = new PhoneConverter();
            var phoneObject = pc.Convert(number, null, null, null);
            if (phoneObject != null)
            {
                if (string.IsNullOrWhiteSpace(phoneObject.ToString()) == false)
                {
                    return phoneObject.ToString();
                }
            }

            return null;
        }

        partial void OnBereavementSourceKeyChanged()
        {
            _BereavementSourceCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("BereavementSourceCodeDescription");
        }

        partial void OnBereavementLocationKeyChanged()
        {
            _BereavementLocationCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("BereavementLocationCodeDescription");
        }

        partial void OnRelationshipChanged()
        {
            _RelationshipCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("RelationshipCodeDescription");
            RaisePropertyChanged("RelationshipAndDeceasedFullNameWithMiddleInitialAndDeathDate");
        }

        partial void OnDeceasedDeathDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DeceasedDeathDateString");
        }

        partial void OnDisenrollmentDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EnrollmentStartDateString");
            RaisePropertyChanged("EnrollmentStartDateSort");
        }

        partial void OnEnrollmentIDChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MrnDashEnrollmentID");
        }

        partial void OnDeceasedGenderChanged()
        {
            _DeceasedGenderCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DeceasedGenderCodeDescription");
        }

        public void RaiseRiskAssessmentChanged()
        {
            RaisePropertyChanged("RiskAssessmentButtonLabel");
            RaisePropertyChanged("RiskLevelLabel");
        }

        partial void OnRiskRangeKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("RiskAssessmentButtonLabel");
            RaisePropertyChanged("RiskLevelLabel");
        }

        partial void OnBereavementPlanKeyChanged()
        {
            _BereavementPlanDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            if (BereavementPlanKey == 0)
            {
                BereavementPlanKey = null;
            }

            RaisePropertyChanged("BereavementPlanDescription");
            RaisePropertyChanged("BereavementPlanGenerateLabel");
        }
    }

    public partial class SurvivorEncounterRisk : IEncounterRisk
    {
        private string _RiskRangeDescription;

        public string RiskRangeDescription
        {
            get
            {
                if (_RiskRangeDescription != null)
                {
                    return _RiskRangeDescription;
                }

                var rr = DynamicFormCache.GetRiskRangeByKey(RiskRangeKey.GetValueOrDefault());
                _RiskRangeDescription = (rr == null) | string.IsNullOrWhiteSpace(rr.Label) ? null : rr.Label;
                return _RiskRangeDescription;
            }
        }

        public void CopyFrom(EncounterRisk copyFrom)
        {
            // Bogus for IEncounterRisk
        }

        partial void OnRiskRangeKeyChanged()
        {
            _RiskRangeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            if (RiskRangeKey == 0)
            {
                RiskRangeKey = null;
            }

            RaisePropertyChanged("RiskRangeDescription");
        }

        public SurvivorEncounterRisk CloneMe()
        {
            return new SurvivorEncounterRisk
            {
                TenantID = TenantID,
                RiskAssessmentKey = RiskAssessmentKey,
                RiskGroupKey = RiskGroupKey,
                RiskQuestionKey = RiskQuestionKey,
                RiskRangeKey = RiskRangeKey,
                IsTotal = IsTotal,
                IsSelected = IsSelected,
                Score = Score,
                RiskForID = RiskForID,
                Comment = Comment,
                CodeLookupKey = CodeLookupKey
            };
        }
    }

    public partial class SurvivorPlanActivity
    {
        private bool _ActivityWasChanged;
        private string _EventTypeCode;
        private bool _InNewPlan;
        private bool _IsSelected;
        private List<UserProfile> _PerformedByList;
        private string _PerformedByUserName;

        private string _TimePointCode;
        private string _TimePointCodeDescription;

        public bool InNewPlan
        {
            get { return _InNewPlan; }
            set
            {
                _InNewPlan = value;
                RaisePropertyChanged("InNewPlan");
            }
        }

        public bool IsSelected
        {
            get { return _IsSelected; }
            set
            {
                _IsSelected = value;
                RaisePropertyChanged("IsSelected");
                Messenger.Default.Send(this, "SurvivorPlanActivitySelected");
            }
        }

        public bool ActivityWasChanged
        {
            get { return _ActivityWasChanged; }
            set
            {
                _ActivityWasChanged = value;
                RaisePropertyChanged("ActivityWasChanged");
            }
        }

        public string EventTypeCode
        {
            get
            {
                if (_EventTypeCode != null)
                {
                    return _EventTypeCode;
                }

                var d = CodeLookupCache.GetCodeFromKey(EventTypeKey);
                _EventTypeCode = string.IsNullOrWhiteSpace(d) ? "Unknown" : d.Trim();
                if (_EventTypeCode == "Unknown" && EventTypeKey == -1)
                {
                    _EventTypeCode = "Disenrollment";
                }

                return _EventTypeCode;
            }
        }

        public bool CanSelect => EventTypeIsEnrollment || EventTypeIsDisenrollment ? false : true;

        public bool EventTypeIsEnrollment => EventTypeCode == "Enrollment";

        public bool EventTypeIsDisenrollment => EventTypeKey == -1 || EventTypeKey == -2;

        public string ActivityBlirb
        {
            get
            {
                var ab = (ActivityDescription == null ? "Activity ??" : ActivityDescription) + " (" +
                         TimePointDescription + ")";
                return ab;
            }
        }

        private string TimePointCode
        {
            get
            {
                if (_TimePointCode != null)
                {
                    return _TimePointCode;
                }

                var d = CodeLookupCache.GetCodeFromKey(TimePointKey);
                _TimePointCode = string.IsNullOrWhiteSpace(d) ? null : d.Trim();
                return _TimePointCode;
            }
        }

        private string TimePointCodeDescription
        {
            get
            {
                if (_TimePointCodeDescription != null)
                {
                    return _TimePointCodeDescription;
                }

                var d = CodeLookupCache.GetCodeDescriptionFromKey(TimePointKey);
                _TimePointCodeDescription = string.IsNullOrWhiteSpace(d) ? "Unknown" : d.Trim();
                return _TimePointCodeDescription;
            }
        }

        private string TimePointDescription
        {
            get
            {
                if (TimePointDate == null)
                {
                    return TimePointCodeDescription;
                }

                return "on " + ((DateTime)TimePointDate).Date.ToShortDateString();
            }
        }

        public int TimePointSortTimePoint
        {
            get
            {
                // If dates happen to be the same - we want to sort by Immediate TimePoints, followed by CalendarDate TimePoints, Followed by Month TimePoints
                var code = TimePointCode;
                if (code == "I")
                {
                    return 0;
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    return 1;
                }

                return 2;
            }
        }

        public static List<string> StatusList { get; } =
            new List<string> { "Planned", "Complete", "Unable to Complete" };

        public string StatusProxy
        {
            get
            {
                if (ActivityComplete)
                {
                    return StatusList[1];
                }

                if (ActivityUnableToComplete)
                {
                    return StatusList[2];
                }

                return StatusList[0];
            }
            set
            {
                if (value == StatusList[1])
                {
                    ActivityComplete = true;
                    return;
                }

                if (value == StatusList[2])
                {
                    ActivityUnableToComplete = true;
                    return;
                }

                ActivityComplete = false;
                ActivityUnableToComplete = false;
            }
        }

        public string PerformedByUserName
        {
            get
            {
                if (_PerformedByUserName != null)
                {
                    return _PerformedByUserName;
                }

                var pbn = UserCache.Current.GetFormalNameFromUserId(PerformedBy);
                _PerformedByUserName = string.IsNullOrWhiteSpace(pbn) ? "??" : pbn;
                return _PerformedByUserName;
            }
        }

        public string ActivityStatusBlirb =>
            ActivityComplete == false && ActivityUnableToComplete == false
                ? "Planned, due on " + ((DateTime)DueDate).Date.ToShortDateString() +
                  (PlannedActivityOverdue ? " (overdue)" : "")
                : (ActivityComplete ? "Completed on " : "Unable to Complete, determined on ") +
                  (ActivityCompleteDateTime == null
                      ? "??"
                      : ((DateTime)ActivityCompleteDateTime).Date.ToShortDateString() + " by " + PerformedByUserName);

        public bool IsPlannedActivity => ActivityComplete == false && ActivityUnableToComplete == false ? true : false;

        public bool PlannedActivityOverdue
        {
            get
            {
                if (IsPlannedActivity == false || LateDate == null)
                {
                    return false;
                }

                if (DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date < ((DateTime)LateDate).Date)
                {
                    return false;
                }

                return true;
            }
        }

        public string ActivityPlannedBlirb
        {
            get
            {
                if (IsPlannedActivity == false || DueDate == null)
                {
                    return null;
                }

                return "Due on " + ((DateTime)DueDate).Date.ToShortDateString() +
                       (PlannedActivityOverdue ? " (overdue)" : "");
            }
        }

        public new bool CanDelete => IsPlannedActivity ? true : false;

        public bool CanAttachDocument => string.IsNullOrWhiteSpace(DocumentFileName);

        public bool CanOpenOrDeleteDocument => string.IsNullOrWhiteSpace(DocumentFileName) == false;

        public string OpenOrDeletehDocumentTootip => "Open or Delete document " +
                                                     (string.IsNullOrWhiteSpace(DocumentFileName)
                                                         ? "??"
                                                         : DocumentFileName);

        public List<UserProfile> PerformedByList
        {
            get
            {
                if (_PerformedByList != null)
                {
                    return _PerformedByList;
                }

                _PerformedByList = UserCache.Current.GetBereavementRoleUserProfilePlusCurrentList();
                var uppb = UserCache.Current.GetUserProfileFromUserId(PerformedBy);
                if (uppb != null && _PerformedByList.Contains(uppb) == false)
                {
                    _PerformedByList.Insert(0, uppb);
                }

                return _PerformedByList;
            }
        }

        public string ActivityCommentOrNone => string.IsNullOrWhiteSpace(ActivityComment) ? "None" : ActivityComment;

        public string SurvivorFullNameWithMiddleInitial => Survivor == null ? "??" : Survivor.SurvivorFullNameWithMiddleInitial;

        public string AddressFull => Survivor == null ? "??" : Survivor.AddressFull;

        public string PreferredPhoneNumber => Survivor == null ? "??" : Survivor.PreferredPhoneNumber;

        public string RelationshipCodeDescription => Survivor == null ? "" : Survivor.RelationshipCodeDescription;

        public string DeceasedFullNameWithMiddleInitial => Survivor == null ? "" : Survivor.DeceasedFullNameWithMiddleInitial;

        public string DeceasedDeathDateString => Survivor == null ? "" : Survivor.DeceasedDeathDateString;

        public string RelationshipAndDeceasedFullNameWithMiddleInitialAndDeathDate => Survivor == null
            ? "??"
            : Survivor.RelationshipAndDeceasedFullNameWithMiddleInitialAndDeathDate;

        public string SurvivorFirstName => Survivor == null ? "??" : Survivor.SurvivorFirstName;

        public string SurvivorLastName => Survivor == null ? "??" : Survivor.SurvivorLastName;

        public int? PatientKey => Survivor == null ? null : Survivor.PatientKey;

        public int? PatientContactKey => Survivor == null ? null : Survivor.PatientContactKey;

        public string DueDateToShort => DueDate == null ? "??" : ((DateTime)DueDate).Date.ToShortDateString();

        public string BereavementLocationCodeDescription => Survivor == null ? "??" : Survivor.BereavementLocationCodeDescription;

        partial void OnEventTypeKeyChanged()
        {
            _EventTypeCode = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EventTypeCode");
            RaisePropertyChanged("EventTypeIsEnrollment");
        }

        partial void OnTimePointKeyChanged()
        {
            _TimePointCode = null;
            _TimePointCodeDescription = null;
            if (IsDeserializing)
            {
                return;
            }

            if (TimePointKey != null)
            {
                TimePointDate = null;
            }

            RaisePropertyChanged("TimePointCode");
            RaisePropertyChanged("TimePointCodeDescription");
            RaisePropertyChanged("TimePointDescription");
            RaisePropertyChanged("TimePointSortTimePoint");
            RaisePropertyChanged("ActivityBlirb");
        }

        partial void OnTimePointDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (TimePointDate != null)
            {
                TimePointKey = null;
            }

            RaisePropertyChanged("TimePointDescription");
            RaisePropertyChanged("ActivityBlirb");
        }

        public bool IsTimePointDateOutsideScope(DateTime deathDate)
        {
            if (TimePointDate == null)
            {
                return false;
            }

            if (((DateTime)TimePointDate).Date < deathDate.Date)
            {
                return true;
            }

            if (((DateTime)TimePointDate).Date > deathDate.Date.AddMonths(13))
            {
                return true;
            }

            return false;
        }

        partial void OnDeletedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            DeletedDate = Deleted ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified) : (DateTime?)null;
            DeletedBy = Deleted ? WebContext.Current.User.MemberID : (Guid?)null;
        }

        partial void OnDueDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ActivityStatusBlirb");
            RaisePropertyChanged("ActivityPlannedBlirb");
        }

        partial void OnLateDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ActivityStatusBlirb");
            RaisePropertyChanged("ActivityPlannedBlirb");
            RaisePropertyChanged("PlannedActivityOverdue");
        }

        partial void OnActivityCompleteChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ActivityComplete)
            {
                ActivityUnableToComplete = false;
            }

            SetupCompleted();
        }

        partial void OnActivityUnableToCompleteChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ActivityUnableToComplete)
            {
                ActivityComplete = false;
            }

            SetupCompleted();
        }

        private void SetupCompleted()
        {
            if (ActivityComplete == false && ActivityUnableToComplete == false)
            {
                ActivityCompleteDateTime = null;
                PerformedBy = null;
            }
            else
            {
                if (ActivityCompleteDateTime == null)
                {
                    ActivityCompleteDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                }

                if (PerformedBy == null)
                {
                    PerformedBy = WebContext.Current.User.MemberID;
                }
            }

            RaisePropertyChanged("ActivityStatusBlirb");
            RaisePropertyChanged("ActivityPlannedBlirb");
            RaisePropertyChanged("IsPlannedActivity");
            RaisePropertyChanged("CanDelete");
            RaisePropertyChanged("PlannedActivityOverdue");
        }

        partial void OnActivityCompleteDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ActivityCompleteDateTime == DateTime.MinValue)
            {
                ActivityCompleteDateTime = null;
            }

            var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
            if (ActivityCompleteDateTime != null)
            {
                ActivityCompleteDateTime = ((DateTime)ActivityCompleteDateTime).Date;
            }

            if (ActivityCompleteDateTime == null && IsPlannedActivity == false)
            {
                ActivityCompleteDateTime = today;
            }

            if (ActivityCompleteDateTime != null && IsPlannedActivity == false &&
                (DateTime)ActivityCompleteDateTime > today)
            {
                ActivityCompleteDateTime = today;
            }

            RaisePropertyChanged("ActivityStatusBlirb");
            RaisePropertyChanged("ActivityPlannedBlirb");
        }

        partial void OnPerformedByChanged()
        {
            _PerformedByUserName = null;
            if (IsDeserializing)
            {
                return;
            }

            if (PerformedBy == null || PerformedBy == Guid.Empty || PerformedBy == Guid.NewGuid())
            {
                PerformedBy = IsPlannedActivity ? (Guid?)null : WebContext.Current.User.MemberID;
            }

            RaisePropertyChanged("ActivityStatusBlirb");
            RaisePropertyChanged("ActivityPlannedBlirb");
        }

        partial void OnDocumentFileNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CanAttachDocument");
            RaisePropertyChanged("CanOpenOrDeleteDocument");
            RaisePropertyChanged("OpenOrDeletehDocumentTootip");
        }

        partial void OnActivityCommentChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ActivityCommentOrNone");
        }
    }

    public partial class SurvivorPlanActivityDocument
    {
        partial void OnDeletedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            DeletedDate = Deleted ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified) : (DateTime?)null;
            DeletedBy = Deleted ? WebContext.Current.User.MemberID : (Guid?)null;
        }
    }
}