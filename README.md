# TempCleaner

A Windows Presentation Foundation (WPF) application for cleaning temporary files from Windows systems.

## Overview

TempCleaner is a Windows-specific application that helps users clean temporary files from various system locations including:
- Windows Prefetch
- Software Distribution Downloads
- Windows Temp folders
- User-specific temporary directories
- Internet Cache
- Explorer temporary files
- Crash dumps
- System log files

## Requirements

- **Windows Operating System** (Windows 10 or later recommended)
- **.NET 8.0 Runtime** with Windows Desktop support
- **Administrator privileges** (recommended for accessing all system directories)

## Running the Application

### Prerequisites
1. Ensure you have .NET 8.0 Runtime installed with Windows Desktop support
2. For development, install Visual Studio 2022 or Visual Studio Build Tools with .NET 8.0 SDK

### Building from Source
```cmd
dotnet restore
dotnet build
```

### Running
```cmd
dotnet run
```

Or build and run the executable:
```cmd
dotnet publish -c Release -r win-x64 --self-contained
```

## Configuration

### GitHub Update Feature
The application can check for updates from GitHub. To enable this feature:

1. Create a GitHub Personal Access Token (optional, for rate limiting)
2. Update `appsettings.json` with your token:
```json
{
  "GitHub": {
    "Token": "your_github_token_here"
  }
}
```

**Note**: The token is optional. If not provided, the update check will be skipped.

## Features

- **Scan Mode**: Safely scan and identify temporary files without deleting them
- **Clean Mode**: Delete identified temporary files
- **Progress Tracking**: Real-time progress display during scanning and cleaning
- **Automatic Update Check**: Check for newer versions on application startup
- **Safe Operation**: Only deletes files/folders with proper read/write permissions

## Security Notes

- Never commit real GitHub tokens to version control
- The application requests administrator privileges for system directory access
- Always review the file list before cleaning
- The application includes safety checks to prevent deletion of critical files

## Troubleshooting

### "Application is not running"
This usually indicates one of the following issues:

1. **Platform Compatibility**: This is a Windows-only application and cannot run on Linux/macOS
2. **Missing .NET Runtime**: Ensure .NET 8.0 with Windows Desktop support is installed
3. **Permission Issues**: Run as administrator if experiencing access denied errors
4. **Missing Dependencies**: Restore NuGet packages with `dotnet restore`

### Build Errors
- Ensure you're building on a Windows machine with the Windows SDK installed
- Visual Studio 2022 with .NET 8.0 workload is recommended for development

## Development

### Architecture
- **MainWindow.xaml/MainWindow.xaml.cs**: Main UI and application logic
- **GitHubUpdater.cs**: Handles GitHub API integration for updates
- **DirFinder.cs**: Core file system scanning functionality
- **Custom.cs**: Data models and configuration classes

### Contributing
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test on Windows
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.