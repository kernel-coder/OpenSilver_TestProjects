#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class Weight : QuestionBase, INotifyPropertyChanged
    {
        public Weight(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public enum eActualType
        {
            Weight,
            Height,
            UsualWeight,
            BMI,
            BSA
        }

        public virtual eActualType ActualType => eActualType.Weight;

        private EncounterWeight _EncounterWeight;

        public EncounterWeight EncounterWeight
        {
            get
            {
                if (_EncounterWeight == null)
                {
                    _EncounterWeight = Readings.OrderByDescending(o => o.WeightDateTime).FirstOrDefault();
                }

                return _EncounterWeight;
            }
        }

        public void EncounterWeight_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (OasisManager == null)
            {
                return;
            }

            if ((e.PropertyName.Equals("WeightValue")) || (e.PropertyName.Equals("WeightScale")) ||
                (e.PropertyName.Equals("HeightValue")) || (e.PropertyName.Equals("HeightScale")))
            {
                OasisManager.HeightWeightOasisMappingChanged(EncounterWeight);
            }
        }


        public float? HeightValue
        {
            get
            {
                var ew = EncounterWeight;
                return ew?.HeightValue;
            }
            set { EncounterWeight.HeightValue = value; }
        }

        public string HeightScale
        {
            get
            {
                var ew = EncounterWeight;
                return ew?.HeightScale;
            }
            set { EncounterWeight.HeightScale = value; }
        }

        public float? ReportedHeightValue
        {
            get
            {
                var ew = EncounterWeight;
                return ew?.ReportedHeightValue;
            }
            set { EncounterWeight.ReportedHeightValue = value; }
        }

        public string ReportedHeightScale
        {
            get
            {
                var ew = EncounterWeight;
                return ew?.ReportedHeightScale;
            }
            set { EncounterWeight.ReportedHeightScale = value; }
        }

        public float? WeightValue
        {
            get
            {
                var ew = EncounterWeight;
                return ew?.WeightValue;
            }
            set { EncounterWeight.WeightValue = value; }
        }

        public string WeightScale
        {
            get
            {
                var ew = EncounterWeight;
                return ew?.WeightScale;
            }
            set { EncounterWeight.WeightScale = value; }
        }

        public float? UsualWeightValue
        {
            get
            {
                var ew = EncounterWeight;
                return ew?.UsualWeightValue;
            }
            set { EncounterWeight.UsualWeightValue = value; }
        }

        public string UsualWeightScale
        {
            get
            {
                var ew = EncounterWeight;
                return ew?.UsualWeightScale;
            }
            set { EncounterWeight.UsualWeightScale = value; }
        }


        public float? WeightKg
        {
            get
            {
                if (WeightValue.HasValue && WeightValue.Value > 0)
                {
                    return HeightWeightCalculations.CvtWeightKg(WeightScale, WeightValue.Value);
                }

                return null;
            }
        }

        public float? UsualWeightKg
        {
            get
            {
                if (UsualWeightValue.HasValue && UsualWeightValue.Value > 0)
                {
                    return HeightWeightCalculations.CvtWeightKg(UsualWeightScale, UsualWeightValue.Value);
                }

                return null;
            }
        }

        public float? HeightCm
        {
            get
            {
                if (HeightValue.HasValue && HeightValue.Value > 0)
                {
                    return HeightWeightCalculations.CvtHeightCm(HeightScale, HeightValue.Value);
                }

                return null;
            }
        }

        public float? ReportedHeightCm
        {
            get
            {
                if (ReportedHeightValue.HasValue)
                {
                    return HeightWeightCalculations.CvtHeightCm(ReportedHeightScale, ReportedHeightValue.Value);
                }

                return null;
            }
        }

        public float? BMIValue
        {
            get
            {
                float? result = null;
                var hcm = HeightCm;
                var wkg = WeightKg;

                if (hcm.HasValue && wkg.HasValue)
                {
                    result = HeightWeightCalculations.CalculateBMIValue(hcm.Value, wkg.Value);
                }

                EncounterWeight.BMIValue = result;
                return result;
            }
        }

        public float? BSAValue
        {
            get
            {
                float? result = null;
                float? hcm = HeightCm;
                float? wkg = WeightKg;
                if (hcm.HasValue && wkg.HasValue)
                {
                    string formula = CodeLookupCache.GetCodeFromKey(TenantSettingsCache.Current.TenantSetting.BSAFormula);

                    result = HeightWeightCalculations.CalculateBSAValue(hcm.Value, wkg.Value, formula);

                    EncounterWeight.BSAScale = formula;
                }

                EncounterWeight.BSAValue = result;
                return result;
            }
        }

        public bool IsVersion1
        {
            get
            {
                var ew = EncounterWeight;
                return ew?.IsVersion1 ?? false;
            }
        }

        public bool ShowReportedHeight => IsVersion1;
        public string HeightPrompt => EncounterWeight.GetHeightPrompt(IsVersion1);
        public string WeightPrompt => EncounterWeight.GetWeightPrompt(IsVersion1);
        public string ReportedHeightPrompt => EncounterWeight.GetReportedHeightPrompt(IsVersion1);
        public string UsualWeightPrompt => EncounterWeight.GetUsualWeightPrompt(IsVersion1);

        public string HeightScaleGroup => "HeightScaleGroup" + Question.QuestionKey.ToString().Trim();

        public string ReportedHeightScaleGroup => "ReportedHeightScaleGroup" + Question.QuestionKey.ToString().Trim();

        public string WeightScaleGroup => "WeightScaleGroup" + Question.QuestionKey.ToString().Trim();

        public string UsualWeightScaleGroup => "UsualWeightScaleGroup" + Question.QuestionKey.ToString().Trim();

        public string UsualWeightScaleGroupV2 => "UsualWeightScaleGroupV2" + Question.QuestionKey.ToString().Trim();

        public ObservableCollection<EncounterWeight> Readings { get; set; }
        public RelayCommand AddReadingCommand { get; set; }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            float? value = EncounterWeight.WeightValue;
            string scale = EncounterWeight.WeightScale;

            var removeSet = EncounterWeight.ValidationErrors
                .Where(w => ("[" + w.ErrorMessage).Contains("[Weight req"))
                .ToList();

            foreach (var ve in removeSet)
            {
                EncounterWeight.ValidationErrors.Remove(ve);
            }

            if (value.HasValue && string.IsNullOrWhiteSpace(scale))
            {
                EncounterWeight.ValidationErrors.Add(new ValidationResult("Weight requires pounds or kilograms",
                    new[] { "WeightScale" }));
                return false;
            }

            if (Encounter.FullValidation)
            {
                if (!value.HasValue && !string.IsNullOrWhiteSpace(scale))
                {
                    EncounterWeight.WeightScale = null;
                }

                if (Required)
                {
                    if (!value.HasValue || value.Value == 0)
                    {
                        EncounterWeight.ValidationErrors.Add(new ValidationResult("Weight required",
                            new[] { "WeightValue" }));
                        return false;
                    }
                }
            }

            return true;
        }

        public override string BuildBMIMessage(bool value)
        {
            if (value)
            {
                return BMIValue.HasValue ? BMIValue.ToString() : string.Empty;
            }

            return BSAValue.HasValue ? BSAValue.ToString() : string.Empty;
        }

        public void CopyProperties(EncounterWeight source, EncounterWeight destination)
        {
            if (source != null && destination != null)
            {
                destination.WeightDateTime = source.WeightDateTime;
                destination.WeightValue = source.WeightValue;
                destination.WeightScale = source.WeightScale;
                destination.WeightValue = source.UsualWeightValue;
                destination.WeightScale = source.UsualWeightScale;
                destination.HeightValue = source.HeightValue;
                destination.HeightScale = source.HeightScale;
                destination.BMIValue = source.BMIValue;
                destination.BSAValue = source.BSAValue;
                destination.BSAScale = source.BSAScale;
                destination.TeleMonitorResultKey = source.TeleMonitorResultKey;
                destination.ReportedHeightValue = source.ReportedHeightValue;
                destination.ReportedHeightScale = source.ReportedHeightScale;

                if (source.Version > 1)
                {
                    destination.Version = source.Version;
                }
                else
                {
                    destination.Version = 1;
                }
            }
        }

        public EncounterWeight PreviousWeightAnyEncounter()
        {
            var x = Admission.Encounter.Where(p => !p.IsNew)
                .SelectMany(s => s.EncounterWeight)
                .OrderByDescending(o => o.WeightDateTime)
                .FirstOrDefault();
            return x;
        }

        public EncounterWeight MostRecentUsualWeight()
        {
            if ((Admission?.Encounter == null) || (Encounter == null))
            {
                return null;
            }

            List<Encounter> eList = Admission.Encounter
                .Where(p => ((p.IsNew == false) && (p.EncounterKey != Encounter.EncounterKey)))
                .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime)
                .ToList();

            foreach (Encounter e in eList)
            {
                EncounterWeight ew = e.EncounterWeight
                    .FirstOrDefault(p => ((p.UsualWeightValue != null) && (string.IsNullOrWhiteSpace(p.UsualWeightScale) == false)));
                if (ew != null)
                {
                    return ew;
                }
            }

            return null;
        }

        public EncounterWeight MostRecentReportedHeight()
        {
            if ((Admission?.Encounter == null) || (Encounter == null))
            {
                return null;
            }

            List<Encounter> eList = Admission.Encounter
                .Where(p => ((p.IsNew == false) && (p.EncounterKey != Encounter.EncounterKey)))
                .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime)
                .ToList();

            foreach (Encounter e in eList)
            {
                EncounterWeight ew = e.EncounterWeight
                    .FirstOrDefault(p => ((p.ReportedHeightValue != null) && (string.IsNullOrWhiteSpace(p.ReportedHeightScale) == false)));
                if (ew != null)
                {
                    return ew;
                }
            }

            return null;
        }

        public override void Cleanup()
        {
            try
            {
                EncounterWeight.PropertyChanged -= EncounterWeight_PropertyChanged;
            }
            catch (Exception)
            {
            }

            base.Cleanup();
        }

        public override bool CopyForwardLastInstance()
        {
            return true;
        }

        private EncounterWeight BackupEncounterWeight = new EncounterWeight();

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if (BackupEncounterWeight != null && EncounterWeight != null)
                {
                    CopyProperties(BackupEncounterWeight, EncounterWeight);
                }
            }
            else
            {
                if (BackupEncounterWeight != null && EncounterWeight != null)
                {
                    BackupEncounterWeight = new EncounterWeight();
                    CopyProperties(EncounterWeight, BackupEncounterWeight);
                }
            }
        }

        public void ReadWeights(Encounter encounter, bool copyforward)
        {
            Readings = new ObservableCollection<EncounterWeight>();

            var ewc = encounter.EncounterWeight
                .OrderBy(x => x.WeightDateTime)
                .ToList();

            Readings.Clear();
            foreach (var item in ewc) Readings.Add(item);

            //default with one and allow more to be added
            if (Readings.Any() == false)
            {
                EncounterWeight ew = new EncounterWeight { TenantID = encounter.TenantID, Version = 2 };
                // Only copy forward the Most Recent UsualWeight and Reported Height
                // All measures stuff never copies forward
                EncounterWeight mruw = MostRecentUsualWeight();
                if (mruw != null)
                {
                    ew.UsualWeightValue = mruw.UsualWeightValue;
                    ew.UsualWeightScale = mruw.UsualWeightScale;
                }

                EncounterWeight mrrh = MostRecentReportedHeight();
                if (mrrh != null)
                {
                    ew.ReportedHeightValue = mrrh.ReportedHeightValue;
                    ew.ReportedHeightScale = mrrh.ReportedHeightScale;
                }

                encounter.EncounterWeight.Add(ew);
                Readings.Add(ew);
                if (OasisManager != null)
                {
                    OasisManager.HeightWeightOasisMappingChanged(ew);
                }
            }

            EncounterWeight w = EncounterWeight;
            w.PropertyChanged += EncounterWeight_PropertyChanged;
        }
    }

    public class WeightFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            var qui = new Weight(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
            };

            qui.ReadWeights(vm.CurrentEncounter, copyforward);

            return qui;
        }
    }
}