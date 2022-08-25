using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public static class IntegerValidations
    {
        public static ValidationResult NullableIntegerRequired(int? value, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { validationContext.MemberName };
            string displayName = (validationContext.DisplayName == null) ? validationContext.MemberName : validationContext.DisplayName;

            if (value.HasValue == false || value.GetValueOrDefault() == 0)
            {
                return new ValidationResult(string.Format("{0} is required.", displayName), memberNames);
            }

            return ValidationResult.Success;
        }

        public static ValidationResult IntegerRequired(int value, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { validationContext.MemberName };
            string displayName = (validationContext.DisplayName == null) ? validationContext.MemberName : validationContext.DisplayName;

            if (value <= 0)
            {
                return new ValidationResult(string.Format("{0} is required.", displayName), memberNames);
            }

            return ValidationResult.Success;
        }
    }
}
