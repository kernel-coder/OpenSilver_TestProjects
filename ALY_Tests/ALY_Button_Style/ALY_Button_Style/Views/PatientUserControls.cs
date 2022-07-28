#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
//using Virtuoso.Client.Offline;
//using Virtuoso.Controls;
using Virtuoso.Core;
//using Virtuoso.Core.Cache;
//using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Framework;
//using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
//using Virtuoso.Core.View;
//using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
//using Virtuoso.Services.Authentication;
//using static Virtuoso.Core.Controls.AniMan.AniManForWoundLocation;

#endregion

namespace Virtuoso.Maintenance.Controls
{
    public class ItemSourceItem
    {
        public string ItemName { get; set; }
    }

    //public class
    //    PatientAdvancedDirectiveUserControlBase : ChildControlBase<PatientAdvancedDirectiveUserControl,
    //        PatientAdvancedDirective>
    //{
    //    public RelayCommand AddAdvancedDirective_Command { get; protected set; }
    //    public RelayCommand PatientContact1Details_Command { get; protected set; }
    //    public RelayCommand PatientContact2Details_Command { get; protected set; }
    //    public RelayCommand PatientContact3Details_Command { get; protected set; }

    //    void OnPatientViewModelSaved(IParentViewModel parentVM)
    //    {
    //        if (ParentViewModel != null && ParentViewModel.Equals(parentVM))
    //        {
    //            RefreshPatientAdvancedDirectives();
    //        }

    //        if (CurrentPatient != null)
    //        {
    //            bool hasPOLST = (CurrentPatient.ActiveAdvancedDirectivesOfType("POLST") == null) ? false : true;
    //            Messenger.Default.Send(hasPOLST,
    //                string.Format("AdvanceCarePlanChangedPOLSTChanged{0}",
    //                    CurrentPatient.PatientKey.ToString().Trim()));
    //        }
    //    }

    //    public PatientAdvancedDirectiveUserControlBase()
    //    {
    //        Messenger.Default.Register<IParentViewModel>(this, Constants.DomainEvents.PatientViewModelSaved,
    //            OnPatientViewModelSaved);
    //        PopupDataTemplate = "PatientAdvancedDirectivePopupDataTemplate";
    //        OKPressed += OnOKPressed;
    //        EditPressed += OnEditPressed;
    //        CancelPressed += OnCancelPressed;
    //        ItemSelected += Update_ItemSelected;

    //        AddAdvancedDirective_Command = new RelayCommand(() =>
    //        {
    //            PatientAdvancedDirective newPAD = new PatientAdvancedDirective();

    //            if (PatientAdvancedDirectiveTypePickListKey != 0)
    //            {
    //                newPAD.AdvancedDirectiveType = PatientAdvancedDirectiveTypePickListKey;
    //            }

    //            newPAD.CurrentPatient = CurrentPatient;
    //            Admission mostRecentAdmission = ((CurrentPatient != null) && (CurrentPatient.Admission != null))
    //                ? CurrentPatient.Admission.Where(a => a.HistoryKey == null).OrderByDescending(a => a.ReferDateTime)
    //                    .FirstOrDefault()
    //                : null;

    //            if (mostRecentAdmission != null)
    //            {
    //                AdmissionPhysician ap = mostRecentAdmission.AdmissionPhysician
    //                    .Where(p => p.Inactive == false)
    //                    .Where(p => p.Signing)
    //                    .Where(p =>
    //                        (p.SigningEffectiveFromDate.HasValue &&
    //                         p.SigningEffectiveFromDate.Value.Date <= DateTime.Now.Date) &&
    //                        ((p.SigningEffectiveThruDate.HasValue == false) || (p.SigningEffectiveThruDate.HasValue &&
    //                            (p.SigningEffectiveThruDate.Value.Date > DateTime.Now.Date)))
    //                    ).FirstOrDefault();
    //                if (ap != null)
    //                {
    //                    newPAD.SigningPhysicianKey = ap.PhysicianKey;
    //                }
    //            }

    //            newPAD.EffectiveDate = DateTime.Today;
    //            PatientAddress pa = CurrentPatient?.MainAddress(null);
    //            if ((CurrentPatient != null) && (pa != null))
    //            {
    //                newPAD.RecordedStateCode = pa.StateCode;
    //            }

    //            newPAD.Expand = Expand;

    //            ItemsSource.Add(newPAD);
    //            RefreshPatientAdvancedDirectives();

    //            SelectedItem = newPAD;
    //            if (SelectedItem != null)
    //            {
    //                SelectedItem.RaiseChanged();
    //                SelectedItem.BeginEditting();
    //            }

    //            IsEdit = true;

    //            ParentViewModel.PopupDataContext = this;
    //            if ((PopupDataTemplate is string && String.IsNullOrEmpty(PopupDataTemplate)) ||
    //                (PopupDataTemplate == null))
    //            {
    //                SetFocusHelper.SelectFirstEditableWidget(this);
    //            }
    //        }, () => IsOnline && AllowAdd());

    //        PatientContact1Details_Command = new RelayCommand(() =>
    //        {
    //            if (SelectedItem == null)
    //            {
    //                return;
    //            }

    //            ShowPatientContactDetails(SelectedItem.PatientContact1Key);
    //        });
    //        PatientContact2Details_Command = new RelayCommand(() =>
    //        {
    //            if (SelectedItem == null)
    //            {
    //                return;
    //            }

    //            ShowPatientContactDetails(SelectedItem.PatientContact2Key);
    //        });
    //        PatientContact3Details_Command = new RelayCommand(() =>
    //        {
    //            if (SelectedItem == null)
    //            {
    //                return;
    //            }

    //            ShowPatientContactDetails(SelectedItem.PatientContact3Key);
    //        });
    //    }

    //    public override void Cleanup()
    //    {
    //        Messenger.Default.Unregister(this);
    //        OKPressed -= OnOKPressed;
    //        EditPressed -= OnEditPressed;
    //        CancelPressed -= OnCancelPressed;
    //        ItemSelected -= Update_ItemSelected;
    //        if (_PatientAdvancedDirectives != null)
    //        {
    //            _PatientAdvancedDirectives.Source = null;
    //            _PatientAdvancedDirectives.Filter -= _PatientAdvancedDirectives_Filter;
    //            _PatientAdvancedDirectives = null;
    //        }

    //        base.Cleanup();
    //    }

    //    private void ShowPatientContactDetails(int? patientContactKey)
    //    {
    //        if (patientContactKey == null)
    //        {
    //            return;
    //        }

    //        if (patientContactKey <= 0)
    //        {
    //            return;
    //        }

    //        if (CurrentPatient == null)
    //        {
    //            return;
    //        }

    //        if (CurrentPatient.PatientContact == null)
    //        {
    //            return;
    //        }

    //        PatientContact pc = CurrentPatient.PatientContact.FirstOrDefault(c => c.PatientContactKey == patientContactKey);
    //        PatientContactDetailsDialog cw = new PatientContactDetailsDialog(pc);
    //        cw.Show();
    //    }

    //    public Patient CurrentPatient
    //    {
    //        get { return (Patient)GetValue(CurrentPatientProperty); }
    //        set { SetValue(CurrentPatientProperty, value); }
    //    }

    //    public static readonly DependencyProperty CurrentPatientProperty =
    //        DependencyProperty.Register("CurrentPatient", typeof(Patient),
    //            typeof(PatientAdvancedDirectiveUserControlBase), new PropertyMetadata(null, CurrentPatientChanged));

    //    private static void CurrentPatientChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    //    {
    //        PatientAdvancedDirectiveUserControlBase me = sender as PatientAdvancedDirectiveUserControlBase;
    //        if (me == null)
    //        {
    //            return;
    //        }

    //        if (me.ItemsSource != null)
    //        {
    //            foreach (PatientAdvancedDirective pad in me.ItemsSource)
    //                pad.CurrentPatient = me.CurrentPatient;
    //        }
    //    }

    //    private bool firstRefresh;

    //    private void Update_ItemSelected(object sender, EventArgs e)
    //    {
    //        if ((ItemsSource != null) && (firstRefresh == false))
    //        {
    //            firstRefresh = true;
    //            RefreshPatientAdvancedDirectives();
    //        }
    //    }

    //    private void RefreshPatientAdvancedDirectives()
    //    {
    //        PatientAdvancedDirective selectedItem = SelectedItem;
    //        if (ItemsSource != null)
    //        {
    //            foreach (PatientAdvancedDirective pad in ItemsSource)
    //                pad.CurrentPatient = CurrentPatient;
    //        }

    //        if (_PatientAdvancedDirectives == null)
    //        {
    //            _PatientAdvancedDirectives = new CollectionViewSource();
    //            _PatientAdvancedDirectives.SortDescriptions.Add(new SortDescription("EffectiveDate",
    //                ListSortDirection.Descending));
    //            _PatientAdvancedDirectives.Source = ItemsSource;

    //            _PatientAdvancedDirectives.Filter += _PatientAdvancedDirectives_Filter;
    //        }

    //        if (selectedItem == null)
    //        {
    //            _PatientAdvancedDirectives.View.MoveCurrentToFirst();
    //        }
    //        else
    //        {
    //            _PatientAdvancedDirectives.View.MoveCurrentTo(selectedItem);
    //        }

    //        PatientAdvancedDirectives.Refresh();
    //        RaisePropertyChanged("PatientAdvancedDirectives");
    //    }

    //    private void _PatientAdvancedDirectives_Filter(object s, FilterEventArgs args)
    //    {
    //        PatientAdvancedDirective pad = args.Item as PatientAdvancedDirective;
    //        if (pad.HistoryKey != null)
    //        {
    //            args.Accepted = false;
    //            return;
    //        }

    //        if (PatientAdvancedDirectiveTypePickListKey == -1)
    //        {
    //            args.Accepted = true;
    //            return;
    //        }

    //        if (PatientAdvancedDirectiveTypePickListKey <= 0)
    //        {
    //            // Insure Advanced Directive is currently active
    //            if (DateTime.Compare(((DateTime)pad.EffectiveDate).Date, DateTime.Today) > 0)
    //            {
    //                args.Accepted = false;
    //                return;
    //            }

    //            if (pad.ExpirationDate != null)
    //            {
    //                if (DateTime.Compare(((DateTime)pad.ExpirationDate).Date, DateTime.Today) < 0)
    //                {
    //                    args.Accepted = false;
    //                    return;
    //                }
    //            }

    //            if (pad.Inactive)
    //            {
    //                args.Accepted = false;
    //                return;
    //            } // Filter out inactive advance directives - caurni00 - 16889

    //            args.Accepted = true;
    //            return;
    //        }

    //        args.Accepted = (pad.AdvancedDirectiveType == PatientAdvancedDirectiveTypePickListKey) ? true : false;
    //    }

    //    private bool _Expand;

    //    public bool Expand
    //    {
    //        get { return _Expand; }
    //        set
    //        {
    //            _Expand = value;
    //            if (ItemsSource == null)
    //            {
    //                return;
    //            }

    //            foreach (PatientAdvancedDirective pad in ItemsSource) pad.Expand = _Expand;
    //            RefreshPatientAdvancedDirectives();
    //        }
    //    }

    //    public List<CodeLookup> PatientAdvancedDirectiveTypePickList
    //    {
    //        get
    //        {
    //            int i = 0;
    //            List<CodeLookup> r = new List<CodeLookup>();
    //            r.Insert(i++,
    //                new CodeLookup
    //                { CodeLookupKey = 0, Code = "Current", CodeDescription = "View Current Advance Directives" });
    //            List<CodeLookup> cl = CodeLookupCache.GetCodeLookupsFromType("AdvancedDirectives");
    //            if (cl != null)
    //            {
    //                foreach (CodeLookup c in cl)
    //                    r.Insert(i++,
    //                        new CodeLookup
    //                        {
    //                            CodeLookupKey = c.CodeLookupKey,
    //                            Code = c.Code,
    //                            CodeDescription = "View History of " + c.CodeDescription + ""
    //                        });
    //            }

    //            r.Insert(i++,
    //                new CodeLookup
    //                {
    //                    CodeLookupKey = -1,
    //                    Code = "All",
    //                    CodeDescription = "View History of All Advance Directives"
    //                });
    //            return r;
    //        }
    //    }

    //    private int _PatientAdvancedDirectiveTypePickListKey;

    //    public int PatientAdvancedDirectiveTypePickListKey
    //    {
    //        get { return _PatientAdvancedDirectiveTypePickListKey; }
    //        set
    //        {
    //            _PatientAdvancedDirectiveTypePickListKey = value;
    //            RefreshPatientAdvancedDirectives();
    //            RaisePropertyChanged("PatientAdvancedDirectiveTypePickListKey");
    //        }
    //    }

    //    public string PatientAdvancedDirectiveTypePickListCode
    //    {
    //        get
    //        {
    //            CodeLookup cl = PatientAdvancedDirectiveTypePickList
    //                .Where(c => c.CodeLookupKey == PatientAdvancedDirectiveTypePickListKey).FirstOrDefault();
    //            return (cl == null) ? null : cl.Code.ToLower();
    //        }
    //    }

    //    private CollectionViewSource _PatientAdvancedDirectives;

    //    public ICollectionView PatientAdvancedDirectives => _PatientAdvancedDirectives?.View;

    //    public void OnEditPressed(object sender, UserControlBaseEventArgs<PatientAdvancedDirective> e)
    //    {
    //        e.Entity.CurrentPatient = CurrentPatient;
    //        e.Entity.CanFullEdit = (e.Entity.ExpirationDate == null) ? true : false;
    //        SelectedItem?.RaiseChanged();

    //        ParentViewModel.PopupDataContext = this;
    //    }

    //    public void OnOKPressed(object sender, UserControlBaseEventArgs<PatientAdvancedDirective> e)
    //    {
    //        if (((e.Entity.AdvancedDirectiveTypeCode.ToLower() == "dnr") ||
    //             (e.Entity.AdvancedDirectiveTypeCode.ToLower() == "communitydnr")) == false)
    //        {
    //            e.Entity.SigningPhysicianKey = null;
    //        }

    //        SelectedItem.EffectiveDatePatientContacts();

    //        ParentViewModel.PopupDataContext = null;
    //        if ((SelectedItem != null) && (CurrentPatient != null) &&
    //            (CurrentPatient.PatientAdvancedDirective != null) &&
    //            (CurrentPatient.PatientAdvancedDirective.Contains(SelectedItem) == false))
    //        {
    //            CurrentPatient.PatientAdvancedDirective.Add(SelectedItem);
    //        }
    //    }

    //    public void OnCancelPressed(object sender, UserControlBaseEventArgs<PatientAdvancedDirective> e)
    //    {
    //        RefreshPatientAdvancedDirectives();
    //        if (_PatientAdvancedDirectives != null)
    //        {
    //            _PatientAdvancedDirectives.View.MoveCurrentToFirst();
    //        }

    //        ParentViewModel.PopupDataContext = null;
    //    }

    //    public IPatientService Model
    //    {
    //        get { return (IPatientService)GetValue(ModelProperty); }
    //        set { SetValue(ModelProperty, value); }
    //    }

    //    public static readonly DependencyProperty ModelProperty =
    //        DependencyProperty.Register("Model", typeof(IPatientService),
    //            typeof(PatientAdvancedDirectiveUserControlBase), null);

    //    public override void RemoveFromModel(PatientAdvancedDirective entity)
    //    {
    //        if (Model == null)
    //        {
    //            throw new ArgumentNullException("Model", "Model is NULL");
    //        }

    //        Model.Remove(entity);
    //    }

    //    public override void SaveModel(UserControlBaseCommandType command)
    //    {
    //        if (Model == null)
    //        {
    //            throw new ArgumentNullException("Model", "Model is NULL");
    //        }
    //    }
    //}


    //public class PatientAddressUserControlBase : ChildControlBase<PatientAddressUserControl, PatientAddress>
    //{
    //    public bool CanEditAddress
    //    {
    //        get
    //        {
    //            bool canEdit = false;

    //            if (IsEdit
    //                && (SelectedItem != null)
    //               )
    //            {
    //                canEdit = SelectedItem.CanEditAddress;
    //            }

    //            return canEdit;
    //        }
    //    }

    //    public int? Type
    //    {
    //        get
    //        {
    //            if (SelectedItem == null)
    //            {
    //                return null;
    //            }

    //            return SelectedItem.Type;
    //        }
    //        set
    //        {
    //            if (SelectedItem != null)
    //            {
    //                SelectedItem.Type = value;
    //                RaisePropertyChanged("Type");
    //                RaisePropertyChanged("CanEditAddress");
    //            }
    //        }
    //    }

    //    public PatientAddressUserControlBase()
    //    {
    //        IsEditChanged += PatientAddressUserControlBase_IsEditChanged;
    //        PropertyChanged += PatientAddressUserControlBase_PropertyChanged;
    //        //SortOrder = "-CurrentAddress|PatientAddressKey";
    //        // don't double tap ProcessFilteredItems on initial load as we remove the SortOrder - refer to OnProcessFilteredItems() method below
    //        ProcessFilteredItemsOnSortOrderPropertyChanged = false;
    //    }

    //    void PatientAddressUserControlBase_PropertyChanged(object sender, PropertyChangedEventArgs e)
    //    {
    //        if (e.PropertyName == "SelectedItem")
    //        {
    //            RaisePropertyChanged("Type");
    //            RaisePropertyChanged("CanEditAddress");
    //        }
    //    }

    //    void PatientAddressUserControlBase_IsEditChanged(object sender, UserControlBaseEventArgs<PatientAddress> e)
    //    {
    //        RaisePropertyChanged("CanEditAddress");
    //    }

    //    private bool InitialFilter = true;

    //    public override void OnPreProcessFilteredItems()
    //    {

    //    }

    //    public override void OnProcessFilteredItems()
    //    {
    //        // Remove sorting after the initial load puts the Current addresss at the top of the list
    //        // Otherwise we get white screens on the reordering of the FilteredItemsSource w.r.t CurrentItem/SelectedItem
    //        if (InitialFilter == false)
    //        {
    //            return;
    //        }

    //        InitialFilter = false;
    //        if ((FilteredItemsSource == null) || (FilteredItemsSource.SourceCollection == null))
    //        {
    //            return;
    //        }

    //        PatientAddress pa = FilteredItemsSource.SourceCollection.Cast<PatientAddress>().AsQueryable()
    //            .Where(p => p.CurrentAddress == true).FirstOrDefault();
    //        if (pa != null)
    //        {
    //            FilteredItemsSource.MoveCurrentTo(pa);
    //        }

    //        SelectedItem = pa;
    //    }

    //    public override void Cleanup()
    //    {
    //        base.Cleanup();

    //        IsEditChanged -= PatientAddressUserControlBase_IsEditChanged;
    //        PropertyChanged -= PatientAddressUserControlBase_PropertyChanged;
    //    }

    //    public IPatientService Model
    //    {
    //        get { return (IPatientService)GetValue(ModelProperty); }
    //        set { SetValue(ModelProperty, value); }
    //    }

    //    public static readonly DependencyProperty ModelProperty =
    //        DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientAddressUserControl), null);

    //    public override void RemoveFromModel(PatientAddress entity)
    //    {
    //        if (Model == null)
    //        {
    //            throw new ArgumentNullException("Model", "Model is NULL");
    //        }

    //        Model.Remove(entity);
    //    }

    //    public override Task<bool> ValidateAsync()
    //    {
    //        Task<bool> result = Model.ValidatePatientAddressAsync(SelectedItem);
    //        return result;
    //    }


    //    public override bool Validate()
    //    {
    //        bool AllValid = true;

    //        SelectedItem.ValidationErrors.Clear();
    //        AllValid = SelectedItem.Validate();

    //        if (SelectedItem.EffectiveFromDate == null)
    //        {
    //            SelectedItem.ValidationErrors.Add(new ValidationResult("Effective From date is required.",
    //                new[] { "EffectiveFromDate" }));
    //            AllValid = false;
    //        }

    //        if (SelectedItem.IsAddressVerificationGeoCode)
    //        {
    //            if (SelectedItem.VerifiedDate == DateTime.MinValue)
    //            {
    //                SelectedItem.VerifiedDate = null;
    //            }

    //            if (SelectedItem.Longitude == 0)
    //            {
    //                SelectedItem.Longitude = null;
    //            }

    //            if (SelectedItem.Latitude == 0)
    //            {
    //                SelectedItem.Latitude = null;
    //            }

    //            if (SelectedItem.Longitude.HasValue == false)
    //            {
    //                SelectedItem.ValidationErrors.Add(new ValidationResult("The Longitude field is required.",
    //                    new[] { "Longitude" }));
    //                AllValid = false;
    //            }
    //            else if (SelectedItem.Longitude.HasValue &&
    //                     ((SelectedItem.Longitude > 180) || (SelectedItem.Longitude < -180)))
    //            {
    //                SelectedItem.ValidationErrors.Add(new ValidationResult(
    //                    "Invalid Longitude field format. Must be between -180.000000 and 180..000000",
    //                    new[] { "Longitude" }));
    //                AllValid = false;
    //            }

    //            if (SelectedItem.Latitude.HasValue == false)
    //            {
    //                SelectedItem.ValidationErrors.Add(new ValidationResult("The Latitude field is required.",
    //                    new[] { "Latitude" }));
    //                AllValid = false;
    //            }
    //            else if (SelectedItem.Latitude.HasValue &&
    //                     ((SelectedItem.Latitude > 90) || (SelectedItem.Latitude < -90)))
    //            {
    //                SelectedItem.ValidationErrors.Add(new ValidationResult(
    //                    "Invalid Latitude field format. Must be between -90.000000 and 90.000000",
    //                    new[] { "Latitude" }));
    //                AllValid = false;
    //            }

    //            if (SelectedItem.VerifiedDate == null)
    //            {
    //                SelectedItem.ValidationErrors.Add(
    //                    new ValidationResult("The Long/Lat Verified Date field is required.",
    //                        new[] { "VerifiedDate" }));
    //                AllValid = false;
    //            }
    //            else if (SelectedItem.VerifiedDate != null)
    //            {
    //                SelectedItem.VerifiedDate = ((DateTime)SelectedItem.VerifiedDate).Date;
    //                if (SelectedItem.VerifiedDate > DateTime.Today.Date)
    //                {
    //                    SelectedItem.ValidationErrors.Add(new ValidationResult(
    //                        "The Long/Lat Verified Date cannot be a future date.", new[] { "VerifiedDate" }));
    //                    AllValid = false;
    //                }
    //            }
    //        }

    //        return AllValid;
    //    }

    //    public override void SaveModel(UserControlBaseCommandType command)
    //    {
    //        // SAVE - regardless of whethe command = OK or CANCEL...
    //        if (Model == null)
    //        {
    //            throw new ArgumentNullException("Model", "Model is NULL");
    //        }

    //        //Child control can now do intermediate saves to the database!
    //        //However - Model ensures save only occurs if there are no open edits or invalid entities.
    //        //This should be OK for everything except when adding a parent entity...especially when the detail 
    //        //tab is still in open edit mode!


    //        //On the current crop of maintenance screens - not certain that we want to SAVE...because not certain how 
    //        //'pending' saves and status of pending saves will be communicated to UI.  If child tab/control does save,
    //        //but it is pending - then how is parent UI to know this?
    //    }
    //}

    //public class PatientDiagnosisHistoryItem
    //{
    //    public string Code { get; set; }
    //    public string Description { get; set; }
    //    public int DiagnosisStatus { get; set; } // 0 = current, 2 = discontinued 
    //    public string ServiceLines { get; set; }
    //    public int Version { get; set; }
    //    public bool Diagnosis { get; set; }
    //}

    //public class PatientDiagnosisUserControlBase : ChildControlBase<PatientDiagnosisUserControl, AdmissionDiagnosis>
    //{
    //    public PatientDiagnosisUserControlBase()
    //    {
    //        PatientDiagnosisCMHistoryItemsSource = null;
    //        PatientDiagnosisPCSHistoryItemsSource = null;
    //        if (TenantSettingsCache.Current.TenantSettingICDVersionDefault == 9)
    //        {
    //            ICDViewVersion9 = true;
    //        }
    //        else
    //        {
    //            ICDViewVersion10 = true;
    //        }
    //    }

    //    public override void Cleanup()
    //    {
    //        PatientDiagnosisCMHistoryItemsSource = null;
    //        PatientDiagnosisPCSHistoryItemsSource = null;
    //        base.Cleanup();
    //    }

    //    public List<PatientDiagnosisHistoryItem> PatientDiagnosisCMHistoryItemsSource { get; set; }
    //    public List<PatientDiagnosisHistoryItem> PatientDiagnosisPCSHistoryItemsSource { get; set; }

    //    private void SetupDiagnosisHistory()
    //    {
    //        SetupDiagnosisCMHistory();
    //        SetupDiagnosisPCSHistory();
    //    }

