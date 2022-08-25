using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;
using System.Diagnostics;

namespace Virtuoso.Validation
{
    public static class ServiceLineValidations
    {
        public static ValidationResult IsGoLiveValid(DateTime? goLiveDate, ValidationContext validationContext)
        {
            try
            {
                var serviceLine = validationContext.ObjectInstance as ServiceLine;
                if (serviceLine.ServiceLineGrouping == null)  //prevent validation from running server-side
                    return ValidationResult.Success;
                string[] memberNames = new string[] { validationContext.MemberName };
                var existsOnServiceLineGrouping = serviceLine.ServiceLineGrouping.Any(slg => slg.GoLiveDate.HasValue);
                if (goLiveDate.HasValue && existsOnServiceLineGrouping)
                    return new ValidationResult("Cannot set Go Live Date, because it is already defined on Service Line Grouping", memberNames);
                return ValidationResult.Success;
            }
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
                return ValidationResult.Success;
            }
        }

        public static ValidationResult IsServiceLineValid(ServiceLine serviceLine, ValidationContext validationContext)
        {
            // If any part of the address is entered - Address1, City, StateCode and ZipCode are required
            if ((!(string.IsNullOrEmpty(serviceLine.Address1))) ||
                (!(string.IsNullOrEmpty(serviceLine.Address2))) ||
                (!(string.IsNullOrEmpty(serviceLine.City))) ||
                (!(serviceLine.StateCode == null)) ||
                (!(string.IsNullOrEmpty(serviceLine.ZipCode))))
            {
                if ((string.IsNullOrEmpty(serviceLine.Address1)) ||
                    (string.IsNullOrEmpty(serviceLine.City)) ||
                    (serviceLine.StateCode == null) ||
                    (string.IsNullOrEmpty(serviceLine.ZipCode)))
                {
                    string[] memberNames = new string[] { "Address1", "City", "StateCode", "ZipCode" };
                    return new ValidationResult("An address must contain Addesss, City, State and ZipCode.", memberNames);
                }
            }
            // You cannot have a phone extension without a phone number
            if ((!(string.IsNullOrEmpty(serviceLine.PhoneExtension))) && (string.IsNullOrEmpty(serviceLine.Number)))
            {
                string[] memberNames = new string[] { "Number", "PhoneExtension" };
                return new ValidationResult("You cannot have a phone extension without a phone number.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult OasisHeaderKeyValidation(ServiceLineGrouping serviceLineGrouping,  ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (serviceLineGrouping == null) return ValidationResult.Success;
            int key = (serviceLineGrouping.OasisHeaderKey == null) ? 0 : (int)serviceLineGrouping.OasisHeaderKey;
            if (key != 0) return ValidationResult.Success;
            if (serviceLineGrouping.IsOasisHeaderRequired)
            {
                string[] memberNames = new string[] { "OasisHeaderKey" };
                return new ValidationResult("The CMS Header field is required on service line group zero", memberNames);
            }
#endif
            return ValidationResult.Success;
        }


    }
}
