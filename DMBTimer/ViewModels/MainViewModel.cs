using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DMBTimer.Models;
using DMBTimer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DMBTimer.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly TimerService _timerService;
    private readonly CountdownService _countdownService;
    private readonly MilestoneService _milestoneService;
    private readonly MilitaryBranchService _militaryBranchService;
    private readonly MilestoneNotificationService _milestoneNotificationService;
    private readonly CustomMilestoneService _customMilestoneService;
    private readonly AutoStartService _autoStartService;
    private readonly SettingsService _settings;
    private readonly LocalizationService _localization;
    private List<Milestone> _milestoneModels = new();
    private List<CustomMilestone> _customMilestoneModels = new();
    private DateTime? _lastDailyUpdate;
    private bool _isLoading;
    private bool _isSynchronizingAutoStart;
    private bool _isSynchronizingServiceTermOption;
    private bool _started;

    [ObservableProperty] private DateTime _startDate;
    [ObservableProperty] private DateTime _endDate;
    [ObservableProperty] private int _serviceLengthDays = 365;
    [ObservableProperty] private ServiceTermMode _currentServiceTermMode = ServiceTermMode.CalendarYear;
    [ObservableProperty] private string _timeDisplay = string.Empty;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _percentText = "0%";
    [ObservableProperty] private string _dateDisplay = string.Empty;
    [ObservableProperty] private string _daysServedText = string.Empty;
    [ObservableProperty] private string _daysLeftText = string.Empty;
    [ObservableProperty] private string _dembelDateText = string.Empty;
    [ObservableProperty] private string _currentTheme = "Default";
    [ObservableProperty] private string _notificationSoundPath = string.Empty;
    [ObservableProperty] private bool _autoStartEnabled;
    [ObservableProperty] private bool _canChangeAutoStart = true;
    [ObservableProperty] private bool _systemNotificationsEnabled = true;
    [ObservableProperty] private bool _onboardingCompleted;
    [ObservableProperty] private string _profileName = string.Empty;
    [ObservableProperty] private string _currentLanguage = "ru-RU";
    [ObservableProperty] private DisplayMode _currentDisplayMode = DisplayMode.Classic;
    [ObservableProperty] private MilitaryBranch? _currentMilitaryBranch;
    [ObservableProperty] private ObservableCollection<LanguageItem> _availableLanguages = new();
    [ObservableProperty] private ObservableCollection<ServiceTermOption> _serviceTermOptions = new();
    [ObservableProperty] private ServiceTermOption? _selectedServiceTermOption;
    [ObservableProperty] private ObservableCollection<MilitaryBranchOption> _militaryBranches = new();
    [ObservableProperty] private ObservableCollection<MilestoneItemViewModel> _milestones = new();
    [ObservableProperty] private ObservableCollection<CustomMilestoneItemViewModel> _customMilestones = new();
    [ObservableProperty] private string _currentServiceDayText = string.Empty;
    [ObservableProperty] private string _nextMilestoneTitle = string.Empty;
    [ObservableProperty] private string _nextMilestoneDescription = string.Empty;
    [ObservableProperty] private string _nextMilestoneDateText = string.Empty;
    [ObservableProperty] private string _nextMilestoneCountdownText = string.Empty;
    [ObservableProperty] private string _nextMilestoneGlyph = "\u2B50";

    public ICommand ToggleThemeCommand { get; }
    public ICommand ApplyServiceTermCommand { get; }
    public ICommand SelectSoundCommand { get; }
    public ICommand PreviewSoundCommand { get; }
    public ICommand ResetSoundCommand { get; }
    public ICommand SetStartDateCommand { get; }
    public ICommand SetEndDateCommand { get; }
    public ICommand SetDisplayModeCommand { get; }
    public ICommand AddCustomMilestoneCommand { get; }
    public ICommand EditCustomMilestoneCommand { get; }
    public ICommand DeleteCustomMilestoneCommand { get; }
    public ICommand ExportBackupCommand { get; }
    public ICommand ImportBackupCommand { get; }
    public ICommand TestNotificationCommand { get; }
    public ICommand RunOnboardingCommand { get; }

    public event EventHandler<string>? ThemeChanged;
    public event Action? StatsUpdated;
    public event Action? SoundSelectionRequested;
    public event Action? SoundPreviewRequested;
    public event Action? LanguageChanged;
    public event EventHandler<Milestone>? MilestoneReached;
    public event Action<CustomMilestone?>? CustomMilestoneEditRequested;
    public event Action<CustomMilestone>? CustomMilestoneDeleteRequested;
    public event Action? BackupExportRequested;
    public event Action? BackupImportRequested;
    public event Action? TestNotificationRequested;
    public event Action? OnboardingRequested;
    public event Action<string, string>? UserNotificationRequested;

    public bool IsDaysTermMode => CurrentServiceTermMode == ServiceTermMode.Days;
    public bool IsEndDateTermMode => CurrentServiceTermMode == ServiceTermMode.EndDate;
    public bool HasSelectedMilitaryBranch => CurrentMilitaryBranch.HasValue;
    public bool NeedsOnboarding => !OnboardingCompleted || !CurrentMilitaryBranch.HasValue;
    public string ProfileGreeting => string.IsNullOrWhiteSpace(ProfileName)
        ? GetString("HomeGreetingGeneric")
        : _localization.Format("HomeGreetingNamed", ProfileName);
    public string SelectedMilitaryBranchDisplay => CurrentMilitaryBranch.HasValue
        ? MilitaryBranches.FirstOrDefault(option => option.Branch == CurrentMilitaryBranch.Value)?.DisplayName
          ?? MilitaryBranchNotSelected
        : MilitaryBranchNotSelected;
    public string NotificationSoundDisplay => string.IsNullOrWhiteSpace(NotificationSoundPath)
        ? NotificationSoundNotSelected
        : Path.GetFileName(NotificationSoundPath);

    public MainViewModel()
    {
        _settings = SettingsService.Instance;
        _localization = LocalizationService.Instance;
        _countdownService = new CountdownService(_localization);
        _militaryBranchService = new MilitaryBranchService(_localization);
        _milestoneService = new MilestoneService(_localization, _militaryBranchService);
        _milestoneNotificationService = new MilestoneNotificationService(_settings);
        _customMilestoneService = new CustomMilestoneService();
        _autoStartService = new AutoStartService();
        _timerService = new TimerService();
        _timerService.Tick += OnTimerTick;

        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        ApplyServiceTermCommand = new RelayCommand(ApplyServiceTerm);
        SelectSoundCommand = new RelayCommand(() => SoundSelectionRequested?.Invoke());
        PreviewSoundCommand = new RelayCommand(() => SoundPreviewRequested?.Invoke());
        ResetSoundCommand = new RelayCommand(ResetSound);
        SetStartDateCommand = new RelayCommand<DateTime>(SetStartDate);
        SetEndDateCommand = new RelayCommand<DateTime>(SetEndDate);
        SetDisplayModeCommand = new RelayCommand<DisplayMode>(SetDisplayMode);
        AddCustomMilestoneCommand = new RelayCommand(() => CustomMilestoneEditRequested?.Invoke(null));
        EditCustomMilestoneCommand = new RelayCommand<CustomMilestoneItemViewModel>(item =>
        {
            if (item is not null)
                CustomMilestoneEditRequested?.Invoke(item.Model);
        });
        DeleteCustomMilestoneCommand = new RelayCommand<CustomMilestoneItemViewModel>(item =>
        {
            if (item is not null)
                CustomMilestoneDeleteRequested?.Invoke(item.Model);
        });
        ExportBackupCommand = new RelayCommand(() => BackupExportRequested?.Invoke());
        ImportBackupCommand = new RelayCommand(() => BackupImportRequested?.Invoke());
        TestNotificationCommand = new RelayCommand(() => TestNotificationRequested?.Invoke());
        RunOnboardingCommand = new RelayCommand(() => OnboardingRequested?.Invoke());

        LoadSettings();
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        _timerService.Start();
        RefreshForCurrentTime(forceDailyUpdate: true);
    }

    public async Task InitializeAutoStartAsync()
    {
        CanChangeAutoStart = false;
        var result = await _autoStartService.GetStateAsync();
        SynchronizeAutoStartState(result.IsEnabled);
        CanChangeAutoStart = true;
    }

    private void LoadSettings()
    {
        _isLoading = true;
        CurrentLanguage = _localization.CurrentLanguage;
        StartDate = _settings.LoadStartDate(DateTime.Now.Date);
        CurrentServiceTermMode = _settings.LoadServiceTermMode();
        ServiceLengthDays = _settings.LoadServiceLength();
        EndDate = _settings.LoadEndDate() ?? StartDate.AddYears(1);
        NotificationSoundPath = _settings.LoadNotificationSound();
        CurrentTheme = _settings.LoadTheme();
        CurrentDisplayMode = _settings.LoadDisplayMode();
        CurrentMilitaryBranch = _settings.LoadMilitaryBranch();
        ProfileName = _settings.LoadProfileName();
        SystemNotificationsEnabled = _settings.LoadSystemNotificationsEnabled();
        OnboardingCompleted = _settings.LoadOnboardingCompleted();
        _customMilestoneModels = _customMilestoneService.Load().ToList();
        _isLoading = false;

        BuildLanguageList();
        BuildServiceTermOptions();
        BuildMilitaryBranchOptions();
        RefreshCustomMilestoneItems();
        RecalculatePeriod(save: false, resetNotifications: false);
    }

    public void SaveSettings()
    {
        _settings.SaveStartDate(StartDate);
        _settings.SaveEndDate(EndDate);
        _settings.SaveServiceLength(ServiceLengthDays);
        _settings.SaveServiceTermMode(CurrentServiceTermMode);
        _settings.SaveNotificationSound(NotificationSoundPath);
        _settings.SaveLanguage(CurrentLanguage);
        _settings.SaveTheme(CurrentTheme);
        _settings.SaveDisplayMode(CurrentDisplayMode);
        if (CurrentMilitaryBranch.HasValue)
            _settings.SaveMilitaryBranch(CurrentMilitaryBranch.Value);
        _settings.SaveProfileName(ProfileName);
        _settings.SaveSystemNotificationsEnabled(SystemNotificationsEnabled);
        _settings.SaveOnboardingCompleted(OnboardingCompleted);
        _customMilestoneService.Save(_customMilestoneModels);
    }

    private void ApplyServiceTerm() => RecalculatePeriod(save: true, resetNotifications: true);

    private void RecalculatePeriod(bool save, bool resetNotifications)
    {
        switch (CurrentServiceTermMode)
        {
            case ServiceTermMode.CalendarYear:
                EndDate = StartDate.AddYears(1);
                ServiceLengthDays = (EndDate - StartDate).Days;
                break;
            case ServiceTermMode.Days:
                ServiceLengthDays = Math.Clamp(ServiceLengthDays, 1, 3650);
                EndDate = StartDate.AddDays(ServiceLengthDays);
                break;
            case ServiceTermMode.EndDate:
                if (EndDate <= StartDate)
                    EndDate = StartDate.AddYears(1);
                ServiceLengthDays = Math.Clamp((EndDate - StartDate).Days, 1, 3650);
                EndDate = StartDate.AddDays(ServiceLengthDays);
                break;
        }

        DateDisplay = _localization.Format("DembelDateLabel", EndDate.ToString("d", _localization.Culture));
        RefreshForCurrentTime(forceDailyUpdate: true, checkMilestones: false);

        if (!save) return;
        _settings.SaveStartDate(StartDate);
        _settings.SaveEndDate(EndDate);
        _settings.SaveServiceLength(ServiceLengthDays);
        _settings.SaveServiceTermMode(CurrentServiceTermMode);
        if (resetNotifications)
            _settings.ResetMilestoneNotificationState();
        if (_started)
            CheckMilestones(DateTime.Now.Date);
    }

    private void SetStartDate(DateTime newDate)
    {
        StartDate = newDate.Date;
        RecalculatePeriod(save: true, resetNotifications: true);
    }

    private void SetEndDate(DateTime newDate)
    {
        EndDate = newDate.Date <= StartDate ? StartDate.AddDays(1) : newDate.Date;
        if (CurrentServiceTermMode != ServiceTermMode.EndDate)
            CurrentServiceTermMode = ServiceTermMode.EndDate;
        else
            RecalculatePeriod(save: true, resetNotifications: true);
    }

    private void SetDisplayMode(DisplayMode mode) => CurrentDisplayMode = mode;

    private void OnTimerTick(object? sender, object e) => RefreshForCurrentTime(forceDailyUpdate: false);

    private void RefreshForCurrentTime(bool forceDailyUpdate, bool checkMilestones = true)
    {
        var now = DateTime.Now;
        var snapshot = _countdownService.Calculate(StartDate, EndDate, now, CurrentDisplayMode);
        TimeDisplay = snapshot.TimeDisplay;
        ProgressValue = snapshot.ProgressPercent;
        PercentText = snapshot.PercentText;

        if (!forceDailyUpdate && _lastDailyUpdate == now.Date) return;
        _lastDailyUpdate = now.Date;
        RefreshMilestones();
        ApplyStats(snapshot);
        StatsUpdated?.Invoke();
        if (_started && checkMilestones)
            CheckMilestones(now.Date);
    }

    public void UpdateStats()
    {
        var snapshot = _countdownService.Calculate(StartDate, EndDate, DateTime.Now, CurrentDisplayMode);
        ApplyStats(snapshot);
        StatsUpdated?.Invoke();
    }

    private void ApplyStats(CountdownSnapshot snapshot)
    {
        DaysServedText = _localization.Format("DaysServedFormat", snapshot.DaysServed, snapshot.TotalDays);
        DaysLeftText = _localization.Format("DaysLeftFormat", snapshot.DaysLeft);
        DembelDateText = EndDate.ToString("d", _localization.Culture);
        CurrentServiceDayText = DateTime.Now.Date < StartDate.Date
            ? GetString("ServiceNotStarted")
            : snapshot.IsCompleted
                ? GetString("ServiceCompletedStatus")
                : _localization.Format("CurrentServiceDayFormat", snapshot.DaysServed + 1);
    }

    private void RefreshMilestones()
    {
        _milestoneModels = _milestoneService.Build(
            StartDate,
            EndDate,
            CurrentMilitaryBranch,
            _customMilestoneModels);
        var today = DateTime.Now.Date;
        Milestones = new ObservableCollection<MilestoneItemViewModel>(
            _milestoneModels.Select(model => new MilestoneItemViewModel(model, today, _localization.Culture)));
        UpdateNextMilestone(today);
    }

    private void CheckMilestones(DateTime today)
    {
        var dueMilestones = _milestoneNotificationService.GetDueMilestones(_milestoneModels, today);
        if (!SystemNotificationsEnabled)
            return;

        foreach (var milestone in dueMilestones)
            MilestoneReached?.Invoke(this, milestone);
    }

    private void UpdateNextMilestone(DateTime today)
    {
        var next = _milestoneModels
            .Where(item => item.Date >= today)
            .OrderBy(item => item.Date)
            .FirstOrDefault();

        if (next is null)
        {
            NextMilestoneGlyph = "\u2705";
            NextMilestoneTitle = GetString("NoUpcomingMilestones");
            NextMilestoneDescription = string.Empty;
            NextMilestoneDateText = string.Empty;
            NextMilestoneCountdownText = string.Empty;
            return;
        }

        var days = (next.Date - today).Days;
        NextMilestoneGlyph = next.Glyph;
        NextMilestoneTitle = next.Title;
        NextMilestoneDescription = next.Description;
        NextMilestoneDateText = next.Date.ToString("d", _localization.Culture);
        NextMilestoneCountdownText = days == 0
            ? GetString("MilestoneToday")
            : _localization.Format(
                "MilestoneInDays",
                days,
                _localization.GetPlural(days, "UnitDayOne", "UnitDayFew", "UnitDayMany"));
    }

    private void ToggleTheme()
    {
        CurrentTheme = CurrentTheme switch
        {
            "Default" => "Light",
            "Light" => "Dark",
            _ => "Default"
        };
        _settings.SaveTheme(CurrentTheme);
        ThemeChanged?.Invoke(this, CurrentTheme);
    }

    private void ResetSound()
    {
        NotificationSoundPath = string.Empty;
        _settings.SaveNotificationSound(string.Empty);
    }

    public void SetSoundPath(string path)
    {
        NotificationSoundPath = path;
        _settings.SaveNotificationSound(path);
    }

    public void SelectMilitaryBranch(MilitaryBranch branch) => CurrentMilitaryBranch = branch;

    public OnboardingViewModel CreateOnboardingViewModel() => new(
        _localization,
        ProfileName,
        CurrentLanguage,
        CurrentMilitaryBranch,
        StartDate,
        EndDate,
        CurrentServiceTermMode,
        ServiceLengthDays,
        SystemNotificationsEnabled,
        AutoStartEnabled,
        AvailableLanguages,
        MilitaryBranches,
        ServiceTermOptions);

    public async Task CompleteOnboardingAsync(OnboardingData data)
    {
        _isLoading = true;
        ProfileName = data.ProfileName;
        CurrentLanguage = data.Language;
        CurrentMilitaryBranch = data.MilitaryBranch;
        StartDate = data.StartDate.Date;
        EndDate = data.EndDate.Date;
        CurrentServiceTermMode = data.ServiceTermMode;
        ServiceLengthDays = data.ServiceLengthDays;
        SystemNotificationsEnabled = data.SystemNotificationsEnabled;
        AutoStartEnabled = data.AutoStartEnabled;
        OnboardingCompleted = true;
        _isLoading = false;

        _localization.SetLanguage(CurrentLanguage);
        BuildLanguageList();
        BuildServiceTermOptions();
        BuildMilitaryBranchOptions();
        RecalculatePeriod(save: true, resetNotifications: true);
        SaveSettings();
        await ApplyAutoStartAsync(AutoStartEnabled);
        OnPropertyChanged(string.Empty);
        ThemeChanged?.Invoke(this, CurrentTheme);
        LanguageChanged?.Invoke();
    }

    public void UpsertCustomMilestone(CustomMilestone milestone)
    {
        var index = _customMilestoneModels.FindIndex(item => item.Id == milestone.Id);
        if (index >= 0)
            _customMilestoneModels[index] = milestone;
        else
            _customMilestoneModels.Add(milestone);

        _customMilestoneModels = _customMilestoneModels.OrderBy(item => item.Date).ToList();
        _customMilestoneService.Save(_customMilestoneModels);
        _settings.ForgetMilestoneNotification($"custom_{milestone.Id:N}");
        RefreshCustomMilestoneItems();
        RefreshForCurrentTime(forceDailyUpdate: true);
    }

    public void DeleteCustomMilestone(Guid id)
    {
        if (_customMilestoneModels.RemoveAll(item => item.Id == id) == 0)
            return;

        _customMilestoneService.Save(_customMilestoneModels);
        _settings.ForgetMilestoneNotification($"custom_{id:N}");
        RefreshCustomMilestoneItems();
        RefreshForCurrentTime(forceDailyUpdate: true);
    }

    public AppBackup CreateBackup() => new()
    {
        ProfileName = ProfileName,
        StartDate = StartDate,
        EndDate = EndDate,
        ServiceLengthDays = ServiceLengthDays,
        ServiceTermMode = CurrentServiceTermMode,
        MilitaryBranch = CurrentMilitaryBranch,
        Theme = CurrentTheme,
        Language = CurrentLanguage,
        DisplayMode = CurrentDisplayMode,
        AutoStartEnabled = AutoStartEnabled,
        SystemNotificationsEnabled = SystemNotificationsEnabled,
        CustomMilestones = _customMilestoneModels.Select(item => new CustomMilestone
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Date = item.Date,
            NotificationsEnabled = item.NotificationsEnabled
        }).ToList()
    };

    public async Task RestoreBackupAsync(BackupImportResult result)
    {
        var backup = result.Backup;
        _isLoading = true;
        ProfileName = backup.ProfileName?.Trim() ?? string.Empty;
        StartDate = backup.StartDate.Date;
        EndDate = backup.EndDate.Date;
        ServiceLengthDays = backup.ServiceLengthDays;
        CurrentServiceTermMode = backup.ServiceTermMode;
        CurrentMilitaryBranch = backup.MilitaryBranch;
        CurrentTheme = backup.Theme;
        CurrentLanguage = backup.Language;
        CurrentDisplayMode = backup.DisplayMode;
        AutoStartEnabled = backup.AutoStartEnabled;
        SystemNotificationsEnabled = backup.SystemNotificationsEnabled;
        NotificationSoundPath = result.NotificationSoundPath;
        OnboardingCompleted = backup.MilitaryBranch.HasValue;
        _customMilestoneModels = backup.CustomMilestones
            .Where(item => item.Id != Guid.Empty && !string.IsNullOrWhiteSpace(item.Title))
            .OrderBy(item => item.Date)
            .ToList();
        _isLoading = false;

        _localization.SetLanguage(CurrentLanguage);
        BuildLanguageList();
        BuildServiceTermOptions();
        BuildMilitaryBranchOptions();
        RefreshCustomMilestoneItems();
        RecalculatePeriod(save: true, resetNotifications: true);
        SaveSettings();
        await ApplyAutoStartAsync(AutoStartEnabled);
        OnPropertyChanged(string.Empty);
        ThemeChanged?.Invoke(this, CurrentTheme);
        LanguageChanged?.Invoke();
    }

    private void BuildLanguageList()
    {
        AvailableLanguages = new ObservableCollection<LanguageItem>(
            _localization.GetAvailableLanguages().Select(code =>
            {
                string displayName;
                try
                {
                    displayName = CultureInfo.GetCultureInfo(code).NativeName;
                }
                catch (CultureNotFoundException)
                {
                    displayName = code;
                }
                return new LanguageItem { Code = code, DisplayName = displayName };
            }));
    }

    private void BuildServiceTermOptions()
    {
        ServiceTermOptions = new ObservableCollection<ServiceTermOption>
        {
            new() { Mode = ServiceTermMode.CalendarYear, DisplayName = ServiceTermCalendarYear },
            new() { Mode = ServiceTermMode.Days, DisplayName = ServiceTermDays },
            new() { Mode = ServiceTermMode.EndDate, DisplayName = ServiceTermEndDate }
        };
        SynchronizeSelectedServiceTermOption();
    }

    private void SynchronizeSelectedServiceTermOption()
    {
        _isSynchronizingServiceTermOption = true;
        SelectedServiceTermOption = ServiceTermOptions.FirstOrDefault(
            option => option.Mode == CurrentServiceTermMode);
        _isSynchronizingServiceTermOption = false;
    }

    private void BuildMilitaryBranchOptions()
    {
        MilitaryBranches = new ObservableCollection<MilitaryBranchOption>(
            _militaryBranchService.GetOptions());
        OnPropertyChanged(nameof(SelectedMilitaryBranchDisplay));
    }

    private void RefreshCustomMilestoneItems()
    {
        CustomMilestones = new ObservableCollection<CustomMilestoneItemViewModel>(
            _customMilestoneModels
                .OrderBy(item => item.Date)
                .Select(item => new CustomMilestoneItemViewModel(item, _localization.Culture)));
    }

    partial void OnCurrentServiceTermModeChanged(ServiceTermMode value)
    {
        if (!_isSynchronizingServiceTermOption)
            SynchronizeSelectedServiceTermOption();

        OnPropertyChanged(nameof(IsDaysTermMode));
        OnPropertyChanged(nameof(IsEndDateTermMode));
        if (!_isLoading)
            RecalculatePeriod(save: true, resetNotifications: true);
    }

    partial void OnSelectedServiceTermOptionChanged(ServiceTermOption? value)
    {
        if (_isSynchronizingServiceTermOption || value is null || value.Mode == CurrentServiceTermMode)
            return;

        CurrentServiceTermMode = value.Mode;
    }

    partial void OnCurrentDisplayModeChanged(DisplayMode value)
    {
        if (!_isLoading)
            _settings.SaveDisplayMode(value);
        if (_started)
            RefreshForCurrentTime(forceDailyUpdate: false);
    }

    partial void OnCurrentMilitaryBranchChanged(MilitaryBranch? value)
    {
        OnPropertyChanged(nameof(HasSelectedMilitaryBranch));
        OnPropertyChanged(nameof(NeedsOnboarding));
        OnPropertyChanged(nameof(SelectedMilitaryBranchDisplay));
        if (_isLoading || !value.HasValue)
            return;

        _settings.SaveMilitaryBranch(value.Value);
        _settings.ResetMilestoneNotificationState();
        RefreshForCurrentTime(forceDailyUpdate: true);
    }

    partial void OnProfileNameChanged(string value)
    {
        OnPropertyChanged(nameof(ProfileGreeting));
        if (!_isLoading)
            _settings.SaveProfileName(value);
    }

    partial void OnSystemNotificationsEnabledChanged(bool value)
    {
        if (!_isLoading)
            _settings.SaveSystemNotificationsEnabled(value);
    }

    partial void OnOnboardingCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(NeedsOnboarding));
        if (!_isLoading)
            _settings.SaveOnboardingCompleted(value);
    }

    async partial void OnAutoStartEnabledChanged(bool value)
    {
        if (_isLoading || _isSynchronizingAutoStart)
            return;

        await ApplyAutoStartAsync(value);
    }

    private async Task ApplyAutoStartAsync(bool requestedValue)
    {
        CanChangeAutoStart = false;
        var result = await _autoStartService.SetEnabledAsync(requestedValue);
        SynchronizeAutoStartState(result.IsEnabled);
        CanChangeAutoStart = true;

        if (result.IsEnabled == requestedValue && result.Failure == AutoStartFailure.None)
            return;

        var message = result.Failure switch
        {
            AutoStartFailure.DisabledByUser => AutoStartDisabledByUserMessage,
            AutoStartFailure.DisabledByPolicy => AutoStartDisabledByPolicyMessage,
            _ => AutoStartErrorMessage
        };
        UserNotificationRequested?.Invoke(AutoStartErrorTitle, message);
    }

    private void SynchronizeAutoStartState(bool isEnabled)
    {
        _isSynchronizingAutoStart = true;
        AutoStartEnabled = isEnabled;
        _isSynchronizingAutoStart = false;
    }

    partial void OnNotificationSoundPathChanged(string value) =>
        OnPropertyChanged(nameof(NotificationSoundDisplay));

    partial void OnCurrentLanguageChanged(string value)
    {
        if (_isLoading || string.IsNullOrWhiteSpace(value)) return;
        _localization.SetLanguage(value);
        _settings.SaveLanguage(value);
        BuildServiceTermOptions();
        BuildMilitaryBranchOptions();
        RefreshCustomMilestoneItems();
        RecalculatePeriod(save: false, resetNotifications: false);
        OnPropertyChanged(string.Empty);
        LanguageChanged?.Invoke();
    }

    public string GetString(string key) => _localization.GetString(key);

    public string AppTitle => GetString("AppTitle");
    public string NavigationTimer => GetString("NavigationTimer");
    public string NavigationStats => GetString("NavigationStats");
    public string NavigationSettings => GetString("NavigationSettings");
    public string TimerLabel => GetString("TimerLabel");
    public string StartDateLabel => GetString("StartDateLabel");
    public string ModeClassic => GetString("ModeClassic");
    public string ModeWeeks => GetString("ModeWeeks");
    public string ModeDays => GetString("ModeDays");
    public string ModeHours => GetString("ModeHours");
    public string ModeMinutes => GetString("ModeMinutes");
    public string ModeSeconds => GetString("ModeSeconds");
    public string StatsHeader => GetString("StatsHeader");
    public string StatsServed => GetString("StatsServed");
    public string StatsLeft => GetString("StatsLeft");
    public string StatsProgress => GetString("StatsProgress");
    public string StatsDembel => GetString("StatsDembel");
    public string CalendarHeader => GetString("CalendarHeader");
    public string SettingsHeader => GetString("SettingsHeader");
    public string ServiceTermLabel => GetString("ServiceTermLabel");
    public string MilitaryBranchLabel => GetString("MilitaryBranchLabel");
    public string MilitaryBranchDescription => GetString("MilitaryBranchDescription");
    public string MilitaryBranchChange => GetString("MilitaryBranchChange");
    public string MilitaryBranchNotSelected => GetString("MilitaryBranchNotSelected");
    public string MilitaryBranchWelcomeTitle => GetString("MilitaryBranchWelcomeTitle");
    public string MilitaryBranchWelcomeDescription => GetString("MilitaryBranchWelcomeDescription");
    public string MilitaryBranchPickerPlaceholder => GetString("MilitaryBranchPickerPlaceholder");
    public string MilitaryBranchContinue => GetString("MilitaryBranchContinue");
    public string Cancel => GetString("Cancel");
    public string ServiceTermCalendarYear => GetString("ServiceTermCalendarYear");
    public string ServiceTermDays => GetString("ServiceTermDays");
    public string ServiceTermEndDate => GetString("ServiceTermEndDate");
    public string ServiceLengthLabel => GetString("ServiceLengthLabel");
    public string DembelDatePickerLabel => GetString("DembelDatePickerLabel");
    public string ServiceLengthApply => GetString("ServiceLengthApply");
    public string NotificationSoundLabel => GetString("NotificationSoundLabel");
    public string NotificationSoundNotSelected => GetString("NotificationSoundNotSelected");
    public string NotificationSoundSelect => GetString("NotificationSoundSelect");
    public string NotificationSoundPreview => GetString("NotificationSoundPreview");
    public string NotificationSoundReset => GetString("NotificationSoundReset");
    public string AutoStartLabel => GetString("AutoStartLabel");
    public string AutoStartToggle => GetString("AutoStartToggle");
    public string AutoStartErrorTitle => GetString("AutoStartErrorTitle");
    public string AutoStartDisabledByUserMessage => GetString("AutoStartDisabledByUserMessage");
    public string AutoStartDisabledByPolicyMessage => GetString("AutoStartDisabledByPolicyMessage");
    public string AutoStartErrorMessage => GetString("AutoStartErrorMessage");
    public string LanguageLabel => GetString("LanguageLabel");
    public string HotkeysLabel => GetString("HotkeysLabel");
    public string HotkeysDescription => GetString("HotkeysDescription");
    public string DembelTitle => GetString("DembelTitle");
    public string DembelMessage => GetString("DembelMessage");
    public string DembelButton => GetString("DembelButton");
    public string ToolTipThemeToggle => GetString("ToolTipThemeToggle");
    public string NextMilestoneLabel => GetString("NextMilestoneLabel");
    public string CustomMilestonesHeader => GetString("CustomMilestonesHeader");
    public string CustomMilestonesDescription => GetString("CustomMilestonesDescription");
    public string CustomMilestoneAdd => GetString("CustomMilestoneAdd");
    public string CustomMilestoneEdit => GetString("CustomMilestoneEdit");
    public string CustomMilestoneDelete => GetString("CustomMilestoneDelete");
    public string SystemNotificationsLabel => GetString("SystemNotificationsLabel");
    public string SystemNotificationsToggle => GetString("SystemNotificationsToggle");
    public string TestNotification => GetString("TestNotification");
    public string BackupLabel => GetString("BackupLabel");
    public string BackupDescription => GetString("BackupDescription");
    public string BackupExport => GetString("BackupExport");
    public string BackupImport => GetString("BackupImport");
    public string RunOnboarding => GetString("RunOnboarding");
    public string ProfileNameLabel => GetString("ProfileNameLabel");
    public string ProfileNamePlaceholder => GetString("ProfileNamePlaceholder");

    public void Dispose()
    {
        _timerService.Tick -= OnTimerTick;
        _timerService.Dispose();
    }
}
