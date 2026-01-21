# Codebase Analysis — TempCleaner

This document summarizes the repository's architecture, core responsibilities, and recommendations for maintainability and future work.

## Architecture Overview

- Application type: WPF desktop application targeting `net8.0-windows`.
- Pattern: simple event-driven UI with separated helper classes for scanning and updates. The codebase appears to follow a pragmatic structure rather than a strict MVVM implementation.

## Core Components

- UI Layer
  - `MainWindow.xaml` / `MainWindow.xaml.cs`: primary user interface and interaction entry points.
  - `DownloadProgressWindow.xaml` / `.xaml.cs`: used for long-running downloads or updates.
- Application Bootstrap
  - `App.xaml` / `App.xaml.cs`: WPF application lifecycle.
- Core Logic
  - `DirFinder.cs`: directory discovery and scanning logic for temporary file locations.
  - `FileStatusReport.cs`: representation of discovered files and their metadata.
  - `TempCleaner.csproj`: project configuration and target frameworks.

# Codebase Analysis — TempCleaner

This document records a developer-focused analysis of the codebase: architecture, hot-paths, notable design choices, and prioritized recommendations.

## Architecture Overview

- Application type: WPF desktop app targeting `net8.0-windows`.
- Style: pragmatic code-behind UI with helper classes for scanning, reporting, and updates. Core performance-sensitive logic uses allocation-reducing patterns.

## Core Components (detailed)

- `MainWindow.xaml` / `MainWindow.xaml.cs`

  - Central UI and orchestration for scanning, logging, and purge flows.
  - Maintains an in-memory list of discovered file/directory paths and performs batched deletes.
  - Reads optional environment variables (and `.env`) for GitHub configuration.

- `DirFinder.cs`

  - Uses a shared `EnumerationOptions` instance to enumerate files and directories with `IgnoreInaccessible` and `RecurseSubdirectories` set.
  - Periodically reports progress via an `Action<FileStatusReport>?` callback. It reports every 256 files to lower callback overhead.
  - Swallows exceptions during enumeration to keep scans resilient on protected paths.

- `FileStatusReport.cs`

  - Small `readonly struct` representing a file count snapshot to minimize allocations when reporting progress.

- `GitHubUpdater.cs`

  - Lazy `HttpClient` initialization with appropriate GitHub headers.
  - Attempts unauthenticated access with an option to retry using a provided token.
  - Parses release JSON for `.msi` assets (uses `browser_download_url` or `url` when token is used) and falls back to constructing a download URL if no asset found.
  - Implements `CompareVersions` using spans and `stackalloc` for efficient parsing and comparison.

- `DownloadProgressWindow.xaml.cs`

  - Streams downloads using `HttpClient` with `HttpCompletionOption.ResponseHeadersRead` and writes to a temp file with buffered async I/O.
  - Updates UI periodically (every ~200ms or after a certain byte threshold) and calculates download speed.
  - Cleans up partial downloads on cancel.

- `CustomMessageBox.cs`

  - In-app dynamic dialog builder for consistent theming and user prompts.

- `Installer/`
  - WiX sources for MSI packaging. Building requires WiX toolchain.

## Implementation Observations

- Performance-aware code: usage of `readonly struct`, `stackalloc`, and shared `EnumerationOptions` shows attention to low allocation and throughput in scans.
- Error handling is pragmatic: many operations catch and ignore exceptions to keep the UX responsive, but this reduces diagnostic visibility.
- UI code is in the code-behind and mixes orchestration with presentation; moving core logic to services/ViewModels will aid testability.

## Risks and Caveats

- Silent exception swallowing in enumeration and deletes can hide permission issues or other failures — consider logging.
- Building the installer or running certain cleanup tasks may require elevated privileges; the app will skip inaccessible items rather than escalate.
- The update fallback URL assumes a release naming pattern; if release assets are named differently, the fallback may fail.

## Recommended Improvements (prioritized)

1. Tests (High)

   - Add a `tests/` project and unit tests for:
     - `DirFinder` (integration tests using temp directories or abstracted FS)
     - `GitHubUpdater.ParseReleaseInfo` and `CompareVersions`

2. Observability (High)

   - Add structured logging (`Microsoft.Extensions.Logging`) to capture enumeration failures, download errors, and unexpected exceptions.

3. Safety & UX (High)

   - Add a preview/dry-run mode to show deletions without performing them.
   - Add per-path toggles for included locations and an 'advanced' confirmation for system folders.

4. Refactor for testability (Medium)

   - Introduce small services and interfaces (e.g., `IFileEnumerator`, `IUpdateService`) and a lightweight DI container or factory wiring.
   - Move long-running logic out of `MainWindow` into ViewModels/Services.

5. CI & Releases (Medium)

   - Add GitHub Actions to run `dotnet restore`, `dotnet build`, and style checks on PRs. Add a release job to publish `dotnet publish` artifacts.

6. Security (Medium)
   - Ensure tokens are never logged. Document secure usage of `GITHUB_TOKEN` via environment variables and `.env` ignored from VCS.

## Low-effort starter tasks for contributors

- Add two unit tests and a GitHub Actions build workflow.
- Add a `dry-run` flag to the purge flow and a simple UI toggle.

## Offer

I can scaffold the `tests/` project and a minimal GitHub Actions workflow, or extract one small service (e.g., version parsing) into a testable class. Which do you prefer next?
