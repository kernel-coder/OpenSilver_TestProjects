#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public enum eServiceLineType
    {
        HomeHealth = 2,
        Hospice = 4,
        HomeCare = 8
    }

    public partial class ServiceLine
    {
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

        public bool IsHospiceServiceLine => ServiceLineType == (int)eServiceLineType.Hospice;

        public bool IsHomeCareServiceLine => (int)eServiceLineType.HomeCare == ServiceLineType;

        public bool IsHomeHealthServiceLine => (int)eServiceLineType.HomeHealth == ServiceLineType;

        public string HomeHealthPrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "2");

        public string HospicePrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "4");

        public string HomeCarePrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "8");

        public string DisplayGroupingLabel => string.IsNullOrWhiteSpace(ServiceLineGroupingLabel)
            ? "Grouping"
            : ServiceLineGroupingLabel.Trim();

        public string GroupingZeroHeaderLabel
        {
            get
            {
                if (ServiceLineGroupHeader == null || ServiceLineGroupHeader.Any() == false)
                {
                    return "Grouping";
                }

                var header = ServiceLineGroupHeader.Where(sl => sl.SequenceNumber == 0).FirstOrDefault();
                if (header == null)
                {
                    return "Grouping";
                }

                return header.GroupHeaderLabel;
            }
        }

        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    return string.Format("ServiceLine {0}", Name.Trim());
                }

                return IsNew ? "New ServiceLine" : "Edit ServiceLine";
            }
        }

        public bool IsIDEditable => !(ServiceLineKey > 0);

        public int MyServiceLineKey => ServiceLineKey;

        public string StateCodeCode => CodeLookupCache.GetCodeFromKey(StateCode);

        public string CityStateZip => FormatHelper.FormatCityStateZip(City, StateCodeCode, ZipCode);

        public bool CollectionHasValidationErrors
        {
            get { return HasValidationErrors || ServiceLineGroupHeader.Any(slg => slg.HasValidationErrors); }
        }

        public bool IsNonHospicePreEvalRequiredEnabled
        {
            get
            {
                //HospicePreEvalRequired can only be set if Agency hasn't set it Before
                var isEnabled = !TenantSettingsCache.Current.TenantSetting.NonHospicePreEvalRequired;
                if (!isEnabled)
                {
                    NonHospicePreEvalRequired = false; //clear it when disabled
                }

                return isEnabled;
            }
        }

        public bool UseSchedulingEnabled
        {
            get
            {
                //UseScheduling can only be set if Agency hasn't set it Before
                var isEnabled = !TenantSettingsCache.Current.TenantSetting.UseScheduling;
                if (!isEnabled)
                {
                    UseScheduling = false; //clear it when disabled
                }

                return isEnabled;
            }
        }

        public string PlanOfCareTasksDesc
        {
            get
            {
                if (string.IsNullOrEmpty(PlanOfCareTasks))
                {
                    return string.Empty;
                }

                var tasks = PlanOfCareTasks.Split('|').ToList();
                if (tasks == null || tasks.Any() == false)
                {
                    return string.Empty;
                }

                var desc = string.Empty;
                var i = 0;
                foreach (var t in tasks)
                {
                    if (t == "A")
                    {
                        desc = StringAdd(desc, "Admission Care Coordinator", i);
                        i++;
                    }

                    if (t == "B")
                    {
                        desc = StringAdd(desc, "Skilled Nursing", i);
                        i++;
                    }

                    if (t == "C")
                    {
                        desc = StringAdd(desc, "Physical Therapy", i);
                        i++;
                    }

                    if (t == "D")
                    {
                        desc = StringAdd(desc, "Speech Language Pathology/ Speech Therapy", i);
                        i++;
                    }

                    if (t == "E")
                    {
                        desc = StringAdd(desc, "Occupational Therapy", i);
                        i++;
                    }
                }

                return desc;
            }
        }

        partial void OnGoLiveDateChanged()
        {
            if (!IsDeserializing)
            {
                UpdateGoLiveLevel();
                RaisePropertyChanged("");
            }
        }

        public void UpdateGoLiveLevel()
        {
            var lvl = ServiceLineGrouping.Where(slg => slg.GoLiveDate.HasValue && slg.HasValidationErrors == false)
                .Select(slg => slg.ServiceLineGroupHeader.SequenceNumber).FirstOrDefault();
            GoLiveLevel = lvl;
        }

        partial void OnServiceLineGroupingLabelChanged()
        {
            if (!IsDeserializing)
            {
                RaisePropertyChanged("DisplayGroupingLabel");
            }
        }

        partial void OnNameChanged()
        {
            if (!IsDeserializing)
            {
                RaisePropertyChanged("TabHeader");
            }
        }

        partial void OnCityChanged()
        {
            if (!IsDeserializing)
            {
                RaisePropertyChanged("CityStateZip");
            }
        }

        partial void OnStateCodeChanged()
        {
            if (!IsDeserializing)
            {
                RaisePropertyChanged("CityStateZip");
                RaisePropertyChanged("StateCodeCode");
            }
        }

        partial void OnZipCodeChanged()
        {
            if (!IsDeserializing)
            {
                RaisePropertyChanged("CityStateZip");
            }
        }

        partial void OnOasisServiceLineChanged()
        {
            if (!IsDeserializing)
            {
                foreach (var slg in ServiceLineGrouping)
                    slg.OnOasisServiceLineChanged();
            }
        }

        partial void OnServiceLineTypeChanged()
        {
            if (!IsDeserializing)
            {
                if (!IsHomeHealthServiceLine)
                {
                    OasisServiceLine = false;
                }

                RaisePropertyChanged("OasisServiceLine");
                RaisePropertyChanged("IsHomeHealthServiceLine");
                RaisePropertyChanged("IsHospiceServiceLine");
                RaisePropertyChanged("IsHomeCareServiceLine");
            }
        }

        partial void OnPlanOfCareTasksChanged()
        {
            if (!IsDeserializing)
            {
                RaisePropertyChanged("PlanOfCareTasksDesc");
            }
        }

        private string StringAdd(string org, string s, int i)
        {
            if (i > 0)
            {
                org = org + ", " + s;
            }
            else
            {
                org = org + s;
            }

            return org;
        }

        #region UserProfile properties

        private UserProfile _UserProfile;

        public UserProfile UserProfile
        {
            get { return _UserProfile; }
            set
            {
                _UserProfile = value;
                RaisePropertyChanged("UserProfile");
                RaisePropertyChanged("MyUserProfileServiceLine");
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
                RaisePropertyChanged("HasSelections");
            }
        }

        public UserProfileServiceLine MyUserProfileServiceLine
        {
            get
            {
                if (UserProfile == null)
                {
                    return null;
                }

                if (UserProfile.UserProfileServiceLine == null)
                {
                    return null;
                }

                var upsl = UserProfile.UserProfileServiceLine
                    .Where(u => u.ServiceLineKey == ServiceLineKey && u.EndDate == null).FirstOrDefault();
                if (upsl == null)
                {
                    upsl = new UserProfileServiceLine
                    {
                        UserID = UserProfile.UserId,
                        StartDate = DateTime.Now,
                        EndDate = null,
                        ServiceLineKey = ServiceLineKey
                    };
                    UserProfile.UserProfileServiceLine.Add(upsl);
                }

                return upsl;
            }
        }

        private List<ServiceLineGrouping> AllChildServiceLineGroupingList(bool includeInactive)
        {
            if (ServiceLineGroupHeader == null || ServiceLineGroupHeader.Any() == false)
            {
                return null;
            }

            var h = ServiceLineGroupHeader.Where(sl => sl.SequenceNumber == 0).FirstOrDefault();
            if (h == null)
            {
                return null;
            }

            if (ServiceLineGrouping == null)
            {
                return null;
            }

            var slgList = ServiceLineGrouping
                .Where(s => s.ServiceLineGroupHeaderKey == h.ServiceLineGroupHeaderKey &&
                            (s.Inactive == false || includeInactive)).OrderBy(sl => sl.Name).ToList();
            if (slgList == null || slgList.Any() == false)
            {
                return null;
            }

            return slgList;
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
                if (MyUserProfileServiceLine != null && MyUserProfileServiceLine.Oversite)
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

        public void DeriveServiceLineGroupingFullNames()
        {
            if (ServiceLineGrouping == null)
            {
                return;
            }

            ServiceLineGrouping.OrderBy(s => s.SequenceNumber).ToList().ForEach(s => { s.DeriveFullName(); });
        }

        public string NameQualified => "Service Line " + (string.IsNullOrWhiteSpace(Name) ? "?" : Name);

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
}