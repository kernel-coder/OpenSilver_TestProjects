using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Virtuoso.Server.Data
{
    public enum EncounterStatusType
    {
        None = 0,
        Edit = 1,
        CoderReview = 2,
        CoderReviewEdit = 3,
        OASISReview = 4,
        OASISReviewEdit = 5,
        OASISReviewEditRR = 6,
        Completed = 7,
        POCOrderReview = 8,
        CoderReviewEditRR = 9,
        // the 3 HIS status (HISReview,HISISReviewEdit,HISISReviewEditRR) are NOT persisted 
        // - they are only used to tell EncounterStatusMark user control what to display
        // - they are persisted as OASISReview,OASISReviewEdit,OASISISReviewEditRR respectively
        HISReview = 10,
        HISReviewEdit = 11,
        HISReviewEditRR = 12,
    };
    
    public enum EncounterReviewType
    {
        KeepInReview = 0,
        ReadyForCoderReviewPreSign = 1,
        PassedCoderReviewToReEdit = 2,
        ReadyForOASISReview = 3,
        PassedOASISReview = 4,
        FailedOASISReview = 5,
        FailedOASISReviewRR = 6,
        ReadyForOASISReReview = 7,
        NoOASISReReview = 8,
        SectionReview = 9,
        ReleaseNoOASISReview = 10,
        ReadyForCoderReviewPostSign = 11,
        PassedCoderReviewToOASISReview = 12,
        PassedCoderReviewToComplete = 13,
        NoOASISReReviewAgree = 14
    };

    public partial class Encounter
    {
        public DateTimeOffset? EncounterStartDateAndTime
        {
            get
            {
                if (EncounterStartDate.HasValue)
                {
                    var ret = EncounterStartDate;
                    if (EncounterStartTime.HasValue)
                        ret = ret.Value.Add(EncounterStartTime.Value.TimeOfDay);
                    return ret;
                }
                else
                    return null;
            }
        }

        public DateTimeOffset? EncounterEndDateAndTime
        {
            get
            {
                if (EncounterEndDate.HasValue)
                {
                    var ret = EncounterEndDate;
                    if (EncounterEndTime.HasValue)
                        ret = ret.Value.Add(EncounterEndTime.Value.TimeOfDay);
                    return ret;
                }
                else
                    return null;
            }
        }
    }
    
    public partial class EncounterOasis
    {
        public bool CheckBXOnHolds(List<EncounterOasis> BXOnHoldsList)
        {
            // return true if the survey passed should not be included in transmissiion because there are other (prior) surveys 
            // for this admission that are on-hold, otherwise return false 
            if (BXOnHoldsList == null) return false;
            if (BXOnHoldsList.Any() == false) return false;
            if (Encounter == null) return false;
            List<EncounterOasis> OnHoldList = BXOnHoldsList.Where(p => ((p.Encounter.AdmissionKey == Encounter.AdmissionKey) && (p.M0090 <= M0090))).ToList();
            if (OnHoldList == null) return false;
            if (OnHoldList.Any() == false) return false;
            // rfa check
            if (RFA == "01") // 1 -Start of care—further visits planned 
            {
                return false; //No prior onhold surveys hold up a SOC
            }
            else if (RFA == "03") // Resumption of care (after inpatient stay)
            {
                EncounterOasis eo = OnHoldList.Where(p => ((p.RFA == "01") || (p.RFA == "03") || (p.RFA == "04") || (p.RFA == "05") || (p.RFA == "06") || (p.RFA == "07"))).FirstOrDefault();
                return (eo != null) ? true : false; //Any prior except a discharge holds up a Resumption
            }
            if (RFA == "04" || RFA == "05") // Recertification (follow-up) reassessment  or Other follow-up
            {
                EncounterOasis eo = OnHoldList.Where(p => ((p.RFA == "01") || (p.RFA == "03") || (p.RFA == "04") || (p.RFA == "05") || (p.RFA == "06") || (p.RFA == "07"))).FirstOrDefault();
                return (eo != null) ? true : false; //Any prior except a discharge holds up a Recertification
            }
            if (RFA == "06" || RFA == "07") //Transferred to an inpatient facility—patient not discharged from agency  or  Transferred to an inpatient facility—patient discharged from agency
            {
                EncounterOasis eo = OnHoldList.Where(p => ((p.RFA == "01") || (p.RFA == "03") || (p.RFA == "04") || (p.RFA == "05") || (p.RFA == "06") || (p.RFA == "07"))).FirstOrDefault();
                return (eo != null) ? true : false; //Any prior except a discharge holds up a Transfer
            }
            if (RFA == "08" || RFA == "09") //Death at home  or Discharge from agency
            {
                return true;  //Any prior onhold surveys hold up a Discharge
            }
            else
                return true; // unknown RFA - keep on hold
        }
    }

    public partial class EncounterOasisWithoutC0_View
    {

        public bool CheckBXOnHolds(List<EncounterOasisWithoutC0_View> BXOnHoldsList)
        {
            // return true if the survey passed should not be included in transmissiion because there are other (prior) surveys 
            // for this admission that are on-hold, otherwise return false 
            if (BXOnHoldsList == null) return false;
            if (BXOnHoldsList.Any() == false) return false;
            if (Encounter == null) return false;
            List<EncounterOasisWithoutC0_View> OnHoldList = BXOnHoldsList.Where(p => ((p.Encounter.AdmissionKey == Encounter.AdmissionKey) && (p.M0090 <= M0090))).ToList();
            if (OnHoldList == null) return false;
            if (OnHoldList.Any() == false) return false;
            // rfa check
            if (RFA == "01") // 1 -Start of care—further visits planned 
            {
                return false; //No prior onhold surveys hold up a SOC
            }
            else if (RFA == "03") // Resumption of care (after inpatient stay)
            {
                EncounterOasisWithoutC0_View eo = OnHoldList.Where(p => ((p.RFA == "01") || (p.RFA == "03") || (p.RFA == "04") || (p.RFA == "05") || (p.RFA == "06") || (p.RFA == "07"))).FirstOrDefault();
                return (eo != null) ? true : false; //Any prior except a discharge holds up a Resumption
            }
            if (RFA == "04" || RFA == "05") // Recertification (follow-up) reassessment  or Other follow-up
            {
                EncounterOasisWithoutC0_View eo = OnHoldList.Where(p => ((p.RFA == "01") || (p.RFA == "03") || (p.RFA == "04") || (p.RFA == "05") || (p.RFA == "06") || (p.RFA == "07"))).FirstOrDefault();
                return (eo != null) ? true : false; //Any prior except a discharge holds up a Recertification
            }
            if (RFA == "06" || RFA == "07") //Transferred to an inpatient facility—patient not discharged from agency  or  Transferred to an inpatient facility—patient discharged from agency
            {
                EncounterOasisWithoutC0_View eo = OnHoldList.Where(p => ((p.RFA == "01") || (p.RFA == "03") || (p.RFA == "04") || (p.RFA == "05") || (p.RFA == "06") || (p.RFA == "07"))).FirstOrDefault();
                return (eo != null) ? true : false; //Any prior except a discharge holds up a Transfer
            }
            if (RFA == "08" || RFA == "09") //Death at home  or Discharge from agency
            {
                return true;  //Any prior onhold surveys hold up a Discharge
            }
            else
                return true; // unknown RFA - keep on hold
        }
    }
}
