#region Usings

using System;
using Virtuoso.Core.Occasional;

#endregion

namespace Virtuoso.Core.Utility
{
    public class NavigationUriBuilder
    {
        private static volatile NavigationUriBuilder instance;
        private static readonly object syncRoot = new Object();

        public static NavigationUriBuilder Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new NavigationUriBuilder();
                        }
                    }
                }

                return instance;
            }
        }

        //Uri="/DynamicFormReadOnly/{patient}/{admission}/{form}/{service}/{parenttask}/{task}/{encounter}/{readonlybit}"
        // if {readonlybit} is set then the user will not be able to save changes in dynamicform
        public string GetDynamicFormReadyOnlyURI(
            int patientKey,
            int admissionKey,
            int formKey,
            int svcTypeKey,
            int taskKey,
            int readonlyBit)
        {
            string uri = string.Empty;
            uri = string.Format("/DynamicFormReadOnly/{0}/{1}/{2}/{3}/{4}/{5}",
                patientKey,
                admissionKey,
                formKey,
                svcTypeKey,
                taskKey,
                readonlyBit);
            return uri;
        }

        public string GetDynamicFormAttemptedURI(
            int patientKey,
            int admissionKey,
            int formKey,
            int svcTypeKey,
            int taskKey,
            int attemptedBit)
        {
            string uri = string.Empty;
            uri = string.Format("/DynamicFormAttempted/{0}/{1}/{2}/{3}/{4}/{5}/{6}",
                patientKey,
                admissionKey,
                formKey,
                svcTypeKey,
                taskKey,
                0,
                attemptedBit);
            return uri;
        }

        public string GetDynamicFormURI(int patientKey, int admissionKey, int formKey, int svcTypeKey, int taskKey)
        {
            string uri = string.Empty;
            //uri = string.Format("/DynamicForm/{0}/{1}/{2}/{3}/{4}/{5}", patientKey, admissionKey, formKey, svcTypeKey, taskKey, encounterKey);
            uri = string.Format("/DynamicForm/{0}/{1}/{2}/{3}/{4}", patientKey, admissionKey, formKey, svcTypeKey,
                taskKey);
            return uri;
        }

        public string GetOrderEntryURI(int admissionKey, int sourceKey)
        {
            string uri = string.Empty;
            uri = string.Format("/OrderEntry/{0}/{1}", admissionKey, sourceKey);
            return uri;
        }

        //NOTE: //Pass in a negative value for the tab index so the view model can control the tab switching 'after' the data has been loaded.
        //TODO: turn parameter 1 into an ENUM
        public string GetAdmissionMaintenanceURI(int tabNumber, int patientKey, int admissionKey)
        {
            string uri = string.Empty;

            //Uri="/MaintenanceAdmission/{tab}/{patient}/{admission}"

            //Tab 1 (SelectedIndex = 0) – Details (Patient)
            //Tab 2 – Admission/Referral
            //Tab 3 – Physicians
            //Tab 4 – Services
            //Tab 5 - Coverages
            //Tab 6 – Authorizations
            //Tab 7 – Order Entry
            //Tab 8 – Communications
            //Tab 9 – Documentation
            //Tab 10 – OASIS

            //uri = string.Format("/MaintenanceAdmission/1/{0}/{1}", patient.PatientKey.ToString(), "0");
            //uri = string.Format("/MaintenanceAdmission/{0}/{1}/{2}", tabNumber, patientKey, admissionKey);

            DynamicFormSipManager.Instance.SetAdmissionTabIndex(admissionKey, tabNumber);
            uri = string.Format("/MaintenanceAdmission/{0}/{1}", patientKey, admissionKey);

            return uri;
        }

        public string GetPatientMaintenanceURI(int tabNumber, int patientKey)
        {
            string uri = string.Empty;

            DynamicFormSipManager.Instance.SetPatientTabIndex(patientKey, tabNumber);
            uri = string.Format("/Maintenance/Patient/{0}", patientKey);

            return uri;
        }
    }
}