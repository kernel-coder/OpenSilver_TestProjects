namespace Virtuoso.Client.Core
{
    using Autofac.Features.Metadata;
    using System;
    using System.Collections.Generic;

    public interface IServiceLocator
    {
        T GetInstance<T>();
        IEnumerable<Meta<System.Lazy<T>>> GetExports<T>();
        System.Lazy<T> GetExport<T>();
        void ReleaseExport<T>(System.Lazy<T> export);
        void ComposeExportedValue<T>(T exportedValue) where T : class;
    }

}
