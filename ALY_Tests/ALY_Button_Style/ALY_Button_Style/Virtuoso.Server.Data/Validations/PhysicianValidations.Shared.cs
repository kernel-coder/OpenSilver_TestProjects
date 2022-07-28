using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public static class PhysicianValidations
    {
        public static ValidationResult ValidateServiceLineTypeUseBits(int? ServiceLineTypeUseBits, ValidationContext validationContext)
        {
            if (ServiceLineTypeUseBits == null || ServiceLineTypeUseBits == 0)
            {
                string[] memberNames = new string[] { "ServiceLineTypeUseBits" };
                return new ValidationResult("At least one Physician Use must be checked.", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult IsPhysicianAddressValid(PhysicianAddress pa, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "EffectiveFromDate" };
            if (pa.EffectiveFromDate.HasValue && pa.EffectiveThruDate.HasValue)
            {
                if (!pa.EffectiveFromDate.Equals(DateTime.MinValue) && (!pa.EffectiveThruDate.Equals(DateTime.MinValue)))
                    if (DateTime.Compare((DateTime)pa.EffectiveFromDate, (DateTime)pa.EffectiveThruDate) > 0)
                        return new ValidationResult("The effective thru date must be on or after the effective from date.", memberNames);
            }
            if (pa.EffectiveFromDate.Equals(DateTime.MinValue)) { pa.EffectiveFromDate = null; }
            if (pa.EffectiveThruDate.Equals(DateTime.MinValue)) { pa.EffectiveThruDate = null; }
            return ValidationResult.Success;
        }

        public static ValidationResult IsNPIValid(string NPI, ValidationContext validationContext)
        {
            // Validate the NPI using Luhn check digit formula
            /*
            The Luhn check digit formula is calculated as follows: 
  
             - Double the value of alternate digits beginning with the rightmost digit. 
             - Add the individual digits of the products resulting from step 1 to the 
               unaffected digits from the original number. 
             - Subtract the total obtained in step 2 from the next higher number ending
               in zero. This is the check digit. If the total obtained in step 2 is a 
               number ending in zero, the check digit is zero. 

            Example of Check Digit Calculation for NPI used as Card Issuer Identifier 
            Assume the 9-position identifier part of the NPI is 123456789. 
            If used as a card issuer identifier on a standard health identification card
            the full number would be 80840123456789. 
            Using the Luhn formula on the identifier portion, the check digit is calculated
            as follows: 
            Card issuer identifier without check digit: 8 0 8 4 0 1 2 3 4 5 6 7 8 9 
            - Step 1: Double the value of alternate digits, beginning with the rightmost digit: 
              0 8 2 6 10 14 18 
            - Step 2: Add the individual digits of products of doubling, plus unaffected digits. 
              8 + 0 + 8 + 8 + 0 + 2 + 2 + 6 + 4 + 1 + 0 + 6 + 1 + 4 + 8 + 1 + 8 = 67 
            - Step 3: Subtract from next higher number ending in zero. 
              70 - 67 = 3 
              Check digit = 3 
              Card issuer identifier with check digit = 808401234567893 

            Example of Check Digit Calculation for NPI used without Prefix 
            Assume the 9-position identifier part of the NPI is 123456789. 
            Using the Luhn formula on the identifier portion, the check digit is calculated as follows: 
            NPI without check digit:  1 2 3 4 5 6 7 8 9 
            - Step 1: Double the value of alternate digits, beginning with the rightmost digit. 
              2 6 10 14 18 
            - Step 2: Add constant 24, to account for the 80840 prefix that would be present
              on a card issuer identifier, plus the individual digits of products of doubling,
              plus unaffected digits. 
              24 + 2 + 2 + 6 + 4 + 1 + 0 + 6 + 1 + 4 + 8 + 1 + 8 = 67 
            - Step 3: Subtract from next higher number ending in zero. 
              70 - 67 = 3 
              Check digit = 3 
              NPI with check digit = 1234567893 
            */
            string[] memberNames = new string[] { validationContext.MemberName };
            int i, n, sum = 24;

            // A NULL NPI is a valid NPI
            if (string.IsNullOrEmpty(NPI)) return ValidationResult.Success;
            // NPI must be a 10 digit number.  
            if ((NPI.Length != 10) || (!IsNumeric(NPI)))
                return new ValidationResult("NPI must be a ten digit number.", memberNames);

            // As Tally the even digits (times 2) and the odd digits
            // pretending the NPI is prefixed with '80840' and ignoring the last/check-digit              
            for (i = 0; i < 9; i++)
            {
                // convert charater in NPI to a number - and double the even numbers
                n = ((Int32.Parse(NPI.Substring(i, 1))) * (((i % 2) == 0) ? 2 : 1));
                // add the number(s) to the sum
                // to add digits in numbers > 9 (2 digit numbers) - just subtract 9. e.g.:
                // 10 = 1+0 = 1 = 10-9...  12 = 1+2 = 3 = 12-9...  14 = 1+4 = 5 = 14-9...
                sum += n - ((n < 10) ? 0 : 9);
            }
            // the check digit is sum rounded to next 10's minus itself, e.g.:
            // sum = 67, then check digit = 70-67 = 3
            // the check digit is sum rounded to next 10's minus itself, e.g.:
            // sum = 67, then check digit = 70-67 = 3
            // error if NPI has wrong check digit, otherwise exit success
            if (Int32.Parse(NPI.Substring(9, 1)) != ((10 - (sum % 10)) % 10))
                return new ValidationResult("Invalid NPI.", memberNames);
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

