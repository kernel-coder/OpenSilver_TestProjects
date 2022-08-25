using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public static class ICDCodeValidations
    {
        public static ValidationResult IsICDThruDateValid(ICDCode icd, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "EffectiveFrom", "EffectiveThru" };
            if ((icd.EffectiveThru.HasValue) && (!icd.EffectiveThru.Equals(DateTime.MinValue)))
            {
                if (DateTime.Compare((DateTime)icd.EffectiveFrom, (DateTime)icd.EffectiveThru) > 0)
                    return new ValidationResult("The effective thru date must be on or after the effective from date.", memberNames);
            }
            if (icd.EffectiveThru.Equals(DateTime.MinValue)) { icd.EffectiveThru = null; }
            return ValidationResult.Success;
        }
    }
}
