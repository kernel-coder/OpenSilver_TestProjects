#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ISupplyService))]
    public class SupplyService : PagedModelBase, ISupplyService
    {
        public VirtuosoDomainContext Context { get; set; }

        public SupplyService()
        {
            Context = new VirtuosoDomainContext();
            var contextServiceProvider = new SimpleServiceProvider();
            contextServiceProvider.AddService<ICodeLookupDataProvider>(new CodeLookupDataProvider());
            contextServiceProvider.AddService<IUniquenessCheckProvider>(new UniquenessCheckProvider());
            Context.ValidationContext = new ValidationContext(this, contextServiceProvider, null);

            Supplies = new PagedEntityCollectionView<Supply>(Context.Supplies, this);
        }

        #region Billing Codes

        public event EventHandler<EntityEventArgs<BillCodes>> OnBillCodesLoaded;

        public void LoadBillingCodeOverridesForType(String SourceTypeParm, int ElementKeyParm)
        {
            var query = Context.GetBillCodesBySourceTypeQuery(SourceTypeParm, ElementKeyParm);

            IsLoading = true;

            Context.Load(
                query,
                LoadBehavior.RefreshCurrent,
                g => HandleEntityResults(g, OnBillCodesLoaded),
                null);
        }

        public List<BillCodes> BillingCodes => Context.BillCodes.ToList();
        public List<BillCodes> BillingCodeOverrideItems => Context.BillCodes.ToList();

        public void Add(BillCodes CodeParm)
        {
            Context.BillCodes.Add(CodeParm);
        }

        public void Remove(BillCodes CodeParm)
        {
            Context.BillCodes.Remove(CodeParm);
        }

        #endregion

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

        #region IModelDataService<Supply> Members

        public void Add(Supply entity)
        {
            Context.Supplies.Add(entity);
        }

        public void Remove(Supply entity)
        {
            Context.Supplies.Remove(entity);
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
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
                Context.Supplies.Clear();

                var query = Context.GetSupplyQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "SupplyKey":
                                query = query.Where(p => p.SupplyKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Code":
                                query = query.Where(i => i.ItemCode.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Description":
                                query = query.Where(i =>
                                    i.Description1.ToLower().Contains(searchvalue.ToLower()) ||
                                    i.Description2.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Inactive":
                                bool inactive = Convert.ToBoolean(searchvalue);
                                if (!inactive)
                                {
                                    query = query.Where(i => i.EffectiveFrom < DateTime.Now);
                                    query = query.Where(i => i.EffectiveThru == null || i.EffectiveThru > DateTime.Now);
                                }

                                break;
                        }
                    }

                    if (isSystemSearch == false)
                    {
                        //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
                        query = query.Where(i => i.EffectiveFrom < DateTime.Now);
                        query = query.Where(i => i.EffectiveThru == null || i.EffectiveThru > DateTime.Now);
                    }
                }
                else
                {
                    query = query.Where(i => i.EffectiveFrom < DateTime.Now);
                    query = query.Where(i => i.EffectiveThru == null || i.EffectiveThru > DateTime.Now);
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
                Context.Supplies.Clear();

                var query = Context.GetSupplyQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "SupplyKey":
                                query = query.Where(p => p.SupplyKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Code":
                                query = query.Where(i => i.ItemCode.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Description":
                                query = query.Where(i =>
                                    i.Description1.ToLower().Contains(searchvalue.ToLower()) ||
                                    i.Description2.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Inactive":
                                bool inactive = Convert.ToBoolean(searchvalue);
                                if (!inactive)
                                {
                                    query = query.Where(i => i.EffectiveFrom < DateTime.Now);
                                    query = query.Where(i => i.EffectiveThru == null || i.EffectiveThru > DateTime.Now);
                                }

                                break;
                        }
                    }
                }
                else
                {
                    query = query.Where(i => i.EffectiveFrom < DateTime.Now);
                    query = query.Where(i => i.EffectiveThru == null || i.EffectiveThru > DateTime.Now);
                }

                query.IncludeTotalCount = true;

                if (PageSize > 0)
                {
                    query = query.Skip(PageSize * PageIndex);
                    query = query.Take(PageSize);
                }

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public IEnumerable<Supply> Items => Context.Supplies;

        PagedEntityCollectionView<Supply> _Supplies;

        public PagedEntityCollectionView<Supply> Supplies
        {
            get { return _Supplies; }
            set
            {
                if (_Supplies != value)
                {
                    _Supplies = value;
                    this.RaisePropertyChanged(p => p.Supplies);
                }
            }
        }

        public event EventHandler<EntityEventArgs<Supply>> OnLoaded;

        public event EventHandler<ErrorEventArgs> OnSaved;

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
            Supplies.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}