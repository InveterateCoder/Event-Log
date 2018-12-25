using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Event_Log
{
    public class TextMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string message = value as string;
            switch (!(string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message)))
            {
                case true:
                    return Visibility.Visible;
                default:
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}