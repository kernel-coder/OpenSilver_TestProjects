#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Client.Core;
using Virtuoso.Common.BusinessLogic;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Cache.ICD.Extensions;
using Virtuoso.Core.Converters;
using Virtuoso.Core.Occasional;
using Virtuoso.Portable.Extensions;
using Virtuoso.Portable.Model;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AgencyOpsPatient
    {
        public override string ToString()
        {
            return FullNameWithMRN; //NOTE: this method overridden for control autoCompleteCombo
        }
    }

    public partial class TaskPatient
    {
        public override string ToString()
        {
            return FullNameWithMRN;
        }
    }

    public partial class TaskEncounter
    {
        private Form _MyForm;

        public Form MyForm
        {
            get
            {
                if (FormKey == null)
                {
                    return null;
                }

                if (_MyForm != null)
                {
                    return _MyForm;
                }

                _MyForm = DynamicFormCache.GetFormByKey((int)FormKey);
                return _MyForm;
            }
        }

        public DateTimeOffset? EncounterStartTime
        {
            get
            {
                if (EncounterStartDatePart.HasValue)
                {
                    var ret = EncounterStartDatePart;
                    if (EncounterStartTimePart.HasValue)
                        ret = ret.Value.Add(EncounterStartTimePart.Value.TimeOfDay);
                    return ret;
                }
                else
                    return null;
            }
        }
    }

    public partial class Patient
    {
        private CollectionViewSource _filteredContacts;
        private bool _IsPatientMaintenance;
        private CollectionViewSource selectedRaceValues;

        public string AdmissionMedicationScreeningBlirb
        {
            get
            {
                if (AdmissionDocumentation == null)
                {
                    return null;
                }

                var ad = AdmissionDocumentation.Where(p => p.DocumentationTypeCode == "MedScreening")
                    .OrderByDescending(p => p.CreatedDateTime).FirstOrDefault();
                return ad == null ? null : ad.AdmissionMedicationScreeningBlirb;
            }
        }


        public string MRNDescription => "MRN: " + (string.IsNullOrWhiteSpace(MRN) ? "?" : MRN);

        public string DOBDescription => "DOB: " + (BirthDate == null ? "?" : ((DateTime)BirthDate).ToShortDateString());

        public string RefillDescription => string.Format("{0} {1} {2}", FullName, MRNDescription, DOBDescription);

        public bool IsUnder18
        {
            get
            {
                if (BirthDate == null || BirthDate == null)
                {
                    return false;
                }

                var dt = (DateTime)BirthDate;
                dt = dt.AddYears(18);
                return dt > DateTime.Today ? true : false;
            }
        }

        public bool IsImmunizationZosterRequiredAndNotOnFile
        {
            get
            {
                if (IsAgeOrOlder(60) == false)
                {
                    return false;
                }

                if (PatientImmunization == null)
                {
                    return true;
                }

                var zosterOnFile = PatientImmunization.Where(pi => pi.Inactive == false && pi.IsImmunizationZoster)
                    .Any();
                return zosterOnFile ? false : true;
            }
        }

        public string FullNameWithMiddleInitial
        {
            get
            {
                var name = string.Format("{0}{1} {2}", FirstName,
                        !string.IsNullOrWhiteSpace(MiddleName) ? " " + MiddleName.Substring(0, 1) + "." : "", LastName)
                    .Trim();
                if (name == "," || name == "")
                {
                    name = " ";
                }

                return name;
            }
        }

        public string GenderCode => CodeLookupCache.GetCodeFromKey(Gender);

        public string GenderDescription => CodeLookupCache.GetDescriptionFromCode("Gender", GenderCode);

        public string ReligionCode => CodeLookupCache.GetCodeFromKey(Religion);

        public string ReligionDescription
        {
            get
            {
                var religionDescription = CodeLookupCache.GetDescriptionFromCode("Religion", ReligionCode) + "";
                if (religionDescription.Length == 0)
                {
                    religionDescription = "N/A";
                }

                return religionDescription;
            }
        }

        public string EmailDescription
        {
            get
            {
                var emailDescription = EmailAddress + "";
                if (emailDescription.Length == 0)
                {
                    emailDescription = "N/A";
                }

                return emailDescription;
            }
        }


        public string SSNLast4
        {
            get
            {
                var last4 = SSN + "";
                if (last4.Length == 0)
                {
                    return "N/A";
                }

                return "****-**-" + Right(SSN, 4);
            }
        }


        public DateTime? CurrentReferDateTime
        {
            get
            {
                DateTime? currentReferDateTime = null;

                if (Admission != null)
                {
                    var admission = Admission.OrderByDescending(a => a.ReferDateTime).FirstOrDefault();

                    if (admission != null)
                    {
                        currentReferDateTime = admission.ReferDateTime;
                    }
                }

                return currentReferDateTime;
            }
        }

        public DateTime? CurrentSOCDate
        {
            get
            {
                DateTime? currentSOC = null;

                if (Admission != null)
                {
                    var admission = Admission.OrderByDescending(a => a.ReferDateTime).FirstOrDefault();

                    if (admission != null)
                    {
                        currentSOC = admission.SOCDate;
                    }
                }

                return currentSOC;
            }
        }

        public int? MostRecentSigningPhysician
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                var aList = Admission.OrderByDescending(a => a.ReferDateTime).ToList();
                if (aList == null)
                {
                    return null;
                }
                
                foreach (var a in aList)
                {
                    var ap = a.AdmissionPhysician
                        .Where(p => p.Inactive == false)
                        .Where(p => p.Signing)
                        .Where(p =>
                            p.SigningEffectiveFromDate.HasValue &&
                            p.SigningEffectiveFromDate.Value.Date <= DateTime.Now.Date &&
                            (p.SigningEffectiveThruDate.HasValue == false || p.SigningEffectiveThruDate.HasValue &&
                                p.SigningEffectiveThruDate.Value.Date > DateTime.Now.Date)
                        ).FirstOrDefault();
                    if (ap != null)
                    {
                        return ap.PhysicianKey;
                    }
                }

                return null;
            }
        }

        public bool PatientNameWithMRNChanged { get; set; }

        public bool ShowEmployer
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Occupation))
                {
                    return false;
                }

                if ((Occupation.Equals("Clerical", StringComparison.OrdinalIgnoreCase) ||
                     Occupation.Equals("Laborer", StringComparison.OrdinalIgnoreCase) ||
                     Occupation.Equals("Professional", StringComparison.OrdinalIgnoreCase)) == false)
                {
                    return false;
                }

                return true;
            }
        }

        public string EditFullName
        {
            get
            {
                if (FullNameInformal != " ")
                {
                    return FullNameInformal;
                }

                return IsNew ? "New Patient" : "Edit Patient";
            }
        }

        public bool IsSpanish
        {
            get
            {
                var languageCode = CodeLookupCache.GetCodeFromKey(Language);
                return string.IsNullOrWhiteSpace(languageCode) ? false :
                    languageCode.Trim().ToLower().StartsWith("span") ? true :
                    languageCode.Trim().ToLower().StartsWith("espa") ? true : false;
            }
        }

        public bool ShowExpectedBirthDate
        {
            get
            {
                if (BirthDate == null)
                {
                    return false;
                }

                var date = (DateTime)BirthDate;
                if (ExpectedDueDate != null)
                {
                    return true;
                }

                if (BirthDate.HasValue && BirthDate.Value.Date > DateTime.Today.Date)
                {
                    return false;
                }

                return date.AddDays(366) > DateTime.Today
                    ? true
                    : false; // Show ExpectedBirthDate if patient is less than a year old
            }
        }

        public bool ShowBMI
        {
            get
            {
                if (BirthDate == null)
                {
                    return true;
                }

                var date = (DateTime)BirthDate;
                return date.AddDays(730) > DateTime.Today ? false : true; // Show BMI if patient is over two years old
            }
        }

        public bool ShowBMIChildLink
        {
            get
            {
                if (BirthDate == null)
                {
                    return false;
                }

                var date = (DateTime)BirthDate;
                return date.AddDays(7300) > DateTime.Today ? true : false; // Show BMI if patient is a child or teanager
            }
        }

        public string PatientAge
        {
            get
            {
                if (BirthDate == null)
                {
                    return null;
                }

                var birthdate = (DateTime)BirthDate;
                var endDate = DeathDate == null ? DateTime.Today : (DateTime)DeathDate;
                if (birthdate > endDate)
                {
                    return null;
                }

                string patientAge = null;
                var diff = endDate.Subtract(birthdate);
                if (diff.Days < 14)
                {
                    patientAge = diff.Days + " day"; // < 14 days return n days
                }
                else if (diff.Days < 56)
                {
                    patientAge = diff.Days / 7 + " week"; // < 8 weeks return n weeks
                }
                else
                {
                    var years = endDate.Year - birthdate.Year - 1 + (endDate.Month > birthdate.Month ||
                                                                     endDate.Month == birthdate.Month &&
                                                                     endDate.Day >= birthdate.Day
                        ? 1
                        : 0);
                    if (years < 2)
                    {
                        patientAge = diff.Days / 30 + " month"; // < 24 months return n months
                    }
                    else
                    {
                        patientAge = years + " year"; // else return n years; 
                    }
                }

                return patientAge + (patientAge.StartsWith("1 ") ? "" : "s") + " old" +
                       (DeathDate == null ? "" : " at death");
            }
        }

        public bool IsPatientMaintenance
        {
            get { return _IsPatientMaintenance; }
            set
            {
                _IsPatientMaintenance = value;
                RaisePropertyChanged("IsPatientMaintenance");
                if (_IsPatientMaintenance)
                {
                    SetCurrentAddress(); // So we initially display the current address on the patient address tab of PatientMaintenance
                }
            }
        }

        public PatientPhone PrimaryPhone
        {
            get
            {
                if (PatientPhone == null)
                {
                    return null;
                }

                var p =
                    PatientPhone
                        .Where(i => i.Inactive == false && i.HistoryKey == null &&
                                    (i.EffectiveFromDate.HasValue == false || i.EffectiveFromDate <= DateTime.UtcNow) &&
                                    i.Main).FirstOrDefault();
                if (p == null)
                {
                    p =
                        PatientPhone
                            .Where(i => i.Inactive == false && i.HistoryKey == null &&
                                        (i.EffectiveFromDate.HasValue == false ||
                                         i.EffectiveFromDate <= DateTime.UtcNow)).OrderBy(i => i.PhoneTypePriority)
                            .FirstOrDefault();
                }

                return p;
            }
        }

        public List<PatientContact> ActiveContacts
        {
            get
            {
                if (PatientContact == null)
                {
                    return null;
                }

                List<PatientContact> p = null;
                try
                {
                    p = PatientContact
                        .Where(i => i.Inactive == false && i.HistoryKey == null &&
                                    (i.EffectiveFromDate.HasValue == false || i.EffectiveFromDate <= DateTime.UtcNow))
                        .OrderBy(i => i.LastName).ThenBy(i => i.FirstName).ToList();
                }
                catch
                {
                }

                if (p != null)
                {
                    if (p.Any() == false)
                    {
                        p = null;
                    }
                }

                return p;
            }
        }

        public List<PatientAdvancedDirective> ActiveDNRs
        {
            get
            {
                var retList = new List<PatientAdvancedDirective>();
                var tempList = ActiveAdvancedDirectivesOfType("dnr");
                if (tempList != null)
                {
                    foreach (var pad in tempList)
                        retList.Add(pad);
                }

                tempList = ActiveAdvancedDirectivesOfType("communitydnr");
                if (tempList != null)
                {
                    foreach (var pad in tempList)
                        retList.Add(pad);
                }

                if (retList.Any() == false)
                {
                    return null;
                }

                return retList;
            }
        }

        public List<PatientMessage> ActivePatientMessages
        {
            get
            {
                if (PatientMessage == null)
                {
                    return null;
                }

                List<PatientMessage> pmList = null;
                try
                {
                    pmList = PatientMessage.Where(p => p.HistoryKey == null && p.Inactive == false)
                        .OrderByDescending(p => p.MessageDateTime).ToList();
                }
                catch
                {
                }

                if (pmList == null || pmList.Any() == false)
                {
                    return null;
                }

                return pmList;
            }
        }

        public string PatientMessages
        {
            get
            {
                string patientMessages = null;
                var pmList = ActivePatientMessages;
                if (pmList == null)
                {
                    return null;
                }

                foreach (var pm in pmList)
                {
                    patientMessages = patientMessages + (patientMessages == null ? "" : "<LineBreak /><LineBreak />");
                    patientMessages = patientMessages + "<Bold>   " + pm.MessageEditedText + "</Bold><LineBreak />";

                    // NOTE: When sending user entered data to vRichTextArea, encode entity values as XAML.
                    //       Altenatively, split up formatted text from unformatted text with different controls,
                    //       maybe vRichTextArea for formatted text and TextBlock for user entered non-XML/non-XAML text.
                    var message_text = XamlHelper.EncodeAsXaml(pm.MessageText);

                    patientMessages = patientMessages + message_text.Replace("\r", "<LineBreak />");
                }

                return patientMessages;
            }
        }

        public List<PatientPharmacy> FilteredPharmacies => PatientPharmacy.ToList();

        public ICollectionView SelectedRaceValues
        {
            get
            {
                try
                {
                    if (selectedRaceValues == null)
                    {
                        selectedRaceValues = new CollectionViewSource();
                        selectedRaceValues.Source = CodeLookupCache.GetCodeLookupsFromType("RACES");
                        selectedRaceValues.Filter += SelectedRaceFilter;
                    }

                    return selectedRaceValues.View;
                }
                catch
                {
                    throw new Exception("ICollectionView SelectedRaceValues");
                }
            }
        }

        public bool PrimaryRaceVisible
        {
            get
            {
                string[] delimeters = { " - " };
                var numRaces = 0;
                if (!string.IsNullOrEmpty(Races))
                {
                    try
                    {
                        numRaces = Races.Split(delimeters, StringSplitOptions.RemoveEmptyEntries).Count();
                    }
                    catch (Exception e)
                    {
                        throw new Exception(e.Message);
                    }
                }

                if (numRaces == 0)
                {
                    PrimaryRaceKey = null;
                }
                else if (numRaces == 1) // default Primary race
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        PrimaryRaceKey = CodeLookupCache.GetKeyFromCodeDescription("RACES", Races);
                    });
                }

                return numRaces > 1;
            }
        }

        public ICollectionView FilteredContacts
        {
            get
            {
                try
                {
                    if (_filteredContacts == null)
                    {
                        _filteredContacts = new CollectionViewSource();
                        _filteredContacts.Source = PatientContact;
                        _filteredContacts.SortDescriptions.Add(new SortDescription("LastName",
                            ListSortDirection.Ascending));
                        _filteredContacts.SortDescriptions.Add(new SortDescription("FirstName",
                            ListSortDirection.Ascending));
                        _filteredContacts.Filter += _filteredContacts_Filter;
                    }

                    return _filteredContacts.View;
                }
                catch
                {
                    throw new Exception("ICollectionView FilteredContacts");
                }
            }
        }

        public PatientPortalAccessDetail LastPatientPortalAccessDetail => PatientPortalAccessDetail;

        public bool ShowModuleAccessedDate
        {
            get
            {
                var value = LastPatientPortalAccessDetail;
                return value != null && value.LastAccessDateTime.HasValue;
            }
        }

        public string ModuleAccessedDate
        {
            get
            {
                var value = LastPatientPortalAccessDetail;
                if (value != null && value.LastAccessDateTime.HasValue)
                {
                    return value.LastAccessDateTime.Value.ToString("MM/dd/yyyy");
                }

                return string.Empty;
            }
        }

        public string ModuleAccessName
        {
            get
            {
                var value = LastPatientPortalAccessDetail;
                if (value != null)
                {
                    return value.ModuleName;
                }

                return string.Empty;
            }
        }

        public bool IsModuleAccessPurchased
        {
            get
            {
                var value = LastPatientPortalAccessDetail;
                if (value != null)
                {
                    return value.HasTenantModuleAccess;
                }

                return false;
            }
        }

        public bool ShowModuleInviteDate
        {
            get
            {
                var value = LastPatientPortalAccessDetail;
                return value != null && value.InviteSentDateTime.HasValue;
            }
        }

        public string ModuleInviteDate
        {
            get
            {
                var value = LastPatientPortalAccessDetail;
                if (value != null && value.InviteSentDateTime.HasValue)
                {
                    return value.InviteSentDateTime.Value.ToString("MM/dd/yyyy");
                }

                return string.Empty;
            }
        }

        public string SendInviteText
        {
            get
            {
                var value = LastPatientPortalAccessDetail;
                if (value != null && value.InviteSentDateTime.HasValue)
                {
                    return value.InviteSentDateTime.Value.ToString("MM/dd/yyyy");
                }

                return string.Empty;
            }
        }

        public bool ShowModuleConfirmDate
        {
            get
            {
                var value = LastPatientPortalAccessDetail;
                return value != null && value.EmailConfirmedDateTime.HasValue;
            }
        }

        public string ModuleConfirmDate
        {
            get
            {
                var value = LastPatientPortalAccessDetail;
                if (value != null && value.EmailConfirmedDateTime.HasValue)
                {
                    return value.EmailConfirmedDateTime.Value.ToString("MM/dd/yyyy");
                }

                return string.Empty;
            }
        }

        partial void OnPatientServedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (PatientServed == true)
            {
                return;
            }

            EnrolledInVA = false;
            RaisePropertyChanged("EnrolledInVA");
        }

        public static string FormatCityStateZip(string City, int? StateCodeLookupKey, string ZipCode)
        {
            return FormatHelper.FormatCityStateZip(City, CodeLookupCache.GetCodeFromKey(StateCodeLookupKey), ZipCode);
        }

        public void SetCurrentAddress()
        {
            if (PatientAddress == null)
            {
                return;
            }

            foreach (var a in PatientAddress) a.CurrentAddress = false;
            var pa = GetEffectiveAddressForDate(DateTime.Today.Date);
            if (pa != null)
            {
                pa.CurrentAddress = true;
            }
        }

        public PatientAddress GetEffectiveAddressForDate(DateTime dt)
        {
            PatientAddress pa = null;
            if (PatientAddress == null)
            {
                return pa;
            }

            pa = PatientAddress.Where(p => p.IsTypeTemporary && !p.Inactive && HistoryKey == null
                                           && (p.EffectiveFromDate == null || p.EffectiveFromDate.Value.Date <= dt.Date)
                                           && (p.EffectiveThruDate == null ||
                                               p.EffectiveThruDate.Value.Date >= dt.Date))
                .FirstOrDefault();
            if (pa != null)
            {
                return pa;
            }

            pa = PatientAddress.Where(p => p.IsTypeFacility && !p.Inactive && HistoryKey == null
                                           && (p.EffectiveFromDate == null || p.EffectiveFromDate.Value.Date <= dt.Date)
                                           && (p.EffectiveThruDate == null ||
                                               p.EffectiveThruDate.Value.Date >= dt.Date))
                .FirstOrDefault();
            if (pa != null)
            {
                return pa;
            }

            pa = PatientAddress.Where(p => p.IsTypeHome && !p.Inactive && HistoryKey == null
                                           && (p.EffectiveFromDate == null || p.EffectiveFromDate.Value.Date <= dt.Date)
                                           && (p.EffectiveThruDate == null ||
                                               p.EffectiveThruDate.Value.Date >= dt.Date))
                .FirstOrDefault();
            return pa;
        }

        public List<PatientAddress> GetActivePatientAddresses(DateTime dt)
        {
            if (PatientAddress == null)
            {
                return null;
            }

            var list = PatientAddress.Where(p => !p.Inactive
                                                 && (p.EffectiveFromDate == null ||
                                                     p.EffectiveFromDate.Value.Date <= dt.Date)
                                                 && (p.EffectiveThruDate == null ||
                                                     p.EffectiveThruDate.Value.Date >= dt.Date))
                .OrderBy(p => p.AddressTypePriority).ToList();
            return list == null || list.Any() == false ? null : list;
        }

        public void AddNewGenderExpression()
        {
            var p = new PatientGenderExpression();
            p.EffectiveFromDate = DateTime.Today.Date;
            PatientGenderExpression.Add(p);
        }

        public void RemoveGenderExpression(PatientGenderExpression p)
        {
            PatientGenderExpression.Remove(p);
        }

        public bool IsAgeOrOlder(int age)
        {
            if (BirthDate == null || BirthDate == null)
            {
                return false;
            }

            var dt = ((DateTime)BirthDate).Date;
            dt = dt.AddYears(age);
            return dt <= DateTime.Today.Date ? true : false;
        }

        public async Task<List<CachedICDCode>> PatientICDCodes(Admission admission, string ICDMode,
            ICDViewVersionType ICDVersion)
        {
            if (admission == null)
            {
                return null;
            }

            var diagnosis = ICDMode == "PCS" ? false : true;
            var version = ICDVersion == ICDViewVersionType.ICD9 ? 10 : ICDVersion == ICDViewVersionType.ICD10 ? 9 : 0;
            var _ActiveDiagnosis = AdmissionDiagnosis.Where(a =>
                    a.Diagnosis == diagnosis && a.Version != version && a.Superceded == false &&
                    a.RemovedDate == null &&
                    a.Code != "000.00" &&
                    (a.DiagnosisEndDate == null || ((DateTime)a.DiagnosisEndDate).Date >= DateTime.Today.Date))
                .OrderBy(a => a.Code).ToList();
            if (_ActiveDiagnosis == null || _ActiveDiagnosis.Any() == false)
            {
                return null;
            }

            var _PatientICDCodes = new List<CachedICDCode>();
            foreach (var ad in _ActiveDiagnosis)
                // if diagnosis is not active in this admission - add it if its not already in the list
                if (IsCodeInList(_PatientICDCodes, ad) == false)
                {
                    if (admission.AdmissionDiagnosis
                            .Where(p => p.ICDCodeKey == ad.ICDCodeKey && !p.DiagnosisEndDate.HasValue).Any() == false)
                    {
                        CachedICDCode cic = null;
                        if (ad.Version == 9 && ad.Diagnosis)
                        {
                            cic = await ICDCM9Cache.Current.GetICDCodeByCode(ad.Code);
                        }
                        else if (ad.Version == 10 && ad.Diagnosis)
                        {
                            cic = await ICDCM10Cache.Current.GetICDCodeByCode(ad.Code);
                        }
                        else if (ad.Version == 9 && ad.Diagnosis == false)
                        {
                            cic = await ICDPCS9Cache.Current.GetICDCodeByCode(ad.Code);
                        }
                        else
                        {
                            cic = await ICDPCS10Cache.Current.GetICDCodeByCode(ad.Code);
                        }

                        if (cic != null)
                        {
                            _PatientICDCodes.Add(cic);
                        }
                    }
                }

            if (_PatientICDCodes.Any() == false)
            {
                return null;
            }

            return _PatientICDCodes;
        }

        private bool IsCodeInList(List<CachedICDCode> list, AdmissionDiagnosis ad)
        {
            if (list == null || list.Any() == false)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(ad.Code))
            {
                return true;
            }

            foreach (var i in list)
                if (i.Code.ToLower() == ad.Code.ToLower() && i.Version == ad.Version)
                {
                    return true;
                }

            return false;
        }

        public static string Right(string value, int length)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length <= length ? value : value.Substring(value.Length - length);
        }

        partial void OnLastNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullName");
            RaisePropertyChanged("EditFullName");
            RaisePropertyChanged("FullNameInformal");
            RaisePropertyChanged("FormattedName");
            PatientNameWithMRNChanged = true;
        }

        public void TriggerGenderChanges()
        {
            foreach (var item in PatientGenderExpression) item.TriggeredChange();

            RaisePropertyChanged("PatientGenderExpression");
        }

        public void PatientGenderExpressionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            TriggerGenderChanges();
        }

        partial void OnFirstNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullName");
            RaisePropertyChanged("EditFullName");
            RaisePropertyChanged("FullNameInformal");
            RaisePropertyChanged("FormattedName");
            PatientNameWithMRNChanged = true;
        }

        partial void OnMRNChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            PatientNameWithMRNChanged = true;
        }

        partial void OnOccupationChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ShowEmployer == false)
            {
                Employer = null;
            }

            RaisePropertyChanged("ShowEmployer");
        }

        partial void OnNickNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullName");
            RaisePropertyChanged("EditFullName");
            RaisePropertyChanged("FullNameInformal");
            RaisePropertyChanged("FormattedName");
            PatientNameWithMRNChanged = true;
        }

        partial void OnMiddleNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullName");
            RaisePropertyChanged("EditFullName");
            RaisePropertyChanged("FullNameInformal");
            RaisePropertyChanged("FormattedName");
            PatientNameWithMRNChanged = true;
        }

        partial void OnSuffixChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FormattedName");
            PatientNameWithMRNChanged = true;
        }

        partial void OnBirthDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PatientAge");
            RaisePropertyChanged("ShowExpectedBirthDate");
            RaisePropertyChanged("ShowBMI");
            RaisePropertyChanged("ShowBMIChildLink");
        }

        partial void OnDeathDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PatientAge");
            Messenger.Default.Send(this, "OasisSetupPatientDefaults");
        }

        public PatientAddress MainAddress(DateTime? AsOfDate)
        {
            var asOfDate = AsOfDate == null
                ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date
                : ((DateTime)AsOfDate).Date;
            if (PatientAddress == null)
            {
                return null;
            }

            var p =
                PatientAddress
                    .Where(i => i.Inactive == false && i.HistoryKey == null &&
                                (i.EffectiveFromDate.HasValue == false ||
                                 ((DateTime)i.EffectiveFromDate).Date <= asOfDate) &&
                                (i.EffectiveThruDate.HasValue == false ||
                                 ((DateTime)i.EffectiveThruDate).Date >= asOfDate) && i.TypeDescription == "Home")
                    .FirstOrDefault();
            if (p == null)
            {
                p =
                    PatientAddress
                        .Where(i => i.Inactive == false && i.HistoryKey == null &&
                                    (i.EffectiveFromDate.HasValue == false ||
                                     ((DateTime)i.EffectiveFromDate).Date <= asOfDate) &&
                                    (i.EffectiveThruDate.HasValue == false ||
                                     ((DateTime)i.EffectiveThruDate).Date >= asOfDate))
                        .OrderBy(i => i.PatientAddressKey).FirstOrDefault();
            }

            return p;
        }

        public List<PatientAdvancedDirective> ActiveAdvancedDirectivesOfType(string pType)
        {
            if (PatientAdvancedDirective == null)
            {
                return null;
            }

            var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
            var padList = PatientAdvancedDirective.Where
                (a => a.HistoryKey == null && a.Inactive == false &&
                      (a.EffectiveDate == null || a.EffectiveDate != null && a.EffectiveDate.Value.Date <= today) &&
                      (a.ExpirationDate == null || a.ExpirationDate != null && a.ExpirationDate.Value.Date >= today))
                .ToList();
            if (padList.Any() == false)
            {
                return null;
            }

            // if asking pType is null return all active ADs
            if (string.IsNullOrWhiteSpace(pType))
            {
                return padList;
            }

            // return active ADs of the type passed
            padList = padList.Where(a => a.AdvancedDirectiveTypeCode.ToLower() == pType.ToLower()).ToList();
            if (padList.Any() == false)
            {
                return null;
            }

            return padList;
        }

        partial void OnGenderChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("GenderCode");
        }

        private void SelectedRaceFilter(object sender, FilterEventArgs e)
        {
            var cl = e.Item as CodeLookup;
            var result = false;
            if (cl != null && Races != null)
            {
                result = Races.Contains(cl.CodeDescription);
            }

            e.Accepted = result;
        }

        private void FilterRaceValues()
        {
            try
            {
                if (!string.IsNullOrEmpty(Races))
                {
                    var selKey = PrimaryRaceKey;
                    SelectedRaceValues.Refresh();
                    if (selKey.HasValue)
                    {
                        if (SelectedRaceValues.Contains(CodeLookupCache.GetCodeLookupFromKey((int)selKey)))
                        {
                            PrimaryRaceKey = selKey;
                        }
                    }

                    Deployment.Current.Dispatcher.BeginInvoke(() => { RaisePropertyChanged("PrimaryRaceVisible"); });
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        partial void OnRacesChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            FilterRaceValues();
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                RaisePropertyChanged("SelectedRaceValues");
                RaisePropertyChanged("PrimaryRaceVisible");
            });
        }

        public List<ServiceLine> GetFilteredServiceLineItemsSource(int? ServiceLineKeyParm, bool IncludeEmpty = false)
        {
            if (Admission == null)
            {
                return null;
            }

            var actAdmissions = Admission.Where(a => a.AdmissionStatusCode == "A" || a.AdmissionStatusCode == "R"
                    || a.AdmissionStatusCode == "H" || a.AdmissionStatusCode == "T" || a.AdmissionStatusCode == "M")
                .Select(key => key.ServiceLineKey).ToList();

            return ServiceLineCache.GetActiveUserServiceLinePlusMe(ServiceLineKeyParm, IncludeEmpty).Where(sl =>
                actAdmissions.Any() == false
                || ServiceLineKeyParm > 0 && ServiceLineKeyParm == sl.ServiceLineKey || sl.IsNew ||
                !actAdmissions.Contains(sl.ServiceLineKey)).ToList();
        }

        private void _filteredContacts_Filter(object sender, FilterEventArgs e)
        {
            var pc = e.Item as PatientContact;
            if (pc == null)
            {
                e.Accepted = false;
            }
            else
            {
                e.Accepted = pc.Inactive || pc.HistoryKey != null ? false : true;
            }
        }

        public void Cleanup()
        {
            if (_filteredContacts != null)
            {
                _filteredContacts.Filter -= _filteredContacts_Filter;
                _filteredContacts.Source = null;
                _filteredContacts = null;
            }

            if (selectedRaceValues != null)
            {
                selectedRaceValues.Filter -= SelectedRaceFilter;
                selectedRaceValues.Source = null;
                selectedRaceValues = null;
            }
        }

        public void TriggerPatientPortalChanges()
        {
            RaisePropertyChanged("ShowModuleAccessedDate");
            RaisePropertyChanged("ModuleAccessedDate");
            RaisePropertyChanged("ModuleAccessName");
            RaisePropertyChanged("IsModuleAccessPurchased");
            RaisePropertyChanged("ShowModuleInviteDate");
            RaisePropertyChanged("ModuleInviteDate");
            RaisePropertyChanged("SendInviteText");
            RaisePropertyChanged("ShowModuleConfirmDate");
            RaisePropertyChanged("ModuleConfirmDate");
        }

        #region "Escort/Guard/Translator display logic"

        public bool RequiresGuard
        {
            get
            {
                /* Patient Requires a guard when the following are all true (in order of Temp 1st, Home 2nd)
				   a) Patient is from the Home or Temp address record that is in effect as of the system date 
				   b) Patient's Zip+4  from the Home or Temp address record that is in effect as of the system date is in the GuardArea Table with an Inactive bit = 0.  
				*/
                foreach (var address in PatientAddress.Where(t => t.IsTypeTemporary || t.IsTypeHome))
                    if (PatientAddressIsValid(address) && PatientAddressInGuardArea(address))
                    {
                        return true;
                    }

                return false;
            }
        }

        public bool CensusTractHighRisk
        {
            get
            {
                /* Patient Requires an Escort when the following are all true (in order of Temp 1st, Home 2nd)
					// a) a Patient's CensusTract is in the CensusTractMapping Table 
					// b)  Patient is in a HighRisk, Mapped CensusTract area
				 */
                foreach (var address in PatientAddress.Where(t => t.IsTypeTemporary || t.IsTypeHome))
                    if (PatientAddressIsValid(address) && PatientAddressInHighRiskCensusTract(address))
                    {
                        return true;
                    }

                return false;
            }
        }

        public bool RequiresEscort
        {
            get
            {
                // Escort is required when the following are all true
                // a) a Patient's CensusTract is in the CensusTractMapping Table 
                // b) CensusTracts.HighRisk = 1 
                // c) Patient address is NOT in the GuardArea Table with an Inactive bit = 0.                 
                var requiresEscort = false;

                if (CensusTractHighRisk && !RequiresGuard)
                {
                    requiresEscort = true;
                }

                return requiresEscort;
            }
        }

        private class ZipCodeDetails
        {
            public ZipCodeDetails()
            {
                ZipCode = "";
                Plus4 = "";
            }

            public string ZipCode { get; set; }
            public string Plus4 { get; set; }
        }

        private ZipCodeDetails PatientZipCode(PatientAddress patientAddress)
        {
            ZipCodeDetails zipCodeDetails = null;


            if (PatientAddressIsValid(patientAddress))
            {
                var zipCodePlus4 = patientAddress.ZipCode;

                var zipArray = zipCodePlus4.Split('-');
                if (zipArray != null)
                {
                    zipCodeDetails = new ZipCodeDetails();
                    if (zipArray.Length >= 1)
                    {
                        zipCodeDetails.ZipCode = zipArray[0];
                    }

                    if (zipArray.Length >= 2)
                    {
                        zipCodeDetails.Plus4 = zipArray[1];
                    }
                }
            }

            return zipCodeDetails;
        }

        private bool PatientAddressIsValid(PatientAddress patientAddress)
        {
            // Simplified logic and handled DateTime? correctly.
            var thruDate = patientAddress.EffectiveThruDate;
            var fromDate = patientAddress.EffectiveFromDate;
            var currentDate = DateTime.Now.Date;

            if (fromDate.HasValue == false)
            {
                return false;
            }

            var patientAddressIsValid = !patientAddress.Inactive
                                        && patientAddress.HistoryKey == null
                                        && !(fromDate.Value.Date > currentDate)
                                        && (!thruDate.HasValue || thruDate.Value.Date >= currentDate);

            return patientAddressIsValid;
        }

        private bool PatientAddressInGuardArea(PatientAddress patientAddress)
        {
            var addressInGuardArea = false;
            var zipCodeDetails = PatientZipCode(patientAddress);

            if (zipCodeDetails != null)
            {
                var guardAreaTemp = GuardAreaCache.GetGuardAreaByZipCodeParts(zipCodeDetails.ZipCode,
                    zipCodeDetails.Plus4);
                if (guardAreaTemp != null)
                {
                    if (guardAreaTemp.Any())
                    {
                        addressInGuardArea = true;
                    }
                }
            }


            return addressInGuardArea;
        }

        private bool PatientAddressInHighRiskCensusTract(PatientAddress patientAddress)
        {
            //Returns whether PatientAddress is in a CensusTract area
            var addressInCensusTract = false;

            if (!string.IsNullOrEmpty(patientAddress.CensusTract))
            {
                var censusTractTemp = CensusTractCache.GetCensusTracts(patientAddress.CensusTract);
                if (censusTractTemp != null)
                {
                    if (censusTractTemp.Where(c => c.HighRisk && c.Inactive == false).Any())
                    {
                        addressInCensusTract = true;
                    }
                }
            }

            return addressInCensusTract;
        }

        #endregion
    }

    public partial class PatientAddress
    {
        private bool _CurrentAddress;

        public List<Facility> FacilityList => FacilityCache.GetActiveFacilitiesPlusMe(FacilityKey, true);

        public List<FacilityBranch> FacilityBranchList
        {
            get
            {
                return FacilityCache.GetActiveBranchesAndMe(FacilityBranchKey).Where(f => f.FacilityKey == FacilityKey)
                    .ToList();
            }
        }

        public bool IsAddressVerificationMelissa
        {
            get
            {
                if (isTypeFacilityHomeTemporary == false)
                {
                    return false;
                }

                var hasVerification = TenantSettingsCache.Current.TenantSetting.AddressVerification;
                if (hasVerification)
                {
                    var serviceName = TenantSettingsCache.Current.TenantSetting.AddressVerificationName;
                    return string.IsNullOrWhiteSpace(serviceName) ? false : true;
                }

                return false;
            }
        }

        public string VerificationBlirb
        {
            get
            {
                if (IsAddressVerificationGeoCode == false)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(ReturnStatus))
                {
                    return null;
                }

                if (ReturnStatus.ToLower().Equals("verified") == false &&
                    ReturnStatus.ToLower().Equals("geocode") == false)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(VerificationStatus))
                {
                    return null;
                }

                if (VerificationStatus.ToLower().Equals("verified") == false)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(VerificationCodes))
                {
                    return null;
                }

                // only valid blirb is a verified address. When we have one the precision is in VerificationCodes
                return "(Precision of " + VerificationCodes + ")";
            }
        }

        public bool IsAddressVerificationGeoCode
        {
            get
            {
                if (IsAddressVerificationMelissa)
                {
                    return false;
                }

                if (isTypeFacilityHomeTemporary == false)
                {
                    return false;
                }

                var purchasedCrescendoConnect = TenantSettingsCache.Current.TenantSetting.PurchasedCrescendoConnect;
                var purchasedCrescendoConnectGPS =
                    TenantSettingsCache.Current.TenantSetting.PurchasedCrescendoConnectGPS;
                return purchasedCrescendoConnect && purchasedCrescendoConnectGPS ? true : false;
            }
        }

        private bool isTypeFacilityHomeTemporary
        {
            get
            {
                var code = Type == null || Type == 0 ? null : CodeLookupCache.GetCodeFromKey(Type);
                if (string.IsNullOrWhiteSpace(code))
                {
                    return false;
                }

                if (code.Equals("Home", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (code.Equals("Facility", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (code.Equals("Temporary", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }
        }

        public DateTime? ProxyEffectiveFromDate
        {
            get { return EffectiveFromDate; }
            set
            {
                EffectiveFromDate = value;
                RaisePropertyChanged("ProxyEffectiveFromDate");
            }
        }

        public DateTime? ProxyEffectiveThruDate
        {
            get { return EffectiveThruDate; }
            set
            {
                EffectiveThruDate = value;
                RaisePropertyChanged("ProxyEffectiveThruDate");
            }
        }

        public bool CanEditAddress
        {
            get
            {
                var canEdit = true;
                var facType = CodeLookupCache.GetCodeFromKey(Type);

                if (!string.IsNullOrEmpty(facType)
                    && facType.ToLower() == "facility"
                   )
                {
                    canEdit = false;
                }

                return canEdit;
            }
        }

        public bool IsFacility => FacilityKey != null && FacilityKey > 0;

        public string TypeDescription => CodeLookupCache.GetCodeFromKey(Type);

        public string TypeDescriptionWithCurrent => TypeDescription + (CurrentAddress ? " (Current)" : "");

        public string CityStateZip => FormatHelper.FormatCityStateZip(City, StateCodeCode, ZipCode);

        public string RoomAptUnitWithPrefix
        {
            get
            {
                if (IsFacility && RoomAptUnit != null && RoomAptUnit.Length > 0)
                {
                    return "Rm/Apt: " + RoomAptUnit;
                }

                return string.Empty;
            }
        }

        public string Address
        {
            get
            {
                var address = string.Empty;
                var CR = char.ToString('\r');
                if (IsTypeFacility)
                {
                    var f = FacilityCache.GetFacilityFromKey(FacilityKey);
                    if (f != null && string.IsNullOrWhiteSpace(f.Name) == false)
                    {
                        address = f.Name;
                    }

                    var fb = FacilityCache.GetFacilityBranchFromKey(FacilityBranchKey);
                    if (fb != null && string.IsNullOrWhiteSpace(fb.BranchName) == false)
                    {
                        address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + fb.BranchName;
                    }

                    if (!string.IsNullOrWhiteSpace(RoomAptUnitWithPrefix))
                    {
                        address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + RoomAptUnitWithPrefix;
                    }
                }

                if (!string.IsNullOrWhiteSpace(Address1))
                {
                    address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + Address1;
                }

                if (!string.IsNullOrWhiteSpace(Address2))
                {
                    address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + Address2;
                }

                if (!string.IsNullOrWhiteSpace(CityStateZip))
                {
                    address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + CityStateZip;
                }

                return address;
            }
        }

        public string StateCodeCode => CodeLookupCache.GetCodeFromKey(StateCode);

        public bool IsTypeBilling => Type == CodeLookupCache.GetKeyFromCode("PATADDRESS", "Billing");
        public bool IsTypeFacility => Type == CodeLookupCache.GetKeyFromCode("PATADDRESS", "Facility");
        public bool IsTypeHome => Type == CodeLookupCache.GetKeyFromCode("PATADDRESS", "Home");
        public bool IsTypeTemporary => Type == CodeLookupCache.GetKeyFromCode("PATADDRESS", "Temporary");

        public int AddressTypePriority
        {
            get
            {
                if (IsTypeTemporary)
                {
                    return 1;
                }

                if (IsTypeFacility)
                {
                    return 2;
                }

                if (IsTypeHome)
                {
                    return 3;
                }

                if (IsTypeBilling)
                {
                    return 4;
                }

                return 5;
            }
        }

        public string FacilityPhoneNumber
        {
            get
            {
                if (IsTypeFacility == false)
                {
                    return null;
                }

                var fb = FacilityCache.GetFacilityBranchFromKey(FacilityBranchKey);
                if (fb != null && string.IsNullOrWhiteSpace(fb.PhoneNumber) == false)
                {
                    return fb.PhoneNumber.Trim();
                }

                var f = FacilityCache.GetFacilityFromKey(FacilityKey);
                if (f != null && string.IsNullOrWhiteSpace(f.Number) == false)
                {
                    return f.Number.Trim();
                }

                return null;
            }
        }

        public string FacilityExtension
        {
            get
            {
                if (IsTypeFacility == false)
                {
                    return null;
                }

                if (FacilityPhoneNumber == null)
                {
                    return null;
                }

                var fb = FacilityCache.GetFacilityBranchFromKey(FacilityBranchKey);
                if (fb != null && string.IsNullOrWhiteSpace(fb.PhoneNumber) == false &&
                    string.IsNullOrWhiteSpace(fb.PhoneExtension) == false)
                {
                    return fb.PhoneExtension.Trim();
                }

                return null;
            }
        }

        public string FacilityPhoneNumberFormatted
        {
            get
            {
                if (IsTypeFacility == false || FacilityPhoneNumber == null)
                {
                    return null;
                }

                return PhoneConvert(FacilityPhoneNumber) + (FacilityExtension == null ? "" : " x" + FacilityExtension);
            }
        }

        public string FacilityFax
        {
            get
            {
                if (IsTypeFacility == false)
                {
                    return null;
                }

                var fb = FacilityCache.GetFacilityBranchFromKey(FacilityBranchKey);
                if (fb != null && string.IsNullOrWhiteSpace(fb.Fax) == false)
                {
                    return fb.Fax.Trim();
                }

                var f = FacilityCache.GetFacilityFromKey(FacilityKey);
                if (f != null && string.IsNullOrWhiteSpace(f.Fax) == false)
                {
                    return f.Fax.Trim();
                }

                return null;
            }
        }

        public bool CurrentAddress
        {
            get { return _CurrentAddress; }
            set
            {
                _CurrentAddress = value;
                RaisePropertyChanged("CurrentAddress");
                RaisePropertyChanged("TypeDescriptionWithCurrent");
            }
        }

        private void ResetVerification()
        {
            if (!VerificationActive())
            {
                return;
            }

            CensusTract = null;
            CensusBlock = null;
            CountyFIPS = null;
            Latitude = null;
            Longitude = null;
            ReturnStatus = "Unverified";
            VerificationStatus = "Unverified";
            if (IsAddressVerificationGeoCode)
            {
                VerificationCodes = null;
                VerifiedBy = null;
                VerifiedDate = null;
            }
        }

        partial void OnFacilityBranchKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            LoadAddressFromFacility();
            ResetVerification();
        }

        partial void OnFacilityKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            FacilityBranchKey = null;
            LoadAddressFromFacility();
            ResetVerification();
        }

        private void LoadAddressFromFacility()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (FacilityBranchKey.HasValue)
            {
                var fb = FacilityCache.GetFacilityBranchFromKey(FacilityBranchKey);
                Address1 = fb == null ? Address1 : fb.Address1;
                Address2 = fb == null ? Address1 : fb.Address2;
                CBSAHomeHealth = fb == null ? CBSAHomeHealth : fb.CBSAHomeHealth;
                CBSAHospice = fb == null ? CBSAHospice : fb.CBSAHospice;
                City = fb == null ? City : fb.City;
                County = fb == null ? County : fb.County;
                StateCode = fb == null ? StateCode : fb.StateCode;
                ZipCode = fb == null ? ZipCode : fb.ZipCode;
            }
            else
            {
                var f = FacilityCache.GetFacilityFromKey(FacilityKey);
                Address1 = f == null ? Address1 : f.Address1;
                Address2 = f == null ? Address2 : f.Address2;
                CBSAHomeHealth = f == null ? CBSAHomeHealth : f.CBSAHomeHealth;
                CBSAHospice = f == null ? CBSAHospice : f.CBSAHospice;
                City = f == null ? City : f.City;
                County = f == null ? County : f.County;
                StateCode = f == null ? StateCode : f.StateCode;
                ZipCode = f == null ? ZipCode : f.ZipCode;
            }

            RaisePropertyChanged("CanEditAddress");
            RaisePropertyChanged("FacilityBranchList");
        }

        partial void OnTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (FacilityKey != null)
            {
                var code = CodeLookupCache.GetCodeFromKey(Type);
                if (code == null)
                {
                    FacilityKey = null;
                }
                else if (code.Equals("facility", StringComparison.OrdinalIgnoreCase) == false)
                {
                    FacilityKey = null;
                }
            }

            RaisePropertyChanged("CanEditAddress");
            RaisePropertyChanged("TypeDescription");
            RaisePropertyChanged("TypeDescriptionWithCurrent");
            RaisePropertyChanged("IsAddressVerificationMelissa");
            RaisePropertyChanged("IsAddressVerificationGeoCode");
            ResetVerification();
        }

        partial void OnVerificationStatusChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("VerificationBlirb");
        }

        partial void OnReturnStatusChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("VerificationBlirb");
        }

        partial void OnVerificationCodesChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("VerificationBlirb");
        }

        partial void OnRoomAptUnitChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            ResetVerification();
        }

        partial void OnAddress1Changed()
        {
            if (IsDeserializing)
            {
                return;
            }

            ResetVerification();
        }

        partial void OnAddress2Changed()
        {
            if (IsDeserializing)
            {
                return;
            }

            ResetVerification();
        }

        partial void OnCityChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
            ResetVerification();
        }

        partial void OnStateCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
            RaisePropertyChanged("StateCodeCode");
            ResetVerification();
        }
        
        public void SetVerifiedBy()
        {
            VerifiedBy = WebContext.Current.User.MemberID;
        }

        public bool VerificationActive()
        {
            if (IsAddressVerificationMelissa)
            {
                return true;
            }

            if (IsAddressVerificationGeoCode)
            {
                return true;
            }

            return false;
        }

        partial void OnZipCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
            ResetVerification();
        }

        private string PhoneConvert(string number)
        {
            var pc = new PhoneConverter();
            var phoneObject = pc.Convert(number, null, null, null);
            if (phoneObject != null)
            {
                if (string.IsNullOrWhiteSpace(phoneObject.ToString()) == false)
                {
                    return phoneObject.ToString();
                }
            }

            return null;
        }
    }

    public partial class PatientContact
    {
        private bool _IsDynamicForm;

        public string FullNameInformalWithRelationship
        {
            get
            {
                var r = RelationshipCodeDescription;
                return string.IsNullOrWhiteSpace(r) ? FullNameInformal : FullNameInformal + "   (" + r + ")";
            }
        }

        public string LastNameWithRelationship
        {
            get
            {
                var r = RelationshipCodeDescription;
                return string.IsNullOrWhiteSpace(r) ? LastName : LastName + "   (" + r + ")";
            }
        }

        public string EmergencyContactText
        {
            get
            {
                string emergencyContactText = null;
                if (EmergencyContact)
                {
                    emergencyContactText = "Emergency Contact";
                }

                return emergencyContactText;
            }
        }

        public List<Facility> FacilityList => FacilityCache.GetActiveFacilitiesPlusMe(FacilityKey, true);

        public List<FacilityBranch> FacilityBranchList
        {
            get
            {
                return FacilityCache.GetActiveBranchesAndMe(FacilityBranchKey).Where(f => f.FacilityKey == FacilityKey)
                    .ToList();
            }
        }

        public bool IsDynamicForm
        {
            get { return _IsDynamicForm; }
            set
            {
                _IsDynamicForm = value;
                RaisePropertyChanged("IsDynamicForm");
            }
        }

        public bool CanEditContactType
        {
            get
            {
                if (IsDynamicForm)
                {
                    return false;
                }

                return IsNew;
            }
        }

        public bool CanEditAddress
        {
            get
            {
                var f = FacilityCache.GetFacilityFromKey(FacilityKey);
                return f == null ? true : false;
            }
        }

        public string FullName => FormatHelper.FormatName(LastName, FirstName, MiddleInitial);

        public string FullNameWithSuffix =>
            string.IsNullOrWhiteSpace(Suffix) ? FullName : FullName + " " + Suffix.Trim();

        public string RelationshipCode => CodeLookupCache.GetCodeFromKey(Relationship);

        public string RelationshipCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(Relationship);

        public string PreferredPhone
        {
            get
            {
                string preferredPhone = null;
                var code = PreferredPhoneCode;
                if (string.IsNullOrWhiteSpace(code) == false)
                {
                    code = code.Trim().ToLower();
                    if (code == "home" && string.IsNullOrWhiteSpace(HomePhoneNumber) == false)
                    {
                        preferredPhone = PhoneConvert(HomePhoneNumber);
                    }
                    else if (code == "cell" && string.IsNullOrWhiteSpace(CellPhoneNumber) == false)
                    {
                        preferredPhone = PhoneConvert(CellPhoneNumber);
                    }
                    else if (code == "work" && string.IsNullOrWhiteSpace(WorkPhoneNumber) == false)
                    {
                        preferredPhone = PhoneConvert(WorkPhoneNumber);
                        if (string.IsNullOrWhiteSpace(preferredPhone) == false)
                        {
                            preferredPhone = preferredPhone + (string.IsNullOrWhiteSpace(WorkPhoneExtension) == false
                                ? " x" + WorkPhoneExtension
                                : "");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(preferredPhone) == false)
                    {
                        return preferredPhone;
                    }
                }

                if (string.IsNullOrWhiteSpace(HomePhoneNumber) == false)
                {
                    preferredPhone = PhoneConvert(HomePhoneNumber);
                    if (string.IsNullOrWhiteSpace(preferredPhone) == false)
                    {
                        return preferredPhone;
                    }
                }

                if (string.IsNullOrWhiteSpace(CellPhoneNumber) == false)
                {
                    preferredPhone = PhoneConvert(CellPhoneNumber);
                    if (string.IsNullOrWhiteSpace(preferredPhone) == false)
                    {
                        return preferredPhone;
                    }
                }

                if (string.IsNullOrWhiteSpace(WorkPhoneNumber) == false)
                {
                    preferredPhone = PhoneConvert(WorkPhoneNumber);
                    if (string.IsNullOrWhiteSpace(preferredPhone) == false)
                    {
                        preferredPhone = preferredPhone + (string.IsNullOrWhiteSpace(WorkPhoneExtension) == false
                            ? " x" + WorkPhoneExtension
                            : "");
                    }

                    if (string.IsNullOrWhiteSpace(preferredPhone) == false)
                    {
                        return preferredPhone;
                    }
                }

                return null;
            }
        }

        public string PreferredPhoneCode => CodeLookupCache.GetCodeFromKey(PreferredPhoneKey);

        public string RelationshipDescription => CodeLookupCache.GetCodeDescriptionFromKey(Relationship);

        public string RoleDescription => CodeLookupCache.GetCodeDescriptionFromKey(Role);

        public string LanguageDescription => CodeLookupCache.GetCodeDescriptionFromKey(Language);

        public string ReligionDescription => CodeLookupCache.GetCodeDescriptionFromKey(Religion);

        public string StateCodeCode => CodeLookupCache.GetCodeDescriptionFromKey(StateCode);

        public string CityStateZip => FormatHelper.FormatCityStateZip(City, StateCodeCode, ZipCode);

        public string FullNameInformalWithRelationShip
        {
            get
            {
                var r = ((string.IsNullOrWhiteSpace(FirstName) ? " " : FirstName.Trim()) + " " +
                         (string.IsNullOrWhiteSpace(LastName) ? " " : LastName)).Trim();
                if (string.IsNullOrWhiteSpace(RelationshipDescription) == false)
                {
                    r = r + " - " + RelationshipDescription;
                }

                return string.IsNullOrWhiteSpace(r) ? "   " : r;
            }
        }

        partial void OnFacilityKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            var f = FacilityCache.GetFacilityFromKey(FacilityKey);
            FirstName = f == null ? null : f.FirstName;
            MiddleInitial = null;
            LastName = f == null ? null : f.LastName;
            Suffix = null;
            Address1 = f == null ? null : f.Address1;
            Address2 = f == null ? null : f.Address2;
            City = f == null ? null : f.City;
            StateCode = f == null ? null : f.StateCode;
            ZipCode = f == null ? null : f.ZipCode;
            WorkPhoneExtension = f == null ? null : f.PhoneExtension;
            WorkPhoneNumber = f == null ? null : f.Number;

            RaisePropertyChanged("CanEditAddress");
            RaisePropertyChanged("FacilityBranchList");
        }

        partial void OnFacilityBranchKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FacilityBranchList");
        }

        partial void OnContactTypeKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (FacilityKey != null)
            {
                var code = CodeLookupCache.GetCodeFromKey(ContactTypeKey);
                if (code == null)
                {
                    FacilityKey = null;
                }
                else if (code.Equals("facility", StringComparison.OrdinalIgnoreCase) == false)
                {
                    FacilityKey = null;
                }
            }

            RaisePropertyChanged("CanEditAddress");
        }

        partial void OnEmergencyContactChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EmergencyContactText");
        }

        partial void OnLastNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullNameInformal");
        }

        partial void OnFirstNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FullNameInformal");
        }

        private string PhoneConvert(string number)
        {
            var pc = new PhoneConverter();
            var phoneObject = pc.Convert(number, null, null, null);
            if (phoneObject != null)
            {
                if (string.IsNullOrWhiteSpace(phoneObject.ToString()) == false)
                {
                    return phoneObject.ToString();
                }
            }

            return null;
        }

        partial void OnPreferredPhoneKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PreferredPhoneCode");
        }

        public bool ValidateFacilityBranch()
        {
            if (FacilityKey == null)
            {
                return true;
            }

            var f = FacilityCache.GetFacilityFromKey(FacilityKey);
            if (f == null)
            {
                return false;
            }

            if (f.FacilityBranch == null || f.FacilityBranch.Any() == false)
            {
                return true;
            }

            if (FacilityBranchKey == null)
            {
                return false;
            }

            return true;
        }
    }

    public partial class PatientAllergy
    {
        private Encounter _currentEncounter;

        // AllergyStatus used for sorting and UI: 0=current, 1=future, 2=discontinued
        public int AllergyStatus
        {
            get
            {
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

                if (AllergyStartDate == null || AllergyStartDate == DateTime.MinValue)
                {
                    return 0;
                }

                if (AllergyStartDate.Value.Date > EncounterDate)
                {
                    return 1;
                }

                if (AllergyEndDate == null || AllergyEndDate == DateTime.MinValue)
                {
                    return 0;
                }

                if (AllergyEndDate.Value.Date < EncounterDate)
                {
                    return 2;
                }

                return 0;
            }
        }

        public Encounter CurrentEncounter
        {
            get { return _currentEncounter; }
            set
            {
                _currentEncounter = value;
                RaisePropertyChanged("AllergyStatus");
                RaisePropertyChanged("CanFullEdit");
                RaisePropertyChanged("CanDelete");
            }
        }

        public bool IsPlanOfCare => CurrentEncounter != null && CurrentEncounter.FormKey != null &&
                                    DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey).IsPlanOfCare;

        public bool IsOrderEntry => CurrentEncounter != null && CurrentEncounter.FormKey != null &&
                                    DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey).IsOrderEntry;

        public bool IsTeamMeeting => CurrentEncounter != null && CurrentEncounter.FormKey != null &&
                                     DynamicFormCache.GetFormByKey((int)CurrentEncounter.FormKey).IsTeamMeeting;

        public bool IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry => IsPlanOfCare || IsTeamMeeting || IsOrderEntry;

        public override bool CanFullEdit
        {
            get
            {
                // Plan of Care can edit everything 01/31/2013
                if (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry)
                {
                    return true;
                }

                if (CurrentEncounter == null)
                {
                    // Not part of an encounter (regular patient maint) - can fully edit only new items
                    if (IsNew
                        || PatientAllergyKey <= 0
                        || EncounterAllergy == null
                        || EncounterAllergy.Any() == false
                       )
                    {
                        return true;
                    }

                    return false;
                }

                // Part of an encounter- can edit new items and any item that was added during this encounter
                if (IsNew || PatientAllergyKey <= 0)
                {
                    return true;
                }

                return AddedFromEncounterKey == CurrentEncounter.EncounterKey ? true : false;
            }
        }

        public override bool CanDelete
        {
            get
            {
                // cannot delete new items in dynamic form until they are Oked/saved the first time
                if (CurrentEncounter != null && (IsNew || PatientAllergyKey <= 0))
                {
                    return IsOKed;
                }

                // cannot delete new items in Patient Maint
                if (CurrentEncounter == null && (IsNew || PatientAllergyKey <= 0))
                {
                    return false;
                }

                if (CurrentEncounter == null)
                    // Not part of an encounter (regular patient maint) - can fully edit/delete only items not yet pulled into an encounter
                {
                    return CanFullEdit;
                }

                // Part of an encounter- can delete items that was added during this encounter
                return AddedFromEncounterKey == CurrentEncounter.EncounterKey ? true : false;
            }
        }

        public string ReactionText
        {
            get
            {
                string reactionText = null;
                if (string.IsNullOrWhiteSpace(Reaction) == false)
                {
                    reactionText = "Reaction " + Reaction;
                }

                if (LastReactionDate != null)
                {
                    if (reactionText != null)
                    {
                        reactionText = reactionText + ",  ";
                    }

                    reactionText = reactionText + "last reaction on " +
                                   ((DateTime)LastReactionDate).ToShortDateString();
                }

                return reactionText;
            }
        }

        public PatientAllergy CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newAllergy = (PatientAllergy)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newAllergy);
            if (newAllergy.HistoryKey == null)
            {
                newAllergy.HistoryKey = PatientAllergyKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newAllergy;
        }

        partial void OnAllergyStartDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("AllergyStatus");
        }

        partial void OnAllergyEndDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("AllergyStatus");
        }

        partial void OnReactionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ReactionText");
        }

        partial void OnLastReactionDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ReactionText");
        }

        partial void OnPatientAllergyKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CanFullEdit");
            RaisePropertyChanged("CanDelete");
        }

        partial void OnAddedFromEncounterKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CanFullEdit");
            RaisePropertyChanged("CanDelete");
        }
    }

    public partial class PatientGenderExpression
    {
        public bool Inactive => InactiveDate.HasValue;

        public string EffectiveThruDate
        {
            get
            {
                var result = "-";
                if (Patient != null)
                {
                    var nextDate = Patient.PatientGenderExpression
                        .Where(w => !w.InactiveDate.HasValue &&
                                    w.PatientGenderExpressionKey != PatientGenderExpressionKey &&
                                    w.EffectiveFromDate > EffectiveFromDate).OrderBy(o => o.EffectiveFromDate)
                        .FirstOrDefault();
                    if (nextDate != null)
                    {
                        result = nextDate.EffectiveFromDate.AddDays(-1).ToShortDateString();
                    }
                }

                return result;
            }
        }

        public DateTime EffectiveFromChanger
        {
            get { return EffectiveFromDate; }
            set
            {
                EffectiveFromDate = value;
                if (IsDeserializing)
                {
                    return;
                }

                if (Patient != null)
                {
                    Patient.PatientGenderExpressionChanged();
                }
            }
        }

        partial void OnGenderExpressionKeyChanged()
        {
            if (Patient != null)
            {
                Patient.PatientGenderExpressionChanged();
            }
        }

        partial void OnEffectiveFromDateChanged()
        {
            if (Patient != null)
            {
                Patient.PatientGenderExpressionChanged();
            }
        }

        public void TriggeredChange()
        {
            RaisePropertyChanged("EffectiveThruDate");
        }

        public void UndoChanges()
        {
            RejectChanges();
        }
    }

    public partial class PatientMedicationAdministration
    {
        private Encounter _Encounter;

        public string AdministrationSiteDescription => CodeLookupCache.GetCodeDescriptionFromKey(AdministrationSite);

        public string AdministeredTypeDescription => CodeLookupCache.GetCodeDescriptionFromKey(AdministeredType);

        public string AdministrationDateTimeFormatted
        {
            get
            {
                var date = AdministrationDatePart == null
                    ? ""
                    : Convert.ToDateTime(AdministrationDatePart).ToShortDateString();
                var time = "";
                if (AdministrationTimePart != null)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        time = Convert.ToDateTime(AdministrationTimePart).ToString("HHmm");
                    }
                    else
                    {
                        time = Convert.ToDateTime(AdministrationTimePart).ToShortTimeString();
                    }
                }

                return date + " " + time;
            }
        }

        public DateTime AdministrationDateTimeSort => AdministrationDatePart == null || AdministrationTimePart == null
            ? DateTime.MinValue
            : new DateTime(AdministrationDatePart.Value.Year, AdministrationDatePart.Value.Month,
                AdministrationDatePart.Value.Day, AdministrationTimePart.Value.Hour,
                AdministrationTimePart.Value.Minute, 0);

        public string FommattedAdministrationBy => UserCache.Current.GetFormalNameFromUserId(AdministrationBy);

        public string AdministrationMedications
        {
            get
            {
                if (PatientMedicationAdministrationMed == null)
                {
                    return null;
                }

                string medDescList = null;
                foreach (var pmam in PatientMedicationAdministrationMed)
                    if (pmam.PatientMedication != null &&
                        string.IsNullOrWhiteSpace(pmam.PatientMedication.MedicationDescription) == false)
                    {
                        medDescList = medDescList + (medDescList == null ? "" : ";  ") +
                                      pmam.PatientMedication.MedicationDescription;
                    }

                return medDescList;
            }
        }

        public string AdministrationEditOrView
        {
            get
            {
                //can edit if not protected and in this encounter
                if (Encounter == null)
                {
                    return "View";
                }

                if (_Encounter == null)
                {
                    return "View";
                }

                if (Encounter.EncounterKey <= 0)
                {
                    return "Edit";
                }

                if (Encounter.EncounterKey == _Encounter.EncounterKey)
                {
                    return "Edit";
                }

                return "View";
            }
        }

        public bool IsForEncounter(Encounter e)
        {
            _Encounter = e;
            if (e == null)
            {
                return false;
            }

            if (e.EncounterKey > 0)
            {
                return e.EncounterKey == EncounterKey ? true : false;
            }

            return e == Encounter ? true : false;
        }

        partial void OnAdministrationDatePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("AdministrationDateTimeFormatted");
            RaisePropertyChanged("AdministrationDateTimeSort");
        }

        partial void OnAdministrationTimePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("AdministrationDateTimeFormatted");
            RaisePropertyChanged("AdministrationDateTimeSort");
        }

        partial void OnAdministrationByChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FommattedAdministrationBy");
        }
    }

    public partial class PatientMedicationReconcile
    {
        private Encounter _Encounter;

        public string ReconcileDateTimeFormatted
        {
            get
            {
                var date = ReconcileDatePart == null ? "" : Convert.ToDateTime(ReconcileDatePart).ToShortDateString();
                var time = "";
                if (ReconcileTimePart != null)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        time = Convert.ToDateTime(ReconcileTimePart).ToString("HHmm");
                    }
                    else
                    {
                        time = Convert.ToDateTime(ReconcileTimePart).ToShortTimeString();
                    }
                }

                return date + " " + time;
            }
        }

        public DateTime ReconcileDateTimeSort => ReconcileDatePart == null || ReconcileTimePart == null
            ? DateTime.MinValue
            : new DateTime(ReconcileDatePart.Value.Year, ReconcileDatePart.Value.Month, ReconcileDatePart.Value.Day,
                ReconcileTimePart.Value.Hour, ReconcileTimePart.Value.Minute, 0);

        public string FommattedReconcileBy => UserCache.Current.GetFormalNameFromUserId(ReconcileBy);

        public string ReconcileMedications
        {
            get
            {
                if (PatientMedicationReconcileMed == null)
                {
                    return null;
                }

                string medDescList = null;
                foreach (var pmrm in PatientMedicationReconcileMed)
                    if (pmrm.PatientMedication != null &&
                        string.IsNullOrWhiteSpace(pmrm.PatientMedication.MedicationDescription) == false)
                    {
                        medDescList = medDescList + (medDescList == null ? "" : ";  ") +
                                      pmrm.PatientMedication.MedicationDescription;
                    }

                return medDescList;
            }
        }

        public string ReconcileEditOrView
        {
            get
            {
                //can edit if not protected and in this encounter
                if (Encounter == null)
                {
                    return "View";
                }

                if (_Encounter == null)
                {
                    return "View";
                }

                if (Encounter.EncounterKey <= 0)
                {
                    return "Edit";
                }

                if (Encounter.EncounterKey == _Encounter.EncounterKey)
                {
                    return "Edit";
                }

                return "View";
            }
        }

        public bool IsForEncounter(Encounter e)
        {
            _Encounter = e;
            if (e == null)
            {
                return false;
            }

            if (e.EncounterKey > 0)
            {
                return e.EncounterKey == EncounterKey ? true : false;
            }

            return e == Encounter ? true : false;
        }

        partial void OnReconcileDatePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ReconcileDateTimeFormatted");
            RaisePropertyChanged("ReconcileDateTimeSort");
        }

        partial void OnReconcileTimePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ReconcileDateTimeFormatted");
            RaisePropertyChanged("ReconcileDateTimeSort");
        }

        partial void OnReconcileByChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FommattedReconcileBy");
        }
    }

    public partial class PatientMedicationTeaching
    {
        private Encounter _Encounter;

        public string TeachingDateTimeFormatted
        {
            get
            {
                var date = TeachingDatePart == null ? "" : Convert.ToDateTime(TeachingDatePart).ToShortDateString();
                var time = "";
                if (TeachingTimePart != null)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        time = Convert.ToDateTime(TeachingTimePart).ToString("HHmm");
                    }
                    else
                    {
                        time = Convert.ToDateTime(TeachingTimePart).ToShortTimeString();
                    }
                }

                return date + " " + time;
            }
        }

        public DateTime TeachingDateTimeSort => TeachingDatePart == null || TeachingTimePart == null
            ? DateTime.MinValue
            : new DateTime(TeachingDatePart.Value.Year, TeachingDatePart.Value.Month, TeachingDatePart.Value.Day,
                TeachingTimePart.Value.Hour, TeachingTimePart.Value.Minute, 0);

        public string FommattedTeachingBy => UserCache.Current.GetFormalNameFromUserId(TeachingBy);

        public string TeachingMedications
        {
            get
            {
                if (PatientMedicationTeachingMed == null)
                {
                    return null;
                }

                string medDescList = null;
                foreach (var pmtm in PatientMedicationTeachingMed)
                    if (pmtm.PatientMedication != null &&
                        string.IsNullOrWhiteSpace(pmtm.PatientMedication.MedicationDescription) == false)
                    {
                        medDescList = medDescList + (medDescList == null ? "" : ";  ") +
                                      pmtm.PatientMedication.MedicationDescription;
                    }

                return medDescList;
            }
        }

        public string TeachingEditOrView
        {
            get
            {
                //can edit if not protected and in this encounter
                if (Encounter == null)
                {
                    return "View";
                }

                if (_Encounter == null)
                {
                    return "View";
                }

                if (Encounter.EncounterKey <= 0)
                {
                    return "Edit";
                }

                if (Encounter.EncounterKey == _Encounter.EncounterKey)
                {
                    return "Edit";
                }

                return "View";
            }
        }

        public bool IsForEncounter(Encounter e)
        {
            _Encounter = e;
            if (e == null)
            {
                return false;
            }

            if (e.EncounterKey > 0)
            {
                return e.EncounterKey == EncounterKey ? true : false;
            }

            return e == Encounter ? true : false;
        }

        partial void OnTeachingDatePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TeachingDateTimeFormatted");
            RaisePropertyChanged("TeachingDateTimeSort");
        }

        partial void OnTeachingTimePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TeachingDateTimeFormatted");
            RaisePropertyChanged("TeachingDateTimeSort");
        }

        partial void OnTeachingByChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FommattedTeachingBy");
        }
    }

    public partial class PatientMedicationManagement
    {
        private Encounter _Encounter;

        public string ManagementDateTimeFormatted
        {
            get
            {
                var date = ManagementDatePart == null ? "" : Convert.ToDateTime(ManagementDatePart).ToShortDateString();
                var time = "";
                if (ManagementTimePart != null)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        time = Convert.ToDateTime(ManagementTimePart).ToString("HHmm");
                    }
                    else
                    {
                        time = Convert.ToDateTime(ManagementTimePart).ToShortTimeString();
                    }
                }

                return date + " " + time;
            }
        }

        public DateTime ManagementDateTimeSort => ManagementDatePart == null || ManagementTimePart == null
            ? DateTime.MinValue
            : new DateTime(ManagementDatePart.Value.Year, ManagementDatePart.Value.Month, ManagementDatePart.Value.Day,
                ManagementTimePart.Value.Hour, ManagementTimePart.Value.Minute, 0);

        public string FormattedManagementBy => UserCache.Current.GetFormalNameFromUserId(ManagementBy);

        public string ManagementMedications
        {
            get
            {
                if (PatientMedicationManagementMed == null)
                {
                    return null;
                }

                string medDescList = null;
                foreach (var pmmm in PatientMedicationManagementMed)
                    if (pmmm.PatientMedication != null &&
                        string.IsNullOrWhiteSpace(pmmm.PatientMedication.MedicationDescription) == false)
                    {
                        medDescList = medDescList + (medDescList == null ? "" : ";  ") +
                                      pmmm.PatientMedication.MedicationDescription;
                    }

                return medDescList;
            }
        }

        public string ManagementEditOrView
        {
            get
            {
                //can edit if not protected and in this encounter
                if (Encounter == null)
                {
                    return "View";
                }

                if (_Encounter == null)
                {
                    return "View";
                }

                if (Encounter.EncounterKey <= 0)
                {
                    return "Edit";
                }

                if (Encounter.EncounterKey == _Encounter.EncounterKey)
                {
                    return "Edit";
                }

                return "View";
            }
        }

        public bool IsForEncounter(Encounter e)
        {
            _Encounter = e;
            if (e == null)
            {
                return false;
            }

            if (e.EncounterKey > 0)
            {
                return e.EncounterKey == EncounterKey ? true : false;
            }

            return e == Encounter ? true : false;
        }

        partial void OnManagementDatePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ManagementDateTimeFormatted");
            RaisePropertyChanged("ManagementDateTimeSort");
        }

        partial void OnManagementTimePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ManagementDateTimeFormatted");
            RaisePropertyChanged("ManagementDateTimeSort");
        }

        partial void OnManagementByChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FommattedManagementBy");
        }
    }

    public partial class PatientMessage
    {
        private bool _CanFullEdit = true;
        private Patient _CurrentPatient;
        private bool _Expand;

        public Patient CurrentPatient
        {
            get { return _CurrentPatient; }
            set
            {
                _CurrentPatient = value;
                RaisePropertyChanged("CurrentPatient");
            }
        }

        public bool Expand
        {
            get { return _Expand; }
            set
            {
                _Expand = value;
                RaisePropertyChanged("Expand");
                RaisePropertyChanged("ThumbNailText");
                RaisePropertyChanged("TextTrimming");
                RaisePropertyChanged("TextWrapping");
            }
        }

        public string InactiveText
        {
            get
            {
                if (InactiveDateTimeFormatted == null)
                {
                    return null;
                }

                return string.Format("Message inactivated on {0}  by {1}", InactiveDateTimeFormatted,
                    UserCache.Current.GetFormalNameFromUserId(InactiveBy));
            }
        }

        public string InactiveDateTimeFormatted
        {
            get
            {
                if (InactiveDateTime == null)
                {
                    return null;
                }

                var dateTime = Convert.ToDateTime(((DateTimeOffset)InactiveDateTime).DateTime).ToString("MM/dd/yyyy");
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    dateTime = dateTime + " " + Convert.ToDateTime(((DateTimeOffset)InactiveDateTime).DateTime)
                        .ToString("HHmm");
                }
                else
                {
                    dateTime = dateTime + " " + Convert.ToDateTime(((DateTimeOffset)InactiveDateTime).DateTime)
                        .ToShortTimeString();
                }

                if (string.IsNullOrWhiteSpace(dateTime))
                {
                    return null;
                }

                return dateTime;
            }
        }

        public TextTrimming TextTrimming => Expand ? TextTrimming.None : TextTrimming.WordEllipsis;

        public TextWrapping TextWrapping => Expand ? TextWrapping.Wrap : TextWrapping.NoWrap;

        public string ThumbNailText
        {
            get
            {
                if (Expand)
                {
                    return MessageText;
                }

                var text = MessageText;
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                string[] CR = { char.ToString('\r') };
                var splitText = text.Split(CR, StringSplitOptions.RemoveEmptyEntries);
                if (splitText.Length == 0)
                {
                    return MessageText;
                }

                foreach (var textLine in splitText)
                    if (string.IsNullOrWhiteSpace(textLine) == false)
                    {
                        return textLine;
                    }

                return MessageText;
            }
        }

        public string MessageLastEditedText
        {
            get
            {
                if (MessageEditedDateTimeFormatted == null)
                {
                    return null;
                }

                return string.Format("Message last edited on {0}  by {1}", MessageEditedDateTimeFormatted,
                    UserCache.Current.GetFormalNameFromUserId(MessageBy));
            }
        }

        public string MessageEditedText
        {
            get
            {
                if (MessageEditedDateTimeFormatted == null)
                {
                    return null;
                }

                return string.Format("Message on {0}  by {1}", MessageEditedDateTimeFormatted,
                    UserCache.Current.GetFormalNameFromUserId(MessageBy));
            }
        }

        public string MessageEditedDateTimeFormatted
        {
            get
            {
                if (MessageDateTime == null)
                {
                    return null;
                }

                var dateTime = Convert.ToDateTime(((DateTimeOffset)MessageDateTime).DateTime).ToString("MM/dd/yyyy");
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    dateTime = dateTime + " " +
                               Convert.ToDateTime(((DateTimeOffset)MessageDateTime).DateTime).ToString("HHmm");
                }
                else
                {
                    dateTime = dateTime + " " + Convert.ToDateTime(((DateTimeOffset)MessageDateTime).DateTime)
                        .ToShortTimeString();
                }

                if (string.IsNullOrWhiteSpace(dateTime))
                {
                    return null;
                }

                return dateTime;
            }
        }

        public bool CanEdit => PatientMessageKey <= 0 ? true : false;

        public new bool CanFullEdit
        {
            get { return _CanFullEdit; }
            set
            {
                _CanFullEdit = value;
                RaisePropertyChanged("CanFullEdit");
            }
        }

        partial void OnCreated()
        {
        }

        partial void OnInactiveChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            InactiveBy = Inactive == false ? (Guid?)null : WebContext.Current.User.MemberID;
            InactiveDateTime = Inactive == false
                ? (DateTimeOffset?)null
                : DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            RaisePropertyChanged("InactiveText");
            RaisePropertyChanged("InactiveDateTimeFormatted");
        }

        partial void OnMessageTextChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ThumbNailText");
        }

        partial void OnMessageDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MessageLastEditedText");
            RaisePropertyChanged("MessageEditedDateTimeFormatted");
        }

        partial void OnMessageByChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MessageLastEditedText");
        }
    }

    public partial class PatientPharmacy
    {
        public Vendor PatientPharmacyVendor => VendorCache.GetVendorFromKey(VendorKey);

        public string NameAndPhone
        {
            get
            {
                if (VendorKey == null || PatientPharmacyVendor == null)
                {
                    return "  ";
                }

                var p = PatientPharmacyVendor.Number ?? string.Empty;
                if (p != string.Empty && p.All(char.IsDigit))
                {
                    var y = double.Parse(p);
                    if (p.Length == 10)
                    {
                        p = string.Format("{0:###-###-####}", y);
                    }

                    if (p.Length == 7)
                    {
                        p = string.Format("{0:###-####}", y);
                    }
                }

                if (p != string.Empty)
                {
                    p = ", phone: " + p;
                }

                return (PatientPharmacyVendor.VendorName ?? "--Name Not Found--") + p;
            }
        }

        public Patient CurrentPatient { get; set; }

        partial void OnVendorKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PatientPharmacyVendor");
            RaisePropertyChanged("NameAndPhone");
        }
    }

    public partial class PatientLab
    {
        private Encounter _currentEncounter;
        private bool _IsSelected;

        public string AddedFromThisEncounterParagraphText
        {
            get
            {
                if (CurrentEncounter == null)
                {
                    return null;
                }

                if ((IsNew || AddedFromEncounterKey == CurrentEncounter.EncounterKey) == false)
                {
                    return null;
                }

                return CurrentEncounter.EncounterIsOrderEntry ? "Added in this order" : "Added in this encounter";
            }
        }

        public bool IsSelected
        {
            get { return _IsSelected; }
            set
            {
                _IsSelected = value;
                RaisePropertyChanged("IsSelected");
            }
        }

        public Encounter CurrentEncounter
        {
            get { return _currentEncounter; }
            set
            {
                _currentEncounter = value;
                RaisePropertyChanged("CanFullEdit");
            }
        }

        public override bool CanFullEdit
        {
            get
            {
                if (CurrentEncounter == null)
                {
                    // Not part of an encounter (regular patient maint) - can fully edit only new items
                    if (IsNew || PatientLabKey <= 0)
                    {
                        return true;
                    }

                    return false;
                }

                // Part of an encounter- can edit new items and any item that was added during this encounter
                if (IsNew || PatientLabKey <= 0)
                {
                    return true;
                }

                return CurrentEncounter.EncounterKey == AddedFromEncounterKey ? true : false;
            }
        }

        public override bool CanDelete
        {
            get
            {
                if (CurrentEncounter == null)
                {
                    // Not part of an encounter (regular patient maint) - can delete only new items
                    if (IsNew || PatientLabKey <= 0)
                    {
                        return true;
                    }

                    return false;
                }

                // Part of an encounter- can delete new items and any item that was added during this encounter
                if (IsNew || PatientLabKey <= 0)
                {
                    return IsOKed;
                }

                return CurrentEncounter.EncounterKey == AddedFromEncounterKey ? true : false;
            }
        }

        public PatientLab CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newPL = (PatientLab)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newPL); //newpain.AdmissionLevelOfCareKey = 0;
            if (newPL.HistoryKey == null)
            {
                newPL.HistoryKey = PatientLabKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newPL;
        }

        partial void OnPatientLabKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CanFullEdit");
            RaisePropertyChanged("CanDelete");
        }

        partial void OnAddedFromEncounterKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CanFullEdit");
            RaisePropertyChanged("CanDelete");
            RaisePropertyChanged("AddedFromThisEncounterParagraphText");
        }
    }

    public partial class PatientImmunization
    {
        public string ImmunizationCode => CodeLookupCache.GetCodeFromKey(Immunization);

        public string ImmunizationCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(Immunization);

        public bool IsImmunizationZoster
        {
            get
            {
                var iCode = ImmunizationCode;
                if (string.IsNullOrWhiteSpace(iCode))
                {
                    return false;
                }

                return iCode.Trim().ToLower() == "zoster" ? true : false;
            }
        }

        public string ImmunizedByCode => CodeLookupCache.GetCodeFromKey(ImmunizedBy);

        public string ImmunizedByCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(ImmunizedBy);

        public bool ReasonForDecliningVisibility
        {
            get
            {
                if (!ImmunizedBy.HasValue)
                {
                    ReasonForDeclining = null;
                    return false;
                }

                if (ImmunizedBy.HasValue)
                {
                    var t = CodeLookupCache.GetCodeFromKey(ImmunizedBy);
                    if (t != null && t.ToLower().Contains("prov"))
                    {
                        ReasonForDeclining = null;
                        return false;
                    }
                }

                return !Contraindications.HasValue && !DateReceived.HasValue;
            }
        }

        public bool ReasonForDecliningCommentsVisibility
        {
            get
            {
                if (!ReasonForDeclining.HasValue)
                {
                    return false;
                }

                if (ImmunizedBy.HasValue)
                {
                    var t = CodeLookupCache.GetCodeFromKey(ImmunizedBy);
                    if (t != null && t.ToLower().Contains("prov"))
                    {
                        DecliningReasonComment = "";
                        return false;
                    }
                }

                var code = CodeLookupCache.GetCodeFromKey(ReasonForDeclining);

                return code == "DecAdd";
            }
        }

        public bool ContraindicationsVisible
        {
            get
            {
                if (!ImmunizedBy.HasValue)
                {
                    Contraindications = null;
                    return false;
                }

                if (ImmunizedBy.HasValue)
                {
                    var t = CodeLookupCache.GetCodeFromKey(ImmunizedBy);
                    if (t != null && t.ToLower().Contains("prov"))
                    {
                        Contraindications = null;
                        return false;
                    }
                }

                // Only show if DateReceived and ReasonForDeclining have not been answered
                return !DateReceived.HasValue && !ReasonForDeclining.HasValue;
            }
        }

        partial void OnImmunizationChanged()
        {
            RaisePropertyChanged("ImmunizationCode");
            RaisePropertyChanged("ImmunizationCodeDescription");
        }

        partial void OnDateReceivedChanged()
        {
            RaisePropertyChanged("ContraindicationsVisible");
            RaisePropertyChanged("ReasonForDecliningVisibility");

            if (!ContraindicationsVisible)
            {
                Contraindications = null;
            }

            if (!ReasonForDecliningVisibility)
            {
                ReasonForDeclining = null;
            }
        }

        partial void OnContraindicationsChanged()
        {
            RaisePropertyChanged("ReasonForDecliningVisibility");
        }

        partial void OnReasonForDecliningChanged()
        {
            RaisePropertyChanged("ContraindicationsVisible");
            RaisePropertyChanged("ReasonForDecliningCommentsVisibility");

            if (!ContraindicationsVisible)
            {
                Contraindications = null;
            }

            if (!ReasonForDecliningCommentsVisibility)
            {
                DecliningReasonComment = "";
            }
        }

        partial void OnImmunizedByChanged()
        {
            RaisePropertyChanged("ImmunizedByCode");
            RaisePropertyChanged("ImmunizedByCodeDescription");
            RaisePropertyChanged("ContraindicationsVisible");
            RaisePropertyChanged("ReasonForDecliningVisibility");
            RaisePropertyChanged("ReasonForDecliningCommentsVisibility");
        }
    }

    public partial class PatientInsurance
    {
        private string _insuredType;

        public string InsuranceName
        {
            get
            {
                if (InsuranceKey <= 0)
                {
                    return "Unknown";
                }

                var name = InsuranceCache.GetInsuranceNameFromKey(InsuranceKey);
                return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
            }
        }

        public string NameAndNumber => string.Format("{0} - {1}", InsuranceName, InsuranceNumber);

        public string InsuredType
        {
            get { return _insuredType; }
            set
            {
                if (_insuredType != value && value == "S")
                {
                    var clRow = CodeLookupCache.GetCodeLookupsFromType("CONTACTRELATIONSHIP")
                        .Where(cl => cl.Code == "SELF").FirstOrDefault();
                    if (clRow != null)
                    {
                        RelToInsured = clRow.CodeLookupKey;
                        RaisePropertyChanged("RelToInsured");
                    }
                }

                _insuredType = value;
            }
        }

        public string InsuranceTypeCode
        {
            get
            {
                if (InsuranceKey == null || InsuranceKey <= 0)
                {
                    return null;
                }

                var i = InsuranceCache.GetInsuranceFromKey(InsuranceKey);
                return i == null ? null : CodeLookupCache.GetCodeFromKey(i.InsuranceType);
            }
        }

        public int InsuranceTypeKey
        {
            get
            {
                if (InsuranceKey == null || InsuranceKey <= 0)
                {
                    return 99;
                }

                var i = InsuranceCache.GetInsuranceFromKey(InsuranceKey);
                return i == null ? 99 : i.InsuranceType == null ? 99 : (int)i.InsuranceType;
            }
        }

        public bool OASIS
        {
            get
            {
                if (InsuranceKey == null || InsuranceKey <= 0)
                {
                    return false;
                }

                var i = InsuranceCache.GetInsuranceFromKey(InsuranceKey);
                return i?.OASIS ?? false;
            }
        }

        // Needed for instance where the associated Insurance isn't in the collection
        public bool InsuranceRequiresDisciplineOrders
        {
            get
            {
                var i = InsuranceOrCachedInsurance;
                return i?.DisciplineOrders ?? false;
            }
        }

        public Insurance InsuranceOrCachedInsurance => Insurance ?? InsuranceCache.GetInsuranceFromKey(InsuranceKey);

        public bool IsMedicare
        {
            get
            {
                var i = InsuranceOrCachedInsurance;
                if (i != null)
                {
                    return i.IsMedicare;
                }

                return false;
            }
        }

        public string InsuranceNumberDisplay
        {
            get
            {
                if (InsuranceNumber != null)
                {
                    return InsuranceNumber.ToUpper();
                }

                return null;
            }
            set
            {
                InsuranceNumber = value?.ToUpper();
                RaisePropertyChanged("InsuranceNumber");
                RaisePropertyChanged("InsuranceNumberDisplay");
            }
        }

        public string InsuranceNumberWarning
        {
            get
            {
                if (IsMedicare)
                {
                    var n = InsuranceNumber;
                    if (n != null)
                    {
                        if (n != n.Replace(" ", "").Replace(".", "").Replace("-", ""))
                        {
                            InsuranceNumber = n.Replace(" ", "").Replace(".", "").Replace("-", "");
                            RaisePropertyChanged("InsuranceNumber");
                            RaisePropertyChanged("InsuranceNumberWarning");
                        }

                        var result = InsuranceNumberValidation.ValidateMedicareInsuranceNumber(n);
                        return result.Message;
                    }

                    return string.Empty;
                }

                return string.Empty;
            }
        }

        public bool ShowAlternateInsuranceID
        {
            get
            {
                if (InsuranceKey == null || InsuranceKey <= 0)
                {
                    return false;
                }

                var i = InsuranceCache.GetInsuranceFromKey(InsuranceKey);
                return i == null ? false : string.IsNullOrWhiteSpace(i.AlternateInsuranceIDLabel) ? false : true;
            }
        }

        public string AlternateInsuranceIDLabel
        {
            get
            {
                if (InsuranceKey == null || InsuranceKey <= 0)
                {
                    return null;
                }

                var i = InsuranceCache.GetInsuranceFromKey(InsuranceKey);
                return i == null ? null :
                    string.IsNullOrWhiteSpace(i.AlternateInsuranceIDLabel) ? null : i.AlternateInsuranceIDLabel.Trim();
            }
        }

        partial void OnInsuranceKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("InsuranceTypeCode");
            RaisePropertyChanged("OASIS");
            RaisePropertyChanged("InsuranceNumberWarning");
            RaisePropertyChanged("ShowAlternateInsuranceID");
            RaisePropertyChanged("AlternateInsuranceIDLabel");
        }

        partial void OnInsuranceNumberChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("InsuranceNumberWarning");
        }

        public bool IsOASISReviewRequiredForRFA(string RFA)
        {
            if (InsuranceKey == null || InsuranceKey <= 0)
            {
                return false;
            }

            var i = InsuranceCache.GetInsuranceFromKey(InsuranceKey);
            if (i == null)
            {
                return false;
            }

            return i.IsOASISReviewRequiredForRFA(RFA);
        }
    }

    public partial class InsuranceVerifyHistory
    {
        public string VerifiedByFullName
        {
            get
            {
                string name = null;
                name = UserCache.Current.GetFullNameFromUserId(VerifiedBy);
                return name;
            }
        }

        public string DetailsButtonLabel
        {
            get
            {
                var label = "Detail";
                if (InsuranceVerifyHistoryDetail == null)
                {
                    label += " (0)";
                }
                else
                {
                    label += " (" + InsuranceVerifyHistoryDetail.Where(i => !i.Inactive).Count() + ")";
                }

                return label;
            }
        }
    }

    public partial class PatientAdvancedDirective
    {
        private bool _CanFullEdit = true;
        private Patient _CurrentPatient;
        private bool _Expand;

        public string AdvancedDirectiveTypeCode
        {
            get
            {
                var r = CodeLookupCache.GetCodeFromKey(AdvancedDirectiveType);
                return r == null ? "" : r;
            }
        }

        public string RecordedStateCodeDescription
        {
            get
            {
                var r = CodeLookupCache.GetCodeDescriptionFromKey(RecordedStateCode);
                return r == null ? "" : r;
            }
        }

        public string PatientContact1KeyLabel
        {
            get
            {
                if (AdvancedDirectiveTypeCode.ToLower() == "healthcareproxy")
                {
                    return "Health Care Proxy";
                }

                if (AdvancedDirectiveTypeCode.ToLower() == "healthcarepoa")
                {
                    return "Health Care Agent";
                }

                return null;
            }
        }

        public string PatientContact2KeyLabel
        {
            get
            {
                if (AdvancedDirectiveTypeCode.ToLower() == "healthcareproxy")
                {
                    return "Alternative Health Care Proxy";
                }

                if (AdvancedDirectiveTypeCode.ToLower() == "healthcarepoa")
                {
                    return "Alternative Health Care Agent";
                }

                return null;
            }
        }

        public string PatientContact3KeyLabel
        {
            get
            {
                if (AdvancedDirectiveTypeCode.ToLower() == "healthcareproxy")
                {
                    return "Second Alternative Health Care Proxy";
                }

                if (AdvancedDirectiveTypeCode.ToLower() == "healthcarepoa")
                {
                    return "Second Alternative Health Care Agent";
                }

                return null;
            }
        }

        public List<Physician> SigningPhysicianList => PhysicianCache.Current.GetActivePhysiciansPlusMe(SigningPhysicianKey);

        public Patient CurrentPatient
        {
            get { return _CurrentPatient; }
            set
            {
                _CurrentPatient = value;
                RaisePropertyChanged("CurrentPatient");
                RaisePropertyChanged("PatientContact1PickList");
                RaisePropertyChanged("PatientContact2PickList");
                RaisePropertyChanged("PatientContact3PickList");
            }
        }

        public List<PatientContact> PatientContact1PickList
        {
            get
            {
                if (CurrentPatient == null)
                {
                    return null;
                }

                if (CurrentPatient.PatientContact == null)
                {
                    return null;
                }

                return CurrentPatient.PatientContact.Where(p =>
                    p.HistoryKey == null && p.Inactive == false || p.PatientContactKey == PatientContact1Key).ToList();
            }
        }

        public List<PatientContact> PatientContact2PickList
        {
            get
            {
                if (CurrentPatient == null)
                {
                    return null;
                }

                if (CurrentPatient.PatientContact == null)
                {
                    return null;
                }

                var pcList = CurrentPatient.PatientContact.Where(p =>
                    p.HistoryKey == null && p.Inactive == false || p.PatientContactKey == PatientContact2Key).ToList();
                pcList.Insert(0, new PatientContact { PatientContactKey = 0, ContactGuid = Guid.NewGuid() });
                return pcList;
            }
        }

        public List<PatientContact> PatientContact3PickList
        {
            get
            {
                if (CurrentPatient == null)
                {
                    return null;
                }

                if (CurrentPatient.PatientContact == null)
                {
                    return null;
                }

                var pcList = CurrentPatient.PatientContact.Where(p =>
                    p.HistoryKey == null && p.Inactive == false || p.PatientContactKey == PatientContact3Key).ToList();
                pcList.Insert(0, new PatientContact { PatientContactKey = 0, ContactGuid = Guid.NewGuid() });
                return pcList;
            }
        }

        public string EffectiveThruExpirationDateFormatted
        {
            get
            {
                var date = EffectiveDate == null ? "" : ((DateTime)EffectiveDate).ToShortDateString();
                var date2 = ExpirationDate == null ? "" : ((DateTime)ExpirationDate).ToShortDateString();
                return date2 == "" ? date : date + "  -  " + date2;
            }
        }

        public string AdvancedDirectiveTypeCodeDescription =>
            CodeLookupCache.GetCodeDescriptionFromKey(AdvancedDirectiveType);

        public bool Expand
        {
            get { return _Expand; }
            set
            {
                _Expand = value;
                RaisePropertyChanged("Expand");
                RaisePropertyChanged("ThumbNailText");
            }
        }

        public string ThumbNailText
        {
            get
            {
                if (Expand)
                {
                    return NoteText;
                }

                var text = NoteText;
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                string[] CR = { char.ToString('\r') };
                var splitText = text.Split(CR, StringSplitOptions.RemoveEmptyEntries);
                if (splitText.Length == 0)
                {
                    return null;
                }

                if (splitText[0] == text)
                {
                    return text;
                }

                return splitText[0] + " ...";
            }
        }

        public string LastReviewedText
        {
            get
            {
                var dateTime = ReviewedDatePart == null ? "" : Convert.ToDateTime(ReviewedDatePart).ToShortDateString();
                if (ReviewedTimePart != null)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        dateTime = dateTime + " " + Convert.ToDateTime(ReviewedTimePart).ToString("HHmm");
                    }
                    else
                    {
                        dateTime = dateTime + " " + Convert.ToDateTime(ReviewedTimePart).ToShortTimeString();
                    }
                }

                if (string.IsNullOrWhiteSpace(dateTime))
                {
                    return null;
                }

                return string.Format("Last reviewed on {0}  by {1}", dateTime,
                    UserCache.Current.GetFormalNameFromUserId(ReviewedBy));
            }
        }

        public bool CanEdit => PatientAdvancedDirectiveKey <= 0 ? true : false;

        public new bool CanFullEdit
        {
            get { return _CanFullEdit; }
            set
            {
                _CanFullEdit = value;
                RaisePropertyChanged("CanFullEdit");
            }
        }

        partial void OnCreated()
        {
            NewReviewed = false;
        }

        public bool IsCurrentlyActiveAsOfDate(DateTime date)
        {
            if (EffectiveDate == null && ExpirationDate == null)
            {
                return true;
            }

            if (ExpirationDate == null && EffectiveDate != null && ((DateTime)EffectiveDate).Date <= date)
            {
                return true;
            }

            if (EffectiveDate == null && ExpirationDate != null && ((DateTime)ExpirationDate).Date >= date)
            {
                return true;
            }

            if (EffectiveDate != null && ((DateTime)EffectiveDate).Date <= date &&
                ExpirationDate != null && ((DateTime)ExpirationDate).Date >= date)
            {
                return true;
            }

            return false;
        }

        partial void OnAdvancedDirectiveTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (AdvancedDirectiveTypeCode.ToLower() == "dnr" || AdvancedDirectiveTypeCode.ToLower() == "communitydnr")
            {
                PatientContact1Key = null;
                PatientContact1EffectiveDate = null;
                PatientContact1RevocationDate = null;
                PatientContact2Key = null;
                PatientContact2EffectiveDate = null;
                PatientContact2RevocationDate = null;
                PatientContact3Key = null;
                PatientContact3EffectiveDate = null;
                PatientContact3RevocationDate = null;
            }

            if (AdvancedDirectiveTypeCode.ToLower() == "healthcareproxy" ||
                AdvancedDirectiveTypeCode.ToLower() == "healthcarepoa")
            {
                RaisePropertyChanged("PatientContact1KeyLabel");
                RaisePropertyChanged("PatientContact2KeyLabel");
                RaisePropertyChanged("PatientContact3KeyLabel");
                SigningPhysicianKey = null;
            }

            if (AdvancedDirectiveTypeCode.ToLower() == "livingwill" ||
                AdvancedDirectiveTypeCode.ToLower() == "organdonation")
            {
                PatientContact1Key = null;
                PatientContact1EffectiveDate = null;
                PatientContact1RevocationDate = null;
                PatientContact2Key = null;
                PatientContact2EffectiveDate = null;
                PatientContact2RevocationDate = null;
                PatientContact3Key = null;
                PatientContact3EffectiveDate = null;
                PatientContact3RevocationDate = null;
                SigningPhysicianKey = null;
            }

            RaisePropertyChanged("AdvancedDirectiveTypeCode");
            RaisePropertyChanged("AdvancedDirectiveTypeCodeDescription");
        }

        partial void OnPatientContact1KeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PatientContact1PickList");
            if (IsInCancel)
            {
                return;
            }

            if (PatientContact1Key == null)
            {
                PatientContact1Key = PatientContact2Key;
                PatientContact2Key = PatientContact3Key;
                PatientContact3Key = null;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    RaisePropertyChanged("PatientContact1Key");
                    RaisePropertyChanged("PatientContact2Key");
                    RaisePropertyChanged("PatientContact3Key");
                });
            }
        }

        partial void OnPatientContact2KeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PatientContact2PickList");
            if (IsInCancel)
            {
                return;
            }

            if (PatientContact2Key == null)
            {
                PatientContact2Key = PatientContact3Key;
                PatientContact3Key = null;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    RaisePropertyChanged("PatientContact2Key");
                    RaisePropertyChanged("PatientContact3Key");
                });
            }
        }

        partial void OnPatientContact3KeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PatientContact3PickList");
        }

        partial void OnEffectiveDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EffectiveThruExpirationDateFormatted");
        }

        partial void OnExpirationDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EffectiveThruExpirationDateFormatted");
        }

        partial void OnNoteTextChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ThumbNailText");
        }

        partial void OnNewReviewedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (NewReviewed)
            {
                NewReviewedDateTimeOffSet = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                NewReviewedBy = WebContext.Current.User.MemberID;
            }
            else
            {
                NewReviewedDateTimeOffSet = null;
                NewReviewedBy = null;
            }

            RaisePropertyChanged("NewReviewedDateTimeOffSet");
            RaisePropertyChanged("NewReviewedDatePart");
            RaisePropertyChanged("NewReviewedTimePart");
            RaisePropertyChanged("NewReviewedBy");
        }

        partial void OnNewReviewedDatePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (NewReviewedDatePart == null)
            {
                NewReviewedOffSetPart = null;
            }
            else
            {
                NewReviewedOffSetPart = ((DateTimeOffset)DateTime.Now).Offset;
            }

            RaisePropertyChanged("NewReviewedOffSetPart");
        }

        public void EffectiveDatePatientContacts()
        {
            var original = GetOriginal() as PatientAdvancedDirective;
            if (original == null)
            {
                if (PatientContact1Key != null)
                {
                    PatientContact1EffectiveDate = EffectiveDate ?? DateTime.Now;
                }

                if (PatientContact2Key != null)
                {
                    PatientContact2EffectiveDate = EffectiveDate ?? DateTime.Now;
                }

                if (PatientContact3Key != null)
                {
                    PatientContact3EffectiveDate = EffectiveDate ?? DateTime.Now;
                }
            }
            else
            {
                if (PatientContact1Key != original.PatientContact1Key)
                {
                    PatientContact1EffectiveDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                }

                if (PatientContact2Key != original.PatientContact2Key)
                {
                    PatientContact2EffectiveDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                }

                if (PatientContact3Key != original.PatientContact3Key)
                {
                    PatientContact3EffectiveDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                }
            }
        }

        public void RaiseChanged()
        {
            RaisePropertyChanged("PatientContact1PickList");
            RaisePropertyChanged("PatientContact2PickList");
            RaisePropertyChanged("PatientContact3PickList");
        }
    }

    public partial class PatientFacilityStay
    {
        private bool _CanEdit;
        private Patient _CurrentPatient;
        private DateTime? _DisplayDateStart;
        private string prevFacilityType;

        public List<FacilityBranch> FacilityBranchList
        {
            get
            {
                return FacilityCache.GetActiveBranchesAndMe(FacilityBranchKey).Where(f => f.FacilityKey == FacilityKey)
                    .ToList();
            }
        }

        public string FacilityName
        {
            get
            {
                var f = FacilityCache.GetFacilityFromKey(FacilityKey);
                return f?.Name;
            }
        }

        public string FacilityBranchName
        {
            get
            {
                var fb = FacilityCache.GetFacilityBranchFromKey(FacilityBranchKey);
                return fb == null || string.IsNullOrWhiteSpace(fb.BranchName) ? null : fb.BranchName;
            }
        }

        public string FacilityType
        {
            get
            {
                var f = FacilityCache.GetFacilityFromKey(FacilityKey);
                return f?.TypeCode;
            }
        }

        public string FacilityTypeDescription
        {
            get
            {
                var f = FacilityCache.GetFacilityFromKey(FacilityKey);
                return f?.TypeCodeDescription;
            }
        }

        public bool IsFacilityTypeAcuteCare => FacilityType == null ? false :
            FacilityType.Trim().ToLower().StartsWith("acute") ? true : false;

        public bool IsFacilityTypeSNF => FacilityType == null ? false :
            FacilityType.Trim().ToLower().StartsWith("snf") ? true : false;

        public bool IsFacilityTypeHospice
        {
            get
            {
                var type = FacilityType;
                if (string.IsNullOrWhiteSpace(type))
                {
                    return false;
                }

                type = type.Trim().ToLower();
                if (type.Contains("hospice"))
                {
                    return true;
                }

                if (type.StartsWith("hospfac"))
                {
                    return true;
                }

                if (type.StartsWith("hosp fac"))
                {
                    return true;
                }

                return false;
            }
        }

        public string FacilityStayTypeCode => CodeLookupCache.GetCodeFromKey(FacilityStayType);

        public string FacilityStayTypeCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(FacilityStayType);

        public string FacilityStayReasonCode => CodeLookupCache.GetCodeFromKey(FacilityStayReason);

        public string FacilityStayReasonCodeDescription =>
            CodeLookupCache.GetCodeDescriptionFromKey(FacilityStayReason);

        public string FacilityStayReasonCodeLookup
        {
            get
            {
                if (IsFacilityTypeAcuteCare)
                {
                    return "FacilityStayAcuteCare";
                }

                if (IsFacilityTypeSNF)
                {
                    return "FacilityStaySNFacility";
                }

                if (IsFacilityTypeHospice)
                {
                    return "FacilityStayHospiceInpatient";
                }

                return "FacilityStayOther";
            }
        }

        public bool CanEdit
        {
            get { return _CanEdit; }
            set
            {
                _CanEdit = value;
                RaisePropertyChanged("CanEdit");
            }
        }

        public Patient CurrentPatient
        {
            get { return _CurrentPatient; }
            set
            {
                _CurrentPatient = value;
                RaisePropertyChanged("CurrentPatient");
            }
        }

        public DateTime? DisplayDateStart
        {
            get { return _DisplayDateStart; }
            set
            {
                _DisplayDateStart = value;
                RaisePropertyChanged("DisplayDateStart");
            }
        }

        public string FullAddress
        {
            get
            {
                var address = string.Empty;
                var CR = char.ToString('\r');
                var f = FacilityCache.GetFacilityFromKey(FacilityKey);
                var fb = FacilityCache.GetFacilityBranchFromKey(FacilityBranchKey);
                if (f != null && string.IsNullOrWhiteSpace(f.Name) == false)
                {
                    address = f.Name;
                }

                if (fb != null && string.IsNullOrWhiteSpace(fb.BranchName) == false)
                {
                    address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + fb.BranchName;
                }

                var usingBranchAddress = false;
                if (fb != null)
                {
                    usingBranchAddress = true;
                    if (!string.IsNullOrWhiteSpace(fb.Address1))
                    {
                        address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + fb.Address1;
                    }

                    if (!string.IsNullOrWhiteSpace(fb.Address2))
                    {
                        address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + fb.Address2;
                    }

                    if (!string.IsNullOrWhiteSpace(fb.CityStateZip))
                    {
                        address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + fb.CityStateZip;
                    }
                }

                if (f != null && usingBranchAddress == false)
                {
                    if (!string.IsNullOrWhiteSpace(f.Address1))
                    {
                        address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + f.Address1;
                    }

                    if (!string.IsNullOrWhiteSpace(f.Address2))
                    {
                        address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + f.Address2;
                    }

                    if (!string.IsNullOrWhiteSpace(f.CityStateZip))
                    {
                        address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + f.CityStateZip;
                    }
                }

                var faxNumber = FaxNumber;
                if (!string.IsNullOrWhiteSpace(faxNumber))
                {
                    address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + faxNumber;
                }

                var phoneNumber = PhoneNumber;
                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    address = address + (string.IsNullOrWhiteSpace(address) ? "" : CR) + phoneNumber;
                }

                return address;
            }
        }

        public string FaxNumber
        {
            get
            {
                var number = string.Empty;
                var f = FacilityCache.GetFacilityFromKey(FacilityKey);
                var fb = FacilityCache.GetFacilityBranchFromKey(FacilityBranchKey);
                if (fb != null)
                {
                    if (!string.IsNullOrWhiteSpace(fb.Fax))
                    {
                        number = PhoneConvert(fb.Fax);
                    }
                }

                if (f != null && string.IsNullOrWhiteSpace(number))
                {
                    if (!string.IsNullOrWhiteSpace(f.Fax))
                    {
                        number = PhoneConvert(f.Fax);
                    }
                }

                return string.IsNullOrWhiteSpace(number) ? null : "Fax        " + number;
            }
        }

        public string PhoneNumber
        {
            get
            {
                var number = string.Empty;
                var f = FacilityCache.GetFacilityFromKey(FacilityKey);
                var fb = FacilityCache.GetFacilityBranchFromKey(FacilityBranchKey);
                if (fb != null)
                {
                    if (!string.IsNullOrWhiteSpace(fb.PhoneNumber))
                    {
                        number = PhoneConvert(fb.PhoneNumber);
                    }

                    if (string.IsNullOrWhiteSpace(number) == false)
                    {
                        number = number + (string.IsNullOrWhiteSpace(fb.PhoneExtension) == false
                            ? " x" + fb.PhoneExtension
                            : "");
                    }
                }

                if (f != null && string.IsNullOrWhiteSpace(number))
                {
                    if (!string.IsNullOrWhiteSpace(f.Number))
                    {
                        number = PhoneConvert(f.Number);
                    }

                    if (string.IsNullOrWhiteSpace(number) == false)
                    {
                        number = number + (string.IsNullOrWhiteSpace(f.PhoneExtension) == false
                            ? " x" + f.PhoneExtension
                            : "");
                    }
                }

                return string.IsNullOrWhiteSpace(number) ? null : "Phone  " + number;
            }
        }

        partial void OnFacilityKeyChanging(int facilityKey)
        {
            if (IsDeserializing)
            {
                return;
            }

            prevFacilityType = FacilityType;
        }

        partial void OnFacilityKeyChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            if (!IsInCancel) //If not canceling the current edit mode for this entity...
            {
                //When FacilityKey changes, reset dependent properties...
                FacilityBranchKey = null;
                FacilityStayType = 0;
                if (prevFacilityType != FacilityType)
                {
                    FacilityStayReason = 0;
                }
            }

            RaisePropertyChanged("FacilityName");
            RaisePropertyChanged("FacilityType");
            RaisePropertyChanged("FacilityTypeDescription");
            RaisePropertyChanged("FacilityStayReasonCodeLookup");
            RaisePropertyChanged("FacilityBranchList");
        }

        partial void OnFacilityStayTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FacilityStayTypeCode");
            RaisePropertyChanged("FacilityStayTypeCodeDescription");
        }

        partial void OnFacilityStayReasonChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FacilityStayReasonCode");
            RaisePropertyChanged("FacilityStayReasonCodeDescription");
        }

        public void RaisePropertyChangedPatientFacilityStay()
        {
            RaisePropertyChanged("FacilityName");
            RaisePropertyChanged("FacilityBranchName");
            RaisePropertyChanged("StartDate");
            RaisePropertyChanged("EndDate");
            RaisePropertyChanged("FullAddress");
        }

        private string PhoneConvert(string number)
        {
            var pc = new PhoneConverter();
            var phoneObject = pc.Convert(number, null, null, null);
            if (phoneObject != null)
            {
                if (string.IsNullOrWhiteSpace(phoneObject.ToString()) == false)
                {
                    return phoneObject.ToString();
                }
            }

            return null;
        }
    }

    public partial class PatientPhone
    {
        public int PhoneTypePriority
        {
            get
            {
                var code = PhoneTypeCode;
                if (string.IsNullOrWhiteSpace(code))
                {
                    return 5;
                }

                code = code.ToLower();
                if (code == "cell")
                {
                    return 1;
                }

                if (code == "home")
                {
                    return 2;
                }

                if (code == "work")
                {
                    return 3;
                }

                return 4;
            }
        }

        public string PhoneTypeCode => CodeLookupCache.GetCodeFromKey(Type);

        public string PhoneNumber
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Number))
                {
                    return null;
                }

                var pc = new PhoneConverter();
                if (pc == null)
                {
                    return null;
                }

                var phoneObject = pc.Convert(Number, null, null, null);
                var number = phoneObject != null ? phoneObject.ToString() : null;
                if (string.IsNullOrWhiteSpace(number))
                {
                    return null;
                }

                var extension = string.IsNullOrWhiteSpace(Extension) ? "" : "x" + Extension.Trim();
                var type = string.IsNullOrWhiteSpace(PhoneTypeCode) ? "" : PhoneTypeCode;
                return string.Format("{0} Phone:  {1} {2}", type, number, extension);
            }
        }
    }

    public partial class PatientAlternateID
    {
        public string DropdownText => Issuer + (string.IsNullOrWhiteSpace(Issuer) ? "" : " : ") + TypeCode + " - " +
                                      Identifier + (IsInactiveBindTarget ? " - (inactive)" : "");

        public bool IsInactiveBindTarget
        {
            get { return InactiveDateTime.HasValue; }
            set
            {
                if (value)
                {
                    if (!InactiveDateTime.HasValue)
                    {
                        InactiveDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                    }
                }
                else
                {
                    InactiveDateTime = null;
                    RaisePropertyChanged("DropdownText");
                }
            }
        }

        partial void OnIssuerChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DropdownText");
        }

        partial void OnTypeCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DropdownText");
        }

        partial void OnIdentifierChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DropdownText");
        }

        partial void OnInactiveDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DropdownText");
        }
    }

    public partial class PatientMedicationSlidingScale
    {
        public PatientMedicationSlidingScale CreateNewVersion()
        {
            var newSS = (PatientMedicationSlidingScale)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newSS);
            return newSS;
        }
    }

    public partial class AdvanceCarePlan
    {
        partial void OnHasAdvanceDirectiveChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            Messenger.Default.Send(this, "AdvanceCarePlanChanged");
        }

        partial void OnHasPowerOfAttorneyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            Messenger.Default.Send(this, "AdvanceCarePlanChanged");
        }
    }
}