using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Virtuoso.Server.Data
{
    public static class TeamMeetingScheduleValidations
    {
        public static ValidationResult NoDuplicatedTeamSchedulesOnServiceLineGrouping
            (TeamMeetingSchedule teamMeetingSchedule, ValidationContext validationContext)
        {
            return ValidationResult.Success;
            // What is this?
           // return new ValidationResult("No duplicated Team schedule");
        }
    }
}
