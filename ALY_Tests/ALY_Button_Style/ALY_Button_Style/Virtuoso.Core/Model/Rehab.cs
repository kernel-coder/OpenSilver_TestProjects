#region Usings

using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Model
{
    public class Rehab : QuestionUI
    {
        public Rehab(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void PreProcessing()
        {
            if (GoalManager == null)
            {
                return;
            }

            if (GoalManager.PrintCollection == null)
            {
                return;
            }

            if (Admission == null)
            {
                return;
            }

            if (Admission.AdmissionGoal == null)
            {
                return;
            }

            CollectionViewSource cvs = new CollectionViewSource
            {
                Source = Admission.AdmissionGoal
            };
            PrintCollection = cvs.View;
            PrintCollection.Filter = item =>
            {
                bool wanttoaccept = true;

                AdmissionGoal ag = item as AdmissionGoal;
                if (ag == null)
                {
                    return false;
                }

                if (GoalManager != null)
                {
                    wanttoaccept =
                        GoalManager.AcceptGoalForFilter(ag, !PrintInterdisciplinaryView, PrintInterdisciplinaryView);
                }

                return wanttoaccept;
            };
        }

        public override QuestionUI Clone()
        {
            Rehab r = new Rehab(__FormSectionQuestionKey)
            {
                Question = Question,
                IndentLevel = IndentLevel,
                Patient = Patient,
                Encounter = Encounter,
                Admission = Admission,
                GoalManager = GoalManager
            };
            r.SetupOrderEntryProtectedOverrideRunTime();
            r.RehabSetup();
            r.PreProcessing();
            return r;
        }

        private string GoalValidationError
        {
            get { return ValidationError; }
            set
            {
                ValidationError = value;
                this.RaisePropertyChangedLambda(p => p.GoalErrors);
            }
        }

        public string CertPeriodWarning
        {
            get
            {
                if (ShowCertPeriodWarning == false)
                {
                    return null;
                }

                return "WARNING: The period is not established, this may impact your Care Plan dates.";
            }
        }

        private bool ShowCertPeriodWarning
        {
            get
            {
                if ((Encounter != null) && (Encounter.EncounterIsOrderEntry))
                {
                    return false;
                }

                return !IsCertPeriodDefined;
            }
        }

        public string GoalErrors
        {
            get
            {
                string errors = null;
                if (string.IsNullOrWhiteSpace(GoalValidationError) == false)
                {
                    errors = ((errors == null) ? "" : errors + "  ") + GoalValidationError;
                }

                if (string.IsNullOrWhiteSpace(CertPeriodWarning) == false)
                {
                    errors = ((errors == null) ? "" : errors + "  ") + CertPeriodWarning;
                }

                if ((GoalManager != null) &&
                    (string.IsNullOrWhiteSpace(GoalManager.ShortLongTermWarningMessage) == false))
                {
                    errors = ((errors == null) ? "" : errors + "  ") + GoalManager.ShortLongTermWarningMessage;
                }

                return string.IsNullOrWhiteSpace(errors) ? null : errors;
            }
        }

        private double _dynamicFormWellHeight = 300.0;

        public double RehabQuestionHeight
        {
            get
            {
                double height = _dynamicFormWellHeight;
                if (QuestionSequenceWithinSection <= 2)
                {
                    height = height - 40; // Assume just a section header
                }
                else if (QuestionSequenceWithinSection == 3)
                {
                    height = height - 40 - 35; // Assume a section header and one (35 height) question above
                }
                else if (QuestionSequenceWithinSection == 4)
                {
                    height = height - 40 - 70; // Assume a section header and two (35 height) questions above
                }
                else if (QuestionSequenceWithinSection <= 2)
                {
                    height = height - 40 -
                             100; // All else - assume a section header and two (35 height) questions above plus a little more - probably scrolling involved
                }

                return (height < 299) ? 300 : height; // return a minium height of 300
            }
        }


        private bool IsCertPeriodDefined
        {
            get
            {
                bool PdDefined = false;
                AdmissionCertification ac = Admission.GetAdmissionCertForDate(Encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime);
                if (ac != null && Encounter.EncounterOrTaskStartDateAndTime != null 
                               && ac.PeriodStartDate.GetValueOrDefault().Date <= Encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date 
                               && ac.PeriodEndDate.GetValueOrDefault().Date >= Encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date)
                {
                    PdDefined = true;
                }

                return PdDefined;
            }
        }

        public bool PrintInterdisciplinaryView
        {
            set
            {
                printInterdisciplinaryView = value;
                this.RaisePropertyChangedLambda(p => p.PrintInterdisciplinaryView);
            }
            get { return printInterdisciplinaryView; }
        }

        private bool printInterdisciplinaryView;

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            GoalValidationError = null;

            if (Protected)
            {
                return true; // Only validate if they can actually do something about it 
            }

            DateTime date = Encounter.EncounterOrTaskStartDateAndTime == null
                ? DateTime.Today.Date
                : ((DateTimeOffset)Encounter.EncounterOrTaskStartDateAndTime).Date;
            if (Encounter.FullValidation && Admission.SOCDate.HasValue && (IsEval || IsPlanOfCare || IsResumption))
            {
                if ((IsPlanOfCare) && (Encounter.EncounterPlanOfCare.FirstOrDefault() != null))
                {
                    if (Encounter.EncounterPlanOfCare.First().CertificationFromDate != null)
                    {
                        date = ((DateTime)Encounter.EncounterPlanOfCare.First().CertificationFromDate).Date;
                    }
                }

                if (Admission.AdmissionGoal.Where(p => !p.Superceded && p.ActiveAsOfDate(date)).Any() == false)
                {
                    GoalValidationError = "ERROR: At least one goal must be entered.";
                }
                else if (!Admission.HospiceAdmission && Admission.AdmissionGoal.Any(p => !p.Superceded && p.ActiveAsOfDate(date) && p.RequiredForDischarge) == false)
                {
                    // Hospice Admission encounters do not require a discharge goal
                    GoalValidationError = "ERROR: At least one discharge goal must be entered.";
                }
            }

            if (Admission.IsHomeHealth &&
                (Encounter.FullValidation) &&
                (Admission.SOCDate.HasValue) &&
                ((Encounter.PreviousEncounterStatus == (int)EncounterStatusType.None) ||
                 (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Edit)) &&
                (Encounter.IsAssistant == false) &&
                (IsEval || IsVisit || IsResumption))
            {
                if (Admission.AdmissionGoal.Any(p => !p.Superceded && p.ActiveAsOfDate(date) && p.RequiredForDischargePlan) == false)
                {
                    GoalValidationError = "ERROR: At least one discharge plan goal must be entered.";
                }
            }

            bool shouldValidate = Encounter.FullValidation &&
                                  Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Edit;
            bool fullValidation = IsVisit && shouldValidate;
            return GoalManager.ValidateGoals(DisciplineCache.GetIsAssistantFromKey(ServiceTypeCache.GetDisciplineKey(Encounter.ServiceTypeKey.Value).Value), fullValidation) 
                   && GoalValidationError == null;
        }

        private bool IsEval
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsEval == false))
                {
                    return false;
                }

                return true;
            }
        }

        private bool IsVisit
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsVisit == false))
                {
                    return false;
                }

                return true;
            }
        }

        private bool IsPlanOfCare
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsPlanOfCare == false))
                {
                    return false;
                }

                return true;
            }
        }

        private bool IsResumption
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsResumption == false))
                {
                    return false;
                }

                return true;
            }
        }

        private bool IsDischarge
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsDischarge == false))
                {
                    return false;
                }

                return true;
            }
        }

        public RelayCommand DataTemplateLoaded { get; set; }
        public RelayCommand DataTemplateUnLoaded { get; set; }

        public void RehabSetup()
        {
            DataTemplateLoaded = new RelayCommand(() =>
            {
                ProtectedOverrideRunTime = SetupOrderEntryProtectedOverrideRunTime();
                if (OrderEntryManager != null)
                {
                    this.RaisePropertyChangedLambda(p => p.Protected);
                }

                if (GoalManager != null)
                {
                    GoalManager.DataTemplateLoaded();
                }

                this.RaisePropertyChangedLambda(p => p.RehabQuestionHeight);
            });
            DataTemplateUnLoaded = new RelayCommand(() =>
            {

            });
            Messenger.Default.Register<double>(this, "DynamicFormWellHeightChanged", newHeight =>
            {
                _dynamicFormWellHeight = newHeight;
                this.RaisePropertyChangedLambda(p => p.RehabQuestionHeight);
            });
            if (GoalManager != null)
            {
                GoalManager.PopupDataTemplateChanged += GoalManager_OnPopupDataTemplateChanged;
            }

            if (GoalManager != null)
            {
                GoalManager.ShortLongTermWarningMessageChanged += GoalManager_OnShortLongTermWarningMessageChanged;
            }
        }

        private void GoalManager_OnPopupDataTemplateChanged(object sender, EventArgs e)
        {
            if ((GoalManager == null) || (DynamicFormViewModel == null))
            {
                return;
            }

            PopupDataTemplate = (string.IsNullOrWhiteSpace(GoalManager.PopupDataTemplate))
                ? null
                : "Rehab" + GoalManager.PopupDataTemplate; // different ADD templates for Rehab vs RehabPOCEdit vs ... to they are a function of the question dataTemplate
            DynamicFormViewModel.PopupDataContext = (string.IsNullOrWhiteSpace(PopupDataTemplate)) ? null : this;
        }

        private void GoalManager_OnShortLongTermWarningMessageChanged(object sender, EventArgs e)
        {
            RaisePropertyChanged("GoalErrors");
        }

        private string _PopupDataTemplate;

        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                _PopupDataTemplate = value;
                RaisePropertyChanged("PopupDataTemplate");
                RaisePropertyChanged("PopupDataTemplateLoaded");
            }
        }

        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject PopupDataTemplateLoaded
        {
            get
            {
                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }

                return DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }

        public override void Cleanup()
        {
            Messenger.Default.Unregister<double>(this, "DynamicFormWellHeightChanged");
            Messenger.Default.Unregister(this);
            try
            {
                if (GoalManager != null)
                {
                    GoalManager.PopupDataTemplateChanged -= GoalManager_OnPopupDataTemplateChanged;
                }

                if (GoalManager != null)
                {
                    GoalManager.ShortLongTermWarningMessageChanged -= GoalManager_OnShortLongTermWarningMessageChanged;
                }
            }
            catch (Exception)
            {
            }

            base.Cleanup();
        }

        public OrderEntryManager OrderEntryManager;

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
            return (!OrderEntryManager.CurrentOrderEntry.CanEditOrderReviewed);
        }
    }

    public class RehabFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            Rehab r = new Rehab(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                GoalManager = vm.CurrentGoalManager,
                OasisManager = vm.CurrentOasisManager,
                OrderEntryManager = vm.CurrentOrderEntryManager,
                DynamicFormViewModel = vm,
            };
            r.SetupOrderEntryProtectedOverrideRunTime();
            r.RehabSetup();
            return r;
        }
    }

    public class RehabInterdiscViewFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            Rehab r = RehabFactory.Create(m, vm, formsection, pqgkey, qgkey, sequence, copyforward, q) as Rehab;
            if (r != null)
            {
                r.PrintInterdisciplinaryView = true;
                var gm = r.GoalManager as GoalManager;
                if (gm != null)
                {
                    gm.VisitPlanChecked = false;
                    gm.PlanHistoryChecked = false;
                    gm.InterdisciplinaryPlanChecked = true;
                }
            }

            return r;
        }
    }
}