using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public static class WoundPhotoValidations
    {
        public static ValidationResult PhotoRequired(byte[] photo, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { validationContext.MemberName };
            string displayName = (validationContext.DisplayName == null) ? validationContext.MemberName : validationContext.DisplayName;
            var wound_photo = validationContext.ObjectInstance as WoundPhoto;

            if (wound_photo != null)
            {
                if (wound_photo.Photo != null)
                    return ValidationResult.Success;

                if (wound_photo.WoundPhotoKey <= 0)
                    return new ValidationResult(string.Format("{0} is required.", displayName), memberNames);
            }
            else
            {
                return new ValidationResult(string.Format("WoundPhotoValidations.PhotoRequired validation is not valid for type {0} and display name {1}.", validationContext.ObjectInstance.GetType().Name, displayName), memberNames);
            }
            return ValidationResult.Success;
        }
    }
}
