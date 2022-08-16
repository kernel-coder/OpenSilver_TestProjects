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
    [Export(typeof(ICodeLookupService))]
    public class CodeLookupService : PagedModelBase, ICodeLookupService
    {
        public VirtuosoDomainContext Context { get; set; }

        public CodeLookupService()
        {
            Context = new VirtuosoDomainContext();
            CodeLookupHeaders = new PagedEntityCollectionView<CodeLookupHeader>(Context.CodeLookupHeaders, this);
            Context.PropertyChanged += Context_PropertyChanged;
        }

        public CodeLookupService(VirtuosoDomainContext ctx)
        {
            Context = ctx;
            CodeLookupHeaders = new PagedEntityCollectionView<CodeLookupHeader>(Context.CodeLookupHeaders, this);
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

        #region IModelDataService<CodeLookupHeader> Members

        public void Add(CodeLookupHeader entity)
        {
            Context.CodeLookupHeaders.Add(entity);
        }

        public void Remove(CodeLookupHeader entity)
        {
            Context.CodeLookupHeaders.Remove(entity);
        }

        public void Remove(CodeLookup entity)
        {
            Context.CodeLookups.Remove(entity);
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
        }

        public void GetSearchAsync(bool isSystemSearch)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.EntityContainer.Clear();

                var query = Context.GetCodeLookupHeaderForSearchQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "CodeLookupHeaderKey":
                                query = query.Where(p => p.CodeLookupHeaderKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Description":
                                query = query.Where(
                                    i => i.CodeTypeDescription.ToLower().Contains(searchvalue.ToLower()));
                                break;
                        }
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
                Context.CodeLookupHeaders.Clear();

                var query = Context.GetCodeLookupHeaderQuery();

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "CodeLookupHeaderKey":
                                query = query.Where(p => p.CodeLookupHeaderKey == Convert.ToInt32(searchvalue));
                                break;
                            case "Description":
                                query = query.Where(i =>
                                    i.CodeType.ToLower().Contains(searchvalue.ToLower()) ||
                                    i.CodeTypeDescription.ToLower().Contains(searchvalue.ToLower()));
                                break;
                        }
                    }
                }
                else
                {
                    query = query.Where(p => p.CodeLookupHeaderKey <= 0);
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

        public IEnumerable<CodeLookupHeader> Items => Context.CodeLookupHeaders;

        PagedEntityCollectionView<CodeLookupHeader> _CodeLookupHeaders;

        public PagedEntityCollectionView<CodeLookupHeader> CodeLookupHeaders
        {
            get { return _CodeLookupHeaders; }
            set
            {
                if (_CodeLookupHeaders != value)
                {
                    _CodeLookupHeaders = value;
                    this.RaisePropertyChanged(p => p.CodeLookupHeaders);
                }
            }
        }

        public event EventHandler<EntityEventArgs<CodeLookupHeader>> OnLoaded;

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
            CodeLookupHeaders.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}