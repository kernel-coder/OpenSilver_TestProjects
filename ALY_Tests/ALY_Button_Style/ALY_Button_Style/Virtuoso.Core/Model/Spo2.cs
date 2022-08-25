#region Usings

using System.Collections.ObjectModel;
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
    public class Spo2 : QuestionUI
    {
        public ObservableCollection<EncounterSpo2> Readings { get; set; }
        public RelayCommand AddReadingCommand { get; set; }

        public Spo2(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            AddReadingCommand = new RelayCommand(() =>
            {
                int sequence = 1;
                if (Readings.Any())
                {
                    sequence = Readings.Max(p => p.Sequence) + 1;
                }

                Readings.Add(new EncounterSpo2 { Sequence = sequence, Version = Encounter.GetVitalsVersion });
            });
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in Readings)
            {
                item.ValidationErrors.Clear();

                if (item.Spo2Percent > 0 || (Required && Encounter.FullValidation))
                {
                    if (ClientValidate(item))
                    {
                        if (item.IsNew)
                        {
                            Encounter.EncounterSpo2.Add(item);
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
                        Encounter.EncounterSpo2.Remove(item);
                    }
                }
            }

            return AllValid;
        }

        private bool ClientValidate(EncounterSpo2 item)
        {
            bool allValid = item.Validate();
            if (ValidateVitalsReadingDateTime(item) == false)
            {
                allValid = false;
            }

            return allValid;
        }
    }

    public class Spo2Factory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            Spo2 s = new Spo2(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                Readings = new ObservableCollection<EncounterSpo2>()
            };

            foreach (var item in vm.CurrentEncounter.EncounterSpo2
                         .Where(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey)
                         .OrderBy(x => x.Sequence))
            {
                s.Readings.Add(item);
            }

            //default with one and allow more to be added
            if (s.Readings.Any() == false)
            {
                s.Readings.Add(new EncounterSpo2 { Sequence = 1, Version = vm.CurrentEncounter.GetVitalsVersion });
            }

            return s;
        }
    }
}