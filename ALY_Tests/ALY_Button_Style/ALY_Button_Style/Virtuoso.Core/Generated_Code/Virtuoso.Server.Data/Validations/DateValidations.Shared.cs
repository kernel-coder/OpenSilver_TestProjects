using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public static class DateValidations
    {
        public static ValidationResult DateTimeValid(DateTime? date, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { validationContext.MemberName };
            string displayName = (validationContext.DisplayName == null) ? validationContext.MemberName : validationContext.DisplayName;
            if (date.HasValue)
            {
                // Likely a non-null type /w default value - existing business logic will set to a current date
                // E.G. object created and initialized with MinValue, then set on server to Now
                if (date.Value == DateTime.MinValue)
                    return ValidationResult.Success;

                var minSQLDateTime = new DateTime(1753, 1, 1);
                //SqlDateTime.MinValue is 1/1/1753 and the .NET DateTime.MinValue is 1/1/0001
                //var isValidSqlDate = ((date.Value >= (DateTime)SqlTypes.SqlDateTime.MinValue) && (date.Value <= (DateTime)SqlTypes.SqlDateTime.MaxValue));
                var isValidSqlDate = date.Value >= minSQLDateTime;
                if (isValidSqlDate == false)
                    return new ValidationResult(string.Format("{0} is not valid.", displayName), memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult DateTimeOffsetValid(DateTimeOffset? date, ValidationContext validationContext)
        {
            //FYI: SQL 0001-01-01 through 9999-12-31
            //         January 1,1 A.D. through December 31, 9999 A.D.
            //     .NET 1/1/0001 12:00:00 AM +00:00

            string[] memberNames = new string[] { validationContext.MemberName };
            string displayName = (validationContext.DisplayName == null) ? validationContext.MemberName : validationContext.DisplayName;
            if (date.HasValue)
            {
                // Likely a non-null type /w default value - existing business logic will set to a current date
                // E.G. object created and initialized with MinValue, then set on server to Now
                if (date.Value == DateTimeOffset.MinValue || date.Value == DateTime.MinValue)
                    return ValidationResult.Success;

                var minSQLDateTime = new DateTime(1753, 1, 1);  //SQL 0001-01-01 through 9999-12-31
                //SqlDateTime.MinValue is 1/1/1753 and the .NET DateTime.MinValue is 1/1/0001
                //var isValidSqlDate = ((date.Value >= (DateTime)SqlTypes.SqlDateTime.MinValue) && (date.Value <= (DateTime)SqlTypes.SqlDateTime.MaxValue));
                var isValidSqlDate = date.Value.Date >= minSQLDateTime;
                if (isValidSqlDate == false)
                    return new ValidationResult(string.Format("{0} is not valid.", displayName), memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult DateRequired(DateTime date, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { validationContext.MemberName };
            string displayName = (validationContext.DisplayName == null) ? validationContext.MemberName : validationContext.DisplayName;
            if (date == null)
            {
                return new ValidationResult(string.Format("{0} is required.",displayName), memberNames);
            }
            else if (date == DateTime.MinValue)
            {
                return new ValidationResult(string.Format("{0} is required.",displayName), memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult ValidateHighRiskMedicationEffectiveThruDate(HighRiskMedication highRiskMedication, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "EffectiveFromDate" };
            if ((highRiskMedication.EffectiveThruDate.HasValue) && (!highRiskMedication.EffectiveThruDate.Equals(DateTime.MinValue)))
            {
                if (DateTime.Compare(highRiskMedication.EffectiveFromDate.Value, highRiskMedication.EffectiveThruDate.Value) > 0)
                    return new ValidationResult("The Effective Through Date must be on or after the From date.", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult ValidateIVDiscontinueDate(AdmissionIVSite admissionIVSite, ValidationContext validationContext)
        {
            if ((admissionIVSite.IVInsertionChangeDate.HasValue == false) || (admissionIVSite.IVInsertionChangeDate.Equals(DateTime.MinValue))) return ValidationResult.Success;

            if ((admissionIVSite.IVDiscontinueDate.HasValue) && (!admissionIVSite.IVDiscontinueDate.Equals(DateTime.MinValue)))
            {
                if (DateTime.Compare(admissionIVSite.IVInsertionChangeDate.Value.Date, admissionIVSite.IVDiscontinueDate.Value.Date) > 0)
                    return new ValidationResult("The IV Discontinue Date must be on or after the IV Insertion/Change Date.", new string[] { "IVDiscontinueDate" });
            }
            return ValidationResult.Success;
        }
        public static ValidationResult DateNotInTheFuture(DateTime date, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { validationContext.MemberName };
            string displayName = (validationContext.DisplayName == null) ? validationContext.MemberName : validationContext.DisplayName;
            if (date != null && date.Date > DateTime.Today)
            {
                return new ValidationResult(string.Format("{0} cannot be in the future.", displayName), memberNames);
            }
            return ValidationResult.Success;
        }
    }

    // NOTE: Making DateNotInThePast it's own class inheriting ValidationAttribute instead of as a function used by CustomValidation, so that it sorts below Required 
    //       and DateRequired in client generated entities.  We want Required to raise and display in UI before validations like 'Field XXX cannot be in the past'.
    //       It's an ugly hack IMO...but lets me still use DataAnnotation Validators for consistency.

    // BEFORE

    //[global::System.ComponentModel.DataAnnotations.CustomValidationAttribute(typeof(global::Virtuoso.Validation.DateValidations), "DateNotInThePast", ErrorMessage = "The Effective From Date cannot be in the past")]
    //[global::System.ComponentModel.DataAnnotations.CustomValidationAttribute(typeof(global::Virtuoso.Validation.DateValidations), "DateRequired", ErrorMessage = "The Effective From Date field is required")]
    //[global::System.ComponentModel.DataAnnotations.CustomValidationAttribute(typeof(global::Virtuoso.Validation.DateValidations), "DateTimeValid")]
    //[global::System.ComponentModel.DataAnnotations.DisplayAttribute(Name = "Effective From Date")]
    //[global::System.ComponentModel.DataAnnotations.RequiredAttribute()]
    //[global::System.Runtime.Serialization.DataMemberAttribute()]
    //[global::Virtuoso.Validations.CompareValidatorAttribute(global::Virtuoso.Validations.CompareOperator.LessThan, "EffectiveThruDate", ErrorMessage = "Effective From cannot be less than Effective Thru")]
    //public global::System.DateTime EffectiveFromDate
    //{
    //}

    // AFTER

    //[global::System.ComponentModel.DataAnnotations.CustomValidationAttribute(typeof(global::Virtuoso.Validation.DateValidations), "DateRequired", ErrorMessage = "The Effective From Date field is required")]
    //[global::System.ComponentModel.DataAnnotations.CustomValidationAttribute(typeof(global::Virtuoso.Validation.DateValidations), "DateTimeValid")]
    //[global::System.ComponentModel.DataAnnotations.DisplayAttribute(Name = "Effective From Date")]
    //[global::System.ComponentModel.DataAnnotations.RequiredAttribute()]
    //[global::System.Runtime.Serialization.DataMemberAttribute()]
    //[global::Virtuoso.Validation.DateNotInThePastAttribute(ErrorMessage = "The Effective From Date cannot be in the past")]
    //[global::Virtuoso.Validations.CompareValidatorAttribute(global::Virtuoso.Validations.CompareOperator.LessThan, "EffectiveThruDate", ErrorMessage = "Effective From cannot be less than Effective Thru")]
    //public global::System.DateTime EffectiveFromDate
    //{
    //}

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
    public class DateNotInThePastAttribute : ValidationAttribute
    {
        public DateNotInThePastAttribute() : base("{0} cannot be in the past")
        { }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            DateTime date = Convert.ToDateTime(value);
            if (date.Date < DateTime.Today.Date)
            {
                string[] memberNames = new string[] { validationContext.MemberName };
                return new ValidationResult(this.FormatErrorMessage(validationContext.DisplayName), memberNames);
            }
            return ValidationResult.Success;
        }
    }
}
