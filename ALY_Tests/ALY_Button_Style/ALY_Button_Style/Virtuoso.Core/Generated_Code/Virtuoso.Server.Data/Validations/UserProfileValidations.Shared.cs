using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;
using System.Linq;
using System.Collections.Generic;

namespace Virtuoso.Validation
{
    public static class UserProfileValidations
    {
        public static ValidationResult UserProfileValidate(UserProfile userProfile, ValidationContext validationContext)
        {
            if (userProfile.BirthDate.HasValue && (userProfile.BirthDate.Value < DateTime.Parse("1/1/1900") || userProfile.BirthDate.Value > DateTime.Now))
            {
                string[] memberNames = new string[] { "BirthDate" };
                return new ValidationResult("Birth Date cannot be before 1/1/1900 or after today.", memberNames);
            }

            if (userProfile.HireDate.HasValue && (userProfile.HireDate.Value < DateTime.Parse("1/1/1900") || userProfile.HireDate.Value > DateTime.Now))
            {
                string[] memberNames = new string[] { "HireDate" };
                return new ValidationResult("Hire Date cannot be before 1/1/1900 or after today.", memberNames);
            }

            if (userProfile.TerminationDate.HasValue && (userProfile.TerminationDate.Value < DateTime.Parse("1/1/1900") || userProfile.TerminationDate.Value > DateTime.Now))
            {
                string[] memberNames = new string[] { "TerminationDate" };
                return new ValidationResult("Termination Date cannot be before 1/1/1900 or after today.", memberNames);
            }

            if (userProfile.IsEmployee)
            {
                bool success = true;
                List<string> memberNames = new List<string>();

                if (string.IsNullOrEmpty(userProfile.Address1))
                {
                    success = false;
                    memberNames.Add("Address1");
                }

                if ((!userProfile.StateCode.HasValue)
                    || (userProfile.StateCode <= 0)
                   )
                {
                    success = false;
                    memberNames.Add("StateCode");
                }

                if (string.IsNullOrEmpty(userProfile.ZipCode))
                {
                    success = false;
                    memberNames.Add("ZipCode");
                }

                if (string.IsNullOrEmpty(userProfile.City))
                {
                    success = false;
                    memberNames.Add("City");
                }

                if (!success)
                {
                    return new ValidationResult("When the user is an employee, the user address is required", memberNames);
                }

            }

            return ValidationResult.Success;
        }

        public static ValidationResult UserProfileNPI(UserProfile userProfile, ValidationContext validationContext)
        {
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if (userProfile.PhysicianKey.HasValue)
            {
                var physicianDataProvider = validationContext.GetService(typeof(IPhysicianDataProvider)) as IPhysicianDataProvider;
                if (physicianDataProvider != null)
                {
                    var p = physicianDataProvider.GetPhysicianFromKey(userProfile.PhysicianKey.Value);
                    if (p != null)
                    {
                        if (string.IsNullOrWhiteSpace(p.NPI))
                        {
                            string[] memberNames = new string[] { "PhysicianKey" };
                            return new ValidationResult("Physician must have an NPI.", memberNames);
                        }
                        else
                        {
                            return ValidationResult.Success;
                        }
                    }
                    else
                    {
                        string[] memberNames = new string[] { "PhysicianKey" };
                        return new ValidationResult(string.Format("Cannot find physician for key {0}.", userProfile.PhysicianKey.Value), memberNames);
                    }
                }
                else
                {
                    string[] memberNames = new string[] { "PhysicianKey" };
                    return new ValidationResult("Data provider (IPhysicianDataProvider) is NULL", memberNames);
                }
            }
            else
                return ValidationResult.Success;
#else
            return ValidationResult.Success;
#endif
        }

        public static ValidationResult UserProfilePhoneValidate(UserProfilePhone userProfilePhone, ValidationContext validationContext)
        {
            if (userProfilePhone.Main == true)
            {
                UserProfile up = userProfilePhone.UserProfile;
                IQueryable<UserProfilePhone> upp = null;
                if( up != null )
                {
                    upp = up.UserProfilePhone.AsQueryable();
                }
                if (upp != null)
                {
                    if (upp.Any(ph => ph.UserProfilePhoneKey != userProfilePhone.UserProfilePhoneKey
                        && (ph.Main == true && !ph.Inactive)))
                    {
                        string[] memberNames = new string[] { "Main" };
                        return new ValidationResult("Only one primary phone can be selected", memberNames);
                    }
                }
            }
            return ValidationResult.Success;
        }
        public static ValidationResult UserProfileProductivityValidate(UserProfileProductivity userProfileProd, ValidationContext validationContext)
        {

            if (userProfileProd.EffectiveFromDate == null)
            {
                string[] memberNames = new string[] { "EffectiveFromDate" };
                return new ValidationResult("From Date cannot be null.", memberNames);
            }
            UserProfile up = userProfileProd.UserProfile;
            IQueryable<UserProfileProductivity> upp = null;
            if (up != null)
            {
               upp = up.UserProfileProductivity.AsQueryable();
            }
            if (upp != null)
            {
                if (upp.Any(ph => ph.UserProductivityKey != userProfileProd.UserProductivityKey
                    // All non null dates
                    && (( userProfileProd.EffectiveFromDate <= ph.EffectiveThruDate && userProfileProd.EffectiveThruDate >=ph.EffectiveFromDate )
                    // row passed in has null thru date
                    || (userProfileProd.EffectiveThruDate == null && ph.EffectiveThruDate >= userProfileProd.EffectiveFromDate )
                    // row passed in has non null
                    || ( ph.EffectiveThruDate == null && userProfileProd.EffectiveThruDate >= ph.EffectiveFromDate )
                    // both have non null thru dates
                    || (ph.EffectiveThruDate == null && userProfileProd.EffectiveThruDate == null )
                    )))
                    
                {
                    string[] memberNames = new string[] { "EffectiveFromDate" };
                    return new ValidationResult("Productivity rows cannot overlap.", memberNames);
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult UserProfileAlternateIDValidate(UserProfileAlternateID userProfileAltID, ValidationContext validationContext)
        {
            UserProfile up = userProfileAltID.UserProfile;
            IQueryable<UserProfileAlternateID> upp = null;
            if (up != null)
            {
                upp = up.UserProfileAlternateID.AsQueryable();
            }            
            return ValidationResult.Success;
        }
    }
}
