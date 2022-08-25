#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class HospiceAdmissionCOTI : QuestionBase
    {
        public override void Cleanup()
        {
            Messenger.Default.Unregister<int>(this, "AdmissionPhysician_FormUpdate");
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }

        public HospiceAdmissionCOTI(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private bool IsCOTIForm
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsCOTI == false))
                {
                    return false;
                }

                return true;
            }
        }

        private bool IsVerbalCOTIForm
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsVerbalCOTI == false))
                {
                    return false;
                }

                return true;
            }
        }

        private bool IsHospiceF2FForm
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsHospiceF2F == false))
                {
                    return false;
                }

                return true;
            }
        }

        public void HospiceAdmissionCOTISetup()
        {
            Messenger.Default.Register<int>(this, "AdmissionPhysician_FormUpdate",
                AdmissionKey => { AdmissionPhysician_FormUpdate(); });

            SetupAdmissionCOTI();
            if (AdmissionCOTI == null)
            {
                return;
            }

            if (AdmissionCOTI.IsSetupDone == false)
            {
                SetupAdmissionCOTICertFields();
            }

            SetupAdmissionCOTIPhysician(true);
            SetupAdmissionCOTIPhysicianVerbal();
            OriginalSigningPhysicianKey = AdmissionCOTI.SigningPhysicianKey.GetValueOrDefault();
            AdmissionCOTI.IsSetupDone = true;
        }

        public AdmissionCOTI AdmissionCOTI { get; set; }

        private void SetupAdmissionCOTI()
        {
            AdmissionCOTI = null;
            if ((Admission == null) || (Admission.AdmissionCOTI == null) || (Encounter == null) ||
                (Encounter.AdmissionCOTI == null))
            {
                return;
            }

            AdmissionCOTI = Encounter.AdmissionCOTI.FirstOrDefault();
            if (AdmissionCOTI != null)
            {
                return;
            }

            AdmissionCOTI = new AdmissionCOTI
            {
                TenantID = Admission.AdmissionKey,
                AdmissionKey = Admission.AdmissionKey,
                AttachingClinicianId = Encounter.EncounterBy,
                AddedFromEncounterKey = Encounter.EncounterKey,
                IsVerbalCOTI = IsVerbalCOTIForm,
                IsCOTI = IsCOTIForm,
                IsF2F = IsHospiceF2FForm,
                IsSetupDone = false
            };
            Admission.AdmissionCOTI.Add(AdmissionCOTI);
            Encounter.AdmissionCOTI.Add(AdmissionCOTI);
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

        private int DaysAllowedToPerformServiceBeforeCertStartDate
        {
            get
            {
                if (IsCOTIForm || IsVerbalCOTIForm)
                {
                    return 15;
                }

                if (IsHospiceF2FForm)
                {
                    return 30;
                }

                return 0;
            }
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
                : Admission.AdmissionCertification.Where(p => p.AdmissionCertKey == AdmissionCOTI.AdmissionCertKey)
                    .FirstOrDefault();
            AdmissionCOTI.ServiceStartDate = HospiceEOBDate;
            AdmissionCOTI.PeriodNumber = (ac == null) ? (int?)null : ac.PeriodNumber;
            AdmissionCOTI.CertificationFromDate = (ac == null) ? null : ac.PeriodStartDate;
            AdmissionCOTI.CertificationThruDate = (ac == null) ? null : ac.PeriodEndDate;
            AdmissionCOTI.AdmissionCertKey = (ac == null) ? (int?)null : ac.AdmissionCertKey;
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
            // if an Admission Certification row already exists for our 'serviceDate' use it
            DateTime serviceDate =
                ServiceDate.AddDays(DaysAllowedToPerformServiceBeforeCertStartDate)
                    .Date; // 15 day window where you can do a Verbal or COTI early
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
            // leave the PeriodEndDate date off so we fail validation until the Admission Certification row is established (as the real dates and/or periof number may change
            AdmissionCOTI.CertificationThruDate = null;
            if ((IsCOTIForm || IsVerbalCOTIForm) && ac.IsNew &&
                (Admission.AdmissionCertification.Contains(ac) == false) && Admission.AdmissionCertification.Any())
            {
                // Allow CTIs and Verbal CTI to create subsequent AdmissionCertification rows - just not the first one
                AdmissionCOTI.CertificationThruDate = ac.PeriodEndDate;
                Admission.AdmissionCertification.Add(ac);
                AdmissionCOTI.AdmissionCertification = ac;
            }
        }

        public int? SigningPhysicianKey
        {
            get { return (AdmissionCOTI == null) ? null : AdmissionCOTI.SigningPhysicianKey; }
            set
            {
                if (AdmissionCOTI != null)
                {
                    AdmissionCOTI.SigningPhysicianKey = value;
                }

                RaisePropertyChanged("SigningPhysician");
            }
        }

        public Physician SigningPhysician
        {
            get
            {
                if (AdmissionCOTI == null)
                {
                    return null;
                }

                return AdmissionCOTI.SigningPhysician;
            }
        }

        public int? SigningAdmissionPhysicianKey
        {
            get { return (AdmissionCOTI == null) ? null : AdmissionCOTI.SigningAdmissionPhysicianKey; }
            set
            {
                if (AdmissionCOTI != null)
                {
                    AdmissionCOTI.SigningAdmissionPhysicianKey = value;
                }

                if (IsVerbalCOTIForm)
                {
                    ResetAdmissionCOTIFieldsIsVerbalCOTIForm();
                }

                RaisePropertyChanged("SigningAdmissionPhysician");
            }
        }

        public AdmissionPhysician SigningAdmissionPhysician
        {
            get
            {
                if ((Admission == null) || (Admission.AdmissionPhysician == null) || (AdmissionCOTI == null))
                {
                    return null;
                }

                AdmissionPhysician ap = Admission.AdmissionPhysician
                    .Where(p => p.AdmissionPhysicianKey == AdmissionCOTI.SigningAdmissionPhysicianKey).FirstOrDefault();
                return ap;
            }
        }

        public List<AdmissionPhysician> SigningAdmissionPhysicianList { get; set; }

        private void AdmissionPhysician_FormUpdate()
        {
            SetupAdmissionCOTIPhysicianVerbal();
            SetupAdmissionCOTIPhysician(false);
        }

        private void SetupAdmissionCOTIPhysician(bool showMessages)
        {
            if (Question == null)
            {
                return;
            }

            if ((IsCOTIForm == false) && (IsHospiceF2FForm == false))
            {
                return;
            }

            if (Question == null)
            {
                return;
            }

            if (IsCOTIForm && (Question.DataTemplate != "HospiceCertificationStatement"))
            {
                return;
            }

            if (IsHospiceF2FForm && (Question.DataTemplate.StartsWith("HospiceAttestationStatement") == false))
            {
                return;
            }

            if ((Admission == null) || (Admission.AdmissionPhysician == null) || (AdmissionCOTI == null) ||
                (Encounter == null) || (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed))
            {
                return;
            }

            AdmissionCOTI.SigningAdmissionPhysicianKey = null;
            AdmissionCOTI.SigningPhysicianKey = null;
            this.RaisePropertyChangedLambda(p => p.SigningPhysician);
            this.RaisePropertyChangedLambda(p => p.SigningPhysicianKey);
            AdmissionCOTI.SigningPhysicianAddressKey = null;
            this.RaisePropertyChangedLambda(p => p.SigningAdmissionPhysician);
            this.RaisePropertyChangedLambda(p => p.SigningAdmissionPhysicianKey);
            if (Encounter.EncounterBy == null)
            {
                return;
            }

            UserProfile up = Encounter.EncounterByUserProfile;
            if (up == null)
            {
                return;
            }

            if (up.PhysicianKey == null)
            {
                if (showMessages)
                {
                    CreateDialogue("Error: User Maintenance",
                        "Your user is not defined as a physician in User Maintenance.  You cannot perform a " +
                        Encounter.ServiceTypeDescription + " for this patient.");
                }

                return;
            }

            if (IsCOTIForm && (Encounter.EncounterByIsHospiceMedicalDirector == false) &&
                (Encounter.EncounterByIsHospicePhysician == false))
            {
                if (showMessages)
                {
                    CreateDialogue("Error: User Maintenance",
                        "Your user is not defined with the Hospice Physician or Medical Director role in User Maintenance.  You cannot perform a " +
                        Encounter.ServiceTypeDescription + " for this patient.");
                }

                return;
            }

            if (IsHospiceF2FForm && (Encounter.EncounterByIsHospiceMedicalDirector == false) &&
                (Encounter.EncounterByIsHospicePhysician == false &&
                 (Encounter.EncounterByIsHospiceNursePractitioner == false)))
            {
                if (showMessages)
                {
                    CreateDialogue("Error: User Maintenance",
                        "Your user is not defined with the Hospice Physician, Medical Director  or Nurse Practitioner role in User Maintenance.  You cannot perform a " +
                        Encounter.ServiceTypeDescription + " for this patient.");
                }

                return;
            }

            // pecking order - MedDirector first
            AdmissionPhysician ap = Admission.AdmissionPhysician
                .Where(p => p.PhysicianKey == up.PhysicianKey)
                .Where(p => p.Inactive == false)
                .Where(p => p.IsMedDirect)
                .Where(p => (p.PhysicianEffectiveFromDate.Date <= ServiceDate) &&
                            (!p.PhysicianEffectiveThruDate.HasValue || (p.PhysicianEffectiveThruDate.HasValue &&
                                                                        (p.PhysicianEffectiveThruDate.Value.Date >=
                                                                         ServiceDate))))
                .FirstOrDefault();
            // pecking order - MedDirector first followed by Attending ( Unless IsHospiceF2FForm - attendings don't do those
            if ((ap == null) && (IsHospiceF2FForm == false))
            {
                ap = Admission.AdmissionPhysician
                    .Where(p => p.PhysicianKey == up.PhysicianKey)
                    .Where(p => p.Inactive == false)
                    .Where(p => p.IsPCP)
                    .Where(p => (p.PhysicianEffectiveFromDate.Date <= ServiceDate) &&
                                (!p.PhysicianEffectiveThruDate.HasValue || (p.PhysicianEffectiveThruDate.HasValue &&
                                                                            (p.PhysicianEffectiveThruDate.Value.Date >=
                                                                             ServiceDate))))
                    .FirstOrDefault();
            }

            if ((ap == null) && IsCOTIForm)
            {
                if (showMessages)
                {
                    CreateDialogue("Error:",
                        "You are not defined as the Medical Director / Hospice Physician or the Attending physician for this Admission.  You cannot perform a " +
                        Encounter.ServiceTypeDescription +
                        " for this patient.  To add yourself as a physician for this patient, use the Physicians tab at the Admission level.");
                }

                return;
            }

            // pecking order - MedDirector first followed by Attending followed by anything else for a F2F
            if (ap == null)
            {
                ap = Admission.AdmissionPhysician
                    .Where(p => p.Inactive == false)
                    .Where(p => (p.PhysicianEffectiveFromDate.Date <= ServiceDate) &&
                                (!p.PhysicianEffectiveThruDate.HasValue || (p.PhysicianEffectiveThruDate.HasValue &&
                                                                            (p.PhysicianEffectiveThruDate.Value.Date >=
                                                                             ServiceDate))))
                    .FirstOrDefault();
            }

            // for a face to face - a NursePractitioner may not be defined as a physician for this patient
            if ((ap == null) && IsHospiceF2FForm && (Encounter.EncounterByIsHospiceNursePractitioner == false))
            {
                if (showMessages)
                {
                    CreateDialogue("Error:",
                        "You are not defined as a physician for this Admission.  You cannot perform a " +
                        Encounter.ServiceTypeDescription +
                        " for this patient.  To add yourself as a physician for this patient, use the Physicians tab at the Admission level.");
                }

                return;
            }

            AdmissionCOTI.SigningAdmissionPhysicianKey = (ap == null) ? (int?)null : ap.AdmissionPhysicianKey;
            AdmissionCOTI.SigningPhysicianKey = up.PhysicianKey;
            this.RaisePropertyChangedLambda(p => p.SigningPhysician);
            this.RaisePropertyChangedLambda(p => p.SigningPhysicianKey);
            AdmissionCOTI.SigningPhysicianAddressKey = (ap == null) ? null : ap.PhysicianAddressKey;
            this.RaisePropertyChangedLambda(p => p.SigningAdmissionPhysician);
            this.RaisePropertyChangedLambda(p => p.SigningAdmissionPhysicianKey);
            if (IsCOTIForm)
            {
                SetupAdmissionCOTIF2FFieldsIsCOTIForm();
            }

            if (IsHospiceF2FForm)
            {
                SetupAdmissionCOTIF2FFieldsIsHospiceF2FForm();
            }
        }

        private void SetupAdmissionCOTIPhysicianVerbal()
        {
            if (IsVerbalCOTIForm == false)
            {
                return;
            }

            if ((Question == null) || (Question.DataTemplate != "HospiceVerbalCertification"))
            {
                return;
            }

            if ((Admission == null) || (Admission.AdmissionPhysician == null) || (AdmissionCOTI == null) ||
                (Encounter == null))
            {
                return;
            }

            AdmissionPhysician sap = null;
            List<AdmissionPhysician> apList = new List<AdmissionPhysician>();
            if (Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed)
            {
                DateTime serviceDate = ServiceDate;
                int? saveSigningAdmissionPhysicianKey = AdmissionCOTI.SigningAdmissionPhysicianKey;
                // Put all active MedDirectors in the list
                apList = Admission.AdmissionPhysician
                    .Where(p => p.Inactive == false)
                    .Where(p => p.IsMedDirect)
                    .Where(p => (p.PhysicianEffectiveFromDate.Date <= serviceDate) &&
                                (!p.PhysicianEffectiveThruDate.HasValue || (p.PhysicianEffectiveThruDate.HasValue &&
                                                                            (p.PhysicianEffectiveThruDate.Value.Date >=
                                                                             serviceDate))))
                    .ToList();
                // Put all active Attendings in the list - if they are not already there as a MedDirector
                List<AdmissionPhysician> apListIsPCP = Admission.AdmissionPhysician
                    .Where(p => p.Inactive == false)
                    .Where(p => p.IsPCP)
                    .Where(p => (p.PhysicianEffectiveFromDate.Date <= serviceDate) &&
                                (!p.PhysicianEffectiveThruDate.HasValue || (p.PhysicianEffectiveThruDate.HasValue &&
                                                                            (p.PhysicianEffectiveThruDate.Value.Date >=
                                                                             serviceDate))))
                    .ToList();
                foreach (AdmissionPhysician ap in apListIsPCP)
                    if (apList.Where(p => p.PhysicianKey == ap.PhysicianKey).Any() == false)
                    {
                        apList.Add(ap);
                    }

                sap = ((apList == null) || (apList.Count != 1)) ? null : apList.FirstOrDefault();
                if (saveSigningAdmissionPhysicianKey == null)
                {
                    AdmissionCOTI.SigningAdmissionPhysicianKey = (sap == null) ? (int?)null : sap.AdmissionPhysicianKey;
                }
                else
                {
                    AdmissionCOTI.SigningAdmissionPhysicianKey =
                        apList.Where(p => p.AdmissionPhysicianKey == saveSigningAdmissionPhysicianKey).Any()
                            ? saveSigningAdmissionPhysicianKey
                            : null;
                }
            }

            sap = SigningAdmissionPhysician;
            if ((sap != null) && (apList.Contains(sap) == false))
            {
                apList.Add(sap);
            }

            SigningAdmissionPhysicianList = apList;
            AdmissionCOTI.SigningPhysicianKey = (sap == null) ? (int?)null : sap.PhysicianKey;
            this.RaisePropertyChangedLambda(p => p.SigningPhysician);
            this.RaisePropertyChangedLambda(p => p.SigningPhysicianKey);
            AdmissionCOTI.SigningPhysicianAddressKey = (sap == null) ? null : sap.PhysicianAddressKey;
            this.RaisePropertyChangedLambda(p => p.SigningAdmissionPhysicianList);
            this.RaisePropertyChangedLambda(p => p.SigningAdmissionPhysician);
            this.RaisePropertyChangedLambda(p => p.SigningAdmissionPhysicianKey);
            SetupAdmissionCOTIF2FFieldsIsCOTIForm();
        }

        private DateTime ServiceDate
        {
            get
            {
                if ((Encounter != null) && (Encounter.EncounterOrTaskStartDateAndTime != null))
                {
                    return ((DateTimeOffset)Encounter.EncounterOrTaskStartDateAndTime).Date;
                }

                return DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified).Date;
            }
        }

        private bool PhysicianIsMedDirectForAdmission(int physicianKey)
        {
            if ((Admission == null) || (Admission.AdmissionPhysician == null))
            {
                return false;
            }

            bool any = Admission.AdmissionPhysician
                .Where(p => p.Inactive == false)
                .Where(p => p.IsMedDirect)
                .Where(p => p.PhysicianKey == physicianKey)
                .Where(p => (p.PhysicianEffectiveFromDate.Date <= ServiceDate) &&
                            (!p.PhysicianEffectiveThruDate.HasValue || (p.PhysicianEffectiveThruDate.HasValue &&
                                                                        (p.PhysicianEffectiveThruDate.Value.Date >=
                                                                         ServiceDate))))
                .Any();
            return any;
        }

        private bool PhysicianIsAttendingForAdmission(int physicianKey)
        {
            if ((Admission == null) || (Admission.AdmissionPhysician == null))
            {
                return false;
            }

            bool any = Admission.AdmissionPhysician
                .Where(p => p.Inactive == false)
                .Where(p => p.IsPCP)
                .Where(p => p.PhysicianKey == physicianKey)
                .Where(p => (p.PhysicianEffectiveFromDate.Date <= ServiceDate) &&
                            (!p.PhysicianEffectiveThruDate.HasValue || (p.PhysicianEffectiveThruDate.HasValue &&
                                                                        (p.PhysicianEffectiveThruDate.Value.Date >=
                                                                         ServiceDate))))
                .Any();
            return any;
        }

        private void ResetAdmissionCOTIFieldsIsVerbalCOTIForm()
        {
            if ((Admission == null) || (AdmissionCOTI == null) || (Encounter == null))
            {
                return;
            }

            if (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed)
            {
                return;
            }

            AdmissionCOTI.IsMedDirect = false;
            AdmissionCOTI.IsAttending = false;
            AdmissionPhysician sap = SigningAdmissionPhysician;
            if ((sap != null) && (PhysicianIsMedDirectForAdmission(sap.PhysicianKey)))
            {
                AdmissionCOTI.IsMedDirect = true;
            }

            if ((sap != null) && (PhysicianIsAttendingForAdmission(sap.PhysicianKey)))
            {
                AdmissionCOTI.IsAttending = true;
            }
        }

        private void SetupAdmissionCOTIF2FFieldsIsCOTIForm()
        {
            if ((Admission == null) || (Admission.AdmissionCOTI == null) || (AdmissionCOTI == null) ||
                (Encounter == null))
            {
                return;
            }

            if (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed)
            {
                return;
            }

            AdmissionCOTI ac = null;
            AdmissionPhysician sap = null;
            AdmissionCOTI.IsMedDirect = false;
            AdmissionCOTI.IsAttending = false;
            AdmissionCOTI.IsHospicePhysician = false;
            AdmissionCOTI.IsHospiceNursePractitioner = false;
            //AdmissionCOTI.EncounterDate = null; physician way have already entered a data

            sap = SigningAdmissionPhysician;
            if ((sap != null) && (PhysicianIsMedDirectForAdmission(sap.PhysicianKey)))
            {
                AdmissionCOTI.IsMedDirect = true;
            }

            if ((sap != null) && (PhysicianIsAttendingForAdmission(sap.PhysicianKey)))
            {
                AdmissionCOTI.IsAttending = true;
            }

            // Populate EncounterDate if possible
            // Although F2f is only applicable to COTI and not VerbalCOTI - figure it out anyway - as the Verbal will create a COTI to send to the doc
            // We need the F2F within (30 days before - 2 days after) of the Cert Period startdate for the given COTI
            DateTime F2FEndDate = (AdmissionCOTI.CertificationFromDate != null)
                ?
                ((DateTime)AdmissionCOTI.CertificationFromDate).Date
                : (Encounter.EncounterOrTaskStartDateAndTime == null)
                    ? DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified)
                    : ((DateTimeOffset)Encounter.EncounterOrTaskStartDateAndTime).Date;
            F2FEndDate = F2FEndDate.AddDays(2);
            DateTime F2FStartDate = F2FEndDate.AddDays(-32).Date;
            // if we already have a physician attestation for the signing physician - use it
            if (AdmissionCOTI.SigningAdmissionPhysicianKey != null)
            {
                ac = Admission.AdmissionCOTI.Where(p => ((p.AdmissionCOTIKey != AdmissionCOTI.AdmissionCOTIKey) &&
                                                         p.IsF2F &&
                                                         (p.SigningPhysicianKey == AdmissionCOTI.SigningPhysicianKey) &&
                                                         p.EncounterDateInRange(F2FStartDate, F2FEndDate)))
                    .FirstOrDefault();
                if (ac != null)
                {
                    AdmissionCOTI.EncounterDate = ac.EncounterDate;
                    return;
                }
            }

            // If we already have one on file for a hospice physician - use it
            ac = Admission.AdmissionCOTI.Where(p => ((p.AdmissionCOTIKey != AdmissionCOTI.AdmissionCOTIKey) &&
                                                     p.IsF2F &&
                                                     p.IsHospicePhysician &&
                                                     p.EncounterDateInRange(F2FStartDate, F2FEndDate)))
                .FirstOrDefault();
            if (ac != null)
            {
                AdmissionCOTI.IsHospiceNursePractitioner =
                    true; // For CTIs - we are overloading this bit for logic in the F2F section of a CTI - see DataTemplate HospiceCertificationStatement
                AdmissionCOTI.IsHospicePhysician = true;
                AdmissionCOTI.EncounterDate = ac.EncounterDate;
                return;
            }

            // If we already have one on file for a nurse practioner - use it
            ac = Admission.AdmissionCOTI.Where(p => ((p.AdmissionCOTIKey != AdmissionCOTI.AdmissionCOTIKey) &&
                                                     p.IsF2F &&
                                                     p.IsHospiceNursePractitioner &&
                                                     p.EncounterDateInRange(F2FStartDate, F2FEndDate)))
                .FirstOrDefault();
            if (ac != null)
            {
                AdmissionCOTI.IsHospiceNursePractitioner = true;
                AdmissionCOTI.EncounterDate = ac.EncounterDate;
            }
            // else we don't have one
        }

        private void SetupAdmissionCOTIF2FFieldsIsHospiceF2FForm()
        {
            if ((Admission == null) || (Admission.AdmissionCOTI == null) || (AdmissionCOTI == null) ||
                (Encounter == null))
            {
                return;
            }

            if (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed)
            {
                return;
            }

            AdmissionCOTI.IsMedDirect = false;
            AdmissionCOTI.IsAttending = false;
            AdmissionCOTI.IsHospicePhysician = false;
            AdmissionCOTI.IsHospiceNursePractitioner = false;
            if (Encounter.EncounterByIsHospiceMedicalDirector)
            {
                AdmissionCOTI.IsMedDirect = true;
            }
            else if (Encounter.EncounterByIsHospicePhysician)
            {
                AdmissionCOTI.IsHospicePhysician = true;
            }
            else if (Encounter.EncounterByIsHospiceNursePractitioner)
            {
                AdmissionCOTI.IsHospiceNursePractitioner = true;
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            bool allValid = true;
            if ((Encounter == null) || (Patient == null) || (AdmissionCOTI == null) || (Question == null))
            {
                return true;
            }

            if (AdmissionCOTI.ServiceStartDate == DateTime.MinValue)
            {
                AdmissionCOTI.ServiceStartDate = null;
            }

            if (AdmissionCOTI.PeriodNumber <= 0)
            {
                AdmissionCOTI.PeriodNumber = null;
            }

            if (AdmissionCOTI.CertificationFromDate == DateTime.MinValue)
            {
                AdmissionCOTI.CertificationFromDate = null;
            }

            if (AdmissionCOTI.CertificationThruDate == DateTime.MinValue)
            {
                AdmissionCOTI.CertificationThruDate = null;
            }

            if (AdmissionCOTI.EncounterDate == DateTime.MinValue)
            {
                AdmissionCOTI.EncounterDate = null;
            }

            if (string.IsNullOrWhiteSpace(AdmissionCOTI.PhysicianNarritive))
            {
                AdmissionCOTI.PhysicianNarritive = null;
            }

            if (AdmissionCOTI.AttestationDate == DateTime.MinValue)
            {
                AdmissionCOTI.AttestationDate = null;
            }

            if (AdmissionCOTI.SigningAdmissionPhysicianKey <= 0)
            {
                AdmissionCOTI.SigningAdmissionPhysicianKey = null;
            }

            if (AdmissionCOTI.SigningPhysicianKey <= 0)
            {
                AdmissionCOTI.SigningPhysicianKey = null;
            }

            this.RaisePropertyChangedLambda(p => p.SigningPhysician);
            this.RaisePropertyChangedLambda(p => p.SigningPhysicianKey);
            if (AdmissionCOTI.VerbalCertificationDate == DateTime.MinValue)
            {
                AdmissionCOTI.VerbalCertificationDate = null;
            }

            if (AdmissionCOTI.VerbalCertificationDate != null)
            {
                AdmissionCOTI.VerbalCertificationDate = ((DateTime)AdmissionCOTI.VerbalCertificationDate).Date;
            }

            if ((IsVerbalCOTIForm) || (IsHospiceF2FForm))
            {
                AdmissionCOTI.PhysicianNarritive = null;
            }

            if ((IsVerbalCOTIForm) || (IsHospiceF2FForm))
            {
                AdmissionCOTI.AttestationSignature = null;
            }

            if ((IsVerbalCOTIForm) || (IsHospiceF2FForm))
            {
                AdmissionCOTI.AttestationDate = null;
            }

            if ((IsCOTIForm) || (IsHospiceF2FForm))
            {
                AdmissionCOTI.VerbalCertificationDate = null;
            }

            if ((AdmissionCOTI.ShowF2F == false) && (IsHospiceF2FForm == false))
            {
                AdmissionCOTI.EncounterDate = null;
            }

            AdmissionPhysician sap = SigningAdmissionPhysician;
            AdmissionCOTI.SigningPhysicianKey = (sap == null) ? (int?)null : sap.PhysicianKey;
            this.RaisePropertyChangedLambda(p => p.SigningPhysician);
            this.RaisePropertyChangedLambda(p => p.SigningPhysicianKey);
            AdmissionCOTI.SigningPhysicianAddressKey = (sap == null) ? null : sap.PhysicianAddressKey;

            // never let a future Receipt of Verbal Certification Date - even if not full validation
            DateTime today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
            if ((IsVerbalCOTIForm) && (AdmissionCOTI.VerbalCertificationDate != null) &&
                (AdmissionCOTI.VerbalCertificationDate > today) &&
                (Question.DataTemplate == "HospiceVerbalCertification"))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult(
                    "The Receipt of Verbal Certification Date cannot be in the future.",
                    new[] { "VerbalCertificationDate" }));
                allValid = false;
                if (DynamicFormViewModel != null)
                {
                    DynamicFormViewModel.ValidEnoughToSave = false;
                }
            }

            if (Encounter.FullValidation == false)
            {
                return true;
            }


            if ((AdmissionCOTI.ServiceStartDate == null) && (Question.DataTemplate == "HospicePatientInformation"))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult("The Service Start Date is required.",
                    new[] { "ServiceStartDate" }));
                allValid = false;
            }

            if ((AdmissionCOTI.PeriodNumber == null) && (Question.DataTemplate == "HospicePatientInformation"))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult("The Benefit Period Number is required.",
                    new[] { "PeriodNumber" }));
                allValid = false;
            }

            if ((AdmissionCOTI.CertificationFromDate == null) && (Question.DataTemplate == "HospicePatientInformation"))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult("The Benefit Period From Date is required.",
                    new[] { "CertificationFromDate" }));
                allValid = false;
            }

            if ((AdmissionCOTI.CertificationThruDate == null) && (Question.DataTemplate == "HospicePatientInformation"))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult("The Benefit Period Thru Date is required.",
                    new[] { "CertificationThruDate" }));
                allValid = false;
            }

            if ((((IsCOTIForm) && AdmissionCOTI.ShowF2F) || (IsHospiceF2FForm)) &&
                (AdmissionCOTI.EncounterDate == null) && (Question.DataTemplate == "HospiceCertificationStatement"))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult("The Encounter Date is required.",
                    new[] { "EncounterDate" }));
                allValid = false;
            }

            if ((IsCOTIForm) && (AdmissionCOTI.PhysicianNarritive == null) &&
                (Question.DataTemplate == "HospiceCertificationStatement"))
            {
                AdmissionCOTI.ValidationErrors.Add(
                    new ValidationResult("The Physician Narritive Statement is required.",
                        new[] { "PhysicianNarritive" }));
                allValid = false;
            }

            if ((IsCOTIForm) && (AdmissionCOTI.AttestationSignature == null) &&
                (Question.DataTemplate == "HospiceCertificationStatement"))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult(
                    "The Attestation Signature and Date are required.", new[] { "AttestationSignature" }));
                allValid = false;
            }

            if ((IsCOTIForm) && (AdmissionCOTI.AttestationDate == null) &&
                (Question.DataTemplate == "HospiceCertificationStatement"))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult("The Attestation Date is required.",
                    new[] { "AttestationDate" }));
                allValid = false;
            }

            if ((IsHospiceF2FForm) && (AdmissionCOTI.EncounterDate == null) &&
                Question.DataTemplate.StartsWith("HospiceAttestationStatement"))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult("The Encounter Date is required.",
                    new[] { "EncounterDate" }));
                allValid = false;
            }

            if (((IsCOTIForm) || (IsVerbalCOTIForm)) && (AdmissionCOTI.SigningAdmissionPhysicianKey == null) &&
                ((Question.DataTemplate == "HospiceCertificationStatement") ||
                 (Question.DataTemplate == "HospiceVerbalCertification")))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult("The Physician is required.",
                    new[] { "SigningAdmissionPhysicianKey" }));
                allValid = false;
            }

            if ((IsHospiceF2FForm) && (AdmissionCOTI.SigningPhysicianKey == null) &&
                (Question.DataTemplate == "HospiceAttestationStatement"))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult("The Physician is required.",
                    new[] { "SigningPhysicianKey" }));
                allValid = false;
            }

            if ((IsVerbalCOTIForm) && (AdmissionCOTI.VerbalCertificationDate == null) &&
                (Question.DataTemplate == "HospiceVerbalCertification"))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult(
                    "The Receipt of Verbal Certification Date is required.", new[] { "VerbalCertificationDate" }));
                allValid = false;
            }

            if (allValid && (IsVerbalCOTIForm))
            {
                ResetAdmissionCOTIFieldsIsVerbalCOTIForm(); // Refresh now that we are sure we have a signing physician
            }

            if (((IsCOTIForm) || (IsVerbalCOTIForm)) && (AdmissionCOTI.IsMedDirect == false) &&
                (AdmissionCOTI.IsAttending == false))
            {
                AdmissionCOTI.ValidationErrors.Add(new ValidationResult(
                    "The Physician must be defined as the Medical Director or an Attending Physician for this admission.",
                    new[] { "SigningAdmissionPhysicianKey" }));
                allValid = false;
            }

            return allValid;
        }

        private new void CreateDialogue(String Title, String Message)
        {
            NavigateCloseDialog d = new NavigateCloseDialog
            {
                Width = double.NaN,
                Height = double.NaN,
                ErrorMessage = Message,
                ErrorQuestion = null,
                Title = Title,
                HasCloseButton = false,
                OKLabel = "OK",
                NoVisible = false
            };
            d.Show();
        }

        public int GetOriginalSigningPhysicianKey()
        {
            return _OriginalSigningPhysicianKey;
        }

        private int _OriginalSigningPhysicianKey;

        public int OriginalSigningPhysicianKey
        {
            get { return _OriginalSigningPhysicianKey; }
            set
            {
                System.Diagnostics.Debug.WriteLine(
                    $"OriginalSigningPhysicianKey - change from {_OriginalSigningPhysicianKey} to {value}");
                _OriginalSigningPhysicianKey = value;
            }
        }

        public FaxingPhysician? GetFaxingPhysician()
        {
            if (AdmissionCOTI == null)
            {
                return null;
            }

            if (AdmissionCOTI.SigningPhysicianKey.HasValue == false)
            {
                return null;
            }

            if (AdmissionCOTI.SigningAdmissionPhysicianKey.HasValue == false)
            {
                return null;
            }

            PhysicianAddress physicianAddress =
                PhysicianCache.Current.GetPhysicianAddressFromKey(AdmissionCOTI.SigningPhysicianAddressKey);
            if (physicianAddress != null && string.IsNullOrWhiteSpace(physicianAddress.Fax) == false)
            {
                return new FaxingPhysician
                    { PhysicianKey = AdmissionCOTI.SigningPhysicianKey.Value, FaxNumber = physicianAddress.Fax };
            }

            return null;
        }
    }

    public class HospiceAdmissionCOTIFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceAdmissionCOTI hac = new HospiceAdmissionCOTI(__FormSectionQuestionKey)
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