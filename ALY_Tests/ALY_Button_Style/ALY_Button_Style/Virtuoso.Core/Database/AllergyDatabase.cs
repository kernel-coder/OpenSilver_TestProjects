#region Usings

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Virtuoso.Client.Core;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Portable.Database;

#endregion

namespace Virtuoso.Core.Database
{
    [ExportMetadata("DatabaseName", VirtuosoDatabase.Allergy)]
    [Export(typeof(IDatabaseWrapper))]
    public class AllergyDatabase : IDatabaseWrapper
    {
        //NOTE: If you change the properties on the objects that you are caching - increase the version 
        //      of the .dbs file - the old file will be deleted and the new file re-cached.
        //      This database currently caches the following objects - 
        //              CacheConfiguration and CachedICDCode and CachedAllergyCode.
        public static readonly string
            StorageName =
                "AllergyDBS008"; //NOTE: check code in RemovePriorAllergyVersions() if you change the value of StorageName

        private object SyncLock = new object();
        public ISimpleStorage Storage { get; internal set; }
        
        ILogger Logger { get; set; }

        [ImportingConstructor]
        public AllergyDatabase(ILogger _logger)
        {
            Logger = _logger;
        }

        public string Name => "Allergy Database";

        public async Task Start()
        {
            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            await RemovePriorDiskVersions();
        }

        public async Task RemovePriorDiskVersions(bool delete_current_version_only = false)
        {
            await DatabaseExtensions.RemovePriorDiskVersions("AllergyDBS", StorageName, delete_current_version_only);
        }

        public void Initialize()
        {
            lock (SyncLock)
            {
                try
                {
                    var mapSchema =
                        TableSchemaRepository.GetSchema(ReferenceTableName.Create(ReferenceTableName.Allergy));
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

        #region IDisposable Members

        public void Dispose()
        {

        }

        #endregion
    }
}