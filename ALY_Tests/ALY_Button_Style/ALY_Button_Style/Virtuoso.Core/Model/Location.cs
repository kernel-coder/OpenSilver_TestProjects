#region Usings

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class Location : QuestionUI
    {
        public Location(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            ProcessGoals = new RelayCommand(() =>
            {
                string response = string.Empty;
                string subresponse = string.Empty;
                string reason = string.Empty;
                bool remove = false;
                int? keytouse = null;

                foreach (var qb in Locations)
                    if (Question.DataTemplate.Equals("Surface"))
                    {
                        if (qb.EncounterData.Int2Data > 0 && !string.IsNullOrEmpty(qb.EncounterData.TextData) &&
                            !string.IsNullOrEmpty(qb.EncounterData.FuncDeficit))
                        {
                            response = qb.EncounterData.Int2Data.ToString();
                            subresponse = qb.EncounterData.TextData;
                            reason = qb.EncounterData.FuncDeficit;
                            keytouse = qb.EncounterData.Int2Data;
                        }
                        else
                        {
                            remove = true;
                        }

                        GoalManager.UpdateGoals(this, response, subresponse, reason, remove, keytouse);
                    }
            });
        }

        public override void Cleanup()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (Locations != null)
                {
                    Locations.ForEach(l => l.Cleanup());
                    if (Locations != null)
                    {
                        Locations.Clear();
                    }
                }

                if (BackupLocations != null)
                {
                    BackupLocations.ForEach(b => b.Cleanup());
                    if (BackupLocations != null)
                    {
                        BackupLocations.Clear();
                    }
                }
            });
            base.Cleanup();
        }

        public ObservableCollection<QuestionBase> Locations { get; set; }
        public ObservableCollection<QuestionBase> BackupLocations { get; set; }
        public RelayCommand<Location> AddLocationCommand { get; set; }

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
            Locations = new ObservableCollection<QuestionBase>();

            foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                foreach (var ed in item.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey))
                    if (ed != null)
                    {
                        Locations.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                        {
                            Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                            Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                        });
                    }

                break;
            }

            //default with one and allow more to be added
            if (Locations.Any() == false)
            {
                EncounterData ed = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey
                };
                Locations.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = ed, Question = Question,
                    ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                });
            }

            return true;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
            Locations = new ObservableCollection<QuestionBase>();
            foreach (var item in e.EncounterData.Where(p =>
                         p.QuestionKey == Question.QuestionKey && p.QuestionGroupKey == QuestionGroupKey &&
                         p.Section.Label == Section.Label))
                if (item != null)
                {
                    Locations.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(item),
                        Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
                }

            //default with one and allow more to be added
            if (Locations.Any() == false)
            {
                EncounterData ed = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey
                };
                Locations.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = ed, Question = Question,
                    ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                });
            }
        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if (Locations != null)
                {
                    foreach (var item in Locations)
                        try
                        {
                            ((IPatientService)DynamicFormViewModel.FormModel).Remove(item.EncounterData);
                        }
                        catch
                        {
                        }
                }

                Locations = new ObservableCollection<QuestionBase>();
                foreach (var item in BackupLocations)
                {
                    //need to copy so raise property changes gets called - can't just copy the entire object
                    EncounterData ed = (EncounterData)Clone(item.EncounterData);
                    Locations.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                        Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
                }

                this.RaisePropertyChangedLambda(p => p.Locations);
            }
            else
            {
                BackupLocations = new ObservableCollection<QuestionBase>();
                foreach (var item in Locations)
                    BackupLocations.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter,
                        EncounterData = (EncounterData)Clone(item.EncounterData), Question = Question,
                        ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in Locations)
            {
                item.EncounterData.ValidationErrors.Clear();

                if (!string.IsNullOrEmpty(item.EncounterData.TextData) || item.EncounterData.IntData.HasValue ||
                    !string.IsNullOrEmpty(item.EncounterData.FuncDeficit) || item.EncounterData.Int2Data.HasValue)
                {
                    if (Question.DataTemplate.StartsWith("OTLevelofAssist"))
                    {
                        if (!item.EncounterData.Int2Data.HasValue ||
                            CodeLookupCache.GetCodeFromKey(Question.SubLookupType, item.EncounterData.Int2Data.Value) ==
                            "Independent")
                        {
                            item.EncounterData.TextData = string.Empty;
                            item.EncounterData.Text2Data = string.Empty;
                            item.EncounterData.Text3Data = string.Empty;
                            item.EncounterData.Text4Data = string.Empty;
                            item.EncounterData.GuidData = null;
                        }

                        if ((item.EncounterData.IntData.HasValue && !item.EncounterData.Int2Data.HasValue) ||
                            (!item.EncounterData.IntData.HasValue && item.EncounterData.Int2Data.HasValue))
                        {
                            item.EncounterData.ValidationErrors.Add(new ValidationResult(
                                (string.Format("Ability and Level of Assist must both be valued", Label)),
                                new[] { "IntData", "Int2Data" }));
                            AllValid = false;
                        }
                        else if (CodeLookupCache.GetCodeFromKey(Question.SubLookupType,
                                     item.EncounterData.Int2Data.Value) != "Independent")
                        {
                            string location = CodeLookupCache.GetCodeDescriptionFromKey(item.Question.LookupType,
                                item.EncounterData.IntData.Value);

                            if (string.IsNullOrEmpty(item.EncounterData.TextData))
                            {
                                item.EncounterData.ValidationErrors.Add(new ValidationResult(
                                    (string.Format("{0}/{1} requires Continuous or Intermittent to be valued", Label,
                                        location)), new[] { "TextData" }));
                                AllValid = false;
                            }

                            if (string.IsNullOrEmpty(item.EncounterData.Text2Data))
                            {
                                item.EncounterData.ValidationErrors.Add(new ValidationResult(
                                    (string.Format("{0}/{1} requires Reason Assistance Needed to be valued", Label,
                                        location)), new[] { "Text2Data" }));
                                AllValid = false;
                            }

                            if (string.IsNullOrEmpty(item.EncounterData.Text3Data))
                            {
                                item.EncounterData.ValidationErrors.Add(new ValidationResult(
                                    (string.Format("{0}/{1} requires Explain Assistance Needed to be valued", Label,
                                        location)), new[] { "Text3Data" }));
                                AllValid = false;
                            }
                        }
                    }
                    else if (Question.LookupType.Equals("Sensation"))
                    {
                        string location = "Unknown";
                        if (item.EncounterData.Int2Data.HasValue)
                        {
                            location = CodeLookupCache.GetCodeDescriptionFromKey(item.Question.SubLookupType,
                                item.EncounterData.Int2Data.Value);
                        }

                        if (item.EncounterData.Int2Data.HasValue && !item.EncounterData.IntData.HasValue)
                        {
                            item.EncounterData.ValidationErrors.Add(new ValidationResult(
                                (string.Format("{0}/{1} requires a Value", Label, location)), new[] { "IntData" }));
                            AllValid = false;
                        }
                        else
                        {
                            string sensation = "";
                            if (item.EncounterData.IntData.HasValue)
                            {
                                sensation = CodeLookupCache.GetCodeDescriptionFromKey(item.Question.LookupType,
                                    item.EncounterData.IntData.Value);
                            }

                            if (item.EncounterData.Int2Data.HasValue)
                            {
                                if ((sensation.Equals("Impaired") || sensation.Equals("Unable to test")) &&
                                    string.IsNullOrEmpty(item.EncounterData.TextData))
                                {
                                    item.EncounterData.ValidationErrors.Add(new ValidationResult(
                                        (string.Format("{0}/{1} requires a Comment", Label, location)),
                                        new[] { "TextData" }));
                                    AllValid = false;
                                }
                            }
                            else
                            {
                                if ((sensation.Equals("Impaired") || sensation.Equals("Unable to test")) &&
                                    string.IsNullOrEmpty(item.EncounterData.TextData))
                                {
                                    item.EncounterData.ValidationErrors.Add(new ValidationResult(
                                        (string.Format("{0} requires a Comment", Label)), new[] { "TextData" }));
                                    AllValid = false;
                                }
                            }
                        }
                    }

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
    }

    public class LocationFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            Location l = new Location(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
                AddLocationCommand = new RelayCommand<Location>(location =>
                {
                    EncounterData ed = new EncounterData
                    {
                        SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey,
                        QuestionKey = q.QuestionKey
                    };
                    location.Locations.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = ed,
                        Question = q, ProcessGoals = new RelayCommand(() => { location.ProcessGoals.Execute(null); })
                    });
                }),
            };

            l.Locations = new ObservableCollection<QuestionBase>();
            foreach (var item in vm.CurrentEncounter.EncounterData.Where(x =>
                         x.EncounterKey == vm.CurrentEncounter.EncounterKey &&
                         x.SectionKey == formsection.Section.SectionKey &&
                         x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey))
                l.Locations.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = item,
                    Question = q, ProcessGoals = new RelayCommand(() => { l.ProcessGoals.Execute(null); })
                });

            //default with one and allow more to be added
            if (l.Locations.Any() == false)
            {
                EncounterData ed = new EncounterData
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
                l.Locations.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = ed, Question = q,
                    ProcessGoals = new RelayCommand(() => { l.ProcessGoals.Execute(null); })
                });
            }

            return l;
        }
    }
}