using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Virtuoso.Portable.Model;
using System.Threading.Tasks;
using Virtuoso.Client.Infrastructure.Storage;

namespace Virtuoso.Portable.Database
{
    public class RecordSet
    {
        public Field[] Fields { get; internal set; }
        public RecordSet(Field[] fields)
        {
            Fields = fields;
        }
        public bool ColumnExists(string name)
        {
            return Fields.Any(f => f.Name == name);
        }
        public string GetValue(string name)
        {
            var ret = (from rr in Fields
                       where rr.Name == name
                       //select rr.Value).FirstOrDefault();  //returns null if not found
                       select rr.Value).DefaultIfEmpty(string.Empty).First(); //returns "" if not found
            return ret.Trim();
        }
    }

    public static class RecordSetExtensions
    {
        //TODO: refactor logic to use delegates for parts of implementation that change - e.g. the TryParser...

        public static string GetString(this RecordSet rs, string columnName, bool throwIfColumnNotExists = false, string defaultValue = null)
        {
            if (throwIfColumnNotExists && !rs.ColumnExists(columnName))
                throw new ArgumentOutOfRangeException(columnName);

            string __value = rs.GetValue(columnName);

            if (string.IsNullOrEmpty(__value)) //have an empty value from the RecordSet for columnName
            {
                if (string.IsNullOrEmpty(defaultValue))
                    return __value;  //no default value supplied
                else
                    return defaultValue;  //no __value, but have a default value
            }
            else
                return __value;
        }

        public static short GetShort(this RecordSet rs, string columnName, bool throwIfColumnNotExists = false, short? defaultValue = null)
        {
            if (throwIfColumnNotExists && !rs.ColumnExists(columnName))
                throw new ArgumentOutOfRangeException(columnName);

            short __value;
            if (Int16.TryParse(rs.GetValue(columnName), out __value))
                return __value;
            else
            {
                if (defaultValue.HasValue)
                    return defaultValue.Value;
                else
                    return Int16.MinValue;
            }
        }

        public static short? GetNullableShort(this RecordSet rs, string columnName, bool throwIfColumnNotExists = false, short? defaultValue = null)
        {
            if (throwIfColumnNotExists && !rs.ColumnExists(columnName))
                throw new ArgumentOutOfRangeException(columnName);

            short __value;
            if (Int16.TryParse(rs.GetValue(columnName), out __value))
                return __value;
            else
            {
                if (defaultValue.HasValue)
                    return defaultValue.Value;
                else
                    return null;
            }
        }

        public static int GetInteger(this RecordSet rs, string columnName, bool throwIfColumnNotExists = false, int? defaultValue = null)
        {
            if (throwIfColumnNotExists && !rs.ColumnExists(columnName))
                throw new ArgumentOutOfRangeException(columnName);

            int __value;
            if (Int32.TryParse(rs.GetValue(columnName), out __value))
                return __value;
            else
            {
                if (defaultValue.HasValue)
                    return defaultValue.Value;
                else
                    return Int32.MinValue;
            }
        }

        public static int? GetNullableInteger(this RecordSet rs, string columnName, bool throwIfColumnNotExists = false, int? defaultValue = null)
        {
            if (throwIfColumnNotExists && !rs.ColumnExists(columnName))
                throw new ArgumentOutOfRangeException(columnName);

            int __value;
            if (Int32.TryParse(rs.GetValue(columnName), out __value))
                return __value;
            else
            {
                if (defaultValue.HasValue)
                    return defaultValue.Value;
                else
                    return null;
            }
        }

        public static Boolean GetBoolean(this RecordSet rs, string columnName, bool throwIfColumnNotExists = false, Boolean? defaultValue = null)
        {
            if (throwIfColumnNotExists && !rs.ColumnExists(columnName))
                throw new ArgumentOutOfRangeException(columnName);
            var __rsValue = rs.GetValue(columnName);
            int __intValue;
            if (Int32.TryParse(__rsValue, out __intValue))
            {
                if (__intValue == 1 || __intValue == 0)
                    return (__intValue == 1) ? true : false;
                else
                {
                    if (defaultValue.HasValue)
                        return defaultValue.Value;
                    else
                        return false;  //default return value for Boolean is false;
                }
            }
            Boolean __boolValue;
            if (Boolean.TryParse(__rsValue, out __boolValue))
                return __boolValue;
            if (defaultValue.HasValue)
                return defaultValue.Value;
            else
                return false;  //default return value for Boolean is false;                
        }

