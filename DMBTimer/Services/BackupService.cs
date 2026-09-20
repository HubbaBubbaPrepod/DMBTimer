using DMBTimer.Models;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DMBTimer.Services;

public sealed class BackupService
{
    private const string DataEntryName = "dmbtimer.json";
    private const string SoundEntryName = "notification.wav";
    private const long MaximumSoundBytes = 25 * 1024 * 1024;
    private const long MaximumDataBytes = 2 * 1024 * 1024;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task ExportAsync(string destinationPath, AppBackup backup, string? soundPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        backup.CreatedAtUtc = DateTime.UtcNow;
        backup.HasCustomSound = !string.IsNullOrWhiteSpace(soundPath) && File.Exists(soundPath);

        await using var output = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            81920,
            useAsync: true);
        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);

        var dataEntry = archive.CreateEntry(DataEntryName, CompressionLevel.Optimal);
        await using (var dataStream = dataEntry.Open())
        {
            await JsonSerializer.SerializeAsync(dataStream, backup, _jsonOptions);
        }

        if (backup.HasCustomSound)
        {
            var soundInfo = new FileInfo(soundPath!);
            if (soundInfo.Length > MaximumSoundBytes)
                throw new InvalidDataException("The notification sound is larger than 25 MB.");

            var soundEntry = archive.CreateEntry(SoundEntryName, CompressionLevel.Optimal);
            await using var source = new FileStream(soundPath!, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            await using var target = soundEntry.Open();
            await source.CopyToAsync(target);
        }
    }

    public async Task<BackupImportResult> ImportAsync(
        string sourcePath,
        string? soundDestinationPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        await using var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        using var archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);

        var dataEntry = archive.GetEntry(DataEntryName)
            ?? throw new InvalidDataException("The backup does not contain dmbtimer.json.");
        if (dataEntry.Length > MaximumDataBytes)
            throw new InvalidDataException("The backup data is larger than 2 MB.");
        AppBackup backup;
        await using (var dataStream = dataEntry.Open())
        {
            backup = await JsonSerializer.DeserializeAsync<AppBackup>(dataStream, _jsonOptions)
                ?? throw new InvalidDataException("The backup data is empty.");
        }

        Validate(backup);

        var restoredSoundPath = string.Empty;
        var soundEntry = archive.GetEntry(SoundEntryName);
        if (backup.HasCustomSound && soundEntry is null)
            throw new InvalidDataException("The backup does not contain its notification sound.");

        if (backup.HasCustomSound && soundEntry is not null)
        {
            if (soundEntry.Length > MaximumSoundBytes)
                throw new InvalidDataException("The notification sound is larger than 25 MB.");

            if (string.IsNullOrWhiteSpace(soundDestinationPath))
                throw new InvalidDataException("A destination path is required for the notification sound.");

            restoredSoundPath = soundDestinationPath;
            var targetDirectory = Path.GetDirectoryName(restoredSoundPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
                Directory.CreateDirectory(targetDirectory);
            var temporarySoundPath = restoredSoundPath + ".importing";
            try
            {
                await using (var source = soundEntry.Open())
                await using (var target = new FileStream(
                                 temporarySoundPath,
                                 FileMode.Create,
                                 FileAccess.Write,
                                 FileShare.None,
                                 81920,
                                 useAsync: true))
                {
                    await source.CopyToAsync(target);
                }

                File.Move(temporarySoundPath, restoredSoundPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporarySoundPath))
                    File.Delete(temporarySoundPath);
            }
        }

        return new BackupImportResult(backup, restoredSoundPath);
    }

    private static void Validate(AppBackup backup)
    {
        if (backup.FormatVersion is < 1 or > 1)
            throw new InvalidDataException("Unsupported DMBTimer backup version.");
        if (backup.StartDate == default || backup.EndDate <= backup.StartDate)
            throw new InvalidDataException("The backup contains an invalid service period.");
        if (backup.ServiceLengthDays is < 1 or > 3650)
            throw new InvalidDataException("The backup contains an invalid service length.");
        if (!Enum.IsDefined(backup.ServiceTermMode) || !Enum.IsDefined(backup.DisplayMode))
            throw new InvalidDataException("The backup contains an unknown option value.");
        if (backup.MilitaryBranch.HasValue && !Enum.IsDefined(backup.MilitaryBranch.Value))
            throw new InvalidDataException("The backup contains an unknown military branch.");
        if (backup.Theme is not ("Default" or "Light" or "Dark"))
            backup.Theme = "Default";
        if (backup.Language is not ("ru-RU" or "en-US"))
            backup.Language = "ru-RU";
        backup.CustomMilestones ??= new();
        if (backup.CustomMilestones.Count > 1000 ||
            backup.CustomMilestones.Any(item =>
                item.Id == Guid.Empty ||
                string.IsNullOrWhiteSpace(item.Title) ||
                item.Title.Length > 160 ||
                item.Description is null ||
                item.Description.Length > 2000))
            throw new InvalidDataException("The backup contains invalid personal milestones.");
        if (backup.ProfileName?.Length > 80)
            throw new InvalidDataException("The profile name is too long.");
    }
}
