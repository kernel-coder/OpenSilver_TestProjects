using System;
using System.ComponentModel.DataAnnotations;

namespace Virtuoso.Server.Data
{
    public partial class PatientSearch
    {
        [Display(Name = "Formal Name")]
        public string FullName
        {
            get
            {
                string name = String.Format("{0}, {1} {2}", this.LastName, this.FirstName, !string.IsNullOrEmpty(this.MiddleName) ? this.MiddleName : "").Trim();
                if ((name == ",") || (name == "")) name = " ";
                return name;
            }
        }

        [Display(Name = "Formal Name With MRN")]
        public string FullNameWithMRN
        {
            get
            {
                string name = String.Format("{0}{1},{2}{3}{4}", 
                    ((this.LastName == null) ? "" : this.LastName.Trim()),
                    ((this.Suffix == null) ? "" : " " + this.Suffix.Trim()),
                    ((this.FirstName == null) ? "" : " " + this.FirstName.Trim()),
                    ((this.MiddleName == null) ? "" : " " + this.MiddleName.Trim()), 
                    ((this.MRN == null) ? "" : " - " + this.MRN.Trim())).Trim();
                if ((name == ",") || (name == "")) name = " ";
                if (name == "All,") name = "All";
                return name;
            }
        }

        [Display(Name = "Formal Name With Suffix")]
        public string FullNameWithSuffix
        {
            get
            {
                string name = String.Format("{0}{1},{2}{3}",
                    ((this.LastName == null) ? "" : this.LastName.Trim()),
                    ((this.Suffix == null) ? "" : " " + this.Suffix.Trim()),
                    ((this.FirstName == null) ? "" : " " + this.FirstName.Trim()),
                    ((this.MiddleName == null) ? "" : " " + this.MiddleName.Trim())).Trim();
                if ((name == ",") || (name == "")) name = " ";
                if (name == "All,") name = "All";
                return name;
            }
        }
    }
}
