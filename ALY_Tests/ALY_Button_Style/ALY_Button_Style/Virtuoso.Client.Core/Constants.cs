#region Usings

using System;

#endregion

namespace Virtuoso.Core
{
    public class AuthorizationDistributionCreated
    {
        public string EventName { get; set; }
    }

    public enum HomePageAgencyOpsRefreshOptionEnum
    {
        None = 0,
        Patient = 1,
        All = 2,
        SingleTask = 3
    }

    public class ServiceTypeKeyChangedEvent
    {
        public int TaskKey { get; set; }
        public int EncounterKey { get; set; }
        public int NewServiceTypeKey { get; set; }

        public ServiceTypeKeyChangedEvent(int taskKey, int encounterKey, int newServiceTypeKey)
        {
            TaskKey = taskKey;
            EncounterKey = encounterKey;
            NewServiceTypeKey = newServiceTypeKey;
        }
    }

    public class DocumentationChangedEvent
    {
        public int? PatientKey { get; private set; }
        public int? AdmissionKey { get; private set; }
        public int? EncounterKey { get; private set; }
        public int? TaskKey { get; private set; }
        public int? EncounterStatus { get; private set; }
        public HomePageAgencyOpsRefreshOptionEnum HomePageAgencyOpsRefreshOption { get; private set; }

        private DocumentationChangedEvent(int? patientKey, int? admissionKey, int? encounterKey, int? taskKey,
            int? encounterStatus, HomePageAgencyOpsRefreshOptionEnum homePageAgencyOpsRefreshOption)
        {
            PatientKey = patientKey;
            AdmissionKey = admissionKey;
            EncounterKey = encounterKey;
            TaskKey = taskKey;
            EncounterStatus = encounterStatus;
            HomePageAgencyOpsRefreshOption = homePageAgencyOpsRefreshOption;
        }

        public static DocumentationChangedEvent CreateForPatient(int? patientKey)
        {
            HomePageAgencyOpsRefreshOptionEnum opt = (patientKey == -1)
                ? HomePageAgencyOpsRefreshOptionEnum.All
                : HomePageAgencyOpsRefreshOptionEnum.None;
            return new DocumentationChangedEvent(patientKey, admissionKey: null, encounterKey: null, taskKey: null,
                encounterStatus: null, homePageAgencyOpsRefreshOption: opt);
        }

        public static DocumentationChangedEvent Create(int? patientKey, int? admissionKey = null,
            int? encounterKey = null, int? taskKey = null, int? encounterStatus = null,
            HomePageAgencyOpsRefreshOptionEnum homePageAgencyOpsRefreshOption = HomePageAgencyOpsRefreshOptionEnum.None)
        {
            return new DocumentationChangedEvent(patientKey, admissionKey, encounterKey, taskKey, encounterStatus,
                homePageAgencyOpsRefreshOption);
        }
    }

    public static class Constants
    {
        public static class AdmissionDocumentation
        {
            public const int MaxRefreshDocumentListRetryCount = 0;
            public const int TimeoutLengthMilliseconds = 60000; //60,000 milliseonds = 1 minute to timeout client http request
        }

        public static class DynamicForm
        {
            public const int NonValidFormSectionQuestionKey = -1111;
        }

        public static class Cache
        {
            public const String USE_ADDRESS_MAP_DATABASE_BASE_FILE_NAME = "UseAddressMapDatabase";
            public const String ROLE_TELEMONITORING = "TeleMonitoring";
            public const String ROLE_BEREAVEMENT = "Bereavement";
        }

        public static class DomainEvents
        {
            public const String AdmissionGroupModified = "AdmissionGroupModified";
            public const String RefreshAdmissionNotTakenDateTime = "RefreshAdmissionNotTakenDateTime";
            public const String RefreshAdmissionNotTakenReason = "RefreshAdmissionNotTakenReason";
            public const String PatientViewModelSaved = "PatientViewModelSaved";
            public const String ServiceTypeKeyChanged = "ServiceTypeKeyChanged";
        }

