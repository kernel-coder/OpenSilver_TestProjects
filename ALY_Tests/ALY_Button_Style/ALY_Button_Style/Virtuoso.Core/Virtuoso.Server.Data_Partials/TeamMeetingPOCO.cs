using System;

namespace Virtuoso.Server.Data
{
    public partial class TeamMeetingPOCO
    {
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
