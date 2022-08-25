namespace Virtuoso.Client.Core
{
    using Autofac;
    using Autofac.Features.Metadata;
    using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
    using Microsoft.Practices.EnterpriseLibrary.Logging;
    using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Reflection;

    internal class VirtuosoServiceLocator : IServiceLocator//, IDisposable
    {
        static readonly LogWriter logWriter;
        static VirtuosoServiceLocator()
        {
            logWriter = EnterpriseLibraryContainer.Current.GetInstance<LogWriter>();
        }

        private Autofac.IContainer _container;
        private ContainerBuilder _containerBuilder;

        public void Initialize(ICollection<Assembly> assembliesToScan)
        {
            _containerBuilder = new Autofac.ContainerBuilder();

            // Wire up anybody with Export
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .Union(assembliesToScan);

            foreach(var assembly in allAssemblies)
            {
                var allTypes = assembly.GetExportedTypes();
                foreach(var type in allTypes)
                {
                    RegisterType(_containerBuilder, type);
                }
            }
        }

        //public IEnumerable<Meta<Lazy<T>>> GetExports<T>()
        //public IEnumerable<Lazy<T>> GetExports<T>()
        public IEnumerable<Meta<System.Lazy<T>>> GetExports<T>()
        {
            EnsureContainerBuilt();
            //var exports = _container.Resolve<IEnumerable<Meta<Lazy<T>>>>().ToList();
            //var exports = _container.Resolve<IEnumerable<Lazy<T>>>().ToList();
            var exports = _container.Resolve<IEnumerable<Meta<System.Lazy<T>>>>().ToList();

            return exports; //.Where(t => t.Metadata.["CacheName"].Equals(cache_name));
        }

        public T GetInstance<T>()
        {
            EnsureContainerBuilt();
            return _container.Resolve<T>();
        }

        public void ComposeExportedValue<T>(T exportedValue) where T : class
        {
            if(_container != null)
            {
                throw new Exception("Container has already been built. If resolvers must be registered after startup, consider rebuilding the container.");
            }

            _containerBuilder.RegisterInstance<T>(exportedValue);
        }
        public System.Lazy<T> GetExport<T>()
        {
            EnsureContainerBuilt();
            return _container.Resolve<System.Lazy<T>>();
        }

        public void ReleaseExport<T>(System.Lazy<T> export)
        {
            /*
             * OPENSILVER TODO
             * See MEF behavior here:
             * https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.composition.hosting.compositioncontainer.releaseexport?view=dotnet-plat-ext-6.0
             * This may not be needed in OS.  Need to ask if properties on a view model are cleaned up/disposed.  Doesn't seem to be so far, but
             * also don't have a view to test this against yet.
             * Otherwise possibly nothing to do here, the caller code could just call .Dispose() if the export is not a singleton
             */
        }

        private void EnsureContainerBuilt()
        {
            if(_container != null)
            {
                return;
            }

            _container = _containerBuilder.Build();
        }

        private static void RegisterType(ContainerBuilder containerBuilder, Type type)
        {
            var exportAttribute = type.GetCustomAttribute<ExportAttribute>();
            if(exportAttribute == null)
            {
                return;
            }

            var builder = containerBuilder.RegisterType(type)
                .As(exportAttribute.ContractType ?? type);

            IEnumerable<ExportMetadataAttribute> metadataAttributes = type.GetCustomAttributes<ExportMetadataAttribute>();
            foreach(var metadata in metadataAttributes)
            {
                builder.WithMetadata(metadata.Name, metadata.Value);
            }

            var partCreationPolicyAttribute = type.GetCustomAttribute<PartCreationPolicyAttribute>();
            if(partCreationPolicyAttribute != null
               && partCreationPolicyAttribute.CreationPolicy == CreationPolicy.NonShared)
            {
                builder.InstancePerDependency();
            }
            else
            {
                builder.SingleInstance();
            }

            var constructorToUse = type.GetConstructors().Where(a => a.GetCustomAttribute<ImportingConstructorAttribute>() != null).FirstOrDefault();
            if(constructorToUse != null)
            {
                builder.UsingConstructor(constructorToUse.GetParameters().Select(s => s.ParameterType).ToArray());
            }
        }
    }
}
