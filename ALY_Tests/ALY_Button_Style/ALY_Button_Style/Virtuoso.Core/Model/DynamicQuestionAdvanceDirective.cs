#region Usings

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class AdvanceDirective : QuestionBase
    {
        public AdvanceDirective(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private string _PopupDataTemplate = "PatientAdvancedDirectivePopupDataTemplate";

        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                _PopupDataTemplate = value;
                RaisePropertyChanged("PopupDataTemplate");
            }
        }

        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject PopupDataTemplateLoaded
        {
            get
            {
                // Turn off caching of PopupDataTemplateLoaded 0 popup was waking up with 'old' SelectedItem
                //if (DataTemplateHelper == null) DataTemplateHelper = new DataTemplateHelper();
                if (DataTemplateHelper != null)
                {
                    DataTemplateHelper.Cleanup();
                    DataTemplateHelper = null;
                }

                DataTemplateHelper = new DataTemplateHelper();
                return DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }

        public Patient CurrentPatient => Patient;
        private PatientAdvancedDirective _SelectedItem;

        public PatientAdvancedDirective SelectedItem
        {
            get { return _SelectedItem; }
            set
            {
                _SelectedItem = value;
                RaisePropertyChanged("SelectedItem");
            }
        }

        public RelayCommand OK_Command { get; protected set; }
        public RelayCommand Cancel2_Command { get; protected set; }

        public new void Setup()
        {
            SetupCommands();
            SetupEncounterData();
        }

        private void SetupCommands()
        {
            OK_Command = new RelayCommand(() =>
            {
                if (SelectedItem == null)
                {
                    return;
                }

                if (SelectedItem.Validate())
                {
                    if (((SelectedItem.AdvancedDirectiveTypeCode.ToLower() == "dnr") ||
                         (SelectedItem.AdvancedDirectiveTypeCode.ToLower() == "communitydnr")) == false)
                    {
                        SelectedItem.SigningPhysicianKey = null;
                    }

                    if (SelectedItem.IsNew)
                    {
                        Patient.PatientAdvancedDirective.Add(SelectedItem);
                    }

                    if (DynamicFormViewModel != null)
                    {
                        DynamicFormViewModel.PopupDataContext = null;
                    }

                    ResetEncounterData();
                }
            });
            Cancel2_Command = new RelayCommand(() =>
            {
                if (SelectedItem == null)
                {
                    return;
                }

                if (DynamicFormViewModel != null)
                {
                    ((IPatientService)DynamicFormViewModel.FormModel).Remove(SelectedItem);
                }

                if (SelectedItem.CurrentPatient != null)
                {
                    SelectedItem.CurrentPatient = null;
                }

                if (SelectedItem.Patient != null)
                {
                    SelectedItem.Patient = null;
                }

                SelectedItem = null;
                if (DynamicFormViewModel != null)
                {
                    DynamicFormViewModel.PopupDataContext = null;
                }

                ResetEncounterData(); // Probably... reset to 'No', the patient does NOT have an advance directive
            });
        }

        private void SetupEncounterData()
        {
            if ((DynamicFormViewModel == null) || (Encounter == null))
            {
                return;
            }

            EncounterData = DynamicFormViewModel.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == DynamicFormViewModel.CurrentEncounter.EncounterKey &&
                x.SectionKey == Section.SectionKey &&
                x.QuestionGroupKey == QuestionGroupKey && x.QuestionKey == Question.QuestionKey).FirstOrDefault();
            if (EncounterData == null)
            {
                EncounterData = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey, BoolData = false
                };
                EncounterData.TextData = "0";
                Encounter.EncounterData.Add(EncounterData);
            }

            if (Encounter.EncounterStatus != (int)EncounterStatusType.Completed)
            {
                ResetEncounterData();
            }

            EncounterData.PropertyChanged += EncounterData_PropertyChanged;
        }

        private void ResetEncounterData()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (EncounterData != null)
                {
                    EncounterData.TextData = HavePatientAdvancedDirective ? "1" : "0";
                }
            });
            if (Patient != null)
            {
                bool hasPOLST = (Patient.ActiveAdvancedDirectivesOfType("POLST") != null);
                Messenger.Default.Send(hasPOLST,
                    string.Format("AdvanceCarePlanChangedPOLSTChanged{0}", Patient.PatientKey.ToString().Trim()));
            }
        }

        private new void EncounterData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            EncounterData ed = sender as EncounterData;
            if ((e.PropertyName == "TextData" && ed != null) == false)
            {
                return;
            }

            if (ed.TextData == "1")
            {
                LaunchAdvanceDirectivePopup();
            }
            else if (ed.TextData == "0")
            {
                ResetEncounterData();
            }
        }

        private bool HavePatientAdvancedDirective
        {
            get
            {
                if ((Encounter == null) || (Patient == null) || (Patient.PatientAdvancedDirective == null))
                {
                    return false;
                }

                DateTime date = (Encounter.EncounterOrTaskStartDateAndTime == null)
                    ? DateTime.Today.Date
                    : ((DateTimeOffset)Encounter.EncounterOrTaskStartDateAndTime).Date;
                return Patient.PatientAdvancedDirective
                    .Where(a => ((a.Inactive == false) && (a.IsCurrentlyActiveAsOfDate(date)))).Any();
            }
        }

        private PatientAdvancedDirective NewAdvancedDirective
        {
            get
            {
                if ((Patient == null || (Admission == null)))
                {
                    return null;
                }

                PatientAdvancedDirective newPAD = new PatientAdvancedDirective();
                newPAD.PatientKey = Patient.PatientKey;
                newPAD.CurrentPatient = Patient;
                newPAD.Patient = Patient;
                if (Admission.AdmissionPhysician != null)
                {
                    AdmissionPhysician ap = Admission.AdmissionPhysician
                        .Where(p => p.Inactive == false)
                        .Where(p => p.Signing)
                        .Where(p => (p.SigningEffectiveFromDate.HasValue &&
                                     p.SigningEffectiveFromDate.Value.Date <= DateTime.Now.Date) &&
                                    ((p.SigningEffectiveThruDate.HasValue == false) ||
                                     (p.SigningEffectiveThruDate.HasValue &&
                                      (p.SigningEffectiveThruDate.Value.Date > DateTime.Now.Date)))
                        ).FirstOrDefault();
                    if (ap != null)
                    {
                        newPAD.SigningPhysicianKey = ap.PhysicianKey;
                    }
                }

                newPAD.EffectiveDate = DateTime.Today;
                PatientAddress pa = Patient.MainAddress(null);
                if ((pa != null))
                {
                    newPAD.RecordedStateCode = pa.StateCode;
                }

                newPAD.Expand = false;
                return newPAD;
            }
        }

        private void LaunchAdvanceDirectivePopup()
        {
            if (HavePatientAdvancedDirective)
            {
                return; // We don't 'launch' to edit we only initially launch to create - go figure...
            }

            SelectedItem = NewAdvancedDirective;
            if (SelectedItem == null)
            {
                return;
            }

            SelectedItem.RaiseChanged();
            DynamicFormViewModel.PopupDataContext = this;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    RaisePropertyChanged("SelectedItem");
                    SelectedItem.RaiseChanged();
                });
            });
        }
    }

    public class AdvanceDirectiveFactory
    {
        public static AdvanceDirective Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            AdvanceDirective qb = new AdvanceDirective(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission
            };
            qb.Setup();
            return qb;
        }
    }
}