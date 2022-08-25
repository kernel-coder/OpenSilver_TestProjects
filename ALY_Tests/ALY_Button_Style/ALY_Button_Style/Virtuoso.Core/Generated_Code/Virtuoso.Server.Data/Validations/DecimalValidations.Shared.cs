using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
	public static class DecimalValidations
	{
		public static ValidationResult PositiveQuarterHoursOnly(decimal? value, ValidationContext validationContext)
		{
			string[] memberNames = new string[] { validationContext.MemberName };
			string displayName = (validationContext.DisplayName == null) ? validationContext.MemberName : validationContext.DisplayName;

			if (value.HasValue)
			{
				var x = value.Value * 4;

				if (x < 0)
				{
					return new ValidationResult(string.Format("{0} cannot be negative.", displayName), memberNames);
				}

				if (x > 0)
				{
					if (Decimal.Truncate(x) != x)
					{
						return new ValidationResult(string.Format("{0} must be in quarter hours increments.", displayName), memberNames);
					}
				}
			}

			return ValidationResult.Success;
		}

        public static ValidationResult DecimalGreaterThanZero(decimal value, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { validationContext.MemberName };
            string displayName = (validationContext.DisplayName == null) ? validationContext.MemberName : validationContext.DisplayName;
            var x = value;
            if (x <= 0m)
                return new ValidationResult(string.Format("{0} is required.", displayName), memberNames);
            return ValidationResult.Success;
        }
	}
}