    //    private void SetupDiagnosisCMHistory()
    //    {
    //        if ((Patient == null) || (Patient.AdmissionDiagnosis == null))
    //        {
    //            PatientDiagnosisCMHistoryItemsSource = null;
    //            RaisePropertyChanged("PatientDiagnosisCMHistoryItemsSource");
    //            return;
    //        }

    //        // Ignore dummy ICD and any removed diagnosis
    //        List<PatientDiagnosisHistoryItem> dhList = new List<PatientDiagnosisHistoryItem>();
    //        // start with active list
    //        int version = (ICDViewVersion == ICDViewVersionType.ICD9)
    //            ? 10
    //            : ((ICDViewVersion == ICDViewVersionType.ICD10) ? 9 : 0);
    //        List<AdmissionDiagnosis> _diagnosis = Patient.AdmissionDiagnosis.Where(a =>
    //                (a.Version != version) && a.Diagnosis && (a.Superceded == false) && (a.RemovedDate == null) &&
    //                (a.Code != "000.00") &&
    //                (a.DiagnosisEndDate == null || ((DateTime)a.DiagnosisEndDate).Date >= DateTime.Today.Date))
    //            .OrderBy(a => a.Code).ThenBy(a => a.Version).ToList();
    //        foreach (AdmissionDiagnosis ad in _diagnosis)
    //            if (IsDiagnosisInList(dhList, ad) == false)
    //            {
    //                dhList.Add(new PatientDiagnosisHistoryItem
    //                {
    //                    Code = ad.Code,
    //                    Description = ad.Description,
    //                    DiagnosisStatus = 0,
    //                    ServiceLines = SetDiagnosisServiceLines(ad),
    //                    Version = ad.Version,
    //                    Diagnosis = ad.Diagnosis
    //                });
    //            }

    //        if (ICDViewCurrentAll == ViewAllDiagnosis)
    //        {
    //            // append inactive list
    //            _diagnosis = Patient.AdmissionDiagnosis.Where(a =>
    //                    (a.Version != version) && a.Diagnosis && (a.Superceded == false) && (a.RemovedDate == null) &&
    //                    (a.Code != "000.00") && (a.DiagnosisEndDate != null &&
    //                                             ((DateTime)a.DiagnosisEndDate).Date < DateTime.Today.Date))
    //                .OrderBy(a => a.Code).ThenBy(a => a.Version).ToList();
    //            foreach (AdmissionDiagnosis ad in _diagnosis)
    //                if (IsDiagnosisInList(dhList, ad) == false)
    //                {
    //                    dhList.Add(new PatientDiagnosisHistoryItem
    //                    {
    //                        Code = ad.Code,
    //                        Description = ad.Description,
    //                        DiagnosisStatus = 2,
    //                        ServiceLines = null,
    //                        Version = ad.Version,
    //                        Diagnosis = ad.Diagnosis
    //                    });
    //                }
    //        }

    //        PatientDiagnosisCMHistoryItemsSource = dhList;
    //        RaisePropertyChanged("PatientDiagnosisCMHistoryItemsSource");
    //    }

    //    private void SetupDiagnosisPCSHistory()
    //    {
    //        if ((Patient == null) || (Patient.AdmissionDiagnosis == null))
    //        {
    //            PatientDiagnosisPCSHistoryItemsSource = null;
    //            RaisePropertyChanged("PatientDiagnosisPCSHistoryItemsSource");
    //            return;
    //        }

    //        // Ignore dummy ICD and any removed diagnosis
    //        List<PatientDiagnosisHistoryItem> dhList = new List<PatientDiagnosisHistoryItem>();
    //        // start with active list
    //        int version = (ICDViewVersion == ICDViewVersionType.ICD9)
    //            ? 10
    //            : ((ICDViewVersion == ICDViewVersionType.ICD10) ? 9 : 0);
    //        List<AdmissionDiagnosis> _diagnosis = Patient.AdmissionDiagnosis.Where(a =>
    //                (a.Version != version) && (a.Diagnosis == false) && (a.Superceded == false) &&
    //                (a.RemovedDate == null) && (a.Code != "000.00") && (a.DiagnosisEndDate == null ||
    //                                                                    ((DateTime)a.DiagnosisEndDate).Date >=
    //                                                                    DateTime.Today.Date))
    //            .OrderBy(a => a.DiagnosisStartDate).ThenBy(a => a.Code).ThenBy(a => a.Version).ToList();
    //        foreach (AdmissionDiagnosis ad in _diagnosis)
    //            if (IsDiagnosisInList(dhList, ad) == false)
    //            {
    //                dhList.Add(new PatientDiagnosisHistoryItem
    //                {
    //                    Code = ad.Code,
    //                    Description = ad.Description,
    //                    DiagnosisStatus = 0,
    //                    ServiceLines = SetDiagnosisServiceLines(ad),
    //                    Version = ad.Version,
    //                    Diagnosis = ad.Diagnosis
    //                });
    //            }

    //        if (ICDViewCurrentAll == ViewAllDiagnosis)
    //        {
    //            // append inactive list
    //            _diagnosis = Patient.AdmissionDiagnosis.Where(a =>
    //                    (a.Version != version) && (a.Diagnosis == false) && (a.Superceded == false) &&
    //                    (a.RemovedDate == null) && (a.Code != "000.00") && (a.DiagnosisEndDate != null &&
    //                                                                        ((DateTime)a.DiagnosisEndDate).Date <
    //                                                                        DateTime.Today.Date))
    //                .OrderBy(a => a.DiagnosisStartDate).ThenBy(a => a.Code).ThenBy(a => a.Version).ToList();
    //            foreach (AdmissionDiagnosis ad in _diagnosis)
    //                if (IsDiagnosisInList(dhList, ad) == false)
    //                {
    //                    dhList.Add(new PatientDiagnosisHistoryItem
    //                    {
    //                        Code = ad.Code,
    //                        Description = ad.Description,
    //                        DiagnosisStatus = 2,
    //                        ServiceLines = null,
    //                        Version = ad.Version,
    //                        Diagnosis = ad.Diagnosis
    //                    });
    //                }
    //        }

    //        PatientDiagnosisPCSHistoryItemsSource = dhList;
    //        RaisePropertyChanged("PatientDiagnosisPCSHistoryItemsSource");
    //    }

    //    private bool IsDiagnosisInList(List<PatientDiagnosisHistoryItem> list, AdmissionDiagnosis ad)
    //    {
    //        if ((list == null) || (list.Any() == false))
    //        {
    //            return false;
    //        }

    //        if (string.IsNullOrWhiteSpace(ad.Code))
    //        {
    //            return true;
    //        }

    //        foreach (PatientDiagnosisHistoryItem i in list)
    //            if ((i.Code.ToLower() == ad.Code.ToLower()) && (i.Version == ad.Version) &&
    //                (i.Diagnosis == ad.Diagnosis))
    //            {
    //                return true;
    //            }

    //        return false;
    //    }

    //    private string SetDiagnosisServiceLines(AdmissionDiagnosis ad)
    //    {
    //        if ((Patient == null) || (Patient.Admission == null) || (Patient.AdmissionDiagnosis == null))
    //        {
    //            return "none";
    //        }

    //        List<AdmissionDiagnosis> _ActiveInServiceLines = Patient.AdmissionDiagnosis.Where(a =>
    //                (a.Superceded == false) && a.AdmissionActive && (a.RemovedDate == null) &&
    //                (a.ServiceLine != null) &&
    //                (a.Code == ad.Code) &&
    //                (a.DiagnosisEndDate == null || ((DateTime)a.DiagnosisEndDate).Date >= DateTime.Today.Date))
    //            .OrderBy(a => a.ServiceLine).ToList();
    //        if (_ActiveInServiceLines.Any() == false)
    //        {
    //            return "none";
    //        }

    //        string sl = null;
    //        foreach (AdmissionDiagnosis asl in _ActiveInServiceLines)
    //            if (sl == null)
    //            {
    //                sl = "; " + asl.ServiceLine + "; ";
    //            }
    //            else
    //            {
    //                if (sl.Contains("; " + asl.ServiceLine + "; ") == false)
    //                {
    //                    sl = sl + asl.ServiceLine + "; ";
    //                }
    //            }

    //        if (sl.StartsWith("; "))
    //        {
    //            sl = sl.Substring(2, sl.Length - 2);
    //        }

    //        if (sl.EndsWith("; "))
    //        {
    //            sl = sl.Substring(0, sl.Length - 2);
    //        }

    //        return sl;
    //    }

    //    public static string ViewAllDiagnosis = "View all diagnosis(es)";
    //    public static string ViewCurrentDiagnosis = "View current diagnosis(es)";

    //    private static List<ItemSourceItem> _ICDViewCurrentAllItemSource = new List<ItemSourceItem>
    //    {
    //        new ItemSourceItem { ItemName = ViewAllDiagnosis }, new ItemSourceItem { ItemName = ViewCurrentDiagnosis }
    //    };

    //    public List<ItemSourceItem> ICDViewCurrentAllItemSource => _ICDViewCurrentAllItemSource;
    //    private string _ICDViewCurrentAll = ViewAllDiagnosis;

    //    public string ICDViewCurrentAll
    //    {
    //        get { return _ICDViewCurrentAll; }
    //        set
    //        {
    //            _ICDViewCurrentAll = value;
    //            RaisePropertyChanged("ICDViewCurrentAll");
    //            SetupDiagnosisHistory();
    //        }
    //    }

    //    private ICDViewVersionType _ICDViewVersion = ICDViewVersionType.ICD9;

    //    public ICDViewVersionType ICDViewVersion
    //    {
    //        get { return _ICDViewVersion; }
    //        set
    //        {
    //            _ICDViewVersion = value;
    //            RaisePropertyChanged("ICDViewVersion");
    //            SetupDiagnosisHistory();
    //        }
    //    }

    //    private bool _ICDViewVersion9;

    //    public bool ICDViewVersion9
    //    {
    //        get { return _ICDViewVersion9; }
    //        set
    //        {
    //            _ICDViewVersion9 = value;
    //            RaisePropertyChanged("ICDViewVersion9");
    //            if (value)
    //            {
    //                ICDViewVersion = ICDViewVersionType.ICD9;
    //            }
    //        }
    //    }

    //    private bool _ICDViewVersion10;

    //    public bool ICDViewVersion10
    //    {
    //        get { return _ICDViewVersion10; }
    //        set
    //        {
    //            _ICDViewVersion10 = value;
    //            RaisePropertyChanged("ICDViewVersion10");
    //            if (value)
    //            {
    //                ICDViewVersion = ICDViewVersionType.ICD10;
    //            }
    //        }
    //    }

    //    private bool _ICDViewVersionBoth;

    //    public bool ICDViewVersionBoth
    //    {
    //        get { return _ICDViewVersionBoth; }
    //        set
    //        {
    //            _ICDViewVersionBoth = value;
    //            RaisePropertyChanged("ICDViewVersionBoth");
    //            if (value)
    //            {
    //                ICDViewVersion = ICDViewVersionType.ICDBoth;
    //            }
    //        }
    //    }

    //    public Patient Patient
    //    {
    //        get { return (Patient)GetValue(PatientProperty); }
    //        set { SetValue(PatientProperty, value); }
    //    }

    //    public static readonly DependencyProperty PatientProperty =
    //        DependencyProperty.Register("Patient", typeof(Patient), typeof(PatientDiagnosisUserControl),
    //            new PropertyMetadata(PatientPropertyChanged));

    //    private static void PatientPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
    //    {
    //        PatientDiagnosisUserControl uc = sender as PatientDiagnosisUserControl;
    //        if (uc != null)
    //        {
    //            uc.SetupDiagnosisHistory();
    //        }
    //    }

    //    public IPatientService Model
    //    {
    //        get { return (IPatientService)GetValue(ModelProperty); }
    //        set { SetValue(ModelProperty, value); }
    //    }

    //    public static readonly DependencyProperty ModelProperty =
    //        DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientDiagnosisUserControl), null);

    //    public override void RemoveFromModel(AdmissionDiagnosis entity)
    //    {
    //    }

    //    public override void SaveModel(UserControlBaseCommandType command)
    //    {
    //        if (Model == null)
    //        {
    //            throw new ArgumentNullException("Model", "Model is NULL");
    //        }
    //    }
    //}

    //public class PatientAllergyUserControlBase : ChildControlBase<PatientAllergyUserControl, PatientAllergy>
    //{
    //    public event EventHandler DeletePatientAllergyPressed;
    //    public RelayCommand DeletePatientAllergy_Command { get; protected set; }

    //    public PatientAllergyUserControlBase()
    //    {
    //        DeletePatientAllergy_Command = new RelayCommand(() =>
    //            {
    //                if (SelectedItem == null)
    //                {
    //                    return;
    //                }

    //                SelectedItem.BeginEditting();
    //                SelectedItem.Inactive = true;
    //                SelectedItem.InactiveDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
    //                SelectedItem.EndEditting();

    //                ParentViewModel.PopupDataContext = null;
    //                ProcessFilteredItems();
    //                DeletePatientAllergyPressed?.Invoke(this, EventArgs.Empty);
    //            }
    //        );
    //        AddPressed += OnAddPressed;
    //        OKPressed += OnOKPressed;
    //        OKPressedPreValidate += OnOKPressedPreValidate;
    //        EditPressed += OnEditPressed;
    //        CancelPressed += OnCancelPressed;
    //    }

    //    public override bool? FilterItemsOverride(object item)
    //    {
    //        PatientAllergy pa = item as PatientAllergy;
    //        if ((pa != null) && pa.Inactive)
    //        {
    //            return false;
    //        }

    //        return null;
    //    }

    //    public PatientAllergy NewPatientAllergy
    //    {
    //        get { return (PatientAllergy)GetValue(NewPatientAllergyProperty); }
    //        set { SetValue(NewPatientAllergyProperty, value); }
    //    }

    //    public static readonly DependencyProperty NewPatientAllergyProperty =
    //        DependencyProperty.Register("NewPatientAllergy", typeof(PatientAllergy),
    //            typeof(PatientAllergyUserControlBase), new PropertyMetadata(null, NewPatientAllergyChanged));

    //    private static void NewPatientAllergyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    //    {
    //        PatientAllergyUserControlBase me = sender as PatientAllergyUserControlBase;
    //        if (me != null)
    //        {
    //            me.CloseSearch_Command.Execute(null);

    //            if (me.NewPatientAllergy == null)
    //            {
    //                return;
    //            }

    //            me.LaunchAllergyPopup(me.NewPatientAllergy);
    //        }
    //    }

    //    private void LaunchAllergyPopup(PatientAllergy pa)
    //    {
    //        if (pa == null)
    //        {
    //            return;
    //        }

    //        IsEdit = true;
    //        SelectedItem = pa;
    //        PopupDataTemplate = "PatientAllergyPopupDataTemplate";
    //        ParentViewModel.PopupDataContext = this;
    //    }

    //    public void OnAddPressed(object sender, UserControlBaseEventArgs<PatientAllergy> e)
    //    {
    //        PopupDataTemplate = "PatientAllergyPopupDataTemplate";
    //        ParentViewModel.PopupDataContext = this;
    //    }

    //    public void OnOKPressedPreValidate(object sender, UserControlBaseEventArgs<PatientAllergy> e)
    //    {
    //        e.Entity.ValidationErrors.Clear();

    //        e.Entity.Description = string.IsNullOrWhiteSpace(e.Entity.Description) ? null : e.Entity.Description.Trim();
    //        if (e.Entity.AllergyStartDate == DateTime.MinValue)
    //        {
    //            e.Entity.AllergyStartDate = null;
    //        }

    //        if (e.Entity.AllergyStartDate != null)
    //        {
    //            e.Entity.AllergyStartDate = ((DateTime)e.Entity.AllergyStartDate).Date;
    //        }

    //        if (e.Entity.AllergyEndDate == DateTime.MinValue)
    //        {
    //            e.Entity.AllergyEndDate = null;
    //        }

    //        if (e.Entity.AllergyEndDate != null)
    //        {
    //            e.Entity.AllergyEndDate = ((DateTime)e.Entity.AllergyEndDate).Date;
    //        }

    //        if (e.Entity.LastReactionDate == DateTime.MinValue)
    //        {
    //            e.Entity.LastReactionDate = null;
    //        }

    //        if (e.Entity.LastReactionDate != null)
    //        {
    //            e.Entity.LastReactionDate = ((DateTime)e.Entity.LastReactionDate).Date;
    //        }

    //        e.Entity.Reaction = string.IsNullOrWhiteSpace(e.Entity.Reaction) ? null : e.Entity.Reaction.Trim();
    //    }

    //    public void OnOKPressed(object sender, UserControlBaseEventArgs<PatientAllergy> e)
    //    {
    //        ParentViewModel.PopupDataContext = null;
    //    }

    //    public void OnEditPressed(object sender, UserControlBaseEventArgs<PatientAllergy> e)
    //    {
    //        PopupDataTemplate = "PatientAllergyPopupDataTemplate";
    //        ParentViewModel.PopupDataContext = this;
    //    }

    //    public override bool Validate()
    //    {
    //        bool AllValid = true;
    //        if (SelectedItem == null)
    //        {
    //            return AllValid;
    //        }

    //        if ((SelectedItem.AllergyStartDate != null) && (SelectedItem.AllergyStartDate > DateTime.Today.Date))
    //        {
    //            SelectedItem.ValidationErrors.Add(
    //                new ValidationResult("The Allergy Start Date cannot be in the future.",
    //                    new[] { "AllergyStartDate" }));
    //            AllValid = false;
    //        }

    //        if (string.IsNullOrWhiteSpace(SelectedItem.Reaction) && (SelectedItem.AllergyEndDate == null) &&
    //            (SelectedItem.Inactive == false))
    //        {
    //            SelectedItem.ValidationErrors.Add(new ValidationResult("The Reaction field is required.",
    //                new[] { "Reaction" }));
    //            AllValid = false;
    //        }

    //        return AllValid;
    //    }

    //    public void OnCancelPressed(object sender, UserControlBaseEventArgs<PatientAllergy> e)
    //    {
    //        ParentViewModel.PopupDataContext = null;
    //    }

    //    public IPatientService Model
    //    {
    //        get { return (IPatientService)GetValue(ModelProperty); }
    //        set { SetValue(ModelProperty, value); }
    //    }

    //    public static readonly DependencyProperty ModelProperty =
    //        DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientAllergyUserControl), null);

    //    public override void RemoveFromModel(PatientAllergy entity)
    //    {
    //        if (Model == null)
    //        {
    //            throw new ArgumentNullException("Model", "Model is NULL");
    //        }

    //        Model.Remove(entity);
    //    }

    //    public override void SaveModel(UserControlBaseCommandType command)
    //    {
    //        // SAVE - regardless of whethe command = OK or CANCEL...
    //        if (Model == null)
    //        {
    //            throw new ArgumentNullException("Model", "Model is NULL");
    //        }
    //    }

    //    public override void Cleanup()
    //    {
    //        base.Cleanup();
    //        AddPressed -= OnAddPressed;
    //        OKPressed -= OnOKPressed;
    //        OKPressedPreValidate -= OnOKPressedPreValidate;
    //        EditPressed -= OnEditPressed;
    //        CancelPressed -= OnCancelPressed;
    //    }
    //}

    //public class PatientContactUserControlBase : ChildControlBase<PatientContactUserControl, PatientContact>
    //{
    //    private FacilityBranch _SelectedFacilityBranch;

    //    public FacilityBranch SelectedFacilityBranch
    //    {
    //        get { return _SelectedFacilityBranch; }
    //        set
    //        {
    //            _SelectedFacilityBranch = value;
    //            RaisePropertyChanged("SelectedFacilityBranch");
    //        }
    //    }

    //    public RelayCommand FacilitySelectionChanged { set; get; }

    //    public PatientContactUserControlBase()
    //    {
    //        AddPressed += OnAddPressed;
    //        OKPressedPreValidate += OnOKPressedPreValidate;

    //        CancelPressed += PatientContactUserControlBase_CancelPressed;

    //        EditPressed += PatientContactUserControlBase_EditPressed;

    //        FacilitySelectionChanged = new RelayCommand(() =>
    //        {
    //            if (SelectedItem != null)
    //            {
    //                SelectedItem.FacilityBranchKey = null;
    //            }
    //        });
    //    }

    //    public FacilityBranch originalFacilityBranchKey { get; set; }

    //    void PatientContactUserControlBase_EditPressed(object sender, UserControlBaseEventArgs<PatientContact> e)
    //    {
    //        originalFacilityBranchKey = SelectedFacilityBranch;
    //    }

    //    void PatientContactUserControlBase_CancelPressed(object sender, UserControlBaseEventArgs<PatientContact> e)
    //    {
    //        if (SelectedItem == null)
    //        {
    //            return;
    //        }

    //        if (originalFacilityBranchKey != null)
    //        {
    //            SelectedItem.FacilityBranchKey = originalFacilityBranchKey.FacilityBranchKey;
    //            SelectedFacilityBranch = originalFacilityBranchKey;
    //        }
    //        else
    //        {
    //            SelectedItem.FacilityBranchKey = null;
    //        }
    //    }

    //    public override void Cleanup()
    //    {
    //        base.Cleanup();
    //        AddPressed -= OnAddPressed;
    //        OKPressedPreValidate -= OnOKPressedPreValidate;
    //        CancelPressed -= PatientContactUserControlBase_CancelPressed;
    //        EditPressed -= PatientContactUserControlBase_EditPressed;
    //        FacilitySelectionChanged = null;
    //    }

    //    public void OnAddPressed(object sender, UserControlBaseEventArgs<PatientContact> e)
    //    {
    //        //e.Entity.ContactTypeKey = (int)CodeLookupCache.GetKeyFromCode("PATCONTACTADDRESS", "Patient");
    //        e.Entity.ContactGuid = Guid.NewGuid();
    //    }

    //    public void OnOKPressedPreValidate(object sender, UserControlBaseEventArgs<PatientContact> e)
    //    {
    //        e.Cancel = false;
    //        if (e.Entity == null)
    //        {
    //            return;
    //        }

    //        var valid = e.Entity.ValidateFacilityBranch();
    //        if (!valid)
    //        {
    //            e.Entity.ValidationErrors.Add(new ValidationResult("Facility Branch is required.",
    //                new[] { "FacilityBranchKey" }));
    //            e.Cancel = true;
    //        }
    //    }

    //    public IPatientService Model
    //    {
    //        get { return (IPatientService)GetValue(ModelProperty); }
    //        set { SetValue(ModelProperty, value); }
    //    }

    //    public static readonly DependencyProperty ModelProperty =
    //        DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientContactUserControl), null);

    //    public override void RemoveFromModel(PatientContact entity)
    //    {
    //        if (Model == null)
    //        {
    //            throw new ArgumentNullException("Model", "Model is NULL");
    //        }

    //        Model.Remove(entity);
    //    }

    //    public override void SaveModel(UserControlBaseCommandType command)
    //    {
    //        if (Model == null)
    //        {
    //            throw new ArgumentNullException("Model", "Model is NULL");
    //        }
    //    }
    //}

    //    public class POCDiagnosisUserControlBase : ChildControlBase<POCDiagnosisUserControl, AdmissionDiagnosis>
    //    {
    //        public bool HospiceAdmission => IsHospiceServiceLine;

    //        public bool IsHospiceServiceLine
    //        {
    //            get
    //            {
    //                if (Encounter == null)
    //                {
    //                    return false;
    //                }

    //                if (Encounter.Admission == null)
    //                {
    //                    return false;
    //                }

    //                return Encounter.Admission.HospiceAdmission;
    //            }
    //        }

    //        public override bool? FilterItemsOverride(object item)
    //        {
    //            AdmissionDiagnosis ad = item as AdmissionDiagnosis;
    //            if (ad == null)
    //            {
    //                return false;
    //            }

    //            if (Encounter == null)
    //            {
    //                if (ad.Superceded)
    //                {
    //                    return false;
    //                }
    //            }

    //            // If we have an Encounter and the item is not new, only include the item if it is in this encounter
    //            if ((Encounter != null) && (!ad.IsNew))
    //            {
    //                EncounterDiagnosis ed = Encounter.EncounterDiagnosis
    //                    .Where(p => p.AdmissionDiagnosis.AdmissionDiagnosisKey == ad.AdmissionDiagnosisKey)
    //                    .FirstOrDefault();
    //                if (ed == null)
    //                {
    //                    return false;
    //                }
    //            }

    //            // Filter on ICDMode (Medical / Surgical)
    //            if ((ICDMode == "CM") && (ad.Diagnosis == false))
    //            {
    //                return false;
    //            }

    //            if ((ICDMode == "PCS") && ad.Diagnosis)
    //            {
    //                return false;
    //            }

    //            int version = TenantSettingsCache.Current.TenantSettingRequiredICDVersionPrintPOC(Encounter);
    //            if (ad.Version != version)
    //            {
    //                return false;
    //            }

    //            return true;
    //        }

    //        private IICDCodeService ICDCodeModel { get; set; }
    //    }

