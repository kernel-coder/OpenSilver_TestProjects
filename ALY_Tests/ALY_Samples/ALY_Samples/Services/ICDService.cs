#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IICDCodeService))]
    public class ICDService : PagedModelBase, IICDCodeService
    {
        public VirtuosoDomainContext Context { get; set; }

        public ICDService()
        {
            Context = new VirtuosoDomainContext();
            ICDCodes = new PagedEntityCollectionView<ICDCode>(Context.ICDCodes, this);
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

        #region IModelDataService<ICD> Members

        public void Add(ICDCode entity)
        {
            Context.ICDCodes.Add(entity);
        }

        public void Remove(ICDCode entity)
        {
            Context.ICDCodes.Remove(entity);
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
                Context.EntityContainer.Clear();

                int _ICDCodeKey = 0;
                int _VersionCodeLookupKey = 0;
                string _Version = string.Empty;
                string _Code = string.Empty;
                string _Description = string.Empty;
                bool _Inactive = false;

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "ICDCodeKey":
                                //query = query.Where(p => p.ICDCodeKey == Convert.ToInt32(searchvalue));
                                _ICDCodeKey = Int32.Parse(item.Value);
                                break;
                            case "Version":
                                try
                                {
                                    _VersionCodeLookupKey = Int32.Parse(item.Value);
                                }
                                catch
                                {
                                    _VersionCodeLookupKey = 0;
                                }

                                _Version = CodeLookupCache.GetCodeFromKey(_VersionCodeLookupKey);
                                if (string.IsNullOrWhiteSpace(_Version))
                                {
                                    _Version = string.Empty;
                                }

                                break;
                            case "Code":
                                _Code = item.Value;
                                break;
                            case "Description":
                                _Description = item.Value;
                                break;
                            case "Inactive":
                                _Inactive = Convert.ToBoolean(item.Value);
                                break;
                        }

                    if (isSystemSearch == false)
                    {
                        //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
                        _Inactive = false;
                    }
                }

                var query = Context.GetICDCodeForSearchQuery(_ICDCodeKey, _Version, _Code, _Description, _Inactive);

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
                IsLoading = true;

                Context.RejectChanges();
                Context.ICDCodes.Clear();

                int _ICDCodeKey = 0;
                int _VersionCodeLookupKey = 0;
                string _Code = string.Empty;
                string _Version = string.Empty;
                string _Description = string.Empty;
                bool _Inactive = false;

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "ICDCodeKey":
                                _ICDCodeKey = Int32.Parse(item.Value);
                                break;
                            case "Version":
                                try
                                {
                                    _VersionCodeLookupKey = Int32.Parse(item.Value);
                                }
                                catch
                                {
                                    _VersionCodeLookupKey = 0;
                                }

                                _Version = CodeLookupCache.GetCodeFromKey(_VersionCodeLookupKey);
                                if (string.IsNullOrWhiteSpace(_Version))
                                {
                                    _Version = string.Empty;
                                }

                                break;
                            case "Code":
                                _Code = item.Value;
                                break;
                            case "Description":
                                _Description = item.Value;
                                break;
                            case "Inactive":
                                _Inactive = Convert.ToBoolean(item.Value);
                                break;
                        }
                }

                var query = Context.GetICDCodeForSearchQuery(_ICDCodeKey, _Version, _Code, _Description,
                    _Inactive); //this method should only be used by the maint. - e..g it will only be requesting a single ICD code - never a list...

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public IEnumerable<ICDCode> Items => Context.ICDCodes;

        PagedEntityCollectionView<ICDCode> _ICDCodes;

        public PagedEntityCollectionView<ICDCode> ICDCodes
        {
            get { return _ICDCodes; }
            set
            {
                if (_ICDCodes != value)
                {
                    _ICDCodes = value;
                    this.RaisePropertyChanged(p => p.ICDCodes);
                }
            }
        }

        public event EventHandler<EntityEventArgs<ICDCode>> OnLoaded;

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
            ICDCodes.Cleanup();
            if (Context != null)
            {
                Context.PropertyChanged -= Context_PropertyChanged;
                Context.EntityContainer.Clear();
                VirtuosoObjectCleanupHelper.CleanupAll(this);
                Context = null;
            }
        }

        #endregion
    }
}