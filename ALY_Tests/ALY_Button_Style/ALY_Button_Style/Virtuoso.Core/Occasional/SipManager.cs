#region Usings

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using Virtuoso.Client.Core;
using Virtuoso.Client.Infrastructure.Storage;

#endregion

namespace Virtuoso.Core.Occasional
{
    public abstract class SipManager
    {
        static LogWriter logWriter;
        public static string RootSaveFolder { get; set; }
        public static string RootCacheFolder { get; set; }
        public static string FileExtension { get; set; }
        protected abstract string FileFormatStr { get; } //readonly string FileFormatStr = "{0}-DF.{1}";

        static SipManager()
        {
            logWriter = EnterpriseLibraryContainer.Current.GetInstance<LogWriter>();
#if DEBUG
            FileExtension = Constants.JSON_Extension;
#else
            FileExtension = Constants.JSON_Extension_Encrypted;
#endif
            RootSaveFolder = ApplicationStoreInfo.GetUserStoreForApplication(Constants.SAVE_FOLDER);
            RootCacheFolder = ApplicationStoreInfo.GetUserStoreForApplication(Constants.CACHE_FOLDER);
        }

        public async Task<bool> FormPersisted<T>(T key, OfflineStoreType location)
        {
            var formExists = await VirtuosoStorageContext.Current.Exists(GetFileName(key, location));
            Log($"FormPersisted<T>[001]: key={key}, location={location}, formExists={formExists}", "WS_TRACE");
            return formExists;
        }

        public string GetFileName<T>(T key, OfflineStoreType location)
        {
            var _folder = (location == OfflineStoreType.SAVE) ? RootSaveFolder : RootCacheFolder;
            var ret = Path.Combine(_folder,
                string.Format(FileFormatStr, key, FileExtension)); //DynamicFormSipManager.FileExtension));

            Log($"GetFileName<T>[002]: key={key}, location={location}, ret={ret}", "WS_TRACE");

            return ret;
        }

        ////////////////////////////////

        public async Task<bool> MaintenanceFormPersisted(Type key, string id)
        {
            var formExists = await VirtuosoStorageContext.Current.Exists(MaintenanceGetFileName(key, id));
            return formExists;
        }

        public string MaintenanceGetFileName(Type key, string id)
        {
            string rootPath = ApplicationStoreInfo.GetUserStoreForApplication(Constants.PRIVATE_APPDATA_FOLDER);
            string filePath = "";

            switch (key.Name)
            {
                case "AllergyCodeViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_ALLERGY);
                    break;
                case "BereavementPlanViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_BEREAVEMENTPLAN);
                    break;
                case "ComfortPackViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_COMFORTPACK);
                    break;
                case "CodeLookupViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_CODELOOKUP);
                    break;
                case "DisciplineViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_DISCIPLINE);
                    break;
                case "EquipmentViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_EQUIPMENT);
                    break;
                case "FacilityViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_FACILITY);
                    break;
                case "FunctionalDeficitViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_FUNCTIONALDEFICIT);
                    break;
                case "GoalElementViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_GOALELEMENT);
                    break;
                case "GoalViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_GOAL);
                    break;
                case "ICDViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_ICD);
                    break;
                case "InsuranceViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_INSURANCE);
                    break;
                case "NonServiceTypeViewModel":
                    filePath = Path.Combine(rootPath, "NonServiceType");
                    break;
                case "OasisHeaderViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_OASISHEADER);
                    break;
                case "PatientViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_PATIENT);
                    break;
                case "PhysicianViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_PHYSICIAN);
                    break;
                case "ReferralSourceViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_REFERRALSOURCE);
                    break;
                case "ServiceLineViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_SERVICELINE);
                    break;
                case "SupplyViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_SUPPLY);
                    break;
                case "UserProfileViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_USERPROFILE);
                    break;
                case "VendorViewModel":
                    filePath = Path.Combine(rootPath, Constants.APPLICATION_VENDOR);
                    break;
            }

            return filePath + id + ".txt";
        }

        public async Task MaintenanceRemoveFromDisk(Type key, string id)
        {
            string fileName = MaintenanceGetFileName(key, id);

            Log($"MaintenanceRemoveFromDisk[006]: key={key}, id={id}, fileName={fileName}", "WS_TRACE");
            await VirtuosoStorageContext.Current.DeleteFile(fileName);
        }

        private static void Log(string message, string subCategory,
            TraceEventType traceEventType = TraceEventType.Information)
        {
            string category = "SipManager";

            var __category = string.IsNullOrEmpty(subCategory)
                ? category
                : string.Format("{0}-{1}", category, subCategory);
            logWriter.Write(message,
                new[] { __category }, //category
                0, //priority
                0, //eventid
                traceEventType);
        }
    }
}