using System;
using System.Collections.Generic;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Event_Log
{
    public sealed partial class ToastForeground : Page
    {
        string[] _guids;
        public ToastForeground(string args)
        {

            this.InitializeComponent();
            _guids = args.Split('&');
            this.Loaded += ToastForeground_Loaded;
        }

        private async void ToastForeground_Loaded(object sender, RoutedEventArgs e)
        {
            List<Event> list = new List<Event>();
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("data.xml");
            XmlDocument xml = await XmlDocument.LoadFromFileAsync(file);
            var root = xml.FirstChild;
            foreach (var guid in _guids)
            {
                foreach (var evnt in root.ChildNodes)
                {
                    if (evnt.ChildNodes[0].InnerText == guid)
                    {
                        list.Add(new Event(evnt));
                        break;
                    }
                }
            }
            if (list.Count > 1)
                list.Sort((x, y) => (int)(x.Date.Ticks - y.Date.Ticks));
            listView.ItemsSource = list;
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            var _evnt = (sender as FrameworkElement).DataContext as Event;
            if (_evnt != null)
                await Windows.System.Launcher.LaunchFileAsync(await StorageFile.GetFileFromApplicationUriAsync(_evnt.MediaMessageUri));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current as App).GoHome();
        }
    }
}