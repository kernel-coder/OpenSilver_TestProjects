#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IVendorService : IModelDataService<Vendor>, ICleanup
    {
        PagedEntityCollectionView<Vendor> Vendors { get; }
    }
}