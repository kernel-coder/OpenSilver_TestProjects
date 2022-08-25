#region Usings

using System.ComponentModel.DataAnnotations;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class ResumptionDate : QuestionUI
    {
        public ResumptionDate(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private EncounterResumption _EncounterResumption;

        public EncounterResumption EncounterResumption
        {
            get { return _EncounterResumption; }
            set
            {
                _EncounterResumption = value;
                this.RaisePropertyChangedLambda(p => p.EncounterResumption);
            }
        }

        public override void ClearEntity()
        {
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            EncounterResumption.ValidationErrors.Clear();
            if ((Admission == null) || (AdmissionDiscipline == null))
            {
                return true;
            }

            if (AdmissionDiscipline.Admitted)
            {
                if (EncounterResumption.ResumptionDate.HasValue == false)
                {
                    EncounterResumption.ValidationErrors.Add(
                        new ValidationResult("Resumption of Care Date is required.", new[] { "ResumptionDate" }));
                    return false;
                }
            }
            else if (AdmissionDiscipline.NotTaken)
            {
                EncounterResumption.ResumptionDate = null;
            }

            if (EncounterResumption.ResumptionDate.HasValue || Encounter.FullValidation)
            {
                if (EncounterResumption.Validate())
                {
                    if (EncounterResumption.IsNew)
                    {
                        Encounter.EncounterResumption.Add(EncounterResumption);
                    }

                    if (Encounter.FullValidation)
                    {
                        Admission.AdmissionStatus = CodeLookupCache.GetKeyFromCode("AdmissionStatus", "M").Value;
                        AdmissionDiscipline.AdmissionStatus =
                            CodeLookupCache.GetKeyFromCode("AdmissionStatus", "R").Value;
                        AdmissionDiscipline.DisciplineAdmitDateTime = EncounterResumption.ResumptionDate.HasValue
                            ? EncounterResumption.ResumptionDate.Value.Date
                            : EncounterResumption.ResumptionDate;
                    }

                    return true;
                }

                Admission.AdmissionStatus = CodeLookupCache.GetKeyFromCode("AdmissionStatus", "R").Value;
                return false;
            }

            if (EncounterResumption.EntityState == EntityState.Modified)
            {
                Encounter.EncounterResumption.Remove(EncounterResumption);
                EncounterResumption = new EncounterResumption
                    { ResumptionReferralDate = null, AdmissionKey = Admission.AdmissionKey };
                Admission.AdmissionStatus = CodeLookupCache.GetKeyFromCode("AdmissionStatus", "R").Value;
            }

            return true;
        }
    }

    public class ResumptionDateFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterResumption er = vm.CurrentEncounter.EncounterResumption
                .Where(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey).FirstOrDefault();
            er.AdmissionKey = vm.CurrentAdmission.AdmissionKey;

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            ResumptionDate r = new ResumptionDate(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterResumption = er,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline,
                OasisManager = vm.CurrentOasisManager,
            };
            return r;
        }
    }
}