#region Using_Statements
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Virtuoso.Client.Offline;
using Virtuoso.Core;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
#endregion

namespace Virtuoso.Core.ViewModel
{
    public enum VMTypeSelectorEnum
    {
        SupplyOrEquip,
        GEN
    }

    public abstract class AuthDetailViewModelBase : GalaSoft.MvvmLight.ViewModelBase //, INotifyPropertyChanged
    {
        public AdmissionAuthorizationDetail AuthDetailSelectedItem { get; set; }  //TODO: change to AdmissionAuthorizationInstance or VM...
        //public AdmissionAuthorizationDetailVM AuthDetailSelectedItem { get; set; }  //TODO: change to AdmissionAuthorizationInstance or VM...

        protected AuthType SelectedAuthType { get; set; }

        private VMTypeSelectorEnum __Type;
        public VMTypeSelectorEnum VMTypeSelector { get { return __Type; } set { __Type = value; } }

        public AuthDetailViewModelBase(VMTypeSelectorEnum type)
        {
            this.VMTypeSelector = type;
            //LoadAvailableEmployees();  will load when AuthType set
        }

        public virtual void AuthTypeChanged(AuthType selectedAuthType)
        {
            SelectedAuthType = selectedAuthType;
            //LoadAvailableEmployees();  //changed to smart combo
            //if (this.AuthDetailSelectedItem != null)
            //    this.AuthDetailSelectedItem.Validate();
        }

        //private CollectionViewSource _AvailableEmployee = new CollectionViewSource();
        //public ICollectionView AvailableEmployee
        //{
        //    get { return _AvailableEmployee.View; }
        //}

        //private CollectionViewSource _AvailableEmployeeDetail = new CollectionViewSource();
        //public ICollectionView AvailableEmployeeDetail
        //{
        //    get { return _AvailableEmployeeDetail.View; }
        //}

        //private void LoadAvailableEmployees()
        //{
        //    // Break the binding between popup loads.  For some reason the bindings were holding on to all rows edited
        //    // if this isn't done and then updating all rows when one is updated.  It was tracked down to this collection.
        //    _AvailableEmployeeDetail.Source = null;
        //    if (AuthDetailSelectedItem == null) return;
        //    List<UserProfile> EmpList = UserCache.Current.GetUsers().Where(u => !u.Inactive).ToList();
        //    // Add inactive rows assigned to already saved rows.
        //    UserProfile InactiveUser = null;
        //    if (AuthDetailSelectedItem != null && AuthDetailSelectedItem.ReceivedBy != null && AuthDetailSelectedItem.ReceivedBy != Guid.Empty)
        //        InactiveUser = UserCache.Current.GetUserProfileFromUserId(AuthDetailSelectedItem.ReceivedBy);
        //    if (InactiveUser != null && !EmpList.Contains(InactiveUser))
        //        EmpList.Insert(0, InactiveUser);

        //    _AvailableEmployeeDetail.Source = EmpList;
        //}

        //TODO: refactor to interface for use here and in auth popup
        Virtuoso.Client.Core.WeakReference<DependencyObject> View { get; set; }
        public void SetViewDependencyObject(DependencyObject control)
        {
            View = new Client.Core.WeakReference<DependencyObject>(control);

            SelectFirstEditableWidget();
        }

        protected void SelectFirstEditableWidget()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (View.IsAlive != null && View.Target != null)
                    SetFocusHelper.SelectFirstEditableWidget(View.Target);
            });
        }
    }
}
