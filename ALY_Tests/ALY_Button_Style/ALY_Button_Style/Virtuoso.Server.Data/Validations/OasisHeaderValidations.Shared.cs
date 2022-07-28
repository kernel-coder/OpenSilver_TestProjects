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
    public static class OasisHeaderValidations
    {
        public static ValidationResult IsOasisHeaderAddressValid(OasisHeader oasisHeader, ValidationContext validationContext)
        {
            // If any part of the address is entered - Address1, City, StateCode and ZipCode are required
            if ((!(string.IsNullOrEmpty(oasisHeader.Address1))) ||
                (!(string.IsNullOrEmpty(oasisHeader.Address2))) ||
                (!(string.IsNullOrEmpty(oasisHeader.City))) ||
                (!(oasisHeader.StateCode == null)) ||
                (!(string.IsNullOrEmpty(oasisHeader.ZipCode))))
            {
                if ((string.IsNullOrEmpty(oasisHeader.Address1)) ||
                    (string.IsNullOrEmpty(oasisHeader.City)) ||
                    (oasisHeader.StateCode == null) ||
                    (string.IsNullOrEmpty(oasisHeader.ZipCode)))
                {
                    string[] memberNames = new string[] { "ZipCode" };
                    if (string.IsNullOrEmpty(oasisHeader.Address1)) memberNames = new string[] { "Address1" };
                    else if (string.IsNullOrEmpty(oasisHeader.City)) memberNames = new string[] { "City" };
                    else if (oasisHeader.StateCode == null) memberNames = new string[] { "StateCode" };
                    return new ValidationResult("A oasisHeader address must contain Addesss, City, State and ZipCode.", memberNames);
                }
            }
            return ValidationResult.Success;
        }
        public static ValidationResult IsOasisHeaderNameValid(OasisHeader oasisHeader, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (OasisHeaderCache.OasisHeaderNameInUse(oasisHeader))
            {
                    string[] memberNames = new string[] { "OasisHeaderName" };
                    return new ValidationResult("CMS Header name is already in use.", memberNames);
            }
#endif
            return ValidationResult.Success;
        }
        public static ValidationResult IsOasisHeaderIDsValid(OasisHeader oasisHeader, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (OasisHeaderCache.OasisHeaderIDsInUse(oasisHeader))
            {
                    string[] memberNames = new string[] { "HHAAgencyID" };
                    return new ValidationResult("CMS Header NPI, CMS Certification Number, Unique HHA Agency ID Code and Branch ID Number combination is already in use.", memberNames);
            }
#endif
            return ValidationResult.Success;
        }
        public static ValidationResult IsFederalTaxIdValid(string federalTaxId, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { validationContext.MemberName };
            // A NULL FederalTaxId is a valid FederalTaxId
            if (string.IsNullOrEmpty(federalTaxId)) return ValidationResult.Success;
            //SSN must be a 9 digit number.  
            if ((federalTaxId.Length != 9) || (!IsNumeric(federalTaxId)))
                return new ValidationResult("Federal Tax Id must be a nine digit number.", memberNames);

            return ValidationResult.Success;
        }
        private static bool IsNumeric(string s)
        {
            try { Int64.Parse(s); }
            catch { return false; }
            return true;
        }
    }
}

