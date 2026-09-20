using System;

namespace DMBTimer.Models
{
    public enum MilestoneKind
    {
        ServiceStart,
        DaysServed,
        HalfService,
        SeasonStart,
        NewYear,
        DaysBeforeDembel,
        Dembel,
        Holiday,
        BranchHoliday,
        Custom
    }

    public sealed class Milestone
    {
        public string Key { get; }
        public DateTime Date { get; }
        public string Title { get; }
        public string Description { get; }
        public MilestoneKind Kind { get; }
        public string Glyph { get; }
        public bool NotificationsEnabled { get; }

        public Milestone(
            string key,
            DateTime date,
            string title,
            string description,
            MilestoneKind kind,
            string glyph,
            bool notificationsEnabled = true)
        {
            Key = key;
            Date = date.Date;
            Title = title;
            Description = description;
            Kind = kind;
            Glyph = glyph;
            NotificationsEnabled = notificationsEnabled;
        }
    }
}