        //public static Boolean GetBoolean(this RecordSet rs, string columnName, bool throwIfColumnNotExists = false, Boolean? defaultValue = null)
        //{
        //    if (throwIfColumnNotExists && !rs.ColumnExists(columnName))
        //        throw new ArgumentOutOfRangeException(columnName);
        //    var __rsValue = rs.GetValue(columnName);
        //    Boolean __boolValue;
        //    if (Boolean.TryParse(__rsValue, out __boolValue))
        //        return __boolValue;
        //    else
        //    {
        //        int __intValue;
        //        if (Int32.TryParse(__rsValue, out __intValue))
        //        {
        //            if (__intValue == 1 || __intValue == 0)
        //                return (__intValue == 1) ? true : false;
        //        }                
        //        if (defaultValue.HasValue)
        //            return defaultValue.Value;
        //        else
        //            return false;  //default return value for Boolean is false;                
        //    }
        //}

        public static Boolean? GetNullableBoolean(this RecordSet rs, string columnName, bool throwIfColumnNotExists = false, Boolean? defaultValue = null)
        {
            if (throwIfColumnNotExists && !rs.ColumnExists(columnName))
                throw new ArgumentOutOfRangeException(columnName);
            var __rsValue = rs.GetValue(columnName);
            if (__rsValue.Equals("1"))
                return true;
            if (__rsValue.Equals("0"))
                return false;
            //int __intValue;
            //if (Int32.TryParse(__rsValue, out __intValue))
            //{
            //    if (__intValue == 1 || __intValue == 0)
            //        return (__intValue == 1) ? true : false;
            //    else
            //    {
            //        if (defaultValue.HasValue)
            //            return defaultValue.Value;
            //        else
            //            return null;
            //    }
            //}
            Boolean __boolValue;
            if (Boolean.TryParse(__rsValue, out __boolValue))
                return __boolValue;
            if (defaultValue.HasValue)
                return defaultValue.Value;
            else
                return null;
        }

        public static DateTime GetDateTime(this RecordSet rs, string columnName, bool throwIfColumnNotExists = false, DateTime? defaultValue = null)
        {
            if (throwIfColumnNotExists && !rs.ColumnExists(columnName))
                throw new ArgumentOutOfRangeException(columnName);

            DateTime __value;
            if (DateTime.TryParse(rs.GetValue(columnName), out __value))
                return __value;
            else
            {
                if (defaultValue.HasValue)
                    return defaultValue.Value;
                else
                    return DateTime.MinValue;
            }
        }

        public static DateTime? GetNullableDateTime(this RecordSet rs, string columnName, bool throwIfColumnNotExists = false, DateTime? defaultValue = null)
        {
            if (throwIfColumnNotExists && !rs.ColumnExists(columnName))
                throw new ArgumentOutOfRangeException(columnName);

            DateTime __value;
            if (DateTime.TryParse(rs.GetValue(columnName), out __value))
                return __value;
            else
            {
                if (defaultValue.HasValue)
                    return defaultValue.Value;
                else
                    return null;
            }
        }
    }

    public class Field
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public interface IStorageStats
    {
        Task SaveStats(CacheConfiguration tableStats);
        Task SaveStats(string tableName, DateTime anchor, DateTime? updatedDate, bool loadCompleted, int totalRecords);
        Task<CacheConfiguration> GetStats(string tableName);
    }

    public interface ISimpleStorage : IStorageStats
    {
        string DataSetPath(bool withName = true);

        void Save<T>(IEnumerable<T> entitySet, Func<T, bool> callback);
        Task Load(Action<RecordSet, string> callback);
#if OPENSILVER
        Task<byte[]> LoadFile();
#endif
    }

    public class StorageStatsImpl : IStorageStats
    {
        protected string FileExtension { get; set; }
        protected TableSchema ConfigurationSchema { get; set; }
        protected string Location { get; set; }     //full path to location where database can store files 

        public StorageStatsImpl(string location, string fileExtension = ".dat")
        {
            Location = location; //E.G. "C:\\Users\\<user>\\AppData\\Local\\Delta Health Technologies\\Crescendo\\thag-scrub\\AddressMapDBS012"
            FileExtension = fileExtension;

            var cacheName = ReferenceTableName.Create(ReferenceTableName.CacheConfiguration);
            ConfigurationSchema = Virtuoso.Portable.TableSchemaRepository.GetSchema(cacheName);
        }

