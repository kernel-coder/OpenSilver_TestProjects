using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public interface IAdmissionDataProvider
    {
        //IsForm() - on client return true if used in dynamic form to trigger validation logic, everywhere else, client/server return false
        //bool IsForm();

        bool IsServer();
        List<AdmissionPhysician> GetAdmissionPhysicians(Admission AdmissionParm, int AdmissionKey);
        IQueryable<AdmissionDisciplineFrequency> GetAdmissionDisciplineFrequencys(Admission AdmissionParm, int AdmissionKey);
        IQueryable<PatientAddress> GetPatientAddresses(Patient PatientParm, int PatientKey);
        IQueryable<AdmissionCoverage> GetAdmissionCoverage(Admission AdmissionParm, int AdmissionKey);
        IQueryable<PatientInsurance> GetPatientInsurances(Admission AdmitParm, int PatientKey);
        IQueryable<PatientInsurance> GetPatientInsurancesForPatient(Patient PatientParm, int PatientKey);
        IQueryable<AdmissionCertification> GetAdmissionCertification(Admission AdmissionParm, int AdmissionKey);
        bool IsPDFValid(byte[] pdfDocParm);
    }
}
