#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Utility;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class ServiceLineGrouping
    {
        public ServiceLine GetServiceLine
        {
            get
            {
                if (ServiceLine != null)
                {
                    return ServiceLine;
                }

                return null;
            }
        }

        public int MyServiceLineGroupingKey => ServiceLineGroupingKey;

        public int MyServiceLineKey => ServiceLineKey;

        public string MyName => Name;

        public bool DiagnosisCodersPostSignature =>
            TenantSettingsCache.Current.TenantSettingDiagnosisCodersPostSignature;

        public bool HISCoordinatorCanEdit => TenantSettingsCache.Current.TenantSettingHISCoordinatorCanEdit;

        public bool OASISCoordinatorCanEdit => TenantSettingsCache.Current.TenantSettingOASISCoordinatorCanEdit;

        public ServiceLineGroupHeader SLGHeader
        {
            get { return ServiceLineGroupHeader; }
            set { ServiceLineGroupHeader = value; }
        }

        public string ServiceLineGroupingDescription =>
            ServiceLineCache.GetServiceLineGroupingDescriptionFromKey(ServiceLineGroupingKey);

        public string ServiceLineGroupNameWithInactive => MyName + (Inactive ? " (Inactive)" : "");

        public string LabelAndName
        {
            get
            {
                var slgh = ServiceLineCache.GetServiceLineGroupHeaderFromKey(ServiceLineGroupHeaderKey);
                return slgh == null ? MyName : slgh.GroupHeaderLabel + " " + MyName;
            }
        }

        public string GroupHeaderDescription
        {
            get
            {
                var HeaderDscr = "<UNKNOWN>";
                if (ServiceLineGroupHeader != null)
                {
                    HeaderDscr = ServiceLineGroupHeader.GroupHeaderLabel;
                }
                else if (ServiceLineGroupHeaderKey != null)
                {
                    var gh = ServiceLineCache.GetServiceLineGroupHeaderFromKey((int)ServiceLineGroupHeaderKey);
                    if (gh != null)
                    {
                        HeaderDscr = gh.GroupHeaderLabel;
                    }
                }

                return HeaderDscr;
            }
        }

        public string AttributeSectionLabel => GroupHeaderDescription + " Attributes";

        public string MemberSectionLabel => GroupHeaderDescription + " Members";

        public int? ServiceLineGroupHeaderSequenceNumber
        {
            get
            {
                if (ServiceLineGroupHeader != null)
                {
                    return ServiceLineGroupHeader.SequenceNumber;
                }

                var key = ServiceLineGroupHeaderKey == null ? 0 : (int)ServiceLineGroupHeaderKey;
                var gh = ServiceLineCache.GetServiceLineGroupHeaderFromKey(key);
                return gh?.SequenceNumber;
            }
        }

        public bool IsGroupingLevel0
        {
            get
            {
                var isLevel0 = false;

                if (ServiceLineGroupHeaderSequenceNumber != null)
                {
                    if (ServiceLineGroupHeaderSequenceNumber == 0)
                    {
                        isLevel0 = true;
                    }
                }

                return isLevel0;
            }
        }

        public bool IsCMSServiceLine
        {
            get
            {
                if (ServiceLine == null)
                {
                    return false;
                }

                return IsHospiceServiceLine || IsOasisServiceLine;
            }
        }

        public bool IsHospiceServiceLine
        {
            get
            {
                if (ServiceLine == null)
                {
                    return false;
                }

                return ServiceLine.IsHospiceServiceLine;
            }
        }

        public bool IsOasisServiceLine
        {
            get
            {
                if (ServiceLine == null)
                {
                    return false;
                }

                return ServiceLine.OasisServiceLine;
            }
        }

        public bool IsOasisHeaderRequired
        {
            get
            {
                if (IsCMSServiceLine == false)
                {
                    return false;
                }

                if (ServiceLineGroupHeader == null)
                {
                    return false;
                }

                return ServiceLineGroupHeader.SequenceNumber == 0 ? true : false;
            }
        }

        public List<OasisHeader> OasisHeaderList => OasisHeaderCache.GetActiveOasisHeadersPlusMe(OasisHeaderKey, true);

        public bool AttributeSectionVisible =>
            CanEditTeamMeetingSchedule
            || CanEditHasMedicalDirector
            || CanEditHasNursePractitioner
            || CanEditServiceTypesAssociated
            //|| CanEditCensusTractDependency 
            || CanEditIsTeam;

        public bool CanEditTeamMeetingSchedule
        {
            get
            {
                if (ServiceLine == null)
                {
                    return false;
                }

                if (ServiceLine.IsHospiceServiceLine == false)
                {
                    return false;
                }

                return ServiceLine.ServiceLineGroupHeader == null
                    ? false
                    : !ServiceLine.ServiceLineGroupHeader.Any(slgh =>
                        slgh.ScheduleTeamMeeting && slgh.ServiceLineGroupHeaderKey != ServiceLineGroupHeaderKey);
            }
        }

        public bool CanEditHasMedicalDirector
        {
            get
            {
                if (ServiceLine == null)
                {
                    return false;
                }

                if (ServiceLine.IsHospiceServiceLine == false)
                {
                    return false;
                }

                return ServiceLine.ServiceLineGroupHeader == null
                    ? false
                    : !ServiceLine.ServiceLineGroupHeader.Any(slgh =>
                        slgh.HasMedicalDirector && slgh.ServiceLineGroupHeaderKey != ServiceLineGroupHeaderKey);
            }
        }

        public bool CanEditHasNursePractitioner
        {
            get
            {
                if (ServiceLine == null)
                {
                    return false;
                }

                if (ServiceLine.IsHospiceServiceLine == false)
                {
                    return false;
                }

                return ServiceLine.ServiceLineGroupHeader == null
                    ? false
                    : !ServiceLine.ServiceLineGroupHeader.Any(slgh =>
                        slgh.HasNursePractitioner && slgh.ServiceLineGroupHeaderKey != ServiceLineGroupHeaderKey);
            }
        }

        public bool CanEditServiceTypesAssociated
        {
            get
            {
                if (ServiceLine == null)
                {
                    return false;
                }

                if (ServiceLine.ServiceLineGroupHeader == null)
                {
                    return false;
                }

                return true;
            }
        }

        public bool CanEditCensusTractDependency
        {
            get
            {
                if (ServiceLine == null)
                {
                    return false;
                }

                if (ServiceLine.ServiceLineGroupHeader == null)
                {
                    return false;
                }

                return true;
            }
        }

        public bool CanEditIsTeam
        {
            get
            {
                if (ServiceLine == null)
                {
                    return false;
                }

                if (ServiceLine.ServiceLineGroupHeader == null)
                {
                    return false;
                }

                return true;
            }
        }

        public string AgencyStateCodeCode => CodeLookupCache.GetCodeFromKey(AgencyStateCode);

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

        partial void OnGoLiveDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ServiceLine != null)
            {
                ServiceLine.UpdateGoLiveLevel();
            }

            RaisePropertyChanged("");
        }

        public List<ServiceLineGrouping> MyChildrenOnDate(DateTime date)
        {
            var ChildRelationshipsQuery = ServiceLineGroupingParent.Where(w => w.IsEffectiveOnDate(date));
            var ChildrenQuery = ChildRelationshipsQuery.Where(w => !w.Child.Inactive)
                .Select(s => s.ServiceLineGrouping1);
            return ChildrenQuery.ToList();
        }

        public void OnOasisServiceLineChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (!IsHospiceServiceLine)
            {
                UsingHISCoordinator = false;
            }

            if (!IsOasisServiceLine)
            {
                UsingOASISCoordinator = false;
            }

            RaisePropertyChanged("");
        }

        public void RaiseAllPropertiesChanged()
        {
            RaisePropertyChanged("");
        }

        partial void OnServiceLineGroupHeaderKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaiseAllPropertiesChanged();
        }

        partial void OnServiceLineKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaiseAllPropertiesChanged();
        }

        #region ServiceLineGroup TeamMeeting properties

        private static readonly string CONST_TeamMeetingScheduleType_Daily = "D";
        private static readonly string CONST_TeamMeetingScheduleType_Weekly = "W";
        private static readonly string CONST_TeamMeetingScheduleType_EveryOtherWeek = "E";

        partial void OnTeamMeetingScheduleTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TeamMeetingScheduleType_Daily");
            RaisePropertyChanged("TeamMeetingScheduleType_Weekly");
            RaisePropertyChanged("TeamMeetingScheduleType_EveryOtherWeek");
        }

        public bool TeamMeetingScheduleType_Daily
        {
            get { return TeamMeetingScheduleType == CONST_TeamMeetingScheduleType_Daily; }
            set
            {
                if (value)
                {
                    TeamMeetingScheduleType = CONST_TeamMeetingScheduleType_Daily;
                }
            }
        }

        public bool TeamMeetingScheduleType_Weekly
        {
            get { return TeamMeetingScheduleType == CONST_TeamMeetingScheduleType_Weekly; }
            set
            {
                if (value)
                {
                    TeamMeetingScheduleType = CONST_TeamMeetingScheduleType_Weekly;
                }
            }
        }

        public bool TeamMeetingScheduleType_EveryOtherWeek
        {
            get { return TeamMeetingScheduleType == CONST_TeamMeetingScheduleType_EveryOtherWeek; }
            set
            {
                if (value)
                {
                    TeamMeetingScheduleType = CONST_TeamMeetingScheduleType_EveryOtherWeek;
                }
            }
        }

        partial void OnTeamMeetingStartDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (TeamMeetingStartDate == null || TeamMeetingStartDate == DateTime.MinValue)
            {
                TeamMeetingStartDate = null;
                TeamMeetingDayOfWeek = null;
                RaisePropertyChanged("TeamMeetingDayOfWeekDescription");
                RaisePropertyChanged("TeamMeetingEveryOtherWeekBlirb");
                return;
            }

            var startDate = ((DateTime)TeamMeetingStartDate).Date;
            var today = DateTime.Today.Date;
            while (startDate > today) startDate = startDate.AddDays(-14);
            TeamMeetingStartDate = startDate;
            TeamMeetingDayOfWeek = (int)startDate.DayOfWeek;
            RaisePropertyChanged("TeamMeetingDayOfWeekDescription");
            RaisePropertyChanged("TeamMeetingEveryOtherWeekBlirb");
        }

        public string TeamMeetingDayOfWeekDescription
        {
            get
            {
                if (TeamMeetingDayOfWeek == null)
                {
                    return "";
                }

                var dh = new DateHelper();
                return dh.WeekDays.Where(w => w.Day == TeamMeetingDayOfWeek).FirstOrDefault().DayDescription;
            }
        }

        public string TeamMeetingEveryOtherWeekBlirb
        {
            get
            {
                if (TeamMeetingStartDate == null)
                {
                    return null;
                }

                var date = NextTeamMeetingDate_EveryOtherWeek(DateTime.Today.Date);
                return "Places next three meetings on:  " + date.ToLongDateString() + ",   " +
                       date.AddDays(14).ToLongDateString() + ",   and " + date.AddDays(28).ToLongDateString() + ".";
            }
        }

        public DateTime NextTeamMeetingDate_EveryOtherWeek(DateTime LastTeamMeetingDate)
        {
            var date = TeamMeetingStartDate == null ? DateTime.Today.Date : ((DateTime)TeamMeetingStartDate).Date;
            while (date > LastTeamMeetingDate) date = date.AddDays(-14);
            while (date < LastTeamMeetingDate) date = date.AddDays(14);
            return date;
        }

        #endregion

        #region ServiceLineGroupHeader properties

        public int? SequenceNumber
        {
            get
            {
                if (ServiceLineGroupHeader == null)
                {
                    return null;
                }

                return ServiceLineGroupHeader.SequenceNumber;
            }
        }

        public bool IsTopGroup => SequenceNumber != null && SequenceNumber == 0;

        public string ParentGroupHeaderLabel
        {
            get
            {
                if (SequenceNumber == 0)
                {
                    return null; //There is no Parent for this level
                }

                if (ServiceLine == null || ServiceLine.ServiceLineGroupHeader == null)
                {
                    return null; //prevent Exceptions
                }

                var parentLabel = ServiceLine.ServiceLineGroupHeader.Where(h => h.SequenceNumber == SequenceNumber - 1)
                    .Select(h => h.GroupHeaderLabel).FirstOrDefault();
                return parentLabel;
            }
        }

        public string ChildGroupHeaderLabel
        {
            get
            {
                if (SequenceNumber == 5)
                {
                    return null; //There is no child for this level
                }

                if (ServiceLine == null || ServiceLine.ServiceLineGroupHeader == null)
                {
                    return null; //prevent Exceptions
                }

                var childLabel = ServiceLine.ServiceLineGroupHeader.Where(h => h.SequenceNumber == SequenceNumber + 1)
                    .Select(h => h.GroupHeaderLabel).FirstOrDefault();
                return childLabel;
            }
        }

        #endregion

        #region ServiceLineGrouping's prospective Parents

        //Prospective parents are all ServiceLineGrouping rows that can be the 
        //parents of this ServiceLineGrouping instance.
        //Prospective parents must belong to the same ServiceLine and
        //must be one level (ServiceLineGroupHeader.SequenceNumber) higher up 
        //that this instance

        public List<ServiceLineGrouping> ProspectiveParents
        {
            get
            {
                if (SequenceNumber == null || SequenceNumber == 0 || ServiceLine == null ||
                    ServiceLine.ServiceLineGrouping == null)
                {
                    return null; //prevent Exceptions
                }

                //List prospective parents for a Group
                var fosterParents = ServiceLine.ServiceLineGrouping
                    .Where(g => g.Inactive == false && g.SequenceNumber == SequenceNumber - 1).ToList();

                return fosterParents;
            }
        }

        public void UndoParentChanges()
        {
            foreach (var parent in ServiceLineGroupingParent1)
            {
                parent.ValidationErrors.Clear();
                if (parent.IsNew)
                {
                    ServiceLineGroupingParent1.Remove(parent);
                }

                if (parent.IsModified)
                {
                    var origParent = (ServiceLineGroupingParent)parent.GetOriginal();
                    parent.EffectiveFromDate = origParent.EffectiveFromDate;
                    parent.EffectiveThruDate = origParent.EffectiveThruDate;
                    parent.ServiceLineGroupingKey = origParent.ServiceLineGroupingKey;
                    parent.ParentServiceLineGroupingKey = origParent.ParentServiceLineGroupingKey;
                    parent.CancelEditting();
                }


                Validate(null);
            }

            RaisePropertyChanged("FosterParents");
        }

        #endregion

        #region ServiceLineGrouping's children

        public List<ServiceLineGroupingParent> GroupChildren
        {
            get
            {
                if (SequenceNumber == null || SequenceNumber == 5 || ServiceLineGroupingParent == null)
                {
                    SelectedServiceLineGroupChild = null;
                    return null;
                }

                var children = ServiceLineGroupingParent.ToList();
                return children;
            }
        }

        public string GroupChildrenDescription
        {
            get
            {
                var groupChildren = GroupChildren;
                if (groupChildren == null || groupChildren.Any() == false)
                {
                    return "None";
                }

                string groupChildrenDescription = null;
                foreach (var p in groupChildren)
                    if (p.ServiceLineGrouping1 != null &&
                        string.IsNullOrWhiteSpace(p.ServiceLineGrouping1.Name) == false &&
                        p.ServiceLineGrouping1.Inactive == false)
                    {
                        if (groupChildrenDescription != null)
                        {
                            groupChildrenDescription = groupChildrenDescription + ";  ";
                        }

                        groupChildrenDescription = groupChildrenDescription + p.ServiceLineGrouping1.Name;
                    }

                return string.IsNullOrWhiteSpace(groupChildrenDescription) ? "None" : groupChildrenDescription;
            }
        }

        private ServiceLineGroupingParent _selectedServiceLineGroupChild;

        public ServiceLineGroupingParent SelectedServiceLineGroupChild
        {
            get { return _selectedServiceLineGroupChild; }
            set
            {
                if (_selectedServiceLineGroupChild != value)
                {
                    return;
                }

                _selectedServiceLineGroupChild = value;
                RaisePropertyChanged("SelectedServiceLineGroupChild");
            }
        }

        #endregion

        #region ServiceLineGrouping's Foster Parents

        public List<ServiceLineGroupingParent> FosterParents
        {
            get
            {
                var fosterParents = ServiceLineGroupingParent1.OrderByDescending(p => p.EffectiveFromDate).ToList();
                fosterParents.ForEach(p =>
                {
                    p.PropertyChanged -= OnParentChanged;
                    p.PropertyChanged += OnParentChanged;
                });
                return fosterParents;
            }
        }

        public EventHandler<ParentChangedEventArgs> OnGroupingParentChanged;

        private void OnParentChanged(object sender, PropertyChangedEventArgs e)
        {
            //Do not raise event if nobody is listening 
            if (OnGroupingParentChanged == null)
            {
                return;
            }

            //Exclude RIA Services state properties from raising event
            if (!ParentObservingPropertyNamesList.Any(p => p == e.PropertyName))
            {
                return;
            }

            //Raise event
            OnGroupingParentChanged(this,
                new ParentChangedEventArgs((ServiceLineGroupingParent)sender, e.PropertyName));
        }

        public List<string> ParentObservingPropertyNamesList =>
            new List<string>
                { "EffectiveFromDate", "EffectiveThruDate", "ServiceLineGroupingKey", "ParentServiceLineGroupingKey" };

        #endregion

        #region UserProfile properties

        private UserProfile _UserProfile;

        public UserProfile UserProfile
        {
            get { return _UserProfile; }
            set
            {
                _UserProfile = value;
                RaisePropertyChanged("UserProfile");
                RaisePropertyChanged("MyUserProfileGroup");
            }
        }

        private bool _IncludeAllChildren;

        public bool IncludeAllChildren
        {
            get { return _IncludeAllChildren; }
            set
            {
                _IncludeAllChildren = value;
                RaisePropertyChanged("IncludeAllChildren");
                RaisePropertyChanged("ChildServiceLineGroupingList");
            }
        }

        public UserProfileGroup MyUserProfileGroup
        {
            get
            {
                if (UserProfile == null)
                {
                    return null;
                }

                if (UserProfile.UserProfileGroup == null)
                {
                    return null;
                }

                //ensure do not add more than once...not really sure why I need to do this, just observed that it happened when testing something else - creating way too many inserts...
                //var all = UserProfile.UserProfileGroup.Where(upg => upg.UserID.Equals(UserProfile.UserId) && upg.ServiceLineGroupingKey == this.ServiceLineGroupingKey).ToList();
                var upslg = UserProfile.UserProfileGroup
                    .Where(upg => upg.UserID.Equals(UserProfile.UserId))
                    .Where(upg => upg.ServiceLineGroupingKey == ServiceLineGroupingKey)
                    .Where(upg => upg.EndDate == null)
                    .FirstOrDefault();
                if (upslg == null)
                {
                    upslg = new UserProfileGroup
                    {
                        UserID = UserProfile.UserId,
                        StartDate = DateTime.Now,
                        EndDate = null,
                        ServiceLineGroupingKey = ServiceLineGroupingKey
                    };
                    UserProfile.UserProfileGroup.Add(upslg);
                }

                return upslg;
            }
        }

        public List<ServiceLineGrouping> AllChildServiceLineGroupingList(bool includeInactive)
        {
            if (ServiceLineGroupingParent == null)
            {
                return null;
            }

            var slgpList = ServiceLineGroupingParent.Where(p =>
                (p.EffectiveFromDate == null || p.EffectiveFromDate != null &&
                    ((DateTime)p.EffectiveFromDate).Date <= DateTime.Today) && (p.EffectiveThruDate == null ||
                    p.EffectiveThruDate != null && ((DateTime)p.EffectiveThruDate).Date >= DateTime.Today)).ToList();
            if (slgpList == null || slgpList.Any() == false)
            {
                return null;
            }

            var slgList = new List<ServiceLineGrouping>();
            foreach (var slgp in slgpList)
                if (slgp.ServiceLineGrouping1 != null &&
                    (slgp.ServiceLineGrouping1.Inactive == false || includeInactive))
                {
                    slgList.Add(slgp.ServiceLineGrouping1);
                }

            if (slgList == null || slgList.Any() == false)
            {
                return null;
            }

            return slgList.OrderBy(s => s.Name).ToList();
        }

        public List<ServiceLineGrouping> ChildServiceLineGroupingList
        {
            get
            {
                var slgList = AllChildServiceLineGroupingList(false);
                if (slgList == null)
                {
                    return null;
                }

                // return the whole list if in edit - otherwise just return the ones with selections 
                if (IncludeAllChildren)
                {
                    return slgList;
                }

                slgList = slgList.Where(s => s.HasSelections).OrderBy(sl => sl.Name).ToList();
                if (slgList == null || slgList.Any() == false)
                {
                    return null;
                }

                return slgList;
            }
        }

        public bool HasSelections
        {
            get
            {
                if (MyUserProfileGroup != null && (MyUserProfileGroup.Oversite || MyUserProfileGroup.Owner ||
                                                   MyUserProfileGroup.CanVisit))
                {
                    return true;
                }

                return SomeChildHasSelections;
            }
        }

        public bool SomeChildHasSelections
        {
            get
            {
                var slgList = AllChildServiceLineGroupingList(false);
                if (slgList == null)
                {
                    return false;
                }

                var hasSelections = slgList.Any(c => c.HasSelections);
                return hasSelections;
            }
        }

        public string NameQualified =>
            GroupHeaderDescription + " " + (string.IsNullOrWhiteSpace(MyName) ? "?" : MyName);

        partial void OnNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            DeriveFullName();
            RaisePropertyChanged("NameQualified");
            RaisePropertyChanged("FullNameOrName");
        }

        public string FullNameOrName => string.IsNullOrWhiteSpace(FullName) ? MyName : FullName;

        public bool IsChildOf(string slpParentKeys)
        {
            if (string.IsNullOrWhiteSpace(slpParentKeys))
            {
                return false;
            }

            var keys = "|" + slpParentKeys + "|";
            var today = DateTime.Today.Date;
            var slgh = ServiceLineGroupingParent1.Where(p =>
                    (p.EffectiveFromDate == null || p.EffectiveFromDate != null && p.EffectiveFromDate <= today) &&
                    (p.EffectiveThruDate == null || p.EffectiveThruDate != null && p.EffectiveThruDate > today))
                .FirstOrDefault();
            if (slgh == null || slgh.ServiceLineGrouping == null)
            {
                return false;
            }

            return keys.Contains("|" + slgh.ServiceLineGrouping.ServiceLineGroupingKey.ToString().Trim() + "|")
                ? true
                : false;
        }

        public void DeriveFullName()
        {
            var name = string.IsNullOrWhiteSpace(MyName) ? "?" : MyName;
            if (SequenceNumber == 0)
            {
                FullName = name;
                return;
            }

            var unlinkedName = "(unlinked) - " + name;
            if (ServiceLineGroupingParent1 == null)
            {
                FullName = unlinkedName;
                return;
            }

            var today = DateTime.Today.Date;
            var slgh = ServiceLineGroupingParent1.Where(p =>
                (p.EffectiveFromDate == null ||
                 p.EffectiveFromDate != null && ((DateTime)p.EffectiveFromDate).Date <= today) &&
                (p.EffectiveThruDate == null ||
                 p.EffectiveThruDate != null && ((DateTime)p.EffectiveThruDate).Date >= today)).FirstOrDefault();
            if (slgh == null || slgh.ServiceLineGrouping == null)
            {
                FullName = unlinkedName;
                return;
            }

            FullName = name + " - " + slgh.ServiceLineGrouping.FullName;
        }

        public string FullNamePlusServiceLine
        {
            get
            {
                if (ServiceLine == null)
                {
                    return FullNameOrName;
                }

                return FullNameOrName + " - " + ServiceLine.Name;
            }
        }

        public bool IsLeaf
        {
            get
            {
                if (SequenceNumber == null)
                {
                    return false;
                }

                var slgh = ServiceLineCache.Current.Context.ServiceLineGroupHeaders
                    .Where(h => h.ServiceLineKey == ServiceLineKey).OrderByDescending(h => h.SequenceNumber)
                    .FirstOrDefault();
                if (slgh == null || slgh.SequenceNumber == null)
                {
                    return false;
                }

                var isLeaf = SequenceNumber == slgh.SequenceNumber ? true : false;
                return isLeaf;
            }
        }

        private bool _IsExpanded;

        public bool IsExpanded
        {
            get { return _IsExpanded; }
            set
            {
                _IsExpanded = value;
                RaisePropertyChanged("IsExpanded");
            }
        }

        public void RaiseEvents()
        {
            RaisePropertyChanged(null);
        }

        #endregion
    }

    public class ParentChangedEventArgs : EventArgs
    {
        public ParentChangedEventArgs(ServiceLineGroupingParent serviceLineGroupingParent, string propertyName)
        {
            ServiceLineGroupingParent = serviceLineGroupingParent;
            PropertyName = propertyName;
        }

        public ServiceLineGroupingParent ServiceLineGroupingParent { get; protected set; }
        public string PropertyName { get; protected set; }
    }
}