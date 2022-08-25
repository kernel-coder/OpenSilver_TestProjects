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
    public class TeamMeetingAdmittingPhysician : QuestionBase
    {
        public AdmissionPhysician AdmittingPhysician { get; set; }

        public TeamMeetingAdmittingPhysician(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void AdmittingPhysicianSetup()
        {
            AdmittingPhysician = GetAdmittingPhysician();
            if (AdmittingPhysician == null)
            {
                HiddenOverride = true;
            }
        }

        private AdmissionPhysician GetAdmittingPhysician()
        {
            if ((Admission == null) || (Admission.AdmissionPhysician == null) || (Encounter == null) ||
                (Encounter.EncounterTeamMeeting == null))
            {
                return null;
            }

            EncounterTeamMeeting etm = Encounter.EncounterTeamMeeting.FirstOrDefault();
            if (etm == null)
            {
                return null;
            }

            // for completed encounters - return the legacy Attending AdmissionPhysician for this encounter - if any
            if (Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
            {
                return Admission.AdmissionPhysician.FirstOrDefault(p => p.AdmissionPhysicianKey == etm.AttendingAdmissionPhysicianKey);
            }

            // In process encounter - Find the current attending
            DateTime today = DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified).Date;
            AdmissionPhysician attending = Admission.AdmissionPhysician
                .Where(p => p.Inactive == false)
                .Where(p => p.PhysicianType == CodeLookupCache.GetKeyFromCode("PHTP", "PCP").GetValueOrDefault())
                .Where(p =>
                    (p.PhysicianEffectiveFromDate.Date <= today) &&
                    ((p.PhysicianEffectiveThruDate.HasValue == false) ||
                     (p.PhysicianEffectiveThruDate.Value.Date >= today))
                ).FirstOrDefault();
            // assume the attending is not also the medical director
            etm.AttendingAdmissionPhysicianKey = (attending == null) ? (int?)null : attending.AdmissionPhysicianKey;
            if (attending == null)
            {
                return null;
            }

            // see if the attending is also the/a medical director - if it is then act like there is no attending
            bool attendingIsAlsoMedicalDirector = Admission.AdmissionPhysician
                .Where(p => p.Inactive == false)
                .Where(p => p.PhysicianType == CodeLookupCache.GetKeyFromCode("PHTP", "MedDirect").GetValueOrDefault())
                .Where(p => p.PhysicianKey == attending.PhysicianKey)
                .Where(p =>
                    (p.PhysicianEffectiveFromDate.Date <= today) &&
                    ((p.PhysicianEffectiveThruDate.HasValue == false) ||
                     (p.PhysicianEffectiveThruDate.Value.Date >= today))
                ).Any();
            if (attendingIsAlsoMedicalDirector)
            {
                attending = null;
            }

            etm.AttendingAdmissionPhysicianKey = (attending == null) ? (int?)null : attending.AdmissionPhysicianKey;
            return attending;
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            return true;
        }
    }

    public class TeamMeetingAdmittingPhysicianFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            TeamMeetingAdmittingPhysician tmac = new TeamMeetingAdmittingPhysician(__FormSectionQuestionKey)
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
            tmac.AdmittingPhysicianSetup();
            return tmac;
        }
    }
}