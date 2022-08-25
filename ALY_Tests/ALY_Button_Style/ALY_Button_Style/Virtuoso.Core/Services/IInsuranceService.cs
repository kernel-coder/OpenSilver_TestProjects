#region Usings

using System;
using GalaSoft.MvvmLight;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IInsuranceService : IModelDataService<Insurance>, ICleanup
    {
        EntitySet<InsuranceGroup> InsuranceGroups { get; }
        PagedEntityCollectionView<Insurance> Insurances { get; }
        EntitySet<InsuranceParameterJoin> InsuranceParameters { get; }
        void GetInsuranceParametersForMaintAsync(int InsuranceKey);
        void GetInsuranceGroupByKeyAsync(int InsuranceGroupKey);
        void GetInsuranceGroupByNameAsync(string InsuranceGroupName, bool inactive);
        event EventHandler<EntityEventArgs<InsuranceParameterJoin>> OnParmDefsLoaded;
        void Add(InsuranceGroup entity);
        void Add(InsuranceGroupDetail entity);
        void Remove(InsuranceGroup entity);
        void Remove(InsuranceAddress entity);
        void Remove(InsuranceContact entity);
        void Remove(InsuranceCertDefinition entity);
        void Remove(InsuranceCertStatement entity);
        void Remove(InsuranceRecertStatement entity);
        void Remove(InsuranceGroupDetail entity);
        event EventHandler OnCertRefreshed;
        event EventHandler<EntityEventArgs<InsuranceGroup>> OnInsuranceGroupsLoaded;
        System.Threading.Tasks.Task<int> RefreshCertPeriodsForInsuranceAsync(int insuranceKey);

        void GetEVVImplementationAsync();
        event EventHandler<EntityEventArgs<EVVImplementationPOCO>> OnEVVImplementationLoaded;
    }
}