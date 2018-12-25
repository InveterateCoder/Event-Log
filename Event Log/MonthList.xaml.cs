using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Event_Log
{
    public sealed partial class MonthList : Page
    {
        private YearEvents Year;

        public MonthList(YearEvents evnt)
        {
            Year = evnt;
            this.InitializeComponent();
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MonthEvents sel = e.AddedItems.FirstOrDefault() as MonthEvents;

            if (sel != null)
            {
                App.State.Month = sel.Month;
                App.State.ActivePage = PageActive.Events;
                App.Cont.Content = new EventList(sel);
            }
        }

        private void Back_button_Clicked(object sender, RoutedEventArgs e)
        {
            App.State.ActivePage = PageActive.Years;
            App.Cont.Content = App.GetActivePage();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile" && e.NewSize.Width < 760.0)
                bBtn.Visibility = Visibility.Collapsed;
            else
                bBtn.Visibility = Visibility.Visible;
        }

        private void Title_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 0);
        }

        private void Title_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private void Title_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            App.State.ActivePage = PageActive.Years;
            App.Cont.Content = App.GetActivePage();
        }
    }
}