using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
using System.Windows;
#endif
namespace Virtuoso.Validation
{
    public static class EncounterValidations
    {
        public static ValidationResult EncounterBPValidate(EncounterBP CurrentBP, ValidationContext validationContext)
        {
            if (CurrentBP.BPSystolic == 0 || CurrentBP.BPDiastolic == 0 || string.IsNullOrEmpty(CurrentBP.BPSide))
            {
                string[] memberNames = new string[] { "BPSystolic", "BPDiastolic" };
                return new ValidationResult("You must specify a full blood pressure reading including BP and side", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult EncounterCBGValidate(EncounterCBG CurrentCBG, ValidationContext validationContext)
        {
            if (CurrentCBG.CBG == 0)
            {
                string[] memberNames = new string[] { "CBG" };
                return new ValidationResult("You must specify a full blood glucose reading", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult EncounterPTINRValidate(EncounterPTINR CurrentPTINR, ValidationContext validationContext)
        {
            if (CurrentPTINR.PTSeconds == 0.0 || CurrentPTINR.INRRatio == 0.0)
            {
                string[] memberNames = new string[] { "PTSeconds", "INRRatio" };
                return new ValidationResult("You must specify a full PT/INR reading including PT and INR", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult EncounterPulseValidate(EncounterPulse CurrentPulse, ValidationContext validationContext)
        {
            if (CurrentPulse.PulseRhythm < 1 || CurrentPulse.PulseMode < 1)
            {
                string[] memberNames = new string[] { "PulseRate" };
                return new ValidationResult("You must specify full pulse reading including rate, rhythm and mode", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult EncounterRespValidate(EncounterResp CurrentResp, ValidationContext validationContext)
        {
            if (CurrentResp.RespRhythm < 1)
            {
                string[] memberNames = new string[] { "RespRate" };
                return new ValidationResult("You must specify a full respiration reading including rate and rhythm", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult EncounterTempValidate(EncounterTemp CurrentTemp, ValidationContext validationContext)
        {
            if (CurrentTemp.TempScale.Equals("F") && (CurrentTemp.Temp < 70 || CurrentTemp.Temp > 120))
            {
                string[] memberNames = new string[] { "Temp" };
                return new ValidationResult("Fahrenheit temperature must be between 60 and 120", memberNames);
            }

            if (CurrentTemp.TempScale.Equals("C") && (CurrentTemp.Temp < 21 || CurrentTemp.Temp > 50))
            {
                string[] memberNames = new string[] { "Temp" };
                return new ValidationResult("Celsius temperature must be between 21 and 50", memberNames);
            }

            if (string.IsNullOrEmpty(CurrentTemp.TempScale) || CurrentTemp.TempMode < 1)
            {
                string[] memberNames = new string[] { "Temp" };
                return new ValidationResult("You must specify a full temperature reading including temperature, scale and mode", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult EncounterVisitFrequencyValidate(EncounterVisitFrequency CurrentVisitFrequency, ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(CurrentVisitFrequency.Frequency)) CurrentVisitFrequency.Frequency = null;
            if (string.IsNullOrWhiteSpace(CurrentVisitFrequency.Duration)) CurrentVisitFrequency.Duration = null;
            if (string.IsNullOrWhiteSpace(CurrentVisitFrequency.Purpose)) CurrentVisitFrequency.Purpose = null;
            if ((CurrentVisitFrequency.Frequency == null) || (CurrentVisitFrequency.Duration == null) || (CurrentVisitFrequency.Purpose == null))
            {
                string[] memberNames = new string[] { "Frequency" };
                return new ValidationResult("You must specify a full visit frequency including frequency, duration and purpose", memberNames);
            }
            return ValidationResult.Success;
        }
        private static bool IsDateEmpty(DateTime? dt)
        {
            if (dt == null) { return true; }
            return (dt == DateTime.MinValue) ? true : false;
        }
        public static ValidationResult EncounterPlanOfCareValidateCertificationDates(EncounterPlanOfCare CurrentItem, ValidationContext validationContext)
        {
            if (IsDateEmpty(CurrentItem.CertificationFromDate))
            {
                string[] memberNames = new string[] { "CertificationFromDate" };
                return new ValidationResult("Certification from date must be entered", memberNames);
            }
            if (IsDateEmpty(CurrentItem.CertificationThruDate))
            {
                string[] memberNames = new string[] { "CertificationThruDate" };
                return new ValidationResult("Certification thru date must be entered", memberNames);
            }
            if (DateTime.Compare((DateTime)CurrentItem.CertificationFromDate, (DateTime)CurrentItem.CertificationThruDate) > 0)
            {
                string[] memberNames = new string[] { "CertificationThruDate" };
                return new ValidationResult("The Certification thru date must be on or after the Certification from date", memberNames);
            }
            return ValidationResult.Success;
        }
        public static ValidationResult ValidateEncounterSupply(EncounterSupply encounterSupply, ValidationContext validationContext)
        {
            decimal? Charge = encounterSupply.OverrideChg == null ? encounterSupply.SupplyCharge : encounterSupply.OverrideChg;
            decimal? Allow = encounterSupply.OverrideAllow == null ? encounterSupply.SupplyAllow : encounterSupply.OverrideAllow;
            if (Charge < Allow)
            {
                return new ValidationResult("Allowance cannot be greater than the charge", new string[] { "Charge", "Allowance", "SupplyCharge", "SupplyAllow", "OverrideChg", "OverrideAllow" });
            }
            return ValidationResult.Success;
        }
    }
}
