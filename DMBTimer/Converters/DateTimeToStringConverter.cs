using DMBTimer.Services;
using Microsoft.UI.Xaml.Data;
using System;

namespace DMBTimer.Converters;

public sealed class DateTimeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not DateTime dateTime)
            return string.Empty;

        var format = parameter as string ?? "d";
        return dateTime.ToString(format, LocalizationService.Instance.Culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
