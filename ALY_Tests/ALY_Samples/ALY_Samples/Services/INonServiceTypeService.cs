#region Usings

using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface INonServiceTypeService : IModelDataService<NonServiceType>
    {
        PagedEntityCollectionView<NonServiceType> NonServiceTypes { get; }
    }
}