#region Usings

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Ria.Sync;
using Virtuoso.Client.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Portable.Database;
using Virtuoso.Portable.Model;
using Virtuoso.Portable.Utility;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.Allergy)]
    [Export(typeof(ICache))]
    public class AllergyCache : FlatFileCacheBase<CachedAllergyCode>
    {
        public static AllergyCache Current { get; private set; }
        VirtuosoDomainContext Context;

        [ImportingConstructor]
        public AllergyCache(ILogger logManager)
            : base(logManager)
        {
            if (Current == this)
            {
                throw new InvalidOperationException("AllergyCache already initialized.");
            }

            Current = this;
            Context = new VirtuosoDomainContext();
            CacheName = ReferenceTableName.Allergy;
            DatabaseWrapper = DatabaseService.Current.DatabaseFor(VirtuosoDatabase.Allergy);
        }

        public override async System.Threading.Tasks.Task LoadRelatedData()
        {
            Context.EntityContainer.Clear();

            // Load AllergyCodeDescriptions from disk
            var cache = await RIACacheManager.Initialize(
                DatabaseWrapper.Storage.DataSetPath(withName: false),
                CacheName,
                Constants.ENTITY_TYPENAME_FORMAT,
                isReference: true); //NOTE: can throw DirectoryNotFoundException
            await cache.Load(Context);
        }

        protected override void OnEntityDeserialized(CachedAllergyCode entity)
        {
            // Set DisplayName override
            var display_name_override_obj = Context.AllergyCodeDescriptions
                .Where(a => a.AllergyCodeKey == entity.AllergyCodeKey).FirstOrDefault();
            if (display_name_override_obj != null)
            {
                entity.DisplayName = display_name_override_obj.DisplayName;
            }
            else
            {
                entity.DisplayName = entity.SubstanceName;
            }

            entity.FullText = Portable.Extensions.AllergyCacheExtensions.CalculateFullText(entity);
        }

#if !OPENSILVER
        protected override void DeserializeData(RecordSet recordSet)
        {
            var entity = NewObject();
            Portable.Extensions.AllergyCacheExtensions.RecordSetToCachedAllergyCode(recordSet, entity);
            OnEntityDeserialized(entity);
            _DataList.Add(entity);
        }
#endif

        public override System.Threading.Tasks.Task DownloadAdditionalFilesAsync(DateTime new_anchor)
        {
            return Context
                .LoadAsync(Context.GetAllergyCodeDescriptionQuery())
                .ContinueWith(async ret =>
                {
                    // Save AllergyCodeDescriptions to disk
                    var cache = await RIACacheManager.Initialize(
                        DatabaseWrapper.Storage.DataSetPath(withName: false),
                        CacheName,
                        Constants.ENTITY_TYPENAME_FORMAT,
                        isReference: true); //NOTE: can throw DirectoryNotFoundException
                    await cache.Save(Context);
                });
        }

        //Called by maintenance screen after code saved to server database
        public async System.Threading.Tasks.Task UpdateAllergyCodeCache(AllergyCode allergyCode)
        {
            await EnsureDataLoadedFromDisk();

            // FYI - really only the description can be updated...
            var _allergyCache = (from a in Data
                where a.AllergyCodeKey == allergyCode.AllergyCodeKey
                select a).FirstOrDefault();
            if (_allergyCache != null)
            {
                DynamicCopy.CopyProperties(allergyCode, _allergyCache);
                _allergyCache.FullText = Portable.Extensions.AllergyCacheExtensions.CalculateFullText(_allergyCache);
            }
            // NOTE: We're not updating the anchor, so this AND any other changes made by other users will 
            //       be re-sync'd upon next restart.
        }
    }
}