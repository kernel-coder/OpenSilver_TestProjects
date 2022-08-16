#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IDisciplineService))]
    public class DisciplineService : PagedModelBase, IDisciplineService
    {
        public VirtuosoDomainContext Context { get; set; }
        public VirtuosoDomainContext ViewOnlyContext { get; set; }
        private VirtuosoDomainContext ViewOnlyContextAuth { get; set; }

        public DisciplineService()
        {
            Context = new VirtuosoDomainContext();
            ViewOnlyContext = new VirtuosoDomainContext();
            Disciplines = new PagedEntityCollectionView<Discipline>(Context.Disciplines, this);
            Context.PropertyChanged += Context_PropertyChanged;
        }

        #region PagedModelBase Members

        public override void LoadData()
        {
            if (IsLoading || Context == null)
            {
                return;
            }

            IsLoading = true;

            GetAsync();
        }

        #endregion

        #region IModelDataService<Discipline> Members

        public void Add(Discipline entity)
        {
            Context.Disciplines.Add(entity);
        }

        public void Remove(Discipline entity)
        {
            Context.Disciplines.Remove(entity);
        }

        public void Remove(ServiceType entity)
        {
            Context.ServiceTypes.Remove(entity);
        }

        public void UpdateInsuranceReqAuthOrder(GetInsuranceAuthOrderTherapyView_Result cviewParm)
        {
            if (cviewParm == null)
            {
                return;
            }

            var rowToUpdate = Context.InsuranceAuthOrderTherapies
                .Where(ins => ins.InsuranceReqKey == cviewParm.InsuranceReqKey).FirstOrDefault();
            if (rowToUpdate != null)
            {
                rowToUpdate.BeginEditting();
                rowToUpdate.Inactive = cviewParm == null ? true : !(bool)(cviewParm.TypeIsRequired);
            }
            else
            {
                rowToUpdate = new InsuranceAuthOrderTherapy();
                rowToUpdate.ComplianceType = cviewParm.ComplianceType;
                rowToUpdate.DisciplineKey = cviewParm.DisciplineKey;
                rowToUpdate.Inactive = cviewParm == null ? true : (bool)!cviewParm.TypeIsRequired;
                rowToUpdate.InsuranceKey = cviewParm.InsuranceKey;
                if (cviewParm.InsuranceReqKey != null)
                {
                    rowToUpdate.InsuranceReqKey = (int)cviewParm.InsuranceReqKey;
                }

                if (cviewParm.ServiceLineKey > 0)
                {
                    rowToUpdate.ServiceLineKey = cviewParm.ServiceLineKey;
                }

                rowToUpdate.ServiceTypeKey = cviewParm.ServiceTypeKey;
                var svcType = Context.ServiceTypes.Where(st => st.ServiceTypeKey == cviewParm.ServiceTypeKey)
                    .FirstOrDefault();
                if (svcType != null)
                {
                    svcType.InsuranceAuthOrderTherapy.Add(rowToUpdate);
                }

                rowToUpdate.TenantID = cviewParm.TenantID;
                Context.InsuranceAuthOrderTherapies.Add(rowToUpdate);
            }

            cviewParm.TypeIsRequiredOriginal = cviewParm.TypeIsRequired;
        }

        public void InsuranceAuthOrderTherapyEndEditting()
        {
            foreach (var ins in Context.InsuranceAuthOrderTherapies) ins.EndEditting();
        }

        public void UpdateInsuranceAuthOrderTherapy(GetInsuranceAuthOrderTherapyView_Result cviewParm)
        {
            if (cviewParm == null)
            {
                return;
            }

            var rowToUpdate = Context.InsuranceAuthOrderTherapies
                .Where(ins => ins.InsuranceReqKey == cviewParm.InsuranceReqKey).FirstOrDefault();
            if (rowToUpdate != null)
            {
                rowToUpdate.BeginEditting();
                rowToUpdate.Inactive = cviewParm == null ? true : !(bool)(cviewParm.TypeIsRequired);
                if (rowToUpdate.Inactive)
                {
                    rowToUpdate.InactiveDate = DateTime.UtcNow;
                }
            }
            else
            {
                rowToUpdate = new InsuranceAuthOrderTherapy();
                rowToUpdate.ComplianceType = cviewParm.ComplianceType;
                rowToUpdate.DisciplineKey = cviewParm.DisciplineKey;
                rowToUpdate.Inactive = cviewParm == null ? true : (bool)!cviewParm.TypeIsRequired;
                rowToUpdate.InsuranceKey = cviewParm.InsuranceKey;
                if (cviewParm.InsuranceReqKey > 0)
                {
                    rowToUpdate.InsuranceReqKey = (int)cviewParm.InsuranceReqKey;
                }

                if (cviewParm.ServiceLineKey > 0)
                {
                    rowToUpdate.ServiceLineKey = cviewParm.ServiceLineKey;
                }

                rowToUpdate.ServiceTypeKey = cviewParm.ServiceTypeKey;
                var svcType = Context.ServiceTypes.Where(st => st.ServiceTypeKey == cviewParm.ServiceTypeKey)
                    .FirstOrDefault();
                if (svcType != null)
                {
                    svcType.InsuranceAuthOrderTherapy.Add(rowToUpdate);
                }

                rowToUpdate.TenantID = cviewParm.TenantID;
                Context.InsuranceAuthOrderTherapies.Add(rowToUpdate);
            }

            cviewParm.TypeIsRequiredOriginal = cviewParm.TypeIsRequired;
        }

        public void UpdateInsuranceAuthOrderTherapy(InsuranceAuthOrderTherapy insAuth)
        {
            if (insAuth == null)
            {
                return;
            }

            var rowToUpdate = Context.InsuranceAuthOrderTherapies
                .Where(ins => ins.InsuranceReqKey == insAuth.InsuranceReqKey).FirstOrDefault();

            if (rowToUpdate != null)
            {
                rowToUpdate.BeginEditting();
                rowToUpdate.Inactive = insAuth == null ? true : !insAuth.Inactive;
                if (rowToUpdate.Inactive)
                {
                    rowToUpdate.InactiveDate = DateTime.UtcNow;
                }
            }
            else
            {
                rowToUpdate = new InsuranceAuthOrderTherapy();
                rowToUpdate.ComplianceType = insAuth.ComplianceType;
                rowToUpdate.DisciplineKey = insAuth.DisciplineKey;
                rowToUpdate.Inactive = insAuth == null ? true : !insAuth.Inactive;
                rowToUpdate.InsuranceKey = insAuth.InsuranceKey;
                if (insAuth.InsuranceReqKey > 0)
                {
                    rowToUpdate.InsuranceReqKey = insAuth.InsuranceReqKey;
                }

                if (insAuth.ServiceLineKey > 0)
                {
                    rowToUpdate.ServiceLineKey = insAuth.ServiceLineKey;
                }

                rowToUpdate.ServiceTypeKey = insAuth.ServiceTypeKey;
                var svcType = Context.ServiceTypes.Where(st => st.ServiceTypeKey == insAuth.ServiceTypeKey)
                    .FirstOrDefault();
                if (svcType != null)
                {
                    svcType.InsuranceAuthOrderTherapy.Add(rowToUpdate);
                }

                rowToUpdate.TenantID = insAuth.TenantID;
                Context.InsuranceAuthOrderTherapies.Add(rowToUpdate);
            }
        }


        public void CancelInsuranceReqAuthOrderChanges()
        {
            if (Context != null)
            {
                foreach (var ins in Context.InsuranceAuthOrderTherapies) ins.CancelEditting();
                if (Context.InsuranceAuthOrderTherapies != null)
                {
                    var lst = Context.InsuranceAuthOrderTherapies.ToList();
                    lst.Where(ins => ins.IsNew)
                        .ForEach(d => Context.InsuranceAuthOrderTherapies.Remove(d));
                }
            }

            if (ViewOnlyContext != null)
            {
                ViewOnlyContext.RejectChanges();
            }
            //if (cviewParm == null) return;
            //var rowToUpdate = this.Context.InsuranceAuthOrderTherapies.Where(ins => ins.InsuranceReqKey == cviewParm.InsuranceReqKey).FirstOrDefault();
            //if (rowToUpdate != null)
            //{
            //    rowToUpdate.Inactive = cviewParm == null ? true : (cviewParm.Required == 1 ? true : false);
            //    if (rowToUpdate.IsNew) this.Context.InsuranceAuthOrderTherapies.Remove(rowToUpdate);
            //    cviewParm.TypeIsRequired = !cviewParm.Inactive;
            //    cviewParm.TypeIsRequiredOriginal = cviewParm.TypeIsRequired;
            //}
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
            ViewOnlyContext.EntityContainer.Clear();
        }

        public void GetSearchAsync(bool isSystemSearch)
        {
            //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
            //          when isSystemSearch == false, then Inactive checkbox removed from search criteria; however 
            //          we want to always assume that it is checked - e.g. add Inactive==false to query.

            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.Disciplines.Clear();
                bool _Inactive = false;
                var query = Context.GetDisciplineForSearchQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "DisciplineKey":
                                query = query.Where(p => p.DisciplineKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Code":
                                query = query.Where(p => p.Code.ToLower() == searchvalue.ToLower());
                                break;
                            case "Description":
                                query = query.Where(i => i.Description.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Inactive":
                                _Inactive = Convert.ToBoolean(item.Value);
                                break;
                        }
                    }

                    if (isSystemSearch == false)
                    {
                        //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
                        query = query.Where(p => p.Inactive == false);
                    }
                }

                if (_Inactive == false)
                {
                    query = query.Where(p => p.Inactive == false);
                }

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public void GetAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.Disciplines.Clear();
                //ViewOnlyContext.InsuranceAuthOrderTherapy_CViews.Clear();
                ViewOnlyContext.InsuranceServiceLineViews.Clear();

                var query = Context.GetDisciplineQuery();
                int? discKey = null;

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "DisciplineKey":
                                discKey = Convert.ToInt32(searchvalue);
                                query = query.Where(p => p.DisciplineKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Code":
                                query = query.Where(p => p.Code.ToLower() == searchvalue.ToLower());
                                break;
                            case "Description":
                                query = query.Where(i => i.Description.ToLower().Contains(searchvalue.ToLower()));
                                break;
                        }
                    }
                }

                query.IncludeTotalCount = true;

                if (PageSize > 0)
                {
                    query = query.Skip(PageSize * PageIndex);
                    query = query.Take(PageSize);
                }

                IsLoading = true;
                var insAuthQuery = Context.GetInsuranceAuthOrderTherapyQuery();

                DomainContextLoadBatch batch = new DomainContextLoadBatch(DataLoadComplete);
                batch.Add(Context.Load(query, LoadBehavior.RefreshCurrent, false));
                batch.Add(Context.Load(insAuthQuery, LoadBehavior.RefreshCurrent, false));

                // Lazy load AuthOrderTherapyPOCO_CView list when View/Edit is pressed for a given ServiceType
                //if (discKey.HasValue)
                //{
                //    var Insquery = ViewOnlyContext.GetInsuranceAuthOrderTherapyPOCO_CViewQuery(discKey);
                //    batch.Add(ViewOnlyContext.Load<AuthOrderTherapyPOCO_CView>(Insquery, LoadBehavior.RefreshCurrent, false));
                //}
                var insQ = ViewOnlyContext.GetInsuranceServiceLineViewQuery(discKey);
                ViewOnlyContext.Load(insQ, LoadBehavior.RefreshCurrent, g => HandleEntityResults(g, ViewLoaded), null);


                //Context.Load<Discipline>(
                //    query,
                //    LoadBehavior.RefreshCurrent,
                //    g => HandleEntityResults<Discipline>(g, this.OnLoaded),
                //    null);
            });
        }

        public IEnumerable<InsuranceServiceLineView> InsuranceView { get; set; }

        private void ViewLoaded(object sender, EntityEventArgs<InsuranceServiceLineView> e)
        {
            InsuranceView = e.Results;


            RaisePropertyChanged("InsButttonEnabled");
        }

        public IEnumerable<Discipline> Items => Context.Disciplines;

        PagedEntityCollectionView<Discipline> _Disciplines;

        public PagedEntityCollectionView<Discipline> Disciplines
        {
            get { return _Disciplines; }
            set
            {
                if (_Disciplines != value)
                {
                    _Disciplines = value;
                    this.RaisePropertyChanged(p => p.Disciplines);
                }
            }
        }


        public IQueryable<GetInsuranceAuthOrderTherapyView_Result> GetInsAuthOrder_CView(int ServiceTypeKey,
            string ComplianceTypeParm)
        {
            if ((ViewOnlyContextAuth == null) || (ViewOnlyContextAuth.AuthOrderTherapyPOCO_CViews == null))
            {
                return null;
            }

            var lst = ViewOnlyContextAuth.AuthOrderTherapyPOCO_CViews.Where(p => p.ComplianceType == ComplianceTypeParm)
                .FirstOrDefault();
            if ((lst == null) || (lst.RequireAuthOrderTherapyList == null))
            {
                return null;
            }

            IQueryable<GetInsuranceAuthOrderTherapyView_Result> iList = lst.RequireAuthOrderTherapyList
                .Where(ins => ins.ServiceTypeKey == ServiceTypeKey).AsQueryable();
            return iList;
        }

        public IQueryable<InsuranceServiceLineView> GetInsuranceServiceLineView()
        {
            var InsList = InsuranceView;

            return InsList.Distinct(new InsAuthOrder_CViewAll_Comparer()).AsQueryable();
        }

        public void RefreshViewOnly()
        {
            if (ViewOnlyContext != null)
            {
                //var fRow = ViewOnlyContext.InsuranceAuthOrderTherapy_CViews.FirstOrDefault();
                var fRow = Items.FirstOrDefault();
                if (fRow != null)
                {
                    //ViewOnlyContext.InsuranceAuthOrderTherapy_CViews.Clear();
                    DomainContextLoadBatch batch = new DomainContextLoadBatch(DataLoadCompleteRefresh);

                    var insAuthQuery = Context.GetInsuranceAuthOrderTherapyQuery();
                    batch.Add(Context.Load(insAuthQuery, LoadBehavior.RefreshCurrent, false));

                    // Lazy load AuthOrderTherapyPOCO_CView list when View/Edit is pressed for a given ServiceType
                    //var Insquery = ViewOnlyContext.GetInsuranceAuthOrderTherapyPOCO_CViewQuery(this.Items.FirstOrDefault().DisciplineKey);
                    //batch.Add(ViewOnlyContext.Load<AuthOrderTherapyPOCO_CView>(Insquery, LoadBehavior.RefreshCurrent, false));

                    var insQ = ViewOnlyContext.GetInsuranceServiceLineViewQuery(Items.FirstOrDefault().DisciplineKey);
                    ViewOnlyContext.Load(insQ, LoadBehavior.RefreshCurrent, g => HandleEntityResults(g, ViewLoaded),
                        null);
                }
            }
        }

        public void GetInsuranceAuthOrderTherapyForServiceTypeAsync(int serviceTypeKey)
        {
            if (ViewOnlyContextAuth != null)
            {
                ViewOnlyContextAuth.EntityContainer.Clear();
                ViewOnlyContextAuth = null;
            }

            ViewOnlyContextAuth = new VirtuosoDomainContext();
            IsLoading = true;
            var myQuery = ViewOnlyContextAuth.GetInsuranceAuthOrderTherapyForServiceTypePOCO_CViewQuery(serviceTypeKey);
            ViewOnlyContextAuth.Load(
                myQuery,
                LoadBehavior.MergeIntoCurrent,
                InsuranceAuthOrderTherapyForServiceTypeLoaded,
                null);
        }

        private void InsuranceAuthOrderTherapyForServiceTypeLoaded(LoadOperation<AuthOrderTherapyPOCO_CView> results)
        {
            if (results.Error != null)
            {
                MessageBox.Show(results.Error.ToString());
            }

            HandleEntityResults(results, OnGetInsuranceAuthOrderTherapyForServiceTypeLoaded);
            IsLoading = false;
        }

        public event EventHandler<EntityEventArgs<AuthOrderTherapyPOCO_CView>>
            OnGetInsuranceAuthOrderTherapyForServiceTypeLoaded;

        public event EventHandler<EntityEventArgs<Discipline>> OnLoaded;
        public event EventHandler<EntityEventArgs<Discipline>> OnRefreshLoaded;

        private void DataLoadComplete(DomainContextLoadBatch batch)
        {
            List<Exception> LoadErrors = new List<Exception>();

            if (batch.FailedOperationCount > 0)
            {
                foreach (var fop in batch.FailedOperations)
                    if (fop.HasError)
                    {
                        LoadErrors.Add(fop.Error);
                    }
            }

            IsLoading = true;
            if (OnLoaded != null)
            {
                EntityEventArgs<Discipline> e = new EntityEventArgs<Discipline>(null, DataLoadType.SERVER);
                LoadErrors.ForEach(er => e.EntityErrors.Add(er.Message));
                OnLoaded(this, e);
            }
            //if (this.OnLoaded != null)
            //{
            //    Dispatcher.BeginInvoke(() =>
            //    {
            //        Forms = FormContext.Forms;
            //        Sections = FormContext.Sections;
            //        RiskAssessments = FormContext.RiskAssessments;
            //        Questions = FormContext.Questions;
            //        QuestionGroups = FormContext.QuestionGroups;

            //        IsLoading = false;

            //        OnMultiLoaded(this, new MultiErrorEventArgs(LoadErrors));
            //    });
            //}
            //else
            IsLoading = false;
        }

        private void DataLoadCompleteRefresh(DomainContextLoadBatch batch)
        {
            List<Exception> LoadErrors = new List<Exception>();

            if (batch.FailedOperationCount > 0)
            {
                foreach (var fop in batch.FailedOperations)
                    if (fop.HasError)
                    {
                        LoadErrors.Add(fop.Error);
                    }
            }

            IsLoading = true;
            if (OnRefreshLoaded != null)
            {
                EntityEventArgs<Discipline> e = new EntityEventArgs<Discipline>(null, DataLoadType.SERVER);
                LoadErrors.ForEach(er => e.EntityErrors.Add(er.Message));
                OnRefreshLoaded(this, e);
            }

            IsLoading = false;
        }

        public event EventHandler<ErrorEventArgs> OnSaved;

        public void SaveChanges()
        {
            Context.SubmitChanges();
        }

        public bool SaveAllAsync()
        {
            var open_or_invalid = OpenOrInvalidObjects(Context);
            if (open_or_invalid) //TODO: should we raise/return an error or something???
            {
                PendingSubmit = true;
                return false;
            }

            PendingSubmit = false;


            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                IsLoading = true;
                Context.SubmitChanges(g => HandleErrorResults(g, OnSaved), null);
            });

            return true;
        }

        public void RejectChanges()
        {
            Context.RejectChanges();
        }

        #endregion

        public bool ContextHasChanges => Context.HasChanges;

        void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public void Cleanup()
        {
            Disciplines.Cleanup();

            if (ViewOnlyContextAuth != null)
            {
                ViewOnlyContextAuth.EntityContainer.Clear();
                ViewOnlyContextAuth = null;
            }

            if (ViewOnlyContext != null)
            {
                ViewOnlyContext.EntityContainer.Clear();
                ViewOnlyContext = null;
            }

            if (Context != null)
            {
                Context.PropertyChanged -= Context_PropertyChanged;
                Context.EntityContainer.Clear();
                Context = null;
            }

            VirtuosoObjectCleanupHelper.CleanupAll(this);
        }
    }

    public class InsAuthOrder_CViewAll_Comparer : IEqualityComparer<InsuranceServiceLineView>
    {
        public bool Equals(InsuranceServiceLineView x, InsuranceServiceLineView y)
        {
            return
                x.InsuranceKey == y.InsuranceKey &&
                x.ServiceLineKey == y.ServiceLineKey;
        }

        public int GetHashCode(InsuranceServiceLineView obj)
        {
            unchecked // overflow is fine
            {
                int hash = 17;
                hash = hash * 23 + obj.InsuranceKey.GetHashCode();
                hash = hash * 23 + obj.ServiceLineKey.GetHashCode();
                return hash;
            }
        }
    }
}