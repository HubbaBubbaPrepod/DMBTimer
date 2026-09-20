using DMBTimer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DMBTimer.Services;

public sealed class MilestoneService
{
    private static readonly int[] ServedMarks = { 10, 30, 50, 100, 150, 200, 250, 300 };
    private static readonly int[] LeftMarks = { 200, 150, 100, 50, 30, 20, 14, 10, 7, 5, 3, 1 };
    private readonly LocalizationService _localization;
    private readonly MilitaryBranchService _militaryBranchService;

    public MilestoneService(
        LocalizationService localization,
        MilitaryBranchService militaryBranchService)
    {
        _localization = localization;
        _militaryBranchService = militaryBranchService;
    }

    public List<Milestone> Build(
        DateTime startDate,
        DateTime endDate,
        MilitaryBranch? militaryBranch,
        IEnumerable<CustomMilestone>? customMilestones = null)
    {
        startDate = startDate.Date;
        endDate = endDate.Date;
        if (endDate <= startDate)
            endDate = startDate.AddDays(1);

        var totalDays = (endDate - startDate).Days;
        var list = new List<Milestone>
        {
            new(
                "start",
                startDate,
                _localization.GetString("Milestone_Start_Title"),
                _localization.GetString("Milestone_Start_Desc"),
                MilestoneKind.ServiceStart,
                "🎖️")
        };

        foreach (var days in ServedMarks.Where(days => days < totalDays))
        {
            var word = DaysWord(days);
            list.Add(new Milestone(
                $"served_{days}", startDate.AddDays(days),
                _localization.Format("Milestone_Served_Title", days, word),
                _localization.Format("Milestone_Served_Desc", days, word),
                MilestoneKind.DaysServed, "📅"));
        }

        var half = totalDays / 2;
        if (half > 0)
        {
            list.Add(new Milestone(
                "half", startDate.AddDays(half),
                _localization.GetString("Milestone_Half_Title"),
                _localization.GetString("Milestone_Half_Desc"),
                MilestoneKind.HalfService, "⚓"));
        }

        foreach (var days in LeftMarks.Where(days => days < totalDays))
        {
            var date = endDate.AddDays(-days);
            if (date <= startDate) continue;
            var word = DaysWord(days);
            list.Add(new Milestone(
                $"left_{days}", date,
                _localization.Format("Milestone_Left_Title", days, word),
                _localization.Format("Milestone_Left_Desc", days, word),
                MilestoneKind.DaysBeforeDembel, "⏳"));
        }

        AddYearlyIfInRange(list, startDate, endDate, "spring", 3, 1,
            "Milestone_Season_Spring_Title", "Milestone_Season_Spring_Desc", "🌱");
        AddYearlyIfInRange(list, startDate, endDate, "summer", 6, 1,
            "Milestone_Season_Summer_Title", "Milestone_Season_Summer_Desc", "☀️");
        AddYearlyIfInRange(list, startDate, endDate, "autumn", 9, 1,
            "Milestone_Season_Autumn_Title", "Milestone_Season_Autumn_Desc", "🍂");
        AddYearlyIfInRange(list, startDate, endDate, "winter", 12, 1,
            "Milestone_Season_Winter_Title", "Milestone_Season_Winter_Desc", "❄️");

        for (var year = startDate.Year; year <= endDate.Year; year++)
        {
            var newYear = new DateTime(year, 1, 1);
            if (newYear >= startDate && newYear < endDate)
            {
                list.Add(new Milestone(
                    $"newyear_{year}", newYear,
                    _localization.Format("Milestone_NewYear_Title", year),
                    _localization.GetString("Milestone_NewYear_Desc"),
                    MilestoneKind.NewYear, "🎆"));
            }

            var defenderDay = new DateTime(year, 2, 23);
            if (defenderDay >= startDate && defenderDay < endDate)
            {
                list.Add(new Milestone(
                    $"holiday_2_23_{year}", defenderDay,
                    _localization.GetString("Milestone_Holiday_Defender_Title"),
                    _localization.GetString("Milestone_Holiday_Defender_Desc"),
                    MilestoneKind.Holiday, "🎖️"));
            }
        }

        if (militaryBranch.HasValue)
        {
            list.AddRange(_militaryBranchService.BuildHolidayMilestones(
                militaryBranch.Value,
                startDate,
                endDate));
        }

        if (customMilestones is not null)
        {
            foreach (var custom in customMilestones.Where(item =>
                         item.Date.Date >= startDate && item.Date.Date <= endDate))
            {
                list.Add(new Milestone(
                    $"custom_{custom.Id:N}",
                    custom.Date,
                    custom.Title,
                    custom.Description,
                    MilestoneKind.Custom,
                    "\u2B50",
                    custom.NotificationsEnabled));
            }
        }

        list.Add(new Milestone(
            "dembel", endDate,
            _localization.GetString("Milestone_Dembel_Title"),
            _localization.GetString("Milestone_Dembel_Desc"),
            MilestoneKind.Dembel, "🥳"));

        return list
            .GroupBy(milestone => milestone.Key)
            .Select(group => group.First())
            .OrderBy(milestone => milestone.Date)
            .ToList();
    }

    private void AddYearlyIfInRange(
        ICollection<Milestone> list, DateTime startDate, DateTime endDate,
        string key, int month, int day, string titleKey, string descriptionKey, string glyph)
    {
        for (var year = startDate.Year; year <= endDate.Year; year++)
        {
            var date = new DateTime(year, month, day);
            if (date < startDate || date >= endDate) continue;
            list.Add(new Milestone(
                $"{key}_{year}", date,
                _localization.GetString(titleKey),
                _localization.GetString(descriptionKey),
                MilestoneKind.SeasonStart, glyph));
        }
    }

    private string DaysWord(int number) => _localization.GetPlural(
        number, "UnitDayOne", "UnitDayFew", "UnitDayMany");
}
