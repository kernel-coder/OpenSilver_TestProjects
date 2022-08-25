#region Usings

using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class OrdersTracking
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

        public string OTNoteTypeCode
        {
            get
            {
                var result = CodeLookupCache.GetCodeFromKey(OTNoteType);
                return result;
            }
        }

        public string OTNoteTypeCodeDescription
        {
            get
            {
                var result = CodeLookupCache.GetCodeDescriptionFromKey(OTNoteType);
                return result;
            }
        }

        public bool IsOTNoteTypeFollowupNote => OTNoteTypeCode == "FollowupNote";

        public bool IsOTNoteTypeChangePhysician => OTNoteTypeCode == "ChangePhysician";

        public bool IsOTNoteTypeOverrideDelivery => OTNoteTypeCode == "OverrideDelivery";

        public bool IsOTNoteTextReadOnly => !IsOTNoteTypeFollowupNote &&
                                            !(IsOTNoteTypeChangePhysician && !string.IsNullOrWhiteSpace(OTNoteText));

        public bool IsOTNoteDateReadOnly => IsOTNoteTypeChangePhysician;

        public string RecordedBy
        {
            get
            {
                var user = UserCache.Current.GetFullNameWithSuffixFromUserId(UpdatedBy);
                return string.IsNullOrWhiteSpace(user) ? "?" : user;
            }
        }

        public int OriginalPhysicianKey { get; set; }
        public int? OriginalPhysicianAddressKey { get; set; }

        public string PhysicianName
        {
            get
            {
                var s = PhysicianCache.Current.GetPhysicianFullNameInformalWithSuffixFromKey(PhysicianKey);
                return string.IsNullOrWhiteSpace(s) ? "Unknown" : s;
            }
        }

        public string PhysicianAddressType
        {
            get
            {
                var pa = PhysicianCache.Current.GetPhysicianAddressFromKey(PhysicianAddressKey);
                if (pa == null)
                {
                    return "Unknown";
                }

                return string.IsNullOrWhiteSpace(pa.TypeDescription) ? "Unknown" : pa.TypeDescription;
            }
        }

        public string OriginalDestination { get; set; }
        public int? OriginalDeliveryMethod { get; set; }

        public string DeliveryMethodCode => CodeLookupCache.GetCodeFromKey(DeliveryMethod);

        public string OrdersTrackingStatusDescription
        {
            get
            {
                switch (Status)
                {
                    case (int)OrdersTrackingStatus.ReadyForReview:
                        return "Order Review";
                    case (int)OrdersTrackingStatus.Complete:
                        return "Complete";
                    case (int)OrdersTrackingStatus.ReadyForPrint:
                        return "Ready To Print";
                    case (int)OrdersTrackingStatus.Sent:
                        return "Sent";
                    case (int)OrdersTrackingStatus.Signed:
                        return "Signed";
                    case (int)OrdersTrackingStatus.Void:
                        return "Voided";
                    case (int)OrdersTrackingStatus.Edit:
                    default:
                        return "Started";
                }
            }
        }

        private string GetNoteTypeString(NoteType nt)
        {
            switch (nt)
            {
                case NoteType.ChangePhysician: return "ChangePhysician";
                case NoteType.FollowupNote: return "FollowupNote";
                case NoteType.OverrideDelivery: return "OverrideDelivery";
                default: return "Unknown";
            }
        }

        public void SetupNoteFollowupNote()
        {
            SetupNote(NoteType.FollowupNote);
        }

        public void SetupNoteChangePhysician(int originalPhysicianKey, int? originalPhysicianAddressKey,
            int physicianKey, int? physicianAddressKey)
        {
            OriginalPhysicianKey = originalPhysicianKey;
            OriginalPhysicianAddressKey = originalPhysicianAddressKey;
            PhysicianKey = physicianKey;
            PhysicianAddressKey = physicianAddressKey;

            SetupNote(NoteType.ChangePhysician);

            ChangePhysicianOTNoteText();
        }

        public void SetupNoteOverrideDelivery(int? deliveryMethod, string destination, int physicianKey,
            int? physicianAddressKey)
        {
            if (OTNoteType == CodeLookupCache.GetKeyFromCode("OrdersTrackingNoteType", "ChangePhysician"))
            {
                return;
            }

            SetupNote(NoteType.OverrideDelivery);

            OriginalDeliveryMethod = deliveryMethod;
            DeliveryMethod = deliveryMethod;
            OriginalDestination = destination;
            Destination = destination;
            PhysicianKey = physicianKey;
            PhysicianAddressKey = physicianAddressKey;

            OverrideDeliveryOTNoteText();
        }

        private void SetupNote(NoteType nt)
        {
            var otNoteTypeCode = GetNoteTypeString(nt);
            var otNoteType = CodeLookupCache.GetKeyFromCode("OrdersTrackingNoteType", otNoteTypeCode);
            if (otNoteType == null)
            {
                MessageBox.Show(string.Format(
                    "Error OrdersTracking.SetopNote: CodeLookup Code '{0}' for CodeType 'OrdersTrackingNoteType' is not defined.  Contact your system administrator.",
                    otNoteTypeCode));
                return;
            }

            OTNoteType = otNoteType;
            OTNoteDate = DateTime.Today.Date;
            OTNoteText = null;
            if (nt == NoteType.ChangePhysician)
            {
                var ch = new ChangeHistoryInfo();
                OTNoteText = ch.GenerateOrdersTrackingChangePhysicianHistory(OriginalPhysicianKey,
                    OriginalPhysicianAddressKey, PhysicianKey, PhysicianAddressKey);
            }

            UpdatedBy = WebContext.Current.User.MemberID;
        }

        public bool ValidateNote()
        {
            ValidationErrors.Clear();
            var ret = true;

            if ((IsOTNoteTypeFollowupNote || IsOTNoteTypeChangePhysician) && string.IsNullOrWhiteSpace(OTNoteText))
            {
                ValidationErrors.Add(new ValidationResult("The Note Text field is required.", new[] { "OTNoteText" }));
                ret = false;
            }

            if (IsOTNoteTypeChangePhysician) // Early Exit If Change Physician Note
            {
                return ret;
            }

            if (OTNoteDate == DateTime.MinValue)
            {
                OTNoteDate = null;
            }

            if (OTNoteDate == null)
            {
                ValidationErrors.Add(new ValidationResult("The Note Date field is required.", new[] { "OTNoteDate" }));
                ret = false;
            }
            else
            {
                OTNoteDate = ((DateTime)OTNoteDate).Date;
            }

            if (OTNoteDate != null && ((DateTime)OTNoteDate).Date > DateTime.Today.Date)
            {
                ValidationErrors.Add(new ValidationResult("The Note Date cannot be a future date",
                    new[] { "OTNoteDate" }));
                ret = false;
            }

            if (IsOTNoteTypeOverrideDelivery && string.IsNullOrWhiteSpace(OverrideDeliveryOTNoteText()))
            {
                ValidationErrors.Add(new ValidationResult("To save, you must override the Order Delivery Method.",
                    new[] { "DeliveryMethod" }));
                ret = false;
            }

            if (IsOTNoteTypeOverrideDelivery && string.IsNullOrWhiteSpace(DeliveryMethodCode))
            {
                ValidationErrors.Add(new ValidationResult("The Delivery Method Text field is required.",
                    new[] { "DeliveryMethod" }));
                ret = false;
            }

            if (IsOTNoteTypeOverrideDelivery && (DeliveryMethodCode == "HIE" || DeliveryMethodCode == "Portal"))
            {
                ValidationErrors.Add(new ValidationResult(
                    "The Order Delivery Method cannot be changed to HIE or Portal.", new[] { "DeliveryMethod" }));
                ret = false;
            }

            return ret;
        }

        partial void OnOTNoteTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaiseChanges();
        }

        public void RaiseChanges()
        {
            RaisePropertyChanged("OTNoteTypeCode");
            RaisePropertyChanged("OTNoteTypeCodeDescription");
            RaisePropertyChanged("IsOTNoteTypeFollowupNote");
            RaisePropertyChanged("IsOTNoteTypeChangePhysician");
            RaisePropertyChanged("IsOTNoteTypeOverrideDelivery");
            RaisePropertyChanged("IsOTNoteTextReadOnly");
            RaisePropertyChanged("IsOTNoteDateReadOnly");
        }

        partial void OnUpdatedByChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("RecordedBy");
        }

        partial void OnPhysicianKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PhysicianName");
        }

        partial void OnPhysicianAddressKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PhysicianAddressType");
        }

        public string ChangePhysicianOTNoteText()
        {
            var ch = new ChangeHistoryInfo();
            OTNoteText = ch.GenerateOrdersTrackingChangePhysicianHistory(OriginalPhysicianKey,
                OriginalPhysicianAddressKey, PhysicianKey, PhysicianAddressKey);
            return OTNoteText;
        }

        partial void OnDestinationChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            OverrideDeliveryOTNoteText();
        }

        partial void OnDeliveryMethodChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DeliveryMethodCode");
            OverrideDeliveryOTNoteText();
        }

        public string OverrideDeliveryOTNoteText()
        {
            var ch = new ChangeHistoryInfo();
            OTNoteText = ch.GenerateOrdersTrackingOverrideDeliveryHistory(OriginalDeliveryMethod, OriginalDestination,
                DeliveryMethod, Destination);
            return OTNoteText;
        }

        private enum NoteType
        {
            FollowupNote,
            ChangePhysician,
            OverrideDelivery
        }
    }
}