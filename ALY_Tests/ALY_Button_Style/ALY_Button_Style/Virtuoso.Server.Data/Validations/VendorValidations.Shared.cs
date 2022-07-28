using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
   using Virtuoso.Core.Cache;
#endif
namespace Virtuoso.Validation
{
    public static class VendorValidations
    {
        public static ValidationResult IsVendorAddressValid(Vendor vendor, ValidationContext validationContext)
        {
            // If any part of the address is entered - Address1, City, StateCode and ZipCode are required
            if ((!(string.IsNullOrEmpty(vendor.Address1))) ||
                (!(string.IsNullOrEmpty(vendor.Address2))) ||
                (!(string.IsNullOrEmpty(vendor.City))) ||
                (!(vendor.StateCode == null)) ||
                (!(string.IsNullOrEmpty(vendor.ZipCode))))
            {
                if ((string.IsNullOrEmpty(vendor.Address1)) ||
                    (string.IsNullOrEmpty(vendor.City)) ||
                    (vendor.StateCode == null) ||
                    (string.IsNullOrEmpty(vendor.ZipCode)))
                {
                    string[] memberNames = new string[] { "ZipCode" };
                    if (string.IsNullOrEmpty(vendor.Address1)) memberNames = new string[] { "Address1" };
                    else if (string.IsNullOrEmpty(vendor.City)) memberNames = new string[] { "City" };
                    else if (vendor.StateCode == null) memberNames = new string[] { "StateCode" };
                    return new ValidationResult("A vendor address must contain Addesss, City, State and ZipCode.", memberNames);
                }
            }
            return ValidationResult.Success;
        }
        public static ValidationResult IsVendorNameValid(Vendor vendor, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (VendorCache.VendorNameInUse(vendor))
            {
                    string[] memberNames = new string[] { "VendorName" };
                    return new ValidationResult("Vendor name is already in use.", memberNames);
            }
#endif
            return ValidationResult.Success;
        }
        public static ValidationResult VendorValidateTeleMonitorStartDate(Vendor vendor, ValidationContext validationContext)
        {
            if (vendor == null) return ValidationResult.Success;
            if (string.IsNullOrWhiteSpace(vendor.VendorType)) return ValidationResult.Success;
            if (vendor.VendorType.Trim().ToLower().Contains("telemonitor") == false) return ValidationResult.Success;
            if (vendor.TeleMonitorStartDate == null)
            {
                string[] memberNames = new string[] { "TeleMonitorStartDate" };
                return new ValidationResult("The TeleMonitor Start Date field is required", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult VendorValidateTeleMonitorServiceTypeKey(Vendor vendor, ValidationContext validationContext)
        {
            if (vendor == null) return ValidationResult.Success;
            if (string.IsNullOrWhiteSpace(vendor.VendorType)) return ValidationResult.Success;
            if (vendor.VendorType.Trim().ToLower().Contains("telemonitor") == false) return ValidationResult.Success;
            int key = (vendor.TeleMonitorServiceTypeKey == null) ? 0 : (int)vendor.TeleMonitorServiceTypeKey;
            if (key == 0)
            {
                string[] memberNames = new string[] { "TeleMonitorServiceTypeKey" };
                return new ValidationResult("The TeleMonitor Service Type field is required", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult VendorValidateTeleMonitorUserID(Vendor vendor, ValidationContext validationContext)
        {
            if (vendor == null) return ValidationResult.Success;
            if (string.IsNullOrWhiteSpace(vendor.VendorType)) return ValidationResult.Success;
            if (vendor.VendorType.Trim().ToLower().Contains("telemonitor") == false) return ValidationResult.Success;
            if (vendor.TeleMonitorUserID == null)
            {
                string[] memberNames = new string[] { "TeleMonitorUserID" };
                return new ValidationResult("The TeleMonitoring User field is required", memberNames);
            }
            return ValidationResult.Success;
        }
    }
}
