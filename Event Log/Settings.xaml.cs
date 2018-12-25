using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Event_Log
{
    public sealed partial class Settings : Page
    {
        private byte[] code;
        private ToastNotifier _notifier;
        private bool _init = false;
        public Settings()
        {
            App.AddBtn.Visibility = Visibility.Collapsed;
            code = Encoding.ASCII.GetBytes("zxcvbnm,.lkjhgfyudsanqteruyhjdoiuytdjhgdueifjhdkslieqrowqeurhvla;;dfgpouqeytrdfk" +
                "tdjhgdueifjhdkslieqrowqeurhvla;;dfgpouqeytrdfkzxcvbnm,.lkjhgfyudsanqteruyhjdoiuy");
            _notifier = ToastNotificationManager.CreateToastNotifier();
            this.InitializeComponent();
            this.Unloaded += Settings_Unloaded;
        }

        private void Settings_Unloaded(object sender, RoutedEventArgs e)
        {
            App.AddBtn.Visibility = Visibility.Visible;
        }

        private async void GetLogFile(object sender, RoutedEventArgs e)
        {
            if (App.Storage.IsEmpty())
            {
                MessageDialog dialog = new MessageDialog(App.Eng ? "The log is empty. Cannot proceed." :
                    "Журнал пуст. Невозможно продолжить.");
                dialog.Commands.Add(new UICommand(App.Eng ? "Okay" : "Окей"));
                await dialog.ShowAsync();
                return;
            }
            progress.Visibility = Visibility.Visible;
            App.DisableSelection = true;
            App.SgstBox.IsEnabled = false;
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("Event Log", new List<string> { ".elog" });
            savePicker.SuggestedFileName = "log" + DateTimeOffset.Now.ToString().Split(' ')[0];
            var dest = await savePicker.PickSaveFileAsync();
            if (dest != null)
            {
                string path = ApplicationData.Current.TemporaryFolder.Path + "\\events.elog";
                await Task.Run(() => ZipFile.CreateFromDirectory(ApplicationData.Current.LocalFolder.Path, path));
                var file = await ApplicationData.Current.TemporaryFolder.GetFileAsync("events.elog");
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (DataReader reader = new DataReader(stream.GetInputStreamAt(0)))
                    {
                        await reader.LoadAsync((uint)code.Length);
                        byte[] coded = new byte[code.Length];
                        for (int i = 0; i < coded.Length; i++)
                            coded[i] = (byte)(reader.ReadByte() ^ code[i]);
                        using (DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0)))
                            writer.WriteBytes(coded);
                    }
                    
                    using(IRandomAccessStream destStream = await dest.OpenAsync(FileAccessMode.ReadWrite))
                        await RandomAccessStream.CopyAsync(stream, destStream);
                }
                await file.DeleteAsync();
            }
            App.DisableSelection = false;
            App.SgstBox.IsEnabled = true;
            progress.Visibility = Visibility.Collapsed;
        }

        private async void SetLogFile(object sender, RoutedEventArgs e)
        {
            progress.Visibility = Visibility.Visible;
            App.DisableSelection = true;
            App.SgstBox.IsEnabled = false;

            FileOpenPicker open = new FileOpenPicker();
            open.FileTypeFilter.Add(".elog");
            StorageFile file = await open.PickSingleFileAsync();
            if (file != null)
            {
                string text;
                if (add.IsChecked.Value)
                    text = App.Eng ? "Irrevocable operation. New records will be added to the current log." :
                        "Необратимая операция. Новые записи будут добавлены в текущий журнал.";
                else
                    text = App.Eng ? "Irrevocable operation. Current log will be erased." :
                        "Необратимая операция. Текущий журнал будет удален.";
                MessageDialog dialog = new MessageDialog(text);
                string proceed, cancel;
                if (App.Eng)
                {
                    proceed = "Proceed";
                    cancel = "Cancel";
                }
                else
                {
                    proceed = "Продолжить";
                    cancel = "Отмена";
                }
                bool consent = false;
                dialog.Commands.Add(new UICommand(proceed, x => consent = true));
                dialog.Commands.Add(new UICommand(cancel));
                await dialog.ShowAsync();
                if (!consent)
                {
                    App.DisableSelection = false;
                    App.SgstBox.IsEnabled = true;
                    progress.Visibility = Visibility.Collapsed;
                    return;
                }

                file = await file.CopyAsync(ApplicationData.Current.TemporaryFolder, "events.elog", NameCollisionOption.ReplaceExisting);

                using(IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (DataReader reader = new DataReader(stream.GetInputStreamAt(0)))
                    {
                        await reader.LoadAsync((uint)code.Length);
                        byte[] coded = new byte[code.Length];
                        for (int i = 0; i < coded.Length; i++)
                            coded[i] = (byte)(reader.ReadByte() ^ code[i]);
                        using (DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0)))
                            writer.WriteBytes(coded);
                    }
                }
                StorageFolder folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("temp", CreationCollisionOption.ReplaceExisting);
                try
                {
                    await Task.Run(() => ZipFile.ExtractToDirectory(file.Path, folder.Path));
                }
                catch
                {
                    await (new MessageDialog("Wrong file format!")).ShowAsync();
                    await file.DeleteAsync();
                    await folder.DeleteAsync();
                    App.DisableSelection = false;
                    App.SgstBox.IsEnabled = true;
                    progress.Visibility = Visibility.Collapsed;
                    return;
                }

                await file.DeleteAsync();

                if (add.IsChecked.Value)
                {
                    XmlDocument doc = await XmlDocument.LoadFromFileAsync(await folder.GetFileAsync("data.xml"));
                    IXmlNode root = doc.FirstChild;
                    StorageFolder Icons = await folder.TryGetItemAsync("Icons") as StorageFolder;
                    StorageFolder Videos = await folder.TryGetItemAsync("Videos") as StorageFolder;
                    StorageFolder Audios = await folder.TryGetItemAsync("Audios") as StorageFolder;
                    StorageFolder Inks = await folder.TryGetItemAsync("Inks") as StorageFolder;

                    StorageFolder locIcons = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Icons", CreationCollisionOption.OpenIfExists);
                    StorageFolder locVideos = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Videos", CreationCollisionOption.OpenIfExists);
                    StorageFolder locAudios = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Audios", CreationCollisionOption.OpenIfExists);
                    StorageFolder locInks = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Inks", CreationCollisionOption.OpenIfExists);

                    foreach (IXmlNode evntNode in root.ChildNodes)
                    {
                        Event evnt = new Event(evntNode);
                        if (evnt.IsPast && !evnt.Keep)
                            continue;
                        if (App.Storage.Exist(evnt.Guid))
                            continue;

                        if (evnt.IconPath.AbsoluteUri != "ms-appx:///Assets/item.png")
                        {
                            StorageFile iconFile = await Icons.GetFileAsync(Path.GetFileName(evnt.IconPath.AbsolutePath));
                            await iconFile.CopyAsync(locIcons);
                        }
                        switch (evnt.MediaMessageType)
                        {
                            case MediaMessageType.Video:
                                StorageFile videoFile = await Videos.GetFileAsync(Path.GetFileName(evnt.MediaMessageUri.AbsolutePath));
                                await videoFile.CopyAsync(locVideos);
                                break;
                            case MediaMessageType.Voice:
                                StorageFile audioFile = await Audios.GetFileAsync(Path.GetFileName(evnt.MediaMessageUri.AbsolutePath));
                                await audioFile.CopyAsync(locAudios);
                                break;
                        }
                        if (evnt.HasStroke)
                            await (await Inks.GetFileAsync(evnt.Guid.ToString() + ".gif")).CopyAsync(locInks);

                        if(!evnt.IsPast && evnt.Alarm != null)
                        {
                            var xml = App.FormNotification(evnt.Name, evnt.Message, evnt.IconPath.AbsoluteUri, evnt.Guid.ToString());

                            ScheduledToastNotification notification = new ScheduledToastNotification(xml, evnt.Alarm.Value);
                            _notifier.AddToSchedule(notification);
                        }
                        await evnt.SaveToFileAsync();
                        App.Storage.AddEvent(evnt);
                    }
                }
                else
                {
                    foreach (var item in await ApplicationData.Current.LocalFolder.GetItemsAsync())
                        await item.DeleteAsync();
                    var notifs = _notifier.GetScheduledToastNotifications();
                    foreach (var notif in notifs)
                        _notifier.RemoveFromSchedule(notif);

                    foreach (var item in await folder.GetItemsAsync())
                    {
                        if (item.IsOfType(StorageItemTypes.File))
                            await ((StorageFile)item).MoveAsync(ApplicationData.Current.LocalFolder);
                        else if (item.IsOfType(StorageItemTypes.Folder))
                        {
                            StorageFolder fld = (StorageFolder)item;
                            StorageFolder locfld = await ApplicationData.Current.LocalFolder.CreateFolderAsync(fld.DisplayName);
                            foreach (var itm in await fld.GetFilesAsync())
                                await itm.MoveAsync(locfld);
                        }
                    }
                    App.Storage = await EventStorage.DeserializeAsync();
                    foreach (var year in App.Storage.CallendarActive)
                        foreach (var mnth in year.Months)
                            foreach (var evnt in mnth.Events)
                                if (evnt.Alarm != null)
                                {
                                    var xml = App.FormNotification(evnt.Name, evnt.Message, evnt.IconPath.AbsoluteUri, evnt.Guid.ToString());

                                    ScheduledToastNotification notification = new ScheduledToastNotification(xml, evnt.Alarm.Value);
                                    _notifier.AddToSchedule(notification);
                                }
                }
                await folder.DeleteAsync();
            }
            App.DisableSelection = false;
            App.SgstBox.IsEnabled = true;
            progress.Visibility = Visibility.Collapsed;
        }

        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch(cmbBox.SelectedIndex)
            {
                case 0:
                    ApplicationData.Current.LocalSettings.Values["langIndex"] = "auto";
                    break;
                case 1:
                    ApplicationData.Current.LocalSettings.Values["langIndex"] = "en-US";
                    break;
                case 2:
                    ApplicationData.Current.LocalSettings.Values["langIndex"] = "ru-RU";
                    break;
            }
            if (_init)
            {
                MessageDialog dialog = new MessageDialog(App.Eng ? "Restart the application for the changes to take effect." :
                "Перезагрузите приложение, чтобы изменения вступили в силу.");
                dialog.Commands.Add(new UICommand(App.Eng ? "Okay" : "Окей"));
                await dialog.ShowAsync();
            }
            else
                _init = true;
        }

        private void CmbBox_Loaded(object sender, RoutedEventArgs e)
        {
            string lang = ApplicationData.Current.LocalSettings.Values["langIndex"] as string;
            if (App.Eng)
            {
                cmbBox.Items.Add("Auto");
                cmbBox.Items.Add("English");
                cmbBox.Items.Add("Russian");
            }
            else
            {
                cmbBox.Items.Add("Автоматически");
                cmbBox.Items.Add("Английский");
                cmbBox.Items.Add("Русский");
            }
            if (lang == null || lang == "auto")
                cmbBox.SelectedIndex = 0;
            else if (lang == "en-US")
                cmbBox.SelectedIndex = 1;
            else
                cmbBox.SelectedIndex = 2;
        }
    }
}
