namespace Virtuoso.Client.Core
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Reflection;

    internal class VirtuosoServiceLocator : IServiceLocator
    {
        private CompositionContainer _container;
        private AggregateCatalog _catalog;

        public void Initialize(ICollection<Assembly> assembliesToScan)
        {
            _catalog = new AggregateCatalog();
            foreach (var assemblyToScan in assembliesToScan)
            {
                _catalog.Catalogs.Add(new AssemblyCatalog(assemblyToScan));     
            }
            _container = CompositionHost.Initialize(_catalog);
            _container.ComposeExportedValue(_container);
        }

        public IEnumerable<System.Lazy<T, TMetadata>> GetExports<T, TMetadata>()
        {
            return _container.GetExports<T, TMetadata>();
        }

        public T GetInstance<T>()
        {
            return _container.GetExportedValue<T>();
        }

        public void ComposeExportedValue<T>(T exportedValue) where T: class
        {
            _container.ComposeExportedValue(exportedValue);
        }

        public System.Lazy<T> GetExport<T>()
        {
            return _container.GetExport<T>();
        }

        public void ReleaseExport<T>(System.Lazy<T> export)
        {
            _container.ReleaseExport(export);
        }
    }
}
