#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public class
        AdmissionAuthorizationInstanceViewModel : GalaSoft.MvvmLight.ViewModelBase
    {
        bool? _dialogResult;
        public bool HasErrors { get; set; }

        public bool? DialogResult
        {
            get { return _dialogResult; }
            set
            {
                _dialogResult = value;
                RaisePropertyChanged("DialogResult");
            }
        }

        IPatientService Model { get; set; }
        AuthMode Mode { get; set; }

        public AdmissionAuthorizationInstanceViewModel(AdmissionAuthorization selectedItem, AdmissionAuthorizationInstance _admissionAuthorizationInstance, IPatientService model, AuthMode mode)
        {
            Model = model;
            Mode = mode;
            SelectedItem = selectedItem;

            //NOTE: initialize before AuthKey
            AuthInstanceViewModels = new ObservableCollection<AuthInstanceViewModelBase>
            {
                new AuthInstanceGeneralViewModel(_admissionAuthorizationInstance, mode),
                new AuthInstanceSupplyOrEquipViewModel(_admissionAuthorizationInstance, mode)
            };

            AuthInstanceSelectedItem = _admissionAuthorizationInstance;

            LoadAvailableAuthTypes();

            SetupCommands();
        }

        private void SetupCommands()
        {
            Cancel_AuthInstance_Command = new RelayCommand(() =>
            {
                if (DialogResult.HasValue)
                {
                    return;
                }

                RemoveEdits();
                AuthInstanceViewModel.Cleanup();
                DialogResult = false;             
            }, () => DialogResult.HasValue == false);

            bool in_ok = false;
            OK_AuthInstance_Command = new RelayCommand(() =>
            {
                if (DialogResult.HasValue || in_ok)
                {
                    return;
                }

                try
                {
                    in_ok = true;

                    var isAuthInstanceSelectedItemValid = false;
                    var isDetailValid = false;

                    var isAuthInstanceSelectedItemEntityValid = AuthInstanceSelectedItem.Validate();
                    if (isAuthInstanceSelectedItemEntityValid)
                    {
                        foreach (var detail in AuthInstanceSelectedItem.AdmissionAuthorizationDetail)
                            detail.BeginEditting();

                        AuthInstanceViewModel.CreateAuthorizationDetail(AuthInstanceSelectedItem);

                        isAuthInstanceSelectedItemValid = ValidateInstance();
                        isDetailValid = ValidateDetail();
                    }

                    if (isAuthInstanceSelectedItemEntityValid && isAuthInstanceSelectedItemValid && isDetailValid)
                    {
                        if (HaveAuthCountsOnDeleteStampedDetailRows())
                        {
                            PromptToClose(
                                "WARNING: Authorization detail rows with accumulated services will be delete stamped and could cause billing changes.",
                                () =>
                                {
                                    EndEdits();
                                    in_ok = false;
                                },
                                RemoveEdits);
                        }
                        else
                        {
                            EndEdits();

                            DialogResult = true;
                        }
                    }
                    else
                    {
                        RemoveEdits();

                        HasErrors = true;
                        RaisePropertyChanged("HasErrors");
                    }
                }
                finally
                {
                    in_ok = false;
                }
            }, () => (DialogResult.HasValue == false || in_ok == false));
        }

        private void EndEdits()
        {
            foreach (var detail in AuthInstanceSelectedItem.AdmissionAuthorizationDetail)
                if (detail.IsEditting)
                {
                    detail.EndEditting();
                }
        }

        private void RemoveEdits()
        {
            //Data is invalid - user may click OK button again - need to ROLLBACK whatever occurred in CreateAuthorizationDetail(...)
            var lst = AuthInstanceSelectedItem.AdmissionAuthorizationDetail.ToList();
            foreach (var detail in lst)
            {
                if (detail.IsNew)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "NEW (removing from context) AdmissionAuthorizationDetail.  ID: {0}\tAmount: {1}\tFrom Date: {2}\tThru Date: {3}\tAuthCount: {4}\tAuthCountLastUpdate: {5}",
                        detail.AdmissionAuthorizationDetailKey, detail.AuthorizationAmount, detail.EffectiveFromDate,
                        detail.EffectiveToDate, detail.AuthCount, detail.AuthCountLastUpdate);

                    AuthInstanceSelectedItem.AdmissionAuthorizationDetail.Remove(detail);
                    Model.Remove(detail);
                }

                if (detail.IsModified)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "MODIFIED (cancel editting) AdmissionAuthorizationDetail.  ID: {0}\tAmount: {1}\tFrom Date: {2}\tThru Date: {3}\tAuthCount: {4}\tAuthCountLastUpdate: {5}",
                        detail.AdmissionAuthorizationDetailKey, detail.AuthorizationAmount, detail.EffectiveFromDate,
                        detail.EffectiveToDate, detail.AuthCount, detail.AuthCountLastUpdate);

                    detail.CancelEditting();
                }
            }
        }

        public RelayCommand OK_AuthInstance_Command { get; protected set; }

        public RelayCommand Cancel_AuthInstance_Command { get; protected set; }

        public Admission CurrentAdmission => SelectedItem.Admission;

        AuthInstanceViewModelBase __AuthInstanceViewModel;

        public AuthInstanceViewModelBase AuthInstanceViewModel
        {
            get
            {
                if (__AuthInstanceViewModel == null)
                {
                    if (IsSupplyOrEquipment)
                    {
                        __AuthInstanceViewModel = AuthInstanceViewModels.First(a => a.VMTypeSelector == VMTypeSelectorEnum.SupplyOrEquip);
                    }
                    else
                    {
                        __AuthInstanceViewModel = AuthInstanceViewModels.First(a => a.VMTypeSelector == VMTypeSelectorEnum.GEN);
                    }
                }
                else
                {
                    var _type = (IsSupplyOrEquipment) ? VMTypeSelectorEnum.SupplyOrEquip : VMTypeSelectorEnum.GEN;
                    if (__AuthInstanceViewModel.VMTypeSelector != _type) //Has it changed type? E.G. from General auth type to a supply or equipment auth type?
                    {
                        __AuthInstanceViewModel = AuthInstanceViewModels.First(a => a.VMTypeSelector == _type);
                    }
                }

                return __AuthInstanceViewModel;
            }
        }

        public ObservableCollection<AuthInstanceViewModelBase> AuthInstanceViewModels { get; set; }

        public string AuthKey
        {
            get { return (SelectedAuthType == null) ? "GEN" : SelectedAuthType.AuthKey; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    SelectedAuthType = AuthTypeList.FirstOrDefault(ad => ad.Type == "GEN");
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

                        if (selected.Any())
                        {
                            SelectedAuthType = selected.First();
                        }
                    }

                    RaisePropertyChanged("AuthKey");
                    RaisePropertyChanged("AuthInstanceViewModel");
                }
            }
        }

        public AdmissionAuthorization SelectedItem { get; set; }

        public List<AuthType> AuthTypeList { get; set; }
        private AuthType selectedAuthType;

        public AuthType SelectedAuthType
        {
            get { return selectedAuthType; }
            set
            {
                selectedAuthType = value;

                if ((SelectedAuthType != null)
                    && (AuthInstanceSelectedItem != null)
                   )
                {
                    if (SelectedAuthType.Type == "DISC") //skilled dscp
                    {
                        AuthInstanceSelectedItem.AdmissionDisciplineKey = SelectedAuthType.Key;
                        AuthInstanceSelectedItem.AuthorizationDiscCode = null;
                    }
                    else if (SelectedAuthType.Type == "CL") //Equipment or Supply
                    {
                        AuthInstanceSelectedItem.AuthorizationType = null; //Hours or Services
                        AuthInstanceSelectedItem.ServiceTypeGroupKey = null;
                        AuthInstanceSelectedItem.ServiceTypeKey = null;
                        AuthInstanceSelectedItem.AdmissionDisciplineKey = null;
                        AuthInstanceSelectedItem.EffectiveFromDate = SelectedItem.EffectiveFromDate;
                        AuthInstanceSelectedItem.EffectiveToDate = SelectedItem.EffectiveToDate;
                        AuthInstanceSelectedItem.AuthorizationDiscCode = SelectedAuthType.Key;
                    }
                    else //General
                    {
                        AuthInstanceSelectedItem.ServiceTypeGroupKey = null;
                        AuthInstanceSelectedItem.ServiceTypeKey = null;
                        AuthInstanceSelectedItem.AdmissionDisciplineKey = null;
                        AuthInstanceSelectedItem.AuthorizationDiscCode = null;
                    }

                    if (SelectedAuthType.Type != "CL")
                    {
                        if (AuthInstanceSelectedItem.EffectiveFromDate == null)
                        {
                            AuthInstanceSelectedItem.EffectiveFromDate = SelectedItem.EffectiveFromDate;
                        }

                        if (AuthInstanceSelectedItem.EffectiveToDate == null)
                        {
                            AuthInstanceSelectedItem.EffectiveToDate = SelectedItem.EffectiveToDate;
                        }
                    }
                }

                //TODO tell child VM to do something - refilter service types drop down - only General VM will want to do this though...
                AuthInstanceViewModel.AuthTypeChanged(SelectedAuthType);
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
                isSupplyEquipOrHours = IsSupplyOrEquipment || (AuthInstanceSelectedItem.AuthorizationType == CodeLookupCache.GetKeyFromCode("AUTHTYPES", "HOURS"));
                return isSupplyEquipOrHours;
            }
        }

        //NOTE: CodeLookupHeader.CodeType = 'AuthorizationDiscipline' contains (<Equipment>, <Supplies>)
        //      Assumes we'll never add a Code to this lookup which is not of type supply or equipment
        private bool IsSupplyOrEquipment => (SelectedAuthType != null) && SelectedAuthType.Type == "CL";

        private AdmissionAuthorizationInstance _AuthInstanceSelectedItem;

        public AdmissionAuthorizationInstance AuthInstanceSelectedItem
        {
            get { return _AuthInstanceSelectedItem; }
            set
            {
                if (AuthInstanceSelectedItem != null)
                {
                    ((INotifyDataErrorInfo)AuthInstanceSelectedItem).ErrorsChanged -=
                        AdmissionAuthorizationInstanceViewModel_ErrorsChanged;
                }

                _AuthInstanceSelectedItem = value;

                if (value != null)
                {
                    ((INotifyDataErrorInfo)AuthInstanceSelectedItem).ErrorsChanged +=
                        AdmissionAuthorizationInstanceViewModel_ErrorsChanged;
                    AuthInstanceViewModel.AuthTypeChanged(SelectedAuthType);
                }

                RaisePropertyChanged("AuthInstanceSelectedItem");
            }
        }

        void AdmissionAuthorizationInstanceViewModel_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            RaisePropertyChanged("HasErrors");
        }

        public IOrderedEnumerable<AuthType> __availableAuthTypes;
        public IOrderedEnumerable<AuthType> AvailableAuthTypes => __availableAuthTypes;

        private void LoadAvailableAuthTypes()
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
                                              && (AuthInstanceSelectedItem != null)
                                              && (AuthInstanceSelectedItem.AuthorizationDiscCode.HasValue);

            // Break the binding between popup loads.  For some reason the bindings were holding on to all rows edited
            // if this isn't done and then updating all rows when one is updated.  It was tracked down to this collection.
            __availableAuthTypes = null;
            if (CurrentAdmission == null)
            {
                return;
            }

            var DiscList = DisciplineCache.GetDisciplines()
                .Where(disc => (!loadOnlySupplyAndEquipment)
                               && CurrentAdmission.AdmissionDiscipline
                                   //.Where(p => p.AdmissionKey == CurrentAdmission.AdmissionKey && !p.DischargeDateTime.HasValue && !p.NotTakenDateTime.HasValue)
                                   .Where(p => p.AdmissionKey == CurrentAdmission.AdmissionKey &&
                                               !p.NotTakenDateTime
                                                   .HasValue) //DE 2253 need to include discharged admissions
                                   .Where(ads => ads.DisciplineKey == disc.DisciplineKey)
                                   .Any()
                ).Select(d => new AuthType { Type = "DISC", Key = d.DisciplineKey, Description = d.Description })
                .ToList();

            if (!loadOnlySupplyAndEquipment)
            {
                // Add inactive rows assigned to already saved rows.            
                Discipline Inactivedisc = null;
                if (AuthInstanceSelectedItem != null && AuthInstanceSelectedItem.AdmissionDisciplineKey != null &&
                    AuthInstanceSelectedItem.AdmissionDisciplineKey > 0)
                {
                    Inactivedisc =
                        DisciplineCache.GetDisciplineFromKey((int)AuthInstanceSelectedItem.AdmissionDisciplineKey);
                }

                if (Inactivedisc != null && !DiscList.Any(d => d.Key == Inactivedisc.DisciplineKey))
                {
                    DiscList.Insert(0,
                        new AuthType
                        {
                            Type = "DISC", Key = Inactivedisc.DisciplineKey, Description = Inactivedisc.Description
                        });
                }

                DiscList.Insert(0, new AuthType { Sequence = 0, Type = "GEN", Key = null, Description = "<General>" });
            }

            var usedCodeLookups = Model.Context.AdmissionAuthorizationDetails
                .Where(aad => aad.AdmissionAuthorizationKey == SelectedItem.AdmissionAuthorizationKey)
                .Where(aad => aad.DeletedDate.HasValue == false)
                .Where(aad => aad.AdmissionKey == SelectedItem.AdmissionKey)
                .Where(aad => aad.AuthorizationDiscCode.HasValue
                              && ((AuthInstanceSelectedItem == null)
                                  || (AuthInstanceSelectedItem.AuthorizationDiscCode != aad.AuthorizationDiscCode)
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

            if ((AuthInstanceSelectedItem != null))
            {
                if (AuthInstanceSelectedItem.AdmissionDisciplineKey.HasValue &&
                    (AuthInstanceSelectedItem.AdmissionDisciplineKey > 0))
                {
                    SelectedAuthType = AuthTypeList.FirstOrDefault(ad => (ad.Type == "DISC") && (ad.Key == AuthInstanceSelectedItem.AdmissionDisciplineKey));
                }
                else if (AuthInstanceSelectedItem.AuthorizationDiscCode.HasValue &&
                         (AuthInstanceSelectedItem.AuthorizationDiscCode > 0))
                {
                    SelectedAuthType = AuthTypeList.FirstOrDefault(ad => (ad.Type == "CL") && (ad.Key == AuthInstanceSelectedItem.AuthorizationDiscCode));
                }
                else
                {
                    SelectedAuthType = AuthTypeList.FirstOrDefault(ad => (ad.Type == "GEN"));
                }
            }
            else
            {
                SelectedAuthType = AuthTypeList.FirstOrDefault(ad => (ad.Type == "GEN"));
            }
        }

        private bool ValidateInstance()
        {
            bool ret = true;

            if (AuthInstanceSelectedItem.IsDistributed)
            {
                //Ensure that current AdmissionAuthorizationInstance.AuthorizationAmount = SUM(non-deleted detail rows)
                var distributedAmount = AuthInstanceSelectedItem.AdmissionAuthorizationDetail
                    .Where(aad => aad.DeletedDate.HasValue == false).Select(aad => aad.AuthorizationAmount).Sum();
                if (AuthInstanceSelectedItem.AuthorizationAmount != distributedAmount)
                {
                    ret = false;
                    string[] memberNames = { "AuthorizationAmount" };
                    AuthInstanceSelectedItem.ValidationErrors.Add(
                        new System.ComponentModel.DataAnnotations.ValidationResult(
                            "Authorization amount must equal sum of all distributed detail amounts.", memberNames));
                }

                //Ensure that AdmissionAuthorizationInstance.EffectiveFromDate == FIRST(detail).EffectiveFromDate - ordered by (detail).EffectiveFromDate
                var first_detail = AuthInstanceSelectedItem.AdmissionAuthorizationDetail
                    .Where(aad => aad.DeletedDate.HasValue == false).OrderBy(aad => aad.EffectiveFromDate)
                    .FirstOrDefault();
                if (first_detail == null)
                {
                    ret = false;
                    string[] memberNames = { "EffectiveFromDate" };
                    AuthInstanceSelectedItem.ValidationErrors.Add(
                        new System.ComponentModel.DataAnnotations.ValidationResult(
                            "Effective from date must equal effective from date of first detail.", memberNames));
                }
                else
                {
                    if (first_detail.EffectiveFromDate != AuthInstanceSelectedItem.EffectiveFromDate)
                    {
                        ret = false;
                        string[] memberNames = { "EffectiveFromDate" };
                        AuthInstanceSelectedItem.ValidationErrors.Add(
                            new System.ComponentModel.DataAnnotations.ValidationResult(
                                "Effective from date must equal effective from date of first detail.", memberNames));
                    }
                }

                //Ensure that AdmissionAuthorizationInstance.EffectiveToDate == Last(detail).EffectiveToDate - ordered by (detail).EffectiveFromDate
                var last_detail = AuthInstanceSelectedItem.AdmissionAuthorizationDetail
                    .Where(aad => aad.DeletedDate.HasValue == false).OrderBy(aad => aad.EffectiveFromDate)
                    .LastOrDefault();
                if (last_detail == null)
                {
                    ret = false;
                    string[] memberNames = { "EffectiveToDate" };
                    AuthInstanceSelectedItem.ValidationErrors.Add(
                        new System.ComponentModel.DataAnnotations.ValidationResult(
                            "Effective thru date must equal effective thru date of last detail.", memberNames));
                }
                else
                {
                    if (last_detail.EffectiveToDate != AuthInstanceSelectedItem.EffectiveToDate)
                    {
                        ret = false;
                        string[] memberNames = { "EffectiveToDate" };
                        AuthInstanceSelectedItem.ValidationErrors.Add(
                            new System.ComponentModel.DataAnnotations.ValidationResult(
                                "Effective thru date must equal effective thru date of last detail.", memberNames));
                    }
                }
            }

            return ret;
        }

        private bool ValidateDetail()
        {
            bool ret = true;
            foreach (var detail in AuthInstanceSelectedItem.AdmissionAuthorizationDetail)
            {
                var isValid = detail.Validate();
                if (!isValid)
                {
                    ret = isValid; //at least one detail in NOT valid
                }
            }

            return ret;
        }

        public void FilterAvailableAuthTypes()
        {
            
        }

        private NavigateCloseDialogWithRich CreateQuestionDialogueWithRich(String Msg, String Title, String Question, String RichText, string Header)
        {
            NavigateCloseDialogWithRich d = new NavigateCloseDialogWithRich
            {
                LayoutRoot =
                {
                    Margin = new Thickness(5)
                },
                Width = double.NaN,
                Height = double.NaN,
                ErrorMessageTextBox =
                {
                    ParagraphText = Msg
                },
                ErrorRichTextMessage =
                {
                    ParagraphText = RichText
                },
                ErrorQuestionRichTextMessage =
                {
                    ParagraphText = Question
                },
                ErrorMessageHeader = Header,
                Title = Title
            };

            return d;
        }

        private void PromptToClose(string msg, Action yesExitCode, Action noExitCode)
        {
            var d = CreateQuestionDialogueWithRich(
                "Save changes and delete stamp authorizations with accumulated services?", "WARNING", null, msg, null);
            if (d != null)
            {
                d.Closed += (s, err) =>
                {
                    if (s != null)
                    {
                        var _ret = ((ChildWindow)s).DialogResult;
                        if (_ret == true) //user chose to accept delete stamping AdmissionAuthorizationDetail rows with AuthCounts
                        {
                            yesExitCode.Invoke();
                            DialogResult = true;
                        }
                        else
                        {
                            noExitCode.Invoke();
                        }
                    }
                };
                d.Show();
            }
        }

        private bool HaveAuthCountsOnDeleteStampedDetailRows()
        {
            //User Story 40030:Edit of Authorization detail header dates
            //2.IF the accumulated > 0 on a row to be delete stamped, present a message to the user that requires acknowledgement telling 
            //the user that rows are going to be delete stamped. 'Authorization details that accumulated services are going to be delete 
            //stamped and could cause billing changes.'

            var total_modified_delete_stamped_rows_with_auth_counts = 0m;

            foreach (var detail in AuthInstanceSelectedItem.AdmissionAuthorizationDetail)
                if (detail.DeletedDate.HasValue && detail.IsModified && detail.AuthCount.GetValueOrDefault() > 0)
                {
                    total_modified_delete_stamped_rows_with_auth_counts += detail.AuthCount.GetValueOrDefault();
                }

            return (total_modified_delete_stamped_rows_with_auth_counts > 0);
        }

        public override void Cleanup()
        {
            if (Model != null)
            {
                Model = null;
                SelectedItem = null;
                AuthInstanceSelectedItem = null;

                foreach (var aivm in AuthInstanceViewModels)
                    aivm.Cleanup();

                AuthInstanceViewModels.Clear();
            }

            base.Cleanup();
        }
    }
}