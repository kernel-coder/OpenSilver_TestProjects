#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IAllergyCodeService : IModelDataService<AllergyCode>, ICleanup
    {
        PagedEntityCollectionView<AllergyCode> AllergyCodes { get; }
    }
}