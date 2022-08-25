#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core
{
    // TODO Convert from IDataErrorInfo to INotifyDataErrorInfo
    public class AdmissionDocumentationItem : INotifyPropertyChanged, IDataErrorInfo
    {
        int _CMSFormKey;

        public int CMSFormKey
        {
            get { return _CMSFormKey; }
            set
            {
                _CMSFormKey = value;
                RaisePropertyChanged("CMSFormKey");
            }
        }

        public string AttachedFormDescription
        {
            get
            {
                Form f = DynamicFormCache.GetFormByKey((int)AttachedFormKey);
                if (f == null)
                {
                    return String.Format("<No description for form: {0}>", AttachedFormKey.Value);
                }

                string sd = f.Description;
                if (f.IsCMSForm)
                {
                    sd = f.CMSFormDescription;
                    if (Encounter != null)
                    {
                        EncounterCMSForm ecf = Encounter.EncounterCMSForm.FirstOrDefault(p => (p.QuestionKey == AttachedFormQuestionKey));
                        DateTime? signedDate = ecf?.SignedDate;
                        if (signedDate == null)
                        {
                            signedDate = Encounter.EncounterOrTaskStartDateAndTime?.Date;
                        }

                        sd = sd + "_" + ((signedDate == null) ? "?" : ((DateTime)signedDate).ToShortDateString());
                    }
                }

                if (String.IsNullOrWhiteSpace(sd))
                {
                    return String.Format("<No description for form: {0}>", AttachedFormKey.Value);
                }

                return sd;
            }
        }

        public string AttachedFormDescriptionNew
        {
            get
            {
                Form f = DynamicFormCache.GetFormByKey((int)AttachedFormKey);
                if (f == null)
                {
                    return String.Format("<No description for form: {0}>", AttachedFormKey.Value);
                }

                string sd = f.Description;
                if (f.IsCMSForm)
                {
                    sd = f.CMSFormDescription;
                    if ((sd != null) && (sd.ToLower() == "mhes"))
                    {
                        sd = "Hospice Election Statement";
                    }
                }

                if (String.IsNullOrWhiteSpace(sd))
                {
                    return String.Format("<No description for form: {0}>", AttachedFormKey.Value);
                }

                return sd;
            }
        }

        public bool IsOrderEntry { get; set; }
        public bool IsPOC { get; set; }

        public string OrderType
        {
            get
            {
                string orderType = null;

                if (IsPOC)
                {
                    orderType = "Plan of Care";
                }
                else if (IsOrderEntry)
                {
                    orderType = "Interim Order";
                }
                else if (IsFaceToFaceEncounter)
                {
                    orderType = "Face to Face";
                }
                else
                {
                    orderType = "Document";
                }

                return orderType;
            }
        }

        public string OrderTypeToolTip
        {
            get
            {
                string toolTip = null;

                if (IsPOC)
                {
                    if ((Encounter != null)
                        && (Encounter.EncounterPlanOfCare != null)
                        && Encounter.EncounterPlanOfCare.Any()
                       )
                    {
                        DateTime? certFrom = Encounter.EncounterPlanOfCare.First().CertificationFromDate;
                        DateTime? certThru = Encounter.EncounterPlanOfCare.First().CertificationThruDate;

                        if (certFrom != null)
                        {
                            toolTip = certFrom.Value.ToShortDateString();
                        }

                        toolTip += " - ";

                        if (certThru != null)
                        {
                            toolTip += certThru.Value.ToShortDateString();
                        }
                    }
                }
                else if (IsOrderEntry)
                {
                    if ((Encounter != null)
                        && (Encounter.OrderEntry != null)
                        && Encounter.OrderEntry.Any()
                       )
                    {
                        OrderEntry oe = Encounter.OrderEntry.First();

                        if (oe.IsGeneratedReferral)
                        {
                            if (!string.IsNullOrEmpty(toolTip))
                            {
                                toolTip += Environment.NewLine;
                            }

                            toolTip += "Referral";
                        }

                        if (oe.IsGeneratedVisitFrequency)
                        {
                            if (!string.IsNullOrEmpty(toolTip))
                            {
                                toolTip += Environment.NewLine;
                            }

                            toolTip += "Visit Frequency";
                        }

                        if (oe.IsGeneratedGoals)
                        {
                            if (!string.IsNullOrEmpty(toolTip))
                            {
                                toolTip += Environment.NewLine;
                            }

                            toolTip += "Goal and Treatment";
                        }

                        if (oe.IsGeneratedLabs)
                        {
                            if (!string.IsNullOrEmpty(toolTip))
                            {
                                toolTip += Environment.NewLine;
                            }

                            toolTip += "Labs/Test";
                        }

                        if (oe.IsGeneratedInitialServiceOrder)
                        {
                            if (!string.IsNullOrEmpty(toolTip))
                            {
                                toolTip += Environment.NewLine;
                            }

                            toolTip += "Initial Order for Start of Care";
                        }

                        if (oe.IsGeneratedMedications)
                        {
                            if (!string.IsNullOrEmpty(toolTip))
                            {
                                toolTip += Environment.NewLine;
                            }

                            toolTip += "Medication";
                        }

                        if (oe.IsGeneratedEquipment)
                        {
                            if (!string.IsNullOrEmpty(toolTip))
                            {
                                toolTip += Environment.NewLine;
                            }

                            toolTip += "Equipment";
                        }

                        if (oe.IsGeneratedSupply)
                        {
                            if (!string.IsNullOrEmpty(toolTip))
                            {
                                toolTip += Environment.NewLine;
                            }

                            toolTip += "Supplies";
                        }

                        if (oe.IsGeneratedSupplyEquipment)
                        {
                            if (!string.IsNullOrEmpty(toolTip))
                            {
                                toolTip += Environment.NewLine;
                            }

                            toolTip += "Supplies / Equipment";
                        }

                        if (oe.IsGeneratedOther)
                        {
                            if (!string.IsNullOrEmpty(toolTip))
                            {
                                toolTip += Environment.NewLine;
                            }

                            toolTip += "Other";
                        }

                        if (oe.IsGeneratedRecertificationOrder)
                        {
                            if (!string.IsNullOrEmpty(toolTip))
                            {
                                toolTip += Environment.NewLine;
                            }

                            toolTip += "Recertification Order";
                        }
                    }
                }
                else
                {
                    toolTip = null;
                }

                return toolTip;
            }
        }

        public DateTimeOffset? OrderDate
        {
            get
            {
                DateTimeOffset? orderDate = null;

                if (IsPOC || IsFaceToFaceEncounter)
                {
                    if (Encounter != null)
                    {
                        orderDate = Encounter.EncounterOrTaskStartDateAndTime;
                    }
                }
                else if (IsOrderEntry)
                {
                    if (Encounter != null)
                    {
                        if (Encounter.CurrentOrderEntry != null)
                        {
                            orderDate = Encounter.CurrentOrderEntry.CompletedDate;
                        }
                        else
                        {
                            orderDate = Encounter.EncounterOrTaskStartDateAndTime;
                        }
                    }
                }

                return orderDate;
            }
        }

        public string OrderTimeForDisplay
        {
            get
            {
                string orderTimeForDisplay = null;

                if ((OrderDate != null)
                    && (OrderDate != DateTimeOffset.MinValue)
                   )
                {
                    if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                    {
                        return OrderDate.Value.ToString("HHmm");
                    }

                    return OrderDate.Value.ToString("t");
                }

                return orderTimeForDisplay;
            }
        }

        public bool ShowToolTip => !string.IsNullOrEmpty(OrderTypeToolTip);

        public bool IsDocument { get; set; }
        public bool IsCommunication { get; set; }
        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                RaisePropertyChanged("IsSelected");
            }
        }

        public string DocumentationFileName { get; set; }

        public DateTime UpdatedDate { get; set; }
        public Guid UpdatedBy { get; set; }

        public Guid CreatedBy { get; set; }

        public string CreatedByUserName
        {
            get
            {
                if (CreatedBy == null)
                {
                    return null;
                }

                return UserCache.Current.GetFormalNameFromUserId(CreatedBy);
            }
        }

        [Required]
        public int DocumentationType { get; set; }

        public byte[] AttachedDocument { get; set; } //only for adding newly attached documents

        public int PatientKey { get; set; }
        public int? AdmissionKey { get; set; }
        public int? AdmissionCommunicationKey { get; set; }

        private WeakReference __AdmissionComm;

        public AdmissionCommunication AdmissionComm
        {
            get { return (__AdmissionComm == null) ? null : __AdmissionComm.Target as AdmissionCommunication; }
            set { __AdmissionComm = new WeakReference(value); }
        }

        public int SourceKey { get; set; }

        public DateTime Date { get; set; }

        public int? ServiceTypeKey { get; set; }
        public int? TaskKey { get; set; }
        public int? FormKey { get; set; }
        public int? AttachedFormKey { get; set; }
        public int? AttachedFormQuestionKey { get; set; }
        public int? AttachedFormSectionKey { get; set; }
        public Encounter Encounter { get; set; }

        public int? EncounterKey { get; set; }
        public int? AdmissionDocumentationEncounterKey { get; set; }

        private WeakReference __AdmissionDocumentation;

        public AdmissionDocumentation AdmissionDocumentation
        {
            get
            {
                return __AdmissionDocumentation?.Target as AdmissionDocumentation;
            }
            set { __AdmissionDocumentation = new WeakReference(value); }
        }

        public string EncounterViewLabel { get; set; }
        public string DocumentationTypeLabel { get; set; }

        public bool Signed { get; set; }
        public int EncounterStatus { get; set; }
        public int OrderEntryStatus { get; set; }
        public bool Addendum { get; set; }
        public string Oasis { get; set; }

        public bool Inactive { get; set; }
        public string InactiveText { get; set; }
        public string InactiveHistoryInfoText { get; set; }
        public DateTime? InactiveDate { get; set; }
        public Guid? InactivatedBy { get; set; }

        public int? OverrideSigningPhysicianKey
        {
            get
            {
                int? phyKey = null;
                if (Encounter != null)
                {
                    if (Encounter.CurrentOrderEntry != null)
                    {
                        phyKey = Encounter.CurrentOrderEntry.SigningPhysicianKey;
                    }
                    else if ((Encounter.EncounterAdmission != null)
                             && Encounter.EncounterAdmission.Any()
                            )
                    {
                        phyKey = Encounter.EncounterAdmission.First().SigningPhysicianKey;
                    }
                }

                return phyKey;
            }
        }

        public AdmissionDocumentationItem()
        {
            IsCommunication = false;
            IsDocument = false;
            IsSelected = false;
        }

        public string AdmissionDocumentationItemTypeID { get; set; }
        public int EncounterAttachedFormKey { get; set; }

        public string DebugInfo
        {
            get
            {
                var type = "X";
                switch (AdmissionDocumentationItemTypeID)
                {
                    case "A":
                        type = string.Format("A - {0}", EncounterAttachedFormKey);
                        break;
                    case "C":
                        type = string.Format("C - {0}", AdmissionCommunicationKey);
                        break;
                    case "E":
                        type = string.Format("E - {0}", EncounterKey);
                        break;
                    case "D":
                        type = string.Format("D - {0}", AdmissionDocumentationKey);
                        break;
                    default:
                        type = "X";
                        break;
                }

                return type;
            }
        }

        private bool _IsFaceToFaceEncounter;

        public bool IsFaceToFaceEncounter
        {
            get { return _IsFaceToFaceEncounter; }
            set
            {
                _IsFaceToFaceEncounter = value;
                RaisePropertyChanged("IsFaceToFaceEncounter");
            }
        }

        #region IDataErrorInfo

        private string err = string.Empty;
        public string Error => err;

        public string this[string columnName]
        {
            get
            {
                string msg = null;
                if (columnName == "DocumentationType")
                {
                    if (DocumentationType < 1)
                    {
                        msg = "Documentation File Type is required.";
                    }
                }

                return msg;
            }
        }

        #endregion IDataErrorInfo

        public string Status
        {
            get
            {
                string status = null;

                if (IsPOC
                    && (Encounter != null)
                    && (Encounter.EncounterPlanOfCare != null)
                    && Encounter.EncounterPlanOfCare.Any()
                   )
                {
                    EncounterPlanOfCare epoc = Encounter.EncounterPlanOfCare.First();
                    if (epoc.SignedDate != null)
                    {
                        status = "Signed";
                    }
                    else if (epoc.MailedDate != null)
                    {
                        status = "Mailed";
                    }
                    else if (epoc.PrintedDate != null)
                    {
                        status = "Printed";
                    }
                    else if (EncounterStatus == (int)EncounterStatusType.Completed)
                    {
                        status = (Encounter.ReviewDate == null) ? "Completed" : "Reviewed";
                    }
                    else if (EncounterStatus == (int)EncounterStatusType.POCOrderReview)
                    {
                        status = "Ready To Review";
                    }
                    else
                    {
                        status = "In Process";
                    }
                }
                else
                {
                    if (OrderEntryStatus == (int)OrderStatusType.InProcess)
                    {
                        status = "In Process";
                    }
                    else if (OrderEntryStatus == (int)OrderStatusType.OrderEntryReview)
                    {
                        status = "Ready To Review";
                    }
                    else if (OrderEntryStatus == (int)OrderStatusType.Completed)
                    {
                        status = "Reviewed";
                    }
                    else if (OrderEntryStatus == (int)OrderStatusType.SigningPhysicianVerified)
                    {
                        status = "Signed";
                    }
                    else if (OrderEntryStatus == (int)OrderStatusType.Voided)
                    {
                        status = "Void";
                    }
                }

                return status;
            }
        }

        public OrdersTracking OrdersTracking
        {
            get
            {
                // If this is a F2F document, look at the F2F OT row for our status
                if (IsFaceToFaceEncounter)
                {
                    if (Encounter != null)
                    {
                        if (Encounter.OrdersTracking != null)
                        {
                            return Encounter.OrdersTracking.Where(o => o.Inactive == false)
                                .Where(a => (a.OrderType == (int)OrderTypesEnum.FaceToFace) ||
                                            (a.OrderType == (int)OrderTypesEnum.FaceToFaceEncounter))
                                .OrderByDescending(o => o.OrdersTrackingKey).FirstOrDefault();
                        }
                    }
                }
                else if (Encounter != null)
                {
                    if (Encounter.OrdersTracking !=
                        null) // A POC can have multiple ordertracking rows for the same EncounterKey = one for the POC and one for the F2F
                    {
                        return (IsPOC)
                            ? Encounter.OrdersTracking
                                .Where(o => ((o.Inactive == false) && (o.OrderType == (int)OrderTypesEnum.POC)))
                                .OrderByDescending(o => o.OrdersTrackingKey).FirstOrDefault()
                            : Encounter.OrdersTracking.Where(o => o.Inactive == false)
                                .OrderByDescending(o => o.OrdersTrackingKey).FirstOrDefault();
                    }
                }

                if (IsDocument && (AdmissionDocumentation != null))
                {
                    AdmissionCOTI ac = AdmissionCOTI;
                    if ((ac != null) && (ac.OrdersTracking != null))
                    {
                        return ac.OrdersTracking.OrderByDescending(o => o.OrdersTrackingKey).FirstOrDefault();
                    }

                    AdmissionFaceToFace af = AdmissionFaceToFace;
                    if ((af != null) && (af.OrdersTracking != null))
                    {
                        return af.OrdersTracking.OrderByDescending(o => o.OrdersTrackingKey).FirstOrDefault();
                    }
                }

                return null;
            }
        }

        public AdmissionABN AdmissionABN
        {
            get
            {
                AdmissionABN aa = null;
                if (AdmissionDocumentation.AdmissionABN != null)
                {
                    aa = AdmissionDocumentation.AdmissionABN.OrderByDescending(a => a.AdmissionABNKey).FirstOrDefault();
                }

                return aa;
            }
        }

        public AdmissionCOTI AdmissionCOTI
        {
            get
            {
                AdmissionCOTI ac = null;
                if (AdmissionDocumentation.AdmissionCOTI != null)
                {
                    ac = AdmissionDocumentation.AdmissionCOTI.OrderByDescending(a => a.AdmissionCOTIKey)
                        .FirstOrDefault();
                }

                return ac;
            }
        }

        public AdmissionHospiceElectionStatement AdmissionHospiceElectionStatement
        {
            get
            {
                AdmissionHospiceElectionStatement ahes = null;
                if (AdmissionDocumentation.AdmissionHospiceElectionStatement != null)
                {
                    ahes = AdmissionDocumentation.AdmissionHospiceElectionStatement
                        .OrderByDescending(a => a.AdmissionHospiceElectionStatementKey).FirstOrDefault();
                }

                return ahes;
            }
        }

        public AdmissionFaceToFace AdmissionFaceToFace
        {
            get
            {
                AdmissionFaceToFace af = null;
                if (AdmissionDocumentation.AdmissionFaceToFace != null)
                {
                    af = AdmissionDocumentation.AdmissionFaceToFace.OrderByDescending(a => a.AdmissionFaceToFaceKey)
                        .FirstOrDefault();
                }

                return af;
            }
        }

        public int? AdmissionDocumentationKey { get; set; }
        public string ViewDocumentTooltip { get; set; }

        public int? CalculatedAdmissionDocumentationKey
        {
            get
            {
                int? admissionDocumentationKey = null;
                ViewDocumentTooltip = "View Document";
                if ((Encounter != null) && ((FormKey != null) || (AttachedFormKey != null) || (CMSFormKey != 0)))
                {
                    if (Encounter.AdmissionDocumentationEncounter != null)
                    {
                        AdmissionDocumentationEncounter ade = Encounter.AdmissionDocumentationEncounter
                            .OrderByDescending(a => a.AdmissionDocumentationEncounterKey).FirstOrDefault();
                        if (ade != null)
                        {
                            admissionDocumentationKey = ade.AdmissionDocumentationKey;
                            if (ade.AdmissionDocumentation != null)
                            {
                                ViewDocumentTooltip = ade.AdmissionDocumentation.DocumentationFileName;
                            }
                        }

                        if (admissionDocumentationKey == null)
                        {
                            return null;
                        }
                    }

                    // for orders - use tracking status to determine ones that are 'back-in-the-pipeline' - 
                    // if so, ignore the most recent document - its stale - and use the actual form instead
                    // like for batches where an order was removed, or a voided order... 
                    OrdersTracking ot = OrdersTracking;
                    if ((ot != null) &&
                        ((ot.Status != (int)OrdersTrackingStatus.ReadyForPrint) &&
                         (ot.Status != (int)OrdersTrackingStatus.Sent) &&
                         (ot.Status != (int)OrdersTrackingStatus.Signed)))
                    {
                        // order is in Edit, ReadyForReview, Complete (awaiting document generation) or Voided
                        return null;
                    }

                    return admissionDocumentationKey;
                }

                if (IsDocument && (AdmissionDocumentation != null))
                {
                    ViewDocumentTooltip = AdmissionDocumentation.DocumentationFileName;
                    return AdmissionDocumentation.AdmissionDocumentationKey;
                }

                return null;
            }
        }

        private string OrderStatusDescriptionOverride
        {
            get
            {
                if ((IsPOC) && (Encounter != null) && (Encounter.EncounterPlanOfCare != null) &&
                    Encounter.EncounterPlanOfCare.Any())
                {
                    EncounterPlanOfCare epoc = Encounter.EncounterPlanOfCare.First();
                    if ((epoc.PrintedDate != null) || (epoc.MailedDate != null))
                    {
                        return "Sent";
                    }

                    if (epoc.SignedDate != null)
                    {
                        return "Signed";
                    }
                }
                else if (Encounter.CurrentOrderEntry != null)
                {
                    OrderEntry oe = Encounter.CurrentOrderEntry;
                    if (oe.OrderStatus == (int)OrderStatusType.SigningPhysicianVerified)
                    {
                        return "Signed";
                    }

                    if (oe.OrderStatus == (int)OrderStatusType.Voided)
                    {
                        return "Void";
                    }
                }

                return null;
            }
        }

        public string StatusDescription
        {
            get
            {
                if ((Encounter != null) && ((FormKey != null) || (AttachedFormKey != null) || (CMSFormKey != 0)))
                {
                    // Interim Orders:
                    // Skim off OrderEntry Status of OrderStatusType.OrderEntryReview
                    // Ignore InProcess, Completed, SigningPhysicianVerified, Voided - we'll fall thru and get these from OrdersTracking Status
                    if (OrderEntryStatus == (int)OrderStatusType.OrderEntryReview)
                    {
                        return "Order Review";
                    }

                    // POCs:
                    // Skim off Encounter Status of EncounterStatusType.POCOrderReview
                    // Ignore EncounterPlanOfCare.SignedDate/MailedDate/PrintedDate/Voided - we'll fall thru and get these from OrdersTracking Status
                    if (Encounter.EncounterStatus == (int)EncounterStatusType.POCOrderReview)
                    {
                        return Encounter.EncounterStatusDescription;
                    }

                    // For completed encounters - use OrdersTracking Status if it exists (dig deeper into the order if not)
                    if (Encounter.EncounterStatus == (int)EncounterStatusType.Completed)
                    {
                        OrdersTracking ot = null;
                        // skim off electronic CTIs 
                        if (Encounter.AdmissionCOTI != null)
                        {
                            AdmissionCOTI ac = Encounter.AdmissionCOTI.FirstOrDefault();
                            if ((ac != null) && (Encounter.Admission != null) &&
                                (Encounter.Admission.OrdersTracking != null) &&
                                (Encounter.Admission.OrdersTracking != null))
                            {
                                ot = Encounter.Admission.OrdersTracking.FirstOrDefault(o => o.AdmissionCOTIKey == ac.AdmissionCOTIKey);
                            }
                        }

                        if (ot == null)
                        {
                            ot = OrdersTracking;
                        }

                        if (ot != null)
                        {
                            return ot.OrdersTrackingStatusDescription;
                        }

                        if (string.IsNullOrWhiteSpace(OrderStatusDescriptionOverride) == false)
                        {
                            return OrderStatusDescriptionOverride;
                        }
                    }

                    // otherwise - use the encounter status
                    return Encounter.EncounterStatusDescription;
                }
                // This case should never happen - but...

                if ((Encounter == null) && ((AttachedFormKey != null) || (FormKey != null) || (CMSFormKey != 0)))
                {
                    return "Complete";
                }

                if (IsCommunication && (AdmissionComm != null))
                {
                    return "Complete";
                }

                if (IsDocument && (AdmissionDocumentation != null))
                {
                    // Skim off Documents with OrdersTracking (FaceToFace, COTI) - trying to use OrdersTracking status first - otherwise fall thru and default to Complete
                    OrdersTracking ot = OrdersTracking;
                    if (ot != null)
                    {
                        return ot.OrdersTrackingStatusDescription;
                    }
                }

                return "Complete";
            }
        }

        public string DocumentName
        {
            get
            {
                string documentName = null;
                if (IsFaceToFaceEncounter)
                {
                    documentName = "Face to Face";
                }
                else if ((Encounter != null) && ((FormKey != null) || (AttachedFormKey != null) || (CMSFormKey != 0)))
                {
                    documentName = DocumentDescriptionPlusSuffix;
                }
                else if (IsCommunication && (AdmissionComm != null))
                {
                    documentName = DocumentDescriptionPlusSuffix;
                }
                else if (IsDocument && (AdmissionDocumentation != null))
                {
                    documentName = DocumentationTypeLabel;
                }

                return string.IsNullOrWhiteSpace(documentName) ? "Admission Document" : documentName;
            }
        }

        public string DocumentDescriptionPlusSuffix
        {
            get
            {
                ServiceType st = null;
                if (Encounter != null)
                {
                    st = ServiceTypeCache.GetServiceTypeFromKey((int)Encounter.ServiceTypeKey);
                    if ((st != null) && st.IsAttempted)
                    {
                        return Encounter.AttemptedServiceTypeDescription; // Skim off attempted visits
                    }
                }

                if (!string.IsNullOrWhiteSpace(DocumentationFileName))
                {
                    return DocumentationFileName;
                }

                if (AttachedFormKey.HasValue)
                {
                    return AttachedFormDescriptionNew;
                }

                string sd = ServiceTypeCache.GetDescriptionFromKeyWithOasisOverride(ServiceTypeKey.GetValueOrDefault());
                return sd + ((Encounter == null) ? null : " " + Encounter.EncounterSuffix);
            }
        }

        public DateTime CreatedDate
        {
            get
            {
                DateTime? createdDate = null;
                if ((Encounter != null) && ((FormKey != null) || (AttachedFormKey != null) || (CMSFormKey != 0)))
                {
                    // Skim off CMSForm signed date
                    if (CMSFormKey != 0)
                    {
                        EncounterCMSForm ecf = Encounter.EncounterCMSForm.FirstOrDefault(p => (p.QuestionKey == AttachedFormQuestionKey));
                        createdDate = ecf?.SignedDate;
                        if (createdDate != null)
                        {
                            return (DateTime)createdDate;
                        }
                    }

                    // Skim off COTI Dates
                    if (Encounter.EncounterIsCOTI)
                    {
                        AdmissionCOTI ac = (Encounter.AdmissionCOTI == null)
                            ? null
                            : Encounter.AdmissionCOTI.FirstOrDefault();
                        if ((ac != null) && (ac.AttestationDate != null))
                        {
                            return ((DateTime)ac.AttestationDate).Date;
                        }

                        if ((ac != null) && (ac.SignatureDate != null))
                        {
                            return ((DateTime)ac.SignatureDate).Date;
                        }

                        if ((ac != null) && (ac.CertificationFromDate != null))
                        {
                            return ((DateTime)ac.CertificationFromDate).Date;
                        }
                    }

                    createdDate = Encounter.CreatedDate;
                }
                else if (IsCommunication && (AdmissionComm != null))
                {
                    createdDate = AdmissionComm.CompletedDatePart == null
                        ? AdmissionComm.UpdatedDate
                        : (DateTime)AdmissionComm.CompletedDatePart;
                }
                else if (IsDocument && (AdmissionDocumentation != null))
                {
                    OrdersTracking ot = OrdersTracking;
                    if (ot != null)
                    {
                        createdDate = ot.OrderDate;
                    }

                    if ((AdmissionDocumentation.Encounter != null) && (createdDate == null))
                    {
                        createdDate = Encounter.CreatedDate;
                    }

                    AdmissionABN aa = AdmissionABN;
                    if ((aa != null) && (createdDate == null))
                    {
                        createdDate = aa.DateOfIssue;
                    }

                    AdmissionCOTI ac = AdmissionCOTI;
                    if ((ac != null) && (createdDate == null))
                    {
                        createdDate = ac.SignatureDate;
                    }

                    AdmissionHospiceElectionStatement ahes = AdmissionHospiceElectionStatement;
                    if ((ahes != null) && (createdDate == null))
                    {
                        createdDate = (ahes.HospiceEOBDate != null)
                            ? ((DateTime)ahes.HospiceEOBDate).Date
                            : ahes.UpdatedDate.Date;
                    }

                    AdmissionFaceToFace af = AdmissionFaceToFace;
                    if ((af != null) && (createdDate == null))
                    {
                        createdDate = af.PhysianEncounterDate;
                    }

                    if (createdDate == null)
                    {
                        createdDate = (AdmissionDocumentation.CreatedDateTime != null)
                            ? AdmissionDocumentation.CreatedDateTime
                            : AdmissionDocumentation.UpdatedDate;
                    }
                }

                if (createdDate != null)
                {
                    return (DateTime)createdDate;
                }

                return DateTime.Now;
            }
        }

        public List<string> DocTypeDescriptionList
        {
            get
            {
                List<string> dtList = new List<string>();
                if ((Encounter != null) && ((FormKey != null) || (AttachedFormKey != null) || (CMSFormKey != 0)))
                {
                    if (CMSFormKey != 0)
                    {
                        Form f = DynamicFormCache.GetFormByKey(CMSFormKey);
                        if (f != null)
                        {
                            dtList.Add(f.CMSFormDescription);
                        }

                        if (dtList.Contains("CMS Form (all)") == false)
                        {
                            dtList.Add("CMS Form (all)");
                        }
                    }
                    else if (AttachedFormKey != null)
                    {
                        Form f = DynamicFormCache.GetFormByKey((int)AttachedFormKey);
                        if ((f != null) && (string.IsNullOrWhiteSpace(f.Description) == false))
                        {
                            dtList.Add(((f.Description == "Medicare Secondary Payer Questionnaire")
                                ? "MSP Questionnaire"
                                : f.Description));
                        }
                    }
                    else if (FormKey != null)
                    {
                        string dt = DynamicFormCache.GetAdmissionDocumentationFormTypeDescriptionByKey((int)FormKey);
                        if ((Encounter.Admission != null) && Encounter.Admission.HospiceAdmission &&
                            (dt == "Plan Of Care"))
                        {
                            dt = "Hospice Plan";
                        }

                        if ((string.IsNullOrWhiteSpace(dt) == false))
                        {
                            dtList.Add(dt);
                        }

                        if (Encounter.EncounterIsOrderEntry || Encounter.EncounterIsPlanOfCare)
                        {
                            if (dtList.Contains("Order (all)") == false)
                            {
                                dtList.Add("Order (all)");
                            }
                        }
                    }
                }

                if (IsCommunication && (AdmissionComm != null))
                {
                    if (string.IsNullOrWhiteSpace(AdmissionComm.CommunicationTypeCodeDescription) == false)
                    {
                        dtList.Add(AdmissionComm.CommunicationTypeCodeDescription);
                    }

                    if (dtList.Contains("Communication (all)") == false)
                    {
                        dtList.Add("Communication (all)");
                    }
                }

                if (IsDocument && (AdmissionDocumentation != null))
                {
                    string dtc = AdmissionDocumentation.DocumentationTypeCode;
                    string dtcd = AdmissionDocumentation.DocumentationTypeCodeDescription;
                    if ((dtc != null) && (dtcd != null))
                    {
                        if (dtc.ToLower() == "abn")
                        {
                            if (AdmissionDocumentation.AdmissionABN != null)
                            {
                                AdmissionABN aa = AdmissionDocumentation.AdmissionABN.FirstOrDefault();
                                dtList.Add(((aa == null) ? "CMS Form" : aa.AdmissionDocumentationABNTypeDescription));
                            }
                            else
                            {
                                dtList.Add("CMS Form");
                            }

                            dtList.Add("CMS Form (all)");
                        }
                        else if (dtc.ToLower() == "cti")
                        {
                            dtList.Add("CTI");
                        }
                        else if (dtc.ToLower() == "phi access")
                        {
                            dtList.Add("PHI Access/Request");
                        }
                        else if (dtc.ToLower() == "orders")
                        {
                            dtList.Add("Order");
                            if (dtList.Contains("Order (all)") == false)
                            {
                                dtList.Add("Order (all)"); // even though not part or orders tracking
                            }
                        }
                        else
                        {
                            dtList.Add(dtcd);
                        }
                    }
                }

                if (dtList.Any() == false)
                {
                    dtList.Add("Admission Document");
                }

                OrdersTracking ot = OrdersTracking;
                if (ot != null)
                {
                    if (dtList.Contains("Order (all)") == false)
                    {
                        dtList.Add("Order (all)");
                    }
                }

                if (string.IsNullOrWhiteSpace(Oasis) == false)
                {
                    dtList.Add(((Oasis.StartsWith("OASIS")) ? "OASIS (all)" : "HIS (all)"));
                }

                return dtList;
            }
        }

        public string MainDocTypeDescription
        {
            get
            {
                if ((DocTypeDescriptionList == null) || (DocTypeDescriptionList.Any() == false))
                {
                    return "Admission Document";
                }

                return DocTypeDescriptionList[0];
            }
        }

        public string PhysicianNameAndSuffix => PhysicianName;

        public string PhysicianName
        {
            get
            {
                string physicianName = null;

                if (OverrideSigningPhysicianKey.HasValue)
                {
                    physicianName =
                        PhysicianCache.Current.GetPhysicianFullNameInformalWithSuffixFromKey(
                            OverrideSigningPhysicianKey);
                }

                return physicianName;
            }
        }

        public bool CanEdit
        {
            get
            {
                bool canEdit = false;

                if (OrderType != null && OrderType.ToLower().Contains("face"))
                {
                    canEdit = false;
                }
                else if (IsOrderEntry || IsPOC)
                {
                    if ((Status != "Void")
                        && (Status != "Signed")
                       )
                    {
                        canEdit = true;
                    }
                }
                else
                {
                    string docTypeCode = CodeLookupCache.GetCodeFromKey(DocumentationType);

                    if (string.IsNullOrEmpty(docTypeCode)
                        || (docTypeCode.ToLower() != "encounter")
                       )
                    {
                        canEdit = true;
                    }
                }

                return canEdit;
            }
        }

        public bool CanLaunchDocument
        {
            get
            {
                if (AdmissionDocumentation == null)
                {
                    return true;
                }

                return !AdmissionDocumentation.DocumentationTypeNoDocument;
            }
        }

        public bool CanInactivate
        {
            get
            {
                if (RoleAccessHelper.IsSurveyor)
                {
                    return false;
                }

                return AdmissionDocumentationManager.GetCanInactivate(IsDocument, EncounterKey,
                    AdmissionDocumentationEncounterKey, CreatedBy,
                    AdmissionDocumentation, Encounter);
            }
        }

        public bool CanIncludeInChart
        {
            get
            {
                var complete = Encounter == null || EncounterStatus == (int)EncounterStatusType.Completed;
                return complete && !Inactive && CanLaunchDocument;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string DocumentDescriptionLine2
        {
            get
            {
                if ((Encounter != null) && (Encounter.EncounterSupervision != null) && Encounter.EncounterSupervision.Any(d => d.DisciplineKey > 0))
                {
                    string desc = null;
                    foreach (var sup in Encounter.EncounterSupervision.Where(d => d.DisciplineKey > 0))
                    {
                        desc = (desc == null) ? "(Supervised : " : desc + ", ";
                        desc = desc + DisciplineCache.GetDisciplineFromKey(sup.DisciplineKey)
                            .SupervisedServiceTypeLabel;
                    }

                    desc = desc + ")";
                    return desc;
                }

                return null;
            }
        }

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public int Period { get; set; }
        public string PeriodThumbNail { get; set; }
        private string PeriodThumbNailBeforeFirst = "Documents before initial period or outside a period";
        private string PeriodThumbNailAfterLast = "Documents after last period";

        public void SetupAdmissionCertification(Admission admission)
        {
            Period = 0; // Assume Document iscert  before initial period (assuming can't find cert
            if (admission != null)
            {
                if (admission.HideCertPeriods)
                {
                    PeriodThumbNailBeforeFirst = string.Empty;
                    PeriodThumbNailAfterLast = string.Empty;
                }
            }

            if ((admission == null) || (admission.AdmissionCertification == null) ||
                (admission.AdmissionCertification.Any() == false))
            {
                PeriodThumbNail = PeriodThumbNailBeforeFirst;
                return;
            }

            AdmissionCertification ac = null;
            DateTime createdDate = CreatedDate;

            ac = admission.GetAdmissionCertForDate(createdDate, false);
            if (ac == null)
            {
                AdmissionCertification last = admission.AdmissionCertification.OrderBy(a => a.PeriodStartDate)
                    .Last();
                if ((last != null) && (last.PeriodEndDate != null) &&
                    (createdDate > ((DateTime)last.PeriodEndDate).Date))
                {
                    Period = 99999;
                }
            }

            if (ac != null)
            {
                Period = ac.PeriodNumber;
                if (admission.HideCertPeriods)
                {
                    PeriodThumbNail = "";
                }
                else
                {
                    PeriodThumbNail = string.Format("Period: {0}  {1} thru {2}", Period.ToString(),
                        ConvertDate(ac.PeriodStartDate), ConvertDate(ac.PeriodEndDate));
                }
            }
            else if (Period == 99999)
            {
                PeriodThumbNail = PeriodThumbNailAfterLast;
            }
            else
            {
                PeriodThumbNail = PeriodThumbNailBeforeFirst;
            }
        }

        private string ConvertDate(DateTime? dt)
        {
            if (dt == null)
            {
                return "?";
            }

            DateTime ddt = dt.GetValueOrDefault();
            return ddt.ToShortDateString();
        }

        public bool ShowAttachSignedDocument
        {
            get
            {
                if (RoleAccessHelper.IsSurveyor)
                {
                    return false;
                }

                // applicable to POCS, HospicePlans and COTIs generated from VerbalCOTIs
                if (RoleAccessHelper.CheckPermission("PatientEdit") == false)
                {
                    return false;
                }

                if (Encounter == null)
                {
                    return false;
                }

                if (Encounter.EncounterStatus != (int)EncounterStatusType.Completed)
                {
                    return false;
                }

                if (Encounter.EncounterIsPlanOfCare)
                {
                    return true;
                }

                if (IsOrderEntry)
                {
                    if (Encounter.CurrentOrderEntry == null)
                    {
                        return false;
                    }

                    if ((Encounter.Admission == null) || (Encounter.Admission.InterimOrderBatch == null) ||
                        (Encounter.Admission.InterimOrderBatchDetail == null))
                    {
                        return true;
                    }

                    InterimOrderBatchDetail iobd = Encounter.Admission.InterimOrderBatchDetail.FirstOrDefault(b => ((b.OrderEntryKey == Encounter.CurrentOrderEntry.OrderEntryKey) &&
                        (b.RemovedFromBatchDate == null)));
                    if (iobd == null)
                    {
                        return true;
                    }

                    InterimOrderBatch iob = Encounter.Admission.InterimOrderBatch
                        .FirstOrDefault(b => ((b.InterimOrderBatchKey == iobd.InterimOrderBatchKey) && (b.Inactive == false)));
                    var ret = (iob == null) ? true : false;
                    return ret;
                }

                if (Encounter.EncounterIsCOTI)
                {
                    AdmissionCOTI ac = (Encounter.AdmissionCOTI == null)
                        ? null
                        : Encounter.AdmissionCOTI.FirstOrDefault();
                    if (ac == null)
                    {
                        return false;
                    }

                    if (ac.IsCOTI == false)
                    {
                        return false;
                    }

                    if (ac.VerbalCOTIEncounterKey == null)
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }
        }

        public string AttachSignedDocumentTooltip =>
            "Attach signed " + ((string.IsNullOrWhiteSpace(DocumentName)) ? "Document" : DocumentName);
    }
}