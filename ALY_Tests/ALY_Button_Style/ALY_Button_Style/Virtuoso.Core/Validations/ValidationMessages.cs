#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace Virtuoso.Core.Validations
{
    public static class ValidationMessages
    {
        public static ValidationMessage MSG001 =
            new ValidationMessage
            {
                ID = "EXAMPLE001", Template = "{0} or {1} is invalid",
                MemberNames = new[] { "Property01", "Property02" },
                UserAction = "do something or call somebody", Explanaton = "You populated the wrong value on {0} or {1}"
            };

        public static ValidationMessage DSCH002 =
            new ValidationMessage
            {
                ID = "DSCH002", Template = "Discharge Date cannot be in the future",
                MemberNames = new[] { "DischargeDateTime" }
            };

        public static ValidationMessage DSCH003 =
            new ValidationMessage
            {
                ID = "DSCH003", Template = "Discharge Date must be selected.",
                MemberNames = new[] { "DischargeDateTime" }
            };

        public static ValidationMessage DSCH004 =
            new ValidationMessage
            {
                ID = "DSCH004", Template = "Discharge Date can not precede Admission date of {0}",
                MemberNames = new[] { "DischargeDateTime" }
            };

        public static ValidationResult Msg002(params object[] args)
        {
            return new ValidationResult(DSCH002.ToText(args), DSCH002.MemberNames);
        }

        public static ValidationResult Msg003(params object[] args)
        {
            return new ValidationResult(DSCH003.ToText(args), DSCH003.MemberNames);
        }

        public static ValidationResult Msg004(params object[] args)
        {
            return new ValidationResult(DSCH004.ToText(args), DSCH004.MemberNames);
        }
    }
}