using DMBTimer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DMBTimer.Services;

public sealed class MilestoneNotificationService
{
    private readonly SettingsService _settings;

    public MilestoneNotificationService(SettingsService settings)
    {
        _settings = settings;
    }

    public IReadOnlyList<Milestone> GetDueMilestones(IEnumerable<Milestone> milestones, DateTime today)
    {
        today = today.Date;
        var notified = _settings.LoadNotifiedMilestones();
        var lastChecked = _settings.LoadLastCheckedDate();

        var due = milestones
            .Where(m => m.NotificationsEnabled)
            .Where(m => !notified.Contains(m.Key))
            .Where(m => m.Date == today ||
                        (lastChecked.HasValue && m.Date > lastChecked.Value.Date && m.Date <= today))
            .OrderBy(m => m.Date)
            .ToList();

        foreach (var milestone in due)
            notified.Add(milestone.Key);

        _settings.SaveNotifiedMilestones(notified);
        _settings.SaveLastCheckedDate(today);
        return due;
    }
}
