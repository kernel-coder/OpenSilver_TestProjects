#region Usings

using System.ComponentModel.DataAnnotations;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class CBG : QuestionUI
    {
        public CBG(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private EncounterCBG _EncounterCBG;

        public EncounterCBG EncounterCBG
        {
            get { return _EncounterCBG; }
            set
            {
                _EncounterCBG = value;
                this.RaisePropertyChangedLambda(p => p.EncounterCBG);
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;

            EncounterCBG.ValidationErrors.Clear();

            if (EncounterCBG.CBG > 0 || (Required && Encounter.FullValidation))
            {
                if (EncounterCBG.Validate())
                {
                    int bloodGlucoseLow =
                        TenantSettingsCache.Current.TenantSetting.BloodGlucoseLow.GetValueOrDefault(70);
                    int bloodGlucoseHigh =
                        TenantSettingsCache.Current.TenantSetting.BloodGlucoseHigh.GetValueOrDefault(200);

                    if (EncounterCBG.CBG < bloodGlucoseLow || EncounterCBG.CBG > bloodGlucoseHigh)
                    {
                        EncounterCBG.ValidationErrors.Add(
                            new ValidationResult("Warning - Blood glocose is not within defined range",
                                new[] { "CBG" }));
                    }

                    if (EncounterCBG.TestType == "Postprandial" && !EncounterCBG.HoursPP.HasValue)
                    {
                        EncounterCBG.ValidationErrors.Add(
                            new ValidationResult("Warning - Hours PP is required when test type is Postprandial",
                                new[] { "CBG" }));
                    }

                    if (AllValid && EncounterCBG.IsNew)
                    {
                        Encounter.EncounterCBG.Add(EncounterCBG);
                    }
                }
                else
                {
                    AllValid = false;
                }
            }
            else
            {
                if (EncounterCBG.EntityState == EntityState.Modified)
                {
                    Encounter.EncounterCBG.Remove(EncounterCBG);
                    EncounterCBG = new EncounterCBG();
                }
            }

            return AllValid;
        }
    }

    public class CBGFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterCBG cbg = vm.CurrentEncounter.EncounterCBG
                .Where(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey).FirstOrDefault();
            if (cbg == null)
            {
                cbg = new EncounterCBG();
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            return new CBG(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterCBG = cbg,
                OasisManager = vm.CurrentOasisManager,
            };
        }
    }
}