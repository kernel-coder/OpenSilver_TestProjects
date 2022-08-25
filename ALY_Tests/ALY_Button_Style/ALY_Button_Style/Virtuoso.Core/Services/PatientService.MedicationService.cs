#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Medispan.Extensions;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Model;
using Virtuoso.Portable.Model;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public partial class PatientService : PagedModelBase, IPatientService
    {
        public async Task<PatientTeachingSheet> GetPatientTeachingSheetAsync(Patient patient, int MediSpanMedicationKey,
            string medicationName, int Language)
        {
            return await __ConnectedMediSpanTeachingSheet(patient, MediSpanMedicationKey, medicationName, Language);
        }

        public async Task<PatientScreening> GetPatientScreeningAsync(
            Patient patient,
            Encounter Encounter,
            List<PatientMedication> currentMedispanMedications,
            bool IncludeFutureMeds)
        {
            return await __ConnectedMediSpanScreening(patient, Encounter, currentMedispanMedications,
                IncludeFutureMeds);
        }

        private Task<PatientScreening> GeneratePatientScreeningTask(PatientScreening ps)
        {
            var taskCompletionSource = new TaskCompletionSource<PatientScreening>();
            taskCompletionSource.TrySetResult(ps);
            return taskCompletionSource.Task;
        }

        private Task<PatientTeachingSheet> GeneratePatientTeachingSheetTask(PatientTeachingSheet ts)
        {
            var taskCompletionSource = new TaskCompletionSource<PatientTeachingSheet>();
            taskCompletionSource.TrySetResult(ts);
            return taskCompletionSource.Task;
        }

        private async Task<PatientScreening> __ConnectedMediSpanScreening(Patient patient, Encounter Encounter,
            List<PatientMedication> currentMedispanMedications, bool IncludeFutureMeds)
        {
            var data = await __PrepareScreeningData(patient, Encounter, currentMedispanMedications);

            IsLoading = true;

            var result = await Context.GetMediSpanScreening(
                    TenantSettingsCache.Current.TenantSetting.Name,
                    IncludeFutureMeds,
                    patient?.PatientKey ?? 0,
                    ((patient == null) ? "Patient Unknown" : patient.FullNameInformal),
                    ((patient == null) ? "None" : patient.MRN),
                    (((patient.GenderCode == null) ? "M" : ((patient.GenderCode == "F") ? "F" : "M"))),
                    patient.BirthDate ?? DateTime.MinValue,
                    patient.ExpectedDueDate ?? DateTime.MinValue,
                    data.BodySurfaceArea,
                    data.WeightKG,
                    data.RenalFunction,
                    Guid.NewGuid(),
                    data.medications,
                    data.icd9s,
                    data.icd10s,
                    data.allergies
                ).AsTask()
                .ContinueWith(t =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        IsLoading = false; //VM watches for Service property changes...
                    });

                    return t.Result.Value;
                });

            return result;
        }

        private async Task<PatientScreeningIntermediateData> __PrepareScreeningData(Patient patient, Encounter Encounter, List<PatientMedication> currentMedispanMedications)
        {
            var screeningData = new PatientScreeningIntermediateData
            {
                medications = new string[currentMedispanMedications.Count]
            };
            int i = 0;

            foreach (PatientMedication pm in currentMedispanMedications)
            {
                CachedMediSpanMedication med = (pm.MediSpanMedicationKey == null)
                    ? null
                    : await MediSpanMedicationCache.Current.GetMediSpanMedicationByMedispanMedicationKey(
                        (int)pm.MediSpanMedicationKey);
                string DDID = (med == null) ? "" : ((med.DDID == null) ? "0" : med.DDID.ToString());
                string RDID = (med == null) ? "" : ((med.DDID != null) ? "0" : med.RDID.ToString());
                screeningData.medications[i] = string.Format(
                    "{0}|@|{1}|@|{2}|@|{3}|@|{4}|@|{5}|@|{6}|@|{7}|@|{8}|@|{9}|@|{10}|@|{11}|@|{12}|@|{13}|@|{14}",
                    DDID,
                    RDID,
                    pm.MedicationName,
                    pm.MedicationDosageAmount,
                    pm.MedicationDosageAmountTo ?? "",
                    pm.MedicationDosageUnitCode,
                    "",
                    pm.MedicationFrequencyNumber,
                    pm.MedicationFrequencyMax,
                    pm.MedicationFrequencyInterval,
                    pm.MedicationFrequencyUnit,
                    ((pm.AsNeeded) ? "Y" : "N"),
                    pm.MedicationRouteCode,
                    ((pm.MedicationStartDate == null) ? "" : ((DateTime)pm.MedicationStartDate).ToShortDateString()),
                    ((pm.MedicationEndDate == null) ? "" : ((DateTime)pm.MedicationEndDate).ToShortDateString()));
                i++;
            }

            screeningData.icd9s = new string[0];
            screeningData.icd10s = new string[0];
            if (patient != null)
            {
                ProcessFilteredAdmissionDiagnosisItems(patient.AdmissionDiagnosis);
                if (CurrentFilteredAdmissionDiagnosis != null)
                {
                    int j = 0;
                    int t = 0;
                    foreach (AdmissionDiagnosis pd in CurrentFilteredAdmissionDiagnosis)
                    {
                        if (pd.Version == 9)
                        {
                            j++;
                        }

                        if (pd.Version == 10)
                        {
                            t++;
                        }
                    }

                    screeningData.icd9s = new string[j];
                    screeningData.icd10s = new string[t];
                    j = 0;
                    t = 0;
                    foreach (AdmissionDiagnosis pd in CurrentFilteredAdmissionDiagnosis)
                    {
                        if (pd.Version == 9)
                        {
                            screeningData.icd9s[j++] = pd.Code;
                        }

                        if (pd.Version == 10)
                        {
                            screeningData.icd10s[t++] = pd.Code;
                        }
                    }
                }
            }

            screeningData.allergies = new string[0];
            if (patient != null)
            {
                ProcessFilteredPatientAllergyItems(patient.PatientAllergy);
                if (CurrentFilteredPatientAllergy != null)
                {
                    int j = 0;
                    foreach (PatientAllergy pa in CurrentFilteredPatientAllergy) j++;
                    screeningData.allergies = new string[j];
                    j = 0;
                    foreach (PatientAllergy pa in CurrentFilteredPatientAllergy)
                        screeningData.allergies[j++] =
                            string.Format("{0}|@|{1}",
                                pa.AllergyCodeKey.ToString(),
                                pa.ReactionCodes);
                }

                int? key = CodeLookupCache.GetKeyFromCode("Laboratory", "Creatinine");
                screeningData.RenalFunction = null;
                if (key != null)
                {
                    CodeLookup cl = CodeLookupCache.GetCodeLookupFromKey((int)key);
                    if ((cl != null) && (patient.PatientLab != null))
                    {
                        PatientLab patientLab = patient.PatientLab
                            .Where(p => p.HistoryKey == null && p.Result != null && p.ResultDate != null &&
                                        p.Test == cl.CodeDescription).OrderByDescending(p => p.ResultDate)
                            .FirstOrDefault();
                        if (patientLab != null)
                        {
                            screeningData.RenalFunction = patientLab.Result;
                        }
                    }
                }
            }

            screeningData.WeightKG = null;
            screeningData.BodySurfaceArea = 0;
            if (Encounter != null)
            {
                if (Encounter.WeightKG != null)
                {
                    screeningData.WeightKG = Encounter.WeightKG;
                }
                else
                {
                    var ew = Encounter.Admission.GetEncounterDataMostRecentWeight(false);
                    if (ew != null)
                    {
                        screeningData.WeightKG = ew.WeightKG;
                    }
                }

                if (Encounter.BodySurfaceArea != 0)
                {
                    screeningData.BodySurfaceArea = Encounter.BodySurfaceArea;
                }
                else
                {
                    var ew = Encounter.Admission.GetEncounterDataMostRecentWeight(true);
                    if (ew != null)
                    {
                        screeningData.BodySurfaceArea = ew.BSAValue.HasValue ? (double)ew.BSAValue.Value : 0;
                    }
                }
            }

            return screeningData;
        }

        private async Task<PatientTeachingSheet> __ConnectedMediSpanTeachingSheet(Patient patient,
            int MediSpanMedicationKey, string medicationName, int Language)
        {
            CachedMediSpanMedication med =
                await MediSpanMedicationCache.Current.GetMediSpanMedicationByMedispanMedicationKey(
                    MediSpanMedicationKey);
            if (med == null)
            {
                await GeneratePatientTeachingSheetTask(null);
            }

            IsLoading = true;

            var result = await Context.GetMediSpanTeachingSheet(
                    TenantSettingsCache.Current.TenantSetting.Name,
                    patient.PatientKey,
                    ((patient == null) ? "Patient Unknown" : patient.FullNameInformal),
                    ((patient == null) ? "None" : patient.MRN),
                    (((patient.GenderCode == null) ? "M" : ((patient.GenderCode == "F") ? "F" : "M"))),
                    Guid.NewGuid(),
                    med.DDID ?? 0,
                    ((med.DDID != null) ? 0 : med.RDID),
                    medicationName,
                    Language
                )
                .AsTask()
                .ContinueWith(t =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        IsLoading = false; //VM watches for Service property changes...
                    });
                    return t.Result.Value;
                });

            return result;
        }
    }
}