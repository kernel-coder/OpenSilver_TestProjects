#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using RiaServicesContrib;
using RiaServicesContrib.Extensions;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public static class DistributionText
    {
        public const String EXTEND_DISTRIBUTION = "Extend Distribution";
        public const String EDIT_DISTRIBUTION = "Edit Distribution";
        public const String BUILD_DISTRIBUTION = "Build Distribution";
    }

    //Class functions as DataContext for enabling VIEW: AuthDetailGeneralView
    public class AuthInstanceGeneralViewModel : AuthInstanceViewModelBase
    {
        protected CommandManager CommandManager { get; set; }

        private List<AuthDistributionViewModel.Distribution> AuthDistribution =
            new List<AuthDistributionViewModel.Distribution>();

        private CollectionViewSource _DistributionCollection;

        public CollectionViewSource DistributionCollection
        {
            get { return _DistributionCollection; }
            set
            {
                _DistributionCollection = value;
                RaisePropertyChanged("DistributionCollection");
            }
        }

        private bool _ShowDistributionCollection;

        public bool ShowDistributionCollection
        {
            get { return _ShowDistributionCollection; }
            set
            {
                _ShowDistributionCollection = value;
                RaisePropertyChanged("ShowDistributionCollection");
            }
        }

        DateTime OriginalEffectiveToDate { get; set; }

        public AuthInstanceGeneralViewModel(AdmissionAuthorizationInstance instance, AuthMode mode)
            : base(VMTypeSelectorEnum.GEN, mode)
        {
            //Do this FIRST
            AuthInstanceSelectedItem = instance;
            OriginalEffectiveToDate = DateTime.MaxValue;

            DistributionEnabled = TenantSettingsCache.Current.TenantSettingAuthorizationDistributionEnabled;

            DistributionCollection = new CollectionViewSource();
            DistributionCollection.SortDescriptions.Add(new SortDescription("FromDateTime", ListSortDirection.Ascending));

            AuthInstanceSelectedItem.PropertyChanged += SelectedItem_PropertyChanged;

            LoadAvailableServiceTypeGroups();
            LoadAvailableServiceTypes();

            SetupCommands();
            CommandManager = new CommandManager(this);

            MessengerInstance.Register<List<AuthDistributionViewModel.Distribution>>(this,
                AuthInstanceSelectedItem.AdmissionAuthorizationInstanceKey, ReceiveDistribution);

            if (AuthMode.EDIT == mode)
            {
                if (AuthInstanceSelectedItem.IsDistributed)
                {
                    OriginalEffectiveToDate =
                        AuthInstanceSelectedItem.EffectiveToDate
                            .GetValueOrDefault(); //save this value - will need to determine if we can extend the auth range w/o delete stamping all existing details
                    ShowEditDistributionBillingWarning = true;
                    ReceiveDistribution();
                    OK_Distribution();
                }
                else
                {
                    ShowDistributionCollection = false;
                }
            }
        }

        private void SetupCommands()
        {
            DistributeAuthorizationsCommand = new RelayCommand(
                () =>
                {
                    var framework = View.Target as FrameworkElement;
                    var distribution_popup = new AuthDistributionPopup(
                        AuthInstanceSelectedItem,
                        __distribution,
                        framework.ActualHeight,
                        framework.ActualWidth,
                        Mode,
                        DistributeAuthorizationsCommandText.Equals(DistributionText.EXTEND_DISTRIBUTION));

                    distribution_popup.Closed += distribution_popup_Closed;
                    distribution_popup.Show();
                },
                () => (AuthInstanceSelectedItem.ValidationErrors.Any() == false && DistributionIsAllowed()));
        }

        bool DistributionIsAllowed()
        {
            return (AuthInstanceSelectedItem.AuthorizationAmount > 0 &&
                    (AuthInstanceSelectedItem.EffectiveToDate.HasValue &&
                     AuthInstanceSelectedItem.EffectiveToDate.Value != DateTime.MinValue));
        }

        void SelectedItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (CommandManager == null)
            {
                return;
            }

            if (IsBaseClassProperty(e.PropertyName))
            {
                return;
            }

            //BEGIN - US 40030: Edit of Authorization detail header dates
            if (Mode == AuthMode.EDIT && AuthInstanceSelectedItem.IsDistributed)
            {
                if (DistributionIsAllowed())
                {
                    if (AuthInstanceSelectedItem.Validate())
                    {
                        DistributeAuthorizationsCommandText = Set_DistributeAuthorizationsCommandText();
                    }
                }
            }
            //END - US 40030: Edit of Authorization detail header dates

            CommandManager.RaiseCanExecuteChanged();
        }

        bool IsBaseClassProperty(string propertyName)
        {
            //NOTE: base class properties can blow the stack from the property change listener, so exclude them - we primarily only care about AuthorizationAmount, EffectiveFromDate and EffectiveToDate
            BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance;
            var qry =
                from p in AuthInstanceSelectedItem.GetType().GetProperties(bindingAttr) //select database properties
                where p.GetCustomAttributes(typeof(DataMemberAttribute), true).Length > 0
                      && p.GetSetMethod() != null
                select p;
            var ret = qry.Any(e => e.Name.Equals(propertyName)) == false;
            return ret;
        }

        void distribution_popup_Closed(object sender, EventArgs e)
        {
            var window = sender as AuthDistributionPopup;
            if (window != null)
            {
                if (window.DialogResult.HasValue)
                {
                    var __okPressed = window.DialogResult.GetValueOrDefault(); //was OK or Cancel pressed?
                    if (__okPressed)
                    {
                        OK_Distribution();
                    }
                    else
                    {
                        //Anthing to do - just cancelling the distribution dialog?
                        CANCEL_Distribution();
                    }
                }
                else
                {
                    //Anthing to do - just cancelling the distribution dialog?
                    CANCEL_Distribution();
                }
            }
        }

        private bool _ShowEditDistributionBillingWarning;

        public bool ShowEditDistributionBillingWarning
        {
            get { return _ShowEditDistributionBillingWarning; }
            set
            {
                _ShowEditDistributionBillingWarning = value;
                RaisePropertyChanged("ShowEditDistributionBillingWarning");
            }
        }

        private bool _DistributionEnabled;

        public bool DistributionEnabled
        {
            get { return _DistributionEnabled; }
            set
            {
                _DistributionEnabled = value;
                RaisePropertyChanged("DistributionEnabled");
            }
        }

        private string _DistributeAuthorizationsCommandText = DistributionText.BUILD_DISTRIBUTION;

        public string DistributeAuthorizationsCommandText
        {
            get { return _DistributeAuthorizationsCommandText; }
            set
            {
                _DistributeAuthorizationsCommandText = value;
                RaisePropertyChanged("DistributeAuthorizationsCommandText");
            }
        }

        public RelayCommand DistributeAuthorizationsCommand { get; protected set; }

        public CollectionViewSource _AvailableServiceTypes = new CollectionViewSource();
        public ICollectionView AvailableServiceTypes => _AvailableServiceTypes.View;

        public CollectionViewSource _AvailableServiceTypeGroups = new CollectionViewSource();
        public ICollectionView AvailableServiceTypeGroups => _AvailableServiceTypeGroups.View;

        private void LoadAvailableServiceTypes()
        {
            // Break the binding between popup loads.  For some reason the bindings were holding on to all rows edited
            // if this isn't done and then updating all rows when one is updated.  It was tracked down to this collection.
            _AvailableServiceTypes.Source = null;
            if (AuthInstanceSelectedItem == null)
            {
                return;
            }

            List<ServiceType> SvcTypeList = ServiceTypeCache.GetActiveServiceTypes();
            // Add inactive rows assigned to already saved rows.
            ServiceType Inactivesvc = null;
            if (AuthInstanceSelectedItem != null && AuthInstanceSelectedItem.ServiceTypeKey != null &&
                AuthInstanceSelectedItem.ServiceTypeKey > 0)
            {
                Inactivesvc = ServiceTypeCache.GetServiceTypeFromKey((int)AuthInstanceSelectedItem.ServiceTypeKey);
            }

            if (Inactivesvc != null && !SvcTypeList.Contains(Inactivesvc))
            {
                SvcTypeList.Insert(0, Inactivesvc);
            }

            ServiceType emptyDisc = new ServiceType
            {
                ServiceTypeKey = 0,
                Description = " "
            };
            SvcTypeList.Insert(0, emptyDisc);

            _AvailableServiceTypes.Source = SvcTypeList;
            FilterAvailableServiceTypes();
            RaisePropertyChanged("AvailableServiceTypes");
            RaisePropertyChanged("AuthInstanceSelectedItem");
        }

        public void FilterAvailableServiceTypes()
        {
            if (AvailableServiceTypes != null)
            {
                AvailableServiceTypes.Filter = item =>
                {
                    ServiceType st = item as ServiceType;
                    if (AuthInstanceSelectedItem == null)
                    {
                        return false;
                    }

                    if (st.ServiceTypeKey == 0 && st.Description.Equals(" "))
                    {
                        return true;
                    }

                    if ((SelectedAuthType == null)
                        || (SelectedAuthType.Type != "DISC")
                        || (SelectedAuthType.Key != st.DisciplineKey)
                       )
                    {
                        return false;
                    }

                    if (st.ServiceTypeKey <= 0)
                    {
                        return true;
                    }

                    if (st.FinancialUseOnly)
                    {
                        return false; // filter out FinancialUseOnly ServiceTypes
                    }

                    if (st.DisciplineKey == SelectedAuthType.Key)
                    {
                        return true;
                    }

                    return false;
                };
            }

            RaisePropertyChanged("AvailableServiceTypes");
            RaisePropertyChanged("AvailableServiceTypeGroups");
            RaisePropertyChanged("AuthInstanceSelectedItem");
        }

        private void LoadAvailableServiceTypeGroups()
        {
            // Break the binding between popup loads.  For some reason the bindings were holding on to all rows edited
            // if this isn't done and then updating all rows when one is updated.  It was tracked down to this collection.
            _AvailableServiceTypeGroups.Source = null;
            if (AuthInstanceSelectedItem == null)
            {
                return;
            }

            List<CodeLookup> CodeLookupList = CodeLookupCache.GetCodeLookupsFromType("ServiceTypeGroup");
            // Add inactive rows assigned to already saved rows.
            CodeLookup Inactivesvc = null;
            if (AuthInstanceSelectedItem != null && AuthInstanceSelectedItem.ServiceTypeGroupKey != null &&
                AuthInstanceSelectedItem.ServiceTypeGroupKey > 0)
            {
                Inactivesvc =
                    CodeLookupCache.GetCodeLookupFromKey(
                        AuthInstanceSelectedItem.ServiceTypeGroupKey.GetValueOrDefault());
            }

            if (Inactivesvc != null && !CodeLookupList.Contains(Inactivesvc))
            {
                CodeLookupList.Insert(0, Inactivesvc);
            }

            var emptyDisc = new CodeLookup
            {
                CodeDescription = " "
            };
            CodeLookupList.Insert(0, emptyDisc);
            _AvailableServiceTypeGroups.Source = CodeLookupList;
            FilterAvailableServiceTypeGroups();
            RaisePropertyChanged("AvailableServiceTypeGroups");
            RaisePropertyChanged("AuthInstanceSelectedItem");
        }

        public void FilterAvailableServiceTypeGroups()
        {
            if (AvailableServiceTypeGroups != null)
            {
                AvailableServiceTypeGroups.Filter = item =>
                {
                    if (AuthInstanceSelectedItem == null)
                    {
                        return false;
                    }

                    if (SelectedAuthType == null || SelectedAuthType.Type != "DISC")
                    {
                        return false;
                    }

                    return true;
                };
            }

            RaisePropertyChanged("AvailableServiceTypes");
            RaisePropertyChanged("AvailableServiceTypeGroups");
            RaisePropertyChanged("AuthInstanceSelectedItem");
        }

        public override void AuthTypeChanged(AuthType selectedAuthType)
        {
            base.AuthTypeChanged(selectedAuthType);

            FilterAvailableServiceTypes();
            FilterAvailableServiceTypeGroups();
        }

        List<AuthDistributionViewModel.Distribution> __distribution;

        //Called by MessengerInstance.Register when the distribution dialog sends the distribution to the underlying screen (this screen AuthInstanceGeneralViewModel)
        void ReceiveDistribution(List<AuthDistributionViewModel.Distribution> distribution)
        {
            if (distribution != null)
            {
                __distribution = distribution;
            }
        }

        //Called by constructor when in EDIT mode in order to build __distribution from a previously saved list of AdmissionAuthorizationDetail
        void ReceiveDistribution()
        {
            if (AuthInstanceSelectedItem.IsDistributed == false)
            {
                return;
            }

            var distCycleLookup =
                CodeLookupCache.GetCodeLookupFromKey(AuthInstanceSelectedItem.DistCycleKey.GetValueOrDefault());

            __distribution = new List<AuthDistributionViewModel.Distribution>();

            foreach (var detail in AuthInstanceSelectedItem.AdmissionAuthorizationDetail)
            {
                if (detail.DeletedDate.HasValue)
                {
                    continue;
                }

                var d = new AuthDistributionViewModel.Distribution
                {
                    AdmissionAuthorizationDetailKey = detail.AdmissionAuthorizationDetailKey,

                    AuthCount = detail.AuthCount.HasValue ? detail.AuthCount.Value.ToString() : string.Empty,
                    AuthCountLastUpdate = detail.AuthCountLastUpdate.HasValue
                        ? detail.AuthCountLastUpdate.Value.ToString("g")
                        : string.Empty,

                    FromDate = detail.EffectiveFromDate.ToString(AuthDistributionViewModel.dateFormat),
                    FromDateTime = detail.EffectiveFromDate,

                    ThruDate =
                        detail.EffectiveToDate.GetValueOrDefault().ToString(AuthDistributionViewModel.dateFormat),
                    ThruDateTime = detail.EffectiveToDate.GetValueOrDefault(),

                    NumberPerCycle = detail.AuthorizationAmount,
                    Cycle = distCycleLookup.CodeDescription,
                };

                __distribution.Add(d);
            }
        }

        void OK_Distribution()
        {
            DistributionCollection.Source = __distribution;
            ShowDistributionCollection = (__distribution != null && __distribution.Any()) ? true : false;
            DistributeAuthorizationsCommandText = Set_DistributeAuthorizationsCommandText();
            RaisePropertyChanged("DistributionCollection");
        }

        private string Set_DistributeAuthorizationsCommandText()
        {
            if (__distribution != null && __distribution.Any()) // ? "Edit Distribution" : "Build Distribution";
            {
                var changesOnlyState = AuthInstanceSelectedItem.ExtractState(ExtractType.ChangesOnlyState);
                if (changesOnlyState.Keys.Contains("EffectiveToDate")
                    && changesOnlyState.Keys.Contains("EffectiveFromDate") == false
                    && AuthInstanceSelectedItem.EffectiveToDate > OriginalEffectiveToDate)
                {
                    return DistributionText.EXTEND_DISTRIBUTION; //"Extend Distribution";
                }

                return DistributionText.EDIT_DISTRIBUTION; //"Edit Distribution";
            }

            return DistributionText.BUILD_DISTRIBUTION; //"Build Distribution";
        }

        void CANCEL_Distribution()
        {
            //FYI: not deleting the 'existing' collection - even on ADD NEW, so that we preserve any previously generated distribution
            ShowDistributionCollection = (__distribution != null && __distribution.Any()) ? true : false;
            DistributeAuthorizationsCommandText = Set_DistributeAuthorizationsCommandText();
        }

        public override void CreateAuthorizationDetail(AdmissionAuthorizationInstance instance)
        {
            if (__distribution != null)
            {
                if (Mode == AuthMode.ADD)
                {
                    DeleteStampAndAddAuthDetail(instance);

                    //On AdmissionAuthorizationInstance, Set IsDistributed
                    //NOTE: DistCycleKey(set by dist dialog) and NumberPerCycle are set by AuthDistributionView dialog
                    instance.IsDistributed = true;
                }
                else
                {
                    if (DistributionIsChanged())
                    {
                        //differentiate change
                        //1.) deletestamp all rows 
                        //2.) or are we simply extending 
                        //3.) or are we simply changing the numbers

                        //NOTE: only delete stamps if (__distribution.Any(d => d.IsDirty() && d.AdmissionAuthorizationDetailKey.HasValue == false)) 
                        //      E.G. if we have any that were never saved to the database - re-generate the distribution from scratch
                        DeleteStampAndAddAuthDetail(instance);
                    }

                    if (instance
                        .HasChanges) //distribution may not have changed - but properties bound to the instance may have - need to propogate those to the detail objects...
                    {
                        foreach (var detail in instance.AdmissionAuthorizationDetail.Where(aad =>
                                     aad.DeletedDate.HasValue == false))
                        {
                            //Copy all 'instance' properties to the detail - except those set in the distribution dialog - e.g. the NumberPerCycle as AuthorizationAmount - and the distributed date ranges...
                            var exclude = new[] { "AuthorizationAmount", "EffectiveFromDate", "EffectiveToDate" };
                            Portable.Utility.DynamicCopy.CopyDataMemberProperties(instance, detail, exclude);
                        }

                        //On AdmissionAuthorizationInstance, Set IsDistributed
                        //NOTE: DistCycleKey(set by dist dialog) and NumberPerCycle are set by AuthDistributionView dialog
                        instance.IsDistributed = true;
                    }
                }
            }
            else //Not IsDistributed
            {
                if (Mode == AuthMode.ADD)
                {
                    if (instance.HasChanges || instance.IsNew)
                    {
                        var newDetail = new AdmissionAuthorizationDetail();
                        Portable.Utility.DynamicCopy.CopyDataMemberProperties(instance, newDetail);
                        instance.AdmissionAuthorizationDetail.Add(newDetail);
                    }
                }
                else
                {
                    if (instance.HasChanges)
                    {
                        var updatedDetail =
                            instance.AdmissionAuthorizationDetail.First(); //Will we always have 1 (and only 1)?
                        Portable.Utility.DynamicCopy.CopyDataMemberProperties(instance, updatedDetail);
                    }

                    instance.IsDistributed = false;
                }
            }
        }

        private bool DistributionIsChanged()
        {
            if (Mode == AuthMode.ADD)
            {
                return true;
            }

            bool haveChangedDistributionObject =
                __distribution.Any(d => d.IsDirty() || d.AdmissionAuthorizationDetailKey.HasValue == false);

            return (haveChangedDistributionObject);
        }

        private void DeleteStampAndAddAuthDetail(AdmissionAuthorizationInstance instance)
        {
            if (Mode == AuthMode.ADD)
            {
                ____deleteStampAndAddAuthDetail(instance);
            }
            else
            {
                //Reconcile __distribution with existing instance.AdmissionAuthorizationDetail rows...
                //only deletestamp/update rows that are 'dirty'

                //If have any distribution objects which were never saved to the database - then user must have re-generated the auth details.

                //When extending the EffectiveToDate - will have Dirty rows with a server key - do not delete stamp these if we're in 'extend' mode...
                //if we have any that were never saved to the database - re-generate the distribution
                if (__distribution.Any(d => d.IsDirty()
                                            && d.AdmissionAuthorizationDetailKey.HasValue == false)
                    && (DistributeAuthorizationsCommandText.Equals(DistributionText.EXTEND_DISTRIBUTION) == false))
                {
                    ____deleteStampAndAddAuthDetail(instance);
                }
                else //else update existing detail rows with those that are now dirty
                {
                    if (DistributeAuthorizationsCommandText.Equals(DistributionText.EXTEND_DISTRIBUTION))
                    {
                        //1.) May have dirty records
                        //2.) Should have 'NEW' records

                        //1.) Dirty rows with a server key - update their AuthorizationAmount
                        foreach (var dist in __distribution.Where(d =>
                                     d.IsDirty() && d.AdmissionAuthorizationDetailKey.HasValue))
                        {
                            System.Diagnostics.Debug.WriteLine("DIRTY Server Row\t{0:10}\t\t{1}\t\t{2}\t\t{3}",
                                dist.AdmissionAuthorizationDetailKey.GetValueOrDefault(), dist.FromDateTime.Date,
                                dist.ThruDateTime.Date, dist.IsDirty());

                            //Find matching detail record
                            var detail = instance.AdmissionAuthorizationDetail
                                .FirstOrDefault(aad => aad.AdmissionAuthorizationDetailKey == dist.AdmissionAuthorizationDetailKey);
                            if (detail != null)
                            {
                                detail.AuthorizationAmount = dist.NumberPerCycle;
                                detail.EffectiveToDate =
                                    dist.ThruDateTime; //when extending a distribution - the last original auth detail might have had it's thru date updated...
                            }
                        }

                        //2.) Dirty rows without a server key - create and add to instance.AmissionAuthorizationDetail
                        foreach (var dist in __distribution
                                     .Where(d => d.IsDirty() && d.AdmissionAuthorizationDetailKey.HasValue == false)
                                     .OrderByDescending(dd =>
                                         dd.FromDateTime)) //reverse create by effective date - this way it is inserting into DB as correct order
                        {
                            System.Diagnostics.Debug.WriteLine("DIRTY Extended Row\t{0:10}\t\t{1}\t\t{2}\t\t{3}",
                                dist.AdmissionAuthorizationDetailKey.GetValueOrDefault(), dist.FromDateTime.Date,
                                dist.ThruDateTime.Date, dist.IsDirty());

                            var newDetail = new AdmissionAuthorizationDetail();
                            Portable.Utility.DynamicCopy.CopyDataMemberProperties(instance, newDetail);

                            //Now that we've copied all the 'instance' properties to the detail - make sure we're using the NumberPerCycle as AuthorizationAmount - and the distributed date ranges...
                            newDetail.AuthorizationAmount = dist.NumberPerCycle;
                            newDetail.EffectiveFromDate = dist.FromDateTime;
                            newDetail.EffectiveToDate = dist.ThruDateTime;

                            System.Diagnostics.Debug.WriteLine(
                                "Adding AdmissionAuthorizationDetail.  ID: {0}\tAmount: {1}\tFrom Date: {2}\tThru Date: {3}",
                                newDetail.AdmissionAuthorizationDetailKey, newDetail.AuthorizationAmount,
                                newDetail.EffectiveFromDate, newDetail.EffectiveToDate);

                            instance.AdmissionAuthorizationDetail.Add(newDetail);
                        }
                    }
                    else
                    {
                        foreach (var dist in __distribution.Where(d => d.IsDirty()))
                        {
                            //Find matching detail record
                            var detail = instance.AdmissionAuthorizationDetail
                                .FirstOrDefault(aad => aad.AdmissionAuthorizationDetailKey == dist.AdmissionAuthorizationDetailKey);
                            if (detail != null)
                            {
                                detail.AuthorizationAmount = dist.NumberPerCycle;
                            }
                        }
                    }
                }
            }
        }

        private void ____deleteStampAndAddAuthDetail(AdmissionAuthorizationInstance instance)
        {
            //DeleteStamp all existing AdmissionAuthorizationDetail rows
            var deletedDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            instance.AdmissionAuthorizationDetail.ForEach(detail =>
            {
                System.Diagnostics.Debug.WriteLine(
                    "DeleteStamping AdmissionAuthorizationDetail.  ID: {0}\tAmount: {1}\tFrom Date: {2}\tThru Date: {3}\tAuthCount: {4}\tAuthCountLastUpdate: {5}",
                    detail.AdmissionAuthorizationDetailKey, detail.AuthorizationAmount, detail.EffectiveFromDate,
                    detail.EffectiveToDate, detail.AuthCount, detail.AuthCountLastUpdate);

                detail.DeletedDate = deletedDate;
                detail.DeletedBy = Virtuoso.Services.Authentication.WebContext.Current.User.MemberID;
            });

            foreach (var d in
                     __distribution.OrderByDescending(dd =>
                         dd.FromDateTime)) //reverse create by effective date - this way it is inserting into DB as correct order
            {
                var newDetail = new AdmissionAuthorizationDetail();
                Portable.Utility.DynamicCopy.CopyDataMemberProperties(instance, newDetail);

                //Now that we've copied all the 'instance' properties to the detail - make sure we're using the NumberPerCycle as AuthorizationAmount - and the distributed date ranges...
                newDetail.AuthorizationAmount = d.NumberPerCycle;
                newDetail.EffectiveFromDate = d.FromDateTime;
                newDetail.EffectiveToDate = d.ThruDateTime;

                System.Diagnostics.Debug.WriteLine(
                    "Adding AdmissionAuthorizationDetail.  ID: {0}\tAmount: {1}\tFrom Date: {2}\tThru Date: {3}",
                    newDetail.AdmissionAuthorizationDetailKey, newDetail.AuthorizationAmount,
                    newDetail.EffectiveFromDate, newDetail.EffectiveToDate);

                instance.AdmissionAuthorizationDetail.Add(newDetail);
            }
        }

        public override void Cleanup()
        {
            AuthInstanceSelectedItem.PropertyChanged -= SelectedItem_PropertyChanged;
            CommandManager?.CleanUp();
            base.Cleanup();
        }
    }
}