# TempCleaner

TempCleaner is a lightweight Windows desktop utility for locating and removing temporary files and other unused artifacts to free disk space. It provides a GUI (WPF) front-end, targeted for Windows, and includes an update mechanism and installer artifacts.

## Key Features

- Graphical UI for scanning and cleaning temporary locations
- Progress reporting during long operations
- Update-checker tied to GitHub releases
- Installer project (WiX) included for packaging

## Requirements

- Windows 10 or later
- .NET 8.0 (net8.0-windows)
- WiX Toolset (to build installer artifacts)

## Build from source

1. Install the .NET 8.0 SDK: https://dotnet.microsoft.com

# TempCleaner

TempCleaner is a small, fast Windows desktop utility (WPF) for locating and removing temporary files and other unused artifacts to reclaim disk space. It provides a simple GUI for scanning, reviewing results, and purging items, plus an update-check mechanism that can download installer assets from GitHub releases.

This README includes instructions for both end users and developers.

## Highlights

- Fast recursive scanning of common system and user temporary locations
- Progress UI and logs to review discovered items before removal
- Download-and-install update flow using GitHub release assets
- WiX installer sources included for creating MSI packages

## Quick Start (End users)

1. Download a release from the repository Releases page: https://github.com/isubroto/temp_cleaner/releases
2. Run the MSI installer or the published EXE to install and launch the app.
3. In the app: click `Deep Scan` (or `Scan`) to discover candidate files, review logs, then click `Purge` (or `Clean`) to remove items.

Safety notes for users:

- The app scans well-known system and user-temp locations (e.g., `C:\Windows\Temp`, `C:\Users\<you>\AppData\Local\Temp`, Windows Update download folders). Review scan logs before purging.
- Some system folders require administrator privileges to remove contents. The app will skip inaccessible items silently.
- Purged files are removed from disk; the app also attempts to empty the Recycle Bin after cleanup.

## Installation (Developer / Advanced user)

Prerequisites

- Windows 10 or later
- .NET 8.0 SDK (for building): https://dotnet.microsoft.com
- (Optional) WiX Toolset to build MSI installers: https://wixtoolset.org

Build and run locally

```powershell
git clone https://github.com/isubroto/temp_cleaner.git
cd temp_cleaner
dotnet restore
dotnet build -c Release
dotnet run --project TempCleaner.csproj
```

Create a self-contained publish for Windows x64

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
# result will be in bin/Release/net8.0-windows/win-x64/publish
```

Build the installer (WiX)

- The `Installer/` directory contains WiX `.wxs` sources. Building the MSI requires WiX/MSBuild integration (Visual Studio or WiX MSBuild targets). See WiX docs for instructions.

## Usage details (what the app scans)

- Common system locations (example list implemented in `MainWindow.xaml.cs`):
  - `C:\Windows\Prefetch`
  - `C:\Windows\SoftwareDistribution\Download`
  - `C:\Windows\Temp`
  - `C:\Windows\System32\LogFiles` and `C:\Windows\Logs`
  - `C:\Users\<User>\AppData\Local\Temp`
  - `C:\Users\<User>\AppData\Local\CrashDumps`

The scanner uses `DirFinder.DirandFile(...)` to enumerate files and directories with safe `EnumerationOptions` and emits periodic progress reports. Files and directories are deleted directly when the user confirms purge; inaccessible items are skipped.

## Update checking

- The app calls the GitHub Releases API to find the latest release and looks for `.msi` assets. If a downloadable MSI is found, it can either download it with progress UI or open the release page in the browser. The lookup and download logic is implemented in `GitHubUpdater.cs`.

Environment variables and `.env`

- For development or CI, the app reads optional environment variables from the system or a `.env` file in the app base or working directory:
  - `GITHUB_TOKEN` — personal access token for GitHub API (useful to avoid rate limits or to access private repos)
  - `GITHUB_OWNER` — override owner (defaults to the repo owner)
  - `GITHUB_REPO` — override repo (defaults to `temp_cleaner`)

Do NOT commit secrets. If you use a token locally, store it only in your environment or an ignored `.env` file.

## Developer notes & architecture

- UI: `MainWindow.xaml` / `MainWindow.xaml.cs` — code-behind contains the scan and purge flows and UI plumbing.
- Scanning: `DirFinder.cs` — optimized recursive file enumeration using `EnumerationOptions` and periodic progress callbacks.
- Reporting: `FileStatusReport.cs` — a small readonly struct used to avoid allocations when reporting counts.
- Updates: `GitHubUpdater.cs` — HTTP-based release lookup, version comparison (span/stackalloc for speed), and download invocation via `DownloadProgressWindow`.
- Download: `DownloadProgressWindow.xaml.cs` — streams download to a temp path with periodic UI updates and speed calculation.
- Installer: `Installer/` contains WiX sources for packaging an MSI.

For a more extensive code overview and recommendations for contributors, see `CODEBASE_ANALYSIS.md`.

## Contributing

Please read `CONTRIBUTING.md` for contribution workflows, coding standards, and PR expectations. The project welcomes issues and PRs — small, focused changes are easiest to review.

## License

This project is distributed under the MIT License. See `LICENSE` for full terms.

## Contact & Support

- Repository: https://github.com/isubroto/temp_cleaner
- Open an issue for bugs, feature requests, or documentation improvements.

## Maintenance & Best Practices

- Add automated CI to run `dotnet restore`, `dotnet build`, and `dotnet format` on PRs.
- Add unit tests (suggest a `tests/` project) for `DirFinder` and version parsing in `GitHubUpdater`.
- Consider refactoring UI logic into MVVM to improve testability.
- Add a dry-run / preview-only mode before deletion to increase safety for users.
