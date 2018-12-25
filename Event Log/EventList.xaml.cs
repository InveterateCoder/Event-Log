using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Event_Log
{
    public sealed partial class EventList : Page
    {
        private MonthEvents _montEvents;
        private int _year;
        private Event _evnt = null;
        public EventList(MonthEvents mnth)
        {
            Init(mnth);
        }

        public EventList(MonthEvents mnth, Event evnt)
        {
            Init(mnth);
            _evnt = evnt;
        }

        private void Init(MonthEvents mnth)
        {
            _montEvents = mnth;
            _year = mnth.Events[0].Date.Year;
            this.InitializeComponent();
            CVS.Source = _montEvents.Events.GroupBy(i => i.Date.Date, (key, list) => new GroupedListItem(key, list));
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1 && list.SelectionMode == ListViewSelectionMode.Single && e.AddedItems[0] is Event evnt)
                App.Cont.Content = new EventViewPage(evnt);
        }

        private void Back_button_Clicked(object sender, RoutedEventArgs e)
        {
            App.State.ActivePage = PageActive.Months;
            App.Cont.Content = App.GetActivePage();
        }

        private void List_Loaded(object sender, RoutedEventArgs e)
        {
            list.SelectedIndex = -1;
            list.SelectionChanged += List_SelectionChanged;
            list.IsRightTapEnabled = true;
            list.RightTapped += List_RightTapped;
            list.IsHoldingEnabled = true;
            list.Holding += List_Holding;
            if (_evnt != null)
                list.ScrollIntoView(_evnt, ScrollIntoViewAlignment.Leading);
        }

        private void List_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                Menu();
                e.Handled = true;
            }
        }

        private void List_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            Menu();
            e.Handled = true;
        }

        private void Menu()
        {
            list.SelectionMode = ListViewSelectionMode.Multiple;
            recs.Visibility = Visibility.Collapsed;
            cntrls.Visibility = Visibility.Visible;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile" && e.NewSize.Width < 760.0)
            {
                bBtn.IsEnabled = false;
                clmn.Width = new GridLength(0);
                stck.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                bBtn.IsEnabled = true;
                clmn.Width = new GridLength(1, GridUnitType.Star);
                stck.HorizontalAlignment = HorizontalAlignment.Center;
            }
        }

        private void TextBlock_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 0);
        }

        private void TextBlock_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private void Year_Clicked(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            App.State.ActivePage = PageActive.Years;
            App.Cont.Content = App.GetActivePage();
        }

        private void Month_Clicked(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            App.State.ActivePage = PageActive.Months;
            App.Cont.Content = App.GetActivePage();
        }

        private void Cancel_Clicked(object sender, RoutedEventArgs e)
        {
            cntrls.Visibility = Visibility.Collapsed;
            recs.Visibility = Visibility.Visible;
            list.SelectionMode = ListViewSelectionMode.Single;
        }

        private async void Delete_Clicked(object sender, RoutedEventArgs e)
        {
            if (list.SelectedItems.Count > 0)
            {
                cntrls.Visibility = Visibility.Collapsed;
                recs.Visibility = Visibility.Visible;
                List<Event> elist = new List<Event>();
                foreach (Event item in list.SelectedItems)
                    elist.Add(item);
                list.SelectionMode = ListViewSelectionMode.None;
                if (! await App.Storage.RemoveAsync(elist.ToArray()))
                {
                    CVS.Source = _montEvents.Events.GroupBy(i => i.Date.Date, (key, list) => new GroupedListItem(key, list));
                    list.SelectionMode = ListViewSelectionMode.Single;
                    list.SelectedIndex = -1;
                }
                else
                    App.Cont.Content = App.GetActivePage();
            }
        }
    }
}