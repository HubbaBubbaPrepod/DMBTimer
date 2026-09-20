using CommunityToolkit.Mvvm.Input;
using DMBTimer.Dialogs;
using DMBTimer.Models;
using DMBTimer.Services;
using DMBTimer.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Text;

namespace DMBTimer;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    private readonly PageTransitionService _pageTransitionService = new();
    private readonly NotificationSoundService _soundService = new();
    private readonly GlobalHotkeyService _globalHotkeyService = new();
    private readonly BackupService _backupService = new();
    private readonly TextBlock[] _modeTexts;
    private bool _isExiting;
    private bool _isLoaded;

    public MainWindow()
    {
        ViewModel = new MainViewModel();
        InitializeComponent();
        LayoutRoot.DataContext = this;

        _modeTexts = new[] { ClassicText, WeeksText, DaysText, HoursText, MinutesText, SecondsText };

        TrayIcon.LeftClickCommand = new RelayCommand(BringToFront);
        TrayIcon.DoubleClickCommand = new RelayCommand(BringToFront);
        TrayShowMenuItem.Command = new RelayCommand(BringToFront);
        TrayStatsMenuItem.Command = new RelayCommand(OpenStats);
        TrayExitMenuItem.Command = new RelayCommand(ExitApplication);

        AppWindow.Closing += AppWindow_Closing;
        Closed += MainWindow_Closed;

        ViewModel.ThemeChanged += OnThemeChanged;
        ViewModel.StatsUpdated += UpdateStatsPage;
        ViewModel.SoundSelectionRequested += OnSoundSelectionRequested;
        ViewModel.SoundPreviewRequested += OnSoundPreviewRequested;
        ViewModel.LanguageChanged += UpdateLocalizedChrome;
        ViewModel.MilestoneReached += OnMilestoneReached;
        ViewModel.CustomMilestoneEditRequested += OnCustomMilestoneEditRequested;
        ViewModel.CustomMilestoneDeleteRequested += OnCustomMilestoneDeleteRequested;
        ViewModel.BackupExportRequested += OnBackupExportRequested;
        ViewModel.BackupImportRequested += OnBackupImportRequested;
        ViewModel.TestNotificationRequested += OnTestNotificationRequested;
        ViewModel.OnboardingRequested += OnOnboardingRequested;
        ViewModel.UserNotificationRequested += ShowNotification;
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.CurrentDisplayMode))
                UpdateModeSelection(ViewModel.CurrentDisplayMode);
        };

        _globalHotkeyService.Invoked += BringToFront;

        ApplyTheme(ViewModel.CurrentTheme);
        UpdateModeSelection(ViewModel.CurrentDisplayMode);
        UpdateLocalizedChrome();
    }

    private async void LayoutRoot_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        await ViewModel.InitializeAutoStartAsync();

        if (ViewModel.NeedsOnboarding)
            await ShowOnboardingDialogAsync(required: true);

        if (!_globalHotkeyService.Register(this))
        {
            ShowNotification(
                ViewModel.GetString("HotkeyUnavailableTitle"),
                ViewModel.GetString("HotkeyUnavailableMessage"));
        }

        ViewModel.Start();
    }

    private async Task<bool> ShowOnboardingDialogAsync(bool required)
    {
        var dialog = new OnboardingDialog(
            ViewModel.CreateOnboardingViewModel(),
            required,
            ViewModel.Cancel)
        {
            XamlRoot = LayoutRoot.XamlRoot
        };

        await dialog.ShowAsync();
        if (dialog.Result is null)
            return false;

        await ViewModel.CompleteOnboardingAsync(dialog.Result);
        UpdateModeSelection(ViewModel.CurrentDisplayMode);
        return true;
    }

    private async void OnOnboardingRequested() =>
        await ShowOnboardingDialogAsync(required: false);

    private async Task<bool> ShowMilitaryBranchDialogAsync(bool required)
    {
        while (true)
        {
            var branchPicker = new ComboBox
            {
                ItemsSource = ViewModel.MilitaryBranches,
                DisplayMemberPath = nameof(MilitaryBranchOption.DisplayName),
                PlaceholderText = ViewModel.MilitaryBranchPickerPlaceholder,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinWidth = 360
            };

            if (ViewModel.CurrentMilitaryBranch.HasValue)
            {
                branchPicker.SelectedItem = ViewModel.MilitaryBranches.FirstOrDefault(
                    option => option.Branch == ViewModel.CurrentMilitaryBranch.Value);
            }

            var content = new StackPanel { Spacing = 14, MaxWidth = 520 };
            content.Children.Add(new TextBlock
            {
                Text = ViewModel.MilitaryBranchWelcomeDescription,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.8
            });
            content.Children.Add(branchPicker);

            var dialog = new ContentDialog
            {
                XamlRoot = LayoutRoot.XamlRoot,
                Title = ViewModel.MilitaryBranchWelcomeTitle,
                Content = content,
                PrimaryButtonText = ViewModel.MilitaryBranchContinue,
                IsPrimaryButtonEnabled = branchPicker.SelectedItem is not null,
                DefaultButton = ContentDialogButton.Primary
            };

            if (!required)
                dialog.CloseButtonText = ViewModel.Cancel;

            branchPicker.SelectionChanged += (_, _) =>
                dialog.IsPrimaryButtonEnabled = branchPicker.SelectedItem is not null;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary &&
                branchPicker.SelectedItem is MilitaryBranchOption selected)
            {
                ViewModel.SelectMilitaryBranch(selected.Branch);
                return true;
            }

            if (!required)
                return false;
        }
    }

    private async void ChangeMilitaryBranch_Click(object sender, RoutedEventArgs e) =>
        await ShowMilitaryBranchDialogAsync(required: false);

    private async void OnCustomMilestoneEditRequested(CustomMilestone? existing)
    {
        var today = DateTime.Today;
        var suggestedDate = today < ViewModel.StartDate
            ? ViewModel.StartDate
            : today > ViewModel.EndDate
                ? ViewModel.EndDate
                : today;
        var editor = new CustomMilestoneEditorViewModel(
            LocalizationService.Instance,
            existing,
            suggestedDate);
        var dialog = new CustomMilestoneDialog(editor)
        {
            XamlRoot = LayoutRoot.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.Result is not null)
            ViewModel.UpsertCustomMilestone(dialog.Result);
    }

    private void CustomMilestoneEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CustomMilestoneItemViewModel item })
            OnCustomMilestoneEditRequested(item.Model);
    }

    private void CustomMilestoneDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CustomMilestoneItemViewModel item })
            OnCustomMilestoneDeleteRequested(item.Model);
    }

    private async void OnCustomMilestoneDeleteRequested(CustomMilestone milestone)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = LayoutRoot.XamlRoot,
            Title = ViewModel.GetString("CustomMilestoneDeleteTitle"),
            Content = ViewModel.GetString("CustomMilestoneDeleteConfirmation"),
            PrimaryButtonText = ViewModel.CustomMilestoneDelete,
            CloseButtonText = ViewModel.Cancel,
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            ViewModel.DeleteCustomMilestone(milestone.Id);
    }

    private async void OnBackupExportRequested()
    {
        try
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"DMBTimer-backup-{DateTime.Today:yyyy-MM-dd}"
            };
            picker.FileTypeChoices.Add(
                ViewModel.GetString("BackupFileType"),
                new List<string> { ".dmbbackup" });
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker,
                WinRT.Interop.WindowNative.GetWindowHandle(this));

            var file = await picker.PickSaveFileAsync();
            if (file is null)
                return;

            CachedFileManager.DeferUpdates(file);
            await _backupService.ExportAsync(
                file.Path,
                ViewModel.CreateBackup(),
                ViewModel.NotificationSoundPath);
            await CachedFileManager.CompleteUpdatesAsync(file);
            ShowNotification(
                ViewModel.GetString("BackupExportedTitle"),
                ViewModel.GetString("BackupExportedMessage"));
        }
        catch (Exception exception)
        {
            ShowNotification(
                ViewModel.GetString("BackupErrorTitle"),
                $"{ViewModel.GetString("BackupErrorMessage")} {exception.Message}");
        }
    }

    private async void OnBackupImportRequested()
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".dmbbackup");
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker,
                WinRT.Interop.WindowNative.GetWindowHandle(this));

            var file = await picker.PickSingleFileAsync();
            if (file is null)
                return;

            var confirmation = new ContentDialog
            {
                XamlRoot = LayoutRoot.XamlRoot,
                Title = ViewModel.GetString("BackupImportConfirmTitle"),
                Content = ViewModel.GetString("BackupImportConfirmMessage"),
                PrimaryButtonText = ViewModel.BackupImport,
                CloseButtonText = ViewModel.Cancel,
                DefaultButton = ContentDialogButton.Close
            };
            if (await confirmation.ShowAsync() != ContentDialogResult.Primary)
                return;

            var imported = await _backupService.ImportAsync(
                file.Path,
                System.IO.Path.Combine(
                    ApplicationData.Current.LocalFolder.Path,
                    "notification.wav"));
            await ViewModel.RestoreBackupAsync(imported);
            UpdateModeSelection(ViewModel.CurrentDisplayMode);
            ShowNotification(
                ViewModel.GetString("BackupImportedTitle"),
                ViewModel.GetString("BackupImportedMessage"));
        }
        catch (Exception exception)
        {
            ShowNotification(
                ViewModel.GetString("BackupErrorTitle"),
                $"{ViewModel.GetString("BackupErrorMessage")} {exception.Message}");
        }
    }

    private void OnTestNotificationRequested()
    {
        if (!ViewModel.SystemNotificationsEnabled)
        {
            ShowNotification(
                ViewModel.GetString("NotificationsDisabledTitle"),
                ViewModel.GetString("NotificationsDisabledMessage"));
            return;
        }

        NotificationService.Show(
            ViewModel.GetString("TestNotificationTitle"),
            ViewModel.GetString("TestNotificationMessage"),
            ViewModel.GetString("NotificationOpen"));
    }

    private async void MainNavigationView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            await ShowSettingsPageAsync();
            return;
        }

        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
            return;

        switch (tag)
        {
            case "Timer":
                await ShowTimerPageAsync();
                break;
            case "Stats":
                await ShowStatsPageAsync();
                break;
        }
    }

    private Task ShowTimerPageAsync() =>
        _pageTransitionService.SwitchAsync(TimerPage, StatsPage, SettingsPage);

    private async Task ShowStatsPageAsync()
    {
        ViewModel.UpdateStats();
        await _pageTransitionService.SwitchAsync(StatsPage, TimerPage, SettingsPage);
    }

    private Task ShowSettingsPageAsync() =>
        _pageTransitionService.SwitchAsync(SettingsPage, TimerPage, StatsPage);

    private void TimerAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SelectNavigationItem("Timer");
        args.Handled = true;
    }

    private void StatsAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SelectNavigationItem("Stats");
        args.Handled = true;
    }

    private void SettingsAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        MainNavigationView.SelectedItem = MainNavigationView.SettingsItem;
        args.Handled = true;
    }

    private void SelectNavigationItem(string tag)
    {
        var item = MainNavigationView.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(candidate => string.Equals(candidate.Tag?.ToString(), tag, StringComparison.Ordinal));
        if (item is not null)
            MainNavigationView.SelectedItem = item;
    }

    private void UpdateStatsPage()
    {
        if (!_isLoaded) return;
        ServiceCalendar.MinDate = new DateTimeOffset(ViewModel.StartDate.AddDays(-3));
        ServiceCalendar.MaxDate = new DateTimeOffset(ViewModel.EndDate.AddDays(3));
        ServiceCalendar.UpdateLayout();
    }

    private void ServiceCalendar_CalendarViewDayItemChanging(
        CalendarView sender,
        CalendarViewDayItemChangingEventArgs args)
    {
        var date = args.Item.Date.Date;
        var isOutsideServicePeriod = date < ViewModel.StartDate || date > ViewModel.EndDate;

        // CalendarView reuses day containers, so reset both visual states explicitly.
        args.Item.SetDensityColors(Array.Empty<Windows.UI.Color>());
        args.Item.Opacity = isOutsideServicePeriod ? 0.35 : 1.0;
    }

    private void DateButton_Click(object sender, RoutedEventArgs e) => DateFlyout.ShowAt(DateButton);

    private void StartDatePicker_DateChanged(object sender, DatePickerValueChangedEventArgs e)
    {
        if (!_isLoaded || !StartDatePicker.SelectedDate.HasValue) return;
        ViewModel.SetStartDateCommand.Execute(StartDatePicker.SelectedDate.Value.Date);
        DateFlyout.Hide();
    }

    private void ModeText_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is TextBlock { Tag: string modeName } &&
            Enum.TryParse<DisplayMode>(modeName, out var mode))
            ViewModel.SetDisplayModeCommand.Execute(mode);
    }

    private void UpdateModeSelection(DisplayMode mode)
    {
        foreach (var text in _modeTexts)
        {
            text.FontWeight = FontWeights.Normal;
            text.TextDecorations = TextDecorations.None;
        }

        var selected = mode switch
        {
            DisplayMode.Classic => ClassicText,
            DisplayMode.Weeks => WeeksText,
            DisplayMode.Days => DaysText,
            DisplayMode.Hours => HoursText,
            DisplayMode.Minutes => MinutesText,
            DisplayMode.Seconds => SecondsText,
            _ => ClassicText
        };
        selected.FontWeight = FontWeights.Bold;
        selected.TextDecorations = TextDecorations.Underline;
    }

    private async void OnThemeChanged(object? sender, string theme)
    {
        await AnimateOpacityAsync(LayoutRoot.Opacity, 0.82, 110);
        ApplyTheme(theme);
        await AnimateOpacityAsync(LayoutRoot.Opacity, 1, 180);
    }

    private void ApplyTheme(string theme)
    {
        LayoutRoot.RequestedTheme = theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        ThemeToggleIcon.Text = theme switch
        {
            "Light" => "☀️",
            "Dark" => "🌙",
            _ => "🌓"
        };
    }

    private Task AnimateOpacityAsync(double from, double to, int milliseconds)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(milliseconds),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(animation, LayoutRoot);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Children.Add(animation);
        storyboard.Completed += (_, _) => completion.TrySetResult(true);
        storyboard.Begin();
        return completion.Task;
    }

    private async void OnSoundSelectionRequested()
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".wav");
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker,
                WinRT.Interop.WindowNative.GetWindowHandle(this));

            var file = await picker.PickSingleFileAsync();
            if (file is null) return;

            var path = await _soundService.ImportWavAsync(file);
            ViewModel.SetSoundPath(path);
            ShowNotification(
                ViewModel.GetString("SoundSelectedTitle"),
                ViewModel.GetString("SoundSelectedMessage"));
            await _soundService.PlayAsync(path);
        }
        catch (Exception exception)
        {
            ShowNotification(
                ViewModel.GetString("SoundErrorTitle"),
                $"{ViewModel.GetString("SoundErrorMessage")} {exception.Message}");
        }
    }

    private async void OnSoundPreviewRequested()
    {
        try
        {
            await _soundService.PlayAsync(ViewModel.NotificationSoundPath);
        }
        catch (Exception exception)
        {
            ShowNotification(ViewModel.GetString("SoundErrorTitle"), exception.Message);
        }
    }

    private async void OnMilestoneReached(object? sender, Milestone milestone)
    {
        NotificationService.Show(
            milestone.Title,
            milestone.Description,
            ViewModel.GetString("NotificationOpen"));
        try
        {
            await _soundService.PlayAsync(ViewModel.NotificationSoundPath);
        }
        catch
        {
            // The notification remains useful even if audio playback fails.
        }

        if (milestone.Kind == MilestoneKind.Dembel)
            ShowDembelCelebration();
    }

    public void ShowNotification(string title, string message)
    {
        NotificationTitle.Text = title;
        NotificationMessage.Text = message;
        NotificationHost.Visibility = Visibility.Visible;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            NotificationHost.Visibility = Visibility.Collapsed;
        };
        timer.Start();
    }

    private void ShowDembelCelebration()
    {
        DembelOverlay.Visibility = Visibility.Visible;
        DembelCelebrationStoryboard.Begin();
    }

    private void DembelOverlayClose_Click(object sender, RoutedEventArgs e)
    {
        DembelOverlay.Visibility = Visibility.Collapsed;
        DembelOverlay.Opacity = 0;
    }

    private void UpdateLocalizedChrome()
    {
        Title = ViewModel.AppTitle;
        TrayIcon.ToolTipText = ViewModel.AppTitle;
        TrayShowMenuItem.Text = ViewModel.GetString("TrayShow");
        TrayStatsMenuItem.Text = ViewModel.GetString("TrayStats");
        TrayExitMenuItem.Text = ViewModel.GetString("TrayExit");
        if (MainNavigationView.SettingsItem is NavigationViewItem settingsItem)
            settingsItem.Content = ViewModel.NavigationSettings;
    }

    public void BringToFront()
    {
        H.NotifyIcon.WindowExtensions.Show(this);
        Activate();
    }

    private void OpenStats()
    {
        BringToFront();
        SelectNavigationItem("Stats");
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _globalHotkeyService.Dispose();
        Close();
        Application.Current.Exit();
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isExiting) return;
        args.Cancel = true;
        H.NotifyIcon.WindowExtensions.Hide(this);
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (!_isExiting) return;

        ViewModel.SaveSettings();
        ViewModel.Dispose();
        _globalHotkeyService.Dispose();
        _soundService.Dispose();
        NotificationService.Uninitialize();
        TrayIcon.Dispose();
    }
}
