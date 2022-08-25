#region Usings

using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Converters;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class Task
    {
        private bool _CanAttemptTask;
        private bool _CanDeleteTask;
        private bool _CanEditTask;
        private Encounter _CurrentEncounter;

        private DocumentDescriptionConverter _DocumentDescriptionConverter;

        private bool _IsCached;
        private int _OrderEntryStatus;

        private int _TaskStatus;

        public string DocumentName
        {
            get
            {
                var converter = new DocumentDescriptionPlusSuffix();
                var document_name = converter.Convert(this, null, null, null);
                return document_name != null ? document_name.ToString() : "<No description for task activity>";
            }
        }

        public string DocumentDescriptionSortable
        {
            get
            {
                if (_DocumentDescriptionConverter == null)
                {
                    _DocumentDescriptionConverter = new DocumentDescriptionConverter();
                }

                return _DocumentDescriptionConverter.Convert(this, null, null, null) as string;
            }
        }

        public string TaskStartEnd
        {
            get
            {
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    return string.Format("{0} - {1}", TaskStartDateTime.ToString("HHmm"),
                        TaskStartDateTime.AddMinutes(TaskDuration).ToString("HHmm"));
                }

                return string.Format("{0} - {1}", TaskStartDateTime.DateTime.ToShortTimeString(),
                    TaskStartDateTime.AddMinutes(TaskDuration).DateTime.ToShortTimeString());
            }
        }

        public string TaskSuffix
        {
            get
            {
                if (Encounter == null)
                {
                    return null;
                }

                var e = Encounter.FirstOrDefault();
                if (e == null)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(OasisRFA) == false && !e.SYS_CDIsHospice)
                {
                    return OasisRFADescription;
                }

                if (string.IsNullOrWhiteSpace(OasisRFA) == false && e.SYS_CDIsHospice)
                {
                    return "";
                }

                if (e.OrderEntry == null)
                {
                    return null;
                }

                if (e.EncounterIsOrderEntry == false)
                {
                    return null;
                }

                var oe = e.OrderEntry.Where(p => p.HistoryKey == null).FirstOrDefault();
                return oe?.CompletedOnText;
            }
        }

        public Encounter CurrentEncounter
        {
            get
            {
                if (_CurrentEncounter != null)
                {
                    return _CurrentEncounter;
                }

                if (Encounter == null)
                {
                    return null;
                }

                _CurrentEncounter = Encounter.FirstOrDefault();
                return _CurrentEncounter;
            }
        }

        public ServiceType MyServiceType
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

        public int? ServiceTypeDisciplineKey
        {
            get
            {
                var st = MyServiceType;
                return st?.DisciplineKey;
            }
        }

        public string ServiceTypeDescription
        {
            get
            {
                if (CurrentEncounter != null)
                {
                    return CurrentEncounter.ServiceTypeDescription;
                }

                var st = MyServiceType;
                if (st == null)
                {
                    return null;
                }

                return string.IsNullOrWhiteSpace(st.Description) ? "Encounter Type ?" : st.Description;
            }
        }

        private string HISRFADescription
        {
            get
            {
                if (string.IsNullOrWhiteSpace(OasisRFA))
                {
                    return null;
                }

                switch (OasisRFA)
                {
                    case "01":
                        return " HIS Admission";
                    case "09":
                        return " HIS Discharge";
                    default:
                        return " Reason For Assessment '" + OasisRFA + "' Unknown";
                }
            }
        }

        private string OasisRFADescription
        {
            get
            {
                if (string.IsNullOrWhiteSpace(OasisRFA))
                {
                    return null;
                }

                switch (OasisRFA)
                {
                    case "01":
                        return " Start of care — further visits planned";
                    case "03":
                        return " Resumption of care (after inpatient stay)";
                    case "04":
                        return " Recertification (follow-up) reassessment";
                    case "05":
                        return " Other follow-up";
                    case "06":
                        return " Transferred — not discharged from agency";
                    case "07":
                        return " Transferred - discharged from agency";
                    case "08":
                        return " Death at home";
                    case "09":
                        return " Discharge from agency";
                    default:
                        return " Reason For Assessment '" + OasisRFA + "' Unknown";
                }
            }
        }

        public int TaskStatus
        {
            get
            {
                if (_TaskStatus != 0)
                {
                    return _TaskStatus;
                }

                _TaskStatus = GetTaskStatusMark();
                RaisePropertyChanged("IsTaskStatusComplete");
                return _TaskStatus;
            }
        }

        public int OrderEntryStatus
        {
            get
            {
                if (_OrderEntryStatus != 0)
                {
                    return _OrderEntryStatus;
                }

                _OrderEntryStatus = SetOrderEntryStatus();
                return _OrderEntryStatus;
            }
        }

        public bool IsOrderEntry
        {
            get
            {
                var te = CurrentEncounter;
                return te?.EncounterIsOrderEntry ?? false;
            }
        }

        public bool CanAttemptTask
        {
            get { return _CanAttemptTask; }
            set
            {
                _CanAttemptTask = value;
                RaisePropertyChanged("CanAttemptTask");
            }
        }

        public bool CanDeleteTask
        {
            get { return _CanDeleteTask; }
            set
            {
                _CanDeleteTask = value;
                RaisePropertyChanged("CanDeleteTask");
            }
        }

        public bool CanEditTask
        {
            get { return _CanEditTask; }
            set
            {
                _CanEditTask = value;
                RaisePropertyChanged("CanEditTask");
            }
        }

        public bool IsTaskStatusComplete => TaskStatus == (int)EncounterStatusType.Completed ? true : false;

        public bool IsCached
        {
            get { return _IsCached; }
            set
            {
                _IsCached = value;
                RaisePropertyChanged("IsCached");
            }
        }

        public Form MyForm
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

        public bool TaskIsPlanOfCare
        {
            get
            {
                var f = MyForm;
                return f != null && f.IsPlanOfCare;
            }
        }

        public bool TaskIsEval
        {
            get
            {
                var f = MyForm;
                return f?.IsEval ?? false;
            }
        }

        public bool TaskIsVisit
        {
            get
            {
                var f = MyForm;
                return f?.IsVisit ?? false;
            }
        }

        public bool TaskIsResumption
        {
            get
            {
                var f = MyForm;
                return f?.IsResumption ?? false;
            }
        }

        public bool TaskIsEvalOrVisitOrResumption
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

        private int GetTaskStatusMark()
        {
            // If a non-Encounter based task - complete or not
            var te = CurrentEncounter;
            if (te == null)
            {
                return TaskEndDateTime.HasValue ? (int)EncounterStatusType.Completed : 0;
            }

            // for an Encounter based task - can only delete if the encounter has not been started
            return te.EncounterStatusMark;
        }

        private int SetOrderEntryStatus()
        {
            // If a non-Encounter based task - complete or not
            var te = CurrentEncounter;
            if (te == null || te.OrderEntry == null)
            {
                return 0;
            }

            if (te.CurrentOrderEntry == null)
            {
                return 0;
            }

            return te.CurrentOrderEntry.OrderStatus;
        }
    }
}