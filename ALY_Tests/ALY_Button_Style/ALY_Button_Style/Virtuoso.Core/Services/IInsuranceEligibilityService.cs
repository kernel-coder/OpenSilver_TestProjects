#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IInsuranceEligibilityService
    {
        event EventHandler<EntityEventArgs<InsuranceParameterDefinition>> OnInsParmDefLoaded;
        event EventHandler<EntityEventArgs<InsuranceVerificationPOCO>> OnPOCOLoaded;
        event EventHandler<EntityEventArgs<EligibilityRequestJoin>> OnLoaded;
        event EventHandler<EntityEventArgs<Insurance>> OnInsLoaded;
        event EventHandler<EntityEventArgs<PatientInsurance>> OnPatInsLoaded;
        event EventHandler<ErrorEventArgs> OnSaved;
        event EventHandler<EntityEventArgs<InsuranceVerificationRequest>> OnInsVerReqLoaded;
        EntitySet<InsuranceVerificationRequest> InsuranceVerificationRequest { get; }

        VirtuosoDomainContext InsuranceEligibilityContext { get; set; }
        VirtuosoDomainContext InsuranceEligibilityContextViewOnly { get; set; }

        ObservableCollection<EligibilityRequestJoin> EligibilityRequestJoin { get; set; }

        void GetUnverifiedInsurancesAsync(int? SelectedInsuranceGroupKey, int DischargeDays, int TransferredDays);

        void GetNoActiveCoveragePlanInsurancesAsync(int? SelectedInsuranceGroupKey, int DischargeDays, int TransferredDays);

        void GetAuthorizationAlertsWorklistAsync(int? SelectedInsuranceGroupKey, int AuthorizationThreshold, int DischargeDays, int TransferredDays);

        void GetNoAuthOnFileAdmissionsAsync(int? SelectedInsuranceGroupKey, int DischargeDays, int TransferredDays);
        void GetInsVerReqForKeyListAsync(List<int> InsuranceVerificationRequestKeyList);

        void GetInsVerReqForBatch270CreateAsync(bool AdvancedSearch, bool IncludePendingRequests,
            bool PatientsWithCompEncounters, bool PatientsWithTasks, IEnumerable<int> InsuranceKeyList,
            DateTime? FromDate, DateTime? ThruDate);

        bool SaveAllAsync();
        void RejectChanges();

        byte[] GetSSRSPDFDynamicForm(int formKey, int patientKey, int encounterKey, int admissionKey, bool HideOasisQuestions);

        event Action<InvokeOperation<byte[]>> GetSSRSPDFDynamicFormReturned;
    }
}