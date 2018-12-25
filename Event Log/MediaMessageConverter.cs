using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Event_Log
{
    public class MediaMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string param = parameter as string;
            MediaMessageType type = (MediaMessageType)value;

            switch (type)
            {
                case MediaMessageType.None:
                    if (param == "vis")
                        return Visibility.Collapsed;
                    else
                        return "";
                case MediaMessageType.Video:
                    if (param == "vis")
                        return Visibility.Visible;
                    else
                        return App.Eng ? "Video" : "Видео";
                case MediaMessageType.Voice:
                    if (param == "vis")
                        return Visibility.Visible;
                    else
                        return App.Eng ? "Audio" : "Аудио";
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}