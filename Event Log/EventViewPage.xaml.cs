using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Input.Inking;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Event_Log
{
    public sealed partial class EventViewPage : Page
    {
        private Event _evnt;
        private string _prev_page_name;
        private StorageFile _video_file;
        private StorageFile _voice_file;
        private StorageFile _icon_file;
        private CameraCaptureUI _capture;
        private DateTimeOffset? alarmOffset;
        private bool _delete = false;
        private string titleEmpty, audioMissing, videoMissing, alarmMin, alarmYes, alarmNo, showOk, defIcon;
        private List<InkStroke> _strokes = new List<InkStroke>();
        public EventViewPage(Event evnt)
        {
            App.Mode = true;
            _prev_page_name = App.PageHeader.Text;
            _evnt = evnt;
            ResourceLoader res = ResourceLoader.GetForCurrentView();
            App.PageHeader.Text = res.GetString("addNew");
            titleEmpty = res.GetString("titleEmpty");
            audioMissing = res.GetString("audioMissing");
            videoMissing = res.GetString("videoMissing");
            alarmMin = res.GetString("alarmMin");
            alarmYes = res.GetString("alarmYes");
            alarmNo = res.GetString("alarmNo");
            showOk = res.GetString("showOk");
            defIcon = res.GetString("defIcon");
            this.InitializeComponent();
            timePicker.ClockIdentifier = Windows.System.UserProfile.GlobalizationPreferences.Clocks.FirstOrDefault();
            clndPicker.FirstDayOfWeek = Windows.System.UserProfile.GlobalizationPreferences.WeekStartsOn;
            _capture = new CameraCaptureUI();
            _capture.VideoSettings.Format = CameraCaptureUIVideoFormat.Mp4;
            _capture.VideoSettings.MaxDurationInSeconds = 600;
            RefreshPageData();
            this.Unloaded += EventViewPage_Unloaded;
        }

        private async void EventViewPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_delete)
                await _icon_file.DeleteAsync();
            if (_video_file != null)
                await _video_file.DeleteAsync();
            if (_voice_file != null)
                await _voice_file.DeleteAsync();
            App.DisableSelection = false;
            App.PageHeader.Text = _prev_page_name;
            App.AddBtn.IsEnabled = true;
            App.SgstBox.IsEnabled = true;
            App.Mode = false;
        }

        private void Up_Button_Clicked(object sender, RoutedEventArgs e)
            => App.Cont.Content = App.GetActivePage(_evnt);

        private async void Play_Link_Clicked(object sender, RoutedEventArgs e)
            => await Launcher.LaunchFileAsync(await StorageFile.GetFileFromApplicationUriAsync(_evnt.MediaMessageUri));
        
        private void RefreshPageData()
        {
            date.Text = _evnt.DateS;
            nameBlock.Text = _evnt.Name;
            BitmapImage image = new BitmapImage(_evnt.IconPath);
            icon.Source = image;
            if (_evnt.IsPast)
                notif.Visibility = Visibility.Collapsed;
            else if (_evnt.Alarm == null || _evnt.Alarm.Value < DateTimeOffset.Now)
            {
                notifstatus.Text = App.Eng ? "off" : "выкл";
                notif.Visibility = Visibility.Visible;
            }
            else
            {
                notifstatus.Text = App.Eng ? "on / " : "вкл / ";
                notif.Visibility = Visibility.Visible;
                if (Windows.System.UserProfile.GlobalizationPreferences.Clocks.FirstOrDefault() == "24HourClock")
                    notifstatus.Text += _evnt.Alarm.Value.TimeOfDay.ToString(@"hh\:mm");
                else
                {
                    var arr = _evnt.Alarm.ToString().Split(' ');
                    var arrtime = arr[1].Split(':');
                    notifstatus.Text += $"{arrtime[0]}:{arrtime[1]} {arr[2]}";
                }
            }

            if (string.IsNullOrEmpty(_evnt.Message) || string.IsNullOrWhiteSpace(_evnt.Message))
            {
                txtScroll.Visibility = Visibility.Collapsed;
                txtNone.Visibility = Visibility.Visible;
            }
            else
            {
                txtNone.Visibility = Visibility.Collapsed;
                txtMessage.Text = _evnt.Message;
                txtScroll.Visibility = Visibility.Visible;
            }
            switch (_evnt.MediaMessageType)
            {
                case MediaMessageType.None:
                    none.Visibility = Visibility.Visible;
                    playbtn.Visibility = Visibility.Collapsed;
                    break;
                case MediaMessageType.Video:
                    playbtn.Content = App.Eng ? "Video" : "Видео";
                    none.Visibility = Visibility.Collapsed;
                    playbtn.Visibility = Visibility.Visible;
                    break;
                case MediaMessageType.Voice:
                    playbtn.Content = App.Eng ? "Audio" : "Аудио";
                    none.Visibility = Visibility.Collapsed;
                    playbtn.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Edit_Clicked(object sender, RoutedEventArgs e)
        {
            if (edtBtn.IsChecked.Value)
            {
                App.DisableSelection = true;
                App.PageHeader.Text = App.Eng ? "Editing" : "Редактирование";
                App.AddBtn.IsEnabled = false;
                App.SgstBox.IsEnabled = false;
                nameBox.Text = _evnt.Name;
                notif.Visibility = Visibility.Visible;
                inkBord.BorderThickness = new Thickness(1);
                inkBord.Margin = new Thickness(0, 10, 0, 0);
                ink.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                    Windows.UI.Core.CoreInputDeviceTypes.Touch | Windows.UI.Core.CoreInputDeviceTypes.Pen;
                if (clndPicker.Date == _evnt.Date)
                {
                    if (_evnt.Alarm != null)
                    {
                        notiftgl.IsOn = true;
                        timePicker.Time = _evnt.Alarm.Value.TimeOfDay;
                    }
                }
                else
                    clndPicker.Date = _evnt.Date.Date;
                
                if (!string.IsNullOrEmpty(txtMessage.Text))
                    txtBox.Text = txtMessage.Text;

                switch (_evnt.MediaMessageType)
                {
                    case MediaMessageType.None:
                        rbtnNone.IsChecked = true;
                        break;
                    case MediaMessageType.Video:
                        vidPlay.Visibility = Visibility.Visible;
                        rbtnVideo.IsChecked = true;
                        break;
                    case MediaMessageType.Voice:
                        sndPlay.Visibility = Visibility.Visible;
                        rbtnAudio.IsChecked = true;
                        break;
                }

                bckBtn.Visibility = dltBtn.Visibility = nameBlock.Visibility = 
                    notifstatus.Visibility = txtScroll.Visibility = 
                    txtNone.Visibility = mediaInfo.Visibility = Visibility.Collapsed;

                acptBtn.Visibility = chngIcon.Visibility = nameBox.Visibility = 
                    clndPicker.Visibility = notiftgl.Visibility = txtBox.Visibility = 
                    mediaEdit.Visibility = del.Visibility = inkBord.Visibility = inkHead.Visibility = Visibility.Visible;
            }
            else
                Uncheck();
        }

        private async void Uncheck(bool edited = false)
        {
            if (edtBtn.IsChecked == true) edtBtn.IsChecked = false;
            App.DisableSelection = false;
            App.PageHeader.Text = _prev_page_name;
            App.AddBtn.IsEnabled = true;
            App.SgstBox.IsEnabled = true;
            notiftgl.IsOn = false;
            inkBord.BorderThickness = new Thickness(0);
            ink.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.None;
            inkBord.Margin = new Thickness(0, 30, 0, 0);
            if (_video_file != null)
            {
                await _video_file.DeleteAsync();
                _video_file = null;
            }

            if (_voice_file != null)
            {
                await _voice_file.DeleteAsync();
                _voice_file = null;
            }

            acptBtn.Visibility = chngIcon.Visibility = nameBox.Visibility =
                clndPicker.Visibility = notiftgl.Visibility = txtBox.Visibility =
                mediaEdit.Visibility = vidPlay.Visibility =
                sndPlay.Visibility = del.Visibility = inkHead.Visibility = Visibility.Collapsed;

            bckBtn.Visibility = dltBtn.Visibility = nameBlock.Visibility =
                notifstatus.Visibility = mediaInfo.Visibility = Visibility.Visible;

            if (edited)
            {
                if (_strokes.Count == 0)
                    inkBord.Visibility = Visibility.Collapsed;
                RefreshPageData();
            }
            else
            {
                ink.InkPresenter.StrokeContainer.Clear();
                if (_strokes.Count > 0)
                    foreach (var stroke in _strokes)
                        ink.InkPresenter.StrokeContainer.AddStroke(stroke.Clone());
                else
                    inkBord.Visibility = Visibility.Collapsed;
                if (_evnt.IsPast)
                    notif.Visibility = Visibility.Collapsed;
                if (!string.IsNullOrEmpty(_evnt.Message) || !string.IsNullOrWhiteSpace(_evnt.Message))
                    txtScroll.Visibility = Visibility.Visible;
                else txtNone.Visibility = Visibility.Visible;
                if (_icon_file != null)
                {
                    if (_delete)
                    {
                        _delete = false;
                        await _icon_file.DeleteAsync();
                    }
                    BitmapImage image = new BitmapImage();
                    icon.Source = image;
                    await image.SetSourceAsync(await (await StorageFile.GetFileFromApplicationUriAsync(_evnt.IconPath)).OpenReadAsync());
                    _icon_file = null;
                }
            }
        }

        private void Event_Date_Changed(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (args.NewDate.Value < DateTimeOffset.Now.Date)
            {
                notiftgl.IsOn = false;
                delChkBox.IsChecked = false;
                notiftgl.IsEnabled = delChkBox.IsEnabled = false;
            }
            else
            {
                delChkBox.IsChecked = !_evnt.Keep;
                notiftgl.IsEnabled = delChkBox.IsEnabled = true;
                if (_evnt.Alarm != null)
                {
                    timePicker.Time = _evnt.Alarm.Value.TimeOfDay;
                    notiftgl.IsOn = true;
                }
            }
        }

        private async void Accept_Clicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(nameBox.Text) || string.IsNullOrWhiteSpace(nameBox.Text))
                await ShowErrorMessage(titleEmpty);
            else if (rbtnAudio.IsChecked.Value && _voice_file == null && _evnt.MediaMessageType != MediaMessageType.Voice)
                await ShowErrorMessage(audioMissing);
            else if (rbtnVideo.IsChecked.Value && _video_file == null && _evnt.MediaMessageType != MediaMessageType.Video)
                await ShowErrorMessage(videoMissing);
            else if (alarmOffset == null && notiftgl.IsOn && clndPicker.Date.Value.Date + timePicker.Time <
                DateTimeOffset.Now.AddMinutes(15))
            {

                MessageDialog dialog = new MessageDialog(alarmMin);
                dialog.Commands.Add(new UICommand(alarmYes, async x =>
                {
                    alarmOffset = DateTimeOffset.Now.AddMinutes(15);
                    await Save();
                }));
                dialog.Commands.Add(new UICommand(alarmNo));
                await dialog.ShowAsync();
            }
            else
                await Save();
        }

        private async Task Save()
        {
            Event newEvnt = new Event(_evnt.Guid);
            newEvnt.Name = nameBox.Text;
            string name = newEvnt.Guid.ToString();
            if (clndPicker.Date.Value.Date == _evnt.Date.Date)
                newEvnt.Date = _evnt.Date;
            else
                newEvnt.Date = clndPicker.Date.Value.Date + DateTimeOffset.Now.TimeOfDay;
            
            if (_icon_file == null)
            {
                if (_evnt.IconPath.AbsoluteUri.StartsWith("ms-appx"))
                    newEvnt.IconPath = _evnt.IconPath;
                else
                    _icon_file = await (await StorageFile.GetFileFromApplicationUriAsync(_evnt.IconPath)).CopyAsync(ApplicationData.Current.TemporaryFolder);
            }
            newEvnt.Message = txtBox.Text;
            MediaMessageType type = rbtnNone.IsChecked.Value ? MediaMessageType.None : rbtnVideo.IsChecked.Value ? MediaMessageType.Video : MediaMessageType.Voice;
            newEvnt.MediaMessageType = type;
            if (type == MediaMessageType.Video)
            {
                if (_video_file == null)
                    _video_file = await (await StorageFile.GetFileFromApplicationUriAsync(_evnt.MediaMessageUri)).CopyAsync(ApplicationData.Current.TemporaryFolder);
            }
            else if (type == MediaMessageType.Voice)
                if (_voice_file == null)
                    _voice_file = await (await StorageFile.GetFileFromApplicationUriAsync(_evnt.MediaMessageUri)).CopyAsync(ApplicationData.Current.TemporaryFolder);

            Task task = App.Storage.RemoveAsync(_evnt);
            if (notiftgl.IsOn)
            {
                if (alarmOffset == null)
                    alarmOffset = clndPicker.Date.Value.Date + timePicker.Time;
                var xml = App.FormNotification(newEvnt.Name, notiftgl.IsOn ? newEvnt.Message : "", newEvnt.IconPath.AbsoluteUri, newEvnt.Guid.ToString());
                ScheduledToastNotification notification = new ScheduledToastNotification(xml, alarmOffset.Value);
                ToastNotificationManager.CreateToastNotifier().AddToSchedule(notification);
                newEvnt.Alarm = alarmOffset.Value;
            }
            await task;
            _evnt = newEvnt;

            if (ink.InkPresenter.StrokeContainer.GetStrokes().Count > 0)
            {
                _strokes.Clear();
                foreach (var stroke in ink.InkPresenter.StrokeContainer.GetStrokes())
                    _strokes.Add(stroke.Clone());
                _evnt.HasStroke = true;
                StorageFile file = await (await ApplicationData.Current.LocalFolder.
                    CreateFolderAsync("Inks", CreationCollisionOption.OpenIfExists)).CreateFileAsync(newEvnt.Guid.ToString() + ".gif", CreationCollisionOption.ReplaceExisting);
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (IOutputStream outStream = stream.GetOutputStreamAt(0))
                    {
                        await ink.InkPresenter.StrokeContainer.SaveAsync(outStream);
                        await outStream.FlushAsync();
                    }
                }
            }
            else
                _strokes.Clear();

            if (_icon_file != null)
            {
                _evnt.IconPath = await StoreFile("Icons", _icon_file);
                if (_delete)
                {
                    _delete = false;
                    await _icon_file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                _icon_file = null;
            }

            if (_video_file != null)
            {
                _evnt.MediaMessageUri = await StoreFile("Videos", _video_file);
                await _video_file.DeleteAsync();
                _video_file = null;
            }
            if (_voice_file != null)
            {
                _evnt.MediaMessageUri = await StoreFile("Audios", _voice_file);
                await _voice_file.DeleteAsync();
                _voice_file = null;
            }
            _evnt.Keep = !delChkBox.IsChecked.Value;
            task = _evnt.SaveToFileAsync();
            App.Storage.AddEvent(_evnt);
            Uncheck(true);
            await task;
            App.State.MenuSelected = _evnt.IsPast ? MenuSelection.Past : MenuSelection.Scheduled;
            App.State.Year = _evnt.Date.Year;
            App.State.Month = _evnt.Date.Month;
            App.State.ActivePage = PageActive.Events;
            App.MenuRefresh();
        }

        private async Task<Uri> StoreFile(string folder, StorageFile file)
        {
            string name = _evnt.Guid.ToString();
            var local = await file.CopyAsync(await ApplicationData.Current.LocalFolder.
                        CreateFolderAsync(folder, CreationCollisionOption.OpenIfExists),
                        name + file.FileType, NameCollisionOption.GenerateUniqueName);
            return new Uri($"ms-appdata:///local/{folder}/{local.Name}");
        }

        private async void Innk_Loaded(object sender, RoutedEventArgs e)
        {
            ink.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.None;
            InkDrawingAttributes attr = new InkDrawingAttributes();
            attr.Color = (Application.Current.Resources["InkToolbarAccentColorThemeBrush"] as SolidColorBrush).Color;
            attr.IgnorePressure = true;
            ink.InkPresenter.UpdateDefaultDrawingAttributes(attr);
            if (_evnt.HasStroke)
            {
                StorageFile file = await (await ApplicationData.Current.LocalFolder.
                    CreateFolderAsync("Inks", CreationCollisionOption.OpenIfExists)).GetFileAsync(_evnt.Guid.ToString() + ".gif");
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    using (IInputStream inputStream = stream.GetInputStreamAt(0))
                    {
                        await ink.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                    }
                }
                foreach (var stroke in ink.InkPresenter.StrokeContainer.GetStrokes())
                    _strokes.Add(stroke.Clone());
            }
            else
                inkBord.Visibility = Visibility.Collapsed;
        }

        private void RefreshCanvas_Clicked(object sender, RoutedEventArgs e)
        {
            ink.InkPresenter.StrokeContainer.Clear();
        }

        private async Task ShowErrorMessage(string message)
        {
            MessageDialog dialog = new MessageDialog(message);
            dialog.Commands.Add(new UICommand(showOk));
            await dialog.ShowAsync();
        }

        private async void Delete_Clicked(object sender, RoutedEventArgs e)
        {
            MessageDialog dialog = new MessageDialog(App.Eng ? "Delete record?" : "Удалить запись?");
            dialog.Commands.Add(new UICommand(alarmYes, async x =>
            {
                await App.Storage.RemoveAsync(_evnt);
                App.Cont.Content = App.GetActivePage();
            }));
            dialog.Commands.Add(new UICommand(alarmNo));
            await dialog.ShowAsync();
        }

        private async void Record_Video(object sender, RoutedEventArgs e)
        {
            vidPlay.Visibility = Visibility.Collapsed;
            if (_video_file != null)
                await _video_file.DeleteAsync();
            _video_file = await _capture.CaptureFileAsync(CameraCaptureUIMode.Video);
            if (_video_file != null)
                vidPlay.Visibility = Visibility.Visible;
        }

        private async void Record_Sound(object sender, RoutedEventArgs e)
        {
            sndPlay.Visibility = Visibility.Collapsed;
            if (_voice_file != null)
                await _voice_file.DeleteAsync();
            await (new AudioCaptureDialog()).ShowAsync();
            _voice_file = await ApplicationData.Current.TemporaryFolder.TryGetItemAsync("tempVoice.m4a") as StorageFile;
            if (_voice_file != null)
                sndPlay.Visibility = Visibility.Visible;
        }

        private async void Launch_Media(object sender, RoutedEventArgs e)
        {
            MediaMessageType type = rbtnNone.IsChecked.Value ? MediaMessageType.None : rbtnVideo.IsChecked.Value ? MediaMessageType.Video : MediaMessageType.Voice;
            switch (type)
            {
                case MediaMessageType.Video:
                    if (_video_file != null)
                        await Launcher.LaunchFileAsync(_video_file);
                    else
                        await Launcher.LaunchFileAsync(await StorageFile.GetFileFromApplicationUriAsync(_evnt.MediaMessageUri));
                    break;
                case MediaMessageType.Voice:
                    if (_voice_file != null)
                        await Launcher.LaunchFileAsync(_voice_file);
                    else
                        await Launcher.LaunchFileAsync(await StorageFile.GetFileFromApplicationUriAsync(_evnt.MediaMessageUri));
                    break;
            }
            
        }

        private async void ChangeIcon_Clicked(object sender, RoutedEventArgs e)
        {
            bool yes = false;
            if (_icon_file != null)
            {
                var dialog = new MessageDialog(defIcon);
                dialog.Commands.Add(new UICommand(alarmYes, arg => yes = true));
                dialog.Commands.Add(new UICommand(alarmNo));
                dialog.CancelCommandIndex = 1;
                await dialog.ShowAsync();
            }
            if (yes)
            {
                if (_delete)
                {
                    _delete = false;
                    await _icon_file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                BitmapImage image = new BitmapImage();
                icon.Source = image;
                await image.SetSourceAsync(await (await StorageFile.GetFileFromApplicationUriAsync(_evnt.IconPath)).OpenReadAsync());
                _icon_file = null;
            }
            else
            {
                if (_delete)
                {
                    _delete = false;
                    await _icon_file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                FileOpenPicker opener = new FileOpenPicker();
                opener.FileTypeFilter.Add(".png");
                opener.FileTypeFilter.Add(".jpg");
                opener.FileTypeFilter.Add(".ico");
                DateTimeOffset now = DateTimeOffset.Now;
                var file = await opener.PickSingleFileAsync();
                if (file != null)
                {
                    if (file.DateCreated > now)
                        _delete = true;
                    _icon_file = file;
                    BitmapImage image = new BitmapImage();
                    icon.Source = image;
                    await image.SetSourceAsync(await _icon_file.OpenReadAsync());
                }
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile" && e.NewSize.Width < 760.0)
                bBtn.Visibility = Visibility.Collapsed;
            else
                bBtn.Visibility = Visibility.Visible;
        }
    }
}