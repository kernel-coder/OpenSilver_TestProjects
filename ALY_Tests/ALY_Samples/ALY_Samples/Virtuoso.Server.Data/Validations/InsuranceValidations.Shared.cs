using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;
using System.Linq;

namespace Virtuoso.Validation
{
    public static class InsuranceValidations
    {
        private enum eServiceLineType
        {
            HomeHealth = 2,
            Hospice = 4,
            HomeCare = 8
        }

        public static ValidationResult ValidateServiceLineTypeUseBits(int? ServiceLineTypeUseBits, ValidationContext validationContext)
        {
            if (ServiceLineTypeUseBits == null || ServiceLineTypeUseBits == 0)
            {
                string[] memberNames = new string[] { "ServiceLineTypeUseBits" };
                return new ValidationResult("At least one Insurance Use must be checked.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static bool IsHospiceServiceLine(int ServiceLineTypeUseBits)
        {
            int hospiceBit = (int)eServiceLineType.Hospice;
            return (int)eServiceLineType.Hospice == (ServiceLineTypeUseBits & hospiceBit);
        }

        public static bool IsHomeCareServiceLine(int ServiceLineTypeUseBits)
        {
            int homeCareBit = (int)eServiceLineType.HomeCare;
            return (int)eServiceLineType.HomeCare == (ServiceLineTypeUseBits & homeCareBit);
        }

        public static ValidationResult ValidateInsuranceCertDefinitions(Insurance insParm, ValidationContext validationContext)
        {
            return ValidationResult.Success;
        }
        public static ValidationResult ValidateOASISReviewRequiredRFADescriptions(string descriptions, ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(descriptions))
            {
                Insurance i = validationContext.ObjectInstance as Insurance; 
                if ((i != null) && (i.OASISReviewRequired == true))
                {
                    string[] memberNames = new string[] { "OASISReviewRequiredRFADescriptions" };
                    return new ValidationResult("RFAs are required when OASIS Review Required is checked.", memberNames);
                }
            }
            return ValidationResult.Success;
        }

    }
}
