#region Usings

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class VisitFrequency : QuestionUI
    {
        // OLD VISIT FREQUENCY QUESTION
        public VisitFrequency(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public ObservableCollection<EncounterVisitFrequency> VisitFrequencies { get; set; }
        public RelayCommand<VisitFrequency> AddVisitFrequencyCommand { get; set; }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in VisitFrequencies)
            {
                item.ValidationErrors.Clear();

                if (string.IsNullOrWhiteSpace(item.Frequency))
                {
                    item.Frequency = null;
                }

                if (string.IsNullOrWhiteSpace(item.Duration))
                {
                    item.Duration = null;
                }

                if (string.IsNullOrWhiteSpace(item.Purpose))
                {
                    item.Purpose = null;
                }

                // a complete visit frequency consists of a  Frequency, Duration and Purpose
                if ((item.Frequency != null) || (item.Duration != null) || (item.Purpose != null) ||
                    (Required && Encounter.FullValidation))
                {
                    if (item.Validate())
                    {
                        if (item.IsNew)
                        {
                            Encounter.EncounterVisitFrequency.Add(item);
                        }
                    }
                    else
                    {
                        AllValid = false;
                    }
                }
                else
                {
                    if (item.EntityState == EntityState.Modified)
                    {
                        Encounter.EncounterVisitFrequency.Remove(item);
                    }
                }
            }

            foreach (var item in VisitFrequencies.Reverse())
                if ((item.Frequency == null) && (item.Duration == null) && (item.Purpose == null))
                {
                    try
                    {
                        VisitFrequencies.Remove(item);
                    }
                    catch
                    {
                    }
                }

            if (VisitFrequencies.Any() == false 
                && (DynamicFormViewModel.CurrentForm.IsEval || DynamicFormViewModel.CurrentForm.IsResumption))
            {
                EncounterVisitFrequency evf = new EncounterVisitFrequency();
                VisitFrequencies.Add(evf);
                if (Encounter.FullValidation)
                {
                    evf.ValidationErrors.Add(new ValidationResult("At least one visit frequency is required",
                        new[] { "Frequency", "Duration", "Purpose" }));
                    AllValid = false;
                }
            }

            return AllValid;
        }
    }

    public class VisitFrequencyFactory
    {
        // OLD VISIT FREQUENCY QUESTION FACTORY
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            VisitFrequency s = new VisitFrequency(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
                AddVisitFrequencyCommand = new RelayCommand<VisitFrequency>(vso =>
                {
                    vso.VisitFrequencies.Add(new EncounterVisitFrequency());
                }),
            };

            s.VisitFrequencies = new ObservableCollection<EncounterVisitFrequency>();
            if (s.Encounter.IsNew && copyforward)
            {
                foreach (var item in s.Admission.Encounter.OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                    if (item.EncounterVisitFrequency.Any())
                    {
                        foreach (var previous in item.EncounterVisitFrequency)
                            if (previous != null)
                            {
                                EncounterVisitFrequency evf = new EncounterVisitFrequency();
                                evf.Frequency = previous.Frequency;
                                evf.Duration = previous.Duration;
                                evf.Purpose = previous.Purpose;
                                s.VisitFrequencies.Add(evf);
                            }

                        break;
                    }
            }
            else
            {
                foreach (var item in vm.CurrentEncounter.EncounterVisitFrequency.Where(x =>
                             x.EncounterKey == vm.CurrentEncounter.EncounterKey)) s.VisitFrequencies.Add(item);
            }

            //default with one and allow more to be added
            if (s.VisitFrequencies.Any() == false)
            {
                s.VisitFrequencies.Add(new EncounterVisitFrequency());
            }

            return s;
        }
    }
}