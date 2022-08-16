#region Usings

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Core;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Utility;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionReferral
    {
        private Encounter _currentEncounter;
        private string _facilityBranchName;
        private bool _mostRecent;
        private string _ReferralSourceCategoryCode;

        public Encounter CurrentEncounter
        {
            get { return _currentEncounter; }
            set
            {
                _currentEncounter = value;
                RaisePropertyChanged("CanEdit");
            }
        }

        public bool MostRecent
        {
            get { return _mostRecent; }
            set
            {
                _mostRecent = value;
                RaisePropertyChanged("MostRecent");
                RaisePropertyChanged("CanEdit");
            }
        }

        public bool CanEdit
        {
            get
            {
                if (CurrentEncounter != null)
                {
                    return false; // Cannot edit in dynamic form
                }

                if (IsNew)
                {
                    return true; // Can edit new ones
                }

                // SysAdmin can edit most recent
                if (RoleAccessHelper.CheckPermission(RoleAccess.Admin, false))
                {
                    return MostRecent ? true : false; // can only edit the most recent
                }

                // Non SysAdmin can only edit most recent - if Admission is in R=Referred status
                if (Admission == null)
                {
                    return false;
                }

                if (Admission.OriginalAdmissionStatusCode == "R")
                {
                    return MostRecent ? true : false; // can only edit the most recent
                }

                return false;
            }
        }

        public bool PhysicianAddressValidationRequired
        {
            get
            {
                if (!IsCategoryDOC || PhysicianKey.HasValue == false)
                {
                    return false;
                }

                var addr = PhysicianCache.Current.GetActivePhysicianAddressesForPhysicianPlusMe(PhysicianKey.Value);

                return addr != null && addr.Any();
            }
        }

        private bool ClientValidateReferralDate
        {
            get
            {
                // the referral date in invalid if Admission is transferred and its a Medicare PPS patient and 
                // the referral data is more than 2 days past the end of the cert period that was on file at the time of the transfer
                if (Admission == null)
                {
                    return true;
                }

                if (Admission.AdmissionStatusCode != "T")
                {
                    return true;
                }

                if (Admission.IsMedicarePPSPatient == false)
                {
                    return true;
                }

                if (Admission.MostRecentTransfer == null)
                {
                    return true;
                }

                if (Admission.MostRecentTransfer.TransferDate == null)
                {
                    return true;
                }

                var transferDate = Admission.MostRecentTransfer.TransferDate.Date;
                if (transferDate == null)
                {
                    return true;
                }

                var referralDate = ((DateTime)ReferralDate).Date;
                if (referralDate == null)
                {
                    return true;
                }

                var ac = Admission.GetAdmissionCertForDate(transferDate);
                if (ac == null)
                {
                    return true;
                }

                var periodEndDate = ((DateTime)ac.PeriodEndDate).Date;
                if (periodEndDate == null)
                {
                    return true;
                }

                if (referralDate <= periodEndDate.AddDays(2))
                {
                    return true;
                }

                return false;
            }
        }

        public bool FacilityBranchVisible
        {
            get
            {
                if (!IsCategoryFAC)
                {
                    return false;
                }

                if (FacilityKey == null)
                {
                    return false;
                }

                var fac = FacilityCache.GetFacilityFromKey(FacilityKey);
                if (fac == null)
                {
                    return false;
                }

                if (fac.FacilityBranch == null)
                {
                    return false;
                }

                return fac.FacilityBranch.Any();
            }
        }

        public string FacilityBranchName
        {
            get { return FacilityCache.GetFacilityBranchName(FacilityBranchKey); }
            set { _facilityBranchName = value; }
        }

        public string ReferralSourceCategoryCode
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_ReferralSourceCategoryCode))
                {
                    _ReferralSourceCategoryCode = CodeLookupCache.GetCodeFromKey(ReferralSourceCategory);
                }

                return _ReferralSourceCategoryCode;
            }
        }

        public bool IsCategorySELF => string.IsNullOrWhiteSpace(ReferralSourceCategoryCode) ? false :
            ReferralSourceCategoryCode.Trim().ToUpper() == "SELF" ? true : false;

        public bool IsCategoryFAM => string.IsNullOrWhiteSpace(ReferralSourceCategoryCode) ? false :
            ReferralSourceCategoryCode.Trim().ToUpper() == "FAM" ? true : false;

        public bool IsCategoryEMP => string.IsNullOrWhiteSpace(ReferralSourceCategoryCode) ? false :
            ReferralSourceCategoryCode.Trim().ToUpper() == "EMP" ? true : false;

        public bool IsCategoryDOC => string.IsNullOrWhiteSpace(ReferralSourceCategoryCode) ? false :
            ReferralSourceCategoryCode.Trim().ToUpper() == "DOC" ? true : false;

        public bool IsCategoryFAC => string.IsNullOrWhiteSpace(ReferralSourceCategoryCode) ? false :
            IsCategorySELF || IsCategoryFAM || IsCategoryEMP || IsCategoryDOC ? false : true;

        public bool HasReferralNotes => string.IsNullOrWhiteSpace(ReferralNotes) ? false : true;

        public bool ClientValidate()
        {
            var AllValid = true;

            if (string.IsNullOrWhiteSpace(ReferralSourceCategoryType))
            {
                ValidationErrors.Add(new ValidationResult("Referral Status is required",
                    new[] { "ReferralSourceCategoryType" }));
                AllValid = false;
            }

            if (ReferralSourceCategoryType != null && ReferralSourceCategoryType.ToLower() == "physician")
            {
                // if a physician has no address or the all addresses are inactive then skip this validation
                if (PhysicianAddressValidationRequired)
                {
                    var addr = PhysicianCache.Current.GetPhysicianAddressFromKey(PhysicianAddressKey);

                    if (PhysicianAddressKey == null || addr != null && addr.Inactive)
                    {
                        ValidationErrors.Add(new ValidationResult(
                            "An active address must be selected for the Physician", new[] { "PhysicianAddressKey" }));
                        AllValid = false;
                    }
                }
            }

            if (IsCategoryFAM && string.IsNullOrWhiteSpace(PatientContactName))
            {
                ValidationErrors.Add(new ValidationResult("A Referred By family member is required",
                    new[] { "PatientContactName" }));
                AllValid = false;
            }
            else if (IsCategoryEMP && string.IsNullOrWhiteSpace(UserName))
            {
                ValidationErrors.Add(new ValidationResult("A Referred By employee is required", new[] { "UserName" }));
                AllValid = false;
            }
            else if (IsCategoryDOC && string.IsNullOrWhiteSpace(PhysicianName))
            {
                ValidationErrors.Add(new ValidationResult("A Referred By physician is required",
                    new[] { "PhysicianKey" }));
                AllValid = false;
            }
            else if (IsCategoryFAC && string.IsNullOrWhiteSpace(FacilityName))
            {
                ValidationErrors.Add(new ValidationResult("A Referred By facility is required",
                    new[] { "FacilityName" }));
                AllValid = false;
            }
            else if (IsCategoryFAC && !ValidateFacilityBranch())
            {
                ValidationErrors.Add(new ValidationResult("A Facility Branch is required.",
                    new[] { "FacilityBranchKey", "FacilityBranchName" }));
                AllValid = false;
            }

            if (ReferralDate == null || ReferralDate == null)
            {
                ValidationErrors.Add(new ValidationResult("Referral Date is required", new[] { "ReferralDate" }));
                AllValid = false;
            }
            else
            {
                var date = ((DateTime)ReferralDate).Date;
                if (date > DateTime.Today.Date)
                {
                    ValidationErrors.Add(new ValidationResult("Referral Date cannot be a future date",
                        new[] { "ReferralDate" }));
                    AllValid = false;
                }
                else if (Admission != null)
                {
                    if (Admission.AdmissionReferral.Where(referral => referral.HistoryKey == null).Where(ar =>
                                ar.ReferralDate != null && ((DateTime)ar.ReferralDate).Date == date && ar != this)
                            .FirstOrDefault() != null)
                    {
                        ValidationErrors.Add(new ValidationResult(
                            "Referral Date cannot be equal to the date of any other referral for this admission",
                            new[] { "ReferralDate" }));
                        AllValid = false;
                    }
                    else if (Admission.IsAdmissionStatusTransferred && Admission.DischargeDateTime != null &&
                             date >= ((DateTime)Admission.DischargeDateTime).Date == false)
                    {
                        ValidationErrors.Add(new ValidationResult(
                            "Referral Date cannot be before the admission transfer date", new[] { "ReferralDate" }));
                        AllValid = false;
                    }
                }
            }

            if (AllValid)
            {
                if (ClientValidateReferralDate == false)
                {
                    ValidationErrors.Add(new ValidationResult(
                        "For Medicare PPS patients the re-Referral Date cannot be after day 62.",
                        new[] { "ReferralDate" }));
                    AllValid = false;
                }
            }

            return AllValid;
        }

        private bool ValidateFacilityBranch()
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

        public void RaiseOnReferralSourceCategoryChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            _ReferralSourceCategoryCode = null;
            RaisePropertyChanged("ReferralSourceCategoryCode");
            RaisePropertyChanged("IsCategorySELF");
            RaisePropertyChanged("IsCategoryFAM");
            RaisePropertyChanged("IsCategoryEMP");
            RaisePropertyChanged("IsCategoryDOC");
            RaisePropertyChanged("IsCategoryFAC");

            if (IsCategorySELF == false)
            {
                PatientKey = null;
                PatientName = null;
            }

            if (IsCategoryFAM == false)
            {
                PatientContactKey = null;
                PatientContactName = null;
            }

            if (IsCategoryEMP == false)
            {
                UserID = null;
                UserName = null;
            }

            if (IsCategoryDOC == false)
            {
                PhysicianKey = null;
                PhysicianName = null;
            }

            if (IsCategoryFAC == false)
            {
                FacilityKey = null;
                FacilityName = null;
                ReferralSourceKey = null;
                ReferralSourceContactName = null;
            }

            if (IsCategorySELF)
            {
                if (Admission != null && Admission.Patient != null)
                {
                    PatientKey = Admission.Patient.PatientKey;
                    PatientName = Admission.Patient.FormattedName;
                }
            }
        }

        partial void OnReferralSourceCategoryChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaiseOnReferralSourceCategoryChanged();
        }

        partial void OnReferralSourceCategoryTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            FacilityKey = null;
            FacilityName = null;
            ReferralSourceKey = null;
            ReferralSourceContactName = null;
        }

        partial void OnFacilityNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            ReferralSourceKey = null;
            ReferralSourceContactName = null;
        }

        partial void OnFacilityKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            // Facility Drop Down Lists/Search
            // Switched from CodeLookupMulti to SmartCombo
            // CodeLookupMulti can bind to SelectedKeys and SelectedValues
            // SmartCombo can only bind SelectedValue - so need to update here, what the CodeLookupMulti updated via binding
            var _f = FacilityCache.GetFacilityFromKey(FacilityKey);
            if (_f != null)
            {
                FacilityName = _f.Name;
            }

            FacilityBranchKey = null;
            FacilityBranchName = null;
            RaisePropertyChanged("FacilityBranchName");
            RaisePropertyChanged("FacilityBranchVisible");
        }

        partial void OnFacilityBranchKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            FacilityBranchName = FacilityCache.GetFacilityBranchName(FacilityBranchKey);
            RaisePropertyChanged("FacilityBranchName");
        }

        partial void OnPhysicianKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }
            
            // Switched from CodeLookupMulti to SmartCombo
            // CodeLookupMulti can bind to SelectedKeys and SelectedValues
            // SmartCombo can only bind SelectedValue - so need to update here, what the CodeLookupMulti updated via binding
            var _f = PhysicianCache.Current.GetPhysicianFromKey(PhysicianKey);
            if (_f != null)
            {
                PhysicianName = _f.FullNameInformalWithSuffix;
                if (_f.PhysicianAddress.Count() == 1)
                {
                    PhysicianAddressKey = _f.PhysicianAddress.First().PhysicianAddressKey;
                }
            }
            else
            {
                PhysicianName = null;
            }

            RaisePropertyChanged("PhysicianAddressValidationRequired");
        }

        public void MyRejectChanges()
        {
            RejectChanges();
        }

        partial void OnReferralNotesChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("HasReferralNotes");
        }
    }
}