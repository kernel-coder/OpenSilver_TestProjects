#region Usings

using System;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Core.Validations;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public static class DynamicModelValidations
    {
        public static bool ValidateDischargeDate(AdmissionDiscipline admissionDiscipline)
        {
            if (admissionDiscipline.DischargeDateTime.HasValue)
            {
                if (admissionDiscipline.DischargeDateTime.Value.Date > DateTime.Now.Date)
                {
                    admissionDiscipline.ValidationErrors.Add(ValidationMessages.Msg002());
                    return false;
                }

                if (admissionDiscipline.Admission.AdmitDateTime.HasValue)
                {
                    if (admissionDiscipline.DischargeDateTime < admissionDiscipline.Admission.AdmitDateTime)
                    {
                        admissionDiscipline.ValidationErrors.Add(
                            ValidationMessages.Msg004(admissionDiscipline.Admission.AdmitDateTime.Value
                                .ToShortDateString()));
                        return false;
                    }
                }

                if (admissionDiscipline.DisciplineAdmitDateTime.HasValue
                    && admissionDiscipline.DischargeDateTime.Value.Date <
                    admissionDiscipline.DisciplineAdmitDateTime.Value.Date)
                {
                    var memberNames = new[] { "DischargeDateTime" };
                    admissionDiscipline.ValidationErrors.Add(
                        new ValidationResult("Discharge Date cannot be before the Admit Date.", memberNames));
                    return false;
                }
            }

            if (!admissionDiscipline.DischargeDateTime.HasValue)
            {
                admissionDiscipline.ValidationErrors.Add(ValidationMessages.Msg003());
                return false;
            }


            return true;
        }
    }
}