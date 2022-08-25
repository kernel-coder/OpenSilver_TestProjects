#region Usings

using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class Height : Weight
    {
        public Height(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override eActualType ActualType => eActualType.Height;

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            bool ret = true;
            float? value = EncounterWeight.HeightValue;
            string scale = EncounterWeight.HeightScale;
            float? valueReported = EncounterWeight.ReportedHeightValue;
            string scaleReported = EncounterWeight.ReportedHeightScale;

            var removeSet = EncounterWeight.ValidationErrors.Where(w => w.ErrorMessage.Contains("Height")).ToList();
            foreach (var ve in removeSet) EncounterWeight.ValidationErrors.Remove(ve);
            if (value.HasValue && string.IsNullOrWhiteSpace(scale))
            {
                EncounterWeight.ValidationErrors.Add(
                    new ValidationResult("Height requires inches, meters or centimeters", new[] { "HeightScale" }));
                ret = false;
            }
            else if (Encounter.FullValidation)
            {
                if (!value.HasValue && !string.IsNullOrWhiteSpace(scale))
                {
                    EncounterWeight.HeightScale = null;
                }

                if (Required)
                {
                    if (!value.HasValue || value.Value == 0)
                    {
                        EncounterWeight.ValidationErrors.Add(new ValidationResult("Height required",
                            new[] { "HeightValue" }));
                        ret = false;
                    }
                }
            }

            if (valueReported.HasValue && string.IsNullOrWhiteSpace(scaleReported))
            {
                EncounterWeight.ValidationErrors.Add(new ValidationResult(
                    "Reported Height requires inches, meters or centimeters", new[] { "ReportedHeightScale" }));
                ret = false;
            }
            else if (Encounter.FullValidation)
            {
                if (!valueReported.HasValue && !string.IsNullOrWhiteSpace(scaleReported))
                {
                    EncounterWeight.ReportedHeightScale = null;
                }
            }

            return ret;
        }
    }

    public class HeightFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            var qui = new Height(__FormSectionQuestionKey)
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