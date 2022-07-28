#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IBereavementActivityService : IModelDataService<BereavementActivity>, ICleanup
    {
        PagedEntityCollectionView<BereavementActivity> BereavementActivities { get; }
    }
}