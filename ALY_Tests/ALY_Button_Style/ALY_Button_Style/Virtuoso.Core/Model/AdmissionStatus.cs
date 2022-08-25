#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class AdmissionStatus : QuestionUI
    {
        public OrderEntryManager OrderEntryManager { get; set; }

        public AdmissionPhysicianFacade AdmissionPhysician { get; set; }

        public string ServicePriorityOneLabel =>
            CodeLookupCache.GetCodeLookupHeaderDescriptionFromType("ServicePriorityOne");

        public string ServicePriorityTwoLabel =>
            CodeLookupCache.GetCodeLookupHeaderDescriptionFromType("ServicePriorityTwo");

        public string ServicePriorityThreeLabel =>
            CodeLookupCache.GetCodeLookupHeaderDescriptionFromType("ServicePriorityThree");

        public int? ServicePriorityOne
        {
            get
            {
                var obj = AdmissionOrEncounterAdmission as IEncounterAdmission;
                if (obj != null)
                {
                    return obj.ServicePriorityOne;
                }

                return null;
            }
            set
            {
                var admission = AdmissionOrEncounterAdmission as Admission;
                if (admission != null)
                {
                    admission.ServicePriorityOne = value;
                }

                ValidateServicePriority();
                RaisePropertyChanged("ServicePriorityOne");
                FireErrorsChanged("ServicePriorityOne");
            }
        }

        private string _PatientInsuranceValidationMessage;

        public string PatientInsuranceValidationMessage
        {
            get { return _PatientInsuranceValidationMessage; }
            set
            {
                _PatientInsuranceValidationMessage = value;
                RaisePropertyChanged("PatientInsuranceValidationMessage");
            }
        }

        public int? ServicePriorityTwo
        {
            get
            {
                var obj = AdmissionOrEncounterAdmission as IEncounterAdmission;
                if (obj != null)
                {
                    return obj.ServicePriorityTwo;
                }

                return null;
            }
            set
            {
                var admission = AdmissionOrEncounterAdmission as Admission;
                if (admission != null)
                {
                    admission.ServicePriorityTwo = value;
                }

                ValidateServicePriority();
                RaisePropertyChanged("ServicePriorityTwo");
                FireErrorsChanged("ServicePriorityTwo");
            }
        }

        public int? ServicePriorityThree
        {
            get
            {
                var obj = AdmissionOrEncounterAdmission as IEncounterAdmission;
                if (obj != null)
                {
                    return obj.ServicePriorityThree;
                }

                return null;
            }
            set
            {
                var admission = AdmissionOrEncounterAdmission as Admission;
                if (admission != null)
                {
                    admission.ServicePriorityThree = value;
                }

                ValidateServicePriority();
                RaisePropertyChanged("ServicePriorityThree");
                FireErrorsChanged("ServicePriorityThree");
            }
        }

        void AdmissionStatus_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Admission")
            {
                AdmissionPhysician.Admission = Admission;
            }

            if (e.PropertyName == "Encounter")
            {
                AdmissionPhysician.Encounter = Encounter;
            }
        }

        public AdmissionStatus(Admission admission, Encounter encounter, int? formSectionQuestionKey) : base(
            formSectionQuestionKey)
        {
            Messenger.Default.Register<int>(this,
                "AdmissionPhysician_FormUpdate",
                AdmissionKey => { AdmissionPhysician.RaiseEvents(); });

            Admission = admission;
            Encounter = encounter;
            Admission.AdmissionGroupDate = Encounter.EncounterStartDate.HasValue
                ? Encounter.EncounterStartDate.Value.DateTime
                : DateTime.Today;
            AdmissionPhysician = new AdmissionPhysicianFacade();
            AdmissionPhysician.Admission = Admission;
            AdmissionPhysician.Encounter = Encounter;
            PropertyChanged += AdmissionStatus_PropertyChanged;
            Admission.PropertyChanged += Admission_PropertyChanged;
        }

        void Admission_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PatientInsuranceKey")
            {
                if ((Admission != null)
                    && (Admission.PatientInsurance != null)
                    && (Admission.PatientInsurance.InsuranceKey.HasValue)
                   )
                {
                    Insurance i = InsuranceCache.GetInsuranceFromKey(Admission.PatientInsurance.InsuranceKey);
                    if (i != null)
                    {
                        if (i.FaceToFaceOnAdmit
                            && (!Admission.FaceToFaceEncounter.HasValue)
                           )
                        {
                            Admission.FaceToFaceEncounter = CodeLookupCache.GetKeyFromCode("FACETOFACE", "DoWithCert");
                        }
                        else if ((!i.FaceToFaceOnAdmit)
                                 && Admission.FaceToFaceEncounter.HasValue
                                 && (Admission.FaceToFaceEncounter ==
                                     CodeLookupCache.GetKeyFromCode("FACETOFACE", "DoWithCert"))
                                )
                        {
                            Admission.FaceToFaceEncounter = null;
                        }
                    }
                }
            }

            if (e.PropertyName == "FirstCertPeriodNumber")
            {
                this.RaisePropertyChangedLambda(p => p.AdmitStatCanModifyCertFrom);
            }
        }

        public override void Cleanup()
        {
            Messenger.Default.Unregister(this);

            foreach (AdmissionConsent admissionConsent in Admission.AdmissionConsent)
                Messenger.Default.Unregister(admissionConsent);

            if (AdmissionPhysician != null)
            {
                AdmissionPhysician = null;
            }

            PropertyChanged -= AdmissionStatus_PropertyChanged;

            if (Admission != null)
            {
                Admission.PropertyChanged -= Admission_PropertyChanged;
            }

            AdmissionReferralViewReferralNotes_Command = null;
            base.Cleanup();
        }

        public CollectionViewSource _physicianF2FICDList = new CollectionViewSource();
        public ICollectionView PhysicianF2FICDList => _physicianF2FICDList.View;
        private CollectionViewSource _AdmissionConsents = new CollectionViewSource();
        public ICollectionView FilteredAdmissionConsents => _AdmissionConsents.View;

        public void NotifyICDListChanged()
        {
            this.RaisePropertyChangedLambda(p => p.PhysicianF2FICDList);
        }

        public RelayCommand AddF2FRowCommand { get; set; }
        public RelayCommand<object> RemoveRowCommand { get; set; }
        public RelayCommand FacilityChanged { get; set; }
        public RelayCommand GroupChanged { get; set; }
        public RelayCommand GroupDropDownClosed { get; set; }
        public RelayCommand AdmissionConsentAddItem_Command { get; protected set; }
        public RelayCommand<AdmissionConsent> AdmissionConsentCancel_Command { get; protected set; }
        public RelayCommand<AdmissionConsent> AdmissionConsentOK_Command { get; protected set; }
        public RelayCommand<AdmissionConsent> AdmissionConsentEditItem_Command { get; protected set; }
        public RelayCommand AdmissionCareCoordinatorHistory_Command { get; private set; }

        public bool AdmitStatCanModifyHospiceTransfer => Admission == null ? false : AdmitStatCanModifyCertCycles;

        public bool AdmitStatCanModifyCertCycles
        {
            get
            {
                if (Encounter.Inactive)
                {
                    return false;
                }

                return Admission.CanModifyCertCycle && EncounterIsBillable;
            }
        }

        public bool AdmitStatCanModifyCertPeriod
        {
            get
            {
                if (AdmitStatCanModifyCertCycles == false)
                {
                    return false;
                }

                if (Encounter == null)
                {
                    return false;
                }

                if ((Encounter.EncounterCyclePdNum == null) || (Encounter.EncounterCyclePdNum <= 0))
                {
                    return true;
                }

                if ((Admission != null) && (Admission.StartPeriodNumber != null) &&
                    (Encounter.EncounterCyclePdNum == Admission.StartPeriodNumber))
                {
                    return false;
                }

                return true;
            }
        }

        public bool AdmitStatCanModifyCertFrom =>
            AdmitStatCanModifyCertCycles
            && ((Admission.CurrentCertPeriodNumber.HasValue
                 && (Admission.CurrentCertPeriodNumber != 1)
                )
                || Admission.TransferHospice
            );

        public bool AdmitStatCanEditSOC
        {
            get
            {
                if ((DynamicFormViewModel != null) && DynamicFormViewModel.CMSProtectSOCDate)
                {
                    return false;
                }

                if (IsResumption)
                {
                    return false;
                }

                return AdmitStatCanModifyCertCycles && !Protected;
            }
        }

        public bool EncounterIsBillable
        {
            get
            {
                bool isBillable = true;
                if (Encounter.ServiceTypeKey != null)
                {
                    isBillable = ServiceTypeCache.IsBillable((int)Encounter.ServiceTypeKey);
                }

                return isBillable;
            }
        }

        public void RefreshCurrentGroups()
        {
            RaisePropertyChanged("CurrentGroup");
            RaisePropertyChanged("CurrentGroup2");
            RaisePropertyChanged("CurrentGroup3");
            RaisePropertyChanged("CurrentGroup4");
            RaisePropertyChanged("CurrentGroup5");
        }

        private bool ValidateReferralDate()
        {
            bool AnyErrors = false;

            if (Encounter.FullValidation == false)
            {
                return AnyErrors;
            }

            if (Admission.AdmissionStatusCode == "N")
            {
                return AnyErrors;
            }

            if (Question.DataTemplate.Equals("AdmissionStatus") == false)
            {
                return AnyErrors;
            }

            if (EncounterResumption != null)
            {
                if ((EncounterResumption.ResumptionReferralDate == null) ||
                    (EncounterResumption.ResumptionReferralDate == null))
                {
                    EncounterResumption.ValidationErrors.Add(new ValidationResult(
                        Admission.ReferralDateLabel + " is required", new[] { "ResumptionReferralDate" }));
                    AnyErrors = true;
                }

                if ((Admission.HospiceAdmission == false) && (EncounterResumption != null) &&
                    (MostRecentAdmissionReferralReferralDate != null) &&
                    (EncounterResumption.ResumptionReferralDate != null) &&
                    (((DateTime)EncounterResumption.ResumptionReferralDate).Date) <
                    ((DateTime)MostRecentAdmissionReferralReferralDate).Date)
                {
                    EncounterResumption.ValidationErrors.Add(new ValidationResult(
                        Admission.ReferralDateLabel + " cannot be prior to the Re-Referral Date",
                        new[] { "ResumptionReferralDate" }));
                    AnyErrors = true;
                }

                return AnyErrors;
            }

            if (Admission.HospiceAdmission && (Admission.InitialReferralDate == null) ||
                (Admission.InitialReferralDate == null))
            {
                Admission.ValidationErrors.Add(new ValidationResult("Referral Date is required",
                    new[] { "InitialReferralDate" }));
                AnyErrors = true;
            }

            if (Admission.InitialReferralDate.HasValue &&
                Admission.InitialReferralDate.Value.Date > DateTime.Today.Date)
            {
                Admission.ValidationErrors.Add(new ValidationResult("Referral Date cannot be a future date",
                    new[] { "InitialReferralDate" }));
                AnyErrors = true;
            }

            return AnyErrors;
        }

        public override bool Validate(out string SubSections)
        {
            bool AnyErrors = false;
            SubSections = string.Empty;

            // do not recalculate admission status on previously completed forms
            if (Encounter == null)
            {
                return true;
            }

            if (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed)
            {
                return true;
            }

            if (EncounterResumption != null)
            {
                EncounterResumption.ValidationErrors.Clear();
                if (EncounterResumption.Validate())
                {
                    if (EncounterResumption.IsNew)
                    {
                        if (Encounter.EncounterResumption.Contains(EncounterResumption) == false)
                        {
                            Encounter.EncounterResumption.Add(EncounterResumption);
                        }
                    }
                }
            }

            PatientInsuranceValidationMessage = null;
            if (!Question.DataTemplate.Equals("CertCycles"))
            {
                Admission.ValidationErrors.Clear();
                if (Admission.CurrentGroup != null)
                {
                    Admission.CurrentGroup.ValidationErrors.Clear();
                }

                if (Admission.CurrentGroup2 != null)
                {
                    Admission.CurrentGroup2.ValidationErrors.Clear();
                }

                if (Admission.CurrentGroup3 != null)
                {
                    Admission.CurrentGroup3.ValidationErrors.Clear();
                }

                if (Admission.CurrentGroup4 != null)
                {
                    Admission.CurrentGroup4.ValidationErrors.Clear();
                }

                if (Admission.CurrentGroup5 != null)
                {
                    Admission.CurrentGroup5.ValidationErrors.Clear();
                }

                if (Admission.CurrentReferral != null)
                {
                    Admission.CurrentReferral.ValidationErrors.Clear();
                }
            }

            // Clear out any PatientID data that is not applicable to this patients facility
            if (Admission.CurrentReferral != null)
            {
                if (Admission.FacilityKey.HasValue)
                {
                    if (string.IsNullOrEmpty(FacilityCache.GetPatientIDLabelFromKey(Admission.FacilityKey, 1)))
                    {
                        Admission.CurrentReferral.PatientID1 = null;
                    }

                    if (string.IsNullOrEmpty(FacilityCache.GetPatientIDLabelFromKey(Admission.FacilityKey, 2)))
                    {
                        Admission.CurrentReferral.PatientID2 = null;
                    }

                    if (string.IsNullOrEmpty(FacilityCache.GetPatientIDLabelFromKey(Admission.FacilityKey, 3)))
                    {
                        Admission.CurrentReferral.PatientID3 = null;
                    }
                }
                else if (Admission.CurrentReferral.FacilityKey.HasValue)
                {
                    if (string.IsNullOrEmpty(
                            FacilityCache.GetPatientIDLabelFromKey(Admission.CurrentReferral.FacilityKey, 1)))
                    {
                        Admission.CurrentReferral.PatientID1 = null;
                    }

                    if (string.IsNullOrEmpty(
                            FacilityCache.GetPatientIDLabelFromKey(Admission.CurrentReferral.FacilityKey, 2)))
                    {
                        Admission.CurrentReferral.PatientID2 = null;
                    }

                    if (string.IsNullOrEmpty(
                            FacilityCache.GetPatientIDLabelFromKey(Admission.CurrentReferral.FacilityKey, 3)))
                    {
                        Admission.CurrentReferral.PatientID3 = null;
                    }
                }
                else
                {
                    Admission.CurrentReferral.PatientID1 = null;
                    Admission.CurrentReferral.PatientID2 = null;
                    Admission.CurrentReferral.PatientID3 = null;
                }
            }

            // Status is a function of NotTaken checkbox - checked = N(NotTaken) and Admitted checkbox - checked = A(Admitted)
            // otherwise leave the status as is - probably R(Referred)
            if (AdmissionDiscipline.NotTaken)
            {
                if (AdmissionStatusHelper.CanChangeToNotTakenStatus(AdmissionDiscipline.AdmissionStatus))
                {
                    AdmissionDiscipline.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_NotTaken;
                    AdmissionDiscipline.DisciplineAdmitDateTime = null;
                }

                if (!AdmissionDiscipline.NotTakenDateTime.HasValue)
                {
                    AdmissionDiscipline.NotTakenDateTime = CalculateDisciplineNotAdmitDateTime;
                }

                if (!Admission.AdmissionDiscipline.Where(p =>
                        (p.AdmissionDisciplineKey != AdmissionDiscipline.AdmissionDisciplineKey) &&
                        (p.IsAdmissionStatusNTUCorDischarge == false)).Any())
                {
                    // there are no 'Active' AdmissionDisciplines (i.e., they are all NTUCed or Discharged)

                    if (AdmissionStatusHelper.CanChangeToNotTakenStatus(Admission.AdmissionStatus))
                    {
                        Admission.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_NotTaken;
                        Admission.NotTakenDateTime = AdmissionDiscipline.NotTakenDateTime;
                        Admission.NotTakenReason = AdmissionDiscipline.NotTakenReason;
                        if (!Admission.AdmissionDiscipline.Where(p =>
                                (p.AdmissionDisciplineKey != AdmissionDiscipline.AdmissionDisciplineKey) &&
                                p.IsAdmissionStatusDischarge).Any())
                        {
                            // only clear the following it there are/were no discharged disciplines 
                            // i.e., can't clear these if a re-referral has been NTUCed
                            Admission.AdmitDateTime = null;
                            Admission.SOCDate = null;
                        }
                    }
                }
            }
            else if (AdmissionDiscipline.Admitted)
            {
                if (AdmissionStatusHelper.CanDisciplineChangeToAdmittedStatus(AdmissionDiscipline.AdmissionStatus))
                {
                    AdmissionDiscipline.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Admitted;
                }

                if (!AdmissionDiscipline.DisciplineAdmitDateTime.HasValue)
                {
                    AdmissionDiscipline.DisciplineAdmitDateTime = CalculateDisciplineAdmitDateTime;
                }

                AdmissionDiscipline.NotTakenReason = null;
                AdmissionDiscipline.NotTakenDateTime = null;

                if (AdmissionStatusHelper.CanChangeToAdmittedStatus(Admission.AdmissionStatus))
                {
                    Admission.AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Admitted;
                    Admission.NotTakenReason = null;
                    Admission.NotTakenDateTime = null;
                    if (!Admission.AdmitDateTime.HasValue && AdmissionDiscipline.DisciplineAdmitDateTime.HasValue)
                    {
                        Admission.AdmitDateTime = AdmissionDiscipline.DisciplineAdmitDateTime.Value;
                    }

                    if (!Admission.SOCDate.HasValue)
                    {
                        Admission.SOCDate = AdmissionDiscipline.DisciplineAdmitDateTime;
                    }
                }
            }

            if ((Question.DataTemplate.Equals("AdmissionStatus")) &&
                (Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed))
            {
                if (Admission.CareCoordinator == null)
                {
                    Admission.ValidationErrors.Add(new ValidationResult("Care Coordinator is required",
                        new[] { "CareCoordinator" }));
                    AnyErrors = true;
                    DynamicFormViewModel.ValidEnoughToSave = false;
                }
            }

            if ((Patient?.PatientInsurance != null) && (Question.DataTemplate.Equals("AdmissionStatus")) &&
                (Admission.PatientInsuranceKey != null))
            {
                PatientInsurance pi = Patient.PatientInsurance
                    .Where(p => p.PatientInsuranceKey == Admission.PatientInsuranceKey).FirstOrDefault();
                Insurance i = (pi == null) ? null : InsuranceCache.GetInsuranceFromKey(pi.InsuranceKey);
                if (i != null)
                {
                    List<InsuranceCertDefinition> icdList = InsuranceCache.GetInsuranceCertDefs(pi.InsuranceKey);
                    if ((icdList == null) || (icdList.Count == 0))
                    {
                        PatientInsuranceValidationMessage = "There are no " +
                                                            ((Admission.HospiceAdmission == false)
                                                                ? "Certification Periods"
                                                                : "Periods of Care") +
                                                            " defined for the underlying Insurance: " + i.Name;
                        Admission.ValidationErrors.Add(new ValidationResult(PatientInsuranceValidationMessage,
                            new[] { "PatientInsuranceKey" }));
                        AnyErrors = true;
                        DynamicFormViewModel.ValidEnoughToSave = false;
                    }
                }
            }

            if (!Question.DataTemplate.Equals("CertCycles"))
            {
                // These must happen first since the call to 'Validate' on the entities will clear the errors
                Admission.ValidateState_IsEval = (DynamicFormViewModel.CurrentForm.IsEval ||
                                                  DynamicFormViewModel.CurrentForm.IsResumption);
                Admission.ValidateState_IsEvalFullValidation = ((Encounter.FullValidation) &&
                                                                (DynamicFormViewModel.CurrentForm.IsEval ||
                                                                 DynamicFormViewModel.CurrentForm.IsResumption));
                if (Encounter.FullValidation)
                {
                    Admission.IsContractProvider = TenantSettingsCache.Current.TenantSetting.ContractServiceProvider;
                }

                if (!Admission.Validate() || !AdmissionDiscipline.Validate() || AnyErrors)
                {
                    Admission.ValidateState_IsEval = false;
                    Admission.ValidateState_IsEvalFullValidation = false;
                    AnyErrors = true;
                }

                AnyErrors = AnyErrors || ValidateServicePriority();

                if (Encounter.FullValidation && !Admission.ValidateAdmissionPartial())
                {
                    AnyErrors = true;
                    foreach (var er in Admission.ValidationErrors)
                    {
                        if (!String.IsNullOrEmpty(ValidationError))
                        {
                            ValidationError += Environment.NewLine;
                        }

                        ValidationError = er.ErrorMessage;

                        // Add the admission error to the encounter in the instance where we are using the 'Encounter' portion of EncounterOrAdmission.
                        if (Encounter != null)
                        {
                            Encounter.ValidationErrors.Add(new ValidationResult(er.ErrorMessage, er.MemberNames));
                        }

                        AnyErrors = true;
                    }
                }

                if (Encounter.FullValidation && Admission.AdmissionStatusCode != "N")
                {
                    if (Admission.HospiceAdmission && (Question.DataTemplate.Equals("AdmissionStatus")))
                    {
                        if (Admission.SourceOfAdmission == null)
                        {
                            Admission.ValidationErrors.Add(
                                new ValidationResult(
                                    "Admitted From is required",
                                    new[] { "SourceOfAdmission" }));
                            AnyErrors = true;
                        }
                    }

                    AnyErrors = ValidateReferralDate() || AnyErrors;
                }

                AdmissionPhysician.ValidationErrors.Clear();
                AdmissionPhysician.FireErrorsChanged("AttendingPhysicianKey");
                AdmissionPhysician.FireErrorsChanged("SigningPhysicianKey");
                if (Encounter.FullValidationNTUC && Admission.AdmissionStatusCode != "N")
                {
                    if ((AdmissionPhysician.AttendingPhysicianKey == null) && (Encounter.FullValidation))
                    {
                        AdmissionPhysician.ValidationErrors.Add(
                            new ValidationResult(
                                "Attending/Primary Care Physician is required",
                                new[] { "AttendingPhysicianKey" }));
                        AdmissionPhysician.FireErrorsChanged("AttendingPhysicianKey");
                        AnyErrors = true;
                    }

                    if ((AdmissionPhysician.SigningPhysicianKey == null) && (Encounter.FullValidation))
                    {
                        AdmissionPhysician.ValidationErrors.Add(
                            new ValidationResult(
                                "Signing Physician is required",
                                new[] { "SigningPhysicianKey" }));
                        AdmissionPhysician.FireErrorsChanged("SigningPhysicianKey");
                        AnyErrors = true;
                    }

                    // We should probably use the AdmissionOrEncounterAdmission class for this, but since
                    // the EncounterAdmission record doesn't get created until the encounter is signed,
                    // and we really don't want to valdiate after signature since they can't fix anything
                    // I left it at the Admission level for now instead of dipping back to EncounterAdmission.
                    if (Encounter.FullValidation)
                    {
                        AnyErrors = AnyErrors || ValidateAdmissionGroupExistance();
                    }

                    if ((DynamicFormViewModel.CurrentForm.IsEval || DynamicFormViewModel.CurrentForm.IsResumption)
                        && !AdmissionDiscipline.Admitted && !AdmissionDiscipline.NotTaken
                        && AdmissionDiscipline.AdmissionStatus != AdmissionStatusHelper.AdmissionStatus_Transferred
                        && AdmissionDiscipline.AdmissionStatus != AdmissionStatusHelper.AdmissionStatus_Discharged)
                    {
                        AnyErrors = true;
                        AdmissionDiscipline.ValidationErrors.Add(new ValidationResult("Discipline Status is Required.",
                            new[] { "AdmissionStatus", "Admitted", "NotTaken" }));
                    }

                    if (AdmissionDiscipline.Admitted && AdmissionDiscipline.DisciplineAdmitDateTime != null &&
                        Admission.SOCDate != null
                        && AdmissionDiscipline.DisciplineAdmitDateTime.GetValueOrDefault().Date <
                        Admission.SOCDate.GetValueOrDefault().Date)
                    {
                        AnyErrors = true;
                        AdmissionDiscipline.ValidationErrors.Add(new ValidationResult(
                            string.Format(
                                "Discipline Admit Date ({0:d}) must be on or after the Admission Start of Care date. ({1:d}).",
                                AdmissionDiscipline.DisciplineAdmitDateTime.GetValueOrDefault().Date,
                                Admission.SOCDate.GetValueOrDefault().Date),
                            new[] { "AdmissionStatus", "Admitted", "DisciplineAdmitDateTime" }));
                    }

                    if ((DynamicFormViewModel.CurrentForm.IsEval || DynamicFormViewModel.CurrentForm.IsResumption)
                        && AdmissionDiscipline.Admitted && AdmissionDiscipline.DisciplineAdmitDateTime != null &&
                        Encounter.EncounterStartDate != null
                        && AdmissionDiscipline.DisciplineAdmitDateTime.GetValueOrDefault().Date !=
                        Encounter.EncounterStartDate.GetValueOrDefault().Date)
                    {
                        AnyErrors = true;
                        AdmissionDiscipline.ValidationErrors.Add(new ValidationResult(
                            string.Format("Discipline Admit Date must match the Service Date ({0:d}).",
                                Encounter.EncounterStartDate.GetValueOrDefault().Date),
                            new[] { "AdmissionStatus", "Admitted", "DisciplineAdmitDateTime" }));
                    }

                    DateTime? earliestBigFourServiceDate = EarliestBigFourServiceDate;
                    if (AdmitStatCanEditSOC && (DynamicFormViewModel.CurrentForm.IsEval) &&
                        (Admission.SOCDate != null) && (earliestBigFourServiceDate != null) &&
                        (Admission.SOCDate.GetValueOrDefault().Date < earliestBigFourServiceDate))
                    {
                        AnyErrors = true;
                        Admission.ValidationErrors.Add(new ValidationResult(
                            string.Format(
                                "Admission Start of Care date ({0:d}) cannot be before the initial service date of ({1:d}).",
                                Admission.SOCDate.GetValueOrDefault().Date, earliestBigFourServiceDate),
                            new[] { "SOCDate" }));
                    }

                    if ((DynamicFormViewModel.CurrentForm.IsEval || DynamicFormViewModel.CurrentForm.IsResumption)
                        && AdmissionDiscipline.NotTaken && AdmissionDiscipline.NotTakenDateTime == null)
                    {
                        AnyErrors = true;
                        AdmissionDiscipline.ValidationErrors.Add(new ValidationResult("Not Admitted Date is required.",
                            new[] { "NotTakenDateTime" }));
                    }

                    if ((DynamicFormViewModel.CurrentForm.IsEval || DynamicFormViewModel.CurrentForm.IsResumption)
                        && AdmissionDiscipline.NotTaken && AdmissionDiscipline.NotTakenDateTime != null &&
                        Encounter.EncounterStartDate != null
                        && AdmissionDiscipline.NotTakenDateTime.GetValueOrDefault().Date !=
                        Encounter.EncounterStartDate.GetValueOrDefault().Date)
                    {
                        AnyErrors = true;
                        AdmissionDiscipline.ValidationErrors.Add(new ValidationResult(
                            string.Format("Discipline Not Admitted Date ({0:d}) must match the Service Date ({1:d}).",
                                AdmissionDiscipline.NotTakenDateTime,
                                Encounter.EncounterStartDate.GetValueOrDefault().Date),
                            new[] { "AdmissionStatus", "NotTaken", "NotTakenDateTime" }));
                    }

                    if ((DynamicFormViewModel.CurrentForm.IsEval || DynamicFormViewModel.CurrentForm.IsResumption)
                        && AdmissionDiscipline.NotTaken && (string.IsNullOrEmpty(AdmissionDiscipline.NotTakenReason) ||
                                                            string.IsNullOrWhiteSpace(
                                                                AdmissionDiscipline.NotTakenReason)))
                    {
                        AnyErrors = true;
                        AdmissionDiscipline.ValidationErrors.Add(
                            new ValidationResult("Not Admitted Reason is required.", new[] { "NotTakenReason" }));
                    }
                }

                if (Encounter.FullValidationNTUC && Admission.AdmissionStatusCode == "N")
                {
                    if ((DynamicFormViewModel.CurrentForm.IsEval || DynamicFormViewModel.CurrentForm.IsResumption)
                        && !AdmissionDiscipline.Admitted && !AdmissionDiscipline.NotTaken)
                    {
                        AnyErrors = true;
                        AdmissionDiscipline.ValidationErrors.Add(new ValidationResult("Discipline Status is Required.",
                            new[] { "AdmissionStatus", "Admitted", "NotTaken" }));
                    }

                    if ((DynamicFormViewModel.CurrentForm.IsEval || DynamicFormViewModel.CurrentForm.IsResumption)
                        && AdmissionDiscipline.NotTaken && AdmissionDiscipline.NotTakenDateTime == null)
                    {
                        AnyErrors = true;
                        AdmissionDiscipline.ValidationErrors.Add(new ValidationResult("Not Admitted Date is required.",
                            new[] { "NotTakenDateTime" }));
                    }

                    if ((DynamicFormViewModel.CurrentForm.IsEval || DynamicFormViewModel.CurrentForm.IsResumption)
                        && AdmissionDiscipline.NotTaken && AdmissionDiscipline.NotTakenDateTime != null &&
                        Encounter.EncounterStartDate != null
                        && AdmissionDiscipline.NotTakenDateTime.GetValueOrDefault().Date !=
                        Encounter.EncounterStartDate.GetValueOrDefault().Date)
                    {
                        AnyErrors = true;
                        AdmissionDiscipline.ValidationErrors.Add(new ValidationResult(
                            string.Format("Discipline Not Admitted Date ({0:d}) must match the Service Date ({1:d}).",
                                AdmissionDiscipline.NotTakenDateTime,
                                Encounter.EncounterStartDate.GetValueOrDefault().Date),
                            new[] { "AdmissionStatus", "NotTaken", "NotTakenDateTime" }));
                    }

                    if ((DynamicFormViewModel.CurrentForm.IsEval || DynamicFormViewModel.CurrentForm.IsResumption)
                        && AdmissionDiscipline.NotTaken && (string.IsNullOrEmpty(AdmissionDiscipline.NotTakenReason) ||
                                                            string.IsNullOrWhiteSpace(
                                                                AdmissionDiscipline.NotTakenReason)))
                    {
                        AnyErrors = true;
                        AdmissionDiscipline.ValidationErrors.Add(
                            new ValidationResult("Not Admitted Reason is required.", new[] { "NotTakenReason" }));
                    }
                }

                if (Encounter.FullValidation)
                {
                    AnyErrors = AnyErrors || !Admission.ValidateTransferHospice();
                }

                bool anyAssociatedPatientFacilityStay = ValidateAssociatedPatientFacilityStay();
                AnyErrors = AnyErrors || !anyAssociatedPatientFacilityStay;
                if (AnyErrors)
                {
                    return false;
                }
            }
            else
            {
                AnyErrors = AnyErrors || ValidateHospiceAdmission();
                if (AnyErrors)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateAssociatedPatientFacilityStay()
        {
            // Flunk validation request if not applicable...
            if ((AssociatedPatientFacilityStay == null) || (Question.DataTemplate.Equals("AdmissionStatus") == false))
            {
                return true;
            }

            AssociatedPatientFacilityStay.ValidationErrors.Clear();
            if (AssociatedPatientFacilityStay.StartDate == DateTime.MinValue)
            {
                AssociatedPatientFacilityStay.StartDate = null;
            }

            if (AssociatedPatientFacilityStay.StartDate.HasValue)
            {
                AssociatedPatientFacilityStay.StartDate = ((DateTime)AssociatedPatientFacilityStay.StartDate).Date;
            }

            if (AssociatedPatientFacilityStay.EndDate == DateTime.MinValue)
            {
                AssociatedPatientFacilityStay.EndDate = null;
            }

            if (AssociatedPatientFacilityStay.EndDate.HasValue)
            {
                AssociatedPatientFacilityStay.EndDate = ((DateTime)AssociatedPatientFacilityStay.EndDate).Date;
            }

            // Cannot save bad facility data (bad dates) to the database
            bool AllValid = AssociatedPatientFacilityStay.Validate();
            // Additional validation on FacilityStay EndDate - It cannot be: a future date, or after the given discipline admit date
            if (AssociatedPatientFacilityStay.EndDate >
                DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date)
            {
                AssociatedPatientFacilityStay.ValidationErrors.Add(
                    new ValidationResult("The Facility Stay End Date cannot be in the future", new[] { "EndDate" }));
                AllValid = false;
            }

            if ((AdmissionDiscipline?.DisciplineAdmitDateTime != null) && (AssociatedPatientFacilityStay.EndDate >
                                                                           AdmissionDiscipline.DisciplineAdmitDateTime
                                                                               .Value.Date))
            {
                AssociatedPatientFacilityStay.ValidationErrors.Add(new ValidationResult(
                    "The Facility Stay End Date cannot be after the Discipline Admit Date", new[] { "EndDate" }));
                AllValid = false;
            }

            if (AllValid == false)
            {
                if (DynamicFormViewModel != null)
                {
                    DynamicFormViewModel.ValidEnoughToSave = false;
                }

                return AllValid;
            }

            // The FacilityStay EndDate is required for an Admit - not an NTUC
            if (Encounter.FullValidation && (AssociatedPatientFacilityStay.EndDate.HasValue == false) &&
                (Admission?.AdmissionStatusCode != "N"))
            {
                AssociatedPatientFacilityStay.ValidationErrors.Add(
                    new ValidationResult("The Facility Stay End Date is required", new[] { "EndDate" }));
                AllValid = false;
            }

            return AllValid;
        }

        private DateTime? EarliestBigFourServiceDate
        {
            get
            {
                if ((Admission == null || Admission.Encounter == null))
                {
                    return null;
                }

                Encounter e = Admission.Encounter
                    .Where(p => ((p.Inactive == false) && (p.EncounterStatus != 0) && (p.EncounterStartDate != null) &&
                                 p.EncounterIsEval && p.IsBigFourServiceType)).OrderBy(p => p.EncounterStartDate)
                    .FirstOrDefault();
                if (e == null)
                {
                    return null;
                }

                return ((DateTimeOffset)e.EncounterStartDate).Date;
            }
        }

        private bool ValidateAdmissionGroupExistance()
        {
            return Admission.ValidateAdmissionGroupExistance();
        }

        private bool ValidateHospiceAdmission()
        {
            bool AnyErrors = false;
            if (Admission == null || Encounter == null || !Admission.HospiceAdmission)
            {
                return AnyErrors;
            }

            if ((DynamicFormViewModel != null) && (DynamicFormViewModel.CurrentForm != null) &&
                DynamicFormViewModel.CurrentForm.IsTeamMeeting &&
                (Question != null) && (Question.DataTemplate != null) && (Question.DataTemplate.Equals("CertCycles")))
            {
                return AnyErrors; // skip hospice validation during team meeting
            }

            AnyErrors = !Admission.ValidateTransferHospice();
            if (Admission.TransferHospice)
            {
                //Validate newly created cert periods.
                if (Encounter.EncounterCyclePdNum <= 0)
                {
                    string[] memberNames = { "EncounterCyclePdNum" };
                    var Msg = "Period Number is required when 'Transfer From Another Hospice' is selected.";
                    Encounter.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AnyErrors = true;
                }

                if (!Encounter.EncounterCycleStartDate.HasValue)
                {
                    string[] memberNames = { "EncounterCycleStartDate" };
                    var Msg = "Period Start Date is required when 'Transfer From Another Hospice' is selected.";
                    Encounter.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AnyErrors = true;
                }

                if (!Encounter.EncounterCycleEndDate.HasValue)
                {
                    string[] memberNames = { "EncounterCycleEndDate" };
                    var Msg = "Period End Date is required when 'Transfer From Another Hospice' is selected.";
                    Encounter.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AnyErrors = true;
                }

                if (Encounter.EncounterCycleEndDate.HasValue && Admission.SOCDate.HasValue
                                                             && Encounter.EncounterCycleEndDate.Value.Date <
                                                             Admission.SOCDate.Value.Date) // verify period spans soc.
                {
                    string[] memberNames = { "EncounterCycleStartDate" };
                    var Msg =
                        "Period End Date must be on or after the Start of Care Date when 'Transfer From Another Hospice' is selected.";
                    Encounter.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AnyErrors = true;
                }

                if (Encounter.EncounterCycleStartDate.HasValue && Admission.SOCDate.HasValue
                                                               && (Encounter.EncounterCycleStartDate.Value.Date >
                                                                   Admission.SOCDate.Value.Date)
                                                               && (Admission.AdmissionCertification != null)
                                                               && Admission.AdmissionCertification.Any()
                                                               && (Admission.AdmissionCertification
                                                                       .Select(a => a.PeriodStartDate).Min() ==
                                                                   Encounter.EncounterCycleStartDate)
                   ) // verify period spans soc.
                {
                    string[] memberNames = { "EncounterCycleStartDate" };
                    var Msg =
                        "Period From Date must be on or before the Start of Care Date when 'Transfer From Another Hospice' is selected.";
                    Encounter.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AnyErrors = true;
                }
            }
            else
            {
                // somebody emptied out the fields
                if (Encounter.EncounterCyclePdNum <= 0)
                {
                    string[] memberNames = { "EncounterCyclePdNum" };
                    var Msg = "Period Number is required.";
                    Encounter.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AnyErrors = true;
                }

                if (!Encounter.EncounterCycleStartDate.HasValue)
                {
                    string[] memberNames = { "EncounterCycleStartDate" };
                    var Msg = "Period Start Date is required.";
                    Encounter.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AnyErrors = true;
                }

                if (!Encounter.EncounterCycleEndDate.HasValue)
                {
                    string[] memberNames = { "EncounterCycleEndDate" };
                    var Msg = "Period End Date is required.";
                    Encounter.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AnyErrors = true;
                }
            }

            return AnyErrors;
        }

        private DateTime CalculateDisciplineAdmitDateTime
        {
            get
            {
                if ((Encounter != null) && ((Encounter.EncounterStartDate != null)))
                {
                    return Encounter.EncounterStartDate.Value.Date;
                }

                return DateTime.Now.Date;
            }
        }

        private DateTime CalculateDisciplineNotAdmitDateTime => CalculateDisciplineAdmitDateTime;

        private bool ValidationErrorsContainsMessage(string msg)
        {
            return Admission.ValidationErrorsContainsMessage(msg);
        }

        private bool ValidateServicePriority()
        {
            bool AnyErrors = false;
            ClearErrorFromProperty("ServicePriorityOne");
            ClearErrorFromProperty("ServicePriorityTwo");

            if (Encounter.FullValidation && Admission.AdmissionStatusCode != "N")
            {
                var currentEncounterAdmission = AdmissionOrEncounterAdmission as IEncounterAdmission;
                if ((currentEncounterAdmission != null) && (currentEncounterAdmission.ServicePriorityOne == null))
                {
                    AddErrorForProperty("ServicePriorityOne",
                        String.Format("{0} is required.", ServicePriorityOneLabel));
                    AnyErrors = true;
                }
            }

            return AnyErrors;
        }

        // AdmissionStatus also supports resumption
        private EncounterResumption _EncounterResumption;

        public EncounterResumption EncounterResumption
        {
            get { return _EncounterResumption; }
            set
            {
                _EncounterResumption = value;
                this.RaisePropertyChangedLambda(p => p.EncounterResumption);
                this.RaisePropertyChangedLambda(p => p.IsResumption);
            }
        }

        public EncounterTransfer AssociatedEncounterTransfer
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                if (Admission.EncounterTransfer == null)
                {
                    return null;
                }

                if (EncounterResumption == null)
                {
                    return null;
                }

                return Admission.EncounterTransfer
                    .Where(e => e.EncounterTransferKey == EncounterResumption.AssociatedEncounterTransferKey)
                    .FirstOrDefault();
            }
        }

        public PatientFacilityStay AssociatedPatientFacilityStay
        {
            get
            {
                if (Patient == null)
                {
                    return null;
                }

                if (Patient.PatientFacilityStay == null)
                {
                    return null;
                }

                if (AssociatedEncounterTransfer == null)
                {
                    return null;
                }

                return Patient.PatientFacilityStay
                    .Where(e => e.PatientFacilityStayKey == AssociatedEncounterTransfer.PatientFacilityStayKey)
                    .FirstOrDefault();
            }
        }

        public bool ShowAssociatedEncounterTransfer => (AssociatedPatientFacilityStay != null);
        public bool IsResumption => (EncounterResumption == null) ? false : true;

        public void SetupEvalOrResumption()
        {
            if ((Question == null) || (Question.DataTemplate == null))
            {
                return;
            }

            if (Question.DataTemplate.ToLower().Equals("admissionstatus") == false)
            {
                return;
            }

            if ((Admission == null) || ((AdmissionDiscipline == null) || (Encounter == null) ||
                                        Encounter.EncounterResumption == null))
            {
                return;
            }

            EncounterResumption = Encounter.EncounterResumption.Where(x => x.EncounterKey == Encounter.EncounterKey)
                .FirstOrDefault();
            if (EncounterResumption != null)
            {
                AdmissionDiscipline.EncounterResumption = EncounterResumption;
                return;
            }

            if (IsResumptionDue == false)
            {
                return;
            }

            EncounterResumption = new EncounterResumption
                { ResumptionReferralDate = null, AdmissionKey = Admission.AdmissionKey };
            EncounterTransfer transfer = Admission.MostRecentTransfer;
            EncounterResumption.AssociatedEncounterTransferKey = transfer?.EncounterTransferKey;
            AdmissionDiscipline.EncounterResumption = EncounterResumption;
        }

        private bool IsResumptionDue
        {
            get
            {
                if (Question.DataTemplate.Equals("AdmissionStatus") == false)
                {
                    return false;
                }

                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null))
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm.IsResumption)
                {
                    return true;
                }

                if (DynamicFormViewModel.CurrentForm.IsEval == false)
                {
                    return false;
                }

                // Form IsEval - ResumptionDue if we have an outstanding transfer without a corresponding resumption 
                // and this is a new encounter - i.e., once it started - we don't change from an eval to a resumption
                if (Encounter == null)
                {
                    return false;
                }

                if (Encounter.IsNew == false)
                {
                    return false;
                }

                EncounterTransfer transfer = Admission.MostRecentTransfer;
                if (transfer == null)
                {
                    return false;
                }

                EncounterResumption resump = Admission.MostRecentResumption;
                if (resump == null)
                {
                    return true;
                }

                DateTime rDate = (resump.ResumptionDate == null)
                    ? DateTime.Today.Date
                    : ((DateTime)resump.ResumptionDate).Date;
                if (transfer.TransferDate.Date > rDate)
                {
                    return true;
                }

                return false;
            }
        }

        private AdmissionConsent _AdmissionConsentSelectedItem;

        public AdmissionConsent AdmissionConsentSelectedItem
        {
            get { return _AdmissionConsentSelectedItem; }
            set
            {
                _AdmissionConsentSelectedItem = value;
                RaisePropertyChanged("AdmissionConsentSelectedItem");
            }
        }

        public void SetupConsents()
        {
            _AdmissionConsents.Source = Admission.AdmissionConsent;
            _AdmissionConsents.Filter += (s, e) =>
            {
                e.Accepted = true;

                AdmissionConsent ac = e.Item as AdmissionConsent;
                if (ac == null || Encounter == null)
                {
                    e.Accepted = false;
                }
                else if (!ac.IsNew && (ac.EncounterConsent == null ||
                                       !Encounter.EncounterConsent.Any(enc =>
                                           enc.AdmissionConsentKey == ac.AdmissionConsentKey)))
                {
                    e.Accepted = false;
                }
            };
            AdmissionConsentAddItem_Command = new RelayCommand(() =>
            {
                AdmissionConsent instance = Activator.CreateInstance<AdmissionConsent>();
                AdmissionConsentSelectedItem = instance;
                if (Admission != null && Admission.AdmissionConsent != null)
                {
                    Admission.AdmissionConsent.Add(instance);
                }

                Messenger.Default.Register<AdmissionConsent>(instance, "RefreshDecisionTypes",
                    g => RefreshDecisionTypesList());
                instance.BeginEditting();
                AdmissionConsentSelectedItem = instance;
                SetDecisionTypeFilters();
                PopupDataTemplate = "AdmissionConsentPopupDataTemplate";
                DynamicFormViewModel.PopupDataContext = this;
            });
            AdmissionConsentEditItem_Command = new RelayCommand<AdmissionConsent>(item =>
            {
                AdmissionConsentSelectedItem = item;
                if (AdmissionConsentSelectedItem == null)
                {
                    return;
                }


                PopupDataTemplate = "AdmissionConsentPopupDataTemplate";
                if (DynamicFormViewModel != null)
                {
                    DynamicFormViewModel.PopupDataContext = this;
                }
            });
            AdmissionConsentCancel_Command = new RelayCommand<AdmissionConsent>(item =>
            {
                AdmissionConsent ac = item;
                if (ac != null)
                {
                    ac.ClientValidate();
                }

                if (item.IsOKed == false || item.HasValidationErrors)
                {
                    // Canel while adding new item - remove it
                    if (item.IsNew && Admission != null && Admission.AdmissionConsent != null)
                    {
                        var itemtoremove = item;
                        Messenger.Default.Unregister(itemtoremove);
                        Admission.AdmissionConsent.Remove(itemtoremove);
                        if (DynamicFormViewModel.FormModel != null)
                        {
                            DynamicFormViewModel.FormModel.RemoveAdmissionConsent(itemtoremove);
                        }
                    }
                    else
                    {
                        item.CancelEditting();
                    }
                }

                PopupDataTemplate = "";
                DynamicFormViewModel.PopupDataContext = null;

                if (Admission != null)
                {
                    Admission.RefreshRaiseChanged();
                }
            });
            AdmissionConsentOK_Command = new RelayCommand<AdmissionConsent>(item =>
            {
                if (item == null)
                {
                    return;
                }

                if (item.Validate())
                {
                    if (item.Validate())
                    {
                        if (item.ClientValidate())
                        {
                            item.IsOKed = true;
                            PopupDataTemplate = "";
                            DynamicFormViewModel.PopupDataContext = null;
                        }
                    }
                }

                if (Admission != null)
                {
                    Admission.RefreshRaiseChanged();
                }

                FilteredAdmissionConsents.Refresh();
            });
        }

        public bool ConsentGridVisible
        {
            get
            {
                if (AdmissionOrEncounterAdmission == null)
                {
                    return true;
                }

                var ea = AdmissionOrEncounterAdmission as EncounterAdmission;
                // Encounter is not complete, so show the grid.
                if (ea == null)
                {
                    return true;
                }

                // if Release of Info is true, but we don't have any Encounter consent rows, the check box must have been 
                // selected prior to the Consent Grid implementation.
                if (ea.ReleaseOfInformation &&
                    (Encounter.EncounterConsent == null || Encounter.EncounterConsent.Any() == false))
                {
                    return false;
                }

                return true;
            }
        }

        #region AdmissionReferral

        public RelayCommand<AdmissionReferral> AdmissionReferralViewReferralNotes_Command { get; protected set; }

        private string _PopupDataTemplate;

        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                _PopupDataTemplate = value;
                RaisePropertyChanged("PopupDataTemplate");
                RaisePropertyChanged("PopupDataTemplateLoaded");
            }
        }

        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject PopupDataTemplateLoaded
        {
            get
            {
                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }

                return DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }

        public void AdmissionReferralSetup()
        {
            AdmissionReferralCollectionViewSourceSetup();
            AdmissionReferralCommandSetup();
        }

        private void AdmissionReferralCommandSetup()
        {
            AdmissionCareCoordinatorHistory_Command = new RelayCommand(() =>
            {
                AdmissionCareCoordinatorHistoryCommand();
            });
            AdmissionReferralViewReferralNotes_Command = new RelayCommand<AdmissionReferral>(ar =>
            {
                AdmissionReferralViewReferralNotesCommand(ar);
            });
        }

        private void AdmissionCareCoordinatorHistoryCommand()
        {
            IPatientService Model = DynamicFormViewModel?.FormModel as IPatientService;
            if ((Model == null) || (CurrentAdmission == null))
            {
                return;
            }

            AdmissionCareCoordinatorHistoryPopupViewModel viewModel =
                new AdmissionCareCoordinatorHistoryPopupViewModel(Model, CurrentAdmission);
            DialogService ds = new DialogService();
            ds.ShowDialog(viewModel, ret =>
            {
                viewModel?.Cleanup();
                viewModel = null;
            });
        }

        private void AdmissionReferralViewReferralNotesCommand(AdmissionReferral ar)
        {
            AdmissionReferralSelectedItem = ar;
            if (AdmissionReferralSelectedItem == null)
            {
                return;
            }

            if (AdmissionReferralSelectedItem.HasReferralNotes == false)
            {
                return;
            }

            AdmissionReferralNotesPopupViewModel viewModel =
                new AdmissionReferralNotesPopupViewModel(AdmissionReferralSelectedItem);
            DialogService ds = new DialogService();
            ds.ShowDialog(viewModel, ret =>
            {
                viewModel?.Cleanup();
                viewModel = null;
            });
        }

        private string _AdmissionReferralValidateError;

        public string AdmissionReferralValidateError
        {
            get { return _AdmissionReferralValidateError; }
            set
            {
                _AdmissionReferralValidateError = value;
                RaisePropertyChanged("AdmissionReferralValidateError");
            }
        }

        private void AdmissionReferralPreValidate()
        {
            AdmissionReferralValidateError = null;
        }

        private bool AdmissionReferralValidate()
        {
            if (MustAddAdmissionReferral == false)
            {
                return true;
            }

            AdmissionReferralValidateError = "A Referral for Admission must be defined";
            return false;
        }

        private DateTime? MostRecentAdmissionReferralReferralDate
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                if (Admission.AdmissionReferral == null)
                {
                    return null;
                }

                AdmissionReferral first = Admission.AdmissionReferral.Where(referral => referral.HistoryKey == null)
                    .OrderByDescending(ar => ar.ReferralDate).FirstOrDefault();
                return ((first != null) && (first.ReferralDate != null))
                    ? ((DateTime)first.ReferralDate).Date
                    : (DateTime?)null;
            }
        }

        private void AdmissionReferralRefresh()
        {
            AdmissionReferralCollectionViewSourceSetup();
            RaisePropertyChanged("CanAddAdmissionReferral");
        }

        private void AdmissionReferralCollectionViewSourceSetup()
        {
            _AdmissionReferralsView = new CollectionViewSource();
            if ((Admission == null) || (Admission.AdmissionReferral == null))
            {
                return;
            }

            if (Admission != null)
            {
                Admission.AdmissionReferralSetMostRecent();
            }

            _AdmissionReferralsView.SortDescriptions.Add(new SortDescription("ReferralDate",
                ListSortDirection.Descending));
            _AdmissionReferralsView.Source = Admission.AdmissionReferral;
            _AdmissionReferralsView.View.MoveCurrentToFirst();

            _AdmissionReferralsView.Filter += (s, args) =>
            {
                AdmissionReferral ar = args.Item as AdmissionReferral;
                ar.CurrentEncounter = Encounter;
                args.Accepted = (ar.HistoryKey != null) ? false : true;
            };

            RaisePropertyChanged("AdmissionReferralsView");
        }

        private CollectionViewSource _AdmissionReferralsView;
        public ICollectionView AdmissionReferralsView => _AdmissionReferralsView.View;
        private AdmissionReferral _AdmissionReferralSelectedItem;

        public AdmissionReferral AdmissionReferralSelectedItem
        {
            get { return _AdmissionReferralSelectedItem; }
            set
            {
                _AdmissionReferralSelectedItem = value;
                RaisePropertyChanged("AdmissionReferralSelectedItem");
            }
        }

        public bool NotTaken
        {
            get
            {
                EncounterAdmission ed = AdmissionDisciplineOrEncounterAdmission as EncounterAdmission;
                if (ed != null)
                {
                    return (bool)ed.NotTaken;
                }

                AdmissionDiscipline ad = AdmissionDisciplineOrEncounterAdmission as AdmissionDiscipline;
                if (ad != null)
                {
                    return ad.NotTaken;
                }

                return false;
            }
            set
            {
                AdmissionDiscipline ad = AdmissionDisciplineOrEncounterAdmission as AdmissionDiscipline;
                if (ad != null)
                {
                    ad.NotTaken = value;
                }

                EncounterAdmission ed = AdmissionDisciplineOrEncounterAdmission as EncounterAdmission;
                if (ed != null)
                {
                    ed.NotTaken = value;
                }

                if ((Protected == false) && (value) && (OasisManager != null))
                {
                    OasisManager.ForceBypassNTUC();
                }

                if ((Protected == false) && (value) && (OrderEntryManager != null))
                {
                    OrderEntryManager.ForceDiscardNotTaken();
                }

                RaisePropertyChanged("NotTaken");
            }
        }

        public bool Admitted
        {
            get
            {
                EncounterAdmission ed = AdmissionDisciplineOrEncounterAdmission as EncounterAdmission;
                if (ed != null)
                {
                    return (bool)ed.Admitted;
                }

                AdmissionDiscipline ad = AdmissionDisciplineOrEncounterAdmission as AdmissionDiscipline;
                if (ad != null)
                {
                    return ad.Admitted;
                }

                return false;
            }
            set
            {
                AdmissionDiscipline ad = AdmissionDisciplineOrEncounterAdmission as AdmissionDiscipline;
                if (ad != null)
                {
                    ad.Admitted = value;
                }

                EncounterAdmission ed = AdmissionDisciplineOrEncounterAdmission as EncounterAdmission;
                if (ed != null)
                {
                    ed.Admitted = value;
                }

                if ((Protected == false) && (value) && (OasisManager != null))
                {
                    OasisManager.ForceUnBypassNTUC();
                }

                if ((Protected == false) && (value) && (OrderEntryManager != null))
                {
                    OrderEntryManager.ForceUnDiscardNotTaken();
                }

                RaisePropertyChanged("Admitted");
            }
        }

        private bool MustAddAdmissionReferral => false;

        public bool CanAddAdmissionReferral => false;

        #endregion

        #region "AdmissionConsent Decision Types"

        private CollectionViewSource _decisionTypesList = new CollectionViewSource();

        private CollectionViewSource DecisionTypesList
        {
            get
            {
                if (_decisionTypesList.Source == null)
                {
                    var temp = CodeLookupCache.GetCodeLookupsFromType("RHIODecide");

                    if (temp != null)
                    {
                        _decisionTypesList.Source = temp.ToList();
                    }
                }

                return _decisionTypesList;
            }
        }

        public ICollectionView FilteredDecisionTypes
        {
            get
            {
                if (DecisionTypesList.View != null)
                {
                    DecisionTypesList.View.Refresh();
                }

                return DecisionTypesList.View;
            }
        }

        public void SetDecisionTypeFilters()
        {
            if (DecisionTypesList != null)
            {
                DecisionTypesList.Filter -= DecisionTypeFilter;
                DecisionTypesList.Filter += DecisionTypeFilter;
                RefreshDecisionTypesList();
            }
        }

        private void DecisionTypeFilter(object sender, FilterEventArgs e)
        {
            CodeLookup cl = e.Item as CodeLookup;

            if (AdmissionConsentSelectedItem.Requestor == "CAHPS Vendor")
            {
                e.Accepted = cl.CodeDescription != "Access Denied" && cl.CodeDescription != "Access Approved";
            }
            else
            {
                e.Accepted = true;
            }
        }

        public void RefreshDecisionTypesList()
        {
            if (FilteredDecisionTypes != null)
            {
                FilteredDecisionTypes.Refresh();
            }
        }

        #endregion
    }

    public class AdmissionStatusFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            AdmissionStatus admstat =
                new AdmissionStatus(vm.CurrentAdmission, vm.CurrentEncounter, __FormSectionQuestionKey)
                {
                    Section = formsection.Section,
                    QuestionGroupKey = qgkey,
                    Question = q,
                    Patient = vm.CurrentPatient,
                    Admission = vm.CurrentAdmission,
                    AdmissionDiscipline = vm.CurrentAdmissionDiscipline,
                    DynamicFormViewModel = vm,
                    OasisManager = vm.CurrentOasisManager,
                    OrderEntryManager = vm.CurrentOrderEntryManager,
                };
            admstat.SetupEvalOrResumption();

            admstat.GroupDropDownClosed = new RelayCommand(() =>
            {
                // tell all the drop downs to reselect the proper value in case the user did something crazy
                // like select a row that isn't in effect as of the service date.
                admstat.RefreshCurrentGroups();
            });
            admstat.RemoveRowCommand = new RelayCommand<object>(
                item =>
                {
                    AdmissionFaceToFaceDiagnosis RowToRemove = item as AdmissionFaceToFaceDiagnosis;
                    if (RowToRemove != null)
                    {
                        admstat.Admission.AdmissionFaceToFaceDiagnosis.Remove(RowToRemove);
                        admstat.NotifyICDListChanged();
                    }
                });
            admstat.AddF2FRowCommand = new RelayCommand(
                () =>
                {
                    AdmissionFaceToFaceDiagnosis RowToAdd = new AdmissionFaceToFaceDiagnosis();
                    RowToAdd.AdmissionKey = admstat.Admission.AdmissionKey;
                    RowToAdd.PatientKey = admstat.Admission.PatientKey;
                    RowToAdd.Sequence = admstat.PhysicianF2FICDList == null
                        ? 0
                        : admstat.PhysicianF2FICDList.Cast<object>().Count();
                    RowToAdd.Superceded = false;
                    RowToAdd.AddedFromEncounterKey = admstat.Encounter.EncounterKey;
                    admstat.Admission.AdmissionFaceToFaceDiagnosis.Add(RowToAdd);
                    admstat._physicianF2FICDList.Source = admstat.Admission.AdmissionFaceToFaceDiagnosis;
                    admstat.NotifyICDListChanged();
                });

            admstat._physicianF2FICDList.Source = admstat.Admission.AdmissionFaceToFaceDiagnosis;

            admstat.AdmissionReferralSetup();

            admstat.SetupConsents();

            return admstat;
        }
    }
}