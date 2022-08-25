#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class ReasonForDischarge : QuestionBase
    {
        public ReasonForDischarge(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void ReasonForDischargeSetup()
        {
            Messenger.Default.Register<int>(this, "AdmissionPhysician_FormUpdate", AdmissionKey => { SetupAdmissionPhysicianDefaults(); });
            if ((AdmissionDiscipline != null) && (Question != null) &&
                (string.IsNullOrWhiteSpace(Question.Label) == false) &&
                (Question.Label.StartsWith("Reason for Discharge (V")))
            {
                AdmissionDiscipline.DischargeVersion = 2;
            }

            SetupAdmissionDisciplineDefaults();
            SetupAdmissionPhysicianDefaults();
        }

        private int diedREASONDCKey => (int)CodeLookupCache.GetKeyFromCode("REASONDC", "DIED");
        private int diedPATDISCHARGEREASONKey => (int)CodeLookupCache.GetKeyFromCode("PATDISCHARGEREASON", "20");
        private int routinePATDISCHARGEREASONKey => (int)CodeLookupCache.GetKeyFromCode("PATDISCHARGEREASON", "01");

        public int? DischargeAdmissionPhysicianKey
        {
            get { return AdmissionDiscipline?.DischargeAdmissionPhysicianKey; }
            set
            {
                if (AdmissionDiscipline != null)
                {
                    AdmissionDiscipline.DischargeAdmissionPhysicianKey = value;
                }

                RaisePropertyChanged("DischargeAdmissionPhysicianKey");
                RaisePropertyChanged("DischargeAdmissionPhysician");
                if (DischargeAdmissionPhysician != null)
                {
                    DischargeAdmissionPhysician.RaiseAddressChanged();
                }
            }
        }

        public AdmissionPhysician DischargeAdmissionPhysician
        {
            get
            {
                return ((Admission != null) && (Admission.AdmissionPhysician != null))
                    ? Admission.AdmissionPhysician.FirstOrDefault(p => p.AdmissionPhysicianKey == AdmissionDiscipline.DischargeAdmissionPhysicianKey)
                    : null;
            }
        }

        public List<AdmissionPhysician> DischargeAdmissionPhysicianList { get; set; }

        public int? FollowAdmissionPhysicianKey
        {
            get { return AdmissionDiscipline?.FollowAdmissionPhysicianKey; }
            set
            {
                if (AdmissionDiscipline != null)
                {
                    AdmissionDiscipline.FollowAdmissionPhysicianKey = value;
                }

                RaisePropertyChanged("FollowAdmissionPhysicianKey");
                RaisePropertyChanged("FollowAdmissionPhysician");
                if (FollowAdmissionPhysician != null)
                {
                    FollowAdmissionPhysician.RaiseAddressChanged();
                }
            }
        }

        public AdmissionPhysician FollowAdmissionPhysician
        {
            get
            {
                return ((Admission != null) && (Admission.AdmissionPhysician != null))
                    ? Admission.AdmissionPhysician
                        .FirstOrDefault(p => p.AdmissionPhysicianKey == AdmissionDiscipline.FollowAdmissionPhysicianKey)
                    : null;
            }
        }

        public List<AdmissionPhysician> FollowAdmissionPhysicianList { get; set; }

        public int? ReasonDCKey
        {
            get { return AdmissionDiscipline?.ReasonDCKey; }
            set
            {
                if ((AdmissionDiscipline == null) || (Encounter == null) || (Encounter.EncounterIsInEdit == false))
                {
                    return;
                }

                bool prevAgencyDischarge = AdmissionDiscipline.AgencyDischarge;
                AdmissionDiscipline.ReasonDCKey = value;
                if (ReasonDCKey == diedREASONDCKey)
                {
                    AdmissionDiscipline.AgencyDischarge = true;
                    AdmissionDiscipline.DischargeReasonKey = diedPATDISCHARGEREASONKey;
                }
                else
                {
                    if (Admission != null)
                    {
                        Admission.CalculateAgencyDischarge(AdmissionDiscipline,
                            AdmissionDiscipline.OverrideAgencyDischarge);
                    }

                    if (AdmissionDiscipline.DischargeReasonKey == diedPATDISCHARGEREASONKey)
                    {
                        AdmissionDiscipline.DischargeReasonKey = null;
                    }
                }

                if (AdmissionDiscipline.AgencyDischarge)
                {
                    AdmissionDiscipline.DatePhysicianNotified = null;
                }

                if (prevAgencyDischarge != AdmissionDiscipline.AgencyDischarge)
                {
                    SetupOASISByPass();
                    SetupAdmissionPhysicianDefaults();
                }
            }
        }

        private void SetupAdmissionDisciplineDefaults()
        {
            if ((Admission == null) || (AdmissionDiscipline == null) || (Encounter == null) ||
                (Encounter.EncounterStatus != (int)EncounterStatusType.Edit))
            {
                return;
            }

            if ((Patient != null) && (Patient.DeathDate != null) && (AdmissionDiscipline.ReasonDCKey == null))
            {
                ReasonDCKey = diedREASONDCKey;
            }
            else
            {
                Admission.CalculateAgencyDischarge(AdmissionDiscipline, AdmissionDiscipline.OverrideAgencyDischarge);
                SetupOASISByPass();
            }
        }

        private void SetupOASISByPass()
        {
            if ((OasisManager == null) || (Encounter == null) || (Encounter.EncounterIsInEdit == false) ||
                (AdmissionDiscipline == null))
            {
                return;
            }

            if (AdmissionDiscipline.AgencyDischarge)
            {
                OasisManager.ForceUnBypassDischarge();
            }
            else
            {
                if (Patient != null)
                {
                    Patient.DeathDate = null;
                }

                OasisManager.ForceBypassDischarge();
            }
        }

        private void SetupAdmissionPhysicianDefaults()
        {
            if ((Admission == null) || (Admission.AdmissionPhysician == null) || (AdmissionDiscipline == null) ||
                (Encounter == null) || (Encounter.Task == null))
            {
                return;
            }

            // Always setup both DischargeAdmissionPhysician and FollowAdmissionPhysician, validation will do the rest
            // If the form is no in edit 
            DischargeAdmissionPhysicianList = new List<AdmissionPhysician>();
            FollowAdmissionPhysicianList = new List<AdmissionPhysician>();
            if (Encounter.EncounterIsInEdit)
            {
                DateTime? serviceDate = (AdmissionDiscipline.DischargeDateTime != null)
                    ? ((DateTime)AdmissionDiscipline.DischargeDateTime).Date
                    : (DateTime?)null;
                if ((serviceDate == null) && (Encounter.EncounterOrTaskStartDateAndTime != null))
                {
                    serviceDate = ((DateTimeOffset)Encounter.EncounterOrTaskStartDateAndTime).Date;
                }

                if (serviceDate == null)
                {
                    serviceDate = DateTime.Today.Date;
                }

                serviceDate = ((DateTime)serviceDate).Date;
                AdmissionPhysician dap = Admission.AdmissionPhysician
                    .Where(p => p.Inactive == false)
                    .Where(p => p.Signing)
                    .Where(p => (p.SigningEffectiveFromDate.HasValue &&
                                 (p.SigningEffectiveFromDate.Value.Date <= serviceDate)) &&
                                (!p.SigningEffectiveThruDate.HasValue || (p.SigningEffectiveThruDate.HasValue &&
                                                                          (p.SigningEffectiveThruDate.Value.Date >=
                                                                           serviceDate))))
                    .FirstOrDefault();
                DischargeAdmissionPhysicianKey = (dap == null) ? (int?)null : dap.AdmissionPhysicianKey;
                int physicianType = (CodeLookupCache.GetKeyFromCode("PHTP", "Follow") == null)
                    ? 0
                    : (int)(CodeLookupCache.GetKeyFromCode("PHTP", "Follow"));
                AdmissionPhysician fap = Admission.AdmissionPhysician
                    .Where(p => p.Inactive == false)
                    .Where(p => p.PhysicianType == physicianType)
                    .Where(p => (p.PhysicianEffectiveFromDate.Date <= serviceDate) &&
                                (!p.PhysicianEffectiveThruDate.HasValue || (p.PhysicianEffectiveThruDate.HasValue &&
                                                                            (p.PhysicianEffectiveThruDate.Value.Date >=
                                                                             serviceDate))))
                    .FirstOrDefault();
                FollowAdmissionPhysicianKey = fap?.AdmissionPhysicianKey;
            }

            if (DischargeAdmissionPhysician != null)
            {
                DischargeAdmissionPhysicianList.Add(DischargeAdmissionPhysician);
            }

            if (FollowAdmissionPhysician != null)
            {
                FollowAdmissionPhysicianList.Add(FollowAdmissionPhysician);
            }

            this.RaisePropertyChangedLambda(p => p.DischargeAdmissionPhysicianList);
            this.RaisePropertyChangedLambda(p => p.DischargeAdmissionPhysician);
            this.RaisePropertyChangedLambda(p => p.DischargeAdmissionPhysicianKey);
            if (DischargeAdmissionPhysician != null)
            {
                DischargeAdmissionPhysician.RaiseAddressChanged();
            }

            this.RaisePropertyChangedLambda(p => p.FollowAdmissionPhysicianList);
            this.RaisePropertyChangedLambda(p => p.FollowAdmissionPhysician);
            this.RaisePropertyChangedLambda(p => p.FollowAdmissionPhysicianKey);
            if (FollowAdmissionPhysician != null)
            {
                FollowAdmissionPhysician.RaiseAddressChanged();
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            bool allValid = true;
            if ((Encounter == null) || (Patient == null) || (AdmissionDiscipline == null))
            {
                return true;
            }

            if (Encounter.FullValidation == false)
            {
                return true;
            }

            if (AdmissionDiscipline.ReasonDCKey <= 0)
            {
                ReasonDCKey = null;
            }

            if (AdmissionDiscipline.DischargeReasonKey <= 0)
            {
                AdmissionDiscipline.DischargeReasonKey = null;
            }

            if (AdmissionDiscipline.FollowAdmissionPhysicianKey <= 0)
            {
                AdmissionDiscipline.FollowAdmissionPhysicianKey = null;
            }

            if (AdmissionDiscipline.DischargeAdmissionPhysicianKey <= 0)
            {
                AdmissionDiscipline.DischargeAdmissionPhysicianKey = null;
            }

            if (AdmissionDiscipline.DatePhysicianNotified == DateTime.MinValue)
            {
                AdmissionDiscipline.DatePhysicianNotified = null;
            }

            if (string.IsNullOrWhiteSpace(AdmissionDiscipline.PostDischargeGoals))
            {
                AdmissionDiscipline.PostDischargeGoals = null;
            }

            if (string.IsNullOrWhiteSpace(AdmissionDiscipline.PostDischargeTreatmentPreferences))
            {
                AdmissionDiscipline.PostDischargeTreatmentPreferences = null;
            }

            if (AdmissionDiscipline.ReasonDCKey == diedREASONDCKey)
            {
                AdmissionDiscipline.DischargeReasonKey = diedPATDISCHARGEREASONKey;
            }

            if (AdmissionDiscipline.AgencyDischarge)
            {
                if ((AdmissionDiscipline.DischargeReasonKey != null) &&
                    (AdmissionDiscipline.DischargeReasonKey != diedPATDISCHARGEREASONKey))
                {
                    AdmissionDiscipline.DischargeReasonKey = routinePATDISCHARGEREASONKey;
                }

                AdmissionDiscipline.DischargeAdmissionPhysicianKey = null;
                AdmissionDiscipline.DatePhysicianNotified = null;
            }
            else
            {
                AdmissionDiscipline.FollowAdmissionPhysicianKey = null;
                AdmissionDiscipline.PostDischargeGoals = null;
                AdmissionDiscipline.PostDischargeTreatmentPreferences = null;
            }

            if (AdmissionDiscipline.ShowFollowAdmissionPhysician == false)
            {
                AdmissionDiscipline.FollowAdmissionPhysicianKey = null;
            }

            if (AdmissionDiscipline.ReasonDCKey == null)
            {
                AdmissionDiscipline.ValidationErrors.Add(
                    new ValidationResult("The Reason for Discharge to field is required.", new[] { "ReasonDCKey" }));
                allValid = false;
            }

            if ((AdmissionDiscipline.DischargeReasonKey == null) && AdmissionDiscipline.AgencyDischarge)
            {
                AdmissionDiscipline.ValidationErrors.Add(
                    new ValidationResult("The Discharge/Transfer to field is required.",
                        new[] { "DischargeReasonKey" }));
                allValid = false;
            }

            if ((AdmissionDiscipline.ReasonDCKey != diedREASONDCKey) &&
                (AdmissionDiscipline.DischargeReasonKey == diedPATDISCHARGEREASONKey) &&
                AdmissionDiscipline.AgencyDischarge)
            {
                AdmissionDiscipline.ValidationErrors.Add(new ValidationResult(
                    "The Discharge/Transfer to cannot be 'Expired' unless the Reason for Discharge is 'Expired'.",
                    new[] { "DischargeReasonKey" }));
                allValid = false;
            }

            if ((AdmissionDiscipline.FollowAdmissionPhysicianKey == null) &&
                AdmissionDiscipline.ShowFollowAdmissionPhysician)
            {
                AdmissionDiscipline.ValidationErrors.Add(new ValidationResult(
                    "The Follow After Discharge Physician field is required.",
                    new[] { "FollowAdmissionPhysicianKey" }));
                allValid = false;
            }

            if ((AdmissionDiscipline.DischargeAdmissionPhysicianKey == null) &&
                (AdmissionDiscipline.AgencyDischarge == false))
            {
                AdmissionDiscipline.ValidationErrors.Add(new ValidationResult("The Physician field is required.",
                    new[] { "DischargeAdmissionPhysicianKey" }));
                allValid = false;
            }

            if ((AdmissionDiscipline.DatePhysicianNotified == null) && (AdmissionDiscipline.AgencyDischarge == false))
            {
                AdmissionDiscipline.ValidationErrors.Add(new ValidationResult(
                    "The Date Physician Notified field is required.", new[] { "DatePhysicianNotified" }));
                allValid = false;
            }

            if (Patient != null)
            {
                Patient.ValidationErrors.Clear();
                if ((AdmissionDiscipline.ReasonDCKey != null) && (AdmissionDiscipline.ReasonDCKey == diedREASONDCKey) &&
                    (Patient.DeathDate == null))
                {
                    Patient.ValidationErrors.Add(new ValidationResult("The Date of Death field is required.",
                        new[] { "DeathDate" }));
                    allValid = false;
                }
            }

            if ((AdmissionDiscipline.PostDischargeGoals == null) && AdmissionDiscipline.AgencyDischargeVersion2OrHigher)
            {
                AdmissionDiscipline.ValidationErrors.Add(
                    new ValidationResult("The Post-Discharge Goals field is required.",
                        new[] { "PostDischargeGoals" }));
                allValid = false;
            }

            if ((AdmissionDiscipline.PostDischargeTreatmentPreferences == null) &&
                AdmissionDiscipline.AgencyDischargeVersion2OrHigher)
            {
                AdmissionDiscipline.ValidationErrors.Add(new ValidationResult(
                    "The Post-Discharge Treatment Preferences field is required.",
                    new[] { "PostDischargeTreatmentPreferences" }));
                allValid = false;
            }

            return allValid;
        }

        public override void Cleanup()
        {
            Messenger.Default.Unregister<int>(this, "AdmissionPhysician_FormUpdate");
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }
    }

    public class ReasonForDischargeFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            ReasonForDischarge rd = new ReasonForDischarge(__FormSectionQuestionKey)
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
            rd.ReasonForDischargeSetup();
            return rd;
        }
    }
}