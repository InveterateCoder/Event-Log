using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.System;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Event_Log
{
    sealed partial class App : Application
    {
        public static EventStorage Storage { get; set; }
        public static ContentState State { get; set; }
        public static bool DisableSelection { get; set; } = false;
        public static UserControl Cont { get; set; }
        public static Button AddBtn { get; set; }
        public static AutoSuggestBox SgstBox { get; set; }
        public static TextBlock PageHeader { get; set; }
        public static bool Mode { get; set; } = false;
        public static bool Eng { get; set; }
        public delegate void MenuSelect();
        public static MenuSelect MenuRefresh;

        private string bckSys, bckUsr, sett, dlgCancel, dlgRefreshBt, dlgRefresh;
        public App()
        {
            string lang = ApplicationData.Current.LocalSettings.Values["langIndex"] as string;
            if (lang == null || lang == "auto")
            {
                lang = Windows.System.UserProfile.GlobalizationPreferences.Languages.FirstOrDefault();
                if (lang.StartsWith("ru") || lang.StartsWith("hy") || lang.StartsWith("be") || lang.StartsWith("az")
                    || lang.StartsWith("ka") || lang.StartsWith("kk") || lang.StartsWith("uz") || lang.StartsWith("uk")
                    || lang.StartsWith("tg") || lang.StartsWith("tt"))
                {
                    lang = "ru-RU";
                    Eng = false;
                }
                else
                {
                    lang = "en-US";
                    Eng = true;
                }
            }
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = lang;
            ApplicationData.Current.LocalSettings.Values["pl"] = lang;
            this.InitializeComponent();
            this.Suspending += App_Suspending;
        }

        public static Page GetActivePage(Event evnt = null)
        {
            List<YearEvents> years;
            State state;
            if (State.MenuSelected == MenuSelection.Scheduled)
            {
                years = Storage.CallendarActive;
                state = State.ActiveEvents;
            }
            else
            {
                years = Storage.CallendarPast;
                state = State.PastEvents;
            }
            switch (state.Page)
            {
                case PageActive.Years:
                    return new YearList(years);
                case PageActive.Months:
                    YearEvents year = years.Where(i => i.Year == state.Year).FirstOrDefault();
                    if (year == null)
                    {
                        State.ActivePage = PageActive.Years;
                        return new YearList(years);
                    }
                    else
                        return new MonthList(year);
                case PageActive.Events:
                    YearEvents y = years.Where(i => i.Year == state.Year).FirstOrDefault();
                    if (y == null)
                    {
                        State.ActivePage = PageActive.Years;
                        return new YearList(years);
                    }
                    else
                    {
                        MonthEvents month = y.Months.Where(i => i.Month == state.Month).FirstOrDefault();
                        if (month == null)
                        {
                            State.ActivePage = PageActive.Months;
                            return new MonthList(y);
                        }
                        else
                            return evnt == null ? new EventList(month) : new EventList(month, evnt);
                    }
                default: return null;
            }
        }

        public async void GoHome()
        {
            ApplicationData.Current.LocalSettings.Values["time"] = DateTimeOffset.Now.ToString(CultureInfo.InvariantCulture.DateTimeFormat);
            if (Storage == null)
                Storage = await EventStorage.DeserializeAsync();
            if (State == null)
            {
                string state = ApplicationData.Current.LocalSettings.Values["state"] as string;
                if (state != null)
                    State = ContentState.Deserialize(state);
                else
                {
                    State = new ContentState();
                    State.MenuSelected = MenuSelection.Scheduled;
                    State.ActivePage = PageActive.Years;
                }
            }

            Window.Current.Content = new MainPage();
            Window.Current.Activate();

            if (await BackgroundAccessQuery())
            {
                if (!BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals("EventTrackerToastBackgroundTask")))
                {

                    BackgroundTaskBuilder builder = new BackgroundTaskBuilder()
                    {
                        Name = "EventTrackerToastBackgroundTask",
                        TaskEntryPoint = "BackgroundTasks.NotificationBckgndTask"
                    };
                    builder.SetTrigger(new ToastNotificationActionTrigger());
                    builder.Register();
                }
                if (!BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals("EventTrackerSessionStartTask")))
                {
                    BackgroundTaskBuilder builder = new BackgroundTaskBuilder()
                    {
                        Name = "EventTrackerSessionStartTask",
                        TaskEntryPoint = "BackgroundTasks.SessionConnected"
                    };
                    builder.SetTrigger(new SystemTrigger(SystemTriggerType.SessionConnected, false));
                    builder.Register();
                }
            }
        }

        private void App_Suspending(object sender, SuspendingEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["state"] = State.Serialize();
            ApplicationData.Current.LocalSettings.Values["time"] = DateTimeOffset.Now.ToString(CultureInfo.InvariantCulture.DateTimeFormat);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            ResourceLoader res = ResourceLoader.GetForCurrentView();
            bckSys = res.GetString("bckSys");
            bckUsr = res.GetString("bckUsr");
            sett = res.GetString("sett");
            dlgCancel = res.GetString("dlgCancel");
            dlgRefreshBt = res.GetString("dlgRefreshBt");
            dlgRefresh = res.GetString("dlgRefresh");
            object obj;
            bool missed;
            ApplicationData.Current.LocalSettings.Values.TryGetValue("missed", out obj);
            if (obj != null)
                missed = (bool)obj;
            else
                missed = false;
            if (missed)
            {
                ApplicationData.Current.LocalSettings.Values["missed"] = false;
                ToastNotificationManager.History.Clear();
                Window.Current.Content = new ToastForeground(ApplicationData.Current.LocalSettings.Values["args"] as string);
                Window.Current.Activate();
            }
            else
                GoHome();
        }

        private async Task<bool> BackgroundAccessQuery()
        {
            bool ret = true;
            try
            {
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                if (status == BackgroundAccessStatus.DeniedBySystemPolicy)
                {
                    await (new MessageDialog(bckSys)).ShowAsync();
                    ret = false;
                }
                else if (status == BackgroundAccessStatus.DeniedByUser)
                {
                    MessageDialog dialog = new MessageDialog(bckUsr);
                    dialog.Commands.Add(new UICommand(sett, async args =>
                    {
                        await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-backgroundapps"));
                        MessageDialog dlg = new MessageDialog(dlgRefresh);
                        dlg.Commands.Add(new UICommand(dlgRefreshBt, async ags => ret = await BackgroundAccessQuery()));
                        await dlg.ShowAsync();
                    }));
                    dialog.Commands.Add(new UICommand(dlgCancel, args => ret = false));
                    await dialog.ShowAsync();
                }
            }
            catch { ret = false; }
            return ret;
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            ApplicationData.Current.LocalSettings.Values["time"] = DateTimeOffset.Now.ToString(CultureInfo.InvariantCulture.DateTimeFormat);
            if (args.Kind == ActivationKind.ToastNotification)
            {
                var e = args as ToastNotificationActivatedEventArgs;
                ApplicationData.Current.LocalSettings.Values["missed"] = false;
                Window.Current.Content = new ToastForeground(e.Argument);
                Window.Current.Activate();
            }
        }

        public static XmlDocument FormNotification(string text1, string text2, string logoSource, string launch)
        {
            string minutes;
            string hour;
            string hours;
            string hours2;
            if (Eng)
            {
                minutes = "minutes";
                hour = "hour";
                hours = hours2 = "hours";
            }
            else
            {
                minutes = "минут";
                hour = "час";
                hours = "часа";
                hours2 = "часов";
            }

            ToastContent content = new ToastContent
            {
                Launch = launch,
                Scenario = ToastScenario.Alarm,
                Visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric
                    {
                        Children =
                        {
                            new AdaptiveText
                            {
                                Text = text1
                            },
                            new AdaptiveText
                            {
                                Text = text2
                            }
                        },
                        AppLogoOverride = new ToastGenericAppLogo
                        {
                            Source = logoSource
                        }
                    }
                },
                Actions = new ToastActionsCustom
                {
                    Buttons =
                    {
                        new ToastButton(Eng ? "Postpone" : "Отложить", $"action=postpone&text1={text1}&text2={text2}&logo={logoSource}")
                        {
                            ActivationType = ToastActivationType.Background
                        },
                        new ToastButton(Eng ? "Dismiss" : "Отклонить", "dismiss")
                        {
                            ActivationType = ToastActivationType.Background
                        }
                    },
                    Inputs =
                    {
                        new ToastSelectionBox("snoozeTime")
                        {
                            DefaultSelectionBoxItemId = "15",
                            Items =
                            {
                                new ToastSelectionBoxItem("15", "15 " + minutes),
                                new ToastSelectionBoxItem("30", "30 " + minutes),
                                new ToastSelectionBoxItem("60", "1 " + hour),
                                new ToastSelectionBoxItem("180", "3 " + hours),
                                new ToastSelectionBoxItem("300", "5 " + hours2)
                            }
                        }
                    }
                },
                Audio = new ToastAudio
                {
                    Src = new Uri("ms-winsoundevent:Notification.Looping.Alarm"),
                    Loop = true
                }
            };
            return content.GetXml();
        }
    }
}
