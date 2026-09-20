using DMBTimer.Models;
using System;
using System.Globalization;

namespace DMBTimer.ViewModels;

public sealed class MilestoneItemViewModel
{
    public Milestone Model { get; }
    public string Key => Model.Key;
    public DateTime Date => Model.Date;
    public string Glyph => Model.Glyph;
    public string Title => Model.Title;
    public string Description => Model.Description;
    public string DateText { get; }
    public double TextOpacity { get; }

    public MilestoneItemViewModel(Milestone model, DateTime today, CultureInfo culture)
    {
        Model = model;
        DateText = model.Date.ToString("d", culture);
        TextOpacity = model.Date < today.Date ? 0.55 : 1.0;
    }
}

public sealed class LanguageItem
{
    public string Code { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class ServiceTermOption
{
    public ServiceTermMode Mode { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class MilitaryBranchOption
{
    public MilitaryBranch Branch { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class CustomMilestoneItemViewModel
{
    public CustomMilestone Model { get; }
    public Guid Id => Model.Id;
    public string Title => Model.Title;
    public string Description => Model.Description;
    public bool NotificationsEnabled => Model.NotificationsEnabled;
    public string DateText { get; }

    public CustomMilestoneItemViewModel(CustomMilestone model, CultureInfo culture)
    {
        Model = model;
        DateText = model.Date.ToString("d", culture);
    }
}