    //    public class POCDiagnosisPCSUserControlBase : ChildControlBase<POCDiagnosisPCSUserControl, AdmissionDiagnosis>
    //    {
    //        public bool HospiceAdmission => IsHospiceServiceLine;

    //        public bool IsHospiceServiceLine
    //        {
    //            get
    //            {
    //                if (Encounter == null)
    //                {
    //                    return false;
    //                }

    //                if (Encounter.Admission == null)
    //                {
    //                    return false;
    //                }

    //                return Encounter.Admission.HospiceAdmission;
    //            }
    //        }

    //        public override bool? FilterItemsOverride(object item)
    //        {
    //            AdmissionDiagnosis ad = item as AdmissionDiagnosis;
    //            if (ad == null)
    //            {
    //                return false;
    //            }

    //            if (Encounter == null)
    //            {
    //                if (ad.Superceded)
    //                {
    //                    return false;
    //                }
    //            }

    //            // If we have an Encounter and the item is not new, only include the item if it is in this encounter
    //            if ((Encounter != null) && (!ad.IsNew))
    //            {
    //                EncounterDiagnosis ed = Encounter.EncounterDiagnosis
    //                    .Where(p => p.AdmissionDiagnosis.AdmissionDiagnosisKey == ad.AdmissionDiagnosisKey)
    //                    .FirstOrDefault();
    //                if (ed == null)
    //                {
    //                    return false;
    //                }
    //            }

    //            // Filter on ICDMode (Medical / Surgical)
    //            if ((ICDMode == "CM") && (ad.Diagnosis == false))
    //            {
    //                return false;
    //            }

    //            if ((ICDMode == "PCS") && ad.Diagnosis)
    //            {
    //                return false;
    //            }

    //            int version = TenantSettingsCache.Current.TenantSettingRequiredICDVersionPrintPOC(Encounter);
    //            if (ad.Version != version)
    //            {
    //                return false;
    //            }

    //            return true;
    //        }

    //        private IICDCodeService ICDCodeModel { get; set; }
    //    }

    //    public class PatientFacilityStayUserControlBase : ChildControlBase<PatientFacilityStayUserControl, PatientFacilityStay>
    //    {
    //        public PatientFacilityStayUserControlBase()
    //        {
    //            PopupDataTemplate = "PatientFacilityStayPopupDataTemplate";

    //            AddPressed += OnAddPressed;
    //            OKPressed += OnOKPressed;
    //            OKPressedPreValidate += OnOKPressedPreValidate;
    //            OKPressedValidate += OnOKPressedValidate;
    //            EditPressed += OnEditPressed;
    //            CancelPressed += OnCancelPressed;
    //            ItemSelected += Update_ItemSelected;
    //            Messenger.Default.Register<int>(this, "RefreshMaintenancePatient", i => RefreshPatient(i));
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            OKPressed -= OnOKPressed;
    //            OKPressedPreValidate -= OnOKPressedPreValidate;
    //            OKPressedValidate -= OnOKPressedValidate;
    //            EditPressed -= OnEditPressed;
    //            CancelPressed -= OnCancelPressed;
    //            ItemSelected -= Update_ItemSelected;
    //            CurrentPatient = null;
    //            Messenger.Default.Unregister(this);
    //            if (_PatientFacilityStays != null)
    //            {
    //                _PatientFacilityStays.Filter -= _PatientFacilityStays_Filter;
    //                _PatientFacilityStays = null;
    //            }

    //            base.Cleanup();
    //        }

    //        private void RefreshPatient(int patientKey)
    //        {
    //            if (CurrentPatient == null)
    //            {
    //                return;
    //            }

    //            if (CurrentPatient.PatientKey != patientKey)
    //            {
    //                return;
    //            }

    //            SetupFacilityStaysCanEdit();
    //        }

    //        public Patient CurrentPatient
    //        {
    //            get { return (Patient)GetValue(CurrentPatientProperty); }
    //            set { SetValue(CurrentPatientProperty, value); }
    //        }

    //        public static readonly DependencyProperty CurrentPatientProperty =
    //            DependencyProperty.Register("CurrentPatient", typeof(Patient), typeof(PatientFacilityStayUserControlBase),
    //                new PropertyMetadata(null, CurrentPatientChanged));

    //        private static void CurrentPatientChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    //        {
    //            PatientFacilityStayUserControlBase me = sender as PatientFacilityStayUserControlBase;
    //            if (me == null)
    //            {
    //                return;
    //            }

    //            if (me.ItemsSource != null)
    //            {
    //                foreach (PatientFacilityStay pfs in me.ItemsSource)
    //                    pfs.CurrentPatient = me.CurrentPatient;
    //            }
    //        }

    //        void Update_ItemSelected(object sender, EventArgs e)
    //        {
    //            if (CurrentPatient == null)
    //            {
    //                return;
    //            }

    //            if (ItemsSource != null)
    //            {
    //                foreach (PatientFacilityStay pfs in ItemsSource)
    //                    pfs.CurrentPatient = CurrentPatient;
    //            }

    //            _PatientFacilityStays = new CollectionViewSource();
    //            _PatientFacilityStays.SortDescriptions.Add(new SortDescription("AddedDateTime",
    //                ListSortDirection.Descending));
    //            _PatientFacilityStays.Source = ItemsSource;
    //            _PatientFacilityStays.View.MoveCurrentToFirst();

    //            _PatientFacilityStays.Filter += _PatientFacilityStays_Filter;
    //            PatientFacilityStays.Refresh();
    //            SetupFacilityStaysCanEdit();
    //#if !OPENSILVER // TODO: Set method of UserControlBase.SelectedItem property is called
    //                // and we have stack overflow errors with OpenSilver
    //            RaisePropertyChanged("PatientFacilityStays");
    //#endif
    //        }

    //        private void _PatientFacilityStays_Filter(object sender, FilterEventArgs args)
    //        {
    //            PatientFacilityStay pfs = args.Item as PatientFacilityStay;
    //            if (pfs.HistoryKey != null)
    //            {
    //                args.Accepted = false;
    //            }
    //        }

    //        private CollectionViewSource _PatientFacilityStays;
    //        public ICollectionView PatientFacilityStays => _PatientFacilityStays.View;

    //        private void SetupFacilityStaysCanEdit()
    //        {
    //            if (_PatientFacilityStays != null)
    //            {
    //                PatientFacilityStay mostRecentFS = _PatientFacilityStays.View.Cast<PatientFacilityStay>()
    //                    .OrderByDescending(p => p.AddedDateTime).FirstOrDefault();
    //                foreach (PatientFacilityStay fs in _PatientFacilityStays.View.Cast<PatientFacilityStay>())
    //                    fs.CanEdit = (fs == mostRecentFS) ? true : false;
    //            }

    //            PatientFacilityStays.Refresh();
    //        }

    //        private void PopupSetup(UserControlBaseEventArgs<PatientFacilityStay> e)
    //        {
    //            // The start date (DisplayDateStart) for an editable (most recent) or new facility stay has to be 
    //            // on or after the start date of the most recent prior stay
    //            PatientFacilityStay mostRecentFS = _PatientFacilityStays.View.Cast<PatientFacilityStay>()
    //                .Where(p => p != e.Entity).OrderByDescending(p => p.AddedDateTime).FirstOrDefault();
    //            if ((mostRecentFS != null) && (mostRecentFS.EndDate != null) && (mostRecentFS.EndDate != DateTime.MinValue))
    //            {
    //                e.Entity.DisplayDateStart = mostRecentFS.EndDate;
    //            }
    //            else
    //            {
    //                e.Entity.DisplayDateStart = null;
    //            }

