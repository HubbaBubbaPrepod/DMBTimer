using System;
using System.Collections.Generic;

namespace DMBTimer.Models;

public sealed class AppBackup
{
    public int FormatVersion { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ProfileName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ServiceLengthDays { get; set; }
    public ServiceTermMode ServiceTermMode { get; set; }
    public MilitaryBranch? MilitaryBranch { get; set; }
    public string Theme { get; set; } = "Default";
    public string Language { get; set; } = "ru-RU";
    public DisplayMode DisplayMode { get; set; } = DisplayMode.Classic;
    public bool AutoStartEnabled { get; set; }
    public bool SystemNotificationsEnabled { get; set; } = true;
    public bool HasCustomSound { get; set; }
    public List<CustomMilestone> CustomMilestones { get; set; } = new();
}

public sealed record BackupImportResult(AppBackup Backup, string NotificationSoundPath);
