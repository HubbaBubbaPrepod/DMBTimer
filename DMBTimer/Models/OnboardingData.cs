using System;

namespace DMBTimer.Models;

public sealed record OnboardingData(
    string ProfileName,
    string Language,
    MilitaryBranch MilitaryBranch,
    DateTime StartDate,
    ServiceTermMode ServiceTermMode,
    int ServiceLengthDays,
    DateTime EndDate,
    bool SystemNotificationsEnabled,
    bool AutoStartEnabled);
