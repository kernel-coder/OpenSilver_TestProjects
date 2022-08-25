#region Usings

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using Virtuoso.Client.Core;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Portable.Database;

#endregion

namespace Virtuoso.Core.Database
{
    [ExportMetadata("DatabaseName", VirtuosoDatabase.ICDCM9)]
    [Export(typeof(IDatabaseWrapper))]
    public class ICDCM9Database : IDatabaseWrapper
    {
        //NOTE: If you change the properties on the objects that you are caching - increase the version 
        //      of the .dbs file - the old file will be deleted and the new file re-cached.
        //      This database currently caches the following objects - 
        //              CacheConfiguration and CachedICDCM9Code and CachedAllergyCode.
        public static readonly string
            StorageName =
                "ICDCM9DBS009"; //NOTE: check code in RemovePriorICDCM9Versions() if you change the value of StorageName

        private object SyncLock = new object();
        public ISimpleStorage Storage { get; internal set; }

        ILogger Logger { get; set; }

        [ImportingConstructor]
        public ICDCM9Database(ILogger _logger)
        {
            Logger = _logger;
        }

        public string Name => "ICDCM9 Database";

        public async Task Start()
        {
            Logger.Log(TraceEventType.Verbose, Name, "BEGIN Start()");

            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            await RemovePriorDiskVersions();

            Logger.Log(TraceEventType.Verbose, Name, "END Start()");
        }

        public async Task RemovePriorDiskVersions(bool delete_current_version_only = false)
        {
            await DatabaseExtensions.RemovePriorDiskVersions("ICDCM9DBS", StorageName, delete_current_version_only);
        }

        public void Initialize()
        {
            lock (SyncLock)
            {
                try
                {
                    var mapSchema =
                        TableSchemaRepository.GetSchema(ReferenceTableName.Create(ReferenceTableName.ICDCM9));
                    Storage = new FixedLengthFlatFileStorage(
                        ApplicationStoreInfo.GetUserStoreForApplication(StorageName), mapSchema);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    throw;
                }
            }
        }

        public void Dispose()
        {

        }
    }

    [ExportMetadata("DatabaseName", VirtuosoDatabase.ICDCM10)]
    [Export(typeof(IDatabaseWrapper))]
    public class ICDCM10Database : IDatabaseWrapper
    {
        //NOTE: If you change the properties on the objects that you are caching - increase the version 
        //      of the .dbs file - the old file will be deleted and the new file re-cached.
        //      This database currently caches the following objects - 
        //              CacheConfiguration and CachedICDCM10Code and CachedAllergyCode.
        public static readonly string
            StorageName =
                "ICDCM10DBS009"; //NOTE: check code in RemovePriorICDCM10Versions() if you change the value of StorageName

        private object SyncLock = new object();
        public ISimpleStorage Storage { get; internal set; }
        ILogger Logger { get; set; }

        [ImportingConstructor]
        public ICDCM10Database(ILogger _logger)
        {
            Logger = _logger;
        }

        /// <summary>
        ///     The name of the database instance
        /// </summary>
        public string Name => "ICDCM10 Database";

        public async Task Start()
        {
            Logger.Log(TraceEventType.Verbose, Name, "BEGIN Start()");

            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            await RemovePriorDiskVersions();
            //Initialize(); //cannot 'open' the database just yet - may not have the encryption key...
            Logger.Log(TraceEventType.Verbose, Name, "END Start()");
        }

        public async Task RemovePriorDiskVersions(bool delete_current_version_only = false)
        {
            await DatabaseExtensions.RemovePriorDiskVersions("ICDCM10DBS", StorageName, delete_current_version_only);
        }

