using DMBTimer.Models;
using System;

namespace DMBTimer.Services;

public sealed record CountdownSnapshot(
    string TimeDisplay,
    double ProgressPercent,
    string PercentText,
    int DaysServed,
    int DaysLeft,
    int TotalDays,
    bool IsCompleted);

public sealed class CountdownService
{
    private readonly LocalizationService _localization;

    public CountdownService(LocalizationService localization)
    {
        _localization = localization;
    }

    public CountdownSnapshot Calculate(DateTime startDate, DateTime endDate, DateTime now, DisplayMode displayMode)
    {
        startDate = startDate.Date;
        endDate = endDate.Date;

        if (endDate <= startDate)
            endDate = startDate.AddDays(1);

        var total = endDate - startDate;
        var elapsedSeconds = Math.Clamp((now - startDate).TotalSeconds, 0, total.TotalSeconds);
        var progress = 100.0 * elapsedSeconds / total.TotalSeconds;
        var isCompleted = now >= endDate;
        var today = now.Date;
        var totalDays = Math.Max(1, total.Days);
        var daysServed = Math.Clamp((today - startDate).Days, 0, totalDays);
        var daysLeft = Math.Max(0, (endDate - today).Days);

        return new CountdownSnapshot(
            isCompleted ? _localization.GetString("DembelTitle") : FormatRemaining(endDate, now, displayMode),
            isCompleted ? 100 : progress,
            $"{(isCompleted ? 100 : progress):F0}%",
            daysServed,
            daysLeft,
            totalDays,
            isCompleted);
    }

    private string FormatRemaining(DateTime endDate, DateTime now, DisplayMode mode)
    {
        var diff = endDate - now;
        return mode switch
        {
            DisplayMode.Classic => FormatClassic(endDate, now, diff),
            DisplayMode.Weeks => FormatUnit((int)Math.Floor(diff.TotalDays / 7), "Week"),
            DisplayMode.Days => FormatUnit((int)Math.Floor(diff.TotalDays), "Day"),
            DisplayMode.Hours => FormatUnit((int)Math.Floor(diff.TotalHours), "Hour"),
            DisplayMode.Minutes => FormatUnit((int)Math.Floor(diff.TotalMinutes), "Minute"),
            DisplayMode.Seconds => FormatUnit((int)Math.Floor(diff.TotalSeconds), "Second"),
            _ => string.Empty
        };
    }

    private string FormatClassic(DateTime endDate, DateTime now, TimeSpan diff)
    {
        var cursor = now;
        var years = 0;
        var months = 0;

        while (cursor.AddYears(1) <= endDate)
        {
            years++;
            cursor = cursor.AddYears(1);
        }

        while (cursor.AddMonths(1) <= endDate)
        {
            months++;
            cursor = cursor.AddMonths(1);
        }

        var days = Math.Max(0, (int)(endDate - cursor).TotalDays);
        return _localization.Format(
            "ClassicTimeFormat",
            years,
            months,
            days,
            diff.Hours,
            diff.Minutes,
            diff.Seconds);
    }

    private string FormatUnit(int value, string keyPrefix)
    {
        var unit = _localization.GetPlural(
            value,
            $"Unit{keyPrefix}One",
            $"Unit{keyPrefix}Few",
            $"Unit{keyPrefix}Many");
        return $"{value} {unit}";
    }
}
