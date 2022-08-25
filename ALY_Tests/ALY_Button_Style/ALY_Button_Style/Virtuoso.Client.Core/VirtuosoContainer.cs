#region Usings

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;

#endregion

namespace Virtuoso.Client.Core
{
    public static class VirtuosoContainer
    {
        private static volatile IServiceLocator currentContainer;
        private static readonly object currentContainerLock = new object();

        public static IServiceLocator CreateDefaultContainer(ICollection<Assembly> assembliesToScan = null)
        {
            var container = new VirtuosoServiceLocator();
            container.Initialize(assembliesToScan);
            return container;
        }

        private static void SetCurrentContainerIfNotSet()
        {
            if (currentContainer == null)
            {
                lock (currentContainerLock)
                {
                    if (currentContainer == null)
                    {
                        currentContainer = CreateDefaultContainer(null);
                    }
                }
            }
        }
        
        /// <summary>
        /// Initialize with assemblies to parse.  Will fail if the container is already set
        /// </summary>
        /// <param name="assembliesToLoad"></param>
        public static void Initialize(ICollection<Assembly> assembliesToLoad)
        {
            lock(currentContainerLock)
            {
                if(currentContainer != null)
                {
                   throw new System.Exception("The container has already been initialized");
                }

                currentContainer = CreateDefaultContainer(assembliesToLoad);
            }
        }

        public static IServiceLocator Current
        {
            get
            {
                SetCurrentContainerIfNotSet();
                return currentContainer;
            }
        }
    }
}