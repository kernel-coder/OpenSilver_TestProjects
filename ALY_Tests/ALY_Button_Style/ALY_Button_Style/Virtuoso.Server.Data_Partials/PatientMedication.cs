#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Occasional;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class PatientMedication
    {
        private Admission _CurrentAdmission;
        private Encounter _currentEncounter;
        private Patient _CurrentPatient;
        private bool _InSpanish;
        private bool _MARAdministrationTimesRequired;
        private ObservableCollection<PatientMedicationSlidingScale> _SlidingScales;

         public string DescriptionPlusHospiceIndicator
        {
            get
            {
                string desc = null;

                if (MedicationCoveredByHospice)
                {
                    desc = "(H) " + MedicationDescription;
                }
                else
                {
                    desc = MedicationDescription;
                }

                return desc;
            }
        }

        public string StartEndDateRange
        {
            get
            {
                if (MedicationEndDate.HasValue)
                {
                    return string.Format("{0} - {1}",
                        MedicationStartDate.Value.ToString("d"),
                        MedicationEndDate.HasValue ? MedicationEndDate.Value.ToString("d") : string.Empty);
                }

                return MedicationStartDate.Value.ToString("d");
            }
        }

        public string RefillDescription => string.Format("{0} {1}", MedicationDescription, StartEndDateRange);

        public bool Administered
        {
            get
            {
                if (PatientMedicationAdministrationMed.Any())
                {
                    return true;
                }

                if (HistoryKey == null)
                {
                    return false;
                }

                var historyList = Patient != null
                    ? Patient.PatientMedication
                        .Where(p => p.HistoryKey == HistoryKey || p.PatientMedicationKey == HistoryKey).ToList()
                    : null;
                if (historyList == null)
                {
                    return false;
                }

                if (historyList.Any() == false)
                {
                    return false;
                }

                foreach (var pm in historyList)
                    if (pm.PatientMedicationAdministrationMed != null && pm.PatientMedicationAdministrationMed.Any())
                    {
                        return true;
                    }

                return false;
            }
        }

        public bool ShowHospiceFooter
        {
            get
            {
                var show = false;
                var patient = Patient;
                if (patient != null
                    && patient.PatientMedication != null
                   )
                {
                    if (patient.PatientMedication.Any(pm => pm.MedicationCoveredByHospice && !pm.Superceded)
                       )
                    {
                        var last = Patient.PatientMedication.Where(pm => !pm.Superceded)
                            .OrderBy(pm => pm.MedicationDescription).ThenBy(pm => pm.MedicationStartDateTime)
                            .ThenBy(pm => pm.MedicationEndDateTime)
                            .LastOrDefault();

                        show = last != null && last.PatientMedicationKey == PatientMedicationKey;
                    }
                }

                return show;
            }
        }

        public bool AdministeredInThisVisit
        {
            get
            {
                if (Administered == false)
                {
                    return false;
                }

                if (CurrentEncounter == null)
                {
                    return false;
                }

                return PatientMedicationAdministrationMed.Where(p => p.EncounterKey == CurrentEncounter.EncounterKey)
                    .FirstOrDefault() == null
                    ? false
                    : true;
            }
        }

        public string AdministeredHyperLinkToolTip
        {
            get
            {
                if (AdministeredInThisVisit)
                {
                    return "'" + AdministeredLabel +
                           "' indicates administrations were done for this medication during this encounter.  Click here to view them.";
                }

                return "'" + AdministeredLabel +
                       "' indicates administrations were done for this medication.  Click here to view them.";
            }
        }

        public string AdministeredLabel
        {
            get
            {
                string label = null;

                if (AdministeredInThisVisit)
                {
                    label = "A*";
                }
                else if (Administered)
                {
                    label = "A";
                }

                return label;
            }
        }

        public List<CodeLookup> ValidAdministrationRoutes
        {
            get
            {
                if (_medicationRoute.HasValue)
                {
                    var children = CodeLookupCache.GetChildrenFromKey(_medicationRoute.Value)
                        .Where(w => w.Inactive == false).ToList();

                    if (_administrationRouteKey.HasValue)
                    {
                        var current = CodeLookupCache.GetCodeLookupFromKey(_administrationRouteKey.Value);
                        if (current != null && !children.Any(a => a.CodeLookupKey == current.CodeLookupKey))
                        {
                            children.Add(current);
                        }
                    }

                    return children;
                }

                return new List<CodeLookup>();
            }
        }

        public bool Reconciled
        {
            get
            {
                if (PatientMedicationReconcileMed.Any())
                {
                    return true;
                }

                if (HistoryKey == null)
                {
                    return false;
                }

                var historyList = Patient != null
                    ? Patient.PatientMedication
                        .Where(p => p.HistoryKey == HistoryKey || p.PatientMedicationKey == HistoryKey).ToList()
                    : null;
                if (historyList == null)
                {
                    return false;
                }

                if (historyList.Any() == false)
                {
                    return false;
                }

                foreach (var pm in historyList)
                    if (pm.PatientMedicationReconcileMed != null && pm.PatientMedicationReconcileMed.Any())
                    {
                        return true;
                    }

                return false;
            }
        }

        public bool ReconciledInThisVisit
        {
            get
            {
                if (Reconciled == false)
                {
                    return false;
                }

                if (CurrentEncounter == null)
                {
                    return false;
                }

                return PatientMedicationReconcileMed.Where(p => p.EncounterKey == CurrentEncounter.EncounterKey)
                    .FirstOrDefault() == null
                    ? false
                    : true;
            }
        }

        public string ReconciledHyperLinkText
        {
            get
            {
                if (ReconciledInThisVisit)
                {
                    return "R*";
                }

                if (Reconciled)
                {
                    return "R";
                }

                return "";
            }
        }

        public string ReconciledHyperLinkToolTip
        {
            get
            {
                if (ReconciledInThisVisit)
                {
                    return "'" + ReconciledHyperLinkText +
                           "' indicates reconciliations were done for this medication during this encounter.  Click here to view them.";
                }

                return "'" + ReconciledHyperLinkText +
                       "' indicates reconciliations were done for this medication.  Click here to view them.";
            }
        }

        public bool Taught
        {
            get
            {
                if (PatientMedicationTeachingMed.Any())
                {
                    return true;
                }

                if (HistoryKey == null)
                {
                    return false;
                }

                var historyList = Patient.PatientMedication
                    .Where(p => p.HistoryKey == HistoryKey || p.PatientMedicationKey == HistoryKey).ToList();
                if (historyList == null)
                {
                    return false;
                }

                if (historyList.Any() == false)
                {
                    return false;
                }

                foreach (var pm in historyList)
                    if (pm.PatientMedicationTeachingMed != null && pm.PatientMedicationTeachingMed.Any())
                    {
                        return true;
                    }

                return false;
            }
        }

        public bool TaughtInThisVisit
        {
            get
            {
                if (Taught == false)
                {
                    return false;
                }

                if (CurrentEncounter == null)
                {
                    return false;
                }

                return PatientMedicationTeachingMed.Where(p => p.EncounterKey == CurrentEncounter.EncounterKey)
                    .FirstOrDefault() == null
                    ? false
                    : true;
            }
        }

        public string TaughtHyperLinkText
        {
            get
            {
                if (TaughtInThisVisit)
                {
                    return "T*";
                }

                if (Taught)
                {
                    return "T";
                }

                return "";
            }
        }

        public string TaughtHyperLinkToolTip
        {
            get
            {
                if (TaughtInThisVisit)
                {
                    return "'" + TaughtHyperLinkText +
                           "' indicates teachings were done for this medication during this encounter.  Click here to view them.";
                }

                return "'" + TaughtHyperLinkText +
                       "' indicates teachings were done for this medication.  Click here to view them.";
            }
        }

        public bool Managed
        {
            get
            {
                if (PatientMedicationManagementMed.Any())
                {
                    return true;
                }

                if (HistoryKey == null)
                {
                    return false;
                }

                var historyList = Patient != null
                    ? Patient.PatientMedication
                        .Where(p => p.HistoryKey == HistoryKey || p.PatientMedicationKey == HistoryKey).ToList()
                    : null;
                if (historyList == null)
                {
                    return false;
                }

                if (historyList.Any() == false)
                {
                    return false;
                }

                foreach (var pm in historyList)
                    if (pm.PatientMedicationManagementMed != null && pm.PatientMedicationManagementMed.Any())
                    {
                        return true;
                    }

                return false;
            }
        }

        public bool ManagedInThisVisit
        {
            get
            {
                if (Managed == false)
                {
                    return false;
                }

                if (CurrentEncounter == null)
                {
                    return false;
                }

                return PatientMedicationManagementMed.Where(p => p.EncounterKey == CurrentEncounter.EncounterKey)
                    .FirstOrDefault() == null
                    ? false
                    : true;
            }
        }

        public string ManagedHyperLinkText
        {
            get
            {
                if (ManagedInThisVisit)
                {
                    return "M*";
                }

                if (Managed)
                {
                    return "M";
                }

                return "";
            }
        }

        public string ManagedHyperLinkToolTip
        {
            get
            {
                if (ManagedInThisVisit)
                {
                    return "'" + ManagedHyperLinkText +
                           "' indicates managements were done for this medication during this encounter.  Click here to view them.";
                }

                return "'" + ManagedHyperLinkText +
                       "' indicates managements were done for this medication.  Click here to view them.";
            }
        }

        public string MedicationDosageUnitDescription =>
            CodeLookupCache.GetCodeDescriptionFromKey(MedicationDosageUnit);

        public string MedicationDosageUnitCode => CodeLookupCache.GetCodeFromKey(MedicationDosageUnit);

        public string MedicationRouteCode => CodeLookupCache.GetCodeFromKey(MedicationRoute);

        public bool IsMedicationRouteCodeAllowPump
        {
            get
            {
                var mrc = MedicationRouteCode;
                if (string.IsNullOrWhiteSpace(mrc))
                {
                    return false;
                }

                mrc = mrc.ToLower();
                return mrc == "epidural" || mrc == "implant" || mrc == "injection" || mrc == "perfusion" ||
                       mrc == "subcutaneous" || mrc.StartsWith("intra")
                    ? true
                    : false;
            }
        }

        public bool IsMedicationRouteCodeInjectable
        {
            get
            {
                var mrc = MedicationRouteCode;
                if (string.IsNullOrWhiteSpace(mrc))
                {
                    return false;
                }

                mrc = mrc.ToLower();
                return mrc == "injection" || mrc == "subcutaneous" || mrc.StartsWith("intra") ? true : false;
            }
        }

        public string MedicationRouteDescription
        {
            get
            {
                var desc = CodeLookupCache.GetCodeDescriptionFromKey(MedicationRoute);
                return desc;
            }
        }

        public string MedicationAdministrationRouteDescription
        {
            get
            {
                var adesc = CodeLookupCache.GetCodeDescriptionFromKey(AdministrationRouteKey);
                if (adesc != null)
                {
                    var rdesc = MedicationRouteDescription;
                    if (adesc != rdesc)
                    {
                        return adesc;
                    }
                }

                return string.Empty;
            }
        }

        public double MedicationNewChangedFlagWidth
        {
            get
            {
                if (CurrentEncounter == null)
                {
                    return 1;
                }

                double width = CurrentEncounter.EncounterIsPlanOfCare ? 20 : 1;
                return width;
            }
        }

        public string MedicationNewChangedFlag
        {
            get
            {
                if (MedicationNew == false && MedicationChanged == false)
                {
                    return null;
                }

                if (MedicationStartDate == null)
                {
                    return null;
                }

                if (CurrentEncounter == null)
                {
                    return null;
                }

                if (CurrentEncounter.EncounterIsPlanOfCare == false)
                {
                    return null;
                }

                if (CurrentEncounter.EncounterPlanOfCare == null)
                {
                    return null;
                }

                var ePOC = CurrentEncounter.EncounterPlanOfCare.FirstOrDefault();
                if (ePOC == null)
                {
                    return null;
                }

                if (ePOC.CertificationFromDate == null)
                {
                    return null;
                }

                if (MedicationNew)
                {
                    var date = ((DateTime)ePOC.CertificationFromDate).AddDays(-30).Date;
                    if (((DateTime)MedicationStartDate).Date >= date)
                    {
                        return "N";
                    }
                }

                if (MedicationChanged)
                {
                    var date = ((DateTime)ePOC.CertificationFromDate).AddDays(-60).Date;
                    if (((DateTime)MedicationStartDate).Date >= date)
                    {
                        return "C";
                    }
                }

                return null;
            }
        }

        public bool IsMedicationEndDated => MedicationEndDate == null ? false : true;

        public bool MedicationStatusIsCurrent => MedicationStatus == 0 ? true : false;

        // MedicationStatus used for sorting and UI: 0=current, 1=future, 2=discontinued
        // Also used to filter out medications for MAR times - E.G. disallow administering a discontinued medication
        public int MedicationStatus
        {
            get
            {
                var ret = 0;
                var EncounterDate = DateTime.Today;
                if (CurrentEncounter != null)
                {
                    if (CurrentEncounter.EncounterStartDate != null)
                    {
                        if (CurrentEncounter.EncounterStartDate != DateTime.MinValue)
                        {
                            EncounterDate = CurrentEncounter.EncounterStartDate.Value.Date;
                        }
                    }
                }

                if (MedicationEndDate.HasValue && MedicationEndDate.Value.Date < EncounterDate)
                {
                    ret = 2;
                }
                else if (MedicationStartDate == null || MedicationStartDate == DateTime.MinValue)
                {
                    ret = 0;
                }
                else if (MedicationStartDate.Value.Date > EncounterDate)
                {
                    ret = 1;
                }
                else if (MedicationEndDate == null || MedicationEndDate == DateTime.MinValue)
                {
                    ret = 0;
                }

                return ret;
            }
        }

        public Encounter CurrentEncounter
        {
            get { return _currentEncounter; }
            set
            {
                _currentEncounter = value;
                RaisePropertyChanged("MedicationStatus");
                RaisePropertyChanged("CanFullEdit");
                RaisePropertyChanged("CanFullEditMediSpan");
                RaisePropertyChanged("CanDelete");
            }
        }

        public override bool CanFullEdit
        {
            get
            {
                // Plan of Care can edit everything 01/31/2013
                if (CurrentEncounter != null && CurrentEncounter.FormKey != null &&
                    DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey).IsPlanOfCare)
                {
                    return true;
                }

                if (CurrentEncounter == null)
                {
                    // Not part of an encounter (regular patient maint) - can fully edit only new items
                    if (IsNew || PatientMedicationKey <= 0)
                    {
                        return true;
                    }

                    return false;
                }

                // Part of an encounter- can edit new items and any item that was added during this encounter
                if (IsNew || PatientMedicationKey <= 0)
                {
                    return true;
                }

                return AddedFromEncounterKey == CurrentEncounter.EncounterKey ? true : false;
            }
        }

        public bool CanFullEditMediSpan
        {
            get
            {
                if (IsMediSpanMedication)
                {
                    return false;
                }

                return CanFullEdit;
            }
        }

        public override bool CanDelete
        {
            get
            {
                // Can delete new items that were OKed 
                if (IsNew || PatientMedicationKey <= 0)
                {
                    return IsOKed;
                }

                if (CurrentEncounter == null)
                    // Not part of an encounter (regular patient maint) - can fully edit/delete only new items
                {
                    return CanFullEdit;
                }

                // Part of an encounter- can delete items that was added during this encounter
                return AddedFromEncounterKey == CurrentEncounter.EncounterKey ? true : false;
            }
        }

        public string ObsoleteText => ObsoleteDate == null
            ? null
            : "(Obsolete medication as of " + ((DateTime)ObsoleteDate).ToString("MM/dd/yyyy") + ")";

        public ObservableCollection<PatientMedicationSlidingScale> SlidingScales
        {
            get
            {
                if (_SlidingScales == null)
                {
                    _SlidingScales = GetSlidingScalesForPatient();
                }

                return _SlidingScales;
            }
            set
            {
                _SlidingScales = value;
                RaisePropertyChanged("SlidingScales");
            }
        }

        public bool HasMedicationFrequencyData => string.IsNullOrWhiteSpace(MedicationFrequencyData) ? false : true;

        public bool MARAdministrationTimesRequired
        {
            get { return _MARAdministrationTimesRequired; }
            set
            {
                _MARAdministrationTimesRequired = value;
                RaisePropertyChanged("MARAdministrationTimesRequired");
            }
        }

        public int MARAdministrationTimesCount
        {
            get
            {
                // MedicationFrequencyData format:  CodeLookupKey|Count1|Count2 - if not counts = '?' is passed
                if (string.IsNullOrWhiteSpace(MedicationFrequencyData))
                {
                    return 0;
                }

                if (AsNeeded)
                {
                    return 0;
                }

                string[] DELIMITER = { "|" };
                var fDataArray = MedicationFrequencyData.Split(DELIMITER, StringSplitOptions.None);
                if (fDataArray.Length == 0)
                {
                    return 0;
                }

                var clKey = 0;
                try
                {
                    clKey = int.Parse(fDataArray[0]);
                }
                catch
                {
                }

                var cl = CodeLookupCache.GetCodeLookupFromKey(clKey);
                if (cl == null)
                {
                    return 0;
                }

                if (string.IsNullOrWhiteSpace(cl.ApplicationData))
                {
                    return 0;
                }

                // Valid ApplicationData 0,1,H,X
                //Per PO, 'Hourly' frequency is treated as a PRN
                if (cl.ApplicationData.Trim().Equals("0"))
                {
                    return 0;
                }

                if (cl.ApplicationData.Trim().Equals("1"))
                {
                    return 1;
                }

                if (fDataArray.Length <= 1)
                {
                    return 0;
                }

                if (fDataArray[1].Trim().Equals("?"))
                {
                    return 0;
                }

                var fdCount = 0;
                try
                {
                    fdCount = int.Parse(fDataArray[1]);
                }
                catch
                {
                }

                if (fdCount == 0)
                {
                    return 0;
                }

                if (fdCount > 24)
                {
                    return 0;
                }

                if (cl.ApplicationData.Trim().Equals("X"))
                {
                    return fdCount; // X = |{0}|times per day
                }

                if (cl.ApplicationData.Trim()
                    .Equals("H")) //H = Every|{0}|hours - only honor MAR times if the hours is a factor of 24 - otherwise it is treated as a PRN
                {
                    // Per PO: MAR - only even multiples of 24
                    if (fdCount == 2)
                    {
                        return 12;
                    }

                    if (fdCount == 4)
                    {
                        return 6;
                    }

                    if (fdCount == 6)
                    {
                        return 4;
                    }

                    if (fdCount == 8)
                    {
                        return 3;
                    }

                    if (fdCount == 12)
                    {
                        return 2;
                    }
                }

                return 0;
            }
        }

        public string MARAdministrationTimesLabel
        {
            get
            {
                var count = MARAdministrationTimesCount;
                return "MAR Administration Times" + (count == 0 ? "" : " (" + count.ToString().Trim() + ")");
            }
        }

        public string MARAdministrationTimesMessage
        {
            get
            {
                if (HasMARAdministrationTimes == false)
                {
                    if (AsNeeded)
                    {
                        return "This frequency is administered as needed";
                    }

                    return "This frequency is not available in MAR";
                }

                return string.Empty; // in place of this message - display the MARAdministrationTimes codelookup
            }
        }

        public bool HasMARAdministrationTimes => MARAdministrationTimesCount == 0 ? false : true;

        // Frequency               Num   Max  Interval  Unit
        //
        // Every X minutes          0     0      X      Minute
        // Hourly                   1     0      0      Hour
        // Every X hours            0     0      X      Hour
        // Every X to Y hours       0     Y      X      Hour
        // Daily                    1     0      0      Day
        // Every morning            1     0      0      Day
        // Every afternoon          1     0      0      Day
        // Every night at bedtime   1     0      0      Day
        // X times per day          X     0      0      Day
        // Every X days             0     0      X      Day
        // Weekly                   1     0      0      Week
        // X times per week         X     0      0      Week
        // X to Y times per day     X     Y      0      Day

        public string MedicationFrequencyUnit
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MedicationFrequencyData))
                {
                    return "";
                }

                string[] frequencyDataDelimiter = { "|" };
                var pieces = MedicationFrequencyData.Split(frequencyDataDelimiter, StringSplitOptions.None);
                var key = 0;
                try
                {
                    key = int.Parse(pieces[0]);
                }
                catch
                {
                }

                var code = CodeLookupCache.GetCodeFromKey(key);
                if (code.ToLower().Equals("every|{0}|minutes"))
                {
                    return "Minute";
                }

                if (code.ToLower().Equals("hourly"))
                {
                    return "Hour";
                }

                if (code.ToLower().Equals("every|{0}|hours"))
                {
                    return "Hour";
                }

                if (code.ToLower().Equals("every|{0}|to|{1}|hours"))
                {
                    return "Hour";
                }

                if (code.ToLower().Equals("daily"))
                {
                    return "Day";
                }

                if (code.ToLower().Equals("every morning"))
                {
                    return "Day";
                }

                if (code.ToLower().Equals("every afternoon"))
                {
                    return "Day";
                }

                if (code.ToLower().Equals("every night at bedtime"))
                {
                    return "Day";
                }

                if (code.ToLower().Equals("|{0}|times per day"))
                {
                    return "Day";
                }

                if (code.ToLower().Equals("every|{0}|days"))
                {
                    return "Day";
                }

                if (code.ToLower().Equals("weekly"))
                {
                    return "Week";
                }

                if (code.ToLower().Equals("|{0}|times per week"))
                {
                    return "Week";
                }

                if (code.ToLower().Equals("every|{0}|weeks"))
                {
                    return "Week";
                }

                if (code.ToLower().Equals("other|{0}"))
                {
                    return pieces.Length >= 2 ? pieces[1] : "";
                }

                return "";
            }
        }

        public string MedicationFrequencyNumber
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MedicationFrequencyData))
                {
                    return "";
                }

                string[] frequencyDataDelimiter = { "|" };
                var pieces = MedicationFrequencyData.Split(frequencyDataDelimiter, StringSplitOptions.None);
                var key = 0;
                try
                {
                    key = int.Parse(pieces[0]);
                }
                catch
                {
                }

                var code = CodeLookupCache.GetCodeFromKey(key);
                if (code.ToLower().Equals("every|{0}|minutes"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("hourly"))
                {
                    return "1";
                }

                if (code.ToLower().Equals("every|{0}|hours"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every|{0}|to|{1}|hours"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("daily"))
                {
                    return "1";
                }

                if (code.ToLower().Equals("every morning"))
                {
                    return "1";
                }

                if (code.ToLower().Equals("every afternoon"))
                {
                    return "1";
                }

                if (code.ToLower().Equals("every night at bedtime"))
                {
                    return "1";
                }

                if (code.ToLower().Equals("|{0}|times per day"))
                {
                    return pieces.Length >= 2 ? pieces[1] : "0";
                }

                if (code.ToLower().Equals("every|{0}|days"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("weekly"))
                {
                    return "1";
                }

                if (code.ToLower().Equals("|{0}|times per week"))
                {
                    return pieces.Length >= 2 ? pieces[1] : "0";
                }

                if (code.ToLower().Equals("every|{0}|weeks"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("other|{0}"))
                {
                    return "0";
                }

                return "0";
            }
        }

        public string MedicationFrequencyInterval
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MedicationFrequencyData))
                {
                    return "";
                }

                string[] frequencyDataDelimiter = { "|" };
                var pieces = MedicationFrequencyData.Split(frequencyDataDelimiter, StringSplitOptions.None);
                var key = 0;
                try
                {
                    key = int.Parse(pieces[0]);
                }
                catch
                {
                }

                var code = CodeLookupCache.GetCodeFromKey(key);
                if (code.ToLower().Equals("every|{0}|minutes"))
                {
                    return pieces.Length >= 2 ? pieces[1] : "0";
                }

                if (code.ToLower().Equals("hourly"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every|{0}|hours"))
                {
                    return pieces.Length >= 2 ? pieces[1] : "0";
                }

                if (code.ToLower().Equals("every|{0}|to|{1}|hours"))
                {
                    return pieces.Length >= 2 ? pieces[1] : "0";
                }

                if (code.ToLower().Equals("daily"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every morning"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every afternoon"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every night at bedtime"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("|{0}|times per day"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every|{0}|days"))
                {
                    return pieces.Length >= 2 ? pieces[1] : "0";
                }

                if (code.ToLower().Equals("weekly"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("|{0}|times per week"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every|{0}|weeks"))
                {
                    return pieces.Length >= 2 ? pieces[1] : "0";
                }

                if (code.ToLower().Equals("other|{0}"))
                {
                    return "0";
                }

                return "0";
            }
        }

        public string MedicationFrequencyMax
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MedicationFrequencyData))
                {
                    return "";
                }

                string[] frequencyDataDelimiter = { "|" };
                var pieces = MedicationFrequencyData.Split(frequencyDataDelimiter, StringSplitOptions.None);
                var key = 0;
                try
                {
                    key = int.Parse(pieces[0]);
                }
                catch
                {
                }

                var code = CodeLookupCache.GetCodeFromKey(key);
                if (code.ToLower().Equals("every|{0}|minutes"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("hourly"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every|{0}|hours"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every|{0}|to|{1}|hours"))
                {
                    return pieces.Length >= 3 ? pieces[2] : "0";
                }

                if (code.ToLower().Equals("daily"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every morning"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every afternoon"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every night at bedtime"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("|{0}|times per day"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every|{0}|days"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("weekly"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("|{0}|times per week"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("every|{0}|weeks"))
                {
                    return "0";
                }

                if (code.ToLower().Equals("other|{0}"))
                {
                    return "0";
                }

                return "0";
            }
        }

        public string SIG
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MedicationFrequencyDescription))
                {
                    return null;
                }

                var sig = MedicationFrequencyDescription.Trim();
                if (AsNeeded)
                {
                    sig = sig + ", as needed";
                    if (string.IsNullOrWhiteSpace(AsNeededFor) == false)
                    {
                        sig = sig + " for " + AsNeededFor.Trim();
                    }
                }

                return sig;
            }
        }

        public string MedicationDescription
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MedicationName))
                {
                    return null;
                }

                var description = AddedInError ? "(Added in error) " : "";
                description = description + MedicationDescriptionPartial;

                description = AddSlidingScaleTextToMedicationDescription(description);

                return description;
            }
        }

        private string MedicationDescriptionPartial
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MedicationName))
                {
                    return null;
                }

                var description = MedicationName.Trim();
                if (string.IsNullOrWhiteSpace(MedicationDosageAmount) == false)
                {
                    description = description + ", " + DosageFormat(MedicationDosageAmount.Trim());
                }

                if (MedicationDosageVarying && string.IsNullOrWhiteSpace(MedicationDosageAmountTo) == false)
                {
                    description = description + " to " + DosageFormat(MedicationDosageAmountTo.Trim());
                }

                if (string.IsNullOrWhiteSpace(MedicationDosageUnitDescription) == false)
                {
                    description = description + " " + MedicationDosageUnitDescription.Trim();
                }

                if (!string.IsNullOrWhiteSpace(MedicationRouteDescription) &&
                    string.IsNullOrWhiteSpace(MedicationAdministrationRouteDescription))
                {
                    description = description + ", " + MedicationRouteDescription.Trim();
                }

                if (!string.IsNullOrWhiteSpace(MedicationAdministrationRouteDescription))
                {
                    description = description + ", " + MedicationAdministrationRouteDescription.Trim();
                }

                if (string.IsNullOrWhiteSpace(SIG) == false)
                {
                    description = description + ",  " + SIG.Trim();
                }

                return description;
            }
        }

        public string MedicationCoveredByHospiceDisplay
        {
            get
            {
                if (MedicationCoveredByHospice)
                {
                    if (CurrentAdmission == null)
                    {
                        return "H";
                    }

                    return CurrentAdmission.HospiceAdmission ? "H" : "";
                }

                return "";
            }
        }

        public string MedicationDosageAmountDescription
        {
            get
            {
                var dosageFormat = DosageFormat(MedicationDosageAmount);
                if (string.IsNullOrWhiteSpace(dosageFormat))
                {
                    return null;
                }

                return MedicationDosageAmount == dosageFormat ? null : "(" + dosageFormat + ")";
            }
        }

        public string MedicationDosageAmountToDescription
        {
            get
            {
                var dosageFormat = DosageFormat(MedicationDosageAmountTo);
                if (string.IsNullOrWhiteSpace(dosageFormat))
                {
                    return null;
                }

                return MedicationDosageAmountTo == dosageFormat ? null : "(" + dosageFormat + ")";
            }
        }

        public Patient CurrentPatient
        {
            get { return _CurrentPatient; }
            set
            {
                _CurrentPatient = value;
                RaisePropertyChanged("CurrentPatient");
                RaisePropertyChanged("PatientPharmacyPickList");
            }
        }

        public Admission CurrentAdmission
        {
            get { return _CurrentAdmission; }
            set
            {
                _CurrentAdmission = value;
                RaisePropertyChanged("CurrentAdmission");
                RaisePropertyChanged("IsHospiceAdmission");
            }
        }

        public bool IsHospiceAdmission
        {
            get
            {
                if (CurrentAdmission == null)
                {
                    return false;
                }

                return CurrentAdmission.HospiceAdmission;
            }
        }

        public List<PatientPharmacy> PatientPharmacyPickList
        {
            get
            {
                if (CurrentPatient == null)
                {
                    return null;
                }

                if (CurrentPatient.PatientPharmacy == null)
                {
                    return null;
                }

                var ppList = CurrentPatient.PatientPharmacy.Where(p => p.HistoryKey == null && p.PatientPharmacyKey != 0
                                                                       && (p.PharmacyStartDate.HasValue == false ||
                                                                           p.PharmacyStartDate.HasValue &&
                                                                           ((DateTime)p.PharmacyStartDate).Date <=
                                                                           DateTime.UtcNow.Date)
                                                                       && (p.PharmacyEndDate.HasValue == false ||
                                                                           p.PharmacyEndDate.HasValue &&
                                                                           ((DateTime)p.PharmacyEndDate).Date >=
                                                                           DateTime.UtcNow.Date)
                                                                       || p.PatientPharmacyKey == PatientPharmacyKey)
                    .ToList();
                ppList.Insert(0, new PatientPharmacy { PatientPharmacyKey = 0 });
                return ppList;
            }
        }

        public bool InSpanish
        {
            get { return _InSpanish; }
            set
            {
                _InSpanish = value;
                RaisePropertyChanged("InSpanish");
            }
        }

        public bool IsIV
        {
            get
            {
                var code = MedicationRouteCode;
                return string.IsNullOrWhiteSpace(code) ? false : code.ToLower().Contains("intravenous");
            }
        }

        public bool IsHighRiskMedication
        {
            get
            {
                if (CurrentEncounter != null)
                {
                    return IsHighRiskMedicationFromEncounter(CurrentEncounter);
                }

                return IsHighRiskMedicationFromPatient;
            }
        }

        private bool IsHighRiskMedicationFromPatient
        {
            get
            {
                // see if medication is defined as HighRisk - as of today - in any active admissions
                if (MediSpanMedicationKey == null || RDID == null)
                {
                    return false;
                }

                if (Patient == null)
                {
                    return false;
                }

                if (Patient.Admission == null)
                {
                    return false;
                }

                var aList = Patient.Admission.Where(a =>
                    a.AdmissionStatusCode == "A" || a.AdmissionStatusCode == "R" || a.AdmissionStatusCode == "H" ||
                    a.AdmissionStatusCode == "T" || a.AdmissionStatusCode == "M").ToList();
                if (aList == null)
                {
                    return false;
                }

                foreach (var a in aList)
                {
                    var m = HighRiskMedicationCache
                        .GetHighRiskMedicationByRDIDAndServiceLineKey(RDID, a.ServiceLineKey);
                    if (m != null && HighRiskMedicationCache.IsHighRiskMedicationActive(m))
                    {
                        return true;
                    }
                }

                return false;
            }
        }


        public string IVContinuousString
        {
            get
            {
                var code = CodeLookupCache.GetCodeLookupFromKey(IVContinuous.GetValueOrDefault());
                return code == null ? "" : code.CodeDescription;
            }
        }

        public string IVTypeString
        {
            get
            {
                var code = CodeLookupCache.GetCodeLookupFromKey(IVType.GetValueOrDefault());
                return code == null ? "" : code.CodeDescription;
            }
        }

        public void RefreshAdministered()
        {
            RaisePropertyChanged("Administered");
            RaisePropertyChanged("AdministeredLabel");
        }

        public void RefreshReconciled()
        {
            RaisePropertyChanged("Reconciled");
            RaisePropertyChanged("ReconciledHyperLinkText");
        }

        public void RefreshTaught()
        {
            RaisePropertyChanged("Taught");
            RaisePropertyChanged("TaughtHyperLinkText");
        }

        public void RefreshManaged()
        {
            RaisePropertyChanged("Managed");
            RaisePropertyChanged("ManagedHyperLinkText");
        }

        public PatientMedication CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newmed = (PatientMedication)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newmed);
            if (newmed.HistoryKey == null)
            {
                newmed.HistoryKey = PatientMedicationKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newmed;
        }

        partial void OnMedicationStartDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MedicationStatus");
            RaisePropertyChanged("MedicationNewChangedFlag");
        }

        partial void OnMedicationEndDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (MedicationEndDate == null)
            {
                AddedInError = false;
                DisposedFlag = false;
            }

            RaisePropertyChanged("MedicationStatus");
            RaisePropertyChanged("IsMedicationEndDated");
            RaisePropertyChanged("MedicationNewChangedFlag");
        }

        partial void OnMedicationNewChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (MedicationNew)
            {
                MedicationChanged = false;
            }

            RaisePropertyChanged("MedicationNewChangedFlag");
        }

        partial void OnMedicationChangedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (MedicationChanged)
            {
                MedicationNew = false;
            }

            RaisePropertyChanged("MedicationNewChangedFlag");
        }

        partial void OnPatientMedicationKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CanFullEdit");
            RaisePropertyChanged("CanFullEditMediSpan");
            RaisePropertyChanged("CanDelete");
            RaisePropertyChanged("MedicationNewChangedFlag");
        }

        partial void OnAddedFromEncounterKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CanFullEdit");
            RaisePropertyChanged("CanFullEditMediSpan");
            RaisePropertyChanged("CanDelete");
        }

        partial void OnMediSpanMedicationKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsMediSpanMedication");
            RaisePropertyChanged("IsPrescription");
            RaisePropertyChanged("IsPrescriptionDataAllowed");
            RaisePropertyChanged("IsHighRiskMedication");
            RaisePropertyChanged("CanFullEditMediSpan");
        }

        partial void OnRDIDChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsHighRiskMedication");
        }

        partial void OnMedicationNameChanging(string newMedicationName)
        {
            if (IsDeserializing)
            {
                return;
            }

            if (MediSpanMedicationKey != null && MedicationName != null && newMedicationName != null)
            {
                // User overwrote the MedicationName - assume Med went from MediSpanMedication to nonMediSpanMedication - or
                // find a way to speed up cache search to see if med is still a MediSpanMedication
                MediSpanMedicationKey = null;
                RDID = null;
                ObsoleteDate = null;
            }
        }

        partial void OnMedicationNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MedicationDescription");
        }

        partial void OnMedicationRXTypeChanging(int? newMedicationRXType)
        {
            if (IsDeserializing)
            {
                return;
            }

            if (IsOverTheCounter && MedicationRXType != null && newMedicationRXType != null)
            {
                MedicationDurationDays = null;
                MedicationQuantity = null;
                MedicationRefills = null;
                PatientPharmacyKey = null;
                PrescriptionPhysicianKey = null;
            }

            // Default Pharmacy if required and there is only one
            if (newMedicationRXType == 1 && IsNew && PatientPharmacyPickList != null)
            {
                if (PatientPharmacyPickList.Count == 2)
                {
                    PatientPharmacyKey = PatientPharmacyPickList[1].PatientPharmacyKey;
                }
            }

            // Default PrescriptionPhysician if required 
            if (newMedicationRXType == 1 && IsNew)
            {
                if (CurrentPatient != null)
                {
                    PrescriptionPhysicianKey = CurrentPatient.MostRecentSigningPhysician;
                }
            }

            RaisePropertyChanged("IsPrescription");
            RaisePropertyChanged("IsPrescriptionDataAllowed");
        }

        partial void OnObsoleteDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ObsoleteText");
        }

        partial void OnMedicationDosageVaryingChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (MedicationDosageVarying == false && string.IsNullOrWhiteSpace(MedicationDosageAmountTo) == false)
            {
                MedicationDosageAmountTo = null;
                SlidingScale = false;
            }
        }

        private ObservableCollection<PatientMedicationSlidingScale> GetSlidingScalesForPatient()
        {
            var ret = new ObservableCollection<PatientMedicationSlidingScale>();

            var me = Patient.PatientMedication.Where(a => a.PatientMedicationKey == PatientMedicationKey)
                .FirstOrDefault();

            foreach (var item in me.PatientMedicationSlidingScale.OrderBy(a => a.LowerRange)) ret.Add(item);

            return ret;
        }

        partial void OnMedicationDosageAmountToChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MedicationDescription");
            RaisePropertyChanged("MedicationDosageAmountToDescription");
        }

        partial void OnMedicationDosageAmountChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MedicationDescription");
            RaisePropertyChanged("MedicationDosageAmountDescription");
        }

        partial void OnMedicationDosageUnitChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MedicationDosageUnitDescription");
            RaisePropertyChanged("MedicationDescription");
        }

        partial void OnAdministrationRouteKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MedicationDescription");
        }

        partial void OnMedicationRouteChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (IsIV == false)
            {
                IVConcentration = null;
                IVRate = null;
                IVFirstInControlledSetting = false;
                IVContinuous = null;
                IVType = null;
            }

            IVFlag = IsIV;

            if (IsMedicationRouteCodeAllowPump == false)
            {
                InfusionPumpUsed = false;
            }

            var RouteCodeLookupKey = MedicationRoute;
            var z = RouteCodeLookupKey.HasValue ? CodeLookupCache.GetCodeLookupFromKey(RouteCodeLookupKey.Value) : null;
            var desc = z != null ? z.CodeDescription : string.Empty;
            if (desc != "")
            {
                var y = CodeLookupCache.GetCodeLookupsFromType("MedRouteAdminMethod")
                    .Where(w => w.CodeDescription == desc).FirstOrDefault();
                if (y != null)
                {
                    RaisePropertyChanged("ValidAdministrationRoutes");

                    AdministrationRouteKey = y.CodeLookupKey;
                    RaisePropertyChanged("AdministrationRouteKey");
                    RaisePropertyChanged("AdministrationRoute");
                }
            }

            RaisePropertyChanged("MedicationRouteCode");
            RaisePropertyChanged("IsMedicationRouteCodeAllowPump");
            RaisePropertyChanged("MedicationRouteDescription");
            RaisePropertyChanged("MedicationDescription");
            RaisePropertyChanged("IsIV");
        }

        partial void OnInfusionPumpUsedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (InfusionPumpUsed == false)
            {
                InfusionPumpEquipmentKey = null;
                InfusionPumpEquipmentItemDesc = null;
            }
        }

        partial void OnDisposedFlagChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (DisposedFlag == false)
            {
                DisposedBy = null;
                DisposedDateTimeOffSet = null;
                DisposedWitnessLastName = null;
                DisposedWitnessFirstName = null;
                DisposedQuantity = null;
                DisposedComment = null;
            }
            else
            {
                DisposedDateTimeOffSet = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                DisposedBy = WebContext.Current.User.MemberID;
            }
        }

        partial void OnMedicationFrequencyDescriptionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("SIG");
            RaisePropertyChanged("MedicationDescription");
        }

        partial void OnMedicationFrequencyDataChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("HasMedicationFrequencyData");
            RaisePropertyChanged("MARAdministrationTimesCount");
            RaisePropertyChanged("MARAdministrationTimesLabel");
            RaisePropertyChanged("MARAdministrationTimesMessage");
            RaisePropertyChanged("HasMARAdministrationTimes");
        }

        partial void OnAsNeededChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (AsNeeded == false)
            {
                AsNeededFor = null;
            }

            RaisePropertyChanged("SIG");
            RaisePropertyChanged("MedicationDescription");
            if (AsNeeded)
            {
                MARAdministrationTimes = null;
            }

            RaisePropertyChanged("MARAdministrationTimesCount");
            RaisePropertyChanged("MARAdministrationTimesLabel");
            RaisePropertyChanged("MARAdministrationTimesMessage");
            RaisePropertyChanged("HasMARAdministrationTimes");
        }

        partial void OnAddedInErrorChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MedicationDescription");
        }

        partial void OnAsNeededForChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("SIG");
            RaisePropertyChanged("MedicationDescription");
        }

        private string AddSlidingScaleTextToMedicationDescription(string description)
        {
            if (PatientMedicationSlidingScale != null && PatientMedicationSlidingScale.Any())
            {
                var count = 0;
                description += "\n" + "Sliding Scale: ";

                foreach (var item in PatientMedicationSlidingScale.OrderBy(a => a.LowerRange))
                {
                    count++;

                    description += " " + item.LowerRange + " - " + item.UpperRange + " value / " + item.Dosage + " " +
                                   MedicationDosageUnitDescription;

                    if (count < PatientMedicationSlidingScale.Count)
                    {
                        description += ",";
                    }
                }
            }

            return description;
        }

        private string DosageFormat(string doseage)
        {
            if (string.IsNullOrWhiteSpace(doseage))
            {
                return null;
            }

            double d = 0;
            try
            {
                d = Convert.ToDouble(doseage);
            }
            catch
            {
                d = 0;
            }

            if (d == 0.5)
            {
                return "one-half";
            }

            if (d == 0.25)
            {
                return "one-quarter";
            }

            return doseage;
        }

        public void RaiseChanged()
        {
            RaisePropertyChanged("PatientPharmacyPickList");
        }

        public void RaiseChanged(string PropertyName)
        {
            RaisePropertyChanged(PropertyName);
        }

        public bool IsHighRiskMedicationFromEncounter(Encounter encounter)
        {
            if (MediSpanMedicationKey == null || RDID == null)
            {
                return false;
            }

            if (encounter == null)
            {
                return false;
            }

            if (encounter.IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry)
            {
                return false;
            }

            if (encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed)
            {
                if (encounter.EncounterMedication != null)
                {
                    var em = encounter.EncounterMedication.Where(e => e.MedicationKey == PatientMedicationKey)
                        .FirstOrDefault();
                    if (em != null)
                    {
                        return em.HighRiskMedication;
                    }
                }
            }

            var hrm = HighRiskMedicationCache.GetHighRiskMedicationByRDIDAndServiceLineKey(RDID,
                encounter.ServiceLineKey);
            if (hrm != null && HighRiskMedicationCache.IsHighRiskMedicationActive(hrm,
                    encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime))
            {
                return true;
            }

            return false;
        }

        public bool IsPOCMedication(DateTime? certStartDate, DateTime? certEndDate, Encounter encounter)
        {
            // The medication goes on the POC if it is 'active' anywhere between the certStartDate and the greater of the certStartDate and TaskStartDate - (due to the DisciplineCert Window logic)
            var windowStartDate = certStartDate != null ? certStartDate.Value.Date : DateTime.Today.Date;
            var taskDate = encounter == null || encounter.EncounterOrTaskStartDateAndTime == null
                ? DateTime.Today.Date
                : encounter.EncounterOrTaskStartDateAndTime.Value.Date;
            if (certEndDate != null)
            {
                taskDate = certEndDate.Value.Date;
            }

            var windowEndDate = windowStartDate > taskDate ? windowStartDate : taskDate;
            var isPOCMedication = (MedicationStartDate.HasValue == false ||
                                   MedicationStartDate.HasValue && MedicationStartDate.Value.Date <= windowEndDate) &&
                                  (MedicationEndDate.HasValue == false || MedicationEndDate.HasValue &&
                                      MedicationEndDate.Value.Date >= windowStartDate);
            return isPOCMedication;
        }
        public string MedicationStartDateTimeDisplay
        {
            get
            {
                if (MedicationStartTime != null) return MedicationStartTimeDisplay;
                return (MedicationStartDate == null) ? null : ((DateTime)MedicationStartDate).ToShortDateString();
            }
        }
        private string MedicationStartTimeDisplay
        {
            get
            {
                string date = (MedicationStartTime == null) ? "" : Convert.ToDateTime(MedicationStartTime).ToShortDateString();
                string time = "";
                if (MedicationStartTime != null)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        time = Convert.ToDateTime(MedicationStartTime).ToString("HHmm");
                    }
                    else
                    {
                        time = Convert.ToDateTime(MedicationStartTime).ToShortTimeString();
                    }
                }

                return date + " " + time;
            }
        }
        public string MedicationEndDateTimeDisplay
        {
            get
            {
                if (MedicationEndTime != null) return MedicationEndTimeDisplay;
                return (MedicationEndDate == null) ? null : ((DateTime)MedicationEndDate).ToShortDateString();
            }
        }
        private string MedicationEndTimeDisplay
        {
            get
            {
                string date = (MedicationEndTime == null) ? "" : Convert.ToDateTime(MedicationEndTime).ToShortDateString();
                string time = "";
                if (MedicationEndTime != null)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        time = Convert.ToDateTime(MedicationEndTime).ToString("HHmm");
                    }
                    else
                    {
                        time = Convert.ToDateTime(MedicationEndTime).ToShortTimeString();
                    }
                }

                return date + " " + time;
            }
        }
        public void RefreshMedicationDateTimes()
        {
            RaisePropertyChanged("MedicationStartDateTime");
            RaisePropertyChanged("MedicationStartDateTimeDisplay");
            RaisePropertyChanged("MedicationEndDateTime");
            RaisePropertyChanged("MedicationEndDateTimeDisplay");
        }
    }
}