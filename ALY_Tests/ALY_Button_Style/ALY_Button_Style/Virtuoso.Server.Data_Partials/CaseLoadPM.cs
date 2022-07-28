namespace Virtuoso.Server.Data
{
    public partial class CaseLoadPM : IClinicalKeys
    {
        public string FullNameInformal => PatientName;

        public string FullNameWithMRN
        {
            get
            {
                var name = string.Format("{0}{1}",
                    PatientName == null ? "" : PatientName.Trim(),
                    MRN == null ? "" : " - " + MRN.Trim()).Trim();
                if (name == "," || name == "")
                {
                    name = " ";
                }

                if (name == "All,")
                {
                    name = "All";
                }

                return name;
            }
        }

        public string AdmissionStatusAndDate
        {
            get
            {
                //NOTE: max length of AdmissionStatus text is 10 + a few spaces + 10 more spaces for date format = MM/DD/YYYY
                if (AdmissionStatusDate.HasValue)
                {
                    var leftSide = string.Format("{0,-10}", AdmissionStatus); //10 for status text
                    var ret = string.Format("{0} {1:MM/dd/yyyy}", leftSide, AdmissionStatusDate);

                    return ret;
                }

                return string.Format("{0,-23}", AdmissionStatus);
            }
        }

        public string LastVisitAndClinician
        {
            get
            {
                if (LastVisitDate.HasValue)
                {
                    var formattedDate = string.Format("{0:MM/dd/yyyy}", LastVisitDate);
                    return string.Format("{0,-12} {1}", formattedDate, LastVisitClinician);
                }

                return string.Empty;
            }
        }

        public string LastVisitDateForCurrentUser
        {
            get
            {
                if (LastVisitDateByUserID.HasValue)
                {
                    return string.Format("{0:MM/dd/yyyy}", LastVisitDateByUserID);
                }

                return string.Empty;
            }
        }

        int IClinicalKeys.PatientKey => PatientKey.GetValueOrDefault();

        int IClinicalKeys.AdmissionKey => AdmissionKey.GetValueOrDefault();
    }
}