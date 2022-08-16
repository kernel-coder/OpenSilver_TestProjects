#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IInsuranceEligibilityService))]
    public class InsuranceEligibilityService : PagedModelBase, IInsuranceEligibilityService
    {
        public event EventHandler<EntityEventArgs<InsuranceVerificationRequest>> OnInsVerReqLoaded;
        public event EventHandler<EntityEventArgs<InsuranceParameterDefinition>> OnInsParmDefLoaded;
        public event EventHandler<EntityEventArgs<InsuranceVerificationPOCO>> OnPOCOLoaded;
        public event EventHandler<EntityEventArgs<EligibilityRequestJoin>> OnLoaded;
        public event EventHandler<EntityEventArgs<Insurance>> OnInsLoaded;
        public event EventHandler<EntityEventArgs<PatientInsurance>> OnPatInsLoaded;
        public event EventHandler<ErrorEventArgs> OnSaved;

        public EntitySet<InsuranceVerificationRequest> InsuranceVerificationRequest =>
            InsuranceEligibilityContext.InsuranceVerificationRequests;

        public System.Threading.Tasks.Task<IEnumerable<CodeLookup>> InterfaceInstances()
        {
            var query = InsuranceEligibilityContext.InterfaceInstancesQuery();
            return DomainContextExtension.LoadAsync(InsuranceEligibilityContext,
                query);
        }

        public System.Threading.Tasks.Task<IEnumerable<InsuranceVerificationRequest>>
            SearchPatientForBatch270CreateAsync(string InsuranceKeyList, int DaysSinceLastCheck, DateTime? FromDate,
                DateTime? ThruDate)
        {
            var query = InsuranceEligibilityContext.SearchPatientsForBatch270CreateQuery(InsuranceKeyList,
                DaysSinceLastCheck, FromDate, ThruDate);
            return DomainContextExtension.LoadAsync(InsuranceEligibilityContext,
                query); 
        }

        public System.Threading.Tasks.Task<IEnumerable<InsuranceVerificationRequest>> CreateBatch270Async(
            string PatientInsuranceKeyList, int InterfaceInstanceKey)
        {
            var query = InsuranceEligibilityContext.CreateBatch270Query(PatientInsuranceKeyList, InterfaceInstanceKey);
            return DomainContextExtension.LoadAsync(InsuranceEligibilityContext,
                query);
        }

        public VirtuosoDomainContext InsuranceEligibilityContext { get; set; }
        public VirtuosoDomainContext InsuranceEligibilityContextViewOnly { get; set; }

        private ObservableCollection<EligibilityRequestJoin> eligibilityRequestJoin;

        public ObservableCollection<EligibilityRequestJoin> EligibilityRequestJoin
        {
            get { return eligibilityRequestJoin; }
            set
            {
                eligibilityRequestJoin = value;
                RaisePropertyChanged("EligibilityRequestJoin");
            }
        }

        public InsuranceEligibilityService()
        {
            InsuranceEligibilityContext = new VirtuosoDomainContext();
            InsuranceEligibilityContextViewOnly = new VirtuosoDomainContext();
        }

        public void GetInsVerReqForKeyListAsync(List<int> InsuranceVerificationRequestKeyList)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                InsuranceEligibilityContext.RejectChanges();
                InsuranceEligibilityContext.InsuranceVerificationRequests.Clear();
                InsuranceEligibilityContext.EligibilityRequestJoins.Clear();

                var query =
                    InsuranceEligibilityContext.GetInsVerReqForKeyListQuery(InsuranceVerificationRequestKeyList);

                query.IncludeTotalCount = true;

                IsLoading = true;

                InsuranceEligibilityContext.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    Loaded,
                    null);
            });
        }

        public void GetInsVerReqForBatch270CreateAsync(bool AdvancedSearch, bool IncludePendingRequests,
            bool PatientsWithCompEncounters, bool PatientsWithTasks, IEnumerable<int> InsuranceKeyList,
            DateTime? FromDate, DateTime? ThruDate)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                InsuranceEligibilityContext.RejectChanges();
                InsuranceEligibilityContext.InsuranceVerificationRequests.Clear();
                InsuranceEligibilityContext.EligibilityRequestJoins.Clear();

                var query = InsuranceEligibilityContext.GetInsVerReqForBatch270CreateQuery(AdvancedSearch,
                    IncludePendingRequests, PatientsWithCompEncounters, PatientsWithTasks, InsuranceKeyList, FromDate,
                    ThruDate);

                query.IncludeTotalCount = true;

                IsLoading = true;

                InsuranceEligibilityContext.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    Loaded,
                    null);
            });
        }

        public void GetUnverifiedInsurancesAsync(int? SelectedInsuranceKey, int DischargeDays, int TransferredDays)
        {
            IsLoading = true;
            InsuranceEligibilityContextViewOnly.RejectChanges();
            InsuranceEligibilityContextViewOnly.InsuranceVerificationPOCOs.Clear();

            var myQuery =
                InsuranceEligibilityContextViewOnly.GetUnverifiedPatientInsurancesQuery(SelectedInsuranceKey,
                    DischargeDays, TransferredDays);

            InsuranceEligibilityContextViewOnly.Load(
                myQuery,
                LoadBehavior.RefreshCurrent,
                Loaded,
                null);
        }

        public void GetNoActiveCoveragePlanInsurancesAsync(int? SelectedInsuranceGroupKey, int DischargeDays,
            int TransferredDays)
        {
            IsLoading = true;
            InsuranceEligibilityContextViewOnly.RejectChanges();
            InsuranceEligibilityContextViewOnly.InsuranceVerificationPOCOs.Clear();

            var myQuery =
                InsuranceEligibilityContextViewOnly.GetNoActiveCoveragePlanInsurancesQuery(SelectedInsuranceGroupKey,
                    DischargeDays, TransferredDays);

            InsuranceEligibilityContextViewOnly.Load(
                myQuery,
                LoadBehavior.RefreshCurrent,
                Loaded,
                null);
        }

        public void GetAuthorizationAlertsWorklistAsync(int? SelectedInsuranceGroupKey, int AuthorizationThreshold,
            int DischargeDays, int TransferredDays)
        {
            IsLoading = true;
            InsuranceEligibilityContextViewOnly.RejectChanges();
            InsuranceEligibilityContextViewOnly.InsuranceVerificationPOCOs.Clear();

            var myQuery =
                InsuranceEligibilityContextViewOnly.GetAuthorizationAlertsWorklistQuery(SelectedInsuranceGroupKey,
                    AuthorizationThreshold, DischargeDays, TransferredDays);

            InsuranceEligibilityContextViewOnly.Load(
                myQuery,
                LoadBehavior.RefreshCurrent,
                Loaded,
                null);
        }

        public void GetNoAuthOnFileAdmissionsAsync(int? SelectedInsuranceGroupKey, int DischargeDays,
            int TransferredDays)
        {
            IsLoading = true;
            InsuranceEligibilityContextViewOnly.RejectChanges();
            InsuranceEligibilityContextViewOnly.InsuranceVerificationPOCOs.Clear();

            var myQuery =
                InsuranceEligibilityContextViewOnly.GetRequiredAuthorizationsInsurancesQuery(SelectedInsuranceGroupKey,
                    DischargeDays, TransferredDays);

            InsuranceEligibilityContextViewOnly.Load(
                myQuery,
                LoadBehavior.RefreshCurrent,
                Loaded,
                null);
        }

        public override void LoadData()
        {
            if (IsLoading || InsuranceEligibilityContext == null)
            {
                return;
            }

            IsLoading = true;
        }

        private void Loaded(LoadOperation<Insurance> results)
        {
            //check that PatientPhoto has rows...

            DisplayErrors(results.Error);
            HandleEntityResults(results, OnInsLoaded);
            IsLoading = false;
        }

        private void Loaded(LoadOperation<EligibilityRequestJoin> results)
        {
            //check that PatientPhoto has rows...

            DisplayErrors(results.Error);
            EligibilityRequestJoin = results.Entities.ToObservableCollection();
            HandleEntityResults(results, OnLoaded);
            IsLoading = false;
        }

        private void Loaded(LoadOperation<PatientInsurance> results)
        {
            //check that PatientPhoto has rows...

            DisplayErrors(results.Error);
            HandleEntityResults(results, OnPatInsLoaded);
            IsLoading = false;
        }

        private void Loaded(LoadOperation<InsuranceParameterDefinition> results)
        {
            //check that PatientPhoto has rows...

            DisplayErrors(results.Error);
            HandleEntityResults(results, OnInsParmDefLoaded);
            IsLoading = false;
        }

        private void Loaded(LoadOperation<InsuranceVerificationRequest> results)
        {
            //check that PatientPhoto has rows...

            DisplayErrors(results.Error);
            HandleEntityResults(results, OnInsVerReqLoaded);
            IsLoading = false;
        }

        private void Loaded(LoadOperation<InsuranceVerificationPOCO> results)
        {
            DisplayErrors(results.Error);
            HandleEntityResults(results, OnPOCOLoaded);
            IsLoading = false;
        }

        private void DisplayErrors(Exception exc)
        {
            if (exc != null)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        public bool SaveAllAsync()
        {
            var open_or_invalid = OpenOrInvalidObjects(InsuranceEligibilityContext);
            if (open_or_invalid) //TODO: should we raise/return an error or something???
            {
                PendingSubmit = true;
                return false;
            }

            PendingSubmit = false;

            InsuranceEligibilityContext.SubmitChanges(SubmitData, null);

            return true;
        }

        private void SubmitData(SubmitOperation results)
        {
            DisplayErrors(results.Error);
            HandleErrorResults(results, OnSaved);
        }

        public void RejectChanges()
        {
            InsuranceEligibilityContext.RejectChanges();
        }

        public void Add(OtherInsuranceInformation entity)
        {
            InsuranceEligibilityContext.OtherInsuranceInformations.Add(entity);
        }

        public void Remove(OtherInsuranceInformation entity)
        {
            InsuranceEligibilityContext.OtherInsuranceInformations.Remove(entity);
        }

        public event Action<InvokeOperation<byte[]>> GetSSRSPDFDynamicFormReturned;

        public byte[] GetSSRSPDFDynamicForm(int formKey, int patientKey, int encounterKey, int admissionKey,
            bool HideOasisQuestions)
        {
            InsuranceEligibilityContext.GetSSRSPDFDynamicForm(formKey, patientKey, encounterKey, admissionKey,
                HideOasisQuestions, GetSSRSPDFDynamicFormReturned, null);
            return null;
        }
    }
}