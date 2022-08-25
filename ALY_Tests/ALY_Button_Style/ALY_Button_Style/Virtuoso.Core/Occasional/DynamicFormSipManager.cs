#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ria.Sync;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using Virtuoso.Client.Core;
using Virtuoso.Client.Offline;
using Virtuoso.Client.Infrastructure.Storage;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Occasional.Model;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;
using TraceEventType = Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics.TraceEventType;

#endregion

namespace Virtuoso.Core.Occasional
{
    public class OfflineTaskKey
    {
        public int TaskKey { get; set; }
        public OfflineStoreType Type { get; set; }
    }

    public class OfflineTaskKeyComparer : EqualityComparer<OfflineTaskKey>
    {
        public override bool Equals(OfflineTaskKey x, OfflineTaskKey y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.TaskKey == y.TaskKey;
        }

        public override int GetHashCode(OfflineTaskKey key)
        {
            return key.TaskKey.GetHashCode();
        }
    }

    public class OfflineInfo
    {
        public bool FormPersisted { get; internal set; }
        public int CachedEncounterKey { get; internal set; }

        public OfflineInfo(bool persisted, int encounterkey)
        {
            FormPersisted = persisted;
            CachedEncounterKey = encounterkey;
        }
    }

    public class DynamicFormSipManager
    {
        public string Version { get; set; }
        public static string RootAutoSaveFolder { get; set; }
        public static string RootSaveFolder { get; set; }
        public static string RootCacheFolder { get; set; }
        private static volatile DynamicFormSipManager instance;
        private static object syncRoot = new Object();
        private static List<OfflineTaskKey> OfflineTaskKeys = new List<OfflineTaskKey>(); //cache of offline task keys

        private Dictionary<int, bool>
            AutoSaveTaskKeys = new Dictionary<int, bool>(); //cache of task keys that should auto-save/upload on open

        private Dictionary<int, int>
            AdmissionKeyTabIndex =
                new Dictionary<int, int>(); //cache of admission keys and their opening tab index value

        private Dictionary<int, int>
            PatientKeyTabIndex = new Dictionary<int, int>(); //cache of patient keys and their opening tab index value

        public bool GetAutoSave(int taskKey)
        {
            if (AutoSaveTaskKeys.ContainsKey(taskKey))
            {
                return AutoSaveTaskKeys[taskKey];
            }

            return false;
        }

        private void ClearAutoSave(int taskKey)
        {
            if (AutoSaveTaskKeys.ContainsKey(taskKey))
            {
                AutoSaveTaskKeys[taskKey] = false;
            }
        }

        //int? __tabIndex = DynamicFormSipManager.Instance.GetTabIndex(AdmissionKey);
        public int? GetAdmissionTabIndex(int admissionKey)
        {
            if (AdmissionKeyTabIndex.ContainsKey(admissionKey))
            {
                return AdmissionKeyTabIndex[admissionKey];
            }

            return null;
        }

        public int? GetPatientTabIndex(int patientKey)
        {
            if (PatientKeyTabIndex.ContainsKey(patientKey))
            {
                return PatientKeyTabIndex[patientKey];
            }

            return null;
        }

        public void SetAdmissionTabIndex(int admissionKey, int tabValue)
        {
            if (AdmissionKeyTabIndex.ContainsKey(admissionKey))
            {
                AdmissionKeyTabIndex[admissionKey] = tabValue;
            }
            else
            {
                AdmissionKeyTabIndex.Add(admissionKey, tabValue);
            }
        }

        public void SetPatientTabIndex(int patientKey, int tabValue)
        {
            if (PatientKeyTabIndex.ContainsKey(patientKey))
            {
                PatientKeyTabIndex[patientKey] = tabValue;
            }
            else
            {
                PatientKeyTabIndex.Add(patientKey, tabValue);
            }
        }

        static LogWriter logWriter;

        static DynamicFormSipManager()
        {
            logWriter = EnterpriseLibraryContainer.Current.GetInstance<LogWriter>();
            RootAutoSaveFolder = ApplicationStoreInfo.GetUserStoreForApplication(Constants.AUTOSAVE_FOLDER);
            RootSaveFolder = ApplicationStoreInfo.GetUserStoreForApplication(Constants.SAVE_FOLDER);
            RootCacheFolder = ApplicationStoreInfo.GetUserStoreForApplication(Constants.CACHE_FOLDER);
        }

        private DynamicFormSipManager()
        {
            try
            {
                //NOTE: only the main Virtuoso project is versioned - via a build task - and only for RELEASE.
                var asm = AssemblyFileVersionInfo.GetAssemblyByName("Virtuoso, Version=");
                var _assemblyFileVersionInfo = new AssemblyFileVersionInfo(asm);
                Version = _assemblyFileVersionInfo.Version; //"1.1.46.0"
            }
            catch (Exception)
            {
                Version = "0.0.0.0"; //should only hit this in a unit test
            }

            Log($"DynamicFormSipManager[000]: {Version}", "WS_TRACE");
        }

