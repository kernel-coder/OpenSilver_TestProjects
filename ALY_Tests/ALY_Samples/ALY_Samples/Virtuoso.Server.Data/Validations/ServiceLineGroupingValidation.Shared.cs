using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public static class ServiceLineGroupingValidations
    {
        public static ValidationResult IsGoLiveValid(DateTime? goLiveDate, ValidationContext validationContext)
        {
            try
            {
                var serviceLineGrouping = validationContext.ObjectInstance as ServiceLineGrouping;
                if (serviceLineGrouping.ServiceLine == null || serviceLineGrouping.ServiceLineGroupHeader == null)  //prevent validation from running server-side
                    return ValidationResult.Success;
                string[] memberNames = new string[] { validationContext.MemberName };
                //Check if set on Service Line
                var existsOnServiceLine = serviceLineGrouping.ServiceLine.GoLiveDate.HasValue;
                if (goLiveDate.HasValue && existsOnServiceLine)
                    return new ValidationResult("Cannot set Go Live Date, because it is already defined on Service Line", memberNames);

                //Check if set on any other Service Line Grouping level
                var currLevel = serviceLineGrouping.ServiceLineGroupHeader.SequenceNumber;
                var existsOnOtherLevel = serviceLineGrouping.ServiceLine.ServiceLineGrouping.Any(slg => slg.ServiceLineGroupHeader.SequenceNumber != currLevel && slg.GoLiveDate.HasValue);
                if (goLiveDate.HasValue && existsOnOtherLevel)
                {
                    var groupLabel = serviceLineGrouping.ServiceLine.ServiceLineGrouping.Where(slg => slg.ServiceLineGroupHeader.SequenceNumber != currLevel && slg.GoLiveDate.HasValue).Select(slg => slg.ServiceLineGroupHeader.GroupHeaderLabel).FirstOrDefault();
                    return new ValidationResult(string.Format("Cannot set Go Live Date, because it is already defined at grouping - {0}", groupLabel), memberNames);
                }

                return ValidationResult.Success;
            }
            catch (Exception ex)
            {
				System.Diagnostics.Debug.WriteLine(ex.Message);
                return ValidationResult.Success;
            }
        }

        public static ValidationResult IsQIOPhoneNumberRequired(ServiceLineGrouping serviceLineGrouping, ValidationContext validationContext)
        {
            // If the QIO Name is entered - QIO Phone is required
            if (!string.IsNullOrEmpty(serviceLineGrouping.QIOName))
            {
                if (string.IsNullOrEmpty(serviceLineGrouping.QIOPhoneNumber))
                {
                    return new ValidationResult("QIO Toll Free number is required for the QIO Name of " + serviceLineGrouping.QIOName, new string[] { "QIOPhoneNumber" });
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsQIONameRequired(ServiceLineGrouping serviceLineGrouping, ValidationContext validationContext)
        {
            // If the QIO Phone is entered - QIO Name is required
            if (!string.IsNullOrEmpty(serviceLineGrouping.QIOPhoneNumber))
            {
                if (string.IsNullOrEmpty(serviceLineGrouping.QIOName))
                {
                    return new ValidationResult("QIO Name is required for the QIO Toll Free Number of " + serviceLineGrouping.QIOPhoneNumber, new string[] { "QIOName" });
                }
            }
            return ValidationResult.Success;
        }

        #region "Service Line Grouping Address Validations"
        //            // If any part of the address is entered - AgencyName, AgencyPhonenumber Address1, City, StateCode and ZipCode are required


        public static ValidationResult IsServiceLineGroupingValid(ServiceLineGrouping serviceLineGrouping, ValidationContext validationContext)
        {
            // If any part of the address is entered - Address1, City, StateCode and ZipCode are required
            if (SLGroupAddressNotEmpty(serviceLineGrouping))
            {
                if ((string.IsNullOrEmpty(serviceLineGrouping.AgencyName)) ||
                    (string.IsNullOrEmpty(serviceLineGrouping.AgencyPhoneNumber)) ||
                    (string.IsNullOrEmpty(serviceLineGrouping.AgencyAddress1)) ||
                    (string.IsNullOrEmpty(serviceLineGrouping.AgencyCity)) ||
                    (serviceLineGrouping.AgencyStateCode == null) ||
                    (string.IsNullOrEmpty(serviceLineGrouping.AgencyZipCode)))
                {
                      string[] memberNames = new string[] { "AgencyName", "AgencyAddress1", "AgencyCity", "AgencyStateCode", "AgencyPhoneNumber" , "AgencyZipCode" };
                     return new ValidationResult("Agency address must contain Agency Name, Addresss, City, State and ZipCode.", memberNames);
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsSLGroupingAddressAgencyNameValid(ServiceLineGrouping serviceLineGrouping, ValidationContext validationContext)
        {
            if (SLGroupAddressNotEmpty(serviceLineGrouping) && string.IsNullOrEmpty(serviceLineGrouping.AgencyName))
            {
                return new ValidationResult("Agency address must contain Agency Name", new string[] { "AgencyName" });
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsSLGroupingAddressAgencyPhoneNumberValid(ServiceLineGrouping serviceLineGrouping, ValidationContext validationContext)
        {
            if (SLGroupAddressNotEmpty(serviceLineGrouping) && string.IsNullOrEmpty(serviceLineGrouping.AgencyPhoneNumber))
            {
                return new ValidationResult("Agency address must contain a Phone Number", new string[] { "AgencyPhoneNumber" });
            }

            return ValidationResult.Success;
        }

        public static ValidationResult IsSLGroupingAddressAgencyCityValid(ServiceLineGrouping serviceLineGrouping, ValidationContext validationContext)
        {
            if (SLGroupAddressNotEmpty(serviceLineGrouping) && string.IsNullOrEmpty(serviceLineGrouping.AgencyCity))
            {
                return new ValidationResult("Agency address must contain a City", new string[] { "AgencyCity" });
            }

            return ValidationResult.Success;
        }

        public static ValidationResult IsSLGroupingAddressAgencyAddress1Valid(ServiceLineGrouping serviceLineGrouping, ValidationContext validationContext)
        {
            if (SLGroupAddressNotEmpty(serviceLineGrouping) && string.IsNullOrEmpty(serviceLineGrouping.AgencyAddress1))
            {
                return new ValidationResult("Agency address must contain an Address Line", new string[] { "AgencyAddress1" });
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsSLGroupingAddressAgencyZipCodeValid(ServiceLineGrouping serviceLineGrouping, ValidationContext validationContext)
        {
            if (SLGroupAddressNotEmpty(serviceLineGrouping) && string.IsNullOrEmpty(serviceLineGrouping.AgencyZipCode))
            {
                return new ValidationResult("Agency address must contain a ZIP code", new string[] { "AgencyZipCode" });
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsSLGroupingAddressAgencyStateValid(ServiceLineGrouping serviceLineGrouping, ValidationContext validationContext)
        {
            if (SLGroupAddressNotEmpty(serviceLineGrouping) && (serviceLineGrouping.AgencyStateCode == null))
            {
                return new ValidationResult("Agency address must contain a State", new string[] { "AgencyStateCode" });
            }
            return ValidationResult.Success;
        }
        
        
        public static bool SLGroupAddressNotEmpty(ServiceLineGrouping serviceLineGrouping)
        {
            bool addressIsNotEmpty = (
                                      !(string.IsNullOrEmpty(serviceLineGrouping.AgencyPhoneNumber)) ||
                                      !(string.IsNullOrEmpty(serviceLineGrouping.AgencyName)) ||
                                      !(string.IsNullOrEmpty(serviceLineGrouping.AgencyAddress1)) ||
                                      !(string.IsNullOrEmpty(serviceLineGrouping.AgencyAddress2)) ||
                                      !(string.IsNullOrEmpty(serviceLineGrouping.AgencyCity)) ||
                                      !(serviceLineGrouping.AgencyStateCode == null) ||
                                      !(string.IsNullOrEmpty(serviceLineGrouping.AgencyZipCode))
                                      );

            // If any part of the address is entered - Address1, City, StateCode, ZipCode and phone are required
            return addressIsNotEmpty;
        }

        #endregion
    }
}