        public static class Logging
        {
            public const String TRACE_ON_BASE_FILE_NAME = "TraceOn";
            public const String LOG_FILE_NAME = "Trace.txt";
            public const String LOG_COMMAND = "LOG_COMMAND";
            public const String CLEAR_FILE = "CLEAR_FILE";
        }

        public static class Messaging
        {
            public const String NetworkAvailability = "NetworkAvailability";
            public const String CloseSearchDialog = "CloseSearchDialog";
            public const String RefreshPage = "RefreshPage";
            public const String DocumentationChanged = "DocumentationChanged";
            public const String DocumentationChanged2 = "DocumentationChanged2";
            public const String UserCacheChanged = "UserCacheChanged";
            public const String UnhandledException = "UnhandledException";
            public const String NavigatingFromDueToFormOpen = "NavigatingFromDueToFormOpen";
            public const String RefreshMaintenancePatientAdmissions = "RefreshMaintenancePatientAdmissions";
            public const String AdmissionDocumentationChanged = "AdmissionDocumentationChanged";
            public const String NewAdmissionDocumentationChanged = "NewAdmissionDocumentationChanged";
            public const String DocumentationAdded = "DocumentationAdded";
        }

        public static class Application
        {
            public const String Resize = "Resize";
            public const String Authenticated = "Authenticated";
            public const String AuthorizationDistributionCreated = "AuthorizationDistributionCreated";
            public const String SearchDialogNavigationRequest = "SearchDialogNavigationRequest";
        }

        public static class AppSettings
        {
            public const String AllowExecuteOnServer = "AllowExecuteOnServer";
            public const String HavenBridgeServiceUrl = "HavenBridgeServiceUrl";
            public const String CMSGrouperServiceUrl = "CMSGrouperServiceUrl";
            public const String MedispanServiceUrl = "MedispanServiceUrl";
            public const String PatientChartPrintLimit = "PatientChartPrintLimit";

            public const String MelissaAddressVerification = "MelissaAddressVerification";

            public const String PasswordResetEmailAddress = "PasswordResetEmailAddress";

            //DS 14005 10/24/14
            public const String SSMUseSubdomain = "ssmUseSubdomain";
            public const String ReportArchiveRepository = "ReportArchiveRepository";
            public const String METRICS_KEY = "METRICS_KEY";

            public const String DOCUMENT_TAB_TIMEOUT_SECONDS = "DOCUMENT_TAB_TIMEOUT_SECONDS";

            public const String HOST_OVERRIDE = "HOST_OVERRIDE";
        }

        public static class Offline
        {
            public const String RefreshTasks = "RefreshTasks";
        }

        public const int DOCUMENT_TAB_DEFAULT_TIMEOUT_SECONDS = 3;

        public const String PRIVATE_APPDATA_FOLDER = ".AppData";

        public const String IDGEN_SAVE_FILENAME = "id.txt";

        public const String COOKIE_SAVE_FILENAME = "cookie.txt";
        public const String COOKIE_SAVE_ENCRYPTED_FILENAME = "cookie.dat";

        public const String AUTH_USER_SAVE_FILENAME = "users.txt";
        public const String AUTH_USER_SAVE_ENCRYPTED_FILENAME = "users.dat";

        //public const String OFFLINE_DEBUG_FILE = "OFFLINE_DEBUG.txt";
        public const String AUTOSAVE_FOLDER = ".AutoSave";
        public const String SAVE_FOLDER = ".Save";
        public const String CACHE_FOLDER = ".Cache";
        public const String DATA_STORE_FOLDER = ".Data";

        public const String METRICS_FOLDER = ".Log";

        public const String TEMP_FOLDER = ".Temp";
        public const String TRACE_FOLDER = ".Trace";
        public const string JSON_Extension = "json.txt";
        public const string JSON_Extension_Encrypted = "json.dat";

        public const String ACTION = "action";
        public const String ADDNEW = "addNew";

