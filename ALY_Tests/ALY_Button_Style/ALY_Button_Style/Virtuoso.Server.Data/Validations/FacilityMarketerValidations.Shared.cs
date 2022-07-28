using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public static class FacilityMarketerValidations
    {
        public static ValidationResult IsEndDateValid(FacilityMarketer fm, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "StartDate", "EndDate" };
            if ((fm.EndDate.HasValue) && (!fm.EndDate.Equals(DateTime.MinValue)))
            {
                if (DateTime.Compare((DateTime)fm.StartDate, (DateTime)fm.EndDate) > 0)
                    return new ValidationResult("The end date must be on or after the start date.", memberNames);
            }
            if (fm.EndDate.Equals(DateTime.MinValue)) { fm.EndDate = null; }
            return ValidationResult.Success;
        }
    }
}
