#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Core;
using Virtuoso.Core;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Converters;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Occasional;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;
using Virtuoso.Portable.Extensions;
using ViewModelBase = GalaSoft.MvvmLight.ViewModelBase;

#endregion

namespace Virtuoso.Server.Data
{
    //Use this interface for properties common to Admission and EncounterAdmission for when interrogating QuestionUI.AdmissionOrEncounterAdmission
    public interface IEncounterAdmission : INotifyDataErrorInfo
    {
        int? ServicePriorityOne { get; }
        int? ServicePriorityTwo { get; }
        int? ServicePriorityThree { get; }
        string ProviderName { get; }
        string ProviderAddress1 { get; }
        string ProviderAddress2 { get; }
        string ProviderCity { get; }
        int? ProviderStateCode { get; }
        string ProviderZipCode { get; }
        string ProviderCityStateZip { get; }
        string ProviderPhoneExtension { get; }
        string ProviderPhoneNumber { get; }
        string ProviderFaxNumber { get; }
        DateTime? VerbalSOCDate { get; }
        bool IsDependentOnElectricity { get; }
        bool IsEmploymentRelated { get; }
        int? EmploymentRelatedEmployer { get; }
        bool? HasTrauma { get; }
        int? TraumaType { get; }
        DateTime? TraumaDate { get; }
        int? TraumaStateCode { get; }
    }

    public class WoundMeasurementHistoryItem
    {
        public DateTime MeasurementDate { get; set; }
        public string Length { get; set; }
        public string Width { get; set; }
        public string Depth { get; set; }
    }

    public class SynopsisItem
    {
        public string SynopsisText { get; set; }
    }

    public enum ICDViewVersionType
    {
        ICD9,
        ICD10,
        ICDBoth
    }

    public partial class Admission : IServiceLineGroupingService, IEncounterAdmission, INotifyDataErrorInfo
    {
        private int __cleanupCount;


        public ObservableCollection<DisciplineGroups> __GroupedDisciplines;

        private string _AdmissionStatusCode;
        // to avoid triage of data after its fetched from the server, use nullable bool? and triage at first use

        private bool? _Admitted;
        private int? _currentCertPeriodNumber;
        private string _DischargeReasonCode;

        private CollectionViewSource _FilteredItemsSource;
        private string _ICDViewCurrentAll;
        private ICDViewVersionType _ICDViewVersion = ICDViewVersionType.ICD9;
        private Insurance _insurance;

        private bool _IsContractProvider;
        private Encounter _myEncounter;

        private bool? _NotTaken;
        private PatientInsurance _pi;
        private Entity _providerFacility;

        private bool _SearchOpen;

        // This is used in Hospice Admission to establish the cert period with a PeriodNumber that is > 1
        public int? _UserTypedPeriodNumber;

        private bool _ValidateState_IsEval;

        private bool _ValidateState_IsEvalFullValidation;

        private bool _ValidateState_IsPreEval;

        private bool myDiagnosis;
        private int myVersion;
        private int? prevSourceOfAdmission;

        public bool AdmissionWasAdmitted
        {
            get
            {
                if (AdmissionStatusCodeWasAdmitted == false)
                {
                    return false;
                }

                return AdmitDateTime == null ? false : true;
            }
        }

        private bool AdmissionStatusCodeWasAdmitted
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AdmissionStatusCode))
                {
                    return false;
                }

                return AdmissionStatusCode == "A" || AdmissionStatusCode == "D" || AdmissionStatusCode == "T" ||
                       AdmissionStatusCode == "M"
                    ? true
                    : false;
            }
        }

        public bool AdmissionWasDischarged
        {
            get
            {
                if (AdmissionWasAdmitted == false)
                {
                    return false;
                }

                if (AdmissionStatusCodeWasDischarged == false)
                {
                    return false;
                }

                return DischargeDateTime == null ? false : true;
            }
        }

        private bool AdmissionStatusCodeWasDischarged
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AdmissionStatusCode))
                {
                    return false;
                }

                return AdmissionStatusCode == "D" ? true : false;
            }
        }

        public List<AdmissionAlternateID> ActiveAdmissionAlternateIDs
        {
            get
            {
                if (AdmissionAlternateID == null)
                {
                    return null;
                }

                var aList = AdmissionAlternateID.Where(a => a.IsActiveAsOfDate(DateTime.Today.Date)).ToList();
                return aList == null ? null : aList.Any() == false ? null : aList;
            }
        }

        public bool ShowActiveAdmissionAlternateIDs => ActiveAdmissionAlternateIDs == null ? false : true;

        public int ServiceLineType
        {
            get
            {
                if (ServiceLine == null)
                {
                    var SL = ServiceLineCache.GetServiceLineFromKey(ServiceLineKey);
                    if (SL == null)
                    {
                        return 0;
                    }

                    return SL.ServiceLineType;
                }

                return ServiceLine.ServiceLineType;
            }
        }

        public bool SOCBeforeGoLiveDate
        {
            get
            {
                var goLiveDate = ServiceLineCache.Current.GoLiveDateForAdmission(this, ServiceLineKey);
                var ret = false;
                if (SOCDate != null && goLiveDate != null && SOCDate < goLiveDate)
                {
                    ret = true;
                }

                return ret;
            }
        }

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

        public string MRNdashAdmissionID
        {
            get
            {
                var mrn = Patient == null ? "Unknown" : Patient.MRN;
                return mrn + " - " + AdmissionID;
            }
        }

        public bool HIBInsuranceElectionAddendumAvailable
        {
            get
            {
                var electionAddendumAvailable = false;
                if (PatientInsuranceKey != null && PatientInsuranceKey != 0 && Patient != null &&
                    Patient.PatientInsurance != null)
                {
                    var pi = Patient.PatientInsurance.Where(p => p.PatientInsuranceKey == PatientInsuranceKey)
                        .FirstOrDefault();
                    var i = pi == null ? null : InsuranceCache.GetInsuranceFromKey(pi.InsuranceKey);
                    if (i != null)
                    {
                        electionAddendumAvailable = i.ElectionAddendumAvailable;
                    }
                }

                return electionAddendumAvailable;
            }
        }

        public string AdmissionInsurance
        {
            get
            {
                string admissionInsurance = null;
                if (PatientInsuranceKey != null && PatientInsuranceKey != 0 && Patient != null &&
                    Patient.PatientInsurance != null)
                {
                    var pi = Patient.PatientInsurance.Where(p => p.PatientInsuranceKey == PatientInsuranceKey)
                        .FirstOrDefault();
                    var i = pi == null ? null : InsuranceCache.GetInsuranceFromKey(pi.InsuranceKey);
                    if (i != null)
                    {
                        admissionInsurance = i.Name;
                    }
                }

                if (string.IsNullOrWhiteSpace(admissionInsurance))
                {
                    admissionInsurance = "Unknown";
                }

                return admissionInsurance;
            }
        }

        public string CodeStatusString
        {
            get
            {
                var CodeStatus = "Other";
                if (Patient != null && Patient.ActiveDNRs != null)
                {
                    CodeStatus = "DNR";
                }

                return CodeStatus;
            }
        }

        public string InsuranceNumberInfoText =>
            "This is the insurance that will drive the certification requirements for the admission.";

        public string InformationReleaseInfoText =>
            "Identify the external organization/individuals associated with the information release.";

        public string SOCDescription => "SOC: " + (SOCDate == null ? "?" : ((DateTime)SOCDate).ToString("MM/dd/yyyy"));

        public string ServiceLineDescription
        {
            get
            {
                var sl = ServiceLineCache.GetServiceLineFromKey(ServiceLineKey);
                if (sl == null)
                {
                    return "Service Line ?";
                }

                return string.IsNullOrWhiteSpace(sl.Name) ? "Service Line ?" : sl.Name;
            }
        }

        public string ServiceLineFormattedPhoneNumber
        {
            get
            {
                var sl = ServiceLineCache.GetServiceLineFromKey(ServiceLineKey);
                if (sl == null)
                {
                    return null;
                }

                var formattedPhoneNumber = FormatPhoneNumber(sl.Number);
                if (string.IsNullOrWhiteSpace(formattedPhoneNumber))
                {
                    return null;
                }

                return formattedPhoneNumber +
                       (string.IsNullOrWhiteSpace(sl.PhoneExtension) ? "" : " x" + sl.PhoneExtension);
            }
        }

        public string RefillDescription => string.Format("{0} {1}", ServiceLineDescription, SOCDescription);

        private Admission OriginalAdmissionRow { get; set; }

        public bool InDynamicForm { get; set; }

        public string ICDViewCurrentAll
        {
            get { return _ICDViewCurrentAll; }
            set
            {
                _ICDViewCurrentAll = value;
                RaisePropertyChanged("ICDViewCurrentAll");
            }
        }

        public ICDViewVersionType ICDViewVersion
        {
            get { return _ICDViewVersion; }
            set
            {
                _ICDViewVersion = value;
                RaisePropertyChanged("ICDViewVersion");
            }
        }

        public bool SearchOpen
        {
            get { return _SearchOpen; }
            set
            {
                _SearchOpen = value;
                RaisePropertyChanged("SearchOpen");
            }
        }

        public bool CanExecuteIfDischargedOrNotTaken_EditCommand
        {
            get
            {
                // SystemAdministrators can edit everything all the time
                if (RoleAccessHelper.CheckPermission(RoleAccess.Admin, false))
                {
                    return true;
                }

                // Non System Administrators cannot edit (most things) if Admission is Discharged or NotTaken
                if (AdmissionStatusCode == "D" || AdmissionStatusCode == "N")
                {
                    return false;
                }

                return true;
            }
        }

        public bool CanExecuteIfDischargedOrTransferredOrNotTaken_EditCommand
        {
            get
            {
                // SystemAdministrators can edit everything all the time
                if (RoleAccessHelper.CheckPermission(RoleAccess.Admin, false))
                {
                    return true;
                }

                // Non System Administrators cannot edit (most things) if Admission is Discharged or Transferred or NotTaken
                if (AdmissionStatusCode == "D" || AdmissionStatusCode == "T" || AdmissionStatusCode == "N")
                {
                    return false;
                }

                return true;
            }
        }

        public bool CanExecuteIfDischargedOrTransferredOrNotTaken_AddDisciplineCommand
        {
            get
            {
                // Cannot Add (or re-Add) Discipline if Admission is Discharged or Transferred or NotTaken
                if (AdmissionStatusCode == "D" || AdmissionStatusCode == "T" || AdmissionStatusCode == "N")
                {
                    return false;
                }

                return true;
            }
        }

        public bool ShowLevelOfCare
        {
            get
            {
                if (AdmissionKey <= 0)
                {
                    return false;
                }

                return HospiceAdmission;
            }
        }

        public bool ShowHIS
        {
            get
            {
                if (AdmissionKey <= 0)
                {
                    return false;
                }

                if (!HospiceAdmission)
                {
                    return false;
                }

                // if no permission don't show HIS
                // Note - even though there may not be any HIS for this admission - Show the tab so HIS Coordinator can Add new surveys
                return RoleAccessHelper.CheckPermission(RoleAccess.HISCoordinatorOrEntry, false) == false
                    ? false
                    : true;
            }
        }

        public bool ShowOASIS
        {
            get
            {
                if (AdmissionKey <= 0)
                {
                    return false;
                }

                if (HospiceAdmission)
                {
                    return false;
                }

                // if no permission don't show OASIS
                // Note - even though there may not be any surveys for this admission - Show the tab so  Coordinator can Add new surveys
                return RoleAccessHelper.CheckPermission(RoleAccess.OASISCoordinatorOrEntry, false) == false
                    ? false
                    : true;
            }
        }

        public bool ShowAuths
        {
            get
            {
                if (AdmissionKey <= 0)
                {
                    return false;
                }

                try
                {
                    if (Patient.PatientInsurance.Where(pi => pi.Insurance.Authorizations == false).Any())
                    {
                        return true;
                    }
                }
                catch
                {
                    // One of the links is broken, so hide the panel
                    return false;
                }

                return true;
            }
        }

        public bool ShowEligibility
        {
            get
            {
                if (AdmissionKey <= 0)
                {
                    return false;
                }

                try
                {
                    if (Patient != null)
                    {
                        var p = Patient;
                        var r = p.InsuranceVerificationRequest.ToList();

                        return r.Any();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }

                return false;
            }
        }

        public Insurance Insurance
        {
            get
            {
                var pik = PatientInsuranceKey == null ? 0 : PatientInsuranceKey.Value;

                if (pik <= 0)
                {
                    _pi = null;
                    _insurance = null;
                }
                else
                {
                    if (_pi != null && _insurance != null)
                    {
                        if (_pi.PatientInsuranceKey != pik)
                        {
                            _pi = null;
                            _insurance = null;
                        }
                    }

                    if (_insurance == null)
                    {
                        if (Patient != null && Patient.PatientInsurance != null)
                        {
                            _pi = Patient.PatientInsurance.Where(p => p.PatientInsuranceKey == pik).FirstOrDefault();
                            _insurance = _pi == null ? null : InsuranceCache.GetInsuranceFromKey(_pi.InsuranceKey);
                        }
                    }
                }

                return _insurance;
            }
        }

        public bool InsuranceRequiresDisciplineOrders
        {
            get
            {
                Insurance i = null;
                if (PatientInsuranceKey != null && PatientInsuranceKey != 0 && Patient != null &&
                    Patient.PatientInsurance != null)
                {
                    var pi = Patient.PatientInsurance.Where(p => p.PatientInsuranceKey == PatientInsuranceKey)
                        .FirstOrDefault();
                    i = pi == null ? null : InsuranceCache.GetInsuranceFromKey(pi.InsuranceKey);
                }

                return i == null ? false : i.DisciplineOrders;
            }
        }

        public bool CanEditServiceLine => AdmissionKey <= 0 ? true : false;

        public string NotTakenReasonCodeType => HospiceAdmission ? "HospiceNotTakeReason" : "NotTakenReason";
        public string SourceOfAdmissionCodeType => HospiceAdmission ? "HospiceAdmittedFrom" : "AdmissionSource";
        public string SourceOfAdmissionLabel => HospiceAdmission ? "Admitted From" : "Admission Source";
        public string SourceOfAdmissionRequiredLabel => HospiceAdmission ? "* Admitted From" : "Admission Source";
        public string StartOfCareDateLabel => HospiceAdmission ? "Admitted to Service Date" : "Start of Care Date";
        public string ReferralDateLabel => HospiceAdmission ? "Referral Date" : "Referral Date";
        public string ReferralDateRequiredLabel => HospiceAdmission ? "* Referral Date" : "* Referral Date";


        public string PreEvalStatusPlanToAdmit => "Plan To Admit";
        public string PreEvalStatusDoNotAdmit => "Do Not Admit";
        public string PreEvalStatusOnHold => "On Hold";
        public bool IsPreEvalStatusPlanToAdmit => PreEvalStatus == PreEvalStatusPlanToAdmit ? true : false;
        public bool IsPreEvalStatusDoNotAdmit => PreEvalStatus == PreEvalStatusDoNotAdmit ? true : false;
        public bool IsPreEvalStatusOnHold => PreEvalStatus == PreEvalStatusOnHold ? true : false;
        public bool IsAdmissionStatusOnHold => AdmissionStatusCode == "H" ? true : false;
        public bool IsAdmissionStatusTransferred => AdmissionStatusCode == "T" ? true : false;
        public bool IsAdmissionStatusNTUC => AdmissionStatusCode == "N" ? true : false;
        public bool IsAdmissionStatusDischarged => AdmissionStatusCode == "D" ? true : false;

        public bool HideCertPeriods
        {
            get
            {
                var controllingPayer = Insurance;
                var serviceLine = SelectedServiceLine;
                if (controllingPayer == null || serviceLine == null)
                {
                    return true;
                }

                if (serviceLine.IsHomeHealthServiceLine)
                {
                    return controllingPayer.HideHomeHealthPeriods;
                }

                if (serviceLine.IsHospiceServiceLine)
                {
                    return controllingPayer.HideHospicePeriods;
                }

                if (serviceLine.IsHomeCareServiceLine)
                {
                    return controllingPayer.HideHomeCarePeriods;
                }

                return true;
            }
        }

        public bool ShowHospicePeriods => CanModifyCertCycle && !HideCertPeriods;

        public bool ShowNonHospicePeriods => !CanModifyCertCycle && !HideCertPeriods;

        public bool ShowCertEditWidgetsPlusMask => ShowCertEditWidgets && !HideCertPeriods;

        public bool ShowCertEditWidgetsOppositePlusMask => !ShowCertEditWidgets && !HideCertPeriods;

        public bool ShowCertEditWidgets => CanModifyCertCycleMaint ||
                                           (AdmissionCertification == null ? false : AdmissionCertification.Count <= 1);

        public bool CanModifyCertPeriodMaint => CanModifyCertCycleMaint &&
                                                ((FirstCert == null ? true : FirstCert.PeriodNumber > 1) ||
                                                 TransferHospice);

        public bool ReferDateIsBeforeGoLive
        {
            get
            {
                var goLiveDate = ServiceLineCache.Current.GoLiveDateForAdmission(this, ServiceLineKey);
                if (goLiveDate.HasValue == false || ReferDateTime.HasValue == false)
                {
                    return false;
                }

                var _ReferBeforeGoLive = ReferDateTime.Value.Date < goLiveDate;
                return _ReferBeforeGoLive;
            }
        }

        public bool CanModifyCertCycleMaint
        {
            get
            {
                if (OriginalAdmissionRow == null)
                {
                    OriginalAdmissionRow = (Admission)GetOriginal();
                }

                var orig = OriginalAdmissionRow;
                // Allow edit if the refer date is before the go live date AND it hasn't been entered yet.

                // Only allow edit if we are still inside the first cert cycle.
                var inFirstCert = FirstCert != null && DateTime.Today <= FirstCert.PeriodEndDate &&
                                  DateTime.Today >= FirstCert.PeriodStartDate;
                inFirstCert = inFirstCert || AdmissionCertification != null && AdmissionCertification.Count <= 1;

                if (ReferDateIsBeforeGoLive && inFirstCert)
                {
                    return true;
                }

                // only allow edit to system admins.
                if (inFirstCert && RoleAccessHelper.CheckPermission(RoleAccess.Admin, false)
                                && (orig != null && orig.SOCDate.HasValue || orig == null && SOCDate.HasValue))
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanModifyCertCycleMaintPhysicianSOC
        {
            get
            {
                if (OriginalAdmissionRow == null)
                {
                    OriginalAdmissionRow = (Admission)GetOriginal();
                }

                var orig = OriginalAdmissionRow; //(Admission)this.GetOriginal();
                // Allow edit if the refer date is before the go live date AND it hasn't been entered yet.

                // Only allow edit if we are still inside the first cert cycle.
                var inFirstCert = FirstCert != null && DateTime.Today <= FirstCert.PeriodEndDate &&
                                  DateTime.Today >= FirstCert.PeriodStartDate;
                inFirstCert = inFirstCert || AdmissionCertification != null && AdmissionCertification.Count <= 1;

                if (ReferDateIsBeforeGoLive && inFirstCert)
                {
                    return true;
                }

                // Allow anyone to edit pre-admission (pre SOC) - as long as we are in the first Cert (a bit redundant)
                if (inFirstCert && SOCDate.HasValue == false)
                {
                    return true;
                }

                // only allow edit to system admins.
                if (inFirstCert && RoleAccessHelper.CheckPermission(RoleAccess.Admin, false)
                                && (orig != null && orig.SOCDate.HasValue || orig == null && SOCDate.HasValue))
                {
                    return true;
                }

                // allow edit if it is transfer from another hospice AND we are in the first cert
                if (TransferHospice && inFirstCert)
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanEditPhysicianSOC => IsNew || CanModifyCertCycleMaintPhysicianSOC;

        public bool CanEditSOC =>
            // For now the editing of these two can be tied.
            CanModifyCertCycleMaint;

        public bool CanModifyCertCycle
        {
            get
            {
                var CanModify = false;
                CanModify = AdmissionCertification.Count() <= 1;
                if (CanModify && Encounter != null)
                {
                    var Encs = Encounter.Where(e => e.Signed);
                    foreach (var e in Encs)
                    {
                        Form f = null;
                        if (e.FormKey != null)
                        {
                            f = DynamicFormCache.GetFormByKey((int)e.FormKey);
                        }

                        if (f != null && (f.IsEval || f.IsVisit || f.IsResumption || f.IsTransfer))

                        {
                            CanModify = false;
                            break;
                        }
                    }
                }

                // always alow edit if none have been defined yet.
                if (!CanModify)
                {
                    CanModify = AdmissionCertification.Any() == false;
                }

                return CanModify;
            }
        }

        public bool PhysicianMismatch
        {
            get
            {
                var mismatch = false;
                if (SOCDate != null && SOCDate.HasValue && PhysicianOrderedSOCDate != null &&
                    PhysicianOrderedSOCDate.HasValue)
                {
                    mismatch = SOCDate.Value.Date != PhysicianOrderedSOCDate.Value.Date;
                }

                return mismatch;
            }
        }

        public bool FaceToFaceRequired
        {
            get
            {
                if (HospiceAdmission)
                {
                    return false;
                }

                if (FaceToFaceEncounter.HasValue)
                {
                    return true;
                }

                return Insurance == null ? false : Insurance.FaceToFaceOnAdmit;
            }
        }

        public bool RequiresFaceToFaceOnAdmit
        {
            get
            {
                if (HospiceAdmission)
                {
                    return false;
                }

                return Insurance == null ? false : Insurance.FaceToFaceOnAdmit;
            }
        }

        public bool CanUserEdit
        {
            get
            {
                if (RoleAccessHelper.CheckPermission(RoleAccess.Admin, false))
                {
                    return true;
                }

                return AdmissionStatusCode == "T" ? false : true;
            }
        }

        public bool CanUserEditDateOfDeathMaint
        {
            get
            {
                // must be entered first in an encounter
                if (!DeathDate.HasValue)
                {
                    return false;
                }

                // If the DeathNote is not complete, don't allow Edit;
                if (Encounter != null)
                {
                    if (Encounter.Where(e =>
                            e.DeathNote == true && e.Inactive == false &&
                            e.EncounterStatus != (int)EncounterStatusType.Completed).Any())
                    {
                        return false;
                    }
                }

                //Only admin and clinical manager
                if (RoleAccessHelper.CheckPermission(RoleAccess.Admin, false) ||
                    RoleAccessHelper.CheckPermission(RoleAccess.ClinicalManager, false))
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanModifyDischareFromTransfer
        {
            get
            {
                // only if I'm a sys admin
                // Non Admins can now modify 'Discharge Transferred Admission' if (!RoleAccessHelper.CheckPermission(RoleAccess.Admin, false)) return false;

                // Can Edit if I'm coming from a transfer or discharge
                var CanEditDFT = MostRecentTransfer != null && MostRecentTransfer.TransferDate != null
                                                            && (AdmissionStatusCode == "T" ||
                                                                AdmissionStatusCode == "D");
                if (OriginalAdmissionRow == null)
                {
                    OriginalAdmissionRow = (Admission)GetOriginal();
                }

                var orig = OriginalAdmissionRow;
                CanEditDFT = CanEditDFT && (orig == null ? !DischargedFromTransfer : !orig.DischargedFromTransfer);
                if (AdmissionDiscipline != null)
                {
                    CanEditDFT = CanEditDFT && !AdmissionDiscipline.Any(ad => ad.AdmissionStatusCode == "R");
                }

                return CanEditDFT;
            }
        }

        public DateTime? AdmissionDeathDate
        {
            get { return DeathDate; }
            set
            {
                if (IsDeserializing)
                {
                    return;
                }

                if (DeathDate == value)
                {
                    return;
                }

                DeathDate = value;
                CreateEncounterAddendum();
            }
        }

        public DateTimeOffset? AdmissionDeathTime
        {
            get { return DeathTime; }
            set
            {
                if (IsDeserializing)
                {
                    return;
                }

                if (DeathTime == value)
                {
                    return;
                }

                DeathTime = value;
                CreateEncounterAddendum();
            }
        }

        public bool DischargeLabelsVisible => DischargeEditVisible || DischargeDisplayVisible;

        public bool DischargeEditVisible => DischargedFromTransfer && CanModifyDischareFromTransfer;

        public bool DischargeDisplayVisible =>
            (DischargeDateTime.HasValue || DischargeReasonKey.HasValue) && !DischargeEditVisible;

        public bool DischargeFromTransferBoxVisible =>
            AdmissionStatusCode == "T" || AdmissionStatusCode == "D" && DischargedFromTransfer;

        public List<PatientInsurance> PatientInsuranceItemSource => GetPatientInsuranceItemSource(PatientInsuranceKey);

        public bool IsMedicarePPSPatient
        {
            get
            {
                var piList = GetPatientInsuranceItemSource(null);
                if (piList == null)
                {
                    return false;
                }

                return piList.Where(p => p.InsuranceTypeCode == "1").Any() ? true : false;
            }
        }

        public bool IsHomeHealth => (int)eServiceLineType.HomeHealth == ServiceLineType;
        public bool IsHomeCare => (int)eServiceLineType.HomeCare == ServiceLineType;

        public bool CanModifyHospiceTransfer
        {
            get
            {
                if (HospiceBenefitReelection)
                {
                    return false;
                }

                return CanModifyCertCycle;
            }
        }

        public bool CanModifyHospiceBenefitReelection
        {
            get
            {
                if (TransferHospice)
                {
                    return false;
                }

                return CanModifyCertCycle;
            }
        }

        public List<ServiceLineGrouping> ServiceLineGroupingItemsSource
        {
            get
            {
                if (CurrentGroup == null)
                {
                    return null;
                }

                var slg = ServiceLineCache.GetServiceLineGroupingFromKey(CurrentGroup.ServiceLineGroupingKey);
                return ServiceLineCache.GetActiveUserServiceLineGroupingForServiceLinePlusMe(ServiceLineKey,
                    slg == null ? 0 : slg.ServiceLineGroupingKey, true);
            }
        }

        public List<ServiceLine> ServiceLineItemsSource
        {
            get
            {
                if (Patient == null)
                {
                    return null;
                }

                var slKey = IsNew ? 0 : ServiceLineKey;
                return Patient.GetFilteredServiceLineItemsSource(slKey, true);
            }
        }

        public ServiceLineGrouping CurrentServiceLineGrouping
        {
            get
            {
                var ag = AdmissionGroup == null
                    ? null
                    : AdmissionGroup.OrderByDescending(g => g.StartDate).FirstOrDefault();
                if (ag == null)
                {
                    return null;
                }

                return ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey);
            }
        }

        public string DischargeReasonCode
        {
            get
            {
                if (_DischargeReasonCode == null)
                {
                    _DischargeReasonCode = CodeLookupCache.GetCodeFromKey(DischargeReasonKey);
                }

                return _DischargeReasonCode;
            }
        }

        public string DischargeReasonCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(DischargeReasonKey);

        public bool PreEvalRequiredEnabled
        {
            get
            {
                if (ServiceLineKey <= 0)
                {
                    return false;
                }

                if (!ShowPreEvalRequired)
                {
                    return false;
                }

                return AdmissionStatusCode == "R" ? true : false;
            }
        }

        public bool ShowPreEvalRequired
        {
            get
            {
                if (ServiceLineKey <= 0)
                {
                    return false;
                }

                if (PreEvalRequired)
                {
                    return true;
                }

                if (HospiceAdmission && ServiceLineCache.Current.ServiceLineHospicePreEvalRequired(ServiceLineKey))
                {
                    return true;
                }

                if (!HospiceAdmission && ServiceLineCache.Current.ServiceLineNonHospicePreEvalRequired(ServiceLineKey))
                {
                    return true;
                }

                return false;
            }
        }

        public string OriginalAdmissionStatusCode
        {
            get
            {
                if (IsNew)
                {
                    return "R";
                }

                if (OriginalAdmissionRow == null)
                {
                    OriginalAdmissionRow = (Admission)GetOriginal();
                }

                var a = OriginalAdmissionRow;
                if (a != null)
                {
                    if (a.AdmissionStatus == 0)
                    {
                        return "R";
                    }

                    return a.AdmissionStatusCode;
                }

                if (AdmissionStatus == 0)
                {
                    return "R";
                }

                return AdmissionStatusCode;
            }
        }

        public string AdmissionStatusCode
        {
            get
            {
                if (_AdmissionStatusCode == null)
                {
                    _AdmissionStatusCode = CodeLookupCache.GetCodeFromKey(AdmissionStatus);
                }

                return _AdmissionStatusCode;
            }
        }

        public string SourceOfAdmissionCode => CodeLookupCache.GetCodeFromKey((int)SourceOfAdmission);

        public string Facility1Label
        {
            get
            {
                if (FacilityKey.HasValue)
                {
                    return FacilityCache.GetPatientIDLabelFromKey(FacilityKey, 1);
                }

                if (CurrentReferral != null && CurrentReferral.FacilityKey.HasValue)
                {
                    return FacilityCache.GetPatientIDLabelFromKey(CurrentReferral.FacilityKey, 1);
                }

                return string.Empty;
            }
        }

        public string Facility2Label
        {
            get
            {
                if (FacilityKey.HasValue)
                {
                    return FacilityCache.GetPatientIDLabelFromKey(FacilityKey, 2);
                }

                if (CurrentReferral != null && CurrentReferral.FacilityKey.HasValue)
                {
                    return FacilityCache.GetPatientIDLabelFromKey(CurrentReferral.FacilityKey, 2);
                }

                return string.Empty;
            }
        }

        public string Facility3Label
        {
            get
            {
                if (FacilityKey.HasValue)
                {
                    return FacilityCache.GetPatientIDLabelFromKey(FacilityKey, 3);
                }

                if (CurrentReferral != null && CurrentReferral.FacilityKey.HasValue)
                {
                    return FacilityCache.GetPatientIDLabelFromKey(CurrentReferral.FacilityKey, 3);
                }

                return string.Empty;
            }
        }

        public string DischargeDateTimeDisplay => DischargeDateTime == null
            ? "Unknown Date"
            : ((DateTime)DischargeDateTime).ToString("MM/dd/yyyy");

        public string AdmissionStatusText
        {
            get
            {
                if (AdmissionStatusCode == "A" && TenantSettingsCache.Current.TenantSetting.ContractServiceProvider)
                {
                    return "Admitted on " +
                           (AdmitDateTime == null ? "?" : ((DateTime)AdmitDateTime).ToString("MM/dd/yyyy"));
                }

                if (AdmissionStatusCode == "A")
                {
                    return "Admitted on " + (SOCDate == null ? "?" : ((DateTime)SOCDate).ToString("MM/dd/yyyy"));
                }

                if (AdmissionStatusCode == "D")
                {
                    return "Discharged on " + DischargeDateTimeDisplay;
                }

                if (AdmissionStatusCode == "H")
                {
                    return "On Hold on " +
                           (PreEvalOnHoldDateTime.HasValue
                               ? ((DateTime)PreEvalOnHoldDateTime).ToString("MM/dd/yyyy")
                               : "Unknown Date") + " follow-up on " + (PreEvalFollowUpDate.HasValue
                               ? ((DateTime)PreEvalFollowUpDate).ToString("MM/dd/yyyy")
                               : "Unknown Date");
                }

                if (AdmissionStatusCode == "M")
                {
                    return "Resumed on " + (ResumptionDate.HasValue
                        ? ((DateTime)ResumptionDate).ToString("MM/dd/yyyy")
                        : "Unknown Date");
                }

                if (AdmissionStatusCode == "R")
                {
                    var ar = AdmissionReferral.Where(p => p.AdmissionKey == AdmissionKey)
                        .Where(referral => referral.HistoryKey == null).OrderByDescending(p => p.ReferralDate)
                        .FirstOrDefault();

                    if (ar != default(AdmissionReferral))
                    {
                        if (ar.ReferralDate != null)
                        {
                            return "Referred on " + ar.ReferralDate.Value.ToString("MM/dd/yyyy");
                        }
                    }

                    if (InitialReferralDate != null)
                    {
                        return "Referred on " + InitialReferralDate.Value.ToString("MM/dd/yyyy");
                    }

                    return "Referred on " + (ReferDateTime.HasValue
                        ? ((DateTime)ReferDateTime).ToString("MM/dd/yyyy")
                        : "Unknown Date");
                }

                if (AdmissionStatusCode == "N")
                {
                    return "Not admitted on " + (NotTakenDateTime.HasValue
                        ? ((DateTime)NotTakenDateTime).ToString("MM/dd/yyyy")
                        : "Unknown Date");
                }

                if (AdmissionStatusCode == "T")
                {
                    return "Transferred on " + (MostRecentTransfer == null
                        ? "Unknown Date"
                        : MostRecentTransfer.TransferDate.ToString("MM/dd/yyyy"));
                }

                return "Unknown status";
            }
        }

        public int? AdmittingDiagnosisKey9
        {
            get
            {
                if (AdmissionDiagnosis == null)
                {
                    return null;
                }

                var ad = AdmissionDiagnosis
                    .Where(a => a.Version == 9 && a.Diagnosis && a.Superceded == false && a.RemovedDate == null &&
                                a.DiagnosisStartDateDefaultDate <= DateTime.Now.Date && (a.DiagnosisEndDate == null ||
                                    a.DiagnosisEndDate != null && a.DiagnosisEndDateDefaultDate >= DateTime.Now.Date))
                    .OrderBy(a => a.Sequence).FirstOrDefault();
                return ad == null ? (int?)null : ad.AdmissionDiagnosisKey;
            }
        }

        public int? AdmittingDiagnosisKey10
        {
            get
            {
                if (AdmissionDiagnosis == null)
                {
                    return null;
                }

                var ad = AdmissionDiagnosis
                    .Where(a => a.Version == 10 && a.Diagnosis && a.Superceded == false && a.RemovedDate == null &&
                                a.DiagnosisStartDateDefaultDate <= DateTime.Now.Date && (a.DiagnosisEndDate == null ||
                                    a.DiagnosisEndDate != null && a.DiagnosisEndDateDefaultDate >= DateTime.Now.Date))
                    .OrderBy(a => a.Sequence).FirstOrDefault();
                return ad == null ? (int?)null : ad.AdmissionDiagnosisKey;
            }
        }

        public string VitalsText
        {
            get
            {
                string text = null;
                var mostRecentEncounter = Encounter.Where(x => HasVitals(x))
                    .OrderByDescending(e => e.EncounterOrTaskStartDateAndTime).FirstOrDefault();
                text = GetVitalsTextForEncounter(mostRecentEncounter);
                return text;
            }
        }

        public AdmissionReferral CurrentReferral
        {
            get
            {
                var ar = AdmissionReferral.Where(referral => referral.HistoryKey == null)
                    .OrderByDescending(p => p.ReferralDate).FirstOrDefault();
                if (ar == null)
                {
                    return null;
                }

                try
                {
                    ar.PropertyChanged -= ChildPropertyChanged;
                }
                catch
                {
                }

                ar.PropertyChanged += ChildPropertyChanged;
                return ar;
            }
        }

        public DateTime AdmissionGroupDate { get; set; }

        public ObservableCollection<DisciplineGroups> GroupedDisciplines
        {
            get
            {
                CleanupGroupedDisciplines();

                var slg1 = CurrentGroup;
                var slg2 = CurrentGroup2;
                var slg3 = CurrentGroup3;
                var slg4 = CurrentGroup4;
                var slg5 = CurrentGroup5;
                var userCache = UserCache.Current.GetUsers(true);

                __GroupedDisciplines = new ObservableCollection<DisciplineGroups>();

                var dscpList = DisciplineCache.GetDisciplines();

                var dscpFilterd = dscpList
                    .Where(d => AdmissionDiscipline.Any(ad => ad.DisciplineKey == d.DisciplineKey))
                    .OrderBy(p => p.HCFACode);

                dscpFilterd.ForEach(d =>
                {
                    var dg = new DisciplineGroups(d, this, userCache, slg1, slg2, slg3, slg4, slg5);
                    __GroupedDisciplines.Add(dg);
                });

                return __GroupedDisciplines;
            }
        }

        private ICollectionView FilteredItemsSource => _FilteredItemsSource.View;

        public List<Physician> UniquePhysicians
        {
            get
            {
                if (AdmissionPhysician == null)
                {
                    return null;
                }

                var apList = AdmissionPhysician.Where(p => p.HistoryKey == null).ToList();
                if (apList == null || apList.Any() == false)
                {
                    return null;
                }

                var pList = new List<Physician>();
                foreach (var ap in apList)
                {
                    var p = PhysicianCache.Current.GetPhysicianFromKey(ap.PhysicianKey);
                    if (p != null && pList.Where(x => x.PhysicianKey == ap.PhysicianKey).Any() == false)
                    {
                        pList.Add(p);
                    }
                }

                if (pList == null || pList.Any() == false)
                {
                    return null;
                }

                return pList.OrderBy(x => x.FullNameWithSuffix).ToList();
            }
        }

        public List<Discipline> DisciplinesForUnableToMeetFCD
        {
            get
            {
                if (AdmissionDiscipline == null)
                {
                    return null;
                }

                var adList = AdmissionDiscipline.Where(p => p.AdmissionStatusCode != "N").ToList();
                if (adList == null || adList.Any() == false)
                {
                    return null;
                }

                var dList = new List<Discipline>();
                foreach (var ad in adList)
                {
                    var d = DisciplineCache.GetDisciplineFromKey(ad.DisciplineKey);
                    if (d != null && dList.Where(x => x.DisciplineKey == ad.DisciplineKey).Any() == false)
                    {
                        dList.Add(d);
                    }
                }

                if (dList == null || dList.Any() == false)
                {
                    return null;
                }

                return dList.OrderBy(x => x.Description).ToList();
            }
        }

        public ICollection<AdmissionDiscipline> ActiveAdmissionDisciplines
        {
            get
            {
                var ad = AdmissionDiscipline.Where(p => !p.DischargeDateTime.HasValue && !p.NotTakenDateTime.HasValue)
                    .OrderBy(p => p.ReferDateTime).ToList();
                return ad == null || ad.Any() == false ? null : ad;
            }
        }

        public ICollection<AdmissionDiscipline> ActiveAideAdmissionDisciplinesAdmitted
        {
            get
            {
                ICollection<AdmissionDiscipline> aList = AdmissionDiscipline.Where(ad =>
                        (ad.AdmissionStatusCode == "A" || ad.AdmissionStatusCode == "M") &&
                        ad.AdmissionDisciplineIsAide)
                    .ToList();
                if (aList == null || aList.Count == 0)
                {
                    return null;
                }

                return aList;
            }
        }

        public ICollection<AdmissionDiscipline> ActiveAdmissionDisciplinesReferred
        {
            get
            {
                var ad = AdmissionDiscipline.Where(p => p.AdmissionStatusCode == "R")
                    .OrderBy(p => p.DisciplineDescription).ToList();
                return ad == null || ad.Any() == false ? null : ad;
            }
        }

        public ICollection<AdmissionDiscipline> ActiveAdmissionDisciplinesReferredOrAdmitted
        {
            get
            {
                var ad = AdmissionDiscipline
                    .Where(p => !p.DischargeDateTime.HasValue && !p.NotTakenDateTime.HasValue &&
                                (p.AdmissionStatusCode == "R" || p.AdmissionStatusCode == "A" ||
                                 p.AdmissionStatusCode == "M")).OrderBy(p => p.DisciplineDescription).ToList();
                return ad == null || ad.Any() == false ? null : ad;
            }
        }

        public ICollection<AdmissionDiscipline> DischargedAdmissionDisciplines
        {
            get
            {
                var ad = AdmissionDiscipline.Where(p => p.DischargeDateTime.HasValue && !p.NotTakenDateTime.HasValue)
                    .OrderBy(p => p.ReferDateTime).ToList();
                return ad == null || ad.Any() == false ? null : ad;
            }
        }

        public bool CanEdit => AdmissionStatusCode == "R" ? true : false;

        public bool NotTaken
        {
            get
            {
                if (_NotTaken == null)
                {
                    NotTaken = AdmissionStatusCode == "N" ? true : false;
                }

                return (bool)_NotTaken;
            }
            set
            {
                if (value && _NotTaken != null && !_NotTaken.Value)
                {
                    if (AdmissionStatusHelper.CanChangeToNotTakenStatus(AdmissionStatus))
                    {
                        AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_NotTaken;
                        AdmissionDiscipline.Where(
                            p => !p.NotTakenDateTime.HasValue && !p.DischargeDateTime.HasValue).ForEach(ad =>
                        {
                            ad.BeginEditting();
                            ad.ForceNotTaken = true;
                        });
                        NotTakenDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                    }
                }
                else if (!value && _NotTaken != null && _NotTaken.Value)
                {
                    // The order of these events is important.
                    if (OriginalAdmissionRow == null)
                    {
                        OriginalAdmissionRow = (Admission)GetOriginal();
                    }

                    // reset the admission status.
                    AdmissionStatus = OriginalAdmissionRow == null
                        ? AdmissionStatusHelper.AdmissionStatus_Referred
                        : OriginalAdmissionRow.AdmissionStatus;

                    // then reset the admission discipline rows
                    AdmissionDiscipline.Where(p => p.ForceNotTaken).ForEach(ad => { ad.CancelEditting(); });

                    // and finally reset the admission not taken fields.
                    _NotTaken = false;
                    NotTakenDateTime = null;
                    NotTakenReason = null;
                }

                _NotTaken = value;
                RaisePropertyChanged("NotTaken");
                RaisePropertyChanged("CanEdit");
                RaisePropertyChanged("CanUserEdit");
                RaisePropertyChanged("CanEditSOC");
            }
        }

        public bool Admitted
        {
            get
            {
                if (_Admitted == null)
                {
                    Admitted = AdmissionStatusCode == "A" ? true : false;
                }

                return (bool)_Admitted;
            }
            set
            {
                _Admitted = value;
                RaisePropertyChanged("Admitted");
            }
        }

        public Entity ProviderFacility
        {
            get
            {
                if (_providerFacility == null)
                {
                    SetProviderFacility();
                }

                return _providerFacility;
            }
        }

        public string CMSCertificationNumber
        {
            get
            {
                if (ProviderFacility is OasisHeader)
                {
                    return ((OasisHeader)ProviderFacility).CMSCertificationNumber;
                }

                return null;
            }
        }

        public EncounterTransfer MostRecentTransfer
        {
            get
            {
                //NOTE: server code should only be sending a single (most recent) EncounterTransfer row to the client
                return EncounterTransfer.Where(e => e.TransferDate != null).OrderByDescending(o => o.TransferDate)
                    .ThenByDescending(o => o.EncounterTransferKey).FirstOrDefault();
            }
        }

        public EncounterResumption MostRecentResumption
        {
            get
            {
                //NOTE: server code should only be sending a single (most recent) EncounterResumption row to the client
                return EncounterResumption.Where(e => e.ResumptionDate != null).OrderByDescending(o => o.ResumptionDate)
                    .FirstOrDefault();
            }
        }

        public DateTime? ResumptionDate
        {
            get
            {
                var resump = MostRecentResumption;
                var transfer = MostRecentTransfer;
                if (resump != null && transfer != null)
                {
                    if ((resump.ResumptionDate == null ? resump.ResumptionReferralDate : resump.ResumptionDate) >=
                        transfer.TransferDate)
                    {
                        return resump.ResumptionDate == null ? resump.ResumptionReferralDate : resump.ResumptionDate;
                    }
                }
                else
                {
                    if (MostRecentTransfer != null)
                    {
                        var ad = AdmissionDiscipline.OrderByDescending(ado => ado.ReferDateTime)
                            .Where(adr =>
                                adr.NotTaken == false && adr.DischargeDateTime == null &&
                                adr.DisciplineAdmitDateTime >= MostRecentTransfer.TransferDate).FirstOrDefault();
                        if (ad != null)
                        {
                            return ad.DisciplineAdmitDateTime;
                        }
                    }
                    else if (resump != null)
                    {
                        return resump.ResumptionDate == null ? resump.ResumptionReferralDate : resump.ResumptionDate;
                    }
                    else
                    {
                        return null;
                    }
                }

                return null;
            }
        }

        public List<Encounter> EncountersWithOasisList
        {
            get
            {
                if (Encounter == null)
                {
                    return null;
                }

                if (Encounter.Any() == false)
                {
                    return null;
                }

                var eList = Encounter
                    .Where(e => e.SYS_CDIsHospice == false && e.IsEncounterOasis && e.Inactive == false &&
                                e.EncounterStatus == (int)EncounterStatusType.Completed)
                    .OrderByDescending(e => e.EncounterOrTaskStartDateAndTime).ToList();
                if (eList == null)
                {
                    return null;
                }

                if (eList.Any() == false)
                {
                    return null;
                }

                return eList;
            }
        }

        public List<Encounter> EncountersWithHISList
        {
            get
            {
                if (Encounter == null)
                {
                    return null;
                }

                if (Encounter.Any() == false)
                {
                    return null;
                }

                var eList = Encounter
                    .Where(e => e.SYS_CDIsHospice && e.IsEncounterOasis && e.Inactive == false &&
                                e.EncounterStatus == (int)EncounterStatusType.Completed)
                    .OrderByDescending(e => e.EncounterOrTaskStartDateAndTime).ToList();
                if (eList == null)
                {
                    return null;
                }

                if (eList.Any() == false)
                {
                    return null;
                }

                return eList;
            }
        }

        public List<Encounter> EncountersForOrdersTab
        {
            get
            {
                // for now, we'll want to return everything that is either a POC or in Order Entery
                if (Encounter == null)
                {
                    return null;
                }

                if (Encounter.Any() == false)
                {
                    return null;
                }

                var eList = Encounter.Where(e => e.EncounterIsOrderEntry
                                                 || e.FormKey.HasValue
                                                 && DynamicFormCache.IsPlanOfCare(e.FormKey.Value)
                ).OrderByDescending(e => e.EncounterOrTaskStartDateAndTime).ToList();
                if (eList == null)
                {
                    return null;
                }

                if (eList.Any() == false)
                {
                    return null;
                }

                return eList;
            }
        }

        public List<Encounter> EncountersWithOrderEntryList
        {
            get
            {
                if (Encounter == null)
                {
                    return null;
                }

                if (Encounter.Any() == false)
                {
                    return null;
                }

                var eList = Encounter.Where(e => e.EncounterIsOrderEntry && e.Inactive == false)
                    .OrderByDescending(e => e.EncounterOrTaskStartDateAndTime).ToList();
                if (eList == null)
                {
                    return null;
                }

                if (eList.Any() == false)
                {
                    return null;
                }

                return eList;
            }
        }

        public bool AreEncountersWithOrderEntry => EncountersWithOrderEntryList == null ? false : true;

        public DateTime? DateForCertCycleDisplay { get; set; }


        public AdmissionCertification FirstCert
        {
            get
            {
                AdmissionCertification acReturn = null;
                if (AdmissionCertification != null)
                {
                    acReturn = AdmissionCertification.OrderBy(ac => ac.PeriodStartDate).FirstOrDefault();
                }

                return acReturn;
            }
        }

        public DateTime? FirstCertThruDate
        {
            // Currently this should only be able to be set by the user if this Hospice Admission && TransferHospice
            get { return FirstCert == null ? null : FirstCert.PeriodEndDate; }
            set
            {
                if (IsEditting || InDynamicForm) // we are getting in here during the GetOriginal() method.
                {
                    if (FirstCert != null)
                    {
                        FirstCert.PeriodEndDate = value;
                    }

                    RaisePropertyChanged("FirstCertPeriodNumber");
                    RaisePropertyChanged("FirstCertFromDate");
                    RaisePropertyChanged("FirstCertThruDate");
                    RaisePropertyChanged("CurrentCertCycleTodayPhrase");
                    RaisePropertyChanged("MostRecentPOCCertCyclePhrase");
                }
            }
        }

        public DateTime? FirstCertFromDate
        {
            get { return FirstCert == null ? null : FirstCert.PeriodStartDate; }
            set
            {
                if (IsEditting || InDynamicForm) // we are getting in here during the GetOriginal() method.
                {
                    if (FirstCert != null && FirstCert.IsNew && value == null)
                    {
                        AdmissionCertification.Remove(FirstCert);
                    }

                    if (FirstCert != null)
                    {
                        FirstCert.PeriodStartDate = value;
                    }

                    if (FirstCert != null && FirstCert.PeriodStartDate.HasValue)
                    {
                        FirstCert.SetPeriodEndDateForRow();
                    }

                    RaisePropertyChanged("FirstCertPeriodNumber");
                    RaisePropertyChanged("FirstCertFromDate");
                    RaisePropertyChanged("FirstCertThruDate");
                    RaisePropertyChanged("CurrentCertCycleTodayPhrase");
                    RaisePropertyChanged("MostRecentPOCCertCyclePhrase");
                }
            }
        }

        public bool HasFirstCert => FirstCert == null ? false : true;

        public int? FirstCertPeriodNumber
        {
            get
            {
                if (FirstCert == null)
                {
                    return StartPeriodNumber;
                }

                return FirstCert.PeriodNumber;
            }
            set
            {
                if (IsEditting || InDynamicForm)
                {
                    _UserTypedPeriodNumber = value.GetValueOrDefault();
                    StartPeriodNumber = value;
                    if (FirstCert != null && value != null)
                    {
                        FirstCert.PeriodNumber = value.GetValueOrDefault();
                        CurrentCertPeriodNumber = FirstCert.PeriodNumber;
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            if (FirstCert != null && FirstCert.PeriodNumber == 1 && SOCDate != null &&
                                FirstCertFromDate != null)
                            {
                                FirstCertFromDate = SOCDate;
                            }
                        });
                        SetupCertCycles(); //will get called from the setter of CurrentCertPeriodNumber
                    }
                    else if (HospiceAdmission && (TransferHospice || HospiceBenefitReelection))
                    {
                        SetupCertCycles();
                    }

                    RaisePropertyChanged("FirstCertPeriodNumberEnabled");
                    RaisePropertyChanged("FirstCertFromDateEnabled");
                    RaisePropertyChanged("FirstCertPeriodNumber");
                    RaisePropertyChanged("FirstCertFromDate");
                    RaisePropertyChanged("FirstCertThruDate");
                    RaisePropertyChanged("CurrentCertPeriodNumber");
                    RaisePropertyChanged("CurrentCertCycleTodayPhrase");
                    RaisePropertyChanged("MostRecentPOCCertCyclePhrase");
                    RaisePropertyChanged("CanModifyCertPeriodMaint");
                }
            }
        }

        public AdmissionCertification CurrentCert
        {
            get
            {
                AdmissionCertification acReturn = null;
                if (DateForCertCycleDisplay == null)
                {
                    acReturn = CurrentCertCycleToday;
                }
                else
                {
                    acReturn = GetAdmissionCertForDate((DateTime)DateForCertCycleDisplay);
                }

                if (acReturn != null)
                {
                    if (CurrentCertKey != acReturn.AdmissionCertKey)
                    {
                        CurrentCertKey = acReturn.AdmissionCertKey;
                    }

                    if (CurrentCertPeriodNumber != acReturn.PeriodNumber)
                    {
                        CurrentCertPeriodNumber = acReturn.PeriodNumber;
                    }

                    RaisePropertyChanged("CurrentCertKey");
                }

                return acReturn;
            }
        }

        public DateTime? CurrentCertFromDate
        {
            get { return CurrentCert == null ? null : CurrentCert.PeriodStartDate; }
            set
            {
                if (CurrentCert != null && value != null)
                {
                    CurrentCert.PeriodStartDate = value;
                    SetupCertCycles();
                }

                RaisePropertyChanged("CurrentCertCycleTodayPhrase");
                RaisePropertyChanged("MostRecentPOCCertCyclePhrase");
            }
        }

        public DateTime? CurrentCertThruDate => CurrentCert == null ? null : CurrentCert.PeriodEndDate;

        public int CurrentCertKey { get; set; }

        public AdmissionCertification CurrentCertCycleToday => GetAdmissionCertForDate(DateTime.Today);

        public string CertificationPeriodsShortLabel
        {
            get
            {
                if (HideCertPeriods)
                {
                    return string.Empty;
                }

                return CanModifyCertCycle ? "Period Number" : "Certification Period";
            }
        }

        public string CertificationPeriodsLabel
        {
            get
            {
                if (HideCertPeriods)
                {
                    return string.Empty;
                }

                return HospiceAdmission ? "Benefits Period" : "Certification Period";
            }
        }

        private AdmissionCertification MostRecentPOCCertCycle
        {
            get
            {
                // Note - no guarantee OrdersTracking is in use
                AdmissionCertification ac = null;
                EncounterPlanOfCare epoc = null;
                if (AdmissionCertification == null || AdmissionCertification.Any() == false)
                {
                    return null;
                }

                // First try to honor POC orders tracking rows that are attached to valid POCs
                var otList = OrdersTracking != null && OrdersTracking.Any()
                    ? null
                    : OrdersTracking.Where(x =>
                        x.EncounterPlanOfCareKey != null && x.AdmissionCertKey > 0 &&
                        x.OrderType == (int)OrderTypesEnum.POC && x.Inactive == false && x.CancelDate == null &&
                        x.Status != (int)OrdersTrackingStatus.Void).ToList();
                if (otList != null && otList.Any())
                {
                    var acList = new List<AdmissionCertification>();
                    foreach (var ot in otList)
                    {
                        epoc = Encounter.Where(x =>
                                x.EncounterStatus != (int)EncounterStatusType.None && x.Inactive == false &&
                                x.HistoryKey == null && x.EncounterIsPlanOfCare && x.MyEncounterPlanOfCare != null &&
                                x.MyEncounterPlanOfCare.EncounterPlanOfCareKey == ot.EncounterPlanOfCareKey)
                            .Select(x => x.MyEncounterPlanOfCare).FirstOrDefault();
                        if (epoc != null)
                        {
                            ac = AdmissionCertification.Where(x =>
                                x.AdmissionCertKey == ot.AdmissionCertKey && x.PeriodStartDate != null &&
                                x.PeriodEndDate != null).FirstOrDefault();
                            if (ac != null)
                            {
                                acList.Add(ac);
                            }
                        }
                    }

                    ac = acList.OrderByDescending(x => x.PeriodStartDate).FirstOrDefault();
                    if (ac != null)
                    {
                        return ac;
                    }
                }

                // Next, try to Honor most recent POC attached to the Admission
                var epocList = Encounter
                    .Where(x => x.EncounterStatus != (int)EncounterStatusType.None && x.Inactive == false &&
                                x.HistoryKey == null && x.EncounterIsPlanOfCare && x.MyEncounterPlanOfCare != null)
                    .Select(x => x.MyEncounterPlanOfCare).ToList();
                if (epocList == null || epocList.Any() == false)
                {
                    return null;
                }

                epoc = epocList.Where(x => x.CertificationFromDate != null && x.CertificationThruDate != null)
                    .OrderByDescending(x => x.CertificationFromDate).FirstOrDefault();
                if (epoc == null)
                {
                    return null;
                }

                // First, try to honor POCS OrdersTracking row (roughly a duplicate from above - but a different pecking order)
                if (OrdersTracking != null && OrdersTracking.Any())
                {
                    // Try to Honor POCS OrdersTracking row first
                    var ot = OrdersTracking.Where(x =>
                        x.EncounterPlanOfCareKey == epoc.EncounterPlanOfCareKey && x.AdmissionCertKey > 0 &&
                        x.OrderType == (int)OrderTypesEnum.POC && x.Inactive == false && x.CancelDate == null &&
                        x.Status != (int)OrdersTrackingStatus.Void).FirstOrDefault();
                    if (ot != null)
                    {
                        ac = AdmissionCertification.Where(x =>
                            x.AdmissionCertKey == ot.AdmissionCertKey && x.PeriodStartDate != null &&
                            x.PeriodEndDate != null).FirstOrDefault();
                    }

                    if (ac != null)
                    {
                        return ac;
                    }
                }

                // Next, try to honor AdmissionCertification based on POC CertificationFromDate
                ac = GetAdmissionCertForDate(((DateTime)epoc.CertificationFromDate).Date, false);
                if (ac != null)
                {
                    return ac;
                }

                // Lastly, try to honor AdmissionCertification based on POC CertificationThruDate
                ac = GetAdmissionCertForDate(((DateTime)epoc.CertificationThruDate).Date, false);
                return ac;
            }
        }

        public string MostRecentPOCCertCyclePhrase
        {
            get
            {
                if (HideCertPeriods)
                {
                    return string.Empty;
                }

                // For an admission that is Not taken no cert period displays
                if (AdmissionStatusCode == "N")
                {
                    return string.Empty;
                }

                // For an admission that is On hold no cert period displays
                if (AdmissionStatusCode == "H")
                {
                    return string.Empty;
                }

                // For an admission that is INITIALLY referred, no cert period displays
                if (AdmissionStatusCode == "R" && MostRecentTransfer == null)
                {
                    return string.Empty;
                }

                var dt = DateTime.Today.Date;

                // For an admission that is admitted or resumed,, the current cert period displays
                if (AdmissionStatusCode == "A" || AdmissionStatusCode == "M")
                {
                    dt = DateTime.Today.Date;
                }
                // For an admission that is transferred or re-referred after a transfer - the cert period that displays is the period that contains the transfer date.
                // Note - the initial referral was skimmed off above
                else if (AdmissionStatusCode == "T" || AdmissionStatusCode == "R")
                {
                    dt = MostRecentTransfer != null && MostRecentTransfer.TransferDate != null
                        ? MostRecentTransfer.TransferDate.Date
                        : DateTime.Today.Date;
                }
                // For an admission that is discharged - the cert period that displays is the period that contains the discharge date.
                else if (AdmissionStatusCode == "D")
                {
                    dt = DischargeDateTime != null ? ((DateTime)DischargeDateTime).Date : DateTime.Today.Date;
                }

                // get the cert cycle for the given admission status related date 
                var ac = GetAdmissionCertForDate(dt); // default to last if non found for the date
                var DateString = "";
                if (ac != null)
                {
                    DateString = ac.PeriodStartDate != null
                        ? ((DateTime)ac.PeriodStartDate).ToString("MM/dd/yyyy")
                        : "";
                    if (ac.PeriodEndDate != null)
                    {
                        DateString = DateString + " Thru " + ((DateTime)ac.PeriodEndDate).ToString("MM/dd/yyyy");
                    }
                }

                return (HospiceAdmission ? "Period of Care : " : "Certification Period : ") +
                       (string.IsNullOrWhiteSpace(DateString) ? "Not yet established" : DateString);
            }
        }

        private string MostRecentPOCCertCyclePhrase2
        {
            get
            {
                if (HideCertPeriods)
                {
                    return string.Empty;
                }

                var ac = MostRecentPOCCertCycle;
                var DateString = "";
                if (ac != null)
                {
                    DateString = ac.PeriodStartDate != null
                        ? ((DateTime)ac.PeriodStartDate).ToString("MM/dd/yyyy")
                        : "";
                    if (ac.PeriodEndDate != null)
                    {
                        DateString = DateString + " Thru " + ((DateTime)ac.PeriodEndDate).ToString("MM/dd/yyyy");
                    }
                }

                return (HospiceAdmission ? "Period of Care : " : "Certification Period : ") +
                       (string.IsNullOrWhiteSpace(DateString) ? "Not yet established" : DateString);
            }
        }

        public string CurrentServiceLinePhrase
        {
            get
            {
                string groupings = null;
                var group = CurrentGroup?.GroupingName;
                if (string.IsNullOrWhiteSpace(group) == false)
                {
                    groupings = groupings + (groupings == null ? group : "/" + group);
                }

                group = CurrentGroup2?.GroupingName;
                if (string.IsNullOrWhiteSpace(group) == false)
                {
                    groupings = groupings + (groupings == null ? group : "/" + group);
                }

                group = CurrentGroup3?.GroupingName;
                if (string.IsNullOrWhiteSpace(group) == false)
                {
                    groupings = groupings + (groupings == null ? group : "/" + group);
                }

                group = CurrentGroup4?.GroupingName;
                if (string.IsNullOrWhiteSpace(group) == false)
                {
                    groupings = groupings + (groupings == null ? group : "/" + group);
                }

                group = CurrentGroup5?.GroupingName;
                if (string.IsNullOrWhiteSpace(group) == false)
                {
                    groupings = groupings + (groupings == null ? group : "/" + group);
                }

                return ServiceLineDescription + (groupings == null ? "" : "  Groupings: " + groupings);
            }
        }

        public string CurrentCertCycleTodayPhrase
        {
            get
            {
                if (HideCertPeriods)
                {
                    return string.Empty;
                }

                var ac = CurrentCertCycleToday;
                var DateString = "";
                if (ac != null)
                {
                    DateString = ac.PeriodStartDate != null
                        ? ((DateTime)ac.PeriodStartDate).ToString("MM/dd/yyyy")
                        : "";
                    if (ac.PeriodEndDate != null)
                    {
                        DateString = DateString + " Thru " + ((DateTime)ac.PeriodEndDate).ToString("MM/dd/yyyy");
                    }
                }

                return (HospiceAdmission ? "Period of Care : " : "Certification Period : ") + DateString;
            }
        }

        public int? CurrentCertPeriodNumber
        {
            get { return _currentCertPeriodNumber; }
            set
            {
                if (_currentCertPeriodNumber != value)
                {
                    _currentCertPeriodNumber = value;
                    RaisePropertyChanged("CurrentCertPeriodNumber");
                    RaisePropertyChanged("CurrentCertCycleTodayPhrase");
                    RaisePropertyChanged("MostRecentPOCCertCyclePhrase");
                }
            }
        }

        public bool FirstCertPeriodNumberEnabled
        {
            get
            {
                // If the Hospice Admission is Transfer or Reelecting benefits, then allow edit of from date after the PeriodNumber has been entered
                // if we are only a Hospice Admission without TransferHospice or HospiceBenefitReelection being checked, do not allow the user to edit
                if (HospiceAdmission)
                {
                    return TransferHospice || HospiceBenefitReelection;
                }

                // Allow all other servicelines to behave as usual
                return CanModifyCertCycleMaint;
            }
        }

        public bool FirstCertFromDateEnabled
        {
            get
            {
                if (HospiceAdmission)
                {
                    // If the Hospice Admission is Transfer or Reelecting benefits, then allow edit of from date after the PeriodNumber has been entered
                    if (TransferHospice || HospiceBenefitReelection)
                    {
                        return FirstCertPeriodNumber.HasValue;
                    }

                    return
                        false; // if we are only a Hospice Admission without TransferHospice or HospiceBenefitReelection being checked, do not allow the user to edit
                }

                // Allow all other servicelines to behave as usual
                return CanModifyCertCycleMaint;
            }
        }

        // ThruDate is never editable
        public bool FirstCertThruDateEnabled => false;

        [DataMember]
        public bool ValidateState_IsEval
        {
            get { return _ValidateState_IsEval; }
            set
            {
                _ValidateState_IsEval = value;
                RaisePropertyChanged("ValidateState_IsEval");
            }
        }

        [DataMember]
        public bool ValidateState_IsEvalFullValidation
        {
            get { return _ValidateState_IsEvalFullValidation; }
            set
            {
                _ValidateState_IsEvalFullValidation = value;
                RaisePropertyChanged("ValidateState_IsEvalFullValidation");
            }
        }

        [DataMember]
        public bool ValidateState_IsPreEval
        {
            get { return _ValidateState_IsPreEval; }
            set
            {
                _ValidateState_IsPreEval = value;
                RaisePropertyChanged("ValidateState_IsPreEval");
            }
        }

        [DataMember]
        public bool IsContractProvider
        {
            get { return _IsContractProvider; }
            set
            {
                _IsContractProvider = value;
                RaisePropertyChanged("IsContractProvider");
            }
        }

        public string ProviderName
        {
            get
            {
                if (ProviderFacility is OasisHeader)
                {
                    return ((OasisHeader)ProviderFacility).OasisHeaderName;
                }

                if (ProviderFacility is Facility)
                {
                    return ((Facility)ProviderFacility).Name;
                }

                return null;
            }
        }

        public string ProviderAddress1
        {
            get
            {
                if (ProviderFacility is OasisHeader)
                {
                    return ((OasisHeader)ProviderFacility).Address1;
                }

                if (ProviderFacility is Facility)
                {
                    return ((Facility)ProviderFacility).Address1;
                }

                return null;
            }
        }

        public string ProviderAddress2
        {
            get
            {
                if (ProviderFacility is OasisHeader)
                {
                    return ((OasisHeader)ProviderFacility).Address2;
                }

                if (ProviderFacility is Facility)
                {
                    return ((Facility)ProviderFacility).Address2;
                }

                return null;
            }
        }

        public string ProviderCity
        {
            get
            {
                if (ProviderFacility is OasisHeader)
                {
                    return ((OasisHeader)ProviderFacility).City;
                }

                if (ProviderFacility is Facility)
                {
                    return ((Facility)ProviderFacility).City;
                }

                return null;
            }
        }

        public int? ProviderStateCode
        {
            get
            {
                if (ProviderFacility is OasisHeader)
                {
                    return ((OasisHeader)ProviderFacility).StateCode;
                }

                if (ProviderFacility is Facility)
                {
                    return ((Facility)ProviderFacility).StateCode;
                }

                return null;
            }
        }

        public string ProviderZipCode
        {
            get
            {
                if (ProviderFacility is OasisHeader)
                {
                    return ((OasisHeader)ProviderFacility).ZipCode;
                }

                if (ProviderFacility is Facility)
                {
                    return ((Facility)ProviderFacility).ZipCode;
                }

                return null;
            }
        }

        public string ProviderCityStateZip
        {
            get
            {
                if (ProviderFacility is OasisHeader)
                {
                    return ((OasisHeader)ProviderFacility).CityStateZip;
                }

                if (ProviderFacility is Facility)
                {
                    return ((Facility)ProviderFacility).CityStateZip;
                }

                return null;
            }
        }

        public string ProviderPhoneExtension
        {
            get
            {
                if (ProviderFacility is OasisHeader)
                {
                    return ((OasisHeader)ProviderFacility).PhoneExtension;
                }

                if (ProviderFacility is Facility)
                {
                    return ((Facility)ProviderFacility).PhoneExtension;
                }

                return null;
            }
        }

        public string ProviderPhoneNumber
        {
            get
            {
                if (ProviderFacility is OasisHeader)
                {
                    return ((OasisHeader)ProviderFacility).Number;
                }

                if (ProviderFacility is Facility)
                {
                    return ((Facility)ProviderFacility).Number;
                }

                return null;
            }
        }

        public string ProviderFaxNumber
        {
            get
            {
                if (ProviderFacility is OasisHeader)
                {
                    return ((OasisHeader)ProviderFacility).FaxNumber;
                }

                if (ProviderFacility is Facility)
                {
                    return ((Facility)ProviderFacility).Fax;
                }

                return null;
            }
        }

        public bool CalculateAgencyDischarge(AdmissionDiscipline ad, bool pOverrideAgencyDischarge)
        {
            if (ad == null)
            {
                return false;
            }

            var agencyDischarge = IsHomeCare || pOverrideAgencyDischarge ? false :
                IsLastSkilledDiscipline(ad) ? true : false; // Default based on skilled disciplines
            if (ad.ReasonDCKey == (int)CodeLookupCache.GetKeyFromCode("REASONDC", "DIED"))
            {
                agencyDischarge = true; // override if expired
            }

            ad.AgencyDischarge = agencyDischarge;
            return agencyDischarge;
        }

        public AdmissionSiteOfService GetAdmissionSiteOfService(DateTime? pEffectiveDateTime)
        {
            if (HospiceAdmission == false || AdmissionSiteOfService == null)
            {
                return null;
            }

            var effectiveDateTime = pEffectiveDateTime == null
                ? DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
                : (DateTime)pEffectiveDateTime;
            var asos = AdmissionSiteOfService.Where(sos =>
                    sos.HistoryKey == null && sos.SiteOfServiceFromDateTimeHasValue &&
                    sos.SiteOfServiceFromDateTimeSort <= effectiveDateTime &&
                    (sos.SiteOfServiceThruDateTimeHasValue == false || sos.SiteOfServiceThruDateTimeHasValue &&
                        sos.SiteOfServiceThruDateTimeSort >= effectiveDateTime))
                .OrderBy(sos => sos.SiteOfServiceFromDateTimeSort)
                .FirstOrDefault();
            return asos;
        }

        public bool AdmissionSiteOfServiceIsApplicableToMAR(DateTimeOffset? pEffectiveDateTime)
        {
            var effectiveDateTime = pEffectiveDateTime == null
                ? DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
                : ((DateTimeOffset)pEffectiveDateTime).DateTime;
            var asos = GetAdmissionSiteOfService(effectiveDateTime);
            if (asos == null)
            {
                return false;
            }

            return asos.SiteOfServiceIsApplicableToMAR;
        }

        public bool LevelOfCareIsInpatientHospice(DateTimeOffset? pEffectiveDate)
        {
            if (HospiceAdmission == false)
            {
                return false;
            }

            if (AdmissionLevelOfCare == null)
            {
                return false;
            }

            var effectiveDate = pEffectiveDate == null
                ? DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified).Date
                : ((DateTimeOffset)pEffectiveDate).Date;
            var locList = AdmissionLevelOfCare.Where(aloc => aloc.Superceded == false &&
                                                             (aloc.LevelOfCareFromDate.HasValue == false ||
                                                              aloc.LevelOfCareFromDate.HasValue &&
                                                              ((DateTime)aloc.LevelOfCareFromDate).Date <=
                                                              effectiveDate) &&
                                                             (aloc.LevelOfCareThruDate.HasValue == false ||
                                                              aloc.LevelOfCareThruDate.HasValue &&
                                                              ((DateTime)aloc.LevelOfCareThruDate).Date >=
                                                              effectiveDate))
                .ToList();
            if (locList == null || locList.Any() == false)
            {
                return false;
            }

            var isInpatientHospice = locList.Where(aloc => aloc.LevelOfCareLocationIsInpatientHospice).Any();
            return isInpatientHospice;
        }

        public bool IsLastSkilledDiscipline(AdmissionDiscipline myAD)
        {
            if (myAD == null || AdmissionDiscipline == null)
            {
                return false;
            }

            // If we are the last admitted discipline (not dicharged) - return true - skilled or not
            var adList = AdmissionDiscipline.Where(ad =>
                ad.HistoryKey == null && ad.AdmissionDisciplineWasAdmitted &&
                ad.AdmissionDisciplineWasDischarged == false).ToList();
            if (adList == null)
            {
                return false;
            }

            if (adList.Count == 1 && adList.Contains(myAD))
            {
                return true;
            }

            // now see if we are the last skilled discipline
            if (myAD.AdmissionDisciplineIsOTorPTorSLPorSN == false)
            {
                return false;
            }

            adList = adList.Where(ad => ad.AdmissionDisciplineIsOTorPTorSLPorSN).ToList();
            if (adList == null)
            {
                return false;
            }

            if (adList.Count == 1 && adList.Contains(myAD))
            {
                return true;
            }

            return false;
        }

        public DateTime HISTargetDate(string rfa)
        {
            // Target date is a function of RFA and Admission Admit/Discharge dates
            if (string.IsNullOrWhiteSpace(rfa))
            {
                return DateTime.Today.Date;
            }

            if (rfa == "01" && AdmissionWasAdmitted)
            {
                return ((DateTime)AdmitDateTime).Date;
            }

            if (rfa == "09" && AdmissionWasDischarged)
            {
                return ((DateTime)DischargeDateTime).Date;
            }

            return DateTime.Today.Date;
        }

        public AdmissionTeamMeeting GetAdmissionTeamMeetingForDate(DateTime date)
        {
            if (AdmissionTeamMeeting == null)
            {
                return null;
            }

            // Get the proper service line grouping row to use.
            return AdmissionTeamMeeting
                .Where(atm => atm.Inactive == false &&
                              (atm.LastTeamMeetingDate == null || atm.LastTeamMeetingDate != null &&
                                  date >= ((DateTime)atm.LastTeamMeetingDate).Date) &&
                              (atm.NextTeamMeetingDate == null || atm.NextTeamMeetingDate != null &&
                                  date <= ((DateTime)atm.NextTeamMeetingDate).Date))
                .OrderByDescending(atm => atm.UpdatedDate).FirstOrDefault();
        }

        public bool SOCMoreThanXDaysAgo(int days)
        {
            if (SOCDate == null)
            {
                return false;
            }

            if (((DateTime)SOCDate).Date.AddDays(days) >= DateTime.Today.Date)
            {
                return false;
            }

            return true;
        }

        public string AdmissionInsurancePOCCertStatement(int periodNumber, DateTime? periodStartDate)
        {
            var period = periodNumber == 0 ? 1 : periodNumber;
            var startDate = periodStartDate == null
                ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date
                : ((DateTime)periodStartDate).Date;

            string pocCertStatement = null;
            if (PatientInsuranceKey != null && PatientInsuranceKey != 0 && Patient != null &&
                Patient.PatientInsurance != null)
            {
                var pi = Patient.PatientInsurance.Where(p => p.PatientInsuranceKey == PatientInsuranceKey)
                    .FirstOrDefault();
                pocCertStatement = pi == null
                    ? null
                    : InsuranceCache.GetInsuranceCertStatement(pi.InsuranceKey, period, startDate);
            }

            if (string.IsNullOrWhiteSpace(pocCertStatement))
            {
                pocCertStatement = null;
            }

            return pocCertStatement;
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }

            var pc = new PhoneConverter();
            if (pc == null)
            {
                return null;
            }

            var phoneObject = pc.Convert(phoneNumber, null, null, null);
            if (phoneObject != null)
            {
                if (string.IsNullOrWhiteSpace(phoneObject.ToString()) == false)
                {
                    return phoneObject.ToString();
                }
            }

            return null;
        }

        public void ClearOriginalAdmissionRow()
        {
            OriginalAdmissionRow = null;
        }

        public void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            CleanupGroupedDisciplines();

            if (AdmissionReferral != null)
            {
                foreach (var ar in AdmissionReferral)
                    try
                    {
                        ar.PropertyChanged -= ChildPropertyChanged;
                    }
                    catch
                    {
                    }
            }

            if (AdmissionGoal != null)
            {
                AdmissionGoal.ForEach(ag => ag.Cleanup());
            }

            if (Patient != null)
            {
                Patient.Cleanup();
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (_FilteredItemsSource != null)
                {
                    _FilteredItemsSource.Source = null;
                }
            });

            if (_myEncounter != null)
            {
                _myEncounter.Cleanup();
                _myEncounter = null;
            }
        }

        partial void OnAdmissionKeyChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            RaisePropertyChanged("CanEditServiceLine");
            RaisePropertyChanged("ShowAuths");
            RaisePropertyChanged("ShowEligibility");
            RaisePropertyChanged("ShowLevelOfCare");
            RaisePropertyChanged("ShowHIS");
            RaisePropertyChanged("ShowOASIS");
            RaisePropertyChanged("ShowCMS");
            RaisePropertyChanged("CMSTabHeaderLabel");
            RaisePropertyChanged("UseHospiceBilling");
            RaisePropertyChanged("HospiceAdmission"); // Really, should this be the driver or should ServiceLine changes?
        }

        partial void OnIsEmploymentRelatedChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            if (!IsEmploymentRelated && EmploymentRelatedEmployer.HasValue)
            {
                EmploymentRelatedEmployer = null;
            }
        }

        public bool OnlyRoutineCareInDateRange(DateTime startDate, DateTime endDate)
        {
            // If nothing defined - or ill defined - assume only Routine
            if (AdmissionLevelOfCare == null)
            {
                return true;
            }

            var locList = AdmissionLevelOfCare.Where(aloc => aloc.Superceded == false &&
                                                             (aloc.LevelOfCareFromDate.HasValue == false ||
                                                              aloc.LevelOfCareFromDate.HasValue &&
                                                              ((DateTime)aloc.LevelOfCareFromDate).Date <= endDate) &&
                                                             (aloc.LevelOfCareThruDate.HasValue == false ||
                                                              aloc.LevelOfCareThruDate.HasValue &&
                                                              ((DateTime)aloc.LevelOfCareThruDate).Date >= startDate))
                .ToList();
            if (locList == null || locList.Any() == false)
            {
                return true;
            }

            var anyNonRoutine = locList.Where(aloc => aloc.LevelOfCareIsRoutine == false).Any();
            return anyNonRoutine ? false : true;
        }

        partial void OnPreEvalStatusChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }


            if (IsPreEvalStatusDoNotAdmit)
            {
                PreEvalOnHoldReason = null;
                PreEvalOnHoldDateTime = null;
                PreEvalFollowUpDate = null;
                PreEvalFollowUpComments = null;
                if (NotTakenDateTime == null)
                {
                    NotTakenDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                }

                NotTaken = true;
                AdmissionStatus = (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "N");
            }
            else if (IsPreEvalStatusOnHold)
            {
                if (PreEvalOnHoldDateTime == null)
                {
                    PreEvalOnHoldDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                }

                NotTakenReason = null;
                NotTaken = false;
                AdmissionStatus = (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "H");
            }
            else
            {
                PreEvalOnHoldReason = null;
                PreEvalOnHoldDateTime = null;
                PreEvalFollowUpDate = null;
                PreEvalFollowUpComments = null;
                NotTakenDateTime = null;
                NotTakenReason = null;
                NotTaken = false;
                AdmissionStatus = (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "R");
            }

            RaisePropertyChanged("IsPreEvalStatusPlanToAdmit");
            RaisePropertyChanged("IsPreEvalStatusDoNotAdmit");
            RaisePropertyChanged("IsPreEvalStatusOnHold");
        }

        public int? SetFaceToFaceEncounter(bool Onfile)
        {
            FaceToFaceEncounter = Onfile ? CodeLookupCache.GetKeyFromCode("FACETOFACE", "OnFile") :
                RequiresFaceToFaceOnAdmit ? CodeLookupCache.GetKeyFromCode("FACETOFACE", "DoWithCert") : null;
            return FaceToFaceEncounter;
        }

        partial void OnHasTraumaChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            if (HasTrauma.GetValueOrDefault() == false)
            {
                TraumaDate = null;
                TraumaType = null;
                TraumaStateCode = null;
            }
        }

        public void RaisePropertyChangedEventsCustom()
        {
            RaisePropertyChanged("CanModifyDischareFromTransfer");
            RaisePropertyChanged("DischargeLabelsVisible");
            RaisePropertyChanged("DischargeEditVisible");
            RaisePropertyChanged("DischargeDisplayVisible");
            RaisePropertyChanged("DischargeFromTransferBoxVisible");
            RaisePropertyChanged("AdmissionStatusCode");
            RaisePropertyChanged("AdmissionStatusText");
            RaisePropertyChanged("DischargedFromTransferUser");
        }

        private void CreateEncounterAddendum()
        {
            if (Encounter == null)
            {
                return;
            }

            if (OriginalAdmissionRow != null && OriginalAdmissionRow.DeathDate == DeathDate &&
                OriginalAdmissionRow.DeathTime == DeathTime)
            {
                return;
            }

            var dn = Encounter.Where(e =>
                    e.DeathNote == true && e.Inactive == false &&
                    e.EncounterStatus == (int)EncounterStatusType.Completed)
                .FirstOrDefault();
            if (dn != null)
            {
                var user = UserCache.Current.GetCurrentUserProfile().FullName;
                var time = "";
                if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    time = Convert.ToDateTime(((DateTimeOffset)DeathTime).DateTime).ToString("HHmm");
                }
                else
                {
                    time = Convert.ToDateTime(((DateTimeOffset)DeathTime).DateTime).ToShortTimeString();
                }

                var ea = dn.EncounterAddendum.Where(e => e.IsNew).FirstOrDefault();
                if (ea == null)
                {
                    ea = new EncounterAddendum();
                    dn.EncounterAddendum.Add(ea);
                }

                ea.AddendumText = string.Format("Date of death and/or time of death updated to {0} {1} by {2}",
                    ConvertDate(AdmissionDeathDate), time, user);
            }
        }

        partial void OnDeathDateChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            if (Patient != null)
            {
                Patient.DeathDate = DeathDate;
            }

            if (DeathTime.HasValue && DeathDate.HasValue)
            {
                var dt = new DateTimeOffset(DeathDate.Value.Year, DeathDate.Value.Month, DeathDate.Value.Day,
                    DeathTime.Value.Hour, DeathTime.Value.Minute, DeathTime.Value.Second, DeathTime.Value.Offset);
                DeathTime = dt;
            }
        }

        partial void OnDeathTimeChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            if (DeathTime.HasValue && DeathDate.HasValue)
            {
                var dt = new DateTimeOffset(DeathDate.Value.Year, DeathDate.Value.Month, DeathDate.Value.Day,
                    DeathTime.Value.Hour, DeathTime.Value.Minute, DeathTime.Value.Second, DeathTime.Value.Offset);
                DeathTime = dt;
            }
        }

        partial void OnDischargedFromTransferChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            if (DischargedFromTransfer && (AdmissionStatusCode == "T" || AdmissionStatusCode == "D"))
            {
                var adStat = CodeLookupCache.GetKeyFromCode("AdmissionStatus", "D");
                var up = UserCache.Current.GetCurrentUserProfile();
                if (adStat != null && up != null)
                {
                    AdmissionStatus = (int)adStat;
                    DischargedFromTransferUser = up.UserId;
                    RaisePropertyChangedEventsCustom();
                }
            }

            if (!DischargedFromTransfer && (AdmissionStatusCode == "D" || AdmissionStatusCode == "T"))
            {
                if (OriginalAdmissionRow == null)
                {
                    OriginalAdmissionRow = (Admission)GetOriginal();
                }

                var orig = OriginalAdmissionRow;
                if (orig == null)
                {
                    DischargedFromTransfer = true;
                }
                else if (orig.AdmissionStatusCode == "D")
                {
                    AdmissionStatus = (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "T");
                    DischargedFromTransferUser = null;
                    DischargeReasonKey = null;
                    DischargeDateTime = null;
                }
                else
                {
                    AdmissionStatus = orig.AdmissionStatus;
                    DischargedFromTransferUser = orig.DischargedFromTransferUser;
                    DischargeReasonKey = orig.DischargeReasonKey;
                    DischargeDateTime = orig.DischargeDateTime;
                }

                RaisePropertyChangedEventsCustom();
            }
        }

        public List<PatientInsurance> GetPatientInsuranceItemSource(int? patientInsuranceKey)
        {
            if (Patient == null)
            {
                return null;
            }

            if (Patient.PatientInsurance == null)
            {
                return null;
            }

            var key = patientInsuranceKey;
            if (key == null)
            {
                key = 0;
            }

            var pList =
                Patient.PatientInsurance
                    .Where(i => i.Inactive == false && i.HistoryKey == null && i.PatientInsuranceKey != 0 &&
                                i.EffectiveFromDate <= DateTime.UtcNow &&
                                (i.EffectiveThruDate.HasValue == false || i.EffectiveThruDate > DateTime.UtcNow) ||
                                i.PatientInsuranceKey == key)
                    .OrderBy(i => i.NameAndNumber).ToList();
            if (pList == null)
            {
                return null;
            }

            var pRetList = new List<PatientInsurance>();

            var aIsHospice = HospiceAdmission;
            var aIsHomeHealth = (int)eServiceLineType.HomeHealth == ServiceLineType;
            var aIsHomeCare = (int)eServiceLineType.HomeCare == ServiceLineType;

            foreach (var pi in pList)
            {
                var i = InsuranceCache.GetInsuranceFromKey(pi.InsuranceKey);
                if (i != null)
                {
                    if (aIsHomeHealth && i.IsValidForHomeHealth) // HomeHealth
                    {
                        pRetList.Add(pi);
                    }
                    else if (aIsHospice && i.IsValidForHospice) // Hospice
                    {
                        pRetList.Add(pi);
                    }
                    else if (aIsHomeCare && i.IsValidForHomeCare) // HomeCare
                    {
                        pRetList.Add(pi);
                    }
                }
            }

            return pRetList.Any() == false ? null : pRetList;
        }

        partial void OnHospiceBenefitReelectionChanged()
        {
            if (!HospiceBenefitReelection)
            {
                SOCDate = null; // reset SOC date when the user unchecks ReelectHospiceBenefits
                ClearPendingAdmissionCertifications();
            }

            ClearPendingAdmissionCertifications();

            RaisePropertyChanged("FirstCertPeriodNumberEnabled");
            RaisePropertyChanged("FirstCertFromDateEnabled");
            RaisePropertyChanged("FirstCertThruDateEnabled");
            RaisePropertyChanged("CanModifyCertCycle");
            RaisePropertyChanged("CanModifyCertCycleMaint");
            RaisePropertyChanged("CanModifyCertPeriodMaint");
            RaisePropertyChanged("CanEditSOC");
            RaisePropertyChanged("FirstCert");
            RaisePropertyChanged("FirstCertPeriodNumber");
            RaisePropertyChanged("FirstCertThruDate");
            RaisePropertyChanged("FirstCertFromDate");
            RaisePropertyChanged("CertificationPeriodsShortLabel");
            RaisePropertyChanged("HideCertPeriods");


            UpdatePropertiesOnViewModel();
        }

        partial void OnTransferHospiceChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            if (TransferHospice == false)
            {
                TransferHospiceAgency = null;
                if (OriginalAdmissionRow == null)
                {
                    OriginalAdmissionRow = (Admission)GetOriginal();
                }

                var a = OriginalAdmissionRow;
                if (a != null)
                {
                    if (a.SOCDate != null && SOCDate.HasValue || a.SOCDate != SOCDate)
                    {
                        SOCDate = a.SOCDate;
                    }
                }
                else
                {
                    SOCDate = null; // reset SOCdate if an original admission row is not found
                }

                ClearPendingAdmissionCertifications();
            }

            UpdatePropertiesOnViewModel();
            RaisePropertyChanged("FirstCertPeriodNumberEnabled");
            RaisePropertyChanged("FirstCertFromDateEnabled");
            RaisePropertyChanged("FirstCertThruDateEnabled");
            RaisePropertyChanged("CanModifyCertCycle");
            RaisePropertyChanged("CanModifyCertCycleMaint");
            RaisePropertyChanged("CanModifyCertPeriodMaint");
            RaisePropertyChanged("CanEditSOC");
            RaisePropertyChanged("FirstCert");
            RaisePropertyChanged("FirstCertPeriodNumber");
            RaisePropertyChanged("FirstCertThruDate");
            RaisePropertyChanged("FirstCertFromDate");
            RaisePropertyChanged("CertificationPeriodsShortLabel");
            RaisePropertyChanged("HideCertPeriods");
        }

        private void ClearPendingAdmissionCertifications()
        {
            if (AdmissionCertification != null)
            {
                var lst = AdmissionCertification.Where(ac => ac.IsNew).ToList();
                lst.ForEach(ac => AdmissionCertification.Remove(ac));
            }
        }

        private void UpdatePropertiesOnViewModel()
        {
            RaisePropertyChanged("CanModifyHospiceTransfer");
            RaisePropertyChanged("CanModifyHospiceBenefitReelection");
        }

        private void DefaultHospice()
        {
            if (ServiceLineKey <= 0)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    RaisePropertyChanged("PatientInsuranceItemSource");
                });
                if (CurrentGroup != null)
                {
                    CurrentGroup.ServiceLineGroupingKey = 0;
                }

                CareCoordinator = null;
                HospiceAdmission = false;
                UseHospiceBilling = false;
                TransferHospice = false;
                TransferHospiceAgency = null;
                PreEvalRequired = false;
                PatientInsuranceKey = null;
                SourceOfAdmission = null;
                return;
            }

            prevSourceOfAdmission = SourceOfAdmission;
            SourceOfAdmission = null;
            var sl = ServiceLineCache.GetServiceLineFromKey(ServiceLineKey);
            if (sl == null)
            {
                return;
            }

            HospiceAdmission = sl.IsHospiceServiceLine;
            UseHospiceBilling = HospiceAdmission;

            // reset SourceOfAdmission if its still in the list
            RaisePropertyChanged("SourceOfAdmissionCodeType");

            var clList = CodeLookupCache.GetCodeLookupsFromType(SourceOfAdmissionCodeType);
            if (clList != null && prevSourceOfAdmission != null &&
                clList.Where(c => c.CodeLookupKey == (int)prevSourceOfAdmission).Any())
            {
                SourceOfAdmission = prevSourceOfAdmission;
            }

            if (sl.IsHospiceServiceLine == false)
            {
                TransferHospice = false;
                TransferHospiceAgency = null;
                PreEvalRequired = ServiceLineCache.Current.ServiceLineNonHospicePreEvalRequired(ServiceLineKey);
                FaceToFaceEncounter = null;
                FaceToFaceEncounterDate = null;
                FaceToFacePhysicianKey = null;
                FaceToFaceExceptReason = null;
                HasTrauma = false;
                PhysicianOrderedSOCDate = null;
            }
            else
            {
                PerformOasis = false;
                PreEvalRequired = ServiceLineCache.Current.ServiceLineHospicePreEvalRequired(ServiceLineKey);
            }

            // Re-Default PatientInsurance if there is only one
            if (PatientInsuranceItemSource != null)
            {
                if (PatientInsuranceItemSource.Any() == false)
                {
                    PatientInsuranceKey = null;
                }
                else if (PatientInsuranceItemSource.Count == 1)
                {
                    PatientInsuranceKey = PatientInsuranceItemSource[0].PatientInsuranceKey;
                }
                else if (PatientInsuranceKey != null &&
                         PatientInsuranceItemSource.Where(p => p.PatientInsuranceKey == PatientInsuranceKey).Any() ==
                         false)
                {
                    PatientInsuranceKey = null;
                }
            }
            else
            {
                PatientInsuranceKey = null;
            }

            Deployment.Current.Dispatcher.BeginInvoke(() => { RaisePropertyChanged("PatientInsuranceItemSource"); });
            Deployment.Current.Dispatcher.BeginInvoke(() => { RaisePropertyChanged("PatientInsuranceKey"); });
        }

        partial void OnServiceLineKeyChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            if (IsNew == false)
            {
                return;
            }

            if (AdmissionKey > 0)
            {
                return;
            }

            DefaultHospice();

            RaisePropertyChanged("ServiceLineGroupingItemsSource");

            _selectedServiceLine = null;

            RaisePropertyChanged("CurrentGroup");
            RaisePropertyChanged("CurrentGroup2");
            RaisePropertyChanged("CurrentGroup3");
            RaisePropertyChanged("CurrentGroup4");
            RaisePropertyChanged("CurrentGroup5");
            RaisePropertyChanged("ServiceLineGrouping1");
            RaisePropertyChanged("ServiceLineGrouping2");
            RaisePropertyChanged("ServiceLineGrouping3");
            RaisePropertyChanged("ServiceLineGrouping4");
            RaisePropertyChanged("ServiceLineGrouping5");
            RaisePropertyChanged("PreEvalRequired");
            RaisePropertyChanged("ShowPreEvalRequired");
            RaisePropertyChanged("PreEvalRequiredEnabled");
            RaisePropertyChanged("FaceToFaceRequired");
            RaisePropertyChanged("SourceOfAdmissionCodeType");
            RaisePropertyChanged("SourceOfAdmissionRequiredLabel");
            RaisePropertyChanged("SourceOfAdmissionLabel");
            RaisePropertyChanged("StartOfCareDateLabel");
            RaisePropertyChanged("ReferralDateLabel");
            RaisePropertyChanged("ReferralDateRequiredLabel");
            RaisePropertyChanged("CanEditPhysicianSOC");
            RaisePropertyChanged("CanEditSOC");
            RaisePropertyChanged("UseHospiceBilling");
        }

        public void RefreshRaiseChanged()
        {
            RaisePropertyChanged("This");
            RaisePropertyChanged("CurrentReferral");
            RaisePropertyChanged("Facility1Label");
            RaisePropertyChanged("Facility2Label");
            RaisePropertyChanged("Facility3Label");
            RaisePropertyChanged("ServicePriorityOneLabel");
            RaisePropertyChanged("ServicePriorityTwoLabel");
            RaisePropertyChanged("ServicePriorityThreeLabel");
            RaisePropertyChanged("Facility3Label");
            RaisePropertyChanged("AdmissionStatusText");
            RaisePropertyChanged("ServiceLineItemSource");
            RaisePropertyChanged("ServiceLineKey");
            RaisePropertyChanged("PeriodEndDate");
            RaisePropertyChanged("CurrentCertPeriodNumber");
            RaisePropertyChanged("CurrentCertFromDate");
            RaisePropertyChanged("CurrentCertThruDate");
            RaisePropertyChanged("FirstCert");
            RaisePropertyChanged("FirstCertThruDate");
            RaisePropertyChanged("FirstCertFromDate");
            RaisePropertyChanged("CurrentCert");
            RaisePropertyChanged("CertificationPeriodsShortLabel");
            RaisePropertyChanged("HideCertPeriods");
            RaisePropertyChanged("CurrentCertCycleTodayPhrase");
            RaisePropertyChanged("MostRecentPOCCertCyclePhrase");
        }

        public void RefreshAdmissionGroupChanged()
        {
            RaisePropertyChanged("AdmissionGroupSets");
            RaisePropertyChanged("CurrentGroup");
            RaisePropertyChanged("CurrentGroup2");
            RaisePropertyChanged("CurrentGroup3");
            RaisePropertyChanged("CurrentGroup4");
            RaisePropertyChanged("CurrentGroup5");

            //Must call this to re-evaluate the CanXXX properties - e.g. CanEditSOC
            Messenger.Default.Send(AdmissionKey, Constants.DomainEvents.AdmissionGroupModified);
        }

        partial void OnAdmissionStatusChanged()
        {
            _AdmissionStatusCode = null;
            if (IgnoreChanged)
            {
                return;
            }

            RaisePropertyChanged("AdmissionStatusCode");
            RaisePropertyChanged("AdmissionStatusText");
            RaisePropertyChanged("PreEvalRequiredEnabled");
            RaisePropertyChanged("CanEdit");
            if (AdmissionStatusCode == "N" && NotTakenDateTime == null)
            {
                NotTakenDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            }
        }

        partial void OnDischargeReasonKeyChanged()
        {
            _DischargeReasonCode = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DischargeReasonCode");
            RaisePropertyChanged("DischargeReasonCodeDescription");
            RaisePropertyChangedEventsCustom();
        }

        partial void OnReferDateTimeChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            RaisePropertyChanged("AdmissionStatusText");
        }

        partial void OnInitialReferralDateChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            RaisePropertyChanged("AdmissionStatusText");
            Messenger.Default.Send(this, "OasisSetupAdmissionDefaults");
        }

        partial void OnPhysicianOrderedSOCDateChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            Messenger.Default.Send(this, "OasisSetupAdmissionDefaults");
            RaisePropertyChanged("PhysicianMismatch");
        }

        partial void OnPatientInsuranceKeyChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            _insurance = null;
            _pi = null;
            RaisePropertyChanged("FaceToFaceRequired");

            RaisePropertyChanged("CertificationPeriodsLabel");
            RaisePropertyChanged("CertificationPeriodsShortLabel");
            RaisePropertyChanged("ShowCertEditWidgetsPlusMask");
            RaisePropertyChanged("ShowCertEditWidgetsOppositePlusMask");
            RaisePropertyChanged("CurrentCertCycleTodayPhrase");
            RaisePropertyChanged("MostRecentPOCCertCyclePhrase");
            RaisePropertyChanged("FirstCertPeriodNumberEnabled");
            RaisePropertyChanged("FirstCertFromDateEnabled");
            RaisePropertyChanged("FirstCertThruDateEnabled");
            RaisePropertyChanged("HideCertPeriods");
            RaisePropertyChanged("CanModifyCertCycle");
            RaisePropertyChanged("CanModifyCertCycleMaint");
            RaisePropertyChanged("CanModifyCertPeriodMaint");
            RaisePropertyChanged("FirstCert");
            RaisePropertyChanged("FirstCertPeriodNumber");
            RaisePropertyChanged("FirstCertThruDate");
            RaisePropertyChanged("FirstCertFromDate");

            if (PatientInsuranceKey.HasValue)
            {
                if (Patient != null)
                {
                    var pi = Patient.PatientInsurance.Where(ins => ins.PatientInsuranceKey == PatientInsuranceKey)
                        .FirstOrDefault();
                    if (FirstCert != null && pi != null && pi.InsuranceKey != null)
                    {
                        FirstCert.InsuranceKey = (int)pi.InsuranceKey;
                    }
                }
            }

            SetupCertCycles();
        }

        partial void OnFaceToFaceEncounterChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("FaceToFaceEncounterCode");
            RaisePropertyChanged("CanEdit");
            RaisePropertyChanged("FaceToFaceRequired");
        }

        partial void OnSOCDateChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            SetupCertCycles();
            RaisePropertyChanged("PhysicianMismatch");
            Messenger.Default.Send(this, "OasisSetupAdmissionDefaults");

            //for notification purposes, treat SOCDdate change same as AdmissionGroup change
            Messenger.Default.Send(AdmissionKey, Constants.DomainEvents.AdmissionGroupModified);

            RaisePropertyChanged("CurrentCertCycleTodayPhrase");
            RaisePropertyChanged("MostRecentPOCCertCyclePhrase");
            RaisePropertyChanged("FirstCertFromDate");
            RaisePropertyChanged("FirstCertThruDate");
            RaisePropertyChanged("FirstCertThruDateEnabled");
            RaisePropertyChanged("FirstCertFromDateEnabled");
            RaisePropertyChanged("FirstCertPeriodNumberEnabled");
        }

        partial void OnDischargeDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("AdmissionStatusCode");
            RaisePropertyChanged("AdmissionStatusText");
            RaisePropertyChangedEventsCustom();
        }

        partial void OnFacilityKeyChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            RaisePropertyChanged("Facility1Label");
            RaisePropertyChanged("Facility2Label");
            RaisePropertyChanged("Facility3Label");
            Messenger.Default.Send(this, "OasisSetupAdmissionDefaults");
        }

        partial void OnHospiceAdmissionChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            UseHospiceBilling = HospiceAdmission;
            RaisePropertyChanged("HideCertPeriods");
            RaisePropertyChanged("CertificationPeriodsLabel");
            RaisePropertyChanged("CertificationPeriodsShortLabel");
            RaisePropertyChanged("ShowCertEditWidgetsPlusMask");
            RaisePropertyChanged("ShowCertEditWidgetsOppositePlusMask");
            RaisePropertyChanged("CurrentCertCycleTodayPhrase");
            RaisePropertyChanged("MostRecentPOCCertCyclePhrase");
            RaisePropertyChanged("FirstCertPeriodNumberEnabled");
            RaisePropertyChanged("FirstCertFromDateEnabled");
            RaisePropertyChanged("FirstCertThruDateEnabled");

            RaisePropertyChanged("ShowLevelOfCare");
            RaisePropertyChanged("ShowHIS");
            RaisePropertyChanged("ShowOASIS");
            RaisePropertyChanged("ShowCMS");
            RaisePropertyChanged("CMSTabHeaderLabel");
            RaisePropertyChanged("ShowPreEvalRequired");
            RaisePropertyChanged("PreEvalRequiredEnabled");
            RaisePropertyChanged("UseHospiceBilling");
            CalculateHospiceBenefitPeriod();
        }

        private void CalculateHospiceBenefitPeriod()
        {
            if (!HospiceAdmission)
            {
                return;
            }

            // If only Hospice Admission checked, default the Hospice Benefit Period Number and Dates
            if (!TransferHospice && !HospiceBenefitReelection)
            {
                CurrentCertPeriodNumber = 1;
            }
        }


        partial void OnIgnoreSOCMismatchChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            RaisePropertyChanged("PhysicianMismatch");
            RaisePropertyChanged("IsSignatureEnabled");
        }

        public void Refresh()
        {
            RaisePropertyChanged("GroupedDisciplines");
            RaisePropertyChanged("AdmissionStatusText");
        }

        private bool HasVitals(Encounter encounter)
        {
            if (encounter.EncounterBP.Any())
            {
                return true;
            }

            if (encounter.EncounterCBG.Any())
            {
                return true;
            }

            if (encounter.EncounterPulse.Any())
            {
                return true;
            }

            if (encounter.EncounterResp.Any())
            {
                return true;
            }

            if (encounter.EncounterTemp.Any())
            {
                return true;
            }

            if (encounter.EncounterSpo2.Any())
            {
                return true;
            }

            if (encounter.EncounterPTINR.Any())
            {
                return true;
            }

            if (encounter.EncounterWeight.Any())
            {
                return true;
            }

            return false;
        }

        public string GetVitalsTextForEncounter(Encounter mostRecentEncounter)
        {
            string text = null;
            if (mostRecentEncounter != null)
            {
                var CR = char.ToString('\r');
                var eb = mostRecentEncounter.EncounterBP
                    .OrderByDescending(e => e.GetReadingDateTime(mostRecentEncounter.EncounterIsVisitTeleMonitoring))
                    .FirstOrDefault();
                if (eb != null)
                {
                    if (text != null)
                    {
                        text = text + CR;
                    }

                    text = text + "Blood Pressure is  " + eb.ThumbNail;
                }

                var ec = mostRecentEncounter.EncounterCBG.OrderByDescending(e => e.CBGDateTime).FirstOrDefault();
                if (ec != null)
                {
                    if (text != null)
                    {
                        text = text + CR;
                    }

                    text = text + "Blood Sugar is  " + ec.ThumbNail;
                }

                var ep = mostRecentEncounter.EncounterPulse
                    .OrderByDescending(e => e.GetReadingDateTime(mostRecentEncounter.EncounterIsVisitTeleMonitoring))
                    .FirstOrDefault();
                if (ep != null)
                {
                    if (text != null)
                    {
                        text = text + CR;
                    }

                    text = text + "Pulse is  " + ep.ThumbNail;
                }

                var er = mostRecentEncounter.EncounterResp
                    .OrderByDescending(e => e.GetReadingDateTime(mostRecentEncounter.EncounterIsVisitTeleMonitoring))
                    .FirstOrDefault();
                if (er != null)
                {
                    if (text != null)
                    {
                        text = text + CR;
                    }

                    text = text + "Respiration is  " + er.ThumbNail;
                }

                var et = mostRecentEncounter.EncounterTemp
                    .OrderByDescending(e => e.GetReadingDateTime(mostRecentEncounter.EncounterIsVisitTeleMonitoring))
                    .FirstOrDefault();
                if (et != null)
                {
                    if (text != null)
                    {
                        text = text + CR;
                    }

                    text = text + "Temperature is  " + et.ThumbNail;
                }

                var es = mostRecentEncounter.EncounterSpo2
                    .OrderByDescending(e => e.GetReadingDateTime(mostRecentEncounter.EncounterIsVisitTeleMonitoring))
                    .FirstOrDefault();
                if (es != null)
                {
                    if (text != null)
                    {
                        text = text + CR;
                    }

                    text = text + "Pulse Oximetry is  " + es.ThumbNail;
                }

                var ew = mostRecentEncounter.EncounterWeight.OrderByDescending(e => e.WeightDateTime).FirstOrDefault();
                if (ew != null)
                {
                    if (text != null)
                    {
                        text = text + CR;
                    }

                    text = text + "Weight is  " + ew.WeightThumbNail;

                    if (ew.HasBMI)
                    {
                        text = text + "   BMI is  " + ew.BMIThumbNail;
                    }

                    if (ew.HasBSA)
                    {
                        text = text + "   BSA is  " + ew.BSAThumbNail;
                    }
                }

                var ept = mostRecentEncounter.EncounterPTINR.OrderByDescending(e => e.PTINRDateTime).FirstOrDefault();
                if (ept != null)
                {
                    if (text != null)
                    {
                        text = text + CR;
                    }

                    text = text + "PT/INR is  " + ept.ThumbNail;
                }
            }

            return text == null ? "None" : text;
        }

        public void AdmissionReferralSetMostRecent()
        {
            if (AdmissionReferral == null)
            {
                return;
            }

            foreach (var ar in AdmissionReferral) ar.MostRecent = false;
            ;
            var mostRecent = AdmissionReferral.Where(referral => referral.HistoryKey == null)
                .OrderByDescending(ar => ar.ReferralDate).FirstOrDefault();
            if (mostRecent != null)
            {
                mostRecent.MostRecent = true;
            }
        }

        private bool StartDateLessThanOrEqualToAdmissionGroupDate(DateTime admissionGroupDate, DateTime? startDate)
        {
            if (admissionGroupDate == null || admissionGroupDate == DateTime.MinValue)
            {
                return false;
            }

            if (startDate == null)
            {
                return true;
            }

            return ((DateTime)startDate).Date <= admissionGroupDate.Date;
        }

        private bool EndDateGreaterThanOrEqualToAdmissionGroupDate(DateTime admissionGroupDate, DateTime? endDate)
        {
            if (admissionGroupDate == null || admissionGroupDate == DateTime.MinValue)
            {
                return false;
            }

            if (endDate == null)
            {
                return true;
            }

            return ((DateTime)endDate).Date >= admissionGroupDate.Date;
        }

        public AdmissionGroup GetNthCurrentGroup(int GroupToRetrieve, DateTime admissionGroupDate)
        {
            AdmissionGroup ag = null;
            if ((admissionGroupDate == null || admissionGroupDate == DateTime.MinValue) == false)
            {
                ag = AdmissionGroup.Where(ag1 =>
                        ag1.GroupHeaderSequence == GroupToRetrieve &&
                        StartDateLessThanOrEqualToAdmissionGroupDate(admissionGroupDate, ag1.StartDate) &&
                        EndDateGreaterThanOrEqualToAdmissionGroupDate(admissionGroupDate, ag1.EndDate))
                    .OrderByDescending(p => p.StartDate).ThenBy(k => k.AdmissionGroupKey).FirstOrDefault();
            }

            // fall back to the only one we can find.
            if (ag == null && (admissionGroupDate == null || admissionGroupDate == DateTime.MinValue))
            {
                ag = AdmissionGroup.Where(ag2 => ag2.GroupHeaderSequence == GroupToRetrieve)
                    .OrderByDescending(p => p.StartDate).ThenBy(k => k.AdmissionGroupKey)
                    .FirstOrDefault();
            }

            return ag;
        }


        private void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsReadOnly)
            {
                return;
            }

            if (IsEditting && (e.PropertyName.Equals("ReferralSourceKey") ||
                               e.PropertyName.Equals("ServiceLineGroupingKey") ||
                               e.PropertyName.Equals("ReferralDate")))
            {
                RefreshRaiseChanged();
            }
        }

        private void CleanupGroupedDisciplines()
        {
            if (__GroupedDisciplines != null) //cleanup prior __GroupedDisciplines;
            {
                foreach (var ldg in __GroupedDisciplines) ldg.Cleanup();
                __GroupedDisciplines.Clear();
                __GroupedDisciplines = null;
            }
        }

        public void Resequence(Encounter encounter)
        {
            if (AdmissionDiagnosis == null)
            {
                return;
            }

            if (AdmissionDiagnosis.Where(a => a.IsNew || a.HasResequenceChanges).Any() == false)
            {
                return;
            }

            _myEncounter = encounter;
            // Resequence 9s and 10s seperately
            // Medical followed by Surgical - with surgicals in StartDate order
            if (AdmissionDiagnosis.Where(a => (a.IsNew || a.HasResequenceChanges) && a.Version == 9).Any())
            {
                myDiagnosis = true;
                myVersion = 9;
                _FilteredItemsSource = new CollectionViewSource();
                _FilteredItemsSource.Source = AdmissionDiagnosis;
                FilteredItemsSource.SortDescriptions.Clear();
                FilteredItemsSource.SortDescriptions.Add(new SortDescription("DiagnosisStatus", ListSortDirection.Ascending));
                FilteredItemsSource.SortDescriptions.Add(new SortDescription("Sequence", ListSortDirection.Ascending));
                FilteredItemsSource.Filter = FilterItems;
                FilteredItemsSource.Refresh();
                var sequence = 1;
                foreach (AdmissionDiagnosis ad in FilteredItemsSource) ad.Sequence = sequence++;
                myDiagnosis = false;
                myVersion = 9;
                _FilteredItemsSource = new CollectionViewSource();
                _FilteredItemsSource.Source = AdmissionDiagnosis;
                FilteredItemsSource.SortDescriptions.Clear();
                FilteredItemsSource.SortDescriptions.Add(new SortDescription("DiagnosisStartDate", ListSortDirection.Ascending));
                FilteredItemsSource.SortDescriptions.Add(new SortDescription("Sequence", ListSortDirection.Ascending));
                FilteredItemsSource.Filter = FilterItems;
                FilteredItemsSource.Refresh();
                foreach (AdmissionDiagnosis ad in FilteredItemsSource) ad.Sequence = sequence++;
            }

            if (AdmissionDiagnosis.Where(a => (a.IsNew || a.HasResequenceChanges) && a.Version == 10).Any())
            {
                myDiagnosis = true;
                myVersion = 10;
                _FilteredItemsSource = new CollectionViewSource();
                _FilteredItemsSource.Source = AdmissionDiagnosis;
                FilteredItemsSource.SortDescriptions.Clear();
                FilteredItemsSource.SortDescriptions.Add(new SortDescription("DiagnosisStatus", ListSortDirection.Ascending));
                FilteredItemsSource.SortDescriptions.Add(new SortDescription("Sequence", ListSortDirection.Ascending));
                FilteredItemsSource.Filter = FilterItems;
                FilteredItemsSource.Refresh();
                var sequence = 1;
                foreach (AdmissionDiagnosis ad in FilteredItemsSource) ad.Sequence = sequence++;
                myDiagnosis = false;
                myVersion = 10;
                _FilteredItemsSource = new CollectionViewSource();
                _FilteredItemsSource.Source = AdmissionDiagnosis;
                FilteredItemsSource.SortDescriptions.Clear();
                FilteredItemsSource.SortDescriptions.Add(new SortDescription("DiagnosisStartDate", ListSortDirection.Ascending));
                FilteredItemsSource.SortDescriptions.Add(new SortDescription("Sequence", ListSortDirection.Ascending));
                FilteredItemsSource.Filter = FilterItems;
                FilteredItemsSource.Refresh();
                foreach (AdmissionDiagnosis ad in FilteredItemsSource) ad.Sequence = sequence++;
            }
        }

        private bool FilterItems(object item)
        {
            var ad = item as AdmissionDiagnosis;
            if (ad == null)
            {
                return false;
            }

            if (ad.Version != myVersion)
            {
                return false;
            }

            if (ad.Diagnosis != myDiagnosis)
            {
                return false;
            }

            ad.CurrentEncounter = _myEncounter;
            // Ignore superceded rows in Admission Maintenance
            if (_myEncounter == null && ad.Superceded)
            {
                return false;
            }

            // If we have an Encounter and the item is not new, only include the item if it is in this encounter
            if (_myEncounter != null && !ad.IsNew)
            {
                var ed = _myEncounter.EncounterDiagnosis.Where(e => e.DiagnosisKey == ad.AdmissionDiagnosisKey)
                    .FirstOrDefault();
                if (ed == null)
                {
                    return false;
                }
            }

            return true;
        }

        public List<DisciplineInGoalElement> ActiveAdmissionDisciplinesInGoalElementsDisciplines(
            List<DisciplineInGoalElement> dgeList)
        {
            var adList = ActiveAdmissionDisciplines;
            if (dgeList == null || adList == null)
            {
                return null;
            }

            var ageFilteredList = new List<DisciplineInGoalElement>();
            foreach (var dge in dgeList)
                if (adList.Where(p => p.DisciplineKey == dge.DisciplineKey).Any())
                {
                    ageFilteredList.Add(dge);
                }

            return ageFilteredList.Any() == false ? null : ageFilteredList;
        }
        // to avoid triage of data after its fetched from the server, use nullable bool? and triage at first use

        partial void OnNotTakenDateTimeChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            if (AdmissionStatusCode == "N" || NotTaken)
            {
                if (NotTakenDateTime == null)
                {
                    _notTakenDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                    Deployment.Current.Dispatcher.BeginInvoke(() => { RaisePropertyChanged("NotTakenDateTime"); });
                }
            }

            if (NotTakenDateTime != null)
            {
                AdmissionDiscipline
                    .Where(p => p.ForceNotTaken || !p.NotTakenDateTime.HasValue && !p.DischargeDateTime.HasValue)
                    .ForEach(ad =>
                    {
                        ad.BeginEditting();
                        ad.NotTakenDateTime = NotTakenDateTime;
                        ad.ForceNotTaken = true;
                    });
            }

            RaisePropertyChanged("AdmissionStatusText");
        }

        partial void OnNotTakenReasonChanged()
        {
            if (IgnoreChanged)
            {
                return;
            }

            AdmissionDiscipline.Where(p => p.NotTakenDateTime.HasValue && p.ForceNotTaken).ForEach(ad =>
            {
                ad.NotTakenReason = NotTakenReason;
            });
        }

        public void ResetStatusProperties()
        {
            _Admitted = null;
            _NotTaken = null;
            RaisePropertyChanged("Admitted");
            RaisePropertyChanged("NotTaken");
        }

        private string ConvertDateTime(DateTime? dt)
        {
            // Use with UTC tiem fields only
            if (dt == null)
            {
                return "?";
            }

            var ddt = dt.GetValueOrDefault();
            return ddt.ToLocalTime().ToString("MM/dd/yyyy") + " " + ddt.ToLocalTime().ToShortTimeString();
        }

        private string ConvertDate(DateTime? dt)
        {
            // Use with UTC time fields only.
            if (dt == null)
            {
                return "?";
            }

            var ddt = dt.GetValueOrDefault();
            return ddt.ToLocalTime().ToString("MM/dd/yyyy");
        }

        public void SetProviderFacility(DateTime? dateForHeader = null, bool ShowErrorMessages = false)
        {
            var DateToUse = dateForHeader.HasValue ? dateForHeader.Value : DateTime.Today;

            if (FacilityKey == null || FacilityKey <= 0)
            {
                var slg = GetFirstServiceLineGroupWithOasisHeader(DateToUse);
                if (slg == null && ShowErrorMessages)
                {
                    MessageBox.Show(
                        "There is no Service Line Grouping defined for this admission/encounter with a controlling CMS Header,  Provider information will not be valued.");
                    return;
                }

                var oh = slg == null ? null : OasisHeaderCache.GetOasisHeaderFromKey(slg.OasisHeaderKey);
                if (oh == null && ShowErrorMessages)
                {
                    MessageBox.Show(
                        "There is no Service Line Grouping defined for this admission/encounter with a controlling CMS Header,  Provider information will not be valued.");
                    return;
                }

                if (oh != null)
                {
                    _providerFacility = oh;
                }
            }
            else
            {
                _providerFacility = FacilityCache.GetFacilityFromKey(FacilityKey);
            }
        }

        public List<OrderEntry> GetOrderEntryM1510R1(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                o.IsM1510R1 && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM1510R5(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                o.IsM1510R5 && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM1510(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                (o.IsM1510R1 || o.IsM1510R5) && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2004(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                o.IsM2004 && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2300N(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                o.IsM2300N && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2300Y(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                o.IsM2300Y && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2300(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                (o.IsM2300N || o.IsM2300Y) && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2400a(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                o.IsM2400a && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2400b(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                o.IsM2400b && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2400c(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                o.IsM2400c && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2400d(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                o.IsM2400d && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2400e(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                o.IsM2400e && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2400f1(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                o.IsM2400f1 && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2400f2(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                o.IsM2400f2 && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2400f(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                (o.IsM2400f1 || o.IsM2400f2) && o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null &&
                o.CompletedDate != null && o.CompletedDate >= ((DateTime)startDate).Date &&
                ((DateTimeOffset)o.CompletedDate).Date <= _endDate).OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<OrderEntry> GetOrderEntryM2400(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today.Date : ((DateTime)endDate).Date;
            if (OrderEntry == null)
            {
                return null;
            }

            if (OrderEntry.Any() == false)
            {
                return null;
            }

            var oe = OrderEntry.Where(o =>
                    (o.IsM2400a || o.IsM2400b || o.IsM2400c || o.IsM2400d || o.IsM2400e || o.IsM2400f1 ||
                     o.IsM2400f2) &&
                    o.OrderStatus != (int)OrderStatusType.Voided && o.HistoryKey == null && o.CompletedDate != null &&
                    o.CompletedDate >= ((DateTime)startDate).Date && ((DateTimeOffset)o.CompletedDate).Date <= _endDate)
                .OrderByDescending(o => o.CompletedDate).ToList();
            if (oe == null)
            {
                return null;
            }

            if (oe.Any() == false)
            {
                return null;
            }

            return oe;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM1510R1(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege => ege.IsM1510R1 && ege.AdmissionGoalElement != null)
                    .ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM1510R3(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege => ege.IsM1510R3 && ege.AdmissionGoalElement != null)
                    .ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM1510R4(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege => ege.IsM1510R4 && ege.AdmissionGoalElement != null)
                    .ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM1510(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege =>
                    (ege.IsM1510R1 || ege.IsM1510R3 || ege.IsM1510R4) && ege.AdmissionGoalElement != null).ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM2015(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege => ege.IsM2015 && ege.AdmissionGoalElement != null)
                    .ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM2400a(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege => ege.IsM2400a && ege.AdmissionGoalElement != null)
                    .ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM2400b(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege => ege.IsM2400b && ege.AdmissionGoalElement != null)
                    .ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM2400c(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege => ege.IsM2400c && ege.AdmissionGoalElement != null)
                    .ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM2400d(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege => ege.IsM2400d && ege.AdmissionGoalElement != null)
                    .ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM2400e(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege => ege.IsM2400e && ege.AdmissionGoalElement != null)
                    .ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM2400f1(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege => ege.IsM2400f1 && ege.AdmissionGoalElement != null)
                    .ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM2400f2(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege => ege.IsM2400f2 && ege.AdmissionGoalElement != null)
                    .ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM2400f(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement
                    .Where(ege => (ege.IsM2400f1 || ege.IsM2400f2) && ege.AdmissionGoalElement != null).ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterGoalElement> GetEncounterGoalElementM2400(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterGoalElement>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var egeList = e.EncounterGoalElement.Where(ege =>
                    (ege.IsM2400a || ege.IsM2400b || ege.IsM2400c || ege.IsM2400d || ege.IsM2400e || ege.IsM2400f1 ||
                     ege.IsM2400f2) && ege.AdmissionGoalElement != null).ToList();
                if (egeList != null)
                {
                    foreach (var ege in egeList)
                        list.Add(ege);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public Encounter GetFirstEncounterPainLocationOfAdmission(DateTime? startDate, DateTime? endDate)
        {
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            var eListAscending = eList.OrderBy(e => e.EncounterOrTaskStartDateAndTime).ToList();
            foreach (var e in eListAscending)
                if (e.EncounterPainLocation.Where(ews => ews.AdmissionPainLocation.DeletedDate == null).Any())
                {
                    return e;
                }

            return null;
        }

        public EncounterPain GetFirstEncounterPainOfAdmission(DateTime? startDate, DateTime? endDate)
        {
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            var eListAscending = eList.OrderBy(e => e.EncounterOrTaskStartDateAndTime).ToList();
            foreach (var e in eListAscending)
            {
                var p = e.EncounterPain.Where(ep => string.IsNullOrWhiteSpace(ep.PainScore) == false).FirstOrDefault();
                if (p != null)
                {
                    return p;
                }
            }

            return null;
        }

        public List<EncounterData> GetEncounterDataByLabel(string label, DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            var questionkey = DynamicFormCache.GetQuestionKeyByLabel(label);
            foreach (var e in eList)
            {
                var edList = e.EncounterData.Where(ed => ed.QuestionKey == questionkey).ToList();
                if (edList != null)
                {
                    foreach (var ed in edList)
                        list.Add(ed);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterData> GetEncounterDataByLabelAndLookupType(string label, string lookupType,
            DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            var questionkey = DynamicFormCache.GetQuestionKeyByLabelAndLookupType(label, lookupType);
            foreach (var e in eList)
            {
                var edList = e.EncounterData.Where(ed => ed.QuestionKey == questionkey).ToList();
                if (edList != null)
                {
                    foreach (var ed in edList)
                        list.Add(ed);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterData> GetEncounterDataPatientCPR(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            var questionKey =
                DynamicFormCache.GetQuestionKeyByLabelAndLookupType("Patient CPR", "HospicePrefPatientCPR");
            foreach (var e in eList)
            {
                var edList = e.EncounterData.Where(ed => ed.IsPatientCPR(questionKey)).ToList();
                if (edList != null)
                {
                    foreach (var ed in edList)
                        list.Add(ed);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterData> GetEncounterDataPatientLST(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            var questionKey =
                DynamicFormCache.GetQuestionKeyByLabelAndLookupType("Patient LST", "HospicePrefPatientLST");
            foreach (var e in eList)
            {
                var edList = e.EncounterData.Where(ed => ed.IsPatientLST(questionKey)).ToList();
                if (edList != null)
                {
                    foreach (var ed in edList)
                        list.Add(ed);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterData> GetEncounterDataPatientHOSP(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            var questionKey =
                DynamicFormCache.GetQuestionKeyByLabelAndLookupType("Patient HOSP", "HospicePrefPatientHOSP");
            foreach (var e in eList)
            {
                var edList = e.EncounterData.Where(ed => ed.IsPatientHOSP(questionKey)).ToList();
                if (edList != null)
                {
                    foreach (var ed in edList)
                        list.Add(ed);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterData> GetEncounterDataDiscussSpiritualPatient(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            var questionKey =
                DynamicFormCache.GetQuestionKeyByLabelAndLookupType("DiscussSpiritPreference Patient",
                    "DiscussSpiritPreference");
            foreach (var e in eList)
            {
                var edList = e.EncounterData.Where(ed => ed.IsDiscussSpiritualPatient(questionKey)).ToList();
                if (edList != null)
                {
                    foreach (var ed in edList)
                        list.Add(ed);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterData> GetEncounterDataDiscussSpiritualCaregiver(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            var questionKey =
                DynamicFormCache.GetQuestionKeyByLabelAndLookupType("DiscussSpiritPreference Primary Caregiver",
                    "DiscussSpiritPreference");
            foreach (var e in eList)
            {
                var edList = e.EncounterData.Where(ed => ed.IsDiscussSpiritualCaregiver(questionKey)).ToList();
                if (edList != null)
                {
                    foreach (var ed in edList)
                        list.Add(ed);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterData> GetEncounterDataDyspnea(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            var questionKey = DynamicFormCache.GetQuestionKeyByLabel("Dyspnea");
            foreach (var e in eList)
            {
                var edList = e.EncounterData.Where(ed => ed.IsDyspnea(questionKey)).ToList();
                if (edList != null)
                {
                    foreach (var ed in edList)
                        list.Add(ed);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterData> GetEncounterDataDyspneaNone(DateTime? startDate, DateTime? endDate)
        {
            var edList = GetEncounterDataDyspnea(startDate, endDate);
            if (edList == null)
            {
                return null;
            }

            var edListNone = edList.Where(e =>
                e.TextData.ToLower().StartsWith("no dyspnea") || e.TextData.ToLower().Equals("none")).ToList();
            if (edListNone == null)
            {
                return null;
            }

            if (edListNone.Any() == false)
            {
                return null;
            }

            return edListNone;
        }

        public List<EncounterData> GetEncounterDataEdema(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            var questionKey = DynamicFormCache.GetQuestionKeyByLabel("Edema");
            foreach (var e in eList)
            {
                var edList = e.EncounterData.Where(ed => ed.IsEdema(questionKey)).ToList();
                if (edList != null)
                {
                    foreach (var ed in edList)
                        list.Add(ed);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterData> GetEncounterDataEdemaNone(DateTime? startDate, DateTime? endDate)
        {
            var edList = GetEncounterDataEdema(startDate, endDate);
            if (edList == null)
            {
                return null;
            }

            var edListNone = edList.Where(e =>
                e.IntDataCodeDescription.ToLower().Equals("none") ||
                e.IntDataCodeDescription.ToLower().StartsWith("no edema")).ToList();
            if (edListNone == null)
            {
                return null;
            }

            if (edListNone.Any() == false)
            {
                return null;
            }

            return edListNone;
        }

        public List<EncounterData> GetEncounterDataReceivedFrom(EncounterData currentEncounterData, DateTime? startDate,
            DateTime? endDate)
        {
            var list = new List<EncounterData>();
            if (currentEncounterData != null)
            {
                list.Add(currentEncounterData);
            }

            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                var questionKey = DynamicFormCache.GetQuestionKeyByLabel("Admitted or Received From");
                foreach (var e in eList)
                {
                    var edList = e.EncounterData.Where(ed => ed.IsReceivedFrom(questionKey)).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            list.Add(ed);
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterBP> GetEncounterBP(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterBP>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var edList = e.EncounterBP.Where(ed => ed.BPSystolic != 0 && ed.BPDiastolic != 0).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            list.Add(ed);
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        public List<EncounterCBG> GetEncounterCBG(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterCBG>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var edList = e.EncounterCBG.Where(ed => ed.CBG != 0).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            list.Add(ed);
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        public List<EncounterPulse> GetEncounterPulse(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterPulse>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var edList = e.EncounterPulse.Where(ed => ed.PulseRate != 0).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            list.Add(ed);
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        public List<EncounterResp> GetEncounterResp(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterResp>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var edList = e.EncounterResp.Where(ed => ed.RespRate != 0).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            list.Add(ed);
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        public List<EncounterSpo2> GetEncounterSpo2(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterSpo2>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var edList = e.EncounterSpo2.Where(ed => ed.Spo2Percent != 0).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            list.Add(ed);
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        public List<EncounterWeight> GetEncounterWeight(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterWeight>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var edList = e.EncounterWeight.Where(ed => ed.WeightValue.HasValue && ed.WeightValue > 0)
                        .OrderBy(o => o.WeightDateTime).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            list.Add(ed);
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        public List<EncounterTemp> GetEncounterTemp(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterTemp>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var edList = e.EncounterTemp.Where(ed => ed.Temp.HasValue && ed.Temp.Value != 0.0).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            list.Add(ed);
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        public List<WoundMeasurementHistoryItem> GetWoundMeasurementHistory(DateTime? startDate, DateTime? endDate,
            AdmissionWoundSite admissionWoundSite, Encounter currentEncounter)
        {
            var hList = new List<WoundMeasurementHistoryItem>();
            if (admissionWoundSite == null)
            {
                return hList;
            }

            var awsList = new List<AdmissionWoundSite>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return hList;
            }

            eList.Reverse();
            foreach (var e in eList)
            {
                var ews = e.EncounterWoundSite.Where(ed =>
                    ed.AdmissionWoundSite.Number == admissionWoundSite.Number &&
                    ed.AdmissionWoundSite.IsMeasurementValid).FirstOrDefault();
                if (ews != null)
                {
                    var aws = AdmissionWoundSite.Where(a => a.AdmissionWoundSiteKey == ews.AdmissionWoundSiteKey)
                        .FirstOrDefault();
                    if (aws != null && awsList.Where(a => a.AdmissionWoundSiteKey == ews.AdmissionWoundSiteKey)
                            .FirstOrDefault() == null)
                    {
                        awsList.Add(aws);
                        hList.Add(new WoundMeasurementHistoryItem
                        {
                            MeasurementDate = e.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date,
                            Length = aws.Length,
                            Width = aws.Width,
                            Depth = aws.Depth
                        });
                    }
                }
            }

            // add the admissionWoundSite passed if need be (against the current encounter)
            if (awsList.Where(a => a == admissionWoundSite).FirstOrDefault() == null && admissionWoundSite.IsNew &&
                admissionWoundSite.IsMeasurementValid)
            {
                hList.Add(new WoundMeasurementHistoryItem
                {
                    MeasurementDate = currentEncounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date,
                    Length = admissionWoundSite.Length,
                    Width = admissionWoundSite.Width,
                    Depth = admissionWoundSite.Depth
                });
            }

            hList = hList.OrderByDescending(h => h.MeasurementDate).ToList();
            return hList;
        }

        public List<EncounterWoundSite> GetWoundPushScored(DateTime? startDate, DateTime? endDate, int woundNumber)
        {
            var list = new List<EncounterWoundSite>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                eList.Reverse();
                foreach (var e in eList)
                {
                    var edList = e.EncounterWoundSite.Where(ed =>
                            ed.AdmissionWoundSite.Number == woundNumber && ed.AdmissionWoundSite.PushScore != null)
                        .ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                        {
                            var ews = list.Where(ew => ew.AdmissionWoundSiteKey == ed.AdmissionWoundSiteKey)
                                .FirstOrDefault();
                            if (ews == null)
                            {
                                list.Add(ed);
                            }
                        }
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public List<EncounterPain> GetEncounterPainScored(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterPain>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var edList = e.EncounterPain.Where(ed => ed.PainScoreInt != null).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            list.Add(ed);
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        private List<EncounterWeight> GetEncounterWeight(DateTime? startDate, DateTime? endDate, bool RequireBMI,
            bool RequireBSA)
        {
            var list = new List<EncounterWeight>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var edList = e.EncounterWeight.Where(ed =>
                            ed.WeightValue.HasValue && ed.WeightValue > 0 && (!RequireBMI || ed.HeightValue.HasValue))
                        .OrderBy(o => o.WeightDateTime.HasValue ? o.WeightDateTime : o.Encounter.EncounterDateTime)
                        .ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            list.Add(ed);
                    }
                }
            }

            // add most recent weight taken before our interval - much more complicated now that dates can be in two places...
            eList = GetEncounters(DateTime.MinValue, startDate);
            if (eList != null)
            {
                var oldList = new List<Tuple<DateTime, EncounterWeight>>();
                foreach (var e in eList)
                {
                    var edList = e.EncounterWeight
                        .Where(ed =>
                            ed.WeightValue.HasValue && ed.WeightValue > 0 && (!RequireBMI || ed.HeightValue.HasValue))
                        .Select(s => new Tuple<DateTime, EncounterWeight>
                        (s.WeightDateTime.HasValue ? s.WeightDateTime.Value : e.EncounterDateTime,
                            s))
                        .ToList();
                    foreach (var ed in edList) oldList.Add(ed);
                }

                if (oldList.Any())
                {
                    var last = oldList.OrderByDescending(o => o.Item1).Select(s => s.Item2).FirstOrDefault();
                    list.Add(last);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        public List<EncounterWeight> GetEncounterDataWeight(DateTime? startDate, DateTime? endDate)
        {
            return GetEncounterWeight(startDate, endDate, false, false);
        }

        public string GetMostRecentWeightInPounds(DateTime? date, int pastNdays)
        {
            var endDate = date == null ? DateTime.Today.Date : ((DateTime)date).Date;
            var startDate = endDate.AddDays(pastNdays * -1);
            var ewList = new List<EncounterWeight>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var edList = e.EncounterWeight.Where(ed => ed.WeightValue.HasValue && ed.WeightValue > 0)
                        .OrderBy(o => o.WeightDateTime.HasValue ? o.WeightDateTime : o.Encounter.EncounterDateTime)
                        .ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            ewList.Add(ed);
                    }
                }
            }

            if (ewList == null || ewList.Any() == false)
            {
                return null;
            }

            ewList.Reverse();
            var ew = ewList.Where(e => string.IsNullOrWhiteSpace(e.WeightInPounds) == false).FirstOrDefault();
            if (ew == null)
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(ew.WeightInPounds) ? null : ew.WeightInPounds;
        }

        public List<EncounterData> GetEncounterDataIntegerLabel(string label, DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            var questionKey = DynamicFormCache.GetQuestionKeyByLabel(label);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var edList = e.EncounterData.Where(ed => ed.IsIntegerLabel(questionKey) && ed.IntData != null)
                        .ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            list.Add(ed);
                    }
                }
            }

            // add most recent weight taken before our interval
            eList = GetEncounters(DateTime.MinValue, startDate);
            if (eList != null)
            {
                var mostRecentFound = false;
                foreach (var e in eList)
                {
                    var edList = e.EncounterData.Where(ed => ed.IsIntegerLabel(questionKey) && ed.IntData != null)
                        .ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                        {
                            list.Add(ed);
                            mostRecentFound = true;
                            break;
                        }
                    }

                    if (mostRecentFound)
                    {
                        break;
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        public EncounterWeight GetEncounterDataMostRecentWeight(bool BSARequired)
        {
            var eList = GetEncounters(DateTime.MinValue, DateTime.MaxValue);
            if (eList != null)
            {
                var oldList = new List<Tuple<DateTime, EncounterWeight>>();
                foreach (var e in eList)
                {
                    var edList = e.EncounterWeight
                        .Where(ed => ed.WeightValue != null && ed.WeightValue > 0 && (!BSARequired || ed.HasBSA))
                        .Select(s => new Tuple<DateTime, EncounterWeight>
                        (s.WeightDateTime.HasValue ? s.WeightDateTime.Value : e.EncounterDateTime,
                            s))
                        .ToList();
                    foreach (var ed in edList) oldList.Add(ed);
                }

                if (oldList.Any())
                {
                    var last = oldList.OrderByDescending(o => o.Item1).Select(s => s.Item2).FirstOrDefault();
                    return last;
                }
            }

            return null;
        }

        public double GetEncounterDataMostRecentBSA()
        {
            var eList = GetEncounters(DateTime.MinValue, DateTime.MaxValue);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var ed = e.EncounterWeight.Where(ew => ew.HasBSA).OrderByDescending(o => o.WeightDateTime)
                        .FirstOrDefault();
                    if (ed != null)
                    {
                        return ed.BSAValue.Value;
                    }
                }
            }

            return 0;
        }

        public List<EncounterData> GetEncounterDataTypeRealForQuestionLabel(DateTime? startDate, DateTime? endDate,
            string questionLabel)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            var questionkey = DynamicFormCache.GetQuestionKeyByLabel(questionLabel);
            if (eList != null)
            {
                foreach (var e in eList)
                {
                    var edList = e.EncounterData.Where(ed => ed.QuestionKey == questionkey).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            if (ed.RealData != null)
                            {
                                list.Add(ed);
                            }
                    }
                }
            }

            // add most recent RealData taken before our interval
            eList = GetEncounters(DateTime.MinValue, startDate);
            if (eList != null)
            {
                var mostRecentFound = false;
                foreach (var e in eList)
                {
                    var edList = e.EncounterData.Where(ed => ed.QuestionKey == questionkey).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                        {
                            if (ed.RealData != null)
                            {
                                list.Add(ed);
                                mostRecentFound = true;
                            }

                            break;
                        }
                    }

                    if (mostRecentFound)
                    {
                        break;
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        public List<EncounterWeight> GetEncounterDataBMI(DateTime? startDate, DateTime? endDate)
        {
            return GetEncounterWeight(startDate, endDate, true, false);
        }

        public List<EncounterWeight> GetEncounterDataBSA(DateTime? startDate, DateTime? endDate)
        {
            return GetEncounterWeight(startDate, endDate, true, true);
        }

        public List<EncounterData> GetLeftArmCircumferenceData(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                var questionkey = DynamicFormCache.GetQuestionKeyByLabel("Mid Arm Circumference");
                foreach (var e in eList)
                {
                    var edList = e.EncounterData.Where(ed => ed.QuestionKey == questionkey).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            if (string.IsNullOrWhiteSpace(ed.TextData) == false)
                            {
                                list.Add(ed);
                            }
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        public List<EncounterData> GetRightArmCircumferenceData(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterData>();
            var eList = GetEncounters(startDate, endDate);
            if (eList != null)
            {
                var questionkey = DynamicFormCache.GetQuestionKeyByLabel("Mid Arm Circumference");
                foreach (var e in eList)
                {
                    var edList = e.EncounterData.Where(ed => ed.QuestionKey == questionkey).ToList();
                    if (edList != null)
                    {
                        foreach (var ed in edList)
                            if (string.IsNullOrWhiteSpace(ed.Text2Data) == false)
                            {
                                list.Add(ed);
                            }
                    }
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            list.Reverse();
            return list;
        }

        public EncounterData GetEncounterBilateralAmputee(DateTime? startDate, DateTime? endDate)
        {
            var _startDate = startDate == null ? DateTime.MinValue : (DateTime)startDate;
            var eList = GetEncounters(_startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            var questionkey = DynamicFormCache.GetQuestionKeyByLabel("Lower Amputation");
            foreach (var e in eList)
            {
                var edba = e.EncounterData.Where(ed => ed.IsBilateralAmputee(questionkey)).FirstOrDefault();
                if (edba != null)
                {
                    return edba;
                }
            }

            return null;
        }

        public List<EncounterPain> GetEncounterPain(DateTime? startDate, DateTime? endDate)
        {
            var list = new List<EncounterPain>();
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var epList = e.EncounterPain.Where(ed => ed.IsPain).ToList();
                if (epList != null)
                {
                    foreach (var ep in epList)
                        list.Add(ep);
                }
            }

            if (list.Any() == false)
            {
                return null;
            }

            return list;
        }

        public EncounterRisk GetRiskAssessmentM2400b(DateTime? startDate, DateTime? endDate)
        {
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var er = e.EncounterRisk.Where(ere => ere.IsM2400b).FirstOrDefault();
                if (er != null)
                {
                    return er;
                }
            }

            return null;
        }

        public EncounterRisk GetRiskAssessmentM2400c(DateTime? startDate, DateTime? endDate)
        {
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var er = e.EncounterRisk.Where(ere => ere.IsM2400c).FirstOrDefault();
                if (er != null)
                {
                    return er;
                }
            }

            return null;
        }

        public EncounterRisk GetRiskAssessmentM2400e(DateTime? startDate, DateTime? endDate)
        {
            var eList = GetEncounters(startDate, endDate);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList)
            {
                var er = e.EncounterRisk.Where(ere => ere.IsM2400e).FirstOrDefault();
                if (er != null)
                {
                    return er;
                }
            }

            return null;
        }

        public string GetPOC60DayVisitSummaryHistory(AdmissionCertification AdmissionCertification)
        {
            string visitSummaryHistory = null;
            var useMilitaryTime = TenantSettingsCache.Current.TenantSetting.UseMilitaryTime;
            var eList = GetEncountersInclusive(AdmissionCertification.PeriodStartDate,
                AdmissionCertification.PeriodEndDate);
            if (eList == null)
            {
                return null;
            }

            // Note - there are two flavors of a 'Clinical Visit Summary'
            var questionkey1 = DynamicFormCache.GetQuestionKeyByLabelAndDataTemplate("clinical visit summary", "text");
            var questionkey2 =
                DynamicFormCache.GetQuestionKeyByLabelAndDataTemplate("clinical visit summary", "textwithinfoicon");
            foreach (var e in eList)
            {
                var edvs = e.EncounterData
                    .Where(ed => ed.IsClincialVisitSummary(questionkey1) || ed.IsClincialVisitSummary(questionkey2))
                    .FirstOrDefault();
                if (edvs != null)
                {
                    if (visitSummaryHistory != null)
                    {
                        visitSummaryHistory = visitSummaryHistory + "<LineBreak/><LineBreak />";
                    }

                    var userData = XamlHelper.EncodeAsXaml(edvs.TextData);
                    visitSummaryHistory = visitSummaryHistory +
                                          string.Format("<Bold>        {0} on {1} {2}</Bold><LineBreak />{3}",
                                              ServiceTypeCache.GetServiceTypeFromKey((int)e.ServiceTypeKey) == null
                                                  ? "Unknown service type"
                                                  : ServiceTypeCache.GetServiceTypeFromKey((int)e.ServiceTypeKey)
                                                      .Description,
                                              e.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date
                                                  .ToString("MM/dd/yyyy"),
                                              useMilitaryTime
                                                  ? e.EncounterOrTaskStartDateAndTime.GetValueOrDefault()
                                                      .ToString("HHmm")
                                                  : e.EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime
                                                      .ToShortTimeString(),
                                              userData);
                }
            }

            return visitSummaryHistory == null ? "No visit summaries are on file." : visitSummaryHistory;
        }

        public string GetDisciplineSynopsisHistory(DateTime FromDate, DateTime ThruDate, int? DisciplineKeyParm,
            Encounter EncounterParm)
        {
            string visitSummaryHistory = null;

            var eList = GetEncountersInclusive(FromDate, ThruDate);
            if (eList == null)
            {
                return "No discipline synopses are on file.";
            }

            foreach (var e in eList.Where(e => e.EncounterSynopsis != null && e.DisciplineKey == DisciplineKeyParm
                                                                           // don't include yourself, or those things that were entered after yourself.
                                                                           && EncounterParm != null &&
                                                                           e.EncounterKey !=
                                                                           EncounterParm.EncounterKey &&
                                                                           e.EncounterOrTaskStartDateAndTime != null
                                                                           && ((DateTimeOffset)e.EncounterOrTaskStartDateAndTime).Date <= ThruDate.Date
                                                                           && ((DateTimeOffset)e.EncounterOrTaskStartDateAndTime).Date >= FromDate.Date
                                                                           && e.Form != null && !e.Form.IsTeamMeeting)
                         .ToList())
            {
                var edvs = e.EncounterSynopsis.FirstOrDefault();
                if (edvs != null && !string.IsNullOrEmpty(edvs.SynopsisText))
                {
                    var user = e.EncounterBy != null && e.EncounterBy != Guid.Empty
                        ? UserCache.Current.GetFullNameFromUserId(e.EncounterBy)
                        : "UnKnown";

                    if (visitSummaryHistory != null)
                    {
                        visitSummaryHistory = visitSummaryHistory + "<LineBreak/><LineBreak />";
                    }

                    visitSummaryHistory = visitSummaryHistory +
                                          string.Format("<Bold>By {0} on {1}</Bold><LineBreak />{2}",
                                              user,
                                              e.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date
                                                  .ToString("MM/dd/yyyy"),
                                              XamlHelper.EncodeAsXaml(edvs.SynopsisText));
                }
            }

            return visitSummaryHistory == null ? "No discipline synopses are on file." : visitSummaryHistory;
        }

        public string GetDisciplineAgnosticSynopsisHistory(DateTime FromDate, DateTime ThruDate,
            Encounter EncounterParm)
        {
            string visitSummaryHistory = null;

            var eList = GetEncountersInclusive(FromDate, ThruDate);
            if (eList == null)
            {
                return "No synopses are on file.";
            }

            foreach (var e in eList.Where(e =>
                         e.EncounterSynopsis != null && e.EncounterStatus == (int)EncounterStatusType.Completed
                                                     // don't include yourself, or those things that were entered after yourself.
                                                     && EncounterParm != null &&
                                                     e.EncounterKey != EncounterParm.EncounterKey &&
                                                     e.EncounterOrTaskStartDateAndTime != null
                                                     && ((DateTimeOffset)e.EncounterOrTaskStartDateAndTime).Date <= ThruDate.Date
                                                     && ((DateTimeOffset)e.EncounterOrTaskStartDateAndTime).Date >= FromDate.Date
                                                     && e.Form != null && !e.Form.IsTeamMeeting).ToList())
            {
                var edvs = e.EncounterSynopsis.FirstOrDefault();
                if (edvs != null && !string.IsNullOrEmpty(edvs.SynopsisText) && edvs.AdmissionTeamMeetingKey == null)
                {
                    var user = e.EncounterBy != null && e.EncounterBy != Guid.Empty
                        ? UserCache.Current.GetFullNameFromUserId(e.EncounterBy)
                        : "UnKnown";
                    var date = "";
                    try
                    {
                        date = e.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date.ToString("MM/dd/yyyy");
                    }
                    catch
                    {
                    }

                    var time = "";
                    try
                    {
                        if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                        {
                            time = ((DateTimeOffset)e.EncounterOrTaskStartDateAndTime).DateTime.ToString("HHmm");
                        }
                        else
                        {
                            time = ((DateTimeOffset)e.EncounterOrTaskStartDateAndTime).DateTime.ToShortTimeString();
                        }
                    }
                    catch
                    {
                    }

                    if (visitSummaryHistory != null)
                    {
                        visitSummaryHistory = visitSummaryHistory + "<LineBreak/><LineBreak />";
                    }

                    visitSummaryHistory = visitSummaryHistory +
                                          string.Format("<Bold>By {0} on {1} {2} {3}</Bold><LineBreak />{4}",
                                              user, date, time,
                                              e.DisciplineCode == null ? "" : " for " + e.DisciplineCode,
                                              XamlHelper.EncodeAsXaml(edvs.SynopsisText));
                }
            }

            return visitSummaryHistory == null ? "No synopses are on file." : visitSummaryHistory;
        }

        public List<SynopsisItem> GetTeamMeetingSynopsisHistoryList(DateTime FromDate, DateTime ThruDate,
            AdmissionTeamMeeting AdmissionTeamMeeting, bool AssignAdmissionTeamMeeting)
        {
            var admissionTeamMeetingKey = AdmissionTeamMeeting == null || AdmissionTeamMeeting.AdmissionTeamKey <= 0
                ? (int?)null
                : AdmissionTeamMeeting.AdmissionTeamKey;
            var siList = new List<SynopsisItem>();
            ;

            var eList = GetEncountersInclusive(FromDate, ThruDate, true);
            if (eList == null)
            {
                return null;
            }

            foreach (var e in eList.Where(e => e.EncounterSynopsis != null
                                               && ((DateTimeOffset)e.EncounterOrTaskStartDateAndTime).Date <= ThruDate.Date
                                               && ((DateTimeOffset)e.EncounterOrTaskStartDateAndTime).Date >= FromDate.Date
                                               && e.Form != null && !e.Form.IsTeamMeeting).ToList())
            {
                var es = e.EncounterSynopsis.FirstOrDefault();
                if (es != null && !string.IsNullOrEmpty(es.SynopsisText) &&
                    (AssignAdmissionTeamMeeting == false && es.AdmissionTeamMeetingKey == admissionTeamMeetingKey ||
                     AssignAdmissionTeamMeeting && (es.AdmissionTeamMeetingKey == admissionTeamMeetingKey ||
                                                    es.AdmissionTeamMeetingKey == null ||
                                                    es.AdmissionTeamMeetingKey <= 0)))
                {
                    var user = e.EncounterBy != null && e.EncounterBy != Guid.Empty
                        ? UserCache.Current.GetFullNameFromUserId(e.EncounterBy)
                        : "UnKnown";
                    var date = "";
                    try
                    {
                        date = e.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date.ToString("MM/dd/yyyy");
                    }
                    catch
                    {
                    }

                    var time = "";
                    try
                    {
                        if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                        {
                            time = ((DateTimeOffset)e.EncounterOrTaskStartDateAndTime).DateTime.ToString("HHmm");
                        }
                        else
                        {
                            time = ((DateTimeOffset)e.EncounterOrTaskStartDateAndTime).DateTime.ToShortTimeString();
                        }
                    }
                    catch
                    {
                    }

                    if (AssignAdmissionTeamMeeting && AdmissionTeamMeeting != null &&
                        AdmissionTeamMeeting.EncounterSynopsis != null &&
                        AdmissionTeamMeeting.EncounterSynopsis
                            .Where(s => s.EncounterSynopsisKey == es.EncounterSynopsisKey).Any() ==
                        false)
                    {
                        AdmissionTeamMeeting.EncounterSynopsis.Add(es);
                    }

                    ;
                    var synopsisText = string.Format("<Bold>By {0} on {1} {2} {3}</Bold><LineBreak />{4}",
                        user, date, time,
                        e.DisciplineCode == null ? "" : " for " + e.DisciplineCode,
                        XamlHelper.EncodeAsXaml(es.SynopsisText));
                    siList.Add(new SynopsisItem { SynopsisText = synopsisText });
                }
            }

            return siList == null || siList.Any() == false ? null : siList;
        }

        public string GetTeamMeetingSynopsisHistory(DateTime FromDate, DateTime ThruDate,
            AdmissionTeamMeeting AdmissionTeamMeeting, bool AssignAdmissionTeamMeeting)
        {
            var siList =
                GetTeamMeetingSynopsisHistoryList(FromDate, ThruDate, AdmissionTeamMeeting, AssignAdmissionTeamMeeting);
            if (siList == null)
            {
                return "No synopses are on file.";
            }

            string history = null;
            foreach (var si in siList)
                history = history + (history == null ? "" : "<LineBreak/><LineBreak />") + si.SynopsisText;
            return history;
        }

        public List<Encounter> GetEncounters(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today : (DateTime)endDate;
            if (Encounter == null)
            {
                return null;
            }

            if (Encounter.Any() == false)
            {
                return null;
            }

            var eList = Encounter
                .Where(e => e.HistoryKey == null &&
                            e.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date >
                            startDate.GetValueOrDefault().Date &&
                            e.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date <= _endDate.Date)
                .OrderByDescending(e => e.EncounterOrTaskStartDateAndTime).ToList();
            if (eList == null)
            {
                return null;
            }

            if (eList.Any() == false)
            {
                return null;
            }

            return eList;
        }

        public List<Encounter> GetEvalAndVisitEncountersInclusive(DateTime? startDate, DateTime? endDate,
            bool onlyCompleted = false)
        {
            var eList = GetEncountersInclusive(startDate, endDate, onlyCompleted);
            if (eList == null)
            {
                return null;
            }

            eList = eList.Where(e =>
                    (e.EncounterIsEval || e.EncounterIsVisit) && e.EncounterStatus != (int)EncounterStatusType.None)
                .ToList();
            if (eList == null)
            {
                return null;
            }

            if (onlyCompleted)
            {
                eList = eList.Where(e => e.EncounterStatus == (int)EncounterStatusType.Completed).ToList();
            }

            if (eList == null)
            {
                return null;
            }

            if (eList.Any() == false)
            {
                return null;
            }

            return eList;
        }

        public List<Encounter> GetEncountersInclusive(DateTime? startDate, DateTime? endDate,
            bool onlyCompleted = false)
        {
            if (startDate == null)
            {
                return null;
            }

            var _endDate = endDate == null ? DateTime.Today : (DateTime)endDate;
            if (Encounter == null)
            {
                return null;
            }

            if (Encounter.Any() == false)
            {
                return null;
            }

            var eList = Encounter
                .Where(e => e.Inactive == false && e.HistoryKey == null &&
                            e.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date >= startDate &&
                            e.EncounterStartDate.GetValueOrDefault().Date <= _endDate.Date)
                .OrderByDescending(e => e.EncounterOrTaskStartDateAndTime).ToList();
            if (eList == null)
            {
                return null;
            }

            if (onlyCompleted)
            {
                eList = eList.Where(e => e.EncounterStatus == (int)EncounterStatusType.Completed).ToList();
            }

            if (eList == null)
            {
                return null;
            }

            if (eList.Any() == false)
            {
                return null;
            }

            return eList;
        }

        public void RaisePropertyChangedHasFirstCert()
        {
            RaisePropertyChanged("HasFirstCert");
        }

        public bool IsAdmisionDisciplineFrequencyInCurrentCert(AdmissionDisciplineFrequency adf)
        {
            if (adf == null)
            {
                return false;
            }

            if (CurrentCert == null)
            {
                return true;
            }

            if (CurrentCertFromDate != null && adf.EndDate != null)
            {
                if (((DateTime)CurrentCertFromDate).Date > ((DateTime)adf.EndDate).Date)
                {
                    return false;
                }
            }

            if (CurrentCertThruDate != null && adf.StartDate != null)
            {
                if (((DateTime)CurrentCertThruDate).Date < ((DateTime)adf.StartDate).Date)
                {
                    return false;
                }
            }

            return true;
        }

        public AdmissionCertification GetAdmissionCertForDate(DateTime? dateParm, bool DefaultToLast = true)
        {
            AdmissionCertification acReturn = null;
            if (AdmissionCertification != null && dateParm != null)
            {
                acReturn = AdmissionCertification
                    .Where(ac => (ac.PeriodEndDate == null
                                  || ((DateTime)dateParm).Date <= ac.PeriodEndDate.Value.Date
                                 )
                                 && (ac.PeriodStartDate == null
                                     || ((DateTime)dateParm).Date >= ac.PeriodStartDate.Value.Date
                                 )
                    ).FirstOrDefault();
            }

            if (AdmissionCertification != null && acReturn == null && DefaultToLast)
            {
                if (AdmissionCertification.Any())
                {
                    acReturn = AdmissionCertification.OrderBy(ac => ac.PeriodStartDate).Last();
                }
            }

            return acReturn;
        }

        public AdmissionCertification GetAdmissionCertificationByPeriodNumber(int periodNumber)
        {
            if (AdmissionCertification == null)
            {
                return null;
            }

            return AdmissionCertification.Where(ac => ac.PeriodNumber == periodNumber).FirstOrDefault();
        }

        public void SetupCertCycles(bool RaiseEvent = true)
        {
            if (!IsEditting && !InDynamicForm)
            {
                return; // accept the values coming from the db if we aren't going to edit it.
            }

            if (AdmissionCertification != null && AdmissionCertification.Any() && !TransferHospice &&
                !HospiceBenefitReelection)
            {
                CertManager.AdjustCertCycles(this);
                return;
            }

            if (AdmissionCertification != null && AdmissionCertification.Any() &&
                (TransferHospice || HospiceBenefitReelection))
            {
                CertManager.SetThroughDateForHospice(this);
                return;
            }

            if (HospiceAdmission)
            {
                SetupCertCyclesForHospice(InDynamicForm);
            }
            else
            {
                var ac = CertManager.GetOrCreateCertPeriodForDate(this, DateTime.Now);

                if (AdmissionCertification.Any())
                {
                    ac = AdmissionCertification.FirstOrDefault();
                    if (CurrentCertPeriodNumber == null)
                    {
                        CurrentCertPeriodNumber = ac.PeriodNumber;
                        RaisePropertyChanged("CurrentCertPeriodNumber");
                    }
                }
            }

            RaisePropertyChanged("PeriodEndDate");
            RaisePropertyChanged("CurrentCertPeriodNumber");
            RaisePropertyChanged("CurrentCertFromDate");
            RaisePropertyChanged("CurrentCertThruDate");
            RaisePropertyChanged("FirstCert");
            RaisePropertyChanged("FirstCertThruDate");
            RaisePropertyChanged("FirstCertFromDate");
            RaisePropertyChanged("FirstCertPeriodNumber");

            // Force the RaiseCanExecuteChange to fire
            RaisePropertyChanged("CurrentCert");
        }

        private void SetupCertCyclesForHospice(bool inDynamicForm)
        {
            if (!HospiceAdmission)
            {
                return;
            }

            var date = FirstCertFromDate.HasValue ? FirstCertFromDate.Value : DateTime.Today;

            if (TransferHospice)
            {
                var ac = CertManager.GetOrCreateCertPeriodForDate(this, date);
            }
            else if (HospiceBenefitReelection && inDynamicForm == false)
            {
            }
            else if (HospiceBenefitReelection && inDynamicForm)
            {
                var ac = CertManager.GetOrCreateCertPeriodForDate(this, date);
            }
            else
            {
                // just a normal hospice admission, set period number = 1, from date = admit date
                var ac = CertManager.GetOrCreateCertPeriodForDate(this, DateTime.Now);
            }
        }

        public void DischargeAllDisciplines(DateTime DischargeDate, int? DischargeReason, string SummaryOfCareNarrative)
        {
            var dischargeStatus = (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "D");
            var notTakeStatus = (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "N");
            foreach (var adc in AdmissionDiscipline)
                if (!adc.DischargeDateTime.HasValue && !adc.NotTaken)
                {
                    if (adc.DisciplineAdmitDateTime.HasValue)
                    {
                        adc.AdmissionStatus = dischargeStatus;
                        adc.DischargeDateTime = DischargeDate;
                        adc.DischargeReasonKey = DischargeReason;
                        adc.SummaryOfCareNarrative = SummaryOfCareNarrative;
                        adc.NotTakenDateTime = null;
                        adc.NotTakenReason = null;
                    }
                    else
                    {
                        adc.AdmissionStatus = notTakeStatus;
                        adc.DischargeDateTime = null;
                        adc.DischargeReasonKey = null;
                        adc.SummaryOfCareNarrative = null;
                        adc.NotTakenDateTime = DischargeDate;
                        adc.NotTakenReason = "Transferred Admission";
                    }
                }
        }

        public void EndDateAllFCDOrdersForDiscipline(int? disciplineKey, DateTime endDate, bool endDateAll)
        {
            if (AdmissionDiscipline == null || AdmissionDisciplineFrequency == null)
            {
                return;
            }

            AdmissionDiscipline ad = null;
            DateTime? adfEndDate = null;
            foreach (var adf in AdmissionDisciplineFrequency.Where(a =>
                         a.StartDate != null && a.Inactive == false && a.Superceded == false &&
                         (a.DisciplineKey == disciplineKey || endDateAll)))
            {
                adfEndDate = null;
                if (disciplineKey != adf.DisciplineKey && disciplineKey != null)
                {
                    // Note - we assume the disciplines are already set as discharged or notTaken in dynamic form processing prior to this call
                    // Pecking order for end date calculation for disciplines other than the one passed:
                    //   use the greatest AdmissionDiscipline.DischargeDateTime associated with this discipline,
                    //   if its null - use the greatest AdmissionDiscipline.NotTakenDateTime, associated with this discipline,
                    ad = AdmissionDiscipline
                        .Where(a => a.DisciplineKey == adf.DisciplineKey && a.DischargeDateTime != null)
                        .OrderByDescending(a => a.DischargeDateTime).FirstOrDefault();
                    if (ad != null)
                    {
                        adfEndDate = ad.DischargeDateTime;
                    }

                    ad = AdmissionDiscipline
                        .Where(a => a.DisciplineKey == adf.DisciplineKey && a.NotTakenDateTime != null)
                        .OrderByDescending(a => a.NotTakenDateTime).FirstOrDefault();
                    if (ad != null && adfEndDate == null)
                    {
                        adfEndDate = ad.NotTakenDateTime;
                    }
                    //   if its null - use the enddate passed  
                }

                if (adfEndDate == null)
                {
                    adfEndDate = endDate;
                }

                adfEndDate = ((DateTime)adfEndDate).Date;
                // only stamp the FCD end date it if is currently null - or greater than the calculated end date
                if (adf.EndDate == null || adf.EndDate != null && ((DateTime)adf.EndDate).Date > adfEndDate)
                {
                    if (((DateTime)adf.StartDate).Date <= adfEndDate)
                    {
                        adf.EndDate = adfEndDate; // Discontinue active FCDs
                    }

                    if (((DateTime)adf.StartDate).Date > adfEndDate)
                    {
                        adf.EndDate = ((DateTime)adf.StartDate).Date; // EndDate future FCDs
                    }

                    var newadf = adf.CreateNewVersion();
                    AdmissionDisciplineFrequency.Add(newadf);
                    if (((DateTime)adf.StartDate).Date > adfEndDate)
                    {
                        // stamp Future FCDs as inactive
                        newadf.BeginEditting();
                        newadf.Inactive = true;
                        newadf.InactiveDate = DateTime.Now;
                        newadf.EndEditting();
                    }
                }
            }
        }

        public bool ValidationErrorsContainsMessage(string msg)
        {
            foreach (var ve in ValidationErrors)
                if (ve.ErrorMessage == msg)
                {
                    return true;
                }

            return false;
        }

        // Put the validation on the Admission partial class so all areas of the app have access to it.
        public bool ValidateAdmissionPartial()
        {
            // place other calls to validations here if necessary. (i.e. ValidateFCD, ValidCoverage, etc...);
            var empValid = ValidateEmploymentRelated();
            return ValidateAdmissionGroups() && empValid;
        }

        public bool ValidateTrauma()
        {
            var AllValid = true;
            var Msg = "";
            if (HasTrauma.GetValueOrDefault() && TraumaDate.HasValue == false)
            {
                Msg = "Trauma Date is required when have trauma.";
                ValidationErrors.Add(new ValidationResult(Msg, new[] { "TraumaDate" }));
                AllValid = false;
            }

            var haveValidValue = TraumaStateCode.HasValue && TraumaStateCode.Value > 0;
            if (HasTrauma.GetValueOrDefault() && haveValidValue == false)
            {
                Msg = "Trauma State Code is required when have trauma.";
                ValidationErrors.Add(new ValidationResult(Msg, new[] { "TraumaStateCode" }));
                AllValid = false;
            }

            haveValidValue = TraumaType.HasValue && TraumaType.Value > 0;
            if (HasTrauma.GetValueOrDefault() && haveValidValue == false)
            {
                Msg = "Trauma Type is required when have trauma.";
                ValidationErrors.Add(new ValidationResult(Msg, new[] { "TraumaType" }));
                AllValid = false;
            }

            return AllValid;
        }

        public bool ValidateAdmissionGroupSets()
        {
            return ValidateAdmissionGroups() && !ValidateAdmissionGroupExistance(true);
        }

        public bool ValidateAdmissionGroups()
        {
            var allValid = true;
            // clear previous errors
            foreach (var ag in AdmissionGroup) ag.ValidationErrors.Clear();
            var lst = ValidationErrors.Where(v => v.MemberNames.Contains("ServiceLineGroupingKey")).ToList();
            lst.ForEach(er => ValidationErrors.Remove(er));

            foreach (var ag in AdmissionGroup)
                if (!ag.Validate())
                {
                    allValid = false;
                }

            if (AdmissionGroup == null)
            {
                return false;
            }

            allValid = allValid && ValidateAdmissionGroupOverlap();

            // User must not have any ServiceLines/Groupings assigned.
            if (SelectedServiceLine == null)
            {
                string[] memberNames = { "ServiceLineKey", "ServiceLineGroupingKey" };
                var Msg = string.Format("{0} doesn't have any service line permissions.",
                    UserCache.Current.GetCurrentUserProfile().DisplayName);
                ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                allValid = false;
                return allValid; // bail or the below check will crash.
            }

            // Now check for missing rows
            var missing = SelectedServiceLine.ServiceLineGroupHeader.Where(h =>
                !AdmissionGroup.Where(ag => ag.GroupHeaderKey == h.ServiceLineGroupHeaderKey).Any()).ToList();
            foreach (var CurGroup in missing)
            {
                var BindingString = string.Format("Group{0}BindingKey", CurGroup.SequenceNumber + 1);
                string[] memberNames =
                    { "StartDate", "EndDate", "ServiceLineGroupingKey" /*, "ServiceLineKey"*/, BindingString };
                var Msg = string.Format("{0} is required.", CurGroup.GroupHeaderLabel);
                ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                allValid = false;
            }

            return allValid;
        }

        public bool ValidateAdmissionGroupOverlap()
        {
            var allValid = true;
            if (AdmissionGroup == null)
            {
                return true;
            }

            var BindingString = "none";

            string[] memberNames =
            {
                "StartDate", "EndDate", "ServiceLineGroupingKey", "GroupHeaderKey" /*, "ServiceLineKey"*/, BindingString
            };

            // Assure that there are no gaps between the End and Start dates of different 
            if (AdmissionGroupSets.Count > 1)
            {
                for (var i = 0; i < AdmissionGroupSets.Count - 1; i++)
                {
                    var rowEnd = DateTime.MinValue;
                    var nextRowStart = DateTime.MinValue;

                    var curRow = AdmissionGroupSets[i];
                    var nextRow = AdmissionGroupSets[i + 1];

                    if (curRow != null && curRow.EndDate != null)
                    {
                        rowEnd = (DateTime)curRow.EndDate;
                    }

                    if (nextRow != null && nextRow.StartDate != null)
                    {
                        nextRowStart = (DateTime)nextRow.StartDate;
                    }

                    if (rowEnd.AddDays(1) != nextRowStart)
                    {
                        // there is a gap
                        var Msg = "Service Line Grouping without break in dates is required.";
                        curRow.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                        nextRow.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                        allValid = false;
                    }
                }
            }

            // Assure that the last end date is null
            if (AdmissionGroupSets != null && AdmissionGroupSets.Any())
            {
                var lastRow = AdmissionGroupSets[AdmissionGroupSets.Count - 1];
                if (lastRow != null && lastRow.EndDate != null)
                {
                    var Msg = "Service Line Grouping without \"End\" date is required.";
                    lastRow.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    allValid = false;
                }
            }

            foreach (var CurGroup in AdmissionGroup)
            {
                BindingString = string.Format("Group{0}BindingKey", CurGroup.GroupHeaderSequence + 1);
                if (AdmissionGroup.Any(df => df.AdmissionGroupKey != CurGroup.AdmissionGroupKey &&
                                             df.GroupHeaderKey == CurGroup.GroupHeaderKey
                                             // All non null dates
                                             && (CurGroup.StartDate <= df.EndDate && CurGroup.EndDate >= df.StartDate
                                                 // row passed in has null thru date
                                                 || CurGroup.EndDate == null && df.EndDate >= CurGroup.StartDate
                                                 // row passed in has non null
                                                 || df.EndDate == null && CurGroup.EndDate >= df.StartDate
                                                 // both have non null thru dates
                                                 || df.EndDate == null && CurGroup.EndDate == null
                                             )))
                {
                    var MsgPlural = CurGroup.GroupingHeaderName.Substring(CurGroup.GroupingHeaderName.Length - 1);
                    if (MsgPlural == "s" || MsgPlural == "S")
                    {
                        MsgPlural = "";
                    }
                    else
                    {
                        MsgPlural = "s";
                    }

                    var Msg = string.Format("{0}{1} Must not overlap.", CurGroup.GroupingHeaderName, MsgPlural);
                    CurGroup.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    allValid = false;
                }

                if (CurGroup.StartDate > CurGroup.EndDate)
                {
                    var Msg = string.Format("{0} Start Date must be on or before {0} End Date.", CurGroup.GroupingName,
                        CurGroup.GroupingName);
                    CurGroup.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    allValid = false;
                }

                if (AdmissionGroup.Any(df =>
                        df.AdmissionGroupKey != CurGroup.AdmissionGroupKey &&
                        df.GroupHeaderKey == CurGroup.GroupHeaderKey && CurGroup.EndDate == df.EndDate &&
                        CurGroup.EndDate >= df.StartDate))
                {
                    var MsgPlural = CurGroup.GroupingHeaderName.Substring(CurGroup.GroupingHeaderName.Length - 1);
                    if (MsgPlural == "s" || MsgPlural == "S")
                    {
                        MsgPlural = "";
                    }
                    else
                    {
                        MsgPlural = "s";
                    }

                    var Msg = string.Format("{0}{1} Must not overlap.", CurGroup.GroupingHeaderName, MsgPlural);
                    CurGroup.ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    allValid = false;
                }
            }


            return allValid;
        }

        public bool ValidateAdmissionGroupExistance(bool fromMaint = false)
        {
            var AnyErrors = false;

            if (CurrentGroup == null && ServiceLineGrouping1 != null)
            {
                if (fromMaint && CurrentGroup != null && CurrentGroup.StartDate != null)
                {
                    AdmissionGroupDate = (DateTime)CurrentGroup.StartDate;
                }

                if (!ValidationErrorsContainsMessage(string.Format("{0} is required.",
                        ServiceLineGrouping1.GroupHeaderLabel)))
                {
                    ValidationErrors.Add(
                        new ValidationResult(
                            string.Format("{0} is required", ServiceLineGrouping1.GroupHeaderLabel),
                            new[] { "ServiceLineGroupingKey", "Group1BindingKey" }));
                    AnyErrors = true;
                }
            }

            if (CurrentGroup2 == null && ServiceLineGrouping2 != null)
            {
                if (fromMaint && CurrentGroup2 != null && CurrentGroup2.StartDate != null)
                {
                    AdmissionGroupDate = (DateTime)CurrentGroup2.StartDate;
                }

                if (!ValidationErrorsContainsMessage(string.Format("{0} is required.",
                        ServiceLineGrouping2.GroupHeaderLabel)))
                {
                    ValidationErrors.Add(
                        new ValidationResult(
                            string.Format("{0} is required", ServiceLineGrouping2.GroupHeaderLabel),
                            new[] { "ServiceLineGroupingKey", "Group2BindingKey" }));
                    AnyErrors = true;
                }
            }

            if (CurrentGroup3 == null && ServiceLineGrouping3 != null)
            {
                if (fromMaint && CurrentGroup3 != null && CurrentGroup3.StartDate != null)
                {
                    AdmissionGroupDate = (DateTime)CurrentGroup3.StartDate;
                }

                if (!ValidationErrorsContainsMessage(string.Format("{0} is required.",
                        ServiceLineGrouping3.GroupHeaderLabel)))
                {
                    ValidationErrors.Add(
                        new ValidationResult(
                            string.Format("{0} is required", ServiceLineGrouping3.GroupHeaderLabel),
                            new[] { "ServiceLineGroupingKey", "Group3BindingKey" }));
                    AnyErrors = true;
                }
            }

            if (CurrentGroup4 == null && ServiceLineGrouping4 != null)
            {
                if (fromMaint && CurrentGroup4 != null && CurrentGroup4.StartDate != null)
                {
                    AdmissionGroupDate = (DateTime)CurrentGroup4.StartDate;
                }

                if (!ValidationErrorsContainsMessage(string.Format("{0} is required.",
                        ServiceLineGrouping4.GroupHeaderLabel)))
                {
                    ValidationErrors.Add(
                        new ValidationResult(
                            string.Format("{0} is required", ServiceLineGrouping4.GroupHeaderLabel),
                            new[] { "ServiceLineGroupingKey", "Group4BindingKey" }));
                    AnyErrors = true;
                }
            }

            if (CurrentGroup5 == null && ServiceLineGrouping5 != null)
            {
                if (fromMaint && CurrentGroup5 != null && CurrentGroup5.StartDate != null)
                {
                    AdmissionGroupDate = (DateTime)CurrentGroup5.StartDate;
                }

                if (!ValidationErrorsContainsMessage(string.Format("{0} is required.",
                        ServiceLineGrouping5.GroupHeaderLabel)))
                {
                    ValidationErrors.Add(
                        new ValidationResult(
                            string.Format("{0} is required", ServiceLineGrouping5.GroupHeaderLabel),
                            new[] { "ServiceLineGroupingKey", "Group5BindingKey" }));
                    AnyErrors = true;
                }
            }

            return AnyErrors;
        }

        public bool ValidateSOCDate()
        {
            if (OriginalAdmissionRow == null)
            {
                OriginalAdmissionRow = (Admission)GetOriginal();
            }

            var orig = OriginalAdmissionRow;
            var AllValid = true;

            string[] discCodes = { "A", "B", "C", "D" };
            string[] memberNames = { "SOCDate" };
            if (orig != null && SOCDate == null && orig.SOCDate != null)
            {
                var Msg = string.Format("The " + StartOfCareDateLabel + " cannot be removed.");
                ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                AllValid = false;
            }

            // Only perform this validation if we are in a Hospice Service line and/or the Service line requires Oasis. (US 20343)
            var sl = ServiceLine != null ? ServiceLine : ServiceLineCache.GetServiceLineFromKey(ServiceLineKey);
            if (sl != null && (sl.IsHospiceServiceLine || sl.OasisServiceLine))
            {
                // This validation should not occur on a 'Transfer' admission - caurni00 - tfs 29318
                var transferStatusKey = CodeLookupCache.GetKeyFromCode("AdmissionStatus", "T");
                // if a Discipline with the above hcfa codes doesn't exist with an admit date on the SOC, report that to the user.
                if (AdmissionStatus != transferStatusKey && !ReferDateIsBeforeGoLive && AdmissionDiscipline != null &&
                    SOCDate != null && !AdmissionDiscipline.Any(ad => ad.DisciplineAdmitDateTime.HasValue
                                                                      && ad.DisciplineAdmitDateTime.Value.Date ==
                                                                      SOCDate.Value.Date &&
                                                                      discCodes.Contains(DisciplineCache
                                                                          .GetDisciplineFromKey(ad.DisciplineKey)
                                                                          .HCFACode)))
                {
                    var Msg = string.Format("There are no Nursing or Therapy disciplines admitted as of the " +
                                            StartOfCareDateLabel + ".");
                    ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AllValid = false;
                }
            }

            // Skip the last validation and allow the SOC date to be after the election period start date, even for period 1, if TRANSFER from another hospice is TRUE. 
            // Note ValidateHospiceFields will take care of the rest of the validations
            if (sl != null && sl.IsHospiceServiceLine && TransferHospice)
            {
                return AllValid;
            }

            if (FirstCert != null && FirstCert.PeriodNumber == 1 && FirstCert.PeriodStartDate.HasValue &&
                SOCDate.HasValue
                && FirstCert.PeriodStartDate.Value.Date != SOCDate.Value.Date)
            {
                var Msg = string.Format("The " + StartOfCareDateLabel +
                                        " and First Certification from date must match when period 1 exists.");
                ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                AllValid = false;
            }

            return AllValid;
        }

        public bool ValidateEmploymentRelated()
        {
            var AllValid = true;
            if (IsEmploymentRelated && !EmploymentRelatedEmployer.HasValue)
            {
                string[] memberNames = { "EmploymentRelatedEmployer" };
                var Msg = "Employer is required when 'Employment Related' is selected.";
                ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                AllValid = false;
            }

            return AllValid;
        }

        public bool ValidateHospiceFields()
        {
            var AllValid = true;
            AllValid = ValidateTransferHospice() && ValidateHospiceBenefitReelection();

            if (TransferHospice && (AdmissionCertification == null || AdmissionCertification.Any() == false
                                                                   || AdmissionCertification.Any(ac =>
                                                                       ac.PeriodNumber <= 0 ||
                                                                       !ac.PeriodStartDate.HasValue ||
                                                                       !ac.PeriodEndDate.HasValue)))
            {
                //Validate newly created cert periods.
                if ((FirstCertPeriodNumber <= 0 || FirstCertPeriodNumber == null) && ShowCertEditWidgetsPlusMask &&
                    FirstCertPeriodNumberEnabled)
                {
                    string[] memberNames = { "FirstCertPeriodNumber" };
                    var Msg = "Period Number is required when 'Transfer From Another Hospice' is selected.";
                    ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    RaisePropertyChanged("FirstCertPeriodNumber");
                    AllValid = false;
                }

                if (!FirstCertFromDate.HasValue && ShowCertEditWidgetsPlusMask)
                {
                    string[] memberNames = { "FirstCertFromDate" };
                    var Msg = "Period Start Date is required when 'Transfer From Another Hospice' is selected.";
                    ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AllValid = false;
                }

                if (!FirstCertThruDate.HasValue && ShowCertEditWidgetsPlusMask)
                {
                    string[] memberNames = { "FirstCertThruDate" };
                    var Msg = "Period Thru Date is required when 'Transfer From Another Hospice' is selected.";
                    ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AllValid = false;
                }
            }
            else if (AdmissionCertification != null && AdmissionCertification.Any(ac =>
                         ac.HasChanges && !ac.IsNew && !(ac.PeriodStartDate.HasValue || ac.PeriodNumber <= 0)))
            {
                // somebody emptied out the fields
                if (FirstCertPeriodNumber <= 0 && ShowCertEditWidgetsPlusMask && FirstCertPeriodNumberEnabled)
                {
                    string[] memberNames = { "FirstCertPeriodNumber" };
                    var Msg = "Period Number is required.";
                    ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AllValid = false;
                }

                if (!FirstCertFromDate.HasValue && ShowCertEditWidgetsPlusMask)
                {
                    string[] memberNames = { "FirstCertFromDate" };
                    var Msg = "Period Start Date is required.";
                    ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AllValid = false;
                }

                if (!FirstCertThruDate.HasValue && ShowCertEditWidgetsPlusMask)
                {
                    string[] memberNames = { "FirstCertThruDate" };
                    var Msg = "Period Thru Date is required.";
                    ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                    AllValid = false;
                }
            }

            return AllValid;
        }

        private bool ValidateHospiceBenefitReelection()
        {
            var AllValid = true;
            if (HospiceBenefitReelection && (FirstCertPeriodNumber <= 0 || FirstCertPeriodNumber == null) &&
                ShowCertEditWidgetsPlusMask && FirstCertPeriodNumberEnabled)
            {
                string[] memberNames = { "FirstCertPeriodNumber" };
                var Msg = "Benefit Period Number is required when 'Hospice Benefit Reelection' is selected.";
                ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                AllValid = false;
            }

            return AllValid;
        }

        public bool ValidateTransferHospice()
        {
            var AllValid = true;
            if (TransferHospice && string.IsNullOrEmpty(TransferHospiceAgency))
            {
                string[] memberNames = { "TransferHospiceAgency" };
                var Msg = "Hospice Agency is required when 'Transfer From Another Hospice' is selected.";
                ValidationErrors.Add(new ValidationResult(Msg, memberNames));
                AllValid = false;
            }

            return AllValid;
        }

        //Save of form with discharge date, updates the discipline status - discharged and sets the discipline 
        //discharge date = the ISDischarge discharge date.
        //
        //Overrides the Admission's Discharged date, set it Status to "Discharged" and
        //returns a list of all Properties (DischargedDate and DischargedStatus) that have been modified
        public IEnumerable<PropertyChange<object>> OverrideAdmissionDischargedDate()
        {
            var synchronizedProperties = new List<PropertyChange<object>>();
            //Continue only if Patient has been discharged
            if (!DischargeDateTime.HasValue)
            {
                return synchronizedProperties;
            }

            //Continue only if there are Disciplines associated with this Admission
            if (AdmissionDiscipline == null)
            {
                return synchronizedProperties;
            }

            //Continue only if All Disciplines associated with this Admission have been discharged
            if (ActiveAdmissionDisciplines != null && ActiveAdmissionDisciplines.Any())
            {
                return synchronizedProperties;
            }

            //Find Discipline with highest Discharge Date
            var highestDischargedDate = AdmissionDiscipline.Where(d => d.DischargeDateTime.HasValue)
                .Max(d => d.DischargeDateTime);

            //Continue only if there is at least one Discipline Discharged
            if (highestDischargedDate == null)
            {
                return synchronizedProperties;
            }

            //Resets Admission Discharge Date to the highest (further in the future) Discipline's Discharge Date
            if (DischargeDateTime != highestDischargedDate)
            {
                object origDischargeDateTime = DischargeDateTime;
                var dischargeDateTimeDetails = GetType().GetProperty("DischargeDateTime");
                DischargeDateTime = highestDischargedDate;
                synchronizedProperties.Add(new PropertyChange<object>(dischargeDateTimeDetails, origDischargeDateTime,
                    DischargeDateTime, "Admission"));
            }

            //Sets Admission Status to "Discharged"
            var dischargedStatus = CodeLookupCache.GetKeyFromCode("AdmissionStatus", "D") ?? 0;
            if (AdmissionStatus != dischargedStatus)
            {
                object origDisciplineStatus = AdmissionStatus;
                var dischargeDisciplineStatus = GetType().GetProperty("AdmissionStatus");
                AdmissionStatus = dischargedStatus;
                synchronizedProperties.Add(new PropertyChange<object>(dischargeDisciplineStatus, origDisciplineStatus,
                    AdmissionStatus));
            }

            return synchronizedProperties;
        }

        public class AuthorizationGroups : GenericBase
        {
            private bool _AddAvailable;
            private CollectionViewSource _AdmissionAuthorizations = new CollectionViewSource();
            private int _AuthorizationCount;

            private CollectionViewSource _AvailableInsurance = new CollectionViewSource();
            private int _AvailableInsuranceCount;

            public AuthorizationGroups(Discipline d, Admission a)
            {
            }

            public CollectionViewSource AvailableInsurance
            {
                get { return _AvailableInsurance; }
                set
                {
                    _AvailableInsurance = value;
                    RaisePropertyChanged("AvailableInsurance");
                }
            }

            public int AuthorizationCount
            {
                get { return _AuthorizationCount; }
                set
                {
                    _AuthorizationCount = value;
                    RaisePropertyChanged("AuthorizationCount");
                }
            }

            public int AvailableInsuranceCount
            {
                get { return _AvailableInsuranceCount; }
                set
                {
                    _AvailableInsuranceCount = value;
                    RaisePropertyChanged("AvailableInsuranceCount");
                }
            }

            public Admission ParentAdmission { get; set; }
            public Discipline ParentDiscipline { get; set; }

            public bool AddAvailable
            {
                get { return _AddAvailable; }
                set
                {
                    _AddAvailable = value;
                    this.RaisePropertyChangedLambda(p => p.AddAvailable);
                }
            }

            public int InsuranceCount { get; set; }

            public int DisciplineCount { get; set; }

            public CollectionViewSource AdmissionAuthorizations
            {
                get { return _AdmissionAuthorizations; }
                set
                {
                    _AdmissionAuthorizations = value;
                    this.RaisePropertyChangedLambda(p => p.AdmissionAuthorizations);
                }
            }
        }

        public class DisciplineGroups : GenericBase
        {
            private int __cleanupCount;

            private bool _AddAvailable;

            private CollectionViewSource _AdmissionDisciplines = new CollectionViewSource();

            private CollectionViewSource _Employees = new CollectionViewSource();

            private readonly WeakReference _ParentAdmissionRef;

            private readonly WeakReference _ParentDisciplineRef;

            public DisciplineGroups(Discipline d, Admission a, List<UserProfile> userListCache, AdmissionGroup slg1,
                AdmissionGroup slg2, AdmissionGroup slg3, AdmissionGroup slg4, AdmissionGroup slg5)
            {
                _ParentDisciplineRef = new WeakReference(d);
                _ParentAdmissionRef = new WeakReference(a);

                AdmissionDisciplines.Source = ParentAdmission.AdmissionDiscipline;
                AdmissionDisciplines.SortDescriptions.Add(new SortDescription("ReferDateTime",
                    ListSortDirection.Descending));
                AdmissionDisciplines.Filter += (s, e) =>
                {
                    var ad = e.Item as AdmissionDiscipline;
                    e.Accepted = ad.DisciplineKey == ParentDiscipline.DisciplineKey;
                    if (e.Accepted)
                    {
                        DisciplineCount++;
                        if (ParentAdmission.CanExecuteIfDischargedOrTransferredOrNotTaken_AddDisciplineCommand == false)
                        {
                            AddAvailable = false;
                        }
                        else
                        {
                            AddAvailable = !ParentAdmission.AdmissionDiscipline.Where(p =>
                                p.DisciplineKey == ParentDiscipline.DisciplineKey && !p.NotTakenDateTime.HasValue &&
                                !p.DischargeDateTime.HasValue).Any();
                        }
                    }
                };

                CurrentAdmissionDiscipline = AdmissionDisciplines.View.Cast<AdmissionDiscipline>().FirstOrDefault();
                if (DisciplineCount > 0)
                {
                    Employees.Source = userListCache;
                    Employees.SortDescriptions.Add(new SortDescription("LastName", ListSortDirection.Ascending));
                    Employees.Filter += (s, e) =>
                    {
                        var up = e.Item as UserProfile;

                        if (up.UserId.Equals(Guid.Empty)) //up.UserId {00000000-0000-0000-0000-000000000000} System.Guid
                        {
                            e.Accepted = true;
                            return;
                        }

                        if (CurrentAdmissionDiscipline != null &&
                            CurrentAdmissionDiscipline.PrimaryCareGiver == up.UserId)
                        {
                            e.Accepted = true;
                            return;
                        }

                        e.Accepted = false;

                        if (ParentAdmission != null)
                        {
                            var dscp_is_ok = up.DisciplineInUserProfile.Any(diup =>
                                diup.Discipline != null &&
                                diup.Discipline.DisciplineKey == ParentDiscipline.DisciplineKey);
                            if (dscp_is_ok)
                            {
                                e.Accepted = true; //discipline is OK

                                if (ParentAdmission != null)
                                {
                                    e.Accepted = !up.Inactive && (up.IsNew || up.UserProfileServiceLine.Where(p =>
                                        p.ServiceLineKey == ParentAdmission.ServiceLineKey && !p.EndDate.HasValue &&
                                        p.Oversite).Any());
                                }

                                if (slg1 != null && !e.Accepted)
                                {
                                    e.Accepted = !up.Inactive && (up.IsNew ||
                                                                  up.IsOversiteOrCanVisitOrOwnerInHeirachy(
                                                                      slg1.ServiceLineGroupingKey));
                                }

                                if (slg2 != null && !e.Accepted)
                                {
                                    e.Accepted = !up.Inactive && (up.IsNew ||
                                                                  up.IsOversiteOrCanVisitOrOwnerInHeirachy(
                                                                      slg2.ServiceLineGroupingKey));
                                }

                                if (slg3 != null && !e.Accepted)
                                {
                                    e.Accepted = !up.Inactive && (up.IsNew ||
                                                                  up.IsOversiteOrCanVisitOrOwnerInHeirachy(
                                                                      slg3.ServiceLineGroupingKey));
                                }

                                if (slg4 != null && !e.Accepted)
                                {
                                    e.Accepted = !up.Inactive && (up.IsNew ||
                                                                  up.IsOversiteOrCanVisitOrOwnerInHeirachy(
                                                                      slg4.ServiceLineGroupingKey));
                                }

                                if (slg5 != null && !e.Accepted)
                                {
                                    e.Accepted = !up.Inactive && (up.IsNew ||
                                                                  up.IsOversiteOrCanVisitOrOwnerInHeirachy(
                                                                      slg5.ServiceLineGroupingKey));
                                }
                            }
                            else
                            {
                                e.Accepted = false;
                            }
                        }
                    };
                }
                else
                {
                    var invalid_dscp = "NOT CURRENTLY REFERRED DISCIPLINE";
                    Debug.WriteLine(invalid_dscp);
                }
            }

            public AdmissionDiscipline CurrentAdmissionDiscipline
            {
                get;
                set;
            }

            public Admission ParentAdmission =>
                _ParentAdmissionRef.Target as Admission;

            public Discipline ParentDiscipline =>
                _ParentDisciplineRef.Target as Discipline;

            public bool AddAvailable
            {
                get { return _AddAvailable; }
                set
                {
                    _AddAvailable = value;
                    this.RaisePropertyChangedLambda(p => p.AddAvailable);
                }
            }

            public int DisciplineCount { get; set; }

            public CollectionViewSource AdmissionDisciplines
            {
                get { return _AdmissionDisciplines; }
                set
                {
                    _AdmissionDisciplines = value;
                    this.RaisePropertyChangedLambda(p => p.AdmissionDisciplines);
                }
            }

            public CollectionViewSource Employees
            {
                get { return _Employees; }
                set
                {
                    _Employees = value;
                    this.RaisePropertyChangedLambda(p => p.Employees);
                }
            }

            public override void Cleanup()
            {
                ++__cleanupCount;

                if (__cleanupCount > 1)
                {
                    return;
                }

                CurrentAdmissionDiscipline = null;

                if (_AdmissionDisciplines != null)
                {
                    _AdmissionDisciplines.Source = null;
                }

                _AdmissionDisciplines = null;

                if (_Employees != null)
                {
                    _Employees.Source = null;
                }

                _Employees = null;

                base.Cleanup();
            }
        }

        #region ServiceLineGrouping with Headers

        public IEnumerable<ServiceLine> AllServiceLines => ServiceLineCache.GetActiveUserServiceLinePlusMe(null);

        private ServiceLine _selectedServiceLine;

        public ServiceLine SelectedServiceLine
        {
            get
            {
                var slk = ServiceLineKey;
                if (slk <= 0)
                {
                    _selectedServiceLine = null;
                }
                else
                {
                    if (_selectedServiceLine == null || !(_selectedServiceLine.MyServiceLineKey == slk))
                    {
                        _selectedServiceLine = AllServiceLines.Where(s => s.ServiceLineKey == slk).FirstOrDefault();
                    }
                }

                return _selectedServiceLine;
            }
        }

        public ObservableCollection<AdmissionGroup> AdmissionGroupSets
        {
            get
            {
                if (AdmissionGroup == null)
                {
                    return null;
                }

                return new ObservableCollection<AdmissionGroup>(AdmissionGroup
                    .Where(ag => ag.AdmissionGroupSiblingList != null && ag.MasterAdmissionGroupKey != null)
                    .OrderBy(g => g.StartDate)
                    .ToList());
            }
        }

        public bool AdmissionGroupChanged
        {
            get
            {
                if (AdmissionGroup == null)
                {
                    return false;
                }

                return AdmissionGroup.Where(ag => ag.IsNew || ag.HasChanges).Any();
            }
        }

        public bool ValidateServiceLineGrouping()
        {
            // If no serviceLineGroupHeaderKey, no field is displayed so no need to validate.
            if (ServiceLineGrouping1 == null || ServiceLineGrouping1.ServiceLineGroupHeaderKey == 0)
            {
                return true;
            }

            //set memberNames based on NotifyOnValidationError override:  <VirtuosoCoreControls:comboBoxServiceLineGrouping ...
            //BindingKeyForErrorNotify="{Binding CurrentAdmission.Group1BindingKey, Mode=TwoWay, NotifyOnValidationError=True, ValidatesOnNotifyDataErrors=True}" ...
            var memberNames = new[] { "Group1BindingKey" };


            if (CurrentGroup == null)
            {
                ValidationErrors.Add(new ValidationResult(ServiceLineGrouping1.GroupHeaderLabel + " is required.",
                    memberNames));
                return false;
            }

            var groupHeaderKey = ServiceLineCache.GetServiceLineGroupingFromKey(CurrentGroup.ServiceLineGroupingKey)
                .ServiceLineGroupHeaderKey;
            var selected = AdmissionGroup.Where(agg => agg.GroupHeaderKey == groupHeaderKey);

            if (!selected.Any())
            {
                ValidationErrors.Add(new ValidationResult(ServiceLineGrouping1.GroupHeaderLabel + " is required.",
                    memberNames));
                return false;
            }

            return true;
        }


        public int Group1BindingKey => 0;
        public int Group2BindingKey => 0;
        public int Group3BindingKey => 0;
        public int Group4BindingKey => 0;
        public int Group5BindingKey => 0;


        public AdmissionGroup CurrentGroup => GetNthCurrentGroup(0, AdmissionGroupDate);

        public AdmissionGroup CurrentGroup2 => GetNthCurrentGroup(1, AdmissionGroupDate);

        public AdmissionGroup CurrentGroup3 => GetNthCurrentGroup(2, AdmissionGroupDate);

        public AdmissionGroup CurrentGroup4 => GetNthCurrentGroup(3, AdmissionGroupDate);

        public AdmissionGroup CurrentGroup5 => GetNthCurrentGroup(4, AdmissionGroupDate);

        public ServiceLineGrouping GetFirstServiceLineGroupWithQIOName(DateTime currentDate)
        {
            for (var i = 4; i >= 0; i--)
            {
                var ag = GetNthCurrentGroup(i, currentDate.Date);
                if (ag == null)
                {
                    continue;
                }

                var slg = ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey);
                if (slg == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(slg.QIOName) == false)
                {
                    return slg;
                }
            }

            return null;
        }

        public ServiceLineGrouping GetFirstServiceLineGroupWithOasisHeader(DateTime currentDate)
        {
            for (var i = 4; i >= 0; i--)
            {
                var ag = GetNthCurrentGroup(i, currentDate.Date);
                if (ag == null)
                {
                    continue;
                }

                var slg = ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey);
                if (slg == null)
                {
                    continue;
                }

                var oh = OasisHeaderCache.GetOasisHeaderFromKey(slg.OasisHeaderKey);
                if (oh != null)
                {
                    return slg;
                }
            }

            return null;
        }

        public bool UsingDiagnosisCodersAtServiceLineGroupings(DateTime? currentDate)
        {
            var date = currentDate == null ? DateTime.Today.Date : ((DateTime)currentDate).Date;
            for (var i = 4; i >= 0; i--)
            {
                var ag = GetNthCurrentGroup(i, date);
                if (ag == null)
                {
                    continue;
                }

                var slg = ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey);
                if (slg == null)
                {
                    continue;
                }

                if (slg.UsingDiagnosisCoders)
                {
                    return true;
                }
            }

            return false;
        }

        public bool UsingHISCoordinatorAtServiceLineGroupings(DateTime? currentDate)
        {
            var date = currentDate == null ? DateTime.Today.Date : ((DateTime)currentDate).Date;
            for (var i = 4; i >= 0; i--)
            {
                var ag = GetNthCurrentGroup(i, date);
                if (ag == null)
                {
                    continue;
                }

                var slg = ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey);
                if (slg == null)
                {
                    continue;
                }

                if (slg.UsingHISCoordinator)
                {
                    return true;
                }
            }

            return false;
        }

        public bool UsingOASISCoordinatorAtServiceLineGroupings(DateTime? currentDate)
        {
            var date = currentDate == null ? DateTime.Today.Date : ((DateTime)currentDate).Date;
            for (var i = 4; i >= 0; i--)
            {
                var ag = GetNthCurrentGroup(i, date);
                if (ag == null)
                {
                    continue;
                }

                var slg = ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey);
                if (slg == null)
                {
                    continue;
                }

                if (slg.UsingOASISCoordinator)
                {
                    return true;
                }
            }

            return false;
        }

        public bool UsingOrderEntryReviewersAtServiceLineGroupings(DateTime? currentDate)
        {
            var date = currentDate == null ? DateTime.Today.Date : ((DateTime)currentDate).Date;
            for (var i = 4; i >= 0; i--)
            {
                var ag = GetNthCurrentGroup(i, date);
                if (ag == null)
                {
                    continue;
                }

                var slg = ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey);
                if (slg == null)
                {
                    continue;
                }

                if (slg.UsingOrderEntryReviewers)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ServiceOrdersHeldUntilReviewedAtServiceLineGroupings(DateTime? currentDate)
        {
            var date = currentDate == null ? DateTime.Today.Date : ((DateTime)currentDate).Date;
            for (var i = 4; i >= 0; i--)
            {
                var ag = GetNthCurrentGroup(i, date);
                if (ag == null)
                {
                    continue;
                }

                var slg = ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey);
                if (slg == null)
                {
                    continue;
                }

                if (slg.ServiceOrdersHeldUntilReviewed)
                {
                    return true;
                }
            }

            return false;
        }

        public bool UsingPOCOrderReviewersAtServiceLineGroupings(DateTime? currentDate)
        {
            var date = currentDate == null ? DateTime.Today.Date : ((DateTime)currentDate).Date;
            for (var i = 4; i >= 0; i--)
            {
                var ag = GetNthCurrentGroup(i, date);
                if (ag == null)
                {
                    continue;
                }

                var slg = ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey);
                if (slg == null)
                {
                    continue;
                }

                if (slg.UsingPOCOrderReviewers)
                {
                    return true;
                }
            }

            return false;
        }

        public ServiceLineGroupHeader ServiceLineGrouping1 => GetNthServiceLineGroupHeader(0);

        public ServiceLineGroupHeader HospiceServiceLineGroupHeader
        {
            get
            {
                if (AdmissionGroup == null)
                {
                    return null;
                }

                var agList = AdmissionGroup.Where(ag => ServiceLineCache
                        .GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey)
                        .ServiceLineGroupHeader.ScheduleTeamMeeting)
                    .ToList();
                if (agList == null || agList.Any() == false)
                {
                    return null;
                }

                var slgh = agList.FirstOrDefault();
                if (slgh == null || slgh.ServiceLineGroupingKey < 1)
                {
                    return null;
                }

                return ServiceLineCache.GetServiceLineGroupingFromKey(slgh.ServiceLineGroupingKey)
                    .ServiceLineGroupHeader;
            }
        }

        public AdmissionGroup GetServiceLineGroupingForTeamMeeting(DateTime FromDate)
        {
            if (AdmissionGroup == null)
            {
                return null;
            }

            // Get the proper service line grouping row to use.
            return AdmissionGroup.Where(ag =>
                ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey).ServiceLineGroupHeader
                    .ScheduleTeamMeeting
                && (ag.EndDate >= FromDate || ag.EndDate == null)
                && (ag.StartDate <= FromDate || ag.EndDate == null)).FirstOrDefault();
        }

        public ServiceLineGroupHeader ServiceLineGrouping2 => GetNthServiceLineGroupHeader(1);

        public ServiceLineGroupHeader ServiceLineGrouping3 => GetNthServiceLineGroupHeader(2);

        public ServiceLineGroupHeader ServiceLineGrouping4 => GetNthServiceLineGroupHeader(3);

        public ServiceLineGroupHeader ServiceLineGrouping5 => GetNthServiceLineGroupHeader(4);

        public string ServiceLineGroupingDisplayString
        {
            get
            {
                var retstring = "";
                if (ServiceLineGrouping1 != null)
                {
                    retstring = ServiceLineGrouping1.GroupHeaderLabel;
                }

                if (ServiceLineGrouping2 != null)
                {
                    retstring = retstring + " - " + ServiceLineGrouping2.GroupHeaderLabel;
                }

                if (ServiceLineGrouping3 != null)
                {
                    retstring = retstring + " - " + ServiceLineGrouping3.GroupHeaderLabel;
                }

                if (ServiceLineGrouping4 != null)
                {
                    retstring = retstring + " - " + ServiceLineGrouping4.GroupHeaderLabel;
                }

                if (ServiceLineGrouping5 != null)
                {
                    retstring = retstring + " - " + ServiceLineGrouping5.GroupHeaderLabel;
                }

                return retstring;
            }
        }

        public int NumberOfDefinedServiceLineGroups
        {
            get
            {
                if (ServiceLineGrouping5 != null)
                {
                    return 5;
                }

                if (ServiceLineGrouping4 != null)
                {
                    return 4;
                }

                if (ServiceLineGrouping3 != null)
                {
                    return 3;
                }

                if (ServiceLineGrouping2 != null)
                {
                    return 2;
                }

                if (ServiceLineGrouping1 != null)
                {
                    return 1;
                }

                return 0;
            }
        }

        private ServiceLineGroupHeader GetNthServiceLineGroupHeader(int HeaderToRetrieve)
        {
            var ssl = SelectedServiceLine;

            if (ssl == null)
            {
                return null;
            }

            if (ssl.ServiceLineGroupHeader == null)
            {
                return null;
            }

            if (ssl.ServiceLineGroupHeader.Count() > HeaderToRetrieve)
            {
                return ssl.ServiceLineGroupHeader.Where(gh => gh.SequenceNumber == HeaderToRetrieve).FirstOrDefault();
            }

            return null;
        }

        #endregion
    }

    public partial class EncounterAdmission : IServiceLineGroupingService, IEncounterAdmission
    {
        private int? _PreviousAttendingPhysicianKey;
        private int? _PreviousSigningPhysicianKey;

        public bool? Resumed => Admitted;

        public string PreEvalStatusPlanToAdmit => "Plan To Admit";
        public string PreEvalStatusDoNotAdmit => "Do Not Admit";
        public string PreEvalStatusOnHold => "On Hold";
        public bool IsPreEvalStatusPlanToAdmit => PreEvalStatus == PreEvalStatusPlanToAdmit ? true : false;
        public bool IsPreEvalStatusDoNotAdmit => PreEvalStatus == PreEvalStatusDoNotAdmit ? true : false;
        public bool IsPreEvalStatusOnHold => PreEvalStatus == PreEvalStatusOnHold ? true : false;

        public bool CanEditSOC => false;

        public Admission Admission { get; set; }

        public AdmissionReferral CurrentReferral
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                if (AdmissionReferralKey == null)
                {
                    return null;
                }

                // NOTE: It is possible to have an AdmissionReferralKey to a row in AdmissionReferral table that has its HistoryKey set.
                //       This can happen is a Referred admission to reverted back to Transfer by the Override Tool.
                return Admission.AdmissionReferral.Where(p => p.AdmissionReferralKey == (int)AdmissionReferralKey)
                    .FirstOrDefault();
            }
        }

        public string ProviderStateCodeCode => CodeLookupCache.GetCodeFromKey(ProviderStateCode);

        public int? PreviousAttendingPhysicianKey
        {
            get { return _PreviousAttendingPhysicianKey; }
            set
            {
                _PreviousAttendingPhysicianKey = value;
                RaisePropertyChanged("PreviousAttendingPhysicianKey");
            }
        }

        public int? PreviousSigningPhysicianKey
        {
            get { return _PreviousSigningPhysicianKey; }
            set
            {
                _PreviousSigningPhysicianKey = value;
                RaisePropertyChanged("PreviousSigningPhysicianKey");
            }
        }

        public string AddendumText
        {
            get
            {
                if (PreviousSigningPhysicianKey == SigningPhysicianKey)
                {
                    return null;
                }

                var origPhysician =
                    PhysicianCache.Current.GetPhysicianFullNameInformalWithSuffixFromKey(PreviousSigningPhysicianKey);
                if (string.IsNullOrWhiteSpace(origPhysician))
                {
                    origPhysician = "none";
                }

                var newPhysician =
                    PhysicianCache.Current.GetPhysicianFullNameInformalWithSuffixFromKey(SigningPhysicianKey);
                if (string.IsNullOrWhiteSpace(newPhysician))
                {
                    newPhysician = "none";
                }

                if (origPhysician == newPhysician)
                {
                    return null;
                }

                return string.Format("Signing physician changed from {0} to {1}", origPhysician, newPhysician);
            }
        }

        public string ProviderCityStateZip =>
            FormatHelper.FormatCityStateZip(ProviderCity, ProviderStateCodeCode, ProviderZipCode);

        public AdmissionGroup CurrentGroup
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                if (Admission.AdmissionGroup == null)
                {
                    return null;
                }

                return AdmissionGroupKey == null
                    ? null
                    : Admission.AdmissionGroup.Where(ag => ag.AdmissionGroupKey == AdmissionGroupKey).FirstOrDefault();
            }
        }

        public AdmissionGroup CurrentGroup2
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                if (Admission.AdmissionGroup == null)
                {
                    return null;
                }

                return AdmissionGroup2Key == null
                    ? null
                    : Admission.AdmissionGroup.Where(ag => ag.AdmissionGroupKey == AdmissionGroup2Key).FirstOrDefault();
            }
        }

        public AdmissionGroup CurrentGroup3
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                if (Admission.AdmissionGroup == null)
                {
                    return null;
                }

                return AdmissionGroup2Key == null
                    ? null
                    : Admission.AdmissionGroup.Where(ag => ag.AdmissionGroupKey == AdmissionGroup3Key).FirstOrDefault();
            }
        }

        public AdmissionGroup CurrentGroup4
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                if (Admission.AdmissionGroup == null)
                {
                    return null;
                }

                return AdmissionGroup2Key == null
                    ? null
                    : Admission.AdmissionGroup.Where(ag => ag.AdmissionGroupKey == AdmissionGroup4Key).FirstOrDefault();
            }
        }

        public AdmissionGroup CurrentGroup5
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                if (Admission.AdmissionGroup == null)
                {
                    return null;
                }

                return AdmissionGroup5Key == null
                    ? null
                    : Admission.AdmissionGroup.Where(ag => ag.AdmissionGroupKey == AdmissionGroup5Key).FirstOrDefault();
            }
        }

        partial void OnPreEvalStatusChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsPreEvalStatusPlanToAdmit");
            RaisePropertyChanged("IsPreEvalStatusDoNotAdmit");
            RaisePropertyChanged("IsPreEvalStatusOnHold");
        }

        private AdmissionGroup GetNthCurrentGroup(int GroupToRetrieve)
        {
            var ag = Admission.AdmissionGroup.Where(ag1 => ag1.GroupHeaderSequence == GroupToRetrieve
                                                           && !ag1.EndDate.HasValue).OrderByDescending(p => p.StartDate)
                .ThenBy(k => k.AdmissionGroupKey).FirstOrDefault();
            if (ag == null)
            {
                ag = Admission.AdmissionGroup.Where(ag2 => ag2.GroupHeaderSequence == GroupToRetrieve)
                    .OrderByDescending(p => p.StartDate).ThenBy(k => k.AdmissionGroupKey)
                    .FirstOrDefault();
            }

            return ag;
        }

        public void RefreshEncounterAdmissionFromAdmission(Admission a, AdmissionPhysicianFacade phy,
            AdmissionDiscipline ad)
        {
            TenantID = a.TenantID;
            AdmissionID = a.AdmissionID;

            //Should we update these if Encounter.EncounterStatus == (int)EncounterStatusType.Completed)?
            AttendingPhysicianKey = phy == null ? null : phy.AttendingPhysicianKey;
            // The plan of Care physician gets set in POCBase in DynamicQuestion.cs
            if (Encounter.FormKey != null && DynamicFormCache.GetFormByKey((int)Encounter.FormKey).IsPlanOfCare)
            {
                SigningPhysicianKey = SigningPhysicianKey;
                SigningPhysicianAddressKey = SigningPhysicianAddressKey;
            }
            else
            {
                SigningPhysicianKey = phy == null ? null :
                    phy.OverrideSigningPhysicianKey == null ? phy.SigningPhysicianKey : phy.OverrideSigningPhysicianKey;
                if (phy != null && phy.SigningAdmissionPhysician != null)
                {
                    SigningPhysicianAddressKey = phy.SigningAdmissionPhysician.PhysicianAddressKey;
                }
            }

            ServicePriorityOne = a.ServicePriorityOne;
            ServicePriorityTwo = a.ServicePriorityTwo;
            ServicePriorityThree = a.ServicePriorityThree;

            ServicePriorityOneComment = a.ServicePriorityOneComment;

            ServiceLineKey = a.ServiceLineKey;
            NotTakenReason = ad != null ? ad.NotTakenReason : a.NotTakenReason;
            DischargeDateTime = ad != null ? ad.DischargeDateTime : a.DischargeDateTime;
            FacilityKey = a.FacilityKey;
            PhysicianOrderedSOCDate = a.PhysicianOrderedSOCDate;
            SOCDate = a.SOCDate;
            IsEmploymentRelated = a.IsEmploymentRelated;
            EmploymentRelatedEmployer = a.EmploymentRelatedEmployer;
            FaceToFaceEncounter = a.FaceToFaceEncounter;
            FaceToFaceEncounterDate = a.FaceToFaceEncounterDate;
            PatientInsuranceKey = a.PatientInsuranceKey;
            InitialReferralDate = a.InitialReferralDate;
            Confidentiality = a.Confidentiality;
            CareCoordinator = a.CareCoordinator;
            IgnoreSOCMismatch = a.IgnoreSOCMismatch;
            FaceToFacePhysicianKey = a.FaceToFacePhysicianKey;
            FaceToFaceExceptReason = a.FaceToFaceExceptReason;
            SourceOfAdmission = a.SourceOfAdmission;
            ReleaseOfInformation = a.ReleaseOfInformation;
            HasTrauma = a.HasTrauma;
            TraumaType = a.TraumaType;
            TraumaDate = a.TraumaDate;
            TraumaStateCode = a.TraumaStateCode;
            TransferHospice = a.TransferHospice;
            TransferHospiceAgency = a.TransferHospiceAgency;

            Facility1Label = a.Facility1Label;
            Facility2Label = a.Facility2Label;
            Facility3Label = a.Facility3Label;
            FaceToFaceRequired = a.FaceToFaceRequired;
            PhysicianMismatch = a.PhysicianMismatch;
            var ac = a.GetAdmissionCertForDate(Encounter.CreatedDate, false);
            if (ac == null)
            {
                CurrentCertPeriodNumber = a.CurrentCertPeriodNumber;
                CurrentCertFromDate = a.CurrentCertFromDate;
                CurrentCertThruDate = a.CurrentCertThruDate;
            }
            else
            {
                CurrentCertPeriodNumber = ac.PeriodNumber;
                CurrentCertFromDate = ac.PeriodStartDate;
                CurrentCertThruDate = ac.PeriodEndDate;
            }

            if (ad != null)
            {
                Admitted = ad.Admitted;
                NotTaken = ad.NotTaken;
            }

            AdmissionGroupKey = a.CurrentGroup == null ? (int?)null : a.CurrentGroup.AdmissionGroupKey;
            AdmissionGroup2Key = a.CurrentGroup2 == null ? (int?)null : a.CurrentGroup2.AdmissionGroupKey;
            AdmissionGroup3Key = a.CurrentGroup3 == null ? (int?)null : a.CurrentGroup3.AdmissionGroupKey;
            AdmissionGroup4Key = a.CurrentGroup4 == null ? (int?)null : a.CurrentGroup4.AdmissionGroupKey;
            AdmissionGroup5Key = a.CurrentGroup5 == null ? (int?)null : a.CurrentGroup5.AdmissionGroupKey;
            AdmissionReferralKey = a.CurrentReferral == null ? (int?)null : a.CurrentReferral.AdmissionReferralKey;

            PreEvalStatus = a.PreEvalStatus;
            PreEvalOnHoldReason = a.PreEvalOnHoldReason;
            PreEvalOnHoldDateTime = a.PreEvalOnHoldDateTime;
            PreEvalFollowUpDate = a.PreEvalFollowUpDate;
            PreEvalFollowUpComments = a.PreEvalFollowUpComments;

            ProviderName = a.ProviderName;
            ProviderPhoneExtension = a.ProviderPhoneExtension;
            ProviderPhoneNumber = a.ProviderPhoneNumber;
            ProviderStateCode = a.ProviderStateCode;
            ProviderZipCode = a.ProviderZipCode;
            ProviderFaxNumber = a.ProviderFaxNumber;
            ProviderAddress1 = a.ProviderAddress1;
            ProviderAddress2 = a.ProviderAddress2;
            ProviderCity = a.ProviderCity;
            CMSCertificationNumber = a.CMSCertificationNumber;
            DeathDate = a.DeathDate;
            DeathTime = a.DeathTime;
            VerbalSOCDate = a.VerbalSOCDate;
            IsDependentOnElectricity = a.IsDependentOnElectricity;
        }
    }

    public partial class AdmissionABN
    {
        public string ABNTypeDescription => CodeLookupCache.GetCodeDescriptionFromKey(ABNType);

        public string AdmissionDocumentationABNTypeDescription
        {
            get
            {
                var t = ABNTypeDescription;
                if (string.IsNullOrWhiteSpace(t))
                {
                    return "CMS Form";
                }

                if (t.ToLower().Contains("abn"))
                {
                    return "ABN";
                }

                if (t.ToLower().Contains("denc"))
                {
                    return "DENC";
                }

                if (t.ToLower().Contains("hhccn"))
                {
                    return "HHCCN";
                }

                if (t.ToLower().Contains("nomnc"))
                {
                    return "NOMNC";
                }

                if (t.ToLower().Contains("mhes"))
                {
                    return "Hospice Election Statement";
                }

                if (t.ToLower().Contains("patcon"))
                {
                    return "Patient Consent";
                }

                return "CMS Form";
            }
        }
    }

    public partial class AdmissionBillingPOCO
    {
        public string FinalIndicatorDescription => FinalIndicator ? "(final)" : null;
    }

    public partial class AdmissionDiscipline
    {
        private const string _none = "'none'";
        private string _AdmissionStatusCode;
        private bool? _Admitted;

        private string _DischargeReasonCode;

        // This property is currently used to keep track of the discipline status when admitting through the services tab.
        // Without it, but the Status properties above are being hit and altering the status when the shouldn't be.  In order
        // to avoid messing with the admission status logic in dynamic form this property is a 'workaround' ... that I REALLY dislike!
        private int? _disciplineStatusInternal;

        // EncounterResumption is needed to coordinate changes between AdmissionStatus and assocuated EncounterResumption
        private EncounterResumption _EncounterResumption;
        private bool _InSaveAdmissionStatusData;
        private bool? _NotTaken;

        private string _ReasonDCCode;

        public bool AdmissionDisciplineWasAdmitted
        {
            get
            {
                if (AdmissionDisciplineStatusCodeWasAdmitted == false)
                {
                    return false;
                }

                return DisciplineAdmitDateTime == null ? false : true;
            }
        }

        private bool AdmissionDisciplineStatusCodeWasAdmitted
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AdmissionStatusCode))
                {
                    return false;
                }

                if (AdmissionStatusCode == "A" || AdmissionStatusCode == "D" || AdmissionStatusCode == "T" ||
                    AdmissionStatusCode == "M")
                {
                    return true;
                }

                if (AdmissionStatusCode == "R" && DisciplineAdmitDateTime != null)
                {
                    return true; // in the process of being admitted
                }

                return false;
            }
        }

        public bool AdmissionDisciplineStatusCanAddFCDOrder
        {
            get
            {
                // Can add FCD order if Referred, Admitted or Resumed
                if (string.IsNullOrWhiteSpace(AdmissionStatusCode))
                {
                    return false;
                }

                return AdmissionStatusCode == "R" || AdmissionStatusCode == "A" || AdmissionStatusCode == "M"
                    ? true
                    : false;
            }
        }

        public bool AdmissionDisciplineWasDischarged
        {
            get
            {
                if (AdmissionDisciplineWasAdmitted == false)
                {
                    return false;
                }

                return AdmissionDisciplineStatusCodeWasDischarged;
            }
        }

        private bool AdmissionDisciplineStatusCodeWasDischarged
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AdmissionStatusCode))
                {
                    return false;
                }

                return AdmissionStatusCode == "D" ? true : false;
            }
        }

        public bool IsOASISBypass
        {
            get
            {
                var d = DisciplineCache.GetDisciplineFromKey(DisciplineKey);
                if (d == null)
                {
                    return true;
                }

                return d.OASISBypass;
            }
        }

        public bool DisciplineEvalServiceTypeOptional
        {
            get
            {
                var d = DisciplineCache.GetDisciplineFromKey(DisciplineKey);
                if (d == null)
                {
                    return false;
                }

                return d.EvalServiceTypeOptional;
            }
        }

        public string AdmissionDisciplineHCFACode
        {
            get
            {
                var hcfaCode = DisciplineCache.GetHCFACodeFromKey(DisciplineKey);
                if (string.IsNullOrWhiteSpace(hcfaCode))
                {
                    return null;
                }

                return hcfaCode.Trim();
            }
        }

        public bool AdmissionDisciplineIsAide
        {
            get
            {
                var hcfaCode = AdmissionDisciplineHCFACode;
                if (string.IsNullOrWhiteSpace(hcfaCode))
                {
                    return false;
                }

                return hcfaCode.ToUpper() == "F" ? true : false;
            }
        }

        public bool AdmissionDisciplineIsOT
        {
            get
            {
                var hcfaCode = AdmissionDisciplineHCFACode;
                if (string.IsNullOrWhiteSpace(hcfaCode))
                {
                    return false;
                }

                return hcfaCode.ToUpper() == "C" ? true : false;
            }
        }

        public bool AdmissionDisciplineIsPT
        {
            get
            {
                var hcfaCode = AdmissionDisciplineHCFACode;
                if (string.IsNullOrWhiteSpace(hcfaCode))
                {
                    return false;
                }

                return hcfaCode.ToUpper() == "B" ? true : false;
            }
        }

        public bool AdmissionDisciplineIsPhysicianServices
        {
            get
            {
                var hcfaCode = AdmissionDisciplineHCFACode;
                if (string.IsNullOrWhiteSpace(hcfaCode))
                {
                    return false;
                }

                return hcfaCode.ToUpper() == "P" ? true : false;
            }
        }

        public bool AdmissionDisciplineIsSLP
        {
            get
            {
                var hcfaCode = AdmissionDisciplineHCFACode;
                if (string.IsNullOrWhiteSpace(hcfaCode))
                {
                    return false;
                }

                return hcfaCode.ToUpper() == "D" ? true : false;
            }
        }

        public bool AdmissionDisciplineIsSN
        {
            get
            {
                var hcfaCode = AdmissionDisciplineHCFACode;
                if (string.IsNullOrWhiteSpace(hcfaCode))
                {
                    return false;
                }

                return hcfaCode.ToUpper() == "A" ? true : false;
            }
        }

        public bool AdmissionDisciplineIsOTorPTorSLPorSN => AdmissionDisciplineIsOT || AdmissionDisciplineIsPT ||
                                                            AdmissionDisciplineIsSLP || AdmissionDisciplineIsSN
            ? true
            : false;

        public bool AdmissionStatusIsAdmitted
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AdmissionStatusCode))
                {
                    return false;
                }

                return AdmissionStatusCode == "A" ? true : false;
            }
        }

        public bool AdmissionStatusIsResumed
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AdmissionStatusCode))
                {
                    return false;
                }

                return AdmissionStatusCode == "H" ? true : false;
            }
        }

        public bool ForceNotTaken { get; set; }

        private AdmissionDiscipline OriginalAdmissionDisciplineRow { get; set; }

        public bool NotTaken
        {
            get
            {
                if (_NotTaken == null)
                {
                    _NotTaken = AdmissionStatusCode == "N" ? true : false;
                }

                return (bool)_NotTaken;
            }
            set
            {
                _NotTaken = value;
                if (value)
                {
                    if (AdmissionStatusHelper.CanChangeToNotTakenStatus(AdmissionStatus))
                    {
                        AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_NotTaken;
                        DisciplineAdmitDateTime = null;
                    }
                }

                RaisePropertyChanged("NotTaken");
            }
        }

        public bool Admitted
        {
            get
            {
                if (_Admitted == null)
                {
                    _Admitted = AdmissionStatusCode == "A" ? true : false;
                    if (_Admitted == false)
                    {
                        _Admitted = DisciplineAdmitDateTime != null ? true : false;
                    }
                }

                return (bool)_Admitted;
            }
            set
            {
                _Admitted = value;
                if (value)
                {
                    if (AdmissionStatusHelper.CanChangeToAdmittedStatus(AdmissionStatus))
                    {
                        AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Admitted;
                        NotTakenDateTime = null;
                        NotTakenReason = null;
                    }
                }

                RaisePropertyChanged("Admitted");
            }
        }

        public bool TeleMonitorDiscipline
        {
            get
            {
                var d = DisciplineCache.GetDisciplineFromKey(DisciplineKey);
                if (d == null)
                {
                    return false;
                }

                return d.TeleMonitorDiscipline;
            }
        }

        public DateTime? ReferDate => ReferDateTime == null ? (DateTime?)null : ReferDateTime.Value.Date;

        public string DisciplineDescription => DisciplineCache.GetDescriptionFromKey(DisciplineKey);

        public string DisciplineCode => DisciplineCache.GetCodeFromKey(DisciplineKey);

        public string DisciplineDescriptionWithDefault
        {
            get
            {
                var ret = DisciplineCache.GetDescriptionFromKey(DisciplineKey);
                return ret ?? " "; //Need string with space for default value in combo box
            }
        }

        public string ReferralDescriptionForOrderEntry => DisciplineCache.GetDescriptionFromKey(DisciplineKey) +
                                                          (string.IsNullOrWhiteSpace(ReferralReason)
                                                              ? ""
                                                              : ", Referral Reason: " + ReferralReason);

        public bool CanDeleteAdmissionDiscipline => IsNew ? true : false;

        public int? DisciplineStatusInternal
        {
            get { return _disciplineStatusInternal == null ? AdmissionStatus : _disciplineStatusInternal; }
            set { _disciplineStatusInternal = value; }
        }

        public bool CanEditReferDateTime
        {
            get
            {
                // anyone can edit the refer date on new referrals or if the status is R=Referred
                if (IsNew || AdmissionDisciplineKey <= 0)
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(AdmissionStatusCode))
                {
                    return true;
                }

                if (AdmissionStatusCode == "R")
                {
                    return true;
                }

                if (SOCandReferBeforeGoLiveDate)
                {
                    return true;
                }

                // If not new or in R=Referral status, only the system admin can change the referral date
                return RoleAccessHelper.CheckPermission(RoleAccess.Admin, false) ? true : false;
            }
        }

        public bool CanSeeAdmitDateLabel => CanEditAdmitDateTime || DisciplineAdmitDateTime.HasValue;

        public bool CanSeeAdmitDateText => !CanEditAdmitDateTime && DisciplineAdmitDateTime.HasValue;

        private bool SOCandReferBeforeGoLiveDateOrEditableTeleHealth
        {
            get
            {
                if (SOCandReferBeforeGoLiveDate)
                {
                    return true;
                }

                return EditableTeleHealth;
            }
        }

        private bool EditableTeleHealth
        {
            get
            {
                if (TeleMonitorDiscipline == false)
                {
                    return false;
                }

                return AreEncountersAgainstThisAdmissionDiscipline ? false : true;
            }
        }

        private bool SOCandReferBeforeGoLiveDate
        {
            get
            {
                if (Admission == null)
                {
                    return false;
                }

                var goLiveDate = ServiceLineCache.Current.GoLiveDateForAdmission(Admission, Admission.ServiceLineKey);
                var ret = false;
                if (goLiveDate != null && Admission.SOCDate != null && Admission.SOCDate < goLiveDate &&
                    ReferDate != null && ReferDate < goLiveDate)
                {
                    ret = true;
                }

                return ret;
            }
        }

        private bool AreEncountersAgainstThisAdmissionDiscipline
        {
            get
            {
                if (Admission == null || Admission.Encounter == null)
                {
                    return false;
                }

                return Admission.Encounter
                    .Where(e => e.AdmissionDisciplineKey == AdmissionDisciplineKey && e.Inactive == false).Any()
                    ? true
                    : false;
            }
        }

        public bool CanEditAdmitDateTime
        {
            get
            {
                if (SOCandReferBeforeGoLiveDateOrEditableTeleHealth)
                {
                    return true;
                }

                if (TeleMonitorDiscipline == false)
                {
                    return false; // no one can touch admit date for non telemonitor
                }

                if (RoleAccessHelper.CheckPermission(RoleAccess.Admin, false))
                {
                    return true; // Sysadmin can touch AdmitDate for TM all the time
                }

                if (OriginalAdmissionStatusCode == "R")
                {
                    return true; // Non-Sysadmin can touch AdmitDate only while discipline is referred
                }

                return false;
            }
        }

        public bool CanSeeNotTakenLabel => CanEditNotTaken || NotTakenDateTime.HasValue;

        public bool CanSeeNotTakenText => !CanEditNotTaken && NotTakenDateTime.HasValue;

        public bool CanEditNotTaken
        {
            get
            {
                // Can edit NotTaken if status is R=Referred 
                if (string.IsNullOrWhiteSpace(OriginalAdmissionStatusCode))
                {
                    return true;
                }

                if (OriginalAdmissionStatusCode == "R")
                {
                    return true;
                }

                if ((AdmissionStatusCode == "R" || DisciplineAdmitDateTime == null) && EditableTeleHealth)
                {
                    return true;
                }

                // Otherwise, only SystemAdministrators can edit NotTaken fields (but only if the original status is NotTaken)
                if (RoleAccessHelper.CheckPermission(RoleAccess.Admin, false) == false)
                {
                    return false;
                }

                return OriginalAdmissionStatusCode == "N" ? true : false;
                //return ((DischargeDateTime.HasValue== false) && (AdmissionStatus != CodeLookupCache.GetKeyFromCode("AdmissionStatus", "D"))); 
            }
        }

        public bool CanEditNotTakenReason
        {
            get
            {
                if (CanEditNotTaken)
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(OriginalAdmissionStatusCode))
                {
                    return true;
                }

                if (OriginalAdmissionStatusCode == "R")
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanSeeDischargeDateLabel => TelehealthDischargeVisible || DischargeDateTime.HasValue;

        public bool TelehealthDischargeVisible => TeleMonitorDiscipline;

        public bool TelehealthDischargeVisibleAndHospice => Admission.HospiceAdmission && TeleMonitorDiscipline;

        public bool CanSeeDischargeDateText => !TelehealthDischargeVisible && DischargeDateTime.HasValue;

        public EncounterResumption EncounterResumption
        {
            get { return _EncounterResumption; }
            set
            {
                _EncounterResumption = value;
                RaisePropertyChanged("EncounterResumption");
            }
        }

        public bool InSaveAdmissionStatusData
        {
            get { return _InSaveAdmissionStatusData; }
            set
            {
                _InSaveAdmissionStatusData = value;
                RaisePropertyChanged("InSaveAdmissionStatusData");
            }
        }

        public string OriginalAdmissionStatusCode
        {
            get
            {
                if (IsNew)
                {
                    return "R";
                }

                var ad = GetOriginalRow();
                if (ad != null)
                {
                    if (ad.AdmissionStatus == null || ad.AdmissionStatus == 0)
                    {
                        return "R";
                    }

                    return ad.AdmissionStatusCode;
                }

                if (AdmissionStatus == null || AdmissionStatus == 0)
                {
                    return "R";
                }

                return AdmissionStatusCode;
            }
        }

        public string AdmissionStatusCode
        {
            get
            {
                if (_AdmissionStatusCode == null)
                {
                    _AdmissionStatusCode = CodeLookupCache.GetCodeFromKey(AdmissionStatus);
                }

                return _AdmissionStatusCode;
            }
        }

        public string ReasonDCCode
        {
            get
            {
                if (_ReasonDCCode == null)
                {
                    _ReasonDCCode = CodeLookupCache.GetCodeFromKey(ReasonDCKey);
                }

                return _ReasonDCCode;
            }
        }

        public string ReasonDCCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(ReasonDCKey);

        public string DischargeReasonCode
        {
            get
            {
                if (_DischargeReasonCode == null)
                {
                    _DischargeReasonCode = CodeLookupCache.GetCodeFromKey(DischargeReasonKey);
                }

                return _DischargeReasonCode;
            }
        }

        public string DischargeReasonCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(DischargeReasonKey);

        public string AdmissionStatusText
        {
            get
            {
                if (NotTakenDateTime.HasValue && AdmissionStatusCode == "N")
                {
                    return "Not admitted on " + NotTakenDateTime.Value.ToString("MM/dd/yyyy");
                }

                if (DischargeDateTime.HasValue && AdmissionStatusCode == "D")
                {
                    return "Discharged on " + DischargeDateTime.Value.ToString("MM/dd/yyyy");
                }

                if (DisciplineAdmitDateTime.HasValue && AdmissionStatusCode == "A")
                {
                    return "Admitted on " + DisciplineAdmitDateTime.Value.ToString("MM/dd/yyyy");
                }

                if (ReferDateTime.HasValue && AdmissionStatusCode == "R")
                {
                    return "Referred on " + ReferDateTime.Value.ToString("MM/dd/yyyy");
                }

                return "Unknown status";
            }
        }

        public bool IsAdmissionStatusDischarge
        {
            get
            {
                var c = AdmissionStatusCode;
                if (string.IsNullOrWhiteSpace(c))
                {
                    return false;
                }

                return c == "D" ? true : false;
            }
        }

        public bool IsAdmissionStatusNTUC
        {
            get
            {
                var c = AdmissionStatusCode;
                if (string.IsNullOrWhiteSpace(c))
                {
                    return false;
                }

                return c == "N" ? true : false;
            }
        }

        public bool IsAdmissionStatusNTUCorDischarge
        {
            get
            {
                var c = AdmissionStatusCode;
                if (string.IsNullOrWhiteSpace(c))
                {
                    return false;
                }

                return IsAdmissionStatusDischarge || IsAdmissionStatusNTUC ? true : false;
            }
        }

        public bool AgencyDischargeVersion2OrHigher
        {
            get
            {
                if (DischargeVersion < 2)
                {
                    return false;
                }

                return AgencyDischarge ? true : false;
            }
        }

        public bool EnableDischargeReason
        {
            get
            {
                if (ReasonDCKey == (int)CodeLookupCache.GetKeyFromCode("REASONDC", "DIED"))
                {
                    return false;
                }

                return AgencyDischarge;
            }
        }

        public bool ShowFollowAdmissionPhysician
        {
            get
            {
                if (ReasonDCKey == (int)CodeLookupCache.GetKeyFromCode("REASONDC", "DIED"))
                {
                    return false;
                }

                return AgencyDischarge;
            }
        }

        public bool AdmissionDisciplineIsHCFACode(string IsHCFACode)
        {
            if (string.IsNullOrWhiteSpace(IsHCFACode))
            {
                return false;
            }

            var myHCFACode = AdmissionDisciplineHCFACode;
            if (string.IsNullOrWhiteSpace(myHCFACode))
            {
                return false;
            }

            return myHCFACode.ToUpper() == IsHCFACode.ToUpper() ? true : false;
        }

        partial void OnDisciplineAdmitDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (SOCandReferBeforeGoLiveDateOrEditableTeleHealth)
            {
                UpdateAdmissionStatus();
            }

            if (DisciplineEvalServiceTypeOptional)
            {
                ReferDateTime = DisciplineAdmitDateTime;
                UpdateAdmissionStatus();
            }

            RaisePropertyChanged("ReferDateTime");
            RaisePropertyChanged("TelehealthDischargeVisible");
            RaisePropertyChanged("CanSeeDischargeDateLabel");
            RaisePropertyChanged("AdmissionStatusText");

            // coordinate changes between AdmissionStatus and associated EncounterResumption
            if (EncounterResumption != null)
            {
                EncounterResumption.ResumptionDate = DisciplineAdmitDateTime.HasValue
                    ? DisciplineAdmitDateTime.Value.Date
                    : DisciplineAdmitDateTime;
            }
        }

        partial void OnReferDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (SOCandReferBeforeGoLiveDateOrEditableTeleHealth)
            {
                UpdateAdmissionStatus();
            }

            RaisePropertyChanged("AdmissionStatusText");
            RaisePropertyChanged("CanEditAdmitDateTime");
        }

        partial void OnNotTakenDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (InSaveAdmissionStatusData)
            {
                return;
            }

            if (NotTakenDateTime.HasValue && AdmissionStatusCode != "N")
            {
                AdmissionStatus = (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "N");
            }

            // if the NotTakenDate is removed, the reason needs to be removed also.
            if (!NotTakenDateTime.HasValue && !string.IsNullOrEmpty(NotTakenReason))
            {
                NotTakenReason = null;
            }

            // if we don't have any not taken data, try setting the status back.
            if (!NotTakenDateTime.HasValue && string.IsNullOrEmpty(NotTakenReason))
            {
                if (IsNew)
                {
                    AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Referred;
                }
                else
                {
                    var orig = GetOriginalRow();
                    if (orig != null)
                    {
                        AdmissionStatus = orig.AdmissionStatus;
                    }
                }
            }

            if (SOCandReferBeforeGoLiveDateOrEditableTeleHealth)
            {
                UpdateAdmissionStatus();
            }

            RaisePropertyChanged("AdmissionStatusText");
        }

        private void UpdateAdmissionStatus()
        {
            if (NotTakenDateTime.HasValue)
            {
                AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_NotTaken;
            }
            else if (DisciplineAdmitDateTime.HasValue)
            {
                AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Admitted;
            }
            else
            {
                AdmissionStatus = AdmissionStatusHelper.AdmissionStatus_Referred;
            }
        }

        partial void OnAdmissionStatusChanged()
        {
            _AdmissionStatusCode = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("Admitted");
            RaisePropertyChanged("CanSeeNotTakenLabel");
            RaisePropertyChanged("CanSeeNotTakenText");
            RaisePropertyChanged("CanEditNotTaken");
            RaisePropertyChanged("CanEditNotTakenReason");
        }

        private AdmissionDiscipline GetOriginalRow()
        {
            if (OriginalAdmissionDisciplineRow == null)
            {
                OriginalAdmissionDisciplineRow = (AdmissionDiscipline)GetOriginal();
            }

            return OriginalAdmissionDisciplineRow;
        }

        partial void OnDischargeReasonKeyChanged()
        {
            _DischargeReasonCode = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DischargeReasonCode");
            RaisePropertyChanged("DischargeReasonCodeDescription");
            RaisePropertyChanged("EnableDischargeReason");
            RaisePropertyChanged("ShowFollowAdmissionPhysician");
            Messenger.Default.Send(this,
                string.Format("FormAdmissionDisciplineChanged{0}", AdmissionKey.ToString().Trim()));
        }

        partial void OnDischargeDateTimeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            Messenger.Default.Send(this,
                string.Format("OasisAdmissionDisciplineChanged{0}", AdmissionKey.ToString().Trim()));
        }

        public string GetDischargeDateAddendum(TenantSettingsCache tenantSettingCache = null)
        {
            var prevAdmissionDiscipline = GetOriginalRow();
            var prevDischargeDateTime = prevAdmissionDiscipline.DischargeDateTime;
            var logMsg = string.Empty;
            if (DischargeDateTime != prevDischargeDateTime)
            {
                var origDischargeDate = prevDischargeDateTime == null
                    ? _none
                    : ((DateTime)prevDischargeDateTime).ToString("MM/dd/yyyy");
                var newDischargeDate = DischargeDateTime == null
                    ? _none
                    : ((DateTime)DischargeDateTime).ToString("MM/dd/yyyy");
                logMsg = string.Format("Discharge Date changed from {0} to {1}", origDischargeDate, newDischargeDate);
            }

            return logMsg;
        }

        public string GetAdmissionStatus(TenantSettingsCache tenantSettingCache = null)
        {
            var prevAdmissionDiscipline = GetOriginalRow();
            var prevAdmissionStatus = prevAdmissionDiscipline.AdmissionStatus;
            var logMsg = string.Empty;
            if (AdmissionStatus != prevAdmissionStatus)
            {
                var origAdmissionStatus = prevAdmissionStatus == null
                    ? _none
                    : CodeLookupCache.GetDescriptionFromCode("AdmissionStatus",
                        CodeLookupCache.GetCodeFromKey(prevAdmissionStatus));
                var newAdmissionStatus = AdmissionStatus == null
                    ? _none
                    : CodeLookupCache.GetDescriptionFromCode("AdmissionStatus",
                        CodeLookupCache.GetCodeFromKey(AdmissionStatus));
                logMsg = string.Format("Admission Status changed from '{0}' to '{1}'", origAdmissionStatus,
                    newAdmissionStatus);
            }

            return logMsg;
        }

        partial void OnAgencyDischargeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EnableDischargeReason");
            RaisePropertyChanged("ShowFollowAdmissionPhysician");
            RaisePropertyChanged("AgencyDischargeVersion2OrHigher");
            if (AgencyDischarge == false)
            {
                PostDischargeGoals = null;
            }

            if (AgencyDischarge == false)
            {
                PostDischargeTreatmentPreferences = null;
            }

            Messenger.Default.Send(this,
                string.Format("AdmissionDisciplineAgencyDischargeChanged{0}",
                    AdmissionDisciplineKey.ToString().Trim()));
        }

        partial void OnDischargeVersionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("AgencyDischargeVersion2OrHigher");
        }

        partial void OnReasonDCKeyChanged()
        {
            _ReasonDCCode = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EnableDischargeReason");
            RaisePropertyChanged("ShowFollowAdmissionPhysician");
        }
    }

    public partial class AdmissionDisciplineFrequency
    {
        public string HCFACode
        {
            get
            {
                var code = DisciplineCache.GetDisciplineFromKey(DisciplineKey);
                if (code != null)
                {
                    return code.HCFACode;
                }

                return string.Empty;
            }
        }

        public bool IsPRN_Client => CycleCode == "ASNEEDED";

        public string DisciplineDescription => DisciplineCache.GetDescriptionFromKey(DisciplineKey);

        public string DisplayDisciplineFrequencyText
        {
            get
            {
                if (FrequencyMax == null && string.IsNullOrEmpty(Purpose) && DisciplineFrequencyKey <= 0)
                {
                    return "(" + DisciplineCache.GetDescriptionFromKey(DisciplineKey) + ") - <New Frequency>";
                }

                return DisciplineCache.GetDescriptionFromKey(DisciplineKey) + " - " + FormatFCDText()
                       + string.Format(" : {0} - {1}",
                           StartDate == null ? "" : StartDate.Value.Date.ToString("MM/dd/yyyy"),
                           EndDate == null ? "" : EndDate.Value.Date.ToString("MM/dd/yyyy"));
            }
        }

        public string DisplayDisciplineFrequencyTextNoDates
        {
            get
            {
                if (FrequencyMax == null && string.IsNullOrEmpty(Purpose) && DisciplineFrequencyKey <= 0)
                {
                    return "(" + DisciplineCache.GetDescriptionFromKey(DisciplineKey) + ") - <New Frequency>";
                }

                return DisciplineCache.GetDescriptionFromKey(DisciplineKey) + " " + FormatFCDText();
            }
        }

        public string DisplayDisciplineFrequencyTextShort
        {
            get
            {
                if (FrequencyMax == null && string.IsNullOrEmpty(Purpose) && DisciplineFrequencyKey <= 0)
                {
                    return "(" + DisciplineCache.GetDescriptionFromKey(DisciplineKey) + ") - <New Frequency>";
                }

                return FormatFCDText();
            }
        }

        private bool DisciplineAllowsHours
        {
            get
            {
                var disc = DisciplineCache.GetDisciplineFromKey(DisciplineKey);
                if (disc != null && disc.AllowHoursInOrders)
                {
                    return true;
                }

                return false;
            }
        }

        public bool HoursVisible
        {
            get
            {
                if (Hours > 0)
                {
                    return true;
                }

                if (DisciplineAllowsHours)
                {
                    return true;
                }

                return false;
            }
        }

        public int OriginatingDisciplineFrequencyKey { get; set; }

        partial void OnHoursChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RefreshDisplayStrings();
        }

        partial void OnFrequencyMinChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RefreshDisplayStrings();
        }

        partial void OnFrequencyMaxChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RefreshDisplayStrings();
        }

        partial void OnCycleNumberChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RefreshDisplayStrings();
        }

        partial void OnCycleCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RefreshDisplayStrings();
        }

        partial void OnDurationNumberChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RefreshDisplayStrings();
        }

        partial void OnDurationCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RefreshDisplayStrings();
        }

        partial void OnStartDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RefreshDisplayStrings();
        }

        partial void OnEndDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RefreshDisplayStrings();
        }

        partial void OnDisciplineFrequencyKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RefreshDisplayStrings();
        }

        private void RefreshDisplayStrings()
        {
            RaisePropertyChanged("DisplayDisciplineFrequencyText");
            RaisePropertyChanged("DisplayDisciplineFrequencyTextShort");
            RaisePropertyChanged("DisplayDisciplineFrequencyTextNoDates");
        }

        partial void OnDisciplineKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (!DisciplineAllowsHours && IsNew)
            {
                Hours = null;
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                RaisePropertyChanged("DisplayDisciplineFrequencyText");
                RaisePropertyChanged("DisplayDisciplineFrequencyTextShort");
                RaisePropertyChanged("DisciplineDescription");
                RaisePropertyChanged("HoursVisible");
            });
        }

        private string FormatFCDText()
        {
            var retText = "";
            // For now all we support is 'Encounter' ... can change to parameter later if necessary
            var FreqUnit = "Encounter";

            // HOURS
            if (HoursVisible && Hours.HasValue)
            {
                retText = retText + Hours + " Hours x ";
            }

            // RANGE
            if (FrequencyMin.HasValue && FrequencyMin > 0 && FrequencyMin != FrequencyMax)
            {
                retText = retText + FrequencyMin + " to ";
            }

            if (FrequencyMax.HasValue && FrequencyMax > 0)
            {
                retText = retText + FrequencyMax;
                if (!string.IsNullOrEmpty(FreqUnit))
                {
                    retText = retText + " " + FreqUnit;
                    if (FrequencyMax > 1)
                    {
                        retText = retText + "s";
                    }
                }
            }

            if (!string.IsNullOrEmpty(retText)
                && CycleCode != "BID" && CycleCode != "TID" && CycleCode != "QID")
            {
                retText = retText + " ";
            }

            // CYCLE
            if (!string.IsNullOrEmpty(CycleCode))
            {
                if (CycleCode != "ASNEEDED")
                {
                    if (CycleCode == "BID")
                    {
                        retText = retText + " Twice ";
                    }

                    if (CycleCode == "TID")
                    {
                        retText = retText + " Three Times";
                    }

                    if (CycleCode == "QID")
                    {
                        retText = retText + " Four Times";
                    }

                    retText = retText + "Every";

                    var CycleUnitsDscr = CycleCode.Substring(0, 1).ToUpper() +
                                         CycleCode.Substring(1, CycleCode.Length - 1).ToLower();
                    if (CycleCode == "BID" || CycleCode == "TID" || CycleCode == "QID")
                    {
                        CycleUnitsDscr = "Day";
                    }

                    if (CycleCode == "OFTHEMONTH")
                    {
                        CycleUnitsDscr = "of the Month";
                    }

                    if (CycleNumber.HasValue && CycleNumber > 0)
                    {
                        var CyclePiece = CycleCode + "th";
                        if (CycleNumber == 1)
                        {
                            CyclePiece = "";
                        }
                        else if (CycleNumber == 2)
                        {
                            CyclePiece = "2nd";
                        }
                        else if (CycleNumber == 3)
                        {
                            CyclePiece = "3rd";
                        }
                        else
                        {
                            CyclePiece = CycleNumber + "th";
                        }


                        if (!string.IsNullOrEmpty(CyclePiece))
                        {
                            retText = retText + " " + CyclePiece;
                        }

                        retText = retText + " " + CycleUnitsDscr;
                    }
                }
                else
                {
                    retText = retText + "As Needed";
                }
            }

            // DURATION
            var DurUnitDscr = "";
            //--This assumes that the duration units are all plurals where the simple plural rule applies (add an "s")
            if (!string.IsNullOrEmpty(DurationCode))
            {
                DurUnitDscr = DurationCode.Substring(0, 1).ToUpper() +
                              DurationCode.Substring(1, DurationCode.Length - 1).ToLower();
                if (DurationNumber.HasValue && DurationNumber > 0)
                {
                    if (!string.IsNullOrEmpty(retText))
                    {
                        retText = retText + " For";
                    }

                    retText = retText + " " + DurationNumber + " " + DurUnitDscr;
                    if (DurationNumber > 1)
                    {
                        retText = retText + "s";
                    }
                }
            }

            //--Just in case, may not need
            retText.TrimStart().TrimEnd();

            return retText;
        }

        public AdmissionDisciplineFrequency CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newadf = (AdmissionDisciplineFrequency)Clone(this);

            OfflineIDGenerator.Instance.SetKey(newadf);
            newadf.OriginatingDisciplineFrequencyKey = DisciplineFrequencyKey;

            if (newadf.HistoryKey == null)
            {
                newadf.HistoryKey = DisciplineFrequencyKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newadf;
        }

        #region EDIT FCD Outside of an Encounter

        // This is used to interrogate whether or not the Encounter being maintained is the Encounter the adf belongs to.
        public Encounter DynamicFormEncounter { get; set; }

        public bool ShowWarningAsterisk
        {
            get
            {
                var showast = false;
                if (Admission == null)
                {
                    return false;
                }

                if (DynamicFormEncounter == null && Encounter == null)
                {
                    showast = false;
                }
                else if (DynamicFormEncounter == null && IsOnUnsignedEncounter)
                {
                    showast = true;
                }
                else if (DynamicFormEncounter != null &&
                         (DisciplineKey ==
                          (int)ServiceTypeCache.GetDisciplineKey((int)DynamicFormEncounter.ServiceTypeKey)
                          || Admission.CareCoordinator == UserCache.Current.GetCurrentUserProfile().UserId))
                {
                    var f = DynamicFormEncounter.FormKey == null
                        ? null
                        : DynamicFormCache.GetFormByKey((int)DynamicFormEncounter.FormKey);
                    if (f != null && DynamicFormEncounter.EncounterKey != AddedFromEncounterKey && (f.IsEval ||
                            f.IsResumption || f.IsTeamMeeting || f.IsOrderEntry || DynamicFormEncounter.HasVO))
                    {
                        showast = true;
                    }
                }

                return showast && !CanEditVisitFrequencyCompleted;
            }
        }

        public bool IsOnUnsignedEncounter =>
            Encounter == null ? false : Encounter.EncounterStatus != (int)EncounterStatusType.Completed;

        public bool IsOrderEntry => DynamicFormEncounter != null && DynamicFormEncounter.FormKey != null &&
                                    DynamicFormCache.GetFormByKey((int)DynamicFormEncounter.FormKey).IsOrderEntry;

        public bool IsPlanOfCare => DynamicFormEncounter != null && DynamicFormEncounter.FormKey != null &&
                                    DynamicFormCache.GetFormByKey((int)DynamicFormEncounter.FormKey).IsPlanOfCare;

        public bool IsTeamMeeting => DynamicFormEncounter != null && DynamicFormEncounter.FormKey != null &&
                                     DynamicFormCache.GetFormByKey((int)DynamicFormEncounter.FormKey).IsTeamMeeting;

        public bool IsEvalOrResumption
        {
            get
            {
                if (DynamicFormEncounter == null)
                {
                    return false;
                }

                if (DynamicFormEncounter.FormKey == null)
                {
                    return false;
                }

                var f = DynamicFormCache.GetFormByKey((int)DynamicFormEncounter.FormKey);
                if (f == null)
                {
                    return false;
                }

                return f.IsEval || f.IsResumption;
            }
        }

        public bool IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry => IsPlanOfCare || IsTeamMeeting || IsOrderEntry;

        public bool CanEditVisitFrequencyCompleted
        {
            // Used to display asterick when editting visit frequencies that come from an unsigned encounter
            // the button is allowing them to edit but we still need a marker to s how the asterick
            get
            {
                if (Admission == null)
                {
                    return false;
                }

                // We are NOT in an encounter
                if (DynamicFormEncounter == null)
                {
                    return false; // Can only edit now from Order Entry/POC/Team Meeting. 06/24/2013
                }

                return AddedFromEncounterKey != DynamicFormEncounter.EncounterKey && Encounter != null
                    && Encounter.EncounterStatus ==
                    (int)EncounterStatusType
                        .Completed // Ability to edit a visit freqency order before encounter where created is complete.
                    && (DisciplineKey ==
                        (int)ServiceTypeCache.GetDisciplineKey((int)DynamicFormEncounter.ServiceTypeKey) ||
                        Admission.CareCoordinator == UserCache.Current.GetCurrentUserProfile().UserId)
                    && (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry || DynamicFormEncounter.HasVO || IsEvalOrResumption);
            }
        }

        public bool CanEditVisitFrequency
        {
            get
            {
                if (Admission == null)
                {
                    return false;
                }

                // We are NOT in an encounter
                if (DynamicFormEncounter == null)
                {
                    return false; // Can only edit now from Order Entry/POC/Team Meeting. 06/24/2013
                }

                // If we are on a POC or Order, you can edit or add anything.
                if (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry)
                {
                    return true;
                }

                if (!(IsOnUnsignedEncounter && AddedFromEncounterKey != DynamicFormEncounter.EncounterKey) &&
                    (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry || DynamicFormEncounter.HasVO))
                {
                    return true;
                }

                // It was added during this encounter
                if (AddedFromEncounterKey == DynamicFormEncounter.EncounterKey)
                {
                    return true;
                }

                // Was added outside an encounter and either matches the encounter discipline, or the user is a care coordinator
                if (AddedFromEncounterKey == null && (DisciplineKey ==
                                                      (int)ServiceTypeCache.GetDisciplineKey(
                                                          (int)DynamicFormEncounter.ServiceTypeKey)
                                                      || Admission.CareCoordinator ==
                                                      UserCache.Current.GetCurrentUserProfile().UserId) &&
                    (IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry || DynamicFormEncounter.HasVO || IsEvalOrResumption))
                {
                    return true;
                }

                // Was added inside an encounter, the encounter is signed and the discipline either matches the encounter discipline, or the user is a care coordinator
                if (AddedFromEncounterKey != DynamicFormEncounter.EncounterKey && Encounter != null
                                                                               && (DisciplineKey ==
                                                                                   (int)ServiceTypeCache
                                                                                       .GetDisciplineKey(
                                                                                           (int)DynamicFormEncounter
                                                                                               .ServiceTypeKey) ||
                                                                                   Admission.CareCoordinator ==
                                                                                   UserCache.Current
                                                                                       .GetCurrentUserProfile().UserId)
                                                                               && (
                                                                                   IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry ||
                                                                                   DynamicFormEncounter.HasVO ||
                                                                                   IsEvalOrResumption))
                {
                    return true;
                }

                //otherwise I Can't edit the order
                return false;
            }
        }

        public void MyRejectChanges()
        {
            RejectChanges();
        }

        #endregion
    }

    public partial class ADFVendorResponse
    {
        public string Vendor
        {
            get
            {
                var vs = UserCache.Current.GetUsers().Where(x => x.ExternalID == VendorID);
                if (vs.Any() == false)
                {
                    return "Unkown Vendor  #" + VendorID;
                }

                if (vs.Count() > 1)
                {
                    return "Duplicate UserProfile #" + VendorID;
                }

                return vs.First().FriendlyName;
            }
        }

        public string HoursText
        {
            get
            {
                if (!Hours.HasValue || Hours == 0)
                {
                    return string.Empty;
                }

                return Hours + " hrs";
            }
        }


        public int Count { get; set; }
    }

    public class AdmissionPhysicianFacade : ViewModelBase, INotifyDataErrorInfo
    {
        private Admission _Admission;

        private Encounter _Encounter;

        private List<ValidationResult> _validationErrors;
        private readonly bool UseEncounterAdmission = true;

        public AdmissionPhysicianFacade(bool useEncounterAdmission = true)
        {
            UseEncounterAdmission = useEncounterAdmission;
        }

        public Admission Admission
        {
            get { return _Admission; }
            set
            {
                Set(() => Admission, ref _Admission, value);
                RaisePropertyChanged(null);
            }
        }

        public Encounter Encounter
        {
            get { return _Encounter; }
            set
            {
                Set(() => Encounter, ref _Encounter, value);
                RaisePropertyChanged(null);
            }
        }

        public List<ValidationResult> ValidationErrors
        {
            get
            {
                if (_validationErrors == null)
                {
                    _validationErrors = new List<ValidationResult>();
                }

                return _validationErrors;
            }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            IEnumerable rval;
            if (!string.IsNullOrEmpty(propertyName))
            {
                rval = ValidationErrors.Where(e =>
                {
                    if (e.MemberNames.Any())
                    {
                        return e.MemberNames.Contains(propertyName);
                    }

                    return true;
                }).ToArray();
            }
            else
            {
                rval = ValidationErrors.Where(e => e.MemberNames.Contains(propertyName)).ToArray();
            }

            return rval;
        }

        public bool HasErrors => ValidationErrors.Any();

        public void RaiseEvents()
        {
            RaisePropertyChanged(null);
        }

        public Physician CurrentPhysicianByType(string physicianCode)
        {
            var physicianCodeType = "PHTP";

            if (Admission == null)
            {
                return null;
            }

            var _serviceDate = Encounter != null && Encounter.EncounterStartDate.HasValue
                ? Encounter.EncounterStartDate.Value.Date
                : DateTime.Now.Date;

            var ap = Admission.AdmissionPhysician
                .Where(p => p.Inactive == false)
                .Where(p => p.PhysicianType == CodeLookupCache.GetKeyFromCode(physicianCodeType, physicianCode)
                    .GetValueOrDefault())
                .Where(p =>
                    p.PhysicianEffectiveFromDate.Date <= _serviceDate &&
                    (p.PhysicianEffectiveThruDate.HasValue == false ||
                     p.PhysicianEffectiveThruDate.Value.Date >= _serviceDate)
                ).FirstOrDefault();

            if (ap != null)
            {
                return PhysicianCache.Current.GetPhysicianFromKey(ap.PhysicianKey);
            }

            return null;
        }

        public void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }


        #region "Physician Objects"

        public Physician AttendingPhysician => CurrentPhysicianByType("PCP");

        public Physician AdmittingPhysician => CurrentPhysicianByType("Admit");

        public Physician AlternatePhysician => CurrentPhysicianByType("Alt");

        public Physician ConsultingPhysician => CurrentPhysicianByType("Cons");

        public Physician EmergencyPhysician => CurrentPhysicianByType("Emerg");

        public Physician MedicalDirector => CurrentPhysicianByType("MedDirect");

        public Physician PhyschiatricPhysician => CurrentPhysicianByType("Psych");

        public Physician ReferringPhysician => CurrentPhysicianByType("Refer");

        public Physician Specialist => CurrentPhysicianByType("Spec");

        #endregion

        #region "Physician Object Keys"

        //Attending/Primary Care Physician
        public int? AttendingPhysicianKey
        {
            get
            {
                if (Encounter == null)
                {
                    return null;
                }

                var currentEncounterAdmission = Encounter.EncounterAdmission.FirstOrDefault();
                if (currentEncounterAdmission != null && UseEncounterAdmission &&
                    Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return currentEncounterAdmission.AttendingPhysicianKey;
                }

                if (AttendingPhysician != null)
                {
                    if (currentEncounterAdmission != null && UseEncounterAdmission &&
                        Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed &&
                        currentEncounterAdmission.AttendingPhysicianKey == null)
                    {
                        currentEncounterAdmission.AttendingPhysicianKey = AttendingPhysician.PhysicianKey;
                    }

                    return AttendingPhysician.PhysicianKey;
                }

                return null;
            }
        }

        public int? AttendingPhysicianAddressKey
        {
            get
            {
                if (Encounter == null)
                {
                    return null;
                }

                var currentEncounterAdmission = Encounter.EncounterAdmission.FirstOrDefault();
                if (currentEncounterAdmission != null && UseEncounterAdmission &&
                    Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return currentEncounterAdmission.AttendingPhysicianAddressKey;
                }

                if (SigningAdmissionPhysician != null)
                {
                    if (currentEncounterAdmission != null
                        && UseEncounterAdmission
                        && Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed
                        && currentEncounterAdmission.SigningPhysicianAddressKey == null)
                    {
                        currentEncounterAdmission.AttendingPhysicianAddressKey =
                            AttendingAdmissionPhysician.PhysicianAddressKey;
                    }

                    return AttendingAdmissionPhysician.PhysicianAddressKey;
                }

                return null;
            }
        }

        public int? OverrideSigningPhysicianKey { get; set; }

        public int? OverrideAttendingPhysicianKey { get; set; }

        public int? ReferringPhysicianKey
        {
            get
            {
                if (ReferringPhysician != null)
                {
                    return ReferringPhysician.PhysicianKey;
                }

                return null;
            }
        }

        public int? SigningPhysicianKey
        {
            get
            {
                if (Encounter == null)
                {
                    return null;
                }

                var currentEncounterAdmission = Encounter.EncounterAdmission.FirstOrDefault();
                if (currentEncounterAdmission != null && UseEncounterAdmission &&
                    Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return currentEncounterAdmission.SigningPhysicianKey;
                }

                if (SigningPhysician != null)
                {
                    if (currentEncounterAdmission != null && UseEncounterAdmission &&
                        Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed &&
                        currentEncounterAdmission.SigningPhysicianKey == null)
                    {
                        currentEncounterAdmission.SigningPhysicianKey = SigningPhysician.PhysicianKey;
                    }

                    return SigningPhysician.PhysicianKey;
                }

                return null;
            }
        }

        public int? SigningPhysicianAddressKey
        {
            get
            {
                if (Encounter == null)
                {
                    return null;
                }

                var currentEncounterAdmission = Encounter.EncounterAdmission.FirstOrDefault();
                if (currentEncounterAdmission != null && UseEncounterAdmission &&
                    Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                {
                    return currentEncounterAdmission.SigningPhysicianAddressKey;
                }

                if (SigningAdmissionPhysician != null)
                {
                    if (currentEncounterAdmission != null && UseEncounterAdmission &&
                        Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed &&
                        currentEncounterAdmission.SigningPhysicianAddressKey == null)
                    {
                        currentEncounterAdmission.SigningPhysicianAddressKey =
                            SigningAdmissionPhysician.PhysicianAddressKey;
                    }

                    return SigningAdmissionPhysician.PhysicianAddressKey;
                }

                return null;
            }
        }

        public Physician SigningPhysician
        {
            get
            {
                if (Admission != null && Encounter != null)
                {
                    var currentEncounterAdmission = Encounter.EncounterAdmission.FirstOrDefault();
                    if (currentEncounterAdmission != null && UseEncounterAdmission &&
                        Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                    {
                        return PhysicianCache.Current.GetPhysicianFromKey(currentEncounterAdmission
                            .SigningPhysicianKey);
                    }

                    if (OverrideSigningPhysicianKey != null)
                    {
                        return PhysicianCache.Current.GetPhysicianFromKey(OverrideSigningPhysicianKey);
                    }

                    var _serviceDate = Encounter != null && Encounter.EncounterStartDate.HasValue
                        ? Encounter.EncounterStartDate.Value.Date
                        : DateTime.Now.Date;
                    var ap = Admission.AdmissionPhysician
                        .Where(p => p.Inactive == false)
                        .Where(p => p.Signing)
                        .Where(p =>
                            p.SigningEffectiveFromDate.HasValue &&
                            p.SigningEffectiveFromDate.Value.Date <= _serviceDate &&
                            (!p.SigningEffectiveThruDate.HasValue ||
                             p.SigningEffectiveThruDate.Value.Date >= _serviceDate)
                        ).FirstOrDefault();
                    if (ap != null)
                    {
                        return ap.Physician;
                    }

                    return null;
                }

                return null;
            }
        }

        public AdmissionPhysician CurrentCertifyingAdmissionPhysician
        {
            get
            {
                var today = DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified).Date;
                return Admission.AdmissionPhysician
                    .Where(p => p.Inactive == false)
                    .Where(p =>
                        p.SigningEffectiveFromDate.HasValue && p.SigningEffectiveFromDate.Value.Date <= today &&
                        (!p.SigningEffectiveThruDate.HasValue || p.SigningEffectiveThruDate.Value.Date >= today))
                    .Where(p =>
                        p.PhysicianEffectiveFromDate.Date <= today &&
                        (p.PhysicianEffectiveThruDate.HasValue == false ||
                         p.PhysicianEffectiveThruDate.Value.Date >= today))
                    .OrderByDescending(p => p.PhysicianEffectiveFromDate).FirstOrDefault();
            }
        }

        public AdmissionPhysician SigningAdmissionPhysician
        {
            get
            {
                if (SigningPhysician != null)
                {
                    int? addressKey = null;
                    var physKey = OverrideSigningPhysicianKey == null
                        ? SigningPhysicianKey
                        : OverrideSigningPhysicianKey;
                    var currentEncounterAdmission = Encounter.EncounterAdmission.FirstOrDefault();
                    if (currentEncounterAdmission != null && UseEncounterAdmission &&
                        Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                    {
                        addressKey = currentEncounterAdmission.SigningPhysicianAddressKey;
                    }

                    var ap = Admission.AdmissionPhysician
                        .Where(a => a.PhysicianKey == physKey && a.PhysicianAddressKey == addressKey)
                        .FirstOrDefault();
                    // not found, use the first one.
                    if (ap == null)
                    {
                        ap = Admission.AdmissionPhysician
                            .Where(a => a.PhysicianKey == physKey)
                            .FirstOrDefault();
                    }

                    // fall back if we STILL don't have one.
                    if (ap == null && SigningPhysicianKey != null)
                    {
                        ap = new AdmissionPhysician();
                        ap.PhysicianKey = (int)SigningPhysicianKey;
                        ap.PhysicianAddressKey = SigningPhysicianAddressKey;
                    }

                    return ap;
                }

                return null;
            }
        }

        public AdmissionPhysician AttendingAdmissionPhysician
        {
            get
            {
                if (AttendingPhysician != null)
                {
                    int? addressKey = null;
                    var physKey = OverrideAttendingPhysicianKey == null
                        ? AttendingPhysicianKey
                        : OverrideAttendingPhysicianKey;
                    var currentEncounterAdmission = Encounter == null || Encounter.EncounterAdmission == null
                        ? null
                        : Encounter.EncounterAdmission.FirstOrDefault();
                    if (currentEncounterAdmission != null && UseEncounterAdmission &&
                        Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                    {
                        addressKey = currentEncounterAdmission.AttendingPhysicianAddressKey;
                    }

                    var ap = Admission.AdmissionPhysician
                        .Where(a => a.PhysicianKey == physKey && a.PhysicianAddressKey == addressKey)
                        .FirstOrDefault();
                    // not found, use the first one.
                    if (ap == null)
                    {
                        ap = Admission.AdmissionPhysician
                            .Where(a => a.PhysicianKey == physKey)
                            .FirstOrDefault();
                    }

                    // fall back if we STILL don't have one.
                    if (ap == null && AttendingPhysicianKey != null)
                    {
                        ap = new AdmissionPhysician();
                        ap.PhysicianKey = (int)AttendingPhysicianKey;
                        ap.PhysicianAddressKey = AttendingPhysicianAddressKey;
                    }

                    // fall back further...
                    if (ap == null)
                    {
                        var today = DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified).Date;
                        ap = Admission.AdmissionPhysician
                            .Where(p => p.Inactive == false)
                            .Where(p => p.PhysicianType ==
                                        CodeLookupCache.GetKeyFromCode("PHTP", "PCP").GetValueOrDefault())
                            .Where(p =>
                                p.PhysicianEffectiveFromDate.Date <= today &&
                                (p.PhysicianEffectiveThruDate.HasValue == false ||
                                 p.PhysicianEffectiveThruDate.Value.Date >= today)
                            ).OrderByDescending(p => p.PhysicianEffectiveFromDate).FirstOrDefault();
                    }

                    // and further...
                    if (ap == null)
                    {
                        ap = Admission.AdmissionPhysician
                            .Where(p => p.Inactive == false)
                            .Where(p => p.PhysicianType ==
                                        CodeLookupCache.GetKeyFromCode("PHTP", "PCP").GetValueOrDefault())
                            .OrderByDescending(p => p.PhysicianEffectiveFromDate).FirstOrDefault();
                    }

                    return ap;
                }

                return null;
            }
        }

        public AdmissionPhysician FollowAdmissionPhysician
        {
            get
            {
                if (Admission == null || Admission.AdmissionPhysician == null)
                {
                    return null;
                }

                var ap = Admission.AdmissionPhysician.Where(p => p.Inactive == false && p.PhysicianType == 1)
                    .FirstOrDefault();
                return ap;
            }
        }

        #endregion


        #region "Physician Collections"

        public ICollection<AdmissionPhysician> ActiveAdmissionPhysicians
        {
            get
            {
                if (Admission.AdmissionPhysician == null)
                {
                    return null;
                }

                var _serviceDate = Encounter != null && Encounter.EncounterStartDate.HasValue
                    ? Encounter.EncounterStartDate.Value.Date
                    : DateTime.Now.Date;
                var ap = Admission.AdmissionPhysician
                    .Where(p => p.Inactive == false && p.HistoryKey == null)
                    .Where(p =>
                        p.PhysicianEffectiveFromDate != null && p.PhysicianEffectiveFromDate.Date <= _serviceDate &&
                        (p.PhysicianEffectiveThruDate == null || p.PhysicianEffectiveThruDate.Value.Date > _serviceDate)
                    ).ToList();
                // Add inactive rows which are attatched to our Encounter
                if (SigningPhysicianKey != null && SigningPhysicianAddressKey != null && !ap.Where(sp =>
                            sp.PhysicianKey == SigningPhysicianKey &&
                            sp.PhysicianAddressKey == SigningPhysicianAddressKey)
                        .Any())
                {
                    var sphy = Admission.AdmissionPhysician.Where(aap =>
                        aap.PhysicianKey == SigningPhysicianKey &&
                        aap.PhysicianAddressKey == SigningPhysicianAddressKey).FirstOrDefault();
                    if (sphy != null)
                    {
                        ap.Add(sphy);
                    }
                }

                if (SigningPhysicianKey != null && SigningPhysicianAddressKey == null && !ap.Where(sp =>
                        sp.PhysicianKey == SigningPhysicianKey && SigningPhysicianAddressKey == null).Any())
                {
                    var aprow = Admission.AdmissionPhysician
                        .Where(aap => aap.PhysicianKey == SigningPhysicianKey && aap.PhysicianAddressKey == null)
                        .FirstOrDefault();
                    if (aprow == null)
                    {
                        aprow = Admission.AdmissionPhysician.Where(aap => aap.PhysicianKey == SigningPhysicianKey)
                            .FirstOrDefault();
                    }

                    if (aprow != null)
                    {
                        ap.Add(aprow);
                    }
                }

                if (AttendingPhysicianKey != null && !ap.Where(sp => sp.PhysicianKey == AttendingPhysicianKey).Any())
                {
                    var aprow2 = Admission.AdmissionPhysician.Where(aap => aap.PhysicianKey == AttendingPhysicianKey)
                        .FirstOrDefault();
                    if (aprow2 != null)
                    {
                        ap.Add(aprow2);
                    }
                }

                if (OverrideSigningPhysicianKey != null &&
                    !ap.Where(sp => sp.PhysicianKey == OverrideSigningPhysicianKey).Any())
                {
                    var sphy2 = Admission.AdmissionPhysician
                        .Where(aap => aap.PhysicianKey == OverrideSigningPhysicianKey).FirstOrDefault();
                    if (sphy2 != null)
                    {
                        ap.Add(sphy2);
                    }
                }

                if (SigningPhysicianKey != null &&
                    !ap.Any(p =>
                        p.PhysicianKey == SigningPhysicianKey)) // the physician no longer exists on the admission
                {
                    var apnew = new AdmissionPhysician();
                    apnew.PhysicianKey = (int)SigningPhysicianKey;
                    apnew.PhysicianAddressKey = SigningPhysicianAddressKey;
                    ap.Add(apnew);
                }

                if (Encounter != null && Encounter.EncounterPlanOfCare != null)
                {
                    var ep = Encounter.EncounterPlanOfCare.FirstOrDefault();
                    if (ep != null && ep.SigningMedicalDirectorKey != null)
                    {
                        var sphymd = Admission.AdmissionPhysician
                            .Where(aap => aap.AdmissionPhysicianKey == ep.SigningMedicalDirectorKey).FirstOrDefault();
                        if (sphymd != null)
                        {
                            ap.Add(sphymd);
                        }
                    }
                }

                return ap;
            }
        }

        public ICollection<PhysicianPhone> AttendingPhysicianActivePhones
        {
            get
            {
                if (AttendingPhysician != null)
                {
                    if (AttendingPhysician.PhysicianPhone == null)
                    {
                        return null;
                    }

                    var pp = AttendingPhysician.PhysicianPhone.Where(p => p.Inactive == false)
                        .OrderBy(p => p.TypeDescription).ToList();
                    if (pp == null)
                    {
                        return null;
                    }

                    return pp.Any() == false ? null : pp;
                }

                return null;
            }
        }

        public ICollection<PhysicianPhone> SigningPhysicianActivePhones
        {
            get
            {              
                if (SigningPhysician != null)
                {
                    if (SigningPhysician.PhysicianPhone == null)
                    {
                        return null;
                    }

                    var pp = SigningPhysician.PhysicianPhone.Where(p => p.Inactive == false)
                        .OrderBy(p => p.TypeDescription).ToList();
                    if (pp == null)
                    {
                        return null;
                    }

                    return pp.Any() == false ? null : pp;
                }

                return null;
            }
        }

        public ICollection<PhysicianEmail> SigningPhysicianActiveEmails
        {
            get
            {
                if (SigningPhysician != null)
                {
                    if (SigningPhysician.PhysicianEmail == null)
                    {
                        return null;
                    }

                    ICollection<PhysicianEmail> pe = SigningPhysician.PhysicianEmail.Where(p => p.Inactive == false)
                        .OrderBy(p => p.Recipient).ToList();
                    if (pe == null)
                    {
                        return null;
                    }

                    return pe.Any() == false ? null : pe;
                }

                return null;
            }
        }

        #endregion
    }

    public partial class AdmissionCertification
    {
        public string PeriodStartThruEndBlirb
        {
            get
            {
                var endDate = PeriodEndDate.HasValue
                    ? PeriodEndDate
                    : CalculateCertPeriodEndDate(PeriodStartDate, Units, Duration);
                return (PeriodStartDate.HasValue ? ((DateTime)PeriodStartDate).ToString("MM/dd/yyyy") : "??") +
                       " thru " + (endDate.HasValue ? ((DateTime)endDate).ToString("MM/dd/yyyy") : "??");
            }
        }

        partial void OnIsCurrentCertChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            // set all other rows to false;
            if (IsCurrentCert && Admission != null)
            {
                foreach (var ac in Admission.AdmissionCertification.Where(acc =>
                             acc.IsCurrentCert && acc.AdmissionCertKey != AdmissionCertKey))
                    ac.IsCurrentCert = false;
            }
        }

        public bool SetPeriodEndDateForRow()
        {
            if (!PeriodStartDate.HasValue)
            {
                return false;
            }

            PeriodEndDate = CalculateCertPeriodEndDate(PeriodStartDate, Units, Duration);

            return true;
        }

        public DateTime? CalculateCertPeriodEndDate(DateTime? StartDate, int? Units, int? Duration)
        {
            if (StartDate == null || !Units.HasValue || !Duration.HasValue)
            {
                return null;
            }

            var UnitCode = "DAYS";
            double doubleDuration = Duration.Value;
            DateTime? WorkEndDate = null;
            UnitCode = CodeLookupCache.GetCodeFromKey((int)Units);
            if (string.IsNullOrEmpty(UnitCode))
            {
                return null;
            }

            if (UnitCode.ToUpper() == "DAYS")
            {
                WorkEndDate = StartDate.Value.AddDays(doubleDuration);
            }
            else if (UnitCode.ToUpper() == "WEEKS")
            {
                WorkEndDate = StartDate.Value.AddDays(doubleDuration * 7);
            }
            else if (UnitCode.ToUpper() == "MONTHS")
            {
                WorkEndDate = StartDate.Value.AddMonths(Duration.Value);
            }
            else if (UnitCode.ToUpper() == "YEARS")
            {
                WorkEndDate = StartDate.Value.AddYears(Duration.Value);
            }
            else
            {
                WorkEndDate = StartDate.Value.AddDays(Duration.Value);
            }

            WorkEndDate = WorkEndDate.Value.AddDays(-1);
            return WorkEndDate;
        }
    }

    public partial class AdmissionCommunication
    {
        private int __cleanupCount;
        private CollectionViewSource _AdmissionCommunicationAllergyCollectionView;
        private CollectionViewSource _AdmissionCommunicationLabCollectionView;
        private CollectionViewSource _AdmissionCommunicationMedicationCollectionView;
        private List<PatientAllergy> _AdmissionCommunicationPatientAllergy;
        private List<PatientLab> _AdmissionCommunicationPatientLab;
        private List<PatientMedication> _AdmissionCommunicationPatientMedication;
        private bool _Expand;

        public string CommunicationTypeCode
        {
            get
            {
                var r = CodeLookupCache.GetCodeFromKey(CommunicationType);
                return r == null ? "" : r;
            }
        }

        public bool CanSurveyorViewCommunicationType
        {
            get
            {
                if (RoleAccessHelper.IsSurveyor == false)
                {
                    return true;
                }

                var code = CommunicationTypeCode.ToLower();
                if (code == "sbar" || code == "teamcasenote" || code == "carecoordinationnote" ||
                    code == "unabletomeetfcd")
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanPrint
        {
            get
            {
                if (IsNew)
                {
                    return false;
                }

                if (RoleAccessHelper.IsSurveyor)
                {
                    return false;
                }

                return true;
            }
        }

        public DateTime? WeekThrough
        {
            get
            {
                if (WeekFrom == null || WeekFrom == DateTime.MinValue)
                {
                    return null;
                }

                return ((DateTime)WeekFrom).Date.AddDays(6);
            }
        }

        public string ContactedPhysicianFullNameWithSuffix
        {
            get
            {
                var p = PhysicianCache.Current.GetPhysicianFromKey((int)ContactedPhysicianKey);
                return p == null ? "Physician name unknown" : p.FullNameWithSuffix;
            }
        }

        public string DisciplineDescription => DisciplineCache.GetDescriptionFromKey((int)DisciplineKey);

        public bool CanEdit => AdmissionCommunicationKey <= 0 ? true : false;

        public string ThumbNailText => TrimCommunicationText(126);

        public string ThumbNailTextShort => TrimCommunicationText(50);

        public string CompletedDateFormatted
        {
            get
            {
                var date = CompletedDatePart == null
                    ? ""
                    : Convert.ToDateTime(CompletedDatePart).ToString("MM/dd/yyyy");
                var time = "";
                if (CompletedTimePart != null)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        time = Convert.ToDateTime(CompletedTimePart).ToString("HHmm");
                    }
                    else
                    {
                        time = Convert.ToDateTime(CompletedTimePart).ToShortTimeString();
                    }
                }

                return date + " " + time;
            }
        }

        public string CommunicationTypeCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(CommunicationType);

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

        public string AssociateDiagnosis9AndOr10
        {
            get
            {
                var version = TenantSettingsCache.Current.TenantSettingRequiredICDVersionDate(CompletedDatePart);
                string associatedDiagnosis = null;
                // if ICD9 mode: try to use 9 first, but use 10 as a backup  -  if ICD10 mode: try to use 10 first, but use 9 as a backup
                if (version == 9)
                {
                    associatedDiagnosis = string.IsNullOrWhiteSpace(AssociatedDiagnosis)
                        ? AssociatedDiagnosis10
                        : AssociatedDiagnosis;
                }
                else
                {
                    associatedDiagnosis = string.IsNullOrWhiteSpace(AssociatedDiagnosis10)
                        ? AssociatedDiagnosis
                        : AssociatedDiagnosis10;
                }

                return string.IsNullOrWhiteSpace(associatedDiagnosis) ? null : associatedDiagnosis;
            }
        }

        public ICollectionView AdmissionCommunicationAllergyCollectionView =>
            _AdmissionCommunicationAllergyCollectionView == null
                ? null
                : _AdmissionCommunicationAllergyCollectionView.View;

        public bool AreAllergies
        {
            get
            {
                var r = AdmissionCommunicationAllergy == null ? false : AdmissionCommunicationAllergy.Any();
                return r;
            }
        }

        public ICollectionView AdmissionCommunicationMedicationCollectionView =>
            _AdmissionCommunicationMedicationCollectionView == null
                ? null
                : _AdmissionCommunicationMedicationCollectionView.View;

        public bool AreMedications
        {
            get
            {
                var r = AdmissionCommunicationMedication == null ? false : AdmissionCommunicationMedication.Any();
                return r;
            }
        }

        public ICollectionView AdmissionCommunicationLabCollectionView =>
            _AdmissionCommunicationLabCollectionView == null ? null : _AdmissionCommunicationLabCollectionView.View;

        public bool AreLabs
        {
            get
            {
                var r = AdmissionCommunicationLab == null ? false : AdmissionCommunicationLab.Any();
                return r;
            }
        }

        public void RaiseChanged()
        {
            RaisePropertyChanged("");
        }

        public bool ValidateClientUnableToMeetFCD()
        {
            var AllValid = true;
            if (CommunicationTypeCode.ToLower() != "unabletomeetfcd")
            {
                return AllValid;
            }

            // Creal non-UnableToMeetFCD fields
            NoteText = null;
            SituationText = null;
            BackgroundText = null;
            AssessmentText = null;
            RecommendationText = null;
            SOCDate = null;
            CodeStatus = null;
            VitalsText = null;
            AssociatedDiagnosis = null;
            AssociatedDiagnosis10 = null;
            CoordinationOfCare = null;
            CommunicationMode = null;
            TopicIssue = null;
            FollowupResolution = null;
            // Prep Data
            if (CompletedDatePart == null || CompletedDatePart == DateTime.MaxValue)
            {
                CompletedDatePart = null;
            }

            if (DisciplineKey <= 0)
            {
                DisciplineKey = null;
            }

            if (WeekFrom == null || WeekFrom == DateTime.MaxValue)
            {
                WeekFrom = null;
            }

            if (WeekFrom != null)
            {
                WeekFrom = ((DateTime)WeekFrom).Date;
            }

            if (VisitsPrescribed <= 0)
            {
                VisitsPrescribed = null;
            }

            if (VisitsActual < 0)
            {
                VisitsActual = null;
            }

            if (string.IsNullOrWhiteSpace(UnableToReschedule))
            {
                UnableToReschedule = null;
            }

            if (ContactedPhysicianKey <= 0)
            {
                ContactedPhysicianKey = null;
            }

            if (string.IsNullOrWhiteSpace(PhysicianNotification))
            {
                PhysicianNotification = null;
            }

            if (string.IsNullOrWhiteSpace(Narrative))
            {
                Narrative = null;
            }

            // Validate required fields
            if (CompletedDatePart == null)
            {
                ValidationErrors.Add(new ValidationResult("The Communication Date field is required.",
                    new[] { "CompletedDatePart" }));
                AllValid = false;
            }

            if (CompletedTimePart == null)
            {
                ValidationErrors.Add(new ValidationResult("The Communication Time field is required.",
                    new[] { "CompletedTimePart" }));
                AllValid = false;
            }

            if (CompletedDateTimeOffSet != null && CompletedDateTimeOffSet.DateTime > DateTime.Now)
            {
                ValidationErrors.Add(new ValidationResult("The Communication Date/Time field cannot be in the future.",
                    new[] { "CompletedDatePart", "CompletedTimePart" }));
                AllValid = false;
            }

            if (DisciplineKey == null)
            {
                ValidationErrors.Add(new ValidationResult("The Discipline field is required.",
                    new[] { "DisciplineKey" }));
                AllValid = false;
            }

            if (WeekFrom == null)
            {
                ValidationErrors.Add(new ValidationResult("The Week From field is required.", new[] { "WeekFrom" }));
                AllValid = false;
            }
            else if ((DateTime)WeekFrom > DateTime.Today.Date)
            {
                ValidationErrors.Add(new ValidationResult("The Week From field cannot be in the future.",
                    new[] { "WeekFrom" }));
                AllValid = false;
            }
            else if (((DateTime)WeekFrom).DayOfWeek != TenantSettingsCache.Current.TenantSettingWeekStartDay)
            {
                ValidationErrors.Add(new ValidationResult(
                    "The Week From field must be a " + TenantSettingsCache.Current.TenantSettingWeekStartDayText + ".",
                    new[] { "WeekFrom" }));
                AllValid = false;
            }

            if (VisitsPrescribed == null)
            {
                ValidationErrors.Add(new ValidationResult("The Prescribed Visits field is required.",
                    new[] { "VisitsPrescribed" }));
                AllValid = false;
            }

            if (VisitsActual == null)
            {
                ValidationErrors.Add(new ValidationResult("The Actual Visits field is required.",
                    new[] { "VisitsActual" }));
                AllValid = false;
            }

            if (VisitsPrescribed != null && VisitsActual != null && VisitsActual >= VisitsPrescribed)
            {
                ValidationErrors.Add(new ValidationResult("The Actual Visits must be less that the Prescribed Visits.",
                    new[] { "VisitsPrescribed", "VisitsActual" }));
                AllValid = false;
            }

            if (UnableToReschedule == null)
            {
                ValidationErrors.Add(new ValidationResult("The Unable To Re-schedule Visit field is required.",
                    new[] { "UnableToReschedule" }));
                AllValid = false;
            }

            if (ContactedPhysicianKey == null)
            {
                ValidationErrors.Add(new ValidationResult("The Physician field is required.",
                    new[] { "ContactedPhysicianKey" }));
                AllValid = false;
            }

            if (PhysicianNotification == null)
            {
                ValidationErrors.Add(new ValidationResult("The Physician Notification field is required.",
                    new[] { "PhysicianNotification" }));
                AllValid = false;
            }

            return AllValid;
        }

        public void Cleanup()
        {
            ++__cleanupCount;

            if (__cleanupCount > 1)
            {
                return;
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (_AdmissionCommunicationAllergyCollectionView != null)
                {
                    _AdmissionCommunicationAllergyCollectionView.Source = null;
                }

                if (_AdmissionCommunicationMedicationCollectionView != null)
                {
                    _AdmissionCommunicationMedicationCollectionView.Source = null;
                }

                if (_AdmissionCommunicationLabCollectionView != null)
                {
                    _AdmissionCommunicationLabCollectionView.Source = null;
                }

                if (_AdmissionCommunicationPatientLab != null)
                {
                    _AdmissionCommunicationPatientLab.Clear();
                }
            });
        }

        partial void OnCommunicationTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (CommunicationTypeCode.ToLower() == "sbar")
            {
                NoteText = null;

                //BEGIN carecoordinationnote fields
                CoordinationOfCare = null;
                CommunicationMode = null;
                TopicIssue = null;
                Narrative = null;
                FollowupResolution = null;
                //END carecoordinationnote fields
                ClearFieldsUnableToMeetFCD();
            }
            else if (CommunicationTypeCode.ToLower() == "teamcasenote" ||
                     CommunicationTypeCode.ToLower() == "generalnote")
            {
                AssessmentText = null;
                BackgroundText = null;
                RecommendationText = null;
                SituationText = null;
                ContactedPhysicianKey = null;

                //BEGIN carecoordinationnote fields
                CoordinationOfCare = null;
                CommunicationMode = null;
                TopicIssue = null;
                Narrative = null;
                FollowupResolution = null;
                //END carecoordinationnote fields
                ClearFieldsUnableToMeetFCD();
            }
            else if (CommunicationTypeCode.ToLower() == "carecoordinationnote")
            {
                NoteText = null;

                AssessmentText = null;
                BackgroundText = null;
                RecommendationText = null;
                SituationText = null;
                ContactedPhysicianKey = null;
                ClearFieldsUnableToMeetFCD();
            }
            else if (CommunicationTypeCode.ToLower() == "unabletomeetfcd")
            {
                ContactedPhysicianKey = null; // to remove the default
                NoteText = null;
                AssessmentText = null;
                BackgroundText = null;
                RecommendationText = null;
                SituationText = null;
                //BEGIN carecoordinationnote fields
                CoordinationOfCare = null;
                CommunicationMode = null;
                TopicIssue = null;
                FollowupResolution = null;
                //END carecoordinationnote fields
            }

            RaiseChanged();
        }

        private void ClearFieldsUnableToMeetFCD()
        {
            DisciplineKey = null;
            WeekFrom = null;
            VisitsPrescribed = null;
            VisitsActual = null;
            UnableToReschedule = null;
            PhysicianNotification = null;
        }

        partial void OnWeekFromChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("WeekThrough");
        }

        partial void OnContactedPhysicianKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ContactedPhysicianFullNameWithSuffix");
        }

        partial void OnDisciplineKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DisciplineDescription");
        }

        partial void OnSituationTextChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ThumbNailText");
        }

        partial void OnNoteTextChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ThumbNailText");
        }

        private string TrimCommunicationText(int length)
        {
            var retText = "";
            if (Expand)
            {
                if (CommunicationTypeCode.ToLower() == "carecoordinationnote")
                {
                    retText = TopicIssue;
                }
                else if (CommunicationTypeCode.ToLower() == "unabletomeetfcd")
                {
                    retText = PhysicianNotification == null ? "" : PhysicianNotification;
                }
                else
                {
                    retText = CommunicationTypeCode.ToLower() == "sbar" ? SituationText : NoteText;
                }
            }
            else
            {
                var text = string.Empty;

                if (CommunicationTypeCode.ToLower() == "carecoordinationnote")
                {
                    text = TopicIssue;
                }
                else if (CommunicationTypeCode.ToLower() == "unabletomeetfcd")
                {
                    text = PhysicianNotification;
                }
                else
                {
                    text = CommunicationTypeCode.ToLower() == "sbar" ? SituationText : NoteText;
                }

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
                    retText = text;
                }

                retText = splitText[0] + " ...";
            }

            return string.IsNullOrWhiteSpace(retText)
                ? ""
                : retText.Substring(0, retText.Length < length ? retText.Length - 1 : length - 1) + " ...";
        }

        partial void OnCompletedDatePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CompletedDateFormatted");
            RaisePropertyChanged("AssociateDiagnosis9AndOr10");
        }

        partial void OnAssociatedDiagnosis10Changed()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("AssociateDiagnosis9AndOr10");
        }

        partial void OnAssociatedDiagnosisChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("AssociateDiagnosis9AndOr10");
        }

        partial void OnCompletedTimePartChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CompletedDateFormatted");
        }

        public string GetAssociatedDiagnosisText(Admission admission, int? admissionDiagnosisKey)
        {
            if (admissionDiagnosisKey == null)
            {
                return null;
            }

            if (admission == null || admission.AdmissionDiagnosis == null)
            {
                return "AdmissionDiagnosisKey = " + admissionDiagnosisKey;
            }

            var ad = admission.AdmissionDiagnosis.Where(a => a.AdmissionDiagnosisKey == (int)admissionDiagnosisKey)
                .FirstOrDefault();
            if (ad == null)
            {
                return "AdmissionDiagnosisKey = " + admissionDiagnosisKey;
            }

            return (ad.Code == null ? "?" : ad.Code) + " - " + (ad.Description == null ? "?" : ad.Description);
        }

        public void SetupAdmissionCommunicationAllergyCollectionView(Admission currentAdmission)
        {
            _AdmissionCommunicationPatientAllergy = new List<PatientAllergy>();
            if (AdmissionCommunicationAllergy != null)
            {
                if (currentAdmission != null)
                {
                    if (currentAdmission.Patient != null)
                    {
                        if (currentAdmission.Patient.PatientAllergy != null)
                        {
                            foreach (var aca in AdmissionCommunicationAllergy)
                            {
                                var pa = currentAdmission.Patient.PatientAllergy
                                    .Where(p => p.PatientAllergyKey == aca.PatientAllergyKey).FirstOrDefault();
                                if (pa != null)
                                {
                                    _AdmissionCommunicationPatientAllergy.Add(pa);
                                }
                            }
                        }
                    }
                }
            }

            _AdmissionCommunicationAllergyCollectionView = new CollectionViewSource();
            _AdmissionCommunicationAllergyCollectionView.SortDescriptions.Add(new SortDescription("Description",
                ListSortDirection.Ascending));
            _AdmissionCommunicationAllergyCollectionView.Source = _AdmissionCommunicationPatientAllergy;

            AdmissionCommunicationAllergyCollectionView.Refresh();
            _AdmissionCommunicationAllergyCollectionView.View.MoveCurrentToFirst();
            RaisePropertyChanged("AdmissionCommunicationAllergyCollectionView");
            RaisePropertyChanged("AreAllergies");
        }

        public void SetupAdmissionCommunicationAllergyCollectionViewForDashboard(AdmissionCommunication admCom)
        {
            var currentAdmission = admCom.Admission;

            _AdmissionCommunicationPatientAllergy = new List<PatientAllergy>();

            _AdmissionCommunicationAllergyCollectionView = new CollectionViewSource();
            _AdmissionCommunicationAllergyCollectionView.SortDescriptions.Add(new SortDescription("Description",
                ListSortDirection.Ascending));
            _AdmissionCommunicationAllergyCollectionView.Source = _AdmissionCommunicationPatientAllergy;

            admCom._AdmissionCommunicationAllergyCollectionView.Source = currentAdmission.Patient.PatientAllergy.Where(
                    p => p.Superceded == false && p.DeletedDate == null &&
                         !p.Inactive &&
                         p.AllergyStartDate <= DateTime.Now.Date &&
                         (p.AllergyEndDate == null || p.AllergyEndDate >= DateTime.Now.Date))
                .OrderBy(p => p.Description).ToList();

            AdmissionCommunicationAllergyCollectionView.Refresh();
            _AdmissionCommunicationAllergyCollectionView.View.MoveCurrentToFirst();
            RaisePropertyChanged("AdmissionCommunicationAllergyCollectionView");
            RaisePropertyChanged("AreAllergies");
        }

        public void SetupAdmissionCommunicationMedicationCollectionView(Admission currentAdmission)
        {
            _AdmissionCommunicationPatientMedication = new List<PatientMedication>();
            if (AdmissionCommunicationMedication != null)
            {
                if (currentAdmission != null)
                {
                    if (currentAdmission.Patient != null)
                    {
                        if (currentAdmission.Patient.PatientMedication != null)
                        {
                            foreach (var acm in AdmissionCommunicationMedication)
                            {
                                var pm = currentAdmission.Patient.PatientMedication
                                    .Where(p => p.PatientMedicationKey == acm.PatientMedicationKey).FirstOrDefault();
                                if (pm != null)
                                {
                                    _AdmissionCommunicationPatientMedication.Add(pm);
                                }
                            }
                        }
                    }
                }
            }

            _AdmissionCommunicationMedicationCollectionView = new CollectionViewSource();
            _AdmissionCommunicationMedicationCollectionView.SortDescriptions.Add(
                new SortDescription("MedicationStatus", ListSortDirection.Ascending));
            _AdmissionCommunicationMedicationCollectionView.SortDescriptions.Add(
                new SortDescription("MedicationName", ListSortDirection.Ascending));
            _AdmissionCommunicationMedicationCollectionView.Source = _AdmissionCommunicationPatientMedication;

            AdmissionCommunicationMedicationCollectionView.Refresh();
            _AdmissionCommunicationMedicationCollectionView.View.MoveCurrentToFirst();
            RaisePropertyChanged("AdmissionCommunicationMedicationCollectionView");
            RaisePropertyChanged("AreMedications");
        }

        public void SetupAdmissionCommunicationMedicationCollectionViewForDashboard(AdmissionCommunication admCom)
        {
            var currentAdmission = admCom.Admission;
            var pmList = new List<PatientMedication>();

            _AdmissionCommunicationPatientMedication = new List<PatientMedication>();
            if (AdmissionCommunicationMedication != null)
            {
                if (currentAdmission != null)
                {
                    if (currentAdmission.Patient != null)
                    {
                        if (currentAdmission.Patient.PatientMedication != null)
                        {
                            pmList = currentAdmission.Patient.PatientMedication
                                .Where(p => p.Superceded == false && p.DeletedDate == null &&
                                            (p.MedicationStartDate == null || p.MedicationStartDate != null &&
                                                p.MedicationStartDate <= DateTime.Now.Date) &&
                                            (p.MedicationEndDate == null || p.MedicationEndDate != null &&
                                                p.MedicationEndDate >= DateTime.Now.Date.AddDays(-5)))
                                .OrderBy(p => p.MedicationName).ToList();
                        }
                    }
                }
            }

            _AdmissionCommunicationMedicationCollectionView = new CollectionViewSource();
            _AdmissionCommunicationMedicationCollectionView.SortDescriptions.Add(
                new SortDescription("MedicationStatus", ListSortDirection.Ascending));
            _AdmissionCommunicationMedicationCollectionView.SortDescriptions.Add(
                new SortDescription("MedicationName", ListSortDirection.Ascending));
            _AdmissionCommunicationMedicationCollectionView.Source = currentAdmission.AdmissionCommunicationMedication;

            AdmissionCommunicationMedicationCollectionView.Refresh();
            _AdmissionCommunicationMedicationCollectionView.View.MoveCurrentToFirst();

            admCom._AdmissionCommunicationMedicationCollectionView.Source = pmList;

            RaisePropertyChanged("AdmissionCommunicationMedicationCollectionView");
            RaisePropertyChanged("AreMedications");
        }

        public void SetupAdmissionCommunicationLabCollectionView(Admission currentAdmission)
        {
            _AdmissionCommunicationPatientLab = new List<PatientLab>();
            if (AdmissionCommunicationLab != null)
            {
                if (currentAdmission != null)
                {
                    if (currentAdmission.Patient != null)
                    {
                        if (currentAdmission.Patient.PatientLab != null)
                        {
                            foreach (var acl in AdmissionCommunicationLab)
                            {
                                var pl = currentAdmission.Patient.PatientLab
                                    .Where(p => p.PatientLabKey == acl.PatientLabKey).FirstOrDefault();
                                if (pl != null)
                                {
                                    _AdmissionCommunicationPatientLab.Add(pl);
                                }
                            }
                        }
                    }
                }
            }

            _AdmissionCommunicationLabCollectionView = new CollectionViewSource();
            _AdmissionCommunicationLabCollectionView.SortDescriptions.Add(new SortDescription("TestDate", ListSortDirection.Descending));
            _AdmissionCommunicationLabCollectionView.Source = _AdmissionCommunicationPatientLab;

            AdmissionCommunicationLabCollectionView.Refresh();
            _AdmissionCommunicationLabCollectionView.View.MoveCurrentToFirst();
            RaisePropertyChanged("AdmissionCommunicationLabCollectionView");
            RaisePropertyChanged("AreLabs");
        }

        public void SetupAdmissionCommunicationLabCollectionViewForDashboard(AdmissionCommunication admCom)
        {
            var currentAdmission = admCom.Admission;
            _AdmissionCommunicationPatientLab = new List<PatientLab>();

            _AdmissionCommunicationLabCollectionView = new CollectionViewSource();
            _AdmissionCommunicationLabCollectionView.SortDescriptions.Add(new SortDescription("TestDate", ListSortDirection.Descending));

            _AdmissionCommunicationLabCollectionView.Source = currentAdmission.Patient.PatientLab
                .Where(p => p.TestDate > DateTime.Today.Date.AddDays(-65) && p.Result != null)
                .OrderByDescending(p => p.TestDate);

            AdmissionCommunicationLabCollectionView.Refresh();
            _AdmissionCommunicationLabCollectionView.View.MoveCurrentToFirst();
            RaisePropertyChanged("AdmissionCommunicationLabCollectionView");
            RaisePropertyChanged("AreLabs");
        }
    }

    public partial class AdmissionEquipment
    {
        public string EquipmentDescription
        {
            get
            {
                string equipmentDescription = null;

                equipmentDescription = EquipmentCache.GetDescriptionFromKey(EquipmentKey);

                return equipmentDescription;
            }
        }

        public string EffectiveFrom => EquipmentCache.GetEffectiveFromFromKey(EquipmentKey);

        public string EffectiveThru => EquipmentCache.GetEffectiveThruFromKey(EquipmentKey);

        public AdmissionEquipment CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newaeq = (AdmissionEquipment)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newaeq);

            RejectChanges();
            BeginEditting();
            Superceded = true;
            if (newaeq.HistoryKey == null)
            {
                newaeq.HistoryKey = AdmissionEquipmentKey;
            }

            EndEditting();
            return newaeq;
        }
    }

    public partial class AdmissionConsent
    {
        public string DecisionDescription
        {
            get
            {
                var cl = CodeLookupCache.GetCodeDescriptionFromKey(DecisionKey);
                return cl == null ? "" : cl;
            }
        }

        public string DecisionMakerDisplay => DecisionMakerLName +
                                              (string.IsNullOrEmpty(DecisionMakerFName)
                                                  ? ""
                                                  : ", " + DecisionMakerFName);

        public List<CodeLookup> FilteredDecisionTypes
        {
            get
            {
                if (Requestor == "CAHPS Vendor")
                {
                    return CodeLookupCache.GetCodeLookupsFromType("RHIODecide").Where(cl =>
                        cl.CodeDescription != "Access Denied" && cl.CodeDescription != "Access Approved").ToList();
                }

                return CodeLookupCache.GetCodeLookupsFromType("RHIODecide").ToList();
            }
        }


        public bool CanEdit =>
            (AdmissionDocumentationConsent == null || AdmissionDocumentationConsent.Any() == false) && IsNew;

        public AdmissionConsent CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newac = (AdmissionConsent)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newac);

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newac;
        }

        partial void OnRequestorChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            Messenger.Default.Send(this, "RefreshDecisionTypes");
        }

        partial void OnDecisionKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DecisionDescription");
        }

        partial void OnDecisionMakerFNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DecisionMakerDisplay");
        }

        partial void OnDecisionMakerLNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DecisionMakerDisplay");
        }

        public void RaiseCanEditChanged()
        {
            RaisePropertyChanged("CanEdit");
        }

        public bool ClientValidate()
        {
            var AllValid = true;
            if (!Admission.InitialReferralDate.HasValue)
            {
                AllValid = false;
            }

            if (DecisionDate == null)
            {
                AllValid = false;
            }

            if (!AllValid || Admission.InitialReferralDate.Value.Date > DecisionDate.Date)
            {
                ValidationErrors.Add(new ValidationResult("Decision Date cannot be prior to the initial referral date",
                    new[] { "DecisionDate" }));
                AllValid = false;
            }

            if (string.IsNullOrEmpty(DecisionMakerFName))
            {
                if (!string.IsNullOrEmpty(DecisionMakerLName)
                    && DecisionMakerLName.ToUpper() != "SELF"
                   )
                {
                    ValidationErrors.Add(new ValidationResult("First Name is required",
                        new[] { "DecisionMakerLName" }));
                    AllValid = false;
                }
            }

            if (string.IsNullOrEmpty(Requestor))
            {
                ValidationErrors.Add(new ValidationResult("Requestor is required", new[] { "Requestor" }));
                AllValid = false;
            }

            return AllValid;
        }
    }

    public partial class AdmissionCOTI
    {
        private bool _IsSetup;

        public bool CanEdit => VerbalCOTIEncounterKey == null ? true : false;

        public bool IsSetupDone
        {
            get { return _IsSetup; }
            set
            {
                _IsSetup = value;
                RaisePropertyChanged("IsSetup");
            }
        }

        public bool ShowF2F
        {
            get
            {
                if (IsAttending && IsMedDirect == false)
                {
                    return false;
                }

                return PeriodNumber == null || PeriodNumber < 3 ? false : true;
            }
        }

        public bool ShowF2FNursePractitioner => ShowF2F == false ? false : IsHospiceNursePractitioner;

        public bool ShowF2FPhysicianAttestation => ShowF2F == false ? false : IsPhysicianAttestation;

        public bool IsPhysicianAttestation
        {
            get
            {
                if (IsMedDirect && IsHospiceNursePractitioner == false)
                {
                    return true;
                }

                return false;
            }
        }

        public Physician SigningPhysician
        {
            get
            {
                if (SigningPhysicianKey == null)
                {
                    return null;
                }

                var p = PhysicianCache.Current.GetPhysicianFromKey(SigningPhysicianKey);
                return p;
            }
        }

        public bool ShowDischargeInformation =>
            DischargeDate != null || DischargeReason != null || DeathDateTime != null ? true : false;

        public string DischargeDateString
        {
            get
            {
                if (DischargeDate == null)
                {
                    return null;
                }

                return DischargeDate.Value.Date.ToShortDateString();
            }
        }

        public string DeathDateTimeString
        {
            get
            {
                if (DeathDateTime == null)
                {
                    return null;
                }

                return DeathDateTime.Value.Date.ToShortDateString() + " " +
                       (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime
                           ? DeathDateTime.Value.ToString("HHmm")
                           : DeathDateTime.Value.DateTime.ToShortTimeString());
            }
        }

        public bool EncounterDateInRange(DateTime F2FStartDate, DateTime F2FEndDate)
        {
            if (EncounterDate == null)
            {
                return false;
            }

            var encounterDate = ((DateTime)EncounterDate).Date;
            if (encounterDate < F2FStartDate.Date)
            {
                return false;
            }

            if (encounterDate > F2FEndDate.Date)
            {
                return false;
            }

            return true;
        }

        partial void OnPeriodNumberChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowF2F");
            RaisePropertyChanged("ShowF2FNursePractitioner");
            RaisePropertyChanged("ShowF2FPhysicianAttestation");
        }

        partial void OnIsHospiceNursePractitionerChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowF2F");
            RaisePropertyChanged("ShowF2FNursePractitioner");
            RaisePropertyChanged("ShowF2FPhysicianAttestation");
            RaisePropertyChanged("IsPhysicianAttestation");
        }

        partial void OnIsHospicePhysicianChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowF2F");
            RaisePropertyChanged("ShowF2FNursePractitioner");
            RaisePropertyChanged("ShowF2FPhysicianAttestation");
            RaisePropertyChanged("IsPhysicianAttestation");
        }

        partial void OnIsAttendingChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowF2F");
            RaisePropertyChanged("ShowF2FNursePractitioner");
            RaisePropertyChanged("ShowF2FPhysicianAttestation");
            RaisePropertyChanged("IsPhysicianAttestation");
        }

        partial void OnIsMedDirectChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowF2F");
            RaisePropertyChanged("ShowF2FNursePractitioner");
            RaisePropertyChanged("ShowF2FPhysicianAttestation");
            RaisePropertyChanged("IsPhysicianAttestation");
        }

        partial void OnAttestationSignatureChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            AttestationDate = AttestationSignature == null
                ? (DateTime?)null
                : DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
        }
    }

    public partial class AdmissionDocumentation
    {
        private static readonly string NoDocumentAtached = "No document attached";

        public Encounter Encounter
        {
            get
            {
                if (AdmissionDocumentationEncounter == null)
                {
                    return null;
                }

                var ade = AdmissionDocumentationEncounter.FirstOrDefault();
                if (ade == null)
                {
                    return null;
                }

                return ade.Encounter;
            }
        }

        public bool IgnoreDocumentationType
        {
            get
            {
                var docType = DocumentationTypeCode;

                if (!string.IsNullOrEmpty(docType) &&
                    (docType.ToLower() == "unsignedorder" || docType.ToLower() == "unsignedbatchedorder" ||
                     docType.ToLower() == "unsignedpoc"))
                {
                    return true;
                }

                return false;
            }
        }

        public bool DocumentationTypeNullOrNoDocument
        {
            get
            {
                var docType = DocumentationTypeCode;
                if (string.IsNullOrEmpty(docType))
                {
                    return true;
                }

                if (docType.ToUpper().Equals("NDF2F") || docType.ToUpper().Equals("HNDF2F"))
                {
                    return true;
                }

                return false;
            }
        }

        public bool DocumentationTypeNoDocument
        {
            get
            {
                var docType = DocumentationTypeCode;
                if (string.IsNullOrEmpty(docType))
                {
                    return false;
                }

                if (docType.ToUpper().Equals("NDF2F") || docType.ToUpper().Equals("HNDF2F"))
                {
                    return true;
                }

                return false;
            }
        }

        public bool CanEdit
        {
            get
            {
                if (RoleAccessHelper.IsSurveyor)
                {
                    return false;
                }

                var adm = new AdmissionDocumentationManager();
                var subItem = AdmissionDocumentationManager.GetSubItemForAdmissionDocumentation(this);
                var canEdit = adm.CanEditDocumentationType(subItem,
                    AdmissionDocumentationManager.GetDocTypeFromKey(DocumentationType));
                var docType = DocumentationTypeCode;
                if (!string.IsNullOrEmpty(docType)
                    && (docType.ToLower() == "signedorder"
                        || docType.ToLower() == "signedbatchedorder"
                        || docType.ToLower() == "signedpoc"
                        || docType.ToLower() == "ff"
                        || docType.ToUpper() == "NDF2F"
                    )
                   )
                {
                    canEdit = true;
                }

                return canEdit;
            }
        }

        public string UpdatedDateFormatted
        {
            get
            {
                var date = UpdatedDate == null ? "" : Convert.ToDateTime(UpdatedDate).ToString("MM/dd/yyyy");
                var time = "";
                if (UpdatedDate != null)
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        time = Convert.ToDateTime(UpdatedDate).ToString("HHmm");
                    }
                    else
                    {
                        time = Convert.ToDateTime(UpdatedDate).ToShortTimeString();
                    }
                }

                return date + " " + time;
            }
        }

        public string DocumentationTypeCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(DocumentationType);

        public string DocumentationTypeCode => CodeLookupCache.GetCodeFromKey(DocumentationType);

        public string AdmissionMedicationScreeningBlirb
        {
            get
            {
                // Last Screening - mm/dd/yyyy at hh:mm PM by name with suffix
                var name = UserCache.Current.GetFullNameWithSuffixFromUserId(CreatedBy);
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "??";
                }

                var time = "??";
                var date = "??";
                if (CreatedDateTime != null)
                {
                    date = ((DateTime)CreatedDateTime).Date.ToShortDateString();
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        time = ((DateTime)CreatedDateTime).ToString("HHmm");
                    }
                    else
                    {
                        time = ((DateTime)CreatedDateTime).ToShortTimeString();
                    }
                }

                return string.Format("Last Screening - {0} at {1} by {2}", date, time, name);
            }
        }

        public bool IgnoreDocument(Admission a)
        {
            var docType = DocumentationTypeCode;
            if (!string.IsNullOrEmpty(docType) &&
                (docType.ToLower() == "signedorder" || docType.ToLower() == "signedbatchedorder" ||
                 docType.ToLower() == "signedpoc" || docType.ToLower() == "signedhea" ||
                 docType.ToLower() == "unsignedorder" || docType.ToLower() == "unsignedbatchedorder" ||
                 docType.ToLower() == "unsignedpoc"))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(docType) && docType.ToLower() == "cti")
            {
                if (AdmissionDocumentationEncounter != null && AdmissionDocumentationEncounter.Any())
                {
                    return true; // ignore electronic CTIs
                }
            }

            return false;
        }

        partial void OnDocumentationFileNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CurrentDocumentation");
        }

        partial void OnDocumentationTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (string.IsNullOrEmpty(DocumentationTypeCode) == false)
            {
                if (DocumentationTypeNoDocument)
                {
                    DocumentationFileName = NoDocumentAtached;
                }
                else if (DocumentationTypeNoDocument == false && DocumentationFileName == NoDocumentAtached)
                {
                    DocumentationFileName = null;
                }
            }

            RaisePropertyChanged("CurrentDocumentation");
        }
    }

    public enum AuthMode
    {
        ADD,
        EDIT
    }

    public class AuthType : INotifyPropertyChanged
    {
        private int? __key;

        private string description;
        private int sequence = 1;

        private string type;

        public int Sequence
        {
            get { return sequence; }
            set
            {
                sequence = value;
                RaisePropertyChanged("Sequence");
            }
        }

        public string Type
        {
            get { return type; }
            set
            {
                type = value;
                RaisePropertyChanged("Type");
                RaisePropertyChanged("AuthKey");
            }
        }

        public int? Key //will be a DisciplineKey or a CodeLookupKey or null
        {
            get { return __key; }
            set
            {
                __key = value;
                RaisePropertyChanged("Key");
                RaisePropertyChanged("AuthKey");
            }
        }

        public string Description
        {
            get { return description; }
            set
            {
                description = value;
                RaisePropertyChanged("Description");
            }
        }

        public string AuthKey
        {
            get
            {
                string key = null;
                if (Key.HasValue)
                {
                    key = Type + "|" + Key;
                }
                else
                {
                    key = Type;
                }

                return key;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Type = "GEN";
                    Key = null;
                }
                else
                {
                    var s = value.Split('|');
                    Type = s[0];
                    if (s.Count() > 1)
                    {
                        Key = null;
                        var keyString = s[1];
                        var keyInt = 0;
                        if (int.TryParse(keyString, out keyInt))
                        {
                            Key = keyInt;
                        }
                    }
                }

                RaisePropertyChanged("AuthKey");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public partial class AdmissionHospiceElectionStatement
    {
        public bool ShowAttendingAdmissionPhysician
        {
            get
            {
                if (DesignationOfAttending == null)
                {
                    return false;
                }

                return (bool)DesignationOfAttending;
            }
        }

        partial void OnDesignationOfAttendingChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowAttendingAdmissionPhysician");
        }

        public void RaiseAllPropertyChanged()
        {
            RaisePropertyChanged("ShowAttendingAdmissionPhysician");
        }
    }

    public partial class AdmissionGroup
    {
        private AdmissionGroup OrigValues;

        // Used in the maintenance screens in the time frame when the client adds the rows
        // but they haven't been re-Retrieved yet to have the Service Line Links.
        public int? GroupHeaderSequence
        {
            get
            {
                if (ServiceLineGrouping == null && ServiceLineGroupingKey == 0)
                {
                    return null;
                }

                return ServiceLineGrouping == null
                    ? ServiceLineCache.GetServiceLineGroupingFromKey(ServiceLineGroupingKey).ServiceLineGroupHeader
                        .SequenceNumber
                    : ServiceLineCache.GetServiceLineGroupHeaderFromKey(ServiceLineGrouping.ServiceLineGroupHeaderKey)
                        .SequenceNumber;
            }
        }

        public int? GroupHeaderKey
        {
            get
            {
                if (ServiceLineGrouping == null && ServiceLineGroupingKey == 0)
                {
                    return null;
                }

                return ServiceLineGrouping == null
                    ? ServiceLineCache.GetServiceLineGroupingFromKey(ServiceLineGroupingKey).ServiceLineGroupHeaderKey
                    : ServiceLineGrouping.ServiceLineGroupHeader.ServiceLineGroupHeaderKey;
            }
        }

        public string GroupingName
        {
            get
            {
                if (ServiceLineGrouping == null && ServiceLineGroupingKey == 0)
                {
                    return null;
                }

                return ServiceLineGrouping == null
                    ? ServiceLineCache.GetServiceLineGroupingFromKey(ServiceLineGroupingKey).Name
                    : ServiceLineGrouping.Name;
            }
        }

        public string GroupingHeaderName
        {
            get
            {
                if (ServiceLineGrouping == null && ServiceLineGroupingKey == 0)
                {
                    return null;
                }

                return ServiceLineGrouping == null
                    ? ServiceLineCache.GetServiceLineGroupingFromKey(ServiceLineGroupingKey).ServiceLineGroupHeader
                        .GroupHeaderLabel
                    : ServiceLineGrouping.ServiceLineGroupHeader.GroupHeaderLabel;
            }
        }

        public ObservableCollection<AdmissionGroup> AdmissionGroupSiblingList
        {
            get
            {
                if (Admission == null)
                {
                    return null;
                }

                if (OrderBySequence > 0)
                {
                    return null;
                }

                return new ObservableCollection<AdmissionGroup>(Admission.AdmissionGroup
                    .Where(ag =>
                        ag.MasterAdmissionGroupKey != null && ag.MasterAdmissionGroupKey == MasterAdmissionGroupKey)
                    .OrderBy(s => s.OrderBySequence)
                    .ToList());
            }
        }

        public string SiblingDisplayString
        {
            get
            {
                if (AdmissionGroupSiblingList == null)
                {
                    return "<UKNOWN>";
                }

                var retString = "";
                foreach (var ag in AdmissionGroupSiblingList)
                {
                    if (!string.IsNullOrEmpty(retString))
                    {
                        retString += " - ";
                    }

                    retString += ag.GroupingName;
                }

                return retString;
            }
        }

        public bool CanEditStartDate
        {
            get
            {
                if (OrigValues == null)
                {
                    OrigValues = (AdmissionGroup)GetOriginal();
                }

                if (OrigValues.StartDate != StartDate)
                {
                    return true;
                }

                return StartDate.HasValue ? StartDate.Value.Date >= DateTime.Today.Date : true;
            }
        }

        public bool CanEditEndDate
        {
            get
            {
                if (OrigValues == null)
                {
                    OrigValues = (AdmissionGroup)GetOriginal();
                }

                if (OrigValues.EndDate != EndDate)
                {
                    return true;
                }

                return EndDate.HasValue ? EndDate.Value.Date >= DateTime.Today.Date : true;
            }
        }

        public bool CanEditNonGroupDates => MasterAdmissionGroupKey == null;

        public bool CanEditSiblingGroups => CanEditStartDate;

        public int CreatedSequence { get; set; }

        public int OrderBySequence => GroupHeaderSequence == null ? CreatedSequence : (int)GroupHeaderSequence;

        partial void OnServiceLineGroupingKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            Messenger.Default.Send(AdmissionKey, "SetupAdmissionOasisHeaderDefaults");
        }

        partial void OnStartDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            Messenger.Default.Send(AdmissionKey, "SetupAdmissionOasisHeaderDefaults");
            if (AdmissionGroupSiblingList != null)
            {
                AdmissionGroupSiblingList.ForEach(item =>
                {
                    item.BeginEditting();
                    item.StartDate = StartDate == null ? (DateTime?)null : ((DateTime)StartDate).Date;
                });
            }
        }

        partial void OnEndDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            Messenger.Default.Send(AdmissionKey, "SetupAdmissionOasisHeaderDefaults");
            if (EndDate == DateTime.MinValue)
            {
                EndDate = null;
            }

            if (AdmissionGroupSiblingList != null)
            {
                AdmissionGroupSiblingList.ForEach(item =>
                {
                    item.BeginEditting();
                    item.EndDate = EndDate == null ? (DateTime?)null : ((DateTime)EndDate).Date;
                });
            }
        }
    }

    public class AdmissionGroupSet
    {
        public Guid? MasterAdmissionGroupKey { get; set; }
        public int? ServiceLineGroupingKey { get; set; }
        public int? ServiceLineGrouping2Key { get; set; }
        public int? ServiceLineGrouping3Key { get; set; }
        public int? ServiceLineGrouping4Key { get; set; }
        public int? ServiceLineGrouping5Key { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool CanEditStartDate => StartDate.HasValue ? StartDate.Value.Date >= DateTime.Today.Date : true;

        public bool CanEditEndDate => EndDate.HasValue ? EndDate.Value.Date >= DateTime.Today.Date : true;

        public bool CanEditSiblingGroups => CanEditStartDate;

        public void UpdateAdmissionGroupItem(AdmissionGroup item, int groupingKey)
        {
            item.BeginEditting();
            item.ServiceLineGroupingKey = groupingKey;
            item.StartDate = StartDate == null ? (DateTime?)null : ((DateTime)StartDate).Date;
            item.EndDate = EndDate == null ? (DateTime?)null : ((DateTime)EndDate).Date;
        }
    }

    public partial class AdmissionInfection
    {
        public string ServiceLineDescription => Admission.ServiceLineDescription;

        public string TreatmentComment
        {
            get
            {
                var CR = char.ToString('\r');
                var tc = "Treatment/Antibiotic: " +
                         (string.IsNullOrWhiteSpace(Treatment) == false ? Treatment : "None.");
                if (string.IsNullOrWhiteSpace(Comment) == false)
                {
                    tc = (tc == null ? "" : tc + CR + CR) + "Comments: " + Comment;
                }

                return tc;
            }
        }

        public AdmissionInfection CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newInf = (AdmissionInfection)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newInf);
            if (newInf.HistoryKey == null)
            {
                newInf.HistoryKey = AdmissionInfectionKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newInf;
        }

        partial void OnCultureChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (!Culture)
            {
                CultureDate = null;
            }
        }

        partial void OnTreatmentChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TreatmentComment");
        }

        partial void OnCommentChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TreatmentComment");
        }
    }

    public partial class AdmissionFaceToFace
    {
        private bool HasAdmission { get; set; }
        private DateTime? SOCDate { get; set; }
        private DateTime? ReferDateTime { get; set; }
        private bool IsHomeHealthServiceLine { get; set; }

        public Admission UnattachedAdmission
        {
            set
            {
                if (value == null)
                {
                    HasAdmission = false;
                    SOCDate = DateTime.MinValue.Date;
                    ReferDateTime = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified).Date;
                    IsHomeHealthServiceLine = false;
                }
                else
                {
                    HasAdmission = true;
                    SOCDate = value.SOCDate;
                    ReferDateTime = value.ReferDateTime;

                    ServiceLine sl;
                    if (value.ServiceLineKey > 0 && value.ServiceLine == null)
                    {
                        sl = ServiceLineCache.GetServiceLineFromKey(value.ServiceLineKey);
                    }
                    else
                    {
                        sl = value.ServiceLine;
                    }


                    IsHomeHealthServiceLine = sl.IsHomeHealthServiceLine;
                }
            }
        }

        public bool AllIsValid => IsEncounterInDateRangeForAdmission;

        public string InvalidMessage
        {
            get
            {
                var sb = new StringBuilder();
                if (!IsEncounterInDateRangeForAdmission)
                {
                    sb.AppendLine("Date of encounter is outside of the date range for the admission.");
                }
                return sb.ToString();
            }
        }

        public DateTime DateRangeLow
        {
            get
            {
                if (!HasAdmission)
                {
                    return DateTime.MinValue.Date;
                }

                if (SOCDate.HasValue)
                {
                    return SOCDate.Value.Date.AddDays(-90);
                }

                return ReferDateTime.Value.Date.AddDays(-90);
            }
        }

        public DateTime DateRangeHigh
        {
            get
            {
                if (!HasAdmission)
                {
                    return DateTime.MaxValue.Date;
                }

                if (SOCDate.HasValue)
                {
                    return SOCDate.Value.Date.AddDays(30);
                }

                return ReferDateTime.Value.Date.AddDays(30);
            }
        }

        public string EncounterDateRangeMessage
        {
            get
            {
                if (!IsHomeHealthServiceLine)
                {
                    return string.Empty;
                }

                if (SOCDate.HasValue)
                {
                    return "Face-to-Face Date Range for this admission: " + DateRangeLow.ToString("MM/dd/yyyy") +
                           " - " + DateRangeHigh.ToString("MM/dd/yyyy") + " (inclusive)";
                }

                return "Admission is not yet admitted - Encounter Date should be on or after " +
                       DateRangeLow.ToString("MM/dd/yyyy") +
                       ", based upon referral date, but could change depending on SOC date established.";
            }
        }
        public bool IsEncounterInDateRangeForAdmission => PhysianEncounterDate >= DateRangeLow && PhysianEncounterDate <= DateRangeHigh;

        partial void OnSpecificDiscIdentifiedChanged()
        {
            RaisePropertyChanged("AllIsValid");
            RaisePropertyChanged("InvalidMessage");
        }

        partial void OnClinicalNeedDocumentedChanged()
        {
            RaisePropertyChanged("AllIsValid");
            RaisePropertyChanged("InvalidMessage");
        }

        partial void OnHomeboundStatusDocumentedChanged()
        {
            RaisePropertyChanged("AllIsValid");
            RaisePropertyChanged("InvalidMessage");
        }

        partial void OnDatedSignaturePresentChanged()
        {
            RaisePropertyChanged("AllIsValid");
            RaisePropertyChanged("InvalidMessage");
        }

        partial void OnPhysianEncounterDateChanged()
        {
            RaisePropertyChanged("AllIsValid");
            RaisePropertyChanged("InvalidMessage");
        }
    }

    public partial class AdmissionFaceToFaceDiagnosis
    {
        public bool ExistsInAdmissionDiagnosis
        {
            get
            {
                return Admission.AdmissionDiagnosis.Any(d =>
                    d.ICDCodeKey == ICDCodeKey && d.Superceded == false && d.RemovedDate == null);
            }
        }

        partial void OnICDCodeKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ExistsInAdmissionDiagnosis");
        }
    }

    public partial class AdmissionProductCode
    {
        public AdmissionProductCode CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newac = (AdmissionProductCode)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newac);

            RejectChanges();
            BeginEditting();

            EndEditting();
            return newac;
        }

        public bool ClientValidate()
        {
            var AllValid = true;

            if (ProductCodeKey == null)
            {
                string[] memberNames = { "ProductCodeKey" };
                var Msg = "A Product Code Key is required.";
                ValidationErrors.Add(new ValidationResult(Msg, memberNames));

                AllValid = false;
            }

            return AllValid;
        }
    }

    public partial class AdmissionBatchedInterimOrder
    {
        private DateTime? orderDateNullable;

        public DateTime? OrderDateNullable
        {
            get { return orderDateNullable; }
            set
            {
                orderDateNullable = value;
                RaisePropertyChanged("OrderDateNullable");
            }
        }
    }

    public partial class AdmissionSignedPOC
    {
        private DateTime? certFromDateNullable;
        private DateTime? certThruDateNullable;
        private bool paperClip;
        private DateTime? signatureDateNullable;

        public bool PaperClip
        {
            get { return paperClip; }
            set
            {
                paperClip = value;
                RaisePropertyChanged("PaperClip");
            }
        }

        public bool CanEdit
        {
            get
            {
                if (AdmissionSignedPOCKey > 0)
                {
                    return false;
                }

                return PaperClip ? false : true;
            }
        }

        public DateTime? CertFromDateNullable
        {
            get { return certFromDateNullable; }
            set
            {
                certFromDateNullable = value;
                RaisePropertyChanged("CertFromDateNullable");
            }
        }

        public DateTime? CertThruDateNullable
        {
            get { return certThruDateNullable; }
            set
            {
                certThruDateNullable = value;
                RaisePropertyChanged("CertThruDateNullable");
            }
        }

        public DateTime? SignatureDateNullable
        {
            get { return signatureDateNullable; }
            set
            {
                signatureDateNullable = value;
                RaisePropertyChanged("SignatureDateNullable");
            }
        }
    }

    public partial class AdmissionSignedInterimOrder
    {
        private OrderEntry _CurrentOrderEntry;
        private int? _encounterKey;
        private DateTime? orderDateNullable;
        private DateTime? orderTimeNullable;
        private bool paperClip;

        public bool PaperClip
        {
            get { return paperClip; }
            set
            {
                paperClip = value;
                RaisePropertyChanged("PaperClip");
            }
        }

        public bool CanEdit
        {
            get
            {
                if (AdmissionSignedInterimOrderKey > 0)
                {
                    return false;
                }

                return PaperClip ? false : true;
            }
        }

        public int? EncounterKey
        {
            get { return _encounterKey; }
            set
            {
                _encounterKey = value;
                RaisePropertyChanged("EncounterKey");
            }
        }

        public OrderEntry CurrentOrderEntry
        {
            get { return _CurrentOrderEntry; }
            set
            {
                _CurrentOrderEntry = value;
                RaisePropertyChanged("CurrentOrderEntry");
            }
        }

        public DateTime? OrderDateNullable
        {
            get { return orderDateNullable; }
            set
            {
                orderDateNullable = value;
                RaisePropertyChanged("OrderDateNullable");
            }
        }

        public DateTime? OrderTimeNullable
        {
            get { return orderTimeNullable; }
            set
            {
                orderTimeNullable = value;
                RaisePropertyChanged("OrderTimeNullable");
            }
        }
    }

    public partial class AdmissionTeamMeeting
    {
        public AdmissionTeamMeeting CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newatm = (AdmissionTeamMeeting)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newatm);

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();

            return newatm;
        }
    }

    public partial class TaskAdmission : IServiceLineGroupingService
    {
        private string _AdmissionStatusCode;
        private Insurance _MyInsurance;

        public string AdmissionStatusCode
        {
            get
            {
                if (_AdmissionStatusCode == null)
                {
                    _AdmissionStatusCode = CodeLookupCache.GetCodeFromKey(AdmissionStatus);
                }

                return _AdmissionStatusCode;
            }
        }

        public DateTime? ResumptionDate
        {
            get
            {
                var resump = EncounterResumption;
                var transfer = EncounterTransfer;
                if (resump != null && transfer != null)
                {
                    if ((resump.ResumptionDate == null ? resump.ResumptionReferralDate : resump.ResumptionDate) >=
                        transfer.TransferDate)
                    {
                        return resump.ResumptionDate == null ? resump.ResumptionReferralDate : resump.ResumptionDate;
                    }
                }
                else
                {
                    if (transfer != null)
                    {
                        var ad = AdmissionDiscipline.OrderByDescending(ado => ado.ReferDateTime)
                            .Where(adr =>
                                adr.NotTaken == false && adr.DischargeDateTime == null &&
                                adr.DisciplineAdmitDateTime >= transfer.TransferDate).FirstOrDefault();
                        if (ad != null)
                        {
                            return ad.DisciplineAdmitDateTime;
                        }
                    }
                    else if (resump != null)
                    {
                        return resump.ResumptionDate == null ? resump.ResumptionReferralDate : resump.ResumptionDate;
                    }
                    else
                    {
                        return null;
                    }
                }

                return null;
            }
        }

        public string AdmissionStatusText
        {
            get
            {
                var StatusString = "";
                if (AdmissionStatusCode == "A" && TenantSettingsCache.Current.TenantSetting.ContractServiceProvider)
                {
                    StatusString = "Admitted on " +
                                   (AdmitDateTime == null ? "?" : ((DateTime)AdmitDateTime).ToString("MM/dd/yyyy"));
                }

                if (AdmissionStatusCode == "A")
                {
                    StatusString = "Admitted on " +
                                   (SOCDate == null ? "?" : ((DateTime)SOCDate).ToString("MM/dd/yyyy"));
                }
                else if (AdmissionStatusCode == "D")
                {
                    StatusString = "Discharged on " + (DischargeDateTime == null
                        ? "Unknown Date"
                        : ((DateTime)DischargeDateTime).ToString("MM/dd/yyyy"));
                }
                else if (AdmissionStatusCode == "H")
                {
                    StatusString = "On Hold on " +
                                   (PreEvalOnHoldDateTime.HasValue
                                       ? ((DateTime)PreEvalOnHoldDateTime).ToString("MM/dd/yyyy")
                                       : "Unknown Date") + " follow-up on " + (PreEvalFollowUpDate.HasValue
                                       ? ((DateTime)PreEvalFollowUpDate).ToString("MM/dd/yyyy")
                                       : "Unknown Date");
                }
                else if (AdmissionStatusCode == "M")
                {
                    StatusString = "Resumed on " + (ResumptionDate.HasValue
                        ? ((DateTime)ResumptionDate).ToString("MM/dd/yyyy")
                        : "Unknown Date");
                }
                else if (AdmissionStatusCode == "R")
                {
                    StatusString = "Referred on " + (ReferDateTime.HasValue
                        ? ((DateTime)ReferDateTime).ToString("MM/dd/yyyy")
                        : "Unknown Date");
                }
                else if (AdmissionStatusCode == "N")
                {
                    StatusString = "Not admitted on " + (NotTakenDateTime.HasValue
                        ? ((DateTime)NotTakenDateTime).ToString("MM/dd/yyyy")
                        : "Unknown Date");
                }
                else if (AdmissionStatusCode == "T")
                {
                    StatusString = "Transferred on " + (EncounterTransfer == null
                        ? "Unknown Date"
                        : EncounterTransfer.TransferDate.ToString("MM/dd/yyyy"));
                }
                else
                {
                    StatusString = "Unknown status";
                }

                if (ServiceLineKey > 0)
                {
                    StatusString = StatusString + " to " + ServiceLineCache.GetServiceLineFromKey(ServiceLineKey).Name;
                }

                return StatusString;
            }
        }

        public DateTime AdmissionGroupDate { get; set; }

        public bool HIBInsuranceElectionAddendumAvailable =>
            MyInsurance == null ? false : MyInsurance.ElectionAddendumAvailable;

        public bool InsuranceRequiresDisciplineOrders => MyInsurance == null ? false : MyInsurance.DisciplineOrders;

        public Insurance MyInsurance
        {
            get
            {
                if (_MyInsurance != null)
                {
                    return _MyInsurance;
                }

                if (InsuranceKey == null)
                {
                    return null;
                }

                _MyInsurance = InsuranceCache.GetInsuranceFromKey((int)InsuranceKey);
                return _MyInsurance;
            }
        }

        public AdmissionGroup CurrentGroup => GetNthCurrentGroup(0, AdmissionGroupDate);

        public AdmissionGroup CurrentGroup2 => GetNthCurrentGroup(1, AdmissionGroupDate);

        public AdmissionGroup CurrentGroup3 => GetNthCurrentGroup(2, AdmissionGroupDate);

        public AdmissionGroup CurrentGroup4 => GetNthCurrentGroup(3, AdmissionGroupDate);

        public AdmissionGroup CurrentGroup5 => GetNthCurrentGroup(4, AdmissionGroupDate);

        partial void OnAdmissionStatusChanged()
        {
            _AdmissionStatusCode = null;
        }

        private bool StartDateLessThanOrEqualToAdmissionGroupDate(DateTime admissionGroupDate, DateTime? startDate)
        {
            if (admissionGroupDate == null || admissionGroupDate == DateTime.MinValue)
            {
                return false;
            }

            if (startDate == null)
            {
                return true;
            }

            return ((DateTime)startDate).Date <= admissionGroupDate.Date;
        }

        private bool EndDateGreaterThanOrEqualToAdmissionGroupDate(DateTime admissionGroupDate, DateTime? endDate)
        {
            if (admissionGroupDate == null || admissionGroupDate == DateTime.MinValue)
            {
                return false;
            }

            if (endDate == null)
            {
                return true;
            }

            return ((DateTime)endDate).Date >= admissionGroupDate.Date;
        }

        private AdmissionGroup GetNthCurrentGroup(int GroupToRetrieve, DateTime admissionGroupDate)
        {
            AdmissionGroup ag = null;
            if ((admissionGroupDate == null || admissionGroupDate == DateTime.MinValue) == false)
            {
                ag = AdmissionGroup.Where(ag1 =>
                        ag1.GroupHeaderSequence == GroupToRetrieve &&
                        StartDateLessThanOrEqualToAdmissionGroupDate(admissionGroupDate, ag1.StartDate) &&
                        EndDateGreaterThanOrEqualToAdmissionGroupDate(admissionGroupDate, ag1.EndDate))
                    .OrderByDescending(p => p.StartDate).ThenBy(k => k.AdmissionGroupKey).FirstOrDefault();
            }

            // fall back to the only one we can find.
            if (ag == null && (admissionGroupDate == null || admissionGroupDate == DateTime.MinValue))
            {
                ag = AdmissionGroup.Where(ag2 => ag2.GroupHeaderSequence == GroupToRetrieve)
                    .OrderByDescending(p => p.StartDate).ThenBy(k => k.AdmissionGroupKey)
                    .FirstOrDefault();
            }

            return ag;
        }
    }

    public partial class TeamMeetingPOCO
    {
        private string __CareCoordinatorFullNameWithSuffix;

        public string Reason
        {
            get
            {
                if (AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Discharged && DeathDate.HasValue)
                {
                    return "Deceased";
                }

                if (AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Discharged)
                {
                    return "Live Discharge";
                }

                if (SOCDate == null || TeamMeetingCount == 1)
                {
                    return "New SOC";
                }

                return "Active";
            }
        }

        public string PeriodEndDateString
        {
            get
            {
                if (AdmissionCertification_PeriodEndDate.HasValue == false)
                {
                    return "?";
                }

                return AdmissionCertification_PeriodEndDate.Value.ToShortDateString().Trim();
            }
        }

        public DateTime? PeriodEndDate
        {
            get
            {
                if (AdmissionCertification_PeriodEndDate.HasValue == false)
                {
                    return null;
                }

                return AdmissionCertification_PeriodEndDate.Value.Date;
            }
        }

        public int? TaskKey { get; set; }
        public int? EncounterKey { get; set; }
        public int? ServiceTypeKey { get; set; }

        public string CareCoordinatorFullNameWithSuffix
        {
            get
            {
                if (__CareCoordinatorFullNameWithSuffix != null)
                {
                    return __CareCoordinatorFullNameWithSuffix;
                }

                var up = UserCache.Current.GetUserProfileFromUserId(CareCoordinator);
                if (up == null)
                {
                    return "?";
                }

                var __firstName = string.Format("{0}{1}",
                    up.FirstName == null ? "" : " " + up.FirstName.Trim(),
                    up.MiddleName == null ? "" : " " + up.MiddleName.Trim());

                if (up.FriendlyName != null && up.FriendlyName.Trim() != "")
                {
                    __firstName = " " + up.FriendlyName;
                }

                __CareCoordinatorFullNameWithSuffix = string.Format("{0}{1},{2}",
                    up.LastName == null ? "" : up.LastName.Trim(),
                    up.Suffix == null ? "" : " " + up.Suffix.Trim(),
                    __firstName);
                return __CareCoordinatorFullNameWithSuffix;
            }
        }

        public string ServiceStartDateString
        {
            get
            {
                if (ServiceStartDate.HasValue == false)
                {
                    return "?";
                }

                return ServiceStartDate.Value.ToShortDateString().Trim();
            }
        }

        public string TeamDueDateDayOfWeek
        {
            get
            {
                if (TeamDueDate.HasValue == false)
                {
                    return "?";
                }

                return ((DateTime)TeamDueDate).ToString("ddd");
            }
        }

        public string TeamDueDateString
        {
            get
            {
                if (TeamDueDate.HasValue == false)
                {
                    return "?";
                }

                return TeamDueDate.Value.ToShortDateString().Trim();
            }
        }
    }

    public class POCOrdersForDiscAndTreatment
    {
        public DateTime? StartDate { get; set; }

        public int? DisciplineKey { get; set; }

        public string DisciplineCode { get; set; }

        public List<AdmissionDisciplineFrequency> DisciplineFrequencies { get; set; }
        public List<AdmissionGoalElement> AdmissionGoalElements { get; set; }

        public List<AdmissionGoalElement> AdmissionGoalElementsForOrders
        {
            get
            {
                if (CurrentAdmission != null && CurrentAdmission.HospiceAdmission)
                {
                    return new List<AdmissionGoalElement>();
                }

                return AdmissionGoalElements;
            }
        }

        public Admission CurrentAdmission { get; set; }
    }

    public class PrintLibraryDisplayStruct
    {
        public bool IsHeader { get; set; }
        public bool IsBody { get; set; }

        public bool InString2 { get; set; }

        public bool InString3 { get; set; }

        public string DisplayString1 { get; set; }
        public string DisplayString2 { get; set; }
        public string DisplayString3 { get; set; }
    }
}