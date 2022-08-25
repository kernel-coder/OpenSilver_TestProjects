#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using Virtuoso.Client.Core.Storage;
using Virtuoso.Core.Interface;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Occasional
{
    //Multithreaded .NET Singleton - http://msdn.microsoft.com/en-us/library/ff650316.aspx
    public sealed class OfflineIDGenerator : IOfflineIDGeneratorMemento
    {
        private static volatile OfflineIDGenerator instance;
        private static readonly object syncRoot = new Object();
        private Int32 __seed;

        private OfflineIDGenerator()
        {
        }

        public static OfflineIDGenerator Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new OfflineIDGenerator();
                        }
                    }
                }

                return instance;
            }
        }

        private int Identity => Interlocked.Decrement(ref __seed);

        public void SetKey(VirtuosoEntity entity)
        {
            lock (syncRoot)
            {
                List<PropertyInfo> props = GetEntityKey(entity.GetType());
                if ((props != null) && props.Any())
                {
                    var prop = props.First();

                    if (prop.PropertyType == typeof(Int32))
                    {
                        var _id = Identity;
                        prop.SetValue(entity, _id, null);
                    }
                    else if (prop.PropertyType == typeof(Guid))
                    {
                        prop.SetValue(entity, GuidCombGenerator.GenerateComb(), null);
                    }
                }
            }
        }

        public IDGeneratorMemento GetMemento()
        {
            return new IDGeneratorMemento { Seed = __seed };
        }

        public void SetMemento(IDGeneratorMemento memento)
        {
            __seed = memento.Seed;
        }

        //This caches the reflected (Key) information about the entities for performance
        private static readonly Dictionary<Type, List<PropertyInfo>> reflectionKeyCache = new Dictionary<Type, List<PropertyInfo>>();

        //This gets the KEY DataMembers
        private List<PropertyInfo> GetEntityKey(Type entity)
        {
            if (!reflectionKeyCache.ContainsKey(entity))
            {
                BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance;

                var qry = from p in entity.GetProperties(bindingAttr)
                    where p.GetCustomAttributes(typeof(KeyAttribute), true).Length > 0
                          && p.GetSetMethod() != null
                    select p;
                reflectionKeyCache.Add(entity, qry.ToList());
            }

            return reflectionKeyCache[entity];
        }

        //public helper method
        public static void Save()
        {
            PersistentOfflineIDGeneratorCareTaker careTaker = new PersistentOfflineIDGeneratorCareTaker(
                new FileStore(PersistentOfflineIDGeneratorCareTaker.GetFileName()))
            {
                Memento = Instance.GetMemento()
            };
            careTaker.Save();
        }

        public static void Load()
        {
            try
            {
                PersistentOfflineIDGeneratorCareTaker careTaker = new PersistentOfflineIDGeneratorCareTaker(
                    new FileStore(PersistentOfflineIDGeneratorCareTaker.GetFileName()));
                careTaker.Load();
                Instance.SetMemento(careTaker.Memento);
            }
            catch (Exception)
            {
                Instance.SetMemento(new IDGeneratorMemento { Seed = 0 });
            }
        }
    }
}