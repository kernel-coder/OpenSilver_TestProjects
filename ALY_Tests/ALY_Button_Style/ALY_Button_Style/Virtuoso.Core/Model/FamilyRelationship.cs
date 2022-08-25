#region Usings

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.View;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class FamilyRelationship : QuestionUI
    {
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

        public FamilyRelationship(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void FamilyRelationshipSetup()
        {
            Setup_PatientContact();
        }

        public ObservableCollection<QuestionBase> Contacts { get; set; }
        public ObservableCollection<QuestionBase> BackupContacts { get; set; }

        EncounterData CopyProperties(EncounterData source)
        {
            EncounterData EncounterData = new EncounterData();
            EncounterData.TextData = source.TextData;
            EncounterData.Text2Data = source.Text2Data;
            EncounterData.Text3Data = source.Text3Data;
            EncounterData.Text4Data = source.Text4Data;
            EncounterData.AddedDateTime = source.AddedDateTime;
            EncounterData.GuidData = source.GuidData;
            EncounterData.IntData = source.IntData;
            EncounterData.Int2Data = source.Int2Data;
            EncounterData.BoolData = source.BoolData;
            EncounterData.DateTimeData = source.DateTimeData;
            EncounterData.RealData = source.RealData;
            EncounterData.FuncDeficit = source.FuncDeficit;
            EncounterData.SignatureData = source.SignatureData;
            EncounterData.SectionKey = Section.SectionKey;
            EncounterData.QuestionGroupKey = QuestionGroupKey;
            EncounterData.QuestionKey = Question.QuestionKey;
            return EncounterData;
        }

        public override bool CopyForwardLastInstance()
        {
            Contacts = new ObservableCollection<QuestionBase>();

            foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                foreach (var ed in item.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey))
                    if (ed != null)
                    {
                        Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                        {
                            Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                            Patient = Patient, Question = Question
                        });
                    }

                break;
            }

            //default with one and allow more to be added
            if (Contacts.Any() == false)
            {
                EncounterData ed = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey
                };
                Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = ed, Patient = Patient,
                    Question = Question
                });
            }

            return true;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
            Contacts = new ObservableCollection<QuestionBase>();
            foreach (var item in e.EncounterData.Where(p =>
                         p.QuestionKey == Question.QuestionKey && p.QuestionGroupKey == QuestionGroupKey &&
                         p.Section.Label == Section.Label))
            {
                if (item != null)
                {
                    Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(item),
                        Patient = Patient, Question = Question
                    });
                }
            }

            //default with one and allow more to be added
            if (Contacts.Any() == false)
            {
                EncounterData ed = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey
                };
                Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = ed, Patient = Patient,
                    Question = Question
                });
            }
        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if (Contacts != null)
                {
                    foreach (var item in Contacts)
                        try
                        {
                            ((IPatientService)DynamicFormViewModel.FormModel).Remove(item.EncounterData);
                        }
                        catch
                        {
                        }
                }

                Contacts = new ObservableCollection<QuestionBase>();
                foreach (var item in BackupContacts)
                {
                    //need to copy so raise property changes gets called - can't just copy the entire object
                    EncounterData ed = (EncounterData)Clone(item.EncounterData);
                    Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                        Patient = Patient, Question = Question
                    });
                }

                this.RaisePropertyChangedLambda(p => p.Contacts);
            }
            else
            {
                BackupContacts = new ObservableCollection<QuestionBase>();
                foreach (var item in Contacts)
                    BackupContacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter,
                        EncounterData = (EncounterData)Clone(item.EncounterData), Patient = Patient, Question = Question
                    });
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in Contacts)
            {
                item.EncounterData.ValidationErrors.Clear();

                if (!string.IsNullOrEmpty(item.EncounterData.TextData) ||
                    !string.IsNullOrEmpty(item.EncounterData.Text2Data))
                {
                    if (item.EncounterData.IsNew)
                    {
                        Encounter.EncounterData.Add(item.EncounterData);
                    }
                }
                else
                {
                    if (item.EncounterData.EntityState == EntityState.Modified)
                    {
                        Encounter.EncounterData.Remove(item.EncounterData);
                    }
                }
            }

            return AllValid;
        }

        #region PatientContact

        public bool IsEdit => true;
        private PatientContact _SelectedItem;

        public PatientContact SelectedItem
        {
            get { return _SelectedItem; }
            set
            {
                _SelectedItem = value;
                this.RaisePropertyChangedLambda(p => p.SelectedItem);
            }
        }

        public RelayCommand AddContactCommand { get; set; }
        public RelayCommand<FamilyRelationship> AddBehaviorCommand { get; set; }
        public RelayCommand<Guid?> PatientContactDetailsCommand { get; protected set; }

        public RelayCommand CancelPatientContactCommand { get; protected set; }
        public RelayCommand OKPatientContactCommand { get; protected set; }

        private void Setup_PatientContact()
        {
            AddContactCommand = new RelayCommand(() =>
            {
                SelectedItem = new PatientContact();
                SelectedItem.IsDynamicForm = true;
                SelectedItem.ContactGuid = Guid.NewGuid();
                int? TypeKey = CodeLookupCache.GetKeyFromCode("PATCONTACTADDRESS", "Patient");
                SelectedItem.ContactTypeKey = TypeKey == null ? 0 : (int)TypeKey;
                PopupDataTemplate = "PatientContactPopupDataTemplate";
                DynamicFormViewModel.PopupDataContext = this;
            });

            PatientContactDetailsCommand = new RelayCommand<Guid?>(patientContactGuid =>
            {
                if (patientContactGuid == null)
                {
                    return;
                }

                if (patientContactGuid == Guid.Empty)
                {
                    return;
                }

                if (Patient == null)
                {
                    return;
                }

                if (Patient.PatientContact == null)
                {
                    return;
                }

                PatientContact pc = Patient.PatientContact.FirstOrDefault(c => c.ContactGuid == patientContactGuid);
                if (pc == null)
                {
                    return;
                }

                PatientContactDetailsDialog cw = new PatientContactDetailsDialog(pc);
                cw.Show();
            });

            OKPatientContactCommand = new RelayCommand(() =>
            {
                if (SelectedItem == null)
                {
                    return;
                }

                SelectedItem.ValidationErrors.Clear();
                if (SelectedItem.Validate())
                {
                    Patient.PatientContact.Add(SelectedItem);
                    DynamicFormViewModel.PopupDataContext = null;
                    PopupDataTemplate = null;
                }
            });

            CancelPatientContactCommand = new RelayCommand(() =>
            {
                DynamicFormViewModel.PopupDataContext = null;
                PopupDataTemplate = null;
            });
        }

        #endregion
    }

    public class FamilyRelationshipFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            FamilyRelationship fr = new FamilyRelationship(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
                AddBehaviorCommand = new RelayCommand<FamilyRelationship>(familyRelationship =>
                {
                    EncounterData ed = new EncounterData
                    {
                        SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey,
                        QuestionKey = q.QuestionKey
                    };
                    familyRelationship.Contacts.Add(new QuestionBase(__FormSectionQuestionKey)
                    {
                        Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = ed,
                        Patient = vm.CurrentPatient, Question = q
                    });
                }),
            };
            fr.FamilyRelationshipSetup();

            if (fr.Encounter.IsNew && copyforward)
            {
                fr.CopyForwardLastInstance();
            }
            else
            {
                fr.Contacts = new ObservableCollection<QuestionBase>();
                foreach (var item in vm.CurrentEncounter.EncounterData.Where(x =>
                             x.EncounterKey == vm.CurrentEncounter.EncounterKey &&
                             x.SectionKey == formsection.Section.SectionKey &&
                             x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey))
                    fr.Contacts.Add(new QuestionBase(__FormSectionQuestionKey)
                    {
                        Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = item,
                        Patient = vm.CurrentPatient, Question = q
                    });

                //default with one and allow more to be added
                if (fr.Contacts.Any() == false)
                {
                    EncounterData ed = new EncounterData
                    {
                        SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey,
                        QuestionKey = q.QuestionKey
                    };
                    fr.Contacts.Add(new QuestionBase(__FormSectionQuestionKey)
                    {
                        Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = ed,
                        Patient = vm.CurrentPatient, Question = q
                    });
                }
            }

            return fr;
        }
    }
}