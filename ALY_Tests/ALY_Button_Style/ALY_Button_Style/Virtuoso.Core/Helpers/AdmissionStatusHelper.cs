#region Usings

using System;
using System.Diagnostics;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Core.Helpers
{
    public static class AdmissionStatusHelper
    {
        public sealed class AdmissionStatusType
        {
            public static readonly AdmissionStatusType Admitted = new AdmissionStatusType("A");
            public static readonly AdmissionStatusType Referred = new AdmissionStatusType("R");
            public static readonly AdmissionStatusType Discharged = new AdmissionStatusType("D");
            public static readonly AdmissionStatusType NotTaken = new AdmissionStatusType("N");
            public static readonly AdmissionStatusType Transferred = new AdmissionStatusType("T");
            public static readonly AdmissionStatusType Resumed = new AdmissionStatusType("M");
            public static readonly AdmissionStatusType OnHold = new AdmissionStatusType("H");

            private AdmissionStatusType(string value)
            {
                Value = value;
            }

            public string Value { get; private set; }
        }

        public sealed class AdmissionStatuses
        {
            public static readonly int Admitted = AdmissionStatusKey("AdmissionStatus", AdmissionStatusType.Admitted);

            public static readonly int Discharged =
                AdmissionStatusKey("AdmissionStatus", AdmissionStatusType.Discharged);

            public static readonly int NotTaken = AdmissionStatusKey("AdmissionStatus", AdmissionStatusType.NotTaken);
            public static readonly int OnHold = AdmissionStatusKey("AdmissionStatus", AdmissionStatusType.OnHold);
            public static readonly int Referred = AdmissionStatusKey("AdmissionStatus", AdmissionStatusType.Referred);
            public static readonly int Resumed = AdmissionStatusKey("AdmissionStatus", AdmissionStatusType.Resumed);

            public static readonly int Transferred =
                AdmissionStatusKey("AdmissionStatus", AdmissionStatusType.Transferred);

            private AdmissionStatuses(int value)
            {
                Value = value;
            }

            public int Value { get; private set; }

            private static int AdmissionStatusKey(string codeType, AdmissionStatusType Code)
            {
                int statusValue = -1;
                try
                {
                    statusValue = (int)CodeLookupCache.GetKeyFromCode(codeType, Code.Value);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception.Message);
                }


                if (statusValue == -1)
                {
                    //Todo: Should we add Logic to raise fatal exception
                    //Raise Fatal Error
                }

                return statusValue;
            }
        }

        #region "Status Properties"

        private static int __AdmissionStatus_Admitted = -1;

        public static int AdmissionStatus_Admitted
        {
            get
            {
                if (__AdmissionStatus_Admitted == -1)
                {
                    __AdmissionStatus_Admitted = AdmissionStatuses.Admitted;
                }

                return __AdmissionStatus_Admitted;
            }
        }

        private static int __AdmissionStatus_Referred = -1;

        public static int AdmissionStatus_Referred
        {
            get
            {
                if (__AdmissionStatus_Referred == -1)
                {
                    __AdmissionStatus_Referred = AdmissionStatuses.Referred;
                }

                return __AdmissionStatus_Referred;
            }
        }

        private static int __AdmissionStatus_Discharged = -1;

        public static int AdmissionStatus_Discharged
        {
            get
            {
                if (__AdmissionStatus_Discharged == -1)
                {
                    __AdmissionStatus_Discharged = AdmissionStatuses.Discharged;
                }

                return __AdmissionStatus_Discharged;
            }
        }

        private static int __AdmissionStatus_NotTaken = -1;

        public static int AdmissionStatus_NotTaken // { get { return AdmissionStatuses.NotTaken; } }
        {
            get
            {
                if (__AdmissionStatus_NotTaken == -1)
                {
                    __AdmissionStatus_NotTaken = AdmissionStatuses.NotTaken;
                }

                return __AdmissionStatus_NotTaken;
            }
        }

        private static int __AdmissionStatus_Transferred = -1;

        public static int AdmissionStatus_Transferred // { get { return AdmissionStatuses.Transferred; } }
        {
            get
            {
                if (__AdmissionStatus_Transferred == -1)
                {
                    __AdmissionStatus_Transferred = AdmissionStatuses.Transferred;
                }

                return __AdmissionStatus_Transferred;
            }
        }

        private static int __AdmissionStatus_Resumed = -1;

        public static int AdmissionStatus_Resumed // { get { return AdmissionStatuses.Resumed; } }
        {
            get
            {
                if (__AdmissionStatus_Resumed == -1)
                {
                    __AdmissionStatus_Resumed = AdmissionStatuses.Resumed;
                }

                return __AdmissionStatus_Resumed;
            }
        }

        private static int __AdmissionStatus_OnHold = -1;

        public static int AdmissionStatus_OnHold // { get { return AdmissionStatuses.OnHold; } }
        {
            get
            {
                if (__AdmissionStatus_OnHold == -1)
                {
                    __AdmissionStatus_OnHold = AdmissionStatuses.OnHold;
                }

                return __AdmissionStatus_OnHold;
            }
        }

        #endregion

        public static bool CanChangeToAdmittedStatus(int? currentAdmissionStatus)
        {
            bool isStatusChangeValid = false;

            if (currentAdmissionStatus != AdmissionStatus_Resumed &&
                currentAdmissionStatus != AdmissionStatus_Discharged &&
                currentAdmissionStatus != AdmissionStatus_Transferred)
            {
                isStatusChangeValid = true;
            }


            return isStatusChangeValid;
        }

        public static bool CanDisciplineChangeToAdmittedStatus(int? currentAdmissionStatus)
        {
            bool isStatusChangeValid = false;

            if (currentAdmissionStatus != AdmissionStatus_Resumed &&
                currentAdmissionStatus != AdmissionStatus_Discharged &&
                currentAdmissionStatus != AdmissionStatus_Transferred)
            {
                isStatusChangeValid = true;
            }


            return isStatusChangeValid;
        }

        public static bool CanChangeToNotTakenStatus(int? currentAdmissionStatus)
        {
            bool isStatusChangeValid = false;

            if (currentAdmissionStatus != AdmissionStatus_Transferred &&
                currentAdmissionStatus != AdmissionStatus_Discharged)
            {
                isStatusChangeValid = true;
            }


            return isStatusChangeValid;
        }
    }
}