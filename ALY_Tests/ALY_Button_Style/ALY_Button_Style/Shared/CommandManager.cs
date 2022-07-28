#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GalaSoft.MvvmLight.Command;

#endregion

namespace Virtuoso.Core.Utility
{
    public class CommandManager
    {
        List<PropertyInfo> RelayCommandList;
        readonly WeakReference InstanceReference;

        public CommandManager(object instance)
        {
            InstanceReference = new WeakReference(instance);

            //Initialize cache of RelayCommands
            BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance;
            RelayCommandList = (from p in InstanceReference.Target.GetType().GetProperties(bindingAttr)
                where p.PropertyType == typeof(RelayCommand) || (p.PropertyType.IsGenericType &&
                                                                 p.PropertyType.GetGenericTypeDefinition() ==
                                                                 typeof(RelayCommand<>))
                select p).ToList();
        }

        public void RaiseCanExecuteChanged()
        {
            if (RelayCommandList == null)
            {
                return;
            }

            RelayCommandList.ForEach(p =>
            {
                var method = p.PropertyType.GetMethod("RaiseCanExecuteChanged");
                if (method != null)
                {
                    var obj = p.GetValue(InstanceReference.Target, null);
                    if (obj != null)
                    {
                        method.Invoke(obj, null);
                    }
                }
            });
        }

        public void CleanUp()
        {
            if (RelayCommandList == null)
            {
                return;
            }

            RelayCommandList.ForEach(p => { p = null; });
            RelayCommandList.Clear();
            RelayCommandList = null;
            InstanceReference.Target = null;
        }
    }
}