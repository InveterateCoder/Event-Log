using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Event_Log
{
    public sealed partial class YearList : Page
    {
        private List<YearEvents> _list;
        public YearList(List<YearEvents> llist)
        {
            _list = llist;
            this.InitializeComponent();
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            YearEvents sel = e.AddedItems.FirstOrDefault() as YearEvents;

            if (sel != null)
            {
                App.State.Year = sel.Year;
                App.State.ActivePage = PageActive.Months;
                App.Cont.Content = new MonthList(sel);
            }
        }
    }
}