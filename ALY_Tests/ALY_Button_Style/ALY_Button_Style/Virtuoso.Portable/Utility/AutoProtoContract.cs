namespace Virtuoso.Common.Utility
{
   using ProtoBuf;
   using ProtoBuf.Meta;
   using System;
   using System.Collections.Generic;
   using System.Linq;
   using System.Reflection;
   using System.Runtime.Serialization;
   using System.Text;

   public class AutoProtoContract
   {
      private static readonly Type _dataContractType = typeof(DataContractAttribute);
      private static readonly Type _dataMemberType = typeof(DataMemberAttribute);
      private static readonly Type _protoContractType = typeof(ProtoContractAttribute);

      public static void SetupContracts(Assembly assembly)
      {
         foreach(var type in assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(_dataContractType, true).Any()))
         {
            SetupContract(type);
         }
      }

      public static void SetupContract(Type type)
      {
         if(
            RuntimeTypeModel.Default.CanSerialize(type)
            || RuntimeTypeModel.Default[type].GetFields().Any())
         {
            return;
         }

         // Ignore where proto contract manually defined
         if(type.GetCustomAttributes(_protoContractType, false).Any())
         {
            return;
         }

         var postTypesToAdd = new List<Type>();
         // Removing declared only will affect property position with base classes in client
         var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
         int propertyIndex = 1;

         var hasDataContract = type.GetCustomAttributes(_dataContractType, false).Any();

         foreach(var property in properties
             .OrderBy(p => p.DeclaringType == type ? 0 : 1)
             .ThenBy(p => p.Name))
         {
            if(property.GetSetMethod(false) == null)
            {
               continue;
            }

            if(
                hasDataContract
                && !property.GetCustomAttributes(_dataMemberType, false).Any())
            {
               continue;
            }

            Type innerType = property.PropertyType.IsGenericType ? property.PropertyType.GetGenericArguments().Single() : property.PropertyType;

            var hasOwnContract = innerType.GetCustomAttributes(_protoContractType, false).Any()
               || innerType.GetCustomAttributes(_dataContractType, false).Any();

            if(hasOwnContract)
            {
               postTypesToAdd.Add(innerType);
               continue; // Don't serialize other entities in the graph, to avoid circular references
            }

            RuntimeTypeModel.Default[type].AddField(propertyIndex, property.Name);
            propertyIndex++;
         }

#if DEBUG
         var fields = string.Join("\n", RuntimeTypeModel.Default[type].GetFields().Select(f => $"{f.FieldNumber}: {f.Name}"));
#endif
         foreach(var graphType in postTypesToAdd)
         {
            SetupContract(graphType);
         }
      }
   }
}
