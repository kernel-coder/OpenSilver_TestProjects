#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Portable.Database;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.ICDCategory)]
    [Export(typeof(ICache))]
    public class ICDCategoryCache : FlatFileCacheBase<Server.Data.ICDCategory>
    {
        public static ICDCategoryCache Current { get; private set; }

        public EntityContainer EntityContainer { get; } = new VirtuosoDomainContext.VirtuosoDomainContextEntityContainer();

        [ImportingConstructor]
        public ICDCategoryCache(ILogger logManager)
            : base(logManager, true)
        {
            if (Current == this)
            {
                throw new InvalidOperationException("ICDCATEGORY already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.ICDCategory;
            DatabaseWrapper = DatabaseService.Current.DatabaseFor(VirtuosoDatabase.ICDCategory);
        }


#if !OPENSILVER
        protected override void DeserializeData(RecordSet recordSet)
        {
            var entity = NewObject();
            ICD.ICDCategoryExtensions.RecordSetToICDCategory(recordSet, entity);
            _DataList.Add(entity);
        }
#endif

        public override void LoadDataComplete()
        {
            EntityContainer.LoadEntities(_DataList);
        }

        public async Task<IEnumerable<Server.Data.ICDCategory>> GetRootCategoryCodes(bool includeEmpty = true)
        {
            await EnsureDataLoadedFromDisk();
            var ret = EntityContainer.GetEntitySet<Server.Data.ICDCategory>()
                .Where(c => c.ICDParentCategoryKey.HasValue == false).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Server.Data.ICDCategory { ICDCategoryKey = 0, ICDCategoryDescription = " " });
            }

            return ret;
        }

        public async Task<IEnumerable<Server.Data.ICDCategory>> GetSubCategoryCodes(bool includeEmpty = true)
        {
            await EnsureDataLoadedFromDisk();
            var ret = EntityContainer.GetEntitySet<Server.Data.ICDCategory>()
                .Where(c => c.ICDParentCategoryKey.HasValue).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new Server.Data.ICDCategory { ICDCategoryKey = 0, ICDCategoryDescription = " " });
            }

            return ret;
        }
    }
}