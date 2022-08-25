#region Usings

using System;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IServiceLineService : IModelDataService<ServiceLine>, ICleanup
    {
        PagedEntityCollectionView<ServiceLine> ServiceLines { get; }
        void Remove(ServiceLineGrouping entity);
        void Remove(CensusTractMapping entity);
        void Remove(ServiceLineGroupHeader entity);
        void Remove(TeamMeetingSchedule entity);
        void Remove(ServiceLineGroupingParent entity);
        void Remove(ServiceTypeGrouping entity);
        void Add(CensusTractMapping entity);

        void Add(PhysicianGrouping entity);
        void Remove(PhysicianGrouping entity);

        event EventHandler<EntityEventArgs<CensusTractMapping>> CensusTractMapping_OnLoaded;
        void GetCensusMappingAsync(int? serviceLineGroupingKey);

        event EventHandler<EntityEventArgs<CensusTract>> CensusTract_OnLoaded;
        void GetCensusTractAsync(int? CensusTractKey);

        System.Threading.Tasks.Task<bool> ValidateServiceLineAsync(ServiceLine serviceLine);
    }
}