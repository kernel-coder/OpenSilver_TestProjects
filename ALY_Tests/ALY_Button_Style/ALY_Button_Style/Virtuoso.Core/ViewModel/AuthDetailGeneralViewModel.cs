using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.ViewModel
{
    public class AdmissionAuthorizationDetailVM
    {
        public Nullable<int> AuthorizationType { get; set; }
        public decimal AuthorizationAmount { get; set; }
        public Nullable<int> ServiceTypeKey { get; set; }
        public DateTimeOffset EffectiveFromDate { get; set; }
        public Nullable<DateTimeOffset> EffectiveToDate { get; set; }
        public DateTime DateIssued { get; set; }
        public string AuthorizedBy { get; set; }
        public int MethodReceivedType { get; set; }
        public Guid ReceivedBy { get; set; }
        public string Comments { get; set; }
    }

    //Class functions as DataContext for enabling VIEW: AuthDetailGeneralView
    public class AuthDetailGeneralViewModel : AuthDetailViewModelBase
    {
        public AuthDetailGeneralViewModel(AdmissionAuthorizationDetail detail)
            : base(VMTypeSelectorEnum.GEN)
        {
            //SelectedAuthType = authType;

            //AuthDetailSelectedItem = new AdmissionAuthorizationDetailVM();
            //var me = this.AuthDetailSelectedItem;

            //me.AuthorizationType = detail.AuthorizationType;
            //me.AuthorizationAmount = detail.AuthorizationAmount;
            //me.ServiceTypeKey = detail.ServiceTypeKey;
            //me.EffectiveFromDate = detail.EffectiveFromDate;
            //me.EffectiveToDate = detail.EffectiveToDate;
            //me.DateIssued = detail.DateIssued;
            //me.AuthorizedBy = String.Copy(detail.AuthorizedBy);
            //me.MethodReceivedType = detail.MethodReceivedType;
            //me.ReceivedBy = detail.ReceivedBy;
            //me.Comments = String.Copy(me.Comments);

            //Do this FIRST
            AuthDetailSelectedItem = detail;

            LoadAvailableServiceTypeGroups();
            LoadAvailableServiceTypes();
        }

        public CollectionViewSource _AvailableServiceTypes = new CollectionViewSource();
        public ICollectionView AvailableServiceTypes
        {
            get { return _AvailableServiceTypes.View; }
        }

        public CollectionViewSource _AvailableServiceTypeGroups = new CollectionViewSource();
        public ICollectionView AvailableServiceTypeGroups
        {
            get { return _AvailableServiceTypeGroups.View; }
        }

        private void LoadAvailableServiceTypes()
        {
            // Break the binding between popup loads.  For some reason the bindings were holding on to all rows edited
            // if this isn't done and then updating all rows when one is updated.  It was tracked down to this collection.
            _AvailableServiceTypes.Source = null;
            if (AuthDetailSelectedItem == null) return;
            List<ServiceType> SvcTypeList = ServiceTypeCache.GetActiveServiceTypes();
            // Add inactive rows assigned to already saved rows.
            ServiceType Inactivesvc = null;
            if (AuthDetailSelectedItem != null && AuthDetailSelectedItem.ServiceTypeKey != null && AuthDetailSelectedItem.ServiceTypeKey > 0)
                Inactivesvc = ServiceTypeCache.GetServiceTypeFromKey((int)AuthDetailSelectedItem.ServiceTypeKey);
            if (Inactivesvc != null && !SvcTypeList.Contains(Inactivesvc))
                SvcTypeList.Insert(0, Inactivesvc);

            ServiceType emptyDisc = new ServiceType();
            emptyDisc.ServiceTypeKey = 0;
            emptyDisc.Description = " ";
            SvcTypeList.Insert(0, emptyDisc);

            _AvailableServiceTypes.Source = SvcTypeList;
            this.FilterAvailableServiceTypes();
            RaisePropertyChanged("AvailableServiceTypes");
            RaisePropertyChanged("AuthDetailSelectedItem");
        }

        public void FilterAvailableServiceTypes()
        {
            if (AvailableServiceTypes != null)
            {
                AvailableServiceTypes.Filter = new Predicate<object>((item) =>
                {
                    ServiceType st = item as ServiceType;
                    if (AuthDetailSelectedItem == null) return false;                    
                    if (st.ServiceTypeKey == 0 && st.Description.Equals(" ")) return true;
                    if ((SelectedAuthType == null)
                        || (SelectedAuthType.Type != "DISC")
                        || (SelectedAuthType.Key != st.DisciplineKey)
                       )
                    {
                        return false;
                    }
                    if (st.ServiceTypeKey <= 0) return true;
                    if (st.FinancialUseOnly) return false; // filter out FinancialUseOnly ServiceTypes

                    //XXXX - JE - need to figure out what this originally did - need a DisciplineKey defined for the currently selected 'auth' type?
                    if (st.DisciplineKey == SelectedAuthType.Key
                        //||
                        //((AuthDetailSelectedItem.AdmissionDisciplineKey <= 0 ||
                        //AuthDetailSelectedItem.AdmissionDisciplineKey == null) &&
                        //(st.DisciplineKey <= 0))
                        ) return true;

                    return false;
                });
            }

            RaisePropertyChanged("AvailableServiceTypes");
            RaisePropertyChanged("AvailableServiceTypeGroups");
            RaisePropertyChanged("AuthDetailSelectedItem");
        }

        private void LoadAvailableServiceTypeGroups()
        {
            // Break the binding between popup loads.  For some reason the bindings were holding on to all rows edited
            // if this isn't done and then updating all rows when one is updated.  It was tracked down to this collection.
            _AvailableServiceTypeGroups.Source = null;
            if (AuthDetailSelectedItem == null) return;
            List<CodeLookup> CodeLookupList = CodeLookupCache.GetCodeLookupsFromType("ServiceTypeGroup");
            // Add inactive rows assigned to already saved rows.
            CodeLookup Inactivesvc = null;
            if (AuthDetailSelectedItem != null && AuthDetailSelectedItem.ServiceTypeGroupKey != null && AuthDetailSelectedItem.ServiceTypeGroupKey > 0)
                Inactivesvc = CodeLookupCache.GetCodeLookupFromKey(AuthDetailSelectedItem.ServiceTypeGroupKey.GetValueOrDefault());
            if (Inactivesvc != null && !CodeLookupList.Contains(Inactivesvc))
                CodeLookupList.Insert(0, Inactivesvc);

            var emptyDisc = new CodeLookup();
            emptyDisc.CodeDescription = " ";
            CodeLookupList.Insert(0, emptyDisc);
            _AvailableServiceTypeGroups.Source = CodeLookupList;
            this.FilterAvailableServiceTypeGroups();
            RaisePropertyChanged("AvailableServiceTypeGroups");
            RaisePropertyChanged("AuthDetailSelectedItem");
        }

        public void FilterAvailableServiceTypeGroups()
        {
            if (AvailableServiceTypeGroups != null)
            {
                AvailableServiceTypeGroups.Filter = new Predicate<object>((item) =>
                {
                    CodeLookup st = item as CodeLookup;
                    if (AuthDetailSelectedItem == null) return false;

                    if ((SelectedAuthType == null)
                        || (SelectedAuthType.Type != "DISC")
                        //|| (SelectedAuthType.Key != st.DisciplineKey)
                       )
                    {
                        return false;
                    }
                    //if (st.ServiceTypeKey <= 0) return true;
                    //if (st.FinancialUseOnly) return false; // filter out FinancialUseOnly ServiceTypes

                    ////XXXX - JE - need to figure out what this originally did - need a DisciplineKey defined for the currently selected 'auth' type?
                    //if (st.DisciplineKey == SelectedAuthType.Key
                    //    //||
                    //    //((AuthDetailSelectedItem.AdmissionDisciplineKey <= 0 ||
                    //    //AuthDetailSelectedItem.AdmissionDisciplineKey == null) &&
                    //    //(st.DisciplineKey <= 0))
                    //    ) return true;

                    return true;
                });
            }

            RaisePropertyChanged("AvailableServiceTypes");
            RaisePropertyChanged("AvailableServiceTypeGroups");
            RaisePropertyChanged("AuthDetailSelectedItem");
        }

        public override void AuthTypeChanged(AuthType selectedAuthType)
        {
            base.AuthTypeChanged(selectedAuthType);

            this.FilterAvailableServiceTypes();
            this.FilterAvailableServiceTypeGroups();
        }
    }
}
