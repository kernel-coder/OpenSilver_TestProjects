#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IAgencyParameterService))]
    public class AgencyParameterService : PagedModelBase, IAgencyParameterService
    {
        public VirtuosoDomainContext Context { get; set; }

        public AgencyParameterService()
        {
            Context = new VirtuosoDomainContext();
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

        #region IModelDataService<TenantSetting> Members

        public void Add(TenantSetting entity)
        {
            Context.TenantSettings.Add(entity);
        }

        public void Remove(TenantSetting entity)
        {
            Context.TenantSettings.Remove(entity);
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
                Context.TenantSettings.Clear();

                var query = Context.GetTenantSettingForMaintQuery();

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    null);
            });
        }

        public void UpdateAutoTrackingGroups(TenantSetting tenantsetting, object sender)
        {
            Context.UpdateAutoTrackingGroups(tenantsetting, AutoTrackComplete, sender);
        }

        private void AutoTrackComplete(InvokeOperation<bool> obj)
        {
            //no need to fire anything  
        }

        public void TestAzureConnection(string ExternalAuthenticationTenantID,
            string ExternalAuthenticationApplicationID, string ExternalAuthenticationSecret, bool IsDecrypted)
        {
            Context.TestAzureConnection(ExternalAuthenticationTenantID, ExternalAuthenticationApplicationID,
                ExternalAuthenticationSecret, IsDecrypted, TestAzureConnectionReturned, null);
        }

        public event Action<InvokeOperation<string>> TestAzureConnectionReturned;

        public IEnumerable<TenantSetting> Items => Context.TenantSettings;

        public event EventHandler<EntityEventArgs<TenantSetting>> OnLoaded;

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
            Context.PropertyChanged -= Context_PropertyChanged;
        }
    }
}