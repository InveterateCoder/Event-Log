using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Globalization;

namespace BackgroundTasks
{
    public sealed class NotificationBckgndTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            ApplicationData.Current.LocalSettings.Values["time"] = DateTimeOffset.Now.ToString(CultureInfo.InvariantCulture.DateTimeFormat);
            if (taskInstance.Task.Name == "EventTrackerToastBackgroundTask")
            {
                var details = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;

                if (details != null)
                {
                    bool eng = ApplicationData.Current.LocalSettings.Values["pl"] as string == "en-US";
                    string minutes;
                    string hour;
                    string hours;
                    string hours2;
                    if (eng)
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

                    var userInput = details.UserInput;
                    var arguments = new Dictionary<string, string>();
                    foreach (var i in details.Argument.Split('&'))
                    {
                        var temp = i.Split('=');
                        arguments.Add(temp[0], temp[1]);
                    }
                    if (arguments["action"] == "postpone")
                    {
                        int input = int.Parse((string)userInput["snoozeTime"]);
                        ToastContent content = new ToastContent
                        {
                            Launch = arguments["text1"],
                            Scenario = ToastScenario.Alarm,
                            Visual = new ToastVisual
                            {
                                BindingGeneric = new ToastBindingGeneric
                                {
                                    Children =
                                    {
                                        new AdaptiveText
                                        {
                                            Text = arguments["text1"]
                                        },
                                        new AdaptiveText
                                        {
                                            Text = arguments["text2"]
                                        }
                                    },
                                    AppLogoOverride = new ToastGenericAppLogo
                                    {
                                        Source = arguments["logo"]
                                    }
                                }
                            },
                            Actions = new ToastActionsCustom
                            {
                                Buttons =
                                {
                                    new ToastButton(eng ? "Postpone" : "Отложить", details.Argument)
                                    {
                                        ActivationType = ToastActivationType.Background
                                    },
                                    new ToastButton(eng ? "Dismiss" : "Отклонить", "dismiss")
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
                        ScheduledToastNotification notif = new ScheduledToastNotification(content.GetXml(), DateTimeOffset.Now.AddMinutes(input));
                        ToastNotificationManager.CreateToastNotifier().AddToSchedule(notif);
                    }
                }
            }
        }
    }
}
