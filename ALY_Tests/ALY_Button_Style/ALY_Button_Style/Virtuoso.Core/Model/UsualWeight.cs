#region Usings

using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class UsualWeight : Weight
    {
        public UsualWeight(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override eActualType ActualType => eActualType.UsualWeight;

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            float? value = EncounterWeight.UsualWeightValue;
            string scale = EncounterWeight.UsualWeightScale;

            var removeSet = EncounterWeight.ValidationErrors.Where(w => w.ErrorMessage.Contains("Usual Weight"))
                .ToList();
            foreach (var ve in removeSet) EncounterWeight.ValidationErrors.Remove(ve);
            if (value.HasValue && string.IsNullOrWhiteSpace(scale))
            {
                EncounterWeight.ValidationErrors.Add(new ValidationResult("Usual Weight requires pounds or kilograms",
                    new[] { "UsualWeightScale" }));
                return false;
            }

            if (Encounter.FullValidation)
            {
                if (!value.HasValue && !string.IsNullOrWhiteSpace(scale))
                {
                    EncounterWeight.UsualWeightScale = null;
                }

                if (Required)
                {
                    if (!value.HasValue || value.Value == 0)
                    {
                        EncounterWeight.ValidationErrors.Add(new ValidationResult("Usual Weight required",
                            new[] { "UsualWeightValue" }));
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public class UsualWeightFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            var qui = new UsualWeight(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline,
                OasisManager = vm.CurrentOasisManager,
            };

            qui.ReadWeights(vm.CurrentEncounter, copyforward);

            return qui;
        }
    }
}