#region Usings

using System;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Utility;

#endregion

namespace Virtuoso.Core.Helpers
{
    public static class DateCalculationHelper
    {
        private static object _lock = new object();
        public static int DefaultTeamMeetingSpan = 14;

        public static DateTime? CalculateNextTeamMeetingDate(DateTime LastTeamMeetingDate, int ServiceLineGroupingKey,
            string LastName, bool EarliestPossible)
        {
            lock (_lock)
            {
                DateTime? newDT = null;
                DateCalculationHelperInternal newObj = new DateCalculationHelperInternal();
                newDT = newObj.CalculateNextTeamMeetingDate(LastTeamMeetingDate, ServiceLineGroupingKey, LastName,
                    EarliestPossible);
                return newDT;
            }
        }
    }

    public class DateCalculationHelperInternal
    {
        public DateTime? CalculateNextTeamMeetingDate(DateTime LastTeamMeetingDate, int ServiceLineGroupingKey,
            string LastName, bool EarliestPossible)
        {
            DateTime newDate = LastTeamMeetingDate;
            int defaultDays = EarliestPossible ? 0 : DateCalculationHelper.DefaultTeamMeetingSpan;
            var slg = ServiceLineCache.GetServiceLineGroupingFromKey(ServiceLineGroupingKey);
            if (slg == null)
            {
                return LastTeamMeetingDate.AddDays(defaultDays);
            }

            if (slg.TeamMeetingScheduleType_Daily)
            {
                newDate = newDate.AddDays(((EarliestPossible) ? 0 : 1));
            }
            else if (slg.TeamMeetingScheduleType_EveryOtherWeek)
            {
                newDate = newDate.AddDays(((EarliestPossible) ? 0 : 1));
                newDate = slg.NextTeamMeetingDate_EveryOtherWeek(newDate);
            }
            else if (slg.TeamMeetingScheduleType_Weekly)
            {
                DateHelper dh = new DateHelper();

                char LastNamechar = (string.IsNullOrWhiteSpace(LastName)) ? 'A' : LastName.Trim().ToUpper()[0];
                int LastNameAscii = Convert.ToInt32(LastNamechar);
                var Schedule = slg.TeamMeetingSchedule
                    .Where(ts => ts.StartLetterAscii <= LastNameAscii && ts.EndLetterAscii >= LastNameAscii)
                    .FirstOrDefault();
                if (Schedule != null)
                {
                    var WeekDay = Schedule.DayOfWeek;
                    newDate = LastTeamMeetingDate.AddDays(defaultDays);
                    var CalcWeekDay = dh.WeekDays
                        .Where(w => w.DayDescription.ToLower() == newDate.DayOfWeek.ToString().ToLower())
                        .FirstOrDefault().Day;

                    var DaysToAdd = WeekDay - CalcWeekDay;
                    if (DaysToAdd > 0 && defaultDays > 6)
                    {
                        // If > 1 then we need to offset by a week to ensure the next team meeting is within 15 days and not into a 3rd week
                        DaysToAdd = DaysToAdd - 7;
                    }

                    if (EarliestPossible)
                    {
                        // First try to get the earliest possible team meeting day this week.
                        var nextTeamDay = slg.TeamMeetingSchedule.Where(ts => ts.DayOfWeek >= CalcWeekDay)
                            .OrderBy(d => d.DayOfWeek).FirstOrDefault();

                        // If nothing left this week, try  next week.
                        if (nextTeamDay == null)
                        {
                            nextTeamDay = slg.TeamMeetingSchedule.Where(ts => ts.DayOfWeek < CalcWeekDay)
                                .OrderBy(d => d.DayOfWeek).FirstOrDefault();
                        }

                        if (nextTeamDay != null && CalcWeekDay != nextTeamDay.DayOfWeek)
                        {
                            DaysToAdd = CalcWeekDay > nextTeamDay.DayOfWeek
                                ? (7 - CalcWeekDay + nextTeamDay.DayOfWeek)
                                : (nextTeamDay.DayOfWeek - CalcWeekDay);
                        }
                        else if (nextTeamDay != null && CalcWeekDay == nextTeamDay.DayOfWeek && DaysToAdd < 0)
                        {
                            DaysToAdd = 0;
                        }
                    }

                    if (CalcWeekDay != WeekDay)
                    {
                        newDate = newDate.AddDays(DaysToAdd);
                    }
                }
                else
                {
                    newDate = newDate.AddDays(defaultDays);
                }
            }
            else
            {
                newDate = newDate.AddDays(defaultDays);
            }

            return newDate;
        }
    }
}