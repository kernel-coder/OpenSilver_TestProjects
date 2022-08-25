#region Usings

using System;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Services
{
    public class OrdersTrackingManager
    {
        private int? OriginalPhysicianKey;
        private int? OriginalPhysicianAddressKey;
        private int? NewPhysicianKey;
        private int? NewPhysicianAddressKey;

        public bool SetTrackingRowToSent(Entity Order, int OrderType, int OrderKey)
        {
            switch (OrderType)
            {
                case (int)OrderTypesEnum.CoTI:
                case (int)OrderTypesEnum.HospiceFaceToFace:
                    break;
                case (int)OrderTypesEnum.FaceToFace:
                case (int)OrderTypesEnum.FaceToFaceEncounter:
                    OrdersTracking ot1 = Order as OrdersTracking;
                    if (ot1 != null)
                    {
                        ot1.Status = (int)OrdersTrackingStatus.Sent;
                    }

                    break;
                case (int)OrderTypesEnum.InterimOrder:
                    OrderEntry oe = Order as OrderEntry;
                    if (oe != null)
                    {
                        if ((oe.OrderStatus != (int)OrderStatusType.Completed) &&
                            (oe.OrderStatus != (int)OrderStatusType.Voided))
                        {
                            oe.OrderStatus = (int)OrderStatusType.Completed;
                        }
                    }

                    break;
                case (int)OrderTypesEnum.HospiceElectionAddendum:
                    break;
                case (int)OrderTypesEnum.POC:
                    EncounterPlanOfCare poc = Order as EncounterPlanOfCare;
                    if (poc != null)
                    {
                        if ((poc.IsPrinted == false) || (poc.PrintedDate == null) || (poc.PrintedBy == null))
                        {
                            poc.IsPrinted = true;
                            if (poc.PrintedDate == null)
                            {
                                poc.PrintedDate = DateTimeOffset.Now;
                            }

                            if (poc.PrintedBy == null)
                            {
                                poc.PrintedBy = WebContext.Current.User.MemberID;
                            }
                        }

                        if ((poc.MailedDate == null) || (poc.MailedBy == null))
                        {
                            if (poc.MailedDate == null)
                            {
                                poc.MailedDate = poc.PrintedDate;
                            }

                            if (poc.MailedBy == null)
                            {
                                poc.MailedBy = poc.PrintedBy;
                            }
                        }
                    }

                    break;
            }

            // Tidy up the associated ordersTracking
            OrdersTracking ot = FindOrdersTrackingRow(Order, OrderType, OrderKey);
            if (ot == null)
            {
                return false;
            }

            if (((ot.Status == (int)OrdersTrackingStatus.Complete) ||
                 (ot.Status == (int)OrdersTrackingStatus.ReadyForPrint)) == false)
            {
                return false;
            }

            ot.Status = (int)OrdersTrackingStatus.Sent;
            ot.StatusDate = DateTime.UtcNow;
            ot.UpdatedDate = DateTime.UtcNow;
            return true;
        }

        public bool SetTrackingRowToSigned(Entity Order, int OrderType, int OrderKey, DateTimeOffset? SignedDate = null)
        {
            DateTimeOffset signedDate = SignedDate ?? DateTimeOffset.Now;

            // Tidy up the order
            switch (OrderType)
            {
                case (int)OrderTypesEnum.CoTI:
                case (int)OrderTypesEnum.HospiceFaceToFace:
                case (int)OrderTypesEnum.FaceToFace:
                case (int)OrderTypesEnum.FaceToFaceEncounter:
                    break;
                case (int)OrderTypesEnum.InterimOrder:
                    OrderEntry oe = Order as OrderEntry;
                    if (oe != null)
                    {
                        if ((oe.SigningPhysicianVerifiedDate == null) || (oe.SigningPhysicianVerifiedBy == null))
                        {
                            if (oe.SigningPhysicianVerifiedDate == null)
                            {
                                oe.SigningPhysicianVerifiedDate = signedDate;
                            }

                            if (oe.SigningPhysicianVerifiedBy == null)
                            {
                                oe.SigningPhysicianVerifiedBy = WebContext.Current.User.MemberID;
                            }
                        }

                        if ((oe.OrderStatus != (int)OrderStatusType.SigningPhysicianVerified) &&
                            (oe.OrderStatus != (int)OrderStatusType.Voided))
                        {
                            oe.OrderStatus = (int)OrderStatusType.SigningPhysicianVerified;
                        }
                    }

                    break;
                case (int)OrderTypesEnum.HospiceElectionAddendum:
                    break;
                case (int)OrderTypesEnum.POC:
                    EncounterPlanOfCare poc = Order as EncounterPlanOfCare;
                    if (poc != null)
                    {
                        if ((poc.IsPrinted == false) || (poc.PrintedDate == null) || (poc.PrintedBy == null))
                        {
                            poc.IsPrinted = true;
                            if (poc.PrintedDate == null)
                            {
                                poc.PrintedDate = signedDate;
                            }

                            if (poc.PrintedBy == null)
                            {
                                poc.PrintedBy = WebContext.Current.User.MemberID;
                            }
                        }

                        if ((poc.MailedDate == null) || (poc.MailedBy == null))
                        {
                            if (poc.MailedDate == null)
                            {
                                poc.MailedDate = poc.PrintedDate;
                            }

                            if (poc.MailedBy == null)
                            {
                                poc.MailedBy = poc.PrintedBy;
                            }
                        }

                        if ((poc.SignedDate == null) || (poc.SignedBy == null))
                        {
                            if (poc.SignedDate == null)
                            {
                                poc.SignedDate = signedDate;
                            }

                            if (poc.SignedBy == null)
                            {
                                poc.SignedBy = WebContext.Current.User.MemberID;
                            }
                        }
                    }

                    break;
            }

            // Tidy up the associated ordersTracking
            OrdersTracking ot = FindOrdersTrackingRow(Order, OrderType, OrderKey);
            if (ot == null)
            {
                return false;
            }

            if ((ot.Status != (int)OrdersTrackingStatus.Signed) && (ot.Status != (int)OrdersTrackingStatus.Void))
            {
                ot.Status = (int)OrdersTrackingStatus.Signed;
                ot.StatusDate = signedDate.Date;
                ot.UpdatedDate = signedDate.Date;
            }

            return true;
        }

        public OrdersTracking RefreshTrackingRow(
            Entity Order,
            int OrderKey,
            Encounter Encounter,
            Admission Admission,
            AdmissionDocumentation AdmissionDocumentation,
            PhysicianAddress PhysicianAddress,
            int AdmissionCertKey,
            int OrderType,
            int PhysicianKey,
            int? PhysicianAddressKey,
            DateTime OrderDate,
            Guid? ClinicianID,
            bool Inactive,
            bool? Signed)
        {
            OrdersTracking otr = FindOrdersTrackingRow(Order, OrderType, OrderKey);
            OriginalPhysicianKey = (otr == null) ? (int?)null : otr.PhysicianKey;
            OriginalPhysicianAddressKey = (otr == null) ? null : otr.PhysicianAddressKey;
            NewPhysicianKey = PhysicianKey;
            NewPhysicianAddressKey = PhysicianAddressKey;
            bool PhysicianChanged =
                ((OriginalPhysicianKey != NewPhysicianKey) || (OriginalPhysicianAddressKey != NewPhysicianAddressKey))
                    ? true
                    : false;
            if (IsValidOrderType(OrderType))
            {
                SetTrackingData(Order, OrderKey, Admission, AdmissionDocumentation, PhysicianAddress, AdmissionCertKey,
                    OrderType, PhysicianKey, PhysicianAddressKey, OrderDate, ClinicianID, Inactive, Signed,
                    PhysicianChanged);

                if ((Admission.HospiceAdmission == false) && (OrderType == (int)OrderTypesEnum.POC)
                                                          && (Encounter != null)
                                                          && (!Admission.OrdersTracking.Any(ot =>
                                                                  (ot.OrderType == (int)OrderTypesEnum.FaceToFace)
                                                                  || ((ot.OrderType ==
                                                                       (int)OrderTypesEnum.FaceToFaceEncounter)
                                                                      && (ot.EncounterKey != Encounter.EncounterKey)
                                                                  )
                                                              )
                                                          )
                   )
                {
                    var i = Admission.Insurance;
                    if (i.FaceToFaceOnAdmit)
                    {
                        if (!i.IsHospiceOnly)
                        {
                            SetTrackingData(Encounter, Encounter.EncounterKey, Admission, AdmissionDocumentation,
                                PhysicianAddress, AdmissionCertKey, (int)OrderTypesEnum.FaceToFaceEncounter,
                                PhysicianKey, PhysicianAddressKey, OrderDate, ClinicianID, Inactive, Signed,
                                PhysicianChanged);
                        }
                        else
                        {
                            if ((otr != null) && (otr.AdmissionCertKey != 0) && (otr.Admission != null) &&
                                (otr.Admission.AdmissionCertification != null))
                            {
                                int certkey = otr.AdmissionCertKey;
                                var cert = Admission.AdmissionCertification.FirstOrDefault(ac => ac.AdmissionCertKey == certkey);
                                if ((cert != null) && (cert.PeriodNumber >= 3))
                                {
                                    SetTrackingData(Encounter, Encounter.EncounterKey, Admission,
                                        AdmissionDocumentation, PhysicianAddress, certkey,
                                        (int)OrderTypesEnum.HospiceFaceToFace, PhysicianKey, PhysicianAddressKey,
                                        OrderDate, ClinicianID, Inactive, Signed, PhysicianChanged);
                                }
                            }
                        }
                    }
                }
            }

            // change history on physician change if need be
            if ((otr != null) && (PhysicianChanged) && (SetTrackingDataInsert == false))
            {
                otr.SetupNoteChangePhysician((int)OriginalPhysicianKey, OriginalPhysicianAddressKey,
                    (int)NewPhysicianKey, NewPhysicianAddressKey);
            }

            return otr;
        }

        private bool SetTrackingDataInsert;

        public bool SetTrackingData(Entity Order, int OrderKey, Admission a, AdmissionDocumentation ad,
            PhysicianAddress pa, int AdmissionCertKey, int OrderType, int PhysicianKey, int? PhysicianAddressKey,
            DateTime OrderDate,
            Guid? ClinicianID, bool Inactive, bool? Signed, bool PhysicianChanged)
        {
            bool success = true;
            if (IsValidOrderType(OrderType)
               )
            {
                SetTrackingDataInsert = false;

                if (a != null)
                {
                    int? encounterPlanOfCareKey = null;
                    int? orderEntryKey = null;
                    int? admissionFaceToFaceKey = null;
                    int? admissionCOTIKey = null;
                    int? encounterKey = null;
                    int? admissionSignedInterimOrderKey = null;

                    SetKeys(Order, OrderType, OrderKey, out encounterPlanOfCareKey, out orderEntryKey,
                        out admissionFaceToFaceKey, out admissionCOTIKey, out encounterKey,
                        out admissionSignedInterimOrderKey);
                    OrdersTracking ot = FindOrdersTrackingRow(Order, OrderType, OrderKey);
                    if (ot != null)
                    {
                        // we have the row so we're just going to udpate it
                        SetTrackingDataInsert = false;

                        int? portal = CodeLookupCache.GetKeyFromCode("OrderDelivery", "Portal");

                        if ((ot.PhysicianKey != PhysicianKey) && (portal != null && portal.Value == ot.DeliveryMethod))
                        {
                            ot.OrdersTrackingCancelled.Add(new OrdersTrackingCancelled
                            {
                                TenantID = ot.TenantID,
                                OrderID = ot.OrderId,
                                PhysicianKey = ot.PhysicianKey,
                                PhysicianAddressKey = (int)ot.PhysicianAddressKey,
                                CancelReason = CodeLookupCache.GetKeyFromCode("OrderCancelReason", "MDReplaced"),
                                DeliveryMethod = (int)ot.DeliveryMethod,
                                CancelDate = ot.CancelDate,
                                UpdatedDate = DateTime.UtcNow
                            });
                        }
                        else if (Signed == true && (portal != null &&
                                                    (portal != null && portal.Value == ot.DeliveryMethod) &&
                                                    ot.CancelReason ==
                                                    CodeLookupCache.GetKeyFromCode("OrderCancelReason",
                                                        "SignedOnPaper")))
                        {
                            ot.OrdersTrackingCancelled.Add(new OrdersTrackingCancelled
                            {
                                TenantID = ot.TenantID,
                                OrderID = ot.OrderId,
                                PhysicianKey = ot.PhysicianKey,
                                PhysicianAddressKey = (int)ot.PhysicianAddressKey,
                                CancelReason = CodeLookupCache.GetKeyFromCode("OrderCancelReason", "SignedOnPaper"),
                                DeliveryMethod = (int)ot.DeliveryMethod,
                                CancelDate = ot.CancelDate,
                                UpdatedDate = DateTime.UtcNow
                            });
                        }
                    }
                    else
                    {
                        // no orders tracking row exists so we need to create a new one
                        ot = new OrdersTracking
                        {
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = WebContext.Current.User.MemberID,
                            UpdatedBy = WebContext.Current.User.MemberID
                        };
                        SetTrackingDataInsert = true;
                    }

                    SetKeys(Order, OrderType, OrderKey, out encounterPlanOfCareKey, out orderEntryKey,
                        out admissionFaceToFaceKey, out admissionCOTIKey, out encounterKey,
                        out admissionSignedInterimOrderKey);
                    ot.BeginEditting();
                    ot.EncounterPlanOfCareKey = encounterPlanOfCareKey;
                    ot.OrderEntryKey = orderEntryKey;
                    ot.AdmissionFaceToFaceKey = admissionFaceToFaceKey;
                    ot.AdmissionCOTIKey = admissionCOTIKey;
                    ot.EncounterKey = encounterKey;
                    ot.AdmissionKey = a.AdmissionKey;
                    ot.PatientKey = a.PatientKey;
                    ot.OrderDate = OrderDate.Date;
                    ot.ServiceLineKey = a.ServiceLineKey;
                    ot.ClinicianID = ClinicianID;
                    ot.PhysicianKey = PhysicianKey;
                    ot.PhysicianAddressKey = PhysicianAddressKey;
                    ot.AdmissionSignedInterimOrderKey = admissionSignedInterimOrderKey;
                    ot.NotMyOrder = false;
                    ot.NotMyPatient = false;
                    ot.Status = GetOrderStatus(a, ad, Order, OrderType, OrderKey, PhysicianChanged, ot);
                    if (ot.StatusDate == null)
                    {
                        ot.StatusDate = DateTime.Today.Date; // assure that a StatusDate is set if one does not exist
                    }

                    if (pa != null)
                    {
                        ot.DeliveryMethod = pa.OrderDeliveryMethod;
                    }

                    ot.Destination = null;
                    ot.CancelReason = null;
                    ot.CancelDate = null;
                    ot.OrderType = OrderType;
                    ot.AdmissionCertKey = AdmissionCertKey;
                    ot.ServiceLineGroupingKey0 = a.AdmissionGroup.Where(ag => (!ag.StartDate.HasValue || (ag.StartDate.Value.Date <= OrderDate.Date))
                                                                              && (!ag.EndDate.HasValue || (ag.EndDate.Value.Date >= OrderDate.Date))
                                                                              && (ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey) != null)
                                                                              && (ServiceLineCache.GetServiceLineGroupHeaderFromKey(
                                                                                          ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey).ServiceLineGroupHeaderKey) != null)
                                                                              && (ServiceLineCache.GetServiceLineGroupHeaderFromKey(
                                                                                      ServiceLineCache.GetServiceLineGroupingFromKey(
                                                                                              ag.ServiceLineGroupingKey).ServiceLineGroupHeaderKey)
                                                                                  .SequenceNumber == 0)
                        )
                        .Select(ag => ag.ServiceLineGroupingKey)
                        .FirstOrDefault();

                    ot.ServiceLineGroupingKey1 = a.AdmissionGroup.Where(ag => (!ag.StartDate.HasValue || (ag.StartDate.Value.Date <= OrderDate.Date))
                                                                              && (!ag.EndDate.HasValue || (ag.EndDate.Value.Date >= OrderDate.Date))
                                                                              && (ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey) != null)
                                                                              && (ServiceLineCache.GetServiceLineGroupHeaderFromKey(
                                                                                          ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey).ServiceLineGroupHeaderKey) != null)
                                                                              && (ServiceLineCache.GetServiceLineGroupHeaderFromKey(
                                                                                      ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey).ServiceLineGroupHeaderKey)
                                                                                  .SequenceNumber == 1)
                        )
                        .Select(ag => ag.ServiceLineGroupingKey)
                        .FirstOrDefault();

                    ot.ServiceLineGroupingKey2 = a.AdmissionGroup.Where(ag => (!ag.StartDate.HasValue || (ag.StartDate.Value.Date <= OrderDate.Date))
                                                                              && (!ag.EndDate.HasValue || (ag.EndDate.Value.Date >= OrderDate.Date))
                                                                              && (ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey) != null)
                                                                              && (ServiceLineCache.GetServiceLineGroupHeaderFromKey(
                                                                                      ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey).ServiceLineGroupHeaderKey) != null)
                                                                              && (ServiceLineCache.GetServiceLineGroupHeaderFromKey(
                                                                                      ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey).ServiceLineGroupHeaderKey)
                                                                                  .SequenceNumber == 2)
                        )
                        .Select(ag => ag.ServiceLineGroupingKey)
                        .FirstOrDefault();

                    ot.ServiceLineGroupingKey3 = a.AdmissionGroup.Where(ag => (!ag.StartDate.HasValue || (ag.StartDate.Value.Date <= OrderDate.Date))
                                                                              && (!ag.EndDate.HasValue || (ag.EndDate.Value.Date >= OrderDate.Date))
                                                                              && (ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey) != null)
                                                                              && (ServiceLineCache.GetServiceLineGroupHeaderFromKey(
                                                                                          ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey).ServiceLineGroupHeaderKey) != null)
                                                                              && (ServiceLineCache.GetServiceLineGroupHeaderFromKey(
                                                                                      ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey).ServiceLineGroupHeaderKey)
                                                                                  .SequenceNumber == 3)
                        )
                        .Select(ag => ag.ServiceLineGroupingKey)
                        .FirstOrDefault();

                    ot.ServiceLineGroupingKey4 = a.AdmissionGroup.Where(ag => (!ag.StartDate.HasValue || (ag.StartDate.Value.Date <= OrderDate.Date))
                                                                              && (!ag.EndDate.HasValue || (ag.EndDate.Value.Date >= OrderDate.Date))
                                                                              && (ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey) != null)
                                                                              && (ServiceLineCache.GetServiceLineGroupHeaderFromKey(
                                                                                          ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey).ServiceLineGroupHeaderKey) != null)
                                                                              && (ServiceLineCache.GetServiceLineGroupHeaderFromKey(
                                                                                      ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey).ServiceLineGroupHeaderKey)
                                                                                  .SequenceNumber == 4)
                        )
                        .Select(ag => ag.ServiceLineGroupingKey)
                        .FirstOrDefault();

                    ot.ServiceLineGroupingKey0 = (ot.ServiceLineGroupingKey0 > 0) ? ot.ServiceLineGroupingKey0 : null;
                    ot.ServiceLineGroupingKey1 = (ot.ServiceLineGroupingKey1 > 0) ? ot.ServiceLineGroupingKey1 : null;
                    ot.ServiceLineGroupingKey2 = (ot.ServiceLineGroupingKey2 > 0) ? ot.ServiceLineGroupingKey2 : null;
                    ot.ServiceLineGroupingKey3 = (ot.ServiceLineGroupingKey3 > 0) ? ot.ServiceLineGroupingKey3 : null;
                    ot.ServiceLineGroupingKey4 = (ot.ServiceLineGroupingKey4 > 0) ? ot.ServiceLineGroupingKey4 : null;

                    ot.Inactive = Inactive;
                    ot.EndEditting();
                    if (SetTrackingDataInsert)
                    {
                        switch (OrderType)
                        {
                            case (int)OrderTypesEnum.CoTI:
                            case (int)OrderTypesEnum.HospiceFaceToFace:
                                AdmissionCOTI coti = Order as AdmissionCOTI;
                                if (coti != null)
                                {
                                    coti.OrdersTracking.Add(ot);
                                }

                                break;
                            case (int)OrderTypesEnum.FaceToFace:
                                AdmissionFaceToFace f2f = Order as AdmissionFaceToFace;
                                if (f2f != null)
                                {
                                    f2f.OrdersTracking.Add(ot);
                                }

                                break;
                            case (int)OrderTypesEnum.FaceToFaceEncounter:
                                Encounter e = Order as Encounter;
                                if (e != null)
                                {
                                    e.OrdersTracking.Add(ot);
                                }

                                break;
                            case (int)OrderTypesEnum.InterimOrder:
                                OrderEntry oe = Order as OrderEntry;
                                if (oe != null)
                                {
                                    oe.OrdersTracking.Add(ot);
                                }

                                break;
                            case (int)OrderTypesEnum.HospiceElectionAddendum:
                                e = Order as Encounter;
                                if (e != null)
                                {
                                    e.OrdersTracking.Add(ot);
                                }

                                break;
                            case (int)OrderTypesEnum.POC:
                                EncounterPlanOfCare poc = Order as EncounterPlanOfCare;
                                if (poc != null)
                                {
                                    poc.OrdersTracking.Add(ot);
                                }

                                if ((poc.Encounter != null)
                                    && (poc.Encounter.OrdersTracking != null)
                                   )
                                {
                                    poc.Encounter.OrdersTracking.Add(ot);
                                }

                                break;
                        }
                    }
                }
            }

            return success;
        }

        private int GetOrderStatus(Admission Admission, AdmissionDocumentation AdmissionDocumentation, Entity Order,
            int orderType, int orderKey, bool PhysicianChanged, OrdersTracking ordersTracking)
        {
            int? encounterPlanOfCareKey = null;
            int? orderEntryKey = null;
            int? admissionFaceToFaceKey = null;
            int? admissionCOTIKey = null;
            int? encounterKey = null;
            int? admissionSignedInterimOrderKey = null;
            int status = 0;

            if (IsValidOrderType(orderType))
            {
                SetKeys(Order, orderType, orderKey, out encounterPlanOfCareKey, out orderEntryKey,
                    out admissionFaceToFaceKey, out admissionCOTIKey, out encounterKey,
                    out admissionSignedInterimOrderKey);

                OrderTypesEnum type = (OrderTypesEnum)orderType;

                switch (type)
                {
                    case (OrderTypesEnum.CoTI):
                        AdmissionCOTI coti = Order as AdmissionCOTI;

                        if ((AdmissionDocumentation == null) && (coti != null) && coti.IsCOTI &&
                            (coti.AttestationSignature != null) && (ordersTracking != null) &&
                            (ordersTracking.Status == 0))
                        {
                            status = (int)OrdersTrackingStatus.Signed; // Electronic CTI
                        }

                        if ((AdmissionDocumentation != null) && (coti != null) && (ordersTracking != null) &&
                            (ordersTracking.Status == 0))
                        {
                            status = (int)OrdersTrackingStatus.Signed; // Attached CTIs and F2F
                        }

                        if (status == 0)
                        {
                            status = (ordersTracking == null)
                                ? (int)OrdersTrackingStatus.Complete
                                : ordersTracking.Status; // ReEdit preserve status
                        }

                        break;
                    case (OrderTypesEnum.FaceToFace):
                        AdmissionFaceToFace f2f = Order as AdmissionFaceToFace;
                        if (f2f != null)
                        {
                            if (f2f.AllIsValid && (AdmissionDocumentation != null) &&
                                (AdmissionDocumentation.Inactive == false))
                            {
                                status = (int)OrdersTrackingStatus.Signed;
                            }
                            else
                            {
                                status = ((Admission != null) && Admission.RequiresFaceToFaceOnAdmit)
                                    ? (int)OrdersTrackingStatus.Sent
                                    : 0;
                            }
                        }

                        break;
                    case (OrderTypesEnum.FaceToFaceEncounter):
                        Encounter encounter = Order as Encounter;

                        if (encounter != null)
                        {
                            OrdersTracking ot = encounter.OrdersTracking
                                .FirstOrDefault(o => ((o.OrderType == (int)OrderTypesEnum.FaceToFace) || (o.OrderType == (int)OrderTypesEnum.FaceToFaceEncounter))
                                && (o.EncounterKey == encounter.EncounterKey)
                                && (o.AdmissionFaceToFaceKey.HasValue));

                            if ((ot != null)
                                && (ot.AdmissionFaceToFace != null)
                                && (ot.AdmissionFaceToFace.DatedSignaturePresent != null)
                                && (ot.AdmissionFaceToFace.DatedSignaturePresent.Value)
                               )
                            {
                                status = (int)OrdersTrackingStatus.Complete;
                            }
                            else
                            {
                                // This accounts for F2FEncounters where an AdmissionDocumentation has not been uploaded and thus there is no AdmissionF2FKey
                                OrdersTracking otWithoutDocumentUpload = encounter.OrdersTracking
                                    .Where(o =>
                                        ((o.OrderType == (int)OrderTypesEnum.FaceToFace ||
                                          o.OrderType == (int)OrderTypesEnum.FaceToFaceEncounter) &&
                                         (o.Inactive == false) && (o.EncounterKey == encounter.EncounterKey)))
                                    .FirstOrDefault();

                                if (otWithoutDocumentUpload != null &&
                                    otWithoutDocumentUpload.Status == (int)OrdersTrackingStatus.Sent)
                                {
                                    return (int)OrdersTrackingStatus.Sent;
                                }

                                if (otWithoutDocumentUpload != null &&
                                    otWithoutDocumentUpload.Status == (int)OrdersTrackingStatus.Void)
                                {
                                    return (int)OrdersTrackingStatus.Void;
                                }

                                EncounterStatusType encStatus = (EncounterStatusType)encounter.EncounterStatus;
                                switch (encStatus)
                                {
                                    case (EncounterStatusType.Edit):
                                        status = (int)OrdersTrackingStatus.Edit;
                                        break;
                                    case (EncounterStatusType.CoderReview):
                                        status = (int)OrdersTrackingStatus.ReadyForReview;
                                        break;
                                    case (EncounterStatusType.Completed):

                                        status = (int)OrdersTrackingStatus.Complete;
                                        break;
                                }
                            }
                        }

                        break;
                    case (OrderTypesEnum.HospiceFaceToFace):
                        AdmissionCOTI hospF2F = Order as AdmissionCOTI;

                        if (hospF2F != null)
                        {
                            status = (int)OrdersTrackingStatus.Signed;
                        }

                        break;
                    case (OrderTypesEnum.InterimOrder):
                        var orderEntry = Order as OrderEntry;

                        if (orderEntry != null)
                        {
                            OrderStatusType orderStatus = (OrderStatusType)orderEntry.OrderStatus;
                            switch (orderStatus)
                            {
                                case (OrderStatusType.InProcess):
                                    status = (int)OrdersTrackingStatus.Edit;
                                    break;
                                case (OrderStatusType.OrderEntryReview):
                                    status = (int)OrdersTrackingStatus.ReadyForReview;
                                    break;
                                case (OrderStatusType.Completed):
                                    if (orderEntry != null && orderEntry.OrderVerifiedSigned &&
                                        orderEntry.OrderVerifiedSignedDate.HasValue)
                                    {
                                        status = (int)OrdersTrackingStatus.Signed;
                                        orderEntry.OrderStatus = (int)OrderStatusType.SigningPhysicianVerified;
                                    }
                                    else if (orderEntry != null &&
                                             (orderEntry.OrderSent || orderEntry.OrderSentDate.HasValue))
                                    {
                                        if (PhysicianChanged)
                                        {
                                            orderEntry.OrderSent = false;
                                            orderEntry.OrderSentDate = null;
                                        }

                                        status = (PhysicianChanged)
                                            ? (int)OrdersTrackingStatus.Complete
                                            : (int)OrdersTrackingStatus.Sent;
                                    }
                                    else
                                    {
                                        status = (int)OrdersTrackingStatus.Complete;
                                    }

                                    break;
                                case (OrderStatusType.SigningPhysicianVerified):
                                    status = (int)OrdersTrackingStatus.Signed;
                                    break;
                                case (OrderStatusType.Voided):
                                    status = (int)OrdersTrackingStatus.Void;
                                    break;
                            }
                        }

                        break;
                    case (OrderTypesEnum.HospiceElectionAddendum):

                        if ((ordersTracking != null) && (ordersTracking.Status != 0))
                        {
                            status = ordersTracking.Status;
                        }
                        else
                        {
                            Encounter ehEncounter = Order as Encounter;

                            if (ehEncounter != null)
                            {
                                EncounterStatusType encStatus = (EncounterStatusType)ehEncounter.EncounterStatus;
                                switch (encStatus)
                                {
                                    case (EncounterStatusType.Edit):
                                        status = (int)OrdersTrackingStatus.Edit;
                                        break;
                                    case (EncounterStatusType.Completed):
                                        status = (int)OrdersTrackingStatus.Complete;
                                        break;
                                }
                            }
                        }

                        break;

                    case (OrderTypesEnum.POC):
                        EncounterPlanOfCare encounterPlanOfCare = Order as EncounterPlanOfCare;

                        if (encounterPlanOfCare != null)
                        {
                            var myEncounter = encounterPlanOfCare.Encounter;
                            if (myEncounter != null)
                            {
                                EncounterStatusType encStatus = (EncounterStatusType)myEncounter.EncounterStatus;
                                switch (encStatus)
                                {
                                    case (EncounterStatusType.Edit):
                                        status = (int)OrdersTrackingStatus.Edit;
                                        break;
                                    case (EncounterStatusType.CoderReview):
                                        status = (int)OrdersTrackingStatus.ReadyForReview;
                                        break;
                                    case (EncounterStatusType.Completed):
                                        if (encounterPlanOfCare.SignedDate.HasValue)
                                        {
                                            status = (int)OrdersTrackingStatus.Signed;
                                        }
                                        else if (encounterPlanOfCare.PrintedBy.HasValue ||
                                                 encounterPlanOfCare.PrintedDate.HasValue ||
                                                 encounterPlanOfCare.MailedBy.HasValue ||
                                                 encounterPlanOfCare.MailedDate.HasValue ||
                                                 encounterPlanOfCare.IsPrinted)
                                        {
                                            if (PhysicianChanged)
                                            {
                                                encounterPlanOfCare.IsPrinted = false;
                                                encounterPlanOfCare.PrintedBy = null;
                                                encounterPlanOfCare.PrintedDate = null;
                                                encounterPlanOfCare.MailedBy = null;
                                                encounterPlanOfCare.MailedDate = null;
                                            }

                                            status = (PhysicianChanged)
                                                ? (int)OrdersTrackingStatus.Complete
                                                : (int)OrdersTrackingStatus.Sent;
                                        }
                                        else
                                        {
                                            status = (int)OrdersTrackingStatus.Complete;
                                        }

                                        break;
                                }
                            }
                        }

                        break;
                }
            }

            return status;
        }

        private bool CancelOrder(OrdersTracking ordersTracking, int cancelReason, DateTime cancelDate)
        {
            bool success = false;

            if (ordersTracking != null)
            {
                ordersTracking.CancelReason = cancelReason;
                ordersTracking.CancelDate = cancelDate;
            }

            CreateHistoryRowForCancel(ordersTracking, cancelReason, cancelDate);
            return success;
        }

        private bool SetKeys(Entity Order, int orderType, int orderKey, out int? encounterPlanOfCareKey,
            out int? orderEntryKey, out int? admissionFaceToFaceKey, out int? admissionCOTIKey, out int? encounterKey,
            out int? admissionSignedInterimOrderKey)
        {
            bool success = IsValidOrderType(orderType);

            encounterPlanOfCareKey = null;
            orderEntryKey = null;
            encounterPlanOfCareKey = null;
            admissionFaceToFaceKey = null;
            admissionCOTIKey = null;
            encounterKey = null;
            admissionSignedInterimOrderKey = null;

            if (success)
            {
                OrderTypesEnum type = (OrderTypesEnum)orderType;
                switch (type)
                {
                    case OrderTypesEnum.CoTI:
                    case OrderTypesEnum.HospiceFaceToFace:
                        admissionCOTIKey = orderKey;
                        break;
                    case OrderTypesEnum.FaceToFace:
                        AdmissionFaceToFace f2f = Order as AdmissionFaceToFace;
                        if ((f2f != null)
                            && (f2f.Admission != null)
                            && (f2f.Admission.OrdersTracking != null)
                            && f2f.Admission.OrdersTracking.Any()
                           )
                        {
                            OrdersTracking ot = f2f.Admission.OrdersTracking.Where(o =>
                                (o.OrderType == (int)OrderTypesEnum.FaceToFace)
                                || (o.OrderType == (int)OrderTypesEnum.FaceToFaceEncounter)
                            ).FirstOrDefault();
                            if (ot != null)
                            {
                                encounterKey = ot.EncounterKey;
                            }
                        }

                        admissionFaceToFaceKey = orderKey;
                        break;
                    case OrderTypesEnum.FaceToFaceEncounter:
                        Encounter e = Order as Encounter;
                        if ((e != null)
                            && (e.OrdersTracking != null)
                            && e.OrdersTracking.Any()
                           )
                        {
                            OrdersTracking ot = e.OrdersTracking.FirstOrDefault();
                            if (ot != null)
                            {
                                admissionFaceToFaceKey = ot.AdmissionFaceToFaceKey;
                            }
                        }

                        encounterKey = orderKey;
                        break;
                    case OrderTypesEnum.InterimOrder:
                        orderEntryKey = orderKey;
                        var oe = Order as OrderEntry;
                        encounterKey = (oe == null) ? null : oe.AddedFromEncounterKey;
                        break;
                    case OrderTypesEnum.HospiceElectionAddendum:
                        e = Order as Encounter;
                        encounterKey = (e == null) ? (int?)null : e.EncounterKey;
                        break;
                    case OrderTypesEnum.POC:
                        encounterPlanOfCareKey = orderKey;
                        var poc = Order as EncounterPlanOfCare;
                        if (poc != null)
                        {
                            encounterKey = poc.EncounterKey;
                        }

                        break;
                }
            }

            return success;
        }

        private OrdersTracking FindOrdersTrackingRow(Entity order, int orderType, int orderKey)
        {
            OrdersTracking ot = null;
            if (OrdersTrackingHelpers.IsOrderTypeValid(orderType))
            {
                switch (orderType)
                {
                    case (int)OrderTypesEnum.CoTI:
                    case (int)OrderTypesEnum.HospiceFaceToFace:
                        AdmissionCOTI coti = order as AdmissionCOTI;
                        if ((coti != null)
                            && (coti.OrdersTracking != null)
                           )
                        {
                            ot = coti.OrdersTracking.FirstOrDefault();
                        }

                        break;
                    case (int)OrderTypesEnum.FaceToFace:
                        AdmissionFaceToFace f2f = order as AdmissionFaceToFace;
                        if ((f2f != null)
                            && (f2f.Admission != null)
                            && (f2f.Admission.OrdersTracking != null)
                           )
                        {
                            ot = f2f.Admission.OrdersTracking.Where(f =>
                                ((f.OrderType == (int)OrderTypesEnum.FaceToFaceEncounter)
                                 || (f.OrderType == (int)OrderTypesEnum.FaceToFace)
                                )
                            ).FirstOrDefault();
                        }

                        break;
                    case (int)OrderTypesEnum.FaceToFaceEncounter:
                        Encounter e = order as Encounter;
                        // First check for a OrderTypesEnum.FaceToFaceEncounter on this encounter
                        if ((e != null) && (e.OrdersTracking != null))
                        {
                            ot = e.OrdersTracking.Where(f => (f.OrderType == (int)OrderTypesEnum.FaceToFaceEncounter))
                                .FirstOrDefault();
                        }

                        if (ot == null)
                        {
                            // Then check for a OrderTypesEnum.FaceToFace or FaceToFaceEncounter on the admission
                            if ((e != null) && (e.Admission != null) && (e.Admission.OrdersTracking != null))
                            {
                                ot = e.Admission.OrdersTracking
                                    .Where(f => ((f.OrderType == (int)OrderTypesEnum.FaceToFaceEncounter) ||
                                                 (f.OrderType == (int)OrderTypesEnum.FaceToFace)))
                                    .FirstOrDefault();
                            }
                        }

                        break;
                    case (int)OrderTypesEnum.InterimOrder:
                        OrderEntry oe = order as OrderEntry;
                        if ((oe != null)
                            && (oe.OrdersTracking != null)
                           )
                        {
                            ot = oe.OrdersTracking.FirstOrDefault();
                        }

                        break;
                    case (int)OrderTypesEnum.HospiceElectionAddendum:
                        e = order as Encounter;
                        if ((e != null) && (e.OrdersTracking != null))
                        {
                            ot = e.OrdersTracking
                                .Where(f => (f.OrderType == (int)OrderTypesEnum.HospiceElectionAddendum))
                                .FirstOrDefault();
                        }

                        break;
                    case (int)OrderTypesEnum.POC:
                        EncounterPlanOfCare poc = order as EncounterPlanOfCare;
                        if ((poc != null)
                            && (poc.OrdersTracking != null)
                           )
                        {
                            ot = poc.OrdersTracking.Where(o => o.OrderType == (int)OrderTypesEnum.POC).FirstOrDefault();
                        }

                        break;
                }
            }

            return ot;
        }


        private void CreateHistoryRow(OrdersTracking orig, OrdersTracking curr)
        {
            bool create = false;
            OrdersTrackingHistory history = new OrdersTrackingHistory();
            history.OrdersTrackingKey = curr.OrdersTrackingKey;
            if (orig.ServiceLineKey != curr.ServiceLineKey)
            {
                create = true;
                history.ServiceLineKey = orig.ServiceLineKey;
            }

            if (orig.ClinicianID != curr.ClinicianID)
            {
                create = true;
                history.ClinicianID = orig.ClinicianID;
            }

            if (orig.PhysicianKey != curr.PhysicianKey)
            {
                create = true;
                history.PhysicianKey = orig.PhysicianKey;
            }

            if (orig.PhysicianAddressKey != curr.PhysicianAddressKey)
            {
                create = true;
                history.PhysicianAddressKey = orig.PhysicianAddressKey;
            }

            if (orig.DeliveryMethod != curr.DeliveryMethod)
            {
                create = true;
                history.DeliveryMethod = orig.DeliveryMethod;
            }

            if (orig.OrderDate.Date != curr.OrderDate.Date)
            {
                create = true;
                history.OrderDate = orig.OrderDate;
            }

            if (orig.NotMyPatient != curr.NotMyPatient)
            {
                create = true;
                history.NotMyPatient = orig.NotMyPatient;
            }

            if (orig.NotMyOrder != curr.NotMyOrder)
            {
                create = true;
                history.NotMyOrder = orig.NotMyOrder;
            }

            if (orig.Destination != curr.Destination)
            {
                create = true;
                history.Destination = orig.Destination;
            }

            if (orig.AdmissionCertKey != curr.AdmissionCertKey)
            {
                create = true;
                history.AdmissionCertKey = orig.AdmissionCertKey;
            }

            if (orig.EncounterPlanOfCareKey != curr.EncounterPlanOfCareKey)
            {
                create = true;
                history.EncounterPlanOfCareKey = orig.EncounterPlanOfCareKey;
            }

            if (orig.OrderEntryKey != curr.OrderEntryKey)
            {
                create = true;
                history.OrderEntryKey = orig.OrderEntryKey;
            }

            if (orig.AdmissionFaceToFaceKey != curr.AdmissionFaceToFaceKey)
            {
                create = true;
                history.AdmissionFaceToFaceKey = orig.AdmissionFaceToFaceKey;
            }

            if (orig.AdmissionCOTIKey != curr.AdmissionCOTIKey)
            {
                create = true;
                history.AdmissionCOTIKey = orig.AdmissionCOTIKey;
            }

            if (orig.EncounterKey != curr.EncounterKey)
            {
                create = true;
                history.EncounterKey = orig.EncounterKey;
            }

            if (orig.Status != curr.Status)
            {
                create = true;
                history.Status = orig.Status;
            }

            if (orig.Inactive != curr.Inactive)
            {
                create = true;
                history.Inactive = orig.Inactive;
            }

            if (create)
            {
                SetHistoryInfo(orig, history);
            }
        }

        private void CreateHistoryRowForCancel(OrdersTracking orig, int cancelReason, DateTime cancelDate)
        {
            OrdersTrackingHistory history = new OrdersTrackingHistory();
            history.CancelReason = cancelReason;
            history.CancelDate = cancelDate;
            SetHistoryInfo(orig, history);
        }

        private void SetHistoryInfo(OrdersTracking orig, OrdersTrackingHistory history)
        {
            history.TenantID = orig.TenantID;
            history.AdmissionKey = orig.AdmissionKey;
            history.PatientKey = orig.PatientKey;
            history.EncounterPlanOfCareKey = orig.EncounterPlanOfCareKey;
            history.OrderEntryKey = orig.OrderEntryKey;
            history.OrderType = orig.OrderType;
            history.AdmissionFaceToFaceKey = orig.AdmissionFaceToFaceKey;
            history.AdmissionCOTIKey = orig.AdmissionCOTIKey;
            history.EncounterKey = orig.EncounterKey;
            history.UpdatedDate = orig.UpdatedDate;
            history.UpdatedBy = orig.UpdatedBy;
        }

        private bool IsValidOrderType(int orderType)
        {
            bool valid = OrdersTrackingHelpers.IsOrderTypeValid(orderType);
            return valid;
        }
    }
}