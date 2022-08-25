#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IEmployerService : IModelDataService<Employer>, ICleanup
    {
        PagedEntityCollectionView<Employer> Employers { get; }
        void Remove(EmployerContact entity);
        void Remove(EmployerPhone entity);
    }
}