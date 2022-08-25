#region Usings

using System;
using Virtuoso.Core;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Occasional;
using Virtuoso.Core.Occasional.Model;
using Virtuoso.Core.Utility;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class HomeScreenTaskPM : IClinicalKeys
    {
        private static readonly string CR = char.ToString('\r');
        private bool _CanAttemptTask;
        private bool _CanDeleteTask;
        private bool _CanEditTask;
        private bool _CanUploadTask;
        private bool _IsCached;

        public string TaskCommentsLong => PatientKey == null ? Notes : TaskComments;

        public string TaskCommentsShort
        {
            get
            {
                if (PatientKey == null)
                {
                    return TaskCommentsLong;
                }

                if (string.IsNullOrWhiteSpace(TaskCommentsLong))
                {
                    return TaskCommentsLong;
                }

                var s = TaskCommentsLong.Trim();
                if (s.Contains(CR))
                {
                    s = s.Substring(0, s.IndexOf(CR));
                    if (s.Length > 200)
                    {
                        s = s.Substring(0, 200);
                    }

                    return s + "...";
                }

                if (s.Length > 200)
                {
                    s = s.Substring(0, 200) + "...";
                }

                return s;
            }
        }

        public bool ShowMoreTaskComments
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TaskCommentsLong))
                {
                    return false;
                }

                return TaskCommentsLong.Trim() != TaskCommentsShort.Trim();
            }
        }

        #region FullNameWithMRN // store cached value to reduce the sorting time for this column
        private string _fullNameWithMRN; // store cached value to reduce the sorting time for this column
        public string FullNameWithMRN
        {
            get
            {
                if(_fullNameWithMRN == null)
                {
                    UpdateFullNameWithMRN();
                }
                return _fullNameWithMRN;
            }
        }

        private void UpdateFullNameWithMRN()
        {
            var name = string.Format("{0}{1}",
                    FullNameWithSuffix == null ? "" : FullNameWithSuffix.Trim(),
                    MRN == null ? "" : " - " + MRN.Trim()).Trim();
            if (name == "," || name == "")
            {
                name = " ";
            }

            if (name == "All,")
            {
                name = "All";
            }

            _fullNameWithMRN = name;
        }

        partial void OnFullNameWithSuffixChanged()
        {
            UpdateFullNameWithMRN();
        }

        partial void OnMRNChanged()
        {
            UpdateFullNameWithMRN();
        }
        #endregion

        public bool CanAttemptTask
        {
            get { return _CanAttemptTask; }
            set
            {
                if (value != _CanAttemptTask)
                {
                    _CanAttemptTask = value;
                    RaisePropertyChanged("CanAttemptTask");
                }
            }
        }

        public bool CanUploadTask
        {
            get { return _CanUploadTask; }
            set
            {
                if (value != _CanUploadTask)
                {
                    _CanUploadTask = value;
                    RaisePropertyChanged("CanUploadTask");
                }
            }
        }

        public bool CanDeleteTask
        {
            get { return _CanDeleteTask; }
            set
            {
                if (value != _CanDeleteTask)
                {
                    _CanDeleteTask = value;
                    RaisePropertyChanged("CanDeleteTask");
                }
            }
        }

        public bool CanEditTask
        {
            get { return _CanEditTask; }
            set
            {
                if (value != _CanEditTask)
                {
                    _CanEditTask = value;
                    RaisePropertyChanged("CanEditTask");
                }
            }
        }

        public bool IsCached
        {
            get { return _IsCached; }
            set
            {
                if (value != _IsCached)
                {
                    _IsCached = value;
                    RaisePropertyChanged("IsCached");
                }
            }
        }

        public int EncounterStatusMark
        {
            get
            {
                // If a non-Encounter based task - complete or not
                if (PatientKey.HasValue == false)
                {
                    return TaskEndDateTime.HasValue ? (int)EncounterStatusType.Completed : 0;
                }

                if (SYS_CDIsHospice)
                {
                    if (TaskStatus == (int)EncounterStatusType.OASISReview)
                    {
                        return (int)EncounterStatusType.HISReview;
                    }

                    if (TaskStatus == (int)EncounterStatusType.OASISReviewEdit)
                    {
                        return (int)EncounterStatusType.HISReviewEdit;
                    }

                    if (TaskStatus == (int)EncounterStatusType.OASISReviewEditRR)
                    {
                        return (int)EncounterStatusType.HISReviewEditRR;
                    }
                }

                return TaskStatus;
            }
        }

        public string DayInPeriodBlirb
        {
            get
            {
                if (IsPDGM == false || SequenceNum == null || PPSBillStartDate == null)
                {
                    return null;
                }

                return string.Format("Day {0} - {1}", DayInPeriod, SequenceNum.ToString());
            }
        }

        public string DayInPeriodToolTip
        {
            get
            {
                if (IsPDGM == false || SequenceNum == null || PPSBillStartDate == null)
                {
                    return null;
                }

                return string.Format("This task is on day {0} of billing period {1}, starting {2}", DayInPeriod,
                    SequenceNum.ToString(), ((DateTime)PPSBillStartDate).Date.ToShortDateString());
            }
        }

        private string DayInPeriod
        {
            get
            {
                if (PPSBillStartDate == null || TaskStartDateTime.Date < ((DateTime)PPSBillStartDate).Date)
                {
                    return "?";
                }

                var days = 0;
                try
                {
                    days =
                        Convert.ToInt32(TaskStartDateTime.Date.Subtract(((DateTime)PPSBillStartDate).Date).TotalDays) +
                        1;
                }
                catch
                {
                    return "?";
                }

                return days.ToString();
            }
        }

        private bool IsPDGM
        {
            get
            {
                var version = 0;
                try
                {
                    version = int.Parse(PPSModelVersion);
                }
                catch
                {
                }

                return version >= 3;
            }
        }

        private bool SYS_CDIsHospice
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

        private bool IsAttemptedVisit
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DocumentDescriptionSortable))
                {
                    return false;
                }

                return DocumentDescriptionSortable.Trim().ToLower().StartsWith("attempted ") ? true : false;
            }
        }

        public int TaskStartTime
        {
            get
            {
                if (TaskStartDateTime == null)
                {
                    return 0;
                }

                var d = TaskStartDateTime.DateTime;
                return d.Hour * 60 + d.Minute;
            }
        }

        private ServiceType MyServiceType
        {
            get
            {
                if (ServiceTypeKey == null)
                {
                    return null;
                }

                return ServiceTypeCache.GetServiceTypeFromKey((int)ServiceTypeKey);
            }
        }

        private Form MyForm
        {
            get
            {
                var st = MyServiceType;
                if (st == null || st.FormKey == null)
                {
                    return null;
                }

                return DynamicFormCache.GetFormByKey((int)st.FormKey);
            }
        }

        private bool TaskIsEval
        {
            get
            {
                var f = MyForm;
                return f?.IsEval ?? false;
            }
        }

        private bool TaskIsVisit
        {
            get
            {
                var f = MyForm;
                return f?.IsVisit ?? false;
            }
        }

        private bool TaskIsResumption
        {
            get
            {
                var f = MyForm;
                return f?.IsResumption ?? false;
            }
        }

        private bool TaskIsEvalOrVisitOrResumption
        {
            get
            {
                if (TaskIsEval)
                {
                    return true;
                }

                if (TaskIsVisit)
                {
                    return true;
                }

                if (TaskIsResumption)
                {
                    return true;
                }

                return false;
            }
        }

        int IClinicalKeys.PatientKey => PatientKey.GetValueOrDefault();

        int IClinicalKeys.AdmissionKey => AdmissionKey.GetValueOrDefault();

        public void RefreshTaskComments()
        {
            RaisePropertyChanged("TaskCommentsLong");
            RaisePropertyChanged("TaskCommentsShort");
            RaisePropertyChanged("ShowMoreTaskComments");
        }

        public async System.Threading.Tasks.Task SetCanEdit()
        {
            // If a non-Encounter based task - can only delete if its not complete
            if (NonServiceTypeKey.HasValue)
            {
                CanEditTask = TaskEndDateTime.HasValue ? false : true;
            }
            else
            {
                /*
                    Edit = 1,
                    CoderReview = 2,
                    CoderReviewEdit = 3,
                    OASISReview = 4,
                    OASISReviewEdit = 5,
                    OASISReviewEditRR = 6,
                    Completed = 7,
                */
                var okayToEdit = true;

                if (TaskStatus != (int)EncounterStatusType.None)
                {
                    var UserID = WebContext.Current.User.MemberID;

                    if (RoleAccessHelper.CheckPermission(RoleAccess.Admin, false) || CareCoordinator == UserID)
                    {
                        if (TaskStatus == (int)EncounterStatusType.OASISReview ||
                            TaskStatus == (int)EncounterStatusType.Completed)
                        {
                            okayToEdit = false;
                        }
                    }
                    else
                    {
                        okayToEdit = false;
                    }
                }

                //If signed offline - disallow edit
                if (IsCached)
                {
                    var dyn = await DynamicFormSipManager.Instance.GetDynamicInfo(TaskKey, OfflineStoreType.SAVE, true);
                    if (dyn != null && dyn.EncounterStatus == (int)EncounterStatusType.Completed)
                    {
                        okayToEdit = false;
                    }
                }

                CanEditTask = okayToEdit;
            }
        }

        public void SetCanAttempt()
        {
            if (PatientKey == null || PatientKey <= 0) // cannot attempt a non-Encounter based task 
            {
                CanAttemptTask = false; // cannot attempt a non-Encounter based task 
            }
            else if (TenantSettingsCache.Current.UsingAttemptedVisit == false)
            {
                CanAttemptTask = false; // tenant setting takes presidence
            }
            else if (TaskStartDateTime.Date.CompareTo(DateTimeOffset.Now.Date) > 0)
            {
                CanAttemptTask = false; // cannot attempt tomorrow or later tasks - they are on the Scheduled tab
            }
            else if ((TaskStatus == (int)EncounterStatusType.None || TaskStatus == (int)EncounterStatusType.Edit) &&
                     TaskIsEvalOrVisitOrResumption && IsAttemptedVisit == false &&
                     UserID == WebContext.Current.User.MemberID)
            {
                // for an Encounter based task - can only attempt it if the encounter has not been started or if started,
                // that it's EncounterStatus is not (int)EncounterStatusType.None and the encounter is an eval, visit or resumprion
                CanAttemptTask = true;
            }
            else
            {
                CanAttemptTask = false;
            }
        }

        public void SetCanUpload(DynamicFormInfo info, bool isOnline)
        {
            if (info == null || isOnline == false)
            {
                CanUploadTask = false; // cannot upload unless saved and online
            }
            else if (PatientKey == null || PatientKey <= 0)
            {
                CanUploadTask = false; // cannot upload a non-Encounter based task 
            }
            else
            {
                CanUploadTask = isOnline && info != null;
            }
        }

        public void SetCanDelete()
        {
            if (PatientKey == null ||
                PatientKey <= 0) // If a non-Encounter based task - can only delete if its not complete
            {
                CanDeleteTask = TaskEndDateTime.HasValue ? false : true;
            }
            else if (TaskStatus == (int)EncounterStatusType.None) 
            {
                // for an Encounter based task - can only delete if the encounter has not been started or if started,
                // that it's EncounterStatus is not (int)EncounterStatusType.None
                CanDeleteTask = true;
            }
            else // for an Encounter based task - can only delete if the encounter has not been started
            {
                CanDeleteTask = false;
            }
        }
    }
}