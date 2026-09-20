# DMBTimer

WinUI 3 application for tracking a service period, milestones and the discharge date.

## Requirements

- Windows 10 version 1809 or newer
- .NET 8 SDK
- Visual Studio with the Windows App SDK / WinUI workload, or a compatible `dotnet` setup

## Build

```powershell
dotnet restore .\DMBTimer.slnx
dotnet build .\DMBTimer.slnx -c Debug -p:Platform=x64
```

Backup round-trip smoke test:

```powershell
dotnet run --project .\BackupSmokeTest\BackupSmokeTest.csproj -c Debug
```

The app supports calendar-year, fixed-day and explicit end-date service terms. A four-step first-run wizard configures the profile name, language, military branch, service period, Windows notifications and autostart. The selected branch's professional holiday is added to milestones and notifications.

Personal milestones can be added, edited and removed from the Statistics page. Each personal milestone can independently enable or disable its notification. Windows app notifications remain active while the app is minimized to the tray, and the notification button restores the main window.

Settings, profile data, personal milestones and the selected WAV can be exported to a portable `.dmbbackup` archive and restored later. `Ctrl+D` restores the window globally, while `Ctrl+1`, `Ctrl+2` and `Ctrl+3` switch between pages.

Translations live under `DMBTimer\Strings\<language-tag>\Resources.resw`. Add a new language directory and resource file to extend the language list.
