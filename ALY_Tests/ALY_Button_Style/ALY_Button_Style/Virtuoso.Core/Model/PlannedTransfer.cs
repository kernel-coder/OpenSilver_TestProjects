#region Usings

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class PlannedTransfer : QuestionBase, INotifyDataErrorInfo
    {
        public PlannedTransfer(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public EncounterTransfer EncounterTransfer => DynamicFormViewModel?.CurrentEncounterTransfer;

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;

            if ((Encounter != null && Encounter.FullValidation && EncounterTransfer != null))
            {
                if (EncounterTransfer.PlannedTransfer == null)
                {
                    DynamicFormViewModel.CurrentEncounterTransfer.ValidationErrors.Add(
                        new ValidationResult("Planned Transfer field is required", new[] { "PlannedTransfer" }));
                    AllValid = false;
                }
                else if (EncounterTransfer.PlannedTransfer == true)
                {
                    EncounterTransfer.TransferAwareDate = null;
                    EncounterTransfer.PatientCareAtAware = false;
                }
                else if ((EncounterTransfer.PlannedTransfer == false) && (EncounterTransfer.TransferAwareDate == null))
                {
                    DynamicFormViewModel.CurrentEncounterTransfer.ValidationErrors.Add(
                        new ValidationResult("Date HHA Became Aware of Transfer field is required",
                            new[] { "TransferAwareDate" }));
                    AllValid = false;
                }
                else if ((EncounterTransfer.PlannedTransfer == false) &&
                         (EncounterTransfer.TransferAwareDate != null) &&
                         (((DateTime)EncounterTransfer.TransferAwareDate).Date > DateTime.Today.Date))
                {
                    DynamicFormViewModel.CurrentEncounterTransfer.ValidationErrors.Add(
                        new ValidationResult("Date HHA Became Aware of Transfer cannot be a future date",
                            new[] { "TransferAwareDate" }));
                    AllValid = false;
                }
            }

            return AllValid;
        }
    }

    public class PlannedTransferFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            PlannedTransfer pt = new PlannedTransfer(__FormSectionQuestionKey)
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

            return pt;
        }
    }
}