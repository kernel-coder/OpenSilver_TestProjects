using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Virtuoso.Server.Data
{
    public enum OrderStatusType
    {
        InProcess = 1,
        OrderEntryReview = 2,
        Completed = 3,
        SigningPhysicianVerified = 4,
        Voided = 5,
    };

    public enum OrderTypesEnum
    {
        POC = 1,
        InterimOrder = 2,
        FaceToFace = 3,
        HospiceFaceToFace = 4,
        CoTI = 5,
        FaceToFaceEncounter = 6,
        HospiceElectionAddendum = 7
    };


    public enum OrdersTrackingStatus
    {
        Edit = 10,
        ReadyForReview = 20,
        Complete = 30,
        ReadyForPrint = 35,
        Sent = 40,
        Signed = 50,
        Void = 60
    }

    public class OrdersTrackingHelpers
    {
        public static bool IsOrderTypeValid(int orderType)
        {
            return (Enum.GetValues(typeof(OrderTypesEnum)) as int[]).Any(i => i == orderType);
        }

        public static string OrderTypeDescription(OrderTypesEnum orderType)
        {
            string description = null;

            switch (orderType)
            {
                case OrderTypesEnum.POC:
                    description = "Plan of Care";
                    break;
                case OrderTypesEnum.InterimOrder:
                    description = "Interim Order";
                    break;
                case OrderTypesEnum.FaceToFace:
                case OrderTypesEnum.FaceToFaceEncounter:
                    description = "Face to Face";
                    break;
                case OrderTypesEnum.HospiceFaceToFace:
                    description = "Hospice Face To Face";
                    break;
                case OrderTypesEnum.CoTI:
                    description = "CTI";
                    break;
            }
            return description;
        }

        public static string OrderTypeDescription(int orderTypeInt)
        {
            string description = null;

            if (IsOrderTypeValid(orderTypeInt))
            {
                description = OrderTypeDescription((OrderTypesEnum)orderTypeInt);
            }
            return description;
        }


        public static bool IsStatusValid(int status)
        {
            return (Enum.GetValues(typeof(OrdersTrackingStatus)) as int[]).Any(i => i == status);
        }

        public static string StatusDescription(OrdersTrackingStatus status)
        {
            string description = null;

            switch (status)
            {
                case OrdersTrackingStatus.Edit:
                    description = "Edit";
                    break;
                case OrdersTrackingStatus.ReadyForReview:
                    description = "Ready To Review";
                    break;
                case OrdersTrackingStatus.Complete:
                    description = "Complete";
                    break;
                case OrdersTrackingStatus.ReadyForPrint:
                    description = "Ready To Print";
                    break;
                case OrdersTrackingStatus.Sent:
                    description = "Sent";
                    break;
                case OrdersTrackingStatus.Signed:
                    description = "Signed";
                    break;
                case OrdersTrackingStatus.Void:
                    description = "Void";
                    break;
            }
            return description;
        }

        public static string StatusDescription(int statusInt)
        {
            string description = null;

            if (IsStatusValid(statusInt))
            {
                description = StatusDescription((OrdersTrackingStatus)statusInt);
            }
            return description;
        }
        
        public static string StatusDescription(string statusString)
        {
            int statusInt = 0;
            try { statusInt = Int32.Parse(statusString); }
            catch { }
            string description = StatusDescription((OrdersTrackingStatus)statusInt);
            return (description == null) ? "Unknown" : description;
        }
    }
}
