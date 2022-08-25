#region Usings

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class AuthorizationRequest : AttatchedForm
    {
        public AuthorizationRequest(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void EncounterData_PropertyChanged_Manager(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "BoolData")
            {
                return;
            }

            if (DynamicFormViewModel == null)
            {
                return;
            }

            if (EncounterData.BoolData.HasValue && EncounterData.BoolData.Value)
            {
                AttatchedFormCommand.Execute(null);
            }
        }

        public override bool Validate(out string SubSections)
        {
            EncounterData.ValidationErrors.Clear();
            if (EncounterData.BoolData.HasValue && EncounterData.BoolData.Value)
            {
                bool valid = base.Validate(out SubSections);
                if (valid == false)
                {
                    EncounterData.ValidationErrors.Add(new ValidationResult(
                        "Insurance Authorization Request form details are required.", new[] { "BoolData" }));
                }

                return valid;
            }

            // Pass validation as the question was not answered. Do not validate subsections
            SubSections = string.Empty;
            return true;
        }

        public override void Cleanup()
        {
            try
            {
                EncounterData.PropertyChanged -= EncounterData_PropertyChanged_Manager;
            }
            catch (Exception)
            {
            }

            base.Cleanup();
        }
    }

    public class AuthorizationRequestFactory
    {
        public static AuthorizationRequest Create(IDynamicFormService m, DynamicFormViewModel vm,
            FormSection formsection, int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            AuthorizationRequest qb = new AuthorizationRequest(__FormSectionQuestionKey)
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
                CurrentPatient = vm.CurrentPatient,
                FormSection = formsection,
                CopyForward = false
            };

            // Override data template to have red text
            qb.Question.DataTemplate = "AuthorizationRequestCheckbox";

            // Setup data for AttachedForm - never copyforward an Authorization request attached form
            qb.SetupData(vm, formsection, qgkey, false, q);
            SetHidden(qb);

            if (qb.EncounterData != null)
            {
                qb.EncounterData.PropertyChanged += qb.EncounterData_PropertyChanged_Manager;
            }

            return qb;
        }

        private static void SetHidden(AuthorizationRequest qb)
        {
            if (qb.EncounterData != null && qb.EncounterData.BoolData.HasValue)
            {
                qb.Hidden = false;
                ; // this question has been answered in the past, automatically show checkbox so the user can change answer
            }
            else
            {
                // Iterate over AdmissionAuthorization insurances, if any of those require authorizations, check the corresponding AuthorizationDetail to
                // see if it is contained in InsuranceAuthOrderTherapy_CView where Required = 1 and ComplianceType = 'Auth'

                if (CanPerformAuthorizationThresholdCheck(qb) && AdmissionIsInActiveCoverage(qb))
                {
                    // This method is responsible for setting the Hidden property of this question, called only after we are sure that the
                    // criteria is met to perform the Authorization Threshold check (same as Authorization Alerts check)
                    AuthorizationThresholdCheck(qb);
                }
                else
                {
                    // Criteria to show has not been met, hide question
                    qb.Hidden = true;
                }
            }
        }

        private static void AuthorizationThresholdCheck(AuthorizationRequest qb)
        {
            // Iterate over each AdmissionAuthorization to see if this admission meets the auth threshold
            if (qb.Admission.AdmissionAuthorizationDetail != null)
            {
                int authThreshold = TenantSettingsCache.Current.TenantSettingAuthorizationThreshold;

                if (qb.Admission.AdmissionAuthorizationDetail.Where(aad => aad.DeletedDate.HasValue == false).Any() ==
                    false)
                {
                    qb.Hidden =
                        true; // this admission has an insurance that requires authorizations but not authorizationdetail exists, hide checkbox
                    return;
                }

                qb.Hidden = true; // By default, hide the question

                foreach (var item in qb.Admission.AdmissionAuthorizationDetail
                             .Where(aad => aad.DeletedDate.HasValue == false)
                             .Where(aad => aad.AdmissionDisciplineKey == null || (aad.AdmissionDisciplineKey.HasValue &&
                                 aad.AdmissionDisciplineKey == qb.Encounter.DisciplineKey))
                             .Where(aad => aad.EffectiveFromDate <= DateTime.Today.Date)
                             .Where(aad => !aad.EffectiveToDate.HasValue || aad.EffectiveToDate >= DateTime.Today.Date)
                             .OrderBy(aad => aad.EffectiveToDate))
                {
                    // Find if the number of authorizations has met the threshold
                    bool authCountHasMetThreshold = false;
                    bool dateHasMetThreshold = false;

                    // Perform AuthCount check
                    if (item.AuthCount != null && item.AuthorizationAmount != 0 && item.AuthorizationAmount != 0)
                    {
                        authCountHasMetThreshold = ((item.AuthCount / item.AuthorizationAmount) * 100) > authThreshold;
                    }

                    // In addition, if there is an EffectiveToDate, check to see if the dates have met the threshold
                    if (item.EffectiveFromDate != null && item.EffectiveToDate != null)
                    {
                        // Find if the dates have met threshold
                        TimeSpan dif = ((DateTime)item.EffectiveToDate).Subtract(item.EffectiveFromDate);
                        int t = dif.Days * authThreshold / 100;

                        var date = item.EffectiveFromDate.AddDays(t - 1);

                        dateHasMetThreshold = (date < DateTime.Today.Date);
                    }

                    // If this admission has not met the AuthorizationThreshold then hide the question
                    qb.Hidden = (dateHasMetThreshold == false && authCountHasMetThreshold == false) && qb.Hidden;
                }
            }
            else
            {
                // No AdmissionAuthorizations, hide checkbox
                qb.Hidden = true;
            }
        }

        private static bool AdmissionIsInActiveCoverage(AuthorizationRequest qb)
        {
            // this admission is in an active coverage plan
            return qb.Admission.AdmissionCoverage.Where(a =>
                a.StartDate <= DateTime.Today.Date && (a.EndDate >= DateTime.Today.Date || a.EndDate == null)).Any();
        }

        private static bool CanPerformAuthorizationThresholdCheck(AuthorizationRequest qb)
        {
            bool canPerformAuthThresCheck = false;
            if (qb.Admission.AdmissionAuthorization != null && qb.Admission.AdmissionAuthorization.Any())
            {
                foreach (var authHeader in qb.Admission.AdmissionAuthorization)
                {
                    if (authHeader.PatientInsurance != null && authHeader.PatientInsurance.Insurance != null &&
                        authHeader.PatientInsurance.Insurance.Authorizations)
                    {
                        // HasValue is to filter non-service type auths from results (General, Supplies, Equipment)
                        // We only want to show the checkbox for the Discipline of the Encounter, ignore all
                        var myServiceTypes = authHeader.AdmissionAuthorizationDetail
                            .Where(aad => aad.DeletedDate.HasValue == false)
                            .Where(a => a.Discipline != null)
                            .Where(a => a.Discipline.DisciplineKey == qb.Encounter.DisciplineKey)
                            .Select(a => a.ServiceTypeKey);

                        // Get my insurance and assure that authorizations are required
                        var insurance = InsuranceCache.GetInsuranceFromKey(authHeader.PatientInsurance.InsuranceKey);

                        if (insurance.Authorizations)
                        {
                            canPerformAuthThresCheck = canPerformAuthThresCheck || myServiceTypes.Any();
                        }
                    }
                }
            }

            return canPerformAuthThresCheck;
        }
    }
}