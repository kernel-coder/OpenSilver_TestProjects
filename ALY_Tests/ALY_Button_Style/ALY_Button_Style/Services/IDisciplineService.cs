#region Usings

using System;
using System.Linq;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IDisciplineService : IModelDataService<Discipline>, ICleanup
    {
        PagedEntityCollectionView<Discipline> Disciplines { get; }

        IQueryable<GetInsuranceAuthOrderTherapyView_Result> GetInsAuthOrder_CView(int ServiceTypeKey, string ComplianceTypeParm);

        IQueryable<InsuranceServiceLineView> GetInsuranceServiceLineView();
        void Remove(ServiceType entity);
        void UpdateInsuranceReqAuthOrder(GetInsuranceAuthOrderTherapyView_Result cviewParm);
        void UpdateInsuranceAuthOrderTherapy(GetInsuranceAuthOrderTherapyView_Result cviewParm);
        void UpdateInsuranceAuthOrderTherapy(InsuranceAuthOrderTherapy insAuth);
        void CancelInsuranceReqAuthOrderChanges();
        event EventHandler<EntityEventArgs<Discipline>> OnRefreshLoaded;
        void InsuranceAuthOrderTherapyEndEditting();
        void RefreshViewOnly();
        void SaveChanges();

        void GetInsuranceAuthOrderTherapyForServiceTypeAsync(int serviceTypeKey);

        event EventHandler<EntityEventArgs<AuthOrderTherapyPOCO_CView>> OnGetInsuranceAuthOrderTherapyForServiceTypeLoaded;
    }
}