using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Event_Log
{
    class IsPaneOpenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var param = parameter as string;
            if (string.IsNullOrEmpty(param))
                return null;
            else
            {
                if(param == "f")
                {
                    if ((bool)value)
                        return Visibility.Collapsed;
                    else
                        return Visibility.Visible;
                }
                if(param == "n")
                {
                    if ((bool)value)
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                }
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}