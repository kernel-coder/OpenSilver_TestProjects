#region Usings

using System;
using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class TeamMeetingPatientFacility : QuestionBase
    {
        public PatientAddress PatientFacility { get; set; }

        public TeamMeetingPatientFacility(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void PatientFacilitySetup()
        {
            PatientFacility = GetPatientFacility();
            if (PatientFacility == null)
            {
                HiddenOverride = true;
            }
        }

        private PatientAddress GetPatientFacility()
        {
            if ((Patient == null) || (Patient.PatientAddress == null) || (Encounter == null) ||
                (Encounter.EncounterTeamMeeting == null))
            {
                return null;
            }

            EncounterTeamMeeting etm = Encounter.EncounterTeamMeeting.FirstOrDefault();
            if (etm == null)
            {
                return null;
            }

            // for completed encounters - return the legacy PatientFacility for this encounter - if any
            if (Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
            {
                return Patient.PatientAddress.FirstOrDefault(p => p.PatientAddressKey == etm.FacilityPatientAddressKey);
            }

            // In process encounter - Find the current facility
            DateTime today = DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified).Date;
            PatientAddress facility = Patient.PatientAddress
                .Where(p => p.Inactive == false)
                .Where(p => p.HistoryKey == null)
                .Where(p => p.IsTypeFacility)
                .Where(p =>
                    ((p.EffectiveFromDate.HasValue == false) || (p.EffectiveFromDate.Value.Date <= today)) &&
                    ((p.EffectiveThruDate.HasValue == false) || (p.EffectiveFromDate.Value.Date >= today))
                ).FirstOrDefault();
            // assume the attending is not also the medical director
            etm.FacilityPatientAddressKey = (facility == null) ? (int?)null : facility.PatientAddressKey;
            return facility;
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            return true;
        }
    }

    public class TeamMeetingPatientFacilityFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            TeamMeetingPatientFacility tmpf = new TeamMeetingPatientFacility(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline,
                DynamicFormViewModel = vm,
                OasisManager = vm.CurrentOasisManager,
            };
            tmpf.PatientFacilitySetup();
            return tmpf;
        }
    }
}