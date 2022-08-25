using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Server.Data;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Virtuoso.Validation
{
    public static class NonServiceTypeValidations
    {
        public static ValidationResult ValidateUniqueID(NonServiceType CurrentNonServiceType, ValidationContext validationContext)
        {
            ValidationResult result = ValidationResult.Success;

            if (!CurrentNonServiceType.Inactive)
            {
                var nonServiceTypeDataProvider = validationContext.GetService(typeof(INonServiceTypeDataProvider)) as INonServiceTypeDataProvider;

                IQueryable<NonServiceType> nonServiceType = null;

                if (nonServiceTypeDataProvider != null)
                {
                    nonServiceType = nonServiceTypeDataProvider.GetDuplicateNonServiceType(CurrentNonServiceType);

                    if (nonServiceType.Any(n => (n.NonServiceTypeKey != CurrentNonServiceType.NonServiceTypeKey) && !n.Inactive))
                    {
                        string[] memberNames = new string[] { "NonServiceTypeID" };
                        result = new ValidationResult("Cannot enter a duplicate Non Service Type ID", memberNames);
                    }
                }
            }

            return result;
        }
        public static ValidationResult ValidateServiceLineTypeUseBits(int? ServiceLineTypeUseBits, ValidationContext validationContext)
        {
            if (ServiceLineTypeUseBits == null || ServiceLineTypeUseBits == 0)
            {
                string[] memberNames = new string[] { "ServiceLineTypeUseBits" };
                return new ValidationResult("At least one Non Service Use must be checked.", memberNames);
            }
            return ValidationResult.Success;
        }
    }
}
