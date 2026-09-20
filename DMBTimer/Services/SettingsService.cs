using DMBTimer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace DMBTimer.Services;

public sealed class SettingsService
{
    private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
    public static SettingsService Instance => _instance.Value;

    private IPropertySet Values => ApplicationData.Current.LocalSettings.Values;

    private const string StartDateKey = "StartDate";
    private const string EndDateKey = "EndDate";
    private const string ServiceLengthKey = "ServiceLength";
    private const string ServiceTermModeKey = "ServiceTermMode";
    private const string ThemeKey = "AppTheme";
    private const string NotifiedKey = "NotifiedMilestones";
    private const string LastCheckedKey = "LastCheckedDate";
    private const string NotificationSoundKey = "NotificationSound";
    private const string LanguageKey = "Language";
    private const string DisplayModeKey = "DisplayMode";
    private const string MilitaryBranchKey = "MilitaryBranch";
    private const string ProfileNameKey = "ProfileName";
    private const string SystemNotificationsKey = "SystemNotificationsEnabled";
    private const string OnboardingCompletedKey = "OnboardingCompletedV2";
    private SettingsService() { }

    public DateTime LoadStartDate(DateTime fallback) =>
        TryParseStoredDate(StartDateKey, out var date) ? date.Date : fallback.Date;

    public void SaveStartDate(DateTime date) => SaveDate(StartDateKey, date);

    public DateTime? LoadEndDate() =>
        TryParseStoredDate(EndDateKey, out var date) ? date.Date : null;

    public void SaveEndDate(DateTime date) => SaveDate(EndDateKey, date);

    public int LoadServiceLength() =>
        TryGetStringValue(ServiceLengthKey, out var value) && int.TryParse(value, out var days)
            ? Math.Clamp(days, 1, 3650)
            : 365;

    public void SaveServiceLength(int days) =>
        Values[ServiceLengthKey] = Math.Clamp(days, 1, 3650).ToString(CultureInfo.InvariantCulture);

    public ServiceTermMode LoadServiceTermMode()
    {
        if (TryGetStringValue(ServiceTermModeKey, out var value) &&
            Enum.TryParse<ServiceTermMode>(value, out var mode))
            return mode;
        return ServiceTermMode.CalendarYear;
    }

    public void SaveServiceTermMode(ServiceTermMode mode) => Values[ServiceTermModeKey] = mode.ToString();

    public string LoadTheme() => TryGetStringValue(ThemeKey, out var value) ? value : "Default";
    public void SaveTheme(string theme) => Values[ThemeKey] = theme;

    public HashSet<string> LoadNotifiedMilestones()
    {
        if (TryGetStringValue(NotifiedKey, out var value) && value.Length > 0)
            return new HashSet<string>(value.Split('|', StringSplitOptions.RemoveEmptyEntries));
        return new HashSet<string>();
    }

    public void SaveNotifiedMilestones(IEnumerable<string> keys) =>
        Values[NotifiedKey] = string.Join("|", keys.Distinct());

    public void ResetMilestoneNotificationState()
    {
        Values[NotifiedKey] = string.Empty;
        Values.Remove(LastCheckedKey);
    }

    public void ForgetMilestoneNotification(string milestoneKey)
    {
        var notified = LoadNotifiedMilestones();
        if (notified.Remove(milestoneKey))
            SaveNotifiedMilestones(notified);
    }

    public DateTime? LoadLastCheckedDate() =>
        TryParseStoredDate(LastCheckedKey, out var date) ? date.Date : null;

    public void SaveLastCheckedDate(DateTime date) => SaveDate(LastCheckedKey, date);

    public string LoadNotificationSound() =>
        TryGetStringValue(NotificationSoundKey, out var value) ? value : string.Empty;

    public void SaveNotificationSound(string path) => Values[NotificationSoundKey] = path;

    public string LoadLanguage() => TryGetStringValue(LanguageKey, out var value) ? value : "ru-RU";
    public void SaveLanguage(string language) => Values[LanguageKey] = language;

    public DisplayMode LoadDisplayMode()
    {
        if (TryGetStringValue(DisplayModeKey, out var value) && Enum.TryParse<DisplayMode>(value, out var mode))
            return mode;
        return DisplayMode.Classic;
    }

    public void SaveDisplayMode(DisplayMode mode) => Values[DisplayModeKey] = mode.ToString();

    public MilitaryBranch? LoadMilitaryBranch()
    {
        if (TryGetStringValue(MilitaryBranchKey, out var value) &&
            Enum.TryParse<MilitaryBranch>(value, out var branch))
            return branch;
        return null;
    }

    public void SaveMilitaryBranch(MilitaryBranch branch) =>
        Values[MilitaryBranchKey] = branch.ToString();

    public string LoadProfileName() =>
        TryGetStringValue(ProfileNameKey, out var value) ? value : string.Empty;

    public void SaveProfileName(string profileName) =>
        Values[ProfileNameKey] = profileName?.Trim() ?? string.Empty;

    public bool LoadSystemNotificationsEnabled() =>
        Values[SystemNotificationsKey] is not bool value || value;

    public void SaveSystemNotificationsEnabled(bool value) =>
        Values[SystemNotificationsKey] = value;

    public bool LoadOnboardingCompleted() =>
        Values[OnboardingCompletedKey] is bool value && value;

    public void SaveOnboardingCompleted(bool value) =>
        Values[OnboardingCompletedKey] = value;

    private void SaveDate(string key, DateTime date) =>
        Values[key] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private bool TryParseStoredDate(string key, out DateTime date)
    {
        date = default;
        return TryGetStringValue(key, out var value) &&
               DateTime.TryParseExact(
                   value,
                   "yyyy-MM-dd",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out date);
    }

    private bool TryGetStringValue(string key, out string value)
    {
        value = string.Empty;
        if (!Values.ContainsKey(key)) return false;
        if (Values[key] is not string stored) return false;
        value = stored;
        return true;
    }
}
