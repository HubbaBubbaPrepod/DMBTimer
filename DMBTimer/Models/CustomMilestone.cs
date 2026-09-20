using System;

namespace DMBTimer.Models;

public sealed class CustomMilestone
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public bool NotificationsEnabled { get; set; } = true;
}
