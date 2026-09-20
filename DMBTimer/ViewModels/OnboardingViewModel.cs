using CommunityToolkit.Mvvm.ComponentModel;
using DMBTimer.Models;
using DMBTimer.Services;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DMBTimer.ViewModels;

public partial class OnboardingViewModel : ObservableObject
{
    private const int LastStep = 3;
    private readonly LocalizationService _localization;

    [ObservableProperty] private int _currentStep;
    [ObservableProperty] private string _profileName = string.Empty;
    [ObservableProperty] private string _currentLanguage = "ru-RU";
    [ObservableProperty] private MilitaryBranchOption? _selectedBranch;
    [ObservableProperty] private DateTimeOffset _startDate;
    [ObservableProperty] private DateTimeOffset _endDate;
    [ObservableProperty] private ServiceTermMode _serviceTermMode;
    [ObservableProperty] private int _serviceLengthDays;
    [ObservableProperty] private bool _systemNotificationsEnabled = true;
    [ObservableProperty] private bool _autoStartEnabled;

    public ObservableCollection<LanguageItem> Languages { get; }
    public ObservableCollection<MilitaryBranchOption> MilitaryBranches { get; }
    public ObservableCollection<ServiceTermOption> ServiceTermOptions { get; }

    public Visibility StepOneVisibility => CurrentStep == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility StepTwoVisibility => CurrentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility StepThreeVisibility => CurrentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility StepFourVisibility => CurrentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
    public bool IsDaysTermMode => ServiceTermMode == ServiceTermMode.Days;
    public bool IsEndDateTermMode => ServiceTermMode == ServiceTermMode.EndDate;
    public string ProgressText => _localization.Format("OnboardingProgress", CurrentStep + 1, LastStep + 1);
    public int ProgressValue => CurrentStep + 1;
    public string PrimaryButtonText => CurrentStep == LastStep ? FinishText : NextText;
    public string SecondaryButtonText => CurrentStep == 0 ? string.Empty : BackText;
    public bool CanContinue => CurrentStep switch
    {
        1 => SelectedBranch is not null,
        2 => IsPeriodValid,
        _ => true
    };

    private bool IsPeriodValid => ServiceTermMode switch
    {
        ServiceTermMode.Days => ServiceLengthDays is >= 1 and <= 3650,
        ServiceTermMode.EndDate => EndDate.Date > StartDate.Date,
        _ => true
    };

    public OnboardingViewModel(
        LocalizationService localization,
        string profileName,
        string currentLanguage,
        MilitaryBranch? currentBranch,
        DateTime startDate,
        DateTime endDate,
        ServiceTermMode serviceTermMode,
        int serviceLengthDays,
        bool notificationsEnabled,
        bool autoStartEnabled,
        IEnumerable<LanguageItem> languages,
        IEnumerable<MilitaryBranchOption> militaryBranches,
        IEnumerable<ServiceTermOption> serviceTermOptions)
    {
        _localization = localization;
        _profileName = profileName;
        _currentLanguage = currentLanguage;
        _startDate = new DateTimeOffset(startDate.Date);
        _endDate = new DateTimeOffset(endDate.Date);
        _serviceTermMode = serviceTermMode;
        _serviceLengthDays = serviceLengthDays;
        _systemNotificationsEnabled = notificationsEnabled;
        _autoStartEnabled = autoStartEnabled;

        Languages = new ObservableCollection<LanguageItem>(languages);
        MilitaryBranches = new ObservableCollection<MilitaryBranchOption>(militaryBranches);
        ServiceTermOptions = new ObservableCollection<ServiceTermOption>(serviceTermOptions);
        _selectedBranch = currentBranch.HasValue
            ? MilitaryBranches.FirstOrDefault(item => item.Branch == currentBranch.Value)
            : null;
    }

    public void MoveNext()
    {
        if (!CanContinue || CurrentStep >= LastStep)
            return;
        CurrentStep++;
    }

    public void MoveBack()
    {
        if (CurrentStep > 0)
            CurrentStep--;
    }

    public OnboardingData CreateResult()
    {
        if (SelectedBranch is null)
            throw new InvalidOperationException("A military branch must be selected.");

        var start = StartDate.Date;
        var days = Math.Clamp(ServiceLengthDays, 1, 3650);
        var end = ServiceTermMode switch
        {
            ServiceTermMode.CalendarYear => start.AddYears(1),
            ServiceTermMode.Days => start.AddDays(days),
            ServiceTermMode.EndDate when EndDate.Date > start => EndDate.Date,
            _ => start.AddYears(1)
        };
        days = (end - start).Days;

        return new OnboardingData(
            ProfileName.Trim(),
            CurrentLanguage,
            SelectedBranch.Branch,
            start,
            ServiceTermMode,
            days,
            end,
            SystemNotificationsEnabled,
            AutoStartEnabled);
    }

    partial void OnCurrentStepChanged(int value) => NotifyWizardState();

    partial void OnSelectedBranchChanged(MilitaryBranchOption? value) =>
        OnPropertyChanged(nameof(CanContinue));

    partial void OnServiceTermModeChanged(ServiceTermMode value)
    {
        OnPropertyChanged(nameof(IsDaysTermMode));
        OnPropertyChanged(nameof(IsEndDateTermMode));
        OnPropertyChanged(nameof(CanContinue));
    }

    partial void OnServiceLengthDaysChanged(int value) => OnPropertyChanged(nameof(CanContinue));
    partial void OnStartDateChanged(DateTimeOffset value) => OnPropertyChanged(nameof(CanContinue));
    partial void OnEndDateChanged(DateTimeOffset value) => OnPropertyChanged(nameof(CanContinue));

    private void NotifyWizardState()
    {
        OnPropertyChanged(nameof(StepOneVisibility));
        OnPropertyChanged(nameof(StepTwoVisibility));
        OnPropertyChanged(nameof(StepThreeVisibility));
        OnPropertyChanged(nameof(StepFourVisibility));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ProgressValue));
        OnPropertyChanged(nameof(PrimaryButtonText));
        OnPropertyChanged(nameof(SecondaryButtonText));
        OnPropertyChanged(nameof(CanContinue));
    }

    public string WelcomeTitle => Get("OnboardingWelcomeTitle");
    public string WelcomeDescription => Get("OnboardingWelcomeDescription");
    public string ProfileNameLabel => Get("ProfileNameLabel");
    public string ProfileNamePlaceholder => Get("ProfileNamePlaceholder");
    public string LanguageLabel => Get("LanguageLabel");
    public string BranchStepTitle => Get("OnboardingBranchTitle");
    public string BranchStepDescription => Get("OnboardingBranchDescription");
    public string BranchPlaceholder => Get("MilitaryBranchPickerPlaceholder");
    public string PeriodStepTitle => Get("OnboardingPeriodTitle");
    public string PeriodStepDescription => Get("OnboardingPeriodDescription");
    public string StartDateLabel => Get("StartDateLabel");
    public string ServiceTermLabel => Get("ServiceTermLabel");
    public string ServiceLengthLabel => Get("ServiceLengthLabel");
    public string EndDateLabel => Get("DembelDatePickerLabel");
    public string ReadyTitle => Get("OnboardingReadyTitle");
    public string ReadyDescription => Get("OnboardingReadyDescription");
    public string NotificationsToggle => Get("SystemNotificationsToggle");
    public string AutoStartToggle => Get("AutoStartToggle");
    public string NextText => Get("Next");
    public string BackText => Get("Back");
    public string FinishText => Get("Finish");
    private string Get(string key) => _localization.GetString(key);
}
