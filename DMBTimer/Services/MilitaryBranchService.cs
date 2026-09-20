using DMBTimer.Models;
using DMBTimer.ViewModels;
using System;
using System.Collections.Generic;

namespace DMBTimer.Services;

public sealed class MilitaryBranchService
{
    private sealed record BranchDefinition(
        string NameKey,
        string HolidayTitleKey,
        string Glyph,
        Func<int, DateTime> DateForYear);

    private readonly LocalizationService _localization;

    private static readonly IReadOnlyDictionary<MilitaryBranch, BranchDefinition> Definitions =
        new Dictionary<MilitaryBranch, BranchDefinition>
        {
            [MilitaryBranch.MotorRifle] = Fixed("Branch_MotorRifle", "BranchHoliday_GroundForces", "\uD83E\uDE96", 10, 1),
            [MilitaryBranch.Artillery] = Fixed("Branch_Artillery", "BranchHoliday_Artillery", "\uD83D\uDCA5", 11, 19),
            [MilitaryBranch.MilitaryCounterintelligence] = Fixed("Branch_Counterintelligence", "BranchHoliday_Counterintelligence", "\uD83D\uDEE1\uFE0F", 12, 19),
            [MilitaryBranch.Rhbz] = Fixed("Branch_Rhbz", "BranchHoliday_Rhbz", "\u2623\uFE0F", 11, 13),
            [MilitaryBranch.Tank] = Moving("Branch_Tank", "BranchHoliday_Tank", "\uD83D\uDEE1\uFE0F", year => NthWeekday(year, 9, DayOfWeek.Sunday, 2)),
            [MilitaryBranch.Airborne] = Fixed("Branch_Airborne", "BranchHoliday_Airborne", "\uD83E\uDE82", 8, 2),
            [MilitaryBranch.Navy] = Moving("Branch_Navy", "BranchHoliday_Navy", "\u2693", year => LastWeekday(year, 7, DayOfWeek.Sunday)),
            [MilitaryBranch.AirForce] = Fixed("Branch_AirForce", "BranchHoliday_AirForce", "\u2708\uFE0F", 8, 12),
            [MilitaryBranch.AirDefense] = Moving("Branch_AirDefense", "BranchHoliday_AirDefense", "\uD83D\uDEE1\uFE0F", year => NthWeekday(year, 4, DayOfWeek.Sunday, 2)),
            [MilitaryBranch.Engineering] = Fixed("Branch_Engineering", "BranchHoliday_Engineering", "\u2699\uFE0F", 1, 21),
            [MilitaryBranch.Signals] = Fixed("Branch_Signals", "BranchHoliday_Signals", "\uD83D\uDCE1", 10, 20),
            [MilitaryBranch.SpecialForces] = Fixed("Branch_SpecialForces", "BranchHoliday_SpecialForces", "\uD83C\uDFAF", 10, 24),
            [MilitaryBranch.Railway] = Fixed("Branch_Railway", "BranchHoliday_Railway", "\uD83D\uDE82", 8, 6),
            [MilitaryBranch.MilitaryIntelligence] = Fixed("Branch_MilitaryIntelligence", "BranchHoliday_MilitaryIntelligence", "\uD83E\uDDED", 11, 5),
            [MilitaryBranch.Marines] = Fixed("Branch_Marines", "BranchHoliday_Marines", "\u2693", 11, 27),
            [MilitaryBranch.StrategicMissile] = Fixed("Branch_StrategicMissile", "BranchHoliday_StrategicMissile", "\uD83D\uDE80", 12, 17),
            [MilitaryBranch.SpaceForces] = Fixed("Branch_SpaceForces", "BranchHoliday_SpaceForces", "\uD83D\uDEF0\uFE0F", 10, 4),
            [MilitaryBranch.Logistics] = Fixed("Branch_Logistics", "BranchHoliday_Logistics", "\uD83D\uDE9A", 8, 1),
            [MilitaryBranch.BorderGuard] = Fixed("Branch_BorderGuard", "BranchHoliday_BorderGuard", "\uD83D\uDFE2", 5, 28),
            [MilitaryBranch.NationalGuard] = Fixed("Branch_NationalGuard", "BranchHoliday_NationalGuard", "\uD83C\uDDF7\uD83C\uDDFA", 3, 27),
            [MilitaryBranch.MilitaryPolice] = Fixed("Branch_MilitaryPolice", "BranchHoliday_MilitaryPolice", "\uD83D\uDE94", 2, 8)
        };

    public MilitaryBranchService(LocalizationService localization)
    {
        _localization = localization;
    }

    public IReadOnlyList<MilitaryBranchOption> GetOptions()
    {
        var options = new List<MilitaryBranchOption>(Definitions.Count);
        foreach (var pair in Definitions)
        {
            options.Add(new MilitaryBranchOption
            {
                Branch = pair.Key,
                DisplayName = _localization.GetString(pair.Value.NameKey)
            });
        }

        return options;
    }

    public List<Milestone> BuildHolidayMilestones(
        MilitaryBranch branch,
        DateTime startDate,
        DateTime endDate)
    {
        var result = new List<Milestone>();
        if (!Definitions.TryGetValue(branch, out var definition))
            return result;

        for (var year = startDate.Year; year <= endDate.Year; year++)
        {
            var date = definition.DateForYear(year);
            if (date < startDate.Date || date >= endDate.Date)
                continue;

            result.Add(new Milestone(
                $"branch_{branch}_{year}",
                date,
                _localization.GetString(definition.HolidayTitleKey),
                _localization.Format(
                    "BranchHoliday_Description",
                    _localization.GetString(definition.NameKey)),
                MilestoneKind.BranchHoliday,
                definition.Glyph));
        }

        return result;
    }

    private static BranchDefinition Fixed(
        string nameKey,
        string holidayTitleKey,
        string glyph,
        int month,
        int day) =>
        new(nameKey, holidayTitleKey, glyph, year => new DateTime(year, month, day));

    private static BranchDefinition Moving(
        string nameKey,
        string holidayTitleKey,
        string glyph,
        Func<int, DateTime> dateForYear) =>
        new(nameKey, holidayTitleKey, glyph, dateForYear);

    private static DateTime NthWeekday(
        int year,
        int month,
        DayOfWeek dayOfWeek,
        int occurrence)
    {
        var first = new DateTime(year, month, 1);
        var offset = ((int)dayOfWeek - (int)first.DayOfWeek + 7) % 7;
        return first.AddDays(offset + (occurrence - 1) * 7);
    }

    private static DateTime LastWeekday(int year, int month, DayOfWeek dayOfWeek)
    {
        var last = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        var offset = ((int)last.DayOfWeek - (int)dayOfWeek + 7) % 7;
        return last.AddDays(-offset);
    }
}