        public void Initialize()
        {
            //Logger.Log(VirtuosoLogLevel.TRACE, "BEGIN Initialize()", this.Name);

            lock (SyncLock)
            {
                try
                {
                    var mapSchema =
                        TableSchemaRepository.GetSchema(ReferenceTableName.Create(ReferenceTableName.ICDCM10));
                    Storage = new FixedLengthFlatFileStorage(
                        ApplicationStoreInfo.GetUserStoreForApplication(StorageName), mapSchema);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    //Logger.Log(VirtuosoLogLevel.FATAL, ex.Message, this.Name);
                    throw;
                }
                //else
                //{
                //Logger.Log(VirtuosoLogLevel.DEBUG, "Database already initialized", this.Name);
                //}
            }
            //Logger.Log(VirtuosoLogLevel.TRACE, "END Initialize()", this.Name);
        }


        public void Dispose()
        {
            //if (Current != null)
            //{
            //    Current.Flush();
            //    Current.Close();
            //    Current = null;
            //}
        }
    }

    [ExportMetadata("DatabaseName", VirtuosoDatabase.ICDPCS9)]
    [Export(typeof(IDatabaseWrapper))]
    public class ICDPCS9Database : IDatabaseWrapper
    {
        //NOTE: If you change the properties on the objects that you are caching - increase the version 
        //      of the .dbs file - the old file will be deleted and the new file re-cached.
        //      This database currently caches the following objects - 
        //              CacheConfiguration and CachedICDPCS9Code and CachedAllergyCode.
        public static readonly string
            StorageName =
                "ICDPCS9DBS009"; //NOTE: check code in RemovePriorICDPCS9Versions() if you change the value of StorageName

        private object SyncLock = new object();
        public ISimpleStorage Storage { get; internal set; }
        ILogger Logger { get; set; }

        [ImportingConstructor]
        public ICDPCS9Database(ILogger _logger)
        {
            Logger = _logger;
        }

        /// <summary>
        ///     The name of the database instance
        /// </summary>
        public string Name => "ICDPCS9 Database";

        public async Task Start()
        {
            Logger.Log(TraceEventType.Verbose, Name, "BEGIN Start()");

            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            await RemovePriorDiskVersions();
            //Initialize(); //cannot 'open' the database just yet - may not have the encryption key...

            Logger.Log(TraceEventType.Verbose, Name, "END Start()");
        }

        public async Task RemovePriorDiskVersions(bool delete_current_version_only = false)
        {
            await DatabaseExtensions.RemovePriorDiskVersions("ICDPCS9DBS", StorageName, delete_current_version_only);
        }

        public void Initialize()
        {
            //Logger.Log(VirtuosoLogLevel.TRACE, "BEGIN Initialize()", this.Name);

            lock (SyncLock)
            {
                //if (Current == null)
                {
                    try
                    {
                        var mapSchema =
                            TableSchemaRepository.GetSchema(ReferenceTableName.Create(ReferenceTableName.ICDPCS9));
                        Storage = new FixedLengthFlatFileStorage(
                            ApplicationStoreInfo.GetUserStoreForApplication(StorageName), mapSchema);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        //Logger.Log(VirtuosoLogLevel.FATAL, ex.Message, this.Name);
                        throw;
                    }
                }
            }
            //Logger.Log(VirtuosoLogLevel.TRACE, "END Initialize()", this.Name);
        }


        public void Dispose()
        {
            //if (Current != null)
            //{
            //    Current.Flush();
            //    Current.Close();
            //    Current = null;
            //}
        }
    }

    [ExportMetadata("DatabaseName", VirtuosoDatabase.ICDPCS10)]
    [Export(typeof(IDatabaseWrapper))]
    public class ICDPCS10Database : IDatabaseWrapper
    {
        //NOTE: If you change the properties on the objects that you are caching - increase the version 
        //      of the .dbs file - the old file will be deleted and the new file re-cached.
        //      This database currently caches the following objects - 
        //              CacheConfiguration and CachedICDPCS10Code and CachedAllergyCode.
        public static readonly string
            StorageName =
                "ICDPCS10DBS009"; //NOTE: check code in RemovePriorICDPCS10Versions() if you change the value of StorageName

        private object SyncLock = new object();
        public ISimpleStorage Storage { get; internal set; }

