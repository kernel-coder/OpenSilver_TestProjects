#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IProcessingRulesService : IModelDataService<RuleHeader>, ICleanup
    {
        PagedEntityCollectionView<RuleHeader> RuleHeaders { get; }
    }
}