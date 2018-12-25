using System;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Event_Log
{
    public enum MenuSelection { Scheduled, Past, Settings};
    public sealed partial class MainPage : Page
    {
        private SolidColorBrush _brushOver = Application.Current.Resources["InkToolbarAccentColorThemeBrush"] as SolidColorBrush;
        private bool _changeRequired = false;

        private ContentState ContState { get; set; }
        private MenuSelection SelectedPage
        {
            get => ContState.MenuSelected;
            set
            {
                ContState.MenuSelected = value;
                if (value == MenuSelection.Scheduled || value == MenuSelection.Past)
                    cont.Content = App.GetActivePage();
                else
                    cont.Content = new Settings();
            }
        }
        private bool _blocked = false;
        private Storyboard _storyboard;
        private bool _settings = false;
        private string _plannedE;
        private string _pastE;
        private string _sett;

        public MainPage()
        {
            ResourceLoader res = ResourceLoader.GetForCurrentView();
            _plannedE = res.GetString("plannedE");
            _pastE = res.GetString("pastE");
            _sett = res.GetString("sett");
            ContState = App.State;
            App.MenuRefresh = SelectMenu;
            this.InitializeComponent();
            App.AddBtn = addBtn;
            App.SgstBox = sgstBox;
            App.PageHeader = pageName;
            App.Cont = cont;
            StackPanel item;
            if (SelectedPage == MenuSelection.Settings)
                SelectedPage = MenuSelection.Scheduled;
            if (SelectedPage == MenuSelection.Scheduled)
            {
                item = panel.Children[3] as StackPanel;
                pageName.Text = _plannedE;
            }
            else
            {
                item = panel.Children[4] as StackPanel;
                pageName.Text = _pastE;
            }
            cont.Content = App.GetActivePage();
            (item.Children[0] as SymbolIcon).Foreground = _brushOver;
            (item.Children[1] as TextBlock).Foreground = _brushOver;
            SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;
            _storyboard = new Storyboard();
            DoubleAnimation opacityAnimation = new DoubleAnimation()
            {
                From = 1.0,
                To = 0.5,
                BeginTime = TimeSpan.Zero,
                Duration = new Duration(TimeSpan.FromMilliseconds(40))
            };
            Storyboard.SetTarget(opacityAnimation, sgstBox);
            Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
            _storyboard.Children.Add(opacityAnimation);
            _storyboard.AutoReverse = true;
        }

        

        private void MainPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (App.Mode)
            {
                e.Handled = true;
                cont.Content = App.GetActivePage();
            }
            else 
                if (App.State.GoBack())
                {
                    cont.Content = App.GetActivePage();
                    e.Handled = true;
                }
        }

        private void StackPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ((sender as StackPanel).Children[0] as FontIcon).Foreground = _brushOver;
            _changeRequired = true;

        }

        private void StackPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if(_changeRequired)
            {
                ((sender as StackPanel).Children[0] as FontIcon).Foreground = Foreground;
                _changeRequired = false;
                split.IsPaneOpen = !split.IsPaneOpen;
            }
        }

        private void StackPanel_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_changeRequired)
            {
                ((sender as StackPanel).Children[0] as FontIcon).Foreground = Foreground;
                _changeRequired = false;
            }
        }

        private void MenuItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            StackPanel panel = sender as StackPanel;
            _changeRequired = true;
            switch (panel.Tag)
            {
                case "F":
                    (panel.Children[0] as SymbolIcon).Foreground = _brushOver;
                    break;
                case "S":
                    if (SelectedPage == MenuSelection.Scheduled)
                        break;
                    (panel.Children[0] as SymbolIcon).Foreground = _brushOver;
                    (panel.Children[1] as TextBlock).Foreground = _brushOver;
                    break;
                case "P":
                    if (SelectedPage == MenuSelection.Past)
                        break;
                    (panel.Children[0] as SymbolIcon).Foreground = _brushOver;
                    (panel.Children[1] as TextBlock).Foreground = _brushOver;
                    break;
                case "T":
                    if (_settings)
                        break;
                    (panel.Children[0] as SymbolIcon).Foreground = _brushOver;
                    (panel.Children[1] as TextBlock).Foreground = _brushOver;
                    break;
            }
        }

        private void SelectMenu()
        {
            if (ContState.MenuSelected == MenuSelection.Settings)
                return;
            var pastSymb = past.Children[0] as SymbolIcon;
            var pastTxt = past.Children[1] as TextBlock;
            var planSymb = planned.Children[0] as SymbolIcon;
            var planTxt = planned.Children[1] as TextBlock;

            (settings.Children[0] as SymbolIcon).Foreground = Foreground;
            (settings.Children[1] as TextBlock).Foreground = Foreground;
            switch (ContState.MenuSelected)
            {
                case MenuSelection.Past:
                    pastSymb.Foreground = pastTxt.Foreground = _brushOver;
                    planSymb.Foreground = planTxt.Foreground = Foreground;
                    break;
                case MenuSelection.Scheduled:
                    planSymb.Foreground = planTxt.Foreground = _brushOver;
                    pastSymb.Foreground = pastTxt.Foreground = Foreground;
                    break;
            }
        }

        private void MenuItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            StackPanel panel = sender as StackPanel;
            if (!_blocked)
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
                panel.Background = new SolidColorBrush(Colors.Transparent);
            }
            else
                _blocked = false;
            if (_changeRequired)
            {
                _changeRequired = false;
                switch ((sender as FrameworkElement).Tag)
                {
                    case "F":
                        (panel.Children[0] as SymbolIcon).Foreground = Foreground;
                        break;
                    case "S":
                        if (SelectedPage == MenuSelection.Scheduled)
                            break;
                        (panel.Children[0] as SymbolIcon).Foreground = Foreground;
                        (panel.Children[1] as TextBlock).Foreground = Foreground;
                        break;
                    case "P":
                        if (SelectedPage == MenuSelection.Past)
                            break;
                        (panel.Children[0] as SymbolIcon).Foreground = Foreground;
                        (panel.Children[1] as TextBlock).Foreground = Foreground;
                        break;
                    case "T":
                        if (_settings)
                            break;
                        (panel.Children[0] as SymbolIcon).Foreground = Foreground;
                        (panel.Children[1] as TextBlock).Foreground = Foreground;
                        break;
                }
            }
        }

        private async void MenuItem_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (App.DisableSelection)
            {
                if(_changeRequired)
                {
                    _blocked = true;
                    MenuItem_PointerExited(sender, e);
                }
                return;
            }
            if (_changeRequired)
            {
                _changeRequired = false;
                StackPanel panel = sender as StackPanel;
                switch (panel.Tag)
                {
                    case "F":
                        (panel.Children[0] as SymbolIcon).Foreground = Foreground;
                        split.IsPaneOpen = true;
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => sgstBox.Focus(FocusState.Programmatic));
                        break;
                    case "S":
                        if (SelectedPage == MenuSelection.Scheduled)
                            break;
                        _settings = false;
                        switch(SelectedPage)
                        {
                            case MenuSelection.Past:
                                (past.Children[0] as SymbolIcon).Foreground = Foreground;
                                (past.Children[1] as TextBlock).Foreground = Foreground;
                                break;
                            case MenuSelection.Settings:
                                (settings.Children[0] as SymbolIcon).Foreground = Foreground;
                                (settings.Children[1] as TextBlock).Foreground = Foreground;
                                break;
                        }
                        pageName.Text = _plannedE;
                        SelectedPage = MenuSelection.Scheduled;
                        if (hmbgBtn.Visibility == Visibility.Visible)
                            split.IsPaneOpen = false;
                        break;
                    case "P":
                        if (SelectedPage == MenuSelection.Past)
                            break;
                        _settings = false;
                        switch (SelectedPage)
                        {
                            case MenuSelection.Scheduled:
                                (planned.Children[0] as SymbolIcon).Foreground = Foreground;
                                (planned.Children[1] as TextBlock).Foreground = Foreground;
                                break;
                            case MenuSelection.Settings:
                                (settings.Children[0] as SymbolIcon).Foreground = Foreground;
                                (settings.Children[1] as TextBlock).Foreground = Foreground;
                                break;
                        }
                        pageName.Text = _pastE;
                        SelectedPage = MenuSelection.Past;
                        if (hmbgBtn.Visibility == Visibility.Visible)
                            split.IsPaneOpen = false;
                        break;
                    case "T":
                        if (_settings)
                            break;
                        _settings = true;
                        switch (SelectedPage)
                        {
                            case MenuSelection.Scheduled:
                                (planned.Children[0] as SymbolIcon).Foreground = Foreground;
                                (planned.Children[1] as TextBlock).Foreground = Foreground;
                                break;
                            case MenuSelection.Past:
                                (past.Children[0] as SymbolIcon).Foreground = Foreground;
                                (past.Children[1] as TextBlock).Foreground = Foreground;
                                break;
                        }
                        pageName.Text = _sett;
                        SelectedPage = MenuSelection.Settings;
                        if (hmbgBtn.Visibility == Visibility.Visible)
                            split.IsPaneOpen = false;
                        break;
                }
            }
        }

        private void HmbgBtn_Click(object sender, RoutedEventArgs e) => split.IsPaneOpen = true;

        private void Add_Event_Clicked(object sender, RoutedEventArgs e)
        {
            cont.Content = new AddPage();
        }

        private void StackPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 0);
            StackPanel panel = sender as StackPanel;
            panel.Background = Resources["ButtonPointerOverBackgroundThemeBrush"] as SolidColorBrush;
        }

        private void AutoSgst_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                sender.ItemsSource = App.Storage.Names.Where(i => i.StartsWith(sender.Text, StringComparison.CurrentCultureIgnoreCase));
        }

        private void SgstBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var evnt = App.Storage.GetEvent(sgstBox.Text);
            if (evnt != null)
            {
                this.Focus(FocusState.Programmatic);
                sgstBox.Text = "";
                cont.Content = new EventViewPage(evnt);
                if (hmbgBtn.Visibility == Visibility.Visible)
                    split.IsPaneOpen = false;
            }
            else
                _storyboard.Begin();
        }
    }
}