using DMBTimer.Models;
using DMBTimer.Services;

var path = Path.Combine(Path.GetTempPath(), $"dmbtimer-{Guid.NewGuid():N}.dmbbackup");
try
{
    var milestoneId = Guid.NewGuid();
    var backup = new AppBackup
    {
        ProfileName = "Test",
        StartDate = new DateTime(2024, 2, 29),
        EndDate = new DateTime(2025, 2, 28),
        ServiceLengthDays = 365,
        ServiceTermMode = ServiceTermMode.EndDate,
        MilitaryBranch = MilitaryBranch.Artillery,
        CustomMilestones =
        {
            new CustomMilestone
            {
                Id = milestoneId,
                Title = "Oath",
                Description = "Smoke test",
                Date = new DateTime(2024, 3, 15)
            }
        }
    };

    var service = new BackupService();
    await service.ExportAsync(path, backup, null);
    var restored = await service.ImportAsync(path);
    if (restored.Backup.ProfileName != backup.ProfileName ||
        restored.Backup.MilitaryBranch != backup.MilitaryBranch ||
        restored.Backup.CustomMilestones.Single().Id != milestoneId ||
        restored.NotificationSoundPath.Length != 0)
        throw new InvalidOperationException("Backup round-trip mismatch.");

    Console.WriteLine("BACKUP_ROUNDTRIP_OK");
}
finally
{
    if (File.Exists(path))
        File.Delete(path);
}
