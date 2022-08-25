#region Usings

using System;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class HospiceTeamMeetingAdmissionCOTI : QuestionBase
    {
        public override void Cleanup()
        {
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }

        public HospiceTeamMeetingAdmissionCOTI(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void HospiceAdmissionCOTISetup()
        {
            AdmissionCOTI = null;
            if ((Admission == null) || (Admission.AdmissionCOTI == null) || (Encounter == null) ||
                (Encounter.EncounterTeamMeeting == null))
            {
                return;
            }

            EncounterTeamMeeting etm = Encounter.EncounterTeamMeeting.FirstOrDefault();
            if (etm == null)
            {
                return;
            }

            AdmissionCOTI = etm.AdmissionCOTI;
            if (AdmissionCOTI != null)
            {
                SetupAdmissionCOTIDischargeFields(AdmissionCOTI);
                return;
            }

            AdmissionCOTI = Admission.AdmissionCOTI.Where(p => (p.IsCOTI && (p.EncounterDate != null)))
                .OrderByDescending(p => p.EncounterDate).FirstOrDefault();
            if (AdmissionCOTI == null)
            {
                AdmissionCOTI = Admission.AdmissionCOTI.Where(p => (p.IsCOTI && (p.AttestationDate != null)))
                    .OrderByDescending(p => p.AttestationDate).FirstOrDefault();
            }

            if (AdmissionCOTI != null)
            {
                etm.AdmissionCOTIKey = AdmissionCOTI.AdmissionCOTIKey;
                AdmissionCOTI.EncounterTeamMeeting.Add(etm);
                return;
            }

            AdmissionCOTI = CreateAdmissionCOTI();
            SetupAdmissionCOTICertFields();
            SetupAdmissionCOTIDischargeFields(AdmissionCOTI);
            AdmissionCOTI.EncounterTeamMeeting.Add(etm);
        }

        private AdmissionCOTI CreateAdmissionCOTI()
        {
            AdmissionCOTI ac = new AdmissionCOTI
            {
                TenantID = Admission.AdmissionKey,
                AdmissionKey = Admission.AdmissionKey,
                AttachingClinicianId = Encounter.EncounterBy,
                AddedFromEncounterKey = Encounter.EncounterKey,
                IsVerbalCOTI = false,
                IsCOTI = false,
                IsF2F = false,
                IsSetupDone = true
            };
            Admission.AdmissionCOTI.Add(ac);
            Encounter.AdmissionCOTI.Add(ac);
            return ac;
        }

        private void SetupAdmissionCOTICertFields()
        {
            if ((Admission == null) || (Admission.AdmissionCertification == null) || (AdmissionCOTI == null) ||
                (Encounter == null) || (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed))
            {
                return;
            }

            // refresh while in edit
            AdmissionCertification ac = (AdmissionCOTI.AdmissionCertKey == null)
                ? null
                : Admission.AdmissionCertification.FirstOrDefault(p => p.AdmissionCertKey == AdmissionCOTI.AdmissionCertKey);
            AdmissionCOTI.ServiceStartDate = HospiceEOBDate;
            AdmissionCOTI.PeriodNumber = ac?.PeriodNumber;
            AdmissionCOTI.CertificationFromDate = ac?.PeriodStartDate;
            AdmissionCOTI.CertificationThruDate = ac?.PeriodEndDate;
            AdmissionCOTI.AdmissionCertKey = ac?.AdmissionCertKey;
            if (ac != null)
            {
                return;
            }

            // without an election of benifits date - the best we can do is default a period number - if we have one
            if (HospiceEOBDate == null)
            {
                AdmissionCOTI.PeriodNumber = Admission.StartPeriodNumber;
                return;
            }

            AdmissionCOTI.ServiceStartDate = HospiceEOBDate;
            DateTime serviceDate = ServiceDate.Date;
            ac = Admission.GetAdmissionCertForDate(serviceDate, false);
            if (ac != null)
            {
                AdmissionCOTI.PeriodNumber = ac.PeriodNumber;
                AdmissionCOTI.CertificationFromDate = ac.PeriodStartDate;
                AdmissionCOTI.CertificationThruDate = ac.PeriodEndDate;
                AdmissionCOTI.AdmissionCertKey = ac.AdmissionCertKey;
                return;
            }

            // one d.n.e. - we may have enough info to forcast it (a HospiceEOBDate and a period number - so do that (but don't add it)
            // without an Admission.StartPeriodNumber - we don't really know where to start, so MakeCertPeriodForDate will assume period one
            ac = CertManager.MakeCertPeriodForDate(Admission, serviceDate, false);
            if (ac == null)
            {
                return;
            }

            AdmissionCOTI.PeriodNumber = ac.PeriodNumber;
            AdmissionCOTI.CertificationFromDate = ac.PeriodStartDate;
            // leave the PeriodEndDate date off so we fail validation until the Admission Certification row is established (as the real dates and/or period number may change
            AdmissionCOTI.CertificationThruDate = null;
        }

        private void SetupAdmissionCOTIDischargeFields(AdmissionCOTI ac)
        {
            if ((ac == null || (Encounter == null) || (Admission == null) ||
                 Admission.AdmissionHospiceDischarge == null))
            {
                return;
            }

            if (Encounter.EncounterStatus != (int)EncounterStatusType.Edit)
            {
                return; // olny refresh defaults if still in EDIT
            }

            Virtuoso.Server.Data.AdmissionHospiceDischarge ahd = Admission.AdmissionHospiceDischarge
                .Where(p => ((p.Inactive == false) && (p.HistoryKey == null)))
                .OrderByDescending(p => p.AdmissionHospiceDischargeKey).FirstOrDefault();
            if (ahd == null)
            {
                return;
            }

            ac.DischargeDate = ahd.DischargeDate;
            ac.DischargeReason = ahd.DischargeReasonDesc;
            ac.DeathDateTime = ahd.DeathDateTime;
        }

        private DateTime? HospiceEOBDate
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                if (Admission.HospiceEOBDate != null)
                {
                    return ((DateTime)Admission.HospiceEOBDate).Date;
                }

                if (Admission.SOCDate != null)
                {
                    return ((DateTime)Admission.SOCDate).Date;
                }

                return null;
            }
        }

        private DateTime ServiceDate => DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified).Date;

        public AdmissionCOTI AdmissionCOTI { get; set; }

        public AdmissionPhysician SigningAdmissionPhysician
        {
            get
            {
                if ((Admission == null) || (Admission.AdmissionPhysician == null) || (AdmissionCOTI == null))
                {
                    return null;
                }

                AdmissionPhysician ap = Admission.AdmissionPhysician.FirstOrDefault(p => p.AdmissionPhysicianKey == AdmissionCOTI.SigningAdmissionPhysicianKey);
                return ap;
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            return true;
        }
    }

    public class HospiceTeamMeetingAdmissionCOTIFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceTeamMeetingAdmissionCOTI hac = new HospiceTeamMeetingAdmissionCOTI(__FormSectionQuestionKey)
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
            hac.HospiceAdmissionCOTISetup();
            return hac;
        }
    }
}