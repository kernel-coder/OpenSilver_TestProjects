#region Usings

using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class BMI : Weight
    {
        public BMI(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override eActualType ActualType => eActualType.BMI;

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            return true;
        }

        public bool ShowBMIChildLink
        {
            get
            {
                if (DynamicFormViewModel == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentPatient == null)
                {
                    return false;
                }

                return DynamicFormViewModel.CurrentPatient.ShowBMIChildLink;
            }
        }

        public bool ShouldShowBSA
        {
            get
            {
                if (DynamicFormViewModel == null || DynamicFormViewModel.CurrentForm == null)
                {
                    return false;
                }

                var ret = (Admission.HospiceAdmission == false);
                return ret;
            }
        }

        public string BSALabel
        {
            get
            {
                string BSAFormula =
                    CodeLookupCache.GetCodeFromKey(TenantSettingsCache.Current.TenantSetting.BSAFormula);
                return BSAFormula + " BSA";
            }
        }

        public override void ProcessBMIMessage(string[] message)
        {
            if (WeightValue.HasValue && WeightValue.Value > 0)
            {
                Encounter.WeightKG = WeightValue;
                if (HeightValue.HasValue && HeightValue.Value > 0)
                {
                    try
                    {
                        Encounter.BodySurfaceArea = (double)BSAValue;
                    }
                    catch
                    {
                        Encounter.BodySurfaceArea = 0;
                    }
                }
                else
                {
                    Encounter.BodySurfaceArea = 0;
                }
            }
            else
            {
                Encounter.WeightKG = null;
                Encounter.BodySurfaceArea = 0;
            }

            this.RaisePropertyChangedLambda(p => p.BMIValue);
            this.RaisePropertyChangedLambda(p => p.BSAValue);
        }
    }

    public class BMIFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            var qui = new BMI(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline,
                OasisManager = vm.CurrentOasisManager,
            };

            if (vm != null)
            {
                if (vm.CurrentPatient != null)
                {
                    qui.Hidden = (vm.CurrentPatient.ShowBMI) ? false : true;
                }
            }

            qui.ReadWeights(vm.CurrentEncounter, copyforward);

            return qui;
        }
    }
}