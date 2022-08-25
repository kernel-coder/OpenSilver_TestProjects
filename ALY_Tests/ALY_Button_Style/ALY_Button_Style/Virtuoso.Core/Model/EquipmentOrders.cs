#region Usings

using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class EquipmentOrders : PatientCollectionBase
    {
        public EquipmentOrders(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public IOrderedEnumerable<AdmissionEquipment> AdmissionEquipmentList
        {
            get
            {
                IOrderedEnumerable<AdmissionEquipment> admissionEquipmentList = null;

                if ((Admission != null)
                    && (Admission.AdmissionEquipment != null)
                   )
                {
                    admissionEquipmentList = Admission.AdmissionEquipment.Where(ae => (!ae.Inactive)
                        && (Encounter != null)
                        && (Encounter.EncounterEquipment != null)
                        && Encounter.EncounterEquipment.Any(ee => ee.AdmissionEquipmentKey == ae.AdmissionEquipmentKey)
                    ).OrderBy(ae => ae.EquipmentDescription);
                }

                return admissionEquipmentList;
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;

            return AllValid;
        }
    }

    public class EquipmentOrdersFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            EquipmentOrders qb = new EquipmentOrders(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                FormModel = m,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission
            };
            EncounterData ed = vm.CurrentEncounter.EncounterData
                .FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                if (formsection.SectionKey != null)
                {
                    ed = new EncounterData
                    {
                        SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey,
                        QuestionKey = q.QuestionKey,
                        BoolData = false
                    };
                }

                if (qb.Encounter.IsNew && copyforward)
                {
                    qb.CopyForwardLastInstance();
                }
            }

            return qb;
        }
    }
}