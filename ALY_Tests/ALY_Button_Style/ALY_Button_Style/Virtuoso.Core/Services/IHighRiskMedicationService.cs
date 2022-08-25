#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IHighRiskMedicationService : IModelDataService<HighRiskMedication>, ICleanup
    {
        PagedEntityCollectionView<HighRiskMedication> HighRiskMedications { get; }
    }
}