        //NOTE: these application 'suite' tags will be displayed in the open work view to differentiate record types
        public const String APPLICATION_TENANT = "Agency";
        public const String APPLICATION_ALLERGY = "Allergy";
        public const String APPLICATION_BEREAVEMENT = "Bereavement";
        public const String APPLICATION_BEREAVEMENTPLAN = "Bereavement Plan";
        public const String APPLICATION_COMFORTPACK = "Comfort Pack";
        public const String APPLICATION_CODELOOKUP = "Code Lookup";
        public const String APPLICATION_DELTA = "AlayaCare";
        public const String APPLICATION_DISCIPLINE = "Discipline";
        public const String APPLICATION_EMPLOYER = "Employer";
        public const String APPLICATION_EQUIPMENT = "Equipment";
        public const String APPLICATION_FACILITY = "Facility";
        public const String APPLICATION_VENDOR = "Vendor";
        public const String APPLICATION_OASISHEADER = "OasisHeader";
        public const String APPLICATION_FUNCTIONALDEFICIT = "Functional Deficit";
        public const String APPLICATION_HIGHRISKMEDICATION = "High Risk Medication";
        public const String APPLICATION_GOAL = "Goal";
        public const String APPLICATION_GOALELEMENT = "Goal Element";
        public const String APPLICATION_CENSUSTRACT = "Census Tract";
        public const String APPLICATION_GUARDAREA = "GUard Area";
        public const String APPLICATION_ICD = "ICD";
        public const String APPLICATION_INSURANCE = "Insurance";
        public const String APPLICATION_PATIENT = "Patient";
        public const String APPLICATION_ADMISSION = "Admission";
        public const String APPLICATION_PHYSICIAN = "Physician";
        public const String APPLICATION_REFERRALSOURCE = "Referral Contact";
        public const String APPLICATION_SERVICELINE = "Service Line";
        public const String APPLICATION_SUPPLY = "Supply";
        public const String APPLICATION_USERPROFILE = "User";
        public const String APPLICATION_INSURANCEGROUP = "Insurance Group";
        public const String APPLICATION_INSURANCEWORKLIST = "Insurance Verification Worklist";
        public const String APPLICATION_HOSPICEPUMPMANUAL = "Hospice IV Pump Manual Entry";
        public const String APPLICATION_PHARMACYREFILLIMPORT = "Hospice Perscription Fill - Import";
        public const String APPLICATION_PHARMACYREFILLMANUAL = "Hospice Perscription Fill - Manual";
        public const String APPLICATION_TEAMMEETINGWORKLIST = "Hospice Team Meeting Work List";
        public const String APPLICATION_VERIFICATIONREQ = "Verification Request";
        public const String APPLICATION_ORDERSTRACKING = "Orders Tracking";
        public const String APPLICATION_RULEDEFINITION = "Rule Definition";
        public const String APPLICATION_DISCHARGETRANSFERSUMMARY = "Discharge Transfer Summary Worklist";
        public const String APPLICATION_PDGMWORKLIST = "PDGM Worklist";

        public const String ID = "id";

        public const String ADDING = "Adding";
        public const String EDITING = "Editing";
        public const String VIEWING = "Viewing";

        public const String HOME_URI_STRING = "/Home";

        public const String PATIENT_HOME_URI_STRING = "/Patient";

        public const String RECORD_NOT_FOUND = "Record not found";
        public const String UNKNOWN_ACTION = "Unknown action";
        public const String TRACKING_KEY = "trackingKey";
        public const String TRACKING_KEY_QUERYSTRING = TRACKING_KEY + "=";
        public const String ADD_NEW_QUERYSTRING = ACTION + "=" + ADDNEW;

        public const String ENTITY_TYPENAME_FORMAT =
            "{0}, Virtuoso.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        public const String
            REFERENCE_DATA_STORE_FOLDER =
                "ReferenceData"; //NOTE: this value is used in class ApplicationStoreInfo for creating directories on startup.  If you change this - update that class.
    }
}