using System;
using System.Linq;
using Virtuoso.Core.Cache;

namespace Virtuoso.Server.Data
{
    public partial class OrdersTrackingDisplay
    {
        public int? OldPrimaryInsuranceKey
        {
            get
            {
                if (AdmissionCoverageInfo == null)
                {
                    return null;
                }

                if (AdmissionCoverageInfo == null || AdmissionCoverageInfo.AdmissionCoverageInsuranceInfo == null)
                {
                    return null;
                }

                var aci = AdmissionCoverageInfo.AdmissionCoverageInsuranceInfo.OrderBy(a => a.Sequence)
                    .FirstOrDefault();
                if (aci == null || aci.PatientInsuranceKey == 0)
                {
                    return null;
                }

                if (PatientInsuranceInfo == null)
                {
                    return null;
                }

                var pii = PatientInsuranceInfo.Where(p => p.PatientInsuranceKey == aci.PatientInsuranceKey)
                    .FirstOrDefault();
                if (pii == null)
                {
                    return null;
                }

                return pii.InsuranceKey;
            }
        }

        public string OldPrimaryInsuranceType
        {
            get
            {
                var i = InsuranceCache.GetInsuranceFromKey(OldPrimaryInsuranceKey);
                if (i == null)
                {
                    return null;
                }

                return i.InsuranceTypeCodeDescription;
            }
        }

        public string PrimaryInsuranceType
        {
            get
            {
                var i = InsuranceCache.GetInsuranceFromKey(PrimaryInsuranceKey);
                if (i == null)
                {
                    return null;
                }

                return i.InsuranceTypeCodeDescription;
            }
        }

        public bool HasNotes
        {
            get
            {
                if (ChangeHistoryInfo == null)
                {
                    return false;
                }

                if (ChangeHistoryInfo.Any() == false)
                {
                    return false;
                }

                return ChangeHistoryInfo.Where(c => c.HasHistory).Any();
            }
        }

