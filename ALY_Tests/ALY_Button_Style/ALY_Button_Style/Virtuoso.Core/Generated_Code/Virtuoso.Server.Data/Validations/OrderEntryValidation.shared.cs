using System;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    //FYI - when adding validations to OrderEntry consider whether the validation should run if the order is Voided.
    public static class OrderEntryValidations
    {
        public static ValidationResult ValidateOrderEntryOrderText(string orderText, ValidationContext validationContext)
        {
            OrderEntry oe = validationContext.ObjectInstance as OrderEntry;
            if (oe == null) return ValidationResult.Success;
            //#if SILVERLIGHT
            if ((oe.OrderStatus == (int)OrderStatusType.Completed) || (oe.OrderStatus == (int)OrderStatusType.OrderEntryReview))
            {
                if (string.IsNullOrWhiteSpace(orderText))
                {
                    if (oe.OrderEntryVersion == 1)
                    {
                        string[] memberNames = new string[] { "OrderText" };
                        return new ValidationResult("The Order Text field is required", memberNames);
                    }
                    else
                    {
                        string[] memberNames = new string[] { "GeneratedOrderText" };
                        return new ValidationResult("Order Text is required", memberNames);
                    }
                }
            }
            //#endif
            return ValidationResult.Success;
        }
        public static ValidationResult ValidateOrderEntrySigningPhysicianKey(int? signingPhysicianKey, ValidationContext validationContext)
        {
            OrderEntry oe = validationContext.ObjectInstance as OrderEntry;
            if (oe == null) return ValidationResult.Success;
            //#if SILVERLIGHT
            if ((oe.OrderStatus == (int)OrderStatusType.Completed) || (oe.OrderStatus == (int)OrderStatusType.OrderEntryReview))
            {
                int key = (signingPhysicianKey == null) ? 0 : (int)signingPhysicianKey;
                if (key <= 0)
                {
                    string[] memberNames = new string[] { "SigningPhysicianKey" };
                    return new ValidationResult("The Signing Physician field is required", memberNames);
                }
            }
            //#endif
            return ValidationResult.Success;
        }
        public static ValidationResult IsReadToRequired(OrderEntry oe, ValidationContext validationContext)
        {
            if (oe.VoidDate != null) //voided order, skip IsReadToRequired validation
                return ValidationResult.Success;

            if (oe.ReadBack && string.IsNullOrEmpty(oe.ReadTo))
                return new ValidationResult("Read To required when Read Back checked.", new string[] { "ReadTo" });
            else
                return ValidationResult.Success;
        }

        public static ValidationResult IsVoidReasonRequired(OrderEntry oe, ValidationContext validationContext)
        {
            if (oe.VoidDate != null && string.IsNullOrEmpty(oe.VoidReason))
                return new ValidationResult("Void Reason required on voided orders.", new string[] { "VoidReason" });
            else
                return ValidationResult.Success;
        }
        public static ValidationResult IsCoSignatureRequired(OrderEntry oe, ValidationContext validationContext)
        {
            if (oe.VoidDate != null) return ValidationResult.Success; //voided order, skip validation

#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            if ((oe.CoSign) && (IsCoSigned(oe) == false))
                return new ValidationResult("Co-Signature is required when Co-Sign Order? is checked.", new string[] { "CoSign" });
            else
                return ValidationResult.Success;
#else
            return ValidationResult.Success;
#endif
        }
        private static bool IsCoSigned(OrderEntry oe)
        {
            if (oe.OrderEntryCoSignature == null) return false;
            if (oe.OrderEntryCoSignature.Any() == false) return false;
            return (oe.OrderEntryCoSignature.First().Signature != null);
        }
    }
}
