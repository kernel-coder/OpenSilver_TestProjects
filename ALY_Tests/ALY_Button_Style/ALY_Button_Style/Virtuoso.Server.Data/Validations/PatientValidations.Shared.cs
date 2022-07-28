using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public static class PatientValidations
    {
        private static bool IsDateEmpty(DateTime? dt)
        {
            if (dt == null) { return true; }
            return (dt == DateTime.MinValue) ? true : false;
        }

        public static ValidationResult IsSSNValid(string SSN, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { validationContext.MemberName };
            // A NULL SSN is a valid SSN
            if (string.IsNullOrEmpty(SSN)) return ValidationResult.Success;
            //SSN must be a 10 digit number.  
            if ((SSN.Length != 9) || (!IsNumeric(SSN)))
                return new ValidationResult("SSN must be a nine digit number.", memberNames);

            return ValidationResult.Success;
        }

        private static bool IsNumeric(string s)
        {
            try { Int64.Parse(s); }
            catch { return false; }
            return true;
        }

        public static ValidationResult IsDeathDateValid(Patient patient, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "BirthDate", "DeathDate" };
            if ((patient.BirthDate.HasValue) && (patient.DeathDate.HasValue))
            {
                if ((!patient.BirthDate.Equals(DateTime.MinValue) && (!patient.DeathDate.Equals(DateTime.MinValue))))
                {
                    if (DateTime.Compare((DateTime)patient.BirthDate, (DateTime)patient.DeathDate) > 0)
                        return new ValidationResult("The Date of death date must be on or after the birth date.", memberNames);
                }
            }
            if ((patient.ValidateState_DeathDateRequired) && (patient.DeathDate.HasValue == false))
                return new ValidationResult("The Date of Death field is required.", memberNames);
            return ValidationResult.Success;
        }

        public static ValidationResult IsBirthDateValid(Patient patient, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "BirthDate" };
            if (patient.BirthDate.HasValue)
            {
                if (patient.BirthDate.Value.Date > DateTime.Today.Date)
                {
                    return new ValidationResult("Birth Date cannot be in the future.", memberNames);
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsDeathDateInTheFuture(Patient patient, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "DeathDate" };
            if (patient.DeathDate.HasValue)
            {
                if (patient.DeathDate.Value.Date > DateTime.Today.Date)
                {
                    return new ValidationResult("Death Date cannot be in the future.", memberNames);
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult PatientContactValidateFacilityKey(int? facilityKey, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                PatientContact pc = validationContext.ObjectInstance as PatientContact;
                if (pc == null) return ValidationResult.Success;

                var _typeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(pc.ContactTypeKey);
                _typeCode = (_typeCode == null) ? String.Empty : _typeCode;

                int key = (facilityKey == null) ? 0 : (int)facilityKey;
                if ((_typeCode.ToLower() == "facility") && (key <= 0))
                {
                    string[] memberNames = new string[] { "FacilityKey" };
                    return new ValidationResult("The Facility field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "FacilityKey" };
                return new ValidationResult("Data provider(s) is NULL", memberNames);
            }
        }
        public static ValidationResult IsPatientContactValid(PatientContact pc, ValidationContext validationContext)
        {
            // If any part of the address is entered - Address1, City, StateCode and ZipCode are required
            if ((!(string.IsNullOrEmpty(pc.Address1))) ||
                (!(string.IsNullOrEmpty(pc.Address2))) ||
                (!(string.IsNullOrEmpty(pc.City))) ||
                (!(pc.StateCode == null)) ||
                (!(string.IsNullOrEmpty(pc.ZipCode))))
            {
                if ((string.IsNullOrEmpty(pc.Address1)) ||
                    (string.IsNullOrEmpty(pc.City)) ||
                    (pc.StateCode == null) ||
                    (string.IsNullOrEmpty(pc.ZipCode)))
                {
                    string[] memberNames = new string[] { "Address1", "City", "StateCode", "ZipCode" };
                    return new ValidationResult("An address must contain Addesss, City, State and ZipCode.", memberNames);
                }
            }
            // You cannot have a work phone extension without a work phone
            if ((!(string.IsNullOrEmpty(pc.WorkPhoneExtension))) && (string.IsNullOrEmpty(pc.WorkPhoneNumber)))
            {
                string[] memberNames = new string[] { "WorkPhoneExtension", "WorkPhoneNumber" };
                return new ValidationResult("You cannot have a work phone extension without a work phone number.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult PatientInsuranceValidateInsuranceKey(PatientInsurance CurrentItem, ValidationContext validationContext)
        {
            if (CurrentItem.HistoryKey.HasValue) return ValidationResult.Success;

            if ((CurrentItem.InsuranceKey == null) || (CurrentItem.InsuranceKey <= 0))
            {
                string[] memberNames = new string[] { "InsuranceKey" };
                return new ValidationResult("Insurance name must be entered", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult PatientInsuranceValidateInsuredName(PatientInsurance CurrentItem, ValidationContext validationContext)
        {
            if (CurrentItem.HistoryKey.HasValue) return ValidationResult.Success;

            if (CurrentItem.InsuredFirstName != null) CurrentItem.InsuredFirstName = CurrentItem.InsuredFirstName.Trim();
            if (CurrentItem.InsuredLastName != null) CurrentItem.InsuredLastName = CurrentItem.InsuredLastName.Trim();
            if ((string.IsNullOrEmpty(CurrentItem.InsuredFirstName)) && (!string.IsNullOrEmpty(CurrentItem.InsuredLastName)))
            {
                if (!CurrentItem.InsuredLastName.ToUpper().Trim().Equals("SELF"))
                {
                    string[] memberNames = new string[] { "InsuredLastName" };
                    return new ValidationResult("Insured name must include a first and last name", memberNames);
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult PatientInsuranceValidateInsuranceVerifiedDate(DateTime? CurrentInsuranceVerifiedDate, ValidationContext validationContext)
        {
            PatientInsurance pi = validationContext.ObjectInstance as PatientInsurance;

            if (pi == null) return ValidationResult.Success;
            if (pi.HistoryKey.HasValue) return ValidationResult.Success;

            if (!pi.InsuranceVerified)
            {
                if ((IsDateEmpty(CurrentInsuranceVerifiedDate) == false) && (IsDateEmpty(pi.InsuranceVerifiedDate) == false)) pi.InsuranceVerifiedDate = null;
                return ValidationResult.Success;
            }
            if ((pi.InsuranceVerified) && (IsDateEmpty(CurrentInsuranceVerifiedDate)))
            {
                string[] memberNames = new string[] { "InsuranceVerifiedDate" };
                return new ValidationResult("Insurance verification must include a Verification Date", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult PatientInsuranceValidateInsuranceVerifiedByFirstName(string CurrentInsuranceVerifiedByFirstName, ValidationContext validationContext)
        {
            PatientInsurance pi = validationContext.ObjectInstance as PatientInsurance;
            if (pi == null) return ValidationResult.Success;
            if (pi.HistoryKey.HasValue) return ValidationResult.Success;

            if (!pi.InsuranceVerified)
            {
                if ((CurrentInsuranceVerifiedByFirstName != null) && (pi.InsuranceVerifiedByFirstName != null)) pi.InsuranceVerifiedByFirstName = null;
                return ValidationResult.Success;
            }
            if ((pi.InsuranceVerified) && (string.IsNullOrWhiteSpace(CurrentInsuranceVerifiedByFirstName)))
            {
                string[] memberNames = new string[] { "InsuranceVerifiedByFirstName" };
                return new ValidationResult("Insurance verification must include a Verified by First Name", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult PatientInsuranceValidateInsuranceVerifiedByLastName(string CurrentInsuranceVerifiedByLastName, ValidationContext validationContext)
        {
            PatientInsurance pi = validationContext.ObjectInstance as PatientInsurance;
            if (pi == null) return ValidationResult.Success;
            if (pi.HistoryKey.HasValue) return ValidationResult.Success;

            if (!pi.InsuranceVerified)
            {
                if ((CurrentInsuranceVerifiedByLastName != null) && (pi.InsuranceVerifiedByLastName != null)) pi.InsuranceVerifiedByLastName = null;
                return ValidationResult.Success;
            }
            if ((pi.InsuranceVerified) && (string.IsNullOrWhiteSpace(CurrentInsuranceVerifiedByLastName)))
            {
                string[] memberNames = new string[] { "InsuranceVerifiedByLastName" };
                return new ValidationResult("Insurance verification must include a Verified by Last Name", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult PatientInsuranceValidateSelfPayInsuranceNumber(string curInsuranceNumber, ValidationContext validationContext)
        {
            PatientInsurance pi = validationContext.ObjectInstance as PatientInsurance;
            if (pi == null) return ValidationResult.Success;
            if (string.IsNullOrEmpty(pi.InsuranceNumber) == false) return ValidationResult.Success;

            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            var insuranceDataProvider = validationContext.GetService(typeof(IInsuranceDataProvider)) as IInsuranceDataProvider;

            Insurance myInsurance = null;
            if (insuranceDataProvider != null)
                myInsurance = insuranceDataProvider.GetInsuranceFromInsuranceKey(pi.InsuranceKey);

            string key = null;
            if (myInsurance != null)
            {
                key = codeLookupDataProvider.GetCodeLookupCodeFromKey(myInsurance.InsuranceType);
            }

            if (!string.IsNullOrEmpty(key) && key != "10")
            {
                string[] memberNames = new string[] { "InsuranceNumber" };
                return new ValidationResult("Insurance number is required unless you are self insured.", memberNames); 
            }

            return ValidationResult.Success;
        }
        
        public static ValidationResult PatientInsuranceValidateEffectiveDates(PatientInsurance CurrentItem, ValidationContext validationContext)
        {
            if (CurrentItem.HistoryKey.HasValue) return ValidationResult.Success;

            if (IsDateEmpty(CurrentItem.EffectiveFromDate))
            {
                string[] memberNames = new string[] { "EffectiveFromDate" };
                return new ValidationResult("Effective from date must be entered", memberNames);
            }
            else
            {
                if (!IsDateEmpty(CurrentItem.EffectiveThruDate))
                {
                    if (DateTime.Compare((DateTime)CurrentItem.EffectiveFromDate, (DateTime)CurrentItem.EffectiveThruDate) > 0)
                    {
                        string[] memberNames = new string[] { "EffectiveThruDate" };
                        return new ValidationResult("The effective thru date must be on or after the effective from date", memberNames);
                    }
                }
            }

            if (CurrentItem.Patient != null)
            {
                if (!CurrentItem.Inactive)
                {
                    var admissionDataProvider = validationContext.GetService(typeof(IAdmissionDataProvider)) as IAdmissionDataProvider;
                    IQueryable<PatientInsurance> patInsuranceList = null;
                    if (admissionDataProvider != null)
                    {
                        patInsuranceList = admissionDataProvider.GetPatientInsurancesForPatient(CurrentItem.Patient, CurrentItem.Patient.PatientKey);
                    }

                    if (patInsuranceList != null)
                    {
                        // do not allow the same insurance to be added multiple times to the same patient with overlapping date ranges
                        var overlaps = patInsuranceList.Any(p => !p.Inactive
                                                                 && (p.PatientInsuranceKey != CurrentItem.PatientInsuranceKey)
                                                                 && (!p.HistoryKey.HasValue)
                                                                 && (p.InsuranceKey == CurrentItem.InsuranceKey)
                                                                 // All non null dates
                                                                 && ((CurrentItem.EffectiveFromDate <= p.EffectiveThruDate
                                                                      && CurrentItem.EffectiveThruDate >= p.EffectiveFromDate
                                                                     )
                                                                     // row passed in has null thru date
                                                                     || (CurrentItem.EffectiveThruDate == null
                                                                         && p.EffectiveThruDate >= CurrentItem.EffectiveFromDate)
                                                                     // row passed in has non null
                                                                     || (p.EffectiveThruDate == null
                                                                         && CurrentItem.EffectiveThruDate >= p.EffectiveFromDate)
                                                                     // both have non null thru dates
                                                                     || (p.EffectiveThruDate == null
                                                                         && CurrentItem.EffectiveThruDate == null
                                                                        )
                                                                    )
                                                           );

                        if (overlaps)
                        {
                            string[] memberNames = new string[] { "EffectiveThruDate" };
                            return new ValidationResult("This insurance already exists for this patient and the dates are overlapping.  The dates cannot overlap for the same insurance.", memberNames);
                        }
                    }
                }

                if (CurrentItem.Patient.Admission != null)
                {
                    var admissionList = CurrentItem.Patient.Admission.Where(p => p.HistoryKey == null);
                    if (admissionList.Any() == true)
                    {
                        foreach (Admission admission in admissionList)
                        {
                            if (admission.AdmissionCoverage != null)
                            {
                                var covList = (admission.AdmissionCoverage.Where(ac => (ac.AdmissionCoverageInsurance != null)
                                                                                        && ((ac.StartDate < CurrentItem.EffectiveFromDate)
                                                                                            || ((!ac.EndDate.HasValue
                                                                                                && CurrentItem.EffectiveThruDate.HasValue
                                                                                                )
                                                                                                || ((ac.EndDate.HasValue && CurrentItem.EffectiveThruDate.HasValue)
                                                                                                    && (ac.EndDate > CurrentItem.EffectiveThruDate)
                                                                                                )
                                                                                            )
                                                                                        )
                                                                                    )
                                                );

                                foreach (AdmissionCoverage ac in covList)
                                {
                                    if (ac.AdmissionCoverageInsurance != null)
                                    {
                                        if (ac.AdmissionCoverageInsurance.Where(ins => !ins.Inactive
                                                                                        && (ins.PatientInsuranceKey == CurrentItem.PatientInsuranceKey)
                                                                                ).Any()
                                            )
                                        {
                                            string[] memberNames = new string[] { "EffectiveThruDate" };
                                            return new ValidationResult("Date range does not cover all coverage plans containing this Insurance", memberNames);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsAddressThruDateValid(PatientAddress address, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "EffectiveFromDate" };
            if ((address.EffectiveFromDate.HasValue) && (!address.EffectiveThruDate.Equals(DateTime.MinValue)))
            {
                if ((address.EffectiveThruDate.HasValue) && (!address.EffectiveThruDate.Equals(DateTime.MinValue)))
                {
                    if (DateTime.Compare(address.EffectiveFromDate.Value, address.EffectiveThruDate.Value) > 0)
                        return new ValidationResult("The thru date must be on or after the from date.", memberNames);
                }
            }
            return ValidationResult.Success;
        }
        public static ValidationResult DoesAddressOverlapByType(PatientAddress address, ValidationContext validationContext)
        {
            var admissionDataProvider = validationContext.GetService(typeof(IAdmissionDataProvider)) as IAdmissionDataProvider;
            var codeLookupDataProfider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            IQueryable<PatientAddress> patAddresses = null ;
            string[] memberNames = new string[] { "EffectiveFromDate" };
            if (admissionDataProvider != null)
            {
                patAddresses = admissionDataProvider.GetPatientAddresses(address.Patient, address.PatientKey);
            }
            else
            {   
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }

            if (patAddresses != null)
            {
                if (patAddresses.Any(pa => pa.PatientAddressKey != address.PatientAddressKey && !pa.Inactive && pa.HistoryKey == null && pa.Type == address.Type
                    // All non null dates
                    && ((address.EffectiveFromDate <= pa.EffectiveThruDate && address.EffectiveThruDate >= pa.EffectiveFromDate)
                    // row passed in has null thru date
                    || (address.EffectiveThruDate == null && pa.EffectiveThruDate >= address.EffectiveFromDate)
                    // row passed in has non null
                    || (pa.EffectiveThruDate == null && address.EffectiveThruDate >= pa.EffectiveFromDate)
                    // both have non null thru dates
                    || (pa.EffectiveThruDate == null && address.EffectiveThruDate == null)
                    )))
                {
                    if (codeLookupDataProfider == null)
                    {
                        return new ValidationResult("Only one address of type " + address.Type.ToString() + " can be in affect for a date range.", memberNames);
                    }
                    else
                    {
                        return new ValidationResult("Only one address of type " 
                            + (address.Type != null ? codeLookupDataProfider.GetCodeLookupCodeDescriptionFromKey( (int)address.Type) : "<unknown>")
                            + " can be in affect for a date range.", memberNames);
                    }
                    
                }
            }
            return ValidationResult.Success;
        }
        public static ValidationResult IsPharmacyEndDateValid(PatientPharmacy pharm, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "PharmacyStartDate", "PharmacyEndDate" };            
            if ((pharm.PharmacyEndDate.HasValue) && (!pharm.PharmacyEndDate.Equals(DateTime.MinValue)))
            {
                if (!pharm.PharmacyStartDate.HasValue)
                {
                    return new ValidationResult("The end date cannot be set if a start date is not set.", memberNames);
                }
                else if (DateTime.Compare(pharm.PharmacyStartDate.Value, pharm.PharmacyEndDate.Value) > 0)
                {
                    return new ValidationResult("The end date must be on or after the start date.", memberNames);
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsMedicationStartDateRequired(PatientMedication med, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "MedicationStartDate" };
            // Only require on new meds
            if (!med.MedicationStartDate.HasValue && (med.PatientMedicationKey <= 0) && (med.HistoryKey == null))
            {
                return new ValidationResult("The Start Date field is Required.", memberNames);
            }
            return ValidationResult.Success;
        }

         public static ValidationResult IsDisposedDateValid(PatientMedication patientMedication, ValidationContext validationContext)
        {
            if (((patientMedication.DisposedDatePart == null) && (patientMedication.DisposedTimePart != null)) ||
                ((patientMedication.DisposedDatePart != null) && (patientMedication.DisposedTimePart == null)))
            {
                string[] memberNames = new string[] { "DisposedDatePart", "DisposedTimePart" };
                return new ValidationResult("Both a Disposed Date and Time field are required", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult IsAdministrationDateTimeValid(PatientMedicationAdministration patientMedicationAdministration, ValidationContext validationContext)
        {
            if (((patientMedicationAdministration.AdministrationDatePart == null) || (patientMedicationAdministration.AdministrationTimePart == null)))
            {
                string[] memberNames = new string[] { "AdministrationDatePart", "AdministrationTimePart" };
                return new ValidationResult("Both an Administration Date and Time field are required", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult IsAdministrationSiteValid(int? administrationSite, ValidationContext validationContext)
        {
            PatientMedicationAdministration pma = validationContext.ObjectInstance as PatientMedicationAdministration;
            if (pma.IsAdministrationSiteRequired == null) return ValidationResult.Success;
            if ((pma.IsAdministrationSiteRequired == true) && ((administrationSite == null) || (administrationSite <= 0)))
            {
                string[] memberNames = new string[] { "AdministrationSite" };
                return new ValidationResult("The Administration Site field is required", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult IsManagementDateTimeValid(PatientMedicationManagement patientMedicationManagement, ValidationContext validationContext)
        {
            if (((patientMedicationManagement.ManagementDatePart == null) || (patientMedicationManagement.ManagementTimePart == null)))
            {
                string[] memberNames = new string[] { "ManagementDatePart", "ManagementTimePart" };
                return new ValidationResult("Both a Management Date and Time field are required", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult IsReconcileDateTimeValid(PatientMedicationReconcile patientMedicationReconcile, ValidationContext validationContext)
        {
            if (((patientMedicationReconcile.ReconcileDatePart == null) || (patientMedicationReconcile.ReconcileTimePart == null)))
            {
                string[] memberNames = new string[] { "ReconcileDatePart", "ReconcileTimePart" };
                return new ValidationResult("Both a Reconciliation Date and Time field are required", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult IsTeachingDateTimeValid(PatientMedicationTeaching patientMedicationTeaching, ValidationContext validationContext)
        {
            if (((patientMedicationTeaching.TeachingDatePart == null) || (patientMedicationTeaching.TeachingTimePart == null)))
            {
                string[] memberNames = new string[] { "TeachingDatePart", "TeachingTimePart" };
                return new ValidationResult("Both a Teaching Date and Time field are required", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsAllergyStartDateValid(PatientAllergy allergy, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "AllergyStartDate" };
            if (allergy.AllergyStartDate.HasValue)
            {
                if ( (allergy.AllergyStartDate > DateTime.Today.Date)
                     && (!allergy.Inactive)
                   )
                    return new ValidationResult("The start date cannot be in the future.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsAllergyEndDateValid(PatientAllergy allergy, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "AllergyEndDate" };
            if (((allergy.AllergyEndDate.HasValue) && (!allergy.AllergyEndDate.Equals(DateTime.MinValue)))
                && (!allergy.Inactive)
               )
            {
                if (allergy.AllergyStartDate.HasValue && DateTime.Compare(allergy.AllergyStartDate.Value.Date, allergy.AllergyEndDate.Value.Date) > 0)
                    return new ValidationResult("The end date must be on or after the start date.", memberNames);
                if (allergy.AllergyEndDate.Value.Date > DateTime.Today.Date)
                    return new ValidationResult("The end date cannot be in the future.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsAllergyLastReactionDateValid(PatientAllergy allergy, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "LastReactionDate" };
            if (allergy.LastReactionDate.HasValue && (!allergy.Inactive))
            {
                if (allergy.LastReactionDate.Value.Date > DateTime.Today.Date)
                    return new ValidationResult("The last reaction date cannot be in the future.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsDiagnosisStartDateValid(AdmissionDiagnosis icd, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "DiagnosisStartDate" };
            if ((icd.DiagnosisStartDate.HasValue))
            {
                if (icd.DiagnosisStartDate.Value.Date > DateTime.Today.Date)
                    return new ValidationResult("The start date cannot be in the future.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsLabResultDateValid(PatientLab lab, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "ResultDate" };
            if ((lab.ResultDate.HasValue))
            {
                if (lab.ResultDate.Value.Date > DateTime.Today.Date)
                    return new ValidationResult("The result date cannot be in the future.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsLabOrderDateValid(PatientLab lab, ValidationContext validationContext)
        {
            string[] memberNames = new string[] { "OrderDate" };
            if ((lab.OrderDate.HasValue))
            {
                if (lab.OrderDate.Value.Date > DateTime.Today.Date)
                    return new ValidationResult("The order date cannot be in the future.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult AdmissionWoundSiteValidateWoundDimensions(AdmissionWoundSite CurrentWoundSite, ValidationContext validationContext)
        {
            if (CurrentWoundSite.Length != null) { CurrentWoundSite.Length = CurrentWoundSite.Length.Trim(); }
            if (CurrentWoundSite.Width != null) { CurrentWoundSite.Width = CurrentWoundSite.Width.Trim(); }
            if (CurrentWoundSite.Depth != null) { CurrentWoundSite.Depth = CurrentWoundSite.Depth.Trim(); }
            if ((!string.IsNullOrEmpty(CurrentWoundSite.Length)) ||
                (!string.IsNullOrEmpty(CurrentWoundSite.Width)) ||
                (!string.IsNullOrEmpty(CurrentWoundSite.Depth)))
            {
                if ((string.IsNullOrEmpty(CurrentWoundSite.Length)) ||
                    (string.IsNullOrEmpty(CurrentWoundSite.Width)) ||
                    (string.IsNullOrEmpty(CurrentWoundSite.Depth)))
                {
                    string[] memberNames = new string[] { "Length" };
                    return new ValidationResult("Wound length, width and depth must either all be valued or left blank", memberNames);
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult AdmissionWoundSiteValidateGranulationPercents(AdmissionWoundSite CurrentWoundSite, ValidationContext validationContext)
        {
            int totalPercent =
                ((CurrentWoundSite.WoundPercentGranulation == null) ? 0 : (int)CurrentWoundSite.WoundPercentGranulation) +
                ((CurrentWoundSite.WoundPercentEschar == null) ? 0 : (int)CurrentWoundSite.WoundPercentEschar) +
                ((CurrentWoundSite.WoundPercentSlough == null) ? 0 : (int)CurrentWoundSite.WoundPercentSlough);
            if (totalPercent > 100)
            {
                string[] memberNames = new string[] { "WoundPercentSlough" };
                if (CurrentWoundSite.WoundPercentGranulation != null) memberNames = new string[] { "WoundPercentGranulation" };
                else if (CurrentWoundSite.WoundPercentEschar != null) memberNames = new string[] { "WoundPercentEschar" };
                return new ValidationResult("The total of % Granulation, % Eschar and % Slough cannot exceed 100%", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult AdmissionWoundValidateBurnDegree(int? CurrentBurnDegree, ValidationContext validationContext)
        {
            var woundSiteDataProvider = validationContext.GetService(typeof(IWoundDataProvider)) as IWoundDataProvider;
            if (woundSiteDataProvider != null)
            {
                AdmissionWoundSite w = validationContext.ObjectInstance as AdmissionWoundSite;

                var _IsTypeBurn = woundSiteDataProvider.IsTypeBurn(w);
                var _IsObservable = woundSiteDataProvider.IsObservable(w);

                if (w == null) return ValidationResult.Success;
                if (string.IsNullOrWhiteSpace(w.WoundTypeCode))
                {
                    if ((CurrentBurnDegree != null) && (w.BurnDegree != null)) w.BurnDegree = null;
                    return ValidationResult.Success;
                }
                if ((_IsTypeBurn) && (CurrentBurnDegree == null) && (_IsObservable))
                {
                    string[] memberNames = new string[] { "BurnDegree" };
                    return new ValidationResult("Burn Degree is required for observable burn wounds", memberNames);
                }
                if ((!_IsTypeBurn) && (CurrentBurnDegree != null) && (w.BurnDegree != null)) w.BurnDegree = null;
                return ValidationResult.Success;
            }
            else
            {
                return ValidationResult.Success;
                //string[] memberNames = new string[] { "BurnDegree" };
                //return new ValidationResult("Wound data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionWoundValidateWoundClosure(int? CurrentWoundClosure, ValidationContext validationContext)
        {
            var woundSiteDataProvider = validationContext.GetService(typeof(IWoundDataProvider)) as IWoundDataProvider;
            if (woundSiteDataProvider != null)
            {
                AdmissionWoundSite w = validationContext.ObjectInstance as AdmissionWoundSite;
                if (w == null) return ValidationResult.Success;

                var _IsTypeSurgicalWound = woundSiteDataProvider.IsTypeSurgicalWound(w);
                var _IsObservable = woundSiteDataProvider.IsObservable(w);

                if (string.IsNullOrWhiteSpace(w.WoundTypeCode))
                {
                    if ((CurrentWoundClosure != null) && (w.WoundClosure != null)) w.WoundClosure = null;
                    return ValidationResult.Success;
                }
                if ((_IsTypeSurgicalWound) && (CurrentWoundClosure == null) && (_IsObservable))
                {
                    string[] memberNames = new string[] { "WoundClosure" };
                    return new ValidationResult("Wound Closure is required for observable surgical wounds", memberNames);
                }
                if ((!_IsTypeSurgicalWound) && (CurrentWoundClosure != null) && (w.WoundClosure != null)) w.WoundClosure = null;
                return ValidationResult.Success;
            }
            else
            {
                return ValidationResult.Success; 
                //string[] memberNames = new string[] { "WoundClosure" };
                //return new ValidationResult("Wound data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionWoundValidatePressureUlcerStage(int? CurrentPressureUlcerStage, ValidationContext validationContext)
        {
            var woundSiteDataProvider = validationContext.GetService(typeof(IWoundDataProvider)) as IWoundDataProvider;
            if (woundSiteDataProvider != null)
            {
                AdmissionWoundSite w = validationContext.ObjectInstance as AdmissionWoundSite;
                if (w == null) return ValidationResult.Success;

                var _IsTypePressureUlcer = woundSiteDataProvider.IsTypePressureUlcer(w);

                if (string.IsNullOrWhiteSpace(w.WoundTypeCode))
                {
                    if ((CurrentPressureUlcerStage != null) && (w.PressureUlcerStage != null)) w.PressureUlcerStage = null;
                    return ValidationResult.Success;
                }
                //if ((_IsTypePressureUlcer) && (CurrentPressureUlcerStage == null))
                //{
                //    string[] memberNames = new string[] { validationContext.MemberName };
                //    return new ValidationResult("Pressure Ulcer Stage is required for pressure ulcer wounds", memberNames);
                //}
                if ((!_IsTypePressureUlcer) && (CurrentPressureUlcerStage != null) && (w.PressureUlcerStage != null)) w.PressureUlcerStage = null;
                return ValidationResult.Success;
            }
            else
            {
                return ValidationResult.Success;
                //string[] memberNames = new string[] { "PressureUlcerStage" };
                //return new ValidationResult("Wound data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionWoundValidateDrainageAmountCode(string CurrentDrainageAmountCode, ValidationContext validationContext)
        {
            var woundSiteDataProvider = validationContext.GetService(typeof(IWoundDataProvider)) as IWoundDataProvider;
            if (woundSiteDataProvider != null)
            {
                AdmissionWoundSite w = validationContext.ObjectInstance as AdmissionWoundSite;
                if (w == null) return ValidationResult.Success;

                var _IsObservable = woundSiteDataProvider.IsObservable(w);
                var _IsTypePressureUlcer = woundSiteDataProvider.IsTypePressureUlcer(w);

                if (_IsObservable == false)
                {
                    w.DrainageColor = null;
                    return ValidationResult.Success;
                }
                if (string.IsNullOrWhiteSpace(CurrentDrainageAmountCode))
                {
                    string[] memberNames = new string[] { "DrainageAmountCode" };
                    return new ValidationResult("Drainage Amount is required for observable wounds", memberNames);
                }
                if (_IsTypePressureUlcer && (((CurrentDrainageAmountCode.ToLower().Trim().Equals("none") || CurrentDrainageAmountCode.ToLower().Trim().Equals("light") || CurrentDrainageAmountCode.ToLower().Trim().Equals("moderate") || CurrentDrainageAmountCode.ToLower().Trim().Equals("heavy")) == false)))
                {
                    string[] memberNames = new string[] { "DrainageAmountCode" };
                    return new ValidationResult("Drainage Amount must be None, Light, Moderate or Heavy for Pressure Ulcers ", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                return ValidationResult.Success;
                //string[] memberNames = new string[] { "DrainageAmountCode" };
                //return new ValidationResult("Wound data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionWoundValidateDrainageColor(int? CurrentDrainageColor, ValidationContext validationContext)
        {
            AdmissionWoundSite w = validationContext.ObjectInstance as AdmissionWoundSite;
            if (w == null) return ValidationResult.Success;

            var woundSiteDataProvider = validationContext.GetService(typeof(IWoundDataProvider)) as IWoundDataProvider;
            if (woundSiteDataProvider != null)
            {
                var _IsObservable = woundSiteDataProvider.IsObservable(w);
                if (_IsObservable == false)
                {
                    return ValidationResult.Success;
                }
                if (string.IsNullOrWhiteSpace(w.DrainageAmountCode))
                {
                    return ValidationResult.Success;
                }
                if (w.DrainageAmountCode.ToLower().Trim() == "none")
                {
                    return ValidationResult.Success;
                }
                if (CurrentDrainageColor == null)
                {
                    string[] memberNames = new string[] { "DrainageColor" };
                    return new ValidationResult("Drainage Color is required when a Drainage Amount is specified", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                return ValidationResult.Success;
                //string[] memberNames = new string[] { "DrainageAmountCode" };
                //return new ValidationResult("Wound data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionWoundValidateTreatment(string CurrentTreatemnt, ValidationContext validationContext)
        {
            var woundSiteDataProvider = validationContext.GetService(typeof(IWoundDataProvider)) as IWoundDataProvider;
            if (woundSiteDataProvider != null)
            {
                AdmissionWoundSite w = validationContext.ObjectInstance as AdmissionWoundSite;

                if (w == null) return ValidationResult.Success;
                if ((w == null) || (string.IsNullOrWhiteSpace(CurrentTreatemnt) == false)) return ValidationResult.Success;

                var _IsObservable = woundSiteDataProvider.IsObservable(w);

                if (_IsObservable)
                {
                    string[] memberNames = new string[] { "Treatment" };
                    return new ValidationResult("Treatment is required for observable wounds", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                return ValidationResult.Success;
                //string[] memberNames = new string[] { "Treatment" };
                //return new ValidationResult("Wound data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionWoundValidateWoundStatus(int? CurrentWoundStatus, ValidationContext validationContext)
        {
            var woundSiteDataProvider = validationContext.GetService(typeof(IWoundDataProvider)) as IWoundDataProvider;
            if (woundSiteDataProvider != null)
            {
                AdmissionWoundSite w = validationContext.ObjectInstance as AdmissionWoundSite;
                if (w == null) return ValidationResult.Success;

                var _IsTypeSurgicalWound = woundSiteDataProvider.IsTypeSurgicalWound(w);
                var _IsTypeStasisUlcer = woundSiteDataProvider.IsTypeStasisUlcer(w);
                var _IsTypePressureUlcer = woundSiteDataProvider.IsTypePressureUlcer(w);

                if (string.IsNullOrWhiteSpace(w.WoundTypeCode))
                {
                    //if ((CurrentWoundStatus != null) && (w.WoundStatus != null)) w.WoundStatus = null;
                    return ValidationResult.Success;
                }
                if ((_IsTypeSurgicalWound || _IsTypeStasisUlcer || _IsTypePressureUlcer) && (CurrentWoundStatus == null))
                {
                    string[] memberNames = new string[] { "WoundStatus" };
                    if (_IsTypeSurgicalWound) return new ValidationResult("Status is required for surgical wounds", memberNames);
                    else if (_IsTypeStasisUlcer) return new ValidationResult("Status is required for stasis ulcer wounds", memberNames);
                    else if (_IsTypePressureUlcer) return new ValidationResult("Status is required for pressure ulcer wounds", memberNames);
                }
                //if ((!w.IsTypeSurgicalWound && !w.IsTypeStasisUlcer && !w.IsTypePressureUlcer) && (CurrentWoundStatus != null) && (w.WoundStatus != null)) w.WoundStatus = null;
                return ValidationResult.Success;
            }
            else
            {
                return ValidationResult.Success;
                //string[] memberNames = new string[] { "WoundStatus" };
                //return new ValidationResult("Wound data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionWoundValidateWoundTissueType(int? CurrentWoundTissueType, ValidationContext validationContext)
        {
            var woundSiteDataProvider = validationContext.GetService(typeof(IWoundDataProvider)) as IWoundDataProvider;
            if (woundSiteDataProvider != null)
            {
                AdmissionWoundSite w = validationContext.ObjectInstance as AdmissionWoundSite;

                //Have 2 'tissue type' fields - both will have validation called.  Make sure that we are only
                //validating WoundTissueTypeV1 for Version 1 wounds and WoundTissueTypeV2 for Version 2 wounds
                if (validationContext.MemberName.Equals("WoundTissueTypeV1") && w.Version == 2)
                    return ValidationResult.Success;
                if (validationContext.MemberName.Equals("WoundTissueTypeV2") && w.Version == 1)
                    return ValidationResult.Success;

                if (w == null) return ValidationResult.Success;
                if (string.IsNullOrWhiteSpace(w.WoundTypeCode)) return ValidationResult.Success;

                var _IsTypeSurgicalWound = woundSiteDataProvider.IsTypeSurgicalWound(w);
                var _IsTypeStasisUlcer = woundSiteDataProvider.IsTypeStasisUlcer(w);
                var _IsTypePressureUlcer = woundSiteDataProvider.IsTypePressureUlcer(w);
                var _IsObservable = woundSiteDataProvider.IsObservable(w);

                if (_IsObservable == false)
                {
                    return ValidationResult.Success;
                }

                if (CurrentWoundTissueType == null)
                {
                    string[] memberNames = new string[] { validationContext.MemberName };
                    if (_IsTypePressureUlcer) return new ValidationResult("Tissue Type is required for pressure ulcer wounds", memberNames);
                    else if (_IsTypeSurgicalWound) return new ValidationResult("Tissue Type is required for surgical wounds", memberNames);
                    else if (_IsTypeStasisUlcer) return new ValidationResult("Tissue Type is required for stasis ulcer wounds", memberNames);
                }

                return ValidationResult.Success;
            }
            else
            {
                return ValidationResult.Success;
                //string[] memberNames = new string[] { "WoundTissueType" };
                //return new ValidationResult("Wound data provider is NULL", memberNames);
            }
        }

        public static ValidationResult AdmissionWoundValidateTunnelingDescription(string CurrentTunnelingDescription, ValidationContext validationContext)
        {
            AdmissionWoundSite w = validationContext.ObjectInstance as AdmissionWoundSite;
            if (w == null) return ValidationResult.Success;
            if (w.Tunneling == false)
            {
                if ((CurrentTunnelingDescription != null) && (w.TunnelingDescription != null)) w.TunnelingDescription = null;
                return ValidationResult.Success;
            }
            else if (string.IsNullOrWhiteSpace(CurrentTunnelingDescription))
            {
                string[] memberNames = new string[] { "TunnelingDescription" };
                return new ValidationResult("At least one Tunneling must be specified", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult AdmissionWoundValidateUnderminingDescription(string CurrentUnderminingDescription, ValidationContext validationContext)
        {
            AdmissionWoundSite w = validationContext.ObjectInstance as AdmissionWoundSite;
            if (w == null) return ValidationResult.Success;
            if (w.Undermining == false)
            {
                if ((CurrentUnderminingDescription != null) && (w.UnderminingDescription != null)) w.UnderminingDescription = null;
                return ValidationResult.Success;
            }
            else if (string.IsNullOrWhiteSpace(CurrentUnderminingDescription))
            {
                string[] memberNames = new string[] { "UnderminingDescription" };
                return new ValidationResult("At least one Undermining site must be specified", memberNames);
            }
            if (CurrentUnderminingDescription.Contains("?"))
            {
                string[] memberNames = new string[] { "UnderminingDescription" };
                return new ValidationResult("Each Undermining site must contain a depth", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult AdvancedDirectiveTypeValid(int advancedDirectiveType, ValidationContext validationContext)
        {
            if (advancedDirectiveType == 0)
            {
                string[] memberNames = new string[] { "AdvancedDirectiveType" };
                return new ValidationResult("The Advance Directive Type field is required", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsExpirationDateValid(PatientAdvancedDirective advancedDirective, ValidationContext validationContext)
        {
            if ((advancedDirective.EffectiveDate != null) && (advancedDirective.ExpirationDate != null))
            {
                if (DateTime.Compare(((DateTime)advancedDirective.EffectiveDate).Date, ((DateTime)advancedDirective.ExpirationDate).Date) > 0)
                {
                    string[] memberNames = new string[] { "EffectiveDate", "ExpirationDate" };
                    return new ValidationResult("The Expiration Date must be on or after the Effective Date", memberNames);
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult IsNewReviewedDateValid(PatientAdvancedDirective advancedDirective, ValidationContext validationContext)
        {
            if (((advancedDirective.NewReviewedDatePart == null) && (advancedDirective.NewReviewedTimePart != null)) ||
                ((advancedDirective.NewReviewedDatePart != null) && (advancedDirective.NewReviewedTimePart == null)))
            {
                string[] memberNames = new string[] { "NewReviewedDatePart", "NewReviewedTimePart" };
                return new ValidationResult("Both a Reviewed Date and Time field are required", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult PatientAddressValidateFacilityKey(int? facilityKey, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                PatientAddress pa = validationContext.ObjectInstance as PatientAddress;
                if (pa == null) return ValidationResult.Success;

                var _typeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(pa.Type);
                _typeCode = (_typeCode == null) ? String.Empty : _typeCode;

                int key = (facilityKey == null) ? 0 : (int)facilityKey;
                if ((_typeCode.ToLower() == "facility") && (key == 0))
                {
                    string[] memberNames = new string[] { "FacilityKey" };
                    return new ValidationResult("The Facility field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "FacilityKey" };
                return new ValidationResult("Data provider(s) is NULL", memberNames);
            }
        }
        public static ValidationResult PatientCommunicationValidatePatientContact1Key(PatientAdvancedDirective pad, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            var patientContactDataProvider = validationContext.GetService(typeof(IPatientContactDataProvider)) as IPatientContactDataProvider;
            if ((codeLookupDataProvider != null) && (patientContactDataProvider != null))
            {
                if (pad == null) return ValidationResult.Success;

                var _AdvancedDirectiveTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(pad.AdvancedDirectiveType);
                _AdvancedDirectiveTypeCode = (_AdvancedDirectiveTypeCode == null) ? String.Empty : _AdvancedDirectiveTypeCode;

                var _PatientContact1KeyLabel = patientContactDataProvider.PatientContact1KeyLabel(pad.AdvancedDirectiveType);

                int key = (pad.PatientContact1Key == null) ? 0 : (int)pad.PatientContact1Key;
                if (((_AdvancedDirectiveTypeCode.ToLower() == "healthcareproxy") || (_AdvancedDirectiveTypeCode.ToLower() == "healthcarepoa")) && (key <= 0))
                {
                    string[] memberNames = new string[] { "PatientContact1Key" };
                    return new ValidationResult("The " + _PatientContact1KeyLabel + " field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "PatientContact1Key" };
                return new ValidationResult("Data provider(s) is NULL", memberNames);
            }
        }

        public static ValidationResult ComfortPackMedicationValidateAsNeededFor(string asNeededFor, ValidationContext validationContext)
        {
            ComfortPackMedication cpm = validationContext.ObjectInstance as ComfortPackMedication;
            if (cpm == null) return ValidationResult.Success;
            if ((cpm.AsNeeded == true) && (string.IsNullOrWhiteSpace(asNeededFor)))
            {
                string[] memberNames = new string[] { "AsNeededFor" };
                return new ValidationResult("The As Needed For field is required", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult PatientMedicationValidateAsNeededFor(string asNeededFor, ValidationContext validationContext)
        {
            PatientMedication pam = validationContext.ObjectInstance as PatientMedication;
            if (pam == null) return ValidationResult.Success;
            if ((pam.AsNeeded == true) && (string.IsNullOrWhiteSpace(asNeededFor)))
            {
                string[] memberNames = new string[] { "AsNeededFor" };
                return new ValidationResult("The As Needed For field is required", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult PatientMedicationValidateMedicationFrequencyDescription(string medicationFrequencyDescription, ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(medicationFrequencyDescription) == false)
            {
                if (medicationFrequencyDescription.Contains("?"))
                {
                    string[] memberNames = new string[] { "MedicationFrequencyDescription" };
                    return new ValidationResult("A complete frequency must be entered", memberNames);
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult PatientMedicationValidateInfusionPumpEquipmentItemDesc(string InfusionPumpEquipmentItemDesc, ValidationContext validationContext)
        {
            PatientMedication pam = validationContext.ObjectInstance as PatientMedication;
            if (pam == null) return ValidationResult.Success;
            if ((pam.InfusionPumpUsed == true) && (string.IsNullOrWhiteSpace(InfusionPumpEquipmentItemDesc) == true))
            {
                string[] memberNames = new string[] { "InfusionPumpEquipmentItemDesc" };
                return new ValidationResult("The Pump field is required", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult ArePatientContactsValid(PatientAdvancedDirective advancedDirective, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                PatientAdvancedDirective pad = validationContext.ObjectInstance as PatientAdvancedDirective;
                if (pad == null) return ValidationResult.Success;

                var _AdvancedDirectiveTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(pad.AdvancedDirectiveType);
                _AdvancedDirectiveTypeCode = (_AdvancedDirectiveTypeCode == null) ? String.Empty : _AdvancedDirectiveTypeCode;

                if ((_AdvancedDirectiveTypeCode.ToLower() != "healthcareproxy") && (_AdvancedDirectiveTypeCode.ToLower() != "healthcarepoa")) return ValidationResult.Success;
                string label = (_AdvancedDirectiveTypeCode.ToLower() == "healthcareproxy") ? "Proxies" : "Agents";

                if ((advancedDirective.PatientContact2Key != null) && (advancedDirective.PatientContact2Key == advancedDirective.PatientContact1Key))
                {
                    string[] memberNames = new string[] { "PatientContact2Key" };
                    return new ValidationResult("Duplicate Health Care " + label + " are not allowed.", memberNames);
                }
                else if ((advancedDirective.PatientContact3Key != null) && ((advancedDirective.PatientContact3Key == advancedDirective.PatientContact2Key) || (advancedDirective.PatientContact3Key == advancedDirective.PatientContact1Key)))
                {
                    string[] memberNames = new string[] { "PatientContact3Key" };
                    return new ValidationResult("Duplicate Health Care " + label + " are not allowed.", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { validationContext.MemberName };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }
        }

        public static ValidationResult PatientCommunicationValidateSigningPhysicianKey(PatientAdvancedDirective pad, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;
            if (codeLookupDataProvider != null)
            {
                if (pad == null) return ValidationResult.Success;

                var _AdvancedDirectiveTypeCode = codeLookupDataProvider.GetCodeLookupCodeFromKey(pad.AdvancedDirectiveType);
                _AdvancedDirectiveTypeCode = (_AdvancedDirectiveTypeCode == null) ? String.Empty : _AdvancedDirectiveTypeCode;

                int key = (pad.SigningPhysicianKey == null) ? 0 : (int)pad.SigningPhysicianKey;
                if (((_AdvancedDirectiveTypeCode.ToLower() == "dnr") || (_AdvancedDirectiveTypeCode.ToLower() == "communitydnr")) && (key <= 0))
                {
                    string[] memberNames = new string[] { "SigningPhysicianKey" };
                    return new ValidationResult("The Signing Physician field is required", memberNames);
                }
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "SigningPhysicianKey" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }

        }
        public static ValidationResult ValidatePatientFacilityStayEndDateValid(PatientFacilityStay patientFacilityStay, ValidationContext validationContext)
        {
             if (patientFacilityStay != null)
            {
                if (patientFacilityStay.StartDate.HasValue && !patientFacilityStay.StartDate.Equals(DateTime.MinValue) && patientFacilityStay.EndDate.HasValue && !patientFacilityStay.EndDate.Equals(DateTime.MinValue))
                {
                    string[] memberNames = new string[] { "EndDate" };
                    if (DateTime.Compare(patientFacilityStay.StartDate.Value.Date, patientFacilityStay.EndDate.Value.Date) > 0)
                        return new ValidationResult("The End Date must be on or after the Start Date.", memberNames);
                }
            }
            return ValidationResult.Success;
        }
        public static ValidationResult ValidatePatientFacilityStayStartDateValid(DateTime startDate, ValidationContext validationContext)
        {
            if ((startDate == null) || (startDate.Equals(DateTime.MinValue)))
            {
                string[] memberNames = new string[] { "StartDate" };
                return new ValidationResult("The Start Date field is required.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult PatientAlternateIDValidate(PatientAlternateID PatientAltID, ValidationContext validationContext)
        {
            Patient up = PatientAltID.Patient;
            IQueryable<PatientAlternateID> upp = null;
            if (up != null)
            {
                upp = up.PatientAlternateID.AsQueryable();
            }
            return ValidationResult.Success;
        }
    }
}
