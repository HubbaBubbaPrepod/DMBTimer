using CommunityToolkit.Mvvm.ComponentModel;
using DMBTimer.Models;
using DMBTimer.Services;
using System;

namespace DMBTimer.ViewModels;

public partial class CustomMilestoneEditorViewModel : ObservableObject
{
    private readonly LocalizationService _localization;
    private readonly Guid _id;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private DateTimeOffset _date;
    [ObservableProperty] private bool _notificationsEnabled = true;

    public bool CanSave => !string.IsNullOrWhiteSpace(Title);

    public CustomMilestoneEditorViewModel(
        LocalizationService localization,
        CustomMilestone? existing,
        DateTime suggestedDate)
    {
        _localization = localization;
        _id = existing?.Id ?? Guid.NewGuid();
        _title = existing?.Title ?? string.Empty;
        _description = existing?.Description ?? string.Empty;
        _date = new DateTimeOffset((existing?.Date ?? suggestedDate).Date);
        _notificationsEnabled = existing?.NotificationsEnabled ?? true;
    }

    public CustomMilestone CreateResult() => new()
    {
        Id = _id,
        Title = Title.Trim(),
        Description = Description.Trim(),
        Date = Date.Date,
        NotificationsEnabled = NotificationsEnabled
    };

    partial void OnTitleChanged(string value) => OnPropertyChanged(nameof(CanSave));

    public string DialogTitle => Get("CustomMilestoneDialogTitle");
    public string TitleLabel => Get("CustomMilestoneTitleLabel");
    public string DescriptionLabel => Get("CustomMilestoneDescriptionLabel");
    public string DateLabel => Get("CustomMilestoneDateLabel");
    public string NotifyLabel => Get("CustomMilestoneNotifyLabel");
    public string SaveText => Get("Save");
    public string CancelText => Get("Cancel");
    private string Get(string key) => _localization.GetString(key);
}
