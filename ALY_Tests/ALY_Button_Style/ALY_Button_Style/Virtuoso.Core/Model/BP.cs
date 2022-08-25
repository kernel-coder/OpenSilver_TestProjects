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
    public class BP : QuestionUI
    {
        public ObservableCollection<EncounterBP> Readings { get; set; }
        public RelayCommand AddReadingCommand { get; set; }

        public BP(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            AddReadingCommand = new RelayCommand(() =>
            {
                int sequence = 1;
                if (Readings.Any())
                {
                    sequence = Readings.Max(p => p.Sequence) + 1;
                }

                Readings.Add(new EncounterBP { Sequence = sequence, Version = Encounter.GetVitalsVersion });
            });
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in Readings)
            {
                item.ValidationErrors.Clear();

                if (item.BPSystolic > 0 || item.BPDiastolic > 0 || (Required && Encounter.FullValidation))
                {
                    if (ClientValidate(item))
                    {
                        if (item.BPSystolic < TenantSettingsCache.Current.TenantSetting.SystolicControlLow ||
                            item.BPSystolic > TenantSettingsCache.Current.TenantSetting.SystolicControlHigh)
                        {
                            item.ValidationErrors.Add(new ValidationResult(
                                "Warning - BP Systolic entry not within defined range", new[] { "BPSystolic" }));
                        }

                        if (item.BPDiastolic < TenantSettingsCache.Current.TenantSetting.DiastolicControlLow ||
                            item.BPDiastolic > TenantSettingsCache.Current.TenantSetting.DiastolicControlHigh)
                        {
                            item.ValidationErrors.Add(new ValidationResult(
                                "Warning - BP Diastolic entry not within defined range", new[] { "BPDiastolic" }));
                        }

                        if (AllValid && item.IsNew)
                        {
                            Encounter.EncounterBP.Add(item);
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
                        Encounter.EncounterBP.Remove(item);
                    }
                }
            }

            return AllValid;
        }

        private bool ClientValidate(EncounterBP item)
        {
            bool allValid = item.Validate();
            if (ValidateVitalsReadingDateTime(item) == false)
            {
                allValid = false;
            }

            return allValid;
        }
    }

    public class BPFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            BP b = new BP(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
            };

            b.Readings = new ObservableCollection<EncounterBP>();

            var bps = vm.CurrentEncounter.EncounterBP.Where(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey)
                .OrderBy(x => x.Sequence).ToList();
            foreach (var item in bps) b.Readings.Add(item);

            //default with one and allow more to be added
            if (b.Readings.Any() == false)
            {
                b.Readings.Add(new EncounterBP { Sequence = 1, Version = vm.CurrentEncounter.GetVitalsVersion });
            }

            return b;
        }
    }
}