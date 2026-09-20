using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace DMBTimer.Services;

public enum AutoStartFailure
{
    None,
    DisabledByUser,
    DisabledByPolicy,
    Error
}

public readonly record struct AutoStartResult(bool IsEnabled, AutoStartFailure Failure = AutoStartFailure.None);

public sealed class AutoStartService
{
    private const string StartupTaskId = "DMBTimerAutoStartV3";
    private const string LegacyShortcutFileName = "DMBTimer.lnk";
    private const string LegacyRunPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string LegacyRunValueName = "DMBTimer";

    public async Task<AutoStartResult> GetStateAsync()
    {
        RemoveLegacyEntries();

        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            return FromState(task.State);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[AutoStartService] Reading StartupTask failed: {exception.Message}");
            return new AutoStartResult(false, AutoStartFailure.Error);
        }
    }

    public async Task<AutoStartResult> SetEnabledAsync(bool enable)
    {
        RemoveLegacyEntries();

        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            if (!enable)
            {
                if (task.State == StartupTaskState.Enabled)
                    task.Disable();

                return FromState(task.State);
            }

            if (task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy)
                return new AutoStartResult(true);

            if (task.State == StartupTaskState.DisabledByUser)
                return new AutoStartResult(false, AutoStartFailure.DisabledByUser);

            if (task.State == StartupTaskState.DisabledByPolicy)
                return new AutoStartResult(false, AutoStartFailure.DisabledByPolicy);

            return FromState(await task.RequestEnableAsync());
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[AutoStartService] Updating StartupTask failed: {exception.Message}");
            return new AutoStartResult(false, AutoStartFailure.Error);
        }
    }

    private static AutoStartResult FromState(StartupTaskState state) => state switch
    {
        StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy => new AutoStartResult(true),
        StartupTaskState.DisabledByUser => new AutoStartResult(false, AutoStartFailure.DisabledByUser),
        StartupTaskState.DisabledByPolicy => new AutoStartResult(false, AutoStartFailure.DisabledByPolicy),
        StartupTaskState.Disabled => new AutoStartResult(false),
        _ => new AutoStartResult(false, AutoStartFailure.Error)
    };

    private static void RemoveLegacyEntries()
    {
        try
        {
            var shortcut = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                LegacyShortcutFileName);
            if (File.Exists(shortcut))
                File.Delete(shortcut);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[AutoStartService] Removing legacy shortcut failed: {exception.Message}");
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(LegacyRunPath, writable: true);
            key?.DeleteValue(LegacyRunValueName, throwOnMissingValue: false);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[AutoStartService] Removing legacy Run entry failed: {exception.Message}");
        }
    }
}
