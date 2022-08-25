#region Usings

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Model
{
    public class DisciplineReferral : QuestionUI
    {
        public override void Cleanup()
        {
            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChildComboBoxInput(null);
            }

            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChildTextBoxInput(null);
            }

            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChild(null);
            }

            TriggerButton = null;
            PopupControl = null;
            SetupPopupProvider();
            base.Cleanup();
        }

        public DisciplineReferral(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            ProcessGoals = new RelayCommand(() => { });
        }

        private bool inAddDisciplineReferral;

        public void DisciplineReferralSetup()
        {
            DataTemplateLoaded = new RelayCommand(() =>
            {
                ProtectedOverrideRunTime = SetupOrderEntryProtectedOverrideRunTime();
                if (OrderEntryManager != null)
                {
                    this.RaisePropertyChangedLambda(p => p.Protected);
                }
            });
            AddDisciplineReferralCommand = new RelayCommand(() =>
            {
                inAddDisciplineReferral = true;
                // Insure no discipline selection on initial popup of list
                AvailableDisciplines.View.MoveCurrentTo(null);
                ProxyAvailableDisciplines = null;
                RefreshAvailableDisciplines();
                SelectedDiscipline = null;
                this.RaisePropertyChangedLambda(p => p.SelectedDiscipline);
                if (_popupProvider != null)
                {
                    _popupProvider.TriggerClick(); // popup the list
                }
            });
            DeleteDisciplineReferralCommand = new RelayCommand<AdmissionDiscipline>(admissionDiscipline =>
            {
                if (admissionDiscipline == null)
                {
                    return;
                }

                if (admissionDiscipline.IsNew == false)
                {
                    return;
                }

                ((IPatientService)DynamicFormViewModel.FormModel).Remove(admissionDiscipline);
                SetupDisciplineReferrals();
                RefreshAvailableDisciplines();
            });
            PopupTriggerLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                TriggerButton = frameworkElement;
                SetupPopupProvider();
            });
            PopupTriggerUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                //TriggerButton = null;
                //SetupPopupProvider();
            });
            PopupLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                PopupControl = frameworkElement as Popup;
                SetupPopupProvider();
            });
            PopupUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                //PopupControl = null;
                //SetupPopupProvider();
            });
            PopupChildLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                ProxyAvailableDisciplines = AvailableDisciplines;
                ProxyAvailableDisciplines.View.MoveCurrentTo(null);
                if (_popupProvider != null)
                {
                    _popupProvider.SetPopupChild(frameworkElement);
                }
            });
            PopupChildUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                //if (_popupProvider != null) _popupProvider.SetPopupChild(null);
            });
            ProtectedOverrideRunTime = SetupOrderEntryProtectedOverrideRunTime();
            SetupDisciplineReferrals();
            SetupAvailableDisciplines();
        }

        public bool? SetupOrderEntryProtectedOverrideRunTime()
        {
            // If not an order - do not override Protection (VO orders don't count)
            if (OrderEntryManager == null)
            {
                return null;
            }

            if (OrderEntryManager.IsVO)
            {
                return null;
            }

            if (Encounter == null)
            {
                return null;
            }

            if (Encounter.EncounterIsOrderEntry == false)
            {
                return null;
            }

            // Everything is protected on inactive forms
            if (Encounter.Inactive)
            {
                return true;
            }

            if (OrderEntryManager.CurrentOrderEntry == null)
            {
                return true;
            }

            // the clinician who 'owns' the order can edit it if its in an edit state (and not voided)
            if ((Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                (Encounter.EncounterStatus == (int)EncounterStatusType.Edit))
            {
                return (OrderEntryManager.CurrentOrderEntry.OrderStatus == (int)OrderStatusType.Voided) ? true : false;
            }

            // anyone with OrderEntry role when the form is in orderentry review
            return (OrderEntryManager.CurrentOrderEntry.CanEditOrderReviewed) ? false : true;
        }

        private FrameworkElement TriggerButton;
        private Popup PopupControl;
        private PopupProvider _popupProvider;

        private void SetupPopupProvider()
        {
            if ((TriggerButton == null) || (PopupControl == null))
            {
                if (_popupProvider != null)
                {
                    _popupProvider.Cleanup();
                    _popupProvider = null;
                }

                return;
            }

            if (_popupProvider == null)
            {
                _popupProvider = new PopupProvider(TriggerButton, TriggerButton, PopupControl, null, Direction.Bottom);
            }
        }

        private void SetupDisciplineReferrals()
        {
            DisciplineReferrals = new ObservableCollection<AdmissionDiscipline>();
            foreach (AdmissionDiscipline ad in Admission.AdmissionDiscipline
                         .Where(x => (x.IsNew) || (x.AddedFromEncounterKey == Encounter.EncounterKey))
                         .OrderBy(x => x.DisciplineDescription)) DisciplineReferrals.Add(ad);
            this.RaisePropertyChangedLambda(p => p.DisciplineReferrals);
        }

        private void SetupAvailableDisciplines()
        {
            _AvailableDisciplines = new CollectionViewSource();
            AvailableDisciplines.Source = DisciplineCache.GetActiveDisciplines();
            AvailableDisciplines.SortDescriptions.Add(new SortDescription("Description", ListSortDirection.Ascending));
            AvailableDisciplines.Filter += (s, ad) =>
            {
                ad.Accepted = false; // assume not accepted
                Discipline d = ad.Item as Discipline;

                if (d == null || Admission == null || Admission.SelectedServiceLine == null)
                {
                    return;
                }

                var mask = TenantSettingsCache.Current.ServiceLineTypeUseBits;
                if ((d.ServiceLineTypeUseBits & mask) ==
                    0) // Do discipline have anything in common with the tenant's purchases?
                {
                    return;
                }

                if ((Admission.ServiceLineType & d.ServiceLineTypeUseBits) == 0)
                {
                    return;
                }

                if (Admission.ActiveAdmissionDisciplines == null)
                {
                    ad.Accepted = true;
                }
                else
                {
                    ad.Accepted =
                        Admission.ActiveAdmissionDisciplines.Where(a => a.DisciplineKey == d.DisciplineKey).Any()
                            ? false
                            : true;
                }

                if (ad.Accepted && d.DisciplineKey > 0)
                {
                    AvailableDisciplineCount++;
                }
            };
        }

        private void RefreshAvailableDisciplines()
        {
            AvailableDisciplineCount = 0;
            AvailableDisciplines.View.Refresh();
        }

        private CollectionViewSource _ProxyAvailableDisciplines;

        public CollectionViewSource ProxyAvailableDisciplines
        {
            get { return _ProxyAvailableDisciplines; }
            set
            {
                _ProxyAvailableDisciplines = value;
                RaisePropertyChanged("ProxyAvailableDisciplines");
            }
        }

        private CollectionViewSource _AvailableDisciplines = new CollectionViewSource();

        public CollectionViewSource AvailableDisciplines
        {
            get { return _AvailableDisciplines; }
            set
            {
                _AvailableDisciplines = value;
                RaisePropertyChanged("AvailableDisciplines");
            }
        }

        int _AvailableDisciplineCount;

        public int AvailableDisciplineCount
        {
            get { return _AvailableDisciplineCount; }
            set
            {
                _AvailableDisciplineCount = value;
                RaisePropertyChanged("AvailableDisciplineCount");
            }
        }

        public ObservableCollection<AdmissionDiscipline> DisciplineReferrals { get; set; }

        public RelayCommand DataTemplateLoaded { get; set; }
        public RelayCommand AddDisciplineReferralCommand { get; set; }
        public RelayCommand<AdmissionDiscipline> DeleteDisciplineReferralCommand { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildUnLoaded { get; set; }

        private Discipline _SelectedDiscipline;

        public Discipline SelectedDiscipline
        {
            get { return _SelectedDiscipline; }
            set
            {
                if (value == null)
                {
                    return;
                }

                if (inAddDisciplineReferral == false)
                {
                    return;
                }

                inAddDisciplineReferral = false;

                _SelectedDiscipline = value;
                if (_popupProvider != null)
                {
                    _popupProvider.ForceClosePopup();
                }

                if ((_SelectedDiscipline != null) && ((Admission.ActiveAdmissionDisciplines == null) ||
                                                      (Admission.ActiveAdmissionDisciplines.Where(a =>
                                                           a.DisciplineKey == _SelectedDiscipline.DisciplineKey)
                                                       .Any() ==
                                                       false)))
                {
                    AdmissionDiscipline ad = new AdmissionDiscipline
                    {
                        AddedFromEncounterKey = Encounter.EncounterKey,
                        AdmissionKey = Admission.AdmissionKey,
                        AdmissionStatus = (int)CodeLookupCache.GetKeyFromCode("AdmissionStatus", "R"),
                        DisciplineKey = _SelectedDiscipline.DisciplineKey,
                        ReferDateTime = (OrderEntryManager == null)
                            ? DateTime.Now
                            : ((OrderEntryManager.CurrentIOrderEntry == null)
                                ? DateTime.Now
                                : ((OrderEntryManager.CurrentIOrderEntry.CompletedDate == null)
                                    ? DateTime.Now
                                    : OrderEntryManager.CurrentIOrderEntry.CompletedDate.Value.DateTime)),
                        ReferralReason = "Ordered by physician " + ((OrderEntryManager == null)
                            ? ""
                            : ((OrderEntryManager.CurrentIOrderEntry == null)
                                ? ""
                                : ((OrderEntryManager.CurrentIOrderEntry.SigningPhysicianName == null)
                                    ? ""
                                    : OrderEntryManager.CurrentIOrderEntry.SigningPhysicianName))),
                        AgencyDischarge = false,
                        OverrideAgencyDischarge = false
                    };
                    Admission.AdmissionDiscipline.Add(ad);
                    Encounter.AdmissionDiscipline_1.Add(ad); // to set AddedFromEncounterKey if new encounters
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        SetupDisciplineReferrals();
                        RefreshAvailableDisciplines();
                    });
                }
            }
        }

        public OrderEntryManager OrderEntryManager { get; set; }

        EncounterData CopyProperties(EncounterData source)
        {
            EncounterData EncounterData = new EncounterData();
            return EncounterData;
        }

        public override bool CopyForwardLastInstance()
        {
            return false;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
        }

        public override void BackupEntity(bool restore)
        {
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (AdmissionDiscipline ad in DisciplineReferrals)
            {
                ad.ValidationErrors.Clear();
                if (string.IsNullOrWhiteSpace(ad.ReferralReason))
                {
                    ad.ValidationErrors.Add(new ValidationResult("The Referral Reason field is required.",
                        new[] { "ReferralReason" }));
                    AllValid = false;
                }

                if (ad.ReferDateTime == null)
                {
                    ad.ValidationErrors.Add(new ValidationResult("The Referral Date field is required.",
                        new[] { "ReferDateTime" }));
                    AllValid = false;
                }
            }

            return AllValid;
        }
    }

    public class DisciplineReferralFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            DisciplineReferral dr = new DisciplineReferral(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                OrderEntryManager = vm.CurrentOrderEntryManager,
                DynamicFormViewModel = vm,
            };
            dr.DisciplineReferralSetup();
            return dr;
        }
    }
}