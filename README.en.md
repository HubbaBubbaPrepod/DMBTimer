# DMBTimer

<p align="center">
  <img src="DMBTimer/Assets/Square1254x1254Logo.png" width="96" alt="DMBTimer logo">
</p>

> A modern Windows service countdown with milestones, military branch holidays, and native notifications.

[Русская версия](README.md)

[![CI](https://github.com/HubbaBubbaPrepod/DMBTimer/actions/workflows/ci.yml/badge.svg)](https://github.com/HubbaBubbaPrepod/DMBTimer/actions/workflows/ci.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/UI-WinUI%203-0078D4)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![Windows](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?logo=windows11)](https://www.microsoft.com/windows/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

DMBTimer tracks time until the end of a service period, visualizes progress and important dates, and sends reminders for milestones and military holidays. It is a native WinUI 3 and .NET 8 application that lives in the system tray and stores its data locally.

[Features](#features) · [Getting started](#getting-started) · [Architecture](docs/ARCHITECTURE.md) · [Development](#development) · [Issues](https://github.com/HubbaBubbaPrepod/DMBTimer/issues)

## Features

| Feature | Implementation |
| --- | --- |
| Accurate service terms | Calendar year, fixed number of days, or an explicit end date |
| First-run experience | Guided setup for profile, language, branch, term, notifications, and autostart |
| Progress tracking | Elapsed and remaining days, percentage, next milestone, and statistics |
| Milestones | Built-in stages, holidays, and user-defined dates |
| Military holidays | Automatically calculated for 21 branches, including movable dates |
| Notifications | Windows App Notifications, duplicate prevention, and app activation |
| Sound | Bundled alert or a custom WAV file with preview |
| Windows integration | System tray, autostart, and a global `Ctrl+D` shortcut |
| Interface | Light and dark themes with animated page transitions |
| Localization | Russian and English resource files using `.resw` |
| Backup | Export and restore settings, profile, milestones, and the selected WAV file |

## Service term calculation

DMBTimer does not assume that every service period is exactly 365 days.

| Mode | Behavior |
| --- | --- |
| Calendar year | The end date uses `AddYears(1)`, so leap years are handled correctly |
| Number of days | Accepts a service term from 1 to 3650 days |
| End date | Uses an explicitly selected discharge date |

All milestones and percentages are calculated from the actual start and end dates.

## Military branches and holidays

The selected military branch is part of the first-run profile. Its professional holiday is automatically added as a milestone for every applicable service year.

The current catalog covers motor rifle, tank, artillery, military counterintelligence, CBRN defense, airborne, navy, air force, air defense, engineering, signals, special forces, railway troops, military intelligence, marines, strategic missile forces, space forces, logistics, border guard, national guard, and military police.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl+1` | Timer |
| `Ctrl+2` | Statistics |
| `Ctrl+3` | Settings |
| `Ctrl+D` | Restore the window globally |

## Requirements

- Windows 10 version 1809 or later, or Windows 11;
- .NET 8 SDK;
- Visual Studio 2022 with the Windows App SDK / WinUI 3 workload for IDE development.

## Getting started

A ready-to-install package has not been published yet. Build the project from Visual Studio or the terminal:

```powershell
git clone https://github.com/HubbaBubbaPrepod/DMBTimer.git
cd DMBTimer
dotnet restore .\DMBTimer.slnx
dotnet build .\DMBTimer.slnx -c Debug -p:Platform=x64
```

Open `DMBTimer.slnx` in Visual Studio and start the `DMBTimer` project with package deployment enabled.

## Data and privacy

Profile data, preferences, and custom milestones stay on the local Windows device. Export creates a portable `.dmbbackup` archive and includes the selected custom WAV file when present. The application requires no account and sends no personal data to a server.

## Architecture

The UI follows MVVM using `CommunityToolkit.Mvvm`. `MainViewModel` owns presentation state, while date calculation, milestones, notifications, localization, autostart, and backups live in dedicated services.

See the [architecture document](docs/ARCHITECTURE.md) for component boundaries and data flows.

## Development

```powershell
dotnet restore .\DMBTimer.slnx
dotnet build .\DMBTimer.slnx -c Debug -p:Platform=x64
dotnet run --project .\BackupSmokeTest\BackupSmokeTest.csproj -c Debug
```

The last command exercises a complete backup export/restore round trip. GitHub Actions runs the same checks for every push and pull request.

Add another language under `DMBTimer/Strings/<language-tag>/Resources.resw`. See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidelines.

## Project status

DMBTimer is under active development. Core user flows are implemented; an installable release, real interface screenshots, and broader automated test coverage are planned before the first stable release.

## License

[MIT](LICENSE). Please report security issues according to [SECURITY.md](SECURITY.md) instead of opening a public issue.
