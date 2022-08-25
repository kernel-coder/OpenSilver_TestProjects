#region Usings

using System.Collections.ObjectModel;

#endregion

namespace Virtuoso.Core.Utility
{
    public class vDayOfWeek
    {
        public int Day { get; set; }
        public string DayDescription { get; set; }

        public vDayOfWeek(int DayParm, string DescriptionParm)
        {
            Day = DayParm;
            DayDescription = DescriptionParm;
        }
    }

    public class DateHelper
    {
        readonly ObservableCollection<vDayOfWeek> _weekDays = new ObservableCollection<vDayOfWeek>();
        public ObservableCollection<vDayOfWeek> WeekDays => _weekDays;

        public DateHelper()
        {
            vDayOfWeek dw = new vDayOfWeek(1, "Sunday");
            _weekDays.Add(dw);
            dw = new vDayOfWeek(2, "Monday");
            _weekDays.Add(dw);
            dw = new vDayOfWeek(3, "Tuesday");
            _weekDays.Add(dw);
            dw = new vDayOfWeek(4, "Wednesday");
            _weekDays.Add(dw);
            dw = new vDayOfWeek(5, "Thursday");
            _weekDays.Add(dw);
            dw = new vDayOfWeek(6, "Friday");
            _weekDays.Add(dw);
            dw = new vDayOfWeek(7, "Saturday");
            _weekDays.Add(dw);
        }
    }
}