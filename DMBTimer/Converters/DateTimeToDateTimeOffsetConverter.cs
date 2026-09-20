using Microsoft.UI.Xaml.Data;
using System;

namespace DMBTimer.Converters;

public sealed class DateTimeToDateTimeOffsetConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not DateTime dateTime)
            return DateTimeOffset.MinValue;

        var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, TimeZoneInfo.Local.GetUtcOffset(unspecified));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is DateTimeOffset dateTimeOffset ? dateTimeOffset.Date : DateTime.MinValue;
}
