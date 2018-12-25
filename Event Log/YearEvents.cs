using System.Collections.Generic;
using System.Linq;

namespace Event_Log
{
    public class YearEvents
    {
        public int Year { get; set; }
        public int TotalEvents
        {
            get
            {
                int count = 0;
                foreach (var month in Months)
                    count += month.Events.Count;
                return count;
            }
        }
        public List<MonthEvents> Months = new List<MonthEvents>();

        public YearEvents(Event evnt)
        {
            Year = evnt.Date.Year;
            Months.Add(new MonthEvents(evnt));
        }

        public void AddSorted(Event evnt)
        {
            MonthEvents month = Months.Where(i => i.Month == evnt.Date.Month).FirstOrDefault();
            if (month == null)
            {
                MonthEvents last = Months.Last();
                foreach(var m in Months)
                {
                    if (evnt.Date.Month < m.Month)
                    {
                        Months.Insert(Months.IndexOf(m), new MonthEvents(evnt));
                        break;
                    }
                    else if (last == m)
                    {
                        Months.Add(new MonthEvents(evnt));
                        break;
                    }
                }
            }
            else
                month.AddSorted(evnt);
        }
    }
}