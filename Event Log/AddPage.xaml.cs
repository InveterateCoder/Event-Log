using System;
using System.Linq;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Media;
using Windows.UI.ViewManagement;
using Windows.System;
using Windows.Storage.Streams;

namespace Event_Log
{

    public sealed partial class AddPage : Page
    {
        private CameraCaptureUI _capture;
        private StorageFile _icon_file;
        private StorageFile _video_file;
        private StorageFile _audio_file;
        private string _prevPageName;
        private DateTimeOffset? alarmOffset;
        private bool _delete = false;
        private string titleEmpty, audioMissing, videoMissing, alarmMin, alarmYes, alarmNo, showOk, defIcon;
        public AddPage()
        {
            App.DisableSelection = true;
            App.Mode = true;
            _prevPageName = App.PageHeader.Text;
            App.AddBtn.Visibility = Visibility.Collapsed;
            App.SgstBox.IsEnabled = false;
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
            clndPicker.Date = DateTimeOffset.Now;
            _capture = new CameraCaptureUI();
            _capture.VideoSettings.Format = CameraCaptureUIVideoFormat.Mp4;
            _capture.VideoSettings.MaxDurationInSeconds = 600;
            this.Unloaded += AddPage_Unloaded;
            UISettings uISettings = new UISettings();
        }

