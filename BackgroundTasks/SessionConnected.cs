using System;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Globalization;

namespace BackgroundTasks
{
    public sealed class SessionConnected : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var defferal = taskInstance.GetDeferral();
            StorageFile file;
            XmlDocument doc;
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync("data.xml");
            }
            catch
            {
                doc = new XmlDocument();
                doc.AppendChild(doc.CreateElement("root"));
                file = await ApplicationData.Current.LocalFolder.CreateFileAsync("data.xml");
                await doc.SaveToFileAsync(file);
                defferal.Complete();
                return;
            }
            doc = await XmlDocument.LoadFromFileAsync(file);
            var root = doc.FirstChild;
            DateTimeOffset last = DateTimeOffset.Parse(ApplicationData.Current.LocalSettings.Values["time"] as string,
                CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeLocal);
            string argument = "";
            foreach (var node in root.ChildNodes)
            {
                if (!string.IsNullOrEmpty(node.ChildNodes[8].InnerText))
                {
                    string[] arr = node.ChildNodes[2].InnerText.Split(' ');
                    DateTimeOffset alarm = DateTimeOffset.Parse($"{arr[0]} {node.ChildNodes[8].InnerText} {arr[2]}",
                        CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeLocal);
                    if (alarm < DateTimeOffset.Now)
                    {
                        if (alarm > last)
                            argument += node.ChildNodes[0].InnerText + "&";
                        else
                            node.ChildNodes[8].InnerText = "";
                    }
                }
            }
            await doc.SaveToFileAsync(file);

            if (!string.IsNullOrEmpty(argument))
            {
                ApplicationData.Current.LocalSettings.Values["missed"] = true;
                ApplicationData.Current.LocalSettings.Values["args"] = argument;
                bool eng = ApplicationData.Current.LocalSettings.Values["pl"] as string == "en-US";
                ToastContent content = new ToastContent
                {
                    Launch = argument,
                    Scenario = ToastScenario.Reminder,
                    Visual = new ToastVisual
                    {
                        BindingGeneric = new ToastBindingGeneric
                        {
                            Children =
                            {
                                new AdaptiveText
                                {
                                    Text= eng ? "You missed some notifications" : "Вы пропустили некоторые оповещения"
                                }
                            },
                            AppLogoOverride = new ToastGenericAppLogo
                            {
                                Source = "ms-appx:///assets/logo.png"
                            }
                        }
                    }
                };
                ToastNotificationManager.History.Clear();
                ToastNotification notification = new ToastNotification(content.GetXml());
                ToastNotificationManager.CreateToastNotifier().Show(notification);
            }
            ApplicationData.Current.LocalSettings.Values["time"] = DateTimeOffset.Now.Ticks;
            defferal.Complete();
        }
    }
}
