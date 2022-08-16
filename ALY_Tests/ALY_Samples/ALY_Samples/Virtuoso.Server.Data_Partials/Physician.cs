#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Converters;
using Virtuoso.Core.Services;
using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class Physician
    {
        public bool IsValidForHomeHealth
        {
            get
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;

                var tst = ServiceLineTypeUseBits.HasValue ? ServiceLineTypeUseBits.Value : 0;
                return (tst & homeHealthBit) > 0; // Is Valid for HomeHealth
            }
            set
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;
                var hospiceBit = (int)eServiceLineType.Hospice;
                var homeCareBit = (int)eServiceLineType.HomeCare;

                var isSet = value;
                var tst = ServiceLineTypeUseBits.HasValue ? ServiceLineTypeUseBits.Value : 0;
                if (isSet)
                {
                    tst = tst | homeHealthBit;
                }
                else
                {
                    tst = tst & (hospiceBit | homeCareBit);
                }

                ServiceLineTypeUseBits = tst;
                SharedBitChanges();
            }
        }

        public bool IsValidForHospice
        {
            get
            {
                var hospiceBit = (int)eServiceLineType.Hospice;
                var tst = ServiceLineTypeUseBits.HasValue ? ServiceLineTypeUseBits.Value : 0;
                return (tst & hospiceBit) > 0; // Is Valid for Hospice
            }
            set
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;
                var hospiceBit = (int)eServiceLineType.Hospice;
                var homeCareBit = (int)eServiceLineType.HomeCare;

                var isSet = value;
                var tst = ServiceLineTypeUseBits.HasValue ? ServiceLineTypeUseBits.Value : 0;
                if (isSet)
                {
                    tst = tst | hospiceBit;
                }
                else
                {
                    tst = tst & (homeCareBit | homeHealthBit);
                }

                ServiceLineTypeUseBits = tst;
                SharedBitChanges();
            }
        }

        public bool IsValidForHomeCare
        {
            get
            {
                var homeCareBit = (int)eServiceLineType.HomeCare;
                var tst = ServiceLineTypeUseBits.HasValue ? ServiceLineTypeUseBits.Value : 0;
                return (tst & homeCareBit) > 0; // Is Valid for HomeCare
            }
            set
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;
                var hospiceBit = (int)eServiceLineType.Hospice;
                var homeCareBit = (int)eServiceLineType.HomeCare;

                var isSet = value;
                var tst = ServiceLineTypeUseBits.HasValue ? ServiceLineTypeUseBits.Value : 0;
                if (isSet)
                {
                    tst = tst | homeCareBit;
                }
                else
                {
                    tst = tst & (hospiceBit | homeHealthBit);
                }

                ServiceLineTypeUseBits = tst;
                SharedBitChanges();
            }
        }

        public bool IsHomeHealth => IsValidForHomeHealth || IsValidForHomeCare;
        public void SharedBitChanges()
        {
            RaisePropertyChanged("IsValidForHomeHealth");
            RaisePropertyChanged("IsValidForHospice");
            RaisePropertyChanged("IsValidForHomeCare");
            RaisePropertyChanged("IsHomeHealth");
            RaisePropertyChanged("ServiceLineTypeUseBits");
        }
        partial void OnServiceLineTypeUseBitsChanged()
        {
            if (IsDeserializing)
            {
                return;
            }
            SharedBitChanges();
        }
        public string HomeHealthPrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "2");

        public string HospicePrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "4");

        public string HomeCarePrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "8");
        public string EditFullName
        {
            get
            {
                if (FullNameInformal != " ")
                {
                    return FullNameInformal;
                }

                return IsNew ? "New Physician" : "Edit Physician";
            }
        }


        public IEnumerable<PhysicianAddress> RelatedBranches
        {
            get { return PhysicianAddress.Where(p => p.FacilityBranchRelated && p.Inactive == false); }
        }

        public PhysicianPhone PrimaryPhone
        {
            get
            {
                if (PhysicianPhone == null)
                {
                    return null;
                }

                var p = PhysicianPhone.Where(i => i.Inactive == false && i.Main).FirstOrDefault();
                if (p == null)
                {
                    p = PhysicianPhone
                            .Where(i => i.Inactive == false).OrderBy(i => i.PhysicianPhoneKey).FirstOrDefault();
                }

                return p;
            }
        }

        public string NPIString => string.IsNullOrWhiteSpace(NPI) ? null : "NPI     " + NPI;


        public string FacilityName
        {
            get
            {
                var facility = FacilityCache.GetFacilityFromKey(FacilityKey);
                var facilityName = "";

                if (facility != null)
                {
                    facilityName = facility.Name;
                }

                return facilityName;
            }
        }


        public FacilityBranch AttendingBranch
        {
            get
            {
                FacilityBranch attendingBranch = null;

                foreach (var address in PhysicianAddress.Where(pa => pa.Inactive == false))
                    if (address.AttendingPhysician)
                    {
                        if (address.FacilityBranchRelated)
                        {
                            var facilityBranchKey = address.FacilityBranchKey;
                            if (address.FacilityBranchKey != null)
                            {
                                attendingBranch = FacilityCache.GetFacilityBranchFromKey(facilityBranchKey);
                            }
                        }
                    }

                return attendingBranch;
            }
        }

        public bool IsBranchAttending
        {
            get
            {
                var isAttending = true;

                if (AttendingBranch == null)
                {
                    isAttending = false;
                }


                return isAttending;
            }
        }

        public string AttendingBranchName
        {
            get
            {
                var attendingBranchName = "";

                if (AttendingBranch != null)
                {
                    attendingBranchName = AttendingBranch.BranchName;
                }


                return attendingBranchName;
            }
        }

        public List<PhysicianLicense> ActiveLicenses
        {
            get
            {
                if (PhysicianLicense == null)
                {
                    return null;
                }

                var p =
                    PhysicianLicense.Where(i => !i.Inactive).OrderBy(i => i.StateCode).ThenBy(i => i.Expiration)
                        .ToList();
                return p == null ? null : p.Any() == false ? null : p;
            }
        }

        public List<PhysicianPhone> ActivePhones
        {
            get
            {
                if (PhysicianPhone == null)
                {
                    return null;
                }

                var p =
                    PhysicianPhone.Where(i => !i.Inactive).OrderBy(i => i.Main).ThenBy(i => i.PhysicianPhoneKey)
                        .ToList();
                return p == null ? null : p.Any() == false ? null : p;
            }
        }

        public bool HasLicenses => ActiveLicenses.Any();

        public List<PhysicianEmail> ActiveEmails
        {
            get
            {
                if (PhysicianEmail == null)
                {
                    return null;
                }

                var p =
                    PhysicianEmail.Where(i => i.Inactive == false).OrderBy(i => i.PhysiciansEmail)
                        .ThenBy(i => i.PhysicianEmailKey).ToList();
                return p == null ? null : p.Any() == false ? null : p;
            }
        }

        public PhysicianAddress MainAddress
        {
            get
            {
                // FormattedName Address1 Address2 CityStateZip
                if (PhysicianAddress == null)
                {
                    return null;
                }

                var p = PhysicianAddress.Where
                (i => i.Inactive == false &&
                      (i.EffectiveFromDate.HasValue == false || i.EffectiveFromDate <= DateTime.UtcNow) &&
                      (i.EffectiveThruDate.HasValue == false || i.EffectiveThruDate >= DateTime.UtcNow) &&
                      i.Main).FirstOrDefault();
                if (p == null)
                {
                    p = PhysicianAddress.Where
                        (i => i.Inactive == false &&
                              (i.EffectiveFromDate.HasValue == false || i.EffectiveFromDate <= DateTime.UtcNow) &&
                              (i.EffectiveThruDate.HasValue == false || i.EffectiveThruDate >= DateTime.UtcNow))
                        .OrderBy(i => i.PhysicianAddressKey).FirstOrDefault();
                }

                return p;
            }
        }

        public string SpecialityDescription
        {
            get
            {
                var value = "";
                var s = Speciality;
                if (s != null)
                {
                    var sp = s.Split('|');
                    foreach (var Key in sp)
                    {
                        var iKey = 0;
                        if (int.TryParse(Key, out iKey))
                        {
                            var cl = CodeLookupCache.GetCodeLookupFromKey(iKey);
                            value = (value.Length > 0 ? value + " - " : string.Empty) + cl.CodeDescription;
                        }
                        else
                        {
                            if (Key.Length > 2 && Key.Substring(0, 1).Equals("\"") &&
                                Key.Substring(Key.Length - 1, 1).Equals("\""))
                            {
                                value = (value.Length > 0 ? value + " - " : string.Empty) +
                                        Key.Substring(1, Key.Length - 2);
                            }
                        }
                    }
                }

                return value;
            }
        }

        public string SearchedPhone
        {
            get
            {
                if (PhysicianPhone != null && PhysicianPhone.Where(i => i.Inactive == false && i.Main).Any())
                {
                    var p = PhysicianPhone.Where(i => i.Inactive == false && i.Main).FirstOrDefault();
                    if (p != null)
                    {
                        return p.Number;
                    }
                }

                if (PhysicianAddress != null && PhysicianAddress.Where(x => x.Main && !x.Inactive).Any())
                {
                    return PhysicianAddress.FirstOrDefault(x => x.Main && !x.Inactive).PhoneNumber;
                }

                if (PhysicianPhone != null && PhysicianPhone.Where(x => !x.Inactive).Count() == 1)
                {
                    return PhysicianPhone.Where(x => !x.Inactive).FirstOrDefault().Number;
                }

                if (PhysicianAddress != null && PhysicianAddress.Where(x => !x.Inactive).Count() == 1)
                {
                    return PhysicianAddress.Where(x => !x.Inactive).FirstOrDefault().PhoneNumber;
                }

                return string.Empty;
            }
        }

        // Used to display * in PhysicianSearchResultsView - caurni00
        public string IsInactiveIndicator
        {
            get
            {
                if (Inactive)
                {
                    return "*";
                }

                return string.Empty;
            }
        }

        public PhysicianPhone MainPhone
        {
            get
            {
                if (PhysicianPhone == null)
                {
                    return null;
                }

                var p = PhysicianPhone.Where
                (i => i.Inactive == false &&
                      i.Main).FirstOrDefault();
                if (p == null)
                {
                    p = PhysicianPhone.Where
                            (i => i.Inactive == false)
                        .OrderBy(i => i.PhysicianPhoneKey).FirstOrDefault();
                }

                return p;
            }
        }

        partial void OnLastNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FormattedName");
            RaisePropertyChanged("FullName");
            RaisePropertyChanged("FullNameWithSuffix");
            RaisePropertyChanged("EditFullName");
            RaisePropertyChanged("FullNameInformal");
            RaisePropertyChanged("FullNameInformalWithSuffix");
        }

        partial void OnFirstNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FormattedName");
            RaisePropertyChanged("FullName");
            RaisePropertyChanged("FullNameWithSuffix");
            RaisePropertyChanged("EditFullName");
            RaisePropertyChanged("FullNameInformal");
            RaisePropertyChanged("FullNameInformalWithSuffix");
        }

        partial void OnMiddleInitialChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FormattedName");
            RaisePropertyChanged("FullName");
            RaisePropertyChanged("FullNameWithSuffix");
            RaisePropertyChanged("EditFullName");
            RaisePropertyChanged("FullNameInformal");
            RaisePropertyChanged("FullNameInformalWithSuffix");
        }

        partial void OnSuffixChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FormattedName");
            RaisePropertyChanged("FullName");
            RaisePropertyChanged("FullNameWithSuffix");
            RaisePropertyChanged("EditFullName");
            RaisePropertyChanged("FullNameInformal");
            RaisePropertyChanged("FullNameInformalWithSuffix");
        }

        public PhysicianPhone GetPhoneByType(string TypeParm)
        {
            if (PhysicianPhone == null)
            {
                return null;
            }

            var typeKey = CodeLookupCache.GetKeyFromCode("PHYPHONE", "FAX");
            if (!typeKey.HasValue)
            {
                return null;
            }

            var p = PhysicianPhone.Where
            (i => i.Inactive == false &&
                  i.Type == typeKey).FirstOrDefault();
            return p;
        }
    }

    public partial class PhysicianPhone
    {
        public string TypeDescription => Type == null ? null : CodeLookupCache.GetCodeDescriptionFromKey((int)Type);

        partial void OnTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TypeDescription");
        }
    }

    public partial class PhysicianAddress
    {
        private List<FacilityBranch> _Branches;

        public string DisplayExternalID
        {
            get
            {
                if (string.IsNullOrEmpty(ExternalID))
                {
                    return string.Empty;
                }

                return "  (" + ExternalID + ")";
            }
        }

        public bool IsAttendingPhysicianBranch
        {
            get
            {
                var isAttendingBranch = false;

                if (FacilityBranchRelated)
                {
                    if (AttendingPhysician)
                    {
                        if ((FacilityBranchKey != null) & (FacilityBranchKey > 0))
                        {
                            isAttendingBranch = true;
                        }
                    }
                }

                return isAttendingBranch;
            }
        }

        public bool BranchVisibility
        {
            get
            {
                if (ShowFacility)
                {
                    return false;
                }

                if (Physician != null && string.IsNullOrEmpty(Physician.FacilityName))
                {
                    return false;
                }

                return true;
            }
        }

        public bool ShowFacility
        {
            get
            {
                if (string.IsNullOrEmpty(TypeDescription) == false && TypeDescription.ToLower().Contains("facility"))
                {
                    return true;
                }

                return false;
            }
        }

        public bool ShowFacilityBranch
        {
            get
            {
                RaisePropertyChanged("Branches");
                if (string.IsNullOrEmpty(TypeDescription) == false && TypeDescription.ToLower().Contains("facility"))
                {
                    return true;
                }

                if (string.IsNullOrEmpty(TypeDescription) == false &&
                    TypeDescription.ToLower().Contains("branch office") &&
                    FacilityBranchRelated && Physician != null && string.IsNullOrEmpty(Physician.FacilityName) == false)
                {
                    return true;
                }

                return false;
            }
        }

        public string TypeDescription => Type == null ? null : CodeLookupCache.GetCodeDescriptionFromKey((int)Type);

        public string StateCodeCode => CodeLookupCache.GetCodeFromKey(StateCode);

        public string CityStateZip
        {
            get
            {
                try
                {
                    var cityStateZip = string.Format("{0}, {1}     {2}", City, StateCodeCode, ZipCode);
                    return cityStateZip.Trim() == "," ? null : cityStateZip;
                }
                catch
                {
                    return null;
                }
            }
        }

        public string CityStateZip2
        {
            get
            {
                try
                {
                    var cityStateZip = FormatHelper.FormatCityStateZip(City, StateCodeCode, ZipCode);
                    return string.IsNullOrEmpty(cityStateZip) ? null : cityStateZip;
                }
                catch
                {
                    return null;
                }
            }
        }

        public List<FacilityBranch> Branches
        {
            get
            {
                if (_Branches == null)
                {
                    _Branches = FacilityCache.GetActiveBranches();
                }

                if (_Branches == null)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(TypeDescription) == false &&
                    TypeDescription.ToLower().Contains("branch office") && Physician.FacilityKey != null &&
                    FacilityBranchRelated)
                {
                    return _Branches.Where(a => a.FacilityKey == Physician.FacilityKey && a.Inactive == false).ToList();
                }

                if (FacilityKey != null)
                {
                    return _Branches.Where(a => a.FacilityKey == FacilityKey && a.Inactive == false).ToList();
                }

                return null;
            }
        }

        public string BranchName { get; set; }

        public string FullAddress
        {
            get
            {
                var address = string.Empty;
                var CR = char.ToString('\r');
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

        public string AddressPhones
        {
            get
            {
                var addressPhones = string.Empty;
                var CR = char.ToString('\r');
                var faxNumber = AddressFaxNumber;
                if (!string.IsNullOrWhiteSpace(faxNumber))
                {
                    addressPhones = addressPhones + (string.IsNullOrWhiteSpace(addressPhones) ? "" : CR) + faxNumber;
                }

                var phoneNumber = AddressPhoneNumber;
                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    addressPhones = addressPhones + (string.IsNullOrWhiteSpace(addressPhones) ? "" : CR) + phoneNumber;
                }

                return addressPhones;
            }
        }

        public string AddressFaxNumber => string.IsNullOrWhiteSpace(Fax) ? null : "Fax     " + PhoneConvert(Fax);

        public string AddressPhoneNumber
        {
            get
            {
                var number = string.Empty;
                if (!string.IsNullOrWhiteSpace(PhoneNumber))
                {
                    number = PhoneConvert(PhoneNumber);
                }

                if (string.IsNullOrWhiteSpace(number) == false)
                {
                    number = number + (string.IsNullOrWhiteSpace(PhoneExtension) == false ? " x" + PhoneExtension : "");
                }

                return string.IsNullOrWhiteSpace(number) ? null : "Phone  " + number;
            }
        }

        partial void OnExternalIDChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DisplayExternalID");
        }

        partial void OnFacilityBranchRelatedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowFacility");
            RaisePropertyChanged("ShowFacilityBranch");
            RaisePropertyChanged("BranchVisibility");
        }

        partial void OnOrderDeliveryMethodChanging(int? value)
        {
            if (IsDeserializing)
            {
                return;
            }

            if (value == null)
            {
                return;
            }

            var prevOrderDeliveryMethod = CodeLookupCache.GetCodeDescriptionFromKey(OrderDeliveryMethod);
            var newOrderDeliveryMethod = CodeLookupCache.GetCodeDescriptionFromKey(value);

            /* The destination field is only editable when the OrderDeliveryMethod is Mail, Messenger, or Fax
             *      When the OrderDeliveryMethod is Mail or Messenger then the Destination is populated from a combobox
             *      that represents printers. Destination will be a string of the form '\\domain\PrinterName\'
             *      
             *      When OrderDeliveryMethod is Fax then Destination is populated from the user in a TextBox with
             *      a phone numeric filter. Destination will be a string of the form 'XXX.XXX.XXXX'
             *      
             *      The Destination must be nulled when the OrderDeliveryMethod is changed from Mail or Messenger to
             *      Fax to avoid populating the TextBox with a string of form '\\domain\PrinterName\'
             *      
             * Secondly, if the OrderDeliveryMethod is anything other than Mail, Messenger, or Fax then the Destination field
             * should not be enterable by the User so we will null out the previous value
             *
             * */
            if (newOrderDeliveryMethod != null && newOrderDeliveryMethod.ToLower() == "fax" &&
                prevOrderDeliveryMethod != null && (prevOrderDeliveryMethod.ToLower() == "mail" ||
                                                    prevOrderDeliveryMethod.ToLower() == "messenger"))
            {
                Destination = null;
            }
            else if (newOrderDeliveryMethod != null && newOrderDeliveryMethod.ToLower() != "fax" &&
                     newOrderDeliveryMethod.ToLower() != "mail" && newOrderDeliveryMethod.ToLower() != "messenger")
            {
                Destination = null;
            }
        }

        partial void OnTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetTrackingGroup();

            // If the user is adding an address of type facility then that address cannot be facilitybranchrelated
            if (TypeDescription != null && TypeDescription.ToLower().Contains("facility") == false &&
                FacilityBranchRelated)
            {
                FacilityBranchRelated = false;
                RaisePropertyChanged("FacilityBranchRelated");
            }

            if (TypeDescription != null && TypeDescription.ToLower().Contains("branch office") == false &&
                FacilityBranchRelated)
            {
                FacilityBranchRelated = false;
                RaisePropertyChanged("FacilityBranchRelated");
            }

            RaisePropertyChanged("TypeDescription");
            RaisePropertyChanged("ShowFacility");
            RaisePropertyChanged("ShowFacilityBranch");
            RaisePropertyChanged("BranchVisibility");
        }

        partial void OnCityChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
        }

        partial void OnCountyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetTrackingGroup();
        }

        partial void OnStateCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetTrackingGroup();
            RaisePropertyChanged("CityStateZip");
            RaisePropertyChanged("StateCodeCode");
        }

        partial void OnZipCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SetTrackingGroup();
            RaisePropertyChanged("CityStateZip");
        }

        partial void OnFacilityBranchKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("BranchName");
            SetBranchName();
        }

        partial void OnFacilityKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (FacilityBranchRelated == false && FacilityKey == null)
            {
                FacilityBranchKey = null;
            }

            RaisePropertyChanged("Branches");
            RaisePropertyChanged("ShowFacilityBranches");
        }

        private void SetBranchName()
        {
            if (FacilityBranchKey != null)
            {
                BranchName = FacilityCache.GetFacilityBranchName(FacilityBranchKey);
            }
        }

        private void SetTrackingGroup()
        {
            if (IsEditting)
            {
                var orderTrackingManager = new OrderTrackingManager();
                TrackingGroup = orderTrackingManager.FindDefaultTrackingGroupForPhysician(this);
            }
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

    public partial class PhysicianAlternateID
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

    public partial class PhysicianLicense
    {
        public string ExpiresWithPrefix
        {
            get
            {
                if (Expiration.HasValue)
                {
                    return "Expires: " + Expiration.Value.ToString("MM/dd/yyyy");
                }

                return string.Empty;
            }
        }

        public string StateCodeCode =>
            StateCode == null ? null : CodeLookupCache.GetCodeDescriptionFromKey((int)StateCode);

        partial void OnStateCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("StateCodeCode");
        }
    }

    public class PhysicianDisplay
    {
        public Physician Physician { get; set; }
        public AdmissionPhysician AdmissionPhysician { get; set; }

        public PhysicianAddress DisplayAddress
        {
            get
            {
                PhysicianAddress retAddr = null;
                if (AdmissionPhysician != null)
                {
                    retAddr = AdmissionPhysician.PhysicianAddressProxy;
                }

                if (retAddr == null && Physician != null)
                {
                    retAddr = Physician.MainAddress;
                }

                return retAddr;
            }
        }

        public string DisplayAddressLabel
        {
            get
            {
                var retAddr = "Main Address";
                if (AdmissionPhysician != null && AdmissionPhysician.PhysicianAddressProxy != null)
                {
                    retAddr = "Mailing Address";
                }

                return retAddr;
            }
        }
    }

    public partial class PhysicianAddressInfo
    {
        public string AddressPhoneNumber
        {
            get
            {
                string number = string.Empty;
                if (!string.IsNullOrWhiteSpace(PhoneNumber)) number = PhoneConvert(PhoneNumber);
                if (string.IsNullOrWhiteSpace(number) == false) number = number + (string.IsNullOrWhiteSpace(PhoneExtension) == false ? " x" + PhoneExtension : "");

                return string.IsNullOrWhiteSpace(number) ? null : number;
            }
        }
        private string PhoneConvert(string number)
        {
            PhoneConverter pc = new PhoneConverter();
            object phoneObject = pc.Convert(number, null, null, null);
            if ((phoneObject != null) && (string.IsNullOrWhiteSpace(phoneObject.ToString()) == false)) return phoneObject.ToString();
            return null;
        }
    }
}