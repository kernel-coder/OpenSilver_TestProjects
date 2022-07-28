#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionPhysician
    {
        public string FormattedName
        {
            get
            {
                string name = null;
                var phy = PhysicianCache.Current.GetPhysicianFromKey(PhysicianKey);
                if (phy != null)
                {
                    name = phy.FormattedName;
                }

                return name;
            }
        }

        public string FullName
        {
            get
            {
                string name = null;
                var phy = PhysicianCache.Current.GetPhysicianFromKey(PhysicianKey);
                if (phy != null)
                {
                    name = phy.FullName;
                }

                return name;
            }
        }

        public string FormattedNameWithType => FormattedName + " - " + PhysicianTypeCodeDescription;

        public string PhysicianName
        {
            get
            {
                string name = null;
                var phy = PhysicianCache.Current.GetPhysicianFromKey(PhysicianKey);
                if (phy != null)
                {
                    name = phy.FullNameInformalWithSuffix;
                }

                return name;
            }
        }

        public string MedicalDirectorPhysicianName
        {
            get
            {
                if (PhysicianKey == -1)
                {
                    return "Not Applicable";
                }

                return PhysicianName;
            }
        }

        public string PhysicianEffectiveDateRange =>
            string.Format("{0} - {1}",
                PhysicianEffectiveFromDate.ToString("d"),
                PhysicianEffectiveThruDate.HasValue ? PhysicianEffectiveThruDate.Value.ToString("d") : string.Empty);

        public string SigningEffectiveDateRange
        {
            get
            {
                if (SigningEffectiveFromDate.HasValue)
                {
                    return string.Format("{0} - {1}",
                        SigningEffectiveFromDate.Value.ToString("d"),
                        SigningEffectiveThruDate.HasValue
                            ? SigningEffectiveThruDate.Value.ToString("d")
                            : string.Empty);
                }

                return string.Empty;
            }
        }

        public string PhysicianNotes
        {
            get
            {
                string physicianNotes = null;
                if (TenantSettingsCache.Current.TenantSettingPECOSAlert &&
                    PhysicianCache.Current.GetPhysicianPECOSFromKey(PhysicianKey) == false)
                {
                    physicianNotes = "Not in PECOS.";
                }

                var expiredLicense = ExpiredLicenseInStates;
                if (string.IsNullOrWhiteSpace(expiredLicense) == false)
                {
                    physicianNotes = (physicianNotes == null ? "" : physicianNotes + "  ") + "Expired License in " +
                                     ExpiredLicenseInStates + ".";
                }

                return physicianNotes;
            }
        }

        private DateTime LesserOf_Today_AdmissionPhysicianEffectiveThruDate_AdmissionDischargeDate
        {
            get
            {
                // Return lesser of Today PhysicianEffectiveThruDate AdmissionDischargeDate 
                var LesserDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                if (PhysicianEffectiveThruDate != null)
                {
                    LesserDateTime = (DateTime)PhysicianEffectiveThruDate < LesserDateTime
                        ? (DateTime)PhysicianEffectiveThruDate
                        : LesserDateTime;
                }

                if (Admission == null)
                {
                    return LesserDateTime;
                }

                if (Admission.AdmissionStatusCode == "D" && Admission.DischargeDateTime != null)
                {
                    LesserDateTime = (DateTime)Admission.DischargeDateTime < LesserDateTime
                        ? (DateTime)Admission.DischargeDateTime
                        : LesserDateTime;
                }

                return LesserDateTime.Date;
            }
        }

        private string ExpiredLicenseInStates
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                if (Admission.Patient == null)
                {
                    return null;
                }

                if (Admission.Patient.PatientAddress == null)
                {
                    return null;
                }

                var p = PhysicianCache.Current.GetPhysicianFromKey(PhysicianKey);
                if (p == null)
                {
                    return null;
                }

                // get short list of most recent licences (by Expiration date) for each state - don't include the state if it has a row with a null Expiration date
                var plListNoExpiration = p.PhysicianLicense.Where(pl => pl.Inactive == false && pl.Expiration == null)
                    .OrderBy(pl => pl.StateCodeCode).ToList();
                var plListExpiration = p.PhysicianLicense.Where(pl => pl.Inactive == false && pl.Expiration != null)
                    .OrderBy(pl => pl.StateCodeCode).ThenByDescending(pl => pl.Expiration).ToList();
                var plListMostRecent = new List<PhysicianLicense>();
                foreach (var pl in plListExpiration)
                    // A NoExpiration trumps an Expiration row  - e.g., if there is a License for PA without an Expiration date - you'll never have an 'Expired License in PA' condition
                    if (plListNoExpiration.Where(plne => plne.StateCode == pl.StateCode).FirstOrDefault() ==
                        null) // A NoExpiration trumps an Expiration row
                        // We sorted by Expiration date Descending - so only add most recently expired license for each state
                    {
                        if (plListMostRecent.Where(plmr => plmr.StateCode == pl.StateCode).FirstOrDefault() == null)
                        {
                            plListMostRecent.Add(pl);
                        }
                    }

                if (plListMostRecent.Any() == false)
                {
                    return null;
                }

                string expiredLicenseInStates = null;
                foreach (var pl in plListMostRecent)
                {
                    var currentActiveAddressInThisState = Admission.Patient.PatientAddress
                        .Where(pa => pa.Inactive == false && pa.HistoryKey == null && pa.StateCode == pl.StateCode
                                     && (pa.EffectiveFromDate.HasValue == false || pa.EffectiveFromDate.HasValue &&
                                         ((DateTime)pa.EffectiveFromDate).Date <= DateTime.Today.Date)
                                     && (pa.EffectiveThruDate.HasValue == false || pa.EffectiveThruDate.HasValue &&
                                         ((DateTime)pa.EffectiveThruDate).Date >= DateTime.Today.Date)
                                     && (pa.TypeDescription == "Home" || pa.TypeDescription == "Temporary" ||
                                         pa.TypeDescription == "Facility")).FirstOrDefault();
                    if (currentActiveAddressInThisState != null)
                        // Patient has a currently active address (active today) in the state of this license (a license that has an Expiration date)
                        // if the Licence will expire before the LesserOf_Today_PhysicianEffectiveThruDate_AdmissionDischargeDate add the state to the list of Expired License states
                    {
                        if (((DateTime)pl.Expiration).Date <
                            LesserOf_Today_AdmissionPhysicianEffectiveThruDate_AdmissionDischargeDate.Date)
                        {
                            if (expiredLicenseInStates == null || expiredLicenseInStates != null &&
                                expiredLicenseInStates.Contains(currentActiveAddressInThisState.StateCodeCode) == false)
                            {
                                expiredLicenseInStates = expiredLicenseInStates +
                                                         currentActiveAddressInThisState.StateCodeCode + ",";
                            }
                        }
                    }
                }

                if (expiredLicenseInStates != null)
                {
                    expiredLicenseInStates = expiredLicenseInStates.Substring(0, expiredLicenseInStates.Length - 1);
                }

                return expiredLicenseInStates;
            }
        }


        public Physician PhysicianProxy => PhysicianCache.Current.GetPhysicianFromKey(PhysicianKey);

        public PhysicianAddress PhysicianAddressProxy
        {
            get
            {
                if (PhysicianAddressKey == null)
                {
                    return null;
                }

                if (PhysicianProxy == null)
                {
                    return null;
                }

                if (PhysicianProxy.PhysicianAddress == null)
                {
                    return null;
                }

                return PhysicianProxy.PhysicianAddress.FirstOrDefault(pa => pa.PhysicianAddressKey == PhysicianAddressKey);
            }
        }

        public PhysicianAddress PhysicianAddressOrMain
        {
            get
            {
                if (PhysicianAddressProxy != null)
                {
                    return PhysicianAddressProxy;
                }

                PhysicianAddress PhyAddress = null;
                if (Physician == null)
                {
                    if (PhysicianProxy != null)
                    {
                        PhyAddress = PhysicianProxy.MainAddress;
                    }
                }
                else
                {
                    PhyAddress = Physician.MainAddress;
                }

                return PhyAddress ?? new PhysicianAddress();
            }
        }

        public string FullAddress
        {
            get
            {
                var pa = PhysicianAddressOrMain;
                return pa?.FullAddress;
            }
        }

        public ICollection<PhysicianPhone> ActivePhones
        {
            get
            {
                if (PhysicianProxy == null || PhysicianProxy.PhysicianPhone == null)
                {
                    return null;
                }

                var pp = PhysicianProxy.PhysicianPhone.Where(p => p.Inactive == false).OrderBy(p => p.TypeDescription)
                    .ToList();
                if (pp == null)
                {
                    return null;
                }

                return pp.Any() == false ? null : pp;
            }
        }

        public string AddressPhones
        {
            get
            {
                var pa = PhysicianAddressOrMain;
                return pa?.AddressPhones;
            }
        }

        public string AddressPhoneNumber
        {
            get
            {
                var pa = PhysicianAddressOrMain;
                return pa?.AddressPhoneNumber;
            }
        }

        public string NPIString => PhysicianProxy?.NPIString;

        public string PhysicianTypeCode => CodeLookupCache.GetCodeFromKey(PhysicianType);

        public string PhysicianTypeCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(PhysicianType);

        public bool IsMedDirect => PhysicianTypeCode != null && PhysicianTypeCode.ToLower() == "meddirect";

        public bool IsAdmit => PhysicianTypeCode != null && PhysicianTypeCode.ToLower() == "admit";

        public bool IsPCP => PhysicianTypeCode != null && PhysicianTypeCode.ToLower() == "pcp";

        public bool IsMedicalDirectorOrHospicePhysicianUser =>
            UserCache.Current.GetUserProfileFromPhysicianKeyWherePhysicianIsMedicalDirectorOrHospicePhysician(
                PhysicianKey) == null
                ? false
                : true;

        partial void OnInactiveChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Inactive)
            {
                InactiveDate = DateTime.UtcNow;
            }
            else
            {
                InactiveDate = null;
            }
        }

        partial void OnSigningChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Signing == false)
            {
                SigningEffectiveFromDate = null;
                SigningEffectiveThruDate = null;
                RaisePropertyChanged("SigningEffectiveDateRange");
            }
        }

        partial void OnPhysicianEffectiveFromDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PhysicianEffectiveDateRange");
            RaisePropertyChanged("PhysicianNotes");
        }

        partial void OnPhysicianEffectiveThruDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PhysicianEffectiveDateRange");
            RaisePropertyChanged("PhysicianNotes");
        }

        partial void OnPhysicianKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PhysicianNotes");
        }

        partial void OnSigningEffectiveFromDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("SigningEffectiveDateRange");
        }

        partial void OnSigningEffectiveThruDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("SigningEffectiveDateRange");
        }

        public void RaisePropertyChangedPhysicianNotes()
        {
            RaisePropertyChanged("PhysicianNotes");
        }

        public void RaiseAddressChanged()
        {
            RaisePropertyChanged("AdmissionPhysicianKey");
            RaisePropertyChanged("FormattedName");
            RaisePropertyChanged("FullAddress");
            RaisePropertyChanged("ActivePhones");
            RaisePropertyChanged("AddressPhones");
            RaisePropertyChanged("NPIString");
        }

        partial void OnPhysicianTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("PhysicianTypeCode");
        }
    }
}