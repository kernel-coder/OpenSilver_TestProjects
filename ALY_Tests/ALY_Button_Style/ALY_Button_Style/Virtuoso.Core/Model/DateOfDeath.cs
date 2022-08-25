#region Usings

using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class DateOfDeath : QuestionBase
    {
        public DateOfDeath(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;

            if (Encounter != null && Encounter.FullValidation)
            {
                var deathDateError = "Date of Death is required.";
                var deathTimeError = "Time of Death is required.";

                var err = Admission.ValidationErrors.FirstOrDefault(p => p.ErrorMessage == deathDateError);
                if (err != null)
                {
                    Admission.ValidationErrors.Remove(err);
                }

                err = Admission.ValidationErrors.FirstOrDefault(p => p.ErrorMessage == deathDateError);
                if (err != null)
                {
                    Admission.ValidationErrors.Remove(err);
                }

                if (Encounter != null && Encounter.DeathNote == true)
                {
                    if (!Admission.DeathDate.HasValue)
                    {
                        Admission.ValidationErrors.Add(new ValidationResult(deathDateError, new[] { "DeathDate" }));
                        AllValid = false;
                    }

                    if (!Admission.DeathTime.HasValue)
                    {
                        Admission.ValidationErrors.Add(new ValidationResult(deathTimeError, new[] { "DeathTime" }));
                        AllValid = false;
                    }
                }
            }

            return AllValid;
        }
    }

    public class DateOfDeathFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            DateOfDeath qb = new DateOfDeath(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
            };

            // Used for the QuestionNotificationLogic ONLY!
            if (qb.EncounterData == null)
            {
                qb.EncounterData = new EncounterData();
            }

            return qb;
        }
    }
}