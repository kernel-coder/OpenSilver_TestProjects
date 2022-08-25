#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Ria.Sync.Occasional;
using OpenRiaServices.DomainServices.Client;

#endregion

namespace Virtuoso.Core.Occasional
{
    //Extension method helper class to import/export, serialize/deserialize OfflinableEntity
    //Also contains fixup helper method - FixupEntityCollectionRelationships
    public static class DomainContextPersistenceExtensions
    {
        public static List<List<OfflinableEntity>> Export(this DomainContext source)
        {
            if (source == null)
            {
                throw new ArgumentNullException();
            }

            IEnumerable<Type> entityTypes = GetEntityTypesInContext(source.GetType());
            EntityChangeSet changeSet = source.EntityContainer.GetChanges();

            List<List<OfflinableEntity>> offlineEntityLists = new List<List<OfflinableEntity>>();

            foreach (Type entityType in entityTypes)
            {
                EntitySet entitySet = source.EntityContainer.GetEntitySet(entityType);

                List<OfflinableEntity> offlineEntityList = new List<OfflinableEntity>();
                foreach (Entity entity in entitySet)
                {
                    OfflinableEntity offEntity = new OfflinableEntity(entity);
                    offlineEntityList.Add(offEntity);
                }

                //Get deleted entities from changeset
                IEnumerable<Entity> removedEntites = changeSet.RemovedEntities.Where(x => x.GetType().Equals(entityType));

                //Need to save deleted entities as well
                foreach (Entity entity in removedEntites)
                {
                    OfflinableEntity offEntity = new OfflinableEntity(entity);
                    offlineEntityList.Add(offEntity);
                }

                offlineEntityLists.Add(offlineEntityList);
            }

            return offlineEntityLists;
        }

        public static void Import(this DomainContext source, List<List<OfflinableEntity>> data)
        {
            //source.Restore(data); getting ambiguous match found...
            source.Load(data);
        }

        public static string Serialize(this DomainContext domainContext, List<List<OfflinableEntity>> source)
        {
            IEnumerable<Type> entityTypes = GetEntityTypesInContext(domainContext.GetType());
            string jsonData = JsonUtil.SerializeList(source, entityTypes);
            return jsonData;
        }

        public static List<List<OfflinableEntity>> Deserialize(this DomainContext domainContext, string source)
        {
            IEnumerable<Type> entityTypes = GetEntityTypesInContext(domainContext.GetType());
            var data = JsonUtil.DeserializeList(source, entityTypes);
            return data;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Add child entities back to their parent's EntityCollectionView, doing so will also set the EntityRef on the child's parent
        // Caveat: unknown whether this works when the primary/foreign keys are not integers
        //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static EntitySet GetEntitySetForEntityType(DomainContext domainContext, Type entityType)
        {
            //Get entitylist for entityType
            MethodInfo methodInfo = domainContext.EntityContainer.GetType().GetMethod("GetEntitySet", Type.EmptyTypes);
            Type[] genericArguments = { entityType };
            MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(genericArguments);
            EntitySet entitySet = (EntitySet)genericMethodInfo.Invoke(domainContext.EntityContainer, null);
            return entitySet;
        }

        //This caches the reflected information about the entities for a DomainContext for performance
        private static readonly Dictionary<Type, IEnumerable<Type>> DomainContextEntityTypeCache = new Dictionary<Type, IEnumerable<Type>>();

        //Gets list of entity types exposed in the domain context
        private static IEnumerable<Type> GetEntityTypesInContext(Type domainContextType)
        {
            if (!DomainContextEntityTypeCache.ContainsKey(domainContextType))
            {
                IEnumerable<PropertyInfo> entityListProps = domainContextType
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => typeof(EntitySet).IsAssignableFrom(p.PropertyType));
                IEnumerable<Type> entityTypes = entityListProps.Select(e => e.PropertyType.GetGenericArguments()[0]);
                DomainContextEntityTypeCache.Add(domainContextType, entityTypes);
            }

            return DomainContextEntityTypeCache[domainContextType];
        }
    }
}