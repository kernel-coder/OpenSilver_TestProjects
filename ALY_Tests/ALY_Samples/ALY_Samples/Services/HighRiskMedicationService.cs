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
    [Export(typeof(IHighRiskMedicationService))]
    public class HighRiskMedicationService : PagedModelBase, IHighRiskMedicationService
    {
        public VirtuosoDomainContext Context { get; set; }

        public HighRiskMedicationService()
        {
            Context = new VirtuosoDomainContext();
            HighRiskMedications = new PagedEntityCollectionView<HighRiskMedication>(Context.HighRiskMedications, this);
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

        #region IModelDataService<HighRiskMedication> Members

        public void Add(HighRiskMedication entity)
        {
            Context.HighRiskMedications.Add(entity);
        }

        public void Remove(HighRiskMedication entity)
        {
            Context.HighRiskMedications.Remove(entity);
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
                Context.HighRiskMedications.Clear();

                var query = Context.GetHighRiskMedicationQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "HighRiskMedicationKey":
                                query = query.Where(p => p.HighRiskMedicationKey == Convert.ToInt32(searchvalue));
                                break;
                            case "MedicationName":
                                query = query.Where(p => p.MedicationName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "ServiceLineKey":
                                int serviceLineKey = 0;
                                try
                                {
                                    serviceLineKey = Convert.ToInt32(searchvalue);
                                }
                                catch
                                {
                                    serviceLineKey = 0;
                                }

                                if (serviceLineKey != 0)
                                {
                                    query = query.Where(p => p.ServiceLineKey == serviceLineKey);
                                }

                                break;
                            case "Inactive":
                                bool inactive = Convert.ToBoolean(searchvalue);
                                if (inactive == false)
                                {
                                    DateTime today = DateTime.Today.Date;
                                    query = query.Where(p =>
                                        ((p.EffectiveFromDate == null) || ((p.EffectiveFromDate != null) &&
                                                                           (p.EffectiveFromDate <= today))) &&
                                        ((p.EffectiveThruDate == null) || ((p.EffectiveThruDate != null) &&
                                                                           (p.EffectiveThruDate >= today))));
                                }

                                break;
                        }
                    }

                    if (isSystemSearch == false)
                    {
                        //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
                        DateTime today = DateTime.Today.Date;
                        query = query.Where(p =>
                            ((p.EffectiveFromDate == null) ||
                             ((p.EffectiveFromDate != null) && (p.EffectiveFromDate <= today))) &&
                            ((p.EffectiveThruDate == null) ||
                             ((p.EffectiveThruDate != null) && (p.EffectiveThruDate >= today))));
                    }
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
                Context.HighRiskMedications.Clear();

                var query = Context.GetHighRiskMedicationQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "HighRiskMedicationKey":
                                query = query.Where(p => p.HighRiskMedicationKey == Convert.ToInt32(searchvalue));
                                break;
                            case "MedicationName":
                                query = query.Where(p => p.MedicationName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "ServiceLine":
                                int serviceLineKey = Convert.ToInt32(searchvalue);
                                query = query.Where(p => p.ServiceLineKey == serviceLineKey);
                                break;
                            case "Inactive":
                                bool inactive = Convert.ToBoolean(searchvalue);
                                if (inactive == false)
                                {
                                    DateTime today = DateTime.Today.Date;
                                    query = query.Where(p =>
                                        ((p.EffectiveFromDate.HasValue == false) ||
                                         ((p.EffectiveFromDate.HasValue == true) &&
                                          (p.EffectiveFromDate.Value.Date <= today))) &&
                                        ((p.EffectiveThruDate.HasValue == false) ||
                                         ((p.EffectiveThruDate.HasValue == true) &&
                                          (p.EffectiveThruDate.Value.Date >= today))));
                                }

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

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public IEnumerable<HighRiskMedication> Items => Context.HighRiskMedications;

        PagedEntityCollectionView<HighRiskMedication> _HighRiskMedications;

        public PagedEntityCollectionView<HighRiskMedication> HighRiskMedications
        {
            get { return _HighRiskMedications; }
            set
            {
                if (_HighRiskMedications != value)
                {
                    _HighRiskMedications = value;
                    this.RaisePropertyChanged(p => p.HighRiskMedications);
                }
            }
        }

        public event EventHandler<EntityEventArgs<HighRiskMedication>> OnLoaded;

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
            HighRiskMedications.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}