        private async void AddPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_icon_file != null && _delete)
                await _icon_file.DeleteAsync();
            if (_video_file != null)
                await _video_file.DeleteAsync();
            if (_audio_file != null)
                await _audio_file.DeleteAsync();
            App.DisableSelection = false;
            App.AddBtn.Visibility = Visibility.Visible;
            App.SgstBox.IsEnabled = true;
            App.Mode = false;
            App.PageHeader.Text = _prevPageName;
        }


        private async void AudioRecord_Clicked(object sender, RoutedEventArgs e)
        {
            sndPlay.Visibility = Visibility.Collapsed;
            await (new AudioCaptureDialog()).ShowAsync();
            _audio_file = await ApplicationData.Current.TemporaryFolder.TryGetItemAsync("tempVoice.m4a") as StorageFile;
            if (_audio_file != null)
                sndPlay.Visibility = Visibility.Visible;
        }

        private async void VideoRecord_Clicked(object sender, RoutedEventArgs e)
        {
            vidPlay.Visibility = Visibility.Collapsed;
            if (_video_file != null)
                await _video_file.DeleteAsync();
            _video_file = await _capture.CaptureFileAsync(CameraCaptureUIMode.Video);
            if (_video_file != null)
                vidPlay.Visibility = Visibility.Visible;
        }

        private async void Icon_change_Clicked(object sender, RoutedEventArgs e)
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
            if(yes)
            {
                BitmapImage image = new BitmapImage();
                icon.Source = image;
                await image.SetSourceAsync(await (await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/item.png"))).OpenReadAsync());
                if (_delete)
                {
                    _delete = false;
                    await _icon_file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
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

        private async void Play_sound_Clicked(object sender, RoutedEventArgs e)
            => await Windows.System.Launcher.LaunchFileAsync(_audio_file);

        private async void Play_video_Clicked(object sender, RoutedEventArgs e)
            => await Windows.System.Launcher.LaunchFileAsync(_video_file);

        private void Cancel_Clicked(object sender, RoutedEventArgs e) =>
            App.Cont.Content = App.GetActivePage();

        private void Innk_Loaded(object sender, RoutedEventArgs e)
        {
            ink.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
            InkDrawingAttributes attr = new InkDrawingAttributes();
            attr.Color = (Application.Current.Resources["InkToolbarAccentColorThemeBrush"] as SolidColorBrush).Color;
            attr.IgnorePressure = true;
            ink.InkPresenter.UpdateDefaultDrawingAttributes(attr);
        }

        private void RefreshCanvas_Clicked(object sender, RoutedEventArgs e)
        {
            ink.InkPresenter.StrokeContainer.Clear();
        }

        private async void Schedule_Clicked(object sender, RoutedEventArgs e)
        {
            NewEvent.Date = clndPicker.Date.Value.Date + DateTimeOffset.Now.TimeOfDay;
            if (string.IsNullOrEmpty(NewEvent.Name) || string.IsNullOrWhiteSpace(NewEvent.Name))
                await ShowErrorMessage(titleEmpty);

            else if (rbtnAudio.IsChecked.Value && _audio_file == null)
                await ShowErrorMessage(audioMissing);

            else if (rbtnVideo.IsChecked.Value && _video_file == null)
                await ShowErrorMessage(videoMissing);

            else if (alarmOffset == null && tglNotify.IsOn && NewEvent.Date.Date + timePicker.Time <
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
            while (App.Storage.Exist(NewEvent.Guid))
                NewEvent.Guid = Guid.NewGuid();

            if (_icon_file != null)
            {
                NewEvent.IconPath = await StoreFile("Icons", _icon_file);
            }
            else
                NewEvent.IconPath = new Uri("ms-appx:///Assets/item.png");
            if (rbtnVideo.IsChecked.Value)
            {
                NewEvent.MediaMessageType = MediaMessageType.Video;
                NewEvent.MediaMessageUri = await StoreFile("Videos", _video_file);
            }
            else if (rbtnAudio.IsChecked.Value)
            {
                NewEvent.MediaMessageType = MediaMessageType.Voice;
                NewEvent.MediaMessageUri = await StoreFile("Audios", _audio_file);
            }

            if (ink.InkPresenter.StrokeContainer.GetStrokes().Count > 0)
            {
                NewEvent.HasStroke = true;
                StorageFile file = await (await ApplicationData.Current.LocalFolder.
                    CreateFolderAsync("Inks", CreationCollisionOption.OpenIfExists)).CreateFileAsync(NewEvent.Guid.ToString() + ".gif");
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (IOutputStream outStream = stream.GetOutputStreamAt(0))
                    {
                        await ink.InkPresenter.StrokeContainer.SaveAsync(outStream);
                        await outStream.FlushAsync();
                    }
                }
            }

            if (tglNotify.IsOn)
            {
                if (alarmOffset == null)
                    alarmOffset = NewEvent.Date.Date + timePicker.Time;
                var xml = App.FormNotification(NewEvent.Name, string.IsNullOrWhiteSpace(NewEvent.Message) ? "" : NewEvent.Message,
                    NewEvent.IconPath.AbsoluteUri, NewEvent.Guid.ToString());
                ScheduledToastNotification notification = new ScheduledToastNotification(xml, alarmOffset.Value);
                ToastNotificationManager.CreateToastNotifier().AddToSchedule(notification);
                NewEvent.Alarm = alarmOffset.Value;
            }
            await NewEvent.SaveToFileAsync();
            App.Storage.AddEvent(NewEvent);
            if (NewEvent.IsPast)
                App.State.MenuSelected = MenuSelection.Past;
            else
                App.State.MenuSelected = MenuSelection.Scheduled;
            App.State.Year = NewEvent.Date.Year;
            App.State.Month = NewEvent.Date.Month;
            App.State.ActivePage = PageActive.Events;
            App.MenuRefresh();
            App.Cont.Content = App.GetActivePage(NewEvent);
        }

        private async Task<Uri> StoreFile(string folder, StorageFile file)
        {
            string name = NewEvent.Guid.ToString();
            var local = await file.CopyAsync(await ApplicationData.Current.LocalFolder.
                        CreateFolderAsync(folder, CreationCollisionOption.OpenIfExists),
                        name + file.FileType, NameCollisionOption.GenerateUniqueName);
            return new Uri($"ms-appdata:///local/{folder}/{local.Name}");
        }

        private async Task ShowErrorMessage(string message)
        {
            MessageDialog dialog = new MessageDialog(message);
            dialog.Commands.Add(new UICommand(showOk));
            await dialog.ShowAsync();
        }

        private void Date_Changed(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (args.NewDate.Value.Date.Ticks < DateTimeOffset.Now.Date.Ticks)
            {
                tglNotify.IsOn = false;
                dltCheck.IsChecked = false;
                tglNotify.IsEnabled = dltCheck.IsEnabled = false;
            }
            else
            {
                tglNotify.IsEnabled = dltCheck.IsEnabled = true;
            }
        }

        private void ClndPicker_Loaded(object sender, RoutedEventArgs e)
        {
            clndPicker.DateChanged += Date_Changed;
        }

        private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Button btn = sender as Button;
            btn.BorderThickness = new Thickness(1);
        }

        private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Button btn = sender as Button;
            if(btn.Tag as string == "acpt")
                btn.BorderThickness = new Thickness(1, 0, 2, 2);
            else
                btn.BorderThickness = new Thickness(2, 0, 1, 2);
        }
    }
}
