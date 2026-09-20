# Contributing

Thanks for helping improve DMBTimer.

## Development setup

Requirements:

- Windows 10 version 1809 or later, or Windows 11;
- .NET 8 SDK;
- Visual Studio 2022 with the Windows App SDK / WinUI 3 workload.

Build and verify the project before submitting a change:

```powershell
dotnet restore .\DMBTimer.slnx
dotnet build .\DMBTimer.slnx -c Debug -p:Platform=x64
dotnet run --project .\BackupSmokeTest\BackupSmokeTest.csproj -c Debug
```

## Pull requests

1. Keep changes focused and explain the user-visible reason.
2. Follow the existing MVVM and service boundaries.
3. Add Russian and English resources for every new user-facing string.
4. Do not commit certificates, package artifacts, `.vs`, `bin`, or `obj` directories.
5. Use Conventional Commits, for example `fix(backup): reject malformed archives`.
6. Describe manual checks when UI behavior cannot be covered automatically.

Bug reports should include the Windows version, application build, reproduction steps, expected behavior, and screenshots or logs when relevant. Never attach a backup containing personal data to a public issue.
