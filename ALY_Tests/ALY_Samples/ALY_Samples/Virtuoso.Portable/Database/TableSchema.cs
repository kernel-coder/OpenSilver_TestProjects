using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Virtuoso.Portable.Database
{
    public class TableSchema
    {
        public TableSchema(string tableName)
        {
            _Name = tableName;
            CurrentColumnIndex = 0;
            _RowDefinition = new Dictionary<string, ColumnDefinition>();
            ColumnNames = new List<string>();
        }

        #region Properties

        private string _Name;
        public string Name
        {
            get { return _Name; }
        }

        int CurrentColumnIndex;

        List<string> ColumnNames;

        Dictionary<string, ColumnDefinition> _RowDefinition;
        public Dictionary<string, ColumnDefinition> RowDefinition
        {
            get { return _RowDefinition; }
        }

        private bool EndSpecification = false;
        private int[] _LineSpec;
        internal int[] LineSpec
        {
            get
            {
                if (EndSpecification == false)
                    throw new InvalidOperationException("LineSpec not calculated.  You must call EndSpec() before accessing LineSpec");
                return _LineSpec;
            }
        }

        private string _LineSpecFmt;
        internal string LineSpecFmt
        {
            get
            {
                if (EndSpecification == false)
                    throw new InvalidOperationException("LineSpecFmt not calculated.  You must call EndSpec() before accessing LineSpecFmt");
                return _LineSpecFmt;
            }
        }

        #endregion Properties

        #region Public Methods

        public TableSchema AddDefinition(string columnName, int width)
        {
            AddDefinition(columnName, new ColumnDefinition(CurrentColumnIndex++, width));
            return this;
        }

        public TableSchema AddDefinition(string columnName, ColumnDefinition field)
        {
            if (EndSpecification == true)
                throw new InvalidOperationException("Cannot add more columns after EndSpec()");
            RowDefinition.Add(columnName, field);
            ColumnNames.Add(columnName);
            return this;
        }

        public string GetColumnValue(string columnName, string[] fields)
        {
            ColumnDefinition ret;
            RowDefinition.TryGetValue(columnName, out ret);
            if (ret == null)
                return string.Empty;
            return fields[ret.Position].Trim();
        }

        public int GetColumnWidth(string columnName)
        {
            ColumnDefinition ret;
            RowDefinition.TryGetValue(columnName, out ret);
            if (ret == null)
                return 0;
            return ret.Width;
        }

        public string GetColumnName(int position)
        {
            return (from pair in RowDefinition
                    where pair.Value.Position == position
                    select pair.Key).FirstOrDefault();
        }

        public string GetValue(string columnName, object propertyValue)
        {
            //e.AddressMapKey.ToString().Truncate(this.AddressMapSchema.GetColumnWidth("AddressMapKey")),
            ColumnDefinition ret;
            RowDefinition.TryGetValue(columnName, out ret);
            if (ret == null)
                return string.Empty;
            return propertyValue.ToString()?.Substring(0, Math.Min(propertyValue.ToString().Length, this.GetColumnWidth(columnName))) ?? string.Empty;
        }

        Dictionary<Type, PropertyInfo[]> reflectionCache = new Dictionary<Type, PropertyInfo[]>();
        public string GetFixedWidthData(object entity)
        {
            if (EndSpecification == false)
                throw new InvalidOperationException("Must call EndSpec() first");
            if (entity == null)
            {
                return string.Empty;  //maybe return a line of spaces as long as the spec?
            }
            var str = new StringBuilder();

            Type entityType = entity.GetType();
            if (!reflectionCache.ContainsKey(entityType))
            {
                reflectionCache[entityType] = entityType.GetProperties().Where(p => ColumnNames.Any(c => c.Equals(p.Name))).ToArray();
                if (ColumnNames.Count() != reflectionCache[entityType].Count())
                    System.Diagnostics.Debug.WriteLine("WARNING: Not all properties defined in schema " + this._Name + " for type " + entityType.ToString());
            }
            PropertyInfo[] propertyInfo = reflectionCache[entityType];

            foreach (var colName in ColumnNames)
            {
                ColumnDefinition fieldInfo;
                RowDefinition.TryGetValue(colName, out fieldInfo); //will always exist, since we are getting column names from this.ColumnNames
                var fmt = "{0," + fieldInfo.Width + "}";
                var val = string.Empty;
                var colProp = propertyInfo.Where(p => p.Name == colName).FirstOrDefault();
                if (colProp != null)
                {
                    object propValue = colProp.GetValue(entity, null);
                    if (propValue != null)
                    {
                        //if (colProp.PropertyType == typeof(bool))
                        if (propValue is Boolean)
                        {
                            var boolValue = (bool)propValue;
                            if (boolValue)
                                val = "1";
                            else
                                val = "0";
                        }
                        else
                        {
                            val = propValue.ToString();
                        }
                    }
                }
                str.AppendFormat(fmt, val);
            }
            return str.ToString();
        }

        public TableSchema EndSpec()
        {
            EndSpecification = true;
            _LineSpec = GetLineSpec();
            _LineSpecFmt = GetLineSpecFmt();
            return this;
        }

        #endregion Public Methods

        #region Private Methods

        //protected string[] GetColumnNames()
        //{
        //    var ret = new string[Schema.Keys.Count];


        //    var r = Schema.Values.OrderBy(e => e.Position).ToArray();
        //    for (var i = 0; i < ret.Length; i++)
        //        ret[i] = r[i].;
        //    return ret;
        //}

        protected int[] GetLineSpec()
        {
            //new int[] { 19, 5, 22, 22, 5, 22, 22, 50, 50, 2, 5 }
            var ret = new int[RowDefinition.Keys.Count];
            var r = RowDefinition.Values.OrderBy(e => e.Position).ToArray();
            for (var i = 0; i < ret.Length; i++)
                ret[i] = r[i].Width;
            return ret;
        }

        protected string GetLineSpecFmt()
        {
            //var data = String.Format("{0,19}{1,5}{2,22}{3,22}{4,5}{5,22}{6,22}{7,50}{8,50}{9,2}{10,5}",
            var str = new StringBuilder();
            var colCount = RowDefinition.Keys.Count;
            var r = RowDefinition.Values.OrderBy(e => e.Position).ToArray();
            for (var i = 0; i < colCount; i++)
            {
                str.AppendFormat("{{{0},{1}}}", r[i].Position, r[i].Width);
            }
            return str.ToString();
        }

        #endregion Private Methods
    }
}
