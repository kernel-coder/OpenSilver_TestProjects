using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace Virtuoso.Server.Data
{
    public partial class Physician
    {
        [Display(Name = "Full Name")]
        public string FullName
        {
            get
            {
                string name = string.Format("{0}, {1} {2}", this.LastName, this.FirstName, (!string.IsNullOrWhiteSpace(this.MiddleInitial)) ? this.MiddleInitial : "").Trim();
                if ((name.Trim() == ",") || (name == "")) name = " ";
                return name;
            }
        }
        public string FullNameWithSuffix
        {
            get
            {
                return (string.IsNullOrWhiteSpace(Suffix)) ? FullName : FullName + " " + this.Suffix.Trim();
            }
        }
        public string FormattedName
        {
            get
            {
                string formattedName = (!string.IsNullOrWhiteSpace(this.MiddleInitial)) ? this.FirstName + " " + this.MiddleInitial : this.FirstName;
                formattedName = formattedName.Trim() + " " + this.LastName.Trim() + " " + this.Suffix;
                return formattedName;
            }
        }

        public string FullNameInformal
        {
            get
            {
                string name = String.Format("{0} {1}", this.FirstName, this.LastName).Trim();
                if (name == null) return " ";
                if (name.Trim() == "") return " ";
                return name.Trim();
            }
        }
        public string FullNameInformalWithSuffix
        {
            get
            {
                return (string.IsNullOrWhiteSpace(Suffix)) ? FullNameInformal : FullNameInformal + " " + this.Suffix.Trim();
            }
        }
    }
}