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
    [Export(typeof(IAllergyCodeService))]
    public class AllergyCodeService : PagedModelBase, IAllergyCodeService
    {
        public AllergyCodeService()
        {
            Context = new VirtuosoDomainContext();
            AllergyCodes = new PagedEntityCollectionView<AllergyCode>(Context.AllergyCodes, this);
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

        #region IModelDataService<AllergyCode> Members

        public void Add(AllergyCode entity)
        {
            Context.AllergyCodes.Add(entity);
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
                Context.AllergyCodes.Clear();

                int allergycodekey = 0;
                string searchvalue = string.Empty;
                bool inactive = false;

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "AllergyCodeKey":
                                allergycodekey = Int32.Parse(item.Value);
                                break;
                            case "Substance":
                                searchvalue = item.Value;
                                break;
                            case "Inactive":
                                inactive = Convert.ToBoolean(item.Value);
                                break;
                        }

                    if (isSystemSearch == false)
                    {
                        //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
                        inactive = false;
                    }
                }

                var query = Context.GetAllergyCodeForSearchQuery(allergycodekey, searchvalue, inactive);

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
                Context.AllergyCodes.Clear();

                int allergycodekey = 0;
                string searchvalue = string.Empty;

                bool inactive = false;

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "AllergyCodeKey":
                                allergycodekey = Int32.Parse(item.Value);
                                break;
                            case "Substance":
                                searchvalue = item.Value;
                                break;
                            case "Inactive":
                                inactive = Convert.ToBoolean(item.Value);
                                break;
                        }
                }

                //this method should only be used by the maint. - e..g it will only be requesting a single ICD code - never a list...
                var query = Context.GetAllergyCodeForSearchQuery(allergycodekey, searchvalue, inactive); 

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public IEnumerable<AllergyCode> Items => Context.AllergyCodes;

        PagedEntityCollectionView<AllergyCode> _AllergyCodes;

        public PagedEntityCollectionView<AllergyCode> AllergyCodes
        {
            get { return _AllergyCodes; }
            set
            {
                if (_AllergyCodes != value)
                {
                    _AllergyCodes = value;
                    this.RaisePropertyChanged(p => p.AllergyCodes);
                }
            }
        }

        public event EventHandler<EntityEventArgs<AllergyCode>> OnLoaded;

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

        public void Remove(AllergyCode entity)
        {
            throw new NotImplementedException();
        }

        public bool ContextHasChanges => Context.HasChanges;
        public VirtuosoDomainContext Context { get; set; }

        void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public void Cleanup()
        {
            AllergyCodes.Cleanup();

            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}