        public async Task SaveStats(CacheConfiguration tableStats)
        {
            var anchorFileName = this.ConfigurationSchema.Name + FileExtension;
            var anchorFileNameFQN = System.IO.Path.Combine(Location, anchorFileName);

            //delete and re-add file...
            await VirtuosoStorageContext.Current.DeleteFile(anchorFileNameFQN);

            await SaveObject(anchorFileNameFQN, tableStats, (e) =>
            {
                //var data = String.Format("{0,30}{1,22}{2,19}{3,1}{4,22}{5,19}",  //TODO: refactor to 'table definition'
                //    e.Name.ToString().Truncate(30),                                                   // 0  - 30  
                //    e.Anchor.ToString().Truncate(22),                                                 // 1  - 22
                //    e.TotalRecords.ToString().Truncate(19),                                           // 2  - 19
                //    (e.CacheLoadCompleted) ? "1" : "0",                                               // 3  - 1
                //    e.LastUpdatedDate.HasValue ? e.LastUpdatedDate.ToString().Truncate(22) : null,    // 4  - 22
                //    e.Ticks.ToString().Truncate(19));                                                 // 5 - 19
                var data = this.ConfigurationSchema.GetFixedWidthData(e);
                return data;
            });
        }

        public async Task SaveStats(string tableName, DateTime anchor, DateTime? updatedDate, bool loadCompleted = false, int totalRecords = 0)  //tableName = AddressMapping
        {
            var tableStats = new CacheConfiguration()
            {
                Anchor = anchor,  //client anchor date
                Name = tableName,
                TotalRecords = totalRecords,
                CacheLoadCompleted = loadCompleted,
                LastUpdatedDate = updatedDate,  //max server updated date
                Ticks = (updatedDate.HasValue) ? updatedDate.Value.Ticks : 0
            };

            await SaveStats(tableStats);
        }

        public async Task<CacheConfiguration> GetStats(string tableName)  //tableName = AddressMapping
        {
            var anchorFileName = this.ConfigurationSchema.Name + FileExtension;
            var anchorFileNameFQN = System.IO.Path.Combine(Location, anchorFileName);

            if (await VirtuosoStorageContext.Current.Exists(anchorFileNameFQN))
            {
                return await LoadObject<CacheConfiguration>(anchorFileNameFQN, (line) =>
                {
                    var stats = new CacheConfiguration() { Name = tableName };

                    RecordSet recordSet = SplitFixedWidth(line, false, this.ConfigurationSchema);

                    try
                    {
                        stats.Name = recordSet.Fields[0].Value.Trim();
                        stats.Anchor = DateTime.Parse(recordSet.Fields[1].Value.Trim());
                        stats.TotalRecords = Int32.Parse(recordSet.Fields[2].Value.Trim());

                        var _loaded = Int32.Parse(recordSet.Fields[3].Value.Trim());
                        stats.CacheLoadCompleted = (_loaded == 1) ? true : false;

                        DateTime _LastUpdatedDate;
                        if (DateTime.TryParse(recordSet.Fields[4].Value.Trim(), out _LastUpdatedDate))
                            stats.LastUpdatedDate = _LastUpdatedDate;

                        stats.Ticks = Int64.Parse(recordSet.Fields[5].Value.Trim());
                    }
                    catch (Exception err)
                    {
                        //System.ArgumentNullException
                        //System.FormatException
                        //System.OverflowException
                        System.Diagnostics.Debug.WriteLine(string.Format("Line: {0} Error: {1}", err.Message, line));

                        throw;
                    }
                    return stats;
                });
            }
            else
                return new CacheConfiguration() { Name = tableName };
        }

        #region Helper_Methods

        private async Task SaveObject<T>(string file, T entity, Func<T, string> typeMapper)
        {
            var data = typeMapper(entity);
            await VirtuosoStorageContext.Current.WriteToFile(file, data);
        }

        private async Task<T> LoadObject<T>(string file, Func<string, T> typeMapper)
        {
            T entity = default(T);
            var data = await VirtuosoStorageContext.Current.Read(file);

            if (data != null)
            {
                entity = typeMapper(data);
            }

            return entity;
        }

        //private static string[] SplitFixedWidth(string original, bool spaceBetweenItems, params int[] widths)
        //{
        //    string[] results = new string[widths.Length];
        //    int current = 0;

        //    for (int i = 0; i < widths.Length; ++i)
        //    {
        //        if (current < original.Length)
        //        {
        //            int len = Math.Min(original.Length - current, widths[i]);
        //            results[i] = original.Substring(current, len);
        //            current += widths[i] + (spaceBetweenItems ? 1 : 0);
        //        }
        //        else results[i] = string.Empty;
        //    }

        //    return results;
        //}

        protected RecordSet SplitFixedWidth(string original, bool spaceBetweenItems, TableSchema schemaStore)
        {
            return SplitFixedWidthInternal(original, spaceBetweenItems, schemaStore, schemaStore.LineSpec);
        }

