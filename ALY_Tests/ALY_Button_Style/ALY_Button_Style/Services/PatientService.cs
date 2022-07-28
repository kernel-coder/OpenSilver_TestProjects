#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Offline;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Utility;
using Virtuoso.Metrics;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IPatientService))]
    public partial class PatientService : PagedModelBase, IPatientService
    {
        public event EventHandler<EntityEventArgs<Patient>> OnPatientRefreshLoaded;

        public event EventHandler<EntityEventArgs<InsuranceVerificationRequest>> OnInsuranceVerificationRequestLoaded;

        public event EventHandler<EntityEventArgs<PatientTeachingSheet>> OnPatientTeachingSheetLoaded;
        public event EventHandler<EntityEventArgs<PatientScreening>> OnPatientScreeningLoaded;

        public event EventHandler<EntityEventArgs<Admission>> OnPatientAdmissionRefreshLoaded; //DS 0406

        public event EventHandler<EntityEventArgs<AdmissionCareCoordinatorHistoryPOCO>>
            OnGetAdmissionCareCoordinatorHistoryLoaded; //DS 0406

        public event EventHandler<EntityEventArgs<Patient>> OnPatientAdmissionFullDetailsLoaded; //DS 0421

        public event EventHandler<EntityEventArgs<PatientInsurance>> OnPatientInsuranceRefreshLoaded;

        public event EventHandler<EntityEventArgs<Admission>> OnAdmissionRefreshLoaded;
        public event EventHandler<EntityEventArgs<ReportArchive>> OnGetReportArchiveLoaded;
        public event EventHandler<EntityEventArgs<Patient>> OnGetPatientsPharmacyRefillLoaded;
        public event EventHandler<EntityEventArgs<Patient>> OnGetPatientsHospicePumpLoaded;
        public event EventHandler<EntityEventArgs<AdmissionHospicePump>> OnGetAdmissionHospicePumpForAdmissionLoaded;

        public event EventHandler<EntityEventArgs<AdmissionPharmacyRefill>>
            OnGetAdmissionPharmacyRefillForAdmissionLoaded;

        public event EventHandler<EntityEventArgs<TeamMeetingPOCO>> OnGetTeamMeetingWorkListLoaded;
        public event EventHandler<EntityEventArgs<TeamMeetingRosterPOCO>> OnGetTeamMeetingRosterWorkListLoaded;
        public event EventHandler<EntityEventArgs<Tenant>> OnGetHospiceRefillImportForImportLoaded;
        public event EventHandler<BatchEventArgs> OnAdmissionEncounterAndServicesRefreshLoaded;
        public event EventHandler<EntityEventArgs<Encounter>> OnAdmissionEncounterRefreshLoaded;
        public event EventHandler<EntityEventArgs<AdmissionDocumentation>> OnAdmissionDocumentationRefreshLoaded;

        public event EventHandler<EntityEventArgs<AdmissionDiagnosisGroup>>
            OnGetAdmissionDiagnosisGroupsForAdmissionLoaded;

        public event EventHandler<EntityEventArgs<InsuranceVerificationRequest>>
            OnRefreshPatientInsuranceVerificationRequestLoaded;

        //KSM 03272014
        public event EventHandler<EntityEventArgs<AddressReturn>> OnAddressReturnLoaded;
        public event EventHandler<ADFResponseEventArgs> OnADFVendorResponseLoaded;

        public event EventHandler<ADFResponseEventArgs> OnADFVendorCountLoaded;
        //KSM

        public event EventHandler<MultiErrorEventArgs> OnMultiSaved;

        protected virtual void
            OnMultiSavedChanged(MultiErrorEventArgs e) // Helper, so that derived classes can raise the event
        {
            // Safely raise the event for all subscribers
            OnMultiSaved?.Invoke(this, e);
        }

        public event EventHandler<MultiErrorEventArgs> OnRefreshCertCyclesAndPhysiciansLoaded;
        public VirtuosoDomainContext Context { get; set; }

        public bool IsSubmitting => Context.IsSubmitting;
        public bool HasInsuranceRelatedChanges => false;

        //throw new NotImplementedException();
        [ImportingConstructor]
        public PatientService(IUriService _uriService)
        {
            if (_uriService != null)
            {
                Context = new VirtuosoDomainContext(new Uri(_uriService.Uri,
                    "Virtuoso-Services-Web-VirtuosoDomainService.svc")); //using alternate constructor, so that it can run in a thread
            }
            else
            {
                Context = new VirtuosoDomainContext();
            }

            var contextServiceProvider = new SimpleServiceProvider();
            contextServiceProvider.AddService<ICodeLookupDataProvider>(new CodeLookupDataProvider());
            contextServiceProvider.AddService<IWoundDataProvider>(new WoundDataProvider());
            contextServiceProvider.AddService<IPatientContactDataProvider>(new PatientContactDataProvider());
            contextServiceProvider.AddService<IAdmissionDataProvider>(new AdmissionDataProvider());
            contextServiceProvider.AddService<IServiceLineTypeProvider>(new ServiceLineTypeProvider());
            contextServiceProvider.AddService<IUniquenessCheckProvider>(new UniquenessCheckProvider());

            Context.ValidationContext = new ValidationContext(this, contextServiceProvider, null);

            Patients = new PagedEntityCollectionView<Patient>(Context.Patients, this);

            Context.PropertyChanged += Context_PropertyChanged;

            EntityManager.Current.NetworkAvailabilityChanged += Current_NetworkAvailabilityChanged;
            IsOnline = EntityManager.Current.IsOnline;
        }

        private bool IsOnline = true;

        private void Current_NetworkAvailabilityChanged(object sender,
            Client.Offline.Events.NetworkAvailabilityEventArgs e)
        {
            IsOnline = e.IsAvailable;
        }

        #region PagedModelBase Members

        public override void LoadData()
        {
            if (IsLoading || Context == null)
            {
                return;
            }

            IsLoading = true;

            GetAsync();
        }

        #endregion

        #region IModelDataService<Patient> Members

        public void AddChangeHistory(ChangeHistory entity)
        {
            Context.ChangeHistories.Add(entity);
        }

        public void Add(Patient entity)
        {
            Context.Patients.Add(entity);
        }

        public void Add(PatientImmunization entity)
        {
            Context.PatientImmunizations.Add(entity);
        }

        public void Remove(Patient entity)
        {
            Context.Patients.Remove(entity);
        }

        public void Remove(Admission entity)
        {
            Context.Admissions.Remove(entity);
        }

        public void Detach(Admission entity)
        {
            Context.Admissions.Detach(entity);
        }

        public void Remove(AdmissionPhysician entity)
        {
            Context.AdmissionPhysicians.Remove(entity);
        }

        public void Remove(AdmissionCommunication entity)
        {
            Context.AdmissionCommunications.Remove(entity);
        }

        public void Remove(AdmissionCommunicationAllergy entity)
        {
            Context.AdmissionCommunicationAllergies.Remove(entity);
        }

        public void Remove(AdmissionCoverage entity)
        {
            if (entity.AdmissionCoverageInsurance != null)
            {
                foreach (AdmissionCoverageInsurance e in entity.AdmissionCoverageInsurance.ToList()) Remove(e);
            }

            Context.AdmissionCoverages.Remove(entity);
        }

        public void Remove(AdmissionCoverageInsurance entity)
        {
            Context.AdmissionCoverageInsurances.Remove(entity);
        }

        public void Remove(AdmissionMedicationMAR entity)
        {
            Context.AdmissionMedicationMARs.Remove(entity);
        }

        public void Remove(PatientMedicationAdministration entity)
        {
            if (entity.PatientMedicationAdministrationMed != null)
            {
                foreach (PatientMedicationAdministrationMed e in entity.PatientMedicationAdministrationMed.ToList())
                    Remove(e);
            }

            Context.PatientMedicationAdministrations.Remove(entity);
        }

        public void Remove(PatientMedicationAdministrationMed entity)
        {
            Context.PatientMedicationAdministrationMeds.Remove(entity);
        }

        public void Remove(PatientMedicationReconcile entity)
        {
            if (entity.PatientMedicationReconcileMed != null)
            {
                foreach (PatientMedicationReconcileMed e in entity.PatientMedicationReconcileMed.ToList()) Remove(e);
            }

            Context.PatientMedicationReconciles.Remove(entity);
        }

        public void Remove(PatientMedicationReconcileMed entity)
        {
            Context.PatientMedicationReconcileMeds.Remove(entity);
        }

        public void Remove(PatientMedicationTeaching entity)
        {
            if (entity.PatientMedicationTeachingMed != null)
            {
                foreach (PatientMedicationTeachingMed e in entity.PatientMedicationTeachingMed.ToList()) Remove(e);
            }

            Context.PatientMedicationTeachings.Remove(entity);
        }

        public void Remove(PatientMedicationTeachingMed entity)
        {
            Context.PatientMedicationTeachingMeds.Remove(entity);
        }

        public void Remove(PatientMedicationManagement entity)
        {
            if (entity.PatientMedicationManagementMed != null)
            {
                foreach (PatientMedicationManagementMed e in entity.PatientMedicationManagementMed.ToList()) Remove(e);
            }

            Context.PatientMedicationManagements.Remove(entity);
        }

        public void Remove(PatientMedicationManagementMed entity)
        {
            Context.PatientMedicationManagementMeds.Remove(entity);
        }

        public void Remove(AdmissionCommunicationLab entity)
        {
            Context.AdmissionCommunicationLabs.Remove(entity);
        }

        public void Remove(AdmissionCommunicationMedication entity)
        {
            Context.AdmissionCommunicationMedications.Remove(entity);
        }

        public void Remove(AdmissionConsent entity)
        {
            Context.AdmissionConsents.Remove(entity);
        }

        public void Remove(EncounterEquipment entity)
        {
            Context.EncounterEquipments.Remove(entity);
        }

        public void Remove(AdmissionEquipment entity)
        {
            Context.AdmissionEquipments.Remove(entity);
        }

        public void Remove(AdmissionProductCode entity)
        {
            Context.AdmissionProductCodes.Remove(entity);
        }

        public void Remove(AdmissionAuthorization entity)
        {
            if (entity.AdmissionAuthorizationDetail != null)

            {
                foreach (AdmissionAuthorizationDetail e in entity.AdmissionAuthorizationDetail.ToList())
                    ////if (e.AdmissionAuthorizationDetailKey <= 0) Remove them all then remove the parent so we don't get foreign key constraint errors.
                    Remove(e);
            }

            Context.AdmissionAuthorizations.Remove(entity);
        }

        public void Remove(AdmissionAuthorizationInstance entity)
        {
            Context.AdmissionAuthorizationInstances.Remove(entity);
        }

        public void Remove(AdmissionAuthorizationDetail entity)
        {
            Context.AdmissionAuthorizationDetails.Remove(entity);
        }

        public void Remove(AdmissionCertification entity)
        {
            Context.AdmissionCertifications.Remove(entity);
        }

        public void Remove(AdmissionFaceToFaceDiagnosis entity)
        {
            Context.AdmissionFaceToFaceDiagnosis.Remove(entity);
        }

        public void Remove(AdmissionGroup entity)
        {
            Context.AdmissionGroups.Remove(entity);
        }

        public void Remove(AdmissionInfection entity)
        {
            Context.AdmissionInfections.Remove(entity);
        }

        public void Remove(PatientInfection entity)
        {
            Context.PatientInfections.Remove(entity);
        }

        public void Remove(PatientAdverseEvent entity)
        {
            Context.PatientAdverseEvents.Remove(entity);
        }

        public void Remove(EncounterDisciplineFrequency entity)
        {
            Context.EncounterDisciplineFrequencies.Remove(entity);
        }

        public void Remove(EncounterStartDisciplineFrequency entity)
        {
            Context.EncounterStartDisciplineFrequencies.Remove(entity);
        }

        public void Remove(EncounterInfection entity)
        {
            Context.EncounterInfections.Remove(entity);
        }

        public void Remove(EncounterPatientInfection entity)
        {
            Context.EncounterPatientInfections.Remove(entity);
        }

        public void Remove(EncounterPatientAdverseEvent entity)
        {
            Context.EncounterPatientAdverseEvents.Remove(entity);
        }

        public void Remove(EncounterLab entity)
        {
            Context.EncounterLabs.Remove(entity);
        }

        public void Remove(EncounterSupervision entity)
        {
            Context.EncounterSupervisions.Remove(entity);
        }

        public void Remove(EncounterOasis entity)
        {
            Context.EncounterOasis.Remove(entity);
        }

        public void Remove(EncounterOasisAlert entity)
        {
            Context.EncounterOasisAlerts.Remove(entity);
        }

        public void Remove(OrderEntry entity)
        {
            Context.OrderEntries.Remove(entity);
        }

        public void Remove(OrderEntryVO entity)
        {
            Context.OrderEntryVOs.Remove(entity);
        }

        public void Remove(OrderEntrySignature entity)
        {
            Context.OrderEntrySignatures.Remove(entity);
        }

        public void Remove(OrderEntryCoSignature entity)
        {
            Context.OrderEntryCoSignatures.Remove(entity);
        }

        public void Remove(PatientAddress entity)
        {
            Context.PatientAddresses.Remove(entity);
        }

        public void Remove(PatientAllergy entity)
        {
            foreach (EncounterAllergy e in entity.EncounterAllergy) Remove(e);
            Context.PatientAllergies.Remove(entity);
        }

        public void Remove(PatientContact entity)
        {
            Context.PatientContacts.Remove(entity);
        }

        public void Remove(AdmissionDiagnosis entity)
        {
            foreach (EncounterDiagnosis e in entity.EncounterDiagnosis) Remove(e);
            Context.AdmissionDiagnosis.Remove(entity);
        }

        public void Remove(PatientDiagnosisComment entity)
        {
            try
            {
                foreach (EncounterDiagnosisComment e in entity.EncounterDiagnosisComment) Remove(e);
            }
            catch
            {
            }

            Context.PatientDiagnosisComments.Remove(entity);
        }

        public void Remove(PatientInsurance entity)
        {
            Context.PatientInsurances.Remove(entity);
        }

        public void Remove(PatientPharmacy entity)
        {
            Context.PatientPharmacies.Remove(entity);
        }

        public void Remove(PatientMedication entity)
        {
            foreach (PatientMedicationSlidingScale s in entity.PatientMedicationSlidingScale) Remove(s);
            foreach (EncounterMedication e in entity.EncounterMedication) Remove(e);
            foreach (EncounterStartMedication e in entity.EncounterStartMedication) Remove(e);
            Context.PatientMedications.Remove(entity);
        }

        public void Remove(PatientMedicationSlidingScale entity)
        {
            Context.PatientMedicationSlidingScales.Remove(entity);
        }

        public void Remove(AdmissionLevelOfCare entity)
        {
            foreach (EncounterLevelOfCare e in entity.EncounterLevelOfCare) Remove(e);
            Context.AdmissionLevelOfCares.Remove(entity);
        }

        public void Remove(AdmissionSiteOfService entity)
        {
            Context.AdmissionSiteOfServices.Remove(entity);
        }

        public void Remove(AdmissionPainLocation entity)
        {
            foreach (EncounterPainLocation e in entity.EncounterPainLocation) Remove(e);
            Context.AdmissionPainLocations.Remove(entity);
        }

        public void Remove(AdmissionPharmacyRefill entity)
        {
            Context.AdmissionPharmacyRefills.Remove(entity);
        }

        public void Remove(AdmissionHospicePump entity)
        {
            Context.AdmissionHospicePumps.Remove(entity);
        }

        public void Remove(AdmissionIVSite entity)
        {
            foreach (EncounterIVSite e in entity.EncounterIVSite) Remove(e);
            Context.AdmissionIVSites.Remove(entity);
        }

        public void Remove(AdmissionWoundSite entity)
        {
            foreach (EncounterWoundSite e in entity.EncounterWoundSite) Remove(e);
            Context.AdmissionWoundSites.Remove(entity);
        }

        public void Remove(WoundPhoto entity)
        {
            Context.WoundPhotos.Remove(entity);
        }

        public void Remove(AdmissionGoal entity)
        {
            foreach (AdmissionGoalElement a in entity.AdmissionGoalElement.ToList()) Remove(a);
            foreach (EncounterGoal e in entity.EncounterGoal.ToList()) Remove(e);
            Context.AdmissionGoals.Remove(entity);
        }

        public void Remove(AdmissionGoalElement entity)
        {
            foreach (EncounterGoalElement e in entity.EncounterGoalElement.ToList()) Remove(e);
            Context.AdmissionGoalElements.Remove(entity);
        }

        public void Remove(AdmissionDocumentation entity)
        {
            Context.AdmissionDocumentations.Remove(entity);
        }

        public void Remove(AdmissionDiscipline entity)
        {
            Context.AdmissionDisciplines.Remove(entity);
        }

        public void Remove(AdmissionDisciplineFrequency entity)
        {
            Context.AdmissionDisciplineFrequencies.Remove(entity);
        }

        public void Remove(AdmissionReferral entity)
        {
            Context.AdmissionReferrals.Remove(entity);
        }

        public void Remove(PatientAdvancedDirective entity)
        {
            Context.PatientAdvancedDirectives.Remove(entity);
        }

        public void Remove(PatientFacilityStay entity)
        {
            Context.PatientFacilityStays.Remove(entity);
        }

        public void Remove(PatientLab entity)
        {
            Context.PatientLabs.Remove(entity);
        }

        public void Remove(PatientMessage entity)
        {
            Context.PatientMessages.Remove(entity);
        }

        public void Remove(PatientPhone entity)
        {
            Context.PatientPhones.Remove(entity);
        }

        public void Remove(PatientAlternateID entity)
        {
            Context.PatientAlternateIDs.Remove(entity);
        }

        public void Remove(PatientGenderExpression entity)
        {
            Context.PatientGenderExpressions.Remove(entity);
        }

        public void Remove(PatientPhoto entity)
        {
            Context.PatientPhotos.Remove(entity);
        }

        public void Remove(PatientTranslator entity)
        {
            Context.PatientTranslators.Remove(entity);
        }

        public void Remove(EncounterAllergy entity)
        {
            Context.EncounterAllergies.Remove(entity);
        }

        public void Remove(EncounterAttachedForm entity)
        {
            Context.EncounterAttachedForms.Remove(entity);
        }

        public void Remove(EncounterCMSForm entity)
        {
            foreach (EncounterCMSFormField ecff in entity.EncounterCMSFormField)
                Context.EncounterCMSFormFields.Remove(ecff);
            Context.EncounterCMSForms.Remove(entity);
        }

        public void Remove(EncounterCMSFormField entity)
        {
            Context.EncounterCMSFormFields.Remove(entity);
        }

        public void Remove(EncounterConsent entity)
        {
            Context.EncounterConsents.Remove(entity);
        }

        public void Remove(EncounterCoSignature entity)
        {
            Context.EncounterCoSignatures.Remove(entity);
        }

        public void Remove(EncounterData entity)
        {
            Context.EncounterDatas.Remove(entity);
        }

        public void Remove(EncounterDiagnosis entity)
        {
            Context.EncounterDiagnosis.Remove(entity);
        }

        public void Remove(EncounterDiagnosisComment entity)
        {
            Context.EncounterDiagnosisComments.Remove(entity);
        }

        public void Remove(EncounterMedication entity)
        {
            Context.EncounterMedications.Remove(entity);
        }

        public void Remove(EncounterStartMedication entity)
        {
            Context.EncounterStartMedications.Remove(entity);
        }

        public void Remove(EncounterLevelOfCare entity)
        {
            Context.EncounterLevelOfCares.Remove(entity);
        }

        public void Remove(EncounterPainLocation entity)
        {
            Context.EncounterPainLocations.Remove(entity);
        }

        public void Remove(EncounterIVSite entity)
        {
            Context.EncounterIVSites.Remove(entity);
        }

        public void Remove(EncounterWoundSite entity)
        {
            Context.EncounterWoundSites.Remove(entity);
        }

        public void Remove(EncounterGoal entity)
        {
            Context.EncounterGoals.Remove(entity);
        }

        public void Remove(EncounterGoalElement entity)
        {
            foreach (var ge in entity.EncounterGoalElementDiscipline)
                Context.EncounterGoalElementDisciplines.Remove(ge);
            Context.EncounterGoalElements.Remove(entity);
        }

        public void Remove(EncounterSupply entity)
        {
            Context.EncounterSupplies.Remove(entity);
        }

        public void Remove(EncounterTeamMeeting entity)
        {
            Context.EncounterTeamMeetings.Remove(entity);
        }

        public void Remove(EncounterVisitFrequency entity)
        {
            Context.EncounterVisitFrequencies.Remove(entity);
        }

        public void Remove(EncounterVendor entity)
        {
            Context.EncounterVendors.Remove(entity);
        }

        public void Remove(EncounterNonServiceTime entity)
        {
            Context.EncounterNonServiceTimes.Remove(entity);
        }

        public void Remove(InsuranceEligibility entity)
        {
            Context.InsuranceEligibilities.Remove(entity);
        }

        public void Remove(HospiceRefillImport entity)
        {
            Context.HospiceRefillImports.Remove(entity);
        }

        public void Remove(HospiceRefillImportColumnList entity)
        {
            Context.HospiceRefillImportColumnLists.Remove(entity);
        }

        public void Add(AdmissionBillingReview entity)
        {
            Context.AdmissionBillingReviews.Add(entity);
        }

        public void Add(HospiceRefillImport entity)
        {
            Context.HospiceRefillImports.Add(entity);
        }

        public void Add(HospiceRefillImportColumnList entity)
        {
            Context.HospiceRefillImportColumnLists.Add(entity);
        }

        public void Remove(InsuranceVerifyHistory entity)
        {
            Context.InsuranceVerifyHistories.Remove(entity);
        }

        public void Remove(InsuranceVerifyHistoryDetail entity)
        {
            Context.InsuranceVerifyHistoryDetails.Remove(entity);
        }

        public void Add(InsuranceVerifyHistory entity)
        {
            Context.InsuranceVerifyHistories.Add(entity);
        }

        public void Add(InsuranceVerifyHistoryDetail entity)
        {
            Context.InsuranceVerifyHistoryDetails.Add(entity);
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
        }

        //public List<string> GetUnusedIDs(Patient patient)
        private List<string> GetUnusedIDs(string currentID)
        {
            return _pendingIDs
                //.Where(_ID => _ID.Equals(patient.MRN)==false)
                .Where(_ID => _ID.Equals(currentID) == false)
                .ToList();
        }

        private List<string> _pendingIDs = new List<string>();

        public Task<string> GetMRNAsync()
        {
            return Context.GetNewID()
                .AsTask()
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        return string.Empty;
                    }

                    _pendingIDs.Add(t.Result.Value);
                    return t.Result.Value;
                });
        }

        //public System.Threading.Tasks.Task<bool> ReleasePendingMRNAsync(string excludeID = "")
        //{
        //    return this.Context.UpdatePendingIDList(GetUnusedIDs(excludeID))
        //        .AsTask()
        //        .ContinueWith((t) =>
        //        {
        //            if (t.IsFaulted)
        //                return false;
        //            else
        //            {
        //                return t.Result.Value;
        //            }
        //        });
        //}

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Return TRUE if no server-side validation errors
        //
        // NOTE: async-server side functions coded to return true if the error condition exists
        ///////////////////////////////////////////////////////////////////////////////////////////////
        private object asyncValidationsLock = new object();
        private List<ValidationResult> asyncValidationResultList = new List<ValidationResult>();

        public Task<bool> ValidatePatientAddressAsync(PatientAddress patientAddress)
        {
            lock (asyncValidationsLock)
            {
                asyncValidationResultList.Clear();
            }
            //NOTE: async-server side functions coded to return true if the error condition exists

            var validateCountyTask = Context.CountySelected(patientAddress)
                .AsTask()
                .ContinueWith(t =>
                {
                    var operation = t.Result;
                    if (operation.HasError || !operation.Value)
                    {
                        lock (asyncValidationsLock)
                        {
                            asyncValidationResultList.Add(new ValidationResult("County is required.",
                                new[] { "County" }));
                        }
                    }

                    return t.Result.Value;
                });

            //wait for all async server calls to complete
            return System.Threading.Tasks.Task.Factory.ContinueWhenAll(
                new System.Threading.Tasks.Task[] { validateCountyTask },
                tasks =>
                {
                    if (tasks.Any(_t => _t.IsFaulted))
                    {
                        return false;
                    }

                    //Add cached errors to entity on the UI thread
                    asyncValidationResultList.ForEach(error => { patientAddress.ValidationErrors.Add(error); });
                    return validateCountyTask.Result;
                },
                System.Threading.CancellationToken.None,
                TaskContinuationOptions.None,
                Client.Utils.AsyncUtility.TaskScheduler);
        }

        public Task<bool> ValidatePatientAsync(Patient patient)
        {
            lock (asyncValidationsLock)
            {
                asyncValidationResultList.Clear();
            }

            //NOTE: async-server side functions coded to return true if the error condition exists
            var validateMRNTask = Context.MRNExists(
                    Virtuoso.Services.Authentication.WebContext.Current.User.TenantID,
                    patient.MRN,
                    patient.PatientKey)
                .AsTask()
                .ContinueWith(t =>
                {
                    var operation = t.Result;
                    if (!operation.HasError && operation.Value)
                    {
                        lock (asyncValidationsLock)
                        {
                            asyncValidationResultList.Add(new ValidationResult("Duplicate MRNs are not permitted.",
                                new[] { "MRN" }));
                        }
                    }

                    return t.Result.Value;
                });

            //NOTE: async-server side functions coded to return true if the error condition exists
            var validateSSNTask = Context.SSNExists(
                    Virtuoso.Services.Authentication.WebContext.Current.User.TenantID,
                    patient.SSN,
                    patient.PatientKey)
                .AsTask()
                .ContinueWith(t =>
                {
                    var operation = t.Result;
                    if (!operation.HasError && operation.Value)
                    {
                        lock (asyncValidationsLock)
                        {
                            asyncValidationResultList.Add(new ValidationResult("Duplicate SSNs are not permitted.",
                                new[] { "SSN" }));
                        }
                    }

                    return t.Result.Value;
                });

            //wait for all async server calls to complete
            return System.Threading.Tasks.Task.Factory.ContinueWhenAll(
                new System.Threading.Tasks.Task[] { validateMRNTask, validateSSNTask },
                tasks =>
                {
                    if (tasks.Any(_t => _t.IsFaulted))
                    {
                        return false;
                    }

                    //Add cached errors to entity on the UI thread
                    asyncValidationResultList.ForEach(error => { patient.ValidationErrors.Add(error); });
                    return !(validateMRNTask.Result || validateSSNTask.Result);
                },
                System.Threading.CancellationToken.None,
                TaskContinuationOptions.None,
                Client.Utils.AsyncUtility.TaskScheduler);
        }

        //KSM 03262014
        public void GetMelissaVerification(string Mode, string ServiceName, string FirstName, string LastName,
            string AddressLine1, string AddressLine2, string City, string State, string PostalCode)
        {
            var query = Context.GetAddressVerificationQuery(Mode, ServiceName, FirstName, LastName, AddressLine1,
                AddressLine2, City, State, PostalCode);

            Context.Load(
                query,
                LoadBehavior.RefreshCurrent,
                VerifyAddressLoaded,
                null);
        }

        public Task<IEnumerable<InsuranceVerificationRequest>> GenerateImmediateEligibilityCheckAsync(int TenantID,
            int PatientKey, int InsuranceKey, int PatientInsuranceKey, Guid CreatedBy)
        {
            var query = Context.GenerateImmediateEligibilityCheckQuery(TenantID, PatientKey, InsuranceKey,
                PatientInsuranceKey, CreatedBy);
            return DomainContextExtension.LoadAsync(Context, query); //return Context.LoadAsync(query);  
        }

        public Task<IEnumerable<InsuranceVerificationRequest>> GetInsuranceVerificationRequestsAsync(int TenantID)
        {
            var query = Context.GetInsuranceVerificationRequestsQuery(TenantID);
            return DomainContextExtension.LoadAsync(Context, query); //return Context.LoadAsync(query);  
        }

        public Task<IEnumerable<PatientInsurance>> ProcessInsuranceVerificationRequest(int TenantID,
            bool UpdatePatientInsurance, int InsuranceVerificationRequestKey, bool WasVerified, Guid UpdatedBy)
        {
            var query = Context.ProcessInsuranceVerificationRequestQuery(TenantID, UpdatePatientInsurance,
                InsuranceVerificationRequestKey, WasVerified, UpdatedBy);
            return DomainContextExtension.LoadAsync(Context, query);
        }


        public Task<IEnumerable<InsuranceVerificationRequest>> RefreshPatientInsuranceVerificationRequestAsync(
            int patientkey)
        {
            var query = Context.GetRefreshPatientInsuranceVerificationRequestQuery(patientkey);
            return DomainContextExtension.LoadAsync(Context, query, LoadBehavior.RefreshCurrent);

            //Dispatcher.BeginInvoke(() =>
            //{
            //    var query = Context.GetRefreshPatientInsuranceVerificationRequestQuery(patientkey);
            //    query.IncludeTotalCount = true;
            //    IsLoading = true;
            //    Context.Load<InsuranceVerificationRequest>(query, LoadBehavior.RefreshCurrent, RefreshPatientInsuranceVerificationRequestLoaded, null);
            //});
        }

        public Task<IEnumerable<PatientInsurance>> RefreshPatientInsuranceAsync(int patientkey)
        {
            var query = Context.GetRefreshPatientInsuranceForMaintQuery(patientkey);
            return DomainContextExtension.LoadAsync(Context, query, LoadBehavior.RefreshCurrent);

            //Dispatcher.BeginInvoke(() =>
            //{
            //    var query = Context.GetRefreshPatientInsuranceForMaintQuery(patientkey);

            //    query.IncludeTotalCount = true;

            //    IsLoading = true;

            //    Context.Load<PatientInsurance>(
            //        query,
            //        LoadBehavior.RefreshCurrent,
            //        RefreshPatientInsuranceLoaded,
            //        null);           
            //});
        }


        private void ReturnFromCall(InvokeOperation<int?> result)
        {
            // and submit to Vendor
            if (result != null)
            {
                if (result.Error == null)
                {
                    int id = result.Value.Value;

                    var test = new EligibilityChecks();
                    test.SubmitEligibilityCheck(id, s => { submit270Returned(s); });
                }
            }
        }

        private void submit270Returned(string result)
        {
            string msg;
            if (result == string.Empty)
            {
                msg = "Capturing 271 Response.";
            }
            else
            {
                msg = result;
            }

            Debug.WriteLine(msg);
            Messenger.Default.Send(true, "EligibilityStringChangeNeeded");
        }

        private void VerifyAddressLoaded(LoadOperation<AddressReturn> results)
        {
            HandleEntityResults(results, OnAddressReturnLoaded);
            IsLoading = false;
        }

        //public InsuranceVerificationRequest GetLastRequestSync(int insuranceKey, int patientKey)
        //{
        //	var r
        //		= Context.GetLastInsuranceVerificationRequestSync(insuranceKey, patientKey);

        //	return r;
        //}

        //public void GetLastRequest(int insuranceKey, int patientKey)
        //{
        //	var query 
        //		= Context.GetLastInsuranceVerificationRequestQuery(insuranceKey, patientKey);

        //	Context.Load<InsuranceVerificationRequest>(
        //		query,
        //		LoadBehavior.RefreshCurrent,
        //		InsuranceVerificationRequestLoaded, null);			
        //}

        //public void GetResultsOfCheck(int insuranceKey, int patientKey)
        //{
        //    var query
        //        = Context.GetResultInsuranceVerificationRequestQuery(insuranceKey, patientKey);

        //    Context.Load<InsuranceVerificationRequest>(
        //        query,
        //        LoadBehavior.RefreshCurrent,
        //        InsuranceVerificationRequestLoaded, null);
        //}

        private void InsuranceVerificationRequestLoaded(LoadOperation<InsuranceVerificationRequest> results)
        {
            if (results.HasError)
            {
                results.MarkErrorAsHandled();
            }
            else
            {
                HandleEntityResults(results, OnInsuranceVerificationRequestLoaded);
            }
        }

        public void getADFVendorResponse(int adfkey, object me)
        {
            var query = Context.GetADFVendorResponseQuery(adfkey);

            Context.Load(
                query,
                LoadBehavior.RefreshCurrent,
                ADFVendorResponseLoaded, me);
        }

        public void getADFVendorCount(int admissionKey)
        {
            if (EntityManager.Current.IsOnline)
            {
                var query = Context.GetADFVendorCountQuery(admissionKey);

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    ADFVendorCountLoaded, admissionKey);
            }
        }

        int RetryCount;

        private void ADFVendorCountLoaded(LoadOperation<ADFVendorResponse> results)
        {
            if (results.HasError)
            {
                results.MarkErrorAsHandled();

                if (results.Error.Message.Contains("NotFound") ||
                    results.Error.Message.Contains("HttpWebRequest_WebException_RemoteServer"))
                {
                    if (RetryCount < 2)
                    {
                        RetryCount++;
                        System.Threading.Thread.Sleep(1000 * 1);
                        var admissionKey =
                            (int)results.UserState; //var tmp = results.EntityQuery.Parameters.FirstOrDefault().Value;
                        getADFVendorCount(admissionKey);
                    }
                    else
                    {
                        NavigateCloseDialog d = new NavigateCloseDialog();
                        if (d != null)
                        {
                            d.Closed += (s, err) =>
                            {
                                RetryCount = 0;
                                var _s = (NavigateCloseDialog)s;
                                var ret = _s.DialogResult.GetValueOrDefault();
                                if (ret) //Retry
                                {
                                    var admissionKey =
                                        (int)results
                                            .UserState; //var tmp = results.EntityQuery.Parameters.FirstOrDefault().Value;
                                    getADFVendorCount(admissionKey);
                                }
                                else //Cancel
                                {
                                    ADFResponseEventArgs args = new ADFResponseEventArgs(ignoreEmptyResults: true);
                                    args.adfVendorResponses = new List<ADFVendorResponse>();
                                    RetryCount = 0;
                                    OnADFVendorCountLoaded(results.UserState, args);
                                }
                            };

                            d.NoVisible = true;
                            d.YesButton.Content = "Retry";
                            d.NoButton.Content = "Cancel";
                            d.YesButton.Width = double.NaN;
                            d.NoButton.Width = double.NaN;
                            d.Title = "Warning";
                            d.Width = double.NaN;
                            d.Height = double.NaN;
                            d.ErrorMessage =
                                string.Format("{0}Error retrieving vendor response count. Unable to contact server.{0}",
                                    Environment.NewLine);

                            if (IsOnline)
                            {
                                d.Show();
                            }
                        }
                    }
                }
            }
            else //if (results.HasError)
            {
                ADFResponseEventArgs args = new ADFResponseEventArgs();
                args.adfVendorResponses = results.Entities.ToList();
                RetryCount = 0;

                OnADFVendorCountLoaded(results.UserState, args);
            }
        }

        private void ADFVendorResponseLoaded(LoadOperation<ADFVendorResponse> results)
        {
            if (results.HasError)
            {
                results.MarkErrorAsHandled();

                if (results.Error.Message.Contains("NotFound") ||
                    results.Error.Message.Contains("HttpWebRequest_WebException_RemoteServer"))
                {
                    if (RetryCount < 2)
                    {
                        RetryCount++;
                        System.Threading.Thread.Sleep(1000 * 1);

                        var tmp = results.EntityQuery.Parameters.FirstOrDefault().Value;
                        getADFVendorResponse((int)tmp, null);
                    }
                    else
                    {
                        NavigateCloseDialog d = new NavigateCloseDialog();
                        if (d != null)
                        {
                            d.Closed += (s, err) =>
                            {
                                RetryCount = 0;
                                var _s = (NavigateCloseDialog)s;
                                var ret = _s.DialogResult.GetValueOrDefault();

                                if (ret) //Retry
                                {
                                    var tmp = results.EntityQuery.Parameters.FirstOrDefault().Value;
                                    getADFVendorResponse((int)tmp, null);
                                }
                            };

                            d.NoVisible = true;
                            d.YesButton.Content = "Retry";
                            d.NoButton.Content = "Cancel";
                            d.YesButton.Width = double.NaN;
                            d.NoButton.Width = double.NaN;
                            d.Title = "Warning";
                            d.Width = double.NaN;
                            d.Height = double.NaN;
                            d.ErrorMessage =
                                string.Format("{0}Error retrieving vendor response. Unable to contact server.{0}",
                                    Environment.NewLine);
                            d.Show();
                        }
                    }
                }
            }
            else
            {
                ADFResponseEventArgs args = new ADFResponseEventArgs();
                args.adfVendorResponses = results.Entities.ToList();
                RetryCount = 0;

                OnADFVendorResponseLoaded(results.UserState, args);
            }
        }

        public void RefreshCertCyclesAndPhysicians(int admissionKey, bool refresh_cert_cycles,
            bool refresh_admission_physician)
        {
            if (refresh_cert_cycles || refresh_admission_physician)
            {
                IsLoading = true;

                DomainContextLoadBatch batch =
                    new DomainContextLoadBatch(RefreshCertCyclesAndPhysicians_DataLoadComplete);

                if (refresh_cert_cycles)
                {
                    Context.RejectChanges();
                    Context.AdmissionCertifications.Clear();

                    batch.Add(Context.Load(Context.RefreshAndGetCertCyclesQuery(admissionKey),
                        LoadBehavior.RefreshCurrent, false));
                }

                if (refresh_admission_physician)
                {
                    batch.Add(Context.Load(Context.GetAdmissionPhysicianForAdmissionQuery(admissionKey),
                        LoadBehavior.RefreshCurrent, false));
                }
            }
        }

        private void RefreshCertCyclesAndPhysicians_DataLoadComplete(DomainContextLoadBatch batch)
        {
            List<Exception> LoadErrors = new List<Exception>();

            if (batch.FailedOperationCount > 0)
            {
                foreach (var fop in batch.FailedOperations)
                    if (fop.HasError)
                    {
                        LoadErrors.Add(fop.Error);
                    }
            }

            IsLoading = false;

            if (OnRefreshCertCyclesAndPhysiciansLoaded != null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    OnRefreshCertCyclesAndPhysiciansLoaded(this, new MultiErrorEventArgs(LoadErrors));
                });
            }
        }

        public void GetOrderTrackingGroupForOrdersTrackingAsync()
        {
            Dispatcher.BeginInvoke(() =>
            {
                var query = Context.GetOrderTrackingGroupForOrdersTrackingQuery();

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnOrderTrackingGroupsLoaded),
                    null);
            });
        }

        public event Action<InvokeOperation<byte[]>> OnGenerateBatchDocumentAsyncReturned;

        public void GenerateBatchDocumentAsync(int InterimOrderBatchKey, bool ReturnDocument)
        {
            //byte[] pdf = LoadPdf();
            Context.GenerateBatchDocument(InterimOrderBatchKey, ReturnDocument, DateTime.Now,
                OnGenerateBatchDocumentAsyncReturned, InterimOrderBatchKey);
        }

        public event Action<InvokeOperation<bool>> OnUpdateBatchStatusToSentAsyncReturned;

        public void UpdateBatchStatusToSentAsync(string InterimOrderBatchKeys)
        {
            Context.UpdateBatchStatusToSent(InterimOrderBatchKeys, DateTime.Now, OnUpdateBatchStatusToSentAsyncReturned,
                InterimOrderBatchKeys);
        }

        public void GetOrdersTrackingByTrackingGroupAsync(int? orderTrackingGroupKey, string serviceLineKeys)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.OrdersTrackingDisplays.Clear();
                Context.OrdersTrackings.Clear();
                Context.AdmissionCoverageInfos.Clear();
                Context.AdmissionCoverageInsuranceInfos.Clear();
                Context.ChangeHistoryInfos.Clear();
                Context.ChangeHistoryDetailInfos.Clear();
                Context.InterimOrderBatchInfos.Clear();
                Context.PatientInsuranceInfos.Clear();
                Context.PhysicianAddressInfos.Clear();

                var query = Context.GetOrdersTrackingByTrackingGroupQuery(orderTrackingGroupKey, serviceLineKeys);

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    OrdersTrackingLoaded,
                    null);
            });
        }

        public void GetOrdersTrackingChangeHistoryByKeyAsync(int orderTrackingGroupKey)
        {
            if (orderTrackingGroupKey == 0)
            {
                return;
            }

            Dispatcher.BeginInvoke(() =>
            {
                Context.ChangeHistories.Clear();
                Context.ChangeHistoryDetails.Clear();

                var query = Context.GetChangeHistoryByTableAndKeyQuery("OrdersTracking", orderTrackingGroupKey);

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    OrdersTrackingChangeHistoryLoaded,
                    null);
            });
        }

        public void GetOrdersTrackingDataAsync(bool GetVO, bool GetPOC, bool GetFaceToFace, bool GetCOTI,
            bool GetHospF2F)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.OrdersTrackingDatas.Clear();

                var query = Context.GetOrdersTrackingDataQuery(GetVO, GetPOC, GetFaceToFace, GetCOTI, GetHospF2F);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    TrackingDataLoaded,
                    null);
            });
        }

        public void GetSearchAsync(bool isSystemSearch)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.PatientSearches.Clear();

                //  <SearchItem field="PatientSearch" label="Patient" displayURL="Patient" destinationURL="/Maintenance/Patient" role="PatientEdit">
                //  <SearchField field="LastName" label="Last Name" type="TextBoxSearchField"></SearchField>
                //  <SearchField field="FirstName" label="First Name" type="TextBoxSearchField"></SearchField>
                //  <SearchField field="MiddleName" label="Middle Name" type="TextBoxSearchField"></SearchField>
                //  <SearchField field="Suffix" label="Suffix" type="TextBoxSearchField"></SearchField>
                //  <SearchField field="DOB" label="Date of Birth" type="DateSearchField"></SearchField>
                //  <SearchField field="MRN" label="MRN" type="TextBoxSearchField"></SearchField>
                //  <SearchField field="SSN" label="SSN" type="TextBoxSearchField"></SearchField>
                //  <SearchField field="Gender" label="Gender" type="CodeLookupSearchField" codeType="Gender"></SearchField>
                //  <SearchField field="Phone" label="Phone" type="TextBoxSearchField"></SearchField>
                //  <SearchField field="Status" label="Status" type="CodeLookupSearchField" codeType="AdmissionStatus"></SearchField>
                //  <SearchField field="ServiceLine" label="ServiceLine" type="ServiceLineSearchField"></SearchField>
                //</SearchItem>

                var lastName = GetSearchParameterValue("LastName");
                var firstName = GetSearchParameterValue("FirstName");
                var middleName = GetSearchParameterValue("MiddleName");
                var suffix = GetSearchParameterValue("Suffix");
                var mrn = GetSearchParameterValue("MRN");
                var ssn = GetSearchParameterValue("SSN");
                if (!String.IsNullOrEmpty(ssn))
                {
                    ssn = new string(ssn.Where(s => char.IsDigit(s)).ToArray());
                }

                var phone = GetSearchParameterValue("Phone");
                DateTime tryDOB;
                DateTime? birthDate = null;
                if (DateTime.TryParse(GetSearchParameterValue("DOB"), out tryDOB))
                {
                    birthDate = tryDOB;
                }

                int tryGender;
                int? gender = null;
                if (Int32.TryParse(GetSearchParameterValue("Gender"), out tryGender))
                {
                    gender = tryGender;
                }

                int tryStatus;
                int? status = null;
                if (Int32.TryParse(GetSearchParameterValue("Status"), out tryStatus))
                {
                    status = tryStatus;
                }

                int tryServiceLineKey;
                int? serviceLineKey = null;
                if (Int32.TryParse(GetSearchParameterValue("ServiceLineKey"), out tryServiceLineKey))
                {
                    serviceLineKey = tryServiceLineKey;
                }

                int tryServiceLineGroupingKey;
                int? serviceLineGroupingKey = null;
                if (Int32.TryParse(GetSearchParameterValue("ServiceLineGroupingKey"), out tryServiceLineGroupingKey))
                {
                    serviceLineGroupingKey = tryServiceLineGroupingKey;
                }

                var query = Context.GetPatientForSearchQuery(firstName, lastName, middleName, suffix, birthDate, mrn,
                    ssn, gender, phone, status, serviceLineKey, serviceLineGroupingKey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    SearchLoaded,
                    //new MetricsTimer(this.CorrelationID, Logging.Context.PatientSearch_GetSearchAsync));
                    new MetricsTimer(new StopWatchFactory(), CorrelationIDHelper.ID,
                        Logging.Context.PatientSearch_GetSearchAsync));
            });
        }

        private string GetSearchParameterValue(string paramName)
        {
            string ret = string.Empty;
            var param = SearchParameters.Where(i => i.Field.Equals(paramName)).FirstOrDefault();
            if (param != null)
            {
                ret = param.Value;
            }

            return ret;
        }

        public void RefreshInsuranceAsync()
        {
            Dispatcher.BeginInvoke(() => { }
            );
        }

        public void GetAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.Patients.Clear();

                int patientkey = -1;

                if (SearchParameters.Any())
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "PatientKey":
                                patientkey = Convert.ToInt32(searchvalue);
                                break;
                        }
                    }
                }

                var query = Context.GetPatientForMaintQuery(patientkey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    Loaded,
                    //new MetricsTimer(this.CorrelationID, Logging.Context.PatientService_GetAsync));
                    new MetricsTimer(new StopWatchFactory(), CorrelationIDHelper.ID,
                        Logging.Context.PatientService_GetAsync));
            });
        }

        public void GetPatientAdmissionFullDetailsAsync(int patientkey, int? admissionKey)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.Patients.Clear();

                var query = Context.GetPatientAdmissionFullDetailsQuery(patientkey, admissionKey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    PatientAdmissionFullDetailsLoaded,
                    null);
            });
        }

        public void GetPatientAdmissionAsync(int patientkey, int admissionKey)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.Patients.Clear();

                var query = Context.GetPatientAdmissionForMaintQuery(patientkey, admissionKey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    Loaded,
                    new MetricsTimer(new StopWatchFactory(), CorrelationIDHelper.ID,
                        Logging.Context.PatientService_GetPatientAdmissionAsync));
            });
        }

        public void GetTeamMeetingWorkListAsync(int? ServiceLineGroupingKey)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.Patients.Clear();
                Context.Admissions.Clear();
                Context.AdmissionTeamMeetings.Clear();

                var query = Context.GetTeamMeetingWorkListQuery(ServiceLineGroupingKey,
                    DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    GetTeamMeetingWorkListLoaded,
                    null);
            });
        }

        public void GetTeamMeetingRosterWorkListAsync()
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.TeamMeetingRosterPOCOs.Clear();
                var query = Context.GetTeamMeetingRosterWorkListQuery();
                query.IncludeTotalCount = true;
                IsLoading = true;
                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    GetTeamMeetingRosterWorkListLoaded,
                    null);
            });
        }

        public event Action<InvokeOperation<string>> SetTeamMeetingRosterSignatureDataReturned;

        public string SetTeamMeetingRosterSignatureData(int physicianKey, byte[] signatureData)
        {
            Context.SetTeamMeetingRosterSignatureData(physicianKey, signatureData,
                SetTeamMeetingRosterSignatureDataReturned, null);
            return null;
        }

        public void AdmissionPharmacyRefillClear()
        {
            Context.AdmissionPharmacyRefills.Clear();
        }

        public void AdmissionHospicePumpClear()
        {
            Context.AdmissionHospicePumps.Clear();
        }

        public void GetReportArchiveAsync()
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.ReportArchives.Clear();

                var query = Context.GetReportArchiveQuery();
                query.IncludeTotalCount = true;
                IsLoading = true;
                Context.Load(query, LoadBehavior.RefreshCurrent, GetReportArchiveLoaded, null);
            });
        }

        public void GetPatientsPharmacyRefillAsync(int? PatientKey, int? AdmissionKey, int? PatientMedicationKey,
            DateTime? DateFilled)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.Patients.Clear();
                Context.Admissions.Clear();
                Context.PatientMedications.Clear();
                Context.AdmissionPharmacyRefills.Clear();

                var query = Context.GetPatientsPharmacyRefillQuery(PatientKey, AdmissionKey, PatientMedicationKey,
                    DateFilled);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    GetPatientsPharmacyRefillLoaded,
                    null);
            });
        }

        public void GetPatientsHospicePumpAsync(int? PatientKey, int? AdmissionKey, int? PatientMedicationKey,
            DateTime? DateFilled)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.Patients.Clear();
                Context.Admissions.Clear();
                Context.PatientMedications.Clear();
                Context.AdmissionHospicePumps.Clear();

                var query = Context.GetPatientsHospicePumpQuery(PatientKey, AdmissionKey, PatientMedicationKey,
                    DateFilled);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    GetPatientsHospicePumpLoaded,
                    null);
            });
        }

        public void GetAdmissionPharmacyRefillForAdmissionAsync(int AdmissionKey)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.AdmissionPharmacyRefills.Clear();

                var query = Context.GetAdmissionPharmacyRefillForAdmissionQuery(AdmissionKey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    GetAdmissionPharmacyRefillForAdmissionLoaded,
                    null);
            });
        }

        public void GetAdmissionDiagnosisGroupsForAdmissionAsync(int AdmissionKey)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.AdmissionDiagnosisGroups.Clear();

                var query = Context.GetAdmissionDiagnosisGroupsForAdmissionQuery(AdmissionKey);

                query.IncludeTotalCount = true;
                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    GetAdmissionDiagnosisGroupsForAdmissionLoaded,
                    null);
            });
        }


        public void GetAdmissionHospicePumpForAdmissionAsync(int AdmissionKey)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.AdmissionHospicePumps.Clear();

                var query = Context.GetAdmissionHospicePumpForAdmissionQuery(AdmissionKey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    GetAdmissionHospicePumpForAdmissionLoaded,
                    null);
            });
        }

        public void GetHospiceRefillImportForImportAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.HospiceRefillImports.Clear();
                Context.HospiceRefillImportColumnLists.Clear();

                var query = Context.GetHospiceRefillImportForImportQuery();

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    GetHospiceRefillImportForImportLoaded,
                    null);
            });
        }

        public void GetAdmissionCareCoordinatorHistoryAsync(int admissionKey)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.AdmissionCareCoordinatorHistoryPOCOs.Clear();
                var query = Context.GetAdmissionCareCoordinatorHistoryQuery(admissionKey);
                query.IncludeTotalCount = true;
                IsLoading = true;
                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    GetAdmissionCareCoordinatorHistoryLoaded,
                    new MetricsTimer(new StopWatchFactory(), CorrelationIDHelper.ID,
                        Logging.Context.PatientService_GetAdmissionCareCoordinatorHistoryAsync));
            });
        }

        public void RefreshPatientAdmissionsAsync(int patientkey)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                var query = Context.GetRefreshPatientAdmissionsForMaintQuery(patientkey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    RefreshPatientAdmissionLoaded,
                    new MetricsTimer(new StopWatchFactory(), CorrelationIDHelper.ID,
                        Logging.Context.PatientService_RefreshPatientAdmissionsAsync));
            });
        }

        public void RefreshPatientAdmissionEncountersAndServicesAsync(int patientkey, int admissionKey,
            bool refreshServices)
        {
            Dispatcher.BeginInvoke(() =>
            {
                IsLoading = true;
                DomainContextLoadBatch batch = new DomainContextLoadBatch(RefreshAdmissionEncounterAndServicesLoaded);

                batch.Add(Context.Load(
                    Context.GetRefreshPatientAdmissionEncountersForMaintQuery(patientkey, admissionKey),
                    LoadBehavior.RefreshCurrent, false));
                if (refreshServices)
                {
                    batch.Add(Context.Load(Context.GetRefreshPatientAdmissionDisciplinesForMaintQuery(admissionKey),
                        LoadBehavior.RefreshCurrent, false));
                    batch.Add(Context.Load(
                        Context.GetRefreshPatientAdmissionDisciplineFrequenciesForMaintQuery(admissionKey),
                        LoadBehavior.RefreshCurrent, false));
                }
            });
        }

        public void RefreshPatientAdmissionEncountersAsync(int patientkey, int admissionKey)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                var query = Context.GetRefreshPatientAdmissionEncountersForMaintQuery(patientkey, admissionKey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    RefreshAdmissionEncounterLoaded,
                    null);
            });
        }

        //LJN
        //
        public void RefreshPatientAdmissionDocumentationsAsync(int patientkey, int admissionKey)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                var query = Context.GetRefreshPatientAdmissionDocumentationsForMaintQuery(patientkey, admissionKey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    RefreshAdmissionDocumentationsLoaded,
                    null);
            });
        }

        public void GeneratePatientPortalInvite(int patientkey, Guid createdBy)
        {
            Dispatcher.BeginInvoke(() =>
            {
                var query = Context.GeneratePatientPortalInviteQuery(patientkey, createdBy);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    PostPatientInviteSent,
                    null);
            });
        }

        private void PostPatientInviteSent(LoadOperation<Patient> results)
        {
            HandleEntityResults(results, OnPatientRefreshLoaded);
            IsLoading = false;

            foreach (Patient p in results.Entities.ToList()) p.TriggerPatientPortalChanges();
        }


        public void RefreshPatientAsync(int patientkey)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                var query = Context.GetRefreshPatientForMaintQuery(patientkey);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    RefreshPatientLoaded,
                    null);
            });
        }


        private EntityCollection<AdmissionDiagnosis> CurrentAdmissionDiagnosis { get; set; }

        //private CollectionViewSource _CurrentFilteredAdmissionDiagnosis = new CollectionViewSource(); //throws invalid cross thread access violation when created in background thread
        private CollectionViewSource _CurrentFilteredAdmissionDiagnosisBackingStore;

        private CollectionViewSource _CurrentFilteredAdmissionDiagnosis
        {
            get
            {
                if (_CurrentFilteredAdmissionDiagnosisBackingStore == null)
                {
                    _CurrentFilteredAdmissionDiagnosisBackingStore = new CollectionViewSource();
                }

                return _CurrentFilteredAdmissionDiagnosisBackingStore;
            }
        }

        public ICollectionView CurrentFilteredAdmissionDiagnosis => _CurrentFilteredAdmissionDiagnosis.View;

        private void ProcessFilteredAdmissionDiagnosisItems(EntityCollection<AdmissionDiagnosis> pd)
        {
            if (pd == CurrentAdmissionDiagnosis)
            {
                if (CurrentFilteredAdmissionDiagnosis != null)
                {
                    CurrentFilteredAdmissionDiagnosis.Refresh();
                    return;
                }
            }

            CurrentAdmissionDiagnosis = pd;
            if (CurrentAdmissionDiagnosis == null)
            {
                return;
            }

            _CurrentFilteredAdmissionDiagnosis.Source = CurrentAdmissionDiagnosis;
            CurrentFilteredAdmissionDiagnosis.Filter = FilterItems;
            CurrentFilteredAdmissionDiagnosis.Refresh();
        }

        private EntityCollection<PatientAllergy> CurrentPatientAllergy { get; set; }

        //private CollectionViewSource _CurrentFilteredPatientAllergy = new CollectionViewSource(); //throws invalid cross thread access violation when created in background thread
        private CollectionViewSource _CurrentFilteredPatientAllergyBackingStore;

        private CollectionViewSource _CurrentFilteredPatientAllergy
        {
            get
            {
                if (_CurrentFilteredPatientAllergyBackingStore == null)
                {
                    _CurrentFilteredPatientAllergyBackingStore = new CollectionViewSource();
                }

                return _CurrentFilteredPatientAllergyBackingStore;
            }
        }

        public ICollectionView CurrentFilteredPatientAllergy => _CurrentFilteredPatientAllergy.View;

        private void ProcessFilteredPatientAllergyItems(EntityCollection<PatientAllergy> pa)
        {
            if (pa == CurrentPatientAllergy)
            {
                if (CurrentFilteredPatientAllergy != null)
                {
                    CurrentFilteredPatientAllergy.Refresh();
                    return;
                }
            }

            CurrentPatientAllergy = pa;
            if (CurrentPatientAllergy == null)
            {
                return;
            }

            _CurrentFilteredPatientAllergy.Source = CurrentPatientAllergy;
            CurrentFilteredPatientAllergy.Filter = FilterItems;
            CurrentFilteredPatientAllergy.Refresh();
        }

        private bool FilterItems(object item)
        {
            AdmissionDiagnosis pd = item as AdmissionDiagnosis;
            if (pd != null)
            {
                // all current ICD9 diagnosis (across admissions)
                if (pd.RemovedDate != null)
                {
                    return false;
                }

                ////if (pd.Version != 9) return false;
                if (pd.DiagnosisEndDate != null)
                {
                    return false;
                }

                if (pd.Superceded)
                {
                    return false;
                }
            }

            PatientAllergy pa = item as PatientAllergy;
            if (pa != null)
            {
                if (pa.AllergyStatus != 0)
                {
                    return false;
                }

                if (pa.AllergyCodeKey == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(pa.Code))
                {
                    return false;
                }

                if (pa.Encounter == null)
                {
                    if (pa.Superceded)
                    {
                        return false;
                    }
                }
                else
                {
                    EncounterAllergy ea = pa.Encounter.EncounterAllergy
                        .Where(p => p.PatientAllergy.PatientAllergyKey == pa.PatientAllergyKey).FirstOrDefault();
                    if (ea == null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        private void TrackingDataLoaded(LoadOperation<OrdersTrackingData> results)
        {
            HandleEntityResults(results, OnTrackingDataLoaded);
            IsLoading = false;
        }

        private void OrdersTrackingLoaded(LoadOperation<OrdersTrackingDisplay> results)
        {
            HandleEntityResults(results, OnOrdersTrackingLoaded);
            IsLoading = false;
        }

        private void OrderTrackingGropusLoaded(LoadOperation<OrderTrackingGroup> results)
        {
            HandleEntityResults(results, OnOrderTrackingGroupsLoaded);
            IsLoading = false;
        }

        private void OrdersTrackingChangeHistoryLoaded(LoadOperation<ChangeHistory> results)
        {
            HandleEntityResults(results, OnOrdersTrackingChangeHistoryLoaded);
            IsLoading = false;
        }


        private void SearchLoaded(LoadOperation<PatientSearch> results)
        {
            HandleEntityResults(results, OnSearchLoaded);
            IsLoading = false;
        }

        private void Loaded(LoadOperation<Patient> results)
        {
            //check that PatientPhoto has rows...

            HandleEntityResults(results, OnLoaded);
            IsLoading = false;
        }

        private void RefreshPatientLoaded(LoadOperation<Patient> results)
        {
            //check that PatientPhoto has rows...

            HandleEntityResults(results, OnPatientRefreshLoaded);
            IsLoading = false;
        }

        //DS 0421
        private void PatientAdmissionFullDetailsLoaded(LoadOperation<Patient> results)
        {
            HandleEntityResults(results, OnPatientAdmissionFullDetailsLoaded);
            IsLoading = false;
        }

        private void PatientTeachingSheetLoaded(LoadOperation<PatientTeachingSheet> results)
        {
            HandleEntityResults(results, OnPatientTeachingSheetLoaded);
            IsLoading = false;
        }

        private void PatientScreeningLoaded(LoadOperation<PatientScreening> results)
        {
            HandleEntityResults(results, OnPatientScreeningLoaded);
            IsLoading = false;
        }

        private void GetTeamMeetingWorkListLoaded(LoadOperation<TeamMeetingPOCO> results)
        {
            HandleEntityResults(results, OnGetTeamMeetingWorkListLoaded);
            IsLoading = false;
        }

        private void GetTeamMeetingRosterWorkListLoaded(LoadOperation<TeamMeetingRosterPOCO> results)
        {
            HandleEntityResults(results, OnGetTeamMeetingRosterWorkListLoaded);
            IsLoading = false;
        }

        private void GetReportArchiveLoaded(LoadOperation<ReportArchive> results)
        {
            HandleEntityResults(results, OnGetReportArchiveLoaded);
            IsLoading = false;
        }

        private void GetPatientsPharmacyRefillLoaded(LoadOperation<Patient> results)
        {
            HandleEntityResults(results, OnGetPatientsPharmacyRefillLoaded);
            IsLoading = false;
        }

        private void GetPatientsHospicePumpLoaded(LoadOperation<Patient> results)
        {
            HandleEntityResults(results, OnGetPatientsHospicePumpLoaded);
            IsLoading = false;
        }

        private void GetAdmissionPharmacyRefillForAdmissionLoaded(LoadOperation<AdmissionPharmacyRefill> results)
        {
            HandleEntityResults(results, OnGetAdmissionPharmacyRefillForAdmissionLoaded);
            IsLoading = false;
        }

        private void GetAdmissionDiagnosisGroupsForAdmissionLoaded(LoadOperation<AdmissionDiagnosisGroup> results)
        {
            HandleEntityResults(results, OnGetAdmissionDiagnosisGroupsForAdmissionLoaded);
            IsLoading = false;
        }

        private void GetAdmissionHospicePumpForAdmissionLoaded(LoadOperation<AdmissionHospicePump> results)
        {
            HandleEntityResults(results, OnGetAdmissionHospicePumpForAdmissionLoaded);
            IsLoading = false;
        }

        private void GetHospiceRefillImportForImportLoaded(LoadOperation<Tenant> results)
        {
            HandleEntityResults(results, OnGetHospiceRefillImportForImportLoaded);
            IsLoading = false;
        }

        private void RefreshPatientInsuranceVerificationRequestLoaded(
            LoadOperation<InsuranceVerificationRequest> results)
        {
            HandleEntityResults(results, OnRefreshPatientInsuranceVerificationRequestLoaded);
            IsLoading = false;
        }

        private void RefreshAdmissionLoaded(LoadOperation<Admission> results)
        {
            //check that PatientPhoto has rows...

            HandleEntityResults(results, OnAdmissionRefreshLoaded);
            IsLoading = false;
        }

        private void GetAdmissionCareCoordinatorHistoryLoaded(
            LoadOperation<AdmissionCareCoordinatorHistoryPOCO> results)
        {
            HandleEntityResults(results, OnGetAdmissionCareCoordinatorHistoryLoaded);
            IsLoading = false;
        }

        private void RefreshPatientAdmissionLoaded(LoadOperation<Admission> results)
        {
            //check that PatientPhoto has rows...

            HandleEntityResults(results, OnPatientAdmissionRefreshLoaded);
            IsLoading = false;
        }

        private void RefreshPatientInsuranceLoaded(LoadOperation<PatientInsurance> results)
        {
            HandleEntityResults(results, OnPatientInsuranceRefreshLoaded);

            Messenger.Default.Send(0, "PatientInsuranceRefreshed");
            IsLoading = false;
        }

        private void RefreshAdmissionDocumentationsLoaded(LoadOperation<AdmissionDocumentation> results)
        {
            //check that PatientPhoto has rows...

            HandleEntityResults(results, OnAdmissionDocumentationRefreshLoaded);
            IsLoading = false;
        }

        private void RefreshAdmissionEncounterLoaded(LoadOperation<Encounter> results)
        {
            //check that PatientPhoto has rows...

            HandleEntityResults(results, OnAdmissionEncounterRefreshLoaded);
            IsLoading = false;
        }

        private void RefreshAdmissionEncounterAndServicesLoaded(DomainContextLoadBatch batch)
        {
            if (batch.FailedOperationCount > 0)
            {
                foreach (var fop in batch.FailedOperations)
                    if (fop.HasError)
                    {
                        MessageBox.Show(String.Format("GetAdmissionEncounterAndServicesAsync error: {0}",
                            fop.Error.Message));
                        fop.MarkErrorAsHandled();
                    }
            }

            if (OnAdmissionEncounterAndServicesRefreshLoaded != null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    OnAdmissionEncounterAndServicesRefreshLoaded(this, new BatchEventArgs(batch));
                });
            }

            IsLoading = false;
        }

        public void GetPDGMWorkListAsync(string insuranceKeys)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.PDGMWorkListPOCOs.Clear();

                var query = Context.GetPDGMWorkListQuery(insuranceKeys);

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    PDGMWorkListLoaded,
                    null);
            });
        }

        private void PDGMWorkListLoaded(LoadOperation<PDGMWorkListPOCO> results)
        {
            HandleEntityResults(results, OnPDGMWorkListLoaded);
            IsLoading = false;
        }

        public event EventHandler<EntityEventArgs<PDGMWorkListPOCO>> OnPDGMWorkListLoaded;

        public void GetDischargeTransferWorkListAsync()
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.DischargeTransferTasks.Clear();

                var query = Context.GetDischargeTransferTaskQuery();

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    DischargeTransferWorkListLoaded,
                    null);
            });
        }

        private void DischargeTransferWorkListLoaded(LoadOperation<DischargeTransferTask> results)
        {
            HandleEntityResults(results, OnDischargeTransferWorkListLoaded);
            IsLoading = false;
        }

        public event EventHandler<EntityEventArgs<DischargeTransferTask>> OnDischargeTransferWorkListLoaded;


        public IEnumerable<Patient> Items => Context.Patients;
        public EntitySet<AdmissionHospicePump> AdmissionHospicePumps => Context.AdmissionHospicePumps;
        public EntitySet<AdmissionPharmacyRefill> AdmissionPharmacyRefills => Context.AdmissionPharmacyRefills;
        public EntitySet<HospiceRefillImport> HospiceRefillImports => Context.HospiceRefillImports;

        public EntitySet<HospiceRefillImportColumnList> HospiceRefillImportColumnLists =>
            Context.HospiceRefillImportColumnLists;

        PagedEntityCollectionView<Patient> _Patients;

        public PagedEntityCollectionView<Patient> Patients
        {
            get { return _Patients; }
            set
            {
                if (_Patients != value)
                {
                    _Patients = value;
                    this.RaisePropertyChanged(p => p.Patients);
                }
            }
        }

        public event EventHandler<EntityEventArgs<OrdersTrackingData>> OnTrackingDataLoaded;
        public event EventHandler<EntityEventArgs<OrdersTrackingDisplay>> OnOrdersTrackingLoaded;
        public event EventHandler<EntityEventArgs<OrderTrackingGroup>> OnOrderTrackingGroupsLoaded;
        public event EventHandler<EntityEventArgs<ChangeHistory>> OnOrdersTrackingChangeHistoryLoaded;
        public event EventHandler<EntityEventArgs<PatientSearch>> OnSearchLoaded;
        public event EventHandler<EntityEventArgs<Patient>> OnLoaded;

        public event EventHandler<ErrorEventArgs> OnSaved;

        public bool SaveAllAsync()
        {
            try
            {
                // remove any child rows that were 'emptied' out or created and never populated.
                RemoveEmptyChildRows();

                //problem new entities - not returned in IsEditting...
                //add new insurance - don't enter phone, which is required
                //add new address - fill out everything, save - the open edit add on insurance isn't picked up by
                //prior code - also - the save fails but the address tab's Edit button is disabled...

                var shouldLog = false;
#if DEBUG
                shouldLog = true;
#endif
                var open_or_invalid = OpenOrInvalidObjects(Context, "PatientService", shouldLog);
                if (open_or_invalid) //TODO: should we raise/return an error or something???
                {
                    PendingSubmit = true;
                    return false;
                }

                PendingSubmit = false;

                //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
                //      thread after the current set of processing completes.
                //Dispatcher.BeginInvoke(() =>
                //{
                //Context.SubmitChanges(g => HandleErrorResults(g, OnSaved), null);
                Context.SubmitChanges(SubmitData, null);
                //});

                return true;
            }
            catch (Exception e)
            {
                string fullStackTrace = "Unknown exception";
                if (e != null)
                {
                    fullStackTrace = e.StackTrace;
                    // Account for nested exceptions
                    Exception innerException = e.InnerException;
                    while (innerException != null)
                    {
                        fullStackTrace += "\nCaused by: " + e.Message + "\n\n" + e.StackTrace;
                        innerException = innerException.InnerException;
                    }
                }

                MessageBox.Show(fullStackTrace, "PatientService.SaveAllAsync Exception", MessageBoxButton.OK);
                return false;
            }
        }

        private void SubmitData(SubmitOperation results)
        {
            //g => HandleErrorResults(g, OnSaved)
            HandleErrorResults(results, OnSaved);
        }

        public void RejectChanges()
        {
            Context.RejectChanges();
        }

        #endregion

        public bool ContextHasChanges => Context.HasChanges;

        void Context_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public void Cleanup()
        {
            Patients.Cleanup();

            if (Context != null)
            {
                Context.PropertyChanged -= Context_PropertyChanged;
                Context.EntityContainer.Clear();
            }

            try
            {
                EntityManager.Current.NetworkAvailabilityChanged -= Current_NetworkAvailabilityChanged;
            }
            catch (Exception)
            {
            }

            VirtuosoObjectCleanupHelper.CleanupAll(this);
            Context = null;
            //Context = null; //this may cause errors with DetailControlBase/ChildControlBase
        }

        private void RemoveEmptyChildRows()
        {
            int NumRows = Context.AdmissionFaceToFaceDiagnosis.Count;
            bool KeepLooking = true;
            AdmissionFaceToFaceDiagnosis af;
            while (NumRows > 0 && KeepLooking && Context.AdmissionFaceToFaceDiagnosis.Any())
            {
                af = Context.AdmissionFaceToFaceDiagnosis.LastOrDefault();
                if (af != null)
                {
                    KeepLooking = false;
                    if (String.IsNullOrEmpty(af.ICDCode) && af.ICDCodeKey <= 0)
                    {
                        Context.AdmissionFaceToFaceDiagnosis.Remove(af);
                        KeepLooking = true;
                    }
                }

                NumRows--;
            }


            List<AdmissionDocumentation> adi = Context.AdmissionDocumentations
                .Where(p => p.DocumentationFileName == null).ToList();

            foreach (AdmissionDocumentation add in adi) Context.AdmissionDocumentations.Remove(add);

            List<AdmissionCertification> adc = Context.AdmissionCertifications.Where(p => p.IsNew &&
                (p.PeriodStartDate == null
                 || p.PeriodEndDate == null
                 //|| p.PeriodNumber == null // cannot be null
                 //|| p.Duration == null     // cannot be null
                 //|| p.Units == null        // cannot be null
                 || p.Admission == null
                )
            ).ToList();
            foreach (AdmissionCertification ac in adc) Context.AdmissionCertifications.Remove(ac);
        }

        public event Action<InvokeOperation<string[]>> OnHospiceImportReadFirstRecordReturned;

        public void HospiceImportReadFirstRecord(byte[] importFile)
        {
            Context.HospiceImportReadFirstRecord(importFile, OnHospiceImportReadFirstRecordReturned, null);
        }

        public event Action<InvokeOperation<byte[]>> GetPatientChartPrintReturned;

        public byte[] GetPatientChartPrint(List<VirtuosoPrintRequestDetail> PRList)
        {
            Context.GetPatientChartPrint(PRList, GetPatientChartPrintReturned, null);
            return null;
        }

        public event Action<InvokeOperation<byte[]>> GetSSRSPDFDynamicFormReturned;

        public byte[] GetSSRSPDFDynamicForm(int formKey, int patientKey, int encounterKey, int admissionKey,
            bool HideOasisQuestions)
        {
            Context.GetSSRSPDFDynamicForm(formKey, patientKey, encounterKey, admissionKey, HideOasisQuestions,
                GetSSRSPDFDynamicFormReturned, null);
            return null;
        }

        public event Action<InvokeOperation<byte[]>> GetSSRSPDFAdmissionCommunicationReturned;

        public byte[] GetSSRSPDFAdmissionCommunication(int admissioncommunicationKey, int patientKey, int admissionKey)
        {
            Context.GetSSRSPDFAdmissionCommunication(admissioncommunicationKey, patientKey, admissionKey,
                GetSSRSPDFAdmissionCommunicationReturned, null);
            return null;
        }

        public event Action<InvokeOperation<byte[]>> GetSSRSPDFAdmissionDocumentationReturned;

        public byte[] GetSSRSPDFAdmissionDocumentation(int admissiondocumentationKey, int patientKey, int admissionKey)
        {
            Context.GetSSRSPDFAdmissionDocumentation(admissiondocumentationKey, patientKey, admissionKey,
                GetSSRSPDFAdmissionDocumentationReturned, null);
            return null;
        }

        public event Action<InvokeOperation<byte[]>> GetSSRSPDFReportWithParametersReturned;

        public byte[] GetSSRSPDFReportWithParameters(string ReportName, string Parameters)
        {
            Context.GetSSRSPDFReportWithParameters(ReportName, Parameters, GetSSRSPDFReportWithParametersReturned,
                null);
            return null;
        }

        public event Action<InvokeOperation<int>> OnHospiceImportImportReturned;

        public void HospiceImportImport(byte[] importFile)
        {
            Context.HospiceImportImport(importFile, OnHospiceImportImportReturned, null);
        }

        public void GetPatientWithPatientMessagesAsync(int PatientKey)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Context.Patients.Clear();
                Context.PatientMessages.Clear();

                var query = Context.GetPatientWithPatientMessagesQuery(PatientKey);

                query.IncludeTotalCount = false;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    GetPatientWithPatientMessages,
                    null);
            });
        }

        private void GetPatientWithPatientMessages(LoadOperation<Patient> results)
        {
            HandleEntityResults(results, OnPatientWithPatientMessagesLoaded);
            IsLoading = false;
        }

        public event EventHandler<EntityEventArgs<Patient>> OnPatientWithPatientMessagesLoaded;

        public void
            HandleError<T>(Task<T> _task, string tag) //where T : global::OpenRiaServices.DomainServices.Client.Entity
        {
            //http://msdn.microsoft.com/en-us/library/ff963549.aspx  section on Handling Exceptions
            //foreach (var e in _task.Exception.Flatten().InnerExceptions)    
            //    System.Diagnostics.Debug.WriteLine("Observed exception: " + e.Message);
            if (_task.IsFaulted)
            {
                if (_task.Exception is AggregateException)
                {
                    //_task.Exception
                    _task.Exception.Flatten()
                        .Handle(x =>
                        {
                            //var _webException = x.InnerException as System.Net.WebException;
                            //if (_webException != null)
                            //{
                            //    //Log it?
                            //    var msg = _webException.Message;
                            //    var innerException = _webException.InnerException;
                            //    var innerMsg = innerException != null ? innerException.Message : string.Empty;

                            //    var _responseUri = _webException.Response.ResponseUri;  // {http://local01.crescendoit.com:60000/Services/CrescendoMedispanServiceScreenings.svc}
                            //}

                            //var is_web_exception = x.InnerException.Message.Contains("The remote server returned an error: NotFound.");  //is_web_exception = true

                            //{System.Net.WebException: The remote server returned an error: NotFound. ---> System.Net.WebException: The remote server returned an error: NotFound.
                            //   at System.Net.Browser.BrowserHttpWebRequest.InternalEndGetResponse(IAsyncResult asyncResult)
                            //   at System.Net.Browser.BrowserHttpWebRequest.<>c__DisplayClassa.<EndGetResponse>b__9(Object sendState)
                            //   at System.Net.Browser.AsyncHelper.<>c__DisplayClass4.<BeginOnUI>b__0(Object sendState)
                            //   --- End of inner exception stack trace ---
                            //   at System.Net.Browser.AsyncHelper.BeginOnUI(SendOrPostCallback beginMethod, Object state)
                            //   at System.Net.Browser.BrowserHttpWebRequest.EndGetResponse(IAsyncResult asyncResult)
                            //   at System.ServiceModel.Channels.HttpChannelFactory.HttpRequestChannel.HttpChannelAsyncRequest.CompleteGetResponse(IAsyncResult result)}

                            if (x.InnerException != null)
                            {
                                ErrorWindow.CreateNew($"[000] {tag}",
                                    x.InnerException); // MessageBox.Show(String.Format("[000] {0} exception: {1}", tag, x.InnerException.Message));
                            }
                            else
                            {
                                ErrorWindow.CreateNew($"[001] {tag}",
                                    x); // MessageBox.Show(String.Format("[001] {0} exception: {1}", tag, x.Message));
                            }

                            return true; //exception was handled.
                        });
                }
                else if (_task.Exception != null)
                    //task.Exception.Flatten().Message
                {
                    ErrorWindow.CreateNew($"[002] {tag}",
                        _task.Exception); // MessageBox.Show(String.Format("[002] {0} exception: {1}", tag, _task.Exception.Message));
                }
            }
        }

        public bool OpenOrInvalidObjects(string tag = "", bool log = false)
        {
            return OpenOrInvalidObjects(Context, tag, log);
        }

        public Tuple<string, EntityChangeSet>[] CheckChanges()
        {
            EntityChangeSet changeSet1 = Context.EntityContainer.GetChanges();
            var _ret = new[]
            {
                new Tuple<string, EntityChangeSet>("Context", changeSet1),
            };
            return _ret;
        }
    }
}