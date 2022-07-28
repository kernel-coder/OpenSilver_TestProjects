#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

namespace Virtuoso.Core.Utility
{
    public class VirtuosoEntityProperties
    {
        static string path = "Virtuoso.Server.Data";
        public static Dictionary<Type, PropertyInfo[]> EntityMetadata = new Dictionary<Type, PropertyInfo[]>();

        public static void BuildEntityMetadata()
        {
            EntityMetadata = new Dictionary<Type, PropertyInfo[]>();

            var tables = Assembly.GetExecutingAssembly().GetTypes()
                .Where(p => p.Namespace != null && p.IsClass && p.IsVisible && p.Namespace.Equals(path))
                .OrderBy(p => p.Name).ToList();
            tables.ForEach(t => { EntityMetadata.Add(t, t.GetProperties()); });
        }

        public static Type GetEntity(string table)
        {
            return EntityMetadata.Where(p => p.Key.Name.Equals(table)).FirstOrDefault().Key;
        }

        public static PropertyInfo[] GetEntityProperties(string table)
        {
            return EntityMetadata.Where(p => p.Key.Name.Equals(table)).FirstOrDefault().Value;
        }

        public static PropertyInfo GetEntityProperty(string table, string property)
        {
            var props = EntityMetadata.Where(p => p.Key.Name.Equals(table)).FirstOrDefault().Value;
            return props.Where(p => p.Name.Equals(property)).FirstOrDefault();
        }
    }
}