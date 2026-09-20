using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Globalization;

namespace DMBTimer.Services;

public sealed class LocalizationService
{
    private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
    public static LocalizationService Instance => _instance.Value;

    private ResourceLoader? _resourceLoader;
    private string _currentLanguage = "ru-RU";

    public string CurrentLanguage => _currentLanguage;
    public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("ru-RU");

    private LocalizationService() { }

    public static void Initialize() => Instance.SetLanguage(SettingsService.Instance.LoadLanguage());

    public void SetLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            languageCode = "ru-RU";

        try
        {
            Culture = CultureInfo.GetCultureInfo(languageCode);
        }
        catch (CultureNotFoundException)
        {
            languageCode = "ru-RU";
            Culture = CultureInfo.GetCultureInfo(languageCode);
        }

        _currentLanguage = languageCode;
        ApplicationLanguages.PrimaryLanguageOverride = languageCode;
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
        _resourceLoader = ResourceLoader.GetForViewIndependentUse("Resources");
    }

    public string GetString(string key)
    {
        try
        {
            var value = _resourceLoader?.GetString(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }
        catch
        {
            return key;
        }
    }

    public string Format(string key, params object[] arguments)
    {
        var template = GetString(key);
        try
        {
            return string.Format(Culture, template, arguments);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public string GetPlural(int number, string oneKey, string fewKey, string manyKey)
    {
        if (!_currentLanguage.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            return GetString(Math.Abs(number) == 1 ? oneKey : manyKey);

        var absolute = Math.Abs(number);
        var n100 = absolute % 100;
        var n10 = absolute % 10;
        if (n100 is >= 11 and <= 14) return GetString(manyKey);
        if (n10 == 1) return GetString(oneKey);
        if (n10 is >= 2 and <= 4) return GetString(fewKey);
        return GetString(manyKey);
    }

    public IReadOnlyList<string> GetAvailableLanguages()
    {
        try
        {
            var languages = ApplicationLanguages.ManifestLanguages
                .Where(language => !string.IsNullOrWhiteSpace(language))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (languages.Count > 0)
                return languages;
        }
        catch
        {
            // Use the known bundled languages below.
        }

        return new[] { "ru-RU", "en-US" };
    }
}
