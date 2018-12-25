using System;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;


namespace Event_Log
{
    public sealed partial class AudioCaptureDialog : ContentDialog
    {
        private string _tempFile = "tempVoice.m4a";
        private DateTime _time_started;
        private MediaCapture _capture = null;
        private DispatcherTimer _timer;
        private bool _proceed = false;
        private MediaPlayer _player;
        private MediaTimelineController _mediaTimelineController;
        private StorageFile _track;
        private TimeSpan _duration;
        private int _total;
        private bool _resume_required = false;
        private string dlgRecord, dlgStop, dlgFailed;

        public AudioCaptureDialog()
        {
            ResourceLoader res = ResourceLoader.GetForCurrentView();
            this.InitializeComponent();
            this.Title = res.GetString("dlgTitle");
            dlgRecord = res.GetString("dlgRecord");
            this.SecondaryButtonText = res.GetString("dlgCancel");
            dlgStop = res.GetString("dlgStop");
            dlgFailed = res.GetString("dlgFailed");
            this.PrimaryButtonText = dlgRecord;
            _player = new MediaPlayer();
            _player.AudioCategory = MediaPlayerAudioCategory.Media;
            _mediaTimelineController = new MediaTimelineController();
            _player.CommandManager.IsEnabled = false;
            _mediaTimelineController.PositionChanged += MediaTimelineController_PositionChanged;
            _mediaTimelineController.StateChanged += MediaTimelineController_StateChanged;
            _player.TimelineController = _mediaTimelineController;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Tick_Tock;
            this.Closing += AudioCaptureDialog_Closing; 
        }

        private async void MediaTimelineController_StateChanged(MediaTimelineController sender, object args)
        {
            switch (sender.State)
            {
                case MediaTimelineControllerState.Running:
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ppButton.Tag = "pa";
                        smbl.Symbol = Symbol.Pause;
                    });
                    break;
                default:
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ppButton.Tag = "pl";
                        if (!_resume_required)
                            smbl.Symbol = Symbol.Play;
                    });
                    break;
            }
        }

        private async void MediaTimelineController_PositionChanged(MediaTimelineController sender, object args)
        {
            if (_duration != TimeSpan.Zero)
            {
                if (_duration <= sender.Position)
                {
                    _mediaTimelineController.Pause();
                    _mediaTimelineController.Position = TimeSpan.Zero;
                }
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    slide.Value = sender.Position.TotalSeconds;
                    display.Text = (int)sender.Position.TotalSeconds + " / " + _total;
                });
            }
        }

        private async void MediaSource_OpenOperationCompleted(MediaSource sender, MediaSourceOpenOperationCompletedEventArgs args)
        {
            _duration = sender.Duration.GetValueOrDefault();
            _total = (int)Math.Floor(_duration.TotalSeconds);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
             {
                 slide.Maximum = _duration.TotalSeconds;
                 display.Text = "0 / " + _total;
                 slide.Value = 0;
                 slide.IsEnabled = true;
             });
        }

        private async void AudioCaptureDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (_capture != null)
            {
                try
                {
                    await _capture.StopRecordAsync();
                }
                catch { };
                try
                {
                    _capture.Dispose();
                }
                catch { };
                _mediaTimelineController.Pause();
                try
                {
                    (_player.Source as MediaSource).Dispose();
                }
                catch { };
                _player.Dispose();
                _timer.Stop();
                if(!_proceed)
                    await (await ApplicationData.Current.TemporaryFolder.GetFileAsync(_tempFile)).DeleteAsync();
            }
        }

        private async void Tick_Tock(object sender, object e)
        {
            DateTime now = DateTime.Now;
            
            if ((now - _time_started).Seconds >= 600)
            {
                await _capture.StopRecordAsync();
                _timer.Stop();
                var mediaSource = MediaSource.CreateFromStorageFile(_track);
                mediaSource.OpenOperationCompleted += MediaSource_OpenOperationCompleted;
                (_player.Source as MediaSource)?.Dispose();
                _player.Source = mediaSource;
                PrimaryButtonText = dlgRecord;
                ppButton.IsEnabled = true;
                acceptButton.IsEnabled = true;
            }
            else
            {
                int count = (now - _time_started).Seconds;
                slide.Value = count;
                display.Text = count.ToString();
            }
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            ppButton.Tag = "pl";
            smbl.Symbol = Symbol.Play;
            ppButton.IsEnabled = false;
            acceptButton.IsEnabled = false;
            if (PrimaryButtonText.First() == dlgRecord.First())
            {
                display.Text = 0.ToString();
                slide.IsEnabled = false;
                slide.Maximum = 600;
                slide.Value = 0;
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Audio
                };
                _capture = new MediaCapture();
                try
                {
                    await _capture.InitializeAsync(settings);
                }
                catch
                {
                    await (new MessageDialog(dlgFailed)).ShowAsync();
                    return;
                }
                var profile = MediaEncodingProfile.CreateM4a(AudioEncodingQuality.High);
                _track = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(
                    _tempFile, CreationCollisionOption.ReplaceExisting);
                _time_started = DateTime.Now;
                _timer.Start();
                try
                {
                    await _capture.StartRecordToStorageFileAsync(profile, _track);
                }
                catch(Exception ex)
                {
                    await (new MessageDialog(ex.Message)).ShowAsync();
                }
                PrimaryButtonText = dlgStop;
            }
            else
            {
                await _capture.StopRecordAsync();
                _timer.Stop();
                var mediaSource = MediaSource.CreateFromStorageFile(_track);
                mediaSource.OpenOperationCompleted += MediaSource_OpenOperationCompleted;
                (_player.Source as MediaSource)?.Dispose();
                _player.Source = mediaSource;
                PrimaryButtonText = dlgRecord;
                ppButton.IsEnabled = true;
                acceptButton.IsEnabled = true;
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            
        }

        private void Play_Pause_Clicked(object sender, RoutedEventArgs e)
        {
            if (ppButton.Tag.ToString() == "pl")
            {
                _mediaTimelineController.Resume();
            }
            else
            {
                _mediaTimelineController.Pause();
            }
        }

        private void Accept_Button_Clicked(object sender, RoutedEventArgs e)
        {
            _proceed = true;
            Hide();
        }

        private void Slide_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (_mediaTimelineController.State == MediaTimelineControllerState.Running)
            {
                _mediaTimelineController.Pause();
                _resume_required = true;
            }
        }

        private void Slide_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_resume_required)
            {
                _resume_required = false;
                _mediaTimelineController.Position = TimeSpan.FromSeconds(slide.Value);
                _mediaTimelineController.Resume();
            }
        }

        private void Slide_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_resume_required)
                display.Text = slide.Value + " / " + _total;
            else if (_mediaTimelineController.State != MediaTimelineControllerState.Running)
                _mediaTimelineController.Position = TimeSpan.FromSeconds(slide.Value);
        }
    }
}