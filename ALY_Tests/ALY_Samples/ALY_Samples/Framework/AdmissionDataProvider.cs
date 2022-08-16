#region Usings

using System.Collections.Generic;
using System.Linq;
using Virtuoso.Server.Data;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Framework
{
    public class AdmissionDataProvider : IAdmissionDataProvider
    {
        bool IAdmissionDataProvider.IsServer()
        {
            return false;
        }

        List<AdmissionPhysician> IAdmissionDataProvider.GetAdmissionPhysicians(Admission AdmissionParm,
            int AdmissionKey)
        {
            return AdmissionParm.AdmissionPhysician.ToList();
        }

        IQueryable<AdmissionDisciplineFrequency> IAdmissionDataProvider.GetAdmissionDisciplineFrequencys(
            Admission AdmissionParm, int AdmissionKey)
        {
            return AdmissionParm.AdmissionDisciplineFrequency.AsQueryable();
        }

        IQueryable<PatientAddress> IAdmissionDataProvider.GetPatientAddresses(Patient PatientParm, int PatientKey)
        {
            return PatientParm.PatientAddress.AsQueryable();
        }

        IQueryable<PatientInsurance> IAdmissionDataProvider.GetPatientInsurances(Admission AdmitParm, int PatientKey)
        {
            return AdmitParm.Patient.PatientInsurance.AsQueryable();
        }

        IQueryable<PatientInsurance> IAdmissionDataProvider.GetPatientInsurancesForPatient(Patient PatientParm,
            int PatientKey)
        {
            return PatientParm.PatientInsurance.AsQueryable();
        }

        IQueryable<AdmissionCoverage> IAdmissionDataProvider.GetAdmissionCoverage(Admission AdmitParm, int AdmissionKey)
        {
            return AdmitParm.AdmissionCoverage.AsQueryable();
        }

        IQueryable<AdmissionCertification> IAdmissionDataProvider.GetAdmissionCertification(Admission AdmitParm,
            int AdmissionKey)
        {
            return AdmitParm.AdmissionCertification.AsQueryable();
        }

        bool IAdmissionDataProvider.IsPDFValid(byte[] pdfDocParm)
        {
            return true;
        }
    }
}