        public static DynamicFormSipManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new DynamicFormSipManager();
                        }
                    }
                }

                return instance;
            }
        }

        public async System.Threading.Tasks.Task<DynamicFormInfo> GetDynamicInfo(int taskKey, OfflineStoreType location,
            bool deleteFileWhenVersionDoesNotMatchAssembly)
        {
            Log(
                $"GetDynamicInfo[000]: taskKey={taskKey}, location={location}, deleteFileWhenVersionDoesNotMatchAssembly={deleteFileWhenVersionDoesNotMatchAssembly}",
                "WS_TRACE");

            var _ret = GetDynamicInfo<DynamicFormInfo>(taskKey, location, deleteFileWhenVersionDoesNotMatchAssembly);
            return await _ret;
        }

        public async System.Threading.Tasks.Task<T> GetDynamicInfo<T>(int taskOrAdmissionKey, OfflineStoreType location,
            bool deleteFileWhenVersionDoesNotMatchAssembly, string fileFormatStr = "{0}-DF")
            where T : IDynamicFormInfo, new()
        {
            Log(
                $"GetDynamicInfo[001]: taskOrAdmissionKey={taskOrAdmissionKey}, location={location}, deleteFileWhenVersionDoesNotMatchAssembly={deleteFileWhenVersionDoesNotMatchAssembly}, fileFormatStr={fileFormatStr}",
                "WS_TRACE");

            return await LoadInfo<T>(taskOrAdmissionKey, location, deleteFileWhenVersionDoesNotMatchAssembly);
        }

        public async System.Threading.Tasks.Task<bool> DashboardPersisted(int admissionKey, OfflineStoreType location)
        {
            Log($"DashboardPersisted[002]: admissionKey={admissionKey}, location={location}", "WS_TRACE");

            var __folder = GetCacheFolder(admissionKey, location, "{0}-DB");
            return await VirtuosoStorageContext.Current.HasAnyFiles(__folder);
        }

        private async System.Threading.Tasks.Task<T> LoadInfo<T>(int taskKey, OfflineStoreType location,
            bool deleteFileWhenVersionDoesNotMatchAssembly) where T : IDynamicFormInfo, new()
        {
            Log(
                $"LoadInfo[003]: taskKey={taskKey}, location={location}, deleteFileWhenVersionDoesNotMatchAssembly={deleteFileWhenVersionDoesNotMatchAssembly}",
                "WS_TRACE");

            var __cacheFolder = GetCacheFolder(taskKey, location);
            var __infoExits = await DynamicFormInfoExists<T>(__cacheFolder);
            if (__infoExits)
            {
                return await __LoadInfoWithDeleteInternal<T>(taskKey, location,
                    deleteFileWhenVersionDoesNotMatchAssembly, __cacheFolder);
            }

            return default(T);
        }

        private async System.Threading.Tasks.Task<T> __LoadInfoWithDeleteInternal<T>(int taskKey,
            OfflineStoreType location, bool deleteFileWhenVersionDoesNotMatchAssembly, string __cacheFolder)
            where T : IDynamicFormInfo, new()
        {
            Log(
                $"__LoadInfoWithDeleteInternal[004]: taskKey={taskKey}, location={location}, deleteFileWhenVersionDoesNotMatchAssembly={deleteFileWhenVersionDoesNotMatchAssembly}, __cacheFolder={__cacheFolder}",
                "WS_TRACE");

            var __loadCache = new SecureObjectCacheManager( //note: this will create folder
                __cacheFolder);

            T __dynamicFormInfoState_Ret = await __Load_T_Internal<T>(__loadCache);

            if (__dynamicFormInfoState_Ret != null && (deleteFileWhenVersionDoesNotMatchAssembly ||
                                                       __dynamicFormInfoState_Ret.IsValid == false))
            {
                if (__dynamicFormInfoState_Ret is IObjectVersion)
                {
                    var versionedInfo = __dynamicFormInfoState_Ret as IObjectVersion;
                    if (string.IsNullOrWhiteSpace(versionedInfo.Version))
                    {
                        await RemoveFromDisk(taskKey, location);
                        __dynamicFormInfoState_Ret = default(T);
                    }

                    if (__dynamicFormInfoState_Ret != null && versionedInfo.Version.Equals(Version) == false)
                    {
                        await RemoveFromDisk(taskKey, location);
                        __dynamicFormInfoState_Ret = default(T);
                    }

                    if (__dynamicFormInfoState_Ret != null && __dynamicFormInfoState_Ret.IsValid == false)
                    {
                        await RemoveFromDisk(taskKey, location);
                        __dynamicFormInfoState_Ret = default(T);
                    }
                }
                else
                {
                    //delete file - does not implement IObjectVersion
                    await RemoveFromDisk(taskKey, location);
                    __dynamicFormInfoState_Ret = default(T);
                }
            }

            return __dynamicFormInfoState_Ret;
        }

        private static async System.Threading.Tasks.Task<T> __Load_T_Internal<T>(SecureObjectCacheManager __loadCache)
            where T : IDynamicFormInfo, new()
        {
            Log("__LoadInfoWithDeleteInternal[005]:", "WS_TRACE");

            //Method to hide deserialization errors from callers
            try
            {
                T ret = await __loadCache
                    .Load<T>(); //This can throw an exception if the data does not deserialize with the current definition of T
                return ret;
            }
            catch (System.Runtime.Serialization.SerializationException e)
            {
                //System.Runtime.Serialization.SerializationException: object of type Virtuoso.Core.Occasional.Model.DynamicFormInfo did not read its entire buffer during deserialization.This is most likely an inbalance between the writes and the reads of the object.
                //at Virtuoso.Serializer.CustomBinaryFormatter.Deserialize(Stream serializationStream)
                //at RiaSync.Store.ObjectDataStore.__ImportInternal[T](FileInfo file)
                //at RiaSync.Store.ObjectDataStore.Import[T](String key)
                //at System.Windows.Ria.Sync.SecureObjectCacheManager.Load[T]()
                //at Virtuoso.Core.Occasional.DynamicFormSipManager.__Load_T_Internal[T](SecureObjectCacheManager __loadCache)}

                //return default(T);  //returns NULL
                Debug.WriteLine(e.Message);

                Log($"__LoadInfoWithDeleteInternal[005]: Exception={e.Message}", "WS_TRACE", TraceEventType.Error);

                return new T(); //create empty object.  IsValid should be FALSE
            }
        }

        private async System.Threading.Tasks.Task<bool> DynamicFormInfoExists<T>(string folder)
            where T : IDynamicFormInfo, new()
        {
            //var typeName = typeof(DynamicFormInfo).FullName; //"Virtuoso.Core.Occasional.Model.DynamicFormInfo"
            //"C:\\Users\\<domainuser>\\AppData\\Local\\Delta Health Technologies\\Crescendo\\localutest\\.Save\\125485-DF\\Virtuoso.Core.Occasional.Model.DynamicFormInfo.dat
            var path = Path.Combine(folder, string.Format("{0}.dat", typeof(T).FullName));
            var ret = await VirtuosoStorageContext.Current.Exists(path);
            Log($"DynamicFormInfoExists[006]: exists={ret}, path={path}", "WS_TRACE");
            return ret;
        }

        public static string GetCacheFolder(int taskOrAdmissionKey, OfflineStoreType location,
            string fileFormatStr = "{0}-DF") // = OfflineStoreType.SAVE)
        {
            Log(
                $"GetCacheFolder[007]: taskOrAdmissionKey={taskOrAdmissionKey}, location={location}, fileFormatStr={fileFormatStr}",
                "WS_TRACE");

            var __folder = (location == OfflineStoreType.SAVE)
                ? RootSaveFolder
                : ((location == OfflineStoreType.AUTOSAVE) ? RootAutoSaveFolder : RootCacheFolder);
            var __applicationStore = ApplicationStoreInfo.GetUserStoreForApplication(__folder);

            // fileFormatStr = "{0}-DF" for DynamicForm and "{0}-DB" for Dashboard
            var __cacheFolder = Path.Combine(__applicationStore, string.Format(fileFormatStr, taskOrAdmissionKey));

            Log($"GetCacheFolder[007]: __cacheFolder={__cacheFolder}", "WS_TRACE");

            return
                __cacheFolder; //"C:\\Users\\<domainuser>\\AppData\\Local\\Delta Health Technologies\\Crescendo\\localutest\\.Save\\125485-DF"
        }

        public async System.Threading.Tasks.Task<List<OfflineTaskKey>> GetTaskKeys(OfflineStoreType location)
        {
            Log($"GetTaskKeys[008]: location={location}", "WS_TRACE");

            var _ret = new List<OfflineTaskKey>();
            if ((location & OfflineStoreType.CACHE) != 0)
            {
                var _cachedKeys = await GetTaskKeysInternal(OfflineStoreType.CACHE);
                _cachedKeys.ForEach(k => _ret.Add(new OfflineTaskKey { TaskKey = k, Type = OfflineStoreType.CACHE }));
            }

            if ((location & OfflineStoreType.SAVE) != 0)
            {
                var _savedKeys = await GetTaskKeysInternal(OfflineStoreType.SAVE);
                _savedKeys.ForEach(k => _ret.Add(new OfflineTaskKey { TaskKey = k, Type = OfflineStoreType.SAVE }));
            }

            OfflineTaskKeys.Clear();
            OfflineTaskKeys = _ret.Distinct(new OfflineTaskKeyComparer()).ToList();
            return OfflineTaskKeys;
        }

        private async System.Threading.Tasks.Task<List<int>> GetTaskKeysInternal(OfflineStoreType location)
        {
            Log($"GetTaskKeysInternal[009]: location={location}", "WS_TRACE");

            char[] delimiterChars = { ' ', '-', '.' };
            var _ret = new List<int>();
            var _folder = (location == OfflineStoreType.SAVE) ? RootSaveFolder : RootCacheFolder;
            var directories = (await VirtuosoStorageContext.Current.EnumerateDirectories(_folder))
                .Where(dir => dir.Name.EndsWith("-DF", StringComparison.OrdinalIgnoreCase));

            foreach (var directory in directories)
            {
                //filename = "C:\\Users\\<user>\\Documents\\Crescendo\\local\\Sip\\596-DF.json.txt/dat"
                var fileParts = Path.GetFileNameWithoutExtension(directory.Name).Split(delimiterChars);
                _ret.Add(Int32.Parse(fileParts[0]));
            }

            return _ret;
        }

        public async System.Threading.Tasks.Task<OfflineInfo> FormPersisted(int taskKey, OfflineStoreType location,
            bool deleteFileWhenVersionDoesNotMatchAssembly)
        {
            Log(
                $"FormPersisted[010]: taskKey={taskKey}, location={location}, deleteFileWhenVersionDoesNotMatchAssembly={deleteFileWhenVersionDoesNotMatchAssembly}",
                "WS_TRACE");

            return await FormPersisted<DynamicFormInfo>(taskKey, location, deleteFileWhenVersionDoesNotMatchAssembly);
        }

        public async System.Threading.Tasks.Task<OfflineInfo> FormPersisted<T>(int taskKey, OfflineStoreType location,
            bool deleteFileWhenVersionDoesNotMatchAssembly) where T : IDynamicFormInfo, new()
        {
            Log(
                $"FormPersisted<T>[011]: taskKey={taskKey}, location={location}, deleteFileWhenVersionDoesNotMatchAssembly={deleteFileWhenVersionDoesNotMatchAssembly}",
                "WS_TRACE");

            var info = await LoadInfo<T>(taskKey, location, deleteFileWhenVersionDoesNotMatchAssembly);
            var ret = (info != null);
            var key = (info != null) ? info.EncounterKey : -1;
            var offinfo = new OfflineInfo(ret, key);
            return offinfo;
        }

        public async System.Threading.Tasks.Task<bool>
            HavePersistedData(OfflineStoreType location) // = OfflineStoreType.SAVE)
        {
            //DS - US5275
            var ret = await TotalPersistedData(location) > 0;
            Log($"HavePersistedData[012]: location={location}, ret={ret}", "WS_TRACE");
            return ret;
        }

        public async System.Threading.Tasks.Task<int>
            TotalPersistedData(OfflineStoreType location) // = OfflineStoreType.SAVE)
        {
            Log($"TotalPersistedData[013]: location={location}", "WS_TRACE");
            var _folder = (location == OfflineStoreType.SAVE) ? RootSaveFolder : RootCacheFolder;
            var directories = (await VirtuosoStorageContext.Current.EnumerateDirectories(_folder))
                .Where(dir => dir.Name.EndsWith("-DF", StringComparison.OrdinalIgnoreCase));

            var count = directories.Count();
            Log($"TotalPersistedData[013]: _folder={_folder}, count={count}", "WS_TRACE");
            foreach (string d in directories.Select(d => d.FullName))
                Log($"TotalPersistedData[013]: directory={d}", "WS_TRACE");
            return count;
        }

        public async System.Threading.Tasks.Task RemoveFromDisk(int taskKey, OfflineStoreType location)
        {
            Log($"RemoveFromDisk[016]: taskKey={taskKey}, location={location}", "WS_TRACE", TraceEventType.Warning);

            try
            {
                var __folder = GetCacheFolder(taskKey, location);
                if (await VirtuosoStorageContext.Current.HasAnyFiles(__folder))
                {
                    Log($"RemoveFromDisk[016]: Directory.Delete={__folder}", "WS_TRACE", TraceEventType.Warning);

                    await VirtuosoStorageContext.Current.DeleteDirectory(__folder);
                    ClearAutoSave(taskKey);
                }
            }
            catch (IOException
                   e) //You probably have the folder or a sub-folder open in Windows Explorer.  Or maybe anti-virus...
            {
                //https://msdn.microsoft.com/en-us/library/fxeahc5f.aspx
                //A file with the same name and location specified by path exists.
                //-or-
                //The directory specified by path is read-only, or recursive is false and path is not an empty directory.
                //-or-
                //The directory is the application's current working directory.
                //-or-
                //The directory contains a read-only file.
                //-or-
                //The directory is being used by another process.

                Log($"RemoveFromDisk[016]: exception={e.Message}", "WS_TRACE", TraceEventType.Error);

                System.Threading.Thread.Sleep(0);
                System.Threading.ThreadPool.QueueUserWorkItem(async _ =>
                {
                    System.Threading.Thread.Sleep(2000);
                    var __folder = GetCacheFolder(taskKey, location);
                    if (await VirtuosoStorageContext.Current.HasAnyFiles(__folder))
                    {
                        Log($"RemoveFromDisk[016]: background thread Directory.Delete={__folder}", "WS_TRACE");

                        await VirtuosoStorageContext.Current.DeleteDirectory(__folder);
                        ClearAutoSave(taskKey);
                    }
                });
            }
        }

        #region Persistence

        public async System.Threading.Tasks.Task Save(int taskKey, OfflineStoreType location, DynamicFormInfo info)
        {
            Log($"Save[017]: taskKey={taskKey}, location={location}", "WS_TRACE");
            Log($"Save[017]: info={info}", "WS_TRACE");

            await Save<DynamicFormInfo>(taskKey, location, info);
        }

        public async System.Threading.Tasks.Task Save<T>(int taskKey, OfflineStoreType location, T info)
            where T : IDynamicFormInfo
        {
            Log($"Save<T>[018]: taskKey={taskKey}, location={location}", "WS_TRACE");
            Log($"Save<T>[018]: info={info}", "WS_TRACE");

            var __saveCache = new SecureObjectCacheManager(
                GetCacheFolder(taskKey, location));
            await __saveCache.Save(info);
        }

        #endregion

        #region Navigation

        //NOTE: this version of NavigateToForm is only called from AlertManagerWorkListViewModel,
        //can use NavigateToForm(HomeScreenTaskPM task) when refactored to use refactored to use 
        //PresentationModel objects 
        public async System.Threading.Tasks.Task NavigateToForm(Task task, int encounterKey = -1)
        {
            Log($"NavigateToForm[019]: task={task}, encounterKey={encounterKey}", "WS_TRACE");

            if (EntityManager.Current.IsOnline)
            {
                var dynamicFormInfo = await GetDynamicInfo(task.TaskKey, OfflineStoreType.SAVE,
                    deleteFileWhenVersionDoesNotMatchAssembly: true); //.ToString()); //is task SIP'd
                if (dynamicFormInfo != null) //.Any() == true)
                {
                    var dynFormInfo = dynamicFormInfo; //.First();

                    var saved_task_form_key = ServiceTypeCache.GetFormKey(dynFormInfo.ServiceTypeKey);
                    var curr_task_form_key = ServiceTypeCache.GetFormKey(task.ServiceTypeKey.Value);

                    //originally SIP'd, but could have changed the service type if went back online...
                    //If FormKey of new ServiceTypeKey doesn't match the FormKey of the cached Task information - then delete the SAVE file and navigate normally
                    if (dynFormInfo.ServiceTypeKey != task.ServiceTypeKey &&
                        saved_task_form_key != curr_task_form_key && dynFormInfo.EncounterStatus != 7)
                    {
                        //Delete the save file before loading - we're online and they changed the Task's Service Type...
                        await Instance.RemoveFromDisk(task.TaskKey, OfflineStoreType.SAVE);
                        NavigateToFormInternal(GetFormUriKeys(task, encounterKey)); //, encounterKey);
                    }
                    else
                    {
                        NavigateAndVerifySavedForm(dynFormInfo);
                    }
                }
                else
                {
                    NavigateToFormInternal(GetFormUriKeys(task, encounterKey)); //, encounterKey);
                }
            }
            else //else we're offline or the form was never saved in process, so just launch it
            {
                NavigateToFormInternal(GetFormUriKeys(task, encounterKey)); //, encounterKey);
            }
        }

        public async System.Threading.Tasks.Task NavigateToForm(DischargeTransferTask task, int encounterKey = -1)
        {
            var key = (task != null) ? task.DischargeTransferTaskKey : -111;
            Log($"NavigateToForm[020]: DischargeTransferTaskKey={key}, encounterKey ={encounterKey}", "WS_TRACE");

            if (EntityManager.Current.IsOnline)
            {
                int taskKey = ((task.TaskKey == null) || (task.TaskKey < 0)) ? 0 : (int)task.TaskKey;
                var dynamicFormInfo = await GetDynamicInfo(taskKey, OfflineStoreType.SAVE,
                    deleteFileWhenVersionDoesNotMatchAssembly: true); //.ToString()); //is task SIP'd
                if (dynamicFormInfo != null) //.Any() == true)
                {
                    var dynFormInfo = dynamicFormInfo; //.First();

                    var saved_task_form_key = ServiceTypeCache.GetFormKey(dynFormInfo.ServiceTypeKey);
                    var curr_task_form_key = ServiceTypeCache.GetFormKey(task.ServiceTypeKey.Value);

                    //originally SIP'd, but could have changed the service type if went back online...
                    //If FormKey of new ServiceTypeKey doesn't match the FormKey of the cached Task information - then delete the SAVE file and navigate normally
                    if (dynFormInfo.ServiceTypeKey != task.ServiceTypeKey &&
                        saved_task_form_key != curr_task_form_key && dynFormInfo.EncounterStatus != 7)
                    {
                        //Delete the save file before loading - we're online and they changed the Task's Service Type...
                        await Instance.RemoveFromDisk(taskKey, OfflineStoreType.SAVE);
                        NavigateToFormInternal(GetFormUriKeys(task, encounterKey)); //, encounterKey);
                    }
                    else
                    {
                        NavigateAndVerifySavedForm(dynFormInfo);
                    }
                }
                else
                {
                    NavigateToFormInternal(GetFormUriKeys(task, encounterKey)); //, encounterKey);
                }
            }
            else //else we're offline or the form was never saved in process, so just launch it
            {
                NavigateToFormInternal(GetFormUriKeys(task, encounterKey)); //, encounterKey);
            }
        }

        public async System.Threading.Tasks.Task NavigateToForm(HomeScreenTaskPM task, int encounterKey = -1)
        {
            encounterKey =
                (task.EncounterKey.HasValue)
                    ? task.EncounterKey.Value
                    : encounterKey; //since HomeScreenTaskPM will usually always now have an EncounterKey for a patient related task. 

            Log($"NavigateToForm[021]: encounterKey ={encounterKey}", "WS_TRACE");

            if (EntityManager.Current.IsOnline)
            {
                var dynamicFormInfo = await GetDynamicInfo(task.TaskKey, OfflineStoreType.SAVE,
                    deleteFileWhenVersionDoesNotMatchAssembly: true); //.ToString()); //is task SIP'd
                if (dynamicFormInfo != null) //.Any() == true)
                {
                    var dynFormInfo = dynamicFormInfo; //.First();
                    var saved_servicetype = ServiceTypeCache.GetServiceTypeFromKey(dynFormInfo.ServiceTypeKey);
                    var saved_task_form_key = ServiceTypeCache.GetFormKey(dynFormInfo.ServiceTypeKey);
                    var curr_task_form_key = ServiceTypeCache.GetFormKey(task.ServiceTypeKey.Value);

                    //originally SIP'd, but could have changed the service type if went back online...
                    //If FormKey of new ServiceTypeKey doesn't match the FormKey of the cached Task information - then delete the SAVE file and navigate normally
                    if ((saved_servicetype != null) && saved_servicetype.IsAttempted)
                    {
                        if (dynFormInfo.EncounterStatus != (int)EncounterStatusType.Completed)
                        {
                            AutoSaveTaskKeys[task.TaskKey] = true;
                            NavigateToFormInternal(GetFormUriKeys(task, encounterKey)); //, encounterKey);
                        }
                        else
                        {
                            if (task.TaskStatus == (int)EncounterStatusType.Completed)
                            {
                                //Delete the save file before loading - we're online and they changed the Task's Service Type...
                                AutoSaveTaskKeys[task.TaskKey] = false;
                                await Instance.RemoveFromDisk(task.TaskKey, OfflineStoreType.SAVE);
                                NavigateToFormInternal(GetFormUriKeys(task, encounterKey)); //, encounterKey);
                            }
                            else
                            {
                                AutoSaveTaskKeys[task.TaskKey] = true;
                                NavigateAndVerifySavedForm(dynFormInfo);
                            }
                        }
                    }
                    else
                    {
                        if ((dynFormInfo.ServiceTypeKey != task.ServiceTypeKey) &&
                            (saved_task_form_key != curr_task_form_key) && (dynFormInfo.EncounterStatus != 7))
                        {
                            //Delete the save file before loading - we're online and they changed the Task's Service Type...
                            await Instance.RemoveFromDisk(task.TaskKey, OfflineStoreType.SAVE);
                            NavigateToFormInternal(GetFormUriKeys(task, encounterKey)); //, encounterKey);
                        }
                        else
                        {
                            NavigateAndVerifySavedForm(dynFormInfo);
                        }
                    }
                }
                else
                {
                    AutoSaveTaskKeys[task.TaskKey] = false;
                    NavigateToFormInternal(GetFormUriKeys(task, encounterKey)); //, encounterKey);
                }
            }
            else //else we're offline or the form was never saved in process, so just launch it
            {
                NavigateToFormInternal(GetFormUriKeys(task, encounterKey)); //, encounterKey);
            }
        }

        public void NavigateToReadOnlyForm(Dictionary<string, int?> param)
        {
            Log(
                $"NavigateToReadOnlyForm[022]: patient={param["patient"].GetValueOrDefault()}, admission={param["admission"].GetValueOrDefault()}, form={param["form"].GetValueOrDefault()}, service={param["service"].GetValueOrDefault()}, task={param["task"].GetValueOrDefault()}",
                "WS_TRACE");

            string uri = NavigationUriBuilder.Instance.GetDynamicFormReadyOnlyURI(
                param["patient"].GetValueOrDefault(),
                param["admission"].GetValueOrDefault(),
                param["form"].GetValueOrDefault(),
                param["service"].GetValueOrDefault(),
                param["task"].GetValueOrDefault(),
                //param["encounter"].GetValueOrDefault(),
                1); // if this bit is set then the user will not be able to save changes in dynamicform

            //LogWriter.Write(
            //            string.Format("Uri: {0}", uri),
            //            new[] { this.GetType().ToString() },  //category
            //            0,  //priority
            //            0,  //eventid
            //            TraceEventType.Information);

            Messenger.Default.Send(new Uri(uri, UriKind.Relative), "NavigationRequest");
        }

        public void NavigateToAttemptedForm(Dictionary<string, int?> param)
        {
            Log(
                $"NavigateToAttemptedForm[023]: patient={param["patient"].GetValueOrDefault()}, admission={param["admission"].GetValueOrDefault()}, form={param["form"].GetValueOrDefault()}, service={param["service"].GetValueOrDefault()}, task={param["task"].GetValueOrDefault()}",
                "WS_TRACE");

            string uri = NavigationUriBuilder.Instance.GetDynamicFormAttemptedURI(
                param["patient"].GetValueOrDefault(),
                param["admission"].GetValueOrDefault(),
                param["form"].GetValueOrDefault(),
                param["service"].GetValueOrDefault(),
                param["task"].GetValueOrDefault(),
                //param["encounter"].GetValueOrDefault(),
                1); // if this bit is set then the user will not be able to save changes in dynamicform

            //LogWriter.Write(
            //            string.Format("Uri: {0}", uri),
            //            new[] { this.GetType().ToString() },  //category
            //            0,  //priority
            //            0,  //eventid
            //            TraceEventType.Information);

            Messenger.Default.Send(new Uri(uri, UriKind.Relative), "NavigationRequest");
        }

        private void NavigateToFormInternal(Dictionary<string, int?> param)
        {
            Log("NavigateToFormInternal[024]", "WS_TRACE");

            ////SIP'd encounters have negative keys - start with -1, will change to some other negative value once the encounter is created on the client.
            ////In order for navigation to function - keep all negative keys equal to -1, else the caching navigation code will create a new instance of
            ////your form - one for each negative key.
            //// If encounterKey == -2 that means that the client will automatically upload the encounter
            //if (encounterKey < 0 && encounterKey != -2)
            //    encounterKey = -1;  //reset client generated encounters to -1, so that the navigation system doesn't create an instance for each key

            string uri = GetFormUri(param);
            //LogWriter.Write(
            //            string.Format("Uri: {0}", uri),
            //            new[] { this.GetType().ToString() },  //category
            //            0,  //priority
            //            0,  //eventid
            //            TraceEventType.Information);
            Messenger.Default.Send(new Uri(uri, UriKind.Relative), "NavigationRequest");
        }

        private void NavigateAndVerifySavedForm(DynamicFormInfo dynamicFormInfo)
        {
            Log($"NavigateAndVerifySavedForm[025]: dynamicFormInfo={dynamicFormInfo}", "WS_TRACE");

            AutoSaveTaskKeys[dynamicFormInfo.TaskKey] = false;

            ServiceLayerUtil.IsEncounterSyncedAsync(
                    dynamicFormInfo.TaskKey,
                    dynamicFormInfo.EncounterKey,
                    dynamicFormInfo.EncounterStatus) //combined server ping/encounter synced status fetch
                .ContinueWith(isEncounterSynced =>
                    {
                        if (isEncounterSynced.IsFaulted == false)
                        {
                            if (isEncounterSynced.Result)
                            {
                                // If the client and server encounters are synced then show prompt
                                UploadEncounterPopup d = new UploadEncounterPopup();

                                if (d != null)
                                {
                                    d.Closed += (s, err) =>
                                    {
                                        bool? dialogResult = ((ChildWindow)s).DialogResult;
                                        if (dialogResult == false)
                                        {
                                            return;
                                        }

                                        UploadEncounterPopup ued = s as UploadEncounterPopup;
                                        if (ued == null)
                                        {
                                            return;
                                        }

                                        if (ued.Discard) //user chose to not load client data
                                        {
                                            Log("NavigateAndVerifySavedForm[025]: Creating DiscardDataEvent",
                                                "WS_TRACE", TraceEventType.Warning);

                                            //Create Discard audit
                                            ServiceLayerUtil.DiscardDataEventAsync(dynamicFormInfo.EncounterKey)
                                                .ContinueWith(async auditTask =>
                                                    {
                                                        if (!auditTask.IsFaulted)
                                                        {
                                                            await Instance.RemoveFromDisk(dynamicFormInfo.TaskKey,
                                                                OfflineStoreType.SAVE);
                                                            NavigateToFormInternal(
                                                                GetFormUriKeys(
                                                                    dynamicFormInfo)); //, dynamicFormInfo.EncounterKey);
                                                        }
                                                        else
                                                        {
                                                            if (Deployment.Current.Dispatcher.CheckAccess())
                                                            {
                                                                await Instance.RemoveFromDisk(dynamicFormInfo.TaskKey,
                                                                    OfflineStoreType.SAVE);
                                                                NavigateToFormInternal(
                                                                    GetFormUriKeys(
                                                                        dynamicFormInfo)); //, dynamicFormInfo.EncounterKey);
                                                            }
                                                            else
                                                            {
                                                                Deployment.Current.Dispatcher.BeginInvoke(async () =>
                                                                {
                                                                    await Instance.RemoveFromDisk(
                                                                        dynamicFormInfo.TaskKey, OfflineStoreType.SAVE);
                                                                    NavigateToFormInternal(
                                                                        GetFormUriKeys(
                                                                            dynamicFormInfo)); //, dynamicFormInfo.EncounterKey);
                                                                });
                                                            }
                                                        }
                                                    },
                                                    System.Threading.CancellationToken.None,
                                                    System.Threading.Tasks.TaskContinuationOptions.None,
                                                    Client.Utils.AsyncUtility.TaskScheduler);
                                        }
                                        else //(ued.Discard == false)  //user chose to upload client data...so why doing a regular navigate?
                                        {
                                            AutoSaveTaskKeys[dynamicFormInfo.TaskKey] = true;
                                            NavigateToFormInternal(GetFormUriKeys(dynamicFormInfo)); //, -2);
                                        }
                                    };
                                    d.Show();
                                }
                            }
                            else // else, client and server are not synced, upload client to server
                            {
                                //Dynamic form will load data from local cache and automatically upload the encounter
                                AutoSaveTaskKeys[dynamicFormInfo.TaskKey] = true;
                                NavigateToFormInternal(GetFormUriKeys(dynamicFormInfo)); //, -2);
                            }
                        }
                        else
                        {
                            if (dynamicFormInfo.EncounterKey == -1)
                            {
                                NavigateToFormInternal(
                                    GetFormUriKeys(
                                        dynamicFormInfo)); //, dynamicFormInfo.EncounterKey);  //Dynamic form will load data from local cache
                            }
                        }
                    },
                    System.Threading.CancellationToken.None,
                    System.Threading.Tasks.TaskContinuationOptions.None,
                    Client.Utils.AsyncUtility.TaskScheduler);
        }

        private string GetFormUri(Dictionary<string, int?> param, bool readOnly = false)
        {
            Log("GetFormUri[026]", "WS_TRACE");
            string uri = "";
            if (!readOnly)
            {
                uri = NavigationUriBuilder.Instance.GetDynamicFormURI(
                    param["patient"].GetValueOrDefault(),
                    param["admission"].GetValueOrDefault(),
                    param["form"].GetValueOrDefault(),
                    param["service"].GetValueOrDefault(),
                    param["task"].GetValueOrDefault());
                //param["encounter"].GetValueOrDefault());
            }
            else
            {
                uri = NavigationUriBuilder.Instance.GetDynamicFormReadyOnlyURI(
                    param["patient"].GetValueOrDefault(),
                    param["admission"].GetValueOrDefault(),
                    param["form"].GetValueOrDefault(),
                    param["service"].GetValueOrDefault(),
                    param["task"].GetValueOrDefault(),
                    //param["encounter"].GetValueOrDefault(),
                    1); // if this bit is set then the user will not be able to save changes in dynamicform
            }

            Log($"GetFormUri[026]: uri={uri}", "WS_TRACE");
            return uri;
        }

        public Dictionary<string, int?> GetFormUriKeys(Task task, int encounterKey)
        {
            var taskKey = (task != null) ? task.TaskKey : -111;
            Log($"GetFormUriKeys[027]: TaskKey={taskKey}, encounterKey={encounterKey}", "WS_TRACE");

            var ret = new Dictionary<string, int?>();
            ret.Add("patient", task.PatientKey);
            ret.Add("admission", task.AdmissionKey);
            ret.Add("form", ServiceTypeCache.GetFormKey(task.ServiceTypeKey.Value));
            ret.Add("service", task.ServiceTypeKey);
            ret.Add("task", task.TaskKey);
            ret.Add("encounter", encounterKey);
            //in case encounter saved under different form definition than current
            if (task.Encounter.Any())
            {
                var _savedFormKey = ServiceTypeCache.GetFormKey(task.ServiceTypeKey.Value);
                var _formKey = _savedFormKey;
                var _encounterKey = -1;
                var _Encounter = task.Encounter.FirstOrDefault();
                if (_Encounter != null &&
                    _Encounter.EncounterStatus !=
                    (int)EncounterStatusType.None) //US 28409:Create Encounter when Create Task
                {
                    _formKey = _Encounter.FormKey;
                }

                if (_Encounter != null)
                {
                    _encounterKey = _Encounter.EncounterKey;
                }

                ret["form"] = _formKey; // ret.Add("form", _formKey);
                ret["encounter"] = _encounterKey; // ret.Add("encounter", _encounterKey);
                //if (_formKey != _savedFormKey)
                //{
                //    LogWriter.Write(
                //            string.Format("Encounter {0} saved under different form definition than current. \nOld: {1}\nNew: {2}", _encounterKey, _savedFormKey, _formKey),
                //            new[] { this.GetType().ToString() },  //category
                //            0,  //priority
                //            0,  //eventid
                //            TraceEventType.Information);
                //}
            }

            return ret;
        }

        public Dictionary<string, int?> GetFormUriKeys(DischargeTransferTask task, int encounterKey)
        {
            var dischargeTransferTaskKey = (task != null) ? task.DischargeTransferTaskKey : -111;
            Log(
                $"GetFormUriKeys[028]: DischargeTransferTaskKey={dischargeTransferTaskKey}, encounterKey={encounterKey}",
                "WS_TRACE");

            var ret = new Dictionary<string, int?>();
            ret.Add("patient", task.PatientKey);
            ret.Add("admission", task.AdmissionKey);
            ret.Add("form", ServiceTypeCache.GetFormKey(task.ServiceTypeKey.Value));
            ret.Add("service", task.ServiceTypeKey);
            ret.Add("task", task.TaskKey);
            ret.Add("encounter", encounterKey);
            return ret;
        }

        public Dictionary<string, int?> GetFormUriKeys(HomeScreenTaskPM task, int encounterKey)
        {
            var homeScreenTaskPMTaskKey = (task != null) ? task.TaskKey : -111;
            Log($"GetFormUriKeys[029]: HomeScreenTaskPM.TaskKey={homeScreenTaskPMTaskKey}, encounterKey={encounterKey}",
                "WS_TRACE");

            var ret = new Dictionary<string, int?>();
            ret.Add("patient", task.PatientKey);
            ret.Add("admission", task.AdmissionKey);
            ret.Add("form", ServiceTypeCache.GetFormKey(task.ServiceTypeKey.Value));
            ret.Add("service", task.ServiceTypeKey);
            ret.Add("task", task.TaskKey);
            ret.Add("encounter", encounterKey);
            //in case encounter saved under different form definition than current
            if (task.EncounterKey.HasValue)
            {
                var _savedFormKey = ServiceTypeCache.GetFormKey(task.ServiceTypeKey.Value);
                var _formKey = _savedFormKey;
                var _encounterKey = -1;
                bool hasEncounter = task.EncounterKey.HasValue;
                if (task.EncounterKey.HasValue &&
                    task.TaskStatus != (int)EncounterStatusType.None) //US 28409:Create Encounter when Create Task
                {
                    _formKey = task.FormKey;
                }

                if (hasEncounter)
                {
                    _encounterKey = task.EncounterKey.Value;
                }

                ret["form"] = _formKey; // ret.Add("form", _formKey);
                ret["encounter"] = _encounterKey; // ret.Add("encounter", _encounterKey);
                //if (_formKey != _savedFormKey)
                //{
                //    LogWriter.Write(
                //            string.Format("Encounter {0} saved under different form definition than current. \nOld: {1}\nNew: {2}", _encounterKey, _savedFormKey, _formKey),
                //            new[] { this.GetType().ToString() },  //category
                //            0,  //priority
                //            0,  //eventid
                //            TraceEventType.Information);
                //}
            }

            return ret;
        }

        public Dictionary<string, int?> GetFormUriKeys(DynamicFormInfo dfi)
        {
            Log($"GetFormUriKeys[030]: dfi={dfi}", "WS_TRACE");

            int encounterKey = dfi.EncounterKey;

            var ret = new Dictionary<string, int?>();
            ret.Add("patient", dfi.PatientKey);
            ret.Add("admission", dfi.AdmissionKey);
            ret.Add("form", ServiceTypeCache.GetFormKey(dfi.ServiceTypeKey));
            ret.Add("service", dfi.ServiceTypeKey);
            ret.Add("task", dfi.TaskKey);
            ret.Add("encounter", encounterKey);
            //in case encounter saved under different form definition than current
            if (dfi.EncounterKey > 0)
            {
                var _savedFormKey = ServiceTypeCache.GetFormKey(dfi.ServiceTypeKey);
                var _formKey = _savedFormKey;
                var _encounterKey = -1;
                //var _Encounter = dfi.EncounterKey.HasValue;
                //if (dfi.EncounterKey.HasValue && dfi.EncounterStatus != (int)Virtuoso.Server.Data.EncounterStatusType.None) //US 28409:Create Encounter when Create Task
                if (dfi.EncounterStatus != (int)EncounterStatusType.None) //US 28409:Create Encounter when Create Task
                {
                    _formKey = dfi.FormKey;
                }

                _encounterKey = dfi.EncounterKey;

                ret["form"] = _formKey; // ret.Add("form", _formKey);
                ret["encounter"] = _encounterKey; // ret.Add("encounter", _encounterKey);
                //if (_formKey != _savedFormKey)
                //{
                //    LogWriter.Write(
                //            string.Format("Encounter {0} saved under different form definition than current. \nOld: {1}\nNew: {2}", _encounterKey, _savedFormKey, _formKey),
                //            new[] { this.GetType().ToString() },  //category
                //            0,  //priority
                //            0,  //eventid
                //            TraceEventType.Information);
                //}
            }

            return ret;
        }

        private NavigateCloseDialog CreateDialogue(String Title, String Msg, String okLabel, String cancelLabel)
        {
            // Currently not being used - caurni00 - 6/5/2015
            NavigateCloseDialog d = new NavigateCloseDialog();
            //d.LayoutRoot.Margin = new Thickness(5);
            d.Width = 900;
            d.Height = 225;
            d.ErrorMessage = Msg;
            d.ErrorQuestion = null;
            d.Title = Title;
            d.OKLabel = okLabel;
            //d.OKWidth = 275;
            //d.CancelWidth = 275;
            //d.CancelLabel = cancelLabel;
            d.HasCloseButton = true;
            return d;
        }

        #endregion Navigation

        private static void Log(string message, string subCategory,
            TraceEventType traceEventType = TraceEventType.Information)
        {
            string category = "DynamicFormSipManager";

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