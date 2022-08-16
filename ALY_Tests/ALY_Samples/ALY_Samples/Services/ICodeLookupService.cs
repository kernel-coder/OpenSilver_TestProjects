#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface ICodeLookupService : IModelDataService<CodeLookupHeader>, ICleanup
    {
        PagedEntityCollectionView<CodeLookupHeader> CodeLookupHeaders { get; }
        void Remove(CodeLookup entity);
    }
}