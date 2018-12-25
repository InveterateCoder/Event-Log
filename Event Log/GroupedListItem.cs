using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Event_Log
{
    internal class GroupedListItem : IGrouping<string, Event>
    {
        private IEnumerable<Event> _events;

        public string Key { get; }

        public string Key2 { get; }

        public string Counter { get; }

        public GroupedListItem(DateTime key, IEnumerable<Event> events)
        {
            Key = key.Day.ToString();
            switch (key.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    Key2 = App.Eng ? "Monday" : "Понедельник";
                    break;
                case DayOfWeek.Tuesday:
                    Key2 = App.Eng ? "Tuesday" : "Вторник";
                    break;
                case DayOfWeek.Wednesday:
                    Key2 = App.Eng ? "Wednesday" : "Среда";
                    break;
                case DayOfWeek.Thursday:
                    Key2 = App.Eng ? "Thursday" : "Четверг";
                    break;
                case DayOfWeek.Friday:
                    Key2 = App.Eng ? "Friday" : "Пятница";
                    break;
                case DayOfWeek.Saturday:
                    Key2 = App.Eng ? "Saturday" : "Суббота";
                    break;
                case DayOfWeek.Sunday:
                    Key2 = App.Eng ? "Sunday" : "Воскресенье";
                    break;
            }
            double days = key.Date.Subtract(DateTime.Now.Date).TotalDays;
            Counter = days < 0 ? "-" : "";

            switch (days)
            {
                case -1:
                    Counter = App.Eng ? "Yesterday" : "Вчера";
                    break;
                case 0:
                    Counter = App.Eng ? "Today" : "Сегодня";
                    break;
                case 1:
                    Counter = App.Eng ? "Tomorrow" : "Завтра";
                    break;
                default:
                    double d = days < 0 ? days * -1 : days;
                    Counter += d;
                    if (App.Eng)
                        Counter += " days ";
                    else
                    {
                        var remnant = d % 10;
                        if (remnant == 1 && d > 20)
                            Counter += " день";
                        else
                            if (remnant > 1 && remnant < 5 && (d > 20 || d < 5))
                                Counter += " дня";
                        else
                            Counter += " дней";
                    }
                    break;
            }
            _events = events;
        }

        public IEnumerator<Event> GetEnumerator() => _events.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _events.GetEnumerator();
    }
}