        protected RecordSet SplitFixedWidthInternal(string original, bool spaceBetweenItems, TableSchema schemaStore, params int[] widths)
        {
            var columnLength = schemaStore.LineSpec.Length;
            Field[] fields = new Field[columnLength];
            int current = 0;

            for (int i = 0; i < columnLength; ++i)
            {
                fields[i] = new Field();
                fields[i].Name = schemaStore.GetColumnName(i);
                if (current < original.Length)
                {
                    int len = Math.Min(original.Length - current, widths[i]);
                    fields[i].Value = original.Substring(current, len);
                    current += widths[i] + (spaceBetweenItems ? 1 : 0);
                }
                else fields[i].Value = string.Empty;
            }
            return new RecordSet(fields);
        }

        #endregion Helper_Methods
    }

    public class FixedLengthFlatFileStorage : StorageStatsImpl, ISimpleStorage
    {
        public FixedLengthFlatFileStorage(string location, TableSchema schemaStore) : base(location)
        {
            MainSchema = schemaStore; //E.G. "AddressMapping"
        }

        #region Properties

        TableSchema MainSchema { get; set; }

        #endregion Properties

        #region Methods

        public string DataSetPath(bool withName = true)
        {
            if (!withName) return this.Location;

            //Silverlight: "C:\\Users\\<user>\\AppData\\Local\\Delta Health Technologies\\Crescendo\\thag-scrub\\AddressMapDBS012\\AddressMapping.dat"
            var file = MainSchema.Name + ".dat";
            var storageFileName = System.IO.Path.Combine(this.Location, file);
            return storageFileName;
        }

        public void Save<T>(IEnumerable<T> entitySet, Func<T, bool> callback)
        {
            Save<T>(MainSchema.Name, entitySet, callback);
        }

        public void Save<T>(string dataSetName, IEnumerable<T> entitySet, Func<T, bool> callback)
        {
            //REFERENCE: http://msdn.microsoft.com/en-us/library/ms182334.aspx
            FileStream fs = null;
            try
            {
                //Silverlight: "C:\\Users\\<user>\\AppData\\Local\\Delta Health Technologies\\Crescendo\\thag-scrub\\AddressMapDBS012\\AddressMapping.dat"
                var file = dataSetName + ".dat";
                var storageFileName = Path.Combine(this.Location, file);
                fs = new FileStream(storageFileName, FileMode.OpenOrCreate);
                using (TextWriter tr = new StreamWriter(fs))
                {
                    fs = null;

                    foreach (var entity in entitySet)
                    {
                        var outputEntity = callback(entity);
                        if (outputEntity)
                        {
                            var data = MainSchema.GetFixedWidthData(entity);
                            tr.WriteLine(data);
                        }
                    }
                }
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }

            // Write protobuf files as well for new OpenSilver code
            using (var pfs = new FileStream(Path.Combine(this.Location, dataSetName + ".bin"), FileMode.OpenOrCreate))
            {
                ProtoBuf.Serializer.Serialize(pfs, entitySet.ToArray());
            }
        }

        public string GetFilePath()
        {
            return Path.Combine(this.Location, MainSchema.Name + ".dat");
        }

        public async Task<byte[]> LoadFile()
        {
            var fullPathToCache = GetFilePath();
            return await VirtuosoStorageContext.Current.ReadAsBytes(fullPathToCache);
        }

        public async Task Load(Action<RecordSet, string> callback)
        {
            await Load(MainSchema.LineSpec, callback);
        }

        private async Task Load(
            int[] spec,
            Action<RecordSet, string> callback)
        {
            await Load(MainSchema.Name, spec, callback);
        }

        private async Task Load(
            string dataSetName,
            int[] spec,
            Action<RecordSet, string> callback)
        {
            var fullPathToCache = GetFilePath();
            try
            {
                //REFERENCE: http://msdn.microsoft.com/en-us/library/ms182334.aspx
                Stream fs = null;
                try
                {
                    var fileData = await VirtuosoStorageContext.Current.ReadAsBytes(fullPathToCache);
                    fs = new System.IO.MemoryStream(fileData);
                    using (TextReader tr = new StreamReader(fs))
                    {
                        fs = null;
                        string line;
                        while ((line = tr.ReadLine()) != null)
                        {
                            var ret = SplitFixedWidth(line, false, MainSchema);
                            callback(ret, line);
                        }
                    }
                }
                finally
                {
                    if (fs != null)
                        fs.Dispose();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("LoadCacheFromFile failure for cache: {0}.  Exception: {1}", fullPathToCache, e.ToString()));
                throw;
            }
        }

        #endregion Methods
    }
}
