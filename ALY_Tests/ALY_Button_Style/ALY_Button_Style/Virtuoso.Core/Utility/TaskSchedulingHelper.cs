#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Helpers
{
    public class LookupPM
    {
        public string CodeType { get; set; }
        public string LookupKey { get; set; }
        public int DatabaseKey { get; set; }
        public bool ServiceIsAttempted { get; set; }
        public bool ServiceIsHIS { get; set; }
        public bool ServiceIsOASIS { get; set; }
        public bool ServiceIsOrderEntry { get; set; }
        public bool ServiceIsPreEval { get; set; }
        public bool ServiceIsEval { get; set; }
        public bool ServiceIsResumption { get; set; }
        public bool ServiceIsTransfer { get; set; }
        public bool ServiceIsCOTI { get; set; }
        public bool ServiceIsVerbalCOTI { get; set; }
        public bool ServiceIsHospiceF2F { get; set; }
        public bool ServiceIsHospicePhysicianEncounter { get; set; }
        public bool ServiceIsHospiceElectionAddendum { get; set; }
        public bool ServiceIsPlanOfCare { get; set; }
        public bool ServiceIsTeamMeeting { get; set; }
        public bool ServiceIsDischarge { get; set; }
        public bool ServiceIsVisit { get; set; }
        public bool ServiceIsFinancialUseOnly { get; set; }
        public bool ServiceIsSchedulable { get; set; }
        public string Description { get; set; }
        public int? Duration { get; set; }
        public int? DisciplineKey { get; set; }
        public string HCFACode { get; set; }
    }

    public static class TaskSchedulingHelper
    {
        public static bool IsSchedulable(int serviceTypeKey)
        {
            ServiceType serviceType = ServiceTypeCache.GetServiceTypeFromKey(serviceTypeKey);
            if (serviceType == null)
            {
                return false;
            }

            return serviceType.IsSchedulable;
        }

        public static bool IsFinancialUseOnly(int serviceTypeKey)
        {
            ServiceType serviceType = ServiceTypeCache.GetServiceTypeFromKey(serviceTypeKey);
            if (serviceType == null)
            {
                return false;
            }

            return serviceType.FinancialUseOnly;
        }

        public static bool IsAttempted(int serviceTypeKey)
        {
            ServiceType serviceType = ServiceTypeCache.GetServiceTypeFromKey(serviceTypeKey);
            if (serviceType == null)
            {
                return false;
            }

            return serviceType.IsAttempted;
        }

        public static bool IsHIS(int serviceTypeKey)
        {
            ServiceType serviceType = ServiceTypeCache.GetServiceTypeFromKey(serviceTypeKey);
            if (serviceType == null)
            {
                return false;
            }

            return serviceType.IsHIS;
        }

        public static bool IsOASIS(int serviceTypeKey)
        {
            ServiceType serviceType = ServiceTypeCache.GetServiceTypeFromKey(serviceTypeKey);
            if (serviceType == null)
            {
                return false;
            }

            return serviceType.IsOasis;
        }

        public static bool IsOrderEntry(int serviceTypeKey)
        {
            ServiceType serviceType = ServiceTypeCache.GetServiceTypeFromKey(serviceTypeKey);
            if (serviceType == null)
            {
                return false;
            }

            return serviceType.IsOrderEntry;
        }

        public static bool IsPreEval(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsPreEval(form_key.Value);
        }

        public static bool IsEval(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsEval(form_key.Value);
        }

        public static bool IsResumption(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsResumption(form_key.Value);
        }

        public static bool IsDischarge(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsDischarge(form_key.Value);
        }

        public static bool IsTransfer(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsTransfer(form_key.Value);
        }

        public static bool IsCOTI(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsCOTI(form_key.Value);
        }

        public static bool IsVerbalCOTI(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsVerbalCOTI(form_key.Value);
        }

        public static bool IsHospiceF2F(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsHospiceF2F(form_key.Value);
        }

        public static bool IsHospicePhysicianEncounter(int serviceTypeKey)
        {
            int? form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            Form f = DynamicFormCache.GetFormByKey((int)form_key);
            if (f == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(f.Description))
            {
                return false;
            }

            if (f.Description.Trim().ToLower() != "hospice physician encounter")
            {
                return false;
            }

            return true;
        }

        public static bool IsHospiceElectionAddendum(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsHospiceElectionAddendum(form_key.Value);
        }

        public static bool IsPlanOfCare(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsPlanOfCare(form_key.Value);
        }

        public static bool IsTeamMeeting(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsTeamMeeting(form_key.Value);
        }

        public static bool IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry(int serviceTypeKey)
        {
            return (IsPlanOfCare(serviceTypeKey) || IsTeamMeeting(serviceTypeKey));
        }

        public static bool IsVisit(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            if (DynamicFormCache.IsVisitTeleMonitoring(form_key.Value))
            {
                return false;
            }

            return DynamicFormCache.IsVisit(form_key.Value);
        }

        public static bool IsWOCN(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsWOCN(form_key.Value);
        }

        public static bool IsAuthorizationRequest(int serviceTypeKey)
        {
            var form_key = ServiceTypeCache.GetFormKey(serviceTypeKey);
            if (form_key == null)
            {
                return false;
            }

            return DynamicFormCache.IsAuthorizationRequest(form_key.Value);
        }

        public static bool ScheduleInIsUseFormAllowed(int ServiceTypeKey)
        {
            return (IsTransfer(ServiceTypeKey) || IsDischarge(ServiceTypeKey) || IsPlanOfCare(ServiceTypeKey) ||
                    IsTeamMeeting(ServiceTypeKey) || IsWOCN(ServiceTypeKey) || IsAuthorizationRequest(ServiceTypeKey))
                ? true
                : false; //Note - assumes we CANNOT create PreEval tasks when scheduler is in use
        }

        public static bool CanCVScheduleTask(IEnumerable<AdmissionDisciplineFrequency> adfParm, int ServiceTypeKeyParm,
            DateTime TaskDateParm, bool IsSchedulable)
        {
            // This method assumes a Foreign scheduling system is being used.
            if (adfParm == null)
            {
                return false;
            }

            // Does this patient have any PRN orders?
            bool canSchedule = false;
            var pRN = adfParm.Where(adf => adf.IsPRN
                                           && (TaskDateParm == null || TaskDateParm >= adf.StartDate ||
                                               adf.StartDate == null)
                                           && (TaskDateParm == null || TaskDateParm <= adf.EndDate ||
                                               adf.EndDate == null)).ToList();
            // Only need to check PRN's for forms that are marked as not allowed.  PRN's override the scheduling in use and form allowed logic
            if (ScheduleInIsUseFormAllowed(ServiceTypeKeyParm))
            {
                canSchedule = true;
            }
            else if (pRN != null && pRN.Any() && IsVisit(ServiceTypeKeyParm))
            {
                if (pRN.Where(p => p.DisciplineKey == ServiceTypeCache.GetDisciplineKey(ServiceTypeKeyParm)).Any())
                {
                    canSchedule = true;
                }
            }

            if (!canSchedule)
            {
                canSchedule = !IsSchedulable; // Filter out Is Schedulable == true Service Types
            }

            return canSchedule;
        }

        public static bool UserCanPerformServiceType(ServiceType st, UserProfile up)
        {
            if (st == null || up == null)
            {
                return true;
            }

            bool CanPerform = false;
            if (up.DisciplineInUserProfile != null)
            {
                // first verify the disciplin exists in the users profile
                var diup = up.DisciplineInUserProfile.Where(d => d.DisciplineKey == st.DisciplineKey).FirstOrDefault();
                if (diup != null)
                {
                    CanPerform = true;
                }

                // now check for supervision specific validation
                if (diup != null && CanPerform)
                {
                    if (diup.IsAssistant && !st.IsAssistant)
                    {
                        CanPerform = false;
                    }
                }

                return CanPerform;
            }

            return CanPerform;
        }

        public static bool UserCanSupervizeServiceType(int disciplineKey, UserProfile up)
        {
            if (up == null)
            {
                return false;
            }

            bool CanPerform = false;
            if (up.DisciplineInUserProfile != null)
            {
                // first verify the disciplin exists in the users profile
                var diup = up.DisciplineInUserProfile.Where(d => d.DisciplineKey == disciplineKey).FirstOrDefault();
                if (diup != null)
                {
                    CanPerform = true;
                }

                // now check for supervision specific validation
                if (diup != null && CanPerform)
                {
                    if (!diup.IsSupervisor)
                    {
                        CanPerform = false;
                    }
                }

                return CanPerform;
            }

            return CanPerform;
        }

        public static bool UserIsAssistantOfServiceType(int DisciplineKey, UserProfile up)
        {
            if (up == null)
            {
                return true;
            }

            bool IsAssist = false;
            if (up.DisciplineInUserProfile != null)
            {
                // first verify the disciplin exists in the users profile
                var diup = up.DisciplineInUserProfile.Where(d => d.DisciplineKey == DisciplineKey).FirstOrDefault();
                if (diup != null)
                {
                    IsAssist = diup.IsAssistant;
                }
            }

            return IsAssist;
        }
    }
}