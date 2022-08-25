#region Usings

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Core.Behaviors;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class EncounterVendor
    {
        [Display(Name = "Phone Number")]
        public string PhoneNumber
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Number))
                {
                    return TextBoxFilters.FormatPhoneText(Number);
                }

                return string.Format("{0} {1}", TextBoxFilters.FormatPhoneText(Number), PhoneExtension);
            }
        }

        [Display(Name = "Contact Name")]
        public string ContactFullName
        {
            get
            {
                var name = string.Format("{0}, {1}", ContactLastName, ContactFirstName).Trim();
                if (name == "," || name == "")
                {
                    name = " ";
                }

                return name;
            }
        }

        partial void OnNumberChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PhoneNumber");
        }

        partial void OnPhoneExtensionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PhoneNumber");
        }

        partial void OnContactFirstNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ContactFullName");
        }

        partial void OnContactLastNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ContactFullName");
        }
    }

    public partial class Vendor
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

        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(VendorName))
                {
                    return string.Format("Vendor {0}", VendorName.Trim());
                }

                return IsNew ? "New Vendor" : "Edit Vendor";
            }
        }

        public string StateCodeCode => CodeLookupCache.GetCodeFromKey(StateCode);

        public string CityStateZip => FormatHelper.FormatCityStateZip(City, StateCodeCode, ZipCode);

        public bool IsVendorTypeTeleMonitor
        {
            get
            {
                if (string.IsNullOrWhiteSpace(VendorType))
                {
                    return false;
                }

                return VendorType.Trim().ToLower().Contains("telemonitor") ? true : false;
            }
        }

        public bool IsVendorTypePharmacy
        {
            get
            {
                if (string.IsNullOrWhiteSpace(VendorType))
                {
                    return false;
                }

                return VendorType.Trim().ToLower().Contains("pharmacy") ? true : false;
            }
        }

        public List<UserProfile> TeleMonitoringUsersList =>
            UserCache.Current.GetActiveTeleMonitoringUsersPlusMe(TeleMonitorUserID);

        public List<ServiceType> TeleMonitoringServiceTypeList =>
            ServiceTypeCache.GetActiveTeleMonitoringVisitServiceTypesPlusMe(TeleMonitorServiceTypeKey);

        public override string ToString()
        {
            return VendorName;
        }

        partial void OnVendorNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TabHeader");
        }

        partial void OnCityChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
        }

        partial void OnStateCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
            RaisePropertyChanged("StateCodeCode");
        }

        partial void OnZipCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
        }

        partial void OnVendorTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsVendorTypeTeleMonitor");
            if (IsVendorTypeTeleMonitor)
            {
                return;
            }

            TeleMonitorNoPreAdmit = false;
            TeleMonitorStartDate = null;
            TeleMonitorServiceTypeKey = null;
            TeleMonitorUserID = null;
        }
    }
}