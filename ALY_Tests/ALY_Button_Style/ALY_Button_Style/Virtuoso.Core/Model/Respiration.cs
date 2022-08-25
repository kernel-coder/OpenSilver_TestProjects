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
    public class Respiration : QuestionUI
    {
        public ObservableCollection<EncounterResp> Readings { get; set; }
        public RelayCommand AddReadingCommand { get; set; }

        public Respiration(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            AddReadingCommand = new RelayCommand(() =>
            {
                int sequence = 1;
                if (Readings.Any())
                {
                    sequence = Readings.Max(p => p.Sequence) + 1;
                }

                Readings.Add(new EncounterResp { Sequence = sequence, Version = Encounter.GetVitalsVersion });
            });
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in Readings)
            {
                item.ValidationErrors.Clear();

                if (item.RespRate > 0 || (Required && Encounter.FullValidation))
                {
                    if (ClientValidate(item))
                    {
                        if (item.RespRate < TenantSettingsCache.Current.TenantSetting.RespControlLow ||
                            item.RespRate > TenantSettingsCache.Current.TenantSetting.RespControlHigh)
                        {
                            item.ValidationErrors.Add(new ValidationResult(
                                "Warning - Respiration entry not within defined range", new[] { "RespRate" }));
                        }

                        if (AllValid && item.IsNew)
                        {
                            Encounter.EncounterResp.Add(item);
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
                        Encounter.EncounterResp.Remove(item);
                    }
                }
            }

            return AllValid;
        }

        private bool ClientValidate(EncounterResp item)
        {
            bool allValid = item.Validate();
            if (ValidateVitalsReadingDateTime(item) == false)
            {
                allValid = false;
            }

            return allValid;
        }
    }

    public class RespirationFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            Respiration r = new Respiration(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                Readings = new ObservableCollection<EncounterResp>()
            };

            foreach (var item in vm.CurrentEncounter.EncounterResp
                         .Where(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey)
                         .OrderBy(x => x.Sequence)) r.Readings.Add(item);

            //default with one and allow more to be added
            if (r.Readings.Any() == false)
            {
                r.Readings.Add(new EncounterResp { Sequence = 1, Version = vm.CurrentEncounter.GetVitalsVersion });
            }

            return r;
        }
    }
}