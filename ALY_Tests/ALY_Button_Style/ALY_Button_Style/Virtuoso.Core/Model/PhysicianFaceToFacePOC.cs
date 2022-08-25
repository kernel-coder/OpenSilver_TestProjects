#region Usings

using System;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    // FYI Have no PhysicianFaceToFacePOC entity, but do have a Question in the Question table where BackingFactory = 'PhysicianFaceToFacePOC'
    public class PhysicianFaceToFacePOCFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            QuestionBase pf = new QuestionBase(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                EncounterData = q.EncounterData.FirstOrDefault(),
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
            };

            EncounterData ed = vm.CurrentEncounter.EncounterData
                .FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
                pf.EncounterData = ed;
                pf.ApplyDefaults();

                if (pf.Encounter.IsNew && copyforward)
                {
                    pf.CopyForwardLastInstance();
                }
            }
            else
            {
                pf.EncounterData = ed;
            }

            ed.PropertyChanged += pf.EncounterData_PropertyChanged;

            PhysicianFaceToFacePOCSetupHidden(vm, pf);

            pf.Setup();
            return (pf);
        }

        // Turn off embedded F2F in POC for all tenants
        // Remove F2F entirely from the POC print.  This code will still conditionally show the F2F onscreen; however always 
        // setting EncounterPlanOfCare.PrintF2FwithPOC to false will disable the F2F from showing on the printed report.
        private static void PhysicianFaceToFacePOCSetupHidden(DynamicFormViewModel vm, QuestionBase pf)
        {
            if ((vm == null) || (pf == null) || (pf.Admission == null) || (pf.Admission.OrdersTracking == null) ||
                (pf.Encounter == null) || (pf.EncounterData == null))
            {
                return;
            }

            EncounterPlanOfCare ep = pf.Encounter.EncounterPlanOfCare.FirstOrDefault();
            if (pf.Encounter.PreviousEncounterStatusIsInEdit == false)
            {
                pf.Hidden = (pf.EncounterData.IntData == 0);
                return;
            }

            if (ep != null)
            {
                ep.PrintF2FwithPOC = false;
            }

            // Fell thru to: 
            // Service line IS configured to print the Face to Face with the Plan of Care 
            // AND the patient DOES NOT HAVE a Face to Face on file
            // Revert to the old logic to determine if we include or print it -
            // taking AdmissionCertifiaction.PeriodStartDate and Admission.SOCDate and Admission.FaceToFaceEncounter into account
            AdmissionCertification ac = vm.GetAdmissionCertForEncounter();
            DateTime periodStart = (ac != null && ac.PeriodStartDate.HasValue)
                ? ((DateTime)ac.PeriodStartDate).Date
                : DateTime.MinValue.Date;
            DateTime socDate = (pf.Admission.SOCDate.HasValue)
                ? ((DateTime)pf.Admission.SOCDate).Date
                : DateTime.MinValue.Date;
            bool dateComp = false;
            if ((periodStart != DateTime.MinValue) && (socDate != DateTime.MinValue))
            {
                dateComp = (periodStart.Date != socDate.Date);
            }

            pf.Hidden = ((ac != null) && (dateComp)) || CodeLookupCache.GetKeyFromCode("FACETOFACE", "DoWithCert") != pf.Admission.FaceToFaceEncounter;
            pf.EncounterData.IntData = (pf.Hidden) ? 0 : 1;
        }
    }
}