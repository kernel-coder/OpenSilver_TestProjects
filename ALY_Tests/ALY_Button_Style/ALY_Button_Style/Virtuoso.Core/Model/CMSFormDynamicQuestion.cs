#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Converters;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Model
{
    public class CMSFormPage
    {
        public int Page { get; set; }
        public CMSForm CMSForm { get; set; }

        public string CMSFormPageDataTemplate => (CMSForm == null)
            ? "CMSFormUndefined"
            : "CMSForm" + CMSForm.Name + CMSForm.Version + "_" + Page;

        public string CMSFormPageDataTemplatePrint => CMSFormPageDataTemplate + "Print";
        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject CMSFormPageDataTemplateLoaded
        {
            get
            {
                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }

                return DataTemplateHelper.LoadDataTemplate(CMSFormPageDataTemplate);
            }
        }

        public DependencyObject CMSFormPageDataTemplatePrintLoaded
        {
            get
            {
                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }

                return DataTemplateHelper.LoadDataTemplate(CMSFormPageDataTemplatePrint);
            }
        }
    }

    public class CMSFormQuestion : QuestionUI
    {
        private EncounterAttachedForm _EncounterAttachedForm;

        public EncounterAttachedForm EncounterAttachedForm
        {
            get { return _EncounterAttachedForm; }
            set
            {
                _EncounterAttachedForm = value;
                this.RaisePropertyChangedLambda(p => p.EncounterAttachedForm);
            }
        }

        private EncounterCMSForm _EncounterCMSForm;

        public EncounterCMSForm EncounterCMSForm
        {
            get { return _EncounterCMSForm; }
            set
            {
                _EncounterCMSForm = value;
                this.RaisePropertyChangedLambda(p => p.EncounterCMSForm);
            }
        }

        private List<EncounterCMSFormField> _FieldList;

        public List<EncounterCMSFormField> EncounterCMSFormFieldList
        {
            get { return _FieldList; }
            set
            {
                _FieldList = value;
                this.RaisePropertyChangedLambda(p => p.EncounterCMSFormFieldList);
            }
        }

        public EncounterCMSFormField FieldAdditionalInformation => GetFieldByDeltaFieldName("AdditionalInformation");
        public EncounterCMSFormField FieldAgencyAddress1 => GetFieldByDeltaFieldName("AgencyAddress1");
        public EncounterCMSFormField FieldAgencyAddress2 => GetFieldByDeltaFieldName("AgencyAddress2");
        public EncounterCMSFormField FieldAgencyCityStateZip => GetFieldByDeltaFieldName("AgencyCityStateZip");
        public EncounterCMSFormField FieldAgencyName => GetFieldByDeltaFieldName("AgencyName");
        public EncounterCMSFormField FieldAgencyPhone => GetFieldByDeltaFieldName("AgencyPhone");
        public EncounterCMSFormField FieldDLabel => GetFieldByDeltaFieldName("DLabel");
        public EncounterCMSFormField FieldDText => GetFieldByDeltaFieldName("DText");
        public EncounterCMSFormField FieldEffectiveDate => GetFieldByDeltaFieldName("EffectiveDate");
        public EncounterCMSFormField FieldEText => GetFieldByDeltaFieldName("EText");
        public EncounterCMSFormField FieldFText => GetFieldByDeltaFieldName("FText");
        public EncounterCMSFormField FieldGOption1 => GetFieldByDeltaFieldName("GOption1");
        public EncounterCMSFormField FieldGOption2 => GetFieldByDeltaFieldName("GOption2");
        public EncounterCMSFormField FieldGOption3 => GetFieldByDeltaFieldName("GOption3");
        public EncounterCMSFormField FieldItemsServices => GetFieldByDeltaFieldName("ItemsServices");
        public EncounterCMSFormField FieldOrderChange => GetFieldByDeltaFieldName("OrderChange");
        public EncounterCMSFormField FieldOrderStop => GetFieldByDeltaFieldName("OrderStop");
        public EncounterCMSFormField FieldPatientFullName => GetFieldByDeltaFieldName("PatientFullName");
        public EncounterCMSFormField FieldPatientMRNAdmissionID => GetFieldByDeltaFieldName("PatientMRNAdmissionID");
        public EncounterCMSFormField FieldPatientSignature => GetFieldByDeltaFieldName("PatientSignature");
        public EncounterCMSFormField FieldPatientSignatureDate => GetFieldByDeltaFieldName("PatientSignatureDate");
        public EncounterCMSFormField FieldPatientSignatureName => GetFieldByDeltaFieldName("PatientSignatureName");

        public EncounterCMSFormField FieldPatientSignatureFirstName =>
            GetFieldByDeltaFieldName("PatientSignatureFirstName", false);

        public EncounterCMSFormField FieldPatientSignatureLastName =>
            GetFieldByDeltaFieldName("PatientSignatureLastName", false);

        public EncounterCMSFormField FieldWitnessSignature => GetFieldByDeltaFieldName("WitnessSignature", false);

        public EncounterCMSFormField FieldWitnessSignatureDate =>
            GetFieldByDeltaFieldName("WitnessSignatureDate", false);

        public EncounterCMSFormField FieldWitnessSignatureName =>
            GetFieldByDeltaFieldName("WitnessSignatureName", false);

        public EncounterCMSFormField FieldPlanContactInformation => GetFieldByDeltaFieldName("PlanContactInformation");
        public EncounterCMSFormField FieldQIOContact => GetFieldByDeltaFieldName("QIOContact");
        public EncounterCMSFormField FieldReason => GetFieldByDeltaFieldName("Reason");
        public EncounterCMSFormField FieldServiceType => GetFieldByDeltaFieldName("ServiceType");
        public EncounterCMSFormField FieldStartDate => GetFieldByDeltaFieldName("StartDate");

        public EncounterCMSFormField FieldAttendingPhysicianCheck =>
            GetFieldByDeltaFieldName("AttendingPhysicianCheck");

        public EncounterCMSFormField FieldAttendingPhysician => GetFieldByDeltaFieldName("AttendingPhysician");
        public EncounterCMSFormField FieldEOBDate => GetFieldByDeltaFieldName("EOBDate");
        public EncounterCMSFormField FieldDENCDate => GetFieldByDeltaFieldName("DENCDate");
        public EncounterCMSFormField FieldDecisionFacts => GetFieldByDeltaFieldName("DecisionFacts");
        public EncounterCMSFormField FieldDetailedExplanation => GetFieldByDeltaFieldName("DetailedExplanation");
        public EncounterCMSFormField FieldDecisionRationale => GetFieldByDeltaFieldName("DecisionRationale");
        public EncounterCMSFormField FieldQIOPhone => GetFieldByDeltaFieldName("QIOPhone");
        public EncounterCMSFormField FieldPATCONConsentText => GetFieldByDeltaFieldName("PATCONConsentText", false);

        public EncounterCMSFormField FieldPATCONReleaseInformationQuestionText =>
            GetFieldByDeltaFieldName("PATCONReleaseInformationQuestionText", false);

        public EncounterCMSFormField FieldPATCONReleaseInformationQuestion =>
            GetFieldByDeltaFieldName("PATCONReleaseInformationQuestion", false);

        public EncounterCMSFormField FieldPATCONAdvancedDirectivesText =>
            GetFieldByDeltaFieldName("PATCONAdvancedDirectivesText", false);

        public EncounterCMSFormField FieldPATCONAdvancedDirectivesAHCD =>
            GetFieldByDeltaFieldName("PATCONAdvancedDirectivesAHCD", false);

        public EncounterCMSFormField FieldPATCONAdvancedDirectivesPOA =>
            GetFieldByDeltaFieldName("PATCONAdvancedDirectivesPOA", false);

        public EncounterCMSFormField FieldPATCONAdvancedDirectivesPOLST =>
            GetFieldByDeltaFieldName("PATCONAdvancedDirectivesPOLST", false);

        public EncounterCMSFormField FieldPATCONCoveredByText => GetFieldByDeltaFieldName("PATCONCoveredByText", false);
        public EncounterCMSFormField FieldPATCONCoveredBy => GetFieldByDeltaFieldName("PATCONCoveredBy", false);
        public EncounterCMSFormField FieldPATCONLiabilityText => GetFieldByDeltaFieldName("PATCONLiabilityText", false);

        public EncounterCMSFormField FieldPATCONIncludeWitnessSignature =>
            GetFieldByDeltaFieldName("PATCONIncludeWitnessSignature", false);

        public bool ProxyFieldAttendingPhysicianCheck
        {
            get
            {
                if ((Question == null) || (Question.LookupType != "MHES"))
                {
                    return false;
                }

                if (FieldAttendingPhysicianCheck == null)
                {
                    return false;
                }

                if (FieldAttendingPhysicianCheck.BoolData == null)
                {
                    FieldAttendingPhysicianCheck.BoolData = false;
                }

                return (bool)FieldAttendingPhysicianCheck.BoolData;
            }
            set
            {
                if ((Question == null) || (Question.LookupType != "MHES"))
                {
                    return;
                }

                if (FieldAttendingPhysicianCheck == null)
                {
                    return;
                }

                FieldAttendingPhysicianCheck.BoolData = value;
                this.RaisePropertyChangedLambda(p => p.ShowAttendingPhysician);
            }
        }

        public int? ProxyFieldAttendingPhysician
        {
            get
            {
                if ((Question == null) || (Question.LookupType != "MHES"))
                {
                    return null;
                }

                if (FieldAttendingPhysician == null)
                {
                    return null;
                }

                if (FieldAttendingPhysician.IntData <= 0)
                {
                    FieldAttendingPhysician.IntData = null;
                }

                return FieldAttendingPhysician.IntData;
            }
            set
            {
                if ((Question == null) || (Question.LookupType != "MHES"))
                {
                    return;
                }

                if (FieldAttendingPhysician == null)
                {
                    return;
                }

                FieldAttendingPhysician.IntData = value;
                if (FieldAttendingPhysician.IntData <= 0)
                {
                    FieldAttendingPhysician.IntData = null;
                }

                this.RaisePropertyChangedLambda(p => p.AttendingAdmissionPhysician);
            }
        }

        public DateTime? ProxyFieldEOBDate
        {
            get
            {
                if ((Question == null) || (Question.LookupType != "MHES"))
                {
                    return null;
                }

                if (FieldEOBDate == null)
                {
                    return null;
                }

                if (FieldEOBDate.DateTimeData == DateTime.MinValue)
                {
                    FieldEOBDate.BoolData = null;
                }

                return FieldEOBDate.DateTimeData;
            }
            set
            {
                if ((Question == null) || (Question.LookupType != "MHES"))
                {
                    return;
                }

                if (FieldEOBDate == null)
                {
                    return;
                }

                FieldEOBDate.DateTimeData = value;
                if (FieldEOBDate.DateTimeData == DateTime.MinValue)
                {
                    FieldEOBDate.DateTimeData = null;
                }

                if (FieldEOBDate.DateTimeData != null)
                {
                    FieldEOBDate.DateTimeData = ((DateTime)FieldEOBDate.DateTimeData).Date;
                }

                if (Admission != null)
                {
                    Admission.SOCDate = FieldEOBDate.DateTimeData;
                }

                if (Admission != null)
                {
                    Admission.HospiceEOBDate = FieldEOBDate.DateTimeData;
                }
            }
        }

        public byte[] ProxyFieldPatientSignature
        {
            get
            {
                if (FieldPatientSignature == null)
                {
                    return null;
                }

                return FieldPatientSignature.SignatureData;
            }
            set
            {
                if (FieldPatientSignature == null)
                {
                    return;
                }

                FieldPatientSignature.SignatureData = value;
                if (FieldPatientSignatureDate == null)
                {
                    return;
                }

                FieldPatientSignatureDate.DateTimeData = (FieldPatientSignature.SignatureData == null)
                    ? (DateTime?)null
                    : DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified).Date;
            }
        }

        public byte[] ProxyFieldWitnessSignature
        {
            get
            {
                if (FieldWitnessSignature == null)
                {
                    return null;
                }

                return FieldWitnessSignature.SignatureData;
            }
            set
            {
                if (FieldWitnessSignature == null)
                {
                    return;
                }

                FieldWitnessSignature.SignatureData = value;
                if (FieldWitnessSignatureDate == null)
                {
                    return;
                }

                FieldWitnessSignatureDate.DateTimeData = (FieldWitnessSignature.SignatureData == null)
                    ? (DateTime?)null
                    : DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified).Date;
            }
        }

        public AdmissionPhysician AttendingAdmissionPhysician
        {
            get
            {
                return ((FieldAttendingPhysician != null) && (FieldAttendingPhysician.IntData != null) &&
                        (Admission != null) && (Admission.AdmissionPhysician != null))
                    ? Admission.AdmissionPhysician
                        .Where(p => p.AdmissionPhysicianKey == (int)FieldAttendingPhysician.IntData).FirstOrDefault()
                    : null;
            }
        }

        public List<AdmissionPhysician> AttendingAdmissionPhysicianList { get; set; }

        public bool ShowAttendingPhysician => ProxyFieldAttendingPhysicianCheck ? false : true;

        private EncounterCMSFormField GetFieldByDeltaFieldName(string deltaFieldName, bool showErrorMessage = true)
        {
            if (EncounterCMSFormFieldList == null)
            {
                return null;
            }

            EncounterCMSFormField ecff = EncounterCMSFormFieldList.Where(p => p.DeltaFieldName == deltaFieldName)
                .FirstOrDefault();
            if ((ecff == null) && (showErrorMessage))
            {
                MessageBox.Show(String.Format(
                    "Error CMSFormDynamicQuestion.GetFieldByDeltaFieldName: DeltaFieldName {0} is not defined.  Contact your system administrator.",
                    deltaFieldName));
            }

            return ecff;
        }

        private CMSForm _CMSForm;

        public CMSForm CMSForm
        {
            get { return _CMSForm; }
            set
            {
                _CMSForm = value;
                this.RaisePropertyChangedLambda(p => p.CMSForm);
            }
        }

        private List<CMSFormPage> _CMSFormPageList;

        public List<CMSFormPage> CMSFormPageList
        {
            get { return _CMSFormPageList; }
            set
            {
                if (!Deployment.Current.CheckAccess())
                {
                    return;
                }

                if (_CMSFormPageList != value)
                {
                    _CMSFormPageList = value;
                    this.RaisePropertyChangedLambda(p => p.CMSFormPageList);
                }
            }
        }

        public List<CMSFormPage> CMSFormPrintPageList
        {
            get
            {
                List<CMSFormPage> cfppList = new List<CMSFormPage>();
                if (((CMSFormPageList == null) || (CMSFormPageList.Count < Page)) == false)
                {
                    cfppList.Add(CMSFormPageList[Page - 1]);
                }

                return cfppList;
            }
        }

        private int _Page = 1;

        public int Page
        {
            get { return _Page; }
            set
            {
                _Page = value;
                this.RaisePropertyChangedLambda(p => p.Page);
            }
        }

        public FormSection FormSection { get; set; }

        public IEnumerable<EncounterCMSFormField> LiabilityList
        {
            get
            {
                if (EncounterCMSFormFieldList == null)
                {
                    return null;
                }

                List<EncounterCMSFormField> ecffList = EncounterCMSFormFieldList
                    .Where(p => p.DeltaFieldName == "PATCONLiability").OrderBy(p => p.Sequence).ToList();
                return ecffList;
            }
        }

        public RelayCommand<string> ABNCheckBoxCommand { get; set; }
        public RelayCommand<string> HHCCNCheckBoxCommand { get; set; }

        public CMSFormQuestion(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public string CMSFormUndefinedMessage => "CMSForm for form type of " +
                                                 ((Question == null) ? "?" : Question.LookupType) + " is not defined";

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }

            PhoneConverter pc = new PhoneConverter();
            if (pc == null)
            {
                return null;
            }

            object phoneObject = pc.Convert(phoneNumber, null, null, null);
            if (phoneObject != null)
            {
                if (string.IsNullOrWhiteSpace(phoneObject.ToString()) == false)
                {
                    return phoneObject.ToString();
                }
            }

            return null;
        }

        private ServiceLineGrouping _ServiceLineGroupingZero;

        private ServiceLineGrouping ServiceLineGroupingZero
        {
            get
            {
                if (_ServiceLineGroupingZero != null)
                {
                    return _ServiceLineGroupingZero;
                }

                if ((Admission == null) || (Encounter == null))
                {
                    return null;
                }

                DateTime admissionGroupDate = (Encounter.EncounterOrTaskStartDateAndTime == null)
                    ? DateTime.Today.Date
                    : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;
                AdmissionGroup ag = Admission.GetNthCurrentGroup(0, admissionGroupDate);
                if (ag == null)
                {
                    return null;
                }

                _ServiceLineGroupingZero = ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey);
                return _ServiceLineGroupingZero;
            }
        }

        private string DefaultAgencyName
        {
            get
            {
                if ((ServiceLineGroupingZero != null) &&
                    (string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyName) == false))
                {
                    return ServiceLineGroupingZero.AgencyName;
                }

                return (string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.Name))
                    ? "?"
                    : TenantSettingsCache.Current.TenantSetting.Name;
            }
        }

        private string DefaultAgencyAddress1
        {
            get
            {
                if ((ServiceLineGroupingZero != null) &&
                    (string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress1) == false))
                {
                    return ServiceLineGroupingZero.AgencyAddress1;
                }

                return (string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.Address1))
                    ? "?"
                    : TenantSettingsCache.Current.TenantSetting.Address1;
            }
        }

        private string DefaultAgencyAddress2
        {
            get
            {
                if ((ServiceLineGroupingZero != null) &&
                    (string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress1) == false))
                {
                    return (string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress2))
                        ? null
                        : ServiceLineGroupingZero.AgencyAddress2;
                }

                return (string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.Address2))
                    ? null
                    : TenantSettingsCache.Current.TenantSetting.Address2;
            }
        }

        private string DefaultAgencyCityStateZip
        {
            get
            {
                if ((ServiceLineGroupingZero != null) &&
                    (string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress1) == false))
                {
                    return string.Format("{0}, {1}  {2}",
                        ((string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyCity))
                            ? "City ?"
                            : ServiceLineGroupingZero.AgencyCity),
                        ((string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyStateCodeCode))
                            ? "State ?"
                            : ServiceLineGroupingZero.AgencyStateCodeCode),
                        ((string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyZipCode))
                            ? "ZipCode ?"
                            : ServiceLineGroupingZero.AgencyZipCode));
                }

                return string.Format("{0}, {1}  {2}",
                    ((string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.City))
                        ? "City ?"
                        : TenantSettingsCache.Current.TenantSetting.City),
                    ((string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.StateCodeCode))
                        ? "State ?"
                        : TenantSettingsCache.Current.TenantSetting.StateCodeCode),
                    ((string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.ZipCode))
                        ? "ZipCode ?"
                        : TenantSettingsCache.Current.TenantSetting.ZipCode));
            }
        }

        private string DefaultAgencyPhone
        {
            get
            {
                if ((ServiceLineGroupingZero != null) &&
                    (string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyPhoneNumber) == false))
                {
                    return FormatPhoneNumber(ServiceLineGroupingZero.AgencyPhoneNumber);
                }

                return null;
            }
        }

        private string DefaultPatientMRNAdmissionID
        {
            get
            {
                if ((Patient != null) && (Admission != null))
                {
                    return string.Format("{0}{1}",
                        ((string.IsNullOrWhiteSpace(Patient.MRN)) ? "?" : Patient.MRN),
                        ((string.IsNullOrWhiteSpace(Admission.AdmissionID)) ? "" : "-" + Admission.AdmissionID));
                }

                return "?";
            }
        }

        private string DefaultPatientFullName
        {
            get
            {
                if (Patient != null)
                {
                    return (string.IsNullOrWhiteSpace(Patient.FullNameWithMiddleInitial)
                        ? "?"
                        : Patient.FullNameWithMiddleInitial);
                }

                return "?";
            }
        }

        private string DefaultQIOContact
        {
            get
            {
                if ((Admission != null) && (Encounter != null))
                {
                    DateTime admissionGroupDate = (Encounter.EncounterOrTaskStartDateAndTime == null)
                        ? DateTime.Today.Date
                        : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;
                    ServiceLineGrouping slg = Admission.GetFirstServiceLineGroupWithQIOName(admissionGroupDate);
                    if (slg != null)
                    {
                        string QIOName = (string.IsNullOrWhiteSpace(slg.QIOName)) ? "Contact ?" : slg.QIOName.Trim();
                        if (QIOName.Length > 40)
                        {
                            QIOName = QIOName.Substring(0, 40);
                        }

                        string slgPhoneNumber = FormatPhoneNumber(slg.QIOPhoneNumber);
                        return string.Format("{0} {1}", QIOName,
                            ((string.IsNullOrWhiteSpace(slgPhoneNumber)) ? "Phone Number ?" : slgPhoneNumber));
                    }
                }

                string tsPhoneNumber = FormatPhoneNumber(TenantSettingsCache.Current.TenantSetting.QIOPhoneNumber);
                return string.Format("{0} {1}",
                    ((string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.QIOName))
                        ? "Contact ?"
                        : TenantSettingsCache.Current.TenantSetting.QIOName),
                    ((string.IsNullOrWhiteSpace(tsPhoneNumber)) ? "Phone Number ?" : tsPhoneNumber));
            }
        }

        private string DefaultPATCONConsentText => (Admission == null)
            ? null
            : ServiceLineCache.GetConsentTextFromServiceLineKey(Admission.ServiceLineKey);

        private string DefaultPATCONReleaseInformationQuestionText => (Admission == null)
            ? null
            : ServiceLineCache.GetReleaseInformationQuestionTextFromServiceLineKey(Admission.ServiceLineKey);

        private string DefaultPATCONAdvancedDirectivesText => (Admission == null)
            ? null
            : ServiceLineCache.GetAdvancedDirectivesTextFromServiceLineKey(Admission.ServiceLineKey);

        private string DefaultPATCONAdvancedDirectivesAHCD
        {
            get
            {
                if ((Patient == null) || (Patient.AdvanceCarePlan == null))
                {
                    return "0";
                }

                Virtuoso.Server.Data.AdvanceCarePlan acp = Patient.AdvanceCarePlan.Where(a => a.HistoryKey == null)
                    .OrderByDescending(a => a.AdvanceCarePlanKey).FirstOrDefault();
                return ((acp == null)
                    ? null
                    : ((acp.HasAdvanceDirective.HasValue == false)
                        ? null
                        : ((acp.HasAdvanceDirective == false) ? "0" : "1")));
            }
        }

        private string DefaultPATCONAdvancedDirectivesPOA
        {
            get
            {
                if ((Patient == null) || (Patient.AdvanceCarePlan == null))
                {
                    return "0";
                }

                Virtuoso.Server.Data.AdvanceCarePlan acp = Patient.AdvanceCarePlan.Where(a => a.HistoryKey == null)
                    .OrderByDescending(a => a.AdvanceCarePlanKey).FirstOrDefault();
                return ((acp == null)
                    ? null
                    : ((acp.HasPowerOfAttorney.HasValue == false)
                        ? null
                        : ((acp.HasPowerOfAttorney == false) ? "0" : "1")));
            }
        }

        private string DefaultPATCONAdvancedDirectivesPOLST
        {
            get
            {
                if (Patient == null)
                {
                    return "0";
                }

                if (Patient.ActiveAdvancedDirectivesOfType("POLST") != null)
                {
                    return "1";
                }

                return "0";
            }
        }

        private string DefaultPATCONCoveredByText => (Admission == null)
            ? null
            : ServiceLineCache.GetCoveredByTextFromServiceLineKey(Admission.ServiceLineKey);

        private string DefaultPATCONCoveredBy
        {
            get
            {
                if ((Encounter == null) || (Patient == null) || (Patient.PatientInsurance == null) ||
                    (Admission == null) || (Admission.AdmissionCoverage == null))
                {
                    return "Unknown";
                }

                DateTime date = (Encounter.EncounterOrTaskStartDateAndTime == null)
                    ? DateTime.Today.Date
                    : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;
                List<string> cbList = new List<string>();
                PatientInsurance pi = null;
                List<AdmissionCoverage> acList = Admission.AdmissionCoverage.Where(a => a.IsActiveAsOfDate(date))
                    .ToList();
                if ((acList != null) && acList.Any())
                {
                    foreach (AdmissionCoverage ac in acList)
                    {
                        List<AdmissionCoverageInsurance> aciList = (ac.AdmissionCoverageInsurance == null)
                            ? null
                            : ac.AdmissionCoverageInsurance.Where(a => (a.HistoryKey == null) && (a.Inactive == false))
                                .ToList();
                        if ((aciList == null) || (aciList.Any() == false))
                        {
                            continue;
                        }

                        foreach (AdmissionCoverageInsurance aci in aciList)
                        {
                            pi = Patient.PatientInsurance.Where(i =>
                                ((i.Inactive == false) &&
                                 (i.HistoryKey == null) &&
                                 (i.PatientInsuranceKey == aci.PatientInsuranceKey) &&
                                 (string.IsNullOrWhiteSpace(i.InsuranceName) == false) &&
                                 ((i.EffectiveFromDate.HasValue == false) || (i.EffectiveFromDate.HasValue &&
                                                                              (i.EffectiveFromDate.Value.Date <=
                                                                               date))) &&
                                 ((i.EffectiveThruDate.HasValue == false) || (i.EffectiveThruDate.HasValue &&
                                                                              (i.EffectiveThruDate.Value.Date >=
                                                                               date))))).FirstOrDefault();
                            if ((pi != null) && (cbList.Contains(pi.InsuranceName.Trim()) == false))
                            {
                                cbList.Add(pi.InsuranceName.Trim());
                            }
                        }
                    }
                }

                // punt and return HIB if no active insurances in coverage plan(s)
                pi = (cbList.Count == 0)
                    ? null
                    : Patient.PatientInsurance.Where(i =>
                        ((i.PatientInsuranceKey == Admission.PatientInsuranceKey) &&
                         (string.IsNullOrWhiteSpace(i.InsuranceName) == false))).FirstOrDefault();
                if ((pi != null) && (cbList.Contains(pi.InsuranceName.Trim()) == false))
                {
                    cbList.Add(pi.InsuranceName.Trim());
                }

                if (cbList.Count == 0)
                {
                    return "Unknown";
                }

                cbList.Sort();
                return string.Join(", ", cbList);
            }
        }

        private string DefaultPATCONLiabilityText => (Admission == null)
            ? null
            : ServiceLineCache.GetLiabilityTextFromServiceLineKey(Admission.ServiceLineKey);

        private bool DefaultPATCONIncludeWitnessSignature => (Admission == null)
            ? false
            : ServiceLineCache.GetIncludeWitnessSignatureFromServiceLineKey(Admission.ServiceLineKey);

        private string DefaultPlanContactInformation
        {
            get
            {
                string pciDefault = "Name ? Phone Number ?";
                if ((Patient == null) || (Patient.PatientInsurance == null) || (Admission == null))
                {
                    return pciDefault;
                }

                PatientInsurance pi = Patient.PatientInsurance
                    .Where(p => p.PatientInsuranceKey == Admission.PatientInsuranceKey).FirstOrDefault();
                if (pi == null)
                {
                    return pciDefault;
                }

                Insurance i = InsuranceCache.GetInsuranceFromKey(pi.InsuranceKey);
                if (i == null)
                {
                    return pciDefault;
                }

                string pciPhoneNumber = FormatPhoneNumber(i.Number);
                return string.Format("{0} {1}",
                    ((string.IsNullOrWhiteSpace(i.Name)) ? "Name ?" : i.Name),
                    ((string.IsNullOrWhiteSpace(pciPhoneNumber)) ? "Phone Number ?" : pciPhoneNumber));
            }
        }

        public void CMSFormQuestionSetup(DynamicFormViewModel vm)
        {
            Hidden = CMSFormHidden;
            if ((Hidden) || (Patient == null) || (Admission == null) || (Question == null))
            {
                return;
            }

            // We don't prompt for the SOCDate in Hospice when we are using the 'Medicare Hospice Election Statement' (CMS) form - we default it from the form's EOBDate
            if (Admission.HospiceAdmission && (string.IsNullOrWhiteSpace(Question.LookupType) == false) &&
                (Question.LookupType.Trim().ToUpper() == "MHES") && (DynamicFormViewModel != null))
            {
                DynamicFormViewModel.CMSProtectSOCDate = true;
            }

            SetupEncounterAttachedForm();
            SetupEncounterCMSForm();
            SetupCMSFormPageList();
            SetupABNCheckBoxCommand();
            SetupHHCCNCheckBoxCommand();

            Messenger.Default.Register<int>(this, "AdmissionCoverage_FormUpdate",
                AdmissionKey => { SetupAdmittedAdmissionCoverageDefaults(AdmissionKey); });
            Messenger.Default.Register<int>(this, "AdmissionPhysician_FormUpdate",
                AdmissionKey => { SetupAdmittedAdmissionPhysicianDefaults(); });
            Messenger.Default.Register<Server.Data.AdvanceCarePlan>(this, "AdvanceCarePlanChanged",
                acp => AdvanceCarePlanChanged(acp));
            Messenger.Default.Register<bool>(this,
                string.Format("AdvanceCarePlanChangedPOLSTChanged{0}", Patient.PatientKey.ToString().Trim()),
                hasPOLST => AdvanceCarePlanPOLSTChanged(hasPOLST));
            SetupAdmittedAdmissionPhysicianDefaults();
        }

        public void AdvanceCarePlanChanged(Server.Data.AdvanceCarePlan acp)
        {
            if ((Patient == null) || (acp == null) || Patient.PatientKey != acp.PatientKey)
            {
                return;
            }

            if (FieldPATCONAdvancedDirectivesAHCD != null)
            {
                FieldPATCONAdvancedDirectivesAHCD.TextData = ((acp.HasAdvanceDirective.HasValue == false)
                    ? null
                    : ((acp.HasAdvanceDirective == false) ? "0" : "1"));
            }

            if (FieldPATCONAdvancedDirectivesPOA != null)
            {
                FieldPATCONAdvancedDirectivesPOA.TextData = ((acp.HasPowerOfAttorney.HasValue == false)
                    ? null
                    : ((acp.HasPowerOfAttorney == false) ? "0" : "1"));
            }
        }

        public void AdvanceCarePlanPOLSTChanged(bool hasPOLST)
        {
            if (FieldPATCONAdvancedDirectivesPOLST != null)
            {
                FieldPATCONAdvancedDirectivesPOLST.TextData = hasPOLST ? "1" : "0";
            }
        }

        public QuestionUI CloneForPrint(int page)
        {
            CMSFormQuestion cfq = new CMSFormQuestion(__FormSectionQuestionKey)
            {
                FormSection = FormSection,
                Section = Section,
                QuestionGroupKey = QuestionGroupKey,
                Question = Question,
                IndentLevel = IndentLevel,
                Patient = Patient,
                Encounter = Encounter,
                Admission = Admission,
                OasisManager = OasisManager,
                DynamicFormViewModel = DynamicFormViewModel
            };
            cfq.CMSForm = CMSForm;
            cfq.EncounterAttachedForm = EncounterAttachedForm;
            cfq.EncounterCMSForm = EncounterCMSForm;
            cfq.EncounterCMSFormFieldList = EncounterCMSFormFieldList;
            cfq.Hidden = Hidden;
            cfq.CMSFormPageList = CMSFormPageList;
            cfq.Page = page;
            return cfq;
        }

        private bool CMSFormHidden
        {
            get
            {
                // Determine if the question should be hidden 
                // If hidden, and this is the only question in the section – the section is hidden as well due to default DynamicForm processing (as required).

                // If EncounterAttachedForm already exists - show the form - (its legacy complete or in process)
                if ((Patient == null) || (Patient.PatientInsurance == null) || (Admission == null) ||
                    (Encounter == null) || (Encounter.EncounterAttachedForm == null) || (Question == null) ||
                    (string.IsNullOrWhiteSpace(Question.LookupType)))
                {
                    return true;
                }

                if (Encounter.EncounterAttachedForm.Where(p => (p.QuestionKey == Question.QuestionKey)).Any())
                {
                    return false;
                }

                if (((Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                     (Encounter.PreviousEncounterStatusIsInEdit)) == false)
                {
                    return true;
                }

                // Never hide Patient Consent if it is sited on the form
                if (Question.LookupType.ToUpper() == "PATCON")
                {
                    return false;
                }

                // All others - hidden is a function of Insurance
                PatientInsurance pi = Patient.PatientInsurance
                    .Where(p => p.PatientInsuranceKey == Admission.PatientInsuranceKey).FirstOrDefault();
                if (pi == null)
                {
                    return true;
                }

                Insurance i = InsuranceCache.GetInsuranceFromKey(pi.InsuranceKey);
                if ((i == null) || (string.IsNullOrWhiteSpace(i.FormsRequired)))
                {
                    return true;
                }

                if ((Question.LookupType.ToUpper() == "MHES") && (Admission.HospiceEOBDate != null))
                {
                    return true;
                }

                bool hidden = ((" - " + i.FormsRequired + " - ").Contains(" - " + Question.LookupType + " - "))
                    ? false
                    : true;
                return hidden;
            }
        }

        private void SetupABNCheckBoxCommand()
        {
            if (ABNCheckBoxCommand == null)
            {
                ABNCheckBoxCommand = new RelayCommand<string>(s =>
                {
                    switch (s.ToLower())
                    {
                        case "option1":
                            FieldGOption1.BoolData = true;
                            FieldGOption2.BoolData = false;
                            FieldGOption3.BoolData = false;
                            break;
                        case "option2":
                            FieldGOption1.BoolData = false;
                            FieldGOption2.BoolData = true;
                            FieldGOption3.BoolData = false;
                            break;
                        case "option3":
                            FieldGOption1.BoolData = false;
                            FieldGOption2.BoolData = false;
                            FieldGOption3.BoolData = true;
                            break;
                    }

                    this.RaisePropertyChangedLambda(p => p.FieldGOption1);
                    this.RaisePropertyChangedLambda(p => p.FieldGOption2);
                    this.RaisePropertyChangedLambda(p => p.FieldGOption3);
                });
            }
        }


        private void SetupHHCCNCheckBoxCommand()
        {
            if (HHCCNCheckBoxCommand == null)
            {
                HHCCNCheckBoxCommand = new RelayCommand<string>(s =>
                {
                    switch (s.ToLower())
                    {
                        case "option1":
                            FieldOrderChange.BoolData = true;
                            FieldOrderStop.BoolData = false;
                            break;
                        case "option2":
                            FieldOrderChange.BoolData = false;
                            FieldOrderStop.BoolData = true;
                            break;
                    }

                    this.RaisePropertyChangedLambda(p => p.FieldOrderChange);
                    this.RaisePropertyChangedLambda(p => p.FieldOrderStop);
                });
            }
        }

        private void SetupEncounterAttachedForm()
        {
            // - Fetch or default  the EncounterAttachForm row, 
            if ((Question == null) || (FormSection == null) || (Encounter == null) ||
                (Encounter.EncounterAttachedForm == null))
            {
                return;
            }

            EncounterAttachedForm = Encounter.EncounterAttachedForm.Where(p => (p.QuestionKey == Question.QuestionKey))
                .FirstOrDefault();
            if (EncounterAttachedForm != null)
            {
                return;
            }

            if (((Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                 (Encounter.PreviousEncounterStatusIsInEdit)) == false)
            {
                return;
            }

            // need to default
            Form cmsForm = DynamicFormCache.GetCMSFormByName(Question.Label);
            if (cmsForm == null)
            {
                return;
            }

            EncounterAttachedForm = new EncounterAttachedForm
            {
                QuestionKey = Question.QuestionKey, FormSectionKey = FormSection.FormSectionKey,
                FormKey = cmsForm.FormKey
            };
        }

        private void SetupEncounterCMSForm()
        {
            // - Fetch or default  the EncounterCMSForm row, and the EncounterCMSFormField rows using the Question.LookupType to derive the CMSForm.CMSFormKey row 
            //   (take version effective date into account).
            if ((Question == null) || (Encounter == null) || (Encounter.EncounterCMSForm == null) ||
                (Encounter.EncounterCMSFormField == null))
            {
                return;
            }

            EncounterCMSForm = Encounter.EncounterCMSForm.Where(p => (p.QuestionKey == Question.QuestionKey))
                .FirstOrDefault();
            if (EncounterCMSForm != null)
            {
                CMSForm = CMSFormCache.GetCMSFormByKey(EncounterCMSForm.CMSFormKey);
                EncounterCMSFormFieldList = Encounter.EncounterCMSFormField
                    .Where(p => (p.EncounterCMSFormKey == EncounterCMSForm.EncounterCMSFormKey)).ToList();
                DefaultCMSFormFieldsIfNeedBe(true);
                RaiseChangedEncounterCMSFormFields();
                return;
            }

            if (((Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                 (Encounter.PreviousEncounterStatusIsInEdit)) == false)
            {
                return;
            }

            // need to default = fetch appropriate version of the CMSForm from and create/default the EncounterCMSForm row and EncounterCMSFormField rows
            DateTime effectiveDate = (Encounter.EncounterOrTaskStartDateAndTime == null)
                ? DateTime.Today.Date
                : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;
            CMSForm = CMSFormCache.GetActiveVersionOfCMSForm(Question.LookupType, effectiveDate);
            EncounterCMSFormFieldList = new List<EncounterCMSFormField>();
            if (CMSForm == null)
            {
                RaiseChangedEncounterCMSFormFields();
                return;
            }

            EncounterCMSForm = new EncounterCMSForm
            {
                QuestionKey = Question.QuestionKey, SectionKey = Section.SectionKey, CMSFormKey = CMSForm.CMSFormKey
            };
            if (CMSForm.CMSFormField == null)
            {
                return;
            }

            bool includeRIQ = (DefaultPATCONReleaseInformationQuestionText == null) ? false : true;
            bool includeAD = (DefaultPATCONAdvancedDirectivesText == null) ? false : true;
            bool includeCB = (DefaultPATCONCoveredByText == null) ? false : true;
            bool includeL = (DefaultPATCONLiabilityText == null) ? false : true;
            bool includeWS = DefaultPATCONIncludeWitnessSignature;
            foreach (CMSFormField cff in CMSForm.CMSFormField)
            {
                // Within the PatientConsent form - only include child Liability fields if we are including that section
                if ((includeL == false) && (cff.DeltaFieldName == "PATCONLiability"))
                {
                    continue;
                }

                if (includeL && (cff.DeltaFieldName == "PATCONLiability"))
                {
                    SetupPATCONLiability(cff);
                    continue;
                }

                EncounterCMSFormField ecff = new EncounterCMSFormField { CMSFormFieldKey = cff.CMSFormFieldKey };
                if (ecff.DataType_IsBool)
                {
                    ecff.BoolData = false;
                }

                // Within the PatientConsent form - only include child fields if we are including that section
                if ((includeRIQ == false) && (cff.DeltaFieldName == "PATCONReleaseInformationQuestion"))
                {
                    continue;
                }

                if ((includeAD == false) && (cff.DeltaFieldName == "PATCONAdvancedDirectivesAHCD"))
                {
                    continue;
                }

                if ((includeAD == false) && (cff.DeltaFieldName == "PATCONAdvancedDirectivesPOA"))
                {
                    continue;
                }

                if ((includeAD == false) && (cff.DeltaFieldName == "PATCONAdvancedDirectivesPOLST"))
                {
                    continue;
                }

                if ((includeCB == false) && (cff.DeltaFieldName == "PATCONCoveredBy"))
                {
                    continue;
                }

                if ((includeWS == false) && (cff.DeltaFieldName == "WitnessSignature"))
                {
                    continue;
                }

                if ((includeWS == false) && (cff.DeltaFieldName == "WitnessSignatureDate"))
                {
                    continue;
                }

                if ((includeWS == false) && (cff.DeltaFieldName == "WitnessSignatureName"))
                {
                    continue;
                }

                EncounterCMSFormFieldList.Add(ecff);
            }

            DefaultCMSFormFieldsIfNeedBe(false);
        }

        private void SetupPATCONLiability(CMSFormField cff)
        {
            EncounterCMSFormField ecff = new EncounterCMSFormField
                { CMSFormFieldKey = cff.CMSFormFieldKey, Sequence = 1, Text2Data = "Deductible" };
            EncounterCMSFormFieldList.Add(ecff);

            ICollection<AdmissionDiscipline> adList = Admission.ActiveAdmissionDisciplinesReferredOrAdmitted;
            if (adList != null)
            {
                adList = adList.OrderBy(p => p.AdmissionDisciplineHCFACode).ThenBy(p => p.DisciplineDescription)
                    .ToList();
                int i = 2;
                foreach (AdmissionDiscipline ad in adList)
                {
                    ecff = new EncounterCMSFormField
                        { CMSFormFieldKey = cff.CMSFormFieldKey, Sequence = i++, Text2Data = ad.DisciplineDescription };
                    EncounterCMSFormFieldList.Add(ecff);
                }
            }
        }

        private void DefaultCMSFormFieldsIfNeedBe(bool reEdit)
        {
            if ((Encounter == null) || (EncounterCMSFormFieldList == null))
            {
                return;
            }

            if (((Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                 (Encounter.PreviousEncounterStatusIsInEdit)) == false)
            {
                return;
            }

            foreach (EncounterCMSFormField ecff in EncounterCMSFormFieldList)
                if (ecff.DeltaFieldName == "AgencyName")
                {
                    ecff.TextData = DefaultAgencyName;
                }
                else if (ecff.DeltaFieldName == "AgencyAddress1")
                {
                    ecff.TextData = DefaultAgencyAddress1;
                }
                else if (ecff.DeltaFieldName == "AgencyAddress2")
                {
                    ecff.TextData = DefaultAgencyAddress2;
                }
                else if (ecff.DeltaFieldName == "AgencyCityStateZip")
                {
                    ecff.TextData = DefaultAgencyCityStateZip;
                }
                else if (ecff.DeltaFieldName == "AgencyPhone")
                {
                    ecff.TextData = DefaultAgencyPhone;
                }
                else if (ecff.DeltaFieldName == "PatientMRNAdmissionID")
                {
                    ecff.TextData = DefaultPatientMRNAdmissionID;
                }
                else if (ecff.DeltaFieldName == "PatientFullName")
                {
                    ecff.TextData = DefaultPatientFullName;
                }
                else if (ecff.DeltaFieldName == "QIOContact")
                {
                    ecff.TextData = DefaultQIOContact;
                }
                else if (ecff.DeltaFieldName == "PlanContactInformation")
                {
                    ecff.TextData = DefaultPlanContactInformation;
                }
                else if ((reEdit == false) && (ecff.DeltaFieldName == "PATCONConsentText"))
                {
                    ecff.TextData = DefaultPATCONConsentText;
                }
                else if ((reEdit == false) && (ecff.DeltaFieldName == "PATCONReleaseInformationQuestionText"))
                {
                    ecff.TextData = DefaultPATCONReleaseInformationQuestionText;
                }
                else if ((reEdit == false) && (ecff.DeltaFieldName == "PATCONAdvancedDirectivesText"))
                {
                    ecff.TextData = DefaultPATCONAdvancedDirectivesText;
                }
                else if (ecff.DeltaFieldName == "PATCONAdvancedDirectivesAHCD")
                {
                    ecff.TextData = DefaultPATCONAdvancedDirectivesAHCD;
                }
                else if (ecff.DeltaFieldName == "PATCONAdvancedDirectivesPOA")
                {
                    ecff.TextData = DefaultPATCONAdvancedDirectivesPOA;
                }
                else if (ecff.DeltaFieldName == "PATCONAdvancedDirectivesPOLST")
                {
                    ecff.TextData = DefaultPATCONAdvancedDirectivesPOLST;
                }
                else if ((reEdit == false) && (ecff.DeltaFieldName == "PATCONCoveredByText"))
                {
                    ecff.TextData = DefaultPATCONCoveredByText;
                }
                else if (ecff.DeltaFieldName == "PATCONCoveredBy")
                {
                    ecff.TextData = DefaultPATCONCoveredBy;
                }
                else if ((reEdit == false) && (ecff.DeltaFieldName == "PATCONLiabilityText"))
                {
                    ecff.TextData = DefaultPATCONLiabilityText;
                }
                else if ((ecff.DeltaFieldName == "PATCONIncludeWitnessSignature"))
                {
                    ecff.BoolData = DefaultPATCONIncludeWitnessSignature;
                }

            RaiseChangedEncounterCMSFormFields();
        }

        private void SetupCMSFormPageList()
        {
            CMSFormPageList = new List<CMSFormPage>();
            if (CMSForm == null)
            {
                return;
            }

            for (int i = 1; i <= CMSForm.Pages; i++)
            {
                CMSFormPage cfp = new CMSFormPage { Page = i, CMSForm = CMSForm };
                CMSFormPageList.Add(cfp);
            }
        }

        private void RaiseChangedEncounterCMSFormFields()
        {
            this.RaisePropertyChangedLambda(p => p.FieldAdditionalInformation);
            this.RaisePropertyChangedLambda(p => p.FieldAgencyAddress1);
            this.RaisePropertyChangedLambda(p => p.FieldAgencyAddress2);
            this.RaisePropertyChangedLambda(p => p.FieldAgencyCityStateZip);
            this.RaisePropertyChangedLambda(p => p.FieldAgencyName);
            this.RaisePropertyChangedLambda(p => p.FieldAgencyPhone);
            this.RaisePropertyChangedLambda(p => p.FieldDLabel);
            this.RaisePropertyChangedLambda(p => p.FieldDText);
            this.RaisePropertyChangedLambda(p => p.FieldEffectiveDate);
            this.RaisePropertyChangedLambda(p => p.FieldEText);
            this.RaisePropertyChangedLambda(p => p.FieldFText);
            this.RaisePropertyChangedLambda(p => p.FieldGOption1);
            this.RaisePropertyChangedLambda(p => p.FieldGOption2);
            this.RaisePropertyChangedLambda(p => p.FieldGOption3);
            this.RaisePropertyChangedLambda(p => p.FieldItemsServices);
            this.RaisePropertyChangedLambda(p => p.FieldOrderChange);
            this.RaisePropertyChangedLambda(p => p.FieldOrderStop);
            this.RaisePropertyChangedLambda(p => p.FieldPatientFullName);
            this.RaisePropertyChangedLambda(p => p.FieldPatientMRNAdmissionID);
            this.RaisePropertyChangedLambda(p => p.FieldPatientSignature);
            this.RaisePropertyChangedLambda(p => p.FieldPatientSignatureDate);
            this.RaisePropertyChangedLambda(p => p.FieldPatientSignatureName);
            this.RaisePropertyChangedLambda(p => p.FieldPatientSignatureFirstName);
            this.RaisePropertyChangedLambda(p => p.FieldPatientSignatureLastName);
            this.RaisePropertyChangedLambda(p => p.FieldWitnessSignature);
            this.RaisePropertyChangedLambda(p => p.FieldWitnessSignatureDate);
            this.RaisePropertyChangedLambda(p => p.FieldWitnessSignatureName);
            this.RaisePropertyChangedLambda(p => p.FieldPlanContactInformation);
            this.RaisePropertyChangedLambda(p => p.FieldQIOContact);
            this.RaisePropertyChangedLambda(p => p.FieldReason);
            this.RaisePropertyChangedLambda(p => p.FieldServiceType);
            this.RaisePropertyChangedLambda(p => p.FieldStartDate);
            this.RaisePropertyChangedLambda(p => p.FieldAttendingPhysicianCheck);
            this.RaisePropertyChangedLambda(p => p.FieldAttendingPhysician);
            this.RaisePropertyChangedLambda(p => p.FieldEOBDate);
            this.RaisePropertyChangedLambda(p => p.FieldDENCDate);
            this.RaisePropertyChangedLambda(p => p.FieldDecisionFacts);
            this.RaisePropertyChangedLambda(p => p.FieldDetailedExplanation);
            this.RaisePropertyChangedLambda(p => p.FieldDecisionRationale);
            this.RaisePropertyChangedLambda(p => p.FieldQIOPhone);
            this.RaisePropertyChangedLambda(p => p.FieldPATCONConsentText);
            this.RaisePropertyChangedLambda(p => p.FieldPATCONReleaseInformationQuestionText);
            this.RaisePropertyChangedLambda(p => p.FieldPATCONReleaseInformationQuestion);
            this.RaisePropertyChangedLambda(p => p.FieldPATCONAdvancedDirectivesText);
            this.RaisePropertyChangedLambda(p => p.FieldPATCONAdvancedDirectivesAHCD);
            this.RaisePropertyChangedLambda(p => p.FieldPATCONAdvancedDirectivesPOA);
            this.RaisePropertyChangedLambda(p => p.FieldPATCONAdvancedDirectivesPOLST);
            this.RaisePropertyChangedLambda(p => p.FieldPATCONCoveredByText);
            this.RaisePropertyChangedLambda(p => p.FieldPATCONCoveredBy);
            this.RaisePropertyChangedLambda(p => p.FieldPATCONLiabilityText);
            this.RaisePropertyChangedLambda(p => p.FieldPATCONIncludeWitnessSignature);

            this.RaisePropertyChangedLambda(p => p.ProxyFieldPatientSignature);
            this.RaisePropertyChangedLambda(p => p.ProxyFieldWitnessSignature);
            this.RaisePropertyChangedLambda(p => p.ProxyFieldAttendingPhysicianCheck);
            this.RaisePropertyChangedLambda(p => p.ProxyFieldAttendingPhysician);
            this.RaisePropertyChangedLambda(p => p.ProxyFieldEOBDate);
            this.RaisePropertyChangedLambda(p => p.AttendingAdmissionPhysician);

            this.RaisePropertyChangedLambda(p => p.LiabilityList);
        }

        private bool AnyFieldHasData
        {
            get
            {
                if (EncounterCMSFormFieldList == null || EncounterCMSFormFieldList.Any() == false)
                {
                    return false;
                }

                foreach (EncounterCMSFormField ecff in EncounterCMSFormFieldList)
                {
                    if ((ecff.DataType_IsText) && (string.IsNullOrWhiteSpace(ecff.TextData)))
                    {
                        ecff.TextData = null;
                    }

                    if ((ecff.DataType_IsDate) && (ecff.DateTimeData == DateTime.MinValue))
                    {
                        ecff.DateTimeData = null;
                    }

                    if ((ecff.DataType_IsInt) && (ecff.IntData < 0))
                    {
                        ecff.IntData = null;
                    }

                    if ((ecff.DataType_IsYesNo) && (string.IsNullOrWhiteSpace(ecff.TextData)))
                    {
                        ecff.TextData = null;
                    }
                }

                foreach (EncounterCMSFormField ecff in EncounterCMSFormFieldList)
                    if (ecff.ReadOnly == false)
                    {
                        if ((ecff.DataType_IsText) && (string.IsNullOrWhiteSpace(ecff.TextData) == false))
                        {
                            return true; // data in text field
                        }

                        if ((ecff.DataType_IsBool) && (ecff.BoolData.HasValue && ecff.BoolData == true))
                        {
                            return true; // data in bool field
                        }

                        if ((ecff.DataType_IsDate) && (ecff.DateTimeData != null))
                        {
                            return true; // data in datatime field
                        }

                        if ((ecff.DataType_IsSignature) && (ecff.SignatureData != null))
                        {
                            return true; // data in signature field
                        }

                        if ((ecff.DataType_IsInt) && (ecff.IntData != null))
                        {
                            return true; // data in int field
                        }

                        if ((ecff.DataType_IsYesNo) && (string.IsNullOrWhiteSpace(ecff.TextData) == false))
                        {
                            return true; // data in yes/no field
                        }
                    }

                return false;
            }
        }

        private void EncounterAddFormIfNeedBe()
        {
            // Add the associated EncounterAttachedForm, EncounterCMSForm and EncounterCMSFormField entities from the data context if need be
            if ((DynamicFormViewModel == null) || (Encounter == null) || (Encounter.EncounterAttachedForm == null) ||
                (Encounter.EncounterCMSForm == null) || (Encounter.EncounterCMSFormField == null))
            {
                return;
            }

            if ((EncounterAttachedForm != null) &&
                (Encounter.EncounterAttachedForm.Contains(EncounterAttachedForm) == false))
            {
                Encounter.EncounterAttachedForm.Add(EncounterAttachedForm);
            }

            if ((EncounterCMSForm != null) && (Encounter.EncounterCMSForm.Contains(EncounterCMSForm) == false))
            {
                Encounter.EncounterCMSForm.Add(EncounterCMSForm);
            }

            foreach (EncounterCMSFormField ecff in EncounterCMSFormFieldList)
            {
                if (Encounter.EncounterCMSFormField.Contains(ecff) == false)
                {
                    Encounter.EncounterCMSFormField.Add(ecff);
                }

                if ((EncounterCMSForm != null) && (EncounterCMSForm.EncounterCMSFormField.Contains(ecff) == false))
                {
                    EncounterCMSForm.EncounterCMSFormField.Add(ecff);
                }
            }
        }

        private void EncounterRemoveFormIfNeedBe()
        {
            if (((Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                 (Encounter.PreviousEncounterStatusIsInEdit)) == false)
            {
                return;
            }

            // Remove the associated EncounterAttachedForm, EncounterCMSForm and EncounterCMSFormField entities from the data context if need be
            if ((DynamicFormViewModel == null) || (Encounter == null) || (Encounter.EncounterAttachedForm == null) ||
                (Encounter.EncounterCMSForm == null) || (Encounter.EncounterCMSFormField == null))
            {
                return;
            }

            if ((EncounterAttachedForm != null) && (Encounter.EncounterAttachedForm.Contains(EncounterAttachedForm)))
            {
                Encounter.EncounterAttachedForm.Remove(EncounterAttachedForm);
                ((IPatientService)DynamicFormViewModel.FormModel).Remove(EncounterAttachedForm);
            }

            if ((EncounterCMSForm != null) && (Encounter.EncounterCMSForm.Contains(EncounterCMSForm)))
            {
                Encounter.EncounterCMSForm.Remove(EncounterCMSForm);
                ((IPatientService)DynamicFormViewModel.FormModel).Remove(EncounterCMSForm);
            }

            foreach (EncounterCMSFormField ecff in EncounterCMSFormFieldList)
            {
                if (((EncounterCMSForm != null) && EncounterCMSForm.EncounterCMSFormField.Contains(ecff)))
                {
                    EncounterCMSForm.EncounterCMSFormField.Remove(ecff);
                }

                if (Encounter.EncounterCMSFormField.Contains(ecff))
                {
                    Encounter.EncounterCMSFormField.Remove(ecff);
                    ((IPatientService)DynamicFormViewModel.FormModel).Remove(ecff);
                }
            }

            SetupEncounterAttachedForm();
            SetupEncounterCMSForm();
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            if (EncounterCMSFormFieldList != null)
            {
                foreach (EncounterCMSFormField ecff in EncounterCMSFormFieldList) ecff.ValidationErrors.Clear();
            }

            if (Hidden)
            {
                return true;
            }

            if (AnyFieldHasData)
            {
                EncounterAddFormIfNeedBe();
            }
            else
            {
                if (Encounter.FullValidation)
                {
                    EncounterRemoveFormIfNeedBe(); // No CMSFormFields that are NOT ReadOnly are valued, assume the form has not been filled out and clean up
                }

                return true;
            }

            if (Encounter.FullValidation == false)
            {
                return true;
            }

            bool AllValid = true;
            // Apply required field validation
            foreach (EncounterCMSFormField ecff in EncounterCMSFormFieldList)
            {
                if ((ecff.DataType_IsText) && (string.IsNullOrWhiteSpace(ecff.TextData)))
                {
                    ecff.TextData = null;
                }

                if ((ecff.DataType_IsDate) && (ecff.DateTimeData == DateTime.MinValue))
                {
                    ecff.DateTimeData = null;
                }

                if ((ecff.DataType_IsInt) && (ecff.IntData < 0))
                {
                    ecff.IntData = null;
                }

                if ((ecff.DataType_IsYesNo) && (string.IsNullOrWhiteSpace(ecff.TextData)))
                {
                    ecff.TextData = null;
                }

                if (ecff.Required)
                {
                    if ((ecff.DataType_IsText) && (string.IsNullOrWhiteSpace(ecff.TextData)))
                    {
                        // text field required
                        AllValid = false;
                        ecff.ValidationErrors.Add(new ValidationResult(ecff.PDFFieldNameLabel + " is required.",
                            new[] { "TextData" }));
                    }
                    else if ((ecff.DataType_IsBool) && (ecff.BoolData == false))
                    {
                        // bool field required
                        AllValid = false;
                        ecff.ValidationErrors.Add(new ValidationResult(ecff.PDFFieldNameLabel + " is required.",
                            new[] { "BoolData" }));
                    }
                    else if ((ecff.DataType_IsDate) && (ecff.DateTimeData == null))
                    {
                        // datatime field required
                        AllValid = false;
                        ecff.ValidationErrors.Add(new ValidationResult(ecff.PDFFieldNameLabel + " is required.",
                            new[] { "DateTimeData" }));
                    }
                    else if ((ecff.DataType_IsSignature) && (ecff.SignatureData == null))
                    {
                        // signature field required
                        AllValid = false;
                        ecff.ValidationErrors.Add(new ValidationResult(ecff.PDFFieldNameLabel + " is required.",
                            new[] { "SignatureData" }));
                    }
                    else if ((ecff.DataType_IsInt) && (ecff.IntData == null))
                    {
                        // int field required
                        AllValid = false;
                        ecff.ValidationErrors.Add(new ValidationResult(ecff.PDFFieldNameLabel + " is required.",
                            new[] { "IntData" }));
                    }
                    else if ((ecff.DataType_IsYesNo) && (string.IsNullOrWhiteSpace(ecff.TextData)))
                    {
                        // text field required
                        AllValid = false;
                        ecff.ValidationErrors.Add(new ValidationResult(ecff.PDFFieldNameLabel + " is required.",
                            new[] { "TextData" }));
                    }
                }
            }

            // If ABN, validate one and only one GOption1, GOption2, GOption3 is checked
            if ((Question.LookupType == "ABN") && (FieldGOption1 != null) && (FieldGOption2 != null) &&
                (FieldGOption3 != null))
            {
                int checkCount = 0;
                if ((bool)FieldGOption1.BoolData)
                {
                    checkCount++;
                }

                if ((bool)FieldGOption2.BoolData)
                {
                    checkCount++;
                }

                if ((bool)FieldGOption3.BoolData)
                {
                    checkCount++;
                }

                if (checkCount != 1)
                {
                    AllValid = false;
                    FieldGOption1.ValidationErrors.Add(
                        new ValidationResult("One and only one G Option must be checked.", new[] { "BoolData" }));
                    FieldGOption2.ValidationErrors.Add(
                        new ValidationResult("One and only one G Option must be checked.", new[] { "BoolData" }));
                    FieldGOption3.ValidationErrors.Add(
                        new ValidationResult("One and only one G Option must be checked.", new[] { "BoolData" }));
                }
            }

            // If DENC, validate that the FieldDENCDate is not in the future
            if ((Question.LookupType == "DENC") && (FieldDENCDate != null) && (FieldDENCDate.DateTimeData != null))
            {
                if (FieldDENCDate.DateTimeData.Value.Date >
                    DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified).Date)
                {
                    AllValid = false;
                    FieldDENCDate.ValidationErrors.Add(new ValidationResult("The DENC Date cannot be a future date.",
                        new[] { "DateTimeData" }));
                }
            }

            // If HHCCN, validate one and only one of OrderChange, OrderStop is checked
            if ((Question.LookupType == "HHCCN") && (FieldOrderChange != null) && (FieldOrderStop != null))
            {
                int checkCount = 0;
                if ((bool)FieldOrderChange.BoolData)
                {
                    checkCount++;
                }

                if ((bool)FieldOrderStop.BoolData)
                {
                    checkCount++;
                }

                if (checkCount != 1)
                {
                    AllValid = false;
                    FieldOrderChange.ValidationErrors.Add(new ValidationResult(
                        "One and only one Orders Changed/Stopped option must be checked.", new[] { "BoolData" }));
                    FieldOrderStop.ValidationErrors.Add(new ValidationResult(
                        "One and only one Orders Changed/Stopped option must be checked.", new[] { "BoolData" }));
                }
            }

            // If MHES, validate if Attending Physician is checked - we have an Attending Physician
            if ((Question.LookupType == "MHES") && (FieldAttendingPhysicianCheck != null) &&
                (FieldAttendingPhysician != null))
            {
                // Validate AttendingPhysician fields
                if (FieldAttendingPhysicianCheck.BoolData == null)
                {
                    FieldAttendingPhysicianCheck.BoolData = false;
                }

                if ((bool)FieldAttendingPhysicianCheck.BoolData)
                {
                    FieldAttendingPhysician.IntData = null;
                }

                if ((bool)FieldAttendingPhysicianCheck.BoolData)
                {
                    FieldAttendingPhysician.TextData = null;
                }

                if (FieldAttendingPhysician.IntData <= 0)
                {
                    FieldAttendingPhysician.IntData = null;
                }

                if (((bool)FieldAttendingPhysicianCheck.BoolData == false) && (FieldAttendingPhysician.IntData == null))
                {
                    AllValid = false;
                    FieldAttendingPhysician.ValidationErrors.Add(new ValidationResult(
                        FieldAttendingPhysician.PDFFieldNameLabel + " is required.", new[] { "IntData" }));
                }

                SetupFieldAttendingPhysicianTextData();
                // Validate EOBDate
                if (FieldEOBDate.DateTimeData != null)
                {
                    FieldEOBDate.DateTimeData = ((DateTime)FieldEOBDate.DateTimeData).Date;
                }

                if (FieldPatientSignatureDate.DateTimeData != null)
                {
                    FieldPatientSignatureDate.DateTimeData = ((DateTime)FieldPatientSignatureDate.DateTimeData).Date;
                }

                if (FieldPatientSignatureDate.DateTimeData > FieldEOBDate.DateTimeData)
                {
                    AllValid = false;
                    FieldEOBDate.ValidationErrors.Add(new ValidationResult(
                        "The Election of Benefits Date cannot be earlier than the Patient Signature Date.",
                        new[] { "DateTimeData" }));
                }

                if (AllValid)
                {
                    if (Admission != null)
                    {
                        Admission.SOCDate = FieldEOBDate.DateTimeData;
                    }

                    if (Admission != null)
                    {
                        Admission.HospiceEOBDate = FieldEOBDate.DateTimeData;
                    }
                }
            }

            if (AllValid == false)
            {
                return AllValid;
            }

            // Once CMS form is valid set EncounterCMSForm.SignedDate from PatientSignatureDate CMSFormField or Encounter
            EncounterCMSFormField s = GetFieldByDeltaFieldName("PatientSignatureDate", false);
            DateTime? signedDate = (s == null) ? null : s.DateTimeData;
            if (signedDate == null)
            {
                signedDate = (Encounter.EncounterOrTaskStartDateAndTime == null)
                    ? DateTime.Today.Date
                    : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;
            }

            EncounterCMSForm.SignedDate = signedDate;
            return AllValid;
        }

        private void SetupFieldAttendingPhysicianTextData()
        {
            if (FieldAttendingPhysician == null)
            {
                return;
            }

            string name = null;
            if ((FieldAttendingPhysician.IntData != null) && (Admission != null) &&
                (Admission.AdmissionPhysician != null))
            {
                AdmissionPhysician ap = Admission.AdmissionPhysician
                    .Where(p => p.AdmissionPhysicianKey == (int)FieldAttendingPhysician.IntData).FirstOrDefault();
                if (ap != null)
                {
                    name = ap.FormattedName;
                }
            }

            FieldAttendingPhysician.TextData = name;
        }

        private void SetupAdmittedAdmissionCoverageDefaults(int pAdmissionKey)
        {
            if ((Encounter == null) || (EncounterCMSFormFieldList == null) || (Patient == null) ||
                (Patient.PatientInsurance == null) || (Admission == null) || (Admission.AdmissionCoverage == null))
            {
                return;
            }

            if (Admission.AdmissionKey != pAdmissionKey)
            {
                return;
            }

            if (((Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                 (Encounter.PreviousEncounterStatusIsInEdit)) == false)
            {
                return;
            }

            if (FieldPATCONCoveredBy != null)
            {
                FieldPATCONCoveredBy.TextData = DefaultPATCONCoveredBy;
            }

            RaiseChangedEncounterCMSFormFields();
        }

        private void SetupAdmittedAdmissionPhysicianDefaults()
        {
            if ((Admission == null) || (Admission.AdmissionPhysician == null) || (Encounter == null) ||
                (Encounter.Task == null) || (Question == null))
            {
                return;
            }

            if (Question.LookupType != "MHES")
            {
                return;
            }

            AdmissionPhysician aap = null;
            List<AdmissionPhysician> apList = new List<AdmissionPhysician>();
            if (Encounter.EncounterIsInEdit)
            {
                DateTime? serviceDate = null;
                if (Encounter.EncounterOrTaskStartDateAndTime != null)
                {
                    serviceDate = ((DateTimeOffset)Encounter.EncounterOrTaskStartDateAndTime).Date;
                }

                if (serviceDate == null)
                {
                    serviceDate = DateTime.Today.Date;
                }

                serviceDate = ((DateTime)serviceDate).Date;
                int? saveAttendingPhysician = ProxyFieldAttendingPhysician;
                apList = Admission.AdmissionPhysician
                    .Where(p => p.Inactive == false)
                    .Where(p => (p.PhysicianEffectiveFromDate.Date <= serviceDate) &&
                                (!p.PhysicianEffectiveThruDate.HasValue || (p.PhysicianEffectiveThruDate.HasValue &&
                                                                            (p.PhysicianEffectiveThruDate.Value.Date >=
                                                                             serviceDate))))
                    .ToList();
                int physicianType = (CodeLookupCache.GetKeyFromCode("PHTP", "PCP") == null)
                    ? 0
                    : (int)(CodeLookupCache.GetKeyFromCode("PHTP", "PCP"));
                aap = apList
                    .Where(p => p.Inactive == false)
                    .Where(p => p.PhysicianType == physicianType)
                    .FirstOrDefault();
                if (saveAttendingPhysician == null)
                {
                    ProxyFieldAttendingPhysician = (aap == null) ? (int?)null : aap.AdmissionPhysicianKey;
                }
                else
                {
                    ProxyFieldAttendingPhysician =
                        apList.Where(p => p.AdmissionPhysicianKey == saveAttendingPhysician).Any()
                            ? saveAttendingPhysician
                            : null;
                }
            }

            aap = AttendingAdmissionPhysician;
            if ((aap != null) && (apList.Contains(aap) == false))
            {
                apList.Add(aap);
            }

            AttendingAdmissionPhysicianList = apList;
            this.RaisePropertyChangedLambda(p => p.ProxyFieldAttendingPhysician);
            this.RaisePropertyChangedLambda(p => p.AttendingAdmissionPhysician);
        }

        public override void Cleanup()
        {
            if (CMSFormPageList != null)
            {
                CMSFormPageList.Clear();
                CMSFormPageList = null;
            }

            Messenger.Default.Unregister<int>(this, "AdmissionCoverage_FormUpdate");
            Messenger.Default.Unregister<int>(this, "AdmissionPhysician_FormUpdate");
            Messenger.Default.Unregister<Server.Data.AdvanceCarePlan>(this, "AdvanceCarePlanChanged");
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }
    }

    public class CMSFormQuestionFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            CMSFormQuestion cfq = new CMSFormQuestion(__FormSectionQuestionKey)
            {
                FormSection = formsection,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm
            };
            cfq.CMSFormQuestionSetup(vm);
            return cfq;
        }
    }
}