        ILogger Logger { get; set; }

        [ImportingConstructor]
        public ICDPCS10Database(ILogger _logger)
        {
            Logger = _logger;
        }

        /// <summary>
        ///     The name of the database instance
        /// </summary>
        public string Name => "ICDPCS10 Database";

        public async Task Start()
        {
            Logger.Log(TraceEventType.Verbose, Name, "BEGIN Start()");

            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            await RemovePriorDiskVersions();
            //Initialize(); //cannot 'open' the database just yet - may not have the encryption key...

            Logger.Log(TraceEventType.Verbose, Name, "END Start()");
        }

        public async Task RemovePriorDiskVersions(bool delete_current_version_only = false)
        {
            await DatabaseExtensions.RemovePriorDiskVersions("ICDPCS10DBS", StorageName, delete_current_version_only);
        }

        public void Initialize()
        {
            //Logger.Log(VirtuosoLogLevel.TRACE, "BEGIN Initialize()", this.Name);

            lock (SyncLock)
            {
                //if (Current == null)
                {
                    try
                    {
                        var mapSchema =
                            TableSchemaRepository.GetSchema(ReferenceTableName.Create(ReferenceTableName.ICDPCS10));
                        Storage = new FixedLengthFlatFileStorage(
                            ApplicationStoreInfo.GetUserStoreForApplication(StorageName), mapSchema);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        //Logger.Log(VirtuosoLogLevel.FATAL, ex.Message, this.Name);
                        throw;
                    }
                }
                //else
                //{
                //Logger.Log(VirtuosoLogLevel.DEBUG, "Database already initialized", this.Name);
                //}
            }
            //Logger.Log(VirtuosoLogLevel.TRACE, "END Initialize()", this.Name);
        }


        public void Dispose()
        {
            //if (Current != null)
            //{
            //    Current.Flush();
            //    Current.Close();
            //    Current = null;
            //}
        }
    }

    [ExportMetadata("DatabaseName", VirtuosoDatabase.ICDGEMS9)]
    [Export(typeof(IDatabaseWrapper))]
    public class ICDGEMS9Database : IDatabaseWrapper
    {
        //NOTE: If you change the properties on the objects that you are caching - increase the version 
        //      of the .dbs file - the old file will be deleted and the new file re-cached.
        //      This database currently caches the following objects - 
        //              CacheConfiguration and CachedICDGEMS9Code and CachedAllergyCode.
        public static readonly string
            StorageName =
                "ICDGEMS9DBS009"; //NOTE: check code in RemovePriorICDGEMS9Versions() if you change the value of StorageName

        private object SyncLock = new object();
        public ISimpleStorage Storage { get; internal set; }

        ILogger Logger { get; set; }

        [ImportingConstructor]
        public ICDGEMS9Database(ILogger _logger)
        {
            Logger = _logger;
        }

        /// <summary>
        ///     The name of the database instance
        /// </summary>
        public string Name => "ICDGEMS9 Database";

        public async Task Start()
        {
            Logger.Log(TraceEventType.Verbose, Name, "BEGIN Start()");

            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            await RemovePriorDiskVersions();
            //Initialize(); //cannot 'open' the database just yet - may not have the encryption key...

            Logger.Log(TraceEventType.Verbose, Name, "END Start()");
        }

        public async Task RemovePriorDiskVersions(bool delete_current_version_only = false)
        {
            await DatabaseExtensions.RemovePriorDiskVersions("ICDGEMS9DBS", StorageName, delete_current_version_only);
        }

        public void Initialize()
        {
        }


        public void Dispose()
        {
        }
    }

