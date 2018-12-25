using System.Collections.Generic;
using System.Linq;

namespace Event_Log
{
    public class MonthEvents
    {
        public int Month { get; set; }
        public string MontText
        {
            get
            {
                switch (Month)
                {
                    case 1:
                        if (App.Eng)
                            return "January";
                        else
                            return "Январь";
                    case 2:
                        if (App.Eng)
                            return "February";
                        else
                            return "Февраль";
                    case 3:
                        if (App.Eng)
                            return "March";
                        else
                            return "Март";
                    case 4:
                        if (App.Eng)
                            return "April";
                        else
                            return "Апрель";
                    case 5:
                        if (App.Eng)
                            return "May";
                        else
                            return "Май";
                    case 6:
                        if (App.Eng)
                            return "June";
                        else
                            return "Июнь";
                    case 7:
                        if (App.Eng)
                            return "July";
                        else
                            return "Июль";
                    case 8:
                        if (App.Eng)
                            return "August";
                        else
                            return "Август";
                    case 9:
                        if (App.Eng)
                            return "September";
                        else
                            return "Сентябрь";
                    case 10:
                        if (App.Eng)
                            return "October";
                        else
                            return "Октябрь";
                    case 11:
                        if (App.Eng)
                            return "November";
                        else
                            return "Ноябрь";
                    case 12:
                        if (App.Eng)
                            return "December";
                        else
                            return "Декабрь";
                    default:
                        return "?";
                }
            }
        }
        public List<Event> Events = new List<Event>();

        public MonthEvents(Event evnt)
        {
            Month = evnt.Date.Month;
            Events.Add(evnt);
        }

        public void AddSorted(Event evnt)
        {
            Event last = Events.Last();
            if (evnt.IsPast)
                foreach (var e in Events)
                {
                    if (evnt.Date > e.Date)
                    {
                        Events.Insert(Events.IndexOf(e), evnt);
                        break;
                    }
                    else if (e == last)
                    {
                        Events.Add(evnt);
                        break;
                    }
                }
            else
                foreach (var e in Events)
                {
                    if (evnt.Date < e.Date)
                    {
                        Events.Insert(Events.IndexOf(e), evnt);
                        break;
                    }
                    else if (e == last)
                    {
                        Events.Add(evnt);
                        break;
                    }
                }
        }
    }
}