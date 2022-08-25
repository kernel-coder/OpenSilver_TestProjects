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
    public class Temperature : QuestionUI
    {
        public ObservableCollection<EncounterTemp> Readings { get; set; }
        public RelayCommand AddReadingCommand { get; set; }

        public Temperature(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            AddReadingCommand = new RelayCommand(() =>
            {
                int sequence = 1;
                if (Readings.Any())
                {
                    sequence = Readings.Max(p => p.Sequence) + 1;
                }

                Readings.Add(new EncounterTemp { Sequence = sequence, Version = Encounter.GetVitalsVersion });
            });
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in Readings)
            {
                item.ValidationErrors.Clear();

                if (item.Temp.HasValue || (Required && Encounter.FullValidation))
                {
                    if (ClientValidate(item))
                    {
                        if ((item.Temp.HasValue) && (item.Temp > 0) &&
                            (string.IsNullOrEmpty(item.TempScale) || item.TempMode < 1))
                        {
                            item.ValidationErrors.Add(new ValidationResult(
                                "You must specify a full temperature reading including temperature, scale and mode",
                                new[] { "Temp" }));
                        }

                        if (AllValid && item.IsNew)
                        {
                            Encounter.EncounterTemp.Add(item);
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
                        Encounter.EncounterTemp.Remove(item);
                    }
                }
            }

            return AllValid;
        }

        private bool ClientValidate(EncounterTemp item)
        {
            bool allValid = item.Validate();
            if (ValidateVitalsReadingDateTime(item) == false)
            {
                allValid = false;
            }

            return allValid;
        }
    }

    public class TemperatureFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            Temperature r = new Temperature(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                Readings = new ObservableCollection<EncounterTemp>()
            };

            foreach (var item in vm.CurrentEncounter.EncounterTemp
                         .Where(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey)
                         .OrderBy(x => x.Sequence))
            {
                r.Readings.Add(item);
            }

            //default with one and allow more to be added
            if (r.Readings.Any() == false)
            {
                r.Readings.Add(new EncounterTemp { Sequence = 1, Version = vm.CurrentEncounter.GetVitalsVersion });
            }

            return r;
        }
    }
}