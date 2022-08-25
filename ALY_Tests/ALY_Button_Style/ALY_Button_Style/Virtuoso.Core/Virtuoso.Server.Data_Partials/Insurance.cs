#region Usings

using System;
using System.Linq;
using Virtuoso.Core;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Utility;
using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class InsuranceVerificationPOCO
    {
        public bool CanEditStatus
        {
            get
            {
                if (RoleAccessHelper.CheckPermission(RoleAccess.InsuranceCoordinatorAdmin, false))
                {
                    return true;
                }

                if (WorklistHistory == null)
                {
                    return true; // No histories have been entered, anyone can edit
                }

                if (WorklistHistory.Any() == false)
                {
                    return true; // No histories have been entered, anyone can edit
                }

                var user = UserCache.Current.GetCurrentUserProfile();
                if (user == null)
                {
                    return false;
                }

                if (WorklistHistory.Where(u => u.UpdatedBy == user.UserId).Any())
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanEditStatusOld
        {
            get
            {
                if (RoleAccessHelper.CheckPermission(RoleAccess.InsuranceCoordinatorAdmin, false))
                {
                    return true;
                }

                if (WorklistHistoryGUIDs == null)
                {
                    return true; // No histories have been entered, anyone can edit
                }

                if (WorklistHistoryGUIDs.Any() == false)
                {
                    return true; // No histories have been entered, anyone can edit
                }

                var user = UserCache.Current.GetCurrentUserProfile();
                if (user == null)
                {
                    return false;
                }

                if (WorklistHistoryGUIDs.Contains(user.UserId))
                {
                    return true;
                }

                return false;
            }
        }

        public string InsuranceName
        {
            get
            {
                if (InsuranceKey == null)
                {
                    return "";
                }

                return InsuranceCache.GetInsuranceNameFromKey(InsuranceKey);
            }
        }

        public string DisciplineName
        {
            get
            {
                if (DisciplineKey == null)
                {
                    return "<General>";
                }

                return DisciplineCache.GetDescriptionFromKey((int)DisciplineKey);
            }
        }

        public bool HasHistory => WorklistHistory != null && WorklistHistory.Any();

        public string ResponseFrom271
        {
            get
            {
                string responseFrom271 = null;

                if (!this.Verified)
                {
                    responseFrom271 = "Denial";
                }

                //if (hasAdditionalInsurance)
                //{
                //    responseFrom271 += (string.IsNullOrEmpty(responseFrom271) ? " " : ", ") + "Additional Insurance";
                //}
                return responseFrom271;
            }
        }

        public string FullNameWithSuffix
        {
            get
            {
                string name = String.Format("{0}{1},{2}{3}",
                    ((this.LastName == null) ? "" : this.LastName.Trim()),
                    ((this.Suffix == null) ? "" : " " + this.Suffix.Trim()),
                    ((this.FirstName == null) ? "" : " " + this.FirstName.Trim()),
                    ((this.MiddleName == null) ? "" : " " + this.MiddleName.Trim())).Trim();
                if ((name == ",") || (name == "")) name = " ";
                if (name == "All,") name = "All";
                return name;
            }
        }

        public string FullNameInformal
        {
            get
            {
                return FormatHelper.FormatName(this.LastName, this.FirstName, this.MiddleName);
            }
        }

        public string MRNAndAdmissionIDLabel
        {
            get
            {
                return this.MRN + " - " + this.AdmissionID;
            }
        }

        public string SLGFullName
        {
            get
            {
                string name = "";
                if (string.IsNullOrWhiteSpace(this.SLGName0) == false) name += this.SLGName0;
                if (string.IsNullOrWhiteSpace(this.SLGName1) == false) name += " / " + this.SLGName1;
                if (string.IsNullOrWhiteSpace(this.SLGName2) == false) name += " / " + this.SLGName2;
                if (string.IsNullOrWhiteSpace(this.SLGName3) == false) name += " / " + this.SLGName3;
                if (string.IsNullOrWhiteSpace(this.SLGName4) == false) name += " / " + this.SLGName4;

                if (name == null) return " ";
                if (name.Trim() == "") return " ";
                return name.Trim();
            }
        }
    }

    public partial class Insurance
    {
        public string HidePeriodsPrompt => "Do Not Display Periods?";

        public string HomeHealthPrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "2");

        public string HospicePrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "4");

        public string HomeCarePrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "8");

        public bool ShowHomeHealthHidePeriodOption => IsValidForHomeHealth && !DisciplineOrders;

        public bool ShowHospiceHidePeriodOption => IsValidForHospice && !DisciplineOrders;

        public bool ShowHomeCareHidePeriodOption => IsValidForHomeCare && !DisciplineOrders;

        public bool IsHomeHealthLocked => IsValidForHomeHealth && !IsNew;

        // Used to display * in SearchResultsView
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

        public bool IsValidForHomeHealth
        {
            get
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;

                var tst = ServiceLineTypeUseBits;
                return (tst & homeHealthBit) > 0; // Is Valid for HomeHealth
            }
            set
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;
                var hospiceBit = (int)eServiceLineType.Hospice;
                var homeCareBit = (int)eServiceLineType.HomeCare;

                var tst = ServiceLineTypeUseBits;
                if (value)
                {
                    tst = tst | homeHealthBit;
                }
                else // See if the insurance is being used for any HomeHealth admissions?
                {
                    tst = tst & (hospiceBit | homeCareBit);
                }

                ServiceLineTypeUseBits = tst;

                SharedBitChanges();
            }
        }

        public bool IsHospiceLocked => IsValidForHospice && !IsNew;

        public bool IsValidForHospice
        {
            get
            {
                var hospiceBit = (int)eServiceLineType.Hospice;
                var tst = ServiceLineTypeUseBits;
                return (tst & hospiceBit) > 0; // Is Valid for Hospice
            }
            set
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;
                var hospiceBit = (int)eServiceLineType.Hospice;
                var homeCareBit = (int)eServiceLineType.HomeCare;

                var tst = ServiceLineTypeUseBits;
                if (value)
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

        public bool IsHomeCareLocked => IsValidForHomeCare && !IsNew;

        public bool IsValidForHomeCare
        {
            get
            {
                var homeCareBit = (int)eServiceLineType.HomeCare;
                var tst = ServiceLineTypeUseBits;
                return (tst & homeCareBit) > 0; // Is Valid for HomeCare
            }
            set
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;
                var hospiceBit = (int)eServiceLineType.Hospice;
                var homeCareBit = (int)eServiceLineType.HomeCare;

                var tst = ServiceLineTypeUseBits;
                if (value)
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

        public bool HideHomeHealthPeriods
        {
            get
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;

                var tst = HidePeriodsUseBits;
                return (tst & homeHealthBit) > 0; // Is Valid for HomeHealth
            }
            set
            {
                if (HideHomeHealthPeriods != value)
                {
                    var homeHealthBit = (int)eServiceLineType.HomeHealth;
                    var hospiceBit = (int)eServiceLineType.Hospice;
                    var homeCareBit = (int)eServiceLineType.HomeCare;

                    var tst = HidePeriodsUseBits;
                    if (value)
                    {
                        tst = tst | homeHealthBit;
                    }
                    else
                    {
                        tst = tst & (hospiceBit | homeCareBit);
                    }

                    HidePeriodsUseBits = tst;
                    SharedBitChanges();
                }
            }
        }

        public bool HideHospicePeriods
        {
            get
            {
                var hospiceBit = (int)eServiceLineType.Hospice;
                var tst = HidePeriodsUseBits;
                return (tst & hospiceBit) > 0; // Is Valid for HomeCare
            }
            set
            {
                if (HideHospicePeriods != value)
                {
                    var homeHealthBit = (int)eServiceLineType.HomeHealth;
                    var hospiceBit = (int)eServiceLineType.Hospice;
                    var homeCareBit = (int)eServiceLineType.HomeCare;

                    var tst = HidePeriodsUseBits;
                    if (value)
                    {
                        tst = tst | hospiceBit;
                    }
                    else
                    {
                        tst = tst & (homeCareBit | homeHealthBit);
                    }

                    HidePeriodsUseBits = tst;
                    SharedBitChanges();
                }
            }
        }

        public bool HideHomeCarePeriods
        {
            get
            {
                var homeCareBit = (int)eServiceLineType.HomeCare;
                var tst = HidePeriodsUseBits;
                return (tst & homeCareBit) > 0; // Is Valid for HomeCare
            }
            set
            {
                if (HideHomeCarePeriods != value)
                {
                    var homeHealthBit = (int)eServiceLineType.HomeHealth;
                    var hospiceBit = (int)eServiceLineType.Hospice;
                    var homeCareBit = (int)eServiceLineType.HomeCare;

                    var tst = HidePeriodsUseBits;
                    if (value)
                    {
                        tst = tst | homeCareBit;
                    }
                    else
                    {
                        tst = tst & (hospiceBit | homeHealthBit);
                    }

                    HidePeriodsUseBits = tst;
                    SharedBitChanges();
                }
            }
        }

        public bool IsHomeHealth => IsValidForHomeHealth || IsValidForHomeCare;

        public bool IsHospiceOnly
        {
            get
            {
                var result = IsValidForHospice && !IsHomeHealth;
                return result;
            }
        }

        public bool IsHomeHealthOrHomeCareOnly
        {
            get
            {
                var result = IsHomeHealth && !IsValidForHospice;
                return result;
            }
        }

        public string NameWithIDCode => Name + (string.IsNullOrWhiteSpace(IDCode) ? "" : "  (" + IDCode + ")");

        public bool ShowPOCCertStatement => DisciplineOrders;

        public bool ShowElectionAddendumAvailable => RequireBenefitElection;

        public bool ShowNoticeOfElectionNote
        {
            get
            {
                if (IsValidForHospice)
                {
                    var LastCertDef = InsuranceCertDefinition.OrderByDescending(o => o.PeriodNumber).FirstOrDefault();
                    if (LastCertDef != null && LastCertDef.IsNoticeOfElectionRequired)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public string CertificationPeriodsLabel => IsHospiceOnly ? "Periods of Care" : "Certification Periods";

        public string EditName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    return string.Format("Insurance {0}", Name.Trim());
                }

                return IsNew ? "New Insurance" : "Edit Insurance";
            }
        }

        public string InsuranceTypeCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(InsuranceType);

        public string InsuranceTypeCode => CodeLookupCache.GetCodeFromKey(InsuranceType);

        public bool IsMedicare
        {
            get
            {
                var typeCode = InsuranceTypeCode;
                return typeCode == "1" || typeCode == "2" || typeCode == "12";
            }
        }

        public bool IsMedicaid
        {
            get
            {
                var typeCode = InsuranceTypeCode;
                return typeCode == "3" || typeCode == "4" || typeCode == "13";
            }
        }

        public bool InsuranceTypeIsMedicareFFS
        {
            get
            {
                var itc = InsuranceTypeCode;
                if (string.IsNullOrWhiteSpace(itc))
                {
                    return false;
                }

                return itc == "1" ? true : false;
            }
        }

        public string StateCodeCode => CodeLookupCache.GetCodeFromKey(StateCode);

        public string StateCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(StateCode);

        public string FormsRequiredSelectedValues
        {
            get
            {
                if (FormsRequired == null)
                {
                    return null;
                }

                string ret = null;
                var cfList = CMSFormCache.GetActiveVersionOfCMSForms(false, DateTime.Today.Date);
                var formsRequiredSplit = FormsRequired.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var code in formsRequiredSplit)
                {
                    var cf = cfList.Where(c => c.Name == code.Trim()).FirstOrDefault();
                    if (cf != null)
                    {
                        ret = ret + (ret == null ? null : ", ") + cf.Name.Trim() + " - " + cf.Description.Trim();
                    }
                }

                return ret;
            }
            set
            {
                string formsrequired = null;
                if (value != null)
                {
                    var valueSplit = value.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var form in valueSplit)
                    {
                        var nameSplit = form.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                        if (nameSplit != null && nameSplit.Count() > 1 &&
                            string.IsNullOrWhiteSpace(nameSplit[0]) == false)
                        {
                            formsrequired = formsrequired + (formsrequired == null ? null : " - ") +
                                            nameSplit[0].Trim();
                        }
                    }
                }

                FormsRequired = formsrequired;
            }
        }

        public bool IsOASISReviewRequiredForRFA(string RFA)
        {
            if (string.IsNullOrWhiteSpace(RFA))
            {
                return false;
            }

            if (OASIS == false || OASISReviewRequired == false)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(OASISReviewRequiredRFACodes))
            {
                return false;
            }

            if (("|" + OASISReviewRequiredRFACodes + "|").Contains("|" + RFA + "|"))
            {
                return true;
            }

            return false;
        }

        partial void OnNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EditName");
            RaisePropertyChanged("NameWithIDCode");
        }

        partial void OnIDCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("NameWithIDCode");
        }

        partial void OnServiceLineTypeUseBitsChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SharedBitChanges();
        }

        partial void OnDisciplineOrdersChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SharedBitChanges();
        }

        private void SetupPOCCertStatement()
        {
            if (ShowPOCCertStatement == false)
            {
                POCCertStatement = null;
            }

            RaisePropertyChanged("ShowPOCCertStatement");
            // default  POCCertStatement if need be
            if (ShowPOCCertStatement && InsuranceTypeIsMedicareFFS && string.IsNullOrWhiteSpace(POCCertStatement))
            {
                POCCertStatement =
                    "I certify this patient is confined to his/her home and needs skilled nursing care, physical therapy and/or speech therapy, or continues to need occupational therapy.  The patient is under my care, and I have authorized the services on this plan, and will periodically review the plan.  The patient had a face-to-face encounter with an allowed provider type on {0} and the encounter was related to the primary reason for home health care.";
            }
        }

        public void SharedBitChanges()
        {
            if (!IsHospiceOnly)
            {
                LevelOfCareModel = false;
            }

            if (IsHomeHealthOrHomeCareOnly)
            {
                RequireBenefitElection = false;
                ElectionAddendumAvailable = false;
                RequireCertOfTerminalIllness = false;
                RequireFaceToFaceHospice = false;
            }

            if (IsHospiceOnly)
            {
                OASIS = false;
                FaceToFaceOnAdmit = false;
            }

            if (HideHomeHealthPeriods && (!IsValidForHomeHealth || DisciplineOrders))
            {
                HideHomeHealthPeriods = false;
            }

            if (HideHospicePeriods && (!IsValidForHospice || DisciplineOrders))
            {
                HideHospicePeriods = false;
            }

            if (HideHomeCarePeriods && (!IsValidForHomeCare || DisciplineOrders))
            {
                HideHomeCarePeriods = false;
            }

            RaisePropertyChanged("CertificationPeriodsLabel");
            RaisePropertyChanged("IsValidForHomeHealth");
            RaisePropertyChanged("IsValidForHospice");
            RaisePropertyChanged("IsValidForHomeCare");
            RaisePropertyChanged("IsHomeHealth");
            RaisePropertyChanged("ServiceLineTypeUseBits");
            RaisePropertyChanged("HideHomeHealthPeriods");
            RaisePropertyChanged("HideHospicePeriods");
            RaisePropertyChanged("HideHomeCarePeriods");
            RaisePropertyChanged("DisciplineOrders");
            RaisePropertyChanged("ShowHomeHealthHidePeriodOption");
            RaisePropertyChanged("ShowHospiceHidePeriodOption");
            RaisePropertyChanged("ShowHomeCareHidePeriodOption");
            RaisePropertyChanged("NoticeOfElectionNote");
            RaisePropertyChanged("ShowNoticeOfElectionNote");
            SetupPOCCertStatement();
        }

        partial void OnRequireBenefitElectionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (RequireBenefitElection == false)
            {
                ElectionAddendumAvailable = false;
            }

            RaisePropertyChanged("ShowElectionAddendumAvailable");
        }

        public void SignalCertChildChange()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("NoticeOfElectionNote");
            RaisePropertyChanged("ShowNoticeOfElectionNote");
        }

        public InsuranceCertDefinition GetNextCertDefinition(int? PdNum)
        {
            if (InsuranceCertDefinition == null || InsuranceCertDefinition.Any() == false)
            {
                return null;
            }

            return InsuranceCertDefinition.Where(cd => cd.PeriodNumber <= PdNum + 1)
                .OrderByDescending(od => od.PeriodNumber).FirstOrDefault();
        }

        partial void OnOASISChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (OASIS == false)
            {
                OASISReviewRequired = false;
            }
        }

        partial void OnOASISReviewRequiredChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (OASISReviewRequired == false)
            {
                OASISReviewRequiredRFACodes = null;
                OASISReviewRequiredRFADescriptions = null;
            }
        }

        partial void OnInsuranceTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("InsuranceTypeCode");
            RaisePropertyChanged("InsuranceTypeCodeDescription");
            SetupPOCCertStatement();
            if (IsMedicaid)
            {
                StateCode = null;
                EVVImplementationSID = null;
            }
        }

        partial void OnStateCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("StateCodeCode");
            RaisePropertyChanged("StateCodeDescription");
        }

        public void AddNewInsuranceCertStatement()
        {
            var i = new InsuranceCertStatement();
            i.EffectiveFromDate = DateTime.Today.Date;
            InsuranceCertStatement.Add(i);
        }

        public void RemoveInsuranceCertStatement(InsuranceCertStatement i)
        {
            InsuranceCertStatement.Remove(i);
        }

        public void InsuranceCertStatementChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            TriggerInsuranceCertStatementChanges();
        }

        public void TriggerInsuranceCertStatementChanges()
        {
            foreach (var item in InsuranceCertStatement) item.TriggeredChange();
            RaisePropertyChanged("InsuranceCertStatement");
        }

        public void AddNewInsuranceRecertStatement()
        {
            var i = new InsuranceRecertStatement();
            i.EffectiveFromDate = DateTime.Today.Date;
            InsuranceRecertStatement.Add(i);
        }

        public void RemoveInsuranceRecertStatement(InsuranceRecertStatement i)
        {
            InsuranceRecertStatement.Remove(i);
        }

        public void InsuranceRecertStatementChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            TriggerInsuranceRecertStatementChanges();
        }

        public void TriggerInsuranceRecertStatementChanges()
        {
            foreach (var item in InsuranceRecertStatement) item.TriggeredChange();
            RaisePropertyChanged("InsuranceRecertStatement");
        }
    }

    public partial class EVVImplementationPOCO
    {
        public string EVVImplementationNameDisplay => EVVImplementationName + (Inactive ? " (inactive)" : "");
    }

    public partial class InsuranceCertStatement
    {
        public bool Inactive => InactiveDate.HasValue;

        public string EffectiveThruDate
        {
            get
            {
                var result = "-";
                if (Insurance != null)
                {
                    var nextDate = Insurance.InsuranceCertStatement
                        .Where(w => !w.InactiveDate.HasValue &&
                                    w.InsuranceCertStatementKey != InsuranceCertStatementKey &&
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

                if (Insurance != null)
                {
                    Insurance.InsuranceCertStatementChanged();
                }
            }
        }

        partial void OnInsuranceCertStatementKeyChanged()
        {
            if (Insurance != null)
            {
                Insurance.InsuranceCertStatementChanged();
            }
        }

        partial void OnCertStatementChanged()
        {
            if (Insurance != null)
            {
                Insurance.UpdatedDate = DateTime.UtcNow;
            }
        }

        partial void OnEffectiveFromDateChanged()
        {
            if (Insurance != null)
            {
                Insurance.UpdatedDate = DateTime.UtcNow;
                Insurance.InsuranceCertStatementChanged();
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

    public partial class InsurancePPSParameter
    {
        public int ValueInt
        {
            get
            {
                var v = 0;
                try
                {
                    v = int.Parse(ParameterValue);
                }
                catch
                {
                }

                return v;
            }
        }

        public string InsuranceName => InsuranceCache.GetInsuranceNameFromKey(InsuranceKey);
    }

    public partial class InsuranceRecertStatement
    {
        public bool Inactive => InactiveDate.HasValue;

        public string EffectiveThruDate
        {
            get
            {
                var result = "-";
                if (Insurance != null)
                {
                    var nextDate = Insurance.InsuranceRecertStatement
                        .Where(w => !w.InactiveDate.HasValue &&
                                    w.InsuranceRecertStatementKey != InsuranceRecertStatementKey &&
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

                if (Insurance != null)
                {
                    Insurance.InsuranceRecertStatementChanged();
                }
            }
        }

        partial void OnInsuranceRecertStatementKeyChanged()
        {
            if (Insurance != null)
            {
                Insurance.InsuranceRecertStatementChanged();
            }
        }

        partial void OnRecertStatementChanged()
        {
            if (Insurance != null)
            {
                Insurance.UpdatedDate = DateTime.UtcNow;
            }
        }

        partial void OnEffectiveFromDateChanged()
        {
            if (Insurance != null)
            {
                Insurance.UpdatedDate = DateTime.UtcNow;
                Insurance.InsuranceRecertStatementChanged();
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
}