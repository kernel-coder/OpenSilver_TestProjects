using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;
namespace Virtuoso.Validation
{
    public static class DisciplineValidations
    {
        public static ValidationResult ValidateServiceLineTypeUseBits(int? ServiceLineTypeUseBits, ValidationContext validationContext)
        {
            if (ServiceLineTypeUseBits == null || ServiceLineTypeUseBits == 0)
            {
                string[] memberNames = new string[] { "ServiceLineTypeUseBits" };
                return new ValidationResult("At least one Discipline Use must be checked.", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult ValidateHCFACode(string HCFACode, ValidationContext validationContext)
        {
            Discipline dscp = validationContext.ObjectInstance as Discipline;

            if (dscp.Locked == true) return ValidationResult.Success; //Disciplines can only be Locked by Delta via database, cannot set in UI

            if ((string.IsNullOrWhiteSpace(HCFACode) == false) && (HCFACode.Trim().ToLower() == "p"))
            {
                string[] memberNames = new string[] { "HCFACode" };
                return new ValidationResult("The Discipline Group Code cannot be 'P'.  'P' is reserved for future use (as physician).", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult ValidateBillCodes(BillCodes billCodes, ValidationContext validationContext)
        {
            if (billCodes == null) return ValidationResult.Success;
            if ((billCodes.BillCodeType <= 0)
                && (!string.IsNullOrEmpty(billCodes.BillingID)
                    || !string.IsNullOrEmpty(billCodes.Modifier)
                   )
               )
            {
                string[] memberNames = new string[] { "BillCodeType" };
                return new ValidationResult("Code Type is required", memberNames);
            }
            if ((billCodes.BillCodeType > 0)
                && string.IsNullOrEmpty(billCodes.BillingID)
                && !string.IsNullOrEmpty(billCodes.Modifier)
               )
            {
                string[] memberNames = new string[] { "BillingID" };
                return new ValidationResult("Billing Code is required", memberNames);
            }
            return ValidationResult.Success;
        }
    }
}
