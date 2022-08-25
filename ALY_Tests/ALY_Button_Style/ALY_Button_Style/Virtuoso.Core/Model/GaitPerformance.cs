#region Usings

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class GaitPerformance : QuestionUI
    {
        public GaitPerformance(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private EncounterGait _EncounterGait;

        public EncounterGait EncounterGait
        {
            get { return _EncounterGait; }
            set
            {
                _EncounterGait = value;
                _EncounterGait.CalcGaitVelocityAndCadence();
                this.RaisePropertyChangedLambda(p => p.EncounterGait);
            }
        }

        public EncounterGait BackupEncounterGait { get; set; }
        public ObservableCollection<QuestionBase> Observations { get; set; }
        public RelayCommand<GaitPerformance> AddObservationCommand { get; set; }

        public override void ClearEntity()
        {
            EncounterGait.Distance = null;
            EncounterGait.DistanceScale = String.Empty;
            EncounterGait.FuncDeficit = String.Empty;
            EncounterGait.NumberofSteps = null;
            EncounterGait.TimetoTravel = null;
        }

        void CopyProperties(EncounterGait source)
        {
            EncounterGait.Distance = source.Distance;
            EncounterGait.DistanceScale = source.DistanceScale;
            EncounterGait.FuncDeficit = source.FuncDeficit;
            EncounterGait.NumberofSteps = source.NumberofSteps;
            EncounterGait.TimetoTravel = source.TimetoTravel;
        }

        public override bool CopyForwardLastInstance()
        {
            foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                EncounterGait previous = item.EncounterGait.FirstOrDefault(d => d.QuestionKey == Question.QuestionKey);
                if (previous != null)
                {
                    CopyProperties(previous);
                    return true;
                    ;
                }
            }

            return false;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
            EncounterGait previous = e.EncounterGait.FirstOrDefault(p => 
                p.QuestionKey == Question.QuestionKey 
                && p.QuestionGroupKey == QuestionGroupKey 
                && p.Section.Label == Section.Label);
            if (previous != null)
            {
                CopyProperties(previous);
            }
        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                var previous = (EncounterGait)Clone(BackupEncounterGait);
                //need to copy so raise property changes gets called - can't just copy the entire object
                CopyProperties(previous);
            }
            else
            {
                BackupEncounterGait = (EncounterGait)Clone(EncounterGait);
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            if (Hidden)
            {
                ClearEntity();
            }

            bool AllValid = true;
            EncounterGait.ValidationErrors.Clear();

            if (EncounterGait.Distance.HasValue || !string.IsNullOrEmpty(EncounterGait.DistanceScale) ||
                !string.IsNullOrEmpty(EncounterGait.FuncDeficit) || EncounterGait.NumberofSteps.HasValue ||
                EncounterGait.TimetoTravel.HasValue ||
                (Required && Encounter.FullValidation && !Hidden))
            {
                if (!EncounterGait.Distance.HasValue || string.IsNullOrEmpty(EncounterGait.DistanceScale) ||
                    !EncounterGait.NumberofSteps.HasValue || !EncounterGait.TimetoTravel.HasValue)
                {
                    AllValid = false;
                    EncounterGait.ValidationErrors.Add(new ValidationResult(
                        "Distance, Scale, Number of Steps, and Time to Travel must all be valued",
                        new[] { "Distance", "DistanceScale", "NumberofSteps", "TimetoTravel" }));
                }
                else if (EncounterGait.IsNew)
                {
                    Encounter.EncounterGait.Add(EncounterGait);
                }
            }
            else
            {
                if (EncounterGait.EntityState == EntityState.Modified)
                {
                    Encounter.EncounterGait.Remove(EncounterGait);
                    EncounterGait = new EncounterGait
                    {
                        SectionKey = EncounterGait.SectionKey, QuestionGroupKey = EncounterGait.QuestionGroupKey,
                        QuestionKey = EncounterGait.QuestionKey
                    };
                }
            }

            foreach (var item in Observations)
            {
                item.EncounterData.ValidationErrors.Clear();

                if (item.EncounterData.ExternalKey == null && item.EncounterData.AdmissionQuestionKey == null &&
                    item.Label != null)
                {
                    int sequence = 1;
                    if (Admission.AdmissionQuestion.Any(p => p.SectionKey == Section.SectionKey && p.QuestionGroupKey == QuestionGroupKey))
                    {
                        sequence = Admission.AdmissionQuestion.Where(p =>
                                p.SectionKey == Section.SectionKey && p.QuestionGroupKey == QuestionGroupKey)
                            .Max(p => p.Sequence) + 1;
                    }

                    AdmissionQuestion aq = new AdmissionQuestion
                    {
                        Label = item.Label,
                        SectionKey = Section.SectionKey,
                        QuestionGroupKey = QuestionGroupKey,
                        DataTemplate = Question.DataTemplate,
                        Sequence = sequence
                    };
                    Patient.AdmissionQuestion.Add(aq);
                    Admission.AdmissionQuestion.Add(aq);
                    Encounter.AdmissionQuestion.Add(aq);

                    if (!string.IsNullOrEmpty(item.EncounterData.TextData) || item.EncounterData.IntData.HasValue ||
                        item.EncounterData.BoolData.HasValue || item.EncounterData.DateTimeData.HasValue)
                    {
                        aq.EncounterData.Add(item.EncounterData);
                        Encounter.EncounterData.Add(item.EncounterData);
                    }
                }
                else if (!string.IsNullOrEmpty(item.EncounterData.TextData) || item.EncounterData.IntData.HasValue ||
                         item.EncounterData.BoolData.HasValue || item.EncounterData.DateTimeData.HasValue)
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
    }

    public class GaitPerformanceFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterGait eg = vm.CurrentEncounter.EncounterGait.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (eg == null)
            {
                eg = new EncounterGait
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            GaitPerformance gp = new GaitPerformance(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterGait = eg,
                OasisManager = vm.CurrentOasisManager,
                AddObservationCommand = new RelayCommand<GaitPerformance>(gait =>
                {
                    EncounterData ed = new EncounterData
                    {
                        SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey,
                        QuestionKey = q.QuestionKey
                    };
                    QuestionBase observation = new QuestionBase(__FormSectionQuestionKey)
                        { Admission = vm.CurrentAdmission };
                    observation.Encounter = vm.CurrentEncounter;
                    observation.ProtectedOverrideRunTime = false;
                    observation.EncounterData = ed;
                    gait.Observations.Add(observation);
                }),
            };

            gp.Observations = new ObservableCollection<QuestionBase>();
            foreach (var item in vm.CurrentPatient.AdmissionQuestion
                         .Where(aq => aq.SectionKey == formsection.Section.SectionKey && aq.QuestionGroupKey == qgkey)
                         .OrderBy(aq => aq.Sequence))
            {
                EncounterData ed = vm.CurrentEncounter.EncounterData.Where(x =>
                    x.EncounterKey == vm.CurrentEncounter.EncounterKey &&
                    x.SectionKey == formsection.Section.SectionKey &&
                    x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey &&
                    x.AdmissionQuestionKey == item.AdmissionQuestionKey).FirstOrDefault();
                if (ed == null)
                {
                    ed = new EncounterData
                    {
                        SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey,
                        QuestionKey = q.QuestionKey, AdmissionQuestionKey = item.AdmissionQuestionKey
                    };
                }

                QuestionBase observation = new QuestionBase(__FormSectionQuestionKey)
                    { Admission = vm.CurrentAdmission };
                observation.Encounter = vm.CurrentEncounter;
                observation.Label = item.Label;
                observation.ProtectedOverrideRunTime = true;
                observation.EncounterData = ed;
                gp.Observations.Add(observation);
            }

            return gp;
        }
    }
}