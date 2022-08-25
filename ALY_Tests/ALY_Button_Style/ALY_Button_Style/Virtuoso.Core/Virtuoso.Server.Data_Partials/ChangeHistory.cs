#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public class ChangeHistoryItem
    {
        public string HistoryText { get; set; }
    }

    public partial class ChangeHistoryInfo
    {
        public string ChangeHistoryHeader
        {
            get
            {
                var user = UserCache.Current.GetFullNameWithSuffixFromUserId(UpdatedBy);
                if (string.IsNullOrWhiteSpace(user))
                {
                    user = "?";
                }

                var header = string.Format("On {0} by {1}:", UpdatedDate.Date.ToShortDateString(), user);
                return header;
            }
        }

        private DateTime? _MostRecentFollowupNoteDate = null;
        public DateTime? MostRecentFollowupNoteDate
        {
            get
            {
                if (_MostRecentFollowupNoteDate != null) return _MostRecentFollowupNoteDate;
                if (ChangeHistoryDetailInfo == null) return null;
                _MostRecentFollowupNoteDate = this.ChangeHistoryDetailInfo.Where(d => d.ChangedColumn == "OTNoteDate").Max(d => d.NewValueAsDate);
                return _MostRecentFollowupNoteDate;
            }
        }

        public List<ChangeHistoryItem> OrdersTrackingHistory => GenerateOrdersTrackingHistory(UpdatedDate.Date);

        public bool HasHistory
        {
            get
            {
                var hList = GenerateOrdersTrackingHistory(UpdatedDate.Date);
                if (hList == null || hList.Any() == false)
                {
                    return false;
                }

                return true;
            }
        }

        public string GenerateOrdersTrackingChangePhysicianHistory(int originalPhysicianKey,
            int? originalPhysicianAddressKey, int physicianKey, int? physicianAddressKey)
        {
            var chdList = new List<ChangeHistoryDetailInfo>();
            if (originalPhysicianKey != physicianKey)
            {
                chdList.Add(new ChangeHistoryDetailInfo
                {
                    ChangedColumn = "PhysicianKey", OriginalValue = originalPhysicianKey.ToString(),
                    NewValue = physicianKey.ToString()
                });
            }

            if (originalPhysicianAddressKey != physicianAddressKey)
            {
                chdList.Add(new ChangeHistoryDetailInfo
                {
                    ChangedColumn = "PhysicianAddressKey",
                    OriginalValue = originalPhysicianAddressKey == null ? null : originalPhysicianAddressKey.ToString(),
                    NewValue = physicianAddressKey == null ? null : physicianAddressKey.ToString()
                });
            }

            if (chdList.Any() == false)
            {
                return null;
            }

            var chiList = GenerateOrdersTrackingHistory(DateTime.Today.Date, chdList);
            var history = chiList.Any() == false ? null : chiList[0].HistoryText;
            return history;
        }

        public string GenerateOrdersTrackingOverrideDeliveryHistory(int? originalDeliveryMethod,
            string originalDestination, int? deliveryMethod, string destination)
        {
            var chdList = new List<ChangeHistoryDetailInfo>();
            if (originalDeliveryMethod != deliveryMethod)
            {
                chdList.Add(new ChangeHistoryDetailInfo
                {
                    ChangedColumn = "DeliveryMethod",
                    OriginalValue = originalDeliveryMethod == null ? null : originalDeliveryMethod.ToString(),
                    NewValue = deliveryMethod == null ? null : deliveryMethod.ToString()
                });
            }

            if (originalDestination != destination)
            {
                chdList.Add(new ChangeHistoryDetailInfo
                    { ChangedColumn = "Destination", OriginalValue = originalDestination, NewValue = destination });
            }

            if (chdList.Any() == false)
            {
                return null;
            }

            var chiList = GenerateOrdersTrackingHistory(DateTime.Today.Date, chdList);
            if (chiList.Any() == false)
            {
                return null;
            }

            var history = chiList[0].HistoryText;
            if (chiList.Count() == 2)
            {
                history = history + char.ToString('\r') + chiList[1].HistoryText;
            }

            return history;
        }

        public List<ChangeHistoryItem> GenerateOrdersTrackingHistory(DateTime updatedDate,
            List<ChangeHistoryDetailInfo> detailList = null)
        {
            var chiList = new List<ChangeHistoryItem>();
            if (detailList == null && ChangeHistoryDetailInfo == null)
            {
                return chiList;
            }

            var chdList = detailList;
            if (chdList == null)
            {
                chdList = new List<ChangeHistoryDetailInfo>();
                foreach (var chd in ChangeHistoryDetailInfo) chdList.Add(chd);
            }

            if (chdList.Any() == false)
            {
                return chiList;
            }

            foreach (var chd in chdList)
            {
                string historyText = null;
                switch (chd.ChangedColumn)
                {
                    case "OTNoteText":
                        if (string.IsNullOrWhiteSpace(chd.NewValue) == false)
                        {
                            // Ignore OTNoteText history if it wes generated due to DeliveryMethod and/or Destination and/or PhysicianKey and/or PhysicianAddressKey change
                            if (chdList.Where(d => d.ChangedColumn == "DeliveryMethod").Any() ||
                                chdList.Where(d => d.ChangedColumn == "Destination").Any() ||
                                chdList.Where(d => d.ChangedColumn == "PhysicianKey").Any() ||
                                chdList.Where(d => d.ChangedColumn == "PhysicianAddressKey").Any())
                            {
                                break;
                            }

                            var chd2 = chdList.Where(d => d.ChangedColumn == "OTNoteType").FirstOrDefault();
                            if (chd2 == null ||
                                ExtractCode(chd2.NewValue) == "FollowupNote") // assume followup if type not in history
                            {
                                historyText = string.Format("Followup note{0}: {1}",
                                    BuildOnDateClause(updatedDate, chdList, "OTNoteDate"), chd.NewValue);
                            }
                        }

                        break;

                    case "PhysicianKey":
                        var chd3 = chdList.Where(d => d.ChangedColumn == "PhysicianAddressKey").FirstOrDefault();
                        if (chd3 == null)
                        {
                            historyText = string.Format("Physician changed from {0} to {1}",
                                ExtractPhysicianName(chd.OriginalValue), ExtractPhysicianName(chd.NewValue));
                        }
                        else
                        {
                            historyText = string.Format(
                                "Physician changed from {0} at address {1}   to   {2} at address {3}",
                                ExtractPhysicianName(chd.OriginalValue),
                                ExtractPhysicianAddressText(chd3.OriginalValue), ExtractPhysicianName(chd.NewValue),
                                ExtractPhysicianAddressText(chd3.NewValue));
                        }

                        break;

                    case "PhysicianAddressKey":
                        var chd4 = chdList.Where(d => d.ChangedColumn == "PhysicianKey").FirstOrDefault();
                        if (chd4 == null)
                        {
                            historyText = string.Format("Physician address changed from {0}   to   {1}",
                                ExtractPhysicianAddressText(chd.OriginalValue),
                                ExtractPhysicianAddressText(chd.NewValue));
                        }

                        break;

                    case "DeliveryMethod":
                        historyText = string.Format("Delivery Method changed from {0} to {1}{2}",
                            ExtractCodeDescription(chd.OriginalValue), ExtractCodeDescription(chd.NewValue),
                            BuildOnDateClause(updatedDate, chdList, "OTNoteDate"));
                        break;

                    case "Destination":
                        historyText = string.Format("Destination changed from {0} to {1}{2}",
                            ExtractString(chd.OriginalValue), ExtractString(chd.NewValue),
                            BuildOnDateClause(updatedDate, chdList, "OTNoteDate"));
                        break;

                    case "OrderDate":
                        historyText = string.Format("Changed Order Date from {0} to {1}",
                            ExtractDate(chd.OriginalValue), ExtractDate(chd.NewValue));
                        break;

                    case "NotMyPatient":
                        if (chd.OriginalValue != chd.NewValue)
                        {
                            if ((chd.OriginalValue == null || chd.OriginalValue == "0") && chd.NewValue == "1")
                            {
                                historyText = "Changed from my patient to not my patient";
                            }
                            else if ((chd.NewValue == null || chd.NewValue == "0") && chd.OriginalValue == "1")
                            {
                                historyText = "Changed from not my patient to my patient";
                            }
                        }

                        break;

                    case "NotMyOrder":
                        if (chd.OriginalValue != chd.NewValue)
                        {
                            if ((chd.OriginalValue == null || chd.OriginalValue == "0") && chd.NewValue == "1")
                            {
                                historyText = "Changed from my order to not my order";
                            }
                            else if ((chd.NewValue == null || chd.NewValue == "0") && chd.OriginalValue == "1")
                            {
                                historyText = "Changed from not my order to my order";
                            }
                        }

                        break;

                    case "Status":
                        // Ignore StatusDate column - reliy instead on the paretn ChangeHistory.ChangeHistoryHeader
                        historyText = string.Format("Status changed from {0} to {1}",
                            OrdersTrackingHelpers.StatusDescription(chd.OriginalValue),
                            OrdersTrackingHelpers.StatusDescription(chd.NewValue));
                        break;

                    case "Inactive":
                        if (chd.OriginalValue != chd.NewValue)
                        {
                            if ((chd.OriginalValue == null || chd.OriginalValue == "0") && chd.NewValue == "1")
                            {
                                historyText = "Inactivated the order";
                            }
                            else if ((chd.NewValue == null || chd.NewValue == "0") && chd.OriginalValue == "1")
                            {
                                historyText = "Reactivated the order";
                            }
                        }

                        break;
                }

                if (historyText != null)
                {
                    chiList.Add(new ChangeHistoryItem { HistoryText = historyText });
                }
            }

            return chiList;
        }

        private string ExtractPhysicianName(string s)
        {
            var key = 0;
            try
            {
                key = int.Parse(s);
            }
            catch
            {
            }

            var d = key == 0 ? null : PhysicianCache.Current.GetPhysicianFullNameInformalWithSuffixFromKey(key);
            return string.IsNullOrWhiteSpace(d) ? "none" : d;
        }

        private string ExtractPhysicianAddressText(string s)
        {
            var key = 0;
            try
            {
                key = int.Parse(s);
            }
            catch
            {
            }

            var d = key == 0 ? null : PhysicianCache.Current.GetPhysicianAddressTextFromKey(key);
            return string.IsNullOrWhiteSpace(d) ? "none" : d;
        }

        private string ExtractCode(string s)
        {
            var key = 0;
            try
            {
                key = int.Parse(s);
            }
            catch
            {
            }

            var d = key == 0 ? null : CodeLookupCache.GetCodeFromKey(key);
            return string.IsNullOrWhiteSpace(d) ? "none" : d;
        }

        private string ExtractCodeDescription(string s)
        {
            var key = 0;
            try
            {
                key = int.Parse(s);
            }
            catch
            {
            }

            var d = key == 0 ? null : CodeLookupCache.GetCodeDescriptionFromKey(key);
            return string.IsNullOrWhiteSpace(d) ? "none" : d;
        }

        private string ExtractString(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? "none" : s;
        }

        private string ExtractDate(string stringDateTime)
        {
            if (string.IsNullOrWhiteSpace(stringDateTime))
            {
                return "Unknown";
            }

            var split = stringDateTime.Split(' ');
            if (split.Any() == false)
            {
                return "Unknown";
            }

            return split[0];
        }

        private string BuildOnDateClause(DateTime updatedDate, List<ChangeHistoryDetailInfo> detailList,
            string changedColumn)
        {
            var chd = detailList.Where(d => d.ChangedColumn == changedColumn).FirstOrDefault();
            if (chd == null)
            {
                return "";
            }

            var onDate = ExtractDate(chd.NewValue);
            return updatedDate.ToShortDateString() == onDate ? "" : ", dated " + onDate;
        }
    }

    public partial class ChangeHistoryDetail
    {
        private DateTime? _NewValueAsDate = null;
        public DateTime? NewValueAsDate
        {
            get
            {
                if (NewValue == null) return null;
                if (_NewValueAsDate != null) return _NewValueAsDate;
                try
                {
                    _NewValueAsDate = Convert.ToDateTime(NewValue).Date;
                }
                catch
                {
                    _NewValueAsDate = null;
                }
                return _NewValueAsDate;
            }
        }
    }

    public partial class ChangeHistoryDetailInfo
    {
        private DateTime? _NewValueAsDate = null;
        public DateTime? NewValueAsDate
        {
            get
            {
                if (NewValue == null) return null;
                if (_NewValueAsDate != null) return _NewValueAsDate;
                try
                {
                    _NewValueAsDate = Convert.ToDateTime(NewValue).Date;
                }
                catch
                {
                    _NewValueAsDate = null;
                }
                return _NewValueAsDate;
            }
        }
    }

    public partial class ChangeHistory
    {
        public string ChangeHistoryHeader
        {
            get
            {
                var user = UserCache.Current.GetFullNameWithSuffixFromUserId(UpdatedBy);
                if (string.IsNullOrWhiteSpace(user))
                {
                    user = "?";
                }

                var header = string.Format("On {0} by {1}:", UpdatedDate.Date.ToShortDateString(), user);
                return header;
            }
        }

        private DateTime? _MostRecentFollowupNoteDate = null;
        public DateTime? MostRecentFollowupNoteDate
        {
            get
            {
                if (_MostRecentFollowupNoteDate != null) return _MostRecentFollowupNoteDate;
                if (ChangeHistoryDetail == null) return null;
                _MostRecentFollowupNoteDate = this.ChangeHistoryDetail.Where(d => d.ChangedColumn == "OTNoteDate").Max(d => d.NewValueAsDate);
                return _MostRecentFollowupNoteDate;
            }
        }

        public List<ChangeHistoryItem> OrdersTrackingHistory => GenerateOrdersTrackingHistory(UpdatedDate.Date);

        public bool HasHistory
        {
            get
            {
                var hList = GenerateOrdersTrackingHistory(UpdatedDate.Date);
                if (hList == null || hList.Any() == false)
                {
                    return false;
                }

                return true;
            }
        }

        public string GenerateOrdersTrackingChangePhysicianHistory(int originalPhysicianKey,
            int? originalPhysicianAddressKey, int physicianKey, int? physicianAddressKey)
        {
            var chdList = new List<ChangeHistoryDetail>();
            if (originalPhysicianKey != physicianKey)
            {
                chdList.Add(new ChangeHistoryDetail
                {
                    ChangedColumn = "PhysicianKey", OriginalValue = originalPhysicianKey.ToString(),
                    NewValue = physicianKey.ToString()
                });
            }

            if (originalPhysicianAddressKey != physicianAddressKey)
            {
                chdList.Add(new ChangeHistoryDetail
                {
                    ChangedColumn = "PhysicianAddressKey",
                    OriginalValue = originalPhysicianAddressKey == null ? null : originalPhysicianAddressKey.ToString(),
                    NewValue = physicianAddressKey == null ? null : physicianAddressKey.ToString()
                });
            }

            if (chdList.Any() == false)
            {
                return null;
            }

            var chiList = GenerateOrdersTrackingHistory(DateTime.Today.Date, chdList);
            var history = chiList.Any() == false ? null : chiList[0].HistoryText;
            return history;
        }

        public string GenerateOrdersTrackingOverrideDeliveryHistory(int? originalDeliveryMethod,
            string originalDestination, int? deliveryMethod, string destination)
        {
            var chdList = new List<ChangeHistoryDetail>();
            if (originalDeliveryMethod != deliveryMethod)
            {
                chdList.Add(new ChangeHistoryDetail
                {
                    ChangedColumn = "DeliveryMethod",
                    OriginalValue = originalDeliveryMethod == null ? null : originalDeliveryMethod.ToString(),
                    NewValue = deliveryMethod == null ? null : deliveryMethod.ToString()
                });
            }

            if (originalDestination != destination)
            {
                chdList.Add(new ChangeHistoryDetail
                    { ChangedColumn = "Destination", OriginalValue = originalDestination, NewValue = destination });
            }

            if (chdList.Any() == false)
            {
                return null;
            }

            var chiList = GenerateOrdersTrackingHistory(DateTime.Today.Date, chdList);
            if (chiList.Any() == false)
            {
                return null;
            }

            var history = chiList[0].HistoryText;
            if (chiList.Count() == 2)
            {
                history = history + char.ToString('\r') + chiList[1].HistoryText;
            }

            return history;
        }

        public List<ChangeHistoryItem> GenerateOrdersTrackingHistory(DateTime updatedDate,
            List<ChangeHistoryDetail> detailList = null)
        {
            var chiList = new List<ChangeHistoryItem>();
            if (detailList == null && ChangeHistoryDetail == null)
            {
                return chiList;
            }

            var chdList = detailList;
            if (chdList == null)
            {
                chdList = new List<ChangeHistoryDetail>();
                foreach (var chd in ChangeHistoryDetail) chdList.Add(chd);
            }

            if (chdList.Any() == false)
            {
                return chiList;
            }

            foreach (var chd in chdList)
            {
                string historyText = null;
                switch (chd.ChangedColumn)
                {
                    case "OTNoteText":
                        if (string.IsNullOrWhiteSpace(chd.NewValue) == false)
                        {
                            // Ignore OTNoteText history if it wes generated due to DeliveryMethod and/or Destination and/or PhysicianKey and/or PhysicianAddressKey change
                            if (chdList.Where(d => d.ChangedColumn == "DeliveryMethod").Any() ||
                                chdList.Where(d => d.ChangedColumn == "Destination").Any() ||
                                chdList.Where(d => d.ChangedColumn == "PhysicianKey").Any() ||
                                chdList.Where(d => d.ChangedColumn == "PhysicianAddressKey").Any())
                            {
                                break;
                            }

                            var chd2 = chdList.Where(d => d.ChangedColumn == "OTNoteType").FirstOrDefault();
                            if (chd2 == null ||
                                ExtractCode(chd2.NewValue) == "FollowupNote") // assume followup if type not in history
                            {
                                historyText = string.Format("Followup note{0}: {1}",
                                    BuildOnDateClause(updatedDate, chdList, "OTNoteDate"), chd.NewValue);
                            }
                        }

                        break;

                    case "PhysicianKey":
                        var chd3 = chdList.Where(d => d.ChangedColumn == "PhysicianAddressKey").FirstOrDefault();
                        if (chd3 == null)
                        {
                            historyText = string.Format("Physician changed from {0} to {1}",
                                ExtractPhysicianName(chd.OriginalValue), ExtractPhysicianName(chd.NewValue));
                        }
                        else
                        {
                            historyText = string.Format(
                                "Physician changed from {0} at address {1}   to   {2} at address {3}",
                                ExtractPhysicianName(chd.OriginalValue),
                                ExtractPhysicianAddressText(chd3.OriginalValue), ExtractPhysicianName(chd.NewValue),
                                ExtractPhysicianAddressText(chd3.NewValue));
                        }

                        break;

                    case "PhysicianAddressKey":
                        var chd4 = chdList.Where(d => d.ChangedColumn == "PhysicianKey").FirstOrDefault();
                        if (chd4 == null)
                        {
                            historyText = string.Format("Physician address changed from {0}   to   {1}",
                                ExtractPhysicianAddressText(chd.OriginalValue),
                                ExtractPhysicianAddressText(chd.NewValue));
                        }

                        break;

                    case "DeliveryMethod":
                        historyText = string.Format("Delivery Method changed from {0} to {1}{2}",
                            ExtractCodeDescription(chd.OriginalValue), ExtractCodeDescription(chd.NewValue),
                            BuildOnDateClause(updatedDate, chdList, "OTNoteDate"));
                        break;

                    case "Destination":
                        historyText = string.Format("Destination changed from {0} to {1}{2}",
                            ExtractString(chd.OriginalValue), ExtractString(chd.NewValue),
                            BuildOnDateClause(updatedDate, chdList, "OTNoteDate"));
                        break;

                    case "OrderDate":
                        historyText = string.Format("Changed Order Date from {0} to {1}",
                            ExtractDate(chd.OriginalValue), ExtractDate(chd.NewValue));
                        break;

                    case "NotMyPatient":
                        if (chd.OriginalValue != chd.NewValue)
                        {
                            if ((chd.OriginalValue == null || chd.OriginalValue == "0") && chd.NewValue == "1")
                            {
                                historyText = "Changed from my patient to not my patient";
                            }
                            else if ((chd.NewValue == null || chd.NewValue == "0") && chd.OriginalValue == "1")
                            {
                                historyText = "Changed from not my patient to my patient";
                            }
                        }

                        break;

                    case "NotMyOrder":
                        if (chd.OriginalValue != chd.NewValue)
                        {
                            if ((chd.OriginalValue == null || chd.OriginalValue == "0") && chd.NewValue == "1")
                            {
                                historyText = "Changed from my order to not my order";
                            }
                            else if ((chd.NewValue == null || chd.NewValue == "0") && chd.OriginalValue == "1")
                            {
                                historyText = "Changed from not my order to my order";
                            }
                        }

                        break;

                    case "Status":
                        // Ignore StatusDate column - reliy instead on the paretn ChangeHistory.ChangeHistoryHeader
                        historyText = string.Format("Status changed from {0} to {1}",
                            OrdersTrackingHelpers.StatusDescription(chd.OriginalValue),
                            OrdersTrackingHelpers.StatusDescription(chd.NewValue));
                        break;

                    case "Inactive":
                        if (chd.OriginalValue != chd.NewValue)
                        {
                            if ((chd.OriginalValue == null || chd.OriginalValue == "0") && chd.NewValue == "1")
                            {
                                historyText = "Inactivated the order";
                            }
                            else if ((chd.NewValue == null || chd.NewValue == "0") && chd.OriginalValue == "1")
                            {
                                historyText = "Reactivated the order";
                            }
                        }

                        break;
                }

                if (historyText != null)
                {
                    chiList.Add(new ChangeHistoryItem { HistoryText = historyText });
                }
            }

            return chiList;
        }

        private string ExtractPhysicianName(string s)
        {
            var key = 0;
            try
            {
                key = int.Parse(s);
            }
            catch
            {
            }

            var d = key == 0 ? null : PhysicianCache.Current.GetPhysicianFullNameInformalWithSuffixFromKey(key);
            return string.IsNullOrWhiteSpace(d) ? "none" : d;
        }

        private string ExtractPhysicianAddressText(string s)
        {
            var key = 0;
            try
            {
                key = int.Parse(s);
            }
            catch
            {
            }

            var d = key == 0 ? null : PhysicianCache.Current.GetPhysicianAddressTextFromKey(key);
            return string.IsNullOrWhiteSpace(d) ? "none" : d;
        }

        private string ExtractCode(string s)
        {
            var key = 0;
            try
            {
                key = int.Parse(s);
            }
            catch
            {
            }

            var d = key == 0 ? null : CodeLookupCache.GetCodeFromKey(key);
            return string.IsNullOrWhiteSpace(d) ? "none" : d;
        }

        private string ExtractCodeDescription(string s)
        {
            var key = 0;
            try
            {
                key = int.Parse(s);
            }
            catch
            {
            }

            var d = key == 0 ? null : CodeLookupCache.GetCodeDescriptionFromKey(key);
            return string.IsNullOrWhiteSpace(d) ? "none" : d;
        }

        private string ExtractString(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? "none" : s;
        }

        private string ExtractDate(string stringDateTime)
        {
            if (string.IsNullOrWhiteSpace(stringDateTime))
            {
                return "Unknown";
            }

            var split = stringDateTime.Split(' ');
            if (split.Any() == false)
            {
                return "Unknown";
            }

            return split[0];
        }

        private string BuildOnDateClause(DateTime updatedDate, List<ChangeHistoryDetail> detailList,
            string changedColumn)
        {
            var chd = detailList.Where(d => d.ChangedColumn == changedColumn).FirstOrDefault();
            if (chd == null)
            {
                return "";
            }

            var onDate = ExtractDate(chd.NewValue);
            return updatedDate.ToShortDateString() == onDate ? "" : ", dated " + onDate;
        }
    }
}