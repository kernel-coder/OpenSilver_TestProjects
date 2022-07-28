#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class UserProfile
    {
        public string NPI => PhysicianCache.Current.GetPhysicianNPIFromKey(PhysicianKey);

        public string NPIWithPhysicianName
        {
            get
            {
                var p = PhysicianCache.Current.GetPhysicianFromKey(PhysicianKey);
                if (p != null)
                {
                    if (string.IsNullOrWhiteSpace(NPI))
                    {
                        return string.Empty;
                    }

                    var _physician_name = p.FullName;
                    return string.Format("{0} ({1})", NPI, _physician_name);
                }

                return string.Empty;
            }
        }

        // Used to display * in SearchResultsView
        public string IsInactiveIndicator
        {
            get
            {
                if (Inactive)
                {
                    return "*";
                }

                return string.Empty;
            }
        }

        public int RoleCount
        {
            get
            {
                var count = 0;
                foreach (var r in UserProfileInRole)
                    if (!r.RoleEnd.HasValue)
                    {
                        count++;
                    }

                return count;
            }
        }

        public bool IsSurveyor => HasRole("Surveyor");

        public List<int> ServiceLineGroupIdsUserCanVisit
        {
            get
            {
                List<int> canvisitLst = null;

                var canvisitStr = ServiceLineGroupIds_CanVisit;
                if (string.IsNullOrEmpty(canvisitStr))
                {
                    canvisitLst = new List<int>();
                }
                else
                {
                    canvisitLst = canvisitStr.Split(',').Select(i => int.Parse(i)).ToList();
                }

                return canvisitLst;
            }
        }
        
        public List<int> ServiceLineGroupIdsUserIsOwnerOrOverride
        {
            get
            {
                List<int> oversiteLst = null;
                List<int> ownerLst = null;

                var oversiteStr = ServiceLineGroupIds_Oversite;
                if (string.IsNullOrEmpty(oversiteStr))
                {
                    oversiteLst = new List<int>();
                }
                else
                {
                    oversiteLst = oversiteStr.Split(',').Select(i => int.Parse(i)).ToList();
                }

                var ownerStr = ServiceLineGroupIds_Owner;
                if (string.IsNullOrEmpty(ownerStr))
                {
                    ownerLst = new List<int>();
                }
                else
                {
                    ownerLst = ownerStr.Split(',').Select(i => int.Parse(i)).ToList();
                }

                var ownerOrOverride = oversiteLst.Union(ownerLst).Distinct();

                return ownerOrOverride.ToList();
            }
        }

        public List<int> ServiceLineGroupIdsUserCanSee
        {
            get
            {
                var s = ServiceLineGroupIdsUserCanSeeAsStr;
                if (s == null || s == string.Empty)
                {
                    return new List<int>();
                }

                return ServiceLineGroupIdsUserCanSeeAsStr.Split(',').Select(i => int.Parse(i)).ToList();
            }
        }

        public List<UserProfileServiceLine> MyUserProfileServiceLines
        {
            get
            {
                List<UserProfileServiceLine> result;
                if (UserProfileServiceLine == null)
                {
                    result = new List<UserProfileServiceLine>();
                }
                else
                {
                    result = UserProfileServiceLine.ToList();
                }

                var today = DateTime.Today;
                var filtered = result.Where(w => w.IsEffectiveOnDate(today) && w.UserHasRights).ToList();
                return filtered;
            }
        }

        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(FullName))
                {
                    return string.Format("User {0}", FullName.Trim());
                }

                return IsNew ? "New User" : "Edit User";
            }
        }

        public string FullName => FormatHelper.FormatName(LastName, FirstName, MiddleName);

        public string FullNameWithSuffix =>
            string.IsNullOrWhiteSpace(Suffix) ? FullName : FullName + " " + Suffix.Trim();

        public string FullNameNoComma => FullName.TrimEnd(Convert.ToChar(","));

        public Guid? NullableUserId
        {
            get
            {
                if (UserId == Guid.Empty)
                {
                    return null;
                }

                return UserId;
            }
        }

        public string FormalName
        {
            get
            {
                var name = string.Format("{0} {1} {2}", FirstName.Trim(), LastName.Trim(),
                    !string.IsNullOrWhiteSpace(Suffix) ? ", " + Suffix.Trim() : "").Trim();
                if (name == "")
                {
                    name = " ";
                }

                return name;
            }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(FriendlyName))
                {
                    return FriendlyName;
                }

                return FullName;
            }
        }

        public bool UserIsASupervisor
        {
            get { return DisciplineInUserProfile == null ? false : DisciplineInUserProfile.Any(d => d.IsSupervisor); }
        }

        public bool DiagnosisCodersPostSignature =>
            TenantSettingsCache.Current.TenantSettingDiagnosisCodersPostSignature;

        public bool HISCoordinatorCanEdit => TenantSettingsCache.Current.TenantSettingHISCoordinatorCanEdit;

        public bool OASISCoordinatorCanEdit => TenantSettingsCache.Current.TenantSettingOASISCoordinatorCanEdit;

        public UserMobileAccessDetail LastUserMobileAccessDetail => UserMobileAccessDetail;

        public bool ShowModuleAccessedDate
        {
            get
            {
                var value = LastUserMobileAccessDetail;
                return value != null && value.LastAccessDateTime.HasValue;
            }
        }

        public string ModuleAccessedDate
        {
            get
            {
                var value = LastUserMobileAccessDetail;
                if (value != null && value.LastAccessDateTime.HasValue)
                {
                    return value.LastAccessDateTime.Value.ToString("MM/dd/yyyy");
                }

                return string.Empty;
            }
        }

        public string ModuleAccessName
        {
            get
            {
                var value = LastUserMobileAccessDetail;
                if (value != null)
                {
                    return value.ModuleName;
                }

                return string.Empty;
            }
        }

        public bool IsModuleAccessPurchased
        {
            get
            {
                var value = LastUserMobileAccessDetail;
                if (value != null)
                {
                    return value.HasTenantModuleAccess;
                }

                return false;
            }
        }

        public bool ShowModuleInviteDate
        {
            get
            {
                var value = LastUserMobileAccessDetail;
                return value != null && value.InviteSentDateTime.HasValue;
            }
        }

        public string ModuleInviteDate
        {
            get
            {
                var value = LastUserMobileAccessDetail;
                if (value != null && value.InviteSentDateTime.HasValue)
                {
                    return value.InviteSentDateTime.Value.ToString("MM/dd/yyyy");
                }

                return string.Empty;
            }
        }

        public string SendInviteText
        {
            get
            {
                var value = LastUserMobileAccessDetail;
                if (value != null && value.InviteSentDateTime.HasValue)
                {
                    return value.InviteSentDateTime.Value.ToString("MM/dd/yyyy");
                }

                return string.Empty;
            }
        }

        public bool ShowModuleConfirmDate
        {
            get
            {
                var value = LastUserMobileAccessDetail;
                return value != null && value.EmailConfirmedDateTime.HasValue;
            }
        }

        public string ModuleConfirmDate
        {
            get
            {
                var value = LastUserMobileAccessDetail;
                if (value != null && value.EmailConfirmedDateTime.HasValue)
                {
                    return value.EmailConfirmedDateTime.Value.ToString("MM/dd/yyyy");
                }

                return string.Empty;
            }
        }

        partial void OnInactiveChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Inactive)
            {
                InactiveDate = DateTime.UtcNow;
            }
            else
            {
                InactiveDate = null;
            }
        }

        partial void OnCrescendoConnectUserChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (CrescendoConnectUser == false)
            {
                CrescendoConnectPasswordReset = false;
            }
        }

        partial void OnUsingOrderEntryReviewersChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (UsingOrderEntryReviewers == false)
            {
                ServiceOrdersHeldUntilReviewed = false;
            }
        }

        partial void OnBirthDateChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            RaisePropertyChanged("UserAge");
        }

        partial void OnPhysicianKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("NPI");
            RaisePropertyChanged("NPIWithPhysicianName");
        }


        public bool HasRole(string RoleName)
        {
            foreach (var r in UserProfileInRole)
                if (!r.RoleEnd.HasValue)
                {
                    var roleName = RoleCache.GetRoleFromKey(r.RoleKey).RoleName;
                    if (roleName == RoleName)
                    {
                        return true;
                    }
                }

            return false;
        }

        public bool IsOversiteOrCanVisitOrOwnerInHeirachy(List<ServiceLineGrouping> slgList)
        {
            if (slgList == null)
            {
                return false;
            }

            var ids = ServiceLineGroupIdsUserCanSee;
            foreach (var slg in slgList)
                if (ids.Contains(slg.MyServiceLineGroupingKey))
                {
                    return true;
                }

            return false;
        }

        public bool IsOversiteOrCanVisitOrOwnerInHeirachy(int ServiceLineGroupingKey)
        {
            var ids = ServiceLineGroupIdsUserCanSee;
            return ids.Contains(ServiceLineGroupingKey);
        }


        public bool IsOversiteOrCanVisitOrOwnerInHeirachy(ServiceLineGrouping slg)
        {
            if (slg == null)
            {
                return false;
            }

            // Get the basic id list if it's not set on the user.
            return ServiceLineGroupIdsUserCanSee.Contains(slg.MyServiceLineGroupingKey);
        }

        //NOTE: this method overridden for control autoCompleteCombo
        public override string ToString()
        {
            return FullName.TrimEnd(Convert.ToChar(","));
        }

        partial void OnLastNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullName");
            RaisePropertyChanged("FormalName");
            RaisePropertyChanged("TabHeader");
        }

        partial void OnFirstNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullName");
            RaisePropertyChanged("FormalName");
            RaisePropertyChanged("TabHeader");
        }

        partial void OnMiddleNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullName");
            RaisePropertyChanged("FormalName");
            RaisePropertyChanged("TabHeader");
        }

        partial void OnSuffixChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FormalName");
        }

        public bool UserCanSuperviseDiscipline(int DisciplineKey)
        {
            return DisciplineInUserProfile == null
                ? false
                : DisciplineInUserProfile.Any(d => d.DisciplineKey == DisciplineKey && d.IsSupervisor);
        }

        public void TriggerUserMobileChanges()
        {
            RaisePropertyChanged("ShowModuleAccessedDate");
            RaisePropertyChanged("ModuleAccessedDate");
            RaisePropertyChanged("ModuleAccessName");
            RaisePropertyChanged("IsModuleAccessPurchased");
            RaisePropertyChanged("ShowModuleInviteDate");
            RaisePropertyChanged("ModuleInviteDate");
            RaisePropertyChanged("SendInviteText");
            RaisePropertyChanged("ShowModuleConfirmDate");
            RaisePropertyChanged("ModuleConfirmDate");
        }
    }

    public partial class UserProfileServiceLine
    {
        public bool UserHasRights => Oversite;

        public int MyServiceLineKey => ServiceLineKey;

        partial void OnOversiteChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Oversite == false)
            {
                return;
            }

            if (UserProfile == null)
            {
                return;
            }

            if (UserProfile.UserProfileGroup == null)
            {
                return;
            }

            UserProfile.UserProfileGroup.Where(u => u.EndDate == null && u.Oversite).ToList()
                .ForEach(upg => UncheckOversiteForChildrenOfThisServiceLine(upg));
        }

        private void UncheckOversiteForChildrenOfThisServiceLine(UserProfileGroup upg)
        {
            var slg = ServiceLineCache.GetServiceLineGroupingFromKey(upg.ServiceLineGroupingKey);
            if (slg == null)
            {
                return;
            }

            if (slg.ServiceLineKey == ServiceLineKey)
            {
                upg.Oversite = false;
            }
        }

        public bool IsEffectiveOnDate(DateTime date)
        {
            return (StartDate == null || StartDate != null && StartDate <= date) &&
                   (EndDate == null || EndDate != null && EndDate > date);
        }
    }

    public partial class UserProfileGroup
    {
        public ServiceLineGrouping MyServiceLineGrouping =>
            ServiceLineCache.GetServiceLineGroupingFromKey(MyServiceLineGroupingKey);

        public int MyServiceLineGroupingKey => ServiceLineGroupingKey;

        public bool UserHasRights
        {
            get
            {
                var result = CanVisit || Oversite || Owner;
                return result;
            }
        }

        public string ServiceLineGroupingDescription =>
            ServiceLineCache.GetServiceLineGroupingDescriptionFromKey(ServiceLineGroupingKey);

        public bool IsEffectiveOnDate(DateTime date)
        {
            var startDate = StartDate;
            var endDate = EndDate;
            var result = (startDate == null || startDate != null && startDate <= date) &&
                         (endDate == null || endDate != null && endDate > date);
            return result;
        }

        partial void OnOversiteChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Oversite == false)
            {
                return;
            }

            UncheckOversiteForChildrenOfThisServiceLineGrouping();
        }

        private void UncheckOversiteForChildrenOfThisServiceLineGrouping()
        {
            var slg = ServiceLineCache.GetServiceLineGroupingFromKey(ServiceLineGroupingKey);
            if (slg == null)
            {
                return;
            }

            var slgList = slg.AllChildServiceLineGroupingList(true);
            if (slgList == null)
            {
                return;
            }

            if (UserProfile == null)
            {
                return;
            }

            if (UserProfile.UserProfileGroup == null)
            {
                return;
            }

            foreach (var upg in UserProfile.UserProfileGroup.Where(u => u.EndDate == null).ToList())
                if (slgList.Any(s => s.ServiceLineGroupingKey == upg.ServiceLineGroupingKey))
                {
                    upg.CascadeFalseOversite();
                }
        }

        private void CascadeFalseOversite()
        {
            Oversite = false;
            UncheckOversiteForChildrenOfThisServiceLineGrouping();
        }
    }

    public partial class UserProfileAlternateID
    {
        public string DropdownText => Issuer + (string.IsNullOrWhiteSpace(Issuer) ? "" : " : ") + TypeCode + " - " +
                                      Identifier + (IsInactiveBindTarget ? " - (inactive)" : "");

        public bool IsInactiveBindTarget
        {
            get { return InactiveDateTime.HasValue; }
            set
            {
                if (value)
                {
                    if (!InactiveDateTime.HasValue)
                    {
                        InactiveDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                    }
                }
                else
                {
                    InactiveDateTime = null;
                    RaisePropertyChanged("DropdownText");
                }
            }
        }

        partial void OnIssuerChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DropdownText");
        }

        partial void OnTypeCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DropdownText");
        }

        partial void OnIdentifierChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DropdownText");
        }

        partial void OnInactiveDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DropdownText");
        }
    }

    public partial class UserProfileAdmission
    {
        private string _AdmissionID;
        private string _MRN;

        public string MRN
        {
            get { return _MRN; }
            set
            {
                _MRN = value;
                RaisePropertyChanged("MRN");
            }
        }

        public string AdmissionID
        {
            get { return _AdmissionID; }
            set
            {
                _AdmissionID = value;
                RaisePropertyChanged("AdmissionID");
            }
        }
    }

    public partial class DisciplineInUserProfile
    {
        public string IsAssistantLabel
        {
            get
            {
                var supSvcType = Discipline != null
                    ? Discipline.SupervisedServiceTypeLabel
                    : DisciplineCache.GetDisciplineFromKey(DisciplineKey).SupervisedServiceTypeLabel;
                var label = string.Format("Is {0}", supSvcType);
                return label;
            }
        }

        public string CanSuperviseLabel
        {
            get
            {
                var supSvcType = Discipline != null
                    ? Discipline.SupervisedServiceTypeLabel
                    : DisciplineCache.GetDisciplineFromKey(DisciplineKey).SupervisedServiceTypeLabel;
                var label = string.Format("Can Supervise {0}", supSvcType);
                return label;
            }
        }

        public bool DisciplineSupportsSupervision => Discipline != null
            ? Discipline.SupportsAssistants
            : DisciplineCache.GetDisciplineFromKey(DisciplineKey).SupportsAssistants;

        //HCFACode == "P", Code == "PHYS" -> Hospice Physician Services 
        public bool DisciplineIsPhysicianService =>
            Discipline != null
                ? Discipline.HCFACode.Equals("P")
                : DisciplineCache.GetDisciplineFromKey(DisciplineKey).HCFACode.Equals("P");

        partial void OnIsAssistantChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            if (IsAssistant)
            {
                IsSupervisor = false;
            }
        }

        partial void OnIsSupervisorChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            if (IsSupervisor)
            {
                IsAssistant = false;
            }
        }
    }
}