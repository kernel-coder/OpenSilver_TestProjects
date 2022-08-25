#region Usings

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class Pulse : QuestionUI
    {
        public ObservableCollection<EncounterPulse> Readings { get; set; }
        public RelayCommand AddReadingCommand { get; set; }

        public Pulse(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            AddReadingCommand = new RelayCommand(() =>
            {
                int sequence = 1;
                if (Readings != null && Readings.Any())
                {
                    sequence = Readings.Max(p => p.Sequence) + 1;
                }
                if (Readings != null)
                {
                    Readings.Add(new EncounterPulse { Sequence = sequence, Version = Encounter.GetVitalsVersion });
                }
            });
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in Readings)
            {
                item.ValidationErrors.Clear();

                if (item.PulseRate > 0 || (Required && Encounter.FullValidation))
                {
                    if (ClientValidate(item))
                    {
                        if (item.PulseRate < TenantSettingsCache.Current.TenantSetting.PulseControlLow ||
                            item.PulseRate > TenantSettingsCache.Current.TenantSetting.PulseControlHigh)
                        {
                            item.ValidationErrors.Add(new ValidationResult(
                                "Warning - Pulse entry not within defined range", new[] { "PulseRate" }));
                        }

                        if (AllValid && item.IsNew)
                        {
                            Encounter.EncounterPulse.Add(item);
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
                        Encounter.EncounterPulse.Remove(item);
                    }
                }
            }

            return AllValid;
        }

        private bool ClientValidate(EncounterPulse item)
        {
            bool allValid = item.Validate();
            if (ValidateVitalsReadingDateTime(item) == false)
            {
                allValid = false;
            }

            return allValid;
        }
    }

    public class PulseFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            Pulse pulse = new Pulse(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                Readings = new ObservableCollection<EncounterPulse>()
            };
            foreach (var item in vm.CurrentEncounter.EncounterPulse
                         .Where(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey)
                         .OrderBy(x => x.Sequence)) pulse.Readings.Add(item);

            //default with one and allow more to be added
            if (pulse.Readings.Any() == false)
            {
                pulse.Readings.Add(new EncounterPulse { Sequence = 1, Version = vm.CurrentEncounter.GetVitalsVersion });
            }

            return pulse;
        }
    }
}