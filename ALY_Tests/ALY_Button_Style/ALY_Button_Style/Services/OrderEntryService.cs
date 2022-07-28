#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IOrderEntryService))]
    public class OrderEntryService : PagedModelBase, IOrderEntryService
    {
        public VirtuosoDomainContext Context { get; set; }

        public OrderEntryService()
        {
            Context = new VirtuosoDomainContext();
            Orders = new PagedEntityCollectionView<OrderEntry>(Context.OrderEntries, this);
            Patients = new PagedEntityCollectionView<Patient>(Context.Patients, this);
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

        #region IModelDataService<OrderEntry> Members

        //Method required for IModelDataService, but this service does not allow adding of patients
        public void Add(Admission entity)
        {
            throw new NotImplementedException();
        }

        public void Add(OrderEntry entity)
        {
            Context.OrderEntries.Add(entity);
        }

        public void Add(OrderEntryVO entity)
        {
            Context.OrderEntryVOs.Add(entity);
        }

        public void Add(OrderEntrySignature entity)
        {
            Context.OrderEntrySignatures.Add(entity);
        }

        public void Add(OrderEntryCoSignature entity)
        {
            Context.OrderEntryCoSignatures.Add(entity);
        }

        //Method required for IModelDataService, but this service does not allow removing of patients
        public void Remove(Admission entity)
        {
            throw new NotImplementedException();
        }

        public void Remove(OrderEntry entity)
        {
            Context.OrderEntries.Remove(entity);
        }

        public void Remove(OrderEntryVO entity)
        {
            Context.OrderEntryVOs.Remove(entity);
        }

        public void Remove(OrderEntrySignature entity)
        {
            Context.OrderEntrySignatures.Remove(entity);
        }

        public void Remove(OrderEntryCoSignature entity)
        {
            Context.OrderEntryCoSignatures.Remove(entity);
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
        }

        public void GetSearchAsync(bool isSystemSearch)
        {
            GetAsync();
        }

        public void GetAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.OrderEntries.Clear();

                var query = Context.GetAdmissionAndOrderEntryQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;
                        switch (item.Field)
                        {
                            case "admission":
                                query = query.Where(p => p.AdmissionKey == Convert.ToInt32(searchvalue));
                                break;
                        }
                    }
                }

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public IEnumerable<Admission> Items => Context.Admissions;

        PagedEntityCollectionView<Patient> _Patients;

        public PagedEntityCollectionView<Patient> Patients
        {
            get { return _Patients; }
            set
            {
                if (_Patients != value)
                {
                    _Patients = value;
                    this.RaisePropertyChanged(p => p.Patients);
                }
            }
        }

        PagedEntityCollectionView<OrderEntry> _Orders;

        public PagedEntityCollectionView<OrderEntry> Orders
        {
            get { return _Orders; }
            set
            {
                if (_Orders != value)
                {
                    _Orders = value;
                    this.RaisePropertyChanged(p => p.Orders);
                }
            }
        }

        public event EventHandler<EntityEventArgs<Admission>> OnLoaded;

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
            Orders.Cleanup();
            Patients.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}