        public bool IsEncounterInterimOrder
        {
            get
            {
                if (PatientKey == 0 || AdmissionKey == 0 ||
                    FormKey == null || FormKey == 0 ||
                    ServiceTypeKey == null || ServiceTypeKey == 0 ||
                    TaskKey == null || TaskKey == 0 ||
                    EncounterKey == null || EncounterKey == 0)
                {
                    return false;
                }

                if (OrderEntryKey == null)
                {
                    return false;
                }

                if (IsInterimOrder == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsEncounterFaceToFace
        {
            get
            {
                if (PatientKey == 0 || AdmissionKey == 0 ||
                    FormKey == null || FormKey == 0 ||
                    ServiceTypeKey == null || ServiceTypeKey == 0 ||
                    TaskKey == null || TaskKey == 0 ||
                    EncounterKey == null || EncounterKey == 0)
                {
                    return false;
                }

                if (IsHospiceFaceToFace == false && IsFaceToFace == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsEncounterPOC
        {
            get
            {
                if (PatientKey == 0 || AdmissionKey == 0 ||
                    FormKey == null || FormKey == 0 ||
                    ServiceTypeKey == null || ServiceTypeKey == 0 ||
                    TaskKey == null || TaskKey == 0 ||
                    EncounterKey == null || EncounterKey == 0)
                {
                    return false;
                }

                if (EncounterPlanOfCareKey == null)
                {
                    return false;
                }

                if (IsPOC == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsPOC
        {
            get
            {
                return OrderType == (int)OrderTypesEnum.POC;
            }
        }
        public bool IsInterimOrder
        {
            get
            {
                return OrderType == (int)OrderTypesEnum.InterimOrder;
            }
        }
        public bool IsHospiceElectionAddendum
        {
            get
            {
                return OrderType == (int)OrderTypesEnum.HospiceElectionAddendum;
            }
        }
        public bool IsFaceToFace
        {
            get
            {
                return OrderType == (int)OrderTypesEnum.FaceToFace
                    || OrderType == (int)OrderTypesEnum.FaceToFaceEncounter;
            }
        }
        public bool IsHospiceFaceToFace
        {
            get
            {
                return OrderType == (int)OrderTypesEnum.HospiceFaceToFace;
            }
        }
        public bool IsCoTI
        {
            get
            {
                return OrderType == (int)OrderTypesEnum.CoTI;
            }
        }

        public bool IsEncounterInterimOrderOrFaceToFaceOrPOC
        {
            get
            {
                if (IsEncounterInterimOrder || IsEncounterFaceToFace || IsEncounterPOC)
                {
                    return true;
                }

                return false;
            }
        }


        public bool ShowOrderTypeToolTip
        {
            get
            {
                bool showToolTip = !string.IsNullOrEmpty(OrderTypeToolTip);
                return showToolTip;
            }
        }
        public string OrderTypeToolTip
        {
            get
            {
                string toolTip = null;

                if (IsPOC)
                {
                    DateTime? certFrom = PeriodStartDate;
                    DateTime? certThru = PeriodEndDate;

                    if (certFrom != null)
                    {
                        toolTip = certFrom.Value.ToShortDateString();
                    }

                    toolTip += " - ";

                    if (certThru != null)
                    {
                        toolTip += certThru.Value.ToShortDateString();
                    }
                }
                else if (IsInterimOrder)
                {
                    if (IsGeneratedReferral == true)
                    {
                        if (!string.IsNullOrEmpty(toolTip)) toolTip += Environment.NewLine;
                        toolTip += "Referral";
                    }
                    if (IsGeneratedVisitFrequency == true)
                    {
                        if (!string.IsNullOrEmpty(toolTip)) toolTip += Environment.NewLine;
                        toolTip += "Visit Frequency";
                    }
                    if (IsGeneratedGoals == true)
                    {
                        if (!string.IsNullOrEmpty(toolTip)) toolTip += Environment.NewLine;
                        toolTip += "Goal and Treatment";
                    }
                    if (IsGeneratedLabs == true)
                    {
                        if (!string.IsNullOrEmpty(toolTip)) toolTip += Environment.NewLine;
                        toolTip += "Labs/Test";
                    }
                    if (IsGeneratedInitialServiceOrder == true)
                    {
                        if (!string.IsNullOrEmpty(toolTip)) toolTip += Environment.NewLine;
                        toolTip += "Initial Order for Start of Care";
                    }
                    if (IsGeneratedMedications == true)
                    {
                        if (!string.IsNullOrEmpty(toolTip)) toolTip += Environment.NewLine;
                        toolTip += "Medication";
                    }
                    if (IsGeneratedEquipment == true)
                    {
                        if (!string.IsNullOrEmpty(toolTip)) toolTip += Environment.NewLine;
                        toolTip += "Equipment";
                    }
                    if (IsGeneratedSupply == true)
                    {
                        if (!string.IsNullOrEmpty(toolTip)) toolTip += Environment.NewLine;
                        toolTip += "Supplies";
                    }
                    if (IsGeneratedSupplyEquipment == true)
                    {
                        if (!string.IsNullOrEmpty(toolTip)) toolTip += Environment.NewLine;
                        toolTip += "Supplies / Equipment";
                    }
                    if (IsGeneratedOther == true)
                    {
                        if (!string.IsNullOrEmpty(toolTip)) toolTip += Environment.NewLine;
                        toolTip += "Other";
                    }
                    if (IsGeneratedRecertificationOrder == true)
                    {
                        if (!string.IsNullOrEmpty(toolTip)) toolTip += Environment.NewLine;
                        toolTip += "Recertification Order";
                    }
                }
                return toolTip;
            }
        }
        public string SLGGroups
        {
            get
            {
                string slgGroups = null;
                slgGroups = SLGName0;
                if (!string.IsNullOrEmpty(SLGName1)) slgGroups += " - " + SLGName1;
                if (!string.IsNullOrEmpty(SLGName2)) slgGroups += " - " + SLGName2;
                if (!string.IsNullOrEmpty(SLGName3)) slgGroups += " - " + SLGName3;
                if (!string.IsNullOrEmpty(SLGName4)) slgGroups += " - " + SLGName4;
                return slgGroups;
            }
        }
        public string SLGGroups0
        {
            get
            {
                if (string.IsNullOrEmpty(SLGName0)) return null;
                return SLGName0;
            }
        }
        public string SLGGroups1
        {
            get
            {
                if (string.IsNullOrEmpty(SLGName1)) return null;
                string slgGroups = null;
                slgGroups = SLGName0;
                if (!string.IsNullOrEmpty(SLGName1)) slgGroups += " - " + SLGName1;
                return slgGroups;
            }
        }
        public string SLGGroups2
        {
            get
            {
                if (string.IsNullOrEmpty(SLGName2)) return null;
                string slgGroups = null;
                slgGroups = SLGName0;
                if (!string.IsNullOrEmpty(SLGName1)) slgGroups += " - " + SLGName1;
                if (!string.IsNullOrEmpty(SLGName2)) slgGroups += " - " + SLGName2;
                return slgGroups;
            }
        }
        public string SLGGroups3
        {
            get
            {
                if (string.IsNullOrEmpty(SLGName3)) return null;
                string slgGroups = null;
                slgGroups = SLGName0;
                if (!string.IsNullOrEmpty(SLGName1)) slgGroups += " - " + SLGName1;
                if (!string.IsNullOrEmpty(SLGName2)) slgGroups += " - " + SLGName2;
                if (!string.IsNullOrEmpty(SLGName3)) slgGroups += " - " + SLGName3;
                return slgGroups;
            }
        }
        public string SLGGroups4
        {
            get
            {
                if (string.IsNullOrEmpty(SLGName4)) return null;
                string slgGroups = null;
                slgGroups = SLGName0;
                if (!string.IsNullOrEmpty(SLGName1)) slgGroups += " - " + SLGName1;
                if (!string.IsNullOrEmpty(SLGName2)) slgGroups += " - " + SLGName2;
                if (!string.IsNullOrEmpty(SLGName3)) slgGroups += " - " + SLGName3;
                if (!string.IsNullOrEmpty(SLGName4)) slgGroups += " - " + SLGName4;
                return slgGroups;
            }
        }
        public string Flag
        {
            get
            {
                string flag = null;

                if (NotMyPatient)
                {
                    flag = "NMP";
                }
                else if (NotMyOrder)
                {
                    flag = "NMO";
                }
                return flag;
            }
        }

        public DateTime? MostRecentFollowupNoteDate
        {
            get
            {
                if (ChangeHistoryInfo == null)
                {
                    return null;
                }

                return ChangeHistoryInfo.Where(c => c.MostRecentFollowupNoteDate != null).Max(c => c.MostRecentFollowupNoteDate);
                //DateTime? date = null;
                //foreach (var chi in ChangeHistoryInfo)
                //{
                //    var dt = chi.MostRecentFollowupNoteDate;
                //    if (dt != null)
                //    {
                //        if (date == null)
                //        {
                //            date = ((DateTime)dt).Date;
                //        }
                //        else if (((DateTime)date).Date < ((DateTime)dt).Date)
                //        {
                //            date = ((DateTime)dt).Date;
                //        }
                //    }
                //}

                //return date;
            }
        }

        public DateTime CalculatedStatusDate => StatusDate == null ? OrderDate.Date : ((DateTime)StatusDate).Date;

        public DateTime DueDate
        {
            get
            {
                // Assumes Status == OrdersTrackingStatus.Sent
                var baseDate = CalculatedStatusDate;
                var mostRecentFollowupNoteDate = MostRecentFollowupNoteDate;
                if (mostRecentFollowupNoteDate != null && ((DateTime)mostRecentFollowupNoteDate).Date > baseDate)
                {
                    baseDate = ((DateTime)mostRecentFollowupNoteDate).Date;
                }

                return RuleDefinitionCache.CalculateDueDate(baseDate, ServiceLineKey, InitialOrSubsequent,
                    OrderTypeCodeLookupKey);
            }
        }

        public int? OrderTypeCodeLookupKey
        {
            get
            {
                if (IsInterimOrder) return CodeLookupCache.GetOrderTypeInterim();

                if (IsPOC) return CodeLookupCache.GetOrderTypePOC();

                if (IsFaceToFace) return CodeLookupCache.GetOrderTypeF2F();

                if (IsCoTI) return CodeLookupCache.GetOrderTypeCTI();

                if (IsHospiceFaceToFace) return CodeLookupCache.GetOrderTypeHospF2F();

                return null;
            }
        }

        public string InitialOrSubsequent
        {
            get
            {
                // Assumes Status == OrdersTrackingStatus.Sent
                var mostRecentFollowupNoteDate = MostRecentFollowupNoteDate;
                if (mostRecentFollowupNoteDate == null)
                {
                    return "I";
                }

                return ((DateTime)mostRecentFollowupNoteDate).Date > CalculatedStatusDate ? "S" : "I";
            }
        }

        public DateTime WeekStartDate
        {
            get
            {
                var dayOfWeek = TenantSettingsCache.Current.TenantSettingWeekStartDay;
                var workWeekStartDate = OrderDate.Date;
                for (var i = 0; i > -7; i--)
                {
                    workWeekStartDate = OrderDate.AddDays(i).Date;
                    if (workWeekStartDate.DayOfWeek == dayOfWeek)
                    {
                        return workWeekStartDate;
                    }

                    if (PeriodStartDate != null && ((DateTime)PeriodStartDate).Date == workWeekStartDate)
                    {
                        return workWeekStartDate;
                    }
                }

                return OrderDate.Date;
            }
        }

        public bool IsInterimOrderInBatch
        {
            get
            {
                if (InterimOrderBatchInfo == null || InterimOrderBatchKey == null || InterimOrderBatchDetailKey == null)
                {
                    return false;
                }

                return true;
            }
        }

        public int? InterimOrderBatchDetailKey
        {
            get
            {
                if (InterimOrderBatchInfo == null)
                {
                    return null;
                }

                var iobi = InterimOrderBatchInfo.Where(i => i.InterimOrderBatchDetailKey > 0).FirstOrDefault();
                if (iobi == null)
                {
                    return null;
                }

                return iobi.InterimOrderBatchDetailKey;
            }
        }

        public int? InterimOrderBatchKey
        {
            get
            {
                if (InterimOrderBatchInfo == null)
                {
                    return null;
                }

                var iobi = InterimOrderBatchInfo.Where(i => i.InterimOrderBatchKey != null).FirstOrDefault();
                if (iobi == null)
                {
                    return null;
                }

                return iobi.InterimOrderBatchKey == 0 ? null : iobi.InterimOrderBatchKey;
            }
        }

        public int? BatchOrderId
        {
            get
            {
                if (InterimOrderBatchInfo == null)
                {
                    return null;
                }

                var iobi = InterimOrderBatchInfo.Where(i => i.InterimOrderBatchKey != null).FirstOrDefault();
                if (iobi == null)
                {
                    return null;
                }

                return iobi.OrderId == 0 ? null : iobi.OrderId;
            }
        }

        public DateTime? BatchCreateDate
        {
            get
            {
                if (InterimOrderBatchInfo == null)
                {
                    return null;
                }

                var iobi = InterimOrderBatchInfo.Where(i => i.InterimOrderBatchKey != null).FirstOrDefault();
                if (iobi == null)
                {
                    return null;
                }

                return iobi.BatchCreateDate;
            }
        }

        public int? AdmissionDocumentationKey
        {
            get
            {
                if (InterimOrderBatchInfo == null)
                {
                    return null;
                }

                var iobi = InterimOrderBatchInfo.Where(i => i.InterimOrderBatchKey != null).FirstOrDefault();
                if (iobi == null)
                {
                    return null;
                }

                return iobi.AdmissionDocumentationKey == 0 ? null : iobi.AdmissionDocumentationKey;
            }
        }

        #region OrderTypeString
        private string _orderTypeString; // store cached value to reduce the sorting time for this column

        public string OrderTypeString
        {
            get
            {
                if (_orderTypeString == null)
                {
                    UpdateOrderTypeString();
                }
                return _orderTypeString;
            }
        }

        private void UpdateOrderTypeString()
        {
            var orderTypeString = "Unknown";
            if (OrdersTrackingHelpers.IsOrderTypeValid(OrderType))
            {
                orderTypeString = OrdersTrackingHelpers.OrderTypeDescription(OrderType);
                var sl = ServiceLineCache.GetServiceLineFromKey(ServiceLineKey);
                if (sl != null && sl.IsHospiceServiceLine && string.IsNullOrWhiteSpace(orderTypeString) == false &&
                    orderTypeString == "Plan of Care")
                {
                    orderTypeString = "Hospice Plan";
                }
            }
            _orderTypeString = orderTypeString;
        }

        partial void OnOrderTypeChanged()
        {            
            UpdateOrderTypeString();
        }

        partial void OnServiceLineKeyChanged()
        {
            UpdateOrderTypeString();
        }
        #endregion
    }
}
