#region Usings

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.View;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class OtherVaccinesImmunizations : QuestionBase
    {
        public ObservableCollection<PatientImmunization> Immunizations { get; set; }
        public RelayCommand AddImmunizationCommand { get; set; }
        public RelayCommand VaccinesImmunizationsHistoryCommand { get; set; }

        public OtherVaccinesImmunizations(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            AddImmunizationCommand = new RelayCommand(() =>
            {
                Immunizations.Add(new PatientImmunization
                {
                    PatientKey = Patient.PatientKey, AddedFromEncounterKey = Encounter.EncounterKey,
                    TenantID = Patient.TenantID
                });
            });
            VaccinesImmunizationsHistoryCommand = new RelayCommand(() => { VaccinesImmunizationsHistory(); });
        }

        private void VaccinesImmunizationsHistory()
        {
            VaccinesImmunizationsHistory vih =
                new VaccinesImmunizationsHistory(Patient, ((Encounter == null) ? 0 : Encounter.EncounterKey));
            vih.Show();
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in Immunizations)
                if (item.Immunization.HasValue)
                {
                    item.ValidationErrors.Clear();

                    if (item.ImmunizedBy.HasValue)
                    {
                        var t = CodeLookupCache.GetCodeDescriptionFromKey(item.ImmunizedBy);
                        if ((t == "Vaccine provided by HHA" || t == "Vaccine provided by another Provider") &&
                            !item.DateReceived.HasValue)
                        {
                            AllValid = false;
                            item.ValidationErrors.Add(new ValidationResult(
                                "Date Received is required when an immunization was provided.",
                                new[] { "DateReceived" }));
                        }
                        else if (t == "Offered Vaccine by HHA" && item.DateReceived.HasValue)
                        {
                            AllValid = false;
                            item.ValidationErrors.Add(new ValidationResult(
                                "Date Received cannot have value when an immunization was not provided.",
                                new[] { "DateReceived" }));
                        }
                        else if (t == "Offered Vaccine by HHA" &&
                                 (!item.ReasonForDeclining.HasValue && !item.Contraindications.HasValue))
                        {
                            AllValid = false;
                            item.ValidationErrors.Add(new ValidationResult(
                                "A Reason for Declining or a Contraindication is required when a vaccine has been offered but not provided.",
                                new[] { "ReasonForDeclining", "Contraindications" }));
                        }
                    }
                    else
                    {
                        AllValid = false;
                        item.ValidationErrors.Add(new ValidationResult("Offered or Provided by is required.",
                            new[] { "ImmunizedBy" }));
                    }

                    if (item.DateReceived.HasValue && item.DateReceived > DateTime.Now.Date)
                    {
                        AllValid = false;
                        item.ValidationErrors.Add(new ValidationResult("Date Received cannot be in the future.",
                            new[] { "DateReceived" }));
                    }

                    if (item.ReasonForDeclining.HasValue && string.IsNullOrEmpty(item.DecliningReasonComment))
                    {
                        var c = CodeLookupCache.GetCodeFromKey(item.ReasonForDeclining);
                        if (c == "DecAdd")
                        {
                            AllValid = false;
                            item.ValidationErrors.Add(new ValidationResult("Declining Reason Comment is required.",
                                new[] { "DecliningReasonComment" }));
                        }
                    }

                    if ((item.ValidationErrors.Any() == false) && (item.IsNew) &&
                        (Patient.PatientImmunization.Contains(item) == false))
                    {
                        Encounter.PatientImmunization.Add(item);
                        Patient.PatientImmunization.Add(item);
                    }
                }
                else if ((item.Immunization.HasValue == false) &&
                         ((item.IsNew == false) || Patient.PatientImmunization.Contains(item)))
                {
                    // once it has been saved - its required to be complete - (until/unless we add remove/delete functiopn)
                    AllValid = false;
                    item.ValidationErrors.Add(new ValidationResult("Vaccine/Immunization is required.",
                        new[] { "Immunization" }));
                }

            if ((AllValid) && Patient.IsImmunizationZosterRequiredAndNotOnFile && Encounter.FullValidation)
            {
                PatientImmunization pi = Immunizations.Where(i => i.Immunization.HasValue == false).FirstOrDefault();
                if (pi == null)
                {
                    pi = new PatientImmunization
                    {
                        PatientKey = Patient.PatientKey, AddedFromEncounterKey = Encounter.EncounterKey,
                        TenantID = Patient.TenantID
                    };
                    Immunizations.Add(pi);
                }

                AllValid = false;
                pi.ValidationErrors.Add(new ValidationResult(
                    "A Herpes Zoster (Shingles) vaccination record is required for patients 60 and older.",
                    new[] { "Immunization" }));
            }

            return AllValid;
        }
    }

    public class OtherVaccinesImmunizationsFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OtherVaccinesImmunizations w = new OtherVaccinesImmunizations(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                //FormModel = m,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Admission = vm.CurrentAdmission,
                Encounter = vm.CurrentEncounter,
                OasisManager = vm.CurrentOasisManager,
            };

            w.Immunizations = new ObservableCollection<PatientImmunization>();

            foreach (var item in vm.CurrentPatient.PatientImmunization
                         .Where(a => a.AddedFromEncounterKey == vm.CurrentEncounter.EncounterKey)
                         .ToList()) w.Immunizations.Add(item);

            if (w.Immunizations.Any() == false)
            {
                w.Immunizations.Add(new PatientImmunization
                {
                    PatientKey = vm.CurrentPatient.PatientKey, AddedFromEncounterKey = vm.CurrentEncounter.EncounterKey,
                    TenantID = vm.CurrentPatient.TenantID
                });
            }

            return w;
        }
    }
}