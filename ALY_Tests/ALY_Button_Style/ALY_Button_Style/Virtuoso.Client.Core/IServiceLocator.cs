#region Usings

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;

#endregion

namespace Virtuoso.Client.Core
{
    public interface IServiceLocator
    {
        T GetInstance<T>();
        IEnumerable<System.Lazy<T, TMetadata>> GetExports<T, TMetadata>();
        System.Lazy<T> GetExport<T>();
        void ReleaseExport<T>(System.Lazy<T> export);
        void ComposeExportedValue<T>(T exportedValue) where T : class;
    }
}