    [ExportMetadata("DatabaseName", VirtuosoDatabase.ICDGEMS10)]
    [Export(typeof(IDatabaseWrapper))]
    public class ICDGEMS10Database : IDatabaseWrapper
    {
        //NOTE: If you change the properties on the objects that you are caching - increase the version 
        //      of the .dbs file - the old file will be deleted and the new file re-cached.
        //      This database currently caches the following objects - 
        //              CacheConfiguration and CachedICDGEMS10Code and CachedAllergyCode.
        public static readonly string
            StorageName =
                "ICDGEMS10DBS009"; //NOTE: check code in RemovePriorICDGEMS10Versions() if you change the value of StorageName

        private object SyncLock = new object();
        public ISimpleStorage Storage { get; internal set; }

        ILogger Logger { get; set; }

        [ImportingConstructor]
        public ICDGEMS10Database(ILogger _logger)
        {
            Logger = _logger;
        }

        /// <summary>
        ///     The name of the database instance
        /// </summary>
        public string Name => "ICDGEMS10 Database";

        public async Task Start()
        {
            Logger.Log(TraceEventType.Verbose, Name, "BEGIN Start()");

            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            await RemovePriorDiskVersions();
            //Initialize(); //cannot 'open' the database just yet - may not have the encryption key...

            Logger.Log(TraceEventType.Verbose, Name, "END Start()");
        }

        public async Task RemovePriorDiskVersions(bool delete_current_version_only = false)
        {
            await DatabaseExtensions.RemovePriorDiskVersions("ICDGEMS10DBS", StorageName, delete_current_version_only);
        }

        public void Initialize()
        {
        }


        public void Dispose()
        {
        }
    }

    [ExportMetadata("DatabaseName", VirtuosoDatabase.ICDCategory)]
    [Export(typeof(IDatabaseWrapper))]
    public class ICDCategoryDatabase : IDatabaseWrapper
    {
        //NOTE: If you change the properties on the objects that you are caching - increase the version 
        //      of the .dbs file - the old file will be deleted and the new file re-cached.
        //      This database currently caches the following objects - 
        //              CacheConfiguration and CachedICDGEMS10Code and CachedAllergyCode.
        public static readonly string
            StorageName =
                "ICDCategoryDBS009"; //NOTE: check code in RemovePriorICDGEMS10Versions() if you change the value of StorageName

        private object SyncLock = new object();
        public ISimpleStorage Storage { get; internal set; }

        ILogger Logger { get; set; }

        [ImportingConstructor]
        public ICDCategoryDatabase(ILogger _logger)
        {
            Logger = _logger;
        }

        /// <summary>
        ///     The name of the database instance
        /// </summary>
        public string Name => "ICDCategory Database";

        public async Task Start()
        {
            Logger.Log(TraceEventType.Verbose, Name, "BEGIN Start()");

            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            await RemovePriorDiskVersions();
            Logger.Log(TraceEventType.Verbose, Name, "END Start()");
        }

        public async Task RemovePriorDiskVersions(bool delete_current_version_only = false)
        {
            await DatabaseExtensions.RemovePriorDiskVersions("ICDCategoryDBS", StorageName,
                delete_current_version_only);
        }

        public void Initialize()
        {
            //Logger.Log(VirtuosoLogLevel.TRACE, "BEGIN Initialize()", this.Name);

            lock (SyncLock)
            {
                //if (Current == null)
                {
                    try
                    {
                        var mapSchema =
                            TableSchemaRepository.GetSchema(ReferenceTableName.Create(ReferenceTableName.ICDCategory));
                        Storage = new FixedLengthFlatFileStorage(
                            ApplicationStoreInfo.GetUserStoreForApplication(StorageName), mapSchema);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        //Logger.Log(VirtuosoLogLevel.FATAL, ex.Message, this.Name);
                        throw;
                    }
                }
                //else
                //{
                //Logger.Log(VirtuosoLogLevel.DEBUG, "Database already initialized", this.Name);
                //}
            }
            //Logger.Log(VirtuosoLogLevel.TRACE, "END Initialize()", this.Name);
        }


        public void Dispose()
        {
            //if (Current != null)
            //{
            //    Current.Flush();
            //    Current.Close();
            //    Current = null;
            //}
        }
    }
}