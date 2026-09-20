using DMBTimer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Windows.Storage;

namespace DMBTimer.Services;

public sealed class CustomMilestoneService
{
    private const string StorageFileName = "custom-milestones.json";
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private string StoragePath => Path.Combine(ApplicationData.Current.LocalFolder.Path, StorageFileName);

    public IReadOnlyList<CustomMilestone> Load()
    {
        try
        {
            if (!File.Exists(StoragePath))
                return Array.Empty<CustomMilestone>();
            var json = File.ReadAllText(StoragePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<CustomMilestone>();

            return (JsonSerializer.Deserialize<List<CustomMilestone>>(json, _jsonOptions) ?? new())
                .Where(item => item.Id != Guid.Empty && !string.IsNullOrWhiteSpace(item.Title))
                .Select(Normalize)
                .OrderBy(item => item.Date)
                .ToList();
        }
        catch
        {
            return Array.Empty<CustomMilestone>();
        }
    }

    public void Save(IEnumerable<CustomMilestone> milestones)
    {
        var normalized = milestones
            .Where(item => item.Id != Guid.Empty && !string.IsNullOrWhiteSpace(item.Title))
            .Select(Normalize)
            .OrderBy(item => item.Date)
            .ToList();
        var json = JsonSerializer.Serialize(normalized, _jsonOptions);
        var temporaryPath = StoragePath + ".tmp";
        File.WriteAllText(temporaryPath, json, Encoding.UTF8);
        File.Move(temporaryPath, StoragePath, overwrite: true);
    }

    private static CustomMilestone Normalize(CustomMilestone item) => new()
    {
        Id = item.Id,
        Title = item.Title.Trim(),
        Description = item.Description?.Trim() ?? string.Empty,
        Date = item.Date.Date,
        NotificationsEnabled = item.NotificationsEnabled
    };
}
