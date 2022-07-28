#region Usings

using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class TeamMeetingRosterPOCO
    {
        public string MRNAdmissionID => string.Format("{0} - {1}", MRN, AdmissionID);

        public string PhysicianName =>
            PhysicianCache.Current.GetPhysicianFullNameWithSuffixFromKey(MedDirectorPhysicianKey);

        public string TeamMeetingDateString
        {
            get
            {
                if (TeamMeetingDate.HasValue == false)
                {
                    return "?";
                }

                return TeamMeetingDate.Value.ToShortDateString().Trim();
            }
        }
    }
}