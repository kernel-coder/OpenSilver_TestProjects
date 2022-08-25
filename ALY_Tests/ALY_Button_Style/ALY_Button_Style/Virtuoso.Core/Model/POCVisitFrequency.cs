#region Usings

using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class POCVisitFrequency : QuestionUI
    {
        public POCVisitFrequency(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public IEnumerable<AdmissionDisciplineFrequency> VisitFrequencies { get; set; }
    }

    public class POCVisitFrequencyFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            POCVisitFrequency s = new POCVisitFrequency(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
            };
            s.VisitFrequencies = s.Admission.AdmissionDisciplineFrequency.Where(ad => ad.Superceded == false);
            return s;
        }
    }
}