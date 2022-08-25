#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IICDCodeService : IModelDataService<ICDCode>, ICleanup
    {
        PagedEntityCollectionView<ICDCode> ICDCodes { get; }
    }
}