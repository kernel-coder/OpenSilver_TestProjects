#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public class CertManager
    {
        // if the date is not covered by a cert cycle, a new one will be created.
        public static AdmissionCertification CreateCertIfNecessary(Form f, Admission adm, Encounter enc)
        {
            AdmissionCertification ac = null;

            if (CanFormAdvanceCertPeriod(f))
            {
                if ((f != null)
                    && (adm != null)
                    && (enc != null)
                   )
                {
                    DateTime dt = enc.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date;
                    if (AdmissionSetupForCertPeriods(adm))
                    {
                        bool needed = IsNewCertNeeded(f, adm, dt);

                        if (needed)
                        {
                            ac = CreateCertPeriodForDate(adm, dt, f.IsPlanOfCare);
                        }
                    }
                }
            }

            return ac;
        }

        //only create cert cycles if the form is a visit, eval or a POC
        public static bool CanFormAdvanceCertPeriod(IForm f)
        {
            return f.IsPlanOfCare || f.IsVisit || f.IsEval;
        }

        // figures out if the Admission has enough info to create cert cycles.  to create cert cycles, the admission needs a SOCDate,
        // PatientInsurance and the Insurance needs to have InsuranceCertDefintion rows
        public static bool AdmissionSetupForCertPeriods(Admission adm)
        {
            bool setupForCerts = false;

            if (adm.SOCDate.HasValue && adm.PatientInsuranceKey.HasValue)
            {
                if (adm.PatientInsurance != null)
                {
                    int? insuranceKey = GetInsuranceKey(adm);

                    List<InsuranceCertDefinition> certDefList = InsuranceCache.GetInsuranceCertDefs(insuranceKey);
                    if (certDefList != null)
                    {
                        setupForCerts = certDefList.Any();
                    }
                }
            }

            return setupForCerts;
        }

        // if there is no AdmissionCertification that contains the current date, a new cycl will be required
        public static bool IsNewCertNeeded(IForm f, Admission adm, DateTime dt)
        {
            bool isNeeded = false;

            if ((adm != null)
                && (adm.AdmissionCertification != null)
               )
            {
                isNeeded = !adm.AdmissionCertification.Any();
            }

            return isNeeded;
        }

        // determines if the cert cycle contains the date.  if the cert is not a POC, the cert cycle contains the date if the date between
        // the start and end dates.  if it is a POC, it the date is contained if it is between the start date minus the recert window to 
        // the end date ninus the recert window.
        public static bool DoesCertPeriodContainDate(DateTime? PeriodStartDate, DateTime? PeriodEndDate, DateTime dt,
            bool IsPOC, Admission admission)
        {
            bool containsDate = false;

            if (PeriodStartDate.HasValue && PeriodEndDate.HasValue)
            {
                DateTime start = PeriodStartDate.Value;
                DateTime end = PeriodEndDate.Value;

                if (IsPOC)
                {
                    var window = (double)(TenantSettingsCache.Current.DisciplineRecertWindowWithDefault);
                    if ((admission != null) && (admission.ServiceLineKey > 0))
                    {
                        var serviceLine = ServiceLineCache.GetServiceLineFromKey(admission.ServiceLineKey);
                        var serviceLineDisciplineRecertWindow = serviceLine.DisciplineRecertWindow.GetValueOrDefault();
                        if (serviceLineDisciplineRecertWindow > 0)
                        {
                            window = serviceLineDisciplineRecertWindow;
                        }
                    }

                    start = start.AddDays(-window);
                    end = end.AddDays(-window);
                }

                containsDate = (start <= dt) && (end >= dt);
            }

            return containsDate;
        }

        public static AdmissionCertification CreateCertPeriodForDate(Admission adm, DateTime dt, bool isPOC)
        {
            AdmissionCertification ac = MakeCertPeriodForDate(adm, dt, isPOC);
            ac = CalcDatesForCycle(adm, dt, isPOC);

            if (ac != null)
            {
                adm.AdmissionCertification.Add(ac);
                adm.RaisePropertyChangedHasFirstCert();
            }

            return ac;
        }

        public static AdmissionCertification MakeCertPeriodForDate(Admission adm, DateTime dt, bool isPOC)
        {
            AdmissionCertification ac = new AdmissionCertification();
            ac = CalcDatesForCycle(adm, dt, isPOC);
            return ac;
        }

        // this isn't correct, but it matches the current functionality.  for now, it is safer to be wrong in the same way it currently is than it is to 
        // rewrite a bunch of stuff so everything is correct.  ugh... i hate doing this.
        public static int? GetInsuranceKey(Admission adm)
        {
            int? insuranceKey = null;

            if (adm.PatientInsurance != null)
            {
                insuranceKey = adm.PatientInsurance.InsuranceKey.Value;
            }

            return insuranceKey;
        }

        // calculates the dates for a cert period based on a given date or period number
        public static AdmissionCertification CalcDatesForPdNum(Admission adm, AdmissionCertification ac)
        {
            if ((adm != null)
                && (adm.AdmissionCertification != null)
                && (adm.PatientInsurance != null)
                && (adm.SOCDate.HasValue)
                && (ac != null)
                && (ac.PeriodNumber > 0)
               )
            {
                DateTime startDate = adm.SOCDate.Value;
                DateTime? endDate = null;
                int pdNum = 0;
                InsuranceCertDefinition def = null;

                // loop through the cert cycles until we find the one that contains our date
                while (pdNum < ac.PeriodNumber)
                {
                    pdNum++;
                    def = GetDefForPeriod(GetInsuranceKey(adm), pdNum);
                    endDate = CalcEndDateForCert(def, startDate);

                    if (pdNum < ac.PeriodNumber)
                    {
                        startDate = endDate.Value.AddDays(1);
                    }
                }

                // if we found a cert definition, populate the data in the admissioncertification row
                if (def != null)
                {
                    ac.TenantID = adm.TenantID;
                    ac.PatientKey = adm.PatientKey;
                    ac.InsuranceKey = def.InsuranceKey;
                    ac.Units = def.Units;
                    ac.PeriodNumber = pdNum;
                    ac.PeriodStartDate = startDate;
                    ac.PeriodEndDate = endDate;
                    ac.Duration = def.Duration;
                }
            }

            return ac;
        }

        // calculates the dates for a cert period based on a given date
        public static AdmissionCertification CalcDatesForCycle(Admission adm, DateTime dt, bool isPOC)
        {
            AdmissionCertification ac = null;

            if ((adm != null)
                && (adm.AdmissionCertification != null)
                && (adm.PatientInsurance != null)
                && (adm.SOCDate.HasValue ||
                    (adm.HospiceAdmission && (adm.HospiceBenefitReelection || adm.TransferHospice)))
               )
            {
                DateTime? adjDate = dt;

                // if it is a poc, add the recert window from the start date
                if (isPOC)
                {
                    adjDate = dt.AddDays(TenantSettingsCache.Current.DisciplineRecertWindowWithDefault);
                }

                // figure out the date we're going to base our calculations off of
                ac = GetStartDatePdNumForCalc(adm, dt);

                // if the cert row is null or the PeriodStartDate is null, the previous method call couldn't figure out
                // the dates for the cert period
                if ((ac != null) && ac.PeriodStartDate.HasValue)
                {
                    DateTime? endDate = null;
                    DateTime startDate = ac.PeriodStartDate.Value;

                    // we can't have a cert period prior to the SOCDate so if our date is prior to the SOCDate, set it to the SOCDate
                    if (startDate < adm.SOCDate)
                    {
                        startDate = adm.SOCDate.Value;
                    }

                    int pdNum = ac.PeriodNumber;
                    InsuranceCertDefinition def = null;

                    // loop through the cert cycles until we find the one that contains our date
                    while ((!endDate.HasValue) || (adjDate > endDate))
                    {
                        def = GetDefForPeriod(GetInsuranceKey(adm), pdNum);
                        if (def == null)
                        {
                            break;
                        }

                        endDate = CalcEndDateForCert(def, startDate);

                        if (endDate < adjDate)
                        {
                            pdNum += 1;
                            startDate = endDate.Value.AddDays(1);
                        }
                    }

                    // if we found a cert definition, populate the data in the admissioncertification row
                    if (def != null)
                    {
                        ac.TenantID = adm.TenantID;
                        ac.PatientKey = adm.PatientKey;
                        ac.InsuranceKey = def.InsuranceKey;
                        ac.Units = def.Units;
                        ac.PeriodNumber = pdNum;
                        ac.PeriodStartDate = startDate.Date;
                        ac.PeriodEndDate = (endDate.HasValue ? endDate.Value.Date : (DateTime?)null);
                        ac.Duration = def.Duration;
                    }
                }
            }

            return ac;
        }

        public static DateTime? CalcEndDateForCert(InsuranceCertDefinition def, DateTime StartDate)
        {
            DateTime? endDate = null;

            if (def != null)
            {
                string unitsCode = CodeLookupCache.GetCodeFromKey(def.Units);

                if (!string.IsNullOrEmpty(unitsCode))
                {
                    if (unitsCode.ToUpper() == "YEARS")
                    {
                        endDate = StartDate.AddYears(def.Duration).AddDays(-1);
                    }
                    else if (unitsCode.ToUpper() == "MONTHS")
                    {
                        endDate = StartDate.AddMonths(def.Duration).AddDays(-1);
                    }
                    else if (unitsCode.ToUpper() == "WEEKS")
                    {
                        endDate = StartDate.AddDays((7 * def.Duration) - 1);
                    }
                    else
                    {
                        endDate = StartDate.AddDays(def.Duration - 1);
                    }
                }
            }

            return endDate;
        }

        public static InsuranceCertDefinition GetDefForPeriod(int? insuranceKey, int pdNum)
        {
            List<InsuranceCertDefinition> defList = InsuranceCache.GetInsuranceCertDefs(insuranceKey);
            if (defList == null)
            {
                return null;
            }

            InsuranceCertDefinition def = null;
            InsuranceCertDefinition maxDef = defList.OrderByDescending(icd => icd.PeriodNumber).FirstOrDefault();

            if (maxDef != null)
            {
                if (pdNum > maxDef.PeriodNumber)
                {
                    pdNum = maxDef.PeriodNumber;
                }

                def = defList.Where(icd => icd.PeriodNumber == pdNum).FirstOrDefault();
            }

            return def;
        }

        // if there is no admisisoncertification prior to the date given, return the SOC Date.  otherwise return the max
        // start date that is prior to the date
        public static AdmissionCertification GetStartDatePdNumForCalc(Admission adm, DateTime dt)
        {
            AdmissionCertification ac = null;
            if ((adm != null)
                && (adm.SOCDate.HasValue ||
                    (adm.HospiceAdmission && (adm.TransferHospice || adm.HospiceBenefitReelection)))
                && (adm.PatientInsurance != null)
                && (adm.AdmissionCertification != null)
               )
            {
                ac = new AdmissionCertification();
                AdmissionCertification maxLower = adm.AdmissionCertification.Where(acert => acert.PeriodStartDate < dt)
                    .OrderByDescending(acert => acert.PeriodStartDate)
                    .FirstOrDefault();

                if (maxLower != null)
                {
                    ac.PeriodStartDate = maxLower.PeriodEndDate.Value.AddDays(1).Date;
                    ac.PeriodNumber = maxLower.PeriodNumber + 1;
                }
                else
                {
                    var date = (adm.SOCDate.HasValue ? adm.SOCDate.Value.Date : DateTime.Today.Date);
                    ac.PeriodStartDate = date;
                    ac.PeriodNumber = 1;

                    if (adm.HospiceAdmission)
                    {
                        var key = CodeLookupCache.GetKeyFromCode("CYCLECODE", "Days");

                        if (key.HasValue)
                        {
                            ac.Units = key.Value;
                        }

                        // For hospice admissions, allow the user to key a PeriodNumber

                        ac.PeriodNumber = GetHospicePeriodNumber(adm);
                    }
                }
            }

            return ac;
        }

        private static int GetHospicePeriodNumber(Admission adm)
        {
            // For hospice admissions, allow the user to key a PeriodNumber
            if ((adm.HospiceAdmission) && (adm.HospiceBenefitReelection) && (adm.StartPeriodNumber != null) &&
                (adm.HasFirstCert == false))
            {
                return (int)adm.StartPeriodNumber;
            }

            int period = ((adm._UserTypedPeriodNumber.HasValue && adm._UserTypedPeriodNumber.Value > 0)
                ? adm._UserTypedPeriodNumber.Value
                : 1);
            return period;
        }

        public static void AdjustCertCycles(Admission adm)
        {
            if ((adm != null && adm.SOCDate.HasValue) ||
                (adm != null && adm.HospiceAdmission && (adm.TransferHospice || adm.HospiceBenefitReelection))
               )
            {
                // if we only have 1 cert cycle, we change it.  if we have more than 1 cert cycle, we only change current and future cert cycles.
                var ac = adm.AdmissionCertification.Where(a => ((adm.AdmissionCertification.Count <= 1)
                                                                || ((!a.PeriodEndDate.HasValue)
                                                                    || (a.PeriodEndDate.Value >= DateTime.Now.Date)
                                                                )
                    )
                ).OrderBy(a => a.PeriodStartDate);

                if ((ac != null)
                    && ac.Any()
                   )
                {
                    if (adm.AdmissionCertification.Count == 1)
                    {
                        AdmissionCertification currAC = ac.FirstOrDefault();
                        if ((adm.Encounter == null)
                            || (!adm.Encounter.Any(e => e.EncounterIsPlanOfCare
                                                        && e.EncounterPlanOfCare.Any(epoc =>
                                                            (epoc.CertificationFromDate == currAC.PeriodStartDate)
                                                            && (epoc.CertificationThruDate == currAC.PeriodEndDate)
                                                        )
                                )
                            )
                           )
                        {
                            AdmissionCertification calcCert = CalcDatesForPdNum(adm, currAC);
                            currAC.PeriodStartDate = calcCert.PeriodStartDate;
                            currAC.PeriodEndDate = calcCert.PeriodEndDate;
                        }
                    }
                    else
                    {
                        DateTime? myStartDate = ac.First().PeriodStartDate;
                        foreach (AdmissionCertification currAC in ac)
                        {
                            if ((adm.Encounter == null)
                                || (adm.Encounter.Any(e => e.EncounterIsPlanOfCare
                                                           && e.EncounterPlanOfCare.Any(epoc =>
                                                               (epoc.CertificationFromDate == currAC.PeriodStartDate)
                                                               && (epoc.CertificationThruDate == currAC.PeriodEndDate)
                                                           )
                                    )
                                )
                               )
                            {
                                break;
                            }

                            InsuranceCertDefinition def = GetDefForPeriod(GetInsuranceKey(adm), currAC.PeriodNumber);
                            if (myStartDate != null)
                            {
                                DateTime? endDate = CalcEndDateForCert(def, myStartDate.Value);
                                currAC.PeriodStartDate = myStartDate;
                                currAC.PeriodEndDate = endDate;
                                if (endDate.HasValue)
                                {
                                    myStartDate = endDate.Value.AddDays(1);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static AdmissionCertification GetOrCreateCertPeriodForDate(Admission adm, DateTime dt)
        {
            AdmissionCertification ac = null;
            if ((adm != null)
               )
            {
                ac = adm.AdmissionCertification.Where(a => (a.PeriodStartDate.HasValue) && (a.PeriodEndDate.HasValue)
                        && (a.PeriodStartDate.Value.Date <= dt.Date)
                        && (a.PeriodEndDate.Value.Date >= dt.Date)
                    )
                    .FirstOrDefault();

                if (ac == null)
                {
                    ac = CreateCertPeriodForDate(adm, dt, false);
                }
            }

            return ac;
        }

        internal static void SetThroughDateForHospice(Admission adm)
        {
            // if we only have 1 cert cycle, we change it.  if we have more than 1 cert cycle, we only change current and future cert cycles.
            var ac = adm.AdmissionCertification.Where(a => ((adm.AdmissionCertification.Count <= 1)
                                                            || ((!a.PeriodEndDate.HasValue)
                                                                || (a.PeriodEndDate.Value >= DateTime.Now.Date)
                                                            )
                )
            ).OrderBy(a => a.PeriodStartDate);

            if ((ac != null)
                && ac.Any()
               )
            {
                if (adm.AdmissionCertification.Count == 1 && adm._UserTypedPeriodNumber.HasValue &&
                    adm.FirstCertFromDate.HasValue)
                {
                    var def = GetDefForPeriod(GetInsuranceKey(adm), adm._UserTypedPeriodNumber.Value);

                    AdmissionCertification currAC = ac.FirstOrDefault();
                    currAC.PeriodEndDate = CalcEndDateForCert(def, adm.FirstCertFromDate.Value);
                }
            }
        }
    }
}