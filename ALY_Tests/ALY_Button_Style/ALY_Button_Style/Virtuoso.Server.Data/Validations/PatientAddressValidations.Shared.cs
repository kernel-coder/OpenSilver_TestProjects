using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Server.Data;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Virtuoso.Validation
{
    public class PatientAddressValidations
    {
        public static ValidationResult ValidateReturnStatus(string ReturnStatus, ValidationContext validationContext)
        {
            return ValidationResult.Success;
            //if (ReturnStatus == null || ReturnStatus == string.Empty || ReturnStatus.ToLower() == "melissa" || ReturnStatus.ToLower() == "geocode" || ReturnStatus.ToLower() == "override") return ValidationResult.Success;

            //return new ValidationResult(ReturnStatus, new string[] { "ReturnStatus" });
        }
        //public static ValidationResult ValidatePhoneNumber(string ReturnStatus, ValidationContext validationContext)
        //{
        //    if (ReturnStatus == null )return ValidationResult.Success;

        //    if (ReturnStatus == string.Empty ) return new ValidationResult("Phone Number Required", new string[] { "PhoneNumber" });

        //    return ValidationResult.Success;
        //}

        public static ValidationResult ValidateInactive(bool inactive, ValidationContext validationContext)
        {
            var pa = validationContext.ObjectInstance as PhysicianAddress;

            if (pa.FacilityBranchKey == null && pa.FacilityBranchRelated && !inactive)
            {
                return new ValidationResult("error - cannot add an active branch related address with no associated branch", new string[] { "Inactive" });
            }
            else
            {
                return ValidationResult.Success;            
            }           
        }

        public static ValidationResult ValidateFacilityBranchKey(int? facilitybranchkey, ValidationContext validationContext)
        {
            var pa = validationContext.ObjectInstance as PhysicianAddress;

            if (facilitybranchkey == null && pa.FacilityBranchRelated && !pa.Inactive)
            {
                return new ValidationResult("error - cannot add an active branch related address with no associated branch", new string[] { "FacilityBranchKey" });
            }
            else
            {
                return ValidationResult.Success;
            }
        }

        //public static ValidationResult ValidateFacilityBranchRelated(bool branchrelated, ValidationContext validationContext)
        //{
          
        //    var pa = validationContext.ObjectInstance as PhysicianAddress;
           
        //    if (pa.FacilityBranchKey == null && branchrelated && !pa.Inactive)
        //    {
        //        return new ValidationResult("error - cannot add an active branch related address with no associated branch", new string[] { "FacilityBranchRelated" });
        //    }
        //    else
        //    {
        //        return ValidationResult.Success;
        //    }
        //}
      
        //public static ValidationResult ValidateStatus(string Status,ValidationContext validationContext)
        //{
        //    if (Status != "Verified") return new ValidationResult("Unverified", new string[] { "VerificationStatus" });
        //    else return ValidationResult.Success;
        //}

        // public static ValidationResult ValidateOverride(string Override,ValidationContext validationContext)
        //{
        //    PatientAddress address = validationContext.ObjectInstance as PatientAddress;
        //    return ValidationResult.Success;
        //}


    }
}
