#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using OpenRiaServices.DomainServices.Client;
using RiaServicesContrib;
using RiaServicesContrib.Extensions;

#endregion

namespace Virtuoso.Core.Utility
{
    public class DomainContextChangeHelper
    {
        public static string EntityChangeSetInformation(string contextName, EntityChangeSet entityChangeSet)
        {
            return string.Format("{0}\tChangeSet: {1}\n", contextName, entityChangeSet.ToString());
        }

        public static string EntityChangeSetInformation(Tuple<string, EntityChangeSet>[] entityChangeSets)
        {
            var _msg = string.Empty;
            entityChangeSets.ToList().ForEach(cs => _msg += EntityChangeSetInformation(cs.Item1, cs.Item2));
            return _msg;
        }

        public static string EntityChangeInformation(string contextName, Entity _Entity, string tag)
        {
            var _propInfoKey = GetEntityKey(_Entity).FirstOrDefault();
            var _OriginalEntity = _Entity.GetOriginal();
            var _PropertyMsg = "";
            var propInfoList = GetDataMembers(_Entity);

            var changesOnlyState = _Entity.ExtractState(ExtractType.ChangesOnlyState);
            foreach (var key in changesOnlyState.Keys)
                _PropertyMsg += string.Format("CHANGE - KEY: {0}:\tVALUE: {1}\n", key, changesOnlyState[key]);

            foreach (var propInfo in propInfoList)
            {
                object originalPropValue = (_OriginalEntity == null) ? null : propInfo.GetValue(_OriginalEntity, null);
                object currentPropValue = propInfo.GetValue(_Entity, null);
                var objPropEqual = false;
                if (originalPropValue == null && currentPropValue == null)
                {
                    objPropEqual = true;
                }
                else if (originalPropValue == null && currentPropValue != null)
                {
                    objPropEqual = false;
                }
                else if (originalPropValue != null && currentPropValue == null)
                {
                    objPropEqual = false;
                }
                else if (!(currentPropValue.ToString() == originalPropValue.ToString()))
                {
                    objPropEqual = false;
                }
                else
                {
                    objPropEqual = true;
                }

                if (objPropEqual == false)
                {
                    if (_OriginalEntity == null)
                    {
                        _PropertyMsg += string.Format("{0}:\n\tCurrent: {1}\n", propInfo.Name, currentPropValue);
                    }
                    else
                    {
                        _PropertyMsg += string.Format("{0}:\n\tCurrent: {1}\n\tOriginal: {2}\n", propInfo.Name,
                            currentPropValue, originalPropValue);
                    }
                }
            }

            return string.Format("{0}-{1}\n{2}(Key={3}):\n{4}",
                contextName,
                tag,
                _Entity.GetType().ToString(),
                (_propInfoKey != null) ? _propInfoKey.GetValue(_Entity, null) : "KEY UNKNOWN",
                _PropertyMsg);
        }

        //This caches the reflected (DataMember) information about the entities for performance
        private static Dictionary<Type, List<PropertyInfo>> reflectionCache = new Dictionary<Type, List<PropertyInfo>>();

        //This gets the DataMembers
        private static List<PropertyInfo> GetDataMembers(Entity entity)
        {
            if (!reflectionCache.ContainsKey(entity.GetType()))
            {
                BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance;

                var qry = from p in entity.GetType().GetProperties(bindingAttr)
                    where p.GetCustomAttributes(typeof(DataMemberAttribute), true).Length > 0
                          && p.GetSetMethod() != null
                    select p;
                reflectionCache.Add(entity.GetType(), qry.ToList());
            }

            return reflectionCache[entity.GetType()];
        }

        //This caches the reflected (Key) information about the entities for performance
        private static Dictionary<Type, List<PropertyInfo>> reflectionKeyCache = new Dictionary<Type, List<PropertyInfo>>();

        //This gets the KEY DataMembers
        private static List<PropertyInfo> GetEntityKey(Entity entity)
        {
            if (!reflectionKeyCache.ContainsKey(entity.GetType()))
            {
                BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance;

                var qry = from p in entity.GetType().GetProperties(bindingAttr)
                    where p.GetCustomAttributes(typeof(KeyAttribute), true).Length > 0
                          && p.GetSetMethod() != null
                    select p;
                reflectionKeyCache.Add(entity.GetType(), qry.ToList());
            }

            return reflectionKeyCache[entity.GetType()];
        }
    }
}