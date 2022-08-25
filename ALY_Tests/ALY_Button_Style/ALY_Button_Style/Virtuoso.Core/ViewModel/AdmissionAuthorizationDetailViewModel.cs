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
    public class AdmissionAuthorizationDetailViewModel : GalaSoft.MvvmLight.ViewModelBase //Virtuoso.Core.ViewModel.ViewModelBase
    {
        bool? _dialogResult;
        public bool HasErrors { get; set; }
        public bool? DialogResult { get { return _dialogResult; } set { _dialogResult = value; RaisePropertyChanged("DialogResult"); } }
        IPatientService Model { get; set; }

        public AdmissionAuthorizationDetailViewModel(AdmissionAuthorization selectedItem, AdmissionAuthorizationDetail _admissionAuthorizationDetail, IPatientService model)
        {
            this.Model = model;
            //this.CurrentAdmission = selectedItem.Admission;
            this.SelectedItem = selectedItem;            

            //NOTE: initialize before AuthKey
            AuthDetailViewModels = new ObservableCollection<AuthDetailViewModelBase>();
            AuthDetailViewModels.Add(new AuthDetailGeneralViewModel(_admissionAuthorizationDetail)); //, this.SelectedAuthType));
            AuthDetailViewModels.Add(new AuthDetailSupplyOrEquipViewModel(_admissionAuthorizationDetail)); //, this.SelectedAuthType));

            this.AuthDetailSelectedItem = _admissionAuthorizationDetail;

            LoadAvailableAuthTypess();
            //LoadAvailableServiceTypes();  //moved to child VM General
            //LoadAvailableEmployees();     //moved to child VM base class

            SetupCommands();
        }

        private void SetupCommands()
        {
            CancelAuthDetailCommand = new RelayCommand(
                () =>
                {
                    //CancelDetailEdit();
                    this.DialogResult = false;  //CloseDialog(false);
                });

            OK_Command = new RelayCommand(() =>
            {
                var isValid = this.AuthDetailSelectedItem.Validate();
                HasErrors = !isValid;
                if (isValid)
                    this.DialogResult = true;  //CloseDialog(true);
                else
                {
                    RaisePropertyChanged("HasErrors");
                }
            });
        }

        //private void CancelDetailEdit()
        //{
        //    if (SelectedItem != null && SelectedItem.AdmissionAuthorizationDetail != null)
        //    {
        //        SelectedItem.AdmissionAuthorizationDetail.ForEach((ad) =>
        //        {
        //            ad.CancelEditting();
        //            if (ad.IsNew)
        //            {
        //                SelectedItem.AdmissionAuthorizationDetail.Remove(ad);
        //                Model.Remove(ad);
        //            }
        //        });
        //    }
        //    //ParentViewModel.PopupDataContext = null;
        //    //AuthDetailSelectedItem = null;
        //    //this.RaisePropertyChanged("SelectedAuthDetailList");
        //}

        public RelayCommand OK_Command
        {
            get;
            protected set;
        }
        public RelayCommand CancelAuthDetailCommand
        {
            get;
            protected set;
        }

        //public Admission CurrentAdmission { get; internal set; }
        public Admission CurrentAdmission { get { return this.SelectedItem.Admission; } }

        AuthDetailViewModelBase __AuthDetailViewModel = null;
        public AuthDetailViewModelBase AuthDetailViewModel {
            get
            {
                if (__AuthDetailViewModel == null)
                {
                    if (this.IsSupplyOrEquipment)
                        __AuthDetailViewModel = AuthDetailViewModels.Where(a => a.VMTypeSelector == VMTypeSelectorEnum.SupplyOrEquip).First();
                    else
                        __AuthDetailViewModel = AuthDetailViewModels.Where(a => a.VMTypeSelector == VMTypeSelectorEnum.GEN).First();
                }
                else
                {
                    var _type = (this.IsSupplyOrEquipment) ? VMTypeSelectorEnum.SupplyOrEquip : VMTypeSelectorEnum.GEN;
                    //if (__AuthDetailViewModel.VMTypeSelector.Equals(_type) == false) //Has it changed type? E.G. from General auth type to a supply or equipment auth type?
                    if (__AuthDetailViewModel.VMTypeSelector != _type) //Has it changed type? E.G. from General auth type to a supply or equipment auth type?
                        __AuthDetailViewModel = AuthDetailViewModels.Where(a => a.VMTypeSelector == _type).First();
                }
                return __AuthDetailViewModel;
            }
        }
        public ObservableCollection<AuthDetailViewModelBase> AuthDetailViewModels { get; set; }

        public string AuthKey
        {
            get
            {
                return (SelectedAuthType == null) ? "GEN" : SelectedAuthType.AuthKey;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    SelectedAuthType = AuthTypeList.Where(ad => ad.Type == "GEN").FirstOrDefault();
                }
                else
                {
                    if (AuthTypeList != null)
                    {
                        var keyArray = value.Split('|');

                        string type = keyArray[0];
                        int key = 0;

                        if (keyArray.Count() > 1)
                        {
                            int.TryParse(keyArray[1], out key);
                        }

                        var selected = AuthTypeList.Where(ad => ((key <= 0) && (ad.Type == "GEN"))
                                                                || ((key > 0)
                                                                    && (ad.Type == type)
                                                                    && (ad.Key == key)
                                                                    )
                                                        );

                        if (selected.Count() > 0)
                        {
                            SelectedAuthType = selected.First();
                        }
                    }

                    RaisePropertyChanged("AuthKey");
                    RaisePropertyChanged("AuthDetailViewModel");
                }
            }
        }

        public AdmissionAuthorization SelectedItem { get; set; }

        public List<AuthType> AuthTypeList { get; set; }
        private AuthType selectedAuthType = null;
        public AuthType SelectedAuthType
        {
            get
            {
                return selectedAuthType;
            }
            set
            {
                selectedAuthType = value;

                if ((SelectedAuthType != null)
                    && (AuthDetailSelectedItem != null)
                   )
                {
                    if (SelectedAuthType.Type == "DISC")  //skilled dscp
                    {
                        AuthDetailSelectedItem.AdmissionDisciplineKey = SelectedAuthType.Key;
                        AuthDetailSelectedItem.AuthorizationDiscCode = null;
                    }
                    else if (SelectedAuthType.Type == "CL")  //Equipment or Supply
                    {
                        AuthDetailSelectedItem.AuthorizationType = null; //Hours or Services
                        AuthDetailSelectedItem.ServiceTypeGroupKey = null;
                        AuthDetailSelectedItem.ServiceTypeKey = null;
                        AuthDetailSelectedItem.AdmissionDisciplineKey = null;
                        AuthDetailSelectedItem.EffectiveFromDate = SelectedItem.EffectiveFromDate;
                        AuthDetailSelectedItem.EffectiveToDate = SelectedItem.EffectiveToDate;
                        AuthDetailSelectedItem.AuthorizationDiscCode = SelectedAuthType.Key;
                    }
                    else //General
                    {
                        AuthDetailSelectedItem.ServiceTypeGroupKey = null;
                        AuthDetailSelectedItem.ServiceTypeKey = null;
                        AuthDetailSelectedItem.AdmissionDisciplineKey = null;
                        AuthDetailSelectedItem.AuthorizationDiscCode = null;
                    }

                    if (SelectedAuthType.Type != "CL")
                    {
                        if (AuthDetailSelectedItem.EffectiveFromDate == null)
                        {
                            AuthDetailSelectedItem.EffectiveFromDate = SelectedItem.EffectiveFromDate;
                        }
                        if (AuthDetailSelectedItem.EffectiveToDate == null)
                        {
                            AuthDetailSelectedItem.EffectiveToDate = SelectedItem.EffectiveToDate;
                        }
                    }
                }

                //TODO JE tell child VM to do something - refilter service types drop down - only General VM will want to do this though...
                this.AuthDetailViewModel.AuthTypeChanged(this.SelectedAuthType);
                //if (AvailableServiceTypes != null)
                //{
                //    AvailableServiceTypes.Refresh();
                //}
                RaisePropertyChanged("SelectedAuthDiscipline");
                RaisePropertyChanged("AuthKey");
                RaisePropertyChanged("IsSupplyOrEquipment");
                RaisePropertyChanged("IsSupplyEquipOrHours");
            }
        }

        public bool IsSupplyEquipOrHours
        {
            get
            {
                bool isSupplyEquipOrHours = false;
                isSupplyEquipOrHours = IsSupplyOrEquipment || (AuthDetailSelectedItem.AuthorizationType == CodeLookupCache.GetKeyFromCode("AUTHTYPES", "HOURS"));
                return isSupplyEquipOrHours;
            }
        }
        private bool IsSupplyOrEquipment
        {
            get
            {
                //NOTE: CodeLookupHeader.CodeType = 'AuthorizationDiscipline' contains (<Equipment>, <Supplies>)
                //      Assumes we'll never add a Code to this lookup which is not of type supply or equipment
                return (SelectedAuthType != null) ? SelectedAuthType.Type == "CL" : false;
            }
        }

        private AdmissionAuthorizationDetail _AuthDetailSelectedItem;
        public AdmissionAuthorizationDetail AuthDetailSelectedItem
        {
            get { return _AuthDetailSelectedItem; }
            set
            {
                if (AuthDetailSelectedItem != null)
                {
                    //AuthDetailSelectedItem.PropertyChanged -= new PropertyChangedEventHandler(_AuthDetailSelectedItem_PropertyChanged);
                    ((INotifyDataErrorInfo)AuthDetailSelectedItem).ErrorsChanged -= AdmissionAuthorizationDetailViewModel_ErrorsChanged;
                }
                _AuthDetailSelectedItem = value;
                if (value != null)
                {
                    ((INotifyDataErrorInfo)AuthDetailSelectedItem).ErrorsChanged += AdmissionAuthorizationDetailViewModel_ErrorsChanged;
                    //ReLoadCollections();

                    //TODO JE the 'auth' dscp type changed - e.g. whether the auth detail is a General, Equipment, Supply, or DSCP specific auth detail - need to inform the child VM
                    AuthDetailViewModel.AuthTypeChanged(this.SelectedAuthType);
                    //LoadAvailableServiceTypes();
                }
                RaisePropertyChanged("AuthDetailSelectedItem");  //TODO: implement INotifyPropertyChanged
            }
        }

        void AdmissionAuthorizationDetailViewModel_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            this.RaisePropertyChanged("HasErrors");
        }

        //void _AuthDetailSelectedItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName == "AuthorizationType")
        //    {
        //        if (!IsSupplyEquipOrHours)
        //        {
        //            AuthDetailSelectedItem.AuthorizationAmount = Math.Round(AuthDetailSelectedItem.AuthorizationAmount);
        //        }
        //        RaisePropertyChanged("IsSupplyEquipOrHours");  //TODO: implement INotifyPropertyChanged
        //    }
        //    else if (e.PropertyName == "AuthorizationAmount")
        //    {
        //        if (!IsSupplyEquipOrHours)
        //        {
        //            AuthDetailSelectedItem.AuthorizationAmount = Math.Round(AuthDetailSelectedItem.AuthorizationAmount);
        //        }
        //    }
        //}

        public IOrderedEnumerable<AuthType> __availableAuthTypes = null;
        public IOrderedEnumerable<AuthType> AvailableAuthTypes
        {
            get { return __availableAuthTypes; }
        }

        //public CollectionViewSource _AvailableServiceTypes = new CollectionViewSource();
        //public ICollectionView AvailableServiceTypes
        //{
        //    get { return _AvailableServiceTypes.View; }
        //}

        //private void LoadAvailableServiceTypes()
        //{
        //    // Break the binding between popup loads.  For some reason the bindings were holding on to all rows edited
        //    // if this isn't done and then updating all rows when one is updated.  It was tracked down to this collection.
        //    _AvailableServiceTypes.Source = null;
        //    if (AuthDetailSelectedItem == null) return;
        //    List<ServiceType> SvcTypeList = ServiceTypeCache.GetActiveServiceTypes();
        //    // Add inactive rows assigned to already saved rows.
        //    ServiceType Inactivesvc = null;
        //    if (AuthDetailSelectedItem != null && AuthDetailSelectedItem.ServiceTypeKey != null && AuthDetailSelectedItem.ServiceTypeKey > 0)
        //        Inactivesvc = ServiceTypeCache.GetServiceTypeFromKey((int)AuthDetailSelectedItem.ServiceTypeKey);
        //    if (Inactivesvc != null && !SvcTypeList.Contains(Inactivesvc))
        //        SvcTypeList.Insert(0, Inactivesvc);

        //    ServiceType emptyDisc = new ServiceType();
        //    emptyDisc.Description = " ";
        //    SvcTypeList.Insert(0, emptyDisc);
        //    _AvailableServiceTypes.Source = SvcTypeList;
        //    FilterAvailableServiceTypes();
        //}

        private void LoadAvailableAuthTypess()
        {
            if (AuthTypeList == null)
            {
                AuthTypeList = new List<AuthType>();
            }
            else
            {
                AuthTypeList.Clear();
            }

            // if we're editing a row and the original discipline was set to <Supply> or <Equipment>, we don't want to be able to change the
            // discipline to <General> or any discipline specific discipline.
            bool loadOnlySupplyAndEquipment = (SelectedItem.AdmissionAuthorizationKey > 0)
                                                && (AuthDetailSelectedItem != null)
                                                && (AuthDetailSelectedItem.AuthorizationDiscCode.HasValue);

            // Break the binding between popup loads.  For some reason the bindings were holding on to all rows edited
            // if this isn't done and then updating all rows when one is updated.  It was tracked down to this collection.
            __availableAuthTypes = null;
            if (CurrentAdmission == null) return;

            var DiscList = DisciplineCache.GetDisciplines()
                .Where(disc => (!loadOnlySupplyAndEquipment)
                        && CurrentAdmission.AdmissionDiscipline
                    //.Where(p => p.AdmissionKey == CurrentAdmission.AdmissionKey && !p.DischargeDateTime.HasValue && !p.NotTakenDateTime.HasValue)
                        .Where(p => p.AdmissionKey == CurrentAdmission.AdmissionKey && !p.NotTakenDateTime.HasValue)  //DE 2253 need to include discharged admissions
                        .Where(ads => ads.DisciplineKey == disc.DisciplineKey)
                        .Any()
                      ).Select(d => new AuthType { Type = "DISC", Key = d.DisciplineKey, Description = d.Description })
                      .ToList();

            if (!loadOnlySupplyAndEquipment)
            {
                // Add inactive rows assigned to already saved rows.            
                Discipline Inactivedisc = null;
                if (AuthDetailSelectedItem != null && AuthDetailSelectedItem.AdmissionDisciplineKey != null && AuthDetailSelectedItem.AdmissionDisciplineKey > 0)
                    Inactivedisc = DisciplineCache.GetDisciplineFromKey((int)AuthDetailSelectedItem.AdmissionDisciplineKey);
                if (Inactivedisc != null && !DiscList.Any(d => d.Key == Inactivedisc.DisciplineKey)) // .Contains(Inactivedisc))
                {
                    DiscList.Insert(0, new AuthType { Type = "DISC", Key = Inactivedisc.DisciplineKey, Description = Inactivedisc.Description });
                }
                //Discipline emptyDisc = new Discipline();
                //emptyDisc.Description = "<General>";
                DiscList.Insert(0, new AuthType { Sequence = 0, Type = "GEN", Key = null, Description = "<General>" });
            }

            var usedCodeLookups = SelectedItem.AdmissionAuthorizationDetail
                                    .Where(aad => aad.AuthorizationDiscCode.HasValue
                                                    && ((AuthDetailSelectedItem == null)
                                                        || (AuthDetailSelectedItem.AuthorizationDiscCode != aad.AuthorizationDiscCode)
                                                       )
                                          )
                                    .Select(aad => aad.AuthorizationDiscCode);

            var codeLookups = CodeLookupCache.GetCodeLookupsFromType("AuthorizationDiscipline")
                                .Where(cl => !usedCodeLookups.Contains(cl.CodeLookupKey)
                                      )
                                .Select(c => new AuthType { Type = "CL", Key = c.CodeLookupKey, Description = c.CodeDescription });

            AuthTypeList.AddRange(DiscList.Union(codeLookups));
            __availableAuthTypes = AuthTypeList.OrderBy(ad => ad.Sequence).ThenBy(ad => ad.Description);
            FilterAvailableAuthTypes();

            if ((AuthDetailSelectedItem != null))
            {
                if (AuthDetailSelectedItem.AdmissionDisciplineKey.HasValue && (AuthDetailSelectedItem.AdmissionDisciplineKey > 0))
                {
                    SelectedAuthType = AuthTypeList.Where(ad => (ad.Type == "DISC") && (ad.Key == AuthDetailSelectedItem.AdmissionDisciplineKey)).FirstOrDefault();
                }
                else if (AuthDetailSelectedItem.AuthorizationDiscCode.HasValue && (AuthDetailSelectedItem.AuthorizationDiscCode > 0))
                {
                    SelectedAuthType = AuthTypeList.Where(ad => (ad.Type == "CL") && (ad.Key == AuthDetailSelectedItem.AuthorizationDiscCode)).FirstOrDefault();
                }
                else
                {
                    SelectedAuthType = AuthTypeList.Where(ad => (ad.Type == "GEN")).FirstOrDefault();
                }
            }
            else
            {
                SelectedAuthType = AuthTypeList.Where(ad => (ad.Type == "GEN")).FirstOrDefault();
            }
            //RaisePropertyChanged("AvailableDisciplines");
        }

        //public void FilterAvailableServiceTypes()
        //{
        //    if (AvailableServiceTypes != null)
        //    {
        //        AvailableServiceTypes.Filter = new Predicate<object>((item) =>
        //        {
        //            ServiceType st = item as ServiceType;
        //            if (AuthDetailSelectedItem == null) return false;
        //            if ((SelectedAuthDiscipline == null)
        //                || (SelectedAuthDiscipline.Type != "DISC")
        //                || (SelectedAuthDiscipline.DisciplineKey != st.DisciplineKey)
        //               )
        //            {
        //                return false;
        //            }
        //            if (st.ServiceTypeKey <= 0) return true;
        //            if (st.FinancialUseOnly) return false; // filter out FinancialUseOnly ServiceTypes
        //            if (st.DisciplineKey == SelectedAuthDiscipline.DisciplineKey
        //                //||
        //                //((AuthDetailSelectedItem.AdmissionDisciplineKey <= 0 ||
        //                //AuthDetailSelectedItem.AdmissionDisciplineKey == null) &&
        //                //(st.DisciplineKey <= 0))
        //                ) return true;

        //            return false;
        //        });
        //    }

        //    RaisePropertyChanged("AvailableServiceTypes");
        //    RaisePropertyChanged("AuthDetailSelectedItem");
        //}

        public void FilterAvailableAuthTypes()
        {
            //if ((AvailableDisciplines != null)
            //   )
            //{
            //    if (AvailableDisciplines.Filter == null)
            //    {
            //        AvailableDisciplines.Filter = new Predicate<object>((item) =>
            //        {
            //            AuthDiscipline ad = item as AuthDiscipline;

            //            if ((CurrentAdmission != null)
            //                && (AuthDetailSelectedItem != null)
            //               )
            //            {
            //                if ((ad.Type == "CL")
            //                    && (AuthDetailSelectedItem.AuthorizationDiscCode != ad.DisciplineKey)
            //                   )
            //                {
            //                    if (CurrentAdmission.AdmissionAuthorization
            //                        .Any(aa => (ad.Type == "CL")
            //                                   && aa.AdmissionAuthorizationDetail
            //                                        .Any(aad => (aad.AuthorizationDiscCode == ad.DisciplineKey)
            //                                            )
            //                            )
            //                       )
            //                    {
            //                        return false;
            //                    }
            //                }
            //            }

            //            return true;
            //        });
            //    }
            //}
        }

    }
}
