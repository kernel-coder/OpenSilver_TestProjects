using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public static class TenantSettingValidations
    {
        public static ValidationResult IsTenantSettingAddressValid(TenantSetting ts, ValidationContext validationContext)
        {
            // If any part of the address is entered - Address1, City, StateCode and ZipCode are required
            if ((!(string.IsNullOrEmpty(ts.Address1))) ||
                (!(string.IsNullOrEmpty(ts.Address2))) ||
                (!(string.IsNullOrEmpty(ts.City))) ||
                (!(ts.StateCode == null)) ||
                (!(string.IsNullOrEmpty(ts.ZipCode))))
            {
                if ((string.IsNullOrEmpty(ts.Address1)) ||
                    (string.IsNullOrEmpty(ts.City)) ||
                    (ts.StateCode == null) ||
                    (string.IsNullOrEmpty(ts.ZipCode)))
                {
                    string[] memberNames = new string[] { "ZipCode" };
                    if (string.IsNullOrEmpty(ts.Address1)) memberNames = new string[] { "Address1" };
                    else if (string.IsNullOrEmpty(ts.City))memberNames = new string[] { "City" };
                    else if (ts.StateCode == null)memberNames = new string[] { "StateCode" };
                    return new ValidationResult("An Agency address must contain Addesss, City, State and ZipCode.", memberNames);
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsQIOPhoneNumberRequired(TenantSetting ts, ValidationContext validationContext)
        {
            // If the QIO Name is entered - QIO Phone is required
            if (!string.IsNullOrEmpty(ts.QIOName))
            {
                if (string.IsNullOrEmpty(ts.QIOPhoneNumber))
                {
                    return new ValidationResult("QIO Toll Free number is required for the QIO Name of " + ts.QIOName, new string[] { "QIOPhoneNumber" });
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsQIONameRequired(TenantSetting ts, ValidationContext validationContext)
        {
            // If the QIO Phone is entered - QIO Name is required
            if (!string.IsNullOrEmpty(ts.QIOPhoneNumber))
            {
                if (string.IsNullOrEmpty(ts.QIOName))
                {
                    return new ValidationResult("QIO Name is required for the QIO Toll Free Number of " + ts.QIOPhoneNumber, new string[] { "QIOName" });
                }               
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsEnvelopeWindowRequired(string envelopeWindow, ValidationContext validationContext)
        {
            var ts = validationContext.ObjectInstance as TenantSetting;
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (ts != null && ts.PrintInterimOrderForMailing)
            {
                if (string.IsNullOrEmpty(envelopeWindow))
                    return new ValidationResult("Envelope Window is required when 'Print Interim Order for Mailing' checked", new string[] { "EnvelopeWindow" });
                else
                    return ValidationResult.Success;
            }
#endif
            return ValidationResult.Success;
        }
    }

}
