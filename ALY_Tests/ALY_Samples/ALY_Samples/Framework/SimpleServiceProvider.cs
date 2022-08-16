#region Usings

using System;
using System.Collections.Generic;

#endregion

namespace Virtuoso.Core.Framework
{
    public class SimpleServiceProvider : IServiceProvider
    {
        private Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public object GetService(Type serviceType)
        {
            if (_services.ContainsKey(serviceType))
            {
                return _services[serviceType];
            }

            return null;
        }

        public void AddService<T>(T service)
        {
            _services[typeof(T)] = service;
        }
    }
}