    //            e.Entity.CurrentPatient = CurrentPatient;
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<PatientFacilityStay> e)
    //        {
    //            PatientFacilityStay fs = null;
    //            if (_PatientFacilityStays != null)
    //            {
    //                fs = _PatientFacilityStays.View.Cast<PatientFacilityStay>().FirstOrDefault(p => p.EndDate == null);
    //            }

    //            if (fs != null)
    //            {
    //                NavigateCloseDialog d = new NavigateCloseDialog();
    //                if (d != null)
    //                {
    //                    d.Closed += (s, err) => { Cancel2_Command.Execute(null); };
    //                    d.NoVisible = false;
    //                    d.OKLabel = "OK";
    //                    d.Title = "Cannot add new facility stay";
    //                    d.Width = double.NaN;
    //                    d.Height = double.NaN;
    //                    d.ErrorMessage =
    //                        String.Format("Cannot add a new stay until the most recent stay at {0} is end dated .",
    //                            fs.FacilityName);
    //                    d.Show();
    //                }
    //            }

    //            e.Entity.AddedDateTime = DateTime.UtcNow;
    //            PopupSetup(e);

    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<PatientFacilityStay> e)
    //        {
    //            if (e.Entity.CanEdit == false)
    //            {
    //                e.Cancel = true;
    //                return;
    //            }

    //            PopupSetup(e);
    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnOKPressed(object sender, UserControlBaseEventArgs<PatientFacilityStay> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            SetupFacilityStaysCanEdit();
    //        }

    //        public void OnOKPressedPreValidate(object sender, UserControlBaseEventArgs<PatientFacilityStay> e)
    //        {
    //            if (e.Entity.StartDate == DateTime.MinValue)
    //            {
    //                e.Entity.StartDate = null;
    //            }

    //            if (e.Entity.EndDate == DateTime.MinValue)
    //            {
    //                e.Entity.EndDate = null;
    //            }
    //        }

    //        public void OnOKPressedValidate(object sender, UserControlBaseEventArgs<PatientFacilityStay> e)
    //        {
    //            bool AllValid = true;

    //            if (e.Entity.DisplayDateStart.HasValue && !e.Entity.DisplayDateStart.Equals(DateTime.MinValue) &&
    //                e.Entity.StartDate.HasValue && !e.Entity.StartDate.Equals(DateTime.MinValue))
    //            {
    //                if (DateTime.Compare(e.Entity.DisplayDateStart.Value.Date, e.Entity.StartDate.Value.Date) > 0)
    //                {
    //                    string[] memberNames = { "StartDate" };
    //                    e.Entity.ValidationErrors.Add(new ValidationResult(
    //                        string.Format(
    //                            "The Start Date must be on or after the end date of {0} on the most recent facility stay",
    //                            e.Entity.DisplayDateStart.Value.ToString("MM/dd/yyyy")), memberNames));
    //                    AllValid = false;
    //                }
    //            }

    //            e.Cancel = (!AllValid);
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<PatientFacilityStay> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            SetupFacilityStaysCanEdit();
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientFacilityStayUserControlBase),
    //                null);

    //        public override void RemoveFromModel(PatientFacilityStay entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            // SAVE - regardless of whethe command = OK or CANCEL...
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }
    //        }
    //    }

    //    public class PatientInsuranceUserControlBase : ChildControlBase<PatientInsuranceUserControl, PatientInsurance>
    //    {
    //        public PatientInsuranceUserControlBase()
    //        {
    //            PopupDataTemplate = "PatientVerificationHistoryPopupDataTemplate";
    //            AddPressed += OnAddPressed;
    //            ItemSelected += PatientInsuranceUserControlBase_ItemSelected;
    //            InsuranceVerifications.SortDescriptions.Add(new SortDescription("EffectiveFrom",
    //                ListSortDirection.Descending));
    //            InsuranceVerifications.Filter += InsuranceVerifications_Filter;
    //            VerificationDetails.SortDescriptions.Add(new SortDescription("SequenceNumber",
    //                ListSortDirection.Ascending));
    //            VerificationDetails.Filter += VerificationDetails_Filter;

    //            OKHistoryCommand = new RelayCommand(() =>
    //            {
    //                if (SelectedItem != null)
    //                {
    //                    if (ValidateHistory(SelectedItem.InsuranceVerifyHistory))
    //                    {
    //                        if (SelectedItem.InsuranceVerifyHistory != null)
    //                        {
    //                            foreach (InsuranceVerifyHistory ivh in SelectedItem.InsuranceVerifyHistory)
    //                                if (ivh.Validate())
    //                                {
    //                                    if (ivh.InsuranceVerifyHistoryDetail != null)
    //                                    {
    //                                        foreach (InsuranceVerifyHistoryDetail dtl in ivh.InsuranceVerifyHistoryDetail)
    //                                            if (dtl.IsEditting)
    //                                            {
    //                                                dtl.EndEditting();
    //                                            }
    //                                    }

    //                                    if (ivh.IsEditting)
    //                                    {
    //                                        ivh.EndEditting();
    //                                    }
    //                                }
    //                        }

    //                        ParentViewModel.PopupDataContext = null;
    //                    }
    //                }
    //            });

    //            CancelHistoryCommand = new RelayCommand(() =>
    //            {
    //                if ((SelectedItem != null)
    //                    && (SelectedItem.InsuranceVerifyHistory != null)
    //                   )
    //                {
    //                    foreach (InsuranceVerifyHistory ivh in SelectedItem.InsuranceVerifyHistory)
    //                    {
    //                        if (ivh.InsuranceVerifyHistoryDetail != null)
    //                        {
    //                            foreach (InsuranceVerifyHistoryDetail dtl in ivh.InsuranceVerifyHistoryDetail)
    //                                if (dtl.IsNew)
    //                                {
    //                                    Model.Remove(dtl);
    //                                }
    //                                else if (dtl.IsEditting)
    //                                {
    //                                    dtl.CancelEditting();
    //                                }
    //                        }

    //                        if (ivh.IsNew)
    //                        {
    //                            Model.Remove(ivh);
    //                        }
    //                        else if (ivh.IsEditting)
    //                        {
    //                            ivh.CancelEditting();
    //                        }
    //                    }
    //                }

    //                ParentViewModel.PopupDataContext = null;
    //            });

    //            InsVerifyShowDetailsCommand = new RelayCommand<DataGridRow>(dgr =>
    //            {
    //                if (dgr != null)
    //                {
    //                    var ivh = dgr.DataContext as InsuranceVerifyHistory;
    //                    if (ivh != null)
    //                    {
    //                        ivh.RaisePropertyChangedWithPrefix("InsuranceVerifyHistoryDetail");
    //                        SelectedInsuranceVerifyHistory = ivh;
    //                        InsuranceVerifyDetailsDialog d = new InsuranceVerifyDetailsDialog
    //                        {
    //                            DataContext = this
    //                        };
    //                        d.Show();
    //                    }
    //                }
    //            });

    //            AddInsVerifyHistoryDetailCommand = new RelayCommand(() =>
    //            {
    //                if (SelectedInsuranceVerifyHistory != null)
    //                {
    //                    InsuranceVerifyHistoryDetail dtl = new InsuranceVerifyHistoryDetail();
    //                    dtl.PropertyChanged += dtl_PropertyChanged;
    //                    dtl.PatientInsuranceKey = SelectedItem.PatientInsuranceKey;
    //                    var max = SelectedInsuranceVerifyHistory.InsuranceVerifyHistoryDetail.Where(ivhd => !ivhd.Inactive)
    //                        .OrderByDescending(ivhd => ivhd.SequenceNumber).FirstOrDefault();
    //                    if (max != null)
    //                    {
    //                        dtl.SequenceNumber = max.SequenceNumber + 1;
    //                    }
    //                    else
    //                    {
    //                        dtl.SequenceNumber = 1;
    //                    }

    //                    SelectedInsuranceVerifyHistory.InsuranceVerifyHistoryDetail.Add(dtl);
    //                    SelectedInsuranceVerifyHistory.RaisePropertyChangedWithPrefix("DetailsButtonLabel");
    //                    SelectedInsuranceVerifyHistory.InsuranceVerifyHistoryDetail.Where(i => (!i.Inactive))
    //                        .ForEach(row =>
    //                        {
    //                            row.RaisePropertyChangedWithPrefix("CanMoveUp");
    //                            row.RaisePropertyChangedWithPrefix("CanMoveDown");
    //                        });
    //                    RaiseCanExecuteChanged();
    //                }
    //            });

    //            AddInsuranceVerifyCommand = new RelayCommand(() =>
    //            {
    //                if (SelectedItem != null)
    //                {
    //                    InsuranceVerifyHistory ivh = new InsuranceVerifyHistory();
    //                    ivh.PropertyChanged += ivh_PropertyChanged;
    //                    UserProfile up = UserCache.Current.GetCurrentUserProfile();
    //                    if (up != null)
    //                    {
    //                        ivh.VerifiedBy = up.UserId;
    //                        ivh.RequestDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
    //                        ivh.EffectiveFrom = DateTime.Now.Date;
    //                    }

    //                    if (SelectedItem.InsuranceVerifyHistory.Any())
    //                    {
    //                        var max = SelectedItem.InsuranceVerifyHistory.OrderByDescending(i => i.EffectiveFrom)
    //                            .FirstOrDefault();

    //                        if ((max != null)
    //                            && ((max.EffectiveThru == null)
    //                                || (max.EffectiveThru >= ivh.EffectiveFrom)
    //                            )
    //                           )
    //                        {
    //                            max.EffectiveThru = ivh.EffectiveFrom.AddDays(-1);
    //                        }
    //                    }

    //                    SelectedItem.InsuranceVerifyHistory.Add(ivh);
    //                    RaiseCanExecuteChanged();
    //                }
    //            });

    //            VerificationHistoryCommand = new RelayCommand(() =>
    //            {
    //                if ((SelectedItem != null)
    //                    && (SelectedItem.InsuranceVerifyHistory != null)
    //                   )
    //                {
    //                    foreach (InsuranceVerifyHistory ivr in SelectedItem.InsuranceVerifyHistory)
    //                    {
    //                        ivr.BeginEditting();
    //                        if (ivr.InsuranceVerifyHistoryDetail != null)
    //                        {
    //                            foreach (InsuranceVerifyHistoryDetail dtl in ivr.InsuranceVerifyHistoryDetail)
    //                                dtl.BeginEditting();
    //                        }
    //                    }
    //                }

    //                ParentViewModel.PopupDataContext = this;
    //            });

    //            OKHistoryDetails = new RelayCommand<InsuranceVerifyDetailsDialog>(window =>
    //            {
    //                if (ValidateHistoryDetails(SelectedInsuranceVerifyHistory))
    //                {
    //                    if (window != null)
    //                    {
    //                        window.DialogResult = true;
    //                    }
    //                }
    //            });

    //            CancelHistoryDetails = new RelayCommand<InsuranceVerifyDetailsDialog>(window =>
    //            {
    //                if ((SelectedInsuranceVerifyHistory != null)
    //                    && (SelectedInsuranceVerifyHistory.InsuranceVerifyHistoryDetail != null)
    //                   )
    //                {
    //                    foreach (InsuranceVerifyHistoryDetail dtl in SelectedInsuranceVerifyHistory
    //                                 .InsuranceVerifyHistoryDetail)
    //                        if (dtl.IsNew)
    //                        {
    //                            Model.Remove(dtl);
    //                        }
    //                        else if (dtl.IsEditting)
    //                        {
    //                            dtl.CancelEditting();
    //                        }
    //                }

    //                if (window != null)
    //                {
    //                    window.DialogResult = true;
    //                }

    //                if (SelectedInsuranceVerifyHistory != null)
    //                {
    //                    SelectedInsuranceVerifyHistory.RaisePropertyChangedWithPrefix("DetailsButtonLabel");
    //                }
    //            });

    //            MoveDownCommand = new RelayCommand<InsuranceVerifyHistoryDetail>(dtl =>
    //            {
    //                if ((SelectedInsuranceVerifyHistory != null)
    //                    && (SelectedInsuranceVerifyHistory.InsuranceVerifyHistoryDetail != null)
    //                    && SelectedInsuranceVerifyHistory.InsuranceVerifyHistoryDetail.Any(
    //                        ivhd => (ivhd.SequenceNumber > dtl.SequenceNumber) && (!ivhd.Inactive)
    //                    )
    //                   )
    //                {
    //                    var next = SelectedInsuranceVerifyHistory.InsuranceVerifyHistoryDetail.Where(ivhd =>
    //                            (ivhd.SequenceNumber > dtl.SequenceNumber) && (!ivhd.Inactive))
    //                        .OrderBy(ivhd => ivhd.SequenceNumber).FirstOrDefault();
    //                    if (next != null)
    //                    {
    //                        int nextSeqNum = next.SequenceNumber;
    //                        next.SequenceNumber = dtl.SequenceNumber;
    //                        dtl.SequenceNumber = nextSeqNum;
    //                        VerificationDetails.View.Refresh();
    //                    }
    //                }
    //            });

    //            MoveUpCommand = new RelayCommand<InsuranceVerifyHistoryDetail>(dtl =>
    //            {
    //                if ((SelectedInsuranceVerifyHistory != null)
    //                    && (SelectedInsuranceVerifyHistory.InsuranceVerifyHistoryDetail != null)
    //                    && SelectedInsuranceVerifyHistory.InsuranceVerifyHistoryDetail.Any(
    //                        ivhd => (ivhd.SequenceNumber < dtl.SequenceNumber) && (!ivhd.Inactive)
    //                    )
    //                   )
    //                {
    //                    var prev = SelectedInsuranceVerifyHistory.InsuranceVerifyHistoryDetail.Where(ivhd =>
    //                            (ivhd.SequenceNumber < dtl.SequenceNumber) && (!ivhd.Inactive))
    //                        .OrderByDescending(ivhd => ivhd.SequenceNumber).FirstOrDefault();
    //                    if (prev != null)
    //                    {
    //                        int nextSeqNum = prev.SequenceNumber;
    //                        prev.SequenceNumber = dtl.SequenceNumber;
    //                        dtl.SequenceNumber = nextSeqNum;
    //                        VerificationDetails.View.Refresh();
    //                    }
    //                }
    //            });

    //            DeleteDetailCommand = new RelayCommand<InsuranceVerifyHistoryDetail>(dtl =>
    //            {
    //                if (dtl != null)
    //                {
    //                    dtl.Inactive = true;
    //                    dtl.InactiveDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
    //                    SelectedInsuranceVerifyHistory.InsuranceVerifyHistoryDetail.Where(i => (!i.Inactive)
    //                            && (dtl.SequenceNumber <= i.SequenceNumber)
    //                        )
    //                        .ForEach(row =>
    //                        {
    //                            row.SequenceNumber -= 1;
    //                            row.RaisePropertyChangedWithPrefix("CanMoveUp");
    //                            row.RaisePropertyChangedWithPrefix("CanMoveDown");
    //                        });
    //                    SelectedInsuranceVerifyHistory.RaisePropertyChangedWithPrefix("DetailsButtonLabel");
    //                    VerificationDetails.View.Refresh();
    //                }
    //            });

    //            DeleteHeaderCommand = new RelayCommand<InsuranceVerifyHistory>(hdr =>
    //            {
    //                if (hdr != null)
    //                {
    //                    hdr.Inactive = true;
    //                    hdr.InactiveDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

    //                    hdr.InsuranceVerifyHistoryDetail.Where(i => (!i.Inactive)
    //                        )
    //                        .ForEach(row =>
    //                        {
    //                            row.Inactive = true;
    //                            row.InactiveDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
    //                        });
    //                    InsuranceVerifications.View.Refresh();
    //                }
    //            });
    //        }

    //        void InsuranceVerifications_Filter(object sender, FilterEventArgs e)
    //        {
    //            InsuranceVerifyHistory row = e.Item as InsuranceVerifyHistory;

    //            if (row != null)
    //            {
    //                e.Accepted = !row.Inactive;
    //            }
    //        }

    //        void VerificationDetails_Filter(object sender, FilterEventArgs e)
    //        {
    //            InsuranceVerifyHistoryDetail row = e.Item as InsuranceVerifyHistoryDetail;

    //            if (row != null)
    //            {
    //                e.Accepted = !row.Inactive;
    //            }
    //        }

    //        public void GetInsuranceForCarrierCode(InsuranceVerifyHistoryDetail dtl, string carrierCode)
    //        {
    //            Insurance ins = null;
    //            if ((dtl != null)
    //                && (!string.IsNullOrEmpty(dtl.CarrierCode)
    //                )
    //               )
    //            {
    //                dtl.CarrierCode = dtl.CarrierCode.ToUpper();
    //                InsuranceParameterDefinition ipd =
    //                    InsuranceCache.GetInsuranceParameterDefinitionForCode("270", "CarrierCodes");

    //                if (ipd != null)
    //                {
    //                    ins = InsuranceCache.GetActiveInsurances().Where(i => i.InsuranceParameter.Any(ip =>
    //                            (ipd.InsuranceParameterDefinitionKey == ip.ParameterKey)
    //                            && (!string.IsNullOrEmpty(ip.Value))
    //                            && (ip.Value.Replace(',', '|').ToUpper().Split('|').Contains(dtl.CarrierCode))
    //                        )
    //                    ).FirstOrDefault();
    //                }
    //            }

    //            if (ins != null)
    //            {
    //                dtl.CarrierName = ins.Name;
    //                dtl.InsuranceKey = ins.InsuranceKey;

    //                if (ins.InsuranceType.HasValue)
    //                {
    //                    CodeLookup type = CodeLookupCache.GetCodeLookupFromKey(ins.InsuranceType.Value);

    //                    if (type != null)
    //                    {
    //                        dtl.IsMedicare = (type.Code == "1")
    //                                         || (type.Code == "2")
    //                                         || (type.Code == "12");
    //                    }
    //                }

    //                if (!dtl.IsMedicare)
    //                {
    //                    if (ins.InsuranceSubType.HasValue)
    //                    {
    //                        CodeLookup type = CodeLookupCache.GetCodeLookupFromKey(ins.InsuranceSubType.Value);

    //                        if (type != null)
    //                        {
    //                            dtl.IsMedicare = (type.Code == "1")
    //                                             || (type.Code == "2")
    //                                             || (type.Code == "12");
    //                        }
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                dtl.CarrierName = null;
    //                dtl.InsuranceKey = 0;
    //                dtl.CoverageCode = null;
    //                dtl.PhoneNumber = null;
    //            }
    //        }

    //        void dtl_PropertyChanged(object sender, PropertyChangedEventArgs e)
    //        {
    //            if (e.PropertyName == "CarrierCode")
    //            {
    //                InsuranceVerifyHistoryDetail dtl = (sender as InsuranceVerifyHistoryDetail);
    //                if ((dtl != null)
    //                    && (!string.IsNullOrEmpty(dtl.CarrierCode))
    //                   )
    //                {
    //                    GetInsuranceForCarrierCode(dtl, dtl.CarrierCode);
    //                }
    //            }
    //        }

    //        void PatientInsuranceUserControlBase_ItemSelected(object sender, EventArgs e)
    //        {
    //            if ((SelectedItem != null)
    //                && (SelectedItem.InsuranceVerifyHistory != null)
    //               )
    //            {
    //                foreach (InsuranceVerifyHistory ivh in SelectedItem.InsuranceVerifyHistory)
    //                {
    //                    ivh.PropertyChanged += ivh_PropertyChanged;

    //                    if (ivh.InsuranceVerifyHistoryDetail != null)
    //                    {
    //                        foreach (InsuranceVerifyHistoryDetail dtl in ivh.InsuranceVerifyHistoryDetail)
    //                            dtl.PropertyChanged += dtl_PropertyChanged;
    //                    }
    //                }
    //            }

    //            if (SelectedItem != null)
    //            {
    //                InsuranceVerifications.Source = SelectedItem.InsuranceVerifyHistory;
    //                InsuranceVerifications.View.Refresh();
    //            }
    //        }

    //        void ivh_PropertyChanged(object sender, PropertyChangedEventArgs e)
    //        {
    //            RaiseCanExecuteChanged();
    //        }

    //        public InsuranceVerifyHistory SelectedInsuranceVerifyHistory
    //        {
    //            get { return insuranceVerifyHistory; }
    //            set
    //            {
    //                insuranceVerifyHistory = value;
    //                if (value != null)
    //                {
    //                    VerificationDetails.Source = value.InsuranceVerifyHistoryDetail;
    //                    VerificationDetails.View.Refresh();
    //                }

    //                RaisePropertyChanged("InsuranceVerifyHistory");
    //            }
    //        }

    //        private InsuranceVerifyHistory insuranceVerifyHistory;

    //        public CollectionViewSource VerificationDetails => verificationDetails;
    //        private CollectionViewSource verificationDetails = new CollectionViewSource();

    //        public RelayCommand VerificationHistoryCommand { get; protected set; }
    //        public RelayCommand AddInsVerifyHistoryDetailCommand { get; protected set; }
    //        public RelayCommand AddInsuranceVerifyCommand { get; protected set; }
    //        public RelayCommand<DataGridRow> InsVerifyShowDetailsCommand { get; protected set; }
    //        public RelayCommand OKHistoryCommand { get; protected set; }
    //        public RelayCommand CancelHistoryCommand { get; protected set; }
    //        public RelayCommand<InsuranceVerifyDetailsDialog> OKHistoryDetails { get; protected set; }
    //        public RelayCommand<InsuranceVerifyDetailsDialog> CancelHistoryDetails { get; protected set; }
    //        public RelayCommand<InsuranceVerifyHistoryDetail> MoveUpCommand { get; protected set; }
    //        public RelayCommand<InsuranceVerifyHistoryDetail> MoveDownCommand { get; protected set; }
    //        public RelayCommand<InsuranceVerifyHistoryDetail> DeleteDetailCommand { get; protected set; }
    //        public RelayCommand<InsuranceVerifyHistory> DeleteHeaderCommand { get; protected set; }

    //        public CollectionViewSource InsuranceVerifications
    //        {
    //            get { return insuranceVerifications; }
    //            set
    //            {
    //                insuranceVerifications = new CollectionViewSource();
    //                RaisePropertyChanged("InsuranceVerifications");
    //            }
    //        }

    //        private CollectionViewSource insuranceVerifications = new CollectionViewSource();

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            ItemSelected -= PatientInsuranceUserControlBase_ItemSelected;

    //            if ((SelectedItem != null)
    //                && (SelectedItem.InsuranceVerifyHistory != null)
    //               )
    //            {
    //                foreach (InsuranceVerifyHistory ivh in SelectedItem.InsuranceVerifyHistory)
    //                    ivh.PropertyChanged -= ivh_PropertyChanged;
    //            }

    //            Model = null;
    //            base.Cleanup();
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<PatientInsurance> e)
    //        {
    //            e.Entity.InsuranceVerified = false;
    //            e.Entity.Inactive = false;
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientInsuranceUserControl), null);

    //        public override void RemoveFromModel(PatientInsurance entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            // SAVE - regardless of whethe command = OK or CANCEL...
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }
    //        }

    //        public EntityCollection<PatientContact> ContactItemsSource
    //        {
    //            get { return (EntityCollection<PatientContact>)GetValue(ContactItemsSourceProperty); }
    //            set { SetValue(ContactItemsSourceProperty, value); }
    //        }

    //        public static readonly DependencyProperty ContactItemsSourceProperty =
    //            DependencyProperty.Register("ContactItemsSource", typeof(EntityCollection<PatientContact>),
    //                typeof(PatientInsuranceUserControl),
    //                new PropertyMetadata(ContactItemsSourcePropertyChanged));

    //        public bool ValidateHistory(IEnumerable<InsuranceVerifyHistory> rowsToValidate)
    //        {
    //            bool allValid = true;

    //            if (rowsToValidate != null)
    //            {
    //                foreach (InsuranceVerifyHistory insuranceVerifyHistory in rowsToValidate.Where(r => !r.Inactive))
    //                {
    //                    if ((insuranceVerifyHistory.EffectiveFrom == null)
    //                        || (insuranceVerifyHistory.EffectiveFrom.Equals(DateTime.MinValue))
    //                       )
    //                    {
    //                        allValid = false;
    //                        string[] memberNames = { "EffectiveFrom" };
    //                        insuranceVerifyHistory.ValidationErrors.Add(
    //                            new ValidationResult("The Effective From field is required.", memberNames));
    //                    }

    //                    if ((insuranceVerifyHistory.EffectiveThru.HasValue)
    //                        && (insuranceVerifyHistory.EffectiveThru < insuranceVerifyHistory.EffectiveFrom)
    //                       )
    //                    {
    //                        allValid = false;
    //                        string[] memberNames = { "EffectiveFrom", "EffectiveThru" };
    //                        insuranceVerifyHistory.ValidationErrors.Add(new ValidationResult(
    //                            "The Effective From date must be earlier than or equal to the Effective Thru date.",
    //                            memberNames));
    //                    }

    //                    if (insuranceVerifyHistory.VerificationStatus <= 0)
    //                    {
    //                        allValid = false;
    //                        string[] memberNames = { "VerificationStatus" };
    //                        insuranceVerifyHistory.ValidationErrors.Add(
    //                            new ValidationResult("The Verification Status field is required.", memberNames));
    //                    }

    //                    var overlaps = rowsToValidate.Where(ivh => (!ivh.Inactive)
    //                                                               && (ivh.InsuranceVerifyHistoryKey !=
    //                                                                   insuranceVerifyHistory.InsuranceVerifyHistoryKey)
    //                                                               && ((ivh.EffectiveThru >=
    //                                                                    insuranceVerifyHistory.EffectiveFrom
    //                                                                   )
    //                                                                   || (ivh.EffectiveThru == null)
    //                                                               )
    //                                                               && ((ivh.EffectiveFrom <=
    //                                                                    insuranceVerifyHistory.EffectiveThru)
    //                                                                   || (insuranceVerifyHistory.EffectiveThru == null)
    //                                                               )
    //                    );

    //                    if (overlaps.Any())
    //                    {
    //                        allValid = false;
    //                        string[] memberNames = { "EffectiveFrom", "EffectiveThru" };
    //                        insuranceVerifyHistory.ValidationErrors.Add(
    //                            new ValidationResult("Effective Date ranges must not overlap.", memberNames));
    //                    }
    //                }
    //            }

    //            return allValid;
    //        }

    //        public bool ValidateHistoryDetails(InsuranceVerifyHistory RowToValidate)
    //        {
    //            bool isValid = true;

    //            if ((RowToValidate != null)
    //                && (RowToValidate.InsuranceVerifyHistoryDetail != null)
    //               )
    //            {
    //                foreach (InsuranceVerifyHistoryDetail dtl in RowToValidate.InsuranceVerifyHistoryDetail.Where(d =>
    //                             !d.Inactive))
    //                {
    //                    dtl.ValidationErrors.Clear();
    //                    if (string.IsNullOrEmpty(dtl.CarrierCode))
    //                    {
    //                        isValid = false;
    //                        string[] memberNames = { "CarrierCode" };
    //                        string message = "Carrier Code is required";
    //                        dtl.ValidationErrors.Add(new ValidationResult(message, memberNames));
    //                    }
    //                    else if (dtl.InsuranceKey <= 0)
    //                    {
    //                        isValid = false;
    //                        string[] memberNames = { "CarrierCode" };
    //                        string message = "Could not determine Insurance for Coverage Code";
    //                        dtl.ValidationErrors.Add(new ValidationResult(message, memberNames));
    //                    }

    //                    if (string.IsNullOrEmpty(dtl.CoverageCode))
    //                    {
    //                        isValid = false;
    //                        string[] memberNames = { "CoverageCode" };
    //                        string message = "Coverage Code is required";
    //                        dtl.ValidationErrors.Add(new ValidationResult(message, memberNames));
    //                    }

    //                    if (string.IsNullOrEmpty(dtl.PolicyNumber))
    //                    {
    //                        isValid = false;
    //                        string[] memberNames = { "PolicyNumber" };
    //                        string message = "Policy Number is required";
    //                        dtl.ValidationErrors.Add(new ValidationResult(message, memberNames));
    //                    }
    //                }
    //            }

    //            return isValid;
    //        }

    //        private static void ContactItemsSourcePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
    //        {
    //            try
    //            {
    //                var instance = sender as PatientInsuranceUserControl;
    //                instance.ProcessContactFilteredItems();
    //            }
    //            catch (Exception oe)
    //            {
    //                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
    //                throw;
    //            }
    //        }

    //        public ICollectionView FilteredContactItemsSource { get; set; }

    //        public void ProcessContactFilteredItems()
    //        {
    //            if (ContactItemsSource != null)
    //            {
    //                FilteredContactItemsSource = new PagedCollectionView(ContactItemsSource);
    //                FilteredContactItemsSource.SortDescriptions.Add(new SortDescription("LastName",
    //                    ListSortDirection.Ascending));
    //                FilteredContactItemsSource.SortDescriptions.Add(new SortDescription("FirstName",
    //                    ListSortDirection.Ascending));
    //                FilteredContactItemsSource.Filter = ContactFilterItems;
    //                RaisePropertyChanged("FilteredContactItemsSource");
    //            }
    //        }

    //        public bool ContactFilterItems(object item)
    //        {
    //            PatientContact pc = item as PatientContact;
    //            if (pc == null)
    //            {
    //                return false;
    //            }

    //            return (!pc.Inactive && pc.HistoryKey == null);
    //        }

    //        public override bool Validate()
    //        {
    //            bool AllValid = true;
    //            if (SelectedItem == null)
    //            {
    //                return AllValid;
    //            }

    //            var RelToInsCode = (SelectedItem.RelToInsured == null
    //                ? ""
    //                : CodeLookupCache.GetCodeFromKey(SelectedItem.RelToInsured));
    //            bool RelToInsSelf = RelToInsCode == "01" || (RelToInsCode != null && RelToInsCode.ToLower() == "self");


    //            if ((SelectedItem.InsuranceKey == null) || (SelectedItem.InsuranceKey <= 0))
    //            {
    //                string[] memberNames = { "InsuranceKey" };
    //                SelectedItem.ValidationErrors.Add(new ValidationResult("Insurance name must be entered", memberNames));
    //                AllValid = false;
    //            }

    //            bool InsLastNameIsSelf = String.IsNullOrEmpty(SelectedItem.InsuredLastName)
    //                ? false
    //                : (SelectedItem.InsuredLastName.ToLower() == "self");

    //            if (RelToInsSelf && !InsLastNameIsSelf)
    //            {
    //                string[] memberNames = { "InsuredLastName" };
    //                SelectedItem.ValidationErrors.Add(
    //                    new ValidationResult("Policy Holder must be Self when Relationship to Insured is Patient.",
    //                        memberNames));
    //                AllValid = false;
    //            }

    //            if (!RelToInsSelf && InsLastNameIsSelf)
    //            {
    //                string[] memberNames = { "RelToInsured" };
    //                SelectedItem.ValidationErrors.Add(
    //                    new ValidationResult("Relationship to Insured must be Patient when Policy Holder is Self.",
    //                        memberNames));
    //                AllValid = false;
    //            }

    //            foreach (InsuranceVerifyHistory ivh in SelectedItem.InsuranceVerifyHistory)
    //            {
    //                AllValid = AllValid && ivh.Validate();

    //                foreach (InsuranceVerifyHistoryDetail dtl in ivh.InsuranceVerifyHistoryDetail)
    //                    AllValid = AllValid && dtl.Validate();
    //            }

    //            return AllValid;
    //        }

    //        public override bool HasChanges()
    //        {
    //            bool childHasChanges = false;
    //            if (Model != null)
    //            {
    //                if (Model.Context != null)
    //                {
    //                    if (Model.Context.InsuranceEligibilities != null)
    //                    {
    //                        childHasChanges = Model.Context.InsuranceEligibilities.HasChanges;
    //                    }

    //                    if (!childHasChanges)
    //                    {
    //                        if (Model.Context.InsuranceVerifyHistories != null)
    //                        {
    //                            childHasChanges = Model.Context.InsuranceVerifyHistories.HasChanges
    //                                              || Model.Context.InsuranceVerifyHistories.Any(i => i.IsNew);
    //                        }
    //                    }

    //                    if (!childHasChanges)
    //                    {
    //                        if (Model.Context.InsuranceVerifyHistoryDetails != null)
    //                        {
    //                            childHasChanges = Model.Context.InsuranceVerifyHistoryDetails.HasChanges
    //                                              || Model.Context.InsuranceVerifyHistoryDetails.Any(i => i.IsNew);
    //                        }
    //                    }
    //                }
    //            }

    //            bool hasChanges = (SelectedItem.IsNew || SelectedItem.HasChanges || childHasChanges);

    //            if (!hasChanges)
    //            {
    //                hasChanges = SelectedItem.InsuranceVerificationRequest.Where(p => p.IsNew || p.HasChanges).Any();
    //            }

    //            return hasChanges;
    //        }

    //        private bool _InsuranceDisplayMode = true;

    //        public bool InsuranceDisplayModeDetails
    //        {
    //            get { return _InsuranceDisplayMode; }
    //            set
    //            {
    //                _InsuranceDisplayMode = value;
    //                RaisePropertyChanged("InsuranceDisplayModeDetails");
    //                RaisePropertyChanged("InsuranceDisplayModeRequests");
    //            }
    //        }

    //        public bool InsuranceDisplayModeRequests
    //        {
    //            get { return !_InsuranceDisplayMode; }
    //            set { InsuranceDisplayModeDetails = !value; }
    //        }
    //    }

    //    public class PatientInfectionUserControlBase : ChildControlBase<PatientInfectionUserControl, PatientInfection>, IDynamicFormControl
    //    {
    //        public PatientInfectionUserControlBase()
    //        {
    //            PopupDataTemplate = "PatientInfectionPopupDataTemplate";

    //            AddPressed += OnAddPressed;
    //            CancelPressed += OnCancelPressed;
    //            OKPressed += OnOKPressed;
    //            EditPressed += OnEditPressed;
    //            PostAddPressed += OnPostAddPressed;
    //            OKPressedPreValidate += OnOkPressedPreValidate;
    //            FilteredItemsSourceChanged += OnFilteredItemsSourceChanged;
    //            ParentViewModelChanged += OnParentViewModelChanged;
    //            ItemSelected += Update_ItemSelected;
    //            EncounterChanged += OnEncounterChanged;
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            CancelPressed -= OnCancelPressed;
    //            OKPressed -= OnOKPressed;
    //            EditPressed -= OnEditPressed;
    //            PostAddPressed -= OnPostAddPressed;
    //            OKPressedPreValidate -= OnOkPressedPreValidate;
    //            FilteredItemsSourceChanged -= OnFilteredItemsSourceChanged;
    //            ParentViewModelChanged -= OnParentViewModelChanged;
    //            ItemSelected -= Update_ItemSelected;
    //            EncounterChanged -= OnEncounterChanged;
    //            if (FilteredInfections != null)
    //            {
    //                FilteredInfections.Filter -= FilteredInfections_Filter;
    //                FilteredInfections.Source = null;
    //                FilteredInfections = null;
    //            }

    //            var df = ParentViewModel as DynamicFormViewModel;
    //            if ((df != null) && (df.DFControlManager != null))
    //            {
    //                df.DFControlManager.UnRegisterControl(this);
    //            }

    //            base.Cleanup();
    //        }

    //        private void OnEncounterChanged(object sender, EventArgs e)
    //        {
    //            RaisePropertyChanged("HasEncounter");
    //        }

    //        public bool HasEncounter => (Encounter != null);

    //        public bool RefreshAfterSave()
    //        {
    //            if (FilteredInfections == null || FilteredInfections.View == null)
    //            {
    //                return false;
    //            }

    //            FilteredInfections.View.Refresh();
    //            return true;
    //        }

    //        private CollectionViewSource _FilteredInfections = new CollectionViewSource();

    //        public CollectionViewSource FilteredInfections
    //        {
    //            get { return _FilteredInfections; }
    //            set
    //            {
    //                _FilteredInfections = value;
    //                RaisePropertyChanged("FilteredInfections");
    //            }
    //        }

    //        public bool HasMultipleServicelines
    //        {
    //            get
    //            {
    //                if (HistoricalInfections == null)
    //                {
    //                    return false;
    //                }

    //                return HistoricalInfections.Select(s => s.ServiceLineDescription).Distinct().Count() > 1;
    //            }
    //        }

    //        public bool HasHistoricalData
    //        {
    //            get
    //            {
    //                if (HistoricalInfections == null)
    //                {
    //                    return false;
    //                }

    //                return HistoricalInfections.Any();
    //            }
    //        }

    //        private List<AdmissionInfection> _HistoricalInfections = new List<AdmissionInfection>();

    //        public List<AdmissionInfection> HistoricalInfections
    //        {
    //            get { return _HistoricalInfections; }
    //            set
    //            {
    //                _HistoricalInfections = value;
    //                RaisePropertyChanged("HistoricalInfections");
    //                RaisePropertyChanged("HasHistoricalData");
    //            }
    //        }

    //        void Update_ItemSelected(object sender, EventArgs e)
    //        {
    //        }

    //        public void OnParentViewModelChanged(object sender, UserControlBaseEventArgs<PatientInfection> e)
    //        {
    //            RegisterWithControlManager();
    //        }

    //        private void RegisterWithControlManager()
    //        {
    //            if (ParentViewModel != null)
    //            {
    //                var df = ParentViewModel as DynamicFormViewModel;
    //                if (df != null)
    //                {
    //                    df.DFControlManager.RegisterControl(this);
    //                }
    //            }
    //        }

    //        public void OnOkPressedPreValidate(object sender, UserControlBaseEventArgs<PatientInfection> e)
    //        {
    //            String sections = "";
    //            e.Cancel = !Validate(out sections);
    //        }

    //        public void OnFilteredItemsSourceChanged(object sender, UserControlBaseEventArgs<PatientInfection> e)
    //        {
    //            if (ItemsSource != null)
    //            {
    //                FilteredInfections.Source = ItemsSource;
    //                FilteredInfections.SortDescriptions.Add(new SortDescription("ConfirmationDate",
    //                    ListSortDirection.Ascending));
    //                FilteredInfections.SortDescriptions.Add(
    //                    new SortDescription("ResolvedDate", ListSortDirection.Ascending));
    //                FilteredInfections.Filter += FilteredInfections_Filter;
    //            }
    //        }

    //        private void FilteredInfections_Filter(object sender, FilterEventArgs es)
    //        {
    //            var enc = Encounter;
    //            PatientInfection pl = es.Item as PatientInfection;
    //            if (enc == null)
    //            {
    //                es.Accepted = !pl.Superceded;
    //            }
    //            else
    //            {
    //                es.Accepted = pl.IsNew || enc.EncounterPatientInfection.Any(a =>
    //                    a.EncounterKey == enc.EncounterKey && a.PatientInfectionKey == pl.PatientInfectionKey);
    //            }
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<PatientInfection> e)
    //        {
    //            ParentViewModel.PopupDataContext = this;
    //            SelectedItem = e.Entity;
    //            SelectedItem.PatientKey = CurrentPatient.PatientKey;
    //        }

    //        public void OnPostAddPressed(object sender, UserControlBaseEventArgs<PatientInfection> e)
    //        {
    //            SelectedItem = e.Entity;
    //            SelectedItem.PatientKey = CurrentPatient.PatientKey;
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<PatientInfection> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            FilteredInfections.View.Refresh();
    //            SelectedItem = null;
    //            if (ItemsSource != null && FilteredInfections.View != null)
    //            {
    //                SelectedItem = ItemsSource.FirstOrDefault(s => FilteredInfections.View.Contains(s));
    //            }

    //            RaisePropertyChanged("SelectedItem");
    //        }

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<PatientInfection> e)
    //        {
    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnOKPressed(object sender, UserControlBaseEventArgs<PatientInfection> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            FilteredInfections.View.Refresh();
    //            RaisePropertyChanged("FilteredInfections");
    //            RaisePropertyChanged("FilteredItemsSource");
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientInfectionUserControl), null);

    //        public override void RemoveFromModel(PatientInfection entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //            RegisterWithControlManager();
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            // SAVE - regardless of whethe command = OK or CANCEL...
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            RegisterWithControlManager();
    //        }

    //        public bool Validate(out string SubSections)
    //        {
    //            bool AllValid = true;
    //            SubSections = string.Empty;
    //            if (SelectedItem == null)
    //            {
    //                return true;
    //            }

    //            SelectedItem.ValidationErrors.Clear();
    //            SelectedItem.Validate();

    //            if (SelectedItem.PresentAtSOCROC == null)
    //            {
    //                AllValid = false;
    //            }

    //            if (SelectedItem.InfectionSiteKey == 0)
    //            {
    //                string[] memberNames = { "InfectionSiteKey" };
    //                SelectedItem.ValidationErrors.Add(new ValidationResult("The Infection Site field is required.",
    //                    memberNames));
    //                AllValid = false;
    //            }

    //            if (string.IsNullOrWhiteSpace(SelectedItem.TransmissionPrecautionsFormatted))
    //            {
    //                string[] memberNames = { "TransmissionPrecautionsFormatted" };
    //                SelectedItem.ValidationErrors.Add(
    //                    new ValidationResult("The Transmission Precautions field is required.", memberNames));
    //                AllValid = false;
    //            }

    //            if (SelectedItem.ConfirmationDate.HasValue &&
    //                SelectedItem.ConfirmationDate.Value.Date > DateTime.Today.Date)
    //            {
    //                string[] memberNames = { "ConfirmationDate" };
    //                SelectedItem.ValidationErrors.Add(
    //                    new ValidationResult("The Confirmation Date field cannot be in the future.", memberNames));
    //                AllValid = false;
    //            }

    //            if (SelectedItem.ResolvedDate.HasValue && SelectedItem.ResolvedDate.Value.Date > DateTime.Today.Date)
    //            {
    //                string[] memberNames = { "ResolvedDate" };
    //                SelectedItem.ValidationErrors.Add(
    //                    new ValidationResult("The Date Resolved field cannot be in the future.", memberNames));
    //                AllValid = false;
    //            }

    //            if (SelectedItem.ShowInfectionSiteOther && string.IsNullOrWhiteSpace(SelectedItem.InfectionSiteOther))
    //            {
    //                string[] memberNames = { "InfectionSiteOther" };
    //                SelectedItem.ValidationErrors.Add(new ValidationResult(
    //                    "The 'Other' Infection Site field is required when 'Other' is selected.", memberNames));
    //                AllValid = false;
    //            }

    //            if (SelectedItem.ShowInfectionPathogenOther &&
    //                string.IsNullOrWhiteSpace(SelectedItem.InfectionPathogenOther))
    //            {
    //                string[] memberNames = { "InfectionPathogenOther" };
    //                SelectedItem.ValidationErrors.Add(
    //                    new ValidationResult("The 'Other' Pathogen field is required when 'Other' is selected.",
    //                        memberNames));
    //                AllValid = false;
    //            }

    //            if (SelectedItem.ShowInfectionConfirmationOther &&
    //                string.IsNullOrWhiteSpace(SelectedItem.InfectionConfirmationOther))
    //            {
    //                string[] memberNames = { "InfectionConfirmationOther" };
    //                SelectedItem.ValidationErrors.Add(new ValidationResult(
    //                    "The 'Other' Confirmation of Infection field is required when 'Other' is selected.", memberNames));
    //                AllValid = false;
    //            }

    //            return AllValid;
    //        }

    //        public Patient CurrentPatient
    //        {
    //            get { return (Patient)GetValue(CurrentPatientProperty); }
    //            set { SetValue(CurrentPatientProperty, value); }
    //        }

    //        public static readonly DependencyProperty CurrentPatientProperty = DependencyProperty.Register("CurrentPatient",
    //            typeof(Patient), typeof(PatientInfectionUserControl), new PropertyMetadata(null, CurrentPatientChanged));

    //        private static void CurrentPatientChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    //        {
    //            PatientInfectionUserControlBase me = sender as PatientInfectionUserControlBase;
    //            if (me == null)
    //            {
    //                return;
    //            }

    //            if (me.ItemsSource != null)
    //            {
    //                foreach (PatientInfection pph in me.ItemsSource) pph.Patient = me.CurrentPatient;
    //            }

    //            if (me.CurrentPatient != null && me.CurrentPatient.Admission != null)
    //            {
    //                me.HistoricalInfections = me.CurrentPatient.Admission.SelectMany(s => s.AdmissionInfection).ToList();
    //                if (me.HistoricalInfections != null)
    //                {
    //                    me.HistoricalInfections = me.HistoricalInfections.Where(s => s.Superceded == false).ToList();
    //                }
    //            }
    //            else
    //            {
    //                me.HistoricalInfections = new List<AdmissionInfection>();
    //            }
    //        }
    //    }

    //    public class PatientAdverseEventUserControlBase : ChildControlBase<PatientAdverseEventUserControl, PatientAdverseEvent>, IDynamicFormControl
    //    {
    //        public PatientAdverseEventUserControlBase()
    //        {
    //            PopupDataTemplate = "PatientAdverseEventPopupDataTemplate";

    //            AddPressed += OnAddPressed;
    //            CancelPressed += OnCancelPressed;
    //            OKPressed += OnOKPressed;
    //            EditPressed += OnEditPressed;
    //            PostAddPressed += OnPostAddPressed;
    //            OKPressedPreValidate += OnOkPressedPreValidate;
    //            FilteredItemsSourceChanged += OnFilteredItemsSourceChanged;
    //            ParentViewModelChanged += OnParentViewModelChanged;
    //            ItemSelected += Update_ItemSelected;
    //            EncounterChanged += OnEncounterChanged;
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            CancelPressed -= OnCancelPressed;
    //            OKPressed -= OnOKPressed;
    //            EditPressed -= OnEditPressed;
    //            PostAddPressed -= OnPostAddPressed;
    //            OKPressedPreValidate -= OnOkPressedPreValidate;
    //            FilteredItemsSourceChanged -= OnFilteredItemsSourceChanged;
    //            ParentViewModelChanged -= OnParentViewModelChanged;
    //            ItemSelected -= Update_ItemSelected;
    //            EncounterChanged -= OnEncounterChanged;
    //            if (FilteredAdverseEvents != null)
    //            {
    //                FilteredAdverseEvents.Filter -= FilteredAdverseEvents_Filter;
    //                FilteredAdverseEvents.Source = null;
    //                FilteredAdverseEvents = null;
    //            }

    //            var df = ParentViewModel as DynamicFormViewModel;
    //            if ((df != null) && (df.DFControlManager != null))
    //            {
    //                df.DFControlManager.UnRegisterControl(this);
    //            }

    //            base.Cleanup();
    //        }

    //        private void OnEncounterChanged(object sender, EventArgs e)
    //        {
    //            RaisePropertyChanged("HasEncounter");
    //        }

    //        public bool HasEncounter => (Encounter != null);

    //        public bool RefreshAfterSave()
    //        {
    //            if (FilteredAdverseEvents == null || FilteredAdverseEvents.View == null)
    //            {
    //                return false;
    //            }

    //            FilteredAdverseEvents.View.Refresh();
    //            return true;
    //        }

    //        private CollectionViewSource _FilteredAdverseEvents = new CollectionViewSource();

    //        public CollectionViewSource FilteredAdverseEvents
    //        {
    //            get { return _FilteredAdverseEvents; }
    //            set
    //            {
    //                _FilteredAdverseEvents = value;
    //                RaisePropertyChanged("FilteredAdverseEvents");
    //            }
    //        }

    //        void Update_ItemSelected(object sender, EventArgs e)
    //        {
    //        }

    //        public void OnParentViewModelChanged(object sender, UserControlBaseEventArgs<PatientAdverseEvent> e)
    //        {
    //            RegisterWithControlManager();
    //        }

    //        private void RegisterWithControlManager()
    //        {
    //            if (ParentViewModel != null)
    //            {
    //                var df = ParentViewModel as DynamicFormViewModel;
    //                if (df != null)
    //                {
    //                    df.DFControlManager.RegisterControl(this);
    //                }
    //            }
    //        }

    //        public void OnOkPressedPreValidate(object sender, UserControlBaseEventArgs<PatientAdverseEvent> e)
    //        {
    //            if (SelectedItem == null)
    //            {
    //                return;
    //            }

    //            if (SelectedItem.EventTypeKey <= 0)
    //            {
    //                SelectedItem.EventTypeKey = null;
    //            }

    //            if (SelectedItem.EventDate == DateTime.MinValue)
    //            {
    //                SelectedItem.EventDate = null;
    //            }

    //            if (SelectedItem.EventDate.HasValue)
    //            {
    //                SelectedItem.EventDate = ((DateTime)SelectedItem.EventDate).Date;
    //            }

    //            if (string.IsNullOrWhiteSpace(SelectedItem.Outcome))
    //            {
    //                SelectedItem.Outcome = null;
    //            }

    //            if ((SelectedItem.WitnessedByAgency == null) || (SelectedItem.WitnessedByAgency == false))
    //            {
    //                SelectedItem.WitnessedBy = null;
    //            }

    //            if (string.IsNullOrWhiteSpace(SelectedItem.Comment))
    //            {
    //                SelectedItem.Comment = null;
    //            }
    //        }

    //        public void OnFilteredItemsSourceChanged(object sender, UserControlBaseEventArgs<PatientAdverseEvent> e)
    //        {
    //            if (ItemsSource != null)
    //            {
    //                FilteredAdverseEvents.Source = ItemsSource;
    //                FilteredAdverseEvents.SortDescriptions.Add(
    //                    new SortDescription("EventDate", ListSortDirection.Ascending));
    //                FilteredAdverseEvents.SortDescriptions.Add(new SortDescription("DocumentedDateTime",
    //                    ListSortDirection.Ascending));
    //                FilteredAdverseEvents.Filter += FilteredAdverseEvents_Filter;
    //            }
    //        }

    //        private void FilteredAdverseEvents_Filter(object sender, FilterEventArgs es)
    //        {
    //            var enc = Encounter;
    //            PatientAdverseEvent pae = es.Item as PatientAdverseEvent;
    //            if (enc == null)
    //            {
    //                es.Accepted = !pae.Superceded;
    //            }
    //            else
    //            {
    //                es.Accepted = pae.IsNew || enc.EncounterPatientAdverseEvent.Any(a =>
    //                    a.EncounterKey == enc.EncounterKey && a.PatientAdverseEventKey == pae.PatientAdverseEventKey);
    //            }
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<PatientAdverseEvent> e)
    //        {
    //            ParentViewModel.PopupDataContext = this;
    //            SelectedItem = e.Entity;
    //            SelectedItem.PatientKey = CurrentPatient.PatientKey;
    //        }

    //        public void OnPostAddPressed(object sender, UserControlBaseEventArgs<PatientAdverseEvent> e)
    //        {
    //            SelectedItem = e.Entity;
    //            SelectedItem.DocumentedBy = WebContext.Current.User.MemberID;
    //            SelectedItem.DocumentedDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
    //            Deployment.Current.Dispatcher.BeginInvoke(() => { SelectedItem.EventTypeKey = 0; });
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<PatientAdverseEvent> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            FilteredAdverseEvents.View.Refresh();
    //            SelectedItem = null;
    //            if (ItemsSource != null && FilteredAdverseEvents.View != null)
    //            {
    //                SelectedItem = ItemsSource.FirstOrDefault(s => FilteredAdverseEvents.View.Contains(s));
    //            }

    //            RaisePropertyChanged("SelectedItem");
    //        }

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<PatientAdverseEvent> e)
    //        {
    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnOKPressed(object sender, UserControlBaseEventArgs<PatientAdverseEvent> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            FilteredAdverseEvents.View.Refresh();
    //            RaisePropertyChanged("FilteredAdverseEvents");
    //            RaisePropertyChanged("FilteredItemsSource");
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientAdverseEventUserControl), null);

    //        public override void RemoveFromModel(PatientAdverseEvent entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //            RegisterWithControlManager();
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            // SAVE - regardless of whethe command = OK or CANCEL...
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            RegisterWithControlManager();
    //        }

    //        public override bool Validate()
    //        {
    //            bool AllValid = true;
    //            if (SelectedItem == null)
    //            {
    //                return AllValid;
    //            }

    //            DateTime today = DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified);

    //            if (SelectedItem.EventDate.HasValue && SelectedItem.EventDate.Value.Date > today)
    //            {
    //                string[] memberNames = { "EventDate" };
    //                SelectedItem.ValidationErrors.Add(new ValidationResult("The Event Date field cannot be in the future.",
    //                    memberNames));
    //                AllValid = false;
    //            }

    //            if ((SelectedItem.WitnessedByAgency == true) && (SelectedItem.WitnessedBy == null))
    //            {
    //                string[] memberNames = { "WitnessedBy" };
    //                SelectedItem.ValidationErrors.Add(new ValidationResult(
    //                    "The Witnessed by Whom field is required when Witnessed by Agency is Yes.", memberNames));
    //                AllValid = false;
    //            }

    //            return AllValid;
    //        }

    //        public Patient CurrentPatient
    //        {
    //            get { return (Patient)GetValue(CurrentPatientProperty); }
    //            set { SetValue(CurrentPatientProperty, value); }
    //        }

    //        public static readonly DependencyProperty CurrentPatientProperty = DependencyProperty.Register("CurrentPatient",
    //            typeof(Patient), typeof(PatientAdverseEventUserControl), new PropertyMetadata(null, CurrentPatientChanged));

    //        private static void CurrentPatientChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    //        {
    //            PatientAdverseEventUserControlBase me = sender as PatientAdverseEventUserControlBase;
    //            if (me == null)
    //            {
    //                return;
    //            }

    //            if (me.ItemsSource != null)
    //            {
    //                foreach (PatientAdverseEvent pae in me.ItemsSource) pae.Patient = me.CurrentPatient;
    //            }
    //        }
    //    }

    //    public class PatientLabUserControlBase : ChildControlBase<PatientLabUserControl, PatientLab>
    //    {
    //        public PatientLabUserControlBase()
    //        {
    //            PopupDataTemplate = "PatientLabPopupDataTemplate";

    //            AddPressed += OnAddPressed;
    //            CancelPressed += OnCancelPressed;
    //            OKPressed += OnOKPressed;
    //            EditPressed += OnEditPressed;
    //            FilteredLabs.SortDescriptions.Add(new SortDescription("OrderDate", ListSortDirection.Descending));
    //            FilteredLabs.SortDescriptions.Add(new SortDescription("Test", ListSortDirection.Descending));
    //            SetupFilterLabs();
    //            ItemSelected += Update_ItemSelected;
    //            Messenger.Default.Register<int>(this, "RefreshMaintenancePatient", i => FilterLabs());
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            CancelPressed -= OnCancelPressed;
    //            OKPressed -= OnOKPressed;
    //            EditPressed -= OnEditPressed;
    //            ItemSelected -= Update_ItemSelected;
    //            if (FilteredLabs != null)
    //            {
    //                FilteredLabs.Filter -= FilteredLabs_Filter;
    //            }

    //            Messenger.Default.Unregister(this);
    //            base.Cleanup();
    //        }

    //        void Update_ItemSelected(object sender, EventArgs e)
    //        {
    //            FilteredLabs.Source = ItemsSource;
    //        }

    //        public void FilterLabs()
    //        {
    //            if (FilteredLabs != null && FilteredLabs.View != null)
    //            {
    //                FilteredLabs.View.Refresh();
    //            }
    //        }

    //        public void SetupFilterLabs()
    //        {
    //            FilteredLabs.Filter += FilteredLabs_Filter;
    //        }

    //        private void FilteredLabs_Filter(object s, FilterEventArgs e)
    //        {
    //            PatientLab pl = e.Item as PatientLab;
    //            if (Encounter == null)
    //            {
    //                if (pl.Superceded)
    //                {
    //                    e.Accepted = false;
    //                    return;
    //                }
    //            }
    //            else
    //            {
    //                if (pl.IsNew == false)
    //                {
    //                    EncounterLab el = Encounter.EncounterLab.FirstOrDefault(p => p.PatientLab.PatientLabKey == pl.PatientLabKey);
    //                    if (el == null)
    //                    {
    //                        e.Accepted = false;
    //                        return;
    //                    }
    //                }
    //            }

    //            e.Accepted = !string.IsNullOrEmpty(pl.Category) && (AllChecked ||
    //                                                                (LabChecked && pl.Category.Equals("Laboratory")) ||
    //                                                                (ABGChecked && pl.Category.Equals("ABG")) ||
    //                                                                (PulmChecked && pl.Category.Equals("Pulmonary")) ||
    //                                                                (OtherChecked && pl.Category.Equals("Other")));
    //        }


    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<PatientLab> e)
    //        {
    //            if ((OrderEntryManager != null) && (OrderEntryManager.CurrentIOrderEntry != null))
    //            {
    //                e.Entity.OrderingPhysicianKey = OrderEntryManager.CurrentIOrderEntry.SigningPhysicianKey;
    //                e.Entity.OrderDate = OrderEntryManager.CurrentIOrderEntry.CompletedDate?.DateTime;
    //            }

    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<PatientLab> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<PatientLab> e)
    //        {
    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnOKPressed(object sender, UserControlBaseEventArgs<PatientLab> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            FilterLabs();
    //            if (OrderEntryManager != null)
    //            {
    //                OrderEntryManager.UpdateGeneratedOrderText();
    //            }
    //        }

    //        private bool _AllChecked = true;

    //        public bool AllChecked
    //        {
    //            get { return _AllChecked; }
    //            set
    //            {
    //                _AllChecked = value;
    //                RaisePropertyChanged("AllChecked");
    //                if (value)
    //                {
    //                    LabChecked = ABGChecked = PulmChecked = OtherChecked = false;
    //                }

    //                FilterLabs();
    //            }
    //        }

    //        private bool _LabChecked;

    //        public bool LabChecked
    //        {
    //            get { return _LabChecked; }
    //            set
    //            {
    //                _LabChecked = value;
    //                RaisePropertyChanged("LabChecked");
    //                if (value)
    //                {
    //                    AllChecked = false;
    //                }

    //                FilterLabs();
    //            }
    //        }

    //        private bool _ABGChecked;

    //        public bool ABGChecked
    //        {
    //            get { return _ABGChecked; }
    //            set
    //            {
    //                _ABGChecked = value;
    //                RaisePropertyChanged("ABGChecked");
    //                if (value)
    //                {
    //                    AllChecked = false;
    //                }

    //                FilterLabs();
    //            }
    //        }

    //        private bool _PulmChecked;

    //        public bool PulmChecked
    //        {
    //            get { return _PulmChecked; }
    //            set
    //            {
    //                _PulmChecked = value;
    //                RaisePropertyChanged("PulmChecked");
    //                if (value)
    //                {
    //                    AllChecked = false;
    //                }

    //                FilterLabs();
    //            }
    //        }

    //        private bool _OtherChecked;

    //        public bool OtherChecked
    //        {
    //            get { return _OtherChecked; }
    //            set
    //            {
    //                _OtherChecked = value;
    //                RaisePropertyChanged("OtherChecked");
    //                if (value)
    //                {
    //                    AllChecked = false;
    //                }

    //                FilterLabs();
    //            }
    //        }

    //        private CollectionViewSource _FilteredLabs = new CollectionViewSource();

    //        public CollectionViewSource FilteredLabs
    //        {
    //            get { return _FilteredLabs; }
    //            set
    //            {
    //                _FilteredLabs = value;
    //                RaisePropertyChanged("FilteredLabs");
    //            }
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientLabUserControl), null);

    //        public override void RemoveFromModel(PatientLab entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            // SAVE - regardless of whethe command = OK or CANCEL...
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }
    //        }
    //    }

    //    public class EncounterSupplyUserControlBase : ChildControlBase<EncounterSupplyUserControl, EncounterSupply>
    //    {
    //        public RelayCommand<EncounterSupply> EncounterSupplyEditItem_Command { get; protected set; }

    //        public IEnumerable<Supply> AvailableSupplies
    //        {
    //            get
    //            {
    //                return SupplyCache.GetSupplies()
    //                    .Where(sup =>
    //                        (sup.EffectiveFrom.Date <= Encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date)
    //                        && ((sup.EffectiveThru == null)
    //                            || (sup.EffectiveThru.HasValue && sup.EffectiveThru.Value.Date >=
    //                                Encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date)
    //                        )
    //                        && (!sup.ExcludeFromSelection)
    //                    )
    //                    .OrderBy(s => s.Description1);
    //            }
    //        }

    //        public EncounterSupplyUserControlBase()
    //        {
    //            PopupDataTemplate = "SuppliesPopupDataTemplate";

    //            AddPressed += OnAddPressed;
    //            CancelPressed += OnCancelPressed;
    //            OKPressed += OnOKPressed;
    //            EditPressed += OnEditPressed;

    //            EncounterSupplyEditItem_Command = new RelayCommand<EncounterSupply>(item =>
    //            {
    //                if (item == null)
    //                {
    //                    return;
    //                }

    //                IsEdit = true;
    //                SelectedItem = item;

    //                if (SelectedItem != null)
    //                {
    //                    SelectedItem.BeginEditting();
    //                }

    //                PopupDataTemplate = "SuppliesPopupDataTemplate";
    //                ParentViewModel.PopupDataContext = this;
    //            });
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            CancelPressed -= OnCancelPressed;
    //            OKPressed -= OnOKPressed;
    //            EditPressed -= OnEditPressed;
    //            base.Cleanup();
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<EncounterSupply> e)
    //        {
    //            if (e != null)
    //            {
    //                e.Entity.SequenceNo = Encounter.EncounterSupply.Count();
    //                e.Entity.SequenceNo = e.Entity.SequenceNo == 0 ? 1 : ++e.Entity.SequenceNo;
    //                e.Entity.LocationKey = CodeLookupCache.GetKeyFromCode("ILOC", "MAIN");
    //            }

    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<EncounterSupply> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<EncounterSupply> e)
    //        {
    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnOKPressed(object sender, UserControlBaseEventArgs<EncounterSupply> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            if (OrderEntryManager != null)
    //            {
    //                OrderEntryManager.UpdateGeneratedOrderText();
    //            }
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(EncounterSupplyUserControl), null);

    //        public override void RemoveFromModel(EncounterSupply entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            // SAVE - regardless of whethe command = OK or CANCEL...
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }
    //        }
    //    }

    //    public class EncounterVendorUserControlBase : ChildControlBase<EncounterVendorUserControl, EncounterVendor>
    //    {
    //        public RelayCommand VendorSearchCommand { get; protected set; }
    //        public RelayCommand<EncounterVendor> EncounterVendorEditItem_Command { get; protected set; }

    //        public IEnumerable<Vendor> AvailableVendors
    //        {
    //            get
    //            {
    //                return VendorCache.GetVendors()
    //                    .Where(v => v.Inactive == false)
    //                    .OrderBy(v => v.VendorName);
    //            }
    //        }

    //        public EncounterVendorUserControlBase()
    //        {
    //            PopupDataTemplate = "VendorPopupDataTemplate";

    //            AddPressed += OnAddPressed;
    //            CancelPressed += OnCancelPressed;
    //            OKPressed += OnOKPressed;
    //            EditPressed += OnEditPressed;

    //            EncounterVendorEditItem_Command = new RelayCommand<EncounterVendor>(item =>
    //            {
    //                if (item == null)
    //                {
    //                    return;
    //                }

    //                IsEdit = true;
    //                SelectedItem = item;

    //                if (SelectedItem != null)
    //                {
    //                    SelectedItem.BeginEditting();
    //                }

    //                PopupDataTemplate = "VendorPopupDataTemplate";
    //                ParentViewModel.PopupDataContext = this;
    //            });

    //            VendorSearchCommand = new RelayCommand(() =>
    //                {
    //                    SearchDialog window = new SearchDialog
    //                    {
    //                        CurrentSearchOverride = "Vendor"
    //                    };
    //                    SearchPanelViewModel vm = window.ParentViewModel;
    //                    if (vm != null)
    //                    {
    //                        vm.ItemSelected += (s, e) =>
    //                        {
    //                            Vendor selectedVendor = null;
    //                            int id = 0;
    //                            if (int.TryParse(e.ID, out id))
    //                            {
    //                                selectedVendor = e.Object as Vendor;
    //                            }

    //                            if ((selectedVendor != null) && (SelectedItem != null))
    //                            {
    //                                SelectedItem.VendorKey = selectedVendor.VendorKey;
    //                                SelectedItem.VendorName = selectedVendor.VendorName;
    //                                SelectedItem.VendorType = selectedVendor.VendorType;
    //                                SelectedItem.ContactFirstName = selectedVendor.ContactFirstName;
    //                                SelectedItem.ContactLastName = selectedVendor.ContactLastName;
    //                                SelectedItem.Number = selectedVendor.Number;
    //                                SelectedItem.PhoneExtension = selectedVendor.PhoneExtension;
    //                                SelectedItem.Fax = selectedVendor.Fax;
    //                            }

    //                            window.Close();
    //                        };
    //                    }

    //                    window.InitSearch();
    //                    window.Show();
    //                },
    //                () => true);
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            CancelPressed -= OnCancelPressed;
    //            OKPressed -= OnOKPressed;
    //            EditPressed -= OnEditPressed;
    //            base.Cleanup();
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<EncounterVendor> e)
    //        {
    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<EncounterVendor> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<EncounterVendor> e)
    //        {
    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnOKPressed(object sender, UserControlBaseEventArgs<EncounterVendor> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(EncounterVendorUserControl), null);

    //        public override void RemoveFromModel(EncounterVendor entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            // SAVE - regardless of whethe command = OK or CANCEL...
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }
    //        }
    //    }

    //    public class PatientImmunizationUserControlBase : ChildControlBase<PatientImmunizationUserControl, PatientImmunization>
    //    {
    //        public PatientImmunizationUserControlBase()
    //        {
    //            PopupDataTemplate = "PatientImmunizationPopupDataTemplate";

    //            AddPressed += OnAddPressed;
    //            EditPressed += OnEditPressed;
    //            CancelPressed += OnCancelPressed;
    //            OKPressed += OnOKPressed;

    //            Loaded += PatientImmunizationUserControlBase_Loaded;
    //        }

    //        void PatientImmunizationUserControlBase_Loaded(object sender, RoutedEventArgs e)
    //        {
    //            FilteredImmunizations.Source = ItemsSource;

    //            RaisePropertyChanged("FilteredImmunizations");
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            EditPressed -= OnEditPressed;
    //            CancelPressed -= OnCancelPressed;
    //            OKPressed -= OnOKPressed;
    //            Loaded -= PatientImmunizationUserControlBase_Loaded;

    //            base.Cleanup();
    //        }

    //        public override void RemoveFromModel()
    //        {
    //            base.RemoveFromModel();
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<PatientImmunization> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public void OnOKPressed(object sender, UserControlBaseEventArgs<PatientImmunization> e)
    //        {
    //            foreach (var pp in CurrentPatient.PatientImmunization) pp.EndEditting();

    //            FilteredImmunizations.SortDescriptions.Add(new SortDescription("PatientImmunizationKey",
    //                ListSortDirection.Ascending));
    //            FilteredImmunizations.View.Refresh();
    //            RaisePropertyChanged("FilteredImmunizations");
    //            RaisePropertyChanged("FilteredImmunizations.View");
    //            ParentViewModel.PopupDataContext = null;
    //        }


    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<PatientImmunization> e)
    //        {
    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<PatientImmunization> e)
    //        {
    //            e.Entity.Patient = CurrentPatient;

    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            // SAVE - regardless of whethe command = OK or CANCEL...
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }
    //        }

    //        private CollectionViewSource _FilteredImmunizations = new CollectionViewSource();

    //        public CollectionViewSource FilteredImmunizations
    //        {
    //            get { return _FilteredImmunizations; }
    //            set
    //            {
    //                _FilteredImmunizations = value;
    //                RaisePropertyChanged("FilteredImmunizations");
    //            }
    //        }

    //        public Patient CurrentPatient
    //        {
    //            get { return (Patient)GetValue(CurrentPatientProperty); }
    //            set { SetValue(CurrentPatientProperty, value); }
    //        }

    //        public static readonly DependencyProperty CurrentPatientProperty = DependencyProperty.Register("CurrentPatient",
    //            typeof(Patient), typeof(PatientImmunizationUserControl), new PropertyMetadata(null, CurrentPatientChanged));

    //        private static void CurrentPatientChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    //        {
    //            PatientImmunizationUserControlBase me = sender as PatientImmunizationUserControlBase;
    //            if (me == null)
    //            {
    //                return;
    //            }

    //            if (me.ItemsSource != null)
    //            {
    //                foreach (PatientImmunization pph in me.ItemsSource) pph.Patient = me.CurrentPatient;
    //            }
    //        }

    //        public IPatientService Model
    //        {
    //            get
    //            {
    //                return (IPatientService)GetValue(ModelProperty);
    //            }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public override bool Validate()
    //        {
    //            SelectedItem.ValidationErrors.Clear();

    //            bool Success = true;
    //            if (!SelectedItem.Immunization.HasValue)
    //            {
    //                Success = false;
    //                SelectedItem.ValidationErrors.Add(new ValidationResult("Immunization is required.",
    //                    new[] { "Immunization" }));
    //            }

    //            if (!SelectedItem.ImmunizedBy.HasValue)
    //            {
    //                Success = false;
    //                SelectedItem.ValidationErrors.Add(new ValidationResult("Offered or Provided By is required.",
    //                    new[] { "ImmunizedBy" }));
    //            }

    //            if (!SelectedItem.DateReceived.HasValue && !SelectedItem.ImmunizedBy.HasValue)
    //            {
    //                Success = false;
    //                SelectedItem.ValidationErrors.Add(new ValidationResult(
    //                    "For each immunization being entered a date and/or Offered or Provided by is required.  The date represents the date that the immunization was received.  The Offered or Provided by would indicate that the agency offered the vaccine, even when no date received was recorded.",
    //                    new[] { "DateReceived", "ImmunizedBy" }));
    //            }

    //            if (SelectedItem.ImmunizedBy.HasValue)
    //            {
    //                var t = CodeLookupCache.GetCodeDescriptionFromKey(SelectedItem.ImmunizedBy);
    //                if ((t == "Vaccine provided by HHA" || t == "Vaccine provided by another Provider") &&
    //                    !SelectedItem.DateReceived.HasValue)
    //                {
    //                    Success = false;
    //                    SelectedItem.ValidationErrors.Add(new ValidationResult(
    //                        "Date Received is required when an immunization was provided.", new[] { "DateReceived" }));
    //                }
    //                else if (t == "Offered Vaccine by HHA" && SelectedItem.DateReceived.HasValue)
    //                {
    //                    Success = false;
    //                    SelectedItem.ValidationErrors.Add(new ValidationResult(
    //                        "Date Received cannot have value when an immunization was not provided.",
    //                        new[] { "DateReceived" }));
    //                }
    //                else if (t == "Offered Vaccine by HHA" && (!SelectedItem.ReasonForDeclining.HasValue &&
    //                                                           !SelectedItem.Contraindications.HasValue))
    //                {
    //                    Success = false;
    //                    SelectedItem.ValidationErrors.Add(new ValidationResult(
    //                        "A Reason for Declining or a Contraindication is required when a vaccine has been offered but not provided.",
    //                        new[] { "ReasonForDeclining", "Contraindications" }));
    //                }
    //            }

    //            if (SelectedItem.DateReceived.HasValue && SelectedItem.DateReceived > DateTime.Now.Date)
    //            {
    //                Success = false;
    //                SelectedItem.ValidationErrors.Add(new ValidationResult("Date Received cannot be in the future.",
    //                    new[] { "DateReceived" }));
    //            }

    //            if (SelectedItem.ReasonForDeclining.HasValue && string.IsNullOrEmpty(SelectedItem.DecliningReasonComment))
    //            {
    //                var c = CodeLookupCache.GetCodeFromKey(SelectedItem.ReasonForDeclining);

    //                if (c == "DecAdd")
    //                {
    //                    Success = false;
    //                    SelectedItem.ValidationErrors.Add(new ValidationResult("Declining Reason Comment is required.",
    //                        new[] { "DecliningReasonComment" }));
    //                }
    //            }

    //            return Success && base.Validate();
    //        }

    //        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model",
    //            typeof(IPatientService), typeof(PatientImmunizationUserControlBase), null);

    //        public override void RemoveFromModel(PatientImmunization entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }
    //        }
    //    }

    //    public class PatientPharmacyUserControlBase : ChildControlBase<PatientPharmacyUserControl, PatientPharmacy>
    //    {
    //        private Vendor _SelectedVendor;

    //        public Vendor SelectedVendor
    //        {
    //            get { return _SelectedVendor; }
    //            set
    //            {
    //                _SelectedVendor = value;
    //                RaisePropertyChanged("SelectedVendor");
    //            }
    //        }

    //        public List<Vendor> VendorList
    //        {
    //            get
    //            {
    //                return VendorCache.GetActiveVendors().Where(x => x.IsVendorTypePharmacy)
    //                    .OrderBy(x => x.VendorName)
    //                    .ThenBy(x => x.Number)
    //                    .ToList();
    //            }
    //        }

    //        public RelayCommand<Vendor> ac_VendorClosed { get; protected set; }

    //        public PatientPharmacyUserControlBase()
    //        {
    //            PopupDataTemplate = "PatientPharmacyPopupDataTemplate";

    //            AddPressed += OnAddPressed;
    //            EditPressed += OnEditPressed;
    //            CancelPressed += OnCancelPressed;
    //            OKPressed += OnOKPressed;

    //            Loaded += PatientPharmacyUserControlBase_Loaded;
    //        }

    //        void PatientPharmacyUserControlBase_Loaded(object sender, RoutedEventArgs e)
    //        {
    //            ac_VendorClosed = new RelayCommand<Vendor>(vendor =>
    //            {
    //                if (vendor == null)
    //                {
    //                    return;
    //                }

    //                SelectedItem.VendorKey = vendor.VendorKey;
    //            });

    //            FilteredPatientPharmacy.Source = ItemsSource;
    //            RaisePropertyChanged("FilteredPatientPharmacy");
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            EditPressed -= OnEditPressed;
    //            CancelPressed -= OnCancelPressed;
    //            OKPressed -= OnOKPressed;
    //            Loaded -= PatientPharmacyUserControlBase_Loaded;

    //            base.Cleanup();
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<PatientPharmacy> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public void OnOKPressed(object sender, UserControlBaseEventArgs<PatientPharmacy> e)
    //        {
    //            if (e.Entity.Preferred)
    //            {
    //                foreach (var currPharmacy in CurrentPatient.PatientPharmacy)
    //                    if ((e.Entity.IsNew) && (currPharmacy.PatientPharmacyKey != e.Entity.PatientPharmacyKey))
    //                    {
    //                        currPharmacy.Preferred = false;
    //                    }
    //                    else if (currPharmacy.Preferred && currPharmacy.PatientPharmacyKey != e.Entity.PatientPharmacyKey)
    //                    {
    //                        currPharmacy.Preferred = false;
    //                    }
    //            }

    //            foreach (var pp in CurrentPatient.PatientPharmacy) pp.EndEditting();

    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<PatientPharmacy> e)
    //        {
    //            SelectedVendor = VendorList.FirstOrDefault(x => x.VendorKey == SelectedItem.VendorKey);
    //            e.Entity.Patient = CurrentPatient;
    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<PatientPharmacy> e)
    //        {
    //            SelectedVendor = null;
    //            e.Entity.Patient = CurrentPatient;

    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            // SAVE - regardless of whethe command = OK or CANCEL...
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }
    //        }

    //        private CollectionViewSource _FilteredPatientPharmacy = new CollectionViewSource();

    //        public CollectionViewSource FilteredPatientPharmacy
    //        {
    //            get { return _FilteredPatientPharmacy; }
    //            set
    //            {
    //                _FilteredPatientPharmacy = value;
    //                RaisePropertyChanged("_FilteredPatientPharmacy");
    //            }
    //        }

    //        public Patient CurrentPatient
    //        {
    //            get { return (Patient)GetValue(CurrentPatientProperty); }
    //            set { SetValue(CurrentPatientProperty, value); }
    //        }

    //        public static readonly DependencyProperty CurrentPatientProperty = DependencyProperty.Register("CurrentPatient",
    //            typeof(Patient), typeof(PatientPharmacyUserControlBase), new PropertyMetadata(null, CurrentPatientChanged));

    //        private static void CurrentPatientChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    //        {
    //            PatientPharmacyUserControlBase me = sender as PatientPharmacyUserControlBase;
    //            if (me == null)
    //            {
    //                return;
    //            }

    //            if (me.ItemsSource != null)
    //            {
    //                foreach (PatientPharmacy pph in me.ItemsSource)
    //                    pph.CurrentPatient = me.CurrentPatient;
    //            }
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model",
    //            typeof(IPatientService), typeof(PatientPharmacyUserControlBase), null);

    //        public override void RemoveFromModel(PatientPharmacy entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //        }
    //    }

    //    public class PatientMessageUserControlBase : ChildControlBase<PatientMessageUserControl, PatientMessage>
    //    {
    //        public RelayCommand AddMessage_Command { get; protected set; }

    //        void OnPatientViewModelSaved(IParentViewModel parentVM)
    //        {
    //            RefreshPatientMessages();
    //            Deployment.Current.Dispatcher.BeginInvoke(() => { Expand = Expand; });
    //        }

    //        public PatientMessageUserControlBase()
    //        {
    //            Messenger.Default.Register<IParentViewModel>(this, Constants.DomainEvents.PatientViewModelSaved,
    //                OnPatientViewModelSaved);
    //            PopupDataTemplate = "PatientMessagePopupDataTemplate";
    //            OKPressed += OnOKPressed;
    //            EditPressed += OnEditPressed;
    //            CancelPressed += OnCancelPressed;
    //            ItemSelected += Update_ItemSelected;

    //            AddMessage_Command = new RelayCommand(() =>
    //            {
    //                PatientMessage newPM = new PatientMessage
    //                {
    //                    CurrentPatient = CurrentPatient,
    //                    Expand = Expand
    //                };

    //                oldMessageText = null;

    //                ItemsSource.Add(newPM);
    //                RefreshPatientMessages();

    //                SelectedItem = newPM;
    //                if (SelectedItem != null)
    //                {
    //                    SelectedItem.BeginEditting();
    //                }

    //                IsEdit = true;

    //                ParentViewModel.PopupDataContext = this;
    //                if ((PopupDataTemplate is string && String.IsNullOrEmpty(PopupDataTemplate)) ||
    //                    (PopupDataTemplate == null))
    //                {
    //                    SetFocusHelper.SelectFirstEditableWidget(this);
    //                }
    //            }, () => IsOnline && AllowAdd());
    //        }

    //        public override void Cleanup()
    //        {
    //            Messenger.Default.Unregister(this);
    //            OKPressed -= OnOKPressed;
    //            EditPressed -= OnEditPressed;
    //            CancelPressed -= OnCancelPressed;
    //            ItemSelected -= Update_ItemSelected;
    //            CurrentPatient = null;
    //            if (_PatientMessages != null)
    //            {
    //                _PatientMessages.Source = null;
    //                _PatientMessages.Filter -= _PatientMessages_Filter;
    //                _PatientMessages = null;
    //            }

    //            base.Cleanup();
    //        }

    //        public Patient CurrentPatient
    //        {
    //            get { return (Patient)GetValue(CurrentPatientProperty); }
    //            set { SetValue(CurrentPatientProperty, value); }
    //        }

    //        public static readonly DependencyProperty CurrentPatientProperty =
    //            DependencyProperty.Register("CurrentPatient", typeof(Patient), typeof(PatientMessageUserControlBase),
    //                new PropertyMetadata(null, CurrentPatientChanged));

    //        private static void CurrentPatientChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    //        {
    //            PatientMessageUserControlBase me = sender as PatientMessageUserControlBase;

    //            if (me?.ItemsSource != null)
    //            {
    //                foreach (PatientMessage pm in me.ItemsSource)
    //                    pm.CurrentPatient = me.CurrentPatient;
    //            }
    //        }

    //        private bool firstRefresh;

    //        private void Update_ItemSelected(object sender, EventArgs e)
    //        {
    //            if ((ItemsSource != null) && (firstRefresh == false))
    //            {
    //                firstRefresh = true;
    //                RefreshPatientMessages();
    //            }
    //        }

    //        private void RefreshPatientMessages()
    //        {
    //            if (ItemsSource == null)
    //            {
    //                return;
    //            }

    //            PatientMessage selectedItem = SelectedItem;
    //            foreach (PatientMessage pm in ItemsSource) pm.CurrentPatient = CurrentPatient;
    //            if (_PatientMessages == null)
    //            {
    //                _PatientMessages = new CollectionViewSource();
    //                _PatientMessages.SortDescriptions.Add(new SortDescription("Inactive", ListSortDirection.Ascending));
    //                _PatientMessages.SortDescriptions.Add(new SortDescription("MessageDateTime",
    //                    ListSortDirection.Descending));
    //                _PatientMessages.Source = ItemsSource;

    //                _PatientMessages.Filter += _PatientMessages_Filter;
    //            }

    //            if (selectedItem == null)
    //            {
    //                _PatientMessages.View.MoveCurrentToFirst();
    //            }
    //            else
    //            {
    //                _PatientMessages.View.MoveCurrentTo(selectedItem);
    //            }

    //            PatientMessages.Refresh();
    //            RaisePropertyChanged("PatientMessages");
    //        }

    //        private void _PatientMessages_Filter(object s, FilterEventArgs args)
    //        {
    //            PatientMessage pm = args.Item as PatientMessage;
    //            args.Accepted = (pm.HistoryKey == null) ? true : false;
    //        }

    //        private bool _Expand;

    //        public bool Expand
    //        {
    //            get { return _Expand; }
    //            set
    //            {
    //                _Expand = value;
    //                if (ItemsSource == null)
    //                {
    //                    return;
    //                }

    //                foreach (PatientMessage pm in ItemsSource) pm.Expand = _Expand;
    //                Deployment.Current.Dispatcher.BeginInvoke(RefreshPatientMessages);
    //            }
    //        }

    //        private CollectionViewSource _PatientMessages;
    //        public ICollectionView PatientMessages => _PatientMessages?.View;
    //        private string oldMessageText;

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<PatientMessage> e)
    //        {
    //            e.Entity.CanFullEdit = true;
    //            e.Entity.CurrentPatient = CurrentPatient;
    //            oldMessageText = e.Entity.MessageText;
    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public override bool? CanExecute_OK_CommandOverride()
    //        {
    //            return true;
    //        }

    //        public void OnOKPressed(object sender, UserControlBaseEventArgs<PatientMessage> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            if ((SelectedItem != null) && (SelectedItem.IsNew || (SelectedItem.MessageText != oldMessageText)))
    //            {
    //                SelectedItem.MessageBy = WebContext.Current.User.MemberID;
    //                SelectedItem.MessageDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
    //            }
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<PatientMessage> e)
    //        {
    //            RefreshPatientMessages();
    //            if (_PatientMessages != null)
    //            {
    //                _PatientMessages.View.MoveCurrentToFirst();
    //            }

    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientMessageUserControlBase), null);

    //        public override void RemoveFromModel(PatientMessage entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }
    //        }
    //    }

    //    public class PatientLevelOfCareUserControlBase : ChildControlBase<PatientLevelOfCareUserControl, AdmissionLevelOfCare>
    //    {
    //        public PatientLevelOfCareUserControlBase()
    //        {
    //            PopupDataTemplate = "LevelOfCarePopupDataTemplate";
    //            AddPressed += OnAddPressed;
    //            OKPressed += OnOKPressed;
    //            EditPressed += OnEditPressed;
    //            CancelPressed += OnCancelPressed;
    //            DeletePressed += OnDeletePressed;
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            OKPressed -= OnOKPressed;
    //            EditPressed -= OnEditPressed;
    //            CancelPressed -= OnCancelPressed;
    //            DeletePressed -= OnDeletePressed;
    //            base.Cleanup();
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<AdmissionLevelOfCare> e)
    //        {
    //            if (IsEncounter == false)
    //            {
    //                return;
    //            }

    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<AdmissionLevelOfCare> e)
    //        {
    //            if (IsEncounter == false)
    //            {
    //                return;
    //            }

    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnOKPressed(object sender, UserControlBaseEventArgs<AdmissionLevelOfCare> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<AdmissionLevelOfCare> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public void OnDeletePressed(object sender, UserControlBaseEventArgs<AdmissionLevelOfCare> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientLevelOfCareUserControl), null);

    //        public override void RemoveFromModel(AdmissionLevelOfCare entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //        }

    //        public override bool IsEncounter => (Encounter != null);

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {

    //        }
    //    }

    //    public class PatientPOCLevelOfCareUserControlBase : ChildControlBase<PatientPOCLevelOfCareUserControl, AdmissionLevelOfCare>
    //    {
    //        public PatientPOCLevelOfCareUserControlBase()
    //        {
    //            PopupDataTemplate = "LevelOfCarePopupDataTemplate";
    //            AddPressed += OnAddPressed;
    //            OKPressed += OnOKPressed;
    //            EditPressed += OnEditPressed;
    //            CancelPressed += OnCancelPressed;
    //            DeletePressed += OnDeletePressed;
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            OKPressed -= OnOKPressed;
    //            EditPressed -= OnEditPressed;
    //            CancelPressed -= OnCancelPressed;
    //            DeletePressed -= OnDeletePressed;
    //            base.Cleanup();
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<AdmissionLevelOfCare> e)
    //        {
    //            if (IsEncounter == false)
    //            {
    //                return;
    //            }

    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<AdmissionLevelOfCare> e)
    //        {
    //            if (IsEncounter == false)
    //            {
    //                return;
    //            }

    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnOKPressed(object sender, UserControlBaseEventArgs<AdmissionLevelOfCare> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<AdmissionLevelOfCare> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public void OnDeletePressed(object sender, UserControlBaseEventArgs<AdmissionLevelOfCare> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientPOCLevelOfCareUserControl),
    //                null);

    //        public override void RemoveFromModel(AdmissionLevelOfCare entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //        }

    //        public override bool IsEncounter => (Encounter != null);

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {

    //        }
    //    }

    //    public class
    //        PatientPainLocationUserControlBase : ChildControlBase<PatientPainLocationUserControl, AdmissionPainLocation>
    //    {
    //        public PatientPainLocationUserControlBase()
    //        {
    //            PopupDataTemplate = "PainLocationPopupDataTemplate";
    //            AddPressed += OnAddPressed;
    //            OKPressed += OnOKPressed;
    //            EditPressed += OnEditPressed;
    //            CancelPressed += OnCancelPressed;
    //            DeletePressed += OnDeletePressed;
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            OKPressed -= OnOKPressed;
    //            EditPressed -= OnEditPressed;
    //            CancelPressed -= OnCancelPressed;
    //            DeletePressed -= OnDeletePressed;
    //            base.Cleanup();
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<AdmissionPainLocation> e)
    //        {
    //            e.Entity.Version = 3;
    //            int maxsite = 1;
    //            try
    //            {
    //                maxsite = ItemsSource.Max(p => p.PainSite);
    //            }
    //            catch
    //            {
    //                maxsite = 1;
    //            }

    //            e.Entity.PainSite = ++maxsite;
    //            ParentViewModel.PopupDataContext = this;
    //            RaisePropertyChangedProtectedSelectedItem();
    //        }

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<AdmissionPainLocation> e)
    //        {
    //            ParentViewModel.PopupDataContext = this;
    //            RaisePropertyChangedProtectedSelectedItem();
    //        }

    //        public async void OnOKPressed(object sender, UserControlBaseEventArgs<AdmissionPainLocation> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            RaisePropertyChangedProtectedSelectedItem();

    //            // If we are in a Re-Evaluate section popup - even though we set the PopupDataContext to null above - it will now point to the re-eval popup itself
    //            // If that is the case - defer the save to the underlying re-eval popup 'Include in this Encounter' command - since they may also choose 'cancel' from the underlying re-eval popup
    //            if (ParentViewModel.PopupDataContext != null)
    //            {
    //                return;
    //            }

    //            DynamicFormViewModel vm = ParentViewModel as DynamicFormViewModel;
    //            if (vm != null)
    //            {
    //                await vm.AutoSave_Command("PatientPainLocationControlReEvalOK");
    //            }
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<AdmissionPainLocation> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            RaisePropertyChangedProtectedSelectedItem();
    //        }

    //        public void OnDeletePressed(object sender, UserControlBaseEventArgs<AdmissionPainLocation> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            RaisePropertyChangedProtectedSelectedItem();
    //        }

    //        public override bool Validate()
    //        {
    //            bool AllValid = true;

    //            if (SelectedItem.FirstIdentifiedDate == DateTime.MinValue)
    //            {
    //                SelectedItem.FirstIdentifiedDate = null;
    //            }

    //            if (SelectedItem.FirstIdentifiedDate != null)
    //            {
    //                SelectedItem.FirstIdentifiedDate = ((DateTime)SelectedItem.FirstIdentifiedDate).Date;
    //            }

    //            if ((SelectedItem.Version == 2) && (SelectedItem.FirstIdentifiedDate == null))
    //            {
    //                SelectedItem.ValidationErrors.Add(new ValidationResult("The Date First Identified field is required",
    //                    new[] { "FirstIdentifiedDate" }));
    //                AllValid = false;
    //            }

    //            if ((SelectedItem.FirstIdentifiedDate != null) && (SelectedItem.FirstIdentifiedDate >
    //                                                               DateTime.SpecifyKind(DateTime.Today,
    //                                                                   DateTimeKind.Unspecified).Date))
    //            {
    //                SelectedItem.ValidationErrors.Add(new ValidationResult(
    //                    "The Date First Identified field cannot be a future date", new[] { "FirstIdentifiedDate" }));
    //                AllValid = false;
    //            }

    //            if (SelectedItem.ResolvedDate == DateTime.MinValue)
    //            {
    //                SelectedItem.ResolvedDate = null;
    //            }

    //            if (SelectedItem.ResolvedDate != null)
    //            {
    //                SelectedItem.ResolvedDate = ((DateTime)SelectedItem.ResolvedDate).Date;
    //            }

    //            if ((SelectedItem.ResolvedDate != null) && (SelectedItem.ResolvedDate >
    //                                                        DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified)
    //                                                            .Date))
    //            {
    //                SelectedItem.ValidationErrors.Add(
    //                    new ValidationResult("The Resolved Date field cannot be a future date", new[] { "ResolvedDate" }));
    //                AllValid = false;
    //            }

    //            if (string.IsNullOrWhiteSpace(SelectedItem.PainDuration))
    //            {
    //                SelectedItem.PainDuration = null;
    //            }

    //            if (SelectedItem.ShowPainDuration && (SelectedItem.PainDuration == null))
    //            {
    //                string[] memberNames = { "PainDuration" };
    //                SelectedItem.ValidationErrors.Add(new ValidationResult("The Pain Duration field is required",
    //                    memberNames));
    //                AllValid = false;
    //            }

    //            return AllValid;
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientPainLocationUserControl), null);

    //        public override void RemoveFromModel(AdmissionPainLocation entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            // SAVE - regardless of whethe command = OK or CANCEL...
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }
    //        }

    //        private void RaisePropertyChangedProtectedSelectedItem()
    //        {
    //            Deployment.Current.Dispatcher.BeginInvoke(() => { RaisePropertyChanged("ProtectedSelectedItem"); });
    //        }

    //        public bool ProtectedSelectedItem
    //        {
    //            get
    //            {
    //                if ((Protected) || (SelectedItem == null))
    //                {
    //                    return true;
    //                }

    //                return (!SelectedItem.CanEditResolved);
    //            }
    //        }
    //    }

    public class PatientPhoneUserControlBase : ChildControlBase<PatientPhoneUserControl, PatientPhone>
    {
        public PatientPhoneUserControlBase()
        {
            OKPressed += OnOKPressed;
        }

        public override void Cleanup()
        {
            OKPressed -= OnOKPressed;
            base.Cleanup();
        }

        public void OnOKPressed(object sender, UserControlBaseEventArgs<PatientPhone> e)
        {
            // there can be only one main phone
            if (!e.Entity.Main)
            {
                return;
            }

            foreach (PatientPhone pp in ItemsSource)
                if ((e.Entity != pp) && (pp.Main = true) && (pp.HistoryKey == null))
                {
                    pp.Main = false;
                }
        }

        //public IPatientService Model
        //{
        //    get { return (IPatientService)GetValue(ModelProperty); }
        //    set { SetValue(ModelProperty, value); }
        //}

        //public static readonly DependencyProperty ModelProperty =
        //    DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientPhoneUserControl), null);

        public override void RemoveFromModel(PatientPhone entity)
        {
            //if (Model == null)
            {
                //throw new ArgumentNullException("Model", "Model is NULL");
            }

            //Model.Remove(entity);
        }

        public override void SaveModel(UserControlBaseCommandType command)
        {
            //issue SAVE - regardless of whethe command = OK or CANCEL...
           // if (Model == null)
            {
               // throw new ArgumentNullException("Model", "Model is NULL");
            }

            //Child control can now do intermediate saves to the database!
            //However - Model ensures save only occurs if there are no open edits or invalid entities.
            //This should be OK for everything except when adding a parent entity...especially when the detail 
            //tab is still in open edit mode!


            //On the current crop of maintenance screens - not certain that we want to SAVE...because not certain how 
            //'pending' saves and status of pending saves will be communicated to UI.  If child tab/control does save,
            //but it is pending - then how is parent UI to know this?
        }
    }

    //    public class PatientAlternateIDUserControlBase : ChildControlBase<PatientAlternateIDUserControl, PatientAlternateID>
    //    {
    //        public override void Cleanup()
    //        {
    //            base.Cleanup();
    //        }

    //        public Patient CurrentPatient
    //        {
    //            get { return (Patient)GetValue(CurrentPatientProperty); }
    //            set { SetValue(CurrentPatientProperty, value); }
    //        }

    //        public static readonly DependencyProperty CurrentPatientProperty =
    //            DependencyProperty.Register("CurrentPatient", typeof(Patient), typeof(PatientAlternateIDUserControlBase),
    //                new PropertyMetadata(null, CurrentPatientChanged));

    //        private static void CurrentPatientChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    //        {
    //            PatientAlternateIDUserControlBase me = sender as PatientAlternateIDUserControlBase;
    //            if (me == null)
    //            {
    //                return;
    //            }

    //            if (me.ItemsSource != null)
    //            {
    //                foreach (PatientAlternateID pad in me.ItemsSource)
    //                    pad.Patient = me.CurrentPatient;
    //            }
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientAlternateIDUserControl), null);

    //        public override void RemoveFromModel(PatientAlternateID entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            // SAVE - regardless of when command = OK or CANCEL...
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            //Child control can now do intermediate saves to the database!
    //            //However - Model ensures save only occurs if there are no open edits or invalid entities.
    //            //This should be OK for everything except when adding a parent entity...especially when the detail 
    //            //tab is still in open edit mode!


    //            //On the current crop of maintenance screens - not certain that we want to SAVE...because not certain how 
    //            //'pending' saves and status of pending saves will be communicated to UI.  If child tab/control does save,
    //            //but it is pending - then how is parent UI to know this?
    //        }
    //    }

    //    public class PatientIVUserControlBase : ChildControlBase<PatientIVUserControl, AdmissionIVSite>
    //    {
    //        public PatientIVUserControlBase()
    //        {
    //            PopupDataTemplate = "IVSitePopupDataTemplate";
    //            AddPressed += OnAddPressed;
    //            OKPressed += OnOKPressed;
    //            EditPressed += OnEditPressed;
    //            CancelPressed += OnCancelPressed;
    //            DeletePressed += OnDeletePressed;
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            OKPressed -= OnOKPressed;
    //            EditPressed -= OnEditPressed;
    //            CancelPressed -= OnCancelPressed;
    //            DeletePressed -= OnDeletePressed;
    //            base.Cleanup();
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<AdmissionIVSite> e)
    //        {
    //            int max_number = 0;
    //            if (ParentViewModel != null)
    //            {
    //                try
    //                {
    //                    max_number = (ParentViewModel.CurrentAdmission == null)
    //                        ? 0
    //                        : ParentViewModel.CurrentAdmission.AdmissionIVSite
    //                            .Where(i => ((i.DeletedDate == null) && (i.Superceded == false)))
    //                            .MaxOrDefault(i => i.Number, 0);
    //                }
    //                catch
    //                {
    //                }
    //            }

    //            e.Entity.Number = ++max_number;
    //            e.Entity.CurrentEncounter = Encounter;
    //            ParentViewModel.PopupDataContext = this;
    //            RaisePropertyChanged("AnyItemsCheck");
    //        }

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<AdmissionIVSite> e)
    //        {
    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<AdmissionIVSite> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            RaisePropertyChanged("AnyItemsCheck");
    //        }

    //        public void OnDeletePressed(object sender, UserControlBaseEventArgs<AdmissionIVSite> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //        }

    //        public void OnOKPressed(object sender, UserControlBaseEventArgs<AdmissionIVSite> e)
    //        {
    //            ParentViewModel.PopupDataContext = null;
    //            RaisePropertyChanged("AnyItemsCheck");
    //            if (SelectedItem != null)
    //            {
    //                if (SelectedItem.IVInsertionChangeDate == DateTime.MinValue)
    //                {
    //                    SelectedItem.IVInsertionChangeDate = null;
    //                }

    //                if (SelectedItem.IVDiscontinueDate == DateTime.MinValue)
    //                {
    //                    SelectedItem.IVDiscontinueDate = null;
    //                }
    //            }
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientIVUserControl), null);

    //        public override void RemoveFromModel(AdmissionIVSite entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            Model.Remove(entity);
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }
    //        }
    //    }

    //    public class PatientWoundUserControlBase : ChildControlBase<PatientWoundUserControl, AdmissionWoundSite>, INotifyDataErrorInfo, IAniManControlDataContext
    //    {
    //        public RelayCommand FetchWoundSitePhotosCommand { get; protected set; }
    //        public RelayCommand ImportPhotoCommand { get; protected set; }
    //        public RelayCommand OlderPhotoCommand { get; protected set; }
    //        public RelayCommand NewerPhotoCommand { get; protected set; }
    //        public RelayCommand MeasurementHistoryCommand { get; protected set; }
    //        public RelayCommand EditPhotoCommand { get; protected set; }
    //        public RelayCommand CancelPhotoCommand { get; protected set; }
    //        public RelayCommand AcceptPhotoCommand { get; protected set; }

    //        public RelayCommand<AniManUIStruct> AniManToggleDisplay { get; set; }
    //        public RelayCommand<AniManUIStruct> AniManSelectPartOnSilhouette { get; set; }
    //        public RelayCommand<AniManUIStruct> AniManSelectPartOnOptionsMenu { get; set; }
    //        public RelayCommand<AniManUIStruct> AniManReset { get; set; }

    //        public int siteNumber;

    //        public bool IsConsult
    //        {
    //            get { return (bool)GetValue(IsConsultProperty); }
    //            set { SetValue(IsConsultProperty, value); }
    //        }

    //        public static readonly DependencyProperty IsConsultProperty =
    //            DependencyProperty.Register("IsConsult", typeof(bool), typeof(PatientWoundUserControlBase), null);

    //        public bool ProtectedWound
    //        {
    //            get
    //            {
    //                if (SelectedItem != null)
    //                {
    //                    if (SelectedItem.HealedLocked)
    //                    {
    //                        return true;
    //                    }
    //                }

    //                return Protected;
    //            }
    //        }

    //        public bool IsConsultProtected
    //        {
    //            get
    //            {
    //                if (ParentViewModel == null)
    //                {
    //                    return true;
    //                }

    //                DynamicFormViewModel df = ParentViewModel as DynamicFormViewModel;
    //                if (df == null)
    //                {
    //                    return true;
    //                }

    //                if (IsConsult == false)
    //                {
    //                    return true;
    //                }

    //                if (df.PreviousEncounterStatus == (int)EncounterStatusType.Completed)
    //                {
    //                    return true;
    //                }

    //                return false;
    //            }
    //        }

    //        public PatientWoundUserControlBase()
    //        {
    //            PopupDataTemplate = "WoundPopupDataTemplate";
    //            AddPressed += OnAddPressed;
    //            PostAddPressed += OnPostAddPressed;
    //            OKPressed += OnOKPressed;
    //            EditPressed += OnEditPressed;
    //            CancelPressed += OnCancelPressed;
    //            DeletePressed += OnDeletePressed;
    //            ItemSelected += UserControl_ItemSelected;

    //            FetchWoundSitePhotosCommand =
    //                new RelayCommand(OnFetchWoundSitePhotos, () => EntityManager.Current.IsOnline);
    //            ImportPhotoCommand = new RelayCommand(() =>
    //            {
    //                OnImportExecute();
    //                RaiseCanExecuteChanged();
    //            });
    //            OlderPhotoCommand = new RelayCommand(GotoOlderPhoto);
    //            NewerPhotoCommand = new RelayCommand(GotoNewerPhoto);
    //            MeasurementHistoryCommand = new RelayCommand(MeasurementHistory);
    //            EditPhotoCommand = new RelayCommand(EditPhoto, EditPhotoCommandCanExecute);
    //            CancelPhotoCommand = new RelayCommand(CancelPhoto);
    //            AcceptPhotoCommand = new RelayCommand(AcceptPhoto);

    //            AniManToggleDisplay = new RelayCommand<AniManUIStruct>(AniManUIStruct =>
    //            {
    //                _aniManToggleDisplay(AniManUIStruct);
    //            });
    //            AniManSelectPartOnSilhouette = new RelayCommand<AniManUIStruct>(AniManUIStruct =>
    //            {
    //                _aniManSelectPartOnSilhouette(AniManUIStruct);
    //            });
    //            AniManSelectPartOnOptionsMenu = new RelayCommand<AniManUIStruct>(AniManUIStruct =>
    //            {
    //                _aniManSelectPartOnOptionsMenu(AniManUIStruct);
    //            });
    //            AniManReset = new RelayCommand<AniManUIStruct>(AniManUIStruct => { _aniManReset(AniManUIStruct); });

    //            _currentErrors = new Dictionary<string, List<string>>();
    //        }

    //        public bool EditPhotoCommandCanExecute()
    //        {
    //            var ret = CurrentPhotoPhotoAvailable && !ProtectedWound;
    //            return ret;
    //        }

    //        void UserControl_ItemSelected(object sender, EventArgs e)
    //        {
    //            if (SelectedItem != null)
    //            {
    //                SelectedItem.SelectMode = AdmissionWoundSite.SelectModeValue.NotShown;
    //                UpdateUI();
    //            }
    //        }

    //        private bool _IsSiteLockedValue;

    //        public bool IsSiteLockedValue
    //        {
    //            get { return _IsSiteLockedValue; }
    //            set
    //            {
    //                _IsSiteLockedValue = value;
    //                RaisePropertyChanged("IsSiteLockedValue");
    //            }
    //        }

    //        private bool _IsLockedValue;

    //        public bool IsLockedValue
    //        {
    //            get { return _IsLockedValue; }
    //            set
    //            {
    //                _IsLockedValue = value;
    //                RaisePropertyChanged("IsLockedValue");
    //            }
    //        }

    //        private int _SlaveValue;

    //        public int SlaveValue
    //        {
    //            get { return _SlaveValue; }
    //            set
    //            {
    //                _SlaveValue = value;
    //                RaisePropertyChanged("SlaveValue");
    //            }
    //        }

    //        private void _aniManReset(AniManUIStruct a)
    //        {
    //            try
    //            {
    //                var admissionWoundSite = SelectedItem;
    //                Color color = admissionWoundSite?.DrawColor ?? Colors.White;
    //                var animan = a.AniMan;
    //                animan.SelectedPart = admissionWoundSite.Site;

    //                if (admissionWoundSite.Site > 0)
    //                {
    //                    SelectedItem.SelectMode = AdmissionWoundSite.SelectModeValue.NotShown;
    //                }
    //                else
    //                {
    //                    SelectedItem.SelectMode = AdmissionWoundSite.SelectModeValue.DisplaySelectedPartOnSilhouette;
    //                }

    //                animan.AniManCtrl1.DrawSelectionMode(a.AniMan.SelectedPart, a.Canvas1);
    //                subDrawingDisplays = animan.DisplayDetailBasedOnSelectedPart(color, a.Canvas2, a.Canvas3, a.Canvas4,
    //                    a.Canvas5, a.Canvas6, a.Canvas7);
    //                UpdateUI();
    //                a.WasSuccess = true;
    //            }
    //            catch (Exception e)
    //            {
    //                Debug.WriteLine(e.Message);
    //            }
    //        }

    //        public string WoundSiteCountMessage
    //        {
    //            get
    //            {
    //                if (SelectedItem == null)
    //                {
    //                    return "";
    //                }

    //                int thiskey = SelectedItem.AdmissionWoundSiteKey;
    //                int thisHistoryKey = !SelectedItem.HistoryKey.HasValue
    //                    ? SelectedItem.AdmissionWoundSiteKey
    //                    : SelectedItem.HistoryKey.Value;
    //                int thisadmissionkey = SelectedItem.AdmissionKey;
    //                int thissitekey = SelectedItem.Site;
    //                Admission admissionhandle = SelectedItem.Admission;

    //                var f = FilteredItemsSource.Cast<AdmissionWoundSite>().ToList();

    //                var fakel = f
    //                    .Where(w => (w.AdmissionKey == thisadmissionkey) && (w.Site == thissitekey))
    //                    .Select(s => new
    //                    { UseKey = (s.HistoryKey == null ? s.AdmissionWoundSiteKey : s.HistoryKey), s.IsHealed })
    //                    .ToList();
    //                var heall = from t in fakel
    //                            group t by t.UseKey
    //                    into g
    //                            select new { UseKey = g.Key, IsHealed = (from t2 in g select t2.IsHealed).Max() };

    //                var l = heall.Where(w => w.UseKey != thisHistoryKey && w.UseKey != thiskey && !w.IsHealed)
    //                    .Select(s => s.UseKey.Value).ToList();


    //                int c = l.Count();
    //                if (c == 1)
    //                {
    //                    return "There is 1 other unhealed wound at this site."; // [" + extra + "]";
    //                }

    //                if (c > 1)
    //                {
    //                    return "There are " + c + " other unhealed wounds at this site."; // [" + extra + "]";
    //                }

    //                return "There are no other unhealed wounds at this site";
    //            }
    //        }

    //        private void _aniManSelectPartOnOptionsMenu(AniManUIStruct a)
    //        {
    //            try
    //            {
    //                if (Protected)
    //                {
    //                    a.WasSuccess = false;
    //                    return;
    //                }

    //                if (IsEdit)
    //                {
    //                    var animan = a.AniMan;
    //                    SelectedItem.SetSite(a.AniMan.SelectedPart.HasValue ? a.AniMan.SelectedPart.Value : 0);

    //                    tempSelectedPartForRegion = a.AniMan.SelectedPart;
    //                    if (a.AniMan.SelectedPart.HasValue)
    //                    {
    //                        SelectedItem.SelectMode = AdmissionWoundSite.SelectModeValue.DisplaySelectedPartOnSilhouette;
    //                        animan.AniManCtrl1.DrawDisplayMode(a.AniMan.SelectedPart, a.Canvas1, SelectedItem.DrawColor);
    //                    }
    //                    else
    //                    {
    //                        SelectedItem.SelectMode = AdmissionWoundSite.SelectModeValue.DisplayPartOnOptionsMenu;
    //                        animan.AniManCtrl1.DrawSelectionMode(a.AniMan.SelectedPart, a.Canvas1);
    //                    }

    //                    Color color = SelectedItem?.DrawColor ?? Colors.White;
    //                    subDrawingDisplays = animan.DisplayDetailBasedOnSelectedPart(color, a.Canvas2, a.Canvas3, a.Canvas4,
    //                        a.Canvas5, a.Canvas6, a.Canvas7);
    //                    UpdateUI();
    //                    a.WasSuccess = true;
    //                }
    //            }
    //            catch (Exception e)
    //            {
    //                Debug.WriteLine(e.Message);
    //            }
    //        }

    //        private int? tempSelectedPartForRegion;
    //        SubDrawingDisplays subDrawingDisplays = new SubDrawingDisplays();

    //        private void _aniManSelectPartOnSilhouette(AniManUIStruct a)
    //        {
    //            try
    //            {
    //                if (IsSiteLockedValue)
    //                {
    //                    a.WasSuccess = false;
    //                    return;
    //                }

    //                // Transition state
    //                SelectedItem.SelectMode = AdmissionWoundSite.SelectModeValue.DisplaySelectedPartOnSilhouette;
    //                var animan = a.AniMan;
    //                if (IsEdit)
    //                {
    //                    tempSelectedPartForRegion = a.AniMan.SelectedPart;
    //                }

    //                animan.AniManCtrl1.DrawSelectionMode(a.AniMan.SelectedPart, a.Canvas1);
    //                Color color = SelectedItem?.DrawColor ?? Colors.White;
    //                subDrawingDisplays = animan.DisplayDetailBasedOnSelectedPart(color, a.Canvas2, a.Canvas3, a.Canvas4,
    //                    a.Canvas5, a.Canvas6, a.Canvas7);
    //                UpdateUI();
    //                a.WasSuccess = true;
    //            }
    //            catch (Exception e)
    //            {
    //                Debug.WriteLine(e.Message);
    //            }
    //        }

    //        public bool AniManShown => SelectedItem != null &&
    //                                   (SelectedItem.SelectMode == AdmissionWoundSite.SelectModeValue
    //                                           .DisplaySelectedPartOnSilhouette || SelectedItem.SelectMode ==
    //                                       AdmissionWoundSite.SelectModeValue.DisplayPartOnOptionsMenu);

    //        public bool AniManRightDetailShown => subDrawingDisplays.AniManRightDetailShown;

    //        public bool AniManLeftDetailShown => subDrawingDisplays.AniManLeftDetailShown;

    //        private void _aniManToggleDisplay(AniManUIStruct a)
    //        {
    //            try
    //            {
    //                if (SelectedItem != null)
    //                {
    //                    var animan = a.AniMan;
    //                    a.AniMan.SelectedPart = SelectedItem.Site;

    //                    if (SelectedItem.SelectMode == AdmissionWoundSite.SelectModeValue.NotShown)
    //                    {
    //                        SelectedItem.SelectMode = AdmissionWoundSite.SelectModeValue.DisplaySelectedPartOnSilhouette;
    //                        animan.AniManCtrl1.DrawDisplayMode(SelectedItem.Site, a.Canvas1, SelectedItem.DrawColor);
    //                        Color color = SelectedItem?.DrawColor ?? Colors.White;
    //                        subDrawingDisplays = animan.DisplayDetailBasedOnSelectedPart(color, a.Canvas2, a.Canvas3,
    //                            a.Canvas4, a.Canvas5, a.Canvas6, a.Canvas7);
    //                    }
    //                    else
    //                    {
    //                        SelectedItem.SelectMode = AdmissionWoundSite.SelectModeValue.NotShown;
    //                    }

    //                    UpdateUI();
    //                }
    //            }
    //            catch (Exception e)
    //            {
    //                Debug.WriteLine(e.Message);
    //            }
    //        }

    //        public int? ProxyPressureUlcerStage
    //        {
    //            get
    //            {
    //                if (SelectedItem == null)
    //                {
    //                    return null;
    //                }

    //                return SelectedItem.PressureUlcerStage;
    //            }
    //            set
    //            {
    //                PressureUlcerStageDowngradedCheck(value);
    //                RaisePropertyChanged("ProxyPressureUlcerStage");
    //            }
    //        }

    //        private void PressureUlcerStageDowngradedCheck(int? newStage)
    //        {
    //            if ((SelectedItem == null) || (IsEdit == false))
    //            {
    //                return;
    //            }

    //            if ((SelectedItem.CanFullEdit) || (newStage == null))
    //            {
    //                SelectedItem.PressureUlcerStage = newStage;
    //                RaisePropertyChanged("ProxyPressureUlcerStage");
    //                return;
    //            }

    //            if (PressureUlcerStageDowngraded(newStage) == false)
    //            {
    //                SelectedItem.PressureUlcerStage = newStage;
    //                RaisePropertyChanged("ProxyPressureUlcerStage");
    //                return;
    //            }

    //            NavigateCloseDialog d = new NavigateCloseDialog
    //            {
    //                Width = double.NaN,
    //                Height = double.NaN,
    //                ErrorMessage = "It is clinically incorrect to downgrade the stage of a pressure ulcer. Do you want to retain this change?",
    //                ErrorQuestion = null,
    //                Title = "Downgrading Pressure Ulcer Stage",
    //                HasCloseButton = false
    //            };

    //            d.Closed += (s, err) =>
    //                {
    //                    var _ret = ((NavigateCloseDialog)s).DialogResult;
    //                    if (_ret == true)
    //                    {
    //                        SelectedItem.PressureUlcerStage = newStage;
    //                    }

    //                    RaisePropertyChanged("ProxyPressureUlcerStage");
    //                };
    //            d.Show();

    //        }

    //        private bool PressureUlcerStageDowngraded(int? newStage)
    //        {
    //            if (SelectedItem == null)
    //            {
    //                return false;
    //            }

    //            string _prevStageCode = CodeLookupCache.GetCodeFromKey(SelectedItem.PressureUlcerStage);
    //            string _newStageCode = CodeLookupCache.GetCodeFromKey(newStage);
    //            if ((string.IsNullOrWhiteSpace(_newStageCode)) || (string.IsNullOrWhiteSpace(_prevStageCode)))
    //            {
    //                return false;
    //            }

    //            if ((_newStageCode == "1.") &&
    //                ((_prevStageCode == "a.") || (_prevStageCode == "b.") || (_prevStageCode == "c.")))
    //            {
    //                return true;
    //            }

    //            if ((_newStageCode == "a.") && ((_prevStageCode == "b.") || (_prevStageCode == "c.")))
    //            {
    //                return true;
    //            }

    //            if ((_newStageCode == "b.") && (_prevStageCode == "c."))
    //            {
    //                return true;
    //            }

    //            return false;
    //        }

    //        public override void Cleanup()
    //        {
    //            AddPressed -= OnAddPressed;
    //            PostAddPressed -= OnPostAddPressed;
    //            OKPressed -= OnOKPressed;
    //            EditPressed -= OnEditPressed;
    //            CancelPressed -= OnCancelPressed;
    //            DeletePressed -= OnDeletePressed;
    //            ItemSelected -= UserControl_ItemSelected;
    //            if (WPService != null)
    //            {
    //                WPService.OnGetWoundPhotosLoaded -= WPService_OnGetWoundPhotosLoaded;
    //            }

    //            base.Cleanup();
    //        }

    //        public void OnAddPressed(object sender, UserControlBaseEventArgs<AdmissionWoundSite> e)
    //        {
    //            RaisePropertyChanged("ProtectedWound");
    //            RaisePropertyChanged("ProxyPressureUlcerStage");

    //            int max_number = 0;
    //            if (ParentViewModel != null)
    //            {
    //                try
    //                {
    //                    max_number = (ParentViewModel.CurrentAdmission == null)
    //                        ? 0
    //                        : ParentViewModel.CurrentAdmission.AdmissionWoundSite.MaxOrDefault(w => w.Number, 0);
    //                }
    //                catch
    //                {
    //                }
    //            }

    //            siteNumber = ++max_number;
    //            e.Entity.Number = siteNumber;
    //            e.Entity.Site = 0;
    //            e.Entity.Version = 2;
    //            e.Entity.WoundStatus = CodeLookupCache.GetKeyFromCode("WOUNDSTATUS", "Observable");
    //            e.Entity.CurrentEncounter = Encounter;

    //            PopupDataTemplate = "WoundPopupDataTemplateV2";
    //            ParentViewModel.PopupDataContext = this;
    //        }

    //        public void OnPostAddPressed(object sender, UserControlBaseEventArgs<AdmissionWoundSite> e)
    //        {
    //            // Trigger Popup Changes - PJSAniMan
    //            var token = "SomethingUniqueEventually?";
    //            Messenger.Default.Send(0, token);

    //            RaisePropertyChanged("ProtectedWound");
    //            RaisePropertyChanged("ProxyPressureUlcerStage");
    //            RaisePropertyChanged("SelectedItem");

    //            IsLockedValue = Protected;
    //            IsSiteLockedValue = Protected;

    //            SlaveValue++;
    //            SelectedItem.UpdateAniManUI();

    //            if (SelectedItem != null)
    //            {
    //                SelectedItem.RefreshRaiseChanged();
    //            }

    //            UpdateUI();
    //        }

    //        public void OnEditPressed(object sender, UserControlBaseEventArgs<AdmissionWoundSite> e)
    //        {
    //            // Trigger Popup Changes - PJSAniMan
    //            var token = "SomethingUniqueEventually?";
    //            Messenger.Default.Send(SelectedItem.AdmissionWoundSiteKey, token);

    //            // Need to set the siteNumber - to remove WoundPhotos from a Cancel event later
    //            // if the photo is new
    //            // Only on Edit are we sure we have a TRUE SelectedItem we're working with
    //            RaisePropertyChanged("ProtectedWound");
    //            RaisePropertyChanged("ProxyPressureUlcerStage");

    //            siteNumber = SelectedItem.Number;

    //            // Try pointing to one with a photo first
    //            WoundPhoto cp = SelectedItem.Admission.WoundPhoto.Where(w => w.Number == siteNumber && w.Photo != null)
    //                .OrderByDescending(w => w.PhotoDate).FirstOrDefault();
    //            if (cp == null)
    //            {
    //                cp = SelectedItem.Admission.WoundPhoto.Where(w => w.Number == siteNumber && w.Photo == null)
    //                    .OrderByDescending(w => w.PhotoDate).FirstOrDefault();
    //            }

    //            CurrentPhoto = cp;
    //            if (SelectedItem.Version == 1)
    //            {
    //                PopupDataTemplate = "WoundPopupDataTemplate";
    //            }
    //            else
    //            {
    //                PopupDataTemplate = "WoundPopupDataTemplateV2";
    //            }

    //            ParentViewModel.PopupDataContext = this;

    //            IsLockedValue = Protected;
    //            IsSiteLockedValue = (SelectedItem.AdmissionWoundSiteKey > 0) ||
    //                                (SelectedItem.AddedFromEncounterKey != Encounter.EncounterKey);
    //        }

    //        public void OnCancelPressed(object sender, UserControlBaseEventArgs<AdmissionWoundSite> e)
    //        {
    //            if (null != CurrentPhoto)
    //            {
    //                CurrentPhoto.CancelEditting();
    //            }

    //            // This currently remove ALL the photos that are NEW for the site with Number
    //            // Need to see if we can flag WoundPhotos as post Ok
    //            if (e.Entity != null)
    //            {
    //                foreach (WoundPhoto x in e.Entity.Admission.WoundPhoto.Where(wp =>
    //                             wp.IsNew && wp.IsOKed == false && wp.Number == siteNumber))
    //                {
    //                    e.Entity.Admission.WoundPhoto.Remove(x);

    //                    //NEED to remove from MODEL else get :
    //                    //      The INSERT statement conflicted with the FOREIGN KEY constraint "FK_WoundPhoto_AdmissionKey". 
    //                    //      The conflict occurred in database "CrescendoLive", table "dbo.Admission", column 'AdmissionKey'.
    //                    Model.Remove(x);
    //                }
    //            }

    //            Reset();
    //        }

    //        public void OnDeletePressed(object sender, UserControlBaseEventArgs<AdmissionWoundSite> e)
    //        {
    //            Reset();
    //        }

    //        public override bool Validate()
    //        {
    //            bool AllValid = true;

    //            if ((SelectedItem.IsTypePressureUlcer) && (SelectedItem.PressureUlcerStage == null))
    //            {
    //                AddErrorForProperty("ProxyPressureUlcerStage",
    //                    "Pressure Ulcer Stage is required for pressure ulcer wounds");
    //                AllValid = false;
    //            }
    //            else
    //            {
    //                ClearErrorFromProperty("ProxyPressureUlcerStage");
    //            }

    //            AllValid = base.Validate() && AllValid;
    //            if (SelectedItem.DateFirstIdentified.HasValue)
    //            {
    //                SelectedItem.DateFirstIdentified = ((DateTime)SelectedItem.DateFirstIdentified).Date;
    //                if (((DateTime)SelectedItem.DateFirstIdentified).Date > DateTime.Today.Date)
    //                {
    //                    SelectedItem.ValidationErrors.Add(new ValidationResult(
    //                        "Date First Identified cannot be a future date", new[] { "DateFirstIdentified" }));
    //                    AllValid = false;
    //                }
    //            }

    //            return AllValid;
    //        }

    //        public async void OnOKPressed(object sender, UserControlBaseEventArgs<AdmissionWoundSite> e)
    //        {
    //            RaisePropertyChanged("ProxyPressureUlcerStage");

    //            if (null != CurrentPhoto)
    //            {
    //                CurrentPhoto.EndEditting();
    //            }

    //            // can be one and only one most problematic pressure ulcer, surgical wound and stasis ulcer
    //            List<AdmissionWoundSite> awsList = null;
    //            if (e.Entity.MostProblematic)
    //            {
    //                // This was specified as most problematic - turn off all others of this type
    //                awsList = FilteredItemsSource.OfType<AdmissionWoundSite>().Where(w => w.MostProblematic).ToList();
    //                if (awsList != null)
    //                {
    //                    foreach (AdmissionWoundSite w in awsList)
    //                        if ((w != e.Entity) && ((w.IsTypePressureUlcer && e.Entity.IsTypePressureUlcer) ||
    //                                                (w.IsTypeStasisUlcer && e.Entity.IsTypeStasisUlcer) ||
    //                                                (w.IsTypeSurgicalWound && e.Entity.IsTypeSurgicalWound)))
    //                        {
    //                            w.MostProblematic = false;
    //                        }
    //                }
    //            }

    //            // if there is only one unhealed observable pressure ulcer, surgical wound or stasis ulcer - make it the most problematic
    //            awsList = FilteredItemsSource.OfType<AdmissionWoundSite>()
    //                .Where(w => w.IsUnhealedPressureUlcerStageIIorHigherObservable).ToList();
    //            if (awsList != null)
    //            {
    //                if ((awsList.Count == 1) && (awsList.FirstOrDefault() != null))
    //                {
    //                    awsList.FirstOrDefault().MostProblematic = true;
    //                }
    //            }

    //            awsList = FilteredItemsSource.OfType<AdmissionWoundSite>().Where(w => w.IsUnhealedStatisUlcerObservable)
    //                .ToList();
    //            if (awsList != null)
    //            {
    //                if ((awsList.Count == 1) && (awsList.FirstOrDefault() != null))
    //                {
    //                    awsList.FirstOrDefault().MostProblematic = true;
    //                }
    //            }

    //            awsList = FilteredItemsSource.OfType<AdmissionWoundSite>().Where(w => w.IsUnhealedSurgicalWoundObservable)
    //                .ToList();
    //            if (awsList != null)
    //            {
    //                if ((awsList.Count == 1) && (awsList.FirstOrDefault() != null))
    //                {
    //                    awsList.FirstOrDefault().MostProblematic = true;
    //                }
    //            }

    //            // make the current siteNumber PhotoWounds marked as IsOKed?
    //            foreach (WoundPhoto wp in e.Entity.Admission.WoundPhoto.Where(wp => wp.IsNew && wp.Number == siteNumber))
    //                wp.IsOKed = true;
    //            Reset();

    //            // If we are in a Re-Evaluate section popup - even though we set the PopupDataContext to null above - it will now point to the re-eval popup itself
    //            // If that is the case - defer the save to the underlying re-eval popup 'Include in this Encounter' command - since they may also choose 'cancel' from the underlying re-eval popup
    //            if (ParentViewModel.PopupDataContext != null)
    //            {
    //                return;
    //            }

    //            DynamicFormViewModel vm = ParentViewModel as DynamicFormViewModel;
    //            if (vm != null)
    //            {
    //                await vm.AutoSave_Command("PatientWoundControlReEvalOK");
    //            }
    //        }

    //        public IPatientService Model
    //        {
    //            get { return (IPatientService)GetValue(ModelProperty); }
    //            set { SetValue(ModelProperty, value); }
    //        }

    //        public static readonly DependencyProperty ModelProperty =
    //            DependencyProperty.Register("Model", typeof(IPatientService), typeof(PatientWoundUserControl), null);

    //        public override void RemoveFromModel(AdmissionWoundSite entity)
    //        {
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            if (HavePhotos)
    //            {
    //                foreach (WoundPhoto x in SelectedItem.Admission.WoundPhoto.Where(wp =>
    //                             wp.Number == siteNumber && wp.AddedFromEncounterKey == Encounter.EncounterKey))
    //                    Model.Remove(x);
    //            }

    //            //NOTE: DetailUserControlBase will only call RemoveFromModel if (entity.EntityState == EntityState.New)
    //            Model.Remove(entity);

    //            //UI should enforce only allowing edits for a single parent entity and it's children entities at a time...
    //            //Removing the current entity, so remove everything that might have been done during the current 
    //            //edit session - e.g. toss everything, since we allow adding related data to new parent entities...
    //        }

    //        public override void SaveModel(UserControlBaseCommandType command)
    //        {
    //            //Need to issue SAVE - regardless of whether command = OK or CANCEL...
    //            //because there could be pending saves and the last user initiated action
    //            //could have been a CANCEL.
    //            if (Model == null)
    //            {
    //                throw new ArgumentNullException("Model", "Model is NULL");
    //            }

    //            //FYI: for now letting the view models handle Model.SaveAllAsync();
    //        }

    //        protected void Reset()
    //        {
    //            CurrentPhoto = null;
    //            ParentViewModel.PopupDataContext = null;
    //            UpdateUI();
    //        }

    //        WoundPhoto _currentPhoto;

    //        public WoundPhoto CurrentPhoto
    //        {
    //            get { return _currentPhoto; }
    //            set
    //            {
    //                _currentPhoto = value;

    //                UpdateUI();
    //            }
    //        }

    //        public bool CurrentPhotoPhotoAvailable
    //        {
    //            get
    //            {
    //                if (_currentPhoto == null)
    //                {
    //                    return true;
    //                }

    //                return (_currentPhotoPhoto != null);
    //            }
    //        }

    //        public byte[] CurrentPhotoPhoto
    //        {
    //            get
    //            {
    //                RaisePropertyChanged("CurrentPhotoPhotoAvailable");
    //                RaisePropertyChanged("CurrentPhotoNotAvailableBlirb");
    //                return _currentPhotoPhoto;
    //            }
    //        }

    //        private byte[] _currentPhotoPhoto
    //        {
    //            get
    //            {
    //                if (_currentPhoto == null)
    //                {
    //                    return null;
    //                }

    //                if (_currentPhoto.Photo != null)
    //                {
    //                    return _currentPhoto.Photo;
    //                }

    //                if ((_currentPhoto.Number != AdditionalWoundPhotoNumber) || (AdditionalWoundPhotoList == null))
    //                {
    //                    return null;
    //                }

    //                WoundPhoto wp = AdditionalWoundPhotoList.FirstOrDefault(w => w.WoundPhotoKey == _currentPhoto.WoundPhotoKey);
    //                if (wp == null)
    //                {
    //                    return null;
    //                }

    //                return wp.Photo;
    //            }
    //        }

    //        public string CurrentPhotoNotAvailableBlirb =>
    //            string.Format("Photo(s) not currently available, {0}",
    //                (IsBusyLoadingAdditionalWoundPhotos == false) ? "due to offline." : "loading previous photo(s)...");

    //        public string CurrentPhotoDate
    //        {
    //            get
    //            {
    //                if (null == CurrentPhoto)
    //                {
    //                    return string.Empty;
    //                }

    //                return CurrentPhoto.PhotoDate.DateTime.ToString("M/d/yyyy HH:mm");
    //            }
    //        }

    //        public bool HavePhotos
    //        {
    //            get
    //            {
    //                if (SelectedItem == null || SelectedItem.Admission == null || SelectedItem.Admission.WoundPhoto == null)
    //                {
    //                    return false;
    //                }

    //                return (SelectedItem.Admission.WoundPhoto.Count(wp => wp.Number == siteNumber) > 0);
    //            }
    //        }

    //        public bool HavePhotoPhotos
    //        {
    //            get
    //            {
    //                if (SelectedItem == null || SelectedItem.Admission == null || SelectedItem.Admission.WoundPhoto == null)
    //                {
    //                    return false;
    //                }

    //                return SelectedItem.Admission.WoundPhoto.Any(wp =>
    //                    wp.Number == siteNumber && wp.Photo != null); // do we have any Photos for this site
    //            }
    //        }

    //        public bool ShowInactiveCheckbox => CurrentPhoto != null && (CurrentPhoto.IsEditting || CurrentPhoto.Inactive);

    //        public bool HasOlderPhoto
    //        {
    //            get
    //            {
    //                if (null == CurrentPhoto || CurrentPhoto.IsEditting)
    //                {
    //                    return false;
    //                }

    //                if (CurrentPhotoPhotoAvailable == false)
    //                {
    //                    return false; // true first time in when haven't downloaded photos yet - there may be photos though
    //                }

    //                var ret = ClientSequencedWoundPhotoList.Any(w => w.ClientSequence < (CurrentPhoto.ClientSequence));
    //                return ret;
    //            }
    //        }

    //        public bool HasNewerPhoto
    //        {
    //            get
    //            {
    //                if (null == CurrentPhoto || CurrentPhoto.IsEditting)
    //                {
    //                    return false;
    //                }

    //                return ClientSequencedWoundPhotoList.Any(w => w.ClientSequence > (CurrentPhoto.ClientSequence));
    //            }
    //        }

    //#if OPENSILVER
    //        private async void OnImportExecute()
    //        {
    //            if(null != CurrentPhoto)
    //            {
    //                CurrentPhoto.EndEditting();
    //            }

    //            var ofd = new FileDialogs.OpenFileDialog();
    //            ofd.Multiselect = false;
    //            // SelectedItem could be NULL if adding new 1st Wound to Encounter/Admission
    //            //INFO: Only allow JPG and PNG - DO NOT allow GIF, Silverlight does not support GIF.
    //            //      Using GIF bytes will GPF the application when used as 'source' to image controls.
    //            ofd.Filter = "Image files (*.jpg;*.png)|*.jpg;*.png|JPEG file|*.jpg|PNG file|*.png";
    //            ofd.ResultKind = FileDialogs.ResultKind.DataURL;
    //            if(await ofd.ShowDialog() == true)
    //            {
    //                var ff = ofd.File;
    //                if(ff != null)
    //                {
    //                    byte[] photo = null;

    //                    using(Stream stream = new MemoryStream(ff.Buffer))
    //                    {
    //                        long numBytes = ff.Buffer.Length;
    //                        photo = await Client.Utils.ImageToolsUtility.CreateThumbnailImage(stream, 360 * 2, numBytes);
    //                        GC.Collect();
    //                    }

    //                    // create a new one each time
    //                    CurrentPhoto = new WoundPhoto
    //                    {
    //                        Photo = photo,
    //                        Number = siteNumber, // tie to this wound
    //                        AddedFromEncounterKey = Encounter.EncounterKey,
    //                        PhotoDate = DateTimeOffset.Now,
    //                        TenantID = WebContext.Current.User.TenantID,
    //                        UpdatedBy = WebContext.Current.User.MemberID,
    //                        UpdatedDate = DateTime.Now
    //                    };

    //                    SelectedItem.Admission.WoundPhoto.Add(CurrentPhoto);
    //                    EditPhoto();
    //                }
    //            }
    //        }
    //#else
    //        private void OnImportExecute()
    //        {
    //            if (null != CurrentPhoto)
    //            {
    //                CurrentPhoto.EndEditting();
    //            }

    //            OpenFileDialog ofd = new OpenFileDialog();
    //            ofd.Multiselect = false;
    //            // SelectedItem could be NULL if adding new 1st Wound to Encounter/Admission
    //            //INFO: Only allow JPG and PNG - DO NOT allow GIF, Silverlight does not support GIF.
    //            //      Using GIF bytes will GPF the application when used as 'source' to image controls.
    //            ofd.Filter = "Image files (*.jpg;*.png)|*.jpg;*.png|JPEG file|*.jpg|PNG file|*.png";
    //            if ((bool)ofd.ShowDialog())
    //            {
    //                FileInfo fi = ofd.File;
    //                if (fi != null)
    //                {
    //                    byte[] photo = null;

    //                    using (Stream stream = fi.OpenRead())
    //                    {
    //                        string name = fi.Name;
    //                        long numBytes = fi.Length;

    //                        photo = Client.Utils.ImageToolsUtility.CreateThumbnailImage(stream, 360 * 2, numBytes);

    //                        GC.Collect();
    //                    }

    //                    // create a new one each time
    //                    CurrentPhoto = new WoundPhoto
    //                    {
    //                        Photo = photo,
    //                        Number = siteNumber, // tie to this wound
    //                        AddedFromEncounterKey = Encounter.EncounterKey,
    //                        PhotoDate = DateTimeOffset.Now,
    //                        TenantID = WebContext.Current.User.TenantID,
    //                        UpdatedBy = WebContext.Current.User.MemberID,
    //                        UpdatedDate = DateTime.Now
    //                    };

    //                    SelectedItem.Admission.WoundPhoto.Add(CurrentPhoto);
    //                    EditPhoto();
    //                }
    //            }
    //        }
    //#endif
    //        private List<WoundPhoto> ClientSequencedWoundPhotoList
    //        {
    //            get
    //            {
    //                // show wound photos with duplicate date.times
    //                int i = 1;
    //                foreach (WoundPhoto wp in SelectedItem.Admission.WoundPhoto.Where(w => w.Number == siteNumber)
    //                             .OrderBy(w => w.PhotoDate).ThenBy(w => w.WoundPhotoKey)) wp.ClientSequence = i++;
    //                return SelectedItem.Admission.WoundPhoto.Where(w => w.Number == siteNumber)
    //                    .OrderBy(w => w.ClientSequence).ToList();
    //            }
    //        }

    //        private void GotoOlderPhoto()
    //        {
    //            CurrentPhoto = ClientSequencedWoundPhotoList.FirstOrDefault(w => w.ClientSequence == (CurrentPhoto.ClientSequence - 1));
    //        }

    //        private void MeasurementHistory()
    //        {
    //            if (SelectedItem == null)
    //            {
    //                return;
    //            }

    //            if (SelectedItem.Admission == null)
    //            {
    //                return;
    //            }

    //            WoundMeasurementHistory wmh = new WoundMeasurementHistory(SelectedItem, Encounter);
    //            wmh.Show();
    //        }

    //        public string GraphicalSelectorButtonText =>
    //            AniManShown ? "Hide Anatomical Silhouette" : "Show Anatomical Silhouette";

    //        private void GotoNewerPhoto()
    //        {
    //            CurrentPhoto = ClientSequencedWoundPhotoList.FirstOrDefault(w => w.ClientSequence == (CurrentPhoto.ClientSequence + 1));
    //        }

    //        public override bool HasChanges()
    //        {
    //            if (HavePhotos)
    //            {
    //                foreach (WoundPhoto wp in SelectedItem.Admission.WoundPhoto)
    //                    if (wp.EntityState == EntityState.New || wp.HasChanges)
    //                    {
    //                        return true;
    //                    }
    //            }

    //            return false;
    //        }

    //        private void EditPhoto()
    //        {
    //            if (CurrentPhotoPhoto == null)
    //            {
    //                return;
    //            }

    //            CurrentPhoto.BeginEditting();
    //            UpdateUI();
    //        }

    //        private void CancelPhoto()
    //        {
    //            CurrentPhoto.CancelEditting();
    //            UpdateUI();
    //        }

    //        private void AcceptPhoto()
    //        {
    //            CurrentPhoto.EndEditting();
    //            RaiseCanExecuteChanged();
    //            UpdateUI();
    //        }

    //        private void UpdateUI()
    //        {
    //            RaisePropertyChanged("HavePhotos");
    //            RaisePropertyChanged("ShowInactiveCheckbox");
    //            RaisePropertyChanged("HasOlderPhoto");
    //            RaisePropertyChanged("HasNewerPhoto");
    //            RaisePropertyChanged("CurrentPhoto");
    //            RaisePropertyChanged("CurrentPhotoPhoto");
    //            RaisePropertyChanged("CurrentPhotoDate");
    //            RaisePropertyChanged("CurrentPhotoNotAvailableBlirb");
    //            RaisePropertyChanged("GraphicalSelectorButtonText");
    //            RaisePropertyChanged("AniManShown");
    //            RaisePropertyChanged("AniManLeftDetailShown");
    //            RaisePropertyChanged("AniManRightDetailShown");
    //            RaisePropertyChanged("WoundSiteCountMessage");

    //            if (SelectedItem != null)
    //            {
    //                SelectedItem.UpdateAniManUI();
    //            }

    //            EditPhotoCommand
    //                .RaiseCanExecuteChanged(); // re-evaluate whether it is OK to edit the WoundPhoto information
    //        }

    //        private IWoundPhotoService WPService;
    //        private int AdditionalWoundPhotoNumber;

    //        private List<WoundPhoto> AdditionalWoundPhotoList =>
    //            ((WPService == null) || (WPService.EntitySet_WoundPhoto == null) ||
    //             (WPService.EntitySet_WoundPhoto.Any() == false))
    //                ? null
    //                : WPService.EntitySet_WoundPhoto.ToList();

    //        private bool _IsBusyLoadingAdditionalWoundPhotos;

    //        private bool IsBusyLoadingAdditionalWoundPhotos
    //        {
    //            get { return _IsBusyLoadingAdditionalWoundPhotos; }
    //            set
    //            {
    //                _IsBusyLoadingAdditionalWoundPhotos = value;
    //                RaisePropertyChanged("CurrentPhotoNotAvailableBlirb");

    //                DynamicFormViewModel df = ParentViewModel as DynamicFormViewModel;
    //                if (df == null)
    //                {
    //                    return;
    //                }

    //                df.IsBusy = value;
    //            }
    //        }

    //        private void OnFetchWoundSitePhotos()
    //        {
    //            LoadAdditionalWoundPhotos(SelectedItem);
    //            UpdateUI();
    //        }

    //        private bool LoadAdditionalWoundPhotos(AdmissionWoundSite aws)
    //        {
    //            // return true if ne need to load the wounds for this wound site number, other wise return false (they are alreay loaded)
    //            if (WPService == null)
    //            {
    //                WPService = new WoundPhotoService();
    //                WPService.OnGetWoundPhotosLoaded += WPService_OnGetWoundPhotosLoaded;
    //            }

    //            if ((aws == null) || (aws.AdmissionKey <= 0))
    //            {
    //                WPService.ClearWoundPhotos();
    //                AdditionalWoundPhotoNumber = 0;
    //                return false;
    //            }

    //            if (aws.Number == AdditionalWoundPhotoNumber)
    //            {
    //                return false;
    //            }

    //            if (EntityManager.Current.IsOnline == false)
    //            {
    //                WPService.ClearWoundPhotos();
    //                AdditionalWoundPhotoNumber = 0;
    //                return false;
    //            }

    //            AdditionalWoundPhotoNumber = aws.Number;
    //            IsBusyLoadingAdditionalWoundPhotos = true;
    //            WPService.GetWoundPhotoByNumberAsync(aws.AdmissionKey, aws.Number);
    //            return true;
    //        }

    //        private void WPService_OnGetWoundPhotosLoaded(object sender, Core.Events.EntityEventArgs<WoundPhoto> e)
    //        {
    //            IsBusyLoadingAdditionalWoundPhotos = false;
    //            if (e.Error != null)
    //            {
    //                ErrorWindow.CreateNew("Error: Loading Additional Wound Photos", e);
    //                WPService.ClearWoundPhotos();
    //                AdditionalWoundPhotoNumber = 0;
    //            }

    //            UpdateUI();
    //        }

    //        #region INotifyDataErrorInfo

    //        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
    //        readonly Dictionary<string, List<string>> _currentErrors;

    //        public IEnumerable GetErrors(string propertyName)
    //        {
    //            if (string.IsNullOrEmpty(propertyName))
    //            {
    //                //FYI: if you are not supporting entity level errors, it is acceptable to return null
    //                var ret = _currentErrors.Values.Where(c => c.Any());
    //                return ret.Any() ? ret : null;
    //            }

    //            MakeOrCreatePropertyErrorList(propertyName);
    //            if (_currentErrors[propertyName].Any())
    //            {
    //                return _currentErrors[propertyName];
    //            }

    //            return null;
    //        }

    //        public bool HasErrors
    //        {
    //            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
    //        }


    //        void FireErrorsChanged(string property)
    //        {
    //            if (ErrorsChanged != null)
    //            {
    //                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
    //            }
    //        }

    //        public void ClearErrorFromProperty(string property)
    //        {
    //            MakeOrCreatePropertyErrorList(property);
    //            _currentErrors[property].Clear();
    //            FireErrorsChanged(property);
    //        }

    //        public void AddErrorForProperty(string property, string error)
    //        {
    //            MakeOrCreatePropertyErrorList(property);
    //            _currentErrors[property].Add(error);
    //            FireErrorsChanged(property);
    //        }

    //        void MakeOrCreatePropertyErrorList(string propertyName)
    //        {
    //            if (!_currentErrors.ContainsKey(propertyName))
    //            {
    //                _currentErrors[propertyName] = new List<string>();
    //            }
    //        }

    //        #endregion INotifyDataErrorInfo
    //    }
}